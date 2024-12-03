using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using AOT;
using Newtonsoft.Json.Linq;

namespace ARCeye
{
    public class NetworkController : MonoBehaviour
    {
        private static NetworkController s_Instance;
        private static TextureProvider s_TextureProvider;

        public delegate void RequestVLDelegate(int key, ARCeye.RequestVLInfo requestInfo);


#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif

        private List<Coroutine> m_RequestCoroutines = new List<Coroutine>();
        private Coroutine m_RequestCoroutine = null;

        [DllImport(dll)]
        private static extern void SetRequestFuncNative(RequestVLDelegate func);

        [DllImport(dll)]
        private static extern void SendSuccessResponseNative(int key, IntPtr msg);

        [DllImport(dll)]
        private static extern void SendSuccessVLGetResponseNative(IntPtr msg, int code, IntPtr fptr);

        [DllImport(dll)]
        private static extern void SendFailureResponseNative(int key, IntPtr msg, int code);


        static private Texture2D s_QueryTexture = null;

        [SerializeField]
        private bool m_SaveQueryImage = false;
        public bool SaveQueryImage
        {
            get => m_SaveQueryImage;
            set => m_SaveQueryImage = value;
        }

        // 현재 위치 기반 VL 요청 기능 활성화 여부 설정.
        // 기본값은 true.
        // 사용자의 필요에 따라 직접 이 값을 변경할 수 있음.
        public bool useRequestWithPosition { get; set; } = true;

        private DateTime m_FirstQueueFullTime;
        private bool m_CheckQueueFullDuration = false;
        private const int m_FullQueueWaitingSeconds = 5;

        public UnityAction<ARCeye.RequestVLInfo> OnRequestInfoReceived { get; set; }
        public UnityAction<string> OnRawResponseReceived { get; set; }


        private void Awake()
        {
            s_Instance = this;
            s_TextureProvider = GetComponent<TextureProvider>();
        }

        public void Initialize()
        {
            SetRequestFuncNative(OnRequest);
        }


        [MonoPInvokeCallback(typeof(RequestVLDelegate))]
        unsafe private static void OnRequest(int key, ARCeye.RequestVLInfo requestInfo)
        {
            s_Instance.OnRequestInfoReceived?.Invoke(requestInfo);

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if(requestInfo.imageBuffer.pixels == IntPtr.Zero || requestInfo.imageBuffer.length == 0) {
                return;
            }

            if(Application.internetReachability == NetworkReachability.NotReachable)
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.ERROR, "Network is disconnected");
                return;
            }

            VLRequestBody body = VLRequestBody.Create(requestInfo);

            byte[] imageBuffer = new byte[requestInfo.imageBuffer.length];
            Marshal.Copy(requestInfo.imageBuffer.pixels, imageBuffer, 0, requestInfo.imageBuffer.length);

            if(body.method == "POST") {
                s_Instance.OnSendingRequestAsync(key, body, imageBuffer);
            } else {
                s_Instance.OnSendingLimitlessRequest(key, body, imageBuffer);
            }
#else
            // 쿼리 텍스쳐 생성.
            if (requestInfo.texture != IntPtr.Zero && !CreateQueryTexture(requestInfo.texture))
            {
                return;
            }

            // 네트워크 연결 여부 확인.
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.ERROR, "Network is disconnected");
                return;
            }

            // 현재 위치 기반 요청 여부 설정
            // useRequestWithPosition이 true인 경우에는 native에서 할당된 requestWithPosition 값을 그대로 사용.
            // useRequestWithPosition이 false인 경우에만 requestWithPosition 값을 강제로 false로 설정.
            if(!s_Instance.useRequestWithPosition)
            {
                requestInfo.requestWithPosition = false;
            }

            // Request body 생성.
            VLRequestBody body = VLRequestBody.Create(requestInfo);

            bool isValidRequest = VLRequestBody.IsValidRequest(body, s_QueryTexture);

            if (!isValidRequest)
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.ERROR, "Invalid VL request body. " + body.ToString());
                return;
            }

            if (body.method == "POST")
            {
                s_Instance.OnSendingRequestAsync(key, body, s_QueryTexture);
            }
            else
            {
                s_Instance.OnSendingLimitlessRequest(key, body, s_QueryTexture);
            }
#endif
        }

        private static bool CreateQueryTexture(IntPtr rawImage)
        {
            object texObj = GCHandle.FromIntPtr(rawImage).Target;
            Type texType = texObj.GetType();

            if (texType == typeof(Texture2D))
            {
                s_QueryTexture = texObj as Texture2D;
                return true;
            }
            else if (texType == typeof(RenderTexture))
            {
                RenderTexture tex = texObj as RenderTexture;
                RenderTexture currRT = RenderTexture.active;

                RenderTexture.active = tex;

                if (s_QueryTexture == null)
                {
                    s_QueryTexture = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                }
                s_QueryTexture.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                s_QueryTexture.Apply();

                RenderTexture.active = currRT;
                return true;
            }
            else
            {
                Debug.LogError("Invalid type of texture is used");
                return false;
            }
        }

        private void OnSendingRequestSync(int key, VLRequestBody body, byte[] imageBuffer)
        {
            if (m_RequestCoroutine == null)
            {
                m_RequestCoroutine = StartCoroutine(Upload(key, body, imageBuffer));
            }
        }

        private void OnSendingRequestSync(int key, VLRequestBody body, Texture texture)
        {
            if (m_RequestCoroutine == null)
            {
                m_RequestCoroutine = StartCoroutine(Upload(key, body, texture));
            }
        }

        private void OnSendingRequestAsync(int key, VLRequestBody body, byte[] imageBuffer, int asyncCount = 20)
        {
            if (m_RequestCoroutines.Count < asyncCount)
            {
                var c = StartCoroutine(Upload(key, body, imageBuffer));
                m_RequestCoroutines.Add(c);
            }
            else
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.WARNING, $"VL request queue is full. Current request is ignored. (Queue size = {asyncCount})");
                CheckRequestQueueCapacity();
            }
        }

        private void OnSendingRequestAsync(int key, VLRequestBody body, Texture texture, int asyncCount = 20)
        {
            if (m_RequestCoroutines.Count < asyncCount)
            {
                var c = StartCoroutine(Upload(key, body, texture));
                m_RequestCoroutines.Add(c);
            }
            else
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.WARNING, $"VL request queue is full. Current request is ignored. (Queue size = {asyncCount})");
                CheckRequestQueueCapacity();
            }
        }

        private void CheckRequestQueueCapacity()
        {
            if (!m_CheckQueueFullDuration)
            {
                // 대기열이 가득 찬 상태가 5초 이상 유지될 경우 대기열 모두 초기화.
                m_FirstQueueFullTime = DateTime.Now;
                m_CheckQueueFullDuration = true;
            }
            else
            {
                TimeSpan currTime = DateTime.Now.TimeOfDay;
                TimeSpan diff = currTime - m_FirstQueueFullTime.TimeOfDay;
                if (diff.Seconds >= m_FullQueueWaitingSeconds)
                {
                    ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.INFO, $"VL request queue is cleared");
                    m_CheckQueueFullDuration = false;
                    m_RequestCoroutines.Clear();
                }
            }
        }

        private void OnSendingLimitlessRequest(int key, VLRequestBody body, byte[] imageBuffer)
        {
            StartCoroutine(Upload(key, body, imageBuffer));
        }

        private void OnSendingLimitlessRequest(int key, VLRequestBody body, Texture texture)
        {
            StartCoroutine(Upload(key, body, texture));
        }

        IEnumerator Upload(int key, VLRequestBody requestBody, byte[] imageBuffer)
        {
            UnityWebRequest www = CreateRequest(requestBody, imageBuffer);

            ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + requestBody.ToString());

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + www.downloadHandler.text);

                IntPtr msgPtr = Marshal.StringToHGlobalAnsi(www.downloadHandler.text);

                ExtractRawVLPose(www.downloadHandler.text);

                if (requestBody.method == "POST")
                {
                    SendSuccessResponseNative(key, msgPtr);
                }
                else
                {
                    Debug.LogError($"[NetworkController] Requested method {requestBody.method} is not implemented!");
                }
            }
            else
            {
                IntPtr msgPtr = Marshal.StringToHGlobalAnsi(www.downloadHandler.text);

                if (requestBody.method == "POST")
                {
                    SendFailureResponseNative(key, msgPtr, (int)www.responseCode);
                }
                else
                {
                    Debug.LogError($"[NetworkController] Requested method {requestBody.method} is not implemented!");
                }
            }

            m_RequestCoroutine = null;
            www.Dispose();

            if (m_RequestCoroutines.Count > 0)
            {
                m_RequestCoroutines.RemoveAt(0);
            }
        }

        IEnumerator Upload(int key, VLRequestBody requestBody, Texture texture)
        {
            UnityWebRequest www = CreateRequest(requestBody, texture);

            if (m_SaveQueryImage)
            {
                ImageUtility.Save(requestBody.filename, requestBody.image);
            }

            ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + requestBody.ToString());

            yield return www.SendWebRequest();

            // raw response 전달.
            OnRawResponseReceived?.Invoke(www.downloadHandler.text);

            if (www.result == UnityWebRequest.Result.Success)
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + www.downloadHandler.text);

                IntPtr msgPtr = Marshal.StringToHGlobalAnsi(www.downloadHandler.text);

                ExtractRawVLPose(www.downloadHandler.text);

                if (requestBody.method == "POST")
                {
                    SendSuccessResponseNative(key, msgPtr);
                }
                else
                {
                    Debug.LogError($"[NetworkController] Requested method {requestBody.method} is not implemented!");
                }
            }
            else
            {
                IntPtr msgPtr = Marshal.StringToHGlobalAnsi(www.downloadHandler.text);

                if (requestBody.method == "POST")
                {
                    SendFailureResponseNative(key, msgPtr, (int)www.responseCode);
                }
                else
                {
                    Debug.LogError($"[NetworkController] Requested method {requestBody.method} is not implemented!");
                }
            }

            m_RequestCoroutine = null;
            www.Dispose();

            if (m_RequestCoroutines.Count > 0)
            {
                m_RequestCoroutines.RemoveAt(0);
            }
        }

        private UnityWebRequest CreateRequest(VLRequestBody requestBody, byte[] imageBuffer)
        {
            UnityWebRequest www = new UnityWebRequest();
            www.url = requestBody.url;
            www.SetRequestHeader("X-ARCEYE-SECRET", requestBody.authorization);

            if (requestBody.method == "POST")
            {
                requestBody.image = imageBuffer;

                www.method = "POST";
                www.uploadHandler = GenerateUploadHandler(requestBody);
            }
            else
            {
                www.method = "GET";
            }

            www.downloadHandler = GenerateDownloadHandler();

            return www;
        }

        private UnityWebRequest CreateRequest(VLRequestBody requestBody, Texture texture)
        {
            UnityWebRequest www = new UnityWebRequest();
            www.url = requestBody.url;
            www.SetRequestHeader("X-ARCEYE-SECRET", requestBody.authorization);

            if (requestBody.method == "POST")
            {
                requestBody.image = ConvertToJpegData(requestBody.filename, texture);

                www.method = "POST";
                www.uploadHandler = GenerateUploadHandler(requestBody);
            }
            else
            {
                www.method = "GET";
            }

            www.downloadHandler = GenerateDownloadHandler();

            return www;
        }

        private byte[] ConvertToJpegData(string filename, Texture texture)
        {
            Texture2D previewTex = texture as Texture2D;

            // 모바일 디바이스의 raw image를 CCW90로 회전.
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        previewTex = ImageUtility.RotateTexture(previewTex, s_TextureProvider.texMatrix);
#endif

            byte[] data = ImageConversion.EncodeToJPG(previewTex, 85);
            return data;
        }

        private UploadHandler GenerateUploadHandler(VLRequestBody requestBody)
        {
            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] body = GenerateBodyBuffer(requestBody, boundary);

            UploadHandler uploader = new UploadHandlerRaw(body);
            string contentType = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));
            uploader.contentType = contentType;

            return uploader;
        }

        private byte[] GenerateBodyBuffer(VLRequestBody requestBody, byte[] boundary)
        {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormFileSection(requestBody.imageFieldName, requestBody.image, requestBody.filename, "image/jpeg"));
            foreach (var param in requestBody.parameters)
            {
                formData.Add(new MultipartFormDataSection(param.Key, param.Value));
            }

            byte[] formSections = UnityWebRequest.SerializeFormSections(formData, boundary);
            byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));
            byte[] body = new byte[formSections.Length + terminate.Length];

            Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
            Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

            return body;
        }

        private DownloadHandler GenerateDownloadHandler()
        {
            return new DownloadHandlerBuffer();
        }


        // pose 디버깅용.

        private List<Matrix4x4> m_VLResponses = new List<Matrix4x4>();
        private List<int> m_Inliers = new List<int>();
        private bool m_ShowVLPose = false;

        public void EnableVLPose(bool value)
        {
            m_ShowVLPose = value;
        }

        private void ExtractRawVLPose(string response)
        {
            JObject jsonObject = JObject.Parse(response);

            var ARCeyeResponseObject = jsonObject["result"];

            if (ARCeyeResponseObject != null)
            {
                ExtractARCeyeRawVLPose(jsonObject);
            }
            else
            {
                ExtractDevRawVLPose(jsonObject);
            }
        }

        private void ExtractARCeyeRawVLPose(JObject jsonObject)
        {
            string result = jsonObject["result"].ToString();
            if (result == "FAILURE")
            {
                return;
            }

            string pose = jsonObject["pose"].ToString();
            string[] parts = pose.Split(',');

            // tx, ty, tz, qw, qx, qy, qz 값을 추출
            float tx = float.Parse(parts[2]);
            float ty = float.Parse(parts[3]);
            float tz = float.Parse(parts[4]);
            float qw = float.Parse(parts[5]);
            float qx = float.Parse(parts[6]);
            float qy = float.Parse(parts[7]);
            float qz = float.Parse(parts[8]);

            // 위치와 회전을 나타내는 Vector3와 Quaternion 생성
            Vector3 position = new Vector3(tx, ty, 1.5f);
            Quaternion rotation = new Quaternion(qx, qy, qz, qw);

            // 이를 사용하여 Matrix4x4 생성
            Matrix4x4 vlMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Matrix4x4 glMatrix = PoseUtility.ConvertVLtoGLCoord(vlMatrix);
            Matrix4x4 lhMatrix = PoseUtility.ConvertLHRH(glMatrix);

            Matrix4x4 unityMatrix = lhMatrix;
            m_VLResponses.Add(unityMatrix);

            int inlier = int.Parse(jsonObject["inlier"].ToString());
            m_Inliers.Add(inlier);
        }

        private void ExtractDevRawVLPose(JObject jsonObject)
        {
            string result = jsonObject["Pose"].ToString();
            if (result == "failed")
            {
                return;
            }

            string pose = jsonObject["Pose"].ToString();
            string[] parts = pose.Split(',');

            // tx, ty, tz, qw, qx, qy, qz 값을 추출
            float tx = float.Parse(parts[2]);
            float ty = float.Parse(parts[3]);
            float tz = float.Parse(parts[4]);
            float qw = float.Parse(parts[5]);
            float qx = float.Parse(parts[6]);
            float qy = float.Parse(parts[7]);
            float qz = float.Parse(parts[8]);

            // 위치와 회전을 나타내는 Vector3와 Quaternion 생성
            Vector3 position = new Vector3(tx, ty, 1.5f);
            Quaternion rotation = new Quaternion(qx, qy, qz, qw);

            // 이를 사용하여 Matrix4x4 생성
            Matrix4x4 vlMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Matrix4x4 glMatrix = PoseUtility.ConvertVLtoGLCoord(vlMatrix);
            Matrix4x4 lhMatrix = PoseUtility.ConvertLHRH(glMatrix);

            Matrix4x4 unityMatrix = lhMatrix;
            m_VLResponses.Add(unityMatrix);

            int inlier = int.Parse(jsonObject["Inlier"].ToString());
            m_Inliers.Add(inlier);
        }

        void OnDrawGizmos()
        {
            if (!m_ShowVLPose)
            {
                return;
            }

            for (int i = 0; i < m_VLResponses.Count; i++)
            {
                Color frameColor = Color.red;

                if (m_Inliers[i] > 400)
                {
                    frameColor = new Color32(0, 255, 0, 255);
                }
                else if (m_Inliers[i] > 300)
                {
                    frameColor = new Color32(128, 255, 0, 255);
                }
                else if (m_Inliers[i] > 200)
                {
                    frameColor = new Color32(255, 255, 0, 255);
                }
                else if (m_Inliers[i] > 100)
                {
                    frameColor = new Color32(255, 128, 0, 255);
                }
                else
                {
                    frameColor = new Color32(255, 0, 0, 255);
                }

                DebugUtility.DrawFrame(m_VLResponses[i], frameColor, 1.0f);
            }
        }
    }

}