using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using AOT;

namespace ARCeye
{
public class DevicePoseTracker : PoseTracker
{
    private ARCameraManager m_CameraManager;
    private Texture2D m_CameraRequestTexture;

    private const int MAJOR_AXIS_LENGTH = 640;  // 장축의 길이를 640으로 고정.
    private float[] m_DisplayMatrix = new float[9];
    private bool m_IsPortrait;

    protected const float DEFAULT_FX = 474.457672f;
    protected const float DEFAULT_FY = 474.457672f;
    protected const float DEFAULT_CX = 240.000000f;
    protected const float DEFAULT_CY = 321.5426635f;


    public override void Initialize(Transform arCamera, Config config)
    {
        Debug.Log("Initialize DevicePoseTracker");
        m_CameraManager = arCamera.GetComponent<ARCameraManager>();
        if(m_CameraManager == null)
        {
            Debug.LogError("Failed to find ARCameraManager.");
            Debug.LogError("Failed to find ARCameraManager. Please check AR Session Origin is placed in a scene.");
        }


        config.tracker.useTranslationFilter = true;
        config.tracker.useRotationFilter = true;
        config.tracker.useInterpolation = true;

        m_IsPortrait = (Screen.orientation == ScreenOrientation.Portrait) || 
                       (Screen.orientation == ScreenOrientation.PortraitUpsideDown);

        // m_Config = config;
        Initialize(config);
        InitComponents();

        int t = m_Config.tracker.previewWidth;
        m_Config.tracker.previewWidth = m_Config.tracker.previewHeight;
        m_Config.tracker.previewHeight = t;
    }

    private void InitComponents()
    {
        Dataset.ARDatasetManager datasetManager = GameObject.FindObjectOfType<Dataset.ARDatasetManager>();
        if(datasetManager != null)
        {
            GameObject.Destroy(datasetManager);
        }
    }

    public override void RegisterFrameLoop()
    {
        if(m_CameraManager != null) 
        {
            Debug.Log("Enable AR Camera Manager");
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    public override void UnregisterFrameLoop()
    {
        if(m_CameraManager != null)
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!m_IsInitialized)
            return;

        Matrix4x4 projMatrix = eventArgs.projectionMatrix ?? Camera.main.projectionMatrix;
        Matrix4x4 transMatrix = eventArgs.displayMatrix ?? Matrix4x4.identity;
        UpdateFrame(projMatrix, transMatrix);
    }


    unsafe public override bool AcquireRequestedTexture(out Texture texture)
    {
        // 디바이스에서 카메라 프레임 이미지를 획득.
        // iOS의 경우 1920 * 1440 이미지가 들어온다.
        if(!m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) 
        {
            texture = null;
            return false;
        }

        CalculateRequestedImageSize(image);

        var buffer = CreateRequestedBufferOfTexture();

        AssignResizedPreviewToBufferOfTexture(image, buffer);

        m_CameraRequestTexture.Apply();

        texture = m_CameraRequestTexture;

        image.Dispose();

        return true;
    }

    public override void AquireCameraIntrinsic(out float fx, out float fy, out float cx, out float cy) 
    {
        if(m_CameraManager.TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics))
        {
            // Device 모드는 MAJOR_AXIS_LENGTH에 맞춰서 이미지를 리사이즈 하고 파라매터들의 스케일을 변경한다.
            // default resolution은 640 * 480
            // fx, fy는 장축의 길이를 기준으로 비율 조절.
            // cx, cy는 각 축의 길이를 기준으로 비율 조절.
            // 이미지를 가져온 뒤에는 m_Config.tracker.previewWidth, previewHeight에 수정된 길이의 값이 입력되어 있다.

            float scale;

            // width가 장축인 경우.
            if(cameraIntrinsics.principalPoint.x > cameraIntrinsics.principalPoint.y)
            {
                scale = (float) MAJOR_AXIS_LENGTH / (float) cameraIntrinsics.resolution.x;
            }
            // height가 장축인 경우.
            else
            {
                scale = (float) MAJOR_AXIS_LENGTH / (float) cameraIntrinsics.resolution.y;
            }
            
            // 스마트폰의 경우 landscape 기준으로 param 전달. x,y 반대로 설정.
            if(CheckIntrinsicTranspose(cameraIntrinsics)) {
                fx = cameraIntrinsics.focalLength.y * scale;
                fy = cameraIntrinsics.focalLength.x * scale;
                cx = cameraIntrinsics.principalPoint.y * scale;
                cy = cameraIntrinsics.principalPoint.x * scale;
            } else {
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
            (m_IsPortrait  && cameraIntrinsics.principalPoint.x > cameraIntrinsics.principalPoint.y) || 
            (!m_IsPortrait && cameraIntrinsics.principalPoint.x < cameraIntrinsics.principalPoint.y);
    }

    private void CalculateRequestedImageSize(XRCpuImage image) 
    {
        if(image.width > image.height) {
            float s = (float) MAJOR_AXIS_LENGTH / (float)image.width;
            m_Config.tracker.previewWidth = MAJOR_AXIS_LENGTH;
            m_Config.tracker.previewHeight = Mathf.FloorToInt((float) image.height * s);
        } else {
            float s = (float) MAJOR_AXIS_LENGTH / (float)image.height;
            m_Config.tracker.previewWidth = Mathf.FloorToInt((float) image.width * s);
            m_Config.tracker.previewHeight = MAJOR_AXIS_LENGTH;
        }
    }

    private NativeArray<byte> CreateRequestedBufferOfTexture()
    {
        if(m_CameraRequestTexture == null || m_CameraRequestTexture.width != m_Config.tracker.previewWidth || m_CameraRequestTexture.height != m_Config.tracker.previewHeight)
        {
            m_CameraRequestTexture = new Texture2D(m_Config.tracker.previewWidth, m_Config.tracker.previewHeight, TextureFormat.BGRA32, false);
        }

        return m_CameraRequestTexture.GetRawTextureData<byte>();
    }

    unsafe private void AssignResizedPreviewToBufferOfTexture(XRCpuImage image, NativeArray<byte> buffer)
    {
        XRCpuImage.Transformation transformation;
        // iOS 환경
        if(m_DisplayMatrix[1] == -1 && m_DisplayMatrix[3] == -1) 
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
            outputDimensions = new Vector2Int(m_Config.tracker.previewWidth, m_Config.tracker.previewHeight),
            outputFormat = TextureFormat.BGRA32,
            transformation = transformation
        };

        image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);    
    }

    public override float[] MakeDisplayRotationMatrix(Matrix4x4 rawDispRotMatrix)
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
        deviceDisplayMatrix[0,0] = affineBasisX.x;
        deviceDisplayMatrix[0,1] = affineBasisY.x;
        deviceDisplayMatrix[1,0] = affineBasisX.y;
        deviceDisplayMatrix[1,1] = affineBasisY.y;
        deviceDisplayMatrix[2,0] = Mathf.Round(affineTranslation.x);
        deviceDisplayMatrix[2,1] = Mathf.Round(affineTranslation.y);
        
        for(int i=0 ; i<3 ; i++) 
        {
            for(int j=0 ; j<3 ; j++)
            {
                m_DisplayMatrix[i * 3 + j] = deviceDisplayMatrix[i, j];
            }
        }

        return m_DisplayMatrix;
    }
}

}