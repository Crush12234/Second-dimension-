using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign019
{
    public sealed class CampaignCommandService019
    {
        public Result<CampaignState> StartChapter(
            CampaignState campaign,
            ICampaignRuleCatalog019 catalog,
            string chapterId,
            IReadOnlyList<string> alliedUnionIds,
            bool ownerApprovedCandidateOrder)
        {
            if (campaign?.Guild?.GuildCity == null || catalog == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_INPUT_REQUIRED");
            if (!catalog.TryGetChapter(chapterId, out var chapter) ||
                !catalog.TryGetArc(chapter.ArcId, out var arc))
                return Result<CampaignState>.Failure("CAMPAIGN019_CHAPTER_UNKNOWN");

            if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"CAMPAIGN"))return Result<CampaignState>.Failure("Resume the saved Campaign before starting another quest.");
            var city = campaign.Guild.GuildCity;
            if (GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN019_FINISH_ACTIVE_ADVENTURE_FIRST");
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            if (progress.ActiveOperation != null || progress.PendingReceipt != null)
                return Result<CampaignState>.Failure("CAMPAIGN019_OPERATION_ALREADY_ACTIVE");
            if (!CampaignReplayRules130.ValidateState(campaign,out var replayError130))
                return Result<CampaignState>.Failure(replayError130);
            var currentCompleted130=CampaignReplayRules130.CurrentCompleted(progress);
            if (Contains(currentCompleted130, chapterId))
                return Result<CampaignState>.Failure("CAMPAIGN019_CHAPTER_ALREADY_COMPLETED");
            if (!ArcGatesSatisfied(catalog, arc, currentCompleted130, strategic.StoryGates,
                    ownerApprovedCandidateOrder))
                return Result<CampaignState>.Failure("CAMPAIGN019_ARC_LOCKED");

            var chapterIndex = Array.IndexOf(arc.ChapterIds ?? Array.Empty<string>(), chapterId);
            if (chapterIndex < 0)
                return Result<CampaignState>.Failure("CAMPAIGN019_CHAPTER_NOT_IN_ARC");
            if (chapterIndex > 0 && !Contains(currentCompleted130, arc.ChapterIds[chapterIndex - 1]))
                return Result<CampaignState>.Failure("CAMPAIGN019_PREVIOUS_CHAPTER_REQUIRED");

            var unions = CopyOwnedUnions(campaign, alliedUnionIds);
            if ((chapter.BattleRequired || CampaignReplayBattle134.ShouldCommit(progress, chapterId)) && unions.Count == 0)
                return Result<CampaignState>.Failure("CAMPAIGN019_ALLIED_UNION_REQUIRED");

            var mapId = chapter.MapIds != null && chapter.MapIds.Length > 0
                ? chapter.MapIds[0]
                : "MAP019_" + chapter.ChapterId;
            var identity = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                campaign.CampaignSeed,
                chapterId,
                city.OperationOrdinal,
                progress.CampaignProgress,
                unions
            });
            var requestId = "CAMPAIGN019_REQ_" + identity.Substring(0, 24).ToUpperInvariant();
            var objectives = new List<string> { "PRIMARY:" + chapter.PrimaryObjective };
            var cycleTag130=CampaignReplayRules130.FrozenObjectiveTag(progress);
            if(!string.IsNullOrEmpty(cycleTag130)) objectives.Add(cycleTag130);
            if(CampaignReplayBattle134.ShouldCommit(progress, chapterId))
                objectives.Add(CampaignReplayBattle134.ObjectivePolicy);
            var commit = new CampaignOperationCommit019(
                requestId,
                chapter.ChapterId,
                chapter.ArcId,
                chapter.WorldId,
                mapId,
                chapter.SiegeId,
                identity,
                unions.AsReadOnly(),
                objectives.AsReadOnly(),
                "campaign019:" + chapter.ChapterId,
                CanonicalJson.Sha256Hex(campaign),
                false);

            var maps = new List<string>(progress.DiscoveredMapIds);
            AddUnique(maps, mapId);
            var arcs = new List<string>(progress.UnlockedArcIds);
            AddUnique(arcs, chapter.ArcId);
            var worlds = new List<string>(progress.UnlockedWorldIds);
            if (!string.IsNullOrWhiteSpace(chapter.WorldId) &&
                !StringComparer.Ordinal.Equals(chapter.WorldId, "SKYHOME"))
                AddUnique(worlds, chapter.WorldId);
            Sort(arcs); Sort(worlds); Sort(maps);

            progress = progress.With(
                activeArcId: chapter.ArcId,
                activeChapterId: chapter.ChapterId,
                activeOperation: commit,
                replaceActiveOperation: true,
                unlockedArcIds: arcs.AsReadOnly(),
                unlockedWorldIds: worlds.AsReadOnly(),
                discoveredMapIds: maps.AsReadOnly(),
                lastCheckpointId: "campaign019_chapter_committed");
            return Success(campaign, city, strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: "campaign019_chapter_committed"));
        }

        public Result<CampaignState> CommitCertifiedBattle(
            CampaignState campaign,
            ICampaignRuleCatalog019 catalog)
        {
            if (campaign?.Guild?.GuildCity == null || catalog == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_INPUT_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var operation = progress.ActiveOperation;
            if (operation == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_ACTIVE_OPERATION_REQUIRED");
            if (!catalog.TryGetChapter(operation.ChapterId, out var chapter) ||
                !(chapter.BattleRequired || CampaignReplayBattle134.Required(campaign, operation.ChapterId)))
                return Result<CampaignState>.Failure("CAMPAIGN019_BATTLE_NOT_REQUIRED");
            var encounter = CreateCertifiedEncounterRequest019(campaign, operation, chapter,
                newCommit094: !operation.BattleCommitted);

            var encounterAuthority = GuildCityBattleBridgeService017D
                .EncounterRequestAuthorityId084(encounter);
            if (operation.BattleCommitted)
            {
                if (city.PendingEncounter != null &&
                    StringComparer.Ordinal.Equals(
                        CanonicalJson.Sha256Hex(city.PendingEncounter),
                        CanonicalJson.Sha256Hex(encounter)) &&
                    campaign.Guild.Development.HasAdventureAuthority(
                        encounterAuthority))
                    return Result<CampaignState>.Success(campaign);
                return Result<CampaignState>.Failure("CAMPAIGN019_BATTLE_ALREADY_COMMITTED");
            }
            if (city.PendingEncounter != null)
                return Result<CampaignState>.Failure("CAMPAIGN019_PENDING_ENCOUNTER_EXISTS");
            var development = campaign.Guild.Development;
            if (development.HasAdventureAuthority(encounterAuthority))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN019_ENCOUNTER_AUTHORITY_ALREADY_COMMITTED");
            if (!development.CanRecordAdventureAuthority(encounterAuthority))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN019_ADVENTURE_AUTHORITY_LEDGER_FULL");
            development = development.RecordAdventureAuthority(encounterAuthority);
            campaign = campaign.With(campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                development), campaign.OpeningFlow);

            progress = progress.With(
                activeOperation: operation.With(battleCommitted: true),
                replaceActiveOperation: true,
                lastCheckpointId: "campaign019_battle_committed");
            city = city.With(
                pendingEncounter: encounter,
                replacePendingEncounter: true,
                strategic017H: strategic.With(
                    campaign019: progress,
                    replaceCampaign019: true,
                    lastCheckpointId: "campaign019_battle_committed"),
                replaceStrategic017H: true,
                lastCheckpointId: "campaign019_battle_committed");
            return Result<CampaignState>.Success(
                campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow));
        }

        public Result<CampaignState> CommitNonCombatReceipt(
            CampaignState campaign,
            ICampaignRuleCatalog019 catalog,
            string outcome)
        {
            if (campaign?.Guild?.GuildCity == null || catalog == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_INPUT_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var operation = progress.ActiveOperation;
            if (operation == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_ACTIVE_OPERATION_REQUIRED");
            if (!catalog.TryGetChapter(operation.ChapterId, out var chapter))
                return Result<CampaignState>.Failure("CAMPAIGN019_CHAPTER_UNKNOWN");
            if (chapter.BattleRequired || CampaignReplayBattle134.Required(campaign, operation.ChapterId))
                return Result<CampaignState>.Failure("CAMPAIGN019_CERTIFIED_BATTLE_REQUIRED");
            var normalized = NormalizeOutcome(outcome);
            return CommitReceipt(
                campaign,
                chapter,
                operation,
                normalized,
                "NONCOMBAT:" + CanonicalJson.Sha256Hex(new { operation.RequestId, normalized }),
                string.Empty);
        }

        public Result<CampaignState> CommitClaimedBattleReceiptAndClearEncounter(
            CampaignState campaign,
            ICampaignRuleCatalog019 catalog)
        {
            if (campaign?.Guild?.GuildCity == null || catalog == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_INPUT_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var operation = progress.ActiveOperation;
            if (operation == null || !operation.BattleCommitted)
                return Result<CampaignState>.Failure("CAMPAIGN019_BATTLE_COMMIT_REQUIRED");
            if (!catalog.TryGetChapter(operation.ChapterId, out var chapter))
                return Result<CampaignState>.Failure("CAMPAIGN019_CHAPTER_UNKNOWN");
            var expectedEncounter = CreateCertifiedEncounterRequest019(campaign, operation, chapter);
            if (city.PendingEncounter == null ||
                !StringComparer.Ordinal.Equals(
                    CanonicalJson.Serialize(city.PendingEncounter),
                    CanonicalJson.Serialize(expectedEncounter)) ||
                !campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityBattleBridgeService017D
                        .EncounterRequestAuthorityId084(expectedEncounter)))
                return Result<CampaignState>.Failure("CAMPAIGN019_COMMITTED_ENCOUNTER_REQUIRED");
            if (!ValidateClaimedCampaignBattle019(campaign, operation, chapter,
                    out var battleError))
                return Result<CampaignState>.Failure(battleError);

            var committed = CommitReceipt(
                campaign,
                chapter,
                operation,
                campaign.Battle.Outcome.ToString(),
                campaign.Battle.FinalStateHash,
                campaign.Battle.Reward.RewardId);
            if (!committed.IsSuccess) return committed;

            var updatedCampaign = committed.Value;
            var updatedCity = updatedCampaign.Guild.GuildCity.With(
                pendingEncounter: null,
                replacePendingEncounter: true,
                lastCheckpointId: "campaign019_battle_return_committed");
            return Result<CampaignState>.Success(updatedCampaign.With(
                updatedCampaign.Guild.WithGuildCity(updatedCity),
                updatedCampaign.OpeningFlow));
        }

        private Result<CampaignState> CommitReceipt(
            CampaignState campaign,
            CampaignChapterRule019 chapter,
            CampaignOperationCommit019 operation,
            string outcome,
            string resultHash,
            string equipmentRewardId)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var normalized = NormalizeOutcome(outcome);
            var receipt = CreateCanonicalReceipt019(chapter, operation, normalized,
                resultHash, equipmentRewardId);
            if (progress.PendingReceipt != null)
            {
                if (SameReceipt019(progress.PendingReceipt, receipt))
                    return Result<CampaignState>.Success(campaign);
                return Result<CampaignState>.Failure("CAMPAIGN019_DIFFERENT_RECEIPT_ALREADY_PENDING");
            }
            progress = progress.With(
                pendingReceipt: receipt,
                replacePendingReceipt: true,
                lastCheckpointId: "campaign019_receipt_committed");
            return Success(campaign, city, strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: "campaign019_receipt_committed"));
        }

        public Result<CampaignState> ApplyReceiptExactlyOnce(
            CampaignState campaign,
            ICampaignRuleCatalog019 catalog,
            ICampaignPlayableCatalog020 playableCatalog=null)
        {
            if (campaign?.Guild?.GuildCity == null || catalog == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_INPUT_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var receipt = progress.PendingReceipt;
            var operation = progress.ActiveOperation;
            if (receipt == null || operation == null)
                return Result<CampaignState>.Failure("CAMPAIGN019_PENDING_RECEIPT_REQUIRED");
            if (Contains(progress.AppliedReceiptIds, receipt.ReceiptId))
                return Result<CampaignState>.Failure("CAMPAIGN019_RECEIPT_ALREADY_APPLIED");
            if (!StringComparer.Ordinal.Equals(receipt.RequestId, operation.RequestId) ||
                !StringComparer.Ordinal.Equals(receipt.ChapterId, operation.ChapterId))
                return Result<CampaignState>.Failure("CAMPAIGN019_RECEIPT_MISMATCH");
            if (!catalog.TryGetChapter(receipt.ChapterId, out var chapter) ||
                !catalog.TryGetArc(chapter.ArcId, out _))
                return Result<CampaignState>.Failure("CAMPAIGN019_CONTENT_MISSING");
            CampaignOperationReceipt019 expectedReceipt;
            if (chapter.BattleRequired || CampaignReplayBattle134.Required(campaign, operation.ChapterId))
            {
                if (!ValidateClaimedCampaignBattle019(campaign, operation, chapter,
                        out var battleError))
                    return Result<CampaignState>.Failure(battleError);
                var expectedEncounter = CreateCertifiedEncounterRequest019(
                    campaign, operation, chapter);
                if (!campaign.Guild.Development.HasAdventureAuthority(
                        GuildCityBattleBridgeService017D
                            .EncounterRequestAuthorityId084(expectedEncounter)))
                    return Result<CampaignState>.Failure(
                        "CAMPAIGN019_COMMITTED_ENCOUNTER_REQUIRED");
                expectedReceipt = CreateCanonicalReceipt019(chapter, operation,
                    campaign.Battle.Outcome.ToString(),
                    campaign.Battle.FinalStateHash,
                    campaign.Battle.Reward.RewardId);
            }
            else
            {
                var normalized = NormalizeOutcome(receipt.Outcome);
                expectedReceipt = CreateCanonicalReceipt019(chapter, operation,
                    normalized,
                    "NONCOMBAT:" + CanonicalJson.Sha256Hex(new
                    {
                        operation.RequestId,
                        normalized
                    }), string.Empty);
            }
            if (!SameReceipt019(receipt, expectedReceipt))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN019_RECEIPT_AUTHORITY_INVALID");
            var playable=progress.Playable020;
            if(playable?.ActiveOperation!=null)
            {
                if(playableCatalog==null)
                    return Result<CampaignState>.Failure(
                        "CAMPAIGN019_CAMPAIGN020_AUTHORITY_REQUIRED");
                if(!new CampaignPlayableCommandService020()
                       .IsReadyForChapterReceipt084(campaign,playableCatalog,
                           receipt.ChapterId,out var readyError))
                    return Result<CampaignState>.Failure(
                        string.IsNullOrWhiteSpace(readyError)
                            ?"CAMPAIGN019_CAMPAIGN020_NOT_READY":readyError);
            }

            if (!CampaignReplayRules130.ValidateState(campaign,out var replayError130))
                return Result<CampaignState>.Failure(replayError130);
            var completion = IsCompletionOutcome(receipt.Outcome);
            var applied = new List<string>(progress.AppliedReceiptIds);
            AddUnique(applied, receipt.ReceiptId);
            var completed = new List<string>(progress.CompletedChapterIds);
            if (completion) AddUnique(completed, chapter.ChapterId);

            var storyGates = new List<string>(strategic.StoryGates);
            for (var i = 0; i < receipt.StoryGateIds.Count; i++)
                AddUnique(storyGates, receipt.StoryGateIds[i]);
            for (var i = 0; i < receipt.CityUnlockIds.Count; i++)
                AddUnique(storyGates, receipt.CityUnlockIds[i]);
            Sort(storyGates);

            var unlockedArcs = new List<string>(progress.UnlockedArcIds);
            var unlockedWorlds = new List<string>(progress.UnlockedWorldIds);
            var candidateArcIds = new[]
            {
                "ARC018_FIRST_GATE_ECHOES",
                "ARC018_LAST_TREASURY",
                "ARC018_GOBLIN_FORTRESS_WAR",
                "ARC018_GOBLIN_WORLD",
                "ARC018_ORC_WORLD",
                "ARC018_BEASTKIN_WORLD",
                "ARC018_DEMON_WORLD",
                "ARC018_DARK_ELF_WORLD",
                "ARC018_BUNNY_WORLD",
                "ARC018_DOG_WORLD"
            };
            for (var i = 0; i < candidateArcIds.Length; i++)
            {
                if (!catalog.TryGetArc(candidateArcIds[i], out var candidate) ||
                    !ArcGatesSatisfied(catalog, candidate, completed, storyGates, true)) continue;
                AddUnique(unlockedArcs, candidate.ArcId);
                if (!string.IsNullOrWhiteSpace(candidate.WorldId) &&
                    !StringComparer.Ordinal.Equals(candidate.WorldId, "SKYHOME"))
                    AddUnique(unlockedWorlds, candidate.WorldId);
            }
            Sort(unlockedArcs); Sort(unlockedWorlds); Sort(completed); Sort(applied);

            var standings = UpdateWorldStanding(
                progress.WorldStandings,
                chapter.WorldId,
                receipt.Outcome);
            var worldTime = new List<string>(progress.WorldTimeCounters);
            if (completion && StringComparer.Ordinal.Equals(chapter.WorldId, "WORLD_GOBLIN_001"))
                AddUnique(worldTime, "WORLD_GOBLIN_001_COMPLETED_" + chapter.ChapterId);
            Sort(worldTime);

            progress = progress.With(
                activeArcId: chapter.ArcId,
                activeChapterId: string.Empty,
                activeOperation: null,
                replaceActiveOperation: true,
                pendingReceipt: null,
                replacePendingReceipt: true,
                completedChapterIds: completed.AsReadOnly(),
                unlockedArcIds: unlockedArcs.AsReadOnly(),
                unlockedWorldIds: unlockedWorlds.AsReadOnly(),
                appliedReceiptIds: applied.AsReadOnly(),
                worldStandings: standings,
                worldTimeCounters: worldTime.AsReadOnly(),
                campaignProgress: checked(progress.CampaignProgress + receipt.GuildXp + receipt.HallXp),
                lastCheckpointId: "campaign019_receipt_applied",
                runRecovery151: completion?CampaignRunRecoveryCommands151.AfterCompletion(progress,chapter.ChapterId):progress.RunRecovery151,
                replaceRunRecovery151:true,
                replay130: completion?CampaignReplayRules130.WithChapterCompleted(progress,chapter.ChapterId):progress.Replay130,
                replaceReplay130:true);

            var development = campaign.Guild.Development.HasClaimedReward(receipt.ReceiptId)
                ? campaign.Guild.Development
                : campaign.Guild.Development.RecordBattleReward(
                    receipt.ReceiptId,
                    Math.Max(1, receipt.GuildXp),
                    Math.Max(1, receipt.HallXp));
            var materials = MergeMaterials(city.Materials, "MAT_WORLD_CAMPAIGN", receipt.Materials);
            var updatedStrategic = strategic.With(
                storyGates: storyGates.AsReadOnly(),
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: "campaign019_receipt_applied");
            var updatedCity = city.With(
                civicTrust: checked(city.CivicTrust + (completion ? Math.Max(1, receipt.HallXp / 20) : 0)),
                materials: materials,
                strategic017H: updatedStrategic,
                replaceStrategic017H: true,
                lastCheckpointId: "campaign019_receipt_applied");
            var guild = campaign.Guild.With(
                checked(campaign.Guild.TreasuryXp + receipt.GuildXp),
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                development).WithGuildCity(updatedCity);
            var resultCampaign = campaign.With(guild, campaign.OpeningFlow);

            // Campaign chapters are meaningful operations: recovery, training, staffing and the next
            // city project advance in parallel without a separate wait-day action.
            var advanced = new GuildCityCommandService017D().CompleteMeaningfulOperation(resultCampaign);
            if (!advanced.IsSuccess) return advanced;
            resultCampaign = advanced.Value;

            // Create one free, deferable relationship memory from the first two participating recruits.
            var pair = FirstRelationshipPair(resultCampaign, operation.AlliedUnionIds);
            if (pair.Count >= 2 && receipt.RelationshipMemoryIds.Count > 0)
            {
                var memory = new GuildCityCommandService017D().AddRelationshipMemory(
                    resultCampaign,
                    pair[0],
                    pair[1],
                    receipt.RelationshipMemoryIds[0],
                    "Shared campaign memory: " + chapter.PrimaryObjective,
                    completion ? 2 : 1,
                    "REL_SCENE_" + receipt.RelationshipMemoryIds[0]);
                if (memory.IsSuccess) resultCampaign = memory.Value;
            }
            return Result<CampaignState>.Success(resultCampaign);
        }

        private static EncounterLaunchRequest017D CreateCertifiedEncounterRequest019(
            CampaignState campaign, CampaignOperationCommit019 operation,
            CampaignChapterRule019 chapter, bool newCommit094 = false)
        {
            var route = new List<string>
            {
                "CAMPAIGN_WORLD:" + chapter.WorldId,
                "CAMPAIGN_CHAPTER:" + chapter.ChapterId
            };
            if (!string.IsNullOrWhiteSpace(chapter.SiegeId))
                route.Add("FORTRESS_OPERATION:" + chapter.SiegeId);
            var replayThreat130=CampaignReplayThreat130.ParseCommittedRoutes(operation.ObjectiveIds);
            var frozenRoutes130=replayThreat130==null?route.AsReadOnly():
                CampaignReplayThreat130.AppendProfile132(route.AsReadOnly(),replayThreat130);
            var legacy094 = new EncounterLaunchRequest017D(
                "CAMPAIGN_ENCOUNTER_" + operation.RequestId,
                chapter.ChapterId,
                "CAMPAIGN019_" + chapter.ArcId,
                operation.MapId,
                string.IsNullOrWhiteSpace(chapter.SiegeId)
                    ? chapter.ChapterId
                    : chapter.SiegeId,
                "CAMPAIGN_OBJECTIVE_" + chapter.ChapterId,
                CertifiedBattleId130(operation, chapter),
                chapter.PrimaryObjective,
                CampaignReplayThreat130.MinimumUnionCount132(Math.Max(1, Math.Min(10, chapter.EnemyUnionCount)),frozenRoutes130),
                operation.CanonicalSeedIdentity,
                operation.AlliedUnionIds,
                Array.Empty<string>(),
                operation.ObjectiveIds,
                frozenRoutes130,
                6, 0, 0,
                operation.ReturnCheckpointId,
                operation.PreOperationStateHash);
            return EnemyForceProfile094.ForRequest094(campaign, legacy094,
                CampaignReplayThreat130.ForceChapter132(EnemyForceProfile094.ChapterNumber094(chapter.ChapterId),frozenRoutes130), newCommit094);
        }

        // Legacy cycle1 IDs stay byte-for-byte unchanged. Later encounters must
        // identify their immutable commitment, so an earlier claimed battle for
        // the same chapter cannot satisfy this operation's result guard.
        private static string CertifiedBattleId130(CampaignOperationCommit019 operation,
            CampaignChapterRule019 chapter)
        {
            if (CampaignReplayThreat130.ParseCommittedRoutes(operation.ObjectiveIds) == null)
                return chapter.BattleId;
            return chapter.BattleId + "_REPLAY130_" +
                CanonicalJson.Sha256Hex(new { operation.RequestId }).Substring(0, 24).ToUpperInvariant();
        }

        private static bool ValidateClaimedCampaignBattle019(
            CampaignState campaign,
            CampaignOperationCommit019 operation,
            CampaignChapterRule019 chapter,
            out string error)
        {
            error = string.Empty;
            var battle = campaign?.Battle;
            if (battle == null || battle.Phase != BattlePhase.Resolved ||
                battle.Outcome == BattleOutcome.InProgress ||
                battle.Reward == null || !battle.Reward.Claimed)
            {
                error = "CAMPAIGN019_CLAIM_EXISTING_BATTLE_REWARD_FIRST";
                return false;
            }
            if (!StringComparer.Ordinal.Equals(battle.BattleId, CertifiedBattleId130(operation, chapter)))
            {
                error = "CAMPAIGN019_STALE_BATTLE_RESULT";
                return false;
            }
            if (battle.Reward.Outcome != battle.Outcome ||
                !M2BattleCommandService.HasValidFinalStateHash090(battle))
            {
                error = "CAMPAIGN019_BATTLE_AUTHORITY_INVALID";
                return false;
            }
            if (!campaign.Guild.Development.HasClaimedReward(
                    battle.Reward.RewardId))
            {
                error = "CAMPAIGN019_BATTLE_REWARD_LINK_REQUIRED";
                return false;
            }
            var battleUnionIds = battle.PlayerUnions
                .Select(value => value.UnionId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var committedUnionIds = operation.AlliedUnionIds
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!battleUnionIds.SequenceEqual(committedUnionIds,
                    StringComparer.Ordinal))
            {
                error = "CAMPAIGN019_BATTLE_ALLIED_UNION_MISMATCH";
                return false;
            }
            return true;
        }

        private static CampaignOperationReceipt019 CreateCanonicalReceipt019(
            CampaignChapterRule019 chapter,
            CampaignOperationCommit019 operation,
            string outcome,
            string resultHash,
            string equipmentRewardId)
        {
            var normalized = NormalizeOutcome(outcome);
            var identity = CanonicalJson.Sha256Hex(new
            {
                operation.RequestId,
                chapter.ChapterId,
                normalized,
                resultHash,
                equipmentRewardId
            });
            var storyGates = new List<string>();
            var relationship = new List<string>
            {
                "CAMPAIGN_MEMORY_" + identity.Substring(0, 20).ToUpperInvariant()
            };
            var cityUnlocks = new List<string>();
            if (IsCompletionOutcome(normalized))
            {
                AddCanonicalStoryGates(chapter.ChapterId, storyGates);
                if (!string.IsNullOrWhiteSpace(chapter.WorldId) &&
                    !StringComparer.Ordinal.Equals(chapter.WorldId, "SKYHOME"))
                    cityUnlocks.Add("CITY_UNLOCK_WORLD_" + chapter.WorldId);
            }
            return new CampaignOperationReceipt019(
                "CAMPAIGN019_REC_" + identity.Substring(0, 24).ToUpperInvariant(),
                operation.RequestId,
                chapter.ChapterId,
                normalized,
                resultHash,
                equipmentRewardId,
                ScaleReward(chapter.GuildXp, normalized),
                ScaleReward(chapter.HallXp, normalized),
                ScaleReward(chapter.Materials, normalized),
                relationship.AsReadOnly(),
                cityUnlocks.AsReadOnly(),
                storyGates.AsReadOnly(),
                operation.ReturnCheckpointId,
                0);
        }

        private static bool SameReceipt019(
            CampaignOperationReceipt019 actual,
            CampaignOperationReceipt019 expected) =>
            actual != null && expected != null &&
            StringComparer.Ordinal.Equals(CanonicalJson.Serialize(actual),
                CanonicalJson.Serialize(expected));

        public static bool ArcGatesSatisfied(
            ICampaignRuleCatalog019 catalog,
            CampaignArcRule019 arc,
            IReadOnlyList<string> completed,
            IReadOnlyList<string> story,
            bool ownerApproved)
        {
            var gates = arc.UnlockGates ?? Array.Empty<string>();
            for (var i = 0; i < gates.Length; i++)
            {
                var gate = gates[i];
                if (StringComparer.Ordinal.Equals(gate, "OWNER_OR_CAMPAIGN_ORDER_APPROVAL") &&
                    !ownerApproved) return false;
                if (gate.StartsWith("COMPLETE:", StringComparison.Ordinal))
                {
                    var last = catalog.LastChapterId(gate.Substring(9));
                    if (string.IsNullOrWhiteSpace(last) || !Contains(completed, last)) return false;
                }
                if (gate.StartsWith("STORY:", StringComparison.Ordinal) &&
                    !Contains(story, gate.Substring(6))) return false;
            }
            return true;
        }

        private static void AddCanonicalStoryGates(string chapterId, List<string> gates)
        {
            switch (chapterId)
            {
                case "CH018_008": AddUnique(gates, "STORY_GATE_BOOK6_LAST_TREASURY"); break;
                case "CH018_010":
                    AddUnique(gates, "FOUNDERS_SECOND_EVOLUTION_COMPLETE");
                    AddUnique(gates, "STORY_GATE_BOOK6_SECOND_EVOLUTION");
                    break;
                case "CH018_011":
                    AddUnique(gates, "KAEL_IN_ENDLESS_ABYSS");
                    AddUnique(gates, "STORY_GATE_KAEL_ABYSS_DEPARTURE");
                    break;
                case "CH018_012": AddUnique(gates, "STORY_GATE_KAEL_ABYSS_DEPARTURE_AND_SECOND_EVOLUTION"); break;
                case "CH018_013": AddUnique(gates, "STORY_GATE_GOBLIN_FORTRESS_CONTACT"); break;
                case "CH018_018": AddUnique(gates, "STORY_GATE_GOBLIN_WAR_FINAL_WAVE"); break;
                case "CH018_019": AddUnique(gates, "STORY_GATE_GOBLIN_WAR_AFTERMATH"); break;
                case "CH018_021":
                case "CH018_022":
                    AddUnique(gates, "GOBLIN_FORTRESS_CAPTURED");
                    AddUnique(gates, "STORY_GATE_GOBLIN_WAR_CAMPAIGN_COMPLETE");
                    AddUnique(gates, "STORY_GATE_GOBLIN_FORTRESS_CAPTURED");
                    AddUnique(gates, "STORY_GATE_GOBLIN_WORLD_THRESHOLD");
                    break;
            }
        }

        private static IReadOnlyList<CampaignWorldStanding019> UpdateWorldStanding(
            IReadOnlyList<CampaignWorldStanding019> source,
            string worldId,
            string outcome)
        {
            var result = new List<CampaignWorldStanding019>(
                source ?? Array.Empty<CampaignWorldStanding019>());
            if (string.IsNullOrWhiteSpace(worldId) || StringComparer.Ordinal.Equals(worldId, "SKYHOME"))
                return result.AsReadOnly();
            var index = -1;
            for (var i = 0; i < result.Count; i++)
                if (StringComparer.Ordinal.Equals(result[i].WorldId, worldId)) { index = i; break; }
            var current = index >= 0
                ? result[index]
                : new CampaignWorldStanding019(worldId, 0, 25, 0, Array.Empty<string>());
            var completion = IsCompletionOutcome(outcome);
            var defeat = StringComparer.OrdinalIgnoreCase.Equals(outcome, "DEFEAT") ||
                         StringComparer.OrdinalIgnoreCase.Equals(outcome, "FAILURE");
            var next = current.With(
                trust: current.Trust + (completion ? 4 : defeat ? -2 : 0),
                tension: current.Tension + (completion ? -3 : defeat ? 4 : 1),
                civilianSupport: current.CivilianSupport + (completion ? 3 : defeat ? -1 : 0));
            if (index >= 0) result[index] = next; else result.Add(next);
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.WorldId, b.WorldId));
            return result.AsReadOnly();
        }

        private static int ScaleReward(int value, string outcome)
        {
            if (value <= 0) return 0;
            if (IsCompletionOutcome(outcome)) return value;
            if (StringComparer.OrdinalIgnoreCase.Equals(outcome, "RETREAT"))
                return Math.Max(1, value / 2);
            return Math.Max(1, value / 4);
        }

        private static string NormalizeOutcome(string value) =>
            string.IsNullOrWhiteSpace(value) ? "SUCCESS" : value.Trim().ToUpperInvariant();

        private static bool IsCompletionOutcome(string value)
        {
            var outcome = NormalizeOutcome(value);
            return outcome == "VICTORY" || outcome == "SUCCESS" || outcome == "MIXED" ||
                   outcome == "TREATY" || outcome == "SURRENDER" || outcome == "REFORM" ||
                   outcome == "RESCUE" || outcome == "EXPOSED";
        }

        private static List<string> CopyOwnedUnions(
            CampaignState campaign,
            IReadOnlyList<string> requested)
        {
            var result = new List<string>();
            if (requested != null)
            {
                for (var i = 0; i < requested.Count; i++)
                {
                    var id = requested[i];
                    for (var j = 0; j < campaign.Guild.Unions.Count; j++)
                    {
                        var union = campaign.Guild.Unions[j];
                        if (union != null && StringComparer.Ordinal.Equals(union.UnionId, id) &&
                            union.MemberRecruitIds.Count > 0 && !result.Contains(id))
                        {
                            result.Add(id);
                            break;
                        }
                    }
                }
            }
            if (result.Count > 10) result.RemoveRange(10, result.Count - 10);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static IReadOnlyList<string> FirstRelationshipPair(
            CampaignState campaign,
            IReadOnlyList<string> alliedUnionIds)
        {
            var recruitIds = new List<string>();
            if (campaign?.Guild?.Unions == null || alliedUnionIds == null)
                return recruitIds.AsReadOnly();
            for (var i = 0; i < alliedUnionIds.Count && recruitIds.Count < 2; i++)
            {
                for (var u = 0; u < campaign.Guild.Unions.Count && recruitIds.Count < 2; u++)
                {
                    var union = campaign.Guild.Unions[u];
                    if (union == null || !StringComparer.Ordinal.Equals(union.UnionId, alliedUnionIds[i]))
                        continue;
                    for (var m = 0; m < union.MemberRecruitIds.Count && recruitIds.Count < 2; m++)
                    {
                        var recruitId = union.MemberRecruitIds[m];
                        if (!string.IsNullOrWhiteSpace(recruitId) && !recruitIds.Contains(recruitId))
                            recruitIds.Add(recruitId);
                    }
                }
            }
            return recruitIds.AsReadOnly();
        }

        private static IReadOnlyList<GuildMaterialState017D> MergeMaterials(
            IReadOnlyList<GuildMaterialState017D> source,
            string id,
            int amount)
        {
            var result = new List<GuildMaterialState017D>(
                source ?? Array.Empty<GuildMaterialState017D>());
            if (amount > 0)
            {
                var found = false;
                for (var i = 0; i < result.Count; i++)
                {
                    if (!StringComparer.Ordinal.Equals(result[i].MaterialId, id)) continue;
                    result[i] = result[i].WithAmount(result[i].Amount + amount);
                    found = true;
                    break;
                }
                if (!found) result.Add(new GuildMaterialState017D(id, amount));
            }
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.MaterialId, b.MaterialId));
            return result.AsReadOnly();
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value)) return false;
            for (var i = 0; i < values.Count; i++)
                if (StringComparer.Ordinal.Equals(values[i], value)) return true;
            return false;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value)) values.Add(value);
        }

        private static void Sort(List<string> values) => values.Sort(StringComparer.Ordinal);

        private static Result<CampaignState> Success(
            CampaignState campaign,
            GuildCityState017D city,
            GuildCityStrategicState017H strategic)
        {
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: strategic.LastCheckpointId);
            return Result<CampaignState>.Success(
                campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow));
        }
    }
}
