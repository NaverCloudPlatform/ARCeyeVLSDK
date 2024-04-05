using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using AOT;
using ARCeye;


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
    private static extern void SendFailureResponseNative(IntPtr msg, int code);


    static private Texture2D s_QueryTexture = null;
    
    [SerializeField]
    private bool m_SaveQueryImage = false;
    public  bool SaveQueryImage {
        get => m_SaveQueryImage;
        set => m_SaveQueryImage = value;
    }
    
    private DateTime m_FirstQueueFullTime;
    private bool m_CheckQueueFullDuration = false;
    private const int m_FullQueueWaitingSeconds = 5;


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
        if(requestInfo.rawImage != IntPtr.Zero && !CreateQueryTexture(requestInfo.rawImage)) {
            return;
        }

        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.ERROR, "네트워크에 접속할 수 없습니다. 네트워크 설정을 확인해주세요");
            return;
        }

        VLRequestBody body = VLRequestBody.Create(requestInfo);

        if(body.method == "POST") {
            s_Instance.OnSendingRequestAsync(key, body, s_QueryTexture);
        } else {
            s_Instance.OnSendingLimitlessRequest(key, body, s_QueryTexture);
        }
    }

    private static bool CreateQueryTexture(IntPtr rawImage) {
        object texObj = GCHandle.FromIntPtr(rawImage).Target;
        Type texType = texObj.GetType();
        
        if(texType == typeof(Texture2D)) {
            s_QueryTexture = texObj as Texture2D;
            return true;
        } else if(texType == typeof(RenderTexture)) {
            RenderTexture tex = texObj as RenderTexture;
            RenderTexture currRT = RenderTexture.active;

            RenderTexture.active = tex;

            if(s_QueryTexture == null) {
                s_QueryTexture = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            }
            s_QueryTexture.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            s_QueryTexture.Apply();

            RenderTexture.active = currRT;
            return true;
        } else {
            Debug.LogError("Invalid type of texture is used");
            return false;
        }
    }

    private void OnSendingRequestSync(int key, VLRequestBody body, Texture texture)
    {
        if (m_RequestCoroutine == null)
        {
            m_RequestCoroutine = StartCoroutine(Upload(key, body, texture));
        }
    }

    private void OnSendingRequestAsync(int key, VLRequestBody body, Texture texture, int asyncCount = 20)
    {
        if(m_RequestCoroutines.Count < asyncCount)
        {
            var c = StartCoroutine(Upload(key, body, texture));
            m_RequestCoroutines.Add(c);
        }
        else
        {
            ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, $"VL 요청 대기열이 가득 찼습니다. 현재 요청을 무시됩니다. (대기열 크기 = {asyncCount})");
            CheckRequestQueueCapacity();   
        }
    }
    
    private void CheckRequestQueueCapacity()
    {
        if(!m_CheckQueueFullDuration)
        {
            // 대기열이 가득 찬 상태가 5초 이상 유지될 경우 대기열 모두 초기화.
            m_FirstQueueFullTime = DateTime.Now;
            m_CheckQueueFullDuration = true;
        }
        else
        {
            TimeSpan currTime = DateTime.Now.TimeOfDay;
            TimeSpan diff = currTime - m_FirstQueueFullTime.TimeOfDay;
            if(diff.Seconds >= m_FullQueueWaitingSeconds) 
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, $"VL 요청 대기열을 초기화합니다.");
                m_CheckQueueFullDuration = false;
                m_RequestCoroutines.Clear();
            }
        }
    }

    private void OnSendingLimitlessRequest(int key, VLRequestBody body, Texture texture)
    {
        StartCoroutine(Upload(key, body, texture));
    }

    IEnumerator Upload(int key, VLRequestBody requestBody, Texture texture)
    {
        UnityWebRequest www = CreateRequest(requestBody, texture);

        if(m_SaveQueryImage)
        {
            ImageUtility.Save(requestBody.filename, requestBody.image);
        }

        ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + requestBody.ToString());

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.WARNING, "[NetworkController] " + www.error);
        }
        else
        {
            ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.DEBUG, "[NetworkController] " + www.downloadHandler.text);
            
            IntPtr msgPtr = Marshal.StringToHGlobalAnsi(www.downloadHandler.text);

            if(requestBody.method == "POST") {
                SendSuccessResponseNative(key, msgPtr);
            } else {
                Debug.LogError($"[NetworkController] Requested method {requestBody.method} is not implemented!");
            }
        }

        m_RequestCoroutine = null;
        www.Dispose();

        if(m_RequestCoroutines.Count > 0)
        {
            m_RequestCoroutines.RemoveAt(0);
        }
    }

    private UnityWebRequest CreateRequest(VLRequestBody requestBody, Texture texture) {
        UnityWebRequest www = new UnityWebRequest();
        www.url = requestBody.url;
        www.SetRequestHeader("X-ARCEYE-SECRET", requestBody.authorization);

        if(requestBody.method == "POST") {
            requestBody.image = ConvertToJpegData(requestBody.filename, texture);

            www.method = "POST";
            www.uploadHandler = GenerateUploadHandler(requestBody);
        } else {
            www.method = "GET";
        }        

        www.downloadHandler = GenerateDownloadHandler();

        return www;
    }

    private byte[] ConvertToJpegData(string filename, Texture texture) {
        Texture2D previewTex = texture as Texture2D;

        // 모바일 디바이스의 raw image를 CCW90로 회전.
#if !UNITY_EDITOR 
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
}
