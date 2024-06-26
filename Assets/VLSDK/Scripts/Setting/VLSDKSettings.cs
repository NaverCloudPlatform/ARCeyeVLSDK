using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    [System.Serializable]
    public class VLSDKSettings : ScriptableObject
    {
        [field:SerializeField, Tooltip("ARC eye의 API 연동을 통해 생성된 요청 주소")]
        private List<VLURL> m_URLList = new List<VLURL>();
        public List<VLURL> URLList { get => m_URLList; }
        
        [field:SerializeField, Tooltip("GPS를 활용한 invokeUrl 추정 및 초기화 가이드 기능 사용")]
        private bool m_GPSGuide = true;
        public bool GPSGuide { 
            get => m_GPSGuide;
            set => m_GPSGuide = value;
        }

        [SerializeField, Tooltip("GPS를 기반으로 추정할 영역들에 대한 geojson 내용")]
        [TextArea(5, 20)]
        private string m_LocationGeoJson;
        public string locationGeoJson { 
            get => m_LocationGeoJson; 
        }

        [SerializeField, Tooltip("VL Pass 상태가 되기 전의 요청 주기. (단위 : ms)")]
        [Range(250, 3000)]
        private int m_VLIntervalInitial = 250;
        public int vlIntervalInitial
        {
            get => m_VLIntervalInitial;
        }

        [SerializeField, Tooltip("VL Pass 상태가 된 이후의 요청 주기. (단위 : ms)")]
        [Range(500, 3000)]
        private int m_VLIntervalPassed = 1000;
        public int vlIntervalPassed
        {
            get => m_VLIntervalPassed;
        }

        [SerializeField, Tooltip("VL 품질. LOW - VL Pass로 인식하는 응답이 더 자주 들어옴. 잘못된 위치를 추정할 가능성이 높음. HIGH - VL Pass로 인식하는 응답이 적게 들어옴. 잘못된 위치를 추정할 가능성이 낮음")]
        private VLQuality m_VLQuality = VLQuality.MEDIUM;
        public VLQuality vlQuality
        {
            get => m_VLQuality;
            set => m_VLQuality = value;
        }

        [SerializeField, Tooltip("콘솔에 출력할 로그 레벨 설정")]
        private LogLevel m_LogLevel = LogLevel.DEBUG;
        public LogLevel logLevel
        {
            get => m_LogLevel;
            set => m_LogLevel = value;
        }
    }
}
