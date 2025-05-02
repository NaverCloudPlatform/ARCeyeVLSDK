using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARCeye
{
    public class ARFoundationPoseTracker : PoseTracker
    {
        private ARCameraManager m_CameraManager;
        private UnityYuvCpuImage currVideoImage = new UnityYuvCpuImage();

        private const int MAJOR_AXIS_LENGTH = 640;  // 장축의 길이를 640으로 고정.
        private bool m_IsPortrait;

        protected const float DEFAULT_FX = 474.457672f;
        protected const float DEFAULT_FY = 474.457672f;
        protected const float DEFAULT_CX = 240.000000f;
        protected const float DEFAULT_CY = 321.5426635f;


        public override void OnCreate(Config config)
        {
            Debug.Log("Initialize DevicePoseTracker");
            m_CameraManager = GameObject.FindObjectOfType<ARCameraManager>();
            if (m_CameraManager == null)
            {
                Debug.LogError("Failed to find ARCameraManager.");
                Debug.LogError("Failed to find ARCameraManager. Please check AR Session Origin is placed in a scene.");
            }

            m_IsPortrait = (Screen.orientation == ScreenOrientation.Portrait) ||
                           (Screen.orientation == ScreenOrientation.PortraitUpsideDown);

            InitComponents();
        }

        private void InitComponents()
        {
            Dataset.ARDatasetManager datasetManager = GameObject.FindObjectOfType<Dataset.ARDatasetManager>();
            if (datasetManager != null)
            {
                GameObject.Destroy(datasetManager);
            }
        }

        public override void RegisterFrameLoop()
        {
            if (m_CameraManager != null)
            {
                Debug.Log("Enable AR Camera Manager");
                m_CameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        public override void UnregisterFrameLoop()
        {
            if (m_CameraManager != null)
            {
                m_CameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if (!m_IsInitialized)
                return;

            ARFrame frame = CreateARFrame(eventArgs);
            UpdateFrame(frame);
        }

        protected ARFrame CreateARFrame(ARCameraFrameEventArgs eventArgs)
        {
            ARFrame frame = new ARFrame();

            // Camera preview.
            if (!TryAcquireLatestImage(out UnityYuvCpuImage? videoImage, out UnityAction disposable))
            {
                Debug.LogError("Failed to acquire a requested video image");
                return null;
            }

            // Camera model matrix.
            frame.localPosition = m_ARCamera.transform.localPosition;
            frame.localRotation = m_ARCamera.transform.localRotation;

            AquireCameraIntrinsic(out float fx, out float fy, out float cx, out float cy);
            frame.intrinsic = new ARIntrinsic(fx, fy, cx, cy);

            // Projection matrix.
            frame.projMatrix = eventArgs.projectionMatrix ?? Camera.main.projectionMatrix;

            // Display matrix.
            frame.displayMatrix = eventArgs.displayMatrix ?? Matrix4x4.identity;
            frame.displayMatrix = MakeDisplayMatrix(frame.displayMatrix);

            frame.yuvBuffer = videoImage;
            frame.disposable = disposable;

            return frame;
        }

        unsafe public bool TryAcquireLatestImage(out UnityYuvCpuImage? image, out UnityAction disposable)
        {
            if (!m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            {
                Debug.LogError("Failed to acquire latest CPU image using ARCameraManager.");
                image = null;
                disposable = null;

                cpuImage.Dispose();

                return false;
            }

            var format = cpuImage.format;
            if (format == XRCpuImage.Format.AndroidYuv420_888)
            {
                var yPlane = cpuImage.GetPlane(0);
                var uPlane = cpuImage.GetPlane(1);
                var vPlane = cpuImage.GetPlane(2);

                currVideoImage.width = cpuImage.width;
                currVideoImage.height = cpuImage.height;
                currVideoImage.format = (int)format;
                currVideoImage.numberOfPlanes = cpuImage.planeCount;

                currVideoImage.yPixels = new IntPtr(yPlane.data.GetUnsafePtr());
                currVideoImage.yLength = yPlane.data.Length;
                currVideoImage.yRowStride = yPlane.rowStride;
                currVideoImage.yPixelStride = yPlane.pixelStride;

                currVideoImage.uPixels = new IntPtr(uPlane.data.GetUnsafePtr());
                currVideoImage.uLength = uPlane.data.Length;
                currVideoImage.uRowStride = uPlane.rowStride;
                currVideoImage.uPixelStride = uPlane.pixelStride;

                currVideoImage.vPixels = new IntPtr(vPlane.data.GetUnsafePtr());
                currVideoImage.vLength = vPlane.data.Length;
                currVideoImage.vRowStride = vPlane.rowStride;
                currVideoImage.vPixelStride = vPlane.pixelStride;

                currVideoImage.rotationMode = YuvRotationMode.YUV_ROTATION_90;

                image = currVideoImage;
                disposable = () =>
                {
                    cpuImage.Dispose();
                };
                return true;
            }
            else if (format == XRCpuImage.Format.IosYpCbCr420_8BiPlanarFullRange)
            {
                var yPlane = cpuImage.GetPlane(0);
                var uvPlane = cpuImage.GetPlane(1);

                currVideoImage.width = cpuImage.width;
                currVideoImage.height = cpuImage.height;
                currVideoImage.format = (int)format;
                currVideoImage.numberOfPlanes = cpuImage.planeCount;

                currVideoImage.yPixels = new IntPtr(yPlane.data.GetUnsafePtr());
                currVideoImage.yLength = yPlane.data.Length;
                currVideoImage.yRowStride = yPlane.rowStride;
                currVideoImage.yPixelStride = yPlane.pixelStride;

                currVideoImage.uPixels = new IntPtr(uvPlane.data.GetUnsafePtr());
                currVideoImage.uLength = uvPlane.data.Length;
                currVideoImage.uRowStride = uvPlane.rowStride;
                currVideoImage.uPixelStride = uvPlane.pixelStride;

                currVideoImage.rotationMode = YuvRotationMode.YUV_ROTATION_90;

                image = currVideoImage;
                disposable = () =>
                {
                    cpuImage.Dispose();
                };
                return true;
            }
            else
            {
                Debug.LogError("Unsupported image format: " + format);

                image = null;
                disposable = null;
                cpuImage.Dispose();
                return false;
            }
        }

        public void AquireCameraIntrinsic(out float fx, out float fy, out float cx, out float cy)
        {
            if (m_CameraManager.TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics))
            {
                // Device 모드는 MAJOR_AXIS_LENGTH에 맞춰서 이미지를 리사이즈 하고 파라매터들의 스케일을 변경한다.
                // default resolution은 640 * 480
                // fx, fy는 장축의 길이를 기준으로 비율 조절.
                // cx, cy는 각 축의 길이를 기준으로 비율 조절.
                // 이미지를 가져온 뒤에는 m_Config.tracker.previewWidth, previewHeight에 수정된 길이의 값이 입력되어 있다.

                float scale;

                // width가 장축인 경우.
                if (cameraIntrinsics.principalPoint.x > cameraIntrinsics.principalPoint.y)
                {
                    scale = (float)MAJOR_AXIS_LENGTH / (float)cameraIntrinsics.resolution.x;
                }
                // height가 장축인 경우.
                else
                {
                    scale = (float)MAJOR_AXIS_LENGTH / (float)cameraIntrinsics.resolution.y;
                }

                // 스마트폰의 경우 landscape 기준으로 param 전달. x,y 반대로 설정.
                if (CheckIntrinsicTranspose(cameraIntrinsics))
                {
                    fx = cameraIntrinsics.focalLength.y * scale;
                    fy = cameraIntrinsics.focalLength.x * scale;
                    cx = cameraIntrinsics.principalPoint.y * scale;
                    cy = cameraIntrinsics.principalPoint.x * scale;
                }
                else
                {
                    fx = cameraIntrinsics.focalLength.x * scale;
                    fy = cameraIntrinsics.focalLength.y * scale;
                    cx = cameraIntrinsics.principalPoint.x * scale;
                    cy = cameraIntrinsics.principalPoint.y * scale;
                }
            }
            else
            {
                // Intrinsic을 받아올 수 없는 경우 기본값 할당.
                fx = DEFAULT_FX;
                fy = DEFAULT_FY;
                cx = DEFAULT_CX;
                cy = DEFAULT_CY;
            }
        }

        private bool CheckIntrinsicTranspose(XRCameraIntrinsics cameraIntrinsics)
        {
            return
                (m_IsPortrait && cameraIntrinsics.principalPoint.x > cameraIntrinsics.principalPoint.y) ||
                (!m_IsPortrait && cameraIntrinsics.principalPoint.x < cameraIntrinsics.principalPoint.y);
        }

        public Matrix4x4 MakeDisplayMatrix(Matrix4x4 rawDispRotMatrix)
        {
            Matrix4x4 deviceDisplayMatrix;

            Vector2 affineBasisX = new Vector2(1.0f, 0.0f);
            Vector2 affineBasisY = new Vector2(0.0f, 1.0f);
            Vector2 affineTranslation = new Vector2(0.0f, 0.0f);
#if UNITY_IOS
            affineBasisX = new Vector2(rawDispRotMatrix[0, 0], rawDispRotMatrix[1, 0]);
            affineBasisY = new Vector2(rawDispRotMatrix[0, 1], rawDispRotMatrix[1, 1]);
            affineTranslation = new Vector2(rawDispRotMatrix[2, 0], rawDispRotMatrix[2, 1]);
#elif UNITY_ANDROID
            affineBasisX = new Vector2(rawDispRotMatrix[0, 0], rawDispRotMatrix[0, 1]);
            affineBasisY = new Vector2(rawDispRotMatrix[1, 0], rawDispRotMatrix[1, 1]);
            affineTranslation = new Vector2(rawDispRotMatrix[0, 2], rawDispRotMatrix[1, 2]);
#endif
            affineBasisX = affineBasisX.normalized;
            affineBasisY = affineBasisY.normalized;
            deviceDisplayMatrix = Matrix4x4.identity;
            deviceDisplayMatrix[0, 0] = affineBasisX.x;
            deviceDisplayMatrix[0, 1] = affineBasisY.x;
            deviceDisplayMatrix[1, 0] = affineBasisX.y;
            deviceDisplayMatrix[1, 1] = affineBasisY.y;
            deviceDisplayMatrix[2, 0] = Mathf.Round(affineTranslation.x);
            deviceDisplayMatrix[2, 1] = Mathf.Round(affineTranslation.y);

            return deviceDisplayMatrix;
        }
    }

}