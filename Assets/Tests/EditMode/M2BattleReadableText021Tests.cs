using System.Globalization;
using System.Threading;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2BattleReadableText021Tests
    {
        [Test]
        public void HitPointLabelsUsePlainInvariantEnglishAtAnyMachineLocale()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ar-EG");

                Assert.That(M2BattleReadableText021.HitPointChange(49, false), Is.EqualTo("-49 HP"));
                Assert.That(M2BattleReadableText021.HitPointChange(12, true), Is.EqualTo("+12 HP"));
                Assert.That(M2BattleReadableText021.HitPointChange(int.MinValue, false),
                    Is.EqualTo("-2147483648 HP"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Test]
        public void ArtXpCalloutUsesCanonicalNameEmbeddedInGrowthCaption()
        {
            var label = M2BattleReadableText021.ArtXpGain(
                "ART_BASIC_SABER_CUT",
                "Maren Holt grows Saber Cut through meaningful use: +5 mastery.",
                5);

            Assert.That(label, Is.EqualTo("SABER CUT\nART XP +5"));
            Assert.That(label, Does.Not.Contain("ART_BASIC_"));
        }

        [Test]
        public void ArtXpCalloutHumanizesStableIdWhenLegacyCaptionHasNoNameFields()
        {
            var label = M2BattleReadableText021.ArtXpGain(
                "ART_BASIC_SABER_CUT",
                "Meaningful use recorded.",
                5);

            Assert.That(label, Is.EqualTo("SABER CUT\nART XP +5"));
            Assert.That(label, Does.Not.Contain("_"));
        }

        [Test]
        public void BreakthroughCalloutExtractsTheLearnedEnglishArtName()
        {
            var label = M2BattleReadableText021.NewArtLearned(
                "ART_POWER_CUT",
                "Maren turns meaningful Saber Cut use into a breakthrough and learns Power Cut! " +
                "It enters later legal Forecast pools.");

            Assert.That(label, Is.EqualTo("NEW ART LEARNED\nPOWER CUT"));
        }

        [Test]
        public void ForecastRegistryNameReplacesDeepProgressionNodeIdInBattleBanner()
        {
            var label = M2BattleReadableText021.ArtBannerTitle(
                "Combat Art",
                "TREE_CA002_WPN_SWORD_N01",
                "Maren Holt attacks the Gate Gnawer.",
                "Ready Cut");

            Assert.That(label, Is.EqualTo("COMBAT ART · READY CUT"));
            Assert.That(label, Does.Not.Contain("TREE"));
            Assert.That(label, Does.Not.Contain("CA002"));
            Assert.That(label, Does.Not.Contain("N01"));
        }

        [Test]
        public void ActionCaptionSuppliesReadableArtNameWhenForecastMetadataIsUnavailable()
        {
            var label = M2BattleReadableText021.ArtBannerTitle(
                "Mystic Art",
                "TREE_CA002_MYS_FLAME_N02",
                "Tovvi Copperspark uses Cinder Spiral on Gate Gnawer 1 for 27 HP.");

            Assert.That(label, Is.EqualTo("MYSTIC ART · CINDER SPIRAL"));
            Assert.That(label, Does.Not.Contain("TREE_CA002"));
        }

        [Test]
        public void DeepNodeFallbackIsFriendlyAndNeverLeaksProductionCodes()
        {
            var name = M2BattleReadableText021.ArtDisplayName(
                "TREE_CA002_MYS_RESTORATION_N04",
                "Restoration resolves.",
                false);

            Assert.That(name, Is.EqualTo("Restoration Art 4"));
            Assert.That(name, Does.Not.Contain("TREE"));
            Assert.That(name, Does.Not.Contain("CA002"));
            Assert.That(name, Does.Not.Contain("N04"));
        }

        [Test]
        public void TechnicalDisplayMetadataIsSanitizedInsteadOfEchoedVerbatim()
        {
            var name = M2BattleReadableText021.ArtDisplayName(
                "TREE_CA002_WPN_SPEAR_POLEARM_N03",
                string.Empty,
                false,
                "TREE_CA002_WPN_SPEAR_POLEARM_N03");

            Assert.That(name, Is.EqualTo("Spear & Polearm Art 3"));
            Assert.That(name, Does.Not.Contain("_"));
        }

        [Test]
        public void GenericPresentationOutcomeFallbackIsReadable()
        {
            var name = M2BattleReadableText021.ArtDisplayName(
                "SUCCESS_WITH_COST",
                string.Empty,
                false);

            Assert.That(name, Is.EqualTo("Success With Cost"));
            Assert.That(name, Does.Not.Contain("_"));
        }
    }
}
