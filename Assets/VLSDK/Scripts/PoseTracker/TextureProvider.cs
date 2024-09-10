using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye
{
    public class TextureProvider : MonoBehaviour
    {
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
    }
}