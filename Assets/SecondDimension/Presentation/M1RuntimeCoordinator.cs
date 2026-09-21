using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation composition adapter for the M1 proof. Gameplay remains pure:
    /// this layer owns file I/O, converts read models, and submits explicit commands.
    /// </summary>
    public sealed partial class M1RuntimeCoordinator : IM2PresentationCoordinator,
        ISuggestedUnionPresentationCoordinator087,
        GuildCity017D.IGuildCityPresentationCoordinator017D,
        GuildCity017D.IBoardQuestDeckCoordinator090,
        GuildCity017D.IFirstHourPatrolRescueCoordinator071,
        GuildCity017D.IGuildHallGuidedProgressionCoordinator080,
        GuildCity017H.IGuildCityStrategicPresentationCoordinator017H,
        Creator028.ICreatorAccessPresentationCoordinator028,
        ISssTenV4PresentationCoordinator090,
        ISssPreparedFamilyPresentationCoordinator090
    {
        private const long TutorialCampaignSeed = 20260813L;
        private const string TutorialCampaignGuid = "00000000-0000-0000-0000-000000000002";
        private const string TutorialGuildId = "GUILD_TUTORIAL_M1";
        private const string ContentAuthorityVersion = "1.0";

        private static readonly IReadOnlyList<M1ModeView> ModeViews = Array.AsReadOnly(new[]
        {
            new M1ModeView
            {
                Id = "Relaxed",
                DisplayName = "RELAXED",
                Summary = "Faster growth, gentler enemies, full learning visibility. Death and departure are off."
            },
            new M1ModeView
            {
                Id = "Standard",
                DisplayName = "STANDARD",
                Summary = "The authored baseline. Death and departure are off."
            },
            new M1ModeView
            {
                Id = "Iron",
                DisplayName = "IRON GUILD",
                Summary = "Enemies are more assertive and injuries recover more slowly. Signed members remain permanent.",
                RequiresPermanentConsequenceConfirmation = true
            },
            new M1ModeView
            {
                Id = "OverpoweredStart",
                DisplayName = "OP START",
                Summary = "Accelerated opening growth and reinforced resources. Story, canon and consent gates remain enforced."
            },
            new M1ModeView
            {
                Id = "Custom",
                DisplayName = "CUSTOM — LATER",
                Summary = "Choose Standard, Relaxed, or Iron Guild for this adventure.",
                IsAvailable = false
            }
        });

        private static readonly KeyValuePair<string, string>[] OpeningSlots =
        {
            new KeyValuePair<string, string>("MAIN_HAND", EquipmentSlotIds.MainHand),
            new KeyValuePair<string, string>("OFF_HAND", EquipmentSlotIds.OffHand),
            new KeyValuePair<string, string>("BODY", EquipmentSlotIds.BodyArmor),
            new KeyValuePair<string, string>("ACCESSORY_1", EquipmentSlotIds.AccessoryOne),
            new KeyValuePair<string, string>("ACCESSORY_2", EquipmentSlotIds.AccessoryTwo),
            new KeyValuePair<string, string>("TOOL_RELIC", EquipmentSlotIds.ToolRelic)
        };

        private readonly M1CommandService _commands = new M1CommandService();
        private readonly M2BattleCommandService _battleCommands = new M2BattleCommandService();
        private readonly GuildCityCommandService017D _guildCityCommands = new GuildCityCommandService017D();
        private readonly GuildCityExpeditionService017D _guildCityExpeditions = new GuildCityExpeditionService017D();
        private readonly GuildCityBattleBridgeService017D _guildCityBattleBridge = new GuildCityBattleBridgeService017D();
        private readonly QuickPlayCommandService070 _quickPlay070 = new QuickPlayCommandService070();
        private readonly GuildCityDefenseService017H _guildCityDefense017H = new GuildCityDefenseService017H();
        private readonly GuildCityCanonEventService017H _guildCityCanon017H = new GuildCityCanonEventService017H();
        private RecruitAutoGenerationSigningService010 _openingRecruitSigning;
        private RecruitStarterEquipment094 _starterEquipment094;
        private GuildCityRecruitmentService017D _guildCityRecruitment;
        private readonly AtomicSaveStore _saveStore = new AtomicSaveStore();
        private readonly string _savePath;
        private readonly Dictionary<string, ScoutingReport> _scouting =
            new Dictionary<string, ScoutingReport>(StringComparer.Ordinal);
        private RecruitmentContent _recruitmentContent;
        private M2CombatContent _combatContent;
        private GuildCityContent017D _guildCityContent;
        private GuildCityStrategicContent017H _guildCityStrategicContent017H;
        private DeepProgressionCatalog070 _deepProgressionCatalog070;
        private RecruitTreeProgressionService070 _recruitTreeProgression070;
        private FirstHourRosterService071 _firstHourRoster071;
        private EncounterRosterResolver070 _encounterRosterResolver070;
        private LootResolver070 _lootResolver070;
        private CampaignState _campaign;
        private bool _saveReloadVerified;
        private string _startupNotice = string.Empty;
        private string _saveRecoveryDiagnostic = string.Empty;

        public M1RuntimeCoordinator()
            : this(null, null)
        {
        }

        public M1RuntimeCoordinator(string contentRoot, string savePath)
        {
            _savePath = string.IsNullOrWhiteSpace(savePath)
                ? Path.Combine(Application.persistentDataPath, "second_dimension_m1_v1.json")
                : savePath;

            RequireTowerPathAvailable116(_savePath);

            RecruitAutoGenerator010 recruitAutoGenerator017D = null;
            HeroMaster300Catalog087 heroMasterCatalog089 = null;

            try
            {
                var resolvedContentRoot = string.IsNullOrWhiteSpace(contentRoot) ? ResolveRecruitmentContentRoot() : contentRoot;
                _recruitmentContent = RecruitmentContent.LoadFromDirectory(resolvedContentRoot);
                _combatContent = M2CombatContent.LoadFromDirectory(resolvedContentRoot);
                var recruitAutoCatalog = RecruitAutoGenerationCatalog010.LoadFromContentRoot(resolvedContentRoot);
                _starterEquipment094 = new RecruitStarterEquipment094(recruitAutoCatalog, _combatContent);
                recruitAutoGenerator017D = new RecruitAutoGenerator010(recruitAutoCatalog);
                _openingRecruitSigning = new RecruitAutoGenerationSigningService010(
                    _commands, recruitAutoGenerator017D);
                if (TryHeroMaster300CreatorRegistry087(out var heroMasterRegistry089))
                {
                    heroMasterCatalog089 = heroMasterRegistry089.Source;
                    _combatContent = _combatContent.WithHeroMasterGeneratedAuthority096(
                        heroMasterCatalog089, recruitAutoCatalog);
                }
                _guildCityRecruitment = new GuildCityRecruitmentService017D(
                    recruitAutoGenerator017D,
                    heroMasterCatalog089);
                InitializeTitans161();
            }
            catch (Exception exception)
            {
                _startupNotice = "Runtime authority could not be loaded: " + exception.Message;
            }

            try
            {
                var resolvedContentRoot = string.IsNullOrWhiteSpace(contentRoot)
                    ? ResolveRecruitmentContentRoot()
                    : contentRoot;
                _guildCityContent = GuildCityContent017D.LoadFromDirectory(
                    ResolveGuildCityContentRoot(resolvedContentRoot));
            }
            catch (Exception exception)
            {
                AppendStartupNotice("Guild/city authority could not be loaded: " + exception.Message);
            }

            try
            {
                var resolvedContentRoot017H = string.IsNullOrWhiteSpace(contentRoot) ? ResolveRecruitmentContentRoot() : contentRoot;
                _guildCityStrategicContent017H = GuildCityStrategicContent017H.LoadFromDirectory(
                    ResolveGuildCityStrategicContentRoot017H(resolvedContentRoot017H));
            }
            catch (Exception exception)
            {
                AppendStartupNotice("Guild/city defense and canon authority could not be loaded: " + exception.Message);
            }

            try
            {
                var resolvedContentRoot070 = string.IsNullOrWhiteSpace(contentRoot)
                    ? ResolveRecruitmentContentRoot()
                    : contentRoot;
                _deepProgressionCatalog070 = DeepProgressionCatalog070.LoadFromContentRoot(
                    resolvedContentRoot070);
                _recruitTreeProgression070 = new RecruitTreeProgressionService070(
                    _deepProgressionCatalog070);
                _lootResolver070 = LootResolver070.LoadFromContentRoot(resolvedContentRoot070);
            }
            catch (Exception exception)
            {
                AppendStartupNotice("Deep recruit paths and weapon loot could not be loaded: " + exception.Message);
                _deepProgressionCatalog070 = null;
                _recruitTreeProgression070 = null;
                _lootResolver070 = null;
            }

            if (recruitAutoGenerator017D != null)
                _guildCityRecruitment = new GuildCityRecruitmentService017D(
                    recruitAutoGenerator017D,
                    heroMasterCatalog089,
                    _recruitTreeProgression070);

            try
            {
                var resolvedEnemyContentRoot070 = string.IsNullOrWhiteSpace(contentRoot)
                    ? ResolveRecruitmentContentRoot()
                    : contentRoot;
                _encounterRosterResolver070 = EncounterRosterResolver070.LoadFromContentRoot(
                    resolvedEnemyContentRoot070);
            }
            catch (Exception exception)
            {
                AppendStartupNotice("Varied enemy encounter authority could not be loaded: " + exception.Message);
                _encounterRosterResolver070 = null;
            }

            try
            {
                var resolvedFirstHourContentRoot071 = string.IsNullOrWhiteSpace(contentRoot)
                    ? ResolveRecruitmentContentRoot()
                    : contentRoot;
                _firstHourRoster071 = FirstHourRosterService071.LoadFromContentRoot(
                    resolvedFirstHourContentRoot071);
            }
            catch (Exception exception)
            {
                AppendStartupNotice("First-hour authored roster could not be loaded: " + exception.Message);
                _firstHourRoster071 = null;
            }

            if (File.Exists(_savePath))
            {
                var loaded = _saveStore.ReadWithRecovery(_savePath);
                if (loaded.IsSuccess)
                {
                    _campaign = loaded.Value.CampaignState;
                    EnsureCommittedTutorialBoardForResume();
                    EnsureFirstHourRosterForResume071();
                    RestorePersistedScoutingReports();
                    RefreshTutorialBattleRulesForResume();
                    var automaticSss107 = SssAutomaticRewards107.ApplyReady(_campaign);
                    if (automaticSss107.IsSuccess)
                        automaticSss107 = ApplyAutomaticEarnedRecruitGrowth136(automaticSss107.Value);
                    if (!automaticSss107.IsSuccess)
                        AppendStartupNotice(FriendlyErrors(automaticSss107.Errors));
                    else if (!ReferenceEquals(_campaign, automaticSss107.Value))
                    {
                        var settled107 = ApplyAndPersist(automaticSss107, notify: false);
                        if (!settled107.Succeeded) AppendStartupNotice(settled107.Message);
                    }
                }
                else
                {
                    // A stale or corrupt local save is not a runtime-authority
                    // failure. Leave every file untouched and make the technical
                    // details available on demand instead of opening the title on
                    // a red failure banner.
                    _saveRecoveryDiagnostic = FriendlyErrors(loaded.Errors);
                }
            }
        }

        public event Action Changed;

        public M1PresentationState State => BuildPresentationState();

        public GuildCity017D.GuildCityPresentationState017D GuildCity017D =>
            BuildGuildCityPresentationState017D();

        public GuildCity017H.GuildCityStrategicPresentationState017H GuildCityStrategic017H =>
            BuildGuildCityStrategicPresentationState017H();

        public bool DeepProgressionReady070 =>
            _deepProgressionCatalog070 != null && _recruitTreeProgression070 != null;

        public bool WeaponLootReady070 => _lootResolver070 != null;

        public bool EnemyVarietyReady070 => _encounterRosterResolver070 != null;

        public Result<QuickPlayOutcome070> ContinueStoryQuickPlay070(string markedStoryContractId)
        {
            if (_campaign == null || _guildCityContent == null)
                return Result<QuickPlayOutcome070>.Failure("QUICK_PLAY_070_GUILD_REQUIRED");
            var boundary = ReleaseIdleTowerForGuildProgression107(_campaign);
            if (!boundary.IsSuccess) return Result<QuickPlayOutcome070>.Failure(boundary.Errors.ToArray());
            var routed = _quickPlay070.ContinueStory(
                boundary.Value,
                _guildCityContent,
                markedStoryContractId,
                _guildCityStrategicContent017H);
            if (!routed.IsSuccess) return routed;

            var outcome = routed.Value;
            var saved = ApplyAndPersist(
                Result<CampaignState>.Success(outcome.Campaign),
                notify: true,
                outcome.Title + ". " + outcome.Guidance);
            if (!saved.Succeeded)
                return Result<QuickPlayOutcome070>.Failure(saved.Message);

            return Result<QuickPlayOutcome070>.Success(new QuickPlayOutcome070(
                _campaign,
                outcome.Destination,
                outcome.Title,
                outcome.Guidance,
                outcome.ContractId,
                outcome.RequirementId,
                outcome.ReusedSavedUnionPlan,
                outcome.AcceptedContract,
                outcome.StartedExpedition));
        }

        public M1CommandResult PlaceGuildCityBuilding017D(string plotId, string buildingId) =>
            ApplyGuildCityAndPersist(_guildCityCommands.PlaceBuilding(_campaign, _guildCityContent, plotId, buildingId),
                "Building placed and the city save was committed.");

        public M1CommandResult UpgradeGuildCityBuilding017D(string plotId) =>
            ApplyGuildCityAndPersist(_guildCityCommands.UpgradeBuilding(_campaign, _guildCityContent, plotId),
                "Building upgraded and saved.");

        public M1CommandResult AssignGuildCityStaff017D(string plotId, string recruitId) =>
            ApplyGuildCityAndPersist(_guildCityCommands.AssignStaff(_campaign, plotId, recruitId),
                "Staff assignment saved. The member remains deployable.");

        public M1CommandResult RecallGuildCityStaff017D(string recruitId) =>
            ApplyGuildCityAndPersist(_guildCityCommands.DeployStaffMember(_campaign, recruitId),
                "Staff member recalled for deployment.");

        public M1CommandResult SetGuildCityAssignment017D(string recruitId, string assignmentKind)
        {
            if (!Enum.TryParse(assignmentKind, true, out GuildMemberAssignmentKind017D kind))
                return M1CommandResult.Failure("Unknown Guild assignment: " + assignmentKind);
            return ApplyGuildCityAndPersist(_guildCityCommands.SetAssignment(_campaign, recruitId, kind),
                kind == GuildMemberAssignmentKind017D.Training
                    ? "Training saved. This member stays home and gains progress after the next completed operation."
                    : "Parallel Guild assignment saved.");
        }

        public M1CommandResult ArchiveGuildCityMember017D(string recruitId, bool confirmed) =>
            ApplyGuildCityAndPersist(_guildCityCommands.ArchiveMemberExplicit(_campaign, recruitId, confirmed),
                "Member archived by explicit Guildmaster command. The complete record remains saved.");

        public M1CommandResult CommitGuildCityApplicantBoard017D()
        {
            if (_guildCityRecruitment == null || _recruitmentContent == null)
                return M1CommandResult.Failure("Recruitment is unavailable right now. " + _startupNotice);
            return ApplyEarnedRecruitmentBoard124(
                _guildCityRecruitment.CommitBoard(_campaign, _recruitmentContent, _guildCityContent),
                "Earned contacts joined the board. Your current interviews remain available.");
        }

        public M1CommandResult RefreshGuildCityApplicantBoard017D()
        {
            if (_guildCityRecruitment == null || _recruitmentContent == null)
                return M1CommandResult.Failure("Recruitment is unavailable right now. " + _startupNotice);
            return ApplyEarnedRecruitmentBoard124(
                _guildCityRecruitment.RefreshBoard(_campaign, _recruitmentContent, _guildCityContent),
                "Earned contacts joined the board. The recruitment notice fee was paid; current interviews remain available.");
        }

        public M1CommandResult SignGuildCityApplicant017D(string recruitId)
        {
            if (_guildCityRecruitment == null)
                return M1CommandResult.Failure("Recruitment is unavailable right now. " + _startupNotice);
            var applicant = _campaign?.Guild?.GuildCity?.RecruitmentBoard
                ?.FindApplicant(recruitId);
            var duplicate = _guildCityRecruitment
                .DescribeHeroMasterDuplicate089(_campaign, applicant);
            return ApplyGuildCityAndPersist(
                _guildCityRecruitment.SignApplicant(_campaign, recruitId),
                duplicate.IsDuplicateOffer
                    ? duplicate.Summary +
                      ". The matching copy merged into the existing hero; no duplicate roster member was created."
                    : "New adventurer recruited permanently. Their starting gear is equipped, their record is saved, and they are ready for the party.");
        }

        public M1CommandResult ConfirmFirstContractUnionBriefing080() =>
            ApplyAndPersist(
                GuildHallGuidedProgression080.ConfirmFirstContractUnionBriefing080(_campaign),
                notify: false,
                "Union assignment confirmed. The six visible slots and saved battle plan are ready for Kiri's first order.");

        public M1CommandResult AcknowledgeFirstFacilityPayoff080() =>
            ApplyAndPersist(
                GuildHallGuidedProgression080.AcknowledgeFirstFacilityPayoff080(_campaign),
                notify: false,
                "Facility payoff confirmed. Its staff and immediate Guild benefit are saved.");

        public M1CommandResult DeclineGuildCityApplicant017D(string recruitId)
        {
            if (_guildCityRecruitment == null)
                return M1CommandResult.Failure("Recruitment is unavailable. " + _startupNotice);
            return ApplyGuildCityAndPersist(
                _guildCityRecruitment.DeclineApplicant(_campaign, recruitId),
                "Applicant declined for this board. The remaining interviews are still available.");
        }

        public M1CommandResult AcceptGuildCityContract017D(string contractId)
        {
            var boundary = ReleaseIdleTowerForGuildProgression107(_campaign);
            if (!boundary.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(boundary.Errors));
            return ApplyGuildCityAndPersist(_guildCityExpeditions.AcceptContract(boundary.Value, _guildCityContent, contractId),
                "Contract accepted. Your party and route are saved.");
        }

        public M1CommandResult StartGuildCityExpedition017D()
        {
            var beginsChapterTwo = StringComparer.Ordinal.Equals(
                _campaign?.Guild?.GuildCity?.ActiveContract?.ContractId,
                GuildCityExpeditionService017D.SecondStoryContractId076);
            return ApplyGuildCityAndPersist(
                _guildCityExpeditions.StartExpedition(
                    _campaign, _guildCityContent, _guildCityStrategicContent017H),
                beginsChapterTwo
                    ? "The Wayglass opened a sealed stair beneath the Hall. Kiri found a surviving apprentice at the threshold; hear them before choosing a route."
                    : "Lantern Road patrol begun. Follow the gold waymarkers toward the missing patrol and their Wayglass.");
        }

        public bool NeedsLegacyFirstRescueMigration069() =>
            GuildCityExpeditionService017D.NeedsLegacyFirstRescueMigration069(_campaign);

        public M1CommandResult MigrateLegacyFirstRescue069() =>
            ApplyGuildCityAndPersist(
                _guildCityExpeditions.MigrateLegacyFirstRescue069(
                    _campaign, _guildCityContent, _guildCityStrategicContent017H),
                "Your older first-story save was moved safely onto Lantern Road. Your Guild, members, equipment, rewards, and completed progress were preserved. Follow the gold waymarkers.");

        public M1CommandResult MoveGuildCityExpedition017D(string destinationNodeId) =>
            ApplyGuildCityAndPersist(_guildCityExpeditions.CommitMove(_campaign, _guildCityContent, destinationNodeId),
                "You reached the next part of the route. Progress saved.");

        public M1CommandResult CommitBoardQuestCard090(string cardId)
        {
            var beforeReward092 = _campaign;
            var offer = _guildCityExpeditions
                .BuildQuestCardRow090(
                    _campaign,
                    _guildCityContent,
                    _guildCityRecruitment)
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.CardId, cardId));
            var result092 = ApplyGuildCityAndPersist(
                _guildCityExpeditions.CommitQuestCard090(
                    _campaign,
                    _guildCityContent,
                    _guildCityRecruitment,
                    cardId),
                offer == null
                    ? "Quest card resolved and progress saved."
                    : offer.Category == "BOON" || offer.Category == "SCAR" || offer.Category == "FATE"
                        ? "Your fate card is saved. Roll or spin to reveal its result, then collect it to continue."
                    : offer.Title + " resolved. " + offer.RewardPreview +
                      ". Your pawn moved and the quest was saved.");
            var earnedItem092 = offer != null && (offer.Category == "CHEST" || offer.Category == "MERCHANT")
                ? GuildCityExpeditionService017D.CreateQuestCardEquipment090(offer.CardId,
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + offer.CardId,
                    offer.Category == "MERCHANT") : null;
            var finalReceipt092 = WithChestReceipt092(result092, beforeReward092, _campaign, earnedItem092);
            if (finalReceipt092?.LootReward092 != null && !string.IsNullOrWhiteSpace(offer?.HeroName) &&
                _campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityRecruitmentService017D.ChestRecruitReceiptPrefix092 + cardId))
                finalReceipt092.LootReward092.Summary += "\nRARE HERO INVITATION: " + offer.HeroName +
                    " • claim FREE at Recruitment";
            return finalReceipt092;
        }

        public M1CommandResult ResolveGuildCityCheck017D(string eventId, string actorRecruitId,
            string assistantRecruitId, int modifier) =>
            ApplyGuildCityAndPersist(_guildCityExpeditions.ResolveCommittedCheck(_campaign, _guildCityContent,
                    eventId, actorRecruitId, assistantRecruitId, modifier),
                "The dice are settled for this challenge.");

        public M1CommandResult DiscoverGateworksMaintenancePassage066() =>
            ApplyGuildCityAndPersist(_guildCityExpeditions.DiscoverGateworksMaintenancePassage066(_campaign),
                "Secret found. The safer passage preserves one supply for the road ahead.");

        public M1CommandResult RescueFirstHourLanternPatrol071()
        {
            if (_firstHourRoster071 == null)
                return M1CommandResult.Failure(
                    "The Lantern Patrol roster is unavailable. The rescue was not changed or saved.");

            var rescued = _guildCityExpeditions.MarkFirstHourLanternPatrolRescued071(
                _campaign, _guildCityContent);
            if (!rescued.IsSuccess)
                return ApplyGuildCityAndPersist(
                    rescued,
                    "The Lantern Patrol is safe.");

            var rostered = _firstHourRoster071.EnsureLanternPatrol(rescued.Value);
            if (!rostered.IsSuccess)
                return ApplyGuildCityAndPersist(
                    rostered,
                    "The Lantern Patrol is safe.");

            var reinforced = FormFirstHourReinforcementUnions071(rostered.Value);
            return ApplyGuildCityAndPersist(
                reinforced,
                "Zorin's Lantern Patrol is safe, the Wayglass is secured, and all ten patrol members joined your permanent roster. Reinforcement Unions formed where space allowed. Face the Gate-Eater together.");
        }

        public M1CommandResult CommitGuildCityEncounter017D(string encounterId) =>
            ApplyGuildCityAndPersist(_guildCityExpeditions.CommitEncounter(_campaign, _guildCityContent, encounterId, _guildCityStrategicContent017H),
                "The enemy stands in your way. Get ready to fight.");

        public M1CommandResult StartCommittedGuildCityBattle017D() =>
            ApplyGuildCityAndPersist(_guildCityBattleBridge.StartCertifiedEncounter(
                    _campaign, _battleCommands, _combatContent, _encounterRosterResolver070),
                "Battle started.");

        public M1CommandResult FinalizeGuildCityOperation017D()
        {
            var firstStoryOperation = StringComparer.Ordinal.Equals(
                _campaign?.Guild?.GuildCity?.ActiveContract?.ContractId,
                GuildCityExpeditionService017D.FirstStoryContractId066);
            var finalized = _guildCityExpeditions.FinalizeCompletedExpedition(
                _campaign, _guildCityContent);
            if (finalized.IsSuccess && firstStoryOperation &&
                finalized.Value.Guild.GuildCity.ActiveContract?.Completed == true)
            {
                if (_firstHourRoster071 == null)
                    return M1CommandResult.Failure(
                        "The rescued Lantern Patrol could not be added because its authored roster is unavailable.");
                finalized = _firstHourRoster071.EnsureLanternPatrol(finalized.Value);
            }
            return ApplyGuildCityAndPersist(
                finalized,
                firstStoryOperation
                    ? "The Gate-Eater fell. Zorin's ten Lanterns are home, the Wayglass is secure, and Chapter 2 is ready."
                    : "Operation results, parallel progress, contract rewards, and city resources were saved.");
        }

        public M1CommandResult AddGuildCityRelationshipMemory017D(string firstRecruitId, string secondRecruitId,
            string sourceId, string summary, int strength, string sceneId) =>
            ApplyGuildCityAndPersist(_guildCityCommands.AddRelationshipMemory(_campaign, firstRecruitId,
                    secondRecruitId, sourceId, summary, strength, sceneId),
                "Relationship memory created from shared play without consuming an operation.");

        public M1CommandResult ViewGuildCityRelationshipScene017D(string sceneId) =>
            ApplyGuildCityAndPersist(_guildCityCommands.ViewRelationshipScene(_campaign, sceneId),
                "Free Hall scene viewed. No operation or contract opportunity was consumed.");

        public M1CommandResult StartGuildCityDefense017H(string profileId) =>
            ApplyGuildCityAndPersist(_guildCityDefense017H.StartDefense(_campaign, _guildCityStrategicContent017H, profileId),
                "City defense prepared. Lane assignments and wave state were committed.");

        public M1CommandResult AssignGuildCityDefenseUnion017H(string laneId, string unionId) =>
            ApplyGuildCityAndPersist(_guildCityDefense017H.AssignUnion(_campaign, _guildCityStrategicContent017H, laneId, unionId),
                "Union assigned to the defense lane. The assignment remains player-controlled.");

        public M1CommandResult CommitGuildCityDefenseWave017H() =>
            ApplyGuildCityAndPersist(_guildCityDefense017H.CommitCurrentWave(_campaign, _guildCityStrategicContent017H),
                "Defense wave committed. Support waves resolve deterministically; decisive breaches create a certified Union encounter.");

        public M1CommandResult StartCommittedGuildCityDefenseBattle017H() =>
            ApplyGuildCityAndPersist(_guildCityBattleBridge.StartCertifiedEncounter(
                    _campaign, _battleCommands, _combatContent, _encounterRosterResolver070),
                "Certified Union battle started for the committed defense breach.");

        public M1CommandResult FinalizeGuildCityDefense017H() =>
            ApplyGuildCityAndPersist(_guildCityDefense017H.FinalizeDefense(_campaign),
                "City defense finalized. Buildings, defenders, XP, and city integrity were saved.");

        public M1CommandResult SynchronizeGuildCityCanonEvents017H() =>
            ApplyGuildCityAndPersist(_guildCityCanon017H.SynchronizeAvailability(_campaign, _guildCityStrategicContent017H),
                "Chronicle and canon-event availability synchronized to the current story gates.");

        public M1CommandResult ViewGuildCityCanonEvent017H(string eventId) =>
            ApplyGuildCityAndPersist(_guildCityCanon017H.ViewEvent(_campaign, _guildCityStrategicContent017H, eventId),
                "Canon event viewed without consuming an operation.");

        public M1CommandResult ResolveGuildCityCanonEvent017H(string eventId, string choiceId) =>
            ApplyGuildCityAndPersist(_guildCityCanon017H.ResolveEvent(_campaign, _guildCityStrategicContent017H, eventId, choiceId),
                "Canon event resolved through its story-safe authority and saved.");

        public M1CommandResult CreateGuild(M1NewGuildIntent intent)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            if (intent == null) return M1CommandResult.Failure("A New Guild request is required.");
            if (_recruitmentContent == null) return M1CommandResult.Failure(_startupNotice);
            if (!TryMode(intent.ModeId, out var mode)) return M1CommandResult.Failure("Choose an available campaign mode.");
            if (!TryTutorialDepth(intent.TutorialDepthId, out var tutorialDepth))
                return M1CommandResult.Failure("Choose an available tutorial depth.");

            try
            {
                var accessibility = new AccessibilitySettingsState(
                    Mathf.RoundToInt(Mathf.Clamp(intent.TextScale, 0.85f, 1.45f) * 100f),
                    intent.HighContrast,
                    intent.ReducedMotion);
                var profile = new NewGuildProfileState(
                    intent.GuildmasterName,
                    mode,
                    tutorialDepth,
                    accessibility,
                    ironConsequencesAcknowledged: mode == GameMode.Iron);
                var command = new NewGuildCommand(
                    TutorialCampaignGuid,
                    TutorialCampaignSeed,
                    ContentAuthorityVersion,
                    TutorialGuildId,
                    profile);

                var create = _commands.CreateNewGuild(command);
                if (!create.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(create.Errors));
                var charter = _commands.AcceptCivicCharter(create.Value);
                if (!charter.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(charter.Errors));
                var board = CreateTutorialBoardState();
                var commit = ApplyAndPersist(
                    _commands.CommitApplicantBoard(charter.Value, board),
                    notify: false);
                if (!commit.Succeeded) return commit;

                _startupNotice = string.Empty;
                _saveRecoveryDiagnostic = string.Empty;
                _saveReloadVerified = false;
                NotifyChanged();
                return M1CommandResult.Success("Guild charter saved. Your six founding companions are waiting to meet you.");
            }
            catch (Exception exception)
            {
                return M1CommandResult.Failure("New Guild could not be created: " + exception.Message);
            }
        }

        public M1CommandResult SignRecruit(string recruitId) =>
            _openingRecruitSigning == null
                ? M1CommandResult.Failure(string.IsNullOrWhiteSpace(_startupNotice)
                    ? "Applicants are unavailable right now. Please try again."
                    : _startupNotice)
                : ApplyAndPersist(
                    _openingRecruitSigning.SignApplicant(_campaign, recruitId),
                    notify: true,
                    "This companion joined the Guild and received starter gear.");

        public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) =>
            ApplyAndPersist(_commands.EquipItem(_campaign, recruitId, slotId, itemId), notify: true, "Equipment change saved.");

        public M1CommandResult UnequipItem(string recruitId, string slotId) =>
            ApplyAndPersist(_commands.UnequipItem(_campaign, recruitId, slotId), notify: true, "Item returned to inventory. Re-equip it before continuing if the loadout needs it.");

        public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) =>
            ApplyAndPersist(_commands.SetEquipmentLock(_campaign, recruitId, slotId, locked), notify: true, locked ? "Equipment locked and saved." : "Equipment unlocked and saved.");

        public M1CommandResult CompleteEquipmentReview() =>
            ApplyAndPersist(_commands.CompleteEquipmentReview(_campaign), notify: true, "Starter loadouts accepted. You can change equipment at any time.");

        public M1CommandResult AddUnion() =>
            ApplyAndPersist(_commands.AddOpeningUnion(_campaign), notify: true, UnionAddNotice165());

        public M1CommandResult RemoveUnion(int unionIndex) =>
            ApplyAndPersist(_commands.RemoveOpeningUnion(_campaign, unionIndex), notify: true, "The empty Union plan was removed.");

        public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) =>
            ApplyAndPersist(_commands.AssignRecruitToUnion(_campaign, recruitId, unionIndex, slotIndex), notify: true, "Union updated. The member in slot 1 leads automatically.");

        public M1CommandResult UnassignRecruitFromUnion(string recruitId) =>
            ApplyAndPersist(_commands.UnassignRecruitFromUnion(_campaign, recruitId), notify: true, "Recruit returned to the available roster.");

        public M1CommandResult ApplySuggestedRoleUnions087() =>
            ApplyAndPersist(
                _commands.ApplySuggestedRoleUnions087(_campaign),
                notify: true,
                "Suggested Unions saved. Healers, tanks, mages, ranged fighters, and warriors now train in role-focused teams.");

        public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) =>
            ApplyAndPersist(_commands.SetUnionLeader(_campaign, unionIndex, recruitId), notify: true, "Recruit moved to slot 1 and made leader.");

        public M1CommandResult SetFormation(int unionIndex, string formationId) =>
            ApplyAndPersist(_commands.SetFormation(_campaign, unionIndex, formationId), notify: true, "Formation saved.");

        public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) =>
            ApplyAndPersist(_commands.SetDoctrine(_campaign, unionIndex, doctrineId), notify: true, "Doctrine saved.");

        public M1CommandResult SaveAndReloadProof()
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            var completedOpening = _commands.CompleteOpening(_campaign);
            if (completedOpening.IsSuccess)
            {
                if (_firstHourRoster071 == null)
                    return M1CommandResult.Failure(
                        "The authored first-hour roster is unavailable. The opening was not finalized.");
                completedOpening = _firstHourRoster071.EnsureCharterRoster(completedOpening.Value);
            }
            var complete = ApplyAndPersist(completedOpening, notify: false);
            if (!complete.Succeeded) return complete;

            var beforeHash = CanonicalJson.Sha256Hex(_campaign);
            var loaded = _saveStore.ReadWithRecovery(_savePath);
            if (!loaded.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(loaded.Errors));
            var afterHash = CanonicalJson.Sha256Hex(loaded.Value.CampaignState);
            if (!StringComparer.Ordinal.Equals(beforeHash, afterHash) ||
                !StringComparer.Ordinal.Equals(afterHash, loaded.Value.CanonicalStateHash))
            {
                return M1CommandResult.Failure("The saved guild record did not match. Please save again.");
            }

            _campaign = loaded.Value.CampaignState;
            _saveReloadVerified = true;
            NotifyChanged();
            return M1CommandResult.Success(
                "Party readiness confirmed. Ten named Guild members, equipment, and Union plans are saved.");
        }

        public M1CommandResult StartTutorialBattle() =>
            ApplyAndPersist(
                _battleCommands.StartTutorialBattle(_campaign, _combatContent),
                notify: true,
                "Tutorial battle ready. Union commands will not reroll when this screen is rebuilt.");

        public M1CommandResult SelectForecast(string unionId, string forecastId) =>
            ApplyAndPersist(
                _battleCommands.SelectForecast(_campaign, unionId, forecastId),
                notify: true,
                "Complete Union command selected and saved.");

        public M1CommandResult ConfirmBattleRound() =>
            ApplyAndPersist(
                _battleCommands.ConfirmRound(_campaign, _combatContent),
                notify: true,
                "Round resolved from the committed Union commands.");

        public M1CommandResult ReplayTutorialBattle() =>
            ApplyAndPersist(
                _battleCommands.ReplayTutorialBattle(_campaign, _combatContent),
                notify: true,
                "Deterministic replay verified: event log and state hash are identical.");

        public M1CommandResult RetryTutorialBattle() =>
            ApplyAndPersist(
                _battleCommands.RestartTutorialBattle(_campaign, _combatContent),
                notify: true,
                "Tutorial battle reset safely to its committed opening state.");

        public M1CommandResult ClaimBattleRewards()
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            var prepared108 = TryBuildClaimedBattleRewards108(
                _campaign, out var candidate, out var migratedLegacyRescue069);
            if (!prepared108.Succeeded) return prepared108;
            // Run the existing return/migration chain first: an older claimed
            // battle may still need repair. A fully settled retry must retain
            // both the primary save and its previous recovery backup.
            if (_campaign?.Battle?.Reward?.Claimed == true &&
                (ReferenceEquals(candidate, _campaign) ||
                 CanonicalJson.Sha256Hex(candidate) == CanonicalJson.Sha256Hex(_campaign)))
                return M1CommandResult.Success("These battle rewards are already saved.");
            return ApplyAndPersist(Result<CampaignState>.Success(candidate), notify: true,
                migratedLegacyRescue069
                    ? "Battle rewards claimed and saved. Your older first-story route was updated without repeating the battle; now follow the gold Lantern Road waymarkers home."
                    : "Battle rewards claimed. Character, Art, Guild, Hall, expedition, relationship, and board-return state were saved.");
        }

        private M1CommandResult TryBuildClaimedBattleRewards108(
            CampaignState source,
            out CampaignState claimedCampaign108,
            out bool migratedLegacyRescue069)
        {
            claimedCampaign108 = null;
            migratedLegacyRescue069 = false;
            var claimed = _battleCommands.ClaimBattleRewards(source);
            if (!claimed.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(claimed.Errors));
            var candidate = claimed.Value;
            var equipmentGrowth022 = new SecondDimension.Gameplay.Campaign022.BattleEquipmentGrowthBridge022()
                .ApplyClaimedBattleGrowth(candidate, Registry022());
            if (!equipmentGrowth022.IsSuccess)
                return M1CommandResult.Failure(FriendlyErrors(equipmentGrowth022.Errors));
            candidate = equipmentGrowth022.Value;
            var version70Loot = ApplyVersion70WeaponLoot070(
                candidate,
                source?.Guild?.GuildCity?.PendingEncounter,
                source?.Battle);
            if (!version70Loot.IsSuccess)
                return M1CommandResult.Failure(FriendlyErrors(version70Loot.Errors));
            candidate = version70Loot.Value;
            if (candidate.Guild.GuildCity?.PendingEncounter != null)
            {
                var activeDefense017H = candidate.Guild.GuildCity.Strategic017H?.ActiveDefense;
                if (SecondDimension.Gameplay.TitanTrials160.TitanTrialCommands161.OwnsBattle(candidate))
                {
                    var titanReturn161 = SecondDimension.Gameplay.TitanTrials160.TitanTrialCommands161.ApplyClaimedReturn(candidate, _titanCatalog161);
                    if (!titanReturn161.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(titanReturn161.Errors));
                    candidate = titanReturn161.Value;
                }
                else if (_campaignCommands022.HasActiveAbyssEncounter(candidate, Registry022()))
                {
                    var committedAbyssReturn022 = _campaignCommands022.CommitAbyssBattleReturn(candidate, Registry022());
                    if (!committedAbyssReturn022.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(committedAbyssReturn022.Errors));
                    var appliedAbyssReturn022 = _campaignCommands022.ApplyAbyssBattleReturnExactlyOnce(committedAbyssReturn022.Value, Registry022());
                    if (!appliedAbyssReturn022.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(appliedAbyssReturn022.Errors));
                    candidate = appliedAbyssReturn022.Value;
                }
                else if (HasPendingPersonalQuestBattle029(candidate))
                {
                    var personalReturn029 = ApplyPersonalQuestBattleReturn029(candidate);
                    if (!personalReturn029.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(personalReturn029.Errors));
                    candidate = personalReturn029.Value;
                }
                else if (activeDefense017H != null && activeDefense017H.Status == CityDefenseStatus017H.AwaitingBattle)
                {
                    var appliedDefense017H = _guildCityDefense017H.ApplyDecisiveBattleAfterClaim(candidate, _guildCityStrategicContent017H);
                    if (!appliedDefense017H.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(appliedDefense017H.Errors));
                    candidate = appliedDefense017H.Value;
                }
                else if (HasActiveWorldGateEncounter023(candidate))
                {
                    var committedReturn = _guildCityBattleBridge.CommitBattleReturn(candidate);
                    if (!committedReturn.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(committedReturn.Errors));
                    var appliedReturn = _guildCityBattleBridge.ApplyBattleReturnExactlyOnce(committedReturn.Value);
                    if (!appliedReturn.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(appliedReturn.Errors));
                    var synchronized023 = SynchronizeClaimedWorldGateBattle023(appliedReturn.Value);
                    if (!synchronized023.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(synchronized023.Errors));
                    candidate = synchronized023.Value;
                }
                else if (HasActiveCampaignEncounter019(candidate))
                {
                    var campaignReturn019 = CommitClaimedCampaignBattle019(candidate);
                    if (!campaignReturn019.IsSuccess)
                        return M1CommandResult.Failure(FriendlyErrors(campaignReturn019.Errors));
                    candidate = campaignReturn019.Value;
                }
                else
                {
                    var committedReturn = _guildCityBattleBridge.CommitBattleReturn(candidate);
                    if (!committedReturn.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(committedReturn.Errors));
                    var appliedReturn = _guildCityBattleBridge.ApplyBattleReturnExactlyOnce(committedReturn.Value);
                    if (!appliedReturn.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(appliedReturn.Errors));
                    candidate = appliedReturn.Value;
                }
            }
            if (GuildCityExpeditionService017D.NeedsLegacyFirstRescueMigration069(candidate))
            {
                var migration069 = _guildCityExpeditions.MigrateLegacyFirstRescue069(
                    candidate, _guildCityContent, _guildCityStrategicContent017H);
                if (!migration069.IsSuccess)
                    return M1CommandResult.Failure(FriendlyErrors(migration069.Errors));
                candidate = migration069.Value;
                migratedLegacyRescue069 = true;
            }
            if(!SecondDimension.Gameplay.TitanTrials160.TitanTrialCommands161.OwnsBattle(source) &&
                (HasActiveWorldGateEncounter023(source)||HasActiveCampaignEncounter019(source)))
            {
                var recovery151=SecondDimension.Gameplay.Campaign019.CampaignRunRecoveryCommands151.ObserveSettlement(source,candidate);
                if(!recovery151.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(recovery151.Errors));
                candidate=recovery151.Value;
            }
            claimedCampaign108 = candidate;
            return M1CommandResult.Success();
        }

        private Result<CampaignState> ApplyVersion70WeaponLoot070(
            CampaignState claimedCampaign,
            EncounterLaunchRequest017D encounter,
            BattleState terminalBattle)
        {
            if (_lootResolver070 == null || encounter == null || terminalBattle == null)
                return Result<CampaignState>.Success(claimedCampaign);
            try
            {
                var missionRank = Math.Max(1, Math.Min(7, encounter.EnemyUnionCount));
                var receipt = _lootResolver070.ResolveReceipt(
                    _campaign,
                    encounter,
                    terminalBattle,
                    missionRank);
                return _lootResolver070.ApplyExactlyOnce(claimedCampaign, receipt);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "LOOT070_RUNTIME_INTEGRATION_REJECTED: " + exception.Message);
            }
        }

        private Result<CampaignState> FormFirstHourReinforcementUnions071(
            CampaignState campaign) =>
            _commands.MaterializeFirstHourLanternPatrolUnions071(campaign);

        private M1CommandResult ApplyGuildCityAndPersist(Result<CampaignState> result, string successMessage)
        {
            if (_guildCityContent == null)
                return M1CommandResult.Failure("Guild/city authority is unavailable. " + _startupNotice);
            return ApplyAndPersist(result, notify: true, successMessage);
        }


        public AccessibilitySettingsState SavedAccessibility156 => _campaign?.Profile?.Accessibility;

        public string AccessibilityRevision156 => _campaign?.Profile == null ? string.Empty :
            _campaign.CampaignGuid + ":" + CanonicalJson.Sha256Hex(_campaign.Profile);

        public M1CommandResult SetAccessibility156(
            string expectedRevision, int textScalePercent, bool highContrast, bool reducedMotion)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != _coordinatorThread116)
                return M1CommandResult.Failure("Options must be changed from the game screen.");
            lock (TowerPathGate116)
            {
                if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
                var source = _campaign;
                var profile = source?.Profile;
                var previous = profile?.Accessibility;
                if (previous == null || string.IsNullOrEmpty(expectedRevision) ||
                    !StringComparer.Ordinal.Equals(expectedRevision, AccessibilityRevision156))
                    return M1CommandResult.Failure("The saved Guild or its options changed. Reopen Options and try again.");
                if (textScalePercent < 85 || textScalePercent > 145)
                    return M1CommandResult.Failure("Text size must be between 85% and 145%.");
                var sourceHash = CanonicalJson.Sha256Hex(source);
                // A stale view cannot report even an unchanged setting as saved.
                if (!ReferenceEquals(_campaign, source) || !AccessibilityPrimaryMatches156(sourceHash))
                    return M1CommandResult.Failure("The saved Guild changed. Reload it before changing Options.");
                if (previous.TextScalePercent == textScalePercent && previous.HighContrast == highContrast &&
                    previous.ReducedMotion == reducedMotion)
                    return M1CommandResult.Success("These options are already saved.");
                var settings = new AccessibilitySettingsState(textScalePercent, highContrast, reducedMotion,
                    previous.ScreenShakePercent, previous.FlashIntensityPercent, previous.CombatSpeed,
                    previous.AutoAdvanceText, previous.HoldAlternatives, previous.ForecastDetail);
                var replacement = new NewGuildProfileState(profile.GuildmasterName, profile.Mode,
                    profile.TutorialDepth, settings, profile.IronConsequencesAcknowledged);
                var candidate = new CampaignState(source.CampaignGuid, source.CampaignSeed,
                    source.ContentAuthorityVersion, source.Rules, source.Guild, replacement, source.OpeningFlow,
                    source.Battle, source.SssV4090, source.EquipmentUndo112, source.NextBattleUnions132,
                    source.Recovery150, source.CatchUp153, source.TitanTrials160, source.Loops164);
                var envelope = SaveEnvelopeV1.Create(candidate, DateTime.UtcNow);
                try { _saveStore.Write(_savePath, envelope); }
                catch (Exception exception)
                {
                    // Match the existing Tower write-before-publish recovery rule,
                    // requiring the actual primary, never a fallback backup.
                    if (!AccessibilityPrimaryMatches156(envelope.CanonicalStateHash))
                    {
                        Debug.LogError("OPTIONS_SAVE_FAILED156\n" + exception);
                        return M1CommandResult.Failure("Could not verify the Options save. The last confirmed settings are shown. Reload the Guild before trying again.");
                    }
                }
                _campaign = candidate;
                _saveReloadVerified = false;
            }
            NotifyChangedAfterCommittedSave109();
            return M1CommandResult.Success("Options saved to this Guild.");
        }

        private bool AccessibilityPrimaryMatches156(string expectedHash)
        {
            try
            {
                var primary = JsonConvert.DeserializeObject<SaveEnvelopeV1>(
                    File.ReadAllText(_savePath), CanonicalJson.DefaultSettings());
                return primary?.CampaignState != null &&
                    primary.SaveFormatVersion == SaveEnvelopeV1.CurrentFormatVersion &&
                    StringComparer.Ordinal.Equals(primary.CampaignGuid, primary.CampaignState.CampaignGuid) &&
                    primary.CampaignSeed == primary.CampaignState.CampaignSeed &&
                    StringComparer.Ordinal.Equals(primary.ContentAuthorityVersion, primary.CampaignState.ContentAuthorityVersion) &&
                    StringComparer.Ordinal.Equals(primary.CanonicalStateHash, expectedHash) &&
                    StringComparer.Ordinal.Equals(CanonicalJson.Sha256Hex(primary.CampaignState), expectedHash);
            }
            catch { return false; }
        }

        private M1CommandResult ApplyAndPersist(
            Result<CampaignState> result,
            bool notify,
            string successMessage = "Saved.")
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            using var timing110 = BeginTowerTiming110("persist");
            if (!result.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(result.Errors));
            CampaignState candidate;
            try
            {
                var outfitted094 = _starterEquipment094 == null ? result.Value :
                    _starterEquipment094.ApplyToNewRecruits(_campaign, result.Value);
                timing110?.Mark("new-recruit-equipment");
                candidate = SynchronizePeopleCreator029BeforeSave(outfitted094);
                timing110?.Mark("creator-synchronization");
                var automaticSss107 = SssAutomaticRewards107.ApplyReady(candidate);
                timing110?.Mark("automatic-sss-rewards");
                if (!automaticSss107.IsSuccess)
                    return M1CommandResult.Failure(FriendlyErrors(automaticSss107.Errors));
                var automaticRecruits136 = ApplyAutomaticEarnedRecruitGrowth136(automaticSss107.Value);
                if (!automaticRecruits136.IsSuccess)
                    return M1CommandResult.Failure(FriendlyErrors(automaticRecruits136.Errors));
                candidate = UnionBattlePlanRules132.PromoteWhenIdle(automaticRecruits136.Value);
                if (_guildCityStrategicContent017H != null && candidate?.Guild?.GuildCity != null)
                {
                    var synchronized017H = GuildCityStoryGateService017H.SynchronizeDerivedGates(candidate);
                    timing110?.Mark("derived-story-gates");
                    if (!synchronized017H.IsSuccess)
                        return M1CommandResult.Failure(FriendlyErrors(synchronized017H.Errors));
                    candidate = synchronized017H.Value;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("PRE_SAVE_STATE_PREPARATION_FAILED109\n" + exception);
                return M1CommandResult.Failure(
                    "Something went wrong while preparing this update. No changes were saved. " + exception.Message);
            }

            try
            {
                var envelope110 = SaveEnvelopeV1.Create(candidate, DateTime.UtcNow);
                timing110?.Mark("save-envelope");
                _saveStore.Write(_savePath, envelope110);
                timing110?.Mark("atomic-store-write-and-readback");
            }
            catch (Exception exception)
            {
                Debug.LogError("SAVE_WRITE_FAILED109\n" + exception);
                return M1CommandResult.Failure("Something went wrong while saving. Please try again. " + exception.Message);
            }

            _campaign = candidate;
            _saveReloadVerified = false;
            if (notify) NotifyChangedAfterCommittedSave109();
            timing110?.Mark("publish-changed");
            return M1CommandResult.Success(successMessage);
        }

        private void EnsureCommittedTutorialBoardForResume()
        {
            if (_campaign?.Profile == null || _campaign.OpeningFlow == null ||
                _campaign.OpeningFlow.ApplicantBoard != null || _recruitmentContent == null)
            {
                return;
            }

            var charter = ApplyAndPersist(_commands.AcceptCivicCharter(_campaign), notify: false);
            if (!charter.Succeeded)
            {
                _startupNotice = charter.Message;
                return;
            }
            var commit = ApplyAndPersist(
                _commands.CommitApplicantBoard(_campaign, CreateTutorialBoardState()),
                notify: false);
            if (!commit.Succeeded) _startupNotice = commit.Message;
        }

        /// <summary>
        /// Release 071 upgrades an existing completed opening to the same named
        /// roster as a fresh campaign. The migration is deterministic and
        /// idempotent; it never removes an existing recruit or spends Treasury.
        /// Operation ordinal is a safe fallback for saves that have already moved
        /// from the first story contract to a later contract.
        /// </summary>
        private void EnsureFirstHourRosterForResume071()
        {
            if (SuppressAutomaticFirstHourRosterMigration072 ||
                _firstHourRoster071 == null || _campaign?.OpeningFlow == null ||
                _campaign.OpeningFlow.Stage != OpeningStage.Complete)
            {
                return;
            }

            var beforeHash = CanonicalJson.Sha256Hex(_campaign);
            var migrated = _firstHourRoster071.EnsureCharterRoster(_campaign);
            if (!migrated.IsSuccess)
            {
                AppendStartupNotice("First-hour charter roster migration was skipped: " +
                                    FriendlyErrors(migrated.Errors));
                return;
            }

            var city = migrated.Value.Guild.GuildCity;
            var firstStoryFinished = city.OperationOrdinal > 0 ||
                                     (StringComparer.Ordinal.Equals(
                                          city.ActiveContract?.ContractId,
                                          GuildCityExpeditionService017D.FirstStoryContractId066) &&
                                      city.ActiveContract.Completed);
            if (firstStoryFinished)
            {
                migrated = _firstHourRoster071.EnsureLanternPatrol(migrated.Value);
                if (!migrated.IsSuccess)
                {
                    AppendStartupNotice("First-hour Lantern Patrol migration was skipped: " +
                                        FriendlyErrors(migrated.Errors));
                    return;
                }
            }

            var afterHash = CanonicalJson.Sha256Hex(migrated.Value);
            if (StringComparer.Ordinal.Equals(beforeHash, afterHash)) return;
            var saved = ApplyAndPersist(migrated, notify: false);
            if (!saved.Succeeded)
                AppendStartupNotice("First-hour roster migration could not be saved: " +
                                    saved.Message);
        }

        private ApplicantBoardState CreateTutorialBoardState()
        {
            var detailed = new TutorialApplicantFactory(_recruitmentContent).CreateFrozenBoard();
            foreach (var slot in detailed.Applicants)
            {
                _scouting[slot.RecruitId] = slot.ScoutingReport;
            }
            return ApplicantBoardStateAdapter.ToFrozenTutorialState(detailed);
        }

        public static bool TryRestorePersistedScoutingReport(
            string recruitId,
            string canonicalJson,
            out ScoutingReport report,
            out string error)
        {
            report = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(canonicalJson)) return false;
            try
            {
                var candidate = JsonConvert.DeserializeObject<ScoutingReport>(
                    canonicalJson,
                    CanonicalJson.DefaultSettings());
                if (candidate == null || !StringComparer.Ordinal.Equals(candidate.RecruitId, recruitId))
                {
                    error = "Persisted scouting report identity mismatch for " + recruitId + ".";
                    return false;
                }
                report = candidate;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private void RestorePersistedScoutingReports()
        {
            var applicants = _campaign?.OpeningFlow?.ApplicantBoard?.Applicants;
            if (applicants == null) return;
            for (var index = 0; index < applicants.Count; index++)
            {
                var applicant = applicants[index];
                if (TryRestorePersistedScoutingReport(
                        applicant.RecruitId,
                        applicant.CanonicalScoutingReportJson,
                        out var report,
                        out var error))
                {
                    _scouting[applicant.RecruitId] = report;
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    _startupNotice = "A persisted scouting report could not be restored: " + error;
                }
            }
        }

        private void RefreshTutorialBattleRulesForResume()
        {
            if (_campaign?.Battle == null || _combatContent == null ||
                M2BattleCommandService.UsesCurrentTutorialRules(_campaign.Battle, _combatContent))
            {
                return;
            }

            var refreshed = ApplyAndPersist(
                _battleCommands.RestartTutorialBattle(_campaign, _combatContent),
                notify: false,
                "The updated Union-command tutorial was restarted safely at round 1.");
            if (!refreshed.Succeeded)
            {
                _startupNotice = refreshed.Message;
                return;
            }
            _startupNotice = "Update 011 loaded. The prior tutorial result was preserved in the guarded rollback, and the playable battle restarted at round 1 so corrected commands, learning, and cinematic presentation are active.";
        }

        private GuildCity017D.GuildCityPresentationState017D BuildGuildCityPresentationState017D()
        {
            var view = new GuildCity017D.GuildCityPresentationState017D
            {
                IsAvailable = _campaign?.Guild?.GuildCity != null && _guildCityContent != null,
                Error = _guildCityContent == null ? "Guild/city authority is unavailable." : string.Empty
            };
            if (!view.IsAvailable) return view;
            var city = _campaign.Guild.GuildCity;
            var effects = new GuildCityEffectService017D();
            view.OperationOrdinal = city.OperationOrdinal;
            view.CampaignModeId = _campaign.Profile?.Mode.ToString() ?? "Standard";
            view.TutorialDepthId = _campaign.Profile?.TutorialDepth.ToString() ?? "ContextualTips";
            var accessibility017G = _campaign.Profile?.Accessibility;
            view.TextScalePercent = accessibility017G?.TextScalePercent ?? 100;
            view.HighContrast = accessibility017G?.HighContrast ?? false;
            view.ReducedMotion = accessibility017G?.ReducedMotion ?? false;
            view.CombatSpeed = accessibility017G?.CombatSpeed ?? 1;
            view.ForecastDetail = accessibility017G?.ForecastDetail ?? "full";
            view.TotalRecruitCount = _campaign.Guild.Recruits.Count;
            view.EquippedRecruitCount = _campaign.Guild.Recruits.Count(value =>
                value.Equipment != null && value.Equipment.Assignments.Count > 0);
            view.NormalUnionCount = _campaign.Guild.Unions.Count(value =>
                value.Kind == UnionKind.Normal && value.MemberRecruitIds.Count > 0);
            view.InventoryItemCount = _campaign.Guild.Inventory.Count;
            view.ClaimedBattleRewardCount = _campaign.Guild.Development.ClaimedBattleRewardIds.Count;
            view.HasUnclaimedBattleReward = _campaign.Battle?.Reward != null && !_campaign.Battle.Reward.Claimed;
            view.CharterBuildCredits = city.CharterBuildCredits;
            view.CivicTrust = city.CivicTrust;
            view.HallEnhancementXp = _campaign.Guild.Development.HallEnhancementXp;
            view.AdjacencyBonusCount = _guildCityCommands.AdjacencyBonusCount(city, _guildCityContent);
            view.ApplicantBoardBonusSlots = effects.ApplicantBoardBonusSlots(
                _campaign.Guild.Development, city, _guildCityContent);
            view.UnionCapacityBonus = effects.UnionCapacityBonus(_campaign.Guild.Development);
            view.StartingSupplyBonus = effects.StartingSupplyBonus(
                _campaign.Guild.Development, city, _guildCityContent);
            view.ScoutInformationBonus = effects.ScoutInformationBonus(
                _campaign.Guild.Development, city, _guildCityContent);
            view.RecoveryProgressPerOperation = effects.RecoveryProgressPerOperation(
                _campaign.Guild.Development, city, _guildCityContent);
            view.TrainingProgressPerOperation = effects.TrainingProgressPerOperation(
                _campaign.Guild.Development, city, _guildCityContent);
            view.RosterCapacity = effects.RosterCapacity(_campaign.Guild.Development);
            view.TreasuryXp = _campaign.Guild.TreasuryXp;
            view.ActiveCityEffects = effects.ActiveEffectSummaries(
                _campaign.Guild.Development, city, _guildCityContent);
            view.HasRecruitmentBoard = city.RecruitmentBoard != null;
            view.CanInviteEarnedContacts124 = _guildCityRecruitment?.CanInviteEarnedContacts124(_campaign) == true;
            view.PendingExpeditionRecruitLeadNames089 = _guildCityRecruitment == null
                ? Array.Empty<string>()
                : _guildCityRecruitment.PendingExpeditionRecruitLeads089(_campaign)
                    .Select(value => value.Name)
                    .ToArray();
            view.FirstContractUnionBriefingConfirmed080 =
                GuildHallGuidedProgression080.HasFirstContractUnionBriefing080(city);
            view.FirstFacilityPayoffAcknowledged080 =
                GuildHallGuidedProgression080.HasFirstFacilityPayoff080(city);
            view.RecoveredLootEquipped080 =
                GuildHallGuidedProgression080.HasEquippedRecoveredLoot080(_campaign.Guild);
            view.RecruitmentBoardId = city.RecruitmentBoard?.BoardId ?? string.Empty;
            view.RecruitmentRefreshOrdinal = city.RecruitmentRefreshOrdinal;
            view.RecruitmentDryStreak = city.RecruitmentDryStreak;
            view.Applicants = city.RecruitmentBoard == null
                ? Array.Empty<GuildCity017D.GuildCityApplicantView017D>()
                : city.RecruitmentBoard.Applicants.Select(value =>
                {
                    var signingCost = GuildCityRecruitmentService017D
                        .EffectiveSigningCostTreasuryXp(_campaign, value.SigningCostTreasuryXp);
                    var duplicate = _guildCityRecruitment
                        ?.DescribeHeroMasterDuplicate089(_campaign, value);
                    var isDuplicate = duplicate?.IsDuplicateOffer == true;
                    var ownedRecruitId = _guildCityRecruitment != null
                        ? _guildCityRecruitment.OwnedRecruitIdForApplicant089(
                            _campaign, value)
                        : _campaign.Guild.Recruits.FirstOrDefault(recruit =>
                            StringComparer.Ordinal.Equals(
                                recruit.RecruitId, value.RecruitId))?.RecruitId ??
                          string.Empty;
                    var isOwned = !string.IsNullOrWhiteSpace(ownedRecruitId);
                    var authoredHero096 = AuthoredApplicantHero096(value);
                    return new GuildCity017D.GuildCityApplicantView017D
                    {
                        Slot = value.Slot,
                        RecruitId = value.RecruitId,
                        OwnedRecruitId = ownedRecruitId,
                        DisplayName = value.DisplayName,
                        Kind = value.Kind.ToString(),
                        RaceId = value.RaceId,
                        WorldId = value.WorldId,
                        ClassTendencyId = value.ClassTendencyId,
                        AuthoredRole096 = authoredHero096?.Role ?? string.Empty,
                        AuthoredWeaponStyle096 = authoredHero096?.Weapon ?? string.Empty,
                        LeadershipBand = value.LeadershipBand,
                        SigningCostTreasuryXp = signingCost,
                        IsSigned = isOwned,
                        ObservedSummary = string.IsNullOrWhiteSpace(value.LeadershipBand)
                            ? "Their confidence will become clearer after traveling together."
                            : "In a crisis, they tend to lead with " + Humanize(value.LeadershipBand).ToLowerInvariant() + ".",
                        VisualSeed = ReadVisualSeed(value.CanonicalApplicantJson, value.RecruitId),
                        PortraitAuthorityId = PortraitAuthority(
                            value.AuthoredStableRecruitId,
                            value.SignatureId,
                            value.RecruitId),
                        ClassSymbol = ClassSymbol(value.ClassTendencyId),
                        TraitSummary = RecurringApplicantTraits(value.CanonicalApplicantJson),
                        EquipmentSummary = ApplicantGearSummary096(value, ownedRecruitId, isDuplicate),
                        PersonalHook = RecurringApplicantPersonalHook(value.CanonicalApplicantJson),
                        CanAfford = _campaign.Guild.TreasuryXp >= signingCost &&
                                    (isDuplicate ||
                                     _campaign.Guild.Recruits.Count < view.RosterCapacity),
                        IsAscensionMerge = isDuplicate,
                        CurrentAscensionLevel = duplicate?.PreviousAscensionLevel ?? 0,
                        NextAscensionLevel = duplicate?.AscensionLevel ?? 0,
                        DuplicateMergeKind = duplicate?.Kind.ToString() ?? string.Empty,
                        DuplicateMergePreview = duplicate?.Summary ?? string.Empty
                    };
                }).ToArray();
            view.SignedApplicantCount = view.Applicants.Count(value => value.IsSigned);
            view.MaterialSummaries = city.Materials
                .Select(value => Humanize(value.MaterialId) + " ×" + value.Amount)
                .ToArray();
            view.Plots = city.CityPlots.Select(plot =>
            {
                var buildingName = string.Empty;
                if (!string.IsNullOrWhiteSpace(plot.BuildingId) &&
                    _guildCityContent.Buildings.TryGetValue(plot.BuildingId, out var building))
                    buildingName = building.DisplayName;
                return new GuildCity017D.GuildCityPlotView017D
                {
                    PlotId = plot.PlotId,
                    DistrictId = plot.DistrictId,
                    Size = plot.Size,
                    Unlocked = plot.Unlocked,
                    RoadConnected = plot.RoadConnected,
                    BuildingId = plot.BuildingId,
                    BuildingName = buildingName,
                    BuildingLevel = plot.BuildingLevel,
                    ConstructionProgress = plot.ConstructionProgress,
                    StaffRecruitIds = plot.StaffRecruitIds
                };
            }).ToArray();
            view.PlacedBuildingCount = view.Plots.Count(value => !string.IsNullOrWhiteSpace(value.BuildingId));
            view.StaffedBuildingCount = view.Plots.Count(value => value.StaffRecruitIds != null && value.StaffRecruitIds.Count > 0);
            view.UpgradedBuildingCount = view.Plots.Count(value => value.BuildingLevel > 1);
            view.Buildings = _guildCityContent.Buildings.Values
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .Select(value =>
                {
                    var levelOneCost = value.CostForLevel(1);
                    var materialRequirements = (levelOneCost?.Materials ??
                                                Array.Empty<GuildMaterialDefinition017D>())
                        .Where(requirement => requirement != null &&
                                              !string.IsNullOrWhiteSpace(requirement.MaterialId))
                        .Select(requirement => new GuildCity017D.GuildCityMaterialRequirementView017D
                        {
                            MaterialId = requirement.MaterialId,
                            DisplayName = Humanize(requirement.MaterialId),
                            RequiredAmount = requirement.Amount,
                            AvailableAmount = city.Materials
                                .Where(material => material != null && StringComparer.Ordinal.Equals(
                                    material.MaterialId,
                                    requirement.MaterialId))
                                .Select(material => material.Amount)
                                .FirstOrDefault()
                        })
                        .ToArray();
                    return new GuildCity017D.GuildCityBuildingView017D
                    {
                        BuildingId = value.Id,
                        DisplayName = value.DisplayName,
                        DistrictId = value.DistrictId,
                        EffectIdentity = value.EffectIdentity,
                        MaxLevel = value.MaxLevel,
                        HasLevelOneCost = levelOneCost != null,
                        LevelOneHallXpCost = levelOneCost?.HallXp ?? 0,
                        CanAffordLevelOneWithResources = levelOneCost != null &&
                                                         view.HallEnhancementXp >= levelOneCost.HallXp &&
                                                         materialRequirements.All(requirement =>
                                                             requirement.AvailableAmount >=
                                                             requirement.RequiredAmount),
                        LevelOneMaterialRequirements = materialRequirements,
                        AdjacencyBuildingIds = value.AdjacencyBuildingIds ?? Array.Empty<string>(),
                        StaffRoles = value.StaffRoles ?? Array.Empty<string>()
                    };
                }).ToArray();
            view.Assignments = city.MemberAssignments.Select(value =>
            {
                var recruit = FindRecruit(value.RecruitId);
                return new GuildCity017D.GuildCityAssignmentView017D
                {
                    RecruitId = value.RecruitId,
                    RecruitName = recruit?.DisplayName ?? Humanize(value.RecruitId),
                    Kind = value.Kind.ToString(),
                    FacilityId = value.FacilityId,
                    RecoveryProgress = value.RecoveryProgress,
                    TrainingProgress = value.TrainingProgress,
                    DutyProgress = value.DutyProgress
                };
            }).ToArray();
            view.Relationships = city.RelationshipMemories.Select(value =>
                new GuildCity017D.GuildCityRelationshipView017D
                {
                    MemoryId = value.MemoryId,
                    FirstRecruitId = value.FirstRecruitId,
                    SecondRecruitId = value.SecondRecruitId,
                    Summary = value.Summary,
                    SceneId = value.SceneId,
                    Viewed = value.Viewed,
                    Strength = value.Strength
                }).ToArray();
            view.RelationshipCount = view.Relationships.Count;
            view.UnviewedRelationshipCount = view.Relationships.Count(value => !value.Viewed);
            view.Contracts = _guildCityContent.Contracts.Values
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .Select(value => new GuildCity017D.GuildCityContractView017D
                {
                    ContractId = value.Id,
                    BoardId = value.BoardId,
                    DisplayName = value.DisplayName,
                    Sponsor = value.Sponsor,
                    Family = value.Family,
                    Hook = value.Hook,
                    PrimaryObjective = value.PrimaryObjective,
                    OptionalObjectives = value.OptionalObjectives ?? Array.Empty<string>(),
                    Hazards = value.Hazards ?? Array.Empty<string>(),
                    RecommendedSkills = value.RecommendedSkills ?? Array.Empty<string>(),
                    GuildXp = value.BaseGuildXp,
                    HallXp = value.BaseHallXp,
                    CityHook = value.CityHook,
                    IsActive = city.ActiveContract != null &&
                        StringComparer.Ordinal.Equals(city.ActiveContract.ContractId, value.Id),
                    // Completed operations include failures and repeats. Recover
                    // chapter identity only from its exact durable contract receipt.
                    IsCompleted = (city.ActiveContract != null &&
                        StringComparer.Ordinal.Equals(city.ActiveContract.ContractId, value.Id) &&
                        city.ActiveContract.Completed) ||
                        GuildCityRecruitmentService017D.HasVerifiedOpeningStoryReward094(_campaign, value.Id),
                    IsFailed = city.ActiveContract != null &&
                        StringComparer.Ordinal.Equals(city.ActiveContract.ContractId, value.Id) &&
                        city.ActiveContract.Failed
                }).ToArray();
            view.HasActiveContract = city.ActiveContract != null &&
                !city.ActiveContract.Completed && !city.ActiveContract.Failed;
            view.HasPendingEncounter = city.PendingEncounter != null;
            view.HasPendingBattleReturn = city.PendingBattleReturn != null;
            view.IsCertifiedEncounterBattle = city.PendingEncounter != null && _campaign.Battle != null &&
                StringComparer.Ordinal.Equals(_campaign.Battle.BattleId, city.PendingEncounter.BattleId);
            view.LastCheckpointId = city.LastCheckpointId;
            var activeWorldGateOperation023 = city.Strategic017H?.Campaign019?
                .Playable020?.WorldGate023?.ActiveOperation;
            var hasCanonicalWorldGateMirror023 = global::SecondDimension.Gameplay
                .Campaign023.CampaignWorldGateCommandService023
                .HasCanonicalLegacyHandoff084(SecondDimension.Gameplay.Campaign020.CampaignRecoveryCommands150.WorldIdentityCity(_campaign), activeWorldGateOperation023) ||
                _expeditionDeckCommands089.HasCanonicalOptionalBattleHandoff104(_campaign) ||
                HasCanonicalTitanWorldGateMirror164(_campaign, _titanCatalog161);
            // Campaign 023 deliberately mirrors its authoritative World Gate run
            // into the legacy GuildCity contract/expedition fields for the shared
            // battle bridge.  That mirror's BOARD023_* ID belongs to the World
            // Gate catalog, so it must not be projected through GuildCityContent.
            // Keep every non-matching legacy expedition on the strict lookup path
            // so stale or forged GuildCity board IDs remain visible diagnostics.
            if (city.Expedition != null && !hasCanonicalWorldGateMirror023)
            {
                var expedition = city.Expedition;
                var board = _guildCityContent.Board(expedition.BoardId);
                var node = board.Node(expedition.CurrentNodeId);
                EventDefinition017D currentEvent = null;
                if (!string.IsNullOrWhiteSpace(node.EventId))
                    _guildCityContent.Events.TryGetValue(node.EventId, out currentEvent);
                var currentCheck = expedition.CommittedChecks.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.NodeId, expedition.CurrentNodeId));
                view.Expedition = new GuildCity017D.GuildCityExpeditionView017D
                {
                    ExpeditionId = expedition.ExpeditionId,
                    BoardId = expedition.BoardId,
                    CurrentNodeId = expedition.CurrentNodeId,
                    CurrentNodeKind = node.Kind,
                    CurrentEventId = node.EventId,
                    CurrentEventTitle = currentEvent?.Title ?? string.Empty,
                    CurrentEventProblem = currentEvent?.Problem ?? string.Empty,
                    CurrentEventEligibleSkills = currentEvent?.EligibleSkills ?? Array.Empty<string>(),
                    CurrentEventConsequence = currentEvent?.ConsequenceIdentity ?? string.Empty,
                    CurrentEventOutcomeText = global::SecondDimension.Presentation.GuildCity017D
                        .ChapterTwoCrewMechanics079.SelectOutcomeCopy079(
                        currentCheck?.Outcome,
                        currentEvent?.ExceptionalText,
                        currentEvent?.FullSuccessText,
                        currentEvent?.SuccessWithCostText,
                        currentEvent?.SetbackText,
                        currentEvent?.SevereSetbackText),
                    CurrentEventUsesCommitted2d6 =
                        GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                            expedition.BoardId,
                            currentEvent),
                    CurrentCheckSkillId = node.CheckSkillId ?? string.Empty,
                    CurrentEncounterId = node.EncounterId,
                    LinkedNodeIds = node.Links ?? Array.Empty<string>(),
                    VisitedNodeIds = expedition.VisitedNodeIds,
                    RevealedNodeIds = expedition.RevealedNodeIds,
                    Status = expedition.Status.ToString(),
                    Supplies = expedition.Supplies,
                    Fatigue = expedition.Fatigue,
                    Threat = expedition.Threat,
                    Urgency = expedition.Urgency,
                    ObjectiveFlags = expedition.ObjectiveFlags,
                    RequiresResolution = GuildCityExpeditionService017D.CurrentNodeRequiresResolution(node),
                    ResolutionComplete = GuildCityExpeditionService017D.IsCurrentNodeResolved(expedition, node),
                    ResolutionHint = GuildCityExpeditionService017D.IsFirstHourGateEater071(expedition, node) &&
                                     !GuildCityExpeditionService017D.HasFirstHourLanternPatrolRescued071(expedition)
                        ? "Free Zorin's Lantern Patrol and secure the Wayglass before facing the Gate-Eater."
                        : currentEvent != null &&
                          !GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                            expedition.BoardId,
                            currentEvent)
                            ? "Choose who leads and whether a partner supports them. Your crew's strengths and the trust you have earned decide the consequence."
                            : GuildCityExpeditionService017D.ResolutionHint(node),
                    CanMove = expedition.Status == ExpeditionStatus017D.Active &&
                        GuildCityExpeditionService017D.IsCurrentNodeResolved(expedition, node),
                    CanCommitEncounter = GuildCityExpeditionService017D.CanCommitEncounter(
                        expedition, node),
                    CanFinalizeOperation = expedition.Status == ExpeditionStatus017D.Completed ||
                        expedition.Status == ExpeditionStatus017D.Extracted ||
                        expedition.Status == ExpeditionStatus017D.Failed,
                    HasCommittedCheckAtCurrentNode = currentCheck != null,
                    LastCheckActorRecruitId = currentCheck?.ActorRecruitId ?? string.Empty,
                    LastCheckAssistantRecruitId = currentCheck?.AssistantRecruitId ?? string.Empty,
                    LastCheckDieOne = currentCheck?.DieOne ?? 0,
                    LastCheckDieTwo = currentCheck?.DieTwo ?? 0,
                    LastCheckModifier = currentCheck?.Modifier ?? 0,
                    LastCheckTotal = currentCheck?.Total ?? 0,
                    LastCheckOutcome = currentCheck?.Outcome ?? string.Empty
                };
                view.ExpeditionVisitedNodeCount = expedition.VisitedNodeIds.Count;
                view.CommittedCheckCount = expedition.CommittedChecks.Count;

                var questCards = _guildCityExpeditions.BuildQuestCardRow090(
                    _campaign,
                    _guildCityContent,
                    _guildCityRecruitment);
                view.QuestCards090 = questCards.Select(value =>
                    new GuildCity017D.GuildQuestCardView090
                    {
                        CardId = value.CardId,
                        Category = value.Category,
                        Title = value.Title,
                        Description = value.Description,
                        RewardPreview = value.RewardPreview,
                        RiskLabel = value.RiskLabel,
                        DestinationNodeId = value.DestinationNodeId,
                        DestinationLabel = value.DestinationLabel,
                        RoutePreview = "NEXT: " +
                            (string.IsNullOrWhiteSpace(value.DestinationLabel)
                                ? "A NEW ROOM"
                                : value.DestinationLabel.ToUpperInvariant()),
                        RarityId = value.RarityId,
                        ItemName = value.ItemName,
                        ItemVisualId = QuestCardItemVisualId092(value),
                        PhysicalPower = value.PhysicalPower,
                        MysticPower = value.MysticPower,
                        TreasuryXpDelta = value.TreasuryXpDelta,
                        TreasuryXpCost = value.TreasuryXpCost,
                        DieOne = value.DieOne,
                        DieTwo = value.DieTwo,
                        Target = value.Target,
                        FateCheckModifier = value.FateCheckModifier,
                        EncounterId = value.EncounterId,
                        EnemyUnionCount = value.EnemyUnionCount,
                        IsPermanentHeroBoon = value.IsPermanentHeroBoon,
                        HeroName = value.HeroName,
                        CanChoose = value.CanChoose,
                        LockedReason = value.LockedReason,
                        VisualResourcePath = global::SecondDimension.Presentation
                            .GuildCity017D.GuildQuestCardPresentation090
                            .VisualResourcePath090(value.Category)
                    }).ToArray();
                view.QuestCardRound090 = questCards.Count == 0
                    ? 0
                    : GuildCityExpeditionService017D.QuestCardRoundCount090(
                          expedition.ObjectiveFlags) + 1;
                view.QuestCardMinimumRounds090 =
                    StringComparer.Ordinal.Equals(
                        expedition.BoardId,
                        GuildCityExpeditionService017D.LegacyFirstRescueBoardId069) ||
                    StringComparer.Ordinal.Equals(
                        expedition.BoardId,
                        GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069)
                        ? 0
                        : GuildCityExpeditionService017D.MinimumQuestCardRounds090;
            }
            return view;
        }

        private GuildCity017H.GuildCityStrategicPresentationState017H BuildGuildCityStrategicPresentationState017H()
        {
            var view = new GuildCity017H.GuildCityStrategicPresentationState017H
            {
                IsAvailable = _campaign?.Guild?.GuildCity != null && _guildCityContent != null && _guildCityStrategicContent017H != null,
                Error = _startupNotice
            };
            if (!view.IsAvailable) return view;
            var city = _campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var contributionService = new GuildCityBuildingContributionService017H();
            var aggregate = contributionService.Calculate(city, _guildCityStrategicContent017H);
            view.DefenseMasteryXp = strategic.DefenseMasteryXp;
            view.DefensesWon = strategic.TotalDefensesWon;
            view.DefensesLost = strategic.TotalDefensesLost;
            view.TotalBuildingCount = _guildCityStrategicContent017H.Contributions.Count;
            view.ActiveContributionBuildingCount = aggregate.SourceBuildingIds.Count;
            view.AggregateCombatSummary = "Defense " + aggregate.DefensePowerFlat + " • Barrier " + aggregate.BarrierIntegrityFlat + " • AP +" + aggregate.StartingApFlat + " • Cohesion +" + aggregate.StartingCohesionFlat + " • MP +" + aggregate.StartingMpFlat + " • Enemy Cohesion -" + aggregate.EnemyCohesionDamageFlat;
            view.AggregateXpSummary = "Personal XP +" + aggregate.PersonalXpBp / 100f + "% • Combat mastery +" + aggregate.CombatArtMasteryBp / 100f + "% • Mystic +" + aggregate.MysticMasteryBp / 100f + "% • Restoration +" + aggregate.RestorationMasteryBp / 100f + "% • Warding +" + aggregate.WardingMasteryBp / 100f + "% • Guild +" + aggregate.GuildTreasuryXpBp / 100f + "% • Hall +" + aggregate.CivicHallXpBp / 100f + "%";
            view.Buildings = city.CityPlots.Where(plot => !string.IsNullOrWhiteSpace(plot.BuildingId)).Select(plot =>
            {
                _guildCityContent.Buildings.TryGetValue(plot.BuildingId, out var building);
                _guildCityStrategicContent017H.Contributions.TryGetValue(plot.BuildingId, out var contribution);
                var v = contribution?.PerLevel;
                return new GuildCity017H.BuildingContributionView017H
                {
                    BuildingId = plot.BuildingId,
                    DisplayName = building?.DisplayName ?? Humanize(plot.BuildingId),
                    Level = plot.BuildingLevel,
                    Staffed = plot.StaffRecruitIds != null && plot.StaffRecruitIds.Count > 0,
                    Summary = contribution?.Summary ?? "Contribution authority missing.",
                    CombatContribution = v == null ? "None" : "AP " + v.StartingApFlat + " • Cohesion " + v.StartingCohesionFlat + " • MP " + v.StartingMpFlat + " • Defense " + v.DefensePowerFlat,
                    XpContribution = v == null ? "None" : "Personal " + v.PersonalXpBp / 100f + "% • Art " + (v.CombatArtMasteryBp + v.MysticMasteryBp + v.RestorationMasteryBp + v.WardingMasteryBp) / 100f + "% • Guild/Hall " + (v.GuildTreasuryXpBp + v.CivicHallXpBp) / 100f + "%"
                };
            }).ToArray();
            view.DefenseProfiles = _guildCityStrategicContent017H.Profiles.Values.OrderBy(value => value.Id, StringComparer.Ordinal).Select(value => new GuildCity017H.DefenseProfileView017H
            {
                ProfileId = value.Id, DisplayName = value.DisplayName, Classification = value.Classification, Summary = value.Summary,
                RequiredStoryGate = value.RequiredStoryGate, FutureLocked = value.FutureLocked,
                Available = StoryGateSatisfied017H(strategic.StoryGates, value.RequiredStoryGate),
                WaveCount = value.Waves?.Length ?? 0,
                LaneNames = (value.LaneIds ?? Array.Empty<string>()).Select(id => _guildCityStrategicContent017H.Lanes.TryGetValue(id, out var lane) ? lane.DisplayName : Humanize(id)).ToArray()
            }).ToArray();
            if (strategic.ActiveDefense != null)
            {
                var active = strategic.ActiveDefense; var profile = _guildCityStrategicContent017H.DefenseProfile(active.ProfileId);
                var wave = active.CurrentWaveIndex < (profile.Waves?.Length ?? 0) ? profile.WaveAt(active.CurrentWaveIndex) : null;
                view.ActiveDefense = new GuildCity017H.ActiveDefenseView017H
                {
                    OperationId = active.OperationId, ProfileId = active.ProfileId, DisplayName = profile.DisplayName, Status = active.Status.ToString(),
                    CurrentWave = active.CurrentWaveIndex, TotalWaves = profile.Waves?.Length ?? 0, CurrentWaveName = wave?.DisplayName ?? "All waves resolved",
                    CurrentWaveDecisiveBattle = wave?.DecisiveBattle ?? false, EnemyUnionCount = wave?.EnemyUnionCount ?? 0,
                    CityIntegrity = active.CityIntegrity, BarrierIntegrity = active.BarrierIntegrity,
                    Lanes = profile.LaneIds.Select(laneId =>
                    {
                        var assignment = active.LaneAssignments.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.LaneId, laneId));
                        var lane = _guildCityStrategicContent017H.Lanes[laneId];
                        return new GuildCity017H.DefenseLaneView017H { LaneId = laneId, DisplayName = lane.DisplayName, Summary = lane.Summary, AssignedUnionIds = assignment?.UnionIds ?? Array.Empty<string>() };
                    }).ToArray()
                };
            }
            view.CanonEvents = _guildCityStrategicContent017H.CanonEvents.Values.OrderBy(value => value.Id, StringComparer.Ordinal).Select(value =>
            {
                var state = strategic.CanonEvents.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.EventId, value.Id));
                var available = StoryGateSatisfied017H(strategic.StoryGates, value.RequiredStoryGate) && !StringComparer.Ordinal.Equals(value.Classification, "WHAT_IF_SANDBOX");
                return new GuildCity017H.CanonEventView017H { EventId = value.Id, DisplayName = value.DisplayName, Classification = value.Classification, Summary = value.Summary, Status = state?.Status.ToString() ?? (available ? "Available" : "Locked"), RequiredStoryGate = value.RequiredStoryGate, DefenseProfileId = value.DefenseProfileId, OutcomeLocked = value.OutcomeLocked, Available = available };
            }).ToArray();
            view.StoryGates = strategic.StoryGates;
            return view;
        }

        private M1PresentationState BuildPresentationState()
        {
            using var timing110 = BeginTowerTiming110("read-full-presentation");
            var state = new M1PresentationState
            {
                HasCampaign = _campaign != null,
                HasSave = File.Exists(_savePath),
                Modes = ModeViews,
                StatusMessage = _startupNotice,
                SaveRecoveryDiagnostic = _saveRecoveryDiagnostic,
                ResumeScreen = ResumeScreen(_campaign)
            };
            if (_campaign == null) return state;

            state.GuildmasterName = _campaign.Profile?.GuildmasterName ?? string.Empty;
            state.SelectedModeId = _campaign.Profile?.Mode.ToString() ?? "Standard";
            var development = _campaign.Guild.Development ?? GuildDevelopmentState.Default();
            state.GuildLevel = development.GuildLevel;
            state.LifetimeGuildXp = development.LifetimeTreasuryXpEarned;
            state.GuildXpIntoCurrentLevel = development.GuildXpIntoCurrentLevel;
            state.GuildXpRequiredForNextLevel = development.GuildXpRequiredForNextLevel;
            state.TreasuryXp = _campaign.Guild.TreasuryXp;
            state.HallStageIndex = development.HallStageIndex;
            state.HallStageId = development.HallStageId;
            state.HallStageName = Humanize(development.HallStageId);
            state.HallEnhancementXp = development.HallEnhancementXp;
            state.Facilities = development.Facilities.Select(value => new M1FacilityProgressionView
            {
                FacilityId = value.FacilityId,
                DisplayName = Humanize(value.FacilityId),
                Level = value.Level,
                TotalFacilityXp = value.TotalFacilityXp
            }).ToArray();
            state.Applicants = BuildApplicants();
            state.Recruits = BuildLoadouts();
            state.Unions = BuildUnions();
            state.Formations = OpeningUnionCatalog.Formations
                .Select(value => new M1ChoiceView
                {
                    Id = value.Id,
                    DisplayName = value.DisplayName,
                    Summary = value.Summary
                })
                .ToArray();
            state.Doctrines = OpeningUnionCatalog.Doctrines
                .Select(value => new M1ChoiceView
                {
                    Id = value.Id,
                    DisplayName = value.DisplayName,
                    Summary = value.Summary
                })
                .ToArray();
            state.AllSixSigned = _campaign.Guild.Recruits.Count >= OpeningFlowState.RequiredOpeningRecruitCount;
            state.OpeningEquipmentLegal = OpeningEquipmentLegal(_campaign);
            state.OpeningUnionsLegal = _campaign.OpeningFlow?.Stage == OpeningStage.Complete
                ? _commands.ValidateGuildUnionPlans(UnionBattlePlanRules132.ProjectGuild(_campaign)).IsSuccess
                : _commands.ValidateOpeningUnions(_campaign.Guild).IsSuccess;
            state.TwoUnionsLegal = state.OpeningUnionsLegal;
            state.UsedUnionCount = UnionBattlePlanRules132.Read(_campaign).Count(value => value.MemberRecruitIds.Count > 0);
            state.MaximumUnionPlanCount = NormalUnionPlanRules.MaximumPlanCount;
            state.SaveReloadVerified = _saveReloadVerified;
            state.CanonicalStateHash = PresentationHash110();
            state.Battle = BuildBattleView();
            return state;
        }

        private M2BattleView BuildBattleView()
        {
            var battle = _campaign?.Battle;
            if (battle == null) return null;
            var selections = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < battle.Selections.Count; i++)
                selections[battle.Selections[i].UnionId] = battle.Selections[i].ForecastId;
            var players = battle.PlayerUnions.Select(union => ToBattleUnionView(union, selections)).ToArray();
            var enemies = battle.EnemyUnions.Select(union => ToBattleUnionView(union, selections)).ToArray();
            var forecasts = battle.CommittedForecasts.Select(forecast => new M2ForecastView
            {
                ForecastId = forecast.ForecastId,
                UnionId = forecast.UnionId,
                CommandId = forecast.CommandId,
                CommandName = forecast.CommandName,
                Phrase = forecast.Phrase,
                TacticalIntent = Humanize(forecast.TacticalIntent),
                TargetId = forecast.TargetId,
                TargetName = forecast.TargetName,
                SharedApCost = forecast.SharedApCost,
                ApRecovery = forecast.ApRecovery,
                CombinedMpCost = forecast.CombinedMpCost,
                ExpectedEffect = forecast.ExpectedEffect,
                Risk = forecast.Risk,
                LearningOpportunity = forecast.LearningOpportunity,
                FallbackBehavior = forecast.FallbackBehavior,
                IsSelected = selections.TryGetValue(forecast.UnionId, out var selected) &&
                             StringComparer.Ordinal.Equals(selected, forecast.ForecastId),
                MemberActions = forecast.MemberActions
                    .Select(action => ToPredictedActionView088(battle, action))
                    .ToArray()
            }).ToArray();
            var active = battle.PlayerUnions.Count(union => !union.Retreated && !union.IsDefeated);
            // Old terminal event prose is part of the replay proof. Correct the
            // read model only: an objective timeout is not a full-party wipe.
            var defeatWithSurvivors134 = battle.Outcome == BattleOutcome.Defeat &&
                battle.PlayerUnions.Any(union => union.Members.Any(member =>
                    !member.Downed && member.CurrentHp > 0));
            var events = battle.EventLog.Select(item => ToBattleEventView(item, defeatWithSurvivors134)).ToArray();
            var recent = events.Skip(Math.Max(0, events.Length - 12)).ToArray();
            var lastRecord = battle.RoundRecords.LastOrDefault();
            var lastRoundEvents = lastRecord == null
                ? Array.Empty<M2BattleEventView>()
                : lastRecord.Events.Select(item => ToBattleEventView(item, defeatWithSurvivors134)).ToArray();
            var breakthrough = events.LastOrDefault(value =>
                value.EventType.IndexOf("Breakthrough", StringComparison.OrdinalIgnoreCase) >= 0);
            return new M2BattleView
            {
                BattleId = battle.BattleId,
                Round = battle.Round,
                Outcome = battle.Outcome == BattleOutcome.InProgress ? "In Progress" : Humanize(battle.Outcome.ToString()),
                Objective = battle.Objective,
                PlayerUnions = players,
                EnemyUnions = enemies,
                Forecasts = forecasts,
                Events = events,
                RecentEvents = recent,
                LastResolvedRound = lastRecord?.Round ?? 0,
                LastResolvedRoundEvents = lastRoundEvents,
                CanConfirmRound = battle.Outcome == BattleOutcome.InProgress && battle.Selections.Count == active,
                IsResolved = battle.Outcome != BattleOutcome.InProgress,
                TutorialBreakthroughOccurred = battle.TutorialBreakthroughOccurred,
                TutorialBreakthroughSummary = battle.TutorialBreakthroughOccurred
                    ? breakthrough?.Text ?? "Guaranteed tutorial breakthrough achieved through meaningful combat use."
                    : "A gold learning marker identifies the guaranteed eligible tutorial breakthrough.",
                Reward = BuildBattleRewardView(battle.Reward),
                StateHash = M2BattleCommandService.AuthoritativeStateHash(battle),
                FinalStateHash = battle.FinalStateHash
            };
        }

        private M2BattleRewardView BuildBattleRewardView(BattleRewardState reward)
        {
            if (reward == null) return null;
            var development = _campaign?.Guild?.Development ?? GuildDevelopmentState.Default();
            var guildXpBefore = reward.Claimed
                ? Math.Max(0L, development.LifetimeTreasuryXpEarned - reward.GuildTreasuryXpAward)
                : development.LifetimeTreasuryXpEarned;
            var guildXpAfter = checked(guildXpBefore + reward.GuildTreasuryXpAward);
            var previousGuildLevel = GuildProgressionRules021.LevelForLifetimeXp(guildXpBefore);
            var projectedGuildLevel = GuildProgressionRules021.LevelForLifetimeXp(guildXpAfter);
            return new M2BattleRewardView
            {
                TitanRewardSummary161 = TitanRewardSummary161(),
                TitanRewardHeroId161 = TitanRewardHeroId161(),
                RewardId = reward.RewardId,
                RewardRulesVersion = reward.RewardRulesVersion,
                Outcome = Humanize(reward.Outcome.ToString()),
                Claimed = reward.Claimed,
                CanClaim = !reward.Claimed,
                BasePersonalXpPerMember = reward.BasePersonalXpPerMember,
                BaseGuildTreasuryXp = reward.BaseGuildTreasuryXp,
                EnemyUnionMultiplierPermille = reward.EnemyUnionMultiplierPermille,
                OutcomeMultiplierPermille = reward.OutcomeMultiplierPermille,
                PersonalXpModePercent = reward.PersonalXpModePercent,
                TreasuryXpModePercent = reward.TreasuryXpModePercent,
                GuildTreasuryXpAward = reward.GuildTreasuryXpAward,
                HallEnhancementXpAward = reward.HallEnhancementXpAward,
                GuildXpBefore = guildXpBefore,
                GuildXpAfter = guildXpAfter,
                GuildPreviousLevel = previousGuildLevel,
                GuildProjectedLevel = projectedGuildLevel,
                GuildLevelsGained = projectedGuildLevel - previousGuildLevel,
                EquipmentRewardInstanceId = reward.EquipmentReward?.InstanceId ?? string.Empty,
                EquipmentRewardDefinitionId = reward.EquipmentReward?.DefinitionId ?? string.Empty,
                EquipmentRewardDisplayName = reward.EquipmentReward?.DisplayName ?? string.Empty,
                EquipmentRewardQualityId = reward.EquipmentReward?.QualityId ?? string.Empty,
                EquipmentRewardValidSlotIds = reward.EquipmentReward?.ValidSlotIds?.ToArray() ??
                                              Array.Empty<string>(),
                MemberRewards = reward.MemberRewards.Select(value => BuildMemberRewardView164(reward, value)).ToArray()
            };
        }

        private M2BattleUnionView ToBattleUnionView(
            BattleUnionState union,
            IReadOnlyDictionary<string, string> selections)
        {
            selections.TryGetValue(union.UnionId, out var selected);
            return new M2BattleUnionView
            {
                UnionId = union.UnionId,
                DisplayName = M1UnionIdentity076.ResolveCanonicalOpeningUnion(
                    union.UnionId,
                    union.DisplayName),
                Side = union.Side.ToString(),
                LeaderMemberId = union.LeaderMemberId,
                Formation = union.FormationName,
                FormationBenefitActive = union.FormationBenefitActive,
                FormationStatus = union.FormationBenefitActive
                    ? union.FormationName + " benefit active"
                    : !union.FormationMemberCountEligible
                        ? union.FormationInactiveReason
                        : union.FormationName + " is broken; restore formation condition and Cohesion to reactivate it.",
                CurrentAp = union.CurrentAp,
                MaximumAp = union.MaximumAp,
                Cohesion = union.Cohesion,
                FormationConditionPercent = union.FormationConditionBasisPoints / 100,
                Engagement = Humanize(union.Engagement.ToString()),
                CanAct = !union.Retreated && !union.IsDefeated,
                IsSelected = !string.IsNullOrWhiteSpace(selected),
                SelectedForecastId = selected,
                UnionDisciplinePoints = union.UnionMeaningfulUsePoints,
                Members = union.Members.Select(member =>
                {
                    var recruit = FindRecruit(member.MemberId);
                    return new M2BattleMemberView
                    {
                        MemberId = member.MemberId,
                        DisplayName = member.DisplayName,
                        ClassName = Humanize(member.ClassId),
                        ClassSymbol = ClassSymbol(member.ClassId),
                        RaceId = recruit?.RaceId ?? EnemyVisualFamily070(member),
                        VisualSeed = recruit == null
                            ? member.MemberId
                            : ReadVisualSeed(recruit.CanonicalApplicantJson, recruit.RecruitId),
                        PortraitAuthorityId = recruit == null
                            ? EnemyPortraitAuthority070(member)
                            : PortraitAuthority(
                                recruit.AuthoredStableRecruitId,
                                recruit.SignatureId,
                                recruit.RecruitId),
                        EnemyArtBaseId090 = recruit == null
                            ? member.EnemyArtBaseId090
                            : null,
                        EnemyArtVariantId090 = recruit == null
                            ? member.EnemyArtVariantId090
                            : null,
                        EnemyThreatTier089 = union.Side == BattleSide.Enemy
                            ? M2EnemyThreatPalette089.ResolveTowerTier(
                                _campaign?.Battle?.BattleId)
                            : 0,
                        CurrentHp = member.CurrentHp,
                        MaximumHp = member.MaximumHp,
                        CurrentMp = member.CurrentMp,
                        MaximumMp = member.MaximumMp,
                        Downed = member.Downed,
                        Stabilized = member.Stabilized,
                        MeaningfulUsePoints = member.MeaningfulUsePoints,
                        ArtGrowthSummary = ArtGrowthSummary(member),
                        NextSkillProgress = BuildNextSkillProgress(member, recruit),
                        EquipmentTags = member.EquipmentTags,
                        EquipmentVisualFamily = M2EquipmentVisualPolicy018.Resolve(member.EquipmentTags).WeaponFamilyId,
                        EquipmentAnimatorSet = M2EquipmentVisualPolicy018.Resolve(member.EquipmentTags).AnimatorSetId
                    };
                }).ToArray()
            };
        }

        private static M2BattleEventView ToBattleEventView(BattleEventState item, bool defeatWithSurvivors134) =>
            new M2BattleEventView
            {
                Sequence = item.Sequence,
                Round = item.Round,
                EventType = item.EventType,
                Text = defeatWithSurvivors134 && StringComparer.Ordinal.Equals(item.EventType, "BATTLE_RESULT")
                    ? "The battle objective was not completed. Surviving heroes can regroup for another attempt."
                    : item.Text,
                Side = Humanize(item.Side.ToString()),
                UnionId = item.UnionId,
                MemberId = item.MemberId,
                ArtId = item.ArtId,
                Amount = item.Amount,
                ActorUnionId = item.ActorUnionId,
                ActorMemberId = item.ActorMemberId,
                TargetUnionId = item.TargetUnionId,
                TargetMemberId = item.TargetMemberId
            };

        private static string BattleMemberName(BattleState battle, string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId)) return "Battle objective";
            for (var unionIndex = 0; unionIndex < battle.PlayerUnions.Count; unionIndex++)
                for (var memberIndex = 0; memberIndex < battle.PlayerUnions[unionIndex].Members.Count; memberIndex++)
                    if (StringComparer.Ordinal.Equals(
                            battle.PlayerUnions[unionIndex].Members[memberIndex].MemberId,
                            memberId)) return battle.PlayerUnions[unionIndex].Members[memberIndex].DisplayName;
            for (var unionIndex = 0; unionIndex < battle.EnemyUnions.Count; unionIndex++)
                for (var memberIndex = 0; memberIndex < battle.EnemyUnions[unionIndex].Members.Count; memberIndex++)
                    if (StringComparer.Ordinal.Equals(
                            battle.EnemyUnions[unionIndex].Members[memberIndex].MemberId,
                            memberId)) return battle.EnemyUnions[unionIndex].Members[memberIndex].DisplayName;
            return Humanize(memberId);
        }

        private M2PredictedActionView ToPredictedActionView088(
            BattleState battle,
            BattlePlannedActionState action)
        {
            var actor = BattleMember088(battle, action.ActorMemberId);
            var masteryPoints = 0;
            if (actor?.ArtProgress != null)
                for (var index = 0; index < actor.ArtProgress.Count; index++)
                    if (StringComparer.Ordinal.Equals(
                            actor.ArtProgress[index].ArtId,
                            action.ArtId))
                    {
                        masteryPoints = actor.ArtProgress[index].MasteryPoints;
                        break;
                    }
            var progress = M2ArtMasteryLevelPolicy088
                .ProgressForMasteryPoints(masteryPoints);
            var scalingEnabled = M2BattleCommandService
                .UsesArtMasteryLevelScaling088(battle, _combatContent);
            return new M2PredictedActionView
            {
                ActorMemberId = action.ActorMemberId,
                ActorName = action.ActorName,
                ArtId = action.ArtId,
                ArtName = action.ArtName,
                Discipline = action.Discipline,
                ActionKind = Humanize(action.Kind.ToString()),
                TargetUnionId = action.TargetUnionId,
                TargetMemberId = action.TargetMemberId,
                TargetName = BattleMemberName(battle, action.TargetMemberId),
                AreaTargetCount095 = action.AreaActionPlan095?.Recipients.Count ?? 0,
                AreaUnionIds095 = action.AreaActionPlan095?.Recipients.Select(value => value.UnionId)
                    .Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>(),
                AreaPredictedHpLoss095 = action.AreaActionPlan095?.PredictedHpLoss ?? 0,
                SharedApCost = action.SharedApCost,
                PersonalMpCost = action.PersonalMpCost,
                PredictedGrowth = action.PredictedGrowth,
                PredictedHpDelta097 = action.PredictedHpDelta,
                DamageRecipients097 = action.AreaActionPlan095?.Recipients.Select(value => new M2PredictedDamageRecipient097
                {
                    UnionId = value.UnionId,
                    MemberId = value.MemberId,
                    PredictedHpLoss = value.PredictedHpLoss
                }).ToArray() ?? Array.Empty<M2PredictedDamageRecipient097>(),
                ArtLevel = progress.Level,
                ArtLevelProgressBasisPoints = progress.ProgressBasisPoints,
                ArtPowerCue = scalingEnabled
                    ? ArtPowerCue088(progress.Level)
                    : string.Empty,
                Prediction = action.Prediction,
                IsRevival091 = _combatContent != null &&
                    _combatContent.Arts.TryGetValue(action.ArtId ?? string.Empty, out var projectedArt091) &&
                    M2BattleCommandService.IsRevivalArtForVerification080(projectedArt091),
                BreakthroughOpportunity = action.BreakthroughOpportunity,
                BreakthroughTargetArtName = action.BreakthroughTargetArtName,
                AnimationTag = action.AnimationTag
            };
        }

        private static BattleMemberState BattleMember088(
            BattleState battle,
            string memberId)
        {
            if (battle == null || string.IsNullOrWhiteSpace(memberId)) return null;
            for (var unionIndex = 0; unionIndex < battle.PlayerUnions.Count; unionIndex++)
                for (var memberIndex = 0;
                     memberIndex < battle.PlayerUnions[unionIndex].Members.Count;
                     memberIndex++)
                    if (StringComparer.Ordinal.Equals(
                            battle.PlayerUnions[unionIndex].Members[memberIndex].MemberId,
                            memberId))
                        return battle.PlayerUnions[unionIndex].Members[memberIndex];
            for (var unionIndex = 0; unionIndex < battle.EnemyUnions.Count; unionIndex++)
                for (var memberIndex = 0;
                     memberIndex < battle.EnemyUnions[unionIndex].Members.Count;
                     memberIndex++)
                    if (StringComparer.Ordinal.Equals(
                            battle.EnemyUnions[unionIndex].Members[memberIndex].MemberId,
                            memberId))
                        return battle.EnemyUnions[unionIndex].Members[memberIndex];
            return null;
        }

        private static string ArtPowerCue088(int level)
        {
            if (level >= 10) return "MASTERED";
            if (level >= 7) return "SURGING";
            if (level >= 4) return "REFINED";
            return string.Empty;
        }

        private static string ArtGrowthSummary(BattleMemberState member)
        {
            if (member?.ArtProgress == null || member.ArtProgress.Count == 0) return string.Empty;
            return string.Join(" · ", member.ArtProgress
                .OrderByDescending(value => value.MasteryPoints)
                .ThenBy(value => value.ArtId, StringComparer.Ordinal)
                .Take(2)
                .Select(value => Humanize(value.ArtId) + " M" + value.MasteryPoints));
        }

        private M2BattleSkillProgressView BuildNextSkillProgress(
            BattleMemberState member,
            RecruitState recruit)
        {
            if (member == null || recruit == null || _combatContent == null) return null;

            if (!string.IsNullOrWhiteSpace(member.BreakthroughArtId) &&
                !member.LearnedArtIds.Contains(member.BreakthroughArtId) &&
                _combatContent.Arts.TryGetValue(member.BreakthroughArtId, out var tutorialTarget))
            {
                return new M2BattleSkillProgressView
                {
                    SkillId = tutorialTarget.Id,
                    DisplayName = tutorialTarget.Name,
                    ProgressKind = "DISCOVERY",
                    CurrentPoints = member.DiscoveryProgress,
                    RequiredPoints = _combatContent.GuaranteedBreakthroughThreshold,
                    RequiredLevel = recruit.Progression.Level,
                    LevelGateMet = true
                };
            }

            M2DeepLearningOpportunity070 selected = null;
            foreach (var sourceArtId in member.LearnedArtIds.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!M2DeepArtRuntime070.TryGetNextLearning(
                        recruit,
                        member,
                        sourceArtId,
                        0,
                        _combatContent,
                        out var candidate) ||
                    candidate == null)
                    continue;
                if (selected == null || PreferLearningOpportunity(candidate, selected)) selected = candidate;
            }
            if (selected == null) return null;

            return new M2BattleSkillProgressView
            {
                SkillId = selected.TargetArtId,
                DisplayName = selected.TargetArtName,
                ProgressKind = "DISCOVERY",
                CurrentPoints = selected.CurrentPoints,
                RequiredPoints = selected.RequiredPoints,
                RequiredLevel = selected.RequiredLevel,
                LevelGateMet = selected.LevelGateMet
            };
        }

        private static bool PreferLearningOpportunity(
            M2DeepLearningOpportunity070 candidate,
            M2DeepLearningOpportunity070 current)
        {
            if (candidate.CanLearnNow != current.CanLearnNow) return candidate.CanLearnNow;
            if (candidate.LevelGateMet != current.LevelGateMet) return candidate.LevelGateMet;

            var candidateRequired = Math.Max(1, candidate.RequiredPoints);
            var currentRequired = Math.Max(1, current.RequiredPoints);
            var candidateProgress = Math.Min(candidate.CurrentPoints, candidateRequired);
            var currentProgress = Math.Min(current.CurrentPoints, currentRequired);
            var ratioComparison = ((long)candidateProgress * currentRequired).CompareTo(
                (long)currentProgress * candidateRequired);
            if (ratioComparison != 0) return ratioComparison > 0;

            var candidateRemaining = Math.Max(0, candidateRequired - candidateProgress);
            var currentRemaining = Math.Max(0, currentRequired - currentProgress);
            if (candidateRemaining != currentRemaining) return candidateRemaining < currentRemaining;
            return StringComparer.Ordinal.Compare(candidate.TargetArtId, current.TargetArtId) < 0;
        }

        private IReadOnlyList<M1ApplicantView> BuildApplicants()
        {
            var board = _campaign.OpeningFlow?.ApplicantBoard;
            if (board == null) return Array.Empty<M1ApplicantView>();
            var result = new List<M1ApplicantView>();
            foreach (var applicant in board.Applicants)
            {
                _scouting.TryGetValue(applicant.RecruitId, out var report);
                result.Add(new M1ApplicantView
                {
                    RecruitId = applicant.RecruitId,
                    RaceId = applicant.RaceId,
                    VisualSeed = ReadVisualSeed(applicant.CanonicalApplicantJson, applicant.RecruitId),
                    PortraitAuthorityId = PortraitAuthority(
                        applicant.AuthoredStableRecruitId,
                        applicant.SignatureId,
                        applicant.RecruitId),
                    DisplayName = applicant.DisplayName,
                    RaceAndWorld = Humanize(applicant.RaceId) + " • " + Humanize(applicant.WorldId),
                    ObservedClass = Humanize(applicant.ClassTendencyId),
                    ClassSymbol = ClassSymbol(applicant.ClassTendencyId),
                    GearSummary = EquipmentSummary(applicant.OpeningEquipment),
                    ScoutObservations = report == null
                        ? "Your scout has only a first impression. Some details remain unknown."
                        : "Scouting confidence " + report.ScoutingAccuracy.ToString(CultureInfo.InvariantCulture) + "% • " + Humanize(report.BackgroundId),
                    LeadershipBand = report == null
                        ? (string.IsNullOrWhiteSpace(applicant.LeadershipBand) ? "Not yet understood" : applicant.LeadershipBand)
                        : FormatLeadership(report.LeadershipEstimate),
                    PersonalityClues = report?.VisibleTraitIds == null || report.VisibleTraitIds.Count == 0
                        ? "Not yet understood"
                        : string.Join(", ", report.VisibleTraitIds.Select(Humanize)),
                    SigningCost = applicant.SigningCostTreasuryXp.ToString(CultureInfo.InvariantCulture) + " charter XP",
                    Ambition = string.Empty,
                    IsSigned = FindRecruit(applicant.RecruitId) != null
                });
            }
            return result;
        }

        private IReadOnlyList<M1RecruitLoadoutView> BuildLoadouts()
        {
            if (_campaign?.Guild == null) return Array.Empty<M1RecruitLoadoutView>();
            var result = new List<M1RecruitLoadoutView>();
            // The same spare item can appear for many heroes. Format its display
            // facts once per projection; legality is still checked for each hero.
            // This cache never outlives the read and never shares returned DTOs.
            var choiceFacts110 = new Dictionary<(EquipmentItemState, string), M1EquipmentChoiceView>();
            foreach (var recruit in _campaign.Guild.Recruits)
            {
                var progression = recruit.Progression ?? RecruitProgressionState.Default();
                var equipmentPower087 = M2EquipmentPowerPolicy087.Resolve(recruit.Equipment);
                var isSss090 = SssTenV4Roster090.TryGetRecruit(
                    recruit, out var sssHero090);
                var sssSignatureEffectReady090 = isSss090 &&
                    SssBattleIntegration090.HasSignatureWeaponEffectHandler090(
                        sssHero090.HeroId);
                var slots = new List<M1EquipmentSlotView>();
                foreach (var mapping in OpeningSlots)
                {
                    var assignment = recruit.Equipment.Find(mapping.Value);
                    var equippedItem = assignment?.Item;
                    var equippedPower087 = M2EquipmentPowerPolicy087.Resolve(equippedItem);
                    var equippedQualityId = equippedItem?.QualityId ?? string.Empty;
                    var choices = new List<M1EquipmentChoiceView>();
                    if (assignment != null)
                    {
                        choices.Add(ToEquipmentChoice(recruit, assignment.Item, mapping.Value, true, choiceFacts110));
                    }
                    choices.AddRange(AvailableItemsFor(recruit, mapping.Value, choiceFacts110));
                    slots.Add(new M1EquipmentSlotView
                    {
                        SlotId = mapping.Value,
                        DisplayName = Humanize(mapping.Value),
                        EquippedItemId = assignment?.Item.InstanceId,
                        EquippedItemName = assignment == null ? "Empty" : assignment.Item.DisplayName,
                        VisualGlyph = EquipmentGlyph(mapping.Value, assignment?.Item.EquipmentTags),
                        EquippedVisualId = EquipmentVisualId090(
                            equippedItem,
                            mapping.Value),
                        EquippedQualityId = equippedQualityId,
                        EquippedRarityTierId = M1VisualAssets.EquipmentRarityTierId(equippedQualityId),
                        EquippedRarityDisplayName = M1VisualAssets.EquipmentRarityDisplayName(equippedQualityId),
                        EquippedPhysicalAttackBonus = equippedPower087.PhysicalAttack,
                        EquippedMysticAttackBonus = equippedPower087.MysticAttack,
                        IsLocked = assignment?.Item.PlayerLocked ?? false,
                        IsLegal = assignment != null,
                        Choices = choices
                    });
                }
                result.Add(new M1RecruitLoadoutView
                {
                    RecruitId = recruit.RecruitId,
                    RaceId = recruit.RaceId,
                    VisualSeed = ReadVisualSeed(recruit.CanonicalApplicantJson, recruit.RecruitId),
                    PortraitAuthorityId = PortraitAuthority(
                        recruit.AuthoredStableRecruitId,
                        recruit.SignatureId,
                        recruit.RecruitId),
                    DisplayName = recruit.DisplayName,
                    ObservedClass = Humanize(recruit.ClassTendencyId),
                    ClassSymbol = ClassSymbol(recruit.ClassTendencyId),
                    Level = progression.Level,
                    TotalPersonalXp = progression.TotalPersonalXp,
                    XpIntoCurrentLevel = progression.XpIntoCurrentLevel,
                    XpRequiredForNextLevel = progression.XpRequiredForNextLevel,
                    // Campaign 023 can legitimately persist a post-opening
                    // campaign without the optional opening tutorial flow.
                    // Loadout presentation must remain readable in that save
                    // shape; the missing observation means no manual equip
                    // action has been witnessed, not that the recruit is bad.
                    HasManualEquipAction =
                        _campaign.OpeningFlow?.ManualEquipmentCommitObserved ?? false,
                    IsLegal = recruit.Equipment.Find(EquipmentSlotIds.BodyArmor) != null,
                    MaximumHp = checked(recruit.MaximumHp + progression.MaximumHpBonus),
                    MaximumMp = checked(recruit.MaximumMp + progression.MaximumMpBonus),
                    MaximumHpBonus = progression.MaximumHpBonus,
                    MaximumMpBonus = progression.MaximumMpBonus,
                    StrengthIndex = checked(M1VisualAssets.OpeningBaseStatIndex(recruit.CanonicalApplicantJson, "STR") + progression.StrengthBonus),
                    MagicIndex = checked(M1VisualAssets.OpeningBaseStatIndex(recruit.CanonicalApplicantJson, "MAGIC") + progression.MagicBonus),
                    DefenseIndex = checked(M1VisualAssets.OpeningBaseStatIndex(recruit.CanonicalApplicantJson, "DEF") + progression.DefenseBonus),
                    AgilityIndex = checked(M1VisualAssets.OpeningBaseStatIndex(recruit.CanonicalApplicantJson, "AGI") + progression.AgilityBonus),
                    WillIndex = checked(M1VisualAssets.OpeningBaseStatIndex(recruit.CanonicalApplicantJson, "WILL") + progression.WillBonus),
                    StrengthBonus = progression.StrengthBonus,
                    DefenseBonus = progression.DefenseBonus,
                    AgilityBonus = progression.AgilityBonus,
                    MagicBonus = progression.MagicBonus,
                    WillBonus = progression.WillBonus,
                    PhysicalAttack = checked(18 + recruit.PotentialBasisPoints / 500 +
                        recruit.TacticalAptitude / 8 + progression.StrengthBonus +
                        progression.AgilityBonus / 2 + equipmentPower087.PhysicalAttack),
                    MysticAttack = checked(12 + recruit.MaximumMp / 4 +
                        recruit.PotentialBasisPoints / 750 + progression.MagicBonus +
                        progression.WillBonus / 2 + equipmentPower087.MysticAttack),
                    EquipmentCombatFamily = M2EquipmentVisualPolicy018.Resolve(CollectEquipmentTags(recruit)).PlayerFacingSummary,
                    ArtsAccessSummary = M2EquipmentVisualPolicy018.ArtsAccessSummary(CollectEquipmentTags(recruit)),
                    LearnedArtIds = progression.LearnedArtIds,
                    ArtMastery = progression.ArtMastery
                        .OrderByDescending(value => value.MasteryPoints)
                        .ThenByDescending(value => value.MeaningfulUses)
                        .ThenBy(value => value.ArtId, StringComparer.Ordinal)
                        .Select(value => new M1ArtMasteryView
                        {
                            ArtId = value.ArtId,
                            DisplayName = ArtDisplayName(value.ArtId),
                            Discipline = Humanize(value.Discipline),
                            MeaningfulUses = value.MeaningfulUses,
                            MasteryPoints = value.MasteryPoints
                        }).ToArray(),
                    Slots = slots,
                    IsSssHero = isSss090,
                    SssHeroId = isSss090 ? sssHero090.HeroId : string.Empty,
                    SssRole = isSss090 ? sssHero090.Role : string.Empty,
                    AscensionLevel = progression.AscensionLevel,
                    AscensionCredits = isSss090
                        ? SssTenV4Inventory090.AscensionCreditCount(_campaign, sssHero090.HeroId)
                        : 0,
                    SssHuntFamiliesCompleted = isSss090
                        ? SecondDimension.SSS.V3.SssWeaponHunt.CompletedFamilies(
                            SssTenV4CampaignAccessor090.Read(_campaign).WeaponHunt)
                        : 0,
                    SssHuntRemainingDefeats = isSss090
                        ? SecondDimension.SSS.V3.SssWeaponHunt.RemainingDefeats(
                            SssTenV4CampaignAccessor090.Read(_campaign).WeaponHunt)
                        : string.Empty,
                    SssSignatureWeaponName = isSss090 ? sssHero090.WeaponName : string.Empty,
                    SssSignatureWeaponArtResourcePath = isSss090
                        ? sssHero090.WeaponArtResourcePath
                        : string.Empty,
                    SssSignatureEffectReady = sssSignatureEffectReady090,
                    SssSignatureReadiness = isSss090
                        ? SecondDimension.Gameplay.TitanHeroes161.TitanHeroCatalog161.Slot(sssHero090.HeroId) > 0
                            ? TitanHeroProgressionSummary161(_campaign, sssHero090.HeroId)
                            : sssHero090.SignatureEffectName +
                          (sssSignatureEffectReady090
                              ? " • Omega stat budget active • equipped-only battle handler active"
                              : " • Omega stat budget active • signature effect adapter pending")
                        : string.Empty,
                    SssIdleArtResourcePath = isSss090
                        ? sssHero090.IdleArtResourcePath
                        : string.Empty,
                    SssAttackArtResourcePath = isSss090
                        ? sssHero090.AttackArtResourcePath
                        : string.Empty,
                    SssPortraitArtResourcePath = isSss090
                        ? sssHero090.PortraitArtResourcePath
                        : string.Empty
                });
            }
            return result;
        }

        private IReadOnlyList<M1EquipmentChoiceView> AvailableItemsFor(
            RecruitState recruit,
            string slotId,
            Dictionary<(EquipmentItemState, string), M1EquipmentChoiceView> choiceFacts110)
        {
            var items = new List<EquipmentItemState>();
            foreach (var item in _campaign.Guild.Inventory)
            {
                if (SssTenV4Inventory090.CanEquip(recruit, item, slotId, out _)) items.Add(item);
            }
            items.Sort((left, right) => StringComparer.Ordinal.Compare(left.InstanceId, right.InstanceId));
            return items.Select(item => ToEquipmentChoice(recruit, item, slotId, false, choiceFacts110)).ToArray();
        }

        private static M1EquipmentChoiceView ToEquipmentChoice(
            RecruitState recruit,
            EquipmentItemState item,
            string slotId,
            bool isEquipped,
            Dictionary<(EquipmentItemState, string), M1EquipmentChoiceView> choiceFacts110)
        {
            if (choiceFacts110.TryGetValue((item, slotId), out var existing110))
            {
                var copy110 = existing110.Copy110();
                var compatible110 = SssTenV4Inventory090.CanEquip(recruit, item, slotId, out var reason110);
                copy110.IsEquipped = isEquipped;
                copy110.IsLegal = compatible110;
                copy110.LegalityReason = compatible110
                    ? isEquipped ? "Currently equipped" : "Compatible slot"
                    : reason110;
                return copy110;
            }
            var equipmentPower087 = M2EquipmentPowerPolicy087.Resolve(item);
            var legal = SssTenV4Inventory090.CanEquip(recruit, item, slotId, out var reason);
            var choice110 = new M1EquipmentChoiceView
            {
                ItemId = item.InstanceId,
                DisplayName = item.DisplayName,
                VisualGlyph = EquipmentGlyph(slotId, item.EquipmentTags),
                EquipmentVisualId = EquipmentVisualId090(item, slotId),
                QualityId = item.QualityId,
                RarityTierId = M1VisualAssets.EquipmentRarityTierId(item.QualityId),
                RarityDisplayName = M1VisualAssets.EquipmentRarityDisplayName(item.QualityId),
                DirectChange = Humanize(slotId) + " • " + string.Join(", ", item.EquipmentTags.Select(Humanize)),
                ForecastBehavior = ForecastBehavior(item.EquipmentTags),
                PhysicalAttackBonus = equipmentPower087.PhysicalAttack,
                MysticAttackBonus = equipmentPower087.MysticAttack,
                IsEquipped = isEquipped,
                IsLegal = legal,
                LegalityReason = isEquipped
                    ? legal ? "Currently equipped" : reason
                    : legal ? "Compatible slot" : reason
            };
            choiceFacts110.Add((item, slotId), choice110.Copy110());
            return choice110;
        }

        private static string EquipmentVisualId090(
            EquipmentItemState item,
            string slotId)
        {
            if (item != null && SssTenV4Roster090.TryGetByWeaponItemId(
                    item.DefinitionId, out var hero))
                return "RESOURCE:" + hero.WeaponArtResourcePath;
            return M1VisualAssets.EquipmentVisualId(
                item?.DefinitionId,
                slotId,
                item?.EquipmentTags);
        }

        private IReadOnlyList<M1UnionView> BuildUnions()
        {
            var result = new List<M1UnionView>();
            var plannedUnions132 = UnionBattlePlanRules132.Read(_campaign);
            var unionCount = Math.Max(OpeningFlowState.MinimumOpeningUnionCount, plannedUnions132.Count);
            for (var index = 0; index < unionCount; index++)
            {
                var union = index < plannedUnions132.Count ? plannedUnions132[index] : null;
                var members = union?.MemberRecruitIds ?? Array.Empty<string>();
                var memberStates = members
                    .Select(FindRecruit)
                    .Where(value => value != null)
                    .ToArray();
                var firstMemberLeader = members.Count == 0 ? null : members[0];
                var resourcePreview = M1CommandService.ProjectOpeningUnionResources(
                    _campaign.Guild,
                    union);
                var individualLegal = union != null && members.Count >= 1 &&
                                      members.Count <= NormalUnionPlanRules.MaximumMembersPerUnion &&
                                      OpeningUnionCatalog.IsFormation(union.FormationId) &&
                                      OpeningUnionCatalog.IsDoctrine(union.DoctrineId);
                result.Add(new M1UnionView
                {
                    Index = index,
                    UnionId = union?.UnionId ?? "UNION_OPENING_0" + (index + 1),
                    DisplayName = M1UnionIdentity076.Resolve(
                        union?.UnionId ?? "UNION_OPENING_0" + (index + 1),
                        union?.DisplayName,
                        index),
                    MemberRecruitIds = members,
                    LeaderRecruitId = firstMemberLeader,
                    FormationId = union?.FormationId,
                    DoctrineId = union?.DoctrineId,
                    SharedAp = resourcePreview.SharedAp,
                    CombinedCurrentHp = memberStates.Sum(value => Math.Min(
                        value.MaximumHp + value.Progression.MaximumHpBonus,
                        value.CurrentHp + value.Progression.MaximumHpBonus)),
                    CombinedMaximumHp = memberStates.Sum(value => value.MaximumHp + value.Progression.MaximumHpBonus),
                    CombinedMp = memberStates.Sum(value => Math.Min(
                        value.MaximumMp + value.Progression.MaximumMpBonus,
                        value.CurrentMp + value.Progression.MaximumMpBonus)),
                    CombinedMaximumMp = memberStates.Sum(value => value.MaximumMp + value.Progression.MaximumMpBonus),
                    CombinedAttack = memberStates.Sum(value =>
                        M1VisualAssets.OpeningBaseStatIndex(value.CanonicalApplicantJson, "STR") + value.Progression.StrengthBonus),
                    CombinedMagicAttack = memberStates.Sum(value =>
                        M1VisualAssets.OpeningBaseStatIndex(value.CanonicalApplicantJson, "MAGIC") + value.Progression.MagicBonus),
                    CombinedDefense = memberStates.Sum(value =>
                        M1VisualAssets.OpeningBaseStatIndex(value.CanonicalApplicantJson, "DEF") + value.Progression.DefenseBonus),
                    CombinedAgility = memberStates.Sum(value =>
                        M1VisualAssets.OpeningBaseStatIndex(value.CanonicalApplicantJson, "AGI") + value.Progression.AgilityBonus),
                    CombinedWill = memberStates.Sum(value =>
                        M1VisualAssets.OpeningBaseStatIndex(value.CanonicalApplicantJson, "WILL") + value.Progression.WillBonus),
                    BaseCohesionBasisPoints = resourcePreview.BaseCohesionBasisPoints,
                    FormationCohesionRuleBasisPoints = resourcePreview.FormationCohesionRuleBasisPoints,
                    CohesionBasisPoints = resourcePreview.CohesionBasisPoints,
                    IsLegal = individualLegal,
                    LegalitySummary = members.Count == 0
                        ? "Empty plan — optional"
                        : individualLegal
                            ? members.Count + " member" + (members.Count == 1 ? string.Empty : "s") + ", slot 1 leads, formation and doctrine set"
                            : "Needs one to six members, a formation and a doctrine",
                    ExpectedTendencies = UnionTendencies(union)
                });
            }
            return result;
        }

        private static bool OpeningEquipmentLegal(CampaignState campaign)
        {
            if (campaign?.OpeningFlow == null || !campaign.OpeningFlow.RecruitmentCompleted)
            {
                return false;
            }
            for (var index = 0; index < campaign.Guild.Recruits.Count; index++)
            {
                if (campaign.Guild.Recruits[index].Equipment.Find(EquipmentSlotIds.BodyArmor) == null) return false;
            }
            return true;
        }

        private static string ClassSymbol(string classId)
        {
            var id = classId ?? string.Empty;
            if (id.IndexOf("GUARD", StringComparison.OrdinalIgnoreCase) >= 0) return "◈";
            if (id.IndexOf("WARRIOR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("FIGHT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("PORTER", StringComparison.OrdinalIgnoreCase) >= 0) return "⚔";
            if (id.IndexOf("RANGER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("ARCHER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("TRAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("GENERALIST", StringComparison.OrdinalIgnoreCase) >= 0) return "➶";
            if (id.IndexOf("ROGUE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("SCOUT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("DUSK", StringComparison.OrdinalIgnoreCase) >= 0) return "◇";
            if (id.IndexOf("MYST", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("MAGE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("RUNE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("SIGNAL", StringComparison.OrdinalIgnoreCase) >= 0) return "✦";
            if (id.IndexOf("PRIEST", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("HEAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("MEDIC", StringComparison.OrdinalIgnoreCase) >= 0) return "✚";
            return "◆";
        }

        private static string EnemyVisualFamily070(BattleMemberState member)
        {
            if (member?.EquipmentTags != null)
                for (var index = 0; index < member.EquipmentTags.Count; index++)
                    if (!string.IsNullOrWhiteSpace(member.EquipmentTags[index]) &&
                        member.EquipmentTags[index].StartsWith("ENEMY_FAMILY_", StringComparison.Ordinal))
                        return member.EquipmentTags[index];
            var sourceEnemyId = member?.ClassId;
            if (string.IsNullOrWhiteSpace(sourceEnemyId) ||
                StringComparer.Ordinal.Equals(sourceEnemyId, "ENEMY_FORMATION_NUISANCE"))
                return "ENEMY_GATE_GNAWER";
            return sourceEnemyId;
        }

        private static string EnemyPortraitAuthority070(BattleMemberState member)
        {
            if (member == null) return string.Empty;
            return string.IsNullOrWhiteSpace(member.ClassId) ||
                   StringComparer.Ordinal.Equals(member.ClassId, "ENEMY_FORMATION_NUISANCE")
                ? member.MemberId
                : member.ClassId;
        }

        private static string EquipmentGlyph(string slotId, IReadOnlyList<string> tags)
        {
            if (tags != null)
            {
                if (tags.Contains("BOW")) return "➶";
                if (tags.Contains("SHIELD")) return "◈";
                if (tags.Contains("HEALING") || tags.Contains("REMEDY_KIT")) return "✚";
                if (tags.Contains("FOCUS") || tags.Contains("FOCUS_TOOL")) return "✦";
                if (tags.Contains("WEAPON")) return "⚔";
            }
            var id = slotId ?? string.Empty;
            if (id.IndexOf("BODY", StringComparison.Ordinal) >= 0) return "♜";
            if (id.IndexOf("ACCESSORY", StringComparison.Ordinal) >= 0) return "◆";
            if (id.IndexOf("TOOL", StringComparison.Ordinal) >= 0 ||
                id.IndexOf("RELIC", StringComparison.Ordinal) >= 0) return "◇";
            if (id.IndexOf("OFF_HAND", StringComparison.Ordinal) >= 0) return "◈";
            return "⚔";
        }

        private RecruitState FindRecruit(string recruitId) =>
            _campaign?.Guild?.Recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, recruitId));

        private static string ReadVisualSeed(string canonicalApplicantJson, string fallbackRecruitId)
        {
            if (!string.IsNullOrWhiteSpace(canonicalApplicantJson))
            {
                try
                {
                    var visualSeed = JObject.Parse(canonicalApplicantJson).Value<string>("visualSeed");
                    if (!string.IsNullOrWhiteSpace(visualSeed)) return visualSeed;
                }
                catch (JsonException)
                {
                    // A presentation fallback must never make a valid save unplayable.
                }
            }
            return fallbackRecruitId ?? string.Empty;
        }

        private static string RecurringApplicantTraits(string canonicalApplicantJson)
        {
            try
            {
                var record = JObject.Parse(canonicalApplicantJson ?? string.Empty);
                var traits = record["visibleTraitIds"] as JArray;
                var names = traits?.Values<string>()
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(Humanize)
                    .Take(3)
                    .ToArray();
                return names == null || names.Length == 0
                    ? "Patient, capable, and still getting to know the Guild"
                    : string.Join(" • ", names);
            }
            catch (JsonException)
            {
                return "Their strengths will become clearer while traveling together.";
            }
        }

        private static string RecurringApplicantPersonalHook(string canonicalApplicantJson)
        {
            try
            {
                var record = JObject.Parse(canonicalApplicantJson ?? string.Empty);
                var personalEvent = record.Value<string>("personalEventHookId");
                if (!string.IsNullOrWhiteSpace(personalEvent))
                    return RecurringApplicantPersonalHookForVerification078(personalEvent);
                var ambition = record.Value<string>("ambitionId");
                if (!string.IsNullOrWhiteSpace(ambition))
                    return "They hope to " + Humanize(ambition).ToLowerInvariant() + " by joining the Guild.";
            }
            catch (JsonException)
            {
                // Keep a human presentation fallback even if optional interview copy is unavailable.
            }
            return "They want a place in a Guild that protects people beyond Skyhome's gate.";
        }

        public static string RecurringApplicantPersonalHookForVerification078(string personalEventHookId)
        {
            var identity = (personalEventHookId ?? string.Empty).Trim();
            if (StringComparer.Ordinal.Equals(identity, "HOOK_CHILD_APPRENTICE"))
                return "They asked the Guild to help protect a young apprentice they have taken responsibility for.";

            var isHook = identity.StartsWith("HOOK_", StringComparison.Ordinal);
            var isQuest = identity.StartsWith("QUEST_", StringComparison.Ordinal);
            var authoredIdentity = isHook
                ? identity.Substring("HOOK_".Length)
                : isQuest
                    ? identity.Substring("QUEST_".Length)
                    : identity;
            var subject = Humanize(authoredIdentity).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(subject))
                return "They want a place in a Guild that protects people beyond Skyhome's gate.";
            if (isQuest)
                return "They hope the Guild will help them pursue " + subject + ".";
            if (isHook)
                return "They carry an unresolved responsibility connected to " + subject + ".";
            return "They joined the Guild because of " + subject + ".";
        }

        private static string PortraitAuthority(params string[] identities)
        {
            for (var index = 0; index < identities.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(identities[index])) return identities[index];
            }
            return string.Empty;
        }

        private static string FormatLeadership(ValueRange estimate)
        {
            if (estimate?.Range == null || estimate.Range.Count < 2) return "Not yet understood";
            return estimate.Range[0].ToString(CultureInfo.InvariantCulture) + "–" +
                   estimate.Range[1].ToString(CultureInfo.InvariantCulture) + " observed";
        }

        private static string EquipmentSummary(IReadOnlyList<EquipmentItemState> equipment) =>
            equipment == null || equipment.Count == 0
                ? "No starter gear recorded"
                : string.Join(", ", equipment.Select(value => value.DisplayName));

        private static string ForecastBehavior(IReadOnlyList<string> tags)
        {
            if (tags == null) return "No strong forecast tendency.";
            if (tags.Contains("SHIELD") || tags.Contains("ARMOR")) return "May strengthen Guard and rescue-oriented Union forecasts.";
            if (tags.Contains("HEALING") || tags.Contains("REMEDY_KIT")) return "May strengthen healing and support-oriented Union forecasts.";
            if (tags.Contains("FOCUS") || tags.Contains("FOCUS_TOOL")) return "May strengthen mystic and support-oriented Union forecasts.";
            if (tags.Contains("BOW") || tags.Contains("SCOUTING")) return "May strengthen ranged and tactical Union forecasts.";
            if (tags.Contains("WEAPON")) return "May strengthen offense-oriented Union forecasts.";
            return "May change which automatic actions fit the selected Union forecast.";
        }

        private static IReadOnlyList<string> CollectEquipmentTags(RecruitState recruit)
        {
            var result = new List<string>();
            if (recruit?.Equipment?.Assignments == null) return result.AsReadOnly();
            for (var assignmentIndex = 0; assignmentIndex < recruit.Equipment.Assignments.Count; assignmentIndex++)
            {
                var tags = recruit.Equipment.Assignments[assignmentIndex].Item.EquipmentTags;
                for (var tagIndex = 0; tagIndex < tags.Count; tagIndex++)
                {
                    if (!result.Contains(tags[tagIndex])) result.Add(tags[tagIndex]);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static string UnionTendencies(UnionState union)
        {
            if (union == null) return "Choose a formation and doctrine to establish expected tendencies.";
            var formation = OpeningUnionCatalog.Formations.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.Id, union.FormationId));
            var doctrine = OpeningUnionCatalog.Doctrines.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.Id, union.DoctrineId));
            return (formation?.DisplayName ?? "Formation not chosen") + " • " + (doctrine?.DisplayName ?? "Doctrine not chosen");
        }

        private static M1Screen ResumeScreen(CampaignState campaign)
        {
            if (campaign?.Battle != null)
            {
                if (campaign.Battle.Outcome == BattleOutcome.InProgress) return M1Screen.Battle;
                if (campaign.Battle.Reward != null && !campaign.Battle.Reward.Claimed)
                    return M1Screen.BattleResults;
            }
            var flow = campaign?.OpeningFlow;
            if (flow == null) return M1Screen.NewGuild;
            switch (flow.Stage)
            {
                case OpeningStage.Equipment: return M1Screen.Equipment;
                case OpeningStage.UnionBuilder: return M1Screen.UnionBuilder;
                case OpeningStage.Complete: return M1Screen.GuildOperations;
                default: return M1Screen.ApplicantBoard;
            }
        }

        private static bool TryMode(string id, out GameMode mode)
        {
            if (Enum.TryParse(id, ignoreCase: false, out mode) && mode != GameMode.Custom) return true;
            mode = GameMode.Standard;
            return false;
        }

        private static bool TryTutorialDepth(string id, out TutorialDepth depth)
        {
            switch (id)
            {
                case "Full Tutorial": depth = TutorialDepth.FullTutorial; return true;
                case "Contextual Tips": depth = TutorialDepth.ContextualTips; return true;
                case "Fast Charter": depth = TutorialDepth.FastCharter; return true;
                default: depth = TutorialDepth.FullTutorial; return false;
            }
        }

        private static string ResolveGuildCityContentRoot(string contentRoot)
        {
            if (File.Exists(Path.Combine(contentRoot, "GUILD_CITY_BUILDINGS_017D.json"))) return contentRoot;
            var nested = Path.Combine(contentRoot, "GUILD_CITY_017D");
            if (File.Exists(Path.Combine(nested, "GUILD_CITY_BUILDINGS_017D.json"))) return nested;
            return Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT", "GUILD_CITY_017D");
        }


        private static bool StoryGateSatisfied017H(IReadOnlyList<string> gates, string requiredGate)
        {
            if (string.IsNullOrWhiteSpace(requiredGate)) return true;
            if (gates == null) return false;
            for (var index = 0; index < gates.Count; index++)
                if (StringComparer.Ordinal.Equals(gates[index], requiredGate)) return true;
            return false;
        }

        private static string ResolveGuildCityStrategicContentRoot017H(string contentRoot)
        {
            if (File.Exists(Path.Combine(contentRoot, "BUILDING_COMBAT_XP_CONTRIBUTIONS_017H.json"))) return contentRoot;
            var nested = Path.Combine(contentRoot, "GUILD_CITY_017H");
            if (File.Exists(Path.Combine(nested, "BUILDING_COMBAT_XP_CONTRIBUTIONS_017H.json"))) return nested;
            return Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT", "GUILD_CITY_017H");
        }

        private void AppendStartupNotice(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            _startupNotice = string.IsNullOrWhiteSpace(_startupNotice) ? value : _startupNotice + " • " + value;
        }

        private static string ResolveRecruitmentContentRoot()
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var repositoryContent = Path.Combine(repositoryRoot, "CONTENT");
            if (File.Exists(Path.Combine(repositoryContent, "OPENING_PROCEDURAL_TABLES.json"))) return repositoryContent;
            return Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        }

        private string ArtDisplayName(string artId)
        {
            if (_combatContent != null && !string.IsNullOrWhiteSpace(artId) &&
                _combatContent.Arts.TryGetValue(artId, out var art))
            {
                return art.Name;
            }
            return Humanize(artId);
        }

        private static string Humanize(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return "Not yet understood";
            var value = stableId;
            foreach (var prefix in new[]
                     {
                         "HALL_STAGE_", "DISCIPLINE_", "FACILITY_", "FORMATION_", "DOCTRINE_",
                         "PERSONAL_EVENT_", "AMBITION_", "TRAIT_", "EVENT_", "LEAD_",
                         "EQ_PROC_", "SLOT_", "CLASS_", "WORLD_", "RACE_", "ART_", "EQ_"
                     })
            {
                if (value.StartsWith(prefix, StringComparison.Ordinal))
                {
                    value = value.Substring(prefix.Length);
                    break;
                }
            }
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').ToLowerInvariant());
        }

        private static string FriendlyErrors(IReadOnlyList<string> errors) =>
            errors == null || errors.Count == 0
                ? "The command could not be completed."
                : string.Join(" • ", errors.Select(FriendlyCode));

        private static string FriendlyCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "The command could not be completed.";
            var split = code.Split(new[] { ':' }, 2);
            switch (split[0])
            {
                case "M1_MAXIMUM_NORMAL_UNION_PLANS_REACHED":
                    return "You can prepare up to ten Union plans.";
                case "M1_NORMAL_UNION_PLAN_COUNT_OUT_OF_RANGE":
                    return "Prepare between two and ten Union plans.";
                case "M1_NORMAL_UNION_SUPPORTS_ONE_TO_SIX_MEMBERS":
                    return "Each ready Union supports one to six members.";
                case "M1_REQUIRES_AT_LEAST_TWO_USED_NORMAL_UNIONS":
                    return "Assign members to at least two Union plans.";
                case GuildMemberDeploymentPolicy017D.TrainingUnavailableError:
                    return "Training members stay home. Remove them from Union plans, or reassign them to Active or Reserve before deployment.";
                case "GC017D_INSUFFICIENT_TREASURY_TO_SIGN":
                    return "You need more Treasury XP before this person can join.";
                case "GC017D_ROSTER_CAPACITY_REACHED":
                    return "Your Guild roster is full. Improve the Hall before recruiting another member.";
                case "GC017D_INSUFFICIENT_TREASURY_FOR_BOARD_REFRESH":
                    return "You need more Treasury XP before posting another recruitment notice.";
                case "GC017D_SIGNED_MEMBER_CANNOT_BE_DECLINED":
                    return "A recruited member is permanent and cannot be dismissed from the applicant board.";
                case "GC017D_LAST_APPLICANT_REQUIRES_BOARD_REFRESH":
                    return "Keep this final interview open, or post a new recruitment notice.";
                case "GC017D_V69_RESOLVE_PENDING_ENCOUNTER_FIRST":
                    return "Your saved rescue has a committed encounter. Finish that battle before the route can be updated safely.";
                case "GC017D_V69_APPLY_PENDING_BATTLE_RETURN_FIRST":
                    return "Your saved rescue has a battle result waiting to return. Apply it before updating the route.";
                case "GC017D_V69_FINISH_ACTIVE_BATTLE_FIRST":
                    return "Finish the active battle before updating the rescue route.";
                case "GC017D_V69_CLAIM_BATTLE_REWARD_FIRST":
                    return "Claim the saved battle rewards first; the route update will preserve them and will not repeat the battle.";
                case "GC017D_V69_LEGACY_RESCUE_MUST_RESOLVE_FIRST":
                    return "This rescue is already at a final return state. Finish returning to the Hall instead of restarting it.";
                case "GC017D_FIRST_HOUR_PATROL_RESCUE_REQUIRED":
                    return "Free Zorin's Lantern Patrol and secure the Wayglass before facing the Gate-Eater.";
                case "GC017D_FIRST_HOUR_PATROL_NODE_REQUIRED":
                    return "Follow the gold waymarkers to Zorin's patrol before attempting the rescue.";
                case "GC017D_FIRST_HOUR_PATROL_PENDING_BATTLE_BLOCKS_RESCUE":
                    return "Finish the committed battle return before securing the patrol.";
            }
            if (split[0].IndexOf(' ') >= 0)
                return code.EndsWith(".", StringComparison.Ordinal) ? code : code + ".";
            var head = split[0].StartsWith("M1_", StringComparison.Ordinal)
                ? split[0].Substring(3)
                : split[0].StartsWith("GC017D_", StringComparison.Ordinal)
                    ? split[0].Substring(7)
                    : split[0];
            var message = Humanize(head) + (split.Length > 1 ? ":" + split[1] : string.Empty);
            return message + ".";
        }

        private void NotifyChangedAfterCommittedSave109()
        {
            var observers = Changed;
            if (observers == null) return;
            foreach (Action observer in observers.GetInvocationList())
            {
                using var observerTiming110 = BeginTowerObserverTiming110(observer);
                try
                {
                    observer();
                }
                catch (Exception exception)
                {
                    var owner = observer.Method.DeclaringType?.FullName ?? "unknown";
                    Debug.LogError(
                        "POST_SAVE_PRESENTATION_REFRESH_FAILED109\n" +
                        "The save is committed. Observer: " + owner + "." + observer.Method.Name + "\n" +
                        exception);
                }
            }
        }

        private void NotifyChanged() => Changed?.Invoke();
    }
}
