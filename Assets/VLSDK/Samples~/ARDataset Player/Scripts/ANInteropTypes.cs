using ARCeye.Dataset;

using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace ARDecode.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ANFrame
    {
        public int width;
        public int height;
        public int format;

        public long timestamp;
        public long frame_num;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] viewMtx;  // 4x4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] projMtx;  // 4x4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public float[] cameraIntxs;  // 3x4

        public long pc_count;
        public IntPtr points;       // float*
        public IntPtr identifiers;  // int64_t*

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] gps;

        public double airpressure;
        public double rel_altitude;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] gravity;

        public double direction;
        public double directionAccuracy;

        public long unix_time;

        public static FrameData ConvertToFrameData(IntPtr framePtr)
        {
            ANFrame frame = Marshal.PtrToStructure<ANFrame>(framePtr);

            FrameData data = new FrameData();
            data.timestamp = frame.timestamp;

            // ViewMatrix 복사
            if (frame.viewMtx != null && frame.viewMtx.Length == 16)
            {
                // 오른손 좌표계 view matrix를 왼손 좌표계로 변환 후 inverse
                Matrix4x4 rhViewMatrix = PoseUtility.FloatArrayToMatrix4x4(frame.viewMtx);
                Matrix4x4 lhViewMatrix = PoseUtility.ConvertLHRHView(rhViewMatrix);

                data.modelMatrix = lhViewMatrix.inverse;
            }

            // ProjectionMatrix 복사 (필요 시)
            if (frame.projMtx != null && frame.projMtx.Length == 16)
            {
                data.projMatrix = FloatArrayToMatrix4x4(frame.projMtx);
            }

            data.transMatrix = Matrix4x4.identity;

            // Intrinsic 설정
            if (frame.cameraIntxs != null && frame.cameraIntxs.Length >= 4 * 3)
            {
                data.intrinsic = new ARDatasetIntrinsic
                {
                    fx = frame.cameraIntxs[0],
                    fy = frame.cameraIntxs[5],
                    cx = frame.cameraIntxs[8],
                    cy = frame.cameraIntxs[9]
                };
            }

            // GPS
            if (frame.gps != null && frame.gps.Length == 3)
            {
                data.latitude = frame.gps[0];
                data.longitude = frame.gps[1];
                data.relAltitude = frame.rel_altitude;
            }

            return data;
        }

        private static Matrix4x4 FloatArrayToMatrix4x4(float[] arr)
        {
            if (arr == null || arr.Length != 16)
                return Matrix4x4.identity;

            Matrix4x4 mat = new Matrix4x4();
            mat.m00 = arr[0]; mat.m01 = arr[1]; mat.m02 = arr[2]; mat.m03 = arr[3];
            mat.m10 = arr[4]; mat.m11 = arr[5]; mat.m12 = arr[6]; mat.m13 = arr[7];
            mat.m20 = arr[8]; mat.m21 = arr[9]; mat.m22 = arr[10]; mat.m23 = arr[11];
            mat.m30 = arr[12]; mat.m31 = arr[13]; mat.m32 = arr[14]; mat.m33 = arr[15];
            return mat;
        }
    }
}
