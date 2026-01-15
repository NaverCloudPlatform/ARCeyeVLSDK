#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

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
        private SerializedProperty m_InitialPoseCountProp;
        private SerializedProperty m_InitialPoseDegreeProp;
        private SerializedProperty m_FailureCountToNotRecognizedProp;
        private SerializedProperty m_FailureCountToVLFailProp;
        private SerializedProperty m_FailureCountToResetProp;
        private SerializedProperty m_FaceBlurringProp;
        private SerializedProperty m_ShowVLPoseProp;
        private SerializedProperty m_LogLevelProp;


        private GUIContent m_LabelVLCount = new GUIContent("VL Count");
        private GUIContent m_LabelYawDegree = new GUIContent("Yaw Degree");
        private GUIContent m_LabelVLNotRecognized = new GUIContent("to NotRecognized");
        private GUIContent m_LabelVLFail = new GUIContent("to VLFail");
        private GUIContent m_LabelVLReset = new GUIContent("to Initial");


        void OnEnable()
        {
            m_VLSDKSettings = (VLSDKSettings)target;

            m_URLListProp = serializedObject.FindProperty("m_URLList");
            m_GPSGuideProp = serializedObject.FindProperty("m_GPSGuide");
            m_LocationGeoJsonProp = serializedObject.FindProperty("m_LocationGeoJson");
            m_VLIntervalInitialProp = serializedObject.FindProperty("m_VLIntervalInitial");
            m_VLIntervalPassedProp = serializedObject.FindProperty("m_VLIntervalPassed");
            m_VLQualityProp = serializedObject.FindProperty("m_VLQuality");
            m_InitialPoseCountProp = serializedObject.FindProperty("m_InitialPoseCount");
            m_InitialPoseDegreeProp = serializedObject.FindProperty("m_InitialPoseDegree");
            m_FailureCountToNotRecognizedProp = serializedObject.FindProperty("m_FailureCountToNotRecognized");
            m_FailureCountToVLFailProp = serializedObject.FindProperty("m_FailureCountToFail");
            m_FailureCountToResetProp = serializedObject.FindProperty("m_FailureCountToReset");
            m_FaceBlurringProp = serializedObject.FindProperty("m_FaceBlurring");
            m_ShowVLPoseProp = serializedObject.FindProperty("m_ShowVLPose");
            m_LogLevelProp = serializedObject.FindProperty("m_LogLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLogo();

            DrawLabel("VL Configs", () =>
            {
                DrawVLURLList();
                DrawVLInterval();
                DrawVLQuality();
                DrawGPSGuide();
                DrawFaceBlurring();
            });

            DrawLabel("Initial Pose Calculation", () =>
            {
                DrawInitialPoseInfo();
            });

            DrawFailureCounts();
            DrawDebugTools();
        }

        private void DrawLabel(string label, UnityAction action)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            action?.Invoke();
            EditorGUI.indentLevel--;
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

            if (m_VLSDKSettings.GPSGuide)
            {
                DrawLocationGeoJsonField();
            }
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

        private void DrawInitialPoseInfo()
        {
            EditorGUILayout.PropertyField(m_InitialPoseCountProp, m_LabelVLCount);
            EditorGUILayout.PropertyField(m_InitialPoseDegreeProp, m_LabelYawDegree);

            EditorUtility.SetDirty(m_InitialPoseCountProp.serializedObject.targetObject);
            EditorUtility.SetDirty(m_InitialPoseDegreeProp.serializedObject.targetObject);

            m_InitialPoseCountProp.serializedObject.ApplyModifiedProperties();
            m_InitialPoseDegreeProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawFailureCounts()
        {
            EditorGUILayout.LabelField("Failure Counts", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_FailureCountToNotRecognizedProp, m_LabelVLNotRecognized);
            EditorGUILayout.PropertyField(m_FailureCountToVLFailProp, m_LabelVLFail);
            EditorGUILayout.PropertyField(m_FailureCountToResetProp, m_LabelVLReset);

            EditorGUI.indentLevel--;

            EditorUtility.SetDirty(m_FailureCountToNotRecognizedProp.serializedObject.targetObject);
            EditorUtility.SetDirty(m_FailureCountToVLFailProp.serializedObject.targetObject);
            EditorUtility.SetDirty(m_FailureCountToResetProp.serializedObject.targetObject);

            m_FailureCountToNotRecognizedProp.serializedObject.ApplyModifiedProperties();
            m_FailureCountToVLFailProp.serializedObject.ApplyModifiedProperties();
            m_FailureCountToResetProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawFaceBlurring()
        {
            EditorGUILayout.PropertyField(m_FaceBlurringProp);

            EditorUtility.SetDirty(m_FaceBlurringProp.serializedObject.targetObject);
            m_FaceBlurringProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawDebugTools()
        {
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_ShowVLPoseProp);
            EditorGUILayout.PropertyField(m_LogLevelProp);

            EditorGUI.indentLevel--;

            EditorUtility.SetDirty(m_ShowVLPoseProp.serializedObject.targetObject);
            EditorUtility.SetDirty(m_LogLevelProp.serializedObject.targetObject);

            m_ShowVLPoseProp.serializedObject.ApplyModifiedProperties();
            m_LogLevelProp.serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif