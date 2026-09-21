using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
    // Authority contracts and immutable evidence copies; not rendered combat results.
    public sealed class TowerScaling138Tests
    {
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "SecondDimension_TowerScaling138");
        readonly CampaignProgressionCommandService022 _tower = new CampaignProgressionCommandService022();
        string _directory;
        [SetUp] public void CreateCopyDirectory138()
        { _directory = Path.Combine(TempRoot, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_directory); }
        [TearDown] public void RemoveOnlyOwnedCopyDirectory138()
        {
            if (_directory == null || !Directory.Exists(_directory)) return;
            var full = Path.GetFullPath(_directory); var root = Path.GetFullPath(TempRoot).TrimEnd(Path.DirectorySeparatorChar);
            Assert.That(full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                Path.GetDirectoryName(full) == root && Guid.TryParseExact(Path.GetFileName(full), "N", out _), Is.True);
            Assert.That((File.GetAttributes(root) & FileAttributes.ReparsePoint) == 0 &&
                (File.GetAttributes(full) & FileAttributes.ReparsePoint) == 0, Is.True);
            Directory.Delete(full, true);
        }

        [TestCase(1, 0L, 0L)] [TestCase(10, 4500L, 2700L)] [TestCase(11, 6500L, 4700L)]
        [TestCase(25, 34500L, 32700L)] [TestCase(81, 146500L, 144700L)]
        [TestCase(82, 149500L, 147200L)] [TestCase(100, 203500L, 192200L)]
        [TestCase(350, 953500L, 817200L)]
        public void FixedCurveMatchesApprovedAbsoluteBreakpoints138(int floor, long hp, long offense)
        {
            Assert.That(TowerScalingRules138.HpBonusBasisPoints138(floor), Is.EqualTo(hp));
            Assert.That(TowerScalingRules138.OffenseBonusBasisPoints138(floor), Is.EqualTo(offense));
            var scaled = TowerScalingRules138.Apply138(Union138(), floor);
            Assert.That(scaled.Members[0].MaximumHp, Is.EqualTo((100L * (10000 + hp) + 9999) / 10000));
            Assert.That(scaled.Members[0].Attack, Is.EqualTo((20L * (10000 + offense) + 9999) / 10000));
            Assert.That(scaled.Members.Count, Is.EqualTo(3), "Scaling never changes composition.");
        }

        [Test]
        public void CurveIsMonotonicBoundedAndKeepsOpeningApArtsAndInjurySemantics138()
        {
            var original = Union138(); var sourceHash = CanonicalJson.Sha256Hex(original); var previous = original;
            foreach (var floor in new[] { 1, 9, 10, 11, 25, 80, 81, 82, 100, 350, 2000, int.MaxValue })
            {
                var actual = TowerScalingRules138.Apply138(original, floor);
                var old = TowerThreatRules098.Apply098(original, floor);
                if (floor <= 10) Assert.That(CanonicalJson.Serialize(actual), Is.EqualTo(CanonicalJson.Serialize(old)));
                Assert.That(actual.Members[0].MaximumHp, Is.GreaterThanOrEqualTo(previous.Members[0].MaximumHp).And.LessThanOrEqualTo(1000000));
                Assert.That(actual.Members[0].Attack, Is.GreaterThanOrEqualTo(previous.Members[0].Attack).And.LessThanOrEqualTo(10000));
                Assert.That(actual.CurrentAp, Is.EqualTo(old.CurrentAp)); Assert.That(actual.MaximumAp, Is.EqualTo(old.MaximumAp));
                Assert.That(actual.Cohesion, Is.EqualTo(old.Cohesion)); Assert.That(actual.FormationId, Is.EqualTo(old.FormationId));
                Assert.That(actual.Members[1].CurrentHp, Is.EqualTo(actual.Members[1].MaximumHp / 2));
                Assert.That(actual.Members[2].CurrentHp, Is.Zero); Assert.That(actual.Members[2].Downed, Is.True);
                for (var i = 0; i < original.Members.Count; i++)
                {
                    Assert.That(actual.Members[i].MemberId, Is.EqualTo(original.Members[i].MemberId));
                    Assert.That(actual.Members[i].LearnedArtIds, Is.EqualTo(original.Members[i].LearnedArtIds));
                    Assert.That(actual.Members[i].EquipmentTags, Is.EqualTo(original.Members[i].EquipmentTags));
                    Assert.That(actual.Members[i].CurrentMp, Is.EqualTo(original.Members[i].CurrentMp));
                    Assert.That(actual.Members[i].MaximumMp, Is.EqualTo(original.Members[i].MaximumMp));
                }
                previous = actual;
            }
            Assert.That(previous.Members[0].MaximumHp, Is.EqualTo(1000000));
            Assert.That(previous.Members[0].Attack, Is.EqualTo(10000));
            Assert.That(CanonicalJson.Sha256Hex(original), Is.EqualTo(sourceHash));
            Assert.Throws<ArgumentOutOfRangeException>(() => TowerScalingRules138.HpBonusBasisPoints138(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TowerScalingRules138.OffenseBonusBasisPoints138(-1));
        }

        [Test]
        public void Existing098And137RoutesStillUseExactFrozenOldStatResult138()
        {
            var source = new[] { Union138() };
            var apply = typeof(M2BattleCommandService).GetMethod("ApplyRouteModifiersToEnemies", BindingFlags.Static | BindingFlags.NonPublic);
            var floor = TowerThreatRules098.Modifier098(350);
            var old = TowerThreatRules098.Apply098(source[0], 350);
            Assert.That(old.Members[0].MaximumHp, Is.EqualTo(1845)); Assert.That(old.Members[0].Attack, Is.EqualTo(230));
            foreach (var routes in new[] { new[] { floor }, new[] { floor, TowerEnemyPartyRules137.Modifier137 } })
            {
                Assert.That(TowerScalingRules138.ReadFloor138(routes), Is.Zero);
                var result = (IReadOnlyList<BattleUnionState>)apply.Invoke(null, new object[] { source, routes });
                Assert.That(CanonicalJson.Serialize(result), Is.EqualTo(CanonicalJson.Serialize(new[] { old })));
            }
        }

        [TestCase("unknown")] [TestCase("changed-coefficient")] [TestCase("two-markers")]
        [TestCase("no-floor")] [TestCase("no-party")] [TestCase("legacy-mix")] [TestCase("campaign-mix")]
        public void MalformedAndMixedNewMarkersFailClosed138(string alteration)
        {
            var routes = new List<string> { TowerThreatRules098.Modifier098(81), TowerEnemyPartyRules137.Modifier137, TowerScalingRules138.Modifier138 };
            if (alteration == "unknown") routes[2] = "TOWER_SCALING138_V2_H20_O20_A81_H30_O25";
            if (alteration == "changed-coefficient") routes[2] = TowerScalingRules138.Modifier138.Replace("H30", "H31");
            if (alteration == "two-markers") routes.Add("TOWER_SCALING138_V0");
            if (alteration == "no-floor") routes.RemoveAt(0);
            if (alteration == "no-party") routes.RemoveAt(1);
            if (alteration == "legacy-mix") routes.Add("TOWER_THREAT_TIER_10");
            if (alteration == "campaign-mix") routes.Add(EnemyForceProfile094.ForNewChapter135(81, 10).Tag094);
            Assert.Throws<InvalidOperationException>(() => TowerScalingRules138.ReadFloor138(routes));
        }

        [Test]
        public void RealNewFloorCommitsCurveOnceAndStartsItsExactEnemyStats138()
        {
            var path = Path.Combine(_directory, "OldTen.json");
            File.Copy(Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", "R100_OldCompletedFloor010.json"), path);
            var original = Read138(path); var registry = CampaignRegistry022.LoadFromResources();
            var state = Require138(_tower.BeginTowerFloor094(original, registry));
            for (var guard = 0; guard < 24; guard++)
            {
                var active = state.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022.ActiveAbyssOperation;
                if (registry.AbyssOperations[active.OperationDefinitionId].steps[active.CurrentStepIndex].requiresBattle) break;
                state = Require138(_tower.ApplyAbyssStep(Require138(_tower.CommitAbyssStep(state, registry, "SUCCESS")), registry));
            }
            state = Require138(_tower.CommitAbyssBattleEncounter(state, registry));
            var request = state.Guild.GuildCity.PendingEncounter;
            Assert.That(TowerScalingRules138.ReadCommitted138(request), Is.True);
            Assert.That(TowerScalingRules138.ReadFloor138(request.RouteModifiers), Is.EqualTo(11));
            Assert.That(CanonicalJson.Sha256Hex(Require138(_tower.CommitAbyssBattleEncounter(Clone138(state), registry))), Is.EqualTo(CanonicalJson.Sha256Hex(state)));
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(ContentRoot); var combat = M2CombatContent.LoadFromDirectory(ContentRoot);
            var roster = resolver.Resolve(state.CampaignSeed, request);
            var construct = typeof(M2BattleCommandService).GetMethod("CreateEnemyUnions070", BindingFlags.Static | BindingFlags.NonPublic);
            var unscaled = (IReadOnlyList<BattleUnionState>)construct.Invoke(null, new object[] { combat, roster });
            var expected = EnemyArtIdentity090.CommitIdentities090(unscaled.Select(u => TowerScalingRules138.Apply138(u, 11)).ToArray(), request.BattleId);
            var started = Require138(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(state, new M2BattleCommandService(), combat, resolver));
            Assert.That(CanonicalJson.Serialize(started.Battle.EnemyUnions), Is.EqualTo(CanonicalJson.Serialize(expected)));
            Assert.That(started.Battle.EnemyUnions.Count, Is.EqualTo(6));
            Assert.That(started.Battle.EnemyUnions.All(u => u.Members.Count == 6), Is.True);
            var missing = JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(state).Replace(TowerScalingRules138.Modifier138, "TOWER_SCALING138_V0"));
            Assert.That(_tower.CommitAbyssBattleEncounter(missing, registry).IsSuccess, Is.False, "Committed curve cannot be retagged after reveal.");
        }

        [TestCase("SD_TOWER137_EARNED_FIXTURE", "SD_TOWER137_EARNED_SHA256", false)]
        [TestCase("SD_TOWER138_EARNED_FIXTURE", "SD_TOWER138_EARNED_SHA256", true)]
        public void ProtectedExisting098Or137SaveRetainsBattleRewardsAndHistoryBytes138(string sourceVariable, string hashVariable, bool fullParties)
        {
            var source = Environment.GetEnvironmentVariable(sourceVariable);
            if (string.IsNullOrWhiteSpace(source)) Assert.Ignore("Set " + sourceVariable + " and " + hashVariable + " for earned evidence.");
            source = Path.GetFullPath(source); var expected = Environment.GetEnvironmentVariable(hashVariable);
            Assert.That(source.StartsWith(@"C:\SecondDimension\BuildEvidence\Reset110\", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(string.IsNullOrWhiteSpace(expected), Is.False); Assert.That(Hash138(source), Is.EqualTo(expected).IgnoreCase);
            var copy = Path.Combine(_directory, "ProtectedEarnedCopy138.json"); File.Copy(source, copy, false);
            try
            {
                Assert.That(Hash138(copy), Is.EqualTo(expected).IgnoreCase);
                var old = Read138(copy); var canonical = CanonicalJson.Sha256Hex(old); var request = old.Guild.GuildCity.PendingEncounter;
                Assert.That(old.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
                Assert.That(TowerScalingRules138.ReadCommitted138(request), Is.False);
                Assert.That(TowerEnemyPartyRules137.ReadCommitted137(request), Is.EqualTo(fullParties));
                var registry = CampaignRegistry022.LoadFromResources();
                Assert.That(CanonicalJson.Sha256Hex(Require138(_tower.CommitAbyssBattleEncounter(Clone138(old), registry))), Is.EqualTo(canonical));
                Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(old, registry).IsSuccess, Is.True);
                var owner = new M1RuntimeCoordinator(ContentRoot, copy);
                Assert.That(owner.State.CanonicalStateHash, Is.EqualTo(canonical));
                Assert.That(Hash138(copy), Is.EqualTo(expected).IgnoreCase);
            }
            finally { Assert.That(Hash138(source), Is.EqualTo(expected).IgnoreCase, "Original earned evidence changed."); }
        }

        static BattleUnionState Union138()
        {
            var members = Enumerable.Range(0, 3).Select(i => new BattleMemberState("NUMERIC138_" + i, "Numeric contract fixture", "ENEMY",
                i == 2 ? 0 : i == 1 ? 50 : 100, 100, 20, 40, 20, 10, new[] { "SWORD" }, i == 2, i == 2, i == 0,
                new[] { "ART_BASIC_SABER_CUT" }, 0, 0, string.Empty, visualVariantSeed090: 42)).ToArray();
            return new BattleUnionState("NUMERIC138_UNION", "Numeric contract fixture", BattleSide.Enemy, members[0].MemberId, members,
                "FORMATION_SKIRMISH_LINE", "Skirmish", true, string.Empty, 12, 14, 80, 10000, EngagementState.Open, false, false, 0);
        }
        static CampaignState Read138(string path) => Require138(new AtomicSaveStore().ReadWithRecovery(path)).CampaignState;
        static CampaignState Clone138(CampaignState s) => JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(s));
        static T Require138<T>(Result<T> result) { Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value; }
        static string Hash138(string path) { using (var file = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", ""); }
    }
}
