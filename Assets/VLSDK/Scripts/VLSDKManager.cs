using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;

namespace ARCeye
{
    public class VLSDKManager : MonoBehaviour, IGPSLocationRequester
    {
        const string PACKAGE_VERSION = "1.6.5";

        private PoseTracker m_PoseTracker;
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
        public bool playOnAwake {
            get => m_PlayOnAwake;
            set => m_PlayOnAwake = value;
        }
        
        [SerializeField]
        private VLSDKSettings m_Settings;
        public  VLSDKSettings settings {
            get => m_Settings;
            set => m_Settings = value;
        }

        private Transform m_ARCamera;
        public Transform arCamera {
            get => m_ARCamera;
            set => m_ARCamera = value;
        }

        private Transform m_Origin;
        public Transform origin {
            get => m_Origin;
            set => m_Origin = value;
        }

        private Config m_Config;


        [Header("Event")]
        [SerializeField]
        private InitialPoseReceivedEvent m_OnInitialPoseReceived = new InitialPoseReceivedEvent();
        public  InitialPoseReceivedEvent OnInitalPoseReceived {
            get => m_OnInitialPoseReceived;
            set {
                m_PoseTracker.onInitialPoseReceived = value;
                m_OnInitialPoseReceived = value;
            }
        }

        [SerializeField]
        private ChangedStateEvent m_OnStateChanged = new ChangedStateEvent();
        public  ChangedStateEvent OnStateChanged {
            get => m_OnStateChanged;
            set {
                m_PoseTracker.onStateChanged = value;
                m_OnStateChanged = value;
            }
        }

        // [SerializeField]
        private ChangedLocationEvent m_OnLocationChanged;
        public  ChangedLocationEvent OnLocationChanged {
            get => m_OnLocationChanged;
            set {
                m_PoseTracker.onLocationChanged = value;
                m_OnLocationChanged = value;
            }
        }

        // [SerializeField]
        private ChangedBuildingEvent m_OnBuildingChanged; 
        public ChangedBuildingEvent OnBuildingChanged {
            get => m_OnBuildingChanged;
            set {
                m_PoseTracker.onBuildingChanged = value;
                m_OnBuildingChanged = value;
            }
        }

        // [SerializeField]
        private ChangedFloorEvent m_OnFloorChanged; 
        public ChangedFloorEvent OnFloorChanged {
            get => m_OnFloorChanged;
            set {
                m_PoseTracker.onFloorChanged = value;
                m_OnFloorChanged = value;
            }
        }

        // [SerializeField]
        private ChangedRegionCodeEvent m_OnRegionCodeChanged; 
        public ChangedRegionCodeEvent OnRegionCodeChanged {
            get => m_OnRegionCodeChanged;
            set {
                m_PoseTracker.onRegionCodeChanged = value;
                m_OnRegionCodeChanged = value;
            }
        }

        [SerializeField]
        private ChangedLayerInfoEvent m_OnLayerInfoChanged = new ChangedLayerInfoEvent(); 
        public ChangedLayerInfoEvent OnLayerInfoChanged {
            get => m_OnLayerInfoChanged;
            set {
                m_PoseTracker.onLayerInfoChanged = value;
                m_OnLayerInfoChanged = value;
            }
        }

        [SerializeField]
        private UpdatedPoseEvent m_OnPoseUpdated = new UpdatedPoseEvent();
        public  UpdatedPoseEvent OnPoseUpdated {
            get => m_OnPoseUpdated;
            set {
                m_PoseTracker.onPoseUpdated = value;
                m_OnPoseUpdated = value;
            }
        }

        [SerializeField]
        private UpdatedGeoCoordEvent m_OnGeoCoordUpdated = new UpdatedGeoCoordEvent();
        public  UpdatedGeoCoordEvent OnGeoCoordUpdated {
            get => m_OnGeoCoordUpdated;
            set {
                m_PoseTracker.onGeoCoordUpdated = value;
                m_OnGeoCoordUpdated = value;
            }
        }

        // [SerializeField]
        private DetectedObjectEvent m_OnObjectDetected;
        public  DetectedObjectEvent OnObjectDetected {
            get => m_OnObjectDetected;
            set {
                m_PoseTracker.onObjectDetected = value;
                m_OnObjectDetected = value;
            }
        }

        private Camera m_MainCamera;
        public Camera mainCamera {
            get {
                if(m_MainCamera == null) {
                    m_MainCamera = Camera.main;
                }
                return m_MainCamera;
            }
        }

        public string version {
            get {
                return PACKAGE_VERSION;
            }
        }
        
        private bool m_IsInitialized = false;

        //// Lifecycle

        private void Awake()
        {
            s_Instance = this;
        }

        private void OnEnable()
        {
            if(m_NetworkController == null)
            {
                m_NetworkController = GetComponent<NetworkController>();
            }
            m_NetworkController.Initialize();

            if(m_GeoCoordProvider == null)
            {
                m_GeoCoordProvider = GetComponent<GeoCoordProvider>();
            }

            if(m_TextureProvider == null)
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
            if(!m_IsInitialized)
            {
                InitCamera();
                InitConfig();
                InitPoseTracker();
                InitNetworkController();
                InitLogViewer();
                
                m_IsInitialized = true;
            }
        }

        private void Start()
        {
            Initialize();

            m_OnPoseUpdated?.AddListener(UpdateOriginPose);

            m_PoseTracker.onInitialPoseReceived = m_OnInitialPoseReceived;
            m_PoseTracker.onStateChanged = m_OnStateChanged;
            m_PoseTracker.onPoseUpdated = m_OnPoseUpdated;
            m_PoseTracker.onRegionCodeChanged = m_OnRegionCodeChanged;
            m_PoseTracker.onLayerInfoChanged = m_OnLayerInfoChanged;
            m_PoseTracker.onObjectDetected = m_OnObjectDetected;

            CheckObsoleteEvents();

            m_PoseTracker.SetGPSLocationRequester(this);
            StartDetectingGPSLocation();

            if(m_PlayOnAwake)
            {
                StartSession();
            }
        }

        private void InitCamera()
        {
            if(mainCamera == null) {
                Debug.LogError("Main Camera를 찾을 수 없습니다.");
            }
            m_ARCamera = mainCamera.transform;
            m_Origin = m_ARCamera.parent;
        }

        private void InitConfig()
        {
            if(m_Config == null) {
                Debug.LogWarning("[VLSDKManager] Config is null. Use default Config setting");
                m_Config = new Config();
            }

            if(m_Settings == null || m_Settings.URLList.Count == 0) {
                Debug.LogWarning("[VLSDKManager] URL List is empty.");
                return;
            }

            m_Config.tracker.resetByDevicePose = true;

            m_Config.tracker.requestIntervalBeforeLocalization = m_Settings.vlIntervalInitial;
            m_Config.tracker.requestIntervalAfterLocalization = m_Settings.vlIntervalPassed;
            m_Config.tracker.useGPSGuide = m_Settings.GPSGuide;
            m_Config.tracker.vlQuality = m_Settings.vlQuality;
            m_Config.tracker.vlSearchRange = 10;

            m_Config.logLevel = m_Settings.logLevel;
            m_Config.urlList = m_Settings.URLList;
            m_Config.vlAreaGeoJson = m_Settings.locationGeoJson;
        }

        private void InitPoseTracker()
        {
#if UNITY_EDITOR
            m_PoseTracker = new EditorPoseTracker();
            
            // unit test일 경우에는 모든 필터 비활성화.
            m_Config.tracker.useTranslationFilter = !settings.testMode;
            m_Config.tracker.useRotationFilter = !settings.testMode;
            m_Config.tracker.useInterpolation = !settings.testMode;
#else
            m_PoseTracker = new DevicePoseTracker();
#endif
            m_PoseTracker.SetGeoCoordProvider(m_GeoCoordProvider);
            m_PoseTracker.Initialize(m_ARCamera, m_Config);

            string nativeVersion = m_PoseTracker?.GetVersion();
            Debug.Log($"<b>VLSDK version {PACKAGE_VERSION}, native {nativeVersion}</b>");
        }

        private void InitNetworkController()
        {
            m_NetworkController.EnableVLPose(m_Settings.showVLPose);
        }

        private void InitLogViewer()
        {
            var logViewer = GetComponentInChildren<LogViewer>();
            if(logViewer) { 
                logViewer.logLevel = m_Config.logLevel;

                m_OnStateChanged?.AddListener(logViewer.OnStateChanged);
                // m_OnLocationChanged.AddListener(logViewer.OnLocationChanged);
                // m_OnBuildingChanged.AddListener(logViewer.OnBuildingChanged);
                // m_OnFloorChanged.AddListener(logViewer.OnFloorChanged);
                m_OnRegionCodeChanged?.AddListener(logViewer.OnLayerInfoChanged);
                m_OnLayerInfoChanged?.AddListener(logViewer.OnLayerInfoChanged);
                m_OnPoseUpdated?.AddListener(logViewer.OnPoseUpdated);
            }
        }

        private void CheckObsoleteEvents() {
            if(m_OnLocationChanged?.GetPersistentEventCount() > 0) {
                Debug.LogWarning("[VLSDKManager] OnLocationChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
            if(m_OnBuildingChanged?.GetPersistentEventCount() > 0) {
                Debug.LogWarning("[VLSDKManager] OnBuildingChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
            if(m_OnFloorChanged?.GetPersistentEventCount() > 0) {
                Debug.LogWarning("[VLSDKManager] OnFloorChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
            if(m_OnRegionCodeChanged?.GetPersistentEventCount() > 0) {
                Debug.LogWarning("[VLSDKManager] OnRegionCodeChanged 이벤트는 삭제 될 예정입니다. OnLayerInfoChanged 이벤트를 사용해주세요");
            }
        }

        private void OnDisable() {
            StopSession();
        }

        private void OnDestroy()
        {
            m_PoseTracker.Release();
        }

        private void UpdateOriginPose(Matrix4x4 localizedViewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 texMatrix)
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
            if(originModelMatrix.ValidTRS()) {
                m_Origin.localPosition = originModelMatrix.GetColumn(3);
                m_Origin.localRotation = originModelMatrix.rotation;
            }
        }

        public void StartSession() {
            m_PoseTracker.RegisterFrameLoop();
        }

        public void StopSession() {
            m_PoseTracker.UnregisterFrameLoop();
        }
        
        public void ResetSession() {
            m_PoseTracker.Reset();
        }

        public void ChangeState(TrackerState state) {
            m_PoseTracker.ChangeState(state);
        }

        public void SetTrackerConfig(TrackerConfig config) {
            m_PoseTracker.SetTrackerConfig(config);
        }

        private void DetectVLLocation() {
            double latitude = m_GeoCoordProvider.info.latitude;
            double longitude = m_GeoCoordProvider.info.longitude;
            double radius = 100;

            if(m_Config.tracker.useGPSGuide && (latitude == 0 || longitude == 0)) {
                LogViewer.DebugLog(LogLevel.ERROR, "Failed to get GPS coordinate");
            }

            m_PoseTracker.DetectVLLocation(latitude, longitude, radius);
            m_OnGeoCoordUpdated.Invoke(latitude, longitude);
        }

        public string FindLocation(double latitude, double longitude) {
            return m_PoseTracker.FindVLLocation(latitude, longitude);
        }

        public void EnableResetByDevicePose(bool active) {
            m_PoseTracker.EnableResetByDevicePose(active);
        }

        /* -- GPS Location Requester -- */
        public void StartDetectingGPSLocation() {
            if(!m_Config.tracker.useGPSGuide) {
                return;
            }

            // Tracking state가 initial일 경우에는 1초에 한 번씩 GPS 기반 VL Location Id 요청.
            CancelInvoke(nameof(DetectVLLocation));
            InvokeRepeating(nameof(DetectVLLocation), 1.0f, 1.0f);
        }

        public void StopDetectingGPSLocation() {
            CancelInvoke(nameof(DetectVLLocation));
        }


        /// Debugging

        private void OnDrawGizmos()
        {
            Matrix4x4 poseMatrix = mainCamera.transform.localToWorldMatrix;
            DebugUtility.DrawFrame(poseMatrix, Color.black, 1.5f);
        }
    }
}