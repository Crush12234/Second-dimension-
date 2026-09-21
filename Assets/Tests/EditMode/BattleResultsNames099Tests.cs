using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.M2;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleResultsNames099Tests
    {
        M2CombatContent _content;
        [OneTimeSetUp] public void Load099() => _content = M2CombatContent.LoadFromDirectory(
            Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));

        [TestCase("TREE_CA002_WPN_SWORD_N04", "Suyin Wispbound learned Crescent Step through Guardbreaker. Ready for future battles.", "Crescent Step")]
        [TestCase("TREE_CA002_WPN_ENGINEERING_N04", "Colt Ashbarrel learned Snare Plate through Signal Flare. Ready for future battles.", "Snare Plate")]
        [TestCase("TREE_CA002_ROLE_DUELIST_N03", "Elow Fernharp learned Single Out through Answering Cut. Ready for future battles.", "Single Out")]
        [TestCase("ART_STAND_AGAIN", "Odelia Fen learned Stand Again through Field Medic training. Revives a fallen ally through a Union rescue order (6 AP / 12 MP).", "Stand Again")]
        public void ActualCommittedCaptionKeepsItsExactExistingArtName099(string artId, string caption, string expected)
        {
            Assert.That(_content.Art(artId).Name, Is.EqualTo(expected), "Expected name must match the live content authority.");
            Assert.That(M2BattleReadableText021.ArtDisplayName(artId, caption, true), Is.EqualTo(expected));
            Assert.That(M2BattleReadableText021.NewArtLearned(artId, caption),
                Is.EqualTo("NEW ART LEARNED\n" + expected.ToUpperInvariant()));
        }

        [Test]
        public void ArchivedEarnedTowerEventsResolveTheirAuthoredNamesWithoutChangingTheSave099()
        {
            var path = Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", "R100_OldCompletedFloor010.json");
            var original = File.ReadAllBytes(path);
            var saved = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(saved.IsSuccess, Is.True, string.Join(";", saved.Errors));
            var learned = saved.Value.CampaignState.Battle.EventLog
                .Where(item => item.EventType == "BREAKTHROUGH" && item.ArtId.StartsWith("TREE_CA002_", StringComparison.Ordinal)).ToArray();
            Assert.That(learned.Length, Is.GreaterThanOrEqualTo(4), "Exercise recorded real combat, not only formatted test prose.");
            foreach (var item in learned)
                Assert.That(M2BattleReadableText021.ArtDisplayName(item.ArtId, item.Text, true),
                    Is.EqualTo(_content.Art(item.ArtId).Name), item.ArtId + ": " + item.Text);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        }

        [Test]
        public void EveryCurrentNamedTreeArtSurvivesTheShippingLearnedSentence099()
        {
            var arts = _content.Arts.Values.Where(art => art.Id.StartsWith("TREE_CA002_", StringComparison.Ordinal)).ToArray();
            Assert.That(arts.Length, Is.EqualTo(360));
            foreach (var art in arts)
            {
                var caption = "Guild adventurer learned " + art.Name + " through prior training. Ready for future battles.";
                Assert.That(M2BattleReadableText021.ArtDisplayName(art.Id, caption, true), Is.EqualTo(art.Name), art.Id);
            }
        }

        [Test]
        public void RegisteredAndLegacyNamesRetainTheirPreviousPrecedence099()
        {
            Assert.That(M2BattleReadableText021.ArtDisplayName("TREE_CA002_WPN_SWORD_N04",
                "Someone learned Obsolete Name through training.", true, "Crescent Step"), Is.EqualTo("Crescent Step"));
            Assert.That(M2BattleReadableText021.ArtDisplayName("ART_POWER_CUT",
                "Maren turns meaningful Saber Cut use into a breakthrough and learns Power Cut!", true), Is.EqualTo("Power Cut"));
            Assert.That(M2BattleReadableText021.ArtDisplayName("TREE_CA002_WPN_SWORD_N04",
                "Legacy record without an authored name.", true), Is.EqualTo("Sword Art 4"),
                "Missing metadata stays an honest fallback; do not invent a named Art from its ordinal.");
        }
    }
}
