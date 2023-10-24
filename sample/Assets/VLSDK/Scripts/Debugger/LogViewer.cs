using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using AOT;

namespace ARCeye
{
    class LogElem
    {
        public string str { get; set; }
        public LogLevel level { get; set; }
        public LogElem(LogLevel l, string s)
        {
            str = s;
            level = l;
        }
    }

    public class LogViewer : MonoBehaviour
    {
        public delegate void DebugLogFuncDelegate(LogLevel level, IntPtr message);


#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif

        [DllImport(dll)]
        private static extern void SetDebugLogFuncNative(DebugLogFuncDelegate func);
        [DllImport(dll)]
        private static extern void ReleaseLoggerNative();


        public VLSDKManager m_VLSDKManager;
        private GeoCoordProvider m_GeoCoordProvider;
        public Transform m_ARCoordRoot;
        public Transform m_VOTObject;

        [SerializeField]
        private bool m_ShowDebugUI = true;

        [SerializeField]
        private bool m_ShowARCoord = true;


        ///// GUI Layout /////
        private float m_TargetScreenWidth = 720.0f * 1.0f;
        private float m_TargetScreenHeight = 1280.0f * 1.0f;
        private Vector3 m_GUIScale = Vector3.zero;
        private Matrix4x4 m_PrevGUIMat = Matrix4x4.identity;
        private Vector2 m_ScrollPosition;

        static private LogLevel s_LogLevel = LogLevel.DEBUG;
        public LogLevel logLevel
        { 
            get => s_LogLevel; 
            set => s_LogLevel = value;
        }
        static private LinkedList<LogElem> s_LogList = new LinkedList<LogElem>();
        static private int s_MaxLogsCount = 100;

        private static string m_Location = "";
        private static string m_Building = "";
        private static string m_CurrState = "";
        private static string m_Floor = "";
        private static string m_LayerInfo = "";

        private Vector3 m_Position;
        private Vector3 m_EulerRotation;

        private GUIStyle style = new GUIStyle();
        private int m_LineHeight = 25;
        private int m_LogBoxHeight = 0;
        private static bool s_IsLogScrollUpdated = false;
        private bool m_LockLogScrollUpdate = false;


        void Awake()
        {
            s_LogList = new LinkedList<LogElem>();

            SetDebugLogFuncNative(DebugLog);

            m_VLSDKManager = GetComponentInParent<VLSDKManager>();
            m_GeoCoordProvider = m_VLSDKManager.GetComponentInChildren<GeoCoordProvider>();
        }

        void OnDestroy()
        {
            ReleaseLoggerNative();
        }

        public void OnStateChanged(TrackerState state)
        {
            m_CurrState = state.ToString();
            Debug.Log($"[LogViewer] OnStateChanged : {state}");
            
            // var locationInfo = m_VLSDKManager.GetLocationInfo();
            // Debug.Log($"[LogViewer] Latitude : {locationInfo.latitude}, Longitude : {locationInfo.longitude}");
        }

        public void OnLayerInfoChanged(string layerInfo) {
            Debug.Log($"[LogViewer] OnLayerInfoChanged : {layerInfo}");
            m_LayerInfo = layerInfo;
        }

        public void OnPoseUpdated(Matrix4x4 matrix, Matrix4x4 projMatrix) {
            Matrix4x4 poseMatrix = matrix.inverse;

            Quaternion rotation = Quaternion.LookRotation(poseMatrix.GetColumn(2), poseMatrix.GetColumn(1));
            m_EulerRotation = rotation.eulerAngles;
        }

        public void OnObjectDetected(DetectedObject detectedObject) {
            Debug.Log("[LogViewer] OnObjectDetected : " + detectedObject.id);
        }

        [MonoPInvokeCallback(typeof(DebugLogFuncDelegate))]
        static public void DebugLog(LogLevel logLevel, IntPtr raw)
        {
            string log = Marshal.PtrToStringAnsi(raw);
            DebugLog(logLevel, log);
        }

        static public void DebugLog(LogLevel logLevel, string log)
        {
            if(logLevel < s_LogLevel) {
                return;
            }
            
            string currTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string msg = string.Format("[{0}] {1}", currTime, log);
            switch (logLevel)
            {
                case LogLevel.DEBUG: {
                    msg = "[Debug] " + msg;
                    Debug.Log(msg);
                    break;
                }
                case LogLevel.INFO: {
                    msg = "[Info] " + msg;
                    Debug.Log(msg);
                    break;
                }
                case LogLevel.WARNING: {
                    msg = "[Warning] " + msg;
                    Debug.LogWarning(msg);
                    break;
                }
                case LogLevel.ERROR: {
                    msg = "[Error] " + msg;
                    Debug.LogError(msg);
                    break;
                }
                case LogLevel.FATAL: {
                    msg = "[Fatal] " + msg;
                    Debug.LogError(msg);
                    break;
                }
            }

            if ((int) logLevel >= (int) LogLevel.INFO)
            {
                if(s_LogList == null)
                {
                    s_LogList = new LinkedList<LogElem>();   
                }

                if (s_LogList.Count > s_MaxLogsCount)
                {
                    s_LogList.RemoveFirst();
                }
                s_LogList.AddLast(new LogElem(logLevel, msg));
                s_IsLogScrollUpdated = true;
            }
        }

        void OnGUI()
        {
            // Scale GUI to target
            m_GUIScale.x = Screen.width / m_TargetScreenWidth;
            m_GUIScale.y = Screen.height / m_TargetScreenHeight;
            m_GUIScale.z = 1.0f;

            m_PrevGUIMat = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, m_GUIScale);

            m_ShowARCoord = GUILayout.Toggle(m_ShowARCoord, "Show AR Coord");
            if(GUI.changed) {
                m_ARCoordRoot.gameObject.SetActive(m_ShowARCoord);
                GUI.changed = false;
            }

            m_ShowDebugUI = GUILayout.Toggle(m_ShowDebugUI, "Show Debug View");
            GUI.changed = false; // 이 처리를 하지 않으면 아래의 ChangeLocation이 항상 실행.

            if (m_ShowDebugUI)
            {
                GUILayout.BeginVertical();

                float debugViewHeight = 800.0f;
                GUI.Box(new Rect(0, 0, m_TargetScreenWidth, debugViewHeight), "");

                string poseStr =  $"Pose : {m_Position}, {m_EulerRotation}";
                GUILayout.Label(poseStr);

                GUILayout.Space(15);

                GUILayout.BeginHorizontal();
                string stateStr = "State : " + m_CurrState;
                GUILayout.Label(stateStr);
                
                GUILayout.Label($"Layer Info : {m_LayerInfo}");

                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                m_GeoCoordProvider.UseFakeGPSCoordOnDevice = GUILayout.Toggle(m_GeoCoordProvider.UseFakeGPSCoordOnDevice, "Use Fake GPS");

                GUILayout.Label($"GPS : {m_GeoCoordProvider.info.latitude}, {m_GeoCoordProvider.info.longitude}");

                DrawLogScrollViewArea();

                GUILayout.EndVertical();
            }

            // Restore original GUIScale.
            GUI.matrix = m_PrevGUIMat;
        }

        private void DrawPoseTrackerControls()
        {
            GUILayout.BeginHorizontal();

            if(GUILayout.Button("Set Config Test")) {
                TrackerConfig config = new TrackerConfig();
                config.useInterpolation = false;
                config.logLevel = 0;
                m_VLSDKManager.SetTrackerConfig(config);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStateControls()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Initial"))
            {
                m_VLSDKManager.ChangeState(TrackerState.INITIAL);
            }
            if (GUILayout.Button("VL Pass"))
            {
                m_VLSDKManager.ChangeState(TrackerState.VL_PASS);
            }
            if (GUILayout.Button("VL FAIL"))
            {
                m_VLSDKManager.ChangeState(TrackerState.VL_FAIL);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset"))
            {
                m_VLSDKManager.ResetSession();
            }
        }

        private void DrawLogScrollViewArea()
        {
            GUILayout.Label("Log");

            if (!m_LockLogScrollUpdate && s_IsLogScrollUpdated)
            {
                m_ScrollPosition = new Vector2(m_ScrollPosition.x, m_LogBoxHeight);
                s_IsLogScrollUpdated = false;
            }

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(m_TargetScreenWidth), GUILayout.Height(350));

            foreach (var elem in s_LogList)
            {
                DrawLogs(elem);
            }

            GUILayout.EndScrollView();

            m_LockLogScrollUpdate = GUILayout.Toggle(m_LockLogScrollUpdate, "Lock Log View");
        }

        private void DrawLogs(LogElem elem)
        {
            // int fontSize = 20;

            switch (elem.level)
            {
                case LogLevel.DEBUG:
                case LogLevel.INFO:
                {
                    style.normal.textColor = Color.white;
                    break;
                }
                case LogLevel.WARNING:
                {
                    style.normal.textColor = Color.yellow;
                    break;
                }
                case LogLevel.ERROR:
                {
                    style.normal.textColor = Color.red;
                    break;
                }
                case LogLevel.FATAL:
                {
                    style.normal.textColor = Color.red;
                    break;
                }
                default:
                {
                    style.normal.textColor = Color.white;
                    break;
                }
            }
            // style.fontSize = fontSize;

            GUILayout.Label(string.Format("{0}\n", elem.str), style);

            m_LogBoxHeight += m_LineHeight;
        }

        public void MoveToPrevScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Prev");
        }
    }

}