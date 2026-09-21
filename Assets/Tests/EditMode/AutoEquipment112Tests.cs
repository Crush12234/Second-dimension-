using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class AutoEquipment112Tests
    {
        const string A = "SSS_ASTERION_SUNWARD";
        const string B = "SSS_SOLENNE_AEGIS";
        const string C = "SSS_CAEDRAN_TEMPEST";
        AutoEquipmentService112 _service;
        M2CombatContent _combat;

        [OneTimeSetUp]
        public void ExistingAuthorities112()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _combat = M2CombatContent.LoadFromDirectory(root);
            _service = new AutoEquipmentService112(_combat, new RecruitStarterEquipment094(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(root), _combat));
        }

        [Test]
        public void AllUnionAllocationSharesScarceGearGloballyWithoutProcessingOrderBias112()
        {
            var state = AllocationFixture112();
            var before = CanonicalJson.Serialize(state);
            var result = Require112(_service.EquipAllUnions(state));
            Assert.That(ItemAt112(result, A, EquipmentSlotIds.AccessoryOne), Is.EqualTo("LOOT_ITEM_070_BOUND_A"));
            Assert.That(ItemAt112(result, B, EquipmentSlotIds.AccessoryOne), Is.EqualTo("LOOT_ITEM_070_SHARED"));
            AssertItems112(state, result);
            Assert.That(CanonicalJson.Serialize(state), Is.EqualTo(before), "Planning cannot mutate the original campaign.");
            var reversed = state.With(state.Guild.With(state.Guild.TreasuryXp,
                state.Guild.Recruits.Reverse().ToArray(), state.Guild.Unions.Reverse().ToArray(), state.Guild.Inventory), state.OpeningFlow);
            var reverseResult = Require112(_service.EquipAllUnions(reversed));
            Assert.That(Owners112(reverseResult), Is.EqualTo(Owners112(result)));
        }

        [Test]
        public void HeroCommandNeverTakesAnotherHerosGearAndAllUnionsNeverStripsReserves112()
        {
            var reserveItem = Item112("RESERVE_OMEGA", EquipmentSlotIds.AccessoryOne, "QUALITY_CREATOR_OMEGA", "ACCESSORY");
            var active = Hero112(A, EquipmentSlotIds.AccessoryOne, Item112("A_COMMON", EquipmentSlotIds.AccessoryOne, "QUALITY_COMMON", "ACCESSORY"));
            var reserve = Hero112(B, EquipmentSlotIds.AccessoryOne, reserveItem);
            var state = Campaign112(new[] { active, reserve }, Array.Empty<EquipmentItemState>(), activeCount: 1);
            Assert.That(_service.EquipAllUnions(state).Value, Is.SameAs(state));
            Assert.That(_service.EquipHero(state, A).Value, Is.SameAs(state));
            Assert.That(ItemAt112(state, B, EquipmentSlotIds.AccessoryOne), Is.EqualTo(reserveItem.InstanceId));
        }

        [Test]
        public void LockedItemsAndEquippedSignatureRemainOnExactOwners112()
        {
            var locked = Item112("LOCKED", EquipmentSlotIds.MainHand, "QUALITY_COMMON", "SPEAR").WithPlayerLock(true);
            var signature = Item112("SIGNATURE", EquipmentSlotIds.MainHand, "QUALITY_COMMON", "SHIELD", "SSS_SIGNATURE");
            var heroes = new[] { Hero112(A, EquipmentSlotIds.MainHand, locked), Hero112(B, EquipmentSlotIds.MainHand, signature) };
            var inventory = new[]
            {
                Item112("STRONG_SPEAR", EquipmentSlotIds.MainHand, "QUALITY_CREATOR_OMEGA", "SPEAR"),
                Item112("STRONG_SHIELD", EquipmentSlotIds.MainHand, "QUALITY_CREATOR_OMEGA", "SHIELD"),
                Item112("LOCKED_SPARE", EquipmentSlotIds.AccessoryOne, "QUALITY_CREATOR_OMEGA", "ACCESSORY").WithPlayerLock(true)
            };
            var state = Campaign112(heroes, inventory);
            var result = Require112(_service.EquipAllUnions(state));
            Assert.That(ItemAt112(result, A, EquipmentSlotIds.MainHand), Is.EqualTo(locked.InstanceId));
            Assert.That(ItemAt112(result, B, EquipmentSlotIds.MainHand), Is.EqualTo(signature.InstanceId));
            Assert.That(result.Guild.Inventory.Any(value => value.InstanceId == "LOOT_ITEM_070_LOCKED_SPARE"), Is.True);
            AssertItems112(state, result);
        }

        [Test]
        public void EmptyCasterHandUsesAuthoredFamilyAndExistingWeaponArtsRemainLegal112()
        {
            var mage = SssTenV4Roster090.MaterializeGrant(C).Recruit;
            var inventory = new[]
            {
                Item112("WRONG_SWORD", EquipmentSlotIds.MainHand, "QUALITY_CREATOR_OMEGA", "SWORD"),
                Item112("RIGHT_STAFF", EquipmentSlotIds.MainHand, "QUALITY_RARE", "STAFF")
            };
            var result = Require112(_service.EquipHero(Campaign112(new[] { mage }, inventory), C));
            Assert.That(ItemAt112(result, C, EquipmentSlotIds.MainHand), Is.EqualTo("LOOT_ITEM_070_RIGHT_STAFF"));

            var spear = Hero112(A, EquipmentSlotIds.MainHand,
                Item112("CURRENT_SPEAR", EquipmentSlotIds.MainHand, "QUALITY_COMMON", "SPEAR", "POLEARM"));
            var unchanged = Campaign112(new[] { spear }, new[] { inventory[0] });
            Assert.That(Require112(_service.EquipHero(unchanged, A)), Is.SameAs(unchanged));
        }

        [Test]
        public void CompatibleBasicArmorFillsAnEmptySlotWithoutInventingPower112()
        {
            var armor = new EquipmentItemState("BASIC_ARMOR112", "TRAVEL_ARMOR", "Travel Armor",
                new[] { EquipmentSlotIds.BodyArmor }, new[] { "ARMOR", "BODY_ARMOR" }, "QUALITY_STARTER", 10000, false);
            var hero = SssTenV4Roster090.MaterializeGrant(A).Recruit;
            var result = Require112(_service.EquipHero(Campaign112(new[] { hero }, new[] { armor }), A));
            Assert.That(ItemAt112(result, A, EquipmentSlotIds.BodyArmor), Is.EqualTo(armor.InstanceId));
            Assert.That(result.EquipmentUndo112.Summary, Does.Contain("physical +0, mystic +0"));
        }

        [Test]
        public void NewReceiptRoundTripsAndUndoRestoresOnlyEquipment112()
        {
            var state = AllocationFixture112();
            var result = Require112(_service.EquipAllUnions(state));
            Assert.That(result.EquipmentUndo112, Is.Not.Null);
            var reloaded = RoundTrip112(result);
            Assert.That(CanonicalJson.Sha256Hex(reloaded), Is.EqualTo(CanonicalJson.Sha256Hex(result)));
            // Currency rewards do not change equipment ownership and must never
            // be rolled back by a gear-only undo transaction.
            var rewarded = reloaded.With(reloaded.Guild.With(12345, reloaded.Guild.Recruits,
                reloaded.Guild.Unions, reloaded.Guild.Inventory), reloaded.OpeningFlow);
            var undone = Require112(AutoEquipmentService112.Undo(rewarded));
            Assert.That(Owners112(undone), Is.EqualTo(Owners112(state)));
            Assert.That(undone.Guild.TreasuryXp, Is.EqualTo(12345));
            Assert.That(undone.EquipmentUndo112, Is.Null);
            Assert.That(CanonicalJson.Serialize(undone.Guild.Recruits), Is.EqualTo(CanonicalJson.Serialize(state.Guild.Recruits)));
            AssertItems112(state, undone);
            Assert.That(AutoEquipmentService112.Undo(undone).IsSuccess, Is.False);
            Assert.That(RoundTrip112(undone).EquipmentUndo112, Is.Null);
        }

        [TestCase("inventory")]
        [TestCase("roster")]
        [TestCase("lock")]
        public void InterveningChangesInvalidateUndoWithoutDiscardingThem112(string change)
        {
            var equipped = Require112(_service.EquipAllUnions(AllocationFixture112()));
            CampaignState changed;
            if (change == "inventory")
                changed = equipped.With(equipped.Guild.With(equipped.Guild.TreasuryXp, equipped.Guild.Recruits,
                    equipped.Guild.Unions, equipped.Guild.Inventory.Concat(new[] {
                        Item112("NEW_REWARD", EquipmentSlotIds.ToolRelic, "QUALITY_RARE", "TOOL") }).ToArray()), equipped.OpeningFlow);
            else if (change == "roster")
                changed = equipped.With(equipped.Guild.With(equipped.Guild.TreasuryXp,
                    equipped.Guild.Recruits.Concat(new[] { SssTenV4Roster090.MaterializeGrant(C).Recruit }).ToArray(),
                    equipped.Guild.Unions, equipped.Guild.Inventory), equipped.OpeningFlow);
            else
                changed = Require112(new M1CommandService().SetEquipmentLock(equipped, A, EquipmentSlotIds.AccessoryOne, true));
            var before = CanonicalJson.Serialize(changed);
            Assert.That(AutoEquipmentService112.Undo(changed).IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(changed), Is.EqualTo(before));
        }

        [Test]
        public void LegacyCurrentVersionSaveWithoutOptionalReceiptKeepsItsCanonicalHash112()
        {
            var state = AllocationFixture112();
            var raw = JObject.FromObject(SaveEnvelopeV1.Create(state,
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)), JsonSerializer.Create(CanonicalJson.DefaultSettings()));
            var campaign = (JObject)raw["CampaignState"];
            Assert.That(campaign.Property("EquipmentUndo112"), Is.Null,
                "A null optional property must be omitted despite global NullValueHandling.Include.");
            var hash = CanonicalJson.Sha256Hex(campaign);
            Assert.That(raw["CanonicalStateHash"].Value<string>(), Is.EqualTo(hash));
            var directory = NewDirectory112();
            try
            {
                var path = Path.Combine(directory, "pre_feature_v11.json");
                File.WriteAllText(path, raw.ToString(Formatting.None));
                var loaded = Require112(new AtomicSaveStore().ReadWithRecovery(path));
                Assert.That(loaded.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
                Assert.That(loaded.CampaignState.EquipmentUndo112, Is.Null);
                Assert.That(CanonicalJson.Sha256Hex(loaded.CampaignState), Is.EqualTo(hash));
                Assert.That(loaded.MigrationHistory, Is.Empty);
            }
            finally { DeleteDirectory112(directory); }
        }

        [Test]
        public void ActiveBattleCannotChangeEquipmentOrUndo112()
        {
            var state = Require112(_service.EquipAllUnions(AllocationFixture112()));
            var started = Require112(new M2BattleCommandService().StartEncounterBattle(state, _combat,
                "AUTO_EQUIP_BATTLE112", "Preserve committed equipment", 1));
            var hash = CanonicalJson.Sha256Hex(started);
            Assert.That(_service.EquipAllUnions(started).IsSuccess, Is.False);
            Assert.That(AutoEquipmentService112.Undo(started).IsSuccess, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(started), Is.EqualTo(hash));
        }

        [Test]
        public void AffectedSixtyOneOwnedCoordinatorBatchesPersistOnceAndUndoSurvivesReload112()
        {
            var source = Environment.GetEnvironmentVariable("SECOND_DIMENSION_RESET_PROFILE");
            if (string.IsNullOrWhiteSpace(source))
                Assert.Ignore("Set SECOND_DIMENSION_RESET_PROFILE to the preserved 20260912_083507 baseline profile.");
            var sourceBytes = File.ReadAllBytes(source);
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(sourceBytes)).Replace("-", ""),
                    Is.EqualTo("1668BC89991E6A41D2BCEFD66084753715E7C5311B22A4BA534E680AF86F3A75"),
                    "This regression must use the original affected 61-owned backup, never the live player profile.");
            var directory = NewDirectory112();
            var path = Path.Combine(directory, "affected_equipment.json");
            File.Copy(source, path);
            try
            {
                var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
                var coordinator = new M1RuntimeCoordinator(contentRoot, path);
                // Complete the source's genuine unbanked Tower victory first.
                // No battle/operation fields, inventories or heroes are injected.
                var terminal = M2BattleViewAccess098.Read(coordinator);
                if (terminal?.IsResolved == true && terminal.Reward?.Claimed == false)
                    RequireCommand112(coordinator.ClaimBattleRewards());
                for (var guard = 0; !string.IsNullOrWhiteSpace(
                         coordinator.CampaignProgression022.ActiveAbyssOperationId) && guard < 32; guard++)
                    RequireCommand112(coordinator.AdvanceTowerRun081());
                Assert.That(coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.Empty);
                var banked = CoordinatorCampaign112(coordinator);
                Assert.That(banked.Guild.Recruits.Count, Is.EqualTo(61));
                Assert.That(AutoEquipmentService112.MutationBlockedReason(banked), Is.Empty);
                var active = new HashSet<string>(banked.Guild.Unions.Where(value => value.Kind == UnionKind.Normal)
                    .SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
                var equipped = banked.Guild.Recruits.Where(value => active.Contains(value.RecruitId) &&
                        value.Equipment.Find(EquipmentSlotIds.BodyArmor)?.Item.PlayerLocked == false &&
                        value.Equipment.Find(EquipmentSlotIds.MainHand)?.Item.PlayerLocked == false)
                    .OrderBy(value => value.RecruitId, StringComparer.Ordinal).Take(2).ToArray();
                Assert.That(equipped, Has.Length.EqualTo(2), "Use existing equipped members as real upgrade opportunities.");
                var heroId = equipped[0].RecruitId;
                var secondId = equipped[1].RecruitId;
                var reserveId = banked.Guild.Recruits.First(value => !active.Contains(value.RecruitId)).RecruitId;

                RequireCommand112(coordinator.SetEquipmentLock(heroId, EquipmentSlotIds.MainHand, true));
                RequireCommand112(coordinator.UnequipItem(heroId, EquipmentSlotIds.BodyArmor));
                var beforeHero = CoordinatorCampaign112(coordinator);
                var lockedWeapon = CanonicalJson.Serialize(beforeHero.Guild.Recruits.Single(value =>
                    value.RecruitId == heroId).Equipment.Find(EquipmentSlotIds.MainHand).Item);
                var afterHero = PersistOnce112(coordinator, path, () => coordinator.AutoEquipHero112(heroId));
                Assert.That(afterHero.EquipmentUndo112, Is.Not.Null);
                Assert.That(afterHero.EquipmentUndo112.Changes, Is.Not.Empty);
                Assert.That(afterHero.EquipmentUndo112.Changes.All(value => value.RecruitId == heroId), Is.True);
                Assert.That(ItemAt112(afterHero, heroId, EquipmentSlotIds.BodyArmor), Is.Not.Null);
                AssertUnaffectedEquipment112(beforeHero, afterHero, new HashSet<string> { heroId });
                AssertRecruitProgress112(beforeHero, afterHero);
                coordinator = ReloadCoordinator112(contentRoot, path, afterHero);
                Assert.That(coordinator.CanUndoAutoEquip112, Is.True);
                var undoneHero = PersistOnce112(coordinator, path, coordinator.UndoAutoEquip112);
                Assert.That(Owners112(undoneHero), Is.EqualTo(Owners112(beforeHero)));
                Assert.That(undoneHero.EquipmentUndo112, Is.Null);
                coordinator = ReloadCoordinator112(contentRoot, path, undoneHero);
                Assert.That(coordinator.CanUndoAutoEquip112, Is.False);

                // Undo restored the first empty body slot. Unequip a second
                // existing member so All Unions must commit a multi-hero batch.
                RequireCommand112(coordinator.UnequipItem(secondId, EquipmentSlotIds.BodyArmor));
                var beforeAll = CoordinatorCampaign112(coordinator);
                var afterAll = PersistOnce112(coordinator, path, coordinator.AutoEquipAllUnions112);
                Assert.That(afterAll.EquipmentUndo112, Is.Not.Null);
                Assert.That(afterAll.EquipmentUndo112.Changes.Select(value => value.RecruitId).Distinct().Count(),
                    Is.GreaterThanOrEqualTo(2));
                Assert.That(ItemAt112(afterAll, heroId, EquipmentSlotIds.BodyArmor), Is.Not.Null);
                Assert.That(ItemAt112(afterAll, secondId, EquipmentSlotIds.BodyArmor), Is.Not.Null);
                Assert.That(CanonicalJson.Serialize(afterAll.Guild.Recruits.Single(value =>
                    value.RecruitId == heroId).Equipment.Find(EquipmentSlotIds.MainHand).Item), Is.EqualTo(lockedWeapon));
                AssertUnaffectedEquipment112(beforeAll, afterAll, active);
                AssertRecruitProgress112(beforeAll, afterAll);
                coordinator = ReloadCoordinator112(contentRoot, path, afterAll);
                Assert.That(coordinator.CanUndoAutoEquip112, Is.True);
                var committedAllBytes = File.ReadAllBytes(path);
                var undoneAll = PersistOnce112(coordinator, path, coordinator.UndoAutoEquip112);
                Assert.That(Owners112(undoneAll), Is.EqualTo(Owners112(beforeAll)));
                AssertItems112(beforeAll, undoneAll);
                AssertRecruitProgress112(beforeAll, undoneAll);
                Assert.That(undoneAll.EquipmentUndo112, Is.Null);
                coordinator = ReloadCoordinator112(contentRoot, path, undoneAll);
                Assert.That(coordinator.CanUndoAutoEquip112, Is.False);

                // Branch only the already committed save bytes. Real manual
                // commands must invalidate the durable receipt after reload.
                foreach (var change in new[] { "equipment", "roster" })
                {
                    var changedPath = Path.Combine(directory, change + "_changed.json");
                    File.WriteAllBytes(changedPath, committedAllBytes);
                    var changedCoordinator = ReloadCoordinator112(contentRoot, changedPath, afterAll);
                    if (change == "equipment")
                        RequireCommand112(changedCoordinator.UnequipItem(heroId, EquipmentSlotIds.BodyArmor));
                    else
                    {
                        var current = CoordinatorCampaign112(changedCoordinator);
                        var unionIndex = current.Guild.Unions.ToList().FindIndex(value =>
                            value.Kind == UnionKind.Normal && value.MemberRecruitIds.Count > 0);
                        var incumbent = current.Guild.Unions[unionIndex].MemberRecruitIds[0];
                        RequireCommand112(changedCoordinator.AssignReserveRecruitToUnion109(
                            reserveId, unionIndex, 0, incumbent));
                    }
                    var changed = CoordinatorCampaign112(changedCoordinator);
                    AssertItems112(afterAll, changed);
                    changedCoordinator = ReloadCoordinator112(contentRoot, changedPath, changed);
                    Assert.That(changedCoordinator.CanUndoAutoEquip112, Is.False);
                    var changedBytes = File.ReadAllBytes(changedPath);
                    var changedHash = CanonicalJson.Sha256Hex(changed);
                    Assert.That(changedCoordinator.UndoAutoEquip112().Succeeded, Is.False);
                    Assert.That(File.ReadAllBytes(changedPath), Is.EqualTo(changedBytes),
                        "Rejected Undo must not replace the save or discard the intervening " + change + " command.");
                    Assert.That(CanonicalJson.Sha256Hex(CoordinatorCampaign112(changedCoordinator)), Is.EqualTo(changedHash));
                }
            }
            finally
            {
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBytes), "The original affected backup must remain byte-for-byte intact.");
                DeleteDirectory112(directory);
            }
        }

        static CampaignState CoordinatorCampaign112(M1RuntimeCoordinator coordinator) =>
            (CampaignState)typeof(M1RuntimeCoordinator).GetField("_campaign",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coordinator);

        static void RequireCommand112(M1CommandResult result) =>
            Assert.That(result.Succeeded, Is.True, result.Message);

        static CampaignState PersistOnce112(M1RuntimeCoordinator coordinator, string path, Func<M1CommandResult> action)
        {
            var before = CoordinatorCampaign112(coordinator);
            var precedingPrimary = File.ReadAllBytes(path);
            var notifications = 0;
            void Changed() => notifications++;
            coordinator.Changed += Changed;
            try { RequireCommand112(action()); }
            finally { coordinator.Changed -= Changed; }
            var after = CoordinatorCampaign112(coordinator);
            Assert.That(notifications, Is.EqualTo(1), "Publish one committed batch, not per-slot equipment changes.");
            Assert.That(File.ReadAllBytes(path + ".bak"), Is.EqualTo(precedingPrimary),
                "AtomicSaveStore's predecessor must be the pre-batch primary, never an intermediate unequip/partial assignment.");
            Assert.That(File.Exists(path + ".tmp"), Is.False);
            var raw = JObject.Parse(File.ReadAllText(path));
            var saved = Require112(new AtomicSaveStore().ReadWithRecovery(path));
            Assert.That(saved.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
            Assert.That(raw.Value<string>("CanonicalStateHash"), Is.EqualTo(CanonicalJson.Sha256Hex(after)));
            Assert.That(CanonicalJson.Sha256Hex(saved.CampaignState), Is.EqualTo(CanonicalJson.Sha256Hex(after)));
            AssertItems112(before, after);
            return after;
        }

        static M1RuntimeCoordinator ReloadCoordinator112(string contentRoot, string path, CampaignState expected)
        {
            var coordinator = new M1RuntimeCoordinator(contentRoot, path);
            var loaded = CoordinatorCampaign112(coordinator);
            Assert.That(CanonicalJson.Sha256Hex(loaded), Is.EqualTo(CanonicalJson.Sha256Hex(expected)),
                "A full real coordinator reload must retain the exact campaign and Undo receipt.");
            AssertItems112(expected, loaded);
            return coordinator;
        }

        static void AssertUnaffectedEquipment112(CampaignState before, CampaignState after, ISet<string> selected)
        {
            foreach (var recruit in before.Guild.Recruits.Where(value => !selected.Contains(value.RecruitId)))
                Assert.That(CanonicalJson.Serialize(after.Guild.Recruits.Single(value =>
                    value.RecruitId == recruit.RecruitId).Equipment), Is.EqualTo(CanonicalJson.Serialize(recruit.Equipment)),
                    "An unselected hero or reserve must retain every equipped instance.");
        }

        static void AssertRecruitProgress112(CampaignState before, CampaignState after)
        {
            string WithoutEquipment(RecruitState recruit)
            {
                var token = JObject.FromObject(recruit);
                token.Remove("Equipment");
                return CanonicalJson.Serialize(token);
            }
            Assert.That(after.Guild.Recruits.Select(WithoutEquipment).OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(before.Guild.Recruits.Select(WithoutEquipment).OrderBy(value => value, StringComparer.Ordinal)),
                "Identity, vitals, progression and learned Arts must survive the equipment transaction.");
            Assert.That(CanonicalJson.Serialize(after.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(before.Guild.Unions)));
            Assert.That(after.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
        }

        static CampaignState AllocationFixture112()
        {
            var a = Hero112(A, EquipmentSlotIds.AccessoryOne, Item112("A_COMMON", EquipmentSlotIds.AccessoryOne, "QUALITY_COMMON", "ACCESSORY"));
            var b = Hero112(B, EquipmentSlotIds.AccessoryOne, Item112("B_COMMON", EquipmentSlotIds.AccessoryOne, "QUALITY_COMMON", "ACCESSORY"));
            return Campaign112(new[] { a, b }, new[] {
                Item112("SHARED", EquipmentSlotIds.AccessoryOne, "QUALITY_CREATOR_OMEGA", "ACCESSORY"),
                Item112("BOUND_A", EquipmentSlotIds.AccessoryOne, "QUALITY_GODLY", "ACCESSORY", SssTenV4Inventory090.EquipOnlyPrefix + A)
            });
        }
        static EquipmentItemState Item112(string id, string slot, string quality, params string[] tags) =>
            new EquipmentItemState("LOOT_ITEM_070_" + id, "ITEM_" + id, id, new[] { slot }, tags, quality, 10000, false);
        static RecruitState Hero112(string id, string slot, EquipmentItemState item) =>
            SssTenV4Roster090.MaterializeGrant(id).Recruit.WithEquipment(new EquipmentLoadoutState(new[] {
                new EquipmentSlotAssignmentState(slot, item) }));
        static CampaignState Campaign112(RecruitState[] heroes, EquipmentItemState[] inventory, int activeCount = 2) =>
            new CampaignState("00000000-0000-0000-0000-000000000112", 112, "1.0", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("AUTO112", 0, heroes, heroes.Take(activeCount).Select((hero, index) =>
                    new UnionState("U112_" + index, "Union " + index, UnionKind.Normal, hero.RecruitId,
                        new[] { hero.RecruitId }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 8500)).ToArray(), inventory),
                new NewGuildProfileState("Equipment regression", GameMode.Standard, TutorialDepth.FullTutorial,
                    AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null,
                    false, 439, 0, true, true, false, false, "autosave_unions"));
        static string ItemAt112(CampaignState state, string id, string slot) =>
            state.Guild.Recruits.Single(value => value.RecruitId == id).Equipment.Find(slot)?.Item.InstanceId;
        static string[] Owners112(CampaignState state) => state.Guild.Recruits.SelectMany(recruit =>
                recruit.Equipment.Assignments.Select(assignment => recruit.RecruitId + ":" + assignment.SlotId + ":" + assignment.Item.InstanceId))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        static void AssertItems112(CampaignState before, CampaignState after)
        {
            string[] Items(CampaignState state) => state.Guild.Inventory.Concat(state.Guild.Recruits.SelectMany(value =>
                    value.Equipment.Assignments.Select(assignment => assignment.Item)))
                .Select(CanonicalJson.Serialize).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Assert.That(Items(after), Is.EqualTo(Items(before)));
            Assert.That(after.Guild.Inventory.Select(value => value.InstanceId).Concat(after.Guild.Recruits.SelectMany(value =>
                    value.Equipment.Assignments.Select(assignment => assignment.Item.InstanceId))).GroupBy(value => value).All(group => group.Count() == 1), Is.True);
        }
        static T Require112<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            return result.Value;
        }
        static CampaignState RoundTrip112(CampaignState state)
        {
            var directory = NewDirectory112();
            try
            {
                var path = Path.Combine(directory, "equipment.json");
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(state, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                return Require112(store.ReadWithRecovery(path)).CampaignState;
            }
            finally { DeleteDirectory112(directory); }
        }
        static string NewDirectory112()
        {
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimension_Equipment112_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }
        static void DeleteDirectory112(string directory)
        {
            if (Directory.Exists(directory) && Path.GetFullPath(directory).StartsWith(
                    Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) Directory.Delete(directory, true);
        }
    }
}
