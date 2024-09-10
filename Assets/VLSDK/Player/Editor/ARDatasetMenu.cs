using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
#if ARFOUNDATION_VERSION_5
using UnityEngine.InputSystem.XR;
#endif

namespace ARCeye.Dataset
{
    public class ARDatasetMenu
    {
        private static ARCameraManager m_ARCameraManager;

        
        [MenuItem("GameObject/ARC-eye/Dataset/Create ARDatasetManager")]
        private static void CreateARDatasetManager()
        {
            if(CheckIsARDatasetManagerExisting())
            {
                Debug.LogWarning("ARDatasetManager가 이미 추가 되어있습니다.");
                return;
            }

            if(!CheckIsARSessionExisting())
            {
                Debug.LogError("AR 시스템이 초기화 되지 않아 ARDatasetManager를 생성할 수 없습니다.");
                return;
            }

            // ARDatasetManager 오브젝트 생성.
            ARDatasetManager arDatasetManager = CreateARDatasetManagerObject();

#if ARFOUNDATION_VERSION_5
            // Tracker의 Update Type 변경.
            ARCameraManager arCameraManager = GameObject.FindObjectOfType<ARCameraManager>();

            TrackedPoseDriver trackedPoseDriver = arCameraManager.GetComponent<TrackedPoseDriver>();
            if(trackedPoseDriver != null)
            {
                trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
            }
#endif

            // Debug Preview 추가.
            CreatePreviewImage();
        }

        private static bool CheckIsARDatasetManagerExisting()
        {
            var arDatasetManager = GameObject.FindObjectOfType<ARDatasetManager>();
            return arDatasetManager != null;
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

        private static ARDatasetManager CreateARDatasetManagerObject()
        {
            GameObject ARDatasetManager;
            
            ARDatasetManager = ObjectFactory.CreateGameObject("ARDatasetManager", typeof(ARDatasetManager));

            return ARDatasetManager.GetComponent<ARDatasetManager>();
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
