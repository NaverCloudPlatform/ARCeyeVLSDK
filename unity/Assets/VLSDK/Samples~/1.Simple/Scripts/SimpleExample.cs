using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARCeye.Example.Simple
{
    public class SimpleExample : MonoBehaviour
    {
        [Header("VLSDK")]
        public ARCeye.VLSDKManager m_VLSDKManager;

        [Header("UI")]
        [SerializeField] private Text m_RequestCountText;
        [SerializeField] private Text m_VLPassCountText;
        [SerializeField] private Text m_VLFailCountText;
        [SerializeField] private Text m_MessageText;

        public RawImage m_RequestedTexture;
        private RectTransform m_RequestedTextureRT;

        private int m_RequestCount = 0;
        private int m_VLPassCount = 0;
        private int m_VLFailCount = 0;


        private void Awake()
        {
            m_RequestedTextureRT = m_RequestedTexture.GetComponent<RectTransform>();
        }

        // Receive various VLSDKManager events.

        ///
        /// Method executed through VLSDKManager's OnVLPoseRequested(VLRequestEventData) event. 
        /// Called every time a VL request is sent.
        /// Passes the information used in the request in VLRequestEventData format.
        /// 
        /// VLRequestEventData:
        ///   * Url (string): Invoke URL used in the request
        ///   * SecretKey (string): SecretKey used in the request
        ///   * RequestTexture (Texture2D): Request image
        /// 
        public void OnVLPoseRequested(VLRequestEventData eventData)
        {
            // Increase VL request count.
            m_RequestCount++;
            m_RequestCountText.text = $"Request Count: {m_RequestCount}";

            // Visualize VL request image.
            float ratio = (float)eventData.RequestTexture.width / (float)eventData.RequestTexture.height;
            float height = m_RequestedTextureRT.sizeDelta.y;
            float width = height * ratio;

            m_RequestedTextureRT.sizeDelta = new Vector2(width, height);
            m_RequestedTexture.texture = eventData.RequestTexture;
        }

        ///
        /// Method executed through VLSDKManager's OnVLPoseResponded(VLResponseEventData) event. 
        /// Called every time a response to a VL request is received.
        /// Passes the information used in the request in VLResponseEventData format.
        /// 
        /// VLResponseEventData:
        ///   * Timestamp (long): Timestamp of when the request was sent
        ///   * Status (ResponseStatus): Response status. Delivers the final response status based on the VL response and VLSDK internal state.
        ///   * Message (string): Debugging message for the response status.
        ///   * IsVLPassed (bool): VL pass status. Indicates whether the VL request was successful.
        ///   * VLPosition (Vector3): Position value delivered as a result of the VL request.
        ///   * VLRotation (Quaternion): Rotation value delivered as a result of the VL request.
        ///   * Confidence (float): VL confidence. Has a value between 0 and 1.
        ///   * ResponseBody (string): Response result for the request. Delivers the original response in JSON format.
        /// 
        /// !! Notice
        ///   * IsVLPassed may be true, but Status may not be Success.
        ///     If the VL succeeds but the response is determined to be invalid based on VLSDK's internal logic, it can be finally processed as a failure.
        /// 
        public void OnVLPoseResponded(VLResponseEventData eventData)
        {
            if (eventData.Status != ResponseStatus.Success)
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
        /// Event called when the VL state changes.
        ///
        /// TrackerState:
        ///   * INITIAL: State where all sessions are initialized. When VL initialization continues to fail for a long time.
        ///   * NOT_RECOGNIZED: When VL request fails 20 times in the INITIAL state.
        ///   * VL_PASS: When VL initialization succeeds at least once.
        ///   * VL_FAIL: When VL request fails 40 times consecutively.
        ///   * VL_OUT_OF_SERVICE: When out of service range.
        ///    
        public void OnStateChanged(TrackerState state)
        {
            Debug.Log($"OnStateChanged: " + state);
        }

        /// 
        /// Event called when the information of the location recognized by VL changes.
        ///   
        /// Delivers the layerInfo value generated based on the hierarchy information in the ARCeye console.
        /// layerInfo is generated according to the rule {layer1}_{layer2}_{layer3}_...
        ///   ex. NAVER_GND_device03172309
        ///   
        public void OnLayerInfoChanged(string layerInfo)
        {
            Debug.Log($"OnLayerInfoChanged: " + layerInfo);

            // When Ground is recognized. 
            if (layerInfo.Contains("GND"))
            {
                Debug.Log("Load GND Stage in amproj file");
            }
            // When 2F is recognized.
            else if (layerInfo.Contains("2F"))
            {
                Debug.Log("Load 2F Stage in amproj file");
            }
        }

        /// 
        /// Event called every frame
        /// 
        /// viewMatrix: Camera's viewMatrix
        /// projMatrix: Camera's projMatrix
        /// texMatrix: Camera's texMatrix
        /// relativeAltitude: Camera's relative altitude. Relative altitude value set to 0 at the time VLSDK is initialized. Unit is m. 
        /// 
        public void OnPoseUpdated(Matrix4x4 viewMatrix, Matrix4x4 projMatrix, Matrix4x4 texMatrix, double relativeAltitude)
        {

        }

        /// 
        ///   Event called whenever GPS information is updated.
        ///   Delivers GPS information obtained through Unity Input.location.lastData.
        /// 
        public void OnGeoCoordUpdated(double latitude, double longitude)
        {

        }
    }
}
