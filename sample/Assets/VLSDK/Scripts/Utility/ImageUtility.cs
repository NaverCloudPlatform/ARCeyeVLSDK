using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageUtility
{
    static private Texture2D m_PreviewTexture = null;
    static private RenderTexture m_RenderTexture = null;

    static private Color32[] m_Rotated;
    static private Texture2D m_RotatedTexture;

    static public Texture2D ConvertToTexture2D(Texture texture)
    {
        Texture mainTexture = texture;
        if (m_PreviewTexture == null)
        {
            m_PreviewTexture = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
        }

        RenderTexture currentRT = RenderTexture.active;
        if (m_RenderTexture == null)
        {
            m_RenderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
        }

        Graphics.Blit(mainTexture, m_RenderTexture);

        RenderTexture.active = m_RenderTexture;
        m_PreviewTexture.ReadPixels(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height), 0, 0);
        m_PreviewTexture.Apply();

        RenderTexture.active = currentRT;

        return m_PreviewTexture;
    }

    static public Texture2D RotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        int w = originalTexture.width;
        int h = originalTexture.height;

        if(m_Rotated == null)
            m_Rotated = new Color32[original.Length];

        if(m_RotatedTexture == null)
            m_RotatedTexture = new Texture2D(h, w);

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                m_Rotated[iRotated] = original[iOriginal];
            }
        }

        m_RotatedTexture.SetPixels32(m_Rotated);
        m_RotatedTexture.Apply();

        return m_RotatedTexture;
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
