using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[InitializeOnLoad]
public class VLSDKImportPreprocess
{
    static VLSDKImportPreprocess()
    {
        // Always included shader 체크.
        string[] guids = AssetDatabase.FindAssets("VLSDK t:shader");

        if (guids.Length == 0)
        {
            UnityEngine.Debug.LogWarning("VLSDK 쉐이더 파일을 경로를 찾을 수 없습니다. 이 메시지가 지속적으로 출력된다면 VLSDK > Shaders 경로에 VLSDKPreviewRotation.shader가 있는지 확인해주세요.");
            return;
        }

        SerializedObject graphicsSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        SerializedProperty alwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

        int shadersCount = alwaysIncludedShaders.arraySize;

        List<string> shaderNames = new List<string>();

        for(int i=0 ; i<shadersCount ; i++)
        {
            Object obj = alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue;
            shaderNames.Add(obj.name);
        }

        foreach (string guid in guids)
        {
            string shaderPath = AssetDatabase.GUIDToAssetPath(guid);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            string shaderName = shader.name;

            if(shaderNames.Contains(shaderName))
            {
                continue;
            }

            alwaysIncludedShaders.InsertArrayElementAtIndex(alwaysIncludedShaders.arraySize);
            alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1).objectReferenceValue = shader;
        }

        graphicsSettings.ApplyModifiedProperties();
    }
}
