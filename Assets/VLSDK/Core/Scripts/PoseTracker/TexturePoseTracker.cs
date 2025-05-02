using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARCeye.Dataset;

namespace ARCeye
{
    public class TexturePoseTracker : PoseTracker
    {
        private TextureProvider m_TextureProvider;
        private ARIntrinsic m_Intrinsic = new ARIntrinsic();

        private Camera m_MainCamera;
        private DebugPreview m_DebugPreview;

        public override void OnCreate(Config config)
        {
            InitComponents();

            m_MainCamera = Camera.main;

            config.tracker.useTranslationFilter = false;
            config.tracker.useRotationFilter = false;
            config.tracker.useInterpolation = false;
        }

        private void InitComponents()
        {
            m_DebugPreview = GameObject.FindObjectOfType<DebugPreview>();
            m_TextureProvider = GameObject.FindObjectOfType<TextureProvider>();
            if (m_TextureProvider == null)
            {
                Debug.LogError("TextureProvider not found in the scene. If you want to use TexturePoseTracker, please add TextureProvider to the scene.");
                return;
            }
        }

        public override void RegisterFrameLoop()
        {
            FrameLoopRunner.Instance?.StartFrameLoop(OnFrameUpdated);
        }

        public override void UnregisterFrameLoop()
        {
            FrameLoopRunner.Instance?.StopFrameLoop();
        }

        private void OnFrameUpdated()
        {
            if (!m_IsInitialized)
                return;

            ARFrame frame = CreateARFrame();
            UpdateFrame(frame);
        }

        public ARFrame CreateARFrame()
        {
            ARFrame frame = new ARFrame();

            frame.texture = GetTexture();
            frame.localPosition = m_MainCamera.transform.localPosition;
            frame.localRotation = m_MainCamera.transform.localRotation;
            frame.intrinsic = GetIntrinsic();
            frame.projMatrix = GetProjectionMatrix();
            frame.displayMatrix = GetDisplayMatrix();

            if (m_GeoCoordProvider != null)
            {
                Vector2 geoCoord = GetGeoCoord();
                m_GeoCoordProvider.latitude = geoCoord.x;
                m_GeoCoordProvider.longitude = geoCoord.y;
            }

            m_DebugPreview.SetTexture(frame.texture);

            return frame;
        }

        private Texture GetTexture()
        {
            return m_TextureProvider.textureToSend;
        }

        private ARIntrinsic GetIntrinsic()
        {
            // TexturePoseTracker를 사용할 땐 intrinsic이 모두 0인 값을 사용한다.
            return m_Intrinsic;
        }

        private Matrix4x4 GetProjectionMatrix()
        {
            return m_MainCamera.projectionMatrix;
        }

        private Matrix4x4 GetDisplayMatrix()
        {
            return Matrix4x4.identity;
        }

        private Vector2 GetGeoCoord()
        {
            if (m_GeoCoordProvider != null)
            {
                return new Vector2(m_GeoCoordProvider.latitude, m_GeoCoordProvider.longitude);
            }
            else
            {
                return Vector2.zero;
            }
        }
    }
}