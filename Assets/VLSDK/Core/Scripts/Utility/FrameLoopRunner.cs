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


        private void OnDestroy()
        {
            s_ApplicationQuitting = true;
        }

        public void StartFrameLoop(UnityAction updateFrameAction)
        {
            m_UpdateFrameAction = updateFrameAction;
            m_IsLoopRunning = true;
            m_LoopCoroutine = StartCoroutine(FrameLoop());
        }

        public void StopFrameLoop()
        {
            m_IsLoopRunning = false;
            if (m_LoopCoroutine != null)
            {
                StopCoroutine(m_LoopCoroutine);
            }
        }

        private IEnumerator FrameLoop()
        {
            if (m_UpdateFrameAction == null)
            {
                Debug.LogError("UpdateFrameAction is not assigned. Cannot start frame loop.");
                yield break;
            }

            while (m_IsLoopRunning)
            {
                m_UpdateFrameAction?.Invoke();
                yield return null;
            }
        }
    }
}