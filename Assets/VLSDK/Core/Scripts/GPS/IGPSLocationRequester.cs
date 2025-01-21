using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    public interface IGPSLocationRequester
    {
        void StartDetectingGPSLocation();
        void StopDetectingGPSLocation();
    }
}