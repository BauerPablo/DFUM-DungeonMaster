// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Helper for automating standalone Windows Player production builds with Native Multiplayer.
    /// Automatically builds Addressables Localization bundles before Player builds to prevent
    /// SelectedLocale null exceptions on standalone clients.
    /// </summary>
    [InitializeOnLoad]
    public class StandaloneBuildHelper : IPreprocessBuildWithReport
    {
        static StandaloneBuildHelper()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EnsureAddressablesBuilt();
                }
            };
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureAddressablesBuilt();
        }

        [MenuItem("Daggerfall Tools/Multiplayer/Build Addressables Content")]
        public static void EnsureAddressablesBuilt()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string aaPath = Path.Combine(projectRoot, "Library", "com.unity.addressables", "aa", "Windows");
            string settingsJson = Path.Combine(aaPath, "settings.json");

            bool needsBuild = !Directory.Exists(aaPath)
                || !File.Exists(settingsJson)
                || !File.ReadAllText(settingsJson).Contains("\"1.21.1\"");

            if (needsBuild)
            {
                Debug.Log("[DFU Build] Building Addressables Player Content (v1.21.1 Localization String Tables)...");
                AddressableAssetSettings.BuildPlayerContent();
            }

            SyncAddressablesToTestingBuild(aaPath, projectRoot);
        }

        private static void SyncAddressablesToTestingBuild(string aaWindowsPath, string projectRoot)
        {
            try
            {
                string sourceAaRoot = Path.Combine(projectRoot, "Library", "com.unity.addressables", "aa");
                string targetStreamingAssets = Path.Combine(projectRoot, "z_Testing_compilation", "Daggerfall Unity_Data", "StreamingAssets");
                if (!Directory.Exists(sourceAaRoot) || !Directory.Exists(targetStreamingAssets))
                    return;

                string sourceSettings = Path.Combine(aaWindowsPath, "settings.json");
                if (!File.Exists(sourceSettings) || !File.ReadAllText(sourceSettings).Contains("\"1.21.1\""))
                    return;

                string targetAaRoot = Path.Combine(targetStreamingAssets, "aa");
                string targetSettings = Path.Combine(targetAaRoot, "Windows", "settings.json");
                if (File.Exists(targetSettings) && File.ReadAllText(targetSettings).Contains("\"1.21.1\""))
                    return;

                foreach (string dirPath in Directory.GetDirectories(sourceAaRoot, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceAaRoot, targetAaRoot));
                }
                foreach (string filePath in Directory.GetFiles(sourceAaRoot, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(filePath, filePath.Replace(sourceAaRoot, targetAaRoot), true);
                }
                Debug.Log("[DFU Build] Synchronized v1.21.1 Addressables bundles to z_Testing_compilation.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DFU Build] Could not sync Addressables to z_Testing_compilation: {ex.Message}");
            }
        }

        [MenuItem("Daggerfall Tools/Build Standalone Windows Player (x64)")]
        public static void BuildWindowsPlayer()
        {
            EnsureAddressablesBuilt();

            string[] scenes = new string[]
            {
                "Assets/Scenes/DaggerfallUnityStartup.unity",
                "Assets/Scenes/DaggerfallUnityGame.unity"
            };

            string buildDir = "Builds/StandaloneWindows64";
            string buildPath = System.IO.Path.Combine(buildDir, "DaggerfallUnity.exe");

            if (!System.IO.Directory.Exists(buildDir))
            {
                System.IO.Directory.CreateDirectory(buildDir);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[DFU Build] Starting Standalone Windows64 Player build to: {buildPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[DFU Build] Build succeeded! Total time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024 * 1024)} MB");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[DFU Build] Build failed with {summary.totalErrors} errors!");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
