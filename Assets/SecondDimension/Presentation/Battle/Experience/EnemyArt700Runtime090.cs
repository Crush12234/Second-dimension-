using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public enum EnemyArt700Pose090
    {
        Idle = 0,
        Attack = 1,
        Portrait = 2
    }

    public enum EnemyArt700ScaleClass090
    {
        Small = 0,
        Default = 1,
        Large = 2
    }

    [Serializable]
    public sealed class EnemyArt700FileSet090
    {
        public string idle;
        public string attack;
        public string portrait;
    }

    [Serializable]
    public sealed class EnemyArt700Variant090
    {
        public string variantId;
        public string baseEnemyId;
        public string baseDisplayName;
        public int variantIndex;
        public string theme;
        public string artMethod;
        public bool isOriginalVersion;
        public bool newlyRedrawnAnatomy;
        public EnemyArt700FileSet090 files;
        public EnemyArt700FileSet090 unityAssetPaths;
        public int[] suggestedLayoutAnchorPixelsBottomLeft;
        public bool layoutAnchorIsSkeletalPivot;
        public bool runtimeStatRulesProvided;
        public string recoveryProvenance;
    }

    [Serializable]
    public sealed class EnemyArt700Catalog090
    {
        public string schemaId;
        public bool assetOnly;
        public int baseFamilyCount;
        public int variantRecordCount;
        public int versionsPerBaseIncludingOriginal;
        public int battleSpriteCount;
        public int portraitCount;
        public EnemyArt700Variant090[] variants;
        public bool recoveryPreservesUploadedPixels;
    }

    public sealed class EnemyArt700VariantSprites090
    {
        internal EnemyArt700VariantSprites090(
            EnemyArt700Variant090 variant090,
            Sprite idle090,
            Sprite attack090,
            Sprite portrait090)
        {
            Variant = variant090;
            Idle = idle090;
            Attack = attack090;
            Portrait = portrait090;
        }

        public EnemyArt700Variant090 Variant { get; }
        public Sprite Idle { get; }
        public Sprite Attack { get; }
        public Sprite Portrait { get; }
    }

    /// <summary>
    /// Keeps one runtime-loaded EnemyArt700 sprite alive while a presentation owner
    /// is displaying it. Dispose the lease when that owner stops retaining the
    /// sprite. The lease is idempotent and never owns fallback/Resources artwork.
    /// </summary>
    public sealed class EnemyArt700SpriteLease090 : IDisposable
    {
        private Action _release090;

        internal EnemyArt700SpriteLease090(
            Sprite sprite090,
            string cacheKey090,
            Action release090)
        {
            Sprite = sprite090;
            CacheKey090 = cacheKey090 ?? string.Empty;
            _release090 = release090;
        }

        public Sprite Sprite { get; }
        public string CacheKey090 { get; }
        public bool IsReleased090 => _release090 == null;

        public void Dispose()
        {
            var release090 = _release090;
            if (release090 == null) return;
            _release090 = null;
            release090();
        }
    }

    public sealed class EnemyArt700RuntimeDiagnostics090
    {
        internal EnemyArt700RuntimeDiagnostics090(
            bool catalogLoaded090,
            int catalogCount090,
            int cacheCapacity090,
            string[] cachedKeys090,
            long catalogReadCount090,
            long pngReadCount090,
            long cacheHitCount090,
            long evictionCount090,
            int residentSpriteCount090,
            int pinnedSpriteCount090,
            int outstandingLeaseCount090,
            int retiredSpriteCount090,
            string currentVariantId090,
            string rootDirectory090,
            string lastError090)
        {
            CatalogLoaded = catalogLoaded090;
            CatalogCount = catalogCount090;
            CacheCapacity = cacheCapacity090;
            CachedKeys = cachedKeys090 ?? Array.Empty<string>();
            CatalogReadCount = catalogReadCount090;
            PngReadCount = pngReadCount090;
            CacheHitCount = cacheHitCount090;
            EvictionCount = evictionCount090;
            ResidentSpriteCount = residentSpriteCount090;
            PinnedSpriteCount = pinnedSpriteCount090;
            OutstandingLeaseCount = outstandingLeaseCount090;
            RetiredSpriteCount = retiredSpriteCount090;
            CurrentVariantId = currentVariantId090 ?? string.Empty;
            RootDirectory = rootDirectory090 ?? string.Empty;
            LastError = lastError090 ?? string.Empty;
        }

        public bool CatalogLoaded { get; }
        public int CatalogCount { get; }
        public int CacheCapacity { get; }
        public int CachedSpriteCount => CachedKeys.Count;
        public IReadOnlyList<string> CachedKeys { get; }
        public long CatalogReadCount { get; }
        public long PngReadCount { get; }
        public long CacheHitCount { get; }
        public long EvictionCount { get; }
        public int ResidentSpriteCount { get; }
        public int PinnedSpriteCount { get; }
        public int OutstandingLeaseCount { get; }
        public int RetiredSpriteCount { get; }
        public string CurrentVariantId { get; }
        public string RootDirectory { get; }
        public string LastError { get; }
        public int LiveRemasterResidentSprites098 { get; internal set; }
        public long LiveRemasterResourceReads098 { get; internal set; }
    }

    /// <summary>
    /// Lazy, presentation-only access to the recovered EnemyArt700 package. Callers
    /// must pass its explicit base and variant IDs; this class never maps gameplay
    /// enemy identities or changes battle state. PNGs are decoded only on demand.
    /// </summary>
    public static class EnemyArt700Runtime090
    {
        public const string AssetRoot090 =
            "Assets/SecondDimension/EnemyArt700";
        public const string PlayerRelativeRoot090 =
            "SecondDimension/EnemyArt700";
        public const string CatalogRelativePath090 =
            "Data/EnemyArtCatalog_001_070.json";
        public const string ExpectedSchemaId090 =
            "SD_GOW_ENEMY_ART_700_RECOVERED_V2";
        public const int ExpectedCatalogCount090 = 700;
        // Ninety slots cover idle/action/portrait for the maximum 30-member live
        // encounter; six spare entries absorb pose transitions without evicting an
        // actor that is still on screen. This remains far below the 2,100-image pack.
        public const int DefaultCacheCapacity090 = 96;
        public const int MaximumCacheCapacity090 = 96;
        public const float SmallRelativeScale090 = 0.86f;
        public const float DefaultRelativeScale090 = 1f;
        public const float LargeRelativeScale090 = 1.18f;

        private const int MinimumCacheCapacity090 = 3;
        private const float PixelsPerUnit090 = 100f;
        private static readonly Dictionary<string, EnemyArt700Variant090>
            VariantsById090 = new Dictionary<string, EnemyArt700Variant090>(
                StringComparer.Ordinal);
        private static readonly Dictionary<string, List<EnemyArt700Variant090>>
            VariantsByBase090 = new Dictionary<string, List<EnemyArt700Variant090>>(
                StringComparer.Ordinal);
        private static readonly Dictionary<string, SpriteCacheEntry090>
            SpriteCache090 = new Dictionary<string, SpriteCacheEntry090>(
                StringComparer.Ordinal);
        private static readonly LinkedList<string> LeastRecentlyUsed090 =
            new LinkedList<string>();
        private static readonly HashSet<SpriteCacheEntry090> RetiredEntries090 =
            new HashSet<SpriteCacheEntry090>();

        private static EnemyArt700Catalog090 _catalog090;
        private static int _cacheCapacity090 = DefaultCacheCapacity090;
        private static string _currentBaseEnemyId090 = string.Empty;
        private static string _currentVariantId090 = string.Empty;
        private static string _rootDirectoryOverrideForTests090;
        private static string _lastError090 = string.Empty;
        private static long _catalogReadCount090;
        private static long _pngReadCount090;
        private static long _cacheHitCount090;
        private static long _evictionCount090;

        public static EnemyArt700Catalog090 Catalog090
        {
            get { EnsureCatalog090(); return _catalog090; }
        }

        public static int CatalogCount090
        {
            get
            {
                EnsureCatalog090();
                return _catalog090.variants == null ? 0 : _catalog090.variants.Length;
            }
        }

        public static int CacheCapacity090 => _cacheCapacity090;
        public static string CurrentVariantId090 => _currentVariantId090;
        public static string RootDirectory090 => ResolveRootDirectory090();

        /// <summary>
        /// Returns an explicit framing scale for the 70 recovered base silhouettes.
        /// The three classes are presentation-only: they do not imply mechanics,
        /// stats, rank, hit-box size, or boss authority. Unknown IDs stay neutral.
        /// </summary>
        public static float RelativeScale090(string baseEnemyId090)
        {
            switch (ScaleClass090(baseEnemyId090))
            {
                case EnemyArt700ScaleClass090.Small: return SmallRelativeScale090;
                case EnemyArt700ScaleClass090.Large: return LargeRelativeScale090;
                default: return DefaultRelativeScale090;
            }
        }

        public static EnemyArt700ScaleClass090 ScaleClass090(string baseEnemyId090)
        {
            switch (NormalizeId090(baseEnemyId090))
            {
                // Compact, narrow, aerial, or low silhouettes.
                case "ENEMY_REC_002":
                case "ENEMY_REC_005":
                case "ENEMY_REC_008":
                case "ENEMY_REC_009":
                case "ENEMY_REC_013":
                case "ENEMY_REC_015":
                case "ENEMY_REC_016":
                case "ENEMY_REC_017":
                case "ENEMY_REC_022":
                case "ENEMY_REC_023":
                case "ENEMY_REC_024":
                case "ENEMY_REC_026":
                case "ENEMY_REC_028":
                case "ENEMY_REC_032":
                case "ENEMY_REC_035":
                case "ENEMY_REC_039":
                case "ENEMY_REC_040":
                case "ENEMY_REC_045":
                case "ENEMY_REC_050":
                case "ENEMY_REC_054":
                case "ENEMY_REC_055":
                case "ENEMY_REC_058":
                case "ENEMY_REC_062":
                case "ENEMY_REC_065":
                case "ENEMY_REC_068":
                    return EnemyArt700ScaleClass090.Small;

                // Broad, tall, or high-mass silhouettes.
                case "ENEMY_REC_003":
                case "ENEMY_REC_007":
                case "ENEMY_REC_010":
                case "ENEMY_REC_011":
                case "ENEMY_REC_014":
                case "ENEMY_REC_020":
                case "ENEMY_REC_025":
                case "ENEMY_REC_029":
                case "ENEMY_REC_031":
                case "ENEMY_REC_033":
                case "ENEMY_REC_034":
                case "ENEMY_REC_038":
                case "ENEMY_REC_041":
                case "ENEMY_REC_042":
                case "ENEMY_REC_043":
                case "ENEMY_REC_044":
                case "ENEMY_REC_046":
                case "ENEMY_REC_048":
                case "ENEMY_REC_049":
                case "ENEMY_REC_052":
                case "ENEMY_REC_053":
                case "ENEMY_REC_056":
                case "ENEMY_REC_059":
                case "ENEMY_REC_060":
                case "ENEMY_REC_061":
                case "ENEMY_REC_063":
                case "ENEMY_REC_067":
                case "ENEMY_REC_070":
                    return EnemyArt700ScaleClass090.Large;

                // Mid-frame silhouettes are listed explicitly so catalog additions
                // cannot silently acquire a non-neutral framing class.
                case "ENEMY_REC_001":
                case "ENEMY_REC_004":
                case "ENEMY_REC_006":
                case "ENEMY_REC_012":
                case "ENEMY_REC_018":
                case "ENEMY_REC_019":
                case "ENEMY_REC_021":
                case "ENEMY_REC_027":
                case "ENEMY_REC_030":
                case "ENEMY_REC_036":
                case "ENEMY_REC_037":
                case "ENEMY_REC_047":
                case "ENEMY_REC_051":
                case "ENEMY_REC_057":
                case "ENEMY_REC_064":
                case "ENEMY_REC_066":
                case "ENEMY_REC_069":
                default:
                    return EnemyArt700ScaleClass090.Default;
            }
        }

        public static bool TryLoadSprite090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            EnemyArt700Pose090 pose090,
            out Sprite sprite090,
            out string error090)
        {
            return TryLoadSpriteCore090(
                baseEnemyId090,
                variantIdOrIndex090,
                pose090,
                acquireLease090: false,
                out sprite090,
                out _,
                out error090);
        }

        /// <summary>
        /// Loads and pins a sprite for a presentation owner. Unlike TryLoadSprite090,
        /// the returned sprite cannot be destroyed by LRU eviction until its lease is
        /// disposed. Long-lived Images and SpriteRenderers must use this API.
        /// </summary>
        public static bool TryAcquireSprite090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            EnemyArt700Pose090 pose090,
            out Sprite sprite090,
            out EnemyArt700SpriteLease090 lease090,
            out string error090)
        {
            return TryLoadSpriteCore090(
                baseEnemyId090,
                variantIdOrIndex090,
                pose090,
                acquireLease090: true,
                out sprite090,
                out lease090,
                out error090);
        }

        /// <summary>Archived raw-pixel QA only; bypasses live exact-identity repairs.</summary>
        public static bool TryLoadSourceSpriteForVerification098(
            string baseEnemyId090, string variantIdOrIndex090, EnemyArt700Pose090 pose090,
            out Sprite sprite090, out string error090) => TryLoadSpriteCore090(
                baseEnemyId090, variantIdOrIndex090, pose090, false,
                out sprite090, out _, out error090, allowLiveRemaster098: false);

        private static bool TryLoadSpriteCore090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            EnemyArt700Pose090 pose090,
            bool acquireLease090,
            out Sprite sprite090,
            out EnemyArt700SpriteLease090 lease090,
            out string error090,
            bool allowLiveRemaster098 = true)
        {
            sprite090 = null;
            lease090 = null;
            error090 = string.Empty;
            if ((int)pose090 < (int)EnemyArt700Pose090.Idle ||
                (int)pose090 > (int)EnemyArt700Pose090.Portrait)
                return Fail090("EnemyArt700 pose is outside the supported range.", out error090);
            if (!TryEnsureCatalog090(out error090)) return false;
            if (!TryResolveVariantFromLoadedCatalog090(
                    baseEnemyId090,
                    variantIdOrIndex090,
                    out var variant090,
                    out error090))
                return false;

            // Only this exact reviewed pair is replaced. It owns one shared
            // Resources atlas outside the per-PNG LRU, with independent leases.
            if (allowLiveRemaster098 && _rootDirectoryOverrideForTests090 == null &&
                EnemyArtRemaster098.Handles098(variant090.baseEnemyId, variant090.variantId))
            {
                var loaded098 = EnemyArtRemaster098.TryLoad098(pose090, acquireLease090,
                    out sprite090, out lease090, out error090);
                _lastError090 = error090;
                return loaded098;
            }

            var cacheKey090 = NormalizeId090(variant090.variantId) + ":" + pose090;
            if (TryGetCachedSprite090(cacheKey090, out sprite090, out var cachedEntry090))
            {
                _cacheHitCount090++;
                if (acquireLease090)
                    lease090 = AcquireLease090(cacheKey090, cachedEntry090);
                _lastError090 = string.Empty;
                return true;
            }

            if (!TryResolvePngPath090(variant090, pose090, out var pngPath090, out error090))
                return false;
            if (!File.Exists(pngPath090))
                return Fail090("EnemyArt700 PNG is missing: " + pngPath090, out error090);

            Texture2D texture090 = null;
            Sprite createdSprite090 = null;
            try
            {
                _pngReadCount090++;
                var pngBytes090 = File.ReadAllBytes(pngPath090);
                texture090 = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
                {
                    name = NormalizeId090(variant090.variantId) + "_" + pose090 + "_Texture090",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (!ImageConversion.LoadImage(texture090, pngBytes090, false))
                    throw new InvalidDataException("Unity rejected the PNG byte stream.");

                var pivot090 = ResolvePivot090(variant090, pose090, texture090);
                var sourceRect091 = new Rect(0f, 0f, texture090.width, texture090.height);
                var framedRect091 = pose090 == EnemyArt700Pose090.Portrait ? sourceRect091 :
                    M1SilhouetteFraming091.PaddedRect091(
                        M1SilhouetteFraming091.VisibleRect091(texture090, sourceRect091), sourceRect091);
                if (framedRect091 != sourceRect091) pivot090 = new Vector2(0.5f, 0.035f);
                createdSprite090 = Sprite.Create(
                    texture090,
                    framedRect091,
                    pivot090,
                    PixelsPerUnit090,
                    0,
                    SpriteMeshType.FullRect);
                if (createdSprite090 == null)
                    throw new InvalidOperationException("Unity could not create a Sprite.");
                createdSprite090.name = NormalizeId090(variant090.variantId) + "_" + pose090 + "_Sprite090";
                texture090.Apply(false, true);

                var createdEntry090 = AddToCache090(
                    cacheKey090,
                    texture090,
                    createdSprite090,
                    acquireLease090 ? 1 : 0);
                sprite090 = createdSprite090;
                if (acquireLease090)
                    lease090 = CreateLease090(cacheKey090, createdEntry090);
                _lastError090 = string.Empty;
                return true;
            }
            catch (Exception exception090)
            {
                if (createdSprite090 != null) DestroyOwnedObject090(createdSprite090);
                if (texture090 != null) DestroyOwnedObject090(texture090);
                return Fail090(
                    "EnemyArt700 failed to load " + pngPath090 + ": " +
                    exception090.Message,
                    out error090);
            }
        }

        public static bool TryLoadSprite090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            string pose090,
            out Sprite sprite090,
            out string error090)
        {
            sprite090 = null;
            if (!TryParsePose090(pose090, out var parsedPose090))
                return Fail090(
                    "EnemyArt700 pose must be IDLE, ATTACK, or PORTRAIT.",
                    out error090);
            return TryLoadSprite090(
                baseEnemyId090,
                variantIdOrIndex090,
                parsedPose090,
                out sprite090,
                out error090);
        }

        public static bool TryLoadVariantSprites090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            out EnemyArt700VariantSprites090 sprites090,
            out string error090)
        {
            sprites090 = null;
            if (!TryResolveVariant090(
                    baseEnemyId090,
                    variantIdOrIndex090,
                    out var variant090,
                    out error090))
                return false;
            if (!TryLoadSprite090(baseEnemyId090, variant090.variantId,
                    EnemyArt700Pose090.Idle, out var idle090, out error090) ||
                !TryLoadSprite090(baseEnemyId090, variant090.variantId,
                    EnemyArt700Pose090.Attack, out var attack090, out error090) ||
                !TryLoadSprite090(baseEnemyId090, variant090.variantId,
                    EnemyArt700Pose090.Portrait, out var portrait090, out error090))
                return false;

            sprites090 = new EnemyArt700VariantSprites090(
                variant090,
                idle090,
                attack090,
                portrait090);
            return true;
        }

        public static bool TryResolveVariant090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            out EnemyArt700Variant090 variant090,
            out string error090)
        {
            variant090 = null;
            if (!TryEnsureCatalog090(out error090)) return false;
            return TryResolveVariantFromLoadedCatalog090(
                baseEnemyId090,
                variantIdOrIndex090,
                out variant090,
                out error090);
        }

        public static bool TrySelectVariantDeterministically090(
            string baseEnemyId090,
            int selectionSeed090,
            out EnemyArt700Variant090 variant090,
            out string error090)
        {
            variant090 = null;
            if (!TryGetFamily090(baseEnemyId090, out var family090, out error090))
                return false;
            var selectedIndex090 = (int)(unchecked((uint)selectionSeed090) %
                                         (uint)family090.Count);
            variant090 = family090[selectedIndex090];
            return true;
        }

        public static bool TrySelectVariantDeterministically090(
            string baseEnemyId090,
            string stableSelectionKey090,
            out EnemyArt700Variant090 variant090,
            out string error090)
        {
            variant090 = null;
            if (string.IsNullOrWhiteSpace(stableSelectionKey090))
                return Fail090(
                    "EnemyArt700 deterministic selection key is required.",
                    out error090);
            if (!TryGetFamily090(baseEnemyId090, out var family090, out error090))
                return false;
            var hash090 = StableHash090(
                NormalizeId090(baseEnemyId090) + "|" + stableSelectionKey090.Trim());
            variant090 = family090[(int)(hash090 % (uint)family090.Count)];
            return true;
        }

        public static bool TrySetCurrentVariant090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            out string error090)
        {
            if (!TryResolveVariant090(
                    baseEnemyId090,
                    variantIdOrIndex090,
                    out var variant090,
                    out error090))
                return false;
            _currentBaseEnemyId090 = NormalizeId090(variant090.baseEnemyId);
            _currentVariantId090 = NormalizeId090(variant090.variantId);
            return true;
        }

        public static bool TryLoadCurrentVariant090(
            out EnemyArt700VariantSprites090 sprites090,
            out string error090)
        {
            sprites090 = null;
            if (string.IsNullOrWhiteSpace(_currentBaseEnemyId090) ||
                string.IsNullOrWhiteSpace(_currentVariantId090))
                return Fail090(
                    "EnemyArt700 current variant has not been selected.",
                    out error090);
            return TryLoadVariantSprites090(
                _currentBaseEnemyId090,
                _currentVariantId090,
                out sprites090,
                out error090);
        }

        public static IReadOnlyList<string> ValidateCatalog090(bool verifyFiles090 = false)
        {
            var issues090 = new List<string>();
            if (!TryEnsureCatalog090(out var loadError090))
            {
                issues090.Add("ENEMY_ART_700_CATALOG_LOAD_FAILED: " + loadError090);
                return issues090;
            }

            if (!StringComparer.Ordinal.Equals(_catalog090.schemaId, ExpectedSchemaId090))
                issues090.Add("ENEMY_ART_700_SCHEMA_ID_DRIFT");
            if (!_catalog090.assetOnly)
                issues090.Add("ENEMY_ART_700_ASSET_ONLY_FLAG_FALSE");
            if (_catalog090.variants == null ||
                _catalog090.variants.Length != ExpectedCatalogCount090 ||
                _catalog090.variantRecordCount != ExpectedCatalogCount090)
                issues090.Add("ENEMY_ART_700_VARIANT_COUNT_DRIFT");
            if (_catalog090.baseFamilyCount != 70)
                issues090.Add("ENEMY_ART_700_BASE_COUNT_DRIFT");
            if (_catalog090.versionsPerBaseIncludingOriginal != 10)
                issues090.Add("ENEMY_ART_700_VERSION_COUNT_DRIFT");

            var seenIds090 = new HashSet<string>(StringComparer.Ordinal);
            var familyCounts090 = new Dictionary<string, int>(StringComparer.Ordinal);
            var variants090 = _catalog090.variants ?? Array.Empty<EnemyArt700Variant090>();
            for (var index090 = 0; index090 < variants090.Length; index090++)
            {
                var variant090 = variants090[index090];
                if (variant090 == null)
                {
                    issues090.Add("ENEMY_ART_700_NULL_VARIANT_" + index090);
                    continue;
                }

                var variantId090 = NormalizeId090(variant090.variantId);
                var baseId090 = NormalizeId090(variant090.baseEnemyId);
                if (variantId090.Length == 0)
                    issues090.Add("ENEMY_ART_700_VARIANT_ID_MISSING_" + index090);
                else if (!seenIds090.Add(variantId090))
                    issues090.Add("ENEMY_ART_700_DUPLICATE_VARIANT_" + variantId090);
                if (baseId090.Length == 0)
                    issues090.Add("ENEMY_ART_700_BASE_ID_MISSING_" + variantId090);
                else
                {
                    familyCounts090.TryGetValue(baseId090, out var count090);
                    familyCounts090[baseId090] = count090 + 1;
                }
                if (variant090.variantIndex < 1 || variant090.variantIndex > 10)
                    issues090.Add("ENEMY_ART_700_VARIANT_INDEX_INVALID_" + variantId090);

                ValidatePose090(variant090, EnemyArt700Pose090.Idle,
                    verifyFiles090, issues090);
                ValidatePose090(variant090, EnemyArt700Pose090.Attack,
                    verifyFiles090, issues090);
                ValidatePose090(variant090, EnemyArt700Pose090.Portrait,
                    verifyFiles090, issues090);
            }

            if (familyCounts090.Count != 70)
                issues090.Add("ENEMY_ART_700_FAMILY_INDEX_COUNT_DRIFT");
            foreach (var pair090 in familyCounts090)
                if (pair090.Value != 10)
                    issues090.Add("ENEMY_ART_700_FAMILY_VARIANT_COUNT_" + pair090.Key);
            return issues090;
        }

        public static EnemyArt700RuntimeDiagnostics090 GetDiagnostics090()
        {
            var keys090 = new string[LeastRecentlyUsed090.Count];
            var node090 = LeastRecentlyUsed090.First;
            for (var index090 = 0; node090 != null; index090++, node090 = node090.Next)
                keys090[index090] = node090.Value;
            var pinnedSpriteCount090 = 0;
            var outstandingLeaseCount090 = 0;
            foreach (var pair090 in SpriteCache090)
            {
                if (pair090.Value.PinCount090 <= 0) continue;
                pinnedSpriteCount090++;
                outstandingLeaseCount090 += pair090.Value.PinCount090;
            }
            foreach (var entry090 in RetiredEntries090)
            {
                if (entry090.PinCount090 <= 0) continue;
                pinnedSpriteCount090++;
                outstandingLeaseCount090 += entry090.PinCount090;
            }
            return new EnemyArt700RuntimeDiagnostics090(
                _catalog090 != null,
                _catalog090?.variants?.Length ?? 0,
                _cacheCapacity090,
                keys090,
                _catalogReadCount090,
                _pngReadCount090,
                _cacheHitCount090,
                _evictionCount090,
                SpriteCache090.Count + RetiredEntries090.Count + EnemyArtRemaster098.ResidentSpriteCount098,
                pinnedSpriteCount090 + EnemyArtRemaster098.PinnedSpriteCount098,
                outstandingLeaseCount090 + EnemyArtRemaster098.OutstandingLeaseCount098,
                RetiredEntries090.Count + EnemyArtRemaster098.RetiredSpriteCount098,
                _currentVariantId090,
                ResolveRootDirectory090(),
                _lastError090)
            {
                LiveRemasterResidentSprites098 = EnemyArtRemaster098.ResidentSpriteCount098,
                LiveRemasterResourceReads098 = EnemyArtRemaster098.ResourceLoadCount098
            };
        }

        public static void ConfigureCacheCapacity090(int capacity090)
        {
            if (capacity090 < MinimumCacheCapacity090 ||
                capacity090 > MaximumCacheCapacity090)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity090),
                    "EnemyArt700 cache capacity must be between " +
                    MinimumCacheCapacity090 + " and " + MaximumCacheCapacity090 + ".");
            _cacheCapacity090 = capacity090;
            TrimCache090();
        }

        public static void ClearCache090()
        {
            EnemyArtRemaster098.Reset098();
            foreach (var pair090 in SpriteCache090)
            {
                var entry090 = pair090.Value;
                if (entry090.PinCount090 > 0)
                {
                    entry090.Retired090 = true;
                    RetiredEntries090.Add(entry090);
                }
                else
                {
                    DestroyEntry090(entry090);
                }
            }
            SpriteCache090.Clear();
            LeastRecentlyUsed090.Clear();
        }

        public static void SetRootDirectoryForTests090(string rootDirectory090)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory090))
                throw new ArgumentException(
                    "EnemyArt700 test root is required.",
                    nameof(rootDirectory090));
            ResetForTests090();
            _rootDirectoryOverrideForTests090 = Path.GetFullPath(rootDirectory090);
        }

        public static void ResetForTests090()
        {
            EnemyArtRemaster098.Reset098(resetReadCounter: true);
            ForceDestroyAllEntriesForTests090();
            _catalog090 = null;
            VariantsById090.Clear();
            VariantsByBase090.Clear();
            _cacheCapacity090 = DefaultCacheCapacity090;
            _currentBaseEnemyId090 = string.Empty;
            _currentVariantId090 = string.Empty;
            _rootDirectoryOverrideForTests090 = null;
            _lastError090 = string.Empty;
            _catalogReadCount090 = 0;
            _pngReadCount090 = 0;
            _cacheHitCount090 = 0;
            _evictionCount090 = 0;
        }

        private static bool TryEnsureCatalog090(out string error090)
        {
            try
            {
                EnsureCatalog090();
                error090 = string.Empty;
                return true;
            }
            catch (Exception exception090)
            {
                return Fail090(exception090.Message, out error090);
            }
        }

        private static void EnsureCatalog090()
        {
            if (_catalog090 != null) return;
            EnsureSupportedPlatform090();
            var catalogPath090 = Path.Combine(
                ResolveRootDirectory090(),
                CatalogRelativePath090.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(catalogPath090))
                throw new FileNotFoundException(
                    "EnemyArt700 catalog is missing.",
                    catalogPath090);

            var json090 = File.ReadAllText(catalogPath090);
            _catalogReadCount090++;
            var parsed090 = JsonUtility.FromJson<EnemyArt700Catalog090>(json090);
            if (parsed090 == null || parsed090.variants == null)
                throw new InvalidDataException(
                    "EnemyArt700 catalog JSON did not contain a variants array.");
            ValidateParsedCatalogOrThrow090(parsed090);

            var byId090 = new Dictionary<string, EnemyArt700Variant090>(
                StringComparer.Ordinal);
            var byBase090 = new Dictionary<string, List<EnemyArt700Variant090>>(
                StringComparer.Ordinal);
            for (var index090 = 0; index090 < parsed090.variants.Length; index090++)
            {
                var variant090 = parsed090.variants[index090];
                if (variant090 == null) continue;
                var variantId090 = NormalizeId090(variant090.variantId);
                var baseId090 = NormalizeId090(variant090.baseEnemyId);
                if (variantId090.Length == 0 || baseId090.Length == 0) continue;
                if (byId090.ContainsKey(variantId090))
                    throw new InvalidDataException(
                        "EnemyArt700 duplicate variant ID: " + variantId090);
                byId090.Add(variantId090, variant090);
                if (!byBase090.TryGetValue(baseId090, out var family090))
                {
                    family090 = new List<EnemyArt700Variant090>();
                    byBase090.Add(baseId090, family090);
                }
                family090.Add(variant090);
            }

            foreach (var pair090 in byBase090)
                pair090.Value.Sort(CompareVariants090);
            VariantsById090.Clear();
            VariantsByBase090.Clear();
            foreach (var pair090 in byId090) VariantsById090.Add(pair090.Key, pair090.Value);
            foreach (var pair090 in byBase090) VariantsByBase090.Add(pair090.Key, pair090.Value);
            _catalog090 = parsed090;
        }

        private static void ValidateParsedCatalogOrThrow090(
            EnemyArt700Catalog090 catalog090)
        {
            if (!StringComparer.Ordinal.Equals(
                    catalog090.schemaId,
                    ExpectedSchemaId090))
                throw new InvalidDataException(
                    "EnemyArt700 catalog schema ID is not supported: " +
                    catalog090.schemaId);
            if (!catalog090.assetOnly ||
                !catalog090.recoveryPreservesUploadedPixels)
                throw new InvalidDataException(
                    "EnemyArt700 catalog asset-only or pixel-preservation contract is false.");
            if (catalog090.baseFamilyCount != 70 ||
                catalog090.variantRecordCount != ExpectedCatalogCount090 ||
                catalog090.versionsPerBaseIncludingOriginal != 10 ||
                catalog090.battleSpriteCount != 1400 ||
                catalog090.portraitCount != 700 ||
                catalog090.variants.Length != ExpectedCatalogCount090)
                throw new InvalidDataException(
                    "EnemyArt700 catalog header counts do not match the 70-family/700-variant contract.");

            var variantIds090 = new HashSet<string>(StringComparer.Ordinal);
            var familyIndexMasks090 = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index090 = 0; index090 < catalog090.variants.Length; index090++)
            {
                var variant090 = catalog090.variants[index090];
                if (variant090 == null)
                    throw new InvalidDataException(
                        "EnemyArt700 catalog contains a null variant at index " + index090 + ".");

                var baseId090 = NormalizeId090(variant090.baseEnemyId);
                var variantId090 = NormalizeId090(variant090.variantId);
                if (!IsCanonicalBaseId090(baseId090))
                    throw new InvalidDataException(
                        "EnemyArt700 base ID is not canonical: " + baseId090);
                if (variant090.variantIndex < 1 || variant090.variantIndex > 10)
                    throw new InvalidDataException(
                        "EnemyArt700 variant index is outside 1-10: " + variantId090);
                var expectedVariantId090 = baseId090 + "_VAR_" +
                                           variant090.variantIndex.ToString(
                                               "00",
                                               CultureInfo.InvariantCulture);
                if (!StringComparer.Ordinal.Equals(variantId090, expectedVariantId090))
                    throw new InvalidDataException(
                        "EnemyArt700 variant ID does not match its base/index: " + variantId090);
                if (!variantIds090.Add(variantId090))
                    throw new InvalidDataException(
                        "EnemyArt700 duplicate variant ID: " + variantId090);
                if (variant090.isOriginalVersion != (variant090.variantIndex == 1))
                    throw new InvalidDataException(
                        "EnemyArt700 original-version flag is inconsistent: " + variantId090);
                if (variant090.runtimeStatRulesProvided)
                    throw new InvalidDataException(
                        "EnemyArt700 unexpectedly contains runtime stat rules: " + variantId090);
                if (string.IsNullOrWhiteSpace(variant090.recoveryProvenance))
                    throw new InvalidDataException(
                        "EnemyArt700 recovery provenance is missing: " + variantId090);

                familyIndexMasks090.TryGetValue(baseId090, out var mask090);
                var variantBit090 = 1 << variant090.variantIndex;
                if ((mask090 & variantBit090) != 0)
                    throw new InvalidDataException(
                        "EnemyArt700 duplicate family variant index: " + expectedVariantId090);
                familyIndexMasks090[baseId090] = mask090 | variantBit090;

                ValidateParsedPosePathOrThrow090(
                    variant090,
                    EnemyArt700Pose090.Idle);
                ValidateParsedPosePathOrThrow090(
                    variant090,
                    EnemyArt700Pose090.Attack);
                ValidateParsedPosePathOrThrow090(
                    variant090,
                    EnemyArt700Pose090.Portrait);
            }

            const int completeVariantMask090 = 0x7FE;
            if (familyIndexMasks090.Count != 70)
                throw new InvalidDataException(
                    "EnemyArt700 catalog does not contain exactly 70 base families.");
            foreach (var pair090 in familyIndexMasks090)
                if (pair090.Value != completeVariantMask090)
                    throw new InvalidDataException(
                        "EnemyArt700 family does not contain each variant index 1-10: " +
                        pair090.Key);
        }

        private static void ValidateParsedPosePathOrThrow090(
            EnemyArt700Variant090 variant090,
            EnemyArt700Pose090 pose090)
        {
            var authoredPath090 = PosePath090(variant090.unityAssetPaths, pose090);
            if (string.IsNullOrWhiteSpace(authoredPath090))
                authoredPath090 = PosePath090(variant090.files, pose090);
            if (!TryGetPackRelativePath090(authoredPath090, out _, out var error090))
                throw new InvalidDataException(
                    "EnemyArt700 invalid " + pose090 + " path for " +
                    NormalizeId090(variant090.variantId) + ": " + error090);
        }

        private static bool IsCanonicalBaseId090(string baseId090)
        {
            if (baseId090.Length != 13 ||
                !baseId090.StartsWith("ENEMY_REC_", StringComparison.Ordinal))
                return false;
            if (!int.TryParse(
                    baseId090.Substring(10, 3),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var baseIndex090))
                return false;
            return baseIndex090 >= 1 && baseIndex090 <= 70;
        }

        private static bool TryResolveVariantFromLoadedCatalog090(
            string baseEnemyId090,
            string variantIdOrIndex090,
            out EnemyArt700Variant090 variant090,
            out string error090)
        {
            variant090 = null;
            var baseId090 = NormalizeId090(baseEnemyId090);
            var variantToken090 = NormalizeId090(variantIdOrIndex090);
            if (baseId090.Length == 0)
                return Fail090("EnemyArt700 base enemy ID is required.", out error090);
            if (variantToken090.Length == 0)
                return Fail090("EnemyArt700 variant ID or index is required.", out error090);

            if (VariantsById090.TryGetValue(variantToken090, out var direct090))
            {
                if (!StringComparer.Ordinal.Equals(
                        baseId090,
                        NormalizeId090(direct090.baseEnemyId)))
                    return Fail090(
                        "EnemyArt700 variant " + variantToken090 +
                        " does not belong to " + baseId090 + ".",
                        out error090);
                variant090 = direct090;
                error090 = string.Empty;
                _lastError090 = string.Empty;
                return true;
            }

            if (!TryParseVariantIndex090(variantToken090, out var variantIndex090))
                return Fail090(
                    "EnemyArt700 variant was not found: " + variantToken090,
                    out error090);
            if (!VariantsByBase090.TryGetValue(baseId090, out var family090))
                return Fail090(
                    "EnemyArt700 base enemy was not found: " + baseId090,
                    out error090);
            for (var index090 = 0; index090 < family090.Count; index090++)
                if (family090[index090].variantIndex == variantIndex090)
                {
                    variant090 = family090[index090];
                    error090 = string.Empty;
                    _lastError090 = string.Empty;
                    return true;
                }
            return Fail090(
                "EnemyArt700 variant index " + variantIndex090 +
                " was not found for " + baseId090 + ".",
                out error090);
        }

        private static bool TryGetFamily090(
            string baseEnemyId090,
            out List<EnemyArt700Variant090> family090,
            out string error090)
        {
            family090 = null;
            if (!TryEnsureCatalog090(out error090)) return false;
            var baseId090 = NormalizeId090(baseEnemyId090);
            if (baseId090.Length == 0)
                return Fail090("EnemyArt700 base enemy ID is required.", out error090);
            if (!VariantsByBase090.TryGetValue(baseId090, out family090) ||
                family090.Count == 0)
                return Fail090(
                    "EnemyArt700 base enemy was not found: " + baseId090,
                    out error090);
            error090 = string.Empty;
            _lastError090 = string.Empty;
            return true;
        }

        private static bool TryResolvePngPath090(
            EnemyArt700Variant090 variant090,
            EnemyArt700Pose090 pose090,
            out string pngPath090,
            out string error090)
        {
            pngPath090 = string.Empty;
            var authoredPath090 = PosePath090(variant090.unityAssetPaths, pose090);
            if (string.IsNullOrWhiteSpace(authoredPath090))
                authoredPath090 = PosePath090(variant090.files, pose090);
            if (!TryGetPackRelativePath090(
                    authoredPath090,
                    out var relativePath090,
                    out error090))
                return false;

            var root090 = Path.GetFullPath(ResolveRootDirectory090());
            var rootPrefix090 = root090.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var candidate090 = Path.GetFullPath(Path.Combine(
                root090,
                relativePath090.Replace('/', Path.DirectorySeparatorChar)));
            if (!candidate090.StartsWith(rootPrefix090, StringComparison.OrdinalIgnoreCase))
                return Fail090(
                    "EnemyArt700 rejected a path outside its runtime root.",
                    out error090);
            pngPath090 = candidate090;
            error090 = string.Empty;
            return true;
        }

        private static bool TryGetPackRelativePath090(
            string authoredPath090,
            out string relativePath090,
            out string error090)
        {
            relativePath090 = string.Empty;
            if (string.IsNullOrWhiteSpace(authoredPath090))
                return Fail090("EnemyArt700 catalog pose path is missing.", out error090);
            var normalized090 = authoredPath090.Trim().Replace('\\', '/');
            var assetPrefix090 = AssetRoot090 + "/";
            var stagedPrefix090 = "UNITY_DROP_IN/" + assetPrefix090;
            if (normalized090.StartsWith(assetPrefix090, StringComparison.Ordinal))
                relativePath090 = normalized090.Substring(assetPrefix090.Length);
            else if (normalized090.StartsWith(stagedPrefix090, StringComparison.Ordinal))
                relativePath090 = normalized090.Substring(stagedPrefix090.Length);
            else
                return Fail090(
                    "EnemyArt700 catalog path is outside " + AssetRoot090 + ".",
                    out error090);

            if (!relativePath090.StartsWith("Textures/", StringComparison.Ordinal) ||
                !relativePath090.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return Fail090(
                    "EnemyArt700 catalog pose path is not a texture PNG.",
                    out error090);
            var segments090 = relativePath090.Split('/');
            for (var index090 = 0; index090 < segments090.Length; index090++)
                if (segments090[index090].Length == 0 ||
                    segments090[index090] == "." ||
                    segments090[index090] == "..")
                    return Fail090(
                        "EnemyArt700 catalog pose path contains an invalid segment.",
                        out error090);
            error090 = string.Empty;
            return true;
        }

        private static void ValidatePose090(
            EnemyArt700Variant090 variant090,
            EnemyArt700Pose090 pose090,
            bool verifyFile090,
            List<string> issues090)
        {
            if (!TryResolvePngPath090(variant090, pose090, out var path090, out _))
            {
                issues090.Add("ENEMY_ART_700_PATH_INVALID_" +
                              NormalizeId090(variant090.variantId) + "_" + pose090);
                return;
            }
            if (verifyFile090 && !File.Exists(path090))
                issues090.Add("ENEMY_ART_700_FILE_MISSING_" +
                              NormalizeId090(variant090.variantId) + "_" + pose090);
        }

        private static string PosePath090(
            EnemyArt700FileSet090 fileSet090,
            EnemyArt700Pose090 pose090)
        {
            if (fileSet090 == null) return string.Empty;
            switch (pose090)
            {
                case EnemyArt700Pose090.Idle: return fileSet090.idle;
                case EnemyArt700Pose090.Attack: return fileSet090.attack;
                case EnemyArt700Pose090.Portrait: return fileSet090.portrait;
                default: return string.Empty;
            }
        }

        private static bool TryParsePose090(
            string value090,
            out EnemyArt700Pose090 pose090)
        {
            pose090 = EnemyArt700Pose090.Idle;
            var normalized090 = NormalizeId090(value090);
            if (normalized090 == "IDLE") return true;
            if (normalized090 == "ATTACK" || normalized090 == "ACTION")
            {
                pose090 = EnemyArt700Pose090.Attack;
                return true;
            }
            if (normalized090 == "PORTRAIT")
            {
                pose090 = EnemyArt700Pose090.Portrait;
                return true;
            }
            return false;
        }

        private static bool TryParseVariantIndex090(string value090, out int index090)
        {
            var token090 = value090;
            if (token090.StartsWith("VAR_", StringComparison.Ordinal))
                token090 = token090.Substring(4);
            else if (token090.StartsWith("VAR", StringComparison.Ordinal))
                token090 = token090.Substring(3);
            return int.TryParse(
                       token090,
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out index090) &&
                   index090 >= 1 && index090 <= 10;
        }

        private static bool TryGetCachedSprite090(
            string key090,
            out Sprite sprite090,
            out SpriteCacheEntry090 entry090)
        {
            sprite090 = null;
            entry090 = null;
            if (!SpriteCache090.TryGetValue(key090, out entry090)) return false;
            if (entry090.Sprite090 == null || entry090.Texture090 == null)
            {
                SpriteCache090.Remove(key090);
                LeastRecentlyUsed090.Remove(entry090.Node090);
                DestroyEntry090(entry090);
                entry090 = null;
                return false;
            }
            LeastRecentlyUsed090.Remove(entry090.Node090);
            LeastRecentlyUsed090.AddFirst(entry090.Node090);
            sprite090 = entry090.Sprite090;
            return true;
        }

        private static SpriteCacheEntry090 AddToCache090(
            string key090,
            Texture2D texture090,
            Sprite sprite090,
            int initialPinCount090)
        {
            var node090 = LeastRecentlyUsed090.AddFirst(key090);
            var entry090 = new SpriteCacheEntry090(
                texture090,
                sprite090,
                node090,
                initialPinCount090);
            SpriteCache090.Add(
                key090,
                entry090);
            // Never destroy the object that this load is about to return. If every
            // older entry is pinned, a borrowed load may extend residency by one
            // until the next load/release; long-lived owners use leases instead.
            TrimCache090(entry090);
            return entry090;
        }

        private static EnemyArt700SpriteLease090 AcquireLease090(
            string key090,
            SpriteCacheEntry090 entry090)
        {
            entry090.PinCount090++;
            return CreateLease090(key090, entry090);
        }

        private static EnemyArt700SpriteLease090 CreateLease090(
            string key090,
            SpriteCacheEntry090 entry090)
        {
            return new EnemyArt700SpriteLease090(
                entry090.Sprite090,
                key090,
                () => ReleaseLease090(entry090));
        }

        private static void ReleaseLease090(SpriteCacheEntry090 entry090)
        {
            if (entry090 == null || entry090.Destroyed090 || entry090.PinCount090 <= 0)
                return;
            entry090.PinCount090--;
            if (entry090.PinCount090 > 0) return;
            if (entry090.Retired090)
            {
                RetiredEntries090.Remove(entry090);
                DestroyEntry090(entry090);
                return;
            }
            TrimCache090();
        }

        private static void TrimCache090(
            SpriteCacheEntry090 protectedEntry090 = null)
        {
            while (SpriteCache090.Count > _cacheCapacity090)
            {
                var node090 = LeastRecentlyUsed090.Last;
                var evicted090 = false;
                while (node090 != null)
                {
                    var previous090 = node090.Previous;
                    if (!SpriteCache090.TryGetValue(node090.Value, out var entry090))
                    {
                        LeastRecentlyUsed090.Remove(node090);
                        node090 = previous090;
                        continue;
                    }
                    if (entry090.PinCount090 > 0)
                    {
                        node090 = previous090;
                        continue;
                    }
                    if (ReferenceEquals(entry090, protectedEntry090))
                    {
                        node090 = previous090;
                        continue;
                    }
                    LeastRecentlyUsed090.Remove(node090);
                    SpriteCache090.Remove(node090.Value);
                    DestroyEntry090(entry090);
                    _evictionCount090++;
                    evicted090 = true;
                    break;
                }
                // Active presentation owners are an explicit extension to the LRU
                // capacity. Normal battles retain fewer than the 96-slot maximum;
                // this guard also guarantees correctness if a future battle exceeds it.
                if (!evicted090) break;
            }
        }

        private static void DestroyEntry090(SpriteCacheEntry090 entry090)
        {
            if (entry090 == null || entry090.Destroyed090) return;
            entry090.Destroyed090 = true;
            entry090.Retired090 = false;
            RetiredEntries090.Remove(entry090);
            if (entry090.Sprite090 != null) DestroyOwnedObject090(entry090.Sprite090);
            if (entry090.Texture090 != null) DestroyOwnedObject090(entry090.Texture090);
        }

        private static void ForceDestroyAllEntriesForTests090()
        {
            foreach (var pair090 in SpriteCache090)
                DestroyEntry090(pair090.Value);
            foreach (var entry090 in new List<SpriteCacheEntry090>(RetiredEntries090))
                DestroyEntry090(entry090);
            SpriteCache090.Clear();
            LeastRecentlyUsed090.Clear();
            RetiredEntries090.Clear();
        }

        private static void DestroyOwnedObject090(UnityEngine.Object value090)
        {
            if (value090 == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value090);
            else UnityEngine.Object.DestroyImmediate(value090);
        }

        private static Vector2 ResolvePivot090(
            EnemyArt700Variant090 variant090,
            EnemyArt700Pose090 pose090,
            Texture2D texture090)
        {
            if (pose090 == EnemyArt700Pose090.Portrait ||
                variant090.suggestedLayoutAnchorPixelsBottomLeft == null ||
                variant090.suggestedLayoutAnchorPixelsBottomLeft.Length < 2 ||
                texture090.width <= 0 || texture090.height <= 0)
                return new Vector2(0.5f, 0.5f);
            return new Vector2(
                Mathf.Clamp01(
                    variant090.suggestedLayoutAnchorPixelsBottomLeft[0] /
                    (float)texture090.width),
                Mathf.Clamp01(
                    variant090.suggestedLayoutAnchorPixelsBottomLeft[1] /
                    (float)texture090.height));
        }

        private static string ResolveRootDirectory090()
        {
            if (!string.IsNullOrWhiteSpace(_rootDirectoryOverrideForTests090))
                return _rootDirectoryOverrideForTests090;
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "EnemyArt700"));
#else
            return Path.GetFullPath(Path.Combine(
                Application.streamingAssetsPath,
                "SecondDimension",
                "EnemyArt700"));
#endif
        }

        private static void EnsureSupportedPlatform090()
        {
            if (!string.IsNullOrWhiteSpace(_rootDirectoryOverrideForTests090)) return;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return;
#else
            throw new PlatformNotSupportedException(
                "EnemyArt700 raw-file loading is supported only in the Windows Editor and Windows players.");
#endif
        }

        private static int CompareVariants090(
            EnemyArt700Variant090 left090,
            EnemyArt700Variant090 right090)
        {
            var indexComparison090 = left090.variantIndex.CompareTo(right090.variantIndex);
            return indexComparison090 != 0
                ? indexComparison090
                : StringComparer.Ordinal.Compare(
                    NormalizeId090(left090.variantId),
                    NormalizeId090(right090.variantId));
        }

        private static uint StableHash090(string value090)
        {
            unchecked
            {
                var hash090 = 2166136261u;
                for (var index090 = 0; index090 < value090.Length; index090++)
                {
                    hash090 ^= value090[index090];
                    hash090 *= 16777619u;
                }
                return hash090;
            }
        }

        private static string NormalizeId090(string value090)
        {
            return string.IsNullOrWhiteSpace(value090)
                ? string.Empty
                : value090.Trim().ToUpperInvariant();
        }

        private static bool Fail090(string message090, out string error090)
        {
            error090 = message090 ?? "EnemyArt700 operation failed.";
            _lastError090 = error090;
            return false;
        }

        private sealed class SpriteCacheEntry090
        {
            public SpriteCacheEntry090(
                Texture2D texture090,
                Sprite sprite090,
                LinkedListNode<string> node090,
                int initialPinCount090)
            {
                Texture090 = texture090;
                Sprite090 = sprite090;
                Node090 = node090;
                PinCount090 = Math.Max(0, initialPinCount090);
            }

            public Texture2D Texture090 { get; }
            public Sprite Sprite090 { get; }
            public LinkedListNode<string> Node090 { get; }
            public int PinCount090 { get; set; }
            public bool Retired090 { get; set; }
            public bool Destroyed090 { get; set; }
        }
    }
}
