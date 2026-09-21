using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class IndependentArtMastery091Tests
    {
        private const string OldArt = "ART_BASIC_SABER_CUT";
        private const string NewArt = "ART_POWER_CUT";
        private const string Actor = "RECRUIT_1";
        private M2CombatContent _content;
        private M2BattleCommandService _commands;

        [SetUp]
        public void SetUp091()
        {
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _commands = new M2BattleCommandService();
        }

        [TestCase(OldArt, NewArt)]
        [TestCase(NewArt, OldArt)]
        public void EachDistinctArtCanReachEveryLevelWithoutLevelingOrRemovingItsSibling091(
            string practiced, string sibling)
        {
            IReadOnlyList<BattleArtProgressState> progress = new[]
            {
                new BattleArtProgressState(practiced, _content.Art(practiced).Discipline, 0, 0),
                new BattleArtProgressState(sibling, _content.Art(sibling).Discipline, 0, 0)
            };
            var original = CanonicalJson.Serialize(progress);
            var initial = progress;
            for (var level = 1; level <= 10; level++)
            {
                if (level > 1)
                {
                    var current = progress.Single(value => value.ArtId == practiced);
                    progress = AddMeaningfulUse091(progress, practiced,
                        M2ArtMasteryLevelPolicy088.ThresholdForLevel(level) - current.MasteryPoints);
                }
                var trained = progress.Single(value => value.ArtId == practiced);
                var untouched = progress.Single(value => value.ArtId == sibling);
                Assert.That(M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(trained.MasteryPoints), Is.EqualTo(level));
                Assert.That(trained.MeaningfulUses, Is.EqualTo(level - 1));
                Assert.That(untouched.MasteryPoints, Is.Zero);
                Assert.That(untouched.MeaningfulUses, Is.Zero);
                Assert.That(M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(untouched.MasteryPoints), Is.EqualTo(1));
                var saved = RecruitProgressionState.Default().WithArts(new[] { OldArt, NewArt },
                    progress.Select(value => new RecruitArtMasteryState(
                        value.ArtId, value.Discipline, value.MeaningfulUses, value.MasteryPoints)).ToArray());
                Assert.That(saved.LearnedArtIds, Is.EquivalentTo(new[] { OldArt, NewArt }));
            }
            progress = AddMeaningfulUse091(progress, practiced, 100000);
            Assert.That(M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(
                progress.Single(value => value.ArtId == practiced).MasteryPoints), Is.EqualTo(10));
            Assert.That(progress.Single(value => value.ArtId == sibling).MasteryPoints, Is.Zero);
            Assert.That(CanonicalJson.Serialize(initial), Is.EqualTo(original), "Immutable input must not be rewritten.");
        }

        [TestCase(OldArt, NewArt)]
        [TestCase(NewArt, OldArt)]
        public void LiveBattlePowerUsesOnlyTheExactArtMasteryNotItsWeaponFamily091(string trained, string sibling)
        {
            var campaign = CreateCampaign091();
            var equipmentBefore = CanonicalJson.Serialize(campaign.Guild.Recruits.Single(value => value.RecruitId == Actor).Equipment);
            campaign = WithMastery091(campaign, trained, 4700, sibling, 0);
            var started = Require091(_commands.StartEncounterBattle(campaign, _content,
                "BATTLE_INDEPENDENT_ART_POWER_091", "Verify independently mastered Arts.", 1));
            var actor = Member091(started);
            Assert.That(actor.LearnedArtIds, Does.Contain(OldArt));
            Assert.That(actor.LearnedArtIds, Does.Contain(NewArt));
            var trainedPower = M2BattleCommandService.EffectiveArtPowerPermille088(started.Battle, _content, actor, trained);
            var siblingPower = M2BattleCommandService.EffectiveArtPowerPermille088(started.Battle, _content, actor, sibling);
            Assert.That(trainedPower, Is.EqualTo(1540));
            Assert.That(siblingPower, Is.EqualTo(1000));
            Assert.That(M2ArtMasteryLevelPolicy088.ScaleMagnitudeByPowerPermille(100, trainedPower), Is.EqualTo(154));
            Assert.That(M2ArtMasteryLevelPolicy088.ScaleMagnitudeByPowerPermille(100, siblingPower), Is.EqualTo(100));
            Assert.That(CanonicalJson.Serialize(started.Guild.Recruits.Single(value => value.RecruitId == Actor).Equipment),
                Is.EqualTo(equipmentBefore), "Art levels cannot be implemented by leveling or replacing the weapon.");
        }

        [Test]
        public void RealCombatMeaningfulUseSurvivesAtomicReloadClaimAndNextBattlePerArt091()
        {
            var campaign = WithMastery091(CreateCampaign091(), OldArt, 99, NewArt, 260);
            campaign = Require091(_commands.StartTutorialBattle(campaign, _content));
            var initial = Member091(campaign).ArtProgress.ToDictionary(value => value.ArtId);
            campaign = ResolveRound091(campaign);
            Assert.That(campaign.Battle.RoundRecords.SelectMany(value => value.Events)
                .Any(value => value.ActorMemberId == Actor && value.EventType == "ART_GROWTH"), Is.True,
                "This fixture must exercise real meaningful combat use, not only seeded progression.");

            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionIndependentArt091", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "art-mastery.json");
            try
            {
                var store = new AtomicSaveStore();
                Save091(store, path, campaign);
                var loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(CanonicalJson.Serialize(Member091(loaded.Value.CampaignState).ArtProgress),
                    Is.EqualTo(CanonicalJson.Serialize(Member091(campaign).ArtProgress)));
                campaign = loaded.Value.CampaignState;
                for (var round = 0; round < 20 && campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
                    campaign = ResolveRound091(campaign);
                Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));

                var growth = campaign.Battle.RoundRecords.SelectMany(value => value.Events)
                    .Where(value => value.ActorMemberId == Actor && value.EventType == "ART_GROWTH").ToArray();
                foreach (var id in new[] { OldArt, NewArt })
                {
                    var current = Member091(campaign).ArtProgress.Single(value => value.ArtId == id);
                    Assert.That(current.MasteryPoints, Is.EqualTo(initial[id].MasteryPoints +
                        growth.Where(value => value.ArtId == id).Sum(value => value.Amount)));
                    Assert.That(current.MeaningfulUses, Is.EqualTo(initial[id].MeaningfulUses +
                        growth.Count(value => value.ArtId == id)), "A sibling's meaningful use must not increment this Art.");
                }

                var claimed = Require091(_commands.ClaimBattleRewards(campaign));
                var recruit = claimed.Guild.Recruits.Single(value => value.RecruitId == Actor);
                foreach (var id in new[] { OldArt, NewArt })
                {
                    var live = Member091(campaign).ArtProgress.Single(value => value.ArtId == id);
                    var persistent = recruit.Progression.ArtMastery.Single(value => value.ArtId == id);
                    Assert.That(persistent.MasteryPoints, Is.EqualTo(live.MasteryPoints));
                    Assert.That(persistent.MeaningfulUses, Is.EqualTo(live.MeaningfulUses));
                    Assert.That(recruit.Progression.LearnedArtIds, Does.Contain(id));
                }
                Save091(store, path, claimed);
                var reloaded = store.ReadWithRecovery(path);
                Assert.That(reloaded.IsSuccess, Is.True, string.Join("\n", reloaded.Errors));
                var restarted = Require091(_commands.RestartTutorialBattle(reloaded.Value.CampaignState, _content));
                foreach (var id in new[] { OldArt, NewArt })
                {
                    var saved = recruit.Progression.ArtMastery.Single(value => value.ArtId == id);
                    var next = Member091(restarted).ArtProgress.Single(value => value.ArtId == id);
                    Assert.That(next.MasteryPoints, Is.EqualTo(saved.MasteryPoints));
                    Assert.That(next.MeaningfulUses, Is.EqualTo(saved.MeaningfulUses));
                    Assert.That(Member091(restarted).LearnedArtIds, Does.Contain(id));
                }
            }
            finally
            {
                foreach (var ownedFile in new[] { path, path + ".bak", path + ".tmp" })
                    if (File.Exists(ownedFile)) File.Delete(ownedFile);
                if (Directory.Exists(directory)) Directory.Delete(directory);
            }
        }

        [Test]
        public void AnimationIntensityResolvesDistinctArtAndActorLevelsInsteadOfSharedWeaponLevel091()
        {
            var view = new M2BattleView { Forecasts = new[] { new M2ForecastView { MemberActions = new[]
            {
                new M2PredictedActionView { ActorMemberId = Actor, ArtId = OldArt, ArtLevel = 9 },
                new M2PredictedActionView { ActorMemberId = Actor, ArtId = NewArt, ArtLevel = 2 },
                new M2PredictedActionView { ActorMemberId = "OTHER_ACTOR", ArtId = NewArt, ArtLevel = 10 }
            } } } };
            var oldLevel = M2ArtLevelPresentation089.ResolveCommittedLevel(view, Actor, OldArt);
            var newLevel = M2ArtLevelPresentation089.ResolveCommittedLevel(view, Actor, NewArt);
            Assert.That(oldLevel, Is.EqualTo(9));
            Assert.That(newLevel, Is.EqualTo(2));
            Assert.That(M2ArtLevelPresentation089.EffectScale(oldLevel), Is.GreaterThan(M2ArtLevelPresentation089.EffectScale(newLevel)));
            Assert.That(M2ArtLevelPresentation089.MotionScale(oldLevel), Is.GreaterThan(M2ArtLevelPresentation089.MotionScale(newLevel)));
            Assert.That(M2ArtLevelPresentation089.AccentPulseCount(oldLevel), Is.GreaterThan(M2ArtLevelPresentation089.AccentPulseCount(newLevel)));
        }

        private IReadOnlyList<BattleArtProgressState> AddMeaningfulUse091(
            IReadOnlyList<BattleArtProgressState> source, string artId, int gain) =>
            (IReadOnlyList<BattleArtProgressState>)typeof(M2BattleCommandService)
                .GetMethod("AddMeaningfulArtUse", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { source, artId, _content.Art(artId).Discipline, gain });

        private CampaignState ResolveRound091(CampaignState campaign)
        {
            foreach (var union in campaign.Battle.PlayerUnions.Where(value => !value.Retreated && !value.IsDefeated).ToArray())
            {
                var options = campaign.Battle.CommittedForecasts.Where(value => value.UnionId == union.UnionId).ToArray();
                var forecast = options.FirstOrDefault(value => value.CommandId == "CMD_ALL_OUT") ??
                    options.FirstOrDefault(value => value.CommandId == "CMD_BALANCED") ?? options.First();
                campaign = Require091(_commands.SelectForecast(campaign, union.UnionId, forecast.ForecastId));
            }
            return Require091(_commands.ConfirmRound(campaign, _content));
        }

        private CampaignState WithMastery091(CampaignState campaign, string first, int firstPoints, string second, int secondPoints)
        {
            var recruits = campaign.Guild.Recruits.ToList();
            var index = recruits.FindIndex(value => value.RecruitId == Actor);
            var recruit = recruits[index];
            recruits[index] = recruit.WithProgression(recruit.Progression.WithArts(new[] { first, second }, new[]
            {
                new RecruitArtMasteryState(first, _content.Art(first).Discipline, 2, firstPoints),
                new RecruitArtMasteryState(second, _content.Art(second).Discipline, 3, secondPoints)
            }));
            return campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp, recruits.AsReadOnly(),
                campaign.Guild.Unions, campaign.Guild.Inventory), campaign.OpeningFlow);
        }

        private static void Save091(AtomicSaveStore store, string path, CampaignState campaign) =>
            store.Write(path, SaveEnvelopeV1.Create(campaign, new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc)));

        private static BattleMemberState Member091(CampaignState campaign) => campaign.Battle.PlayerUnions
            .SelectMany(value => value.Members).Single(value => value.MemberId == Actor);

        private static CampaignState Require091(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static CampaignState CreateCampaign091()
        {
            var classes = new[] { "GUARDIAN", "WARRIOR", "RANGER", "PRIEST", "MAGE", "ROGUE" };
            var tags = new[] { new[] { "SHIELD", "SWORD", "WEAPON" }, new[] { "SWORD", "WEAPON" },
                new[] { "BOW", "WEAPON" }, new[] { "STAFF", "HEALING" }, new[] { "WAND", "FOCUS_TOOL" }, new[] { "DAGGER", "WEAPON" } };
            var recruits = new List<RecruitState>();
            for (var index = 0; index < 6; index++)
            {
                var weapon = new EquipmentItemState("ITEM_WEAPON_" + index, "EQ_WEAPON_" + index, "Starter Weapon " + index,
                    new[] { EquipmentSlotIds.MainHand }, tags[index], "QUALITY_STANDARD", 10000, false);
                var armor = new EquipmentItemState("ITEM_BODY_" + index, "EQ_BODY_" + index, "Starter Armor " + index,
                    new[] { EquipmentSlotIds.BodyArmor }, new[] { "ARMOR" }, "QUALITY_STANDARD", 10000, false);
                recruits.Add(new RecruitState("RECRUIT_" + index, 105 + index * 5, 105 + index * 5, 20 + index * 2, 20 + index * 2,
                    "Recruit " + index, RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01", "CLASS_TEND_" + classes[index],
                    "Observed", 5200 + index * 200, RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                    new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, weapon),
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.BodyArmor, armor) }), true, string.Empty, string.Empty, 48 + index, 45 + index));
            }
            var unions = Enumerable.Range(0, 2).Select(index => new UnionState("UNION_OPENING_0" + (index + 1),
                "Opening Union " + (index + 1), UnionKind.Normal, recruits[index * 3].RecruitId,
                recruits.Skip(index * 3).Take(3).Select(value => value.RecruitId).ToArray(),
                "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 18, 8500)).ToArray();
            var guild = new GuildState("GUILD_INDEPENDENT_ART_091", 0, recruits.AsReadOnly(), unions);
            var profile = new NewGuildProfileState("Art Mastery Test", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState("00000000-0000-0000-0000-000000000291", 20260814L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }
    }
}
