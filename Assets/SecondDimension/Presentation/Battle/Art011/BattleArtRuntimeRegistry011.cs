using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;

namespace SecondDimension.Presentation
{
    [Serializable]
    public sealed class BattleArtPose011
    {
        public string poseId;
        public string resourcePath;
        public string sourceStatus;
        public int[] canvas;
        public float[] pivot;
    }

    [Serializable]
    public sealed class BattleArtCharacter011
    {
        public string memberId;
        public string displayName;
        public string roleId;
        public string facing;
        public BattleArtPose011[] poses;
        public string releaseStatus;
    }

    [Serializable]
    public sealed class BattleArtVfx011
    {
        public string vfxId;
        public string resourcePath;
        public string schoolId;
        public string weaponFamilyId;
        public string kind;
        public string primaryColor;
        public string secondaryColor;
    }

    [Serializable]
    public sealed class BattleArtAudio011
    {
        public string cueId;
        public string resourcePath;
        public string familyId;
        public string schoolId;
        public string kind;
    }

    [Serializable]
    public sealed class BattleArtUiAsset011
    {
        public string assetId;
        public string kind;
        public string resourcePath;
    }

    [Serializable]
    public sealed class BattleArtProfile011
    {
        public string artId;
        public string displayName;
        public string artClass;
        public string weaponFamilyId;
        public string schoolId;
        public string animationFamilyId;
        public string motionProfile;
        public string cameraProfileId;
        public string trailResourcePath;
        public string projectileResourcePath;
        public string impactResourcePath;
        public string windupAudioResourcePath;
        public string audioResourcePath;
        public string startAudioResourcePath;
        public string impactAudioResourcePath;
        public string fieldResourcePath;
        public string primaryColor;
        public string secondaryColor;
        public string glyph;
        public int hitStopMilliseconds;
        public string screenShake;
        public bool requiresDedicatedActionPose;
        public bool returnToFormationRequired;
        public bool playerDirectlySelectableInStandard;
        public bool memberActionClickable;

        // Release 076 live-route provenance. Manifest-authored profiles leave these
        // fields empty; the first-hour resolver fills them from the exact 120-recipe
        // catalog so the shipping 072 diorama never presents a role Art as a random
        // weapon/mystic fallback.
        public bool exactFirstHourRecipe;
        public string presentationRecipeId;
        public string presentationTreeId;
        public string presentationVfxSignature;
        public string presentationSfxSignature;
        public string presentationMotionSignature;
        public int presentationDurationMilliseconds;
        public int presentationImpactMilliseconds;
        public string semanticIconAssetId;

        internal BattleArtProfile011 CopyWithIdentity101(string id, string name)
        {
            // All profile fields are strings or scalar values; retain the existing
            // physical resources without renaming the shared authored profile.
            var copy = (BattleArtProfile011)MemberwiseClone();
            copy.artId = id;
            copy.displayName = name;
            // A reusable physical profile is not authority for a different Art's
            // exact choreography/signatures, even if future manifest data adds them.
            copy.exactFirstHourRecipe = false;
            copy.presentationRecipeId = copy.presentationTreeId = null;
            copy.presentationVfxSignature = copy.presentationSfxSignature = null;
            copy.presentationMotionSignature = null;
            copy.presentationDurationMilliseconds = copy.presentationImpactMilliseconds = 0;
            return copy;
        }
    }

    [Serializable]
    public sealed class BattleArtAlias011
    {
        public string aliasArtId;
        public string runtimeArtId;
        public string reason;
    }

    [Serializable]
    public sealed class BattleArtLaw011
    {
        public bool runtimeGenerativeAi;
        public bool presentationNeverChangesResolution;
        public bool playerSelectsCompleteUnionForecast;
        public bool memberActionClickable;
        public int maximumAllyUnions;
        public int maximumEnemyUnions;
    }

    [Serializable]
    public sealed class BattleArtManifest011
    {
        public string contentVersion;
        public int characterCount;
        public BattleArtCharacter011[] characters;
        public int vfxCount;
        public BattleArtVfx011[] vfx;
        public int audioCount;
        public BattleArtAudio011[] audio;
        public int uiAssetCount;
        public BattleArtUiAsset011[] uiAssets;
        public int runtimeArtProfileCount;
        public BattleArtProfile011[] runtimeArtProfiles;
        public int legacyAliasCount;
        public BattleArtAlias011[] legacyAliases;
        public BattleArtLaw011 law;
    }

    /// <summary>
    /// Presentation-only registry for the Thursday 011 art package. It loads clean
    /// pre-authored local assets and never enters combat resolution, save hashes,
    /// Forecast generation, AP/MP legality, damage, or learning decisions.
    /// </summary>
    public static class BattleArtRuntimeRegistry011
    {
        public const string ResourceManifestPath =
            "SecondDimension/Data/BattleArt011/BATTLE_ART_RUNTIME_MANIFEST_011";

        private static BattleArtManifest011 _manifest;
        private static Dictionary<string, BattleArtCharacter011> _characters;
        private static Dictionary<string, BattleArtProfile011> _profiles;
        private static Dictionary<string, BattleArtUiAsset011> _uiAssets;
        private static Dictionary<string, string> _aliases;
        private static Dictionary<string, BattleArtProfile011> _firstHourProfiles076;
        private static BattleArtProfile011 _basicStrikeProfile101;
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly Dictionary<string, AudioClip> AudioCache =
            new Dictionary<string, AudioClip>(StringComparer.Ordinal);

        public static BattleArtManifest011 Manifest
        {
            get { EnsureLoaded(); return _manifest; }
        }

        public static void ReloadForTests()
        {
            _manifest = null;
            _characters = null;
            _profiles = null;
            _uiAssets = null;
            _aliases = null;
            _firstHourProfiles076 = null;
            _basicStrikeProfile101 = null;
            SpriteCache.Clear();
            AudioCache.Clear();
        }

        public static bool TryResolveCharacter(string memberId, out BattleArtCharacter011 character)
        {
            EnsureLoaded();
            var key = Normalize(memberId);
            if (_characters.TryGetValue(key, out character)) return true;
            var spawnMarker070 = key.IndexOf("_SPAWN070_", StringComparison.Ordinal);
            if (spawnMarker070 > 0 &&
                _characters.TryGetValue(key.Substring(0, spawnMarker070), out character)) return true;
            var packMarker = key.IndexOf("_PACK_", StringComparison.Ordinal);
            return packMarker > 0 && _characters.TryGetValue(key.Substring(0, packMarker), out character);
        }

        public static bool TryResolvePose(string memberId, string poseId, out Sprite sprite, out string resourcePath)
        {
            if (TitanArt161.IsMember(memberId))
                return TitanArt161.TryResolvePose(memberId, poseId, out sprite, out resourcePath);
            sprite = null;
            resourcePath = string.Empty;
            if (!TryResolveCharacter(memberId, out var character) || character.poses == null) return false;
            var pose = character.poses.FirstOrDefault(value =>
                value != null && StringComparer.Ordinal.Equals(Normalize(value.poseId), Normalize(poseId)));
            if (pose == null || string.IsNullOrWhiteSpace(pose.resourcePath)) return false;
            resourcePath = pose.resourcePath;
            sprite = LoadSprite(resourcePath);
            return sprite != null;
        }

        public static BattleArtProfile011 ResolveProfile(string artId, BattleBeatFamily fallbackFamily)
        {
            EnsureLoaded();
            var key = Normalize(artId);
            // The resolved event owns recovery presentation. A legacy alias or
            // an attempted Art's exact recipe must not turn it into an attack.
            if (fallbackFamily == BattleBeatFamily.Recovery)
            {
                var recovery = _manifest.runtimeArtProfiles.FirstOrDefault(value =>
                    value != null && StringComparer.Ordinal.Equals(value.artClass, "RESTORATION_ART"));
                return recovery?.CopyWithIdentity101(key,
                    StringComparer.Ordinal.Equals(key, "ART_RECOVER_BREATH")
                        ? "Recover Breath" : "Recovery");
            }
            if (TryResolveExactFirstHourProfile076(key, fallbackFamily, out var firstHour))
                return firstHour;
            if (_profiles.TryGetValue(key, out var direct)) return direct;
            if (StringComparer.Ordinal.Equals(key, "ART_BASIC_STRIKE_101"))
            {
                if (_basicStrikeProfile101 == null)
                {
                    var physical = _manifest.runtimeArtProfiles.FirstOrDefault(value =>
                        value != null && StringComparer.Ordinal.Equals(value.artClass, "COMBAT_ART"));
                    _basicStrikeProfile101 = physical?.CopyWithIdentity101(key, "Basic Strike");
                }
                return _basicStrikeProfile101;
            }
            if (_aliases.TryGetValue(key, out var mapped) && _profiles.TryGetValue(mapped, out var alias)) return alias;

            var preferredClass = fallbackFamily == BattleBeatFamily.Mystic || fallbackFamily == BattleBeatFamily.Invocation ? "MYSTIC_ATTACK" :
                fallbackFamily == BattleBeatFamily.Restoration || fallbackFamily == BattleBeatFamily.Recovery ? "RESTORATION_ART" :
                fallbackFamily == BattleBeatFamily.Guard || fallbackFamily == BattleBeatFamily.Interception ||
                fallbackFamily == BattleBeatFamily.Formation ? "WARDING_ART" :
                "COMBAT_ART";
            return _manifest.runtimeArtProfiles.FirstOrDefault(value =>
                value != null && StringComparer.Ordinal.Equals(value.artClass, preferredClass)) ??
                _manifest.runtimeArtProfiles.FirstOrDefault();
        }

        /// <summary>
        /// Resolves every one of the 120 first-hour Arts to its exact authored recipe
        /// while reusing the local 011 VFX/audio library. This is presentation only:
        /// it never selects an Art or changes forecast/resolution state.
        /// </summary>
        public static bool TryResolveExactFirstHourProfile076(
            string artId,
            BattleBeatFamily fallbackFamily,
            out BattleArtProfile011 profile)
        {
            EnsureLoaded();
            var key = Normalize(artId);
            if (_firstHourProfiles076.TryGetValue(key, out profile)) return true;
            if (!FirstHourArtPresentationResolver071.TryResolve(key, out var recipe))
            {
                profile = null;
                return false;
            }

            var artClass = FirstHourArtClass076(recipe.TreeId, fallbackFamily);
            var template = FirstHourTemplate076(key, recipe.TreeId, artClass);
            if (template == null)
            {
                profile = null;
                return false;
            }

            profile = CloneFirstHourProfile076(template, recipe, artClass);
            _firstHourProfiles076[key] = profile;
            return true;
        }

        public static bool TryResolveSemanticIcon076(
            string artId,
            BattleBeatFamily fallbackFamily,
            bool breakthroughOpportunity,
            out string assetId,
            out Sprite sprite)
        {
            assetId = breakthroughOpportunity
                ? "ICON_ACTION_BREAKTHROUGH"
                : ResolveProfile(artId, fallbackFamily)?.semanticIconAssetId;
            if (string.IsNullOrWhiteSpace(assetId))
                assetId = SemanticIconForFamily076(fallbackFamily);
            return TryResolveUiAsset(assetId, out _, out sprite);
        }

        public static bool TryResolveUiAsset(string assetId, out BattleArtUiAsset011 asset, out Sprite sprite)
        {
            EnsureLoaded();
            sprite = null;
            if (!_uiAssets.TryGetValue(Normalize(assetId), out asset) || asset == null ||
                string.IsNullOrWhiteSpace(asset.resourcePath)) return false;
            sprite = LoadSprite(asset.resourcePath);
            return sprite != null;
        }

        public static Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            if (SpriteCache.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                // First-open safety: the supplied AssetPostprocessor imports these as
                // Sprites, but a Texture2D fallback keeps local Resources usable while
                // Unity is still refreshing or when a .meta file was regenerated.
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    var pivot = resourcePath.IndexOf("/Characters/ENEMY_", StringComparison.Ordinal) >= 0
                        ? new Vector2(0.5f, 0.18f)
                        : resourcePath.IndexOf("/Characters/", StringComparison.Ordinal) >= 0
                            ? new Vector2(0.5f, 0.05f)
                            : new Vector2(0.5f, 0.5f);
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, 100f);
                    sprite.name = texture.name + " · Battle Art 011 Runtime Sprite";
                }
            }
            if (sprite != null) SpriteCache[resourcePath] = sprite;
            return sprite;
        }

        public static AudioClip LoadAudio(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            if (AudioCache.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null) AudioCache[resourcePath] = clip;
            return clip;
        }

        public static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return ColorUtility.TryParseHtmlString(value, out var parsed) ? parsed : fallback;
        }

        public static IReadOnlyList<string> ValidateRuntime()
        {
            EnsureLoaded();
            var issues = new List<string>();
            if (_manifest.law == null) issues.Add("BATTLE_ART_011_LAW_MISSING");
            else
            {
                if (_manifest.law.runtimeGenerativeAi) issues.Add("BATTLE_ART_011_RUNTIME_AI_FORBIDDEN");
                if (!_manifest.law.presentationNeverChangesResolution) issues.Add("BATTLE_ART_011_PRESENTATION_LAW_DRIFT");
                if (!_manifest.law.playerSelectsCompleteUnionForecast) issues.Add("BATTLE_ART_011_FORECAST_LAW_DRIFT");
                if (_manifest.law.memberActionClickable) issues.Add("BATTLE_ART_011_MEMBER_ACTION_CLICKABLE");
                if (_manifest.law.maximumAllyUnions != 10 || _manifest.law.maximumEnemyUnions != 10)
                    issues.Add("BATTLE_ART_011_20_UNION_CAPACITY_DRIFT");
            }
            if (_manifest.characters == null || _manifest.characters.Length != _manifest.characterCount)
                issues.Add("BATTLE_ART_011_CHARACTER_COUNT_DRIFT");
            else
            {
                var requiredPoses = new[]
                {
                    BattleArtPoseDirector011.Idle,
                    BattleArtPoseDirector011.Anticipation,
                    BattleArtPoseDirector011.ActionPrimary,
                    BattleArtPoseDirector011.RolePrimary,
                    BattleArtPoseDirector011.HitReaction,
                    BattleArtPoseDirector011.Recovery,
                    BattleArtPoseDirector011.Downed,
                    BattleArtPoseDirector011.Victory
                };
                foreach (var character in _manifest.characters)
                {
                    if (character == null || string.IsNullOrWhiteSpace(character.memberId))
                    {
                        issues.Add("BATTLE_ART_011_CHARACTER_ID_MISSING");
                        continue;
                    }
                    var poseIds = (character.poses ?? Array.Empty<BattleArtPose011>())
                        .Where(value => value != null)
                        .Select(value => Normalize(value.poseId))
                        .ToArray();
                    if (poseIds.Length != requiredPoses.Length)
                        issues.Add("BATTLE_ART_011_POSE_COUNT_" + character.memberId);
                    foreach (var requiredPose in requiredPoses)
                        if (!poseIds.Contains(Normalize(requiredPose)))
                            issues.Add("BATTLE_ART_011_POSE_MISSING_" + character.memberId + "_" + requiredPose);
                }
            }
            if (_manifest.vfx == null || _manifest.vfx.Length != _manifest.vfxCount)
                issues.Add("BATTLE_ART_011_VFX_COUNT_DRIFT");
            if (_manifest.audio == null || _manifest.audio.Length != _manifest.audioCount)
                issues.Add("BATTLE_ART_011_AUDIO_COUNT_DRIFT");
            if (_manifest.uiAssets == null || _manifest.uiAssets.Length != _manifest.uiAssetCount || _manifest.uiAssetCount != 21)
                issues.Add("BATTLE_ART_011_UI_ASSET_COUNT_DRIFT");
            if (_manifest.runtimeArtProfiles == null || _manifest.runtimeArtProfiles.Length != _manifest.runtimeArtProfileCount)
                issues.Add("BATTLE_ART_011_PROFILE_COUNT_DRIFT");
            if (_manifest.runtimeArtProfiles != null)
            {
                foreach (var profile in _manifest.runtimeArtProfiles)
                {
                    if (profile == null || string.IsNullOrWhiteSpace(profile.artId)) issues.Add("BATTLE_ART_011_PROFILE_ID_MISSING");
                    else if (profile.playerDirectlySelectableInStandard || profile.memberActionClickable)
                        issues.Add("BATTLE_ART_011_FORBIDDEN_DIRECT_ART_" + profile.artId);
                    else if (string.IsNullOrWhiteSpace(profile.startAudioResourcePath) ||
                             string.IsNullOrWhiteSpace(profile.impactAudioResourcePath))
                        issues.Add("BATTLE_ART_011_AUDIO_PHASE_MISSING_" + profile.artId);
                }
            }
            return issues.AsReadOnly();
        }

        private static void EnsureLoaded()
        {
            if (_manifest != null) return;
            var asset = Resources.Load<TextAsset>(ResourceManifestPath);
            if (asset == null) throw new InvalidOperationException("Battle Art 011 manifest is missing at Resources/" + ResourceManifestPath + ".json");
            _manifest = JsonConvert.DeserializeObject<BattleArtManifest011>(asset.text)
                ?? throw new InvalidOperationException("Battle Art 011 manifest could not be parsed.");
            _characters = (_manifest.characters ?? Array.Empty<BattleArtCharacter011>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.memberId))
                .ToDictionary(value => Normalize(value.memberId), value => value, StringComparer.Ordinal);
            _profiles = (_manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.artId))
                .ToDictionary(value => Normalize(value.artId), value => value, StringComparer.Ordinal);
            _uiAssets = (_manifest.uiAssets ?? Array.Empty<BattleArtUiAsset011>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.assetId))
                .ToDictionary(value => Normalize(value.assetId), value => value, StringComparer.Ordinal);
            _aliases = (_manifest.legacyAliases ?? Array.Empty<BattleArtAlias011>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.aliasArtId) && !string.IsNullOrWhiteSpace(value.runtimeArtId))
                .ToDictionary(value => Normalize(value.aliasArtId), value => Normalize(value.runtimeArtId), StringComparer.Ordinal);
            _firstHourProfiles076 = new Dictionary<string, BattleArtProfile011>(StringComparer.Ordinal);
        }

        private static BattleArtProfile011 FirstHourTemplate076(string artId, string treeId, string artClass)
        {
            if (_profiles.TryGetValue(artId, out var exact)) return exact;

            var normalizedTree = Normalize(treeId);
            if (normalizedTree.Contains("_WPN_"))
            {
                var family = "WEAPON_FAMILY_" + normalizedTree.Substring(normalizedTree.IndexOf("_WPN_", StringComparison.Ordinal) + 5);
                var weapon = _manifest.runtimeArtProfiles.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(Normalize(value.artClass), Normalize(artClass)) &&
                    StringComparer.Ordinal.Equals(Normalize(value.weaponFamilyId), family));
                if (weapon != null) return weapon;
            }
            if (normalizedTree.Contains("_MYS_"))
            {
                var school = normalizedTree.Substring(normalizedTree.IndexOf("_MYS_", StringComparison.Ordinal) + 5);
                var mystic = _manifest.runtimeArtProfiles.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(Normalize(value.artClass), Normalize(artClass)) &&
                    StringComparer.Ordinal.Equals(Normalize(value.schoolId), school));
                if (mystic != null) return mystic;
            }

            return _manifest.runtimeArtProfiles.FirstOrDefault(value => value != null &&
                       StringComparer.Ordinal.Equals(Normalize(value.artClass), Normalize(artClass))) ??
                   _manifest.runtimeArtProfiles.FirstOrDefault();
        }

        private static BattleArtProfile011 CloneFirstHourProfile076(
            BattleArtProfile011 source,
            ResolvedFirstHourArtPresentation071 recipe,
            string artClass)
        {
            var treeId = Normalize(recipe.TreeId);
            var weaponFamily = source.weaponFamilyId;
            var school = source.schoolId;
            if (treeId.Contains("_WPN_"))
            {
                weaponFamily = "WEAPON_FAMILY_" + treeId.Substring(treeId.IndexOf("_WPN_", StringComparison.Ordinal) + 5);
                school = string.Empty;
            }
            else if (treeId.Contains("_MYS_"))
            {
                weaponFamily = string.Empty;
                school = treeId.Substring(treeId.IndexOf("_MYS_", StringComparison.Ordinal) + 5);
            }

            return new BattleArtProfile011
            {
                artId = recipe.StableArtId,
                displayName = recipe.DisplayName,
                artClass = artClass,
                weaponFamilyId = weaponFamily,
                schoolId = school,
                animationFamilyId = source.animationFamilyId,
                motionProfile = recipe.MotionSignature,
                cameraProfileId = recipe.CameraSignature,
                trailResourcePath = source.trailResourcePath,
                projectileResourcePath = source.projectileResourcePath,
                impactResourcePath = source.impactResourcePath,
                windupAudioResourcePath = source.windupAudioResourcePath,
                audioResourcePath = source.audioResourcePath,
                startAudioResourcePath = source.startAudioResourcePath,
                impactAudioResourcePath = source.impactAudioResourcePath,
                fieldResourcePath = source.fieldResourcePath,
                primaryColor = FirstHourPrimaryColor076(treeId, source.primaryColor),
                secondaryColor = FirstHourSecondaryColor076(treeId, source.secondaryColor),
                glyph = source.glyph,
                hitStopMilliseconds = Math.Max(45, Math.Min(130, recipe.ImpactMilliseconds / 6)),
                screenShake = source.screenShake,
                requiresDedicatedActionPose = source.requiresDedicatedActionPose,
                returnToFormationRequired = true,
                playerDirectlySelectableInStandard = false,
                memberActionClickable = false,
                exactFirstHourRecipe = true,
                presentationRecipeId = recipe.ClipRecipeId,
                presentationTreeId = recipe.TreeId,
                presentationVfxSignature = recipe.VfxSignature,
                presentationSfxSignature = recipe.SfxSignature,
                presentationMotionSignature = recipe.MotionSignature,
                presentationDurationMilliseconds = recipe.DurationMilliseconds,
                presentationImpactMilliseconds = recipe.ImpactMilliseconds,
                semanticIconAssetId = SemanticIconForTree076(treeId)
            };
        }

        private static string FirstHourArtClass076(string treeId, BattleBeatFamily fallbackFamily)
        {
            var tree = Normalize(treeId);
            if (tree.Contains("_MYS_RESTORATION") || tree.Contains("_ROLE_FIELD_MEDIC"))
                return "RESTORATION_ART";
            if (tree.Contains("_MYS_WARDING") || tree.Contains("_ROLE_GUARDIAN") ||
                tree.Contains("_ROLE_PROVOCATION") || tree.Contains("_ROLE_FORMATION"))
                return "WARDING_ART";
            if (tree.Contains("_MYS_") || tree.Contains("_ROLE_RESONANCE"))
                return "MYSTIC_ATTACK";
            if (tree.Contains("_WPN_") || tree.Contains("_ROLE_"))
                return "COMBAT_ART";
            return fallbackFamily == BattleBeatFamily.Mystic || fallbackFamily == BattleBeatFamily.Invocation
                ? "MYSTIC_ATTACK"
                : fallbackFamily == BattleBeatFamily.Restoration || fallbackFamily == BattleBeatFamily.Recovery
                    ? "RESTORATION_ART"
                    : fallbackFamily == BattleBeatFamily.Guard || fallbackFamily == BattleBeatFamily.Formation
                        ? "WARDING_ART"
                        : "COMBAT_ART";
        }

        private static string SemanticIconForTree076(string treeId)
        {
            var tree = Normalize(treeId);
            if (tree.Contains("_MYS_RESTORATION") || tree.Contains("_ROLE_FIELD_MEDIC"))
                return "ICON_ACTION_RESTORATION";
            if (tree.Contains("_MYS_WARDING") || tree.Contains("_ROLE_GUARDIAN") ||
                tree.Contains("_ROLE_PROVOCATION") || tree.Contains("_ROLE_FORMATION"))
                return "ICON_ACTION_WARDING";
            if (tree.Contains("_MYS_")) return "ICON_ACTION_MYSTIC";
            if (tree.Contains("_ROLE_")) return "ICON_ACTION_TACTICAL";
            return "ICON_ACTION_COMBAT";
        }

        private static string SemanticIconForFamily076(BattleBeatFamily family)
        {
            if (family == BattleBeatFamily.Mystic || family == BattleBeatFamily.Invocation)
                return "ICON_ACTION_MYSTIC";
            if (family == BattleBeatFamily.Restoration || family == BattleBeatFamily.Recovery)
                return "ICON_ACTION_RESTORATION";
            if (family == BattleBeatFamily.Guard || family == BattleBeatFamily.Interception ||
                family == BattleBeatFamily.Formation)
                return "ICON_ACTION_WARDING";
            if (family == BattleBeatFamily.Tactical || family == BattleBeatFamily.Positioning)
                return "ICON_ACTION_TACTICAL";
            return "ICON_ACTION_COMBAT";
        }

        private static string FirstHourPrimaryColor076(string treeId, string fallback)
        {
            if (treeId.Contains("_ROLE_BREAKER")) return "#FF855C";
            if (treeId.Contains("_ROLE_COMMANDER")) return "#F3C86A";
            if (treeId.Contains("_ROLE_DUELIST")) return "#EDF1F7";
            if (treeId.Contains("_ROLE_FIELD_MEDIC")) return "#71E0A5";
            if (treeId.Contains("_ROLE_FORMATION")) return "#77B8FF";
            if (treeId.Contains("_ROLE_GUARDIAN")) return "#8FC9E8";
            if (treeId.Contains("_ROLE_PROVOCATION")) return "#FFB36B";
            if (treeId.Contains("_ROLE_RESONANCE")) return "#AE8CFF";
            if (treeId.Contains("_ROLE_SABOTEUR")) return "#E76B90";
            if (treeId.Contains("_ROLE_SCOUT")) return "#72D9C6";
            return fallback;
        }

        private static string FirstHourSecondaryColor076(string treeId, string fallback)
        {
            if (treeId.Contains("_ROLE_")) return "#F4E6C6";
            return fallback;
        }

        private static string Normalize(string value) =>
            (value ?? string.Empty).Trim().Replace(' ', '_').ToUpperInvariant();
    }
}
