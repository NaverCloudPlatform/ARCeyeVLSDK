using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARCeye
{
    [Serializable]
    public class GeoJsonFeatureCollection
    {
        public string type;
        public List<GeoJsonFeature> features;
    }

    [Serializable]
    public class GeoJsonFeature
    {
        public string type;
        public GeoJsonProperties properties;
        public GeoJsonGeometry geometry;
        public int id;
    }

    [Serializable]
    public class GeoJsonProperties
    {
        public string location;
    }

    [Serializable]
    public class GeoJsonGeometry
    {
        public string type;
    }

    public class GeoJsonUtility
    {
        /// <summary>
        /// GeoJSON FeatureCollection 포맷이 올바른지 검증.
        /// </summary>
        /// <param name="jsonString">검증할 JSON 문자열</param>
        /// <returns>유효한 GeoJSON 포맷이면 true, 아니면 false</returns>
        public static bool IsValidGeoJsonFormat(string jsonString)
        {
            // 빈 문자열이나 null은 유효하지 않은 것으로 처리하되, 에러 로그는 출력하지 않음
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return false;
            }

            jsonString = jsonString.Trim();

            // 기본적인 JSON 포맷 체크 (중괄호 또는 대괄호로 시작/종료)
            if (!(jsonString.StartsWith("{") && jsonString.EndsWith("}")) &&
                !(jsonString.StartsWith("[") && jsonString.EndsWith("]")))
            {
                Debug.LogError("GeoJSON is not a valid JSON format");
                return false;
            }

            try
            {
                var featureCollection = JsonUtility.FromJson<GeoJsonFeatureCollection>(jsonString);

                // type이 "FeatureCollection"인지 확인
                if (featureCollection == null || featureCollection.type != "FeatureCollection")
                {
                    Debug.LogError("GeoJSON type is not 'FeatureCollection'");
                    return false;
                }

                // features 배열이 존재하는지 확인
                if (featureCollection.features == null || featureCollection.features.Count == 0)
                {
                    Debug.LogError("GeoJSON 'features' array is missing or empty");
                    return false;
                }

                // 각 feature 검증
                foreach (var feature in featureCollection.features)
                {
                    if (!IsValidFeature(feature))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GeoJSON 파싱 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// GeoJSON Feature 객체가 유효한지 검증.
        /// </summary>
        private static bool IsValidFeature(GeoJsonFeature feature)
        {
            if (feature == null)
            {
                return false;
            }

            // type이 "Feature"인지 확인
            if (feature.type != "Feature")
            {
                Debug.LogError("GeoJSON feature type is not 'Feature'");
                return false;
            }

            // properties가 존재하는지 확인
            if (feature.properties == null)
            {
                Debug.LogError("GeoJSON feature 'properties' is missing");
                return false;
            }

            // properties.location이 존재하는지 확인
            if (string.IsNullOrEmpty(feature.properties.location))
            {
                Debug.LogError("GeoJSON feature 'properties.location' is missing or empty");
                return false;
            }

            // geometry가 유효한지 확인
            if (!IsValidGeometry(feature.geometry))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// GeoJSON Geometry 객체가 유효한지 검증합니다.
        /// </summary>
        private static bool IsValidGeometry(GeoJsonGeometry geometry)
        {
            if (geometry == null)
            {
                Debug.LogError("GeoJSON feature 'geometry' is missing");
                return false;
            }

            // type이 존재하는지 확인 (예: "Polygon", "Point", "LineString" 등)
            if (string.IsNullOrEmpty(geometry.type))
            {
                Debug.LogError("GeoJSON geometry 'type' is missing or empty");
                return false;
            }

            return true;
        }
    }
}