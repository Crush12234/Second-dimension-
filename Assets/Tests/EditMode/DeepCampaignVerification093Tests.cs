using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.Release030;

namespace SecondDimension.Tests.EditMode
{
    public sealed class DeepCampaignVerification093Tests
    {
        [Test]
        public void BattleEvidenceCountsCommittedEventsOnceWithoutChangingBattle097()
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == 180);
            var row = new HeroRosterAuditRow093();
            var prepared = HeroRosterAudit093.SignOutfitAndPlace093(HeroRosterAudit093.CreateFixture093(hero), hero, row);
            var actual = HeroRosterAudit093.StartBattleAndVerifyForecast093(prepared, row);
            var forecast = actual.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_MYSTIC");
            var commands = new M2BattleCommandService();
            actual = HeroRosterAudit093.Require093(commands.SelectForecast(actual, forecast.UnionId, forecast.ForecastId));
            actual = HeroRosterAudit093.Require093(commands.ConfirmRound(actual, HeroRosterAudit093.LoadCombatContent093()));
            var before = CanonicalJson.Sha256Hex(actual);
            var evidence = new DeepCampaignBattleEvidence097();
            DeepCampaignVerification093.CaptureBattle097(evidence, actual.Battle);
            var evidenceHash = CanonicalJson.Sha256Hex(evidence);
            DeepCampaignVerification093.CaptureBattle097(evidence, actual.Battle);
            Assert.That(CanonicalJson.Sha256Hex(evidence), Is.EqualTo(evidenceHash));
            Assert.That(CanonicalJson.Sha256Hex(actual), Is.EqualTo(before));
            Assert.That(evidence.playerMembers, Is.EqualTo(1));
            Assert.That(evidence.enemyMembers, Is.EqualTo(3));
            Assert.That(evidence.rounds, Is.EqualTo(1));
            Assert.That(evidence.damagingHits, Is.GreaterThan(0));
            Assert.That(evidence.usedPlayerArtIds, Does.Contain("TREE_CA002_WPN_HYBRID_RELIC_N01"));
            Assert.That(evidence.revivals, Is.Zero, "An offered support candidate cannot be reported as an actual revival.");
            DeepCampaignVerification093.CaptureBattle097(evidence, null);
            Assert.That(CanonicalJson.Sha256Hex(evidence), Is.EqualTo(evidenceHash));
        }

        [Test]
        public void CompletedOpeningQuickPlayRoutesToExistingNextStoryInsteadOfAcceptingItAgain()
        {
            var state = CompletedOpening();
            Assert.That(M1FlowPresenter.ShouldOpenNextStoryInsteadOfQuickPlay093(state, false), Is.True);
            // Failed/repeated attempts and unrelated battle rewards are not chapter history.
            state.Contracts = Array.Empty<GuildCityContractView017D>();
            state.OperationOrdinal = 2;
            state.ClaimedBattleRewardCount = 2;
            Assert.That(M1FlowPresenter.ShouldOpenNextStoryInsteadOfQuickPlay093(state, false), Is.False);
        }

        [Test]
        public void FailedOpeningRetryDoesNotInventWayglassCompletion134()
        {
            var state = CompletedOpening();
            state.OperationOrdinal = 2;
            state.ClaimedBattleRewardCount = 10;
            Assert.That(StoryCompleted134(state, "CONTRACT_BELL_BENEATH_GATE"), Is.True);
            Assert.That(StoryCompleted134(state, "CONTRACT_LINES_NOT_RETURNED"), Is.False);
            Assert.That(StoryCompleted134(state, "CONTRACT_RELIEF_ROAD"), Is.False);
        }

        [Test, Timeout(120000)]
        public void EarnedRetryHistoryProjectionRetainsOnlyActuallyRewardedContracts134()
        {
            var source = Environment.GetEnvironmentVariable("SD_STORY134_SOURCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
                Assert.Ignore("Set SD_STORY134_SOURCE to the preserved earned retry checkpoint.");
            var original = File.ReadAllBytes(source);
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionStory134", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var save = Path.Combine(directory, "earned.json");
                File.WriteAllBytes(save, original);
                var coordinator = new M1RuntimeCoordinator(Path.Combine(UnityEngine.Application.streamingAssetsPath,
                    "Authority", "CONTENT"), save);
                var state = coordinator.GuildCity017D;
                Assert.That(state.OperationOrdinal, Is.GreaterThanOrEqualTo(2));
                Assert.That(state.Contracts.Single(c => c.ContractId == "CONTRACT_BELL_BENEATH_GATE").IsCompleted, Is.True);
                Assert.That(StoryCompleted134(state, "CONTRACT_BELL_BENEATH_GATE"), Is.True);
                Assert.That(StoryCompleted134(state, "CONTRACT_RELIEF_ROAD"), Is.True,
                    "Completed contracts must survive replacement of the current active contract.");
                Assert.That(StoryCompleted134(state, "CONTRACT_LINES_NOT_RETURNED"), Is.False,
                    "Failure plus a retry win must not mark the unplayed second story complete.");
                var reloaded = new M1RuntimeCoordinator(Path.Combine(UnityEngine.Application.streamingAssetsPath,
                    "Authority", "CONTENT"), save);
                Assert.That(StoryCompleted134(reloaded.GuildCity017D, "CONTRACT_LINES_NOT_RETURNED"), Is.False);
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(original));
            }
            finally
            {
                var expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SecondDimensionStory134"));
                var resolved = Path.GetFullPath(directory);
                Assert.That(Path.GetDirectoryName(resolved), Is.EqualTo(expectedRoot).IgnoreCase);
                Assert.That(Guid.TryParseExact(Path.GetFileName(resolved), "N", out _), Is.True);
                if (Directory.Exists(resolved))
                {
                    Assert.That(File.GetAttributes(expectedRoot) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Assert.That(File.GetAttributes(resolved) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Directory.Delete(resolved, true);
                }
            }
        }

        private static bool StoryCompleted134(GuildCityPresentationState017D state, string contractId) =>
            (bool)typeof(M1FlowPresenter).GetMethod("IsStoryContractCompleted065",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { state, contractId });

        [TestCase("active_contract")]
        [TestCase("encounter")]
        [TestCase("return")]
        [TestCase("reward")]
        [TestCase("expedition")]
        [TestCase("live_battle")]
        public void ResumableWorkAlwaysWinsOverNextStoryShortcut(string boundary)
        {
            var state = CompletedOpening();
            state.HasActiveContract = boundary == "active_contract";
            state.HasPendingEncounter = boundary == "encounter";
            state.HasPendingBattleReturn = boundary == "return";
            state.HasUnclaimedBattleReward = boundary == "reward";
            state.Expedition = boundary == "expedition" ? new GuildCityExpeditionView017D() : null;
            Assert.That(M1FlowPresenter.ShouldOpenNextStoryInsteadOfQuickPlay093(state, boundary == "live_battle"), Is.False);
        }

        [Test]
        public void FreshOrFailedOpeningDoesNotPretendToBeCompleted()
        {
            Assert.That(M1FlowPresenter.ShouldOpenNextStoryInsteadOfQuickPlay093(null, false), Is.False);
            Assert.That(M1FlowPresenter.ShouldOpenNextStoryInsteadOfQuickPlay093(new GuildCityPresentationState017D(), false), Is.False);
            var state = CompletedOpening();
            state.OperationOrdinal = 5;
            state.ClaimedBattleRewardCount = 5;
            state.Contracts[0].IsFailed = true;
            Assert.That(M1FlowPresenter.ShouldOpenNextStoryInsteadOfQuickPlay093(state, false), Is.False);
        }

        [Test]
        public void RunnerSelectsOnlyActuallyAvailableVisibleCardsAndRotatesThroughThreeChoices()
        {
            var cards = new[]
            {
                new ExpeditionRouteCardView089 { CardId = "CHEST", CanChoose = true },
                new ExpeditionRouteCardView089 { CardId = "UNAFFORDABLE_MERCHANT", CanChoose = false },
                new ExpeditionRouteCardView089 { CardId = "BUFF", CanChoose = true },
                new ExpeditionRouteCardView089 { CardId = "EXTRA_BATTLE", CanChoose = true, RequiresCertifiedBattle = true }
            };
            CollectionAssert.AreEqual(new[] { "CHEST", "BUFF", "EXTRA_BATTLE", "CHEST" },
                Enumerable.Range(0, 4).Select(round => DeepCampaignVerification093.ChooseLegalCard093(cards, round).CardId));
            Assert.That(DeepCampaignVerification093.ChooseLegalCard093(cards.Where(card => !card.CanChoose).ToArray(), 0), Is.Null);
            Assert.That(DeepCampaignVerification093.ChooseLegalCard093(null, 0), Is.Null);
        }

        [Test]
        public void LockedStoryBattleCardRemainsTheOnlyChoiceWhenAuthoredRowRequiresIt()
        {
            var battle = new ExpeditionRouteCardView089
            { CardId = "LOCKED", CanChoose = true, LockedToBattle = true, RequiresCertifiedBattle = true };
            Assert.That(DeepCampaignVerification093.ChooseLegalCard093(new[] { battle }, 99), Is.SameAs(battle));
        }

        [Test]
        public void IsolatedRunnerNeverUsesSourceSaveOrItsDirectoryAsWritableDestination()
        {
            var directory = Path.Combine(Path.GetTempPath(), "SD093_" + Guid.NewGuid().ToString("N"));
            var source = Path.Combine(directory, "EarnedSave.json");
            Assert.Throws<ArgumentException>(() => DeepCampaignVerification093.IsolatedSavePath093(source, directory));
            Assert.Throws<ArgumentException>(() => DeepCampaignVerification093.IsolatedSavePath093(source, Path.GetPathRoot(directory)));
            Assert.Throws<ArgumentException>(() => DeepCampaignVerification093.IsolatedSavePath093("", directory));
            var evidence = directory + "_isolated";
            Assert.That(DeepCampaignVerification093.IsolatedSavePath093(source, evidence),
                Is.EqualTo(Path.Combine(evidence, "CampaignSave093.json")));
        }

        [Test]
        public void FirstFiftyChaptersHaveRealSequentialBlueprintsAndActualDeckBoards()
        {
            var operations = CampaignRegistry020.LoadFromResources();
            var boards = CampaignRegistry023.LoadFromResources();
            foreach (var number in Enumerable.Range(1, 50))
            {
                var id = "CH018_" + number.ToString("000");
                Assert.That(operations.Blueprints.ContainsKey(id), Is.True, id);
                Assert.That(boards.Boards.ContainsKey(id), Is.True, id);
                Assert.That(operations.Blueprints[id].steps.Any(step => step.kind == "WORLD_BOARD"), Is.True, id);
                Assert.That(boards.Boards[id].nodes.Length, Is.GreaterThanOrEqualTo(10), id);
            }
            // Catalog coverage only. The explicit coordinator runner, not this
            // structural test, supplies earned Chapter 1→50 progression evidence.
        }

        private static GuildCityPresentationState017D CompletedOpening() => new GuildCityPresentationState017D
        {
            IsAvailable = true,
            Contracts = new[] { new GuildCityContractView017D
                { ContractId = "CONTRACT_BELL_BENEATH_GATE", IsCompleted = true } }
        };
    }
}
