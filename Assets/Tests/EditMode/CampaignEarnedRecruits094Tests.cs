using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignEarnedRecruits094Tests
    {
        GuildCityRecruitmentService017D _recruitment;
        HeroMaster300Catalog087 _heroes;
        ICampaignRuleCatalog019 _chapters;
        IWorldGateOperationsCatalog023 _gates;
        RecruitmentContent _recruitContent;
        GuildCityContent017D _cityContent;
        CampaignState _earned;

        [OneTimeSetUp]
        public void LoadActualEarnedCheckpointAndExistingRecruitmentAuthorities()
        {
            var fixture = new ExpeditionRecruitLeadApplicant089Tests();
            fixture.SetUp();
            _recruitment = Field<GuildCityRecruitmentService017D>(fixture, "_recruitment");
            _heroes = Field<HeroMaster300Catalog087>(fixture, "_heroes");
            _recruitContent = Field<RecruitmentContent>(fixture, "_recruitmentContent");
            _cityContent = Field<GuildCityContent017D>(fixture, "_cityContent");
            _chapters = new CampaignRuleCatalogAdapter019(CampaignRegistry019.LoadFromResources().Base018);
            _gates = new Campaign023RuleCatalogAdapter(CampaignRegistry023.LoadFromResources());
            // Unmodified output of the actual R65 command-only CH001 recovery.
            // This fixture is earned proof, not fabricated completion flags.
            var path = Path.Combine(Application.dataPath, "Tests", "Fixtures",
                "EarnedChapter001094.fixture.json");
            var envelope = JsonConvert.DeserializeObject<SaveEnvelopeV1>(File.ReadAllText(path));
            Assert.That(CanonicalJson.Sha256Hex(envelope.CampaignState), Is.EqualTo(envelope.CanonicalStateHash));
            _earned = envelope.CampaignState;
            Assert.That(Validate(_earned), Is.True);
            Assert.That(_earned.Guild.GuildCity.Strategic017H.Campaign019.CompletedChapterIds,
                Is.EquivalentTo(new[] { "CH018_001" }));
        }

        [Test]
        public void EarnedChapterGivesThreeUniqueNormalHeroesWithExactPreviewAndRetainedProofs()
        {
            var campaign = Clone(_earned);
            var before = CanonicalJson.Sha256Hex(campaign);
            var view = View(campaign);
            Assert.That(view.PendingChapterRecruits, Is.EqualTo(3));
            Assert.That(view.PendingOpeningStoryRecruits, Is.EqualTo(9));
            Assert.That(view.ClaimableCount, Is.EqualTo(12));
            Assert.That(view.CanClaim, Is.True);
            Assert.That(view.Preview.Count, Is.EqualTo(3));
            Assert.That(view.Preview.Select(value => value.StableId).Distinct().Count(), Is.EqualTo(3));
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(before), "Projection cannot grant or save.");
            var oldIds = new HashSet<string>(campaign.Guild.Recruits.Select(value => value.RecruitId));
            var claimed = Claim(campaign);
            var joined = claimed.Guild.Recruits.Where(value => !oldIds.Contains(value.RecruitId)).ToArray();
            Assert.That(joined, Has.Length.EqualTo(12));
            Assert.That(view.Preview.All(value => joined.Any(recruit => recruit.AuthoredStableRecruitId == value.StableId)), Is.True);
            Assert.That(joined.All(value => value.AuthorityKind == RecruitAuthorityKind.Normal), Is.True);
            Assert.That(joined.All(value => value.Progression.UnlockedTreeIds.Count > 0), Is.True);
            Assert.That(joined.All(value => _heroes.TryGetAcceptedHero(value.AuthoredStableRecruitId, out var hero) &&
                hero.IsNormalApplicantEligible && hero.Rank != HeroMasterRank087.SS), Is.True);
            Assert.That(claimed.Guild.TreasuryXp, Is.EqualTo(campaign.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(claimed.OpeningFlow), Is.EqualTo(CanonicalJson.Serialize(campaign.OpeningFlow)));
            Assert.That(Validate(claimed), Is.True, "Historical roster proofs must survive normal new recruits.");
            Assert.That(View(claimed).PendingCount, Is.Zero);
        }

        [Test]
        public void EarnedHeroMatchesActualPaidSigningArtAndGearMaterializationWithoutItsXpDebit()
        {
            var claimed = Claim(Clone(_earned));
            var heroId = View(_earned).Preview[0].StableId;
            Assert.That(_heroes.TryGetAcceptedHero(heroId, out var hero), Is.True);
            var applicant = HeroMaster300ApplicantLead089.ToApplicant(hero, 1, "SKYHOME");
            Assert.That(applicant.Slot, Is.EqualTo(1), "Reward materialization uses the same legal one-based applicant slot as paid signing.");
            var board = new ApplicantBoardState("FIXTURE_PAID_094", "FIXTURE_PAID_094", 0,
                true, new[] { applicant }, null);
            // An explicit unit-fixture wallet/board, not the isolated real-save
            // assessment. The production paid signing command owns the debit.
            var paidFixture = _earned.With(_earned.Guild.With(10000,
                _earned.Guild.Recruits, _earned.Guild.Unions, _earned.Guild.Inventory)
                .WithGuildCity(_earned.Guild.GuildCity.With(recruitmentBoard: board,
                    replaceRecruitmentBoard: true)), _earned.OpeningFlow);
            var price = _recruitment.DescribeApplicantSigning090(paidFixture, applicant).EffectiveCostTreasuryXp;
            Assert.That(price, Is.GreaterThan(0));
            var paid = Require(_recruitment.SignApplicant(paidFixture, applicant.RecruitId));
            var expected = paid.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == heroId);
            var earned = claimed.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == heroId);
            Assert.That(CanonicalJson.Serialize(earned), Is.EqualTo(CanonicalJson.Serialize(expected)));
            Assert.That(paid.Guild.TreasuryXp, Is.EqualTo(10000 - price));
            Assert.That(claimed.Guild.TreasuryXp, Is.EqualTo(_earned.Guild.TreasuryXp));
        }

        [Test]
        public void SaveReloadAndReplayCannotGrantAgainOrRecursivelyIncreaseCapacity()
        {
            var claimed = Claim(Clone(_earned));
            var effects = new GuildCityEffectService017D();
            var baseCapacity = effects.RosterCapacityAtDormitoryLevel(
                effects.FacilityLevel(_earned.Guild.Development, "FACILITY_DORMITORIES"));
            Assert.That(effects.RosterCapacity(claimed.Guild.Development), Is.EqualTo(baseCapacity + 12));
            var hash = CanonicalJson.Sha256Hex(claimed);
            for (var iteration = 0; iteration < 3; iteration++)
            {
                claimed = Claim(Clone(claimed));
                Assert.That(CanonicalJson.Sha256Hex(claimed), Is.EqualTo(hash));
                Assert.That(effects.RosterCapacity(claimed.Guild.Development), Is.EqualTo(baseCapacity + 12));
            }
            Assert.That(claimed.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .CreatorAccess028.RedeemedCodeIds, Is.EquivalentTo(_earned.Guild.GuildCity
                    .Strategic017H.Campaign019.Playable020.CreatorAccess028.RedeemedCodeIds),
                "Natural rewards must not fabricate code redemptions.");
        }

        [Test]
        public void HousingTwentyFourDoesNotDiscardEarnedNewMembers()
        {
            // Use normal accepted reward materialization to fill the existing
            // fixture's 24-slot housing exactly, without altering its chapter proof.
            var full = FillWithUnownedNormalHeroes(Clone(_earned), 24);
            Assert.That(new GuildCityEffectService017D().RosterCapacity(full.Guild.Development), Is.EqualTo(24));
            Assert.That(Validate(full), Is.True);
            var claimed = Claim(full);
            Assert.That(claimed.Guild.Recruits.Count, Is.EqualTo(36));
            Assert.That(new GuildCityEffectService017D().RosterCapacity(claimed.Guild.Development), Is.EqualTo(36));
            Assert.That(claimed.Guild.TreasuryXp, Is.EqualTo(full.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(claimed.Guild.Development.Facilities),
                Is.EqualTo(CanonicalJson.Serialize(full.Guild.Development.Facilities)));
        }

        [Test]
        public void RegularApplicantDeskRemainsXpPaidBeforeAndAfterEarnedClaims()
        {
            foreach (var campaign in new[] { Clone(_earned), Claim(Clone(_earned)) })
            {
                var boardCampaign = Require(_recruitment.CommitBoard(campaign, _recruitContent, _cityContent));
                var offers = boardCampaign.Guild.GuildCity.RecruitmentBoard.Applicants.Where(value =>
                    !boardCampaign.Guild.Recruits.Any(owned => owned.RecruitId == value.RecruitId)).ToArray();
                Assert.That(offers, Is.Not.Empty);
                foreach (var offer in offers)
                    Assert.That(_recruitment.DescribeApplicantSigning090(boardCampaign, offer)
                        .EffectiveCostTreasuryXp, Is.GreaterThan(0), offer.DisplayName);
            }
        }

        [TestCase("missing_proof")]
        [TestCase("bad_proof_hash")]
        [TestCase("bad_chain")]
        [TestCase("missing_global_chapter_receipt")]
        [TestCase("bare_extra_completed_flag")]
        public void UnverifiedLegacyCompletionStaysBlockedWithoutInventedRewards(string mutation)
        {
            var json = JObject.FromObject(_earned);
            // Remove the separate opening reward proof only in these negative
            // fixtures so its independently valid nine rewards do not obscure
            // whether the tampered campaign chapter was rejected.
            foreach (var oldReward in ((JArray)json.SelectToken("Guild.Development.ClaimedBattleRewardIds"))
                .Where(value => ((string)value).StartsWith("CONTRACT_REWARD_", StringComparison.Ordinal)).ToArray())
                oldReward.Remove();
            var progress = json.SelectToken("Guild.GuildCity.Strategic017H.Campaign019");
            var gate = progress["Playable020"]["WorldGate023"];
            if (mutation == "missing_proof") gate["CompletionProofs"] = new JArray();
            else if (mutation == "bad_proof_hash") gate["CompletionProofs"][0]["CompletionLedgerHash"] = "FORGED";
            else if (mutation == "bad_chain") gate["AuthorityChainHash"] = "FORGED";
            else if (mutation == "missing_global_chapter_receipt")
            {
                var chapterReceipt = (string)progress["AppliedReceiptIds"][0];
                var claims = (JArray)json.SelectToken("Guild.Development.ClaimedBattleRewardIds");
                claims.First(value => (string)value == chapterReceipt).Remove();
            }
            else ((JArray)progress["CompletedChapterIds"]).Add("CH018_002");
            var invalid = json.ToObject<CampaignState>();
            var before = CanonicalJson.Sha256Hex(invalid);
            var view = View(invalid);
            Assert.That(view.BlockedChapterRecruits, Is.GreaterThanOrEqualTo(3));
            Assert.That(view.PendingCount, Is.Zero);
            Assert.That(_recruitment.ClaimEarnedCampaignRecruits094(invalid, _chapters, _gates).IsSuccess, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(invalid), Is.EqualTo(before));
        }

        [Test]
        public void ExhaustedEligiblePoolRetainsThreePendingAndNeverSubstitutesSsOrAscension()
        {
            var campaign = FillWithUnownedNormalHeroes(Clone(_earned), 300);
            var before = CanonicalJson.Sha256Hex(campaign);
            var view = View(campaign);
            Assert.That(view.PendingChapterRecruits, Is.EqualTo(3));
            Assert.That(view.AvailableUniqueHeroes, Is.Zero);
            Assert.That(view.ClaimableCount, Is.Zero);
            Assert.That(view.CanClaim, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(Claim(campaign)), Is.EqualTo(before));
            Assert.That(View(Clone(campaign)).PendingChapterRecruits, Is.EqualTo(3));
        }

        [Test]
        public void OpeningStoryMatchesItsOwnRewardReceiptsAndNeverRepeatableContractOrFlags()
        {
            foreach (var id in new[] { "CONTRACT_BELL_BENEATH_GATE", "CONTRACT_LINES_NOT_RETURNED", "CONTRACT_RELIEF_ROAD" })
                Assert.That(GuildCityRecruitmentService017D.HasVerifiedOpeningStoryReward094(_earned, id), Is.True, id);
            Assert.That(GuildCityRecruitmentService017D.HasVerifiedOpeningStoryReward094(_earned, "CONTRACT_NOT_A_STORY_CHAPTER"), Is.False);
            var json = JObject.FromObject(_earned);
            foreach (var receipt in ((JArray)json.SelectToken("Guild.Development.ClaimedBattleRewardIds"))
                .Where(value => ((string)value).StartsWith("CONTRACT_REWARD_", StringComparison.Ordinal)).ToArray()) receipt.Remove();
            Assert.That(View(json.ToObject<CampaignState>()).PendingOpeningStoryRecruits, Is.Zero,
                "Operation ordinal and story flags are not a completed-contract receipt.");
        }

        [Test]
        public void NearlyFullDurableLedgerRejectsClaimAtomicallyBeforeAnyNewRecruitOrCapacity()
        {
            var development = _earned.Guild.Development;
            var entries = development.AppliedAdventureAuthorityIds.ToList();
            entries.AddRange(Enumerable.Range(0,
                GuildDevelopmentState.AdventureAuthorityEntryLimit - 1 - entries.Count)
                .Select(index => "FIXTURE_FULL094_" + index));
            var full = new GuildDevelopmentState(development.HallStageIndex,
                development.HallStageId, development.HallEnhancementXp,
                development.LifetimeTreasuryXpEarned, development.Facilities,
                development.ClaimedBattleRewardIds, entries.AsReadOnly());
            var campaign = _earned.With(_earned.Guild.With(_earned.Guild.TreasuryXp,
                _earned.Guild.Recruits, _earned.Guild.Unions, _earned.Guild.Inventory,
                full), _earned.OpeningFlow);
            var result = _recruitment.ClaimEarnedCampaignRecruits094(campaign, _chapters, _gates);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(string.Join(";", result.Errors), Does.Contain("LEDGER_FULL"));
            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(_earned.Guild.Recruits.Count));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(_earned.Guild.TreasuryXp));
            Assert.That(GuildCityRecruitmentService017D.EarnedRecruitCapacity094(campaign.Guild.Development), Is.Zero);
            Assert.That(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(GuildDevelopmentState.AdventureAuthorityEntryLimit - 1));
        }

        CampaignState FillWithUnownedNormalHeroes(CampaignState campaign, int count)
        {
            var materialize = typeof(GuildCityRecruitmentService017D).GetMethod(
                "MaterializeApplicant094", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var hero in _heroes.AcceptedHeroes.Where(value => value.IsNormalApplicantEligible))
            {
                if (campaign.Guild.Recruits.Count >= count) break;
                if (HeroMaster300ApplicantLead089.RosterContains(campaign.Guild.Recruits, hero)) continue;
                var recruit = (RecruitState)materialize.Invoke(_recruitment, new object[] {
                    HeroMaster300ApplicantLead089.ToApplicant(hero, 1, "SKYHOME") });
                campaign = Require(new CreatorAccessCommandService028().GrantCharacterReward(campaign,
                    "FIXTURE094_" + hero.StableId, hero.StableId,
                    new CreatorRecruitGrant028(recruit, Array.Empty<EquipmentItemState>())));
            }
            return campaign;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualCoordinatorReloadPreservesEarnedCapacityWithoutFreeHousingMigration(bool claimFirst)
        {
            var campaign = claimFirst ? Claim(Clone(_earned)) : Clone(_earned);
            var expected = CanonicalJson.Sha256Hex(campaign);
            var effects = new GuildCityEffectService017D();
            var level = effects.FacilityLevel(campaign.Guild.Development, "FACILITY_DORMITORIES");
            Assert.That(level, Is.EqualTo(1));
            Assert.That(effects.RosterCapacity(campaign.Guild.Development), Is.EqualTo(claimFirst ? 36 : 24));
            var directory = Path.Combine(Path.GetTempPath(), "SD_EarnedCapacity094_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "CampaignSave.json");
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(campaign,
                    new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc)));
                for (var iteration = 0; iteration < 2; iteration++)
                {
                    var coordinator = new SecondDimension.Presentation.M1RuntimeCoordinator(
                        Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), path);
                    Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(expected),
                        "Actual resume initialization must not inflate Dormitories for earned roster slots.");
                    var loaded = store.ReadWithRecovery(path);
                    Assert.That(loaded.IsSuccess, Is.True, string.Join("; ", loaded.Errors));
                    Assert.That(loaded.Value.CanonicalStateHash, Is.EqualTo(expected));
                    Assert.That(effects.FacilityLevel(loaded.Value.CampaignState.Guild.Development,
                        "FACILITY_DORMITORIES"), Is.EqualTo(level));
                    Assert.That(coordinator.GuildCity017D.RosterCapacity, Is.EqualTo(claimFirst ? 36 : 24));
                }
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        EarnedCampaignRecruitsView094 View(CampaignState campaign) =>
            _recruitment.DescribeEarnedCampaignRecruits094(campaign, _chapters, _gates);
        CampaignState Claim(CampaignState campaign) => Require(
            _recruitment.ClaimEarnedCampaignRecruits094(campaign, _chapters, _gates));
        bool Validate(CampaignState campaign) => CampaignWorldGateCommandService023
            .ValidateStoredCompletionProofs084(campaign, _gates, campaign.Guild.GuildCity
                .Strategic017H.Campaign019.Playable020.WorldGate023);
        static CampaignState Clone(CampaignState campaign) => JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
        static CampaignState Require(Result<CampaignState> result)
        { Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value; }
        static T Field<T>(object fixture, string name) => (T)fixture.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture);
    }
}
