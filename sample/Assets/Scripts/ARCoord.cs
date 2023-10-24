using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ARCoord : MonoBehaviour
{
    [SerializeField] TextMeshPro m_CoordText;

    private Transform m_CameraTransform;
    
    void Start()
    {
        m_CameraTransform = Camera.main.transform;
    }

    
    void Update()
    {
        Vector3 forward = transform.position - m_CameraTransform.position;
        transform.rotation = Quaternion.LookRotation(forward);
    }

    public void SetCoord(int x, int y)
    {
        m_CoordText.text = string.Format("({0}, {1})", x, y);
    }
}
