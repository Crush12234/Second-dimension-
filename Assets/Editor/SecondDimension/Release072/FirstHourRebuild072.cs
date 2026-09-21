#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SecondDimension.Presentation.FirstHour072;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.Release072
{
    /// <summary>
    /// Exact build authority for the replacement opening. The delivery launcher owns
    /// the short output directory and immutable build identity through command-line
    /// arguments; this method refuses an older identity or an unsafe output target.
    /// </summary>
    public static class FirstHourRebuild072
    {
        public const string BuildId = "FIRST-HOUR-REBUILD-072";
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string ExecutableName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        private const string PreparePassMarker = "FIRST HOUR REBUILD 072 PREPARE PASS";
        private const string BuildPassMarker = "FIRST HOUR REBUILD 072 BUILD PASS";

        private static readonly string[] EssentialProjectFiles =
        {
            ScenePath,
            "Assets/SecondDimension/Presentation/Boot/BootCoordinator.cs",
            "Assets/SecondDimension/Presentation/FirstHour072/FirstHourExperienceRoot.cs",
            "Assets/SecondDimension/Presentation/FirstHour072/FirstHourOpeningState072.cs",
            "Assets/SecondDimension/Presentation/FirstHour072/FirstHourExperienceContracts072.cs",
            "Assets/SecondDimension/Presentation/FirstHour072/M1RuntimeCoordinator.FirstHour072.cs",
            "Assets/SecondDimension/Presentation/FirstHour072/FirstHourWorldStage072.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/WalkableSkyhomeArrival071.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/WalkableGuildHall069.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/WorldCharacterAnimator070.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityVerticalSlicePresenter017D.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M1FlowPresenter.BattleExperience072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleExperienceController072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleCommandHud072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleDioramaView072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleActorRig072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleSequenceDirector072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleResultsView072.cs",
            "Assets/Resources/SecondDimension/Art/Portraits/Recruits/CANON_KIRI_AETHERHEART.jpg",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/LANTERN_ROAD_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/GATEHOUSE_BOSS_ARENA_071.png"
        };

        public static void PrepareFromCommandLine()
        {
            try
            {
                RequireExactBuildId();
                ValidateEssentialsOrThrow();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateEssentialsOrThrow();
                Debug.Log(PreparePassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                RequireExactBuildId();
                ValidateEssentialsOrThrow();
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                    throw new InvalidOperationException(
                        "First Hour Rebuild 072 requires StandaloneWindows64.");

                var outputRoot = ResolveSafeOutputRoot();
                if (Directory.Exists(outputRoot)) Directory.Delete(outputRoot, true);
                Directory.CreateDirectory(outputRoot);
                var executable = Path.Combine(outputRoot, ExecutableName);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = executable,
                    targetGroup = BuildTargetGroup.Standalone,
                    target = BuildTarget.StandaloneWindows64,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                var builtSubtarget = summary.GetSubtarget<StandaloneBuildSubtarget>();
                if (summary.result != BuildResult.Succeeded ||
                    summary.totalErrors != 0 ||
                    summary.platform != BuildTarget.StandaloneWindows64 ||
                    builtSubtarget != StandaloneBuildSubtarget.Player)
                    throw new InvalidOperationException(
                        "Rebuild 072 Windows build failed. Result=" + summary.result +
                        ", Errors=" + summary.totalErrors +
                        ", Warnings=" + summary.totalWarnings +
                        ", Platform=" + summary.platform +
                        ", Subtarget=" + builtSubtarget + ".");

                var dataRoot = Path.Combine(
                    outputRoot,
                    Path.GetFileNameWithoutExtension(ExecutableName) + "_Data");
                RequireNonEmptyFile(executable);
                RequireNonEmptyFile(Path.Combine(outputRoot, "UnityPlayer.dll"));
                RequireNonEmptyFile(Path.Combine(dataRoot, "globalgamemanagers"));

                var identityPath = Path.Combine(outputRoot, "BUILD_ID_FIRST_HOUR_REBUILD_072.txt");
                File.WriteAllText(identityPath, BuildId + Environment.NewLine);
                RequireNonEmptyFile(identityPath);

                var summaryPath = Path.Combine(outputRoot, "BUILD_SUMMARY_FIRST_HOUR_REBUILD_072.txt");
                File.WriteAllLines(summaryPath, new[]
                {
                    "SECOND DIMENSION - FIRST HOUR REBUILD 072",
                    "Build ID: " + BuildId,
                    "Result: " + summary.result,
                    "Errors: " + summary.totalErrors,
                    "Warnings: " + summary.totalWarnings,
                    "Platform: " + summary.platform,
                    "Subtarget: " + builtSubtarget,
                    "Total bytes: " + summary.totalSize,
                    "Built UTC: " + DateTime.UtcNow.ToString("o"),
                    "Scene: " + ScenePath,
                    "Executable: " + ExecutableName
                });
                RequireNonEmptyFile(summaryPath);
                Debug.Log(BuildPassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RequireExactBuildId()
        {
            var requested = ReadArgument("-sdBuildId");
            if (!StringComparer.Ordinal.Equals(requested, BuildId))
                throw new InvalidOperationException(
                    "Rebuild 072 requires -sdBuildId " + BuildId + ".");
        }

        private static string ResolveSafeOutputRoot()
        {
            var requested = ReadArgument("-sdOutput");
            if (string.IsNullOrWhiteSpace(requested))
                throw new InvalidOperationException("Rebuild 072 requires -sdOutput.");
            var outputRoot = Path.GetFullPath(requested);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var buildsRoot = Path.GetFullPath(Path.Combine(projectRoot, "Builds"));
            var prefix = buildsRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!outputRoot.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Rebuild 072 output must be a child of the project Builds directory.");
            var leaf = new DirectoryInfo(outputRoot).Name;
            if (!leaf.StartsWith(".fh072_", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Rebuild 072 output must use the launcher's unique .fh072_ directory.");
            return outputRoot;
        }

        private static string ReadArgument(string key)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < arguments.Length; index++)
                if (StringComparer.Ordinal.Equals(arguments[index], key))
                    return arguments[index + 1] ?? string.Empty;
            return string.Empty;
        }

        private static void ValidateEssentialsOrThrow()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (var relativePath in EssentialProjectFiles)
                RequireNonEmptyFile(Path.Combine(
                    projectRoot,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Boot scene is not importable: " + ScenePath);

            var bootSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets/SecondDimension/Presentation/Boot/BootCoordinator.cs"));
            if (!bootSource.Contains("AddComponent<FirstHourExperienceRoot>()") ||
                bootSource.Contains("AddComponent<M1FlowPresenter>()"))
                throw new InvalidOperationException(
                    "Boot must enter FirstHourExperienceRoot directly.");

            var rootSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour072/FirstHourExperienceRoot.cs"));
            var requiredRootContracts = new List<string>
            {
                BuildId,
                "SECOND_DIMENSION_PLAYER_READY_072_V1",
                "-sdReadyMarker",
                "START NEW GUILD"
            };
            foreach (var contract in requiredRootContracts)
                if (!rootSource.Contains(contract))
                    throw new InvalidOperationException(
                        "First Hour Rebuild root contract is missing: " + contract);
        }

        private static void RequireNonEmptyFile(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= 0)
                throw new InvalidOperationException(
                    "Required Rebuild 072 file is missing or empty: " + path);
        }
    }
}
#endif
