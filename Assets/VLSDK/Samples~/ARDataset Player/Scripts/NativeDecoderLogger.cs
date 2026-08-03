using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace ARCeye.Dataset
{
    public class NativeDecoderLogger
    {
#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "ARDecodeSDK";
#endif

        [DllImport(dll)]
        private static extern void RegisterUnityLogger(LogCallback infoCallback, LogCallback errorCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

        private static LogCallback m_InfoLogCallback;
        private static LogCallback m_ErrorLogCallback;


        public static void Initialize()
        {
#if !UNITY_EDITOR_OSX
            m_InfoLogCallback = NativeInfoLogHandler;
            m_ErrorLogCallback = NativeErrorLogHandler;

            RegisterUnityLogger(m_InfoLogCallback, m_ErrorLogCallback);
#else
            // OSX 환경이 아닌 경우에는 NativeLogger를 지원하지 않음.
#endif
        }

        private static void NativeInfoLogHandler(string msg)
        {
            UnityEngine.Debug.Log("[Native] " + msg);
        }

        private static void NativeErrorLogHandler(string msg)
        {
            UnityEngine.Debug.LogError("[Native] " + msg);
        }
    }
}