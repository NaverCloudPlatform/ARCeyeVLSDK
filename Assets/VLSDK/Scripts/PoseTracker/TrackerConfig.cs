using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ARCeye
{
    [System.Serializable]
    public class Config
    {
        [HideInInspector]
        private TrackerConfig m_Tracker = new TrackerConfig();
        public TrackerConfig tracker {
            get => m_Tracker;
            set => m_Tracker = value;
        }

        [HideInInspector]
        private LogLevel m_LogLevel;
        public LogLevel logLevel {
            get => m_LogLevel;
            set => m_LogLevel = value;
        }

        [field:SerializeField]
        private List<VLURL> m_URLList = new List<VLURL>();
        public List<VLURL> urlList { 
            get => m_URLList;
            set => m_URLList = value;
        }

        public string vlAreaGeoJson { get; set; }
    }
}