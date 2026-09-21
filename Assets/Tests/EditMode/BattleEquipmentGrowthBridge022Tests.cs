#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleEquipmentGrowthBridge022Tests
    {
        private CampaignRegistry022 _registry;
        private M2CombatContent _content;
        private M2BattleCommandService _battles;
        private BattleEquipmentGrowthBridge022 _growth;

        [SetUp]
        public void SetUp()
        {
            _registry = CampaignRegistry022.LoadFromResources();
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _battles = new M2BattleCommandService();
            _growth = new BattleEquipmentGrowthBridge022();
        }

        [Test]
        public void RealClaimBindsDeterministicSnapshotItemsToActualArtGrowth()
        {
            var pending = ResolveBattle(CreateCampaign());
            var expected = PositiveGrowthByMember(pending.Battle);
            Assert.That(expected, Is.Not.Empty);
            foreach (var union in pending.Battle.PlayerUnions)
            foreach (var member in union.Members)
            {
                var recruit = pending.Guild.Recruits.Single(value => value.RecruitId == member.MemberId);
                Assert.That(member.EquippedMainHandInstanceId,
                    Is.EqualTo(recruit.Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId));
            }

            var claimed = Require(_battles.ClaimBattleRewards(pending));
            var applied = Require(_growth.ApplyClaimedBattleGrowth(claimed, _registry));
            var progression = Progression(applied);

            foreach (var pair in expected)
            {
                var member = pending.Battle.PlayerUnions.SelectMany(value => value.Members)
                    .Single(value => value.MemberId == pair.Key);
                var itemGrowth = progression.EquipmentEvolution.Single(value =>
                    value.ItemInstanceId == member.EquippedMainHandInstanceId);
                Assert.That(itemGrowth.TrackId, Is.EqualTo("WTRACK022_01"));
                Assert.That(itemGrowth.MeaningfulUses, Is.EqualTo(pair.Value.Count));
                Assert.That(itemGrowth.MasteryPoints, Is.EqualTo(pair.Value.Sum(value => value.Amount)));
                Assert.That(itemGrowth.HistoryTag, Does.StartWith("EQUSE022_"));
                Assert.That(progression.AppliedReceiptIds.Count(value => value == itemGrowth.HistoryTag), Is.EqualTo(1));
            }

            var identicalPending = ResolveBattle(CreateCampaign());
            var identicalClaimed = Require(_battles.ClaimBattleRewards(identicalPending));
            var identicalApplied = Require(_growth.ApplyClaimedBattleGrowth(identicalClaimed, _registry));
            CollectionAssert.AreEqual(
                progression.AppliedReceiptIds.Where(value => value.StartsWith("EQUSE022_", StringComparison.Ordinal)),
                Progression(identicalApplied).AppliedReceiptIds.Where(value =>
                    value.StartsWith("EQUSE022_", StringComparison.Ordinal)));

            var legacyMember = new BattleMemberState(
                "LEGACY_MEMBER", "Legacy Member", "CLASS_WARRIOR",
                10, 10, 0, 0, 1, 1, new[] { "SWORD" },
                false, false, false, Array.Empty<string>(), 0, 0, string.Empty);
            Assert.That(CanonicalJson.Serialize(legacyMember),
                Does.Not.Contain("EquippedMainHandInstanceId"),
                "Missing snapshots must stay omitted so old save-v11 battle JSON remains readable.");
        }

        [Test]
        public void RepeatedRewardClaimAndGrowthApplicationAreAFullNoOp()
        {
            var pending = ResolveBattle(CreateCampaign());
            var claimed = Require(_battles.ClaimBattleRewards(pending));
            var first = Require(_growth.ApplyClaimedBattleGrowth(claimed, _registry));
            var before = CanonicalJson.Sha256Hex(first);
            var receiptCount = Progression(first).AppliedReceiptIds.Count(value =>
                value.StartsWith("EQUSE022_", StringComparison.Ordinal));

            var claimedAgain = Require(_battles.ClaimBattleRewards(first));
            var appliedAgain = Require(_growth.ApplyClaimedBattleGrowth(claimedAgain, _registry));

            Assert.That(CanonicalJson.Sha256Hex(claimedAgain), Is.EqualTo(before));
            Assert.That(CanonicalJson.Sha256Hex(appliedAgain), Is.EqualTo(before));
            Assert.That(Progression(appliedAgain).AppliedReceiptIds.Count(value =>
                value.StartsWith("EQUSE022_", StringComparison.Ordinal)), Is.EqualTo(receiptCount));
        }

        [TestCase("WF01_SWORD", "WTRACK022_01")]
        [TestCase("WF02_GREAT_WEAPON", "WTRACK022_02")]
        [TestCase("WF03_AXE", "WTRACK022_03")]
        [TestCase("WF04_SPEAR_POLEARM", "WTRACK022_04")]
        [TestCase("WF05_BOW", "WTRACK022_05")]
        [TestCase("WF06_DAGGER", "WTRACK022_06")]
        [TestCase("WF07_SHIELD", "WTRACK022_07")]
        [TestCase("WF08_GAUNTLET", "WTRACK022_08")]
        [TestCase("WF09_STAFF", "WTRACK022_09")]
        [TestCase("WF10_CATALYST_FOCUS", "WTRACK022_10")]
        [TestCase("WF11_ENGINEERING_TOOL", "WTRACK022_11")]
        [TestCase("WF12_HYBRID_RELIC", "WTRACK022_12")]
        public void PackagedFamilyBindsExistingRegistryTrackWithoutLegacyCombatTags107(string family, string trackId)
        {
            var item = new EquipmentItemState("FAMILY107_" + family, "FAMILY107", "Authored family item",
                new[] { EquipmentSlotIds.MainHand }, new[] { family, "MODIFIED_WEAPON" }, "QUALITY_SSS_SIGNATURE", 10000, false);
            var resolve = typeof(BattleEquipmentGrowthBridge022).GetMethod("ResolveWeaponTrack",
                BindingFlags.Static | BindingFlags.NonPublic);
            var track = (WeaponTrackDto022)resolve.Invoke(null, new object[] { item, _registry });
            Assert.That(track, Is.SameAs(_registry.WeaponTracks[trackId]),
                "Use the existing authored evolution track, without granting or substituting equipment.");
        }

        [TestCase("SSS_ELYSIA_NIGHTCALL", "WTRACK022_09")]
        [TestCase("SSS_RYLEN_STONEBOND", "WTRACK022_09")]
        [TestCase("SSS_MYRIEN_STARFALL", "WTRACK022_10")]
        [TestCase("SSS_VAELIS_MANYFORM", "WTRACK022_08")]
        [TestCase("SSS_ASTERION_SUNWARD", "WTRACK022_04")]
        public void ActualSssSignatureSnapshotReceivesClaimedArtGrowthExactlyOnce107(string heroId, string trackId)
        {
            var codes = new SssTenV4CreatorCommand090();
            var hero = SssTenV4Roster090.Get(heroId);
            var campaign = Require(codes.Redeem(CreateCampaign(), hero.RecruitCode));
            campaign = Require(codes.Redeem(campaign, hero.RecruitCode + "-WEAPON"));
            var recruit = SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, heroId);
            var signature = campaign.Guild.Inventory.Single(item => item.DefinitionId == hero.WeaponItemId);
            campaign = Require(new M1CommandService().EquipItem(campaign, recruit.RecruitId,
                EquipmentSlotIds.MainHand, signature.InstanceId));
            var union = new UnionState("SSS_EQUSE107", "Signature Growth", UnionKind.Normal,
                recruit.RecruitId, new[] { recruit.RecruitId }, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 30, 8500);
            campaign = campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits, new[] { union }, campaign.Guild.Inventory), campaign.OpeningFlow);
            var pending = ResolveBattle(campaign);
            var evidence = PositiveGrowthByMember(pending.Battle)[recruit.RecruitId];
            Assert.That(pending.Battle.PlayerUnions.Single().Members.Single().EquippedMainHandInstanceId,
                Is.EqualTo(signature.InstanceId));
            var claimed = Require(_battles.ClaimBattleRewards(pending));
            var claimedHash = CanonicalJson.Sha256Hex(claimed);
            var applied = Require(_growth.ApplyClaimedBattleGrowth(claimed, _registry));
            var evolution = Progression(applied).EquipmentEvolution.Single(item => item.ItemInstanceId == signature.InstanceId);
            Assert.That(evolution.TrackId, Is.EqualTo(trackId));
            Assert.That(evolution.MeaningfulUses, Is.EqualTo(evidence.Count));
            Assert.That(evolution.MasteryPoints, Is.EqualTo(evidence.Sum(item => item.Amount)));
            Assert.That(CanonicalJson.Sha256Hex(claimed), Is.EqualTo(claimedHash), "Do not mutate the source receipt or item snapshot.");
            Assert.That(_growth.ApplyClaimedBattleGrowth(applied, _registry).Value, Is.SameAs(applied));
        }

        [Test]
        public void UnknownPackagedFamilyStillRejectsClaimedSnapshotWithoutAwardingGrowth107()
        {
            var claimed = Require(_battles.ClaimBattleRewards(ResolveBattle(CreateCampaign())));
            var memberId = PositiveGrowthByMember(claimed.Battle).Keys.First();
            var recruit = claimed.Guild.Recruits.Single(value => value.RecruitId == memberId);
            var snapshot = recruit.Equipment.Find(EquipmentSlotIds.MainHand).Item;
            var unknown = new EquipmentItemState(snapshot.InstanceId, snapshot.DefinitionId, snapshot.DisplayName,
                snapshot.ValidSlotIds, new[] { "WF99_UNKNOWN", "MODIFIED_WEAPON" }, snapshot.QualityId,
                snapshot.ConditionBasisPoints, snapshot.PlayerLocked);
            var altered = recruit.WithEquipment(new EquipmentLoadoutState(recruit.Equipment.Assignments.Select(slot =>
                slot.Item.InstanceId == snapshot.InstanceId ? new EquipmentSlotAssignmentState(slot.SlotId, unknown) : slot).ToArray()));
            claimed = claimed.With(claimed.Guild.With(claimed.Guild.TreasuryXp,
                claimed.Guild.Recruits.Select(value => value.RecruitId == memberId ? altered : value).ToArray(),
                claimed.Guild.Unions, claimed.Guild.Inventory), claimed.OpeningFlow);
            var before = CanonicalJson.Sha256Hex(claimed);
            var result = _growth.ApplyClaimedBattleGrowth(claimed, _registry);
            Assert.That(result.IsSuccess, Is.False);
            CollectionAssert.Contains(result.Errors, "CAMPAIGN022_EQUIPMENT_WEAPON_TRACK_UNRESOLVED");
            Assert.That(CanonicalJson.Sha256Hex(claimed), Is.EqualTo(before));
        }

        [Test]
        public void CurrentLoadoutCannotSubstituteForBattleMainHandSnapshot()
        {
            var pending = ResolveBattle(CreateCampaign());
            var growthMemberId = PositiveGrowthByMember(pending.Battle).Keys.OrderBy(value => value, StringComparer.Ordinal).First();
            var snapshotMember = pending.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == growthMemberId);
            var snapshotItemId = snapshotMember.EquippedMainHandInstanceId;
            var replacementItemId = pending.Battle.Reward.EquipmentReward.InstanceId;
            var claimed = Require(_battles.ClaimBattleRewards(pending));

            var loadoutChanged = Require(new M1CommandService().EquipItem(
                claimed,
                growthMemberId,
                EquipmentSlotIds.MainHand,
                replacementItemId));
            Assert.That(loadoutChanged.Guild.Recruits.Single(value => value.RecruitId == growthMemberId)
                .Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId, Is.EqualTo(replacementItemId));
            Assert.That(loadoutChanged.Guild.Inventory.Any(value => value.InstanceId == snapshotItemId), Is.True);

            var applied = Require(_growth.ApplyClaimedBattleGrowth(loadoutChanged, _registry));
            Assert.That(Progression(applied).EquipmentEvolution.Any(value =>
                value.ItemInstanceId == snapshotItemId && value.MasteryPoints > 0), Is.True);
            Assert.That(Progression(applied).EquipmentEvolution.Any(value =>
                value.ItemInstanceId == replacementItemId), Is.False,
                "A post-battle loadout change cannot redirect immutable battle evidence.");
        }

        [Test]
        public void UnclaimedUnresolvedForgedAndBadItemEvidenceAreRejected()
        {
            var unresolved = Require(_battles.StartTutorialBattle(CreateCampaign(), _content));
            var unresolvedResult = _growth.ApplyClaimedBattleGrowth(unresolved, _registry);
            Assert.That(unresolvedResult.IsSuccess, Is.False);
            CollectionAssert.Contains(unresolvedResult.Errors, "CAMPAIGN022_EQUIPMENT_TERMINAL_REWARD_REQUIRED");

            var pending = ResolveBattle(CreateCampaign());
            var unclaimedResult = _growth.ApplyClaimedBattleGrowth(pending, _registry);
            Assert.That(unclaimedResult.IsSuccess, Is.False);
            CollectionAssert.Contains(unclaimedResult.Errors, "CAMPAIGN022_EQUIPMENT_REWARD_CLAIM_REQUIRED");

            var claimed = Require(_battles.ClaimBattleRewards(pending));
            var forgedEvents = new List<BattleEventState>(claimed.Battle.EventLog);
            var forgedIndex = forgedEvents.FindIndex(value => value.EventType == "ART_GROWTH" && value.Amount > 0);
            Assert.That(forgedIndex, Is.GreaterThanOrEqualTo(0));
            var source = forgedEvents[forgedIndex];
            forgedEvents[forgedIndex] = new BattleEventState(
                source.Sequence, source.Round, source.EventType, source.Side,
                source.UnionId, source.MemberId, source.ArtId, source.Text,
                source.Amount + 1, source.StateHash, source.ActorUnionId, source.ActorMemberId,
                source.TargetUnionId, source.TargetMemberId);
            var forgedCampaign = claimed.WithBattle(claimed.Battle.With(eventLog: forgedEvents.AsReadOnly()));
            var forgedResult = _growth.ApplyClaimedBattleGrowth(forgedCampaign, _registry);
            Assert.That(forgedResult.IsSuccess, Is.False);
            CollectionAssert.Contains(forgedResult.Errors, "CAMPAIGN022_EQUIPMENT_BATTLE_AUTHORITY_INVALID");

            var badItemCampaign = WithBadSnapshotItem(claimed);
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(badItemCampaign.Battle), Is.True,
                "The bad-item fixture must retain the current dual-hash terminal authority shape so ownership validation is reached.");
            var badItemResult = _growth.ApplyClaimedBattleGrowth(badItemCampaign, _registry);
            Assert.That(badItemResult.IsSuccess, Is.False);
            CollectionAssert.Contains(badItemResult.Errors, "CAMPAIGN022_EQUIPMENT_SNAPSHOT_ITEM_NOT_OWNED");
        }

        private CampaignState ResolveBattle(CampaignState campaign)
        {
            campaign = Require(_battles.StartTutorialBattle(campaign, _content));
            for (var guard = 0; guard < 30 && campaign.Battle.Outcome == BattleOutcome.InProgress; guard++)
            {
                var active = campaign.Battle.PlayerUnions.Where(value => !value.IsDefeated && !value.Retreated).ToArray();
                for (var unionIndex = 0; unionIndex < active.Length; unionIndex++)
                {
                    var unionId = active[unionIndex].UnionId;
                    var forecast = campaign.Battle.CommittedForecasts.FirstOrDefault(value =>
                        value.UnionId == unionId && value.CommandId == "CMD_ALL_OUT") ??
                        campaign.Battle.CommittedForecasts.FirstOrDefault(value =>
                            value.UnionId == unionId && value.CommandId == "CMD_BALANCED") ??
                        campaign.Battle.CommittedForecasts.First(value => value.UnionId == unionId);
                    campaign = Require(_battles.SelectForecast(campaign, unionId, forecast.ForecastId));
                }
                campaign = Require(_battles.ConfirmRound(campaign, _content));
            }
            Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "ART_GROWTH" && value.Amount > 0), Is.True);
            return campaign;
        }

        private static Dictionary<string, List<BattleEventState>> PositiveGrowthByMember(BattleState battle) =>
            battle.EventLog
                .Where(value => value.EventType == "ART_GROWTH" && value.Side == BattleSide.Player && value.Amount > 0)
                .GroupBy(value => value.MemberId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.ToList(), StringComparer.Ordinal);

        private static CampaignState WithBadSnapshotItem(CampaignState campaign)
        {
            var targetId = PositiveGrowthByMember(campaign.Battle).Keys.OrderBy(value => value, StringComparer.Ordinal).First();
            var unions = new List<BattleUnionState>(campaign.Battle.PlayerUnions);
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var memberIndex = unions[unionIndex].FindMemberIndex(targetId);
                if (memberIndex < 0) continue;
                var members = new List<BattleMemberState>(unions[unionIndex].Members);
                var member = members[memberIndex];
                members[memberIndex] = new BattleMemberState(
                    member.MemberId, member.DisplayName, member.ClassId,
                    member.CurrentHp, member.MaximumHp, member.CurrentMp, member.MaximumMp,
                    member.Attack, member.MagicAttack, member.EquipmentTags,
                    member.Downed, member.Stabilized, member.Guarding, member.LearnedArtIds,
                    member.MeaningfulUsePoints, member.DiscoveryProgress, member.BreakthroughArtId,
                    member.ArtProgress, "ITEM_FORGED_NOT_OWNED_022");
                unions[unionIndex] = unions[unionIndex].With(members: members.AsReadOnly());
                break;
            }
            var altered = campaign.Battle.With(
                playerUnions: unions.AsReadOnly(),
                finalStateHash: string.Empty,
                finalIntegrityStateHash090: string.Empty);
            altered = altered.With(
                finalStateHash: M2BattleCommandService.GameplayRngStateHash090(altered),
                finalIntegrityStateHash090: M2BattleCommandService.AuthoritativeStateHash(altered));
            return campaign.WithBattle(altered);
        }

        private static CampaignState CreateCampaign()
        {
            var recruits = new List<RecruitState>();
            var memberIds = new List<string>();
            for (var index = 0; index < 3; index++)
            {
                var recruitId = "EQUSE022_RECRUIT_" + index;
                var item = new EquipmentItemState(
                    "EQUSE022_ITEM_" + index,
                    "CA002_WPN_SWORD_TRAINING",
                    "Equipment Growth Sword " + index,
                    new[] { EquipmentSlotIds.MainHand },
                    new[] { "SWORD", "WEAPON" },
                    "TRAINING",
                    10000,
                    false);
                recruits.Add(new RecruitState(
                    recruitId, 180, 180, 35, 35, "Equipment Tester " + index,
                    RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01",
                    index == 2 ? "CLASS_TEND_MAGE" : "CLASS_TEND_WARRIOR",
                    "Observed", 7500, RecruitAuthorityKind.Normal,
                    string.Empty, string.Empty,
                    new EquipmentLoadoutState(new[]
                    {
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, item)
                    }),
                    true, string.Empty, string.Empty, 65, 65));
                memberIds.Add(recruitId);
            }
            var union = new UnionState(
                "EQUSE022_UNION", "Equipment Growth Union", UnionKind.Normal,
                memberIds[0], memberIds.AsReadOnly(), "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED",
                18, 8500);
            var guild = new GuildState("EQUSE022_GUILD", 0, recruits.AsReadOnly(), new[] { union });
            var profile = new NewGuildProfileState(
                "Equipment Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "equse022_test_ready");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000229",
                22029,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                flow);
        }

        private static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
