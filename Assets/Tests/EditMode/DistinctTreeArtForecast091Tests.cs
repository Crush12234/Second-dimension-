using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class DistinctTreeArtForecast091Tests
    {
        private const string HeroId = "PROC_TREE_RETENTION_091";
        private const string UnionId = "UNION_TREE_RETENTION_091";
        private M2CombatContent _content;
        private M2BattleCommandService _commands;

        [OneTimeSetUp]
        public void LoadAuthority()
        {
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _commands = new M2BattleCommandService();
        }

        [TestCase("TREE_CA002_WPN_SWORD", "Ready Cut", "Cross Slash")]
        [TestCase("TREE_CA002_WPN_SHIELD", "Brace", "Shield Bash")]
        [TestCase("TREE_CA002_MYS_FLAME", "Ember", "Firebolt")]
        [TestCase("TREE_CA002_MYS_RESTORATION", "Mend", "Cleanse")]
        [TestCase("TREE_CA002_ROLE_SABOTEUR", "Distract", "Disarm")]
        public void MeaningfulUnionCommandLearnsDistinctNextArtWithoutReplacingEarlierActions(
            string treeId, string oldName, string newName)
        {
            var oldId = treeId + "_N01";
            var nextId = treeId + "_N02";
            var before = Start(Campaign(treeId, new[] { oldId }, 91001,
                firstUsePending: true, discoveryReady: true));
            var union = before.Battle.PlayerUnions.Single();
            var actor = union.Members.Single();
            Assert.That(_content.Art(oldId).Name, Is.EqualTo(oldName));
            Assert.That(_content.Art(nextId).Name, Is.EqualTo(newName));
            Assert.That(actor.LearnedArtIds, Does.Contain(oldId));
            Assert.That(actor.LearnedArtIds, Does.Not.Contain(nextId));
            var forecast = before.Battle.CommittedForecasts.Single(value =>
                value.CommandId == CommandFor(_content.Art(oldId).Discipline));
            var action = forecast.MemberActions.Single();
            Assert.That(action.ArtId, Is.EqualTo(oldId), treeId);
            Assert.That(action.BreakthroughOpportunity, Is.True, treeId);
            Assert.That(action.BreakthroughTargetArtId, Is.EqualTo(nextId), treeId);

            var selected = Require(_commands.SelectForecast(before, union.UnionId, forecast.ForecastId));
            var resolved = Require(_commands.ConfirmRound(selected, _content));
            var learned = resolved.Battle.PlayerUnions.Single().Members.Single();
            Assert.That(actor.LearnedArtIds.All(learned.LearnedArtIds.Contains), Is.True,
                "Learning a new action must retain every earlier learned Art ID.");
            Assert.That(learned.LearnedArtIds, Does.Contain(nextId));
            Assert.That(learned.ArtProgress.Any(value => value.ArtId == oldId &&
                value.MeaningfulUses > 0), Is.True, "The old Art must actually execute meaningfully.");
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value =>
                value.EventType == "ART_GROWTH" && value.ArtId == oldId), Is.True);
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value =>
                value.EventType == "BREAKTHROUGH" && value.ArtId == nextId &&
                value.Text.Contains(newName)), Is.True);
            var nextForecast = resolved.Battle.CommittedForecasts.Single(value =>
                value.CommandId == CommandFor(_content.Art(oldId).Discipline));
            var legalIds = CandidateIds(nextForecast);
            Assert.That(legalIds, Does.Contain(oldId));
            Assert.That(legalIds, Does.Contain(nextId));
        }

        [Test]
        public void AllThirtyUnlockedTreesRetainEveryDistinctActiveArtAndExcludePassiveNodesFromCommands()
        {
            var treeIds = _content.DeepProgression.RuntimeArts.Select(value => value.TreeId)
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Assert.That(treeIds, Has.Length.EqualTo(30));
            foreach (var treeId in treeIds)
            {
                var nodes = _content.DeepProgression.Tree(treeId).NodeIds;
                var active = nodes.Where(value => _content.Art(value).IsForecastAction).ToArray();
                var passives = nodes.Except(active).ToArray();
                Assert.That(active, Has.Length.EqualTo(9), treeId);
                Assert.That(active.Select(value => _content.Art(value).Name)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(active.Length), treeId);
                var campaign = Start(Campaign(treeId, nodes, 91030,
                    firstUsePending: false, discoveryReady: false));
                var member = campaign.Battle.PlayerUnions.Single().Members.Single();
                Assert.That(active.All(member.LearnedArtIds.Contains), Is.True, treeId);
                var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                    value.CommandId == CommandFor(_content.Art(active[0]).Discipline));
                var candidateIds = CandidateIds(forecast);
                foreach (var id in active)
                {
                    Assert.That(candidateIds, Does.Contain(id), treeId + " retains " + id);
                    Assert.That(_content.Art(id).PlayerDirectlySelectableInStandard, Is.False,
                        "Actions remain chosen by Union Forecast, never individual Art buttons.");
                }
                foreach (var id in passives)
                    Assert.That(candidateIds, Does.Not.Contain(id), treeId + " passive " + id);
            }
        }

        [TestCase("TREE_CA002_WPN_SWORD")]
        [TestCase("TREE_CA002_MYS_FLAME")]
        [TestCase("TREE_CA002_MYS_RESTORATION")]
        [TestCase("TREE_CA002_ROLE_SABOTEUR")]
        public void OlderAndNewerPreviouslyUsedArtsBothAppearNaturallyInLegalForecastContexts(string treeId)
        {
            var oldId = treeId + "_N01";
            var nextId = treeId + "_N02";
            var observed = new HashSet<string>(StringComparer.Ordinal);
            var restoration = _content.Art(oldId).Discipline == "Restoration";
            for (var index = 0; index < 48 && observed.Count < 2; index++)
            {
                // Both actions have a recorded use: the new-Art first-use priority
                // is exhausted. Heals respond to wound severity, not a forced RNG
                // lottery; other Arts use the existing deterministic weighted pool.
                var campaign = Start(Campaign(treeId, new[] { oldId, nextId }, 91100 + index,
                    firstUsePending: false, discoveryReady: false,
                    currentHp: restoration && index % 2 == 0 ? 399 : 100,
                    cohesionBasisPoints: restoration ? 10000 : 5000));
                var member = campaign.Battle.PlayerUnions.Single().Members.Single();
                Assert.That(member.ArtProgress.Where(value => value.ArtId == oldId ||
                    value.ArtId == nextId).All(value => value.MeaningfulUses > 0), Is.True);
                var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                    value.CommandId == CommandFor(_content.Art(oldId).Discipline));
                foreach (var action in forecast.MemberActions)
                    if (action.ArtId == oldId || action.ArtId == nextId)
                        observed.Add(action.ArtId);
            }
            Assert.That(observed, Is.EquivalentTo(new[] { oldId, nextId }),
                treeId + " must be able to choose both retained actions through Union commands.");
        }

        private static string[] CandidateIds(BattleForecastState forecast) =>
            JObject.Parse(forecast.DeterministicDebugEvidence)
                .SelectTokens("LegalActionPool[*].CandidateArts[*].ArtId")
                .Select(value => value.Value<string>()).ToArray();

        private CampaignState Start(CampaignState campaign) =>
            Require(_commands.StartEncounterBattle(campaign, _content,
                "BATTLE_DISTINCT_TREE_091", "Verify distinct learned Union Arts.", 1));

        private CampaignState Campaign(string treeId, IReadOnlyList<string> learnedIds,
            long seed, bool firstUsePending, bool discoveryReady, int currentHp = 100,
            int cohesionBasisPoints = 5000)
        {
            var allNodes = _content.DeepProgression.Tree(treeId).NodeIds;
            var tags = allNodes.SelectMany(value => _content.Art(value).RequiredEquipmentTags)
                .Concat(new[] { "STAFF", "WEAPON" }).Distinct(StringComparer.Ordinal).ToArray();
            var equipment = new EquipmentItemState("TREE_TEST_WEAPON_091", "TREE_TEST_GEAR_091",
                "Tree authority fixture", new[] { EquipmentSlotIds.MainHand }, tags,
                "QUALITY_STANDARD", 10000, false);
            var discoveryPoints = discoveryReady
                ? _content.DeepProgression.Node(treeId + "_N02").DiscoveryMeaningfulUsePoints - 1 : 1;
            var mastery = learnedIds.Select(value => new RecruitArtMasteryState(value,
                _content.Art(value).Discipline, firstUsePending ? 0 : 1, discoveryPoints)).ToArray();
            const int level = 40;
            var progression = new RecruitProgressionState(level,
                RecruitProgressionRules021.TotalXpRequiredForLevel(level),
                0, 0, 0, 0, 0, 0, 0, learnedIds, mastery, new[] { treeId });
            var recruit = new RecruitState(HeroId, currentHp, 400, 100, 100,
                "Tree Keeper", RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01",
                "CLASS_TEND_TEST", "Observed", 5000, RecruitAuthorityKind.Normal,
                "{\"visualSeed\":\"TREE_TEST_091\",\"startingClassId\":\"CLASS_TEND_TEST\"}", string.Empty,
                new EquipmentLoadoutState(new[]
                { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, equipment) }),
                true, string.Empty, string.Empty, 60, 60, progression);
            var union = new UnionState(UnionId, "Tree Keeper Union", UnionKind.Normal,
                HeroId, new[] { HeroId }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED",
                99, cohesionBasisPoints);
            var guild = new GuildState("GUILD_TREE_RETENTION_091", 0, new[] { recruit }, new[] { union });
            var profile = new NewGuildProfileState("Tree Tester", GameMode.Standard,
                TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false);
            var opening = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001",
                true, null, false, 439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState("00000000-0000-0000-0000-000000000391", seed, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, opening);
        }

        private static string CommandFor(string discipline)
        {
            switch (discipline)
            {
                case "Guard": return "CMD_GUARD";
                case "Mystic": return "CMD_MYSTIC";
                case "Restoration": return "CMD_HEAL";
                case "Support": return "CMD_SUPPORT";
                default: return "CMD_ALL_OUT";
            }
        }

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
