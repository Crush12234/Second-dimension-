#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SecondDimension.Presentation.Battle.ArtProduction011;
using SecondDimension.Presentation.Battle.ArtProduction013;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecondDimension.Editor.ArtProduction013
{
    public static class BattleArtAutoSetup013
    {
        public const string PrefabDirectory = "Assets/Generated/SecondDimension/Art013/Prefabs";
        public const string SmokeScenePath = "Assets/Scenes/Dev/BattleArtSmoke013.unity";
        public const string EvidenceDirectory = "BuildEvidence/Thursday013";
        public const string EvidenceReportPath = EvidenceDirectory + "/ART_BINDINGS_AND_PREFABS_013.txt";
        private const string GroundShadowPath = "Assets/Resources/SecondDimension/Art/Generated013/VFX/VFX013_GROUND_SHADOW.png";

        [MenuItem("Second Dimension/Art 013/Build Prefabs + Smoke Scene")]
        public static void BuildPrefabsAndSmokeScene()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(PrefabDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(SmokeScenePath) ?? "Assets/Scenes/Dev");

            BattleArtManifest011 manifest = BattleArtManifestLoader013.Load();
            foreach (CharacterPoseSet011 character in manifest.characters)
                CreateCharacterPrefab(character);
            CreateSmokeScene(manifest.characters);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Art 013 setup created {manifest.characters.Length} prefabs and {SmokeScenePath}.");
        }

        [MenuItem("Second Dimension/Art 013/Validate 240 Art Bindings")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow(requireGeneratedPrefabs: false);
        }

        [MenuItem("Second Dimension/Art 013/Prepare + Validate Everything")]
        public static void PrepareAndValidateFromMenu()
        {
            PrepareAndValidate();
        }

        public static void PrepareAndValidateFromCommandLine()
        {
            try
            {
                PrepareAndValidate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void PrepareAndValidate()
        {
            BuildPrefabsAndSmokeScene();
            ValidateOrThrow(requireGeneratedPrefabs: true);
        }

        public static void ValidateOrThrow(bool requireGeneratedPrefabs)
        {
            BattleArtManifest011 visualManifest = BattleArtManifestLoader013.Load();
            ArtPresentationBindingManifest013 bindingManifest = ArtPresentationBindingLoader013.Load();
            List<string> failures = new List<string>();
            HashSet<string> visualIds = new HashSet<string>(StringComparer.Ordinal);

            if (visualManifest.characters == null || visualManifest.characters.Length != 9)
                failures.Add("Expected 9 clean pose-set combatants.");
            if (visualManifest.vfx == null || visualManifest.vfx.Length != 41)
                failures.Add("Expected 41 VFX entries after 13 weapon-impact/grounding additions.");
            if (visualManifest.icons == null || visualManifest.icons.Length != 15)
                failures.Add("Expected 15 semantic icons.");
            if (bindingManifest.bindings == null || bindingManifest.bindings.Length != 240 || bindingManifest.bindingCount != 240)
                failures.Add("Expected exactly 240 Art presentation bindings.");

            foreach (CharacterPoseSet011 character in visualManifest.characters ?? Array.Empty<CharacterPoseSet011>())
            {
                if (character.poses == null || character.poses.Length != 8)
                    failures.Add(character.stableId + " must have 8 poses.");
                foreach (PoseAsset011 pose in character.poses ?? Array.Empty<PoseAsset011>())
                    if (Resources.Load<Sprite>(pose.resourcesPath) == null)
                        failures.Add("Missing pose Sprite: " + pose.resourcesPath);
            }
            foreach (VfxAsset011 vfx in visualManifest.vfx ?? Array.Empty<VfxAsset011>())
            {
                if (!visualIds.Add(vfx.assetId)) failures.Add("Duplicate VFX ID: " + vfx.assetId);
                if (Resources.Load<Sprite>(vfx.resourcesPath) == null)
                    failures.Add("Missing VFX Sprite: " + vfx.resourcesPath);
            }
            foreach (IconAsset011 icon in visualManifest.icons ?? Array.Empty<IconAsset011>())
            {
                if (!visualIds.Add(icon.assetId)) failures.Add("Duplicate visual ID: " + icon.assetId);
                if (Resources.Load<Sprite>(icon.resourcesPath) == null)
                    failures.Add("Missing icon Sprite: " + icon.resourcesPath);
            }

            HashSet<string> artIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> weaponFamilies = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, int> classCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ArtPresentationBinding013 binding in bindingManifest.bindings ?? Array.Empty<ArtPresentationBinding013>())
            {
                if (!artIds.Add(binding.stableArtId)) failures.Add("Duplicate Art ID: " + binding.stableArtId);
                if (!string.IsNullOrWhiteSpace(binding.weaponFamilyId)) weaponFamilies.Add(binding.weaponFamilyId);
                classCounts[binding.artClass] = classCounts.TryGetValue(binding.artClass, out int count) ? count + 1 : 1;
                if (!visualIds.Contains(binding.primaryVfxId)) failures.Add(binding.stableArtId + " missing primary VFX " + binding.primaryVfxId);
                if (!visualIds.Contains(binding.impactVfxId)) failures.Add(binding.stableArtId + " missing impact VFX " + binding.impactVfxId);
                if (!visualIds.Contains(binding.semanticIconId)) failures.Add(binding.stableArtId + " missing semantic icon " + binding.semanticIconId);
                if (!binding.presentationOnly || binding.changesBattleResolution)
                    failures.Add(binding.stableArtId + " violates presentation-only law.");
                if (binding.memberActionClickable || binding.playerDirectlySelectableInStandard)
                    failures.Add(binding.stableArtId + " leaks individual Art selection.");
            }

            if (weaponFamilies.Count != 12) failures.Add("Expected all 12 weapon families; found " + weaponFamilies.Count + ".");
            RequireCount(classCounts, "COMBAT_ART", 144, failures);
            RequireCount(classCounts, "MYSTIC_ATTACK", 72, failures);
            RequireCount(classCounts, "RESTORATION_ART", 12, failures);
            RequireCount(classCounts, "WARDING_ART", 12, failures);

            ArtPresentationLaws013 laws = bindingManifest.laws;
            if (laws == null || !laws.presentationOnly || laws.changesBattleResolution || laws.individualArtSelectableInStandard)
                failures.Add("Art Presentation 013 laws are invalid.");
            if (laws == null || laws.supportsAlliedUnionSlots != 10 || laws.supportsEnemyUnionSlots != 10)
                failures.Add("Art Presentation 013 must preserve 10 ally and 10 enemy Union slots.");
            if (laws == null || laws.runtimeGenerativeAI || !laws.unknownRecruitUsesModularPuppet010 || !laws.unknownRecruitMayNotBorrowAnotherIdentity)
                failures.Add("Generated recruit visual fallback law is invalid.");

            if (requireGeneratedPrefabs)
            {
                string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDirectory });
                if (prefabs.Length != 9) failures.Add("Expected 9 generated Art 013 prefabs; found " + prefabs.Length + ".");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SmokeScenePath) == null)
                    failures.Add("Missing generated smoke scene: " + SmokeScenePath);
            }

            Directory.CreateDirectory(EvidenceDirectory);
            string report = BuildReport(visualManifest, bindingManifest, weaponFamilies.Count, failures);
            File.WriteAllText(EvidenceReportPath, report);
            AssetDatabase.Refresh();
            if (failures.Count > 0) throw new InvalidOperationException(report);
            Debug.Log(report);
        }

        private static void RequireCount(Dictionary<string, int> counts, string key, int expected, ICollection<string> failures)
        {
            int actual = counts.TryGetValue(key, out int value) ? value : 0;
            if (actual != expected) failures.Add($"Expected {expected} {key} bindings; found {actual}.");
        }

        private static string BuildReport(
            BattleArtManifest011 visual,
            ArtPresentationBindingManifest013 bindings,
            int weaponFamilyCount,
            IReadOnlyCollection<string> failures)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "SECOND DIMENSION — ART BINDINGS + AUTOPREFAB PREFLIGHT 013",
                "Timestamp UTC: " + DateTime.UtcNow.ToString("O"),
                "Clean pose-set combatants: " + (visual.characters?.Length ?? 0),
                "Visual assets: " + ((visual.vfx?.Length ?? 0) + (visual.icons?.Length ?? 0)),
                "Art bindings: " + (bindings.bindings?.Length ?? 0),
                "Weapon families bound: " + weaponFamilyCount,
                "Union capacity: 10 allied / 10 enemy",
                "Runtime generative AI: OFF",
                "Individual member Art selection: OFF",
                "Failures: " + failures.Count,
                failures.Count == 0 ? "RESULT: PASS" : "RESULT: FAIL",
                failures.Count == 0 ? string.Empty : string.Join(Environment.NewLine, failures.Select(value => "- " + value)),
            });
        }

        private static void CreateCharacterPrefab(CharacterPoseSet011 character)
        {
            GameObject root = new GameObject("BattleArt013_" + character.stableId);
            try
            {
                BattleArtCharacterTag013 tag = root.AddComponent<BattleArtCharacterTag013>();
                tag.Configure(character.stableId, character.displayName, character.side);

                GameObject motionRoot = new GameObject("MotionRoot");
                motionRoot.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = motionRoot.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(character.poses.First(pose => pose.poseId == "IDLE_READY").resourcesPath);
                renderer.sortingOrder = 10;
                root.AddComponent<BattlePoseAnimator011>();

                Sprite shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundShadowPath);
                if (shadowSprite != null)
                {
                    GameObject shadow = new GameObject("GroundShadow");
                    shadow.transform.SetParent(root.transform, false);
                    shadow.transform.localPosition = new Vector3(0f, -0.03f, 0f);
                    shadow.transform.localScale = new Vector3(1.1f, 0.45f, 1f);
                    SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                    shadowRenderer.sprite = shadowSprite;
                    shadowRenderer.sortingOrder = 0;
                }

                string path = PrefabDirectory + "/" + character.stableId + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Sprite LoadSprite(string resourcesPath)
        {
            string assetPath = "Assets/Resources/" + resourcesPath + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) throw new InvalidOperationException("Could not load Sprite at " + assetPath);
            return sprite;
        }

        private static void CreateSmokeScene(IReadOnlyList<CharacterPoseSet011> characters)
        {
            string previousScene = SceneManager.GetActiveScene().path;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 6.3f;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0f, -0.4f, -10f);

            GameObject controller = new GameObject("Battle Art Smoke Controller 013");
            controller.AddComponent<BattleArtSmokeController013>();

            int allyIndex = 0;
            int enemyIndex = 0;
            foreach (CharacterPoseSet011 character in characters)
            {
                string prefabPath = PrefabDirectory + "/" + character.stableId + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) throw new InvalidOperationException("Missing generated prefab " + prefabPath);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                bool enemy = StringComparer.Ordinal.Equals(character.side, "ENEMY");
                int index = enemy ? enemyIndex++ : allyIndex++;
                float x = enemy ? 2.8f + index * 2.0f : -7.0f + index * 1.8f;
                float y = index % 2 == 0 ? -2.6f : -1.75f;
                instance.transform.position = new Vector3(x, y, 0f);
                instance.transform.localScale = Vector3.one * (enemy ? 1.5f : 1.35f);
            }

            EditorSceneManager.SaveScene(scene, SmokeScenePath);
            if (!string.IsNullOrWhiteSpace(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        [MenuItem("Second Dimension/Build/Battle Art Owner Review 013")]
        public static void BuildWindowsOwnerReview013()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                                 ?? throw new InvalidOperationException("Could not resolve project root.");
            string outputDirectory = Path.Combine(projectRoot, "Builds", "Windows_Owner_Review_013");
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "SECOND_DIMENSION_GUILD_OF_WORLDS.exe");
            string boot = "Assets/Scenes/Boot.unity";
            if (!File.Exists(Path.Combine(projectRoot, boot.Replace('/', Path.DirectorySeparatorChar))))
                throw new FileNotFoundException("Missing startup scene", boot);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.22f1.");

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { boot },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            });
            BuildSummary summary = report.summary;
            File.WriteAllText(Path.Combine(outputDirectory, "BUILD_SUMMARY.txt"),
                $"Result: {summary.result}{Environment.NewLine}Output: {summary.outputPath}{Environment.NewLine}Warnings: {summary.totalWarnings}{Environment.NewLine}Errors: {summary.totalErrors}{Environment.NewLine}Size: {summary.totalSize}{Environment.NewLine}Time: {summary.totalTime}{Environment.NewLine}");
            File.WriteAllText(Path.Combine(outputDirectory, "PLAY_SECOND_DIMENSION.cmd"),
                "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"SECOND_DIMENSION_GUILD_OF_WORLDS.exe\"\r\n");
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows owner-review build failed: " + summary.result);
            WriteSha256Manifest(outputDirectory);
            Debug.Log("Windows owner-review build succeeded: " + executable);
        }

        public static void BuildWindowsOwnerReview013FromCommandLine()
        {
            try
            {
                PrepareAndValidate();
                BuildWindowsOwnerReview013();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void WriteSha256Manifest(string directory)
        {
            StringBuilder output = new StringBuilder();
            using (SHA256 sha = SHA256.Create())
            {
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal))
                {
                    if (file.EndsWith("BUILD_SHA256.txt", StringComparison.OrdinalIgnoreCase)) continue;
                    using (FileStream stream = File.OpenRead(file))
                    {
                        string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                        string relative = file.Substring(directory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                        output.Append(hash).Append("  ").Append(relative).AppendLine();
                    }
                }
            }
            File.WriteAllText(Path.Combine(directory, "BUILD_SHA256.txt"), output.ToString());
        }
    }
}
#endif
