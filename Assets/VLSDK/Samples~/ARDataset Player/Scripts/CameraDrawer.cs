using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ARCeye.Dataset
{
    public class CameraDrawer
    {
        public static void DrawFrame(Matrix4x4 matrix, Color frameColor, float size)
        {
#if UNITY_EDITOR            
            float width = 9 * 0.05f * size;
            float height = 16 * 0.05f * size;
            float depth = 10 * 0.05f * size; // 원점에서 사각형까지의 거리

            Vector3 origin = new Vector3(0, 0, 0);
            Vector3 originUp = Vector3.up;

            // 사각형의 4개 꼭지점 계산 (로컬 좌표)
            Vector3 topLeft = origin + Vector3.forward * depth + Vector3.up * height / 2 - Vector3.right * width / 2;
            Vector3 topRight = origin + Vector3.forward * depth + Vector3.up * height / 2 + Vector3.right * width / 2;
            Vector3 bottomLeft = origin + Vector3.forward * depth - Vector3.up * height / 2 - Vector3.right * width / 2;
            Vector3 bottomRight = origin + Vector3.forward * depth - Vector3.up * height / 2 + Vector3.right * width / 2;

            // 모델 행렬을 사용하여 월드 좌표로 변환
            origin = matrix.MultiplyPoint3x4(origin);
            originUp = matrix.MultiplyPoint3x4(originUp);
            topLeft = matrix.MultiplyPoint3x4(topLeft);
            topRight = matrix.MultiplyPoint3x4(topRight);
            bottomLeft = matrix.MultiplyPoint3x4(bottomLeft);
            bottomRight = matrix.MultiplyPoint3x4(bottomRight);

            Gizmos.color = frameColor;

            // 원점에서 사각형의 각 꼭지점까지 선 그리기
            float thickness = 7;
            Handles.DrawBezier(origin, topLeft, origin, topLeft, frameColor, null, thickness);
            Handles.DrawBezier(origin, topRight, origin, topRight, frameColor, null, thickness);
            Handles.DrawBezier(origin, bottomLeft, origin, bottomLeft, frameColor, null, thickness);
            Handles.DrawBezier(origin, bottomRight, origin, bottomRight, frameColor, null, thickness);

            Handles.DrawBezier(origin, originUp, origin, originUp, frameColor, null, thickness);

            // 사각형의 모서리 연결
            Handles.DrawBezier(topLeft, topRight, topLeft, topRight, frameColor, null, thickness);
            Handles.DrawBezier(topRight, bottomRight, topRight, bottomRight, frameColor, null, thickness);
            Handles.DrawBezier(bottomRight, bottomLeft, bottomRight, bottomLeft, frameColor, null, thickness);
            Handles.DrawBezier(bottomLeft, topLeft, bottomLeft, topLeft, frameColor, null, thickness);

            Gizmos.DrawSphere(origin, 0.1f);
#endif 
        }
    }
}