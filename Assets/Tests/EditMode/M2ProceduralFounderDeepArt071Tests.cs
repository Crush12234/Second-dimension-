using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2ProceduralFounderDeepArt071Tests
    {
        private static readonly string[] FounderIds =
        {
            "PROC_36344E2400DC98B6",
            "PROC_F85A4CAA747BC8C6",
            "PROC_5B14E7816E55FFB5",
            "PROC_748DD03A23E1FEB0"
        };

        private M2CombatContent _content;
        private M2BattleCommandService _battle;
        private IReadOnlyDictionary<string, RecruitState> _founders;

        [OneTimeSetUp]
        public void SetUp()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            _content = M2CombatContent.LoadFromDirectory(contentRoot);
            _battle = new M2BattleCommandService();

            var recruitment = RecruitmentContent.LoadFromDirectory(contentRoot);
            var board = ApplicantBoardStateAdapter.ToFrozenTutorialState(
                new TutorialApplicantFactory(recruitment).CreateFrozenBoard());
            var initializer = new RecruitAutoGenerationSigningService010(
                new M1CommandService(),
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)));
            var founders = new Dictionary<string, RecruitState>(StringComparer.Ordinal);
            foreach (var applicant in board.Applicants)
            {
                if (applicant.Kind != ApplicantKind.Procedural) continue;
                var recruit = new RecruitState(
                    applicant.RecruitId,
                    applicant.CurrentHp,
                    applicant.MaximumHp,
                    applicant.CurrentMp,
                    applicant.MaximumMp,
                    applicant.DisplayName,
                    RecruitOriginKind.Procedural,
                    applicant.SignatureId,
                    applicant.RaceId,
                    applicant.WorldId,
                    applicant.ClassTendencyId,
                    applicant.LeadershipBand,
                    applicant.PotentialBasisPoints,
                    RecruitAuthorityKind.Normal,
                    applicant.CanonicalApplicantJson,
                    applicant.CanonicalScoutingReportJson,
                    applicant.OpeningLoadout,
                    applicant.VitalsInitialized,
                    applicant.TutorialAliasId,
                    applicant.AuthoredStableRecruitId,
                    applicant.LeadershipScore,
                    applicant.TacticalAptitude);
                recruit = initializer.InitializeRecruit(recruit);
                founders.Add(recruit.RecruitId, recruit);
            }
            _founders = founders;
        }

        [TestCase("PROC_36344E2400DC98B6")]
        [TestCase("PROC_F85A4CAA747BC8C6")]
        [TestCase("PROC_5B14E7816E55FFB5")]
        [TestCase("PROC_748DD03A23E1FEB0")]
        public void FrozenProceduralFounderCanDiscoverTheNextNodeInASeededTree(
            string founderId)
        {
            var recruit = ReadyForNextDiscovery(_founders[founderId]);
            var member = BattleMember(recruit);
            var sourceArtId = ProgressibleSource(member);

            Assert.That(M2DeepArtRuntime070.TryGetNextLearning(
                    recruit,
                    member,
                    sourceArtId,
                    0,
                    _content,
                    out var opportunity),
                Is.True,
                founderId);
            Assert.That(opportunity, Is.Not.Null, founderId);
            Assert.That(opportunity.CanLearnNow, Is.True, founderId);
            Assert.That(opportunity.TargetArtId, Does.EndWith("_N02")
                    .Or.EndWith("_N03")
                    .Or.EndWith("_N04"),
                founderId + " should advance beyond its generated starting node(s).");
            Assert.That(member.LearnedArtIds, Does.Not.Contain(opportunity.TargetArtId),
                founderId);
            Assert.That(_content.DeepProgression.Node(opportunity.TargetArtId).TreeId,
                Is.EqualTo(_content.DeepProgression.Node(sourceArtId).TreeId),
                founderId);

            var campaign = Require(_battle.StartEncounterBattle(
                Campaign(recruit),
                _content,
                "BATTLE_PROCEDURAL_FOUNDER_DEEP_ART_071",
                "Prove generated founder deep-Art learning."));
            var union = campaign.Battle.PlayerUnions.Single();
            var unionForecasts = campaign.Battle.CommittedForecasts
                .Where(value => value.UnionId == union.UnionId)
                .ToArray();
            var forecast = unionForecasts.FirstOrDefault(value =>
                value.MemberActions.Any(action => action.BreakthroughOpportunity));
            Assert.That(forecast, Is.Not.Null,
                founderId + " must surface a ready generated-tree discovery in a discipline-compatible forecast. " +
                string.Join(" | ", unionForecasts.Select(value =>
                    value.CommandId + ":" + string.Join(",", value.MemberActions.Select(action => action.ArtId)))));
            var action = forecast.MemberActions.Single();
            Assert.That(action.BreakthroughOpportunity, Is.True, founderId);
            Assert.That(action.BreakthroughTargetArtId, Is.Not.Empty, founderId);
            Assert.That(_content.DeepProgression.TryNode(
                action.BreakthroughTargetArtId, out _), Is.True, founderId);

            campaign = Require(_battle.SelectForecast(
                campaign,
                union.UnionId,
                forecast.ForecastId));
            campaign = Require(_battle.ConfirmRound(campaign, _content));
            Assert.That(campaign.Battle.PlayerUnions.Single().Members.Single().LearnedArtIds,
                Does.Contain(action.BreakthroughTargetArtId),
                founderId + " must learn the forecasted generated-tree Art in live resolution.");
            Assert.That(campaign.Battle.EventLog.Any(value =>
                    value.EventType == "BREAKTHROUGH" &&
                    value.ArtId == action.BreakthroughTargetArtId),
                Is.True,
                founderId);
        }

        [Test]
        public void TransientOrUnverifiedProceduralArtCannotOpenADeepTree()
        {
            var lawful = ReadyForNextDiscovery(_founders[FounderIds[0]]);
            var member = BattleMember(lawful);
            var sourceArtId = ProgressibleSource(member);
            var unverified = CopyRecruit(
                lawful,
                RecruitOriginKind.Procedural,
                canonicalApplicantJson: string.Empty);

            Assert.That(M2DeepArtRuntime070.TryGetNextLearning(
                    unverified,
                    member,
                    sourceArtId,
                    1000,
                    _content,
                    out _),
                Is.False,
                "A battle-only learned Art is not procedural generation authority.");

            var legacy = CopyRecruit(
                lawful,
                RecruitOriginKind.Legacy,
                canonicalApplicantJson: lawful.CanonicalApplicantJson);
            Assert.That(M2DeepArtRuntime070.TryGetNextLearning(
                    legacy,
                    member,
                    sourceArtId,
                    1000,
                    _content,
                    out _),
                Is.False,
                "Legacy recruits must not inherit procedural root authority.");
        }

        [Test]
        public void ProceduralWeaponTreeNeedsItsCurrentlyEquippedWeaponTags()
        {
            var recruit = ReadyForNextDiscovery(_founders[FounderIds[0]]);
            var equipped = BattleMember(recruit);
            var sourceArtId = recruit.Progression.LearnedArtIds.First(value =>
                _content.DeepProgression.TryNode(value, out var node) &&
                node.RequiredEquipmentTagsAny.Count > 0 &&
                IsEquipmentLegal(node, equipped.EquipmentTags));
            var unequipped = new BattleMemberState(
                equipped.MemberId,
                equipped.DisplayName,
                equipped.ClassId,
                equipped.CurrentHp,
                equipped.MaximumHp,
                equipped.CurrentMp,
                equipped.MaximumMp,
                equipped.Attack,
                equipped.MagicAttack,
                Array.Empty<string>(),
                false,
                false,
                false,
                equipped.LearnedArtIds,
                equipped.MeaningfulUsePoints,
                equipped.DiscoveryProgress,
                equipped.BreakthroughArtId,
                equipped.ArtProgress);

            Assert.That(M2DeepArtRuntime070.TryGetNextLearning(
                    recruit,
                    unequipped,
                    sourceArtId,
                    1000,
                    _content,
                    out _),
                Is.False);
        }

        private RecruitState ReadyForNextDiscovery(RecruitState recruit)
        {
            var mastery = recruit.Progression.LearnedArtIds
                .Select(artId => new RecruitArtMasteryState(
                    artId,
                    _content.Arts.TryGetValue(artId, out var art)
                        ? art.Discipline
                        : "LEGACY",
                    0,
                    1000))
                .ToArray();
            var progression = new RecruitProgressionState(
                RecruitProgressionRules021.MaximumLevel,
                RecruitProgressionRules021.TotalXpRequiredForLevel(
                    RecruitProgressionRules021.MaximumLevel),
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                recruit.Progression.LearnedArtIds,
                mastery,
                recruit.Progression.UnlockedTreeIds);
            return recruit.WithProgression(progression);
        }

        private BattleMemberState BattleMember(RecruitState recruit)
        {
            var tags = recruit.Equipment.Assignments
                .SelectMany(value => value.Item.EquipmentTags)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var progress = recruit.Progression.ArtMastery
                .Select(value => new BattleArtProgressState(
                    value.ArtId,
                    value.Discipline,
                    value.MeaningfulUses,
                    value.MasteryPoints))
                .ToArray();
            return new BattleMemberState(
                recruit.RecruitId,
                recruit.DisplayName,
                recruit.ClassTendencyId,
                recruit.CurrentHp,
                recruit.MaximumHp,
                recruit.CurrentMp,
                recruit.MaximumMp,
                50,
                50,
                tags,
                false,
                false,
                false,
                recruit.Progression.LearnedArtIds,
                progress.Sum(value => value.MasteryPoints),
                0,
                string.Empty,
                progress);
        }

        private static CampaignState Campaign(RecruitState recruit)
        {
            var union = new UnionState(
                "UNION_" + recruit.RecruitId,
                recruit.DisplayName + "'s Union",
                UnionKind.Normal,
                recruit.RecruitId,
                new[] { recruit.RecruitId },
                "FORMATION_SKIRMISH_LINE",
                "DOCTRINE_BALANCED",
                18,
                8500);
            var guild = new GuildState(
                "GUILD_PROCEDURAL_FOUNDER_071",
                0,
                new[] { recruit },
                new[] { union });
            var profile = new NewGuildProfileState(
                "Guildmaster",
                GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            var opening = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                false,
                "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000071",
                20260829L,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                opening);
        }

        private string ProgressibleSource(BattleMemberState member)
        {
            var learned = new HashSet<string>(member.LearnedArtIds, StringComparer.Ordinal);
            foreach (var sourceArtId in member.LearnedArtIds.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!_content.DeepProgression.TryNode(sourceArtId, out var source) ||
                    !_content.Arts[sourceArtId].IsForecastAction ||
                    !IsEquipmentLegal(source, member.EquipmentTags))
                    continue;
                var tree = _content.DeepProgression.Tree(source.TreeId);
                foreach (var nodeId in tree.NodeIds)
                {
                    if (learned.Contains(nodeId)) continue;
                    var candidate = _content.DeepProgression.Node(nodeId);
                    if (candidate.PrerequisiteNodeIds.Any(value => !learned.Contains(value)) ||
                        !IsEquipmentLegal(candidate, member.EquipmentTags))
                        continue;
                    return sourceArtId;
                }
            }
            Assert.Fail("The frozen founder needs a legal active generated Art with a next node.");
            return string.Empty;
        }

        private static bool IsEquipmentLegal(
            DeepNodeDefinition070 node,
            IReadOnlyList<string> equipmentTags) =>
            node.RequiredEquipmentTagsAny.Count == 0 ||
            node.RequiredEquipmentTagsAny.Any(required =>
                equipmentTags.Contains(required, StringComparer.Ordinal));

        private static RecruitState CopyRecruit(
            RecruitState source,
            RecruitOriginKind originKind,
            string canonicalApplicantJson) =>
            new RecruitState(
                source.RecruitId,
                source.CurrentHp,
                source.MaximumHp,
                source.CurrentMp,
                source.MaximumMp,
                source.DisplayName,
                originKind,
                source.SignatureId,
                source.RaceId,
                source.WorldId,
                source.ClassTendencyId,
                source.LeadershipBand,
                source.PotentialBasisPoints,
                source.AuthorityKind,
                canonicalApplicantJson,
                source.CanonicalScoutingReportJson,
                source.Equipment,
                source.VitalsInitialized,
                source.TutorialAliasId,
                source.AuthoredStableRecruitId,
                source.LeadershipScore,
                source.TacticalAptitude,
                source.Progression);

        private static T Require<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
