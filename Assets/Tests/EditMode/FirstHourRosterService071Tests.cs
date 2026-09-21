using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourRosterService071Tests
    {
        private static readonly string[] OpeningRecruitIds =
        {
            "PROC_36344E2400DC98B6",
            "PROC_F85A4CAA747BC8C6",
            "PROC_5B14E7816E55FFB5",
            "PROC_748DD03A23E1FEB0",
            "SIGREC_MAREN_HOLT",
            "SIGREC_ODELIA_FEN"
        };

        private string _contentRoot;
        private FirstHourRosterService071 _service;
        private M2CombatContent _combat;
        private M2BattleCommandService _battle;
        private RecruitAutoGenerator010 _generator;
        private RecruitAutoGenerationSigningService010 _initializer;
        private CampaignState _initializedFirstHourRoster;

        [OneTimeSetUp]
        public void SetUp()
        {
            _contentRoot = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            _service = FirstHourRosterService071.LoadFromContentRoot(_contentRoot);
            _combat = M2CombatContent.LoadFromDirectory(_contentRoot);
            _battle = new M2BattleCommandService();
            _generator = new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(_contentRoot));
            _initializer = new RecruitAutoGenerationSigningService010(
                new M1CommandService(),
                _generator);
            _initializedFirstHourRoster = CreateInitializedFirstHourRoster();
        }

        [Test]
        public void StoryWavesGrowPermanentRosterFromSixToTenToTwenty()
        {
            var opening = CreateOpeningCampaign();
            var charter = Require(_service.EnsureCharterRoster(opening));
            var completed = Require(_service.EnsureLanternPatrol(charter));

            Assert.That(opening.Guild.Recruits, Has.Count.EqualTo(6));
            Assert.That(charter.Guild.Recruits, Has.Count.EqualTo(10));
            Assert.That(completed.Guild.Recruits, Has.Count.EqualTo(20));
            Assert.That(opening.Guild.TreasuryXp, Is.EqualTo(500));
            Assert.That(charter.Guild.TreasuryXp, Is.EqualTo(500));
            Assert.That(completed.Guild.TreasuryXp, Is.EqualTo(500));
            Assert.That(new GuildCityEffectService017D().RosterCapacity(
                completed.Guild.Development), Is.EqualTo(24));

            AssertStableIds(charter, FirstHourRosterService071.CharterStableRecruitIds);
            AssertStableIds(completed, FirstHourRosterService071.CharterStableRecruitIds
                .Concat(FirstHourRosterService071.PatrolStableRecruitIds));
        }

        [Test]
        public void EverySparseW02PatrolSignatureMaterializesThroughTheShippingAuthority()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var content = RecruitmentContent.FromJson(
                File.ReadAllText(Path.Combine(contentRoot, "OPENING_PROCEDURAL_TABLES.json")),
                File.ReadAllText(Path.Combine(
                    contentRoot, "CONTENT_AUTHORITY_002", "DATA", "SIGNATURE_RECRUITS_300.json")),
                File.ReadAllText(Path.Combine(contentRoot, "RECRUITMENT_OFFICE_PROGRESSION.json")));
            var materializer = new SignatureRecruitMaterializer(content);

            foreach (var index in Enumerable.Range(1, 10))
            {
                var signatureId = "SIG_W02_" + (index < 10 ? "0" : string.Empty) + index;
                var first = materializer.Materialize(
                    20260828L, signatureId, "FIRST_HOUR_071_LANTERN_PATROL");
                var replay = materializer.Materialize(
                    20260828L, signatureId, "FIRST_HOUR_071_LANTERN_PATROL");

                Assert.That(first.SignatureId, Is.EqualTo(signatureId));
                Assert.That(first.WeaponAptitudes, Is.Not.Empty, signatureId);
                Assert.That(first.WeaponAptitudes.Keys.All(value =>
                    value.StartsWith("WEAPON_FAMILY_", StringComparison.Ordinal)), Is.True,
                    signatureId + " must accept the authored weaponFamilyId alias.");
                Assert.That(first.EquipmentLoadout.AggregateBonuses, Is.Empty,
                    signatureId + " intentionally omits aggregateBonuses.");

                var equippedItems = first.EquipmentLoadout.Slots.Values
                    .Where(value => value != null)
                    .ToArray();
                Assert.That(equippedItems, Is.Not.Empty, signatureId);
                Assert.That(equippedItems.All(value => value.Tags != null && value.Tags.Count > 0), Is.True,
                    signatureId + " must carry canonical equipment tags into live combat.");
                Assert.That(CanonicalJson.Serialize(replay), Is.EqualTo(CanonicalJson.Serialize(first)),
                    signatureId + " must remain deterministic after tolerant sparse-field parsing.");
            }

            var charter = Require(_service.EnsureCharterRoster(CreateOpeningCampaign()));
            var completed = Require(_service.EnsureLanternPatrol(charter));
            Assert.That(completed.Guild.Recruits.Count(value =>
                value.SignatureId != null &&
                value.SignatureId.StartsWith("SIG_W02_", StringComparison.Ordinal)), Is.EqualTo(10));
            AssertStableIds(completed, FirstHourRosterService071.PatrolStableRecruitIds);
        }

        [Test]
        public void ReplayingEitherStoryWaveIsExactlyIdempotent()
        {
            var charter = Require(_service.EnsureCharterRoster(CreateOpeningCampaign()));
            var charterReplay = Require(_service.EnsureCharterRoster(charter));
            Assert.That(CanonicalJson.Serialize(charterReplay), Is.EqualTo(CanonicalJson.Serialize(charter)));

            var completed = Require(_service.EnsureLanternPatrol(charter));
            var completedReplay = Require(_service.EnsureLanternPatrol(completed));
            var outOfOrderReplay = Require(_service.EnsureCharterRoster(completedReplay));
            Assert.That(CanonicalJson.Serialize(completedReplay), Is.EqualTo(CanonicalJson.Serialize(completed)));
            Assert.That(CanonicalJson.Serialize(outOfOrderReplay), Is.EqualTo(CanonicalJson.Serialize(completed)));
            Assert.That(completed.Guild.Recruits.Select(value => value.RecruitId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
            Assert.That(completed.Guild.GuildCity.MemberAssignments.Select(value => value.RecruitId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
        }

        [Test]
        public void AuthoredRosterMaterializationIsDeterministicForCampaignSeed()
        {
            var first = Require(_service.EnsureLanternPatrol(
                Require(_service.EnsureCharterRoster(CreateOpeningCampaign()))));
            var second = Require(_service.EnsureLanternPatrol(
                Require(_service.EnsureCharterRoster(CreateOpeningCampaign()))));

            Assert.That(CanonicalJson.Sha256Hex(second), Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            Assert.That(second.Guild.Recruits.Select(value => value.RecruitId),
                Is.EqualTo(first.Guild.Recruits.Select(value => value.RecruitId)));
        }

        [Test]
        public void StoryAnnexRaisesDormitoriesOnlyAsNeededAndNeverReducesThem()
        {
            var charter = Require(_service.EnsureCharterRoster(CreateOpeningCampaign()));
            Assert.That(new GuildCityEffectService017D().RosterCapacity(
                charter.Guild.Development), Is.EqualTo(12));

            var completed = Require(_service.EnsureLanternPatrol(charter));
            Assert.That(new GuildCityEffectService017D().FacilityLevel(
                completed.Guild.Development, "FACILITY_DORMITORIES"), Is.EqualTo(1));
            Assert.That(new GuildCityEffectService017D().RosterCapacity(
                completed.Guild.Development), Is.EqualTo(24));

            var alreadyExpanded = Require(_service.EnsureLanternPatrol(
                Require(_service.EnsureCharterRoster(CreateOpeningCampaign(dormitoryLevel: 3)))));
            Assert.That(new GuildCityEffectService017D().FacilityLevel(
                alreadyExpanded.Guild.Development, "FACILITY_DORMITORIES"), Is.EqualTo(3));
            Assert.That(new GuildCityEffectService017D().RosterCapacity(
                alreadyExpanded.Guild.Development), Is.EqualTo(75));
        }

        [Test]
        public void AddedMembersAreNormalEquippedProgressingAndNeverProtectedActors()
        {
            var completed = Require(_service.EnsureLanternPatrol(
                Require(_service.EnsureCharterRoster(CreateOpeningCampaign()))));
            var authoredIds = new HashSet<string>(
                FirstHourRosterService071.CharterStableRecruitIds
                    .Concat(FirstHourRosterService071.PatrolStableRecruitIds),
                StringComparer.Ordinal);
            var added = completed.Guild.Recruits
                .Where(value => authoredIds.Contains(value.AuthoredStableRecruitId))
                .ToArray();
            var protectedNames = new HashSet<string>(
                new FirstHourDirector071().ProtectedActors.Select(value => value.DisplayName),
                StringComparer.Ordinal);

            Assert.That(added, Has.Length.EqualTo(14));
            Assert.That(added.Select(value => value.AuthoredStableRecruitId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(14));
            Assert.That(added.Select(value => value.SignatureId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(14));
            foreach (var recruit in added)
            {
                Assert.That(recruit.AuthorityKind, Is.EqualTo(RecruitAuthorityKind.Normal),
                    recruit.AuthoredStableRecruitId);
                Assert.That(protectedNames.Contains(recruit.DisplayName), Is.False,
                    recruit.AuthoredStableRecruitId);
                Assert.That(ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                    recruit.RecruitId, recruit.AuthoredStableRecruitId, recruit.AuthorityKind),
                    Is.True, recruit.AuthoredStableRecruitId);
                Assert.That(recruit.CanonicalApplicantJson, Is.Not.Empty,
                    recruit.AuthoredStableRecruitId);
                Assert.That(recruit.Equipment.Assignments, Is.Not.Empty,
                    recruit.AuthoredStableRecruitId);
                Assert.That(recruit.Equipment.Assignments.All(value =>
                        value.Item.EquipmentTags.Count > 0), Is.True,
                    recruit.AuthoredStableRecruitId);
                Assert.That(recruit.Progression.LearnedArtIds, Is.Not.Empty,
                    recruit.AuthoredStableRecruitId);
                Assert.That(recruit.Progression.ArtMastery, Is.Not.Empty,
                    recruit.AuthoredStableRecruitId);
                Assert.That(recruit.Progression.UnlockedTreeIds, Has.Count.EqualTo(2),
                    recruit.AuthoredStableRecruitId);
            }
        }

        [Test]
        public void AllTwentyLiveRecruitsStartWithWeaponAndPrimaryRoleOnly()
        {
            Assert.That(_initializedFirstHourRoster.Guild.Recruits, Has.Count.EqualTo(20));
            foreach (var recruit in _initializedFirstHourRoster.Guild.Recruits)
            {
                var profile = _generator.Generate(recruit);
                var expectedTrees = new[]
                {
                    profile.WeaponTreeId,
                    profile.PrimaryRoleTreeId
                };
                var persistentTrees = DeepTreeIds(recruit.Progression.LearnedArtIds);
                var battleArts = M2DeepArtRuntime070.BattleLearnedArts(recruit, _combat);
                var battleTrees = DeepTreeIds(battleArts);

                Assert.That(expectedTrees.All(value => !string.IsNullOrWhiteSpace(value)), Is.True,
                    recruit.RecruitId);
                Assert.That(expectedTrees.Distinct(StringComparer.Ordinal).ToArray(), Has.Length.EqualTo(2),
                    recruit.RecruitId);
                Assert.That(recruit.Progression.UnlockedTreeIds, Is.EquivalentTo(expectedTrees),
                    recruit.RecruitId + " must persist exactly its weapon and primary role trees.");
                Assert.That(persistentTrees, Is.EquivalentTo(expectedTrees),
                    recruit.RecruitId + " must not pre-learn a locked deep-tree root.");
                Assert.That(battleTrees, Is.EquivalentTo(expectedTrees),
                    recruit.RecruitId + " must enter battle with the same two usable families.");

                AssertLockedRootAbsent(profile.MysticTreeId, recruit, battleArts);
                AssertLockedRootAbsent(profile.SecondaryRoleTreeId, recruit, battleArts);
                foreach (var legacyArtId in recruit.Progression.LearnedArtIds.Where(value =>
                             !_combat.DeepProgression.TryNode(value, out _)))
                    Assert.That(battleArts, Does.Contain(legacyArtId),
                        recruit.RecruitId + " must preserve legitimate non-deep legacy Arts.");
            }
        }

        [Test]
        public void UnlockingAThirdTreeAddsItsRootWithoutOpeningTheFourth()
        {
            var recruit = _initializedFirstHourRoster.Guild.Recruits.Single(value =>
                StringComparer.Ordinal.Equals(value.SignatureId, "SIG_W02_01"));
            var profile = _generator.Generate(recruit);
            var thirdTreeId = profile.MysticTreeId;
            var fourthTreeId = profile.SecondaryRoleTreeId;
            var thirdRoot = _combat.DeepProgression.Tree(thirdTreeId).RootNodeId;
            var fourthRoot = _combat.DeepProgression.Tree(fourthTreeId).RootNodeId;

            var oldSaveArts = recruit.Progression.LearnedArtIds
                .Concat(new[] { thirdRoot, "ART_GUARD" })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var oldSave = recruit.WithProgression(recruit.Progression.WithArts(
                oldSaveArts,
                recruit.Progression.ArtMastery));
            var dormantBattleArts = M2DeepArtRuntime070.BattleLearnedArts(oldSave, _combat);
            Assert.That(oldSave.Progression.LearnedArtIds, Does.Contain(thirdRoot),
                "Compatibility keeps dormant learned data in the save.");
            Assert.That(dormantBattleArts, Does.Not.Contain(thirdRoot),
                "A dormant old-save root cannot become a battle action before unlock.");
            Assert.That(dormantBattleArts, Does.Contain("ART_GUARD"),
                "Non-deep legacy Arts remain available.");

            var unlocked = _combat.DeepTreeProgression.UnlockEarnableTree(recruit, thirdTreeId);
            var battleArts = M2DeepArtRuntime070.BattleLearnedArts(unlocked, _combat);
            Assert.That(unlocked.Progression.UnlockedTreeIds, Has.Count.EqualTo(3));
            Assert.That(unlocked.Progression.UnlockedTreeIds, Does.Contain(thirdTreeId));
            Assert.That(unlocked.Progression.LearnedArtIds, Does.Contain(thirdRoot));
            Assert.That(battleArts, Does.Contain(thirdRoot));
            Assert.That(battleArts, Does.Not.Contain(fourthRoot));
        }

        [Test]
        public void EveryPatrolRecruitBuildsAndExecutesAnEquipmentLegalForecast()
        {
            var patrol = _initializedFirstHourRoster.Guild.Recruits
                .Where(value => value.SignatureId != null &&
                    value.SignatureId.StartsWith("SIG_W02_", StringComparison.Ordinal))
                .OrderBy(value => value.SignatureId, StringComparer.Ordinal)
                .ToArray();
            Assert.That(patrol, Has.Length.EqualTo(10));

            foreach (var recruit in patrol)
            {
                var mainHand = recruit.Equipment.Find(EquipmentSlotIds.MainHand);
                Assert.That(mainHand, Is.Not.Null, recruit.SignatureId);
                Assert.That(mainHand.Item.EquipmentTags, Is.Not.Empty, recruit.SignatureId);

                var campaign = Require(_battle.StartEncounterBattle(
                    CreateSingleRecruitCampaign(recruit),
                    _combat,
                    "BATTLE_W02_TAG_TRUTH_" + recruit.SignatureId,
                    "Verify patrol recruit forecast legality."));
                var union = campaign.Battle.PlayerUnions.Single();
                var member = union.Members.Single();
                var forecasts = campaign.Battle.CommittedForecasts
                    .Where(value => StringComparer.Ordinal.Equals(value.UnionId, union.UnionId))
                    .ToArray();
                var actions = forecasts.SelectMany(value => value.MemberActions).ToArray();

                Assert.That(DeepTreeIds(member.LearnedArtIds), Has.Length.EqualTo(2),
                    recruit.SignatureId);
                Assert.That(forecasts, Is.Not.Empty, recruit.SignatureId);
                Assert.That(actions, Is.Not.Empty,
                    recruit.SignatureId + " must never produce the former empty-action patrol forecast.");
                Assert.That(actions.All(value =>
                    StringComparer.Ordinal.Equals(value.ActorMemberId, recruit.RecruitId)), Is.True,
                    recruit.SignatureId);
                foreach (var action in actions)
                {
                    Assert.That(member.LearnedArtIds, Does.Contain(action.ArtId),
                        recruit.SignatureId + ":" + action.ArtId);
                    if (!_combat.DeepProgression.TryNode(action.ArtId, out var node)) continue;
                    Assert.That(node.RequiredEquipmentTagsAny.Count == 0 ||
                        node.RequiredEquipmentTagsAny.Any(required =>
                            member.EquipmentTags.Contains(required, StringComparer.Ordinal)), Is.True,
                        recruit.SignatureId + ":" + action.ArtId);
                }

                var selected = forecasts.FirstOrDefault(value =>
                    value.CommandId == "CMD_ALL_OUT" && value.MemberActions.Count > 0) ??
                    forecasts.First(value => value.MemberActions.Count > 0);
                var planned = selected.MemberActions[0];
                campaign = Require(_battle.SelectForecast(
                    campaign,
                    union.UnionId,
                    selected.ForecastId));
                campaign = Require(_battle.ConfirmRound(campaign, _combat));
                Assert.That(campaign.Battle.EventLog.Any(value =>
                        StringComparer.Ordinal.Equals(value.ActorMemberId, recruit.RecruitId) &&
                        StringComparer.Ordinal.Equals(value.ArtId, planned.ArtId)), Is.True,
                    recruit.SignatureId + " must execute its forecasted action.");
            }
        }

        [Test]
        public void ReplayingPatrolRepairsEmptyCanonicalTagsWithoutReplacingPlayerState()
        {
            var recruits = new List<RecruitState>(_initializedFirstHourRoster.Guild.Recruits);
            var recruitIndex = recruits.FindIndex(value =>
                StringComparer.Ordinal.Equals(value.SignatureId, "SIG_W02_01"));
            var source = recruits[recruitIndex];
            var strippedAssignments = new List<EquipmentSlotAssignmentState>();
            foreach (var assignment in source.Equipment.Assignments)
            {
                var item = assignment.Item;
                var isMainHand = StringComparer.Ordinal.Equals(
                    assignment.SlotId,
                    EquipmentSlotIds.MainHand);
                var stripped = new EquipmentItemState(
                    item.InstanceId,
                    item.DefinitionId,
                    isMainHand ? "Player Renamed Spear" : item.DisplayName,
                    item.ValidSlotIds,
                    Array.Empty<string>(),
                    isMainHand ? "QUALITY_PLAYER_UPGRADED" : item.QualityId,
                    isMainHand ? 7777 : item.ConditionBasisPoints,
                    isMainHand || item.PlayerLocked);
                strippedAssignments.Add(new EquipmentSlotAssignmentState(
                    assignment.SlotId,
                    stripped));
            }
            recruits[recruitIndex] = source.WithEquipment(
                new EquipmentLoadoutState(strippedAssignments.AsReadOnly()));
            var guild = _initializedFirstHourRoster.Guild.With(
                _initializedFirstHourRoster.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                _initializedFirstHourRoster.Guild.Unions,
                _initializedFirstHourRoster.Guild.Inventory);
            var strippedCampaign = _initializedFirstHourRoster.With(
                guild,
                _initializedFirstHourRoster.OpeningFlow);

            var repaired = Require(_service.EnsureLanternPatrol(strippedCampaign));
            var repairedRecruit = repaired.Guild.Recruits[recruitIndex];
            var repairedMain = repairedRecruit.Equipment.Find(EquipmentSlotIds.MainHand).Item;
            Assert.That(repairedRecruit.Equipment.Assignments.All(value =>
                value.Item.EquipmentTags.Count > 0), Is.True);
            Assert.That(repairedMain.InstanceId, Is.EqualTo(
                source.Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId));
            Assert.That(repairedMain.DisplayName, Is.EqualTo("Player Renamed Spear"));
            Assert.That(repairedMain.QualityId, Is.EqualTo("QUALITY_PLAYER_UPGRADED"));
            Assert.That(repairedMain.ConditionBasisPoints, Is.EqualTo(7777));
            Assert.That(repairedMain.PlayerLocked, Is.True);

            var replay = Require(_service.EnsureLanternPatrol(repaired));
            Assert.That(CanonicalJson.Serialize(replay), Is.EqualTo(CanonicalJson.Serialize(repaired)));
        }

        private CampaignState CreateInitializedFirstHourRoster()
        {
            var recruitment = RecruitmentContent.LoadFromDirectory(_contentRoot);
            var board = ApplicantBoardStateAdapter.ToFrozenTutorialState(
                new TutorialApplicantFactory(recruitment).CreateFrozenBoard());
            var commands = new M1CommandService();
            var profile = new NewGuildProfileState(
                "First Hour Truth Test",
                SecondDimension.Core.GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            var campaign = Require(commands.CreateNewGuild(new NewGuildCommand(
                "00000000-0000-0000-0000-000000000172",
                20260813L,
                "1.0",
                "GUILD_FIRST_HOUR_TRUTH_071",
                profile)));
            campaign = Require(commands.AcceptCivicCharter(campaign));
            campaign = Require(commands.CommitApplicantBoard(campaign, board));
            foreach (var applicant in board.Applicants)
                campaign = Require(_initializer.SignApplicant(campaign, applicant.RecruitId));
            campaign = Require(_service.EnsureCharterRoster(campaign));
            return Require(_service.EnsureLanternPatrol(campaign));
        }

        private CampaignState CreateSingleRecruitCampaign(RecruitState recruit)
        {
            var union = new UnionState(
                "UNION_" + recruit.RecruitId,
                recruit.DisplayName + " Patrol",
                UnionKind.Normal,
                recruit.RecruitId,
                new[] { recruit.RecruitId },
                "FORMATION_SKIRMISH_LINE",
                "DOCTRINE_BALANCED",
                18,
                8500);
            var guild = new GuildState(
                "GUILD_PATROL_TRUTH_071",
                0,
                new[] { recruit },
                new[] { union });
            var profile = new NewGuildProfileState(
                "Guildmaster",
                SecondDimension.Core.GameMode.Standard,
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
                "00000000-0000-0000-0000-000000000271",
                20260829L,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                opening);
        }

        private string[] DeepTreeIds(IEnumerable<string> artIds) =>
            artIds
                .Select(value => _combat.DeepProgression.TryNode(value, out var node)
                    ? node.TreeId
                    : string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private void AssertLockedRootAbsent(
            string treeId,
            RecruitState recruit,
            IReadOnlyList<string> battleArts)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return;
            var root = _combat.DeepProgression.Tree(treeId).RootNodeId;
            Assert.That(recruit.Progression.UnlockedTreeIds, Does.Not.Contain(treeId),
                recruit.RecruitId + ":" + treeId);
            Assert.That(recruit.Progression.LearnedArtIds, Does.Not.Contain(root),
                recruit.RecruitId + ":" + root);
            Assert.That(battleArts, Does.Not.Contain(root),
                recruit.RecruitId + ":" + root);
        }

        private static CampaignState CreateOpeningCampaign(int dormitoryLevel = 0)
        {
            var recruits = new List<RecruitState>();
            for (var index = 0; index < OpeningRecruitIds.Length; index++)
                recruits.Add(new RecruitState(OpeningRecruitIds[index], 100, 100, 20, 20));
            var development = GuildDevelopmentState.Default();
            if (dormitoryLevel > 0)
                development = development.SetFacilityLevel(
                    "FACILITY_DORMITORIES", dormitoryLevel, facilityXpContribution: 0);
            var guild = new GuildState(
                "GUILD_FIRST_HOUR_071_TEST",
                500,
                recruits.AsReadOnly(),
                Array.Empty<UnionState>(),
                Array.Empty<EquipmentItemState>(),
                development);
            return new CampaignState(
                "00000000-0000-0000-0000-000000000071",
                20260828L,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild);
        }

        private static CampaignState Require(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void AssertStableIds(
            CampaignState campaign,
            IEnumerable<string> expectedStableIds)
        {
            var actual = new HashSet<string>(
                campaign.Guild.Recruits.Select(value => value.AuthoredStableRecruitId),
                StringComparer.Ordinal);
            foreach (var stableId in expectedStableIds)
                Assert.That(actual.Contains(stableId), Is.True, stableId);
        }
    }
}
