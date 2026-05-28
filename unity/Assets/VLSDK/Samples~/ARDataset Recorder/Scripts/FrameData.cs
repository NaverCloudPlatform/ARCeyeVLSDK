using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ARCeye.Dataset.Recorder
{
    public class FrameData
    {
        public Texture2D texture;
        public long timestamp;
        public int width;
        public int height;
        public Matrix4x4 modelMatrix;
        public Matrix4x4 projMatrix;
        public Matrix4x4 transMatrix;
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public double latitude;
        public double longitude;
        public double relAltitude;

        public string Encode()
        {
            string modelMatrixStr = PoseUtility.MatrixToString(modelMatrix);
            string projMatrixStr = PoseUtility.MatrixToString(projMatrix);
            string transMatrixStr = PoseUtility.MatrixToString(transMatrix);

            string frameStr = $"{timestamp}&{modelMatrixStr}&{projMatrixStr}&{transMatrixStr}&{fx}&{fy}&{cx}&{cy}&{latitude}&{longitude}&{relAltitude}|";

            return frameStr;
        }

        public byte[] EncodeToBase64()
        {
            string encoded = Encode();
            return Encoding.UTF8.GetBytes(encoded);
        }
    }
}