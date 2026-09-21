using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ChestRecruitRewards092Tests
    {
        private ExpeditionRecruitLeadApplicant089Tests _fixture;
        private GuildCityRecruitmentService017D _recruitment;
        private RecruitmentContent _recruitmentContent;
        private GuildCityContent017D _cityContent;
        private HeroMaster300Catalog087 _heroes;

        [OneTimeSetUp]
        public void LoadExistingProductionFixture()
        {
            // Reuse the existing accepted-HeroMaster + real generator/trees setup,
            // not a mock applicant board or a second recruitment implementation.
            _fixture = new ExpeditionRecruitLeadApplicant089Tests();
            _fixture.SetUp();
            _recruitment = Field<GuildCityRecruitmentService017D>("_recruitment");
            _recruitmentContent = Field<RecruitmentContent>("_recruitmentContent");
            _cityContent = Field<GuildCityContent017D>("_cityContent");
            _heroes = Field<HeroMaster300Catalog087>("_heroes");
        }

        [Test]
        public void ChestInvitationPreviewIsDeterministicRareAcceptedSOnlyAndNeverMutatesState()
        {
            var campaign = Create();
            var before = CanonicalJson.Sha256Hex(campaign);
            var hits = 0;
            for (var index = 0; index < 1024; index++)
            {
                var id = "CHEST_RARITY_092_" + index;
                var hero = _recruitment.PreviewChestRecruit092(campaign, id);
                Assert.That(_recruitment.PreviewChestRecruit092(campaign, id)?.StableId, Is.EqualTo(hero?.StableId));
                if (hero == null) continue;
                hits++;
                Assert.That(hero.Rank, Is.EqualTo(HeroMasterRank087.S));
                Assert.That(hero.IsNormalApplicantEligible, Is.True);
                Assert.That(_heroes.TryGetAcceptedHero(hero.StableId, out _), Is.True);
            }
            Assert.That(hits, Is.InRange(1, 65), "The deterministic sample must demonstrate a rare invitation, not all chests.");
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(before));
        }

        [Test]
        public void ReceiptRequiredThenOneExactLeadSignsFreeOnActualBoardAndReplayGrantsNothing()
        {
            var campaign = Create();
            var cursor = 0;
            var card = FindCard(campaign, ref cursor, null, out var hero);
            Assert.That(_recruitment.ApplyChestRecruit092(campaign, card), Is.SameAs(campaign),
                "A preview is not an earned chest; the normal Quest090 receipt must already exist.");
            Assert.That(World(campaign).ExpeditionRecruitLeadIds089, Does.Not.Contain(hero.StableId));
            var beforeXp = campaign.Guild.TreasuryXp;
            campaign = _recruitment.ApplyChestRecruit092(WithQuestReceipt(campaign, card), card);
            Assert.That(World(campaign).ExpeditionRecruitLeadIds089.Count(id => id == hero.StableId), Is.EqualTo(1));
            Assert.That(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count(id =>
                id == GuildCityRecruitmentService017D.ChestRecruitReceiptPrefix092 + card), Is.EqualTo(1));
            Assert.That(campaign.Guild.Recruits.Any(value => value.AuthoredStableRecruitId == hero.StableId), Is.False,
                "An invitation uses the existing recruitment desk; it must not directly inject a recruit.");
            Assert.That(_recruitment.ApplyChestRecruit092(campaign, card), Is.SameAs(campaign));
            campaign = Reload(campaign);
            campaign = Require(_recruitment.CommitBoard(campaign, _recruitmentContent, _cityContent));
            var offer = campaign.Guild.GuildCity.RecruitmentBoard.Applicants.Single(value => value.AuthoredStableRecruitId == hero.StableId);
            Assert.That(offer.DisplayName, Is.EqualTo(hero.Name));
            var preview = _recruitment.DescribeApplicantSigning090(campaign, offer);
            Assert.That(preview.CanSign, Is.True);
            Assert.That(preview.EffectiveCostTreasuryXp, Is.Zero);
            campaign = Require(_recruitment.SignApplicant(campaign, offer.RecruitId));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(beforeXp));
            Assert.That(campaign.Guild.Recruits.Count(value => value.AuthoredStableRecruitId == hero.StableId), Is.EqualTo(1));
            Assert.That(World(campaign).ExpeditionRecruitLeadIds089, Does.Not.Contain(hero.StableId));
            var hash = CanonicalJson.Sha256Hex(campaign);
            var replay = Require(_recruitment.SignApplicant(Reload(campaign), offer.RecruitId));
            Assert.That(CanonicalJson.Sha256Hex(replay), Is.EqualTo(hash));
            Assert.That(_recruitment.ApplyChestRecruit092(replay, card), Is.SameAs(replay));
        }

        [Test]
        public void TenAdditionalEarnedChestInvitationsUseExistingAscensionTenWithoutRosterOrXpDuplication()
        {
            var campaign = Create();
            var cursor = 0;
            var firstCard = FindCard(campaign, ref cursor, null, out var hero);
            campaign = _recruitment.ApplyChestRecruit092(WithQuestReceipt(campaign, firstCard), firstCard);
            campaign = Require(_recruitment.CommitBoard(campaign, _recruitmentContent, _cityContent));
            var offer = campaign.Guild.GuildCity.RecruitmentBoard.Applicants.Single(value => value.AuthoredStableRecruitId == hero.StableId);
            campaign = Require(_recruitment.SignApplicant(campaign, offer.RecruitId));
            var count = campaign.Guild.Recruits.Count;
            var treasury = campaign.Guild.TreasuryXp;
            Assert.That(RecruitAscensionRules089.MaximumLevel, Is.EqualTo(10));
            for (var ascension = 1; ascension <= RecruitAscensionRules089.MaximumLevel; ascension++)
            {
                var card = FindCard(campaign, ref cursor, hero.StableId, out _);
                campaign = _recruitment.ApplyChestRecruit092(WithQuestReceipt(campaign, card), card);
                campaign = Reload(campaign);
                var preview = _recruitment.DescribeApplicantSigning090(campaign, offer);
                Assert.That(preview.CanSign, Is.True);
                Assert.That(preview.EffectiveCostTreasuryXp, Is.Zero);
                campaign = Require(_recruitment.SignApplicant(campaign, offer.RecruitId));
                Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(count));
                Assert.That(campaign.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == hero.StableId)
                    .Progression.AscensionLevel, Is.EqualTo(ascension));
                Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasury));
                var replay = Require(_recruitment.SignApplicant(campaign, offer.RecruitId));
                Assert.That(CanonicalJson.Sha256Hex(replay), Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
            }
        }

        private T Field<T>(string name) => (T)typeof(ExpeditionRecruitLeadApplicant089Tests)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_fixture);
        private CampaignState Create() => (CampaignState)typeof(ExpeditionRecruitLeadApplicant089Tests)
            .GetMethod("CreateAtWorldBoard", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_fixture, new object[] { 920907L });
        private string FindCard(CampaignState campaign, ref int cursor, string exactHeroId, out HeroMaster300Hero087 hero)
        {
            var stop = cursor + 32768;
            while (cursor < stop)
            {
                var id = "CHEST_CREDIT_092_" + cursor++;
                hero = _recruitment.PreviewChestRecruit092(campaign, id);
                if (hero != null && (exactHeroId == null || hero.StableId == exactHeroId)) return id;
            }
            Assert.Fail("The bounded deterministic chest sample did not contain the requested accepted S hero.");
            hero = null;
            return null;
        }
        private static CampaignState WithQuestReceipt(CampaignState campaign, string card) => campaign.With(
            campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits, campaign.Guild.Unions, campaign.Guild.Inventory,
                campaign.Guild.Development.RecordAdventureAuthority(GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + card)), campaign.OpeningFlow);
        private static WorldGateRuntimeState023 World(CampaignState campaign) => campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023;
        private static CampaignState Reload(CampaignState campaign) => JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
        private static CampaignState Require(Result<CampaignState> result)
        { Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors)); return result.Value; }
    }
}
