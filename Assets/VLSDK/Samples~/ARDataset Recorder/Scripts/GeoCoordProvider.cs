using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace ARCeye.Dataset.Recorder
{
    public class GeoCoordProvider : MonoBehaviour
    {
        // 외부에서 latitude와 longitude를 할당했을 경우 GPSData를 사용하지 않고 입력된 값을 사용함.
        [field: SerializeField]
        private float m_Latitude;
        public float latitude
        {
            get => m_Latitude;
            set => m_Latitude = value;
        }

        [field: SerializeField]
        private float m_Longitude;
        public float longitude
        {
            get => m_Longitude;
            set => m_Longitude = value;
        }

        public LocationInfo info
        {
            get
            {
#if UNITY_EDITOR
                return new LocationInfo(latitude, longitude);
#else
                return new LocationInfo(Input.location.lastData);
#endif
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
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("[GeoCoordProvider] InitLocationService - 사용자가 location 서비스를 활성화하지 않음");
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
                Debug.Log("[GeoCoordProvider] InitLocationService - Timeout");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.Log("[GeoCoordProvider] InitLocationService - Input.location.status == LocationServiceStatus.Failed");
                yield break;
            }

            Debug.Log("[GeoCoordProvider] GPS module is initialized");
        }

        private void StartGPS()
        {
#if !UNITY_EDITOR
            StartCoroutine( InitLocationService() );
#endif
        }

        private void ReceiveGPSPermission(string permissionName)
        {
#if !UNITY_EDITOR
            StartGPS();
#endif
        }
    }
}
