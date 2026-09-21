#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using SecondDimension.Presentation.Release030;
using SecondDimension.Save;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor.Release030
{
    public static class FinalSupersessionValidation030
    {
        [MenuItem("Second Dimension/Release 030/Validate Supersession and Pipeline")]
        public static void ValidateMenu() => ValidateOrThrow();

        public static void ValidateFromCommandLine()
        {
            try
            {
                ValidateOrThrow();
                Debug.Log("FINAL SUPERSESSION 030 VALIDATION PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateOrThrow()
        {
            var snapshot = ReleaseSupersessionReadinessService030.BuildSnapshot();
            if (!snapshot.IsReady)
                throw new InvalidOperationException(snapshot.Error);
            if (SaveEnvelopeV1.CurrentFormatVersion != 11)
                throw new InvalidOperationException("Release 030 requires save v11.");

            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifest = JObject.Parse(
                File.ReadAllText(Path.Combine(project, "Packages", "manifest.json")));
            var dependencies = (JObject)manifest["dependencies"];
            foreach (var required in ReleaseSupersessionRegistry030.Contract.requiredPackages)
                if (dependencies[required] == null)
                    throw new InvalidOperationException("Missing package: " + required);
            foreach (var forbidden in ReleaseSupersessionRegistry030.Contract.forbiddenPackages)
                if (dependencies[forbidden] != null)
                    throw new InvalidOperationException("Removed package returned: " + forbidden);

            var legacy = Path.Combine(project, "05_RUN_COMPLETE_FINAL_PIPELINE_025.ps1");
            if (!File.Exists(legacy) ||
                !File.ReadAllText(legacy)
                    .Contains("05_RUN_COMPLETE_FINAL_SUPERSESSION_PIPELINE_030.ps1"))
                throw new InvalidOperationException(
                    "Legacy pipeline does not route to Release 030.");
        }
    }

    public static class FinalSupersessionSetup030
    {
        public static void PrepareFromCommandLine()
        {
            var consoleFailures = new List<string>();
            Application.LogCallback capture = (condition, stackTrace, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    consoleFailures.Add(condition ?? type.ToString());
            };
            Application.logMessageReceived += capture;
            try
            {
                FinalSupersessionValidation030.ValidateOrThrow();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (consoleFailures.Count > 0)
                    throw new InvalidOperationException(
                        "Release 030 prepare observed a Unity Console error: " +
                        consoleFailures[0]);
                Debug.Log("FINAL SUPERSESSION 030 PREPARE PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }
    }

    public static class FinalSupersessionWindowsBuild030
    {
        public static void BuildFromCommandLine()
        {
            var consoleFailures = new List<string>();
            Application.LogCallback capture = (condition, stackTrace, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    consoleFailures.Add(condition ?? type.ToString());
            };
            Application.logMessageReceived += capture;
            try
            {
                FinalSupersessionValidation030.ValidateOrThrow();
                SecondDimension.Editor.Release029.FinalPeopleCreatorWindowsBuild029.Build();
                if (consoleFailures.Count > 0)
                    throw new InvalidOperationException(
                        "Release 030 build observed a Unity Console error: " +
                        consoleFailures[0]);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }
    }

    /// <summary>
    /// Player-facing build gate for the Release 071 first-hour gold slice.
    ///
    /// This intentionally does not delegate to the frozen Release 029/030
    /// evidence pipeline.  That pipeline recursively hashes the finished
    /// StreamingAssets tree and can turn a successful Windows build into a
    /// false failure when the project lives under a long Downloads path.
    /// The gold-slice gate proves the assets needed to boot and play, builds
    /// the one enabled runtime scene, and trusts Unity's BuildReport as the
    /// build authority.
    /// </summary>
    public static class FirstHourGoldSliceBuild071
    {
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string BuildFolder = "Builds/First_Hour_Gold_Slice_071";
        private const string ExecutableName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        private const string PreparePassMarker = "FIRST HOUR GOLD SLICE 071 PREPARE PASS";
        private const string BuildPassMarker = "FIRST HOUR GOLD SLICE 071 BUILD PASS";

        private static readonly string[] EssentialProjectFiles =
        {
            ScenePath,
            "Assets/SecondDimension/Presentation/Boot/BootCoordinator.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.Version69.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/WalkableGuildHall069.cs",
            "Assets/SecondDimension/Presentation/Battle/M2CinematicBattlePresenter.cs",
            "Assets/SecondDimension/Gameplay/FirstHour071/FirstHourDirector071.cs",
            "Assets/SecondDimension/Gameplay/FirstHour071/FirstHourRosterService071.cs",
            "Assets/Resources/SecondDimension/FirstHour071/FIRST_HOUR_ART_RECIPES_120.json",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/LANTERN_ROAD_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/GATEHOUSE_BOSS_ARENA_071.png"
        };

        public static void PrepareFromCommandLine()
        {
            try
            {
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
                ValidateEssentialsOrThrow();
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                    throw new InvalidOperationException(
                        "First-hour build requires -buildTarget StandaloneWindows64.");

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var outputRoot = Path.Combine(projectRoot, BuildFolder);
                Directory.CreateDirectory(outputRoot);
                var executable = Path.Combine(outputRoot, ExecutableName);

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = executable,
                    targetGroup = BuildTargetGroup.Standalone,
                    target = BuildTarget.StandaloneWindows64,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.None
                };
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                var builtSubtarget = summary.GetSubtarget<StandaloneBuildSubtarget>();
                if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded ||
                    summary.totalErrors != 0 ||
                    summary.platform != BuildTarget.StandaloneWindows64 ||
                    builtSubtarget != StandaloneBuildSubtarget.Player)
                {
                    throw new InvalidOperationException(
                        "First-hour Windows build failed. Result=" + summary.result +
                        ", Errors=" + summary.totalErrors +
                        ", Warnings=" + summary.totalWarnings +
                        ", Platform=" + summary.platform +
                        ", Subtarget=" + builtSubtarget + ".");
                }

                var dataRoot = Path.Combine(
                    outputRoot,
                    Path.GetFileNameWithoutExtension(ExecutableName) + "_Data");
                RequireNonEmptyFile(executable);
                RequireNonEmptyFile(Path.Combine(outputRoot, "UnityPlayer.dll"));
                RequireNonEmptyFile(Path.Combine(dataRoot, "globalgamemanagers"));

                var summaryPath = Path.Combine(outputRoot, "BUILD_SUMMARY_FIRST_HOUR_GOLD_SLICE_071.txt");
                File.WriteAllLines(summaryPath, new[]
                {
                    "SECOND DIMENSION - FIRST HOUR GOLD SLICE 071",
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
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateEssentialsOrThrow()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (var relativePath in EssentialProjectFiles)
            {
                var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                RequireNonEmptyFile(fullPath);
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
                throw new InvalidOperationException("Boot scene is not importable: " + ScenePath);
        }

        private static void RequireNonEmptyFile(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= 0)
                throw new InvalidOperationException("Required first-hour file is missing or empty: " + path);
        }
    }
}
#endif
