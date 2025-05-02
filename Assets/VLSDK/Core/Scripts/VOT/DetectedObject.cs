using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    [System.Serializable]
    public class DetectedObject
    {
        // 인식 된 물체의 고유 id.
        private string m_Id;
        public string id
        {
            get => m_Id;
        }

        // 인식 된 물체의 WC 상에서의 model matrix.
        private Vector3 m_Position;
        public Vector3 position
        {
            get => m_Position;
        }

        private Quaternion m_Rotation;
        public Quaternion rotation
        {
            get => m_Rotation;
        }

        // 인식 된 물체의 크기.
        private Vector3 m_Scale;
        public Vector3 scale
        {
            get => m_Scale;
        }

        public DetectedObject(string i, Vector3 p, Quaternion r, Vector3 s)
        {
            m_Id = i;
            m_Position = p;
            m_Rotation = r;
            m_Scale = s;
        }
    }
}