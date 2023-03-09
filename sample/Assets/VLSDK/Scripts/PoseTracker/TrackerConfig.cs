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
        public List<VLURL> urlList { get; set; }

        public string vlAreaGeoJson { get; set; }


        public bool useGPSGuide { get; set; }
    }
}