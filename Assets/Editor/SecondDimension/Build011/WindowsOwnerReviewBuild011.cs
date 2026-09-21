#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor
{
    public static class WindowsOwnerReviewBuild011
    {
        private const string BootScene = "Assets/Scenes/Boot.unity";
        private const string Executable = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";

        [MenuItem("Second Dimension/Build/Windows Owner Review 011", false, 5200)]
        public static void BuildFromMenu() => Build(false);

        public static void BuildFromCommandLine()
        {
            try
            {
                Build(false);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Build(bool development)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Could not resolve project root.");
            var bootSceneOnDisk = Path.Combine(projectRoot, BootScene.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(bootSceneOnDisk))
                throw new FileNotFoundException("Required Boot scene is missing.", bootSceneOnDisk);

            var scenes = EditorBuildSettings.scenes
                .Where(value => value.enabled && !string.IsNullOrWhiteSpace(value.path))
                .Select(value => value.path)
                .ToArray();
            if (!scenes.Contains(BootScene, StringComparer.Ordinal))
                throw new InvalidOperationException(BootScene + " must be enabled in Build Settings.");

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException(
                    "Windows x86_64 target is unavailable. Install Windows Build Support in Unity Hub.");

            var outputFolder = Path.Combine(projectRoot, "Builds", "Windows_Owner_Review_011");
            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, Executable);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development ? BuildOptions.Development : BuildOptions.None
            });

            var summary = report.summary;
            var text = new StringBuilder()
                .AppendLine("SECOND DIMENSION — WINDOWS OWNER REVIEW BUILD 011")
                .AppendLine("Result: " + summary.result)
                .AppendLine("Output: " + summary.outputPath)
                .AppendLine("Size: " + summary.totalSize)
                .AppendLine("Time: " + summary.totalTime)
                .AppendLine("Warnings: " + summary.totalWarnings)
                .AppendLine("Errors: " + summary.totalErrors)
                .ToString();
            File.WriteAllText(Path.Combine(outputFolder, "BUILD_SUMMARY_011.txt"), text);
            File.WriteAllText(
                Path.Combine(outputFolder, "PLAY_SECOND_DIMENSION.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"" + Executable + "\"\r\n");
            File.WriteAllText(
                Path.Combine(outputFolder, "README_PLAY_BUILD_011.txt"),
                "Keep the EXE, UnityPlayer.dll, the _Data folder, and every generated dependency together. Double-click PLAY_SECOND_DIMENSION.cmd or " + Executable + ".\r\n");

            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + summary.result);
            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Unity reported success but the expected executable is missing.", outputPath);

            WriteSha256Manifest(outputFolder);
            Debug.Log("Windows owner-review build 011 succeeded: " + outputPath);
        }

        private static void WriteSha256Manifest(string outputFolder)
        {
            var manifestPath = Path.Combine(outputFolder, "BUILD_SHA256_011.txt");
            var files = Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories)
                .Where(path => !StringComparer.OrdinalIgnoreCase.Equals(path, manifestPath))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            using (var sha = SHA256.Create())
            using (var writer = new StreamWriter(manifestPath, false, new UTF8Encoding(false)))
            {
                foreach (var path in files)
                {
                    byte[] digest;
                    using (var stream = File.OpenRead(path)) digest = sha.ComputeHash(stream);
                    var hex = BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
                    var relative = path.Substring(outputFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');
                    writer.WriteLine(hex + "  " + relative);
                }
            }
        }
    }
}
#endif
