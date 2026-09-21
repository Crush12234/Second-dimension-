using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class CompactInventoryPresenter069
    {
        enum AutoEquipmentUi123 { Hero, Unions, Undo }
        IAutoEquipmentAsyncCoordinator123 _equipmentOwner123;
        Task<M1CommandResult> _equipmentTask123;
        Coroutine _equipmentWait123;
        int _equipmentGeneration123;
        GameObject _equipmentPendingPanel123;
        Text _equipmentPendingText123;

        public bool IsAutoEquipmentResolving123 => _equipmentTask123 != null ||
            (_coordinator as IAutoEquipmentAsyncCoordinator123)?.PendingAutoEquipment123 != null;

        void RunAutoEquipment123(IAutoEquipmentCoordinator112 authority, string recruitId, AutoEquipmentUi123 mode)
        {
            if (!_running || !isActiveAndEnabled || !ReferenceEquals(authority, _coordinator) || IsAutoEquipmentResolving123) return;
            if (!(authority is IAutoEquipmentAsyncCoordinator123 asyncAuthority))
            {
                RunAutoEquip112(() => mode == AutoEquipmentUi123.Hero ? authority.AutoEquipHero112(recruitId) :
                    mode == AutoEquipmentUi123.Unions ? authority.AutoEquipAllUnions112() : authority.UndoAutoEquip112());
                return;
            }
            Task<M1CommandResult> task;
            try
            {
                task = mode == AutoEquipmentUi123.Hero ? asyncAuthority.AutoEquipHeroAsync123(recruitId) :
                    mode == AutoEquipmentUi123.Unions ? asyncAuthority.AutoEquipAllUnionsAsync123() : asyncAuthority.UndoAutoEquipAsync123();
            }
            catch (Exception error) { task = Task.FromResult(M1CommandResult.Failure("Equipment action failed: " + error.Message)); }
            AttachAutoEquipment123(asyncAuthority, task ?? Task.FromResult(M1CommandResult.Failure("Equipment authority returned no result.")));
        }

        bool ObservePendingAutoEquipment123()
        {
            if (_equipmentTask123 != null)
            {
                ShowAutoEquipmentPending123();
                return true;
            }
            var owner = _coordinator as IAutoEquipmentAsyncCoordinator123;
            var task = owner?.PendingAutoEquipment123;
            if (task == null) return false;
            AttachAutoEquipment123(owner, task);
            return true;
        }

        void AttachAutoEquipment123(IAutoEquipmentAsyncCoordinator123 owner, Task<M1CommandResult> task)
        {
            if (!_running || !isActiveAndEnabled || _root == null) return;
            _equipmentOwner123 = owner;
            _equipmentTask123 = task;
            var generation = ++_equipmentGeneration123;
            ShowAutoEquipmentPending123();
            _equipmentWait123 = StartCoroutine(WaitAutoEquipment123(owner, task, generation));
        }

        IEnumerator WaitAutoEquipment123(IAutoEquipmentAsyncCoordinator123 owner, Task<M1CommandResult> task, int generation)
        {
            // Also handles immediate rejection/no-op without leaving a coroutine
            // handle assigned after its body has already completed.
            yield return null;
            while (!task.IsCompleted)
            {
                if (!EquipmentObserverCurrent123(owner, generation)) yield break;
                ShowAutoEquipmentPending123();
                yield return null;
            }
            if (!EquipmentObserverCurrent123(owner, generation)) yield break;
            M1CommandResult result;
            try { result = task.GetAwaiter().GetResult() ?? M1CommandResult.Failure("Equipment authority returned no result."); }
            catch (Exception error) { result = M1CommandResult.Failure("Equipment action failed: " + error.Message); }
            _equipmentTask123 = null;
            _equipmentWait123 = null;
            _equipmentOwner123 = null;
            RemoveAutoEquipmentPending123();
            var message = result.Message ?? string.Empty;
            _statusMessage = message.Split('\n')[0];
            _statusPositive = result.Succeeded;
            // Changed during publication is held by ObservePending above. Read
            // the committed/current state once after the owner finishes its task.
            Refresh();
            ShowAutoEquipmentSummary112(message, result.Succeeded);
        }

        bool EquipmentObserverCurrent123(IAutoEquipmentAsyncCoordinator123 owner, int generation) =>
            generation == _equipmentGeneration123 && _running && _root != null && isActiveAndEnabled &&
            ReferenceEquals(_coordinator, owner) && ReferenceEquals(_equipmentOwner123, owner);

        void ShowAutoEquipmentPending123()
        {
            if (_root == null || !_running) return;
            if (_equipmentPendingPanel123 == null)
            {
                var shade = RuntimeUi.AddPanel(_root, "Auto Equip Pending 123", new Color(0f, 0f, 0f, 0.90f));
                _equipmentPendingPanel123 = shade.gameObject;
                shade.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch069(shade.rectTransform, 0f);
                var panel = RuntimeUi.AddPanel(shade.transform, "Auto Equip Pending Body 123", RuntimeUi.PanelRaised);
                panel.rectTransform.anchorMin = new Vector2(0.14f, 0.26f);
                panel.rectTransform.anchorMax = new Vector2(0.86f, 0.74f);
                panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;
                RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(30, 30, 24, 24), 20f, TextAnchor.UpperLeft);
                _equipmentPendingText123 = RuntimeUi.AddText(panel.transform, "Auto Equip Pending Phase 123", string.Empty,
                    30, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
                RuntimeUi.SetLayout(_equipmentPendingText123, flexibleHeight: 1f, preferredHeight: 150f);
                Action returnFromPending = () =>
                {
                    _equipmentOwner123?.CancelAutoEquipmentBeforeSave123();
                    if (_closeRequested != null) _closeRequested.Invoke();
                    else Shutdown();
                };
                var close = RuntimeUi.AddButton(panel.transform, "Return From Pending Auto Equip 123", "RETURN", returnFromPending, 78f, RuntimeUi.Accent);
                EquipmentModalGuard156.Attach(_root, shade.gameObject, close, returnFromPending);
                close.Select();
            }
            var dots = new string('.', (int)(Time.unscaledTime * 3f) % 4);
            _equipmentPendingText123.text = _equipmentTask123?.IsCompleted == true
                ? "EQUIPMENT UPDATE FINISHED\nOpening the result..."
                : _equipmentOwner123?.AutoEquipmentIsSaving123 == true
                ? "SAVING EQUIPMENT" + dots + "\nClosing this view will not interrupt saving."
                : "CHECKING EQUIPMENT" + dots + "\nNo changes saved yet. Returning cancels before saving begins.";
            _equipmentPendingPanel123.transform.SetAsLastSibling();
        }

        void DetachAutoEquipment123()
        {
            ++_equipmentGeneration123;
            _equipmentOwner123?.CancelAutoEquipmentBeforeSave123();
            if (_equipmentWait123 != null) StopCoroutine(_equipmentWait123);
            _equipmentWait123 = null;
            _equipmentTask123 = null;
            _equipmentOwner123 = null;
            RemoveAutoEquipmentPending123();
        }

        void RemoveAutoEquipmentPending123()
        {
            if (_equipmentPendingPanel123 != null)
            {
                _equipmentPendingPanel123.SetActive(false);
                DestroyUiObject069(_equipmentPendingPanel123);
            }
            _equipmentPendingPanel123 = null;
            _equipmentPendingText123 = null;
        }
    }
}
