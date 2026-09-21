using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SssTenV4RosterInventory090Tests
    {
        [Test]
        public void RegistryOwnsExactTenHeroesAndOneHundredTwentyCodes090()
        {
            var service = new SssTenV4CreatorCommand090();
            Assert.That(SssTenV4Roster090.All.Count, Is.EqualTo(10));
            CollectionAssert.AreEqual(SssHeroes.All,
                SssTenV4Roster090.All.Select(x => x.HeroId).ToArray());
            Assert.That(service.CodeCount, Is.EqualTo(120));
            Assert.That(SssTenV4Roster090.Get("SS_RYLEN_STONEBOND").HeroId,
                Is.EqualTo("SSS_RYLEN_STONEBOND"));
            Assert.That(SssTenV4Roster090.Get("HERO_SS_ELYSIA_NIGHTCALL").HeroId,
                Is.EqualTo("SSS_ELYSIA_NIGHTCALL"));
            Assert.That(SssTenV4Roster090.Get("SS_VAELIS_MANYFORM").HeroId,
                Is.EqualTo("SSS_VAELIS_MANYFORM"));
        }

        [Test]
        public void AllOneHundredTenSupplementalCodesRouteToBoundInventory090()
        {
            var supplemental = SssSupplementalCodes.Definitions();
            Assert.That(supplemental.Count, Is.EqualTo(110));
            Assert.That(supplemental.Count(value =>
                    value.kind == SssSupplementKind.HeroBoundAscensionCredit),
                Is.EqualTo(100));
            Assert.That(supplemental.Count(value =>
                    value.kind != SssSupplementKind.HeroBoundAscensionCredit),
                Is.EqualTo(10));

            var campaign = Campaign090();
            var service = new SssTenV4CreatorCommand090();
            foreach (var hero in SssTenV4Roster090.All)
            {
                campaign = Require(service.Redeem(campaign, hero.RecruitCode));
                var heroCodes = supplemental.Where(value =>
                    StringComparer.Ordinal.Equals(value.heroId, hero.HeroId)).ToArray();
                Assert.That(heroCodes.Count(value =>
                        value.kind == SssSupplementKind.HeroBoundAscensionCredit),
                    Is.EqualTo(10), hero.HeroId);
                Assert.That(heroCodes.Count(value =>
                        value.kind != SssSupplementKind.HeroBoundAscensionCredit),
                    Is.EqualTo(1), hero.HeroId);
                foreach (var definition in heroCodes)
                {
                    Assert.That(service.TryPreview(campaign, definition.code,
                        out var preview), Is.True, definition.code);
                    Assert.That(preview.CanRedeem, Is.True, definition.code);
                    campaign = Require(service.Redeem(campaign, definition.code));
                }

                Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                    campaign, hero.HeroId), Is.EqualTo(10), hero.HeroId);
                var signature = campaign.Guild.Inventory.Single(item =>
                    StringComparer.Ordinal.Equals(item.DefinitionId,
                        hero.WeaponItemId));
                var owner = SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, hero.HeroId);
                Assert.That(owner.Equipment.Assignments, Is.Empty,
                    "Codes must never auto-equip items.");
                Assert.That(SssTenV4Inventory090.CanEquip(owner, signature,
                    signature.ValidSlotIds[0], out _), Is.True, hero.HeroId);
            }

            Assert.That(campaign.Guild.Inventory.Count, Is.EqualTo(110));
            Assert.That(campaign.Guild.Inventory.Count(item => item.InventoryOnly),
                Is.EqualTo(100));
            Assert.That(campaign.Guild.Inventory.Count(item =>
                    item.EquipmentTags.Contains("SSS_SIGNATURE")),
                Is.EqualTo(10));
        }

        [Test]
        public void PrimaryCodesRecruitAllTenAtA0WithoutEquipment090()
        {
            var campaign = Campaign090();
            var service = new SssTenV4CreatorCommand090();
            foreach (var hero in SssTenV4Roster090.All)
            {
                campaign = Require(service.Redeem(campaign, hero.RecruitCode));
                var recruit = SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, hero.HeroId);
                Assert.That(recruit, Is.Not.Null, hero.HeroId);
                Assert.That(recruit.Progression.AscensionLevel, Is.Zero);
                Assert.That(recruit.Equipment.Assignments, Is.Empty,
                    "Recruit codes must never auto-equip gear.");
            }
            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(10));
            Assert.That(SssTenV4CampaignAccessor090.Read(campaign)
                .Progression.codeReceipts.Count, Is.EqualTo(10));
        }

        [Test]
        public void LegacyAliasInRosterPreventsDuplicateRecruit090()
        {
            var alias = new RecruitState(
                "SS_RYLEN_STONEBOND", 100, 100, 30, 30);
            var campaign = Campaign090(new[] { alias });
            var result = new SssTenV4CreatorCommand090().Redeem(
                campaign, "SDGOW-SSS-BOND");
            campaign = Require(result);
            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(1));
            Assert.That(SssTenV4Roster090.RosterContains(
                campaign.Guild.Recruits, "SSS_RYLEN_STONEBOND"), Is.True);
        }

        [Test]
        public void EarlyBoundCodeConsumesNothingThenTenCreditsReachA10Only090()
        {
            var service = new SssTenV4CreatorCommand090();
            var campaign = Campaign090();
            var original = campaign;
            var early = service.Redeem(campaign,
                "SDGOW-SSS-MERCY-ASCEND-01");
            Assert.That(early.IsSuccess, Is.False);
            Assert.That(campaign, Is.SameAs(original));
            Assert.That(campaign.Guild.Inventory, Is.Empty);
            Assert.That(SssTenV4CampaignAccessor090.Read(campaign)
                .Progression.codeReceipts, Is.Empty);

            campaign = Require(service.Redeem(campaign, "SDGOW-SSS-MERCY"));
            var hero = SssTenV4Roster090.Get("SSS_NERIS_DAWNWELL");
            var beforeArts = SssTenV4Roster090.FindOwned(
                campaign.Guild.Recruits, hero.HeroId).Progression.LearnedArtIds.ToArray();
            for (var rank = 1; rank <= 10; rank++)
            {
                campaign = Require(service.Redeem(campaign,
                    "SDGOW-SSS-MERCY-ASCEND-" + rank.ToString("00")));
            }
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, hero.HeroId), Is.EqualTo(10));
            Assert.That(campaign.Guild.Inventory.All(x => x.InventoryOnly), Is.True);
            for (var rank = 1; rank <= 10; rank++)
            {
                campaign = Require(Ascend090(
                    campaign, hero.HeroId, "ASCEND_NERIS_" + rank));
                Assert.That(SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, hero.HeroId)
                    .Progression.AscensionLevel, Is.EqualTo(rank));
            }
            var mastered = SssTenV4Roster090.FindOwned(
                campaign.Guild.Recruits, hero.HeroId);
            CollectionAssert.AreEqual(beforeArts,
                mastered.Progression.LearnedArtIds,
                "Ascension must preserve all learned Arts/mastery authority.");
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, hero.HeroId), Is.Zero);
            var atTen = Ascend090(
                campaign, hero.HeroId, "ASCEND_NERIS_11");
            Assert.That(atTen.IsSuccess, Is.False);
            Assert.That(SssTenV4Roster090.FindOwned(
                campaign.Guild.Recruits, hero.HeroId)
                .Progression.AscensionLevel, Is.EqualTo(10));
        }

        [Test]
        public void SignatureWeaponIsManualHeroOnlyAndCannotReturnAfterDiscard090()
        {
            var service = new SssTenV4CreatorCommand090();
            var campaign = Require(service.Redeem(
                Campaign090(), "SDGOW-SSS-PACT"));
            campaign = Require(service.Redeem(
                campaign, "SDGOW-SSS-PACT-WEAPON"));
            var hero = SssTenV4Roster090.Get("SSS_ELYSIA_NIGHTCALL");
            var item = campaign.Guild.Inventory.Single(x =>
                StringComparer.Ordinal.Equals(x.DefinitionId, hero.WeaponItemId));
            var elysia = SssTenV4Roster090.FindOwned(
                campaign.Guild.Recruits, hero.HeroId);
            Assert.That(elysia.Equipment.Assignments, Is.Empty);
            Assert.That(SssTenV4Inventory090.CanEquip(
                elysia, item, EquipmentSlotIds.MainHand, out _), Is.True);
            var stranger = new RecruitState("OTHER_HERO", 100, 100, 20, 20);
            Assert.That(SssTenV4Inventory090.CanEquip(
                stranger, item, EquipmentSlotIds.MainHand, out var reason), Is.False);
            StringAssert.Contains("only hero", reason);

            var inventory = campaign.Guild.Inventory
                .Where(x => !StringComparer.Ordinal.Equals(x.InstanceId, item.InstanceId))
                .ToArray();
            campaign = campaign.With(
                campaign.Guild.With(
                    campaign.Guild.TreasuryXp,
                    campaign.Guild.Recruits,
                    campaign.Guild.Unions,
                    inventory,
                    campaign.Guild.Development),
                campaign.OpeningFlow);
            var replay = service.Redeem(campaign,
                "SDGOW-SSS-PACT-WEAPON");
            Assert.That(replay.IsSuccess, Is.False);
            Assert.That(campaign.Guild.Inventory, Is.Empty,
                "Persistent entitlement must survive sale/discard and block regrant.");
            Assert.That(SssTenV4CampaignAccessor090.Read(campaign)
                .Progression.codeReceipts, Contains.Item(
                    SssSignatureWeapons.EntitlementReceipt(hero.HeroId)));
        }

        [Test]
        public void CampaignRoundTripKeepsRankInventoryAndSharedReceiptsAtomic090()
        {
            var service = new SssTenV4CreatorCommand090();
            var campaign = Require(service.Redeem(
                Campaign090(), "SDGOW-SSS-MERCY"));
            campaign = Require(service.Redeem(
                campaign, "SDGOW-SSS-MERCY-ASCEND-01"));
            campaign = Require(Ascend090(
                campaign, "SSS_NERIS_DAWNWELL", "ROUNDTRIP_ASCEND_090"));
            campaign = Require(service.Redeem(
                campaign, "SDGOW-SSS-MERCY-WEAPON"));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));
            var hero = SssTenV4Roster090.Get("SSS_NERIS_DAWNWELL");
            var recruit = SssTenV4Roster090.FindOwned(
                reloaded.Guild.Recruits, hero.HeroId);
            Assert.That(recruit.Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(recruit.Equipment.Assignments, Is.Empty);
            Assert.That(reloaded.Guild.Inventory.Count(item =>
                StringComparer.Ordinal.Equals(item.DefinitionId,
                    hero.WeaponItemId)), Is.EqualTo(1));
            Assert.That(SssTenV4CampaignAccessor090.Read(reloaded)
                    .Progression.codeReceipts,
                Contains.Item(SssSignatureWeapons.EntitlementReceipt(hero.HeroId)));
            Assert.That(service.Redeem(reloaded,
                "SDGOW-SSS-MERCY-WEAPON").IsSuccess, Is.False,
                "A reload must not reopen the code/natural unique entitlement.");
        }

        [Test]
        public void EverySignatureMatchesItsExplicitLiveOmegaBudget090()
        {
            var campaign = Campaign090();
            foreach (var hero in SssTenV4Roster090.All)
            {
                var item = SssTenV4Inventory090.CreateSignatureWeapon(
                    campaign, hero.HeroId);
                var audit = M2EquipmentPowerBudget087.AuditSssSignature090(item);
                Assert.That(audit.ReferenceItemId,
                    Is.EqualTo("OMEGA_" + hero.WeaponFamilyId), hero.HeroId);
                Assert.That(audit.ReservedEffectPoints, Is.EqualTo(4));
                Assert.That(audit.SignatureTotalBudget,
                    Is.EqualTo(audit.OmegaReferenceStatPoints), hero.HeroId);
                Assert.That(audit.BudgetParityResolved, Is.True, hero.HeroId);
                Assert.That(audit.EffectHandlerReady, Is.True,
                    "Every shipped SSS signature must bind to its equipped-only M2 handler.");
                StringAssert.Contains("active", audit.EffectReadiness.ToLowerInvariant());
            }
        }

        [Test]
        public void StaleAscensionPreviewCannotSpendAnotherCredit090()
        {
            var service = new SssTenV4CreatorCommand090();
            const string heroId = "SSS_RYLEN_STONEBOND";
            var campaign = Require(service.Redeem(
                Campaign090(), "SDGOW-SSS-BOND"));
            campaign = Require(service.Redeem(
                campaign, "SDGOW-SSS-BOND-ASCEND-01"));
            var staleBalance = SssTenV4HostRewards090.PreviewAscension(
                campaign, heroId);
            campaign = Require(service.Redeem(
                campaign, "SDGOW-SSS-BOND-ASCEND-02"));

            var balanceRejected = SssTenV4HostRewards090.Ascend(
                campaign, heroId, "ASCEND_RYLEN_STALE_BALANCE_01",
                staleBalance.CurrentRank, staleBalance.CreditCount,
                staleBalance.StateGuard);
            Assert.That(balanceRejected.IsSuccess, Is.False);
            StringAssert.Contains("Stale Ascension preview",
                string.Join(" | ", balanceRejected.Errors));
            Assert.That(SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, heroId)
                .Progression.AscensionLevel, Is.Zero);
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, heroId), Is.EqualTo(2),
                "A stale inventory-balance guard must consume nothing.");

            var stale = SssTenV4HostRewards090.PreviewAscension(
                campaign, heroId);

            campaign = Require(SssTenV4HostRewards090.Ascend(
                campaign, heroId, "ASCEND_RYLEN_FRESH_01",
                stale.CurrentRank, stale.CreditCount, stale.StateGuard));
            Assert.That(SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, heroId)
                .Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, heroId), Is.EqualTo(1));

            var rejected = SssTenV4HostRewards090.Ascend(
                campaign, heroId, "ASCEND_RYLEN_STALE_02",
                stale.CurrentRank, stale.CreditCount, stale.StateGuard);
            Assert.That(rejected.IsSuccess, Is.False);
            StringAssert.Contains("Stale Ascension preview",
                string.Join(" | ", rejected.Errors));
            Assert.That(SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, heroId)
                .Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, heroId), Is.EqualTo(1),
                "A rejected stale command must consume nothing.");

            campaign = Require(Ascend090(
                campaign, heroId, "ASCEND_RYLEN_FRESH_02"));
            Assert.That(SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, heroId)
                .Progression.AscensionLevel, Is.EqualTo(2));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, heroId), Is.Zero);
        }

        [Test]
        public void EverySignatureWeaponUsesItsExactImportedResourceSprite090()
        {
            foreach (var hero in SssTenV4Roster090.All)
            {
                Assert.That(hero.WeaponArtResourcePath,
                    Is.EqualTo("SecondDimension/SSSTenV4/Weapons/" + hero.WeaponItemId));
                Assert.That(SecondDimension.Presentation.M1VisualAssets.TryResolveEquipment(
                    "RESOURCE:" + hero.WeaponArtResourcePath,
                    out var sprite,
                    out var resolvedResourcePath), Is.True,
                    hero.HeroId + " signature art is missing at its exact Resources key.");
                Assert.That(sprite, Is.Not.Null);
                Assert.That(resolvedResourcePath, Is.EqualTo(hero.WeaponArtResourcePath));
            }
        }

        [Test]
        public void SssPresentationUsesOnlyExactIdentitySpritePaths090()
        {
            foreach (var hero in SssTenV4Roster090.All)
            {
                CollectionAssert.AreEqual(new[] { hero.PortraitArtResourcePath },
                    SecondDimension.Presentation.M1VisualAssets.PortraitResourceKeys(
                        hero.HeroId,
                        string.Empty,
                        "HUMAN",
                        hero.HeroId),
                    hero.HeroId + " must never borrow a generic race portrait.");
                Assert.That(SecondDimension.Presentation.M1VisualAssets
                    .TryResolveBattleStandee(
                    hero.HeroId, string.Empty, string.Empty, hero.HeroId,
                    out var idle, out var idleKey), Is.True);
                Assert.That(idle, Is.Not.Null);
                Assert.That(idleKey, Is.EqualTo(hero.IdleArtResourcePath));
                Assert.That(SecondDimension.Presentation.M1VisualAssets
                    .TryResolveBattleActionPose(
                    hero.HeroId, string.Empty, string.Empty, hero.HeroId,
                    out var attack, out var attackKey), Is.True);
                Assert.That(attack, Is.Not.Null);
                Assert.That(attackKey, Is.EqualTo(hero.AttackArtResourcePath));
                Assert.That(SecondDimension.Presentation.M1VisualAssets
                    .TryResolvePortrait(
                    hero.HeroId, string.Empty, string.Empty, hero.HeroId,
                    out var portrait, out var portraitKey), Is.True);
                Assert.That(portrait, Is.Not.Null);
                Assert.That(portraitKey, Is.EqualTo(hero.PortraitArtResourcePath));
            }
        }

        private static CampaignState Campaign090(RecruitState[] recruits = null) =>
            new CampaignState(
                "00000000-0000-0000-0000-000000000091",
                90091L,
                "SSS_ROSTER_TEST_090",
                ModeRuleSnapshot.StandardDefaults(),
                new GuildState(
                    "GUILD_SSS_ROSTER_090",
                    0,
                    recruits ?? Array.Empty<RecruitState>(),
                    Array.Empty<UnionState>()));

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join(" | ", result.Errors));
            return result.Value;
        }

        private static Result<CampaignState> Ascend090(
            CampaignState campaign,
            string heroId,
            string requestId)
        {
            var preview = SssTenV4HostRewards090.PreviewAscension(
                campaign, heroId);
            return SssTenV4HostRewards090.Ascend(
                campaign, heroId, requestId, preview.CurrentRank,
                preview.CreditCount, preview.StateGuard);
        }
    }
}
