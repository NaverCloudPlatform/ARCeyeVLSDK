using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ARCeye.Dataset;
using System;

namespace ARCeye
{
    public class ARDatasetPoseTracker : PoseTracker
    {
        private ARDatasetManager m_ARDatasetManager;
        private Camera m_MainCamera;


        public override void OnCreate(Config config)
        {
            Debug.Log("Class Name: " + this.GetType().Name);

            NativeLogger.DebugLog(ARCeye.LogLevel.INFO, "Initialize EditorPoseTracker");

            m_MainCamera = Camera.main;

            InitDatasetManager();
            InitComponents();

            PlayDatasetManager();
        }

        private void InitDatasetManager()
        {
            if (m_ARDatasetManager == null)
            {
                m_ARDatasetManager = GameObject.FindObjectOfType<ARDatasetManager>();
            }

            if (m_ARDatasetManager != null)
            {
                if (m_ARDatasetManager.datasetPath == "")
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
            m_GeoCoordProvider = GameObject.FindObjectOfType<GeoCoordProvider>();
        }

        private void PlayDatasetManager()
        {
            m_ARDatasetManager.frameReceived += OnPreviewUpdated;
            m_ARDatasetManager.Play();
        }

        public override void RegisterFrameLoop()
        {
            m_ARDatasetManager.frameReceived += OnCameraFrameReceived;
        }

        public override void UnregisterFrameLoop()
        {
            m_ARDatasetManager.frameReceived -= OnCameraFrameReceived;
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

            ARFrame frame = CreateARFrame(frameData);
            UpdateFrame(frame);
        }

        protected ARFrame CreateARFrame(FrameData frameData)
        {
            ARFrame frame = new ARFrame();

            // Camera preview.
            m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture);
            frame.texture = frameTexture as Texture2D;

            // Camera model matrix.
            Matrix4x4 modelMatrix = frameData.modelMatrix;
            frame.localPosition = modelMatrix.GetColumn(3);
            frame.localRotation = Quaternion.LookRotation(modelMatrix.GetColumn(2), modelMatrix.GetColumn(1));

            // Projection matrix.
            Matrix4x4 projMatrix = frameData.projMatrix;
            frame.projMatrix = projMatrix;

            // Camera intrinsic.
            frame.intrinsic = new ARIntrinsic(
                frameData.intrinsic.fx,
                frameData.intrinsic.fy,
                frameData.intrinsic.cx,
                frameData.intrinsic.cy
            );

            // Display matrix.
            frame.displayMatrix = frameData.transMatrix;

            // Additional data.
            m_CurrRelAltitude = frameData.relAltitude;

            return frame;
        }
    }
}