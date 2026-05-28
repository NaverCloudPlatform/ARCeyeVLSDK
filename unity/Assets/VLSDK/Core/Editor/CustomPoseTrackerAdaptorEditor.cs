#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ARCeye
{
    [CustomEditor(typeof(CustomPoseTrackerAdaptor))]
    public class CustomPoseTrackerAdaptorEditor : Editor
    {
        private CustomPoseTrackerAdaptor m_CustomPoseTrackerAdaptor;

        private SerializedProperty m_UseCustomEditorPoseTrackerProp;
        private SerializedProperty m_EditorPoseTrackerClassNameProp;
        private SerializedProperty m_UseCustomDevicePoseTrackerProp;
        private SerializedProperty m_DevicePoseTrackerClassNameProp;

        void OnEnable()
        {
            m_CustomPoseTrackerAdaptor = (CustomPoseTrackerAdaptor)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_UseCustomEditorPoseTrackerProp = serializedObject.FindProperty("m_UseCustomEditorPoseTracker");
            m_EditorPoseTrackerClassNameProp = serializedObject.FindProperty("m_EditorPoseTrackerClassName");
            m_UseCustomDevicePoseTrackerProp = serializedObject.FindProperty("m_UseCustomDevicePoseTracker");
            m_DevicePoseTrackerClassNameProp = serializedObject.FindProperty("m_DevicePoseTrackerClassName");

            EditorGUILayout.PropertyField(m_UseCustomEditorPoseTrackerProp);

            if (m_UseCustomEditorPoseTrackerProp.boolValue)
            {
                DrawPoseTrackerDropdown("Editor PoseTracker", m_EditorPoseTrackerClassNameProp);
            }

            EditorGUILayout.PropertyField(m_UseCustomDevicePoseTrackerProp);

            if (m_UseCustomDevicePoseTrackerProp.boolValue)
            {
                DrawPoseTrackerDropdown("Device PoseTracker", m_DevicePoseTrackerClassNameProp);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPoseTrackerDropdown(string label, SerializedProperty property)
        {
            // PoseTracker를 상속받는 클래스들을 드롭다운으로 출력.
            var poseTrackerTypes = new List<string>();
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(PoseTracker)) && !type.IsAbstract)
                    {
                        poseTrackerTypes.Add(type.FullName);
                    }
                }
            }

            int selectedIndex = Mathf.Max(0, poseTrackerTypes.IndexOf(property.stringValue));
            selectedIndex = EditorGUILayout.Popup(label, selectedIndex, poseTrackerTypes.ToArray());
            property.stringValue = poseTrackerTypes.Count > 0 ? poseTrackerTypes[selectedIndex] : string.Empty;
        }
    }
}
#endif