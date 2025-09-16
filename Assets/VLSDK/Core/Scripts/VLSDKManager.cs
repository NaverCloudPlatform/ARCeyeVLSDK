using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ARCeye
{
    public class VLSDKManager : MonoBehaviour, IGPSLocationRequester
    {
        const string PACKAGE_VERSION = "1.11.0-preview.2";

        private PoseTracker m_PoseTracker;
        public PoseTracker poseTracker => m_PoseTracker;

        private NetworkController m_NetworkController;
        private GeoCoordProvider m_GeoCoordProvider;
        private TextureProvider m_TextureProvider;


#if UNITY_IOS
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif
        private static VLSDKManager s_Instance;

        [SerializeField]
        private bool m_PlayOnAwake = true;
        public bool playOnAwake
        {
            get => m_PlayOnAwake;
            set => m_PlayOnAwake = value;
        }

        [SerializeField]
        private VLSDKSettings m_Settings;
        public VLSDKSettings settings
        {
            get => m_Settings;
            set => m_Settings = value;
        }

        private Transform m_ARCamera;
        public Transform arCamera
        {
            get => m_ARCamera;
            set => m_ARCamera = value;
        }

        private Transform m_Origin;
        public Transform origin
        {
            get => m_Origin;
            set => m_Origin = value;
        }

        private Config m_Config;


        [Header("Event")]
        [SerializeField]
        private VLRequestEvent m_OnVLPoseRequested = new VLRequestEvent();
        public VLRequestEvent OnVLPoseRequested
        {
            get => m_OnVLPoseRequested;
            set
            {
                m_NetworkController.OnVLPoseRequested = value;
                m_OnVLPoseRequested = value;
            }
        }

        [SerializeField]
        private VLRespondedEvent m_OnVLPoseResponded = new VLRespondedEvent();
        public VLRespondedEvent OnVLPoseResponded
        {
            get => m_OnVLPoseResponded;
            set
            {
                m_NetworkController.OnVLPoseResponded = value;
                m_OnVLPoseResponded = value;
            }
        }

        [SerializeField]
        private ChangedStateEvent m_OnStateChanged = new ChangedStateEvent();
        public ChangedStateEvent OnStateChanged
        {
            get => m_OnStateChanged;
            set
            {
                m_PoseTracker.onStateChanged = value;
                m_OnStateChanged = value;
            }
        }

        // [SerializeField]
        private ChangedLocationEvent m_OnLocationChanged;
        public ChangedLocationEvent OnLocationChanged
        {
            get => m_OnLocationChanged;
            set
            {
                m_PoseTracker.onLocationChanged = value;
                m_OnLocationChanged = value;
            }
        }

        // [SerializeField]
        private ChangedBuildingEvent m_OnBuildingChanged;
        public ChangedBuildingEvent OnBuildingChanged
        {
            get => m_OnBuildingChanged;
            set
            {
                m_PoseTracker.onBuildingChanged = value;
                m_OnBuildingChanged = value;
            }
        }

        // [SerializeField]
        private ChangedFloorEvent m_OnFloorChanged;
        public ChangedFloorEvent OnFloorChanged
        {
            get => m_OnFloorChanged;
            set
            {
                m_PoseTracker.onFloorChanged = value;
                m_OnFloorChanged = value;
            }
        }

        // [SerializeField]
        private ChangedRegionCodeEvent m_OnRegionCodeChanged;
        public ChangedRegionCodeEvent OnRegionCodeChanged
        {
            get => m_OnRegionCodeChanged;
            set
            {
                m_PoseTracker.onRegionCodeChanged = value;
                m_OnRegionCodeChanged = value;
            }
        }

        [SerializeField]
        private ChangedLayerInfoEvent m_OnLayerInfoChanged = new ChangedLayerInfoEvent();
        public ChangedLayerInfoEvent OnLayerInfoChanged
        {
            get => m_OnLayerInfoChanged;
            set
            {
                m_PoseTracker.onLayerInfoChanged = value;
                m_OnLayerInfoChanged = value;
            }
        }

        [SerializeField]
        private UpdatedARFrameEvent m_OnARFrameUpdated = new UpdatedARFrameEvent();
        public UpdatedARFrameEvent OnARFrameUpdated
        {
            get => m_OnARFrameUpdated;
            set
            {
                m_PoseTracker.onARFrameUpdated = value;
                m_OnARFrameUpdated = value;
            }
        }

        [SerializeField]
        private UpdatedPoseEvent m_OnPoseUpdated = new UpdatedPoseEvent();
        public UpdatedPoseEvent OnPoseUpdated
        {
            get => m_OnPoseUpdated;
            set
            {
                m_PoseTracker.onPoseUpdated = value;
                m_OnPoseUpdated = value;
            }
        }

        [SerializeField]
        private UpdatedGeoCoordEvent m_OnGeoCoordUpdated = new UpdatedGeoCoordEvent();
        public UpdatedGeoCoordEvent OnGeoCoordUpdated
        {
            get => m_OnGeoCoordUpdated;
            set
            {
                m_PoseTracker.onGeoCoordUpdated = value;
                m_OnGeoCoordUpdated = value;
            }
        }

        [SerializeField]
        private UpdatedRelAltitudeEvent m_OnRelativeAltitudeUpdated = new UpdatedRelAltitudeEvent();
        public UpdatedRelAltitudeEvent OnRelativeAltitudeUpdated
        {
            get => m_OnRelativeAltitudeUpdated;
            set
            {
                m_PoseTracker.onRelAltitudeUpdated = value;
                m_OnRelativeAltitudeUpdated = value;
            }
        }

        // [SerializeField]
        private DetectedObjectEvent m_OnObjectDetected;
        public DetectedObjectEvent OnObjectDetected
        {
            get => m_OnObjectDetected;
            set
            {
                m_PoseTracker.onObjectDetected = value;
                m_OnObjectDetected = value;
            }
        }

        private Camera m_MainCamera;
        public Camera mainCamera
        {
            get
            {
                if (m_MainCamera == null)
                {
                    m_MainCamera = Camera.main;
                }
                return m_MainCamera;
            }
        }

        public TrackerState trackerState => m_PoseTracker.state;

        public string version
        {
            get
            {
                return PACKAGE_VERSION;
            }
        }

        private bool m_IsInitialized = false;

        private NativeLogger m_NativeLogger;


        //// Lifecycle

        private void Awake()
        {
            s_Instance = this;
        }

        private void OnEnable()
        {
            if (m_NetworkController == null)
            {
                m_NetworkController = GetComponent<NetworkController>();
            }
            m_NetworkController.Initialize();

            if (m_GeoCoordProvider == null)
            {
                m_GeoCoordProvider = GetComponent<GeoCoordProvider>();
            }

            if (m_TextureProvider == null)
            {
                m_TextureProvider = GetComponent<TextureProvider>();
            }
        }

        /// <summary>
        ///  VLSDKManager 시스템 초기화. VLSDKManager를 런타임에 생성하는 경우가 아니라면 직접 호출하지 않는다.
        ///  VLSDKManager를 런타임에 생성하여 VLSDKSettings를 직접 할당하는 경우 VLSDKSettings를 VLSDKManager에 할당한 뒤에 초기화 메서드를 호출해야 한다.
        /// </summary>
        public void Initialize()
        {
            if (!m_IsInitialized)
            {
                InitConfig();
                InitLogger();

                InitCamera();
                InitPoseTracker();
                InitNetworkController();

                m_IsInitialized = true;
            }
        }

        private void Start()
        {
            Initialize();

            m_OnPoseUpdated?.AddListener(UpdateOriginPose);

            m_PoseTracker.onStateChanged = m_OnStateChanged;
            m_PoseTracker.onARFrameUpdated = m_OnARFrameUpdated;
            m_PoseTracker.onPoseUpdated = m_OnPoseUpdated;
            m_PoseTracker.onRelAltitudeUpdated = m_OnRelativeAltitudeUpdated;
            m_PoseTracker.onRegionCodeChanged = m_OnRegionCodeChanged;
            m_PoseTracker.onLayerInfoChanged = m_OnLayerInfoChanged;
            m_PoseTracker.onObjectDetected = m_OnObjectDetected;

            m_NetworkController.OnVLPoseRequested = m_OnVLPoseRequested;
            m_NetworkController.OnVLPoseResponded = m_OnVLPoseResponded;

            CheckObsoleteEvents();

            m_PoseTracker.SetGPSLocationRequester(this);
            StartDetectingGPSLocation();

            if (m_PlayOnAwake)
            {
                StartSession();
            }
        }

        private void InitCamera()
        {
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera를 찾을 수 없습니다.");
            }
            m_ARCamera = mainCamera.transform;
            m_Origin = m_ARCamera.parent;
        }

        private void InitConfig()
        {
            if (m_Config == null)
            {
                Debug.LogWarning("[VLSDKManager] Config is null. Use default Config setting");
                m_Config = new Config();
            }

            if (m_Settings == null || m_Settings.URLList.Count == 0)
            {
                Debug.LogWarning("[VLSDKManager] URL List is empty.");
                return;
            }

            m_Config.tracker.requestIntervalBeforeLocalization = m_Settings.vlIntervalInitial;
            m_Config.tracker.requestIntervalAfterLocalization = m_Settings.vlIntervalPassed;
            m_Config.tracker.useGPSGuide = m_Settings.GPSGuide;
            m_Config.tracker.useFaceBlurring = m_Settings.faceBlurring;
            m_Config.tracker.vlQuality = m_Settings.vlQuality;

            m_Config.tracker.failureCountToNotRecognized = m_Settings.failureCountToNotRecognized;
            m_Config.tracker.failureCountToFail = m_Settings.failureCountToFail;
            m_Config.tracker.failureCountToReset = m_Settings.failureCountToReset;

            m_Config.tracker.originPoseCount = m_Settings.initialPoseCount;
            m_Config.tracker.originPoseDegree = m_Settings.initialPoseDegree;

            m_Config.logLevel = m_Settings.logLevel;
            m_Config.urlList = m_Settings.URLList;
            m_Config.vlAreaGeoJson = m_Settings.locationGeoJson;
        }

        private void InitPoseTracker()
        {
            // PoseTracker를 초기화. CustomPoseTrackerAdaptor가 등록 되어 있다면
            // CustomPoseTrackerAdaptor에서 설정한 PoseTracker를 사용한다.
            CustomPoseTrackerAdaptor customPoseTracker = GetComponent<CustomPoseTrackerAdaptor>();

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            if (customPoseTracker != null && customPoseTracker.UseCustomEditorPoseTracker)
            {
                customPoseTracker.Initialize();

                m_PoseTracker = customPoseTracker.CustomEditorPoseTracker;
            }
            else
            {
                m_PoseTracker = new ARDatasetPoseTracker();

                // unit test일 경우에는 모든 필터 비활성화.
                m_Config.tracker.useTranslationFilter = !settings.testMode;
                m_Config.tracker.useRotationFilter = !settings.testMode;
                m_Config.tracker.useInterpolation = !settings.testMode;
            }
#else
            if (customPoseTracker != null && customPoseTracker.UseCustomDevicePoseTracker)
            {
                customPoseTracker.Initialize();

                m_PoseTracker = customPoseTracker.CustomDevicePoseTracker;
            }
            else
            {
                m_PoseTracker = new ARFoundationPoseTracker();
                // (m_PoseTracker as ARFoundationPoseTracker).UseAccurateHeight(m_Settings.AccurateHeight);
            }
#endif

            m_PoseTracker.SetGeoCoordProvider(m_GeoCoordProvider);
            m_PoseTracker.Initialize(m_Config);

            string nativeVersion = m_PoseTracker?.GetVersion();
            Debug.Log($"<b>VLSDK version {PACKAGE_VERSION}, native {nativeVersion}</b>");
        }

        private void InitNetworkController()
        {
            if (m_Settings == null)
            {
                Debug.LogWarning("[VLSDKManager] VLSDKSettings is null. Hide VL pose gizmo.");
                m_NetworkController.EnableVLPose(false);
            }
            else
            {
                m_NetworkController.EnableVLPose(m_Settings.showVLPose);
            }
        }

        private void InitLogger()
        {
            m_NativeLogger = new NativeLogger();
            m_NativeLogger.logLevel = m_Config.logLevel;
            m_NativeLogger.Initialize();

            var logViewer = GetComponentInChildren<LogViewer>();
            if (logViewer)
            {
                m_NativeLogger.onLogAdded.AddListener(logElem =>
                {
                    logViewer.AddLogElem(logElem);
                });

                m_OnStateChanged?.AddListener(logViewer.OnStateChanged);
                m_OnVLPoseRequested?.AddListener(logViewer.OnVLPoseRequested);
                m_OnVLPoseResponded?.AddListener(logViewer.OnVLPoseResponded);
                m_OnRegionCodeChanged?.AddListener(logViewer.OnLayerInfoChanged);
                m_OnLayerInfoChanged?.AddListener(logViewer.OnLayerInfoChanged);
                m_OnPoseUpdated?.AddListener(logViewer.OnPoseUpdated);
                m_OnRelativeAltitudeUpdated?.AddListener(logViewer.OnRelAltitudeUpdated);
            }
        }

        private void CheckObsoleteEvents()
        {
            if (m_OnLocationChanged?.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning("[VLSDKManager] OnLocationChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
            if (m_OnBuildingChanged?.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning("[VLSDKManager] OnBuildingChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
            if (m_OnFloorChanged?.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning("[VLSDKManager] OnFloorChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
            if (m_OnRegionCodeChanged?.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning("[VLSDKManager] OnRegionCodeChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
        }

        private void OnDisable()
        {
            StopSession();
        }

        private void OnDestroy()
        {
            m_PoseTracker.Release();
            m_NativeLogger.Release();
        }

        private void UpdateOriginPose(Matrix4x4 localizedViewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 texMatrix, double relativeAltitude)
        {
            // VL 수신 결과의 WC Transform Matrix 계산.
            Matrix4x4 localizedPoseMatrix = Matrix4x4.Inverse(localizedViewMatrix);

            // 디바이스의 AR SDK session을 기준으로 하는 AR Camera의 Local Transform Matrix 계산.
            Transform camTrans = mainCamera.transform;
            Matrix4x4 lhCamModelMatrix = Matrix4x4.TRS(camTrans.localPosition, camTrans.localRotation, Vector3.one);

            // 위의 두 Matrix를 이용하여 AR Session Origin의 WC Transform Matrix 계산.
            Matrix4x4 originModelMatrix = localizedPoseMatrix * Matrix4x4.Inverse(lhCamModelMatrix);

            m_TextureProvider.texMatrix = texMatrix;

            // 계산 된 Origin의 Transform Matrix를 이용해 OriginTransform의 position, rotation 설정.
            if (originModelMatrix.ValidTRS())
            {
                m_Origin.localPosition = originModelMatrix.GetColumn(3);
                m_Origin.localRotation = originModelMatrix.rotation;
            }
        }

        public void StartSession()
        {
            m_PoseTracker.RegisterFrameLoop();
        }

        public void StopSession()
        {
            m_PoseTracker.UnregisterFrameLoop();
        }

        public void ResetSession()
        {
            m_PoseTracker.Reset();
        }

        public void ChangeState(TrackerState state)
        {
            m_PoseTracker.ChangeState(state);
        }

        public void SetTrackerConfig(TrackerConfig config)
        {
            m_PoseTracker.SetTrackerConfig(config);
        }

        public TrackerConfig GetTrackerConfig()
        {
            return m_PoseTracker.GetTrackerConfig();
        }

        private void DetectVLLocation()
        {
            double latitude = m_GeoCoordProvider.info.latitude;
            double longitude = m_GeoCoordProvider.info.longitude;
            double radius = 100;

            if (m_Config.tracker.useGPSGuide && (latitude == 0 || longitude == 0))
            {
                NativeLogger.DebugLog(LogLevel.ERROR, "Failed to get GPS coordinate");
            }

            m_PoseTracker.DetectVLLocation(latitude, longitude, radius);
            m_OnGeoCoordUpdated.Invoke(latitude, longitude);
        }

        public string FindLocation(double latitude, double longitude)
        {
            return m_PoseTracker.FindVLLocation(latitude, longitude);
        }

        public void EnableResetByDevicePose(bool active)
        {
            m_PoseTracker.EnableResetByDevicePose(active);
        }

        /* -- GPS Location Requester -- */
        public void StartDetectingGPSLocation()
        {
            if (!m_Config.tracker.useGPSGuide)
            {
                return;
            }

            // Tracking state가 initial일 경우에는 1초에 한 번씩 GPS 기반 VL Location Id 요청.
            CancelInvoke(nameof(DetectVLLocation));
            InvokeRepeating(nameof(DetectVLLocation), 1.0f, 1.0f);
        }

        public void StopDetectingGPSLocation()
        {
            CancelInvoke(nameof(DetectVLLocation));
        }


        /// Debugging

        private void OnDrawGizmos()
        {
            if (mainCamera == null)
            {
                return;
            }

            Matrix4x4 poseMatrix = mainCamera.transform.localToWorldMatrix;
            DebugUtility.DrawFrame(poseMatrix, Color.black, 1.5f);
        }
    }
}