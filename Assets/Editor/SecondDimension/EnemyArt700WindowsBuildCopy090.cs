#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor
{
    /// <summary>
    /// Adds the raw EnemyArt700 runtime payload to Windows players after Unity has
    /// produced the player data directory. Existing unrelated StreamingAssets are
    /// never enumerated for deletion or otherwise modified.
    /// </summary>
    public sealed class EnemyArt700WindowsBuildCopy090 : IPostprocessBuildWithReport
    {
        private const string AssetRoot090 =
            "Assets/SecondDimension/EnemyArt700/";
        private const string CatalogRelativePath090 =
            "Data/EnemyArtCatalog_001_070.json";
        private const string BaseIndexRelativePath090 =
            "Data/BaseEnemyIndex_001_070.json";
        private const int ExpectedVariantCount090 = 700;
        private const int ExpectedTextureCount090 = 2100;
        public const int ExpectedRuntimeFileCount090 = ExpectedTextureCount090 + 2;

        public int callbackOrder => 700;

        public void OnPostprocessBuild(BuildReport report090)
        {
            if (report090 == null)
                throw new ArgumentNullException(nameof(report090));

            var target090 = report090.summary.platform;
            if (target090 != BuildTarget.StandaloneWindows64 &&
                target090 != BuildTarget.StandaloneWindows)
                return;

            var playerPath090 = Path.GetFullPath(report090.summary.outputPath);
            var playerDirectory090 = Path.GetDirectoryName(playerPath090);
            var playerName090 = Path.GetFileNameWithoutExtension(playerPath090);
            if (string.IsNullOrWhiteSpace(playerDirectory090) ||
                string.IsNullOrWhiteSpace(playerName090))
                throw new BuildFailedException(
                    "EnemyArt700 could not resolve the Windows player data directory from: " +
                    report090.summary.outputPath);

            var sourceRoot090 = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "EnemyArt700"));
            var destinationRoot090 = Path.Combine(
                playerDirectory090,
                playerName090 + "_Data",
                "StreamingAssets",
                "SecondDimension",
                "EnemyArt700");

            var copiedFileCount090 = CopyRuntimePayload090(
                sourceRoot090,
                destinationRoot090);
            Debug.Log(
                "EnemyArt700 copied " + copiedFileCount090 +
                " runtime files to " + destinationRoot090);
        }

        /// <summary>
        /// Performs the same closed-manifest validation used by the post-build copy
        /// without writing anything. Release preflight calls this before Unity starts
        /// a player build so an incomplete 700-variant payload fails immediately.
        /// </summary>
        public static int ValidateSourcePayload090(string sourceRoot090)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot090))
                throw new ArgumentException("Source root is required.", nameof(sourceRoot090));

            var fullSourceRoot090 = Path.GetFullPath(sourceRoot090);
            if (!Directory.Exists(fullSourceRoot090))
                throw new BuildFailedException(
                    "EnemyArt700 source root is missing: " + fullSourceRoot090);
            var payload090 = ExactRuntimePayload090(fullSourceRoot090);
            if (payload090.Count != ExpectedRuntimeFileCount090)
                throw new BuildFailedException(
                    "EnemyArt700 runtime payload must contain exactly " +
                    ExpectedRuntimeFileCount090 + " files, not " + payload090.Count + ".");
            return payload090.Count;
        }

        /// <summary>
        /// Copies the two known Data files and the 2,100 catalog-addressed PNGs.
        /// Files at the same destination paths are overwritten; no target file is
        /// deleted and unrelated StreamingAssets content is not enumerated.
        /// </summary>
        public static int CopyRuntimePayload090(
            string sourceRoot090,
            string destinationRoot090)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot090))
                throw new ArgumentException("Source root is required.", nameof(sourceRoot090));
            if (string.IsNullOrWhiteSpace(destinationRoot090))
                throw new ArgumentException("Destination root is required.", nameof(destinationRoot090));

            var fullSourceRoot090 = Path.GetFullPath(sourceRoot090);
            var fullDestinationRoot090 = Path.GetFullPath(destinationRoot090);
            if (!Directory.Exists(fullSourceRoot090))
                throw new BuildFailedException(
                    "EnemyArt700 source root is missing: " + fullSourceRoot090);

            var relativePaths090 = ExactRuntimePayload090(fullSourceRoot090);
            var sourcePrefix090 = fullSourceRoot090.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var destinationPrefix090 = fullDestinationRoot090.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            for (var fileIndex090 = 0;
                 fileIndex090 < relativePaths090.Count;
                 fileIndex090++)
            {
                var relativePath090 = relativePaths090[fileIndex090];
                var platformRelativePath090 = relativePath090.Replace(
                    '/',
                    Path.DirectorySeparatorChar);
                var sourceFile090 = Path.GetFullPath(Path.Combine(
                    fullSourceRoot090,
                    platformRelativePath090));
                var destinationFile090 = Path.GetFullPath(Path.Combine(
                    fullDestinationRoot090,
                    platformRelativePath090));
                if (!sourceFile090.StartsWith(
                        sourcePrefix090,
                        StringComparison.OrdinalIgnoreCase) ||
                    !destinationFile090.StartsWith(
                        destinationPrefix090,
                        StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException(
                        "EnemyArt700 payload traversal was rejected: " + relativePath090);
                if (!File.Exists(sourceFile090))
                    throw new BuildFailedException(
                        "EnemyArt700 source file is missing: " + sourceFile090);

                var destinationDirectory090 = Path.GetDirectoryName(destinationFile090);
                if (string.IsNullOrWhiteSpace(destinationDirectory090))
                    throw new BuildFailedException(
                        "EnemyArt700 destination path is invalid: " + destinationFile090);
                Directory.CreateDirectory(destinationDirectory090);
                File.Copy(sourceFile090, destinationFile090, true);
            }

            return relativePaths090.Count;
        }

        private static List<string> ExactRuntimePayload090(string sourceRoot090)
        {
            var catalogPath090 = Path.Combine(
                sourceRoot090,
                CatalogRelativePath090.Replace('/', Path.DirectorySeparatorChar));
            var baseIndexPath090 = Path.Combine(
                sourceRoot090,
                BaseIndexRelativePath090.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(catalogPath090) || !File.Exists(baseIndexPath090))
                throw new BuildFailedException(
                    "EnemyArt700 requires both catalog Data JSON files.");

            BuildCatalog090 catalog090;
            try
            {
                catalog090 = JsonUtility.FromJson<BuildCatalog090>(
                    File.ReadAllText(catalogPath090));
            }
            catch (Exception exception090)
            {
                throw new BuildFailedException(
                    "EnemyArt700 catalog JSON could not be parsed: " +
                    exception090.Message);
            }
            if (catalog090 == null ||
                catalog090.variantRecordCount != ExpectedVariantCount090 ||
                catalog090.variants == null ||
                catalog090.variants.Length != ExpectedVariantCount090)
                throw new BuildFailedException(
                    "EnemyArt700 catalog must contain exactly 700 variants.");

            var paths090 = new HashSet<string>(StringComparer.Ordinal);
            paths090.Add(CatalogRelativePath090);
            paths090.Add(BaseIndexRelativePath090);
            for (var variantIndex090 = 0;
                 variantIndex090 < catalog090.variants.Length;
                 variantIndex090++)
            {
                var variant090 = catalog090.variants[variantIndex090];
                if (variant090 == null || variant090.unityAssetPaths == null)
                    throw new BuildFailedException(
                        "EnemyArt700 catalog variant paths are missing at index " +
                        variantIndex090 + ".");
                AddTexturePath090(paths090, variant090.unityAssetPaths.idle);
                AddTexturePath090(paths090, variant090.unityAssetPaths.attack);
                AddTexturePath090(paths090, variant090.unityAssetPaths.portrait);
            }

            if (paths090.Count != ExpectedTextureCount090 + 2)
                throw new BuildFailedException(
                    "EnemyArt700 catalog must address exactly 2,100 unique PNGs.");
            var orderedPaths090 = new List<string>(paths090);
            orderedPaths090.Sort(StringComparer.Ordinal);
            var sourcePrefix090 = sourceRoot090.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            for (var pathIndex090 = 0;
                 pathIndex090 < orderedPaths090.Count;
                 pathIndex090++)
            {
                var sourceFile090 = Path.GetFullPath(Path.Combine(
                    sourceRoot090,
                    orderedPaths090[pathIndex090].Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
                if (!sourceFile090.StartsWith(
                        sourcePrefix090,
                        StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(sourceFile090))
                    throw new BuildFailedException(
                        "EnemyArt700 exact source payload is incomplete: " +
                        orderedPaths090[pathIndex090]);
            }
            return orderedPaths090;
        }

        private static void AddTexturePath090(
            HashSet<string> paths090,
            string assetPath090)
        {
            if (string.IsNullOrWhiteSpace(assetPath090))
                throw new BuildFailedException(
                    "EnemyArt700 catalog contains an empty texture path.");
            var normalizedPath090 = assetPath090.Trim().Replace('\\', '/');
            if (!normalizedPath090.StartsWith(AssetRoot090, StringComparison.Ordinal))
                throw new BuildFailedException(
                    "EnemyArt700 catalog texture is outside its asset root: " +
                    normalizedPath090);
            var relativePath090 = normalizedPath090.Substring(AssetRoot090.Length);
            if (!relativePath090.StartsWith("Textures/", StringComparison.Ordinal) ||
                !relativePath090.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException(
                    "EnemyArt700 catalog contains a non-texture payload path: " +
                    normalizedPath090);
            var segments090 = relativePath090.Split('/');
            for (var index090 = 0; index090 < segments090.Length; index090++)
                if (segments090[index090].Length == 0 ||
                    segments090[index090] == "." ||
                    segments090[index090] == "..")
                    throw new BuildFailedException(
                        "EnemyArt700 catalog texture path contains an invalid segment: " +
                        normalizedPath090);
            if (!paths090.Add(relativePath090))
                throw new BuildFailedException(
                    "EnemyArt700 catalog contains a duplicate texture path: " +
                    normalizedPath090);
        }

        [Serializable]
        private sealed class BuildCatalog090
        {
            public int variantRecordCount;
            public BuildVariant090[] variants;
        }

        [Serializable]
        private sealed class BuildVariant090
        {
            public BuildFileSet090 unityAssetPaths;
        }

        [Serializable]
        private sealed class BuildFileSet090
        {
            public string idle;
            public string attack;
            public string portrait;
        }
    }
}
#endif
