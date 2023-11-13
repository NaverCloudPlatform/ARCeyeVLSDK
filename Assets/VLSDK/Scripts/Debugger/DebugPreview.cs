using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye
{
    public class DebugPreview : MonoBehaviour
    {
        private RawImage m_PreviewImage;

        void Awake()
        {
            m_PreviewImage = GetComponent<RawImage>();
            if(m_PreviewImage == null) {
                m_PreviewImage = gameObject.AddComponent<RawImage>();
            }
#if !UNITY_EDITOR
            gameObject.SetActive(false);
#endif
        }

        public void SetTexture(Texture texture) 
        {
            m_PreviewImage.texture = texture;
        }
    }
}
