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

    public delegate void PoseUpdateDelegate(IntPtr viewMatrix, IntPtr projMatrix, IntPtr image, IntPtr texTrans);
    public delegate void ChangeStateDelegate(int state);
    public delegate void ChangeLocationDelegate(IntPtr rawLocation);
    public delegate void ChangeBuildingDelegate(IntPtr rawBuilding);
    public delegate void ChangeFloorDelegate(IntPtr rawFloor);
    public delegate void ChangeRegionCodeDelegate(IntPtr rawRegionCode);

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

    protected Config m_Config;
    protected UnityFrame m_Frame;
    protected bool m_IsInitialized = false;

    protected ChangedStateEvent m_OnStateChanged;
    public ChangedStateEvent onStateChanged {
        get => m_OnStateChanged;
        set => m_OnStateChanged = value;
    }

    protected ChangedLocationEvent m_OnLocationChanged;
    public ChangedLocationEvent onLocationChanged {
        get => m_OnLocationChanged;
        set => m_OnLocationChanged = value;
    }

    protected ChangedBuildingEvent m_OnBuildingChanged;
    public ChangedBuildingEvent onBuildingChanged {
        get => m_OnBuildingChanged;
        set => m_OnBuildingChanged = value;
    }

    protected ChangedFloorEvent m_OnFloorChanged;
    public ChangedFloorEvent onFloorChanged {
        get => m_OnFloorChanged;
        set => m_OnFloorChanged = value;
    }

    protected ChangedRegionCodeEvent m_OnRegionCodeChanged;
    public ChangedRegionCodeEvent onRegionCodeChanged {
        get => m_OnRegionCodeChanged;
        set => m_OnRegionCodeChanged = value;
    }

    protected UpdatedPoseEvent m_OnPoseUpdated;
    public  UpdatedPoseEvent onPoseUpdated {
        get => m_OnPoseUpdated;
        set => m_OnPoseUpdated = value;
    }

    protected DetectedObjectEvent m_OnObjectDetected;
    public DetectedObjectEvent onObjectDetected {
        get => m_OnObjectDetected;
        set => m_OnObjectDetected = value;
    }

    protected DetectedVLLocationEvent m_OnVLLocaitonDetected;
    public DetectedVLLocationEvent onVLLocationDetected {
        get => m_OnVLLocaitonDetected;
        set => m_OnVLLocaitonDetected = value;
    }

    protected IGPSLocationRequester m_GPSLocationRequester;
    public    IGPSLocationRequester gpsLocationRequester => m_GPSLocationRequester;


    private bool m_IsIntrinsicAssigned = false;
    private float m_PrevFx, m_PrevFy, m_PrevCx, m_PrevCy;


    /* -- Native method -- */

#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif

    [DllImport(dll)]
    private static extern void SetVLSDKConfigNative(TrackerConfig config);
    
    [DllImport(dll)]
    private static extern void InitVLSDKNative(TrackerConfig config);

    [DllImport(dll)]
    private static extern TrackerConfig GetVLSDKConfigNative();

    [DllImport(dll)]
    private static extern void ResetVLSDKNative();

    [DllImport(dll)]
    private static extern void ReleaseVLSDKNative();

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetInvokeUrl(string invokeUrl, string secretKey);

    [DllImport(dll)]
    private static extern void SetPoseUpdateFuncNative(PoseUpdateDelegate func);

    [DllImport(dll)]
    private static extern void SetChangeStateFuncNative(ChangeStateDelegate func);

    [DllImport(dll)]
    private static extern void SetChangeLocationFuncNative(ChangeLocationDelegate func);

    [DllImport(dll)]
    private static extern void SetChangeBuildingFuncNative(ChangeBuildingDelegate func);

    [DllImport(dll)]
    private static extern void SetChangeFloorFuncNative(ChangeFloorDelegate func);

    [DllImport(dll)]
    private static extern void SetChangeRegionCodeFuncNative(ChangeRegionCodeDelegate func);

    [DllImport(dll)]
    private static extern void SetDetectObjectFuncNative(DetectObjectDelegate func);

    
    [DllImport(dll)]
    private static extern void UpdateUnityFrameNative(UnityFrame uframe);

    [DllImport(dll)]
    private static extern void ChangeStateNative(int state);

    [DllImport(dll)]
    private static extern void SetCameraIntrinsicNative(float fx, float fy, float cx, float cy);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void InitVLURLCandidatesNative(VLURLNative[] vlURLs, string vlAreaGeoJson, int size);

    [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
    private static extern void DetectVLLocationNative(double lat, double lon, double radius, DetectVLLocationNativeDelegate func);

    public abstract void Initialize(Transform arCamera, Config config);
    public abstract void RegisterFrameLoop();
    public abstract void UnregisterFrameLoop();
    public abstract bool AcquireRequestedTexture(out Texture texture);
    public abstract void AquireCameraIntrinsic(out float fx, out float fy, out float cx, out float cy);
    public abstract float[] MakeDisplayRotationMatrix(Matrix4x4 rawDispRotMatrix);



    public PoseTracker()
    {
        s_Instance = this;
    }


    /* -- Initialization -- */

    public void Initialize(Config config)
    {
        m_Config = config;
        m_Frame = new UnityFrame();

        InitNativeMethods();
        InitVLURLCandidates(config);

        m_IsInitialized = true;
    }

    public void SetTrackerConfig(TrackerConfig config) {
        SetVLSDKConfigNative(config);
    }

    private void InitNativeMethods()
    {
        SetPoseUpdateFuncNative(OnPoseUpdated);
        SetChangeStateFuncNative(OnStateChanged);
        SetChangeLocationFuncNative(OnLocationChanged);
        SetChangeBuildingFuncNative(OnBuildingChanged);
        SetChangeFloorFuncNative(OnFloorChanged);
        SetChangeRegionCodeFuncNative(OnRegionCodeChanged);
        SetDetectObjectFuncNative(OnObjectDetected);

        InitVLSDKNative(m_Config.tracker);
    }

    private void InitVLURLCandidates(Config config)
    {
        string geojson = config.vlAreaGeoJson;
        if(!config.useGPSGuide) {
            geojson = null;
        }

        List<VLURLNative> nativeURLs = new List<VLURLNative>();
        for(int i=0 ; i<config.urlList.Count ; i++) {
            VLURL url = config.urlList[i];

            if(url.isInactivated) {
                continue;
            }

            var nativeUrl = new VLURLNative();
            nativeUrl.location = url.location;
            nativeUrl.invokeUrl = url.invokeUrl;
            nativeUrl.secretKey = url.secretKey;
            nativeURLs.Add(nativeUrl);
        }

        InitVLURLCandidatesNative(nativeURLs.ToArray(), geojson, nativeURLs.Count);
    }

    public void Reset()
    {
        ResetVLSDKNative();
    }

    public void Release() 
    {
        ReleaseVLSDKNative();
    }


    /* -- VLSDK Control -- */

    public void ChangeState(TrackerState state) {
        ChangeStateNative((int) state);
    }

    public void DetectVLLocation(double lat, double lon, double radius) {
        DetectVLLocationNative(lat, lon, radius, OnVLLocationDetected);
    }

    /* -- Listeners -- */

    public void SetGPSLocationRequester(IGPSLocationRequester listener) {
        m_GPSLocationRequester = listener;
    }

    /* -- Frame loop -- */

    unsafe protected void UpdateFrame(Matrix4x4 projMatrix, Matrix4x4 transMatrix)
    {
        Texture requestImageTexture;
        if(!AcquireRequestedTexture(out requestImageTexture)) 
        {
            Debug.LogError("Failed to acquire a requested texture");
            return;
        }

        AssignCameraIntrinsicToNative();

        Matrix4x4 lhCamModelMatrix;
        if(Camera.main == null)
        {
            lhCamModelMatrix = Matrix4x4.identity;
        }
        else
        {
            Transform camTrans = Camera.main.transform;
            lhCamModelMatrix = Matrix4x4.TRS(camTrans.localPosition, camTrans.localRotation, Vector3.one);
        }
        

        Matrix4x4 camModelMatrix = PoseUtility.ConvertLHRH(lhCamModelMatrix);
        Matrix4x4 viewMatrix = Matrix4x4.Inverse(camModelMatrix).transpose;

        float[] v = viewMatrix.ToData();
        float[] p = projMatrix.ToData();
        p[14] = 1;  // Left Handed to Right Handed.

        float[] trans = MakeDisplayRotationMatrix(transMatrix);

        GCHandle gcI = GCHandle.Alloc(requestImageTexture, GCHandleType.Weak);

        m_Frame.viewMatrix = v;
        m_Frame.projMatrix = p;
        m_Frame.texTrans = trans;
        m_Frame.imageBuffer = GCHandle.ToIntPtr(gcI);

        UpdateUnityFrameNative(m_Frame);

        gcI.Free();
    }

    private void AssignCameraIntrinsicToNative() {
        float fx, fy, cx, cy;
        AquireCameraIntrinsic(out fx, out fy, out cx, out cy);

        if(!m_IsIntrinsicAssigned ||
            (m_PrevFx != fx || m_PrevFy != fy || m_PrevCx != cx || m_PrevCy != cy)) {
            SetCameraIntrinsicNative(fx, fy, cx, cy);

            m_PrevFx = fx;
            m_PrevFy = fy;
            m_PrevCx = cx;
            m_PrevCy = cy;

            m_IsIntrinsicAssigned = true;
        }
    }


    /* -- Callbacks from native -- */

    [MonoPInvokeCallback(typeof(ChangeStateDelegate))]
    private static void OnStateChanged(int state) 
    {
        TrackerState tstate = (TrackerState) state;
        if(s_Instance.onStateChanged != null) {
            s_Instance.onStateChanged.Invoke(tstate);
        }

        if(tstate == TrackerState.VL_PASS) {
            s_Instance.gpsLocationRequester?.StopDetectingGPSLocation();
        } else if(tstate == TrackerState.VL_FAIL) {
            s_Instance.gpsLocationRequester?.StartDetectingGPSLocation();
        }
    }

    [MonoPInvokeCallback(typeof(ChangeLocationDelegate))]
    private static void OnLocationChanged(IntPtr rawLocation)
    {
        string location = Marshal.PtrToStringAnsi(rawLocation);
        if(s_Instance.onLocationChanged != null) {
            s_Instance.onLocationChanged.Invoke(location);
        }
    }

    [MonoPInvokeCallback(typeof(ChangeBuildingDelegate))]
    private static void OnBuildingChanged(IntPtr rawBuilding)
    {
        string building = Marshal.PtrToStringAnsi(rawBuilding);
        if(s_Instance.onBuildingChanged != null) {
            s_Instance.onBuildingChanged.Invoke(building);
        }
    }

    [MonoPInvokeCallback(typeof(ChangeFloorDelegate))]
    private static void OnFloorChanged(IntPtr rawFloor)
    {
        string floor = Marshal.PtrToStringAnsi(rawFloor);
        if(s_Instance.onFloorChanged != null) {
            s_Instance.onFloorChanged.Invoke(floor);
        }
    }

    [MonoPInvokeCallback(typeof(ChangeRegionCodeDelegate))]
    private static void OnRegionCodeChanged(IntPtr rawRegionCode)
    {
        string regionCode = Marshal.PtrToStringAnsi(rawRegionCode);
        if(s_Instance.onRegionCodeChanged != null) {
            s_Instance.onRegionCodeChanged.Invoke(regionCode);
        }
    }
    
    [MonoPInvokeCallback(typeof(PoseUpdateDelegate))]
    private static void OnPoseUpdated(IntPtr vm, IntPtr pm, IntPtr im, IntPtr tx)
    {
        // M_loc = X * M_vio
        // X = M_loc * M_vio^-1
        // X^-1 = M_vio * M_loc^-1

        Matrix4x4 rhLocalizedViewMatrix = PoseUtility.UnmanagedToMatrix4x4(vm);
        Matrix4x4 localizedViewMatrix = PoseUtility.ConvertLHRHView(rhLocalizedViewMatrix);
        Matrix4x4 localizedPoseMatrix = Matrix4x4.Inverse(localizedViewMatrix);

        s_Instance.onPoseUpdated?.Invoke(localizedViewMatrix);
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
    unsafe protected static void OnVLLocationDetected(string[] locations, int length) {
        s_Instance.onVLLocationDetected?.Invoke(locations);
    }
}
}