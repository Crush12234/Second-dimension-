#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.Studio163
{
    /// <summary>A portable, reproducible entry point for future full Unity builds.</summary>
    public static class PortableAlphaBuild163
    {
        [MenuItem("Second Dimension/Build/Windows portable alpha")]
        public static void Build()
        {
            try
            {
                var project = Directory.GetParent(Application.dataPath).FullName;
                var argument = Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("--sd-build-output=", StringComparison.Ordinal));
                var output = Path.GetFullPath(argument == null
                    ? Path.Combine(project, "Builds", "Alpha_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"))
                    : argument.Substring("--sd-build-output=".Length));
                var assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
                if (output.StartsWith(assets, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(output, project, StringComparison.OrdinalIgnoreCase) ||
                    (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()))
                    throw new InvalidOperationException("Choose a new empty build folder outside Assets. Existing files and saves are never replaced.");
                if (!File.Exists(Path.Combine(project, "Assets/Scenes/Boot.unity")))
                    throw new FileNotFoundException("The complete project and Boot scene are required.");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.22f1.");
                Directory.CreateDirectory(output);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Boot.unity" },
                    locationPathName = Path.Combine(output, "SECOND_DIMENSION_GUILD_OF_WORLDS.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.None
                });
                File.WriteAllText(Path.Combine(output, "BUILD_REPORT.txt"),
                    "Unity " + Application.unityVersion + "\nResult: " + report.summary.result +
                    "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings +
                    "\nBuilt UTC: " + DateTime.UtcNow.ToString("O") + "\n");
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new InvalidOperationException("Full Unity build failed; see BUILD_REPORT.txt and the Editor log.");
                File.WriteAllText(Path.Combine(output, "SECOND_DIMENSION_PORTABLE_ALPHA.txt"), "SECOND_DIMENSION_PORTABLE_ALPHA132\n");
                Directory.CreateDirectory(Path.Combine(output, "SaveData"));
                File.WriteAllText(Path.Combine(output, "START_GAME.cmd"),
                    "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"Second Dimension\" \"%~dp0SECOND_DIMENSION_GUILD_OF_WORLDS.exe\"\r\n");
                Debug.Log("Windows portable build complete: " + output);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
#endif
