using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VLRequestBody
{
    public string method;
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
}
