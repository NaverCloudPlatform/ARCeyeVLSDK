using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ARCeye
{
    public class VLResponseEventData
    {
        private ResponseStatus m_Status;
        public ResponseStatus Status => m_Status;

        private long m_Timestamp;
        public long Timestamp => m_Timestamp;

        private string m_Message;
        public string Message => m_Message;

        private string m_ResponseBody;
        public string ResponseBody => m_ResponseBody;

        private bool m_IsVLPassed;
        public bool IsVLPassed => m_IsVLPassed;

        private Vector3 m_VLPosition;
        public Vector3 VLPosition => m_VLPosition;

        private Quaternion m_VLRotation;
        public Quaternion VLRotation => m_VLRotation;

        private float m_Confidence;
        public float Confidence => m_Confidence;

        private VLResponseParser m_VLResponseParser;


        public static VLResponseEventData Create(ResponseStatus status)
        {
            VLResponseEventData eventData = new VLResponseEventData();
            eventData.m_Status = status;
            return eventData;
        }

        public static VLResponseEventData Create(NativeVLResponseEventData nativeEventData)
        {
            VLResponseEventData eventData = new VLResponseEventData();
            
            eventData.m_Status = (ResponseStatus) nativeEventData.statusCode;
            eventData.m_Message = Marshal.PtrToStringAnsi(nativeEventData.message);
            eventData.m_ResponseBody = Marshal.PtrToStringAnsi(nativeEventData.responseBody);

            if(VLResponseParser.IsARCeyeResponse(eventData.m_ResponseBody))
            {
                eventData.m_VLResponseParser = new ARCeyeResponseParser(eventData.m_ResponseBody);
            }
            else
            {
                eventData.m_VLResponseParser = new DevResponseParser(eventData.m_ResponseBody);
            }

            eventData.m_VLResponseParser.Parse();

            eventData.m_Timestamp = eventData.m_VLResponseParser.Timestamp;
            eventData.m_IsVLPassed = eventData.m_VLResponseParser.IsVLPassed;
            eventData.m_VLPosition = eventData.m_VLResponseParser.VLPosition;
            eventData.m_VLRotation = eventData.m_VLResponseParser.VLRotation;
            eventData.m_Confidence = eventData.m_VLResponseParser.Confidence;
            eventData.m_ResponseBody = eventData.m_VLResponseParser.ResponseBody;

            return eventData;
        }
    }
}