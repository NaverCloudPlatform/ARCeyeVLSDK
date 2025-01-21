using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ARCeye
{
    // [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    [StructLayout(LayoutKind.Sequential)]
    public struct VLURLNative
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string location;
        [MarshalAs(UnmanagedType.LPStr)]
        public string invokeUrl;
        [MarshalAs(UnmanagedType.LPStr)]
        public string secretKey;
    }
}