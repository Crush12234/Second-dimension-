using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class AutoEquipment112PlayModeTests
    {
        // This is a real canvas/input wiring test with a fake equipment authority.
        // Equipment state, persistence and affected-save behavior have separate tests.
        [UnityTest]
        public IEnumerator CommandsRunOnlyFromTheirNamedButtonsAndShowFactualSummary112()
        {
            var canvasObject = new GameObject("Auto equipment UI test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2796f, 1290f);
            scaler.matchWidthOrHeight = 0.5f;
            var eventSystem = EventSystem.current;
            GameObject ownedEventSystem = null;
            if (eventSystem == null)
            {
                ownedEventSystem = new GameObject("Auto equipment pointer test 112", typeof(EventSystem));
                eventSystem = ownedEventSystem.GetComponent<EventSystem>();
            }
            var host = canvasObject.GetComponent<RectTransform>();
            var owner = new GameObject("Inventory authority test 112");
            var inventory = owner.AddComponent<CompactInventoryPresenter069>();
            var authority = new EquipmentCoordinator112();
            try
            {
                inventory.Begin(host, authority, "HERO112");
                yield return null;
                Button Find(string name) => canvasObject.GetComponentsInChildren<Button>(true)
                    .Last(value => value.name == name && value.gameObject.activeInHierarchy);
                Assert.That(authority.HeroCalls + authority.UnionCalls + authority.UndoCalls, Is.Zero);
                Assert.That(Find("Undo Auto Equip 112").interactable, Is.False);
                PointerClick112(eventSystem, Find("Undo Auto Equip 112"), false);
                Assert.That(authority.UndoCalls, Is.Zero, "Disabled Undo must reject pointer input.");
                PointerClick112(eventSystem, Find("Auto Equip Hero 112"));
                yield return null;
                Assert.That(authority.HeroCalls, Is.EqualTo(1));
                Assert.That(authority.SelectedHero, Is.EqualTo("HERO112"));
                Assert.That(Find("Undo Auto Equip 112").interactable, Is.True);
                Assert.That(canvasObject.GetComponentsInChildren<Text>(true).Any(text =>
                    text.text.Contains("physical +3, mystic +1")), Is.True);
                AssertSummaryIntercepts112(eventSystem, Find("Auto Equip Hero 112"));
                AssertSummaryIntercepts112(eventSystem, Find("Auto Equip All Unions 112"));
                AssertSummaryIntercepts112(eventSystem, Find("Undo Auto Equip 112"));
                PointerClick112(eventSystem, Find("Close Auto Equip Summary 112"));
                yield return null;
                Assert.That(canvasObject.GetComponentsInChildren<Transform>(true).Any(value =>
                    value.name == "Auto Equip Summary 112" && value.gameObject.activeInHierarchy), Is.False);
                PointerClick112(eventSystem, Find("Auto Equip All Unions 112"));
                yield return null;
                Assert.That(authority.UnionCalls, Is.EqualTo(1));
                AssertSummaryIntercepts112(eventSystem, Find("Undo Auto Equip 112"));
                PointerClick112(eventSystem, Find("Close Auto Equip Summary 112"));
                yield return null;
                PointerClick112(eventSystem, Find("Undo Auto Equip 112"));
                yield return null;
                Assert.That(authority.UndoCalls, Is.EqualTo(1));
                Assert.That(Find("Undo Auto Equip 112").interactable, Is.False);
                PointerClick112(eventSystem, Find("Close Auto Equip Summary 112"));
                yield return null;
                PointerClick112(eventSystem, Find("Undo Auto Equip 112"), false);
                Assert.That(authority.HeroCalls, Is.EqualTo(1));
                Assert.That(authority.UnionCalls, Is.EqualTo(1));
                Assert.That(authority.UndoCalls, Is.EqualTo(1), "Each usable action must run exactly once; disabled Undo must stay inert.");
            }
            finally
            {
                inventory.Shutdown();
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(canvasObject);
                if (ownedEventSystem != null) UnityEngine.Object.DestroyImmediate(ownedEventSystem);
            }
        }

        static PointerEventData RaycastButtonCenter112(EventSystem eventSystem, Button button)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(button.IsActive(), Is.True, button.name + " must be active.");
            var rect = (RectTransform)button.transform;
            Assert.That(rect.rect.width, Is.GreaterThan(1f), button.name + " must have visible width.");
            Assert.That(rect.rect.height, Is.GreaterThan(1f), button.name + " must have visible height.");
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0f, (float)Screen.width), button.name + " center must be on screen.");
            Assert.That(point.y, Is.InRange(0f, (float)Screen.height), button.name + " center must be on screen.");
            var pointer = new PointerEventData(eventSystem)
            {
                pointerId = -1, button = PointerEventData.InputButton.Left,
                position = point, pressPosition = point, eligibleForClick = true, clickCount = 1
            };
            var hits = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " center must hit the UI.");
            pointer.pointerCurrentRaycast = hits[0];
            pointer.pointerPressRaycast = hits[0];
            return pointer;
        }

        static void PointerClick112(EventSystem eventSystem, Button button, bool interactable = true)
        {
            Assert.That(button.IsInteractable(), Is.EqualTo(interactable), button.name + " enabled state.");
            var pointer = RaycastButtonCenter112(eventSystem, button);
            var hit = pointer.pointerCurrentRaycast.gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject),
                button.name + " is intercepted by " + hit.name + ".");
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(pointer.pointerPress, Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        static void AssertSummaryIntercepts112(EventSystem eventSystem, Button underlyingButton)
        {
            var pointer = RaycastButtonCenter112(eventSystem, underlyingButton);
            var hit = pointer.pointerCurrentRaycast.gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.Not.EqualTo(underlyingButton.gameObject),
                "The summary must block input to " + underlyingButton.name + ".");
            Assert.That(hit.GetComponentsInParent<Transform>().Any(value => value.name == "Auto Equip Summary 112"), Is.True,
                "The top hit must belong to the visible equipment summary, not an unrelated intercepting UI element.");
        }

        sealed class EquipmentCoordinator112 : IM1PresentationCoordinator, IAutoEquipmentCoordinator112
        {
            public event Action Changed { add { } remove { } }
            public int HeroCalls, UnionCalls, UndoCalls;
            public string SelectedHero;
            public bool CanUndoAutoEquip112 { get; private set; }
            public M1PresentationState State { get; } = new M1PresentationState
            {
                HasCampaign = true,
                Recruits = new[] { new M1RecruitLoadoutView { RecruitId = "HERO112", DisplayName = "Test Hero",
                    ObservedClass = "Guardian", Level = 1, Slots = new[] { new M1EquipmentSlotView {
                        SlotId = "SLOT_MAIN_HAND", DisplayName = "Weapon" } } } }
            };
            public M1CommandResult AutoEquipHero112(string recruitId)
            {
                HeroCalls++; SelectedHero = recruitId; CanUndoAutoEquip112 = true;
                return M1CommandResult.Success("1 item changes across 1 heroes.\nTest Hero: physical +3, mystic +1.");
            }
            public M1CommandResult AutoEquipAllUnions112()
            { UnionCalls++; CanUndoAutoEquip112 = true; return M1CommandResult.Success("2 item changes across 2 heroes."); }
            public M1CommandResult UndoAutoEquip112()
            { UndoCalls++; CanUndoAutoEquip112 = false; return M1CommandResult.Success("Previous equipment restored."); }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => throw new NotSupportedException();
            public M1CommandResult SignRecruit(string recruitId) => throw new NotSupportedException();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => throw new NotSupportedException();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => throw new NotSupportedException();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => throw new NotSupportedException();
            public M1CommandResult CompleteEquipmentReview() => throw new NotSupportedException();
            public M1CommandResult AddUnion() => throw new NotSupportedException();
            public M1CommandResult RemoveUnion(int unionIndex) => throw new NotSupportedException();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => throw new NotSupportedException();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => throw new NotSupportedException();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => throw new NotSupportedException();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => throw new NotSupportedException();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => throw new NotSupportedException();
            public M1CommandResult SaveAndReloadProof() => throw new NotSupportedException();
        }
    }
}
