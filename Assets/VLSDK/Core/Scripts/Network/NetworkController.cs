using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using AOT;

[assembly: InternalsVisibleTo("ARCeye.VLSDK.Tests")]

namespace ARCeye
{
    public class NetworkController : MonoBehaviour
    {
        private static NetworkController s_Instance;
        private static TextureProvider s_TextureProvider;
        private static VLPoseDrawer s_VLPoseDrawer;

        public delegate void RequestVLDelegate(int key, ARCeye.RequestVLInfo requestInfo);
        public delegate void ResponseVLDelegate(NativeVLResponseEventData eventData);


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
        private static extern void SetResponseFuncNative(ResponseVLDelegate func);


        [DllImport(dll)]
        internal static extern void SendSuccessResponseNative(int key, IntPtr msg);

        [DllImport(dll)]
        internal static extern void SendSuccessVLGetResponseNative(IntPtr msg, int code, IntPtr fptr);

        [DllImport(dll)]
        internal static extern void SendFailureResponseNative(int key, IntPtr msg, int code);



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

        public UnityEvent<VLRequestEventData> OnVLPoseRequested { get; set; }
        public UnityEvent<VLResponseEventData> OnVLPoseResponded { get; set; }


        private void Awake()
        {
            s_Instance = this;
            s_TextureProvider = GetComponent<TextureProvider>();
#if UNITY_EDITOR
            s_VLPoseDrawer = gameObject.AddComponent<VLPoseDrawer>();
#endif
        }

        public void Initialize()
        {
            SetRequestFuncNative(OnRequest);
            SetResponseFuncNative(OnResponse);
        }

        public void EnableVLPose(bool value)
        {
            s_VLPoseDrawer?.EnableVLPose(value);
        }

        /// <summary>
        ///   Native 영역에서 호출하는 VL 요청 메서드.
        /// </summary>
        [MonoPInvokeCallback(typeof(RequestVLDelegate))]
        unsafe private static void OnRequest(int key, ARCeye.RequestVLInfo requestInfo)
        {
            // 쿼리 텍스쳐 생성.
            if (!s_TextureProvider.CreateQueryTexture(requestInfo, ref s_QueryTexture))
            {
                NativeLogger.DebugLog(ARCeye.LogLevel.WARNING, "Failed to create a query texture");
                return;
            }

            // 네트워크 연결 여부 확인.
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                var responseEventData = VLResponseEventData.Create(ResponseStatus.NetworkConnectionError);
                s_Instance.OnVLPoseResponded?.Invoke(responseEventData);

                NativeLogger.DebugLog(ARCeye.LogLevel.ERROR, "Network is disconnected");
                return;
            }

            // 현재 위치 기반 요청 여부 설정
            // useRequestWithPosition이 true인 경우에는 native에서 할당된 requestWithPosition 값을 그대로 사용.
            // useRequestWithPosition이 false인 경우에만 requestWithPosition 값을 강제로 false로 설정.
            if (!s_Instance.useRequestWithPosition)
            {
                requestInfo.requestWithPosition = false;
            }

            // Request body 생성.
            VLRequestBody body = VLRequestBody.Create(requestInfo);

            // 유효하지 않은 형태의 요청인 경우.
            if (!VLRequestBody.IsValidRequest(body, s_QueryTexture))
            {
                var responseEventData = VLResponseEventData.Create(ResponseStatus.BadRequestClient);
                s_Instance.OnVLPoseResponded?.Invoke(responseEventData);

                NativeLogger.DebugLog(ARCeye.LogLevel.ERROR, "Invalid VL request body. " + body.ToString());
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
                NativeLogger.DebugLog(ARCeye.LogLevel.WARNING, $"VL request queue is full. Current request is ignored. (Queue size = {asyncCount})");
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
                    NativeLogger.DebugLog(ARCeye.LogLevel.INFO, $"VL request queue is cleared");
                    m_CheckQueueFullDuration = false;
                    m_RequestCoroutines.Clear();
                }
            }
        }

        private void OnSendingLimitlessRequest(int key, VLRequestBody body, Texture texture)
        {
            StartCoroutine(Upload(key, body, texture));
        }

        /// <summary>
        ///   VLRequestBody를 이용하여 VL 요청을 보내고 응답을 처리.
        /// </summary>
        IEnumerator Upload(int key, VLRequestBody requestBody, Texture texture)
        {
            UnityWebRequest www = HandleRequest(requestBody, texture);

            yield return www.SendWebRequest();

            HandleReponse(key, requestBody.method, www);
        }

        /// <summary>
        ///   VLRequestBody를 이용한 VL 요청 처리.
        /// </summary>
        private UnityWebRequest HandleRequest(VLRequestBody requestBody, Texture texture)
        {
            UnityWebRequest www = CreateRequest(requestBody, texture);

            if (m_SaveQueryImage)
            {
                ImageUtility.Save(requestBody.filename, requestBody.image);
            }

            // 요청 이벤트 전달.
            VLRequestEventData requestEventData = VLRequestEventData.Create(requestBody, requestBody.image);
            s_Instance.OnVLPoseRequested?.Invoke(requestEventData);

            NativeLogger.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + requestBody.ToString());

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

        /// <summary>
        ///   수신한 결과를 바탕으로 VL 응답 처리.
        /// </summary>
        private void HandleReponse(int key, string method, UnityWebRequest www)
        {
            string rawResponse = www.downloadHandler.text;

            if (www.result == UnityWebRequest.Result.Success)
            {
                NativeLogger.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + rawResponse);

                if (method == "POST")
                {
                    IntPtr msgPtr = Marshal.StringToHGlobalAnsi(rawResponse);
                    SendSuccessResponseNative(key, msgPtr);
                }
                else
                {
                    Debug.LogError($"[NetworkController] Requested method {method} is not implemented!");
                }
            }
            else
            {
                IntPtr msgPtr = Marshal.StringToHGlobalAnsi(rawResponse);

                if (method == "POST")
                {
                    // 응답 코드를 바탕으로 ResponseStatus를 설정.
                    ResponseStatus responseStatus = ResponseStatus.UnknownError
                    ;
                    int responseCode = (int)www.responseCode;
                    if (responseCode == 400)
                    {
                        responseStatus = ResponseStatus.BadRequestServer;
                    }
                    else if (responseCode == 500)
                    {
                        responseStatus = ResponseStatus.InternalServerError;
                    }

                    SendFailureResponseNative(key, msgPtr, (int)responseStatus);
                }
                else
                {
                    Debug.LogError($"[NetworkController] Requested method {method} is not implemented!");
                }
            }

            m_RequestCoroutine = null;
            www.Dispose();

            if (m_RequestCoroutines.Count > 0)
            {
                m_RequestCoroutines.RemoveAt(0);
            }
        }


        /// <summary>
        ///   Native 영역에서 호출하는 VL 응답 메서드. 
        /// </summary>
        [MonoPInvokeCallback(typeof(ResponseVLDelegate))]
        unsafe private static void OnResponse(NativeVLResponseEventData nativeEventData)
        {
            var responseEventData = VLResponseEventData.Create(nativeEventData);
            s_VLPoseDrawer?.AddRawVLPose(responseEventData);

            // VL Response Event 호출.
            s_Instance.OnVLPoseResponded?.Invoke(responseEventData);
        }
    }

}