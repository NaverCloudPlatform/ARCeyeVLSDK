using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    [System.Serializable]
    public class VLSDKSettings : ScriptableObject
    {
        [field: SerializeField, Tooltip("The request URL generated through the integration of ARC eye's API")]
        private List<VLURL> m_URLList = new List<VLURL>();
        public List<VLURL> URLList { get => m_URLList; }

        [field: SerializeField, Tooltip("Estimate which invoke URL to use based on GPS location")]
        private bool m_GPSGuide = true;
        public bool GPSGuide
        {
            get => m_GPSGuide;
            set => m_GPSGuide = value;
        }

        [SerializeField, Tooltip("GeoJSON content for the areas to be estimated based on GPS location")]
        [TextArea(5, 20)]
        private string m_LocationGeoJson;
        public string locationGeoJson
        {
            get => m_LocationGeoJson;
        }

        [SerializeField, Tooltip("Request interval before entering VL Pass state (unit: ms)")]
        [Range(250, 3000)]
        private int m_VLIntervalInitial = 250;
        public int vlIntervalInitial
        {
            get => m_VLIntervalInitial;
        }

        [SerializeField, Tooltip("Request interval after entering VL Pass state (unit: ms)")]
        [Range(500, 3000)]
        private int m_VLIntervalPassed = 1000;
        public int vlIntervalPassed
        {
            get => m_VLIntervalPassed;
        }

        [SerializeField, Tooltip("VL Quality. LOW - Responses recognized as VL Pass occur more frequently, with a higher likelihood of estimating incorrect locations. HIGH - Responses recognized as VL Pass occur less frequently, with a lower likelihood of estimating incorrect locations.")]
        private VLQuality m_VLQuality = VLQuality.MEDIUM;
        public VLQuality vlQuality
        {
            get => m_VLQuality;
            set => m_VLQuality = value;
        }

        [SerializeField, Tooltip("The number of poses required to calculate the initial position")]
        private int m_InitialPoseCount = 1;
        public int initialPoseCount
        {
            get => m_InitialPoseCount;
            set => m_InitialPoseCount = value;
        }

        [SerializeField, Tooltip("The difference in rotation angles(degree) between poses used to calculate the initial position")]
        private float m_InitialPoseDegree = 10;
        public float initialPoseDegree
        {
            get => m_InitialPoseDegree;
            set => m_InitialPoseDegree = value;
        }

        [SerializeField, Tooltip("Blur the faces in the request image")]
        private bool m_FaceBlurring = false;
        public bool faceBlurring
        {
            get => m_FaceBlurring;
            set => m_FaceBlurring = value;
        }

        [SerializeField, Tooltip("Visualize the responsed VL poses")]
        private bool m_ShowVLPose = false;
        public bool showVLPose
        {
            get => m_ShowVLPose;
            set => m_ShowVLPose = value;
        }

        [SerializeField, Tooltip("Set the log level for console output")]
        private LogLevel m_LogLevel = LogLevel.DEBUG;
        public LogLevel logLevel
        {
            get => m_LogLevel;
            set => m_LogLevel = value;
        }


        public bool testMode { get; set; } = false;
    }
}
