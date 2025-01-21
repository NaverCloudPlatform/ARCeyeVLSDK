using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ARCeye
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeVLResponseEventData
    {
        [MarshalAs(UnmanagedType.I8)]
        public long timestamp;

        [MarshalAs(UnmanagedType.I4)]
        public int statusCode;

        [MarshalAs(UnmanagedType.R4)]
        public float confidence;

        public IntPtr vlPose;
        public IntPtr message;
        public IntPtr responseBody;
    }
}