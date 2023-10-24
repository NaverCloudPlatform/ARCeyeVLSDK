using System.IO;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Events;

using UnityEngine.XR.ARFoundation;

using ARCeye;

public class VLSDKManagerTest
{
    private const string PREFAB_PATH = "Test/VLSDKManager";
    private const string IMAGE_001_PATH = "Test/1784/1784_1F_001";
    

    private VLSDKManager m_VLSDKManager;
    private NetworkController m_NetworkController;

    private Camera m_MainCamera;

    [SetUp]
    public void SetUp() 
    {
        CreateMainCamera();
        CreateVLSDKManager();
        InitNetworkController();
    }

    private void CreateMainCamera()
    {
        m_MainCamera = (new GameObject()).AddComponent<Camera>();
        m_MainCamera.tag = "MainCamera";
    }

    private void CreateVLSDKManager()
    {
        VLSDKManager prefab = Resources.Load<VLSDKManager>("Test/VLSDKManager");

        prefab.arCamera = (new GameObject()).AddComponent<ARCameraManager>().transform;
        
        m_VLSDKManager = GameObject.Instantiate(prefab);
        m_VLSDKManager.origin = (new GameObject()).transform;
    }

    private void InitNetworkController()
    {
        m_NetworkController = m_VLSDKManager.GetComponent<NetworkController>();
        m_NetworkController.SaveQueryImage = false;
    }

    [UnityTest]
    public IEnumerator VLSDKManager_VL요청_001() {
        yield return RunVLResponseTest(IMAGE_001_PATH);
    }

    [TearDown]
    public void TearDown()
    {        
        GameObject.DestroyImmediate(m_VLSDKManager.origin.gameObject);
        GameObject.DestroyImmediate(m_VLSDKManager.gameObject);
        GameObject.DestroyImmediate(m_MainCamera.gameObject);
    }

    private IEnumerator RunVLResponseTest(string imagePath)
    {
        // Given. VLSDKManager가 ICNT2에 대해 설정이 됐을때,
        // m_VLSDKManager.m_Config.debugRequestTexture = Resources.Load<Texture2D>(imagePath);
        m_VLSDKManager.StartSession();

        TrackerState actual = TrackerState.INITIAL;

        float timeout = 7;
        float totalTime = 0;

        // When. 내부 루프가 동작하면서 이미지 쿼리를 보내면.
        while(totalTime < timeout)
        {
            var stateChangedEvent = m_VLSDKManager.OnStateChanged;
            stateChangedEvent.AddListener(state => {
                // Then. expected regionCode가 전달되어야 한다.
                actual = state;
                totalTime = timeout;
            });

            yield return null;

            totalTime += Time.deltaTime;
        }

        Assert.AreEqual(TrackerState.VL_PASS, actual);

    }

    private Texture2D LoadJPG(string filePath) {
        Texture2D tex = null;
        byte[] fileData;
    
        if (File.Exists(filePath))     {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
        }
        return tex;
    }
}
