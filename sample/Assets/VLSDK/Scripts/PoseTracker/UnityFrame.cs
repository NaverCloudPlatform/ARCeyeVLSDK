using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;


namespace ARCeye
{
[StructLayout(LayoutKind.Sequential)]
public class UnityFrame {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public float[] viewMatrix;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public float[] projMatrix;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
    public float[] texTrans;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public IntPtr  imageBuffer;

    public UnityFrame() {
        viewMatrix = new float[16];
        projMatrix = new float[16];
        texTrans = new float[9];
        imageBuffer = new IntPtr(0);
    }
}
}