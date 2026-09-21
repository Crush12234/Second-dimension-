using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Release023;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Presentation.Release030
{
    public sealed class OwnerReviewSmokeExecution030
    {
        public OwnerReviewSmokeReport030 Report { get; set; }
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// Command-line-only owner-review path. Every mutation is submitted through the existing
    /// presentation coordinator; this class owns no gameplay state or parallel manager.
    /// </summary>
    public sealed class OwnerReviewSmokeWorkflow030
    {
        public const int ExitPass = 0;
        public const int ExitWorkflowFailure = 30;
        public const int ExitEvidenceFailure = 31;
        public const int ExitInvalidArguments = 32;

        private const string Pass = "PASS";
        private const string Fail = "FAIL";
        private const string Blocked = "BLOCKED";
        private static readonly IReadOnlyList<string> RequiredInitialGateIds = Array.AsReadOnly(new[]
        {
            "release_030_source_authority",
            "unity_6000_3_22f1_runtime",
            "guild_create_board_sign",
            "manual_equipment_inventory",
            "two_legal_unions_save_reload",
            "city_building_and_staff",
            "creator_code_redemption",
            "strategic_city_defense",
            "campaign_world_gate_people",
            "campaign_world_gate_return",
            "personal_quest_battle_memory",
            "free_hall_scene",
            "legend_and_signature_technique",
            "bond_doctrine",
            "guild_contract_event_check_camp",
            "guild_battles_rewards_return",
            "creator_room",
            "creator_room_return",
            "cinematic_union_combat",
            "union_capacity_10v10",
            "complete_union_forecasts",
            "predicted_member_arts_non_clickable",
            "weapon_family_one",
            "weapon_family_two",
            "offensive_mystic_school_one",
            "offensive_mystic_school_two",
            "discipline_combat",
            "discipline_mystic",
            "discipline_restoration",
            "discipline_guard",
            "discipline_warding",
            "deadlock_relationship",
            "positional_relationship",
            "exact_once_battle_reward",
            "reward_equipment_manual_equip",
            "link_art",
            "learn_by_use_weapon_growth",
            "equipment_evolution_or_advanced_class",
            "endless_abyss",
            "invocation_artifact",
            "summon_echo",
            "great_covenant",
            "save_format_11_identity"
        });
        private static readonly IReadOnlyList<string> RequiredRelaunchGateIds = Array.AsReadOnly(new[]
        {
            "initial_report_present",
            "standalone_save_loaded",
            "save_format_11_relaunch",
            "canonical_state_identity",
            "owner_state_reloaded"
        });
        private readonly OwnerReviewSmokeOptions030 _options;
        private readonly AtomicSaveStore _saveStore = new AtomicSaveStore();
        private readonly HashSet<string> _disciplines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _animationTags = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _weaponFamilies = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _mysticSchools = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _claimedRewardIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _manuallyEquippedRewardItemIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, SelectedActionEvidence030> _selectedActionEvidence =
            new Dictionary<string, SelectedActionEvidence030>(StringComparer.Ordinal);
        private OwnerReviewSmokeReport030 _report;
        private M1RuntimeCoordinator _coordinator;
        private string _legendRecruitId = string.Empty;
        private string _primaryRewardItemId = string.Empty;
        private string _primaryRewardRecruitId = string.Empty;
        private bool _linkArtForecastSeen;
        private bool _linkArtTriggered;
        private bool _completeForecastsObserved;
        private bool _completeForecastsValid = true;
        private bool _predictedMemberActionsObserved;
        private bool _deadlockObserved;
        private bool _positionalRelationshipObserved;
        private bool _allRewardsExactOnce = true;
        private bool _allRewardEquipmentExactOnce = true;
        private int _maximumAlliedUnionsObserved;
        private int _maximumEnemyUnionsObserved;
        private int _battleCount;

        public OwnerReviewSmokeWorkflow030(OwnerReviewSmokeOptions030 options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public OwnerReviewSmokeExecution030 Execute()
        {
            _report = NewReport();
            try
            {
                Directory.CreateDirectory(_options.EvidenceDirectory);
                WriteProgress();
                if (_options.RunKind == OwnerReviewSmokeRunKind030.Relaunch)
                    ExecuteRelaunch();
                else
                    ExecuteInitial();
            }
            catch (Exception exception)
            {
                _report.failure = exception.ToString();
                AddGate("unexpected_failure", _report.currentStage, Fail, exception.Message);
            }

            _report.completedUtc = DateTime.UtcNow.ToString("O");
            _report.checklistComplete = EvaluateChecklist030(_report, _options.RunKind);
            _report.runStatus = _report.checklistComplete ? Pass : Fail;
            _report.processExitCode = _report.checklistComplete ? ExitPass : ExitWorkflowFailure;
            if (!_report.checklistComplete && string.IsNullOrWhiteSpace(_report.failure))
                _report.failure = "Owner-review checklist is incomplete, duplicated, missing, or contains a non-PASS gate.";
            try
            {
                OwnerReviewSmokeEvidenceWriter030.Write(_options.ReportPath, _report);
                if (File.Exists(_options.ProgressPath)) File.Delete(_options.ProgressPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("OWNER REVIEW 030 EVIDENCE WRITE FAILED: " + exception);
                _report.runStatus = Fail;
                _report.processExitCode = ExitEvidenceFailure;
                _report.failure = exception.ToString();
            }
            return new OwnerReviewSmokeExecution030 { Report = _report, ExitCode = _report.processExitCode };
        }

        public static IReadOnlyList<string> RequiredGateIdsFor030(OwnerReviewSmokeRunKind030 runKind)
        {
            return runKind == OwnerReviewSmokeRunKind030.Initial
                ? RequiredInitialGateIds
                : runKind == OwnerReviewSmokeRunKind030.Relaunch
                    ? RequiredRelaunchGateIds
                    : Array.Empty<string>();
        }

        public static bool EvaluateChecklist030(
            OwnerReviewSmokeReport030 report,
            OwnerReviewSmokeRunKind030 runKind)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var required = RequiredGateIdsFor030(runKind);
            report.requiredGateIds = required.ToList();
            report.requiredGateCount = required.Count;
            report.missingRequiredGateIds = new List<string>();
            report.duplicateRequiredGateIds = new List<string>();
            report.nonPassingRequiredGateIds = new List<string>();
            report.passedRequiredGateCount = 0;
            var gates = report.gates ?? new List<OwnerReviewGateEvidence030>();
            foreach (var requiredId in required)
            {
                var matches = gates.Where(value => value != null &&
                                                   StringComparer.Ordinal.Equals(value.id, requiredId)).ToArray();
                if (matches.Length == 0)
                {
                    report.missingRequiredGateIds.Add(requiredId);
                    continue;
                }
                if (matches.Length != 1)
                {
                    report.duplicateRequiredGateIds.Add(requiredId);
                    continue;
                }
                if (!StringComparer.Ordinal.Equals(matches[0].status, Pass))
                {
                    report.nonPassingRequiredGateIds.Add(requiredId);
                    continue;
                }
                report.passedRequiredGateCount++;
            }
            return required.Count > 0 &&
                   report.passedRequiredGateCount == required.Count &&
                   report.missingRequiredGateIds.Count == 0 &&
                   report.duplicateRequiredGateIds.Count == 0 &&
                   report.nonPassingRequiredGateIds.Count == 0 &&
                   gates.Count > 0 && gates.All(value => value != null &&
                       StringComparer.Ordinal.Equals(value.status, Pass));
        }

        private OwnerReviewSmokeReport030 NewReport()
        {
            return new OwnerReviewSmokeReport030
            {
                runKind = _options.RunKind == OwnerReviewSmokeRunKind030.Relaunch ? "RELAUNCH" : "INITIAL",
                startedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                executablePath = Application.dataPath,
                reportPath = _options.ReportPath,
                savePath = _options.SavePath,
                currentStage = "Source"
            };
        }

        private void ExecuteInitial()
        {
            PrepareCleanInitialSave();
            var readiness = ReleaseSupersessionReadinessService030.BuildSnapshot();
            if (!readiness.IsReady) throw new InvalidOperationException(readiness.Error);
            _report.activePipeline = readiness.ActivePipeline ?? string.Empty;
            _report.saveFormatVersion = readiness.SaveFormatVersion;
            AddGate("release_030_source_authority", "Source", Pass,
                "Active supersession contract and " + readiness.CompatibleReleaseManifests + " retained manifests validated.");
            AddGate("unity_6000_3_22f1_runtime", "Unity",
                StringComparer.Ordinal.Equals(Application.unityVersion, "6000.3.22f1") ? Pass : Fail,
                "Running Unity " + Application.unityVersion + ".");
            if (!StringComparer.Ordinal.Equals(Application.unityVersion, "6000.3.22f1"))
                throw new InvalidOperationException("Built player is not running Unity 6000.3.22f1.");

            _coordinator = new M1RuntimeCoordinator(ContentRoot(), _options.SavePath);
            RunOpening();
            RunCityBuilding();
            RunCreatorCode();
            RunDefense();
            RunCampaignAndWorldGate();
            RunPeople();
            RunGuildExpedition();
            RunLegendAndSignature();
            RunProgression();
            SummarizeCombatCoverage();
            Require(_coordinator.SaveAndReloadProof(), "final save/reload proof");
            CaptureFinalSaveIdentity();
        }

        private void ExecuteRelaunch()
        {
            var initial = OwnerReviewSmokeEvidenceWriter030.Read(_options.InitialReportPath);
            if (initial == null) throw new InvalidOperationException("Initial built-player report is missing.");
            if (!StringComparer.Ordinal.Equals(initial.runStatus, Pass) || !initial.checklistComplete)
                throw new InvalidOperationException("Initial built-player report is not a complete PASS.");
            AddGate("initial_report_present", "Source", Pass, "Initial evidence report loaded.");
            if (!File.Exists(_options.SavePath)) throw new InvalidOperationException("Owner-review v11 save is missing.");
            _coordinator = new M1RuntimeCoordinator(ContentRoot(), _options.SavePath);
            if (!_coordinator.State.HasCampaign) throw new InvalidOperationException("Coordinator could not restore the owner-review campaign.");
            AddGate("standalone_save_loaded", "Relaunch", Pass, "Standalone coordinator restored the owner-review save.");

            var loaded = _saveStore.ReadWithRecovery(_options.SavePath);
            if (!loaded.IsSuccess) throw new InvalidOperationException(string.Join("\n", loaded.Errors));
            _report.saveFormatVersion = loaded.Value.SaveFormatVersion;
            _report.canonicalStateHash = loaded.Value.CanonicalStateHash;
            _report.expectedCanonicalStateHash = initial.canonicalStateHash ?? string.Empty;
            _report.stateIdentityMatched = StringComparer.Ordinal.Equals(
                _report.canonicalStateHash, _report.expectedCanonicalStateHash);
            if (loaded.Value.SaveFormatVersion != SaveEnvelopeV1.CurrentFormatVersion)
                throw new InvalidOperationException("Relaunch save format is not v" + SaveEnvelopeV1.CurrentFormatVersion + ".");
            AddGate("save_format_11_relaunch", "Relaunch", Pass, "Save envelope format v11 restored.");
            if (!_report.stateIdentityMatched)
                throw new InvalidOperationException("Canonical state hash changed across close and relaunch.");
            AddGate("canonical_state_identity", "Relaunch", Pass,
                "Canonical state hash matches the initial built-player pass.");

            _report.state = Snapshot(loaded.Value.CampaignState);
            var snapshotsMatch = StringComparer.Ordinal.Equals(
                JsonConvert.SerializeObject(initial.state), JsonConvert.SerializeObject(_report.state));
            if (!snapshotsMatch) throw new InvalidOperationException("Owner-review state summary changed across relaunch.");
            AddGate("owner_state_reloaded", "Relaunch", Pass,
                "Guild, roster, Union, city, campaign, People, Creator, and progression summary fields match.");
            _report.activePipeline = initial.activePipeline ?? string.Empty;
        }

        private void RunOpening()
        {
            Require(_coordinator.CreateGuild(new M1NewGuildIntent
            {
                GuildmasterName = "Owner Review 030",
                ModeId = "Standard",
                TutorialDepthId = "Full Tutorial",
                TextScale = 1f,
                HighContrast = false,
                ReducedMotion = false
            }), "create Release 030 guild");
            var applicants = _coordinator.State.Applicants.Select(value => value.RecruitId).ToArray();
            if (applicants.Length != 6) throw new InvalidOperationException("Opening board did not expose six committed applicants.");
            foreach (var recruitId in applicants) Require(_coordinator.SignRecruit(recruitId), "sign " + recruitId);

            var recruit = _coordinator.State.Recruits.FirstOrDefault();
            var slot = recruit?.Slots.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.EquippedItemId));
            if (recruit == null || slot == null) throw new InvalidOperationException("No starter item was available for manual equipment proof.");
            var itemId = slot.EquippedItemId;
            Require(_coordinator.UnequipItem(recruit.RecruitId, slot.SlotId), "manual unequip");
            Require(_coordinator.EquipItem(recruit.RecruitId, slot.SlotId, itemId), "manual re-equip");
            if (!_coordinator.State.Recruits.Any(value => value.HasManualEquipAction))
                throw new InvalidOperationException("Manual equipment action was not persisted.");
            Require(_coordinator.CompleteEquipmentReview(), "complete equipment review");
            AddGate("manual_equipment_inventory", "Opening", Pass,
                "Starter equipment was manually unequipped to inventory and re-equipped to the same slot.");

            var recruitIds = _coordinator.State.Recruits.Select(value => value.RecruitId).ToArray();
            for (var index = 0; index < 3; index++)
                Require(_coordinator.AssignRecruitToUnion(recruitIds[index], 0, index), "assign first Union member");
            for (var index = 3; index < 6; index++)
                Require(_coordinator.AssignRecruitToUnion(recruitIds[index], 1, index - 3), "assign second Union member");
            for (var unionIndex = 0; unionIndex < 2; unionIndex++)
            {
                var formation = _coordinator.State.Formations[unionIndex % _coordinator.State.Formations.Count].Id;
                var doctrine = _coordinator.State.Doctrines[unionIndex % _coordinator.State.Doctrines.Count].Id;
                Require(_coordinator.SetFormation(unionIndex, formation), "set formation");
                Require(_coordinator.SetDoctrine(unionIndex, doctrine), "set doctrine");
            }
            Require(_coordinator.SaveAndReloadProof(), "opening save/reload proof");
            var state = _coordinator.State;
            if (!state.AllSixSigned || !state.OpeningEquipmentLegal || !state.TwoUnionsLegal ||
                state.Unions.Count != 2 || state.Unions.Any(value => !value.IsLegal))
                throw new InvalidOperationException("Opening roster or two-Union legality proof failed.");
            AddGate("two_legal_unions_save_reload", "Opening", Pass,
                "Two legal three-member Unions persisted with manual formations and doctrines.");

            var firstRecurringRecruitId = SignCharterCoveredRecurringRecruit030(_coordinator);
            AddGate("guild_create_board_sign", "Opening", Pass,
                "Guild created, all six founders signed permanently, and charter-covered recurring member " +
                firstRecurringRecruitId + " joined in Reserve before the first contract.");
        }

        public static string SignCharterCoveredRecurringRecruit030(M1RuntimeCoordinator coordinator)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            var before = coordinator.GuildCity017D;
            if (before.OperationOrdinal != 0 || before.RecruitmentRefreshOrdinal != 0 ||
                before.TotalRecruitCount != 6 || !coordinator.State.AllSixSigned)
                throw new InvalidOperationException(
                    "The charter-covered recurring recruit must be signed immediately after the six-founder opening.");

            var treasuryBefore = before.TreasuryXp;
            Require(coordinator.CommitGuildCityApplicantBoard017D(),
                "commit first recurring Applicant Board");
            var committed = coordinator.GuildCity017D;
            var applicant = committed.Applicants
                .Where(value => !value.IsSigned)
                .OrderBy(value => value.Slot)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (applicant == null)
                throw new InvalidOperationException(
                    "The first recurring Applicant Board did not expose an unsigned recruit.");
            if (applicant.SigningCostTreasuryXp != 0)
                throw new InvalidOperationException(
                    "The founding charter did not cover the first recurring recruit.");

            Require(coordinator.SignGuildCityApplicant017D(applicant.RecruitId),
                "sign charter-covered recurring recruit");
            var after = coordinator.GuildCity017D;
            var signedRecruit = coordinator.State.Recruits.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, applicant.RecruitId));
            var reserveAssignment = after.Assignments.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, applicant.RecruitId));
            if (after.TotalRecruitCount != 7 || after.SignedApplicantCount != 1 ||
                after.TreasuryXp != treasuryBefore || !coordinator.State.AllSixSigned ||
                signedRecruit == null || !signedRecruit.Slots.Any(value =>
                    !string.IsNullOrWhiteSpace(value.EquippedItemId)) ||
                reserveAssignment == null ||
                !StringComparer.OrdinalIgnoreCase.Equals(reserveAssignment.Kind, "Reserve"))
                throw new InvalidOperationException(
                    "The charter-covered recurring recruit did not persist as an equipped seventh Reserve member.");

            var signedStateHash = coordinator.State.CanonicalStateHash;
            Require(coordinator.SignGuildCityApplicant017D(applicant.RecruitId),
                "repeat charter-covered recurring recruit signing");
            if (!StringComparer.Ordinal.Equals(signedStateHash, coordinator.State.CanonicalStateHash) ||
                coordinator.GuildCity017D.TotalRecruitCount != 7 ||
                coordinator.GuildCity017D.TreasuryXp != treasuryBefore)
                throw new InvalidOperationException(
                    "Recurring recruit signing was not exact-once and idempotent.");
            return applicant.RecruitId;
        }

        private void RunCityBuilding()
        {
            var city = _coordinator.GuildCity017D;
            M1CommandResult placement = null;
            string placedPlot = null;
            foreach (var plot in city.Plots.Where(value => value.Unlocked && value.RoadConnected &&
                                                            string.IsNullOrWhiteSpace(value.BuildingId)))
            {
                foreach (var building in city.Buildings.Where(value =>
                             StringComparer.Ordinal.Equals(value.DistrictId, plot.DistrictId)))
                {
                    placement = _coordinator.PlaceGuildCityBuilding017D(plot.PlotId, building.BuildingId);
                    if (!placement.Succeeded) continue;
                    placedPlot = plot.PlotId;
                    break;
                }
                if (placedPlot != null) break;
            }
            if (placedPlot == null) throw new InvalidOperationException("No legal charter building placement succeeded: " + placement?.Message);
            var staffRecruit = _coordinator.State.Recruits.First().RecruitId;
            Require(_coordinator.AssignGuildCityStaff017D(placedPlot, staffRecruit), "assign city staff");
            city = _coordinator.GuildCity017D;
            if (city.PlacedBuildingCount < 1 || city.StaffedBuildingCount < 1)
                throw new InvalidOperationException("Building/staff assignment did not persist.");
            AddGate("city_building_and_staff", "City", Pass,
                "A legal charter building was placed and a signed member staffed it while remaining deployable.");
        }

        private void RunCreatorCode()
        {
            if (string.IsNullOrWhiteSpace(_options.CreatorCode))
            {
                AddGate("creator_code_redemption", "Creator", Blocked,
                    "No external Creator code was supplied. Plaintext codes are intentionally absent from player assets and evidence.");
                return;
            }
            var before = _coordinator.CreatorAccess028.RedeemedCodes;
            var result = _coordinator.RedeemCreatorCode028(_options.CreatorCode);
            if (!result.Succeeded) throw new InvalidOperationException("Creator code redemption failed: " + result.Message);
            if (_coordinator.CreatorAccess028.RedeemedCodes <= before)
                throw new InvalidOperationException("Creator code ledger did not advance.");
            AddGate("creator_code_redemption", "Creator", Pass,
                "Externally supplied code redeemed exactly once; plaintext was not written to evidence.");
        }

        private void RunDefense()
        {
            Require(_coordinator.SynchronizeGuildCityCanonEvents017H(), "synchronize canon events");
            var before = _coordinator.GuildCityStrategic017H;
            var profile = before.DefenseProfiles.FirstOrDefault(value =>
                              value.Available && !value.FutureLocked &&
                              StringComparer.Ordinal.Equals(value.ProfileId, "DEF_PROFILE_FIRST_BELL_DRILL")) ??
                          before.DefenseProfiles.FirstOrDefault(value => value.Available && !value.FutureLocked);
            if (profile == null) throw new InvalidOperationException("No opening defense profile is available.");
            Require(_coordinator.StartGuildCityDefense017H(profile.ProfileId), "start city defense");
            var active = _coordinator.GuildCityStrategic017H.ActiveDefense;
            var unions = _coordinator.State.Unions.Select(value => value.UnionId).ToArray();
            for (var laneIndex = 0; laneIndex < active.Lanes.Count; laneIndex++)
                Require(_coordinator.AssignGuildCityDefenseUnion017H(active.Lanes[laneIndex].LaneId,
                    unions[Math.Min(laneIndex, unions.Length - 1)]), "assign defense lane");

            for (var guard = 0; guard < 16; guard++)
            {
                active = _coordinator.GuildCityStrategic017H.ActiveDefense;
                if (active == null) break;
                if (StringComparer.OrdinalIgnoreCase.Equals(active.Status, "Completed") ||
                    StringComparer.OrdinalIgnoreCase.Equals(active.Status, "Failed"))
                {
                    Require(_coordinator.FinalizeGuildCityDefense017H(), "finalize city defense");
                    break;
                }
                if (StringComparer.OrdinalIgnoreCase.Equals(active.Status, "AwaitingBattle"))
                {
                    Require(_coordinator.StartCommittedGuildCityDefenseBattle017H(), "start decisive defense battle");
                    RunBattle("defense");
                }
                else
                {
                    Require(_coordinator.CommitGuildCityDefenseWave017H(), "commit defense wave");
                }
            }
            var after = _coordinator.GuildCityStrategic017H;
            if (after.ActiveDefense != null || after.DefensesWon + after.DefensesLost <= before.DefensesWon + before.DefensesLost)
                throw new InvalidOperationException("City defense did not reach and persist a terminal result.");
            AddGate("strategic_city_defense", "Defense", Pass,
                "Lane assignment, support waves, decisive certified battle, and final defense result persisted.");
        }

        private void RunCampaignAndWorldGate()
        {
            var worldGateCompleted = CompletePlayableChapter("CH018_001");
            if (!worldGateCompleted) throw new InvalidOperationException("CH018_001 did not execute its World Gate board.");
            var chapter = _coordinator.Campaign019.Chapters.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.ChapterId, "CH018_001"));
            if (chapter == null || !chapter.Completed) throw new InvalidOperationException("CH018_001 completion was not persisted.");
            AddGate("campaign_world_gate_people", "Campaign", Pass,
                "CH018_001 completed through its committed World Gate board, civic People event, camp, objective, and return.");
            var returned = string.IsNullOrWhiteSpace(_coordinator.CampaignWorldGate023.ActiveOperationId) &&
                           string.IsNullOrWhiteSpace(_coordinator.CampaignPlayable020.ActiveOperationId);
            AddGate("campaign_world_gate_return", "Campaign", returned ? Pass : Blocked,
                returned
                    ? "The World Gate receipt returned to the same chapter operation and the completed chapter archived cleanly."
                    : "A World Gate or chapter operation remained active after its terminal receipt.");
        }

        private bool CompletePlayableChapter(string chapterId)
        {
            Require(_coordinator.StartPlayableChapter020(chapterId), "start " + chapterId);
            var worldGateCompleted = false;
            for (var guard = 0; guard < 40; guard++)
            {
                var playable = _coordinator.CampaignPlayable020;
                if (string.IsNullOrWhiteSpace(playable.ActiveOperationId)) break;
                var step = playable.Steps.FirstOrDefault(value =>
                    StringComparer.OrdinalIgnoreCase.Equals(value.Status, "CURRENT"));
                if (step == null || StringComparer.OrdinalIgnoreCase.Equals(playable.Status, "ReadyToFinalize")) break;
                if (StringComparer.OrdinalIgnoreCase.Equals(step.Kind, "WORLD_BOARD"))
                {
                    Require(_coordinator.BeginWorldGateOperation023(playable.ActiveChapterId), "begin World Gate chapter board");
                    DriveWorldGate();
                    worldGateCompleted = true;
                }
                else if (step.RequiresCertifiedBattle)
                {
                    Require(_coordinator.EnterPlayableBattle020(), "enter playable campaign battle");
                    RunBattle("campaign");
                    Require(_coordinator.CommitPlayableBattleStepResult020(), "commit campaign battle result");
                }
                else
                {
                    Require(_coordinator.CommitPlayableStep020("SUCCESS"), "commit campaign step");
                    Require(_coordinator.ApplyPlayableStep020(), "apply campaign step");
                }
            }
            Require(_coordinator.FinalizePlayableChapter020(), "finalize playable chapter");
            Require(_coordinator.ApplyPlayableChapterResult020(), "apply playable chapter result");
            return worldGateCompleted;
        }

        private void DriveWorldGate()
        {
            for (var guard = 0; guard < 32; guard++)
            {
                TryCreatorRoom();
                var state = _coordinator.CampaignWorldGate023;
                if (string.IsNullOrWhiteSpace(state.ActiveOperationId)) return;
                if (StringComparer.OrdinalIgnoreCase.Equals(state.ActiveStatus, "ReadyToFinalize"))
                {
                    Require(_coordinator.FinalizeWorldGateOperation023(), "finalize World Gate operation");
                    return;
                }
                var node = state.CurrentNode;
                if (node == null) throw new InvalidOperationException("World Gate current node is unavailable.");
                if (node.RequiresBattle)
                {
                    Require(_coordinator.EnterWorldGateBattle023(), "enter World Gate battle");
                    RunBattle("world_gate");
                    continue;
                }
                var choice = node.Choices != null && node.Choices.Count > 0 ? node.Choices[0] : string.Empty;
                Require(_coordinator.CommitWorldGateChoice023(choice), "commit World Gate node");
                Require(_coordinator.ApplyWorldGateReceipt023(), "apply World Gate node");
            }
            throw new InvalidOperationException("World Gate guard limit exceeded.");
        }

        private void RunPeople()
        {
            var people = _coordinator.PeopleRuntime029;
            if (!people.IsAvailable || people.Quests.Count == 0)
                throw new InvalidOperationException("Signed opening recruits did not resolve to authored personal quests: " + people.Error);
            var unionMemberRecruitIds = new HashSet<string>(
                people.Unions.SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
            var eligibleQuests = people.Quests.Where(value =>
                unionMemberRecruitIds.Contains(value.RecruitId)).ToArray();
            var quest = eligibleQuests.FirstOrDefault(value =>
                            StringComparer.Ordinal.Equals(
                                value.BoardId, "PQ025_SIGREC_MAREN_HOLT_01")) ??
                        eligibleQuests.FirstOrDefault();
            if (quest == null)
                throw new InvalidOperationException(
                    "No authored personal quest belonged to a deployed Union member.");
            var boardId = quest.BoardId;
            var savedBeforeQuest = ReadSave().CampaignState;
            var questRecruit = savedBeforeQuest.Guild.Recruits.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, quest.RecruitId));
            if (questRecruit == null)
                throw new InvalidOperationException(
                    "The selected authored personal quest did not resolve to its signed recruit instance.");
            var questUnion = savedBeforeQuest.Guild.Unions.FirstOrDefault(value =>
                value.MemberRecruitIds.Contains(questRecruit.RecruitId, StringComparer.Ordinal));
            var relationshipPartnerId = questUnion?.MemberRecruitIds
                .Where(value => !StringComparer.Ordinal.Equals(value, questRecruit.RecruitId))
                .OrderBy(value => value, StringComparer.Ordinal)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(relationshipPartnerId))
                throw new InvalidOperationException(
                    "The selected personal-quest recruit had no Union partner for relationship-memory proof.");
            var peopleOperationOrdinal = savedBeforeQuest.Guild.GuildCity.OperationOrdinal;
            var memoryCountBefore = RecruitChronicleRules025.MeaningfulMemoryCount(
                savedBeforeQuest.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025,
                questRecruit.RecruitId);
            Require(_coordinator.StartPersonalQuest029(boardId), "start personal quest");
            for (var guard = 0; guard < 32; guard++)
            {
                quest = _coordinator.PeopleRuntime029.Quests.First(value =>
                    StringComparer.Ordinal.Equals(value.BoardId, boardId));
                if (quest.Complete) break;
                if (StringComparer.OrdinalIgnoreCase.Equals(quest.CurrentNodeKind, "BATTLE"))
                {
                    Require(_coordinator.EnterPersonalQuestBattle029(boardId), "enter personal quest battle");
                    RunBattle("personal_quest");
                }
                else
                {
                    if (quest.NextNodeIds == null || quest.NextNodeIds.Count == 0)
                        throw new InvalidOperationException("Personal quest has no legal next node.");
                    Require(_coordinator.AdvancePersonalQuest029(boardId, quest.NextNodeIds[0]),
                        "advance personal quest");
                }
            }
            quest = _coordinator.PeopleRuntime029.Quests.First(value =>
                StringComparer.Ordinal.Equals(value.BoardId, boardId));
            if (!quest.Complete) throw new InvalidOperationException("Personal quest did not reach RETURN.");
            var afterPeopleMemories = ReadSave().CampaignState;
            var memoryCount = RecruitChronicleRules025.MeaningfulMemoryCount(
                afterPeopleMemories.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025,
                questRecruit.RecruitId);
            people = _coordinator.PeopleRuntime029;
            var relationshipPair = people.Bonds.FirstOrDefault(value =>
                (StringComparer.Ordinal.Equals(value.FirstRecruitId, questRecruit.RecruitId) &&
                 StringComparer.Ordinal.Equals(value.SecondRecruitId, relationshipPartnerId)) ||
                (StringComparer.Ordinal.Equals(value.FirstRecruitId, relationshipPartnerId) &&
                 StringComparer.Ordinal.Equals(value.SecondRecruitId, questRecruit.RecruitId)));
            const int earnedPersonalQuestMemories = 5;
            if (memoryCount != memoryCountBefore + earnedPersonalQuestMemories ||
                relationshipPair == null || relationshipPair.TierRank < 2 ||
                relationshipPair.Trust < 25 ||
                relationshipPair.SharedMemories < earnedPersonalQuestMemories ||
                afterPeopleMemories.Guild.GuildCity.OperationOrdinal != peopleOperationOrdinal)
                throw new InvalidOperationException(
                    "The personal quest did not persist its exact five-memory Trusted bond without consuming an operation.");
            AddGate("personal_quest_battle_memory", "People", Pass,
                "The authored personal quest and its certified battle automatically persisted five exact-once relationship memories, increasing the selected recruit's meaningful-memory count from " +
                memoryCountBefore + " to " + memoryCount + " and earned a Trusted Union bond.");

            var hall = _coordinator.PeopleRuntime029.AvailableHallScenes.FirstOrDefault();
            var operationBeforeHall = ReadSave().CampaignState.Guild.GuildCity.OperationOrdinal;
            if (hall != null)
            {
                Require(_coordinator.ViewHallScene029(hall.SceneId), "view Hall scene");
                var saved = ReadSave().CampaignState;
                var viewed = saved.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                    .RecruitChronicles025.ViewedHallSceneIds.Contains(hall.SceneId);
                var free = saved.Guild.GuildCity.OperationOrdinal == operationBeforeHall;
                AddGate("free_hall_scene", "People", viewed && free ? Pass : Blocked,
                    viewed && free
                        ? "A canonical non-expiring Hall scene persisted as viewed without consuming an operation."
                        : "The Hall scene command did not persist its viewed ID or changed the operation ordinal.");
            }
            else
            {
                const string deferredSceneId = "HALL025_000";
                Require(_coordinator.DeferHallScene029(deferredSceneId), "defer Hall scene without penalty");
                var saved = ReadSave().CampaignState;
                var chronicles = saved.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
                var deferred = RecruitChronicleRules025.HasDeferredHallScene(
                    chronicles, saved.CampaignSeed, deferredSceneId);
                var free = saved.Guild.GuildCity.OperationOrdinal == operationBeforeHall;
                AddGate("free_hall_scene", "People", deferred && free ? Pass : Blocked,
                    deferred && free
                        ? "The authored free Hall scene was deferred through its exact-once no-penalty receipt without consuming an operation."
                        : "The Hall deferral receipt was absent or the operation ordinal changed.");
            }

            _legendRecruitId = quest.RecruitId;
            people = _coordinator.PeopleRuntime029;
            var doctrineUnion = people.Unions.FirstOrDefault(value =>
                value.MemberRecruitIds.Contains(questRecruit.RecruitId, StringComparer.Ordinal));
            var doctrineMembers = doctrineUnion == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(doctrineUnion.MemberRecruitIds, StringComparer.Ordinal);
            var strongestBond = people.Bonds
                .Where(value => doctrineMembers.Contains(value.FirstRecruitId) &&
                                doctrineMembers.Contains(value.SecondRecruitId))
                .OrderByDescending(value => value.TierRank)
                .ThenByDescending(value => value.Trust)
                .ThenBy(value => value.PairId, StringComparer.Ordinal)
                .FirstOrDefault();
            var eligibleDoctrine = strongestBond == null
                ? null
                : people.Doctrines
                    .Where(value => SecondDimension.Gameplay.PeopleBonds026.PeopleBondService026.TierRank(
                                        value.MinimumBondTierId) <= strongestBond.TierRank)
                    .OrderByDescending(value => SecondDimension.Gameplay.PeopleBonds026.PeopleBondService026.TierRank(
                        value.MinimumBondTierId))
                    .ThenBy(value => value.DoctrineId, StringComparer.Ordinal)
                    .FirstOrDefault();
            if (doctrineUnion == null || strongestBond == null || eligibleDoctrine == null)
                AddGate("bond_doctrine", "People", Blocked,
                    "The completed personal quest did not produce an eligible authored doctrine for an in-Union bond pair.");
            else
            {
                var doctrineResult = _coordinator.SetUnionBondDoctrine029(
                    doctrineUnion.UnionId, eligibleDoctrine.DoctrineId);
                var runtimePersisted = _coordinator.PeopleRuntime029.Unions.Any(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, doctrineUnion.UnionId) &&
                    StringComparer.Ordinal.Equals(value.BondDoctrineId, eligibleDoctrine.DoctrineId));
                var savedBonds = ReadSave().CampaignState.Guild.GuildCity.Strategic017H.Campaign019.Playable020.PeopleBonds026;
                var savePersisted = savedBonds.Unions.Any(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, doctrineUnion.UnionId) &&
                    StringComparer.Ordinal.Equals(value.DoctrineId, eligibleDoctrine.DoctrineId));
                var doctrinePassed = doctrineResult.Succeeded && runtimePersisted && savePersisted;
                AddGate("bond_doctrine", "People", doctrinePassed ? Pass : Blocked,
                    doctrinePassed
                        ? "Authored quest memories earned the selected doctrine tier, and the eligible Union doctrine persisted to save."
                        : "An eligible earned doctrine was selected, but the command or persistence proof failed: " + doctrineResult.Message);
            }
        }

        private void RunLegendAndSignature()
        {
            if (string.IsNullOrWhiteSpace(_legendRecruitId))
            {
                AddGate("legend_and_signature_technique", "People", Blocked,
                    "No completed authored personal quest supplied a recruit identity for the Legend gates.");
                return;
            }
            var before = ReadSave().CampaignState.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .RecruitChronicles025;
            var beforeLegendCount = before.UnlockedLegendIds.Count;
            var beforeTechniqueCount = before.UnlockedSignatureTechniqueIds.Count;
            M1CommandResult unlock = null;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                unlock = _coordinator.UnlockLegendTechnique029(_legendRecruitId);
                if (unlock.Succeeded) break;
                if (attempt == 7) break;
                Require(_coordinator.StartTutorialBattle(), "start earned Legend mastery battle");
                RunBattle("legend_mastery");
            }
            var runtime = _coordinator.PeopleRuntime029.Recruits.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, _legendRecruitId));
            var saved = ReadSave().CampaignState.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .RecruitChronicles025;
            var persisted = unlock != null && unlock.Succeeded && runtime != null && runtime.LegendUnlocked &&
                            runtime.SignatureTechniqueUnlocked &&
                            saved.UnlockedLegendIds.Count == beforeLegendCount + 1 &&
                            saved.UnlockedSignatureTechniqueIds.Count == beforeTechniqueCount + 1;
            AddGate("legend_and_signature_technique", "People", persisted ? Pass : Blocked,
                persisted
                    ? "Certified meaningful use met both mastery thresholds; one emergent Legend and one signature-technique ID persisted."
                    : "The authored quest/memory gates persisted, but both mastery-qualified unlock IDs were not proven: " +
                      (unlock?.Message ?? "no unlock result"));
        }

        private void RunGuildExpedition()
        {
            Require(_coordinator.AcceptGuildCityContract017D("CONTRACT_BELL_BENEATH_GATE"), "accept opening contract");
            Require(_coordinator.StartGuildCityExpedition017D(), "start opening expedition");
            var resolvedCheck = false;
            var encounterCount = 0;
            var preferredRoutes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "N00", "N01" }, { "N01", "N04" }, { "N04", "N06" },
                { "N06", "N13" }, { "N13", "N14" }
            };
            for (var guard = 0; guard < 48; guard++)
            {
                TryCreatorRoom();
                var expedition = _coordinator.GuildCity017D.Expedition;
                if (expedition == null) throw new InvalidOperationException("Guild expedition state disappeared.");
                if (expedition.CanFinalizeOperation)
                {
                    Require(_coordinator.FinalizeGuildCityOperation017D(), "finalize guild operation");
                    break;
                }
                if (expedition.CanCommitEncounter && !string.IsNullOrWhiteSpace(expedition.CurrentEncounterId))
                {
                    Require(_coordinator.CommitGuildCityEncounter017D(expedition.CurrentEncounterId), "commit guild encounter");
                    Require(_coordinator.StartCommittedGuildCityBattle017D(), "start guild encounter battle");
                    RunBattle("guild_expedition");
                    encounterCount++;
                    continue;
                }
                if (expedition.RequiresResolution && !expedition.ResolutionComplete)
                {
                    var recruits = _coordinator.State.Recruits.Select(value => value.RecruitId).ToArray();
                    Require(_coordinator.ResolveGuildCityCheck017D(expedition.CurrentEventId,
                        recruits[0], recruits[1], 0), "resolve guild event/check");
                    if (StringComparer.OrdinalIgnoreCase.Equals(expedition.CurrentNodeKind, "SKILL_CHECK")) resolvedCheck = true;
                    continue;
                }
                if (!expedition.CanMove) throw new InvalidOperationException("Guild expedition cannot progress at " + expedition.CurrentNodeId + ".");
                var destination = preferredRoutes.TryGetValue(expedition.CurrentNodeId, out var preferred) &&
                                  expedition.LinkedNodeIds.Contains(preferred)
                    ? preferred
                    : expedition.LinkedNodeIds.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(destination))
                    throw new InvalidOperationException("Guild expedition has no legal linked node.");
                Require(_coordinator.MoveGuildCityExpedition017D(destination), "move guild expedition");
            }
            var city = _coordinator.GuildCity017D;
            var contract = city.Contracts.First(value =>
                StringComparer.Ordinal.Equals(value.ContractId, "CONTRACT_BELL_BENEATH_GATE"));
            if (!contract.IsCompleted || !resolvedCheck || encounterCount != 3)
                throw new InvalidOperationException(
                    "The guided opening contract did not complete one visible check, three story battles, rewards, and physical return.");
            AddGate("guild_contract_event_check_camp", "Guild", Pass,
                "The first-hour contract covered one visible deterministic check, three story battles, the rescue objective, extraction, and finalization without menu detours.");
            AddGate("guild_battles_rewards_return", "Guild", Pass,
                "Hall Breach, Lantern Road Ambush, and Gate-Eater resolved through exact reward claims and saved return checkpoints.");
            if (_coordinator.CreatorAccess028.CompletedRooms > 0)
            {
                AddGate("creator_room", "Creator", Pass,
                    "The owner run entered and resolved an authored optional Creator Room exactly once during its World Gate route.");
                AddGate("creator_room_return", "Creator", contract.IsCompleted ? Pass : Blocked,
                    contract.IsCompleted
                        ? "After the optional room returned to its committed route, the owner run continued and later finalized the guided rescue."
                        : "The optional Creator Room resolved, but the guided rescue did not reach a terminal return.");
            }
            else
            {
                AddGate("creator_room", "Creator", Blocked,
                    "No authored optional Creator Room produced a completed-room receipt during the owner run.");
                AddGate("creator_room_return", "Creator", Blocked,
                    "No completed Creator Room receipt existed from which to prove return to its committed route.");
            }
        }

        private void RunProgression()
        {
            if (string.IsNullOrWhiteSpace(_primaryRewardItemId) ||
                string.IsNullOrWhiteSpace(_primaryRewardRecruitId))
                throw new InvalidOperationException("No exact-once M2 reward item was retained for progression proof.");
            var progressionAtStart = ProgressionState030(ReadSave().CampaignState);
            var retainedItemAtStart = progressionAtStart.EquipmentEvolution.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.ItemInstanceId, _primaryRewardItemId));
            var retainedUsesAtStart = retainedItemAtStart?.MeaningfulUses ?? 0;
            var retainedMasteryAtStart = retainedItemAtStart?.MasteryPoints ?? 0;
            var equipmentReceiptsAtStart = new HashSet<string>(
                progressionAtStart.AppliedReceiptIds.Where(value =>
                    value.StartsWith("EQUSE022_", StringComparison.Ordinal)),
                StringComparer.Ordinal);

            CompletePlayableChapter("CH018_002");
            var completedChapter = _coordinator.Campaign019.Chapters.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.ChapterId, "CH018_002"));
            if (completedChapter == null || !completedChapter.Completed)
                throw new InvalidOperationException("CH018_002 did not persist its earned material rewards.");

            var beforeAbyss = ReadSave().CampaignState;
            var terminalAbyssReceiptsBefore = ProgressionState030(beforeAbyss).AppliedReceiptIds.Count(value =>
                value.StartsWith("ABYSSREC022_", StringComparison.Ordinal));
            CampaignState beforeFirstFinalization = null;
            CampaignState afterFirstFinalization = null;
            for (var floor = 1; floor <= 10; floor++)
            {
                var operationId = "ABYSS_OP022_" + floor.ToString("00") + "_RECON";
                CompleteNonbattleAbyssOperation030(
                    operationId, out var beforeFinalization, out var afterFinalization);
                if (floor != 1) continue;
                beforeFirstFinalization = beforeFinalization;
                afterFirstFinalization = afterFinalization;
            }

            CompleteBattleAbyssOperation030("ABYSS_OP022_02_TRIAL", 3);
            var afterAbyss = ReadSave().CampaignState;
            var abyssState = ProgressionState030(afterAbyss);
            var terminalAbyssReceiptsAfter = abyssState.AppliedReceiptIds.Where(value =>
                    value.StartsWith("ABYSSREC022_", StringComparison.Ordinal))
                .ToArray();
            var allTenFloorsCleared = Enumerable.Range(1, 10).All(floor =>
                abyssState.AbyssFloors.Any(value =>
                    value.FloorId.StartsWith("ABYSS_FLOOR_" + floor.ToString("000") + "_", StringComparison.Ordinal) &&
                    value.ClearCount > 0));
            var firstFinalizationValid = EvaluateAbyssFinalizationEvidence030(
                beforeFirstFinalization, afterFirstFinalization, out var firstFinalizationEvidence);
            var abyssPassed = firstFinalizationValid && allTenFloorsCleared &&
                              terminalAbyssReceiptsAfter.Length == terminalAbyssReceiptsBefore + 12 &&
                              terminalAbyssReceiptsAfter.Distinct(StringComparer.Ordinal).Count() ==
                              terminalAbyssReceiptsAfter.Length &&
                              _coordinator.CampaignProgression022.GreatCovenantGateEarned;

            const string artifactBaseId = "INVOCATION_BASE022_02_01";
            var beforeArtifactCampaign = ReadSave().CampaignState;
            var beforeArtifactState = ProgressionState030(beforeArtifactCampaign);
            var beforeSkyhomeThree = MaterialAmount030(
                PlayableState030(beforeArtifactCampaign).WorldMaterials, "MAT020_SKYHOME_03");
            var beforeBunnyOne = MaterialAmount030(
                PlayableState030(beforeArtifactCampaign).WorldMaterials, "MAT020_BUNNY_01");
            Require(_coordinator.CraftInvocationArtifact022(artifactBaseId),
                "craft earned-material Invocation Artifact");
            var craftedCampaign = ReadSave().CampaignState;
            var craftedState = ProgressionState030(craftedCampaign);
            var newArtifacts = craftedState.InvocationArtifacts.Where(value =>
                    beforeArtifactState.InvocationArtifacts.All(previous =>
                        !StringComparer.Ordinal.Equals(previous.InstanceId, value.InstanceId)))
                .ToArray();
            if (newArtifacts.Length != 1)
                throw new InvalidOperationException("Invocation crafting did not create exactly one new artifact instance.");
            var artifactInstanceId = newArtifacts[0].InstanceId;
            var artifactCrafted = StringComparer.Ordinal.Equals(newArtifacts[0].BaseId, artifactBaseId) &&
                                  CountOwnedEquipmentInstances(craftedCampaign, artifactInstanceId) == 1 &&
                                  craftedCampaign.Guild.Inventory.Count(value =>
                                      StringComparer.Ordinal.Equals(value.InstanceId, artifactInstanceId)) == 1 &&
                                  CountAssignedEquipmentInstances(craftedCampaign, artifactInstanceId) == 0 &&
                                  MaterialAmount030(PlayableState030(craftedCampaign).WorldMaterials,
                                      "MAT020_SKYHOME_03") == beforeSkyhomeThree - 3 &&
                                  MaterialAmount030(PlayableState030(craftedCampaign).WorldMaterials,
                                      "MAT020_BUNNY_01") == beforeBunnyOne - 2;

            var artifactRecruit = craftedCampaign.Guild.Recruits
                .Where(value => value.AuthorityKind == RecruitAuthorityKind.Normal &&
                                (value.Equipment.Find(EquipmentSlotIds.ToolRelic) == null ||
                                 !value.Equipment.Find(EquipmentSlotIds.ToolRelic).Item.PlayerLocked) &&
                                craftedCampaign.Guild.Unions.Any(union =>
                                    union.MemberRecruitIds.Contains(value.RecruitId)))
                .OrderByDescending(value => value.MaximumMp)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (artifactRecruit == null)
                throw new InvalidOperationException("No deployed normal recruit can equip the Invocation Artifact.");
            Require(_coordinator.EquipItem(
                    artifactRecruit.RecruitId, EquipmentSlotIds.ToolRelic, artifactInstanceId),
                "manually equip Invocation Artifact");
            var equippedArtifactCampaign = ReadSave().CampaignState;
            artifactCrafted &= CountOwnedEquipmentInstances(equippedArtifactCampaign, artifactInstanceId) == 1 &&
                               CountAssignedEquipmentInstances(equippedArtifactCampaign, artifactInstanceId) == 1 &&
                               equippedArtifactCampaign.Guild.Inventory.All(value =>
                                   !StringComparer.Ordinal.Equals(value.InstanceId, artifactInstanceId));

            Require(_coordinator.StartTutorialBattle(), "start Echo forecast battle");
            var echoBattle = _coordinator.State.Battle;
            if (echoBattle == null) throw new InvalidOperationException("Echo battle did not start.");
            var artifactUnionId = echoBattle.PlayerUnions
                .Where(value => value.Members.Any(member =>
                    StringComparer.Ordinal.Equals(member.MemberId, artifactRecruit.RecruitId)))
                .Select(value => value.UnionId)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(artifactUnionId))
                throw new InvalidOperationException("Artifact recruit was not present in an active battle Union.");
            foreach (var union in echoBattle.PlayerUnions.Where(value => value.CanAct))
            {
                var choices = echoBattle.Forecasts.Where(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, union.UnionId)).ToArray();
                var choice = StringComparer.Ordinal.Equals(union.UnionId, artifactUnionId)
                    ? choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_GUARD"))
                    : ChooseSmokeForecast(choices);
                if (choice == null)
                    throw new InvalidOperationException("No complete Guard Forecast was available to the artifact Union.");
                Require(_coordinator.SelectForecast(union.UnionId, choice.ForecastId),
                    "select complete Echo-eligible Union Forecast");
                ObserveSelectedForecast(echoBattle, union, choice);
            }
            var beforeEchoCampaign = ReadSave().CampaignState;
            var beforeEchoState = ProgressionState030(beforeEchoCampaign);
            var beforeEchoReceipts = new HashSet<string>(beforeEchoState.AppliedReceiptIds,
                StringComparer.Ordinal);
            var beforeArtifactResonance = beforeEchoState.InvocationArtifacts.First(value =>
                StringComparer.Ordinal.Equals(value.InstanceId, artifactInstanceId)).Resonance;
            Require(_coordinator.InvokeEligibleEchoForecast022(),
                "invoke earned Echo through complete Union Forecast");
            var afterEchoCampaign = ReadSave().CampaignState;
            var afterEchoState = ProgressionState030(afterEchoCampaign);
            var newEchoReceipts = afterEchoState.AppliedReceiptIds.Where(value =>
                    value.StartsWith("ECHOREC022_", StringComparison.Ordinal) &&
                    !beforeEchoReceipts.Contains(value))
                .ToArray();
            var echoEvents = _coordinator.State.Battle?.Events.Where(value =>
                    StringComparer.Ordinal.Equals(value.EventType, "SUMMON_ECHO_INVOKED") &&
                    StringComparer.Ordinal.Equals(value.UnionId, artifactUnionId) &&
                    StringComparer.Ordinal.Equals(value.ActorUnionId, artifactUnionId) &&
                    StringComparer.Ordinal.Equals(value.MemberId, artifactRecruit.RecruitId) &&
                    StringComparer.Ordinal.Equals(value.ActorMemberId, artifactRecruit.RecruitId) &&
                    (value.ArtId ?? string.Empty).StartsWith("SUMMON_ECHO022_", StringComparison.Ordinal))
                .ToArray() ?? Array.Empty<M2BattleEventView>();
            ObserveBattle(_coordinator.State.Battle);
            var echoPassed = newEchoReceipts.Length == 1 &&
                             afterEchoState.AppliedReceiptIds.Count(value =>
                                 StringComparer.Ordinal.Equals(value, newEchoReceipts[0])) == 1 &&
                             echoEvents.Length == 1 &&
                             echoEvents[0].Amount > 0 &&
                             (echoEvents[0].Text ?? string.Empty).IndexOf(
                                 newEchoReceipts[0], StringComparison.Ordinal) >= 0 &&
                             afterEchoState.SummonResonance == beforeEchoState.SummonResonance + 1 &&
                             afterEchoState.InvocationArtifacts.First(value =>
                                 StringComparer.Ordinal.Equals(value.InstanceId, artifactInstanceId)).Resonance ==
                             beforeArtifactResonance + 1;
            RunBattle("echo");

            EquipmentEvolutionState022 earnedGrowth = null;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                earnedGrowth = ProgressionState030(ReadSave().CampaignState).EquipmentEvolution.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.ItemInstanceId, _primaryRewardItemId));
                if (earnedGrowth != null && earnedGrowth.MeaningfulUses >= 5 &&
                    earnedGrowth.MasteryPoints >= 120 &&
                    earnedGrowth.MeaningfulUses > retainedUsesAtStart &&
                    earnedGrowth.MasteryPoints > retainedMasteryAtStart &&
                    !equipmentReceiptsAtStart.Contains(earnedGrowth.HistoryTag)) break;
                Require(_coordinator.StartTutorialBattle(), "start equipment mastery battle");
                RunBattle("equipment_mastery");
            }
            var growthCampaign = ReadSave().CampaignState;
            var growthState = ProgressionState030(growthCampaign);
            earnedGrowth = growthState.EquipmentEvolution.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.ItemInstanceId, _primaryRewardItemId));
            var equipmentReceipts = growthState.AppliedReceiptIds.Where(value =>
                    value.StartsWith("EQUSE022_", StringComparison.Ordinal))
                .ToArray();
            var newEquipmentReceipts = equipmentReceipts.Where(value =>
                    !equipmentReceiptsAtStart.Contains(value))
                .ToArray();
            var growthPassed = earnedGrowth != null && earnedGrowth.MeaningfulUses >= 5 &&
                               earnedGrowth.MasteryPoints >= 120 &&
                               earnedGrowth.MeaningfulUses > retainedUsesAtStart &&
                               earnedGrowth.MasteryPoints > retainedMasteryAtStart &&
                               newEquipmentReceipts.Length > 0 &&
                               equipmentReceipts.Distinct(StringComparer.Ordinal).Count() ==
                               equipmentReceipts.Length &&
                               earnedGrowth.HistoryTag.StartsWith("EQUSE022_", StringComparison.Ordinal) &&
                               newEquipmentReceipts.Contains(earnedGrowth.HistoryTag,
                                   StringComparer.Ordinal) &&
                               growthState.AppliedReceiptIds.Count(value =>
                                   StringComparer.Ordinal.Equals(value, earnedGrowth.HistoryTag)) == 1 &&
                               CountOwnedEquipmentInstances(growthCampaign, _primaryRewardItemId) == 1;

            if (earnedGrowth == null ||
                !StringComparer.Ordinal.Equals(earnedGrowth.TrackId, "WTRACK022_01"))
                throw new InvalidOperationException(
                    "The retained First-Gate Sword did not resolve to WTRACK022_01.");
            const string recipeId = "WRECIPE022_01_01";
            var beforeEvolutionCampaign = ReadSave().CampaignState;
            var beforeEvolutionMaterials = PlayableState030(beforeEvolutionCampaign).WorldMaterials;
            var beforeEvolutionSkyhome = MaterialAmount030(beforeEvolutionMaterials, "MAT020_SKYHOME_02");
            var beforeEvolutionBeast = MaterialAmount030(beforeEvolutionMaterials, "MAT020_BEAST_02");
            Require(_coordinator.EvolveWeapon022(_primaryRewardItemId, recipeId),
                "evolve earned reward equipment");
            var evolvedCampaign = ReadSave().CampaignState;
            var evolvedHash = SecondDimension.Determinism.CanonicalJson.Sha256Hex(evolvedCampaign);
            Require(_coordinator.EvolveWeapon022(_primaryRewardItemId, recipeId),
                "repeat exact-once equipment evolution");
            var repeatedEvolutionCampaign = ReadSave().CampaignState;
            var evolvedItem = ProgressionState030(repeatedEvolutionCampaign).EquipmentEvolution.First(value =>
                StringComparer.Ordinal.Equals(value.ItemInstanceId, _primaryRewardItemId));
            var evolutionPassed = StringComparer.Ordinal.Equals(evolvedItem.TierId, "COMMON") &&
                                  evolvedItem.AppliedRecipeIds.Count(value =>
                                      StringComparer.Ordinal.Equals(value, recipeId)) == 1 &&
                                  CountOwnedEquipmentInstances(repeatedEvolutionCampaign, _primaryRewardItemId) == 1 &&
                                  MaterialAmount030(PlayableState030(repeatedEvolutionCampaign).WorldMaterials,
                                      "MAT020_SKYHOME_02") == beforeEvolutionSkyhome - 3 &&
                                  MaterialAmount030(PlayableState030(repeatedEvolutionCampaign).WorldMaterials,
                                      "MAT020_BEAST_02") == beforeEvolutionBeast - 1 &&
                                  StringComparer.Ordinal.Equals(evolvedHash,
                                      SecondDimension.Determinism.CanonicalJson.Sha256Hex(
                                          repeatedEvolutionCampaign));

            const string covenantId = "COVENANT022_AEGIS_FIRST_WALL";
            var beforeCovenant = ProgressionState030(ReadSave().CampaignState);
            var beforeTrialReceipts = beforeCovenant.AppliedReceiptIds.Count(value =>
                value.StartsWith("COVTRIALREC022_", StringComparison.Ordinal));
            var beforeAcceptanceReceipts = beforeCovenant.AppliedReceiptIds.Count(value =>
                value.StartsWith("COVACCEPTREC022_", StringComparison.Ordinal));
            for (var stage = 0; stage < 4; stage++)
                Require(_coordinator.AdvanceCovenant022(covenantId),
                    "advance canonical Covenant trial stage " + (stage + 1));
            var completedTrialHash = SecondDimension.Determinism.CanonicalJson.Sha256Hex(
                ReadSave().CampaignState);
            Require(_coordinator.AdvanceCovenant022(covenantId),
                "repeat completed Covenant trial stage exactly once");
            var repeatedTrialHash = SecondDimension.Determinism.CanonicalJson.Sha256Hex(
                ReadSave().CampaignState);
            Require(_coordinator.AcceptCovenant022(covenantId),
                "voluntarily accept completed Great Covenant");
            var acceptedCovenantHash = SecondDimension.Determinism.CanonicalJson.Sha256Hex(
                ReadSave().CampaignState);
            Require(_coordinator.AcceptCovenant022(covenantId),
                "repeat voluntary Covenant acceptance exactly once");
            var repeatedAcceptanceHash = SecondDimension.Determinism.CanonicalJson.Sha256Hex(
                ReadSave().CampaignState);
            const string battleCovenantId = "COVENANT022_WILDROAD_LEVIATHAN";
            for (var stage = 0; stage < 4; stage++)
                Require(_coordinator.AdvanceCovenant022(battleCovenantId),
                    "advance Wildroad Leviathan Covenant trial stage " + (stage + 1));
            Require(_coordinator.AcceptCovenant022(battleCovenantId),
                "voluntarily accept Wildroad Leviathan for battle proof");
            Require(_coordinator.StartTutorialBattle(), "start Great Covenant Forecast battle");
            var covenantBattle = _coordinator.State.Battle;
            if (covenantBattle == null) throw new InvalidOperationException("Great Covenant battle did not start.");
            foreach (var union in covenantBattle.PlayerUnions.Where(value => value.CanAct))
            {
                var choices = covenantBattle.Forecasts.Where(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, union.UnionId)).ToArray();
                var choice = choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_ALL_OUT"));
                if (choice == null) throw new InvalidOperationException("CMD_ALL_OUT was unavailable for a Great Covenant Union proof.");
                Require(_coordinator.SelectForecast(union.UnionId, choice.ForecastId),
                    "select complete CMD_ALL_OUT Great Covenant Union Forecast");
                ObserveSelectedForecast(covenantBattle, union, choice);
            }
            var beforeCovenantBattleState = ProgressionState030(ReadSave().CampaignState);
            var beforeCovenantBattleReceipts = new HashSet<string>(beforeCovenantBattleState.AppliedReceiptIds,StringComparer.Ordinal);
            Require(_coordinator.InvokeAcceptedCovenantForecast022(battleCovenantId),
                "invoke Wildroad Leviathan through CMD_ALL_OUT complete Forecast");
            var afterCovenantBattleState = ProgressionState030(ReadSave().CampaignState);
            var newCovenantBattleReceipts = afterCovenantBattleState.AppliedReceiptIds.Where(value =>
                    value.StartsWith("COVBATTLE022_",StringComparison.Ordinal)&&!beforeCovenantBattleReceipts.Contains(value))
                .ToArray();
            var covenantBattleEvents = _coordinator.State.Battle?.Events.Where(value =>
                    StringComparer.Ordinal.Equals(value.EventType,"GREAT_COVENANT_INVOKED")&&
                    StringComparer.Ordinal.Equals(value.ArtId,battleCovenantId)&&value.Amount>0)
                .ToArray() ?? Array.Empty<M2BattleEventView>();
            var covenantBattlePassed = newCovenantBattleReceipts.Length == 1 &&
                                       afterCovenantBattleState.AppliedReceiptIds.Count(value =>
                                           StringComparer.Ordinal.Equals(value,newCovenantBattleReceipts[0])) == 1 &&
                                       covenantBattleEvents.Length == 1 &&
                                       (covenantBattleEvents[0].Text??string.Empty).IndexOf(newCovenantBattleReceipts[0],StringComparison.Ordinal)>=0;
            ObserveBattle(_coordinator.State.Battle);
            RunBattle("great_covenant");
            var afterCovenant = ProgressionState030(ReadSave().CampaignState);
            var covenant = afterCovenant.Covenants.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.CovenantId, covenantId));
            var battleCovenant = afterCovenant.Covenants.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.CovenantId, battleCovenantId));
            var localTrialReceipts = covenant?.AppliedReceiptIds.Where(value =>
                    value.StartsWith("COVTRIALREC022_", StringComparison.Ordinal))
                .ToArray() ?? Array.Empty<string>();
            var localAcceptanceReceipts = covenant?.AppliedReceiptIds.Where(value =>
                    value.StartsWith("COVACCEPTREC022_", StringComparison.Ordinal))
                .ToArray() ?? Array.Empty<string>();
            var battleCovenantTrialReceipts = battleCovenant?.AppliedReceiptIds.Where(value =>
                    value.StartsWith("COVTRIALREC022_", StringComparison.Ordinal))
                .ToArray() ?? Array.Empty<string>();
            var battleCovenantAcceptanceReceipts = battleCovenant?.AppliedReceiptIds.Where(value =>
                    value.StartsWith("COVACCEPTREC022_", StringComparison.Ordinal))
                .ToArray() ?? Array.Empty<string>();
            var covenantPassed = covenant != null && covenant.Status == CovenantStatus022.Accepted &&
                                 covenant.TrialProgress == 100 && localTrialReceipts.Length == 4 &&
                                 localAcceptanceReceipts.Length == 1 &&
                                 battleCovenant != null && battleCovenant.Status == CovenantStatus022.Accepted &&
                                 battleCovenant.TrialProgress == 100 && battleCovenantTrialReceipts.Length == 4 &&
                                 battleCovenantAcceptanceReceipts.Length == 1 && covenantBattlePassed &&
                                 StringComparer.Ordinal.Equals(completedTrialHash, repeatedTrialHash) &&
                                 StringComparer.Ordinal.Equals(acceptedCovenantHash, repeatedAcceptanceHash) &&
                                 afterCovenant.AppliedReceiptIds.Count(value =>
                                     value.StartsWith("COVTRIALREC022_", StringComparison.Ordinal)) ==
                                 beforeTrialReceipts + 8 &&
                                 afterCovenant.AppliedReceiptIds.Count(value =>
                                     value.StartsWith("COVACCEPTREC022_", StringComparison.Ordinal)) ==
                                 beforeAcceptanceReceipts + 2 &&
                                 localTrialReceipts.Concat(localAcceptanceReceipts).Concat(battleCovenantTrialReceipts).Concat(battleCovenantAcceptanceReceipts).All(receipt =>
                                     afterCovenant.AppliedReceiptIds.Count(value =>
                                         StringComparer.Ordinal.Equals(value, receipt)) == 1);

            AddGate("learn_by_use_weapon_growth", "Progression", growthPassed ? Pass : Blocked,
                growthPassed
                    ? "Claimed immutable battle receipts bound " + earnedGrowth.MeaningfulUses +
                      " meaningful uses and " + earnedGrowth.MasteryPoints +
                      " mastery to the retained main-hand instance through exact EQUSE022 receipts."
                    : "No claimed battle receipt proved the required equipment-instance meaningful uses and mastery.");
            AddGate("equipment_evolution_or_advanced_class", "Progression", evolutionPassed ? Pass : Blocked,
                evolutionPassed
                    ? "Earned battle history and cross-world materials evolved the same owned instance through " +
                      recipeId + "; repeat application left the canonical state unchanged."
                    : "The retained equipment instance did not preserve identity, exact material deltas, or its one recipe receipt.");
            AddGate("endless_abyss", "Progression", abyssPassed ? Pass : Blocked,
                abyssPassed
                    ? firstFinalizationEvidence + " All ten authored floors then cleared in order, and the floor-2 Trial used the certified battle bridge."
                    : "The ten-floor terminal receipts, floor-2 certified Trial, or Great-Covenant floor-10 gate were incomplete. " +
                      firstFinalizationEvidence);
            AddGate("invocation_artifact", "Progression", artifactCrafted ? Pass : Blocked,
                artifactCrafted
                    ? "Earned materials crafted exactly one Invocation Artifact into inventory without auto-equip; the same instance was then manually equipped in Tool/Relic."
                    : "Invocation crafting, exact material costs, inventory ownership, or manual Tool/Relic equipment was not proven.");
            AddGate("summon_echo", "Progression", echoPassed ? Pass : Blocked,
                echoPassed
                    ? "An eligible Echo resolved only through the selected complete Union Forecast, emitted one canonical event, and advanced artifact plus summon resonance exactly once."
                    : "The complete-Forecast Echo event, receipt, or exact resonance deltas were incomplete.");
            AddGate("great_covenant", "Progression", covenantPassed ? Pass : Blocked,
                covenantPassed
                    ? "The floor-10 terminal gate authorized exact four-stage voluntary acceptance chains; accepted Wildroad Leviathan then answered CMD_ALL_OUT through a complete Union Forecast with one positive combat event and one exact COVBATTLE022 receipt."
                    : "The earned Covenants did not preserve their voluntary receipt chains or Wildroad Leviathan's positive complete-Forecast battle proof.");
        }

        private void CompleteNonbattleAbyssOperation030(
            string operationId, out CampaignState beforeFinalization, out CampaignState afterFinalization)
        {
            Require(_coordinator.BeginAbyssOperation022(operationId), "begin " + operationId);
            for (var guard = 0; guard < 16; guard++)
            {
                var active = ProgressionState030(ReadSave().CampaignState).ActiveAbyssOperation;
                if (active == null)
                    throw new InvalidOperationException(operationId + " lost its active state before finalization.");
                if (active.Status == AbyssOperationStatus022.ReadyToFinalize) break;
                if (active.Status == AbyssOperationStatus022.AwaitingBattle)
                    throw new InvalidOperationException(operationId + " unexpectedly required a battle.");
                Require(_coordinator.CommitAbyssStep022(), "commit " + operationId + " step");
                Require(_coordinator.ApplyAbyssStep022(), "apply " + operationId + " step");
            }
            beforeFinalization = ReadSave().CampaignState;
            Require(_coordinator.FinalizeAbyssOperation022(), "finalize " + operationId);
            afterFinalization = ReadSave().CampaignState;
        }

        private void CompleteBattleAbyssOperation030(string operationId, int battleStepIndex)
        {
            Require(_coordinator.BeginAbyssOperation022(operationId), "begin " + operationId);
            for (var guard = 0; guard < 16; guard++)
            {
                var active = ProgressionState030(ReadSave().CampaignState).ActiveAbyssOperation;
                if (active == null)
                    throw new InvalidOperationException(operationId + " lost its active state before finalization.");
                if (active.Status == AbyssOperationStatus022.ReadyToFinalize)
                {
                    Require(_coordinator.FinalizeAbyssOperation022(), "finalize " + operationId);
                    return;
                }
                if (active.CurrentStepIndex == battleStepIndex)
                {
                    Require(_coordinator.EnterAbyssBattle022(), "enter " + operationId + " certified battle");
                    RunBattle("abyss_trial");
                    Require(_coordinator.CommitAbyssBattleResult022(),
                        "commit " + operationId + " claimed battle result");
                    Require(_coordinator.FinalizeAbyssBattle022(),
                        "apply " + operationId + " certified battle receipt");
                    continue;
                }
                Require(_coordinator.CommitAbyssStep022(), "commit " + operationId + " step");
                Require(_coordinator.ApplyAbyssStep022(), "apply " + operationId + " step");
            }
            throw new InvalidOperationException(operationId + " exceeded the bounded operation guard.");
        }

        private void RunBattle(string source)
        {
            for (var guard = 0; guard < 96; guard++)
            {
                var battle = _coordinator.State.Battle;
                if (battle == null) throw new InvalidOperationException(source + " battle did not start.");
                ObserveBattle(battle);
                if (battle.IsResolved)
                {
                    if (battle.Reward == null || !battle.Reward.CanClaim)
                        throw new InvalidOperationException(source + " battle resolved without a claimable reward.");
                    var rewardId = battle.Reward.RewardId;
                    var pendingCampaign = ReadSave().CampaignState;
                    var equipmentReward = pendingCampaign.Battle?.Reward?.EquipmentReward;
                    var rewardOccurrencesBefore = pendingCampaign.Guild.Development.ClaimedBattleRewardIds
                        .Count(value => StringComparer.Ordinal.Equals(value, rewardId));
                    var ownedBefore = equipmentReward == null
                        ? 0
                        : CountOwnedEquipmentInstances(pendingCampaign, equipmentReward.InstanceId);
                    Require(_coordinator.ClaimBattleRewards(), "claim " + source + " reward");
                    var claimedCampaign = ReadSave().CampaignState;
                    var afterClaim = claimedCampaign.Guild.Development.ClaimedBattleRewardIds;
                    _allRewardsExactOnce &= _claimedRewardIds.Add(rewardId) &&
                                            afterClaim.Count(value =>
                                                StringComparer.Ordinal.Equals(value, rewardId)) ==
                                            rewardOccurrencesBefore + 1;
                    _allRewardEquipmentExactOnce &= equipmentReward != null &&
                                                    ownedBefore == 0 &&
                                                    claimedCampaign.Guild.Inventory.Count(value =>
                                                        StringComparer.Ordinal.Equals(
                                                            value.InstanceId, equipmentReward.InstanceId)) == 1 &&
                                                    CountAssignedEquipmentInstances(
                                                        claimedCampaign, equipmentReward.InstanceId) == 0;
                    var claimedStateHash = SecondDimension.Determinism.CanonicalJson.Sha256Hex(claimedCampaign);
                    Require(_coordinator.ClaimBattleRewards(), "repeat exact-once " + source + " reward claim");
                    var repeatedCampaign = ReadSave().CampaignState;
                    var afterRepeat = repeatedCampaign.Guild.Development.ClaimedBattleRewardIds;
                    _allRewardsExactOnce &= afterRepeat.Count == afterClaim.Count &&
                                            afterRepeat.Count(value => StringComparer.Ordinal.Equals(value, rewardId)) ==
                                            rewardOccurrencesBefore + 1 &&
                                            StringComparer.Ordinal.Equals(
                                                claimedStateHash,
                                                SecondDimension.Determinism.CanonicalJson.Sha256Hex(repeatedCampaign));
                    if (equipmentReward != null)
                    {
                        _allRewardEquipmentExactOnce &=
                            CountOwnedEquipmentInstances(repeatedCampaign, equipmentReward.InstanceId) == 1 &&
                            repeatedCampaign.Guild.Inventory.Count(value =>
                                StringComparer.Ordinal.Equals(value.InstanceId, equipmentReward.InstanceId)) == 1 &&
                            CountAssignedEquipmentInstances(repeatedCampaign, equipmentReward.InstanceId) == 0;
                        var slotId = equipmentReward.ValidSlotIds.FirstOrDefault(EquipmentSlotIds.IsOpeningSlot);
                        var battleParticipantIds = new HashSet<string>(battle.PlayerUnions
                            .SelectMany(value => value.Members)
                            .Select(value => value.MemberId), StringComparer.Ordinal);
                        var recruit = string.IsNullOrWhiteSpace(_primaryRewardItemId)
                            ? repeatedCampaign.Guild.Recruits.FirstOrDefault(value =>
                                value.AuthorityKind == RecruitAuthorityKind.Normal &&
                                battleParticipantIds.Contains(value.RecruitId))
                            : repeatedCampaign.Guild.Recruits.FirstOrDefault(value =>
                                value.AuthorityKind == RecruitAuthorityKind.Normal &&
                                battleParticipantIds.Contains(value.RecruitId) &&
                                !StringComparer.Ordinal.Equals(value.RecruitId, _primaryRewardRecruitId));
                        if (recruit == null || string.IsNullOrWhiteSpace(slotId))
                            throw new InvalidOperationException(source + " reward item has no legal normal recruit/slot target.");
                        var previousItemId = recruit.Equipment.Find(slotId)?.Item?.InstanceId ?? string.Empty;
                        Require(_coordinator.EquipItem(recruit.RecruitId, slotId, equipmentReward.InstanceId),
                            "manually equip " + source + " reward item");
                        var equippedCampaign = ReadSave().CampaignState;
                        _allRewardEquipmentExactOnce &=
                            CountOwnedEquipmentInstances(equippedCampaign, equipmentReward.InstanceId) == 1 &&
                            equippedCampaign.Guild.Inventory.All(value =>
                                !StringComparer.Ordinal.Equals(value.InstanceId, equipmentReward.InstanceId)) &&
                            CountAssignedEquipmentInstances(equippedCampaign, equipmentReward.InstanceId) == 1 &&
                            _manuallyEquippedRewardItemIds.Add(equipmentReward.InstanceId);
                        if (string.IsNullOrWhiteSpace(_primaryRewardItemId))
                        {
                            _primaryRewardItemId = equipmentReward.InstanceId;
                            _primaryRewardRecruitId = recruit.RecruitId;
                        }
                        else
                        {
                            Require(_coordinator.UnequipItem(recruit.RecruitId, slotId),
                                "return manually inspected " + source + " reward to inventory");
                            if (!string.IsNullOrWhiteSpace(previousItemId))
                                Require(_coordinator.EquipItem(recruit.RecruitId, slotId, previousItemId),
                                    "restore prior manual equipment after reward inspection");
                            var restored = ReadSave().CampaignState;
                            _allRewardEquipmentExactOnce &=
                                CountOwnedEquipmentInstances(restored, equipmentReward.InstanceId) == 1 &&
                                CountAssignedEquipmentInstances(restored, _primaryRewardItemId) == 1;
                        }
                    }
                    _battleCount++;
                    return;
                }
                foreach (var union in battle.PlayerUnions.Where(value => value.CanAct))
                {
                    var choices = battle.Forecasts.Where(value =>
                        StringComparer.Ordinal.Equals(value.UnionId, union.UnionId)).ToArray();
                    if (choices.Length == 0) throw new InvalidOperationException("No complete Forecast for " + union.UnionId + ".");
                    var choice = ChooseSmokeForecast(choices);
                    Require(_coordinator.SelectForecast(union.UnionId, choice.ForecastId), "select complete Union Forecast");
                    ObserveSelectedForecast(battle, union, choice);
                }
                Require(_coordinator.ConfirmBattleRound(), "confirm complete Union battle round");
            }
            throw new InvalidOperationException(source + " battle exceeded the round guard.");
        }

        private void ObserveBattle(M2BattleView battle)
        {
            if (battle.PlayerUnions.Count > 10 || battle.EnemyUnions.Count > 10)
                throw new InvalidOperationException("Battle exceeded 10 allied or 10 enemy Unions.");
            _maximumAlliedUnionsObserved = Math.Max(_maximumAlliedUnionsObserved, battle.PlayerUnions.Count);
            _maximumEnemyUnionsObserved = Math.Max(_maximumEnemyUnionsObserved, battle.EnemyUnions.Count);
            foreach (var forecast in battle.Forecasts)
            {
                ObserveForecast(forecast);
                var union = battle.PlayerUnions.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, forecast.UnionId));
                if (union == null) continue;
                _completeForecastsObserved = true;
                _completeForecastsValid &= CompleteForecastMatchesUnion030(forecast, union);
            }
            foreach (var battleEvent in battle.Events)
            {
                if (StringComparer.Ordinal.Equals(battleEvent.EventType, "LINK_ART_TRIGGERED")) _linkArtTriggered = true;
                if (StringComparer.Ordinal.Equals(battleEvent.EventType, "POSITION_SHIFT"))
                    _positionalRelationshipObserved = true;
                if ((battleEvent.Text ?? string.Empty).IndexOf("deadlock", StringComparison.OrdinalIgnoreCase) >= 0)
                    _deadlockObserved = true;
                if (!IsExecutedMemberActionEvent030(battleEvent.EventType)) continue;
                var key = SelectedActionKey030(
                    battle.BattleId, battleEvent.Round, battleEvent.ActorMemberId, battleEvent.ArtId);
                if (!_selectedActionEvidence.TryGetValue(key, out var selected)) continue;
                if (!string.IsNullOrWhiteSpace(selected.AnimationTag))
                    _animationTags.Add(selected.AnimationTag);
                if (!string.IsNullOrWhiteSpace(selected.Discipline))
                {
                    _disciplines.Add(selected.Discipline);
                    if (StringComparer.OrdinalIgnoreCase.Equals(selected.Discipline, "Martial") ||
                        StringComparer.OrdinalIgnoreCase.Equals(selected.Discipline, "Tactical"))
                        _disciplines.Add("Combat");
                }
                if (!string.IsNullOrWhiteSpace(selected.WeaponFamily))
                    _weaponFamilies.Add(selected.WeaponFamily);
                if (!string.IsNullOrWhiteSpace(selected.MysticSchool))
                    _mysticSchools.Add(selected.MysticSchool);
                if (StringComparer.OrdinalIgnoreCase.Equals(selected.ProfileSchool, "WARDING"))
                    _disciplines.Add("Warding");
            }
        }

        private void ObserveForecast(M2ForecastView forecast)
        {
            if (forecast == null) return;
            if (IsLinkArtForecast(forecast)) _linkArtForecastSeen = true;
            if (forecast.MemberActions != null && forecast.MemberActions.Count > 0)
                _predictedMemberActionsObserved = true;
        }

        private static bool CompleteForecastMatchesUnion030(
            M2ForecastView forecast, M2BattleUnionView union)
        {
            if (forecast?.MemberActions == null || union?.Members == null ||
                forecast.MemberActions.Count != union.Members.Count) return false;
            var expected = new HashSet<string>(
                union.Members.Where(value => value != null).Select(value => value.MemberId),
                StringComparer.Ordinal);
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in forecast.MemberActions)
            {
                if (action == null || string.IsNullOrWhiteSpace(action.ActorMemberId) ||
                    !actual.Add(action.ActorMemberId)) return false;
            }
            return expected.Count == union.Members.Count && expected.SetEquals(actual);
        }

        private M2ForecastView ChooseSmokeForecast(IReadOnlyList<M2ForecastView> choices)
        {
            return (!_linkArtTriggered ? choices.FirstOrDefault(IsLinkArtForecast) : null) ??
                   (!_positionalRelationshipObserved
                       ? choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_FLANK"))
                       : null) ??
                   (!_disciplines.Contains("Restoration")
                       ? choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_HEAL"))
                       : null) ??
                   (!_disciplines.Contains("Guard")
                       ? choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_GUARD"))
                       : null) ??
                   (_mysticSchools.Count < 2
                       ? choices.FirstOrDefault(value =>
                           StringComparer.Ordinal.Equals(value.CommandId, "CMD_MYSTIC") &&
                           ForecastHasUnseenOffensiveMysticSchool030(value))
                       : null) ??
                   (_mysticSchools.Count < 2
                       ? choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_MYSTIC"))
                       : null) ??
                   choices.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_ALL_OUT")) ??
                   choices[0];
        }

        private bool ForecastHasUnseenOffensiveMysticSchool030(M2ForecastView forecast)
        {
            foreach (var action in forecast?.MemberActions ?? Array.Empty<M2PredictedActionView>())
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(action.Discipline, "Mystic")) continue;
                var profile = ExactBattleArtProfile(action.ArtId);
                if (profile == null || string.IsNullOrWhiteSpace(profile.schoolId) ||
                    StringComparer.OrdinalIgnoreCase.Equals(profile.schoolId, "RESTORATION") ||
                    StringComparer.OrdinalIgnoreCase.Equals(profile.schoolId, "WARDING")) continue;
                if (!_mysticSchools.Contains(profile.schoolId)) return true;
            }
            return false;
        }

        private void ObserveSelectedForecast(
            M2BattleView battle, M2BattleUnionView union, M2ForecastView forecast)
        {
            foreach (var action in forecast.MemberActions ?? Array.Empty<M2PredictedActionView>())
            {
                var member = union.Members.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.MemberId, action.ActorMemberId));
                var profile = ExactBattleArtProfile(action.ArtId);
                var mysticSchool = StringComparer.OrdinalIgnoreCase.Equals(action.Discipline, "Mystic") &&
                                   !string.IsNullOrWhiteSpace(profile?.schoolId) &&
                                   !StringComparer.OrdinalIgnoreCase.Equals(profile.schoolId, "RESTORATION") &&
                                   !StringComparer.OrdinalIgnoreCase.Equals(profile.schoolId, "WARDING")
                    ? profile.schoolId
                    : string.Empty;
                var key = SelectedActionKey030(
                    battle.BattleId, battle.Round, action.ActorMemberId, action.ArtId);
                _selectedActionEvidence[key] = new SelectedActionEvidence030
                {
                    AnimationTag = action.AnimationTag ?? string.Empty,
                    Discipline = action.Discipline ?? string.Empty,
                    WeaponFamily = member?.EquipmentVisualFamily ?? string.Empty,
                    MysticSchool = mysticSchool,
                    ProfileSchool = profile?.schoolId ?? string.Empty
                };
            }
        }

        private static string SelectedActionKey030(
            string battleId, int round, string actorMemberId, string artId)
        {
            return string.Join("|", new[]
            {
                battleId ?? string.Empty,
                round.ToString(),
                actorMemberId ?? string.Empty,
                artId ?? string.Empty
            });
        }

        private static bool IsExecutedMemberActionEvent030(string eventType)
        {
            return StringComparer.Ordinal.Equals(eventType, "MARTIAL_HIT") ||
                   StringComparer.Ordinal.Equals(eventType, "MYSTIC_HIT") ||
                   StringComparer.Ordinal.Equals(eventType, "TACTICAL_HIT") ||
                   StringComparer.Ordinal.Equals(eventType, "RESTORATION") ||
                   StringComparer.Ordinal.Equals(eventType, "STABILIZED") ||
                   StringComparer.Ordinal.Equals(eventType, "GUARD") ||
                   StringComparer.Ordinal.Equals(eventType, "RECOVERY");
        }

        private static BattleArtProfile011 ExactBattleArtProfile(string artId)
        {
            if (string.IsNullOrWhiteSpace(artId)) return null;
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            var key = artId.Trim();
            var profile = (manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>()).FirstOrDefault(value =>
                value != null && StringComparer.OrdinalIgnoreCase.Equals(value.artId, key));
            if (profile != null) return profile;
            var alias = (manifest.legacyAliases ?? Array.Empty<BattleArtAlias011>()).FirstOrDefault(value =>
                value != null && StringComparer.OrdinalIgnoreCase.Equals(value.aliasArtId, key));
            if (alias == null) return null;
            return (manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>()).FirstOrDefault(value =>
                value != null && StringComparer.OrdinalIgnoreCase.Equals(value.artId, alias.runtimeArtId));
        }

        private static bool IsLinkArtForecast(M2ForecastView forecast)
        {
            return forecast != null && !string.IsNullOrWhiteSpace(forecast.LearningOpportunity) &&
                   forecast.LearningOpportunity.IndexOf("LINK_ART_ID=", StringComparison.Ordinal) >= 0;
        }

        private void SummarizeCombatCoverage()
        {
            if (_battleCount == 0) throw new InvalidOperationException("No certified built-player battle completed.");
            var cinematicPassed = _battleCount > 0 && _animationTags.Count > 0;
            AddGate("cinematic_union_combat", "Battle", cinematicPassed ? Pass : Blocked,
                cinematicPassed
                    ? _battleCount + " certified Union battles emitted executed member-action events bound to " +
                      _animationTags.Count + " authored animation tags."
                    : "No executed member-action event could be bound to an authored animation tag.");
            var capacity = FinalReleaseReadinessService023.BuildSnapshot();
            var capacityPassed = capacity.IsAvailable && capacity.AlliedUnionCapacity == 10 &&
                                 capacity.EnemyUnionCapacity == 10 &&
                                 _maximumAlliedUnionsObserved <= capacity.AlliedUnionCapacity &&
                                 _maximumEnemyUnionsObserved <= capacity.EnemyUnionCapacity;
            AddGate("union_capacity_10v10", "Battle", capacityPassed ? Pass : Blocked,
                capacityPassed
                    ? "Release authority accepted ten allied and ten enemy Union positions; executed battles remained within capacity (observed maximum " +
                      _maximumAlliedUnionsObserved + " allied / " + _maximumEnemyUnionsObserved + " enemy)."
                    : "The Release 023 authority did not expose a valid 10/10 Union capacity or an executed battle exceeded it: " +
                      (capacity.Error ?? string.Empty));
            AddGate("complete_union_forecasts", "Battle",
                _completeForecastsObserved && _completeForecastsValid ? Pass : Blocked,
                _completeForecastsObserved && _completeForecastsValid
                    ? "Every observed player Forecast contained one predicted action for every Union member."
                    : "At least one observed Forecast was missing a complete member-action projection.");
            var artLaw = BattleArtRuntimeRegistry011.Manifest.law;
            var predictedActionsNonClickable = _predictedMemberActionsObserved && artLaw != null &&
                                               artLaw.playerSelectsCompleteUnionForecast &&
                                               !artLaw.memberActionClickable &&
                                               (BattleArtRuntimeRegistry011.Manifest.runtimeArtProfiles ??
                                                Array.Empty<BattleArtProfile011>()).All(value =>
                                                   value != null && !value.memberActionClickable &&
                                                   !value.playerDirectlySelectableInStandard);
            AddGate("predicted_member_arts_non_clickable", "Battle",
                predictedActionsNonClickable ? Pass : Blocked,
                predictedActionsNonClickable
                    ? "Predicted member Arts were present while runtime law exposed only whole-Forecast selection."
                    : "Predicted actions or the non-clickable complete-Forecast runtime law were not observed.");
            AddOrdinalCoverageGates("weapon_family", _weaponFamilies, "weapon families");
            AddOrdinalCoverageGates("offensive_mystic_school", _mysticSchools, "offensive Mystic schools");
            foreach (var discipline in new[] { "Combat", "Mystic", "Restoration", "Guard", "Warding" })
            {
                AddGate("discipline_" + discipline.ToLowerInvariant(), "Battle",
                    _disciplines.Contains(discipline) ? Pass : Blocked,
                    _disciplines.Contains(discipline)
                        ? discipline + " appeared in complete member-action Forecasts."
                        : discipline + " did not appear in the deterministic smoke battle Forecasts.");
            }
            AddGate("deadlock_relationship", "Battle", _deadlockObserved ? Pass : Blocked,
                _deadlockObserved
                    ? "A committed positional action emitted the authored deadlock relationship text."
                    : "No executed battle event demonstrated Deadlock.");
            AddGate("positional_relationship", "Battle", _positionalRelationshipObserved ? Pass : Blocked,
                _positionalRelationshipObserved
                    ? "An executed Forecast emitted POSITION_SHIFT and side-strike positioning."
                    : "No executed Forecast emitted a positional relationship event.");
            AddGate("exact_once_battle_reward", "Reward",
                _battleCount > 0 && _allRewardsExactOnce && _claimedRewardIds.Count == _battleCount ? Pass : Blocked,
                _battleCount > 0 && _allRewardsExactOnce && _claimedRewardIds.Count == _battleCount
                    ? "Every reward ID advanced the ledger once; a repeated claim left the ledger unchanged."
                    : "Reward receipt counts or duplicate-claim behavior did not prove exact-once application.");
            var rewardEquipmentPassed = _battleCount > 0 && _allRewardEquipmentExactOnce &&
                                        _manuallyEquippedRewardItemIds.Count == _battleCount;
            AddGate("reward_equipment_manual_equip", "Reward", rewardEquipmentPassed ? Pass : Blocked,
                rewardEquipmentPassed
                    ? "Every immutable M2 reward added one deterministic item to inventory without auto-equip; repeat claim did not duplicate it, and the same instance was manually equipped through the coordinator."
                    : "A deterministic M2 reward item was missing, duplicated, auto-equipped, or not manually equipped through the coordinator.");
            AddGate("link_art", "People", _linkArtForecastSeen && _linkArtTriggered ? Pass : Blocked,
                _linkArtForecastSeen && _linkArtTriggered
                    ? "An earned complete Link Art Forecast was selected and emitted LINK_ART_TRIGGERED."
                    : "No organic tier-2 intra-Union bond was earned in the short smoke path; Link Art was not fabricated.");
        }

        private void AddOrdinalCoverageGates(string idPrefix, HashSet<string> values, string label)
        {
            var ordered = values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            for (var index = 0; index < 2; index++)
            {
                var present = index < ordered.Length;
                AddGate(idPrefix + "_" + (index == 0 ? "one" : "two"), "Battle",
                    present ? Pass : Blocked,
                    present ? "Executed " + label + " included " + ordered[index] + "."
                        : "Fewer than two distinct executed " + label + " were observed.");
            }
        }

        private void TryCreatorRoom()
        {
            var creator = _coordinator.CreatorAccess028;
            if (!string.IsNullOrWhiteSpace(creator.ActiveRoomVisitId))
            {
                Require(_coordinator.ResolveCreatorRoom028(), "resolve Creator Room");
                return;
            }
            if (creator.CurrentRoom != null && creator.CurrentRoom.KeyUnlocked)
            {
                Require(_coordinator.EnterCreatorRoom028(), "enter Creator Room");
                Require(_coordinator.ResolveCreatorRoom028(), "resolve Creator Room");
            }
        }

        private void CaptureFinalSaveIdentity()
        {
            var loaded = ReadSave();
            _report.saveFormatVersion = loaded.SaveFormatVersion;
            _report.canonicalStateHash = loaded.CanonicalStateHash;
            _report.expectedCanonicalStateHash = loaded.CanonicalStateHash;
            _report.stateIdentityMatched = StringComparer.Ordinal.Equals(
                loaded.CanonicalStateHash, _coordinator.State.CanonicalStateHash);
            if (loaded.SaveFormatVersion != SaveEnvelopeV1.CurrentFormatVersion)
                throw new InvalidOperationException("Final owner-review save is not format v11.");
            if (!_report.stateIdentityMatched)
                throw new InvalidOperationException("Coordinator and save-envelope canonical hashes differ.");
            _report.state = Snapshot(loaded.CampaignState);
            AddGate("save_format_11_identity", "Save", Pass,
                "Atomic save v11 reloaded with the same canonical state hash.");
        }

        private SaveEnvelopeV1 ReadSave()
        {
            var loaded = _saveStore.ReadWithRecovery(_options.SavePath);
            if (!loaded.IsSuccess) throw new InvalidOperationException(string.Join("\n", loaded.Errors));
            return loaded.Value;
        }

        public static bool EvaluateAbyssFinalizationEvidence030(
            CampaignState before, CampaignState after, out string detail)
        {
            const string floorId = "ABYSS_FLOOR_001_MUD_TRENCHES";
            const string aboveGroundChangeId = "CITY_CHANGE_TRENCH_DRAINAGE";
            const string skyhomeMaterialId = "MAT020_SKYHOME_04";
            const string bunnyMaterialId = "MAT020_BUNNY_03";

            var beforePlayable = before?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020;
            var afterPlayable = after?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020;
            var beforeProgression = beforePlayable?.Progression022;
            var afterProgression = afterPlayable?.Progression022;
            if (beforeProgression == null || afterProgression == null || before?.Guild == null ||
                after?.Guild == null)
            {
                detail = "Abyss evidence was unavailable in the persisted Campaign 022 state.";
                return false;
            }

            var beforeFloor = beforeProgression.AbyssFloors.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.FloorId, floorId));
            var afterFloor = afterProgression.AbyssFloors.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.FloorId, floorId));
            var beforeReceipts = new HashSet<string>(beforeProgression.AppliedReceiptIds,
                StringComparer.Ordinal);
            var newReceipts = afterProgression.AppliedReceiptIds.Where(value =>
                    value.StartsWith("ABYSSREC022_", StringComparison.Ordinal) &&
                    !beforeReceipts.Contains(value))
                .ToArray();
            var receiptId = newReceipts.Length == 1 ? newReceipts[0] : string.Empty;
            var beforeSkyhome = MaterialAmount030(beforePlayable.WorldMaterials, skyhomeMaterialId);
            var afterSkyhome = MaterialAmount030(afterPlayable.WorldMaterials, skyhomeMaterialId);
            var beforeBunny = MaterialAmount030(beforePlayable.WorldMaterials, bunnyMaterialId);
            var afterBunny = MaterialAmount030(afterPlayable.WorldMaterials, bunnyMaterialId);
            var beforeClearCount = beforeFloor?.ClearCount ?? 0;

            var valid = beforeProgression.ActiveAbyssOperation != null &&
                        StringComparer.Ordinal.Equals(
                            beforeProgression.ActiveAbyssOperation.OperationDefinitionId,
                            "ABYSS_OP022_01_RECON") &&
                        StringComparer.Ordinal.Equals(
                            beforeProgression.ActiveAbyssOperation.FloorId, floorId) &&
                        beforeProgression.ActiveAbyssOperation.Status ==
                        AbyssOperationStatus022.ReadyToFinalize &&
                        afterProgression.ActiveAbyssOperation == null &&
                        afterFloor != null &&
                        afterFloor.ClearCount == beforeClearCount + 1 &&
                        afterFloor.FirstClearApplied &&
                        afterProgression.AboveGroundChangeIds.Contains(aboveGroundChangeId) &&
                        newReceipts.Length == 1 &&
                        afterFloor.AppliedReceiptIds.Count(value =>
                            StringComparer.Ordinal.Equals(value, receiptId)) == 1 &&
                        afterProgression.AppliedReceiptIds.Count(value =>
                            StringComparer.Ordinal.Equals(value, receiptId)) == 1 &&
                        after.Guild.Development.ClaimedBattleRewardIds.Count(value =>
                            StringComparer.Ordinal.Equals(value, receiptId)) == 1 &&
                        after.Guild.TreasuryXp == before.Guild.TreasuryXp + 25 &&
                        after.Guild.Development.LifetimeTreasuryXpEarned ==
                        before.Guild.Development.LifetimeTreasuryXpEarned + 25 &&
                        after.Guild.Development.HallEnhancementXp ==
                        before.Guild.Development.HallEnhancementXp + 19 &&
                        afterSkyhome == beforeSkyhome + 4 &&
                        afterBunny == beforeBunny + 4;

            detail = valid
                ? "The nonbattle Recon authority finalized one canonical Abyss receipt, cleared Mud Trenches once, applied trench drainage, awarded Guild/Hall XP, and added both authored materials by exactly four."
                : "Persisted Abyss finalization evidence was incomplete or did not match the authored exact-once floor, receipt, city-change, XP, and material deltas.";
            return valid;
        }

        private static int MaterialAmount030(
            IReadOnlyList<WorldMaterialAmount020> materials, string materialId)
        {
            return (materials ?? Array.Empty<WorldMaterialAmount020>())
                .Where(value => StringComparer.Ordinal.Equals(value.MaterialId, materialId))
                .Sum(value => value.Amount);
        }

        private static CampaignPlayableState020 PlayableState030(CampaignState campaign)
        {
            var playable = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020;
            if (playable == null)
                throw new InvalidOperationException("Campaign 020 playable state is unavailable.");
            return playable;
        }

        private static CampaignProgressionState022 ProgressionState030(CampaignState campaign)
        {
            var progression = PlayableState030(campaign).Progression022;
            if (progression == null)
                throw new InvalidOperationException("Campaign 022 progression state is unavailable.");
            return progression;
        }

        private static OwnerReviewStateEvidence030 Snapshot(CampaignState campaign)
        {
            var city = campaign?.Guild?.GuildCity;
            var strategic = city?.Strategic017H;
            var progress = strategic?.Campaign019;
            var playable = progress?.Playable020;
            var people = playable?.RecruitChronicles025;
            var creator = playable?.CreatorAccess028;
            var progression = playable?.Progression022;
            return new OwnerReviewStateEvidence030
            {
                guildmasterName = campaign?.Profile?.GuildmasterName ?? string.Empty,
                signedRecruitCount = campaign?.Guild?.Recruits?.Count ?? 0,
                permanentRecruitCount = campaign?.Guild?.Recruits?.Count(value =>
                    value.AuthorityKind == RecruitAuthorityKind.Normal) ?? 0,
                normalUnionCount = campaign?.Guild?.Unions?.Count(value => value.Kind == UnionKind.Normal) ?? 0,
                placedBuildingCount = city?.CityPlots?.Count(value => !string.IsNullOrWhiteSpace(value.BuildingId)) ?? 0,
                staffedBuildingCount = city?.CityPlots?.Count(value => value.StaffRecruitIds != null && value.StaffRecruitIds.Count > 0) ?? 0,
                relationshipCount = city?.RelationshipMemories?.Count ?? 0,
                claimedBattleRewardCount = campaign?.Guild?.Development?.ClaimedBattleRewardIds?.Count ?? 0,
                defensesResolved = (strategic?.TotalDefensesWon ?? 0) + (strategic?.TotalDefensesLost ?? 0),
                completedPersonalQuests = people?.PersonalQuests?.Count(value => value.Completed) ?? 0,
                completedWorldGateBoards = playable?.WorldGate023?.CompletedDefinitionIds?.Count ?? 0,
                completedCampaignChapters = progress?.CompletedChapterIds?.Count ?? 0,
                completedCreatorRooms = creator?.CompletedRoomIds?.Count ?? 0,
                progressionItems = progression?.EquipmentEvolution?.Count ?? 0,
                invocationArtifacts = progression?.InvocationArtifacts?.Count ?? 0,
                summonResonance = progression?.SummonResonance ?? 0,
                lastCheckpointId = city?.LastCheckpointId ?? string.Empty
            };
        }

        private static ProgressionWeaponProof030 FindWeaponForProgression(CampaignState campaign)
        {
            foreach (var recruit in campaign?.Guild?.Recruits ?? Array.Empty<RecruitState>())
            {
                foreach (var assignment in recruit.Equipment?.Assignments ?? Array.Empty<EquipmentSlotAssignmentState>())
                {
                    if (assignment?.Item == null) continue;
                    var track = TrackForTags(assignment.Item.EquipmentTags);
                    if (track == null) continue;
                    return new ProgressionWeaponProof030
                    {
                        RecruitId = recruit.RecruitId,
                        SlotId = assignment.SlotId,
                        ItemId = assignment.Item.InstanceId,
                        TrackId = track
                    };
                }
            }
            return null;
        }

        private static string TrackForTags(IReadOnlyList<string> tags)
        {
            var set = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (set.Contains("SWORD")) return "WTRACK022_01";
            if (set.Contains("GREAT_WEAPON") || set.Contains("GREATSWORD")) return "WTRACK022_02";
            if (set.Contains("AXE")) return "WTRACK022_03";
            if (set.Contains("SPEAR") || set.Contains("POLEARM")) return "WTRACK022_04";
            if (set.Contains("BOW")) return "WTRACK022_05";
            if (set.Contains("DAGGER")) return "WTRACK022_06";
            if (set.Contains("SHIELD")) return "WTRACK022_07";
            if (set.Contains("GAUNTLET")) return "WTRACK022_08";
            if (set.Contains("STAFF")) return "WTRACK022_09";
            if (set.Contains("FOCUS") || set.Contains("WAND")) return "WTRACK022_10";
            if (set.Contains("ENGINEERING") || set.Contains("ENGINEERING_TOOL")) return "WTRACK022_11";
            if (set.Contains("RELIC_WEAPON") || set.Contains("HYBRID_RELIC")) return "WTRACK022_12";
            return null;
        }

        private static int CountOwnedEquipmentInstances(CampaignState campaign, string itemInstanceId)
        {
            if (campaign?.Guild == null || string.IsNullOrWhiteSpace(itemInstanceId)) return 0;
            return campaign.Guild.Inventory.Count(value =>
                       StringComparer.Ordinal.Equals(value.InstanceId, itemInstanceId)) +
                   CountAssignedEquipmentInstances(campaign, itemInstanceId);
        }

        private static int CountAssignedEquipmentInstances(CampaignState campaign, string itemInstanceId)
        {
            if (campaign?.Guild == null || string.IsNullOrWhiteSpace(itemInstanceId)) return 0;
            return campaign.Guild.Recruits.Sum(recruit => recruit.Equipment.Assignments.Count(assignment =>
                StringComparer.Ordinal.Equals(assignment.Item.InstanceId, itemInstanceId)));
        }

        private void PrepareCleanInitialSave()
        {
            var directory = Path.GetDirectoryName(_options.SavePath);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Owner-review save directory is missing.");
            Directory.CreateDirectory(directory);
            DeleteIfExists(_options.SavePath);
            DeleteIfExists(_options.SavePath + ".bak");
            DeleteIfExists(_options.SavePath + ".tmp");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static string ContentRoot()
        {
            return Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        }

        private static void Require(M1CommandResult result, string action)
        {
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException(action + " failed: " + (result?.Message ?? "no result"));
        }

        private void AddGate(string id, string stage, string status, string detail)
        {
            _report.currentStage = stage;
            _report.gates.Add(new OwnerReviewGateEvidence030
            {
                id = id,
                stage = stage,
                status = status,
                detail = detail ?? string.Empty
            });
            Debug.Log("OWNER REVIEW 030 • " + stage + " • " + id + " • " + status + " • " + detail);
            WriteProgress();
        }

        private void WriteProgress()
        {
            OwnerReviewSmokeEvidenceWriter030.Write(_options.ProgressPath, _report);
        }

        private sealed class ProgressionWeaponProof030
        {
            public string RecruitId;
            public string SlotId;
            public string ItemId;
            public string TrackId;
        }

        private sealed class SelectedActionEvidence030
        {
            public string AnimationTag;
            public string Discipline;
            public string WeaponFamily;
            public string MysticSchool;
            public string ProfileSchool;
        }
    }
}
