using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ARCeye;

public class Example : MonoBehaviour
{
    public VLSDKManager m_VLSDKManager;

    // Start is called before the first frame update
    void Start()
    {
        m_VLSDKManager.StartSession();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
