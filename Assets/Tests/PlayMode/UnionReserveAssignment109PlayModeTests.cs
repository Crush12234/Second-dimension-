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
    public sealed class UnionReserveAssignment109PlayModeTests
    {
        static FieldInfo Field109(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static void Set109(object target, string name, object value) => Field109(target.GetType(), name).SetValue(target, value);

        [UnityTest]
        public IEnumerator RealReserveAndSlotButtonsRequireSelectionAndExplicitSwap109()
        {
            var host = new GameObject("Union click regression 109");
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.enabled = false;
            var fake = new UnionCoordinator109();
            Set109(presenter, "_screen", M1Screen.UnionBuilder);
            presenter.Initialize(fake);
            var canvas = (Canvas)Field109(typeof(M1FlowPresenter), "_canvas").GetValue(presenter);
            try
            {
                yield return null;
                Button Find(string name) => canvas.GetComponentsInChildren<Button>(true)
                    .Last(value => value.name == name && value.gameObject.activeInHierarchy);
                var reserve = Find("Union Planner Reserve Member R4 074");
                Assert.That(reserve.interactable, Is.True);
                reserve.onClick.Invoke();
                yield return null;
                Assert.That(fake.Placements, Is.Zero, "Selecting a reserve cannot mutate the roster.");
                Assert.That(Find("Union Planner Reserve Member R4 074").GetComponentInChildren<Text>().text,
                    Does.Contain("SELECTED"));
                Assert.That(Find("Union Planner Member Slot 2 074").interactable, Is.True);
                Assert.That(Find("Union Planner Member Slot 5 074").interactable, Is.False);
                Find("Union Planner Member Slot 2 074").onClick.Invoke();
                yield return null;
                Assert.That(fake.Placements, Is.EqualTo(1));
                Assert.That(fake.LastSlot, Is.EqualTo(2));
                Assert.That(fake.LastOccupant, Is.Null);

                Find("Union Planner Reserve Member R4 074").onClick.Invoke();
                yield return null;
                Find("Union Planner Member Slot 0 074").onClick.Invoke();
                yield return null;
                Assert.That(fake.Placements, Is.EqualTo(1), "Occupied slots need the explicit modal action.");
                Assert.That(Find("Confirm").GetComponentInChildren<Text>().text, Is.EqualTo("SWAP MEMBERS"));
                Assert.That(canvas.GetComponentsInChildren<Text>(true).Any(value =>
                    value.text.Contains("Member 4 will take slot 1. Member 0 will return to reserve.")), Is.True);
                Find("Confirm").onClick.Invoke();
                yield return null;
                Assert.That(fake.Placements, Is.EqualTo(2));
                Assert.That(fake.LastSlot, Is.EqualTo(0));
                Assert.That(fake.LastOccupant, Is.EqualTo("R0"));
                Assert.That(fake.Unassignments, Is.Zero, "Swap selection must never remove the incumbent first.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
        }

        sealed class UnionCoordinator109 : IM1PresentationCoordinator, IUnionReserveAssignmentCoordinator109
        {
            public event Action Changed { add { } remove { } }
            public int Placements, Unassignments, LastSlot;
            public string LastOccupant;
            public M1PresentationState State { get; } = new M1PresentationState
            {
                HasCampaign = true,
                OpeningUnionsLegal = true,
                Recruits = Enumerable.Range(0, 5).Select(index => new M1RecruitLoadoutView
                {
                    RecruitId = "R" + index, DisplayName = "Member " + index, ObservedClass = "Guardian", Level = 1
                }).ToArray(),
                Unions = new[] { new M1UnionView { Index = 0, UnionId = "U0", DisplayName = "Union One",
                    MemberRecruitIds = new[] { "R0", "R1" }, IsLegal = true } }
            };
            public M1CommandResult AssignReserveRecruitToUnion109(string recruitId, int unionIndex, int slotIndex, string expectedOccupantId)
            { Placements++; LastSlot = slotIndex; LastOccupant = expectedOccupantId; return M1CommandResult.Success(); }
            public M1CommandResult UnassignRecruitFromUnion(string recruitId)
            { Unassignments++; return M1CommandResult.Success(); }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => throw new NotSupportedException();
            public M1CommandResult SignRecruit(string recruitId) => throw new NotSupportedException();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => throw new NotSupportedException();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => throw new NotSupportedException();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => throw new NotSupportedException();
            public M1CommandResult CompleteEquipmentReview() => throw new NotSupportedException();
            public M1CommandResult AddUnion() => throw new NotSupportedException();
            public M1CommandResult RemoveUnion(int unionIndex) => throw new NotSupportedException();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => throw new NotSupportedException();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => throw new NotSupportedException();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => throw new NotSupportedException();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => throw new NotSupportedException();
            public M1CommandResult SaveAndReloadProof() => throw new NotSupportedException();
        }
    }
}
