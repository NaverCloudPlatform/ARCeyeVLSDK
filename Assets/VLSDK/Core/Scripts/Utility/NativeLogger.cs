using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.Events;

namespace ARCeye
{
    public class LogElem
    {
        public string str { get; set; }
        public LogLevel level { get; set; }
        public LogElem(LogLevel l, string s)
        {
            str = s;
            level = l;
        }
    }

    public class NativeLogger
    {
        public delegate void DebugLogFuncDelegate(LogLevel level, IntPtr message);

#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "VLSDK";
#endif

        [DllImport(dll)]
        private static extern void SetDebugLogFuncNative(DebugLogFuncDelegate func);
        [DllImport(dll)]
        private static extern void ReleaseLoggerNative();

        static private LogLevel s_LogLevel = LogLevel.DEBUG;
        public LogLevel logLevel
        { 
            get => s_LogLevel; 
            set => s_LogLevel = value;
        }


        static private UnityEvent<LogElem> s_OnLogAdded = new UnityEvent<LogElem>();
        public UnityEvent<LogElem> onLogAdded => s_OnLogAdded;


        public void Initialize()
        {
            SetDebugLogFuncNative(DebugLog);
        }

        public void Release()
        {
            ReleaseLoggerNative();
        }


        ///
        /// Debug Log 
        ///

        [MonoPInvokeCallback(typeof(DebugLogFuncDelegate))]
        static public void DebugLog(LogLevel logLevel, IntPtr raw)
        {
            string log = Marshal.PtrToStringAnsi(raw);
            DebugLog(logLevel, log);
        }

        static public void DebugLog(LogLevel logLevel, string log)
        {
            if(logLevel < s_LogLevel) {
                return;
            }
            
            string currTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string msg = string.Format("[{0}] {1}", currTime, log);
            switch (logLevel)
            {
                case LogLevel.DEBUG: {
                    msg = "[Debug] " + msg;
                    Debug.Log(msg);
                    break;
                }
                case LogLevel.INFO: {
                    msg = "[Info] " + msg;
                    Debug.Log(msg);
                    break;
                }
                case LogLevel.WARNING: {
                    msg = "[Warning] " + msg;
                    Debug.LogWarning(msg);
                    break;
                }
                case LogLevel.ERROR: {
                    msg = "[Error] " + msg;
                    Debug.LogError(msg);
                    break;
                }
                case LogLevel.FATAL: {
                    msg = "[Fatal] " + msg;
                    Debug.LogError(msg);
                    break;
                }
            }

            s_OnLogAdded?.Invoke(new LogElem(logLevel, msg));
        }
    }
}