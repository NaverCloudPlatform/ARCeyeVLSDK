using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace ARCeye {
    public class GeoCoordProvider : MonoBehaviour
    {
        /// 실제 디바이스에서 Location 필드의 위치에 해당하는 GPS를 사용할지 여부 설정.
        [SerializeField]
        private bool m_UseFakeGPSCoordOnDevice;
        public  bool UseFakeGPSCoordOnDevice
        {
            get => m_UseFakeGPSCoordOnDevice;
            set => m_UseFakeGPSCoordOnDevice = value;
        }

        // 외부에서 latitude와 longitude를 할당했을 경우 GPSData를 사용하지 않고 입력된 값을 사용함.
        [field:SerializeField]
        public float latitude { get; set; }
        
        [field:SerializeField]
        public float longitude { get; set; }

        public LocationInfo info {
            get {
    #if UNITY_EDITOR
                return new LocationInfo(latitude, longitude);
    #else
                if(m_UseFakeGPSCoordOnDevice) {
                    return new LocationInfo(latitude, longitude);
                } else {
                    return new LocationInfo(Input.location.lastData);
                }
    #endif
            }
        }
        
        private GeoCoordInitEvent m_InitEvent;
        public  GeoCoordInitEvent onInitialized {
            get => m_InitEvent;
            set => m_InitEvent = value;
        }

        void Awake()
        {
            if(m_UseFakeGPSCoordOnDevice)
            {
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.WARNING, "******** 주의 ********");
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.WARNING, "Fake GPS를 사용중입니다.\n기기의 GPS 센서값을 사용하기 위해서는 VLSDKManager의 GeoCoordProvider에 설정 된 Use Fake GPS Coord 값을 false로 설정해주세요");
                ARCeye.LogViewer.DebugLog(ARCeye.LogLevel.WARNING, "*********************");
            }
        }

        void Start()
        {
    #if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += ReceiveGPSPermission;
                Permission.RequestUserPermission(Permission.FineLocation, callbacks);
            }
            else
            {
                StartGPS();    
            }
    #else
            StartGPS();
    #endif   
        }

        private IEnumerator InitLocationService() 
        {
            if (!Input.location.isEnabledByUser) {
                ARCeye.LogViewer.DebugLog(LogLevel.WARNING, "[GeoCoordProvider] InitLocationService - 사용자가 location 서비스를 활성화하지 않음");
                yield break;
            }

            Input.location.Start();

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (maxWait < 1)
            {
                ARCeye.LogViewer.DebugLog(LogLevel.WARNING, "[GeoCoordProvider] InitLocationService - Timeout");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                ARCeye.LogViewer.DebugLog(LogLevel.WARNING, "[GeoCoordProvider] InitLocationService - Input.location.status == LocationServiceStatus.Failed");
                yield break;
            }
            
            if(m_InitEvent != null) {
                m_InitEvent.Invoke();
            }

            ARCeye.LogViewer.DebugLog(LogLevel.INFO, "[GeoCoordProvider] GPS module is initialized");
        }

        private void StartGPS() {
#if !UNITY_EDITOR
            StartCoroutine( InitLocationService() );
#endif
        }

        private void ReceiveGPSPermission(string permissionName) {
#if !UNITY_EDITOR
            StartGPS();
#endif
        }
    }
}
