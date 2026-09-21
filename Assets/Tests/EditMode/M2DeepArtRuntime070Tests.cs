using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2DeepArtRuntime070Tests
    {
        private M2CombatContent _content;
        private M2BattleCommandService _battle;

        [SetUp]
        public void SetUp()
        {
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _battle = new M2BattleCommandService();
        }

        [Test]
        public void LoadedCombatAuthorityIncludesAllThirtyTreesAndNeverAddsArtButtons()
        {
            Assert.That(_content.DeepProgression, Is.Not.Null);
            Assert.That(_content.DeepProgression.RuntimeArtCount, Is.EqualTo(360));
            Assert.That(_content.Arts.Keys.Count(value => value.StartsWith("TREE_CA002_", StringComparison.Ordinal)),
                Is.EqualTo(360));
            Assert.That(_content.Arts.Values.All(value => !value.PlayerDirectlySelectableInStandard), Is.True);
            Assert.That(_content.Art("TREE_CA002_WPN_SPEAR_POLEARM_N01").PowerCoefficientPermille,
                Is.GreaterThan(1000));
            Assert.That(_content.Art("TREE_CA002_WPN_SWORD_N05").IsForecastAction, Is.False,
                "Passive nodes may be learned and shown in the tree, but cannot masquerade as active battle actions.");
        }

        [Test]
        public void SignatureRecruitEntersBattleWithTwoActiveTreesAndLockedTreesStayDormant()
        {
            var started = Require(_battle.StartEncounterBattle(
                CreateMarenCampaign(levelTwoAndNearFirstDiscovery: false),
                _content,
                "BATTLE_DEEP_ART_ENTRY_070",
                "Prove deep Art entry."));
            var member = started.Battle.PlayerUnions.Single().Members.Single();

            Assert.That(member.LearnedArtIds, Does.Contain("TREE_CA002_WPN_SPEAR_POLEARM_N01"));
            Assert.That(member.LearnedArtIds, Does.Contain("TREE_CA002_ROLE_GUARDIAN_N01"));
            Assert.That(member.LearnedArtIds, Does.Not.Contain("TREE_CA002_MYS_WARDING_N01"));
            Assert.That(member.LearnedArtIds, Does.Not.Contain("TREE_CA002_ROLE_COMMANDER_N01"));
            Assert.That(started.Battle.CommittedForecasts.SelectMany(value => value.MemberActions)
                .Any(value => value.ArtId == "TREE_CA002_WPN_SPEAR_POLEARM_N01"), Is.True);
        }

        [Test]
        public void MeaningfulUnionForecastShowsBarLearnsNextArtAndOffersItNextRound()
        {
            var campaign = Require(_battle.StartEncounterBattle(
                CreateMarenCampaign(levelTwoAndNearFirstDiscovery: true),
                _content,
                "BATTLE_DEEP_ART_LEARNING_070",
                "Prove visible authority-backed Art learning."));
            var union = campaign.Battle.PlayerUnions.Single();
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == union.UnionId && value.CommandId == "CMD_ALL_OUT");
            var action = forecast.MemberActions.Single();

            Assert.That(action.ArtId, Is.EqualTo("TREE_CA002_WPN_SPEAR_POLEARM_N01"));
            Assert.That(action.BreakthroughOpportunity, Is.True);
            Assert.That(action.BreakthroughTargetArtId, Is.EqualTo("TREE_CA002_WPN_SPEAR_POLEARM_N02"));
            Assert.That(forecast.LearningOpportunity, Does.Contain("Brace"));
            Assert.That(forecast.LearningOpportunity, Does.Contain("12/12 discovery"));
            Assert.That(forecast.LearningOpportunity, Does.Contain("learning bar is ready"));
            Assert.That(forecast.LearningOpportunity, Does.Not.Contain("Discovery 95→100"),
                "Deep-tree learning must show its authored threshold rather than the legacy tutorial counter.");

            campaign = Require(_battle.SelectForecast(campaign, union.UnionId, forecast.ForecastId));
            campaign = Require(_battle.ConfirmRound(campaign, _content));

            var learned = campaign.Battle.PlayerUnions.Single().Members.Single();
            Assert.That(learned.LearnedArtIds, Does.Contain("TREE_CA002_WPN_SPEAR_POLEARM_N02"));
            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "BREAKTHROUGH" &&
                value.ArtId == "TREE_CA002_WPN_SPEAR_POLEARM_N02" &&
                value.Text.Contains("Brace")), Is.True);
            Assert.That(campaign.Battle.TutorialBreakthroughOccurred, Is.False,
                "Normal tree learning must not consume the tutorial-only breakthrough receipt.");

            Assert.That(campaign.Battle.CommittedForecasts
                .Where(value => value.CommandId == "CMD_ALL_OUT")
                .SelectMany(value => value.MemberActions)
                .Any(value => value.ArtId == "TREE_CA002_WPN_SPEAR_POLEARM_N02"), Is.True,
                "A newly learned active Art must enter the next complete-Union Forecast pool immediately.");
        }

        [Test]
        public void SupportForecastUsesTheMembersSupportTreeArtAndKeepsRecoveryFallback()
        {
            const string supportRoot = "TREE_CA002_ROLE_SABOTEUR_N01";
            var campaign = Require(_battle.StartEncounterBattle(
                CreateDainSupportCampaign(),
                _content,
                "BATTLE_DEEP_SUPPORT_070",
                "Prove member-specific Support Arts."));
            var union = campaign.Battle.PlayerUnions.Single();
            var support = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == union.UnionId && value.CommandId == "CMD_SUPPORT");
            var action = support.MemberActions.Single();

            Assert.That(action.ArtId, Is.EqualTo(supportRoot));
            Assert.That(action.ArtName, Is.EqualTo("Distract"));
            Assert.That(action.Discipline, Is.EqualTo("Support"));
            Assert.That(action.Kind, Is.EqualTo(BattleActionKind.Recovery));
            Assert.That(support.DeterministicDebugEvidence, Does.Contain(supportRoot));

            campaign = Require(_battle.SelectForecast(campaign, union.UnionId, support.ForecastId));
            campaign = Require(_battle.ConfirmRound(campaign, _content));
            var progressed = campaign.Battle.PlayerUnions.Single().Members.Single();
            Assert.That(progressed.ArtProgress.Single(value => value.ArtId == supportRoot).MeaningfulUses,
                Is.EqualTo(1));
            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "RECOVERY" && value.ArtId == supportRoot), Is.True,
                "Execution and its visible event must retain the chosen member Art identity.");

            var fallbackCampaign = Require(_battle.StartEncounterBattle(
                CreateMarenCampaign(levelTwoAndNearFirstDiscovery: false),
                _content,
                "BATTLE_DEEP_SUPPORT_FALLBACK_070",
                "Prove generic Support fallback."));
            var fallback = fallbackCampaign.Battle.CommittedForecasts.Single(value =>
                value.CommandId == "CMD_SUPPORT");
            Assert.That(fallback.MemberActions.Single().ArtId, Is.EqualTo("ART_RECOVER_BREATH"));
        }

        private static CampaignState CreateMarenCampaign(bool levelTwoAndNearFirstDiscovery)
        {
            const string root = "TREE_CA002_WPN_SPEAR_POLEARM_N01";
            var progression = levelTwoAndNearFirstDiscovery
                ? new RecruitProgressionState(
                    2, 100, 0, 0, 0, 0, 0, 0, 0,
                    new[] { root },
                    new[] { new RecruitArtMasteryState(root, "Martial", 0, 11) })
                : RecruitProgressionState.Default();
            var spear = new EquipmentItemState(
                "ITEM_MAREN_SPEAR_070",
                "EQ_MAREN_SPEAR_070",
                "Gatewatch Spear",
                new[] { EquipmentSlotIds.MainHand },
                new[] { "SPEAR", "POLEARM", "WEAPON" },
                "QUALITY_STANDARD",
                10000,
                false);
            var maren = new RecruitState(
                "SIGREC_MAREN_HOLT", 170, 170, 34, 34, "Maren Holt",
                RecruitOriginKind.Signature, "SIG_W01_01", "HUMAN", "WORLD_GATE_01",
                "CLASS_TEND_GUARDIAN", "Leader", 7200, RecruitAuthorityKind.Founder,
                string.Empty, string.Empty,
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, spear)
                }),
                true, string.Empty, "SIGREC_MAREN_HOLT", 82, 68, progression);
            var union = new UnionState(
                "UNION_MAREN_070", "Maren's Union", UnionKind.Normal,
                maren.RecruitId, new[] { maren.RecruitId },
                "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 18, 8500);
            var guild = new GuildState(
                "GUILD_DEEP_ART_070", 0, new[] { maren }, new[] { union });
            var profile = new NewGuildProfileState(
                "Guildmaster", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var opening = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000270",
                20260828L,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                opening);
        }

        private static CampaignState CreateDainSupportCampaign()
        {
            var staff = new EquipmentItemState(
                "ITEM_DAIN_STAFF_070",
                "EQ_DAIN_STAFF_070",
                "Deepwell Staff",
                new[] { EquipmentSlotIds.MainHand },
                new[] { "STAFF", "WEAPON" },
                "QUALITY_STANDARD",
                10000,
                false);
            var dain = new RecruitState(
                "SIGREC_DAIN_DEEPWELL", 145, 145, 20, 38, "Dain Deepwell",
                RecruitOriginKind.Signature, "SIG_W02_04", "GOBLIN", "WORLD_GATE_02",
                "CLASS_TEND_MAGE", "Specialist", 6960, RecruitAuthorityKind.Normal,
                string.Empty, string.Empty,
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, staff)
                }),
                true, string.Empty, "SIGREC_DAIN_DEEPWELL", 64, 79,
                RecruitProgressionState.Default());
            var union = new UnionState(
                "UNION_DAIN_SUPPORT_070", "Dain's Union", UnionKind.Normal,
                dain.RecruitId, new[] { dain.RecruitId },
                "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 18, 8500);
            var guild = new GuildState(
                "GUILD_DEEP_SUPPORT_070", 0, new[] { dain }, new[] { union });
            var profile = new NewGuildProfileState(
                "Guildmaster", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var opening = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000271",
                20260829L,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                opening);
        }

        private static T Require<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
