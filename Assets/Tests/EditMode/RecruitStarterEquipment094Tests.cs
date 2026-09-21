using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>Isolated recruitment and save fixtures; no personal saves or artificial acquisition claims.</summary>
    public sealed class RecruitStarterEquipment094Tests
    {
        RecruitAutoGenerator010 _generator;
        RecruitStarterEquipment094 _policy;
        M2CombatContent _combat;
        Dictionary<string, JObject> _families;

        [OneTimeSetUp]
        public void Load094()
        {
            var root = HeroRosterAudit093.ContentRoot093;
            var catalog = RecruitAutoGenerationCatalog010.LoadFromContentRoot(root);
            _generator = new RecruitAutoGenerator010(catalog);
            _combat = M2CombatContent.LoadFromDirectory(root);
            _policy = new RecruitStarterEquipment094(catalog, _combat);
            _families = JObject.Parse(File.ReadAllText(Path.Combine(root,
                "CONTENT_AUTHORITY_002", "DATA", "WEAPON_FAMILIES_12_PRESERVED.json")))
                ["weaponFamilies"].Cast<JObject>().ToDictionary(value => value.Value<string>("id"));
        }

        public static IEnumerable<TestCaseData> Accepted094() =>
            HeroRosterAudit093.Catalog093.AcceptedHeroes.OrderBy(value => value.RosterId)
                .Select(value => new TestCaseData(value.RosterId)
                    .SetName("RealAcceptedHeroGetsLegalNewRecruitEquipment094_" + value.StableId));
        public static IEnumerable<TestCaseData> Sss094() =>
            SssTenV4Roster090.All.Select(value => new TestCaseData(value.HeroId));

        [TestCaseSource(nameof(Accepted094))]
        public void RealAcceptedHeroGetsLegalNewRecruitEquipment094(int rosterId)
        {
            var hero = Hero094(rosterId);
            var before = HeroRosterAudit093.CreateFixture093(hero);
            var raw = HeroRosterAudit093.SignAndPlace093(before, hero, new HeroRosterAuditRow093());
            var authored = raw.Guild.Recruits.Single();
            var beforeHash = CanonicalJson.Sha256Hex(before);
            var rawHash = CanonicalJson.Sha256Hex(raw);
            var outfitted = _policy.ApplyToNewRecruits(before, raw);
            var recruit = outfitted.Guild.Recruits.Single();
            Assert.That(recruit.AuthoredStableRecruitId, Is.EqualTo(hero.StableId));
            Assert.That(CanonicalJson.Serialize(recruit.Progression),
                Is.EqualTo(CanonicalJson.Serialize(authored.Progression)));
            Assert.That(recruit.CurrentHp, Is.EqualTo(authored.CurrentHp));
            Assert.That(recruit.CurrentMp, Is.EqualTo(authored.CurrentMp));
            Assert.That(outfitted.Guild.TreasuryXp, Is.EqualTo(raw.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(outfitted.OpeningFlow), Is.EqualTo(CanonicalJson.Serialize(raw.OpeningFlow)));
            if (authored.Equipment.Assignments.Count == 0)
            {
                var family = _generator.Generate(authored).FixedWeaponFamilyId;
                var tags = _families[family]["equipmentTagsGranted"].Values<string>().ToArray();
                Assert.That(recruit.Equipment.Assignments, Has.Count.EqualTo(2));
                Assert.That(recruit.Equipment.Assignments.Any(value =>
                    value.Item.EquipmentTags.Contains(family) && tags.All(value.Item.EquipmentTags.Contains)), Is.True);
                Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
                foreach (var assignment in recruit.Equipment.Assignments)
                {
                    Assert.That(SssTenV4Inventory090.CanEquip(recruit, assignment.Item, assignment.SlotId, out _), Is.True);
                    Assert.That(assignment.Item.EquipmentTags, Does.Contain("RECRUIT_STARTER_094"));
                    Assert.That(assignment.Item.EquipmentTags, Does.Not.Contain("SSS_SIGNATURE"));
                    Assert.That(M2EquipmentPowerPolicy087.Resolve(assignment.Item).PhysicalAttack, Is.Zero);
                    Assert.That(M2EquipmentPowerPolicy087.Resolve(assignment.Item).MysticAttack, Is.Zero);
                }
                Assert.That(outfitted.Guild.Inventory, Is.Empty, "Basic grants are equipped, not duplicated in inventory.");
            }
            else
                Assert.That(CanonicalJson.Serialize(recruit.Equipment),
                    Is.EqualTo(CanonicalJson.Serialize(authored.Equipment)), "Authored loadouts are not rewritten.");
            Assert.That(CanonicalJson.Sha256Hex(_policy.ApplyToNewRecruits(before, outfitted)),
                Is.EqualTo(CanonicalJson.Sha256Hex(outfitted)), "Replaying the same transaction cannot grant duplicates.");
            Assert.That(CanonicalJson.Sha256Hex(before), Is.EqualTo(beforeHash));
            Assert.That(CanonicalJson.Sha256Hex(raw), Is.EqualTo(rawHash));
        }

        [TestCaseSource(nameof(Sss094))]
        public void SssPrimaryCodeGetsBasicGearWithoutSignatureEntitlement094(string heroId)
        {
            var hero = SssTenV4Roster090.Get(heroId);
            var before = HeroRosterAudit093.CreateFixture093(Hero094(171));
            var command = new SssTenV4CreatorCommand090();
            var raw = HeroRosterAudit093.Require093(command.Redeem(before, hero.RecruitCode));
            var original = SssTenV4Roster090.FindOwned(raw.Guild.Recruits, heroId);
            Assert.That(original.Equipment.Assignments, Is.Empty, "The raw code authority remains equipment-free.");
            var result = _policy.ApplyToNewRecruits(before, raw);
            var recruit = SssTenV4Roster090.FindOwned(result.Guild.Recruits, heroId);
            Assert.That(recruit.Equipment.Assignments, Has.Count.EqualTo(2));
            Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
            Assert.That(recruit.Equipment.Assignments.All(value => value.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")), Is.True);
            Assert.That(recruit.Equipment.Assignments.All(value => value.Item.DefinitionId != hero.WeaponItemId), Is.True);
            Assert.That(result.Guild.Inventory.Any(value => value.EquipmentTags.Contains("SSS_SIGNATURE")), Is.False);
            Assert.That(CanonicalJson.Serialize(result.SssV4090), Is.EqualTo(CanonicalJson.Serialize(raw.SssV4090)),
                "Starter equipment cannot mint signature entitlements or alter code/progression receipts.");
            Assert.That(CanonicalJson.Serialize(recruit.Progression), Is.EqualTo(CanonicalJson.Serialize(original.Progression)));
            var completedHash = CanonicalJson.Sha256Hex(result);
            var replay = command.Redeem(result, hero.RecruitCode);
            Assert.That(replay.IsSuccess, Is.False,
                "Existing SSS authority deliberately rejects a code that was already redeemed.");
            Assert.That(string.Join("; ", replay.Errors), Does.Contain("already redeemed"));
            Assert.That(CanonicalJson.Sha256Hex(result), Is.EqualTo(completedHash));
            Assert.That(_policy.ApplyToNewRecruits(result, result), Is.SameAs(result));
        }

        [TestCaseSource(nameof(Sss094))]
        public void NewSssCanStartWithAlreadyEarnedExactSignatureWithoutCreatingEntitlement121(string heroId)
        {
            var before = HeroRosterAudit093.CreateFixture093(Hero094(171));
            var raw = HeroRosterAudit093.Require093(SssTenV4HostRewards090.GrantRecruit(before, heroId, "NEW_SSS121"));
            raw = HeroRosterAudit093.Require093(SssTenV4HostRewards090.GrantSignatureWeapon(raw, heroId, "GRANTED_SIGNATURE121"));
            var hero = SssTenV4Roster090.Get(heroId);
            var signature = raw.Guild.Inventory.Single(item => item.DefinitionId == hero.WeaponItemId);
            var result = _policy.ApplyToNewRecruits(before, raw);
            var recruit = SssTenV4Roster090.FindOwned(result.Guild.Recruits, heroId);
            var equipped = recruit.Equipment.Assignments.Single(slot => slot.Item.InstanceId == signature.InstanceId);
            Assert.That(CanonicalJson.Serialize(equipped.Item), Is.EqualTo(CanonicalJson.Serialize(signature)));
            Assert.That(recruit.Equipment.Assignments, Has.Count.EqualTo(2));
            Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
            Assert.That(result.Guild.Inventory.Any(item => item.InstanceId == signature.InstanceId), Is.False);
            Assert.That(CanonicalJson.Serialize(result.SssV4090), Is.EqualTo(CanonicalJson.Serialize(raw.SssV4090)));
            Assert.That(CanonicalJson.Serialize(recruit.Progression), Is.EqualTo(CanonicalJson.Serialize(
                SssTenV4Roster090.FindOwned(raw.Guild.Recruits, heroId).Progression)));
            Assert.That(_policy.ApplyToNewRecruits(result, result), Is.SameAs(result));
            var removed = HeroRosterAudit093.Require093(new M1CommandService().UnequipItem(result, heroId, equipped.SlotId));
            Assert.That(_policy.ApplyToNewRecruits(result, removed), Is.SameAs(removed), "The hero is now existing; starter equipment must not return.");
            Assert.That(HeroRosterAudit093.Require093(SssAutomaticRewards107.ApplyReady(removed)), Is.SameAs(removed));
        }

        [Test]
        public void NewSssKeepsLockedExactSignatureInInventory121()
        {
            const string heroId = "SSS_NERIS_DAWNWELL";
            var before = HeroRosterAudit093.CreateFixture093(Hero094(171));
            var raw = HeroRosterAudit093.Require093(SssTenV4HostRewards090.GrantRecruit(before, heroId, "NEW_LOCKED121"));
            raw = HeroRosterAudit093.Require093(SssTenV4HostRewards090.GrantSignatureWeapon(raw, heroId, "LOCKED_SIGNATURE121"));
            var signature = raw.Guild.Inventory.Single(item => item.DefinitionId == SssTenV4Roster090.Get(heroId).WeaponItemId);
            raw = Inventory094(raw, raw.Guild.Inventory.Select(item => item.InstanceId == signature.InstanceId ? item.WithPlayerLock(true) : item).ToArray());
            var result = _policy.ApplyToNewRecruits(before, raw);
            var recruit = SssTenV4Roster090.FindOwned(result.Guild.Recruits, heroId);
            Assert.That(recruit.Equipment.Assignments, Has.Count.EqualTo(2));
            Assert.That(recruit.Equipment.Assignments.All(slot => slot.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")), Is.True);
            Assert.That(result.Guild.Inventory.Single(item => item.InstanceId == signature.InstanceId).PlayerLocked, Is.True);
            Assert.That(CanonicalJson.Serialize(result.SssV4090), Is.EqualTo(CanonicalJson.Serialize(raw.SssV4090)));
        }

        [Test]
        public void OwnedLegalUpgradeWinsWithoutGeneratingAnUnusedBasicWeapon094()
        {
            var hero = Hero094(170);
            var before = HeroRosterAudit093.CreateFixture093(hero);
            var raw = HeroRosterAudit093.SignAndPlace093(before, hero, new HeroRosterAuditRow093());
            var family = _generator.Generate(raw.Guild.Recruits.Single()).FixedWeaponFamilyId;
            var tags = _families[family]["equipmentTagsGranted"].Values<string>().ToArray();
            var slot = family == "WEAPON_FAMILY_SHIELD" ? EquipmentSlotIds.OffHand : EquipmentSlotIds.MainHand;
            var upgrade = Item094("LOOT_ITEM_070_RECRUIT_UPGRADE", slot, tags);
            raw = Inventory094(raw, new[] { upgrade });
            var result = _policy.ApplyToNewRecruits(before, raw);
            var recruit = result.Guild.Recruits.Single();
            Assert.That(recruit.Equipment.Find(slot).Item.InstanceId, Is.EqualTo(upgrade.InstanceId));
            Assert.That(recruit.Equipment.Assignments.Count(value => value.Item.InstanceId.StartsWith("RECRUIT_BASIC_094_")),
                Is.EqualTo(1), "Only the missing armor needs a generated item.");
            Assert.That(result.Guild.Inventory, Is.Empty);
            Assert.That(result.Guild.TreasuryXp, Is.EqualTo(raw.Guild.TreasuryXp));
        }

        [Test]
        public void LockedWrongFamilyAndBoundSignatureStayInInventory094()
        {
            var hero = Hero094(170);
            var before = HeroRosterAudit093.CreateFixture093(hero);
            var raw = HeroRosterAudit093.SignAndPlace093(before, hero, new HeroRosterAuditRow093());
            var family = _generator.Generate(raw.Guild.Recruits.Single()).FixedWeaponFamilyId;
            var tags = _families[family]["equipmentTagsGranted"].Values<string>().ToArray();
            var slot = family == "WEAPON_FAMILY_SHIELD" ? EquipmentSlotIds.OffHand : EquipmentSlotIds.MainHand;
            var locked = Item094("LOOT_ITEM_070_LOCKED_RECRUIT", slot, tags, true);
            var wrong = Item094("LOOT_ITEM_070_WRONG_FAMILY", slot, new[] { "NEVER_THIS_RECRUIT_FAMILY" });
            var signature = SssTenV4Inventory090.CreateSignatureWeapon(raw, "SSS_ISOLDE_ECLIPSERIFT");
            raw = Inventory094(raw, new[] { locked, wrong, signature });
            var inventoryHash = CanonicalJson.Sha256Hex(raw.Guild.Inventory);
            var result = _policy.ApplyToNewRecruits(before, raw);
            Assert.That(CanonicalJson.Sha256Hex(result.Guild.Inventory), Is.EqualTo(inventoryHash));
            Assert.That(result.Guild.Recruits.Single().Equipment.Assignments.All(value =>
                value.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")), Is.True);
        }

        [Test]
        public void ExistingEmptyRecruitAndPartiallyEquippedArrivalAreNotMigrated094()
        {
            var hero = Hero094(170);
            var before = HeroRosterAudit093.CreateFixture093(hero);
            var raw = HeroRosterAudit093.SignAndPlace093(before, hero, new HeroRosterAuditRow093());
            Assert.That(raw.Guild.Recruits.Single().Equipment.Assignments, Is.Empty);
            Assert.That(_policy.ApplyToNewRecruits(raw, raw), Is.SameAs(raw));
            var armor = Item094("AUTHORED_PARTIAL_ARMOR", EquipmentSlotIds.BodyArmor, new[] { "ARMOR" });
            var original = raw.Guild.Recruits.Single().WithEquipment(new EquipmentLoadoutState(
                new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.BodyArmor, armor) }));
            var partial = raw.With(raw.Guild.With(raw.Guild.TreasuryXp, new[] { original },
                raw.Guild.Unions, raw.Guild.Inventory), raw.OpeningFlow);
            Assert.That(_policy.ApplyToNewRecruits(before, partial), Is.SameAs(partial));
        }

        [TestCase(170)] [TestCase(171)]
        public void ShippingSignOrSsCodeSavesGearExactlyOnceAndReloadDoesNotRegrant094(int rosterId)
        {
            var hero = Hero094(rosterId);
            WithSave094(HeroRosterAudit093.CreateFixture093(hero), (path, before) =>
            {
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var result = hero.Rank == HeroMasterRank087.SS
                    ? coordinator.RedeemCreatorCode028(hero.SsGenerationCode)
                    : coordinator.SignGuildCityApplicant017D(before.Guild.GuildCity.RecruitmentBoard.Applicants.Single().RecruitId);
                Assert.That(result.Succeeded, Is.True, result.Message);
                var saved = HeroRosterAudit093.Read093(path);
                var recruit = saved.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == hero.StableId);
                Assert.That(recruit.Equipment.Assignments, Has.Count.EqualTo(2));
                Assert.That(recruit.Equipment.Assignments.All(value =>
                    value.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")), Is.True);
                var itemIds = recruit.Equipment.Assignments.Select(value => value.Item.InstanceId).ToArray();
                var reloaded = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var retry = hero.Rank == HeroMasterRank087.SS
                    ? reloaded.RedeemCreatorCode028(hero.SsGenerationCode)
                    : reloaded.SignGuildCityApplicant017D(before.Guild.GuildCity.RecruitmentBoard.Applicants.Single().RecruitId);
                var after = HeroRosterAudit093.Read093(path);
                Assert.That(after.Guild.Recruits, Has.Count.EqualTo(saved.Guild.Recruits.Count));
                Assert.That(after.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == hero.StableId)
                    .Equipment.Assignments.Select(value => value.Item.InstanceId), Is.EquivalentTo(itemIds));
                Assert.That(after.Guild.Inventory.Count, Is.EqualTo(saved.Guild.Inventory.Count));
                Assert.That(after.Guild.TreasuryXp, Is.EqualTo(saved.Guild.TreasuryXp));
            });
        }

        [Test]
        public void ShippingSssRecruitCodeWritesBasicGearButDoesNotGrantItsSignature094()
        {
            var hero = SssTenV4Roster090.Get("SSS_NERIS_DAWNWELL");
            WithSave094(HeroRosterAudit093.CreateFixture093(Hero094(171)), (path, before) =>
            {
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var result = coordinator.RedeemCreatorCode028(hero.RecruitCode);
                Assert.That(result.Succeeded, Is.True, result.Message);
                var saved = HeroRosterAudit093.Read093(path);
                var recruit = SssTenV4Roster090.FindOwned(saved.Guild.Recruits, hero.HeroId);
                Assert.That(recruit, Is.Not.Null);
                Assert.That(recruit.Equipment.Assignments, Has.Count.EqualTo(2));
                Assert.That(recruit.Equipment.Assignments.All(value =>
                    value.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")), Is.True);
                Assert.That(saved.Guild.Inventory.Any(value => value.DefinitionId == hero.WeaponItemId), Is.False);
                var beforeGear = CanonicalJson.Sha256Hex(recruit.Equipment);
                var reloaded = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var replay = reloaded.RedeemCreatorCode028(hero.RecruitCode);
                Assert.That(replay.Succeeded, Is.False, "The existing SSS code must reject repeat redemption.");
                Assert.That(replay.Message, Does.Contain("already redeemed"));
                var after = HeroRosterAudit093.Read093(path);
                Assert.That(CanonicalJson.Sha256Hex(SssTenV4Roster090.FindOwned(
                    after.Guild.Recruits, hero.HeroId).Equipment), Is.EqualTo(beforeGear));
                Assert.That(after.Guild.Recruits.Count, Is.EqualTo(saved.Guild.Recruits.Count));
                Assert.That(after.Guild.Inventory.Count, Is.EqualTo(saved.Guild.Inventory.Count));
            });
        }

        [Test]
        public void ShippingReloadDoesNotRepairOldEmptyLoadoutsWithoutNewRecruitTransaction094()
        {
            var hero = Hero094(170);
            var old = HeroRosterAudit093.SignAndPlace093(HeroRosterAudit093.CreateFixture093(hero),
                hero, new HeroRosterAuditRow093());
            WithSave094(old, (path, before) =>
            {
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var after = HeroRosterAudit093.Read093(path);
                Assert.That(after.Guild.Recruits.Single().Equipment.Assignments, Is.Empty);
                Assert.That(after.Guild.Inventory, Is.Empty);
                Assert.That(after.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
            });
        }

        static HeroMaster300Hero087 Hero094(int rosterId) =>
            HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == rosterId);
        static EquipmentItemState Item094(string id, string slot, string[] tags, bool locked = false) =>
            new EquipmentItemState(id, "GEAR_TEST_094", "Existing owned item", new[] { slot },
                tags, "QUALITY_EPIC", 10000, locked);
        static CampaignState Inventory094(CampaignState campaign, EquipmentItemState[] inventory) =>
            campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, inventory), campaign.OpeningFlow);
        static void WithSave094(CampaignState state, Action<string, CampaignState> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "sd_starter_094_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "isolated_save.json");
                HeroRosterAudit093.Write093(path, state);
                action(path, state);
            }
            finally
            {
                // This test owns only its explicit unique temporary fixture directory.
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}

