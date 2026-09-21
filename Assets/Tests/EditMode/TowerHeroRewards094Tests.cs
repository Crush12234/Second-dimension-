#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>
    /// Authority fixtures use the existing Tower test's synthetic claimed-victory
    /// helper. They exercise begin, return and completion proofs, not rendered
    /// combat or genuine player playthroughs. Host-only growth tests are labelled.
    /// </summary>
    public sealed class TowerHeroRewards094Tests
    {
        CampaignProgressionCommandService022 _service;
        CampaignRegistry022 _registry;
        GuildCityRecruitmentService017D _recruitment;
        HeroMaster300Catalog087 _heroes;

        [SetUp]
        public void SetUp()
        {
            _service = new CampaignProgressionCommandService022();
            _registry = CampaignRegistry022.LoadFromResources();
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _heroes = HeroMaster300Catalog087.FromJson(Resources.Load<TextAsset>(
                "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300").text);
            _recruitment = new GuildCityRecruitmentService017D(new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(root)), _heroes,
                new RecruitTreeProgressionService070(DeepProgressionCatalog070.LoadFromContentRoot(root)));
        }

        [TestCase(9, "", false)]
        [TestCase(10, "SS", false)]
        [TestCase(40, "SS", false)]
        [TestCase(49, "", false)]
        [TestCase(50, "SS", true)]
        [TestCase(100, "SS", true)]
        [TestCase(450, "SS", true)]
        [TestCase(490, "SS", false)]
        [TestCase(499, "", false)]
        [TestCase(500, "SSS", true)]
        [TestCase(510, "SSS", false)]
        [TestCase(550, "SSS", true)]
        [TestCase(551, "", false)]
        public void ExactTenFiftyAndTierBoundaries094(int floor, string tier, bool guaranteed)
        {
            Assert.That(TowerHeroRewardRules094.TierForFloor(floor), Is.EqualTo(tier));
            var plan = TowerHeroRewardRules094.BuildPlan(NewCampaign(), "RULE_FIXTURE_094_" + floor, floor, _heroes);
            if (tier.Length == 0) { Assert.That(plan, Is.Null); return; }
            Assert.That(plan.Tier, Is.EqualTo(tier));
            Assert.That(plan.Guaranteed, Is.EqualTo(guaranteed));
            Assert.That(plan.ChanceBasisPoints, Is.EqualTo(guaranteed ? 10000 : 100));
            if (guaranteed) Assert.That(plan.WinsHero, Is.True);
        }

        [TestCase(10)]
        [TestCase(510)]
        public void ChanceUsesSavedTowerRngAndSurvivesReload094(int floor)
        {
            var campaign = NewCampaign();
            var first = TowerHeroRewardRules094.BuildPlan(campaign, "CHANCE_RELOAD_094", floor, _heroes);
            SssTenV4AcquisitionService090.TowerOfferRoll090(campaign.CampaignSeed, campaign.CampaignGuid,
                TowerHeroRewardRules094.RollIdentity094(campaign, floor), floor, first.Tier == "SSS" ? 10 :
                    _heroes.AcceptedHeroes.Count(hero => hero.Rank == HeroMasterRank087.SS), out var roll, out var selection);
            Assert.That(first.SavedRoll0To9999, Is.EqualTo(roll));
            Assert.That(first.SelectionIndex, Is.EqualTo(selection));
            Assert.That(first.WinsHero, Is.EqualTo(roll < 100));
            Assert.That(CanonicalJson.Serialize(TowerHeroRewardRules094.BuildPlan(Reload(campaign),
                first.CompletionReceiptId, floor, _heroes)), Is.EqualTo(CanonicalJson.Serialize(first)));
        }

        [TestCase(10)]
        [TestCase(510)]
        public void RetryingSameActualFloorCannotRerollItsPrize094(int floor)
        {
            var campaign = NewCampaign();
            var first = TowerHeroRewardRules094.BuildPlan(campaign, "FIRST_ATTEMPT_COMPLETION_094", floor, _heroes);
            var retry = TowerHeroRewardRules094.BuildPlan(Reload(campaign), "RETRY_ATTEMPT_COMPLETION_094", floor, _heroes);
            Assert.That(retry.SavedRoll0To9999, Is.EqualTo(first.SavedRoll0To9999));
            Assert.That(retry.SelectionIndex, Is.EqualTo(first.SelectionIndex));
            Assert.That(retry.SelectedHeroId, Is.EqualTo(first.SelectedHeroId));
            Assert.That(retry.PlanId, Is.Not.EqualTo(first.PlanId), "The final award remains bound to its own certified receipt.");
        }

        [Test]
        public void OwningEverySssDoesNotEmptyOrRerollTheExactTenPool094()
        {
            var source = NewCampaign();
            var first = TowerHeroRewardRules094.BuildPlan(source, "ALL_OWNED_SSS_094", 500, _heroes);
            var recruits = source.Guild.Recruits.Concat(SssTenV4Roster090.All.Select(hero =>
                SssTenV4Roster090.MaterializeGrant(hero.HeroId).Recruit)).ToArray();
            var owned = WithRoster(source, recruits);
            var replay = TowerHeroRewardRules094.BuildPlan(owned, "ALL_OWNED_SSS_094", 500, _heroes);
            Assert.That(CanonicalJson.Serialize(replay), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(SssTenV4Roster090.FindOwned(owned.Guild.Recruits, replay.SelectedHeroId), Is.Not.Null);
        }

        [TestCase(10)]
        [TestCase(50)]
        [TestCase(500)]
        public void SavedEnvelopeRejectsRehashedChanceSelectionPoolAndIdentityTampering094(int floor)
        {
            // Envelope validator unit, not a certified floor-clear/grant fixture.
            var campaign = NewCampaign();
            var receipt = "SAVED_ENVELOPE_FIXTURE_094_" + floor;
            var plan = TowerHeroRewardRules094.BuildPlan(campaign, receipt, floor, _heroes);
            var eventId = (string)typeof(GuildCityRecruitmentService017D).GetMethod("TowerHeroEventId094",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { campaign, receipt, floor });
            var fields = new[] { eventId, floor.ToString(), plan.Tier, plan.Guaranteed ? "GUARANTEED" : "CHANCE",
                plan.ChanceBasisPoints.ToString(), plan.SavedRoll0To9999.ToString(), plan.SelectionIndex.ToString(),
                plan.CandidatePoolHash, plan.WinsHero ? plan.SelectedHeroId : "NO_HERO", plan.PlanId,
                string.Join(",", plan.CandidateHeroIds) };
            Assert.That(ValidEnvelope(campaign, Envelope(fields), eventId, receipt, floor), Is.True);
            foreach (var mutation in new[] {
                Tuple.Create(4, "1"), // cannot weaken a guaranteed reward with a newly hashed payload
                Tuple.Create(4, plan.Guaranteed ? "100" : "10000"),
                Tuple.Create(6, plan.CandidateHeroIds.Count.ToString()),
                Tuple.Create(7, new string('0', 64)),
                Tuple.Create(8, floor >= 500 ? "HERO_REC_001" : "NOT_AN_ACCEPTED_SS_HERO"),
                Tuple.Create(9, "TOWER_HERO094_" + new string('0', 64)),
                Tuple.Create(10, string.Join(",", plan.CandidateHeroIds.Concat(new[] { plan.CandidateHeroIds[0] }))) })
            {
                var altered = fields.ToArray();
                altered[mutation.Item1] = mutation.Item2;
                Assert.That(ValidEnvelope(campaign, Envelope(altered), eventId, receipt, floor), Is.False,
                    "Rehashing field " + mutation.Item1 + " must not create an authoritative saved reward.");
            }
        }

        bool ValidEnvelope(CampaignState campaign, string record, string eventId, string receipt, int floor) =>
            (bool)typeof(GuildCityRecruitmentService017D).GetMethod("ValidSavedTowerResult094",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_recruitment,
                    new object[] { campaign, record, eventId, receipt, floor });

        static string Envelope(string[] fields)
        {
            var payload = string.Join("|", fields);
            return GuildCityRecruitmentService017D.TowerHeroResultPrefix094 + payload + "|" + CanonicalJson.Sha256Hex(payload);
        }

        [Test]
        public void NewFloorSequencePassesTenAndUsesTemplateOneAtEleven094()
        {
            var campaign = NewCampaign();
            for (var floor = 1; floor <= 12; floor++)
            {
                var view = ReadFloors(campaign);
                Assert.That(view.NextActualFloor, Is.EqualTo(floor));
                campaign = Require(_service.BeginTowerFloor094(campaign, _registry));
                Assert.That(ReadFloors(campaign).ActiveActualFloor, Is.EqualTo(floor));
                Assert.That(ReadFloors(campaign).ContentTemplateFloor, Is.EqualTo((floor - 1) % 10 + 1));
                campaign = Require(_service.ApplyAbyssOperationCompletion(
                    CommitActiveFloor(campaign, "SEQUENCE_" + floor), _registry, _recruitment));
                Assert.That(ReadFloors(campaign).HighestActualFloor, Is.EqualTo(floor));
            }
            Assert.That(ReadFloors(Reload(campaign)).NextActualFloor, Is.EqualTo(13));
            Assert.That(HeroOutcomes(campaign).Length, Is.EqualTo(1), "Only actual floor10 had a hero roll.");
            Assert.That(campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023
                .ExpeditionRecruitLeadIds089, Is.Empty, "New policy must not also award an old paid lead.");
        }

        [Test]
        public void TenRepeatDefinitionsKeepGuardianEncountersAndDoNotBecomeAscensionTrials094()
        {
            Assert.That(_registry.AbyssOperations.Count, Is.EqualTo(40));
            for (var floor = 1; floor <= 10; floor++)
            {
                var repeat = _registry.AbyssOperations[CampaignProgressionCommandService022.TowerOperationDefinitionId094(floor, false)];
                var guardian = _registry.AbyssOperations[CampaignProgressionCommandService022.TowerOperationDefinitionId094(floor, true)];
                var historicalTrial = _registry.AbyssOperations["ABYSS_OP022_" + floor.ToString("00") + "_TRIAL"];
                Assert.That(repeat.kind, Is.EqualTo(CampaignProgressionCommandService022.EndlessBattleKind094));
                Assert.That(repeat.kind, Is.Not.EqualTo("TRIAL"), "Ordinary repeated battles cannot enter the SSS ascension-trial credit hook.");
                Assert.That(repeat.firstClearOnly, Is.False);
                Assert.That(repeat.steps.Count(step => step.requiresBattle), Is.EqualTo(1));
                Assert.That(repeat.steps.Single(step => step.requiresBattle).bossId,
                    Is.EqualTo(guardian.steps.Single(step => step.requiresBattle).bossId));
                Assert.That(repeat.guildXp, Is.EqualTo(historicalTrial.guildXp));
                Assert.That(repeat.hallXp, Is.EqualTo(historicalTrial.hallXp));
                Assert.That(repeat.rewardMaterialIds, Is.EqualTo(historicalTrial.rewardMaterialIds));
                Assert.That(TowerAdventureRules084.IsCompatible084(repeat), Is.True);
                if (floor % 2 == 1)
                    Assert.That(historicalTrial.steps.Any(step => step.requiresBattle), Is.False,
                        "Historical nonbattle trials remain unchanged for old saved receipts.");
            }
        }

        [Test]
        public void AbortAndRetryKeepTheSameFloorAndMissingMarkerFailsClosed094()
        {
            var begun = Require(_service.BeginTowerFloor094(NewCampaign(), _registry));
            var firstInstance = Progression(begun).ActiveAbyssOperation.OperationInstanceId;
            var lostMarker = WithoutBindings(begun);
            Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(lostMarker, _registry).IsSuccess, Is.False);
            Assert.That(_service.CommitAbyssBattleEncounter(lostMarker, _registry).IsSuccess, Is.False);
            var retreated = Require(_service.RetreatAbyssOperation(begun, _registry));
            Assert.That(ReadFloors(retreated).HighestActualFloor, Is.Zero);
            Assert.That(ReadFloors(retreated).NextActualFloor, Is.EqualTo(1));
            var retried = Require(_service.BeginTowerFloor094(Reload(retreated), _registry));
            Assert.That(ReadFloors(retried).ActiveActualFloor, Is.EqualTo(1));
            Assert.That(Progression(retried).ActiveAbyssOperation.OperationInstanceId, Is.Not.EqualTo(firstInstance));
            Assert.That(HeroOutcomes(retried), Is.Empty);
            Assert.That(_service.BeginAbyssOperation(retreated, _registry, "ABYSS_OP022_01_GUARDIAN")
                .Errors, Does.Contain("TOWER094_USE_ENDLESS_FLOOR_START"));
        }

        [Test]
        public void ExistingPendingRunKeepsItsOriginalPolicyAndReplay094()
        {
            var old = Require(_service.BeginAbyssOperation(NewCampaign(), _registry, "ABYSS_OP022_01_GUARDIAN"));
            Assert.That(ReadFloors(old).ActiveUsesNewPolicy, Is.False);
            var committed = CommitActiveFloor(old, "LEGACY_PENDING_094");
            var first = Require(_service.ApplyAbyssOperationCompletion(committed, _registry));
            var withNewDependency = Require(_service.ApplyAbyssOperationCompletion(Reload(committed), _registry, _recruitment));
            Assert.That(CanonicalJson.Serialize(withNewDependency), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(HeroOutcomes(first), Is.Empty);
        }

        [Test]
        public void RehashedBindingCannotSkipFromFirstFloorToFiftieth094()
        {
            var begun = Require(_service.BeginTowerFloor094(NewCampaign(), _registry));
            var forged = RebindFloorForNegativeFixture(begun, 50);
            Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(forged, _registry).IsSuccess, Is.False,
                "Even a matching binding hash and instance ID cannot replace the sequential completion chain.");
            Assert.That(_service.CommitAbyssBattleEncounter(forged, _registry).IsSuccess, Is.False);
            Assert.That(InvokeReward(forged, "FORGED_FIFTIETH_094", 50).IsSuccess, Is.False);
            Assert.That(HeroOutcomes(forged), Is.Empty);
        }

        [Test]
        public void HistoricalRepeatedFloorTenClearsDoNotBecomeActualFloorTwelve094()
        {
            var campaign = NewCampaign();
            for (var ordinal = 1; ordinal <= 12; ordinal++)
                campaign = Legacy<CampaignState>("CompleteNextTowerFloor089", campaign, _service, _registry, ordinal);
            Assert.That(Progression(campaign).AbyssFloors.Sum(value => value.ClearCount), Is.EqualTo(12));
            Assert.That(ReadFloors(campaign).HighestActualFloor, Is.EqualTo(10));
            campaign = Require(_service.BeginTowerFloor094(campaign, _registry));
            Assert.That(ReadFloors(campaign).ActiveActualFloor, Is.EqualTo(11));
        }

        [Test]
        public void TenthFloorOutcomeAndMissOrWinAreSavedExactlyOnce094()
        {
            var campaign = NewCampaign();
            for (var floor = 1; floor < 10; floor++) campaign = CompleteNewFloor(campaign, floor);
            var begun = Require(_service.BeginTowerFloor094(campaign, _registry));
            var committed = CommitActiveFloor(begun, "SAVED_EVENT_10");
            var before = CanonicalJson.Serialize(committed);
            Assert.That(_service.ApplyAbyssOperationCompletion(committed, _registry).Errors,
                Does.Contain("TOWER094_RECRUITMENT_AUTHORITY_REQUIRED"));
            Assert.That(CanonicalJson.Serialize(committed), Is.EqualTo(before));
            var first = Require(_service.ApplyAbyssOperationCompletion(committed, _registry, _recruitment));
            var replay = Require(_service.ApplyAbyssOperationCompletion(Reload(committed), _registry, _recruitment));
            Assert.That(first.Guild.TreasuryXp, Is.EqualTo(committed.Guild.TreasuryXp +
                Progression(committed).ActiveAbyssOperation.PendingReceipt.GuildXp),
                "Hero chance must neither charge XP nor inflate the existing floor-clear XP reward.");
            Assert.That(CanonicalJson.Serialize(replay), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(HeroOutcomes(first).Length, Is.EqualTo(1));
            var receipt = Progression(first).AbyssAuthorityEntries.Last().CompletionProof.CompletionReceipt;
            var replayAdapter = InvokeReward(first, receipt.ReceiptId, 10);
            Assert.That(CanonicalJson.Serialize(Require(replayAdapter)), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(_service.ApplyAbyssOperationCompletion(first, _registry, _recruitment).IsSuccess, Is.False);
        }

        [Test]
        public void ActiveOrInventedClearCannotGrantMilestoneHero094()
        {
            var active = Require(_service.BeginTowerFloor094(NewCampaign(), _registry));
            var before = CanonicalJson.Serialize(active);
            Assert.That(InvokeReward(active, "NOT_A_COMPLETION_094", 50).IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(active), Is.EqualTo(before));
            Assert.That(HeroOutcomes(active), Is.Empty);
        }

        [TestCase(50, 0)]
        [TestCase(50, 9)]
        [TestCase(50, 10)]
        [TestCase(500, 0)]
        [TestCase(500, 9)]
        [TestCase(500, 10)]
        public void IsolatedHostDuplicateUsesA10ThenLegalGrowthWithoutWeaponGrant094(int floor, int startingRank)
        {
            // Host-layer unit only: the separate completion tests prove that this
            // private helper is unreachable from unverified gameplay reward calls.
            var campaign = NewCampaign();
            var plan = TowerHeroRewardRules094.BuildPlan(campaign, "HOST_GROWTH_094_" + floor, floor, _heroes);
            campaign = InvokeHost(campaign, plan, "HOST_INITIAL_094_" + floor);
            var owned = FindSelected(campaign, plan);
            var raised = owned.Progression;
            for (var rank = 0; rank < startingRank; rank++) raised = raised.Ascend089();
            campaign = WithRoster(campaign, campaign.Guild.Recruits.Select(value =>
                value == owned ? owned.WithProgression(raised) : value).ToArray());
            owned = FindSelected(campaign, plan);
            var beforeProgression = CanonicalJson.Serialize(owned.Progression);
            var beforeInventory = CanonicalJson.Serialize(campaign.Guild.Inventory);
            var beforeSss = CanonicalJson.Serialize(SssTenV4CampaignAccessor090.Read(campaign));
            var count = campaign.Guild.Recruits.Count;
            var treasury = campaign.Guild.TreasuryXp;
            var grown = InvokeHost(campaign, plan, "HOST_DUPLICATE_094_" + floor + "_" + startingRank);
            var next = FindSelected(grown, plan).Progression;
            Assert.That(grown.Guild.Recruits.Count, Is.EqualTo(count));
            Assert.That(grown.Guild.TreasuryXp, Is.EqualTo(treasury));
            Assert.That(next.AscensionLevel, Is.EqualTo(Math.Min(10, startingRank + 1)));
            Assert.That(CanonicalJson.Serialize(next), Is.Not.EqualTo(beforeProgression));
            Assert.That(next.LearnedArtIds, Is.SupersetOf(owned.Progression.LearnedArtIds));
            if (startingRank == 10)
                Assert.That(next.UnlockedTreeIds.Count > owned.Progression.UnlockedTreeIds.Count ||
                    next.ArtMastery.Sum(value => value.MasteryPoints) > owned.Progression.ArtMastery.Sum(value => value.MasteryPoints),
                    Is.True, "An unmastered A10 hero must gain a legal tree or Art level, not useless credits.");
            Assert.That(CanonicalJson.Serialize(grown.Guild.Inventory), Is.EqualTo(beforeInventory));
            Assert.That(CanonicalJson.Serialize(SssTenV4CampaignAccessor090.Read(grown)), Is.EqualTo(beforeSss),
                "No family defeats, world wins, weapon hunt receipts or signature entitlements are fabricated.");
        }

        [TestCase(50)]
        [TestCase(500)]
        public void IsolatedNewHeroGrantIsExactReserveAndCannotGrantSignatureWeapon094(int floor)
        {
            var before = NewCampaign();
            var plan = TowerHeroRewardRules094.BuildPlan(before, "NEW_HERO_HOST_094_" + floor, floor, _heroes);
            var beforeSss = CanonicalJson.Serialize(SssTenV4CampaignAccessor090.Read(before));
            var granted = InvokeHost(before, plan, "NEW_HERO_SOURCE_094_" + floor);
            var hero = FindSelected(granted, plan);
            Assert.That(hero, Is.Not.Null);
            Assert.That(granted.Guild.Recruits.Count, Is.EqualTo(before.Guild.Recruits.Count + 1));
            Assert.That(granted.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
            Assert.That(granted.Guild.GuildCity.MemberAssignments.Single(value => value.RecruitId == hero.RecruitId)
                .Kind, Is.EqualTo(GuildMemberAssignmentKind017D.Reserve));
            var newItems = granted.Guild.Inventory.Where(value =>
                !before.Guild.Inventory.Any(old => old.InstanceId == value.InstanceId));
            Assert.That(newItems.Any(item => SssTenV4Roster090.All.Any(sss => sss.WeaponItemId == item.DefinitionId)), Is.False,
                "Common starter gear may be supplied by the shared host; unique signature/Omega weapons may not.");
            Assert.That(CanonicalJson.Serialize(SssTenV4CampaignAccessor090.Read(granted)), Is.EqualTo(beforeSss));
        }

        [Test, Explicit("Bounded 50-floor authority fixture; run separately from the fast rule/host checks.")]
        public void FiftyActualFloorsEndWithOneGuaranteedSsAndNoLegacyLead094()
        {
            var campaign = NewCampaign();
            for (var floor = 1; floor <= 50; floor++) campaign = CompleteNewFloor(campaign, floor);
            Assert.That(ReadFloors(campaign).HighestActualFloor, Is.EqualTo(50));
            Assert.That(Progression(campaign).AbyssAuthorityEntries.Count(entry => !entry.Aborted), Is.EqualTo(50));
            Assert.That(Progression(campaign).AbyssAuthorityEntries.Where(entry => !entry.Aborted)
                .All(entry => !string.IsNullOrWhiteSpace(entry.CompletionProof.ExistingBattleRewardReceiptId)), Is.True,
                "Every one of the fifty actual floors must retain its own claimed certified battle proof.");
            Assert.That(HeroOutcomes(campaign).Length, Is.EqualTo(5));
            Assert.That(HeroOutcomes(campaign).Count(value => value.Contains("|50|SS|GUARANTEED|10000|")), Is.EqualTo(1));
            Assert.That(campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023
                .ExpeditionRecruitLeadIds089, Is.Empty);
        }

        CampaignState CompleteNewFloor(CampaignState campaign, int ordinal) => Require(
            _service.ApplyAbyssOperationCompletion(CommitActiveFloor(
                Require(_service.BeginTowerFloor094(campaign, _registry)), "FLOOR_" + ordinal), _registry, _recruitment));

        CampaignState CommitActiveFloor(CampaignState campaign, string receiptSalt)
        {
            var op = _registry.AbyssOperations[Progression(campaign).ActiveAbyssOperation.OperationDefinitionId];
            Assert.That(op.steps.Count(step => step.requiresBattle), Is.EqualTo(1),
                "Every new-policy actual floor must contain exactly one certified Union battle: " + op.operationId);
            while (Progression(campaign).ActiveAbyssOperation.Status == AbyssOperationStatus022.Active &&
                !op.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle)
            {
                campaign = Require(_service.CommitAbyssStep(campaign, _registry, "SUCCESS"));
                campaign = Require(_service.ApplyAbyssStep(campaign, _registry));
            }
            campaign = Require(_service.CommitAbyssBattleEncounter(campaign, _registry));
            campaign = Legacy<CampaignState>("WithClaimedVictory", campaign,
                campaign.Guild.GuildCity.PendingEncounter, "TOWER094_TEST_REWARD_" + receiptSalt);
            campaign = Require(_service.CommitAbyssBattleReturn(campaign, _registry));
            campaign = Require(_service.ApplyAbyssBattleReturnExactlyOnce(campaign, _registry));
            campaign = Require(_service.CommitAbyssBattleResult(campaign, _registry));
            campaign = Require(_service.ApplyAbyssBattleAndFinalize(campaign, _registry));
            while (Progression(campaign).ActiveAbyssOperation.Status == AbyssOperationStatus022.Active)
            {
                campaign = Require(_service.CommitAbyssStep(campaign, _registry, "SUCCESS"));
                campaign = Require(_service.ApplyAbyssStep(campaign, _registry));
            }
            return Require(_service.CommitAbyssOperationCompletion(campaign, _registry));
        }

        Result<CampaignState> InvokeReward(CampaignState campaign, string receipt, int floor) =>
            (Result<CampaignState>)typeof(GuildCityRecruitmentService017D).GetMethod("ApplyTowerFloorReward094",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_recruitment, new object[] { campaign, _registry, receipt, floor });

        CampaignState InvokeHost(CampaignState campaign, TowerHeroRewardPlan094 plan, string source) => Require(
            (Result<CampaignState>)typeof(GuildCityRecruitmentService017D).GetMethod("ApplyTowerHero094",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_recruitment, new object[] { campaign, plan, source }));

        static RecruitState FindSelected(CampaignState campaign, TowerHeroRewardPlan094 plan) => plan.Tier == "SSS"
            ? SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, plan.SelectedHeroId)
            : campaign.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == plan.SelectedHeroId);

        static CampaignState WithoutBindings(CampaignState campaign)
        {
            var d = campaign.Guild.Development;
            var clean = new GuildDevelopmentState(d.HallStageIndex, d.HallStageId, d.HallEnhancementXp,
                d.LifetimeTreasuryXpEarned, d.Facilities, d.ClaimedBattleRewardIds,
                d.AppliedAdventureAuthorityIds.Where(value => !value.StartsWith("TOWER_FLOOR094|", StringComparison.Ordinal)).ToArray());
            return campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory, clean), campaign.OpeningFlow);
        }

        static CampaignState RebindFloorForNegativeFixture(CampaignState campaign, int fakeFloor)
        {
            var state = Progression(campaign);
            var source = state.ActiveAbyssOperation;
            var instance = CampaignProgressionCommandService022.TowerOperationPrefix094 +
                fakeFloor.ToString("D6") + "_" + source.CanonicalSeedIdentity.Substring(0, 24).ToUpperInvariant();
            var active = new AbyssOperationState022(instance, source.OperationDefinitionId, source.FloorId,
                source.CurrentStepIndex, source.Status, source.CompletedStepIds, source.PendingReceipt,
                source.ExistingBattleRewardReceiptId, source.CanonicalSeedIdentity, source.AppliedReceiptCountAtBegin,
                source.AppliedStepProofs, source.CommittedOperationOrdinal, source.PreviousAuthorityHash,
                source.BeginAuthorityHash, source.AlliedRosterIdentity, source.AlliedRecruitIds, source.FloorClearCountAtBegin);
            var grant = new AbyssBeginGrant022(active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.CommittedOperationOrdinal, instance, active.OperationDefinitionId, active.FloorId,
                active.AppliedReceiptCountAtBegin.Value, active.FloorClearCountAtBegin,
                active.AlliedRosterIdentity, active.AlliedRecruitIds);
            var binding = (string)typeof(CampaignProgressionCommandService022).GetMethod("TowerFloorBinding094",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { campaign,
                    active.BeginAuthorityHash, active.PreviousAuthorityHash, instance,
                    active.OperationDefinitionId, active.FloorId, fakeFloor, TowerThreatRules098.Policy098 });
            var d = campaign.Guild.Development;
            var forgedLedger = new GuildDevelopmentState(d.HallStageIndex, d.HallStageId, d.HallEnhancementXp,
                d.LifetimeTreasuryXpEarned, d.Facilities, d.ClaimedBattleRewardIds,
                d.AppliedAdventureAuthorityIds.Where(value => !value.StartsWith("TOWER_FLOOR094|", StringComparison.Ordinal))
                    .Concat(new[] { binding }).ToArray());
            var changed = campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory, forgedLedger), campaign.OpeningFlow);
            return Legacy<CampaignState>("WithProgression", changed, state.With(activeAbyssOperation: active,
                replaceActiveAbyssOperation: true, activeAbyssBeginGrant: grant, replaceActiveAbyssBeginGrant: true));
        }

        TowerFloorProgress094 ReadFloors(CampaignState campaign)
        {
            var result = CampaignProgressionCommandService022.DescribeTowerFloors094(campaign, _registry);
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        static string[] HeroOutcomes(CampaignState campaign) => campaign.Guild.Development.AppliedAdventureAuthorityIds
            .Where(value => value.StartsWith(GuildCityRecruitmentService017D.TowerHeroResultPrefix094, StringComparison.Ordinal)).ToArray();
        static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static CampaignState NewCampaign() => Legacy<CampaignState>("CreateTowerCampaign");
        static CampaignState Reload(CampaignState campaign) => JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
        static CampaignState WithRoster(CampaignState campaign, RecruitState[] roster) => campaign.With(
            campaign.Guild.With(campaign.Guild.TreasuryXp, roster, campaign.Guild.Unions,
                campaign.Guild.Inventory, campaign.Guild.Development), campaign.OpeningFlow);
        static T Legacy<T>(string method, params object[] args) => (T)typeof(TowerRun081EditModeTests)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
