using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARCeye.Dataset.Recorder
{
    public class Recorder : MonoBehaviour
    {
        // ARMetaDatasetRecorder의 결과를 디코딩한 디렉토리 경로.
        // Editor 모드에서만 사용 된다.
        [SerializeField]
        private string m_DecodedDatasetPath;

        [SerializeField]
        private ARCameraManager m_CameraManager;
        private DecodedDatasetManager m_DatasetManager;

        public UnityAction<FrameData> OnFrameUpdated { get; set; }

        private Transform m_ARCamera;
        private GeoCoordProvider m_GeoCoordProvider;

        // 레코딩 정보.
        private string m_RecordingPath;
        private int m_RecordingFramerate;
        public string recordingPath => m_RecordingPath;

        // rgb 이미지 관련.
        private Texture2D m_CameraRequestTexture;
        private Vector2Int m_ImageSize;
        private const int MAJOR_AXIS_LENGTH = 640;  // 장축의 길이를 640으로 고정.
        private float[] m_DisplayMatrix = new float[9];
        private bool m_IsPortrait;


        // 각종 변수.
        private float m_PrevRecordTime;
        private float m_StartHeight;


        private void Awake()
        {
            // 30fps 단위로 레코딩.
            m_RecordingFramerate = 30;
            m_ARCamera = Camera.main.transform;

            InitComponents();
        }

        private void InitComponents()
        {
            m_GeoCoordProvider = GetComponent<GeoCoordProvider>();

#if UNITY_EDITOR
            m_DatasetManager = FindAnyObjectByType<DecodedDatasetManager>();
            if (m_DatasetManager == null)
            {
                Debug.LogError("Failed to find DecodedDatasetManager");
            }
            m_DatasetManager.datasetUrl = m_DecodedDatasetPath;
            m_DatasetManager.framerate = m_RecordingFramerate;
#else
            m_CameraManager = FindAnyObjectByType<ARCameraManager>();
            if(m_CameraManager == null)
            {
                Debug.LogError("Failed to find ARCameraManager");
            }
#endif
        }

        public void StartRecording()
        {
            string datasetName = TimeUtility.GetCurrentTimeString();

#if !UNITY_EDITOR && UNITY_ANDROID
            m_RecordingPath = GetAndroidExternalFolderPath(datasetName);
#else
            m_RecordingPath = Application.persistentDataPath + "/" + datasetName;
#endif

            Debug.Log("Start Recording");
            Debug.Log("  Recording Path : " + m_RecordingPath);

            Directory.CreateDirectory(m_RecordingPath);
#if UNITY_EDITOR
            m_DatasetManager.frameReceived += OnFrameDataReceived;
            m_DatasetManager.Play();
#else
            PlaneDetectionHelper planeDetectionHelper = FindAnyObjectByType<PlaneDetectionHelper>();
            Vector3 centerPoint = planeDetectionHelper.centerPoint;
            m_StartHeight = -centerPoint.y;

            m_CameraManager.frameReceived += OnCameraFrameReceived;
#endif
        }

        private string GetAndroidExternalFolderPath(string folderName)
        {
            string bundleId = Application.identifier;
            string externalDir = $"/storage/emulated/0/Android/media/{bundleId}";

            if (!Directory.Exists(externalDir))
            {
                Directory.CreateDirectory(externalDir);
            }

            return Path.Combine(externalDir, folderName);
        }

        public void StopRecording()
        {
#if UNITY_EDITOR
            m_DatasetManager.Stop();
            m_DatasetManager.frameReceived -= OnFrameDataReceived;
#else
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
#endif
        }

        private void OnFrameDataReceived(FrameData frameData)
        {
            Write(frameData);
            OnFrameUpdated?.Invoke(frameData);
        }

        // Device에서 실행되는 callback method.
        private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            // 사전에 설정한 framerate에 도달했는지 확인.
            if (!CheckRecordingInterval())
            {
                return;
            }

            if (!m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return;
            }

            if (!m_CameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsic))
            {
                return;
            }

            CalculateRequestedImageSize(image);

            var buffer = CreateRequestedBufferOfTexture();

            AssignResizedPreviewToBufferOfTexture(image, buffer);

            m_CameraRequestTexture.Apply();

            // width가 장축인 경우.
            float scale;
            if (intrinsic.principalPoint.x > intrinsic.principalPoint.y)
            {
                scale = (float)MAJOR_AXIS_LENGTH / (float)intrinsic.resolution.x;
            }
            // height가 장축인 경우.
            else
            {
                scale = (float)MAJOR_AXIS_LENGTH / (float)intrinsic.resolution.y;
            }

            // 획득한 데이터를 저장.
            FrameData frameData = new FrameData();

            // Height 보정.
            Vector3 position = m_ARCamera.position;
            position.y += m_StartHeight;
            m_ARCamera.position = position;

            frameData.texture = m_CameraRequestTexture;
            frameData.timestamp = TimeUtility.GetUnixTime();
            frameData.modelMatrix = Matrix4x4.TRS(m_ARCamera.position, m_ARCamera.rotation, Vector3.one);
            frameData.projMatrix = eventArgs.projectionMatrix ?? Camera.main.projectionMatrix;
            frameData.transMatrix = MakeDisplayRotationMatrix(eventArgs.displayMatrix ?? Matrix4x4.identity);

            // portrait. 수신한 값을 transpose 한다.
            frameData.width = m_CameraRequestTexture.height;
            frameData.height = m_CameraRequestTexture.width;

            frameData.fx = intrinsic.focalLength.y * scale;
            frameData.fy = intrinsic.focalLength.x * scale;
            frameData.cx = intrinsic.principalPoint.y * scale;
            frameData.cy = intrinsic.principalPoint.x * scale;

            frameData.latitude = m_GeoCoordProvider.info.latitude;
            frameData.longitude = m_GeoCoordProvider.info.longitude;

            // Write(frameData);
            // OnFrameUpdated?.Invoke(frameData);

            OnFrameDataReceived(frameData);

            image.Dispose();
        }

        private bool CheckRecordingInterval()
        {
            float currTime = Time.time;
            float frameInterval = 1.0f / m_RecordingFramerate;
            if (currTime - m_PrevRecordTime > frameInterval)
            {
                m_PrevRecordTime = currTime;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CalculateRequestedImageSize(XRCpuImage image)
        {
            if (image.width > image.height)
            {
                float s = (float)MAJOR_AXIS_LENGTH / (float)image.width;
                m_ImageSize = new Vector2Int(MAJOR_AXIS_LENGTH, Mathf.FloorToInt((float)image.height * s));
            }
            else
            {
                float s = (float)MAJOR_AXIS_LENGTH / (float)image.height;
                m_ImageSize = new Vector2Int(Mathf.FloorToInt((float)image.width * s), MAJOR_AXIS_LENGTH);
            }
        }

        private NativeArray<byte> CreateRequestedBufferOfTexture()
        {
            if (m_CameraRequestTexture == null || m_CameraRequestTexture.width != m_ImageSize.x || m_CameraRequestTexture.height != m_ImageSize.y)
            {
                m_CameraRequestTexture = new Texture2D(m_ImageSize.x, m_ImageSize.y, TextureFormat.BGRA32, false);
            }

            return m_CameraRequestTexture.GetRawTextureData<byte>();
        }

        unsafe private void AssignResizedPreviewToBufferOfTexture(XRCpuImage image, NativeArray<byte> buffer)
        {
            XRCpuImage.Transformation transformation;
            // iOS 환경
            if (m_DisplayMatrix[1] == -1 && m_DisplayMatrix[3] == -1)
            {
                transformation = XRCpuImage.Transformation.None;
            }
            // Android 환경. 
            else
            {
                transformation = XRCpuImage.Transformation.MirrorY;
            }

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = m_ImageSize,
                outputFormat = TextureFormat.BGRA32,
                transformation = transformation
            };

            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
        }

        public Matrix4x4 MakeDisplayRotationMatrix(Matrix4x4 rawDispRotMatrix)
        {
            Matrix4x4 deviceDisplayMatrix;

            Vector2 affineBasisX = new Vector2(1.0f, 0.0f);
            Vector2 affineBasisY = new Vector2(0.0f, 1.0f);
            Vector2 affineTranslation = new Vector2(0.0f, 0.0f);
#if UNITY_IOS
            // rawDispRotMatrix를 그대로 사용하면 상하 반전이 된 이미지 획득.
            affineBasisX = new Vector2(rawDispRotMatrix[0, 0], rawDispRotMatrix[1, 0]);
            affineBasisY = new Vector2(-rawDispRotMatrix[0, 1], -rawDispRotMatrix[1, 1]);
            affineTranslation = new Vector2(rawDispRotMatrix[2, 0], rawDispRotMatrix[2, 1]);
#elif UNITY_ANDROID
            affineBasisX = new Vector2(rawDispRotMatrix[0, 0], rawDispRotMatrix[1, 0]);
            affineBasisY = new Vector2(rawDispRotMatrix[0, 1], rawDispRotMatrix[1, 1]);
            affineTranslation = new Vector2(rawDispRotMatrix[2, 0], rawDispRotMatrix[2, 1]);
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

        private void Write(FrameData frameData)
        {
            string frameName = frameData.timestamp.ToString();

            // rgb 이미지 저장.
#if UNITY_EDITOR
            Texture2D rotatedTexture = frameData.texture;
#else
            Texture2D rotatedTexture = ImageUtility.RotateTexture(frameData.texture, frameData.transMatrix);
#endif
            byte[] data = ImageConversion.EncodeToJPG(rotatedTexture, 85);
            System.IO.File.WriteAllBytes($"{m_RecordingPath}/{frameName}.jpg", data);

            try
            {
                string dataPath = m_RecordingPath + "/data.bin";
                using FileStream fileStream = File.Open(dataPath, FileMode.Append);
                using BinaryWriter writer = new(fileStream, Encoding.UTF8);

                string encodedFrame = frameData.Encode();
                writer.Write(encodedFrame);
                writer.Flush();
            }
            catch (IOException e)
            {
                Debug.LogError("File write error: " + e.Message);
            }
        }
    }
}