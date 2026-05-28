using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye.Dataset.Recorder
{
    public class TimeUtility
    {
        public static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);
            return formattedTime;
        }

        public static long GetUnixTime()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTime = now.ToUnixTimeMilliseconds();
            return unixTime;
        }

        public static string GetCurrentTimeString()
        {
            DateTime now = DateTime.Now;
            string formattedTime = now.ToString("yyMMdd_HHmmss");
            return formattedTime;
        }
    }
}