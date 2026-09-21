#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class AutomaticEarnedAscension136Tests
    {
        EarnedCardDuplicate099Tests _real;
        GuildCityRecruitmentService017D _service;
        ICampaignRuleCatalog019 _chapters;
        IWorldGateOperationsCatalog023 _gates;
        CampaignState _earned;
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [OneTimeSetUp]
        public void LoadExistingRealEarnedFixture()
        {
            _real = new EarnedCardDuplicate099Tests();
            _real.LoadUnmodifiedActualR103Invitation099();
            _service = Field<GuildCityRecruitmentService017D>("_recruitment");
            _chapters = Field<ICampaignRuleCatalog019>("_chapters");
            _gates = Field<IWorldGateOperationsCatalog023>("_gates");
            _earned = Field<CampaignState>("_earned");
        }

        [TestCase(0)]
        [TestCase(10)]
        public void ActualUnclaimedCardWithSeparatelyAcquiredOwnerAutomaticallyGrowsOnce(int rank)
        {
            // Existing R103 earned receipt stays real. Its old test's paid
            // acquisition and A10 setup are explicit UNIT boundary fixtures.
            var owned = Invoke("PaidAcquireExactHero099", Clone(_earned));
            owned = Invoke("AtAscension099", owned, rank);
            var before = owned.Guild.Recruits.Single(r => r.AuthoredStableRecruitId == "HERO_REC_136");
            var next = Auto(owned);
            var after = next.Guild.Recruits.Single(r => r.RecruitId == before.RecruitId);
            Assert.That(after.Progression.AscensionLevel, Is.EqualTo(Math.Min(10, rank + 1)));
            Assert.That(CanonicalJson.Serialize(after.Progression), Is.Not.EqualTo(CanonicalJson.Serialize(before.Progression)),
                "A10 must retain the existing tree/Art/veteran overflow value.");
            Assert.That(next.Guild.Recruits.Count, Is.EqualTo(owned.Guild.Recruits.Count));
            Assert.That(next.Guild.TreasuryXp, Is.EqualTo(owned.Guild.TreasuryXp));
            Assert.That(EquipmentAndParty(next), Is.EqualTo(EquipmentAndParty(owned)));
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(next.Guild.Development), Is.EqualTo(1));
            if (rank == 0) Assert.That(after.Progression.TotalPersonalXp, Is.EqualTo(before.Progression.TotalPersonalXp));
            AssertReplay(next);
        }

        [Test]
        public void UnownedEarnedNewHeroAndPaidInvitationAreNotAutomaticallyPurchased()
        {
            Assert.That(CanonicalJson.Sha256Hex(Auto(_earned)), Is.EqualTo(CanonicalJson.Sha256Hex(_earned)));
            var paid = Invoke("PaidAcquireExactHero099", Clone(_earned));
            // Remove the free right in a UNIT boundary fixture. The owned hero,
            // desk offer, lead and original committed dice receipt cannot alone
            // manufacture another acquisition or authorize an automatic charge.
            var development = paid.Guild.Development;
            var noFreeRight = new GuildDevelopmentState(development.HallStageIndex, development.HallStageId,
                development.HallEnhancementXp, development.LifetimeTreasuryXpEarned, development.Facilities,
                development.ClaimedBattleRewardIds, development.AppliedAdventureAuthorityIds
                    .Where(id => !id.StartsWith("EARNED_RECRUIT094_CARD|", StringComparison.Ordinal)).ToArray());
            paid = WithDevelopment(paid, noFreeRight);
            Assert.That(CanonicalJson.Sha256Hex(Auto(paid)), Is.EqualTo(CanonicalJson.Sha256Hex(paid)));
        }

        [Test]
        public void RealQuest62SettlesOnly34OwedChapterSlotsNot60AlreadyClaimedNewHeroCards()
        {
            var source = ReadQuest62();
            Assert.That(source.Guild.Recruits.Count, Is.EqualTo(241));
            Assert.That(source.Guild.Recruits.Sum(r => r.Progression.AscensionLevel), Is.Zero);
            var before = _service.DescribeEarnedCampaignRecruits094(source, _chapters, _gates);
            Assert.That(before.PendingChapterRecruits, Is.EqualTo(34));
            Assert.That(before.BlockedChapterRecruits, Is.Zero);
            Assert.That(before.PendingCardRecruits, Is.Zero);
            var identities = source.Guild.Development.AppliedAdventureAuthorityIds
                .Where(id => id.StartsWith("EARNED_RECRUIT094_HERO|", StringComparison.Ordinal)).ToArray();
            var next = Auto(source);
            Assert.That(next.Guild.Recruits.Count, Is.EqualTo(241));
            Assert.That(next.Guild.Recruits.Sum(r => r.Progression.AscensionLevel), Is.EqualTo(34));
            Assert.That(next.Guild.TreasuryXp, Is.EqualTo(source.Guild.TreasuryXp));
            Assert.That(EquipmentAndParty(next), Is.EqualTo(EquipmentAndParty(source)));
            Assert.That(next.Guild.Recruits.Select(r => r.Progression.TotalPersonalXp),
                Is.EqualTo(source.Guild.Recruits.Select(r => r.Progression.TotalPersonalXp)));
            Assert.That(next.Guild.Development.AppliedAdventureAuthorityIds
                .Where(id => id.StartsWith("EARNED_RECRUIT094_HERO|", StringComparison.Ordinal)), Is.EqualTo(identities));
            Assert.That(_service.DescribeEarnedCampaignRecruits094(next, _chapters, _gates).PendingCount, Is.Zero);
            Assert.That(GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(next.Guild.Development), Is.EqualTo(34));
            AssertReplay(next);
        }

        [Test]
        public void SaturatedLedgerLeavesRealDuplicatePendingWithoutPartialGrowth()
        {
            var source = Invoke("PaidAcquireExactHero099", Clone(_earned));
            var d = source.Guild.Development;
            var entries = d.AppliedAdventureAuthorityIds.Concat(Enumerable.Range(0,
                GuildDevelopmentState.AdventureAuthorityEntryLimit - d.AppliedAdventureAuthorityIds.Count - 1)
                .Select(i => "UNIT_AUTO136_CAPACITY_" + i)).ToArray();
            source = WithDevelopment(source, new GuildDevelopmentState(d.HallStageIndex, d.HallStageId,
                d.HallEnhancementXp, d.LifetimeTreasuryXpEarned, d.Facilities, d.ClaimedBattleRewardIds, entries));
            var hash = CanonicalJson.Sha256Hex(source);
            var blocked = _service.ApplyAutomaticEarnedDuplicates136(source, _chapters, _gates);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(hash));
        }

        [Test]
        public void RealChestSelectionRequiresCommittedRewardThenOwnedCreditAutomaticallyGrowsOnce()
        {
            var source = Auto(ReadQuest62());
            string card = null;
            SecondDimension.Gameplay.Recruitment.HeroMaster300Hero087 hero = null;
            for (var i = 0; i < 4096 && hero == null; i++)
            {
                card = "UNIT_CHEST_AUTO136_" + i;
                hero = _service.PreviewChestRecruit092(source, card);
            }
            Assert.That(hero, Is.Not.Null, "Bounded normal deterministic chest selection needs a real S-rank result.");
            var owned = source.Guild.Recruits.Single(r => r.AuthoredStableRecruitId == hero.StableId);
            Assert.That(_service.ApplyChestRecruit092(source, card), Is.SameAs(source), "A preview is not an acquisition.");
            // Same explicit committed-receipt UNIT setup as ChestRecruitRewards092Tests.
            var credited = _service.ApplyChestRecruit092(WithDevelopment(source, source.Guild.Development
                .RecordAdventureAuthority(GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + card)), card);
            Assert.That(credited.Guild.Recruits.Single(r => r.RecruitId == owned.RecruitId).Progression.AscensionLevel,
                Is.EqualTo(owned.Progression.AscensionLevel), "Credit preparation must not mutate an active character.");
            var next = Auto(credited);
            Assert.That(next.Guild.Recruits.Single(r => r.RecruitId == owned.RecruitId).Progression.AscensionLevel,
                Is.EqualTo(owned.Progression.AscensionLevel + 1));
            Assert.That(next.Guild.TreasuryXp, Is.EqualTo(source.Guild.TreasuryXp));
            Assert.That(EquipmentAndParty(next), Is.EqualTo(EquipmentAndParty(source)));
            Assert.That(next.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ExpeditionRecruitLeadIds089,
                Does.Not.Contain(hero.StableId));
            AssertReplay(next);
        }

        CampaignState ReadQuest62()
        {
            var fixture = Path.Combine(Application.dataPath, "Tests", "Fixtures", "R430_CH062_AutoAscension136.fixture.json.gz");
            var temp = Path.Combine(Path.GetTempPath(), "SD_AUTO136_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                using (var source = File.OpenRead(fixture)) using (var zip = new GZipStream(source, CompressionMode.Decompress))
                using (var target = File.Create(temp)) zip.CopyTo(target);
                var loaded = new AtomicSaveStore().ReadWithRecovery(temp);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("; ", loaded.Errors));
                Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState), Is.EqualTo(loaded.Value.CanonicalStateHash));
                return loaded.Value.CampaignState;
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        CampaignState Auto(CampaignState source)
        {
            var result = _service.ApplyAutomaticEarnedDuplicates136(source, _chapters, _gates);
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value;
        }
        void AssertReplay(CampaignState source)
        {
            var hash = CanonicalJson.Sha256Hex(source);
            Assert.That(CanonicalJson.Sha256Hex(Auto(Clone(source))), Is.EqualTo(hash));
            Assert.That(CanonicalJson.Sha256Hex(Auto(source)), Is.EqualTo(hash));
        }
        static string EquipmentAndParty(CampaignState c) => CanonicalJson.Sha256Hex(new {
            c.Guild.Unions, c.Guild.Inventory, Equipment = c.Guild.Recruits.Select(r => new { r.RecruitId, r.Equipment }).ToArray() });
        static CampaignState WithDevelopment(CampaignState c, GuildDevelopmentState d) =>
            c.With(c.Guild.With(c.Guild.TreasuryXp, c.Guild.Recruits, c.Guild.Unions, c.Guild.Inventory, d), c.OpeningFlow);
        CampaignState Invoke(string name, params object[] args) => (CampaignState)typeof(EarnedCardDuplicate099Tests).GetMethod(name, Private).Invoke(_real, args);
        T Field<T>(string name) => (T)typeof(EarnedCardDuplicate099Tests).GetField(name, Private).GetValue(_real);
        static CampaignState Clone(CampaignState c) => JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(c));
    }
}
#endif
