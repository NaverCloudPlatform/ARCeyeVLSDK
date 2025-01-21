using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    public class MultiTouchDetector
    {
        private float m_MaxTimeBetweenTouches = 0.3f;
        private int m_RequiredTouchCount = 3;
        private float m_LastTouchTime;
        private int m_TouchCount;

        private int touchCount {
            get {
#if UNITY_EDITOR
                bool isTouched = Input.GetMouseButton(0);
                if(Input.GetKey(KeyCode.LeftAlt)) {
                    return isTouched ? 2 : 0;
                } else {
                    return isTouched ? 1 : 0;
                }
#else
                return Input.touchCount;
#endif
            }
        }

        private bool mouseButtonDown {
            get {
#if UNITY_EDITOR
                return Input.GetMouseButtonDown(0);
#else
                return Input.GetTouch(0).phase == TouchPhase.Began;
#endif
            }
        }
        
        public bool CheckMultiTouch()
        {
            if (touchCount > 0 && mouseButtonDown)
            {
                float currentTime = Time.time;
                
                if (currentTime - m_LastTouchTime <= m_MaxTimeBetweenTouches)
                {
                    m_TouchCount++;
                }
                else
                {
                    m_TouchCount = 1;
                }

                m_LastTouchTime = currentTime;
                
                if (m_TouchCount >= m_RequiredTouchCount)
                {
                    m_TouchCount = 0;
                    return true;
                }
            }
            return false;
        }
    }
}