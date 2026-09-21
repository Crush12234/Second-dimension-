using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StringComparer = System.StringComparer;
using NUnit.Framework;
using SecondDimension.Gameplay.M2;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2BattleTacticalPresentation074PlayModeTests
    {
        [TestCase(1280f, 800f)]
        [TestCase(1920f, 1080f)]
        public void FirstBattleCoachIsACompactDockBelowTheTacticalStatusAtSupportedResolutions(
            float width,
            float height)
        {
            var coachHeight = (M2BattleExperienceController072.FirstBattleCoachMaxY076 -
                               M2BattleExperienceController072.FirstBattleCoachMinY076) * height;
            var statusGap = (M2BattleDioramaView072.TacticalHeaderMinY076 -
                             M2BattleExperienceController072.FirstBattleCoachMaxY076) * height;
            var focusedHpGap = (M2BattleDioramaView072.FocusedUnionHpRibbonMinY076 -
                                M2BattleExperienceController072.FirstBattleCoachMaxY076) * height;

            Assert.That(width, Is.GreaterThan(0f));
            Assert.That(coachHeight, Is.LessThanOrEqualTo(height * 0.13f),
                "The lesson may occupy only a compact dock, never a formation-obscuring modal.");
            Assert.That(statusGap, Is.GreaterThanOrEqualTo(6f),
                "The coach must leave a physical gap below active Union/AP and enemy-intent rows.");
            Assert.That(focusedHpGap, Is.GreaterThanOrEqualTo(6f),
                "The one-time coach must leave both exact focused-Union HP rails fully readable.");
        }

        [UnityTest]
        public IEnumerator CommandTrayKeepsFiveDistinctReadableOrdersAtSupportedResolutions()
        {
            foreach (var size in new[] { new Vector2(1280f, 800f), new Vector2(1920f, 1080f) })
            {
            var host = Host("Combat HUD Test Host 074 " + size.x, size);
            var owner = new GameObject("Combat HUD Test Owner 074 " + size.x);
            try
            {
            var hud = owner.AddComponent<M2BattleCommandHud072>();
            hud.Initialize(host, _ => { }, (_, __) => { }, (_, __) => { }, () => { });
            var battle = Battle(6, 6);
            hud.Refresh(battle, "ALLY_1");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var tray = GameObject.Find("Battle Command HUD 072").GetComponent<RectTransform>();
            Assert.That(tray.anchorMax.y, Is.LessThanOrEqualTo(M2BattleCommandHud072.MaximumTrayAnchorY));
            Assert.That(tray.anchorMax.y, Is.LessThanOrEqualTo(0.25f),
                "The command tray must leave at least three quarters of the battlefield visible.");
            Assert.That(tray.rect.height, Is.LessThanOrEqualTo(size.y * 0.25f),
                "The command tray must remain within roughly one quarter of either supported screen.");

            var orders = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Complete Forecast Order ") &&
                                value.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(orders.Length, Is.EqualTo(5), "Only five contextual orders may occupy the tray.");
            var labels = orders.Select(value => value.GetComponentInChildren<Text>()).ToArray();
            foreach (var label in labels)
            {
                Assert.That(label.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(M2BattleCommandHud072.MinimumCriticalOrderFontSize074));
                Assert.That(label.text, Does.Not.Contain("GATE-EATER").And.Not.Contain("HINGE-EATER"),
                    "Targets belong in the readable forecast pane, not repeated across five narrow cards.");
                Assert.That(label.text, Does.Not.Contain("...").And.Not.Contain("…"));
                var lines092 = label.text.Split('\n');
                var compact092 = lines092[0] == "ARTS" || lines092[0] == "MYSTIC" || lines092[0] == "GUARD";
                Assert.That(lines092.Length, Is.EqualTo(compact092 ? 3 : 4),
                    "Keep title, cost and effect; show a separate semantic role only when it adds information.");
                Assert.That(lines092.Skip(1), Does.Not.Contain(lines092[0]),
                    "The presentation pass deliberately removes repeated ARTS/ARTS and GUARD/GUARD lines.");
                Assert.That(lines092[lines092.Length - 2], Does.StartWith("AP "));
                Assert.That(lines092.Last(), Is.Not.Empty);
                Assert.That(label.text.Split('\n').All(value =>
                    value.Length <= 18 && !value.EndsWith("-") && !value.EndsWith("—")), Is.True,
                    "No order-card line may rely on Unity splitting a player-facing word mid-token.");
            }
            Assert.That(labels.Select(value => value.text.Split('\n')[0]).Distinct().Count(), Is.EqualTo(5));
            Assert.That(labels.Select(value => value.text.Split('\n'))
                .Select(lines => lines.Length == 4 ? lines[1] : lines[0]).Distinct().Count(), Is.EqualTo(5),
                "The five orders need distinct tactical roles, not target-name clones.");
            Assert.That(orders.Select(value => value.colors.normalColor)
                .Select(value => Mathf.RoundToInt(value.r * 100f) + ":" +
                                 Mathf.RoundToInt(value.g * 100f) + ":" +
                                 Mathf.RoundToInt(value.b * 100f))
                .Distinct().Count(), Is.EqualTo(5),
                "Semantic order categories must also remain visually distinct.");

            var activeStats = GameObject.Find("Active Union HP AP Members 074").GetComponent<Text>();
            Assert.That(activeStats.text, Does.Contain("HP").And.Contain("AP").And.Contain("UP"));
            Assert.That(activeStats.resizeTextMinSize, Is.GreaterThanOrEqualTo(20));
            var forecastPlan = GameObject.Find("Predicted Member Arts Preview 072").GetComponent<Text>();
            Assert.That(forecastPlan.text,
                Does.Contain("DAEVEN  •  WEIGHTED SWING")
                    .And.Contain("SERA  •  SHIELD RUSH")
                    .And.Contain("TALA  •  EMBER BOLT"),
                "Every participating member Art must be named before an order is committed.");
            Assert.That(forecastPlan.resizeTextMinSize, Is.GreaterThanOrEqualTo(14));
            Assert.That(GameObject.Find("Forecast Preview Heading 072").GetComponent<Text>().text,
                Does.StartWith("FORECAST"));
            Assert.That(GameObject.Find("Forecast Target Cost Effect 072").GetComponent<Text>().text,
                Does.Contain("THE GATE-EATER").And.Contain("AP 12 → 11").And.Contain("MP 0"),
                "Forecast hierarchy must keep the order state in its heading and the target/cost consequence below it.");

            var chips = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Union Focus Chip ") &&
                                value.gameObject.activeInHierarchy)
                .OrderBy(value => value.name)
                .ToArray();
            Assert.That(chips.Length, Is.EqualTo(6));
            var distinctRows = chips.Select(value =>
                    Mathf.RoundToInt(value.GetComponent<RectTransform>().anchorMin.y * 1000f))
                .Distinct().Count();
            Assert.That(distinctRows, Is.EqualTo(1),
                "Six Unions must use one full-width selector row, not two undersized rows.");
            foreach (var chip in chips)
            {
                var rect = chip.GetComponent<RectTransform>().rect;
                var label = chip.GetComponentInChildren<Text>().text;
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(M2BattleCommandHud072.MinimumUnionSelectorWidth074),
                    chip.name + " must keep its Union name readable at 1280x800.");
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(M2BattleCommandHud072.MinimumUnionSelectorHeight074),
                    chip.name + " must remain a touch/controller-sized target at 1280x800.");
                Assert.That(label, Does.Not.Contain("..."));
                Assert.That(label, Does.Not.Contain("…"));
                Assert.That(label, Does.Contain("ORDER NEEDED"));
                Assert.That(chip.navigation.mode, Is.Not.EqualTo(Navigation.Mode.None));
            }
            Assert.That(chips[0].GetComponentInChildren<Text>().text,
                Is.EqualTo("01  OPENING\nORDER NEEDED"));

            var navigator = GameObject.Find("Union Order Navigator 074").GetComponent<RectTransform>();
            Assert.That(navigator.rect.width, Is.GreaterThan(size.x * 0.93f));
            Assert.That(navigator.rect.height, Is.GreaterThanOrEqualTo(50f));

            var readyButton = GameObject.Find("Confirm Complete Union Forecasts 072").GetComponent<Button>();
            var readyLabel = readyButton.GetComponentInChildren<Text>();
            Assert.That(readyButton.interactable, Is.False);
            Assert.That(readyLabel.text, Does.Contain("SET ORDERS").And.Contain("LOCKED"));
            Assert.That(readyLabel.color.grayscale - readyButton.colors.disabledColor.grayscale,
                Is.GreaterThan(0.55f),
                "Disabled Ready must remain explanatory and high-contrast, never black text on a dark control.");

            orders[0].onClick.Invoke();
            yield return null;
            Assert.That(orders[0].GetComponentInChildren<Text>().text, Does.StartWith("✓ "),
                "A selected complete order needs an explicit readied mark, not color alone.");

            foreach (var union in battle.PlayerUnions)
            {
                union.IsSelected = true;
                union.SelectedForecastId = union.UnionId == "ALLY_1" ? "FORECAST_1" : "ORDERED_" + union.UnionId;
            }
            battle.Forecasts[0].IsSelected = true;
            battle.CanConfirmRound = true;
            hud.Refresh(battle, "ALLY_1");
            yield return null;
            Assert.That(GameObject.Find("Union Focus Chip 1 072").GetComponentInChildren<Text>().text,
                Does.Contain("ORDER READY"));
            Assert.That(readyButton.interactable, Is.True);
            Assert.That(readyLabel.text, Does.Contain("EXECUTE ROUND").And.Contain("ALL ORDERS READY"));

            }
            finally
            {
                // Do not leak a failed fixture into later GameObject.Find checks.
                Object.Destroy(owner);
                Object.Destroy(host.gameObject);
            }
            yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ForecastPreviewNamesAllyAndOwnUnionTargetsExplicitly080()
        {
            var host = Host("Friendly Union Target HUD Test Host 080", new Vector2(1280f, 800f));
            var owner = new GameObject("Friendly Union Target HUD Test Owner 080");
            var hud = owner.AddComponent<M2BattleCommandHud072>();
            hud.Initialize(host, _ => { }, (_, __) => { }, (_, __) => { }, () => { });
            var battle = Battle(2, 1);
            var forecast = battle.Forecasts[0];
            forecast.CommandId = "CMD_HEAL";
            forecast.CommandName = "Restore Allies!";
            forecast.TargetId = "ALLY_2";
            forecast.TargetName = "Opening Union 2";
            forecast.ExpectedEffect = "up to 28 HP restored";

            hud.Refresh(battle, "ALLY_1");
            yield return null;
            Canvas.ForceUpdateCanvases();
            var summary = GameObject.Find("Forecast Target Cost Effect 072").GetComponent<Text>();
            Assert.That(summary.text,
                Does.StartWith("ALLY UNION  •  OPENING UNION 2").And.Contain("UP TO 28 HP"));

            forecast.CommandId = "CMD_SUPPORT";
            forecast.CommandName = "Form Up!";
            forecast.TargetId = "ALLY_1";
            forecast.TargetName = "Opening Union 1";
            forecast.ExpectedEffect = "formation +12%";
            hud.Refresh(battle, "ALLY_1");
            yield return null;
            Assert.That(summary.text, Does.StartWith("OWN UNION  •  OPENING UNION 1"));

            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FocusedSixMemberTelemetryUsesPlainLanguageAndFitsAtSupportedResolutions()
        {
            foreach (var size in new[] { new Vector2(1280f, 800f), new Vector2(1920f, 1080f) })
            {
                var host = Host("Six Member Telemetry Host 078 " + size.x, size);
                var owner = new GameObject("Six Member Telemetry Owner 078 " + size.x);
                var hud = owner.AddComponent<M2BattleCommandHud072>();
                hud.Initialize(host, _ => { }, (_, __) => { }, (_, __) => { }, () => { });
                hud.Refresh(TenSixMemberBattle078(), "ALLY_1");
                yield return null;
                Canvas.ForceUpdateCanvases();

                var rail = GameObject.Find("Focused Union Rail 072").GetComponent<RectTransform>();
                var states = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                    .Select(index => GameObject.Find("Active Union Member State " + index + " 078")
                        .GetComponent<Text>())
                    .ToArray();
                var progressValues = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                    .Select(index => GameObject.Find(
                            "Active Union Member Next Art Progress Value " + index + " 078")
                        .GetComponent<Text>())
                    .ToArray();
                Assert.That(states.Length, Is.EqualTo(6));
                Assert.That(states.All(value => value.gameObject.activeInHierarchy), Is.True);
                Assert.That(progressValues.All(value => value.gameObject.activeInHierarchy), Is.True);
                Assert.That(states[0].text, Is.EqualTo("ASTER • HP71/120"));
                Assert.That(progressValues[0].text, Is.EqualTo("NEXT ART  •  34 / 80"));
                Assert.That(states[4].text, Is.EqualTo("VALERIA • HP999/999"));
                Assert.That(progressValues[4].text, Is.EqualTo("NEXT ART  •  38 / 80"));
                Assert.That(states[5].text, Is.EqualTo("VALERIA • HP9999/9999"));
                Assert.That(progressValues[5].text, Is.EqualTo("NEXT ART  •  39 / 80"));

                foreach (var state in states.Concat(progressValues))
                {
                    Assert.That(state.resizeTextMinSize, Is.GreaterThanOrEqualTo(10));
                    Assert.That(state.text, Does.Not.Contain("...").And.Not.Contain("…"));
                    var settings = state.GetGenerationSettings(state.rectTransform.rect.size);
                    Assert.That(state.cachedTextGenerator.Populate(state.text, settings), Is.True);
                    Assert.That(state.cachedTextGenerator.lines.Count, Is.EqualTo(1),
                        state.name + " must own one explicit, unclipped telemetry line.");
                    Assert.That(state.cachedTextGenerator.characterCountVisible,
                        Is.GreaterThanOrEqualTo(state.text.Length),
                        state.name + " clipped player-facing telemetry at " + size.x + "x" + size.y + ".");
                    Assert.That(state.rectTransform.rect.height, Is.GreaterThanOrEqualTo(14f),
                        state.name + " must own a full minimum line box without relying on truncation tolerance.");
                    var preferredHeight = state.cachedTextGeneratorForLayout
                        .GetPreferredHeight(state.text, settings) / state.pixelsPerUnit;
                    Assert.That(preferredHeight, Is.LessThanOrEqualTo(state.rectTransform.rect.height + 0.1f),
                        state.name + " preferred height " + preferredHeight +
                        " exceeded its owned rect " + state.rectTransform.rect.height +
                        " at " + size.x + "x" + size.y + ".");
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rail, state.rectTransform);
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(rail.rect.xMin - 1f));
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(rail.rect.xMax + 1f));
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(rail.rect.yMin - 1f));
                    Assert.That(bounds.max.y, Is.LessThanOrEqualTo(rail.rect.yMax + 1f));
                }

                var progressRails = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                    .Select(index => GameObject.Find(
                        "Active Union Member Next Art Progress Rail " + index + " 078")
                        .GetComponent<RectTransform>())
                    .ToArray();
                Assert.That(progressRails.All(value => value.rect.height >= 4f), Is.True,
                    "Every exact next-Art fraction must retain a visible rail at both shipping resolutions.");
                for (var index = 0; index < progressRails.Length; index++)
                {
                    var progressBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                        rail,
                        progressValues[index].rectTransform);
                    var barBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                        rail,
                        progressRails[index]);
                    Assert.That(barBounds.max.y, Is.LessThanOrEqualTo(progressBounds.min.y + 0.1f),
                        progressRails[index].name +
                        " must own a dedicated band below the readable next-Art copy.");
                }

                Object.Destroy(owner);
                Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CommandTrayPagesTenSixMemberUnionsWithoutControllerOrOrderDeadlocks()
        {
            var host = Host("Ten Union Combat HUD Test Host 078", new Vector2(1280f, 800f));
            var owner = new GameObject("Ten Union Combat HUD Test Owner 078");
            var createdEventSystem = EventSystem.current == null
                ? new GameObject("Ten Union Combat HUD Event System 078", typeof(EventSystem))
                : null;
            var eventSystem = EventSystem.current;
            var previousSelection = eventSystem?.currentSelectedGameObject;
            var focusedUnionId = string.Empty;
            var selectedUnionId = string.Empty;
            var selectedForecastId = string.Empty;
            var hud = owner.AddComponent<M2BattleCommandHud072>();
            hud.Initialize(
                host,
                value => focusedUnionId = value,
                (_, __) => { },
                (unionId, forecastId) =>
                {
                    selectedUnionId = unionId;
                    selectedForecastId = forecastId;
                },
                () => { });
            var battle = TenSixMemberBattle078();
            hud.Refresh(battle, "ALLY_1");
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(M2BattleCommandHud072.MaximumSupportedPlayerUnions078, Is.EqualTo(10));
            var firstPage = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Union Focus Chip ") &&
                                value.gameObject.activeInHierarchy)
                .OrderBy(value => value.name)
                .ToArray();
            Assert.That(firstPage.Length, Is.EqualTo(M2BattleCommandHud072.UnionSelectorPageSize078));
            Assert.That(firstPage[0].GetComponentInChildren<Text>().text, Does.StartWith("01"));
            Assert.That(firstPage[5].GetComponentInChildren<Text>().text, Does.StartWith("06"));

            var memberStates = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                .Select(index => GameObject.Find("Active Union Member State " + index + " 078")
                    .GetComponent<Text>())
                .ToArray();
            var memberProgressValues = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                .Select(index => GameObject.Find(
                        "Active Union Member Next Art Progress Value " + index + " 078")
                    .GetComponent<Text>())
                .ToArray();
            Assert.That(memberStates.All(value => value.gameObject.activeInHierarchy), Is.True);
            Assert.That(memberProgressValues.All(value => value.gameObject.activeInHierarchy), Is.True);
            Assert.That(memberStates.All(value => value.text.Contains("HP")) &&
                        memberProgressValues.All(value => value.text.StartsWith("NEXT ART  •  ")), Is.True,
                "Every deployed member needs a plain-language identity/HP line and a separate next-Art line.");
            Assert.That(memberStates.All(value => value.resizeTextMinSize >= 10), Is.True);
            Assert.That(memberStates.Select(value => value.text).Distinct().Count(), Is.EqualTo(6));
            Assert.That(memberStates.Any(value => value.text.Contains("HP999/999")), Is.True);
            Assert.That(memberStates.Any(value => value.text.Contains("HP9999/9999")), Is.True,
                "Generated-fit coverage must include both three- and four-digit breakthrough labels.");
            Assert.That(memberStates[0].text, Is.EqualTo("ASTER • HP71/120"));
            Assert.That(memberProgressValues[0].text,
                Is.EqualTo("NEXT ART  •  34 / 80"),
                "The focused rail must keep exact next-Art progress visible during command play.");
            Assert.That(memberStates.Concat(memberProgressValues).All(value =>
                    !value.text.Contains("...") && !value.text.Contains("…")), Is.True,
                "The six-member rail must never fall back to cryptic one-letter truncation.");
            var progressRails = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                .Select(index => GameObject.Find(
                    "Active Union Member Next Art Progress Rail " + index + " 078").GetComponent<RectTransform>())
                .ToArray();
            var progressFills = Enumerable.Range(1, M2BattleCommandHud072.MaximumFocusedMemberStates078)
                .Select(index => GameObject.Find(
                    "Active Union Member Next Art Progress Fill " + index + " 078").GetComponent<RectTransform>())
                .ToArray();
            Assert.That(progressRails.All(value => value.gameObject.activeInHierarchy), Is.True);
            Assert.That(progressRails.All(value => value.rect.height >= 3f), Is.True,
                "Next-Art rails must remain visible without taking another command-row.");
            Assert.That(progressFills[0].rect.width,
                Is.EqualTo(progressRails[0].rect.width * (34f / 80f)).Within(1f),
                "The compact rail must visualize the exact authority-projected discovery ratio.");
            Assert.That(M2BattleCommandHud072.FocusedMemberNextArtProgressFraction078(
                    battle.PlayerUnions[0].Members[0].NextSkillProgress),
                Is.EqualTo("34/80"));
            var focusedEyebrow = GameObject.Find("Active Union Eyebrow 074").GetComponent<Text>();
            var focusedHeading = GameObject.Find("Active Union Heading 074").GetComponent<Text>();
            var focusedStats = GameObject.Find("Active Union HP AP Members 074").GetComponent<Text>();
            Assert.That(focusedEyebrow.text,
                Does.Contain("SHIELD WALL").And.Contain("APPROACH"),
                "Formation and engagement must remain visible after reclaiming the old duplicate state row.");
            Assert.That(focusedHeading.text, Is.EqualTo("01/10 OPENING"),
                "The heading should remove the redundant 'Union 1' suffix while keeping ordinal and identity.");
            Assert.That(focusedEyebrow.resizeTextMinSize, Is.GreaterThanOrEqualTo(10),
                "The tactical eyebrow must not solve wrapping with illegibly tiny type.");
            Assert.That(focusedEyebrow.resizeTextMaxSize, Is.EqualTo(12),
                "The tactical eyebrow should use a deliberate secondary 12px hierarchy at 1280x800.");
            Assert.That(focusedHeading.resizeTextMaxSize, Is.EqualTo(12),
                "The focused Union heading should share the readable single-line 12px hierarchy.");
            Assert.That(focusedEyebrow.rectTransform.rect.height, Is.GreaterThanOrEqualTo(16f),
                "The tactical eyebrow must own an honest one-line header band at 1280x800.");
            Assert.That(focusedHeading.rectTransform.anchorMax.x,
                Is.LessThanOrEqualTo(focusedEyebrow.rectTransform.anchorMin.x),
                "Focused Union name/ordinal and tactical context must own separate columns.");
            Assert.That(focusedHeading.rectTransform.anchorMax.x -
                        focusedHeading.rectTransform.anchorMin.x,
                Is.GreaterThanOrEqualTo(0.40f),
                "The active Union name column must fit a complete authored name such as Zorin's Vanguard.");
            Assert.That(focusedStats.rectTransform.anchorMax.y,
                Is.LessThanOrEqualTo(focusedHeading.rectTransform.anchorMin.y),
                "The full-width HP/AP row must remain below, rather than overlap, the header columns.");
            Assert.That(GameObject.Find("Active Union Formation Engagement 074")
                    .GetComponent<Text>().text,
                Is.Empty,
                "The compatibility state object must not render duplicate formation copy over member telemetry.");
            var focusedRailReadouts = new[]
                {
                    focusedEyebrow,
                    focusedHeading,
                    focusedStats
                }
                .Concat(memberStates)
                .Concat(memberProgressValues)
                .ToArray();
            foreach (var readout in focusedRailReadouts)
            {
                var settings = readout.GetGenerationSettings(readout.rectTransform.rect.size);
                Assert.That(readout.cachedTextGenerator.Populate(readout.text, settings), Is.True);
                Assert.That(readout.cachedTextGenerator.lines.Count, Is.EqualTo(1),
                    readout.name + " must render only its authored telemetry lines at 1280x800.");
                var visibleGlyphFloor = readout.text.Count(character => character != '\n');
                Assert.That(readout.cachedTextGenerator.characterCountVisible,
                    Is.GreaterThanOrEqualTo(visibleGlyphFloor),
                    readout.name + " must generate every character rather than vertically truncating it.");
                var preferredHeight = readout.cachedTextGeneratorForLayout
                    .GetPreferredHeight(readout.text, settings) / readout.pixelsPerUnit;
                Assert.That(preferredHeight,
                    Is.LessThanOrEqualTo(readout.rectTransform.rect.height + 1f),
                    readout.name + " must own enough height for its best-fit line.");
            }
            var sampleMember = new M2BattleMemberView
            {
                DisplayName = "Aster",
                CurrentHp = 90,
                MaximumHp = 120
            };
            var powerCut = M2BattleCommandHud072.FocusedMemberStateLabel078(
                sampleMember,
                new M2PredictedActionView { ArtName = "Power Cut" });
            var powerGuard = M2BattleCommandHud072.FocusedMemberStateLabel078(
                sampleMember,
                new M2PredictedActionView { ArtName = "Power Guard" });
            Assert.That(powerCut, Does.Contain("POWER CUT"));
            Assert.That(powerGuard, Is.Not.EqualTo(powerCut).And.Contain("POWER G"),
                "Compact Art states must preserve enough of a second word to remain distinct.");
            Assert.That(GameObject.Find("Predicted Member Arts Preview 072").GetComponent<Text>().text,
                Does.Contain("+3 MEMBERS").And.Contain("SEE UNION"),
                "The narrow forecast pane must summarize overflow while the focused rail names all six members.");

            var nextPage = GameObject.Find("Next Union Page 078").GetComponent<Button>();
            Assert.That(nextPage.gameObject.activeInHierarchy, Is.True);
            Assert.That(nextPage.interactable, Is.True);
            eventSystem?.SetSelectedGameObject(nextPage.gameObject);
            nextPage.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var secondPage = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Union Focus Chip ") &&
                                value.gameObject.activeInHierarchy)
                .OrderBy(value => value.name)
                .ToArray();
            Assert.That(secondPage.Length, Is.EqualTo(4));
            Assert.That(secondPage[0].GetComponentInChildren<Text>().text, Does.StartWith("07"));
            Assert.That(secondPage[3].GetComponentInChildren<Text>().text, Does.StartWith("10"));
            Assert.That(eventSystem?.currentSelectedGameObject, Is.EqualTo(secondPage[0].gameObject),
                "Paging must move controller focus off the newly disabled page button and onto a live Union.");
            Assert.That(hud.FocusedUnionId, Is.EqualTo("ALLY_7"));
            Assert.That(focusedUnionId, Is.EqualTo("ALLY_7"));
            Assert.That(memberStates.All(value => value.text.Contains("HP77/120")), Is.True,
                "Turning the page must also move the active command context to its first live Union.");

            secondPage[3].onClick.Invoke();
            yield return null;
            Assert.That(hud.FocusedUnionId, Is.EqualTo("ALLY_10"));
            Assert.That(focusedUnionId, Is.EqualTo("ALLY_10"));
            Assert.That(memberStates.All(value => value.text.Contains("HP80/120")), Is.True,
                "The six member readouts must remain reachable for Union ten.");

            var order = GameObject.Find("Complete Forecast Order 1 072").GetComponent<Button>();
            Assert.That(order.gameObject.activeInHierarchy, Is.True);
            Assert.That(order.interactable, Is.True);
            order.onClick.Invoke();
            yield return null;
            Assert.That(selectedUnionId, Is.EqualTo("ALLY_10"));
            Assert.That(selectedForecastId, Is.EqualTo("FORECAST_ALLY_10"));

            foreach (var union in battle.PlayerUnions.Skip(6)) union.CanAct = false;
            eventSystem?.SetSelectedGameObject(secondPage[3].gameObject);
            hud.Refresh(battle, "ALLY_10");
            yield return null;
            var previousPage = GameObject.Find("Previous Union Page 078").GetComponent<Button>();
            Assert.That(previousPage.interactable, Is.True);
            Assert.That(eventSystem?.currentSelectedGameObject, Is.EqualTo(previousPage.gameObject),
                "A page of spent or down Unions must leave controller focus on the live return-page control.");
            previousPage.onClick.Invoke();
            yield return null;
            Assert.That(hud.FocusedUnionId, Is.EqualTo("ALLY_1"));
            Assert.That(focusedUnionId, Is.EqualTo("ALLY_1"));
            Assert.That(eventSystem?.currentSelectedGameObject,
                Is.EqualTo(GameObject.Find("Union Focus Chip 1 072")),
                "Returning a page must restore both command context and controller focus to a live Union.");

            eventSystem?.SetSelectedGameObject(nextPage.gameObject);
            nextPage.onClick.Invoke();
            yield return null;
            Assert.That(hud.FocusedUnionId, Is.EqualTo("ALLY_7"));
            Assert.That(focusedUnionId, Is.EqualTo("ALLY_7"),
                "Inspection focus must stay synchronized when a page contains only spent or down Unions.");
            Assert.That(GameObject.Find("Complete Forecast Order 1 072").GetComponent<Button>().interactable,
                Is.False, "Inspecting a down Union must never make its complete Forecast selectable.");
            Assert.That(eventSystem?.currentSelectedGameObject,
                Is.EqualTo(GameObject.Find("Previous Union Page 078")),
                "An inactive page must keep the controller on its live return control.");

            if (createdEventSystem == null && eventSystem != null)
                eventSystem.SetSelectedGameObject(previousSelection);
            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            if (createdEventSystem != null) Object.Destroy(createdEventSystem);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattlefieldShowsMoraleRoundChainAndDeadlockRelationship()
        {
            var host = Host("Combat Diorama Test Host 074", new Vector2(1280f, 800f));
            var owner = new GameObject("Combat Diorama Test Owner 074");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            diorama.Initialize(host);
            var battle = Battle(1, 3);
            battle.PlayerUnions[0].Engagement = "Engaged";
            battle.EnemyUnions[0].Engagement = "Engaged";
            battle.LastResolvedRoundEvents = new[]
            {
                new M2BattleEventView { EventType = "PLAYER_HIT" },
                new M2BattleEventView { EventType = "ENEMY_HIT" }
            };
            battle.Events = new[]
            {
                new M2BattleEventView
                {
                    EventType = "BREAKTHROUGH",
                    MemberId = "ALLY_MEMBER_1",
                    ArtId = "ART_POWER_CUT",
                    Text = "Power Cut"
                }
            };

            diorama.Refresh(battle, "ALLY_1");
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(M2BattleTacticalReadout074.CombatChain(battle), Is.EqualTo(2));
            var localTexts074 = host.GetComponentsInChildren<Text>(true);
            var localImages074 = host.GetComponentsInChildren<Image>(true);
            var round074 = localTexts074.Single(value => value.name == "Round and Combat Chain 074");
            Assert.That(localImages074.Any(value => value.name == "Allies Versus Enemies Morale Rail 074"), Is.True);
            Assert.That(round074.text,
                Does.Contain("CHAIN 2"));
            Assert.That(localTexts074.Single(value => value.name == "Focused Engagement State 074").text,
                Is.EqualTo("DEADLOCK"));
            Assert.That(localTexts074.Single(value => value.name == "Focused Ally Union Name 072").text,
                Does.Contain("HP"));
            Assert.That(localTexts074.Single(value => value.name == "Target Enemy Union Name 072").text,
                Does.Contain("AP"));
            var allyHeader = localTexts074.Single(value => value.name == "Focused Ally Union Name 072");
            var enemyHeader = localTexts074.Single(value => value.name == "Target Enemy Union Name 072");
            var round = round074;
            Assert.That(allyHeader.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
            Assert.That(enemyHeader.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
            Assert.That(round.resizeTextMinSize,
                Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumCriticalTopHudFontSize074));
            Assert.That(host.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name == "Union Focus Header 072").rect.height,
                Is.GreaterThanOrEqualTo(48f));
            Assert.That(localTexts074.Single(value => value.name == "Active Union AP and Formation 076").text,
                Does.StartWith("ACTIVE UNION").And.Contain("AP"));
            var enemyIntent074 = localTexts074.Single(value => value.name == "Enemy Intent 076");
            Assert.That(enemyIntent074.text,
                Is.EqualTo("ENEMY INTENT  •  HOLD THE DEADLOCK"));
            Assert.That(enemyIntent074.resizeTextMinSize,
                Is.GreaterThanOrEqualTo(19));
            Assert.That(localImages074.Any(value => value.name == "Authored Ally Focus Plate 072"), Is.False);
            Assert.That(localImages074.Any(value => value.name == "Authored Enemy Focus Plate 072"), Is.False,
                "Debug-like Union bounding boxes must not survive into the authored battlefield.");
            var allyGrounding = localImages074.Single(value => value.name == "Authored Ally Grounding Wash 076");
            var enemyGrounding = localImages074.Single(value => value.name == "Authored Enemy Grounding Wash 076");
            Assert.That(allyGrounding.sprite, Is.Not.Null);
            Assert.That(enemyGrounding.sprite, Is.SameAs(allyGrounding.sprite));
            Assert.That(allyGrounding.rectTransform.rect.height, Is.LessThan(210f));
            Assert.That(enemyGrounding.rectTransform.rect.height, Is.LessThan(210f));
            Assert.That(allyGrounding.color.a, Is.LessThan(0.5f));
            Assert.That(enemyGrounding.color.a, Is.LessThan(0.5f));
            var lowerThirds = host.GetComponentsInChildren<Image>(true)
                .Where(value => value.name == "Authored Actor Lower Third 076")
                .ToArray();
            Assert.That(lowerThirds, Is.Not.Empty);
            Assert.That(lowerThirds.All(value => value.rectTransform.rect.height <= 48f), Is.True,
                "Actor identity should read as a subtle lower third, not a debug nameplate box.");
            Assert.That(host.GetComponentsInChildren<Image>(true)
                .Count(value => value.name == "Authored Contact Shadow 076"), Is.GreaterThanOrEqualTo(2));

            Assert.That(diorama.BreakthroughNotificationVisible076, Is.False);
            Assert.That(diorama.BreakthroughNotificationPresentationCount076, Is.Zero);
            Assert.That(diorama.QueuedBreakthroughNotificationCount076, Is.Zero,
                "Historical battle events must not reopen or queue a learned-Art notification on Refresh.");
            Assert.That(diorama.PresentBreakthroughEvent076(battle.Events[0]), Is.True);
            var breakthrough = host.GetComponentsInChildren<Image>(true)
                .SingleOrDefault(value => value.name == "Persistent Breakthrough Callout 074")
                ?.gameObject;
            Assert.That(breakthrough, Is.Not.Null.And.Property("activeInHierarchy").True,
                "A newly learned Art must create one unmistakable short-lived notification.");
            Assert.That(localTexts074.Single(value => value.name == "Breakthrough Heading 074").text,
                Is.EqualTo("NEW ART LEARNED"));
            Assert.That(localTexts074.Single(value => value.name == "Breakthrough Heading 074").resizeTextMinSize,
                Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumBreakthroughHeadingFontSize074));
            Assert.That(localTexts074.Single(value => value.name == "Breakthrough Learned Art 074").text,
                Does.Contain("POWER CUT").And.Contain("READY FOR FUTURE BATTLES"));
            Assert.That(breakthrough.GetComponent<RectTransform>().rect.height,
                Is.EqualTo(host.rect.height * 0.16f).Within(1f));

            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Real072BattleBeatConsumesExactMotionVfxTimingAndSfxRecipe076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host = Host("Live Exact Art Recipe Test Host 076", new Vector2(1280f, 800f));
            var owner = new GameObject("Live Exact Art Recipe Test Owner 076");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            diorama.Initialize(host);
            var audio = owner.AddComponent<M2BattleAudioDirector>();
            var sequence = owner.AddComponent<M2BattleSequenceDirector072>();
            sequence.Initialize(diorama, audio, (_, __) => { });
            var battle = Battle(1, 1);
            diorama.Refresh(battle, "ALLY_1");
            yield return null;

            const string artId076 = "TREE_CA002_WPN_SWORD_N01";
            Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                artId076,
                BattleBeatFamily.CombatArt,
                out var profile076), Is.True);
            var actor076 = diorama.ResolveActor("ALLY_MEMBER_1", "ALLY_1");
            Assert.That(actor076, Is.Not.Null);
            sequence.StartCoroutine(sequence.PlayRound(
                battle,
                battle,
                new[]
                {
                    new M2BattleEventView
                    {
                        Sequence = 1,
                        Round = 2,
                        EventType = "MARTIAL_HIT",
                        ActorUnionId = "ALLY_1",
                        ActorMemberId = "ALLY_MEMBER_1",
                        TargetUnionId = "ENEMY_1",
                        TargetMemberId = "ENEMY_MEMBER_1",
                        ArtId = artId076,
                        Amount = 24,
                        Text = "Daeven uses Ready Cut."
                    }
                },
                false,
                () => 1f,
                () => false));

            var sawMotion076 = false;
            var sawLiveVfx076 = false;
            // Batchmode can advance hundreds of near-zero-delta frames before the
            // authored one-second recipe reaches its VFX phase. Observe the entire
            // normal-speed beat behind a real-time hang guard so the assertion still
            // requires a live Image instead of relying only on telemetry afterward.
            var observationDeadline076 = Time.realtimeSinceStartup + 8f;
            while (sequence.IsPlaying && Time.realtimeSinceStartup < observationDeadline076)
            {
                sawMotion076 = sawMotion076 ||
                               Vector2.Distance(
                                   actor076.Root.anchoredPosition,
                                   actor076.HomePosition) > 0.5f ||
                               Mathf.Abs(actor076.Root.localScale.x - actor076.HomeScale.x) > 0.005f;
                sawLiveVfx076 = sawLiveVfx076 ||
                                host.GetComponentsInChildren<Image>(true).Any(value =>
                                    value != null && value.gameObject.activeInHierarchy &&
                                    (value.name == "Authored Battle Effect 072" ||
                                     value.name == "Authored Battle Projectile 072"));
                yield return null;
            }

            var diagnostics076 = diorama.LiveArtRecipeDiagnostics076;
            Assert.That(sequence.IsPlaying, Is.False,
                "The exact normal-speed recipe did not complete before the real-time hang guard.");
            Assert.That(sequence.LastPresentedBeatCount, Is.EqualTo(1));
            Assert.That(sawMotion076, Is.True,
                "The exact motion recipe must visibly move or scale its actor rig.");
            Assert.That(sawLiveVfx076, Is.True,
                "The exact VFX recipe must spawn a visible authored effect in the live 072 stage.");
            Assert.That(diagnostics076.HasCompleteLiveConsumption, Is.True);
            Assert.That(diagnostics076.ArtId, Is.EqualTo(artId076));
            Assert.That(diagnostics076.RecipeId, Is.EqualTo(profile076.presentationRecipeId));
            Assert.That(diagnostics076.MotionSignature,
                Is.EqualTo(profile076.presentationMotionSignature));
            Assert.That(diagnostics076.VfxSignature,
                Is.EqualTo(profile076.presentationVfxSignature));
            Assert.That(diagnostics076.SfxSignature,
                Is.EqualTo(profile076.presentationSfxSignature));
            Assert.That(diagnostics076.DurationMilliseconds,
                Is.EqualTo(profile076.presentationDurationMilliseconds));
            Assert.That(diagnostics076.ImpactMilliseconds,
                Is.EqualTo(profile076.presentationImpactMilliseconds));
            Assert.That(diagnostics076.MotionRecipe, Is.EqualTo("SetAdvancePrimaryRecover"));
            Assert.That(diagnostics076.VfxRecipe, Is.EqualTo("FocusedPrimary"));
            Assert.That(diagnostics076.CompletedExactBeatCount, Is.EqualTo(1));
            Assert.That(diagnostics076.LastCompletedCameraRecipe,
                Is.EqualTo("MediumThreeQuarterTrackIn"));
            Assert.That(diagnostics076.LastCompletedTraceRecipe, Is.Not.Empty);
            Assert.That(diagnostics076.LastCompletedTraceGeometryCount, Is.GreaterThan(0));
            Assert.That(diagnostics076.LastCompletedObservedImpactMilliseconds, Is.GreaterThan(0));
            Assert.That(diagnostics076.LastCompletedObservedCompletionMilliseconds,
                Is.GreaterThan(diagnostics076.LastCompletedObservedImpactMilliseconds));
            Assert.That(diagnostics076.LastCompletedObservedImpactMilliseconds,
                Is.EqualTo(profile076.presentationImpactMilliseconds).Within(220),
                "Normal-speed contact must occur at the authored impact time within frame tolerance.");
            Assert.That(diagnostics076.LastCompletedObservedCompletionMilliseconds,
                Is.EqualTo(profile076.presentationDurationMilliseconds).Within(280),
                "Normal-speed recovery/home must finish at the authored duration within frame tolerance.");
            Assert.That(Vector2.Distance(actor076.Root.anchoredPosition, actor076.HomePosition),
                Is.LessThanOrEqualTo(0.05f));
            Assert.That(sequence.HasConsumedExactRecipeSfx076, Is.True);
            Assert.That(sequence.HasConsumedExactRecipeSfxSignature076(
                profile076.presentationSfxSignature), Is.True);
            Assert.That(sequence.ExactRecipeStartSfxCount076, Is.EqualTo(1));
            Assert.That(sequence.ExactRecipeImpactSfxCount076, Is.EqualTo(1));
            Assert.That(sequence.LastExactRecipeSfxSignature076,
                Is.EqualTo(profile076.presentationSfxSignature));
            Assert.That(sequence.LastExactRecipeStartCue076,
                Is.EqualTo("RESOURCE:" + profile076.startAudioResourcePath));
            Assert.That(sequence.LastExactRecipeImpactCue076,
                Is.EqualTo("RESOURCE:" + profile076.impactAudioResourcePath));
            Assert.That(audio.OneShotPlaybackCount076, Is.EqualTo(2));
            Assert.That(audio.ResourceOneShotPlaybackCount076, Is.EqualTo(2));
            Assert.That(audio.SynthesizedOneShotPlaybackCount076, Is.Zero);
            Assert.That(diorama.Stats.AuthoredVfx, Is.GreaterThan(0));

            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Live072MatrixConsumesCameraTraceAndOneAuthoredCuePerPhase076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host076 = Host("Live Exact Matrix Test Host 076", new Vector2(1280f, 800f));
            var owner076 = new GameObject("Live Exact Matrix Test Owner 076");
            var diorama076 = owner076.AddComponent<M2BattleDioramaView072>();
            diorama076.Initialize(host076);
            var audio076 = owner076.AddComponent<M2BattleAudioDirector>();
            var sequence076 = owner076.AddComponent<M2BattleSequenceDirector072>();
            sequence076.Initialize(diorama076, audio076, (_, __) => { });
            var battle076 = Battle(1, 1);
            diorama076.Refresh(battle076, "ALLY_1");
            yield return null;

            var cases076 = new[]
            {
                new[] { "TREE_CA002_WPN_SWORD_N01", "MARTIAL_HIT" },
                new[] { "TREE_CA002_WPN_SWORD_N02", "MARTIAL_HIT" },
                new[] { "TREE_CA002_WPN_SWORD_N03", "MARTIAL_HIT" },
                new[] { "TREE_CA002_WPN_SWORD_N04", "MARTIAL_HIT" },
                new[] { "TREE_CA002_MYS_FLAME_N01", "MYSTIC_HIT" },
                new[] { "TREE_CA002_MYS_FLAME_N02", "MYSTIC_HIT" },
                new[] { "TREE_CA002_MYS_FLAME_N03", "MYSTIC_HIT" },
                new[] { "TREE_CA002_MYS_FLAME_N04", "MYSTIC_HIT" },
                new[] { "TREE_CA002_ROLE_GUARDIAN_N01", "TACTICAL_HIT" },
                new[] { "TREE_CA002_ROLE_GUARDIAN_N02", "TACTICAL_HIT" },
                new[] { "TREE_CA002_ROLE_GUARDIAN_N03", "TACTICAL_HIT" },
                new[] { "TREE_CA002_ROLE_GUARDIAN_N04", "TACTICAL_HIT" }
            };
            var cameraRecipes076 = new HashSet<string>(StringComparer.Ordinal);
            var traceRecipes076 = new HashSet<string>(StringComparer.Ordinal);
            var cumulativeTraceGeometry076 = 0;

            for (var index076 = 0; index076 < cases076.Length; index076++)
            {
                var artId076 = cases076[index076][0];
                var eventType076 = cases076[index076][1];
                Assert.That(Battle3DArtChoreography071.TryCreate(
                    artId076,
                    out var expected076), Is.True, artId076);
                Assert.That(expected076, Is.Not.Null, artId076);
                var oneShotsBefore076 = audio076.OneShotPlaybackCount076;
                var resourcesBefore076 = audio076.ResourceOneShotPlaybackCount076;
                var synthesizedBefore076 = audio076.SynthesizedOneShotPlaybackCount076;
                var startsBefore076 = sequence076.ExactRecipeStartSfxCount076;
                var impactsBefore076 = sequence076.ExactRecipeImpactSfxCount076;

                yield return sequence076.PlayRound(
                    battle076,
                    battle076,
                    new[] { ExactArtEvent076(index076 + 1, artId076, eventType076) },
                    false,
                    () => 8f,
                    () => false);

                var diagnostics076 = diorama076.LiveArtRecipeDiagnostics076;
                var expectedGeometry076 =
                    (Mathf.Clamp(expected076.SemanticTracePointCount, 6, 12) - 1) *
                    Mathf.Clamp(expected076.SemanticTraceLayerCount, 1, 2);
                cumulativeTraceGeometry076 += expectedGeometry076;
                Assert.That(diagnostics076.CompletedExactBeatCount,
                    Is.EqualTo(index076 + 1), artId076 + " completed count");
                Assert.That(diagnostics076.LastCompletedArtId, Is.EqualTo(artId076));
                Assert.That(diagnostics076.LastCompletedCameraRecipe,
                    Is.EqualTo(expected076.CameraRecipe.ToString()), artId076);
                Assert.That(diagnostics076.LastCompletedTraceRecipe,
                    Is.EqualTo(expected076.TraceRecipe.ToString()), artId076);
                Assert.That(diagnostics076.LastCompletedTraceGeometryCount,
                    Is.EqualTo(expectedGeometry076), artId076);
                Assert.That(diagnostics076.TraceGeometryCount,
                    Is.EqualTo(cumulativeTraceGeometry076), artId076);
                Assert.That(diagnostics076.CameraIntentCount,
                    Is.EqualTo(index076 + 1), artId076);
                Assert.That(diagnostics076.LastCompletedObservedImpactMilliseconds,
                    Is.GreaterThan(0), artId076);
                Assert.That(diagnostics076.LastCompletedObservedCompletionMilliseconds,
                    Is.GreaterThan(diagnostics076.LastCompletedObservedImpactMilliseconds), artId076);
                Assert.That(sequence076.HasConsumedExactRecipeSfxSignature076(
                    diagnostics076.LastCompletedSfxSignature), Is.True, artId076);
                Assert.That(sequence076.ExactRecipeStartSfxCount076,
                    Is.EqualTo(startsBefore076 + 1), artId076);
                Assert.That(sequence076.ExactRecipeImpactSfxCount076,
                    Is.EqualTo(impactsBefore076 + 1), artId076);
                Assert.That(audio076.OneShotPlaybackCount076,
                    Is.EqualTo(oneShotsBefore076 + 2), artId076);
                Assert.That(audio076.ResourceOneShotPlaybackCount076,
                    Is.EqualTo(resourcesBefore076 + 2), artId076);
                Assert.That(audio076.SynthesizedOneShotPlaybackCount076,
                    Is.EqualTo(synthesizedBefore076), artId076);
                cameraRecipes076.Add(diagnostics076.LastCompletedCameraRecipe);
                traceRecipes076.Add(diagnostics076.LastCompletedTraceRecipe);
            }

            Assert.That(cameraRecipes076.Count, Is.EqualTo(4),
                "N01-N04 must visibly use all four stage-camera intents.");
            Assert.That(traceRecipes076.Count, Is.GreaterThanOrEqualTo(3),
                "Weapon, mystic, and role Arts must not collapse to one trace geometry.");
            Object.Destroy(owner076);
            Object.Destroy(host076.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkipAfterImpactDoesNotCertifyCompletionOrErasePriorEvidence076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host076 = Host("Live Exact Skip Test Host 076", new Vector2(1280f, 800f));
            var owner076 = new GameObject("Live Exact Skip Test Owner 076");
            var diorama076 = owner076.AddComponent<M2BattleDioramaView072>();
            diorama076.Initialize(host076);
            var audio076 = owner076.AddComponent<M2BattleAudioDirector>();
            var sequence076 = owner076.AddComponent<M2BattleSequenceDirector072>();
            sequence076.Initialize(diorama076, audio076, (_, __) => { });
            var battle076 = Battle(1, 1);
            diorama076.Refresh(battle076, "ALLY_1");
            yield return null;

            const string completedArt076 = "TREE_CA002_WPN_SWORD_N01";
            yield return sequence076.PlayRound(
                battle076,
                battle076,
                new[] { ExactArtEvent076(1, completedArt076, "MARTIAL_HIT") },
                false,
                () => 8f,
                () => false);
            var completed076 = diorama076.LiveArtRecipeDiagnostics076;
            Assert.That(completed076.CompletedExactBeatCount, Is.EqualTo(1));
            var completedSignature076 = completed076.LastCompletedSfxSignature;
            var observedImpactBefore076 = completed076.ObservedImpactExactBeatCount;

            var skip076 = false;
            sequence076.StartCoroutine(sequence076.PlayRound(
                battle076,
                battle076,
                new[]
                {
                    ExactArtEvent076(2, "TREE_CA002_MYS_FLAME_N02", "MYSTIC_HIT")
                },
                false,
                () => 4f,
                () => skip076));
            for (var frame076 = 0; frame076 < 30 && !sequence076.IsPlaying; frame076++)
                yield return null;
            Assert.That(sequence076.IsPlaying, Is.True,
                "The partial exact-Art round must start before the test waits for its impact boundary.");
            var impactDeadline076 = Time.realtimeSinceStartup + 5f;
            while (sequence076.IsPlaying &&
                   diorama076.LiveArtRecipeDiagnostics076
                       .ObservedImpactExactBeatCount <= observedImpactBefore076 &&
                   Time.realtimeSinceStartup < impactDeadline076)
                yield return null;
            Assert.That(diorama076.LiveArtRecipeDiagnostics076.ObservedImpactExactBeatCount,
                Is.EqualTo(observedImpactBefore076 + 1),
                "The test must skip after impact but before recovery completes.");
            skip076 = true;
            var stopDeadline076 = Time.realtimeSinceStartup + 5f;
            while (sequence076.IsPlaying && Time.realtimeSinceStartup < stopDeadline076)
                yield return null;
            Assert.That(sequence076.IsPlaying, Is.False);

            var afterSkip076 = diorama076.LiveArtRecipeDiagnostics076;
            Assert.That(afterSkip076.StartedExactBeatCount, Is.EqualTo(2));
            Assert.That(afterSkip076.CompletedExactBeatCount, Is.EqualTo(1));
            Assert.That(afterSkip076.LastCompletedArtId, Is.EqualTo(completedArt076));
            Assert.That(afterSkip076.LastCompletedSfxSignature,
                Is.EqualTo(completedSignature076));
            Assert.That(sequence076.HasConsumedExactRecipeSfxSignature076(
                completedSignature076), Is.True,
                "A later partial beat must not erase cumulative completed SFX evidence.");

            Object.Destroy(owner076);
            Object.Destroy(host076.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReducedMotionShrinksLiveReactionScaleVfxProjectileAndCameraBounds076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host076 = Host("Live Exact Reduced Motion Test Host 076", new Vector2(1280f, 800f));
            var owner076 = new GameObject("Live Exact Reduced Motion Test Owner 076");
            var diorama076 = owner076.AddComponent<M2BattleDioramaView072>();
            diorama076.Initialize(host076);
            var audio076 = owner076.AddComponent<M2BattleAudioDirector>();
            var sequence076 = owner076.AddComponent<M2BattleSequenceDirector072>();
            sequence076.Initialize(diorama076, audio076, (_, __) => { });
            var battle076 = Battle(1, 1);
            diorama076.Refresh(battle076, "ALLY_1");
            yield return null;

            const string artId076 = "TREE_CA002_MYS_FLAME_N03";
            Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                artId076,
                BattleBeatFamily.Mystic,
                out var profile076), Is.True);
            yield return sequence076.PlayRound(
                battle076,
                battle076,
                new[] { ExactArtEvent076(1, artId076, "MYSTIC_HIT") },
                false,
                () => 1f,
                () => false);
            var full076 = diorama076.LiveArtRecipeDiagnostics076;
            Assert.That(full076.LastCompletedProjectileArcPixels, Is.GreaterThan(0));
            Assert.That(full076.LastCompletedProjectileRotationDegrees, Is.GreaterThan(0));
            Assert.That(full076.LastCompletedVfxPulseDelta, Is.GreaterThan(0));
            Assert.That(full076.LastCompletedObservedImpactMilliseconds,
                Is.EqualTo(profile076.presentationImpactMilliseconds).Within(220));
            Assert.That(full076.LastCompletedObservedCompletionMilliseconds,
                Is.EqualTo(profile076.presentationDurationMilliseconds).Within(280));

            yield return sequence076.PlayRound(
                battle076,
                battle076,
                new[] { ExactArtEvent076(2, artId076, "MYSTIC_HIT") },
                true,
                () => 1f,
                () => false);
            var reduced076 = diorama076.LiveArtRecipeDiagnostics076;
            Assert.That(reduced076.CompletedExactBeatCount, Is.EqualTo(2));
            Assert.That(reduced076.LastCompletedReducedMotion, Is.True);
            Assert.That(reduced076.LastCompletedReactionDisplacementPixels,
                Is.LessThanOrEqualTo(full076.LastCompletedReactionDisplacementPixels * 0.30f));
            Assert.That(reduced076.LastCompletedActorScaleDelta,
                Is.LessThanOrEqualTo(full076.LastCompletedActorScaleDelta * 0.30f));
            Assert.That(reduced076.LastCompletedVfxRotationDegrees,
                Is.LessThanOrEqualTo(full076.LastCompletedVfxRotationDegrees * 0.30f));
            Assert.That(reduced076.LastCompletedVfxPulseDelta,
                Is.LessThanOrEqualTo(full076.LastCompletedVfxPulseDelta * 0.30f));
            Assert.That(reduced076.LastCompletedProjectileArcPixels,
                Is.LessThanOrEqualTo(full076.LastCompletedProjectileArcPixels * 0.30f));
            Assert.That(reduced076.LastCompletedProjectileRotationDegrees,
                Is.LessThanOrEqualTo(full076.LastCompletedProjectileRotationDegrees * 0.30f));
            Assert.That(reduced076.LastCompletedCameraOffsetPixels,
                Is.LessThanOrEqualTo(full076.LastCompletedCameraOffsetPixels * 0.30f));
            Assert.That(reduced076.LastCompletedCameraScaleDelta,
                Is.LessThanOrEqualTo(full076.LastCompletedCameraScaleDelta * 0.30f));
            Assert.That(reduced076.LastCompletedCameraRotationDegrees,
                Is.LessThanOrEqualTo(full076.LastCompletedCameraRotationDegrees * 0.30f));
            var expectedReducedImpact076 =
                Mathf.RoundToInt(profile076.presentationImpactMilliseconds * 0.34f);
            var expectedReducedCompletion076 =
                Mathf.RoundToInt(profile076.presentationDurationMilliseconds * 0.34f);
            Assert.That(reduced076.LastCompletedObservedImpactMilliseconds,
                Is.EqualTo(expectedReducedImpact076).Within(150),
                "Reduced motion must preserve the authored contact ratio at the documented 0.34 cadence.");
            Assert.That(reduced076.LastCompletedObservedCompletionMilliseconds,
                Is.EqualTo(expectedReducedCompletion076).Within(190),
                "Reduced motion must complete recovery/home at the documented 0.34 cadence.");
            Assert.That(reduced076.LastCompletedObservedImpactMilliseconds,
                Is.LessThan(full076.LastCompletedObservedImpactMilliseconds * 0.70f));
            Assert.That(reduced076.LastCompletedObservedCompletionMilliseconds,
                Is.LessThan(full076.LastCompletedObservedCompletionMilliseconds * 0.70f));

            Object.Destroy(owner076);
            Object.Destroy(host076.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReducedMotionShrinksLiveRestorationReactionAndReportsItsActualEnvelope076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host076 = Host("Live Restoration Reduced Motion Test Host 076", new Vector2(1280f, 800f));
            var owner076 = new GameObject("Live Restoration Reduced Motion Test Owner 076");
            var diorama076 = owner076.AddComponent<M2BattleDioramaView072>();
            diorama076.Initialize(host076);
            var audio076 = owner076.AddComponent<M2BattleAudioDirector>();
            var sequence076 = owner076.AddComponent<M2BattleSequenceDirector072>();
            sequence076.Initialize(diorama076, audio076, (_, __) => { });
            var battle076 = Battle(1, 1);
            diorama076.Refresh(battle076, "ALLY_1");
            yield return null;

            const string restorationArtId076 = "TREE_CA002_MYS_RESTORATION_N03";
            Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                restorationArtId076,
                BattleBeatFamily.Restoration,
                out _), Is.True);
            M2BattleEventView RestorationEvent076(int sequenceNumber076) => new M2BattleEventView
            {
                Sequence = sequenceNumber076,
                Round = 2,
                EventType = "RESTORATION",
                ActorUnionId = "ALLY_1",
                ActorMemberId = "ALLY_MEMBER_1",
                TargetUnionId = "ALLY_1",
                TargetMemberId = "ALLY_MEMBER_1_2",
                ArtId = restorationArtId076,
                Amount = 24,
                Text = "Sera Vale is restored by an exact first-hour Art."
            };

            yield return sequence076.PlayRound(
                battle076,
                battle076,
                new[] { RestorationEvent076(1) },
                false,
                () => 4f,
                () => false);
            var full076 = diorama076.LiveArtRecipeDiagnostics076;
            Assert.That(full076.CompletedExactBeatCount, Is.EqualTo(1));
            Assert.That(full076.LastCompletedArtId, Is.EqualTo(restorationArtId076));
            Assert.That(full076.LastCompletedReactionDisplacementPixels, Is.EqualTo(8f).Within(0.001f),
                "Supportive Art evidence must report the supportive live reaction, not the harmful envelope.");

            yield return sequence076.PlayRound(
                battle076,
                battle076,
                new[] { RestorationEvent076(2) },
                true,
                () => 4f,
                () => false);
            var reduced076 = diorama076.LiveArtRecipeDiagnostics076;
            Assert.That(reduced076.CompletedExactBeatCount, Is.EqualTo(2));
            Assert.That(reduced076.LastCompletedReducedMotion, Is.True);
            Assert.That(reduced076.LastCompletedArtId, Is.EqualTo(restorationArtId076));
            Assert.That(reduced076.LastCompletedReactionDisplacementPixels, Is.EqualTo(2f).Within(0.001f));
            Assert.That(reduced076.LastCompletedReactionDisplacementPixels,
                Is.LessThanOrEqualTo(full076.LastCompletedReactionDisplacementPixels * 0.30f));

            Object.Destroy(owner076);
            Object.Destroy(host076.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrossUnionRestorationStagesBothAlliesAndLandsOnTheRescueTarget086()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host086 = Host("Cross-Union Restoration Stage Test Host 086", new Vector2(1280f, 800f));
            var owner086 = new GameObject("Cross-Union Restoration Stage Test Owner 086");
            var diorama086 = owner086.AddComponent<M2BattleDioramaView072>();
            diorama086.Initialize(host086);
            var sequence086 = owner086.AddComponent<M2BattleSequenceDirector072>();
            sequence086.Initialize(diorama086, null, (_, __) => { });
            var battle086 = Battle(2, 0);
            var sourceMember086 = battle086.PlayerUnions[0].Members[0];
            var targetMember086 = battle086.PlayerUnions[1].Members[0];
            targetMember086.CurrentHp = 20;
            const string restorationArtId086 = "TREE_CA002_MYS_RESTORATION_N03";
            Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                restorationArtId086,
                BattleBeatFamily.Restoration,
                out var profile086), Is.True);
            Assert.That(profile086.projectileResourcePath, Is.Not.Empty,
                "Cross-Union restoration needs a visible traveling support effect.");
            var restoration086 = new M2BattleEventView
            {
                Sequence = 1,
                Round = 2,
                EventType = "RESTORATION",
                ActorUnionId = battle086.PlayerUnions[0].UnionId,
                ActorMemberId = sourceMember086.MemberId,
                TargetUnionId = battle086.PlayerUnions[1].UnionId,
                TargetMemberId = targetMember086.MemberId,
                ArtId = restorationArtId086,
                Amount = 24,
                Text = "Daeven sends restoration across the line to the wounded Union."
            };
            var sourceAuthorityHp086 = sourceMember086.CurrentHp;
            var targetAuthorityHp086 = targetMember086.CurrentHp;

            diorama086.Refresh(battle086, battle086.PlayerUnions[0].UnionId);
            yield return null;
            Canvas.ForceUpdateCanvases();
            sequence086.StartCoroutine(sequence086.PlayRound(
                battle086,
                battle086,
                new[] { restoration086 },
                false,
                () => 1f,
                () => false));

            var sawBothAlliedUnions086 = false;
            var sawTravelEffect086 = false;
            var farthestFromTarget086 = 0f;
            var closestToTarget086 = float.MaxValue;
            // Batchmode can render hundreds of very small unscaled-delta frames while
            // honoring the exact one-second Art recipe. Observe the complete recipe,
            // not only its early anticipation frames, while retaining a hard hang guard.
            var observationDeadline086 = Time.realtimeSinceStartup + 8f;
            while (sequence086.IsPlaying && Time.realtimeSinceStartup < observationDeadline086)
            {
                var sourceActor086 = diorama086.ResolveActor(
                    sourceMember086.MemberId,
                    battle086.PlayerUnions[0].UnionId);
                var targetActor086 = diorama086.ResolveActor(
                    targetMember086.MemberId,
                    battle086.PlayerUnions[1].UnionId);
                if (sourceActor086 != null && targetActor086 != null &&
                    sourceActor086.Root.gameObject.activeInHierarchy &&
                    targetActor086.Root.gameObject.activeInHierarchy)
                {
                    sawBothAlliedUnions086 = true;
                    Assert.That(sourceActor086.Enemy, Is.False);
                    Assert.That(targetActor086.Enemy, Is.False,
                        "The right-lane rescue recipient must keep allied identity and styling.");
                    Assert.That(sourceActor086.Root.anchorMin.x, Is.LessThan(0.5f));
                    Assert.That(targetActor086.Root.anchorMin.x, Is.GreaterThan(0.5f));

                    var targetEffectPoint086 = targetActor086.Root.TransformPoint(
                        new Vector3(0f, targetActor086.Root.rect.height * 0.55f, 0f));
                    var projectile086 = host086.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(value =>
                            value != null && value.gameObject.activeInHierarchy &&
                            value.name == "Authored Battle Projectile 072");
                    if (projectile086 != null)
                    {
                        sawTravelEffect086 = true;
                        var distance086 = Vector3.Distance(
                            projectile086.rectTransform.position,
                            targetEffectPoint086);
                        farthestFromTarget086 = Mathf.Max(farthestFromTarget086, distance086);
                        closestToTarget086 = Mathf.Min(closestToTarget086, distance086);
                    }
                }
                yield return null;
            }
            Assert.That(sequence086.IsPlaying, Is.False,
                "Cross-Union restoration did not complete before the real-time hang guard.");

            Assert.That(sequence086.IsPlaying, Is.False);
            Assert.That(sawBothAlliedUnions086, Is.True,
                "The healer and recipient Unions must be staged together for the complete support beat.");
            Assert.That(sawTravelEffect086, Is.True);
            Assert.That(farthestFromTarget086, Is.GreaterThan(40f),
                "The restoration effect must visibly travel instead of spawning only on the recipient.");
            Assert.That(closestToTarget086, Is.LessThanOrEqualTo(4f),
                "The traveling restoration effect must finish on the target Union actor.");

            var finalSourceActor086 = diorama086.ResolveActor(
                sourceMember086.MemberId,
                battle086.PlayerUnions[0].UnionId);
            var finalTargetActor086 = diorama086.ResolveActor(
                targetMember086.MemberId,
                battle086.PlayerUnions[1].UnionId);
            Assert.That(finalSourceActor086, Is.Not.Null);
            Assert.That(finalTargetActor086, Is.Not.Null);
            Assert.That(diorama086.ActiveActorCount, Is.EqualTo(6),
                "Only the complete acting and recipient Unions should remain staged for this beat.");
            Assert.That(finalSourceActor086.Enemy, Is.False);
            Assert.That(finalTargetActor086.Enemy, Is.False);
            Assert.That(finalSourceActor086.PresentedCurrentHp076, Is.EqualTo(sourceAuthorityHp086));
            Assert.That(finalTargetActor086.PresentedCurrentHp076, Is.EqualTo(targetAuthorityHp086 + 24));
            Assert.That(sourceMember086.CurrentHp, Is.EqualTo(sourceAuthorityHp086),
                "Presentation must not mutate the acting member's authoritative HP.");
            Assert.That(targetMember086.CurrentHp, Is.EqualTo(targetAuthorityHp086),
                "Presentation must not mutate the recipient member's authoritative HP.");
            Assert.That(GameObject.Find("Focused Ally Union Name 072").GetComponent<Text>().text,
                Does.Contain("OPENING UNION 1"));
            Assert.That(GameObject.Find("Target Enemy Union Name 072").GetComponent<Text>().text,
                Does.Contain("OPENING UNION 2"));
            Assert.That(GameObject.Find("Active Union AP and Formation 076").GetComponent<Text>().text,
                Does.StartWith("SUPPORT SOURCE"));
            Assert.That(GameObject.Find("Enemy Intent 076").GetComponent<Text>().text,
                Does.StartWith("ALLY RESCUE TARGET").And.Not.Contain("ENEMY INTENT"));
            Assert.That(diorama086.FocusedEnemyHpLabel076.text,
                Does.StartWith("ALLY RESCUE TARGET HP").And.Contain("244 / 360"));
            Assert.That(diorama086.FocusedEnemyHpFill076.color,
                Is.EqualTo(diorama086.FocusedAllyHpFill076.color),
                "A right-lane allied recipient must retain allied HP styling.");

            Object.Destroy(owner086);
            Object.Destroy(host086.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExactAudioSynthesizesOnlyWhenAuthoredPhaseResourcesAreMissing076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            const string artId076 = "TREE_CA002_WPN_SWORD_N01";
            Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                artId076,
                BattleBeatFamily.CombatArt,
                out var profile076), Is.True);
            var originalAudio076 = profile076.audioResourcePath;
            var originalWindup076 = profile076.windupAudioResourcePath;
            var originalStart076 = profile076.startAudioResourcePath;
            var originalImpact076 = profile076.impactAudioResourcePath;
            var host076 = Host("Live Exact Audio Fallback Test Host 076", new Vector2(1280f, 800f));
            var owner076 = new GameObject("Live Exact Audio Fallback Test Owner 076");
            var diorama076 = owner076.AddComponent<M2BattleDioramaView072>();
            diorama076.Initialize(host076);
            var audio076 = owner076.AddComponent<M2BattleAudioDirector>();
            var sequence076 = owner076.AddComponent<M2BattleSequenceDirector072>();
            sequence076.Initialize(diorama076, audio076, (_, __) => { });
            var battle076 = Battle(1, 1);
            diorama076.Refresh(battle076, "ALLY_1");
            yield return null;

            try
            {
                const string missing076 =
                    "SecondDimension/Audio/Battle011/Missing/FH071_EXACT_076";
                profile076.audioResourcePath = missing076;
                profile076.windupAudioResourcePath = missing076;
                profile076.startAudioResourcePath = missing076;
                profile076.impactAudioResourcePath = missing076;
                yield return sequence076.PlayRound(
                    battle076,
                    battle076,
                    new[] { ExactArtEvent076(1, artId076, "MARTIAL_HIT") },
                    false,
                    () => 8f,
                    () => false);

                Assert.That(audio076.OneShotPlaybackCount076, Is.EqualTo(2));
                Assert.That(audio076.ResourceOneShotPlaybackCount076, Is.Zero);
                Assert.That(audio076.SynthesizedOneShotPlaybackCount076, Is.EqualTo(2));
                Assert.That(sequence076.LastExactRecipeStartCue076,
                    Does.StartWith("FH071_EXACT_START|"));
                Assert.That(sequence076.LastExactRecipeImpactCue076,
                    Does.StartWith("FH071_EXACT_IMPACT|"));
                Assert.That(sequence076.HasConsumedExactRecipeSfxSignature076(
                    profile076.presentationSfxSignature), Is.True);
            }
            finally
            {
                profile076.audioResourcePath = originalAudio076;
                profile076.windupAudioResourcePath = originalWindup076;
                profile076.startAudioResourcePath = originalStart076;
                profile076.impactAudioResourcePath = originalImpact076;
            }

            Object.Destroy(owner076);
            Object.Destroy(host076.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GateEaterOwnsOnePlayerFacingIdentityAndBossScaleAtSupportedResolutions()
        {
            foreach (var size in new[] { new Vector2(1280f, 800f), new Vector2(1920f, 1080f) })
            {
                var host = Host("Gate-Eater Diorama Test Host 076 " + size.x, size);
                var owner = new GameObject("Gate-Eater Diorama Test Owner 076 " + size.x);
                var diorama = owner.AddComponent<M2BattleDioramaView072>();
                diorama.Initialize(host);
                var battle = Battle(1, 1);
                battle.BattleId = "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER_PRESENTATION_076";
                battle.Round = 2;
                battle.EnemyUnions = new[]
                {
                    new M2BattleUnionView
                    {
                        UnionId = "EU_GATE_EATER_076",
                        DisplayName = "Hinge-Eater Colossus",
                        Engagement = "Engaged",
                        CurrentAp = M2BattleCommandService.GateEaterBossMinimumUnionAp076,
                        MaximumAp = M2BattleCommandService.GateEaterBossMinimumUnionAp076,
                        CanAct = true,
                        Members = new[]
                        {
                            new M2BattleMemberView
                            {
                                MemberId = "ENEMY_HINGE_EATER_COLOSSUS_01_SPAWN070_076",
                                DisplayName = "Hinge-Eater Colossus",
                                CurrentHp = M2BattleCommandService.GateEaterBossMinimumMaximumHp076,
                                MaximumHp = M2BattleCommandService.GateEaterBossMinimumMaximumHp076
                            },
                            new M2BattleMemberView
                            {
                                MemberId = "ENEMY_GATE_GNAWER_01",
                                DisplayName = "Gate Gnawer Scout",
                                CurrentHp = 90,
                                MaximumHp = 90
                            },
                            new M2BattleMemberView
                            {
                                MemberId = "ENEMY_GATE_GNAWER_02",
                                DisplayName = "Gate Gnawer Guard",
                                CurrentHp = 110,
                                MaximumHp = 110
                            }
                        }
                    }
                };

                diorama.Refresh(battle, "ALLY_1");
                yield return null;
                Canvas.ForceUpdateCanvases();

                var battleRects076 = host.GetComponentsInChildren<RectTransform>(true);
                var battleTexts076 = host.GetComponentsInChildren<Text>(true);
                var boss = battleRects076.Single(value =>
                    value.name == "Battle Actor 072 · THE GATE-EATER");
                var minion = battleRects076.Single(value =>
                    value.name == "Battle Actor 072 · Gate Gnawer Scout");
                var targetEnemyName076 = battleTexts076.Single(value =>
                    value.name == "Target Enemy Union Name 072");
                var enemyIntent076 = battleTexts076.Single(value =>
                    value.name == "Enemy Intent 076");
                Assert.That(targetEnemyName076.text,
                    Does.Contain("THE GATE-EATER").And.Not.Contain("HINGE-EATER"));
                Assert.That(boss.localScale.x, Is.GreaterThan(minion.localScale.x * 2f),
                    "The authoritative climax boss must dominate its escort silhouette.");
                Assert.That(enemyIntent076.text,
                    Is.EqualTo("BOSS CLOCK  •  3 ROUNDS  •  BREACH SKYHOME"));
                Assert.That(targetEnemyName076.text,
                    Does.Contain("HP 920/920").And.Contain("AP 24/24"));
                Assert.That(boss.GetComponentsInChildren<Text>(true)
                        .Single(value => value.name == "Member Display Name 072").text,
                    Is.EqualTo("THE GATE-EATER  •  THREAT X"));
                var bossRig089 = diorama.ResolveActor(
                    "ENEMY_HINGE_EATER_COLOSSUS_01_SPAWN070_076",
                    "EU_GATE_EATER_076");
                Assert.That(bossRig089, Is.Not.Null);
                Assert.That(bossRig089.EnemyThreatTier089, Is.EqualTo(10));
                Assert.That(bossRig089.EnemyThreatLabel089, Is.EqualTo("APOCALYPTIC"));
                Assert.That(host.GetComponentsInChildren<Text>(true)
                    .All(value => (value.text ?? string.Empty).IndexOf(
                        "HINGE-EATER",
                        System.StringComparison.OrdinalIgnoreCase) < 0), Is.True,
                    "No player-facing battle copy may expose the internal Hinge-Eater asset token.");
                Assert.That(boss.GetComponentsInChildren<Outline>(true).Length, Is.GreaterThanOrEqualTo(2),
                    "Both boss-pose layers should carry authored rim lighting.");
                Assert.That(boss.GetComponentsInChildren<Image>(true)
                    .Single(value => value.name == "Authored Contact Shadow 076").color.a,
                    Is.GreaterThan(0.8f));

                Object.Destroy(owner);
                Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ActorRigUsesBattle075EnemyCutoutsBossIdentityAndKeepsPlayerManifestPoses()
        {
            var hostObject = new GameObject("Actor Rig Resource Test 074", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            var enemyUnion = new M2BattleUnionView { UnionId = "EU_GNAWER_PACK" };
            var enemyMember = new M2BattleMemberView
            {
                MemberId = "ENEMY_GATE_GNAWER_01",
                DisplayName = "Gate Gnawer",
                CurrentHp = 40,
                MaximumHp = 40
            };
            var enemy = new M2BattleActorRig072(host, enemyUnion, enemyMember, true);
            var idlePath = enemy.CurrentResourcePath;
            Assert.That(idlePath,
                Is.EqualTo("SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_SCOUT_IDLE"));
            AssertCertifiedBattle075EnemyPath(idlePath);
            Assert.That(enemy.BossEnemy075, Is.False);
            Assert.That(enemy.EnemyArtworkShaderName075,
                Is.EqualTo(M2BattleActorRig072.EnemyCutoutShaderName075));
            var enemyArtwork = enemy.Root.GetComponentsInChildren<Image>(true)
                .Where(value => value.name.StartsWith("Authored Pose "))
                .ToArray();
            Assert.That(enemyArtwork.Length, Is.EqualTo(2));
            Assert.That(enemyArtwork.All(value => value.material != null &&
                value.material.shader != null &&
                value.material.shader.name == M2BattleActorRig072.EnemyCutoutShaderName075), Is.True,
                "Both crossfade layers must share the enemy-only fringe cleanup shader.");
            Assert.That(enemyArtwork.All(value => value.GetComponent<Outline>() != null), Is.True,
                "Cutouts need authored rim light separation from the painted battlefield.");
            Assert.That(enemy.GroundShadow076.sprite, Is.Not.Null);
            Assert.That(enemy.GroundShadow076.color.a, Is.GreaterThanOrEqualTo(0.70f));
            Assert.That(enemy.NamePlate076.rectTransform.anchorMax.y, Is.LessThanOrEqualTo(0.08f),
                "The actor lower third must not return to a debug-sized bounding box.");
            var disposableEnemyMaterial = enemyArtwork[0].material;

            Assert.That(enemy.SetPoseImmediate(BattleArtPoseDirector011.ActionPrimary), Is.True);
            var actionPath = enemy.CurrentResourcePath;
            Assert.That(actionPath,
                Is.EqualTo("SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_SCOUT_ACTION"));
            AssertCertifiedBattle075EnemyPath(actionPath);
            Assert.That(actionPath, Is.Not.EqualTo(idlePath),
                "Enemy action motion must remain visually distinct from idle.");

            Assert.That(enemy.SetPoseImmediate(BattleArtPoseDirector011.Anticipation), Is.True);
            Assert.That(enemy.CurrentResourcePath, Is.EqualTo(actionPath));
            Assert.That(enemy.SetPoseImmediate(BattleArtPoseDirector011.RolePrimary), Is.True);
            Assert.That(enemy.CurrentResourcePath, Is.EqualTo(actionPath));
            Assert.That(enemy.SetPoseImmediate(BattleArtPoseDirector011.HitReaction), Is.True);
            Assert.That(enemy.CurrentResourcePath, Is.EqualTo(idlePath));

            var bossMember = new M2BattleMemberView
            {
                MemberId = "ENEMY_HINGE_EATER_COLOSSUS_01_SPAWN070_1029384756AB",
                DisplayName = "Hinge-Eater Colossus",
                CurrentHp = 400,
                MaximumHp = 400
            };
            var boss = new M2BattleActorRig072(
                host,
                new M2BattleUnionView { UnionId = "EU_HINGE_EATER" },
                bossMember,
                true);
            Assert.That(boss.BossEnemy075, Is.True);
            Assert.That(boss.DisplayName, Is.EqualTo(M2BattleActorRig072.GateEaterDisplayName076));
            Assert.That(boss.Root.name, Does.Contain("THE GATE-EATER").And.Not.Contain("Hinge-Eater"));
            Assert.That(boss.Root.GetComponentsInChildren<Text>(true)
                    .Single(value => value.name == "Member Display Name 072").text,
                Is.EqualTo("THE GATE-EATER  •  THREAT X"),
                "Internal resource identity must never leak into the boss nameplate.");
            Assert.That(boss.EnemyThreatTier089, Is.EqualTo(10));
            Assert.That(boss.EnemyThreatLabel089, Is.EqualTo("APOCALYPTIC"));
            Assert.That(boss.CurrentResourcePath, Is.EqualTo(
                M2BattleActorRig072.GateEaterIdleResourcePath076));
            AssertCertifiedBattle075EnemyPath(boss.CurrentResourcePath);
            Assert.That(boss.CurrentResourcePath, Is.Not.EqualTo(idlePath),
                "The boss must never inherit a hash-selected Gate Gnawer identity.");
            var bossIdleArtwork = boss.Root.GetComponentsInChildren<Image>(true)
                .Single(value => value.name == "Authored Pose A 072");
            Assert.That(bossIdleArtwork.sprite, Is.Not.Null);
            Assert.That(bossIdleArtwork.sprite.rect.height,
                Is.GreaterThan(bossIdleArtwork.sprite.rect.width),
                "The internal boss idle asset is intentionally vertical and must remain visually distinct.");
            Assert.That(boss.SetPoseImmediate(BattleArtPoseDirector011.Anticipation), Is.True);
            Assert.That(boss.CurrentResourcePath, Is.EqualTo(
                M2BattleActorRig072.GateEaterActionResourcePath076));
            AssertCertifiedBattle075EnemyPath(boss.CurrentResourcePath);

            var playerUnion = new M2BattleUnionView { UnionId = "PLAYER_TEST" };
            var playerMember = new M2BattleMemberView
            {
                MemberId = "SIGI_MAREN_TEST_074",
                PortraitAuthorityId = "SIGREC_MAREN_HOLT",
                DisplayName = "Maren Holt",
                CurrentHp = 60,
                MaximumHp = 60
            };
            var player = new M2BattleActorRig072(host, playerUnion, playerMember, false);
            Assert.That(player.CurrentResourcePath, Is.EqualTo(
                "SecondDimension/Art/Battle011/Characters/SIGREC_MAREN_HOLT/POSE_IDLE"));
            Assert.That(player.SetPoseImmediate(BattleArtPoseDirector011.ActionPrimary), Is.True);
            Assert.That(player.CurrentResourcePath, Is.EqualTo(
                "SecondDimension/Art/Battle011/Characters/SIGREC_MAREN_HOLT/POSE_ACTION_PRIMARY"));
            Assert.That(player.EnemyArtworkShaderName075, Is.Empty,
                "The alpha-repair shader must never alter player character artwork.");
            Assert.That(player.Root.GetComponentsInChildren<Image>(true)
                .Where(value => value.name.StartsWith("Authored Pose "))
                .All(value => value.material == null ||
                              value.material.shader == null ||
                              value.material.shader.name != M2BattleActorRig072.EnemyCutoutShaderName075),
                Is.True);

            enemy.Dispose();
            boss.Dispose();
            player.Dispose();
            Object.Destroy(hostObject);
            yield return null;
            Assert.That(disposableEnemyMaterial == null, Is.True,
                "Each runtime fringe-cleanup material must be destroyed with its enemy rig.");
        }

        [UnityTest]
        public IEnumerator Battle075HashSelectionCoversStandardScoutAndBulwarkWithoutFallbacks()
        {
            var hostObject = new GameObject("Enemy Variant Resource Test 075", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            var enemyUnion = new M2BattleUnionView { UnionId = "EU_VARIANTS" };
            var cases = new[]
            {
                new[] { "ENEMY_0", "GATE_GNAWER_SCOUT" },
                new[] { "ENEMY_1", "GATE_GNAWER_BULWARK" },
                new[] { "ENEMY_2", "GATE_GNAWER_STANDARD" }
            };

            foreach (var selection in cases)
            {
                var member = new M2BattleMemberView
                {
                    MemberId = selection[0],
                    DisplayName = selection[1],
                    CurrentHp = 50,
                    MaximumHp = 50
                };
                var actor = new M2BattleActorRig072(host, enemyUnion, member, true);
                Assert.That(actor.CurrentResourcePath, Is.EqualTo(
                    "SecondDimension/Art/Battle075/Enemies/" + selection[1] + "_IDLE"));
                AssertCertifiedBattle075EnemyPath(actor.CurrentResourcePath);
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.RolePrimary), Is.True);
                Assert.That(actor.CurrentResourcePath, Is.EqualTo(
                    "SecondDimension/Art/Battle075/Enemies/" + selection[1] + "_ACTION"));
                AssertCertifiedBattle075EnemyPath(actor.CurrentResourcePath);
                actor.Dispose();
            }

            Object.Destroy(hostObject);
            yield return null;
        }

        private static void AssertCertifiedBattle075EnemyPath(string path)
        {
            Assert.That(path, Does.StartWith("SecondDimension/Art/Battle075/Enemies/"));
            Assert.That(path, Does.Not.Contain("Battle011/Characters"));
            Assert.That(path, Does.Not.StartWith("SecondDimension/Art/Battle/"),
                "Legacy enemy standees are recovery-only and must not become the live selection.");
        }

        [UnityTest]
        public IEnumerator ResultStageSpotlightsGrowthLootAndBossVictoryAtSupportedResolutions()
        {
            foreach (var size in new[] { new Vector2(1280f, 800f), new Vector2(1920f, 1080f) })
            {
            var host = Host("Battle Result Test Host 074 " + size.x, size);
            var owner = new GameObject("Battle Result Test Owner 074 " + size.x);
            var results = owner.AddComponent<M2BattleResultsView072>();
            var continueCalls = 0;
            results.Initialize(host, () => continueCalls++);
            results.Show(ResolvedBattle());
            yield return new WaitForSecondsRealtime(M2BattleResultsView072.BreakthroughHeroSeconds076 + 1.35f);

            var root = GameObject.Find("Battle Results Payoff 072");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<Image>().color.a,
                Is.EqualTo(1f),
                "Resolved battlefield nameplates and health bars must retire behind a fully opaque payoff veil; " +
                "otherwise bright member names bleed through the stage gutters.");
            var trayCover = GameObject.Find("Battle Command Tray Cover 074").GetComponent<Image>();
            Assert.That(trayCover.color.a, Is.EqualTo(1f));
            Assert.That(trayCover.rectTransform.anchorMax.y,
                Is.GreaterThan(M2BattleCommandHud072.MaximumTrayAnchorY),
                "The result stage must fully cover the inactive command tray only.");
            var priorHudCover = GameObject.Find("Battle Prior HUD Cover 074").GetComponent<Image>();
            Assert.That(priorHudCover.color.a, Is.EqualTo(1f));
            Assert.That(priorHudCover.rectTransform.anchorMin.y, Is.LessThanOrEqualTo(0.75f));
            Assert.That(priorHudCover.rectTransform.anchorMax.y, Is.EqualTo(1f));
            Assert.That(root.GetComponentsInChildren<ScrollRect>(true), Is.Empty);
            Assert.That(GameObject.Find("Featured Growth Story 076"), Is.Not.Null);
            Assert.That(GameObject.Find("Meaningful Rewards 076"), Is.Not.Null);
            Assert.That(GameObject.Find("Next Story Objective 076"), Is.Not.Null);
            var studioRect = GameObject.Find("Battle Results Studio Stage 076").GetComponent<RectTransform>().rect;
            Assert.That(studioRect.width, Is.GreaterThanOrEqualTo(size.x * 0.96f));
            Assert.That(studioRect.height, Is.GreaterThanOrEqualTo(size.y * 0.95f),
                "The payoff composition should not collapse into a small panel surrounded by dead black space.");
            Assert.That(GameObject.Find("Performance Summary 074"), Is.Null,
                "A studio payoff must not read like a combat analytics dashboard.");
            Assert.That(GameObject.Find("Outcome Story Title 076").GetComponent<Text>().text,
                Is.EqualTo("THE GATE-EATER HAS FALLEN"));
            Assert.That(GameObject.Find("Featured Adventurer Name 076").GetComponent<Text>().text,
                Is.EqualTo("ADVENTURER 1"));
            Assert.That(GameObject.Find("Featured Adventurer Portrait Art 076").GetComponent<Image>().sprite,
                Is.Not.Null, "The named growth spotlight must carry the adventurer's portrait.");
            Assert.That(GameObject.Find("Featured Adventurer XP 076").GetComponent<Text>().text,
                Does.StartWith("PERSONAL XP").And.Contain("+100 XP"));
            Assert.That(GameObject.Find("Featured Adventurer XP 076").GetComponent<Text>().resizeTextMinSize,
                Is.GreaterThanOrEqualTo(31));
            Assert.That(GameObject.Find("Featured Adventurer Level 076").GetComponent<Text>().text,
                Does.Contain("LEVEL 1  →  2"));
            Assert.That(GameObject.Find("Featured Adventurer Art 076").GetComponent<Text>().text,
                Does.Contain("NEW ART"));
            Assert.That(GameObject.Find("Party Growth Summary 076").GetComponent<Text>().text,
                Does.Contain("9 ADVENTURERS GREW"));
            Assert.That(GameObject.Find("Story Reward Heading 076").GetComponent<Text>().text,
                Is.EqualTo("SKYHOME IS SAFE"));
            Assert.That(GameObject.Find("Armory Reward Name 076").GetComponent<Text>().text,
                Is.EqualTo("Gatewarden Saber"));
            Assert.That(GameObject.Find("Authored Loot Identity Art 076").GetComponent<Image>().sprite,
                Is.Not.Null, "Equipment rewards need authored loot identity, not text alone.");
            var growth = GameObject.Find("Guild Growth Reward 076").GetComponent<Text>();
            Assert.That(growth.text,
                Does.StartWith("MEMBER XP TO SPEND").And.Contain("MEMBER XP TO SPEND  +240")
                    .And.Contain("HALL IMPROVEMENT  +90").And.Not.Contain("HALL XP"));
            Assert.That(growth.text.Split('\n'), Does.Contain("HALL IMPROVEMENT  +90"),
                "Hall improvement and its authoritative amount must remain inseparable at both resolutions.");
            Assert.That(GameObject.Find("Guild Growth Reward 076").GetComponent<Text>().resizeTextMinSize,
                Is.GreaterThanOrEqualTo(19));
            Assert.That(GameObject.Find("Featured Growth Heading 076").GetComponent<Text>().text,
                Is.EqualTo("GROWTH  •  PERSONAL PROGRESS"));
            Assert.That(GameObject.Find("Meaningful Rewards Heading 076").GetComponent<Text>().text,
                Is.EqualTo("LOOT  •  GUILD REWARDS"));
            Assert.That(GameObject.Find("Outcome Kicker 076").GetComponent<Text>().text,
                Is.EqualTo("BOSS VICTORY  •  SKYHOME SECURED"));
            var bossCrest = GameObject.Find("Gate-Eater Victory Crest 076").GetComponent<Image>();
            Assert.That(bossCrest.gameObject.activeInHierarchy, Is.True);
            Assert.That(bossCrest.sprite, Is.Not.Null,
                "The climax result needs a distinct boss-victory image, not another generic dashboard.");
            Assert.That(GameObject.Find("Next Story Objective Copy 076").GetComponent<Text>().text,
                Does.StartWith("Return to Skyhome"));
            var visibleCopy = root.GetComponentsInChildren<Text>(true)
                .Where(value => value.gameObject.activeInHierarchy)
                .Select(value => value.text ?? string.Empty)
                .ToArray();
            Assert.That(visibleCopy.Any(value => value.Contains("ALLIED UNIONS") ||
                                                 value.Contains("ENEMY UNIONS") ||
                                                 value.Contains("CHAIN")), Is.False,
                "Internal combat analytics must not displace character and story payoff.");

            var buttons = root.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(1));
            Assert.That(buttons[0].GetComponentInChildren<Text>().text,
                Is.EqualTo("CONTINUE TO THE RETURN ROAD"));
            Assert.That(buttons[0].GetComponentInChildren<Text>().color.grayscale,
                Is.GreaterThan(0.85f), "Focused return text must stay bright on its blue surface.");
            Assert.That(buttons[0].colors.selectedColor.grayscale,
                Is.LessThan(0.45f), "The selected return surface must remain dark enough for white text.");
            Assert.That(EventSystem.current?.currentSelectedGameObject, Is.EqualTo(buttons[0].gameObject));
            buttons[0].onClick.Invoke();
            Assert.That(continueCalls, Is.EqualTo(1));
            Assert.That(buttons[0].interactable, Is.False);

            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AuthoredHallBreakthroughGetsAReadableHeroBeatBeforeRewardsAtSupportedResolutions()
        {
            foreach (var size in new[] { new Vector2(1280f, 800f), new Vector2(1920f, 1080f) })
            {
                var host = Host("Art Breakthrough Hero Test Host 076 " + size.x, size);
                var owner = new GameObject("Art Breakthrough Hero Test Owner 076 " + size.x);
                var results = owner.AddComponent<M2BattleResultsView072>();
                results.Initialize(host, () => { });
                var battle = AuthoredHallBreakthroughBattle076();
                battle.FinalStateHash = "HALL_BREAKTHROUGH_FINAL_076_" + size.x;

                results.Show(battle);
                yield return null;
                Canvas.ForceUpdateCanvases();

                var root = GameObject.Find("Battle Results Payoff 072");
                var hero = GameObject.Find("Art Breakthrough Hero Stage 076");
                Assert.That(hero, Is.Not.Null.And.Property("activeInHierarchy").True,
                    "The authoritative learning event must own the first resolved-battle frame.");
                Assert.That(root.GetComponent<Image>().color.a,
                    Is.GreaterThanOrEqualTo(M2BattleResultsView072.ResultsVeilOpacity076));
                Assert.That(GameObject.Find("Battle Results Studio Stage 076").GetComponent<CanvasGroup>().alpha,
                    Is.EqualTo(0f),
                    "Generic growth and loot must not compete with the one-time Art hero beat.");
                Assert.That(GameObject.Find("Art Breakthrough Hero Title 076").GetComponent<Text>().text,
                    Is.EqualTo("ART BREAKTHROUGH"));
                Assert.That(GameObject.Find("Art Breakthrough Hero Title 076").GetComponent<Text>().resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(46));
                Assert.That(GameObject.Find("Art Breakthrough Hero Adventurer 076").GetComponent<Text>().text,
                    Is.EqualTo("DAEVEN FELLSTAR"));
                Assert.That(GameObject.Find("Art Breakthrough Hero Art 076").GetComponent<Text>().text,
                    Is.EqualTo("BLOODLESS PRESSURE"));
                Assert.That(GameObject.Find("Art Breakthrough Hero Trigger 076").GetComponent<Text>().text,
                    Is.EqualTo("MASTERED IN BATTLE  •  HAMMERFALL"));
                Assert.That(GameObject.Find("Art Breakthrough Hero Meaning 076").GetComponent<Text>().text,
                    Is.EqualTo("PERMANENTLY LEARNED  •  READY FOR FUTURE BATTLES"));
                Assert.That(GameObject.Find("Art Breakthrough Mastery Threshold 076").GetComponent<Text>().text,
                    Is.EqualTo("DISCOVERY  100 / 100  •  THRESHOLD REACHED"));
                var masteryRail = GameObject.Find("Art Breakthrough Mastery Rail 076").GetComponent<RectTransform>();
                var masteryFill = GameObject.Find("Art Breakthrough Mastery Fill 076").GetComponent<RectTransform>();
                Assert.That(masteryFill.rect.width, Is.EqualTo(masteryRail.rect.width).Within(1f),
                    "The genuine breakthrough should visibly cross its authoritative discovery threshold.");
                var cue = GameObject.Find("Art Breakthrough Rewards Cue 076").GetComponent<Text>();
                Assert.That(cue.text, Is.EqualTo("REWARDS FOLLOW"));
                Assert.That(cue.resizeTextMinSize, Is.GreaterThanOrEqualTo(21));
                var heroRect = hero.GetComponent<RectTransform>().rect;
                Assert.That(heroRect.width, Is.GreaterThanOrEqualTo(size.x * 0.90f));
                Assert.That(heroRect.height, Is.GreaterThanOrEqualTo(size.y * 0.85f));

                yield return new WaitForSecondsRealtime(
                    M2BattleResultsView072.BreakthroughHeroSeconds076 + 0.1f);
                Assert.That(hero.activeSelf, Is.False,
                    "The visible advance cue must resolve automatically into the reward hierarchy.");
                Assert.That(GameObject.Find("Battle Results Studio Stage 076").GetComponent<CanvasGroup>().alpha,
                    Is.EqualTo(1f));

                Object.Destroy(owner);
                Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HidingAnActiveBreakthroughCannotLeaveTheResultStageStuckOnReentry()
        {
            var host = Host("Breakthrough Reentry Test Host 076", new Vector2(1280f, 800f));
            var owner = new GameObject("Breakthrough Reentry Test Owner 076");
            var results = owner.AddComponent<M2BattleResultsView072>();
            results.Initialize(host, () => { });
            var battle = AuthoredHallBreakthroughBattle076();
            battle.FinalStateHash = "BREAKTHROUGH_REENTRY_FINAL_076";

            results.Show(battle);
            yield return null;
            var hero = GameObject.Find("Art Breakthrough Hero Stage 076");
            Assert.That(hero.activeSelf, Is.True);

            results.Hide();
            Assert.That(hero.activeSelf, Is.False);
            results.Show(battle);
            yield return null;

            Assert.That(hero.activeSelf, Is.False,
                "A previously presented authoritative event must not reopen as a coroutine-free dead end.");
            Assert.That(GameObject.Find("Battle Results Studio Stage 076").GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f));

            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstHourVictoriesHaveDistinctStoryCompositionAndLootIdentityAtSupportedResolutions()
        {
            foreach (var size in new[] { new Vector2(1280f, 800f), new Vector2(1920f, 1080f) })
            {
            var host = Host("Battle Story Payoff Test Host 076 " + size.x, size);
            var owner = new GameObject("Battle Story Payoff Test Owner 076 " + size.x);
            var results = owner.AddComponent<M2BattleResultsView072>();
            results.Initialize(host, () => { });

            var headings = new string[3];
            var kickers = new string[3];
            var lootSprites = new string[3];
            var hall = ResolvedBattle();
            hall.BattleId = "BATTLE_FIRST_HOUR072_HALL_BREACH";
            hall.Objective = "Protect the Guild Hall while Kael contains the breach.";
            hall.Reward.EquipmentRewardDisplayName = "First-Gate Sword";
            results.Show(hall);
            yield return null;
            headings[0] = GameObject.Find("Story Reward Heading 076").GetComponent<Text>().text;
            kickers[0] = GameObject.Find("Outcome Kicker 076").GetComponent<Text>().text;
            lootSprites[0] = GameObject.Find("Authored Loot Identity Art 076").GetComponent<Image>().sprite?.name;
            Assert.That(GameObject.Find("Outcome Story Title 076").GetComponent<Text>().text,
                Is.EqualTo("THE HALL STILL STANDS"));
            Assert.That(GameObject.Find("Armory Reward Heading 076").GetComponent<Text>().text,
                Does.Contain("HALL BREACH"));
            Assert.That(GameObject.Find("Next Story Objective Copy 076").GetComponent<Text>().text,
                Does.Contain("Lantern Road"));

            var ambush = ResolvedBattle();
            ambush.BattleId = "BATTLE_CONTRACT_ENCOUNTER071_LANTERN_ROAD_AMBUSH";
            ambush.Objective = "Break the Lantern Road ambush and reopen the way to the patrol.";
            ambush.Reward.EquipmentRewardDisplayName = "Alliance Spear";
            results.Show(ambush);
            yield return null;
            headings[1] = GameObject.Find("Story Reward Heading 076").GetComponent<Text>().text;
            kickers[1] = GameObject.Find("Outcome Kicker 076").GetComponent<Text>().text;
            lootSprites[1] = GameObject.Find("Authored Loot Identity Art 076").GetComponent<Image>().sprite?.name;
            Assert.That(GameObject.Find("Outcome Story Title 076").GetComponent<Text>().text,
                Is.EqualTo("THE ROAD IS OPEN"));
            Assert.That(GameObject.Find("Armory Reward Heading 076").GetComponent<Text>().text,
                Does.Contain("ROAD AMBUSH"));
            Assert.That(GameObject.Find("Next Story Objective Copy 076").GetComponent<Text>().text,
                Does.Contain("missing Lantern Patrol"));

            var gateEater = ResolvedBattle();
            gateEater.BattleId = "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER";
            gateEater.Objective = "Defeat the Gate-Eater before it reaches Skyhome.";
            gateEater.Reward.EquipmentRewardDisplayName = "Skyhome Shield";
            gateEater.Reward.EquipmentRewardValidSlotIds = new[] { "SLOT_OFF_HAND" };
            results.Show(gateEater);
            yield return null;
            headings[2] = GameObject.Find("Story Reward Heading 076").GetComponent<Text>().text;
            kickers[2] = GameObject.Find("Outcome Kicker 076").GetComponent<Text>().text;
            lootSprites[2] = GameObject.Find("Authored Loot Identity Art 076").GetComponent<Image>().sprite?.name;
            Assert.That(GameObject.Find("Armory Reward Heading 076").GetComponent<Text>().text,
                Does.Contain("GATE-EATER"));
            Assert.That(headings.Distinct().Count(), Is.EqualTo(3),
                "The first-hour battles must celebrate different narrative achievements, not repeat one placeholder reward card.");
            Assert.That(kickers.Distinct().Count(), Is.EqualTo(3));
            Assert.That(lootSprites, Is.EqualTo(new[]
            {
                "EQUIPMENT_SWORD", "EQUIPMENT_SPEAR", "EQUIPMENT_SHIELD"
            }), "Each victory must show the authoritative loot family it actually awards.");
            Assert.That(GameObject.Find("Armory Reward Name 076").GetComponent<Text>().text,
                Is.EqualTo("Skyhome Shield"),
                "The presentation must remain honest about the authoritative Gate-Eater inventory item.");
            Assert.That(GameObject.Find("Armory Reward Detail 076").GetComponent<Text>().text,
                Does.Contain("OFF-HAND GEAR"));
            Assert.That(GameObject.Find("Gate-Eater Victory Crest 076").activeInHierarchy, Is.True);

            Object.Destroy(owner);
            Object.Destroy(host.gameObject);
            yield return null;
            }
        }

        private static M2BattleEventView ExactArtEvent076(
            int sequence076,
            string artId076,
            string eventType076)
        {
            return new M2BattleEventView
            {
                Sequence = sequence076,
                Round = 2,
                EventType = eventType076,
                ActorUnionId = "ALLY_1",
                ActorMemberId = "ALLY_MEMBER_1",
                TargetUnionId = "ENEMY_1",
                TargetMemberId = "ENEMY_MEMBER_1",
                ArtId = artId076,
                Amount = 24,
                Text = "The selected exact first-hour Art resolves."
            };
        }

        private static RectTransform Host(string name, Vector2? size = null)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size ?? new Vector2(1920f, 1080f);
            return rect;
        }

        private static M2BattleView AuthoredHallBreakthroughBattle076()
        {
            var battle = ResolvedBattle();
            battle.BattleId = "BATTLE_FIRST_HOUR072_HALL_BREACH";
            battle.Objective = "Protect the Guild Hall while Kael contains the breach.";
            battle.TutorialBreakthroughOccurred = true;
            battle.TutorialBreakthroughSummary = "Daeven Fellstar learned Bloodless Pressure.";
            var daeven = battle.PlayerUnions[0].Members[0];
            daeven.MemberId = "PROC_F85A4CAA747BC8C6";
            daeven.DisplayName = "Daeven Fellstar";
            daeven.ClassName = "Warrior";
            daeven.RaceId = "DEMON_HERITAGE";
            daeven.VisualSeed = "E9459BD93D70BC112D1E101E";
            battle.Events = new[]
            {
                new M2BattleEventView
                {
                    Sequence = 6,
                    Round = 1,
                    EventType = "BREAKTHROUGH",
                    Side = "Player",
                    UnionId = battle.PlayerUnions[0].UnionId,
                    MemberId = daeven.MemberId,
                    ActorUnionId = battle.PlayerUnions[0].UnionId,
                    ActorMemberId = daeven.MemberId,
                    ArtId = "ART_BLOODLESS_PRESSURE",
                    Amount = 100,
                    Text = "Daeven Fellstar learned Bloodless Pressure through Hammerfall. " +
                           "Ready for future battles."
                }
            };
            var reward = battle.Reward.MemberRewards[0];
            reward.MemberId = daeven.MemberId;
            reward.DisplayName = daeven.DisplayName;
            return battle;
        }

        private static M2BattleView ResolvedBattle()
        {
            var battle = Battle(2, 1);
            var spotlight = battle.PlayerUnions[0].Members[0];
            spotlight.MemberId = "REWARD_MEMBER_1";
            spotlight.DisplayName = "Adventurer 1";
            spotlight.ClassName = "Vanguard";
            spotlight.VisualSeed = "RESULT_SPOTLIGHT_074";
            battle.Outcome = "Victory";
            battle.IsResolved = true;
            battle.Round = 3;
            battle.LastResolvedRound = 3;
            battle.Objective = "Break the Gate-Eater line and reopen Lantern Road.";
            battle.Events = new[]
            {
                new M2BattleEventView
                {
                    EventType = "BREAKTHROUGH",
                    MemberId = "REWARD_MEMBER_1",
                    ArtId = "ART_POWER_CUT",
                    Text = "Power Cut"
                }
            };
            battle.LastResolvedRoundEvents = new[]
            {
                new M2BattleEventView { EventType = "PLAYER_HIT" },
                new M2BattleEventView { EventType = "PLAYER_HIT" },
                new M2BattleEventView { EventType = "INTERCEPTION" }
            };
            battle.Reward = new M2BattleRewardView
            {
                EquipmentRewardDisplayName = "Gatewarden Saber",
                EquipmentRewardQualityId = "QUALITY_RARE",
                EquipmentRewardValidSlotIds = new[] { "SLOT_MAIN_HAND" },
                GuildTreasuryXpAward = 240,
                HallEnhancementXpAward = 90,
                GuildPreviousLevel = 2,
                GuildProjectedLevel = 3,
                MemberRewards = Enumerable.Range(1, 9).Select(index => new M2BattleMemberRewardView
                {
                    MemberId = "REWARD_MEMBER_" + index,
                    DisplayName = "Adventurer " + index,
                    PersonalXp = index * 100,
                    PreviousLevel = index,
                    ProjectedLevel = index + (index == 1 ? 1 : 0),
                    LevelsGained = index == 1 ? 1 : 0,
                    StrengthGain = index == 1 ? 1 : 0
                }).ToArray()
            };
            return battle;
        }

        private static M2BattleView Battle(int unionCount, int forecastCount)
        {
            var allies = Enumerable.Range(1, unionCount).Select(index => new M2BattleUnionView
            {
                UnionId = "ALLY_" + index,
                DisplayName = "Opening Union " + index,
                Engagement = "Open",
                Formation = "Shield Wall",
                CurrentAp = 12,
                MaximumAp = 16,
                CanAct = true,
                Members = Enumerable.Range(1, 3).Select(memberIndex => new M2BattleMemberView
                {
                    MemberId = memberIndex == 1
                        ? "ALLY_MEMBER_" + index
                        : "ALLY_MEMBER_" + index + "_" + memberIndex,
                    DisplayName = memberIndex == 1
                        ? "Daeven Fellstar"
                        : memberIndex == 2 ? "Sera Vale" : "Tala Ember",
                    CurrentHp = 100,
                    MaximumHp = 120
                }).ToArray()
            }).ToArray();
            var enemy = new M2BattleUnionView
            {
                UnionId = "ENEMY_1",
                DisplayName = "Hinge-Eater Colossus",
                Engagement = "Open",
                CurrentAp = 9,
                MaximumAp = 12,
                CanAct = true,
                Members = new[]
                {
                    new M2BattleMemberView
                    {
                        MemberId = "ENEMY_MEMBER_1",
                        DisplayName = "Gate-Eater",
                        CurrentHp = 80,
                        MaximumHp = 120
                    }
                }
            };
            var commandIds = new[]
            {
                "CMD_BALANCED", "CMD_ALL_OUT", "CMD_MYSTIC",
                "CMD_GUARD", "CMD_SUPPORT", "CMD_FLANK"
            };
            var commandNames = new[]
            {
                "Attack!", "Attack Using Combat Arts!", "Use Mystic Arts!",
                "Hold the Line!", "Restore Formation!", "Side Strike!"
            };
            var effects = new[]
            {
                "about 40 HP damage", "about 70 HP damage", "about 55 HP damage",
                "interception readied", "Formation +12%", "side strike pressure"
            };
            var forecastArts = new[]
            {
                new[] { "Weighted Swing", "Shield Rush", "Ember Bolt" },
                new[] { "Power Cut", "Piercing Shot", "Ember Bolt" },
                new[] { "Ember Bolt", "Mystic Volley", "Ward Spark" },
                new[] { "Guard Ally", "Shield Wall", "Cover Step" },
                new[] { "Anchor Stance", "Rally", "Assist Ally" },
                new[] { "Quick Cut", "Piercing Shot", "Blindside" }
            };
            var forecasts = Enumerable.Range(1, forecastCount).Select(index =>
            {
                var commandIndex = (index - 1) % commandIds.Length;
                return new M2ForecastView
                {
                    ForecastId = "FORECAST_" + index,
                    UnionId = "ALLY_1",
                    CommandId = commandIds[commandIndex],
                    CommandName = commandNames[commandIndex],
                    TargetId = "ENEMY_1",
                    TargetName = "Hinge-Eater Colossus",
                    SharedApCost = index,
                    CombinedMpCost = commandIndex == 1 ? 3 : commandIndex == 2 ? 8 : 0,
                    ExpectedEffect = effects[commandIndex],
                    Risk = "Standard",
                    MemberActions = new[]
                    {
                        new M2PredictedActionView
                        {
                            ActorName = "Daeven Fellstar",
                            ArtName = forecastArts[commandIndex][0]
                        },
                        new M2PredictedActionView
                        {
                            ActorName = "Sera Vale",
                            ArtName = forecastArts[commandIndex][1]
                        },
                        new M2PredictedActionView
                        {
                            ActorName = "Tala Ember",
                            ArtName = forecastArts[commandIndex][2]
                        }
                    }
                };
            }).ToArray();
            return new M2BattleView
            {
                BattleId = "BATTLE_TEST_074",
                Round = 2,
                PlayerUnions = allies,
                EnemyUnions = new[] { enemy },
                Forecasts = forecasts
            };
        }

        private static M2BattleView TenSixMemberBattle078()
        {
            var battle = Battle(10, 0);
            var names = new[] { "Aster", "Bryn", "Cato", "Dara", "Eris", "Fenn" };
            var arts = new[]
            {
                "Weighted Swing", "Shield Rush", "Ember Bolt",
                "Quiet Mend", "Ward Step", "Piercing Shot"
            };

            for (var unionIndex = 0; unionIndex < battle.PlayerUnions.Count; unionIndex++)
            {
                var unionOrdinal = unionIndex + 1;
                battle.PlayerUnions[unionIndex].Members = Enumerable.Range(0, 6)
                    .Select(memberIndex => new M2BattleMemberView
                    {
                        MemberId = "ALLY_" + unionOrdinal + "_MEMBER_" + (memberIndex + 1),
                        DisplayName = unionIndex == 0 && memberIndex >= 4
                            ? "Valeria"
                            : names[memberIndex],
                        CurrentHp = unionIndex == 0 && memberIndex >= 4
                            ? (memberIndex == 4 ? 999 : 9999)
                            : 70 + unionOrdinal,
                        MaximumHp = unionIndex == 0 && memberIndex >= 4
                            ? (memberIndex == 4 ? 999 : 9999)
                            : 120,
                        ArtGrowthSummary = arts[memberIndex],
                        NextSkillProgress = new M2BattleSkillProgressView
                        {
                            SkillId = "NEXT_ART_" + (memberIndex + 1),
                            DisplayName = "Next " + arts[memberIndex],
                            ProgressKind = "DISCOVERY",
                            CurrentPoints = 34 + memberIndex,
                            RequiredPoints = 80,
                            RequiredLevel = 2,
                            LevelGateMet = true
                        }
                    })
                    .ToArray();
            }

            battle.Forecasts = battle.PlayerUnions.Select(union => new M2ForecastView
            {
                ForecastId = "FORECAST_" + union.UnionId,
                UnionId = union.UnionId,
                CommandId = "CMD_BALANCED",
                CommandName = "Attack!",
                TargetId = "ENEMY_1",
                TargetName = "Hinge-Eater Colossus",
                SharedApCost = 1,
                CombinedMpCost = 0,
                ExpectedEffect = "about 40 HP damage",
                Risk = "Standard",
                MemberActions = union.Members.Select((member, memberIndex) => new M2PredictedActionView
                {
                    ActorMemberId = member.MemberId,
                    ActorName = member.DisplayName,
                    ArtName = union.UnionId == "ALLY_1" && memberIndex >= 4
                        ? "Weighted Strike"
                        : arts[memberIndex],
                    BreakthroughOpportunity = union.UnionId == "ALLY_1" && memberIndex >= 4
                }).ToArray()
            }).ToArray();
            return battle;
        }
    }
}
