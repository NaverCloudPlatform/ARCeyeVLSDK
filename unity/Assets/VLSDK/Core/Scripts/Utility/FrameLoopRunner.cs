#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ARCeye
{
    public class FrameLoopRunner : MonoBehaviour
    {
        private static FrameLoopRunner? s_Instance = null;
        public static FrameLoopRunner? Instance
        {
            get
            {
                if (s_ApplicationQuitting)
                {
                    return null;
                }

                if (s_Instance == null)
                {
                    GameObject obj = new GameObject("FrameLoopRunner");
                    s_Instance = obj.AddComponent<FrameLoopRunner>();
                    DontDestroyOnLoad(obj);
                }
                return s_Instance;
            }
        }

        private static bool s_ApplicationQuitting = false;

        private bool m_IsLoopRunning = false;
        private Coroutine? m_LoopCoroutine;
        private UnityAction? m_UpdateFrameAction;
        private float m_TargetFrameInterval = 0f;


        private void OnDestroy()
        {
            s_ApplicationQuitting = true;
        }

        public void StartFrameLoop(UnityAction updateFrameAction, float targetFps = 0f)
        {
            m_UpdateFrameAction = updateFrameAction;
            m_TargetFrameInterval = targetFps > 0f ? 1f / targetFps : 0f;
            m_IsLoopRunning = true;
            if (m_LoopCoroutine != null)
            {
                StopCoroutine(m_LoopCoroutine);
                m_LoopCoroutine = null;
            }
            m_LoopCoroutine = StartCoroutine(FrameLoop());
        }

        public void StopFrameLoop()
        {
            m_IsLoopRunning = false;
            if (m_LoopCoroutine != null)
            {
                StopCoroutine(m_LoopCoroutine);
                m_LoopCoroutine = null;
            }
        }

        private IEnumerator FrameLoop()
        {
            if (m_UpdateFrameAction == null)
            {
                Debug.LogError("UpdateFrameAction is not assigned. Cannot start frame loop.");
                yield break;
            }

            float nextTickTime = Time.realtimeSinceStartup;

            while (m_IsLoopRunning)
            {
                m_UpdateFrameAction?.Invoke();

                if (m_TargetFrameInterval <= 0f)
                {
                    yield return null;
                    continue;
                }

                nextTickTime += m_TargetFrameInterval;
                float waitSeconds = nextTickTime - Time.realtimeSinceStartup;
                if (waitSeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(waitSeconds);
                }
                else
                {
                    // Frame processing took longer than the target interval; resync on next frame.
                    nextTickTime = Time.realtimeSinceStartup;
                    yield return null;
                }
            }
        }
    }
}