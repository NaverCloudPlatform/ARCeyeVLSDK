using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ARCeye
{
    public class VLSDKManagerFactory : MonoBehaviour
    {
        public static VLSDKManager CreateVLSDKManager()
        {
            // Create VLSDKManager
#if UNITY_EDITOR
            GameObject VLSDKManagerObject;
            if(EditorApplication.isPlaying)
            {
                VLSDKManagerObject = new GameObject("VLSDKManager");

                VLSDKManagerObject.AddComponent<TextureProvider>();
                VLSDKManagerObject.AddComponent<NetworkController>();
                VLSDKManagerObject.AddComponent<GeoCoordProvider>();
                VLSDKManagerObject.AddComponent<VLSDKManager>();
            }
            else
            {
                VLSDKManagerObject = ObjectFactory.CreateGameObject("VLSDKManager", typeof(VLSDKManager));
                VLSDKManagerObject.AddComponent<TextureProvider>();
                VLSDKManagerObject.AddComponent<NetworkController>();
                VLSDKManagerObject.AddComponent<GeoCoordProvider>();
            }
#else
            GameObject VLSDKManagerObject = new GameObject("VLSDKManager");
            VLSDKManagerObject.AddComponent<VLSDKManager>();
#endif 

            return VLSDKManagerObject.GetComponent<VLSDKManager>();
        }
    }
}