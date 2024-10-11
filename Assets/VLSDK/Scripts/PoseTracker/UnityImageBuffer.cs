using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;


namespace ARCeye
{
[StructLayout(LayoutKind.Sequential)]
public struct UnityImageBuffer {
    public IntPtr pixels;
    public int length;
}

}
