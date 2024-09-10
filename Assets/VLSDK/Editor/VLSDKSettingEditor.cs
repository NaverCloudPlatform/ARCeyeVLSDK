#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ARCeye
{
    [CustomEditor(typeof(VLSDKSettings))]
    public class VLSDKSettingsEditor : Editor
    {
        private VLSDKSettings m_VLSDKSettings;

        private SerializedProperty m_URLListProp;
        private SerializedProperty m_GPSGuideProp;
        private SerializedProperty m_LocationGeoJsonProp;
        private SerializedProperty m_VLIntervalInitialProp;
        private SerializedProperty m_VLIntervalPassedProp;
        private SerializedProperty m_VLQualityProp;
        private SerializedProperty m_ShowVLPoseProp;
        private SerializedProperty m_LogLevelProp;


        void OnEnable()
        {
            m_VLSDKSettings = (VLSDKSettings) target;

            m_URLListProp = serializedObject.FindProperty("m_URLList");
            m_GPSGuideProp = serializedObject.FindProperty("m_GPSGuide");
            m_LocationGeoJsonProp = serializedObject.FindProperty("m_LocationGeoJson");
            m_VLIntervalInitialProp = serializedObject.FindProperty("m_VLIntervalInitial");
            m_VLIntervalPassedProp = serializedObject.FindProperty("m_VLIntervalPassed");
            m_VLQualityProp = serializedObject.FindProperty("m_VLQuality");
            m_ShowVLPoseProp = serializedObject.FindProperty("m_ShowVLPose");
            m_LogLevelProp = serializedObject.FindProperty("m_LogLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLogo();

            DrawVLURLList();
            DrawGPSGuide();

            if(m_VLSDKSettings.GPSGuide)
            {
                DrawLocationGeoJsonField();
            }

            DrawVLInterval();
            DrawVLQuality();
            DrawShowVLPose();
            DrawLogLevel();
        }

        private void DrawLogo()
        {
            EditorGUILayout.Space();

            GUIStyle style = new GUIStyle();
            style.fixedHeight = 30;
            style.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label(Resources.Load("Sprites/Logo") as Texture, style, GUILayout.ExpandWidth(true));

            EditorGUILayout.Space();
        }

        private void DrawVLURLList()
        {
            EditorGUILayout.PropertyField(m_URLListProp);

            EditorUtility.SetDirty(m_URLListProp.serializedObject.targetObject);
            m_URLListProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawGPSGuide()
        {
            EditorGUILayout.PropertyField(m_GPSGuideProp);

            EditorUtility.SetDirty(m_GPSGuideProp.serializedObject.targetObject);
            m_GPSGuideProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawLocationGeoJsonField()
        {
            EditorGUILayout.PropertyField(m_LocationGeoJsonProp);

            EditorUtility.SetDirty(m_LocationGeoJsonProp.serializedObject.targetObject);
            m_LocationGeoJsonProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawVLInterval()
        {
            EditorGUILayout.PropertyField(m_VLIntervalInitialProp);
            EditorGUILayout.PropertyField(m_VLIntervalPassedProp);

            EditorUtility.SetDirty(m_VLIntervalInitialProp.serializedObject.targetObject);
            EditorUtility.SetDirty(m_VLIntervalPassedProp.serializedObject.targetObject);

            m_VLIntervalInitialProp.serializedObject.ApplyModifiedProperties();
            m_VLIntervalPassedProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawVLQuality()
        {
            EditorGUILayout.PropertyField(m_VLQualityProp);

            EditorUtility.SetDirty(m_VLQualityProp.serializedObject.targetObject);
            m_VLQualityProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawShowVLPose()
        {
            EditorGUILayout.PropertyField(m_ShowVLPoseProp);

            EditorUtility.SetDirty(m_ShowVLPoseProp.serializedObject.targetObject);
            m_ShowVLPoseProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawLogLevel()
        {
            EditorGUILayout.PropertyField(m_LogLevelProp);

            EditorUtility.SetDirty(m_LogLevelProp.serializedObject.targetObject);
            m_LogLevelProp.serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif