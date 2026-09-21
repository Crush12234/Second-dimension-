using System.Collections.Generic;
using NUnit.Framework;
using SecondDimension.Presentation;
using Pair099 = SecondDimension.Tests.EditMode.R148PartyArt099Tests.ReviewedPair099;

namespace SecondDimension.Tests.EditMode
{
    // Only root source-reviewed exact originals. Tests are not native art acceptance.
    public sealed class HeroArtFirstNine100Tests
    {
        static IEnumerable<Pair099> ReviewedPairs100()
        {
            yield return new Pair099
            {
                Id = "HERO_REC_013",
                Name = "Sylra Moonveil",
                Race = "Elf",
                Role = "Mystic",
                Weapon = "Crystal staff",
                Sha = "3E9B6E712B0ED51DA094FC23408E3F3F625E9E74957D2FF07B4B5E3D918071D9",
                Split = 755,
                GutterFrom = 642,
                GutterThrough = 872,
                IdleWidth = 536,
                IdleHeight = 921,
                IdleVisible = 248733,
                ActionWidth = 604,
                ActionHeight = 912,
                ActionVisible = 292454,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_030",
                Name = "Vex Ironshot",
                Race = "Goblin",
                Role = "Tinkerer",
                Weapon = "Arcane hand cannon",
                Sha = "0101D2A6BB8CAA99A92B38A3DBF4A7BE9FA43CE6FA5ECD705F23A4650815821D",
                Split = 773,
                GutterFrom = 617,
                GutterThrough = 872,
                IdleWidth = 482,
                IdleHeight = 868,
                IdleVisible = 276506,
                ActionWidth = 593,
                ActionHeight = 824,
                ActionVisible = 269551,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_033",
                Name = "Brokk Emberanvil",
                Race = "Dwarf",
                Role = "Guardian",
                Weapon = "Runic hammer and shield",
                Sha = "44E02BA541ADD47F51302EABDFAF678530045FC5955FB35F7B82BF7C7F6CA887",
                Split = 768,
                GutterFrom = 677,
                GutterThrough = 782,
                IdleWidth = 655,
                IdleHeight = 774,
                IdleVisible = 330407,
                ActionWidth = 743,
                ActionHeight = 723,
                ActionVisible = 296575,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_034",
                Name = "Elyra Starbow",
                Race = "Human",
                Role = "Tactical Ranger",
                Weapon = "Longbow",
                Sha = "8B845D01571FEFB465E39CBC0ACFB4FB2E690CECF0A3723C3488B416A9679332",
                Split = 768,
                GutterFrom = 695,
                GutterThrough = 909,
                IdleWidth = 547,
                IdleHeight = 908,
                IdleVisible = 246314,
                ActionWidth = 492,
                ActionHeight = 894,
                ActionVisible = 232067,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_041",
                Name = "Aveline Rook",
                Race = "Human",
                Role = "Vanguard",
                Weapon = "Sword and shield",
                Sha = "CC4DA880FC812AA29303217DD42C63B6FA2A3AD60712865F76A0857E34F02C14",
                Split = 768,
                GutterFrom = 637,
                GutterThrough = 825,
                IdleWidth = 540,
                IdleHeight = 922,
                IdleVisible = 247778,
                ActionWidth = 674,
                ActionHeight = 755,
                ActionVisible = 229678,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_044",
                Name = "Fenra Swiftpaw",
                Race = "Wolf Tribe",
                Role = "Ranger",
                Weapon = "Bow",
                Sha = "77DCE91B747425C26CB888478935C933D5F567AAEA5E3A404FBDB78FD2C7772E",
                Split = 805,
                GutterFrom = 657,
                GutterThrough = 859,
                IdleWidth = 562,
                IdleHeight = 958,
                IdleVisible = 231300,
                ActionWidth = 605,
                ActionHeight = 974,
                ActionVisible = 238032,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_047",
                Name = "Kharra Emberfury",
                Race = "Orc",
                Role = "Warlord",
                Weapon = "Molten axe",
                Sha = "9F6D3B4F4493D3409F4C7BC4063F14D5F819EA115BB1E6E7F46466642B27B26B",
                Split = 710,
                GutterFrom = 678,
                GutterThrough = 739,
                IdleWidth = 636,
                IdleHeight = 921,
                IdleVisible = 352761,
                ActionWidth = 776,
                ActionHeight = 844,
                ActionVisible = 325672,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_078",
                Name = "Garruk Whitefang",
                Race = "Lion Tribe",
                Role = "Guardian",
                Weapon = "Heavy gauntlet blade",
                Sha = "DA812704FBCBA80DE221F161F381243F19039E2FD2DAA20FD6C7AB1D84093893",
                Split = 768,
                GutterFrom = 705,
                GutterThrough = 824,
                IdleWidth = 562,
                IdleHeight = 845,
                IdleVisible = 268943,
                ActionWidth = 609,
                ActionHeight = 801,
                ActionVisible = 244996,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_220",
                Name = "Aurelia Orbit",
                Race = "Human",
                Role = "Orbit Cleric",
                Weapon = "Orb Staff",
                Sha = "B00126CDE41A636A84E1C656EC7E7C820612957B5DB1D241EDBB6F5D387832F4",
                Split = 768,
                GutterFrom = 672,
                GutterThrough = 829,
                IdleWidth = 562,
                IdleHeight = 947,
                IdleVisible = 254433,
                ActionWidth = 643,
                ActionHeight = 912,
                ActionVisible = 278207,
            };
        }
        [TestCaseSource(nameof(ReviewedPairs100))]
        public void ExactOriginalUsesProductionStandingActionAndPortrait100(Pair099 expected) =>
            new R148PartyArt099Tests().ExactReviewedOriginalBindsBothProductionPosesAndPortrait099(expected);
        [TestCaseSource(nameof(ReviewedPairs100))]
        public void ExactFramesRetainReviewedGeometryAndClearEdges100(Pair099 expected) =>
            new R148PartyArt099Tests().BothReviewedCellsRetainCompleteInspectedAlphaBounds099(expected);
        [TestCaseSource(nameof(ReviewedPairs100))]
        public void RegistrationNeverBorrowsAnotherIdentity100(Pair099 expected) =>
            new R148PartyArt099Tests().ExactRegistrationDoesNotBorrowForAnotherIdentity099(expected);
        [Test]
        public void ExactBatchUsesExistingReviewedSelection100()
        {
            foreach (var pair in ReviewedPairs100())
            {
                var number = int.Parse(pair.Id.Substring("HERO_REC_".Length));
                Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(number, pair.Id, 1, 300, true), Is.True);
                Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(number, pair.Id + "_UNKNOWN", 1, 300, true), Is.False);
                Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(number, pair.Id, number + 1, number + 1, true), Is.False);
            }
            Assert.That(HeroRosterBuiltPlayerAudit093.IsReviewedPoseIdentity099("HERO_REC_035"), Is.True);
        }
    }
}
