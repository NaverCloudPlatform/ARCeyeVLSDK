using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace ARCeye
{
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public class RequestVLInfo {
    [MarshalAs(UnmanagedType.LPStr)]
    public string method;
    [MarshalAs(UnmanagedType.LPStr)]
    public string location;
    [MarshalAs(UnmanagedType.LPStr)]
    public string url;
    [MarshalAs(UnmanagedType.LPStr)]
    public string secretKey;
    [MarshalAs(UnmanagedType.LPStr)]
    public string fieldName;
    [MarshalAs(UnmanagedType.LPStr)]
    public string filename;
    public IntPtr rawImage;
    [MarshalAs(UnmanagedType.LPStr)]
    public string cameraParam;
    [MarshalAs(UnmanagedType.LPStr)]
    public string odometry;
    [MarshalAs(UnmanagedType.LPStr)]
    public string lastPose;
    public IntPtr nativeNetworkServiceHandle;
    [MarshalAs(UnmanagedType.U1)]
    public bool requestWithPosition;
    [MarshalAs(UnmanagedType.U1)]
    public bool withGlobal;
}
}