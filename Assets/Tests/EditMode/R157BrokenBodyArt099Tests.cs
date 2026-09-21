using System.Collections.Generic;
using NUnit.Framework;
using Pair099 = SecondDimension.Tests.EditMode.R148PartyArt099Tests.ReviewedPair099;

namespace SecondDimension.Tests.EditMode
{
    // Exact new originals replacing the three defective nonprocedural R157 party bodies.
    // These repairs do not reduce the separate remaining procedural-placeholder census.
    // Root source approval is an import prerequisite; test success is not timed/native animation proof.
    public sealed class R157BrokenBodyArt099Tests
    {
        static IEnumerable<Pair099> ReviewedPairs099()
        {
            yield return new Pair099
            {
                Id = "HERO_REC_219",
                Name = "Rook Chainveil",
                Race = "Human",
                Role = "Chain Reaper",
                Weapon = "Chain Sickle",
                Sha = "D79F22FD06D82DCAD7B4ED97C5DEA6C6C0368A8BCC3BC78AFB96CB7AFB994833",
                Split = 772,
                GutterFrom = 559,
                GutterThrough = 852,
                IdleWidth = 506,
                IdleHeight = 960,
                IdleVisible = 243923,
                ActionWidth = 663,
                ActionHeight = 925,
                ActionVisible = 241092
            };
            yield return new Pair099
            {
                Id = "HERO_REC_237",
                Name = "Bront Ashhorn",
                Race = "Minotaur",
                Role = "Ash Reaver",
                Weapon = "Double Axe",
                Sha = "F7DDEDFD4DDDC95B9CCEC29CC2230A57346B18DB8A6F5DA22D8F641D53361FE7",
                Split = 750,
                GutterFrom = 737,
                GutterThrough = 761,
                IdleWidth = 712,
                IdleHeight = 922,
                IdleVisible = 321001,
                ActionWidth = 758,
                ActionHeight = 871,
                ActionVisible = 320603
            };
            yield return new Pair099
            {
                Id = "HERO_REC_263",
                Name = "Korin Bluefist",
                Race = "Human",
                Role = "Blue Aura Monk",
                Weapon = "Gauntlets",
                Sha = "7E99DD7105A47C7921EAD1B7505DCCD4803CBB6C979522FB13B25E0168B6DD25",
                Split = 768,
                GutterFrom = 628,
                GutterThrough = 833,
                IdleWidth = 497,
                IdleHeight = 895,
                IdleVisible = 229452,
                ActionWidth = 684,
                ActionHeight = 852,
                ActionVisible = 254287
            };
        }

        [TestCaseSource(nameof(ReviewedPairs099))]
        public void CompleteR157OriginalBindsExactProductionStandingActionAndPortrait099(Pair099 expected) =>
            new R148PartyArt099Tests().ExactReviewedOriginalBindsBothProductionPosesAndPortrait099(expected);

        [TestCaseSource(nameof(ReviewedPairs099))]
        public void CompleteR157FramesPreserveInspectedBodyAndEquipmentGeometry099(Pair099 expected) =>
            new R148PartyArt099Tests().BothReviewedCellsRetainCompleteInspectedAlphaBounds099(expected);

        [TestCaseSource(nameof(ReviewedPairs099))]
        public void CompleteR157ReplacementNeverBorrowsAnotherHero099(Pair099 expected) =>
            new R148PartyArt099Tests().ExactRegistrationDoesNotBorrowForAnotherIdentity099(expected);
    }
}
