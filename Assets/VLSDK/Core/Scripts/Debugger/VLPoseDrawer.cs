using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    public class VLPoseDrawer : MonoBehaviour
    {
        private List<Matrix4x4> m_VLResponses = new List<Matrix4x4>();
        private List<float> m_AccuracyList = new List<float>();
        private bool m_ShowVLPose = false;

        public void EnableVLPose(bool value)
        {
            m_ShowVLPose = value;
        }

        public void AddRawVLPose(VLResponseEventData responseData)
        {
            if (!responseData.IsVLPassed)
            {
                return;
            }

            Matrix4x4 vlPose = Matrix4x4.TRS(responseData.VLPosition, responseData.VLRotation, Vector3.one);

            m_VLResponses.Add(vlPose);
            m_AccuracyList.Add(responseData.Confidence);
        }

        void OnDrawGizmos()
        {
            if (!m_ShowVLPose)
            {
                return;
            }

            const float low1Accuracy = 0.1f;
            const float low2Accuracy = 0.2f;
            const float med1Accuracy = 0.4f;
            const float med2Accuracy = 0.5f;
            const float highAccuracy = 0.6f;

            for (int i = 0; i < m_VLResponses.Count; i++)
            {
                Color frameColor = Color.red;

                if (m_AccuracyList[i] > highAccuracy)
                {
                    frameColor = new Color32(0, 255, 0, 255);
                }
                else if (m_AccuracyList[i] > med2Accuracy)
                {
                    frameColor = new Color32(128, 255, 0, 255);
                }
                else if (m_AccuracyList[i] > med1Accuracy)
                {
                    frameColor = new Color32(255, 255, 0, 255);
                }
                else if (m_AccuracyList[i] > low2Accuracy)
                {
                    frameColor = new Color32(255, 128, 0, 255);
                }
                else
                {
                    frameColor = new Color32(255, 0, 0, 255);
                }

                DebugUtility.DrawFrame(m_VLResponses[i], frameColor, 1.0f);
            }
        }
    }
}