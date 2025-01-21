using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace ARCeye
{
    public class VLSDKSettingsGenerator {
        [MenuItem("Assets/Create/ARCeye/VLSDKSettings")]
        public static void CreateVLSDKSettings()
        {
            VLSDKSettings asset = ScriptableObject.CreateInstance<VLSDKSettings>();

            string path = GetSelectedDirectoryPath();
            string assetPathAndName = GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        private static string GetSelectedDirectoryPath()
        {
            string path = "Assets";
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (Selection.activeObject != null)
            {
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (File.Exists(selectedPath))
                    {
                        selectedPath = Path.GetDirectoryName(selectedPath);
                    }

                    path = selectedPath;
                }
            }

            return path;
        }

        private static string GenerateUniqueAssetPath(string path)
        {
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(path, "New VLSDKSettings.asset")
            );

            return assetPathAndName;
        }
    }
}