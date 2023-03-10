using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARCeye
{
    public class VLSDKManager : MonoBehaviour, IGPSLocationRequester
    {
        private PoseTracker m_PoseTracker;
        private NetworkController m_NetworkController;
        private GeoCoordProvider m_GeoCoordProvider;


#if UNITY_IOS
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif
        private static VLSDKManager s_Instance;
        
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

        [SerializeField]
        private ChangedLocationEvent m_OnLocationChanged;
        public  ChangedLocationEvent OnLocationChanged {
            get => m_OnLocationChanged;
            set {
                m_PoseTracker.onLocationChanged = value;
                m_OnLocationChanged = value;
            }
        }

        [SerializeField]
        private ChangedBuildingEvent m_OnBuildingChanged; 
        public ChangedBuildingEvent OnBuildingChanged {
            get => m_OnBuildingChanged;
            set {
                m_PoseTracker.onBuildingChanged = value;
                m_OnBuildingChanged = value;
            }
        }

        [SerializeField]
        private ChangedFloorEvent m_OnFloorChanged; 
        public ChangedFloorEvent OnFloorChanged {
            get => m_OnFloorChanged;
            set {
                m_PoseTracker.onFloorChanged = value;
                m_OnFloorChanged = value;
            }
        }

        [SerializeField]
        private ChangedRegionCodeEvent m_OnRegionCodeChanged; 
        public ChangedRegionCodeEvent OnRegionCodeChanged {
            get => m_OnRegionCodeChanged;
            set {
                m_PoseTracker.onRegionCodeChanged = value;
                m_OnRegionCodeChanged = value;
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
        private DetectedObjectEvent m_OnObjectDetected;
        public  DetectedObjectEvent OnObjectDetected {
            get => m_OnObjectDetected;
            set {
                m_PoseTracker.onObjectDetected = value;
                m_OnObjectDetected = value;
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

                m_OnStateChanged.AddListener(logViewer.OnStateChanged);
                m_OnLocationChanged.AddListener(logViewer.OnLocationChanged);
                m_OnBuildingChanged.AddListener(logViewer.OnBuildingChanged);
                m_OnFloorChanged.AddListener(logViewer.OnFloorChanged);
                m_OnRegionCodeChanged.AddListener(logViewer.OnRegionCodeChanged);
                m_OnPoseUpdated.AddListener(logViewer.OnPoseUpdated);
            }
        }

        private void InitCamera()
        {
            if(Camera.main == null) {
                Debug.LogError("Main Camera??? ?????? ??? ????????????.");
            }
            m_ARCamera = Camera.main.transform;
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

            m_PoseTracker.Initialize(m_ARCamera, m_Config);
        }

        private void Start()
        {
#if UNITY_EDITOR         
            // Editor ????????? ????????? ??? ??? ????????? Texture provider ??????.
            // Texture provider?????? ???????????? ?????? ???????????? preview??? ??????????????? VL ????????? ?????????.
            var textureProvider = GetComponent<TextureProvider>();
            (m_PoseTracker as EditorPoseTracker).textureProvider = textureProvider;

            DebugPreview preview = m_ARCamera.GetComponentInChildren<DebugPreview>(true);
            if(preview == null) {
                Debug.LogError("DebugPreview??? ?????? ??? ????????????. ????????? ?????? ??? Main Camera??? ?????? ?????? ?????? ???????????? ??????????????????");
            }
            preview.SetTexture(textureProvider.textureToSend);
#endif

            m_OnPoseUpdated.AddListener(UpdateOriginPose);

            m_PoseTracker.onStateChanged = m_OnStateChanged;
            m_PoseTracker.onPoseUpdated = m_OnPoseUpdated;
            m_PoseTracker.onLocationChanged = m_OnLocationChanged;
            m_PoseTracker.onBuildingChanged = m_OnBuildingChanged;
            m_PoseTracker.onFloorChanged = m_OnFloorChanged;
            m_PoseTracker.onRegionCodeChanged = m_OnRegionCodeChanged;
            m_PoseTracker.onObjectDetected = m_OnObjectDetected;

            m_PoseTracker.SetGPSLocationRequester(this);
            StartDetectingGPSLocation();

            StartSession();
        }

        private void OnDisable() {
            StopSession();
        }

        private void OnDestroy()
        {
            m_PoseTracker.Release();
        }

        private void UpdateOriginPose(Matrix4x4 localizedViewMatrix)
        {
            // VL ?????? ????????? WC Transform Matrix ??????.
            Matrix4x4 localizedPoseMatrix = Matrix4x4.Inverse(localizedViewMatrix);

            // ??????????????? AR SDK session??? ???????????? ?????? AR Camera??? Local Transform Matrix ??????.
            Transform camTrans = Camera.main.transform;
            Matrix4x4 lhCamModelMatrix = Matrix4x4.TRS(camTrans.localPosition, camTrans.localRotation, Vector3.one);

            // ?????? ??? Matrix??? ???????????? AR Session Origin??? WC Transform Matrix ??????.
            Matrix4x4 originModelMatrix = localizedPoseMatrix * Matrix4x4.Inverse(lhCamModelMatrix);

            // ?????? ??? Origin??? Transform Matrix??? ????????? OriginTransform??? position, rotation ??????.
            m_Origin.localPosition = originModelMatrix.GetColumn(3);
            m_Origin.localRotation = originModelMatrix.rotation;
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
        }

        /* -- GPS Location Requester -- */
        public void StartDetectingGPSLocation() {
            if(!m_Config.useGPSGuide) {
                return;
            }

            // Tracking state??? initial??? ???????????? 1?????? ??? ?????? GPS ?????? VL Location Id ??????.
            CancelInvoke(nameof(DetectVLLocation));
            InvokeRepeating(nameof(DetectVLLocation), 1.0f, 1.0f);
        }

        public void StopDetectingGPSLocation() {
            CancelInvoke(nameof(DetectVLLocation));
        }
    }
}