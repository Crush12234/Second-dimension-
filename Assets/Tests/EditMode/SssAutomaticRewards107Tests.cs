using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SssAutomaticRewards107Tests
    {
        [Test]
        public void AllTenHeroesAscendFromCodesWithoutInventoryInteraction107()
        {
            var codes = new SssTenV4CreatorCommand090();
            var campaign = Campaign();
            foreach (var hero in SssTenV4Roster090.All)
            {
                campaign = Require(codes.Redeem(campaign, hero.RecruitCode));
                var arts = SssTenV4Roster090.FindOwned(campaign.Guild.Recruits,
                    hero.HeroId).Progression.LearnedArtIds.ToArray();
                var rank = 0;
                foreach (var definition in SssSupplementalCodes.Definitions().Where(x =>
                    x.heroId == hero.HeroId && x.kind == SssSupplementKind.HeroBoundAscensionCredit))
                {
                    campaign = Require(SssAutomaticRewards107.ApplyReady(
                        Require(codes.Redeem(campaign, definition.code))));
                    Assert.That(SssTenV4Roster090.FindOwned(campaign.Guild.Recruits,
                        hero.HeroId).Progression.AscensionLevel, Is.EqualTo(++rank));
                    Assert.That(codes.Redeem(campaign, definition.code).IsSuccess, Is.False);
                }
                CollectionAssert.AreEqual(arts, SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, hero.HeroId).Progression.LearnedArtIds);
                Assert.That(SssTenV4Inventory090.AscensionCreditCount(campaign, hero.HeroId), Is.Zero);
            }
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(campaign)), Is.SameAs(campaign));
        }

        [Test]
        public void PreviouslyRedeemedCreditsSettleOnceAndSurviveReload107()
        {
            var codes = new SssTenV4CreatorCommand090();
            var campaign = Require(codes.Redeem(Campaign(), "SDGOW-SSS-PACT"));
            for (var index = 1; index <= 10; index++)
                campaign = Require(codes.Redeem(campaign, "SDGOW-SSS-PACT-ASCEND-" + index.ToString("00")));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(campaign, "SSS_ELYSIA_NIGHTCALL"), Is.EqualTo(10));
            campaign = Require(SssAutomaticRewards107.ApplyReady(campaign));
            var loaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            Assert.That(SssTenV4Roster090.FindOwned(loaded.Guild.Recruits, "SSS_ELYSIA_NIGHTCALL")
                .Progression.AscensionLevel, Is.EqualTo(10));
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(loaded)), Is.SameAs(loaded));
            var extra = Require(SssTenV4HostRewards090.GrantAscensionCredit(loaded,
                "SSS_ELYSIA_NIGHTCALL", "EXTRA_NATURAL107"));
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(extra)), Is.SameAs(extra));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(extra, "SSS_ELYSIA_NIGHTCALL"), Is.EqualTo(1));
        }

        [Test]
        public void SignatureRewardsWaitForManualEquipAndPreserveLaterUnequip121()
        {
            var codes = new SssTenV4CreatorCommand090();
            var campaign = Campaign();
            foreach (var hero in SssTenV4Roster090.All)
            {
                campaign = Require(codes.Redeem(campaign, hero.RecruitCode));
                campaign = Require(codes.Redeem(campaign, hero.RecruitCode + "-WEAPON"));
            }
            var opening = campaign.OpeningFlow;
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(campaign)), Is.SameAs(campaign),
                "Reward preparation cannot choose equipment for existing heroes, including empty slots.");
            Assert.That(campaign.OpeningFlow, Is.SameAs(opening));
            foreach (var hero in SssTenV4Roster090.All)
            {
                var owner = SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, hero.HeroId);
                Assert.That(owner.Equipment.Assignments, Is.Empty);
                var signature = campaign.Guild.Inventory.Single(item => item.DefinitionId == hero.WeaponItemId);
                campaign = Require(new M1CommandService().EquipItem(campaign, owner.RecruitId,
                    signature.ValidSlotIds.First(), signature.InstanceId));
                owner = SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, hero.HeroId);
                var assignment = owner.Equipment.Assignments.Single(x => x.Item.DefinitionId == hero.WeaponItemId);
                var power = M2EquipmentPowerPolicy087.Resolve(owner.Equipment);
                Assert.That(power.PhysicalAttack + power.MysticAttack, Is.GreaterThanOrEqualTo(24), hero.HeroId);
                Assert.That(SssBattleIntegration090.HasSignatureWeaponEffectHandler090(hero.HeroId), Is.True);
                Assert.That(campaign.Guild.Inventory.Any(x => x.InstanceId == assignment.Item.InstanceId), Is.False);
                campaign = Require(new M1CommandService().UnequipItem(campaign, owner.RecruitId, assignment.SlotId));
                Assert.That(Require(SssAutomaticRewards107.ApplyReady(campaign)), Is.SameAs(campaign),
                    "Reward preparation must preserve the subsequent player choice.");
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ExistingSssGearAndSignatureOwnershipSurviveRewardAscensionAndReload121(bool equipmentLocked, bool signatureLocked)
        {
            const string heroId = "SSS_RYLEN_STONEBOND";
            var campaign = Require(SssTenV4HostRewards090.GrantRecruit(Campaign(), heroId, "NEW_HERO121"));
            var chosen = new EquipmentItemState("PLAYER_STAFF121", "BASIC_094_WEAPON_FAMILY_STAFF", "Chosen Staff",
                new[] { EquipmentSlotIds.MainHand }, new[] { "STAFF", "WEAPON", "WEAPON_FAMILY_STAFF" }, "QUALITY_STARTER", 10000, false);
            campaign = campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory.Concat(new[] { chosen }).ToArray()), campaign.OpeningFlow);
            var commands = new M1CommandService();
            campaign = Require(commands.EquipItem(campaign, heroId, EquipmentSlotIds.MainHand, chosen.InstanceId));
            if (equipmentLocked) campaign = Require(commands.SetEquipmentLock(campaign, heroId, EquipmentSlotIds.MainHand, true));
            campaign = Require(SssTenV4HostRewards090.GrantSignatureWeapon(campaign, heroId, "EARNED_SIGNATURE121"));
            campaign = Require(SssTenV4HostRewards090.GrantAscensionCredit(campaign, heroId, "EARNED_CREDIT121"));
            var definition = SssTenV4Roster090.Get(heroId);
            campaign = campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory.Select(item => item.DefinitionId == definition.WeaponItemId
                    ? item.WithPlayerLock(signatureLocked) : item).ToArray()), campaign.OpeningFlow);
            var prior = SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, heroId);
            var gear = CanonicalJson.Serialize(prior.Equipment);
            var inventory = CanonicalJson.Serialize(campaign.Guild.Inventory.Where(item =>
                item.QualityId != SssTenV4Inventory090.CreditQualityId).ToArray());
            var result = Require(SssAutomaticRewards107.ApplyReady(campaign));
            var grown = SssTenV4Roster090.FindOwned(result.Guild.Recruits, heroId);
            Assert.That(grown.RecruitId, Is.EqualTo(prior.RecruitId));
            Assert.That(grown.Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(grown.Progression.Level, Is.EqualTo(prior.Progression.Level));
            Assert.That(grown.Progression.TotalPersonalXp, Is.EqualTo(prior.Progression.TotalPersonalXp));
            Assert.That(grown.Progression.LearnedArtIds, Is.EqualTo(prior.Progression.LearnedArtIds));
            Assert.That(CanonicalJson.Serialize(grown.Progression.ArtMastery), Is.EqualTo(CanonicalJson.Serialize(prior.Progression.ArtMastery)));
            Assert.That(CanonicalJson.Serialize(grown.Equipment), Is.EqualTo(gear));
            Assert.That(CanonicalJson.Serialize(result.Guild.Inventory), Is.EqualTo(inventory));
            Assert.That(result.Guild.TreasuryXp, Is.EqualTo(campaign.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(result.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(campaign.Guild.Unions)));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(result, heroId), Is.Zero);
            Assert.That(SssTenV4CampaignAccessor090.Read(result).SpecialUseReceiptIds,
                Is.SupersetOf(SssTenV4CampaignAccessor090.Read(campaign).SpecialUseReceiptIds));
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(result));
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(reloaded)), Is.SameAs(reloaded));
            Assert.That(CanonicalJson.Serialize(SssTenV4Roster090.FindOwned(reloaded.Guild.Recruits, heroId).Equipment), Is.EqualTo(gear));
            Assert.That(CanonicalJson.Serialize(reloaded.Guild.Inventory), Is.EqualTo(inventory));
        }

        [TestCase(BattleOutcome.InProgress)]
        [TestCase(BattleOutcome.Victory)]
        public void BattleAndUnclaimedResultDeferItemsUntilPreparation107(BattleOutcome outcome)
        {
            var codes = new SssTenV4CreatorCommand090();
            var campaign = Require(codes.Redeem(Campaign(), "SDGOW-SSS-BOND"));
            campaign = Require(codes.Redeem(campaign, "SDGOW-SSS-BOND-ASCEND-01"));
            campaign = Require(codes.Redeem(campaign, "SDGOW-SSS-BOND-WEAPON"));
            var battle = new BattleState("TEST_BATTLE107", "107", 1,
                outcome == BattleOutcome.InProgress ? BattlePhase.ForecastSelection : BattlePhase.Resolved,
                outcome, "Fixture only", null, null, null, null, null, null,
                "", "", "", "", "", false);
            campaign = campaign.WithBattle(battle);
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(campaign)), Is.SameAs(campaign));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(campaign, "SSS_RYLEN_STONEBOND"), Is.EqualTo(1));
            var ready = Require(SssAutomaticRewards107.ApplyReady(campaign.WithBattle(null)));
            var hero = SssTenV4Roster090.FindOwned(ready.Guild.Recruits, "SSS_RYLEN_STONEBOND");
            Assert.That(hero.Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(hero.Equipment.Assignments, Is.Empty);
            Assert.That(ready.Guild.Inventory.Count(item => item.DefinitionId ==
                SssTenV4Roster090.Get(hero.RecruitId).WeaponItemId), Is.EqualTo(1),
                "Preparing earned ascension leaves the signature reward in inventory.");
        }

        [Test]
        public void LegacyCampaignWithoutOpeningProfileCanKeepSignatureWithoutBlockingSave107()
        {
            var codes = new SssTenV4CreatorCommand090();
            var campaign = Require(codes.Redeem(CampaignFactory.CreateM0Proof(107), "SDGOW-SSS-BOND"));
            campaign = Require(codes.Redeem(campaign, "SDGOW-SSS-BOND-WEAPON"));
            Assert.That(Require(SssAutomaticRewards107.ApplyReady(campaign)), Is.SameAs(campaign));
            Assert.That(campaign.Guild.Inventory.Count, Is.EqualTo(1));
        }

        private static CampaignState Campaign() => Require(new M1CommandService().CreateNewGuild(
            new NewGuildCommand("00000000-0000-0000-0000-000000000107", 107L, "SSS107", "GUILD107",
                new NewGuildProfileState("Test", GameMode.Standard, TutorialDepth.FullTutorial,
                    AccessibilitySettingsState.Defaults(), false))));
        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join(" | ", result.Errors));
            return result.Value;
        }
    }
}
