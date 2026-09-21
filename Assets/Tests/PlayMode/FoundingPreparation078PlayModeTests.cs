using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FoundingPreparation078PlayModeTests
    {
        [TestCase(1280f, 800f)]
        [TestCase(1920f, 1080f)]
        public void CompanyBriefingKeepsLeaderClassBelowPrimaryTeamJob078(
            float width,
            float height)
        {
            var teamJob = M1FlowPresenter
                .FoundingCompanyTeamJobRibbonRectForVerification078;
            var leaderClass = M1FlowPresenter
                .FoundingCompanyLeaderClassPlateRectForVerification078;
            var cardHeight = height *
                             M1FlowPresenter.FoundingCompanyUnionCardHeightFraction078;
            var verticalGap = (teamJob.yMin - leaderClass.yMax) * cardHeight;

            Assert.That(width, Is.GreaterThan(0f));
            Assert.That(leaderClass.yMax, Is.LessThan(teamJob.yMin),
                "The leader's class is secondary and must sit wholly below the primary team job.");
            Assert.That(verticalGap, Is.GreaterThanOrEqualTo(8f),
                "Team job and leader class need at least eight physical pixels of separation at every supported resolution.");
            Assert.That(leaderClass.xMin, Is.EqualTo(teamJob.xMin).Within(0.001f));
            Assert.That(leaderClass.xMax, Is.EqualTo(teamJob.xMax).Within(0.001f));
        }

        [Test]
        public void CompatibleAutoFillKeepsChosenLeadAndSafelyDistributesEveryFounder078()
        {
            var founders = new[]
            {
                Recruit("GUARD", "Guardian"),
                Recruit("WARRIOR", "Warrior"),
                Recruit("RANGER", "Ranger"),
                Recruit("ROGUE", "Rogue"),
                Recruit("MAGE", "Mage"),
                Recruit("PRIEST", "Priest")
            };

            var groups = M1FlowPresenter.BuildFoundingRoleGroupsForVerification078(
                founders,
                "RANGER",
                2);

            Assert.That(groups, Has.Count.EqualTo(3));
            Assert.That(groups.All(value => value.Count == 2), Is.True);
            Assert.That(groups[2][0], Is.EqualTo("RANGER"),
                "The player's selected lead and destination must survive auto-fill.");
            Assert.That(groups[2], Does.Contain("ROGUE"),
                "The chosen lead should receive a compatible teammate when capacity permits.");
            Assert.That(groups.SelectMany(value => value)
                    .Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(6));

            foreach (var group in groups)
            {
                var bands = group.Select(id =>
                        M1FlowPresenter.FoundingRoleBandForVerification078(
                            founders.First(value => value.RecruitId == id)
                                .ObservedClass))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                Assert.That(bands, Has.Length.EqualTo(1),
                    "The safe recommendation should form three compatible two-person groups.");
            }

            Assert.That(NormalUnionPlanRules.MaximumMembersPerUnion, Is.EqualTo(6));
            Assert.That(NormalUnionPlanRules.MaximumPlanCount, Is.EqualTo(10));
            Assert.That(NormalUnionPlanRules.MaximumDeployedUnionCount, Is.EqualTo(10));
        }

        [TestCase("EQ_PROC_WARD_SABER", "SIGI_31A3_MAIN",
            "SLOT_MAIN_HAND", "Ward Saber")]
        [TestCase("EQ_PROC_WARD_COAT", "SIGI_31A3_BODY",
            "SLOT_BODY_ARMOR", "Ward Coat")]
        [TestCase("Ashwood Medic Staff", "EQ_ASHWOOD_MEDIC_STAFF",
            "SLOT_MAIN_HAND", "Ashwood Medic Staff")]
        [TestCase(null, "SIGI_31A3_BODY", "SLOT_BODY_ARMOR", "Starter Armor")]
        [TestCase(null, null, "SLOT_BODY_ARMOR", "Starter Armor")]
        public void FoundingEquipmentUsesPlayerFacingNames078(
            string displayedName,
            string itemId,
            string slotId,
            string expected)
        {
            var actual = M1FlowPresenter
                .FoundingEquipmentDisplayNameForVerification078(
                    displayedName,
                    itemId,
                    slotId);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Does.Not.StartWith("EQ_"));
        }

        [Test]
        public void FoundingGoldSelectionsAndDisabledActionsRemainHighContrast078()
        {
            var selectedContrast = ContrastRatio078(
                M1FlowPresenter.FoundingGoldChoiceSurfaceForVerification078(true),
                M1FlowPresenter.FoundingGoldChoiceLabelForVerification078(true));
            var pressedContrast = ContrastRatio078(
                M1FlowPresenter.FoundingGoldChoicePressedSurfaceForVerification078(),
                M1FlowPresenter.FoundingGoldChoiceLabelForVerification078(true));
            var disabledContrast = ContrastRatio078(
                M1FlowPresenter.FoundingDisabledActionSurfaceForVerification078(),
                M1FlowPresenter.FoundingDisabledActionLabelForVerification078());

            Assert.That(selectedContrast, Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(pressedContrast, Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(disabledContrast, Is.GreaterThanOrEqualTo(4.5f));
        }

        [TestCase("FORMATION_SHIELD_WALL", "Shield Wall",
            "STAND CLOSE  •  PROTECT EACH OTHER")]
        [TestCase("FORMATION_WEDGE", "Wedge",
            "PRESS THE CENTER  •  BREAK THE OPENING")]
        [TestCase("FORMATION_SKIRMISH_LINE", "Skirmish Line",
            "SPREAD OUT  •  RESPOND QUICKLY")]
        public void FoundingFormationCardsTranslateAuthorityCopyAndShowSixPositions078(
            string formationId,
            string displayName,
            string expectedPromise)
        {
            var formation = new M1ChoiceView
            {
                Id = formationId,
                DisplayName = displayName,
                Summary = "OPENING: NO RAW STAT CHANGE • M2 ROLE: INTERNAL"
            };

            var promise = M1FlowPresenter
                .FriendlyFormationPromiseForVerification078(formation);
            var diagram = M1FlowPresenter
                .UnionPlannerFormationDiagramForVerification078(formationId);

            Assert.That(promise, Is.EqualTo(expectedPromise));
            Assert.That(promise, Does.Not.Contain("M2 ROLE"));
            Assert.That(promise, Does.Not.Contain("RAW STAT"));
            Assert.That(diagram.Count(value => value == '●'), Is.EqualTo(6),
                "Every starter diagram must preview all six possible Union positions.");
        }

        [TestCase(FoundingPreparationStage078.ChooseEquipmentPriority,
            FoundingPreparationStage078.ChooseFieldLead, "EDIT FIELD LEAD")]
        [TestCase(FoundingPreparationStage078.PlaceFieldLead,
            FoundingPreparationStage078.ChooseEquipmentPriority, "EDIT EQUIPMENT")]
        [TestCase(FoundingPreparationStage078.ChooseFormation,
            FoundingPreparationStage078.PlaceFieldLead, "EDIT UNION")]
        [TestCase(FoundingPreparationStage078.ConfirmRescueTeam,
            FoundingPreparationStage078.ChooseFormation, "EDIT FORMATION")]
        public void StepsTwoThroughFiveExposeOnePreviousDecision078(
            FoundingPreparationStage078 current,
            FoundingPreparationStage078 expectedPrevious,
            string expectedLabel)
        {
            Assert.That(
                M1FlowPresenter.FoundingCanEditPreviousForVerification078(current),
                Is.True);
            Assert.That(
                M1FlowPresenter.FoundingPreviousStageForVerification078(current),
                Is.EqualTo(expectedPrevious));
            Assert.That(
                M1FlowPresenter.FoundingEditPreviousLabelForVerification078(current),
                Does.Contain(expectedLabel));
        }

        [UnityTest]
        public IEnumerator RealRuntimeRequiresFiveGuildmasterDecisionsBeforeRescue078()
        {
            var savePath = Path.Combine(
                Path.GetTempPath(),
                "sd_founding_preparation_078_" + Guid.NewGuid().ToString("N") +
                ".json");
            M1FlowPresenter presenter = null;
            try
            {
                var contentRoot = Path.Combine(
                    Application.streamingAssetsPath,
                    "Authority",
                    "CONTENT");
                var coordinator = new M1RuntimeCoordinator(contentRoot, savePath);
                var host = new GameObject("Founding Preparation Presenter 078");
                presenter = host.AddComponent<M1FlowPresenter>();
                presenter.Initialize(coordinator);
                yield return null;

                ClickVisibleLabel078("START NEW GUILD");
                yield return null;
                var arrival = UnityEngine.Object
                    .FindFirstObjectByType<WalkableSkyhomeArrival071>();
                Assert.That(arrival, Is.Not.Null);
                Assert.That(arrival.TeleportToGuildHallForVerification071(), Is.True);
                arrival.ApplyInteractionForVerification071();
                yield return null;
                ClickVisibleLabel078("SIGN THE CHARTER");
                yield return null;

                Assert.That(GameObject.Find(
                    M1FlowPresenter.FoundingPreparationRootName078), Is.Not.Null);
                Assert.That(coordinator.State.Recruits, Is.Empty,
                    "No founder may be silently signed before the person decision.");
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "RESCUE PREPARATION  •  STEP 1 OF 5");
                var breadcrumb = GameObject.Find(
                        "Founding Preparation Status 078 [Responsive 062] [Authored Compact 076]")
                    ?.GetComponent<Text>();
                var leadConfirm = GameObject.Find("Founding Lead Confirm 078")
                    ?.GetComponent<RectTransform>();
                Assert.That(breadcrumb, Is.Not.Null);
                Assert.That(breadcrumb.text, Is.EqualTo(
                    "PERSON → EQUIPMENT → PLACE → FORMATION → CONFIRM"));
                Assert.That(breadcrumb.text, Does.Not.Contain("\n"));
                Assert.That(breadcrumb.horizontalOverflow,
                    Is.EqualTo(HorizontalWrapMode.Overflow));
                Assert.That(leadConfirm, Is.Not.Null);
                Assert.That(breadcrumb.rectTransform.anchorMax.x,
                    Is.LessThanOrEqualTo(leadConfirm.anchorMin.x - 0.01f),
                    "The one-line breadcrumb must stop before the primary action.");
                var foundingPromiseLabels = GameObject.Find(
                        M1FlowPresenter.FoundingPreparationRootName078)
                    .GetComponentsInChildren<Text>(includeInactive: true)
                    .Where(value => value != null && value.gameObject.name.StartsWith(
                        "Founding Applicant Role ",
                        StringComparison.Ordinal))
                    .Select(value => value.text)
                    .ToArray();
                Assert.That(foundingPromiseLabels, Has.Length.EqualTo(6));
                CollectionAssert.AreEqual(new[]
                {
                    "GUARDIAN PROMISE",
                    "WARRIOR PROMISE",
                    "RANGER PROMISE",
                    "PRIEST PROMISE",
                    "GUARDIAN PROMISE",
                    "PRIEST PROMISE"
                }, foundingPromiseLabels);
                Assert.That(foundingPromiseLabels.Distinct().Count(), Is.EqualTo(4),
                    "Promise ribbons must describe each recruit's permanent trained class; " +
                    "duplicate Guardians and Priests must not be relabeled as Mages or Rogues for artificial variety.");
                AssertFocusedPrefix078("Founding Applicant ");
                AssertDisabledActionReadable078("Founding Lead Confirm 078");
                var leadApplicant = coordinator.State.Applicants
                    .Where(value => value != null)
                    .Take(6)
                    .First(value => value.ObservedClass.IndexOf(
                        "Ranger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    value.ObservedClass.IndexOf(
                        "Rogue", StringComparison.OrdinalIgnoreCase) >= 0);
                ClickObject078("Founding Applicant " +
                               leadApplicant.RecruitId + " 076");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Applicant " +
                                                   leadApplicant.RecruitId + " 076");
                ClickObject078("Founding Lead Confirm 078");
                yield return null;

                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(1));
                Assert.That(coordinator.State.Recruits[0].RecruitId,
                    Is.EqualTo(leadApplicant.RecruitId));
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "2 OF 5");
                AssertFocusedPrefix078("Founding Equipment Choice ");
                AssertBackButton078("EDIT FIELD LEAD");
                AssertPreparationContainsNoRawEquipmentId078();
                AssertPreparationContainsNoVisibleCopy078("EQUIPPED");
                Assert.That(CountPreparationObjects078(
                    "Founding Equipment Priority Badge "), Is.EqualTo(0));

                ClickObject078(M1FlowPresenter.FoundingPreparationBackName078);
                yield return null;
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "1 OF 5");
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(1));
                AssertFocusedPrefix078("Founding Lead Confirm 078");
                ClickObject078("Founding Lead Confirm 078");
                yield return null;
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "2 OF 5");
                AssertBackButton078("EDIT FIELD LEAD");

                ClickObject078("Founding Equipment Choice " +
                               EquipmentSlotIds.MainHand + " 078");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Equipment Choice " +
                                                   EquipmentSlotIds.MainHand + " 078");
                AssertPreparationContainsNoRawEquipmentId078();
                AssertPreparationContainsNoVisibleCopy078("EQUIPPED");
                Assert.That(CountPreparationObjects078(
                    "Founding Equipment Priority Badge "), Is.EqualTo(1),
                    "Only the selected equipment card may carry the PRIORITY badge.");
                AssertNamedTextContains078(
                    "Founding Equipment Priority Text " +
                    EquipmentSlotIds.MainHand + " 078",
                    "PRIORITY");
                var selectedEquipment = GameObject.Find(
                        "Founding Equipment Choice " +
                        EquipmentSlotIds.MainHand + " 078")
                    ?.GetComponent<Button>();
                Assert.That(selectedEquipment, Is.Not.Null);
                var selectedEquipmentLabel = ButtonLabel078(selectedEquipment);
                var selectedArtwork = selectedEquipment
                    .GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name.StartsWith(
                        "Premium Equipment Rarity Frame ",
                        StringComparison.Ordinal));
                var selectedPriority = GameObject.Find(
                        "Founding Equipment Priority Badge " +
                        EquipmentSlotIds.MainHand + " 078")
                    ?.GetComponent<RectTransform>();
                Assert.That(selectedEquipmentLabel, Is.Not.Null);
                Assert.That(selectedPriority, Is.Not.Null);
                Assert.That(selectedEquipmentLabel.rectTransform.anchorMin.x,
                    Is.GreaterThan(selectedArtwork.anchorMax.x),
                    "Selected equipment support copy must begin after its item art.");
                Assert.That(selectedEquipmentLabel.rectTransform.anchorMax.x,
                    Is.LessThan(selectedPriority.anchorMin.x),
                    "Selected equipment support copy must stop before PRIORITY.");
                Assert.That(selectedEquipmentLabel.rectTransform.offsetMin,
                    Is.EqualTo(Vector2.zero));
                Assert.That(selectedEquipmentLabel.rectTransform.offsetMax,
                    Is.EqualTo(Vector2.zero));
                ClickObject078("Founding Equipment Confirm 078");
                yield return null;

                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(6));
                var preparedLead = coordinator.State.Recruits.First(value =>
                    value.RecruitId == leadApplicant.RecruitId);
                Assert.That(preparedLead.HasManualEquipAction, Is.True,
                    "The preparation must commit a real equipment command.");
                Assert.That(preparedLead.Slots.Any(value =>
                    value.SlotId == EquipmentSlotIds.MainHand && value.IsLocked), Is.True);
                Assert.That(coordinator.State.Unions, Has.Count.EqualTo(3));
                Assert.That(coordinator.State.Unions.SelectMany(value =>
                        value.MemberRecruitIds ?? Array.Empty<string>())
                    .Contains(leadApplicant.RecruitId, StringComparer.Ordinal), Is.False,
                    "The lead must remain visibly unplaced until the player's placement decision.");
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "3 OF 5");
                AssertFocusedPrefix078("Founding Union Destination ");
                AssertBackButton078("EDIT EQUIPMENT");

                ClickObject078(M1FlowPresenter.FoundingPreparationBackName078);
                yield return null;
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "2 OF 5");
                AssertFocusedPrefix078("Founding Equipment Confirm 078");
                ClickObject078("Founding Equipment Choice " +
                               EquipmentSlotIds.BodyArmor + " 078");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Equipment Choice " +
                                                   EquipmentSlotIds.BodyArmor + " 078");
                AssertPreparationContainsNoVisibleCopy078("EQUIPPED");
                Assert.That(CountPreparationObjects078(
                    "Founding Equipment Priority Badge "), Is.EqualTo(1));
                AssertNamedTextContains078(
                    "Founding Equipment Priority Text " +
                    EquipmentSlotIds.BodyArmor + " 078",
                    "PRIORITY");
                ClickObject078("Founding Equipment Confirm 078");
                yield return null;
                preparedLead = coordinator.State.Recruits.First(value =>
                    value.RecruitId == leadApplicant.RecruitId);
                Assert.That(preparedLead.Slots.Any(value =>
                    value.SlotId == EquipmentSlotIds.MainHand && value.IsLocked), Is.False,
                    "Editing equipment must replace, not accumulate, the saved priority.");
                Assert.That(preparedLead.Slots.Any(value =>
                    value.SlotId == EquipmentSlotIds.BodyArmor && value.IsLocked), Is.True);
                var bodyPriority = preparedLead.Slots.First(value =>
                    value.SlotId == EquipmentSlotIds.BodyArmor);
                var expectedEquipmentSummary = M1FlowPresenter
                    .FoundingEquipmentDisplayNameForVerification078(
                        bodyPriority.EquippedItemName,
                        bodyPriority.EquippedItemId,
                        bodyPriority.SlotId)
                    .ToUpperInvariant();
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "3 OF 5");
                AssertFocusedPrefix078("Founding Union Destination ");
                AssertNamedTextContains078(
                    "Founding Preparation Instruction Copy 078",
                    "compatible teammate");
                AssertPreparationContainsNoVisibleCopy078("ROLE COLORS AUTO-FILL");

                var chosenUnion = coordinator.State.Unions[0];
                ClickObject078("Founding Union Destination " +
                               chosenUnion.Index + " 078");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Union Destination " +
                                                   chosenUnion.Index + " 078");
                ClickObject078("Founding Placement Confirm 078");
                yield return null;

                var placedUnion = coordinator.State.Unions.First(value =>
                    value.Index == chosenUnion.Index);
                Assert.That(placedUnion.MemberRecruitIds.First(),
                    Is.EqualTo(leadApplicant.RecruitId));
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "4 OF 5");
                AssertFocusedPrefix078("Founding Formation Choice ");
                AssertBackButton078("EDIT UNION");

                ClickObject078(M1FlowPresenter.FoundingPreparationBackName078);
                yield return null;
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "3 OF 5");
                AssertFocusedPrefix078("Founding Placement Confirm 078");
                var previousUnionIndex = chosenUnion.Index;
                chosenUnion = coordinator.State.Unions.First(value =>
                    value.Index != previousUnionIndex);
                ClickObject078("Founding Union Destination " +
                               chosenUnion.Index + " 078");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Union Destination " +
                                                   chosenUnion.Index + " 078");
                ClickObject078("Founding Placement Confirm 078");
                yield return null;
                Assert.That(coordinator.State.Unions.First(value =>
                        value.Index == previousUnionIndex).MemberRecruitIds,
                    Does.Not.Contain(leadApplicant.RecruitId));
                placedUnion = coordinator.State.Unions.First(value =>
                    value.Index == chosenUnion.Index);
                Assert.That(placedUnion.MemberRecruitIds.First(),
                    Is.EqualTo(leadApplicant.RecruitId));
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "4 OF 5");
                AssertFocusedPrefix078("Founding Formation Choice ");

                var chosenFormation = coordinator.State.Formations.Take(3).Skip(1).First();
                ClickObject078("Founding Formation Choice " +
                               chosenFormation.Id + " 078");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Formation Choice " +
                                                   chosenFormation.Id + " 078");
                ClickObject078("Founding Formation Confirm 078");
                yield return null;

                Assert.That(coordinator.State.Unions.First(value =>
                        value.Index == chosenUnion.Index).FormationId,
                    Is.EqualTo(chosenFormation.Id));
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "5 OF 5");
                Assert.That(GameObject.Find(
                    M1FlowPresenter.FoundingPreparationConfirmName078), Is.Not.Null);
                AssertBackButton078("EDIT FORMATION");
                AssertPreparationContainsNoRawEquipmentId078();
                AssertNamedTextContains078(
                    "Founding Rescue Team Saved Choices 078",
                    expectedEquipmentSummary);
                AssertNamedTextContains078(
                    "Founding Confirmation Reserve Text 078",
                    "BESSA BRASSWHISTLE");

                ClickObject078(M1FlowPresenter.FoundingPreparationBackName078);
                yield return null;
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "4 OF 5");
                AssertFocusedPrefix078("Founding Formation Confirm 078");
                var previousFormationId = chosenFormation.Id;
                chosenFormation = coordinator.State.Formations.Take(3)
                    .First(value => !StringComparer.Ordinal.Equals(
                        value.Id,
                        previousFormationId));
                ClickObject078("Founding Formation Choice " +
                               chosenFormation.Id + " 078");
                yield return null;
                AssertSelectedGoldLabelReadable078("Founding Formation Choice " +
                                                   chosenFormation.Id + " 078");
                ClickObject078("Founding Formation Confirm 078");
                yield return null;
                Assert.That(coordinator.State.Unions.First(value =>
                        value.Index == chosenUnion.Index).FormationId,
                    Is.EqualTo(chosenFormation.Id));
                AssertNamedTextContains078(
                    "Founding Preparation Step 078",
                    "5 OF 5");
                AssertNamedTextContains078(
                    "Founding Rescue Team Saved Choices 078",
                    expectedEquipmentSummary);
                AssertNamedTextContains078(
                    "Founding Confirmation Reserve Text 078",
                    "BESSA BRASSWHISTLE");
                AssertFocusedPrefix078(M1FlowPresenter.FoundingPreparationConfirmName078);
                ClickObject078(M1FlowPresenter.FoundingPreparationConfirmName078);
                yield return null;
                yield return null;

                Assert.That(GameObject.Find(
                    M1FlowPresenter.FoundingPreparationRootName078), Is.Null);
                var companyBriefing = GameObject.Find("Founding Company Presentation 076");
                Assert.That(companyBriefing, Is.Not.Null);
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(10));
                Assert.That(coordinator.State.OpeningUnionsLegal, Is.True);
                var active = coordinator.State.Unions
                    .Where(value => value != null &&
                                    (value.MemberRecruitIds?.Count ?? 0) > 0)
                    .ToArray();
                Assert.That(active, Has.Length.EqualTo(3));
                for (var index = 0; index < active.Length; index++)
                {
                    var union = active[index];
                    var jobRibbonName =
                        "Founding Union Leader Role Color " + union.Index + " 076";
                    var jobLabelName =
                        "Founding Union Leader Role " + union.Index + " 076";
                    var leaderClassName =
                        "Founding Union Leader Class " + union.Index + " 076";
                    var leaderClassPlateName =
                        "Founding Union Leader Class Plate " + union.Index + " 076";
                    var jobRibbon = companyBriefing
                        .GetComponentsInChildren<Image>(includeInactive: true)
                        .FirstOrDefault(value => value != null &&
                            value.gameObject.name.StartsWith(
                                jobRibbonName,
                                StringComparison.Ordinal));
                    var jobLabel = companyBriefing
                        .GetComponentsInChildren<Text>(includeInactive: true)
                        .FirstOrDefault(value => value != null &&
                            value.gameObject.name.StartsWith(
                                jobLabelName,
                                StringComparison.Ordinal));
                    var leaderClass = companyBriefing
                        .GetComponentsInChildren<Text>(includeInactive: true)
                        .FirstOrDefault(value => value != null &&
                            value.gameObject.name.StartsWith(
                                leaderClassName,
                                StringComparison.Ordinal));
                    var leaderClassPlate = companyBriefing
                        .GetComponentsInChildren<Image>(includeInactive: true)
                        .FirstOrDefault(value => value != null &&
                            value.gameObject.name.StartsWith(
                                leaderClassPlateName,
                                StringComparison.Ordinal));
                    var leader = coordinator.State.Recruits.First(value =>
                        value != null && StringComparer.Ordinal.Equals(
                            value.RecruitId,
                            union.MemberRecruitIds.First()));
                    var expectedJobColor = M1FlowPresenter
                        .FoundingDestinationColorForVerification078(index);
                    Assert.That(jobRibbon, Is.Not.Null);
                    Assert.That(jobRibbon.gameObject.activeInHierarchy, Is.True);
                    Assert.That(jobRibbon.color.r,
                        Is.EqualTo(expectedJobColor.r).Within(0.001f));
                    Assert.That(jobRibbon.color.g,
                        Is.EqualTo(expectedJobColor.g).Within(0.001f));
                    Assert.That(jobRibbon.color.b,
                        Is.EqualTo(expectedJobColor.b).Within(0.001f));
                    Assert.That(jobLabel?.text, Is.EqualTo(
                        M1FlowPresenter.FoundingDestinationRibbonForVerification078(index)),
                        "The confirmation's team job must remain primary in the company briefing.");
                    Assert.That(jobLabel?.gameObject.activeInHierarchy, Is.True);
                    Assert.That(leaderClass?.text, Does.Contain(
                        M1FlowPresenter.UnionPlannerRoleDesignationForVerification074(
                            leader.ObservedClass)),
                        "Leader class remains visible as a secondary cue.");
                    Assert.That(leaderClass?.gameObject.activeInHierarchy, Is.True);
                    Assert.That(leaderClassPlate, Is.Not.Null);
                    Assert.That(leaderClassPlate.gameObject.activeInHierarchy, Is.True);
                    Assert.That(leaderClass.text, Does.Not.Contain("\n"));
                    Assert.That(leaderClass.horizontalOverflow,
                        Is.EqualTo(HorizontalWrapMode.Overflow),
                        "The compact secondary class cue must remain one readable line.");
                    Assert.That(leaderClass.color.r, Is.GreaterThanOrEqualTo(0.75f));
                    Assert.That(leaderClass.color.g, Is.GreaterThanOrEqualTo(0.75f));
                    Assert.That(leaderClass.color.b, Is.GreaterThanOrEqualTo(0.75f));
                    Assert.That(leaderClassPlate.color.maxColorComponent,
                        Is.LessThanOrEqualTo(0.10f),
                        "The secondary cue needs a consistently dark plate behind its light copy.");
                    var expectedJobRect = M1FlowPresenter
                        .FoundingCompanyTeamJobRibbonRectForVerification078;
                    var expectedClassRect = M1FlowPresenter
                        .FoundingCompanyLeaderClassPlateRectForVerification078;
                    Assert.That(jobRibbon.rectTransform.anchorMin.y,
                        Is.EqualTo(expectedJobRect.yMin).Within(0.001f));
                    Assert.That(jobRibbon.rectTransform.anchorMax.y,
                        Is.EqualTo(expectedJobRect.yMax).Within(0.001f));
                    Assert.That(leaderClassPlate.rectTransform.anchorMin.y,
                        Is.EqualTo(expectedClassRect.yMin).Within(0.001f));
                    Assert.That(leaderClassPlate.rectTransform.anchorMax.y,
                        Is.EqualTo(expectedClassRect.yMax).Within(0.001f));
                }
                Assert.That(active.All(value =>
                    value.MemberRecruitIds.Count == 3 &&
                    value.MemberRecruitIds.Count <=
                    NormalUnionPlanRules.MaximumMembersPerUnion), Is.True);
                Assert.That(active.SelectMany(value => value.MemberRecruitIds)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
                Assert.That(coordinator.State.Recruits.Count -
                            active.SelectMany(value => value.MemberRecruitIds)
                                .Distinct(StringComparer.Ordinal).Count(),
                    Is.EqualTo(1));
                var finalLeadUnion = active.First(value =>
                    value.MemberRecruitIds.Contains(
                        leadApplicant.RecruitId,
                        StringComparer.Ordinal));
                Assert.That(finalLeadUnion.Index, Is.EqualTo(chosenUnion.Index));
                Assert.That(finalLeadUnion.MemberRecruitIds[0],
                    Is.EqualTo(leadApplicant.RecruitId));
                Assert.That(finalLeadUnion.FormationId,
                    Is.EqualTo(chosenFormation.Id));
                Assert.That(active.Length,
                    Is.LessThanOrEqualTo(NormalUnionPlanRules.MaximumPlanCount));

                var restored = new M1RuntimeCoordinator(contentRoot, savePath);
                var restoredLeadUnion = restored.State.Unions.First(value =>
                    value.MemberRecruitIds.Contains(
                        leadApplicant.RecruitId,
                        StringComparer.Ordinal));
                Assert.That(restored.State.Recruits, Has.Count.EqualTo(10));
                Assert.That(restored.State.OpeningUnionsLegal, Is.True);
                Assert.That(restoredLeadUnion.Index, Is.EqualTo(chosenUnion.Index));
                Assert.That(restoredLeadUnion.MemberRecruitIds[0],
                    Is.EqualTo(leadApplicant.RecruitId));
                Assert.That(restoredLeadUnion.FormationId,
                    Is.EqualTo(chosenFormation.Id));
            }
            finally
            {
                if (presenter != null)
                    UnityEngine.Object.Destroy(presenter.gameObject);
                foreach (var path in new[]
                         {
                             savePath,
                             savePath + ".bak",
                             savePath + ".tmp"
                         })
                    if (File.Exists(path)) File.Delete(path);
            }
            yield return null;
        }

        private static float ContrastRatio078(Color left, Color right)
        {
            var leftLuminance = RelativeLuminance078(left);
            var rightLuminance = RelativeLuminance078(right);
            var lighter = Mathf.Max(leftLuminance, rightLuminance);
            var darker = Mathf.Min(leftLuminance, rightLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance078(Color color)
        {
            return 0.2126f * LinearChannel078(color.r) +
                   0.7152f * LinearChannel078(color.g) +
                   0.0722f * LinearChannel078(color.b);
        }

        private static float LinearChannel078(float channel)
        {
            return channel <= 0.04045f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static M1RecruitLoadoutView Recruit(string id, string role)
        {
            return new M1RecruitLoadoutView
            {
                RecruitId = id,
                DisplayName = id,
                ObservedClass = role
            };
        }

        private static void AssertBackButton078(string expectedLabel)
        {
            var back = GameObject.Find(M1FlowPresenter.FoundingPreparationBackName078)
                ?.GetComponent<Button>();
            Assert.That(back, Is.Not.Null, "Steps 2-5 need a visible Edit Previous action.");
            Assert.That(back.interactable, Is.True);
            Assert.That(back.navigation.mode, Is.Not.EqualTo(Navigation.Mode.None),
                "Edit Previous must remain reachable by keyboard/controller navigation.");
            Assert.That(back.GetComponentInChildren<Text>()?.text,
                Does.Contain(expectedLabel));
        }

        private static void AssertDisabledActionReadable078(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.interactable, Is.False, name);
            var label = ButtonLabel078(button);
            Assert.That(label, Is.Not.Null, name + " label");
            Assert.That(ContrastRatio078(button.colors.disabledColor, label.color),
                Is.GreaterThanOrEqualTo(4.5f),
                "Disabled placeholder copy must remain legible.");
        }

        private static void AssertSelectedGoldLabelReadable078(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name);
            var label = ButtonLabel078(button);
            Assert.That(label, Is.Not.Null, name + " label");
            foreach (var surface in new[]
                     {
                         button.colors.normalColor,
                         button.colors.highlightedColor,
                         button.colors.selectedColor,
                         button.colors.pressedColor
                     })
            {
                Assert.That(ContrastRatio078(surface, label.color),
                    Is.GreaterThanOrEqualTo(4.5f),
                    "Selected gold cards require dark ink in every focus state.");
            }
        }

        private static void AssertPreparationContainsNoRawEquipmentId078()
        {
            var root = GameObject.Find(M1FlowPresenter.FoundingPreparationRootName078);
            Assert.That(root, Is.Not.Null);
            var visibleCopy = root.GetComponentsInChildren<Text>(includeInactive: true)
                .Where(value => value != null)
                .Select(value => value.text ?? string.Empty)
                .ToArray();
            Assert.That(visibleCopy, Is.Not.Empty);
            Assert.That(visibleCopy.All(value =>
                    value.IndexOf("EQ_", StringComparison.OrdinalIgnoreCase) < 0),
                Is.True,
                "Founding preparation must never expose equipment authority IDs.");
        }

        private static void AssertPreparationContainsNoVisibleCopy078(string forbidden)
        {
            var root = GameObject.Find(M1FlowPresenter.FoundingPreparationRootName078);
            Assert.That(root, Is.Not.Null);
            var visibleCopy = root.GetComponentsInChildren<Text>(includeInactive: true)
                .Where(value => value != null)
                .Select(value => value.text ?? string.Empty)
                .ToArray();
            Assert.That(visibleCopy.All(value => value.IndexOf(
                    forbidden,
                    StringComparison.OrdinalIgnoreCase) < 0),
                Is.True,
                "Founding preparation must not show stale copy: " + forbidden);
        }

        private static int CountPreparationObjects078(string namePrefix)
        {
            var root = GameObject.Find(M1FlowPresenter.FoundingPreparationRootName078);
            Assert.That(root, Is.Not.Null);
            return root.GetComponentsInChildren<Transform>(includeInactive: true)
                .Count(value => value != null && value.name.StartsWith(
                    namePrefix,
                    StringComparison.Ordinal));
        }

        private static void ClickObject078(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, "Missing button object: " + name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }

        private static void AssertFocusedPrefix078(string prefix)
        {
            Assert.That(EventSystem.current, Is.Not.Null,
                "Preparation must create a controller-ready EventSystem.");
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.That(selected, Is.Not.Null,
                "Preparation must focus a visible choice before mouse input.");
            Assert.That(selected.name, Does.StartWith(prefix),
                "Controller focus must start on the first readable choice.");
        }

        private static void ClickVisibleLabel078(string prefix)
        {
            var button = UnityEngine.Object
                .FindObjectsByType<Button>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value => value != null &&
                    value.gameObject.activeInHierarchy &&
                    value.interactable &&
                    (value.GetComponentInChildren<Text>()?.text ?? string.Empty)
                    .StartsWith(prefix, StringComparison.Ordinal));
            Assert.That(button, Is.Not.Null, "Missing visible action: " + prefix);
            button.onClick.Invoke();
        }

        private static void AssertNamedTextContains078(string name, string expected)
        {
            var preparationRoot = GameObject.Find(
                M1FlowPresenter.FoundingPreparationRootName078);
            var text = preparationRoot?
                .GetComponentsInChildren<Text>(includeInactive: true)
                .FirstOrDefault(value => value != null &&
                    value.gameObject.name.StartsWith(
                        name,
                        StringComparison.Ordinal));
            Assert.That(text, Is.Not.Null,
                "Missing preparation text object: " + name);
            Assert.That(text.gameObject.activeInHierarchy, Is.True,
                "Preparation text must be visible: " + name);
            Assert.That(text.text, Does.Contain(expected));
        }

        private static Text ButtonLabel078(Button button)
        {
            return button?.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(value => value != null &&
                    value.gameObject.name.StartsWith("Label", StringComparison.Ordinal));
        }
    }
}
