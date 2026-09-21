using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        Coroutine _towerRestartWait130;
        int _towerRestartGeneration130;
        Text _towerRestartPhase130;
        string _towerRestartFailure130;
        GameObject _towerRestartFailurePanel130;
        Button[] _towerRestartButtons130 = Array.Empty<Button>();
        const string TowerRestartButton130 = "Restart Tower from Floor 1 after party defeat 130";

        void BeginTowerRestart130(Campaign022.ITowerRestartAsyncTransition130 owner)
        {
            if (_towerRoomCommandRunning084 || _towerRestartWait130 != null) return;
            ClearTowerRestartFailure130();
            _towerRoomCommandRunning084 = true;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            _towerRestartButtons130 = _screenRoot.GetComponentsInChildren<Button>(false)
                .Where(button => button.name == TowerRestartButton130 || button.name == "Retreat from defeated Tower run 081").ToArray();
            _towerRestartPhase130 = _towerRestartButtons130.FirstOrDefault(button => button.name == TowerRestartButton130)?.GetComponentInChildren<Text>();
            foreach (var button in _towerRestartButtons130) button.interactable = false;
            try
            {
                var task = owner.PendingTowerRestart130 ?? owner.RestartTowerFromFloorOneAsync130();
                ObserveTowerRestart130(owner, task);
            }
            catch (Exception exception)
            {
                ClearTowerRestartView130();
                _localStatus = exception.Message;
                _towerRestartFailure130 = _localStatus;
                _localStatusPositive = false;
                BuildCurrentScreen();
            }
        }

        bool DrawPendingTowerRestart130(Transform body, Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
        {
            var owner = coordinator as Campaign022.ITowerRestartAsyncTransition130;
            var task = owner?.PendingTowerRestart130;
            if (task == null)
            {
                if (!string.IsNullOrWhiteSpace(_towerRestartFailure130))
                {
                    var failure = AddMessagePanel(body, "RESTART NOT SAVED", _towerRestartFailure130, RuntimeUi.Warning);
                    failure.gameObject.name = "Tower Restart Failure 130";
                    _towerRestartFailurePanel130 = failure.gameObject;
                }
                return false;
            }
            var heading = TowerRestartPhaseCopy130(owner.TowerRestartIsSaving130);
            var panel = AddMessagePanel(body, heading,
                "Your earned rewards and highest Tower record are being preserved.", RuntimeUi.Accent);
            _towerRestartPhase130 = panel.GetComponentsInChildren<Text>(true).Single(text => text.text == heading);
            if (isActiveAndEnabled && gameObject.activeInHierarchy && _towerRestartWait130 == null)
                ObserveTowerRestart130(owner, task);
            return true;
        }

        static string TowerRestartPhaseCopy130(bool saving) => saving ? "SAVING…" : "PREPARING FLOOR 1…";

        void ObserveTowerRestart130(Campaign022.ITowerRestartAsyncTransition130 owner, Task<M1CommandResult> task)
        {
            if (_towerRestartWait130 != null) return;
            _towerRoomCommandRunning084 = true;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            var generation = ++_towerRestartGeneration130;
            _towerRestartWait130 = StartCoroutine(AwaitTowerRestart130(owner, task, generation));
        }

        IEnumerator AwaitTowerRestart130(Campaign022.ITowerRestartAsyncTransition130 owner,
            Task<M1CommandResult> task, int generation)
        {
            while (!task.IsCompleted)
            {
                if (_towerRestartPhase130 != null) _towerRestartPhase130.text = TowerRestartPhaseCopy130(owner.TowerRestartIsSaving130);
                yield return null;
            }
            yield return null;
            if (this == null || generation != _towerRestartGeneration130 || !ReferenceEquals(_coordinator, owner)) yield break;
            ClearTowerRestartView130();
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;
            var result = task.IsCanceled || task.IsFaulted
                ? M1CommandResult.Failure(task.Exception?.GetBaseException().Message ?? "The Tower restart was interrupted.") : task.Result;
            _localStatus = result?.Message ?? "The Tower restart returned no result.";
            _localStatusPositive = result?.Succeeded == true;
            _towerRestartFailure130 = _localStatusPositive ? null : _localStatus;
            // Prepare Floor1 only. The existing Fight button starts its battle;
            // leaving this page while saving must not navigate the player back.
            BuildCurrentScreen();
        }

        void ClearTowerRestartView130()
        {
            _towerRestartWait130 = null;
            _towerRoomCommandRunning084 = false;
            _suppressBoardAdventureCoordinatorRefresh084 = false;
            foreach (var button in _towerRestartButtons130) if (button != null) button.interactable = true;
            _towerRestartButtons130 = Array.Empty<Button>();
            _towerRestartPhase130 = null;
        }

        void ClearTowerRestartFailure130()
        {
            _towerRestartFailure130 = null;
            if (_towerRestartFailurePanel130 != null) _towerRestartFailurePanel130.SetActive(false);
            _towerRestartFailurePanel130 = null;
        }

        void DetachTowerRestart130(bool refreshOnEnable = true)
        {
            if (!refreshOnEnable) ClearTowerRestartFailure130();
            if (refreshOnEnable && (_towerRestartWait130 != null ||
                (_coordinator as Campaign022.ITowerRestartAsyncTransition130)?.PendingTowerRestart130 != null))
                _towerManualRefreshOnEnable117 = true;
            (_coordinator as Campaign022.ITowerRestartAsyncTransition130)?.CancelTowerRestartBeforeSave130();
            ++_towerRestartGeneration130;
            if (_towerRestartWait130 != null) StopCoroutine(_towerRestartWait130);
            ClearTowerRestartView130();
        }
    }
}
