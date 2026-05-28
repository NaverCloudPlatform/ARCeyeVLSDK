using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace ARCeye.Dataset.Recorder
{
    struct Intrinsic
    {
        public Vector2 focalLength;
        public Vector2 principalPoint;
    }


    public class DecodedDatasetManager : MonoBehaviour
    {
        public string datasetUrl { get; set; }
        public int framerate { get; set; } = 30;
        public event Action<FrameData> frameReceived;

        private Texture2D m_DatasetTexture;
        private string m_DataTxtFile;
        private List<string> m_FrameNameList;

        private const int MAJOR_AXIS_LENGTH = 640;  // 장축의 길이를 640으로 고정.


        public void Play()
        {
            StartCoroutine(StartStreaming());
        }

        public void Stop()
        {
            StopStreaming();
        }

        /// <summary>
        ///   데이터셋 스트리밍 시작.
        /// </summary>
        private IEnumerator StartStreaming()
        {
            Debug.Log("데이터셋 변환 시작");
            Debug.Log("  Dataset URL : " + datasetUrl);
            Debug.Log("  Target framerate : " + framerate);
            Debug.Log("  Major axis length : " + MAJOR_AXIS_LENGTH);

            CreateDecodedFrameList();

            yield return LoadFrameData();
        }

        /// <summary>
        ///   데이터셋 스트리밍 종료.
        /// </summary>
        private void StopStreaming()
        {
            StopAllCoroutines();
        }


        private void CreateDecodedFrameList()
        {
            /// data.txt는 다음과 같은 구조로 되어 있음.
            /// 767
            /// 1719192519232
            /// 1719192519574
            /// 1719192519907
            /// 1719192520241
            /// ...
            /// 

            string rgbDirPath = $"{datasetUrl}/rgb";
            string absolutePath = rgbDirPath.Replace("file://", "");
            string[] files = Directory.GetFiles(absolutePath);
            double prevTimestamp = 0;

            m_FrameNameList = new List<string>();

            foreach (string filePath in files)
            {
                string filename = Path.GetFileNameWithoutExtension(filePath);
                double currTimestamp = double.Parse(filename);

                if ((currTimestamp - prevTimestamp) * 0.001f > 1.0 / (float)framerate)
                {
                    m_FrameNameList.Add(filename);
                    prevTimestamp = currTimestamp;
                }
            }
        }

        private IEnumerator LoadFrameData()
        {
            int idx = 0;

            while (idx < m_FrameNameList.Count)
            {
                string filename = m_FrameNameList[idx].Trim();

                string rgbPath = $"{datasetUrl}/rgb/{filename}.jpg";
                string posePath = $"{datasetUrl}/trajectory/{filename}.txt";
                string cameraParamPath = $"{datasetUrl}/cameraParam/{filename}.txt";
                string relAltitudePath = $"{datasetUrl}/relAltitude/{filename}.txt";

                Texture2D rgbTexture = new Texture2D(2, 2);
                string poseStr = "";
                string cameraParamStr = "";
                string relAltitudeStr = "";
                float resizedScale = 1;

                // 모든 frame 데이터 다운로드.
                yield return LoadTxtFile(posePath, (result) =>
                {
                    poseStr = result;
                });

                yield return LoadTxtFile(cameraParamPath, (result) =>
                {
                    cameraParamStr = result;
                });

                yield return LoadTxtFile(relAltitudePath, (result) =>
                {
                    relAltitudeStr = result;
                });

                yield return LoadJpgFile(rgbPath, (result, scale) =>
                {
                    rgbTexture = result;
                    // 서버에서 이미지를 로딩한 다음에 scale 값을 알 수 있음.
                    resizedScale = scale;
                });


                float scale = (float)MAJOR_AXIS_LENGTH / (float)rgbTexture.height;

                Intrinsic intrinsic = ParseIntrinsic(cameraParamStr);
                FrameData frameData = new FrameData();

                frameData.texture = rgbTexture;
                frameData.timestamp = long.Parse(filename);
                frameData.modelMatrix = ParseModelMatrix(poseStr);
                frameData.projMatrix = ParseProjMatrix(poseStr);
                frameData.transMatrix = Matrix4x4.identity;
                frameData.relAltitude = ParseRelAltitude(relAltitudeStr);

                frameData.width = frameData.texture.width;
                frameData.height = frameData.texture.height;

                frameData.fx = intrinsic.focalLength.x * resizedScale;
                frameData.fy = intrinsic.focalLength.y * resizedScale;
                frameData.cx = intrinsic.principalPoint.x * resizedScale;
                frameData.cy = intrinsic.principalPoint.y * resizedScale;

                frameData.latitude = 0;
                frameData.longitude = 0;

                frameReceived?.Invoke(frameData);

                float progress = ((float)(idx + 1) / (float)m_FrameNameList.Count) * 100.0f;

                Debug.Log($"Converting... {progress.ToString("N2")}% ({idx + 1}/{m_FrameNameList.Count})");

                yield return null;

                idx++;
            }

            // 모든 파일들의 로딩이 끝나면 스트리밍 종료.
            StopStreaming();

            Debug.Log($"Converting Finish!");
        }

        ///   1276.263428, 1276.263428, 536.622620, 959.895813
        private Intrinsic ParseIntrinsic(string intrinsicStr)
        {
            Intrinsic intrinsic = new Intrinsic();

            string[] elems = intrinsicStr.Split(',');
            intrinsic.focalLength = new Vector2(float.Parse(elems[0]), float.Parse(elems[1]));
            intrinsic.principalPoint = new Vector2(float.Parse(elems[2]), float.Parse(elems[3]));

            return intrinsic;
        }

        ///   # view matrix
        ///   -0.99996078014373779297, -0.00024511085939593613, 0.00885896850377321243, 0.00000000000000000000, 0.00115075730718672276, 0.98756355047225952148, 0.15721657872200012207, 0.00000000000000000000, -0.00878728460520505905, 0.15722060203552246094, -0.98752450942993164062, 0.00000000000000000000, -1.96944713592529296875, 0.59379428625106811523, -3.93875384330749511719, 1.00000000000000000000, 
        ///   # proj matrix
        ///   2.88214707374572753906, 0.00000000000000000000, 0.00000000000000000000, 0.00000000000000000000, 0.00000000000000000000, 1.32944107055664062500, 0.00000000000000000000, 0.00000000000000000000, -0.00649785995483398438, 0.00041234493255615234, -1.00010001659393310547, -1.00000000000000000000, 0.00000000000000000000, 0.00000000000000000000, -0.01000100001692771912, 0.00000000000000000000, 
        private Matrix4x4 ParseModelMatrix(string trajectoryStr)
        {
            string[] elems = trajectoryStr.Split('\n');
            string modelMatrixStr = elems[1];
            string[] poseElems = modelMatrixStr.Split(',');

            float[] values = new float[16];
            for (int i = 0; i < 16; i++)
            {
                values[i] = float.Parse(poseElems[i].Trim());
            }

            Matrix4x4 matrix = new Matrix4x4();
            matrix.m00 = values[0];
            matrix.m01 = values[1];
            matrix.m02 = values[2];
            matrix.m03 = values[3];
            matrix.m10 = values[4];
            matrix.m11 = values[5];
            matrix.m12 = values[6];
            matrix.m13 = values[7];
            matrix.m20 = values[8];
            matrix.m21 = values[9];
            matrix.m22 = values[10];
            matrix.m23 = values[11];
            matrix.m30 = values[12];
            matrix.m31 = values[13];
            matrix.m32 = values[14];
            matrix.m33 = values[15];

            matrix = PoseUtility.ConvertOpenGLToUnityMatrix(matrix.transpose.inverse);

            return matrix;
        }

        private Matrix4x4 ParseProjMatrix(string trajectoryStr)
        {
            string[] elems = trajectoryStr.Split('\n');
            string modelMatrixStr = elems[3];
            string[] poseElems = modelMatrixStr.Split(',');

            float[] values = new float[16];
            for (int i = 0; i < 16; i++)
            {
                values[i] = float.Parse(poseElems[i].Trim());
            }

            Matrix4x4 matrix = new Matrix4x4();
            matrix.m00 = values[0];
            matrix.m01 = values[1];
            matrix.m02 = values[2];
            matrix.m03 = values[3];
            matrix.m10 = values[4];
            matrix.m11 = values[5];
            matrix.m12 = values[6];
            matrix.m13 = values[7];
            matrix.m20 = values[8];
            matrix.m21 = values[9];
            matrix.m22 = values[10];
            matrix.m23 = values[11];
            matrix.m30 = values[12];
            matrix.m31 = values[13];
            matrix.m32 = values[14];
            matrix.m33 = values[15];

            return matrix.transpose;
        }

        private double ParseRelAltitude(string relAltitudeStr)
        {
            if (double.TryParse(relAltitudeStr, out double result))
            {
                return result;
            }
            else
            {
                Debug.LogWarning("Failed to parse relAltitude value");
                return 0;
            }
        }

        private IEnumerator LoadJpgFile(string url, UnityAction<Texture2D, float> successCallback)
        {
            // URL로부터 프레임들의 이름이 저장된 data.txt 파일 다운로드.
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("LoadJpgFile Failed " + url);
                Debug.LogError(www.error);
            }
            else
            {
                byte[] rgbBuffer = www.downloadHandler.data;

                if (m_DatasetTexture == null)
                {
                    m_DatasetTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                }
                m_DatasetTexture.LoadImage(rgbBuffer);

                float scale = (float)MAJOR_AXIS_LENGTH / (float)m_DatasetTexture.height;

                float resizedWidth = m_DatasetTexture.width * scale;
                float resizedHeight = m_DatasetTexture.height * scale;
                Texture2D resized = ImageUtility.Resize(m_DatasetTexture, (int)resizedWidth, (int)resizedHeight);

                successCallback.Invoke(resized, scale);
            }
        }

        private IEnumerator LoadTxtFile(string url, UnityAction<string> successCallback)
        {
            // URL로부터 프레임들의 이름이 저장된 data.txt 파일 다운로드.
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("LoadTxtFile Failed. " + url);
                Debug.LogError(www.error);
            }
            else
            {
                successCallback.Invoke(www.downloadHandler.text);
            }
        }
    }
}