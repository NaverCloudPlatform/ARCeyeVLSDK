using System;
using System.Collections;
using UnityEngine;

namespace ARCeye.Dataset
{
    public class ARDatasetManager : MonoBehaviour
    {
        [SerializeField]
        private string m_DatasetPath;
        public string DatasetPath
        {
            get => m_DatasetPath;
            set => m_DatasetPath = value;
        }

        public float Progress { get; set; }
        public bool IsUpdating { get; set; }
        private bool m_IsPaused = false;

        private int m_PlaySpeedIndex = 0;
        private float[] m_PlaySpeeds = new[] { 1.0f, 2.0f, 5.0f, 10.0f };
        public float PlaySpeed => m_PlaySpeeds[m_PlaySpeedIndex];


        private FrameDataLoader m_FrameDataLoader;


        // 외부에서 프레임 데이터를 수신하는 이벤트.
        public event Action<FrameData> FrameReceived;
        private FrameData m_CurrFrameData;
        private int m_CurrIdx = 0;

        private Camera m_MainCamera;
        private DebugPreview m_DebugPreview;


        private void Awake()
        {
            Progress = 0;
            IsUpdating = true;
            m_MainCamera = Camera.main;

            UpdateDatasetPath();
            LoadAllFrameData();
        }

        private void Start()
        {
            m_DebugPreview = FindAnyObjectByType<DebugPreview>();

            if (m_DebugPreview == null)
            {
                Debug.LogError("Cannot find DebugPreivew in scene");
            }

            StartCoroutine(UpdateFrame());
        }

        private void UpdateDatasetPath()
        {
            // StreamingAssets에서 읽어오는 데이터일 경우 각 디바이스에 맞는 StreamingAssets 경로로 변경.
            if (DatasetPath.Contains("StreamingAssets"))
            {
                // StreamingAssets 하위의 경로 추출.
                string key = "StreamingAssets";
                int pivot = DatasetPath.IndexOf(key, StringComparison.Ordinal);
                string subPath = DatasetPath.Substring(pivot + key.Length);

                DatasetPath = Application.streamingAssetsPath + subPath;
            }
        }

        private IEnumerator UpdateFrame()
        {
            while (true)
            {
                if (m_IsPaused || m_FrameDataLoader == null || !m_FrameDataLoader.IsCompleted)
                {
                    yield return null;
                }
                else
                {
                    FrameData currFrameData = ReadCurrFrame();
                    FrameData nextFrameData = ReadNextFrame();

                    m_CurrFrameData = currFrameData;

                    // 두 프레임이 실행된 timestamp를 비교하여 실제 fps 시뮬레이션.
                    float interval = (nextFrameData.timestamp - currFrameData.timestamp) * 0.001f;

                    interval /= m_PlaySpeeds[m_PlaySpeedIndex];

                    yield return new WaitForSeconds(interval);

                    // 외부로 프레임 데이터 전달.
                    FrameReceived?.Invoke(currFrameData);

                    // 다음 프레임 인덱스 계산.
                    UpdateProgress();
                }
            }
        }

        private FrameData ReadCurrFrame()
        {
            m_CurrIdx = GetSafeFrameIndex(m_CurrIdx);
            return m_FrameDataLoader.GetFrameData(m_CurrIdx);
        }

        private FrameData ReadNextFrame()
        {
            m_CurrIdx = GetSafeFrameIndex(m_CurrIdx + 1);
            return m_FrameDataLoader.GetFrameData(m_CurrIdx);
        }

        private int GetSafeFrameIndex(int index)
        {
            return Mathf.Clamp(index, 0, m_FrameDataLoader.DataCount - 1); ;
        }

        public void UpdateProgress()
        {
            int dataCount = m_FrameDataLoader.DataCount;

            if (IsUpdating)
            {
                Progress = (float)m_CurrIdx / (float)dataCount;
            }
            else
            {
                m_CurrIdx = Mathf.FloorToInt(((float)dataCount) * Progress);
                Progress = (float)m_CurrIdx / (float)dataCount;
            }
        }

        public void Play()
        {
            m_IsPaused = false;
        }

        public void Pause()
        {
            m_IsPaused = true;
        }

        private void LoadAllFrameData()
        {
            Debug.Log("Start loading dataset...");

            m_FrameDataLoader = new FrameDataLoader();
            m_FrameDataLoader.Load(DatasetPath);
        }

        public bool TryAcquireFrameImage(out Texture texture)
        {
            string datasetPath = DatasetPath;
            string frameImagePath = $"{datasetPath}/{m_CurrFrameData.timestamp}.jpg";

            texture = m_FrameDataLoader.GetFrameTexture(frameImagePath);

            return texture != null;
        }

        public ARDatasetIntrinsic GetIntrinsic()
        {
            return m_CurrFrameData.intrinsic;
        }

        public void SetPreviewTexture(Texture previewTexture)
        {
            m_DebugPreview.SetTexture(previewTexture);
        }

        public float GetTotalSeconds()
        {
            if (!m_FrameDataLoader.IsCompleted)
            {
                Debug.LogError("아직 데이터셋이 로드되지 않았음");
                return 0;
            }

            FrameData firstFrame = m_FrameDataLoader.GetFrameData(0);
            FrameData lastFrame = m_FrameDataLoader.GetFrameData(m_FrameDataLoader.DataCount - 1);

            float totalSeconds = (lastFrame.timestamp - firstFrame.timestamp) * 0.001f;
            return totalSeconds;
        }

        public void TogglePlaySpeed()
        {
            m_PlaySpeedIndex = ++m_PlaySpeedIndex % m_PlaySpeeds.Length;
        }

        private void OnDrawGizmos()
        {
            if (m_MainCamera != null)
            {
                Matrix4x4 poseMatrix = m_MainCamera.transform.localToWorldMatrix;
                CameraDrawer.DrawFrame(poseMatrix, Color.magenta, 1.5f);
            }
        }
    }
}