using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ChestLootAutoEquip092Tests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void GenuineChestUpgradeEquipsBestMatchingWarriorOrCasterViaExistingAuthority(bool caster)
        {
            var tags = caster ? new[] { "STAFF", "HEALING" } : new[] { "SWORD", "WEAPON" };
            var incoming = Item("NEW_EPIC", tags, "QUALITY_EPIC");
            var weak = Hero("WEAK", caster, Item("STARTER", tags, "QUALITY_COMMON", progression: false));
            var stronger = Hero("STRONG", caster, Item("RARE", tags, "QUALITY_RARE"));
            var otherRole = Hero("OTHER", !caster, Item("OTHER_START", caster ? new[] { "SWORD", "WEAPON" } : new[] { "STAFF", "HEALING" }, "QUALITY_COMMON", progression: false));
            var campaign = Campaign(new[] { stronger, otherRole, weak }, incoming);
            var decision = ChestLootAutoEquip092.Preview(campaign, incoming);
            Assert.That(decision, Is.Not.Null);
            Assert.That(decision.RecruitId, Is.EqualTo(weak.RecruitId));
            Assert.That(decision.PhysicalGain, Is.GreaterThanOrEqualTo(0));
            Assert.That(decision.MysticGain, Is.GreaterThanOrEqualTo(0));
            Assert.That(decision.PhysicalGain + decision.MysticGain, Is.GreaterThan(0));
            var manual = new M1CommandService().EquipItem(campaign, decision.RecruitId, decision.SlotId, incoming.InstanceId);
            Assert.That(manual.IsSuccess, Is.True, string.Join("\n", manual.Errors));
            var equipped = ChestLootAutoEquip092.Apply(campaign, incoming.InstanceId);
            Assert.That(CanonicalJson.Serialize(equipped.Guild), Is.EqualTo(CanonicalJson.Serialize(manual.Value.Guild)),
                "Auto must produce precisely the inventory/loadout mutation of the existing equipment authority.");
            var recipient = equipped.Guild.Recruits.Single(value => value.RecruitId == weak.RecruitId);
            Assert.That(recipient.Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId, Is.EqualTo(incoming.InstanceId));
            Assert.That(tags.All(recipient.Equipment.Find(EquipmentSlotIds.MainHand).Item.EquipmentTags.Contains), Is.True);
            Assert.That(equipped.Guild.Inventory.Count(value => value.InstanceId == weak.Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId), Is.EqualTo(1));
            Assert.That(equipped.Guild.Inventory.Any(value => value.InstanceId == incoming.InstanceId), Is.False);
            Assert.That(CanonicalJson.Serialize(equipped.OpeningFlow), Is.EqualTo(CanonicalJson.Serialize(campaign.OpeningFlow)),
                "Automatic loot cannot mark an equipment review/manual tutorial commit as completed.");
            Assert.That(ChestLootAutoEquip092.Apply(equipped, incoming.InstanceId), Is.SameAs(equipped));
        }

        [TestCase("LOSE_HEALING")]
        [TestCase("CHANGE_WEAPON")]
        [TestCase("NEW_LOCKED")]
        [TestCase("CURRENT_LOCKED")]
        [TestCase("SIGNATURE_CURRENT")]
        [TestCase("PROTECTED_ACTOR")]
        [TestCase("DOWNGRADE")]
        [TestCase("EQUAL_POWER")]
        public void UnsafeOrAmbiguousChestEquipmentStaysInInventory(string reason)
        {
            var healer = reason == "LOSE_HEALING";
            var tags = healer ? new[] { "STAFF", "HEALING" } : new[] { "SWORD", "WEAPON" };
            var currentTags = reason == "SIGNATURE_CURRENT" ? tags.Concat(new[] { "SSS_SIGNATURE" }).ToArray() : tags;
            var current = Item("CURRENT", currentTags,
                reason == "DOWNGRADE" ? "QUALITY_GODLY" : "QUALITY_RARE", locked: reason == "CURRENT_LOCKED");
            var nextTags = reason == "LOSE_HEALING" ? new[] { "STAFF" }
                : reason == "CHANGE_WEAPON" ? new[] { "BOW", "WEAPON" } : tags;
            var incoming = Item("INCOMING", nextTags,
                reason == "DOWNGRADE" ? "QUALITY_COMMON" : reason == "EQUAL_POWER" ? "QUALITY_RARE" : "QUALITY_EPIC",
                locked: reason == "NEW_LOCKED");
            var hero = Hero(reason == "PROTECTED_ACTOR" ? "KAEL" : "NORMAL", healer, current);
            var campaign = Campaign(new[] { hero }, incoming);
            Assert.That(ChestLootAutoEquip092.Preview(campaign, incoming), Is.Null, reason);
            Assert.That(ChestLootAutoEquip092.Apply(campaign, incoming.InstanceId), Is.SameAs(campaign));
            Assert.That(campaign.Guild.Inventory.Single().InstanceId, Is.EqualTo(incoming.InstanceId));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyHandReceivesOnlyMatchingWeaponRole(bool caster)
        {
            var hero = Hero("EMPTY", caster, null);
            var wrong = Item("WRONG", caster ? new[] { "SWORD", "WEAPON" } : new[] { "STAFF", "HEALING" }, "QUALITY_EPIC");
            var right = Item("RIGHT", caster ? new[] { "STAFF", "HEALING" } : new[] { "SWORD", "WEAPON" }, "QUALITY_EPIC");
            Assert.That(ChestLootAutoEquip092.Preview(Campaign(new[] { hero }, wrong), wrong), Is.Null);
            Assert.That(ChestLootAutoEquip092.Preview(Campaign(new[] { hero }, right), right)?.RecruitId, Is.EqualTo(hero.RecruitId));
        }

        [Test]
        public void ActiveBattlePreventsAutoChangingEquipmentOrCommittedCombatState()
        {
            var tags = new[] { "SWORD", "WEAPON" };
            var incoming = Item("BATTLE_UPGRADE", tags, "QUALITY_EPIC");
            var campaign = Campaign(new[] { Hero("WARRIOR", false, Item("OLD", tags, "QUALITY_COMMON")) }, incoming);
            var content = M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var started = new M2BattleCommandService().StartEncounterBattle(campaign, content,
                "BATTLE_LOOT_AUTO_092", "Preserve committed combat loadouts.", 1);
            Assert.That(started.IsSuccess, Is.True, string.Join("\n", started.Errors));
            campaign = started.Value;
            var hash = CanonicalJson.Sha256Hex(campaign);
            Assert.That(ChestLootAutoEquip092.Preview(campaign, incoming), Is.Null);
            var after = ChestLootAutoEquip092.Apply(campaign, incoming.InstanceId);
            Assert.That(after, Is.SameAs(campaign));
            Assert.That(CanonicalJson.Sha256Hex(after), Is.EqualTo(hash));
        }

        private static EquipmentItemState Item(string id, string[] tags, string quality,
            bool locked = false, bool progression = true) => new EquipmentItemState(
                (progression ? "LOOT_ITEM_070_" : "STARTER_") + id, "GEAR_" + id, "Chest " + id,
                new[] { EquipmentSlotIds.MainHand }, tags, quality, 10000, locked);

        private static RecruitState Hero(string id, bool caster, EquipmentItemState item) =>
            new RecruitState(id, 400, 400, 100, 100, id, RecruitOriginKind.Procedural, string.Empty,
                "HUMAN", "WORLD_GATE_01", caster ? "CLASS_TEND_PRIEST" : "CLASS_TEND_WARRIOR", "Observed", 6000,
                RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                new EquipmentLoadoutState(item == null ? Array.Empty<EquipmentSlotAssignmentState>() :
                    new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, item) }),
                true, string.Empty, string.Empty, 60, 60);

        private static CampaignState Campaign(RecruitState[] heroes, EquipmentItemState reward) =>
            new CampaignState("00000000-0000-0000-0000-000000000692", 20260907L, "1.0", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("CHEST_AUTO_TEST", 0, heroes, new[] { new UnionState("CHEST_UNION", "Chest Testers", UnionKind.Normal,
                    heroes[0].RecruitId, heroes.Select(value => value.RecruitId).ToArray(), "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 8500) },
                    new[] { reward }),
                new NewGuildProfileState("Loot Tester", GameMode.Standard, TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null,
                    false, 439, 0, true, true, false, false, "autosave_unions"));
    }
}
