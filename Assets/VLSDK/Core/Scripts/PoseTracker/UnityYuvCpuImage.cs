using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;


namespace ARCeye
{
[StructLayout(LayoutKind.Sequential)]
public struct UnityYuvCpuImage {
    public int width;
    public int height;
    public int format;
    public int numberOfPlanes;

    public IntPtr yPixels;
    public int yLength;
    public int yRowStride;
    public int yPixelStride;

    public IntPtr uPixels;
    public int uLength;
    public int uRowStride;
    public int uPixelStride;

    public IntPtr vPixels;
    public int vLength;
    public int vRowStride;
    public int vPixelStride;
}

}
