using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Presentation.Release030
{
    [Serializable]
    public sealed class DeepCampaignChapterEvidence093
    {
        public string chapterId, title, worldId, status = "RUNNING", canonicalHash;
        public int cardChoices, savedCardReceipts, battles, battleRounds;
        public long treasuryBefore, treasuryAfter;
        public List<string> cardCategories = new List<string>();
        public List<string> cardReceiptIds = new List<string>();
        public List<string> rewardIds = new List<string>();
    }

    [Serializable]
    public sealed class DeepCampaignBattleEvidence097
    {
        public string battleId, chapterId, label, outcome;
        public int playerMembers, enemyMembers, rounds, damagingHits, restorations, crossUnionRestorations, revivals;
        public List<string> usedPlayerArtIds = new List<string>();
        public List<string> usedEnemyArtIds = new List<string>();
        public List<string> eventTypes = new List<string>();
    }

    [Serializable]
    public sealed class DeepCampaignReport093
    {
        public string schema = "SECOND_DIMENSION_SEQUENTIAL_CAMPAIGN_093_1";
        public string status = "RUNNING", sourceSave, sourceSha256, isolatedSave, evidenceDirectory;
        public string startedUtc, completedUtc, lastAction, failure, activeChapter, activeNode;
        public string initialCanonicalHash, finalCanonicalHash;
        public string verificationScope = "Actual shipping coordinator commands and certified combat; no rendered-player or art certification.";
        public int targetChapter, completedThroughChapter, saveReloadChecks, replayChecks;
        public int commands, worldGateProofs, claimedBattleRewards;
        public List<string> completedAtStart = new List<string>();
        public List<string> commandsExecuted = new List<string>();
        public List<DeepCampaignChapterEvidence093> openingPrerequisites = new List<DeepCampaignChapterEvidence093>();
        public List<DeepCampaignChapterEvidence093> chapters = new List<DeepCampaignChapterEvidence093>();
        public List<DeepCampaignBattleEvidence097> actualBattles097 = new List<DeepCampaignBattleEvidence097>();
    }

    /// <summary>
    /// Explicit QA runner over an untouched copy of an earned save. It never creates
    /// campaign flags, recruits, XP, health, battle outcomes or receipt authorities.
    /// All mutations go through the same coordinator commands as player input.
    /// A real gate, illegal command, defeat or stalled battle is a recorded blocker.
    /// </summary>
    public sealed class DeepCampaignVerification093
    {
        private readonly AtomicSaveStore _saveStore = new AtomicSaveStore();
        private M1RuntimeCoordinator _coordinator;
        private DeepCampaignReport093 _report;
        private DeepCampaignChapterEvidence093 _chapter;
        private string _contentRoot;
        private Action<string> _log;

        public static string IsolatedSavePath093(string sourceSave, string evidenceDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourceSave) || string.IsNullOrWhiteSpace(evidenceDirectory))
                throw new ArgumentException("An existing earned save and a new evidence directory are required.");
            var source = Path.GetFullPath(sourceSave);
            var directory = Path.GetFullPath(evidenceDirectory).TrimEnd(Path.DirectorySeparatorChar);
            if (StringComparer.OrdinalIgnoreCase.Equals(directory,
                    Path.GetPathRoot(directory)?.TrimEnd(Path.DirectorySeparatorChar)))
                throw new ArgumentException("A drive root is not an isolated evidence directory.");
            var destination = Path.Combine(directory, "CampaignSave093.json");
            if (StringComparer.OrdinalIgnoreCase.Equals(source, destination) ||
                StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(source), directory))
                throw new ArgumentException("Never run against the original save directory; use a new evidence directory.");
            if (File.Exists(destination))
                throw new IOException("The isolated save already exists. Resume it as the source of a new evidence run.");
            return destination;
        }

        public DeepCampaignReport093 Run(string contentRoot, string sourceSave,
            string evidenceDirectory, int targetChapter = 50, Action<string> log = null)
        {
            if (targetChapter < 1 || targetChapter > 82) throw new ArgumentOutOfRangeException(nameof(targetChapter));
            var isolated = IsolatedSavePath093(sourceSave, evidenceDirectory);
            if (!File.Exists(sourceSave)) throw new FileNotFoundException("Earned source save not found.", sourceSave);
            _contentRoot = contentRoot;
            _log = log ?? Debug.Log;
            _report = new DeepCampaignReport093
            {
                sourceSave = Path.GetFullPath(sourceSave), sourceSha256 = FileHash093(sourceSave),
                isolatedSave = isolated, evidenceDirectory = Path.GetFullPath(evidenceDirectory),
                targetChapter = targetChapter, startedUtc = DateTime.UtcNow.ToString("O")
            };
            Directory.CreateDirectory(_report.evidenceDirectory);
            File.Copy(_report.sourceSave, isolated, false);
            try
            {
                var initial = ReadSave();
                _report.initialCanonicalHash = initial.CanonicalStateHash;
                _report.completedAtStart = initial.CampaignState.Guild.GuildCity.Strategic017H
                    .Campaign019.CompletedChapterIds.ToList();
                Reload("initial earned checkpoint");
                CompleteMissingOpeningPrerequisite093();
                for (var number = 1; number <= targetChapter; number++)
                {
                    var id = "CH018_" + number.ToString("000");
                    var view = _coordinator.Campaign019;
                    Require(view.IsAvailable, "Campaign catalog unavailable: " + view.Error);
                    var chapter = view.Chapters.FirstOrDefault(value => value.ChapterId == id);
                    Require(chapter != null, "Missing authored chapter " + id);
                    if (!chapter.Completed) CompleteChapter(id, chapter.Title, chapter.WorldId);
                    Require(_coordinator.Campaign019.Chapters.Any(value => value.ChapterId == id && value.Completed),
                        "Normal chapter receipt did not complete " + id);
                    _report.completedThroughChapter = number;
                    WriteReport();
                }
                _report.status = "PASS";
            }
            catch (Exception exception)
            {
                _report.status = "BLOCKED";
                _report.failure = exception.ToString();
                if (_chapter != null && _chapter.status != "PASS") _chapter.status = "BLOCKED";
                _log("DEEP CAMPAIGN 093 BLOCKED at " + _report.lastAction + ": " + exception.Message);
            }
            finally
            {
                _report.completedUtc = DateTime.UtcNow.ToString("O");
                try
                {
                    var final = ReadSave();
                    _report.finalCanonicalHash = final.CanonicalStateHash;
                    var progress = final.CampaignState.Guild.GuildCity.Strategic017H.Campaign019;
                    _report.activeChapter = progress.ActiveChapterId;
                    _report.activeNode = progress.Playable020.WorldGate023.ActiveOperation?.CurrentNodeId ?? "";
                    _report.worldGateProofs = progress.Playable020.WorldGate023.CompletionProofs.Count;
                    _report.claimedBattleRewards = final.CampaignState.Guild.Development.ClaimedBattleRewardIds.Count;
                    Require(FileHash093(_report.sourceSave) == _report.sourceSha256,
                        "The source save changed during verification; evidence cannot certify isolation.");
                }
                catch (Exception exception)
                {
                    _report.status = "BLOCKED";
                    _report.failure += "\nFinal evidence check: " + exception;
                }
                WriteReport();
            }
            return _report;
        }

        private void CompleteMissingOpeningPrerequisite093()
        {
            const string contractId = "CONTRACT_RELIEF_ROAD";
            var progress = ReadSave().CampaignState.Guild.GuildCity.Strategic017H.Campaign019;
            // Continuing an already earned catalog chapter never sends the player
            // back through the opening. R54 itself ends immediately after contract 2.
            if (progress.CompletedChapterIds.Count > 0 || progress.ActiveOperation != null) return;
            var city = _coordinator.GuildCity017D;
            var third = city.Contracts.FirstOrDefault(value => value.ContractId == contractId);
            var thirdCompleted = third?.IsCompleted == true ||
                (third?.IsFailed != true && third?.IsActive != true && city.OperationOrdinal >= 3 &&
                 city.ClaimedBattleRewardCount >= 3);
            if (thirdCompleted) return;
            var previous = city.Contracts.FirstOrDefault(value => value.ContractId == "CONTRACT_LINES_NOT_RETURNED");
            Require(previous?.IsCompleted == true || (previous?.IsFailed != true && previous?.IsActive != true &&
                    city.OperationOrdinal >= 2 && city.ClaimedBattleRewardCount >= 2),
                "The source checkpoint has not earned opening contract 2; finish its actual quest before deep verification.");
            Require(!city.HasActiveContract || third?.IsActive == true,
                "Another opening contract is still active; finish it before the Relief Road prerequisite.");
            _chapter = new DeepCampaignChapterEvidence093
            {
                chapterId = "OPENING_RELIEF_ROAD", title = "Relief Convoy Through Broken Roadworks",
                worldId = "SKYHOME", treasuryBefore = ReadSave().CampaignState.Guild.TreasuryXp
            };
            _report.openingPrerequisites.Add(_chapter);
            _log("DEEP CAMPAIGN 093 opening prerequisite: finish the real Relief Road contract first.");
            if (!city.HasActiveContract)
                Command("accept third opening contract", () => _coordinator.AcceptGuildCityContract017D(contractId));
            if (_coordinator.GuildCity017D.Expedition == null)
                Command("start third opening quest", () => _coordinator.StartGuildCityExpedition017D());
            for (var guard = 0; guard < 160; guard++)
            {
                city = _coordinator.GuildCity017D;
                var expedition = city.Expedition;
                Require(expedition != null, "Relief Road expedition disappeared before its actual return.");
                if (expedition.CanFinalizeOperation)
                {
                    Require(expedition.Status == "Completed", "Relief Road ended in " + expedition.Status);
                    // The view's NEXT-round counter is zero once the final row
                    // disappears. Count the committed receipt flags, not that label.
                    _chapter.savedCardReceipts = GuildCityExpeditionService017D.QuestCardRoundCount090(
                        ReadSave().CampaignState.Guild.GuildCity.Expedition.ObjectiveFlags);
                    Require(_chapter.savedCardReceipts >= 10, "Relief Road ended before ten actual card choices.");
                    Command("finalize third opening contract", () => _coordinator.FinalizeGuildCityOperation017D());
                    AssertReplayUnchanged("replay third contract reward", () => _coordinator.FinalizeGuildCityOperation017D());
                    Reload("earned opening prerequisite complete");
                    var earned = ReadSave();
                    Require(earned.CampaignState.Guild.GuildCity.ActiveContract?.ContractId == contractId &&
                        earned.CampaignState.Guild.GuildCity.ActiveContract.Completed,
                        "The third opening contract was not completed by its real return.");
                    _chapter.status = "PASS";
                    _chapter.canonicalHash = earned.CanonicalStateHash;
                    _chapter.treasuryAfter = earned.CampaignState.Guild.TreasuryXp;
                    File.Copy(_report.isolatedSave, Path.Combine(_report.evidenceDirectory,
                        "OPENING_RELIEF_ROAD_earned_save.json"), false);
                    WriteReport();
                    _chapter = null;
                    return;
                }
                if (city.HasPendingEncounter || city.HasUnclaimedBattleReward)
                {
                    if (city.HasPendingEncounter && !city.HasUnclaimedBattleReward && !city.IsCertifiedEncounterBattle)
                        Command("enter committed opening battle", () => _coordinator.StartCommittedGuildCityBattle017D());
                    RunBattle("opening prerequisite battle");
                    continue;
                }
                if (expedition.CanCommitEncounter)
                {
                    Command("commit locked opening encounter " + expedition.CurrentEncounterId,
                        () => _coordinator.CommitGuildCityEncounter017D(expedition.CurrentEncounterId));
                    continue;
                }
                if (expedition.RequiresResolution && !expedition.ResolutionComplete)
                {
                    ResolveOpeningCheck093(city);
                    continue;
                }
                var cards = (city.QuestCards090 ?? Array.Empty<GuildQuestCardView090>())
                    .Where(value => value.CanChoose).ToArray();
                Require(cards.Length > 0, "No legal opening card or required action at " + expedition.CurrentNodeId +
                    ": " + expedition.ResolutionHint);
                var card = cards[Math.Max(0, city.QuestCardRound090) % cards.Length];
                Command("choose opening card " + card.CardId + " " + card.Category,
                    () => _coordinator.CommitBoardQuestCard090(card.CardId));
                _chapter.cardChoices++;
                _chapter.cardCategories.Add(card.Category);
                var receipt = GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + card.CardId;
                Require(ReadSave().CampaignState.Guild.Development.HasAdventureAuthority(receipt),
                    "Opening card did not record its real reward authority.");
                _chapter.cardReceiptIds.Add(receipt);
                AssertReplayUnchanged("replay opening card", () => _coordinator.CommitBoardQuestCard090(card.CardId));
                WriteReport();
            }
            throw new InvalidOperationException("Relief Road exceeded its actual gameplay guard.");
        }

        private void ResolveOpeningCheck093(GuildCityPresentationState017D city)
        {
            // Match BoardQuestCrewFor081 and its automatic best-team choice. These
            // are public shipping preview rules, not fabricated successful dice.
            var state = _coordinator.State;
            var active = new HashSet<string>(state.Unions.SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
            var recruits = state.Recruits.ToDictionary(value => value.RecruitId, StringComparer.Ordinal);
            var skills = city.Expedition.CurrentEventEligibleSkills;
            var crew = city.Assignments.Where(value => !string.IsNullOrWhiteSpace(value.RecruitId) &&
                    (active.Count == 0 || active.Contains(value.RecruitId)))
                .OrderByDescending(value => recruits.TryGetValue(value.RecruitId, out var recruit)
                    ? M1FlowPresenter.ExpeditionLeadSuitabilityScore076(recruit.ObservedClass, skills) : 0)
                .ThenBy(value => value.RecruitName, StringComparer.Ordinal).Take(2)
                .Select(value => new ChapterTwoCrewCandidate079
                {
                    RecruitId = value.RecruitId, DisplayName = value.RecruitName,
                    ObservedClass = recruits.TryGetValue(value.RecruitId, out var recruit) ? recruit.ObservedClass : ""
                }).ToArray();
            Require(crew.Length > 0, "The opening check has no deployed crew.");
            var fast = ChapterTwoCrewMechanics079.BuildCheckPreview079(crew[0], null, skills, 1, city.CampaignModeId, true);
            var partner = crew.Length > 1 ? crew[1] : null;
            var team = ChapterTwoCrewMechanics079.BuildCheckPreview079(crew[0], partner, skills, 0, city.CampaignModeId, true);
            var flags = city.Expedition.ObjectiveFlags;
            var bonus = GuildCityExpeditionService017D.BoardRoomSkillBonus081(flags) +
                GuildCityExpeditionService017D.BoardRoomBlessingBonus081(flags) +
                GuildCityExpeditionService017D.QuestCardRunCheckModifier090(flags);
            var useTeam = BoardQuestRules081.ShouldUseBestTeam081(partner != null &&
                GuildCityExpeditionService017D.CanUseCarefulApproach076(city.Expedition.Urgency),
                team.EffectiveModifier + bonus, fast.EffectiveModifier + bonus);
            var eventId = string.IsNullOrWhiteSpace(city.Expedition.CurrentEventId)
                ? "EVENT_COLLAPSED_HANDRAIL" : city.Expedition.CurrentEventId;
            Command("roll real opening dice " + eventId, () => _coordinator.ResolveGuildCityCheck017D(
                eventId, crew[0].RecruitId, useTeam ? partner.RecruitId : "",
                (useTeam ? team.CommandModifier : fast.CommandModifier) + bonus));
        }

        private void CompleteChapter(string chapterId, string title, string worldId)
        {
            _chapter = new DeepCampaignChapterEvidence093
            {
                chapterId = chapterId, title = title, worldId = worldId,
                treasuryBefore = ReadSave().CampaignState.Guild.TreasuryXp
            };
            _report.chapters.Add(_chapter);
            _log("DEEP CAMPAIGN 093 START " + chapterId + " " + title);
            var active = _coordinator.CampaignPlayable020;
            if (string.IsNullOrWhiteSpace(active.ActiveOperationId))
                Command("start " + chapterId, () => _coordinator.StartPlayableChapter020(chapterId));
            else Require(active.ActiveChapterId == chapterId, "Another earned chapter is still active: " + active.ActiveChapterId);

            for (var guard = 0; guard < 64; guard++)
            {
                var state = _coordinator.CampaignPlayable020;
                Require(state.IsAvailable, "Playable operation unavailable: " + state.Error);
                Require(state.ActiveChapterId == chapterId, "Active chapter unexpectedly changed.");
                if (!string.IsNullOrEmpty(state.PendingStepReceiptId))
                {
                    Command("apply pending chapter step", () => _coordinator.ApplyPlayableStep020());
                    continue;
                }
                if (state.Status == "ReadyToFinalize") break;
                var step = state.Steps.FirstOrDefault(value => value.Status == "CURRENT");
                Require(step != null, "No current authored chapter step: " + state.Status);
                if (step.IsWorldBoard || step.Kind == "WORLD_BOARD") DriveWorldGate(chapterId, worldId);
                else if (step.RequiresCertifiedBattle)
                {
                    // A resumed real battle/reward remains owned by this step.
                    if (state.Status != "AwaitingBattle" && !state.BattleInProgress && !state.AwaitingBattleRewardClaim &&
                        !state.ExistingBattleRewardReferenced)
                        Command("enter locked story battle " + step.StepId, () => _coordinator.EnterPlayableBattle020());
                    RunBattle("locked story battle " + step.StepId);
                    Command("commit locked battle step", () => _coordinator.CommitPlayableBattleStepResult020());
                    Command("apply locked battle step", () => _coordinator.ApplyPlayableStep020());
                }
                else
                {
                    Command("commit story step " + step.StepId, () => _coordinator.CommitPlayableStep020("SUCCESS"));
                    Command("apply story step " + step.StepId, () => _coordinator.ApplyPlayableStep020());
                }
            }
            Require(_coordinator.CampaignPlayable020.Status == "ReadyToFinalize", "Chapter exceeded its normal step guard.");
            Command("finalize chapter " + chapterId, () => _coordinator.FinalizePlayableChapter020());
            Reload("committed chapter completion " + chapterId);
            Command("apply chapter completion " + chapterId, () => _coordinator.ApplyPlayableChapterResult020());
            AssertReplayUnchanged("replay chapter completion", () => _coordinator.ApplyPlayableChapterResult020());
            Reload("closed chapter " + chapterId);
            var saved = ReadSave();
            var progress = saved.CampaignState.Guild.GuildCity.Strategic017H.Campaign019;
            Require(progress.CompletedChapterIds.Count(value => value == chapterId) == 1,
                "Chapter completion must exist exactly once.");
            Require(progress.Playable020.WorldGate023.CompletionProofs.Count(value => value.DefinitionId == chapterId) == 1,
                "Chapter must retain exactly one real World Gate completion proof.");
            Require(progress.ActiveOperation == null && progress.Playable020.ActiveOperation == null &&
                progress.Playable020.WorldGate023.ActiveOperation == null, "Chapter did not close its actual operations.");
            _chapter.canonicalHash = saved.CanonicalStateHash;
            _chapter.treasuryAfter = saved.CampaignState.Guild.TreasuryXp;
            _chapter.status = "PASS";
            File.Copy(_report.isolatedSave, Path.Combine(_report.evidenceDirectory, chapterId + "_earned_save.json"), false);
            WriteReport();
            _log("DEEP CAMPAIGN 093 COMPLETE " + chapterId + " cards=" + _chapter.savedCardReceipts +
                 " battles=" + _chapter.battles + " rounds=" + _chapter.battleRounds);
        }

        private void DriveWorldGate(string chapterId, string worldId)
        {
            var state = _coordinator.CampaignWorldGate023;
            Require(state.IsAvailable, "World Gate unavailable: " + state.Error);
            if (string.IsNullOrEmpty(state.ActiveOperationId))
            {
                if (state.CurrentWorldId != worldId)
                    Command("travel to earned world " + worldId, () => _coordinator.TravelWorldGate023(worldId));
                Command("begin shuffled chapter deck", () => _coordinator.BeginWorldGateOperation023(chapterId));
            }
            var checkedPendingReload = false;
            for (var guard = 0; guard < 160; guard++)
            {
                state = _coordinator.CampaignWorldGate023;
                Require(state.ActiveDefinitionId == chapterId, "Another board is still active.");
                if (state.ActiveStatus == "ReadyToFinalize")
                {
                    var deck = ReadSave().CampaignState.Guild.GuildCity.Strategic017H.Campaign019
                        .Playable020.WorldGate023.ActiveOperation.ExpeditionDeck089;
                    _chapter.savedCardReceipts = deck?.AppliedReceiptIds.Count ?? 0;
                    Require(_chapter.savedCardReceipts >= Math.Max(10, state.DeckMinimumChoiceRounds),
                        "Chapter ended before the required ten real card-choice rounds.");
                    Command("finalize chapter board", () => _coordinator.FinalizeWorldGateOperation023());
                    return;
                }
                if (!string.IsNullOrEmpty(state.PendingReceiptId))
                {
                    if (!checkedPendingReload)
                    {
                        var pending = state.PendingReceiptId;
                        Reload("committed card before reveal");
                        Require(_coordinator.CampaignWorldGate023.PendingReceiptId == pending,
                            "Reload changed a committed card receipt.");
                        checkedPendingReload = true;
                        state = _coordinator.CampaignWorldGate023;
                    }
                    var receiptId = state.PendingReceiptId;
                    if (state.PendingRequiresCertifiedBattle)
                    {
                        Command("enter chosen optional battle", () => _coordinator.EnterExpeditionCardBattle089());
                        RunBattle("optional card battle");
                        // ClaimBattleRewards applies the genuine optional-card return.
                    }
                    else Command("apply card receipt " + receiptId, () => _coordinator.ApplyWorldGateReceipt023());
                    var saved = ReadSave().CampaignState;
                    Require(saved.Guild.Development.HasAdventureAuthority(receiptId),
                        "The committed card reward has no applied authority: " + receiptId);
                    _chapter.cardReceiptIds.Add(receiptId);
                    AssertReplayUnchanged("replay applied card reward", () => _coordinator.ApplyWorldGateReceipt023());
                    WriteReport();
                    continue;
                }
                if (state.CurrentNode?.RequiresBattle == true)
                {
                    Command("enter locked board battle", () => _coordinator.EnterWorldGateBattle023());
                    RunBattle("locked board battle");
                    continue;
                }
                var card = ChooseLegalCard093(state.RouteCards, state.DeckChoiceRound);
                Require(card != null, "No legal visible route card. " + string.Join("; ",
                    (state.RouteCards ?? Array.Empty<ExpeditionRouteCardView089>()).Select(value => value.Title + ": " + value.LockedReason)));
                Command("choose card " + card.CardId + " " + card.Category,
                    () => _coordinator.CommitExpeditionRouteCard089(card.CardId));
                _chapter.cardChoices++;
                _chapter.cardCategories.Add(card.Category);
            }
            throw new InvalidOperationException("Chapter deck exceeded its legal route guard.");
        }

        public static ExpeditionRouteCardView089 ChooseLegalCard093(
            IReadOnlyList<ExpeditionRouteCardView089> cards, int round)
        {
            var legal = (cards ?? Array.Empty<ExpeditionRouteCardView089>())
                .Where(value => value != null && value.CanChoose && !string.IsNullOrWhiteSpace(value.CardId)).ToArray();
            return legal.Length == 0 ? null : legal[Math.Max(0, round) % legal.Length];
        }

        private void RunBattle(string label)
        {
            Reload("saved battle planning " + label);
            var first = _coordinator.State.Battle;
            Require(first != null, "A certified battle was not created.");
            var battleId = first.BattleId;
            var evidence = new DeepCampaignBattleEvidence097
            { battleId = battleId, chapterId = _chapter?.chapterId ?? "", label = label };
            _report.actualBattles097.Add(evidence);
            CaptureBattle097(evidence, ReadSave().CampaignState.Battle);
            for (var guard = 0; guard < 128; guard++)
            {
                var battle = _coordinator.State.Battle;
                Require(battle != null && battle.BattleId == battleId, "Battle identity changed during resolution.");
                if (battle.IsResolved)
                {
                    Require(battle.Outcome == "Victory", "Real combat ended in " + battle.Outcome + "; no forced victory or healing permitted.");
                    Require(battle.Reward != null, "Resolved battle has no real reward.");
                    if (!battle.Reward.Claimed)
                    {
                        Require(battle.Reward.CanClaim, "Victory reward is not claimable.");
                        Reload("resolved battle before reward claim");
                        var pending = ReadSave().CampaignState;
                        var item = pending.Battle.Reward.EquipmentReward;
                        var rewardId = pending.Battle.Reward.RewardId;
                        Require(!pending.Guild.Development.HasClaimedReward(rewardId), "Unclaimed reward already exists in ledger.");
                        Command("claim real victory " + battleId, () => _coordinator.ClaimBattleRewards());
                        var claimed = ReadSave().CampaignState;
                        Require(claimed.Guild.Development.ClaimedBattleRewardIds.Count(value => value == rewardId) == 1,
                            "Battle reward did not apply exactly once.");
                        if (item != null) Require(CountOwned093(claimed, item.InstanceId) == 1,
                            "Battle item must occur exactly once across inventory and equipped slots.");
                        _chapter.rewardIds.Add(rewardId);
                        AssertReplayUnchanged("replay battle reward", () => _coordinator.ClaimBattleRewards());
                        Reload("claimed victory " + battleId);
                    }
                    _chapter.battles++;
                    return;
                }
                Require(M2BattleAutoOrders091.SelectCompletePlan(_coordinator, out var error),
                    "Existing Auto cannot select a complete legal Union plan: " + error);
                Command("resolve real battle round " + battleId + "/" + battle.Round,
                    () => _coordinator.ConfirmBattleRound());
                _chapter.battleRounds++;
                CaptureBattle097(evidence, ReadSave().CampaignState.Battle);
                WriteReport();
            }
            throw new InvalidOperationException("Real battle stalled beyond 128 rounds: " + battleId);
        }

        private static int CountOwned093(CampaignState campaign, string instanceId) =>
            campaign.Guild.Inventory.Count(item => item.InstanceId == instanceId) +
            campaign.Guild.Recruits.Sum(recruit => EquipmentSlotIds.All.Count(slot =>
                recruit.Equipment.Find(slot)?.Item?.InstanceId == instanceId));

        // Evidence only: observe committed shipping round records. Never change
        // battle state or infer a successful heal/revival from a candidate preview.
        public static void CaptureBattle097(DeepCampaignBattleEvidence097 evidence, BattleState battle)
        {
            if (evidence == null || battle == null) return;
            evidence.playerMembers = Math.Max(evidence.playerMembers, battle.PlayerUnions.Sum(value => value.Members.Count));
            evidence.enemyMembers = Math.Max(evidence.enemyMembers, battle.EnemyUnions.Sum(value => value.Members.Count));
            evidence.rounds = battle.RoundRecords.Count;
            evidence.outcome = battle.Outcome.ToString();
            var events = battle.RoundRecords.SelectMany(value => value.Events).ToArray();
            evidence.damagingHits = events.Count(value => value.Amount > 0 &&
                (value.EventType == "MARTIAL_HIT" || value.EventType == "MYSTIC_HIT" ||
                 value.EventType == "TACTICAL_HIT" || value.EventType == "ENEMY_HIT"));
            evidence.restorations = events.Count(value => value.EventType == "RESTORATION" && value.Amount > 0);
            evidence.crossUnionRestorations = events.Count(value => value.EventType == "RESTORATION" && value.Amount > 0 &&
                !string.IsNullOrEmpty(value.ActorUnionId) && !string.IsNullOrEmpty(value.TargetUnionId) &&
                value.ActorUnionId != value.TargetUnionId);
            evidence.revivals = events.Count(value => value.EventType == "REVIVED");
            evidence.usedPlayerArtIds = events.Where(value => value.Side == BattleSide.Player &&
                    !string.IsNullOrEmpty(value.ArtId) && value.EventType == "ART_GROWTH")
                .Select(value => value.ArtId).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToList();
            evidence.usedEnemyArtIds = events.Where(value => value.Side == BattleSide.Enemy &&
                    !string.IsNullOrEmpty(value.ArtId) && value.EventType == "ART_GROWTH")
                .Select(value => value.ArtId).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToList();
            evidence.eventTypes = events.Select(value => value.EventType).Distinct()
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        private void Command(string label, Func<M1CommandResult> action)
        {
            _report.lastAction = (_chapter?.chapterId ?? "INIT") + ": " + label;
            var result = action();
            Require(result != null && result.Succeeded, _report.lastAction + ": " + result?.Message);
            _report.commands++;
            _report.commandsExecuted.Add(_report.lastAction);
        }

        private void AssertReplayUnchanged(string label, Func<M1CommandResult> action)
        {
            var before = ReadSave().CanonicalStateHash;
            _report.lastAction = (_chapter?.chapterId ?? "INIT") + ": " + label;
            action(); // A rejected replay is equally safe; neither may change state.
            Require(ReadSave().CanonicalStateHash == before && _coordinator.State.CanonicalStateHash == before,
                "Replay changed earned state: " + label);
            _report.replayChecks++;
        }

        private void Reload(string label)
        {
            _report.lastAction = label;
            var before = ReadSave().CanonicalStateHash;
            _coordinator = new M1RuntimeCoordinator(_contentRoot, _report.isolatedSave);
            Require(_coordinator.State.CanonicalStateHash == before && ReadSave().CanonicalStateHash == before,
                "Coordinator reload changed or failed to load canonical earned state: " + label);
            _report.saveReloadChecks++;
        }

        private SaveEnvelopeV1 ReadSave()
        {
            var loaded = _saveStore.ReadWithRecovery(_report.isolatedSave);
            Require(loaded.IsSuccess, "Save read failed: " + string.Join("; ", loaded.Errors));
            Require(CanonicalJson.Sha256Hex(loaded.Value.CampaignState) == loaded.Value.CanonicalStateHash,
                "The atomic save canonical hash is invalid.");
            return loaded.Value;
        }

        private void WriteReport() => File.WriteAllText(Path.Combine(_report.evidenceDirectory,
            "deep_campaign093_report.json"), JsonUtility.ToJson(_report, true));

        private static string FileHash093(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
