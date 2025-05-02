using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ARCeye
{
    public class ARFrame
    {
        public Texture texture;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public ARIntrinsic intrinsic;
        public Matrix4x4 projMatrix;
        public Matrix4x4 displayMatrix;

        public UnityYuvCpuImage? yuvBuffer;
        public UnityAction disposable;

        public ARFrame()
        {
            texture = null;
            yuvBuffer = null;
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            intrinsic = new ARIntrinsic();
            projMatrix = Matrix4x4.identity;
            displayMatrix = Matrix4x4.identity;
        }
    }

    public class ARIntrinsic
    {
        public float fx;
        public float fy;
        public float cx;
        public float cy;

        public ARIntrinsic()
        {
            fx = 0;
            fy = 0;
            cx = 0;
            cy = 0;
        }

        public ARIntrinsic(float fx, float fy, float cx, float cy)
        {
            this.fx = fx;
            this.fy = fy;
            this.cx = cx;
            this.cy = cy;
        }
    }
}