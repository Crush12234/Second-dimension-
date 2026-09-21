using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BoardAdventure084PlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDownBoardAdventure084()
        {
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<M1FlowPresenter>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (presenter.name.StartsWith("Board Adventure PlayMode Presenter 084",
                        StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(presenter.gameObject);
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.name.StartsWith("Board Adventure PlayMode Canvas 084",
                        StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignWorldGateAndTowerKeepPrimaryMoveVisibleAtBothResolutions()
        {
            Assert.That(M1FlowPresenter.BoardAdventureRevealDuration084,
                Is.EqualTo(0.18f));
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            {
                yield return VerifyCampaign020(resolution);
                yield return VerifyWorldGate023(resolution);
                yield return VerifyTower022(resolution);
            }
        }

        [UnityTest]
        public IEnumerator PackagedTowerLobbyTitleLookupSurvivesResponsiveRename084()
        {
            var harness = CreateHarness(new Vector2(1920f, 1080f),
                "TowerLobbyResponsiveTitle");
            var state = new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                TowerFloorNumber = 1,
                TowerFloorId = "ABYSS_FLOOR_001_MUD_TRENCHES",
                TowerFloorDisplayName = "Mud Trenches",
                TowerOperationId = "ABYSS_OP022_01_GUARDIAN",
                TowerExpectedEnemyUnions = 1,
                TowerRewardSummary = "+25 GUILD XP  •  +19 HALL XP",
                TowerArtResourcePath = TowerRunRules081.ArtResourcePath(1),
                FloorStates = new[]
                {
                    new AbyssFloorView022
                    {
                        FloorId = "ABYSS_FLOOR_001_MUD_TRENCHES",
                        DisplayName = "Mud Trenches",
                        FloorNumber = 1,
                        IsUnlocked = true,
                        IsCurrentGoal = true,
                        Status = "NEXT FIGHT"
                    }
                }
            };
            InvokeBuild(harness.Presenter, "BuildGuildCityAbyss022", harness.Body,
                new TowerCoordinator084(state), state);
            Rebuild(harness.Body);

            var hero = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Tower Current Floor Hero 085");
            var title = hero.GetComponentsInChildren<Text>(true)
                .Single(value => value != null && value.name.StartsWith(
                    "Tower Floor Hero Title 085", StringComparison.Ordinal));
            Assert.That(title.name, Does.StartWith("Tower Floor Hero Title 085"),
                "Responsive typography must retain the stable Tower hero title prefix.");
            var statusBand = hero.GetComponentsInChildren<Image>(true)
                .Single(value => value.name == "Tower Floor Status Band 085");
            Assert.That(TowerRunRules081.ContrastRatio083(
                    title.color, statusBand.color),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            var floorArt = harness.Body.GetComponentsInChildren<Image>(true)
                .Single(value => value.name == "Tower Current Floor Art 081");
            Assert.That(floorArt.sprite, Is.Not.Null);
            Assert.That(floorArt.sprite.texture.name,
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtTextureName));
            Assert.That(floorArt.transform.parent.name,
                Is.EqualTo("Tower Current Floor Hero 085"));
            Assert.That(floorArt.GetComponent<AspectRatioFitter>(), Is.Not.Null);
            Assert.That(floorArt.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            Assert.That(floorArt.GetComponentInParent<Mask>(), Is.Not.Null,
                "The cover-fit Tower art must remain clipped by its named hero frame.");
            Assert.That(Buttons(harness.Body).Single(value =>
                value.name == "Climb next Tower floor 084").IsInteractable(), Is.True);

            UnityEngine.Object.Destroy(harness.Presenter.gameObject);
            UnityEngine.Object.Destroy(harness.Canvas.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WorldGatePendingDiceBattleAndReturnTilesStayExplicitAndVisible()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            {
                var cases = new[]
                {
                    new
                    {
                        State = WorldGateState084("Active", false, false),
                        Button = string.Empty,
                        Copy = "DICE SETTLED"
                    },
                    new
                    {
                        State = WorldGateState084("Active", true, false),
                        Button = "Flip monster battle tile 084",
                        Copy = "ENTER UNION BATTLE"
                    },
                    new
                    {
                        State = WorldGateState084("ReadyToFinalize", false, true),
                        Button = "Return from completed quest board 084",
                        Copy = "RETURN TO THE GUILD"
                    }
                };
                foreach (var item in cases)
                {
                    var harness = CreateHarness(resolution, "WorldGateState");
                    InvokeBuild(harness.Presenter, "BuildGuildCityWorldGate023",
                        harness.Body, new WorldGateCoordinator084(item.State), item.State);
                    Rebuild(harness.Body);
                    Assert.That(VisibleText(harness.Body), Does.Contain(item.Copy));
                    if (string.IsNullOrWhiteSpace(item.Button))
                    {
                        Assert.That(Buttons(harness.Body), Is.Empty,
                            "Saved room receipts auto-apply after their physical reveal.");
                        Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                                .Any(value => value.name ==
                                    "Face Down Room Card Back 084"),
                            Is.True);
                    }
                    else
                    {
                        var primary = Buttons(harness.Body).Single(value =>
                            value.name == item.Button);
                        AssertInside(harness.Viewport,
                            primary.GetComponent<RectTransform>(),
                            resolution + " " + item.Button);
                    }
                    UnityEngine.Object.Destroy(harness.Presenter.gameObject);
                    UnityEngine.Object.Destroy(harness.Canvas.gameObject);
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator WorldGateWrongWorldStoryQuestOffersOneReachableTravelAction()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            {
                var harness = CreateHarness(resolution, "WrongWorldStory");
                var state = new CampaignWorldGatePresentationState023
                {
                    IsAvailable = true,
                    CurrentWorldId = "SKYHOME",
                    CurrentWorldName = "Skyhome",
                    TravelSupplies = 5,
                    Boards = new[]
                    {
                        new BoardView023
                        {
                            DefinitionId = "BOARD023_STORY_TARGET_HIDDEN_084",
                            WorldId = "ASHFALL",
                            WorldName = "Ashfall Reach",
                            Title = "The Bell Beyond the Ash",
                            Kind = "CHAPTER",
                            PlayerKind = "STORY QUEST",
                            NodeCount = 10,
                            IsCurrentStoryBoard = true,
                            Available = false
                        }
                    },
                    Standings = new[]
                    {
                        new StandingView023
                        {
                            WorldId = "SKYHOME",
                            DisplayName = "Skyhome",
                            Tier = "HOME",
                            Unlocked = true,
                            CanAffordTravel = true
                        },
                        new StandingView023
                        {
                            WorldId = "ASHFALL",
                            DisplayName = "Ashfall Reach",
                            Tier = "OPEN_ROAD",
                            Unlocked = true,
                            TravelSupplyCost = 2,
                            CanAffordTravel = true
                        }
                    }
                };
                InvokeBuild(harness.Presenter, "BuildGuildCityWorldGate023",
                    harness.Body, new WorldGateCoordinator084(state), state);
                Rebuild(harness.Body);

                var visible = VisibleText(harness.Body);
                Assert.That(visible, Does.Contain("Your story pawn belongs in Ashfall Reach"));
                Assert.That(visible, Does.Not.Contain("NO QUEST READY"));
                Assert.That(visible, Does.Not.Contain("BOARD023_STORY_TARGET_HIDDEN_084"));
                var storyTravel = Buttons(harness.Body).Single(value =>
                    value.name == "Travel to required story board 084");
                Assert.That(storyTravel.IsInteractable(), Is.True);
                AssertInside(harness.Viewport,
                    storyTravel.GetComponent<RectTransform>(),
                    resolution + " wrong-world story travel");
                AssertAutoSizedAncestorContains084(storyTravel,
                    resolution + " wrong-world story travel panel");

                UnityEngine.Object.Destroy(harness.Presenter.gameObject);
                UnityEngine.Object.Destroy(harness.Canvas.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator LegacyBoardRecoveryRoutesStayPlainAndReachableAtBothResolutions()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            {
                var campaignHarness = CreateHarness(resolution, "LegacyCampaignRecovery");
                var campaignState = new CampaignPlayablePresentationState020
                {
                    IsAvailable = true,
                    ActiveOperationId = "C020_LEGACY_HIDDEN_084",
                    LegacyRecoveryRequired = true
                };
                InvokeBuild(campaignHarness.Presenter, "BuildGuildCityCampaign020",
                    campaignHarness.Body, new CampaignCoordinator084(campaignState),
                    campaignState);
                Rebuild(campaignHarness.Body);
                var campaignVisible = VisibleText(campaignHarness.Body);
                Assert.That(campaignVisible, Does.Contain("OLD UNFINISHED QUEST FOUND"));
                Assert.That(campaignVisible, Does.Not.Contain("C020_LEGACY_HIDDEN_084"));
                var campaignRecover = Buttons(campaignHarness.Body).Single(value =>
                    value.name == "Recover legacy campaign quest 084");
                AssertInside(campaignHarness.Viewport,
                    campaignRecover.GetComponent<RectTransform>(),
                    resolution + " legacy Campaign recovery");
                AssertAutoSizedAncestorContains084(campaignRecover,
                    resolution + " legacy Campaign recovery panel");
                UnityEngine.Object.Destroy(campaignHarness.Presenter.gameObject);
                UnityEngine.Object.Destroy(campaignHarness.Canvas.gameObject);
                yield return null;

                var worldHarness = CreateHarness(resolution, "LegacyWorldGateRecovery");
                var worldState = new CampaignWorldGatePresentationState023
                {
                    IsAvailable = true,
                    ActiveOperationId = "C023_LEGACY_HIDDEN_084",
                    LegacyRecoveryRequired = true
                };
                InvokeBuild(worldHarness.Presenter, "BuildGuildCityWorldGate023",
                    worldHarness.Body, new WorldGateCoordinator084(worldState), worldState);
                Rebuild(worldHarness.Body);
                var worldVisible = VisibleText(worldHarness.Body);
                Assert.That(worldVisible, Does.Contain("OLD UNFINISHED ADVENTURE FOUND"));
                Assert.That(worldVisible, Does.Not.Contain("C023_LEGACY_HIDDEN_084"));
                var worldRecover = Buttons(worldHarness.Body).Single(value =>
                    value.name == "Recover legacy World Gate quest 084");
                AssertInside(worldHarness.Viewport,
                    worldRecover.GetComponent<RectTransform>(),
                    resolution + " legacy World Gate recovery");
                AssertAutoSizedAncestorContains084(worldRecover,
                    resolution + " legacy World Gate recovery panel");
                UnityEngine.Object.Destroy(worldHarness.Presenter.gameObject);
                UnityEngine.Object.Destroy(worldHarness.Canvas.gameObject);
                yield return null;

                var towerHarness = CreateHarness(resolution, "LegacyTowerRecovery");
                var towerState = new CampaignProgressionPresentationState022
                {
                    IsAvailable = true,
                    LegacyTowerRecoveryRequired = true
                };
                InvokeBuild(towerHarness.Presenter, "BuildGuildCityAbyss022",
                    towerHarness.Body, new TowerCoordinator084(towerState), towerState);
                Rebuild(towerHarness.Body);
                var towerVisible = VisibleText(towerHarness.Body);
                Assert.That(towerVisible,
                    Does.Contain("OLDER TOWER RECORD READY TO RECOVER"));
                Assert.That(towerVisible, Does.Not.Contain("CAMPAIGN_PROGRESSION"));
                var towerRecover = Buttons(towerHarness.Body).Single(value =>
                    value.name == "Recover inactive legacy Tower 084");
                AssertInside(towerHarness.Viewport,
                    towerRecover.GetComponent<RectTransform>(),
                    resolution + " inactive legacy Tower recovery");
                AssertAutoSizedAncestorContains084(towerRecover,
                    resolution + " inactive legacy Tower recovery panel");
                UnityEngine.Object.Destroy(towerHarness.Presenter.gameObject);
                UnityEngine.Object.Destroy(towerHarness.Canvas.gameObject);
                yield return null;

                var heldHarness = CreateHarness(resolution, "HeldLegacyTower");
                var heldState = new CampaignProgressionPresentationState022
                {
                    IsAvailable = true,
                    ActiveAbyssOperationId = "TOWER_LEGACY_ACTIVE_HIDDEN_084",
                    LegacyTowerRecoveryRequired = true,
                    LegacyTowerRecoveryRequiresSupport = true
                };
                InvokeBuild(heldHarness.Presenter, "BuildGuildCityAbyss022",
                    heldHarness.Body, new TowerCoordinator084(heldState), heldState);
                Rebuild(heldHarness.Body);
                var heldVisible = VisibleText(heldHarness.Body);
                Assert.That(heldVisible, Does.Contain("OLDER TOWER RUN HELD SAFELY"));
                Assert.That(heldVisible, Does.Contain("contact support"));
                Assert.That(heldVisible,
                    Does.Not.Contain("TOWER_LEGACY_ACTIVE_HIDDEN_084"));
                Assert.That(Buttons(heldHarness.Body).Any(value =>
                    value.name == "Recover inactive legacy Tower 084"), Is.False);
                var heldReturn = Buttons(heldHarness.Body).Single(value =>
                    value.name == "Return from held Tower run 084");
                AssertInside(heldHarness.Viewport,
                    heldReturn.GetComponent<RectTransform>(),
                    resolution + " held legacy Tower return");
                UnityEngine.Object.Destroy(heldHarness.Presenter.gameObject);
                UnityEngine.Object.Destroy(heldHarness.Canvas.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator WorldGateAuthoredRoomsAreScrollSafeAndAdvanceOneSpace()
        {
            var board = AuthoredBoard084();
            var before = AdventureBoardTrackProjection084.Project(
                board, "ROOM_01", new[] {"ROOM_00"});
            var after = AdventureBoardTrackProjection084.Project(
                board, "ROOM_03", new[] {"ROOM_00", "ROOM_01"});
            Assert.That(before.Count, Is.EqualTo(12));
            Assert.That(after.Count, Is.EqualTo(12));
            var beforePawn = before.Single(value =>
                value.State == AdventureBoardTrackProjection084.CurrentState);
            var afterPawn = after.Single(value =>
                value.State == AdventureBoardTrackProjection084.CurrentState);
            Assert.That(afterPawn.SpaceNumber, Is.EqualTo(beforePawn.SpaceNumber + 1),
                "One legal authored edge must move the visible pawn exactly one space.");
            Assert.That(before.Single(value => value.NodeId == "ROOM_02").State,
                Is.EqualTo(AdventureBoardTrackProjection084.ClosedState));

            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            {
                var harness = CreateHarness(resolution, "AuthoredWorldGateBefore");
                var state = AuthoredWorldGateState084(board, before, "ROOM_01");
                InvokeBuild(harness.Presenter, "BuildGuildCityWorldGate023",
                    harness.Body, new WorldGateCoordinator084(state), state);
                Rebuild(harness.Body);

                AssertCompactAuthoredBoardRendered084(harness, 10, 2, resolution);
                var primary = Buttons(harness.Body).First(value =>
                    value.name == "Move forward World Gate room 084");
                AssertInside(harness.Viewport, primary.GetComponent<RectTransform>(),
                    resolution + " authored-board primary move");

                UnityEngine.Object.Destroy(harness.Presenter.gameObject);
                UnityEngine.Object.Destroy(harness.Canvas.gameObject);
                yield return null;

                var afterHarness = CreateHarness(resolution, "AuthoredWorldGateAfter");
                var afterState = AuthoredWorldGateState084(board, after, "ROOM_03");
                InvokeBuild(afterHarness.Presenter, "BuildGuildCityWorldGate023",
                    afterHarness.Body, new WorldGateCoordinator084(afterState), afterState);
                Rebuild(afterHarness.Body);
                AssertCompactAuthoredBoardRendered084(afterHarness, 10, 3, resolution);

                UnityEngine.Object.Destroy(afterHarness.Presenter.gameObject);
                UnityEngine.Object.Destroy(afterHarness.Canvas.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ProjectedCatalogCopyStaysReadableAndScrollSafeAtBothResolutions()
        {
            var boards = CampaignRegistry023.LoadFromResources();
            var chapters = CampaignRegistry019.LoadFromResources();
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            foreach (var definitionId in new[]
                     {
                         "CH018_032", "REPEAT020_SKYHOME_04"
                     })
            {
                var board = boards.Boards[definitionId];
                chapters.Chapters.TryGetValue(definitionId, out var chapter);
                var world = boards.Standing[board.worldId].displayName;
                var node = StringComparer.Ordinal.Equals(definitionId, "CH018_032")
                    ? board.nodes.Single(value => value.nodeId == board.startNodeId)
                    : board.nodes.Where(value => !value.requiresCertifiedBattle)
                        .OrderByDescending(value => (value.choiceIds ?? Array.Empty<string>()).Length)
                        .ThenByDescending(value => value.description?.Length ?? 0)
                        .First();
                var state = ProjectedCatalogState084(board, node, chapter, world);
                var harness = CreateHarness(resolution,
                    "ProjectedCatalog" + definitionId);
                InvokeBuild(harness.Presenter, "BuildGuildCityWorldGate023",
                    harness.Body, new WorldGateCoordinator084(state), state);
                Rebuild(harness.Body);

                var copy = AdventureBoardNarrativeProjection084.Project(
                    board, node, chapter, world);
                var visible = VisibleText(harness.Body);
                Assert.That(visible,
                    Does.Contain((state.ActiveBoardTitle ?? string.Empty).ToUpperInvariant()),
                    definitionId);
                var identifyingGoal = string.Join(" ", (state.BoardObjective ?? string.Empty)
                    .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                    .Take(6));
                Assert.That(visible, Does.Contain("GOAL  •"), definitionId);
                Assert.That(visible, Does.Contain(identifyingGoal), definitionId);

                var compactPanels = harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Where(value => value.name.StartsWith(
                                        "Adventure Compact Room ",
                                        StringComparison.Ordinal) &&
                                    !value.name.StartsWith(
                                        "Adventure Compact Room Copy ",
                                        StringComparison.Ordinal) &&
                                    value.GetComponent<Image>() != null)
                    .ToArray();
                Assert.That(compactPanels, Is.Not.Empty, definitionId);
                Assert.That(compactPanels.Length, Is.InRange(1, 5),
                    definitionId + " must show at most five phone-simple rooms.");
                Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                        .Any(value => value.name.StartsWith(
                            "Adventure Authored Room Tile ", StringComparison.Ordinal)),
                    Is.False,
                    definitionId + " must not render the retired authored-room track.");

                var compactCopies = harness.Body.GetComponentsInChildren<Text>(true)
                    .Where(value => value.name.StartsWith(
                        "Adventure Compact Room Copy ", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(compactCopies.Count(value => value.text.Contains("YOUR PAWN")),
                    Is.EqualTo(1), definitionId + " must show exactly one current pawn.");
                var physicalPawns = harness.Body
                    .GetComponentsInChildren<RectTransform>(true)
                    .Where(value => value.name ==
                        "Board Adventure Guild Pawn 086")
                    .ToArray();
                Assert.That(physicalPawns, Has.Length.EqualTo(1),
                    definitionId + " must render one physical pawn, not only a text bullet.");
                Assert.That(physicalPawns[0].parent.name,
                    Does.StartWith("Adventure Compact Room "),
                    definitionId + " physical pawn must live on the current room card.");
                Assert.That(physicalPawns[0].GetComponentsInChildren<RectTransform>(true)
                        .Any(value => value.name == "Board Adventure Pawn Base 086"),
                    Is.True,
                    definitionId + " pawn needs a visible tabletop base.");
                var faceDownFuture = compactCopies.Where(value =>
                    value.text.Contains("FACE DOWN") &&
                    !value.text.Contains("YOUR PAWN")).ToArray();
                Assert.That(faceDownFuture, Is.Not.Empty,
                    definitionId + " must keep a future room face down.");
                foreach (var future in faceDownFuture)
                {
                    Assert.That(future.text, Does.Not.Contain(copy.Title));
                    Assert.That(future.text, Does.Not.Contain(copy.Description));
                    Assert.That(future.text, Does.Not.Contain(copy.StoryFlavor));
                    Assert.That(future.text, Does.Not.Contain(copy.Objective));
                }

                var progress = harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name == "Adventure Compact Progress Strip 084");
                var playableRoomCount = state.AdventureTiles.Max(value => value.SpaceNumber);
                var playableRoom = state.AdventureTiles.Single(value =>
                    value.State == AdventureBoardTrackProjection084.CurrentState).SpaceNumber;
                Assert.That(visible, Does.Contain(
                        "ROOM  " + playableRoom + " OF " + playableRoomCount + "  •  SUPPLY"),
                    definitionId + " mission copy must use the same playable-space count as its strip.");
                if (state.TotalNodes != playableRoomCount)
                    Assert.That(visible, Does.Not.Contain(
                            "ROOM  " + playableRoom + " OF " + state.TotalNodes + "  •  SUPPLY"),
                        definitionId + " must not expose the authored branch-node count as room count.");
                Assert.That(progress.GetComponent<LayoutElement>().preferredHeight,
                    Is.LessThan(0f));
                AssertInside(harness.Scroll.content, progress,
                    resolution + " " + definitionId + " compact progress strip");
                var primary = Buttons(harness.Body).Single(value =>
                    value.name == "Move forward World Gate room 084");
                var primaryCopy = primary.GetComponentInChildren<Text>();
                Assert.That(primaryCopy, Is.Not.Null);
                Assert.That(primaryCopy.text,
                    Is.EqualTo("MOVE FORWARD  •  FLIP NEXT ROOM"));
                AssertInside(harness.Viewport,
                    primary.GetComponent<RectTransform>(),
                    resolution + " " + definitionId + " first projected action");
                AssertAutoSizedAncestorContains084(primary,
                    resolution + " " + definitionId + " projected action");
                AssertInside(harness.Scroll.content,
                    primary.GetComponent<RectTransform>(),
                    resolution + " " + definitionId + " action in scroll content");
                Assert.That(harness.Scroll.content.rect.height,
                    Is.GreaterThanOrEqualTo(harness.Viewport.rect.height));

                UnityEngine.Object.Destroy(harness.Presenter.gameObject);
                UnityEngine.Object.Destroy(harness.Canvas.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CampaignAndTowerBattleDetoursOpenTheOnlyActionableBattleScreen()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f), new Vector2(1920f, 1080f)
                     })
            {
                foreach (var campaignCase in new[]
                         {
                             new
                             {
                                 InProgress = true, AwaitingClaim = false,
                                 Button = "Return to campaign battle 084",
                                 Copy = "RETURN TO THE UNION BATTLE"
                             },
                             new
                             {
                                 InProgress = false, AwaitingClaim = true,
                                 Button = "Open campaign battle results 084",
                                 Copy = "OPEN BATTLE RESULTS & CLAIM REWARD"
                             }
                         })
                {
                    var campaignHarness = CreateHarness(resolution,
                        "CampaignBattleDetour" + campaignCase.Button);
                    var campaignState = new CampaignPlayablePresentationState020
                    {
                        IsAvailable = true,
                        ActiveOperationId = "C020_BATTLE_OPERATION_HIDDEN_084",
                        OperationTitle = "The Bell Beneath the Gate",
                        WorldName = "Skyhome",
                        Status = "AwaitingBattle",
                        CurrentStepIndex = 0,
                        TotalSteps = 1,
                        BattleInProgress = campaignCase.InProgress,
                        AwaitingBattleRewardClaim = campaignCase.AwaitingClaim,
                        Steps = new[]
                        {
                            new CampaignStepView020
                            {
                                StepId = "C020_BATTLE_STEP_HIDDEN_084",
                                TileType = "DANGER",
                                Title = "The Bell Chamber Guard",
                                Description = "Stand with every friendly Union.",
                                Status = "CURRENT",
                                RequiresCertifiedBattle = true,
                                ActionLabel = "ENTER UNION BATTLE",
                                RewardPreview = "Claim the existing battle reward"
                            }
                        }
                    };
                    InvokeBuild(campaignHarness.Presenter, "BuildGuildCityCampaign020",
                        campaignHarness.Body, new CampaignCoordinator084(campaignState),
                        campaignState);
                    Rebuild(campaignHarness.Body);
                    Assert.That(VisibleText(campaignHarness.Body),
                        Does.Contain(campaignCase.Copy));
                    Assert.That(Buttons(campaignHarness.Body).Any(value =>
                            value.name == "Commit campaign battle tile 084"), Is.False,
                        "A battle still in progress or awaiting its reward cannot be committed.");
                    var campaignPrimary = Buttons(campaignHarness.Body).Single(value =>
                        value.name == campaignCase.Button);
                    AssertInside(campaignHarness.Viewport,
                        campaignPrimary.GetComponent<RectTransform>(),
                        resolution + " Campaign battle detour");
                    UnityEngine.Object.Destroy(campaignHarness.Presenter.gameObject);
                    UnityEngine.Object.Destroy(campaignHarness.Canvas.gameObject);
                    yield return null;
                }

                var towerHarness = CreateHarness(resolution, "TowerBattleRewardDetour");
                var towerState = new CampaignProgressionPresentationState022
                {
                    IsAvailable = true,
                    ActiveAbyssOperationId = "TOWER_OPERATION_HIDDEN_084",
                    ActiveAbyssStatus = "AwaitingBattle",
                    ActiveStepIndex = 3,
                    ActiveStepCount = 5,
                    ActiveCompletedStepCount = 3,
                    ActiveOperationDisplayName = "Mud-Trench Guardian",
                    ActiveOperationKind = "GUARDIAN",
                    ActiveStepKind = "BATTLE",
                    ActiveStepTitle = "The Trench Warden",
                    ActiveStepDescription = "The Union battle is complete.",
                    ActiveStepRequiresBattle = true,
                    TowerBattleRewardAwaitingClaim = true,
                    TowerTrackPhase = 3,
                    TowerTrackLabels = new[]
                    {
                        "ENTER", "ROUTE", "EVENT", "DANGER", "RETURN"
                    },
                    TowerFloorNumber = 1,
                    TowerFloorDisplayName = "Mud Trenches",
                    TowerExpectedEnemyUnions = 1,
                    TowerRewardSummary = "+25 Guild XP  •  +19 Hall XP"
                };
                InvokeBuild(towerHarness.Presenter, "BuildGuildCityAbyss022",
                    towerHarness.Body, new TowerCoordinator084(towerState), towerState);
                Rebuild(towerHarness.Body);
                Assert.That(VisibleText(towerHarness.Body),
                    Does.Contain("OPEN BATTLE RESULTS & CLAIM REWARD"));
                Assert.That(Buttons(towerHarness.Body).Any(value =>
                    value.name == "Enter prepared Tower battle 081"), Is.False);
                var towerPrimary = Buttons(towerHarness.Body).Single(value =>
                    value.name == "Open Tower battle results 084");
                AssertInside(towerHarness.Viewport,
                    towerPrimary.GetComponent<RectTransform>(),
                    resolution + " Tower battle reward detour");
                UnityEngine.Object.Destroy(towerHarness.Presenter.gameObject);
                UnityEngine.Object.Destroy(towerHarness.Canvas.gameObject);
                yield return null;
            }
        }

        static IEnumerator VerifyCampaign020(Vector2 resolution)
        {
            var harness = CreateHarness(resolution, "Campaign020");
            // Prevent Start from replacing the screen used by this partial fixture.
            typeof(M1FlowPresenter).GetField("_canvas", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(harness.Presenter, harness.Canvas);
            var state = new CampaignPlayablePresentationState020
            {
                IsAvailable = true,
                ActiveOperationId = "OP020_PLAYER_HIDDEN_084",
                OperationTitle = "The Bell Beneath the Gate",
                WorldName = "Skyhome",
                Status = "Active",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Steps = new[]
                {
                    Step("STEP020_RAW_01", "BRIEFING", "BRIEFING", "CLEARED", "COMPLETED"),
                    Step("STEP020_RAW_02", "CIVIC_EVENT", "STORY", "Speak with the watch captain", "CURRENT"),
                    Step("STEP020_RAW_03", "DIPLOMACY", "FUTURE SECRET DIPLOMACY", "LEAKED FUTURE C020", "FUTURE"),
                    Step("STEP020_RAW_04", "OBJECTIVE", "FUTURE SECRET OBJECTIVE", "LEAKED FUTURE C020", "FUTURE"),
                    Step("STEP020_RAW_05", "RESULTS", "FUTURE SECRET RETURN", "LEAKED FUTURE C020", "FUTURE")
                }
            };
            InvokeBuild(harness.Presenter, "BuildGuildCityCampaign020", harness.Body,
                new CampaignCoordinator084(state), state);
            var cardRoot = CampaignCardRoot129(harness.Presenter);
            yield return null;
            yield return null;

            var visible = VisibleText(cardRoot);
            Assert.That(visible, Does.Contain("STORY MOMENT"));
            Assert.That(visible, Does.Contain("CONTINUE"));
            Assert.That(visible, Does.Not.Contain("QUEST PATH"));
            Assert.That(cardRoot.GetComponentsInChildren<RectTransform>(true)
                .Count(value => value.name == "Campaign Quest Scene 131"), Is.EqualTo(1));
            Assert.That(harness.Scroll.gameObject.activeInHierarchy, Is.False,
                "The old quest-path page must be hidden behind the card table.");
            Assert.That(visible, Does.Not.Contain("LEAKED FUTURE C020"));
            Assert.That(visible, Does.Not.Contain("STEP020_RAW"));
            var primary = Buttons(cardRoot).Single(value =>
                value.name == "Move forward campaign room 084");
            AssertInside(cardRoot, primary.GetComponent<RectTransform>(),
                resolution + " Campaign story primary move");
            UnityEngine.Object.Destroy(harness.Presenter.gameObject);
            UnityEngine.Object.Destroy(harness.Canvas.gameObject);
            yield return null;

            state.PendingStepReceiptId = "C020_RECEIPT_HIDDEN_084";
            state.PendingOutcome = "STORY_ADVANCED";
            state.PendingReward = "+25 Guild XP  •  witness rescued";
            var resultHarness = CreateHarness(resolution, "Campaign020Result");
            InvokeBuild(resultHarness.Presenter, "BuildGuildCityCampaign020",
                resultHarness.Body, new CampaignCoordinator084(state), state);
            var resultRoot = CampaignCardRoot129(resultHarness.Presenter);
            Assert.That(Buttons(resultRoot).Where(value =>
                value.name != "Campaign Quest Return To Guild 131" && value.name != "Story Card Skip 129"), Is.Empty,
                "A saved Campaign room receipt auto-applies after its card reveal.");
            Assert.That(VisibleText(resultRoot),
                Does.Contain("SAVED RESULT  •  CONTINUING THE STORY"));
            Assert.That(resultRoot.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Face Down Room Card Back 084"),
                Is.True);
            UnityEngine.Object.Destroy(resultHarness.Presenter.gameObject);
            UnityEngine.Object.Destroy(resultHarness.Canvas.gameObject);
            yield return null;
        }

        static RectTransform CampaignCardRoot129(M1FlowPresenter presenter)
        {
            var field = typeof(M1FlowPresenter).GetField("_campaignCardRoot129",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var root = field.GetValue(presenter) as RectTransform;
            Assert.That(root, Is.Not.Null);
            return root;
        }

        static IEnumerator VerifyWorldGate023(Vector2 resolution)
        {
            var harness = CreateHarness(resolution, "WorldGate023");
            var state = new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                CurrentWorldName = "Skyhome",
                TravelSupplies = 9,
                ActiveOperationId = "WGOP023_PLAYER_HIDDEN_084",
                ActiveBoardTitle = "The Bell Beneath the Gate",
                ActiveOperationKind = "CHAPTER",
                ActiveStatus = "Active",
                ExpeditionDeckTutorialSeen = true,
                BoardObjective = "Protect Skyhome's maintenance crew and silence the buried bell.",
                Supplies = 4,
                Fatigue = 1,
                Urgency = 0,
                Threat = 1,
                CompletedNodes = 1,
                TotalNodes = 12,
                AdventureTiles = AdventureBoardTrackProjection084.Project(
                    AuthoredBoard084(), "ROOM_01", new[] {"ROOM_00"}),
                CurrentNode = new NodeView023
                {
                    NodeId = "ROOM_01",
                    RoomTitle = "ROOM FLIPPED",
                    Title = "A Watch Captain's Warning",
                    Description = "Boots stop at the sealed bell chamber.",
                    StoryFlavor = "Maren asks the Guild to protect the maintenance crew before touching the bell.",
                    Objective = "Escort Maren to the bell chamber and bring everyone home.",
                    RewardPreview = "Story consequence and Guild growth",
                    IconResource = "SecondDimension/Campaign023/Icons/NODE_ROUTE_023",
                    ChoiceViews = new[]
                    {
                        new ChoiceView023 { ChoiceId = "SAFE_ROUTE", Label = "Follow Maren" },
                        new ChoiceView023 { ChoiceId = "BOLD_ROUTE", Label = "Scout ahead" }
                    }
                }
            };
            InvokeBuild(harness.Presenter, "BuildGuildCityWorldGate023", harness.Body,
                new WorldGateCoordinator084(state), state);
            var root = (RectTransform)typeof(M1FlowPresenter).GetField("_campaignQuestRoot131",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(harness.Presenter);
            Assert.That(root, Is.Not.Null);
            yield return null;
            yield return null;

            var visible = VisibleText(root);
            Assert.That(visible, Does.Contain("Protect Skyhome"));
            Assert.That(visible, Does.Contain("A Watch Captain's Warning"));
            Assert.That(visible, Does.Contain("Maren asks"),
                "The current authored chapter scene is visible before Continue.");
            Assert.That(visible, Does.Not.Contain("Boots stop"),
                "Face-down rooms must not reveal generic event copy before the move.");
            Assert.That(visible, Does.Not.Contain("WGOP023_PLAYER_HIDDEN_084"));
            Assert.That(visible, Does.Not.Contain("ROOM_01"));
            Assert.That(root.GetComponentsInChildren<RectTransform>(true)
                .Any(value => value.name.StartsWith("Adventure Compact Room ",
                    StringComparison.Ordinal)), Is.False,
                "The active quest presents its current action; the retired node graph must stay absent.");
            Assert.That(visible, Does.Not.Contain("YOUR PAWN"));
            Assert.That(visible, Does.Not.Contain("LEAKED FUTURE ROOM"));

            var primary = Buttons(root).Single(value =>
                value.name == "Move forward World Gate room 084");
            AssertInside(root, primary.GetComponent<RectTransform>(),
                resolution + " World Gate primary move");
            var reading = root.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Board Card Reading Column 091");
            Assert.That(primary.transform.IsChildOf(reading), Is.True);
            var scroll = reading.GetComponentInParent<ScrollRect>();
            var readingViewport = scroll != null ? scroll.viewport : reading;
            Assert.That(readingViewport, Is.Not.Null);
            AssertInside(root, readingViewport, resolution + " bounded quest reading column");
            AssertInside(readingViewport, primary.GetComponent<RectTransform>(),
                resolution + " current quest action fits its reading viewport");
            var currentView = state.AdventureTiles.Single(value =>
                value.State == AdventureBoardTrackProjection084.CurrentState);
            Assert.That(currentView.NodeId, Is.EqualTo(state.CurrentNode.NodeId),
                "The saved current-room projection must still identify the same authored room.");
            Assert.That(root.GetComponentsInChildren<RectTransform>(true)
                .Any(value => value.name == "Board Adventure Guild Pawn 086"), Is.False,
                "Removing the node graph must not leave an orphaned tabletop pawn.");
            UnityEngine.Object.Destroy(harness.Presenter.gameObject);
            UnityEngine.Object.Destroy(harness.Canvas.gameObject);
            yield return null;
        }

#if LEGACY_TOWER_CARDS
        static IEnumerator VerifyTower022(Vector2 resolution)
        {
            var harness = CreateHarness(resolution, "Tower022");
            typeof(M1FlowPresenter).GetField("_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(harness.Presenter, true);
            var state = new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                ActiveAbyssOperationId = "ABYSS_OP022_RAW_084",
                ActiveAbyssFloorId = "ABYSS_FLOOR022_01",
                ActiveAbyssStatus = "Active",
                ActiveStepIndex = 1,
                ActiveStepCount = 5,
                ActiveCompletedStepCount = 1,
                ActiveOperationDisplayName = "Mud-Trench Recon",
                ActiveOperationKind = "RECON",
                ActiveStepKind = "BOARD",
                ActiveStepTitle = "Turn the first room",
                ActiveStepDescription = "Your scouts mark a safe line through the trench.",
                HasPendingAbyssStepReceipt = true,
                PendingAbyssOutcome = "SAFE_ROUTE",
                PendingAbyssReward = "Scout route saved",
                TowerTrackPhase = 1,
                TowerTrackLabels = new[]
                {
                    "ENTER", "BOARD", "LEAKED TOWER EVENT", "LEAKED TOWER DANGER", "RETURN"
                },
                TowerFloorNumber = 1,
                TowerFloorDisplayName = "Mud Trenches",
                TowerExpectedEnemyUnions = 1,
                TowerRewardSummary = "+25 Guild XP  •  +19 Hall XP",
                TowerArtResourcePath = TowerRunRules081.ArtResourcePath(1),
                TowerRoomModuleId001 = "BTR001_TOWER_INTERNAL_HIDDEN_001",
                TowerRoomModuleDisplayName001 = "Sealed Landing",
                TowerRoomModuleNarrative001 =
                    "Sealed emblems overlook the landing as the Guild records the newly revealed route.",
                TowerRoomModuleRevealedStepNumber001 = 1
            };
            InvokeBuild(harness.Presenter, "BuildGuildCityAbyss022", harness.Body,
                new TowerCoordinator084(state), state);
            Rebuild(harness.Body);

            var futureCopies = harness.Body.GetComponentsInChildren<Text>(true).Where(value =>
                value.name.StartsWith("Tower Pawn Tile Copy ", StringComparison.Ordinal))
                .Skip(2).Select(value => value.text).ToArray();
            foreach (var copy in futureCopies)
                Assert.That(copy, Is.EqualTo("◇  FACE DOWN\n???"));
            var visible = VisibleText(harness.Body);
            Assert.That(visible, Does.Contain("ROOM FLIPPED  •  SAFE ROUTE"));
            Assert.That(visible, Does.Contain("RESULT  •  SAFE_ROUTE"));
            Assert.That(visible, Does.Contain("REWARD SAVED  •  Scout route saved"),
                "A persisted Tower receipt must read as saved, not as a prospective reward preview.");
            Assert.That(visible, Does.Contain(
                "SAVED  •  MOVING TO THE NEXT ROOM"));
            Assert.That(visible, Does.Not.Contain("BTR001_"));
            Assert.That(visible, Does.Not.Contain("BOON CHOICE"));
            Assert.That(visible, Does.Not.Contain("ROTATING ENVIRONMENT MODIFIER"));
            Assert.That(visible, Does.Not.Contain("ENHANCED REWARD"));
            Assert.That(Buttons(harness.Body), Is.Empty,
                "A saved Tower room receipt auto-applies after its card reveal.");
            var resolving = harness.Body.GetComponentsInChildren<Text>(true)
                .Single(value => value.name == "Tower Automatic Result Apply 084");
            AssertInside(harness.Scroll.content, resolving.rectTransform,
                resolution + " Tower automatic result status");
            var art = harness.Body.GetComponentsInChildren<Image>(true).Single(value =>
                value.name == "Tower Active Floor Art 084");
            Assert.That(art.sprite, Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(state.TowerArtResourcePath), Is.Not.Null);
            Assert.That(art.transform.parent.name,
                Is.EqualTo("Tower Active Floor Hero 085"));
            Assert.That(art.GetComponent<AspectRatioFitter>(), Is.Not.Null);
            Assert.That(art.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            Assert.That(art.GetComponentInParent<Mask>(), Is.Not.Null,
                "The active floor art must remain cover-fit inside its hero frame.");
            var currentTile = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Tower Track Card 1 084");
            Assert.That(currentTile.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(currentTile.localScale.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(currentTile.localScale.z, Is.EqualTo(1f).Within(0.001f));
            Assert.That(currentTile.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f).Within(0.001f));
            UnityEngine.Object.Destroy(harness.Presenter.gameObject);
            UnityEngine.Object.Destroy(harness.Canvas.gameObject);
            yield return null;
        }

#endif

        static IEnumerator VerifyTower022(Vector2 resolution)
        {
            var harness = CreateHarness(resolution, "Tower022BattleOnly");
            var state = new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                ActiveAbyssOperationId = "ABYSS_OP022_RAW_088",
                ActiveAbyssFloorId = "ABYSS_FLOOR022_01",
                ActiveAbyssStatus = "Active",
                ActiveStepIndex = 2,
                ActiveStepCount = 5,
                ActiveCompletedStepCount = 2,
                ActiveOperationDisplayName = "Mud-Trench Guardian",
                ActiveOperationKind = "GUARDIAN",
                ActiveStepKind = "BATTLE",
                ActiveStepTitle = "Defeat the trench guardian",
                ActiveStepDescription = "The Union must win to climb.",
                ActiveStepRequiresBattle = true,
                TowerFloorNumber = 1,
                TowerFloorDisplayName = "Mud Trenches",
                TowerExpectedEnemyUnions = 1,
                TowerRewardSummary = "+25 Guild XP  •  +19 Hall XP",
                TowerArtResourcePath = TowerRunRules081.ArtResourcePath(1)
            };
            InvokeBuild(harness.Presenter, "BuildGuildCityAbyss022", harness.Body,
                new TowerCoordinator084(state), state);
            Rebuild(harness.Body);

            var buttons = Buttons(harness.Body);
            Assert.That(buttons, Has.Length.EqualTo(1));
            Assert.That(buttons[0].name, Is.EqualTo("Enter Tower battle 081"));
            AssertInside(harness.Scroll.content, buttons[0].transform as RectTransform,
                resolution + " Tower battle action");
            var visible = VisibleText(harness.Body);
            Assert.That(visible, Does.Contain("FIGHT FLOOR 1"));
            Assert.That(visible, Does.Contain("VICTORY REWARD"));
            Assert.That(visible, Does.Not.Contain("FACE DOWN"));
            Assert.That(visible, Does.Not.Contain("MOVE PAWN"));
            Assert.That(visible, Does.Not.Contain("FLIP"));
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name.StartsWith("Tower Track Card ",
                        StringComparison.Ordinal)), Is.False);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Board Adventure Guild Pawn 086"),
                Is.False);
            var art = harness.Body.GetComponentsInChildren<Image>(true).Single(value =>
                value.name == "Tower Active Floor Art 084");
            Assert.That(art.sprite, Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(state.TowerArtResourcePath), Is.Not.Null);
            Assert.That(art.transform.parent.name,
                Is.EqualTo("Tower Active Floor Hero 085"));
            Assert.That(art.GetComponent<AspectRatioFitter>(), Is.Not.Null);
            Assert.That(art.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            Assert.That(art.GetComponentInParent<Mask>(), Is.Not.Null);
            UnityEngine.Object.Destroy(harness.Presenter.gameObject);
            UnityEngine.Object.Destroy(harness.Canvas.gameObject);
            yield return null;
        }

        static CampaignStepView020 Step(string id, string kind, string tile,
            string title, string status) => new CampaignStepView020
        {
            StepId = id,
            Kind = kind,
            TileType = tile,
            Title = title,
            Description = title,
            Status = status,
            RewardPreview = "Story progress saved",
            ActionLabel = "FLIP & SAVE STORY TILE"
        };

        static CampaignWorldGatePresentationState023 WorldGateState084(
            string status, bool battle, bool ready)
        {
            var pending = !battle && !ready && status == "Active";
            return new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                CurrentWorldName = "Skyhome",
                ActiveOperationId = "WGOP023_STATE_HIDDEN_084",
                ActiveBoardTitle = "Relief Road",
                ActiveOperationKind = "REPEATABLE",
                ActiveStatus = ready ? "ReadyToFinalize" : status,
                ExpeditionDeckTutorialSeen = true,
                BoardObjective = "Bring the relief caravan safely home.",
                Supplies = 3,
                AdventureTrackPhase = ready ? 5 : 3,
                PendingReceiptId = pending ? "WGREC023_STATE_HIDDEN_084" : string.Empty,
                PendingOutcomeTitle = pending ? "FULL SUCCESS" : string.Empty,
                PendingDice = pending ? "2D6  •  4 + 5" : string.Empty,
                PendingHasCheck = pending,
                PendingDifficulty = pending ? 8 : 0,
                PendingDieOne = pending ? 4 : 0,
                PendingDieTwo = pending ? 5 : 0,
                PendingModifier = pending ? 3 : 0,
                PendingTotal = pending ? 12 : 0,
                PendingReward = pending ? "+2 Guild XP  •  route saved" : string.Empty,
                CurrentNode = new NodeView023
                {
                    NodeId = "B023_STATE_HIDDEN_084",
                    RoomTitle = ready ? "RETURN" : battle ? "DANGER" : "CHECK",
                    Title = ready ? "The road home" : battle ? "Gate wolves" : "Collapsed bridge",
                    Description = ready ? "The Guild banner is in sight." :
                        battle ? "A hostile Union blocks the road." :
                        "The crew tests the remaining stones.",
                    Objective = "Bring the relief caravan home.",
                    RewardPreview = "Saved quest progress",
                    RequiresBattle = battle,
                    IconResource = battle
                        ? "SecondDimension/Campaign023/Icons/NODE_BATTLE_023"
                        : ready
                            ? "SecondDimension/Campaign023/Icons/NODE_EXIT_023"
                            : "SecondDimension/Campaign023/Icons/NODE_CHECK_023",
                    ChoiceViews = ready || battle
                        ? Array.Empty<ChoiceView023>()
                        : new[]
                        {
                            new ChoiceView023
                                { ChoiceId = "CAREFUL", Label = "Test the stones" }
                        }
                }
            };
        }

        static CampaignWorldGatePresentationState023 ProjectedCatalogState084(
            BoardDto023 board,
            NodeDto023 node,
            ChapterNarrative019 chapter,
            string world)
        {
            var copies = board.nodes.ToDictionary(value => value.nodeId,
                value => AdventureBoardNarrativeProjection084.Project(
                    board, value, chapter, world), StringComparer.Ordinal);
            var labels = copies.ToDictionary(
                value => value.Key, value => value.Value.Title, StringComparer.Ordinal);
            var completed = PathBefore084(board, node.nodeId);
            var copy = copies[node.nodeId];
            var choices = (node.choiceIds ?? Array.Empty<string>())
                .Select(value => new ChoiceView023
                {
                    ChoiceId = value,
                    Label = BoardAdventureRules084.ChoiceLabel084(value),
                    StableOrderKey = value
                }).ToArray();
            return new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                CurrentWorldId = board.worldId,
                CurrentWorldName = world,
                ActiveOperationId = "PROJECTED_CATALOG_STATE_084",
                ActiveDefinitionId = board.definitionId,
                ActiveBoardTitle = BoardAdventureRules084.PlayerCopy084(
                    board.title, board.definitionId, board.boardId),
                ActiveOperationKind = board.operationKind,
                ActiveStatus = "Active",
                ExpeditionDeckTutorialSeen = true,
                BoardObjective = AdventureBoardNarrativeProjection084.BoardObjective(
                    board, chapter, world),
                Supplies = 4,
                CompletedNodes = completed.Length,
                TotalNodes = board.nodes.Length,
                AdventureTiles = AdventureBoardTrackProjection084.Project(
                    board, node.nodeId, completed, null, labels),
                CurrentNode = new NodeView023
                {
                    NodeId = node.nodeId,
                    Kind = node.kind,
                    RoomKind = BoardAdventureRules084.RoomKind084(node.kind),
                    RoomTitle = BoardAdventureRules084.RoomTitle084(node.kind),
                    Title = copy.Title,
                    Description = copy.Description,
                    StoryFlavor = copy.StoryFlavor,
                    Objective = copy.Objective,
                    RewardPreview = "Only the listed tile reward is applied.",
                    Choices = node.choiceIds ?? Array.Empty<string>(),
                    ChoiceViews = choices,
                    Difficulty = node.checkDifficulty,
                    RequiresBattle = node.requiresCertifiedBattle,
                    IconResource = "SecondDimension/Campaign023/Icons/NODE_" +
                                   node.kind + "_023"
                }
            };
        }

        static string[] PathBefore084(BoardDto023 board, string targetNodeId)
        {
            var byId = board.nodes.ToDictionary(value => value.nodeId,
                StringComparer.Ordinal);
            var path = new List<string>();
            bool Find(string nodeId)
            {
                path.Add(nodeId);
                if (StringComparer.Ordinal.Equals(nodeId, targetNodeId)) return true;
                foreach (var next in byId[nodeId].nextNodeIds ?? Array.Empty<string>())
                    if (Find(next)) return true;
                path.RemoveAt(path.Count - 1);
                return false;
            }
            Assert.That(Find(board.startNodeId), Is.True,
                board.definitionId + " path to " + targetNodeId);
            return path.Take(Math.Max(0, path.Count - 1)).ToArray();
        }

        static BoardDto023 AuthoredBoard084()
        {
            NodeDto023 Node(string id, string kind, string title,
                params string[] next) => new NodeDto023
            {
                nodeId = id,
                kind = kind,
                title = title,
                description = title,
                objective = title,
                sourceId = "AUTHORED_TEST_084",
                nextNodeIds = next,
                choiceIds = next.Length > 1
                    ? new[] {"SAFE_ROUTE", "BOLD_ROUTE"}
                    : new[] {"CONTINUE"}
            };
            return new BoardDto023
            {
                boardId = "BOARD_TEST_084",
                definitionId = "DEFINITION_TEST_084",
                title = "Maren and the Buried Bell",
                startNodeId = "ROOM_00",
                exitNodeId = "ROOM_11",
                nodes = new[]
                {
                    Node("ROOM_00", "START", "Guild Hall Door", "ROOM_01", "ROOM_02"),
                    Node("ROOM_01", "ROUTE", "Maren's Measured Road", "ROOM_03"),
                    Node("ROOM_02", "ROUTE", "LEAKED FUTURE ROOM 02", "ROOM_03"),
                    Node("ROOM_03", "EVENT", "Workers at the Bell", "ROOM_04"),
                    Node("ROOM_04", "CHECK", "LEAKED FUTURE ROOM 04", "ROOM_05"),
                    Node("ROOM_05", "CAMP", "LEAKED FUTURE ROOM 05", "ROOM_06", "ROOM_07"),
                    Node("ROOM_06", "RESOURCE", "LEAKED FUTURE ROOM 06", "ROOM_08"),
                    Node("ROOM_07", "DIPLOMACY", "LEAKED FUTURE ROOM 07", "ROOM_08"),
                    Node("ROOM_08", "BATTLE", "LEAKED FUTURE ROOM 08", "ROOM_09"),
                    Node("ROOM_09", "OBJECTIVE", "LEAKED FUTURE ROOM 09", "ROOM_10"),
                    Node("ROOM_10", "RESULTS", "LEAKED FUTURE ROOM 10", "ROOM_11"),
                    Node("ROOM_11", "EXIT", "LEAKED FUTURE ROOM 11")
                }
            };
        }

        static CampaignWorldGatePresentationState023 AuthoredWorldGateState084(
            BoardDto023 board,
            System.Collections.Generic.IReadOnlyList<AdventureTrackTileView023> tiles,
            string currentNodeId)
        {
            var current = board.nodes.Single(value => value.nodeId == currentNodeId);
            return new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                CurrentWorldName = "Skyhome",
                ActiveOperationId = "WGOP023_AUTHORED_HIDDEN_084",
                ActiveBoardTitle = "Maren and the Buried Bell",
                ActiveOperationKind = "CHAPTER",
                ActiveStatus = "Active",
                ExpeditionDeckTutorialSeen = true,
                BoardObjective = "Protect the maintenance crew and silence the buried bell.",
                Supplies = 4,
                CompletedNodes = tiles.Count(value =>
                    value.State == AdventureBoardTrackProjection084.ClearedState),
                TotalNodes = tiles.Count,
                AdventureTiles = tiles,
                CurrentNode = new NodeView023
                {
                    NodeId = current.nodeId,
                    RoomTitle = "ROOM FLIPPED",
                    Title = current.title,
                    Description = "The Guild reaches the next saved room.",
                    StoryFlavor = "Maren leads the maintenance crew toward the buried bell.",
                    Objective = "Keep Maren and every worker together.",
                    RewardPreview = "Story consequence and Guild growth",
                    ChoiceViews = new[]
                    {
                        new ChoiceView023 {ChoiceId = "CONTINUE", Label = "Move together"}
                    }
                }
            };
        }

        static void AssertCompactAuthoredBoardRendered084(
            Harness084 harness, int expectedRooms, int pawnSpace, Vector2 resolution)
        {
            var tiles = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Where(value => value.name.StartsWith(
                    "Adventure Compact Room ", StringComparison.Ordinal) &&
                                !value.name.StartsWith(
                                    "Adventure Compact Room Copy ",
                                    StringComparison.Ordinal) &&
                                value.GetComponent<Image>() != null)
                .ToArray();
            Assert.That(tiles, Is.Not.Empty);
            Assert.That(tiles.Length, Is.LessThanOrEqualTo(5),
                resolution + " compact board must not expose more than five rooms at once");
            var visible = VisibleText(harness.Body);
            Assert.That(visible, Does.Contain(
                "ROOM " + pawnSpace + " OF " + expectedRooms));
            Assert.That(visible, Does.Not.Contain("LEAKED FUTURE ROOM"));
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name.StartsWith(
                        "Adventure Authored Room Tile ", StringComparison.Ordinal) ||
                                  value.name == "Adventure Authored Board Track 084"),
                Is.False,
                "The authored-world-gate test must cover the production compact strip, not retired tiles.");
            var faceDown = harness.Body.GetComponentsInChildren<Text>(true)
                .Where(value => value.name.StartsWith(
                                    "Adventure Compact Room Copy ",
                                    StringComparison.Ordinal) &&
                                value.text.Contains("FACE DOWN"))
                .ToArray();
            Assert.That(faceDown, Is.Not.Empty);
            Assert.That(faceDown.All(value =>
                !value.text.Contains("LEAKED FUTURE ROOM")), Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<Text>(true)
                    .Count(value => value.name.StartsWith(
                                       "Adventure Compact Room Copy ",
                                       StringComparison.Ordinal) &&
                                    value.text.Contains("YOUR PAWN")),
                Is.EqualTo(1),
                resolution + " compact board must show exactly one current pawn");
            var track = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Adventure Compact Progress Strip 084");
            Assert.That(track.GetComponent<LayoutElement>().preferredHeight,
                Is.LessThan(0f));
            foreach (var row in track.GetComponentsInChildren<RectTransform>(true)
                         .Where(value => value.name.StartsWith(
                             "Adventure Compact Progress Row ",
                             StringComparison.Ordinal)))
                AssertInside(track, row, resolution + " compact board row in strip");
            Assert.That(harness.Scroll.content.rect.height,
                Is.GreaterThan(harness.Viewport.rect.height),
                resolution + " compact board must remain scrollable");
        }

        static Harness084 CreateHarness(Vector2 resolution, string suffix)
        {
            var presenterObject = new GameObject(
                "Board Adventure PlayMode Presenter 084 " + suffix + " " + resolution.x);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();
            var canvasObject = new GameObject(
                "Board Adventure PlayMode Canvas 084 " + suffix + " " + resolution.x,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            typeof(M1FlowPresenter).GetField("_canvas", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(presenter, canvas); // Keep Start on this isolated fixture's existing canvas.
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = resolution;
            var screenRootObject = new GameObject(
                "Board Adventure Production Screen Root 084", typeof(RectTransform));
            var screenRoot = screenRootObject.GetComponent<RectTransform>();
            screenRoot.SetParent(canvasRect, false);
            screenRoot.anchorMin = Vector2.zero;
            screenRoot.anchorMax = Vector2.one;
            screenRoot.offsetMin = new Vector2(96f, 42f);
            screenRoot.offsetMax = new Vector2(-96f, -42f);

            typeof(M1FlowPresenter).GetField("_screenRoot",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(presenter, screenRoot);
            typeof(M1FlowPresenter).GetField("_coordinator",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(presenter, new PageCoordinator084());
            var createPage = typeof(M1FlowPresenter).GetMethod("CreatePage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(createPage, Is.Not.Null, "Production CreatePage");
            var body = (RectTransform)createPage.Invoke(presenter,
                new object[]
                {
                    "BOARD ADVENTURE", "ONE SAVED TILE AT A TIME", null
                });
            var scroll = body.GetComponentInParent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null,
                "The production board page must use the shipping ScrollRect.");
            Assert.That(scroll.content, Is.SameAs(body));
            Assert.That(scroll.viewport.GetComponent<Mask>(), Is.Not.Null);
            return new Harness084(presenter, canvas, canvasRect, body, scroll);
        }

        static void InvokeBuild(M1FlowPresenter presenter, string methodName,
            Transform body, object coordinator, object state)
        {
            var method = typeof(M1FlowPresenter).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(presenter, new[] { body, coordinator, state });
        }

        static void Rebuild(RectTransform body)
        {
            var scroll = body.GetComponentInParent<ScrollRect>();
            var page = scroll == null ? null : scroll.transform.parent as RectTransform;
            Canvas.ForceUpdateCanvases();
            if (page != null) LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            if (page != null) LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        static string VisibleText(RectTransform body) => string.Join("\n",
            body.GetComponentsInChildren<Text>(true).Select(value => value.text));

        static Button[] Buttons(RectTransform body) =>
            body.GetComponentsInChildren<Button>(true);

        static void AssertInside(RectTransform outer, RectTransform inner, string label)
        {
            var outerCorners = new Vector3[4];
            var innerCorners = new Vector3[4];
            outer.GetWorldCorners(outerCorners);
            inner.GetWorldCorners(innerCorners);
            Assert.That(innerCorners.Min(value => value.x),
                Is.GreaterThanOrEqualTo(outerCorners.Min(value => value.x) - 0.5f), label);
            Assert.That(innerCorners.Max(value => value.x),
                Is.LessThanOrEqualTo(outerCorners.Max(value => value.x) + 0.5f), label);
            Assert.That(innerCorners.Min(value => value.y),
                Is.GreaterThanOrEqualTo(outerCorners.Min(value => value.y) - 0.5f), label);
            Assert.That(innerCorners.Max(value => value.y),
                Is.LessThanOrEqualTo(outerCorners.Max(value => value.y) + 0.5f), label);
        }

        static void AssertAutoSizedAncestorContains084(Button button, string label)
        {
            var content = button.GetComponentInParent<ScrollRect>()?.content;
            var ancestor = button.transform.parent as RectTransform;
            while (ancestor != null && ancestor != content &&
                   ancestor.GetComponent<LayoutElement>() == null)
                ancestor = ancestor.parent as RectTransform;
            Assert.That(ancestor, Is.Not.Null, label + " has no layout owner.");
            Assert.That(ancestor, Is.Not.SameAs(content),
                label + " must be owned by its content-driven action/result panel.");
            var layout = ancestor.GetComponent<LayoutElement>();
            Assert.That(layout.preferredHeight, Is.LessThan(0f),
                label + " still uses a fixed preferred height.");
            AssertInside(ancestor, button.GetComponent<RectTransform>(), label);
        }

        readonly struct Harness084
        {
            public Harness084(M1FlowPresenter presenter, Canvas canvas,
                RectTransform canvasRect, RectTransform body, ScrollRect scroll)
            {
                Presenter = presenter;
                Canvas = canvas;
                CanvasRect = canvasRect;
                Body = body;
                Scroll = scroll;
            }

            public M1FlowPresenter Presenter { get; }
            public Canvas Canvas { get; }
            public RectTransform CanvasRect { get; }
            public RectTransform Body { get; }
            public ScrollRect Scroll { get; }
            public RectTransform Viewport => Scroll.viewport;
        }

        sealed class PageCoordinator084 : IM1PresentationCoordinator
        {
            public event Action Changed;
            public M1PresentationState State { get; } = new M1PresentationState
            {
                HasCampaign = true,
                GuildXpIntoCurrentLevel = 25,
                GuildXpRequiredForNextLevel = 100,
                TreasuryXp = 40
            };
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Pass084();
            public M1CommandResult SignRecruit(string recruitId) => Pass084();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => Pass084();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => Pass084();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => Pass084();
            public M1CommandResult CompleteEquipmentReview() => Pass084();
            public M1CommandResult AddUnion() => Pass084();
            public M1CommandResult RemoveUnion(int unionIndex) => Pass084();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => Pass084();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => Pass084();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => Pass084();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => Pass084();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => Pass084();
            public M1CommandResult SaveAndReloadProof() => Pass084();
            static M1CommandResult Pass084() => M1CommandResult.Success("Saved for test.");
#pragma warning disable 67
            void PreserveChanged084() => Changed?.Invoke();
#pragma warning restore 67
        }

        sealed class CampaignCoordinator084 : ICampaignPlayablePresentationCoordinator020
        {
            public CampaignCoordinator084(CampaignPlayablePresentationState020 state) =>
                CampaignPlayable020 = state;
            public CampaignPlayablePresentationState020 CampaignPlayable020 { get; }
            public M1CommandResult StartPlayableChapter020(string chapterId) => M1CommandResult.Success();
            public M1CommandResult CommitPlayableStep020(string outcome) => M1CommandResult.Success();
            public M1CommandResult ApplyPlayableStep020() => M1CommandResult.Success();
            public M1CommandResult EnterPlayableBattle020() => M1CommandResult.Success();
            public M1CommandResult CommitPlayableBattleStepResult020() => M1CommandResult.Success();
            public M1CommandResult FinalizePlayableChapter020() => M1CommandResult.Success();
            public M1CommandResult ApplyPlayableChapterResult020() => M1CommandResult.Success();
      public M1CommandResult RecoverLegacyPlayableQuest020() => M1CommandResult.Success();
      public M1CommandResult RecoverInsertedBattleBoundary020() => M1CommandResult.Success();
        }

        sealed class WorldGateCoordinator084 : ICampaignWorldGatePresentationCoordinator023
        {
            public WorldGateCoordinator084(CampaignWorldGatePresentationState023 state) =>
                CampaignWorldGate023 = state;
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; }
            public M1CommandResult BeginWorldGateOperation023(string definitionId) => M1CommandResult.Success();
            public M1CommandResult CommitWorldGateChoice023(string choiceId) => M1CommandResult.Success();
            public M1CommandResult CommitAutomaticWorldGateRoom023() => M1CommandResult.Success();
            public M1CommandResult ApplyWorldGateReceipt023() => M1CommandResult.Success();
            public M1CommandResult EnterWorldGateBattle023() => M1CommandResult.Success();
            public M1CommandResult FinalizeWorldGateOperation023() => M1CommandResult.Success();
            public M1CommandResult TravelWorldGate023(string worldId) => M1CommandResult.Success();
            public M1CommandResult RecoverLegacyWorldGateQuest023() => M1CommandResult.Success();
        }

        sealed class TowerCoordinator084 : ICampaignProgressionPresentationCoordinator022
        {
            public TowerCoordinator084(CampaignProgressionPresentationState022 state) =>
                CampaignProgression022 = state;
            public CampaignProgressionPresentationState022 CampaignProgression022 { get; }
            public M1CommandResult RecordEquipmentUse022(string itemInstanceId, string trackId) => M1CommandResult.Success();
            public M1CommandResult EvolveWeapon022(string itemInstanceId, string recipeId) => M1CommandResult.Success();
            public M1CommandResult CertifyAdvancedClass022(string recruitId, string classId) => M1CommandResult.Success();
            public M1CommandResult BeginAbyssOperation022(string operationId) => M1CommandResult.Success();
            public M1CommandResult BeginTowerRun081() => M1CommandResult.Success();
            public M1CommandResult AdvanceTowerRun081() => M1CommandResult.Success();
            public M1CommandResult RetreatTowerRun081() => M1CommandResult.Success();
            public M1CommandResult RecoverInactiveLegacyTower084() => M1CommandResult.Success();
            public M1CommandResult EnterAbyssBattle022() => M1CommandResult.Success();
            public M1CommandResult CommitAbyssStep022() => M1CommandResult.Success();
            public M1CommandResult ApplyAbyssStep022() => M1CommandResult.Success();
            public M1CommandResult CommitAbyssBattleResult022() => M1CommandResult.Success();
            public M1CommandResult FinalizeAbyssOperation022() => M1CommandResult.Success();
            public M1CommandResult FinalizeAbyssBattle022() => M1CommandResult.Success();
            public M1CommandResult CraftInvocationArtifact022(string baseId) => M1CommandResult.Success();
            public M1CommandResult CraftInvocationArtifact022(string baseId,
                System.Collections.Generic.IReadOnlyList<string> affixIds) => M1CommandResult.Success();
            public M1CommandResult EvolveInvocationArtifact022(string itemInstanceId) => M1CommandResult.Success();
            public M1CommandResult InvokeEligibleEchoForecast022() => M1CommandResult.Success();
            public M1CommandResult InvokeAcceptedCovenantForecast022(string covenantId) => M1CommandResult.Success();
            public M1CommandResult AdvanceCovenant022(string covenantId) => M1CommandResult.Success();
            public M1CommandResult AcceptCovenant022(string covenantId) => M1CommandResult.Success();
        }
    }
}
