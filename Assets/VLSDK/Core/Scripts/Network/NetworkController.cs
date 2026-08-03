using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
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

        // 대기열 밖에서 실행되는 요청의 ID. 유효한 요청 ID는 1부터 시작한다.
        private const long k_UnqueuedRequestId = 0;

        private Dictionary<long, Coroutine> m_RequestCoroutines = new Dictionary<long, Coroutine>();
        private long m_NextRequestId = k_UnqueuedRequestId;

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
            // 하나의 프레임에서 URL 후보별로 요청이 연달아 생성되기 때문에
            // 시간 값은 요청의 고유 키가 될 수 없다.
            long requestId = ++m_NextRequestId;

            if (m_RequestCoroutines.Count < asyncCount)
            {
                var c = StartCoroutine(Upload(requestId, key, body, texture));
                m_RequestCoroutines.Add(requestId, c);
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
            // 대기열 제한을 받지 않는 요청이기 때문에 m_RequestCoroutines에 등록하지 않는다.
            StartCoroutine(Upload(k_UnqueuedRequestId, key, body, texture));
        }

        /// <summary>
        ///   VLRequestBody를 이용하여 VL 요청을 보내고 응답을 처리.
        /// </summary>
        IEnumerator Upload(long requestId, int key, VLRequestBody requestBody, Texture texture)
        {
            // POST 요청은 JPEG 인코딩을 백그라운드 스레드로 오프로드.
            // 코루틴은 첫 yield 전까지 네이티브 재진입 경로에서 동기 실행되므로,
            // 아래 동기 구간은 리드백 결과 복사 + 인코딩 Task 시작 등 가벼운 작업만 수행.
            if (requestBody.method == "POST")
            {
                Texture2D queryTexture = texture as Texture2D;
                if (queryTexture == null)
                {
                    NativeLogger.DebugLog(ARCeye.LogLevel.WARNING, "Query texture is not a Texture2D. Request is skipped");
                    RemoveRequestCoroutine(requestId);
                    yield break;
                }

                // 리드백 직후의 픽셀을 byte[] 사본으로 캡처. 이후 리드백과 경합 없이 백그라운드에서 사용.
                byte[] rawData = queryTexture.GetRawTextureData();
                GraphicsFormat graphicsFormat = queryTexture.graphicsFormat;
                uint width = (uint)queryTexture.width;
                uint height = (uint)queryTexture.height;

                // 배열 기반 인코딩은 스레드 안전하므로 백그라운드 스레드에서 실행.
                Task<byte[]> encodeTask = Task.Run(
                    () => EncodeJpegInBackground(rawData, graphicsFormat, width, height));

                // 인코딩 완료까지 대기. 이 지점부터는 네이티브 호출 밖(다음 프레임 이후)에서 실행됨.
                yield return new WaitUntil(() => encodeTask.IsCompleted);

                byte[] encoded = null;
                if (encodeTask.IsFaulted)
                {
                    NativeLogger.DebugLog(ARCeye.LogLevel.WARNING, "Failed to encode a query image in background. " + encodeTask.Exception?.GetBaseException().Message);
                }
                else
                {
                    encoded = encodeTask.Result;
                }

                if (encoded == null || encoded.Length == 0)
                {
                    NativeLogger.DebugLog(ARCeye.LogLevel.WARNING, "Encoded query image is empty. Request is skipped");
                    RemoveRequestCoroutine(requestId);
                    yield break;
                }

                requestBody.image = encoded;
            }

            // 무거운 작업(멀티파트 바디 생성) 및 UnityWebRequest 생성/전송은 모두 메인 스레드에서 수행.
            UnityWebRequest www = HandleRequest(requestBody, texture);

            yield return www.SendWebRequest();

            HandleResponse(requestId, key, requestBody.method, www);
        }

        private void RemoveRequestCoroutine(long requestId)
        {
            if (requestId == k_UnqueuedRequestId)
            {
                return;
            }

            if (!m_RequestCoroutines.Remove(requestId))
            {
                Debug.LogWarning("Failed to remove request id: " + requestId);
            }
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
                // requestBody.image는 Upload 코루틴에서 백그라운드 인코딩으로 이미 세팅됨.
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

        // 백그라운드 스레드에서 실행되는 JPEG 인코딩. GetRawTextureData 기반 배열 입력이라 스레드 안전.
        private static byte[] EncodeJpegInBackground(byte[] rawData, GraphicsFormat format, uint width, uint height)
        {
            return ImageConversion.EncodeArrayToJPG(rawData, format, width, height, 0, 85);
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
        private void HandleResponse(long requestId, int key, string method, UnityWebRequest www)
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
                    ResponseStatus responseStatus = ResponseStatus.UnknownError;

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

            www.Dispose();

            RemoveRequestCoroutine(requestId);
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