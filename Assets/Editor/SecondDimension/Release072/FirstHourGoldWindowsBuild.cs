#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.Release072
{
    /// <summary>
    /// Canonical Windows delivery authority for the Release 090 card-quest polish slice.
    /// The output remains fixed to the owner-requested short path and is rebuilt from
    /// an empty directory only after the shipping scene and smoke contract are proven.
    /// </summary>
    public static class FirstHourGoldWindowsBuild
    {
        public const string BuildId = "SECOND-DIMENSION-ALPHA-132";
        public const string BuildLabel = "ALPHA 132";
        public const string PlayerVersion = "0.132.0-alpha";
        // Compatibility schema retained while Release 090 expands the playable quest loop.
        // Build identity is versioned independently above.
        public const string SmokeReportSchema = "SECOND_DIMENSION_FIRST_HOUR_GOLD_SMOKE_084_1";
        public const string SmokeReportFileName = "FIRST_HOUR_GOLD_SMOKE_084.json";
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string ExecutableName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        private const string BuildCompleteFileName = "FIRST_HOUR_GOLD_BUILD_COMPLETE.txt";
        private const string SmokeCommandLineFlag = "--sd-first-hour-gold-smoke";
        private const int SmokeExpectedScreenshotCount = 75;
        private const int SmokeExpectedGateCount = 58;
        private const string BuildPassMarker =
            "SECOND DIMENSION ALPHA 132 WINDOWS BUILD PASS";

        private static readonly string[] Campaign022RegistryResourceFiles =
        {
            "Assets/Resources/SecondDimension/Campaign022/Data/CampaignManifest022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/WeaponEvolutionTracks022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/ArmorEvolutionRecipes022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/AdvancedClassCertifications022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/AbyssFloors022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/AbyssOperations022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/EndlessBattleOperations094.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/InvocationArtifacts022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/InvocationAffixes022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/ArtifactEvolutionPaths022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/SummonEchoes022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/GreatCovenants022.json",
            "Assets/Resources/SecondDimension/Campaign022/Data/EndgameUnlocks022.json"
        };

        private static readonly string[] TowerFloorArtResourcePaths =
        {
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_01_MUD_TRENCHES_BATTLE_083",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_02_ARROW_RAIN_FIELD_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_03_SILENT_CAMP_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_04_COLLAPSED_SIEGE_WALL_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_05_GRAVE_BANNER_HILL_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_06_IRON_MARCH_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_07_BLOODLESS_RIVER_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_08_WAR_BEAST_PENS_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_09_ASH_COMMAND_TENT_BATTLE_084",
            "SecondDimension/Art/Campaign083/ABYSS_FLOOR_10_AEGIS_GATE_BATTLE_084"
        };

        private static readonly string[] TowerProductionArtAssetPaths084 =
            TowerFloorArtResourcePaths.Select(value =>
                "Assets/Resources/" + value + ".png").ToArray();
        private static readonly string[] TowerProductionArtSha256084 =
        {
            "0EEC14FC782F262B98578BEF3B41C17A3A585400DCBB20A6E374F1D28582E633",
            "BE86B18582B038152B4D33857AA781DEADC7A2BB58FC7557C247EBE68E30325A",
            "4D5D4F05F7A20D5774FB59672C073FEFF712466CA6576DA52614DFF33E021A44",
            "5D981B56D29362F8F5A3A4340F1F3E1AC833A8875B538A8E7A3C0237B865AB0B",
            "713F396CC1D27D6F8B4509FEF553DAC39F9B62A1849CA0A22CA5DBD4C79B0C09",
            "57E82E5A65413317B431C2F78B96E06E9E8E1A08EF74CE55BAE1C97411231243",
            "7A7806293B3774421DD77BF84CF77B8AD12D20D52AF704D7C25EBF69753D83AF",
            "19373D37019C2C27FC23537D796608CA67D9C7ED0F793FEFA33D6ADFE4E9E427",
            "1345BD9696073351D4A6118145D12E41F7A2D5C42A16E5CD757E46427D225F4B",
            "3ECDB0FB09B997D3E711BA936A4DE16AF5D59FF76204E36ADC09C3AAA6FC7B8B"
        };
        private static readonly string[] PreservedTowerLegacyArtAssetPaths084 =
        {
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_01_Mud_Trenches.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_02_Arrow_Rain_Field.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_03_Silent_Camp.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_04_Collapsed_Siege_Wall.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_05_Grave_Banner_Hill.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_06_Iron_March.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_07_Bloodless_River.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_08_War_Beast_Pens.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_09_Ash_Command_Tent.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_10_Aegis_Gate.jpg"
        };
        private static readonly string[] PreservedTowerLegacyArtSha256084 =
        {
            "2B708EEBE18731EF5004E4EE81ED9459213EC84672855A749C7BCC14D76A0BBE",
            "DEE5F07E8CECFD11A03F46E49C71E1D9B66146ED6DA722545F1B5103ABABC88F",
            "33E43357023227B3A6E1F9ACE160D5FE43803321CBB97C5526736E25CDD2CD7F",
            "84448779C7B660E43067F75A2ADE4294C589A1FDC44979CFC72CBA08978CC971",
            "8D2674FDE7BB95F2F60083FF3996279E3F2340378E38848EA2C6FF620F5AB0BA",
            "423E8974BD5B3F18483445CC68B0C76CB9BB4E8B4C587EC8FFB777941FA8F801",
            "50920CB062AA10310DF4596433BD49B3B163FB6126C65CD407FDD04CBF4B4CDF",
            "3290324C880AABB73B78B4A52939BC15EE806C7B724528E6FEE5C4CD89AEB928",
            "1E943D75D17E00E6A221C02FECDB10CDD3F9172FEA4D6CF295ADFB8D6CFC108F",
            "F2B353E37AE3A8ED034F13610E965E30EC5410FF75434F9C81982542DE6923A9"
        };

        private static readonly string[] TowerVisual083CertificationFiles =
        {
            "Assets/SecondDimension/Presentation/Battle/M1FlowPresenter.Invocation022.cs",
            "Assets/SecondDimension/Presentation/Battle/M2FullScreenUnionCommandStage.cs",
            "Assets/SecondDimension/Presentation/Battle3D/M2Battle3DWorld.cs",
            "Assets/SecondDimension/Gameplay/Campaign022/TowerAdventureRules084.cs",
            "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionCommandService022.cs",
            "Assets/SecondDimension/Gameplay/Campaign020/CampaignAdventureRules084.cs",
            "Assets/SecondDimension/Gameplay/Campaign020/CampaignPlayableModels020.cs",
            "Assets/SecondDimension/Gameplay/Campaign020/CampaignPlayableCommandService020.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/BoardAdventureRules084.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/CampaignWorldGateModels023.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/CampaignWorldGateCommandService023.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.BoardAdventureAnimation084.cs",
            "Assets/SecondDimension/Presentation/Campaign022/Campaign022PresentationContracts.cs",
            "Assets/SecondDimension/Presentation/Campaign022/GuildCityFlowPresenter022.cs",
            "Assets/SecondDimension/Presentation/Campaign020/GuildCityCampaignAdventure084.cs",
            "Assets/SecondDimension/Presentation/Campaign023/GuildCityAdventureBoard084.cs",
            "Assets/SecondDimension/Presentation/Campaign023/AdventureBoardNarrativeProjection084.cs",
            "Assets/SecondDimension/Presentation/Campaign023/AdventureBoardTrackProjection084.cs",
            "Assets/Resources/SecondDimension/Art/Campaign083/TOWER_PLATES_083_084_PROVENANCE.md",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_01_MUD_TRENCHES_BATTLE_083.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_02_ARROW_RAIN_FIELD_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_03_SILENT_CAMP_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_04_COLLAPSED_SIEGE_WALL_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_05_GRAVE_BANNER_HILL_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_06_IRON_MARCH_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_07_BLOODLESS_RIVER_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_08_WAR_BEAST_PENS_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_09_ASH_COMMAND_TENT_BATTLE_084.png",
            "Assets/Resources/SecondDimension/Art/Campaign083/ABYSS_FLOOR_10_AEGIS_GATE_BATTLE_084.png",
            "Assets/Tests/EditMode/BoardAdventure084Tests.cs",
            "Assets/Tests/EditMode/BoardAdventure084AuthorityTests.cs",
            "Assets/Tests/EditMode/BoardAdventure084EndToEndTests.cs",
            "Assets/Tests/EditMode/CampaignProgressionAbyssCovenant022Tests.cs",
            "Assets/Tests/EditMode/TowerRun081EditModeTests.cs",
            "Assets/Tests/PlayMode/BoardAdventure084PlayModeTests.cs",
            "Assets/Tests/PlayMode/TowerBattleOnly088Tests.cs",
            "Assets/Tests/PlayMode/TowerRun081PlayModeTests.cs"
        };

        private static readonly string[] CreatorCodeRuntimeFiles =
        {
            "Assets/SecondDimension/Gameplay/Creator028/CreatorAccessRules028.cs",
            "Assets/SecondDimension/Gameplay/Creator028/CreatorAccessModels028.cs",
            "Assets/SecondDimension/Gameplay/Creator028/CreatorAccessCommandService028.cs",
            "Assets/SecondDimension/Gameplay/Creator028/CreatorRecruitProjection028.cs",
            "Assets/SecondDimension/Presentation/Creator028/CreatorRegistry028.cs",
            "Assets/SecondDimension/Presentation/Creator028/CreatorPresentationContracts028.cs",
            "Assets/SecondDimension/Presentation/Creator028/GuildCityFlowPresenter028.cs",
            "Assets/SecondDimension/Presentation/Creator028/M1RuntimeCoordinator.Creator028.cs",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorManifest028.json",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorCodeCatalog028.json",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorResourceCatalog028.json",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorCharacterInvitations028.json",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorModifiedWeapons028.json",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorContentUnlocks028.json",
            "Assets/Resources/SecondDimension/Creator028/Data/CreatorRoomCatalog028.json"
        };

        private static readonly string[] CreatorGiveaway10000SourceFiles =
        {
            "Assets/SecondDimension/Gameplay/Creator028/CreatorGiveawayRules10000.cs",
            "Assets/SecondDimension/Gameplay/Creator028/CreatorGiveawayCommandService10000.cs",
            "Assets/SecondDimension/Presentation/Creator028/CreatorGiveawayRegistry10000.cs"
        };

        private static readonly string[] CreatorGiveaway10000ResourceFiles =
        {
            "Assets/Resources/SecondDimension/Creator10000/Data/RUNTIME_SAFE_CREATOR_CODE_HASH_MANIFEST_10000_v1.json",
            "Assets/Resources/SecondDimension/Creator10000/Data/CREATOR_REWARD_BUNDLES_10000_v1.json",
            "Assets/Resources/SecondDimension/Creator10000/Data/EXPEDITION_PREPARATION_ITEM_CATALOG_v1.csv",
            "Assets/Resources/SecondDimension/Creator10000/Data/NATURAL_REWARD_AND_SKILL_USE_HOOKS_v1.csv",
            "Assets/Resources/SecondDimension/Creator10000/Data/WEAPON_GIVEAWAY_TEMPLATE_CATALOG_v1.csv",
            "Assets/Resources/SecondDimension/Creator10000/Data/CODE_SYSTEM_INTEGRATION_RULES_v1.csv"
        };

        private static readonly string[] BoardTowerEnhancement001SourceFiles =
        {
            "Assets/SecondDimension/Gameplay/GuildCity017D/BoardTowerEnhancementRules001.cs",
            "Assets/SecondDimension/Presentation/BoardTower001/BoardTowerEnhancementCatalog001.cs",
            "Assets/SecondDimension/Presentation/BoardTower001/TowerRevealedRoomProjection001.cs"
        };

        private static readonly string[] BoardTowerEnhancement001ResourceFiles =
        {
            "Assets/Resources/SecondDimension/BoardTower001/Data/BOARD_TOWER_ITEM_CATALOG_144_001.json",
            "Assets/Resources/SecondDimension/BoardTower001/Data/ROOM_MODULE_CATALOG_72_001.json",
            "Assets/Resources/SecondDimension/BoardTower001/Data/RUN_EFFECT_CATALOG_72_001.json",
            "Assets/Resources/SecondDimension/BoardTower001/Data/TURNING_POINT_SEEDS_24_001.json",
            "Assets/Resources/SecondDimension/BoardTower001/Data/NATURAL_REWARD_HOOKS_30_001.json",
            "Assets/Resources/SecondDimension/BoardTower001/Data/P0_FIRST_HOUR_AND_TOWER_SUBSET_001.json",
            "Assets/Resources/SecondDimension/BoardTower001/Data/CONTENT_COUNTS_AND_VALIDATION_001.json"
        };

        private static readonly string[] RelicPatch083SourceFiles =
        {
            "Assets/SecondDimension/Gameplay/SpecialRelic001/SpecialRelicContracts001.cs",
            "Assets/SecondDimension/Gameplay/SpecialRelic001/SpecialRelicInvocationRules001.cs",
            "Assets/SecondDimension/Gameplay/SpecialRelic001/SpecialRelicUltimateArtBattleHook001.cs",
            "Assets/SecondDimension/Presentation/SpecialRelic001/SpecialRelicRegistry001.cs",
            "Assets/SecondDimension/Gameplay/RelicCode1000/RelicCodeContracts1000.cs",
            "Assets/SecondDimension/Gameplay/RelicCode1000/RelicCodeCommandService1000.cs",
            "Assets/SecondDimension/Presentation/RelicCode1000/RelicCodeRegistry1000.cs"
        };

        private static readonly string[] SpecialRelic001ResourceFiles =
        {
            "Assets/Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_CATALOG_60_001.json",
            "Assets/Resources/SecondDimension/SpecialRelic001/Data/P0_SPECIAL_RELICS_12_001.json",
            "Assets/Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_REWARD_HOOKS_001.csv"
        };

        private static readonly string[] RelicCode1000ResourceFiles =
        {
            "Assets/Resources/SecondDimension/RelicCode1000/Data/RUNTIME_SAFE_RELIC_CODE_HASH_MANIFEST_1000_v1.json",
            "Assets/Resources/SecondDimension/RelicCode1000/Data/RELIC_CODE_REWARD_BUNDLES_64_v1.json"
        };

        private static readonly string[] RelicPatch083CertificationFiles =
        {
            "Assets/Tests/EditMode/RelicCode1000EditModeTests.cs",
            "Assets/Tests/EditMode/SpecialRelicP0Combat001Tests.cs",
            "Assets/Tests/EditMode/SpecialRelicP0Invocation001Tests.cs"
        };

        private static readonly string[] Overnight089ProductionFiles =
        {
            "Assets/Editor/SecondDimension/BattleArtEnemy086Importer.cs",
            "Assets/Editor/SecondDimension/ExpeditionCardFace089Importer.cs",
            "Assets/Editor/SecondDimension/HeroMasterSprite089Importer.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/CampaignWorldGateModels023.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/ExpeditionDeckModels089.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/ExpeditionDeckService089.cs",
            "Assets/SecondDimension/Gameplay/Campaign023/ExpeditionDeckCommandService089.cs",
            "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityRecruitmentService017D.cs",
            "Assets/SecondDimension/Gameplay/Progression070/DeepProgressionCatalog070.cs",
            "Assets/SecondDimension/Gameplay/Progression070/RecruitTreeProgressionService070.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300ApplicantLead089.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300CreatorIntegration087.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300DeepProgressionAdapter089.cs",
            "Assets/SecondDimension/Gameplay/State/ProgressionState.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.BoardAdventureAnimation084.cs",
            "Assets/SecondDimension/Presentation/M1PresentationContracts.cs",
            "Assets/SecondDimension/Presentation/M1RuntimeCoordinator.cs",
            "Assets/SecondDimension/Presentation/Campaign023/Campaign023Definitions.cs",
            "Assets/SecondDimension/Presentation/Campaign023/Campaign023PresentationContracts.cs",
            "Assets/SecondDimension/Presentation/Campaign023/GuildCityAdventureBoard084.cs",
            "Assets/SecondDimension/Presentation/Campaign023/GuildCityFlowPresenter023.cs",
            "Assets/SecondDimension/Presentation/Campaign023/M1RuntimeCoordinator.Campaign023.cs",
            "Assets/SecondDimension/Presentation/Campaign023/M1FlowPresenter.ExpeditionDeck089.cs",
            "Assets/SecondDimension/Presentation/Campaign023/ExpeditionRouteCardMotion089.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2ArtLevelPresentation089.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2EnemyThreatPalette089.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.RecruitmentDesk074.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.RecruitAscension089.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/M1FlowPresenter.FirstHourGoldSmoke071.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityPresentationContracts017D.cs",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/RUSTBACK_HOUND_LEADER_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/HOLLOW_SALVAGER_LEADER_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/SHARDWING_SIGNAL_QUEEN_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/TOLLROAD_CUTTER_LEADER_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/RIFT_MOLD_CROWN_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/CHAINCALLER_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/ECHO_STALKER_IDLE_089.png",
            "Assets/Resources/SecondDimension/Art/Battle086/Enemies/OVERNIGHT089_ENEMY_IMAGEGEN_PROVENANCE.md",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_STORY_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_CHEST_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_BUFF_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_HAZARD_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_CHANCE_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_RECRUIT_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_CAMP_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_BATTLE_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/CARD_FACE_OBJECTIVE_089.png",
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/EXPEDITION_CARD_FACES_089_PROVENANCE.md",
            "Assets/Resources/SecondDimension/HeroMaster300/Data/HERO_300_REVIEW_SPRITE_PROMOTION_089.json",
            "Assets/Resources/SecondDimension/HeroMaster300/HERO_MASTER_001_300_READINESS.md",
            "Assets/Tests/EditMode/EnemyEncounterVariety089AuditTests.cs",
            "Assets/Tests/EditMode/ExpeditionDeck089Tests.cs",
            "Assets/Tests/EditMode/ExpeditionCardFace089ImportTests.cs",
            "Assets/Tests/EditMode/ExpeditionDeck089PresentationContractTests.cs",
            "Assets/Tests/EditMode/ExpeditionRecruitLeadApplicant089Tests.cs",
            "Assets/Tests/EditMode/FirstHourGoldSmokeIsolation078Tests.cs",
            "Assets/Tests/EditMode/HeroMasterReviewSpritePromotion089Tests.cs",
            "Assets/Tests/EditMode/M2ArtLevelPresentation089Tests.cs",
            "Assets/Tests/EditMode/M2EnemyThreatPalette089Tests.cs",
            "Assets/Tests/EditMode/M2DefeatedTargetRetarget088Tests.cs",
            "Assets/Tests/PlayMode/M2ChapterTwoBattlePresentation079PlayModeTests.cs",
            "Assets/Tests/PlayMode/M2EnemyVariant089PlayModeTests.cs",
            "Assets/Tests/PlayMode/M2DefeatedTargetRetarget088PlayModeTests.cs",
            "Assets/Tests/PlayMode/ExpeditionDeck089PresentationPlayModeTests.cs"
        };

        private static readonly string[] Release090ProductionFiles =
        {
            "Assets/Editor/SecondDimension/EnemyArt700TexturePostprocessor090.cs",
            "Assets/Editor/SecondDimension/EnemyArt700WindowsBuildCopy090.cs",
            "Assets/SecondDimension/EnemyArt700/Data/BaseEnemyIndex_001_070.json",
            "Assets/SecondDimension/EnemyArt700/Data/EnemyArtCatalog_001_070.json",
            "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityExpeditionService017D.QuestDeck090.cs",
            "Assets/SecondDimension/Gameplay/M2/BattleState.cs",
            "Assets/SecondDimension/Gameplay/M2/EnemyArtIdentity090.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/EnemyArt700Runtime090.cs",
            "Assets/SecondDimension/Presentation/Battle/M2LastRemnantBattleStaging.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.QuestDeck090.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.MenuSpritePresentation090.cs",
            "Assets/Tests/EditMode/GuildQuestDeck090GameplayTests.cs",
            "Assets/Tests/EditMode/BoardQuestDeck090PresentationTests.cs",
            "Assets/Tests/EditMode/EnemyArt700Runtime090Tests.cs",
            "Assets/Tests/EditMode/EnemyArt700Battle3DGroundContact090Tests.cs",
            "Assets/Tests/EditMode/EnemyArtIdentity090Tests.cs",
            "Assets/Tests/EditMode/M2BattleForecastTests.cs",
            "Assets/Tests/PlayMode/EnemyArt700BattlePresentation090PlayModeTests.cs"
        };

        // These are the exact bytes independently audited from the two runtime-safe
        // Library packs. The closed filename allowlist prevents extra files; these
        // pins prevent a same-named file from silently changing after that audit.
        private static readonly IReadOnlyDictionary<string, string> PinnedRuntimeDataSha256 =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Assets/Resources/SecondDimension/Creator10000/Data/CODE_SYSTEM_INTEGRATION_RULES_v1.csv", "D00FA379B32840A5A650B1D512AF11FCB6ED2B4262907BC4CB36487A223EA65B" },
                { "Assets/Resources/SecondDimension/Creator10000/Data/CREATOR_REWARD_BUNDLES_10000_v1.json", "AA16D14E105CFDE5C98B4A4791E62E71EC346B3FC61950475CD8F36894387D2C" },
                { "Assets/Resources/SecondDimension/Creator10000/Data/EXPEDITION_PREPARATION_ITEM_CATALOG_v1.csv", "C6164AB9133EA48962293B288E53743CD1F0B19A7DE62A16B63EFA513E4890D7" },
                { "Assets/Resources/SecondDimension/Creator10000/Data/NATURAL_REWARD_AND_SKILL_USE_HOOKS_v1.csv", "0081E6E8E09C407536DA248F068F32B4CF64ECC5EF60C91B70A0918673E6D49B" },
                { "Assets/Resources/SecondDimension/Creator10000/Data/RUNTIME_SAFE_CREATOR_CODE_HASH_MANIFEST_10000_v1.json", "F6AFAA0E4B6559BD05CC69930A5CFEA2B1B0F0E1EB110B6D2634D692DF3DE6EA" },
                { "Assets/Resources/SecondDimension/Creator10000/Data/WEAPON_GIVEAWAY_TEMPLATE_CATALOG_v1.csv", "222034FD27BAEA198FC9106B32771FFCCF3ADB1EAF7E59FEFED4E53AE54958AA" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/BOARD_TOWER_ITEM_CATALOG_144_001.json", "8A8FF4FE46E3B7B86FBD4BA28FA1FA8AC4A3453EE9A606997A3CBD75DCAC6851" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/ROOM_MODULE_CATALOG_72_001.json", "001C5114BFEAF747C133FEA6D438055D34AA6EE52FB88A0D024E310EACBE9174" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/RUN_EFFECT_CATALOG_72_001.json", "080D8D0176F9E2302E27B85CE0D810569154A372B986BEFD94CB3EF8F449552E" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/TURNING_POINT_SEEDS_24_001.json", "6D273078349C577C8E213C2FBB6BD5E0456D235D409523DE1AB212AED62F9808" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/NATURAL_REWARD_HOOKS_30_001.json", "24A936245A173E3FE1760EFB5C554B97311565C76BCE15640737A2CCB09AF138" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/P0_FIRST_HOUR_AND_TOWER_SUBSET_001.json", "1BF7BB70C66C1628CF4DC4C87F8F0CC4A6A7A1D71D726EC8EFCB8EB8FAD8F941" },
                { "Assets/Resources/SecondDimension/BoardTower001/Data/CONTENT_COUNTS_AND_VALIDATION_001.json", "35E8F47E61D72CECDF9E70412425EE14946845752CE5014D71F005E45DB80F1D" },
                { "Assets/Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_CATALOG_60_001.json", "9B463F49CD82E51984A8498B36252C09A3FE0BD780829DFB34144DE3F039E909" },
                { "Assets/Resources/SecondDimension/SpecialRelic001/Data/P0_SPECIAL_RELICS_12_001.json", "978B794447079C83739A06BCC5562B78CF5FB215D592D3AD096F9B198B306796" },
                { "Assets/Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_REWARD_HOOKS_001.csv", "F0C575E8853FA018372411436B00AECE20517726C266BCCC7CBEBEBF011C44B8" },
                { "Assets/Resources/SecondDimension/RelicCode1000/Data/RUNTIME_SAFE_RELIC_CODE_HASH_MANIFEST_1000_v1.json", "13D67DF4AF699DDD8B381F53AF87193D16808BD4D48AE407E5FBA2D951E7C299" },
                { "Assets/Resources/SecondDimension/RelicCode1000/Data/RELIC_CODE_REWARD_BUNDLES_64_v1.json", "642A84BE335385B3EDFB6CDCB368BBA2BD079F9186F4F586355F8378D8E5E5D4" }
            };

        private static readonly string[] EssentialProjectFiles = new[]
        {
            ScenePath,
            "Assets/SecondDimension/Presentation/Campaign020/M1FlowPresenter.CampaignCards129.cs",
            "Assets/SecondDimension/Gameplay/Campaign019/CampaignReplayCommandService130.cs",
            "Assets/SecondDimension/Gameplay/Campaign019/CampaignReplayThreat130.cs",
            "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionCommandService022.TowerRestart130.cs",
            "Assets/SecondDimension/Presentation/Boot/BootCoordinator.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.FirstHourArrival071.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.PlayableHub069.cs",
            "Assets/SecondDimension/Presentation/M1FlowPresenter.Version69.cs",
            "Assets/SecondDimension/Presentation/M1RuntimeCoordinator.cs",
            "Assets/SecondDimension/Presentation/M1VisualAssets.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/WalkableSkyhomeArrival071.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/FirstHourArtRegistry071.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/FirstHourArtPresentationResolver071.cs",
            "Assets/SecondDimension/Presentation/FirstHour072/FirstHourWorldStage072.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.GuildCharterPrologue074.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.FoundingPreparation078.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.RecruitmentDesk074.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.UnionPlanner074.cs",
            "Assets/SecondDimension/Presentation/FirstHour074/UnionPlannerRecruitDrag074.cs",
            "Assets/SecondDimension/Presentation/M1UnionIdentity076.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.cs",
            "Assets/SecondDimension/Presentation/FirstHour071/M1FlowPresenter.FirstHourGoldSmoke071.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.LivingGuildHub074.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.ExpeditionBoard074.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.BoardQuest081.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/ChapterTwoCrewMechanics079.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.ChapterTwoOpening076.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.FirstOperationConsequences077.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.GuildCityWorkshop078.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityFlowPresenter017D.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityVerticalSlicePresenter017D.cs",
            "Assets/SecondDimension/Presentation/Campaign022/Campaign022PresentationContracts.cs",
            "Assets/SecondDimension/Presentation/Campaign022/Campaign022Registry.cs",
            "Assets/SecondDimension/Presentation/Campaign022/GuildCityFlowPresenter022.cs",
            "Assets/SecondDimension/Presentation/Campaign022/M1RuntimeCoordinator.Campaign022.cs",
            "Assets/SecondDimension/Presentation/Battle/M2CinematicBattlePresenter.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/WorldInput071.cs",
            "Assets/SecondDimension/Presentation/GuildCity017D/WorldCharacterMotor070.cs",
            "Assets/SecondDimension/Presentation/Battle/Art011/BattleArtRuntimeRegistry011.cs",
            "Assets/SecondDimension/Presentation/Battle3D/M2FirstHourArtChoreography071.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M1FlowPresenter.BattleExperience072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleExperienceController072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleActorRig072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleCommandHud072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleDioramaView072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleResultsView072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleSequenceDirector072.cs",
            "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleTacticalReadout074.cs",
            "Assets/SecondDimension/Gameplay/FirstHour071/FirstHourDirector071.cs",
            "Assets/SecondDimension/Gameplay/FirstHour071/FirstHourRosterService071.cs",
            "Assets/SecondDimension/Gameplay/Campaign022/Campaign022ContentContracts.cs",
            "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityExpeditionService017D.cs",
            "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionCommandService022.cs",
            "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionModels022.cs",
            "Assets/SecondDimension/Gameplay/M1/M1CommandService.cs",
            "Assets/SecondDimension/Gameplay/M1/OpeningFlowState.cs",
            "Assets/SecondDimension/Gameplay/M2/M2BattleCommandService.cs",
            "Assets/SecondDimension/Gameplay/M2/M2CombatContent.cs",
            "Assets/SecondDimension/Gameplay/M2/M2DeepArtRuntime070.cs",
            "Assets/SecondDimension/Gameplay/M2/M2ProgressionRewards.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/AutoGeneration010/RecruitAutoGenerationCatalog010.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/AutoGeneration010/RecruitAutoGenerationSigningService010.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/AutoGeneration010/RecruitAutoGenerator010.cs",
            "Assets/StreamingAssets/Authority/CONTENT/CONTENT_AUTHORITY_002/DATA/SIGNATURE_RECRUITS_300.json",
            "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017D/GUILD_CITY_BUILDINGS_017D.json",
            "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017D/OPENING_CITY_LAYOUT_017D.json",
            "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017D/OPENING_CONTRACTS_017D.json",
            "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017D/OPENING_BOARD_017D.json",
            "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017D/OPENING_EVENTS_017D.json",
            "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017E/OPENING_EXPERIENCE_017E.json",
            "Assets/StreamingAssets/Authority/CONTENT/PASS_02/UNION_COMMANDS.json",
            "Assets/StreamingAssets/Authority/CONTENT/PASS_02/ART_DEFINITIONS.json",
            "Assets/StreamingAssets/Authority/CONTENT/PASS_02/FORMATIONS.json",
            "Assets/StreamingAssets/Authority/CONTENT/PASS_02/LEARN_BY_USE_SPEC.json",
            "Assets/StreamingAssets/Authority/CONTENT/PASS_03/ENEMY_DEFINITIONS.json",
            "Assets/StreamingAssets/Authority/CONTENT/PASS_03/ENEMY_UNIONS.json",
            "Assets/Resources/SecondDimension/GuildCity017E/Data/OPENING_EXPERIENCE_017E.json",
            "Assets/Resources/SecondDimension/FirstHour071/FIRST_HOUR_ART_RECIPES_120.json",
            "Assets/Resources/SecondDimension/Data/BattleArt011/BATTLE_ART_RUNTIME_MANIFEST_011.json",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/LANTERN_ROAD_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071.png",
            "Assets/Resources/SecondDimension/Art/GuildHome074/LIVING_GUILD_HALL_074.png",
            "Assets/Resources/SecondDimension/Art/Guided063/EXPEDITION_ROUTE_MAP_V63.png",
            "Assets/Resources/SecondDimension/Art/Board086/SECOND_DIMENSION_CARD_BACK_088.png",
            "Assets/Resources/SecondDimension/Art/Board086/BOARD_086_IMAGEGEN_PROVENANCE.md",
            "Assets/Tests/EditMode/BoardCardVisual088Tests.cs",
            "Assets/SecondDimension/Gameplay/M2/M2ArtMasteryLevelPolicy088.cs",
            "Assets/Tests/EditMode/M2ArtMasteryLevelPolicy088Tests.cs",
            "Assets/Tests/EditMode/RecruitmentUnlockFlow088Tests.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300Catalog087.cs",
            "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300CreatorIntegration087.cs",
            "Assets/SecondDimension/Presentation/Creator028/HeroMaster300CreatorRegistry087.cs",
            "Assets/Resources/SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300.json",
            "Assets/Resources/SecondDimension/HeroMaster300/HERO_MASTER_001_300_READINESS.md",
            "Assets/Tests/EditMode/HeroMaster300Catalog087Tests.cs",
            "Assets/Tests/EditMode/HeroMaster300CreatorCodes087Tests.cs",
            "Assets/Resources/SecondDimension/Art/FirstHour071/Environments/GATEHOUSE_BOSS_ARENA_071.png",
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/WAYGLASS_UNDERCROFT_BATTLE_PLATE_079.png",
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/WAYGLASS_DOOR_RESCUE_ARENA_080.png",
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/ECHO_STALKER_SCOUT_079.png",
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/ECHO_STALKER_VEILWARDEN_079.png",
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/CHAINCALLER_LEADER_079.png",
            "Assets/Resources/SecondDimension/Art/Portraits/Recruits/CANON_KIRI_AETHERHEART.jpg",
            "Assets/Resources/SecondDimension/Art/Portraits/ChapterTwo079/SELLA_VEY_PORTRAIT_079.png",
            "Assets/Resources/SecondDimension/Art/Portraits/ChapterTwo079/ORRA_VALE_PORTRAIT_079.png",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_01_Mud_Trenches.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_02_Arrow_Rain_Field.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_03_Silent_Camp.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_04_Collapsed_Siege_Wall.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_05_Grave_Banner_Hill.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_06_Iron_March.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_07_Bloodless_River.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_08_War_Beast_Pens.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_09_Ash_Command_Tent.jpg",
            "Assets/Resources/SecondDimension/Campaign022/UI/Abyss/Floor_10_Aegis_Gate.jpg",
            "Assets/Resources/SecondDimension/Art/FirstHour076/Guildmaster/GUILDMASTER_STANDEE_076.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_IDLE.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_ANTICIPATION.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_ACTION_PRIMARY.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_ROLE_PRIMARY.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_RECOVERY.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_HIT_REACTION.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_DOWNED.png",
            "Assets/Resources/SecondDimension/Art/Battle011/Characters/GUILDMASTER_076/POSE_VICTORY.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_ASTER_MARSHLIGHT_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_DAIN_DEEPWELL_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_JUNIA_SKYWARD_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_PETRA_RUNEBROOK_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_QUIN_CROWNHILL_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_QUIN_LOWEN_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_UNA_QUEENSREST_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_WILLOW_LONGSTRIDE_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_YVES_THORNFIELD_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Portraits/FirstHour076/SIGREC_ZORIN_BRAMBLECROSS_PORTRAIT_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_ASTER_MARSHLIGHT_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_DAIN_DEEPWELL_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_JUNIA_SKYWARD_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_PETRA_RUNEBROOK_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_QUIN_CROWNHILL_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_QUIN_LOWEN_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_UNA_QUEENSREST_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_WILLOW_LONGSTRIDE_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_YVES_THORNFIELD_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/ACTION_SIGREC_ZORIN_BRAMBLECROSS_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_ASTER_MARSHLIGHT_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_DAIN_DEEPWELL_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_JUNIA_SKYWARD_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_PETRA_RUNEBROOK_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_QUIN_CROWNHILL_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_QUIN_LOWEN_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_UNA_QUEENSREST_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_WILLOW_LONGSTRIDE_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_YVES_THORNFIELD_076.png",
            "Assets/Resources/SecondDimension/Art/Battle/FirstHour076/STANDEE_SIGREC_ZORIN_BRAMBLECROSS_076.png"
        }.Concat(Campaign022RegistryResourceFiles)
            .Concat(TowerVisual083CertificationFiles)
            .Concat(CreatorCodeRuntimeFiles)
            .Concat(CreatorGiveaway10000SourceFiles)
            .Concat(CreatorGiveaway10000ResourceFiles)
            .Concat(BoardTowerEnhancement001SourceFiles)
            .Concat(BoardTowerEnhancement001ResourceFiles)
            .Concat(RelicPatch083SourceFiles)
            .Concat(SpecialRelic001ResourceFiles)
            .Concat(RelicCode1000ResourceFiles)
            .Concat(RelicPatch083CertificationFiles)
            .Concat(Overnight089ProductionFiles)
            .Concat(Release090ProductionFiles)
            .ToArray();

        public static void BuildFromCommandLine()
        {
            try
            {
                ValidateEssentialsOrThrow();
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                    throw new InvalidOperationException(
                        "First Hour Gold requires -buildTarget StandaloneWindows64.");

                var outputRoot = ResolveOutputRoot();
                var stagingRoot = ResolveStagingRoot(outputRoot);
                if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true);
                Directory.CreateDirectory(stagingRoot);
                var executable = Path.Combine(stagingRoot, ExecutableName);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = executable,
                    targetGroup = BuildTargetGroup.Standalone,
                    target = BuildTarget.StandaloneWindows64,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.None
                });

                var summary = report.summary;
                var subtarget = summary.GetSubtarget<StandaloneBuildSubtarget>();
                if (summary.result != BuildResult.Succeeded ||
                    summary.totalErrors != 0 ||
                    summary.totalWarnings != 0 ||
                    summary.platform != BuildTarget.StandaloneWindows64 ||
                    subtarget != StandaloneBuildSubtarget.Player)
                    throw new InvalidOperationException(
                        "First Hour Gold build failed. Result=" + summary.result +
                        ", Errors=" + summary.totalErrors +
                        ", Warnings=" + summary.totalWarnings +
                        ", Platform=" + summary.platform +
                        ", Subtarget=" + subtarget + ".");

                var dataRoot = Path.Combine(
                    stagingRoot,
                    Path.GetFileNameWithoutExtension(ExecutableName) + "_Data");
                RequireNonEmptyFile(executable);
                RequireNonEmptyFile(Path.Combine(stagingRoot, "UnityPlayer.dll"));
                RequireNonEmptyFile(Path.Combine(dataRoot, "globalgamemanagers"));
                RequireNonEmptyFile(Path.Combine(dataRoot, "resources.assets"));

                File.WriteAllText(
                    Path.Combine(stagingRoot, "FIRST_HOUR_GOLD_BUILD_ID.txt"),
                    BuildId + Environment.NewLine,
                    Encoding.UTF8);
                File.WriteAllLines(
                    Path.Combine(stagingRoot, "BUILD_SUMMARY_FIRST_HOUR_GOLD.txt"),
                    new[]
                    {
                        "SECOND DIMENSION - GUILD OF WORLDS",
                        "Build ID: " + BuildId,
                        "Build label: " + BuildLabel,
                        "Player version: " + PlayerSettings.bundleVersion,
                        "Unity: " + Application.unityVersion,
                        "Result: " + summary.result,
                        "Errors: " + summary.totalErrors,
                        "Warnings: " + summary.totalWarnings,
                        "Platform: " + summary.platform,
                        "Subtarget: " + subtarget,
                        "Total bytes: " + summary.totalSize,
                        "Built UTC: " + DateTime.UtcNow.ToString("o"),
                        "Scene: " + ScenePath,
                        "Executable: " + ExecutableName,
                        "Smoke flag: " + SmokeCommandLineFlag,
                        "Smoke report schema: " + SmokeReportSchema,
                        "Smoke report file: " + SmokeReportFileName,
                        "Smoke expected screenshots: exactly " + SmokeExpectedScreenshotCount,
                        "Smoke expected gates: " + SmokeExpectedGateCount,
                        "Evidence identity: build ID + executable + player assembly + resources + manifest SHA-256"
                    },
                    Encoding.UTF8);
                File.WriteAllText(
                    Path.Combine(stagingRoot, "PLAY_SECOND_DIMENSION.cmd"),
                    "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"" + ExecutableName + "\"\r\n",
                    Encoding.ASCII);
                File.WriteAllLines(
                    Path.Combine(stagingRoot, BuildCompleteFileName),
                    new[]
                    {
                        "BUILD COMPLETE",
                        "Build ID: " + BuildId,
                        "Build label: " + BuildLabel,
                        "Player version: " + PlayerVersion,
                        "Smoke schema: " + SmokeReportSchema
                    },
                    Encoding.UTF8);
                WriteHashes(stagingRoot);
                PublishStagedBuild(stagingRoot, outputRoot);
                Debug.Log(BuildPassMarker);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string ResolveOutputRoot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var secondDimensionRoot = Directory.GetParent(projectRoot)?.FullName;
            if (string.IsNullOrWhiteSpace(secondDimensionRoot))
                throw new InvalidOperationException("The canonical SecondDimension root is unavailable.");
            var outputRoot = Path.GetFullPath(Path.Combine(
                secondDimensionRoot,
                "Builds",
                "FIRST_HOUR_GOLD"));
            var expected = Path.GetFullPath(@"C:\SecondDimension\Builds\FIRST_HOUR_GOLD");
            if (!StringComparer.OrdinalIgnoreCase.Equals(outputRoot, expected))
                throw new InvalidOperationException(
                    "Refusing to build outside the canonical delivery path: " + outputRoot);
            return outputRoot;
        }

        private static string ResolveStagingRoot(string outputRoot)
        {
            var stagingRoot = Path.GetFullPath(outputRoot + ".__building_084");
            var expected = Path.GetFullPath(
                @"C:\SecondDimension\Builds\FIRST_HOUR_GOLD.__building_084");
            if (!StringComparer.OrdinalIgnoreCase.Equals(stagingRoot, expected))
                throw new InvalidOperationException(
                    "Refusing to stage outside the canonical build area: " + stagingRoot);
            return stagingRoot;
        }

        private static void PublishStagedBuild(string stagingRoot, string outputRoot)
        {
            RequireNonEmptyFile(Path.Combine(stagingRoot, ExecutableName));
            RequireNonEmptyFile(Path.Combine(stagingRoot, BuildCompleteFileName));
            RequireNonEmptyFile(Path.Combine(stagingRoot, "BUILD_SHA256.txt"));
            var backupRoot = Path.GetFullPath(
                outputRoot + ".__previous_084_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"));
            var expectedBackupPrefix = Path.GetFullPath(
                @"C:\SecondDimension\Builds\FIRST_HOUR_GOLD.__previous_084_");
            if (!backupRoot.StartsWith(expectedBackupPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Refusing to publish through an unexpected backup path: " + backupRoot);

            var movedPrevious = false;
            try
            {
                if (Directory.Exists(outputRoot))
                {
                    Directory.Move(outputRoot, backupRoot);
                    movedPrevious = true;
                }
                Directory.Move(stagingRoot, outputRoot);
            }
            catch
            {
                if (!Directory.Exists(outputRoot) && movedPrevious && Directory.Exists(backupRoot))
                    Directory.Move(backupRoot, outputRoot);
                throw;
            }

            if (movedPrevious)
                Debug.Log(
                    "Previous canonical player preserved for recovery at " + backupRoot + ".");
        }

        private static void ValidateEssentialsOrThrow()
        {
            if (!StringComparer.Ordinal.Equals(Application.unityVersion, "6000.3.22f1"))
                throw new InvalidOperationException(
                    "First Hour Gold must be built with Unity 6000.3.22f1, not " +
                    Application.unityVersion + ".");
            if (!StringComparer.Ordinal.Equals(PlayerSettings.bundleVersion, PlayerVersion))
                throw new InvalidOperationException(
                    "First Hour Gold must use player version " + PlayerVersion + ", not " +
                    PlayerSettings.bundleVersion + ".");
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (var relativePath in EssentialProjectFiles)
            {
                var absolutePath = Path.Combine(
                    projectRoot,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                RequireNonEmptyFile(absolutePath);
                RequireNonEmptyFile(absolutePath + ".meta");
            }
            if (!StringComparer.Ordinal.Equals(ScenePath, "Assets/Scenes/Boot.unity"))
                throw new InvalidOperationException(
                    "Studio First Hour must ship the canonical Boot scene only: " + ScenePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Boot scene is not importable: " + ScenePath);
            ValidateOvernight089ContractOrThrow(projectRoot);
            ValidateRelease090ContractOrThrow(projectRoot);
            ValidateTowerContractOrThrow(projectRoot);
            ValidateBoardTowerEnhancementContractOrThrow(projectRoot);
            ValidateCreatorCodeContractOrThrow(projectRoot);
            ValidateRelicPatchContractOrThrow(projectRoot);

            var bootSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets",
                "SecondDimension",
                "Presentation",
                "Boot",
                "BootCoordinator.cs"));
            if (!bootSource.Contains("AddComponent<M1FlowPresenter>()") ||
                bootSource.Contains("AddComponent<FirstHourExperienceRoot>()"))
                throw new InvalidOperationException(
                    "Boot must use the complete shipping M1 route, not the truncated 072 opening root.");

            var presenterSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/M1FlowPresenter.cs");
            var arrivalBridgeSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/M1FlowPresenter.FirstHourArrival071.cs");
            var marketSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/WalkableSkyhomeArrival071.cs");
            RequireSourceToken(presenterSource,
                "EnterDirectGuildCharter091()",
                "runtime direct Guild entry without mandatory walking");
            RequireSourceToken(arrivalBridgeSource,
                "_walkableSkyhomeArrival071.Begin071(",
                "walkable Market launch");
            RequireSourceToken(arrivalBridgeSource,
                "Navigate(M1Screen.FirstHourOpening);",
                "Market-to-charter story handoff");
            RequireSourceToken(marketSource,
                "InvokeHost071(_reachHall071);",
                "player-entered Guild Hall threshold");
            RequireSourceToken(marketSource,
                "WorldInput071",
                "cross-platform Market movement input");
            RequireSourceToken(marketSource,
                "CurrentObjectiveDirectionForVerification076",
                "read-only Market objective direction seam");
            RequireSourceToken(marketSource,
                "CurrentObjectiveDistanceForVerification076",
                "read-only Market objective distance seam");
            RequireSourceToken(marketSource,
                "CurrentObjectiveInteractionRadiusForVerification076",
                "read-only Market interaction-radius seam");
            RequireSourceToken(marketSource,
                "IsWithinCurrentObjectiveInteractionRangeForVerification076",
                "read-only Market interaction-range seam");

            var guildCityFlowSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityFlowPresenter017D.cs");
            RequireSourceToken(guildCityFlowSource,
                "StartGuildCityExpedition017D()",
                "authoritative first rescue deployment");
            RequireSourceToken(guildCityFlowSource,
                "ShouldEnterGuidedFirstHourField076(coordinator)",
                "first rescue board-to-field routing");
            RequireSourceToken(guildCityFlowSource,
                "gameObject.AddComponent<GuildCity017D.OuterGateworksExploration066>()",
                "walkable Lantern Road host");
            RequireSourceToken(guildCityFlowSource,
                "() => EnterBattleFromOuterGateworks066(coordinator)",
                "field-to-battle handoff");
            RequireSourceToken(guildCityFlowSource,
                "FOLLOW SELLA AND ORRA'S BRASS LINE",
                "person-first phone-simple Chapter 2 contract promise");
            RequireSourceToken(guildCityFlowSource,
                "EXPOSE THE WAYGLASS FORGERY",
                "concrete Chapter 2 story consequence");
            ForbidSourceToken(guildCityFlowSource,
                "AMBUSH OR SUPPLY LOSS  •  YOUR FIRST TEST CHOOSES WHICH",
                "random-sounding Chapter 2 contract promise");

            var fieldSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityVerticalSlicePresenter017D.cs");
            RequireSourceToken(fieldSource,
                "public void InteractForVerification076() => ApplyInteractionInput066();",
                "physical field interaction seam");
            RequireSourceToken(fieldSource,
                "_requestBattle?.Invoke();",
                "player-entered field battle request");
            RequireSourceToken(fieldSource,
                "RebuildRoomPresentation072(requestedRoom072, requestedBeatKey072);",
                "saved field-room reconstruction");
            RequireSourceToken(fieldSource,
                "PatrolRescueCeremonyRequestedForVerification076 = true;",
                "on-foot patrol rescue ceremony request");
            RequireSourceToken(fieldSource,
                "CurrentObjectiveDirectionForVerification076",
                "read-only Lantern Road objective direction seam");
            RequireSourceToken(fieldSource,
                "CurrentObjectiveDistanceForVerification076",
                "read-only Lantern Road objective distance seam");
            RequireSourceToken(fieldSource,
                "CurrentObjectiveInteractionRadiusForVerification076",
                "read-only Lantern Road interaction-radius seam");
            RequireSourceToken(fieldSource,
                "IsWithinCurrentObjectiveInteractionRangeForVerification076",
                "read-only Lantern Road interaction-range seam");

            var battleReturnSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M1FlowPresenter.BattleExperience072.cs");
            RequireSourceToken(battleReturnSource,
                "ReturnFromClaimedBattleExperience072()",
                "claimed battle return routing");
            RequireSourceToken(battleReturnSource,
                "EnterExpeditionBoard074(guildCity);",
                "post-battle guided field reconstruction");

            var expeditionBoardSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.ExpeditionBoard074.cs");
            RequireSourceToken(expeditionBoardSource,
                "OpenPostRescueUnionReview076",
                "rescue ceremony to Union review handoff");
            RequireSourceToken(expeditionBoardSource,
                "_returnToExpeditionAfterUnionReview076 = true;",
                "rescue review field-return intent");
            ForbidSourceToken(expeditionBoardSource,
                "FOLLOW THE FRESH MARKS",
                "Chapter 2 order that sounds like obeying forged marks");

            var boardQuestSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.BoardQuest081.cs");
            RequireSourceToken(boardQuestSource,
                "AutomaticDestination081(state, view)",
                "phone-simple automatic next-room authority");
            RequireSourceToken(boardQuestSource,
                "Continue to reveal the next saved room. Its reward or dice result resolves once.",
                "approved saved-room mission continuation promise");
            RequireSourceToken(boardQuestSource,
                "() => FlipBoardQuestRoom081(coordinator, destination.NodeId)",
                "single mission movement control invokes the existing exact-room authority");
            RequireSourceToken(boardQuestSource,
                "MOVE FORWARD\\nFLIP NEXT ROOM",
                "single mission movement action");
            RequireSourceToken(boardQuestSource,
                "ROLLING 2D6…\\nWATCH THE DICE",
                "automatic visible dice action");
            RequireSourceToken(boardQuestSource,
                "The game chose the best useful crew.",
                "automatic field-team explanation");
            RequireSourceToken(presenterSource,
                "TryReturnToGuidedFirstHourFieldAfterUnionReview076()",
                "Union review return to the saved Gate-Eater marker");

            var foundingPreparationSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.FoundingPreparation078.cs");
            var unionPlannerSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.UnionPlanner074.cs");
            var coordinatorSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/M1RuntimeCoordinator.cs");
            var openingEventsSource = ReadProjectSource(
                projectRoot,
                "Assets/StreamingAssets/Authority/CONTENT/GUILD_CITY_017D/OPENING_EVENTS_017D.json");
            RequireSourceToken(foundingPreparationSource,
                "FoundingPreparationDecisionCount078 = 5",
                "five mandatory saved Guildmaster preparation decisions");
            RequireSourceToken(foundingPreparationSource,
                "NormalUnionPlanRules.MaximumMembersPerUnion",
                "six-member Union-capacity preparation guard");
            RequireSourceToken(foundingPreparationSource,
                "FoundingEquipmentDisplayNameForVerification078",
                "player-facing founding equipment names");
            RequireSourceToken(foundingPreparationSource,
                "FoundingCanEditPreviousForVerification078",
                "reversible founding-preparation decisions");
            RequireSourceToken(unionPlannerSource,
                "UnionPlannerScaleCopyForVerification078",
                "visible ten-Union by six-member battle scale");
            RequireSourceToken(coordinatorSource,
                "RecurringApplicantPersonalHookForVerification078",
                "natural-language recurring applicant motivation copy");
            RequireSourceToken(openingEventsSource,
                "EVENT_LANTERN_WATCH_CAMP",
                "dedicated Chapter 1 Maren-Jazzi-Zorin camp authority");

            var chapterTwoCrewSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/ChapterTwoCrewMechanics079.cs");
            RequireSourceToken(chapterTwoCrewSource,
                "BuildCheckPreview079(",
                "Chapter 2 role-fit and truthful authored-outcome preview");
            RequireSourceToken(chapterTwoCrewSource,
                "PROMISED CONSEQUENCE",
                "Chapter 2 deterministic outcome label");
            RequireSourceToken(chapterTwoCrewSource,
                "BuildEvidenceStatus079(",
                "Chapter 2 evidence and crew-trust projection");
            RequireSourceToken(expeditionBoardSource,
                "preview079.CommandModifier",
                "Chapter 2 command modifier without campaign-mode double counting");
            RequireSourceToken(expeditionBoardSource,
                "chapterTwoEvidence079.CompactReadout",
                "visible Chapter 2 evidence and crew-trust readout");
            RequireSourceToken(expeditionBoardSource,
                "ExpeditionFieldLeadLabel078",
                "unambiguous field-lead decision label");
            RequireSourceToken(expeditionBoardSource,
                "ExpeditionCompanionVoiceLabel078",
                "companion story voice distinguished from the selected field lead");

            var activeOpeningExperienceSource = ReadProjectSource(
                projectRoot,
                "Assets/Resources/SecondDimension/GuildCity017E/Data/OPENING_EXPERIENCE_017E.json");
            var legacyOpeningNarrativeSource = ReadProjectSource(
                projectRoot,
                "Assets/Resources/SecondDimension/GuildCity017F/Data/OPENING_NARRATIVE_017F.json");
            var activeChapterTwoSource = ExtractBoardNarrative079(
                activeOpeningExperienceSource,
                "BOARD_LINES_NOT_RETURNED",
                "BOARD_RELIEF_ROAD");
            var legacyChapterTwoSource = ExtractBoardNarrative079(
                legacyOpeningNarrativeSource,
                "BOARD_LINES_NOT_RETURNED",
                "BOARD_RELIEF_ROAD");
            RequireSourceToken(activeChapterTwoSource,
                "Sella Vey",
                "named Sella evidence in active Chapter 2 narrative");
            RequireSourceToken(activeChapterTwoSource,
                "Orra Vale",
                "named Orra evidence in active Chapter 2 narrative");
            RequireSourceToken(legacyChapterTwoSource,
                "Whose Evidence Leads",
                "person-first legacy Chapter 2 route order");
            ForbidSourceToken(activeChapterTwoSource,
                "Route Decision",
                "generic active Chapter 2 route decision copy");
            ForbidSourceToken(activeChapterTwoSource,
                "deterministic 2d6",
                "dice-language in active Chapter 2 narrative");
            ForbidSourceToken(legacyChapterTwoSource,
                "Route Decision",
                "generic legacy Chapter 2 route decision copy");
            ForbidSourceToken(legacyChapterTwoSource,
                "deterministic 2d6",
                "dice-language in legacy Chapter 2 narrative");

            var artRegistrySource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/FirstHourArtRegistry071.cs");
            var liveArtSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Art011/BattleArtRuntimeRegistry011.cs");
            var visualAssetsSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/M1VisualAssets.cs");
            var liveActorRigSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleActorRig072.cs");
            var liveDioramaSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleDioramaView072.cs");
            var liveResultsSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleResultsView072.cs");
            var liveSequenceSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleSequenceDirector072.cs");
            var liveControllerSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleExperienceController072.cs");
            RequireSourceToken(artRegistrySource,
                "public const int ExpectedTreeCount = 30;",
                "thirty first-hour Art trees");
            RequireSourceToken(artRegistrySource,
                "public const int ExpectedRecipesPerTree = 4;",
                "four exact presentations per Art tree");
            RequireSourceToken(liveArtSource,
                "TryResolveExactFirstHourProfile076(",
                "shipping exact first-hour Art resolver");
            RequireSourceToken(liveArtSource,
                "FirstHourArtPresentationResolver071.TryResolve(key, out var recipe)",
                "120-recipe live battle presentation bridge");
            RequireSourceToken(liveArtSource,
                "TryResolveSemanticIcon076(",
                "semantic forecast icon resolver");
            RequireSourceToken(visualAssetsSource,
                "WayglassUndercroftBattlePlateResourceKey079",
                "Chapter 2 Wayglass battle plate resolver");
            RequireSourceToken(visualAssetsSource,
                "TryResolveChapterTwoEnemyBattleStandee079(",
                "exact Chapter 2 enemy standee resolver");
            RequireSourceToken(liveActorRigSource,
                "M1VisualAssets.TryResolveChapterTwoEnemyBattleStandee079(",
                "Fog-Stalker actor routing before Gate Gnawer recovery art");
            RequireSourceToken(liveDioramaSource,
                "TryResolveLiveExactRecipe076(",
                "live 072 exact motion, VFX, and timing resolver");
            RequireSourceToken(liveDioramaSource,
                "M2ArtLevelPresentation089.AccentPulseCount(artLevel)",
                "visible level-4, level-7, and level-10 Art impact escalation");
            RequireSourceToken(liveDioramaSource,
                "PreferredNarrativeTargetUnionId079(",
                "Gate-Eater boss-first opening focus");
            RequireSourceToken(liveDioramaSource,
                "public bool PresentHpEvent076(M2BattleEventView item)",
                "authoritative-event-to-visible-HP presentation bridge");
            RequireSourceToken(liveDioramaSource,
                "public bool HpImpactVisible076",
                "active HP-impact visibility diagnostic");
            RequireSourceToken(liveDioramaSource,
                "Signed HP Impact Heading 076",
                "prominent signed HP-impact callout");
            RequireSourceToken(liveDioramaSource,
                "Before After HP Impact Value 076",
                "visible HP before-to-after readout");
            RequireSourceToken(liveResultsSource,
                "FOG-STALKERS DOWN",
                "non-ellipsized Fog-Stalker outcome recap");
            RequireSourceToken(liveResultsSource,
                "WAYGLASS RECOVERY  •  ECHO FILAMENT",
                "encounter-specific Wayglass loot identity");
            RequireSourceToken(liveResultsSource,
                "reward?.EquipmentRewardDisplayName",
                "player-truthful authority-backed Fog reward item name");
            ForbidSourceToken(liveResultsSource,
                "LootDisplayNameOverride",
                "presentation-only battle reward rename");
            RequireSourceToken(liveSequenceSource,
                "ExactRecipeAudioCueId076(",
                "live exact-signature SFX selector");
            RequireSourceToken(liveSequenceSource,
                "M2ArtLevelPresentation089.ResolveCommittedLevel(",
                "committed learned-Art level presentation bridge");
            RequireSourceToken(liveControllerSource,
                "HasConsumedLiveExactArtRecipe076",
                "read-only live Art recipe consumption diagnostic");
            RequireSourceToken(liveControllerSource,
                "CompletedExactBeatCount076",
                "cumulative fully completed exact-Art beat diagnostic");
            RequireSourceToken(liveControllerSource,
                "HasCompleteLiveArtRecipeConsumption076",
                "complete renderer-consumption diagnostic");
            RequireSourceToken(liveControllerSource,
                "HasConsumedExactRecipeSfxSignature076(",
                "exact completed-recipe audio-signature diagnostic");

            var smokeSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets",
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "FirstHourGoldSmoke071.cs"));
            RequireSourceToken(smokeSource,
                "public string schema = \"" + SmokeReportSchema + "\";",
                "Release 084 smoke schema");
            RequireSourceToken(smokeSource,
                "private const string SmokeFlag = \"" + SmokeCommandLineFlag + "\";",
                "built-player smoke flag");
            RequireSourceToken(smokeSource,
                "\"" + SmokeReportFileName + "\"",
                "Release 084 smoke report filename");
            RequireSourceToken(smokeSource,
                "Debug.Log(\"FIRST HOUR GOLD BUILT PLAYER SMOKE 084 \" + " +
                "_report.status + \" • \" + _reportPath);",
                "Release 084 exact live built-player completion expression");
            for (var retiredMarker = 70; retiredMarker <= 83; retiredMarker++)
                ForbidSourceToken(
                    smokeSource,
                    "FIRST HOUR GOLD BUILT PLAYER SMOKE " +
                    retiredMarker.ToString("000"),
                    "retired numbered built-player completion marker");
            RequireSourceToken(smokeSource,
                "_screenshotOrdinal == " + SmokeExpectedScreenshotCount,
                "Release 084 exact screenshot-count gate");
            RequireSourceToken(smokeSource,
                "_report.gates.Count == " + SmokeExpectedGateCount,
                "Release 084 certification-gate count");
            RequireSourceToken(smokeSource,
                "campaignEnemyArt700Verified",
                "packaged Campaign Enemy Art 700 render proof");
            RequireSourceToken(smokeSource,
                "towerEnemyArt700Verified",
                "packaged Tower Enemy Art 700 render proof");
            RequireSourceToken(smokeSource,
                "CertifyLiveEnemyArt700Actors090(",
                "live packaged Enemy Art 700 actor certification");
            RequireSourceToken(smokeSource,
                "PASS: packaged_campaign_enemy_art_700",
                "packaged Campaign Enemy Art 700 smoke gate");
            RequireSourceToken(smokeSource,
                "PASS: packaged_tower_enemy_art_700",
                "packaged Tower Enemy Art 700 smoke gate");
            RequireSourceToken(smokeSource, "_report.status = \"PASS\";", "smoke PASS contract");
            RequireSourceToken(smokeSource,
                "chapterTwoRoutePersistenceVerified",
                "persisted Chapter 2 route gate");
            RequireSourceToken(smokeSource,
                "foundingGuildmasterPreparationVerified",
                "five-step Guildmaster preparation evidence gate");
            RequireSourceToken(smokeSource,
                "chapterTwoCrewAssignmentsVerified",
                "Chapter 2 role-fit, partner, and guaranteed authored-outcome evidence gate");
            RequireSourceToken(smokeSource,
                "RequireChapterTwoCommitted2d6Resolution081",
                "Chapter 2 evidence chain contains no committed Board Quest 2d6 resolution proof.");
            RequireSourceToken(smokeSource,
                "primaryCopy.IndexOf('%') < 0",
                "Chapter 2 authored outcomes forbid stale random-percentage copy");
            RequireSourceToken(smokeSource,
                "EVENT_LANTERN_WATCH_CAMP",
                "Chapter 1 camp remains separate from Chapter 2 evidence authority");
            RequireSourceToken(smokeSource,
                "RequireNamedVisibleTextNotContains076",
                "natural-language applicant motivation evidence without internal codes");
            RequireSourceToken(smokeSource,
                "FIELD  •  10 UNIONS × 6 = 60",
                "visible sixty-member field-scale evidence");
            RequireSourceToken(smokeSource,
                "chapterTwoPlayableChainVerified",
                "played Chapter 2 decision, camp, encounter, and objective chain");
            RequireSourceToken(smokeSource, "buildIdentityVerified", "packaged build identity gate");
            RequireSourceToken(smokeSource,
                "characterIdentityArtVerified",
                "packaged character identity-art gate");
            RequireSourceToken(smokeSource,
                "personFirstTitleVerified",
                "person-first title key-art gate");
            RequireSourceToken(smokeSource,
                "directGuildEntry091Verified",
                "direct Guild entry gate");
            RequireSourceToken(smokeSource,
                "_report.expeditionBoardVerified = true;",
                "complete first-hour expedition-board route gate");
            RequireSourceToken(smokeSource,
                "FindFirstObjectByType<OuterGateworksExploration066>() == null",
                "retired side-scroll corridor remains absent during expedition-board play");
            RequireSourceToken(smokeSource,
                "missionBriefRoundTripVerified",
                "optional Mission Brief round-trip gate");
            RequireSourceToken(smokeSource,
                "postBattleFieldReturnVerified",
                "post-battle field reconstruction gate");
            RequireSourceToken(smokeSource,
                "oneObjectivePerFieldNodeVerified",
                "single gold field objective gate");
            RequireSourceToken(smokeSource,
                "twoStartingTreesVerified",
                "two usable starting Art trees per recruit gate");
            RequireSourceToken(smokeSource,
                "patrolCombatReadyVerified",
                "rescued patrol combat participation gate");
            RequireSourceToken(smokeSource,
                "firstHourArtPresentationVerified",
                "exact 120-Art live presentation gate");
            RequireSourceToken(smokeSource,
                "liveFirstHourArtRecipeConsumptionVerified",
                "real-battle exact Art recipe consumption gate");
            RequireSourceToken(smokeSource,
                "!_report.liveFirstHourArtRecipeConsumptionVerified",
                "release-wide one-time exact Art renderer certification guard");
            RequireSourceToken(smokeSource,
                "priorCompletedExactBeatCount076",
                "pre-Execute completed exact-beat baseline");
            RequireSourceToken(smokeSource,
                "HasCompleteLiveArtRecipeConsumption076",
                "post-recovery exact renderer-consumption gate");
            RequireSourceToken(smokeSource,
                "LastCompletedExactObservedCompletionMilliseconds076",
                "observed exact-Art completion timing gate");
            RequireSourceToken(smokeSource,
                "LastCompletedExactCameraRecipe076",
                "completed exact-Art camera recipe gate");
            RequireSourceToken(smokeSource,
                "LastCompletedExactTraceRecipe076",
                "completed exact-Art trace recipe gate");
            RequireSourceToken(smokeSource,
                "LastCompletedExactTraceGeometryCount076",
                "completed exact-Art trace geometry gate");
            RequireSourceToken(smokeSource,
                "HasConsumedExactRecipeSfxSignature076(",
                "completed exact-Art audio-signature gate");
            RequireSourceToken(smokeSource,
                "ObjectiveTraversalRetainedTickSeconds076",
                "low-frame-rate retained motor-tick allowance");
            RequireSourceToken(smokeSource,
                "WalkMarketToCurrentObjective076(",
                "motor-driven Market traversal gate");
            RequireSourceToken(smokeSource,
                "WalkFieldToCurrentObjective076(",
                "motor-driven Lantern Road traversal gate");
            RequireSourceToken(smokeSource,
                "ApplyMovementForVerification071(",
                "shipping Skyhome movement-motor input gate");
            RequireSourceToken(smokeSource,
                "ApplyMovementInput066(",
                "shipping Lantern Road movement-motor input gate");
            RequireSourceToken(smokeSource,
                "IsWithinCurrentObjectiveInteractionRangeForVerification076",
                "real objective interaction-range gate");
            RequireSourceToken(smokeSource,
                "CurrentObjectiveDistanceForVerification076",
                "finite objective-distance progress gate");
            RequireSourceToken(smokeSource,
                "CurrentObjectiveInteractionRadiusForVerification076",
                "objective interaction-radius gate");
            RequireSourceToken(smokeSource,
                "ObjectiveTraversalStallLimit076",
                "motor traversal stall guard");
            RequireSourceToken(smokeSource,
                "ObjectiveTraversalTimeoutSeconds076",
                "motor traversal real-time guard");
            RequireSourceToken(smokeSource,
                "\"Union Focus Chip \"",
                "visible Union focus input gate");
            RequireSourceToken(smokeSource,
                "\"Complete Forecast Order \"",
                "visible Union Forecast input gate");
            ForbidSourceToken(smokeSource,
                "TeleportToGuildHallForVerification071(",
                "Market teleport shortcut");
            ForbidSourceToken(smokeSource,
                "TeleportToCurrentObjectiveForVerification076(",
                "Lantern Road teleport shortcut");
            ForbidSourceToken(smokeSource,
                "_coordinator.SelectForecast(",
                "direct coordinator Forecast-selection shortcut");
            ForbidSourceToken(smokeSource,
                "TeleportTo",
                "any certification teleport shortcut");
            ForbidSourceToken(smokeSource,
                ".SelectForecast(",
                "any direct Forecast-selection shortcut");
            ForbidSourceToken(smokeSource,
                "priorBeatCount076",
                "started-beat live Art shortcut");
            ForbidSourceToken(smokeSource,
                "RequestSkipCurrentRound(",
                "built-player battle-round presentation skip shortcut");
            RequireSourceToken(smokeSource,
                "sequence076.LastPresentedBeatCount > 0 &&",
                "nonempty accelerated battle presentation gate");
            RequireSourceToken(smokeSource,
                "sequence076.LastPresentedBeatCount <=",
                "bounded accelerated battle presentation gate");
            RequireSourceToken(smokeSource,
                "controller076.OwnedSequenceDirector078",
                "controller-owned battle sequence certification binding");
            RequireSourceToken(smokeSource,
                "controller076.OwnedDiorama078",
                "controller-owned battle diorama certification binding");
            ForbidSourceToken(smokeSource,
                ".GetComponentInChildren<M2BattleSequenceDirector072>",
                "ambiguous stale battle-sequence lookup");
            ForbidSourceToken(smokeSource,
                ".FindFirstObjectByType<M2BattleDioramaView072>",
                "ambiguous stale battle-diorama lookup");
            RequireSourceToken(smokeSource,
                "HasVisibleZeroHpDownedPresentation078",
                "terminal Gate-Eater zero-HP DOWNED presentation gate");
            RequireSourceToken(smokeSource,
                "battle_3_gate_eater_lethal_hp_zero_downed_before_results",
                "terminal Gate-Eater pre-results evidence frame");
            RequireSourceToken(smokeSource,
                "renderedScreenshotValidationVerified",
                "rendered screenshot validation gate");
            RequireSourceToken(smokeSource,
                "recipes076.Count == 120",
                "complete 120-Art smoke enumeration");
            RequireSourceToken(smokeSource,
                "VerifyPersistedStoryUnionPlans078(chapterTwoReloaded.State);",
                "legal persisted story Union-plan gate");
            RequireSourceToken(smokeSource,
                "VerifyMaximumCampaignBattleCapacity078();",
                "built-player ten-Union by six-member capacity gate");
            RequireSourceToken(smokeSource,
                "RosterCapacityAtDormitoryLevel(3)",
                "organic Guild Housing progression reaches the sixty-member field target");
            RequireSourceToken(smokeSource,
                "rescuedPatrolMemberIds078.SetEquals(deployedPatrolMemberIds078)",
                "all ten rescued patrol members deployed gate");
            RequireSourceToken(smokeSource,
                "rescuedPatrolMemberIds078.All(actionMemberIds078.Contains)",
                "all ten rescued patrol members receive actions gate");
            RequireSourceToken(smokeSource,
                "_report.claimedBattleRewardCount == 5",
                "five claimed encounter rewards gate");
            RequireSourceToken(smokeSource,
                "_report.battleOrdersReadiedVerified",
                "all active Union orders readied evidence gate");
            RequireSourceToken(smokeSource,
                "_report.battleExchangeResolvedVerified",
                "authoritative battle exchange evidence gate");
            RequireSourceToken(smokeSource,
                "_report.battleHpImpactPresentationVerified",
                "real mid-impact visible HP-change gate");
            RequireSourceToken(smokeSource,
                "HpImpactPresentationCount076",
                "mid-impact HP presentation-count boundary");
            RequireSourceToken(smokeSource,
                "TryParseSignedHpImpact076(",
                "signed HP-delta evidence parser");
            RequireSourceToken(smokeSource,
                "TryParseBeforeAfterHpImpact076(",
                "HP before-to-after evidence parser");
            RequireSourceToken(smokeSource,
                "PresentedCurrentHp076 !=",
                "visible actor HP-change assertion");
            RequireSourceToken(smokeSource,
                "battle_1_first_exact_hp_impact_readout",
                "first exact-impact screenshot evidence");
            RequireSourceToken(smokeSource,
                "afterHp076 < beforeHp076",
                "strict damaging HP-loss gate");
            RequireSourceToken(smokeSource,
                "PresentedHpFill076 <",
                "visibly shortened target HP-fill gate");
            RequireSourceToken(smokeSource,
                "battle_learned_art_notice_visible",
                "in-round learned-Art notice screenshot");
            RequireSourceToken(smokeSource,
                "battle_learned_art_notice_cleared",
                "cleared learned-Art notice screenshot");
            RequireSourceToken(smokeSource,
                "transientLearnedArtNoticeVerified",
                "bounded learned-Art notification gate");
            RequireSourceToken(smokeSource,
                "source078.OnBeginDrag(moveEvent078);",
                "shipping Union drag component invocation");
            RequireSourceToken(smokeSource,
                "target078.OnDrop(moveEvent078);",
                "shipping Union drop-target invocation");
            RequireSourceToken(smokeSource,
                "union_planner_six_slots_before_drag",
                "six visible Union-slot screenshot");
            RequireSourceToken(smokeSource,
                "union_planner_after_shipping_drop",
                "authoritative post-drop Union screenshot");
            RequireSourceToken(smokeSource,
                "guildCityVisualProofVerified",
                "twelve-plot staffed-person visual gate");
            RequireSourceToken(smokeSource,
                "chapterTwoObjectiveAndEvidenceVerified",
                "Sella-Orra Chapter 2 evidence-copy gate");
            RequireSourceToken(smokeSource,
                "_report.totalClaimedProgressionReceiptCount == 7",
                "two completed operations plus five encounter progression receipts gate");
            RequireSourceToken(smokeSource,
                "_report.resolvedCheckCount == 12",
                "six Chapter 1 plus six authored Chapter 2 decision receipts gate");
            RequireSourceToken(smokeSource, "screenshotSha256", "screenshot hash evidence gate");
            RequireSourceToken(smokeSource, "ENCOUNTER071_HALL_BREACH", "Hall Breach battle gate");
            RequireSourceToken(smokeSource,
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                "Lantern Road battle gate");
            RequireSourceToken(smokeSource, "ENCOUNTER071_GATE_EATER", "Gate-Eater battle gate");
            RequireSourceToken(smokeSource,
                "ENCOUNTER_FOG_STALKERS_STANDARD",
                "Chapter 2 Fog Stalkers battle gate");
            RequireSourceToken(smokeSource,
                "ENCOUNTER_SURVEYOR_RESCUE",
                "Chapter 2 Surveyor Rescue climax battle gate");
            RequireSourceToken(smokeSource,
                "chapter_2_surveyors_at_door_rescue",
                "pre-climax Surveyor Rescue objective screenshot");
            RequireSourceToken(smokeSource,
                "battle_5_chapter_2_surveyor_rescue",
                "Surveyor Rescue battle and reward screenshot pair");
            RequireSourceToken(smokeSource,
                "chapter_2_testimony_to_skyhome_cliffhanger",
                "authored Sella-Orra return cliffhanger screenshot");
            RequireSourceToken(smokeSource,
                "chapter_3_hall_objective_after_surveyor_rescue",
                "clear Chapter 3 Hall objective screenshot");
            RequireSourceToken(smokeSource,
                "CertifyProjectedCampaign023Boards084",
                "packaged projected C023 board proof");
            RequireSourceToken(smokeSource,
                "SmokeChapterBoardId084 = \"CH018_032\"",
                "authored C023 chapter-board smoke fixture");
            RequireSourceToken(smokeSource,
                "SmokeLongestRepeatableBoardId084 =",
                "longest C023 repeatable-board smoke fixture");
            RequireSourceToken(smokeSource,
                "\"REPEAT020_SKYHOME_04\"",
                "twelve-room C023 packaged smoke board");
            RequireSourceToken(smokeSource,
                "campaign023_authored_chapter_objective",
                "mission-specific C023 chapter-objective screenshot");
            RequireSourceToken(smokeSource,
                "campaign023_authored_chapter_mission_brief",
                "mission-specific C023 chapter mission-brief screenshot");
            RequireSourceToken(smokeSource,
                "campaign023_longest_12_room_contract_objective",
                "mission-specific C023 contract-objective screenshot");
            RequireSourceToken(smokeSource,
                "campaign023_longest_12_room_contract_track",
                "longest C023 contract-track screenshot");
            RequireSourceToken(smokeSource,
                "ProjectedExpeditionCards089(",
                "built-player C023 three-card fixture");
            RequireSourceToken(smokeSource,
                "CountActiveButtonsNamed076(\"Blind Quest Card Back \") == 3",
                "built-player exact three blind-card interaction proof");
            RequireSourceToken(smokeSource,
                "CountActiveNamedObjects076(\"Expedition card face \") == 0",
                "built-player card faces remain hidden before selection");
            RequireSourceToken(smokeSource,
                "FindActiveButtonByName081(\"Move forward World Gate room 084\") == null",
                "built-player rejection of the retired automatic C023 card");
            RequireSourceToken(smokeSource,
                "c023_three_card_chapter_and_twelve_room_contract_at_certified_resolution",
                "built-player C023 three-card certification gate");
            RequireSourceToken(smokeSource,
                "A future C023 room title leaked before its tile was revealed",
                "C023 future-room secrecy assertion");
            RequireSourceToken(smokeSource,
                "Raw C023 authority token leaked into player copy",
                "C023 raw-authority-token rejection");
            RequireSourceToken(smokeSource,
                "_report.chapterTwoRouteNodeId = \"N14\";",
                "completed Chapter 2 return-node receipt");
            RequireSourceToken(smokeSource,
                "_report.nextObjective = \"Chapter 3: Keep Skyhome's relief road open\";",
                "clear post-slice Chapter 3 objective receipt");
            RequireSourceToken(smokeSource,
                "chapter_2_surveyor_rescue_return_and_chapter_3_objective",
                "Chapter 2 return and clear Chapter 3 objective gate");
        }

        private static void ValidateOvernight089ContractOrThrow(string projectRoot)
        {
            foreach (var assetPath in Overnight089ProductionFiles.Where(value =>
                         value.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null || sprite.texture == null ||
                    sprite.texture.width <= 0 || sprite.texture.height <= 0)
                    throw new InvalidOperationException(
                        "Release 089 production art is not importable as a Sprite: " +
                        assetPath + ".");
            }

            var adventureBoardSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign023/GuildCityAdventureBoard084.cs");
            var expeditionUiSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign023/M1FlowPresenter.ExpeditionDeck089.cs");
            var expeditionCoordinatorSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign023/M1RuntimeCoordinator.Campaign023.cs");
            var expeditionMotionSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign023/ExpeditionRouteCardMotion089.cs");
            var campaignWorldGateModelsSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Campaign023/CampaignWorldGateModels023.cs");
            var expeditionCommandSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Campaign023/ExpeditionDeckCommandService089.cs");
            var heroIntegrationSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300CreatorIntegration087.cs");
            var heroApplicantLeadSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300ApplicantLead089.cs");
            var heroDeepProgressionSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Recruitment/HeroMaster300/HeroMaster300DeepProgressionAdapter089.cs");
            var recruitmentServiceSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityRecruitmentService017D.cs");
            var progressionStateSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/State/ProgressionState.cs");
            var mainCoordinatorSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/M1RuntimeCoordinator.cs");
            var recruitmentDeskSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.RecruitmentDesk074.cs");
            var presentationContractsSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/M1PresentationContracts.cs");
            var expeditionTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionDeck089Tests.cs");
            var expeditionPresentationTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionDeck089PresentationContractTests.cs");
            var expeditionRecruitTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionRecruitLeadApplicant089Tests.cs");
            var cardImportTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionCardFace089ImportTests.cs");
            var artLevelTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/M2ArtLevelPresentation089Tests.cs");
            var expeditionPresentationPlayTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/PlayMode/ExpeditionDeck089PresentationPlayModeTests.cs");
            var chapterTwoBattlePlayTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/PlayMode/M2ChapterTwoBattlePresentation079PlayModeTests.cs");
            var firstHourSmokeSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.cs");
            var recruitAscensionSmokeSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.RecruitAscension089.cs");
            var smokePresenterSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/M1FlowPresenter.FirstHourGoldSmoke071.cs");
            var smokeIsolationTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/FirstHourGoldSmokeIsolation078Tests.cs");
            var heroPromotionManifestSource = ReadProjectSource(projectRoot,
                "Assets/Resources/SecondDimension/HeroMaster300/Data/HERO_300_REVIEW_SPRITE_PROMOTION_089.json");
            var heroPromotionTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/HeroMasterReviewSpritePromotion089Tests.cs");
            var heroVisualSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/M1VisualAssets.cs");
            var enemyRigSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleActorRig072.cs");
            var enemyThreatSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2EnemyThreatPalette089.cs");
            var enemyVarietyTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/EnemyEncounterVariety089AuditTests.cs");
            var enemyThreatTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/M2EnemyThreatPalette089Tests.cs");
            var defeatedTargetEditTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/M2DefeatedTargetRetarget088Tests.cs");
            var defeatedTargetPlayTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/PlayMode/M2DefeatedTargetRetarget088PlayModeTests.cs");

            RequireSourceToken(adventureBoardSource,
                "BuildExpeditionDeckHelp089(body, coordinator, state);",
                "active campaign Expedition Deck help and tutorial");
            RequireSourceToken(adventureBoardSource,
                "BuildExpeditionRouteRow089(body, deckCoordinator, state);",
                "active shuffled three-card campaign choice row");
            RequireSourceToken(expeditionUiSource,
                ".Take(3).ToArray();",
                "three-card-only route presentation");
            RequireSourceToken(expeditionUiSource,
                "coordinator.CommitExpeditionRouteCard089(captured.CardId)",
                "saved route-card choice authority");
            RequireSourceToken(expeditionUiSource,
                "Expedition card possible results ",
                "pre-commit success and setback result presentation");
            RequireSourceToken(expeditionUiSource,
                "ExpeditionRouteCardMotion089",
                "physical route-card hover and press motion");
            RequireSourceToken(expeditionMotionSource,
                "IPointerEnterHandler",
                "route-card pointer motion without alternate command authority");
            RequireSourceToken(expeditionMotionSource,
                "_motionEnabled = motionEnabled",
                "reduced-motion opt-out for route cards");
            RequireSourceToken(adventureBoardSource,
                "if (!state.ExpeditionDeckTutorialSeen)",
                "tutorial-first route-card disclosure");
            RequireSourceToken(adventureBoardSource,
                "BuildExpeditionDeckHelp089(body, coordinator, state);",
                "tutorial card rendered before first route disclosure");
            RequireSourceToken(adventureBoardSource,
                "RunWorldGateAnimatedCommand084",
                "single-authority animated World Gate command handoff");
            RequireSourceToken(adventureBoardSource,
                "WorldGateRoomResultHold084 = 0.35f",
                "readable room-result hold timing");
            RequireSourceToken(adventureBoardSource,
                "RETRY SAVED RESULT",
                "explicit saved-receipt retry recovery");
            RequireSourceToken(expeditionCoordinatorSource,
                "state.PendingCardCategory=card==null?string.Empty:",
                "chosen card category retained through reveal");
            RequireSourceToken(expeditionCoordinatorSource,
                "state.PendingCardTitle=card?.Title??string.Empty;",
                "chosen card title retained through reveal");
            RequireSourceToken(expeditionPresentationPlayTestsSource,
                "UnseenTutorialHidesRouteCardsUntilTheFinalAcknowledgement089",
                "tutorial-first route-card presentation certification");
            RequireSourceToken(expeditionPresentationPlayTestsSource,
                "SynchronousCoordinatorChangedCannotEraseTheLiveRouteFlip089",
                "live physical flip survives synchronous state change");
            RequireSourceToken(expeditionPresentationPlayTestsSource,
                "RevealedResultUsesTheChosenCardTitleFaceAndCategory089",
                "chosen room identity survives reveal certification");
            RequireSourceToken(expeditionPresentationPlayTestsSource,
                "FailedSavedReceiptWaitsForExplicitRetryAndThenContinues",
                "saved-result failure retry certification");
            RequireSourceToken(expeditionPresentationPlayTestsSource,
                "InterruptedReceiptScheduleIsReleasedForPresenterReEntry",
                "presenter re-entry receipt recovery certification");
            RequireSourceToken(campaignWorldGateModelsSource,
                "ExpeditionRecruitLeadIds089=Copy(expeditionRecruitLeadIds089)",
                "saved exact Expedition recruit-lead authority");
            RequireSourceToken(expeditionCommandSource,
                "expeditionRecruitLeadIds089: leads.AsReadOnly()",
                "earned recruit-card lead persistence");
            RequireSourceToken(heroIntegrationSource,
                "BuildExpeditionApplicantRecord089(",
                "Hero Master applicant conversion authority");
            RequireSourceToken(heroApplicantLeadSource,
                "public static ApplicantSnapshotState ToApplicant(",
                "exact Hero Master lead-to-applicant adapter");
            RequireSourceToken(heroDeepProgressionSource,
                "var profile = _generator.Generate(recruit);",
                "canonical generated-tree adapter source");
            RequireSourceToken(heroDeepProgressionSource,
                "TryUnlockNextEarnableTree089",
                "post-cap canonical tree unlock path");
            RequireSourceToken(recruitmentServiceSource,
                "return CommitEarnedBoard124(campaign, refresh: false);",
                "Applicant Board imports earned Expedition leads without a refresh charge");
            RequireSourceToken(recruitmentServiceSource,
                "return CommitEarnedBoard124(campaign, refresh: true);",
                "explicit recruitment refresh uses the same earned-lead authority");
            RequireSourceToken(recruitmentServiceSource,
                "var board = MergeExpeditionRecruitLeads089(campaign, city.RecruitmentBoard);",
                "earned contacts merge into the existing committed Applicant Board");
            RequireSourceToken(recruitmentServiceSource,
                "if (ReferenceEquals(board, city.RecruitmentBoard))",
                "unchanged earned-lead merge preserves state before charging or advancing refresh");
            RequireSourceToken(recruitmentServiceSource,
                "var pending = PendingExpeditionRecruitLeads089(campaign, board);",
                "Applicant Board takes contacts from the saved earned-lead queue");
            RequireSourceToken(recruitmentServiceSource,
                "_heroMasterCatalog089.TryGetAcceptedHero(leadId, out var hero)",
                "earned contacts resolve their exact accepted Hero Master identity");
            RequireSourceToken(recruitmentServiceSource,
                "!hero.IsNormalApplicantEligible",
                "earned contacts preserve canonical normal-applicant eligibility");
            RequireSourceToken(recruitmentServiceSource,
                "applicants.Add(HeroMaster300ApplicantLead089.ToApplicant(",
                "earned contacts use the existing exact Hero Master applicant adapter");
            ForbidSourceToken(recruitmentServiceSource,
                "new DetailedApplicantBoardGenerator(",
                "recurring recruitment cannot mint an unauthored procedural pool");
            RequireSourceToken(recruitmentServiceSource,
                "owned.WithProgression(duplicate.ProjectedProgression)",
                "duplicate merge updates exact owned hero");
            RequireSourceToken(recruitmentServiceSource,
                "ConsumeExpeditionRecruitLead089(",
                "exact-once Expedition recruit-lead consumption");
            RequireSourceToken(recruitmentServiceSource,
                "var ascended = progression.Ascend089();",
                "ten-rank duplicate Ascension authority");
            RequireSourceToken(progressionStateSource,
                "public RecruitProgressionState Ascend089()",
                "bounded persisted Ascension progression");
            RequireSourceToken(progressionStateSource,
                "[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]",
                "legacy-save compatible Ascension serialization");
            RequireSourceToken(mainCoordinatorSource,
                "_recruitTreeProgression070);",
                "existing DeepProgression authority injected into recruitment");
            RequireSourceToken(mainCoordinatorSource,
                "IsAscensionMerge = isDuplicate",
                "distinct Ascension presentation projection");
            RequireSourceToken(mainCoordinatorSource,
                "DuplicateMergePreview = duplicate?.Summary",
                "pre-confirmation duplicate outcome preview");
            RequireSourceToken(mainCoordinatorSource,
                "EnemyThreatTier089 = union.Side == BattleSide.Enemy",
                "enemy threat grade projected into certified battle view");
            RequireSourceToken(recruitmentDeskSource,
                "Merge Hero Duplicate Primary 089",
                "Ascension merge control");
            RequireSourceToken(recruitmentDeskSource,
                "USES NO ROSTER SLOT",
                "plain-language duplicate roster-cost disclosure");
            RequireSourceToken(recruitmentDeskSource,
                "applicant.NextAscensionLevel",
                "next Ascension rank preview");
            RequireSourceToken(presentationContractsSource,
                "public int EnemyThreatTier089 { get; set; }",
                "certified battle threat-grade contract");
            RequireSourceToken(expeditionCoordinatorSource,
                "SecondDimension/Art/Board086/CardFaces/CARD_FACE_",
                "stable Expedition card-face resource binding");
            RequireSourceToken(expeditionTestsSource,
                "CurrentRow.Count, Is.EqualTo(3)",
                "three-card draw certification");
            RequireSourceToken(expeditionTestsSource,
                "RecruitCardsWeightNewHeroesThreeToOneAndExposeOneOwnedAscensionCopy",
                "earned-recruit and duplicate draw weighting certification");
            RequireSourceToken(expeditionPresentationTestsSource,
                "AscensionCopyKeepsRecruitGameplayAuthorityButProjectsDistinctSafeVisual",
                "Ascension retains recruit gameplay authority certification");
            foreach (var recruitCertification089 in new[]
                     {
                         "EveryAcceptedHeroMasterProfileUsesCanonicalGeneratedTreesForAllAuthoredAliases",
                         "ResolveRecruitCardPersistsExactLeadAndApplicantBoardSignsItOnce",
                         "LegacyProgressionDefaultsToZeroAscensionWithoutChangingCanonicalJson",
                         "TenDuplicateLeadsAscendExactHeroAcrossReloadWithoutRosterGrowthOrReplay",
                         "PostCapDuplicatesUnlockAssignedTreesThenLevelArtWithDeterministicFallback",
                         "PostCapHeroWithoutLegacyTreePlanStillProducesDuplicateForecast",
                         "GeneratedTreeAdapterRejectsUnknownAndUnassignedUnlocks"
                     })
                RequireSourceToken(expeditionRecruitTestsSource,
                    recruitCertification089,
                    "earned recruit/Ascension certification " + recruitCertification089);
            RequireSourceToken(cardImportTestsSource,
                "CardFaceIsAResourceLoadableSingleSprite089",
                "card-face import certification");
            RequireSourceToken(artLevelTestsSource,
                "EveryLevelIncreasesEffectAndMotionWithoutExceedingTheLevelTenCap",
                "visible Art level one-to-ten escalation certification");
            RequireSourceToken(artLevelTestsSource,
                "CommittedForecastIsTheOnlyLevelSource",
                "existing Forecast remains the Art-level authority");
            RequireSourceToken(chapterTwoBattlePlayTestsSource,
                "FogStalkerDioramaUsesWayglassPlateNewFamilyArtAndReadableThreatGrades089",
                "Chapter 2 keeps certified battle presentation and threat grades");
            RequireSourceToken(firstHourSmokeSource,
                "yield return CertifyEarnedRecruitAndAscension089();",
                "packaged smoke invokes earned-recruit and Ascension proof");
            RequireSourceToken(recruitAscensionSmokeSource,
                "CertifyEarnedRecruitAndAscension089()",
                "packaged earned-recruit and Ascension certification routine");
            foreach (var screenshot089 in new[]
                     {
                         "earned_recruit_card_exact_hero",
                         "earned_recruit_applicant_board",
                         "earned_ascension_card_exact_hero",
                         "recruitment_duplicate_ascension_preview"
                     })
                RequireSourceToken(recruitAscensionSmokeSource,
                    screenshot089,
                    "packaged recruit/Ascension screenshot " + screenshot089);
            foreach (var smokeGate089 in new[]
                     {
                         "earned_recruit_card_saves_exact_hero_master_lead",
                         "earned_recruit_lead_survives_production_save_reload",
                         "applicant_board_exposes_and_signs_same_exact_hero_once",
                         "another_earned_card_targets_owned_hero_for_ascension",
                         "live_recruitment_desk_renders_duplicate_merge_preview_and_control",
                         "duplicate_merge_advances_ascension_once_without_roster_growth"
                     })
                RequireSourceToken(firstHourSmokeSource,
                    smokeGate089,
                    "packaged recruit/Ascension gate " + smokeGate089);
            RequireSourceToken(smokePresenterSource,
                "ShowFirstHourGoldRecruitmentApplicant089",
                "live recruitment desk packaged-smoke presentation seam");
            RequireSourceToken(smokeIsolationTestsSource,
                "Release089RequiresSeventyTwoFramesAndEarnedRecruitAscensionProof089",
                "72-frame packaged recruit/Ascension smoke isolation certification");
            RequireSourceToken(heroPromotionManifestSource,
                "\"promoted_hero_count\": 57",
                "57 QA-pass and adapter-accepted Hero 300 sprite identities");
            RequireSourceToken(heroPromotionManifestSource,
                "\"accepted_catalog_hero_count\": 250",
                "all 250 adapter-accepted Hero 300 records");
            RequireSourceToken(heroPromotionManifestSource,
                "\"exact_unique_sprite_set_count\": 66",
                "66 honest same-identity exact hero sprite sets");
            RequireSourceToken(heroPromotionManifestSource,
                "\"deterministic_sprite_fallback_count\": 184",
                "184 explicitly labelled non-blank sprite-form fallbacks");
            RequireSourceToken(heroPromotionManifestSource,
                "\"rights_status\": \"PENDING_USER_RIGHTS_CONFIRMATION\"",
                "honest user-supplied Hero 300 review-build rights status");
            RequireSourceToken(heroPromotionTestsSource,
                "EveryPromotedHero089_ResolvesThroughExistingStableVisualAuthority",
                "Hero 300 portrait/standee/action binding certification");
            RequireSourceToken(heroPromotionTestsSource,
                "AllAcceptedHeroes089_ResolveSpriteFormUiAndBattleCoverage_WithoutMislabelingFallbacks",
                "all accepted Hero 300 records non-blank sprite-form certification");
            RequireSourceToken(heroVisualSource,
                "RUNTIME_HERO_SPRITE_FALLBACK_089",
                "explicitly labelled Hero 300 runtime fallback authority");
            RequireSourceToken(defeatedTargetEditTestsSource,
                "FinalEnemyDefeatStopsAllQueuedActionsAndResolvesVictoryImmediately088",
                "terminal defeated-target combat authority certification");
            RequireSourceToken(defeatedTargetPlayTestsSource,
                "AuthorityEventsReuseExistingVisibleBeatsWithoutPhantomPresentation088",
                "visible defeated-target combat presentation certification");
            RequireSourceToken(enemyThreatSource,
                "ResolveTowerTier",
                "ten-grade Tower threat presentation authority");
            RequireSourceToken(enemyThreatTestsSource,
                "TenThreatGradesMapEveryTowerFloorWithoutRandomTint089",
                "ten deterministic threat-grade certification");
            RequireSourceToken(enemyVarietyTestsSource,
                "AllThirtyEnemyAuthoritiesRenderAcrossMoreThanTwoHundredLiveThreatVariants089",
                "all Pass 03 enemy authorities live-render certification");
            RequireSourceToken(enemyVarietyTestsSource,
                "rendered.Add(rig.CurrentResourcePath + \"|\" + expectedTier);",
                "visual-path and colour combination proof");
            RequireSourceToken(enemyVarietyTestsSource,
                "Is.GreaterThan(200)",
                "more-than-two-hundred rendered-combination gate");

            foreach (var eliteArtName089 in new[]
                     {
                         "RUSTBACK_HOUND_LEADER_IDLE_089",
                         "HOLLOW_SALVAGER_LEADER_IDLE_089",
                         "SHARDWING_SIGNAL_QUEEN_IDLE_089",
                         "TOLLROAD_CUTTER_LEADER_IDLE_089",
                         "RIFT_MOLD_CROWN_IDLE_089",
                         "CHAINCALLER_IDLE_089",
                         "ECHO_STALKER_IDLE_089"
                     })
                RequireSourceToken(enemyRigSource, eliteArtName089,
                    "exact elite enemy visual binding " + eliteArtName089);
        }

        private static void ValidateRelease090ContractOrThrow(string projectRoot)
        {
            var buildSource = ReadProjectSource(projectRoot,
                "Assets/Editor/SecondDimension/Release072/FirstHourGoldWindowsBuild.cs");
            var smokeSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.cs");
            var smokeTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/FirstHourGoldSmokeIsolation078Tests.cs");
            var projectSettingsSource = ReadProjectSource(projectRoot,
                "ProjectSettings/ProjectSettings.asset");
            var guildCityContentSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityContent017D.cs");
            var questRuntimeSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityExpeditionService017D.QuestDeck090.cs");
            var questPresenterSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.QuestDeck090.cs");
            var blindChoiceSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign023/ExpeditionCardChoice091.cs");
            var campaignCardPresenterSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign023/M1FlowPresenter.ExpeditionDeck089.cs");
            var blindChoiceTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionBlindCardChoice091Tests.cs");
            var boardQuestPresenterSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.BoardQuest081.cs");
            var questContractsSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityPresentationContracts017D.cs");
            var coordinatorSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/M1RuntimeCoordinator.cs");
            var questGameplayTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/GuildQuestDeck090GameplayTests.cs");
            var questPresentationTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/BoardQuestDeck090PresentationTests.cs");
            var progressionSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/State/ProgressionState.cs");
            var recruitTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionRecruitLeadApplicant089Tests.cs");
            var expeditionDeckSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Campaign023/ExpeditionDeckService089.cs");
            var expeditionCommandSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Campaign023/ExpeditionDeckCommandService089.cs");
            var expeditionTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/ExpeditionDeck089Tests.cs");
            var towerSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionCommandService022.cs");
            var towerCoordinatorSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Campaign022/M1RuntimeCoordinator.Campaign022.cs");
            var battleSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/M2/M2BattleCommandService.cs");
            var threatPaletteSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2EnemyThreatPalette089.cs");
            var towerTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/TowerRun081EditModeTests.cs");
            var threatTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/M2EnemyThreatPalette089Tests.cs");
            var towerPlayTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/PlayMode/TowerBattleOnly088Tests.cs");
            var heroVisualSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/M1VisualAssets.cs");
            var heroPromotionManifestSource = ReadProjectSource(projectRoot,
                "Assets/Resources/SecondDimension/HeroMaster300/Data/HERO_300_REVIEW_SPRITE_PROMOTION_089.json");
            var heroPromotionTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/HeroMasterReviewSpritePromotion089Tests.cs");
            var menuSpriteSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour074/M1FlowPresenter.MenuSpritePresentation090.cs");
            var enemyArtIdentitySource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/M2/EnemyArtIdentity090.cs");
            var battleStateSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Gameplay/M2/BattleState.cs");
            var enemyArtRuntimeSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/EnemyArt700Runtime090.cs");
            var enemyArtImporterSource = ReadProjectSource(projectRoot,
                "Assets/Editor/SecondDimension/EnemyArt700TexturePostprocessor090.cs");
            var enemyArtBuildCopySource = ReadProjectSource(projectRoot,
                "Assets/Editor/SecondDimension/EnemyArt700WindowsBuildCopy090.cs");
            var actorRigSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/Experience/M2BattleActorRig072.cs");
            var cinematicBattleSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/M2CinematicBattlePresenter.cs");
            var lastRemnantStagingSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle/M2LastRemnantBattleStaging.cs");
            var battle3DSource = ReadProjectSource(projectRoot,
                "Assets/SecondDimension/Presentation/Battle3D/M2Battle3DWorld.cs");
            var enemyArtRuntimeTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/EnemyArt700Runtime090Tests.cs");
            var enemyArtGroundingTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/EnemyArt700Battle3DGroundContact090Tests.cs");
            var enemyArtIdentityTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/EnemyArtIdentity090Tests.cs");
            var enemyArtBattleTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/EditMode/M2BattleForecastTests.cs");
            var enemyArtPresentationTestsSource = ReadProjectSource(projectRoot,
                "Assets/Tests/PlayMode/EnemyArt700BattlePresentation090PlayModeTests.cs");

            ValidateEnemyArt700PayloadOrThrow090(projectRoot);

            RequireSourceToken(buildSource,
                "public const string BuildId = \"SECOND-DIMENSION-ALPHA-132\";",
                "Release 090 build identity");
            RequireSourceToken(buildSource,
                "public const string BuildLabel = \"ALPHA 132\";",
                "Release 090 player-facing build label");
            RequireSourceToken(buildSource,
                "public const string PlayerVersion = \"0.132.0-alpha\";",
                "Release 090 player version");
            RequireSourceToken(buildSource,
                "SECOND DIMENSION ALPHA 132 WINDOWS BUILD PASS",
                "Release 090 completion marker");
            RequireSourceToken(smokeSource,
                "ExpectedBuildId = \"SECOND-DIMENSION-ALPHA-132\";",
                "Release 090 packaged-smoke build identity");
            RequireSourceToken(smokeSource,
                "ExpectedPlayerVersion = \"0.132.0-alpha\";",
                "Release 090 packaged-smoke player version");
            RequireSourceToken(smokeSource, "threeCardQuestChoiceVerified",
                "Release 090 packaged-smoke three-card proof field");
            RequireSourceToken(smokeSource, "WaitForQuestCardDraft090(",
                "Release 090 packaged smoke waits for the physical three-card deal");
            RequireSourceToken(smokeSource,
                "SelectQuestCardForDestinationForVerification090(",
                "Release 090 packaged smoke follows the authored route through a legal card");
            RequireSourceToken(smokeSource, "WaitForBoardQuestCardResult090(",
                "Release 090 packaged smoke observes the card result before continuing");
            RequireSourceToken(smokeSource,
                "RequireQuestCardRounds090(12, \"Chapter 1\")",
                "packaged Chapter 1 twelve-card commit proof");
            RequireSourceToken(smokeSource,
                "RequireQuestCardRounds090(10, \"Chapter 2\")",
                "packaged Chapter 2 ten-card commit proof");
            RequireSourceToken(smokeSource,
                "three_card_choose_one_quest_deck_ch1_12_ch2_10_saved_receipts",
                "saved three-card Chapter 1 and Chapter 2 gate-ledger proof");
            ForbidSourceToken(smokeSource,
                "five_card_one_tap_room_flip_quest_loop_over_authored_route",
                "retired one-tap Board Quest gate-ledger wording");
            RequireSourceToken(smokeSource, "\"Choose Board Quest Card \"",
                "Release 090 packaged smoke drives the shipping card buttons");
            ForbidSourceToken(smokeSource, "departureActions081.Length == 1",
                "retired one-button departure assumption in Release 090 packaged smoke");
            RequireSourceToken(smokeTestsSource,
                "QuestDeckSmokeSelectsSafeUsableCardForExactStoryDestination090",
                "Release 090 packaged-smoke exact route-card selector certification");
            RequireSourceToken(projectSettingsSource,
                "bundleVersion: 0.132.0-alpha",
                "Release 090 serialized Unity player version");

            RequireSourceToken(questRuntimeSource,
                "public const int MinimumQuestCardRounds090 = 10;",
                "first-hour minimum ten-card-round authority");
            RequireSourceToken(guildCityContentSource,
                "MinimumQuestCardMovesToExit090(",
                "real shortest-route card-round validator");
            RequireSourceToken(guildCityContentSource,
                "ValidateMinimumQuestCardRoute090(",
                "shipping opening-board ten-card invariant");
            RequireSourceToken(questRuntimeSource, "QuestCardSchedule090",
                "first-hour authored three-card schedule");
            RequireSourceToken(questRuntimeSource, "BuildQuestCardRow090(",
                "first-hour deterministic three-card deal authority");
            RequireSourceToken(questRuntimeSource, "CommitQuestCard090(",
                "first-hour saved card-resolution authority");
            RequireSourceToken(questRuntimeSource, "\"OPTIONAL_ELITE\"",
                "locked optional-elite fight-or-skip preservation");
            RequireSourceToken(questRuntimeSource,
                "Applicant = QuestApplicantIdentity090(campaign, applicant)",
                "recruit offer identity bound into first-hour card ID");
            RequireSourceToken(questRuntimeSource,
                "PermanentBoonHero = permanentBoon",
                "permanent-boon hero bound into first-hour card ID");
            RequireSourceToken(questRuntimeSource,
                "FateCheckModifier = QuestCardRunCheckModifier090(",
                "saved boon and scar modifier applied to first-hour fate checks");
            RequireSourceToken(questRuntimeSource, "CreateQuestCardEquipment090(",
                "first-hour chest and merchant equipment authority");
            RequireSourceToken(questRuntimeSource, "recruitment.SignApplicant(",
                "existing recruitment signing authority reused by quest cards");
            RequireSourceToken(questRuntimeSource, "DescribeHeroMasterDuplicate089(",
                "duplicate Hero Master merge forecast reused by quest cards");
            RequireSourceToken(questRuntimeSource, "new EncounterLaunchRequest017D(",
                "optional quest-card battle uses the certified encounter bridge");

            RequireSourceToken(boardQuestPresenterSource,
                "ShouldBuildBoardQuestCardDraft090(coordinator, state, view)",
                "first-hour board flow selects the QuestDeck presentation");
            RequireSourceToken(questPresenterSource, "BuildBoardQuestCardDraft090(",
                "full-width first-hour three-card draft");
            RequireSourceToken(questPresenterSource, "Take(3)",
                "first-hour presentation exposes exactly three choices");
            RequireSourceToken(questPresenterSource, "blindChoice.Register091(",
                "first-hour draft bound to deliberate blind-card selection");
            RequireSourceToken(campaignCardPresenterSource, "blindChoice.Register091(",
                "later campaign draft uses the same blind-card selection");
            RequireSourceToken(blindChoiceSource, "face.gameObject.SetActive(false)",
                "hidden card fronts before player choice");
            RequireSourceToken(blindChoiceSource, "Mathf.Abs(Mathf.Cos(t * Mathf.PI))",
                "selected physical card turns edge-on before its reveal");
            RequireSourceToken(blindChoiceTestsSource,
                "DealingNeverRevealsAnOfferOrCommitsWithoutPlayerInput091",
                "blind deal cannot auto-select or auto-commit");
            RequireSourceToken(blindChoiceTestsSource,
                "BlindPickLocksOtherCardsThenRevealsOnlyChosenCardBeforeExactAction091",
                "selected-only reveal and exactly-once revealed action certification");
            RequireSourceToken(blindChoiceTestsSource,
                "OptionalOrLockedOfferCanReturnWithoutSpendingAndStaysKnown091",
                "blind optional purchase cannot spend undisclosed XP");
            RequireSourceToken(questPresenterSource, "BuildAuthoritativeDiceRoll084(",
                "physical authoritative 2D6 first-hour resolution");
            RequireSourceToken(questPresenterSource, "+ CARD ROUNDS THIS QUEST",
                "plain-language first-hour quest length promise");
            RequireSourceToken(questContractsSource, "QuestCards090",
                "QuestDeck presentation-state contract");
            RequireSourceToken(questContractsSource, "CommitBoardQuestCard090(string cardId)",
                "QuestDeck presentation command contract");
            RequireSourceToken(coordinatorSource, ".BuildQuestCardRow090(",
                "runtime coordinator QuestDeck projection wiring");
            RequireSourceToken(coordinatorSource, ".CommitQuestCard090(",
                "runtime coordinator QuestDeck commit wiring");
            RequireSourceToken(menuSpriteSource, "DecorateLivingGuildFacility090(",
                "illustrated phone-readable Guild facility presentation");
            RequireSourceToken(menuSpriteSource, "AddMenuStandeeToButton090(",
                "sprite-form hero menu presentation");

            foreach (var questCertification090 in new[]
                     {
                         "TenRoundsExposeThirtyDeterministicChoicesBesideAuthoredBattles090",
                         "UnclearedOptionalEliteKeepsItsFightOrSkipChoiceInsteadOfDeck090",
                         "RecruitCardIdBindsTheCurrentApplicantAndRejectsAStaleBoard090",
                         "PermanentBoonCardIdBindsTheHeroItWillRaise090",
                         "FateCardStaysNeutralUntilCommitAndUsesSavedRunModifier090",
                         "ChestCommitsExactPowerReceiptAndSavePersistentEquipment090",
                         "MerchantChargesItsDisplayedExactXpAndLocksWhenOneXpShort090",
                         "HeroicBreakthroughPermanentlyRaisesTheSelectedRealHero090",
                         "RecruitCardUsesTheExistingSigningAuthority090",
                         "BattleCardCommitsBridgeAuthorityWithoutClearingLockedStoryBattle090",
                         "EveryAdvertisedOpeningQuestHasTenActualCardMovesOnShortestRoute090"
                     })
                RequireSourceToken(questGameplayTestsSource, questCertification090,
                    "first-hour QuestDeck certification " + questCertification090);
            foreach (var presentationCertification090 in new[]
                     {
                         "FullWidthDraftHidesThreeOffersUntilDeliberatePick091",
                         "ThreeCardDealFocusesFirstBlindCardWithoutRevealingIt091",
                         "QuestDeckCommandIsOptionalAndProductionImplementsIt090",
                         "SavedQuestBoonsAndScarsReachTheSubmittedDiceModifier090"
                     })
                RequireSourceToken(questPresentationTestsSource,
                    presentationCertification090,
                    "first-hour QuestDeck presentation certification " +
                    presentationCertification090);

            RequireSourceToken(progressionSource,
                "public static class RecruitAscensionRules089",
                "Hero Master duplicate Ascension policy");
            RequireSourceToken(progressionSource,
                "public const int MaximumLevel = 10;",
                "ten-rank Hero Master duplicate Ascension cap");
            RequireSourceToken(recruitTestsSource,
                "TenDuplicateLeadsAscendExactHeroAcrossReloadWithoutRosterGrowthOrReplay",
                "ten-rank duplicate Ascension save/replay certification");
            RequireSourceToken(recruitTestsSource,
                "PostCapDuplicatesUnlockAssignedTreesThenLevelArtWithDeterministicFallback",
                "post-cap duplicate tree and Art progression certification");

            RequireSourceToken(expeditionDeckSource,
                "public const int ChoiceRowsPerNonBattleNode089 = 2;",
                "Campaign023 two three-card rows per nonbattle room");
            RequireSourceToken(expeditionDeckSource,
                "public const int MinimumQuestChoiceRounds089 = 10;",
                "Campaign023 minimum ten card-round choices");
            RequireSourceToken(expeditionDeckSource,
                "CardsPerNodeForProgressionTier089(",
                "Campaign023 progression-driven deck growth");
            RequireSourceToken(expeditionDeckSource, "UnlockedCategoriesForTier089(",
                "Campaign023 persisted deck-category growth");
            RequireSourceToken(expeditionDeckSource, "MerchantEquipmentReward089(",
                "Campaign023 exact merchant equipment preview");
            RequireSourceToken(expeditionDeckSource, "ChestCarriesRecruitLead089(",
                "Campaign023 rare chest recruit-lead authority");
            RequireSourceToken(expeditionDeckSource,
                "EXPEDITION_CHEST_RECRUIT_CHANCE_089",
                "Campaign023 deterministic one-in-sixteen chest recruit chance");
            RequireSourceToken(expeditionCommandSource,
                ".MerchantEquipmentReward089(selectedCard.CardId,",
                "Campaign023 merchant purchase commit wiring");
            RequireSourceToken(expeditionCommandSource,
                "progression.WithUnlockedTrees(traits.AsReadOnly())",
                "Campaign023 permanent hero boon persistence");
            RequireSourceToken(expeditionCommandSource,
                "SynchronizeOptionalBattleAfterClaim089(",
                "Campaign023 optional battle return synchronization");
            RequireSourceToken(expeditionCommandSource,
                "new EncounterLaunchRequest017D(",
                "Campaign023 optional battle certified encounter bridge");
            RequireSourceToken(expeditionCommandSource,
                "EXPEDITION_OPTIONAL_BATTLE_089",
                "Campaign023 optional battle route identity");
            foreach (var expeditionCertification090 in new[]
                     {
                         "EveryAuthoredQuestPathHasAtLeastTenCardChoicesPlusLockedBattles",
                         "ChestEquipmentSpansCommonThroughGodlyAndIsManualProgressionGear",
                         "ProgressionTierIsPureSavedAndExpandsTheAuthoredDeck",
                         "MerchantSpendsExactTreasuryXpAndGrantsPreviewedItemOnce",
                         "PermanentHeroBoonUsesRecruitProgressionAndAppliesOnce",
                         "RareChestRecruitLeadIsPreviewedAndPersistedExactlyOnce",
                         "OptionalBattleUsesCertifiedBridgeAndReturnsToSameCardRoomOnce"
                     })
                RequireSourceToken(expeditionTestsSource,
                    expeditionCertification090,
                    "Campaign023 Release 090 certification " +
                    expeditionCertification090);

            RequireSourceToken(towerSource,
                "public const int TowerMajorRecruitInterval089=50;",
                "Tower major recruit every fifty clears");
            RequireSourceToken(towerSource,
                "public const int MaximumTowerThreatTier089=10;",
                "Tower ten-tier combat authority");
            RequireSourceToken(towerSource, "TowerThreatTier089(int floorNumber)",
                "Tower floor-to-threat authority");
            RequireSourceToken(towerSource, "IsTowerMajorRecruitMilestone089(int totalClears)",
                "Tower floor-50 milestone authority");
            RequireSourceToken(towerCoordinatorSource,
                ".IsTowerMajorRecruitMilestone089(",
                "Tower milestone reward presentation wiring");
            RequireSourceToken(battleSource, "ApplyTowerThreatPower089(",
                "Tower power scaling remains inside certified Union combat");
            RequireSourceToken(threatPaletteSource, "public const int MaximumTier = 10;",
                "ten deterministic enemy colour grades");
            RequireSourceToken(threatTestsSource,
                "TenThreatGradesMapEveryTowerFloorWithoutRandomTint089",
                "ten-grade Tower palette certification");
            RequireSourceToken(towerTestsSource,
                "TenTowerThreatTiersHaveDeterministicCertifiedPowerScaling089",
                "ten-tier certified Tower power test");
            RequireSourceToken(towerTestsSource,
                "EveryFiftiethTowerClearBanksOneSavedSRankRecruitLead089",
                "floor-50 exact-once recruit reward certification");
            foreach (var battleOnlyCertification090 in new[]
                     {
                         "LobbyOffersOneBattleAndNoCardRoute088",
                         "BattleReadyOffersFightWithoutCards088",
                         "VictoryOffersBankWithoutRevealCard088",
                         "DefeatOffersReturnWithoutCards088"
                     })
                RequireSourceToken(towerPlayTestsSource,
                    battleOnlyCertification090,
                    "Tower battle-only presentation certification " +
                    battleOnlyCertification090);

            RequireSourceToken(heroVisualSource,
                "RUNTIME_HERO_SPRITE_FALLBACK_089",
                "explicit non-blank Hero Master sprite fallback authority");
            RequireSourceToken(heroPromotionManifestSource,
                "\"accepted_catalog_hero_count\": 250",
                "all accepted Hero Master records covered by sprite presentation");
            RequireSourceToken(heroPromotionManifestSource,
                "\"deterministic_sprite_fallback_count\": 184",
                "honestly labelled Hero Master fallback coverage");
            RequireSourceToken(heroPromotionTestsSource,
                "AllAcceptedHeroes089_ResolveSpriteFormUiAndBattleCoverage_WithoutMislabelingFallbacks",
                "all accepted Hero Master sprite-form coverage certification");

            RequireSourceToken(enemyArtIdentitySource,
                "public const int BaseFamilyCount = 70;",
                "Enemy Art 700 exact family address space");
            RequireSourceToken(enemyArtIdentitySource,
                "public const int VariantsPerBase = 10;",
                "Enemy Art 700 exact per-family variant address space");
            RequireSourceToken(enemyArtIdentitySource, "CommitIdentities090(",
                "saved deterministic Enemy Art identity routing");
            RequireSourceToken(battleSource,
                "EnemyArtIdentity090.CommitIdentities090(enemies, battleId)",
                "Enemy Art identity commitment inside certified Union combat");
            RequireSourceToken(battleStateSource, "WithEnemyArt090(",
                "Enemy Art identity persistence across immutable battle-state copies");
            RequireSourceToken(battleStateSource, "InitialIntegrityStateHash090",
                "separate persisted initial battle-art integrity hash");
            RequireSourceToken(battleStateSource, "FinalIntegrityStateHash090",
                "separate persisted terminal battle-art integrity hash");
            RequireSourceToken(battleSource, "GameplayRngStateHash090(",
                "art-stripped deterministic battle gameplay hash");
            RequireSourceToken(battleSource, "HasValidFinalStateHash090(",
                "legacy-compatible dual terminal battle hash validation");
            RequireSourceToken(coordinatorSource,
                "EnemyArtBaseId090 = recruit == null",
                "Enemy Art base identity projection into battle presentation");
            RequireSourceToken(coordinatorSource,
                "EnemyArtVariantId090 = recruit == null",
                "Enemy Art variant identity projection into battle presentation");

            RequireSourceToken(enemyArtRuntimeSource,
                "public const string ExpectedSchemaId090 =",
                "Enemy Art 700 catalog schema authority");
            RequireSourceToken(enemyArtRuntimeSource,
                "public const int ExpectedCatalogCount090 = 700;",
                "Enemy Art 700 exact runtime catalog count");
            RequireSourceToken(enemyArtRuntimeSource,
                "public const int MaximumCacheCapacity090 = 96;",
                "bounded Enemy Art runtime sprite cache");
            RequireSourceToken(enemyArtRuntimeSource,
                "EnemyArt700SpriteLease090",
                "lease-owned Enemy Art sprite lifetime");
            RequireSourceToken(enemyArtRuntimeSource,
                "TryAcquireSprite090(",
                "pinned Enemy Art sprite acquisition");
            RequireSourceToken(enemyArtRuntimeSource, "ValidateCatalog090(",
                "Enemy Art runtime catalog validation authority");
            RequireSourceToken(enemyArtRuntimeSource, "UNITY_STANDALONE_WIN",
                "Enemy Art raw-file runtime restricted to the certified Windows player");
            RequireSourceToken(enemyArtImporterSource,
                "Assets/SecondDimension/EnemyArt700/Textures/",
                "scoped Enemy Art texture import root");
            RequireSourceToken(enemyArtImporterSource,
                "TextureImporterFormat.DXT5",
                "Enemy Art Windows alpha-compression import contract");
            RequireSourceToken(enemyArtBuildCopySource,
                "IPostprocessBuildWithReport",
                "Enemy Art Windows post-build payload integration");
            RequireSourceToken(enemyArtBuildCopySource,
                "public const int ExpectedRuntimeFileCount090 = ExpectedTextureCount090 + 2;",
                "Enemy Art exact 2,102-file runtime payload contract");
            RequireSourceToken(enemyArtBuildCopySource, "ValidateSourcePayload090(",
                "Enemy Art non-writing payload preflight authority");
            RequireSourceToken(enemyArtBuildCopySource, "CopyRuntimePayload090(",
                "Enemy Art exact Windows payload copy authority");

            RequireSourceToken(actorRigSource,
                "EnemyArt700Runtime090.TryAcquireSprite090(",
                "lease-owned Enemy Art idle/action pose loading in certified actor rigs");
            RequireSourceToken(cinematicBattleSource,
                "hasEnemyArt700Standee090",
                "Enemy Art standees in the cinematic battle presenter");
            RequireSourceToken(cinematicBattleSource,
                "EnemyArt700Runtime090.TryAcquireSprite090(",
                "lease-owned Enemy Art sprites in the cinematic battle presenter");
            RequireSourceToken(lastRemnantStagingSource,
                "EnemyArt700Pose090.Attack",
                "Enemy Art attack poses in Last-Remnant battle staging");
            RequireSourceToken(lastRemnantStagingSource,
                "EnemyArt700Runtime090.TryAcquireSprite090(",
                "lease-owned Enemy Art transient attack poses in Last-Remnant staging");
            RequireSourceToken(battle3DSource,
                "EnemyArt700Runtime090.RelativeScale090(",
                "Enemy Art silhouette framing in the 3D battle world");
            RequireSourceToken(battle3DSource,
                "FitGroundedActorSprite090(",
                "pivot-aware Enemy Art grounding in the 3D battle world");
            RequireSourceToken(battle3DSource,
                "EnemyArt700Runtime090.TryAcquireSprite090(",
                "lease-owned Enemy Art idle/action sprites in the 3D battle world");
            RequireSourceToken(battle3DSource,
                "M1VisualAssets.TryResolveEnemyBattleStandee",
                "existing enemy-art fallback remains available");

            foreach (var identityCertification090 in new[]
                     {
                         "CatalogAddressSpaceContainsExactlySeventyByTenStableIds",
                         "TowerUsesSevenFamilyBucketAndThreatVariantForEachFloor",
                         "CommitChangesOnlyVisualIdentityFields",
                         "ExplicitEnemyArtIdentitySurvivesJsonAndMemberCopies"
                     })
                RequireSourceToken(enemyArtIdentityTestsSource,
                    identityCertification090,
                    "Enemy Art identity certification " + identityCertification090);
            foreach (var runtimeCertification090 in new[]
                     {
                         "CatalogAndExplicitSelectorsCoverAllSevenHundredVariants090",
                         "CacheRemainsBoundedAndEvictsLeastRecentSprites090",
                         "LeasePinsDisplayedSpriteAcrossEvictionAndSafeClearThenReclaimsIt090",
                         "PairedIdleAttackIdleReusesTheOriginalIdleSprite090",
                         "MissingOrMismatchedExplicitIdsFailClosedWithoutPngReads090",
                         "EveryBaseHasAnExplicitBoundedPresentationScale090",
                         "EveryExplicitVariantLoadsEveryPoseWithinBoundedCache090",
                         "WindowsBuildPayloadGateAcceptsExactlyTwoDataFilesAndTwentyOneHundredPngs090",
                         "WindowsBuildPayloadGateRejectsAnIncompleteSourceRoot090",
                         "Release090BuilderInventoriesAndPreflightsEnemyArt700090"
                     })
                RequireSourceToken(enemyArtRuntimeTestsSource,
                    runtimeCertification090,
                    "Enemy Art runtime/build certification " + runtimeCertification090);
            RequireSourceToken(enemyArtGroundingTestsSource,
                "LowAuthoredPivotPlacesEnemyArtBottomOnThe3dGround090",
                "low-pivot Enemy Art 3D ground-contact certification");
            RequireSourceToken(enemyArtGroundingTestsSource,
                "CenteredFallbackRetainsItsExistingHalfHeightPlacement090",
                "center-pivot legacy fallback 3D placement preservation");
            RequireSourceToken(enemyArtBattleTestsSource,
                "EnemyArt700IdentityIsCommittedByRealCampaignAndTowerUnionBattlesAndSurvivesRoundAndJson090",
                "real Campaign and Tower Union battle Enemy Art identity persistence");
            RequireSourceToken(enemyArtBattleTestsSource,
                "EnemyArtIdentityChangesCannotAlterForecastEnemyRngOrBattleOutcome090",
                "Enemy Art cannot alter Forecasts, targeting, damage, rewards, or loot");
            RequireSourceToken(enemyArtBattleTestsSource,
                "EncounterVisualVariantSeedCannotAlterEnemyStatsForecastsOrDamage090",
                "enemy color seed cannot alter combat statistics or deterministic outcomes");
            RequireSourceToken(enemyArtPresentationTestsSource,
                "CertifiedActorRigUsesPairedIdleAttackIdleAndCutoutShader090",
                "certified actor-rig idle/attack/idle Enemy Art presentation");
            RequireSourceToken(enemyArtPresentationTestsSource,
                "ActiveActorSpriteSurvivesMoreThanNinetySixOtherLoadsAndCleansUp090",
                "active Enemy Art sprite lifetime beyond the bounded cache window");
        }

        private static void ValidateEnemyArt700PayloadOrThrow090(string projectRoot)
        {
            var enemyArtRoot090 = Path.Combine(
                projectRoot,
                "Assets",
                "SecondDimension",
                "EnemyArt700");
            var payloadCount090 =
                global::SecondDimension.Editor.EnemyArt700WindowsBuildCopy090
                    .ValidateSourcePayload090(enemyArtRoot090);
            if (payloadCount090 !=
                global::SecondDimension.Editor.EnemyArt700WindowsBuildCopy090
                    .ExpectedRuntimeFileCount090)
                throw new InvalidOperationException(
                    "Enemy Art 700 payload count drifted before build: " +
                    payloadCount090 + ".");

            var expectedRuntimeRoot090 = Path.GetFullPath(enemyArtRoot090);
            var resolvedRuntimeRoot090 = Path.GetFullPath(
                global::SecondDimension.Presentation.EnemyArt700Runtime090.RootDirectory090);
            if (!StringComparer.OrdinalIgnoreCase.Equals(
                    expectedRuntimeRoot090,
                    resolvedRuntimeRoot090))
                throw new InvalidOperationException(
                    "Enemy Art 700 runtime resolved the wrong source root before build: " +
                    resolvedRuntimeRoot090 + ".");

            var issues090 =
                global::SecondDimension.Presentation.EnemyArt700Runtime090
                    .ValidateCatalog090(verifyFiles090: true);
            if (issues090.Count > 0)
                throw new InvalidOperationException(
                    "Enemy Art 700 runtime catalog failed release preflight: " +
                    string.Join(" | ", issues090.Take(12)));
        }

        private static void ValidateTowerContractOrThrow(string projectRoot)
        {
            var registrySource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Campaign022/Campaign022Registry.cs");
            foreach (var relativePath in Campaign022RegistryResourceFiles)
            {
                const string resourcesPrefix = "Assets/Resources/";
                const string jsonSuffix = ".json";
                if (!relativePath.StartsWith(resourcesPrefix, StringComparison.Ordinal) ||
                    !relativePath.EndsWith(jsonSuffix, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Campaign 022 registry essential is not a Resources JSON path: " +
                        relativePath + ".");
                var resourceKey = relativePath.Substring(
                    resourcesPrefix.Length,
                    relativePath.Length - resourcesPrefix.Length - jsonSuffix.Length);
                RequireSourceToken(
                    registrySource,
                    "\"" + resourceKey + "\"",
                    "Campaign 022 registry load for " + resourceKey);
                if (AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath) == null)
                    throw new InvalidOperationException(
                        "Campaign 022 registry resource is not importable: " + relativePath + ".");
            }

            var towerContractsSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Campaign022/Campaign022PresentationContracts.cs");
            var towerCoordinatorSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Campaign022/M1RuntimeCoordinator.Campaign022.cs");
            var towerUiSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Campaign022/GuildCityFlowPresenter022.cs");
            var livingHubSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.LivingGuildHub074.cs");
            var towerCommandSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/Campaign022/CampaignProgressionCommandService022.cs");
            var invocationSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/M1FlowPresenter.Invocation022.cs");
            var fullScreenForecastSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle/M2FullScreenUnionCommandStage.cs");
            var battle3DSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Battle3D/M2Battle3DWorld.cs");
            var smokeSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/FirstHour071/FirstHourGoldSmoke071.cs");
            var towerEditModeTestSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/EditMode/TowerRun081EditModeTests.cs");
            var towerPlayModeTestSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/PlayMode/TowerRun081PlayModeTests.cs");
            var boardAdventurePlayModeTestSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/PlayMode/BoardAdventure084PlayModeTests.cs");
            var towerBattleOnlyPlayModeTestSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/PlayMode/TowerBattleOnly088Tests.cs");
            RequireSourceToken(towerContractsSource,
                "public const int OpeningFloorCount=10;",
                "ten-floor Tower rule");
            RequireSourceToken(towerContractsSource,
                "M1CommandResult BeginTowerRun081();",
                "public Begin Tower command contract");
            RequireSourceToken(towerContractsSource,
                "M1CommandResult AdvanceTowerRun081();",
                "public Advance Tower command contract");
            RequireSourceToken(towerContractsSource,
                "M1CommandResult RetreatTowerRun081();",
                "public Retreat Tower command contract");
            RequireSourceToken(towerCoordinatorSource,
                "public M1CommandResult BeginTowerRun081()",
                "shipping Begin Tower coordinator command");
            RequireSourceToken(towerCoordinatorSource,
                "public M1CommandResult AdvanceTowerRun081()",
                "shipping Advance Tower coordinator command");
            RequireSourceToken(towerCoordinatorSource,
                "public M1CommandResult RetreatTowerRun081()",
                "shipping Retreat Tower coordinator command");
            RequireSourceToken(towerCoordinatorSource,
                "_campaignCommands022.CommitAbyssStep(candidate,registry,\"SUCCESS\")",
                "saved exact-once Tower bookend commit");
            RequireSourceToken(towerCoordinatorSource,
                "_campaignCommands022.ApplyAbyssStep(candidate,registry)",
                "saved exact-once Tower bookend apply");
            ForbidSourceToken(towerCoordinatorSource,
                "AdvanceAutomaticTowerSteps081(",
                "retired unsaved Tower compression helper");
            RequireSourceToken(towerEditModeTestSource,
                "PublicCoordinatorContractExposesTheThreeSimpleTowerCommands",
                "EditMode one-room-per-click Tower coordinator proof");
            RequireSourceToken(towerEditModeTestSource,
                "AdvanceAutomaticTowerSteps081",
                "EditMode retired multi-room helper identity");
            RequireSourceToken(towerEditModeTestSource,
                "automatic room compression would skip the board-game play",
                "EditMode multi-room compression rejection");
            RequireSourceToken(towerUiSource,
                "BuildGuildCityAbyss022(",
                "Endless Tower presentation entry");
            RequireSourceToken(towerUiSource,
                "BeginTowerBattleOnly088(c)",
                "battle-only Tower begin button wiring");
            RequireSourceToken(towerUiSource,
                "BuildTowerPrimaryAction084(body,c,s);",
                "active Tower primary-action routing");
            RequireSourceToken(towerUiSource,
                "AdvanceTowerBattleOnlyTransitions088(",
                "saved Tower bookend fast-forward authority");
            RequireSourceToken(towerUiSource,
                "YOUR NEXT UNION BATTLE",
                "plain-language battle-only Tower lobby action");
            RequireSourceToken(towerUiSource,
                "BANK VICTORY & UNLOCK NEXT FLOOR",
                "battle-only Tower victory banking action");
            ForbidSourceToken(towerUiSource,
                "BuildTowerAdventureTrack084(body,s);",
                "Tower card-track presentation routing");
            RequireSourceToken(towerUiSource,
                "()=>EnterTowerBattle081(coordinator)",
                "Tower battle button wiring");
            RequireSourceToken(towerUiSource,
                "var result=coordinator?.EnterAbyssBattle022();",
                "Tower battle command authority");
            RequireSourceToken(towerUiSource,
                "coordinator.AdvanceTowerRun081",
                "Tower reward and advance button wiring");
            RequireSourceToken(towerUiSource,
                "coordinator.RetreatTowerRun081",
                "Tower retreat button wiring");
            RequireSourceToken(towerUiSource,
                "s.TowerArtResourcePath",
                "Tower floor art presentation binding");
            RequireSourceToken(towerContractsSource,
                "FloorOneCampaign083BattleArtResourcePath",
                "exact Campaign 083 Floor 1 art identity");
            RequireSourceToken(towerUiSource,
                "Tower Phone Simple Summary 084",
                "stable phone-simple Tower summary identity");
            RequireSourceToken(towerUiSource,
                "FirstClimbHeadingColor083",
                "readable first-climb heading color");
            RequireSourceToken(invocationSource,
                "ApplyLightSurfaceForecastContrast083",
                "Echo and covenant light-surface contrast policy");
            RequireSourceToken(invocationSource,
                "LightActionDisabledSurfaceColor083",
                "readable disabled Echo surface");
            RequireSourceToken(invocationSource,
                "ECHO · SELECTED FORECAST",
                "plain-language Echo forecast action");
            RequireSourceToken(fullScreenForecastSource,
                "ForecastTextColor083",
                "readable Union forecast text");
            RequireSourceToken(battle3DSource,
                "ActiveBackdropResourceKey083",
                "live 3D battlefield backdrop evidence");
            RequireSourceToken(smokeSource,
                "ActiveTowerPlaceholderCopyPresent083",
                "packaged placeholder-copy rejection");
            RequireSourceToken(smokeSource,
                "Title [Responsive",
                "packaged responsive Tower ribbon-title lookup");
            RequireSourceToken(smokeSource,
                "ActiveBackdropResourceKey083",
                "packaged live 3D backdrop proof");
            RequireSourceToken(smokeSource,
                "Bank Tower battle victory 088",
                "packaged battle-only Tower victory banking path");
            RequireSourceToken(smokeSource,
                "battle-only Floor 1 action did not enter combat directly",
                "packaged direct Tower battle entry proof");
            ForbidSourceToken(smokeSource,
                "Move forward Tower room 084",
                "retired Tower room-card move in packaged smoke");
            ForbidSourceToken(smokeSource,
                "Save Tower battle tile 084",
                "retired Tower battle-card save in packaged smoke");
            ForbidSourceToken(smokeSource,
                "Reveal Tower battle tile 084",
                "retired Tower battle-card reveal in packaged smoke");
            ForbidSourceToken(smokeSource,
                "Bank Tower rewards and return 084",
                "retired Tower results-card bank action in packaged smoke");
            RequireSourceToken(towerEditModeTestSource,
                "FloorOneUsesExactCampaign083BattlefieldAndPreservesLegacyPlate",
                "EditMode Floor 1 art and preservation proof");
            RequireSourceToken(towerPlayModeTestSource,
                "FloorOneRuntimeAndBattleResolverUseCampaign083ArtWithoutPlaceholderCopy",
                "PlayMode Floor 1 runtime resolver proof");
            RequireSourceToken(boardAdventurePlayModeTestSource,
                "PackagedTowerLobbyTitleLookupSurvivesResponsiveRename084",
                "PlayMode packaged Tower ribbon-title lookup proof");
            RequireSourceToken(towerBattleOnlyPlayModeTestSource,
                "LobbyOffersOneBattleAndNoCardRoute088",
                "PlayMode battle-only Tower lobby proof");
            RequireSourceToken(towerBattleOnlyPlayModeTestSource,
                "AssertNoCards088",
                "PlayMode Tower card-and-pawn absence proof");

            var visibleTowerVisualSources = string.Join(
                "\n",
                new[]
                {
                    towerUiSource,
                    invocationSource,
                    fullScreenForecastSource,
                    battle3DSource
                }).ToUpperInvariant();
            ForbidSourceToken(
                visibleTowerVisualSources,
                "SCHEMATIC MAP",
                "visible Tower schematic copy");
            ForbidSourceToken(
                visibleTowerVisualSources,
                "FINAL ILLUSTRATION MAY REPLACE",
                "visible Tower temporary-art copy");
            ForbidSourceToken(
                visibleTowerVisualSources,
                "PLACEHOLDER ART",
                "visible Tower placeholder copy");
            RequireSourceToken(livingHubSource,
                "LivingGuildHubTowerDestinationId081 = \"ENDLESS_TOWER_081\";",
                "Living Guild Hall Tower destination");
            RequireSourceToken(livingHubSource,
                "_guildCityTab017D = \"ABYSS\";",
                "Living Guild Hall to Tower tab routing");
            RequireSourceToken(towerCommandSource,
                "TowerEnemyUnionCount081(",
                "Tower enemy-Union scaling authority");
            RequireSourceToken(towerCommandSource,
                "HasMatchingActiveAbyssBattle(",
                "exact active Tower battle identity authority");

            var productionTowerHashes = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < TowerProductionArtAssetPaths084.Length; index++)
            {
                var assetPath = TowerProductionArtAssetPaths084[index];
                var absolutePath = Path.Combine(
                    projectRoot,
                    assetPath.Replace('/', Path.DirectorySeparatorChar));
                var actualHash = ComputeFileSha256(absolutePath);
                if (!StringComparer.Ordinal.Equals(
                        actualHash,
                        TowerProductionArtSha256084[index]))
                    throw new InvalidOperationException(
                        "Release 084 Tower plate changed after approval: " + assetPath +
                        ". Expected " + TowerProductionArtSha256084[index] +
                        "; found " + actualHash + ".");
                if (!productionTowerHashes.Add(actualHash))
                    throw new InvalidOperationException(
                        "Release 084 Tower plates must be visually distinct: " + assetPath + ".");
                if (assetPath.IndexOf("/UI/Abyss/", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException(
                        "Release 084 production Tower art cannot use a legacy schematic path: " +
                        assetPath + ".");
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture == null || texture.width < 1600 || texture.height < 900 ||
                    !StringComparer.Ordinal.Equals(
                        texture.name,
                        Path.GetFileNameWithoutExtension(assetPath)))
                    throw new InvalidOperationException(
                        "Release 084 Tower plate must be an exact, loadable 1600x900+ texture: " +
                        assetPath + ".");
            }

            for (var index = 0; index < PreservedTowerLegacyArtAssetPaths084.Length; index++)
            {
                var assetPath = PreservedTowerLegacyArtAssetPaths084[index];
                var absolutePath = Path.Combine(
                    projectRoot,
                    assetPath.Replace('/', Path.DirectorySeparatorChar));
                if (!StringComparer.Ordinal.Equals(
                        ComputeFileSha256(absolutePath),
                        PreservedTowerLegacyArtSha256084[index]) ||
                    AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
                    throw new InvalidOperationException(
                        "A preserved legacy Tower plate changed or is no longer importable: " +
                        assetPath + ".");
            }

            var registry = SecondDimension.Presentation.Campaign022.CampaignRegistry022
                .LoadFromResources();
            if (registry == null || registry.Floors == null || registry.Floors.Count != 10)
                throw new InvalidOperationException(
                    "Release 084 Tower registry must contain exactly ten floors.");
            if (registry.AbyssOperations == null ||
                registry.AbyssOperations.Values.Count(value => value.kind == "RECON") != 10 ||
                registry.AbyssOperations.Values.Count(value => value.kind == "TRIAL") != 10 ||
                registry.AbyssOperations.Values.Count(value => value.kind == "GUARDIAN") != 10 ||
                registry.AbyssOperations.Values.Count(value => value.kind ==
                    SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022
                        .EndlessBattleKind094) != 10 ||
                registry.AbyssOperations.Count != 40)
                throw new InvalidOperationException(
                    "Tower registry must preserve thirty historical operations and exactly " +
                    "ten distinct ENDLESS_BATTLE094 repeat-battle definitions.");

            for (var floorNumber = 1; floorNumber <= 10; floorNumber++)
            {
                var floor = registry.Floors.Values.SingleOrDefault(value =>
                    value != null && value.floor == floorNumber);
                if (floor == null)
                    throw new InvalidOperationException(
                        "Release 084 Tower floor is missing or duplicated: " + floorNumber + ".");
                var expectedGuardianId =
                    "ABYSS_OP022_" + floorNumber.ToString("00") + "_GUARDIAN";
                var mappedGuardianId =
                    SecondDimension.Presentation.Campaign022.TowerRunRules081.OperationId(
                        floorNumber,
                        true);
                if (!StringComparer.Ordinal.Equals(mappedGuardianId, expectedGuardianId) ||
                    !registry.AbyssOperations.TryGetValue(expectedGuardianId, out var guardian))
                    throw new InvalidOperationException(
                        "Release 084 Tower guardian mapping is missing for floor " +
                        floorNumber + ".");
                ValidateTowerOperationOrThrow(
                    guardian,
                    expectedGuardianId,
                    floor.floorId,
                    "GUARDIAN",
                    true);

                var endlessId = SecondDimension.Gameplay.Campaign022
                    .CampaignProgressionCommandService022.TowerOperationDefinitionId094(
                        floorNumber, false);
                if (!registry.AbyssOperations.TryGetValue(endlessId, out var endless))
                    throw new InvalidOperationException(
                        "Endless Tower battle definition is missing: " + endlessId + ".");
                ValidateTowerOperationOrThrow(
                    endless, endlessId, floor.floorId,
                    SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022
                        .EndlessBattleKind094, false);
                if (!StringComparer.Ordinal.Equals(
                        endless.steps[3].bossId, guardian.steps[3].bossId))
                    throw new InvalidOperationException(
                        "Endless Tower must reuse its exact guardian encounter: " + endlessId + ".");

                var expectedArtPath = TowerFloorArtResourcePaths[floorNumber - 1];
                var mappedArtPath =
                    SecondDimension.Presentation.Campaign022.TowerRunRules081.ArtResourcePath(
                        floorNumber);
                if (!StringComparer.Ordinal.Equals(mappedArtPath, expectedArtPath))
                    throw new InvalidOperationException(
                        "Release 084 Tower art mapping is stale for floor " + floorNumber +
                        ": " + mappedArtPath + ".");
                var floorArt = Resources.Load<Texture2D>(mappedArtPath);
                if (floorArt == null || floorArt.width <= 0 || floorArt.height <= 0)
                    throw new InvalidOperationException(
                        "Release 084 Tower floor art is not loadable: " + mappedArtPath + ".");
            }

            const string floorTenTrialId = "ABYSS_OP022_10_TRIAL";
            var mappedTrialId =
                SecondDimension.Presentation.Campaign022.TowerRunRules081.OperationId(10, false);
            var floorTen = registry.Floors.Values.Single(value => value != null && value.floor == 10);
            if (!StringComparer.Ordinal.Equals(mappedTrialId, floorTenTrialId) ||
                !registry.AbyssOperations.TryGetValue(floorTenTrialId, out var floorTenTrial))
                throw new InvalidOperationException(
                    "Release 084 repeatable floor-ten Tower trial is missing.");
            ValidateTowerOperationOrThrow(
                floorTenTrial,
                floorTenTrialId,
                floorTen.floorId,
                "TRIAL",
                false);
        }

        private static void ValidateBoardTowerEnhancementContractOrThrow(string projectRoot)
        {
            ValidateClosedWorldRuntimeDataDirectoryOrThrow(
                projectRoot,
                "Assets/Resources/SecondDimension/BoardTower001/Data",
                BoardTowerEnhancement001ResourceFiles,
                "BOARD_TOWER_ENHANCEMENT_PACK_001");

            var rulesSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/GuildCity017D/BoardTowerEnhancementRules001.cs");
            var catalogSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/BoardTower001/BoardTowerEnhancementCatalog001.cs");
            var expeditionSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/GuildCity017D/GuildCityExpeditionService017D.cs");
            var boardSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/M1FlowPresenter.BoardQuest081.cs");
            var towerSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Campaign022/M1RuntimeCoordinator.Campaign022.cs");
            var towerProjectionSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/BoardTower001/TowerRevealedRoomProjection001.cs");

            RequireSourceToken(rulesSource,
                "PackageId001 = \"BOARD_TOWER_ENHANCEMENT_PACK_001\";",
                "board/tower enhancement package identity");
            RequireSourceToken(rulesSource,
                "SelectedRoomFlagPrefix001 = \"BTR001_ROOM_COMMITTED_\";",
                "reload-stable committed room proof");
            RequireSourceToken(catalogSource,
                "public const int ExpectedItemCount001 = 144;",
                "exact board/tower item count");
            RequireSourceToken(catalogSource,
                "public const int ExpectedRoomCount001 = 72;",
                "exact board/tower room count");
            RequireSourceToken(catalogSource,
                "public const int ExpectedEffectCount001 = 72;",
                "exact board/tower run-effect count");
            RequireSourceToken(catalogSource,
                "public const int ExpectedTurningPointCount001 = 24;",
                "exact board/tower turning-point count");
            RequireSourceToken(catalogSource,
                "public const int ExpectedHookCount001 = 30;",
                "exact board/tower natural-reward hook count");
            RequireSourceToken(catalogSource,
                "public const int ExpectedP0Count001 = 76;",
                "exact board/tower P0 count");
            RequireSourceToken(catalogSource,
                "ValidateExcludedPreparationNames(items, counts.existingPreparationNamesExcluded);",
                "seventeen-name preparation collision rejection");
            RequireSourceToken(expeditionSource,
                "BoardTowerEnhancementRules001.SelectedRoomFlag001(",
                "board move commits the selected enhancement room once");
            RequireSourceToken(boardSource,
                ".CommittedBoardRoom001(",
                "board presentation reads only a committed enhancement room");
            RequireSourceToken(towerSource,
                "ApplyOptionalTowerRoomModule001(",
                "fail-soft optional Tower room projection");
            RequireSourceToken(towerSource,
                "TowerRevealedRoomProjection001.Project(",
                "saved completed-step Tower room projection");
            RequireSourceToken(towerSource,
                "catalog,committedRunId,floorNumber,completedStepIds);",
                "Tower presentation passes only committed run and saved step proof");
            RequireSourceToken(towerProjectionSource,
                "if (catalog == null || string.IsNullOrWhiteSpace(committedRunId) ||",
                "Tower room projection rejects a missing committed run");
            RequireSourceToken(towerProjectionSource,
                "completedStepIds == null || completedStepIds.Count == 0 ||",
                "Tower room projection rejects missing saved step proof");
            RequireSourceToken(towerProjectionSource,
                "var anchor = catalog.DeterministicTowerRoom001(",
                "Tower room projection retains its deterministic run anchor");
            RequireSourceToken(towerProjectionSource,
                "committedRunId, Math.Max(1, floorNumber));",
                "Tower room anchor consumes the committed run and floor");
            RequireSourceToken(towerProjectionSource,
                "var revealedIndex = completedStepIds.Count - 1;",
                "Tower room projection exposes only a saved completed step");

            var catalog = SecondDimension.Presentation.BoardTower001
                .BoardTowerEnhancementCatalog001.LoadFromResources();
            var counts = catalog?.Counts;
            if (catalog == null || counts == null ||
                !StringComparer.Ordinal.Equals(
                    counts.packageId,
                    SecondDimension.Gameplay.GuildCity017D.BoardTowerEnhancementRules001.PackageId001) ||
                catalog.Items.Count != SecondDimension.Presentation.BoardTower001
                    .BoardTowerEnhancementCatalog001.ExpectedItemCount001 ||
                catalog.Rooms.Count != SecondDimension.Presentation.BoardTower001
                    .BoardTowerEnhancementCatalog001.ExpectedRoomCount001 ||
                catalog.Effects.Count != SecondDimension.Presentation.BoardTower001
                    .BoardTowerEnhancementCatalog001.ExpectedEffectCount001 ||
                catalog.TurningPoints.Count != SecondDimension.Presentation.BoardTower001
                    .BoardTowerEnhancementCatalog001.ExpectedTurningPointCount001 ||
                catalog.Hooks.Count != SecondDimension.Presentation.BoardTower001
                    .BoardTowerEnhancementCatalog001.ExpectedHookCount001 ||
                catalog.P0.Count != SecondDimension.Presentation.BoardTower001
                    .BoardTowerEnhancementCatalog001.ExpectedP0Count001 ||
                counts.existingPreparationNamesExcluded == null ||
                counts.existingPreparationNamesExcluded.Length != 17 ||
                counts.existingPreparationNamesExcluded
                    .Distinct(StringComparer.Ordinal).Count() != 17)
                throw new InvalidOperationException(
                    "Release 084 board/tower enhancement catalog failed exact-count validation.");
        }

        private static void ValidateCreatorCodeContractOrThrow(string projectRoot)
        {
            ValidateClosedWorldRuntimeDataDirectoryOrThrow(
                projectRoot,
                "Assets/Resources/SecondDimension/Creator10000/Data",
                CreatorGiveaway10000ResourceFiles,
                "CREATOR_GIVEAWAY_CODES_10000_V1");
            RejectOwnerCreatorCodeArtifactsOrThrow(projectRoot);

            var guildUiSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/GuildCity017D/GuildCityFlowPresenter017D.cs");
            var codeUiSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Creator028/GuildCityFlowPresenter028.cs");
            var coordinatorSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Creator028/M1RuntimeCoordinator.Creator028.cs");
            var contractsSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Creator028/CreatorPresentationContracts028.cs");
            var modelsSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/Creator028/CreatorAccessModels028.cs");
            var giveawayRulesSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/Creator028/CreatorGiveawayRules10000.cs");
            var giveawayCommandSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/Creator028/CreatorGiveawayCommandService10000.cs");
            var giveawayRegistrySource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Creator028/CreatorGiveawayRegistry10000.cs");
            RequireSourceToken(
                guildUiSource,
                "Guild Details Redeem Code 081",
                "player-reachable bonus-code entry point");
            RequireSourceToken(
                codeUiSource,
                "RuntimeUi.AddInputField(row,\"Creator Code 028\",\"ENTER BONUS CODE\",96)",
                "giveaway-code input capacity");
            RequireSourceToken(
                coordinatorSource,
                "already claimed in this campaign",
                "clear duplicate-code claim feedback");
            RequireSourceToken(
                codeUiSource,
                "Preview Creator Code 10000",
                "Creator giveaway preview-before-commit UI");
            RequireSourceToken(
                codeUiSource,
                "Commit Creator Reward 10000",
                "Creator giveaway explicit commit UI");
            RequireSourceToken(
                contractsSource,
                "CreatorRewardPreview028 PreviewCreatorCode028(string input);",
                "Creator giveaway preview coordinator contract");
            RequireSourceToken(
                contractsSource,
                "RedeemCreatorCode028(string input,string targetId,bool creatorPowerConfirmed);",
                "Creator giveaway target and power-confirmation command contract");
            RequireSourceToken(
                coordinatorSource,
                "Creator028.CreatorGiveawayRegistry10000.Load()",
                "Creator 10000 hash-only registry load");
            RequireSourceToken(
                coordinatorSource,
                "_creatorGiveawayCommands10000.RedeemCode(",
                "Creator 10000 atomic command-service wiring");
            RequireSourceToken(
                modelsSource,
                "GrowthAllocations10000",
                "Creator reward growth-allocation save ledger");
            RequireSourceToken(
                giveawayRulesSource,
                "bool TryGetCodeByHash(string sha256, out CreatorGiveawayCodeRule10000 rule);",
                "hash-only Creator giveaway lookup contract");
            RequireSourceToken(
                giveawayCommandSource,
                "using (var sha = SHA256.Create())",
                "Creator giveaway SHA-256 normalization");
            RequireSourceToken(
                giveawayCommandSource,
                "access.RedeemedCodeIds.Contains(code.CodeId)",
                "Creator giveaway exact-once code ledger guard");
            RequireSourceToken(
                giveawayCommandSource,
                "access.AppliedReceiptIds.Contains(receiptId)",
                "Creator giveaway exact-once receipt guard");
            RequireSourceToken(
                giveawayCommandSource,
                "CreatorPowerUsedFlag = \"CREATOR_POWER_USED\";",
                "Creator-power noncanonical campaign marker");
            RequireSourceToken(
                giveawayRegistrySource,
                "RUNTIME_SAFE_CREATOR_CODE_HASH_MANIFEST_10000_v1",
                "runtime-safe 10,000-code hash manifest load");
            RequireSourceToken(
                giveawayRegistrySource,
                "CREATOR_REWARD_BUNDLES_10000_v1",
                "Creator 10000 reward-bundle load");
            RequireSourceToken(
                giveawayRegistrySource,
                "AssertHashOnlyManifest(codeAsset.text);",
                "Creator manifest field allowlist validation");

            var catalogPath = Path.Combine(
                projectRoot,
                "Assets",
                "Resources",
                "SecondDimension",
                "Creator028",
                "Data",
                "CreatorCodeCatalog028.json");
            var catalogSource = File.ReadAllText(catalogPath);
            if (catalogSource.IndexOf("\"code\"", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(
                    "Runtime Creator catalog must never contain plaintext giveaway codes.");
            var registry = SecondDimension.Presentation.Creator028.CreatorRegistry028.Load();
            if (registry == null || registry.CodeCount != 300)
                throw new InvalidOperationException(
                    "Runtime Creator catalog must preserve exactly the original 300 hash-only codes.");

            var hashManifestPath = Path.Combine(
                projectRoot,
                CreatorGiveaway10000ResourceFiles[0]
                    .Replace('/', Path.DirectorySeparatorChar));
            var hashManifestSource = File.ReadAllText(hashManifestPath);
            RequireSourceToken(hashManifestSource,
                "\"publicBuildPlaintextCodes\": 0",
                "zero plaintext Creator codes in the public build");
            RequireSourceToken(hashManifestSource,
                "\"globalOneUseCurrentlyEnforced\": false",
                "truthful offline redemption scope");
            RequireSourceToken(hashManifestSource,
                "\"redemptionScope\": \"EXACT_ONCE_PER_CAMPAIGN_OFFLINE\"",
                "exact-once-per-campaign Creator redemption scope");
            ForbidSourceToken(hashManifestSource,
                "\"code\"",
                "plaintext Creator code field");
            ForbidSourceToken(hashManifestSource,
                "\"plaintextCode\"",
                "plaintext Creator code value");
            ForbidSourceToken(hashManifestSource,
                "\"ownerCode\"",
                "owner-only Creator code value");
            ForbidSourceToken(hashManifestSource,
                "\"rawCode\"",
                "raw Creator code value");

            var giveawayRegistry = SecondDimension.Presentation.Creator028
                .CreatorGiveawayRegistry10000.Load();
            if (giveawayRegistry == null ||
                giveawayRegistry.CodeCount != SecondDimension.Presentation.Creator028
                    .CreatorGiveawayRegistry10000.ExpectedCodeCount ||
                giveawayRegistry.RewardBundleCount != SecondDimension.Presentation.Creator028
                    .CreatorGiveawayRegistry10000.ExpectedRewardBundleCount ||
                giveawayRegistry.AllRewardBundles.Count != SecondDimension.Presentation.Creator028
                    .CreatorGiveawayRegistry10000.ExpectedRewardBundleCount ||
                giveawayRegistry.AllRewardBundles.Count(bundle =>
                    StringComparer.Ordinal.Equals(bundle.GrantType, "EQUIPMENT_INSTANCE")) !=
                    SecondDimension.Presentation.Creator028.CreatorGiveawayRegistry10000
                        .ExpectedEquipmentTemplateCount ||
                giveawayRegistry.AllRewardBundles.Sum(bundle => bundle.CodeCount) !=
                    SecondDimension.Presentation.Creator028.CreatorGiveawayRegistry10000
                        .ExpectedCodeCount ||
                giveawayRegistry.AllRewardBundles.Where(bundle => bundle.CreatorPowerFlag)
                    .Sum(bundle => bundle.CodeCount) !=
                    SecondDimension.Presentation.Creator028.CreatorGiveawayRegistry10000
                        .ExpectedCreatorPowerCodeCount)
                throw new InvalidOperationException(
                    "Release 084 Creator giveaway catalog failed exact-count validation.");
        }

        private static void ValidateRelicPatchContractOrThrow(string projectRoot)
        {
            ValidateClosedWorldRuntimeDataDirectoryOrThrow(
                projectRoot,
                "Assets/Resources/SecondDimension/SpecialRelic001/Data",
                SpecialRelic001ResourceFiles,
                "SPECIAL_RELIC_ULTIMATE_ART_PACK_001");
            ValidateClosedWorldRuntimeDataDirectoryOrThrow(
                projectRoot,
                "Assets/Resources/SecondDimension/RelicCode1000/Data",
                RelicCode1000ResourceFiles,
                "RELIC_CODE_PATCH_1000_RUNTIME_SAFE_V1");
            RejectOwnerRelicCodeArtifactsOrThrow(projectRoot);

            var specialRegistrySource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/SpecialRelic001/SpecialRelicRegistry001.cs");
            var relicCodeRegistrySource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/RelicCode1000/RelicCodeRegistry1000.cs");
            var relicCommandSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/RelicCode1000/RelicCodeCommandService1000.cs");
            var creatorModelsSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Gameplay/Creator028/CreatorAccessModels028.cs");
            var coordinatorSource = ReadProjectSource(
                projectRoot,
                "Assets/SecondDimension/Presentation/Creator028/M1RuntimeCoordinator.Creator028.cs");
            var certificationSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/EditMode/RelicCode1000EditModeTests.cs");
            var p0CombatCertificationSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/EditMode/SpecialRelicP0Combat001Tests.cs");
            var p0InvocationCertificationSource = ReadProjectSource(
                projectRoot,
                "Assets/Tests/EditMode/SpecialRelicP0Invocation001Tests.cs");

            RequireSourceToken(specialRegistrySource,
                "DuplicatePropertyNameHandling.Error",
                "Special Relic duplicate-field rejection");
            RequireSourceToken(specialRegistrySource,
                "ExpectedRelicCount001 = 60",
                "exact 60-Special-Relic catalog");
            RequireSourceToken(relicCodeRegistrySource,
                "ExpectedCodeCount = 1_000",
                "exact 1,000-code hash manifest");
            RequireSourceToken(relicCodeRegistrySource,
                "RequireExactProperties(entry",
                "Relic-code runtime field allowlist");
            RequireSourceToken(relicCodeRegistrySource,
                "DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error",
                "Relic-code duplicate-field rejection");
            RequireSourceToken(relicCommandSource,
                "CreatorGiveawayCommandService10000.HashCode10000(input)",
                "shared upper-alphanumeric SHA-256 normalization");
            RequireSourceToken(relicCommandSource,
                "access.RedeemedCodeIds.Contains(code.CodeId)",
                "Relic-code exact-once code ledger guard");
            RequireSourceToken(relicCommandSource,
                "appliedReceiptCount = access.AppliedReceiptIds.Count",
                "Relic-code exact-one receipt ledger guard");
            RequireSourceToken(relicCommandSource,
                "EquipmentSlotIds.ToolRelic",
                "manual Tool Relic inventory grant");
            RequireSourceToken(relicCommandSource,
                "MATERIAL_RESONANCE_THREAD",
                "bounded duplicate-conversion material adapter");
            RequireSourceToken(relicCommandSource,
                "SEALED_DATA_ONLY",
                "non-P0 relics remain visibly data-only");
            RequireSourceToken(relicCommandSource,
                "CountOwnedDefinition(campaign.Guild, relic.RelicId)",
                "inventory-and-assignments duplicate scan");
            RequireSourceToken(relicCommandSource,
                "campaignProgressionService.GrantSpecialInvocationRelic(",
                "P0 Invocation artifact plus inventory atomic grant bridge");
            RequireSourceToken(relicCommandSource,
                "SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(",
                "P0 Ultimate Art seal exact inventory contract bridge");
            RequireSourceToken(creatorModelsSource,
                "CreatorRelicReceipt1000",
                "structured reload-stable relic receipt");
            RequireSourceToken(creatorModelsSource,
                "RelicReceipts1000",
                "backward-compatible relic receipt ledger");
            RequireSourceToken(coordinatorSource,
                "reg.CodeCount+giveaway.CodeCount+relicCodes.CodeCount",
                "11,300-code additive presentation total");
            RequireSourceToken(coordinatorSource,
                "Cache outcome committed:",
                "cache outcome player feedback");
            RequireSourceToken(coordinatorSource,
                "manual Tool Relic equip",
                "manual-equip player feedback");
            RequireSourceToken(coordinatorSource,
                "combat adapter is not active yet",
                "sealed non-P0 player disclosure");
            RequireSourceToken(certificationSource,
                "P0InvocationDirectCodeCreatesArtifactAndInventoryAtomically083",
                "P0 Invocation direct-code certification");
            RequireSourceToken(certificationSource,
                "P0UltimateDirectAndCacheOutcomesUseExactActiveSealInventoryContract083",
                "direct/cache P0 Ultimate seal certification");
            RequireSourceToken(certificationSource,
                "CorruptCreatorLedgersAndTamperedReceiptProofFailClosed083",
                "Relic receipt corruption certification");
            RequireSourceToken(certificationSource,
                "NaturallyGrantedP0DuplicatesRemainExactOnceAcrossRetryAndReload083",
                "natural P0 duplicate retry/reload certification");
            RequireSourceToken(p0CombatCertificationSource,
                "GrantShapeRequiresManualToolRelicEquipmentAndExactUnion",
                "P0 manual Tool Relic equip certification");
            RequireSourceToken(p0CombatCertificationSource,
                "OwnershipFailsClosedOnDuplicateOrCorruptP0ButAllowsGenericArt007",
                "P0 relic and generic-art coexistence certification");
            RequireSourceToken(p0CombatCertificationSource,
                "GrandRestorationPreservesCrossUnionTargetWhenAdapterIsRequested",
                "P0 cross-Union support certification");
            RequireSourceToken(p0CombatCertificationSource,
                "ProgressionIsExactOnceRejectsTamperedReplayAndDoesNotChangeSummonResonance",
                "P0 exact-once progression certification");
            RequireSourceToken(p0CombatCertificationSource,
                "MultipleEligibleSealsChooseOneDeterministicallyAcrossReplayReloadAndRound",
                "P0 multi-seal deterministic Union certification");
            RequireSourceToken(p0CombatCertificationSource,
                "UltimateProgressionPreservesCanonicalInvocationPendingCheckpoint",
                "P0 Ultimate and Invocation checkpoint coexistence certification");
            RequireSourceToken(p0CombatCertificationSource,
                "OrphanedIncomingOrFutureEvolutionReceiptFailsWithoutMutation",
                "P0 Ultimate orphan-receipt fail-closed certification");
            RequireSourceToken(p0InvocationCertificationSource,
                "SixRulesGrantDeterministicManualRareToolRelics",
                "six P0 Invocation manual-grant certifications");
            RequireSourceToken(p0InvocationCertificationSource,
                "EveryInvocationExecutesThroughOneSelectedCompleteForecastWithExactGrowth",
                "six live P0 Invocation combat-path certifications");
            RequireSourceToken(p0InvocationCertificationSource,
                "FriendlyWardingAndSupportFollowTheSelectedAllyForecastTarget",
                "P0 Warding/Support selected-friendly target certification");
            RequireSourceToken(p0InvocationCertificationSource,
                "InvocationGrowthAndReceiptsRoundTripThroughExistingV11Save",
                "P0 Invocation v11 persistence certification");

            var manifestPath = Path.Combine(
                projectRoot,
                RelicCode1000ResourceFiles[0].Replace('/', Path.DirectorySeparatorChar));
            var manifestSource = File.ReadAllText(manifestPath);
            foreach (var forbiddenField in new[]
                     {
                         "\"codeText\"", "\"plaintextCode\"", "\"ownerCode\"",
                         "\"rawCode\"", "\"password\"", "\"secret\""
                     })
                ForbidSourceToken(manifestSource, forbiddenField,
                    "plaintext/owner field in Relic-code manifest");

            var specialRegistry = SecondDimension.Presentation.SpecialRelic001
                .SpecialRelicRegistry001.Load();
            if (specialRegistry == null || specialRegistry.RelicCount != 60 ||
                specialRegistry.P0Count != 12 || specialRegistry.RewardHooks.Count != 12)
                throw new InvalidOperationException(
                    "Release 084 Special Relic catalog failed exact-count validation.");
            var relicCodeRegistry = SecondDimension.Presentation.RelicCode1000
                .RelicCodeRegistry1000.Load();
            if (relicCodeRegistry == null || relicCodeRegistry.CodeCount != 1_000 ||
                relicCodeRegistry.RewardBundleCount != 64 ||
                relicCodeRegistry.AllRewardBundles.Count(value => value.IsDirect) != 60 ||
                relicCodeRegistry.AllRewardBundles.Count(value => value.IsCache) != 4 ||
                relicCodeRegistry.AllCodes.Count(value =>
                    StringComparer.Ordinal.Equals(value.Category, "RELIC_CACHE")) != 40)
                throw new InvalidOperationException(
                    "Release 084 Relic-code catalog failed exact-count validation.");

            var legacyRegistry = SecondDimension.Presentation.Creator028.CreatorRegistry028.Load();
            var giveawayRegistry = SecondDimension.Presentation.Creator028
                .CreatorGiveawayRegistry10000.Load();
            var distinctHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var distinctCodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var code in legacyRegistry.AllCodes)
                if (!distinctHashes.Add((code.CodeHash ?? string.Empty).Trim().ToUpperInvariant()) ||
                    !distinctCodeIds.Add(code.CodeId))
                    throw new InvalidOperationException(
                        "Release 084 legacy Creator code collides with another public code.");
            foreach (var code in giveawayRegistry.AllCodes)
                if (!distinctHashes.Add((code.Sha256 ?? string.Empty).Trim().ToUpperInvariant()) ||
                    !distinctCodeIds.Add(code.CodeId))
                    throw new InvalidOperationException(
                        "Release 084 Creator giveaway code collides with another public code.");
            foreach (var code in relicCodeRegistry.AllCodes)
                if (!distinctHashes.Add((code.Sha256 ?? string.Empty).Trim().ToUpperInvariant()) ||
                    !distinctCodeIds.Add(code.CodeId))
                    throw new InvalidOperationException(
                        "Release 084 Relic code collides with another public code.");
            if (distinctHashes.Count != 11_300 || distinctCodeIds.Count != 11_300)
                throw new InvalidOperationException(
                    "Release 084 must expose exactly 11,300 distinct public hashes and code IDs.");
        }

        private static void ValidateTowerOperationOrThrow(
            SecondDimension.Gameplay.Campaign022.AbyssOperationDto022 operation,
            string expectedOperationId,
            string expectedFloorId,
            string expectedKind,
            bool expectedFirstClearOnly)
        {
            if (operation == null ||
                !StringComparer.Ordinal.Equals(operation.operationId, expectedOperationId) ||
                !StringComparer.Ordinal.Equals(operation.floorId, expectedFloorId) ||
                !StringComparer.Ordinal.Equals(operation.kind, expectedKind) ||
                operation.firstClearOnly != expectedFirstClearOnly ||
                operation.steps == null ||
                operation.steps.Length != 5 ||
                operation.steps.Count(step => step != null && step.requiresBattle) != 1 ||
                operation.steps[3] == null ||
                !operation.steps[3].requiresBattle ||
                !operation.exactOnceReceipts ||
                !operation.existingEquipmentRewardRemainsAuthoritative ||
                operation.rewardMaterialIds == null ||
                operation.rewardMaterialIds.Length == 0 ||
                operation.guildXp <= 0 ||
                operation.hallXp <= 0)
                throw new InvalidOperationException(
                    "Release 084 Tower operation contract is invalid: " +
                    expectedOperationId + ".");
        }

        private static void ValidateClosedWorldRuntimeDataDirectoryOrThrow(
            string projectRoot,
            string relativeDirectory,
            IReadOnlyList<string> expectedResourceFiles,
            string packageLabel)
        {
            var absoluteDirectory = Path.Combine(
                projectRoot,
                relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absoluteDirectory))
                throw new InvalidOperationException(
                    packageLabel + " runtime data directory is missing: " + relativeDirectory + ".");
            if (Directory.GetDirectories(absoluteDirectory, "*", SearchOption.TopDirectoryOnly).Length != 0)
                throw new InvalidOperationException(
                    packageLabel + " runtime data directory must not contain nested content.");

            var expectedNames = new HashSet<string>(
                expectedResourceFiles.Select(Path.GetFileName),
                StringComparer.Ordinal);
            var actualNames = Directory.GetFiles(
                    absoluteDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            if (actualNames.Length != expectedNames.Count || !expectedNames.SetEquals(actualNames))
                throw new InvalidOperationException(
                    packageLabel + " runtime files do not match the exact safe allowlist. Expected [" +
                    string.Join(", ", expectedNames.OrderBy(name => name, StringComparer.Ordinal)) +
                    "]; found [" + string.Join(", ", actualNames) + "].");

            foreach (var relativePath in expectedResourceFiles)
            {
                if (!relativePath.StartsWith(
                        relativeDirectory + "/",
                        StringComparison.Ordinal) ||
                    AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath) == null)
                    throw new InvalidOperationException(
                        packageLabel + " safe runtime resource is not importable: " +
                        relativePath + ".");
                if (!PinnedRuntimeDataSha256.TryGetValue(relativePath, out var expectedSha256))
                    throw new InvalidOperationException(
                        packageLabel + " safe runtime resource has no audited SHA-256 pin: " +
                        relativePath + ".");
                var absolutePath = Path.Combine(
                    projectRoot,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                var actualSha256 = ComputeFileSha256(absolutePath);
                if (!StringComparer.Ordinal.Equals(actualSha256, expectedSha256))
                    throw new InvalidOperationException(
                        packageLabel + " safe runtime resource changed after audit: " +
                        relativePath + ". Expected " + expectedSha256 + "; found " +
                        actualSha256 + ".");
            }
        }

        private static void RejectOwnerCreatorCodeArtifactsOrThrow(string projectRoot)
        {
            var assetsRoot = Path.Combine(projectRoot, "Assets");
            var forbiddenNameFragments = new[]
            {
                "OWNER_CODE", "OWNER-CODE", "OWNER_CODES", "OWNER-CODES",
                "PLAINTEXT_CODE", "PLAINTEXT-CODE", "PLAIN_TEXT_CODE",
                "RAW_CODE", "RAW-CODE", "RAW_CODES", "RAW-CODES",
                "UNHASHED_CODE", "UNHASHED-CODE", "MASTER_CODE", "MASTER-CODE",
                "CODEBOOK"
            };
            foreach (var path in Directory.GetFiles(assetsRoot, "*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                var normalized = path.Replace('\\', '/').ToUpperInvariant();
                var creatorCodeArtifact = normalized.Contains("CREATOR") &&
                    (normalized.Contains("CODE") || normalized.Contains("GIVEAWAY"));
                if (creatorCodeArtifact && forbiddenNameFragments.Any(normalized.Contains))
                    throw new InvalidOperationException(
                        "Owner/plaintext Creator code artifact is forbidden from Assets: " + path + ".");
            }
        }

        private static void RejectOwnerRelicCodeArtifactsOrThrow(string projectRoot)
        {
            var assetsRoot = Path.Combine(projectRoot, "Assets");
            var forbiddenNameFragments = new[]
            {
                "OWNER", "PLAINTEXT", "PLAIN_TEXT", "RAW_CODE", "RAW-CODE",
                "UNHASHED", "PASSWORD", "SECRET", "CODEBOOK"
            };
            foreach (var path in Directory.GetFiles(assetsRoot, "*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                var normalized = path.Replace('\\', '/').ToUpperInvariant();
                var relicCodeArtifact = normalized.Contains("RELIC") && normalized.Contains("CODE");
                if (relicCodeArtifact && forbiddenNameFragments.Any(normalized.Contains))
                    throw new InvalidOperationException(
                        "Owner/plaintext Relic code artifact is forbidden from Assets: " + path + ".");
            }
        }

        private static string ReadProjectSource(string projectRoot, string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ExtractBoardNarrative079(
            string source,
            string boardId,
            string nextBoardId)
        {
            var startToken = "\"boardId\": \"" + boardId + "\"";
            var endToken = "\"boardId\": \"" + nextBoardId + "\"";
            var start = source?.IndexOf(startToken, StringComparison.Ordinal) ?? -1;
            if (start < 0)
                throw new InvalidOperationException(
                    "Studio First Hour build contract cannot find board narrative: " + boardId + ".");
            var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
            if (end < 0) end = source.Length;
            return source.Substring(start, end - start);
        }

        private static void RequireSourceToken(string source, string token, string contract)
        {
            if (string.IsNullOrWhiteSpace(source) ||
                source.IndexOf(token, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Studio First Hour build contract is stale or missing: " + contract + ".");
        }

        private static void ForbidSourceToken(string source, string token, string contract)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                source.IndexOf(token, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(
                    "Studio First Hour build contract contains a forbidden shortcut: " +
                    contract + ".");
        }

        private static void WriteHashes(string outputRoot)
        {
            var manifestPath = Path.Combine(outputRoot, "BUILD_SHA256.txt");
            var lines = new List<string>();
            using (var sha = SHA256.Create())
            {
                foreach (var file in Directory.GetFiles(outputRoot, "*", SearchOption.AllDirectories)
                             .Where(path => !StringComparer.OrdinalIgnoreCase.Equals(path, manifestPath))
                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    using (var stream = File.OpenRead(file))
                    {
                        var hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                        var relative = file.Substring(outputRoot.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Replace('\\', '/');
                        lines.Add(hash + "  " + relative);
                    }
                }
            }
            File.WriteAllLines(manifestPath, lines, Encoding.ASCII);
            RequireNonEmptyFile(manifestPath);
        }

        private static string ComputeFileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireNonEmptyFile(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= 0)
                throw new InvalidOperationException("Required First Hour Gold file is missing or empty: " + path);
        }
    }
}
#endif
