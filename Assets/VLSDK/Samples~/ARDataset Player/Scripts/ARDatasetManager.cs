using System;
using System.Collections;
using System.IO;

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
        public float[] PlaySpeeds => m_PlaySpeeds;
        public int PlaySpeedIndex => m_PlaySpeedIndex;


        private enum DatasetMode { Video, Legacy }
        private DatasetMode m_Mode;

        // Video 모드.
        private DatasetDecoder m_Decoder;

        private const float REFRESH_TIMEOUT = 1.5f;
        private const int REFRESH_ATTEMPTS_PER_FRAME = 8;
        private const float SEEK_RETRY_INTERVAL = 0.1f;

        private float m_RefreshDeadline = 0.0f;
        private float m_NextSeekRetryTime = 0.0f;
        private double m_PendingSeekSeconds = 0.0;
        private bool m_PendingSeekAccepted = false;

        // Legacy 모드.
        private FrameDataLoader m_FrameDataLoader;
        private int m_CurrIdx = 0;


        // 외부에서 프레임 데이터를 수신하는 이벤트.
        public event Action<FrameData> FrameReceived;
        private FrameData m_CurrFrameData;

        private Camera m_MainCamera;
        private DebugPreview m_DebugPreview;


        private void Awake()
        {
            Progress = 0;
            IsUpdating = true;
            m_MainCamera = Camera.main;

            UpdateDatasetPath();

            m_Mode = ResolveDatasetMode(DatasetPath);

            if (m_Mode == DatasetMode.Legacy && Directory.Exists(DatasetPath) && File.Exists(Path.Combine(DatasetPath, "data.bin")))
            {
                Debug.LogWarning("[ARDatasetManager] The legacy ARDataset format (data.bin) is deprecated. Please use an AR Recorder video dataset instead.");
            }

            if (m_Mode == DatasetMode.Video)
            {
                m_Decoder = CreateDatasetDecoder();
                NativeDecoderLogger.Initialize();
            }
            else
            {
                LoadAllFrameData();
            }
        }

        private void Start()
        {
            m_DebugPreview = FindAnyObjectByType<DebugPreview>();

            if (m_DebugPreview == null)
            {
                Debug.LogError("Cannot find DebugPreivew in scene");
            }

            if (m_Mode == DatasetMode.Video)
            {
                if (m_Decoder == null)
                {
                    return;
                }

                m_Decoder.Initialize(DatasetPath);
                m_Decoder.Play();

                if (m_DebugPreview != null)
                {
                    m_DebugPreview.SetTexture(m_Decoder.GetPreviewTexture());
                }
            }
            else
            {
                StartCoroutine(UpdateFrame());
            }
        }

        // Video 모드 프레임 진행. Legacy 모드는 UpdateFrame 코루틴을 사용.
        private void Update()
        {
            if (m_Mode != DatasetMode.Video || m_Decoder == null)
            {
                return;
            }

            if (m_IsPaused || !m_Decoder.IsRunning())
            {
                RefreshFrameWhilePaused();
                return;
            }

            FrameData frameData = m_Decoder.GetNextFrame();
            if (frameData != null)
            {
                m_CurrFrameData = frameData;
                FrameReceived?.Invoke(frameData);
            }

            if (IsUpdating)
            {
                Progress = m_Decoder.GetProgress();
            }
        }

        private void OnDestroy()
        {
            if (m_Mode == DatasetMode.Video)
            {
                m_Decoder?.Release();
            }
        }

        // 데이터셋 경로로 재생 방식을 결정.
        // data.bin이 있는 폴더면 구버전(Legacy), 동영상 파일이면 Video.
        private DatasetMode ResolveDatasetMode(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return DatasetMode.Legacy;
            }

            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "data.bin")))
            {
                return DatasetMode.Legacy;
            }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".mp4" || ext == ".mov" || ext == ".m4v")
            {
                return DatasetMode.Video;
            }

            // 확장자를 판별하지 못한 경우 구버전 폴더 방식으로 처리.
            return DatasetMode.Legacy;
        }

        // 현재 데이터셋이 동영상(디코더) 모드인지 여부. 에디터에서도 사용.
        public bool IsVideoMode => ResolveDatasetMode(DatasetPath) == DatasetMode.Video;

        private DatasetDecoder CreateDatasetDecoder()
        {
#if UNITY_EDITOR_OSX
            return new MacDatasetDecoder();
#elif UNITY_EDITOR_WIN
            return new WinDatasetDecoder();
#else
            Debug.LogError("[ARDatasetManager] Video dataset playback is currently supported only on the mac / windows editor.");
            return null;
#endif
        }

        private void UpdateDatasetPath()
        {
            if (string.IsNullOrEmpty(DatasetPath))
            {
                return;
            }

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

        public void Play()
        {
            m_IsPaused = false;
            m_RefreshDeadline = 0.0f;

            if (m_Mode == DatasetMode.Video)
            {
                m_Decoder?.Play();
            }
        }

        public void Pause()
        {
            m_IsPaused = true;

            if (m_Mode == DatasetMode.Video)
            {
                m_Decoder?.Pause();
            }
        }

        // 동영상 모드에서 지정한 시간(초)으로 이동. 구버전 모드는 진행바(Progress)로 프레임 인덱스를 조정.
        public void Seek(double seconds)
        {
            if (m_Mode != DatasetMode.Video)
            {
                return;
            }

            bool accepted = m_Decoder != null && m_Decoder.Seek(seconds);

            if (m_IsPaused)
            {
                m_PendingSeekSeconds = seconds;
                m_PendingSeekAccepted = accepted;
                m_RefreshDeadline = Time.realtimeSinceStartup + REFRESH_TIMEOUT;
                m_NextSeekRetryTime = Time.realtimeSinceStartup + SEEK_RETRY_INTERVAL;
            }
        }

        // 일시정지 중 seek 후, 마감 시각까지 프리뷰를 목표 지점으로 따라잡는다.
        private void RefreshFrameWhilePaused()
        {
            if (m_RefreshDeadline <= 0.0f)
            {
                return;
            }

            if (Time.realtimeSinceStartup > m_RefreshDeadline)
            {
                m_RefreshDeadline = 0.0f;
                Debug.LogWarning($"[ARDatasetManager] Failed to refresh the paused preview for the seek position {m_PendingSeekSeconds:F3}s.");
                return;
            }

            // 디코더가 seek을 거부(연속 seek 스로틀)했으면 갱신이 진행되지 않으므로 간격을 두고 다시 요청.
            if (!m_PendingSeekAccepted)
            {
                if (Time.realtimeSinceStartup < m_NextSeekRetryTime)
                {
                    return;
                }

                m_PendingSeekAccepted = m_Decoder.Seek(m_PendingSeekSeconds);
                if (!m_PendingSeekAccepted)
                {
                    m_NextSeekRetryTime = Time.realtimeSinceStartup + SEEK_RETRY_INTERVAL;
                    return;
                }
            }

            for (int i = 0; i < REFRESH_ATTEMPTS_PER_FRAME && m_RefreshDeadline > 0.0f; i++)
            {
                TryRefreshFrame();
            }
        }

        // 현재(일시정지) 위치의 프레임을 한 번 디코딩 시도. 성공하면 수신자에게 전달하고 갱신을 종료.
        private void TryRefreshFrame()
        {
            if (m_Decoder == null)
            {
                return;
            }

            FrameData frameData = m_Decoder.GetNextFrame();
            if (frameData != null)
            {
                m_CurrFrameData = frameData;
                FrameReceived?.Invoke(frameData);

                // 일시정지 중에도 타임라인(Progress)이 seek 위치를 반영하도록 갱신.
                // 단, 에디터에서 슬라이더를 드래그하는 동안(IsUpdating=false)에는
                // 핸들 값을 뺏지 않도록 갱신을 건너뛴다.
                if (IsUpdating)
                {
                    Progress = m_Decoder.GetProgress();
                }
                m_RefreshDeadline = 0.0f;
            }
        }

        public void TogglePlaySpeed()
        {
            SetPlaySpeed((m_PlaySpeedIndex + 1) % m_PlaySpeeds.Length);
        }

        // 지정한 인덱스의 재생 속도로 설정.
        public void SetPlaySpeed(int index)
        {
            if (index < 0 || index >= m_PlaySpeeds.Length)
            {
                return;
            }

            m_PlaySpeedIndex = index;

            if (m_Mode == DatasetMode.Video)
            {
                m_Decoder?.SetSpeed(m_PlaySpeeds[m_PlaySpeedIndex]);
            }
        }

        public bool TryAcquireFrameImage(out Texture texture)
        {
            if (m_Mode == DatasetMode.Video)
            {
                texture = m_Decoder != null ? m_Decoder.GetPreviewTexture() : null;
                return texture != null;
            }

            // Legacy 모드.
            string frameImagePath = $"{DatasetPath}/{m_CurrFrameData.timestamp}.jpg";
            texture = m_FrameDataLoader.GetFrameTexture(frameImagePath);
            return texture != null;
        }

        public ARDatasetIntrinsic GetIntrinsic()
        {
            return m_CurrFrameData.intrinsic;
        }

        public void SetPreviewTexture(Texture previewTexture)
        {
            if (m_DebugPreview != null)
            {
                m_DebugPreview.SetTexture(previewTexture);
            }
        }

        public float GetTotalSeconds()
        {
            if (m_Mode == DatasetMode.Video)
            {
                return m_Decoder != null ? (float)m_Decoder.GetTotalDuration() : 0.0f;
            }

            // Legacy 모드.
            if (!m_FrameDataLoader.IsCompleted)
            {
                Debug.LogError("Dataset is not loaded yet.");
                return 0;
            }

            FrameData firstFrame = m_FrameDataLoader.GetFrameData(0);
            FrameData lastFrame = m_FrameDataLoader.GetFrameData(m_FrameDataLoader.DataCount - 1);

            float totalSeconds = (lastFrame.timestamp - firstFrame.timestamp) * 0.001f;
            return totalSeconds;
        }

        private void OnDrawGizmos()
        {
            if (m_MainCamera != null)
            {
                Matrix4x4 poseMatrix = m_MainCamera.transform.localToWorldMatrix;
                CameraDrawer.DrawFrame(poseMatrix, Color.magenta, 1.5f);
            }
        }


        // ---------------------------------------------------------------------
        // Legacy(구버전 정지영상 데이터셋) 재생 경로.
        // 구버전 ARDataset 포맷(data.bin + {timestamp}.jpg) 전용. deprecated, 당분간 함께 동작.
        // ---------------------------------------------------------------------

        private void LoadAllFrameData()
        {
            Debug.Log("Start loading dataset...");

            m_FrameDataLoader = new FrameDataLoader();
            m_FrameDataLoader.Load(DatasetPath);
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
            return Mathf.Clamp(index, 0, m_FrameDataLoader.DataCount - 1);
        }

        private void UpdateProgress()
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
    }
}
