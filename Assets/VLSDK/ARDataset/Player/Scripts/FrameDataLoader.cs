using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ARCeye.Dataset
{
    public class FrameDataLoader
    {
        private List<FrameData> m_FrameDataList;

        public int DataCount
        {
            get => m_FrameDataList.Count;
        }

        public bool IsCompleted
        {
            get; private set;
        }

        private Texture2D m_DatasetTexture;

        // 안드로이드 환경에서 사용하는 이미지 캐시.
        private Dictionary<string, byte[]> m_FrameImageCache = new();
        private MonoBehaviour m_Runner;

        private string m_DatasetPath;

        public void Load(string datasetPath)
        {
            m_DatasetPath = datasetPath;

            string dataPath = datasetPath + "/" + "data.bin";

#if !UNITY_EDITOR && UNITY_ANDROID
            m_Runner = GameObject.FindObjectOfType<ARDatasetManager>();
            m_Runner.StartCoroutine(LoadAllFrameDataUsingWebRequest(dataPath));
#else
            LoadAllFramdDataUsingFileStream(dataPath);
#endif
        }

        private IEnumerator LoadAllFrameDataUsingWebRequest(string dataPath)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(dataPath))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load dataset: " + request.error);
                    yield break;
                }

                byte[] bytes = request.downloadHandler.data;

                using MemoryStream memoryStream = new MemoryStream(bytes);
                using BinaryReader reader = new BinaryReader(memoryStream);

                m_FrameDataList = new List<FrameData>();

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    string frameStr = reader.ReadString();
                    m_FrameDataList.Add(new FrameData(frameStr));
                }

                Debug.Log("Finish loading dataset!");
            }

            m_Runner.StartCoroutine(StartFrameImageLoader());

            IsCompleted = true;
        }

        // 5개씩 프레임 이미지를 로드. 5개 이하일 경우 다음 프레임 로드.
        private IEnumerator StartFrameImageLoader()
        {
            int maxCacheCount = 5;
            int cacheIndex = 0;

            while (true)
            {
                if (m_FrameImageCache.Count < maxCacheCount && cacheIndex < DataCount)
                {
                    FrameData frameData = m_FrameDataList[cacheIndex++];
                    string frameImagePath = $"{m_DatasetPath}/{frameData.timestamp}.jpg";

                    UnityWebRequest request = UnityWebRequest.Get(frameImagePath);

                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("Failed to load frame image: " + request.error);
                        Debug.LogError("Image Path: " + frameImagePath);
                        continue;
                    }
                    else
                    {
                        byte[] src = request.downloadHandler.data;
                        byte[] dest = new byte[src.Length];
                        Buffer.BlockCopy(src, 0, dest, 0, dest.Length);
                        m_FrameImageCache[frameImagePath] = dest;
                    }

                    if (cacheIndex == DataCount)
                    {
                        Debug.Log("All frame images loaded.");
                        break;
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void LoadAllFramdDataUsingFileStream(string dataPath)
        {
            using FileStream fileStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new BinaryReader(fileStream);

            m_FrameDataList = new List<FrameData>();

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                string frameStr = reader.ReadString();
                m_FrameDataList.Add(new FrameData(frameStr));
            }

            Debug.Log("Finish loading dataset!");

            IsCompleted = true;
        }

        public FrameData GetFrameData(int index)
        {
            if (index < 0 || index >= DataCount)
            {
                Debug.LogError("Index out of range: " + index);
                return null;
            }

            return m_FrameDataList[index];
        }

        public Texture2D GetFrameTexture(string frameImagePath)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (m_FrameImageCache.TryGetValue(frameImagePath, out byte[] imageData))
            {
                if (m_DatasetTexture == null)
                {
                    m_DatasetTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                }

                m_DatasetTexture.LoadImage(imageData);

                m_FrameImageCache.Remove(frameImagePath);
            }

            return m_DatasetTexture;
#else
            if (File.Exists(frameImagePath))
            {
                byte[] fileData = File.ReadAllBytes(frameImagePath);

                if (m_DatasetTexture == null)
                {
                    m_DatasetTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                }
                m_DatasetTexture.LoadImage(fileData);

                return m_DatasetTexture;
            }
            else
            {
                return null;
            }
#endif
        }
    }
}