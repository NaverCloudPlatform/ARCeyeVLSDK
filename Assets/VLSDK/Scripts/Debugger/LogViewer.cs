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


        [SerializeField]
        public VLSDKManager m_VLSDKManager;

        [SerializeField]
        private bool m_UseDebugUI = true;
        private bool m_ShowDebugUI = false;

        private GeoCoordProvider m_GeoCoordProvider;
        private MultiTouchDetector m_MultiTouchDetector;


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
        private static string m_CurrState = "INITIAL";
        private static string m_Floor = "";
        private static string m_LayerInfo = "none";

        private Vector3 m_Position;
        private Vector3 m_EulerRotation;

        private GUIStyle m_TitleStyle = new GUIStyle();
        private GUIStyle m_ContentsStyle = new GUIStyle();
        private int m_LineHeight = 25;
        private int m_LogBoxHeight = 0;
        private static bool s_IsLogScrollUpdated = false;
        private bool m_LockLogScrollUpdate = false;


        void Awake()
        {
            s_LogList = new LinkedList<LogElem>();
            m_MultiTouchDetector = new MultiTouchDetector();

            m_TitleStyle.normal.textColor = Color.white;
            m_TitleStyle.fontSize = 25;
            m_TitleStyle.fontStyle = FontStyle.Bold;

            m_ContentsStyle.normal.textColor = Color.white;
            m_ContentsStyle.fontSize = 20;

            SetDebugLogFuncNative(DebugLog);
        }

        void Start()
        {
            m_VLSDKManager = GetComponentInParent<VLSDKManager>();
            m_GeoCoordProvider = m_VLSDKManager.GetComponentInChildren<GeoCoordProvider>();
        }

        void Update()
        {
            if(m_UseDebugUI && m_MultiTouchDetector.CheckMultiTouch())
            {
                m_ShowDebugUI = !m_ShowDebugUI;
            }
        }

        void OnDestroy()
        {
            ReleaseLoggerNative();
        }

        void OnGUI()
        {
            // Scale GUI to target
            m_GUIScale.x = Screen.width / m_TargetScreenWidth;
            m_GUIScale.y = Screen.height / m_TargetScreenHeight;
            m_GUIScale.z = 1.0f;

            m_PrevGUIMat = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, m_GUIScale);

            GUI.changed = false; // 이 처리를 하지 않으면 아래의 ChangeLocation이 항상 실행.

            if (m_ShowDebugUI)
            {
                float windowWidth = 600;
                float windowHeight = 900;
                float windowX = (m_TargetScreenWidth - windowWidth) / 2;
                float windowY = (m_TargetScreenHeight - windowHeight) / 2;
                float topMargin = 15;
                float leftMargin = 30;

                Rect windowArea = new Rect(windowX, windowY, windowWidth, windowHeight);
                Rect viewArea = new Rect(windowX + leftMargin, windowY + topMargin, windowWidth - leftMargin, windowHeight - topMargin);

                GUI.Box(windowArea, "");
                
                GUILayout.BeginArea(viewArea);
                GUILayout.BeginVertical();

                DrawVLSDKTitle();

                GUILayout.Space(20);

                DrawStateInfo();                

                GUILayout.Space(20);

                DrawLayerInfo();

                GUILayout.Space(20);

                DrawGPSInfo();

                GUILayout.Space(20);

                DrawPoseInfo();

                GUILayout.Space(20);

                DrawLogScrollViewArea(viewArea);

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }

            GUI.matrix = m_PrevGUIMat;
        }

        private void DrawVLSDKTitle()
        {
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 50;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.BoldAndItalic;
            titleStyle.normal.textColor = Color.white;
            GUILayout.Label("VLSDK", titleStyle);

            GUIStyle versionStyle = new GUIStyle();
            versionStyle.fontSize = 25;
            versionStyle.alignment = TextAnchor.MiddleCenter;
            versionStyle.fontStyle = FontStyle.BoldAndItalic;
            versionStyle.normal.textColor = Color.white;
            GUILayout.Label($"v{m_VLSDKManager?.version}", versionStyle);
        }

        private void DrawStateInfo()
        {
            GUILayout.Label("State", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label(m_CurrState, m_ContentsStyle);
        }

        private void DrawLayerInfo()
        {
            GUILayout.Label("Layer Info", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label(m_LayerInfo, m_ContentsStyle);
        }

        private void DrawGPSInfo()
        {
            GUILayout.Label("GPS", m_TitleStyle);
            GUILayout.Space(10);

            if(m_GeoCoordProvider)
            {
                GUILayout.Label($"{m_GeoCoordProvider.info.latitude}, {m_GeoCoordProvider.info.longitude}", m_ContentsStyle);
            }
            else
            {
                GUILayout.Label("GeoCoordProvider is not detected", m_ContentsStyle);
            }
        }

        private void DrawPoseInfo()
        {
            GUILayout.Label("Pose", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label($"Position (x:{m_Position.x.ToString("N1")} y:{m_Position.y.ToString("N1")} z:{m_Position.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Label($"Rotation (x:{m_EulerRotation.x.ToString("N1")} y:{m_EulerRotation.y.ToString("N1")} z:{m_EulerRotation.z.ToString("N1")})", m_ContentsStyle);
        }

        private void DrawLogScrollViewArea(Rect viewRect)
        {
            GUILayout.Label("Log", m_TitleStyle);

            GUILayout.Space(10);

            if (!m_LockLogScrollUpdate && s_IsLogScrollUpdated)
            {
                m_ScrollPosition = new Vector2(m_ScrollPosition.x, m_LogBoxHeight);
                s_IsLogScrollUpdated = false;
            }

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(viewRect.width - 10), GUILayout.Height(350));

            foreach (var elem in s_LogList)
            {
                DrawLogs(elem);
            }

            GUILayout.EndScrollView();

            m_LockLogScrollUpdate = GUILayout.Toggle(m_LockLogScrollUpdate, "Lock Log View");
        }

        private void DrawLogs(LogElem elem)
        {
            GUIStyle logStyle = new GUIStyle();

            switch (elem.level)
            {
                case LogLevel.DEBUG:
                case LogLevel.INFO:
                {
                    logStyle.normal.textColor = Color.white;
                    break;
                }
                case LogLevel.WARNING:
                {
                    logStyle.normal.textColor = Color.yellow;
                    break;
                }
                case LogLevel.ERROR:
                {
                    logStyle.normal.textColor = Color.red;
                    break;
                }
                case LogLevel.FATAL:
                {
                    logStyle.normal.textColor = Color.red;
                    break;
                }
                default:
                {
                    logStyle.normal.textColor = Color.white;
                    break;
                }
            }

            logStyle.fontSize = 18;

            GUILayout.Label(string.Format("{0}", elem.str), logStyle);

            m_LogBoxHeight += m_LineHeight;
        }


        ///
        ///  Events.
        ///

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

        public void OnPoseUpdated(Matrix4x4 matrix, Matrix4x4 projMatrix, Matrix4x4 texMatrix) {
            Matrix4x4 poseMatrix = matrix.inverse;
            m_Position = poseMatrix.GetColumn(3);

            Quaternion rotation = Quaternion.LookRotation(poseMatrix.GetColumn(2), poseMatrix.GetColumn(1));
            m_EulerRotation = rotation.eulerAngles;
        }

        public void OnObjectDetected(DetectedObject detectedObject) {
            Debug.Log("[LogViewer] OnObjectDetected : " + detectedObject.id);
        }

        ///
        /// Debug Log 
        ///

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
    }

}