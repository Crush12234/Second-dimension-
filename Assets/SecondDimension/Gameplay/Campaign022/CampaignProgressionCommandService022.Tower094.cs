using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    public sealed class TowerFloorProgress094
    {
        public int HighestActualFloor { get; internal set; }
        public int CurrentRunClearedFloor130 { get; internal set; }
        public int LatestCompletedActualFloor130 { get; internal set; }
        public int ActiveActualFloor { get; internal set; }
        public int NextActualFloor { get; internal set; }
        public int ContentTemplateFloor { get; internal set; }
        public bool ActiveUsesNewPolicy { get; internal set; }
    }

    public sealed partial class CampaignProgressionCommandService022
    {
        public const string TowerOperationPrefix094 = "TOWERRUN094_";
        const string TowerFloorBindingPrefix094 = "TOWER_FLOOR094|";
        const string TowerFloorPolicy094 = "ENDLESS_FLOOR_094_V1";
        const int TowerContentTemplateCount094 = 10;
        public const string EndlessBattleKind094 = "ENDLESS_BATTLE094";

        public static string TowerOperationDefinitionId094(int templateFloor, bool firstClear)
        {
            if (templateFloor < 1 || templateFloor > TowerContentTemplateCount094)
                throw new ArgumentOutOfRangeException(nameof(templateFloor));
            return firstClear
                ? "ABYSS_OP022_" + templateFloor.ToString("00", CultureInfo.InvariantCulture) + "_GUARDIAN"
                : "ABYSS_OP094_" + templateFloor.ToString("00", CultureInfo.InvariantCulture) + "_ENDLESS_BATTLE";
        }

        static bool HasOneTowerBattle094(ICampaignRegistry022 registry, string operationId) =>
            registry.AbyssOperations.TryGetValue(operationId, out var operation) &&
            operation.steps != null && operation.steps.Count(step => step?.requiresBattle == true) == 1;

        public static int TowerContentTemplate094(int actualFloor)
        {
            if (actualFloor < 1) throw new ArgumentOutOfRangeException(nameof(actualFloor));
            return (actualFloor - 1) % TowerContentTemplateCount094 + 1;
        }

        public Result<CampaignState> BeginTowerFloor094(CampaignState campaign, ICampaignRegistry022 registry)
        {
            var preview = DescribeTowerFloors094(campaign, registry);
            if (!preview.IsSuccess) return Result<CampaignState>.Failure(preview.Errors.ToArray());
            var floor = preview.Value.NextActualFloor;
            var template = TowerContentTemplate094(floor);
            var state = campaign.Guild.GuildCity.Strategic017H?.Campaign019?.Playable020?.Progression022
                ?? CampaignProgressionState022.Default();
            var definition = registry.Floors.Values.SingleOrDefault(value => value.floor == template);
            if (definition == null) return Result<CampaignState>.Failure("TOWER094_TEMPLATE_REQUIRED");
            var firstClear = !state.AbyssFloors.Any(value => value.FloorId == definition.floorId && value.ClearCount > 0);
            var operationId = TowerOperationDefinitionId094(template, firstClear);
            if (!HasOneTowerBattle094(registry, operationId))
                return Result<CampaignState>.Failure("TOWER094_CERTIFIED_BATTLE_TEMPLATE_REQUIRED");
            // Reuse the certified begin law, roster snapshot, seed and receipt chain.
            var begun = BeginAbyssOperationCore094(campaign, registry, operationId);
            if (!begun.IsSuccess) return begun;
            var candidate = begun.Value;
            if (!TryContext(candidate, out var city, out var strategic, out var progress,
                    out var playable, out var begunState, out var error))
                return Result<CampaignState>.Failure(error);
            var source = begunState.ActiveAbyssOperation;
            var instance = TowerOperationId094(source.CanonicalSeedIdentity, floor);
            var active = new AbyssOperationState022(instance, source.OperationDefinitionId,
                source.FloorId, source.CurrentStepIndex, source.Status, source.CompletedStepIds,
                source.PendingReceipt, source.ExistingBattleRewardReceiptId, source.CanonicalSeedIdentity,
                source.AppliedReceiptCountAtBegin, source.AppliedStepProofs, source.CommittedOperationOrdinal,
                source.PreviousAuthorityHash, source.BeginAuthorityHash, source.AlliedRosterIdentity,
                source.AlliedRecruitIds, source.FloorClearCountAtBegin);
            var grant = new AbyssBeginGrant022(active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.CommittedOperationOrdinal, active.OperationInstanceId, active.OperationDefinitionId,
                active.FloorId, active.AppliedReceiptCountAtBegin.Value, active.FloorClearCountAtBegin,
                active.AlliedRosterIdentity, active.AlliedRecruitIds);
            var binding = TowerFloorBinding094(candidate, active.BeginAuthorityHash,
                active.PreviousAuthorityHash, active.OperationInstanceId, active.OperationDefinitionId,
                active.FloorId, floor, TowerEconomy159.Policy159);
            var development = candidate.Guild.Development;
            if (!development.CanRecordAdventureAuthority(binding))
                return Result<CampaignState>.Failure("TOWER094_FLOOR_BINDING_LEDGER_FULL");
            var guild = candidate.Guild.With(candidate.Guild.TreasuryXp, candidate.Guild.Recruits,
                candidate.Guild.Unions, candidate.Guild.Inventory, development.RecordAdventureAuthority(binding));
            var committed = Success(candidate.With(guild, candidate.OpeningFlow), city, strategic,
                progress, playable, begunState.With(activeAbyssOperation: active, replaceActiveAbyssOperation: true,
                    activeAbyssBeginGrant: grant, replaceActiveAbyssBeginGrant: true,
                    lastCheckpointId: "tower094_floor_" + floor + "_begun"));
            if (!committed.IsSuccess) return committed;
            var checkedView = DescribeTowerFloors094(committed.Value, registry);
            return checkedView.IsSuccess ? committed : Result<CampaignState>.Failure(checkedView.Errors.ToArray());
        }

        public static Result<TowerFloorProgress094> DescribeTowerFloors094(
            CampaignState campaign, ICampaignRegistry022 registry)
        {
            if (campaign?.Guild?.GuildCity == null || registry == null)
                return Result<TowerFloorProgress094>.Failure("TOWER094_CONTEXT_REQUIRED");
            var state = campaign.Guild.GuildCity.Strategic017H?.Campaign019?.Playable020?.Progression022
                ?? CampaignProgressionState022.Default();
            state = EnsureAbyssAuthority084(campaign, state);
            if (!ValidateAbyssAuthority084(campaign, registry, state) ||
                !TryTowerFloorHistory130(campaign, registry, state, out var highest, out var activeFloor, out var currentRun, out var latestCompleted))
                return Result<TowerFloorProgress094>.Failure("TOWER094_FLOOR_AUTHORITY_INVALID");
            if (currentRun == int.MaxValue)
                return Result<TowerFloorProgress094>.Failure("TOWER094_FLOOR_LIMIT_REACHED");
            var active = state.ActiveAbyssOperation;
            var newPolicy = IsEndlessTowerOperation094(active?.OperationInstanceId);
            if (active != null && !newPolicy)
                activeFloor = registry.Floors.TryGetValue(active.FloorId, out var definition) ? definition.floor : 0;
            return Result<TowerFloorProgress094>.Success(new TowerFloorProgress094 {
                HighestActualFloor = highest, ActiveActualFloor = activeFloor,
                CurrentRunClearedFloor130 = currentRun, LatestCompletedActualFloor130 = latestCompleted,
                NextActualFloor = currentRun + 1, ActiveUsesNewPolicy = newPolicy,
                ContentTemplateFloor = active == null ? TowerContentTemplate094(currentRun + 1) :
                    registry.Floors.TryGetValue(active.FloorId, out var template) ? template.floor : 0 });
        }

        internal static bool IsEndlessTowerOperation094(string instanceId) =>
            !string.IsNullOrWhiteSpace(instanceId) && instanceId.StartsWith(TowerOperationPrefix094, StringComparison.Ordinal);

        static string TowerOperationId094(string seed, int floor) => TowerOperationPrefix094 +
            floor.ToString("D6", CultureInfo.InvariantCulture) + "_" + seed.Substring(0, 24).ToUpperInvariant();

        static string TowerFloorBinding094(CampaignState campaign, string beginHash, string previousHash,
            string instanceId, string definitionId, string templateFloorId, int actualFloor,
            string policy = TowerFloorPolicy094)
        {
            var hash = CanonicalJson.Sha256Hex(new { Policy = policy,
                campaign.CampaignGuid, campaign.CampaignSeed, BeginAuthorityHash = beginHash,
                PreviousAuthorityHash = previousHash, OperationInstanceId = instanceId,
                OperationDefinitionId = definitionId, TemplateFloorId = templateFloorId,
                ActualFloor = actualFloor });
            return TowerFloorBindingPrefix094 + beginHash + "|" +
                actualFloor.ToString(CultureInfo.InvariantCulture) + "|" + policy + "|" + instanceId + "|" + hash;
        }

        static bool TryReadTowerFloorBinding094(CampaignState campaign, string beginHash, string previousHash,
            string instanceId, string definitionId, string templateFloorId, out int floor)
            => TryReadTowerFloorBinding098(campaign, beginHash, previousHash, instanceId,
                definitionId, templateFloorId, out floor, out _);

        static bool TryReadTowerFloorBinding098(CampaignState campaign, string beginHash, string previousHash,
            string instanceId, string definitionId, string templateFloorId, out int floor, out string policy)
        {
            floor = 0;
            policy = string.Empty;
            if (!IsEndlessTowerOperation094(instanceId) || string.IsNullOrWhiteSpace(beginHash)) return false;
            var prefix = TowerFloorBindingPrefix094 + beginHash + "|";
            var matches = campaign.Guild.Development.AppliedAdventureAuthorityIds
                .Where(value => value.StartsWith(prefix, StringComparison.Ordinal)).Take(2).ToArray();
            if (matches.Length != 1) return false;
            var parts = matches[0].Substring(TowerFloorBindingPrefix094.Length).Split('|');
            if (parts.Length != 5 || (parts[2] != TowerFloorPolicy094 && parts[2] != TowerThreatRules098.Policy098 && parts[2] != TowerEnemyPartyRules137.Policy137 && parts[2] != TowerScalingRules138.Policy138 && parts[2] != TowerEconomy159.Policy159) || parts[3] != instanceId ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out floor) || floor < 1)
                return false;
            policy = parts[2];
            return matches[0] == TowerFloorBinding094(campaign, beginHash, previousHash,
                instanceId, definitionId, templateFloorId, floor, policy);
        }

        static bool HasCanonicalTowerOperationIdentity094(CampaignState campaign,
            AbyssOperationState022 active, string canonicalSeed) =>
            TryReadTowerFloorBinding094(campaign, active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.OperationInstanceId, active.OperationDefinitionId, active.FloorId, out var floor) &&
            active.OperationInstanceId == TowerOperationId094(canonicalSeed, floor);

        static bool ValidateTowerFloorBindings094(CampaignState campaign, ICampaignRegistry022 registry,
            CampaignProgressionState022 state) => TryTowerFloorHistory094(campaign, registry, state, out _, out _);

        // Called only after the existing full Abyss chain is validated. This adds
        // sequential actual-floor binding; it never replaces the battle proofs.
        static bool TryTowerFloorHistory094(CampaignState campaign, ICampaignRegistry022 registry,
            CampaignProgressionState022 state, out int highest, out int activeFloor)
            => TryTowerFloorHistory130(campaign, registry, state, out highest, out activeFloor, out _, out _);

        static bool TryTowerFloorHistory130(CampaignState campaign, ICampaignRegistry022 registry,
            CampaignProgressionState022 state, out int highest, out int activeFloor, out int currentRun, out int latestCompleted)
        {
            highest = state.AbyssAuthorityBaseFloors.Where(value => value.ClearCount > 0 &&
                    registry.Floors.ContainsKey(value.FloorId))
                .Select(value => registry.Floors[value.FloorId].floor).DefaultIfEmpty(0).Max();
            activeFloor = 0;
            currentRun = highest;
            latestCompleted = highest;
            var restartRewards = new HashSet<string>(StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in state.AbyssAuthorityEntries)
            {
                if (entry == null) return false;
                if (!IsEndlessTowerOperation094(entry.OperationInstanceId))
                {
                    // A genuine legacy floor clear can establish floors 1–10,
                    // never floor 50 merely because its trial was repeated.
                    if (!entry.Aborted && registry.Floors.TryGetValue(entry.FloorId, out var legacy))
                    {
                        highest = Math.Max(highest, legacy.floor);
                        currentRun = Math.Max(currentRun, legacy.floor);
                        latestCompleted = legacy.floor;
                    }
                    continue;
                }
                var begin = entry.Aborted ? entry.AbortedOperation?.BeginAuthorityHash : entry.CompletionProof?.BeginAuthorityHash;
                if (!HasOneTowerBattle094(registry, entry.OperationDefinitionId) ||
                    currentRun == int.MaxValue || !TryReadTowerFloorBinding094(campaign, begin, entry.PreviousHash,
                        entry.OperationInstanceId, entry.OperationDefinitionId, entry.FloorId, out var floor) ||
                    floor != currentRun + 1 || !registry.Floors.TryGetValue(entry.FloorId, out var template) ||
                    template.floor != TowerContentTemplate094(floor) || !used.Add(begin)) return false;
                if (!entry.Aborted)
                {
                    highest = Math.Max(highest, floor);
                    currentRun = floor;
                    latestCompleted = floor;
                }
                else if (entry.TowerRestart130 != null)
                {
                    if (!restartRewards.Add(entry.TowerRestart130.DefeatBattle.Reward.RewardId)) return false;
                    currentRun = 0;
                }
            }
            var active = state.ActiveAbyssOperation;
            if (IsEndlessTowerOperation094(active?.OperationInstanceId))
            {
                if (!HasOneTowerBattle094(registry, active.OperationDefinitionId) ||
                    currentRun == int.MaxValue || !TryReadTowerFloorBinding094(campaign, active.BeginAuthorityHash,
                        active.PreviousAuthorityHash, active.OperationInstanceId, active.OperationDefinitionId,
                        active.FloorId, out activeFloor) || activeFloor != currentRun + 1 ||
                    !registry.Floors.TryGetValue(active.FloorId, out var template) ||
                    template.floor != TowerContentTemplate094(activeFloor) || !used.Add(active.BeginAuthorityHash)) return false;
            }
            // No orphan, duplicate or malformed begin bindings may hide in a save.
            var all = campaign.Guild.Development.AppliedAdventureAuthorityIds
                .Where(value => value.StartsWith(TowerFloorBindingPrefix094, StringComparison.Ordinal)).ToArray();
            return all.Length == used.Count && all.All(value => {
                var parts = value.Substring(TowerFloorBindingPrefix094.Length).Split('|');
                return parts.Length == 5 && used.Contains(parts[0]); });
        }

        internal static bool HasVerifiedTowerFloorCompletion094(CampaignState campaign,
            ICampaignRegistry022 registry, string completionReceipt, int actualFloor)
        {
            var state = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022;
            if (state == null || registry == null || !ValidateAbyssAuthority084(campaign, registry, state) ||
                !campaign.Guild.Development.HasClaimedReward(completionReceipt)) return false;
            var matches = state.AbyssAuthorityEntries.Where(entry => !entry.Aborted &&
                entry.CompletionProof?.CompletionReceipt.ReceiptId == completionReceipt).Take(2).ToArray();
            if (matches.Length != 1) return false;
            var entry = matches[0];
            return IsEndlessTowerOperation094(entry.OperationInstanceId) &&
                TryReadTowerFloorBinding094(campaign, entry.CompletionProof.BeginAuthorityHash, entry.PreviousHash,
                    entry.OperationInstanceId, entry.OperationDefinitionId, entry.FloorId, out var bound) &&
                bound == actualFloor;
        }
    }
}
