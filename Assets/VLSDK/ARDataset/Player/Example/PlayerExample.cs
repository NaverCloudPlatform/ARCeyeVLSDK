using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye.Dataset
{
    public class PlayerExample : MonoBehaviour
    {
        [SerializeField]
        private ARDatasetManager m_ARDatasetManager;

        private Camera m_MainCamera;


        private void Start()
        {
            m_MainCamera = Camera.main;

            RegisterFrameLoop();
        }

        public void RegisterFrameLoop()
        {
            if(m_ARDatasetManager != null)
            {
                m_ARDatasetManager.frameReceived += OnCameraFrameReceived;
                m_ARDatasetManager.Play();
            }
        }

        public void UnregisterFrameLoop()
        {
            if(m_ARDatasetManager != null)
            {
                m_ARDatasetManager.frameReceived -= OnCameraFrameReceived;
                m_ARDatasetManager.Pause();
            }
        }

        private void OnCameraFrameReceived(FrameData frameData)
        {
            m_MainCamera.transform.localPosition = frameData.modelMatrix.GetColumn(3);
            m_MainCamera.transform.localRotation = Quaternion.LookRotation(frameData.modelMatrix.GetColumn(2), frameData.modelMatrix.GetColumn(1));

            Texture previewTexture;
            AcquireRequestedTexture(out previewTexture);
            m_ARDatasetManager.SetPreviewTexture(previewTexture);
        }

        private void AcquireRequestedTexture(out Texture texture)
        {
            if(!m_ARDatasetManager.TryAcquireFrameImage(out Texture frameTexture)) 
            {
                Debug.LogError("Failed to load preview texture");
                texture = null;
                return;
            }

            texture = frameTexture;
        }
    }
}