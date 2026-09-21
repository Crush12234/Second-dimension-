using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleUnionTurnPlanning019Tests
    {
        [Test]
        public void TwoCommittedPlayerUnionsBecomeTwoDistinctOrderedTurns()
        {
            var phases = BattleUnionTurnPlanner019.Plan(Battle(), Events());

            Assert.That(phases.Count, Is.EqualTo(3));
            Assert.That(phases[0].UnionId, Is.EqualTo("UNION_A"));
            Assert.That(phases[0].TurnBadge, Is.EqualTo("UNION 1 OF 2"));
            Assert.That(phases[0].TurnTitle, Is.EqualTo("FIRST UNION'S TURN"));
            Assert.That(phases[0].OrderLine, Is.EqualTo("ORDER · ATTACK!"));
            Assert.That(phases[0].FirstEventIndex, Is.EqualTo(0));
            Assert.That(phases[1].UnionId, Is.EqualTo("UNION_B"));
            Assert.That(phases[1].TurnBadge, Is.EqualTo("UNION 2 OF 2"));
            Assert.That(phases[1].TurnTitle, Is.EqualTo("SECOND UNION'S TURN"));
            Assert.That(phases[1].OrderLine, Is.EqualTo("ORDER · USE MYSTIC ARTS!"));
            Assert.That(phases[1].FirstEventIndex, Is.EqualTo(4));
        }

        [Test]
        public void RawEnemyActorOpensOneEnemyResponseEvenWhenInterceptionSwapsVisualActors()
        {
            var phases = BattleUnionTurnPlanner019.Plan(Battle(), Events());
            var enemy = phases.Single(value => !value.IsPlayer);

            Assert.That(enemy.UnionId, Is.EqualTo("ENEMY_A"));
            Assert.That(enemy.FirstEventIndex, Is.EqualTo(7));
            Assert.That(enemy.TurnBadge, Is.EqualTo("ENEMY RESPONSE"));
            Assert.That(enemy.TurnTitle, Is.EqualTo("GATE GNAWER PACK ATTACKS"));
        }

        [Test]
        public void GrowthBreakthroughResultAndCaptionTextCannotInventOrReopenUnionTurns()
        {
            var events = Events().Concat(new[]
            {
                new M2BattleEventView
                {
                    Sequence = 8,
                    EventType = "OPTIONAL_FUTURE_EVENT",
                    Text = "SECOND UNION'S TURN and ENEMY RESPONSE are only caption words.",
                    ActorUnionId = "UNION_A"
                },
                new M2BattleEventView
                {
                    Sequence = 9,
                    EventType = "ART_GROWTH",
                    Text = "Late growth.",
                    ActorUnionId = "UNION_B"
                }
            }).ToArray();

            var phases = BattleUnionTurnPlanner019.Plan(Battle(), events);
            Assert.That(phases.Count, Is.EqualTo(3));
            Assert.That(phases.Count(value => value.IsPlayer), Is.EqualTo(2));
        }

        [Test]
        public void SameBattleAndEventsProduceIdenticalStableDescriptors()
        {
            var first = BattleUnionTurnPlanner019.Plan(Battle(), Events())
                .Select(value => value.StableDescriptor).ToArray();
            var second = BattleUnionTurnPlanner019.Plan(Battle(), Events())
                .Select(value => value.StableDescriptor).ToArray();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Select(value => value.Split('|')[0]).ToArray(), Is.EqualTo(new[] { "0", "4", "7" }));
        }

        private static M2BattleView Battle()
        {
            var first = Union("UNION_A", "First Union", "Shield Wall", "Deadlock", "FORECAST_A");
            var second = Union("UNION_B", "Second Union", "Arrow of Athlum", "Side Strike", "FORECAST_B");
            return new M2BattleView
            {
                PlayerUnions = new[] { first, second },
                EnemyUnions = new[] { Union("ENEMY_A", "Gate Gnawer Pack", "Pack Rush", "Engaged", string.Empty) },
                Forecasts = new[]
                {
                    new M2ForecastView
                    {
                        ForecastId = "FORECAST_A", UnionId = "UNION_A", CommandId = "CMD_BALANCED",
                        CommandName = "Attack!", IsSelected = true
                    },
                    new M2ForecastView
                    {
                        ForecastId = "FORECAST_B", UnionId = "UNION_B", CommandId = "CMD_MYSTIC",
                        CommandName = "Use Mystic Arts!", IsSelected = true
                    }
                }
            };
        }

        private static M2BattleUnionView Union(
            string id,
            string name,
            string formation,
            string engagement,
            string selectedForecastId) => new M2BattleUnionView
        {
            UnionId = id,
            DisplayName = name,
            Side = id.StartsWith("ENEMY") ? "Enemy" : "Player",
            Formation = formation,
            Engagement = engagement,
            SelectedForecastId = selectedForecastId,
            CanAct = true
        };

        private static M2BattleEventView[] Events() => new[]
        {
            new M2BattleEventView
            {
                Sequence = 0, EventType = "FORECAST_COMMITTED", ActorUnionId = "UNION_A",
                UnionId = "UNION_A", ArtId = "CMD_BALANCED", Text = "First order."
            },
            new M2BattleEventView
            {
                Sequence = 1, EventType = "MARTIAL_HIT", ActorUnionId = "UNION_A",
                TargetUnionId = "ENEMY_A", Text = "First Union attacks."
            },
            new M2BattleEventView
            {
                Sequence = 2, EventType = "ART_GROWTH", ActorUnionId = "UNION_A", Text = "Growth."
            },
            new M2BattleEventView
            {
                Sequence = 3, EventType = "BREAKTHROUGH", ActorUnionId = "UNION_A",
                ArtId = "ART_POWER_CUT", Text = "A learned Art is deferred without reopening the turn."
            },
            new M2BattleEventView
            {
                Sequence = 4, EventType = "FORECAST_COMMITTED", ActorUnionId = "UNION_B",
                UnionId = "UNION_B", ArtId = "CMD_MYSTIC", Text = "Second order."
            },
            new M2BattleEventView
            {
                Sequence = 5, EventType = "POSITION_SHIFT", ActorUnionId = "UNION_B",
                TargetUnionId = "ENEMY_A", ArtId = "CMD_FLANK",
                Text = "Authoritative Side Strike opens the Blind Side."
            },
            new M2BattleEventView
            {
                Sequence = 6, EventType = "MYSTIC_HIT", ActorUnionId = "UNION_B",
                TargetUnionId = "ENEMY_A", Text = "Second Union attacks."
            },
            new M2BattleEventView
            {
                Sequence = 7, EventType = "INTERCEPTION", ActorUnionId = "ENEMY_A",
                ActorMemberId = "ENEMY_MEMBER", TargetUnionId = "UNION_A",
                TargetMemberId = "PLAYER_GUARD", Text = "The guard intercepts the enemy."
            },
            new M2BattleEventView
            {
                Sequence = 8, EventType = "ART_GROWTH", ActorUnionId = "UNION_A", Text = "Deferred growth."
            },
            new M2BattleEventView
            {
                Sequence = 9, EventType = "BATTLE_RESULT", Text = "Resolved."
            }
        };
    }
}
