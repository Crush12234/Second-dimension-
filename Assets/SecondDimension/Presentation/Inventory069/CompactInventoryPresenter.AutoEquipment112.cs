using System;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class CompactInventoryPresenter069
    {
        Button _autoEquipHero112, _autoEquipUnions112, _undoAutoEquip112;

        void BuildAutoEquipmentToolbar112(M1RecruitLoadoutView recruit)
        {
            _autoEquipHero112 = _autoEquipUnions112 = _undoAutoEquip112 = null;
            if (!(_coordinator is IAutoEquipmentCoordinator112 authority)) return;
            var bar = RuntimeUi.AddPanel(_root, "Manual Auto Equip 112", RuntimeUi.PanelRaised);
            var canvas = _root.GetComponentInParent<Canvas>();
            var toolbarHeight = Mathf.Max(90f,44f/Mathf.Max(.01f,canvas!=null?canvas.scaleFactor:1f)+16f);
            RuntimeUi.SetLayout(bar, preferredHeight: toolbarHeight);
            bar.GetComponent<LayoutElement>().minHeight = toolbarHeight;
            RuntimeUi.AddHorizontalLayout(bar.transform, new RectOffset(12, 12, 8, 8), 14f);
            _autoEquipHero112 = AutoButton112(bar.transform, "Auto Equip Hero 112", "AUTO EQUIP HERO",
                () => RunAutoEquipment123(authority, recruit.RecruitId, AutoEquipmentUi123.Hero));
            _autoEquipUnions112 = AutoButton112(bar.transform, "Auto Equip All Unions 112", "AUTO EQUIP ALL UNIONS",
                () => RunAutoEquipment123(authority, null, AutoEquipmentUi123.Unions));
            _undoAutoEquip112 = AutoButton112(bar.transform, "Undo Auto Equip 112", "UNDO AUTO EQUIP",
                () => RunAutoEquipment123(authority, null, AutoEquipmentUi123.Undo));
            _undoAutoEquip112.interactable = authority.CanUndoAutoEquip112;
        }

        Button AutoButton112(Transform parent, string name, string label, Action action)
        {
            var button = RuntimeUi.AddButton(parent, name, label, action, 70f);
            RuntimeUi.SetLayout(button, flexibleWidth: 1f, preferredHeight: 70f);
            MakeCompactButton069(button, 70f, CompactPhone164 ? Mathf.CeilToInt(12f/InventoryScale164) : 24);
            AddCancelOnSelectable069(button);
            return button;
        }

        void RunAutoEquip112(Func<M1CommandResult> action)
        {
            M1CommandResult result;
            try { result = action() ?? M1CommandResult.Failure("Equipment authority returned no result."); }
            catch (Exception error) { result = M1CommandResult.Failure("Equipment action failed: " + error.Message); }
            _statusPositive = result.Succeeded;
            var message = result.Message ?? string.Empty;
            _statusMessage = message.Split('\n')[0];
            Refresh();
            ShowAutoEquipmentSummary112(message, result.Succeeded);
        }

        void ShowAutoEquipmentSummary112(string summary, bool succeeded)
        {
            if (_root == null) return;
            var shade = RuntimeUi.AddPanel(_root, "Auto Equip Summary 112", new Color(0f, 0f, 0f, 0.88f));
            shade.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch069(shade.rectTransform, 0f);
            var panel = RuntimeUi.AddPanel(shade.transform, "Auto Equip Result 112", RuntimeUi.PanelRaised);
            panel.rectTransform.anchorMin = new Vector2(0.12f, 0.10f);
            panel.rectTransform.anchorMax = new Vector2(0.88f, 0.90f);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(30, 30, 24, 24), 18f, TextAnchor.UpperLeft);
            var title = RuntimeUi.AddText(panel.transform, "Auto Equip Summary Heading 112",
                "EQUIPMENT STATUS", 32, TextAnchor.MiddleLeft,
                succeeded ? RuntimeUi.Positive : RuntimeUi.Warning, FontStyle.Bold);
            RuntimeUi.SetLayout(title, preferredHeight: 56f);

            var scrollObject = new GameObject("Auto Equip Change List 112", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel.transform, false);
            RuntimeUi.SetLayout(scrollObject.GetComponent<RectTransform>(), flexibleHeight: 1f, preferredHeight: 360f);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            viewportObject.GetComponent<Image>().color = Color.clear;
            var viewport = viewportObject.GetComponent<RectTransform>();
            Stretch069(viewport, 0f);
            var details = RuntimeUi.AddText(viewport, "Auto Equip Factual Changes 112", summary,
                25, TextAnchor.UpperLeft, RuntimeUi.Text);
            var content = details.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-16f, 0f);
            details.horizontalOverflow = HorizontalWrapMode.Wrap;
            details.verticalOverflow = VerticalWrapMode.Overflow;
            var fitter = details.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            Action closeSummary = () =>
            {
                shade.gameObject.SetActive(false);
                Destroy(shade.gameObject);
                if (_undoAutoEquip112 != null && _undoAutoEquip112.interactable) _undoAutoEquip112.Select();
                else _autoEquipHero112?.Select();
            };
            var done = RuntimeUi.AddButton(panel.transform, "Close Auto Equip Summary 112", "DONE", closeSummary, 72f, RuntimeUi.Accent);
            RuntimeUi.SetLayout(done, preferredHeight: 72f);
            EquipmentModalGuard156.Attach(_root, shade.gameObject, done, closeSummary);
            done.Select();
        }
    }

    // Owns input only while an Armory equipment operation/result is displayed.
    // It has no equipment or save APIs. Removing/rebuilding the view releases it.
    internal sealed class EquipmentModalGuard156 : MonoBehaviour
    {
        Transform _owner;
        CanvasGroup _ownerGroup;
        bool _ownerWasInteractable, _closeQueued;
        Action _close;

        internal static void Attach(Transform owner, GameObject modal, Selectable control, Action close)
        {
            var guard = modal.AddComponent<EquipmentModalGuard156>();
            guard._owner = owner;
            guard._close = close;
            var group = modal.GetComponent<CanvasGroup>() ?? modal.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            guard.Acquire();
            // One action exists on these two overlays. Automatic navigation must
            // not choose live Armory controls located behind the overlay.
            var navigation = control.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = navigation.selectOnDown = navigation.selectOnLeft = navigation.selectOnRight = control;
            control.navigation = navigation;
            control.gameObject.AddComponent<ConfirmationCancelHandler077>().Cancel = guard.QueueClose;
        }

        void Acquire()
        {
            if (_owner == null || _ownerGroup != null) return;
            _ownerGroup = _owner.GetComponent<CanvasGroup>() ?? _owner.gameObject.AddComponent<CanvasGroup>();
            _ownerWasInteractable = _ownerGroup.interactable;
            _ownerGroup.interactable = false;
        }

        void Release()
        {
            if (_ownerGroup == null) return;
            _ownerGroup.interactable = _ownerWasInteractable;
            // Retain the inert component: a successor overlay may acquire it in
            // this frame, so delayed Destroy would undermine its input guard.
            _ownerGroup = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) QueueClose();
        }

        void QueueClose()
        {
            if (_closeQueued || !isActiveAndEnabled) return;
            _closeQueued = true;
            StartCoroutine(CloseAfterInputFrame());
        }

        System.Collections.IEnumerator CloseAfterInputFrame()
        {
            // Do not deliver the same Cancel/Escape to newly focused Hall or
            // Armory controls after dismissing this overlay.
            yield return new WaitForEndOfFrame();
            if (!isActiveAndEnabled) yield break;
            _close?.Invoke();
        }

        void OnEnable() { Acquire(); }
        void OnDisable()
        {
            StopAllCoroutines();
            _closeQueued = false;
            Release();
        }
        void OnDestroy() { Release(); }
    }
}
