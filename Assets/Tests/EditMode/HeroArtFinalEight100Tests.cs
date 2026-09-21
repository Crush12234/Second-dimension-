using System.Collections.Generic;
using NUnit.Framework;
using SecondDimension.Presentation;
using Pair099 = SecondDimension.Tests.EditMode.R148PartyArt099Tests.ReviewedPair099;

namespace SecondDimension.Tests.EditMode
{
    // Only root source-reviewed exact originals. Tests are not native art acceptance.
    public sealed class HeroArtFinalEight100Tests
    {
        static IEnumerable<Pair099> ReviewedPairs100()
        {
            yield return new Pair099
            {
                Id = "HERO_REC_277",
                Name = "Grond Whitepelt",
                Race = "Beastkin",
                Role = "Whitepelt Reaver",
                Weapon = "Great Axe",
                Sha = "E8893D3B2487975BF6916D84126C9A94F5F37D4BC149164DCF759AA7D4532410",
                Split = 787,
                GutterFrom = 751,
                GutterThrough = 856,
                IdleWidth = 644,
                IdleHeight = 850,
                IdleVisible = 323251,
                ActionWidth = 610,
                ActionHeight = 821,
                ActionVisible = 271193,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_280",
                Name = "Tink Mossbrew",
                Race = "Gnome",
                Role = "Mossbrew Alchemist",
                Weapon = "Catalyst Cannon",
                Sha = "D8CBDE4A4876ACD6423355ECD249EE8976C8DC1ABCA75F41597614FAB28E484A",
                Split = 774,
                GutterFrom = 659,
                GutterThrough = 949,
                IdleWidth = 488,
                IdleHeight = 915,
                IdleVisible = 265238,
                ActionWidth = 521,
                ActionHeight = 783,
                ActionVisible = 232616,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_284",
                Name = "Ysara Glacier",
                Race = "Human",
                Role = "Glacier Sorceress",
                Weapon = "Ice Spear",
                Sha = "48828C67E8628534627B6F315D7A39E8119DCF60BF70CF85147B4FDFAE8D9156",
                Split = 775,
                GutterFrom = 716,
                GutterThrough = 859,
                IdleWidth = 549,
                IdleHeight = 961,
                IdleVisible = 258108,
                ActionWidth = 567,
                ActionHeight = 958,
                ActionVisible = 270779,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_289",
                Name = "Odelia Runebook",
                Race = "Human",
                Role = "Runebook Witch",
                Weapon = "Spellbooks",
                Sha = "FCE76C93E51B48F8185718B9DFE6B9FF88DDF02FD0ED716E0EF12982F4CF7DD7",
                Split = 786,
                GutterFrom = 660,
                GutterThrough = 877,
                IdleWidth = 574,
                IdleHeight = 980,
                IdleVisible = 300401,
                ActionWidth = 580,
                ActionHeight = 979,
                ActionVisible = 322489,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_292",
                Name = "Corvus Blackfeather",
                Race = "Human",
                Role = "Blackfeather Hunter",
                Weapon = "Raven Bow",
                Sha = "65691577F70F68205A41C025ABA6608C99DA517772F1885F615E1CFA1EBBF4A2",
                Split = 768,
                GutterFrom = 711,
                GutterThrough = 909,
                IdleWidth = 645,
                IdleHeight = 890,
                IdleVisible = 284218,
                ActionWidth = 576,
                ActionHeight = 876,
                ActionVisible = 268909,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_293",
                Name = "Brok Chainmane",
                Race = "Beastkin",
                Role = "Chainmane Berserker",
                Weapon = "Chain Axe",
                Sha = "90C7FA5798BCDA2B10EC52F2192080786E005FFB6E59EA54E3C23A976F1DF645",
                Split = 768,
                GutterFrom = 671,
                GutterThrough = 856,
                IdleWidth = 642,
                IdleHeight = 889,
                IdleVisible = 324015,
                ActionWidth = 608,
                ActionHeight = 940,
                ActionVisible = 322704,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_296",
                Name = "Kharox Emberhorn",
                Race = "Beastkin",
                Role = "Emberhorn Reaver",
                Weapon = "Crescent Axe",
                Sha = "70F68EE9DECB23B9D0EBAEB77D7C2C3982661CADE7D5D2BBBA9102B9E2CAADD4",
                Split = 768,
                GutterFrom = 699,
                GutterThrough = 883,
                IdleWidth = 666,
                IdleHeight = 959,
                IdleVisible = 369447,
                ActionWidth = 622,
                ActionHeight = 841,
                ActionVisible = 289072,
            };
            yield return new Pair099
            {
                Id = "HERO_REC_300",
                Name = "Seraphiel Goldwing",
                Race = "Fae",
                Role = "Goldwing Valkyrie",
                Weapon = "Radiant Spear",
                Sha = "45080B3E9DB5A1728FDB0ED7502CC9D08F4BC7C1B921BD0731B0A01E024D0356",
                Split = 768,
                GutterFrom = 643,
                GutterThrough = 912,
                IdleWidth = 525,
                IdleHeight = 957,
                IdleVisible = 305057,
                ActionWidth = 576,
                ActionHeight = 958,
                ActionVisible = 316550,
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
