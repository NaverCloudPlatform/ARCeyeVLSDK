using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEngine.Rendering;
using UnityEditor.XR.Management;
using UnityEngine.SceneManagement;

namespace ARCeye
{
    /// <summary>
    /// VLSDK Project Validation Rule
    /// </summary>
    public class VLSDKValidationRule
    {
        public string message;
        public string category;
        public Func<bool> checkPredicate;
        public Action fixIt;
        public string fixItMessage;
        public bool fixItAutomatic;
        public BuildTargetGroup[] buildTargetGroup;
        public bool error;
        public string settingsPath; // Settings page path for Edit button

        public VLSDKValidationRule()
        {
            error = true;
            fixItAutomatic = false;
            buildTargetGroup = null;
            settingsPath = "Project/Player";
        }

        public bool CanFix => fixIt != null;
    }

    /// <summary>
    /// VLSDK Project Validation System
    /// </summary>
    public static class VLSDKProjectValidation
    {
        private static List<VLSDKValidationRule> s_ValidationRules;

        public static List<VLSDKValidationRule> GetRules()
        {
            if (s_ValidationRules == null)
            {
                s_ValidationRules = new List<VLSDKValidationRule>();
                BuildValidationRules();
            }
            return s_ValidationRules;
        }

        private static void BuildValidationRules()
        {
            s_ValidationRules.Clear();

            // ARCore Plugin Validation (Android)
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "ARCore must be enabled in XR Plug-in Management",
                category = "XR Settings",
                checkPredicate = () => IsARCoreEnabled(),
                fixIt = null,
                fixItMessage = "Enable ARCore in Edit > Project Settings > XR Plug-in Management > Android",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.Android },
                error = true,
                settingsPath = "Project/XR Plug-in Management"
            });

            // ARKit Plugin Validation (iOS)
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "ARKit must be enabled in XR Plug-in Management",
                category = "XR Settings",
                checkPredicate = () => IsARKitEnabled(),
                fixIt = null,
                fixItMessage = "Enable ARKit in Edit > Project Settings > XR Plug-in Management > iOS",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.iOS },
                error = true,
                settingsPath = "Project/XR Plug-in Management"
            });

            // Camera Usage Description Validation
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Camera Usage Description is required for iOS builds",
                category = "iOS Permissions",
                checkPredicate = () => IsCameraUsageDescriptionSet(),
                fixIt = () => SetCameraUsageDescription(),
                fixItMessage = "Set Camera Usage Description in Player Settings",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.iOS },
                error = true
            });

            // Location Usage Description Validation
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Location Usage Description is recommended for location-based features",
                category = "iOS Permissions",
                checkPredicate = () => IsLocationUsageDescriptionSet(),
                fixIt = () => SetLocationUsageDescription(),
                fixItMessage = "Set Location Usage Description in Player Settings",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.iOS },
                error = false
            });

            // Graphics API Validation (Android)
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Only OpenGLES3 should be enabled in Graphics APIs",
                category = "Google ARCore",
                checkPredicate = () => IsGraphicsAPICorrect(),
                fixIt = () => SetGraphicsAPI(),
                fixItMessage = "Set Graphics API to OpenGLES3 only",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.Android },
                error = true
            });

            // Minimum API Level Validation (Android)
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Minimum API Level must be 25 or higher",
                category = "Google ARCore",
                checkPredicate = () => IsMinimumAPILevelCorrect(),
                fixIt = () => SetMinimumAPILevel(),
                fixItMessage = "Set Minimum API Level to 25 or higher",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.Android },
                error = true
            });

            // Scripting Backend Validation (Android)
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Scripting Backend must be set to IL2CPP",
                category = "Google ARCore",
                checkPredicate = () => IsScriptingBackendIL2CPP(),
                fixIt = () => SetScriptingBackendIL2CPP(),
                fixItMessage = "Set Scripting Backend to IL2CPP",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.Android },
                error = true
            });

            // Target Architectures Validation (Android)
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Only ARM64 should be enabled in Target Architectures",
                category = "Google ARCore",
                checkPredicate = () => IsTargetArchitectureARM64Only(),
                fixIt = () => SetTargetArchitectureARM64Only(),
                fixItMessage = "Set Target Architectures to ARM64 only",
                fixItAutomatic = false,
                buildTargetGroup = new[] { BuildTargetGroup.Android },
                error = true
            });

            // Define Symbol: VLSDK_ARFOUNDATION
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "VLSDK_ARFOUNDATION must be defined in Scripting Define Symbols",
                category = "Scripting Define Symbols",
                checkPredicate = () => IsDefineSymbolSet("VLSDK_ARFOUNDATION"),
                fixIt = () => AddDefineSymbol("VLSDK_ARFOUNDATION"),
                fixItMessage = "Add VLSDK_ARFOUNDATION to Scripting Define Symbols",
                fixItAutomatic = false,
                buildTargetGroup = null,
                error = true
            });

            // Define Symbol: VLSDK_NEWTONSOFT_JSON
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "VLSDK_NEWTONSOFT_JSON must be defined in Scripting Define Symbols",
                category = "Scripting Define Symbols",
                checkPredicate = () => IsDefineSymbolSet("VLSDK_NEWTONSOFT_JSON"),
                fixIt = () => AddDefineSymbol("VLSDK_NEWTONSOFT_JSON"),
                fixItMessage = "Add VLSDK_NEWTONSOFT_JSON to Scripting Define Symbols",
                fixItAutomatic = false,
                buildTargetGroup = null,
                error = true
            });

            // Multiple Main Cameras Validation
            s_ValidationRules.Add(new VLSDKValidationRule
            {
                message = "Active scene should not have more than one enabled Main Camera",
                category = "Scene Setup",
                checkPredicate = () => HasSingleMainCamera(),
                fixIt = null,
                fixItMessage = null,
                fixItAutomatic = false,
                buildTargetGroup = null,
                error = false
            });
        }

        private static bool IsCameraUsageDescriptionSet()
        {
            string cameraUsageDescription = PlayerSettings.iOS.cameraUsageDescription;
            return !string.IsNullOrEmpty(cameraUsageDescription);
        }

        private static void SetCameraUsageDescription()
        {
            PlayerSettings.iOS.cameraUsageDescription = "This app requires camera access for AR functionality";
            Debug.Log("Camera Usage Description has been set.");
        }

        private static bool IsLocationUsageDescriptionSet()
        {
            string locationUsageDescription = PlayerSettings.iOS.locationUsageDescription;
            return !string.IsNullOrEmpty(locationUsageDescription);
        }

        private static void SetLocationUsageDescription()
        {
            PlayerSettings.iOS.locationUsageDescription = "This app requires location access for localization";
            Debug.Log("Location Usage Description has been set.");
        }

        private static bool IsARCoreEnabled()
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null || settings.Manager == null)
                return false;

            var loaders = settings.Manager.activeLoaders;
            return loaders.Any(loader => loader.GetType().Name.Contains("ARCore"));
        }

        private static bool IsARKitEnabled()
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.iOS);
            if (settings == null || settings.Manager == null)
                return false;

            var loaders = settings.Manager.activeLoaders;
            return loaders.Any(loader => loader.GetType().Name.Contains("ARKit"));
        }

        private static bool IsGraphicsAPICorrect()
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            return graphicsAPIs.Length == 1 && graphicsAPIs[0] == GraphicsDeviceType.OpenGLES3;
        }

        private static void SetGraphicsAPI()
        {
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            Debug.Log("Graphics API has been set to OpenGLES3 only.");
        }

        private static bool IsMinimumAPILevelCorrect()
        {
            return PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel25;
        }

        private static void SetMinimumAPILevel()
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            Debug.Log("Minimum API Level has been set to 24.");
        }

        private static bool IsDefineSymbolSet(string symbol)
        {
            NamedBuildTarget[] namedTargets = new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS };

            foreach (var namedTarget in namedTargets)
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                var symbolList = defines.Split(';').Select(s => s.Trim()).ToList();

                if (!symbolList.Contains(symbol))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddDefineSymbol(string symbol)
        {
            NamedBuildTarget[] namedTargets = new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS };

            foreach (var namedTarget in namedTargets)
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                var symbolList = defines.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (!symbolList.Contains(symbol))
                {
                    symbolList.Add(symbol);
                    string newDefines = string.Join(";", symbolList);
                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
                    Debug.Log($"{symbol} has been added to Scripting Define Symbols for {namedTarget}.");
                }
            }
        }

        private static bool IsScriptingBackendIL2CPP()
        {
            return PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP;
        }

        private static void SetScriptingBackendIL2CPP()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            Debug.Log("Scripting Backend has been set to IL2CPP.");
        }

        private static bool IsTargetArchitectureARM64Only()
        {
            var targetArchitectures = PlayerSettings.Android.targetArchitectures;
            return targetArchitectures == AndroidArchitecture.ARM64;
        }

        private static void SetTargetArchitectureARM64Only()
        {
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log("Target Architectures has been set to ARM64 only.");
        }

        private static bool HasSingleMainCamera()
        {
            // Get active scene
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                return true; // Skip validation if no scene is loaded
            }

            // Find all cameras tagged as MainCamera that are enabled
            var mainCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(cam => cam.tag == "MainCamera" && cam.enabled && cam.gameObject.activeInHierarchy)
                .ToList();

            return mainCameras.Count <= 1;
        }

        public static void RefreshRules()
        {
            s_ValidationRules = null;
            GetRules();
        }
    }
}
