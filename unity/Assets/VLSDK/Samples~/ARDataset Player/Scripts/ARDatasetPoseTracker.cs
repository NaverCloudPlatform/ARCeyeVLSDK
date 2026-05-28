using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye.Dataset
{
    public class ARDatasetPoseTracker : PoseTracker
    {
        private ARDatasetManager m_ARDatasetManager;
        private Camera m_MainCamera;
        private FrameData m_LastFrameData;

        public override void OnCreate(Config config)
        {
            Debug.Log("Class Name: " + this.GetType().Name);

            NativeLogger.DebugLog(ARCeye.LogLevel.INFO, "Initialize EditorPoseTracker");

            m_MainCamera = Camera.main;

            InitDatasetManager();
            InitComponents();

            DisableARFoundation();

            PlayDatasetManager();
        }

        private void InitDatasetManager()
        {
            if (m_ARDatasetManager == null)
            {
                m_ARDatasetManager = GameObject.FindAnyObjectByType<ARDatasetManager>();
            }

            if (m_ARDatasetManager != null)
            {
                if (m_ARDatasetManager.DatasetPath == "")
                {
                    Debug.LogError("Dataset path is empty!");
                }
            }
            else
            {
                Debug.LogError("Failed to find ARDatasetManager.");
            }
        }

        private void InitComponents()
        {
            m_GeoCoordProvider = GameObject.FindAnyObjectByType<GeoCoordProvider>();
        }

        private void DisableARFoundation()
        {
            var backgroundType = Type.GetType("UnityEngine.XR.ARFoundation.ARCameraBackground, Unity.XR.ARFoundation");
            var poseDriverType = Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem");

            var arCameraBackground = GameObject.FindAnyObjectByType(backgroundType);
            var trackedPoseDriver = GameObject.FindAnyObjectByType(poseDriverType);

            if (arCameraBackground != null)
            {
                GameObject.Destroy(arCameraBackground);
            }

            if (trackedPoseDriver != null)
            {
                GameObject.Destroy(trackedPoseDriver);
            }
        }

        private void PlayDatasetManager()
        {
            m_ARDatasetManager.FrameReceived += OnPreviewUpdated;
            m_ARDatasetManager.Play();
        }

        public override void RegisterFrameLoop()
        {
            m_ARDatasetManager.FrameReceived += OnCameraFrameReceived;
        }

        public override void UnregisterFrameLoop()
        {
            m_ARDatasetManager.FrameReceived -= OnCameraFrameReceived;
        }

        private void OnPreviewUpdated(FrameData frameData)
        {
            if (!m_IsInitialized)
                return;

            if (!m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture))
                return;

            m_MainCamera.transform.localPosition = frameData.modelMatrix.GetColumn(3);
            m_MainCamera.transform.localRotation = Quaternion.LookRotation(frameData.modelMatrix.GetColumn(2), frameData.modelMatrix.GetColumn(1));
            m_MainCamera.fieldOfView = CalculateFOV(frameData.projMatrix);

            m_ARDatasetManager.SetPreviewTexture(frameTexture);
        }

        private float CalculateFOV(Matrix4x4 projMatrix)
        {
            float f = projMatrix.m11;
            float verticalFOV = 2.0f * Mathf.Atan(1.0f / f) * Mathf.Rad2Deg;
            return verticalFOV;
        }

        private void OnCameraFrameReceived(FrameData frameData)
        {
            if (!m_IsInitialized)
                return;

            if (m_GeoCoordProvider)
            {
                m_GeoCoordProvider.latitude = (float)frameData.latitude;
                m_GeoCoordProvider.longitude = (float)frameData.longitude;
            }

            m_LastFrameData = frameData;

            ARFrame frame = CreateARFrame();
            UpdateFrame(frame);
        }

        protected override ARFrame CreateARFrame()
        {
            ARFrame frame = new ARFrame();

            // Camera preview.
            m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture);
            frame.texture = frameTexture;

            // Camera model matrix.
            Matrix4x4 modelMatrix = m_LastFrameData.modelMatrix;
            frame.localPosition = modelMatrix.GetColumn(3);
            frame.localRotation = Quaternion.LookRotation(modelMatrix.GetColumn(2), modelMatrix.GetColumn(1));

            // Projection matrix.
            Matrix4x4 projMatrix = m_LastFrameData.projMatrix;
            frame.projMatrix = projMatrix;

            // Camera intrinsic.
            frame.intrinsic = new ARIntrinsic(
                m_LastFrameData.intrinsic.fx,
                m_LastFrameData.intrinsic.fy,
                m_LastFrameData.intrinsic.cx,
                m_LastFrameData.intrinsic.cy
            );

            // Display matrix.
            frame.displayMatrix = m_LastFrameData.transMatrix;

            // Additional data.
            m_CurrRelAltitude = m_LastFrameData.relAltitude;

            return frame;
        }
    }
}