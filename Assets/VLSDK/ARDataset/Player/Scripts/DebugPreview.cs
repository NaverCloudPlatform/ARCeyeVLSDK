using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye.Dataset
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

            var rectTransform = GetComponent<RectTransform>();
            SetAnchorsToCenter(rectTransform);

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            gameObject.SetActive(false);
#endif
        }

        void SetAnchorsToCenter(RectTransform rectTransform)
        {
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
        }

        public void SetTexture(Texture texture) 
        {
            float texRatio = (float) texture.height / (float) texture.width;
            float screenRatio = (float) Screen.height / (float) Screen.width;

            Vector2 newSize;

            // texRatio가 더 클 경우 -> texture의 height를 Screen에 맞춤
            if(texRatio < screenRatio)
            {
                float previewWidth = Screen.height / texRatio;
                float previewHeight = Screen.height;
                newSize = new Vector2(previewWidth, previewHeight);
            }
            // texRatio가 더 작을 경우 -> texture의 width를 Screen에 맞춤
            else
            {
                float previewWidth = Screen.width;
                float previewHeight = Screen.width * texRatio;
                newSize = new Vector2(previewWidth, previewHeight);
            }

            m_PreviewImage.rectTransform.sizeDelta = newSize;

            m_PreviewImage.texture = texture;
            m_PreviewImage.color = Color.white;
        }
    }
}