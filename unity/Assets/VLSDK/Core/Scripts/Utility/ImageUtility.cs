using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class ImageUtility
{
    static private Texture2D m_PreviewTexture = null;
    static private RenderTexture m_RenderTexture = null;
    static private Material m_PreviewRotater;


    static public Texture2D RotateTexture(Texture originalTexture, Matrix4x4 texMatrix)
    {
        if (m_PreviewRotater == null)
        {
            Shader shader = Shader.Find("VLSDK/PreviewRotation");
            if (shader == null)
            {
                Debug.LogError("[ImageUtility] Shader 'VLSDK/PreviewRotation'를 찾을 수 없음");
                return null;
            }
            m_PreviewRotater = new Material(shader);
        }

        if (m_PreviewRotater == null)
        {
            Debug.LogError("[ImageUtility] Preview를 회전시킬 material을 찾을 수 없음");
            return null;
        }

        // 현재 RenderTexture를 캐싱.
        RenderTexture prevRT = RenderTexture.active;

        // RenderTexture 생성 시도.
        RenderTexture currentRT = TryCreatingRenderTexture(originalTexture, texMatrix);

        RenderTexture.active = currentRT;

        // RenderTexture 실행.
        m_PreviewRotater.SetTexture("_MainTex", originalTexture);
        m_PreviewRotater.SetMatrix("_TransformMatrix", texMatrix);

        Graphics.Blit(originalTexture, currentRT, m_PreviewRotater);

        Texture2D result = ConvertToTexture2D(currentRT);

        RenderTexture.active = prevRT;

        return result;
    }

    static private RenderTexture TryCreatingRenderTexture(Texture originalTexture, Matrix4x4 texMatrix)
    {
        // rotation이 없는 경우. width, height를 그대로 사용.
        int textureWidth;
        int textureHeight;

        if (IsTextureRotated(texMatrix))
        {
            textureWidth = originalTexture.height;
            textureHeight = originalTexture.width;
        }
        else
        {
            textureWidth = originalTexture.width;
            textureHeight = originalTexture.height;
        }

        // 첫 프레임에서는 landscape 기준으로 RenderTexture가 생성됐는데
        // 그 다음 프레임에서는 portrait 기준으로 RenderTexture를 생성해야 하는 경우가 있음.
        // 해당 현상을 대응하기 위해 생성된 rtt의 width와 입력된 텍스쳐의 width가 동일한지 비교.
        if (m_RenderTexture == null || m_RenderTexture.width != textureWidth)
        {
            m_RenderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        }

        return m_RenderTexture;
    }

    static private bool IsTextureRotated(Matrix4x4 texMatrix)
    {
        // landscape 센서에 portrait 이미지가 들어올 경우
        //  0 1 0  이 행렬이나  0 -1 0 이 행렬이 입력됨.
        // -1 0 0            1  0 0
        //  0 0 1            0  0 1
        return Mathf.Abs(texMatrix.m00) == 0;
    }

    static public Texture2D ConvertToTexture2D(Texture texture)
    {
        Texture mainTexture = texture;
        if (m_PreviewTexture == null || mainTexture.width != m_PreviewTexture.width)
        {
            m_PreviewTexture = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
        }

        RenderTexture currentRT = RenderTexture.active;
        if (m_RenderTexture == null || m_RenderTexture.width != mainTexture.width)
        {
            m_RenderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 24);
        }

        Graphics.Blit(mainTexture, m_RenderTexture);

        RenderTexture.active = m_RenderTexture;
        m_PreviewTexture.ReadPixels(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height), 0, 0);
        m_PreviewTexture.Apply();

        RenderTexture.active = currentRT;

        return m_PreviewTexture;
    }

    static public void Save(string filenameNoExt, byte[] data)
    {
        if (data == null)
        {
            return;
        }

        if (filenameNoExt.Contains(",true"))
        {
            filenameNoExt = filenameNoExt.Replace(",true", "");
        }
        if (filenameNoExt.Contains(",false"))
        {
            filenameNoExt = filenameNoExt.Replace(",false", "");
        }


        string filename;
        if (filenameNoExt.Contains(".jpg"))
        {
            filename = "/" + filenameNoExt;
        }
        else
        {
            filename = "/" + filenameNoExt + ".jpg";
        }
        System.IO.File.WriteAllBytes(Application.persistentDataPath + filename, data);
        Debug.Log("Save a query image at path : " + Application.persistentDataPath + filename);
    }

    static public void Save(string filenameNoExt, Texture2D tex)
    {
        byte[] data = ImageConversion.EncodeToJPG(tex, 85);
        Save(filenameNoExt, data);
    }
}
