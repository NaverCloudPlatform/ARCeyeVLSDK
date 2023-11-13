using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ARCeye.VLSDKManager m_VLSDKManager;

    void Start() 
    {
        m_VLSDKManager.StartSession();
    }

    public void OnLayerInfoChanged(string layerInfo)
    {
        // ARCeye 콘솔의 계층 정보를 바탕으로 생성되는 layerInfo를 사용하여 AMapper 스테이지와 연동합니다.
        // layerInfo는 {계층1}_{계층2}_{계층3}_... 값이 전달됩니다.
        //   ex. GeomdanOryu_GND_device03172309

        // 검단오류역의 Ground가 인식 된 경우. 
        if(layerInfo.Contains("GeomdanOryu_GND")) 
        {
            Debug.Log("AMapper의 GND 스테이지 로드");
        }
        // 검단오류역의 2층이 인식 된 경우.
        else if(layerInfo.Contains("GeomdanOryu_2F")) 
        {
            Debug.Log("AMapper의 2F 스테이지 로드");
        }
    }
}
