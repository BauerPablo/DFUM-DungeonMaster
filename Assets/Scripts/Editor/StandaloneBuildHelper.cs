// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DaggerfallWorkshop.Editor
{
    /// <summary>
    /// Helper for automating standalone Windows Player production builds with Native Multiplayer.
    /// </summary>
    public static class StandaloneBuildHelper
    {
        [MenuItem("Daggerfall Tools/Build Standalone Windows Player (x64)")]
        public static void BuildWindowsPlayer()
        {
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
