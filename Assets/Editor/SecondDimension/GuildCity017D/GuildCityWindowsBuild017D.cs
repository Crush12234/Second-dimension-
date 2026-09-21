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

namespace SecondDimension.Editor.GuildCity017D
{
    public static class GuildCityWindowsBuild017D
    {
        private const string BootScene = "Assets/Scenes/Boot.unity";
        private const string ExecutableName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        private const string OutputFolderName = "Windows_Owner_Review_017D";

        [MenuItem("Second Dimension/Build/Windows Guild City Owner Review 017D")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Build()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var bootDiskPath = Path.Combine(projectRoot, BootScene.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(bootDiskPath))
                throw new FileNotFoundException("Boot scene is missing.", bootDiskPath);

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException(
                    "Windows Build Support is unavailable. Install it for Unity 6000.3.22f1 in Unity Hub.");

            var scenes = new List<string> { BootScene };
            scenes.AddRange(EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .Where(path => !string.Equals(path, BootScene, StringComparison.Ordinal)));

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
            File.WriteAllText(
                Path.Combine(outputFolder, "BUILD_SUMMARY_017D.txt"),
                new StringBuilder()
                    .AppendLine("SECOND DIMENSION — GUILD/CITY OWNER REVIEW 017D")
                    .AppendLine("Result: " + summary.result)
                    .AppendLine("Output: " + summary.outputPath)
                    .AppendLine("Size: " + summary.totalSize)
                    .AppendLine("Time: " + summary.totalTime)
                    .AppendLine("Warnings: " + summary.totalWarnings)
                    .AppendLine("Errors: " + summary.totalErrors)
                    .AppendLine("Unity: " + Application.unityVersion)
                    .AppendLine("Required loop: Hall/City -> Recruit -> Equip -> Union -> Contract -> Board -> Battle -> Return -> Build -> Save/Relaunch")
                    .ToString());

            File.WriteAllText(
                Path.Combine(outputFolder, "PLAY_SECOND_DIMENSION.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"" + ExecutableName + "\"\r\n");
            File.WriteAllText(
                Path.Combine(outputFolder, "README_PLAY_BUILD_017D.txt"),
                "Keep this entire folder together. Double-click PLAY_SECOND_DIMENSION.cmd or " +
                ExecutableName + ". The EXE cannot be distributed alone.\r\n\r\n" +
                "Owner proof: recruit a permanent member, equip and assign a Union, accept a contract, " +
                "move the expedition board, resolve an event/check, enter the cinematic Union battle, " +
                "claim the exact reward, return to the exact board node, finalize the operation, place/" +
                "staff a city building, save, close, relaunch, and verify persistence.\r\n");

            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + summary.result);
            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Unity reported success but the expected EXE is missing.", outputPath);

            WriteHashes(outputFolder);
            Debug.Log("Windows Guild/City owner-review build 017D succeeded: " + outputPath);
        }

        private static void WriteHashes(string outputFolder)
        {
            var manifestPath = Path.Combine(outputFolder, "BUILD_SHA256_017D.txt");
            var lines = new List<string>();
            foreach (var file in Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories)
                         .Where(file => !string.Equals(file, manifestPath, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
                lines.Add(Sha256(file) + "  " + Relative(outputFolder, file).Replace('\\', '/'));
            File.WriteAllLines(manifestPath, lines.ToArray());
        }

        private static string Sha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string Relative(string root, string path)
        {
            var rootUri = new Uri(AppendSeparator(Path.GetFullPath(root)));
            var pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
        }

        private static string AppendSeparator(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
    }
}
#endif
