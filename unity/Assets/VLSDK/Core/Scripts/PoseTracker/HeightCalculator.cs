using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARCeye
{
    public class HeightCalculator : MonoBehaviour
    {
        private ARPlaneManager m_ARPlaneManager;
        private ARRaycastManager m_ARRaycastManager;

        private float m_RealHeight;
        public float realHeight => m_RealHeight;

        private Camera m_MainCamera;

        private bool m_Started = false;

        public void Initialize()
        {
            m_ARPlaneManager = GameObject.FindAnyObjectByType<ARPlaneManager>();
            if (m_ARPlaneManager == null)
            {
                m_ARPlaneManager = gameObject.AddComponent<ARPlaneManager>();
            }

            m_ARRaycastManager = GameObject.FindAnyObjectByType<ARRaycastManager>();
            if (m_ARRaycastManager == null)
            {
                m_ARRaycastManager = gameObject.AddComponent<ARRaycastManager>();
            }

            m_MainCamera = Camera.main;

            CreateARPlane();

            StartCoroutine(DetectCenterPlane());
        }

        private void CreateARPlane()
        {
            GameObject planeObject = new GameObject("ARPlane");
            ARPlane plane = planeObject.AddComponent<ARPlane>();
            plane.destroyOnRemoval = true;
            plane.vertexChangedThreshold = 0.01f;

            MeshCollider meshCollider = planeObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            meshCollider.providesContacts = false;

            planeObject.AddComponent<MeshFilter>();

            m_ARPlaneManager.planePrefab = planeObject;
        }

        private IEnumerator DetectCenterPlane()
        {
            float centerX = Screen.width / 2.0f;
            float centerY = Screen.height / 4.0f;
            Vector2 center = new Vector2(centerX, centerY);

            // 0.1초 간격으로 화면의 중앙 아래에서 ray 발사. 그 결과를 다른 컴포넌트에서 사용할 수 있도록 한다.
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                if (!m_Started)
                {
                    continue;
                }

                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (m_ARRaycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
                {
                    var localCameraHeight = m_MainCamera.transform.localPosition.y;
                    var planeHeight = 0f;

                    if (hits.Count > 0)
                    {
                        planeHeight = hits[0].pose.position.y;
                    }

                    m_RealHeight = localCameraHeight - planeHeight;
                }
            }
        }

        private void Start()
        {
            m_Started = true;
        }

        private void Stop()
        {
            m_Started = false;
        }
    }
}