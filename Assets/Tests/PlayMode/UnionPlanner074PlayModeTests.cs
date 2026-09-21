using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class UnionPlanner074PlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDownPlanner074()
        {
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<M1FlowPresenter>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;

            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (StringComparer.Ordinal.Equals(canvas.name, "M1 Playable Proof Canvas"))
                    UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticThreeUnionTenMemberStateBuildsOneScreenWithControllerNavigation()
        {
            var host = new GameObject("Union Planner 074 Test Host");
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.Initialize(new PlannerCoordinator074());

            var builder = typeof(M1FlowPresenter).GetMethod(
                "BuildUnionPlanner074",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            Assert.That(builder, Is.Not.Null);
            builder.Invoke(presenter, null);
            yield return null;

            var root = GameObject.Find("Union Planner 074");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<ScrollRect>(true), Is.Empty,
                "The Union planner must never require scrolling at a supported resolution.");

            var buttons = root.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Count(value => value.name.StartsWith(
                "Union Planner Tab ", StringComparison.Ordinal)), Is.EqualTo(3));
            Assert.That(buttons.Count(value => value.name.StartsWith(
                "Union Planner Member Slot ", StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(buttons.Count(value => value.name.StartsWith(
                "Union Planner Reserve Member ", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(buttons.Count(value => value.name.StartsWith(
                "Union Planner Formation FORMATION_", StringComparison.Ordinal)), Is.EqualTo(3));
            Assert.That(buttons.Count(value => value.name.StartsWith(
                "Union Planner Doctrine DOCTRINE_", StringComparison.Ordinal)), Is.EqualTo(3));
            Assert.That(buttons.Count(value => StringComparer.Ordinal.Equals(
                value.name,
                "Union Planner Save And Return 074")), Is.EqualTo(1));

            var text = string.Join("\n", root.GetComponentsInChildren<Text>(true)
                .Select(value => value.text));
            Assert.That(text, Does.Contain("XP TO SPEND  725"));
            Assert.That(text, Does.Contain("FIELD  •  10 UNIONS × 6 = 60"));
            Assert.That(text, Does.Contain("HOUSING III GOAL  •  CAP 75"));
            Assert.That(text, Does.Contain("CHAPTER I REWARD ACTIVE"));
            Assert.That(text, Does.Contain(
                "DRAG TO MOVE OR SWAP  •  CLICK A MEMBER TO SEND TO RESERVE"));
            Assert.That(text, Does.Not.Contain("COHESION GRAPH"));

            var loneReserve = buttons.Single(value => value.name.StartsWith(
                "Union Planner Reserve Member ", StringComparison.Ordinal));
            var loneReserveRect = loneReserve.GetComponent<RectTransform>();
            var loneReserveLabel = loneReserve.GetComponentsInChildren<Text>(true)
                .Single(value => value.gameObject.name.Contains("[Readable 079]"));
            Assert.That(loneReserveRect.anchorMin.x, Is.EqualTo(0.120f).Within(0.001f));
            Assert.That(loneReserveRect.anchorMax.x, Is.EqualTo(0.880f).Within(0.001f));
            Assert.That(loneReserveLabel.text.Split('\n'), Has.Length.EqualTo(2),
                "A lone Hall reserve such as Bessa must read as name plus role/level.");
            Assert.That(loneReserveLabel.text,
                Does.Contain("VANGUARD").Or.Contain("WAYFARER"));
            Assert.That(loneReserveLabel.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
            Assert.That(loneReserveLabel.rectTransform.anchorMax.x,
                Is.LessThanOrEqualTo(0.78f));

            var activeMembers = buttons.Where(value => value.name.StartsWith(
                    "Union Planner Member Slot ", StringComparison.Ordinal) && value.interactable)
                .ToArray();
            Assert.That(activeMembers, Has.Length.EqualTo(3));
            foreach (var card in activeMembers)
            {
                var identity = card.GetComponentsInChildren<Text>(true)
                    .Single(value => value.gameObject.name.StartsWith(
                        "Union Planner Active Member Label 074", StringComparison.Ordinal));
                var portrait = card.GetComponentsInChildren<Image>(true)
                    .First(value => value.name.StartsWith("Portrait Frame ", StringComparison.Ordinal));
                var captionRail = card.transform.Find("Union Planner Member Caption Rail 076")
                    ?.GetComponent<Image>();
                Assert.That(identity.text, Does.Contain("MEMBER"));
                Assert.That(identity.text, Does.Contain("VANGUARD").Or.Contain("WAYFARER"));
                Assert.That(identity.text.Split('\n'), Has.Length.EqualTo(2),
                    card.name + " must be exactly a name line and one role/slot line.");
                Assert.That(identity.text, Does.Not.Contain("CLICK TO"));
                Assert.That(captionRail, Is.Not.Null,
                    card.name + " must put its identity on a high-contrast caption rail.");
                Assert.That(identity.color, Is.EqualTo(Color.white));
                Assert.That(identity.resizeTextMinSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMinimumFontSize074));
                Assert.That(identity.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
                Assert.That(identity.resizeTextMaxSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMaximumFontSize074));
                Assert.That(captionRail.rectTransform.anchorMax.y,
                    Is.LessThan(portrait.rectTransform.anchorMin.y),
                    card.name + " caption rail must stop below the portrait boundary.");
                Assert.That(identity.rectTransform.anchorMax.y,
                    Is.LessThanOrEqualTo(captionRail.rectTransform.anchorMax.y),
                    card.name + " must reserve a separate readable identity band below its portrait.");
                if (StringComparer.Ordinal.Equals(
                        card.name,
                        "Union Planner Member Slot 0 074"))
                    Assert.That(card.transform.Find("Premium Selected Brass Edge"), Is.Not.Null,
                        "The navy caption rail must not replace the leader's gold selection frame.");
            }

            foreach (var button in buttons.Where(value => value.interactable))
                Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit),
                    button.name + " is missing explicit controller navigation.");
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject.name,
                Does.StartWith("Union Planner Tab "));

            UnityEngine.Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwentyMemberRosterUsesPortraitSafeSingleRowReserveCards()
        {
            var host = new GameObject("Union Planner 074 Reserve Stress Host");
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.Initialize(new PlannerCoordinator074(20, 5));

            var screen = typeof(M1FlowPresenter).GetField(
                "_screen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var canvasField = typeof(M1FlowPresenter).GetField(
                "_canvas",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var builder = typeof(M1FlowPresenter).GetMethod(
                "BuildCurrentScreen",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(canvasField, Is.Not.Null);
            Assert.That(builder, Is.Not.Null);
            screen.SetValue(presenter, M1Screen.UnionBuilder);
            builder.Invoke(presenter, null);
            yield return null;
            Canvas.ForceUpdateCanvases();

            var ownedCanvas = canvasField.GetValue(presenter) as Canvas;
            Assert.That(ownedCanvas, Is.Not.Null);
            var root = ownedCanvas.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.gameObject.activeInHierarchy &&
                                 StringComparer.Ordinal.Equals(
                                     value.gameObject.name,
                                     "Union Planner 074"))
                .gameObject;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<ScrollRect>(true), Is.Empty);
            var cards = root.GetComponentsInChildren<Button>(true)
                .Where(value => value.name.StartsWith(
                    "Union Planner Reserve Member ",
                    StringComparison.Ordinal))
                .OrderBy(value => value.name)
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(4),
                "The first finite reserve page must show four members without scrolling.");
            Assert.That(cards.Select(value => Mathf.RoundToInt(
                        value.GetComponent<RectTransform>().anchorMin.x * 1000f))
                    .Distinct().Count(),
                Is.EqualTo(4));
            Assert.That(cards.Select(value => Mathf.RoundToInt(
                        value.GetComponent<RectTransform>().anchorMin.y * 1000f))
                    .Distinct().Count(),
                Is.EqualTo(1));

            foreach (var card in cards)
            {
                var label = card.GetComponentsInChildren<Text>(true)
                    .Single(value => value.gameObject.name.StartsWith(
                        "Union Planner Reserve Card Label 074 [Readable 079]",
                        StringComparison.Ordinal));
                var portrait = card.GetComponentsInChildren<Image>(true)
                    .First(value => value.name.StartsWith("Portrait Frame ", StringComparison.Ordinal));
                var crest = card.GetComponentsInChildren<RectTransform>(true)
                    .First(value => value.name.StartsWith(
                        "Premium Class Crest ", StringComparison.Ordinal));
                Assert.That(label.gameObject.name, Does.Contain("[Readable 079]"));
                Assert.That(label.resizeTextForBestFit, Is.True);
                Assert.That(label.resizeTextMinSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerReserveLabelMinimumFontSize074));
                Assert.That(label.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
                Assert.That(label.resizeTextMaxSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerReserveLabelMaximumFontSize079));
                Assert.That(label.fontSize,
                    Is.InRange(
                        M1FlowPresenter.UnionPlannerReserveLabelMinimumFontSize074,
                        M1FlowPresenter.UnionPlannerReserveLabelMaximumFontSize079),
                    card.name + " escaped the authored 18-22 point range.");
                Assert.That(label.text, Does.Contain("BRASSWHISTLE").Or.Contain("THORNFIELD"));
                Assert.That(portrait.rectTransform.anchorMax.x,
                    Is.LessThan(label.rectTransform.anchorMin.x),
                    card.name + " portrait must never cover its name or class.");
                Assert.That(label.rectTransform.anchorMax.x,
                    Is.LessThanOrEqualTo(crest.anchorMin.x),
                    card.name + " class crest must never cover its name or role.");
                Assert.That(card.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            }

            UnityEngine.Object.Destroy(host);
            yield return null;
        }

        [Test]
        public void PostRescueForceHeadingCountsTheActualDistinctDeployment078()
        {
            var unions = new[]
            {
                new M1UnionView
                {
                    MemberRecruitIds = new[] { "R1", "R2", "R3" }
                },
                new M1UnionView
                {
                    MemberRecruitIds = new[] { "R3", "R4" }
                },
                new M1UnionView
                {
                    MemberRecruitIds = Array.Empty<string>()
                }
            };

            var heading = M1FlowPresenter
                .PostRescueForceOverviewHeadingForVerification078(unions, 1);

            Assert.That(heading,
                Is.EqualTo("GATE-EATER TEAM  •  4 DEPLOYED  •  2 UNIONS  •  1 HOLD THE HALL"));
            Assert.That(heading, Does.Not.Contain("18 ASSIGNED NOW"));
        }

        [UnityTest]
        public IEnumerator ReserveRecruitSupportsDragDropAndControllerSubmitFallback()
        {
            var coordinator = new PlannerCoordinator074(
                10,
                3,
                firstUnionVacancy: true,
                roleColorRoster: true);
            var presenter = BuildPlannerForInput074(coordinator, "Union Planner 074 Input Host");
            yield return null;

            var reserve = UnityEngine.Object.FindObjectsByType<UnionPlannerRecruitDrag074>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Single(value => StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    "RECRUIT_074_2"));
            var reserveButton = reserve.GetComponent<Button>();
            var openSlot = GameObject.Find("Union Planner Member Slot 5 074")
                .GetComponent<UnionPlannerDropTarget074>();
            Assert.That(reserveButton, Is.Not.Null);
            Assert.That(reserveButton.interactable, Is.True,
                "Drag is an extra shortcut; the same recruit must remain a normal submit Button.");
            Assert.That(reserve.CanDrag, Is.True);
            Assert.That(openSlot, Is.Not.Null);
            Assert.That(openSlot.IsAvailable, Is.True);
            Assert.That(openSlot.UnionIndex, Is.EqualTo(0));
            Assert.That(openSlot.SlotIndex, Is.EqualTo(5));

            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerDrag = reserve.gameObject,
                position = Vector2.zero
            };
            reserve.OnBeginDrag(pointer);
            Assert.That(UnionPlannerRecruitDrag074.ActiveDrag, Is.SameAs(reserve));
            Assert.That(GameObject.Find("Union Planner Drag Ghost 074"), Is.Not.Null);
            var highlight = openSlot.transform.Find("Union Planner Drop Highlight 074")
                .GetComponent<Image>();
            Assert.That(highlight.color.a, Is.GreaterThan(0f),
                "Every legal destination should light up as soon as a drag begins.");
            openSlot.OnPointerEnter(pointer);
            Assert.That(highlight.color.a, Is.GreaterThanOrEqualTo(0.30f));
            openSlot.OnDrop(pointer);

            Assert.That(coordinator.AssignmentCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastAssignedRecruitId, Is.EqualTo("RECRUIT_074_2"));
            Assert.That(coordinator.LastAssignedUnionIndex, Is.EqualTo(0));
            Assert.That(coordinator.LastAssignedSlotIndex, Is.EqualTo(5));
            Assert.That(openSlot.LastDropAccepted, Is.True);
            yield return null;
            Assert.That(UnionPlannerRecruitDrag074.ActiveDrag, Is.Null);
            Assert.That(GameObject.Find("Union Planner Drag Ghost 074"), Is.Null);

            var fallback = UnityEngine.Object.FindObjectsByType<UnionPlannerRecruitDrag074>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Single(value => StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    "RECRUIT_074_2"))
                .GetComponent<Button>();
            fallback.Select();
            EventSystem.current.SetSelectedGameObject(fallback.gameObject);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(fallback.gameObject));
            ExecuteEvents.Execute(
                fallback.gameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
            Assert.That(coordinator.AssignmentCalls, Is.EqualTo(2),
                "Controller/keyboard Submit must use the original one-click assignment path.");
            Assert.That(EventSystem.current.currentSelectedGameObject.name,
                Does.StartWith("Union Planner Tab "),
                "A successful submit rebuilds the screen and restores deterministic controller focus.");

            UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AssignedRecruitDragUsesAtomicCrossUnionMoveWithoutUnassigning()
        {
            var coordinator = new PlannerCoordinator074(
                10,
                3,
                roleColorRoster: true);
            var presenter = BuildPlannerForInput074(
                coordinator,
                "Union Planner 074 Assigned Drag Host");
            yield return null;

            var member = GameObject.Find("Union Planner Member Slot 0 074")
                .GetComponent<UnionPlannerRecruitDrag074>();
            var growingUnionTab = GameObject.Find("Union Planner Tab 1 074")
                .GetComponent<UnionPlannerDropTarget074>();
            Assert.That(member, Is.Not.Null);
            Assert.That(member.IsAssigned, Is.True);
            Assert.That(growingUnionTab.IsAvailableForReserve, Is.True,
                "A three-member Union still has three campaign-growth slots.");
            Assert.That(growingUnionTab.IsAvailableForAssigned, Is.True,
                "An assigned member can move atomically into the next open slot.");

            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerDrag = member.gameObject,
                position = Vector2.zero
            };
            member.OnBeginDrag(pointer);
            growingUnionTab.OnDrop(pointer);

            Assert.That(coordinator.AssignmentCalls, Is.EqualTo(1));
            Assert.That(coordinator.UnassignmentCalls, Is.Zero,
                "Direct reassignment is atomic; a failed second step must never strand the member.");
            Assert.That(coordinator.LastAssignedRecruitId, Is.EqualTo("RECRUIT_074_0"));
            Assert.That(coordinator.LastAssignedUnionIndex, Is.EqualTo(1));
            Assert.That(coordinator.LastAssignedSlotIndex, Is.EqualTo(3),
                "Dropping onto a three-member Union appends into its fourth slot.");
            Assert.That(growingUnionTab.LastDropAccepted, Is.True);

            UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoleColoredThumbnailsAndDropLayoutStayReadableAtSupportedResolutions()
        {
            var coordinator = new PlannerCoordinator074(
                10,
                3,
                firstUnionVacancy: true,
                roleColorRoster: true);
            var presenter = BuildPlannerForInput074(coordinator, "Union Planner 074 Role Layout Host");
            yield return null;

            var supported = new[]
            {
                new Vector2(1280f, 800f),
                new Vector2(1920f, 1080f)
            };
            foreach (var resolution in supported)
            {
                ConfigureCanvasForResolution074(resolution.x, resolution.y);
                var root = GameObject.Find("Union Planner 074");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<ScrollRect>(true), Is.Empty);

                var allDraggables = root.GetComponentsInChildren<UnionPlannerRecruitDrag074>(true);
                var reserveDraggables = allDraggables.Where(value => !value.IsAssigned).ToArray();
                var assignedDraggables = allDraggables.Where(value => value.IsAssigned).ToArray();
                Assert.That(reserveDraggables, Has.Length.EqualTo(2));
                Assert.That(assignedDraggables, Has.Length.EqualTo(2));
                Assert.That(reserveDraggables.Select(value => value.RoleDesignation),
                    Is.EquivalentTo(new[] { "WARRIOR", "MAGE" }),
                    "Reserve roles should be grouped and identified without reading every portrait.");
                foreach (var draggable in reserveDraggables)
                {
                    var card = draggable.GetComponent<RectTransform>();
                    var roleRail = draggable.transform.Find(
                            "Union Planner Role Color " + draggable.RoleDesignation + " 074")
                        ?.GetComponent<Image>();
                    Assert.That(roleRail, Is.Not.Null, draggable.RecruitId);
                    Assert.That(roleRail.color, Is.EqualTo(draggable.RoleColor));
                    Assert.That(roleRail.raycastTarget, Is.False);
                    Assert.That(roleRail.rectTransform.anchorMax.x,
                        Is.LessThanOrEqualTo(0.30f),
                        "Reserve role color belongs to the thumbnail column, not over its name.");
                    Assert.That(card.rect.width, Is.GreaterThanOrEqualTo(115f));
                    Assert.That(card.rect.height, Is.GreaterThanOrEqualTo(80f));
                }

                var occupiedMembers = root.GetComponentsInChildren<Button>(true)
                    .Where(value => value.interactable && value.name.StartsWith(
                        "Union Planner Member Slot ",
                        StringComparison.Ordinal))
                    .ToArray();
                Assert.That(occupiedMembers, Has.Length.EqualTo(2));
                foreach (var member in occupiedMembers)
                {
                    var roleRail = member.GetComponentsInChildren<Image>(true)
                        .Single(value => value.name.StartsWith(
                            "Union Planner Role Color ",
                            StringComparison.Ordinal));
                    var caption = member.GetComponentsInChildren<Text>(true)
                        .Single(value => value.name.StartsWith(
                            "Union Planner Active Member Label 074",
                            StringComparison.Ordinal));
                    Assert.That(caption.name,
                        Does.Contain("[Compact Fixed 074]")
                            .And.Contain("[Studio Caption Rail 076]"),
                        "Member captions must retain the compact scaling exemption and the studio rail contract.");
                    Assert.That(caption.resizeTextMinSize,
                        Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMinimumFontSize074));
                    Assert.That(caption.resizeTextMaxSize,
                        Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMaximumFontSize074));
                    var normalizedCaption = caption.text.Replace('\u00A0', ' ');
                    if (StringComparer.Ordinal.Equals(member.name, "Union Planner Member Slot 0 074"))
                        Assert.That(normalizedCaption,
                            Is.EqualTo("GARA REDTAIL\nGUARDIAN  •  LEADER"));
                    else if (StringComparer.Ordinal.Equals(member.name, "Union Planner Member Slot 1 074"))
                        Assert.That(normalizedCaption,
                            Is.EqualTo("DAEVEN FELLSTAR\nWARRIOR  •  SLOT 2"));

                    var captionRect = caption.rectTransform.rect;
                    var captionSettings = caption.GetGenerationSettings(captionRect.size);
                    var renderedCaption = caption.cachedTextGenerator;
                    Assert.That(renderedCaption.Populate(caption.text, captionSettings), Is.True);
                    Assert.That(renderedCaption.lines.Count, Is.EqualTo(2),
                        resolution + " " + member.name + " rendered an unintended wrapped or clipped line.");
                    Assert.That(renderedCaption.fontSizeUsedForBestFit,
                        Is.GreaterThanOrEqualTo(
                            M1FlowPresenter.UnionPlannerMemberLabelMinimumFontSize074));
                    Assert.That(renderedCaption.characterCountVisible,
                        Is.GreaterThanOrEqualTo(caption.text.Count(character => character != '\n')),
                        resolution + " " + member.name + " truncated its leader/slot suffix.");
                    var captionLineSettings = captionSettings;
                    captionLineSettings.resizeTextForBestFit = false;
                    captionLineSettings.fontSize = renderedCaption.fontSizeUsedForBestFit;
                    captionLineSettings.horizontalOverflow = HorizontalWrapMode.Overflow;
                    foreach (var line in caption.text.Split('\n'))
                    {
                        var preferredWidth = caption.cachedTextGeneratorForLayout
                            .GetPreferredWidth(line, captionLineSettings) / caption.pixelsPerUnit;
                        Assert.That(preferredWidth, Is.LessThanOrEqualTo(captionRect.width + 1f),
                            resolution + " line '" + line.Replace('\u00A0', ' ') +
                            "' needs " + preferredWidth + " px but owns " + captionRect.width + " px.");
                    }
                    var captionPreferredHeight = caption.cachedTextGeneratorForLayout
                        .GetPreferredHeight(caption.text, captionSettings) / caption.pixelsPerUnit;
                    Assert.That(captionPreferredHeight,
                        Is.LessThanOrEqualTo(captionRect.height + 1f),
                        resolution + " " + member.name + " clips vertically.");
                    Assert.That(roleRail.rectTransform.anchorMin.y,
                        Is.GreaterThan(caption.rectTransform.anchorMax.y),
                        "Role color must mark the portrait without covering the member caption.");
                    Assert.That(roleRail.rectTransform.rect.height, Is.GreaterThanOrEqualTo(4f));
                    var expectedRole = M1FlowPresenter
                        .UnionPlannerRoleDesignationForVerification074(
                            caption.text.Split('\n')[1].Split('•')[0].Trim());
                    Assert.That(member.GetComponentsInChildren<Transform>(true).Any(value =>
                            value.name.StartsWith(
                                "Premium Class Crest " + expectedRole,
                                StringComparison.Ordinal)),
                        Is.True,
                        "Color is reinforced with a readable role crest for color-blind players.");
                }

                var openSlot = GameObject.Find("Union Planner Member Slot 2 074")
                    .GetComponent<UnionPlannerDropTarget074>();
                var sixthSlot = GameObject.Find("Union Planner Member Slot 5 074")
                    .GetComponent<UnionPlannerDropTarget074>();
                var selectedUnion = GameObject.Find("Union Planner Selected Team 074")
                    .GetComponent<UnionPlannerDropTarget074>();
                Assert.That(openSlot.IsAvailable, Is.True);
                Assert.That(sixthSlot.IsAvailable, Is.True,
                    "The campaign-size Union must expose all six drop targets.");
                Assert.That(selectedUnion.IsAvailable, Is.True);
                Assert.That(openSlot.transform.Find("Union Planner Drop Highlight 074"), Is.Not.Null);
                Assert.That(selectedUnion.transform.Find("Union Planner Drop Highlight 074"), Is.Not.Null);
                Assert.That(openSlot.GetComponent<RectTransform>().rect.width,
                    Is.GreaterThanOrEqualTo(185f));
                Assert.That(sixthSlot.GetComponent<RectTransform>().rect.height,
                    Is.GreaterThanOrEqualTo(110f));
            }

            UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignCapacitySummaryFitsAtSupportedResolutions078()
        {
            var presenter = BuildPlannerForInput074(
                new PlannerCoordinator074(),
                "Union Planner 074 Capacity Summary Host");
            yield return null;

            var supported = new[]
            {
                new Vector2(1280f, 800f),
                new Vector2(1920f, 1080f)
            };
            foreach (var resolution in supported)
            {
                ConfigureCanvasForResolution074(resolution.x, resolution.y);
                var label = GameObject.Find("Union Planner 074")
                    .GetComponentsInChildren<Text>(true)
                    .Single(value => value.name.StartsWith(
                        "Union Planner Spendable Treasury XP Text 074",
                        StringComparison.Ordinal));
                var rect = label.rectTransform.rect;
                var settings = label.GetGenerationSettings(rect.size);
                var rendered = label.cachedTextGenerator;

                Assert.That(rendered.Populate(label.text, settings), Is.True);
                Assert.That(label.text.Split('\n'), Has.Length.EqualTo(3));
                Assert.That(label.text,
                    Does.Contain("FIELD  •  10 UNIONS × 6 = 60").And.Contain(
                        "HOUSING III GOAL  •  CAP 75"));
                Assert.That(rendered.lines.Count, Is.EqualTo(3),
                    resolution + " capacity summary rendered an unintended wrapped line.");
                Assert.That(rendered.fontSizeUsedForBestFit,
                    Is.GreaterThanOrEqualTo(label.resizeTextMinSize),
                    resolution + " capacity summary fell below its accessibility floor.");

                var lineSettings = settings;
                lineSettings.resizeTextForBestFit = false;
                lineSettings.fontSize = rendered.fontSizeUsedForBestFit;
                lineSettings.horizontalOverflow = HorizontalWrapMode.Overflow;
                foreach (var line in label.text.Split('\n'))
                {
                    var preferredWidth = label.cachedTextGeneratorForLayout
                        .GetPreferredWidth(line, lineSettings) / label.pixelsPerUnit;
                    Assert.That(preferredWidth, Is.LessThanOrEqualTo(rect.width + 1f),
                        resolution + " line '" + line + "' needs " + preferredWidth +
                        " px but owns " + rect.width + " px.");
                }

                var preferredHeight = label.cachedTextGeneratorForLayout
                    .GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                Assert.That(preferredHeight, Is.LessThanOrEqualTo(rect.height + 1f),
                    resolution + " capacity summary needs " + preferredHeight +
                    " px but owns " + rect.height + " px.");
            }

            UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SixCanonicalOpeningUnionsRenderTwoCleanTabPagesAtSupportedResolutions()
        {
            var host = new GameObject("Union Planner 074 Canonical Tabs Host");
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.Initialize(new PlannerCoordinator074(20, 6, true));

            var screen = typeof(M1FlowPresenter).GetField(
                "_screen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var builder = typeof(M1FlowPresenter).GetMethod(
                "BuildCurrentScreen",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(builder, Is.Not.Null);
            screen.SetValue(presenter, M1Screen.UnionBuilder);
            builder.Invoke(presenter, null);
            yield return null;
            ConfigureCanvasForResolution074(1280f, 800f);

            AssertCanonicalTabPage074(new[]
            {
                "GATEWARDENS", "LANTERN SPEAR", "WAYFINDERS"
            });
            ConfigureCanvasForResolution074(1920f, 1080f);
            AssertCanonicalTabPage074(new[]
            {
                "GATEWARDENS", "LANTERN SPEAR", "WAYFINDERS"
            });

            var next = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Single(value => StringComparer.Ordinal.Equals(
                    value.name,
                    "Next Union Tab Page 074"));
            Assert.That(next.interactable, Is.True);
            next.onClick.Invoke();
            yield return null;
            ConfigureCanvasForResolution074(1920f, 1080f);

            AssertCanonicalTabPage074(new[]
            {
                "VANGUARD", "WAYGLASS", "REARGUARD"
            });
            var createSeventh = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Single(value => StringComparer.Ordinal.Equals(
                    value.name,
                    "Next Union Tab Page 074"));
            Assert.That(createSeventh.interactable, Is.True);
            Assert.That(createSeventh.GetComponentInChildren<Text>().text, Is.EqualTo("+"),
                "After the sixth plan, the same calm pager exposes plan seven instead of hiding campaign capacity.");
            ConfigureCanvasForResolution074(1280f, 800f);
            AssertCanonicalTabPage074(new[]
            {
                "VANGUARD", "WAYGLASS", "REARGUARD"
            });

            UnityEngine.Object.Destroy(host);
            yield return null;
        }

        private static void ConfigureCanvasForResolution074(float width, float height)
        {
            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Single(value => StringComparer.Ordinal.Equals(
                    value.name,
                    "M1 Playable Proof Canvas"));
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            var size = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(width, height);
            var rect = canvas.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            rect.localScale = Vector3.one;
            var root = GameObject.Find("Union Planner 074").GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();
        }

        private static M1FlowPresenter BuildPlannerForInput074(
            PlannerCoordinator074 coordinator,
            string hostName)
        {
            var host = new GameObject(hostName);
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.Initialize(coordinator);
            var screen = typeof(M1FlowPresenter).GetField(
                "_screen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var builder = typeof(M1FlowPresenter).GetMethod(
                "BuildCurrentScreen",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(builder, Is.Not.Null);
            screen.SetValue(presenter, M1Screen.UnionBuilder);
            builder.Invoke(presenter, null);
            return presenter;
        }

        private static void AssertCanonicalTabPage074(string[] expectedNames)
        {
            var tabs = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith(
                    "Union Planner Tab ",
                    StringComparison.Ordinal))
                .OrderBy(value => value.name)
                .ToArray();
            Assert.That(tabs, Has.Length.EqualTo(3));
            var actualNames = new string[tabs.Length];
            for (var index = 0; index < tabs.Length; index++)
            {
                var label = tabs[index].GetComponentInChildren<Text>();
                Assert.That(label, Is.Not.Null, tabs[index].name);
                Assert.That(label.text.Count(character => character == '\n'), Is.EqualTo(1),
                    tabs[index].name + " must be exactly a name line plus one readiness line.");
                var lines = label.text.Split('\n');
                actualNames[index] = lines[0].Replace("◆", string.Empty).Trim();
                Assert.That(lines[1], Is.EqualTo("3/6  •  READY"));
                var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                var rendered = label.cachedTextGenerator;
                Assert.That(rendered.Populate(label.text, settings), Is.True);
                Assert.That(rendered.lines.Count, Is.EqualTo(2),
                    tabs[index].name + " rendered an unintended wrapped line.");
                var preferredHeight = label.cachedTextGeneratorForLayout
                    .GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                Assert.That(preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    tabs[index].name + " wraps or clips at a supported resolution.");
            }
            Assert.That(actualNames, Is.EqualTo(expectedNames));
        }

        private sealed class PlannerCoordinator074 : IM1PresentationCoordinator
        {
            public PlannerCoordinator074(
                int recruitCount = 10,
                int unionCount = 3,
                bool canonicalOpeningNames = false,
                bool firstUnionVacancy = false,
                bool roleColorRoster = false)
            {
                if (recruitCount < unionCount * 3)
                    throw new ArgumentOutOfRangeException(nameof(recruitCount));
                var recruits = Enumerable.Range(0, recruitCount)
                    .Select(index => new M1RecruitLoadoutView
                    {
                        RecruitId = "RECRUIT_074_" + index,
                        RaceId = index % 2 == 0 ? "HUMAN" : "GOBLIN",
                        VisualSeed = "UNION_PLANNER_074_" + index,
                        DisplayName = roleColorRoster && index == 0
                            ? "Gara Redtail"
                            : roleColorRoster && index == 1
                                ? "Daeven Fellstar"
                            : index >= unionCount * 3
                            ? index % 2 == 0 ? "Presa Brasswhistle" : "Aves Thornfield"
                            : "Member " + (index + 1),
                        ObservedClass = roleColorRoster
                            ? index == 0
                                ? "Guardian"
                                : index == 1
                                    ? "Warrior"
                                    : new[] { "Warrior", "Healer", "Mage" }[index % 3]
                            : index % 3 == 0 ? "Vanguard" : "Wayfarer",
                        ClassSymbol = roleColorRoster
                            ? new[] { "⚔", "+", "✦" }[index % 3]
                            : index % 3 == 0 ? "V" : "W",
                        Level = 3,
                        IsLegal = true
                    })
                    .ToArray();
                var unionNames = new[]
                {
                    "Lantern Union", "Bell Union", "Gate Union",
                    "Dawn Union", "Ward Union", "Sky Union"
                };
                var formations = new[]
                {
                    "FORMATION_SHIELD_WALL", "FORMATION_WEDGE", "FORMATION_SKIRMISH_LINE"
                };
                var doctrines = new[]
                {
                    "DOCTRINE_BALANCED", "DOCTRINE_AGGRESSIVE", "DOCTRINE_GUARDIAN"
                };
                var unions = Enumerable.Range(0, unionCount)
                    .Select(index =>
                    {
                        var unionId = canonicalOpeningNames
                            ? "UNION_OPENING_0" + (index + 1)
                            : "UNION_074_" + index;
                        var displayName = canonicalOpeningNames
                            ? M1UnionIdentity076.Resolve(
                                unionId,
                                "Opening Union " + (index + 1),
                                index)
                            : unionNames[index % unionNames.Length];
                        return Union074(
                            index,
                            unionId,
                            displayName,
                            recruits.Skip(index * 3)
                                .Take(firstUnionVacancy && index == 0 ? 2 : 3)
                                .Select(value => value.RecruitId)
                                .ToArray(),
                            formations[index % formations.Length],
                            doctrines[index % doctrines.Length]);
                    })
                    .ToArray();
                State = new M1PresentationState
                {
                    HasCampaign = true,
                    HasSave = true,
                    ResumeScreen = M1Screen.UnionBuilder,
                    GuildmasterName = "Planner",
                    GuildLevel = 2,
                    GuildXpIntoCurrentLevel = 175,
                    GuildXpRequiredForNextLevel = 500,
                    TreasuryXp = 725,
                    Recruits = recruits,
                    Unions = unions,
                    Formations = Enumerable.Range(0, 8)
                        .Select(index => Choice074(
                            index == 0 ? "FORMATION_SHIELD_WALL" :
                            index == 1 ? "FORMATION_WEDGE" :
                            index == 2 ? "FORMATION_SKIRMISH_LINE" : "FORMATION_LATER_" + index,
                            "Formation " + index))
                        .ToArray(),
                    Doctrines = Enumerable.Range(0, 9)
                        .Select(index => Choice074(
                            index == 0 ? "DOCTRINE_BALANCED" :
                            index == 1 ? "DOCTRINE_AGGRESSIVE" :
                            index == 2 ? "DOCTRINE_GUARDIAN" : "DOCTRINE_LATER_" + index,
                            "Intent " + index))
                        .ToArray(),
                    OpeningEquipmentLegal = true,
                    OpeningUnionsLegal = true,
                    TwoUnionsLegal = true,
                    UsedUnionCount = unionCount,
                    MaximumUnionPlanCount = 10,
                    SaveReloadVerified = true
                };
            }

            public event Action Changed;
            public M1PresentationState State { get; }
            public int AssignmentCalls { get; private set; }
            public string LastAssignedRecruitId { get; private set; }
            public int LastAssignedUnionIndex { get; private set; } = -1;
            public int LastAssignedSlotIndex { get; private set; } = -1;
            public int UnassignmentCalls { get; private set; }

            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Pass074();
            public M1CommandResult SignRecruit(string recruitId) => Pass074();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => Pass074();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => Pass074();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => Pass074();
            public M1CommandResult CompleteEquipmentReview() => Pass074();
            public M1CommandResult AddUnion() => Pass074();
            public M1CommandResult RemoveUnion(int unionIndex) => Pass074();
            public M1CommandResult AssignRecruitToUnion(
                string recruitId,
                int unionIndex,
                int slotIndex)
            {
                AssignmentCalls++;
                LastAssignedRecruitId = recruitId;
                LastAssignedUnionIndex = unionIndex;
                LastAssignedSlotIndex = slotIndex;
                return Pass074();
            }
            public M1CommandResult UnassignRecruitFromUnion(string recruitId)
            {
                UnassignmentCalls++;
                return Pass074();
            }
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => Pass074();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => Pass074();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => Pass074();
            public M1CommandResult SaveAndReloadProof() => Pass074();

            private static M1CommandResult Pass074() => M1CommandResult.Success("Saved for test.");

#pragma warning disable 67
            private void PreserveEventForInterface074() => Changed?.Invoke();
#pragma warning restore 67

            private static M1UnionView Union074(
                int index,
                string unionId,
                string name,
                string[] members,
                string formation,
                string doctrine)
            {
                return new M1UnionView
                {
                    Index = index,
                    UnionId = unionId,
                    DisplayName = name,
                    MemberRecruitIds = members,
                    LeaderRecruitId = members[0],
                    FormationId = formation,
                    DoctrineId = doctrine,
                    IsLegal = true,
                    LegalitySummary = "Ready"
                };
            }

            private static M1ChoiceView Choice074(string id, string name)
            {
                return new M1ChoiceView
                {
                    Id = id,
                    DisplayName = name,
                    Summary = "Short effect"
                };
            }
        }
    }
}
