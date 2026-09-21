using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;

namespace SecondDimension.Presentation.Campaign022
{
    public interface ITowerAutoAsyncTransition116
    {
        Task<M1CommandResult> PendingTowerAutoTransition116 { get; }
        bool TowerAutoIsSaving116 { get; }
        M2BattleView ReadPendingTowerVictory116();
        Task<M1CommandResult> AdvanceTowerAutoAfterVictoryAsync116();
        void CancelTowerAutoBeforeSave116();
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign022.ITowerAutoAsyncTransition116
    {
        const string TowerBusy116 = "Tower rewards are being saved. Please wait.";
        const string TowerCanceled116 = "Tower continuation canceled before saving. Your victory is still ready to claim.";
        static readonly object TowerPathGate116 = new object();
        static readonly Dictionary<string, TowerWork116> TowerPathOwners116 =
            new Dictionary<string, TowerWork116>(StringComparer.OrdinalIgnoreCase);
        readonly int _coordinatorThread116 = Thread.CurrentThread.ManagedThreadId;
        TowerWork116 _towerWork116;
        Task<M1CommandResult> _towerTask116;

        public Task<M1CommandResult> PendingTowerAutoTransition116 => _towerWork116 == null || _towerWork116.Mode != TowerManualMode117.Auto ? null : _towerTask116;
        public bool TowerAutoIsSaving116 => _towerWork116?.Mode == TowerManualMode117.Auto && _towerWork116.IsSaving;

        partial void CheckTowerBattleStartLease110(ref M1CommandResult blocked)
        {
            if (TowerWriteBusy116()) blocked = M1CommandResult.Failure(TowerBusy116);
        }

        // Capture once when a view attaches, including during Changed publication.
        // Fresh receipt-only DTOs cannot expose or modify the immutable source.
        public M2BattleView ReadPendingTowerVictory116()
        {
            var battle = _towerWork116?.Mode == TowerManualMode117.Auto ? _towerWork116.Source.Battle : null;
            if (battle == null) return null;
            var reward = battle.Reward;
            return new M2BattleView {
                BattleId = battle.BattleId, IsResolved = true, Outcome = "Victory",
                Reward = reward == null ? null : new M2BattleRewardView {
                    RewardId = reward.RewardId, GuildTreasuryXpAward = reward.GuildTreasuryXpAward,
                    HallEnhancementXpAward = reward.HallEnhancementXpAward,
                    EquipmentRewardDisplayName = reward.EquipmentReward?.DisplayName ?? string.Empty } };
        }

        // Called before a second coordinator loads the same path. A new owner
        // must never read an old primary while this owner is replacing it.
        static void RequireTowerPathAvailable116(string path)
        {
            lock (TowerPathGate116)
                if (TowerPathOwners116.ContainsKey(Path.GetFullPath(path)))
                    throw new InvalidOperationException(TowerBusy116);
        }

        bool TowerWriteBusy116()
        {
            lock (TowerPathGate116)
                return TowerPathOwners116.ContainsKey(Path.GetFullPath(_savePath));
        }

        public void CancelTowerAutoBeforeSave116()
        { if (_towerWork116?.Mode == TowerManualMode117.Auto) _towerWork116.CancelBeforeWrite(); }

        public Task<M1CommandResult> AdvanceTowerAutoAfterVictoryAsync116()
        {
            // Capture Unity resources, cancellation and all dependencies before
            // starting any worker. Await continuations keep this main context.
            if (Thread.CurrentThread.ManagedThreadId != _coordinatorThread116 || SynchronizationContext.Current == null)
                return Task.FromResult(M1CommandResult.Failure("Tower continuation requires the game thread."));
            if (TowerWriteBusy116()) return Task.FromResult(M1CommandResult.Failure(TowerBusy116));
            if (ProgressionState081(_campaign).ActiveAbyssOperation == null ||
                _campaign?.Battle?.Phase != BattlePhase.Resolved || _campaign.Battle.Outcome != BattleOutcome.Victory)
                return Task.FromResult(M1CommandResult.Failure("Finish the Tower battle before continuing."));
            var registry = Registry022();
            var work = new TowerWork116(this, registry);
            lock (TowerPathGate116)
            {
                if (TowerPathOwners116.ContainsKey(work.SavePath))
                    return Task.FromResult(M1CommandResult.Failure(TowerBusy116));
                TowerPathOwners116.Add(work.SavePath, work);
            }
            _towerWork116 = work;
            _towerTask116 = RunTowerAutoTransaction116(work, UnityEngine.Application.exitCancellationToken);
            return _towerTask116;
        }

        async Task<M1CommandResult> RunTowerAutoTransaction116(TowerWork116 work, CancellationToken exit)
        {
            using var timing = BeginTowerTiming110(work.Mode == TowerManualMode117.Auto ? "async-auto-transaction" :
                work.Mode == TowerManualMode117.Bank ? "async-manual-bank" :
                work.Mode == TowerManualMode117.Claim ? "async-tower-claim" :
                work.Mode == TowerManualMode117.RestartAfterPartyDefeat130 ? "async-tower-restart" :
                IsAutoEquipmentMode123(work.Mode) ? "async-equipment-" + work.Mode : "async-manual-start");
            using var cancellation = exit.Register(work.CancelBeforeWrite);
            TowerPrepared116 prepared = null;
            var durable = false;
            try
            {
                // Task lifetime belongs to the coordinator, not a MonoBehaviour
                // coroutine. Hiding/destroying the view cannot abandon the lease.
                prepared = await Task.Run(() => BuildTowerCandidateForMode117(work));
                timing?.Mark("prepare-isolated-candidate");
                if (!prepared.State.IsSuccess)
                    return M1CommandResult.Failure(FriendlyErrors(prepared.State.Errors));
                if (!ReferenceEquals(_towerWork116, work) || !ReferenceEquals(_campaign, work.Source))
                    return M1CommandResult.Failure(IsAutoEquipmentMode123(work.Mode)
                        ? "The active Guild changed before saving. No equipment update was written."
                        : "The active Guild changed before saving. No Tower update was written.");
                work.ThrowIfCanceled();
                if (!prepared.Persist) return M1CommandResult.Success(prepared.Message);
                // This CAS is also the cancellation boundary. Cancellation after
                // it may stop Auto, but cannot discard an in-flight durable save.
                if (!work.TryEnterWrite()) return M1CommandResult.Failure(IsAutoEquipmentMode123(work.Mode) ? EquipmentCanceled123 : work.Mode == TowerManualMode117.Auto ? TowerCanceled116 : "Tower update canceled before saving. Your saved Guild is unchanged.");
                var saved = await Task.Run(() => WriteTowerCandidate116(work, prepared.State.Value));
                timing?.Mark("atomic-store-write-and-readback");
                if (saved != null)
                {
                    UnityEngine.Debug.LogError("SAVE_WRITE_FAILED109\n" + saved);
                    return M1CommandResult.Failure("Something went wrong while saving. Please try again. " + saved.Message);
                }
                durable = true;
                _campaign = prepared.State.Value;
                _saveReloadVerified = false;
                if (!IsAutoEquipmentMode123(work.Mode)) PublishTowerRewardDisplay110(work.Source.Battle?.BattleId, prepared.Reward);
                NotifyChangedAfterCommittedSave109();
                timing?.Mark("publish-changed");
                return M1CommandResult.Success(prepared.Message ?? "Tower rewards banked and the next floor battle started.");
            }
            catch (OperationCanceledException)
            {
                return M1CommandResult.Failure(IsAutoEquipmentMode123(work.Mode) ? EquipmentCanceled123 : work.Mode == TowerManualMode117.Auto ? TowerCanceled116 : "Tower update canceled before saving. Your saved Guild is unchanged.");
            }
            catch (Exception exception)
            {
                if (durable)
                {
                    // A presentation failure cannot roll back a committed file
                    // or leave the coordinator referring to the old victory.
                    _campaign = prepared.State.Value;
                    _saveReloadVerified = false;
                    if (!IsAutoEquipmentMode123(work.Mode)) PublishTowerRewardDisplay110(work.Source.Battle?.BattleId, prepared.Reward);
                    UnityEngine.Debug.LogError("POST_SAVE_PRESENTATION_REFRESH_FAILED109\nThe Guild save is committed.\n" + exception);
                    return M1CommandResult.Success(IsAutoEquipmentMode123(work.Mode) ? "Equipment saved. Return to Equipment to view the result." : work.Mode == TowerManualMode117.Auto
                        ? "Tower rewards saved. Return to the Tower to continue." : "Tower update saved. Return to the Tower to continue.");
                }
                UnityEngine.Debug.LogError("PRE_SAVE_STATE_PREPARATION_FAILED109\n" + exception);
                return M1CommandResult.Failure("Something went wrong while preparing this update. No changes were saved. " + exception.Message);
            }
            finally
            {
                work.Finish();
                lock (TowerPathGate116)
                    if (TowerPathOwners116.TryGetValue(work.SavePath, out var owner) && ReferenceEquals(owner, work))
                        TowerPathOwners116.Remove(work.SavePath);
                if (ReferenceEquals(_towerWork116, work)) _towerWork116 = null;
            }
        }

        static Exception WriteTowerCandidate116(TowerWork116 work, CampaignState candidate)
        {
            var envelope = SaveEnvelopeV1.Create(candidate, DateTime.UtcNow);
            try { work.Store.Write(work.SavePath, envelope); return null; }
            catch (Exception exception)
            {
                // If a platform reports failure after replacement, recognize a
                // verifiably committed primary. Never claim rollback after save.
                try
                {
                    if (File.Exists(work.SavePath))
                    {
                        var recovered = work.Store.ReadWithRecovery(work.SavePath);
                        if (recovered.IsSuccess && StringComparer.Ordinal.Equals(
                            recovered.Value.CanonicalStateHash, envelope.CanonicalStateHash)) return null;
                    }
                }
                catch (Exception recoveryFailure) { return new IOException("The Guild save could not be verified. Reload the saved Guild before continuing.", new AggregateException(exception, recoveryFailure)); }
                return exception;
            }
        }

        // Pure worker path: explicit captured source/dependencies only. These
        // are the same authorities and ordering as AdvanceTowerAutoAfterVictory108
        // and ApplyAndPersist. No coordinator fields, Resources, Unity object,
        // UI callback, live RNG, or in-place campaign mutation is used here.
        static TowerPrepared116 BuildTowerCandidate116(TowerWork116 work)
        {
            var source = work.Source;
            var claimed = BuildClaimedTowerVictory120(work, out _);
            if (!claimed.IsSuccess) return new TowerPrepared116(claimed);
            var candidate = claimed.Value;
            var banked = AdvanceTowerBoundary116(candidate, work);
            if (!banked.IsSuccess) return new TowerPrepared116(banked);
            candidate = banked.Value;
            if (ProgressionState081(candidate).ActiveAbyssOperation != null)
                return new TowerPrepared116(Result<CampaignState>.Failure("The current Tower floor still needs attention. Auto is paused."));
            if (work.Starter != null) candidate = work.Starter.ApplyToNewRecruits(source, candidate);
            var display = PrepareTowerReward116(source, candidate, work);
            var begun = work.Commands.BeginTowerFloor094(candidate, work.Registry);
            if (!begun.IsSuccess) return new TowerPrepared116(begun);
            var prepared = AdvanceTowerBoundary116(begun.Value, work);
            if (!prepared.IsSuccess) return new TowerPrepared116(prepared);
            candidate = prepared.Value;
            if (ProgressionState081(candidate).ActiveAbyssOperation?.Status != AbyssOperationStatus022.Active)
                return new TowerPrepared116(Result<CampaignState>.Failure("The next Tower floor is not ready for battle. Auto is paused."));
            var encounterCommit = work.Commands.CommitAbyssBattleEncounter(candidate, work.Registry);
            if (!encounterCommit.IsSuccess) return new TowerPrepared116(encounterCommit);
            var started = work.Bridge.StartCertifiedEncounter(encounterCommit.Value, work.Battles, work.Combat, work.Enemies);
            if (!started.IsSuccess) return new TowerPrepared116(started);
            work.ThrowIfCanceled();

            return PrepareTowerCandidateFinal117(source, started.Value, work, display);
        }

        static Result<CampaignState> AdvanceTowerBoundary116(CampaignState source, TowerWork116 work)
        {
            var candidate = source;
            for (var transition = 0; transition < 32; transition++)
            {
                work.ThrowIfCanceled();
                var active = ProgressionState081(candidate).ActiveAbyssOperation;
                if (active == null) return Result<CampaignState>.Success(candidate);
                Result<CampaignState> moved;
                if (active.Status == AbyssOperationStatus022.AwaitingBattle)
                    moved = active.PendingReceipt == null
                        ? work.Commands.CommitAbyssBattleResult(candidate, work.Registry)
                        : work.Commands.ApplyAbyssBattleAndFinalize(candidate, work.Registry);
                else if (active.Status == AbyssOperationStatus022.ReadyToFinalize)
                    moved = active.PendingReceipt == null
                        ? work.Commands.CommitAbyssOperationCompletion(candidate, work.Registry)
                        : work.Commands.ApplyAbyssOperationCompletion(candidate, work.Registry, work.Recruitment);
                else
                {
                    if (active.Status != AbyssOperationStatus022.Active)
                        return Result<CampaignState>.Failure("The Tower run is not ready to move.");
                    if (!work.Registry.AbyssOperations.TryGetValue(active.OperationDefinitionId, out var operation))
                        return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
                    if (active.CurrentStepIndex < 0 || active.CurrentStepIndex >= operation.steps.Length)
                        return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_RANGE");
                    if (operation.steps[active.CurrentStepIndex].requiresBattle) return Result<CampaignState>.Success(candidate);
                    moved = active.PendingReceipt == null
                        ? work.Commands.CommitAbyssStep(candidate, work.Registry, "SUCCESS")
                        : work.Commands.ApplyAbyssStep(candidate, work.Registry);
                }
                if (!moved.IsSuccess) return moved;
                candidate = moved.Value;
            }
            return Result<CampaignState>.Failure("The Tower safety limit stopped an unexpected route. No additional step was taken.");
        }

        static Campaign022.TowerSavedReward110 PrepareTowerReward116(CampaignState source, CampaignState banked, TowerWork116 work)
        {
            var active = ProgressionState081(source).ActiveAbyssOperation;
            if (active == null || !work.Registry.AbyssOperations.TryGetValue(active.OperationDefinitionId, out var definition)) return null;
            var floors = CampaignProgressionCommandService022.DescribeTowerFloors094(banked, work.Registry);
            if (!floors.IsSuccess) return null;
            var floor = floors.Value.LatestCompletedActualFloor130;
            var summary = string.Empty;
            if (TowerHeroRewardRules094.IsRewardFloor(floor) && work.Recruitment != null)
            {
                var hero = work.Recruitment.DescribeLatestTowerHeroReward094(banked, work.Registry);
                if (hero.IsSuccess && hero.Value?.ActualFloor == floor) summary = TowerSavedHeroRewardCopy094(hero.Value);
            }
            return new Campaign022.TowerSavedReward110(floor, definition.guildXp, definition.hallXp, definition.rewardMaterialIds, summary);
        }

        static CampaignState SynchronizePeopleCandidate116(CampaignState candidate)
        {
            if (candidate?.Guild?.GuildCity == null) return candidate;
            var city = candidate.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? Gameplay.Campaign019.CampaignProgressState019.Default();
            var playable = progress.Playable020 ?? Gameplay.Campaign020.CampaignPlayableState020.Default();
            playable = Gameplay.RecruitChronicles025.RecruitChronicleSynchronizer025.Synchronize(candidate, playable);
            progress = progress.With(playable020: playable, replacePlayable020: true, lastCheckpointId: playable.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true, lastCheckpointId: progress.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true, lastCheckpointId: strategic.LastCheckpointId);
            return candidate.With(candidate.Guild.WithGuildCity(city), candidate.OpeningFlow);
        }

        sealed class TowerPrepared116
        {
            internal readonly Result<CampaignState> State;
            internal readonly Campaign022.TowerSavedReward110 Reward;
            internal readonly string Message;
            internal readonly bool Persist;
            internal TowerPrepared116(Result<CampaignState> state, Campaign022.TowerSavedReward110 reward = null, string message = null, bool persist = true)
            { State = state; Reward = reward; Message = message; Persist = persist; }
        }

        sealed class TowerWork116
        {
            internal readonly CampaignState Source;
            internal readonly TowerManualMode117 Mode;
            internal readonly string StartupNotice117;
            internal readonly string EquipmentRecruitId123;
            internal readonly string SavePath;
            internal readonly AtomicSaveStore Store;
            internal readonly Campaign022.CampaignRegistry022 Registry;
            internal readonly CampaignProgressionCommandService022 Commands;
            internal readonly M2BattleCommandService Battles;
            internal readonly GuildCityBattleBridgeService017D Bridge;
            internal readonly GuildCityRecruitmentService017D Recruitment;
            internal readonly RecruitStarterEquipment094 Starter;
            internal readonly LootResolver070 Loot;
            internal readonly M2CombatContent Combat;
            internal readonly EncounterRosterResolver070 Enemies;
            internal readonly GuildCityExpeditionService017D Expeditions;
            internal readonly GuildCityContent017D GuildContent;
            internal readonly GuildCityStrategicContent017H StrategicContent;
            int _phase; // 0 preparing, 1 store entered, 2 canceled, 3 finished
            // Main-thread capture only. The worker never keeps an owner reference.
            internal TowerWork116(M1RuntimeCoordinator owner, Campaign022.CampaignRegistry022 registry, TowerManualMode117 mode = TowerManualMode117.Auto, string equipmentRecruitId123 = null)
            {
                Mode = mode; StartupNotice117 = owner._startupNotice; EquipmentRecruitId123 = equipmentRecruitId123;
                Source = owner._campaign; SavePath = Path.GetFullPath(owner._savePath); Store = owner._saveStore; Registry = registry;
                Commands = owner._campaignCommands022; Battles = owner._battleCommands; Bridge = owner._guildCityBattleBridge;
                Recruitment = owner._guildCityRecruitment; Starter = owner._starterEquipment094; Loot = owner._lootResolver070;
                Combat = owner._combatContent; Enemies = owner._encounterRosterResolver070; Expeditions = owner._guildCityExpeditions;
                GuildContent = owner._guildCityContent; StrategicContent = owner._guildCityStrategicContent017H;
            }
            internal bool IsSaving => Volatile.Read(ref _phase) == 1;
            internal void CancelBeforeWrite() => Interlocked.CompareExchange(ref _phase, 2, 0);
            internal bool TryEnterWrite() => Interlocked.CompareExchange(ref _phase, 1, 0) == 0;
            internal void Finish() => Interlocked.Exchange(ref _phase, 3);
            internal void ThrowIfCanceled() { if (Volatile.Read(ref _phase) == 2) throw new OperationCanceledException(); }
        }
    }
}
