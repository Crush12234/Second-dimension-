using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleUnionDeploymentPlanning020Tests
    {
        [Test]
        public void TenPerSideFillMirroredTwoColumnFiveRowOverview()
        {
            var plan = BattleUnionDeploymentPlanning020.Plan(Battle(10, 10));

            Assert.That(plan.Slots.Count, Is.EqualTo(20));
            Assert.That(plan.PlayerOverflowCount, Is.Zero);
            Assert.That(plan.EnemyOverflowCount, Is.Zero);

            var players = plan.Slots.Where(value => !value.Enemy).ToArray();
            var enemies = plan.Slots.Where(value => value.Enemy).ToArray();
            Assert.That(players.Select(value => value.SideOrdinal), Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(enemies.Select(value => value.SideOrdinal), Is.EqualTo(Enumerable.Range(1, 10)));

            for (var index = 0; index < 10; index++)
            {
                var expectedColumn = index / 5;
                var expectedRow = index % 5;
                Assert.That(players[index].Column, Is.EqualTo(expectedColumn));
                Assert.That(players[index].Row, Is.EqualTo(expectedRow));
                Assert.That(enemies[index].Column, Is.EqualTo(expectedColumn));
                Assert.That(enemies[index].Row, Is.EqualTo(expectedRow));
                Assert.That(players[index].Anchor.x, Is.EqualTo(-enemies[index].Anchor.x).Within(0.0001f));
                Assert.That(players[index].Anchor.z, Is.EqualTo(enemies[index].Anchor.z).Within(0.0001f));
                Assert.That(players[index].FacingYaw, Is.EqualTo(-enemies[index].FacingYaw));
                Assert.That(players[index].OverviewScale, Is.EqualTo(enemies[index].OverviewScale));
            }

            Assert.That(players.Take(5).All(value => Mathf.Approximately(value.Anchor.x, -8f)), Is.True);
            Assert.That(players.Skip(5).All(value => Mathf.Approximately(value.Anchor.x, -13f)), Is.True);
            Assert.That(enemies.Take(5).All(value => Mathf.Approximately(value.Anchor.x, 8f)), Is.True);
            Assert.That(enemies.Skip(5).All(value => Mathf.Approximately(value.Anchor.x, 13f)), Is.True);
        }

        [Test]
        public void AllTwentyAnchorsAreUniqueNonOverlappingAndArenaBounded()
        {
            var plan = BattleUnionDeploymentPlanning020.Plan(Battle(10, 10));
            var stablePositions = plan.Slots
                .Select(value => value.Anchor.x + "|" + value.Anchor.y + "|" + value.Anchor.z)
                .ToArray();

            Assert.That(stablePositions.Distinct().Count(), Is.EqualTo(20));
            Assert.That(plan.Slots.All(value => value.Anchor.magnitude < BattleUnionDeploymentPlanning020.ArenaRadius), Is.True);

            for (var first = 0; first < plan.Slots.Count; first++)
            for (var second = first + 1; second < plan.Slots.Count; second++)
            {
                Assert.That(
                    Vector3.Distance(plan.Slots[first].Anchor, plan.Slots[second].Anchor),
                    Is.GreaterThanOrEqualTo(BattleUnionDeploymentPlanning020.RowSpacing - 0.0001f));
            }
        }

        [Test]
        public void OverflowIsCountedWhileFirstTenPerSideRemainDeterministic()
        {
            var plan = BattleUnionDeploymentPlanning020.Plan(Battle(12, 13));

            Assert.That(plan.Slots.Count(value => !value.Enemy), Is.EqualTo(10));
            Assert.That(plan.Slots.Count(value => value.Enemy), Is.EqualTo(10));
            Assert.That(plan.PlayerOverflowCount, Is.EqualTo(2));
            Assert.That(plan.EnemyOverflowCount, Is.EqualTo(3));
            Assert.That(plan.Slots.Count + plan.PlayerOverflowCount + plan.EnemyOverflowCount, Is.EqualTo(25));
            Assert.That(plan.Slots.Select(value => value.UnionId), Does.Not.Contain("P11"));
            Assert.That(plan.Slots.Select(value => value.UnionId), Does.Not.Contain("E12"));
        }

        [Test]
        public void PlanIsImmutableSnapshotAndDoesNotMutateBattleView()
        {
            var player = Union("P00", "Player 1", "Player");
            var enemy = Union("E00", "Enemy 1", "Enemy");
            var battle = new M2BattleView
            {
                PlayerUnions = new[] { player },
                EnemyUnions = new[] { enemy }
            };

            var plan = BattleUnionDeploymentPlanning020.Plan(battle);

            Assert.That(player.UnionId, Is.EqualTo("P00"));
            Assert.That(player.DisplayName, Is.EqualTo("Player 1"));
            Assert.That(player.Side, Is.EqualTo("Player"));
            Assert.That(enemy.UnionId, Is.EqualTo("E00"));
            Assert.That(enemy.DisplayName, Is.EqualTo("Enemy 1"));
            Assert.That(enemy.Side, Is.EqualTo("Enemy"));

            player.UnionId = "MUTATED_AFTER_PLAN";
            player.DisplayName = "Mutated";
            Assert.That(plan.Slots[0].UnionId, Is.EqualTo("P00"));
            Assert.That(plan.Slots[0].DisplayName, Is.EqualTo("Player 1"));

            var mutableInterface = (IList<BattleUnionDeploymentSlot020>)plan.Slots;
            Assert.Throws<NotSupportedException>(() => mutableInterface.RemoveAt(0));
            Assert.That(typeof(BattleUnionDeploymentSlot020).GetProperties().All(value => !value.CanWrite), Is.True);
            Assert.That(typeof(BattleUnionDeploymentPlan020).GetProperties().All(value => !value.CanWrite), Is.True);
            Assert.That(typeof(BattleUnionDeploymentPlanning020).Namespace, Is.EqualTo("SecondDimension.Presentation"));
        }

        [Test]
        public void SameBattleProducesIdenticalDescriptorsAndExactLookup()
        {
            var battle = Battle(10, 10);
            var first = BattleUnionDeploymentPlanning020.Plan(battle);
            var second = BattleUnionDeploymentPlanning020.Plan(battle);

            Assert.That(
                second.Slots.Select(value => value.StableDescriptor),
                Is.EqualTo(first.Slots.Select(value => value.StableDescriptor)));
            Assert.That(first.TryGetSlot("P07", out var slot), Is.True);
            Assert.That(slot.UnionId, Is.EqualTo("P07"));
            Assert.That(slot.Enemy, Is.False);
            Assert.That(first.TryGetSlot("UNKNOWN", out _), Is.False);
            Assert.That(first.TryGetSlot(string.Empty, out _), Is.False);
        }

        [Test]
        public void OpposingSlotsCreateBoundsAdjustableSymmetricEngagementPocket()
        {
            var plan = BattleUnionDeploymentPlanning020.Plan(Battle(5, 5));
            Assert.That(plan.TryGetSlot("P00", out var player), Is.True);
            Assert.That(plan.TryGetSlot("E04", out var enemy), Is.True);

            Assert.That(
                BattleUnionDeploymentPlanning020.TryCreateEngagementPocket(player, enemy, out var pocket),
                Is.True);
            Assert.That(pocket.PlayerUnionId, Is.EqualTo("P00"));
            Assert.That(pocket.EnemyUnionId, Is.EqualTo("E04"));
            Assert.That(pocket.Center, Is.EqualTo(Vector3.zero));
            Assert.That(pocket.PlayerContactAnchor.x, Is.EqualTo(-pocket.EnemyContactAnchor.x).Within(0.0001f));
            Assert.That(pocket.PlayerContactAnchor.z, Is.EqualTo(pocket.EnemyContactAnchor.z).Within(0.0001f));
            Assert.That(pocket.PlayerContactAnchor.magnitude, Is.LessThan(BattleUnionDeploymentPlanning020.ArenaRadius));
            Assert.That(pocket.EnemyContactAnchor.magnitude, Is.LessThan(BattleUnionDeploymentPlanning020.ArenaRadius));

            Assert.That(
                BattleUnionDeploymentPlanning020.TryCreateEngagementPocket(
                    player, enemy, 100f, out var largePocket),
                Is.True);
            Assert.That(
                largePocket.HalfSeparation,
                Is.EqualTo(BattleUnionDeploymentPlanning020.MaximumEngagementHalfSeparation));
            Assert.That(
                BattleUnionDeploymentPlanning020.TryCreateEngagementPocket(player, player, out _),
                Is.False);
        }

        [Test]
        public void NullBattleReturnsEmptyReadOnlyPlan()
        {
            var plan = BattleUnionDeploymentPlanning020.Plan(null);

            Assert.That(plan.Slots, Is.Empty);
            Assert.That(plan.PlayerOverflowCount, Is.Zero);
            Assert.That(plan.EnemyOverflowCount, Is.Zero);
        }

        private static M2BattleView Battle(int playerCount, int enemyCount) => new M2BattleView
        {
            PlayerUnions = Enumerable.Range(0, playerCount)
                .Select(index => Union("P" + index.ToString("00"), "Player " + (index + 1), "Player"))
                .ToArray(),
            EnemyUnions = Enumerable.Range(0, enemyCount)
                .Select(index => Union("E" + index.ToString("00"), "Enemy " + (index + 1), "Enemy"))
                .ToArray()
        };

        private static M2BattleUnionView Union(string id, string name, string side) =>
            new M2BattleUnionView
            {
                UnionId = id,
                DisplayName = name,
                Side = side,
                CanAct = true
            };
    }
}
