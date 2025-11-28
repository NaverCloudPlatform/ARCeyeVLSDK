using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye
{
    public class RecordingViewController : MonoBehaviour
    {
        [SerializeField]
        private Recorder m_Recorder;

        [Header("UI")]
        [SerializeField]
        private Text m_ImageSizeText;
        [SerializeField]
        private Text m_PositionText;
        [SerializeField]
        private Text m_RotationText;
        [SerializeField]
        private Text m_IntrinsicText;
        [SerializeField]
        private Text m_RecordingTimeText;
        [SerializeField]
        private Button m_RecordingButton;

        [Header("Sprite")]
        [SerializeField]
        private Sprite m_RecordingSprite;
        [SerializeField]
        private Sprite m_StopSprite;

        private int m_RecordingTime = 0;
        private bool m_IsRecording = false;

        public Transform m_CameraTrans;


        private void Awake()
        {
            m_Recorder.OnFrameUpdated = OnFrameUpdated;
        }

        public void ToggleRecording()
        {
            // 레코딩 시작 전인 경우.
            if(!m_IsRecording)
            {
                StartRecording();
            }
            // 이미 레코딩 중인 경우.
            else
            {
                StopRecording();
            }

            m_IsRecording = !m_IsRecording;
        }

        private void StartRecording()
        {
            StartTimer();
            m_Recorder.StartRecording();
            m_RecordingButton.image.sprite = m_StopSprite;
        }

        private void StopRecording()
        {
            StopTimer();
            m_Recorder.StopRecording();
            m_RecordingButton.image.sprite = m_RecordingSprite;

            // 레코딩 된 파일을 zip으로 압축.
            string recordingPath = m_Recorder.recordingPath;
            string sourceDirectory = recordingPath;
            string destinationZipFilePath = recordingPath + ".zip";
            
            // 레코딩 디렉토리를 압축.
            ZipUtility.ZipDirectory(sourceDirectory, destinationZipFilePath);

            // 원본 파일 삭제.
            Directory.Delete(sourceDirectory, true);
        }

        private void StartTimer()
        {
            m_RecordingTime = 0;
            m_RecordingTimeText.text = TimeUtility.FormatTime(m_RecordingTime);

            InvokeRepeating(nameof(TickTimer), 1.0f, 1.0f);
        }

        private void TickTimer()
        {
            m_RecordingTime++;
            m_RecordingTimeText.text = TimeUtility.FormatTime(m_RecordingTime);
        }

        private void StopTimer()
        {
            CancelInvoke(nameof(TickTimer));
        }

        private void OnFrameUpdated(FrameData frameData)
        {
            Vector3 position = frameData.modelMatrix.GetColumn(3);
            Quaternion rotation = Quaternion.LookRotation(frameData.modelMatrix.GetColumn(2), frameData.modelMatrix.GetColumn(1));
            Vector3 euler = rotation.eulerAngles;

            m_ImageSizeText.text = $"width : {frameData.width} height : {frameData.height}";
            m_PositionText.text  = $"t: ({position.x.ToString("N1")},{position.y.ToString("N1")},{position.z.ToString("N1")})";
            m_RotationText.text  = $"r: ({euler.x.ToString("N1")},{euler.y.ToString("N1")},{euler.z.ToString("N1")})";
            m_IntrinsicText.text = $"fx : {frameData.fx:000.0}, fy : {frameData.fy:000.0}\ncx : {frameData.cx:000.0}, cy : {frameData.cy:000.0}";

#if UNITY_EDITOR
            m_CameraTrans.localPosition = position;
            m_CameraTrans.localRotation = rotation;
#endif
        }
    }
}