#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ARCeye
{
    [CustomEditor(typeof(VLSDKManager))]
    public class VLSDKManagerEditor : Editor
    {
        private VLSDKManager m_VLSDKManager;

        void OnEnable()
        {
            m_VLSDKManager = (VLSDKManager) target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}

#endif