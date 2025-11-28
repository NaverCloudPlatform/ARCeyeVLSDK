using UnityEngine;

namespace ARCeye {
public class LocationInfo {
    public float latitude { get; }
    public float longitude { get; }
    
    public LocationInfo(float lat, float lon) {
        this.latitude = lat;
        this.longitude = lon;
    }

    public LocationInfo(UnityEngine.LocationInfo info) {
        this.latitude = info.latitude;
        this.longitude = info.longitude;
    }
}
}