#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.Release026
{
    public static class FinalExecutionWindowsBuild026
    {
        private const string BootScene = "Assets/Scenes/Boot.unity";
        private const string ExeName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";

        [MenuItem("Second Dimension/Final Execution 026/Build Clean Windows Owner Review")]
        public static void Build()
        {
            FinalExecutionValidation026.Validate();
            var project = Directory.GetParent(Application.dataPath).FullName;
            var output = Path.Combine(project, "Builds", "Windows_Owner_Review_026");
            if (Directory.Exists(output)) Directory.Delete(output, true);
            Directory.CreateDirectory(output);

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.22f1.");

            var startedUtc = DateTime.UtcNow;
            var exe = Path.Combine(output, ExeName);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { BootScene },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            var summary = report.summary;
            File.WriteAllText(Path.Combine(output, "BUILD_SUMMARY_026.txt"),
                "Result: " + summary.result + "\n" +
                "Output: " + summary.outputPath + "\n" +
                "Warnings: " + summary.totalWarnings + "\n" +
                "Errors: " + summary.totalErrors + "\n" +
                "Time: " + summary.totalTime + "\n" +
                "Size: " + summary.totalSize + "\n" +
                "StartedUtc: " + startedUtc.ToString("o") + "\n" +
                "FinishedUtc: " + DateTime.UtcNow.ToString("o") + "\n");
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + summary.result);

            var data = Path.Combine(output, "SECOND_DIMENSION_GUILD_OF_WORLDS_Data");
            var player = Path.Combine(output, "UnityPlayer.dll");
            if (!File.Exists(exe) || !Directory.Exists(data) || !File.Exists(player))
                throw new InvalidOperationException("Complete, newly generated Unity build folder is required.");
            if (File.GetLastWriteTimeUtc(exe) < startedUtc.AddMinutes(-1))
                throw new InvalidOperationException("The Windows executable appears stale.");

            File.WriteAllText(Path.Combine(output, "PLAY_SECOND_DIMENSION.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"" + ExeName + "\"\r\n");
            File.WriteAllText(Path.Combine(output, "README_PLAY_BUILD_026.txt"),
                "Keep the entire folder together. Complete the owner smoke path and save/relaunch proof before approval.\n");
            WriteHashes(output);
            Debug.Log("Final Execution 026 clean Windows build succeeded: " + exe);
        }

        public static void BuildFromCommandLine()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void WriteHashes(string root)
        {
            var output = new StringBuilder();
            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                if (path.EndsWith("BUILD_SHA256_026.txt", StringComparison.OrdinalIgnoreCase)) continue;
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                    output.Append(string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2"))))
                        .Append("  ").Append(path.Substring(root.Length + 1).Replace('\\', '/')).Append('\n');
            }
            File.WriteAllText(Path.Combine(root, "BUILD_SHA256_026.txt"), output.ToString());
        }
    }
}
#endif
