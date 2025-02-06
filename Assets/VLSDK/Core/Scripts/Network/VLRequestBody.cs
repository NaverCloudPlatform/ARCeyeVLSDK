using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARCeye.Dataset;
using ARCeye;

public class VLRequestBody
{
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
           params - {sb.ToString()}
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

    public static bool IsValidRequest(VLRequestBody body, Texture texture)
    {
        // landscape 모드는 지원하지 않음.
        float texWidth = texture.width;
        float texHeight = texture.height;

        string camParamStr;
        if(body.parameters.ContainsKey("cameraParameters"))
        {
            camParamStr = body.parameters["cameraParameters"];
        }
        else if(body.parameters.ContainsKey("camparams"))
        {
            camParamStr = body.parameters["camparams"];
        }
        else
        {
            return true;
        }

        string[] camParamElems = camParamStr.Split(',');
        float fx = float.Parse(camParamElems[0]);
        float fy = float.Parse(camParamElems[1]);
        float cx = float.Parse(camParamElems[2]);
        float cy = float.Parse(camParamElems[3]);

        // texture가 portrait인 경우 intrinsic도 portrait인지 확인.
        if(texWidth < texHeight && cx > cy)
        {
            NativeLogger.DebugLog(ARCeye.LogLevel.ERROR, $"요청 이미지(w {texWidth}, h {texHeight})와 camParam(cx {cx}, cy {cy})의 방향이 일치하지 않음");
            return false;
        }

        // texture의 해상도를 기반으로 intrinsic이 유효한 값인지 확인.
        float camParamWidth = cx * 2;
        float camParamHeight = cy * 2;

        float diffWidth = Mathf.Abs(texWidth - camParamWidth);
        float diffHeight = Mathf.Abs(texHeight - camParamHeight);

        float diffWidthRatio = diffWidth / texWidth;
        float diffHeightRatio = diffHeight / texHeight;

        if(diffWidthRatio > 0.05f)
        {
            NativeLogger.DebugLog(ARCeye.LogLevel.ERROR, $"요청 이미지의 width({texWidth})와 camParam의 cx({cx})의 차이가 큼");
            return false;
        }
        if(diffHeightRatio > 0.05f)
        {
            NativeLogger.DebugLog(ARCeye.LogLevel.ERROR, $"요청 이미지의 height({texHeight})와 camParam의 cy({cy})의 차이가 큼");
            return false;
        }

        return true;
    }

    public static VLRequestBody Create(ARCeye.RequestVLInfo requestInfo) 
    {
        if(IsARCeyeURL(requestInfo.url))
        {
            return CreateARCeyeRequest(requestInfo);
        }
        else
        {
            return CreateLABSRequest(requestInfo);
        }
    }

    private static bool IsARCeyeURL(string url)
    {
        string prefix1 = "https://vl-arc-eye.ncloud.com/api";
        string prefix2 = "https://api-arc-eye.ncloud.com";
        string prefix3 = "arc-eye.ncloud.com";
        return url.Contains(prefix1) || url.Contains(prefix2) || url.Contains(prefix3);
    }

    private static VLRequestBody CreateARCeyeRequest(ARCeye.RequestVLInfo requestInfo) {
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
        
        if(requestInfo.requestWithPosition && IsPositionBasedRequestValid()) {
            body.parameters.Add("odometry", requestInfo.odometry);
            body.parameters.Add("lastPose", requestInfo.lastPose);

            if(requestInfo.withGlobal) {
                body.parameters.Add("withGlobal", "true");
            }
        }

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
            body.parameters.Add("camparams", requestInfo.cameraParam);
        }

        // LABS 요청에는 location 필드가 필수.
        if(!string.IsNullOrEmpty(requestInfo.location)) 
        {
            body.parameters.Add("location", requestInfo.location);
        }
        
        if(requestInfo.requestWithPosition && IsPositionBasedRequestValid()) {
            body.parameters.Add("odometry", requestInfo.odometry);
            body.parameters.Add("last-pose", requestInfo.lastPose);

            if(requestInfo.withGlobal) {
                body.parameters.Add("withGlobal", "true");
            }
        }

        body.nativeNetworkServiceHandle = requestInfo.nativeNetworkServiceHandle;

        return body;
    }

    /// <summary>
    ///   현재 위치를 기반으로 요청을 보낼 수 있는지 확인.
    ///   Editor 모드에서 ARDatasetManager를 사용하지 않는 경우 항상 false가 리턴된다.
    /// </summary>
    private static bool IsPositionBasedRequestValid() 
    {
#if UNITY_EDITOR
        // 코드 구조를 깔끔하게 하기 위해 여기에서 FindObjectOfType 실행.
        ARDatasetManager datasetManager = GameObject.FindObjectOfType<ARDatasetManager>();
        return datasetManager != null;
#else
        return true;
#endif
    }
}
