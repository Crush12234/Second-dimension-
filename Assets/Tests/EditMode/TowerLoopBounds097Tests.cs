#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Release030;

namespace SecondDimension.Tests.EditMode
{
    // Numeric/path contract fixtures only. Real victories belong to the explicit
    // TowerLoop097Batch runner, never these pure boundary tests.
    public sealed class TowerLoopBounds097Tests
    {
        [TestCase(1, 0, 1)]
        [TestCase(2, 0, 1)]
        [TestCase(10, 0, 5)]
        [TestCase(10, 4, 9)]
        [TestCase(10, 5, 10)]
        [TestCase(1, 9, 10)]
        [TestCase(int.MinValue, int.MinValue, 1)]
        [TestCase(int.MaxValue, 0, 10)]
        [TestCase(1, int.MaxValue, 10)]
        [TestCase(10, int.MaxValue, 10)]
        [TestCase(int.MaxValue, int.MaxValue, 10)]
        [TestCase(int.MinValue, int.MaxValue, 10)]
        public void EnemyUnionCapIsAppliedBeforeAnyOverflow097(int floor, int clears, int expected)
        {
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(floor, clears),
                Is.EqualTo(expected));
        }

        [Test]
        public void OverflowFixPreservesEveryHistoricalTemplateAndOrdinaryRepeat097()
        {
            for (var floor = 1; floor <= 10; floor++)
            for (var clears = 0; clears <= 200; clears++)
            {
                // Independent widened-number expression for the retained published rule.
                var expected = (int)Math.Min(10L, 1L + (floor - 1L) / 2L + clears);
                Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(floor, clears),
                    Is.EqualTo(expected), "template " + floor + ", prior clears " + clears);
            }
        }

        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(11, 1)]
        [TestCase(50, 10)]
        [TestCase(500, 10)]
        [TestCase(501, 1)]
        [TestCase(int.MaxValue, 7)]
        public void ActualFloorMappingNeverCreatesAnEleventhTemplate097(int floor, int expected)
        {
            Assert.That(CampaignProgressionCommandService022.TowerContentTemplate094(floor),
                Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void InvalidActualFloorDoesNotWrapIntoARewardOrTemplate097(int floor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CampaignProgressionCommandService022.TowerContentTemplate094(floor));
            Assert.That(TowerHeroRewardRules094.IsRewardFloor(floor), Is.False);
            Assert.That(TowerHeroRewardRules094.IsGuaranteedFloor(floor), Is.False);
        }

        [TestCase(499, "", false)]
        [TestCase(500, "SSS", true)]
        [TestCase(510, "SSS", false)]
        [TestCase(550, "SSS", true)]
        [TestCase(2147483600, "SSS", true)]
        [TestCase(2147483640, "SSS", false)]
        [TestCase(int.MaxValue, "", false)]
        public void LaterRewardThresholdsDoNotOverflowIntoTheWrongTier097(int floor, string tier, bool guaranteed)
        {
            Assert.That(TowerHeroRewardRules094.TierForFloor(floor), Is.EqualTo(tier));
            Assert.That(TowerHeroRewardRules094.IsGuaranteedFloor(floor), Is.EqualTo(guaranteed));
        }

        [TestCase(500)]
        [TestCase(2147483600)]
        [TestCase(2147483640)]
        public void LateRewardSelectionIsStableAcrossAttemptAndReloadButDoesNotGrantAnything097(int floor)
        {
            var campaign = new CampaignState("TOWER097_PURE_SELECTION_NOT_A_CLEAR", 970123L,
                "TOWER097_RULE_FIXTURE", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD_TOWER097", 0, Array.Empty<RecruitState>(), Array.Empty<UnionState>()));
            var before = CanonicalJson.Serialize(campaign);
            var first = TowerHeroRewardRules094.BuildPlan(campaign, "ATTEMPT_097_ONE", floor, null);
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            var retry = TowerHeroRewardRules094.BuildPlan(reloaded, "ATTEMPT_097_TWO", floor, null);
            Assert.That(first.CandidateHeroIds, Is.EqualTo(SssTenV4Roster090.All
                .Select(value => value.HeroId).OrderBy(value => value, StringComparer.Ordinal).ToArray()));
            Assert.That(first.CandidateHeroIds.Count, Is.EqualTo(10));
            Assert.That(retry.SavedRoll0To9999, Is.EqualTo(first.SavedRoll0To9999));
            Assert.That(retry.SelectionIndex, Is.EqualTo(first.SelectionIndex));
            Assert.That(retry.SelectedHeroId, Is.EqualTo(first.SelectedHeroId));
            Assert.That(retry.PlanId, Is.Not.EqualTo(first.PlanId));
            if (TowerHeroRewardRules094.IsGuaranteedFloor(floor)) Assert.That(first.WinsHero, Is.True);
            Assert.That(CanonicalJson.Serialize(campaign), Is.EqualTo(before));
            Assert.That(CanonicalJson.Serialize(reloaded), Is.EqualTo(before));
        }

        [Test]
        public void FiniteLedgerFailsClosedAtLimitAndRetainsIdempotentReplay097()
        {
            var entries = Enumerable.Range(0, GuildDevelopmentState.AdventureAuthorityEntryLimit)
                .Select(value => "BOUNDARY097_" + value.ToString("D8")).ToArray();
            var ledger = new GuildDevelopmentState(0, GuildDevelopmentState.InitialHallStageId, 0, 0,
                Array.Empty<FacilityProgressionState>(), Array.Empty<string>(), entries);
            Assert.That(ledger.CanRecordAdventureAuthority("OVER_LIMIT_097"), Is.False);
            Assert.Throws<InvalidOperationException>(() => ledger.RecordAdventureAuthority("OVER_LIMIT_097"));
            Assert.That(ledger.RecordAdventureAuthority(entries[0]), Is.SameAs(ledger));
            Assert.That(ledger.AppliedAdventureAuthorityIds.Count, Is.EqualTo(entries.Length));
            Assert.Throws<ArgumentException>(() => new GuildDevelopmentState(0,
                GuildDevelopmentState.InitialHallStageId, 0, 0, Array.Empty<FacilityProgressionState>(),
                Array.Empty<string>(), entries.Concat(new[] { "OVER_LIMIT_097" }).ToArray()));
        }

        [TestCase(0, 1800)]
        [TestCase(551, 1800)]
        [TestCase(12, 29)]
        [TestCase(12, 10801)]
        public void HarnessRejectsUnboundedTargetsBeforeReadingOrCreatingAnySave097(int floor, int seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TowerLoopVerification097().Run("", "", "", floor, seconds));
        }

        [Test]
        public void HarnessNeverUsesOriginalSaveDirectory097()
        {
            var folder = Path.Combine(Path.GetTempPath(), "TowerIsolation097_" + Guid.NewGuid().ToString("N"));
            // Guard is lexical and runs before any IO: no real saved progress is created or loaded.
            Assert.Throws<ArgumentException>(() => new TowerLoopVerification097().Run("",
                Path.Combine(folder, "CampaignSave093.json"), folder));
            Assert.That(Directory.Exists(folder), Is.False);
        }

        [Test]
        public void MissingEarnedSourceNeverCreatesEvidenceOrAReplacementCampaign097()
        {
            var folder = Path.Combine(Path.GetTempPath(), "TowerMissing097_" + Guid.NewGuid().ToString("N"));
            Assert.Throws<FileNotFoundException>(() => new TowerLoopVerification097().Run("",
                Path.Combine(folder, "source", "CampaignSave093.json"), Path.Combine(folder, "evidence")));
            Assert.That(Directory.Exists(folder), Is.False);
        }
    }
}
#endif
