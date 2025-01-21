using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye.Example
{
    public class VLSDKExample : MonoBehaviour
    {
        public ARCeye.VLSDKManager m_VLSDKManager;

        // View components.
        [SerializeField] private Text m_RequestCountText;
        [SerializeField] private Text m_VLPassCountText;
        [SerializeField] private Text m_VLFailCountText;
        [SerializeField] private Text m_MessageText;

        public RawImage m_RequestedTexture;
        private RectTransform m_RequestedTextureRT;

        private const int k_PosePrintInterval = 10;
        private int m_PosePrintCount = 0;

        private int m_RequestCount = 0;
        private int m_VLPassCount = 0;
        private int m_VLFailCount = 0;


        private void Awake()
        {
            m_RequestedTextureRT = m_RequestedTexture.GetComponent<RectTransform>();
        }

        // 각종 VLSDKManager 이벤트 수신.
        
        ///
        /// VLSDKManager의 OnVLPoseRequested(VLRequestEventData) 이벤트를 통해 실행되는 메서드. 
        /// VL 요청을 보낼때마다 호출됩니다.
        /// 요청에 사용된 정보를 VLRequestEventData 형태로 전달합니다.
        /// 
        /// VLRequestEventData:
        ///   * Url (string): 요청 시 사용된 Invoke URL
        ///   * SecretKey (string): 요청 시 사용된 SecretKey
        ///   * RequestTexture (Texture2D): 요청 이미지
        /// 
        public void OnVLPoseRequested(VLRequestEventData eventData)
        {
            // VL 요청 횟수 증가.
            m_RequestCount++;
            m_RequestCountText.text = $"Request Count: {m_RequestCount}";

            // VL 요청 이미지 시각화.
            float ratio = (float) eventData.RequestTexture.width / (float) eventData.RequestTexture.height;
            float height = m_RequestedTextureRT.sizeDelta.y;
            float width = height * ratio;

            m_RequestedTextureRT.sizeDelta = new Vector2(width, height);
            m_RequestedTexture.texture = eventData.RequestTexture;
        }

        ///
        /// VLSDKManager의 OnVLPoseResponded(VLResponseEventData) 이벤트를 통해 실행되는 메서드. 
        /// VL 요청에 대한 응답을 받을 때마다 호출됩니다.
        /// 요청에 사용된 정보를 VLResponseEventData 형태로 전달합니다.
        /// 
        /// VLResponseEventData:
        ///   * Timestamp (long): 요청을 보낸 순간의 타임스탬프
        ///   * Status (ResponseStatus): 응답 상태. VL 응답과 VLSDK 내부 상태를 바탕으로 최종적인 응답 상태를 전달한다.
        ///   * Message (string): 응답 상태에 대한 디버깅용 메시지.
        ///   * IsVLPassed (bool): VL 통과 여부. VL 요청의 성공 여부를 전달한다.
        ///   * VLPosition (Vector3): VL 요청 결과로 전달 된 위치값.
        ///   * VLRotation (Quaternion): VL 요청 결과로 전달 된 회전값.
        ///   * Confidence (float): VL 신뢰도. 0 ~ 1 사이의 값을 가진다.
        ///   * ResponseBody (string): 요청에 대한 응답 결과. JSON 형태의 응답 원문을 전달한다.
        /// 
        /// !! Notice
        ///   * IsVLPassed가 true 값이지만 Status는 Success가 아닐 수 있습니다.
        ///     VL은 성공했지만 VLSDK의 내부 로직을 바탕으로 판단한 결과 유효하지 않은 응답일 경우 최종적으로 실패 처리를 할 수 있습니다.
        /// 
        public void OnVLPoseResponded(VLResponseEventData eventData)
        {
            if(eventData.Status != ResponseStatus.Success)
            {
                m_VLFailCount++;
                m_VLFailCountText.text = $"VL Fail: {m_VLFailCount}";
                m_MessageText.text = $"Message: {eventData.Message}";

                NativeLogger.DebugLog(LogLevel.WARNING, $"VL Error: {eventData.Status}, message: {eventData.Message}");
            }
            else
            {
                m_VLPassCount++;
                m_VLPassCountText.text = $"VL Pass: {m_VLPassCount}";
                m_MessageText.text = $"Message: {eventData.Message}";

                NativeLogger.DebugLog(LogLevel.INFO, $"VL Pass");
            }
        }

        ///
        /// VL의 상태가 변경되는 경우 호출되는 이벤트.
        ///
        /// TrackerState:
        ///   * INITIAL: 모든 세션이 초기화 된 상태. VL 초기화가 긴 시간동안 계속 실패한 경우.
        ///   * VL_RECEIVED: VL 성공 응답을 수신한 상태. 아직 localizedPose가 계산되지 않음.
        ///   * VL_PASS: VL 초기화가 한 번이라도 성공한 경우.
        ///   * VL_FAIL: 40번 연속 VL 요청에 실패하는 경우.
        ///   * VL_OUT_OF_SERVICE: 서비스 범위 밖일 경우.
        ///    
        public void OnStateChanged(TrackerState state)
        {
            Debug.Log($"OnStateChanged: " + state);
        }

        /// 
        /// VL이 인식된 로케이션의 정보가 변경되는 경우 호출되는 이벤트.
        ///   
        /// ARCeye 콘솔의 계층 정보를 바탕으로 생성되는 layerInfo 값을 전달합니다.
        /// layerInfo는 {계층1}_{계층2}_{계층3}_... 과 같은 규칙으로 생성됩니다.
        ///   ex. NAVER_GND_device03172309
        ///   
        public void OnLayerInfoChanged(string layerInfo)
        {
            Debug.Log($"OnLayerInfoChanged: " + layerInfo);

            // Ground가 인식 된 경우. 
            if(layerInfo.Contains("GND")) 
            {
                Debug.Log("Load GND Stage in amproj file");
            }
            // 2층이 인식 된 경우.
            else if(layerInfo.Contains("2F")) 
            {
                Debug.Log("Load 2F Stage in amproj file");
            }
        }
        
        /// 
        /// 매 프레임마다 호출되는 이벤트
        /// 
        /// viewMatrix: 카메라의 viewMatrix
        /// projMatrix: 카메라의 projMatrix
        /// texMatrix: 카메라의 texMatrix
        /// relativeAltitude: 카메라의 상대 고도. VLSDK가 초기화 되는 시점을 0으로 설정한 상대 고도값. 단위는 m. 
        /// 
        public void OnPoseUpdated(Matrix4x4 viewMatrix, Matrix4x4 projMatrix, Matrix4x4 texMatrix, double relativeAltitude)
        {
            if(m_PosePrintCount++ > k_PosePrintInterval)
            {
                Debug.Log($"OnPoseUpdated: \n  viewMatrix: {viewMatrix}, \n  projMatrix: {projMatrix}, \n  texMatrix: {texMatrix} \n  relativeAltitude: {relativeAltitude}");
                m_PosePrintCount = 0;
            }
        }

        /// 
        ///   GPS 정보가 갱신 될 때마다 호출되는 이벤트.
        ///   Unity Input.location.lastData를 통해 얻은 GPS 정보를 전달합니다.
        /// 
        public void OnGeoCoordUpdated(double latitude, double longitude)
        {
            Debug.Log($"OnGeoCoordUpdated: latitude:{latitude}, longitude: {longitude}");
        }
    }
}
