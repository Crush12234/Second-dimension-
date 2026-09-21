#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.Release023
{
    public static class FinalReleaseWindowsBuild023
    {
        const string BootScene = "Assets/Scenes/Boot.unity";
        const string ExeName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";

        [MenuItem("Second Dimension/Final Release 023/Build Windows Owner Review")]
        public static void Build()
        {
            FinalReleaseValidation023.Validate();
            var project = Directory.GetParent(Application.dataPath).FullName;
            var output = Path.Combine(project, "Builds", "Windows_Owner_Review_023");
            Directory.CreateDirectory(output);
            var boot = Path.Combine(project, BootScene);
            if (!File.Exists(boot)) throw new FileNotFoundException(BootScene);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.22f1.");
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
            File.WriteAllText(Path.Combine(output, "BUILD_SUMMARY_023.txt"),
                "Result: " + summary.result + "\nOutput: " + summary.outputPath + "\nWarnings: " + summary.totalWarnings + "\nErrors: " + summary.totalErrors + "\nTime: " + summary.totalTime + "\nSize: " + summary.totalSize + "\n");
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + summary.result);
            var dataDir = Path.Combine(output, "SECOND_DIMENSION_GUILD_OF_WORLDS_Data");
            var player = Path.Combine(output, "UnityPlayer.dll");
            if (!File.Exists(exe) || !Directory.Exists(dataDir) || !File.Exists(player))
                throw new InvalidOperationException("Unity build is incomplete; EXE, _Data, and UnityPlayer.dll are all required.");
            File.WriteAllText(Path.Combine(output, "PLAY_SECOND_DIMENSION.cmd"), "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"" + ExeName + "\"\r\n");
            File.WriteAllText(Path.Combine(output, "README_PLAY_BUILD_023.txt"), "Keep this entire folder together. Launch PLAY_SECOND_DIMENSION.cmd or " + ExeName + ". Complete the full owner-review smoke matrix and save/relaunch proof before approval.\n");
            WriteHashes(output);
            Debug.Log("Final Release 023 Windows build succeeded: " + exe);
        }

        public static void BuildFromCommandLine()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        static void WriteHashes(string root)
        {
            var output = new StringBuilder();
            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                if (path.EndsWith("BUILD_SHA256_023.txt", StringComparison.OrdinalIgnoreCase)) continue;
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var hash = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
                    output.Append(hash).Append("  ").Append(path.Substring(root.Length + 1).Replace('\\', '/')).Append('\n');
                }
            }
            File.WriteAllText(Path.Combine(root, "BUILD_SHA256_023.txt"), output.ToString());
        }
    }
}
#endif
