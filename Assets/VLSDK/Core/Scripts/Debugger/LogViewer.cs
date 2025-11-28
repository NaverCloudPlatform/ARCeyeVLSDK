using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using AOT;

namespace ARCeye
{
    public class LogViewer : MonoBehaviour
    {
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
        private Vector2 m_MainScrollPosition;
        private bool m_IsLandscape = false;




        static private LinkedList<LogElem> s_LogList = new LinkedList<LogElem>();
        static private int s_MaxLogsCount = 100;

        private static string m_CurrState = "INITIAL";
        private static string m_LayerInfo = "none";

        private Camera m_Camera;
        private Transform m_Origin;
        private Vector3 m_VLPosition;
        private Vector3 m_VLRotation;
        private double m_RelAltitude;

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
        }

        void Start()
        {
            m_VLSDKManager = GetComponentInParent<VLSDKManager>();
            m_GeoCoordProvider = m_VLSDKManager.GetComponentInChildren<GeoCoordProvider>();
        }

        void Update()
        {
            if (Debug.isDebugBuild && m_UseDebugUI && m_MultiTouchDetector.CheckMultiTouch())
            {
                m_ShowDebugUI = !m_ShowDebugUI;
            }

            // 화면 방향 감지 및 타겟 해상도 업데이트
            UpdateScreenOrientation();
        }

        /// <summary>
        /// 현재 화면 방향을 감지하고 타겟 해상도를 조정한다.
        /// </summary>
        private void UpdateScreenOrientation()
        {
            bool isCurrentlyLandscape = Screen.width > Screen.height;

            // 방향이 변경된 경우에만 타겟 해상도 업데이트
            if (m_IsLandscape != isCurrentlyLandscape)
            {
                m_IsLandscape = isCurrentlyLandscape;

                if (m_IsLandscape)
                {
                    // Landscape: 가로가 더 김
                    m_TargetScreenWidth = 1280.0f;
                    m_TargetScreenHeight = 720.0f;
                }
                else
                {
                    // Portrait: 세로가 더 김
                    m_TargetScreenWidth = 720.0f;
                    m_TargetScreenHeight = 1280.0f;
                }
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

            GUI.changed = false; // 이 처리를 하지 않으면 아래의 ChangeLocation이 항상 실행.

            if (m_ShowDebugUI)
            {
                // 화면 방향에 따라 윈도우 크기 조정
                float windowWidth, windowHeight, logViewHeight, contentHeight;

                if (m_IsLandscape)
                {
                    // Landscape 모드: 가로로 넓게, 스크롤 가능하도록 높이 제한
                    windowWidth = 900;
                    windowHeight = 650;
                    logViewHeight = 200;
                    contentHeight = 1200; // 실제 컨텐츠 높이
                }
                else
                {
                    // Portrait 모드: 세로로 길게
                    windowWidth = 600;
                    windowHeight = 1200;
                    logViewHeight = 350;
                    contentHeight = 1200;
                }

                float windowX = (m_TargetScreenWidth - windowWidth) / 2;
                float windowY = (m_TargetScreenHeight - windowHeight) / 2;
                float topMargin = 15;
                float leftMargin = 30;
                float rightMargin = 10; // 오른쪽 여백을 줄여서 스크롤바를 바깥쪽으로

                Rect windowArea = new Rect(windowX, windowY, windowWidth, windowHeight);
                Rect viewArea = new Rect(windowX + leftMargin, windowY + topMargin, windowWidth - leftMargin - rightMargin, windowHeight - topMargin * 2);

                GUI.Box(windowArea, "");

                // 메인 스크롤뷰로 전체 컨텐츠 감싸기
                GUILayout.BeginArea(windowArea);

                m_MainScrollPosition = GUI.BeginScrollView(
                    new Rect(leftMargin, topMargin, windowWidth - leftMargin - rightMargin, windowHeight - topMargin * 2),
                    m_MainScrollPosition,
                    new Rect(0, 0, windowWidth - leftMargin - rightMargin - 20, contentHeight)
                );

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

                DrawLogScrollViewArea(viewArea, logViewHeight);

                GUILayout.EndVertical();

                GUI.EndScrollView();
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

            if (m_GeoCoordProvider)
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
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
                m_Origin = m_Camera.transform.parent;
            }

            var localizedPosition = m_Camera.transform.position;
            var localizedRotation = m_Camera.transform.rotation.eulerAngles;

            var vioPosition = m_Camera.transform.localPosition;
            var vioRotation = m_Camera.transform.localRotation.eulerAngles;

            var originPosition = m_Origin.position;
            var originRotation = m_Origin.rotation.eulerAngles;

            GUILayout.Label("Localized Pose", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label($"Position (x:{localizedPosition.x.ToString("N1")} y:{localizedPosition.y.ToString("N1")} z:{localizedPosition.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Label($"Rotation (x:{localizedRotation.x.ToString("N1")} y:{localizedRotation.y.ToString("N1")} z:{localizedRotation.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Space(10);

            GUILayout.Label("VIO Pose", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label($"Position (x:{vioPosition.x.ToString("N1")} y:{vioPosition.y.ToString("N1")} z:{vioPosition.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Label($"Rotation (x:{vioRotation.x.ToString("N1")} y:{vioRotation.y.ToString("N1")} z:{vioRotation.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Space(10);

            GUILayout.Label("VL Pose", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label($"Position (x:{m_VLPosition.x.ToString("N1")} y:{m_VLPosition.y.ToString("N1")} z:{m_VLPosition.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Label($"Rotation (x:{m_VLRotation.x.ToString("N1")} y:{m_VLRotation.y.ToString("N1")} z:{m_VLRotation.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Space(10);

            GUILayout.Label("Origin Pose", m_TitleStyle);
            GUILayout.Space(10);
            GUILayout.Label($"Position (x:{originPosition.x.ToString("N1")} y:{originPosition.y.ToString("N1")} z:{originPosition.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Label($"Rotation (x:{originRotation.x.ToString("N1")} y:{originRotation.y.ToString("N1")} z:{originRotation.z.ToString("N1")})", m_ContentsStyle);
            GUILayout.Space(10);

            GUILayout.Space(10);
            GUILayout.Label($"Altitude: {m_RelAltitude}", m_ContentsStyle);
        }

        private void DrawLogScrollViewArea(Rect viewRect, float logViewHeight)
        {
            GUILayout.Label("Log", m_TitleStyle);

            GUILayout.Space(10);

            if (!m_LockLogScrollUpdate && s_IsLogScrollUpdated)
            {
                m_ScrollPosition = new Vector2(m_ScrollPosition.x, m_LogBoxHeight);
                s_IsLogScrollUpdated = false;
            }

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(viewRect.width - 10), GUILayout.Height(logViewHeight));

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

        public void OnVLPoseRequested(VLRequestEventData eventData)
        {

        }

        public void OnVLPoseResponded(VLResponseEventData eventData)
        {
            if (eventData == null)
            {
                Debug.LogError("[LogViewer] OnVLPoseResponded : eventData is null");
                return;
            }

            // VL을 성공할 경우에만 VL Pose 업데이트.
            if (eventData.Status == ResponseStatus.Success)
            {
                m_VLPosition = eventData.VLPosition;
                m_VLRotation = eventData.VLRotation.eulerAngles;
            }
        }

        public void OnLayerInfoChanged(string layerInfo)
        {
            Debug.Log($"[LogViewer] OnLayerInfoChanged : {layerInfo}");
            m_LayerInfo = layerInfo;
        }

        public void OnPoseUpdated(Matrix4x4 matrix, Matrix4x4 projMatrix, Matrix4x4 texMatrix, double ra)
        {

        }

        public void OnRelAltitudeUpdated(double value)
        {
            m_RelAltitude = value;
        }

        public void OnObjectDetected(DetectedObject detectedObject)
        {
            Debug.Log("[LogViewer] OnObjectDetected : " + detectedObject.id);
        }

        ///
        /// Debug Log 
        ///

        public void AddLogElem(LogElem logElem)
        {
            if ((int)logElem.level >= (int)LogLevel.INFO)
            {
                if (s_LogList == null)
                {
                    s_LogList = new LinkedList<LogElem>();
                }

                if (s_LogList.Count > s_MaxLogsCount)
                {
                    s_LogList.RemoveFirst();
                }
                s_LogList.AddLast(logElem);
                s_IsLogScrollUpdated = true;
            }
        }
    }

}