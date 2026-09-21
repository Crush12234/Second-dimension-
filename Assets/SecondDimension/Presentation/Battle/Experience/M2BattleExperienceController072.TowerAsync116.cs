using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M2BattleExperienceController072
    {
        Coroutine _towerWait116;
        int _towerWaitGeneration116;

        bool ObservePendingTowerTransaction116(M2BattleView battle)
        {
            var owner = _coordinator as Campaign022.ITowerAutoAsyncTransition116;
            var pending = owner?.PendingTowerAutoTransition116;
            if (pending == null) return false;
            if (isActiveAndEnabled && _root != null && _root.gameObject.activeInHierarchy)
                ObserveTowerTask116(owner, pending, owner.ReadPendingTowerVictory116() ?? battle);
            return true;
        }

        void BeginTowerTransaction116(Campaign022.ITowerAutoAsyncTransition116 owner, M2BattleView battle)
        {
            _claiming = true;
            _hud?.SetInteractable(false);
            _results?.Hide();
            SetCaption("BANKING REWARDS…", "Your committed victory is being prepared.");
            ObserveTowerTask116(owner, owner.AdvanceTowerAutoAfterVictoryAsync116(), battle);
        }

        void ObserveTowerTask116(Campaign022.ITowerAutoAsyncTransition116 owner,
            Task<M1CommandResult> task, M2BattleView battle)
        {
            if (_towerWait116 != null) return;
            _claiming = true;
            _hud?.SetInteractable(false);
            _results?.Hide();
            var generation = ++_towerWaitGeneration116;
            _towerWait116 = StartCoroutine(AwaitTowerTask116(owner, task, battle, generation));
        }

        IEnumerator AwaitTowerTask116(Campaign022.ITowerAutoAsyncTransition116 owner,
            Task<M1CommandResult> task, M2BattleView cleared, int generation)
        {
            var started = Time.realtimeSinceStartup;
            bool? saving = null;
            while (!task.IsCompleted)
            {
                var nextSaving = owner.TowerAutoIsSaving116;
                if (saving != nextSaving)
                {
                    saving = nextSaving;
                    SetCaption(nextSaving ? "SAVING…" : "BANKING REWARDS…",
                        nextSaving ? "Finishing the saved Tower update." : "Your committed victory is being prepared.");
                }
                // No campaign projection, command, clock scaling or artificial
                // dwell here. Unity continues rendering/input every frame.
                yield return null;
            }
            // Even an already-completed failure observes one frame. Otherwise
            // StartCoroutine could complete before its handle is assigned.
            yield return null;
            if (this == null || generation != _towerWaitGeneration116 || !ReferenceEquals(_coordinator, owner)) yield break;
            _towerWait116 = null;
            _claiming = false;
            if (!isActiveAndEnabled || _root == null || !_root.gameObject.activeInHierarchy) yield break;
            var result = task.IsCanceled || task.IsFaulted
                ? M1CommandResult.Failure(task.Exception?.GetBaseException().Message ?? "Tower continuation was interrupted.")
                : task.Result;
            if (result == null || !result.Succeeded)
            {
                StopTowerAuto107(result?.Message ?? "The Tower transition returned no result.");
                Refresh(M2BattleViewAccess098.Read(_coordinator));
                yield break;
            }
            var next = CompleteAsyncTowerView116(cleared, started);
            Refresh(next ?? M2BattleViewAccess098.Read(_coordinator));
        }

        M2BattleView CompleteAsyncTowerView116(M2BattleView cleared, float started)
        {
            var next = M2BattleViewAccess098.Read(_coordinator);
            if (cleared == null || !M2BattleAutoOrders091.HasLivingOpposition(next) ||
                StringComparer.Ordinal.Equals(next.BattleId, cleared.BattleId))
                return StopTowerAuto107("The next Tower battle is unavailable. Return to the Tower to continue.");
            _pendingView = next;
            if (_autoOrders091) _autoBattleId091 = next.BattleId;
            var reward = (_coordinator as Campaign022.ITowerSavedRewardReader110)
                ?.ReadTowerSavedReward110(cleared.BattleId, next.BattleId);
            var floor = reward?.Floor ?? ParseTowerFloorNumber108(cleared.BattleId);
            var floorReward = reward == null ? null : new Campaign022.CampaignProgressionPresentationState022 {
                TowerGuildXpReward = reward.GuildXp, TowerHallXpReward = reward.HallXp,
                TowerRewardMaterialIds108 = reward.MaterialIds };
            var heroReward = reward == null ? null : new Campaign022.CampaignProgressionPresentationState022 {
                TowerLastHeroRewardFloor094 = reward.Floor, TowerLastHeroRewardSummary094 = reward.HeroSummary };
            _towerRewardNotice108 = BuildTowerRewardNotice108(floor, floorReward, cleared.Reward, heroReward);
            AppendAutoHistory110(_autoDecisionHistory108, _towerRewardNotice108);
            var completed = Time.realtimeSinceStartup;
            LastCompletedTowerFloor108 = floor;
            LastTowerTransitionSeconds108 = Mathf.Max(0f, completed - started);
            LastTowerFloorSeconds108 = _towerFloorStartedAt108 > 0f
                ? Mathf.Max(0f, completed - _towerFloorStartedAt108) : LastTowerTransitionSeconds108;
            _towerFloorStartedAt108 = completed;
            RefreshAutoDecisionFeed108();
            ScheduleNextAutoOrder108();
            _towerAutoFailure107 = string.Empty;
            return next;
        }

        void DetachTowerWait116()
        {
            DetachTowerClaim120();
            (_coordinator as Campaign022.ITowerAutoAsyncTransition116)?.CancelTowerAutoBeforeSave116();
            ++_towerWaitGeneration116;
            if (_towerWait116 != null)
            {
                StopCoroutine(_towerWait116);
                _towerWait116 = null;
                _claiming = false;
            }
        }

        void OnDisable()
        {
            DetachTowerWait116();
            SetAutoOrders091(false);
        }
    }
}
