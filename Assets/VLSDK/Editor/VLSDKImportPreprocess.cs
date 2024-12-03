using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[InitializeOnLoad]
public class VLSDKImportPreprocess
{
    private static Dictionary<string, string> packagesToAdd = new Dictionary<string, string>()
    {
        { "com.unity.modules.xr", "1.0.0" },
        { "com.unity.xr.arcore", "5.1.5" },
        { "com.unity.xr.arfoundation", "5.1.5" },
        { "com.unity.xr.arkit", "5.1.5" },
        { "com.unity.xr.management", "4.4.0" },
        { "com.unity.nuget.newtonsoft-json", "3.2.1" }
    };

    private static Dictionary<string, string> packagesToDefineSymbols = new Dictionary<string, string>()
    {
        { "com.unity.xr.arfoundation", "VLSDK_ARFOUNDATION" },
        { "com.unity.nuget.newtonsoft-json", "VLSDK_NEWTONSOFT_JSON" }
    };

    static VLSDKImportPreprocess()
    {
        AddPackagesToManifest();
        AddDefineSymbols();

        // 1.6.3 이하 버전을 사용하는 경우 iOS plugin으로 *.a을 사용.
        // 1.6.5 버전 이후부터는 *.framework를 사용.
        // 구 버전의 native plugin이 존재하는 경우 제거.
        RemoveDeprecatedNativePlugins();
    }

    private static void AddPackagesToManifest()
    {
        // manifest.json 파일 경로
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        if (!File.Exists(manifestPath))
        {
            Debug.LogError("Cannot find manifest.json file");
            return;
        }
        
        string manifestJson = File.ReadAllText(manifestPath);
        string dependenciesBlock = GetDependenciesBlock(manifestJson);
        
        Dictionary<string, string> dependencies = ParseDependencies(dependenciesBlock);
        
        bool manifestChanged = false;
        foreach (var package in packagesToAdd)
        {
            if (!dependencies.ContainsKey(package.Key))
            {
                dependencies[package.Key] = package.Value;
                manifestChanged = true;
            }
        }

        if (manifestChanged)
        {
            string updatedManifest = UpdateManifestJson(manifestJson, dependencies);
            File.WriteAllText(manifestPath, updatedManifest);

            // 패키지 매니저 리프레시
            AssetDatabase.Refresh();
        }
    }

    private static string GetDependenciesBlock(string manifestJson)
    {
        int startIndex = manifestJson.IndexOf("\"dependencies\": {");
        if (startIndex == -1)
        {
            Debug.LogError("Failed to find dependencies block in manifest.json");
            return null;
        }

        int endIndex = manifestJson.IndexOf("}", startIndex);
        return manifestJson.Substring(startIndex, endIndex - startIndex + 1);
    }

    private static Dictionary<string, string> ParseDependencies(string dependenciesBlock)
    {
        var dependencies = new Dictionary<string, string>();

        // JSON 형식에서 key-value 추출
        string[] lines = dependenciesBlock.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains(":"))
            {
                string[] keyValue = line.Split(new[] { ':' }, 2);
                string key = keyValue[0].Trim().Replace("\"", "");
                string value = keyValue[1].Trim().Replace("\"", "").Replace(",", "");

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value) && key != "dependencies")
                {
                    dependencies[key] = value;
                }
            }
        }

        return dependencies;
    }

    private static string UpdateManifestJson(string manifestJson, Dictionary<string, string> dependencies)
    {
        // 새로운 dependencies 블록 만들기
        string newDependenciesBlock = "\"dependencies\": {\n";
        foreach (var kvp in dependencies)
        {
            newDependenciesBlock += $"    \"{kvp.Key}\": \"{kvp.Value}\",\n";
        }
        newDependenciesBlock = newDependenciesBlock.TrimEnd(',', '\n') + "\n  }";

        // 기존 dependencies 블록 대체
        string oldDependenciesBlock = GetDependenciesBlock(manifestJson);
        string updatedManifest = manifestJson.Replace(oldDependenciesBlock, newDependenciesBlock);
        return updatedManifest;
    }

    private static void AddDefineSymbols()
    {
        foreach(var packages in packagesToDefineSymbols)
        {
            string defineSymbol = packages.Value;
            
            AddDefineSymbol(BuildTargetGroup.Standalone, defineSymbol);
            AddDefineSymbol(BuildTargetGroup.Android, defineSymbol);
            AddDefineSymbol(BuildTargetGroup.iOS, defineSymbol);
        }
    }

    private static void AddDefineSymbol(BuildTargetGroup group, string defineSymbol)
    {
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

        // Define Symbol이 이미 포함되어 있는지 확인
        if (!symbols.Contains(defineSymbol))
        {
            symbols += ";" + defineSymbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
        }
    }
    
    private static void RemoveDeprecatedNativePlugins()
    {
        // 구버전 iOS 플러그인 제거.
        string iosPluginPath = "Assets/VLSDK/Plugins/IOS/libVLSDK.a";
        if(File.Exists(iosPluginPath))
        {
            Debug.LogWarning("Remove old version ios native plugin");
            File.Delete(iosPluginPath);
            File.Delete(iosPluginPath + ".meta");
        }
    }
}
