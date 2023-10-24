using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye 
{
    public class VOTTextGenerator : MonoBehaviour
    {
        [SerializeField]
        private Transform m_Root;

        [SerializeField]
        private GameObject m_VOTTextPrefab;

        private Dictionary<string, Transform> m_InstanceByKey = new Dictionary<string, Transform>();


        public void OnObjectDetected(DetectedObject detectedObject) {
            string key = detectedObject.id;

            if(m_InstanceByKey.ContainsKey(key)) {
                Transform t = m_InstanceByKey[key].transform;
                UpdateTransformation(t, detectedObject);
            } else {
                Transform t = Instantiate(m_VOTTextPrefab).transform;
                t.SetParent(m_Root);

                var tmpText = t.GetComponentInChildren<TMPro.TextMeshPro>();
                tmpText.text = key;
                
                UpdateTransformation(t, detectedObject);

                m_InstanceByKey.Add(key, t);
            }
        }

        private void UpdateTransformation(Transform t, DetectedObject detectedObject) {
            t.position = detectedObject.position;
            t.rotation = detectedObject.rotation;
            t.localScale = detectedObject.scale;
        }
    }

}