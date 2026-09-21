using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation;
using SecondDimension.Presentation.BoardTower001;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017E;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardTowerEnhancement001Tests
    {
        [Test]
        public void VerifiedRuntimeCatalogLoadsEveryPackRecord001()
        {
            var catalog = BoardTowerEnhancementCatalog001.LoadFromResources();

            Assert.That(catalog.Counts.packageId,
                Is.EqualTo(BoardTowerEnhancementRules001.PackageId001));
            Assert.That(catalog.Items.Count, Is.EqualTo(144));
            Assert.That(catalog.Rooms.Count, Is.EqualTo(72));
            Assert.That(catalog.Effects.Count, Is.EqualTo(72));
            Assert.That(catalog.TurningPoints.Count, Is.EqualTo(24));
            Assert.That(catalog.Hooks.Count, Is.EqualTo(30));
            Assert.That(catalog.P0.Count, Is.EqualTo(76));
            Assert.That(catalog.P0.Count(value => value.contentType == "ITEM"), Is.EqualTo(36));
            Assert.That(catalog.P0.Count(value => value.contentType == "ROOM"), Is.EqualTo(18));
            Assert.That(catalog.P0.Count(value => value.contentType == "RUN_EFFECT"), Is.EqualTo(14));
            Assert.That(catalog.P0.Count(value => value.contentType == "TURNING_POINT"), Is.EqualTo(8));
        }

        [TestCase("MONSTER", "BTR001_BATTLE_")]
        [TestCase("TREASURE", "BTR001_CHEST_")]
        [TestCase("SKILL", "BTR001_BOON_")]
        [TestCase("BLESSING", "BTR001_BOON_")]
        [TestCase("FATE", "BTR001_HAZARD_")]
        [TestCase("CAMPFIRE", "BTR001_CAMP_")]
        [TestCase("STORY", "BTR001_STORY_")]
        [TestCase("DISCOVERY", "BTR001_RESOURCE_")]
        [TestCase("TOWER", "BTR001_TOWER_")]
        public void P0RoomSelectionIsStableAndSemanticallyCompatible001(
            string kind,
            string expectedPrefix)
        {
            var first = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                "EXPEDITION_COMMITTED_001", "N07", kind);
            var second = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                "EXPEDITION_COMMITTED_001", "N07", kind);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.StartWith(expectedPrefix));
            Assert.That(BoardTowerEnhancementCatalog001.LoadFromResources().Rooms.ContainsKey(first),
                Is.True);
        }

        [Test]
        public void RoomIdentityAppearsOnlyAfterItsExactCommitProof001()
        {
            const string expeditionId = "EXP_BOARD_TOWER_PROOF_001";
            const string nodeId = "N05";
            const string kind = "TREASURE";
            var catalog = BoardTowerEnhancementCatalog001.LoadFromResources();
            var roomId = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                expeditionId, nodeId, kind);
            var proof = BoardTowerEnhancementRules001.SelectedRoomFlag001(nodeId, roomId);

            Assert.That(catalog.CommittedBoardRoom001(
                expeditionId, nodeId, kind, Array.Empty<string>()), Is.Null);
            Assert.That(catalog.CommittedBoardRoom001(
                    expeditionId, nodeId, kind, new[] { proof })?.roomId,
                Is.EqualTo(roomId));
            Assert.That(BoardTowerEnhancementRules001.IsRoomModuleCommitted001(
                new[] { proof }, nodeId, roomId), Is.True);
            Assert.That(BoardTowerEnhancementRules001.IsRoomModuleCommitted001(
                new[] { proof }, "N06", roomId), Is.False);
        }

        [Test]
        public void AuthoredStoryAndFateNeverShowGenericPackIdentity001()
        {
            Assert.That(BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001("STORY"), Is.False);
            Assert.That(BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001("FATE"), Is.False);
            Assert.That(BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001("MONSTER"), Is.True);
            Assert.That(BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001("TREASURE"), Is.True);
        }

        [Test]
        public void TowerRoomIsDerivedOnlyFromAnAlreadyCommittedRun001()
        {
            var catalog = BoardTowerEnhancementCatalog001.LoadFromResources();

            Assert.That(catalog.DeterministicTowerRoom001(string.Empty, 1), Is.Null);
            var first = catalog.DeterministicTowerRoom001("ABYSSRUN022_COMMITTED", 5);
            var reload = catalog.DeterministicTowerRoom001("ABYSSRUN022_COMMITTED", 5);
            Assert.That(first, Is.Not.Null);
            Assert.That(reload.roomId, Is.EqualTo(first.roomId));
            Assert.That(first.roomId, Does.StartWith("BTR001_TOWER_"));
        }

        [Test]
        public void RevealedTowerRoomNarrativeIsProofGatedStableAndMechanicsNeutral001()
        {
            const string runId = "ABYSSRUN022_COMMITTED_NARRATIVE_001";
            var catalog = BoardTowerEnhancementCatalog001.LoadFromResources();

            Assert.That(TowerRevealedRoomProjection001.Project(
                catalog, runId, 1, Array.Empty<string>()), Is.Null,
                "No applied step proof means no room identity may be revealed.");

            var first = TowerRevealedRoomProjection001.Project(
                catalog, runId, 1, new[] {"ABYSS_STEP_00_APPLIED"});
            var reload = TowerRevealedRoomProjection001.Project(
                catalog, runId, 1, new[] {"ABYSS_STEP_00_APPLIED"});
            var second = TowerRevealedRoomProjection001.Project(
                catalog, runId, 1,
                new[] {"ABYSS_STEP_00_APPLIED", "ABYSS_STEP_01_APPLIED"});

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(reload.RoomId, Is.EqualTo(first.RoomId));
            Assert.That(reload.Title, Is.EqualTo(first.Title));
            Assert.That(first.StepNumber, Is.EqualTo(1));
            Assert.That(second.StepNumber, Is.EqualTo(2));
            Assert.That(second.RoomId, Is.Not.EqualTo(first.RoomId),
                "Adjacent saved reveals should not repeat the same P0 landmark.");
            foreach (var view in new[] {first, second})
            {
                var playerCopy = (view.Title + " " + view.Flavor).ToUpperInvariant();
                Assert.That(playerCopy, Does.Not.Contain("BOON"));
                Assert.That(playerCopy, Does.Not.Contain("CHOICE"));
                Assert.That(playerCopy, Does.Not.Contain("MODIFIER"));
                Assert.That(playerCopy, Does.Not.Contain("ENHANCED REWARD"));
                Assert.That(playerCopy, Does.Not.Contain("BTR001_"));
            }

            var state = new CampaignProgressionPresentationState022 {IsAvailable = true};
            M1RuntimeCoordinator.ApplyOptionalTowerRoomModule001(
                state, runId, 1, Array.Empty<string>(), () => catalog);
            Assert.That(state.TowerRoomModuleDisplayName001, Is.Empty);
            M1RuntimeCoordinator.ApplyOptionalTowerRoomModule001(
                state, runId, 1, new[] {"ABYSS_STEP_00_APPLIED"}, () => catalog);
            Assert.That(state.TowerRoomModuleId001, Is.EqualTo(first.RoomId));
            Assert.That(state.TowerRoomModuleDisplayName001, Is.EqualTo(first.Title));
            Assert.That(state.TowerRoomModuleNarrative001, Is.EqualTo(first.Flavor));
            Assert.That(state.TowerRoomModuleRevealedStepNumber001, Is.EqualTo(1));
        }

        [Test]
        public void InvalidOptionalCatalogLeavesCoreTowerPresentationAvailable001()
        {
            var core = new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                TowerFloorId = "ABYSS_FLOOR022_01",
                TowerFloorDisplayName = "Mud Trenches",
                TowerOperationId = "ABYSS_OP022_01_GUARDIAN",
                TowerFloorNumber = 1,
                TowerExpectedEnemyUnions = 1
            };

            M1RuntimeCoordinator.ApplyOptionalTowerRoomModule001(
                core,
                "ABYSSRUN022_COMMITTED",
                1,
                () => throw new InvalidOperationException("synthetic optional-pack failure"));

            Assert.That(core.IsAvailable, Is.True);
            Assert.That(core.TowerFloorId, Is.EqualTo("ABYSS_FLOOR022_01"));
            Assert.That(core.TowerOperationId, Is.EqualTo("ABYSS_OP022_01_GUARDIAN"));
            Assert.That(core.TowerExpectedEnemyUnions, Is.EqualTo(1));
            Assert.That(core.TowerRoomModuleId001, Is.Empty);
            Assert.That(core.TowerRoomModuleDisplayName001, Is.Empty);
        }

        [Test]
        public void EveryPresentedQuestRoomHasAnExactPreCommitRouteForecast001()
        {
            var nodes = GuildCityOpeningExperienceRegistry017E.Load().nodes ??
                        Array.Empty<GuildCityNodePresentation017E>();
            Assert.That(nodes, Has.Length.EqualTo(45));
            foreach (var node in nodes)
            {
                var forecast = ExpeditionBoardProjection074.RouteCostForecast076(
                    node.routeCostSummary);
                Assert.That(forecast, Does.Not.Contain("SHOWN ON ARRIVAL"),
                    node.boardId + " / " + node.nodeId);
                Assert.That(forecast, Does.Contain("SUPPLIES"));
                Assert.That(forecast, Does.Contain("FATIGUE"));
                Assert.That(forecast, Does.Contain("PRESSURE"));
            }
        }
    }
}
