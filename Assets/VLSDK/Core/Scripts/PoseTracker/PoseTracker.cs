using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;


namespace ARCeye
{
    public abstract class PoseTracker
    {


        /* -- Delegates -- */

        public delegate void PoseUpdateDelegate(IntPtr viewMatrix, IntPtr projMatrix, IntPtr image, IntPtr texTrans, double relativeAltitude);
        public delegate void InitialPoseReceivedDelegate(int count);
        public delegate void ChangeStateDelegate(int state);
        public delegate void ChangeLocationDelegate(IntPtr rawLocation);
        public delegate void ChangeBuildingDelegate(IntPtr rawBuilding);
        public delegate void ChangeFloorDelegate(IntPtr rawFloor);
        public delegate void ChangeLayerInfoDelegate(IntPtr rawRegionCode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DetectVLLocationNativeDelegate(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 1)]
        string[] locations,
            int length);

        public delegate void StartChangingFloorDelegate();
        public delegate void DetectObjectDelegate(DetectedObjectInfo objectInfo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ResponseVOTLocationsDelegate(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 1)]
        string[] locations,
            int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ResponseVOTMapIdsDelegate(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 1)]
        string[] mapIds,
            int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ResponseVOTObjectsDelegate(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 1)]
        string[] objectIds,
            int length);

        protected static PoseTracker s_Instance;

        protected Transform m_ARCamera;
        protected GeoCoordProvider m_GeoCoordProvider;

        protected UnityFrame m_Frame;
        protected bool m_IsInitialized = false;


        protected ChangedStateEvent m_OnStateChanged;
        public ChangedStateEvent onStateChanged
        {
            get => m_OnStateChanged;
            set => m_OnStateChanged = value;
        }

        protected ChangedLocationEvent m_OnLocationChanged;
        public ChangedLocationEvent onLocationChanged
        {
            get => m_OnLocationChanged;
            set => m_OnLocationChanged = value;
        }

        protected ChangedBuildingEvent m_OnBuildingChanged;
        public ChangedBuildingEvent onBuildingChanged
        {
            get => m_OnBuildingChanged;
            set => m_OnBuildingChanged = value;
        }

        protected ChangedFloorEvent m_OnFloorChanged;
        public ChangedFloorEvent onFloorChanged
        {
            get => m_OnFloorChanged;
            set => m_OnFloorChanged = value;
        }

        protected ChangedRegionCodeEvent m_OnRegionCodeChanged;
        public ChangedRegionCodeEvent onRegionCodeChanged
        {
            get => m_OnRegionCodeChanged;
            set => m_OnRegionCodeChanged = value;
        }

        protected ChangedLayerInfoEvent m_OnLayerInfoChanged;
        public ChangedLayerInfoEvent onLayerInfoChanged
        {
            get => m_OnLayerInfoChanged;
            set => m_OnLayerInfoChanged = value;
        }

        protected UpdatedPoseEvent m_OnPoseUpdated;
        public UpdatedPoseEvent onPoseUpdated
        {
            get => m_OnPoseUpdated;
            set => m_OnPoseUpdated = value;
        }

        protected UpdatedGeoCoordEvent m_OnGeoCoordUpdated;
        public UpdatedGeoCoordEvent onGeoCoordUpdated
        {
            get => m_OnGeoCoordUpdated;
            set => m_OnGeoCoordUpdated = value;
        }

        protected UpdatedRelAltitudeEvent m_OnRelAltitudeUpdated;
        public UpdatedRelAltitudeEvent onRelAltitudeUpdated
        {
            get => m_OnRelAltitudeUpdated;
            set => m_OnRelAltitudeUpdated = value;
        }

        protected DetectedObjectEvent m_OnObjectDetected;
        public DetectedObjectEvent onObjectDetected
        {
            get => m_OnObjectDetected;
            set => m_OnObjectDetected = value;
        }

        protected DetectedVLLocationEvent m_OnVLLocaitonDetected;
        public DetectedVLLocationEvent onVLLocationDetected
        {
            get => m_OnVLLocaitonDetected;
            set => m_OnVLLocaitonDetected = value;
        }

        protected IGPSLocationRequester m_GPSLocationRequester;
        public IGPSLocationRequester gpsLocationRequester => m_GPSLocationRequester;


        private bool m_IsIntrinsicAssigned = false;
        private float m_PrevFx, m_PrevFy, m_PrevCx, m_PrevCy;
        protected double m_CurrRelAltitude = Double.MinValue;

        protected TrackerState m_State;
        public TrackerState state => m_State;


        /* -- Native method -- */

#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif

        [DllImport(dll)]
        private static extern IntPtr GetVersionNative();

        [DllImport(dll)]
        private static extern void SetVLSDKConfigNative(TrackerConfig config);

        [DllImport(dll)]
        private static extern void InitVLSDKNative(TrackerConfig config, VLURLNative[] vlURLs, string vlAreaGeoJson, int size);

        [DllImport(dll)]
        private static extern TrackerConfig GetVLSDKConfigNative();

        [DllImport(dll)]
        private static extern void ResetVLSDKNative();

        [DllImport(dll)]
        private static extern void ReleaseVLSDKNative();

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetInvokeUrl(string invokeUrl, string secretKey);

        [DllImport(dll)]
        private static extern void EnableResetByDevicePoseNative(bool active);

        [DllImport(dll)]
        private static extern void SetPoseUpdateFuncNative(PoseUpdateDelegate func);

        [DllImport(dll)]
        private static extern void SetInitialPoseReceivedFuncNative(InitialPoseReceivedDelegate func);

        [DllImport(dll)]
        private static extern void SetChangeStateFuncNative(ChangeStateDelegate func);

        [DllImport(dll)]
        private static extern void SetChangeLocationFuncNative(ChangeLocationDelegate func);

        [DllImport(dll)]
        private static extern void SetChangeBuildingFuncNative(ChangeBuildingDelegate func);

        [DllImport(dll)]
        private static extern void SetChangeFloorFuncNative(ChangeFloorDelegate func);

        [DllImport(dll)]
        private static extern void SetChangeRegionCodeFuncNative(ChangeLayerInfoDelegate func);
        [DllImport(dll)]
        private static extern void SetChangeLayerInfoFuncNative(ChangeLayerInfoDelegate func);

        [DllImport(dll)]
        private static extern void SetDetectObjectFuncNative(DetectObjectDelegate func);


        [DllImport(dll)]
        private static extern void UpdateUnityFrameNative(UnityFrame uframe);

        [DllImport(dll)]
        private static extern void ChangeStateNative(int state);

        [DllImport(dll)]
        private static extern void SetCameraIntrinsicNative(float fx, float fy, float cx, float cy);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DetectVLLocationNative(double lat, double lon, double radius, DetectVLLocationNativeDelegate func);

        [DllImport(dll)]
        private static extern IntPtr FindLocationNative(double lat, double lon);

        public virtual void OnCreate(Config config) { }
        public abstract void RegisterFrameLoop();
        public abstract void UnregisterFrameLoop();


        public PoseTracker()
        {
            s_Instance = this;
        }


        /* -- Initialization -- */

        public void Initialize(Config config)
        {
            OnCreate(config);

            m_Frame = new UnityFrame();
            m_ARCamera = Camera.main.transform;

            InitNativeMethods();
            InitNativePlugin(config);

            m_IsInitialized = true;
            m_State = TrackerState.INITIAL;
        }

        public string GetVersion()
        {
            IntPtr strPtr = GetVersionNative();
            return Marshal.PtrToStringAnsi(strPtr);
        }

        public void SetTrackerConfig(TrackerConfig config)
        {
            SetVLSDKConfigNative(config);
        }

        public TrackerConfig GetTrackerConfig()
        {
            return GetVLSDKConfigNative();
        }

        public void SetGeoCoordProvider(GeoCoordProvider geoCoordProvider)
        {
            m_GeoCoordProvider = geoCoordProvider;
        }

        private void InitNativeMethods()
        {
            SetPoseUpdateFuncNative(OnPoseUpdated);
            SetInitialPoseReceivedFuncNative(OnInitialPoseReceived);
            SetChangeStateFuncNative(OnStateChanged);
            SetChangeLayerInfoFuncNative(OnLayerInfoChanged);
            SetDetectObjectFuncNative(OnObjectDetected);

            // deprecate 예정.
            SetChangeLocationFuncNative(OnLocationChanged);
            SetChangeBuildingFuncNative(OnBuildingChanged);
            SetChangeFloorFuncNative(OnFloorChanged);
            SetChangeRegionCodeFuncNative(OnLayerInfoChanged);
        }

        private void InitNativePlugin(Config config)
        {
            string geojson = config.vlAreaGeoJson;
            if (!config.tracker.useGPSGuide)
            {
                geojson = null;
            }

            var nativeURLs = GetVLURLNative(config);

            InitVLSDKNative(config.tracker, nativeURLs.ToArray(), geojson, nativeURLs.Count);
        }

        private List<VLURLNative> GetVLURLNative(Config config)
        {
            List<VLURLNative> nativeURLs = new List<VLURLNative>();
            for (int i = 0; i < config.urlList.Count; i++)
            {
                VLURL url = config.urlList[i];

                if (url.Inactive)
                {
                    continue;
                }

                var nativeUrl = new VLURLNative();
                nativeUrl.location = string.IsNullOrEmpty(url.location) ? "_" : url.location;
                nativeUrl.invokeUrl = url.invokeUrl;
                nativeUrl.secretKey = url.secretKey;
                nativeURLs.Add(nativeUrl);
            }
            return nativeURLs;
        }

        public void Reset()
        {
            ResetVLSDKNative();
        }

        public virtual void Release()
        {
            ReleaseVLSDKNative();
        }


        /* -- VLSDK Control -- */

        public void ChangeState(TrackerState state)
        {
            ChangeStateNative((int)state);
        }

        public void DetectVLLocation(double lat, double lon, double radius)
        {
            DetectVLLocationNative(lat, lon, radius, OnVLLocationDetected);
        }

        public void EnableResetByDevicePose(bool value)
        {
            EnableResetByDevicePoseNative(value);
        }

        /// <summary>
        ///   위도, 경도값을 이용하여 하나의 location 값을 받아온다.
        /// </summary>
        public string FindVLLocation(double lat, double lon)
        {
            IntPtr rawStr = FindLocationNative(lat, lon);
            return Marshal.PtrToStringAnsi(rawStr);
        }

        /* -- Listeners -- */

        public void SetGPSLocationRequester(IGPSLocationRequester listener)
        {
            m_GPSLocationRequester = listener;
        }


        /* -- Frame loop -- */

        unsafe protected void UpdateFrame(ARFrame frame)
        {
            if (frame.yuvBuffer == null && frame.texture == null)
            {
                Debug.LogError("Failed to acquire a requested texture");
                return;
            }

            // 카메라 intrinsic 값 설정.
            float fx = frame.intrinsic.fx;
            float fy = frame.intrinsic.fy;
            float cx = frame.intrinsic.cx;
            float cy = frame.intrinsic.cy;

            if (!m_IsIntrinsicAssigned ||
                (m_PrevFx != fx || m_PrevFy != fy || m_PrevCx != cx || m_PrevCy != cy))
            {
                SetCameraIntrinsicNative(fx, fy, cx, cy);

                m_PrevFx = fx;
                m_PrevFy = fy;
                m_PrevCx = cx;
                m_PrevCy = cy;

                m_IsIntrinsicAssigned = true;
            }

            // viewMatrix 설정.
            Matrix4x4 lhCamModelMatrix = Matrix4x4.TRS(frame.localPosition, frame.localRotation, Vector3.one);
            Matrix4x4 camModelMatrix = PoseUtility.ConvertLHRH(lhCamModelMatrix);
            Matrix4x4 viewMatrix = Matrix4x4.Inverse(camModelMatrix).transpose;

            // projMatrix 설정.
            Matrix4x4 projMatrix = frame.projMatrix;

            // displayMatrix 설정.
            Matrix4x4 displayMatrix = frame.displayMatrix;

            // Matrix4x4 to float[].
            float[] v = viewMatrix.ToData();
            float[] p = projMatrix.ToData();
            float[] t = displayMatrix.ToData3x3();

            p[14] = 1;  // Left Handed to Right Handed.

            // LocationInfo 설정.
            LocationInfo locationInfo = new LocationInfo(0, 0);

            if (m_GeoCoordProvider)
            {
                locationInfo = m_GeoCoordProvider.info;
            }

            // 요청을 위한 UnityFrame 초기화.
            m_Frame.viewMatrix = v;
            m_Frame.projMatrix = p;
            m_Frame.texTrans = t;
            m_Frame.realHeight = 1.5f;
            m_Frame.geoCoord = new float[] { locationInfo.latitude, locationInfo.longitude };


            // YUV Buffer를 사용하는 경우.
            if (frame.yuvBuffer?.numberOfPlanes > 0)
            {
                m_Frame.yuvBuffer = (UnityYuvCpuImage)frame.yuvBuffer;
                UpdateUnityFrameNative(m_Frame);
                frame.disposable.Invoke();
            }
            // RGB Texture를 사용하는 경우.
            else if (frame.texture != null)
            {
                Texture requestedTexture = frame.texture;
                // native 영역의 update frame 호출. 
                GCHandle gcI = GCHandle.Alloc(requestedTexture, GCHandleType.Weak);
                m_Frame.textureBuffer = GCHandle.ToIntPtr(gcI);
                UpdateUnityFrameNative(m_Frame);
                gcI.Free();
            }
        }

        /* -- Callbacks from native -- */

        [MonoPInvokeCallback(typeof(InitialPoseReceivedDelegate))]
        private static void OnInitialPoseReceived(int count)
        {
            // 최초 인식 시 수신하는 pose 개수 정보는 불필요한 관계로 deprecate.
        }

        [MonoPInvokeCallback(typeof(ChangeStateDelegate))]
        private static void OnStateChanged(int state)
        {
            TrackerState tstate = (TrackerState)state;
            s_Instance.m_State = tstate;

            if (s_Instance.onStateChanged != null)
            {
                s_Instance.onStateChanged.Invoke(tstate);
            }

            if (tstate == TrackerState.VL_PASS)
            {
                s_Instance.gpsLocationRequester?.StopDetectingGPSLocation();
            }
            else if (tstate == TrackerState.VL_FAIL)
            {
                s_Instance.gpsLocationRequester?.StartDetectingGPSLocation();
            }
        }

        [MonoPInvokeCallback(typeof(ChangeLocationDelegate))]
        private static void OnLocationChanged(IntPtr rawLocation)
        {
            string location = Marshal.PtrToStringAnsi(rawLocation);
            if (s_Instance.onLocationChanged != null)
            {
                s_Instance.onLocationChanged.Invoke(location);
            }
        }

        [MonoPInvokeCallback(typeof(ChangeBuildingDelegate))]
        private static void OnBuildingChanged(IntPtr rawBuilding)
        {
            string building = Marshal.PtrToStringAnsi(rawBuilding);
            if (s_Instance.onBuildingChanged != null)
            {
                s_Instance.onBuildingChanged.Invoke(building);
            }
        }

        [MonoPInvokeCallback(typeof(ChangeFloorDelegate))]
        private static void OnFloorChanged(IntPtr rawFloor)
        {
            string floor = Marshal.PtrToStringAnsi(rawFloor);
            if (s_Instance.onFloorChanged != null)
            {
                s_Instance.onFloorChanged.Invoke(floor);
            }
        }

        [MonoPInvokeCallback(typeof(ChangeLayerInfoDelegate))]
        private static void OnLayerInfoChanged(IntPtr rawRegionCode)
        {
            string layerInfo = Marshal.PtrToStringAnsi(rawRegionCode);
            if (s_Instance.onRegionCodeChanged != null)
            {
                s_Instance.onRegionCodeChanged.Invoke(layerInfo);
            }
            if (s_Instance.onLayerInfoChanged != null)
            {
                s_Instance.onLayerInfoChanged.Invoke(layerInfo);
            }
        }

        [MonoPInvokeCallback(typeof(PoseUpdateDelegate))]
        private static void OnPoseUpdated(IntPtr vm, IntPtr pm, IntPtr im, IntPtr tx, double ra)
        {
            // M_loc = X * M_vio
            // X = M_loc * M_vio^-1
            // X^-1 = M_vio * M_loc^-1

            Matrix4x4 rhLocalizedViewMatrix = PoseUtility.UnmanagedToMatrix4x4<double>(vm);
            Matrix4x4 localizedViewMatrix = PoseUtility.ConvertLHRHView(rhLocalizedViewMatrix);
            Matrix4x4 localizedPoseMatrix = Matrix4x4.Inverse(localizedViewMatrix);

            Matrix4x4 projectionMatrix = PoseUtility.UnmanagedToMatrix4x4<float>(pm);

            Matrix4x4 texMatrix = PoseUtility.UnmanagedToMatrix4x4From3x3(tx);

            double relativeAltitude = ra;

            s_Instance.onPoseUpdated?.Invoke(localizedViewMatrix, projectionMatrix, texMatrix, relativeAltitude);

            // relativeAltitude 값이 갱신 될때마다 이벤트 호출.
            if (s_Instance.m_CurrRelAltitude != relativeAltitude)
            {
                // 데이터셋인 경우. Native 영역에서 항상 값이 0으로 전달되고 m_CurrRelAltitude 값은 EditorPoseTracker에서 갱신됨.
                if (relativeAltitude == 0)
                {
                    s_Instance.onRelAltitudeUpdated?.Invoke(s_Instance.m_CurrRelAltitude);
                }
                // 실제 기기인 경우. Native 영역에서 항상 값이 갱신됨.
                else
                {
                    s_Instance.m_CurrRelAltitude = relativeAltitude;
                    s_Instance.onRelAltitudeUpdated?.Invoke(relativeAltitude);
                }
            }
        }

        [MonoPInvokeCallback(typeof(DetectObjectDelegate))]
        unsafe protected static void OnObjectDetected(DetectedObjectInfo objectInfo)
        {
            // Convert a left-handed camera model matrix to right-handed.
            Matrix4x4 rhWCObjectPoseMatrix = PoseUtility.FloarArrayToMatrix4x4(objectInfo.modelMatrix);
            Matrix4x4 objectPoseMatrix = PoseUtility.ConvertLHRH(rhWCObjectPoseMatrix);

            Vector3 position = objectPoseMatrix.GetColumn(3);
            Quaternion rotation = Quaternion.LookRotation(objectPoseMatrix.GetColumn(2), objectPoseMatrix.GetColumn(1));
            Vector3 scale = new Vector3(
                objectPoseMatrix.GetColumn(0).magnitude,
                objectPoseMatrix.GetColumn(1).magnitude,
                objectPoseMatrix.GetColumn(2).magnitude
            );

            DetectedObject detectedObject = new DetectedObject(objectInfo.name, position, rotation, scale);
            s_Instance.onObjectDetected?.Invoke(detectedObject);
        }

        [MonoPInvokeCallback(typeof(DetectVLLocationNativeDelegate))]
        unsafe protected static void OnVLLocationDetected(string[] locations, int length)
        {
            s_Instance.onVLLocationDetected?.Invoke(locations);
        }
    }
}