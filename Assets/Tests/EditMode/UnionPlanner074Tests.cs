using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class UnionPlanner074Tests
    {
        [Test]
        public void ProductionBuilderIsPrivateAndFixedScreenContractIsBounded()
        {
            var builder = typeof(M1FlowPresenter).GetMethod(
                "BuildUnionPlanner074",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            Assert.That(builder, Is.Not.Null);
            Assert.That(builder.IsPrivate, Is.True);
            Assert.That(M1FlowPresenter.UnionPlannerVisibleTabLimit074, Is.EqualTo(3));
            Assert.That(M1FlowPresenter.UnionPlannerVisibleChoiceLimit074, Is.EqualTo(3));
            Assert.That(M1FlowPresenter.UnionPlannerReservePageSize074, Is.EqualTo(4));
            Assert.That(M1FlowPresenter.UnionPlannerReserveColumns074, Is.EqualTo(4));
            Assert.That(M1FlowPresenter.UnionPlannerMemberSlotColumns078, Is.EqualTo(3));
            Assert.That(M1FlowPresenter.UnionPlannerMemberSlotRows078, Is.EqualTo(2));
            Assert.That(M1FlowPresenter.UnionPlannerReserveLabelMinimumFontSize074,
                Is.EqualTo(18),
                "The four-card reserve row intentionally keeps names and roles at the 18px accessibility floor.");
            Assert.That(M1FlowPresenter.UnionPlannerReserveLabelMaximumFontSize079,
                Is.EqualTo(22));
            Assert.That(M1FlowPresenter.UnionPlannerMemberLabelMinimumFontSize074,
                Is.GreaterThanOrEqualTo(18));
            Assert.That(M1FlowPresenter.UnionPlannerMemberLabelMaximumFontSize074,
                Is.InRange(20, 22));
            Assert.That(M1FlowPresenter.UnionPlannerMinimumSupportedAspect074, Is.EqualTo(1.6f));
        }

        [Test]
        public void CommandBarExplainsTenUnionBattleScaleInPlainLanguage078()
        {
            var copy = M1FlowPresenter.UnionPlannerScaleCopyForVerification078(42);

            Assert.That(copy.Split('\n'), Is.EqualTo(new[]
            {
                "XP TO SPEND  42",
                "FIELD  •  10 UNIONS × 6 = 60",
                "HOUSING III GOAL  •  CAP 75"
            }));
            Assert.That(copy, Does.Not.Contain("TREASURY XP"));
        }

        [Test]
        public void SuggestedUnionPreviewUsesPlainRoleCountsAndRequiresConfirmation087()
        {
            var preview = M1FlowPresenter.SuggestedUnionPreviewCopy087(new[]
            {
                new M1RecruitLoadoutView { ObservedClass = "Restoration Priest" },
                new M1RecruitLoadoutView { ObservedClass = "Field Medic" },
                new M1RecruitLoadoutView { ObservedClass = "Shield Guardian" },
                new M1RecruitLoadoutView { ObservedClass = "Aether Mage" }
            });

            Assert.That(preview, Does.Contain("HEALER ×2"));
            Assert.That(preview, Does.Contain("TANK ×1"));
            Assert.That(preview, Does.Contain("MAGE ×1"));
            Assert.That(preview, Does.Contain("Press Apply Role Teams to confirm."));
        }

        [Test]
        public void ActiveMemberCardKeepsZorinAndRoleOnTwoDeliberateLines079()
        {
            var label = M1FlowPresenter.UnionPlannerMemberCardLabelForVerification079(
                "Zorin Bramblecross",
                "Guardian",
                3,
                true);

            Assert.That(label,
                Is.EqualTo("ZORIN\u00A0BRAMBLECROSS\nGUARDIAN  •  SLOT 4"));
            Assert.That(label.Split('\n'), Has.Length.EqualTo(2));
            Assert.That(label.Replace('\u00A0', ' '), Does.Contain("ZORIN BRAMBLECROSS"));
        }

        [Test]
        public void StarterFormationCardsUseImmediatePositionDiagrams078()
        {
            var diagrams = new[]
            {
                M1FlowPresenter.UnionPlannerFormationDiagramForVerification078(
                    "FORMATION_SHIELD_WALL"),
                M1FlowPresenter.UnionPlannerFormationDiagramForVerification078(
                    "FORMATION_WEDGE"),
                M1FlowPresenter.UnionPlannerFormationDiagramForVerification078(
                    "FORMATION_SKIRMISH_LINE")
            };

            Assert.That(diagrams[0], Is.EqualTo("●  ●  ●\n●  ●  ●"));
            Assert.That(diagrams.All(value => value.Count(character => character == '●') == 6),
                Is.True,
                "Every starter diagram must preview all six Union positions.");
            Assert.That(diagrams.All(value => value.Contains("\n")), Is.True);
        }

        [Test]
        public void ActiveMemberCardsUseWhiteStudioCaptionRailWithoutCoveringPortrait()
        {
            var cardObject = new GameObject(
                "Union Planner Member Card Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(cardObject.transform, false);
            var card = cardObject.GetComponent<Button>();
            var label = labelObject.GetComponent<Text>();

            try
            {
                var configure = typeof(M1FlowPresenter).GetMethod(
                    "ConfigureUnionPlannerMemberCardText074",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(configure, Is.Not.Null);
                configure.Invoke(null, new object[] { card, label });

                var rail = card.transform.Find("Union Planner Member Caption Rail 076");
                Assert.That(rail, Is.Not.Null,
                    "Every occupied member card needs a dedicated contrast rail.");
                Assert.That(rail.GetComponent<Image>(), Is.Not.Null);
                Assert.That(
                    rail.Find("Premium Union Member Caption Rail Edge 076"),
                    Is.Not.Null);
                Assert.That(label.color, Is.EqualTo(Color.white));
                Assert.That(label.resizeTextMinSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMinimumFontSize074));
                Assert.That(label.resizeTextMaxSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMaximumFontSize074));
                Assert.That(label.fontSize,
                    Is.EqualTo(M1FlowPresenter.UnionPlannerMemberLabelMaximumFontSize074));

                var railRegion = M1FlowPresenter.UnionPlannerMemberCaptionRailForVerification074;
                var portraitRegion = M1FlowPresenter.UnionPlannerMemberPortraitForVerification074;
                Assert.That(railRegion.yMax, Is.LessThan(portraitRegion.yMin),
                    "Caption contrast must not cover the portrait boundary.");
                Assert.That(label.rectTransform.anchorMin.y,
                    Is.GreaterThanOrEqualTo(railRegion.yMin));
                Assert.That(label.rectTransform.anchorMax.y,
                    Is.LessThanOrEqualTo(railRegion.yMax));
                Assert.That(label.transform.GetSiblingIndex(),
                    Is.EqualTo(card.transform.childCount - 1),
                    "The white caption must render above its navy rail.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
            }
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void MajorRegionsRemainVisibleSeparatedAndReadableAtSupportedResolutions(
            int width,
            int height)
        {
            var regions = M1FlowPresenter.UnionPlannerMajorRegionsForVerification074;
            Assert.That(regions, Has.Count.EqualTo(5));
            foreach (var region in regions)
            {
                Assert.That(region.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(region.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(region.xMax, Is.LessThanOrEqualTo(1f));
                Assert.That(region.yMax, Is.LessThanOrEqualTo(1f));
                Assert.That(region.width * width, Is.GreaterThanOrEqualTo(300f));
                Assert.That(region.height * height, Is.GreaterThanOrEqualTo(64f));
            }

            for (var left = 0; left < regions.Count; left++)
            for (var right = left + 1; right < regions.Count; right++)
                Assert.That(regions[left].Overlaps(regions[right]), Is.False,
                    "Fixed planner regions must not cover one another.");
        }

        [Test]
        public void ThreeTabWindowPagesAllTenUnionPlansAndPreservesPlanOrder()
        {
            var unions = Enumerable.Range(0, 10)
                .Select(index => new M1UnionView
                {
                    Index = 10 + index,
                    DisplayName = "Union " + index
                })
                .ToArray();

            Assert.That(
                M1FlowPresenter.UnionPlannerVisibleUnionIndicesForVerification074(unions, 10),
                Is.EqualTo(new[] { 10, 11, 12 }));
            Assert.That(
                M1FlowPresenter.UnionPlannerVisibleUnionIndicesForVerification074(unions, 14),
                Is.EqualTo(new[] { 13, 14, 15 }));
            Assert.That(
                M1FlowPresenter.UnionPlannerVisibleUnionIndicesForVerification074(unions, 19),
                Is.EqualTo(new[] { 19 }));
            Assert.That(
                M1FlowPresenter.UnionPlannerVisibleUnionIndicesForVerification074(unions, 999),
                Is.EqualTo(new[] { 10, 11, 12 }));
        }

        [Test]
        public void ChoiceCardsCapAtThreeAndKeepMigratedSelectionVisible()
        {
            var choices = Enumerable.Range(0, 8)
                .Select(index => new M1ChoiceView
                {
                    Id = "CHOICE_" + index,
                    DisplayName = "Choice " + index
                })
                .ToArray();

            var starter = M1FlowPresenter.UnionPlannerVisibleChoicesForVerification074(
                choices,
                "CHOICE_1");
            Assert.That(starter.Select(value => value.Id),
                Is.EqualTo(new[] { "CHOICE_0", "CHOICE_1", "CHOICE_2" }));

            var migrated = M1FlowPresenter.UnionPlannerVisibleChoicesForVerification074(
                choices,
                "CHOICE_7");
            Assert.That(migrated, Has.Count.EqualTo(3));
            Assert.That(migrated.Select(value => value.Id),
                Is.EqualTo(new[] { "CHOICE_0", "CHOICE_1", "CHOICE_7" }));
        }

        [Test]
        public void StoryProgressionUnlocksThreeThenSixThenTheFullChoiceCatalog()
        {
            Assert.That(
                M1FlowPresenter.UnionPlannerUnlockedChoiceCountForVerification076(
                    8,
                    false,
                    false),
                Is.EqualTo(3));
            Assert.That(
                M1FlowPresenter.UnionPlannerUnlockedChoiceCountForVerification076(
                    8,
                    true,
                    false),
                Is.EqualTo(6));
            Assert.That(
                M1FlowPresenter.UnionPlannerUnlockedChoiceCountForVerification076(
                    8,
                    true,
                    true),
                Is.EqualTo(8));
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void PostRescueReviewFitsAllSixUnionReadinessTilesAtOnce(
            int width,
            int height)
        {
            var reserveRegion = M1FlowPresenter.UnionPlannerMajorRegionsForVerification074[2];
            var tiles = M1FlowPresenter.UnionPlannerForceOverviewTileRectsForVerification076();

            Assert.That(tiles, Has.Count.EqualTo(6));
            foreach (var tile in tiles)
            {
                Assert.That(tile.width * reserveRegion.width * width,
                    Is.GreaterThanOrEqualTo(200f));
                Assert.That(tile.height * reserveRegion.height * height,
                    Is.GreaterThanOrEqualTo(60f));
            }
            for (var left = 0; left < tiles.Count; left++)
            for (var right = left + 1; right < tiles.Count; right++)
                Assert.That(tiles[left].Overlaps(tiles[right]), Is.False);
        }

        [Test]
        public void SixOpeningUnionsReceiveDistinctStoryNamesAndCompactTabs()
        {
            var expectedFullNames = new[]
            {
                "Skyhome Gatewardens",
                "Lantern Spear",
                "Wayfinders",
                "Zorin's Vanguard",
                "Wayglass Guard",
                "Skyhome Rearguard"
            };
            var expectedTabNames = new[]
            {
                "Gatewardens",
                "Lantern Spear",
                "Wayfinders",
                "Vanguard",
                "Wayglass",
                "Rearguard"
            };

            for (var index = 0; index < expectedFullNames.Length; index++)
            {
                var unionId = "UNION_OPENING_" + (index + 1);
                var genericName = "Opening Union " + (index + 1);
                Assert.That(
                    M1UnionIdentity076.Resolve(unionId, genericName, index),
                    Is.EqualTo(expectedFullNames[index]));
                Assert.That(
                    M1UnionIdentity076.ResolveTab(unionId, genericName, index),
                    Is.EqualTo(expectedTabNames[index]));
                Assert.That(
                    M1UnionIdentity076.ResolveTab(unionId, expectedFullNames[index], index),
                    Is.EqualTo(expectedTabNames[index]),
                    "The runtime projection already resolves canonical story names before building tabs.");
                Assert.That(expectedTabNames[index].Length, Is.LessThanOrEqualTo(13),
                    "A three-tab window needs compact names that do not split mid-word.");
            }

            Assert.That(expectedFullNames.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(6));
            Assert.That(
                M1UnionIdentity076.ResolveTab("CUSTOM", "The Brass Company", 0),
                Is.EqualTo("The Brass Company"),
                "A player-authored Union name must never be replaced by onboarding copy.");
        }

        [Test]
        public void LaterChapterChoicesUseReadableThreeCardPagesAndPreserveMigratedSelection()
        {
            var choices = Enumerable.Range(0, 8)
                .Select(index => new M1ChoiceView
                {
                    Id = "CHOICE_" + index,
                    DisplayName = "Choice " + index
                })
                .ToArray();

            var chapterTwo =
                M1FlowPresenter.UnionPlannerUnlockedChoicesForVerification076(
                    choices,
                    "CHOICE_1",
                    true,
                    false);
            Assert.That(chapterTwo.Select(value => value.Id),
                Is.EqualTo(new[]
                {
                    "CHOICE_0", "CHOICE_1", "CHOICE_2",
                    "CHOICE_3", "CHOICE_4", "CHOICE_5"
                }));
            Assert.That(
                M1FlowPresenter.UnionPlannerChoicePageCountForVerification076(
                    chapterTwo.Count),
                Is.EqualTo(2));
            Assert.That(
                M1FlowPresenter.UnionPlannerChoicePageForVerification076(
                        chapterTwo,
                        1)
                    .Select(value => value.Id),
                Is.EqualTo(new[] { "CHOICE_3", "CHOICE_4", "CHOICE_5" }));

            var migrated =
                M1FlowPresenter.UnionPlannerUnlockedChoicesForVerification076(
                    choices,
                    "CHOICE_7",
                    true,
                    false);
            Assert.That(migrated.Select(value => value.Id), Does.Contain("CHOICE_7"));
            Assert.That(
                M1FlowPresenter.UnionPlannerChoicePageForVerification076(
                        migrated,
                        2)
                    .Select(value => value.Id),
                Is.EqualTo(new[] { "CHOICE_7" }));
        }

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(10, 3)]
        public void ReserveGrowthUsesFiniteFourMemberPages(int reserveCount, int expectedPages)
        {
            Assert.That(
                M1FlowPresenter.UnionPlannerReservePageCountForVerification074(reserveCount),
                Is.EqualTo(expectedPages));
        }
    }
}
