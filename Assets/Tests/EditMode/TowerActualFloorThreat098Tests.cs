using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerActualFloorThreat098Tests
    {
        CampaignState _oldPending, _oldCompleted;
        CampaignRegistry022 _registry;
        M2CombatContent _combat;
        string _temporary;
        readonly CampaignProgressionCommandService022 _tower = new CampaignProgressionCommandService022();

        [OneTimeSetUp]
        public void ReadUnmodifiedActualOldEvidence098()
        {
            _registry = CampaignRegistry022.LoadFromResources();
            _oldPending = Fixture098("R108_OldPendingFloor001.json");
            _oldCompleted = Fixture098("R100_OldCompletedFloor010.json");
            _combat = HeroRosterAudit093.LoadCombatContent093();
            Assert.That(_oldPending.Guild.GuildCity.PendingEncounter, Is.Not.Null);
            Assert.That(_oldPending.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(_oldCompleted, _registry).Value.HighestActualFloor,
                Is.EqualTo(10));
        }

        [SetUp]
        public void IsolatedDirectory098()
        {
            _temporary = Path.Combine(Path.GetTempPath(), "SecondDimension_Tower098_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporary);
        }

        [TearDown]
        public void RemoveOnlyOwnedTestDirectory098()
        { if (_temporary != null && Directory.Exists(_temporary)) Directory.Delete(_temporary, true); }

        [TestCase(10, 4500L, 2700L, 5)]
        [TestCase(11, 5000L, 3000L, 6)]
        [TestCase(20, 9500L, 5700L, 10)]
        [TestCase(21, 10000L, 6000L, 10)]
        [TestCase(50, 24500L, 14700L, 10)]
        [TestCase(500, 249500L, 149700L, 10)]
        [TestCase(550, 274500L, 164700L, 10)]
        [TestCase(int.MaxValue, 1073741823000L, 644245093800L, 10)]
        public void ActualFloorBudgetDoesNotResetAtTemplateBoundaries098(int floor, long hp, long offense, int unions)
        {
            Assert.That(TowerThreatRules098.HpBonusBasisPoints098(floor), Is.EqualTo(hp));
            Assert.That(TowerThreatRules098.OffenseBonusBasisPoints098(floor), Is.EqualTo(offense));
            Assert.That(TowerThreatRules098.EnemyUnionCount098(floor), Is.EqualTo(unions));
            Assert.That(TowerThreatRules098.ReadFloor098(new[] { TowerThreatRules098.Modifier098(floor) }), Is.EqualTo(floor));
            var scaled = TowerThreatRules098.Apply098(BaseUnion098(), floor);
            Assert.That(scaled.Members.Count, Is.EqualTo(3));
            Assert.That(scaled.Members[0].MaximumHp, Is.GreaterThan(0).And.LessThanOrEqualTo(TowerThreatRules098.MaximumMemberHp098));
            Assert.That(scaled.Members[0].Attack, Is.GreaterThan(0).And.LessThanOrEqualTo(TowerThreatRules098.MaximumMemberOffense098));
        }

        [Test]
        public void FirstTenFloorStatBundlesMatchExisting089Exactly098()
        {
            var original = BaseUnion098();
            var legacy = typeof(M2BattleCommandService).GetMethod("ApplyTowerThreatPower089",
                BindingFlags.NonPublic | BindingFlags.Static);
            for (var floor = 1; floor <= 10; floor++)
            {
                var expected = (BattleUnionState)legacy.Invoke(null, new object[] { original, floor });
                Assert.That(CanonicalJson.Serialize(TowerThreatRules098.Apply098(original, floor)),
                    Is.EqualTo(CanonicalJson.Serialize(expected)), "first-ten compatibility, floor " + floor);
                Assert.That(TowerThreatRules098.EnemyUnionCount098(floor),
                    Is.EqualTo(CampaignProgressionCommandService022.TowerEnemyUnionCount081(floor, 0)));
            }
        }

        [Test]
        public void ScalingIsMonotonicAndPreservesLegalArtsGearMpInjuryAndFormation098()
        {
            var original = BaseUnion098();
            var old = original;
            foreach (var floor in new[] { 1, 10, 11, 20, 21, 50, 500, 550, int.MaxValue })
            {
                var current = TowerThreatRules098.Apply098(original, floor);
                for (var i = 0; i < current.Members.Count; i++)
                {
                    var member = current.Members[i];
                    Assert.That(member.MaximumHp, Is.GreaterThanOrEqualTo(old.Members[i].MaximumHp));
                    Assert.That(member.Attack, Is.GreaterThanOrEqualTo(old.Members[i].Attack));
                    Assert.That(member.MagicAttack, Is.GreaterThanOrEqualTo(old.Members[i].MagicAttack));
                    Assert.That(member.MemberId, Is.EqualTo(original.Members[i].MemberId));
                    Assert.That(member.EquipmentTags, Is.EqualTo(original.Members[i].EquipmentTags));
                    Assert.That(member.LearnedArtIds, Is.EqualTo(original.Members[i].LearnedArtIds));
                    Assert.That(member.CurrentMp, Is.EqualTo(original.Members[i].CurrentMp));
                    Assert.That(member.MaximumMp, Is.EqualTo(original.Members[i].MaximumMp));
                    Assert.That(member.Downed, Is.EqualTo(original.Members[i].Downed));
                    Assert.That(member.Guarding, Is.EqualTo(original.Members[i].Guarding));
                    Assert.That(member.VisualVariantSeed090, Is.EqualTo(original.Members[i].VisualVariantSeed090));
                }
                Assert.That(current.Members[1].CurrentHp * 2, Is.EqualTo(current.Members[1].MaximumHp));
                Assert.That(current.Members[2].CurrentHp, Is.Zero);
                Assert.That(current.CurrentAp, Is.InRange(0, 999));
                Assert.That(current.Cohesion, Is.InRange(0, 100));
                Assert.That(current.FormationId, Is.EqualTo(original.FormationId));
                old = current;
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NonpositiveFloorCannotCreateAWeakerProfile098(int floor)
        { Assert.Throws<ArgumentOutOfRangeException>(() => TowerThreatRules098.Modifier098(floor)); }

        [TestCase("TOWER_ACTUAL_FLOOR098_0")]
        [TestCase("TOWER_ACTUAL_FLOOR098_-1")]
        [TestCase("TOWER_ACTUAL_FLOOR098_01")]
        [TestCase("TOWER_ACTUAL_FLOOR098_2147483648")]
        [TestCase("TOWER_ACTUAL_FLOOR098_x")]
        public void MalformedReservedProfileFailsClosed098(string modifier)
        { Assert.Throws<InvalidOperationException>(() => TowerThreatRules098.ReadFloor098(new[] { modifier })); }

        [Test]
        public void DuplicateOrMixedLegacyModifiersCannotDoubleApply098()
        {
            var current = TowerThreatRules098.Modifier098(11);
            Assert.Throws<InvalidOperationException>(() => TowerThreatRules098.ReadFloor098(new[] { current, current }));
            Assert.Throws<InvalidOperationException>(() => TowerThreatRules098.ReadFloor098(new[] { current, "TOWER_THREAT_TIER_10" }));
            var method = typeof(M2BattleCommandService).GetMethod("ApplyRouteModifiersToEnemies", BindingFlags.Static | BindingFlags.NonPublic);
            var source = new[] { BaseUnion098() };
            var applied = (IReadOnlyList<BattleUnionState>)method.Invoke(null, new object[] { source, new[] { current } });
            Assert.That(CanonicalJson.Serialize(applied[0]), Is.EqualTo(CanonicalJson.Serialize(TowerThreatRules098.Apply098(source[0], 11))));
        }

        [Test]
        public void SaturatingArithmeticNeverAddsOrMultipliesBeforeBounding098()
        {
            Assert.That(TowerThreatRules098.ScaleStat098(int.MaxValue,
                TowerThreatRules098.HpBonusBasisPoints098(int.MaxValue), TowerThreatRules098.MaximumMemberHp098),
                Is.EqualTo(TowerThreatRules098.MaximumMemberHp098));
            Assert.That(TowerThreatRules098.ScaleStat098(int.MaxValue, long.MaxValue,
                TowerThreatRules098.MaximumMemberOffense098), Is.EqualTo(TowerThreatRules098.MaximumMemberOffense098));
            Assert.That(TowerThreatRules098.ScaleStat098(1, long.MaxValue, int.MaxValue), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void CappedOffenseAndMaximumMasteryPreserveExistingCatalogDamageArithmetic098()
        {
            var member = TowerThreatRules098.Apply098(BaseUnion098(), int.MaxValue).Members[0];
            var method = typeof(M2BattleCommandService).GetMethod("PredictedHpDelta",
                BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var entry in _combat.Arts)
            {
                // Numeric boundary only: this does not grant an Art or bypass
                // its equipment/learning/AP/MP eligibility in real Forecasts.
                var baseDamage = 10L + member.Attack * 135L / 100L;
                var scaled = Math.Max(1L, baseDamage * entry.Value.PowerCoefficientPermille / 1000L);
                var expected = Math.Max(1L, scaled * M2ArtMasteryLevelPolicy088.MaximumPowerPermille / 1000L);
                Assert.That(expected, Is.LessThanOrEqualTo(int.MaxValue), entry.Key);
                var actual = (int)method.Invoke(null, new object[] { member, member,
                    BattleActionKind.Mystic, "CMD_ALL_OUT", true, entry.Value,
                    M2ArtMasteryLevelPolicy088.MaximumPowerPermille, null });
                Assert.That(actual, Is.EqualTo(-(int)expected), entry.Key);
            }
        }

        [Test]
        public void ActualOldPending098LoadRecommitAndStatsRemainByteExact()
        {
            var path = Path.Combine(_temporary, "OldPending.json");
            File.Copy(FixturePath098("R108_OldPendingFloor001.json"), path);
            var bytes = File.ReadAllBytes(path);
            var before = CanonicalJson.Sha256Hex(_oldPending);
            var request = CanonicalJson.Serialize(_oldPending.Guild.GuildCity.PendingEncounter);
            var battle = CanonicalJson.Serialize(_oldPending.Battle);
            var coordinator = new M1RuntimeCoordinator(ContentRoot098(), path);
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            var committed = Require098(_tower.CommitAbyssBattleEncounter(Clone098(_oldPending), _registry));
            Assert.That(CanonicalJson.Sha256Hex(committed), Is.EqualTo(before));
            Assert.That(CanonicalJson.Serialize(committed.Guild.GuildCity.PendingEncounter), Is.EqualTo(request));
            Assert.That(CanonicalJson.Serialize(committed.Battle), Is.EqualTo(battle));
            Assert.That(committed.Guild.GuildCity.PendingEncounter.RouteModifiers, Does.Contain("TOWER_THREAT_TIER_01"));
            Assert.That(committed.Guild.GuildCity.PendingEncounter.BattleId, Does.Not.Contain("ACTUAL098"));
        }

        [Test]
        public void GenuineOldTenClearsThenNewElevenBindsOnceAndReplaysIdentically098()
        {
            var sourceHash = CanonicalJson.Sha256Hex(_oldCompleted);
            var oldBindings = _oldCompleted.Guild.Development.AppliedAdventureAuthorityIds
                .Where(value => value.StartsWith("TOWER_FLOOR094|", StringComparison.Ordinal)).ToArray();
            var first = PrepareNewEleven098();
            var second = PrepareNewEleven098();
            var request = first.Guild.GuildCity.PendingEncounter;
            Assert.That(request.RouteModifiers, Does.Contain("TOWER_ACTUAL_FLOOR098_11"));
            Assert.That(request.RouteModifiers.Any(value => value.StartsWith("TOWER_THREAT_TIER_", StringComparison.Ordinal)), Is.False);
            Assert.That(request.EnemyUnionCount, Is.EqualTo(6));
            Assert.That(request.BattleId, Does.StartWith("ABYSS_BATTLE022_FLOOR_01_ACTUAL098_11_"));
            Assert.That(CanonicalJson.Serialize(second.Guild.GuildCity.PendingEncounter), Is.EqualTo(CanonicalJson.Serialize(request)));
            var reloaded = Clone098(first);
            Assert.That(CanonicalJson.Sha256Hex(Require098(_tower.CommitAbyssBattleEncounter(reloaded, _registry))),
                Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            Assert.That(_tower.HasActiveAbyssEncounter(reloaded, _registry), Is.True);
            Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(reloaded, _registry).Value.ActiveActualFloor, Is.EqualTo(11));
            Assert.That(oldBindings.All(reloaded.Guild.Development.AppliedAdventureAuthorityIds.Contains), Is.True);
            Assert.That(reloaded.Guild.Development.AppliedAdventureAuthorityIds.Count(value => value.Contains(TowerThreatRules098.Policy098)), Is.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(_oldCompleted), Is.EqualTo(sourceHash));
            Assert.That(reloaded.Guild.TreasuryXp, Is.EqualTo(_oldCompleted.Guild.TreasuryXp));
        }

        [Test]
        public void LiveCoordinatorNewElevenProducesScaledLegalEnemiesAndReloadKeepsCommittedBattle098()
        {
            var path = Path.Combine(_temporary, "NewEleven.json");
            File.Copy(FixturePath098("R100_OldCompletedFloor010.json"), path);
            var coordinator = new M1RuntimeCoordinator(ContentRoot098(), path);
            RequireCommand098(coordinator.BeginTowerRun081());
            for (var i = 0; !coordinator.CampaignProgression022.ActiveStepRequiresBattle && i < 24; i++)
                RequireCommand098(coordinator.AdvanceTowerRun081());
            Assert.That(coordinator.CampaignProgression022.ActiveStepRequiresBattle, Is.True);
            RequireCommand098(coordinator.EnterAbyssBattle022());
            var store = new AtomicSaveStore();
            var live = Require098(store.ReadWithRecovery(path));
            Assert.That(live.CampaignState.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(live.CampaignState.Battle.EnemyUnions.Count, Is.EqualTo(6));
            Assert.That(live.CampaignState.Battle.EnemyUnions.All(union => union.Members.Count >= 1 && union.Members.Count <= 6), Is.True);
            Assert.That(live.CampaignState.Battle.EventLog.Any(value => value.EventType == "TOWER_ACTUAL_FLOOR_098"), Is.True);
            Assert.That(live.CampaignState.Battle.EventLog.Any(value => value.EventType == "TOWER_THREAT_TIER_089"), Is.False);
            foreach (var member in live.CampaignState.Battle.EnemyUnions.SelectMany(union => union.Members))
                foreach (var art in member.LearnedArtIds)
                    Assert.That(_combat.Arts.ContainsKey(art), Is.True, "No new unsupported enemy Art: " + art);
            var bytes = File.ReadAllBytes(path);
            coordinator = new M1RuntimeCoordinator(ContentRoot098(), path);
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(live.CanonicalStateHash));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
        }

        [TestCase("ENDLESS_FLOOR_UNKNOWN_V1")]
        [TestCase("ENDLESS_FLOOR_094_V1")]
        public void ChangingNewBindingPolicyCannotDowngradeAlreadyCommittedStrength098(string substitutedPolicy)
        {
            var valid = PrepareNewEleven098();
            var json = CanonicalJson.Serialize(valid);
            var changed = JsonConvert.DeserializeObject<CampaignState>(json.Replace(TowerThreatRules098.Policy098, substitutedPolicy));
            Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(changed, _registry).IsSuccess, Is.False);
            Assert.That(_tower.CommitAbyssBattleEncounter(changed, _registry).IsSuccess, Is.False);
        }

        [Test]
        public void FreeFormBattleCannotSpoofNewMarkerWithoutOriginalCommittedAuthority098()
        {
            var result = new M2BattleCommandService().StartEncounterBattle(Clone098(_oldCompleted), _combat,
                TowerThreatRules098.BattleId098(11, "ABCDEF0123456789ABCDEF01"), "Spoof must be refused", 6,
                new[] { TowerThreatRules098.Modifier098(11) });
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(string.Join(";", result.Errors), Does.Contain("TOWER098_COMMITTED_REQUEST_REQUIRED"));
        }

        CampaignState PrepareNewEleven098()
        {
            var campaign = Require098(_tower.BeginTowerFloor094(Clone098(_oldCompleted), _registry));
            var state = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
            var operation = _registry.AbyssOperations[state.ActiveAbyssOperation.OperationDefinitionId];
            while (!operation.steps[state.ActiveAbyssOperation.CurrentStepIndex].requiresBattle)
            {
                campaign = Require098(_tower.CommitAbyssStep(campaign, _registry, "SUCCESS"));
                campaign = Require098(_tower.ApplyAbyssStep(campaign, _registry));
                state = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
            }
            return Require098(_tower.CommitAbyssBattleEncounter(campaign, _registry));
        }

        static BattleUnionState BaseUnion098()
        {
            var members = Enumerable.Range(0, 3).Select(index => new BattleMemberState("TOWER098_MEMBER_" + index,
                "Numeric fixture", "ENEMY", index == 2 ? 0 : index == 1 ? 100 : 200, 200, 20, 40, 30, 25,
                new[] { "SWORD" }, index == 2, index == 2, index == 0,
                new[] { "ART_BASIC_SABER_CUT" }, 0, 0, string.Empty, visualVariantSeed090: 42)).ToArray();
            return new BattleUnionState("TOWER098_UNION", "Numeric fixture", BattleSide.Enemy, members[0].MemberId,
                members, "FORMATION_SKIRMISH_LINE", "Skirmish", true, string.Empty, 12, 14, 80, 10000,
                EngagementState.Open, false, false, 0);
        }
        static string ContentRoot098() => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static string FixturePath098(string name) => Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", name);
        static CampaignState Fixture098(string name)
        { var value = Require098(new AtomicSaveStore().ReadWithRecovery(FixturePath098(name)));
            Assert.That(CanonicalJson.Sha256Hex(value.CampaignState), Is.EqualTo(value.CanonicalStateHash)); return value.CampaignState; }
        static CampaignState Clone098(CampaignState campaign) => JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(campaign));
        static T Require098<T>(Result<T> result)
        { Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value; }
        static void RequireCommand098(M1CommandResult result) => Assert.That(result.Succeeded, Is.True, result.Message);
    }
}
