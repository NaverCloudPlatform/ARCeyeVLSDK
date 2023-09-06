using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
public class EditorPoseTracker : PoseTracker
{
    private TextureProvider m_TextureProvider;
    public TextureProvider textureProvider {
        get => m_TextureProvider;
        set => m_TextureProvider = value;
    }

    private float[] m_DisplayMatrix = new float[9];

    private Coroutine m_LoopCoroutine;
    private YieldInstruction m_YieldEndOfFrame = new WaitForEndOfFrame();

    const float MAJOR_AXIS_LENGTH = 640.0f;  // 장축의 길이를 640으로 고정.

    protected const float DEFAULT_FX = 474.457672f;
    protected const float DEFAULT_FY = 474.457672f;
    protected const float DEFAULT_CX = 180.000000f;
    protected const float DEFAULT_CY = 321.5426635f;

    private Camera m_MainCamera;
    private MonoBehaviour m_CoroutineRunner;
    private bool m_FinishLoop = true;


    public override void Initialize(Transform coroutineRunner, Config config)
    {
        ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.INFO, "Initialize EditorPoseTracker");
        m_CoroutineRunner = coroutineRunner.GetComponent<MonoBehaviour>();
        
        config.tracker.useTranslationFilter = false;
        config.tracker.useRotationFilter = false;
        config.tracker.useInterpolation = false;

        m_MainCamera = Camera.main;

        Initialize(config);
    }

    public override void RegisterFrameLoop()
    {
        if(m_CoroutineRunner != null) {
            m_FinishLoop = false;
            
            m_LoopCoroutine = m_CoroutineRunner.StartCoroutine( CaptureFrame() );
        }
    }

    public override void UnregisterFrameLoop()
    {
        if(m_CoroutineRunner != null && m_LoopCoroutine != null) {
            m_FinishLoop = true;
            m_CoroutineRunner.StopCoroutine(m_LoopCoroutine);
        }
    }

    private IEnumerator CaptureFrame()
    {
        while(!m_FinishLoop)
        {
            yield return m_YieldEndOfFrame;

            if(m_IsInitialized) {
                if(m_MainCamera != null)
                {
                    UpdateFrame(m_MainCamera.projectionMatrix, Matrix4x4.identity); 
                }
                else
                {
                    UpdateFrame(Matrix4x4.identity, Matrix4x4.identity); 
                }
            }
        }
    }

    public override bool AcquireRequestedTexture(out Texture texture)
    {
        if(m_TextureProvider.textureToSend == null) {
            texture = null;
            Debug.LogError("Preview 텍스처가 할당되지 않았습니다. VLSDKManager gameObject의 TextureProvider 컴포넌트에 요청을 할 텍스처를 정상적으로 할당했는지 확인하세요");
            return false;
        } else {
            texture = m_TextureProvider.textureToSend;

            m_Config.tracker.previewWidth = texture.width;
            m_Config.tracker.previewHeight = texture.height;

            return true;
        }
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