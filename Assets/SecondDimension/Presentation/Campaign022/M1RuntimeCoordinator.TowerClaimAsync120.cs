using System;
using System.Threading.Tasks;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation.Campaign022
{
    public interface ITowerClaimAsyncTransition120
    {
        bool TowerVictoryAwaitingClaim120 { get; }
        Task<M1CommandResult> PendingTowerClaim120 { get; }
        bool TowerClaimIsSaving120 { get; }
        void CancelTowerClaimBeforeSave120();
        Task<M1CommandResult> ClaimTowerBattleVictoryAsync120();
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign022.ITowerClaimAsyncTransition120
    {
        // Routing hint only. The worker runs every existing battle/reward authority.
        public bool TowerVictoryAwaitingClaim120 => IsTowerClaimRoute120(_campaign);
        static bool IsTowerClaimRoute120(CampaignState source) => ProgressionState081(source).ActiveAbyssOperation != null &&
            source?.Battle?.Phase == BattlePhase.Resolved && source.Battle.Outcome == BattleOutcome.Victory &&
            source.Battle.Reward != null && !source.Battle.Reward.Claimed &&
            source.Battle.BattleId?.StartsWith("ABYSS_BATTLE022_FLOOR_", StringComparison.Ordinal) == true;
        public Task<M1CommandResult> PendingTowerClaim120 =>
            _towerWork116?.Mode == TowerManualMode117.Claim ? _towerTask116 : null;
        public bool TowerClaimIsSaving120 => _towerWork116?.Mode == TowerManualMode117.Claim && _towerWork116.IsSaving;
        public void CancelTowerClaimBeforeSave120()
        { if (_towerWork116?.Mode == TowerManualMode117.Claim) _towerWork116.CancelBeforeWrite(); }
        public Task<M1CommandResult> ClaimTowerBattleVictoryAsync120() => BeginTowerManual117(TowerManualMode117.Claim);

        static TowerPrepared116 BuildTowerClaim120(TowerWork116 work)
        {
            if (!IsTowerClaimRoute120(work.Source))
                return RejectedTower117("No Tower victory is waiting to claim.");
            var claimed = BuildClaimedTowerVictory120(work, out var migratedLegacy);
            if (!claimed.IsSuccess) return new TowerPrepared116(claimed);
            // Preserve manual semantics: claim/return only; Bank remains deliberate.
            return PrepareTowerCandidateFinal117(work.Source, claimed.Value, work, null, migratedLegacy
                ? "Battle rewards claimed and saved. Your older first-story route was updated without repeating the battle; now follow the gold Lantern Road waymarkers home."
                : "Battle rewards claimed. Character, Art, Guild, Hall, expedition, relationship, and board-return state were saved.");
        }

        static Result<CampaignState> BuildClaimedTowerVictory120(TowerWork116 work, out bool migratedLegacy)
        {
            migratedLegacy = false;
            var source = work.Source;
            work.ThrowIfCanceled();
            var claimed = work.Battles.ClaimBattleRewards(source);
            if (!claimed.IsSuccess) return claimed;
            var growth = new BattleEquipmentGrowthBridge022().ApplyClaimedBattleGrowth(claimed.Value, work.Registry);
            if (!growth.IsSuccess) return growth;
            var candidate = growth.Value;
            if (work.Loot != null && source.Guild.GuildCity?.PendingEncounter != null)
            {
                var encounter = source.Guild.GuildCity.PendingEncounter;
                var loot = work.Loot.ResolveReceipt(source, encounter, source.Battle, Math.Max(1, Math.Min(7, encounter.EnemyUnionCount)));
                var applied = work.Loot.ApplyExactlyOnce(candidate, loot);
                if (!applied.IsSuccess) return applied;
                candidate = applied.Value;
            }
            if (candidate.Guild.GuildCity?.PendingEncounter != null)
            {
                if (!work.Commands.HasActiveAbyssEncounter(candidate, work.Registry))
                    return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_BATTLE_AUTHORITY_REQUIRED");
                var returned = work.Commands.CommitAbyssBattleReturn(candidate, work.Registry);
                if (!returned.IsSuccess) return returned;
                returned = work.Commands.ApplyAbyssBattleReturnExactlyOnce(returned.Value, work.Registry);
                if (!returned.IsSuccess) return returned;
                candidate = returned.Value;
            }
            if (GuildCityExpeditionService017D.NeedsLegacyFirstRescueMigration069(candidate))
            {
                var migrated = work.Expeditions.MigrateLegacyFirstRescue069(candidate, work.GuildContent, work.StrategicContent);
                if (!migrated.IsSuccess) return migrated;
                candidate = migrated.Value;
                migratedLegacy = true;
            }
            return Result<CampaignState>.Success(candidate);
        }
    }
}
