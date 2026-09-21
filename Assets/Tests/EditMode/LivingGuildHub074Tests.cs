using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class LivingGuildHub074Tests
    {
        [Test]
        public void HubHasPrivateBuilderAndExactlyFourPrimaryDestinations110()
        {
            var builder = typeof(M1FlowPresenter).GetMethod(
                "BuildLivingGuildHub074",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(IGuildCityPresentationCoordinator017D),
                    typeof(GuildCityPresentationState017D)
                },
                null);

            Assert.That(builder, Is.Not.Null);
            Assert.That(builder.IsPrivate, Is.True);
            var ids = M1FlowPresenter.LivingGuildHubFacilityIdsForVerification074;
            Assert.That(ids, Has.Count.EqualTo(4));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            Assert.That(ids, Is.EqualTo(new[]
            {
                WalkableGuildHall069.ContractDestinationId069,
                M1FlowPresenter.LivingGuildHubTowerDestinationId081,
                WalkableGuildHall069.PartyDestinationId069,
                WalkableGuildHall069.ArmoryDestinationId069
            }));
            Assert.That(ids, Does.Not.Contain(M1FlowPresenter.LivingGuildHubCityDestinationId078));
            Assert.That(M1FlowPresenter.LivingGuildHubCodesDestinationId084,
                Is.EqualTo("CODES"));
            Assert.That(M1FlowPresenter.LivingGuildHubFacilityLabelsForVerification084,
                Is.EqualTo(new[]
                {
                    "CAMPAIGN\nTHREE-CARD QUESTS",
                    "TOWER\nOPTIONAL BATTLES",
                    "HEROES\nOWNED HEROES & UNIONS",
                    "EQUIPMENT\nGEAR & ITEMS"
                }));
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void FourDirectDockActionsStayContainedAndLeaveTheHallSceneVisible110(
            int width,
            int height)
        {
            var rects = M1FlowPresenter.LivingGuildHubFacilityRectsForVerification074;
            Assert.That(rects, Has.Count.EqualTo(4));
            Assert.That(rects.Select(value => value.width).Distinct().Count(), Is.EqualTo(1));
            Assert.That(rects.Select(value => value.height).Distinct().Count(), Is.EqualTo(1));
            Assert.That(rects.Select(value => value.x).Distinct().Count(), Is.EqualTo(4));
            Assert.That(rects.Select(value => value.y).Distinct().Count(), Is.EqualTo(1));
            Assert.That(rects.Max(value => value.yMax), Is.LessThanOrEqualTo(0.25f),
                "Direct destination controls belong to the bottom dock, leaving the Hall and companions visible.");
            Assert.That(rects.Sum(value => value.width * value.height), Is.LessThan(0.20f),
                "Four destinations must preserve the existing dock area and Hall artwork.");
            foreach (var rect in rects)
            {
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(1f));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(1f));
                Assert.That(rect.width * width, Is.GreaterThanOrEqualTo(112f));
                Assert.That(rect.height * height, Is.GreaterThanOrEqualTo(64f));
            }

            for (var left = 0; left < rects.Count; left++)
            for (var right = left + 1; right < rects.Count; right++)
                Assert.That(rects[left].Overlaps(rects[right]), Is.False,
                    "Facility targets must not cover one another at any supported aspect ratio.");

            foreach (var rect in rects)
                Assert.That(rect.Overlaps(
                        M1FlowPresenter.LivingGuildHubObjectiveRegionForVerification078), Is.False,
                    "A Hall facility target must not sit beneath the chapter objective card.");
        }

        [Test]
        public void FourPrimaryActions110ShareOneControllerRowAndSecondaryActionsRemainReachable()
        {
            var owner = new GameObject("Home navigation test 110");
            try
            {
                var buttons = Enumerable.Range(0, 7).Select(index =>
                {
                    var button = new GameObject("Home action " + index, typeof(RectTransform), typeof(Button))
                        .GetComponent<Button>();
                    button.transform.SetParent(owner.transform, false);
                    return button;
                }).ToArray();
                var facilities = buttons.Take(4).ToArray();
                var primary = buttons[4];
                var secondary = buttons.Skip(5).ToArray();
                typeof(M1FlowPresenter).GetMethod("ConfigureLivingGuildNavigation074",
                    BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                        new object[] { facilities, primary, secondary });
                Assert.That(facilities[2].navigation.selectOnRight, Is.SameAs(facilities[3]));
                Assert.That(facilities[3].navigation.selectOnLeft, Is.SameAs(facilities[2]));
                Assert.That(facilities.All(button => button.navigation.selectOnUp == primary), Is.True);
                Assert.That(primary.navigation.selectOnUp, Is.SameAs(secondary[0]));
                Assert.That(secondary[0].navigation.selectOnRight, Is.SameAs(secondary[1]));
                Assert.That(secondary[1].navigation.selectOnDown, Is.SameAs(primary));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PopulatedGuildHallArtworkIsPackagedWithFallbackFriendlyResourceType()
        {
            var texture = Resources.Load<Texture2D>(M1FlowPresenter.LivingGuildHubBackgroundResource074);
            var sprite = Resources.Load<Sprite>(M1FlowPresenter.LivingGuildHubBackgroundResource074);
            Assert.That(texture != null || sprite != null, Is.True,
                "The living hub must ship with its populated Hall art; production code falls back to the standard Hall backdrop.");
        }

        [Test]
        public void HallHeaderUsesOneCleanLocationHierarchy()
        {
            Assert.That(M1FlowPresenter.LivingGuildHubTitle074, Is.EqualTo("SKYHOME GUILD HALL"));
            Assert.That(M1FlowPresenter.LivingGuildHubTitle074, Does.Not.Contain("GUILDMASTER'S"));
            Assert.That(M1FlowPresenter.LivingGuildHubTitle074.Split(' ')
                .Count(value => value == "GUILD"), Is.EqualTo(1));
        }

        [Test]
        public void PremiumResourceBadgeSeparatesLevelProgressFromXpToSpend079()
        {
            var copy = M1FlowPresenter.PremiumProgressionBadgeCopyForVerification079(
                true,
                0,
                200,
                0);

            Assert.That(copy.Split('\n'), Is.EqualTo(new[]
            {
                "GUILD XP 0 / 200",
                "XP TO SPEND 0"
            }));
            Assert.That(copy, Does.Not.Contain("SPENDABLE"));
            Assert.That(copy, Does.Not.Contain("TREASURY"));
            Assert.That(
                M1FlowPresenter.PremiumProgressionBadgeCopyForVerification079(
                    false,
                    0,
                    0,
                    0),
                Is.EqualTo("GUILD XP — / —\nXP TO SPEND —"));
        }

        [Test]
        public void HomeStatusContainsOnlyLevelProgressAndXpToSpend()
        {
            Assert.That(M1FlowPresenter.LivingGuildHubMembersLabel076,
                Is.EqualTo("RECRUIT\nMEET MEMBERS"));
            var status = M1FlowPresenter.LivingGuildHubStatusCopyForVerification084(
                2,
                58,
                400,
                42);
            Assert.That(status,
                Is.EqualTo("GUILD LEVEL  2  •  GUILD XP  58 / 400  •  XP TO SPEND  42"));
            Assert.That(status.Split('\n'), Has.Length.EqualTo(1));
            Assert.That(status,
                Does.Not.Contain("MEMBER")
                    .And.Not.Contain("HALL")
                    .And.Not.Contain("DAY")
                    .And.Not.Contain("REPUTATION")
                    .And.Not.Contain("TREASURY"));
        }

        [Test]
        public void NextStoryCopyIsSinglePurposeAndRemovesDuplicatePrefix()
        {
            Assert.That(M1FlowPresenter.LivingGuildHubObjectiveHeading084,
                Is.EqualTo("NEXT STORY"));
            Assert.That(M1FlowPresenter.LivingGuildHubConciseObjectiveForVerification084(
                    "NEXT: Rescue the missing patrol."),
                Is.EqualTo("Rescue the missing patrol."));
            Assert.That(M1FlowPresenter.LivingGuildHubConciseObjectiveForVerification084(null),
                Is.EqualTo("Choose your next mission from the board."));
        }

        [Test]
        public void HallRosterExplainsCurrentHousingCapWithoutClaimingFutureCapacity079()
        {
            var heading = M1FlowPresenter.LivingGuildHubRosterHeadingForVerification079(
                20,
                24,
                6);

            Assert.That(heading,
                Is.EqualTo("YOUR PEOPLE  •  20 MEMBERS  •  CURRENT HOUSING CAP 24  •  6 SHOWN"));
            Assert.That(heading, Does.Not.Contain("20 / 24"));
            Assert.That(
                M1FlowPresenter.UnionPlannerScaleCopyForVerification078(116),
                Does.Contain("HOUSING III GOAL  •  CAP 75"),
                "The planner must label 75 as a future housing goal, not the current Hall capacity.");
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void CityWorkshopShowsTwelveReadableNonOverlappingSnapPlots(
            int width,
            int height)
        {
            var rects = M1FlowPresenter.GuildCityWorkshopPlotRectsForVerification078(12);
            Assert.That(rects, Has.Count.EqualTo(12));
            foreach (var rect in rects)
            {
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(1f));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(1f));
                Assert.That(rect.width * 0.55f * width, Is.GreaterThanOrEqualTo(110f));
                Assert.That(rect.height * 0.625f * height, Is.GreaterThanOrEqualTo(74f));
            }
            for (var left = 0; left < rects.Count; left++)
            for (var right = left + 1; right < rects.Count; right++)
                Assert.That(rects[left].Overlaps(rects[right]), Is.False);
        }

        [Test]
        public void CityWorkshopAsksOnlyTheCurrentGuildmasterQuestion()
        {
            var state = new GuildCityPresentationState017D();
            Assert.That(M1FlowPresenter.GuildCityWorkshopQuestionForVerification078(state),
                Is.EqualTo("WHAT WILL YOUR GUILD BUILD FIRST?"));
            state.PlacedBuildingCount = 1;
            Assert.That(M1FlowPresenter.GuildCityWorkshopQuestionForVerification078(state),
                Is.EqualTo("WHO WILL MAKE THIS FACILITY MATTER?"));
            state.StaffedBuildingCount = 1;
            Assert.That(M1FlowPresenter.GuildCityWorkshopQuestionForVerification078(state),
                Is.EqualTo("WHAT CHANGED BECAUSE YOU BUILT IT?"));
            Assert.That(M1FlowPresenter.GuildCityWorkshopQuestionForVerification078(state, true),
                Is.EqualTo("WHAT WILL YOUR GUILD BUILD NEXT?"));
        }

        [Test]
        public void FirstOperationRecoveryAndTrainingCopyExplainsCurrentDutyBenefits156()
        {
            var premise = M1FlowPresenter.FirstOperationPremiseForVerification077(
                FirstOperationConsequenceStage077.RecoveryOrTraining);
            var recovery = M1FlowPresenter.FirstOperationRecoveryChoiceLabel077(25);
            var training = M1FlowPresenter.FirstOperationTrainingChoiceLabel077(10);

            Assert.That(premise,
                Does.Contain("Recovery banks progress without healing HP")
                    .And.Contain("Training builds future growth"));
            Assert.That(premise, Does.Contain("until you choose Reserve").And.Not.Contain("saved parallel order"));
            Assert.That(recovery,
                Is.EqualTo("RECOVERY  •  REST AT THE GUILD\n+25 RECOVERY PROGRESS  •  STAYS HOME"));
            Assert.That(training,
                Is.EqualTo("TRAINING  •  BUILD FUTURE GROWTH\n+10 TRAINING PROGRESS  •  STAYS HOME"));
            Assert.That(recovery + training, Does.Not.Contain("NEXT OPERATION"));
        }

        [Test]
        public void FirstOperationMemoryChoicesNameBothPeopleAndDistinctEncounter079()
        {
            var state = new GuildCityPresentationState017D
            {
                Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                        { RecruitId = "A", RecruitName = "Aster Vale", Kind = "Reserve" },
                    new GuildCityAssignmentView017D
                        { RecruitId = "B", RecruitName = "Bessa Stone", Kind = "Reserve" },
                    new GuildCityAssignmentView017D
                        { RecruitId = "D", RecruitName = "Daeven Fellstar", Kind = "Reserve" }
                }
            };
            var memories = new[]
            {
                new GuildCityRelationshipView017D
                {
                    MemoryId = "PETRA",
                    FirstRecruitId = "A",
                    SecondRecruitId = "B",
                    Summary = "They answered Zorin's count and lifted Petra Runebrook together.",
                    SceneId = "REL_SCENE_PETRA",
                    Strength = 2
                },
                new GuildCityRelationshipView017D
                {
                    MemoryId = "AMBUSH",
                    FirstRecruitId = "B",
                    SecondRecruitId = "D",
                    Summary = "They completed the encounter together.",
                    SceneId = "REL_SCENE_AFTER_BATTLE_LANTERN_ROAD_AMBUSH",
                    Strength = 2
                },
                new GuildCityRelationshipView017D
                {
                    MemoryId = "GATE",
                    FirstRecruitId = "D",
                    SecondRecruitId = "A",
                    Summary = "They completed the encounter together.",
                    SceneId = "REL_SCENE_AFTER_BATTLE_GATE_EATER",
                    Strength = 2
                }
            };
            var labels = memories.Select((memory, index) =>
                M1FlowPresenter.FirstOperationMemoryLabelForVerification077(
                    memory,
                    state,
                    index)).ToArray();

            Assert.That(labels.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
            Assert.That(labels[0], Does.Contain("ASTER VALE").And.Contain("BESSA STONE")
                .And.Contain("Petra Runebrook"));
            Assert.That(labels[1], Does.Contain("BESSA STONE").And.Contain("DAEVEN FELLSTAR")
                .And.Contain("Lantern Road ambush"));
            Assert.That(labels[2], Does.Contain("DAEVEN FELLSTAR").And.Contain("ASTER VALE")
                .And.Contain("Gate-Eater"));
            Assert.That(string.Join("\n", labels),
                Does.Not.Contain("They completed the encounter together."));
        }

        [Test]
        public void FirstOperationStaffChoicesNamePersonFacilityAndCausedBenefit079()
        {
            const string effect =
                "Personal, Union, and Guild XP through preserved history, public drills, and canon events";
            var people = new[] { "Aster Vale", "Bessa Stone", "Daeven Fellstar" };
            var labels = people.Select(person =>
                M1FlowPresenter.FirstOperationStaffChoiceLabel078(
                    person,
                    "Chronicle Plaza",
                    effect)).ToArray();

            Assert.That(labels.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
            for (var index = 0; index < labels.Length; index++)
            {
                Assert.That(labels[index], Does.Contain(people[index].ToUpperInvariant()));
                Assert.That(labels[index], Does.Contain("STAFF CHRONICLE PLAZA"));
                Assert.That(labels[index], Does.Contain("WORK EARNS PERSONAL, UNION & GUILD XP"));
                Assert.That(labels[index], Does.Not.Contain("PROGRESSES BESIDE"));
            }
        }

        [Test]
        public void CityWorkshopKeepsPaidLegalBuildsVisibleAndLabelsExactBalances()
        {
            var building = new GuildCityBuildingView017D
            {
                BuildingId = "GC017D_BUILD_FORGE",
                DisplayName = "Forge",
                DistrictId = "DISTRICT_CRAFT",
                HasLevelOneCost = true,
                LevelOneHallXpCost = 40,
                CanAffordLevelOneWithResources = true,
                LevelOneMaterialRequirements = new[]
                {
                    new GuildCityMaterialRequirementView017D
                    {
                        MaterialId = "MAT_GATE_IRON",
                        DisplayName = "Gate Iron",
                        AvailableAmount = 2,
                        RequiredAmount = 1
                    },
                    new GuildCityMaterialRequirementView017D
                    {
                        MaterialId = "MAT_SALVAGED_TIMBER",
                        DisplayName = "Salvaged Timber",
                        AvailableAmount = 5,
                        RequiredAmount = 3
                    }
                }
            };
            var state = new GuildCityPresentationState017D
            {
                CharterBuildCredits = 0,
                HallEnhancementXp = 44,
                Buildings = new[] { building },
                Plots = new[]
                {
                    new GuildCityPlotView017D
                    {
                        PlotId = "GC017D_PLOT_CRAFT_02",
                        DistrictId = "DISTRICT_CRAFT",
                        Unlocked = true,
                        RoadConnected = true,
                        BuildingId = string.Empty
                    }
                }
            };

            Assert.That(M1FlowPresenter.GuildCityWorkshopFacilityChoiceCountForVerification078(state),
                Is.EqualTo(1),
                "Spending the charter credit must not hide later authority-valid placements.");
            Assert.That(M1FlowPresenter.GuildCityWorkshopCanSubmitBuildForVerification078(state, building),
                Is.True);
            var label = M1FlowPresenter.GuildCityWorkshopBuildCostLabelForVerification078(state, building);
            Assert.That(label, Does.StartWith("READY"));
            Assert.That(label, Does.Contain("HALL XP 44/40"));
            Assert.That(label, Does.Contain("GATE IRON 2/1"));
            Assert.That(label, Does.Contain("SALVAGED TIMBER 5/3"));
            Assert.That(label, Does.Not.Contain("CHARTER CREDIT"));

            building.CanAffordLevelOneWithResources = false;
            Assert.That(M1FlowPresenter.GuildCityWorkshopFacilityChoiceCountForVerification078(state),
                Is.EqualTo(1), "An unaffordable build stays visible so the player can see its goal.");
            Assert.That(M1FlowPresenter.GuildCityWorkshopCanSubmitBuildForVerification078(state, building),
                Is.False, "The simplified screen must never turn a shortfall into a free build.");
            Assert.That(M1FlowPresenter.GuildCityWorkshopBuildCostLabelForVerification078(state, building),
                Does.StartWith("NEED RESOURCES"));
        }

        [TestCase(7, 1, 2)]
        [TestCase(10, 0, 3)]
        public void RecruitmentSelectionFollowsSignedApplicantOntoTheirResortedPage(
            int applicantCount,
            int selectedSlot,
            int expectedPage)
        {
            var selectedId = "APPLICANT_" + selectedSlot;
            var ordered = Enumerable.Range(0, applicantCount)
                .Select(slot => new GuildCityApplicantView017D
                {
                    Slot = slot,
                    RecruitId = "APPLICANT_" + slot,
                    DisplayName = "Applicant " + slot,
                    IsSigned = slot == selectedSlot
                })
                .OrderBy(value => value.IsSigned)
                .ThenBy(value => value.Slot)
                .ToArray();

            var page = M1FlowPresenter.RecruitmentSelectedApplicantPageForVerification074(
                ordered,
                selectedId,
                0);

            Assert.That(page, Is.EqualTo(expectedPage));
            Assert.That(ordered.Skip(page * 3).Take(3)
                    .Any(value => StringComparer.Ordinal.Equals(value.RecruitId, selectedId)),
                Is.True, "The portrait selection and visible three-person shortlist must agree after sorting.");
        }

        [Test]
        public void CityWorkshopPrefersFreeMembersButFallsBackForOneMemberMigratedSave()
        {
            var state = new GuildCityPresentationState017D
            {
                Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_TRAINING",
                        RecruitName = "Ada",
                        Kind = "Training"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_RESERVE",
                        RecruitName = "Bram",
                        Kind = "Reserve"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_RECOVERING",
                        RecruitName = "Cora",
                        Kind = "Recovering"
                    }
                }
            };

            Assert.That(M1FlowPresenter.GuildCityWorkshopStaffCandidateIdsForVerification078(state),
                Is.EqualTo(new[] { "RECRUIT_RESERVE" }),
                "Canonical rosters should keep training and recovery orders intact while anyone is free.");

            state.Assignments = new[] { state.Assignments[0] };
            Assert.That(M1FlowPresenter.GuildCityWorkshopStaffCandidateIdsForVerification078(state),
                Is.EqualTo(new[] { "RECRUIT_TRAINING" }),
                "A migrated one-member roster must be offered the authoritative move into Staff duty.");
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void PreservedHomecomingHelperStillFitsFourDistinctPeopleOffTheHomePage(
            int width,
            int height)
        {
            var ribbon = M1FlowPresenter.LivingGuildHubHomecomingRegionForVerification076;
            var cameos = M1FlowPresenter.LivingGuildHubHomecomingCameoRectsForVerification076();

            Assert.That(ribbon.width * width, Is.GreaterThanOrEqualTo(1000f));
            Assert.That(ribbon.height * height, Is.GreaterThanOrEqualTo(90f));
            Assert.That(cameos, Has.Count.EqualTo(4));
            foreach (var cameo in cameos)
            {
                Assert.That(cameo.width * ribbon.width * width,
                    Is.GreaterThanOrEqualTo(175f));
                Assert.That(cameo.height * ribbon.height * height,
                    Is.GreaterThanOrEqualTo(76f));
            }
            for (var left = 0; left < cameos.Count; left++)
            for (var right = left + 1; right < cameos.Count; right++)
                Assert.That(cameos[left].Overlaps(cameos[right]), Is.False);
            Assert.That(ribbon.Overlaps(M1FlowPresenter.LivingGuildHubRosterRegionForVerification074),
                Is.False);
        }

        [Test]
        public void EvolvedRosterMixesFoundersAndRescuedPatrolInsteadOfRepeatingTheFirstSix()
        {
            var founders = Enumerable.Range(1, 10).Select(index => "FOUNDER_" + index);
            var patrol = FirstHourRosterService071.PatrolStableRecruitIds;
            var featured = M1FlowPresenter.LivingGuildHubFeaturedAuthorityIdsForVerification076(
                founders.Concat(patrol).ToArray(),
                homecoming: true);

            Assert.That(featured, Has.Count.EqualTo(6));
            Assert.That(featured.Take(3), Is.EqualTo(new[]
            {
                "FOUNDER_1", "FOUNDER_2", "FOUNDER_3"
            }));
            Assert.That(featured.Skip(3), Is.EqualTo(patrol.Take(3)));
            Assert.That(featured.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(6));
        }

        [Test]
        public void SixRosterCardsReserveSeparateIdentityAndReadinessRowsAt1280x800()
        {
            const int width = 1280;
            const int height = 800;
            var roster = M1FlowPresenter.LivingGuildHubRosterRegionForVerification074;
            var identity = M1FlowPresenter.LivingGuildHubRosterIdentityRegionForVerification074;
            var readiness = M1FlowPresenter.LivingGuildHubRosterReadinessRegionForVerification074;
            var cards = M1FlowPresenter.LivingGuildHubRosterCardRectsForVerification074(6);

            Assert.That(cards, Has.Count.EqualTo(M1FlowPresenter.LivingGuildHubRosterVisibleLimit074));
            Assert.That(identity.Overlaps(readiness), Is.False,
                "Readiness must have its own fixed row and can never be displaced by a wrapping name.");
            foreach (var card in cards)
            {
                var cardWidth = card.width * roster.width * width;
                var cardHeight = card.height * roster.height * height;
                Assert.That(cardWidth, Is.GreaterThanOrEqualTo(172f));
                Assert.That(cardHeight, Is.GreaterThanOrEqualTo(104f));
                Assert.That(identity.width * cardWidth, Is.GreaterThanOrEqualTo(112f));
                Assert.That(identity.height * cardHeight, Is.GreaterThanOrEqualTo(68f));
                Assert.That(readiness.width * cardWidth, Is.GreaterThanOrEqualTo(112f));
                Assert.That(readiness.height * cardHeight, Is.GreaterThanOrEqualTo(26f));
                Assert.That(identity.height * cardHeight / 22f,
                    Is.GreaterThanOrEqualTo(M1FlowPresenter.LivingGuildHubRosterIdentityLineCapacity074));
            }

            Assert.That(M1FlowPresenter.LivingGuildHubRosterIdentityMinimumFontSize076,
                Is.GreaterThanOrEqualTo(22));
            Assert.That(M1FlowPresenter.LivingGuildHubRosterIdentityMaximumFontSize076,
                Is.LessThanOrEqualTo(26));

            for (var left = 0; left < cards.Count; left++)
            for (var right = left + 1; right < cards.Count; right++)
                Assert.That(cards[left].Overlaps(cards[right]), Is.False);
        }

        [TestCase("Daevan Fellstar", "Warrior", 1, "Daevan\nFellstar\nWarrior L1")]
        [TestCase("Tazren Warmask", "Ranger", 3, "Tazren\nWarmask\nRanger L3")]
        [TestCase("Zorin Bramblecross", "Guardian", 2, "Zorin\nBramblecross\nGuardian L2")]
        [TestCase("Willow Longstride", "Guardian", 2, "Willow\nLongstride\nGuardian L2")]
        public void LongRosterIdentitiesStayCompleteWhileReadinessRemainsIndependent(
            string displayName,
            string role,
            int level,
            string expectedIdentity)
        {
            var identity = M1FlowPresenter.LivingGuildHubRosterIdentityForVerification074(
                displayName,
                role,
                level);

            Assert.That(identity, Is.EqualTo(expectedIdentity));
            Assert.That(identity, Does.Not.Contain("…"));
            Assert.That(identity, Does.Not.Contain("READY"));
            Assert.That(M1FlowPresenter.LivingGuildHubRosterReadinessForVerification074(true),
                Is.EqualTo("READY"));
            Assert.That(M1FlowPresenter.LivingGuildHubRosterReadinessForVerification074(false),
                Is.EqualTo("CHECK GEAR"));
        }

        [TestCase("Zorin Bramblecross", "Zorin\nBramblecross")]
        [TestCase("Seraphina de la Nightveil", "Seraphina de\nla Nightveil")]
        [TestCase("  Willow   Longstride  ", "Willow\nLongstride")]
        public void RosterNameLockupUsesTwoBalancedWholeWordLines(
            string displayName,
            string expected)
        {
            var lockup = M1FlowPresenter.LivingGuildHubRosterNameForVerification076(displayName);
            Assert.That(lockup, Is.EqualTo(expected));
            Assert.That(lockup.Split('\n'), Has.Length.EqualTo(2));

            var sourceWords = displayName.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            var renderedWords = lockup.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            Assert.That(renderedWords, Is.EqualTo(sourceWords),
                "Every authored name token must survive; layout may only break between words.");
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void GuildCharterCardRegionsStaySeparateAtSupportedDesktopResolutions(
            int width,
            int height)
        {
            const float cardWidth = 0.345f;
            const float cardHeight = 0.950f;
            var regions = M1FlowPresenter.GuildCharterCardRegionsForVerification074;
            Assert.That(regions, Has.Count.EqualTo(8));
            foreach (var region in regions)
            {
                Assert.That(region.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(region.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(region.xMax, Is.LessThanOrEqualTo(1f));
                Assert.That(region.yMax, Is.LessThanOrEqualTo(1f));
                Assert.That(region.width * cardWidth * width, Is.GreaterThanOrEqualTo(390f));
                Assert.That(region.height * cardHeight * height, Is.GreaterThanOrEqualTo(34f));
            }

            for (var left = 0; left < regions.Count; left++)
            for (var right = left + 1; right < regions.Count; right++)
                Assert.That(regions[left].Overlaps(regions[right]), Is.False,
                    "Story, objective, signature, and action regions must never overlap.");
        }

        [Test]
        public void RecruitmentGearUsesReadableCompleteLabelsWithoutAuthorityIdsOrEllipsis()
        {
            var visible = M1FlowPresenter.RecruitmentEquipmentSummaryForVerification074(
                "EQ_PROC_ROPE, EQ_PROC_FREIGHT_TAG, EQ_PROC_LOAD_HARNESS, EQ_PROC_TRAVEL_SPEAR");

            Assert.That(visible,
                Is.EqualTo("Rope  •  Freight Tag  •  Load Harness  •  Travel Spear"));
            Assert.That(visible, Does.Not.Contain("EQ_"));
            Assert.That(visible, Does.Not.Contain("PROC"));
            Assert.That(visible, Does.Not.Contain("…"));
            Assert.That(visible, Does.Not.Contain("..."));
        }

        [Test]
        public void ContractBoardFeaturedStackFitsItsFixedCard()
        {
            Assert.That(M1FlowPresenter.ContractBoardFeaturedRequiredHeightForVerification074(false),
                Is.LessThanOrEqualTo(M1FlowPresenter.ContractBoardFeaturedDetailsHeight074));
            Assert.That(M1FlowPresenter.ContractBoardFeaturedRequiredHeightForVerification074(true),
                Is.LessThanOrEqualTo(M1FlowPresenter.ContractBoardFeaturedDetailsHeight074));
            Assert.That(M1FlowPresenter.ContractBoardFeaturedRequiredHeightForVerification074(true),
                Is.EqualTo(534f));
            Assert.That(M1FlowPresenter.ContractBoardFeaturedDetailsHeight074, Is.EqualTo(544f));
        }
    }
}
