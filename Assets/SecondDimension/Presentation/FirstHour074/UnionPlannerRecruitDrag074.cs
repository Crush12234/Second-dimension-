using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Pointer-only shortcut for assigning a reserve recruit. The reserve Button
    /// remains the authoritative click, keyboard, and controller interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnionPlannerRecruitDrag074 : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private Action<string, int, int> _assign;
        private bool _canDrag;
        private RectTransform _ghost;
        private Canvas _rootCanvas;

        public static UnionPlannerRecruitDrag074 ActiveDrag { get; private set; }
        public string RecruitId { get; private set; }
        public string RoleDesignation { get; private set; }
        public Color RoleColor { get; private set; }
        public bool IsAssigned { get; private set; }
        public bool CanDrag => _canDrag;
        public bool DropCompleted { get; private set; }

        public void Configure(
            string recruitId,
            string roleDesignation,
            Color roleColor,
            bool isAssigned,
            bool canDrag,
            Action<string, int, int> assign)
        {
            RecruitId = recruitId ?? string.Empty;
            RoleDesignation = string.IsNullOrWhiteSpace(roleDesignation)
                ? "UNASSIGNED"
                : roleDesignation.Trim().ToUpperInvariant();
            RoleColor = roleColor;
            IsAssigned = isAssigned;
            _canDrag = canDrag && !string.IsNullOrWhiteSpace(RecruitId) && assign != null;
            _assign = assign;
            enabled = _canDrag;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_canDrag) return;
            if (ActiveDrag != null && ActiveDrag != this)
                ActiveDrag.CancelDrag074();

            ActiveDrag = this;
            DropCompleted = false;
            CreateGhost074();
            MoveGhost074(eventData);
            SetDropTargetsActive074(true);
            if (eventData != null && eventData.pointerDrag == null)
                eventData.pointerDrag = gameObject;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ActiveDrag != this) return;
            MoveGhost074(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ActiveDrag == this)
                CancelDrag074();
        }

        public bool TryAssignTo074(int unionIndex, int slotIndex)
        {
            if (ActiveDrag != this || !_canDrag || _assign == null) return false;

            var assign = _assign;
            DropCompleted = true;
            CleanupDragVisual074();
            ActiveDrag = null;
            assign(RecruitId, unionIndex, slotIndex);
            return true;
        }

        private void CancelDrag074()
        {
            CleanupDragVisual074();
            if (ActiveDrag == this) ActiveDrag = null;
        }

        private void CreateGhost074()
        {
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (_rootCanvas == null) return;

            var ghostObject = new GameObject(
                "Union Planner Drag Ghost 074",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            ghostObject.transform.SetParent(_rootCanvas.transform, false);
            _ghost = ghostObject.GetComponent<RectTransform>();
            _ghost.anchorMin = new Vector2(0.5f, 0.5f);
            _ghost.anchorMax = new Vector2(0.5f, 0.5f);
            _ghost.pivot = new Vector2(0.5f, 0.5f);
            _ghost.sizeDelta = new Vector2(360f, 112f);

            var background = ghostObject.GetComponent<Image>();
            background.color = Color.Lerp(
                new Color(0.015f, 0.025f, 0.040f, 0.96f),
                new Color(RoleColor.r, RoleColor.g, RoleColor.b, 0.96f),
                0.24f);
            background.raycastTarget = false;

            var group = ghostObject.GetComponent<CanvasGroup>();
            group.alpha = 0.96f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var stripe = RuntimeUi.AddPanel(
                ghostObject.transform,
                "Union Planner Drag Ghost Role " + RoleDesignation + " 074",
                RoleColor);
            Anchor074(stripe.rectTransform, new Rect(0f, 0f, 0.045f, 1f));
            stripe.raycastTarget = false;

            var label = RuntimeUi.AddText(
                ghostObject.transform,
                "Union Planner Drag Ghost Label 074",
                "MOVE " + RoleDesignation + " INTO A UNION",
                25,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold);
            Anchor074(label.rectTransform, new Rect(0.08f, 0.08f, 0.88f, 0.84f));
            label.raycastTarget = false;
            ghostObject.transform.SetAsLastSibling();
        }

        private void MoveGhost074(PointerEventData eventData)
        {
            if (_ghost == null || eventData == null || _rootCanvas == null) return;
            var parent = _ghost.parent as RectTransform;
            if (parent == null) return;
            var camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : eventData.pressEventCamera ?? _rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    eventData.position,
                    camera,
                    out var localPoint))
                _ghost.anchoredPosition = localPoint;
        }

        private void CleanupDragVisual074()
        {
            SetDropTargetsActive074(false);
            if (_ghost != null)
                Destroy(_ghost.gameObject);
            _ghost = null;
            _rootCanvas = null;
        }

        private void SetDropTargetsActive074(bool active)
        {
            var targets = FindObjectsByType<UnionPlannerDropTarget074>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var index = 0; index < targets.Length; index++)
                targets[index]?.SetDragActive074(active, this);
        }

        private void OnDisable()
        {
            if (ActiveDrag == this) CancelDrag074();
        }

        private static void Anchor074(RectTransform rect, Rect anchors)
        {
            rect.anchorMin = anchors.min;
            rect.anchorMax = anchors.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Drop surface used by open member slots, the selected Union, and visible
    /// Union tabs. It is deliberately separate from Button submission.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnionPlannerDropTarget074 : MonoBehaviour,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private Image _highlight;
        private bool _dragActive;
        private UnionPlannerRecruitDrag074 _activeSource;
        private Color _roleColor;

        public int UnionIndex { get; private set; }
        public int SlotIndex { get; private set; }
        public bool IsAvailableForReserve { get; private set; }
        public bool IsAvailableForAssigned { get; private set; }
        public bool IsAvailable => IsAvailableForReserve || IsAvailableForAssigned;
        public bool LastDropAccepted { get; private set; }

        public void Configure(
            int unionIndex,
            int slotIndex,
            bool availableForReserve,
            bool availableForAssigned)
        {
            UnionIndex = unionIndex;
            SlotIndex = Mathf.Max(0, slotIndex);
            IsAvailableForReserve = availableForReserve;
            IsAvailableForAssigned = availableForAssigned;
            enabled = IsAvailable;
            EnsureHighlight074();
            SetHighlight074(false, false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            LastDropAccepted = false;
            var source = eventData?.pointerDrag == null
                ? UnionPlannerRecruitDrag074.ActiveDrag
                : eventData.pointerDrag.GetComponent<UnionPlannerRecruitDrag074>() ??
                  UnionPlannerRecruitDrag074.ActiveDrag;
            if (!CanAccept074(source)) return;

            LastDropAccepted = source.TryAssignTo074(UnionIndex, SlotIndex);
            SetHighlight074(false, false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_dragActive && CanAccept074(_activeSource)) SetHighlight074(true, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_dragActive && CanAccept074(_activeSource)) SetHighlight074(true, false);
        }

        public void SetDragActive074(bool active, UnionPlannerRecruitDrag074 source)
        {
            _dragActive = active;
            _activeSource = active ? source : null;
            _roleColor = source == null ? Color.clear : source.RoleColor;
            SetHighlight074(active && CanAccept074(source), false);
        }

        private bool CanAccept074(UnionPlannerRecruitDrag074 source)
        {
            if (source == null) return false;
            return source.IsAssigned ? IsAvailableForAssigned : IsAvailableForReserve;
        }

        private void EnsureHighlight074()
        {
            if (_highlight != null) return;
            var highlightObject = new GameObject(
                "Union Planner Drop Highlight 074",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            highlightObject.transform.SetParent(transform, false);
            _highlight = highlightObject.GetComponent<Image>();
            Anchor074(_highlight.rectTransform, new Rect(0f, 0f, 1f, 1f));
            _highlight.raycastTarget = false;
            _highlight.transform.SetAsFirstSibling();
        }

        private void SetHighlight074(bool visible, bool hovered)
        {
            EnsureHighlight074();
            if (!visible)
            {
                _highlight.color = Color.clear;
                return;
            }

            _highlight.color = new Color(
                _roleColor.r,
                _roleColor.g,
                _roleColor.b,
                hovered ? 0.34f : 0.14f);
        }

        private static void Anchor074(RectTransform rect, Rect anchors)
        {
            rect.anchorMin = anchors.min;
            rect.anchorMax = anchors.max;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
        }
    }
}
