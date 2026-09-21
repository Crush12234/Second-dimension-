using System;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2ChapterTwoBattlePresentation079Tests
    {
        private const string FogBattleId =
            "BATTLE_CONTRACT_LINES_NOT_RETURNED_ENCOUNTER_FOG_STALKERS_STANDARD_079";
        private const string RescueBattleId =
            "BATTLE_CONTRACT_LINES_NOT_RETURNED_ENCOUNTER_SURVEYOR_RESCUE_079";

        [TestCase(
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/WAYGLASS_UNDERCROFT_BATTLE_PLATE_079.png",
            false)]
        [TestCase(
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/WAYGLASS_DOOR_RESCUE_ARENA_080.png",
            false)]
        [TestCase(
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/ECHO_STALKER_SCOUT_079.png",
            true)]
        [TestCase(
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/ECHO_STALKER_VEILWARDEN_079.png",
            true)]
        [TestCase(
            "Assets/Resources/SecondDimension/Art/Battle/ChapterTwo079/CHAINCALLER_LEADER_079.png",
            true)]
        public void ChapterTwoBattleProductionResourcesImportCleanly(
            string assetPath,
            bool requiresAlpha)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Assert.That(texture, Is.Not.Null, assetPath + " must import as a Texture2D.");
            Assert.That(texture.width, Is.GreaterThan(64));
            Assert.That(texture.height, Is.GreaterThan(64));
            if (!requiresAlpha) return;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True,
                assetPath + " must retain the production cutout alpha channel.");
        }

        [Test]
        public void FogStalkerBattleUsesWayglassBackdropInsteadOfGatehouse()
        {
            Assert.That(M1VisualAssets.TryResolveBattleBackdrop(
                FogBattleId, out var sprite, out var resourceKey), Is.True);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(resourceKey, Is.EqualTo(
                M1VisualAssets.WayglassUndercroftBattlePlateResourceKey079));
            Assert.That(resourceKey, Does.Not.Contain("GATEHOUSE").IgnoreCase);
        }

        [Test]
        public void SurveyorRescueClimaxKeepsTheWayglassArenaAndAuthoredStakes()
        {
            Assert.That(M1VisualAssets.TryResolveBattleBackdrop(
                RescueBattleId, out var sprite, out var resourceKey), Is.True);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(resourceKey, Is.EqualTo(
                M1VisualAssets.WayglassDoorRescueArenaResourceKey080));
            Assert.That(resourceKey, Does.Not.Contain("GATEHOUSE").IgnoreCase);

            var battle = new M2BattleView
            {
                BattleId = RescueBattleId,
                Objective = "Follow the Wayglass route, find the missing survey crew, and locate the door inside Skyhome."
            };
            Assert.That(M2BattleExperienceController072.EncounterTitle076(battle),
                Is.EqualTo("THE LAST FALSE LINE"));
            Assert.That(M2BattleExperienceController072.EncounterStakes076(battle),
                Is.EqualTo("Free Orra's survey crew and hold the unrecorded door."));
            Assert.That(M2BattleResultsView072.IsSurveyorRescueEncounter079(battle), Is.True);
            Assert.That(M2BattleResultsView072.IsFogStalkerEncounter079(battle), Is.False,
                "The rescue climax must not inherit the earlier Fog-Stalker payoff.");
        }

        [Test]
        public void CanonicalFogStalkerMembersResolveThreeDistinctProductionStandees()
        {
            var ids = new[]
            {
                "ENEMY_ECHO_STALKER_01_SPAWN070_EU_ECHO_AMBUSH_0",
                "ENEMY_ECHO_STALKER_02_SPAWN070_EU_ECHO_AMBUSH_1",
                "ENEMY_CHAINCALLER_01_SPAWN070_EU_ECHO_AMBUSH_2"
            };
            var expected = new[]
            {
                M1VisualAssets.EchoStalkerScoutStandeeResourceKey079,
                M1VisualAssets.EchoStalkerVeilwardenStandeeResourceKey079,
                M1VisualAssets.ChaincallerLeaderStandeeResourceKey079
            };
            var sprites = new Sprite[ids.Length];
            for (var index = 0; index < ids.Length; index++)
            {
                Assert.That(M1VisualAssets.TryResolveChapterTwoEnemyBattleStandee079(
                    ids[index], out sprites[index], out var resourceKey), Is.True, ids[index]);
                Assert.That(resourceKey, Is.EqualTo(expected[index]));
                Assert.That(resourceKey, Does.Not.Contain("GATE_GNAWER").IgnoreCase);
                Assert.That(resourceKey, Does.Not.Contain("Battle075").IgnoreCase);
            }

            Assert.That(sprites[0], Is.Not.SameAs(sprites[1]));
            Assert.That(sprites[0], Is.Not.SameAs(sprites[2]));
            Assert.That(sprites[1], Is.Not.SameAs(sprites[2]));
            Assert.That(M1VisualAssets.TryResolveChapterTwoEnemyBattleStandee079(
                "ENEMY_GATE_GNAWER_01", out _, out _), Is.False,
                "The exact Chapter Two resolver must never accept Gate-Gnawer recovery art.");
        }

        [Test]
        public void GateEaterIsInitialNarrativeTargetButSelectedForecastStillOverrides()
        {
            var battle = GateEaterBattle();
            Assert.That(M2BattleDioramaView072.PreferredNarrativeTargetUnionId079(
                battle, "PU_01"), Is.EqualTo("EU_GATE_EATER"));

            battle.Forecasts = new[]
            {
                new M2ForecastView
                {
                    ForecastId = "F_SELECTED",
                    UnionId = "PU_01",
                    TargetId = "EU_ESCORT",
                    IsSelected = true
                }
            };
            Assert.That(M2BattleDioramaView072.PreferredNarrativeTargetUnionId079(
                battle, "PU_01"), Is.EqualTo("EU_ESCORT"));
        }

        [Test]
        public void FogStalkerOutcomeRecapIsEncounterSpecificAndNeverEllipsized()
        {
            var battle = new M2BattleView
            {
                BattleId = FogBattleId,
                Objective = "Keep Sella alive and hold Orra's true Wayglass line against the Fog Stalkers.",
                Outcome = "Victory",
                Round = 4,
                LastResolvedRound = 3
            };
            var summary = M2BattleResultsView072.BuildBattleSummary079(battle, true);
            Assert.That(summary,
                Does.Contain("FOG-STALKERS DOWN")
                    .And.Contain("ORRA'S LINE HOLDS")
                    .And.Contain("WON IN 3 ROUNDS"));
            Assert.That(summary, Does.Not.Contain("...").And.Not.Contain("…"));
            Assert.That(summary.Split('\n').Length, Is.EqualTo(3));
        }

        [Test]
        public void SurveyorRescueOutcomeNamesThePeopleAndDoorWithoutEllipses()
        {
            var battle = new M2BattleView
            {
                BattleId = RescueBattleId,
                Objective = "Follow the Wayglass route, find the missing survey crew, and locate the door inside Skyhome.",
                Outcome = "Victory",
                Round = 5,
                LastResolvedRound = 4
            };
            var summary = M2BattleResultsView072.BuildBattleSummary079(battle, true);
            Assert.That(summary,
                Does.Contain("ORRA'S CREW IS FREE")
                    .And.Contain("THE DOOR IS SECURED")
                    .And.Contain("WON IN 4 ROUNDS"));
            Assert.That(summary, Does.Not.Contain("...").And.Not.Contain("…"));
            Assert.That(summary.Split('\n').Length, Is.EqualTo(3));
        }

        private static M2BattleView GateEaterBattle() => new M2BattleView
        {
            BattleId = "BATTLE_FIRST_HOUR_ENCOUNTER071_GATE_EATER_079",
            Objective = "Defeat the Gate-Eater and protect the road home.",
            PlayerUnions = new[]
            {
                Union("PU_01", "PLAYER_01")
            },
            EnemyUnions = new[]
            {
                Union("EU_ESCORT", "ENEMY_GATE_GNAWER_01"),
                Union("EU_GATE_EATER", "HINGE_EATER_COLOSSUS")
            },
            Forecasts = Array.Empty<M2ForecastView>()
        };

        private static M2BattleUnionView Union(string unionId, string memberId) =>
            new M2BattleUnionView
            {
                UnionId = unionId,
                CanAct = true,
                Members = new[]
                {
                    new M2BattleMemberView
                    {
                        MemberId = memberId,
                        DisplayName = memberId,
                        CurrentHp = 100,
                        MaximumHp = 100
                    }
                }
            };
    }
}
