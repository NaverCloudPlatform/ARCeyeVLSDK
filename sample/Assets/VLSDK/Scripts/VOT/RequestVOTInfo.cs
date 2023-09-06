using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace ARCeye
{
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public class RequestVOTInfo {
    [MarshalAs(UnmanagedType.LPStr)]
    public string method;
    [MarshalAs(UnmanagedType.LPStr)]
    public string url;
    [MarshalAs(UnmanagedType.LPStr)]
    public string authorization;
    public IntPtr rawImage;
    [MarshalAs(UnmanagedType.LPStr)]
    public string location;
    [MarshalAs(UnmanagedType.LPStr)]
    public string building;
    [MarshalAs(UnmanagedType.LPStr)]
    public string floor;
    [MarshalAs(UnmanagedType.LPStr)]
    public string mapId;
    [MarshalAs(UnmanagedType.LPStr)]
    public string timestamp;
    [MarshalAs(UnmanagedType.LPStr)]
    public string intrinsic;
    [MarshalAs(UnmanagedType.LPStr)]
    public string extrinsic;
    [MarshalAs(UnmanagedType.LPStr)]
    public string distort;
    public IntPtr nativeNetworkServiceHandle;
}
}