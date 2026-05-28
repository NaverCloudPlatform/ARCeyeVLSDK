using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace ARCeye
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DetectedObjectInfo
    {
        [MarshalAs(UnmanagedType.LPStr, SizeConst = 64)]
        public string name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] modelMatrix;
    }
}