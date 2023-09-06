using System.Collections;
using System.Collections.Generic;
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
    }
}