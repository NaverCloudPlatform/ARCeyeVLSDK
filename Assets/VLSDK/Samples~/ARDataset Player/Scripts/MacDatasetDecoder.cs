using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using ARDecode.Interop;

namespace ARCeye.Dataset
{
    public class MacDatasetDecoder : DatasetDecoder
    {
#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "ARDecodeSDK";
#endif

        [DllImport(dll)]
        private static extern bool InitDecoder(string path, out int width, out int height);

        [DllImport(dll)]
        private static extern bool DecodeNextSyncedFrame(double seconds, out double outTimestamp, out IntPtr outFrame);


        // private Texture2D m_VideoTexture;
        private Texture2D m_VideoTexture;


        protected override void InitDecoder(string filePath)
        {
            if (!InitDecoder(filePath, out m_Width, out m_Height))
            {
                Debug.LogError("Failed to initialize decoder.");
                return;
            }

            m_IsInitialized = true;
        }

        protected override void CreatePreviewTexture()
        {
            m_VideoTexture = new Texture2D(m_Width, m_Height, TextureFormat.BGRA32, false);
            SetUnityTexture(m_VideoTexture.GetNativeTexturePtr());
        }

        protected override void ReleaseVideoTexture()
        {
            if (m_VideoTexture != null)
            {
                GameObject.Destroy(m_VideoTexture);
                m_VideoTexture = null;
            }
        }

        public override Texture GetPreviewTexture()
        {
            if (m_VideoTexture == null)
            {
                Debug.LogError("Video texture is not created.");
                return null;
            }

            return m_VideoTexture;
        }

        public override FrameData GetNextFrame()
        {
            double playbackTime = GetCurrentPlaybackTime();

            if (DecodeNextSyncedFrame(playbackTime, out double frameTimestamp, out IntPtr framePtr))
            {
                m_Progress = (float)(frameTimestamp / m_TotalDuration);

                if (framePtr != IntPtr.Zero)
                {
                    return ANFrame.ConvertToFrameData(framePtr);
                }
                else
                {
                    Debug.LogError("Frame pointer is null.");
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}