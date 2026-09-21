using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    // Real canvas, layout and pointer dispatch with a controlled async owner.
    // The real candidate/store/Undo parity is covered by AutoEquipmentAsync123Tests.
    public sealed class AutoEquipmentAsync123PlayModeTests
    {
        [UnityTest]
        public IEnumerator ParentChangedDefersPendingEquipmentAndInactivePublication123()
        {
            using (var ui = new InventoryFixture123())
            {
                var owner = new EquipmentOwner123();
                ui.Begin(owner);
                yield return null;
                ui.Click(ui.Button("Auto Equip Hero 112"));
                var parentHost = new GameObject("Equipment parent event test 123");
                try
                {
                    var parent = parentHost.AddComponent<M1FlowPresenter>();
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    typeof(M1FlowPresenter).GetField("_coordinator", flags).SetValue(parent, owner);
                    typeof(M1FlowPresenter).GetField("_compactInventory069", flags).SetValue(parent, ui.Inventory);
                    var changed = (Action)Delegate.CreateDelegate(typeof(Action), parent,
                        typeof(M1FlowPresenter).GetMethod("HandleCoordinatorChanged", flags));
                    owner.Changed += changed;
                    var reads = owner.StateReads;
                    owner.PublishChanged();
                    Assert.That(owner.StateReads, Is.EqualTo(reads));
                    parent.enabled = false;
                    owner.PublishChanged();
                    Assert.That(owner.StateReads, Is.EqualTo(reads));
                    Assert.That(typeof(M1FlowPresenter).GetField("_towerManualRefreshOnEnable117", flags).GetValue(parent),
                        Is.EqualTo(true), "An inactive parent must defer publication to its existing enable refresh.");
                    // Keep this focused on the actual notification boundary; no title/Boot startup is needed.
                    typeof(M1FlowPresenter).GetField("_compactInventory069", flags).SetValue(parent, null);
                }
                finally { UnityEngine.Object.DestroyImmediate(parentHost); }
                owner.Complete(M1CommandResult.Success("Equipment saved."), true);
                yield return null;
                yield return null;
                Assert.That(ui.Text("Auto Equip Factual Changes 112"), Is.EqualTo("Equipment saved."));
            }
        }

        [UnityTest]
        public IEnumerator HeroAllAndUndoUseAsyncButtonsAndPollOnlyCheapPhase123()
        {
            using (var ui = new InventoryFixture123())
            {
                var owner = new EquipmentOwner123();
                ui.Begin(owner);
                yield return null;
                foreach (var action in new[] { "Hero", "Unions", "Undo" })
                {
                    var button = ui.Button(action == "Hero" ? "Auto Equip Hero 112" :
                        action == "Unions" ? "Auto Equip All Unions 112" : "Undo Auto Equip 112");
                    var reads = owner.StateReads;
                    var undoReads = owner.UndoReads;
                    ui.Click(button);
                    Assert.That(owner.LastAction, Is.EqualTo(action));
                    Assert.That(owner.SyncCalls, Is.Zero);
                    Assert.That(ui.Inventory.IsAutoEquipmentResolving123, Is.True);
                    Assert.That(ui.Text("Auto Equip Pending Phase 123"), Does.Contain("CHECKING EQUIPMENT"));
                    // A retained input callback is also inert while its owner is pending.
                    button.onClick.Invoke();
                    Assert.That(owner.TotalCalls, Is.EqualTo(action == "Hero" ? 1 : action == "Unions" ? 2 : 3));
                    var firstFrame = Time.frameCount;
                    yield return null;
                    yield return null;
                    ui.AssertCoveredByPending(button);
                    owner.Saving = true;
                    yield return null;
                    Assert.That(Time.frameCount, Is.GreaterThan(firstFrame));
                    Assert.That(ui.Text("Auto Equip Pending Phase 123"), Does.Contain("SAVING EQUIPMENT"));
                    owner.PublishChanged(); // Actual coordinator publishes before its owner task completes.
                    Assert.That(owner.StateReads, Is.EqualTo(reads), "No full state projection during preparation/save/publication.");
                    Assert.That(owner.UndoReads, Is.EqualTo(undoReads), "Do not canonical-hash Undo while polling a phase.");
                    var summary = action + " saved: physical +3, mystic +1.";
                    owner.Complete(M1CommandResult.Success(summary), action != "Undo");
                    yield return null;
                    yield return null;
                    Assert.That(ui.Inventory.IsAutoEquipmentResolving123, Is.False);
                    Assert.That(owner.StateReads, Is.EqualTo(reads + 1), "Read the final/current state once after completion.");
                    Assert.That(ui.Text("Auto Equip Factual Changes 112"), Is.EqualTo(summary));
                    Assert.That(ui.Has("Auto Equip Pending 123"), Is.False);
                    ui.Click(ui.Button("Close Auto Equip Summary 112"));
                    yield return null;
                }
                Assert.That(owner.HeroId, Is.EqualTo("HERO123"));
                Assert.That(owner.TotalCalls, Is.EqualTo(3));
                Assert.That(owner.SyncCalls, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator ReturnBeforeSaveCancelsAndDetachedResultDoesNotReopenInventory123()
        {
            using (var ui = new InventoryFixture123())
            {
                var owner = new EquipmentOwner123();
                ui.Begin(owner);
                yield return null;
                ui.Click(ui.Button("Auto Equip Hero 112"));
                yield return null;
                var reads = owner.StateReads;
                ui.Click(ui.Button("Return From Pending Auto Equip 123"));
                Assert.That(owner.CancelCalls, Is.GreaterThanOrEqualTo(1));
                Assert.That(owner.CanceledBeforeSave, Is.True);
                Assert.That(ui.Inventory.IsRunningForTests, Is.False);
                owner.PublishChanged();
                owner.Complete(M1CommandResult.Failure("Canceled before saving."));
                yield return null;
                yield return null;
                Assert.That(owner.StateReads, Is.EqualTo(reads));
                Assert.That(ui.Has("Auto Equip Summary 112"), Is.False);
                Assert.That(ui.Has(CompactInventoryPresenter069.RootObjectName), Is.False);
            }
        }

        [UnityTest]
        public IEnumerator SavingSurvivesDisableAndFreshInventoryReattachesWithoutDispatchOrReads123()
        {
            using (var ui = new InventoryFixture123())
            {
                var owner = new EquipmentOwner123();
                ui.Begin(owner);
                yield return null;
                ui.Click(ui.Button("Auto Equip All Unions 112"));
                owner.Saving = true;
                yield return null;
                ui.Host.SetActive(false); // Real OnDisable -> Shutdown -> detach, never releases the owner save.
                Assert.That(owner.CanceledBeforeSave, Is.False);
                Assert.That(owner.PendingAutoEquipment123, Is.Not.Null);
                var reads = owner.StateReads;
                ui.Host.SetActive(true);
                ui.Begin(owner);
                yield return null;
                Assert.That(owner.TotalCalls, Is.EqualTo(1));
                Assert.That(owner.StateReads, Is.EqualTo(reads), "Fresh entry must attach to the pending owner before reading expensive state.");
                Assert.That(ui.Text("Auto Equip Pending Phase 123"), Does.Contain("SAVING EQUIPMENT"));
                owner.PublishChanged();
                Assert.That(owner.StateReads, Is.EqualTo(reads));
                owner.Complete(M1CommandResult.Success("All Unions saved once."), true);
                yield return null;
                yield return null;
                Assert.That(owner.TotalCalls, Is.EqualTo(1));
                Assert.That(owner.StateReads, Is.EqualTo(reads + 1));
                Assert.That(ui.Text("Auto Equip Factual Changes 112"), Is.EqualTo("All Unions saved once."));
            }
        }

        [UnityTest]
        public IEnumerator ReplacingOwnerIgnoresOldCompletionAndFailedActionAllowsActualPointerRetry123()
        {
            using (var ui = new InventoryFixture123())
            {
                var old = new EquipmentOwner123();
                ui.Begin(old);
                yield return null;
                ui.Click(ui.Button("Auto Equip Hero 112"));
                old.Saving = true;
                var current = new EquipmentOwner123();
                ui.Begin(current);
                yield return null;
                var currentReads = current.StateReads;
                old.PublishChanged();
                old.Complete(M1CommandResult.Success("Stale owner result."), true);
                yield return null;
                Assert.That(current.StateReads, Is.EqualTo(currentReads));
                Assert.That(ui.Has("Auto Equip Summary 112"), Is.False);
                ui.Click(ui.Button("Auto Equip Hero 112"));
                current.Complete(M1CommandResult.Failure("SAVE_WRITE_FAILED: previous equipment kept."));
                yield return null;
                yield return null;
                Assert.That(ui.Text("Auto Equip Factual Changes 112"), Does.Contain("SAVE_WRITE_FAILED"));
                ui.Click(ui.Button("Close Auto Equip Summary 112"));
                yield return null;
                ui.Click(ui.Button("Auto Equip Hero 112"));
                Assert.That(current.TotalCalls, Is.EqualTo(2));
                current.Complete(M1CommandResult.Success("Retry saved once."), true);
                yield return null;
                yield return null;
                Assert.That(ui.Text("Auto Equip Factual Changes 112"), Is.EqualTo("Retry saved once."));
                Assert.That(current.SyncCalls, Is.Zero);
            }
        }

        sealed class InventoryFixture123 : IDisposable
        {
            readonly GameObject _canvas, _ownedEventSystem;
            readonly EventSystem _events;
            internal readonly GameObject Host;
            internal readonly CompactInventoryPresenter069 Inventory;
            internal InventoryFixture123()
            {
                _canvas = new GameObject("Async equipment canvas 123", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = _canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2796f, 1290f);
                scaler.matchWidthOrHeight = 0.5f;
                _events = EventSystem.current;
                if (_events == null)
                {
                    _ownedEventSystem = new GameObject("Async equipment input 123", typeof(EventSystem));
                    _events = _ownedEventSystem.GetComponent<EventSystem>();
                }
                Host = new GameObject("Async inventory host 123");
                Inventory = Host.AddComponent<CompactInventoryPresenter069>();
            }
            internal void Begin(EquipmentOwner123 owner) => Inventory.Begin(_canvas.transform, owner, "HERO123", Inventory.Shutdown);
            internal Button Button(string name) => _canvas.GetComponentsInChildren<Button>(true).Last(x => x.name == name && x.gameObject.activeInHierarchy);
            internal string Text(string name) => _canvas.GetComponentsInChildren<Text>(true).Last(x => x.name == name && x.gameObject.activeInHierarchy).text;
            internal bool Has(string name) => _canvas.GetComponentsInChildren<Transform>(true).Any(x => x.name == name && x.gameObject.activeInHierarchy);
            PointerEventData Raycast(Button button)
            {
                Assert.That(button.IsActive(), Is.True);
                var rect = (RectTransform)button.transform;
                Assert.That(rect.rect.width, Is.GreaterThan(1f));
                Assert.That(rect.rect.height, Is.GreaterThan(1f));
                var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                Assert.That(point.x, Is.InRange(0f, (float)Screen.width));
                Assert.That(point.y, Is.InRange(0f, (float)Screen.height));
                var pointer = new PointerEventData(_events) { pointerId = -1, button = PointerEventData.InputButton.Left,
                    position = point, pressPosition = point, eligibleForClick = true, clickCount = 1 };
                var hits = new List<RaycastResult>();
                _events.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty);
                pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
                return pointer;
            }
            internal void Click(Button button)
            {
                Assert.That(button.IsInteractable(), Is.True);
                var pointer = Raycast(button);
                var hit = pointer.pointerCurrentRaycast.gameObject;
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject),
                    button.name + " is covered by " + hit.name);
                pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
                Assert.That(pointer.pointerPress, Is.EqualTo(button.gameObject));
                ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
            }
            internal void AssertCoveredByPending(Button button)
            {
                var hit = Raycast(button).pointerCurrentRaycast.gameObject;
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.Not.EqualTo(button.gameObject));
                Assert.That(hit.GetComponentsInParent<Transform>().Any(x => x.name == "Auto Equip Pending 123"), Is.True);
            }
            public void Dispose()
            {
                Inventory.Shutdown();
                UnityEngine.Object.DestroyImmediate(Host);
                UnityEngine.Object.DestroyImmediate(_canvas);
                if (_ownedEventSystem != null) UnityEngine.Object.DestroyImmediate(_ownedEventSystem);
            }
        }

        sealed class EquipmentOwner123 : IM1PresentationCoordinator, IAutoEquipmentCoordinator112, IAutoEquipmentAsyncCoordinator123
        {
            public event Action Changed;
            internal int StateReads, UndoReads, SyncCalls, TotalCalls, CancelCalls;
            internal string LastAction, HeroId;
            internal bool Saving, CanceledBeforeSave;
            bool _canUndo;
            TaskCompletionSource<M1CommandResult> _completion;
            public Task<M1CommandResult> PendingAutoEquipment123 { get; private set; }
            public bool AutoEquipmentIsSaving123 => PendingAutoEquipment123 != null && Saving;
            public bool CanUndoAutoEquip112 { get { UndoReads++; return _canUndo; } }
            public M1PresentationState State
            {
                get
                {
                    StateReads++;
                    return new M1PresentationState { HasCampaign = true, Recruits = new[] {
                        new M1RecruitLoadoutView { RecruitId = "HERO123", DisplayName = "Test Hero", ObservedClass = "Guardian", Level = 1,
                            Slots = new[] { new M1EquipmentSlotView { SlotId = "SLOT_MAIN_HAND", DisplayName = "Weapon" } } } } };
                }
            }
            Task<M1CommandResult> Start(string mode)
            {
                Assert.That(PendingAutoEquipment123, Is.Null);
                TotalCalls++; LastAction = mode; Saving = false; CanceledBeforeSave = false;
                _completion = new TaskCompletionSource<M1CommandResult>();
                return PendingAutoEquipment123 = _completion.Task;
            }
            public Task<M1CommandResult> AutoEquipHeroAsync123(string recruitId) { HeroId = recruitId; return Start("Hero"); }
            public Task<M1CommandResult> AutoEquipAllUnionsAsync123() => Start("Unions");
            public Task<M1CommandResult> UndoAutoEquipAsync123() => Start("Undo");
            public void CancelAutoEquipmentBeforeSave123() { CancelCalls++; if (!Saving && PendingAutoEquipment123 != null) CanceledBeforeSave = true; }
            internal void PublishChanged() => Changed?.Invoke();
            internal void Complete(M1CommandResult result, bool canUndo = false)
            {
                _canUndo = canUndo;
                _completion.SetResult(result);
                PendingAutoEquipment123 = null;
                Saving = false;
            }
            public M1CommandResult AutoEquipHero112(string id) { SyncCalls++; throw new InvalidOperationException("Sync Hero must not run."); }
            public M1CommandResult AutoEquipAllUnions112() { SyncCalls++; throw new InvalidOperationException("Sync Unions must not run."); }
            public M1CommandResult UndoAutoEquip112() { SyncCalls++; throw new InvalidOperationException("Sync Undo must not run."); }
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
