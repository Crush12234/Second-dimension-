#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecondDimension.Presentation.Battle.ArtProduction011;
using SecondDimension.Presentation.Battle.ArtProduction012;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecondDimension.Editor.ArtProduction012
{
    public static class ThursdayArtShowcaseSceneBuilder012
    {
        public const string ScenePath = "Assets/Scenes/Dev/ThursdayArtShowcase012.unity";

        [MenuItem("Second Dimension/Art 012/Create or Refresh Showcase Scene")]
        public static void CreateOrRefresh()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes/Dev");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Thursday Art Showcase 012");
            root.AddComponent<ThursdayArtShowcase012>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created Art 012 showcase scene: {ScenePath}");
        }

        public static void CreateFromCommandLine()
        {
            CreateOrRefresh();
            EditorApplication.Exit(0);
        }
    }

    public static class ThursdayArtPreflight012
    {
        private const string EvidenceDirectory = "BuildEvidence";
        private const string ReportPath = EvidenceDirectory + "/ART_012_PREFLIGHT_REPORT.txt";

        [MenuItem("Second Dimension/Art 012/Run Full Thursday Preflight")]
        public static void RunFromMenu()
        {
            RunInternal(createShowcase: true);
        }

        public static void RunFromCommandLine()
        {
            try
            {
                RunInternal(createShowcase: true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RunInternal(bool createShowcase)
        {
            BattleArtManifest011 manifest = BattleArtManifestLoader011.Load();
            List<string> failures = new List<string>();
            List<string> loadedPaths = new List<string>();

            if (manifest.contentVersion != "GOW_BATTLE_ART_011")
                failures.Add($"Unexpected content version: {manifest.contentVersion}");
            if (manifest.characters == null || manifest.characters.Length != 9)
                failures.Add("Expected exactly 9 showcase combatants.");
            if (manifest.vfx == null || manifest.vfx.Length != 28)
                failures.Add("Expected exactly 28 VFX entries.");
            if (manifest.icons == null || manifest.icons.Length != 15)
                failures.Add("Expected exactly 15 semantic icon entries.");
            if (manifest.canvas == null || manifest.canvas.width != 768 || manifest.canvas.height != 1152)
                failures.Add("Character canvas must remain 768x1152.");

            foreach (CharacterPoseSet011 character in manifest.characters ?? Array.Empty<CharacterPoseSet011>())
            {
                if (character.poses == null || character.poses.Length != 8)
                {
                    failures.Add($"{character.stableId} does not have exactly 8 poses.");
                    continue;
                }

                HashSet<string> poseIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (PoseAsset011 pose in character.poses)
                {
                    if (!poseIds.Add(pose.poseId))
                        failures.Add($"Duplicate pose ID {character.stableId}/{pose.poseId}.");
                    if (string.IsNullOrWhiteSpace(pose.productionStatus))
                        failures.Add($"Missing production status for {character.stableId}/{pose.poseId}.");
                    ValidateSprite(pose.resourcesPath, loadedPaths, failures);
                }
            }

            foreach (VfxAsset011 vfx in manifest.vfx ?? Array.Empty<VfxAsset011>())
                ValidateSprite(vfx.resourcesPath, loadedPaths, failures);
            foreach (IconAsset011 icon in manifest.icons ?? Array.Empty<IconAsset011>())
                ValidateSprite(icon.resourcesPath, loadedPaths, failures);

            string[] duplicatePaths = loadedPaths
                .GroupBy(path => path, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            foreach (string path in duplicatePaths)
                failures.Add($"Duplicate manifest Resources path: {path}");

            if (manifest.laws == null)
            {
                failures.Add("Missing battle-art laws.");
            }
            else
            {
                if (!manifest.laws.presentationOnly || manifest.laws.changesBattleResolution)
                    failures.Add("Art layer must remain presentation-only.");
                if (manifest.laws.individualArtSelectableInStandard)
                    failures.Add("Standard mode cannot expose individual Art selection.");
                if (manifest.laws.supportsAlliedUnionSlots != 10 || manifest.laws.supportsEnemyUnionSlots != 10)
                    failures.Add("Battle presentation must preserve 10 allied and 10 enemy Union slots.");
                if (manifest.laws.runtimeGenerativeAI)
                    failures.Add("Runtime generative AI must remain disabled.");
            }

            Directory.CreateDirectory(EvidenceDirectory);
            string report = BuildReport(manifest, loadedPaths.Count, failures);
            File.WriteAllText(ReportPath, report);
            AssetDatabase.Refresh();

            if (failures.Count > 0)
                throw new InvalidOperationException(report);

            if (createShowcase)
                ThursdayArtShowcaseSceneBuilder012.CreateOrRefresh();

            Debug.Log(report);
        }

        private static void ValidateSprite(
            string resourcesPath,
            ICollection<string> loadedPaths,
            ICollection<string> failures)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                failures.Add("Manifest contains an empty Resources path.");
                return;
            }

            loadedPaths.Add(resourcesPath);
            if (Resources.Load<Sprite>(resourcesPath) == null)
                failures.Add($"Missing or non-Sprite asset: {resourcesPath}");
        }

        private static string BuildReport(
            BattleArtManifest011 manifest,
            int loadedAssetCount,
            IReadOnlyCollection<string> failures)
        {
            int poseCount = manifest.characters?.Sum(character => character.poses?.Length ?? 0) ?? 0;
            int proxyCount = manifest.characters?
                .SelectMany(character => character.poses ?? Array.Empty<PoseAsset011>())
                .Count(pose => pose.productionStatus != null && pose.productionStatus.Contains("PROXY")) ?? 0;

            return string.Join(Environment.NewLine, new[]
            {
                "SECOND DIMENSION — THURSDAY ART PREFLIGHT 012",
                $"Timestamp UTC: {DateTime.UtcNow:O}",
                $"Content version: {manifest.contentVersion}",
                $"Combatants: {manifest.characters?.Length ?? 0}",
                $"Poses: {poseCount}",
                $"VFX: {manifest.vfx?.Length ?? 0}",
                $"Icons: {manifest.icons?.Length ?? 0}",
                $"Loaded Resources paths: {loadedAssetCount}",
                $"Replaceable/proxy poses: {proxyCount}",
                $"Union capacity: {manifest.laws?.supportsAlliedUnionSlots ?? 0} allied / {manifest.laws?.supportsEnemyUnionSlots ?? 0} enemy",
                $"Failures: {failures.Count}",
                failures.Count == 0 ? "RESULT: PASS" : "RESULT: FAIL",
                failures.Count == 0 ? string.Empty : string.Join(Environment.NewLine, failures.Select(failure => "- " + failure)),
            });
        }
    }
}
#endif
