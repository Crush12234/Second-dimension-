using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FoundingPreparation078EditModeTests
    {
        [TestCase("EQ_PROC_WARD_SABER", "SIGI_31A3_MAIN",
            "SLOT_MAIN_HAND", "Ward Saber")]
        [TestCase("EQ_PROC_WARD_COAT", "SIGI_31A3_BODY",
            "SLOT_BODY_ARMOR", "Ward Coat")]
        [TestCase("Ashwood Medic Staff", "SIGI_31A3_MAIN",
            "SLOT_MAIN_HAND", "Ashwood Medic Staff")]
        [TestCase(null, "SIGI_31A3_BODY", "SLOT_BODY_ARMOR", "Starter Armor")]
        [TestCase(null, null, "SLOT_BODY_ARMOR", "Starter Armor")]
        public void EquipmentAuthorityTokensBecomePlayerFacingNames078(
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
            Assert.That(actual, Does.Not.StartWith("SIGI_"));
        }

        [TestCase("Guardian", "HOLDS THE LINE")]
        [TestCase("Warrior", "BREAKS THE ENEMY LINE")]
        [TestCase("Fighter", "BREAKS THE ENEMY LINE")]
        [TestCase("Priest", "RESTORES ALLIES")]
        [TestCase("Mage", "CHANNELS BATTLE ARTS")]
        [TestCase("Ranger", "SCOUTS THE FIELD")]
        [TestCase("Rogue", "FLANKS OPEN THREATS")]
        public void FoundingClassPromisesStateEachClassJob078(
            string observedClass,
            string expectedPromise)
        {
            Assert.That(
                M1FlowPresenter.FoundingRolePromiseForVerification078(
                    observedClass),
                Is.EqualTo(expectedPromise));
        }

        [Test]
        public void FoundingPromisesMatchEachPermanentTrainedClass084()
        {
            var trainedClasses = new[]
            {
                "Guardian", "Warrior", "Ranger", "Priest", "Guardian", "Priest"
            };
            var promiseRoles = trainedClasses
                .Select((trainedClass, index) => M1FlowPresenter
                    .FoundingPromiseRoleForVerification079(index, trainedClass))
                .ToArray();

            Assert.That(promiseRoles, Is.EqualTo(trainedClasses
                .Select(value => value.ToUpperInvariant())
                .ToArray()));
            Assert.That(promiseRoles.Distinct().Count(), Is.EqualTo(4));
            Assert.That(trainedClasses, Is.EqualTo(new[]
            {
                "Guardian", "Warrior", "Ranger", "Priest", "Guardian", "Priest"
            }), "Presentation promises must use canonical trained classes.");
        }

        [TestCase(1280f)]
        [TestCase(1920f)]
        public void SelectedEquipmentCopyClearsArtworkAndPriorityAtSupportedWidths079(
            float screenWidth)
        {
            var artwork = M1FlowPresenter
                .FoundingEquipmentArtworkRectForVerification079;
            var label = M1FlowPresenter
                .FoundingEquipmentLabelRectForVerification079(true);
            var priority = M1FlowPresenter
                .FoundingEquipmentPriorityBadgeRectForVerification079;
            var cardWidth = screenWidth * M1FlowPresenter
                .FoundingEquipmentCardWidthFraction079;

            Assert.That(label.xMin, Is.GreaterThan(artwork.xMax));
            Assert.That(label.xMax, Is.LessThan(priority.xMin));
            Assert.That((label.xMin - artwork.xMax) * cardWidth,
                Is.GreaterThanOrEqualTo(8f));
            Assert.That((priority.xMin - label.xMax) * cardWidth,
                Is.GreaterThanOrEqualTo(8f));
        }

        [Test]
        public void SelectedAndDisabledFoundingSurfacesMeetContrastFloor078()
        {
            var selectedInk =
                M1FlowPresenter.FoundingGoldChoiceLabelForVerification078(true);
            foreach (var surface in new[]
                     {
                         M1FlowPresenter.FoundingGoldChoiceSurfaceForVerification078(true),
                         M1FlowPresenter.FoundingGoldChoiceFocusedSurfaceForVerification078(),
                         M1FlowPresenter.FoundingGoldChoicePressedSurfaceForVerification078()
                     })
            {
                Assert.That(ContrastRatio078(surface, selectedInk),
                    Is.GreaterThanOrEqualTo(4.5f));
            }

            Assert.That(ContrastRatio078(
                    M1FlowPresenter.FoundingDisabledActionSurfaceForVerification078(),
                    M1FlowPresenter.FoundingDisabledActionLabelForVerification078()),
                Is.GreaterThanOrEqualTo(4.5f));
        }

        [TestCase(FoundingPreparationStage078.ChooseEquipmentPriority,
            FoundingPreparationStage078.ChooseFieldLead, "EDIT FIELD LEAD")]
        [TestCase(FoundingPreparationStage078.PlaceFieldLead,
            FoundingPreparationStage078.ChooseEquipmentPriority, "EDIT EQUIPMENT")]
        [TestCase(FoundingPreparationStage078.ChooseFormation,
            FoundingPreparationStage078.PlaceFieldLead, "EDIT UNION")]
        [TestCase(FoundingPreparationStage078.ConfirmRescueTeam,
            FoundingPreparationStage078.ChooseFormation, "EDIT FORMATION")]
        public void StepsTwoThroughFiveMapToOneExplicitPreviousDecision078(
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

        [Test]
        public void FailedEquipmentCommitPinsRetryDespitePartialFounderWrites078()
        {
            var inferredWithoutRetry = M1FlowPresenter
                .ResolveFoundingStageForVerification078(
                    FoundingPreparationStage078.ChooseEquipmentPriority,
                    preserveExplicitStage: false,
                    recruitCount: 6,
                    hasLeadUnion: false);
            var heldForRetry = M1FlowPresenter
                .ResolveFoundingStageForVerification078(
                    FoundingPreparationStage078.ChooseEquipmentPriority,
                    preserveExplicitStage: true,
                    recruitCount: 6,
                    hasLeadUnion: false);

            Assert.That(inferredWithoutRetry,
                Is.EqualTo(FoundingPreparationStage078.PlaceFieldLead),
                "Six partial founder writes normally imply the placement step.");
            Assert.That(heldForRetry,
                Is.EqualTo(FoundingPreparationStage078.ChooseEquipmentPriority),
                "A failed equipment commit must retain its visible retry screen.");
        }

        private static float ContrastRatio078(Color left, Color right)
        {
            var lighter = Mathf.Max(
                RelativeLuminance078(left),
                RelativeLuminance078(right));
            var darker = Mathf.Min(
                RelativeLuminance078(left),
                RelativeLuminance078(right));
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
    }
}
