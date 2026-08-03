using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace ARCeye.Dataset
{
    public abstract class DatasetDecoder
    {
#if UNITY_IOS && !UNITY_EDITOR
        const string dll = "__Internal";
#else
        const string dll = "ARDecodeSDK";
#endif

        [DllImport(dll)]
        protected static extern void SetUnityTexture(IntPtr texPtr);

        [DllImport(dll)]
        private static extern bool GetTotalDuration(out double durationSec);

        [DllImport(dll)]
        private static extern bool SeekTo(double seconds);

        [DllImport(dll)]
        private static extern void ShutdownDecoder();


        protected int m_Width = 1920;
        protected int m_Height = 1080;

        protected double m_StartTime;
        protected double m_PausedPlaybackTime;   // 일시정지된 재생 위치(초).
        protected double m_Speed = 1.0;

        protected bool m_IsInitialized = false;
        protected bool m_IsPaused = false;

        protected double m_TotalDuration;
        protected float m_Progress;


        protected abstract void InitDecoder(string filePath);
        protected abstract void CreatePreviewTexture();
        protected abstract void ReleaseVideoTexture();
        public abstract Texture GetPreviewTexture();
        public abstract FrameData GetNextFrame();


        public void Initialize(string filePath)
        {
            InitDecoder(filePath);
            CreatePreviewTexture();

            if (GetTotalDuration(out m_TotalDuration))
            {
                Debug.Log("Total duration (sec): " + m_TotalDuration);
            }
            else
            {
                Debug.LogError("Failed to get total duration.");
            }

            // 재생 기준 시각 설정 (0초부터 재생).
            m_StartTime = AudioSettings.dspTime;
            m_IsPaused = false;
        }

        public void Release()
        {
            ShutdownDecoder();
            ReleaseVideoTexture();
        }

        public bool IsRunning()
        {
            return m_IsInitialized && !m_IsPaused;
        }

        // 현재 재생 위치(초). 재생 중이면 시간축 기반, 일시정지면 고정된 위치.
        protected double GetCurrentPlaybackTime()
        {
            if (m_IsPaused)
            {
                return m_PausedPlaybackTime;
            }

            return (AudioSettings.dspTime - m_StartTime) * m_Speed;
        }

        public void Play()
        {
            if (!m_IsPaused)
            {
                return;
            }

            // 일시정지된 위치에서 이어서 재생.
            m_StartTime = AudioSettings.dspTime - (m_PausedPlaybackTime / m_Speed);
            m_IsPaused = false;
        }

        public void Pause()
        {
            if (m_IsPaused)
            {
                return;
            }

            m_PausedPlaybackTime = (AudioSettings.dspTime - m_StartTime) * m_Speed;
            m_IsPaused = true;
        }

        public void SetSpeed(double value)
        {
            if (value <= 0) return;

            // 목표 시간이 아니라 '실제로 디코딩된 현재 위치'를 기준으로 재생 기준 시각을 재정렬.
            double actual = m_Progress * m_TotalDuration;
            m_Speed = value;

            if (m_IsPaused)
            {
                m_PausedPlaybackTime = actual;
            }
            else
            {
                m_StartTime = AudioSettings.dspTime - (actual / m_Speed);
            }
        }

        public bool Seek(double seconds)
        {
            if (!SeekTo(seconds))
            {
                return false;
            }

            if (m_IsPaused)
            {
                m_PausedPlaybackTime = seconds;
            }
            else
            {
                m_StartTime = AudioSettings.dspTime - (seconds / m_Speed);
            }

            return true;
        }

        public double GetTotalDuration()
        {
            return m_TotalDuration;
        }

        public float GetProgress()
        {
            return m_Progress;
        }
    }
}