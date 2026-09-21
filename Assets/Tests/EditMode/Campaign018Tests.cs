#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.Campaign018;

namespace SecondDimension.Tests.EditMode
{
    public sealed class Campaign018Tests
    {
        CampaignRegistry018 _registry;
        [SetUp] public void SetUp() => _registry = CampaignRegistry018.LoadFromResources();

        [Test] public void SevenRaceWorldsArePresent() => Assert.AreEqual(7, _registry.Worlds.Count);
        [Test] public void GoblinWorldIsFirstRaceWorldAndTimeLawIsLocked()
        {
            var goblin = _registry.Worlds["WORLD_GOBLIN_001"];
            StringAssert.Contains("1 Second Dimension day equals 1 Goblin World month", goblin.timeLaw);
            Assert.AreEqual("Kiri Aetherheart", goblin.founder);
        }
        [Test] public void EveryArcRejectsConquestChecklist() => Assert.IsTrue(_registry.Arcs.Values.All(a => a.noConquestChecklist));
        [Test] public void EveryChapterHasCivilizationContentAndNoIndividualArtButton()
        {
            Assert.IsTrue(_registry.Chapters.Values.All(c => c.nonCombatBeatRequired && c.relationshipMemoryRequired && c.cityConsequenceRequired && !c.individualArtSelection));
        }
        [Test] public void FortressOperationsReuse017HAndCertifiedBattle()
        {
            Assert.IsTrue(_registry.Sieges.Values.All(s => s.usesStrategicDefense017H && s.usesCertifiedUnionBattle && !s.createsSecondCombatResolver));
        }
        [Test] public void FortressOperationsRespectTwentyUnionCapacity()
        {
            Assert.IsTrue(_registry.Sieges.Values.All(s => s.maxAlliedUnions <= 10 && s.maxEnemyUnions <= 10));
        }
        [Test] public void ReceiptIsExactOnce()
        {
            var state = new CampaignProgressState018();
            var service = new CampaignProgressService018(_registry);
            var chapter = _registry.Chapters.Values.First();
            Assert.IsTrue(service.TryStartChapter(state, chapter.id, "REQ-1"));
            var receipt = new CampaignOperationReceipt018 { receiptId="REC-1", requestId="REQ-1", chapterId=chapter.id, outcome="VICTORY", guildXp=5, hallXp=5 };
            Assert.IsTrue(service.TryApplyReceipt(state, receipt));
            Assert.IsFalse(service.TryApplyReceipt(state, receipt));
        }
        [Test] public void FutureGoblinWarRequiresStoryGates()
        {
            var arc = _registry.Arcs["ARC018_GOBLIN_FORTRESS_WAR"];
            CollectionAssert.Contains(arc.unlockGates, "STORY:KAEL_IN_ENDLESS_ABYSS");
            CollectionAssert.Contains(arc.unlockGates, "STORY:FOUNDERS_SECOND_EVOLUTION_COMPLETE");
        }
        [Test] public void LaterWorldOrderIsNotMisrepresentedAsPublishedCanon()
        {
            var candidates = _registry.Arcs.Values.Where(a => a.worldId != "SKYHOME" && a.worldId != "WORLD_GOBLIN_001").ToArray();
            Assert.IsTrue(candidates.All(a => a.canonStatus == "GAME_CAMPAIGN_CANDIDATE_CANON_SAFE"));
        }
    }
}
#endif
