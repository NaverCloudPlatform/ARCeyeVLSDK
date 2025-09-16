using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ARCeye;
using System.Text;
using System.IO;
using UnityEngine.Networking;

public class RecorderTests
{
    [UnityTest]
    public IEnumerator LoadDataTxtFromServerTest()
    {
        string url = "http://data.ar.naverlabs.net/Datasets/BITMAP/20240624_Fursys/20240624-102839";
        string dataTxtPath = url + "/data.txt";

        yield return GetText(dataTxtPath);
    }

    [UnityTest]
    public IEnumerator LoadCameraParamTxtFromServerTest()
    {
        string url = "http://data.ar.naverlabs.net/Datasets/BITMAP/20240624_Fursys/20240624-102839";
        string dataTxtPath = url + "/cameraParam/1719192519232.txt";

        yield return GetText(dataTxtPath);
    }

    [UnityTest]
    public IEnumerator MultipleRequestTest()
    {
        string url = "http://data.ar.naverlabs.net/Datasets/BITMAP/20240624_Fursys/20240624-102839";
        string dataTxtPath = url + "/data.txt";
        string cameraParamPath = url + "/cameraParam/1719192519232.txt";

        yield return GetText(dataTxtPath);
        yield return GetText(cameraParamPath);
    }

    IEnumerator GetText(string dataTxtPath) {
        UnityWebRequest www = UnityWebRequest.Get(dataTxtPath);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
        else {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
        }
    }
}
