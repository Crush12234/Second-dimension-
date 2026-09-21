using System;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    // Retained only for a deliberate restart. It contains the exact terminal
    // battle and admission, not a copied campaign or a mutable current-floor flag.
    [Serializable]
    public sealed class TowerRestartDefeatProof130
    {
        [JsonConstructor]
        public TowerRestartDefeatProof130(BattleState defeatBattle, EncounterLaunchRequest017D encounter)
        {
            DefeatBattle = defeatBattle ?? throw new ArgumentNullException(nameof(defeatBattle));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
        }
        public BattleState DefeatBattle { get; }
        public EncounterLaunchRequest017D Encounter { get; }
    }

    public sealed partial class CampaignProgressionCommandService022
    {
        const string TowerRestartPolicy130 = "TOWER_RESTART_AFTER_FULL_PARTY_DEFEAT_130_V1";

        public static bool IsFullPartyDefeat130(BattleState battle) =>
            battle != null && battle.Phase == BattlePhase.Resolved && battle.Outcome == BattleOutcome.Defeat &&
            battle.Reward?.Outcome == BattleOutcome.Defeat && battle.PlayerUnions.Count > 0 &&
            battle.PlayerUnions.All(union => union.Side == BattleSide.Player && !union.Retreated && union.Members.Count > 0 &&
                union.Members.All(member => member.Downed && member.CurrentHp == 0));

        public bool CanRestartTowerAfterPartyDefeat130(CampaignState campaign, ICampaignRegistry022 registry)
        {
            var active = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022?.ActiveAbyssOperation;
            return registry != null && IsEndlessTowerOperation094(active?.OperationInstanceId) &&
                campaign.Guild.GuildCity.PendingEncounter != null && campaign.Guild.GuildCity.PendingBattleReturn == null &&
                IsFullPartyDefeat130(campaign.Battle) && M2BattleCommandService.HasValidFinalStateHash090(campaign.Battle) &&
                HasMatchingActiveAbyssBattle(campaign, registry);
        }

        // Defeat claim/growth/loot are prepared by the existing coordinator
        // authorities first. This pure command appends the restart and Floor1
        // begin together; its caller persists the resulting candidate once.
        public Result<CampaignState> RestartTowerAfterPartyDefeat130(CampaignState campaign, ICampaignRegistry022 registry)
        {
            if (!CanRestartTowerAfterPartyDefeat130(campaign, registry))
                return Result<CampaignState>.Failure("TOWER130_MATCHING_FULL_PARTY_DEFEAT_REQUIRED");
            if (campaign.Battle.Reward.Claimed != true ||
                !campaign.Guild.Development.HasClaimedReward(campaign.Battle.Reward.RewardId))
                return Result<CampaignState>.Failure("TOWER130_CLAIM_DEFEAT_REWARD_FIRST");
            var proof = new TowerRestartDefeatProof130(campaign.Battle, campaign.Guild.GuildCity.PendingEncounter);
            var retreated = RetreatAbyssOperationCore130(campaign, registry, proof);
            if (!retreated.IsSuccess) return retreated;
            var view = DescribeTowerFloors094(retreated.Value, registry);
            if (!view.IsSuccess) return Result<CampaignState>.Failure(view.Errors.ToArray());
            if (view.Value.NextActualFloor != 1)
                return Result<CampaignState>.Failure("TOWER130_RESTART_BOUNDARY_INVALID");
            return BeginTowerFloor094(retreated.Value, registry);
        }

        static bool ValidateTowerRestartEntry130(CampaignState campaign, ICampaignRegistry022 registry,
            AbyssAuthorityEntry022 entry)
        {
            var proof = entry.TowerRestart130;
            if (proof == null) return true;
            var active = entry.AbortedOperation;
            var battle = proof.DefeatBattle;
            var request = proof.Encounter;
            if (!entry.Aborted || active == null || !IsEndlessTowerOperation094(active.OperationInstanceId) ||
                active.Status != AbyssOperationStatus022.AwaitingBattle || active.PendingReceipt != null ||
                !IsFullPartyDefeat130(battle) || battle.Reward.Claimed != true ||
                !M2BattleCommandService.HasValidFinalStateHash090(battle) ||
                !campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId) ||
                !TryGetActiveAbyssBattleStep(active, registry, out var operation, out var step, out _) ||
                request.BattleId != battle.BattleId ||
                !campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request))) return false;
            // The saved admission supplies its original Union IDs and clear
            // count. A later legitimate roster change cannot rewrite its proof.
            var expected = CreateAbyssBattleEncounterRequest130(campaign, active, operation, step,
                request.AlliedUnionIds, active.FloorClearCountAtBegin);
            if (!AbyssEncountersMatch(request, expected) ||
                !battle.PlayerUnions.Select(union => union.UnionId).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(request.AlliedUnionIds)) return false;
            var members = battle.PlayerUnions.SelectMany(union => union.Members).Select(member => member.MemberId).ToArray();
            // The operation reserves owned Union members; M2 admits only the
            // deployable members. Keep the terminal, hash-validated battle and
            // its reward roster exact without re-reading later mutable duties.
            return members.Length > 0 && members.Distinct(StringComparer.Ordinal).Count() == members.Length &&
                members.All(id => active.AlliedRecruitIds.Contains(id, StringComparer.Ordinal)) &&
                battle.Reward.MemberRewards.Select(reward => reward.MemberId)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(members.OrderBy(id => id, StringComparer.Ordinal));
        }
    }
}
