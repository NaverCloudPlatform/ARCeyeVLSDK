using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;


namespace ARCeye
{
    public enum YuvRotationMode
    {
        YUV_ROTATION_0 = 0,
        YUV_ROTATION_90 = 90,
        YUV_ROTATION_180 = 180,
        YUV_ROTATION_270 = 270
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnityYuvCpuImage
    {
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

        public YuvRotationMode rotationMode;
    }

}
