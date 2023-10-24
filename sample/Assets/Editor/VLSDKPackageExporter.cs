using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using System.IO;

namespace ARCeye
{
    public class VLSDKPackageExporter
    {
        private static List<string> m_ExcludeFiles = new List<string>
        {
            "VLSDK Settings_1784.asset",
            "VLSDK Settings_LotteWorld.asset"
        };

        private static List<string> m_ExcludeDirectories = new List<string>
        {
            "Dataset",
        };
        
        [MenuItem("ARC eye/Export VLSDK Package")]
        private static void CreatePackage()
        {
            string directoryPath = "Assets/VLSDK"; 

            if (Directory.Exists(directoryPath))
            {
                List<string> assetPaths = GetAssetsFromDirectory(directoryPath);

                if (assetPaths.Count > 0)
                {
                    string version = PlayerSettings.bundleVersion;
                    string filename = $"vl-sdk-{version}";

                    string exportPath = EditorUtility.SaveFilePanel("Save UnityPackage", "", filename, "unitypackage");

                    if (!string.IsNullOrEmpty(exportPath))
                    {
                        AssetDatabase.ExportPackage(assetPaths.ToArray(), exportPath, ExportPackageOptions.Recurse);
                        Debug.Log("Package created at: " + exportPath);
                    }
                }
                else
                {
                    Debug.LogWarning("No assets to package.");
                }
            }
            else
            {
                Debug.LogWarning("Directory does not exist: " + directoryPath);
            }
        }

        private static List<string> GetAssetsFromDirectory(string directoryPath)
        {
            List<string> assetList = new List<string>();

            string[] allFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach (string file in allFiles)
            {
                string filename = Path.GetFileName(file);
                if (!m_ExcludeFiles.Contains(filename) && !IsPathExcluded(file))
                {
                    assetList.Add(file);
                }
            }

            return assetList;
        }

        private static bool IsPathExcluded(string path)
        {
            foreach (string excludeDir in m_ExcludeDirectories)
            {
                if (path.Contains("/" + excludeDir + "/") || path.EndsWith("/" + excludeDir))
                {
                    return true;
                }
            }
            return false;
        }
    }
}