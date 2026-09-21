using NUnit.Framework;
using SecondDimension.Gameplay.M2;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EarnedRecruitFeedback099Tests
    {
        [TestCase(0, 1, "1 duplicate invitation applied")]
        [TestCase(0, 2, "2 duplicate invitations applied")]
        [TestCase(3, 1, "3 new recruits joined your reserves")]
        [TestCase(1, 0, "1 new recruit joined your reserves")]
        public void ClaimCopySeparatesBodiesFromConsumedInvitations099(int joined, int duplicates, string expected)
        {
            var text = EarnedRecruitFeedback099.ClaimSummary099(joined, duplicates);
            Assert.That(text, Does.Contain(expected));
            Assert.That(text, Does.Not.Contain("0 new").And.Not.Contain("0 earned").And.Not.Contain("0 joined"));
            Assert.That(text, Does.Contain("No XP spent"));
            if (duplicates > 0) Assert.That(text, Does.Contain("Ascension / skill growth"));
            if (joined == 0) Assert.That(text, Does.Not.Contain("joined your reserves"));
        }

        [Test] public void ReplayDoesNotAnnounceAnotherMerge099() =>
            Assert.That(EarnedRecruitFeedback099.ClaimSummary099(0, 0), Is.EqualTo("No new earned rewards to claim."));

        [Test] public void AscensionCopyComparesActualProjectedStats099()
        {
            var before = new M1RecruitLoadoutView { RecruitId = "EXACT", AscensionLevel = 2, MaximumHp = 300, MaximumMp = 90 };
            var after = new M1RecruitLoadoutView { RecruitId = "EXACT", AscensionLevel = 3, MaximumHp = 330, MaximumMp = 96 };
            Assert.That(EarnedRecruitFeedback099.CommittedGrowth099(before, after),
                Is.EqualTo("ASCENDED 2 → 3/10\n+30 HP  •  +6 MP"));
            Assert.That(before.AscensionLevel, Is.EqualTo(2), "Formatting never applies growth.");
        }

        [Test] public void ArtLevelCopyUsesExistingMasteryThresholdsAndNamedArt099()
        {
            var before = new M1RecruitLoadoutView { RecruitId = "EXACT", AscensionLevel = 10,
                ArtMastery = new[] { new M1ArtMasteryView { ArtId = "ART_TEST", DisplayName = "Crescent Step",
                    MasteryPoints = M2ArtMasteryLevelPolicy088.ThresholdForLevel(3) } } };
            var after = new M1RecruitLoadoutView { RecruitId = "EXACT", AscensionLevel = 10,
                ArtMastery = new[] { new M1ArtMasteryView { ArtId = "ART_TEST", DisplayName = "Crescent Step",
                    MasteryPoints = M2ArtMasteryLevelPolicy088.ThresholdForLevel(4) } } };
            Assert.That(EarnedRecruitFeedback099.CommittedGrowth099(before, after), Is.EqualTo("Crescent Step\nART LEVEL 3 → 4"));
        }

        [Test] public void TreeUnlockCopyOnlyNamesNewlyPresentLearnedArts099()
        {
            var before = new M1RecruitLoadoutView { RecruitId = "EXACT", LearnedArtIds = new[] { "OLD" } };
            var after = new M1RecruitLoadoutView { RecruitId = "EXACT", LearnedArtIds = new[] { "OLD", "NEW" },
                ArtMastery = new[] { new M1ArtMasteryView { ArtId = "NEW", DisplayName = "Ember Thread" } } };
            Assert.That(EarnedRecruitFeedback099.CommittedGrowth099(before, after),
                Is.EqualTo("NEW SKILL TREE / ART UNLOCKED\nEmber Thread"));
            Assert.That(after.LearnedArtIds, Is.EquivalentTo(new[] { "OLD", "NEW" }));
        }

        [Test] public void NoDeltaOrDifferentIdentityDoesNotInventGrowth099()
        {
            var before = new M1RecruitLoadoutView { RecruitId = "EXACT", AscensionLevel = 10, TotalPersonalXp = 100 };
            Assert.That(EarnedRecruitFeedback099.CommittedGrowth099(before, before), Is.Empty);
            Assert.That(EarnedRecruitFeedback099.CommittedGrowth099(before,
                new M1RecruitLoadoutView { RecruitId = "OTHER", AscensionLevel = 10, TotalPersonalXp = 200 }), Is.Empty);
            Assert.That(EarnedRecruitFeedback099.AppliedWithoutDelta099, Does.Contain("caps retained").And.Not.Contain("+"));
            Assert.That(EarnedRecruitFeedback099.CommittedInvitationSummary099(before, before),
                Is.EqualTo("INVITATION APPLIED\nExisting progression caps retained"),
                "The production receipt-result route must retain the owned hero card even when a cap leaves its DTO unchanged.");
            Assert.That(EarnedRecruitFeedback099.CommittedInvitationSummary099(before,
                new M1RecruitLoadoutView { RecruitId = "OTHER" }), Is.Empty);
        }
    }
}
