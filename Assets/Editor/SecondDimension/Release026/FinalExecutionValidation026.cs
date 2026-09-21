#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Editor.Release025;
using SecondDimension.Presentation.Release026;

namespace SecondDimension.Editor.Release026
{
    public static class FinalExecutionValidation026
    {
        [MenuItem("Second Dimension/Final Execution 026/Validate Complete Project")]
        public static void Validate()
        {
            FinalImplementationValidation025.Validate();
            var root = Directory.GetParent(Application.dataPath).FullName;
            var issues = new List<string>();
            var health = FinalExecutionHealthService026.BuildSnapshot();
            if (!health.IsReady) issues.Add(health.Error);

            ValidateBootScene(root, issues);
            ValidateCoordinatorSurface(issues);
            ValidatePackages(root, issues);
            ValidateScripts(root, issues);
            if (issues.Count > 0)
                throw new InvalidOperationException("Final Execution 026 validation failed:\n- " +
                                                    string.Join("\n- ", issues));

            var evidence = Path.Combine(root, "BuildEvidence", "026");
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "UNITY_STATIC_HEALTH_026.md"),
                "# Unity Static Health 026\n\n" +
                "- Generated UTC: " + DateTime.UtcNow.ToString("o") + "\n" +
                "- Inherited 025 validation: PASS\n" +
                "- Coordinator contracts: " + health.CoordinatorInterfacesImplemented + "/" +
                health.CoordinatorInterfacesExpected + "\n" +
                "- Coordinator commands: " + health.CoordinatorCommandMethods + "\n" +
                "- Save format: " + health.SaveFormatVersion + "\n" +
                "- World Gate boards/nodes: " + health.WorldGateBoards + "/" + health.WorldGateNodes + "\n");
            Debug.Log("FINAL EXECUTION READINESS 026 VALIDATION PASS");
        }

        public static void ValidateFromCommandLine()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void ValidateBootScene(string root, List<string> issues)
        {
            var boot = Path.Combine(root, "Assets", "Scenes", "Boot.unity");
            if (!File.Exists(boot)) issues.Add("Boot scene is missing.");
            var enabled = EditorBuildSettings.scenes.Where(value => value.enabled).ToArray();
            if (enabled.Length == 0 || enabled[0].path != "Assets/Scenes/Boot.unity")
                issues.Add("Boot.unity must be the first enabled build scene.");
        }

        private static void ValidateCoordinatorSurface(List<string> issues)
        {
            var coordinator = typeof(SecondDimension.Presentation.M1RuntimeCoordinator);
            foreach (var contract in FinalExecutionHealthService026.ExpectedCoordinatorInterfaces())
            {
                if (!contract.IsAssignableFrom(coordinator))
                {
                    issues.Add("Coordinator contract is unreachable: " + contract.FullName);
                    continue;
                }
                var map = coordinator.GetInterfaceMap(contract);
                if (map.TargetMethods.Any(value => value == null || value.IsAbstract))
                    issues.Add("Coordinator has an abstract/unresolved implementation for " + contract.FullName);
            }
        }

        private static void ValidatePackages(string root, List<string> issues)
        {
            var manifest = File.ReadAllText(Path.Combine(root, "Packages", "manifest.json"));
            var packageLock = File.ReadAllText(Path.Combine(root, "Packages", "packages-lock.json"));
            foreach (var package in new[]
                     {
                         "com.unity.nuget.newtonsoft-json", "com.unity.test-framework",
                         "com.unity.ugui"
                     })
            {
                if (!manifest.Contains("\"" + package + "\"")) issues.Add("Missing package: " + package);
                if (!packageLock.Contains("\"" + package + "\"")) issues.Add("Unresolved package lock: " + package);
            }
        }

        private static void ValidateScripts(string root, List<string> issues)
        {
            foreach (var file in new[]
                     {
                         "00_START_FINAL_EXECUTION_READINESS_026.cmd",
                         "01_RUN_OFFLINE_DIAGNOSTIC_026.cmd",
                         "02_CAPTURE_FIRST_UNITY_COMPILE_026.cmd",
                         "03_RUN_ALL_TESTS_026.cmd",
                         "04_BUILD_FINAL_OWNER_REVIEW_026.cmd",
                         "05_PLAY_FINAL_OWNER_REVIEW_026.cmd",
                         "06_RUN_COMPLETE_FINAL_PIPELINE_026.cmd",
                         "07_OPEN_BUILD_EVIDENCE_026.cmd",
                         "08_COPY_CODEX_FIX_QUEUE_026.cmd"
                     })
                if (!File.Exists(Path.Combine(root, file))) issues.Add("Missing 026 launcher: " + file);
        }
    }
}
#endif
