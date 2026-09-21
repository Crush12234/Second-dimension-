using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only lookup for M1 art. Resource selection is stable for the same
    /// recruit identity, but it never changes campaign state or deterministic gameplay.
    /// </summary>
    public static class M1VisualAssets
    {
        public sealed class PortraitDescriptor
        {
            public string StableId { get; internal set; }
            public string ContentVersion { get; internal set; }
            public string SeedFingerprint { get; internal set; }
            public bool IsBespokeSignature { get; internal set; }
            public string SourceKind { get; internal set; }
            public string ResourceKey { get; internal set; }
            public IReadOnlyList<string> LayerIds { get; internal set; }
            public string DescriptorHash { get; internal set; }
        }

        public enum BackdropRole
        {
            SkyhomeTitle,
            SkyhomeHall,
            ApplicantBoard,
            GuildHallStage01,
            RecruitDossier,
            QuartermasterArmory,
            UnionStrategy
        }

        public const string ResourceRoot = "SecondDimension/Art";
        public const string PortraitRoot = ResourceRoot + "/Portraits";
        public const string FirstHourPortraitRoot076 = PortraitRoot + "/FirstHour076";
        public const string ApplicantPortraitRoot069 = PortraitRoot + "/Applicants069";
        public const string BackgroundRoot = ResourceRoot + "/Backgrounds";
        public const string BattleRoot = ResourceRoot + "/Battle";
        public const string FirstHourBattleRoot076 = BattleRoot + "/FirstHour076";
        public const string ChapterTwoBattleRoot079 = BattleRoot + "/ChapterTwo079";
        public const string WayglassUndercroftBattlePlateResourceKey079 =
            ChapterTwoBattleRoot079 + "/WAYGLASS_UNDERCROFT_BATTLE_PLATE_079";
        public const string WayglassDoorRescueArenaResourceKey080 =
            ChapterTwoBattleRoot079 + "/WAYGLASS_DOOR_RESCUE_ARENA_080";
        public const string EchoStalkerScoutStandeeResourceKey079 =
            ChapterTwoBattleRoot079 + "/ECHO_STALKER_SCOUT_079";
        public const string EchoStalkerVeilwardenStandeeResourceKey079 =
            ChapterTwoBattleRoot079 + "/ECHO_STALKER_VEILWARDEN_079";
        public const string ChaincallerLeaderStandeeResourceKey079 =
            ChapterTwoBattleRoot079 + "/CHAINCALLER_LEADER_079";
        public const string GuildmasterStandeeResourceKey076 =
            ResourceRoot + "/FirstHour076/Guildmaster/GUILDMASTER_STANDEE_076";
        public const string GuildmasterPoseResourceRoot076 =
            ResourceRoot + "/Battle011/Characters/GUILDMASTER_076";
        public const string FirstHourEnvironmentRoot071 =
            ResourceRoot + "/FirstHour071/Environments";
        public const string EquipmentRoot = ResourceRoot + "/Equipment";
        public const string EquipmentAtlasResourceKey = EquipmentRoot + "/EQUIPMENT_ICON_ATLAS_01";
        public const string PortraitContentVersion = "M1_PREMIUM_PORTRAIT_1.0";
        public const string HeroMasterSpriteFallbackRoot089 =
            "RUNTIME_HERO_SPRITE_FALLBACK_089";
        public const string TowerBattlePrefix081 = "ABYSS_BATTLE022_";
        public const string TowerBattleFloorPrefix081 = "ABYSS_BATTLE022_FLOOR_";

        private static readonly IReadOnlyDictionary<string, Vector2Int> EquipmentAtlasCells =
            new Dictionary<string, Vector2Int>(StringComparer.Ordinal)
            {
                { "SWORD", new Vector2Int(0, 0) },
                // The compact Armory composes this blade cell as crossed twin
                // blades. Keeping a distinct ID prevents an off-hand dagger from
                // falling through to the shield artwork.
                { "DAGGER", new Vector2Int(0, 0) },
                { "SPEAR", new Vector2Int(1, 0) },
                { "AXE", new Vector2Int(2, 0) },
                { "BOW", new Vector2Int(0, 1) },
                { "STAFF", new Vector2Int(1, 1) },
                { "SHIELD", new Vector2Int(2, 1) },
                { "ARMOR", new Vector2Int(0, 2) },
                { "ACCESSORY", new Vector2Int(1, 2) },
                { "REMEDY_KIT", new Vector2Int(2, 2) }
            };

        public static readonly IReadOnlyList<string> ModularLayerCategoryIds = new[]
        {
            "L01_BASE_RACE", "L02_BUILD", "L03_FACE", "L04_SKIN", "L05_EYES", "L06_BROWS",
            "L07_EARS_HORNS", "L08_HAIR_BACK", "L09_FACIAL_HAIR", "L10_MARKINGS", "L11_SCARS",
            "L12_OUTFIT", "L13_ARMOR", "L14_HAIR_FRONT", "L15_ACCESSORY", "L16_WEAPON",
            "L17_EXPRESSION", "L18_INJURY", "L19_TURNING_POINT", "L20_LEGEND", "L21_LIGHT",
            "L22_WORLD_FX"
        };

        private static readonly IReadOnlyDictionary<string, string[]> TutorialProceduralLayers =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                {
                    "PROC_36344E2400DC98B6", Layers(
                        "DOG_TRIBE", "AVERAGE_STURDY", "MATURE_LONG", "GRAY_FUR", "GRAY", "STEADY",
                        "MIXED_EARS", "SHAGGY_GRAY", "NONE", "NONE", "NONE", "TRAVELER_GUARDIAN",
                        "MEDIUM_WARD_COAT", "SHAGGY_FORELOCK", "GLOVES_CHAIN_STONE", "WARD_SABER_BUCKLER",
                        "COMPOSED", "NONE", "NONE", "NONE", "COOL_GATE", "SKYHOME_GLOW")
                },
                {
                    "PROC_F85A4CAA747BC8C6", Layers(
                        "DEMON_HERITAGE", "TALL_LOAD_BEARING", "ANGULAR_LONG", "DARK_SKIN", "RED", "PATIENT",
                        "SUBTLE_HORNS", "SHAVED_RED", "NONE", "WARD_LINES", "NONE", "PORTER_WARRIOR",
                        "MEDIUM_LOAD_HARNESS", "RED_STUBBLE", "SCARF_ROPE_TAG", "HOOKSTAFF_CARGO_BRACE",
                        "OPEN_PATIENT", "NONE", "NONE", "NONE", "WARM_RIM", "GATE_DUST")
                },
                {
                    "PROC_5B14E7816E55FFB5", Layers(
                        "DEMON_HERITAGE", "LIGHT_TRAIL", "HEART_TAPER", "PALE_GRAY", "GRAY", "ALERT",
                        "FINE_HORNS", "WAVY_WHITE", "NONE", "WARD_LINES", "CHEEK_SCAR", "TRAIL_MEDIC",
                        "LIGHT_WEATHER_COAT", "WAVY_FRINGE", "MAP_CANTEEN", "TRAVEL_SPEAR",
                        "WATCHFUL", "NONE", "NONE", "NONE", "OVERCAST_RIM", "TRAIL_MIST")
                },
                {
                    "PROC_748DD03A23E1FEB0", Layers(
                        "GOBLIN", "WIRY_COMPACT", "WEDGE_ROUND", "OLIVE_SKIN", "GREEN", "PRAGMATIC",
                        "NOTCHED_LONG_EAR", "CROPPED_WHITE", "NONE", "NONE", "NONE", "FIELD_PRIEST",
                        "LIGHT_HEALER_ROBES", "CROPPED_TUFT", "GLOVES_THREAD_VIAL", "ASH_STAFF_REMEDY_KIT",
                        "CAPABLE", "NONE", "NONE", "NONE", "WARM_CLINICAL", "ANNEX_DUST")
                }
            };

        private static readonly IReadOnlyDictionary<string, string> SignaturePortraitIds =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "SIG_W01_01", "PORTRAIT_SIGNATURE_01" },
                { "SIGREC_MAREN_HOLT", "PORTRAIT_SIGNATURE_01" },
                { "SIG_MAREN_HOLT", "PORTRAIT_SIGNATURE_01" },
                { "SIG_W01_02", "PORTRAIT_SIGNATURE_02" },
                { "SIGREC_BRAKKA_EMBERWALL", "PORTRAIT_SIGNATURE_02" },
                { "SIG_W01_03", "PORTRAIT_SIGNATURE_03" },
                { "SIGREC_ODELIA_FEN", "PORTRAIT_SIGNATURE_03" },
                { "SIG_ODELIA_FEN", "PORTRAIT_SIGNATURE_03" },
                { "SIG_W01_04", "PORTRAIT_SIGNATURE_04" },
                { "SIGREC_TOVVI_COPPERSPARK", "PORTRAIT_SIGNATURE_04" },
                { "SIG_W01_05", "PORTRAIT_SIGNATURE_05" },
                { "SIGREC_VEYRA_ASHGLASS", "PORTRAIT_SIGNATURE_05" },
                { "SIG_W01_06", "PORTRAIT_SIGNATURE_06" },
                { "SIGREC_RUSK_FENRUNNER", "PORTRAIT_SIGNATURE_06" },
                { "SIG_W01_07", "PORTRAIT_SIGNATURE_07" },
                { "SIGREC_TALA_STORMROAD", "PORTRAIT_SIGNATURE_07" },
                { "SIG_W01_08", "PORTRAIT_SIGNATURE_08" },
                { "SIGREC_ORREN_CLAY", "PORTRAIT_SIGNATURE_08" },
                { "SIG_W01_09", "PORTRAIT_SIGNATURE_09" },
                { "SIGREC_BESSA_BRASSWHISTLE", "PORTRAIT_SIGNATURE_09" },
                { "SIG_W01_10", "PORTRAIT_SIGNATURE_10" },
                { "SIGREC_VAELIS_NOCT", "PORTRAIT_SIGNATURE_10" },
                { "SIGREC_ANSEL_WINTERGLASS", "SIGREC_ANSEL_WINTERGLASS" }
            };

        // The rescued Lantern Patrol originally shipped as ten technically unique
        // files built from one red-scarf/bronze-uniform template. At roster-card
        // scale they read as duplicate faces. Release 076 gives each named member
        // a dedicated portrait whose race, age, silhouette, palette, role gear,
        // and expression remain recognizable without reading the label. These
        // keys deliberately precede the original masters, which remain untouched
        // as installation-safe fallbacks.
        private static readonly IReadOnlyDictionary<string, string> FirstHourPortraitResourceKeys076 =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "SIGREC_ZORIN_BRAMBLECROSS", FirstHourPortraitRoot076 + "/SIGREC_ZORIN_BRAMBLECROSS_PORTRAIT_076" },
                { "SIGREC_UNA_QUEENSREST", FirstHourPortraitRoot076 + "/SIGREC_UNA_QUEENSREST_PORTRAIT_076" },
                { "SIGREC_JUNIA_SKYWARD", FirstHourPortraitRoot076 + "/SIGREC_JUNIA_SKYWARD_PORTRAIT_076" },
                { "SIGREC_DAIN_DEEPWELL", FirstHourPortraitRoot076 + "/SIGREC_DAIN_DEEPWELL_PORTRAIT_076" },
                { "SIGREC_WILLOW_LONGSTRIDE", FirstHourPortraitRoot076 + "/SIGREC_WILLOW_LONGSTRIDE_PORTRAIT_076" },
                { "SIGREC_QUIN_LOWEN", FirstHourPortraitRoot076 + "/SIGREC_QUIN_LOWEN_PORTRAIT_076" },
                { "SIGREC_ASTER_MARSHLIGHT", FirstHourPortraitRoot076 + "/SIGREC_ASTER_MARSHLIGHT_PORTRAIT_076" },
                { "SIGREC_PETRA_RUNEBROOK", FirstHourPortraitRoot076 + "/SIGREC_PETRA_RUNEBROOK_PORTRAIT_076" },
                { "SIGREC_QUIN_CROWNHILL", FirstHourPortraitRoot076 + "/SIGREC_QUIN_CROWNHILL_PORTRAIT_076" },
                { "SIGREC_YVES_THORNFIELD", FirstHourPortraitRoot076 + "/SIGREC_YVES_THORNFIELD_PORTRAIT_076" }
            };

        // The Release 076 Lantern Patrol battle cutouts are identity-matched to the
        // studio portraits above. Resolve them before the original battle masters so
        // every rescued recruit keeps the same face, race, age, palette, role gear,
        // and silhouette from dossier to diorama. The original files stay available
        // as safe fallbacks if a versioned asset is absent from an installation.
        private static readonly IReadOnlyDictionary<string, string> FirstHourBattleStandeeResourceKeys076 =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "SIGREC_ZORIN_BRAMBLECROSS", FirstHourBattleRoot076 + "/STANDEE_SIGREC_ZORIN_BRAMBLECROSS_076" },
                { "SIGREC_UNA_QUEENSREST", FirstHourBattleRoot076 + "/STANDEE_SIGREC_UNA_QUEENSREST_076" },
                { "SIGREC_JUNIA_SKYWARD", FirstHourBattleRoot076 + "/STANDEE_SIGREC_JUNIA_SKYWARD_076" },
                { "SIGREC_DAIN_DEEPWELL", FirstHourBattleRoot076 + "/STANDEE_SIGREC_DAIN_DEEPWELL_076" },
                { "SIGREC_WILLOW_LONGSTRIDE", FirstHourBattleRoot076 + "/STANDEE_SIGREC_WILLOW_LONGSTRIDE_076" },
                { "SIGREC_QUIN_LOWEN", FirstHourBattleRoot076 + "/STANDEE_SIGREC_QUIN_LOWEN_076" },
                { "SIGREC_ASTER_MARSHLIGHT", FirstHourBattleRoot076 + "/STANDEE_SIGREC_ASTER_MARSHLIGHT_076" },
                { "SIGREC_PETRA_RUNEBROOK", FirstHourBattleRoot076 + "/STANDEE_SIGREC_PETRA_RUNEBROOK_076" },
                { "SIGREC_QUIN_CROWNHILL", FirstHourBattleRoot076 + "/STANDEE_SIGREC_QUIN_CROWNHILL_076" },
                { "SIGREC_YVES_THORNFIELD", FirstHourBattleRoot076 + "/STANDEE_SIGREC_YVES_THORNFIELD_076" }
            };

        private static readonly IReadOnlyDictionary<string, string> FirstHourBattleActionResourceKeys076 =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "SIGREC_ZORIN_BRAMBLECROSS", FirstHourBattleRoot076 + "/ACTION_SIGREC_ZORIN_BRAMBLECROSS_076" },
                { "SIGREC_UNA_QUEENSREST", FirstHourBattleRoot076 + "/ACTION_SIGREC_UNA_QUEENSREST_076" },
                { "SIGREC_JUNIA_SKYWARD", FirstHourBattleRoot076 + "/ACTION_SIGREC_JUNIA_SKYWARD_076" },
                { "SIGREC_DAIN_DEEPWELL", FirstHourBattleRoot076 + "/ACTION_SIGREC_DAIN_DEEPWELL_076" },
                { "SIGREC_WILLOW_LONGSTRIDE", FirstHourBattleRoot076 + "/ACTION_SIGREC_WILLOW_LONGSTRIDE_076" },
                { "SIGREC_QUIN_LOWEN", FirstHourBattleRoot076 + "/ACTION_SIGREC_QUIN_LOWEN_076" },
                { "SIGREC_ASTER_MARSHLIGHT", FirstHourBattleRoot076 + "/ACTION_SIGREC_ASTER_MARSHLIGHT_076" },
                { "SIGREC_PETRA_RUNEBROOK", FirstHourBattleRoot076 + "/ACTION_SIGREC_PETRA_RUNEBROOK_076" },
                { "SIGREC_QUIN_CROWNHILL", FirstHourBattleRoot076 + "/ACTION_SIGREC_QUIN_CROWNHILL_076" },
                { "SIGREC_YVES_THORNFIELD", FirstHourBattleRoot076 + "/ACTION_SIGREC_YVES_THORNFIELD_076" }
            };

        private static readonly Dictionary<string, Sprite> RuntimeSpriteCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static Dictionary<string, HeroMaster300Hero087> _acceptedHeroMasterByStableId089;
        private static bool _heroMasterCatalogLoadAttempted089;

        // Recurring applicants use a separate authored portrait pool. Keeping these
        // assets outside Portraits/Recruits prevents a generated applicant from ever
        // borrowing one of the six founder faces.
        private static readonly IReadOnlyList<string> ApplicantPortraitPool069 = new[]
        {
            ApplicantPortraitRoot069 + "/APP069_HUMAN_GUARDIAN",
            ApplicantPortraitRoot069 + "/APP069_ORC_WARRIOR",
            ApplicantPortraitRoot069 + "/APP069_GOBLIN_ROGUE",
            ApplicantPortraitRoot069 + "/APP069_DARKELF_MAGE",
            ApplicantPortraitRoot069 + "/APP069_DOG_RANGER",
            ApplicantPortraitRoot069 + "/APP069_BUNNY_PRIEST"
        };

        private static readonly IReadOnlyDictionary<string, string> ApplicantPortraitByRace069 =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "HUMAN", ApplicantPortraitRoot069 + "/APP069_HUMAN_GUARDIAN" },
                { "ORC", ApplicantPortraitRoot069 + "/APP069_ORC_WARRIOR" },
                { "GOBLIN", ApplicantPortraitRoot069 + "/APP069_GOBLIN_ROGUE" },
                { "DARK_ELF", ApplicantPortraitRoot069 + "/APP069_DARKELF_MAGE" },
                { "DOG_TRIBE", ApplicantPortraitRoot069 + "/APP069_DOG_RANGER" },
                { "BUNNY_TRIBE", ApplicantPortraitRoot069 + "/APP069_BUNNY_PRIEST" }
            };

        private static readonly IReadOnlyDictionary<string, string> EnemyBattleStandeeResourceKeys =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ENEMY_GATE_GNAWER_01", BattleRoot + "/ENEMY_GATE_GNAWER_A" },
                { "ENEMY_GATE_GNAWER_02", BattleRoot + "/ENEMY_GATE_GNAWER_SCOUT" },
                { "ENEMY_GATE_GNAWER_03", BattleRoot + "/ENEMY_GATE_GNAWER_BULWARK" },
                { "ENEMY_ECHO_STALKER_01", EchoStalkerScoutStandeeResourceKey079 },
                { "ENEMY_ECHO_STALKER_02", EchoStalkerVeilwardenStandeeResourceKey079 },
                { "ENEMY_CHAINCALLER_01", ChaincallerLeaderStandeeResourceKey079 }
            };

        private static readonly IReadOnlyDictionary<string, string> EnemyBattleActionResourceKeys =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ENEMY_GATE_GNAWER_01", BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_A" },
                { "ENEMY_GATE_GNAWER_02", BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_SCOUT" },
                { "ENEMY_GATE_GNAWER_03", BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_BULWARK" }
            };

        /// <summary>
        /// Creates a presentation-only, deterministic portrait descriptor. The four
        /// tutorial procedurals resolve to approved baked composites while preserving
        /// all 22 modular category slots for later kit expansion. Signature identities
        /// deliberately bypass the modular face stack.
        /// </summary>
        public static PortraitDescriptor BuildPortraitDescriptor(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId)
        {
            var recruit = NormalizeKey(recruitId);
            var authority = NormalizeKey(portraitAuthorityId);
            var stableId = FirstNonEmpty(authority, recruit, NormalizeKey(visualSeed), NormalizeRaceId(raceId));
            var signature = TrySignaturePortrait(authority, out _) || TrySignaturePortrait(recruit, out _);
            var seed = SemanticSeed.Derive(stableId, "PORTRAIT", PortraitContentVersion);

            string[] layers;
            if (signature)
            {
                layers = ModularLayerCategoryIds.Select(id => id + ":BESPOKE_MASTER").ToArray();
            }
            else if (TutorialProceduralLayers.TryGetValue(recruit, out var tutorialLayers))
            {
                layers = tutorialLayers;
            }
            else
            {
                layers = BuildFallbackLayers(stableId, raceId);
            }

            var resourceKey = PortraitResourceKeys(recruitId, visualSeed, raceId, portraitAuthorityId).FirstOrDefault() ?? string.Empty;
            var hashPayload = new
            {
                stableId,
                contentVersion = PortraitContentVersion,
                seed = seed.ToString(),
                signature,
                sourceKind = signature ? "BESPOKE_SIGNATURE" : "AUTHORED_MODULAR_COMPOSITE",
                resourceKey,
                layers
            };
            return new PortraitDescriptor
            {
                StableId = stableId,
                ContentVersion = PortraitContentVersion,
                SeedFingerprint = seed.ToString(),
                IsBespokeSignature = signature,
                SourceKind = signature ? "BESPOKE_SIGNATURE" : "AUTHORED_MODULAR_COMPOSITE",
                ResourceKey = resourceKey,
                LayerIds = layers,
                DescriptorHash = CanonicalJson.Sha256Hex(hashPayload)
            };
        }

        public static string BackdropResourceKey(BackdropRole role)
        {
            switch (role)
            {
                case BackdropRole.SkyhomeTitle:
                    return FirstHourEnvironmentRoot071 + "/SKYHOME_MARKET_GAMEPLAY_PLATE_071";
                case BackdropRole.SkyhomeHall:
                    return FirstHourEnvironmentRoot071 + "/GUILD_HALL_GAMEPLAY_PLATE_071";
                case BackdropRole.ApplicantBoard:
                    return BackgroundRoot + "/ANCHOR_03_APPLICANTS";
                case BackdropRole.GuildHallStage01:
                    return FirstHourEnvironmentRoot071 + "/GUILD_HALL_GAMEPLAY_PLATE_071";
                case BackdropRole.RecruitDossier:
                    return BackgroundRoot + "/BG_RECRUIT_DOSSIER_ALCOVE";
                case BackdropRole.QuartermasterArmory:
                    return BackgroundRoot + "/BG_QUARTERMASTER_ARMORY";
                case BackdropRole.UnionStrategy:
                    return BackgroundRoot + "/BG_UNION_STRATEGY_CHAMBER";
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static bool TryResolveBackdrop(BackdropRole role, out Sprite sprite, out string resourceKey)
        {
            resourceKey = BackdropResourceKey(role);
            sprite = LoadSprite(resourceKey);
            return sprite != null;
        }

        public static bool TryResolveBattleBackdrop(out Sprite sprite, out string resourceKey)
        {
            return TryResolveBattleBackdrop(string.Empty, out sprite, out resourceKey);
        }

        public static bool TryResolveBattleBackdrop(
            string battleId,
            out Sprite sprite,
            out string resourceKey)
        {
            var gatehouseResourceKey =
                FirstHourEnvironmentRoot071 + "/GATEHOUSE_BOSS_ARENA_071";
            if (TryResolveTowerBattleBackdropResourceKey081(battleId, out var towerResourceKey081))
                resourceKey = towerResourceKey081;
            else if (!string.IsNullOrWhiteSpace(battleId) &&
                battleId.IndexOf("ENCOUNTER_SURVEYOR_RESCUE", StringComparison.OrdinalIgnoreCase) >= 0)
                resourceKey = WayglassDoorRescueArenaResourceKey080;
            else if (!string.IsNullOrWhiteSpace(battleId) &&
                battleId.IndexOf("ENCOUNTER_FOG_STALKERS_STANDARD", StringComparison.OrdinalIgnoreCase) >= 0)
                resourceKey = WayglassUndercroftBattlePlateResourceKey079;
            else if (!string.IsNullOrWhiteSpace(battleId) &&
                battleId.IndexOf("HALL_BREACH", StringComparison.OrdinalIgnoreCase) >= 0)
                resourceKey = FirstHourEnvironmentRoot071 + "/GUILD_HALL_GAMEPLAY_PLATE_071";
            else if (!string.IsNullOrWhiteSpace(battleId) &&
                     battleId.IndexOf("LANTERN_ROAD_AMBUSH", StringComparison.OrdinalIgnoreCase) >= 0)
                resourceKey = FirstHourEnvironmentRoot071 + "/LANTERN_ROAD_GAMEPLAY_PLATE_071";
            else
                resourceKey = gatehouseResourceKey;

            sprite = LoadSprite(resourceKey);
            if (sprite != null) return true;

            // A missing encounter-specific plate must not prevent the certified
            // battle from opening. Fall through to the established boss arena,
            // tactical Gateworks painting, and Slice 019 arena in that order.
            if (!StringComparer.Ordinal.Equals(resourceKey, gatehouseResourceKey))
            {
                resourceKey = gatehouseResourceKey;
                sprite = LoadSprite(resourceKey);
                if (sprite != null) return true;
            }

            resourceKey = BattleRoot + "/BG_TACTICAL_GATEWORKS_020";
            sprite = LoadSprite(resourceKey);
            if (sprite != null) return true;

            // Preserve the complete Slice 019 arena as a runtime-safe fallback. A
            // missing optional 020 texture must never prevent the battle screen from
            // opening or alter any authoritative combat state.
            resourceKey = BattleRoot + "/BG_TUTORIAL_GATEWORKS_ARENA";
            sprite = LoadSprite(resourceKey);
            return sprite != null;
        }

        /// <summary>
        /// Selects the authored Abyss floor plate encoded by new Tower battle IDs.
        /// Older saves used an opaque ABYSS_BATTLE022 hash with no recoverable floor;
        /// those IDs intentionally receive the floor-one Tower plate instead of the
        /// unrelated Gatehouse boss arena so an in-progress legacy battle stays usable.
        /// </summary>
        public static bool TryResolveTowerBattleBackdropResourceKey081(
            string battleId,
            out string resourceKey)
        {
            resourceKey = string.Empty;
            if (string.IsNullOrWhiteSpace(battleId)) return false;

            var normalized = battleId.Trim();
            if (!normalized.StartsWith(TowerBattlePrefix081, StringComparison.OrdinalIgnoreCase))
                return false;

            var floorNumber = 1;
            if (normalized.StartsWith(TowerBattleFloorPrefix081, StringComparison.OrdinalIgnoreCase))
            {
                var start = TowerBattleFloorPrefix081.Length;
                if (normalized.Length >= start + 3 &&
                    normalized[start + 2] == '_' &&
                    char.IsDigit(normalized[start]) &&
                    char.IsDigit(normalized[start + 1]))
                {
                    var parsed = (normalized[start] - '0') * 10 + normalized[start + 1] - '0';
                    if (parsed >= 1 && parsed <= TowerRunRules081.OpeningFloorCount)
                        floorNumber = parsed;
                }
            }

            resourceKey = TowerRunRules081.ArtResourcePath(floorNumber);
            return true;
        }

        public static bool TryResolveEnemyBattleStandee(out Sprite sprite, out string resourceKey)
        {
            return TryResolveEnemyBattleStandee(string.Empty, out sprite, out resourceKey);
        }

        public static bool TryResolveEnemyBattleStandee(
            string memberId,
            out Sprite sprite,
            out string resourceKey)
        {
            var rawIdentity = NormalizeKey(memberId);
            var version70Spawn = rawIdentity.IndexOf("_SPAWN070_", StringComparison.Ordinal) > 0;
            var identity = EnemySourceIdentity070(rawIdentity);
            if (TryResolveChapterTwoEnemyBattleStandee079(identity, out sprite, out resourceKey))
                return true;
            if (string.IsNullOrWhiteSpace(identity))
                resourceKey = BattleRoot + "/ENEMY_GATE_GNAWER_A";
            else if (!EnemyBattleStandeeResourceKeys.TryGetValue(identity, out resourceKey))
            {
                if (version70Spawn)
                {
                    resourceKey = BattleRoot + "/STANDEE_" + identity;
                    sprite = LoadSprite(resourceKey);
                    if (sprite != null) return true;
                    resourceKey = BattleRoot + "/" + identity;
                    sprite = LoadSprite(resourceKey);
                    if (sprite != null) return true;
                    resourceKey = "RUNTIME_ENEMY_SILHOUETTE_070/" + identity;
                    sprite = EnemyFallbackSilhouette070(identity, resourceKey);
                    return sprite != null;
                }
                resourceKey = BattleRoot + "/ENEMY_GATE_GNAWER_A";
            }
            sprite = LoadSprite(resourceKey);
            return sprite != null;
        }

        /// <summary>
        /// Resolves the three canonical enemies used by The Lines Not Returned.
        /// The explicit identity gate is important: the 072 actor rig otherwise uses
        /// hash-selected Gate Gnawers as its generic recovery art. Spawn-suffixed
        /// authoritative member IDs are normalized back to their Pass 03 source IDs.
        /// </summary>
        public static bool TryResolveChapterTwoEnemyBattleStandee079(
            string memberId,
            out Sprite sprite,
            out string resourceKey)
        {
            var identity = EnemySourceIdentity070(NormalizeKey(memberId));
            if (!StringComparer.Ordinal.Equals(identity, "ENEMY_ECHO_STALKER_01") &&
                !StringComparer.Ordinal.Equals(identity, "ENEMY_ECHO_STALKER_02") &&
                !StringComparer.Ordinal.Equals(identity, "ENEMY_CHAINCALLER_01"))
            {
                sprite = null;
                resourceKey = string.Empty;
                return false;
            }

            resourceKey = EnemyBattleStandeeResourceKeys[identity];
            sprite = LoadSprite(resourceKey);
            return sprite != null;
        }

        private static Sprite EnemyFallbackSilhouette070(string identity, string resourceKey)
        {
            if (RuntimeSpriteCache.TryGetValue(resourceKey, out var cached) && cached != null)
                return cached;

            const int width = 96;
            const int height = 144;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = identity + " · Runtime Enemy Silhouette 070",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            var hash = StableHash(identity);
            var variant = EnemySilhouetteVariant070(identity, hash);
            var baseColor = Color.HSVToRGB(((hash >> 8) & 0xFFu) / 255f, 0.48f, 0.78f);
            var ink = new Color32(
                (byte)Mathf.RoundToInt(baseColor.r * 255f),
                (byte)Mathf.RoundToInt(baseColor.g * 255f),
                (byte)Mathf.RoundToInt(baseColor.b * 255f),
                255);
            var edge = new Color32(30, 24, 34, 255);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var inside = EnemySilhouettePixel070(variant, x, y, width, height);
                if (!inside) continue;
                var boundary = !EnemySilhouettePixel070(variant, x - 2, y, width, height) ||
                               !EnemySilhouettePixel070(variant, x + 2, y, width, height) ||
                               !EnemySilhouettePixel070(variant, x, y - 2, width, height) ||
                               !EnemySilhouettePixel070(variant, x, y + 2, width, height);
                pixels[y * width + x] = boundary ? edge : ink;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.04f), 32f);
            sprite.name = identity + " · Runtime Enemy Silhouette 070";
            RuntimeSpriteCache[resourceKey] = sprite;
            return sprite;
        }

        private static int EnemySilhouetteVariant070(string identity, uint hash)
        {
            if (identity.IndexOf("HOUND", StringComparison.Ordinal) >= 0) return 1;
            if (identity.IndexOf("SWARM", StringComparison.Ordinal) >= 0) return 2;
            if (identity.IndexOf("BRUTE", StringComparison.Ordinal) >= 0 ||
                identity.IndexOf("PACKLORD", StringComparison.Ordinal) >= 0) return 3;
            if (identity.IndexOf("CREEPER", StringComparison.Ordinal) >= 0 ||
                identity.IndexOf("MOLD", StringComparison.Ordinal) >= 0) return 4;
            if (identity.IndexOf("COLOSSUS", StringComparison.Ordinal) >= 0 ||
                identity.IndexOf("WARDEN", StringComparison.Ordinal) >= 0 ||
                identity.IndexOf("RAVEL", StringComparison.Ordinal) >= 0) return 5;
            return (int)(hash % 6u);
        }

        private static bool EnemySilhouettePixel070(
            int variant, int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            var nx = (x - width * 0.5f) / width;
            var ny = y / (float)height;
            switch (variant)
            {
                case 0: // agile biped
                    return Ellipse070(nx, ny, 0f, 0.66f, 0.13f, 0.23f) ||
                           Ellipse070(nx, ny, 0f, 0.91f, 0.10f, 0.09f) ||
                           (Mathf.Abs(nx) < 0.06f && ny > 0.16f && ny < 0.58f) ||
                           (Mathf.Abs(nx) > 0.06f && Mathf.Abs(nx) < 0.16f && ny > 0.08f && ny < 0.43f);
                case 1: // low hound
                    return Ellipse070(nx, ny, -0.03f, 0.44f, 0.28f, 0.16f) ||
                           Ellipse070(nx, ny, 0.29f, 0.56f, 0.13f, 0.13f) ||
                           (Mathf.Abs(nx + 0.18f) < 0.04f && ny > 0.09f && ny < 0.39f) ||
                           (Mathf.Abs(nx - 0.12f) < 0.04f && ny > 0.09f && ny < 0.39f);
                case 2: // shard swarm
                    return Ellipse070(nx, ny, -0.20f, 0.62f, 0.13f, 0.18f) ||
                           Ellipse070(nx, ny, 0.18f, 0.72f, 0.15f, 0.20f) ||
                           Ellipse070(nx, ny, 0f, 0.42f, 0.17f, 0.17f) ||
                           Ellipse070(nx, ny, 0.02f, 0.91f, 0.09f, 0.08f);
                case 3: // heavy brute
                    return Ellipse070(nx, ny, 0f, 0.58f, 0.29f, 0.30f) ||
                           Ellipse070(nx, ny, 0f, 0.91f, 0.14f, 0.11f) ||
                           (Mathf.Abs(nx) > 0.15f && Mathf.Abs(nx) < 0.27f && ny > 0.10f && ny < 0.52f) ||
                           (Mathf.Abs(nx) < 0.18f && ny > 0.05f && ny < 0.36f);
                case 4: // rift creeper
                    return Ellipse070(nx, ny, 0f, 0.63f, 0.24f, 0.27f) ||
                           Ellipse070(nx, ny, -0.16f, 0.91f, 0.08f, 0.08f) ||
                           Ellipse070(nx, ny, 0.16f, 0.91f, 0.08f, 0.08f) ||
                           (Mathf.Abs(nx + 0.24f) < 0.035f && ny > 0.06f && ny < 0.48f) ||
                           (Mathf.Abs(nx) < 0.035f && ny > 0.04f && ny < 0.42f) ||
                           (Mathf.Abs(nx - 0.24f) < 0.035f && ny > 0.06f && ny < 0.48f);
                default: // towering boss/warden
                    return Ellipse070(nx, ny, 0f, 0.58f, 0.25f, 0.34f) ||
                           Ellipse070(nx, ny, 0f, 0.94f, 0.16f, 0.10f) ||
                           (Mathf.Abs(nx) > 0.14f && Mathf.Abs(nx) < 0.25f && ny > 0.13f && ny < 0.68f) ||
                           (Mathf.Abs(nx) < 0.14f && ny > 0.03f && ny < 0.38f);
            }
        }

        private static bool Ellipse070(
            float x, float y, float centerX, float centerY, float radiusX, float radiusY)
        {
            var dx = (x - centerX) / radiusX;
            var dy = (y - centerY) / radiusY;
            return dx * dx + dy * dy <= 1f;
        }

        /// <summary>
        /// Resolves an authored reusable battle effect. These sprites are presentation-only;
        /// authoritative combat continues to return event logs without Unity dependencies.
        /// </summary>
        public static bool TryResolveBattleVfx(string effectId, out Sprite sprite, out string resourceKey)
        {
            switch (NormalizeKey(effectId))
            {
                case "WEAPON_ARC":
                    resourceKey = BattleRoot + "/VFX_WEAPON_ARC";
                    break;
                case "MYSTIC_BURST":
                    resourceKey = BattleRoot + "/VFX_MYSTIC_BURST";
                    break;
                case "RESTORATION_BLOOM":
                    resourceKey = BattleRoot + "/VFX_RESTORATION_BLOOM";
                    break;
                case "GUARD_IMPACT":
                    resourceKey = BattleRoot + "/VFX_GUARD_IMPACT";
                    break;
                default:
                    resourceKey = string.Empty;
                    sprite = null;
                    return false;
            }
            sprite = LoadSprite(resourceKey);
            return sprite != null;
        }

        /// <summary>
        /// Menu-only standing artwork. An existing identity-owned standee always
        /// wins; otherwise a procedural recruit without a separate authored portrait
        /// authority may use an explicitly compatible race-and-class cutout or portrait.
        /// Unmatched combinations retain the honest initials fallback.
        /// </summary>
        public static bool TryResolveMenuStandee090(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string roleIdentity,
            out Sprite sprite,
            out string resourceKey)
        {
            return TryResolveMenuStandee091(recruitId, visualSeed, raceId,
                portraitAuthorityId, roleIdentity, null, out sprite, out resourceKey);
        }

        public static bool TryResolveMenuStandee091(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string roleIdentity,
            string equipmentIdentity,
            out Sprite sprite,
            out string resourceKey)
        {
            if (TryResolveBattleStandee(
                    recruitId, visualSeed, raceId, portraitAuthorityId,
                    out sprite, out resourceKey))
                return true;

            resourceKey = string.Empty;
            sprite = null;
            var proceduralIdentity = NormalizeKey(recruitId);
            var portraitIdentity = NormalizeKey(portraitAuthorityId);
            // The production applicant/roster projections retain the recruit's own
            // PROC_* ID as their final portrait-authority fallback. That is still a
            // procedural identity, not a separate authored-art claim. Authored art
            // already won above; a different authority must remain unresolved here.
            if (!proceduralIdentity.StartsWith("PROC_", StringComparison.Ordinal) ||
                (!string.IsNullOrEmpty(portraitIdentity) &&
                 !StringComparer.Ordinal.Equals(portraitIdentity, proceduralIdentity)))
                return false;

            // Applicant cards supply CLASS_*; roster cards supply its humanized
            // name. Match only these exact roles, never broad mage/support or
            // rogue/ranger families that could put the wrong equipment on a hero.
            var role = NormalizeKey(roleIdentity);
            if (role.StartsWith("CLASS_", StringComparison.Ordinal))
                role = role.Substring("CLASS_".Length);
            var race = NormalizeRaceId(raceId);
            var equipment = NormalizeKey(equipmentIdentity);
            string proceduralKey;
            if (race == "DOG_TRIBE" && role == "ROGUE")
                proceduralKey = ResourceRoot + "/Standees/Procedural090/DOG_TRIBE_ROGUE_090";
            else if (race == "DEMON_HERITAGE" && role == "MAGE")
                proceduralKey = ResourceRoot + "/Standees/Procedural090/DEMON_HERITAGE_MAGE_090";
            else if (race == "DEMON_HERITAGE" && role == "WARRIOR" && equipment.Contains("HOOKSTAFF"))
                proceduralKey = ResourceRoot + "/Standees/Procedural091/DEMON_WARRIOR_HOOKSTAFF_091";
            else if (race == "DEMON_HERITAGE" && role == "WARRIOR" && equipment.Contains("AXE"))
                proceduralKey = ResourceRoot + "/Standees/Procedural091/DEMON_WARRIOR_AXE_091";
            else if (race == "DEMON_HERITAGE" && role == "RANGER" && equipment.Contains("BOW"))
                proceduralKey = ResourceRoot + "/Standees/Procedural091/DEMON_RANGER_BOW_091";
            else if (race == "GOBLIN" && role == "MAGE" && equipment.Contains("HORN"))
                proceduralKey = ResourceRoot + "/Standees/Procedural091/GOBLIN_MAGE_HORN_091";
            else if (race == "DARK_ELF" && role == "RANGER" && equipment.Contains("SPEAR"))
                proceduralKey = ResourceRoot + "/Standees/Procedural091/DARK_ELF_RANGER_SPEAR_091";
            else if (race == "HUMAN" && role == "MAGE" && equipment.Contains("HORN"))
                proceduralKey = ResourceRoot + "/Standees/Procedural091/HUMAN_MAGE_HORN_091";
            else
            {
                // These are the existing procedural portraits, never another named
                // hero's art. Keep the same stable race/role selection as dossiers.
                return TryResolveProceduralPortrait163(recruitId, visualSeed, raceId,
                    portraitAuthorityId, roleIdentity, out sprite, out resourceKey);
            }

            sprite = LoadSprite(proceduralKey);
            if (sprite == null) return false;
            sprite = M1SilhouetteFraming091.FrameResourceSprite091(sprite);
            resourceKey = proceduralKey;
            return true;
        }

        private static bool MenuPortraitRoleMatches163(string key, string role)
        {
            var name = NormalizeKey(key);
            switch (role)
            {
                case "GUARDIAN": return HasToken(name, "GUARDIAN");
                case "WARRIOR": return HasToken(name, "WARRIOR") || HasToken(name, "VANGUARD");
                case "RANGER": return HasToken(name, "RANGER");
                case "ROGUE": return HasToken(name, "ROGUE");
                case "MAGE": return HasToken(name, "MAGE") || HasToken(name, "STORM_MYSTIC");
                case "PRIEST": case "RESTORATION": case "HEALER":
                    return HasToken(name, "RESTORATION") || HasToken(name, "PRIEST") || HasToken(name, "HEALER");
                case "ENGINEER": return HasToken(name, "ENGINEER");
                default: return false;
            }
        }

        private static bool TryResolveProceduralPortrait163(string recruitId, string visualSeed,
            string raceId, string portraitAuthorityId, string roleIdentity, out Sprite sprite, out string key)
        {
            sprite = null; key = string.Empty;
            var id = NormalizeKey(recruitId); var authority = NormalizeKey(portraitAuthorityId);
            if (!id.StartsWith("PROC_", StringComparison.Ordinal) ||
                (!string.IsNullOrEmpty(authority) && authority != id)) return false;
            var race = NormalizeRaceId(raceId); var role = NormalizeKey(roleIdentity);
            if (role.StartsWith("CLASS_", StringComparison.Ordinal)) role = role.Substring(6);
            // These two cohorts already have matching pooled cutouts but no
            // matching portrait-pool entry. Reuse their own role before a race fallback.
            if ((race == "DOG_TRIBE" && role == "ROGUE") ||
                (race == "DEMON_HERITAGE" && role == "MAGE"))
            {
                key = ResourceRoot + "/Standees/Procedural090/" +
                    (race == "DOG_TRIBE" ? "DOG_TRIBE_ROGUE_090" : "DEMON_HERITAGE_MAGE_090");
                sprite = M1SilhouetteFraming091.FrameResourceSprite091(LoadSprite(key));
                return sprite != null;
            }
            if (race == "GOBLIN" && (role == "PRIEST" || role == "RESTORATION" || role == "HEALER"))
            {
                key = HeroArtRepair163.GoblinPriestResourceKey;
                sprite = HeroArtRepair163.Load(key);
                return sprite != null;
            }
            var folder = PortraitRoot + "/Races/" + race;
            var variants = Resources.LoadAll<Sprite>(folder).Where(s => s != null && MenuPortraitRoleMatches163(s.name, role))
                .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
            if (variants.Length == 0)
                variants = Resources.LoadAll<Texture2D>(folder).Where(t => t != null && MenuPortraitRoleMatches163(t.name, role))
                    .OrderBy(t => t.name, StringComparer.Ordinal).Select(t => RuntimeSprite(folder + "/" + t.name, t)).ToArray();
            if (variants.Length > 0)
            {
                sprite = variants[StableIndex(FirstNonEmpty(visualSeed, portraitAuthorityId, recruitId, race), variants.Length)];
                key = folder + "/" + sprite.name; return true;
            }
            if (ApplicantPortraitByRace069.TryGetValue(race, out var pooled) && MenuPortraitRoleMatches163(pooled, role))
            {
                sprite = LoadSprite(pooled); if (sprite != null) { key = pooled; return true; }
            }
            return HeroArtRepair163.TryProceduralPortrait(race, role, out sprite, out key);
        }

        public static bool TryResolveBattleStandee(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            out Sprite sprite,
            out string resourceKey)
        {
            if (!TryResolveBattleStandeeSource091(recruitId, visualSeed, raceId,
                    portraitAuthorityId, out sprite, out resourceKey)) return false;
            sprite = M1SilhouetteFraming091.FrameResourceSprite091(sprite);
            return sprite != null;
        }

        private static bool TryResolveBattleStandeeSource091(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            out Sprite sprite,
            out string resourceKey)
        {
            var titanHero161 = TitanArt161.HeroIdentity(recruitId, portraitAuthorityId);
            if (titanHero161 != null)
                return TitanArt161.TryHeroPose(titanHero161, false, out sprite, out resourceKey);
            if (TryGetSssHero090(
                    recruitId, portraitAuthorityId, visualSeed, out var sssHero090))
            {
                resourceKey = sssHero090.IdleArtResourcePath;
                sprite = LoadSprite(resourceKey);
                return sprite != null;
            }

            if (TryResolveHeroRemaster091(recruitId, visualSeed, portraitAuthorityId,
                    out sprite, out resourceKey)) return true;

            if (TryResolveFirstHourBattleAsset076(
                    recruitId,
                    visualSeed,
                    portraitAuthorityId,
                    FirstHourBattleStandeeResourceKeys076,
                    out sprite,
                    out resourceKey))
                return true;

            TryGetAcceptedHeroMaster089(
                portraitAuthorityId,
                recruitId,
                visualSeed,
                out var acceptedHero089);
            foreach (var portraitKey in PortraitResourceKeys(recruitId, visualSeed, raceId, portraitAuthorityId))
            {
                var slash = portraitKey.LastIndexOf('/');
                var identity = slash >= 0 ? portraitKey.Substring(slash + 1) : portraitKey;
                if (acceptedHero089 != null &&
                    !StringComparer.Ordinal.Equals(identity, acceptedHero089.StableId))
                    continue;
                resourceKey = BattleRoot + "/STANDEE_" + identity;
                sprite = LoadSprite(resourceKey);
                if (sprite != null) return true;
            }

            if (acceptedHero089 != null &&
                TryResolveHeroMasterSpriteFallback089(
                    acceptedHero089,
                    HeroMasterSpritePose089.Standing,
                    out sprite,
                    out resourceKey))
                return true;

            resourceKey = string.Empty;
            sprite = null;
            return false;
        }

        /// <summary>
        /// Resolves an optional authored action-pose variant for cinematic combat.
        /// It is presentation-only: a missing pose keeps the idle standee and never
        /// changes command legality, event order, damage, growth, or state hashes.
        /// </summary>
        public static bool TryResolveBattleActionPose(
            string memberId,
            out Sprite sprite,
            out string resourceKey)
        {
            return TryResolveBattleActionPose(
                memberId,
                string.Empty,
                string.Empty,
                string.Empty,
                out sprite,
                out resourceKey);
        }

        /// <summary>
        /// Resolves action art through the same permanent portrait authority used by
        /// portraits and standees. Signature recruits have campaign-instance member
        /// IDs (for example SIGI_...) while their authored art is keyed by the stable
        /// SIGREC identity, so memberId alone is not sufficient after recruitment.
        /// </summary>
        public static bool TryResolveBattleActionPose(
            string memberId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            out Sprite sprite,
            out string resourceKey)
        {
            if (!TryResolveBattleActionPoseSource091(memberId, visualSeed, raceId,
                    portraitAuthorityId, out sprite, out resourceKey)) return false;
            sprite = M1SilhouetteFraming091.FrameResourceSprite091(sprite);
            return sprite != null;
        }

        private static bool TryResolveBattleActionPoseSource091(
            string memberId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            out Sprite sprite,
            out string resourceKey)
        {
            var titanHero161 = TitanArt161.HeroIdentity(memberId, portraitAuthorityId);
            if (titanHero161 != null)
                return TitanArt161.TryHeroPose(titanHero161, true, out sprite, out resourceKey);
            if (TryGetSssHero090(
                    memberId, portraitAuthorityId, visualSeed, out var sssHero090))
            {
                resourceKey = sssHero090.AttackArtResourcePath;
                sprite = LoadSprite(resourceKey);
                return sprite != null;
            }

            // The new complete body also supports the existing animated action rig.
            // Never switch back to the damaged torso fragment during an attack.
            if (TryResolveHeroRemaster091(memberId, visualSeed, portraitAuthorityId,
                    out sprite, out resourceKey, true)) return true;

            var identity = EnemySourceIdentity070(NormalizeKey(memberId));
            if (identity.StartsWith("ENEMY_GATE_GNAWER", StringComparison.Ordinal))
            {
                if (!EnemyBattleActionResourceKeys.TryGetValue(identity, out resourceKey))
                    resourceKey = BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_A";
                sprite = LoadSprite(resourceKey);
                return sprite != null;
            }

            if (TryResolveFirstHourBattleAsset076(
                    memberId,
                    visualSeed,
                    portraitAuthorityId,
                    FirstHourBattleActionResourceKeys076,
                    out sprite,
                    out resourceKey))
                return true;

            TryGetAcceptedHeroMaster089(
                portraitAuthorityId,
                memberId,
                visualSeed,
                out var acceptedHero089);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var portraitKey in PortraitResourceKeys(
                         memberId, visualSeed, raceId, portraitAuthorityId))
            {
                var slash = portraitKey.LastIndexOf('/');
                var candidateIdentity = slash >= 0
                    ? portraitKey.Substring(slash + 1)
                    : portraitKey;
                if (string.IsNullOrWhiteSpace(candidateIdentity) || !seen.Add(candidateIdentity)) continue;
                if (acceptedHero089 != null &&
                    !StringComparer.Ordinal.Equals(candidateIdentity, acceptedHero089.StableId))
                    continue;
                resourceKey = BattleRoot + "/ACTION_" + candidateIdentity;
                sprite = LoadSprite(resourceKey);
                if (sprite != null) return true;
            }

            if (acceptedHero089 != null &&
                TryResolveHeroMasterSpriteFallback089(
                    acceptedHero089,
                    HeroMasterSpritePose089.Action,
                    out sprite,
                    out resourceKey))
                return true;

            resourceKey = string.Empty;
            sprite = null;
            return false;
        }

        private static bool TryResolveFirstHourBattleAsset076(
            string recruitId,
            string visualSeed,
            string portraitAuthorityId,
            IReadOnlyDictionary<string, string> versionedResourceKeys,
            out Sprite sprite,
            out string resourceKey)
        {
            var candidates = new[]
            {
                NormalizeKey(portraitAuthorityId),
                NormalizeKey(recruitId),
                NormalizeKey(visualSeed)
            };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !seen.Add(candidate) ||
                    !versionedResourceKeys.TryGetValue(candidate, out resourceKey))
                    continue;
                sprite = LoadSprite(resourceKey);
                if (sprite != null) return true;
            }

            resourceKey = string.Empty;
            sprite = null;
            return false;
        }

        private static string EnemySourceIdentity070(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity)) return string.Empty;
            var spawnMarker = identity.IndexOf("_SPAWN070_", StringComparison.Ordinal);
            if (spawnMarker > 0) return identity.Substring(0, spawnMarker);
            var packMarker = identity.IndexOf("_PACK_", StringComparison.Ordinal);
            return packMarker > 0 ? identity.Substring(0, packMarker) : identity;
        }

        /// <summary>
        /// Maps canonical item identity and tags to one neutral equipment picture.
        /// This selection is presentation-only and never enters equipment state.
        /// </summary>
        public static string EquipmentVisualId(
            string definitionId,
            string slotId,
            IReadOnlyList<string> equipmentTags)
        {
            var tokens = NormalizeKey(definitionId) + "_" + NormalizeKey(slotId);
            if (equipmentTags != null)
            {
                tokens += "_" + string.Join("_", equipmentTags.Select(NormalizeKey));
            }

            if (HasToken(tokens, "BOW")) return "BOW";
            if (HasToken(tokens, "SPEAR") || HasToken(tokens, "POLEARM") || HasToken(tokens, "LANCE")) return "SPEAR";
            if (HasToken(tokens, "AXE")) return "AXE";
            if (HasToken(tokens, "DAGGER") || HasToken(tokens, "TWIN_BLADE")) return "DAGGER";
            if (HasToken(tokens, "STAFF") || HasToken(tokens, "WAND") || HasToken(tokens, "FOCUS")) return "STAFF";
            if (HasToken(tokens, "SHIELD") || HasToken(tokens, "BUCKLER")) return "SHIELD";
            if (HasToken(tokens, "REMEDY") || HasToken(tokens, "HEAL") || HasToken(tokens, "MEDIC")) return "REMEDY_KIT";
            if (HasToken(tokens, "BODY") || HasToken(tokens, "ARMOR") || HasToken(tokens, "COAT")) return "ARMOR";
            if (HasToken(tokens, "ACCESSORY") || HasToken(tokens, "CHARM") || HasToken(tokens, "AMULET") || HasToken(tokens, "RING")) return "ACCESSORY";
            if (HasToken(tokens, "TOOL") || HasToken(tokens, "RELIC") || HasToken(tokens, "SATCHEL")) return "REMEDY_KIT";
            if (HasToken(tokens, "OFF_HAND")) return "SHIELD";
            return "SWORD";
        }

        /// <summary>
        /// Equipment-only owner presentation mapping. Canonical QualityId values stay
        /// intact in state/save data; the returned five-tier ID controls only live UI.
        /// </summary>
        public static string EquipmentRarityTierId(string qualityId)
        {
            switch (NormalizeKey(qualityId))
            {
                case "QUALITY_COMMON":
                case "QUALITY_UNCOMMON":
                case "QUALITY_BALANCED":
                case "QUALITY_REINFORCED":
                    return "COMMON";
                case "QUALITY_RARE":
                case "QUALITY_EPIC":
                case "QUALITY_MASTERWORK":
                case "QUALITY_RESONANT":
                case "QUALITY_STORIED":
                    return "RARE";
                case "QUALITY_LEGENDARY":
                case "QUALITY_CHOSEN":
                case "QUALITY_ABYSS_MARKED":
                case "QUALITY_EVOLVED":
                    return "LEGENDARY";
                case "QUALITY_GODLY":
                case "QUALITY_EDITED_RELIC":
                case "QUALITY_CREATOR_OMEGA":
                case "QUALITY_SSS_SIGNATURE":
                    return "GODLY";
                default:
                    return "BASIC";
            }
        }

        public static string EquipmentRarityDisplayName(string qualityId) =>
            // Color frames intentionally share tiers; their text must still
            // state the item's actual catalog quality.
            NormalizeKey(qualityId) == "QUALITY_UNCOMMON" ? "Uncommon" :
            NormalizeKey(qualityId) == "QUALITY_EPIC" ? "Epic" :
            NormalizeKey(qualityId) == "QUALITY_SSS_SIGNATURE"
                ? "SSS Signature"
                : EquipmentRarityTierId(qualityId) switch
            {
                "COMMON" => "Common",
                "RARE" => "Rare",
                "LEGENDARY" => "Legendary",
                "GODLY" => "Godly",
                _ => "Basic"
            };

        public static bool TryResolveEquipment(string equipmentVisualId, out Sprite sprite, out string resourceKey)
        {
            const string directResourcePrefix = "RESOURCE:";
            if (!string.IsNullOrWhiteSpace(equipmentVisualId) &&
                equipmentVisualId.StartsWith(directResourcePrefix, StringComparison.Ordinal))
            {
                resourceKey = equipmentVisualId.Substring(directResourcePrefix.Length);
                sprite = LoadSprite(resourceKey);
                return sprite != null;
            }

            var visualId = NormalizeKey(equipmentVisualId);
            resourceKey = EquipmentAtlasResourceKey + "#" + visualId;
            if (!EquipmentAtlasCells.TryGetValue(visualId, out var cell))
            {
                sprite = null;
                return false;
            }

            if (RuntimeSpriteCache.TryGetValue(resourceKey, out sprite) && sprite != null) return true;
            var texture = Resources.Load<Texture2D>(EquipmentAtlasResourceKey);
            if (texture == null || texture.width < 3 || texture.height < 3)
            {
                sprite = null;
                return false;
            }

            // A PNG without package-owned importer metadata may be resized by
            // Unity's default NPOT policy (for example 1254 -> 1024). Partition
            // the imported texture at rounded third-boundaries so all pixels are
            // covered and the neutral atlas remains usable at either resolution.
            var xMin = Mathf.RoundToInt(cell.x * texture.width / 3f);
            var xMax = Mathf.RoundToInt((cell.x + 1) * texture.width / 3f);
            var yMin = texture.height - Mathf.RoundToInt((cell.y + 1) * texture.height / 3f);
            var yMax = texture.height - Mathf.RoundToInt(cell.y * texture.height / 3f);
            var rect = new Rect(
                xMin,
                yMin,
                xMax - xMin,
                yMax - yMin);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "EQUIPMENT_" + visualId;
            RuntimeSpriteCache[resourceKey] = sprite;
            return true;
        }

        /// <summary>
        /// Resolves an exact Resources key without substituting an unrelated asset.
        /// LoadSprite also supports PNGs imported as Texture2D by constructing the
        /// runtime Sprite used elsewhere in this presentation registry.
        /// </summary>
        public static bool TryResolveExactResource090(
            string resourceKey,
            out Sprite sprite)
        {
            sprite = string.IsNullOrWhiteSpace(resourceKey)
                ? null
                : LoadSprite(resourceKey);
            return sprite != null;
        }

        private static bool TryGetSssHero090(
            string firstIdentity,
            string secondIdentity,
            string thirdIdentity,
            out SssTenV4HeroDefinition090 hero) =>
            SssTenV4Roster090.TryGet(firstIdentity, out hero) ||
            SssTenV4Roster090.TryGet(secondIdentity, out hero) ||
            SssTenV4Roster090.TryGet(thirdIdentity, out hero);

        /// <summary>
        /// Returns the exact-key candidates in lookup order. Race-folder variants are
        /// handled separately after these keys so a bespoke or seeded image always wins.
        /// </summary>
        public static IReadOnlyList<string> PortraitResourceKeys(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId)
        {
            var keys = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var authority = NormalizeKey(portraitAuthorityId);
            var recruit = NormalizeKey(recruitId);
            var seed = NormalizeKey(visualSeed);
            var race = NormalizeRaceId(raceId);

            if (TryGetSssHero090(
                    recruitId, portraitAuthorityId, visualSeed, out var sssHero090))
            {
                AddKey(keys, seen, string.Empty, sssHero090.PortraitArtResourcePath);
                return keys;
            }

            if (FirstHourPortraitResourceKeys076.TryGetValue(authority, out var firstHourPortrait076) ||
                FirstHourPortraitResourceKeys076.TryGetValue(recruit, out firstHourPortrait076))
            {
                AddKey(keys, seen, string.Empty, firstHourPortrait076);
            }

            AddKey(keys, seen, PortraitRoot + "/Recruits/", authority);
            AddKey(keys, seen, PortraitRoot + "/Recruits/", recruit);

            if (TrySignaturePortrait(authority, out var signaturePortrait) ||
                TrySignaturePortrait(recruit, out signaturePortrait))
            {
                AddKey(keys, seen, PortraitRoot + "/", signaturePortrait);
            }

            AddKey(keys, seen, PortraitRoot + "/Seeds/", seed);
            AddKey(keys, seen, PortraitRoot + "/Races/", race);
            return keys;
        }

        public static bool TryResolvePortrait(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            out Sprite sprite,
            out string resourceKey)
        {
            return TryResolvePortrait(
                recruitId,
                visualSeed,
                raceId,
                portraitAuthorityId,
                string.Empty,
                out sprite,
                out resourceKey);
        }

        public static bool TryResolvePortrait(
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string roleIdentity,
            out Sprite sprite,
            out string resourceKey)
        {
            var titanHero161 = TitanArt161.HeroIdentity(recruitId, portraitAuthorityId);
            if (titanHero161 != null)
                return TitanArt161.TryHeroPortrait(titanHero161, out sprite, out resourceKey);
            if (TryGetSssHero090(
                    recruitId, portraitAuthorityId, visualSeed, out var sssHero090))
            {
                resourceKey = sssHero090.PortraitArtResourcePath;
                sprite = LoadSprite(resourceKey);
                return sprite != null;
            }

            if (TryResolveHeroRemaster091(recruitId, visualSeed, portraitAuthorityId,
                    out sprite, out resourceKey)) return true;

            TryGetAcceptedHeroMaster089(
                portraitAuthorityId,
                recruitId,
                visualSeed,
                out var acceptedHeroMaster089);

            // Hero Master cards deliberately use the same full-body game piece as
            // combat. The nine accepted signature heroes have excellent close-up
            // portraits, but mixing those busts with the sprite-form master roster
            // made phone-sized recruit cards look like two unrelated art systems.
            // An exact same-identity standee wins here; heroes without one use the
            // explicitly labelled procedural dossier below. We never borrow another
            // recruit's portrait or imply that generated coverage is authored art.
            if (acceptedHeroMaster089 != null)
            {
                resourceKey = BattleRoot + "/STANDEE_" + acceptedHeroMaster089.StableId;
                sprite = M1SilhouetteFraming091.FrameResourceSprite091(LoadSprite(resourceKey));
                if (sprite != null) return true;

                if (!TryResolveHeroMasterSpriteFallback089(
                    acceptedHeroMaster089,
                    HeroMasterSpritePose089.Dossier,
                    out sprite,
                    out resourceKey)) return false;
                sprite = M1SilhouetteFraming091.FrameResourceSprite091(sprite);
                return sprite != null;
            }

            var exactKeys = PortraitResourceKeys(recruitId, visualSeed, raceId, portraitAuthorityId);
            for (var index = 0; index < exactKeys.Count; index++)
            {
                sprite = LoadSprite(exactKeys[index]);
                if (sprite != null)
                {
                    resourceKey = exactKeys[index];
                    return true;
                }
            }

            if (TryResolveProceduralPortrait163(recruitId, visualSeed, raceId, portraitAuthorityId,
                    roleIdentity, out sprite, out resourceKey)) return true;

            var race = NormalizeRaceId(raceId);
            if (!string.IsNullOrEmpty(race))
            {
                var raceFolder = PortraitRoot + "/Races/" + race;
                var variants = Resources.LoadAll<Sprite>(raceFolder)
                    .Where(value => value != null)
                    .OrderBy(value => value.name, StringComparer.Ordinal)
                    .ToArray();
                if (variants.Length == 0)
                {
                    variants = Resources.LoadAll<Texture2D>(raceFolder)
                        .Where(value => value != null)
                        .OrderBy(value => value.name, StringComparer.Ordinal)
                        .Select(value => RuntimeSprite(raceFolder + "/" + value.name, value))
                        .ToArray();
                }
                if (variants.Length > 0)
                {
                    var bestRoleScore = variants
                        .Select(value => ApplicantPortraitRoleScore086(value.name, roleIdentity))
                        .DefaultIfEmpty(0)
                        .Max();
                    if (bestRoleScore > 0)
                    {
                        variants = variants
                            .Where(value => ApplicantPortraitRoleScore086(value.name, roleIdentity) == bestRoleScore)
                            .ToArray();
                    }

                    var identity = FirstNonEmpty(visualSeed, portraitAuthorityId, recruitId, race);
                    var selected = StableIndex(identity, variants.Length);
                    sprite = variants[selected];
                    resourceKey = raceFolder + "/" + sprite.name;
                    return true;
                }
            }

            // Version 69 gives every recurring applicant a real character portrait.
            // Known races select their authored representative; future races select
            // a stable pool entry from identity, so save/reload never changes a face.
            // This runs before the legacy emblem fallback and never references the
            // founder portrait folder.
            var applicantIdentity069 = FirstNonEmpty(visualSeed, portraitAuthorityId, recruitId, race);
            if (!ApplicantPortraitByRace069.TryGetValue(race, out var applicantPortraitKey069))
            {
                applicantPortraitKey069 = ApplicantPortraitPool069[
                    StableIndex(applicantIdentity069, ApplicantPortraitPool069.Count)];
            }

            sprite = LoadSprite(applicantPortraitKey069);
            if (sprite != null)
            {
                resourceKey = applicantPortraitKey069;
                return true;
            }

            // Retain the Version 68 crests only as a compatibility fallback for an
            // incomplete installation where the Version 69 portrait pack is absent.
            var crestPack = RaceCrestPack068(race);
            if (!string.IsNullOrWhiteSpace(crestPack))
            {
                var identity = FirstNonEmpty(visualSeed, portraitAuthorityId, recruitId, race);
                var crestIndex = StableIndex(identity, 8) + 1;
                var crestKey = "SecondDimension/Campaign021/UI/Recruits/RECRUIT020_" +
                               crestPack + "_" + crestIndex.ToString("00", CultureInfo.InvariantCulture);
                sprite = LoadSprite(crestKey);
                if (sprite != null)
                {
                    resourceKey = crestKey;
                    return true;
                }
            }

            var guildSealKey = "SecondDimension/GuildCity017E/Contracts/SEAL_RESCUE";
            sprite = LoadSprite(guildSealKey);
            if (sprite != null)
            {
                resourceKey = guildSealKey;
                return true;
            }

            sprite = null;
            resourceKey = string.Empty;
            return false;
        }

        private static int ApplicantPortraitRoleScore086(string portraitName, string roleIdentity)
        {
            var portrait = NormalizeKey(portraitName);
            var role = NormalizeKey(roleIdentity);
            if (string.IsNullOrEmpty(portrait) || string.IsNullOrEmpty(role)) return 0;

            // Restoration/support must win before the broader Mystic/Mage family.
            if (HasToken(role, "RESTOR") || HasToken(role, "HEAL") || HasToken(role, "PRIEST") ||
                HasToken(role, "MEDIC") || HasToken(role, "CLERIC") || HasToken(role, "SUPPORT"))
            {
                return PortraitTokenScore086(portrait, "RESTORATION", "PRIEST", "HEALER", "MEDIC");
            }

            if (HasToken(role, "ENGINEER") || HasToken(role, "MECHANIC") || HasToken(role, "ARTIFICER") ||
                HasToken(role, "SMITH"))
            {
                return PortraitTokenScore086(portrait, "FIELD_ENGINEER", "ENGINEER");
            }

            switch (ClassColorDesignation(role))
            {
                case "GUARDIAN":
                    return PortraitTokenScore086(portrait, "SHIELD_GUARDIAN", "GUARDIAN", "SHIELD");
                case "WARRIOR":
                    return PortraitTokenScore086(portrait, "GREATSWORD_VANGUARD", "GLAIVE_WARRIOR", "VANGUARD", "WARRIOR", "GREATSWORD", "GLAIVE");
                case "RANGER":
                case "ROGUE":
                    return PortraitTokenScore086(portrait, "BOW_RANGER", "RANGER", "SCOUT", "GUNNER", "BOW");
                case "MAGE":
                    return PortraitTokenScore086(portrait, "AETHER_MAGE", "STORM_MYSTIC", "MAGE", "MYSTIC", "AETHER", "STORM", "HEXER");
                default:
                    return 0;
            }
        }

        private static int PortraitTokenScore086(string portrait, params string[] preferredTokens)
        {
            for (var index = 0; index < preferredTokens.Length; index++)
            {
                if (HasToken(portrait, preferredTokens[index]))
                    return preferredTokens.Length - index;
            }
            return 0;
        }

        private static string RaceCrestPack068(string race)
        {
            switch (NormalizeRaceId(race))
            {
                case "ORC": return "ORC";
                case "GOBLIN": return "GOBLIN";
                case "DOG_TRIBE": return "DOG";
                case "DARK_ELF": return "DARKELF";
                case "DEMON_HERITAGE": return "DEMON";
                case "BUNNY_TRIBE": return "BUNNY";
                case "BEAST_TRIBE": return "BEAST";
                default: return string.Empty;
            }
        }

        public static int StableIndex(string identity, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            return (int)(StableHash(identity ?? string.Empty) % (uint)count);
        }

        public static Color FallbackPortraitColor(string raceId, string visualSeed, string recruitId)
        {
            Color baseColor;
            switch (NormalizeRaceId(raceId))
            {
                case "ORC": baseColor = new Color(0.23f, 0.38f, 0.25f, 1f); break;
                case "GOBLIN": baseColor = new Color(0.35f, 0.43f, 0.19f, 1f); break;
                case "DOG_TRIBE": baseColor = new Color(0.43f, 0.30f, 0.20f, 1f); break;
                case "DARK_ELF": baseColor = new Color(0.24f, 0.20f, 0.39f, 1f); break;
                case "DEMON_HERITAGE": baseColor = new Color(0.42f, 0.18f, 0.22f, 1f); break;
                default: baseColor = new Color(0.19f, 0.31f, 0.43f, 1f); break;
            }

            var identity = FirstNonEmpty(visualSeed, recruitId, raceId);
            var variation = (StableHash(identity) & 0xFFu) / 255f;
            return Color.Lerp(baseColor, new Color(0.56f, 0.48f, 0.34f, 1f), 0.08f + variation * 0.14f);
        }

        public static string Initials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "?";
            var words = displayName.Split(new[] { ' ', '\t', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            var first = char.ToUpper(words[0][0], CultureInfo.InvariantCulture).ToString();
            if (words.Length == 1) return first;
            return first + char.ToUpper(words[words.Length - 1][0], CultureInfo.InvariantCulture);
        }

        public static string HumanizeRace(string raceId)
        {
            var normalized = NormalizeRaceId(raceId);
            if (string.IsNullOrEmpty(normalized)) return "RECRUIT";
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.Replace('_', ' ').ToLowerInvariant());
        }

        /// <summary>
        /// Class color identifies role only. It deliberately carries no recruit
        /// quality, potential, rarity, stat, or acquisition information.
        /// </summary>
        public static string ClassColorDesignation(string classIdentity)
        {
            var id = NormalizeKey(classIdentity);
            // VANGUARD contains the substring GUARD, so its explicit warrior
            // identities must be resolved before the broader Guardian token.
            if (HasToken(id, "VANGUARD") || HasToken(id, "GREATSWORD") ||
                HasToken(id, "GLAIVE"))
                return "WARRIOR";
            if (HasToken(id, "GUARD")) return "GUARDIAN";
            if (HasToken(id, "WARRIOR") || HasToken(id, "FIGHT") ||
                HasToken(id, "PORTER"))
                return "WARRIOR";
            if (HasToken(id, "RANGER") || HasToken(id, "ARCHER") || HasToken(id, "TRAIL") || HasToken(id, "GENERALIST")) return "RANGER";
            if (HasToken(id, "ROGUE") || HasToken(id, "SCOUT") || HasToken(id, "DUSK")) return "ROGUE";
            if (HasToken(id, "MAGE") || HasToken(id, "MYST") || HasToken(id, "RUNE") || HasToken(id, "SIGNAL")) return "MAGE";
            if (HasToken(id, "PRIEST") || HasToken(id, "HEAL") || HasToken(id, "MEDIC")) return "PRIEST";
            return "UNASSIGNED";
        }

        public static string ClassColorDisplayName(string classIdentity)
        {
            switch (ClassColorDesignation(classIdentity))
            {
                case "GUARDIAN": return "Sapphire Blue";
                case "WARRIOR": return "Crimson Red";
                case "RANGER": return "Emerald Green";
                case "ROGUE": return "Amber Orange";
                case "MAGE": return "Violet Purple";
                case "PRIEST": return "Ivory Gold";
                default: return "Neutral Silver";
            }
        }

        /// <summary>
        /// Reads a signed recruit's current opening base stat for presentation.
        /// It does not expose development potential, growth bias, or hidden traits.
        /// </summary>
        public static int OpeningBaseStatIndex(string canonicalApplicantJson, string statId)
        {
            if (string.IsNullOrWhiteSpace(canonicalApplicantJson) || string.IsNullOrWhiteSpace(statId)) return 0;
            try
            {
                var root = JObject.Parse(canonicalApplicantJson);
                return root["statTendencies"]?[NormalizeKey(statId)]?["baseIndex"]?.Value<int>() ?? 0;
            }
            catch (JsonException)
            {
                return 0;
            }
        }

        private static bool TrySignaturePortrait(string identity, out string portraitId)
        {
            if (!string.IsNullOrEmpty(identity) && SignaturePortraitIds.TryGetValue(identity, out portraitId))
            {
                return true;
            }
            portraitId = string.Empty;
            return false;
        }

        public static bool IsHeroMasterSpriteFallbackResourceKey089(string resourceKey089) =>
            !string.IsNullOrWhiteSpace(resourceKey089) &&
            resourceKey089.StartsWith(HeroMasterSpriteFallbackRoot089 + "/", StringComparison.Ordinal);

        private enum HeroMasterSpritePose089
        {
            Dossier,
            Standing,
            Action
        }

        private static bool TryResolveHeroRemaster091(
            string recruitId, string visualSeed, string portraitAuthorityId,
            out Sprite sprite, out string resourceKey, bool actionPose093 = false)
        {
            sprite = null;
            resourceKey = string.Empty;
            if (!TryGetAcceptedHeroMaster089(portraitAuthorityId, recruitId,
                    visualSeed, out var hero))
                return false;
            if (actionPose093 && HeroArtRepair163.TryFreyaAction(hero.StableId,
                    out sprite, out resourceKey)) return true;
            if (HeroRemasterAtlas093.TryResolve093(hero.StableId, actionPose093, out sprite, out resourceKey))
                return true;
            if (HeroRepair154.TryResolve(hero.StableId, actionPose093, out sprite, out resourceKey)) return true;
            if (hero.StableId != "HERO_REC_287") return false;
            resourceKey = ResourceRoot + "/Standees/HeroRemaster091/HERO_REC_287_IDLE_091";
            sprite = M1SilhouetteFraming091.FrameResourceSprite091(LoadSprite(resourceKey));
            return sprite != null;
        }

        private static bool TryGetAcceptedHeroMaster089(
            string primaryIdentity089,
            string secondaryIdentity089,
            string tertiaryIdentity089,
            out HeroMaster300Hero087 hero089)
        {
            if (!_heroMasterCatalogLoadAttempted089)
            {
                _heroMasterCatalogLoadAttempted089 = true;
                _acceptedHeroMasterByStableId089 =
                    new Dictionary<string, HeroMaster300Hero087>(StringComparer.Ordinal);
                try
                {
                    var source089 = Resources.Load<TextAsset>(
                        "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300");
                    if (source089 != null)
                    {
                        var catalog089 = HeroMaster300Catalog087.FromJson(source089.text);
                        foreach (var accepted089 in catalog089.AcceptedHeroes)
                            _acceptedHeroMasterByStableId089[accepted089.StableId] = accepted089;
                    }
                }
                catch (Exception)
                {
                    // Presentation recovery must never prevent a save from opening.
                    // Catalog tests retain the detailed validation failure surface.
                    _acceptedHeroMasterByStableId089.Clear();
                }
            }

            var candidates089 = new[]
            {
                NormalizeKey(primaryIdentity089),
                NormalizeKey(secondaryIdentity089),
                NormalizeKey(tertiaryIdentity089)
            };
            for (var index089 = 0; index089 < candidates089.Length; index089++)
            {
                if (!string.IsNullOrWhiteSpace(candidates089[index089]) &&
                    _acceptedHeroMasterByStableId089.TryGetValue(candidates089[index089], out hero089))
                    return true;
            }

            hero089 = null;
            return false;
        }

        private static bool TryResolveHeroMasterSpriteFallback089(
            HeroMaster300Hero087 hero089,
            HeroMasterSpritePose089 pose089,
            out Sprite sprite089,
            out string resourceKey089)
        {
            // Recovered exact-identity pairs are a final art fallback, never an
            // override of existing remasters, legacy art, or gameplay authority.
            if (HeroRecoveredSource100099.TryResolve099(hero089.StableId,
                    pose089 == HeroMasterSpritePose089.Action, out sprite089, out resourceKey089))
                return true;
            resourceKey089 = HeroMasterSpriteFallbackRoot089 + "/" + hero089.StableId + "/" +
                             pose089.ToString().ToUpperInvariant();
            if (RuntimeSpriteCache.TryGetValue(resourceKey089, out sprite089) && sprite089 != null)
                return true;

            if (pose089 == HeroMasterSpritePose089.Dossier)
            {
                if (!TryResolveHeroMasterSpriteFallback089(
                        hero089,
                        HeroMasterSpritePose089.Standing,
                        out var standing089,
                        out _) || standing089 == null || standing089.texture == null)
                {
                    sprite089 = null;
                    return false;
                }
                // Reuse the same framed identity and its cached alpha bounds.
                // A second FullRect sprite would lose those bounds in Windows,
                // where the generated texture no longer has CPU pixels.
                sprite089 = standing089;
                RuntimeSpriteCache[resourceKey089] = sprite089;
                return true;
            }

            const int width089 = 192;
            const int height089 = 256;
            var texture089 = new Texture2D(width089, height089, TextureFormat.RGBA32, false)
            {
                name = resourceKey089.Replace('/', '_'),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels089 = new Color32[width089 * height089];
            var hash089 = StableHash(hero089.StableId + "|" + hero089.Role + "|" + hero089.Weapon);
            var roleColor089 = HeroMasterFallbackRoleColor089(hero089.Role, hash089);
            var dark089 = LerpColor089(roleColor089, new Color32(10, 15, 25, 255), 0.62f);
            var light089 = LerpColor089(roleColor089, new Color32(245, 238, 211, 255), 0.34f);
            var skin089 = HeroMasterFallbackSkinColor089(hero089.Race, hash089);
            var accent089 = HeroMasterFallbackAccentColor089(hash089);
            var action089 = pose089 == HeroMasterSpritePose089.Action;

            // Large silhouette differences survive a 70-100 px phone thumbnail.
            // They are hash/race/role driven and remain honestly procedural: the
            // RUNTIME_* resource key is the authority, never a fabricated art ID.
            if ((hash089 & 1u) != 0u)
            {
                FillEllipse089(pixels089, width089, height089,
                    action089 ? 35 : 48, 69, action089 ? 144 : 145, 165,
                    LerpColor089(dark089, accent089, 0.18f));
            }

            // The battle rig already supplies a grounded contact shadow. Baking
            // one below the feet here made alpha framing ground the shadow instead
            // of the body, leaving these provisional standees visibly floating.

            var hipX089 = action089 ? 89 : 96;
            var hipY089 = action089 ? 89 : 83;
            DrawThickLine089(pixels089, width089, height089,
                hipX089 - 8, hipY089, action089 ? 50 : 73, 30, 13, dark089);
            DrawThickLine089(pixels089, width089, height089,
                hipX089 + 8, hipY089, action089 ? 128 : 114, 30, 13, dark089);
            DrawThickLine089(pixels089, width089, height089,
                action089 ? 50 : 73, 30, action089 ? 36 : 62, 23, 10, accent089);
            DrawThickLine089(pixels089, width089, height089,
                action089 ? 128 : 114, 30, action089 ? 143 : 125, 23, 10, accent089);

            FillEllipse089(pixels089, width089, height089,
                hipX089 - 35, 67, hipX089 + 35, 156, dark089);
            FillEllipse089(pixels089, width089, height089,
                hipX089 - 28, 76, hipX089 + 28, 151, roleColor089);
            FillRect089(pixels089, width089, height089,
                hipX089 - 24, 104, hipX089 + 24, 118, light089);
            FillRect089(pixels089, width089, height089,
                hipX089 - 32, 82, hipX089 + 32, 94, accent089);

            var trimVariant089 = (int)((hash089 >> 22) & 3u);
            if (trimVariant089 == 0 || trimVariant089 == 2)
                FillRect089(pixels089, width089, height089,
                    hipX089 - 5, 82, hipX089 + 5, 149, light089);
            if (trimVariant089 == 1 || trimVariant089 == 2)
                DrawThickLine089(pixels089, width089, height089,
                    hipX089 - 25, 91, hipX089 + 24, 143, 5, accent089);

            var shoulderY089 = 135;
            DrawThickLine089(pixels089, width089, height089,
                hipX089 - 23, shoulderY089,
                action089 ? 39 : hipX089 - 43,
                action089 ? 179 : 91,
                12, dark089);
            DrawThickLine089(pixels089, width089, height089,
                hipX089 + 23, shoulderY089,
                action089 ? 145 : hipX089 + 43,
                action089 ? 159 : 91,
                12, dark089);
            FillEllipse089(pixels089, width089, height089,
                hipX089 - 19, 151, hipX089 + 19, 193, skin089);
            FillEllipse089(pixels089, width089, height089,
                hipX089 - 22, 176, hipX089 + 22, 204, dark089);
            FillEllipse089(pixels089, width089, height089,
                hipX089 - 12, 164, hipX089 + 12, 187, skin089);
            FillRect089(pixels089, width089, height089,
                hipX089 - 8, 172, hipX089 - 3, 176, new Color32(28, 32, 44, 255));
            FillRect089(pixels089, width089, height089,
                hipX089 + 3, 172, hipX089 + 8, 176, new Color32(28, 32, 44, 255));

            DrawHeroMasterFallbackHeadgear089(
                pixels089, width089, height089, hipX089, hero089.Race, hash089, dark089, accent089);

            var weaponLength089 = 55 + (int)(hash089 % 28u);
            var weaponStartX089 = action089 ? 142 : hipX089 + 44;
            var weaponStartY089 = action089 ? 159 : 88;
            var weaponEndX089 = action089 ? 177 : hipX089 + 54;
            var weaponEndY089 = action089 ? 159 + weaponLength089 : 88 + weaponLength089;
            DrawThickLine089(pixels089, width089, height089,
                weaponStartX089, weaponStartY089, weaponEndX089, weaponEndY089, 6,
                new Color32(52, 43, 35, 255));
            DrawThickLine089(pixels089, width089, height089,
                weaponEndX089 - 9, weaponEndY089 - 7, weaponEndX089 + 8, weaponEndY089 + 10, 5,
                light089);

            // Stable-ID heraldry makes repeated fallback silhouettes easy to tell
            // apart while the explicit RUNTIME_* key keeps them honest about being
            // generated coverage rather than identity-authored source art.
            var heraldryCount089 = 1 + (int)(hash089 % 4u);
            for (var mark089 = 0; mark089 < heraldryCount089; mark089++)
            {
                var markX089 = hipX089 - 18 + mark089 * 12;
                FillEllipse089(pixels089, width089, height089,
                    markX089, 123 + mark089 % 2 * 9, markX089 + 8, 131 + mark089 % 2 * 9,
                    accent089);
            }

            DrawHeroMasterFallbackRoleProp089(
                pixels089, width089, height089, hipX089,
                ClassColorDesignation(hero089.Role), hash089, dark089, light089, accent089);

            texture089.SetPixels32(pixels089);
#if UNITY_EDITOR
            const bool discardCpuPixels093 = false;
#else
            const bool discardCpuPixels093 = true;
#endif
            sprite089 = FinalizeHeroFallbackTexture093(texture089, discardCpuPixels093);
            RuntimeSpriteCache[resourceKey089] = sprite089;
            return true;
        }

        // Measure once while pixels are available, then preserve that exact
        // silhouette after player upload. This fixes editor/player presentation
        // parity; it does not make the procedural placeholder authored artwork.
        private static Sprite FinalizeHeroFallbackTexture093(Texture2D texture, bool discardCpuPixels)
        {
            texture.Apply(false, false);
            var source = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.05f), 100f, 0u, SpriteMeshType.FullRect);
            source.name = texture.name;
            var framed = M1SilhouetteFraming091.FrameResourceSprite091(source);
            if (discardCpuPixels) texture.Apply(false, true);
            return framed;
        }

        private static void DrawHeroMasterFallbackHeadgear089(
            Color32[] pixels089,
            int width089,
            int height089,
            int centerX089,
            string race089,
            uint hash089,
            Color32 dark089,
            Color32 accent089)
        {
            var raceKey089 = NormalizeRaceId(race089);
            if (raceKey089.IndexOf("BEAST", StringComparison.Ordinal) >= 0 ||
                raceKey089.IndexOf("DOG", StringComparison.Ordinal) >= 0)
            {
                FillEllipse089(pixels089, width089, height089,
                    centerX089 - 27, 185, centerX089 - 10, 215, dark089);
                FillEllipse089(pixels089, width089, height089,
                    centerX089 + 10, 185, centerX089 + 27, 215, dark089);
                return;
            }

            if (raceKey089.IndexOf("DEMON", StringComparison.Ordinal) >= 0 ||
                raceKey089.IndexOf("ORC", StringComparison.Ordinal) >= 0 ||
                raceKey089.IndexOf("GOBLIN", StringComparison.Ordinal) >= 0)
            {
                DrawThickLine089(pixels089, width089, height089,
                    centerX089 - 13, 196, centerX089 - 27, 224, 7, accent089);
                DrawThickLine089(pixels089, width089, height089,
                    centerX089 + 13, 196, centerX089 + 27, 224, 7, accent089);
                return;
            }

            switch ((hash089 >> 25) & 3u)
            {
                case 0u:
                    FillRect089(pixels089, width089, height089,
                        centerX089 - 24, 190, centerX089 + 24, 200, accent089);
                    break;
                case 1u:
                    DrawThickLine089(pixels089, width089, height089,
                        centerX089, 198, centerX089 + 9, 226, 8, accent089);
                    break;
                case 2u:
                    FillEllipse089(pixels089, width089, height089,
                        centerX089 - 25, 184, centerX089 + 25, 211, dark089);
                    break;
            }
        }

        private static void DrawHeroMasterFallbackRoleProp089(
            Color32[] pixels089,
            int width089,
            int height089,
            int centerX089,
            string role089,
            uint hash089,
            Color32 dark089,
            Color32 light089,
            Color32 accent089)
        {
            switch (role089)
            {
                case "GUARDIAN":
                    FillEllipse089(pixels089, width089, height089,
                        centerX089 - 61, 76, centerX089 - 20, 139, dark089);
                    FillEllipse089(pixels089, width089, height089,
                        centerX089 - 55, 84, centerX089 - 26, 131, accent089);
                    break;
                case "RANGER":
                    DrawThickLine089(pixels089, width089, height089,
                        centerX089 - 52, 75, centerX089 - 63, 160, 5, light089);
                    DrawThickLine089(pixels089, width089, height089,
                        centerX089 - 63, 160, centerX089 - 42, 119, 3, accent089);
                    DrawThickLine089(pixels089, width089, height089,
                        centerX089 - 42, 119, centerX089 - 52, 75, 3, accent089);
                    break;
                case "MAGE":
                case "PRIEST":
                    FillEllipse089(pixels089, width089, height089,
                        centerX089 - 61, 132, centerX089 - 35, 158, accent089);
                    FillEllipse089(pixels089, width089, height089,
                        centerX089 - 54, 139, centerX089 - 42, 151, light089);
                    break;
                case "ROGUE":
                    DrawThickLine089(pixels089, width089, height089,
                        centerX089 - 48, 105, centerX089 - 67, 130, 5, light089);
                    DrawThickLine089(pixels089, width089, height089,
                        centerX089 + 45, 105, centerX089 + 65, 130, 5, light089);
                    break;
                default:
                    if ((hash089 & 4u) != 0u)
                        FillRect089(pixels089, width089, height089,
                            centerX089 - 48, 111, centerX089 - 36, 143, accent089);
                    break;
            }
        }

        private static Color32 HeroMasterFallbackRoleColor089(string role089, uint hash089)
        {
            switch (ClassColorDesignation(role089))
            {
                case "GUARDIAN": return new Color32(55, 116, 205, 255);
                case "WARRIOR": return new Color32(187, 62, 66, 255);
                case "RANGER": return new Color32(54, 151, 103, 255);
                case "ROGUE": return new Color32(210, 126, 45, 255);
                case "MAGE": return new Color32(119, 73, 190, 255);
                case "PRIEST": return new Color32(217, 188, 91, 255);
                default:
                    return new Color32(
                        (byte)(70 + hash089 % 120u),
                        (byte)(80 + (hash089 >> 8) % 110u),
                        (byte)(90 + (hash089 >> 16) % 120u),
                        255);
            }
        }

        private static Color32 HeroMasterFallbackSkinColor089(string race089, uint hash089)
        {
            var raceKey089 = NormalizeRaceId(race089);
            if (raceKey089.IndexOf("ORC", StringComparison.Ordinal) >= 0 ||
                raceKey089.IndexOf("GOBLIN", StringComparison.Ordinal) >= 0)
                return new Color32(126, 154, 91, 255);
            if (raceKey089.IndexOf("BEAST", StringComparison.Ordinal) >= 0 ||
                raceKey089.IndexOf("DOG", StringComparison.Ordinal) >= 0)
                return new Color32(166, 129, 91, 255);
            if (raceKey089.IndexOf("DEMON", StringComparison.Ordinal) >= 0)
                return new Color32(159, 89, 103, 255);
            return new Color32(
                (byte)(191 + hash089 % 35u),
                (byte)(145 + (hash089 >> 6) % 40u),
                (byte)(119 + (hash089 >> 12) % 40u),
                255);
        }

        private static Color32 HeroMasterFallbackAccentColor089(uint hash089) =>
            new Color32(
                (byte)(115 + (hash089 >> 3) % 140u),
                (byte)(105 + (hash089 >> 11) % 150u),
                (byte)(110 + (hash089 >> 19) % 145u),
                255);

        private static Color32 LerpColor089(Color32 from089, Color32 to089, float t089) =>
            new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(from089.r, to089.r, t089)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from089.g, to089.g, t089)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from089.b, to089.b, t089)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from089.a, to089.a, t089)));

        private static void FillRect089(
            Color32[] pixels089,
            int width089,
            int height089,
            int xMin089,
            int yMin089,
            int xMax089,
            int yMax089,
            Color32 color089)
        {
            xMin089 = Mathf.Clamp(xMin089, 0, width089 - 1);
            xMax089 = Mathf.Clamp(xMax089, 0, width089 - 1);
            yMin089 = Mathf.Clamp(yMin089, 0, height089 - 1);
            yMax089 = Mathf.Clamp(yMax089, 0, height089 - 1);
            for (var y089 = yMin089; y089 <= yMax089; y089++)
            for (var x089 = xMin089; x089 <= xMax089; x089++)
                pixels089[y089 * width089 + x089] = color089;
        }

        private static void FillEllipse089(
            Color32[] pixels089,
            int width089,
            int height089,
            int xMin089,
            int yMin089,
            int xMax089,
            int yMax089,
            Color32 color089)
        {
            var centerX089 = (xMin089 + xMax089) * 0.5f;
            var centerY089 = (yMin089 + yMax089) * 0.5f;
            var radiusX089 = Mathf.Max(1f, (xMax089 - xMin089) * 0.5f);
            var radiusY089 = Mathf.Max(1f, (yMax089 - yMin089) * 0.5f);
            for (var y089 = Mathf.Max(0, yMin089); y089 <= Mathf.Min(height089 - 1, yMax089); y089++)
            for (var x089 = Mathf.Max(0, xMin089); x089 <= Mathf.Min(width089 - 1, xMax089); x089++)
            {
                var normalizedX089 = (x089 - centerX089) / radiusX089;
                var normalizedY089 = (y089 - centerY089) / radiusY089;
                if (normalizedX089 * normalizedX089 + normalizedY089 * normalizedY089 <= 1f)
                    pixels089[y089 * width089 + x089] = color089;
            }
        }

        private static void DrawThickLine089(
            Color32[] pixels089,
            int width089,
            int height089,
            int xStart089,
            int yStart089,
            int xEnd089,
            int yEnd089,
            int thickness089,
            Color32 color089)
        {
            var steps089 = Mathf.Max(Mathf.Abs(xEnd089 - xStart089), Mathf.Abs(yEnd089 - yStart089));
            if (steps089 == 0) steps089 = 1;
            var radius089 = Mathf.Max(1, thickness089 / 2);
            for (var step089 = 0; step089 <= steps089; step089++)
            {
                var t089 = step089 / (float)steps089;
                var x089 = Mathf.RoundToInt(Mathf.Lerp(xStart089, xEnd089, t089));
                var y089 = Mathf.RoundToInt(Mathf.Lerp(yStart089, yEnd089, t089));
                FillEllipse089(
                    pixels089,
                    width089,
                    height089,
                    x089 - radius089,
                    y089 - radius089,
                    x089 + radius089,
                    y089 + radius089,
                    color089);
            }
        }

        private static Sprite LoadSprite(string resourceKey)
        {
            if (RuntimeSpriteCache.TryGetValue(resourceKey, out var cached) && cached != null) return cached;
            var importedSprite = Resources.Load<Sprite>(resourceKey);
            if (importedSprite != null)
            {
                if (IsPromotedHeroMasterSpriteResourceKey089(resourceKey))
                {
                    var framed089 = BoundsFitPromotedHeroMasterSprite089(resourceKey, importedSprite);
                    RuntimeSpriteCache[resourceKey] = framed089;
                    return framed089;
                }
                return importedSprite;
            }
            var texture = Resources.Load<Texture2D>(resourceKey);
            return texture == null ? null : RuntimeSprite(resourceKey, texture);
        }

        private static bool IsPromotedHeroMasterSpriteResourceKey089(string resourceKey089) =>
            resourceKey089.StartsWith(PortraitRoot + "/Recruits/HERO_REC_", StringComparison.Ordinal) ||
            resourceKey089.StartsWith(BattleRoot + "/STANDEE_HERO_REC_", StringComparison.Ordinal) ||
            resourceKey089.StartsWith(BattleRoot + "/ACTION_HERO_REC_", StringComparison.Ordinal);

        private static Sprite BoundsFitPromotedHeroMasterSprite089(
            string resourceKey089,
            Sprite importedSprite089)
        {
            var texture089 = importedSprite089.texture;
            if (texture089 == null || !texture089.isReadable) return importedSprite089;

            var pixels089 = texture089.GetPixels32();
            var width089 = texture089.width;
            var height089 = texture089.height;
            var minX089 = width089;
            var minY089 = height089;
            var maxX089 = -1;
            var maxY089 = -1;
            for (var y089 = 0; y089 < height089; y089++)
            for (var x089 = 0; x089 < width089; x089++)
            {
                if (pixels089[y089 * width089 + x089].a < 16) continue;
                minX089 = Mathf.Min(minX089, x089);
                minY089 = Mathf.Min(minY089, y089);
                maxX089 = Mathf.Max(maxX089, x089);
                maxY089 = Mathf.Max(maxY089, y089);
            }

            if (maxX089 < minX089 || maxY089 < minY089) return importedSprite089;
            var padding089 = Mathf.Max(4, Mathf.RoundToInt(Mathf.Max(
                maxX089 - minX089 + 1,
                maxY089 - minY089 + 1) * 0.055f));
            minX089 = Mathf.Max(0, minX089 - padding089);
            minY089 = Mathf.Max(0, minY089 - padding089);
            maxX089 = Mathf.Min(width089 - 1, maxX089 + padding089);
            maxY089 = Mathf.Min(height089 - 1, maxY089 + padding089);
            var rect089 = new Rect(
                minX089,
                minY089,
                maxX089 - minX089 + 1,
                maxY089 - minY089 + 1);
            var dossier089 = resourceKey089.StartsWith(
                PortraitRoot + "/Recruits/", StringComparison.Ordinal);
            var pivot089 = dossier089 ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.05f);
            var framed089 = Sprite.Create(
                texture089,
                rect089,
                pivot089,
                importedSprite089.pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            framed089.name = importedSprite089.name + "_BOUNDS_FIT_089";
            return framed089;
        }

        private static Sprite RuntimeSprite(string resourceKey, Texture2D texture)
        {
            if (RuntimeSpriteCache.TryGetValue(resourceKey, out var cached) && cached != null) return cached;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = texture.name;
            RuntimeSpriteCache[resourceKey] = sprite;
            return sprite;
        }

        private static void AddKey(List<string> keys, HashSet<string> seen, string prefix, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var key = prefix + value;
            if (seen.Add(key)) keys.Add(key);
        }

        private static string NormalizeRaceId(string value)
        {
            var normalized = NormalizeKey(value);
            return normalized.StartsWith("RACE_", StringComparison.Ordinal)
                ? normalized.Substring("RACE_".Length)
                : normalized;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var trimmed = value.Trim().ToUpperInvariant();
            var characters = new char[trimmed.Length];
            for (var index = 0; index < trimmed.Length; index++)
            {
                var character = trimmed[index];
                characters[index] = (character >= 'A' && character <= 'Z') ||
                                    (character >= '0' && character <= '9') ||
                                    character == '_' || character == '-'
                    ? character
                    : '_';
            }
            return new string(characters);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(values[index])) return values[index];
            }
            return string.Empty;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                for (var index = 0; index < value.Length; index++)
                {
                    var character = value[index];
                    hash ^= (byte)(character & 0xFF);
                    hash *= 16777619u;
                    hash ^= (byte)(character >> 8);
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private static bool HasToken(string tokens, string token) =>
            tokens.IndexOf(token, StringComparison.Ordinal) >= 0;

        private static string[] Layers(params string[] values)
        {
            if (values == null || values.Length != ModularLayerCategoryIds.Count)
            {
                throw new ArgumentException("A portrait descriptor must preserve all 22 modular layer categories.", nameof(values));
            }
            return ModularLayerCategoryIds.Select((id, index) => id + ":" + values[index]).ToArray();
        }

        private static string[] BuildFallbackLayers(string stableId, string raceId)
        {
            var rng = Pcg32.FromParts(stableId, "PORTRAIT", PortraitContentVersion);
            var face = new[] { "OVAL", "ROUND", "ANGULAR", "HEART" };
            var hair = new[] { "SHORT", "WAVY", "BRAIDED", "SHAGGY" };
            var outfit = new[] { "TRAVELER", "WARDEN", "HEALER", "PORTER" };
            var build = new[] { "LIGHT", "AVERAGE", "STURDY", "TALL" };
            var values = new[]
            {
                NormalizeRaceId(raceId), build[(int)rng.NextBounded((uint)build.Length)], face[(int)rng.NextBounded((uint)face.Length)],
                "PALETTE_" + rng.NextBounded(8u), "EYES_" + rng.NextBounded(8u), "BROWS_" + rng.NextBounded(6u),
                "COMPATIBLE_" + rng.NextBounded(6u), hair[(int)rng.NextBounded((uint)hair.Length)] + "_BACK", "NONE",
                "MARK_" + rng.NextBounded(6u), "NONE", outfit[(int)rng.NextBounded((uint)outfit.Length)], "ARMOR_" + rng.NextBounded(5u),
                hair[(int)rng.NextBounded((uint)hair.Length)] + "_FRONT", "ACCESSORY_" + rng.NextBounded(8u), "WEAPON_" + rng.NextBounded(8u),
                "NEUTRAL", "NONE", "NONE", "NONE", "LIGHT_" + rng.NextBounded(5u), "WORLD_" + rng.NextBounded(5u)
            };
            return Layers(values);
        }
    }
}
