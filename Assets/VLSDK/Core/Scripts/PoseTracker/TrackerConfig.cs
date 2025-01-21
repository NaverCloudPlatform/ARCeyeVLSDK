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
        private TrackerConfig m_Tracker;
        public ref TrackerConfig tracker {
            get => ref m_Tracker;
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

        public Config()
        {
            CreateDefaultTrackerConfig();
        }

        private void CreateDefaultTrackerConfig()
        {
            m_Tracker = new TrackerConfig();
            m_Tracker.previewWidth  = 640;
            m_Tracker.previewHeight = 360;

            m_Tracker.resetByDevicePose = true;

            m_Tracker.useTranslationFilter = true;
            m_Tracker.useRotationFilter = true;
            m_Tracker.useInterpolation = true;
            m_Tracker.useGPSGuide = true;
            m_Tracker.vlSearchRange = 10;

            m_Tracker.originPoseCount = 1;
            m_Tracker.originPoseDegree = 10;

            m_Tracker.requestIntervalBeforeLocalization = 250;
            m_Tracker.requestIntervalAfterLocalization = 1000;
        }
    }
}