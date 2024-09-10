using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ARCeye.Dataset;

namespace ARCeye
{
public class EditorPoseTracker : PoseTracker
{
    private ARDatasetManager m_ARDatasetManager;
    private TextureProvider m_TextureProvider;

    private float[] m_DisplayMatrix = new float[9];

    const float MAJOR_AXIS_LENGTH = 640.0f;  // 장축의 길이를 640으로 고정.

    protected const float DEFAULT_FX = 469.672760f;
    protected const float DEFAULT_FY = 469.672760f;
    protected const float DEFAULT_CX = 179.404327f;
    protected const float DEFAULT_CY = 315.172272f;

    private Camera m_MainCamera;


    public override void Initialize(Transform coroutineRunner, Config config)
    {
        ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.INFO, "Initialize EditorPoseTracker");
        
        config.tracker.useTranslationFilter = true;
        config.tracker.useRotationFilter = true;
        config.tracker.useInterpolation = true;

        m_MainCamera = Camera.main;

        Initialize(config);
        InitDatasetManager();
        InitComponents();
    }

    private void InitDatasetManager()
    {
        if(m_ARDatasetManager == null)
        {
            m_ARDatasetManager = GameObject.FindObjectOfType<ARDatasetManager>();
        }

        if(m_ARDatasetManager != null && m_ARDatasetManager.datasetPath == "")
        {
            Debug.LogError("Dataset path is empty!");
        }

        PlayDatasetManager();
    }

    private void InitComponents()
    {
        m_TextureProvider = GameObject.FindObjectOfType<TextureProvider>();
        m_GeoCoordProvider = GameObject.FindObjectOfType<GeoCoordProvider>();
    }

    private void PlayDatasetManager()
    {
        if(m_ARDatasetManager != null)
        {
            m_ARDatasetManager.frameReceived += OnPreviewUpdated;
            m_ARDatasetManager.Play();
        }
    }

    public override void RegisterFrameLoop()
    {
        if(m_ARDatasetManager != null)
        {
            m_ARDatasetManager.frameReceived += OnCameraFrameReceived;
        }
    }

    public override void UnregisterFrameLoop()
    {
        if(m_ARDatasetManager != null)
        {
            m_ARDatasetManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnPreviewUpdated(FrameData frameData)
    {
        if (!m_IsInitialized)
            return;

        if(!m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture)) 
            return;
        
        m_MainCamera.transform.localPosition = frameData.modelMatrix.GetColumn(3);
        m_MainCamera.transform.localRotation = Quaternion.LookRotation(frameData.modelMatrix.GetColumn(2), frameData.modelMatrix.GetColumn(1));
        m_MainCamera.fieldOfView = CalculateFOV(frameData.projMatrix);

        m_ARDatasetManager.SetPreviewTexture(frameTexture);
    }

    private void OnCameraFrameReceived(FrameData frameData)
    {
        if (!m_IsInitialized)
            return;

        if (m_GeoCoordProvider)
        {
            m_GeoCoordProvider.latitude = (float) frameData.latitude;
            m_GeoCoordProvider.longitude = (float) frameData.longitude;
        }

        Matrix4x4 projMatrix = frameData.projMatrix;
        Matrix4x4 transMatrix = frameData.transMatrix;
        UpdateFrame(projMatrix, transMatrix);
    }

    private float CalculateFOV(Matrix4x4 projMatrix)
    {
        float f = projMatrix.m11;
        float verticalFOV = 2.0f * Mathf.Atan(1.0f / f) * Mathf.Rad2Deg;
        return verticalFOV;
    }

    public override bool AcquireRequestedTexture(out Texture texture)
    {
        if(!m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture)) 
        {
            texture = m_TextureProvider.textureToSend;
        }
        else
        {
            // m_ARDatasetManager.SetPreviewTexture(frameTexture);
            texture = frameTexture;
        }
        return true;
    }

    public override float[] MakeDisplayRotationMatrix(Matrix4x4 rawDispRotMatrix)
    {
        Matrix4x4 deviceDisplayMatrix = rawDispRotMatrix;

        for(int i=0 ; i<3 ; i++) 
        {
            for(int j=0 ; j<3 ; j++)
            {
                m_DisplayMatrix[i * 3 + j] = deviceDisplayMatrix[i, j];
            }
        }

        return m_DisplayMatrix;
    }

    public override void AquireCameraIntrinsic(out float fx, out float fy, out float cx, out float cy) {
        // VLSDKDatasetRecorder로 기록한 데이터는 transpose를 한 상태에서 저장한다.
        ARDatasetIntrinsic intrinsic = m_ARDatasetManager.GetIntrinsic();

        fx = intrinsic.fx;
        fy = intrinsic.fy;
        cx = intrinsic.cx;
        cy = intrinsic.cy;
    }
}
}