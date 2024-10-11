using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye.Dataset
{    
    public class FrameData
    {
        public long timestamp;
        public Matrix4x4 modelMatrix;
        public Matrix4x4 projMatrix;
        public Matrix4x4 transMatrix;
        public ARDatasetIntrinsic intrinsic;
        public double latitude;
        public double longitude;

        public FrameData(string frameCode)
        {
            frameCode = frameCode.TrimEnd('|');
            string[] elems = frameCode.Split("&");

            if(elems.Length != 10)
            {
                Debug.LogError("frameCode is not valid : " + frameCode);
                return;
            }

            timestamp = long.Parse(elems[0]);
            modelMatrix = Matrix4x4Parse(elems[1]);
            projMatrix = Matrix4x4Parse(elems[2]);
            transMatrix = Matrix4x4Parse(elems[3]);
            intrinsic = new ARDatasetIntrinsic();
            intrinsic.fx = float.Parse(elems[4]);
            intrinsic.fy = float.Parse(elems[5]);
            intrinsic.cx = float.Parse(elems[6]);
            intrinsic.cy = float.Parse(elems[7]);
            latitude = double.Parse(elems[8]);
            longitude = double.Parse(elems[9]);
        }

        public FrameData()
        {
            this.timestamp = 0;
            this.modelMatrix = Matrix4x4.identity;
            this.projMatrix = Matrix4x4.identity;   // 사용되지 않는 값.
            this.transMatrix = Matrix4x4.identity;
            intrinsic = new ARDatasetIntrinsic();   // landscape 기준.
            intrinsic.fx = 480.062f;
            intrinsic.fy = 480.054f;
            intrinsic.cx = 180.626f;
            intrinsic.cy = 318.824f;
            latitude = 0;
            longitude = 0;
        }

        private Matrix4x4 Matrix4x4Parse(string str)
        {
            string[] values = str.Split(',');

            if (values.Length != 16)
            {
                Debug.LogError("Invalid matrix string format.");
                return Matrix4x4.identity;
            }

            Matrix4x4 matrix = new Matrix4x4();

            matrix.m00 = float.Parse(values[0]);
            matrix.m01 = float.Parse(values[1]);
            matrix.m02 = float.Parse(values[2]);
            matrix.m03 = float.Parse(values[3]);
            matrix.m10 = float.Parse(values[4]);
            matrix.m11 = float.Parse(values[5]);
            matrix.m12 = float.Parse(values[6]);
            matrix.m13 = float.Parse(values[7]);
            matrix.m20 = float.Parse(values[8]);
            matrix.m21 = float.Parse(values[9]);
            matrix.m22 = float.Parse(values[10]);
            matrix.m23 = float.Parse(values[11]);
            matrix.m30 = float.Parse(values[12]);
            matrix.m31 = float.Parse(values[13]);
            matrix.m32 = float.Parse(values[14]);
            matrix.m33 = float.Parse(values[15]);

            return matrix;
        }
    }
}