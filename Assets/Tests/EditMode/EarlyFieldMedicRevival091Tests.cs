using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EarlyFieldMedicRevival091Tests
    {
        private const string Tree = "TREE_CA002_ROLE_FIELD_MEDIC";
        private const string Root = Tree + "_N01";
        private const string Training = Tree + "_N02";
        private const string Revive = "ART_STAND_AGAIN";
        private const string Medic = "PROC_EARLY_MEDIC_091";
        private M2CombatContent _content;
        private readonly M2BattleCommandService _commands = new M2BattleCommandService();

        [OneTimeSetUp]
        public void Load() => _content = M2CombatContent.LoadFromDirectory(
            Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));

        [TestCase(false)]
        [TestCase(true)]
        public void NewOrPreviouslyLearnedBandageTrainingEarnsRealStandAgainOnceAfterMeaningfulHeal(bool alreadyTrained)
        {
            var campaign = Start(Create(alreadyTrained));
            Assert.That(Member(campaign).LearnedArtIds, Does.Not.Contain(Revive),
                "Loading or starting a battle must not silently rewrite learned Arts or a committed Forecast.");
            var oldIds = Member(campaign).LearnedArtIds.ToArray();
            var heal = campaign.Battle.CommittedForecasts.Single(value => value.UnionId == "MEDIC_UNION" && value.CommandId == "CMD_HEAL");
            Assert.That(heal.MemberActions.Single().ArtId, Is.EqualTo(alreadyTrained ? Training : Root));
            campaign = Resolve(campaign, "CMD_HEAL");
            var learned = Member(campaign);
            Assert.That(learned.LearnedArtIds, Does.Contain(Training));
            Assert.That(learned.LearnedArtIds, Does.Contain(Revive));
            Assert.That(oldIds.All(learned.LearnedArtIds.Contains), Is.True);
            Assert.That(learned.LearnedArtIds, Does.Not.Contain(Tree + "_N08"), "No late node is unlocked.");
            Assert.That(campaign.Battle.RoundRecords.Last().Events.Count(item => item.EventType == "BREAKTHROUGH" && item.ArtId == Revive), Is.EqualTo(1));
            Assert.That(campaign.Battle.RoundRecords.Last().Events.Single(item => item.EventType == "BREAKTHROUGH" && item.ArtId == Revive).Text,
                Does.Contain("6 AP / 12 MP"));
            Assert.That(_content.Art(Revive).SharedApCost, Is.EqualTo(6));
            Assert.That(_content.Art(Revive).PersonalMpCost, Is.EqualTo(12));
            Assert.That(M2DeepArtRuntime070.CanEarnFieldMedicRevival091(
                campaign.Guild.Recruits.Single(value => value.RecruitId == Medic), learned, Training, _content), Is.False,
                "Already earned Art is idempotent.");

            // Down an allied member, then let an ordinary guard round regenerate
            // the next planning Forecasts. No individual Art is injected or chosen.
            campaign = campaign.WithBattle(campaign.Battle.With(playerUnions: campaign.Battle.PlayerUnions.Select(union =>
                union.UnionId == "ALLY_UNION" ? union.With(members: union.Members.Select(member => member.With(currentHp: 0)).ToArray()) : union).ToArray()));
            campaign = Resolve(campaign, "CMD_GUARD");
            var rescue = campaign.Battle.CommittedForecasts.Single(value => value.UnionId == "MEDIC_UNION" && value.CommandId == "CMD_HEAL");
            Assert.That(rescue.MemberActions.Single().ArtId, Is.EqualTo(Revive));
            campaign = Resolve(campaign, "CMD_HEAL");
            Assert.That(campaign.Battle.RoundRecords.Last().Events.Any(item => item.EventType == "REVIVED" && item.ArtId == Revive), Is.True);
            Assert.That(campaign.Battle.EventLog.Count(item => item.EventType == "BREAKTHROUGH" && item.ArtId == Revive), Is.EqualTo(1));
        }

        [TestCase("N01_ONLY")]
        [TestCase("LEVEL_ONE")]
        [TestCase("WRONG_EQUIPMENT")]
        [TestCase("LOCKED_ROLE")]
        [TestCase("NO_EARNED_MASTERY")]
        public void EarlyTrainingCannotBypassLearnedNodesLevelEquipmentActiveRoleOrMeaningfulMastery(string rejection)
        {
            var campaign = Create(true, rejection);
            var recruit = campaign.Guild.Recruits.Single(value => value.RecruitId == Medic);
            campaign = Start(campaign);
            var member = Member(campaign);
            Assert.That(M2DeepArtRuntime070.CanEarnFieldMedicRevival091(recruit, member, Root, _content), Is.False, rejection);
            Assert.That(member.LearnedArtIds, Does.Not.Contain(Revive));
        }

        [Test]
        public void EarnedRevivalSurvivesAtomicSaveRewardClaimAndNextBattleWithoutRepeatingTraining()
        {
            var campaign = Resolve(Start(Create(false)), "CMD_HEAL");
            Assert.That(Member(campaign).LearnedArtIds, Does.Contain(Revive));
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionMedic091", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "medic.json");
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));
                var loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                campaign = loaded.Value.CampaignState;
                Assert.That(Member(campaign).LearnedArtIds, Does.Contain(Revive));
                for (var round = 0; round < 40 && campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
                    campaign = Resolve(campaign, "CMD_ALL_OUT");
                Assert.That(campaign.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
                campaign = Require(_commands.ClaimBattleRewards(campaign));
                var persistent = campaign.Guild.Recruits.Single(value => value.RecruitId == Medic).Progression;
                Assert.That(persistent.LearnedArtIds, Does.Contain(Root));
                Assert.That(persistent.LearnedArtIds, Does.Contain(Training));
                Assert.That(persistent.LearnedArtIds, Does.Contain(Revive));
                Assert.That(persistent.ArtMastery.Count(value => value.ArtId == Revive), Is.EqualTo(1));
                store.Write(path, SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));
                loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True);
                campaign = Require(_commands.StartEncounterBattle(loaded.Value.CampaignState, _content,
                    "BATTLE_MEDIC_NEXT_091", "Keep earned revival.", 1));
                Assert.That(Member(campaign).LearnedArtIds, Does.Contain(Revive));
                Assert.That(campaign.Battle.EventLog.Any(item => item.EventType == "BREAKTHROUGH" && item.ArtId == Revive), Is.False);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private CampaignState Resolve(CampaignState campaign, string medicCommand)
        {
            foreach (var union in campaign.Battle.PlayerUnions.Where(value => !value.Retreated && !value.IsDefeated).ToArray())
            {
                var options = campaign.Battle.CommittedForecasts.Where(value => value.UnionId == union.UnionId).ToArray();
                var command = union.UnionId == "MEDIC_UNION" ? medicCommand : "CMD_GUARD";
                if (medicCommand == "CMD_ALL_OUT") command = medicCommand;
                var choice = options.FirstOrDefault(value => value.CommandId == command) ?? options.First();
                campaign = Require(_commands.SelectForecast(campaign, union.UnionId, choice.ForecastId));
            }
            return Require(_commands.ConfirmRound(campaign, _content));
        }

        private CampaignState Start(CampaignState campaign) => Require(_commands.StartEncounterBattle(campaign,
            _content, "BATTLE_EARLY_MEDIC_091", "Earn rescue through Field Medic training.", 1));
        private static BattleMemberState Member(CampaignState campaign) => campaign.Battle.PlayerUnions
            .SelectMany(value => value.Members).Single(value => value.MemberId == Medic);
        private static CampaignState Require(Result<CampaignState> result)
        { Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors)); return result.Value; }

        private CampaignState Create(bool trained, string rejection = "")
        {
            var learned = trained && rejection != "N01_ONLY" ? new[] { Root, Training } : new[] { Root };
            var points = rejection == "NO_EARNED_MASTERY" ? 0 : trained ? 12 : 11;
            var level = rejection == "LEVEL_ONE" ? 1 : 2;
            var progression = new RecruitProgressionState(level, RecruitProgressionRules021.TotalXpRequiredForLevel(level),
                0, 0, 0, 0, 0, 0, 0, learned,
                new[] { new RecruitArtMasteryState(Root, "Restoration", points > 0 ? 1 : 0, points) },
                rejection == "LOCKED_ROLE" ? new[] { "TREE_CA002_ROLE_GUARDIAN" } : new[] { Tree });
            var gear = new EquipmentItemState("MEDIC_GEAR", "MEDIC_GEAR", "Medic staff", new[] { EquipmentSlotIds.MainHand },
                rejection == "WRONG_EQUIPMENT" ? new[] { "SWORD", "WEAPON" } : new[] { "STAFF", "HEALING" }, "QUALITY_STANDARD", 10000, false);
            var medic = new RecruitState(Medic, 100, 800, 100, 100, "Field Medic", RecruitOriginKind.Procedural,
                string.Empty, "HUMAN", "WORLD_GATE_01", "CLASS_TEND_TEST", "Observed", 6000, RecruitAuthorityKind.Normal,
                "{\"visualSeed\":\"EARLY_MEDIC_091\",\"startingClassId\":\"CLASS_TEND_TEST\"}", string.Empty,
                new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, gear) }),
                true, string.Empty, string.Empty, 60, 60, progression);
            var allyGear = new EquipmentItemState("ALLY_GEAR", "ALLY_GEAR", "Warrior sword", new[] { EquipmentSlotIds.MainHand },
                new[] { "SWORD", "WEAPON" }, "QUALITY_STANDARD", 10000, false);
            var ally = new RecruitState("MEDIC_ALLY", 800, 800, 100, 100, "Ally", RecruitOriginKind.Procedural,
                string.Empty, "HUMAN", "WORLD_GATE_01", "CLASS_TEND_WARRIOR", "Observed", 6000, RecruitAuthorityKind.Normal,
                string.Empty, string.Empty, new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, allyGear) }),
                true, string.Empty, string.Empty, 60, 60);
            var unions = new[] {
                new UnionState("MEDIC_UNION", "Medics", UnionKind.Normal, Medic, new[] { Medic }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 99, 8500),
                new UnionState("ALLY_UNION", "Allies", UnionKind.Normal, ally.RecruitId, new[] { ally.RecruitId }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 99, 8500) };
            return new CampaignState("00000000-0000-0000-0000-000000000591", 20260907L, "1.0", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("MEDIC_GUILD", 0, new[] { medic, ally }, unions),
                new NewGuildProfileState("Medic Tester", GameMode.Standard, TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false, 439, 0, true, true, true, false, "autosave_unions"));
        }
    }
}
