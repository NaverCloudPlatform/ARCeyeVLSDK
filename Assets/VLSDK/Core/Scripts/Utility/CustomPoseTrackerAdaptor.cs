using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    public class CustomPoseTrackerAdaptor : MonoBehaviour
    {
        [SerializeField]
        private bool m_UseCustomEditorPoseTracker = false;
        public bool UseCustomEditorPoseTracker
        {
            get => m_UseCustomEditorPoseTracker;
            set => m_UseCustomEditorPoseTracker = value;
        }

        [SerializeField]
        private string m_EditorPoseTrackerClassName;
        public string EditorPoseTrackerClassName
        {
            get => m_EditorPoseTrackerClassName;
            set => m_EditorPoseTrackerClassName = value;
        }

        private PoseTracker m_CustomEditorPoseTracker = null;
        public PoseTracker CustomEditorPoseTracker
        {
            get => m_CustomEditorPoseTracker;
            set => m_CustomEditorPoseTracker = value;
        }

        [SerializeField]
        private bool m_UseCustomDevicePoseTracker = false;
        public bool UseCustomDevicePoseTracker
        {
            get => m_UseCustomDevicePoseTracker;
            set => m_UseCustomDevicePoseTracker = value;
        }

        [SerializeField]
        private string m_DevicePoseTrackerClassName;
        public string DevicePoseTrackerClassName
        {
            get => m_DevicePoseTrackerClassName;
            set => m_DevicePoseTrackerClassName = value;
        }

        private PoseTracker m_CustomDevicePoseTracker = null;
        public PoseTracker CustomDevicePoseTracker
        {
            get => m_CustomDevicePoseTracker;
            set => m_CustomDevicePoseTracker = value;
        }


        public void Initialize()
        {
            CreateEditorPoseTracker(m_EditorPoseTrackerClassName);
            CreateDevicePoseTracker(m_DevicePoseTrackerClassName);
        }

        private void CreateEditorPoseTracker(string className)
        {
            if (m_UseCustomEditorPoseTracker)
            {
                CreatePoseTracker(className, ref m_CustomEditorPoseTracker);
            }
        }

        private void CreateDevicePoseTracker(string className)
        {
            if (m_UseCustomDevicePoseTracker)
            {
                CreatePoseTracker(className, ref m_CustomDevicePoseTracker);
            }
        }

        private void CreatePoseTracker(string className, ref PoseTracker poseTracker)
        {
            if (string.IsNullOrEmpty(className))
            {
                Debug.LogWarning("A class name of CustomPoseTracker is empty!");
                return;
            }

            // 해당 클래스가 존재하는지 확인.
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(PoseTracker)))
                .Where(type => type.FullName == className)
                .FirstOrDefault();

            if (type == null)
            {
                Debug.LogError($"CustomPoseTracker class `{className}` does not exist!");
                return;
            }

            // 해당 클래스의 인스턴스를 생성.
            poseTracker = (PoseTracker)Activator.CreateInstance(type);
            if (poseTracker == null)
            {
                Debug.LogError($"Failed to create an instance of CustomPoseTracker class `{className}`!");
                return;
            }
        }
    }
}