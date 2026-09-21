#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.GuildCity017H
{
    public static class GuildCityDefenseCanonWindowsBuild017H
    {
        private const string BootScene = "Assets/Scenes/Boot.unity";
        private const string ExecutableName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        private const string OutputFolderName = "Windows_Owner_Review_017H";

        [MenuItem("Second Dimension/Build/Windows Guild City Defense & Canon 017H")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void Build()
        {
            GuildCityDefenseCanonValidation017H.Validate();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var bootDiskPath = Path.Combine(projectRoot, BootScene.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(bootDiskPath)) throw new FileNotFoundException("Boot scene is missing.", bootDiskPath);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows Build Support is unavailable for Unity 6000.3.22f1.");

            var scenes = new List<string> { BootScene };
            scenes.AddRange(EditorBuildSettings.scenes
                .Where(value => value != null && value.enabled && !string.IsNullOrWhiteSpace(value.path))
                .Select(value => value.path)
                .Where(value => !StringComparer.Ordinal.Equals(value, BootScene)));

            var outputFolder = Path.Combine(projectRoot, "Builds", OutputFolderName);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, ExecutableName);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.Distinct(StringComparer.Ordinal).ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            var summary = report.summary;
            File.WriteAllText(Path.Combine(outputFolder, "BUILD_SUMMARY_017H.txt"), new StringBuilder()
                .AppendLine("SECOND DIMENSION — GUILD CITY DEFENSE & CANON 017H")
                .AppendLine("Result: " + summary.result)
                .AppendLine("Output: " + summary.outputPath)
                .AppendLine("Size: " + summary.totalSize)
                .AppendLine("Time: " + summary.totalTime)
                .AppendLine("Warnings: " + summary.totalWarnings)
                .AppendLine("Errors: " + summary.totalErrors)
                .AppendLine("Unity: " + Application.unityVersion)
                .ToString());
            File.WriteAllText(Path.Combine(outputFolder, "PLAY_SECOND_DIMENSION.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"" + ExecutableName + "\"\r\n");
            File.WriteAllText(Path.Combine(outputFolder, "README_PLAY_BUILD_017H.txt"),
                "Keep this complete folder together. Verify 30 building contributions, ordinary expeditions, a city-defense operation, decisive certified Union battle, Chronicle event gates, reward claim, manual equipment, save, close, and relaunch.\r\n");
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + summary.result);
            if (!File.Exists(outputPath)) throw new FileNotFoundException("Expected EXE is missing.", outputPath);
            WriteHashes(outputFolder);
        }

        private static void WriteHashes(string folder)
        {
            var manifest = Path.Combine(folder, "BUILD_SHA256_017H.txt");
            var lines = new List<string>();
            foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Where(value => !StringComparer.OrdinalIgnoreCase.Equals(value, manifest))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                lines.Add(Hash(file) + "  " + Relative(folder, file).Replace('\\', '/'));
            File.WriteAllLines(manifest, lines.ToArray());
        }
        private static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant(); }
        private static string Relative(string root, string path)
        { var a = new Uri(Append(Path.GetFullPath(root))); var b = new Uri(Path.GetFullPath(path)); return Uri.UnescapeDataString(a.MakeRelativeUri(b).ToString()); }
        private static string Append(string path) => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
    }
}
#endif
