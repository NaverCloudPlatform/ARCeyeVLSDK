using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye.Dataset
{
    public class ARDatasetManager : MonoBehaviour
    {
        [SerializeField]
        private string m_DatasetPath;
        public string datasetPath {
            get => m_DatasetPath;
            set => m_DatasetPath = value;
        }

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float m_Progress;
        public float progress {
            get => m_Progress;
            set => m_Progress = value;
        }

        public bool testMode { get; set; } = false;
        public double testLatitude { get; set; }
        public double testLongitude { get; set; }
        public Matrix4x4 testCameraOffset { get; set; } = Matrix4x4.identity;

        public bool isUpdating { get; set; }

        private float[] m_PlaySpeeds = new[] { 1.0f, 2.0f, 5.0f, 10.0f };
        private int m_PlaySpeedIndex = 0;
        public float playSpeed { 
            get {
                return m_PlaySpeeds[m_PlaySpeedIndex];
            }
         }

        public event Action<FrameData> frameReceived;
        private List<FrameData> m_FrameDataList;
        private FrameData m_CurrFrameData;
        private DebugPreview m_DebugPreview;

        private int m_CurrFrameIdx = 0;
        private int m_TotalFrameCount = 0;
        private bool m_IsPlaying = false;

        private Camera m_MainCamera;
        private Texture2D m_DatasetTexture;


        private void Awake()
        {
            isUpdating = true;
            m_Progress = 0;
            m_MainCamera = Camera.main;
        }

        private void Start()
        {
            m_DebugPreview = FindObjectOfType<DebugPreview>();
            if(m_DebugPreview == null)
            {
                Debug.LogError("Cannot find DebugPreivew in scene");
            }

            StartCoroutine( UpdateFrame() );
        }

        private IEnumerator UpdateFrame()
        {
            while(true)
            {
                if(!m_IsPlaying)
                {
                    yield return null;
                }
                else
                {
                    FrameData currFrameData = ReadCurrFrame();
                    FrameData nextFrameData = ReadNextFrame();

                    if(testMode)
                    {
                        Matrix4x4 modelMatrix = currFrameData.modelMatrix;
                        currFrameData.modelMatrix = modelMatrix * testCameraOffset;
                    }

                    m_CurrFrameData = currFrameData;

                    float interval = (nextFrameData.timestamp - currFrameData.timestamp) * 0.001f;

                    interval /= m_PlaySpeeds[m_PlaySpeedIndex];

                    yield return new WaitForSeconds(interval);

                    frameReceived?.Invoke(currFrameData);

                    UpdateProgress();
                }
            }
        }

        public void UpdateProgress()
        {
            if(isUpdating)
            {
                m_Progress = (float) m_CurrFrameIdx / (float) m_TotalFrameCount;
            }
            else
            {
                m_CurrFrameIdx = Mathf.FloorToInt(((float) m_TotalFrameCount) * m_Progress);
            }
        }

        private FrameData ReadCurrFrame()
        {
            if(m_CurrFrameIdx >= m_FrameDataList.Count)
            {
                m_CurrFrameIdx = m_FrameDataList.Count - 1;
            }

            return m_FrameDataList[m_CurrFrameIdx];
        }

        private FrameData ReadNextFrame()
        {
            m_CurrFrameIdx = GetNextFrameIndex();
            return ReadCurrFrame();
        }

        private int GetNextFrameIndex()
        {
            int currFrameIndex = m_CurrFrameIdx;

            if(currFrameIndex + 1 < m_FrameDataList.Count)
            {
                currFrameIndex++;
            }

            return currFrameIndex;
        }

        public void Play()
        {
            // 유닛 테스트용 FrameData 로드.
            if(testMode)
            {
                LoadTestFrameData();
            }
            // 데이터셋의 FrameData 로드.
            else
            {
                LoadAllFrameData();
            }

            m_IsPlaying = true;
        }

        public void Pause()
        {
            m_IsPlaying = false;
        }

        public bool TryAcquireFrameImage(out Texture texture)
        {
            string datasetPath = m_DatasetPath;
            string frameImagePath = $"{datasetPath}/{m_CurrFrameData.timestamp}.jpg";
            if (File.Exists(frameImagePath))
            {
                byte[] fileData = File.ReadAllBytes(frameImagePath);
                
                if(m_DatasetTexture == null)
                {
                    m_DatasetTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                }
                m_DatasetTexture.LoadImage(fileData);

                texture = m_DatasetTexture;
                return true;
            }
            else
            {
                texture = null;
                // test mode가 아닐 경우에만 로그 출력.
                if(!testMode)
                {
                    Debug.LogError("File not found at path: " + datasetPath);
                }
                return false;
            }
        }

        public ARDatasetIntrinsic GetIntrinsic()
        {
            return m_CurrFrameData.intrinsic;
        }

        public void SetPreviewTexture(Texture previewTexture)
        {
            m_DebugPreview.SetTexture(previewTexture);
        }

        private void LoadTestFrameData()
        {
            const int MaxFrameCount = 100;


            m_FrameDataList = new List<FrameData>();

            for(int i=0 ; i<MaxFrameCount ; i++)
            {
                // 0.1초 단위로 timestamp 값을 증가.
                var frameData = new FrameData();
                frameData.timestamp = 1717988760000 + 100 * i;
                frameData.latitude = testLatitude;
                frameData.longitude = testLongitude;

                m_FrameDataList.Add(frameData);
            }

            m_TotalFrameCount = m_FrameDataList.Count;
        }

        private void LoadAllFrameData()
        {
            Debug.Log("Start loading dataset...");

            string datasetPath = m_DatasetPath;
            string dataPath = datasetPath + "/" + "data.bin";

            if(!File.Exists(dataPath))
            {
                Debug.LogError("Failed to find dataset at path : " + datasetPath);
                return;
            }

            using FileStream fileStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new BinaryReader(fileStream);

            m_FrameDataList = new List<FrameData>();

            while(reader.BaseStream.Position != reader.BaseStream.Length)
            {
                string frameStr = reader.ReadString();
                m_FrameDataList.Add(new FrameData(frameStr));
            }

            m_TotalFrameCount = m_FrameDataList.Count;

            Debug.Log("Finish loading dataset!");
        }

        public float GetTotalSeconds()
        {
            if(m_FrameDataList == null)
            {
                Debug.LogError("아직 데이터셋이 로드되지 않았음");
                return 0;
            }

            FrameData firstFrame = m_FrameDataList[0];
            FrameData lastFrame = m_FrameDataList[m_FrameDataList.Count - 1];

            float totalSeconds = (lastFrame.timestamp - firstFrame.timestamp) * 0.001f;
            return totalSeconds;
        }

        public void TogglePlaySpeed()
        {
            m_PlaySpeedIndex = ++m_PlaySpeedIndex % m_PlaySpeeds.Length;
        }


        private void OnDrawGizmos()
        {
            if(m_MainCamera != null)
            {
                Matrix4x4 poseMatrix = m_MainCamera.transform.localToWorldMatrix;
                CameraDrawer.DrawFrame(poseMatrix, Color.magenta, 1.5f);
            }
        }
    }
}