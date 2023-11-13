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

    static public Texture2D RotateTexture(Texture originalTexture, Matrix4x4 texMatrix)
    {
        if(m_PreviewRotater == null)
        {
            Shader shader = Shader.Find("VLSDK/PreviewRotation");
            if(shader == null)
            {
                Debug.LogError("[ImageUtility] Shader 'VLSDK/PreviewRotation'를 찾을 수 없음");
                return null;
            }
            m_PreviewRotater = new Material(shader);
        }

        if(m_PreviewRotater == null)
        {
            Debug.LogError("[ImageUtility] Preview를 회전시킬 material을 찾을 수 없음");
            return null;
        }

        // RenderTexture 생성.
        RenderTexture currentRT = RenderTexture.active;
        if (m_RenderTexture == null)
        {
            m_RenderTexture = new RenderTexture(originalTexture.width, originalTexture.height, 24);
        }
        else
        {
            if(Mathf.Abs(texMatrix.m00) == 1) 
            {
                m_RenderTexture = new RenderTexture(originalTexture.width, originalTexture.height, 24);
            }
            else
            {
                m_RenderTexture = new RenderTexture(originalTexture.height, originalTexture.width, 24);   
            }
        }

        RenderTexture.active = m_RenderTexture;

        // RenderTexture 실행.
        m_PreviewRotater.SetTexture("_MainTex", originalTexture);
        m_PreviewRotater.SetMatrix("_TransformMatrix", texMatrix);

        Graphics.Blit(originalTexture, m_RenderTexture, m_PreviewRotater);

        Texture2D result = ConvertToTexture2D(m_RenderTexture);

        RenderTexture.active = currentRT;

        return result;
    }

    static public void Save(string filenameNoExt, byte[] data)
    {
        if(data == null) {
            return;
        }

        if(filenameNoExt.Contains(",true")) {
            filenameNoExt = filenameNoExt.Replace(",true", "");
        }
        if(filenameNoExt.Contains(",false")) {
            filenameNoExt = filenameNoExt.Replace(",false", "");
        }


        string filename;
        if(filenameNoExt.Contains(".jpg")) {
            filename = "/" + filenameNoExt;
        } else {
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
