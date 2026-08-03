using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using ARDecode.Interop;

namespace ARCeye.Dataset
{
    public class WinDatasetDecoder : DatasetDecoder
    {
#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "ARDecodeSDK";
#endif
        [DllImport(dll, CharSet = CharSet.Unicode)]
        private static extern bool InitDecoder(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            bool bIsQT,
            [MarshalAs(UnmanagedType.LPStr)] string software,
            out int width,
            out int height);

        [DllImport(dll)]
        private static extern bool DecodeNextSyncedFrame(double seconds, out double outTimestamp);

        [DllImport(dll)]
        private static extern bool GetMetadata(double timestampSec, out IntPtr outFrame);

        private RenderTexture m_VideoTexture;
        private RenderTexture m_FlippedTexture;
        private Material m_YflipMaterial;


        protected override void InitDecoder(string filePath)
        {
            string softwareVer = MetadataReader.GetQuickTimeSoftwareTag(filePath);
            if (!string.IsNullOrEmpty(softwareVer))
            {
                Debug.Log("Software tag: " + softwareVer);
                int underscoreIndex = softwareVer.LastIndexOf('_');
                if (underscoreIndex >= 0 && underscoreIndex < softwareVer.Length - 1)
                {
                    softwareVer = softwareVer.Substring(underscoreIndex + 1);  // "1.0.0.5"
                }
            }
            else
            {
                Debug.LogWarning("Software tag not found.");
            }

            bool bIsQT = MetadataReader.IsQuickTimeFormat(filePath);
            if (!InitDecoder(filePath, bIsQT, softwareVer, out m_Width, out m_Height))
            {
                Debug.LogError("Failed to initialize decoder.");
                return;
            }

            m_YflipMaterial = new Material(Shader.Find("ARDataset/FlipY"));

            m_IsInitialized = true;
        }

        protected override void CreatePreviewTexture()
        {
            m_VideoTexture = new RenderTexture(m_Width, m_Height, 0, RenderTextureFormat.BGRA32);
            m_VideoTexture.Create();

            SetUnityTexture(m_VideoTexture.GetNativeTexturePtr());

            m_FlippedTexture = new RenderTexture(m_Width, m_Height, 0, RenderTextureFormat.BGRA32);
            m_FlippedTexture.Create();
        }

        protected override void ReleaseVideoTexture()
        {
            m_VideoTexture?.Release();
            m_FlippedTexture?.Release();
        }

        public override Texture GetPreviewTexture()
        {
            if (m_FlippedTexture == null)
            {
                Debug.LogError("Video texture is not created.");
                return null;
            }

            return m_FlippedTexture;
        }

        public override FrameData GetNextFrame()
        {
            double playbackTime = GetCurrentPlaybackTime();

            if (DecodeNextSyncedFrame(playbackTime, out double frameTimestamp) && GetMetadata(frameTimestamp, out IntPtr framePtr))
            {
                m_Progress = (float)(frameTimestamp / m_TotalDuration);

                // 디코딩 성공: 이 프레임의 timestamp (초 단위)를 기반으로 메타데이터 추출 가능
                if (framePtr != IntPtr.Zero)
                {
                    FlipVideoTexture();

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

        private void FlipVideoTexture()
        {
            if (m_YflipMaterial != null && m_VideoTexture != null && m_FlippedTexture != null)
            {
                Graphics.Blit(m_VideoTexture, m_FlippedTexture, m_YflipMaterial);
            }
        }
    }
}