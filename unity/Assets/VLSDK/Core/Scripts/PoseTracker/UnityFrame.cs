using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;


namespace ARCeye
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityFrame
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] viewMatrix;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] projMatrix;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public float[] texTrans;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] geoCoord;

        public float realHeight;

        public UnityYuvCpuImage yuvBuffer;

        public IntPtr textureBuffer;
    }
}