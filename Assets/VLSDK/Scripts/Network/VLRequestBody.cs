using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VLRequestBody
{
    public enum Type {
        ARCEYE, LABS
    }

    public string method;
    public string location;
    public string url;
    public string authorization;
    public string filename;
    public string imageFieldName;
    public byte[] image;
    public Dictionary<string, string> parameters = new Dictionary<string, string>();
    public System.IntPtr nativeNetworkServiceHandle;

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach(var elem in parameters)
        {
            sb.Append($"{elem.Key} : {elem.Value}\n");
        }

        return @$"
           Method - {method}
           URL - {url}
           filename - {filename}
           params - 
           {sb.ToString()}
         ";
    }
    
    public static bool IsValidCameraParam(string paramStr)
    {
        if(String.IsNullOrEmpty(paramStr))
            return false;

        string[] elems = paramStr.Split(",");
        return float.Parse(elems[0]) != 0 && float.Parse(elems[1]) != 0 &&
               float.Parse(elems[2]) != 0 && float.Parse(elems[3]) != 0;
    }

    public static VLRequestBody Create(Type type, ARCeye.RequestVLInfo requestInfo) 
    {
        switch(type) {
            case Type.ARCEYE : {
                return CreateARCEyeRequest(requestInfo);
            }
            case Type.LABS : {
                return CreateLABSRequest(requestInfo);
            }
            default : {
                Debug.LogError("Invalid VLRequestBody type is used");
                return null;
            }
        }
    }

    private static VLRequestBody CreateARCEyeRequest(ARCeye.RequestVLInfo requestInfo) {
        VLRequestBody body = new VLRequestBody();

        body.method = requestInfo.method;
        body.url = requestInfo.url;
        body.authorization = requestInfo.secretKey;
        body.filename = requestInfo.filename;
        body.imageFieldName = "image";
        
        if(VLRequestBody.IsValidCameraParam(requestInfo.cameraParam))
        {
            body.parameters.Add("cameraParameters", requestInfo.cameraParam);
        }
        
#if !UNITY_EDITOR
        if(requestInfo.requestWithPosition) {
            body.parameters.Add("odometry", requestInfo.odometry);
            body.parameters.Add("lastPose", requestInfo.lastPose);

            if(requestInfo.withGlobal) {
                body.parameters.Add("withGlobal", "true");
            }
        }
#endif

        body.nativeNetworkServiceHandle = requestInfo.nativeNetworkServiceHandle;

        return body;
    }

    private static VLRequestBody CreateLABSRequest(ARCeye.RequestVLInfo requestInfo) {
        VLRequestBody body = new VLRequestBody();
        body.method = requestInfo.method;
        body.url = requestInfo.url;
        body.authorization = "";
        body.filename = requestInfo.filename;
        body.imageFieldName = "images";

        if(VLRequestBody.IsValidCameraParam(requestInfo.cameraParam))
        {
            body.parameters.Add("camparam", requestInfo.cameraParam);
        }

        // LABS 요청에는 location 필드가 필수.
        if(!string.IsNullOrEmpty(requestInfo.location)) 
        {
            body.parameters.Add("location", requestInfo.location);
        }
        
#if !UNITY_EDITOR
        if(requestInfo.requestWithPosition) {
            body.parameters.Add("odometry", requestInfo.odometry);
            body.parameters.Add("last-pose", requestInfo.lastPose);

            if(requestInfo.withGlobal) {
                body.parameters.Add("withGlobal", "true");
            }
        }
#endif

        body.nativeNetworkServiceHandle = requestInfo.nativeNetworkServiceHandle;

        return body;
    }
}
