using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ARCeye
{
    public enum VLQuality
    {
        LOW, MEDIUM, HIGH
    }

    public enum LogLevel
    {
        DEBUG, INFO, WARNING, ERROR, FATAL
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    [System.Serializable]
    public struct TrackerConfig
    {
        [HideInInspector]
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 1024)]
        public string invokeUrl;

        [HideInInspector]
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 1024)]
        public string secretKey;

        // Request용 Texture2D를 생성하는데 사용.
        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        [System.NonSerialized]
        public int previewWidth;

        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        [System.NonSerialized]
        public int previewHeight;

        /// 기기의 상하 방향을 이용한 VLSDK 세션 리셋 기능을 활성화 하는 플래그.
        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool resetByDevicePose;

        [Header("VL Options")]
        /// VL의 응답 결과가 현재 위치와 비교했을때 5m 이상 차이가 발생하는 경우 VL 응답을 무시하는 기능을 활성화 하는 플래그.
        /// 현재의 위치를 시뮬레이션 하지 못하는 Editor 환경에서는 false로 설정하는 것이 권장된다.
        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool useTranslationFilter;

        /// VL의 응답 결과가 현재 위치와 비교했을때 Yaw 40도 이상 차이가 발생하는 경우 VL 응답을 무시하는 기능을 활성화 하는 플래그.
        /// 현재의 위치를 시뮬레이션 하지 못하는 Editor 환경에서는 false로 설정하는 것이 권장된다.
        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool useRotationFilter;

        /// VL의 응답 결과를 현재 위치에 바로 적용하지 않고 보간법을 이용해 자연스럽게 VL의 응답 결과로 수렴하는 기능을 활성화 하는 플래그.
        /// 현재의 위치를 시뮬레이션 하지 못하는 Editor 환경에서는 false로 설정하는 것이 권장된다.
        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool useInterpolation;

        /// 최초 VL 인식 단계에서 GPS를 이용하여 탐색 속도를 증가시키는 기능
        /// 정확한 GPS 값을 사용하지 못하거나 맵핑 된 지도의 GPS 값에 오류가 발생한 경우 이를 false로 설정한다.
        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool useGPSGuide;

        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool useWithGlobal;

        [HideInInspector, MarshalAs(UnmanagedType.U1)]
        public bool useFaceBlurring;

        [HideInInspector, MarshalAs(UnmanagedType.R4)]
        public float confidenceLow;

        [HideInInspector, MarshalAs(UnmanagedType.R4)]
        public float confidenceMedium;

        [HideInInspector, MarshalAs(UnmanagedType.R4)]
        public float confidenceHigh;


        [Header("Request Options")]
        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        /// VL Pass 상태가 아닐 경우의 VL 요청 주기. 단위 ms.
        public int requestIntervalBeforeLocalization;

        /// VL Pass 상태일 경우의 VL 요청 주기. 단위 ms.
        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        public int requestIntervalAfterLocalization;

        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        public VLQuality vlQuality;

        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        public int vlSearchRange;

        /// 처음 위치를 추정할 때 사용하는 성공 응답 개수.
        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        public int originPoseCount;

        /// 처음 위치를 추정할 때 사용되는 VL 응답의 최소 각도.
        [HideInInspector, MarshalAs(UnmanagedType.R4)]
        public float originPoseDegree;


        [Header("Log Option")]
        [HideInInspector, MarshalAs(UnmanagedType.I4)]
        public LogLevel logLevel;
    }
}