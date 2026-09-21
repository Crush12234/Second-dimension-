using System;
using System.Threading.Tasks;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation.Campaign022
{
    public interface ITowerRestartAsyncTransition130
    {
        bool CanRestartTowerAfterPartyDefeat130 { get; }
        Task<M1CommandResult> PendingTowerRestart130 { get; }
        bool TowerRestartIsSaving130 { get; }
        void CancelTowerRestartBeforeSave130();
        Task<M1CommandResult> RestartTowerFromFloorOneAsync130();
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign022.ITowerRestartAsyncTransition130
    {
        public bool CanRestartTowerAfterPartyDefeat130 =>
            _campaignCommands022.CanRestartTowerAfterPartyDefeat130(_campaign, Registry022());
        public Task<M1CommandResult> PendingTowerRestart130 =>
            _towerWork116?.Mode == TowerManualMode117.RestartAfterPartyDefeat130 ? _towerTask116 : null;
        public bool TowerRestartIsSaving130 => PendingTowerRestart130 != null && _towerWork116.IsSaving;
        public void CancelTowerRestartBeforeSave130()
        { if (_towerWork116?.Mode == TowerManualMode117.RestartAfterPartyDefeat130) _towerWork116.CancelBeforeWrite(); }
        public Task<M1CommandResult> RestartTowerFromFloorOneAsync130() => BeginTowerManual117(TowerManualMode117.RestartAfterPartyDefeat130);

        static TowerPrepared116 BuildTowerRestart130(TowerWork116 work)
        {
            var source = work.Source;
            if (!work.Commands.CanRestartTowerAfterPartyDefeat130(source, work.Registry))
                return RejectedTower117("Restart is available only after every deployed party member falls in this Tower battle.");
            var candidate = source;
            // Preserve the existing RetreatTowerRun081 defeat reward order. The
            // restart itself awards nothing and never equips an existing hero.
            if (candidate.Battle.Reward.Claimed != true)
            {
                var claimed = work.Battles.ClaimBattleRewards(candidate);
                if (!claimed.IsSuccess) return new TowerPrepared116(claimed);
                var growth = new BattleEquipmentGrowthBridge022().ApplyClaimedBattleGrowth(claimed.Value, work.Registry);
                if (!growth.IsSuccess) return new TowerPrepared116(growth);
                candidate = growth.Value;
                if (work.Loot != null)
                {
                    var encounter = source.Guild.GuildCity.PendingEncounter;
                    var receipt = work.Loot.ResolveReceipt(source, encounter, source.Battle, Math.Max(1, Math.Min(7, encounter.EnemyUnionCount)));
                    var loot = work.Loot.ApplyExactlyOnce(candidate, receipt);
                    if (!loot.IsSuccess) return new TowerPrepared116(loot);
                    candidate = loot.Value;
                }
            }
            work.ThrowIfCanceled();
            var restarted = work.Commands.RestartTowerAfterPartyDefeat130(candidate, work.Registry);
            if (!restarted.IsSuccess) return new TowerPrepared116(restarted);
            var prepared = AdvanceTowerBoundary116(restarted.Value, work);
            if (!prepared.IsSuccess) return new TowerPrepared116(prepared);
            return PrepareTowerCandidateFinal117(source, prepared.Value, work, null,
                "Floor 1 is ready. Your highest Tower record, heroes, equipment and earned rewards are preserved.");
        }
    }
}
