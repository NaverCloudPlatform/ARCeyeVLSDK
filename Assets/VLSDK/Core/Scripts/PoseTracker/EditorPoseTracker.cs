using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ARCeye.Dataset;
using System;

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

    private bool m_IsLoopRunning;
    private Coroutine m_LoopCoroutine;
    private MonoBehaviour m_CoroutineRunner;
    private DebugPreview m_DebugPreview;


    public override void Initialize(Transform coroutineRunner, Config config)
    {
        NativeLogger.DebugLog(ARCeye.LogLevel.INFO, "Initialize EditorPoseTracker");

        m_MainCamera = Camera.main;
        m_CoroutineRunner = coroutineRunner.GetComponent<MonoBehaviour>();

        InitDatasetManager(ref config);
        InitComponents();
        Initialize(config);

        PlayDatasetManager();
    }

    private void InitDatasetManager(ref Config config)
    {
        if(m_ARDatasetManager == null)
        {
            m_ARDatasetManager = GameObject.FindObjectOfType<ARDatasetManager>();
        }

        if(m_ARDatasetManager != null)
        {
            if(m_ARDatasetManager.datasetPath == "")
            {
                Debug.LogError("Dataset path is empty!");
            }
        }
        else
        {
            Debug.LogWarning("Failed to find ARDatasetManager. Using ARDatasetManager is highly recommended in editor mode");

            // ARDatasetManager가 없을 경우에는 TextureProvider 기반 요청.
            // TextureProvider만 사용하는 경우에는 필터를 모두 비활성화 한다.
            config.tracker.useTranslationFilter = false;
            config.tracker.useRotationFilter = false;
            config.tracker.useInterpolation = false;
        }
    }

    private void InitComponents()
    {
        m_TextureProvider = GameObject.FindObjectOfType<TextureProvider>();
        m_GeoCoordProvider = GameObject.FindObjectOfType<GeoCoordProvider>();
        m_DebugPreview = GameObject.FindObjectOfType<DebugPreview>();
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
        else
        {
            m_IsLoopRunning = true;
            m_LoopCoroutine = m_CoroutineRunner.StartCoroutine( CaptureFrame() );
        }
    }

    public override void UnregisterFrameLoop()
    {
        if(m_ARDatasetManager != null)
        {
            m_ARDatasetManager.frameReceived -= OnCameraFrameReceived;
        }
        else if(m_CoroutineRunner != null && m_LoopCoroutine != null)
        {
            m_IsLoopRunning = false;
            m_CoroutineRunner.StopCoroutine(m_LoopCoroutine);
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
        m_CurrRelAltitude = frameData.relAltitude;

        UpdateFrame(projMatrix, transMatrix);
    }

    /// <summary>
    ///   ARDatasetManager를 사용하지 않을 경우 실행되는 루프.
    /// </summary>
    private IEnumerator CaptureFrame()
    {
        while(m_IsLoopRunning)
        {
            yield return new WaitForEndOfFrame();

            if(!m_IsInitialized)
                continue;

            UpdateFrame(m_MainCamera.projectionMatrix, Matrix4x4.identity);
        }
    }

    private float CalculateFOV(Matrix4x4 projMatrix)
    {
        float f = projMatrix.m11;
        float verticalFOV = 2.0f * Mathf.Atan(1.0f / f) * Mathf.Rad2Deg;
        return verticalFOV;
    }

    public override bool TryAcquireLatestImage(out UnityYuvCpuImage? image, out Action disposable) {
        image = null;
        disposable = null;
        return false;
    }

    public override bool AcquireRequestedTexture(out Texture texture)
    {
        if(m_ARDatasetManager != null && m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture))
        {
            texture = frameTexture;
        }
        else
        {
            texture = m_TextureProvider.textureToSend;
            m_DebugPreview.SetTexture(texture);
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
        if(m_ARDatasetManager != null)
        {
            ARDatasetIntrinsic intrinsic = m_ARDatasetManager.GetIntrinsic();

            fx = intrinsic.fx;
            fy = intrinsic.fy;
            cx = intrinsic.cx;
            cy = intrinsic.cy;
        }
        else
        {
            float scale;

            // Editor 모드는 데이터셋 이미지에 맞춰서 기본 파라매터들의 스케일을 변경한다.
            // width가 장축인 경우.
            if(m_Config.tracker.previewWidth > m_Config.tracker.previewHeight)
            {
                scale = (float) m_Config.tracker.previewWidth / (float) MAJOR_AXIS_LENGTH;
            }
            // height가 장축인 경우.
            else
            {
                scale = (float) m_Config.tracker.previewHeight / (float) MAJOR_AXIS_LENGTH;
            }
            
            fx = DEFAULT_FX * scale;
            fy = DEFAULT_FY * scale;
            cx = DEFAULT_CX * scale;
            cy = DEFAULT_CY * scale;
        }
    }
}
}