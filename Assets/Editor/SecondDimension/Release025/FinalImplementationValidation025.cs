#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Editor.Release024;
using SecondDimension.Presentation.Release025;

namespace SecondDimension.Editor.Release025
{
    public static class FinalImplementationValidation025
    {
        [MenuItem("Second Dimension/Final Implementation 025/Validate Complete Project")]
        public static void Validate()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var issues = new List<string>();
            FullGameIntegrationValidation024.Validate();
            ValidateRuntimeHealth(issues);
            ValidateAssemblyBoundaries(root, issues);
            ValidatePackages(root, issues);
            ValidateBootAndAuthority(root, issues);
            ValidateSaveAndSingleSource(root, issues);
            ValidateCampaign022Repair(root, issues);
            ValidateDeliveryScripts(root, issues);
            if (issues.Count > 0)
                throw new InvalidOperationException(
                    "Final Implementation 025 validation failed:\n- " + string.Join("\n- ", issues));
            WriteReport(root);
            Debug.Log("FINAL IMPLEMENTATION HARDENING 025 VALIDATION PASS");
        }

        public static void ValidateFromCommandLine()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void ValidateRuntimeHealth(List<string> issues)
        {
            var registry = ImplementationHardeningRegistry025.LoadFromResources();
            var health = FinalImplementationHealthService025.BuildSnapshot();
            if (!health.IsReady) issues.Add(health.Error);
            if (registry.Manifest.saveFormatVersion != 11) issues.Add("Save format 11 is not active.");
            if (registry.CompileRiskGates.gates.Length != 16)
                issues.Add("The 16-gate compile-risk matrix is incomplete.");
        }

        private static void ValidateAssemblyBoundaries(string root, List<string> issues)
        {
            var pure = new[] { "Core", "Determinism", "Content", "Gameplay", "Save" };
            foreach (var assembly in pure)
            {
                var directory = Path.Combine(root, "Assets", "SecondDimension", assembly);
                if (!Directory.Exists(directory))
                {
                    issues.Add("Assembly source directory is missing: " + assembly);
                    continue;
                }
                foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
                {
                    var text = File.ReadAllText(file);
                    if (text.Contains("using UnityEngine") || text.Contains("UnityEngine."))
                        issues.Add("Pure assembly uses UnityEngine: " + Relative(root, file));
                    if (text.Contains("using UnityEditor") || text.Contains("UnityEditor."))
                        issues.Add("Runtime assembly uses UnityEditor: " + Relative(root, file));
                    if (assembly == "Gameplay" &&
                        (text.Contains("using SecondDimension.Presentation") ||
                         text.Contains("SecondDimension.Presentation.") ||
                         text.Contains("using SecondDimension.Save") ||
                         text.Contains("using SecondDimension.Platform")))
                        issues.Add("Gameplay references an upper-layer assembly: " + Relative(root, file));
                }
            }

            var gameplayAsmdef = JObject.Parse(File.ReadAllText(Path.Combine(root,
                "Assets/SecondDimension/Gameplay/SecondDimension.Gameplay.asmdef")));
            var references = gameplayAsmdef["references"] == null
                ? Array.Empty<string>()
                : gameplayAsmdef["references"].Values<string>().ToArray();
            foreach (var forbidden in new[]
                     {
                         "SecondDimension.Presentation", "SecondDimension.Save", "SecondDimension.Platform"
                     })
                if (references.Any(value => StringComparer.Ordinal.Equals(value, forbidden)))
                    issues.Add("Gameplay asmdef references " + forbidden + " and would create a cycle.");
        }

        private static void ValidatePackages(string root, List<string> issues)
        {
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(root, "Packages/manifest.json")));
            var packageLock = JObject.Parse(File.ReadAllText(Path.Combine(root, "Packages/packages-lock.json")));
            var dependencies = manifest["dependencies"] as JObject;
            var locked = packageLock["dependencies"] as JObject;
            foreach (var package in new[]
                     {
                         "com.unity.nuget.newtonsoft-json",
                         "com.unity.test-framework",
                         "com.unity.ugui"
                     })
            {
                var requested = dependencies?[package]?.Value<string>();
                var resolved = locked?[package]?["version"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(requested))
                    issues.Add("Required Unity package is missing: " + package);
                else if (!StringComparer.Ordinal.Equals(requested, resolved))
                    issues.Add("Package manifest/lock mismatch: " + package + " " + requested + " / " + resolved);
            }
        }

        private static void ValidateBootAndAuthority(string root, List<string> issues)
        {
            var boot = Path.Combine(root, "Assets/Scenes/Boot.unity");
            if (!File.Exists(boot)) issues.Add("Boot scene is missing.");
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (scenes.Length == 0 || !StringComparer.Ordinal.Equals(scenes[0].path, "Assets/Scenes/Boot.unity"))
                issues.Add("Boot.unity is not the first enabled build scene.");

            var authority = Path.Combine(root, "Assets/StreamingAssets/Authority");
            var manifestPath = Path.Combine(authority, "AUTHORITY_RUNTIME_MANIFEST.json");
            if (!File.Exists(manifestPath))
            {
                issues.Add("Runtime authority manifest is missing.");
                return;
            }
            var manifest = JObject.Parse(File.ReadAllText(manifestPath));
            var entries = manifest["files"] as JArray;
            if (entries == null || manifest["fileCount"]?.Value<int>() != entries.Count)
            {
                issues.Add("Runtime authority manifest count is invalid.");
                return;
            }
            foreach (var token in entries)
            {
                var relative = token["path"]?.Value<string>() ?? string.Empty;
                var expected = token["sha256"]?.Value<string>() ?? string.Empty;
                var path = Path.Combine(authority, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    issues.Add("Built-player authority file is missing: " + relative);
                    continue;
                }
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var actual = string.Concat(sha.ComputeHash(stream)
                        .Select(value => value.ToString("x2")));
                    if (!StringComparer.OrdinalIgnoreCase.Equals(expected, actual))
                        issues.Add("Built-player authority hash mismatch: " + relative);
                }
            }
        }

        private static void ValidateSaveAndSingleSource(string root, List<string> issues)
        {
            var save = File.ReadAllText(Path.Combine(root,
                "Assets/SecondDimension/Save/SaveEnvelopeV1.cs"));
            var atomic = File.ReadAllText(Path.Combine(root,
                "Assets/SecondDimension/Save/AtomicSaveStore.cs"));
            var saveTests = File.ReadAllText(Path.Combine(root,
                "Assets/Tests/EditMode/SaveRoundTripTests.cs"));
            var creatorSaveTests = File.ReadAllText(Path.Combine(root,
                "Assets/Tests/EditMode/CreatorCodesRooms028Tests.cs"));
            if (!save.Contains("CurrentFormatVersion = 11"))
                issues.Add("SaveEnvelopeV1 is not on format 11.");
            const string migration = "save_v10_to_v11_creator_codes_rooms_028_defaults";
            if (!atomic.Contains(migration) || !saveTests.Contains(migration) ||
                !creatorSaveTests.Contains("V10SaveMigratesToV11WithCreatorDefaults"))
                issues.Add("Direct v10-to-v11 migration authority or tests are missing.");

            var battleMatches = Directory.GetFiles(Path.Combine(root, "Assets/SecondDimension"),
                    "*.cs", SearchOption.AllDirectories)
                .Count(path => File.ReadAllText(path).Contains("class M2BattleCommandService"));
            if (battleMatches != 1)
                issues.Add("Exactly one M2BattleCommandService is required; found " + battleMatches + ".");
        }

        private static void ValidateCampaign022Repair(string root, List<string> issues)
        {
            var contracts = Path.Combine(root,
                "Assets/SecondDimension/Gameplay/Campaign022/Campaign022ContentContracts.cs");
            var service = Path.Combine(root,
                "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionCommandService022.cs");
            var registry = Path.Combine(root,
                "Assets/SecondDimension/Presentation/Campaign022/Campaign022Registry.cs");
            if (!File.Exists(contracts) || !File.ReadAllText(contracts).Contains("interface ICampaignRegistry022"))
                issues.Add("Campaign 022 pure content contract is missing.");
            if (!File.ReadAllText(service).Contains("ICampaignRegistry022 registry") ||
                File.ReadAllText(service).Contains("SecondDimension.Presentation.Campaign022"))
                issues.Add("Campaign 022 command service is not assembly-safe.");
            if (!File.ReadAllText(registry).Contains("CampaignRegistry022 : ICampaignRegistry022"))
                issues.Add("Campaign 022 resource loader does not implement the pure contract.");
            if (!StringComparer.Ordinal.Equals(
                    typeof(SecondDimension.Gameplay.Campaign022.ICampaignRegistry022).Assembly.GetName().Name,
                    "SecondDimension.Gameplay") ||
                !StringComparer.Ordinal.Equals(
                    typeof(SecondDimension.Presentation.Campaign022.CampaignRegistry022).Assembly.GetName().Name,
                    "SecondDimension.Presentation"))
                issues.Add("Campaign 022 contract or loader compiled into the wrong assembly.");
        }

        private static void ValidateDeliveryScripts(string root, List<string> issues)
        {
            foreach (var name in new[]
                     {
                         "00_START_FINAL_IMPLEMENTATION_HARDENING_025.cmd",
                         "01_RUN_STATIC_COMPILE_GATE_025.cmd",
                         "02_RUN_ALL_TESTS_025.cmd",
                         "03_BUILD_FINAL_OWNER_REVIEW_025.cmd",
                         "04_PLAY_FINAL_OWNER_REVIEW_025.cmd",
                         "05_RUN_COMPLETE_FINAL_PIPELINE_025.cmd",
                         "06_OPEN_BUILD_EVIDENCE_025.cmd"
                     })
                if (!File.Exists(Path.Combine(root, name)))
                    issues.Add("Delivery launcher is missing: " + name);
        }

        private static void WriteReport(string root)
        {
            var evidence = Path.Combine(root, "BuildEvidence", "025");
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "STATIC_IMPLEMENTATION_HEALTH_025.md"),
                "# Static Implementation Health 025\n\n" +
                "- Generated UTC: " + DateTime.UtcNow.ToString("o") + "\n" +
                "- Inherited Integration 024 health: PASS\n" +
                "- Assembly graph and pure boundaries: PASS\n" +
                "- Campaign 022 Gameplay → Presentation cycle: REMOVED\n" +
                "- Package manifest/lock: PASS\n" +
                "- Boot scene first: PASS\n" +
                "- Runtime authority hashes: PASS\n" +
                "- Save v10 and migration authority: PASS\n" +
                "- 16 hardening gates: PASS\n");
        }

        private static string Relative(string root, string path) =>
            path.Substring(root.Length + 1).Replace('\\', '/');
    }
}
#endif
