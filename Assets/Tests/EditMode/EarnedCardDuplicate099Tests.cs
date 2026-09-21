using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
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
    public sealed class EarnedCardDuplicate099Tests
    {
        const string RealReceipt099 = "EXPREC089_30C06A7AEA9277FA91150318";
        const string RealHero099 = "HERO_REC_136";
        const string CardPrefix099 = "EARNED_RECRUIT094_CARD|";
        GuildCityRecruitmentService017D _recruitment;
        HeroMaster300Catalog087 _heroes;
        ICampaignRuleCatalog019 _chapters;
        IWorldGateOperationsCatalog023 _gates;
        CampaignState _earned;
        string _fixturePath;

        [OneTimeSetUp]
        public void LoadUnmodifiedActualR103Invitation099()
        {
            var authorities = new ExpeditionRecruitLeadApplicant089Tests();
            authorities.SetUp();
            _recruitment = Field099<GuildCityRecruitmentService017D>(authorities, "_recruitment");
            _heroes = Field099<HeroMaster300Catalog087>(authorities, "_heroes");
            _chapters = new CampaignRuleCatalogAdapter019(CampaignRegistry019.LoadFromResources().Base018);
            _gates = new Campaign023RuleCatalogAdapter(CampaignRegistry023.LoadFromResources());
            _fixturePath = Path.Combine(Application.dataPath, "Tests", "Fixtures", "R103_EarnedCard099.fixture.json");
            var saved = new AtomicSaveStore().ReadWithRecovery(_fixturePath);
            Assert.That(saved.IsSuccess, Is.True, string.Join("; ", saved.Errors));
            _earned = saved.Value.CampaignState;
            Assert.That(CanonicalJson.Sha256Hex(_earned), Is.EqualTo(saved.Value.CanonicalStateHash));
            Assert.That(_earned.Guild.Development.HasAdventureAuthority(RealReceipt099), Is.True);
            Assert.That(_earned.Guild.Development.HasAdventureAuthority(CardPrefix099 + RealReceipt099 +
                "|" + RealHero099 + "|WORLD_GOBLIN_001"), Is.True);
            Assert.That(_heroes.TryGetAcceptedHero(RealHero099, out var hero), Is.True);
            Assert.That(HeroMaster300ApplicantLead089.RosterContains(_earned.Guild.Recruits, hero), Is.False);
            Assert.That(GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(_earned), Is.False);
            Assert.That(ValidProofs099(_earned), Is.True);
        }

        [TestCase(0)]
        [TestCase(10)]
        public void RealEarnedInvitationAcquiredElsewhereGrowsExactOwnerOnce099(int startingAscension)
        {
            var originalBytes = File.ReadAllBytes(_fixturePath);
            var campaign = PaidAcquireExactHero099(Clone099(_earned));
            // A10 is an explicit progression boundary unit fixture, not a claim
            // that this real source player naturally earned ten duplicates.
            campaign = AtAscension099(campaign, startingAscension);
            var owned = Find099(campaign);
            var before = CanonicalJson.Serialize(owned.Progression);
            var capacity = GuildCityRecruitmentService017D.EarnedRecruitCapacity094(campaign.Guild.Development);
            var view = View099(campaign);
            Assert.That(view.PendingCardRecruits, Is.EqualTo(1));
            Assert.That(view.ClaimableDuplicateCards099, Is.EqualTo(1));
            Assert.That(view.CanClaim, Is.True);
            var card = view.Preview.Single(value => value.StableId == RealHero099);
            Assert.That(card.IsDuplicate099, Is.True);
            Assert.That(card.RewardSummary099, Is.Not.Empty);
            var claimed = Claim099(campaign);
            var grown = Find099(claimed);
            Assert.That(grown.RecruitId, Is.EqualTo(owned.RecruitId));
            Assert.That(grown.Progression.AscensionLevel, Is.EqualTo(Math.Min(10, startingAscension + 1)));
            Assert.That(CanonicalJson.Serialize(grown.Progression), Is.Not.EqualTo(before));
            Assert.That(grown.Progression.LearnedArtIds, Is.SupersetOf(owned.Progression.LearnedArtIds));
            Assert.That(claimed.Guild.Recruits.Count,
                Is.EqualTo(campaign.Guild.Recruits.Count + view.ClaimableCount - view.ClaimableDuplicateCards099));
            Assert.That(claimed.Guild.TreasuryXp, Is.EqualTo(campaign.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(claimed.Guild.Inventory), Is.EqualTo(CanonicalJson.Serialize(campaign.Guild.Inventory)));
            Assert.That(CanonicalJson.Serialize(grown.Equipment), Is.EqualTo(CanonicalJson.Serialize(owned.Equipment)));
            Assert.That(GuildCityRecruitmentService017D.EarnedRecruitCapacity094(claimed.Guild.Development),
                Is.EqualTo(capacity + view.ClaimableCount - view.ClaimableDuplicateCards099));
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(claimed.Guild.Development), Is.EqualTo(1));
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateHeroIds099(claimed.Guild.Development).Values,
                Is.EqualTo(new[] { RealHero099 }), "A consumed duplicate remains visible even at a capped growth boundary.");
            Assert.That(View099(claimed).PendingCardRecruits, Is.Zero);
            Assert.That(ValidProofs099(claimed), Is.True, "Existing historical completion proofs must survive legal growth.");
            AssertReplay099(claimed);
            Assert.That(File.ReadAllBytes(_fixturePath), Is.EqualTo(originalBytes));
        }

        [Test]
        public void UnownedRealInvitationStillJoinsOnceRatherThanMerging099()
        {
            var source = Clone099(_earned);
            var preview = View099(source);
            Assert.That(preview.ClaimableDuplicateCards099, Is.Zero);
            var claimed = Claim099(source);
            Assert.That(Find099(claimed).Progression.AscensionLevel, Is.Zero);
            Assert.That(claimed.Guild.Recruits.Count, Is.EqualTo(source.Guild.Recruits.Count + preview.ClaimableCount));
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(claimed.Guild.Development), Is.Zero);
            AssertReplay099(claimed);
        }

        [Test]
        public void TwoIndependentInvitationHostFixtureConsumesEachSourceOnce099()
        {
            // Explicit authority-layer unit fixture only: the first invitation
            // is actual R103 evidence; the second distinct receipt is constructed
            // here. This does NOT claim two live dice wins. Existing opening and
            // ExpeditionDeck094 suites verify the real commit/record boundary.
            const string secondReceipt = "UNIT_ONLY_SECOND_CARD_RECEIPT_099";
            var source = Clone099(_earned);
            var development = source.Guild.Development.RecordAdventureAuthority(secondReceipt)
                .RecordAdventureAuthority(CardPrefix099 + secondReceipt + "|" + RealHero099 + "|WORLD_GOBLIN_001");
            source = WithDevelopment099(source, development);
            var preview = View099(source);
            Assert.That(preview.PendingCardRecruits, Is.EqualTo(2));
            Assert.That(preview.ClaimableDuplicateCards099, Is.EqualTo(1));
            var claimed = Claim099(source);
            Assert.That(claimed.Guild.Recruits.Count(value => value.AuthoredStableRecruitId == RealHero099), Is.EqualTo(1));
            Assert.That(Find099(claimed).Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(claimed.Guild.Recruits.Count, Is.EqualTo(source.Guild.Recruits.Count + preview.ClaimableCount - 1));
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(claimed.Guild.Development), Is.EqualTo(1));
            Assert.That(View099(claimed).PendingCardRecruits, Is.Zero);
            AssertReplay099(claimed);
        }

        [Test]
        public void MissingEarnedReceiptCannotCreateDuplicateReward099()
        {
            var source = PaidAcquireExactHero099(Clone099(_earned));
            var first = Claim099(source);
            var owned = Find099(first);
            const string absentReceipt = "NOT_A_COMMITTED_CARD_099";
            var malformed = WithDevelopment099(first, first.Guild.Development.RecordAdventureAuthority(
                CardPrefix099 + absentReceipt + "|" + RealHero099 + "|WORLD_GOBLIN_001"));
            Assert.That(View099(malformed).PendingCardRecruits, Is.Zero);
            Assert.That(View099(malformed).ClaimableDuplicateCards099, Is.Zero);
            var unchanged = Claim099(malformed);
            Assert.That(CanonicalJson.Serialize(Find099(unchanged).Progression), Is.EqualTo(CanonicalJson.Serialize(owned.Progression)));
            Assert.That(CanonicalJson.Sha256Hex(unchanged), Is.EqualTo(CanonicalJson.Sha256Hex(malformed)));
        }

        [Test]
        public void FullRosterDoesNotBlockAnEarnedDuplicateOrGrantFreeCapacity099()
        {
            var source = PaidAcquireExactHero099(Clone099(_earned));
            // Explicit capacity boundary unit fixture, not 300 claimed heroes.
            var filled = source.Guild.Recruits.Concat(Enumerable.Range(0, 300 - source.Guild.Recruits.Count)
                .Select(index => new RecruitState("UNIT_CAPACITY099_" + index, 100, 100, 20, 20))).ToArray();
            source = source.With(source.Guild.With(source.Guild.TreasuryXp, filled,
                source.Guild.Unions, source.Guild.Inventory, source.Guild.Development), source.OpeningFlow);
            var beforeCapacity = GuildCityRecruitmentService017D.EarnedRecruitCapacity094(source.Guild.Development);
            var preview = View099(source);
            Assert.That(preview.ClaimableCount, Is.EqualTo(1));
            Assert.That(preview.ClaimableDuplicateCards099, Is.EqualTo(1));
            var claimed = Claim099(source);
            Assert.That(claimed.Guild.Recruits.Count, Is.EqualTo(300));
            Assert.That(Find099(claimed).Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(GuildCityRecruitmentService017D.EarnedRecruitCapacity094(claimed.Guild.Development), Is.EqualTo(beforeCapacity));
            Assert.That(View099(claimed).PendingChapterRecruits, Is.EqualTo(preview.PendingChapterRecruits));
            AssertReplay099(claimed);
        }

        [Test]
        public void FullLedgerDuplicateClaimIsAtomicAndNeverConsumesInvitation099()
        {
            var source = PaidAcquireExactHero099(Clone099(_earned));
            var development = source.Guild.Development;
            var entries = development.AppliedAdventureAuthorityIds.Concat(Enumerable.Range(0,
                GuildDevelopmentState.AdventureAuthorityEntryLimit - development.AppliedAdventureAuthorityIds.Count - 1)
                .Select(value => "UNIT_LEDGER_FILL099_" + value)).ToArray();
            var saturated = new GuildDevelopmentState(development.HallStageIndex, development.HallStageId,
                development.HallEnhancementXp, development.LifetimeTreasuryXpEarned,
                development.Facilities, development.ClaimedBattleRewardIds, entries);
            source = WithDevelopment099(source, saturated);
            var hash = CanonicalJson.Sha256Hex(source);
            var rejected = _recruitment.ClaimEarnedCampaignRecruits094(source, _chapters, _gates);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain("CAMPAIGN_RECRUIT094_LEDGER_FULL"));
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(hash));
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(source.Guild.Development), Is.Zero);
        }

        CampaignState PaidAcquireExactHero099(CampaignState source)
        {
            Assert.That(_heroes.TryGetAcceptedHero(RealHero099, out var hero), Is.True);
            var applicant = HeroMaster300ApplicantLead089.ToApplicant(hero, 1, "WORLD_GOBLIN_001");
            // Explicit applicant-board and affordable-wallet UNIT setup. The
            // original R103 file/earned receipt and housing remain untouched.
            var board = new ApplicantBoardState("UNIT_PAID_CARD099", "UNIT_PAID_CARD099", 0,
                true, new[] { applicant }, null);
            var prepared = source.With(source.Guild.WithGuildCity(source.Guild.GuildCity.With(
                recruitmentBoard: board, replaceRecruitmentBoard: true)), source.OpeningFlow);
            var price = _recruitment.DescribeApplicantSigning090(prepared, applicant).EffectiveCostTreasuryXp;
            Assert.That(price, Is.GreaterThan(0), "No earned invitation may zero-price an ordinary desk offer.");
            prepared = prepared.With(prepared.Guild.With(Math.Max(prepared.Guild.TreasuryXp, price),
                prepared.Guild.Recruits, prepared.Guild.Unions, prepared.Guild.Inventory,
                prepared.Guild.Development), prepared.OpeningFlow);
            var signed = Require099(_recruitment.SignApplicant(prepared, applicant.RecruitId));
            Assert.That(signed.Guild.TreasuryXp, Is.EqualTo(prepared.Guild.TreasuryXp - price));
            Assert.That(View099(signed).PendingCardRecruits, Is.EqualTo(1), "The paid acquisition must not consume the earned invitation.");
            return signed;
        }

        void AssertReplay099(CampaignState claimed)
        {
            var serialized = CanonicalJson.Serialize(claimed);
            var reload = Clone099(claimed);
            Assert.That(CanonicalJson.Serialize(reload), Is.EqualTo(serialized));
            Assert.That(CanonicalJson.Serialize(Claim099(reload)), Is.EqualTo(serialized));
            Assert.That(CanonicalJson.Serialize(Claim099(claimed)), Is.EqualTo(serialized));
        }
        static CampaignState AtAscension099(CampaignState campaign, int rank)
        {
            var owned = Find099(campaign);
            var progression = owned.Progression;
            while (progression.AscensionLevel < rank) progression = progression.Ascend089();
            return campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits.Select(value => value.RecruitId == owned.RecruitId
                    ? value.WithProgression(progression) : value).ToArray(), campaign.Guild.Unions,
                campaign.Guild.Inventory, campaign.Guild.Development), campaign.OpeningFlow);
        }
        static RecruitState Find099(CampaignState campaign) => campaign.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == RealHero099);
        EarnedCampaignRecruitsView094 View099(CampaignState campaign) => _recruitment.DescribeEarnedCampaignRecruits094(campaign, _chapters, _gates);
        CampaignState Claim099(CampaignState campaign) => Require099(_recruitment.ClaimEarnedCampaignRecruits094(campaign, _chapters, _gates));
        bool ValidProofs099(CampaignState campaign) => CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
            campaign, _gates, campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023);
        static CampaignState WithDevelopment099(CampaignState campaign, GuildDevelopmentState development) => campaign.With(
            campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits, campaign.Guild.Unions, campaign.Guild.Inventory, development), campaign.OpeningFlow);
        static CampaignState Clone099(CampaignState campaign) => JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
        static CampaignState Require099(Result<CampaignState> result)
        { Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value; }
        static T Field099<T>(object fixture, string field) => (T)fixture.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fixture);
    }
}
