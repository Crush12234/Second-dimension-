using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class UnionBattlePlan132PlayModeTests
    {
        [UnityTest]
        public IEnumerator ReserveSlotFailureRetryAndSwapKeepOneClearSelection132()
        {
            var host = new GameObject("Union plan132 fixture");
            var view = host.AddComponent<M1FlowPresenter>();
            view.enabled = false;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(M1FlowPresenter).GetField("_screen", flags).SetValue(view, M1Screen.UnionBuilder);
            var owner = new Owner132();
            view.Initialize(owner);
            var canvas = (Canvas)typeof(M1FlowPresenter).GetField("_canvas", flags).GetValue(view);
            Button Find(string name) => canvas.GetComponentsInChildren<Button>(true).Last(value => value.name == name && value.gameObject.activeInHierarchy);
            try
            {
                yield return null;
                yield return null;
                Assert.That(canvas.GetComponentsInChildren<Transform>(true).Any(value => value.name == "Union Planner Formation And Intent 074" && value.gameObject.activeInHierarchy), Is.False);
                Assert.That(Find("Union Planner Options 132").GetComponentInChildren<Text>().text, Does.Contain("FORMATION"));
                Find("Union Planner Reserve Member R4 074").onClick.Invoke();
                yield return null;
                Assert.That(owner.Calls, Is.Zero);
                Find("Union Planner Member Slot 2 074").onClick.Invoke();
                yield return null;
                Assert.That(owner.Calls, Is.EqualTo(1));
                Assert.That(Find("Union Planner Reserve Member R4 074").GetComponentInChildren<Text>().text, Does.Contain("SELECTED"), "A failed save keeps the intended hero selected for retry.");
                Assert.That(canvas.GetComponentsInChildren<Text>(true).Any(value => value.text.Contains("Save failed. Retry.")), Is.True);
                owner.Fail = false;
                Find("Union Planner Member Slot 2 074").onClick.Invoke();
                yield return null;
                Assert.That(owner.State.Unions[0].MemberRecruitIds, Is.EqualTo(new[] { "R0", "R1", "R4" }));
                Assert.That((string)typeof(M1FlowPresenter).GetField("_unionSelectedReserve109", flags).GetValue(view), Is.Null);
                Find("Union Planner Reserve Member R3 074").onClick.Invoke();
                yield return null;
                Find("Union Planner Member Slot 0 074").onClick.Invoke();
                yield return null;
                Assert.That(owner.Calls, Is.EqualTo(2));
                Find("Confirm").onClick.Invoke();
                yield return null;
                Assert.That(owner.Calls, Is.EqualTo(3));
                Assert.That(owner.State.Unions[0].MemberRecruitIds, Is.EqualTo(new[] { "R3", "R1", "R4" }));
                Assert.That(Find("Union Planner Reserve Member R0 074"), Is.Not.Null);
                Assert.That(canvas.GetComponentsInChildren<Text>(true).Any(value => value.text.Contains("Applies next battle")), Is.True);
                owner.Editable = false;
                // Rebuild only reflects an external lock; it does not dispatch.
                typeof(M1FlowPresenter).GetMethod("BuildCurrentScreen", flags).Invoke(view, null);
                yield return null;
                Assert.That(Find("Union Planner Reserve Member R0 074").interactable, Is.False);
                Assert.That(Find("Union Planner Member Slot 0 074").interactable, Is.False);
                Find("Union Planner Member Slot 0 074").onClick.Invoke();
                Assert.That(owner.Calls, Is.EqualTo(3), "Retained callbacks also respect the combat lock.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
        }

        sealed class Owner132 : IM1PresentationCoordinator, IUnionReserveAssignmentCoordinator109, IUnionPlanningCoordinator132
        {
            public event Action Changed { add { } remove { } }
            public bool Fail = true, Editable = true;
            public int Calls;
            public bool UnionPlanEditable132 => Editable;
            public string UnionPlanStatus132 => Editable ? "Applies next battle." : "Finish the battle and claim its rewards before changing Unions.";
            public M1PresentationState State { get; } = new M1PresentationState
            {
                HasCampaign = true, OpeningUnionsLegal = true,
                Recruits = Enumerable.Range(0, 5).Select(i => new M1RecruitLoadoutView { RecruitId = "R" + i, DisplayName = "Hero " + i, ObservedClass = "Guardian", Level = 1 }).ToArray(),
                Unions = new[] { new M1UnionView { Index = 0, UnionId = "U0", DisplayName = "First Union", MemberRecruitIds = new[] { "R0", "R1" }, IsLegal = true } }
            };
            public M1CommandResult AssignReserveRecruitToUnion109(string id, int unionIndex, int slot, string occupant)
            {
                Calls++;
                if (Fail) return M1CommandResult.Failure("Save failed. Retry.");
                var members = State.Unions[unionIndex].MemberRecruitIds.ToList();
                if (slot == members.Count) members.Add(id); else members[slot] = id;
                State.Unions[unionIndex].MemberRecruitIds = members.ToArray();
                return M1CommandResult.Success("Union saved. Applies next battle.");
            }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => throw new NotSupportedException();
            public M1CommandResult SignRecruit(string recruitId) => throw new NotSupportedException();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => throw new NotSupportedException();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => throw new NotSupportedException();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => throw new NotSupportedException();
            public M1CommandResult CompleteEquipmentReview() => throw new NotSupportedException();
            public M1CommandResult AddUnion() => throw new NotSupportedException();
            public M1CommandResult RemoveUnion(int index) => throw new NotSupportedException();
            public M1CommandResult AssignRecruitToUnion(string id, int union, int slot) => throw new NotSupportedException();
            public M1CommandResult UnassignRecruitFromUnion(string id) => throw new NotSupportedException();
            public M1CommandResult SetUnionLeader(int index, string id) => throw new NotSupportedException();
            public M1CommandResult SetFormation(int index, string id) => throw new NotSupportedException();
            public M1CommandResult SetDoctrine(int index, string id) => throw new NotSupportedException();
            public M1CommandResult SaveAndReloadProof() => throw new NotSupportedException();
        }
    }
}
