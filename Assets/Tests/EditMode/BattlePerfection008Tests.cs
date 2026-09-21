using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattlePerfection008Tests
    {
        [Test]
        public void TenVersusTenCreatesTwentyReadablePresentationDirectives()
        {
            var battle = Battle(10, 10, 5);
            var request = Request(BattlePresentationMode008.CommandFocus, "A03", "E02");
            request.Relationships = new[]
            {
                Relationship("A03", "E02", BattleRelationshipKind008.Deadlock),
                Relationship("A07", "E02", BattleRelationshipKind008.SideStrike),
                Relationship("E07", "E02", BattleRelationshipKind008.Support)
            };

            var plan = BattlePerfectionPlanning008.Plan(battle, request);

            Assert.That(plan.Directives.Count, Is.EqualTo(20));
            Assert.That(plan.Directives.Count(value => value.Lod == BattleUnionLod008.HeroUnion), Is.EqualTo(2));
            Assert.That(plan.Directives.Count(value => value.Lod == BattleUnionLod008.ContextUnion), Is.LessThanOrEqualTo(2));
            Assert.That(plan.Directives.Sum(value => value.VisibleMemberLimit), Is.LessThanOrEqualTo(plan.Budget.MaxVisibleMembers));
            Assert.That(plan.Directives.All(value => value.KeepNavigatorPlate), Is.True);
            Assert.That(plan.Layout.NavigatorChips.Count, Is.EqualTo(20));
            Assert.That(BattlePerfectionAudit008.TryValidate(plan, out var error), Is.True, error);
        }

        [Test]
        public void SameInputProducesSameStablePresentationPlan()
        {
            var battle = Battle(10, 10, 5);
            var request = Request(BattlePresentationMode008.CinematicAction, "A01", "E01");
            request.Relationships = new[]
            {
                Relationship("A01", "E01", BattleRelationshipKind008.Deadlock),
                Relationship("A06", "E01", BattleRelationshipKind008.SideStrike)
            };

            var first = BattlePerfectionPlanning008.Plan(battle, request);
            var second = BattlePerfectionPlanning008.Plan(battle, request);

            Assert.That(second.StableDescriptor, Is.EqualTo(first.StableDescriptor));
            Assert.That(second.OpeningShot, Is.EqualTo(first.OpeningShot));
            Assert.That(second.Layout.NavigatorChips.Select(value => value.StableDescriptor),
                Is.EqualTo(first.Layout.NavigatorChips.Select(value => value.StableDescriptor)));
        }

        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        [TestCase(19.5f / 9f)]
        public void SafeLayoutsKeepAllTwentyNavigatorChipsInsideNormalizedSafeArea(float aspect)
        {
            var battle = Battle(10, 10, 5);
            var request = Request(BattlePresentationMode008.CommandFocus, "A01", "E01");
            request.AspectRatio = aspect;
            request.NormalizedSafeArea = new Rect(0.025f, 0.03f, 0.95f, 0.94f);
            var plan = BattlePerfectionPlanning008.Plan(battle, request);

            Assert.That(plan.Layout.NavigatorChips.Count, Is.EqualTo(20));
            Assert.That(plan.Layout.NavigatorChips.All(value =>
                value.NormalizedRect.xMin >= plan.Layout.SafeArea.xMin - 0.0001f &&
                value.NormalizedRect.yMin >= plan.Layout.SafeArea.yMin - 0.0001f &&
                value.NormalizedRect.xMax <= plan.Layout.SafeArea.xMax + 0.0001f &&
                value.NormalizedRect.yMax <= plan.Layout.SafeArea.yMax + 0.0001f), Is.True);
            Assert.That(plan.Layout.Battlefield.width, Is.GreaterThan(0.2f));
            Assert.That(plan.Layout.Battlefield.height, Is.GreaterThan(0.2f));
        }

        [Test]
        public void ReducedMotionPreservesActionMeaningAndImpactOrdering()
        {
            var normal = BattleActionPresentationPlanning008.Schedule(
                BattleActionFamily008.Mystic, BattleRelationshipKind008.None, false);
            var reduced = BattleActionPresentationPlanning008.Schedule(
                BattleActionFamily008.Mystic, BattleRelationshipKind008.None, true);

            Assert.That(reduced.Family, Is.EqualTo(normal.Family));
            Assert.That(reduced.Shot, Is.EqualTo(normal.Shot));
            Assert.That(reduced.Beats.Select(value => value.Name), Is.EqualTo(normal.Beats.Select(value => value.Name)));
            Assert.That(reduced.HitTimeSeconds, Is.GreaterThan(0f));
            Assert.That(reduced.TotalDurationSeconds, Is.LessThan(normal.TotalDurationSeconds));
            Assert.That(reduced.Beats.Single(value => value.ImpactBeat).Name, Is.EqualTo("contact_or_release"));
        }

        [Test]
        public void RelationshipAndActionFamiliesChoosePurposefulShots()
        {
            Assert.That(BattleActionPresentationPlanning008.SelectShot(
                BattleActionFamily008.Restoration, BattleRelationshipKind008.None),
                Is.EqualTo(BattleShot008.HealerRecipient));
            Assert.That(BattleActionPresentationPlanning008.SelectShot(
                BattleActionFamily008.BasicMartial, BattleRelationshipKind008.SideStrike),
                Is.EqualTo(BattleShot008.FlankReveal));
            Assert.That(BattleActionPresentationPlanning008.SelectShot(
                BattleActionFamily008.BasicMartial, BattleRelationshipKind008.RearAttack),
                Is.EqualTo(BattleShot008.RearAttack));
            Assert.That(BattleActionPresentationPlanning008.SelectShot(
                BattleActionFamily008.Guard, BattleRelationshipKind008.Protect),
                Is.EqualTo(BattleShot008.GuardInterception));
        }

        [Test]
        public void PlanningDoesNotChangeAuthoritativeBattleHashOrExposeIndividualArtSelection()
        {
            var battle = Battle(10, 10, 5);
            battle.StateHash = "AUTHORITATIVE_HASH_008";
            var before = battle.StateHash;
            BattlePerfectionPlanning008.Plan(
                battle, Request(BattlePresentationMode008.CommandFocus, "A01", "E01"));
            Assert.That(battle.StateHash, Is.EqualTo(before));

            var publicMethods = typeof(IM2PresentationCoordinator).GetMethods().Select(value => value.Name).ToArray();
            Assert.That(publicMethods.Any(value =>
                value.IndexOf("Individual", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(publicMethods.Any(value =>
                value.IndexOf("Art", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (value.StartsWith("Select", StringComparison.OrdinalIgnoreCase) ||
                 value.StartsWith("Choose", StringComparison.OrdinalIgnoreCase))), Is.False);
        }

        [Test]
        public void DefeatedUnionsRemainReadableWithoutReceivingHeroAnimationBudget()
        {
            var battle = Battle(10, 10, 5);
            battle.EnemyUnions[9].Members = battle.EnemyUnions[9].Members.Select(value =>
                new M2BattleMemberView
                {
                    MemberId = value.MemberId,
                    DisplayName = value.DisplayName,
                    Downed = true
                }).ToArray();
            var plan = BattlePerfectionPlanning008.Plan(
                battle, Request(BattlePresentationMode008.CinematicAction, "A01", "E01"));
            var defeated = plan.Directives.Single(value => value.UnionId == "E10");

            Assert.That(defeated.Defeated, Is.True);
            Assert.That(defeated.KeepNavigatorPlate, Is.True);
            Assert.That(defeated.Lod, Is.Not.EqualTo(BattleUnionLod008.HeroUnion));
        }

        private static BattlePresentationRequest008 Request(
            BattlePresentationMode008 mode,
            string active,
            string target) => new BattlePresentationRequest008
        {
            Mode = mode,
            ActiveUnionId = active,
            TargetUnionId = target,
            AspectRatio = 16f / 9f,
            NormalizedSafeArea = new Rect(0f, 0f, 1f, 1f),
            DeviceProfile = BattleDeviceProfile008.Desktop
        };

        private static BattleRelationship008 Relationship(
            string source,
            string target,
            BattleRelationshipKind008 kind) => new BattleRelationship008
        {
            SourceUnionId = source,
            TargetUnionId = target,
            Kind = kind,
            Active = true
        };

        private static M2BattleView Battle(int allies, int enemies, int members)
        {
            return new M2BattleView
            {
                BattleId = "BATTLE_PERFECTION_008",
                PlayerUnions = Enumerable.Range(1, allies).Select(index => Union("A" + index.ToString("00"), false, members)).ToArray(),
                EnemyUnions = Enumerable.Range(1, enemies).Select(index => Union("E" + index.ToString("00"), true, members)).ToArray(),
                StateHash = "HASH_008"
            };
        }

        private static M2BattleUnionView Union(string id, bool enemy, int members)
        {
            return new M2BattleUnionView
            {
                UnionId = id,
                DisplayName = id,
                Side = enemy ? "Enemy" : "Player",
                CanAct = true,
                Engagement = "Open",
                Members = Enumerable.Range(1, members).Select(index => new M2BattleMemberView
                {
                    MemberId = id + "_M" + index,
                    DisplayName = id + " Member " + index,
                    CurrentHp = 100,
                    MaximumHp = 100,
                    CurrentMp = 20,
                    MaximumMp = 20
                }).ToArray()
            };
        }
    }
}
