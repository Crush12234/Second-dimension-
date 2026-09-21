using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign020.ICampaignDeckPresentationCoordinator131
    {
        public M1CommandResult StartOrResumeCampaignDeck131(string chapterId)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            Registry019(); Registry020(); Registry023();
            var prepared = PrepareCampaignDeck131(chapterId);
            if (!prepared.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(prepared.Errors));
            var progress = prepared.Value.Guild.GuildCity.Strategic017H.Campaign019;
            var operation = progress.Playable020?.ActiveOperation;
            var gate = progress.Playable020?.WorldGate023;
            var travelRequired = operation != null && gate?.ActiveOperation == null &&
                _campaignRules020.TryGetBlueprint(chapterId, out var blueprint) &&
                operation.CurrentStepIndex < blueprint.Steps.Count &&
                CampaignAdventureRules084.IsWorldBoardStep084(blueprint.Steps[operation.CurrentStepIndex]) &&
                !StringComparer.Ordinal.Equals(gate?.CurrentWorldId, blueprint.WorldId);
            var message = travelRequired
                ? "Story quest ready. Travel to its world through the World Gate, then continue."
                : "Story quest ready to continue.";
            // A repeated or resumed request cannot save, reroll, or claim anything.
            if (ReferenceEquals(prepared.Value, _campaign)) return M1CommandResult.Success(message);
            return ApplyAndPersist(prepared, true, message);
        }

        Result<CampaignState> PrepareCampaignDeck131(string chapterId, CampaignState source = null)
        {
            source = source ?? _campaign;
            if (source?.Guild?.GuildCity == null || string.IsNullOrWhiteSpace(chapterId) ||
                !_campaignRules019.TryGetChapter(chapterId, out _) ||
                !_campaignRules020.TryGetBlueprint(chapterId, out var blueprint))
                return Result<CampaignState>.Failure("CAMPAIGN131_CHAPTER_REQUIRED");
            var candidate = source;
            if(candidate.Recovery150?.Paused == true)
            {
                var released=ReleaseIdleTowerForGuildProgression107(candidate);
                if(!released.IsSuccess) return released;
                var resumed=CampaignRecoveryCommands150.Resume(released.Value,chapterId,_campaignRules023);
                if(!resumed.IsSuccess) return resumed;
                candidate=resumed.Value;
            }
            var progress = candidate.Guild.GuildCity.Strategic017H.Campaign019;
            var chapter = progress?.ActiveOperation;
            var operation = progress?.Playable020?.ActiveOperation;
            if ((chapter != null && !StringComparer.Ordinal.Equals(chapter.ChapterId, chapterId)) ||
                (operation != null && !StringComparer.Ordinal.Equals(operation.ChapterId, chapterId)) ||
                (!string.IsNullOrWhiteSpace(progress?.ActiveChapterId) &&
                 !StringComparer.Ordinal.Equals(progress.ActiveChapterId, chapterId)))
                return Result<CampaignState>.Failure("CAMPAIGN131_FINISH_CURRENT_STORY_FIRST");

            if (operation == null && progress?.PendingReceipt != null)
                return Result<CampaignState>.Success(candidate);

            if (operation == null)
            {
                // Reuse the established safe idle-Tower release only when starting
                // an operation. No active battle, pending reward or receipt is cleared.
                var released = ReleaseIdleTowerForGuildProgression107(candidate);
                if (!released.IsSuccess) return released;
                candidate = released.Value;
                if (chapter == null)
                {
                    var started = _campaignCommands019.StartChapter(candidate, _campaignRules019,
                        chapterId, source.Guild.Unions.Where(value => value != null && value.MemberRecruitIds.Count > 0).Take(10).Select(value => value.UnionId).ToArray(), ownerApprovedCandidateOrder: true);
                    if (!started.IsSuccess) return started;
                    candidate = started.Value;
                }
            }
            // BeginOperation is an existing authority-checked identity operation
            // when the same playable chapter already exists, including pending steps.
            var begun = _campaignCommands020.BeginOperation(candidate, _campaignRules020, chapterId);
            if (!begun.IsSuccess) return begun;
            candidate = begun.Value;
            progress = candidate.Guild.GuildCity.Strategic017H.Campaign019;
            operation = progress.Playable020.ActiveOperation;
            var gate = progress.Playable020.WorldGate023;
            if (gate?.ActiveOperation != null)
            {
                if (!StringComparer.Ordinal.Equals(gate.ActiveOperation.DefinitionId, chapterId) ||
                    !StringComparer.Ordinal.Equals(gate.ActiveOperation.OperationKind, "CHAPTER"))
                    return Result<CampaignState>.Failure("CAMPAIGN131_FINISH_CURRENT_ADVENTURE_FIRST");
                return CampaignWorldGateCommandService023.ValidateActiveAuthority093(candidate,
                    _campaignRules023, out var error)
                    ? Result<CampaignState>.Success(candidate)
                    : Result<CampaignState>.Failure(error);
            }
            var city = candidate.Guild.GuildCity;
            if (operation.PendingReceipt != null || progress.PendingReceipt != null ||
                city.PendingEncounter != null || city.PendingBattleReturn != null ||
                operation.Status != CampaignPlayableOperationStatus020.Active ||
                (candidate.Battle != null && (candidate.Battle.Outcome == BattleOutcome.InProgress ||
                    candidate.Battle.Reward != null && !candidate.Battle.Reward.Claimed)))
                return Result<CampaignState>.Success(candidate);

            var step = blueprint.Steps[operation.CurrentStepIndex];
            if (operation.CurrentStepIndex == 0 && StringComparer.Ordinal.Equals(step.Kind, "BRIEFING"))
            {
                if (step.RequiresCertifiedBattle || step.ConsumesOperation ||
                    blueprint.Steps.Count(value => StringComparer.Ordinal.Equals(value.Kind, "BRIEFING")) != 1 ||
                    blueprint.Steps.Count < 2 || !CampaignAdventureRules084.IsWorldBoardStep084(blueprint.Steps[1]))
                    return Result<CampaignState>.Failure("CAMPAIGN131_INITIAL_BRIEFING_BOUNDARY_REQUIRED");
                var committed = _campaignCommands020.CommitNonBattleStep(candidate, _campaignRules020, "SUCCESS");
                if (!committed.IsSuccess) return committed;
                var applied = _campaignCommands020.ApplyStepReceiptExactlyOnce(committed.Value, _campaignRules020);
                if (!applied.IsSuccess) return applied;
                candidate = applied.Value;
                progress = candidate.Guild.GuildCity.Strategic017H.Campaign019;
                operation = progress.Playable020.ActiveOperation;
                gate = progress.Playable020.WorldGate023;
                step = blueprint.Steps[operation.CurrentStepIndex];
            }
            // Later civic, diplomacy, battle and return steps remain explicit story
            // moments. Only the exact authoritative WORLD_BOARD can begin a deck.
            if (!CampaignAdventureRules084.IsWorldBoardStep084(step))
                return _campaignCardFlow132.PrepareNextInterruption(candidate, _campaignRules020, _campaignRules019);
            // Starting the chapter does not authorize travel or spend supplies.
            // Save its prepared boundary so the normal travel controls remain usable.
            if (!StringComparer.Ordinal.Equals(gate?.CurrentWorldId, blueprint.WorldId))
                return Result<CampaignState>.Success(candidate);
            SecondDimension.Gameplay.Recruitment.HeroMaster300Catalog087 heroes = null;
            if (TryHeroMaster300CreatorRegistry087(out var heroRegistry)) heroes = heroRegistry.Source;
            return _campaignCommands023.BeginOperation(candidate, _campaignRules023, chapterId,
                source.Guild.Unions.Where(value => value != null && value.MemberRecruitIds.Count > 0).Take(10).Select(value => value.UnionId).ToArray(), _campaignRules020, heroes);
        }
    }
}
