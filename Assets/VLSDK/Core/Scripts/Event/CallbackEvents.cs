using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ARCeye {
[System.Serializable]
public class VLRequestEvent : UnityEvent<VLRequestEventData> {}
[System.Serializable]
public class VLRespondedEvent : UnityEvent<VLResponseEventData> {}
[System.Serializable]
public class ChangedStateEvent : UnityEvent<TrackerState> {}
[System.Serializable]
public class ChangedLocationEvent : UnityEvent<string> {}
[System.Serializable]
public class ChangedBuildingEvent : UnityEvent<string> {}
[System.Serializable]
public class ChangedFloorEvent : UnityEvent<string> {}
[System.Serializable]
public class ChangedRegionCodeEvent : UnityEvent<string> {}
[System.Serializable]
public class ChangedLayerInfoEvent : UnityEvent<string> {}
[System.Serializable]
public class UpdatedPoseEvent : UnityEvent<Matrix4x4, Matrix4x4, Matrix4x4, double> {}
[System.Serializable]
public class UpdatedGeoCoordEvent : UnityEvent<double, double> {}
[System.Serializable]
public class UpdatedRelAltitudeEvent : UnityEvent<double> {}
[System.Serializable]
public class DetectedObjectEvent : UnityEvent<DetectedObject> {}
[System.Serializable]
public class DetectedVLLocationEvent : UnityEvent<string[]> {}
[System.Serializable]
public class ReceivedVOTLocationEvent : UnityEvent<string[]> {}
[System.Serializable]
public class ReceivedVOTMapIdEvent : UnityEvent<string[]> {}
[System.Serializable]
public class ReceivedVOTObjectIdEvent : UnityEvent<string[]> {}
[System.Serializable]
public class GeoCoordInitEvent : UnityEvent {}
}