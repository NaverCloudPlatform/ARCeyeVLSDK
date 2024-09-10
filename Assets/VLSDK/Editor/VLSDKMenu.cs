using System;
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
                Debug.LogError("AR 시스템이 초기화 되지 않아 VLSDKManager를 생성할 수 없습니다.");
                return;
            }

            string[] settingGUIDInAssets   = AssetDatabase.FindAssets("t:VLSDKSettings");

            if(settingGUIDInAssets.Length == 0) {
                Debug.LogWarning("VLSDK Settings 파일을 찾을 수 없습니다. 패키지를 다시 로드해주세요");
                return;
            }

            // Create VLSDKManager
            VLSDKManager VLSDKManager = VLSDKManagerFactory.CreateVLSDKManager();

            // Assign VLSDKSettings
            string settingGUID = settingGUIDInAssets[0];
            string settingsPath = AssetDatabase.GUIDToAssetPath(settingGUID);
            VLSDKSettings settings = (VLSDKSettings) AssetDatabase.LoadAssetAtPath(settingsPath, typeof(VLSDKSettings));            
            VLSDKManager.settings = settings;

            // Create LogViewer
            GameObject logViewerObject = ObjectFactory.CreateGameObject("LogViewer", typeof(LogViewer));
            LogViewer logViewer = logViewerObject.GetComponent<LogViewer>();

            // Register VLSDKManager events to LogViewer
            VLSDKManager.OnStateChanged.AddListener(trackerState => logViewer.OnStateChanged(trackerState));
            VLSDKManager.OnPoseUpdated.AddListener((matrix, proj, tex) => logViewer.OnPoseUpdated(matrix, proj, tex));

            logViewerObject.transform.parent = VLSDKManager.gameObject.transform;

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
            System.Type arSessionType       = GetTypeFromAssemblies("UnityEngine.XR.ARFoundation.ARSession");
            System.Type arSessionOriginType = GetTypeFromAssemblies("UnityEngine.XR.ARFoundation.ARSessionOrigin");
            System.Type arCameraManagerType = GetTypeFromAssemblies("UnityEngine.XR.ARFoundation.ARCameraManager");

            m_ARCameraManager = GameObject.FindObjectOfType<ARCameraManager>();

            var obj1 = arSessionType != null ? GameObject.FindObjectOfType(arSessionType) : null;
            var obj2 = arSessionOriginType != null ? GameObject.FindObjectOfType(arSessionOriginType) : null;
            var obj3 = arCameraManagerType != null ? GameObject.FindObjectOfType(arCameraManagerType) : null;

            return obj1 != null && (obj2 != null || obj3 != null);
        }

        public static Type GetTypeFromAssemblies( string TypeName )
        {
            var type = Type.GetType( TypeName );
            if( type != null )
                return type;

            var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach( var assemblyName in referencedAssemblies )
            {
                var assembly = System.Reflection.Assembly.Load( assemblyName );
                if( assembly != null )
                {
                    type = assembly.GetType( TypeName );
                    if( type != null )
                        return type;
                }
            }

            return null;
        }
    }
}