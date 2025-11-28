using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    public class VLRequestEventData
    {
        private string m_Url;
        public string Url => m_Url;

        private string m_SecretKey;
        public string SecretKey => m_SecretKey;

        private static Texture2D s_RequestTexture;
        public Texture2D RequestTexture => s_RequestTexture;

        private static Vector2 s_FocalLength;
        public Vector2 FocalLength => s_FocalLength;

        private static Vector2 s_PrincipalPoint;
        public Vector2 PrincipalPoint => s_PrincipalPoint;


        public static VLRequestEventData Create(VLRequestBody requestBody, byte[] imageBuffer)
        {
            VLRequestEventData eventData = new VLRequestEventData();
            eventData.Initialize(requestBody, imageBuffer);
            return eventData;
        }

        private void Initialize(VLRequestBody requestBody, byte[] imageBuffer)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (texture.LoadImage(imageBuffer))
            {
                Initialize(requestBody, texture);
            }
            else
            {
                NativeLogger.DebugLog(LogLevel.ERROR, "Failed to convert image buffer to texture");
            }

            s_FocalLength = new Vector2(requestBody.fx, requestBody.fy);
            s_PrincipalPoint = new Vector2(requestBody.cx, requestBody.cy);

            Object.Destroy(texture);
        }

        private void Initialize(VLRequestBody requestBody, Texture2D requestTexture)
        {
            m_Url = requestBody.url;
            m_SecretKey = requestBody.authorization;

            if (s_RequestTexture == null || s_RequestTexture.width != requestTexture.width)
            {
                s_RequestTexture = new Texture2D(requestTexture.width, requestTexture.height, requestTexture.format, false);
            }

            Graphics.CopyTexture(requestTexture, s_RequestTexture);
        }

        public override string ToString()
        {
            return $"URL: {m_Url}\nSecretKey: {m_SecretKey}";
        }
    }
}