using UnityEngine;
using System.Collections;
using UnityEditor;

namespace ARCeye
{
    public class VLSDKSettingsGenerator {
        [MenuItem("Assets/Create/ARCeye/VLSDKSettings")]
        public static void CreateVLSDKSettings()
        {
            VLSDKSettings asset = ScriptableObject.CreateInstance<VLSDKSettings>();

            AssetDatabase.CreateAsset(asset, "Assets/VLSDK/VLSDKSettings.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
    }
}