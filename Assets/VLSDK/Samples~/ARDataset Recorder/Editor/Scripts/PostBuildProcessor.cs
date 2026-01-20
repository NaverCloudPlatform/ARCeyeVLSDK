using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;

namespace ARCeye
{
    public class PostBuildProcessor
    {
#if UNITY_IOS
        [PostProcessBuild]
        public static void EnableFileSharing(BuildTarget buildTarget, string projectPath)
        {
            if(buildTarget != BuildTarget.iOS)
            {
                return;
            }

            string plistPath = $"{projectPath}/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            PlistElementDict root = plist.root;

            root.SetBoolean("UIFileSharingEnabled", true);
            root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);

            File.WriteAllText(plistPath, plist.WriteToString());
        }
#endif
    }
}