using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2PresentationFlowTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<M1FlowPresenter>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.name == "M1 Playable Proof Canvas") UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletedGuildEntersCinematicBattleWithoutDeveloperProofDashboard()
        {
            var fake = new FakeM2Coordinator(M1Screen.Complete, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Assert.That(FindButton("PLAY A BATTLE NOW"), Is.Not.Null);
            Assert.That(FindButton("REVIEW RECRUITS & UNIONS"), Is.Not.Null);
            Click("PLAY A BATTLE NOW");
            yield return null;

            Assert.That(fake.StartCalls, Is.EqualTo(1));
            Assert.That(CountNamed("Live Cinematic Battlefield"), Is.EqualTo(1));
            AssertText("CHOOSE COMPLETE UNION COMMANDS");
            AssertNoText("CANONICAL STATE HASH");
            AssertNoText("UPDATE 010");
            AssertNoText("SELECT INDIVIDUAL ART");
            AssertNoText("MOVEMENT SQUARE");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator BattleStartFailureIsVisibleInsteadOfLeavingADeadScreen()
        {
            var fake = new FakeM2Coordinator(M1Screen.Complete, resolved: false, startFails: true);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("PLAY A BATTLE NOW");
            yield return null;

            Assert.That(fake.StartCalls, Is.EqualTo(1));
            AssertText("BATTLE START BLOCKED");
            Assert.That(CountNamed("Live Cinematic Battlefield"), Is.Zero);
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator BattlePresentsNonGridArenaResourcesAndNonClickableMemberPredictions()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            Assert.That(CountNamed("Live Cinematic Battlefield"), Is.EqualTo(1));
            Assert.That(CountNamed("Battle Character Artwork"), Is.EqualTo(8));
            Assert.That(GameObject.Find("Equipment Attachment Sockets 018"), Is.Not.Null,
                "Every 3D battle actor must expose the shared weapon/armor attachment contract.");
            Assert.That(GameObject.Find(M2EquipmentVisualPolicy018.RightHandSocket), Is.Not.Null);
            Assert.That(GameObject.Find(M2EquipmentVisualPolicy018.LeftHandSocket), Is.Not.Null);
            Assert.That(GameObject.Find(M2EquipmentVisualPolicy018.BodyArmorSocket), Is.Not.Null,
                "All three stable anchors must exist even when legacy loadout data cannot prove slot occupancy.");
            AssertText("CHOOSE ONE ORDER PER UNION");
            AssertText("AP 18/18");
            AssertText("HP 100/120");
            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return null;
            FindNamedButton("Focus Union Command UNION_A").onClick.Invoke();
            yield return null;
            AssertText("PREDICTED MEMBER ARTS");
            var predictions = FindNamed("Predicted Arts Non Clickable").ToArray();
            Assert.That(predictions.Length, Is.EqualTo(3),
                "The selected complete Union command must visibly predict one action per participating member.");
            foreach (var prediction in predictions)
                Assert.That(prediction.GetComponentInParent<Button>(), Is.Null, prediction.name);
            AssertNoText("SELECT INDIVIDUAL ART");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator FirstHourEncounterUsesThePersistent072DioramaAndCompactUnionLayer()
        {
            var fake = new FakeM2Coordinator(
                M1Screen.Battle,
                resolved: false,
                battleId: "BATTLE_CONTRACT_FIRST_STORY_ENCOUNTER071_HALL_BREACH_A1B2C3D4");
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            Assert.That(presenter.IsFirstHourBattleExperienceActive072, Is.True);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<M2BattleExperienceController072>(), Is.Not.Null);
            Assert.That(GameObject.Find("Persistent First Hour Battle Experience 072"), Is.Not.Null);
            Assert.That(GameObject.Find("Battle Diorama Presenter 072"), Is.Not.Null);
            Assert.That(GameObject.Find("Compact Union Command Layer 072"), Is.Not.Null);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<M2Battle3DWorld>(), Is.Null,
                "The first-hour 072 diorama must be the sole battle visual authority.");
            Assert.That(GameObject.Find("Battle Encounter Title 076").GetComponent<Text>().text,
                Is.EqualTo("HALL BREACH"));
            Assert.That(GameObject.Find("Battle Objective 072").GetComponent<Text>().text,
                Does.Contain("STAKES").And.Contain("OBJECTIVE").And.Contain("Guild Hall"));
            Assert.That(GameObject.Find("Target Enemy Union Name 072").GetComponent<Text>().text,
                Does.Not.Contain("TRAINING PROJECTION"),
                "The Hall Breach fixture must carry the same encounter identity as the real first-hour runtime.");
            var coach = GameObject.Find("First Battle Union Coach 076").GetComponent<RectTransform>();
            var tacticalHeader = GameObject.Find("Union Focus Header 072").GetComponent<RectTransform>();
            Assert.That(coach.anchorMax.y - coach.anchorMin.y, Is.LessThanOrEqualTo(0.13f),
                "The one-time Union lesson must preserve most of the encounter stage and both formations.");
            Assert.That(coach.anchorMax.y, Is.LessThan(tacticalHeader.anchorMin.y),
                "The docked lesson must never collide with encounter, morale, or active-Union status rows.");
            Assert.That(GameObject.Find("First Battle Coach Lesson 076").GetComponent<Text>().resizeTextMinSize,
                Is.GreaterThanOrEqualTo(21));
            var takeCommand = GameObject.Find("Dismiss First Battle Union Coach 076").GetComponent<Button>();
            Assert.That(takeCommand.GetComponentInChildren<Text>().text, Is.EqualTo("TAKE COMMAND"));
            Assert.That(takeCommand.GetComponentInChildren<Text>().resizeTextMinSize, Is.GreaterThanOrEqualTo(24));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(takeCommand.gameObject),
                "TAKE COMMAND must open as the unmistakable selected primary action.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ChapterTwoFogStalkersUsesPersistent072DioramaAndShippingUnionControls078()
        {
            var fake = new FakeM2Coordinator(
                M1Screen.Battle,
                resolved: false,
                battleId: "BATTLE_CONTRACT_LINES_NOT_RETURNED_ENCOUNTER_FOG_STALKERS_STANDARD_C14FF436FB15");
            fake.State.Battle.Objective =
                "Follow the Wayglass route, find the missing survey crew, and locate the door inside Skyhome.";
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            Assert.That(presenter.IsFirstHourBattleExperienceActive072, Is.True,
                "The authored Chapter 2 battle must keep the improved first-hour combat presenter.");
            Assert.That(GameObject.Find("Persistent First Hour Battle Experience 072"), Is.Not.Null);
            Assert.That(GameObject.Find("Union Focus Chip 1 072"), Is.Not.Null,
                "Fog Stalkers must expose the same shipping Union controls certified by the first-hour smoke.");
            Assert.That(GameObject.Find("Battle Encounter Title 076").GetComponent<Text>().text,
                Is.EqualTo("THE FOG STALKERS"));
            Assert.That(GameObject.Find("Battle Objective 072").GetComponent<Text>().text,
                Does.Contain("Sella").And.Contain("Orra").And.Contain("missing survey crew"),
                "The improved combat presenter must retain the authored Chapter 2 stakes and objective.");
            Assert.That(UnityEngine.Object.FindFirstObjectByType<M2Battle3DWorld>(), Is.Null,
                "The legacy cinematic battlefield must not replace the persistent 072 diorama in Chapter 2.");
            Assert.That(GameObject.Find("Focus Union Command UNION_A"), Is.Null,
                "Legacy Union command controls must not be layered beneath the 072 command HUD.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ChapterTwoSurveyorRescueUsesPersistent072DioramaAndShippingUnionControls080()
        {
            var fake = new FakeM2Coordinator(
                M1Screen.Battle,
                resolved: false,
                battleId: "BATTLE_CONTRACT_LINES_NOT_RETURNED_ENCOUNTER_SURVEYOR_RESCUE_080");
            fake.State.Battle.Objective =
                "Break the Chaincaller's line, free Orra's survey crew, and secure the unrecorded door.";
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            Assert.That(presenter.IsFirstHourBattleExperienceActive072, Is.True,
                "The Chapter 2 rescue climax must keep the improved Union-combat presenter.");
            Assert.That(GameObject.Find("Persistent First Hour Battle Experience 072"), Is.Not.Null);
            Assert.That(GameObject.Find("Union Focus Chip 1 072"), Is.Not.Null,
                "The surveyor rescue must expose the same shipping Union controls as the other four slice battles.");
            Assert.That(GameObject.Find("Battle Encounter Title 076").GetComponent<Text>().text,
                Is.EqualTo("THE LAST FALSE LINE"));
            Assert.That(GameObject.Find("Battle Objective 072").GetComponent<Text>().text,
                Does.Contain("Orra's survey crew").And.Contain("unrecorded door"),
                "The improved combat presenter must preserve the authored rescue stakes and objective.");
            Assert.That(UnityEngine.Object.FindFirstObjectByType<M2Battle3DWorld>(), Is.Null,
                "The legacy cinematic battlefield must not replace the persistent 072 diorama in the rescue climax.");
            Assert.That(GameObject.Find("Focus Union Command UNION_A"), Is.Null,
                "Legacy Union command controls must not be layered beneath the rescue's 072 command HUD.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator EveryLaterStoryAndTowerBattleUsesShippingUnionPresentation087()
        {
            var fake = new FakeM2Coordinator(
                M1Screen.Battle,
                resolved: false,
                battleId: "BATTLE_TOWER_FLOOR_27_AFTER_CHAPTER_TWO_087");
            fake.State.Battle.Objective =
                "Climb the next Tower floor and defeat the guardian Union.";
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            Assert.That(presenter.IsFirstHourBattleExperienceActive072, Is.True,
                "All authoritative battles must use the shipping diorama and Union Forecast HUD.");
            Assert.That(GameObject.Find("Persistent First Hour Battle Experience 072"), Is.Not.Null);
            Assert.That(GameObject.Find("Compact Union Command Layer 072"), Is.Not.Null);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<M2Battle3DWorld>(), Is.Null,
                "A later story or Tower battle must never fall back to the legacy presenter.");
            Assert.That(GameObject.Find("Focus Union Command UNION_A"), Is.Null,
                "Legacy Union controls must not be layered beneath the shipping HUD.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ReplacingBattleHostReusesOneControllerOwnedPresentationStack078()
        {
            var fake = new FakeM2Coordinator(
                M1Screen.Battle,
                resolved: false,
                battleId: "BATTLE_CONTRACT_FIRST_STORY_ENCOUNTER071_HALL_BREACH_A1B2C3D4");
            var controllerObject = new GameObject("Battle Host Replacement Controller 078");
            var controller = controllerObject.AddComponent<M2BattleExperienceController072>();
            var firstHostObject = new GameObject("First Battle Host 078", typeof(RectTransform));
            var firstHost = firstHostObject.GetComponent<RectTransform>();

            controller.Initialize(fake, firstHost);
            Assert.That(controller.EnterCurrentBattle(), Is.True);
            yield return null;

            var originalDiorama = controller.OwnedDiorama078;
            var originalHud = controller.OwnedCommandHud078;
            var originalResults = controller.OwnedResultsView078;
            var originalSequence = controller.OwnedSequenceDirector078;
            Assert.That(originalDiorama, Is.Not.Null);
            Assert.That(originalHud, Is.Not.Null);
            Assert.That(originalResults, Is.Not.Null);
            Assert.That(originalSequence, Is.Not.Null);

            // Normal navigation destroys the screen host while the controller stays
            // on M1FlowPresenter. Re-entering battle must rebuild visual roots without
            // leaking a stale Hall-Breach presenter beside the new encounter.
            UnityEngine.Object.Destroy(firstHostObject);
            yield return null;
            var replacementHostObject = new GameObject(
                "Replacement Battle Host 078",
                typeof(RectTransform));
            var replacementHost = replacementHostObject.GetComponent<RectTransform>();

            controller.Initialize(fake, replacementHost);
            Assert.That(controller.EnterCurrentBattle(), Is.True);
            yield return null;

            Assert.That(controller.OwnedDiorama078, Is.SameAs(originalDiorama));
            Assert.That(controller.OwnedCommandHud078, Is.SameAs(originalHud));
            Assert.That(controller.OwnedResultsView078, Is.SameAs(originalResults));
            Assert.That(controller.OwnedSequenceDirector078, Is.SameAs(originalSequence));
            Assert.That(controller.GetComponentsInChildren<M2BattleDioramaView072>(true).Length,
                Is.EqualTo(1), "Host replacement leaked a stale battle diorama.");
            Assert.That(controller.GetComponentsInChildren<M2BattleCommandHud072>(true).Length,
                Is.EqualTo(1), "Host replacement leaked a stale command HUD.");
            Assert.That(controller.GetComponentsInChildren<M2BattleResultsView072>(true).Length,
                Is.EqualTo(1), "Host replacement leaked a stale results presenter.");
            Assert.That(controller.GetComponentsInChildren<M2BattleSequenceDirector072>(true).Length,
                Is.EqualTo(1), "Host replacement leaked a stale sequence director.");

            UnityEngine.Object.Destroy(replacementHostObject);
            UnityEngine.Object.Destroy(controllerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PerspectiveArenaPaintingStaysCameraLockedWhilePhysicalFloorRemainsHorizontal()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            var cameraObject = GameObject.Find("Battle 3D Perspective Camera");
            var farVista = GameObject.Find("Tactical Gateworks Authored Far Vista 020");
            var floor = GameObject.Find("Twenty Union Gateworks Arena Floor 020");
            Assert.That(cameraObject, Is.Not.Null);
            Assert.That(farVista, Is.Not.Null);
            Assert.That(floor, Is.Not.Null);

            var battleCamera = cameraObject.GetComponent<Camera>();
            Assert.That(battleCamera, Is.Not.Null);
            Assert.That(farVista.transform.parent, Is.EqualTo(cameraObject.transform),
                "The baked perspective painting must follow the battle camera, never stand upright as world geometry.");
            Assert.That(farVista.transform.localPosition.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(farVista.transform.localPosition.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(farVista.transform.localPosition.z, Is.GreaterThan(0f));
            Assert.That(farVista.transform.localPosition.z, Is.LessThan(battleCamera.farClipPlane));
            Assert.That(Quaternion.Angle(farVista.transform.localRotation, Quaternion.identity),
                Is.LessThan(0.01f));
            Assert.That(Vector3.Dot(floor.transform.up, Vector3.up), Is.GreaterThan(0.999f),
                "Only the real XZ arena mesh may serve as the walkable-looking battle floor.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator BattleOpensWithRecognizableUnionCommandInsideTheActiveViewport()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            AssertText("CHOOSE COMPLETE UNION COMMANDS");
            Assert.That(FindButton("ATTACK!"), Is.Not.Null);
            Assert.That(FindButton("ATTACK USING COMBAT ARTS!"), Is.Not.Null);
            Assert.That(FindButton("HOLD THE LINE!"), Is.Not.Null);
            Assert.That(FindButton("RECOVER AP!"), Is.Not.Null);
            AssertButtonOnScreen("ATTACK!");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator TenActiveUnionsUseTwoByFiveNavigatorWithReadableTouchTargets()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            fake.ExpandToTenPlayerUnions();
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var tabs = FindNamed("Active Union Command Tabs").Single();
            var chips = tabs.GetComponentsInChildren<Button>()
                .Where(value => value.name.StartsWith("Focus Union Command ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(chips.Length, Is.EqualTo(10));

            var rowCenters = new List<float>();
            for (var index = 1; index <= 10; index++)
            {
                var suffix = index.ToString("00");
                var chip = chips.Single(value => StringComparer.Ordinal.Equals(
                    value.name, "Focus Union Command UNION_" + suffix));
                var label = chip.GetComponentInChildren<Text>();
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Does.Contain("UNION " + suffix));
                Assert.That(label.text, Does.Contain("ORDER NEEDED"));

                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    tabs, chip.GetComponent<RectTransform>());
                Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(44f), chip.name + " width");
                Assert.That(bounds.size.y, Is.GreaterThanOrEqualTo(44f), chip.name + " height");
                rowCenters.Add(bounds.center.y);
                AssertButtonOnScreen(index.ToString("00") + " · UNION " + suffix);
            }

            var midpoint = (rowCenters.Min() + rowCenters.Max()) * 0.5f;
            Assert.That(rowCenters.Count(value => value > midpoint), Is.EqualTo(5));
            Assert.That(rowCenters.Count(value => value < midpoint), Is.EqualTo(5));
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator SelectingFirstUnionShowsNextUnionAndKeepsAVisibleChangePath()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            var commandCamera = GameObject.Find("Battle 3D Perspective Camera");
            Assert.That(commandCamera, Is.Not.Null);
            AssertText("UNION 1 OF 2");
            yield return WaitForUnionFocus("UNION_A", Vector3.zero, 2f);
            var firstUnionCamera = commandCamera.transform.position;

            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return WaitForUnionFocus("UNION_B", firstUnionCamera, 2f);
            Canvas.ForceUpdateCanvases();

            Assert.That(FindNamedButton("Complete Union Command FORECAST_A1"), Is.Null);
            Assert.That(FindButton("USE MYSTIC ARTS!"), Is.Not.Null);
            AssertButtonOnScreen("USE MYSTIC ARTS!");
            AssertText("UNION 2 OF 2");
            Assert.That(Vector3.Distance(firstUnionCamera, commandCamera.transform.position), Is.GreaterThan(0.5f),
                "Choosing Union 1 must visibly move command focus to the separate Union 2 formation.");

            FindNamedButton("Focus Union Command UNION_A").onClick.Invoke();
            yield return null;
            Assert.That(FindNamedButton("Complete Union Command FORECAST_A1"), Is.Not.Null);
            AssertText("CHANGE · FIRST UNION");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator CompleteForecastSelectionEnablesOneFinalConfirmRoundAction()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            var selectButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Complete Union Command", StringComparison.Ordinal))
                .ToArray();
            Assert.That(selectButtons.Length, Is.EqualTo(4));
            selectButtons.First(value => value.name.Contains("A1")).onClick.Invoke();
            yield return null;
            selectButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Complete Union Command", StringComparison.Ordinal))
                .ToArray();
            selectButtons.First(value => value.name.Contains("B1")).onClick.Invoke();
            yield return null;

            var confirm = FindButton("CONFIRM ROUND");
            Assert.That(confirm, Is.Not.Null);
            Assert.That(confirm.interactable, Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(value => value.name == "Confirm Complete Forecast Round"), Is.EqualTo(1));
            confirm.onClick.Invoke();
            yield return null;
            Assert.That(fake.ConfirmCalls, Is.EqualTo(1));
            yield return WaitForText("VICTORY", 20f);
            AssertText("VICTORY");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ConfirmedRoundShowsResolutionOnlyArenaBeforeEventPlaybackCompletes()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_B1").onClick.Invoke();
            yield return null;
            FindButton("CONFIRM ROUND").onClick.Invoke();

            AssertText("COMMANDS IN MOTION");
            Assert.That(CountNamed("Live Cinematic Battlefield"), Is.EqualTo(1));
            Assert.That(CountNamed("Always Visible Union Command Tray"), Is.EqualTo(0));
            Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(value => value.name.StartsWith("Complete Union Command", StringComparison.Ordinal)), Is.False);

            yield return WaitForText("VICTORY", 20f);
            AssertText("VICTORY");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator TerminalActionBreakthroughShowsNewArtBeforeBattleResults()
        {
            var fake = new FakeM2Coordinator(
                M1Screen.Battle,
                resolved: false,
                terminalBreakthrough: true);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;

            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_B1").onClick.Invoke();
            yield return null;
            FindButton("CONFIRM ROUND").onClick.Invoke();
            var authorityHash = fake.State.Battle.FinalStateHash;
            FindButton("4×").onClick.Invoke();

            yield return WaitForText("NEW ART LEARNED", 20f);
            Assert.That(CountNamedExactly("Terminal Art Breakthrough Celebration 071"), Is.EqualTo(1));
            AssertText("FIRST UNION LEADER");
            AssertText("POWER CUT");
            Assert.That(CountNamed("Cinematic Result Progression Dashboard 021"), Is.Zero,
                "The terminal breakthrough must remain visible in battle before the result transition.");
            Assert.That(fake.ClaimCalls, Is.Zero,
                "Breakthrough feedback is presentation-only and cannot claim or reorder saved rewards.");
            Assert.That(fake.State.Battle.FinalStateHash, Is.EqualTo(authorityHash),
                "The breakthrough reveal cannot mutate authoritative terminal battle state.");

            yield return WaitForText("VICTORY", 8f);
            Assert.That(CountNamed("Cinematic Result Progression Dashboard 021"), Is.EqualTo(1));
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator BattlefieldAndCompleteUnionCommandTrayRemainCoVisibleWithoutScrolling()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var arena = FindNamed("Live Cinematic Battlefield").Single();
            var tray = FindNamed("Always Visible Union Command Tray").Single();
            Assert.That(arena.GetComponentInParent<ScrollRect>(), Is.Null);
            Assert.That(tray.GetComponentInParent<ScrollRect>(), Is.Null);
            Assert.That(arena.gameObject.activeInHierarchy, Is.True);
            Assert.That(tray.gameObject.activeInHierarchy, Is.True);
            AssertButtonOnScreen("ATTACK!");
            Assert.That(CountNamed("Battle Character Artwork"), Is.EqualTo(8),
                "Every deployed member and enemy must remain visibly represented in the arena.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator CommandsReadAsVerticalUnionOrdersOverTheVisibleBattlefield()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            Canvas.ForceUpdateCanvases();

            AssertText("CHOOSE AN ORDER FOR FIRST UNION");
            var commands = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Complete Union Command", StringComparison.Ordinal))
                .OrderByDescending(value => value.GetComponent<RectTransform>().position.y)
                .ToArray();
            Assert.That(commands.Length, Is.EqualTo(4));
            Assert.That(commands.Select(value => Mathf.RoundToInt(value.GetComponent<RectTransform>().position.y))
                .Distinct().Count(), Is.EqualTo(commands.Length),
                "Last Remnant-style Union orders must read as a vertical command list, not squeezed thumbnails.");

            var arena = FindNamed("Live Cinematic Battlefield").Single();
            var tray = FindNamed("Always Visible Union Command Tray").Single();
            var canvas = arena.GetComponentInParent<Canvas>();
            var arenaBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, arena);
            var trayBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, tray);
            var arenaRect = new Rect(arenaBounds.min.x, arenaBounds.min.y, arenaBounds.size.x, arenaBounds.size.y);
            var trayRect = new Rect(trayBounds.min.x, trayBounds.min.y, trayBounds.size.x, trayBounds.size.y);
            Assert.That(arenaRect.Overlaps(trayRect), Is.True,
                "The Union order menu must overlay the active arena instead of replacing it with a separate screen.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator CommandChoiceCutsIntoFullScreenUnionFormationAndShowsSideStrikeGeometry()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var focused = FindNamed("Focused Player Formation UNION_A").Single();
            var target = FindNamed("Focused Enemy Formation EU_GNAWER_PACK").Single();
            var wing = FindNamed("Wing Player Formation UNION_B").Single();
            var canvas = focused.GetComponentInParent<Canvas>();
            var canvasRect = ((RectTransform)canvas.transform).rect;
            var focusedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, focused);
            var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, target);

            Assert.That(focusedBounds.size.y, Is.GreaterThan(canvasRect.height * 0.40f),
                "The active Union must fill the command camera instead of remaining a small card.");
            Assert.That(targetBounds.size.y, Is.GreaterThan(canvasRect.height * 0.44f),
                "The targeted enemy Union must remain large and readable during command selection.");
            Assert.That(wing.gameObject.activeInHierarchy, Is.True);
            Assert.That(CountNamed("Deadlock Engagement Vector"), Is.EqualTo(1));
            Assert.That(CountNamed("Side Strike Engagement Vector"), Is.EqualTo(1));
            AssertText("SIDE STRIKE ROUTE");
            AssertText("BLIND SIDE");
            AssertText("TARGETED ENEMY UNION");

            FindNamedButton("Focus Union Command UNION_B").onClick.Invoke();
            yield return null;
            var sideStrike = FindNamedButton("Complete Union Command FORECAST_B5");
            Assert.That(sideStrike, Is.Not.Null);
            sideStrike.onClick.Invoke();
            yield return null;
            FindNamedButton("Focus Union Command UNION_B").onClick.Invoke();
            yield return null;
            AssertText("BLIND SIDE ATTACK LOCKED");
            AssertText("SIDE STRIKE WILL OPEN REAR PRESSURE");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator RoundResolutionUsesOnePerspectiveActorPairWithoutLegacyCloseups()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_B1").onClick.Invoke();
            yield return null;
            FindButton("CONFIRM ROUND").onClick.Invoke();
            yield return WaitForVisibleTurnRibbon("UNION_A", 2f);

            AssertText("UNION 1/2");
            AssertText("FIRST UNION");
            Assert.That(CountNamed("Union Turn Ribbon 071 · UNION_A"), Is.EqualTo(1));
            Assert.That(CountNamed("Union Turn Card 020"), Is.Zero);
            var ribbon = FindNamed("Union Turn Ribbon 071 · UNION_A").Single();
            var canvas = ribbon.GetComponentInParent<Canvas>();
            Canvas.ForceUpdateCanvases();
            var ribbonBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, ribbon);
            var canvasRect = ((RectTransform)canvas.transform).rect;
            var coverage = ribbonBounds.size.x * ribbonBounds.size.y /
                           (canvasRect.width * canvasRect.height);
            Assert.That(coverage, Is.LessThan(0.08f),
                "A Union marker must stay a narrow ribbon instead of covering the battlefield.");
            Assert.That(ribbon.GetComponentsInChildren<Graphic>().All(value => !value.raycastTarget), Is.True,
                "The presentation-only ribbon must not intercept battle or accessibility controls.");
            yield return WaitForText("BASIC ATTACK", 6f);

            Assert.That(CountNamedExactly("Cinematic Acting Member Closeup"), Is.Zero,
                "The active 3D path must not create a second legacy acting portrait.");
            Assert.That(CountNamedExactly("Cinematic Target Member Closeup"), Is.Zero,
                "The active 3D path must not create a second legacy target portrait.");
            Assert.That(CountSceneObjectsNamedExactly("Guild Battle Actor UNION_A_MEMBER_0"), Is.EqualTo(1));
            Assert.That(CountSceneObjectsNamedExactly("Enemy Battle Actor EU_GNAWER_PACK_MEMBER_0"), Is.EqualTo(1));
            yield return WaitForText("HP 79/120", 6f);
            AssertText("BASIC ATTACK");
            Assert.That(FindNamed("Cinematic Event Caption").Single().GetComponent<Text>().text, Is.Empty,
                "Raw event prose must not cover the contact moment.");
            Assert.That(FindNamed("Cinematic Relationship Caption").Single().GetComponent<Text>().text, Is.Empty,
                "Debug-style relationship prose must not run through the battlefield.");
            Assert.That(CountNamed("Art Growth Cut In"), Is.Zero);
            Assert.That(CountNamed("Breakthrough Cut In"), Is.Zero,
                "Growth must be deferred instead of interrupting the action closeup.");
            Assert.That(CountNamed("Art Growth Toast"), Is.Zero);
            Assert.That(CountNamed("Breakthrough Toast"), Is.Zero,
                "The 020 scheduler must aggregate growth after combat instead of firing per-event toasts.");
            Assert.That(CountNamed("Always Visible Union Command Tray"), Is.Zero);
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ResolutionRibbonsScaleWithPlaybackAndPreserveAuthoritativeUnionOrder()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_B1").onClick.Invoke();
            yield return null;

            var camera = GameObject.Find("Battle 3D Perspective Camera");
            Assert.That(camera, Is.Not.Null);
            FindButton("CONFIRM ROUND").onClick.Invoke();
            var tacticalOverview = camera.transform.position;
            var authorityHash = fake.State.Battle.FinalStateHash;
            FindButton("4×").onClick.Invoke();

            yield return WaitForVisibleTurnRibbon("UNION_A", 2f);
            AssertText("UNION 1/2");
            AssertText("FIRST UNION");
            AssertNoText("MEMBERS EXECUTE AUTOMATICALLY");
            yield return WaitForTurnRibbonToClose("UNION_A", 0.35f);
            FindButton("1×").onClick.Invoke();
            yield return WaitForUnionFocus("UNION_A", tacticalOverview, 4f);
            var firstUnionCamera = camera.transform.position;

            yield return WaitForVisibleTurnRibbon("UNION_B", 10f);
            AssertText("UNION 2/2");
            AssertText("SECOND UNION");
            Assert.That(CountNamed("Union Turn Ribbon 071 · UNION_B"), Is.EqualTo(1));
            yield return WaitForUnionFocus("UNION_B", firstUnionCamera, 4f);
            var secondUnionCamera = camera.transform.position;
            var firstUnionActor = GameObject.Find("Guild Battle Actor UNION_A_MEMBER_0")
                ?.GetComponentInChildren<SpriteRenderer>();
            var secondUnionActor = GameObject.Find("Guild Battle Actor UNION_B_MEMBER_0")
                ?.GetComponentInChildren<SpriteRenderer>();

            Assert.That(Vector3.Distance(firstUnionCamera, secondUnionCamera), Is.GreaterThan(1f),
                "Each Union chapter must move to that formation instead of reusing one blended camera view.");
            Assert.That(firstUnionActor, Is.Not.Null);
            Assert.That(secondUnionActor, Is.Not.Null);
            Assert.That(secondUnionActor.color.a, Is.GreaterThan(0.9f));
            Assert.That(firstUnionActor.color.a, Is.LessThan(0.5f),
                "The inactive Union must visibly recede while the active Union remains fully readable.");
            Assert.That(fake.ConfirmCalls, Is.EqualTo(1));
            Assert.That(fake.State.Battle.FinalStateHash, Is.EqualTo(authorityHash),
                "Turn ribbons and camera movement are presentation-only and cannot alter battle truth.");

            yield return WaitForVisibleTurnRibbon("EU_GNAWER_PACK", 10f);
            AssertText("ENEMY TURN");
            AssertText("GATE GNAWER PACK · TRAINING PROJECTION");
            AssertNoText("ENEMY RESPONSE");
            AssertNoText("ENEMY UNION IN MOTION");
            yield return WaitForUnionFocus("EU_GNAWER_PACK", secondUnionCamera, 4f);
            var world = UnityEngine.Object.FindFirstObjectByType<M2Battle3DWorld>();
            Assert.That(world, Is.Not.Null);
            Assert.That(world.FocusedUnionId, Is.EqualTo("EU_GNAWER_PACK"));
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator AuthoredCombatEffectsResolveAsTransparentRuntimeSprites()
        {
            foreach (var effect in new[] { "WEAPON_ARC", "MYSTIC_BURST", "RESTORATION_BLOOM", "GUARD_IMPACT" })
            {
                Assert.That(M1VisualAssets.TryResolveBattleVfx(effect, out var sprite, out var key), Is.True, effect);
                Assert.That(sprite, Is.Not.Null, effect);
                Assert.That(sprite.texture, Is.Not.Null, effect);
                Assert.That(key, Does.Contain("SecondDimension/Art/Battle/VFX_"));
            }
            yield return null;
        }

        [Test]
        public void AuthoredActionPosesResolveForOpeningCombatantsAndTutorialEnemy()
        {
            foreach (var memberId in new[]
                     {
                         "PROC_36344E2400DC98B6",
                         "PROC_5B14E7816E55FFB5",
                         "PROC_748DD03A23E1FEB0",
                         "PROC_F85A4CAA747BC8C6",
                         "SIGREC_MAREN_HOLT",
                         "ENEMY_GATE_GNAWER_01"
                     })
            {
                Assert.That(M1VisualAssets.TryResolveBattleActionPose(memberId, out var sprite, out var key),
                    Is.True, memberId);
                Assert.That(sprite, Is.Not.Null, memberId);
                Assert.That(key, Does.Contain("/ACTION_"), memberId);
            }
        }

        [UnityTest]
        public IEnumerator ResolutionExposesLiveSpeedSkipMotionFlashAndShakeControls()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_A1").onClick.Invoke();
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_B1").onClick.Invoke();
            yield return null;
            FindButton("CONFIRM ROUND").onClick.Invoke();
            yield return null;

            Assert.That(FindButton("1×"), Is.Not.Null);
            Assert.That(FindButton("2×"), Is.Not.Null);
            Assert.That(FindButton("4×"), Is.Not.Null);
            Assert.That(FindButton("SKIP BEAT"), Is.Not.Null);
            Assert.That(FindButton("PAUSE"), Is.Not.Null);
            Assert.That(FindButton("OPTIONS"), Is.Not.Null);
            Assert.That(FindButton("MOTION · FULL"), Is.Null);
            Assert.That(FindButton("FLASH · FULL"), Is.Null);
            Assert.That(FindButton("SHAKE · FULL"), Is.Null);
            Assert.That(FindButton("1×").GetComponent<Image>().color,
                Is.Not.EqualTo(FindButton("2×").GetComponent<Image>().color),
                "PRESENTATION-009: normal 1× playback must be the visibly selected default.");
            var authoritativeHash = fake.State.Battle.FinalStateHash;
            FindButton("2×").onClick.Invoke();
            Assert.That(FindButton("2×").GetComponent<Image>().color,
                Is.Not.EqualTo(FindButton("1×").GetComponent<Image>().color),
                "The live speed control must visibly acknowledge the selected playback rate.");
            FindButton("OPTIONS").onClick.Invoke();
            Assert.That(FindButton("RESUME"), Is.Not.Null);
            Assert.That(FindButton("MOTION · FULL"), Is.Not.Null);
            Assert.That(FindButton("FLASH · FULL"), Is.Not.Null);
            Assert.That(FindButton("SHAKE · FULL"), Is.Not.Null);
            Assert.That(FindButton("FINISH SEEN ROUND"), Is.Not.Null);
            Assert.That(FindButton("FINISH SEEN ROUND").interactable, Is.False);
            FindButton("MOTION · FULL").onClick.Invoke();
            FindButton("FLASH · FULL").onClick.Invoke();
            FindButton("SHAKE · FULL").onClick.Invoke();
            FindNamedButton("Battle Presentation Options Toggle").onClick.Invoke();
            Assert.That(FindButton("PAUSE"), Is.Not.Null);
            Assert.That(fake.ConfirmCalls, Is.EqualTo(1));
            Assert.That(fake.State.Battle.FinalStateHash, Is.EqualTo(authoritativeHash),
                "Pure presentation controls must not mutate authoritative battle truth.");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator CompleteForecastDetailShowsEveryPredictedMemberWithoutArtButtons()
        {
            var fake = new FakeM2Coordinator(M1Screen.Battle, resolved: false);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("RESUME BATTLE");
            yield return null;
            FindNamedButton("Complete Union Command FORECAST_A2").onClick.Invoke();
            yield return null;
            FindNamedButton("Focus Union Command UNION_A").onClick.Invoke();
            yield return null;

            AssertText("UNION LEADER: POWER CUT");
            AssertText("UNION MEMBER 2: POWER CUT");
            AssertText("UNION MEMBER 3: POWER CUT");
            AssertText("LEARN ·");
            AssertText("RISK ·");
            AssertText("FALLBACK ·");
            AssertNoText("SELECT INDIVIDUAL ART");
            Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(value =>
                value.name.IndexOf("Art", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ResultsExposeExactlyThreePlayerFacingCardsWithoutBackendIds()
        {
            var fake = new FakeM2Coordinator(M1Screen.BattleResults, resolved: true);
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;

            AssertText("VICTORY");
            Assert.That(FindButton("VERIFY IDENTICAL REPLAY"), Is.Null);
            Assert.That(FindButton("CLAIM XP & RETRY"), Is.Not.Null);
            Assert.That(FindButton("CLAIM XP & RETURN TO GUILD"), Is.Not.Null);
            Assert.That(CountNamedExactly("Character XP Results 021"), Is.EqualTo(1));
            Assert.That(CountNamedExactly("Use Based Art Mastery Results 021"), Is.EqualTo(1));
            Assert.That(CountNamedExactly("Guild Treasury Results 021"), Is.EqualTo(1));
            AssertText("PARTY GROWTH");
            AssertText("ART MASTERY");
            AssertText("GUILD SPOILS");
            AssertText("+252 TOTAL XP");
            AssertText("6 ADVENTURERS GREW");
            AssertText("1 LEVEL UP");
            AssertText("10 STAT POINTS GAINED");
            AssertText("XP TO SPEND +24");
            AssertText("+24 HALL GROWTH");
            AssertText("NEW LOOT");
            AssertText("FIRST-GATE SWORD");
            AssertText("OPEN ARMORY TO EQUIP");
            AssertText("BATTLE COMPLETE");
            AssertNoText("ITEM ID");
            AssertNoText("CATALOG ID");
            AssertNoText("BATTLE_ITEM_REWARD_TEST_021");
            AssertNoText("LOOT020_SKYHOME_01");
            AssertNoText("CHARACTER XP / LEVEL & STAT GAINS");
            AssertNoText("USE-BASED ART XP / MASTERY & NEW ARTS");
            AssertNoText("HALL ENHANCEMENT XP / EARNED EQUIPMENT");
            AssertNoText("CHOOSE COMPLETE UNION COMMANDS");
            AssertNoText("M3 REMAINS LOCKED");
            AssertNoText("EXPEDITION BOARD");
            Click("CLAIM XP & RETRY");
            yield return null;
            Assert.That(fake.ClaimCalls, Is.EqualTo(1));
            Assert.That(fake.RetryCalls, Is.EqualTo(1));
            AssertText("CHOOSE COMPLETE UNION COMMANDS");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator VictoryDefeatAndRetreatResultsReturnControlSafely()
        {
            foreach (var outcome in new[] { "Victory", "Defeat", "Retreat" })
            {
                var fake = new FakeM2Coordinator(M1Screen.BattleResults, resolved: true);
                fake.State.Battle.Outcome = outcome;
                var presenter = CreatePresenter(fake);
                yield return null;
                Click("VIEW LAST BATTLE RESULT");
                yield return null;

                AssertText(outcome);
                Assert.That(FindButton("CLAIM XP & RETRY"), Is.Not.Null);
                Assert.That(FindButton("CLAIM XP & RETURN TO GUILD"), Is.Not.Null);
                UnityEngine.Object.Destroy(presenter.gameObject);
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (canvas.name == "M1 Playable Proof Canvas") UnityEngine.Object.Destroy(canvas.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GuildEncounterResultUsesThePlayerFacingLanternRoadReturnLabel()
        {
            var fake = new FakeGuildCityM2Coordinator();
            var presenter = CreatePresenter(fake);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;

            Assert.That(FindButton("CLAIM & RETURN TO LANTERN ROAD"), Is.Not.Null);
            Assert.That(FindButton("CLAIM XP & RETURN TO GUILD"), Is.Null);
            Assert.That(FindButton("CLAIM XP & RETRY"), Is.Null);
            AssertNoText("CLAIM REWARDS & RETURN TO EXPEDITION");
            UnityEngine.Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator ReturnClaimsRewardBeforeOpeningGuildAndClaimFailureKeepsResult()
        {
            var failed = new FakeM2Coordinator(M1Screen.BattleResults, resolved: true, claimFails: true);
            var failedPresenter = CreatePresenter(failed);
            yield return null;
            Click("VIEW LAST BATTLE RESULT");
            yield return null;
            Click("CLAIM XP & RETURN TO GUILD");
            yield return null;

            Assert.That(failed.ClaimCalls, Is.EqualTo(1));
            AssertText("XP CLAIM BLOCKED");
            Assert.That(FindButton("CLAIM XP & RETURN TO GUILD"), Is.Not.Null,
                "A failed claim must keep the unclaimed battle result under player control.");
            UnityEngine.Object.Destroy(failedPresenter.gameObject);
            yield return null;

            var accepted = new FakeM2Coordinator(M1Screen.BattleResults, resolved: true);
            var acceptedPresenter = CreatePresenter(accepted);
            yield return null;
            Click("VIEW LAST BATTLE RESULT");
            yield return null;
            Click("CLAIM XP & RETURN TO GUILD");
            yield return null;

            Assert.That(accepted.ClaimCalls, Is.EqualTo(1));
            Assert.That(accepted.State.Battle.Reward.Claimed, Is.True);
            AssertText("RUINED ANNEX");
            AssertText("XP TO SPEND 24");
            AssertText("HALL ENHANCEMENT XP 24");
            UnityEngine.Object.Destroy(acceptedPresenter.gameObject);
        }

        private static M1FlowPresenter CreatePresenter(IM1PresentationCoordinator coordinator)
        {
            var gameObject = new GameObject("M2 Presenter Test");
            var presenter = gameObject.AddComponent<M1FlowPresenter>();
            presenter.Initialize(coordinator);
            return presenter;
        }

        private static Button FindButton(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(value => value.GetComponentsInChildren<Text>().Any(text =>
                text.text == label || text.text.StartsWith(label + "\n", StringComparison.Ordinal)));
        private static Button FindNamedButton(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, name));
        private static void Click(string label)
        {
            var button = FindButton(label);
            Assert.That(button, Is.Not.Null, label);
            button.onClick.Invoke();
        }
        private static void AssertText(string expected)
        {
            var visible = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(value => !string.IsNullOrWhiteSpace(value.text))
                .Select(value => value.text)
                .ToArray();
            Assert.That(
                visible.Any(value => value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0),
                Is.True,
                expected + "\nVISIBLE TEXT:\n" + string.Join("\n---\n", visible));
        }
        private static void AssertNoText(string forbidden) => Assert.That(
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(value => value.text != null && value.text.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0),
            Is.False, forbidden);
        private static int CountNamed(string prefix) => UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
            .Count(value => value.name.StartsWith(prefix, StringComparison.Ordinal));
        private static int CountNamedExactly(string name) => UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
            .Count(value => StringComparer.Ordinal.Equals(value.name, name));
        private static int CountSceneObjectsNamedExactly(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Count(value => StringComparer.Ordinal.Equals(value.name, name));
        private static IEnumerable<RectTransform> FindNamed(string prefix) => UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
            .Where(value => value.name.StartsWith(prefix, StringComparison.Ordinal));
        private static IEnumerator WaitForText(string expected, float timeout)
        {
            var elapsed = 0f;
            while (elapsed < timeout)
            {
                if (UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(value => value.text != null &&
                    value.text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0)) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Fail("Timed out waiting for visible text: " + expected);
        }
        private static IEnumerator WaitForVisibleTurnRibbon(string unionId, float timeout)
        {
            var elapsed = 0f;
            var expectedName = "Union Turn Ribbon 071 · " + unionId;
            while (elapsed < timeout)
            {
                var ribbon = FindNamed(expectedName).FirstOrDefault();
                var group = ribbon == null ? null : ribbon.GetComponent<CanvasGroup>();
                if (ribbon != null && ribbon.gameObject.activeInHierarchy && group != null && group.alpha >= 0.5f)
                    yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Fail("Timed out waiting for visible Union turn ribbon: " + unionId);
        }
        private static IEnumerator WaitForTurnRibbonToClose(string unionId, float timeout)
        {
            var elapsed = 0f;
            var expectedName = "Union Turn Ribbon 071 · " + unionId;
            while (elapsed < timeout)
            {
                if (!FindNamed(expectedName).Any()) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Fail("Union turn ribbon ignored the selected playback speed: " + unionId);
        }
        private static IEnumerator WaitForUnionFocus(string unionId, Vector3 previousCameraPosition, float timeout)
        {
            var elapsed = 0f;
            while (elapsed < timeout)
            {
                var world = UnityEngine.Object.FindFirstObjectByType<M2Battle3DWorld>();
                var camera = GameObject.Find("Battle 3D Perspective Camera");
                if (world != null && camera != null &&
                    StringComparer.Ordinal.Equals(world.FocusedUnionId, unionId) &&
                    Vector3.Distance(camera.transform.position, previousCameraPosition) > 0.5f) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Fail("Timed out waiting for the battle camera to focus " + unionId + ".");
        }
        private static void AssertButtonOnScreen(string label)
        {
            var button = FindButton(label);
            Assert.That(button, Is.Not.Null, label);
            Assert.That(button.GetComponentInParent<ScrollRect>(), Is.Null,
                label + " belongs to the fixed cinematic command tray, not a scrolling page.");
            var canvas = button.GetComponentInParent<Canvas>();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                canvas.transform, button.GetComponent<RectTransform>());
            var canvasRect = ((RectTransform)canvas.transform).rect;
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(canvasRect.xMin - 2f));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(canvasRect.xMax + 2f));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(canvasRect.yMin - 2f));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(canvasRect.yMax + 2f));
        }

        private class FakeM2Coordinator : IM2PresentationCoordinator
        {
            private readonly bool _startFails;
            private readonly bool _claimFails;
            private readonly bool _terminalBreakthrough;

            public FakeM2Coordinator(
                M1Screen resume,
                bool resolved,
                bool startFails = false,
                bool claimFails = false,
                bool terminalBreakthrough = false,
                string battleId = "BATTLE_TUTORIAL_UNION_FORECAST_001")
            {
                _startFails = startFails;
                _claimFails = claimFails;
                _terminalBreakthrough = terminalBreakthrough;
                State = new M1PresentationState
                {
                    HasCampaign = true,
                    HasSave = true,
                    ResumeScreen = resume,
                    SelectedModeId = "Standard",
                    GuildLevel = 1,
                    LifetimeGuildXp = 0,
                    GuildXpIntoCurrentLevel = 0,
                    GuildXpRequiredForNextLevel = 250,
                    TreasuryXp = 0,
                    HallStageIndex = 0,
                    HallStageId = "HALL_STAGE_00_RUINED_ANNEX",
                    HallStageName = "Ruined Annex",
                    HallEnhancementXp = 0,
                    Facilities = Enumerable.Range(1, 19).Select(index => new M1FacilityProgressionView
                    {
                        FacilityId = "FACILITY_" + index.ToString("00"),
                        DisplayName = "Foundation Facility " + index,
                        Level = 0,
                        TotalFacilityXp = 0
                    }).ToArray(),
                    Modes = new[] { new M1ModeView { Id = "Standard", DisplayName = "STANDARD", Summary = "Baseline" } },
                    AllSixSigned = true,
                    OpeningEquipmentLegal = true,
                    OpeningUnionsLegal = true,
                    TwoUnionsLegal = true,
                    UsedUnionCount = 2,
                    SaveReloadVerified = true,
                    CanonicalStateHash = "M1_TEST_HASH",
                    Battle = resume == M1Screen.Battle || resume == M1Screen.BattleResults
                        ? Battle(resolved, battleId)
                        : null
                };
            }

            public event Action Changed;
            public M1PresentationState State { get; }
            public int ConfirmCalls { get; private set; }
            public int StartCalls { get; private set; }
            public int ClaimCalls { get; private set; }
            public int RetryCalls { get; private set; }

            public void ExpandToTenPlayerUnions()
            {
                var unions = Enumerable.Range(1, 10).Select(index =>
                {
                    var suffix = index.ToString("00");
                    return Union("UNION_" + suffix, "Union " + suffix, false);
                }).ToArray();
                State.Battle.PlayerUnions = unions;
                State.Battle.Forecasts = unions.Select((union, index) => Forecast(
                    "FORECAST_" + (index + 1).ToString("00"), union.UnionId,
                    "CMD_BALANCED", "Attack!")).ToArray();
                State.Battle.CanConfirmRound = false;
                State.UsedUnionCount = unions.Length;
            }

            public M1CommandResult StartTutorialBattle()
            {
                StartCalls++;
                if (_startFails) return M1CommandResult.Failure("BATTLE START BLOCKED");
                State.Battle = Battle(false);
                Changed?.Invoke();
                return M1CommandResult.Success();
            }
            public M1CommandResult SelectForecast(string unionId, string forecastId)
            {
                foreach (var forecast in State.Battle.Forecasts)
                    if (forecast.UnionId == unionId) forecast.IsSelected = forecast.ForecastId == forecastId;
                foreach (var union in State.Battle.PlayerUnions)
                    if (union.UnionId == unionId) { union.IsSelected = true; union.SelectedForecastId = forecastId; }
                State.Battle.CanConfirmRound = State.Battle.PlayerUnions.All(value => value.IsSelected);
                Changed?.Invoke();
                return M1CommandResult.Success();
            }
            public M1CommandResult ConfirmBattleRound()
            {
                ConfirmCalls++;
                var events = new List<M2BattleEventView>
                {
                    new M2BattleEventView
                    {
                        Sequence = 1, Round = 1, EventType = "FORECAST_COMMITTED",
                        Text = "First Union commits Attack!", ActorUnionId = "UNION_A",
                        UnionId = "UNION_A", TargetUnionId = "EU_GNAWER_PACK", ArtId = "CMD_BALANCED"
                    },
                    new M2BattleEventView
                    {
                        Sequence = 2, Round = 1, EventType = "MARTIAL_HIT",
                        Text = "First Union Leader uses Saber Cut for 21 HP.", Amount = 21,
                        ActorUnionId = "UNION_A", ActorMemberId = "UNION_A_MEMBER_0",
                        TargetUnionId = "EU_GNAWER_PACK", TargetMemberId = "EU_GNAWER_PACK_MEMBER_0",
                        ArtId = "ART_BASIC_SABER_CUT"
                    },
                    new M2BattleEventView
                    {
                        Sequence = 3, Round = 1, EventType = "MARTIAL_HIT",
                        Text = "First Union Member 2 uses Power Cut for 19 HP.", Amount = 19,
                        ActorUnionId = "UNION_A", ActorMemberId = "UNION_A_MEMBER_1",
                        TargetUnionId = "EU_GNAWER_PACK", TargetMemberId = "EU_GNAWER_PACK_MEMBER_1",
                        ArtId = "ART_POWER_CUT"
                    },
                    new M2BattleEventView
                    {
                        Sequence = 4, Round = 1, EventType = "ART_GROWTH",
                        Text = "First Union Leader's Saber Cut mastery rises.",
                        ActorUnionId = "UNION_A", ActorMemberId = "UNION_A_MEMBER_0",
                        ArtId = "ART_BASIC_SABER_CUT", Amount = 5
                    },
                    new M2BattleEventView
                    {
                        Sequence = 5, Round = 1, EventType = "FORECAST_COMMITTED",
                        Text = "Second Union commits Use Mystic Arts!", ActorUnionId = "UNION_B",
                        UnionId = "UNION_B", TargetUnionId = "EU_GNAWER_PACK", ArtId = "CMD_MYSTIC"
                    },
                    new M2BattleEventView
                    {
                        Sequence = 6, Round = 1, EventType = "MYSTIC_HIT",
                        Text = "Second Union Leader uses Ember Bolt for 27 HP.", Amount = 27,
                        ActorUnionId = "UNION_B", ActorMemberId = "UNION_B_MEMBER_0",
                        TargetUnionId = "EU_GNAWER_PACK", TargetMemberId = "EU_GNAWER_PACK_MEMBER_0",
                        ArtId = "ART_EMBER_BOLT"
                    },
                    new M2BattleEventView
                    {
                        Sequence = 7, Round = 1, EventType = "RESTORATION",
                        Text = "Second Union Member 2 restores 12 HP.", Amount = 12,
                        ActorUnionId = "UNION_B", ActorMemberId = "UNION_B_MEMBER_1",
                        TargetUnionId = "UNION_B", TargetMemberId = "UNION_B_MEMBER_0",
                        ArtId = "ART_RESTORE"
                    },
                    new M2BattleEventView
                    {
                        Sequence = 8, Round = 1, EventType = "ENEMY_HIT",
                        Text = "Gate Gnawer 1 strikes First Union Leader for 8 HP.", Amount = 8,
                        ActorUnionId = "EU_GNAWER_PACK", ActorMemberId = "EU_GNAWER_PACK_MEMBER_0",
                        TargetUnionId = "UNION_A", TargetMemberId = "UNION_A_MEMBER_0",
                        ArtId = "ART_QUICK_CUT"
                    }
                };
                if (_terminalBreakthrough)
                {
                    events.Add(new M2BattleEventView
                    {
                        Sequence = 9, Round = 1, EventType = "BREAKTHROUGH",
                        Text = "First Union Leader turns the final strike into a breakthrough and learns Power Cut!",
                        UnionId = "UNION_A", MemberId = "UNION_A_MEMBER_0",
                        ActorUnionId = "UNION_A", ActorMemberId = "UNION_A_MEMBER_0",
                        TargetUnionId = "UNION_A", TargetMemberId = "UNION_A_MEMBER_0",
                        ArtId = "ART_POWER_CUT"
                    });
                }
                events.Add(new M2BattleEventView
                {
                    Sequence = _terminalBreakthrough ? 10 : 9,
                    Round = 1,
                    EventType = "BATTLE_RESULT",
                    Text = "Training projection defeated. Victory."
                });
                State.Battle.IsResolved = true;
                State.Battle.Outcome = "Victory";
                State.Battle.FinalStateHash = "FINAL_TEST_HASH";
                State.Battle.LastResolvedRound = 1;
                State.Battle.LastResolvedRoundEvents = events;
                State.Battle.Events = events;
                State.Battle.RecentEvents = events;
                State.Battle.Reward = BattleReward();
                Changed?.Invoke();
                return M1CommandResult.Success();
            }
            public M1CommandResult ReplayTutorialBattle() { Changed?.Invoke(); return M1CommandResult.Success("Identical replay."); }
            public M1CommandResult RetryTutorialBattle()
            {
                RetryCalls++;
                State.Battle = Battle(false);
                Changed?.Invoke();
                return M1CommandResult.Success();
            }
            public M1CommandResult ClaimBattleRewards()
            {
                ClaimCalls++;
                if (_claimFails) return M1CommandResult.Failure("XP CLAIM BLOCKED");
                var reward = State.Battle?.Reward;
                if (reward == null) return M1CommandResult.Failure("No battle reward is ready.");
                if (!reward.Claimed)
                {
                    reward.Claimed = true;
                    reward.CanClaim = false;
                    State.TreasuryXp += reward.GuildTreasuryXpAward;
                    State.HallEnhancementXp += reward.HallEnhancementXpAward;
                    State.LifetimeGuildXp = reward.GuildXpAfter;
                    State.GuildLevel = reward.GuildProjectedLevel;
                    State.GuildXpIntoCurrentLevel = reward.GuildXpAfter;
                }
                Changed?.Invoke();
                return M1CommandResult.Success("XP claimed and saved.");
            }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => M1CommandResult.Success();
            public M1CommandResult SignRecruit(string recruitId) => M1CommandResult.Success();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => M1CommandResult.Success();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => M1CommandResult.Success();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => M1CommandResult.Success();
            public M1CommandResult CompleteEquipmentReview() => M1CommandResult.Success();
            public M1CommandResult AddUnion() => M1CommandResult.Success();
            public M1CommandResult RemoveUnion(int unionIndex) => M1CommandResult.Success();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => M1CommandResult.Success();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => M1CommandResult.Success();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => M1CommandResult.Success();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => M1CommandResult.Success();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => M1CommandResult.Success();
            public M1CommandResult SaveAndReloadProof() => M1CommandResult.Success();

            private static M2BattleView Battle(
                bool resolved,
                string battleId = "BATTLE_TUTORIAL_UNION_FORECAST_001")
            {
                var a = Union("UNION_A", "First Union", false);
                var b = Union("UNION_B", "Second Union", false);
                var hallBreach = battleId != null &&
                                 battleId.IndexOf("HALL_BREACH", StringComparison.OrdinalIgnoreCase) >= 0;
                var enemy = Union(
                    "EU_GNAWER_PACK",
                    hallBreach ? "Gate Gnawer Pack" : "Gate Gnawer Pack · Training Projection",
                    true);
                return new M2BattleView
                {
                    BattleId = battleId,
                    Round = 1,
                    Outcome = resolved ? "Victory" : "In Progress",
                    Objective = hallBreach
                        ? "Protect the Guild Hall while Kael contains the breach."
                        : "Defeat the training projection — or withdraw safely.",
                    PlayerUnions = new[] { a, b },
                    EnemyUnions = new[] { enemy },
                    Forecasts = resolved ? Array.Empty<M2ForecastView>() : new[]
                    {
                        Forecast("FORECAST_A1", "UNION_A", "CMD_BALANCED", "Attack!"),
                        Forecast("FORECAST_A2", "UNION_A", "CMD_ALL_OUT", "Attack Using Combat Arts!"),
                        Forecast("FORECAST_A3", "UNION_A", "CMD_GUARD", "Hold the Line!"),
                        Forecast("FORECAST_A4", "UNION_A", "CMD_AP_RECOVERY", "Recover AP!"),
                        Forecast("FORECAST_B1", "UNION_B", "CMD_MYSTIC", "Use Mystic Arts!"),
                        Forecast("FORECAST_B2", "UNION_B", "CMD_HEAL", "Heal the Wounded!"),
                        Forecast("FORECAST_B3", "UNION_B", "CMD_SUPPORT", "Restore Formation!"),
                        Forecast("FORECAST_B4", "UNION_B", "CMD_RETREAT", "Get Out of Here!"),
                        Forecast("FORECAST_B5", "UNION_B", "CMD_FLANK", "Side Strike!")
                    },
                    Events = resolved
                        ? new[]
                        {
                            new M2BattleEventView
                            {
                                Sequence = 1, Round = 1, EventType = "ART_GROWTH",
                                Text = "First Union Leader grows Saber Cut through meaningful use.",
                                MemberId = "UNION_A_MEMBER_0", ActorMemberId = "UNION_A_MEMBER_0",
                                ArtId = "ART_BASIC_SABER_CUT", Amount = 5
                            },
                            new M2BattleEventView
                            {
                                Sequence = 2, Round = 1, EventType = "BREAKTHROUGH",
                                Text = "First Union Leader learns Power Cut!",
                                MemberId = "UNION_A_MEMBER_0", ActorMemberId = "UNION_A_MEMBER_0",
                                ArtId = "ART_POWER_CUT"
                            }
                        }
                        : new[] { new M2BattleEventView { Sequence = 0, Round = 0, EventType = "BATTLE_START", Text = "Ready." } },
                    RecentEvents = new[] { new M2BattleEventView { Sequence = 0, Round = 0, EventType = "BATTLE_START", Text = "Ready." } },
                    LastResolvedRound = resolved ? 1 : 0,
                    IsResolved = resolved,
                    TutorialBreakthroughOccurred = resolved,
                    TutorialBreakthroughSummary = resolved ? "Guaranteed tutorial breakthrough achieved through meaningful combat use." : "Eligible.",
                    Reward = resolved ? BattleReward() : null,
                    StateHash = "CURRENT_TEST_HASH",
                    FinalStateHash = resolved ? "FINAL_TEST_HASH" : string.Empty
                };
            }

            private static M2BattleRewardView BattleReward()
            {
                var members = new[]
                {
                    "First Union Leader", "First Union Member 2", "First Union Member 3",
                    "Second Union Leader", "Second Union Member 2", "Second Union Member 3"
                };
                return new M2BattleRewardView
                {
                    RewardId = "REWARD_TEST_021",
                    RewardRulesVersion = "M2_PROGRESSION_021",
                    Outcome = "Victory",
                    Claimed = false,
                    CanClaim = true,
                    BasePersonalXpPerMember = 42,
                    BaseGuildTreasuryXp = 24,
                    GuildTreasuryXpAward = 24,
                    HallEnhancementXpAward = 24,
                    GuildXpBefore = 0,
                    GuildXpAfter = 24,
                    GuildPreviousLevel = 1,
                    GuildProjectedLevel = 1,
                    EquipmentRewardInstanceId = "BATTLE_ITEM_REWARD_TEST_021",
                    EquipmentRewardDefinitionId = "LOOT020_SKYHOME_01",
                    EquipmentRewardDisplayName = "First-Gate Sword",
                    EquipmentRewardQualityId = "QUALITY_STANDARD",
                    EquipmentRewardValidSlotIds = new[] { "MAIN_HAND" },
                    MemberRewards = members.Select((name, index) => new M2BattleMemberRewardView
                    {
                        MemberId = index < 3 ? "UNION_A_MEMBER_" + index : "UNION_B_MEMBER_" + (index - 3),
                        DisplayName = name,
                        PersonalXp = 42,
                        PreviousLevel = 1,
                        ProjectedLevel = index == 0 ? 2 : 1,
                        LevelsGained = index == 0 ? 1 : 0,
                        MaximumHpGain = index == 0 ? 6 : 0,
                        MaximumMpGain = index == 0 ? 2 : 0,
                        StrengthGain = index == 0 ? 1 : 0,
                        DefenseGain = index == 0 ? 1 : 0
                    }).ToArray()
                };
            }

            private static M2BattleUnionView Union(string id, string name, bool enemy)
            {
                var count = enemy ? 2 : 3;
                var members = Enumerable.Range(0, count).Select(index => new M2BattleMemberView
                {
                    MemberId = id + "_MEMBER_" + index,
                    DisplayName = enemy ? "Gate Gnawer " + (index + 1) : name + (index == 0 ? " Leader" : " Member " + (index + 1)),
                    ClassName = enemy ? "Formation Nuisance" : index == 0 ? "Guardian" : index == 1 ? "Warrior" : "Mage",
                    ClassSymbol = enemy ? "◆" : index == 0 ? "◈" : index == 1 ? "⚔" : "✦",
                    RaceId = enemy ? "ENEMY_GATE_GNAWER" : "HUMAN",
                    VisualSeed = "TEST_" + id + "_" + index,
                    CurrentHp = 100, MaximumHp = 120, CurrentMp = 18, MaximumMp = 20
                }).ToArray();
                return new M2BattleUnionView
                {
                    UnionId = id,
                    DisplayName = name,
                    Side = enemy ? "Enemy" : "Player",
                    LeaderMemberId = members[0].MemberId,
                    Formation = "Shield Wall",
                    FormationBenefitActive = true,
                    FormationStatus = "Shield Wall benefit active",
                    CurrentAp = 18,
                    MaximumAp = 18,
                    Cohesion = 85,
                    FormationConditionPercent = 100,
                    Engagement = "Open",
                    CanAct = true,
                    Members = members
                };
            }

            private static M2ForecastView Forecast(string id, string union, string commandId, string name) => new M2ForecastView
            {
                ForecastId = id,
                UnionId = union,
                CommandId = commandId,
                CommandName = name,
                Phrase = "Keep the line moving!",
                TacticalIntent = "Balanced",
                TargetId = "EU_GNAWER_PACK",
                TargetName = "Gate Gnawer Pack",
                SharedApCost = 2,
                CombinedMpCost = 0,
                ExpectedEffect = "about 31 HP damage, Cohesion -3.",
                Risk = "Enemy retaliation can pressure the line.",
                LearningOpportunity = "Meaningful use advances proficiency.",
                FallbackBehavior = "Retarget the next legal enemy; use a zero-cost basic action if resources change.",
                MemberActions = Enumerable.Range(0, 3).Select(index =>
                    new M2PredictedActionView
                    {
                        ActorMemberId = union + "_MEMBER_" + index,
                        ActorName = index == 0 ? "Union Leader" : "Union Member " + (index + 1),
                        ArtId = commandId == "CMD_MYSTIC" ? "ART_EMBER_BOLT" : "ART_POWER_CUT",
                        ArtName = commandId == "CMD_MYSTIC" ? "Ember Bolt" : "Power Cut",
                        Discipline = commandId == "CMD_MYSTIC" ? "Mystic" : "Martial",
                        ActionKind = commandId == "CMD_MYSTIC" ? "Mystic" : "Martial",
                        TargetUnionId = "EU_GNAWER_PACK", TargetMemberId = "EU_GNAWER_PACK_MEMBER_0",
                        TargetName = "Gate Gnawer 1", PersonalMpCost = commandId == "CMD_MYSTIC" ? 3 : 0,
                        PredictedGrowth = 5, Prediction = "Deal about 31 HP; Cohesion -3.",
                        BreakthroughOpportunity = id == "FORECAST_A2", BreakthroughTargetArtName = "Power Cut"
                    }).ToArray()
            };
        }

        private sealed class FakeGuildCityM2Coordinator : FakeM2Coordinator,
            IGuildCityPresentationCoordinator017D
        {
            public FakeGuildCityM2Coordinator()
                : base(M1Screen.BattleResults, resolved: true)
            {
                GuildCity017D = new GuildCityPresentationState017D
                {
                    IsAvailable = true,
                    HasPendingEncounter = true,
                    Expedition = new GuildCityExpeditionView017D
                    {
                        ExpeditionId = "EXPEDITION_RESULT_LABEL_TEST_071",
                        BoardId = "GUILDCITY_FIRST_STORY_071",
                        CurrentNodeId = "N13",
                        Status = "ACTIVE"
                    }
                };
            }

            public GuildCityPresentationState017D GuildCity017D { get; }
            public M1CommandResult PlaceGuildCityBuilding017D(string plotId, string buildingId) =>
                M1CommandResult.Success();
            public M1CommandResult UpgradeGuildCityBuilding017D(string plotId) => M1CommandResult.Success();
            public M1CommandResult AssignGuildCityStaff017D(string plotId, string recruitId) =>
                M1CommandResult.Success();
            public M1CommandResult RecallGuildCityStaff017D(string recruitId) => M1CommandResult.Success();
            public M1CommandResult SetGuildCityAssignment017D(string recruitId, string assignmentKind) =>
                M1CommandResult.Success();
            public M1CommandResult ArchiveGuildCityMember017D(string recruitId, bool confirmed) =>
                M1CommandResult.Success();
            public M1CommandResult CommitGuildCityApplicantBoard017D() => M1CommandResult.Success();
            public M1CommandResult RefreshGuildCityApplicantBoard017D() => M1CommandResult.Success();
            public M1CommandResult SignGuildCityApplicant017D(string recruitId) => M1CommandResult.Success();
            public M1CommandResult DeclineGuildCityApplicant017D(string recruitId) => M1CommandResult.Success();
            public M1CommandResult AcceptGuildCityContract017D(string contractId) => M1CommandResult.Success();
            public M1CommandResult StartGuildCityExpedition017D() => M1CommandResult.Success();
            public M1CommandResult MoveGuildCityExpedition017D(string destinationNodeId) =>
                M1CommandResult.Success();
            public M1CommandResult ResolveGuildCityCheck017D(
                string eventId, string actorRecruitId, string assistantRecruitId, int modifier) =>
                M1CommandResult.Success();
            public M1CommandResult DiscoverGateworksMaintenancePassage066() => M1CommandResult.Success();
            public M1CommandResult CommitGuildCityEncounter017D(string encounterId) => M1CommandResult.Success();
            public M1CommandResult StartCommittedGuildCityBattle017D() => M1CommandResult.Success();
            public M1CommandResult FinalizeGuildCityOperation017D() => M1CommandResult.Success();
            public M1CommandResult AddGuildCityRelationshipMemory017D(
                string firstRecruitId, string secondRecruitId, string sourceId,
                string summary, int strength, string sceneId) => M1CommandResult.Success();
            public M1CommandResult ViewGuildCityRelationshipScene017D(string sceneId) =>
                M1CommandResult.Success();
        }
    }
}
