using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.GuildCity017G;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.RecruitChronicles025;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed partial class GuildCityExpeditionService017D
    {
        public const string FirstStoryContractId066 = "CONTRACT_BELL_BENEATH_GATE";
        public const string SecondStoryContractId076 = "CONTRACT_LINES_NOT_RETURNED";
        public const string SecondStoryBoardId076 = "BOARD_LINES_NOT_RETURNED";
        public const string ChapterTwoWayglassOpenedFlag076 = "CHAPTER_TWO_WAYGLASS_OPENED_076";
        public const string LegacyFirstRescueBoardId069 = "BOARD_BELL_BENEATH_GATE";
        public const string StreamlinedFirstRescueBoardId069 = "BOARD_BELL_BENEATH_GATE_069";
        public const string FirstHourThreeBattleBoardId071 = "BOARD_BELL_BENEATH_GATE_071";
        public const string ReliefRoadBoardId081 = "BOARD_RELIEF_ROAD";
        public const int FirstStoryMinimumRosterCount066 = 7;
        public const int FirstHourThreeBattleRosterCount071 = 10;
        public const string FirstHourLanternPatrolRescuedFlag071 =
            "FIRST_HOUR071_LANTERN_PATROL_RESCUED";
        public const string FirstHourForcedMarchFlag081 =
            "FIRST_HOUR081_FORCED_MARCH";
        public const string StoryQuestForcedMarchFlag081 =
            "STORY_QUEST081_FORCED_MARCH";
        public const string FirstHourHallBreachEncounterId071 = "ENCOUNTER071_HALL_BREACH";
        public const string FirstHourGateEaterEncounterId071 = "ENCOUNTER071_GATE_EATER";
        public const string FirstStorySafePassageEventId080 =
            "EVENT_GARA_WOUNDED_LANTERN_PASSAGE";
        public const string SecondStorySafeDescentEventId080 =
            "EVENT_GARA_SURVEY_CREW_DESCENT";
        public const string LegacyFirstRescueMigratedFlag069 = "LEGACY_FIRST_RESCUE_MIGRATED_069";
        public const string GateworksMaintenancePassageFlag066 = "SECRET_MAINTENANCE_PASSAGE_FOUND_N05";
        public const int CarefulApproachModifier076 = 2;
        public const int CarefulApproachPressureCost076 = 1;
        public const int SwiftApproachModifier076 = 0;

        public static bool CanUseCarefulApproach076(int pressure) =>
            pressure >= CarefulApproachPressureCost076;

        /// <summary>
        /// Release 081 room reveals and their immediate Guild XP belong only to
        /// the three presented opening story quests. Keeping this predicate in
        /// gameplay authority lets command and presentation routing share the same scope.
        /// </summary>
        public static bool UsesBoardQuestRewards081(string boardId) =>
            StringComparer.Ordinal.Equals(boardId, LegacyFirstRescueBoardId069) ||
            StringComparer.Ordinal.Equals(boardId, StreamlinedFirstRescueBoardId069) ||
            StringComparer.Ordinal.Equals(boardId, FirstHourThreeBattleBoardId071) ||
            StringComparer.Ordinal.Equals(boardId, SecondStoryBoardId076) ||
            StringComparer.Ordinal.Equals(boardId, ReliefRoadBoardId081);

        private static readonly string[] ChapterTwoEvidenceEventIds079 =
        {
            "EVENT_FOUND_APPRENTICE",
            "EVENT_WRONG_ROUTE_MARKS",
            "EVENT_BROKEN_SURVEY_BRIDGE",
            "EVENT_CAMP_ARGUMENT",
            "EVENT_FOG_ECHO",
            "EVENT_BROKEN_ASTROLABE",
            "EVENT_ABANDONED_SURVEY_PACK"
        };

        /// <summary>
        /// True only for the retired first-rescue route.  This deliberately does not
        /// treat other active contracts as migration candidates.
        /// </summary>
        public static bool NeedsLegacyFirstRescueMigration069(CampaignState campaign)
        {
            var city = campaign?.Guild?.GuildCity;
            var contract = city?.ActiveContract;
            if (contract == null || contract.Completed || contract.Failed ||
                !StringComparer.Ordinal.Equals(contract.ContractId, FirstStoryContractId066))
                return false;
            var expedition = city.Expedition;
            if (expedition != null &&
                expedition.Status != ExpeditionStatus017D.Active &&
                expedition.Status != ExpeditionStatus017D.AwaitingBattle)
                return false;
            return StringComparer.Ordinal.Equals(contract.BoardId, LegacyFirstRescueBoardId069) ||
                   StringComparer.Ordinal.Equals(expedition?.BoardId, LegacyFirstRescueBoardId069);
        }

        /// <summary>
        /// Safely moves an already-committed legacy opening rescue onto the short
        /// Version 69 route.  Guild, roster, inventory, city, claimed rewards and all
        /// completed operation progress remain untouched.  A pre-rescue expedition is
        /// restarted at the new entrance.  A legacy rescue battle which has already
        /// been claimed is mapped to N06 with its rescue flag intact, so the player
        /// only has to walk home and cannot receive the battle reward twice.
        /// </summary>
        public Result<CampaignState> MigrateLegacyFirstRescue069(
            CampaignState campaign,
            GuildCityContent017D content,
            GuildCityStrategicContent017H strategicContent = null)
        {
            if (campaign == null || content == null)
                return Result<CampaignState>.Failure("GC017D_V69_RESCUE_MIGRATION_INPUT_REQUIRED");
            if (!NeedsLegacyFirstRescueMigration069(campaign))
                return Result<CampaignState>.Failure("GC017D_V69_LEGACY_RESCUE_NOT_ACTIVE");
            if (!content.Boards.ContainsKey(StreamlinedFirstRescueBoardId069))
                return Result<CampaignState>.Failure("GC017D_V69_STREAMLINED_RESCUE_CONTENT_REQUIRED");

            var state = campaign.Guild.GuildCity;
            if (state.PendingEncounter != null)
                return Result<CampaignState>.Failure("GC017D_V69_RESOLVE_PENDING_ENCOUNTER_FIRST");
            if (state.PendingBattleReturn != null)
                return Result<CampaignState>.Failure("GC017D_V69_APPLY_PENDING_BATTLE_RETURN_FIRST");
            if (campaign.Battle != null && campaign.Battle.Outcome == BattleOutcome.InProgress)
                return Result<CampaignState>.Failure("GC017D_V69_FINISH_ACTIVE_BATTLE_FIRST");
            if (campaign.Battle?.Reward != null && !campaign.Battle.Reward.Claimed)
                return Result<CampaignState>.Failure("GC017D_V69_CLAIM_BATTLE_REWARD_FIRST");

            var legacyExpedition = state.Expedition;
            if (legacyExpedition != null &&
                legacyExpedition.Status != ExpeditionStatus017D.Active)
                return Result<CampaignState>.Failure("GC017D_V69_LEGACY_RESCUE_MUST_RESOLVE_FIRST");

            var legacyContract = state.ActiveContract;
            var migratedSeed = SemanticSeed.Derive(
                campaign.CampaignSeed,
                legacyContract.CanonicalSeedIdentity,
                legacyContract.CommitId,
                StreamlinedFirstRescueBoardId069,
                "VERSION_69_SAFE_RESCUE_MIGRATION").ToString();
            // Preserve the accepted contract identity and ordinal.  Those identities
            // are referenced by completion rewards; only its retired route authority
            // and deterministic route seed change.
            var migratedContract = new ContractCommitState017D(
                legacyContract.CommitId,
                legacyContract.ContractId,
                StreamlinedFirstRescueBoardId069,
                migratedSeed,
                legacyContract.AcceptedOperationOrdinal,
                false,
                false);
            var retargetedCity = state.With(
                activeContract: migratedContract,
                replaceActiveContract: true,
                expedition: null,
                replaceExpedition: true,
                lastCheckpointId: "legacy_first_rescue_retargeted_069");
            var retargeted = campaign.With(
                campaign.Guild.WithGuildCity(retargetedCity),
                campaign.OpeningFlow);
            var begun = StartExpedition(retargeted, content, strategicContent);
            if (!begun.IsSuccess) return begun;

            var migrated = begun.Value;
            var newCity = migrated.Guild.GuildCity;
            var newExpedition = newCity.Expedition;
            var rescueAlreadyWon = LegacyRescueAlreadyWon069(legacyExpedition);
            var flags = AddUnique(newExpedition.ObjectiveFlags, LegacyFirstRescueMigratedFlag069);
            IReadOnlyList<string> visited = newExpedition.VisitedNodeIds;
            IReadOnlyList<string> revealed = newExpedition.RevealedNodeIds;
            IReadOnlyList<string> moves = newExpedition.CommittedMoveIds;
            IReadOnlyList<CommittedCheckState017D> checks = newExpedition.CommittedChecks;
            var currentNode = newExpedition.CurrentNodeId;
            var checkpoint = "legacy_first_rescue_restarted_069";

            if (legacyExpedition != null)
            {
                // These are immutable proof records, not rewards.  Keeping them
                // preserves a check the player already rolled (including the N04
                // check shared by both routes) without carrying old encounter-clear
                // flags that could incorrectly skip the new rescue battle.
                moves = legacyExpedition.CommittedMoveIds;
                checks = legacyExpedition.CommittedChecks;
            }

            if (rescueAlreadyWon)
            {
                // Battle return application already committed materials, XP,
                // equipment and relationships outside the expedition object.  Keep
                // its route proof too, then translate only the current location and
                // encounter flag required by the short board.
                flags = MergeUnique069(flags, legacyExpedition.ObjectiveFlags);
                flags = AddUnique(flags, EncounterClearedFlag("N06"));
                flags = AddUnique(flags, "OBJECTIVE_ENCOUNTER_CLEARED");
                flags = AddUnique(flags, "PRIMARY_OBJECTIVE_RESCUE_COMPLETE");
                visited = MergeUnique069(new[] { "N00", "N01", "N04", "N06" },
                    legacyExpedition.VisitedNodeIds);
                revealed = MergeUnique069(visited, new[] { "N14" });
                moves = legacyExpedition.CommittedMoveIds;
                checks = legacyExpedition.CommittedChecks;
                currentNode = "N06";
                checkpoint = "legacy_first_rescue_reward_preserved_walk_home_069";
            }

            var updatedExpedition = new ExpeditionState017D(
                newExpedition.ExpeditionId,
                newExpedition.ContractCommitId,
                newExpedition.BoardId,
                currentNode,
                ExpeditionStatus017D.Active,
                rescueAlreadyWon ? legacyExpedition.Supplies : newExpedition.Supplies,
                rescueAlreadyWon ? legacyExpedition.Fatigue : newExpedition.Fatigue,
                rescueAlreadyWon ? legacyExpedition.Threat : newExpedition.Threat,
                rescueAlreadyWon ? legacyExpedition.Urgency : newExpedition.Urgency,
                visited,
                revealed,
                moves,
                checks,
                flags,
                checkpoint);
            var updatedCity = newCity.With(
                expedition: updatedExpedition,
                replaceExpedition: true,
                lastCheckpointId: checkpoint);
            return Success(migrated, updatedCity);
        }

        private static bool LegacyRescueAlreadyWon069(ExpeditionState017D expedition)
        {
            if (expedition?.ObjectiveFlags == null) return false;
            return Contains(expedition.ObjectiveFlags, "PRIMARY_OBJECTIVE_RESCUE_COMPLETE") ||
                   Contains(expedition.ObjectiveFlags, EncounterClearedFlag("N13"));
        }

        private static IReadOnlyList<string> MergeUnique069(
            IReadOnlyList<string> first,
            IReadOnlyList<string> second)
        {
            var merged = first ?? Array.Empty<string>();
            if (second == null) return merged;
            for (var index = 0; index < second.Count; index++)
                merged = AddUnique(merged, second[index]);
            return merged;
        }

        public static bool HasUnresolvedOperation(GuildCityState017D state)
        {
            if (state == null) return false;
            var expedition = state.Expedition;
            if (expedition != null &&
                (expedition.Status == ExpeditionStatus017D.Active ||
                 expedition.Status == ExpeditionStatus017D.AwaitingBattle)) return true;
            var contract = state.ActiveContract;
            return contract != null && !contract.Completed && !contract.Failed;
        }

        public static bool HasActiveChapterAdventure084(GuildCityState017D state)
        {
            var progress=state?.Strategic017H?.Campaign019;
            return progress?.ActiveOperation!=null||
                   progress?.Playable020?.ActiveOperation!=null;
        }

        /// <summary>
        /// One fail-closed view of every authority-owned adventure which can mutate
        /// the shared Guild City handoff, battle slot, or operation ordinal.  New
        /// operations must use this predicate so Tower, story, World Gate, personal
        /// quests, defense, and ordinary Guild expeditions cannot overlap in either
        /// start order.
        /// </summary>
        public static bool HasAnyUnresolvedAdventure084(CampaignState campaign, bool allowPausedCampaign150 = false)
        {
            if (campaign?.TitanTrials160?.Active != null) return true;
            if (!allowPausedCampaign150 && campaign?.Recovery150?.Paused == true) return true;
            var city=campaign?.Guild?.GuildCity;
            if(city==null)return false;
            var strategic=city.Strategic017H;
            var progress=strategic?.Campaign019;
            var playable=progress?.Playable020;
            if(HasUnresolvedOperation(city)||city.PendingEncounter!=null||
               city.PendingBattleReturn!=null||strategic?.ActiveDefense!=null||
               progress?.ActiveOperation!=null||progress?.PendingReceipt!=null||
               playable?.ActiveOperation!=null||
               playable?.WorldGate023?.ActiveOperation!=null||
               playable?.Progression022?.ActiveAbyssOperation!=null||
               HasActiveChronicleQuest084(campaign,playable?.RecruitChronicles025))
                return true;
            var battle=campaign.Battle;
            return battle!=null&&
                   (battle.Outcome==BattleOutcome.InProgress||
                    (battle.Reward!=null&&!battle.Reward.Claimed));
        }

        /// <summary>
        /// The only intentional nesting is Campaign 020 advancing the already
        /// committed Campaign 019 chapter, and that chapter launching its matching
        /// Campaign 023 WORLD_BOARD.  This view excludes those two story records but
        /// keeps every unrelated authority in the global guard.
        /// </summary>
        public static bool HasUnresolvedAdventureOutsideChapter084(
            CampaignState campaign)
        {
            if (campaign?.TitanTrials160?.Active != null) return true;
            var city=campaign?.Guild?.GuildCity;
            if(city==null)return false;
            var strategic=city.Strategic017H;
            var playable=strategic?.Campaign019?.Playable020;
            if(HasUnresolvedOperation(city)||city.PendingEncounter!=null||
               city.PendingBattleReturn!=null||strategic?.ActiveDefense!=null||
               playable?.WorldGate023?.ActiveOperation!=null||
               playable?.Progression022?.ActiveAbyssOperation!=null||
               HasActiveChronicleQuest084(campaign,playable?.RecruitChronicles025))
                return true;
            var battle=campaign.Battle;
            return battle!=null&&
                   (battle.Outcome==BattleOutcome.InProgress||
                   (battle.Reward!=null&&!battle.Reward.Claimed));
        }

        public static bool HasUnresolvedAdventureOutsideChronicle084(
            CampaignState campaign)
        {
            if (campaign?.TitanTrials160?.Active != null) return true;
            var city=campaign?.Guild?.GuildCity;
            if(city==null)return false;
            var strategic=city.Strategic017H;
            var progress=strategic?.Campaign019;
            var playable=progress?.Playable020;
            if(HasUnresolvedOperation(city)||city.PendingEncounter!=null||
               city.PendingBattleReturn!=null||strategic?.ActiveDefense!=null||
               progress?.ActiveOperation!=null||progress?.PendingReceipt!=null||
               playable?.ActiveOperation!=null||
               playable?.WorldGate023?.ActiveOperation!=null||
               playable?.Progression022?.ActiveAbyssOperation!=null)
                return true;
            var battle=campaign.Battle;
            return battle!=null&&
                   (battle.Outcome==BattleOutcome.InProgress||
                    (battle.Reward!=null&&!battle.Reward.Claimed));
        }

        private static bool HasActiveChronicleQuest084(
            CampaignState campaign,RecruitChronicleState025 chronicle)
        {
            if(campaign==null||chronicle?.PersonalQuests==null)return false;
            for(var index=0;index<chronicle.PersonalQuests.Count;index++)
            {
                var quest=chronicle.PersonalQuests[index];
                if(quest!=null&&!quest.Completed&&
                   !RecruitChronicleRules025.IsArchivedLegacyQuest(
                       chronicle,campaign.CampaignSeed,quest))return true;
            }
            return false;
        }

        public Result<CampaignState> AcceptContract(CampaignState campaign, GuildCityContent017D content, string contractId)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_CONTRACT_INPUT_REQUIRED");
            if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"CAMPAIGN"))return Result<CampaignState>.Failure("Resume the saved Campaign activity first.");
            var state = campaign.Guild.GuildCity;
            if (HasAnyUnresolvedAdventure084(campaign))
                return Result<CampaignState>.Failure(
                    "GC017D_FINISH_ACTIVE_ADVENTURE_FIRST");
            if (state.ActiveContract != null && !state.ActiveContract.Completed && !state.ActiveContract.Failed)
                return Result<CampaignState>.Failure("GC017D_ACTIVE_CONTRACT_EXISTS");
            if (!content.Contracts.ContainsKey(contractId)) return Result<CampaignState>.Failure("GC017D_CONTRACT_NOT_FOUND");
            var isFirstStoryContract =
                StringComparer.Ordinal.Equals(contractId, FirstStoryContractId066);
            if (isFirstStoryContract &&
                campaign.Guild.Recruits.Count < FirstStoryMinimumRosterCount066)
                return Result<CampaignState>.Failure("GC017D_FIRST_RECURRING_RECRUIT_REQUIRED");
            var definition = content.Contract(contractId);
            if (isFirstStoryContract &&
                !content.Boards.ContainsKey(FirstHourThreeBattleBoardId071))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_BOARD_REQUIRED");
            var committedBoardId = isFirstStoryContract
                ? FirstHourThreeBattleBoardId071
                : definition.BoardId;
            var seed = SemanticSeed.Derive(campaign.CampaignGuid, campaign.CampaignSeed, state.OperationOrdinal,
                contractId, committedBoardId).ToString();
            var hash = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                state.OperationOrdinal,
                contractId,
                committedBoardId,
                seed
            });
            var commit = new ContractCommitState017D(
                "CONTRACT_COMMIT_" + hash.Substring(0, 24).ToUpperInvariant(),
                contractId,
                committedBoardId,
                seed,
                state.OperationOrdinal,
                false,
                false);
            return Success(campaign, state.With(
                activeContract: commit,
                replaceActiveContract: true,
                expedition: null,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true,
                lastCheckpointId: "contract_committed"));
        }

        public Result<CampaignState> StartExpedition(CampaignState campaign, GuildCityContent017D content, GuildCityStrategicContent017H strategicContent = null)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_EXPEDITION_INPUT_REQUIRED");
            var state = campaign.Guild.GuildCity;
            if (state.ActiveContract == null) return Result<CampaignState>.Failure("GC017D_CONTRACT_REQUIRED");
            if (state.ActiveContract.Completed || state.ActiveContract.Failed)
                return Result<CampaignState>.Failure("GC017D_CONTRACT_ALREADY_RESOLVED");
            if (state.Expedition != null &&
                (state.Expedition.Status == ExpeditionStatus017D.Active ||
                 state.Expedition.Status == ExpeditionStatus017D.AwaitingBattle))
                return Result<CampaignState>.Success(campaign);
            if (HasTrainingMemberInUnionPlans(campaign.Guild))
            {
                return Result<CampaignState>.Failure(
                    GuildMemberDeploymentPolicy017D.TrainingUnavailableError);
            }
            var board = content.Board(state.ActiveContract.BoardId);
            var hash = CanonicalJson.Sha256Hex(new
            {
                state.ActiveContract.CommitId,
                board.Id,
                state.OperationOrdinal
            });
            var effects = new GuildCityEffectService017D();
            var openingProfile = GuildCityOpeningBalance017G.For(campaign);
            var strategicSnapshot = strategicContent == null ? null : new GuildCityBuildingContributionService017H().Calculate(state, strategicContent);
            var supplyBonus = checked(effects.StartingSupplyBonus(campaign.Guild.Development, state, content) +
                openingProfile.StartingSupplyBonus + (strategicSnapshot?.SuppliesFlat ?? 0));
            var revealCount = effects.InitialRevealNodeCount(campaign.Guild.Development, state, content) +
                (strategicSnapshot?.ScoutingFlat ?? 0);
            var revealedNodes = InitialRevealedNodes(board, revealCount);
            var initialFlags = new List<string>();
            if (supplyBonus != 0) initialFlags.Add("OPENING_MODE_SUPPLY_ADJUSTMENT_" + supplyBonus);
            if (revealCount > 0) initialFlags.Add("CITY_SCOUT_REVEAL_" + revealCount);
            initialFlags.Add("OPENING_MODE_" + openingProfile.Mode.ToString().ToUpperInvariant());
            var opensChapterTwoAtThreshold =
                StringComparer.Ordinal.Equals(state.ActiveContract.ContractId, SecondStoryContractId076) &&
                StringComparer.Ordinal.Equals(board.Id, SecondStoryBoardId076);
            if (opensChapterTwoAtThreshold &&
                !new M1CommandService().ValidateGuildUnionPlans(campaign.Guild).IsSuccess)
                return Result<CampaignState>.Failure("GC017D_UNION_PLANS_NOT_READY");
            // Chapter 2 used to consume N00 -> N01 invisibly and immediately ask
            // the player to choose between two unexplained route cards. Preserve
            // the authored threshold as real play: Kiri finds the surviving
            // apprentice at N00, the Guild resolves that character scene, then the
            // saved board exposes the N01 junction in context.
            var committedStartNodeId = board.StartNodeId;
            IReadOnlyList<string> committedVisitedNodes = new[] { board.StartNodeId };
            if (opensChapterTwoAtThreshold)
                initialFlags.Add(ChapterTwoWayglassOpenedFlag076);
            var expeditionId = "EXPEDITION_" + hash.Substring(0, 24).ToUpperInvariant();
            var committedSupplies = Math.Max(
                1,
                checked(board.StartingSupplies + supplyBonus));
            var committedFatigue = 0;
            var committedUrgency = board.StartingUrgency;
            IReadOnlyList<string> committedStartMoveIds = Array.Empty<string>();
            var expedition = new ExpeditionState017D(
                expeditionId,
                state.ActiveContract.CommitId,
                board.Id,
                committedStartNodeId,
                ExpeditionStatus017D.Active,
                committedSupplies,
                committedFatigue,
                0,
                committedUrgency,
                committedVisitedNodes,
                revealedNodes,
                committedStartMoveIds,
                Array.Empty<CommittedCheckState017D>(),
                initialFlags.AsReadOnly(),
                opensChapterTwoAtThreshold ? "chapter_two_threshold_078" : "board_start");
            var assignments = MarkParticipatingUnionMembersDeployed(
                state.MemberAssignments, campaign.Guild.Unions, campaign.Guild.Recruits);
            return Success(campaign, state.With(
                memberAssignments: assignments,
                expedition: expedition,
                replaceExpedition: true,
                lastCheckpointId: opensChapterTwoAtThreshold
                    ? "chapter_two_threshold_078"
                    : "expedition_started"));
        }

        public Result<CampaignState> CommitMove(CampaignState campaign, GuildCityContent017D content,
            string destinationNodeId)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_MOVE_INPUT_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var expedition = state.Expedition;
            if (expedition?.PendingQuestFate165 != null)
                return Result<CampaignState>.Failure("QUEST_FATE165_COLLECT_PENDING_FIRST");
            if (expedition == null || expedition.Status != ExpeditionStatus017D.Active)
                return Result<CampaignState>.Failure("GC017D_ACTIVE_EXPEDITION_REQUIRED");
            var board = content.Board(expedition.BoardId);
            var current = board.Node(expedition.CurrentNodeId);
            var resolutionError = CurrentNodeResolutionError(expedition, current);
            if (resolutionError != null) return Result<CampaignState>.Failure(resolutionError);
            var destination = board.Node(destinationNodeId);
            var linked = false;
            for (var index = 0; index < (current.Links?.Length ?? 0); index++)
                if (StringComparer.Ordinal.Equals(current.Links[index], destinationNodeId)) linked = true;
            if (!linked) return Result<CampaignState>.Failure("GC017D_DESTINATION_NOT_CONNECTED");
            var moveHash = CanonicalJson.Sha256Hex(new
            {
                expedition.ExpeditionId,
                expedition.CurrentNodeId,
                destinationNodeId,
                MoveOrdinal = expedition.CommittedMoveIds.Count
            });
            var moveId = "MOVE_" + moveHash.Substring(0, 24).ToUpperInvariant();
            if (Contains(expedition.CommittedMoveIds, moveId)) return Result<CampaignState>.Success(campaign);
            var supplyShortfall = Math.Max(0, destination.SupplyCost - expedition.Supplies);
            var isFirstHourForcedMarch = supplyShortfall > 0 &&
                StringComparer.Ordinal.Equals(
                    expedition.BoardId,
                    FirstHourThreeBattleBoardId071);
            var isStoryQuestForcedMarch = supplyShortfall > 0 &&
                (isFirstHourForcedMarch ||
                 StringComparer.Ordinal.Equals(
                     expedition.BoardId,
                     SecondStoryBoardId076));
            if (supplyShortfall > 0 && !isStoryQuestForcedMarch)
                return Result<CampaignState>.Failure("GC017D_SUPPLIES_INSUFFICIENT");

            var visited = AddUnique(expedition.VisitedNodeIds, destinationNodeId);
            var revealed = AddUnique(expedition.RevealedNodeIds, destinationNodeId);
            if (StringComparer.Ordinal.Equals(destination.Kind, "SCOUTING"))
                for (var index = 0; index < (destination.Links?.Length ?? 0); index++)
                    revealed = AddUnique(revealed, destination.Links[index]);
            var moves = AddUnique(expedition.CommittedMoveIds, moveId);
            var objectives = expedition.ObjectiveFlags;
            if (isStoryQuestForcedMarch)
                objectives = AddUnique(objectives, StoryQuestForcedMarchFlag081);
            if (isFirstHourForcedMarch)
                objectives = AddUnique(objectives, FirstHourForcedMarchFlag081);
            var materials = new List<GuildMaterialState017D>(state.Materials);
            var openingProfile = GuildCityOpeningBalance017G.For(campaign);
            var fatigueCost = openingProfile.AdjustFatigueCost(destination.FatigueCost);
            var urgencyCost = openingProfile.AdjustUrgencyCost(destination.UrgencyCost);
            // The two presented story quests are one-way operations with no
            // guaranteed resupply action. Failed checks and battle expenditure can
            // legitimately leave the player below the next room's travel cost.
            // Convert only those story boards' shortage into fatigue so an accepted
            // objective can never become a silent dead end; every other board keeps
            // the strict supply rule above.
            var fatigue = expedition.Fatigue + fatigueCost + supplyShortfall;
            if (StringComparer.Ordinal.Equals(destination.Kind, "CAMP")) fatigue = Math.Max(0, fatigue - 3);
            if (StringComparer.Ordinal.Equals(destination.Kind, "RESOURCE"))
            {
                objectives = AddUnique(objectives, "RESOURCE_RECOVERED_" + destination.Id);
                materials = MergeMaterials(materials,
                    new[] { new GuildMaterialState017D("MAT_SALVAGED_TIMBER", 2) });
            }
            if (StringComparer.Ordinal.Equals(destination.Kind, "SECONDARY_OBJECTIVE"))
                objectives = AddUnique(objectives, "SECONDARY_OBJECTIVE_REACHED_" + destination.Id);
            var supplies = Math.Max(0, expedition.Supplies - destination.SupplyCost);
            var roomGuildXp081 = 0;
            var roomPressureRelief081 = 0;
            if (UsesBoardQuestRewards081(expedition.BoardId))
            {
                // An authored camp can also carry a conversation event. Preserve
                // CAMPFIRE as the revealed room identity so the already-committed
                // three-fatigue rest above remains visible and testable; other
                // event-backed destinations continue to reveal as FATE rooms.
                var authoredRoomKind081 = StringComparer.Ordinal.Equals(
                    destination.Kind,
                    "CAMP")
                    ? destination.Kind
                    : string.IsNullOrWhiteSpace(destination.EventId)
                        ? destination.Kind
                        : "EVENT";
                var roomKind081 = BoardRoomKind081(
                    expedition.ExpeditionId,
                    destinationNodeId,
                    authoredRoomKind081);
                // The P0 enhancement room is chosen inside the same committed
                // move that reveals the destination. Persisting the stable ID in
                // objective flags prevents reload rerolls without altering any
                // authored story, encounter, or reward authority.
                var roomModuleId001 = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                    expedition.ExpeditionId,
                    destinationNodeId,
                    roomKind081);
                var roomModuleFlag001 = BoardTowerEnhancementRules001.SelectedRoomFlag001(
                    destinationNodeId,
                    roomModuleId001);
                if (!string.IsNullOrWhiteSpace(roomModuleFlag001))
                    objectives = AddUnique(objectives, roomModuleFlag001);
                objectives = AddUnique(
                    objectives,
                    BoardRoomRewardFlag081(destinationNodeId, roomKind081));
                switch (roomKind081)
                {
                    case "TREASURE":
                        supplies = checked(supplies + 1);
                        if (!StringComparer.Ordinal.Equals(destination.Kind, "RESOURCE"))
                            materials = MergeMaterials(materials,
                                new[] { new GuildMaterialState017D("MAT_SALVAGED_TIMBER", 1) });
                        break;
                    case "SKILL":
                        // A revealed skill room teaches Trailcraft for this expedition.
                        // The durable room flag supplies +1 to later board checks and
                        // the XP remains useful after the party returns home.
                        roomGuildXp081 = 4;
                        break;
                    case "BLESSING":
                        supplies = checked(supplies + 1);
                        fatigue = Math.Max(0, fatigue - 2);
                        break;
                    case "DISCOVERY":
                        roomPressureRelief081 = 1;
                        break;
                }
            }
            var status = StringComparer.Ordinal.Equals(destinationNodeId, board.ExitNodeId)
                ? ExpeditionStatus017D.Completed
                : ExpeditionStatus017D.Active;
            var updated = expedition.With(
                currentNodeId: destinationNodeId,
                status: status,
                supplies: supplies,
                fatigue: fatigue,
                urgency: Math.Max(
                    0,
                    expedition.Urgency - urgencyCost - roomPressureRelief081),
                visitedNodeIds: visited,
                revealedNodeIds: revealed,
                committedMoveIds: moves,
                objectiveFlags: objectives,
                lastCheckpointId: "move_" + moveId);
            var rewardedCampaign081 = campaign;
            if (roomGuildXp081 > 0)
            {
                var guild081 = campaign.Guild.With(
                    checked(campaign.Guild.TreasuryXp + roomGuildXp081),
                    campaign.Guild.Recruits,
                    campaign.Guild.Unions,
                    campaign.Guild.Inventory,
                    campaign.Guild.Development);
                rewardedCampaign081 = campaign.With(guild081, campaign.OpeningFlow);
            }
            return Success(rewardedCampaign081, state.With(
                materials: materials.AsReadOnly(),
                expedition: updated,
                replaceExpedition: true,
                lastCheckpointId: status == ExpeditionStatus017D.Completed
                    ? "expedition_exit_reached"
                    : "board_move_committed"));
        }

        /// <summary>
        /// Event authority owns whether a check is random. The board parameter is
        /// retained for presentation callers, but chapter behavior cannot override
        /// the event's committed-resolution contract.
        /// </summary>
        public static bool UsesCommitted2d6ForBoard079(
            string boardId,
            EventDefinition017D eventDefinition)
        {
            // Release 081 turns the opening missions into a visible board-game
            // loop. Chapter 2 previously hid its outcome behind an authored score,
            // which made the player's choice feel like an unexplained form. Its
            // story events now use the same committed, replay-safe 2d6 authority as
            // Chapter 1. Other campaign boards retain their authored contract.
            if (StringComparer.Ordinal.Equals(boardId, SecondStoryBoardId076))
                return true;
            return eventDefinition == null || eventDefinition.UsesCommitted2d6;
        }

        public static int BoardGameCheckGuildXp081(string outcome)
        {
            switch ((outcome ?? string.Empty).ToUpperInvariant())
            {
                case "EXCEPTIONAL": return 12;
                case "FULL_SUCCESS": return 8;
                case "SUCCESS_WITH_COST": return 5;
                case "SETBACK": return 2;
                default: return 1;
            }
        }

        public static string BoardRoomKind081(
            string expeditionId,
            string nodeId,
            string authoredKind)
        {
            var kind=(authoredKind??string.Empty).ToUpperInvariant();
            if(kind=="START")return "START";
            if(kind=="EXIT")return "RETURN";
            if(kind.Contains("ENCOUNTER")||kind.Contains("ELITE"))return "MONSTER";
            if(kind.Contains("EVENT")||kind.Contains("CHECK"))return "FATE";
            if(kind.Contains("OBJECTIVE"))return "STORY";
            if(kind=="RESOURCE")return "TREASURE";
            if(kind=="CAMP")return "CAMPFIRE";
            var hash=CanonicalJson.Sha256Hex(new
            {
                Rule="BOARD_ROOM_REVEAL_081",
                Expedition=expeditionId??string.Empty,
                Node=nodeId??string.Empty
            });
            switch(Convert.ToInt32(hash.Substring(0,2),16)%4)
            {
                case 0:return "TREASURE";
                case 1:return "SKILL";
                case 2:return "BLESSING";
                default:return "DISCOVERY";
            }
        }

        public static string BoardRoomShuffleKey081(
            string expeditionId,
            string currentNodeId,
            string destinationNodeId) => CanonicalJson.Sha256Hex(new
            {
                Rule="BOARD_ROUTE_SHUFFLE_081",
                Expedition=expeditionId??string.Empty,
                Current=currentNodeId??string.Empty,
                Destination=destinationNodeId??string.Empty
            });

        public static string BoardRoomRewardFlag081(string nodeId,string roomKind) =>
            "ROOM_REVEALED081_"+(nodeId??"ROOM")+"_"+(roomKind??"DISCOVERY").ToUpperInvariant();

        public static int BoardRoomSkillBonus081(IReadOnlyList<string> objectiveFlags)
        {
            var count=0;
            var flags=objectiveFlags??Array.Empty<string>();
            for(var index=0;index<flags.Count;index++)
                if((flags[index]??string.Empty).StartsWith("ROOM_REVEALED081_",StringComparison.Ordinal)&&
                   (flags[index]??string.Empty).EndsWith("_SKILL",StringComparison.Ordinal))
                    count++;
            return Math.Min(3,count);
        }

        public static int BoardRoomBlessingBonus081(IReadOnlyList<string> objectiveFlags)
        {
            var count=0;
            var flags=objectiveFlags??Array.Empty<string>();
            for(var index=0;index<flags.Count;index++)
                if((flags[index]??string.Empty).StartsWith("ROOM_REVEALED081_",StringComparison.Ordinal)&&
                   (flags[index]??string.Empty).EndsWith("_BLESSING",StringComparison.Ordinal))
                    count++;
            return Math.Min(3,count);
        }

        public static bool IsChapterTwoEvidenceEvent079(string eventId)
        {
            for (var index = 0; index < ChapterTwoEvidenceEventIds079.Length; index++)
                if (StringComparer.Ordinal.Equals(ChapterTwoEvidenceEventIds079[index], eventId))
                    return true;
            return false;
        }

        public static bool ChapterTwoRoleFits079(
            string classTendencyId,
            IReadOnlyList<string> eligibleSkills)
        {
            var role = (classTendencyId ?? string.Empty).ToUpperInvariant();
            if (role.Length == 0) return false;
            var skills = eligibleSkills ?? Array.Empty<string>();
            for (var index = 0; index < skills.Count; index++)
            {
                var skill = (skills[index] ?? string.Empty).ToUpperInvariant();
                if ((skill == "MEDICINE" || skill == "DIPLOMACY") &&
                    (role.Contains("PRIEST") || role.Contains("HEAL") || role.Contains("MAGE")))
                    return true;
                if ((skill == "PERCEPTION" || skill == "SURVIVAL" || skill == "STEALTH") &&
                    (role.Contains("RANGER") || role.Contains("ROGUE") || role.Contains("SCOUT")))
                    return true;
                if ((skill == "LORE" || skill == "ENGINEERING") &&
                    (role.Contains("MAGE") || role.Contains("GUARDIAN") || role.Contains("WARRIOR")))
                    return true;
                if ((skill == "COMMAND" || skill == "RESOLVE") &&
                    (role.Contains("GUARDIAN") || role.Contains("WARRIOR") || role.Contains("PRIEST")))
                    return true;
                if (skill == "ATHLETICS" &&
                    (role.Contains("WARRIOR") || role.Contains("GUARDIAN")))
                    return true;
            }
            return false;
        }

        public static int ChapterTwoEvidenceContribution079(
            int reliableEvidenceCount,
            int partialEvidenceCount) =>
            Math.Min(2, Math.Max(0, reliableEvidenceCount) +
                        (Math.Max(0, partialEvidenceCount) >= 2 ? 1 : 0));

        public static int ChapterTwoTrustContribution079(
            int civicTrust,
            int relationshipStrength)
        {
            var trust = Math.Max(0, civicTrust) + Math.Max(0, relationshipStrength);
            return trust >= 7 ? 2 : trust >= 3 ? 1 : 0;
        }

        public static int ChapterTwoAuthoredScore079(
            bool leadRoleFits,
            bool assistantRoleFits,
            int reliableEvidenceCount,
            int partialEvidenceCount,
            int civicTrust,
            int relationshipStrength) =>
            (leadRoleFits ? 2 : 0) +
            (assistantRoleFits ? 1 : 0) +
            ChapterTwoEvidenceContribution079(reliableEvidenceCount, partialEvidenceCount) +
            ChapterTwoTrustContribution079(civicTrust, relationshipStrength);

        public static string ChapterTwoAuthoredOutcome079(
            bool leadRoleFits,
            bool assistantRoleFits,
            int reliableEvidenceCount,
            int partialEvidenceCount,
            int civicTrust,
            int relationshipStrength)
        {
            var score = ChapterTwoAuthoredScore079(
                leadRoleFits,
                assistantRoleFits,
                reliableEvidenceCount,
                partialEvidenceCount,
                civicTrust,
                relationshipStrength);
            return score >= 6 ? "EXCEPTIONAL"
                : score >= 3 ? "FULL_SUCCESS"
                : score >= 1 ? "SUCCESS_WITH_COST"
                : "SETBACK";
        }

        public Result<CampaignState> ResolveCommittedCheck(CampaignState campaign, GuildCityContent017D content,
            string eventId, string actorRecruitId, string assistantRecruitId, int modifier)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_CHECK_INPUT_REQUIRED");
            if (!ContainsRecruit(campaign, actorRecruitId)) return Result<CampaignState>.Failure("GC017D_CHECK_ACTOR_REQUIRED");
            if (!string.IsNullOrWhiteSpace(assistantRecruitId) && !ContainsRecruit(campaign, assistantRecruitId))
                return Result<CampaignState>.Failure("GC017D_CHECK_ASSISTANT_NOT_OWNED");
            var state = campaign.Guild.GuildCity;
            var expedition = state.Expedition;
            if (expedition?.PendingQuestFate165 != null)
                return Result<CampaignState>.Failure("QUEST_FATE165_COLLECT_PENDING_FIRST");
            if (expedition == null || expedition.Status != ExpeditionStatus017D.Active)
                return Result<CampaignState>.Failure("GC017D_ACTIVE_EXPEDITION_REQUIRED");
            var board = content.Board(expedition.BoardId);
            var current = board.Node(expedition.CurrentNodeId);
            if (!IsCommittedCheckNode(current))
                return Result<CampaignState>.Failure("GC017D_CURRENT_NODE_IS_NOT_A_CHECK");
            if (!string.IsNullOrWhiteSpace(current.EventId) &&
                !StringComparer.Ordinal.Equals(current.EventId, eventId ?? string.Empty))
                return Result<CampaignState>.Failure("GC017D_EVENT_DOES_NOT_MATCH_CURRENT_NODE");
            if (string.IsNullOrWhiteSpace(eventId) || !content.Events.ContainsKey(eventId))
                return Result<CampaignState>.Failure("GC017D_EVENT_NOT_FOUND");
            var eventDefinition = content.Event(eventId);
            for (var committedIndex = 0; committedIndex < expedition.CommittedChecks.Count; committedIndex++)
                if (StringComparer.Ordinal.Equals(expedition.CommittedChecks[committedIndex].NodeId,
                    expedition.CurrentNodeId))
                    return Result<CampaignState>.Success(campaign);
            var checkId = "CHECK_" + CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                expedition.ExpeditionId,
                expedition.CurrentNodeId,
                eventId,
                actorRecruitId,
                assistantRecruitId
            }).Substring(0, 24).ToUpperInvariant();
            for (var index = 0; index < expedition.CommittedChecks.Count; index++)
                if (StringComparer.Ordinal.Equals(expedition.CommittedChecks[index].CheckId, checkId))
                    return Result<CampaignState>.Success(campaign);
            var seed = SemanticSeed.Derive(campaign.CampaignSeed, expedition.ExpeditionId,
                expedition.CurrentNodeId, eventId, actorRecruitId, assistantRecruitId);
            var usesCarefulApproach =
                !string.IsNullOrWhiteSpace(assistantRecruitId) &&
                !StringComparer.Ordinal.Equals(actorRecruitId, assistantRecruitId);
            if (usesCarefulApproach && !CanUseCarefulApproach076(expedition.Urgency))
                return Result<CampaignState>.Failure(
                    "GC017D_CAREFUL_APPROACH_PRESSURE_REQUIRED");
            var usesCommitted2d6 = UsesCommitted2d6ForBoard079(
                expedition.BoardId,
                eventDefinition);
            int dieOne;
            int dieTwo;
            int effectiveModifier;
            int total;
            string outcome;
            string canonicalResolutionIdentity;
            if (usesCommitted2d6)
            {
                var rng = new Pcg32(seed.Seed, seed.Stream);
                dieOne = rng.NextInclusive(1, 6);
                dieTwo = rng.NextInclusive(1, 6);
                effectiveModifier = checked(
                    modifier + GuildCityOpeningBalance017G.For(campaign).CheckModifier +
                    (UsesBoardQuestRewards081(expedition.BoardId) ? HeroCheckBoon165(campaign,actorRecruitId) : 0));
                total = dieOne + dieTwo + effectiveModifier;
                outcome = total >= 12 ? "EXCEPTIONAL"
                    : total >= 10 ? "FULL_SUCCESS"
                    : total >= 7 ? "SUCCESS_WITH_COST"
                    : total >= 5 ? "SETBACK"
                    : "SEVERE_SETBACK";
                canonicalResolutionIdentity = seed.ToString();
            }
            else
            {
                var actor = FindRecruit079(campaign.Guild.Recruits, actorRecruitId);
                var assistant = usesCarefulApproach
                    ? FindRecruit079(campaign.Guild.Recruits, assistantRecruitId)
                    : null;
                var leadRoleFits = ChapterTwoRoleFits079(
                    actor?.ClassTendencyId,
                    eventDefinition.EligibleSkills);
                var assistantRoleFits = assistant != null && ChapterTwoRoleFits079(
                    assistant.ClassTendencyId,
                    eventDefinition.EligibleSkills);
                var reliableEvidence = CountChapterTwoEvidence079(
                    expedition.ObjectiveFlags,
                    successful: true);
                var partialEvidence = CountChapterTwoEvidence079(
                    expedition.ObjectiveFlags,
                    successful: false);
                var relationshipStrength = RelationshipStrength079(state.RelationshipMemories);
                outcome = ChapterTwoAuthoredOutcome079(
                    leadRoleFits,
                    assistantRoleFits,
                    reliableEvidence,
                    partialEvidence,
                    state.CivicTrust,
                    relationshipStrength);
                dieOne = 0;
                dieTwo = 0;
                total = AuthoredOutcomeTotal079(outcome);
                effectiveModifier = total;
                canonicalResolutionIdentity = "AUTHORED079_" + CanonicalJson.Sha256Hex(new
                {
                    expedition.ExpeditionId,
                    expedition.CurrentNodeId,
                    eventId,
                    actorRecruitId,
                    assistantRecruitId,
                    LeadRoleFits = leadRoleFits,
                    AssistantRoleFits = assistantRoleFits,
                    ReliableEvidence = reliableEvidence,
                    PartialEvidence = partialEvidence,
                    CivicTrust = state.CivicTrust,
                    RelationshipStrength = relationshipStrength,
                    Outcome = outcome
                }).Substring(0, 24).ToUpperInvariant();
            }
            var succeeded = OutcomeHeld079(outcome);
            var checks = new List<CommittedCheckState017D>(expedition.CommittedChecks)
            {
                new CommittedCheckState017D(checkId, expedition.CurrentNodeId, eventId,
                    actorRecruitId, assistantRecruitId, dieOne, dieTwo, effectiveModifier, total, outcome,
                    canonicalResolutionIdentity)
            };
            checks.Sort((left, right) => StringComparer.Ordinal.Compare(left.CheckId, right.CheckId));
            var objectiveFlags = expedition.ObjectiveFlags;
            objectiveFlags = AddUnique(objectiveFlags, "EVENT_RESOLVED_" + eventId);
            if (succeeded)
            {
                objectiveFlags = AddUnique(objectiveFlags, "CHECK_SUCCESS_" + checkId);
                objectiveFlags = AddUnique(objectiveFlags, EventSuccessFlag076(eventId));
            }
            var successWithCost = StringComparer.Ordinal.Equals(
                outcome,
                "SUCCESS_WITH_COST");
            var consequenceCost = succeeded && !successWithCost ? 0 : 1;
            var fatigue = expedition.Fatigue + consequenceCost;
            var supplies = Math.Max(0, expedition.Supplies - consequenceCost);
            var urgency = Math.Max(
                0,
                expedition.Urgency -
                (usesCarefulApproach ? CarefulApproachPressureCost076 : 0));
            var updated = expedition.With(
                supplies: supplies,
                fatigue: fatigue,
                urgency: urgency,
                committedChecks: checks.AsReadOnly(),
                objectiveFlags: objectiveFlags,
                lastCheckpointId: "check_" + checkId);
            var memories = new List<RelationshipMemoryState017D>(state.RelationshipMemories);
            if (!string.IsNullOrWhiteSpace(assistantRecruitId) &&
                !StringComparer.Ordinal.Equals(actorRecruitId, assistantRecruitId))
            {
                var memoryId = "REL_MEMORY_" + checkId.Substring("CHECK_".Length);
                if (!ContainsMemory(memories, memoryId))
                {
                    var memorySummary = string.IsNullOrWhiteSpace(
                        eventDefinition.RelationshipMemorySummary)
                        ? "They handled “" + eventDefinition.Title + "” together and remembered who made room for the other."
                        : eventDefinition.RelationshipMemorySummary;
                    memories.Add(new RelationshipMemoryState017D(
                        memoryId,
                        actorRecruitId,
                        assistantRecruitId,
                        eventId,
                        memorySummary,
                        state.OperationOrdinal,
                        succeeded ? 2 : 1,
                        "REL_SCENE_" + checkId,
                        false));
                }
            }
            var rewardXp081 = UsesBoardQuestRewards081(expedition.BoardId)
                ? BoardGameCheckGuildXp081(outcome)
                : 0;
            var rewardedCampaign081 = campaign;
            if (rewardXp081 > 0)
            {
                var rewardedGuild081 = campaign.Guild.With(
                    checked(campaign.Guild.TreasuryXp + rewardXp081),
                    campaign.Guild.Recruits,
                    campaign.Guild.Unions,
                    campaign.Guild.Inventory,
                    campaign.Guild.Development);
                rewardedCampaign081 = campaign.With(
                    rewardedGuild081,
                    campaign.OpeningFlow);
            }
            return Success(rewardedCampaign081, state.With(
                relationshipMemories: memories.AsReadOnly(),
                expedition: updated,
                replaceExpedition: true,
                lastCheckpointId: "check_committed"));
        }

        public Result<CampaignState> CommitEncounter(CampaignState campaign, GuildCityContent017D content,
            string encounterId, GuildCityStrategicContent017H strategicContent = null)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_ENCOUNTER_INPUT_REQUIRED");
            if (string.IsNullOrWhiteSpace(encounterId)) return Result<CampaignState>.Failure("GC017D_ENCOUNTER_ID_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var expedition = state.Expedition;
            var retry = expedition != null &&
                        expedition.Status == ExpeditionStatus017D.AwaitingBattle &&
                        state.PendingEncounter != null;
            if (state.PendingEncounter != null && !retry)
                return Result<CampaignState>.Failure("GC017D_PENDING_ENCOUNTER_MISMATCH");
            if (expedition == null ||
                (expedition.Status != ExpeditionStatus017D.Active && !retry))
                return Result<CampaignState>.Failure("GC017D_ACTIVE_EXPEDITION_REQUIRED");
            if (state.ActiveContract == null) return Result<CampaignState>.Failure("GC017D_CONTRACT_REQUIRED");
            var board = content.Board(expedition.BoardId);
            var current = board.Node(expedition.CurrentNodeId);
            if (!StringComparer.Ordinal.Equals(current.Kind, "ENCOUNTER") &&
                !StringComparer.Ordinal.Equals(current.Kind, "OPTIONAL_ELITE") &&
                !StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE"))
                return Result<CampaignState>.Failure("GC017D_CURRENT_NODE_IS_NOT_AN_ENCOUNTER");
            if (!StringComparer.Ordinal.Equals(current.EncounterId ?? string.Empty, encounterId))
                return Result<CampaignState>.Failure("GC017D_ENCOUNTER_DOES_NOT_MATCH_CURRENT_NODE");
            if (Contains(expedition.ObjectiveFlags, EncounterClearedFlag(expedition.CurrentNodeId)))
                return Result<CampaignState>.Failure("GC017D_ENCOUNTER_ALREADY_CLEARED");
            if (IsFirstHourGateEater071(expedition, current) &&
                !HasFirstHourLanternPatrolRescued071(expedition))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_RESCUE_REQUIRED");
            var contract = content.Contract(state.ActiveContract.ContractId);
            var unionIds = new List<string>();
            var plannedUnions165 = UnionBattlePlanRules132.Read(campaign);
            for (var index = 0; index < plannedUnions165.Count && unionIds.Count < 10; index++)
            {
                var union = plannedUnions165[index];
                if (union.Kind == UnionKind.Normal && union.MemberRecruitIds.Count > 0) unionIds.Add(union.UnionId);
            }
            if (unionIds.Count == 0) return Result<CampaignState>.Failure("GC017D_ALLIED_UNION_REQUIRED");
            var seed = SemanticSeed.Derive(campaign.CampaignSeed, expedition.ExpeditionId,
                expedition.CurrentNodeId, encounterId);
            var routeModifiers = new List<string>();
            if (expedition.RevealedNodeIds.Count >= 4) routeModifiers.Add("SCOUTED_APPROACH");
            if (expedition.Fatigue >= 8) routeModifiers.Add("HIGH_FATIGUE");
            if (expedition.Urgency <= 4) routeModifiers.Add("URGENT_OBJECTIVE");
            if (strategicContent != null)
            {
                var snapshot017H = new GuildCityBuildingContributionService017H().Calculate(state, strategicContent);
                var cityModifiers017H = snapshot017H.ToRouteModifiers();
                for (var modifierIndex017H = 0; modifierIndex017H < cityModifiers017H.Count; modifierIndex017H++)
                    if (!routeModifiers.Contains(cityModifiers017H[modifierIndex017H])) routeModifiers.Add(cityModifiers017H[modifierIndex017H]);
                routeModifiers.Sort(StringComparer.Ordinal);
            }
            var hash = CanonicalJson.Sha256Hex(new
            {
                expedition.ExpeditionId,
                expedition.CurrentNodeId,
                encounterId,
                Seed = seed.ToString(),
                UnionIds = unionIds,
                RouteModifiers = routeModifiers
            });
            var enemyUnionCount = GuildCityOpeningBalance017G.For(campaign)
                .AdjustEnemyUnionCount(FirstHourEnemyUnionCount071(
                    contract, current.Kind, encounterId));
            var request = new EncounterLaunchRequest017D(
                "ENCOUNTER_REQUEST_" + hash.Substring(0, 24).ToUpperInvariant(),
                contract.Id,
                expedition.ExpeditionId,
                expedition.BoardId,
                expedition.CurrentNodeId,
                encounterId,
                "BATTLE_" + contract.Id + "_" + encounterId + "_" +
                hash.Substring(0, 12).ToUpperInvariant(),
                EncounterObjective071(contract, encounterId),
                enemyUnionCount,
                seed.ToString(),
                unionIds.AsReadOnly(),
                Array.Empty<string>(),
                new[] { "OBJECTIVE_PRIMARY" },
                routeModifiers.AsReadOnly(),
                expedition.Supplies,
                expedition.Fatigue,
                expedition.Urgency,
                "RETURN_" + expedition.ExpeditionId + "_" + expedition.CurrentNodeId,
                retry
                    ? state.PendingEncounter.PreBattleStateHash
                    : CanonicalJson.Sha256Hex(expedition));
            var requestAuthority = GuildCityBattleBridgeService017D
                .EncounterRequestAuthorityId084(request);
            if (retry)
            {
                if (!StringComparer.Ordinal.Equals(
                        CanonicalJson.Sha256Hex(state.PendingEncounter),
                        CanonicalJson.Sha256Hex(request)) ||
                    !campaign.Guild.Development.HasAdventureAuthority(
                        requestAuthority))
                    return Result<CampaignState>.Failure(
                        "GC017D_PENDING_ENCOUNTER_MISMATCH");
                return Result<CampaignState>.Success(campaign);
            }
            var development = campaign.Guild.Development;
            if (development.HasAdventureAuthority(requestAuthority))
                return Result<CampaignState>.Failure(
                    "GC017D_ENCOUNTER_AUTHORITY_ALREADY_COMMITTED");
            if (!development.CanRecordAdventureAuthority(requestAuthority))
                return Result<CampaignState>.Failure(
                    "GC017D_ADVENTURE_AUTHORITY_LEDGER_FULL");
            development = development.RecordAdventureAuthority(requestAuthority);
            campaign = campaign.With(campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                development), campaign.OpeningFlow);
            var waiting = expedition.With(
                status: ExpeditionStatus017D.AwaitingBattle,
                lastCheckpointId: "encounter_committed");
            return Success(campaign, state.With(
                expedition: waiting,
                replaceExpedition: true,
                pendingEncounter: request,
                replacePendingEncounter: true,
                lastCheckpointId: "encounter_launch_committed"));
        }

        /// <summary>
        /// Commits the missing Lantern Patrol rescue at the exact Release 071
        /// objective. This is deliberately separate from encounter completion: the
        /// patrol and its Wayglass are secured before the Gate-Eater battle begins.
        /// </summary>
        public Result<CampaignState> MarkFirstHourLanternPatrolRescued071(
            CampaignState campaign,
            GuildCityContent017D content)
        {
            if (campaign == null || content == null)
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_RESCUE_INPUT_REQUIRED");

            var state = campaign.Guild?.GuildCity;
            var contract = state?.ActiveContract;
            var expedition = state?.Expedition;
            if (contract == null || contract.Completed || contract.Failed ||
                !StringComparer.Ordinal.Equals(contract.ContractId, FirstStoryContractId066))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_CONTRACT_REQUIRED");
            if (expedition == null || expedition.Status != ExpeditionStatus017D.Active)
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_ACTIVE_EXPEDITION_REQUIRED");
            if (!StringComparer.Ordinal.Equals(contract.CommitId, expedition.ContractCommitId) ||
                !StringComparer.Ordinal.Equals(contract.BoardId, FirstHourThreeBattleBoardId071) ||
                !StringComparer.Ordinal.Equals(expedition.BoardId, FirstHourThreeBattleBoardId071))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_BOARD_REQUIRED");
            if (!StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13"))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_NODE_REQUIRED");
            if (state.PendingEncounter != null || state.PendingBattleReturn != null)
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_PENDING_BATTLE_BLOCKS_RESCUE");
            if (!content.Boards.TryGetValue(expedition.BoardId, out var board) ||
                !StringComparer.Ordinal.Equals(board.ObjectiveNodeId, "N13"))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_BOARD_REQUIRED");

            var current = board.Node(expedition.CurrentNodeId);
            if (!StringComparer.Ordinal.Equals(current.Id, "N13") ||
                !StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE") ||
                !StringComparer.Ordinal.Equals(current.EncounterId, FirstHourGateEaterEncounterId071))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_OBJECTIVE_REQUIRED");
            if (Contains(expedition.ObjectiveFlags, EncounterClearedFlag("N13")))
                return Result<CampaignState>.Failure("GC017D_FIRST_HOUR_PATROL_RESCUE_TOO_LATE");
            if (HasFirstHourLanternPatrolRescued071(expedition))
                return Result<CampaignState>.Success(campaign);

            var rescued = expedition.With(
                objectiveFlags: AddUnique(
                    expedition.ObjectiveFlags,
                    FirstHourLanternPatrolRescuedFlag071),
                lastCheckpointId: "first_hour071_lantern_patrol_rescued");
            return Success(campaign, state.With(
                expedition: rescued,
                replaceExpedition: true,
                lastCheckpointId: "first_hour071_lantern_patrol_rescued"));
        }

        private static string EncounterObjective071(ContractDefinition017D contract, string encounterId)
        {
            switch (encounterId)
            {
                case "ENCOUNTER071_HALL_BREACH":
                    return "Protect the Guild Hall while Kael contains the breach.";
                case "ENCOUNTER071_LANTERN_ROAD_AMBUSH":
                    return "Break the Lantern Road ambush and reopen the way to the patrol.";
                case "ENCOUNTER071_GATE_EATER":
                    return "Defeat the Gate-Eater before it reaches Skyhome.";
                case "ENCOUNTER_SURVEYOR_RESCUE":
                    return "Break the last false line and free Orra's survey crew at the unrecorded door.";
                default:
                    return contract?.PrimaryObjective ?? string.Empty;
            }
        }

        private static int FirstHourEnemyUnionCount071(
            ContractDefinition017D contract,
            string nodeKind,
            string encounterId)
        {
            switch (encounterId)
            {
                case "ENCOUNTER071_HALL_BREACH": return 2;
                case "ENCOUNTER071_LANTERN_ROAD_AMBUSH": return 2;
                case "ENCOUNTER071_GATE_EATER": return 3;
                default: return EnemyUnionCount(contract, nodeKind);
            }
        }

        public Result<CampaignState> DiscoverGateworksMaintenancePassage066(CampaignState campaign)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_SECRET_INPUT_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var expedition = state.Expedition;
            if (expedition?.PendingQuestFate165 != null)
                return Result<CampaignState>.Failure("QUEST_FATE165_COLLECT_PENDING_FIRST");
            if (expedition == null || expedition.Status != ExpeditionStatus017D.Active)
                return Result<CampaignState>.Failure("GC017D_ACTIVE_EXPEDITION_REQUIRED");
            if (state.ActiveContract == null ||
                !StringComparer.Ordinal.Equals(state.ActiveContract.ContractId, FirstStoryContractId066))
                return Result<CampaignState>.Failure("GC017D_GATEWORKS_CONTRACT_REQUIRED");
            if (!StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N05"))
                return Result<CampaignState>.Failure("GC017D_SECRET_NOT_REACHED");
            if (Contains(expedition.ObjectiveFlags, GateworksMaintenancePassageFlag066))
                return Result<CampaignState>.Success(campaign);

            var updated = expedition.With(
                supplies: checked(expedition.Supplies + 1),
                objectiveFlags: AddUnique(expedition.ObjectiveFlags, GateworksMaintenancePassageFlag066),
                lastCheckpointId: "secret_maintenance_passage_found");
            return Success(campaign, state.With(
                expedition: updated,
                replaceExpedition: true,
                lastCheckpointId: "secret_maintenance_passage_found"));
        }

        public Result<CampaignState> FinalizeCompletedExpedition(CampaignState campaign,
            GuildCityContent017D content)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_FINALIZE_INPUT_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var expedition = state.Expedition;
            var contract = state.ActiveContract;
            if (expedition == null || contract == null) return Result<CampaignState>.Failure("GC017D_OPERATION_REQUIRED");
            if (contract.Completed || contract.Failed) return Result<CampaignState>.Success(campaign);
            if (expedition.Status != ExpeditionStatus017D.Completed &&
                expedition.Status != ExpeditionStatus017D.Extracted &&
                expedition.Status != ExpeditionStatus017D.Failed)
                return Result<CampaignState>.Failure("GC017D_EXPEDITION_NOT_TERMINAL");
            var succeeded = expedition.Status != ExpeditionStatus017D.Failed;
            var definition = content.Contract(contract.ContractId);
            var openingProfile = GuildCityOpeningBalance017G.For(campaign);
            var adjustedGuildXp = openingProfile.AdjustGuildReward(Math.Max(1, definition.BaseGuildXp));
            var adjustedHallXp = openingProfile.AdjustHallReward(Math.Max(1, definition.BaseHallXp));
            var materials = new List<GuildMaterialState017D>(state.Materials);
            var guild = campaign.Guild;
            var development = guild.Development;
            if (succeeded)
            {
                materials = MergeMaterials(materials, ToMaterialStates(definition.MaterialReward));
                var rewardId = "CONTRACT_REWARD_" + contract.CommitId;
                development = development.RecordBattleReward(rewardId, adjustedGuildXp, adjustedHallXp);
                guild = guild.With(
                    checked(guild.TreasuryXp + adjustedGuildXp),
                    guild.Recruits,
                    guild.Unions,
                    guild.Inventory,
                    development);
            }
            var effectService = new GuildCityEffectService017D();
            var assignments = AdvanceAssignments(
                state.MemberAssignments, development, state, content, effectService);
            assignments = RestoreDeployedAssignments(assignments, state.CityPlots, guild.Unions);
            var updatedContract = contract.WithOutcome(succeeded, !succeeded);
            var plots = GuildCityCommandService017D.AdvanceOpeningCityProject(
                state.CityPlots, succeeded ? 30 : 10);
            var updatedCity = state.With(
                operationOrdinal: state.OperationOrdinal + 1,
                civicTrust: state.CivicTrust + (succeeded ? 5 : 0),
                materials: materials.AsReadOnly(),
                memberAssignments: assignments.AsReadOnly(),
                cityPlots: plots,
                activeContract: updatedContract,
                replaceActiveContract: true,
                expedition: null,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true,
                lastCheckpointId: succeeded ? "operation_completed" : "operation_failed_recoverably");
            return Result<CampaignState>.Success(campaign.With(guild.WithGuildCity(updatedCity), campaign.OpeningFlow));
        }

        private static List<GuildMemberAssignmentState017D> AdvanceAssignments(
            IReadOnlyList<GuildMemberAssignmentState017D> source,
            GuildDevelopmentState development,
            GuildCityState017D city,
            GuildCityContent017D content,
            GuildCityEffectService017D effects)
        {
            var result = new List<GuildMemberAssignmentState017D>();
            for (var index = 0; index < source.Count; index++)
            {
                var value = source[index];
                switch (value.Kind)
                {
                    case GuildMemberAssignmentKind017D.Recovering:
                        value = value.With(recoveryProgress: (int)Math.Min(int.MaxValue, checked((long)value.RecoveryProgress +
                            effects.RecoveryProgressPerOperation(development, city, content))));
                        break;
                    case GuildMemberAssignmentKind017D.Training:
                        value = value.With(trainingProgress: value.TrainingProgress +
                            effects.TrainingProgressPerOperation(development, city, content));
                        break;
                    case GuildMemberAssignmentKind017D.Staff:
                        value = value.With(dutyProgress: value.DutyProgress +
                            effects.DutyProgressPerOperation(development, city, content));
                        break;
                }
                result.Add(value);
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.RecruitId, right.RecruitId));
            return result;
        }

        private static IReadOnlyList<GuildMemberAssignmentState017D> MarkParticipatingUnionMembersDeployed(
            IReadOnlyList<GuildMemberAssignmentState017D> source,
            IReadOnlyList<UnionState> unions,
            IReadOnlyList<RecruitState> recruits)
        {
            var deployedIds = new HashSet<string>(StringComparer.Ordinal);
            for (var unionIndex = 0; unionIndex < unions.Count && unionIndex < 10; unionIndex++)
            {
                var union = unions[unionIndex];
                if (union.Kind != UnionKind.Normal || union.MemberRecruitIds.Count == 0) continue;
                for (var memberIndex = 0; memberIndex < union.MemberRecruitIds.Count; memberIndex++)
                {
                    var recruitId = union.MemberRecruitIds[memberIndex];
                    var assignment = FindAssignment(source, recruitId);
                    if (assignment != null &&
                        !GuildMemberDeploymentPolicy017D.IsDeployable(assignment.Kind)) continue;
                    if (ContainsRecruit(recruits, recruitId)) deployedIds.Add(recruitId);
                }
            }
            var result = new List<GuildMemberAssignmentState017D>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < source.Count; index++)
            {
                var value = source[index];
                if (deployedIds.Contains(value.RecruitId))
                    value = value.With(kind: GuildMemberAssignmentKind017D.Deployed);
                result.Add(value);
                seen.Add(value.RecruitId);
            }
            foreach (var recruitId in deployedIds)
                if (!seen.Contains(recruitId))
                    result.Add(new GuildMemberAssignmentState017D(recruitId,
                        GuildMemberAssignmentKind017D.Deployed, string.Empty, 0, 0, 0));
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.RecruitId, right.RecruitId));
            return result.AsReadOnly();
        }

        private static List<GuildMemberAssignmentState017D> RestoreDeployedAssignments(
            IReadOnlyList<GuildMemberAssignmentState017D> source,
            IReadOnlyList<CityPlotState017D> plots,
            IReadOnlyList<UnionState> unions)
        {
            var result = new List<GuildMemberAssignmentState017D>();
            for (var index = 0; index < source.Count; index++)
            {
                var value = source[index];
                if (value.Kind == GuildMemberAssignmentKind017D.Deployed)
                {
                    var buildingId = StaffBuildingForRecruit(plots, value.RecruitId);
                    value = !string.IsNullOrWhiteSpace(buildingId)
                        ? value.With(kind: GuildMemberAssignmentKind017D.Staff, facilityId: buildingId)
                        : value.With(kind: IsUnionMember(unions, value.RecruitId)
                            ? GuildMemberAssignmentKind017D.Active
                            : GuildMemberAssignmentKind017D.Reserve);
                }
                result.Add(value);
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.RecruitId, right.RecruitId));
            return result;
        }

        private static bool HasTrainingMemberInUnionPlans(GuildState guild)
        {
            if (guild == null) return false;
            for (var unionIndex = 0; unionIndex < guild.Unions.Count; unionIndex++)
            {
                var union = guild.Unions[unionIndex];
                if (union.Kind != UnionKind.Normal) continue;
                for (var memberIndex = 0;
                     memberIndex < union.MemberRecruitIds.Count;
                     memberIndex++)
                {
                    if (GuildMemberDeploymentPolicy017D.IsTraining(
                            guild.GuildCity,
                            union.MemberRecruitIds[memberIndex]))
                        return true;
                }
            }
            return false;
        }

        private static string StaffBuildingForRecruit(IReadOnlyList<CityPlotState017D> plots, string recruitId)
        {
            for (var plotIndex = 0; plotIndex < plots.Count; plotIndex++)
                for (var staffIndex = 0; staffIndex < plots[plotIndex].StaffRecruitIds.Count; staffIndex++)
                    if (StringComparer.Ordinal.Equals(plots[plotIndex].StaffRecruitIds[staffIndex], recruitId))
                        return plots[plotIndex].BuildingId;
            return string.Empty;
        }

        private static bool IsUnionMember(IReadOnlyList<UnionState> unions, string recruitId)
        {
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                if (unions[unionIndex].Kind != UnionKind.Normal) continue;
                for (var memberIndex = 0; memberIndex < unions[unionIndex].MemberRecruitIds.Count; memberIndex++)
                    if (StringComparer.Ordinal.Equals(unions[unionIndex].MemberRecruitIds[memberIndex], recruitId))
                        return true;
            }
            return false;
        }

        private static GuildMemberAssignmentState017D FindAssignment(
            IReadOnlyList<GuildMemberAssignmentState017D> values, string recruitId)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].RecruitId, recruitId)) return values[index];
            return null;
        }

        private static IReadOnlyList<string> InitialRevealedNodes(BoardDefinition017D board, int extraRevealCount)
        {
            var revealed = new List<string> { board.StartNodeId };
            if (extraRevealCount <= 0) return revealed.AsReadOnly();
            var queue = new Queue<string>();
            queue.Enqueue(board.StartNodeId);
            while (queue.Count > 0 && revealed.Count < 1 + extraRevealCount)
            {
                var current = board.Node(queue.Dequeue());
                var links = new List<string>(current.Links ?? Array.Empty<string>());
                links.Sort(StringComparer.Ordinal);
                for (var index = 0; index < links.Count && revealed.Count < 1 + extraRevealCount; index++)
                {
                    if (revealed.Contains(links[index])) continue;
                    revealed.Add(links[index]);
                    queue.Enqueue(links[index]);
                }
            }
            revealed.Sort(StringComparer.Ordinal);
            return revealed.AsReadOnly();
        }

        public static bool IsCurrentNodeResolved(ExpeditionState017D expedition,
            BoardNodeDefinition017D current)
        {
            if (expedition == null || current == null) return false;
            if (IsCommittedCheckNode(current))
            {
                for (var index = 0; index < expedition.CommittedChecks.Count; index++)
                    if (StringComparer.Ordinal.Equals(expedition.CommittedChecks[index].NodeId, current.Id))
                        return true;
                return false;
            }
            if (StringComparer.Ordinal.Equals(current.Kind, "ENCOUNTER") ||
                StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE"))
                return IsEncounterCleared(expedition, current);
            return true;
        }

        public static bool IsEncounterCleared(ExpeditionState017D expedition,
            BoardNodeDefinition017D current)
        {
            if (expedition == null || current == null || string.IsNullOrWhiteSpace(current.EncounterId))
                return false;
            return Contains(expedition.ObjectiveFlags, EncounterClearedFlag(current.Id));
        }

        public static bool IsEncounterNode(BoardNodeDefinition017D current) =>
            current != null &&
            (StringComparer.Ordinal.Equals(current.Kind, "ENCOUNTER") ||
             StringComparer.Ordinal.Equals(current.Kind, "OPTIONAL_ELITE") ||
             StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE"));

        public static bool HasFirstHourLanternPatrolRescued071(
            ExpeditionState017D expedition) =>
            HasFirstHourLanternPatrolRescued071(expedition?.ObjectiveFlags);

        public static bool HasFirstHourLanternPatrolRescued071(
            IReadOnlyList<string> objectiveFlags) =>
            objectiveFlags != null &&
            Contains(objectiveFlags, FirstHourLanternPatrolRescuedFlag071);

        public static bool IsFirstHourGateEater071(
            ExpeditionState017D expedition,
            BoardNodeDefinition017D current) =>
            expedition != null && current != null &&
            StringComparer.Ordinal.Equals(expedition.BoardId, FirstHourThreeBattleBoardId071) &&
            StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13") &&
            StringComparer.Ordinal.Equals(current.Id, "N13") &&
            StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE") &&
            StringComparer.Ordinal.Equals(current.EncounterId, FirstHourGateEaterEncounterId071);

        public static bool CanCommitEncounter(
            ExpeditionState017D expedition,
            BoardNodeDefinition017D current) =>
            expedition != null && expedition.Status == ExpeditionStatus017D.Active &&
            IsEncounterNode(current) && !IsEncounterCleared(expedition, current) &&
            (!IsFirstHourGateEater071(expedition, current) ||
             HasFirstHourLanternPatrolRescued071(expedition));

        public static bool CurrentNodeRequiresResolution(BoardNodeDefinition017D current) =>
            current != null &&
            (IsCommittedCheckNode(current) ||
             StringComparer.Ordinal.Equals(current.Kind, "ENCOUNTER") ||
             StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE"));

        public static bool IsCommittedCheckNode(BoardNodeDefinition017D current) =>
            current != null &&
            (StringComparer.Ordinal.Equals(current.Kind, "EVENT") ||
             StringComparer.Ordinal.Equals(current.Kind, "SKILL_CHECK") ||
             (!string.IsNullOrWhiteSpace(current.EventId) &&
               (StringComparer.Ordinal.Equals(current.Kind, "CAMP") ||
                StringComparer.Ordinal.Equals(current.Kind, "SECONDARY_OBJECTIVE") ||
                StringComparer.Ordinal.Equals(current.Kind, "SAFE_ROUTE"))));

        public static string ResolutionHint(BoardNodeDefinition017D current)
        {
            if (current == null) return string.Empty;
            if (StringComparer.Ordinal.Equals(current.Kind, "EVENT"))
                return "Finish this moment before choosing the next road.";
            if (StringComparer.Ordinal.Equals(current.Kind, "SKILL_CHECK"))
                return "Roll 2d6 for this challenge before choosing the next road.";
            if (StringComparer.Ordinal.Equals(current.Kind, "CAMP"))
                return "Resolve the camp conversation before choosing the next road.";
            if (StringComparer.Ordinal.Equals(current.Kind, "SECONDARY_OBJECTIVE"))
                return "Decide how the Guild handles this optional objective before moving on.";
            if (StringComparer.Ordinal.Equals(current.Kind, "SAFE_ROUTE"))
                return "Commit who the Guild protects and what must be left behind before moving on.";
            if (StringComparer.Ordinal.Equals(current.Kind, "ENCOUNTER") ||
                StringComparer.Ordinal.Equals(current.Kind, "MAIN_OBJECTIVE"))
                return "Face the enemy before leaving this place.";
            return string.Empty;
        }

        public static string EncounterClearedFlag(string nodeId) =>
            "ENCOUNTER_CLEARED_" + (nodeId ?? string.Empty);

        public static string EventSuccessFlag076(string eventId) =>
            "EVENT_SUCCESS_" + (eventId ?? string.Empty);

        private static string CurrentNodeResolutionError(ExpeditionState017D expedition,
            BoardNodeDefinition017D current) =>
            CurrentNodeRequiresResolution(current) && !IsCurrentNodeResolved(expedition, current)
                ? "GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"
                : null;

        private static int EnemyUnionCount(ContractDefinition017D contract, string nodeKind)
        {
            var minimum = Math.Max(1, Math.Min(10, contract.ExpectedEnemyUnionMin));
            var maximum = Math.Max(minimum, Math.Min(10, contract.ExpectedEnemyUnionMax));
            if (StringComparer.Ordinal.Equals(nodeKind, "MAIN_OBJECTIVE")) return maximum;
            if (StringComparer.Ordinal.Equals(nodeKind, "OPTIONAL_ELITE"))
                return Math.Min(maximum, minimum + 1);
            return minimum;
        }

        private static int CountChapterTwoEvidence079(
            IReadOnlyList<string> objectiveFlags,
            bool successful)
        {
            var count = 0;
            var flags = objectiveFlags ?? Array.Empty<string>();
            for (var index = 0; index < ChapterTwoEvidenceEventIds079.Length; index++)
            {
                var eventId = ChapterTwoEvidenceEventIds079[index];
                var hasSuccess = Contains(flags, EventSuccessFlag076(eventId));
                if (successful ? hasSuccess : !hasSuccess &&
                    Contains(flags, "EVENT_RESOLVED_" + eventId))
                    count++;
            }
            return count;
        }

        private static int RelationshipStrength079(
            IReadOnlyList<RelationshipMemoryState017D> memories)
        {
            var strength = 0;
            var values = memories ?? Array.Empty<RelationshipMemoryState017D>();
            for (var index = 0; index < values.Count; index++)
                strength = checked(strength + Math.Max(0, values[index]?.Strength ?? 0));
            return strength;
        }

        private static int AuthoredOutcomeTotal079(string outcome)
        {
            if (StringComparer.Ordinal.Equals(outcome, "EXCEPTIONAL")) return 12;
            if (StringComparer.Ordinal.Equals(outcome, "FULL_SUCCESS")) return 10;
            if (StringComparer.Ordinal.Equals(outcome, "SUCCESS_WITH_COST")) return 7;
            return 5;
        }

        private static bool OutcomeHeld079(string outcome) =>
            StringComparer.Ordinal.Equals(outcome, "EXCEPTIONAL") ||
            StringComparer.Ordinal.Equals(outcome, "FULL_SUCCESS") ||
            StringComparer.Ordinal.Equals(outcome, "SUCCESS_WITH_COST");

        private static RecruitState FindRecruit079(
            IReadOnlyList<RecruitState> recruits,
            string recruitId)
        {
            for (var index = 0; index < recruits.Count; index++)
                if (StringComparer.Ordinal.Equals(recruits[index].RecruitId, recruitId))
                    return recruits[index];
            return null;
        }

        private static bool ContainsRecruit(IReadOnlyList<RecruitState> recruits, string recruitId)
        {
            for (var index = 0; index < recruits.Count; index++)
                if (StringComparer.Ordinal.Equals(recruits[index].RecruitId, recruitId)) return true;
            return false;
        }

        private static bool ContainsRecruit(CampaignState campaign, string recruitId)
        {
            for (var index = 0; index < campaign.Guild.Recruits.Count; index++)
                if (StringComparer.Ordinal.Equals(campaign.Guild.Recruits[index].RecruitId, recruitId)) return true;
            return false;
        }

        private static Result<CampaignState> Success(CampaignState campaign, GuildCityState017D city) =>
            Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow));

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index], value)) return true;
            return false;
        }

        private static bool ContainsMemory(IReadOnlyList<RelationshipMemoryState017D> values, string id)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].MemoryId, id)) return true;
            return false;
        }

        private static IReadOnlyList<string> AddUnique(IReadOnlyList<string> source, string value)
        {
            var result = new List<string>(source);
            if (!result.Contains(value)) result.Add(value);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static List<GuildMaterialState017D> ToMaterialStates(
            IReadOnlyList<GuildMaterialDefinition017D> definitions)
        {
            var result = new List<GuildMaterialState017D>();
            if (definitions != null)
                for (var index = 0; index < definitions.Count; index++)
                    result.Add(new GuildMaterialState017D(definitions[index].MaterialId, definitions[index].Amount));
            return result;
        }

        private static List<GuildMaterialState017D> MergeMaterials(
            IReadOnlyList<GuildMaterialState017D> existing,
            IReadOnlyList<GuildMaterialState017D> additions)
        {
            var result = new List<GuildMaterialState017D>(existing);
            for (var index = 0; index < additions.Count; index++)
            {
                var found = false;
                for (var existingIndex = 0; existingIndex < result.Count; existingIndex++)
                {
                    if (!StringComparer.Ordinal.Equals(result[existingIndex].MaterialId, additions[index].MaterialId)) continue;
                    result[existingIndex] = result[existingIndex].WithAmount(
                        result[existingIndex].Amount + additions[index].Amount);
                    found = true;
                    break;
                }
                if (!found) result.Add(additions[index]);
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.MaterialId, right.MaterialId));
            return result;
        }
    }
}
