using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    public class ARCoordGenerator : MonoBehaviour
    {
        [SerializeField] private Transform m_Root;
        [SerializeField] private ARCoord m_ARCoordPrefab;

        private Camera m_MainCamera;
        private Dictionary<float, Dictionary<float, ARCoord>> m_ARCoords;
        private Dictionary<Vector3, ARCoord> m_ARCoordDic;

        void Awake()
        {
            m_ARCoords = new Dictionary<float, Dictionary<float, ARCoord>>();
            m_ARCoordDic = new Dictionary<Vector3, ARCoord>();
        }

        void Start()
        {
            m_MainCamera = Camera.main;
            // GenerateARCoords(20, 20, 5);
            InvokeRepeating(nameof(GenerateARCoords), 0.0f, 0.5f);
        }

        private void GenerateARCoords()
        {
            List<Vector3> removed = new List<Vector3>();
            foreach(var elem in m_ARCoordDic)
            {
                Vector3 diff = m_MainCamera.transform.position - elem.Key;
                float dist = diff.magnitude;
                if(dist > 7) {
                    Destroy(elem.Value.gameObject);
                    removed.Add(elem.Key);

                    float x = elem.Key.x;
                    float z = elem.Key.z;
                    
                    // if(m_ARCoords.ContainsKey(x)) {
                    //     if(m_ARCoords[x].ContainsKey(z)) {
                    //         Destroy(m_ARCoords[x][z].gameObject);
                    //         m_ARCoords[x].Remove(z);
                    //     }
                    //     m_ARCoords.Remove(x);
                    // }
                }
            }

            foreach(var elem in removed)
            {
                m_ARCoordDic.Remove(elem);
            }


            int coordX = Mathf.RoundToInt(m_MainCamera.transform.position.x);
            int coordZ = Mathf.RoundToInt(m_MainCamera.transform.position.z);
            int length = 10;
            int hLength = length / 2;

            Dictionary<float, ARCoord> hCoords;
            float height = 1.8f;

            for(int x = coordX-hLength ; x < coordX+hLength ; x++) 
            {
                for(int z = coordZ-hLength ; z < coordZ+hLength ; z++) 
                {                   
                    if(m_ARCoords.TryGetValue(x, out hCoords)) {
                        ARCoord arCoord;
                        if(!hCoords.TryGetValue(z, out arCoord)) {
                            arCoord = Instantiate<ARCoord>(m_ARCoordPrefab, new Vector3(x, height, z), Quaternion.identity, m_Root);
                            arCoord.SetCoord(x, z);
                            hCoords.TryAdd(z, arCoord);
                            m_ARCoords[x]= hCoords;

                            m_ARCoordDic.TryAdd(arCoord.transform.position, arCoord);
                        }
                    } else {
                        ARCoord arCoord = Instantiate<ARCoord>(m_ARCoordPrefab, new Vector3(x, height, z), Quaternion.identity, m_Root);
                        arCoord.SetCoord(x, z);

                        hCoords = new Dictionary<float, ARCoord>();
                        hCoords.TryAdd(z, arCoord);
                        m_ARCoords.TryAdd(x, hCoords);

                        m_ARCoordDic.TryAdd(arCoord.transform.position, arCoord);
                    }
                }
            }
        }

        /// rows - 원점을 중심으로 횡으로 몇 개의 ARCoords를 생성할 것인지.
        /// cols - 원점을 중심으로 열로 몇 개의 ARCoords를 생성할 것인지.
        /// space - ARCoords 간의 간격 (단위 - (m))
        private void GenerateARCoords(int rows, int cols, int space)
        {
            int hRows = rows / 2;
            int hCols = cols / 2;

            if(hRows == 0 || hCols == 0)
            {
                Debug.LogWarning("rows 혹은 cols는 2 이상의 값이 입력 되어야 함");
                return;
            }

            float height = 1.0f;
            for (int x = -hCols; x < hCols; x++)
            {
                for(int y = -hRows ; y<hRows ; y++)
                {
                    int coordX = x * space;
                    int coordY = y * space;
                    ARCoord arCoord = Instantiate<ARCoord>(m_ARCoordPrefab, new Vector3(coordX, height, coordY), Quaternion.identity, m_Root);
                    arCoord.SetCoord(coordX, coordY);
                }
            }
        }
    }
}