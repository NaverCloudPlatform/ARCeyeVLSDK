using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

[InitializeOnLoad]
public static class VLSDKGradleGenerator
{
    static string[] s_Dependencies = {
        // ***** ADD DYNAMIC DEPENDENCIES *****
        "com.google.android.gms:play-services-mlkit-face-detection:17.1.0",

    };

    // Static constructor - called when the Unity Editor loads
    static VLSDKGradleGenerator()
    {
        // Register the Initialize method to be called on the first update
        EditorApplication.update += Initialize;
    }

    private static void Initialize()
    {
        // Unregister the Initialize method to ensure it runs only once
        EditorApplication.update -= Initialize;

        // Path to Assets/Plugins/Android directory
        string androidPluginPath = Path.Combine(Application.dataPath, "Plugins", "Android");

        // Check if Assets/Plugins/Android directory exists
        if (!Directory.Exists(androidPluginPath))
        {
            Debug.Log("Assets/Plugins/Android directory does not exist. Creating it.");
            Directory.CreateDirectory(androidPluginPath);

            // Path to mainTemplate.gradle
            string mainTemplatePath = Path.Combine(androidPluginPath, "mainTemplate.gradle");

            // Content to write to mainTemplate.gradle
            string mainTemplateContent = GetMainTemplateContent();

            // Create mainTemplate.gradle with the specified content
            File.WriteAllText(mainTemplatePath, mainTemplateContent);
            Debug.Log("mainTemplate.gradle has been created.");
            AssetDatabase.Refresh();
            return;
        }

        // Paths to mainTemplate.gradle and mainTemplate.gradle.DISABLED
        string mainTemplate = Path.Combine(androidPluginPath, "mainTemplate.gradle");
        string disabledTemplate = Path.Combine(androidPluginPath, "mainTemplate.gradle.DISABLED");

        if (File.Exists(disabledTemplate))
        {
            Debug.Log("mainTemplate.gradle.DISABLED exists. Activate this file.");
            File.Move(disabledTemplate, mainTemplate);
            AssetDatabase.Refresh();
        }
        else if (File.Exists(mainTemplate))
        {
            //Debug.Log("mainTemplate.gradle already exists. No action taken.");
        }
        else
        {
            // If neither mainTemplate.gradle nor .DISABLED exists, create mainTemplate.gradle
            Debug.Log("mainTemplate.gradle does not exist. Creating it.");
            string mainTemplateContent = GetMainTemplateContent();
            File.WriteAllText(mainTemplate, mainTemplateContent);
            Debug.Log("mainTemplate.gradle has been created.");
            AssetDatabase.Refresh();
        }

        // Process dynamic dependenices
        foreach(string dependency in s_Dependencies) {
            AddDynamicDependency(dependency);
        }

        // Postprocessing
        CheckForObsoleteDependencies();
        // RemoveObsoleteDependencies();         /* This case might be not work for the case that user has mainTemplate already */
    }

    /// <summary>
    /// Returns the content for mainTemplate.gradle with placeholders.
    /// </summary>
    /// <returns>String content of mainTemplate.gradle</returns>
    private static string GetMainTemplateContent()
    {
        return @"apply plugin: 'com.android.library'
**APPLY_PLUGINS**

dependencies {
    implementation fileTree(dir: 'libs', include: ['*.jar'])
    **DEPS**
}

android {
    namespace ""com.unity3d.player""
    ndkPath ""**NDKPATH**""
    compileSdkVersion **APIVERSION**
    buildToolsVersion '**BUILDTOOLS**'

    compileOptions {
        sourceCompatibility JavaVersion.VERSION_11
        targetCompatibility JavaVersion.VERSION_11
    }

    defaultConfig {
        minSdkVersion **MINSDKVERSION**
        targetSdkVersion **TARGETSDKVERSION**
        ndk {
            abiFilters **ABIFILTERS**
        }
        versionCode **VERSIONCODE**
        versionName '**VERSIONNAME**'
        consumerProguardFiles 'proguard-unity.txt'**USER_PROGUARD**
    }

    lintOptions {
        abortOnError false
    }

    aaptOptions {
        noCompress = **BUILTIN_NOCOMPRESS** + unityStreamingAssets.tokenize(', ')
        ignoreAssetsPattern = ""!.svn:!.git:!.ds_store:!*.scc:!CVS:!thumbs.db:!picasa.ini:!*~""
    }
    **PACKAGING_OPTIONS**
}
**IL_CPP_BUILD_SETUP**
**SOURCE_BUILD_SETUP**
**EXTERNAL_SOURCES**
";
    }

    /// <summary>
    /// Adds a dependency to the mainTemplate.gradle file, ensuring no duplicates.
    /// </summary>
    /// <param name="dependency">The dependency string to add.</param>
    private static void AddDynamicDependency(string dependency)
    {
        // Path to mainTemplate.gradle
        string mainTemplatePath = Path.Combine(Application.dataPath, "Plugins", "Android", "mainTemplate.gradle");

        if (!File.Exists(mainTemplatePath))
        {
            Debug.LogError("mainTemplate.gradle does not exist. Cannot add dependencies.");
            return;
        }

        string gradleContent = File.ReadAllText(mainTemplatePath);

        // Regex to find the dependencies block
        Regex dependenciesBlockRegex = new Regex(@"dependencies\s*\{([^}]*)\}", RegexOptions.Multiline | RegexOptions.Singleline);
        Match match = dependenciesBlockRegex.Match(gradleContent);

        if (match.Success)
        {
            string dependenciesBlock = match.Groups[1].Value;

            // Check if the dependency already exists
            if (dependenciesBlock.Contains(dependency))
            {
                return;
            }

            // Check for similar dependencies (same library but different version)
            // For simplicity, assume the library part before the last colon is the same
            string libraryIdentifier = dependency.Substring(0, dependency.LastIndexOf(':'));
            Regex similarDependencyRegex = new Regex($"implementation\\s+'{Regex.Escape(libraryIdentifier)}:[^']+'");

            if (similarDependencyRegex.IsMatch(dependenciesBlock))
            {
                // Replace the existing dependency with the new one
                gradleContent = similarDependencyRegex.Replace(gradleContent, $"implementation '{dependency}'");
                Debug.LogWarning($"Replace existing dependency to '{dependency}'");
            }
            else
            {
                // Add the new dependency before **DEPS** or at the end of the dependencies block
                int insertPosition = match.Index + match.Length - 1; // Before the closing }

                // Define the indentation (assuming 4 spaces)
                string indentation = "    ";

                // Insert the dependency
                gradleContent = gradleContent.Insert(insertPosition, $"{indentation}implementation '{dependency}'\n");

                Debug.Log($"Add dependency '{dependency}' to mainTemplate.gradle.");
            }

            // Write the updated content back to the file
            File.WriteAllText(mainTemplatePath, gradleContent);
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("Could not find the dependencies block in mainTemplate.gradle.");
        }
    }

    /// <summary>
    /// Checks for dependencies in mainTemplate.gradle that are not in s_Dependencies and logs warnings.
    /// </summary>
    private static void CheckForObsoleteDependencies()
    {
        // Path to mainTemplate.gradle
        string mainTemplatePath = Path.Combine(Application.dataPath, "Plugins", "Android", "mainTemplate.gradle");

        if (!File.Exists(mainTemplatePath))
        {
            Debug.LogError("mainTemplate.gradle does not exist. Cannot check for obsolete dependencies.");
            return;
        }

        string gradleContent = File.ReadAllText(mainTemplatePath);

        // Regex to find the dependencies block
        Regex dependenciesBlockRegex = new Regex(@"dependencies\s*\{([^}]*)\}", RegexOptions.Multiline | RegexOptions.Singleline);
        Match match = dependenciesBlockRegex.Match(gradleContent);

        if (match.Success)
        {
            string dependenciesBlock = match.Groups[1].Value;

            // Extract all implementation dependencies
            Regex implementationRegex = new Regex(@"implementation\s+'([^']+)'");
            MatchCollection implementationMatches = implementationRegex.Matches(dependenciesBlock);

            // List to store dependencies from the gradle file
            List<string> gradleDependencies = new List<string>();

            foreach (Match depMatch in implementationMatches)
            {
                if (depMatch.Groups.Count > 1)
                {
                    gradleDependencies.Add(depMatch.Groups[1].Value);
                }
            }

            // Compare gradleDependencies with s_Dependencies
            foreach (string gradleDep in gradleDependencies)
            {
                bool existsInSDependencies = false;
                foreach (string sDep in s_Dependencies)
                {
                    // Compare library identifiers (group:artifact)
                    string gradleLib = GetLibraryIdentifier(gradleDep);
                    string sLib = GetLibraryIdentifier(sDep);

                    if (gradleLib == sLib)
                    {
                        existsInSDependencies = true;
                        break;
                    }
                }

                if (!existsInSDependencies)
                {
                    Debug.LogWarning($"Dependency '{gradleDep}' exists in mainTemplate.gradle but is not listed in the target list. Consider this to avoid conflicts.");
                }
            }
        }
        else
        {
            Debug.LogError("Could not find the dependencies block in mainTemplate.gradle.");
        }
    }

    /// <summary>
    /// Removes dependencies from mainTemplate.gradle that are not present in s_Dependencies.
    /// </summary>
    private static void RemoveObsoleteDependencies()
    {
        // Path to mainTemplate.gradle
        string mainTemplatePath = Path.Combine(Application.dataPath, "Plugins", "Android", "mainTemplate.gradle");

        if (!File.Exists(mainTemplatePath))
        {
            Debug.LogError("mainTemplate.gradle does not exist. Cannot remove obsolete dependencies.");
            return;
        }

        string gradleContent = File.ReadAllText(mainTemplatePath);

        // Regex to find the dependencies block
        Regex dependenciesBlockRegex = new Regex(@"dependencies\s*\{([^}]*)\}", RegexOptions.Multiline | RegexOptions.Singleline);
        Match match = dependenciesBlockRegex.Match(gradleContent);

        if (match.Success)
        {
            string dependenciesBlock = match.Groups[1].Value;

            // Extract all implementation dependencies
            Regex implementationRegex = new Regex(@"implementation\s+'([^']+)'");
            MatchCollection implementationMatches = implementationRegex.Matches(dependenciesBlock);

            // List to store dependencies from the gradle file
            List<string> gradleDependencies = new List<string>();

            foreach (Match depMatch in implementationMatches)
            {
                if (depMatch.Groups.Count > 1)
                {
                    gradleDependencies.Add(depMatch.Groups[1].Value);
                }
            }

            // List to track dependencies to be removed
            List<string> dependenciesToRemove = new List<string>();

            // Identify dependencies that are not in s_Dependencies
            foreach (string gradleDep in gradleDependencies)
            {
                bool existsInSDependencies = false;
                foreach (string sDep in s_Dependencies)
                {
                    // Compare library identifiers (group:artifact)
                    string gradleLib = GetLibraryIdentifier(gradleDep);
                    string sLib = GetLibraryIdentifier(sDep);

                    if (gradleLib == sLib)
                    {
                        existsInSDependencies = true;
                        break;
                    }
                }

                if (!existsInSDependencies)
                {
                    dependenciesToRemove.Add(gradleDep);
                }
            }

            // Remove obsolete dependencies
            foreach (string obsoleteDep in dependenciesToRemove)
            {
                // Regex pattern to match the exact dependency line
                string pattern = $@"^\s*implementation\s+'{Regex.Escape(obsoleteDep)}'\s*$";
                gradleContent = Regex.Replace(gradleContent, pattern, string.Empty, RegexOptions.Multiline);

                Debug.LogWarning($"Removed obsolete dependency '{obsoleteDep}' from mainTemplate.gradle.");
            }

            // Optionally, clean up extra blank lines or spaces left by removals
            gradleContent = Regex.Replace(gradleContent, @"\n\s*\n", "\n"); // Remove consecutive blank lines

            // Write the updated content back to the file
            File.WriteAllText(mainTemplatePath, gradleContent);
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("Could not find the dependencies block in mainTemplate.gradle.");
        }
    }

    /// <summary>
    /// Extracts the library identifier (group:artifact) from a dependency string.
    /// </summary>
    /// <param name="dependency">The dependency string (e.g., com.google.android.gms:play-services-location:21.3.0)</param>
    /// <returns>The library identifier (e.g., com.google.android.gms:play-services-location)</returns>
    private static string GetLibraryIdentifier(string dependency)
    {
        int lastColonIndex = dependency.LastIndexOf(':');
        if (lastColonIndex > 0)
        {
            return dependency.Substring(0, lastColonIndex);
        }
        return dependency;
    }
}