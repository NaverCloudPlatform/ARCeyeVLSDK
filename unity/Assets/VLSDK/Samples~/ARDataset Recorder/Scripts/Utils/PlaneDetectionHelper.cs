using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARCeye.Dataset.Recorder
{
    public class PlaneDetectionHelper : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent<Vector3> m_OnPlaneTouched;

        [SerializeField]
        private UnityEvent<Vector3> m_OnPlaneDragged;

        private ARPlaneManager m_ARPlaneManager;
        private ARRaycastManager m_ARRaycastManager;

        private Vector3 m_CenterPoint;
        public Vector3 centerPoint => m_CenterPoint;


        private void Awake()
        {
            m_ARPlaneManager = FindAnyObjectByType<ARPlaneManager>();
            m_ARRaycastManager = FindAnyObjectByType<ARRaycastManager>();

            StartCoroutine(DetectCenterPlane());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator DetectCenterPlane()
        {
            float centerX = Screen.width / 2.0f;
            float centerY = Screen.height / 2.0f;
            Vector2 center = new Vector2(centerX, centerY);

            // 0.1초 간격으로 화면의 중앙으로 ray 발사. 그 결과를 다른 컴포넌트에서 사용할 수 있도록 한다.
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (m_ARRaycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
                {
                    if (hits.Count > 0)
                    {
                        m_CenterPoint = hits[0].pose.position;
                    }
                }
            }
        }

        public void Reset()
        {
            m_CenterPoint = Vector3.zero;
        }
    }
}