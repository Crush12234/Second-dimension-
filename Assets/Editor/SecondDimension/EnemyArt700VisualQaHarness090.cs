#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecondDimension.Editor
{
    [Serializable]
    public sealed class EnemyArt700VisualQaReport090
    {
        public string generatedUtc;
        public string result;
        public string schemaId;
        public string evidenceDirectory;
        public string layout;
        public int catalogVariantCount;
        public int expectedSpriteCount;
        public int attemptedSpriteCount;
        public int loadedSpriteCount;
        public int drawnSpriteCount;
        public int contactSheetCount;
        public int cacheCapacity;
        public int maximumObservedCachedSprites;
        public long pngReadCount;
        public long cacheHitCount;
        public long evictionCount;
        public EnemyArt700ContactSheetReport090[] contactSheets;
        public EnemyArt700PairCheckReport090[] pairChecks;
        public string[] failures;
    }

    [Serializable]
    public sealed class EnemyArt700ContactSheetReport090
    {
        public string fileName;
        public int firstOrdinal;
        public int lastOrdinal;
        public int attemptedSpriteCount;
        public int drawnSpriteCount;
        public string[] variantIds;
    }

    [Serializable]
    public sealed class EnemyArt700PairCheckReport090
    {
        public string baseEnemyId;
        public string variantId;
        public bool passed;
        public bool idleSpriteReused;
        public long pngReadsBeforeReturnToIdle;
        public long pngReadsAfterReturnToIdle;
        public string error;
    }

    /// <summary>
    /// Development-only visual QA for the raw EnemyArt700 package. It exercises the
    /// real runtime loader and writes evidence outside Assets; it does not create a
    /// scene, prefab, Resources entry, or player-facing gallery.
    /// </summary>
    public static class EnemyArt700VisualQaHarness090
    {
        public const string EvidenceRelativeDirectory090 =
            "BuildEvidence/EnemyArt700";
        public const string ReportFileName090 =
            "ENEMY_ART_700_VISUAL_QA_090.json";
        public const int VariantsPerSheet090 = 20;
        public const int VariantGroupsPerRow090 = 5;
        public const int PoseCount090 = 3;
        public const int TileWidth090 = 160;
        public const int TileHeight090 = 216;

        private static readonly EnemyArt700Pose090[] Poses090 =
        {
            EnemyArt700Pose090.Idle,
            EnemyArt700Pose090.Attack,
            EnemyArt700Pose090.Portrait
        };

        private static int _pendingCommandLineExitCode090;
        private static int _remainingCommandLineCleanupUpdates090;

        [MenuItem(
            "Second Dimension/Enemy Art 700/Generate Complete Visual QA Evidence 090",
            false,
            5190)]
        public static void RunFromMenu090()
        {
            var reportPath090 = GenerateEvidence090();
            Debug.Log("EnemyArt700 visual QA PASS: " + reportPath090);
            EditorUtility.RevealInFinder(reportPath090);
        }

        public static void RunFromCommandLine090()
        {
            var exitCode090 = 0;
            try
            {
                GenerateEvidence090();
            }
            catch (Exception exception090)
            {
                Debug.LogException(exception090);
                exitCode090 = 1;
            }

            ScheduleCommandLineExitAfterCleanup090(exitCode090);
        }

        private static void ScheduleCommandLineExitAfterCleanup090(int exitCode090)
        {
            _pendingCommandLineExitCode090 = exitCode090;
            _remainingCommandLineCleanupUpdates090 = 2;
            EditorApplication.update -= CompleteCommandLineExit090;
            EditorApplication.update += CompleteCommandLineExit090;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void CompleteCommandLineExit090()
        {
            _remainingCommandLineCleanupUpdates090--;
            if (_remainingCommandLineCleanupUpdates090 > 0)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            EditorApplication.update -= CompleteCommandLineExit090;
            EditorUtility.ClearProgressBar();
            EnemyArt700Runtime090.ResetForTests090();
            EditorApplication.Exit(_pendingCommandLineExitCode090);
        }

        public static string GenerateEvidence090()
        {
            var projectRoot090 = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot090))
                throw new InvalidOperationException(
                    "EnemyArt700 QA could not resolve the Unity project root.");
            var evidenceDirectory090 = Path.GetFullPath(Path.Combine(
                projectRoot090,
                "BuildEvidence",
                "EnemyArt700"));
            var assetsPrefix090 = Path.GetFullPath(Application.dataPath).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (evidenceDirectory090.StartsWith(
                    assetsPrefix090,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "EnemyArt700 QA evidence must remain outside Assets.");
            Directory.CreateDirectory(evidenceDirectory090);
            var reportPath090 = Path.Combine(
                evidenceDirectory090,
                ReportFileName090);

            var report090 = new EnemyArt700VisualQaReport090
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                result = "FAIL",
                evidenceDirectory = evidenceDirectory090,
                layout =
                    "Rows are top-to-bottom. Each row contains five variants; " +
                    "each variant is IDLE, ATTACK, PORTRAIT from left-to-right.",
                expectedSpriteCount = EnemyArt700Runtime090.ExpectedCatalogCount090 *
                                      PoseCount090,
                cacheCapacity = EnemyArt700Runtime090.DefaultCacheCapacity090
            };
            var failures090 = new List<string>();
            var sheets090 = new List<EnemyArt700ContactSheetReport090>();
            var pairs090 = new List<EnemyArt700PairCheckReport090>();
            var maximumCachedSprites090 = 0;
            Exception fatalException090 = null;

            try
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                    throw new InvalidOperationException(
                        "EnemyArt700 contact sheets require an Editor graphics device.");

                EnemyArt700Runtime090.ResetForTests090();
                var catalogIssues090 = EnemyArt700Runtime090.ValidateCatalog090(true);
                for (var issueIndex090 = 0;
                     issueIndex090 < catalogIssues090.Count;
                     issueIndex090++)
                    failures090.Add(catalogIssues090[issueIndex090]);
                if (catalogIssues090.Count > 0)
                    throw new InvalidDataException(
                        "EnemyArt700 catalog/file validation failed before rendering.");

                var catalog090 = EnemyArt700Runtime090.Catalog090;
                report090.schemaId = catalog090.schemaId;
                report090.catalogVariantCount = catalog090.variants.Length;
                var variants090 = (EnemyArt700Variant090[])catalog090.variants.Clone();
                Array.Sort(variants090, CompareVariants090);

                for (var start090 = 0;
                     start090 < variants090.Length;
                     start090 += VariantsPerSheet090)
                {
                    var count090 = Math.Min(
                        VariantsPerSheet090,
                        variants090.Length - start090);
                    sheets090.Add(RenderSheet090(
                        variants090,
                        start090,
                        count090,
                        evidenceDirectory090,
                        report090,
                        failures090,
                        ref maximumCachedSprites090));
                }

                EnemyArt700Runtime090.ClearCache090();
                RunPairChecks090(pairs090, failures090, ref maximumCachedSprites090);
                var diagnostics090 = EnemyArt700Runtime090.GetDiagnostics090();
                report090.maximumObservedCachedSprites = maximumCachedSprites090;
                report090.pngReadCount = diagnostics090.PngReadCount;
                report090.cacheHitCount = diagnostics090.CacheHitCount;
                report090.evictionCount = diagnostics090.EvictionCount;
                report090.contactSheetCount = sheets090.Count;

                if (report090.catalogVariantCount !=
                    EnemyArt700Runtime090.ExpectedCatalogCount090)
                    failures090.Add("Catalog did not expose exactly 700 variants.");
                if (report090.attemptedSpriteCount != report090.expectedSpriteCount)
                    failures090.Add("QA did not attempt every variant pose.");
                if (report090.loadedSpriteCount != report090.expectedSpriteCount)
                    failures090.Add("QA did not load every variant pose.");
                if (report090.drawnSpriteCount != report090.expectedSpriteCount)
                    failures090.Add("QA did not draw every variant pose.");
                if (sheets090.Count != 35)
                    failures090.Add("Expected exactly 35 deterministic contact sheets.");
                if (maximumCachedSprites090 >
                    EnemyArt700Runtime090.MaximumCacheCapacity090)
                    failures090.Add("Runtime cache exceeded its 96-sprite maximum.");
                for (var pairIndex090 = 0;
                     pairIndex090 < pairs090.Count;
                     pairIndex090++)
                    if (!pairs090[pairIndex090].passed)
                        failures090.Add(
                            "Pose-pair check failed: " +
                            pairs090[pairIndex090].variantId);

                report090.result = failures090.Count == 0 ? "PASS" : "FAIL";
            }
            catch (Exception exception090)
            {
                fatalException090 = exception090;
                failures090.Add(
                    "FATAL: " + exception090.GetType().Name + ": " +
                    exception090.Message);
                report090.result = "FAIL";
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                report090.generatedUtc = DateTime.UtcNow.ToString("O");
                report090.maximumObservedCachedSprites = maximumCachedSprites090;
                report090.contactSheetCount = sheets090.Count;
                report090.contactSheets = sheets090.ToArray();
                report090.pairChecks = pairs090.ToArray();
                report090.failures = failures090.ToArray();
                File.WriteAllText(
                    reportPath090,
                    JsonUtility.ToJson(report090, true));
                EnemyArt700Runtime090.ResetForTests090();
            }

            if (fatalException090 != null || report090.result != "PASS")
                throw new InvalidOperationException(
                    "EnemyArt700 visual QA failed. Evidence: " + reportPath090,
                    fatalException090);
            return reportPath090;
        }

        private static EnemyArt700ContactSheetReport090 RenderSheet090(
            EnemyArt700Variant090[] variants090,
            int start090,
            int count090,
            string evidenceDirectory090,
            EnemyArt700VisualQaReport090 report090,
            List<string> failures090,
            ref int maximumCachedSprites090)
        {
            var rows090 = (count090 + VariantGroupsPerRow090 - 1) /
                          VariantGroupsPerRow090;
            var sheetWidth090 = VariantGroupsPerRow090 * PoseCount090 * TileWidth090;
            var sheetHeight090 = rows090 * TileHeight090;
            var firstOrdinal090 = start090 + 1;
            var lastOrdinal090 = start090 + count090;
            var fileName090 = string.Format(
                "CONTACT_SHEET_{0:00}_{1:000}_{2:000}.png",
                start090 / VariantsPerSheet090 + 1,
                firstOrdinal090,
                lastOrdinal090);
            var batch090 = new EnemyArt700ContactSheetReport090
            {
                fileName = fileName090,
                firstOrdinal = firstOrdinal090,
                lastOrdinal = lastOrdinal090,
                attemptedSpriteCount = count090 * PoseCount090,
                variantIds = new string[count090]
            };

            var sheet090 = new RenderTexture(
                sheetWidth090,
                sheetHeight090,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = "EnemyArt700_ContactSheet090",
                hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Texture2D capture090 = null;
            var previousTarget090 = RenderTexture.active;
            var matrixPushed090 = false;
            try
            {
                if (!sheet090.Create())
                    throw new InvalidOperationException(
                        "EnemyArt700 could not create a contact-sheet render texture.");
                RenderTexture.active = sheet090;
                GL.Clear(true, true, new Color(0.015f, 0.02f, 0.03f, 1f));
                GL.PushMatrix();
                matrixPushed090 = true;
                GL.LoadPixelMatrix(0f, sheetWidth090, sheetHeight090, 0f);

                for (var localIndex090 = 0;
                     localIndex090 < count090;
                     localIndex090++)
                {
                    var variant090 = variants090[start090 + localIndex090];
                    batch090.variantIds[localIndex090] = variant090.variantId;
                    EditorUtility.DisplayProgressBar(
                        "EnemyArt700 Visual QA 090",
                        "Drawing " + variant090.variantId,
                        (start090 + localIndex090 + 1f) / variants090.Length);
                    var groupColumn090 = localIndex090 % VariantGroupsPerRow090;
                    var groupRow090 = localIndex090 / VariantGroupsPerRow090;

                    for (var poseIndex090 = 0;
                         poseIndex090 < Poses090.Length;
                         poseIndex090++)
                    {
                        var pose090 = Poses090[poseIndex090];
                        report090.attemptedSpriteCount++;
                        var cell090 = new Rect(
                            (groupColumn090 * PoseCount090 + poseIndex090) *
                            TileWidth090,
                            groupRow090 * TileHeight090,
                            TileWidth090,
                            TileHeight090);
                        DrawSolidRect090(cell090, PoseBackground090(pose090));
                        if (!EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098(
                                variant090.baseEnemyId,
                                variant090.variantId,
                                pose090,
                                out var sprite090,
                                out var error090))
                        {
                            failures090.Add(
                                variant090.variantId + "/" + pose090 + ": " +
                                error090);
                            DrawFailureMarker090(cell090);
                            UpdateCacheMaximum090(
                                failures090,
                                ref maximumCachedSprites090);
                            continue;
                        }

                        report090.loadedSpriteCount++;
                        DrawSprite090(sprite090, cell090);
                        batch090.drawnSpriteCount++;
                        report090.drawnSpriteCount++;
                        UpdateCacheMaximum090(
                            failures090,
                            ref maximumCachedSprites090);
                    }
                }

                GL.PopMatrix();
                matrixPushed090 = false;
                capture090 = new Texture2D(
                    sheetWidth090,
                    sheetHeight090,
                    TextureFormat.RGB24,
                    false,
                    false)
                {
                    name = "EnemyArt700_ContactSheetCapture090",
                    hideFlags = HideFlags.HideAndDontSave
                };
                capture090.ReadPixels(
                    new Rect(0f, 0f, sheetWidth090, sheetHeight090),
                    0,
                    0,
                    false);
                capture090.Apply(false, false);
                var pngBytes090 = ImageConversion.EncodeToPNG(capture090);
                if (pngBytes090 == null || pngBytes090.Length == 0)
                    throw new InvalidOperationException(
                        "EnemyArt700 contact-sheet PNG encoding returned no bytes.");
                File.WriteAllBytes(
                    Path.Combine(evidenceDirectory090, fileName090),
                    pngBytes090);
                return batch090;
            }
            finally
            {
                if (matrixPushed090) GL.PopMatrix();
                RenderTexture.active = previousTarget090;
                if (capture090 != null) UnityEngine.Object.DestroyImmediate(capture090);
                if (sheet090 != null)
                {
                    sheet090.Release();
                    UnityEngine.Object.DestroyImmediate(sheet090);
                }
            }
        }

        private static void DrawSprite090(Sprite sprite090, Rect cell090)
        {
            if (sprite090 == null || sprite090.texture == null)
                throw new InvalidOperationException(
                    "EnemyArt700 loader returned a Sprite without a texture.");
            var texture090 = sprite090.texture;
            var textureRect090 = sprite090.textureRect;
            var available090 = new Rect(
                cell090.x + 7f,
                cell090.y + 7f,
                cell090.width - 14f,
                cell090.height - 14f);
            var scale090 = Mathf.Min(
                available090.width / textureRect090.width,
                available090.height / textureRect090.height);
            var width090 = textureRect090.width * scale090;
            var height090 = textureRect090.height * scale090;
            var destination090 = new Rect(
                available090.x + (available090.width - width090) * 0.5f,
                available090.y + (available090.height - height090) * 0.5f,
                width090,
                height090);
            var source090 = new Rect(
                textureRect090.x / texture090.width,
                textureRect090.y / texture090.height,
                textureRect090.width / texture090.width,
                textureRect090.height / texture090.height);
            Graphics.DrawTexture(
                destination090,
                texture090,
                source090,
                0,
                0,
                0,
                0,
                Color.white,
                null);
        }

        private static void DrawSolidRect090(Rect rect090, Color color090)
        {
            Graphics.DrawTexture(
                rect090,
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                0,
                0,
                0,
                0,
                color090,
                null);
        }

        private static void DrawFailureMarker090(Rect cell090)
        {
            DrawSolidRect090(
                new Rect(
                    cell090.x + 6f,
                    cell090.y + 6f,
                    cell090.width - 12f,
                    cell090.height - 12f),
                new Color(0.75f, 0.02f, 0.22f, 1f));
        }

        private static Color PoseBackground090(EnemyArt700Pose090 pose090)
        {
            switch (pose090)
            {
                case EnemyArt700Pose090.Idle:
                    return new Color(0.035f, 0.08f, 0.15f, 1f);
                case EnemyArt700Pose090.Attack:
                    return new Color(0.16f, 0.045f, 0.035f, 1f);
                default:
                    return new Color(0.14f, 0.105f, 0.025f, 1f);
            }
        }

        private static void RunPairChecks090(
            ICollection<EnemyArt700PairCheckReport090> pairs090,
            ICollection<string> failures090,
            ref int maximumCachedSprites090)
        {
            var identities090 = new[]
            {
                new[] { "ENEMY_REC_001", "ENEMY_REC_001_VAR_01" },
                new[] { "ENEMY_REC_035", "ENEMY_REC_035_VAR_05" },
                new[] { "ENEMY_REC_070", "ENEMY_REC_070_VAR_10" }
            };
            for (var identityIndex090 = 0;
                 identityIndex090 < identities090.Length;
                 identityIndex090++)
            {
                var baseId090 = identities090[identityIndex090][0];
                var variantId090 = identities090[identityIndex090][1];
                var pair090 = new EnemyArt700PairCheckReport090
                {
                    baseEnemyId = baseId090,
                    variantId = variantId090,
                    error = string.Empty
                };
                if (!EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098(
                        baseId090,
                        variantId090,
                        EnemyArt700Pose090.Idle,
                        out var firstIdle090,
                        out var error090) ||
                    !EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098(
                        baseId090,
                        variantId090,
                        EnemyArt700Pose090.Attack,
                        out var attack090,
                        out error090))
                {
                    pair090.error = error090;
                    pairs090.Add(pair090);
                    failures090.Add(variantId090 + " pair setup: " + error090);
                    continue;
                }

                pair090.pngReadsBeforeReturnToIdle =
                    EnemyArt700Runtime090.GetDiagnostics090().PngReadCount;
                if (!EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098(
                        baseId090,
                        variantId090,
                        EnemyArt700Pose090.Idle,
                        out var returnedIdle090,
                        out error090))
                {
                    pair090.error = error090;
                    pairs090.Add(pair090);
                    failures090.Add(variantId090 + " return to idle: " + error090);
                    continue;
                }

                pair090.pngReadsAfterReturnToIdle =
                    EnemyArt700Runtime090.GetDiagnostics090().PngReadCount;
                pair090.idleSpriteReused = ReferenceEquals(
                    firstIdle090,
                    returnedIdle090);
                pair090.passed = pair090.idleSpriteReused &&
                                 attack090 != null &&
                                 !ReferenceEquals(firstIdle090, attack090) &&
                                 pair090.pngReadsAfterReturnToIdle ==
                                 pair090.pngReadsBeforeReturnToIdle;
                if (!pair090.passed)
                    pair090.error =
                        "Idle was not reused or returning to idle reread the PNG.";
                pairs090.Add(pair090);
                UpdateCacheMaximum090(failures090, ref maximumCachedSprites090);
            }
        }

        private static void UpdateCacheMaximum090(
            ICollection<string> failures090,
            ref int maximumCachedSprites090)
        {
            var cached090 = EnemyArt700Runtime090.GetDiagnostics090().CachedSpriteCount;
            maximumCachedSprites090 = Math.Max(maximumCachedSprites090, cached090);
            if (cached090 > EnemyArt700Runtime090.MaximumCacheCapacity090)
                failures090.Add(
                    "Runtime cache exceeded 96 sprites: " + cached090);
        }

        private static int CompareVariants090(
            EnemyArt700Variant090 left090,
            EnemyArt700Variant090 right090)
        {
            var baseComparison090 = StringComparer.Ordinal.Compare(
                left090.baseEnemyId,
                right090.baseEnemyId);
            if (baseComparison090 != 0) return baseComparison090;
            var indexComparison090 = left090.variantIndex.CompareTo(
                right090.variantIndex);
            return indexComparison090 != 0
                ? indexComparison090
                : StringComparer.Ordinal.Compare(
                    left090.variantId,
                    right090.variantId);
        }
    }
}
#endif
