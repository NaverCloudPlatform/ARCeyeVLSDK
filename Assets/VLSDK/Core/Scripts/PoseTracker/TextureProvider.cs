using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye
{
    public class TextureProvider : MonoBehaviour
    {
        [SerializeField]
        private Texture m_TextureToSend;
        public Texture textureToSend {
            get => m_TextureToSend;
            set => m_TextureToSend = value;
        }

        private Matrix4x4 m_TexMatrix = Matrix4x4.identity;
        public Matrix4x4 texMatrix {
            get => m_TexMatrix;
            set => m_TexMatrix = value;
        }

        private static Texture2D s_RequestTexture;


        public bool CreateQueryTexture(ARCeye.RequestVLInfo requestInfo, ref Texture2D queryTexture)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            return CreateTextureUsingRawBuffer(requestInfo.imageBuffer, ref queryTexture);
#else
            if(requestInfo.texture == IntPtr.Zero)
            {
                return false;
            }

            return CreateTextureUsingRawTexture(requestInfo.texture, ref queryTexture);
#endif
        }

        private bool CreateTextureUsingRawTexture(IntPtr rawImage, ref Texture2D queryTexture)
        {
            object texObj = GCHandle.FromIntPtr(rawImage).Target;
            Type texType = texObj.GetType();

            if (texType == typeof(Texture2D))
            {
                queryTexture = texObj as Texture2D;
                return true;
            }
            else if (texType == typeof(RenderTexture))
            {
                RenderTexture tex = texObj as RenderTexture;
                RenderTexture currRT = RenderTexture.active;

                RenderTexture.active = tex;

                if (queryTexture == null)
                {
                    queryTexture = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                }
                queryTexture.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                queryTexture.Apply();

                RenderTexture.active = currRT;
                return true;
            }
            else
            {
                Debug.LogError("Invalid type of texture is used");
                return false;
            }
        }

        private bool CreateTextureUsingRawBuffer(UnityImageBuffer unityImageBuffer, ref Texture2D queryTexture)
        {
            byte[] imageBuffer = new byte[unityImageBuffer.length];
            Marshal.Copy(unityImageBuffer.pixels, imageBuffer, 0, unityImageBuffer.length);

            if(s_RequestTexture == null)
            {
                s_RequestTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);   
            }
            
            if(s_RequestTexture.LoadImage(imageBuffer))
            {
                queryTexture = s_RequestTexture;
                return true;
            }
            else
            {
                Debug.LogError("Failed to convert image buffer to texture");
                return false;
            }
        }
    }
}