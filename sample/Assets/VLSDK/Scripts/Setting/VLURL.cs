using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    [System.Serializable]
    public class VLURL
    {
        [SerializeField]
        private string m_Location;
        public string location { 
            get => m_Location; 
            set => m_Location = value;
        }

        [SerializeField]
        private string m_InvokeUrl;
        public string invokeUrl { 
            get => m_InvokeUrl; 
            set => m_InvokeUrl = value;
        }

        [SerializeField]
        private string m_SecretKey;
        public string secretKey { 
            get => m_SecretKey; 
            set => m_SecretKey = value;
        }

        [SerializeField]
        private bool m_IsInactivated;
        public bool isInactivated { 
            get => m_IsInactivated; 
            set => m_IsInactivated = value;
        }
    }
}