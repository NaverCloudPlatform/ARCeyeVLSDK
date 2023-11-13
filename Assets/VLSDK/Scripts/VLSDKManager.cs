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
        private bool m_PlayOnAwake;
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
        private ChangedStateEvent m_OnStateChanged;
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
        private ChangedLayerInfoEvent m_OnLayerInfoChanged; 
        public ChangedLayerInfoEvent OnLayerInfoChanged {
            get => m_OnLayerInfoChanged;
            set {
                m_PoseTracker.onLayerInfoChanged = value;
                m_OnLayerInfoChanged = value;
            }
        }

        [SerializeField]
        private UpdatedPoseEvent m_OnPoseUpdated;
        public  UpdatedPoseEvent OnPoseUpdated {
            get => m_OnPoseUpdated;
            set {
                m_PoseTracker.onPoseUpdated = value;
                m_OnPoseUpdated = value;
            }
        }

        [SerializeField]
        private UpdatedGeoCoordEvent m_OnGeoCoordUpdated;
        public  UpdatedGeoCoordEvent OnGeoCoordUpdated {
            get => m_OnGeoCoordUpdated;
            set {
                m_PoseTracker.onGeoCoordUpdated = value;
                m_OnGeoCoordUpdated = value;
            }
        }

        [SerializeField]
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

        //// Lifecycle

        private void Awake()
        {
            s_Instance = this;

            InitConfig();
            InitLogViewer();
            InitCamera();
            InitPoseTracker();
        }

        private void InitConfig()
        {
            if(m_Config == null) {
                Debug.LogWarning("[VLSDKManager] Config is null. Use default Config setting");
                m_Config = new Config();
            }

            if(m_Settings.URLList.Count == 0) {
                Debug.LogWarning("[VLSDKManager] URL List is empty.");
                return;
            }

            m_Config.tracker.requestIntervalBeforeLocalization = m_Settings.vlIntervalInitial;
            m_Config.tracker.requestIntervalAfterLocalization = m_Settings.vlIntervalPassed;
            m_Config.logLevel = m_Settings.logLevel;
            m_Config.useGPSGuide = m_Settings.GPSGuide;
            m_Config.urlList = m_Settings.URLList;
            m_Config.vlAreaGeoJson = m_Settings.locationGeoJson;
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

        private void InitCamera()
        {
            if(mainCamera == null) {
                Debug.LogError("Main Camera를 찾을 수 없습니다.");
            }
            m_ARCamera = mainCamera.transform;
            m_Origin = m_ARCamera.parent;
        }

        private void InitPoseTracker()
        {
#if UNITY_EDITOR
            m_PoseTracker = new EditorPoseTracker();
#else
            m_PoseTracker = new DevicePoseTracker();
#endif
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

            m_PoseTracker.SetGeoCoordProvider(m_GeoCoordProvider);
            m_PoseTracker.Initialize(m_ARCamera, m_Config);
        }

        private void Start()
        {
#if UNITY_EDITOR         
            // Editor 상에서 개발을 할 때 사용할 Texture provider 할당.
            // Texture provider에서 전달하는 값을 이용하여 preview를 렌더링하고 VL 쿼리를 보낸다.
            var textureProvider = GetComponent<TextureProvider>();
            (m_PoseTracker as EditorPoseTracker).textureProvider = textureProvider;

            DebugPreview preview = m_ARCamera.GetComponentInChildren<DebugPreview>(true);
            if(preview == null) {
                Debug.LogError("DebugPreview를 찾을 수 없습니다. 기존에 추가 된 Main Camera가 있을 경우 해당 카메라를 제거해주세요");
            }
            preview.SetTexture(textureProvider.textureToSend);
#endif

            m_OnPoseUpdated.AddListener(UpdateOriginPose);

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

            if(m_Config.useGPSGuide && (latitude == 0 || longitude == 0)) {
                LogViewer.DebugLog(LogLevel.ERROR, "Failed to get GPS coordinate");
            }

            m_PoseTracker.DetectVLLocation(latitude, longitude, radius);
            m_OnGeoCoordUpdated.Invoke(latitude, longitude);
        }

        public string FindLocation(double latitude, double longitude) {
            return m_PoseTracker.FindVLLocation(latitude, longitude);
        }

        /* -- GPS Location Requester -- */
        public void StartDetectingGPSLocation() {
            if(!m_Config.useGPSGuide) {
                return;
            }

            // Tracking state가 initial일 경우에는 1초에 한 번씩 GPS 기반 VL Location Id 요청.
            CancelInvoke(nameof(DetectVLLocation));
            InvokeRepeating(nameof(DetectVLLocation), 1.0f, 1.0f);
        }

        public void StopDetectingGPSLocation() {
            CancelInvoke(nameof(DetectVLLocation));
        }
    }
}