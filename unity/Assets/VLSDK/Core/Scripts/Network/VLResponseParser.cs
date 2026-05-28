using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ARCeye
{
    public abstract class VLResponseParser
    {
        private JObject m_RootObject;
        private bool m_IsVLPassed;
        private long m_Timestamp;
        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private float m_Confidence;
        private string m_DatasetInfo;

        /// <summary>
        ///   VL 요청을 보낸 시간.
        /// </summary>
        public long Timestamp => m_Timestamp;

        /// <summary>
        ///   VL 요청 응답 결과.
        /// </summary>
        public bool IsVLPassed => m_IsVLPassed;

        /// <summary>
        ///   수신한 VL 위치값.
        /// </summary>
        // public abstract string VLPose();
        public Vector3 VLPosition => m_Position;

        public Quaternion VLRotation => m_Rotation;

        /// <summary>
        ///   응답 받은 VL 위치값의 정확도. inlier / total로 계산.
        /// </summary>
        public float Confidence => m_Confidence;

        /// <summary>
        ///   VL이 인식된 공간의 로케이션 정보.
        /// </summary>
        public string DatasetInfo => m_DatasetInfo;

        /// <summary>
        ///   VL 요청 응답 결과를 그대로 리턴.
        /// </summary>
        private string m_ResponseBody;
        public string ResponseBody => m_ResponseBody;

        // 최대 Inlier 개수. 최대 Inlier 개수 이상은 Confidence를 1로 간주.
        private float kMaxInliers = 1000;


        protected abstract bool GetVLResult(JObject jobject);
        protected abstract string PoseKey();
        protected abstract string InlierKey();
        protected abstract string TotalKey();
        protected abstract string DatasetInfoKey();
        protected abstract string ConfidenceKey();


        public static bool IsARCeyeResponse(string message)
        {
            JObject jsonObject = JObject.Parse(message);
            var arceyeObject = jsonObject["status"];
            return arceyeObject != null;
        }


        public VLResponseParser(string responseBody)
        {
            m_ResponseBody = responseBody;
        }

        public void Parse()
        {
            try
            {
                m_RootObject = JObject.Parse(m_ResponseBody);

                // VL Pass 여부 저장.
                m_IsVLPassed = GetVLResult(m_RootObject);

                // VL 응답에 실패하는 경우 이후 일련의 파싱 과정을 진행하지 않음.
                if (!m_IsVLPassed)
                {
                    return;
                }

                // Pose 필드 파싱.
                string pose = GetString(m_RootObject, PoseKey(), "0,false.jpg,0,0,0,1,0,0,0");

                // 형태를 판단하여 적절한 파싱 메서드 호출
                if (IsNewFormat(pose))
                {
                    ParseNewFormat(pose);
                }
                else
                {
                    ParseLegacyFormat(pose);
                }

                // 공통 처리
                ProcessPoseData();

                // DatasetInfo
                m_DatasetInfo = GetString(m_RootObject, DatasetInfoKey(), "");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Parsing Error! Exception: {e.Message}\n\nResponse Body\n{m_ResponseBody}");
                m_IsVLPassed = false;
                return;
            }
        }

        /// <summary>
        /// 새로운 형태인지 판단 (언더스코어가 포함되어 있는지 확인)
        /// </summary>
        private bool IsNewFormat(string pose)
        {
            // 1758596137537_false.jpg 형태인지 확인
            string[] parts = pose.Split(',');
            if (parts.Length > 0)
            {
                // 첫 번째 부분에 언더스코어가 있으면 새로운 형태
                return parts[0].Contains("_");
            }
            return false;
        }

        /// <summary>
        /// 기존 형태 파싱: 1758596137537,false.jpg,7,8,9,1,0,0,0
        /// </summary>
        private void ParseLegacyFormat(string pose)
        {
            string[] parts = pose.Split(',');

            // Timestamp Parsing.
            m_Timestamp = long.Parse(parts[0]);

            // Pose Parsing.
            float tx = float.Parse(parts[2]);
            float ty = float.Parse(parts[3]);
            float tz = float.Parse(parts[4]);
            float qw = float.Parse(parts[5]);
            float qx = float.Parse(parts[6]);
            float qy = float.Parse(parts[7]);
            float qz = float.Parse(parts[8]);

            SetPoseData(tx, ty, tz, qw, qx, qy, qz);
        }

        /// <summary>
        /// 새로운 형태 파싱: 1758596137537_false.jpg,7,8,9,1,0,0,0
        /// </summary>
        private void ParseNewFormat(string pose)
        {
            string[] parts = pose.Split(',');

            // Timestamp Parsing (언더스코어 앞 부분)
            string timestampPart = parts[0].Split('_')[0];
            m_Timestamp = long.Parse(timestampPart);

            // Pose Parsing.
            float tx = float.Parse(parts[1]);
            float ty = float.Parse(parts[2]);
            float tz = float.Parse(parts[3]);
            float qw = float.Parse(parts[4]);
            float qx = float.Parse(parts[5]);
            float qy = float.Parse(parts[6]);
            float qz = float.Parse(parts[7]);

            SetPoseData(tx, ty, tz, qw, qx, qy, qz);
        }

        private void SetPoseData(float tx, float ty, float tz, float qw, float qx, float qy, float qz)
        {
            // 위치와 회전을 나타내는 Vector3와 Quaternion 생성
            Vector3 position = new Vector3(tx, ty, 1.5f);
            Quaternion rotation = new Quaternion(qx, qy, qz, qw);

            // 이를 사용하여 Matrix4x4 생성
            Matrix4x4 vlMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Matrix4x4 glMatrix = PoseUtility.ConvertVLtoGLCoord(vlMatrix);
            Matrix4x4 lhMatrix = PoseUtility.ConvertLHRH(glMatrix);

            Matrix4x4 unityMatrix = lhMatrix;
            m_Position = unityMatrix.GetPosition();
            m_Rotation = Quaternion.LookRotation(unityMatrix.GetColumn(2), unityMatrix.GetColumn(1));
        }

        private void ProcessPoseData()
        {
            // 응답 정확도 계산. confidence 값 사용.
            // m_Confidence = GetFloat(m_RootObject, ConfidenceKey(), 0.0f);;

            // 응답 정확도 계산. inlier 값 사용.
            m_Confidence = Mathf.Clamp01(GetFloat(m_RootObject, InlierKey(), 0.0f) / kMaxInliers);
        }

        protected string GetString(JObject jobject, string key, string defaultValue)
        {
            if (!jobject.ContainsKey(key))
            {
                Debug.LogWarning($"Parsing Error! No `{key}` field in the response body\n\nResponse Body\n{m_ResponseBody}");
                return defaultValue;
            }

            return jobject[key].ToString();
        }

        protected float GetFloat(JObject jobject, string key, float defaultValue)
        {
            if (!jobject.ContainsKey(key))
            {
                Debug.LogWarning($"Parsing Error! No `{key}` field in the response body\n\nResponse Body\n{m_ResponseBody}");
                return defaultValue;
            }

            return float.Parse(jobject[key].ToString());
        }
    }

    public class ARCeyeResponseParser : VLResponseParser
    {
        public ARCeyeResponseParser(string responseBody) : base(responseBody) { }

        protected override bool GetVLResult(JObject jobject)
        {
            var resultObject = jobject["result"];
            if (resultObject != null)
            {
                string resultStr = GetString(jobject, "result", "FAILURE");
                return (resultStr == "SUCCESS");
            }
            else
            {
                return false;
            }
        }

        protected override string PoseKey()
        {
            return "pose";
        }

        protected override string InlierKey()
        {
            return "inlier";
        }

        protected override string TotalKey()
        {
            return "total";
        }

        protected override string DatasetInfoKey()
        {
            return "datasetInfo";
        }

        protected override string ConfidenceKey()
        {
            return "confidence";
        }
    }


    public class DevResponseParser : VLResponseParser
    {
        public DevResponseParser(string responseBody) : base(responseBody) { }

        protected override bool GetVLResult(JObject jobject)
        {
            var poseObject = jobject["Pose"];
            if (poseObject == null)
            {
                return false;
            }

            string resultStr = GetString(jobject, "Pose", "failed");
            return (resultStr != "failed");
        }

        protected override string PoseKey()
        {
            return "Pose";
        }

        protected override string InlierKey()
        {
            return "Inlier";
        }

        protected override string TotalKey()
        {
            return "Total";
        }

        protected override string DatasetInfoKey()
        {
            return "DatasetInfo";
        }

        protected override string ConfidenceKey()
        {
            return "Confidence";
        }
    }
}