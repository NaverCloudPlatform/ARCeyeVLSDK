using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEditor.SceneManagement;

namespace ARCeye
{
    public class VLSDKMenu
    {
        private static ARCameraManager m_ARCameraManager;
        private static ARSessionOrigin m_ARSessionOrigin;

        [MenuItem("GameObject/ARC-eye/VL SDK/Create VLSDKManager")]
        private static void CreateVLSDKManager()
        {
            if(CheckIsVLSDKManagerExisting())
            {
                Debug.LogWarning("VLSDKManager가 이미 추가 되어있습니다.");
                return;
            }

            if(!CheckIsARSessionExisting())
            {
                Debug.LogError("AR Session 혹은 AR Session Origin이 추가되지 않아 VLSDKManager를 생성할 수 없습니다.");
                return;
            }

            string[] settingGUID = AssetDatabase.FindAssets("t:VLSDKSettings", new[] {"Assets/VLSDK/"});
            if(settingGUID.Length == 0) {
                Debug.LogWarning("VLSDK Settings 파일을 찾을 수 없습니다. 패키지를 다시 로드해주세요");
                return;
            }

            // Create VLSDKManager
            GameObject VLSDKManagerObject = ObjectFactory.CreateGameObject("VLSDKManager", typeof(VLSDKManager));
            VLSDKManager VLSDKManager = VLSDKManagerObject.GetComponent<VLSDKManager>();
            VLSDKManagerObject.AddComponent<TextureProvider>();
            VLSDKManagerObject.AddComponent<NetworkController>();
            VLSDKManagerObject.AddComponent<GeoCoordProvider>();

            // Assign VLSDKSettings
            string settingsPath = AssetDatabase.GUIDToAssetPath(settingGUID[0]);
            VLSDKSettings settings = (VLSDKSettings) AssetDatabase.LoadAssetAtPath(settingsPath, typeof(VLSDKSettings));            
            VLSDKManager.settings = settings;

            // Create LogViewer
            GameObject logViewerObject = ObjectFactory.CreateGameObject("LogViewer", typeof(LogViewer));
            LogViewer logViewer = logViewerObject.GetComponent<LogViewer>();

            // Register VLSDKManager events to LogViewer
            VLSDKManager.OnStateChanged.AddListener(trackerState => logViewer.OnStateChanged(trackerState));
            VLSDKManager.OnPoseUpdated.AddListener(matrix => logViewer.OnPoseUpdated(matrix));
            VLSDKManager.OnObjectDetected.AddListener(detectedObject => logViewer.OnObjectDetected(detectedObject));

            logViewerObject.transform.parent = VLSDKManagerObject.transform;

            CreatePreviewImage();

            // Assign camera components
            VLSDKManager.arCamera = m_ARCameraManager.transform;
            VLSDKManager.origin = m_ARCameraManager.transform;
        }

        private static bool CheckIsVLSDKManagerExisting()
        {
            var VLSDKManager = GameObject.FindObjectOfType<VLSDKManager>();
            return VLSDKManager != null;
        }

        private static bool CheckIsARSessionExisting()
        {
            var arSession = GameObject.FindObjectOfType<ARSession>();
            m_ARSessionOrigin = GameObject.FindObjectOfType<ARSessionOrigin>();
            m_ARCameraManager = GameObject.FindObjectOfType<ARCameraManager>();

            return arSession != null && m_ARSessionOrigin != null;
        }

        private static void CreatePreviewImage()
        {
            Canvas previewCanvas = m_ARCameraManager.GetComponentInChildren<Canvas>();

            // Add debug preview texture.
            if(previewCanvas == null)
            {
                previewCanvas = ObjectFactory.CreateGameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
                previewCanvas.transform.SetParent(m_ARCameraManager.transform);
            }

            previewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            previewCanvas.worldCamera = m_ARCameraManager.GetComponent<Camera>();
            previewCanvas.planeDistance = previewCanvas.worldCamera.farClipPlane - 0.1f;

            DebugPreview debugPreview = previewCanvas.GetComponentInChildren<DebugPreview>();
            if(debugPreview == null)
            {
                GameObject debugPreviewObject = ObjectFactory.CreateGameObject("DebugPreview", typeof(DebugPreview));
                debugPreviewObject.transform.SetParent(previewCanvas.transform);
                
                RectTransform debugPreviewRT = debugPreviewObject.AddComponent<RectTransform>();

                debugPreviewRT.anchorMin = Vector2.zero;
                debugPreviewRT.anchorMax = Vector2.one;
                debugPreviewRT.offsetMin = Vector3.zero;
                debugPreviewRT.offsetMax = Vector3.zero;

                debugPreviewRT.localPosition = Vector3.zero;
                debugPreviewRT.localScale = Vector3.one;
            }
        }
    }
}