using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M2BattleExperienceController072
    {
        Coroutine _towerClaimWait120;
        int _towerClaimGeneration120;

        bool TryBeginTowerClaim120()
        {
            var owner = _coordinator as Campaign022.ITowerClaimAsyncTransition120;
            if (owner == null || !owner.TowerVictoryAwaitingClaim120 && owner.PendingTowerClaim120 == null) return false;
            _claiming = true;
            _results?.ShowClaimPending120(false);
            try
            {
                var task = owner.PendingTowerClaim120 ?? owner.ClaimTowerBattleVictoryAsync120();
                ObserveTowerClaim120(owner, task);
            }
            catch (Exception exception)
            {
                _claiming = false;
                _results?.ShowClaimFailure107(exception.Message);
            }
            return true;
        }

        bool ObservePendingTowerClaim120(M2BattleView battle)
        {
            var owner = _coordinator as Campaign022.ITowerClaimAsyncTransition120;
            var task = owner?.PendingTowerClaim120;
            if (task == null) return false;
            if (isActiveAndEnabled && _root != null && _root.gameObject.activeInHierarchy)
            {
                if (_towerClaimWait120 == null)
                {
                    // A fresh controller has not passed the normal terminal-result
                    // branch yet. Resume its already-requested claim without replaying
                    // the reveal or polling another campaign projection.
                    SetFirstBattleCoachVisible076(false);
                    _hudLayer.gameObject.SetActive(false);
                    _diorama.SetTacticalOverlayVisible(false);
                    if (_storyRibbon != null) _storyRibbon.gameObject.SetActive(false);
                    _resultLayer.gameObject.SetActive(true);
                    _resultLayer.SetAsLastSibling();
                    _results.ResumeClaim120(battle, owner.TowerClaimIsSaving120);
                }
                ObserveTowerClaim120(owner, task);
            }
            return true;
        }

        void ObserveTowerClaim120(Campaign022.ITowerClaimAsyncTransition120 owner, Task<M1CommandResult> task)
        {
            if (_towerClaimWait120 != null) return;
            _claiming = true;
            _results?.ShowClaimPending120(owner.TowerClaimIsSaving120);
            var generation = ++_towerClaimGeneration120;
            _towerClaimWait120 = StartCoroutine(AwaitTowerClaim120(owner, task, generation));
        }

        IEnumerator AwaitTowerClaim120(Campaign022.ITowerClaimAsyncTransition120 owner, Task<M1CommandResult> task, int generation)
        {
            while (!task.IsCompleted)
            {
                _results?.ShowClaimPending120(owner.TowerClaimIsSaving120);
                yield return null;
            }
            yield return null;
            if (this == null || generation != _towerClaimGeneration120 || !ReferenceEquals(_coordinator, owner)) yield break;
            _towerClaimWait120 = null;
            _claiming = false;
            if (!isActiveAndEnabled || _root == null || !_root.gameObject.activeInHierarchy) yield break;
            var result = task.IsCanceled || task.IsFaulted
                ? M1CommandResult.Failure(task.Exception?.GetBaseException().Message ?? "The reward claim was interrupted.") : task.Result;
            if (result == null || !result.Succeeded)
            {
                _results?.ShowClaimFailure107(result?.Message ?? "The reward claim returned no result. Try again.");
                yield break;
            }
            SetVisible(false);
            BattleCompleted?.Invoke();
        }

        void DetachTowerClaim120()
        {
            var owner = _coordinator as Campaign022.ITowerClaimAsyncTransition120;
            if (owner?.PendingTowerClaim120 != null) owner.CancelTowerClaimBeforeSave120();
            ++_towerClaimGeneration120;
            if (_towerClaimWait120 != null)
            {
                StopCoroutine(_towerClaimWait120);
                _towerClaimWait120 = null;
                _claiming = false;
            }
        }
    }
}
