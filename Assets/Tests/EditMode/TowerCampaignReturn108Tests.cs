using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerCampaignReturn108Tests
    {
        [Test, Timeout(300000)]
        public void EarnedTowerClearCanBankStartIdleFloorThenEnterStoryAndReload108()
        {
            var source = Environment.GetEnvironmentVariable("SD_TOWER_CAMPAIGN108_SOURCE");
            var output = Environment.GetEnvironmentVariable("SD_TOWER_CAMPAIGN108_EVIDENCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source) ||
                string.IsNullOrWhiteSpace(output))
                Assert.Ignore("Set the earned Tower source and evidence paths for this isolated regression.");

            Directory.CreateDirectory(output);
            var sourceHash = Hash108(source);
            var save = Path.Combine(output, "tower_campaign_return_isolated.json");
            File.Copy(source, save, true);
            var coordinator = new M1RuntimeCoordinator(null, save);

            var terminal = M2BattleViewAccess098.Read(coordinator);
            Assert.That(terminal.IsResolved, Is.True);
            Assert.That(terminal.Outcome, Is.EqualTo("Victory"));
            Assert.That(terminal.Reward.Claimed, Is.False);
            Assert.That(coordinator.ClaimBattleRewards().Succeeded, Is.True);

            for (var guard = 0;
                 !string.IsNullOrWhiteSpace(coordinator.CampaignProgression022.ActiveAbyssOperationId) && guard < 32;
                 guard++)
            {
                var advanced = coordinator.AdvanceTowerRun081();
                Assert.That(advanced.Succeeded, Is.True, advanced.Message);
            }
            Assert.That(coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.Empty);

            var begun = coordinator.BeginTowerRun081();
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            var idle = coordinator.CampaignProgression022;
            Assert.That(idle.ActiveAbyssOperationId, Is.Not.Empty);
            Assert.That(idle.TowerBattleInProgress, Is.False);

            var story = coordinator.StartPlayableChapter020("CH018_001");
            Assert.That(story.Succeeded, Is.True, story.Message);
            Assert.That(coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.Empty,
                "Starting Story must close the idle Tower boundary instead of restoring it.");
            Assert.That(coordinator.CampaignPlayable020.ActiveChapterId, Is.EqualTo("CH018_001"));

            var reloaded = new M1RuntimeCoordinator(null, save);
            Assert.That(reloaded.CampaignProgression022.ActiveAbyssOperationId, Is.Empty);
            Assert.That(reloaded.CampaignPlayable020.ActiveChapterId, Is.EqualTo("CH018_001"));
            Assert.That(Hash108(source), Is.EqualTo(sourceHash), "The earned source save changed.");
        }

        private static string Hash108(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
