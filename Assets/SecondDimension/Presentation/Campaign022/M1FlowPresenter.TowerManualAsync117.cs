using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        Coroutine _towerManualWait117;
        int _towerManualGeneration117;
        bool _towerManualRefreshOnEnable117;
        Text _towerManualPhase117;
        Button _towerManualButton117;

        bool TryBeginTowerManual117(Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator, bool startsBattle)
        {
            if (!(coordinator is Campaign022.ITowerManualAsyncTransition117 owner)) return false;
            if (_towerRoomCommandRunning084) return true;
            _towerRoomCommandRunning084 = true;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                CaptureTowerActionLabel117();
                var task = owner.PendingTowerManualTransition117;
                if (task == null) task = startsBattle ? owner.StartTowerBattleAsync117() : owner.BankTowerVictoryAsync117();
                else startsBattle = owner.PendingTowerManualStartsBattle117;
                ObserveTowerManual117(owner, task, startsBattle);
            }
            catch (Exception exception)
            {
                ClearTowerManualView117();
                _localStatus = exception.Message;
                _localStatusPositive = false;
                BuildCurrentScreen();
            }
            return true;
        }

        void CaptureTowerActionLabel117()
        {
            if (_screenRoot == null) return;
            var eventSystem = EventSystem.current;
            var selectedObject = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var selected = selectedObject != null ? selectedObject.GetComponent<Button>() : null;
            var names = new[] { "Climb next Tower floor 084", "Enter Tower battle 081", "Prepare Tower battle only 088",
                "Bank Tower battle victory 088", "Enter prepared Tower battle 081" };
            _towerManualButton117 = selected != null && selected.transform.IsChildOf(_screenRoot) && names.Contains(selected.name)
                ? selected : _screenRoot.GetComponentsInChildren<Button>(true).FirstOrDefault(button => names.Contains(button.name));
            if (_towerManualButton117 == null) return;
            _towerManualPhase117 = _towerManualButton117.GetComponentInChildren<Text>(true);
            _towerManualButton117.interactable = false;
        }

        bool DrawPendingTowerManual117(Transform body, Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
        {
            var owner = coordinator as Campaign022.ITowerManualAsyncTransition117;
            var task = owner?.PendingTowerManualTransition117;
            if (task == null) return false;
            var startsBattle = owner.PendingTowerManualStartsBattle117;
            var heading = TowerManualPhaseCopy117(startsBattle, owner.TowerManualIsSaving117);
            var panel = AddMessagePanel(body, heading, "The existing Tower update is in progress.", RuntimeUi.Accent);
            _towerManualPhase117 = panel.GetComponentsInChildren<Text>(true).FirstOrDefault(text => text.text == heading);
            if (isActiveAndEnabled && gameObject.activeInHierarchy && _towerManualWait117 == null)
                ObserveTowerManual117(owner, task, startsBattle);
            return true;
        }

        static string TowerManualPhaseCopy117(bool startsBattle, bool saving) => saving
            ? "SAVING…" : startsBattle ? "PREPARING BATTLE…" : "BANKING REWARDS…";

        void ObserveTowerManual117(Campaign022.ITowerManualAsyncTransition117 owner, Task<M1CommandResult> task, bool startsBattle)
        {
            if (_towerManualWait117 != null) return;
            _towerRoomCommandRunning084 = true;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            var generation = ++_towerManualGeneration117;
            _towerManualWait117 = StartCoroutine(AwaitTowerManual117(owner, task, startsBattle, generation));
        }

        IEnumerator AwaitTowerManual117(Campaign022.ITowerManualAsyncTransition117 owner, Task<M1CommandResult> task,
            bool startsBattle, int generation)
        {
            while (!task.IsCompleted)
            {
                if (_towerManualPhase117 != null)
                    _towerManualPhase117.text = TowerManualPhaseCopy117(startsBattle, owner.TowerManualIsSaving117);
                // Poll only task/phase; no campaign rebuild, save or gameplay step per frame.
                yield return null;
            }
            yield return null;
            if (this == null || generation != _towerManualGeneration117 || !ReferenceEquals(_coordinator, owner)) yield break;
            ClearTowerManualView117();
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;
            var result = task.IsCanceled || task.IsFaulted
                ? M1CommandResult.Failure(task.Exception?.GetBaseException().Message ?? "The Tower update was interrupted.") : task.Result;
            _localStatus = result?.Message ?? "The Tower update returned no result.";
            _localStatusPositive = result?.Succeeded == true;
            // A user who moved to Title/another page keeps that destination. The
            // coordinator still commits and publishes independently of this view.
            if (_localStatusPositive && startsBattle && _screen == M1Screen.GuildOperations && _guildCityTab017D == "ABYSS")
                Navigate(M1Screen.Battle);
            else BuildCurrentScreen();
        }

        void ClearTowerManualView117()
        {
            _towerManualWait117 = null;
            _towerRoomCommandRunning084 = false;
            _suppressBoardAdventureCoordinatorRefresh084 = false;
            if (_towerManualButton117 != null) _towerManualButton117.interactable = true;
            _towerManualButton117 = null;
            _towerManualPhase117 = null;
        }

        void OnEnable()
        {
            if (!_towerManualRefreshOnEnable117) return;
            _towerManualRefreshOnEnable117 = false;
            if (_coordinator != null) BuildCurrentScreen();
        }

        void DetachTowerManual117(bool refreshOnEnable = true)
        {
            if (!refreshOnEnable) _towerManualRefreshOnEnable117 = false;
            else if (_towerManualWait117 != null ||
                (_coordinator as Campaign022.ITowerManualAsyncTransition117)?.PendingTowerManualTransition117 != null)
                _towerManualRefreshOnEnable117 = true;
            (_coordinator as Campaign022.ITowerManualAsyncTransition117)?.CancelTowerManualBeforeSave117();
            ++_towerManualGeneration117;
            if (_towerManualWait117 != null) StopCoroutine(_towerManualWait117);
            ClearTowerManualView117();
        }
    }
}
