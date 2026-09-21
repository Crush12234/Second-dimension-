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
    // Authority regression fixtures, not evidence of a rendered Tower soak.
    // Historical saves are copied; victories below use real combat commands.
    public sealed class TowerEnemyParties137Tests
    {
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "SecondDimension_TowerParties137");
        static readonly FieldInfo CampaignField = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo CommittedEntry137 = typeof(M2BattleCommandService).GetMethod(
            "StartCommittedEncounterBattle017D", BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(CampaignState), typeof(M2CombatContent), typeof(EncounterLaunchRequest017D), typeof(EncounterRoster070) }, null);
        readonly CampaignProgressionCommandService022 _tower = new CampaignProgressionCommandService022();
        readonly M2BattleCommandService _battles = new M2BattleCommandService();
        CampaignRegistry022 _registry; M2CombatContent _combat; EncounterRosterResolver070 _resolver;
        string _directory;

        [OneTimeSetUp] public void LoadShippingAuthority137()
        {
            _registry = CampaignRegistry022.LoadFromResources();
            _combat = M2CombatContent.LoadFromDirectory(ContentRoot);
            _resolver = EncounterRosterResolver070.LoadFromContentRoot(ContentRoot);
        }
        [SetUp] public void Isolate137()
        { _directory = Path.Combine(TempRoot, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_directory); }
        [TearDown] public void RemoveOnlyOwnedCopy137()
        {
            if (_directory == null || !Directory.Exists(_directory)) return;
            var full = Path.GetFullPath(_directory); var root = Path.GetFullPath(TempRoot).TrimEnd(Path.DirectorySeparatorChar);
            Assert.That(full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                Path.GetDirectoryName(full) == root && Guid.TryParseExact(Path.GetFileName(full), "N", out _), Is.True);
            Assert.That((File.GetAttributes(root) & FileAttributes.ReparsePoint) == 0 &&
                (File.GetAttributes(full) & FileAttributes.ReparsePoint) == 0, Is.True);
            Directory.Delete(full, true);
        }

        [Test]
        public void RealNewElevenCommitStartsSixPerUnionWithUnchangedFloorStatAuthority137()
        {
            var old = Fixture137("R100_OldCompletedFloor010.json"); var originalHash = CanonicalJson.Sha256Hex(old);
            var committed = NewEleven137(old); var request = committed.Guild.GuildCity.PendingEncounter;
            Assert.That(TowerEnemyPartyRules137.ReadCommitted137(request), Is.True);
            Assert.That(TowerThreatRules098.ReadFloor098(request.RouteModifiers), Is.EqualTo(11));
            Assert.That(request.EnemyUnionCount, Is.EqualTo(6));
            Assert.That(CampaignProgressionCommandService022.UsesFullTowerParties137(committed, Progress137(committed).ActiveAbyssOperation), Is.True);
            Assert.That(request.RouteModifiers.Any(t => t.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)), Is.False);
            var roster = _resolver.Resolve(committed.CampaignSeed, request); AssertFull137(request, roster);
            var constructor = typeof(M2BattleCommandService).GetMethod("CreateEnemyUnions070", BindingFlags.Static | BindingFlags.NonPublic);
            var baseUnions = (IReadOnlyList<BattleUnionState>)constructor.Invoke(null, new object[] { _combat, roster });
            var expected = EnemyArtIdentity090.CommitIdentities090(baseUnions.Select(u =>
                TowerScalingRules138.ReadCommitted138(request) ? TowerScalingRules138.Apply138(u, 11) : TowerThreatRules098.Apply098(u, 11)).ToArray(), request.BattleId);
            var started = Require137(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(committed, _battles, _combat, _resolver));
            Assert.That(CanonicalJson.Serialize(started.Battle.EnemyUnions), Is.EqualTo(CanonicalJson.Serialize(expected)),
                "Composition must not add Campaign pressure stats, change the existing floor scalar, Arts, AP or formations.");
            var loaded = Clone137(started);
            Assert.That(CanonicalJson.Sha256Hex(Require137(_tower.CommitAbyssBattleEncounter(loaded, _registry))), Is.EqualTo(CanonicalJson.Sha256Hex(started)));
            Assert.That(CanonicalJson.Sha256Hex(old), Is.EqualTo(originalHash));

            var tampered = Clone137(committed);
            var changed = Request137(request, request.RouteModifiers.Where(t => t != TowerEnemyPartyRules137.Modifier137).ToArray());
            tampered = tampered.With(tampered.Guild.WithGuildCity(tampered.Guild.GuildCity.With(pendingEncounter: changed, replacePendingEncounter: true)), tampered.OpeningFlow);
            Assert.That(_tower.CommitAbyssBattleEncounter(tampered, _registry).IsSuccess, Is.False,
                "Removing the party marker cannot downgrade an already bound new floor.");
            Assert.That(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(tampered, _battles, _combat, _resolver).IsSuccess, Is.False);
        }

        [TestCase(1, 1)] [TestCase(3, 2)] [TestCase(11, 6)] [TestCase(19, 10)] [TestCase(325, 10)]
        public void RequestContractFixturesKeepRampAndDeterministicNonBossEscorts137(int floor, int unions)
        {
            var raw = Fixture137("R108_OldPendingFloor001.json").Guild.GuildCity.PendingEncounter;
            var request = Request137(raw, new[] { TowerThreatRules098.Modifier098(floor), TowerEnemyPartyRules137.Modifier137 }, unions);
            var resolved = _resolver.Resolve(137, request); AssertFull137(request, resolved);
            Assert.That(request.EnemyUnionCount, Is.EqualTo(TowerThreatRules098.EnemyUnionCount098(floor)));
            Assert.That(CanonicalJson.Serialize(_resolver.Resolve(137, JsonConvert.DeserializeObject<EncounterLaunchRequest017D>(CanonicalJson.Serialize(request)))),
                Is.EqualTo(CanonicalJson.Serialize(resolved)), "Reopening the same request must not reroll escorts or spawn IDs.");
            foreach (var union in resolved.Unions)
                foreach (var member in union.Members.Skip(union.SourceDefinition.MemberIds.Count))
                    Assert.That(IsNamedBoss137(member.FamilyId), Is.False, "Additional escort cannot duplicate a named boss.");
        }

        [TestCase("unknown")] [TestCase("five")] [TestCase("two-markers")] [TestCase("no-floor")]
        [TestCase("legacy-mix")] [TestCase("campaign-mix")] [TestCase("wrong-count")]
        public void InvalidOrMixedPartyRequestFailsBeforeRosterReveal137(string alteration)
        {
            var raw = Fixture137("R108_OldPendingFloor001.json").Guild.GuildCity.PendingEncounter;
            var tags = new List<string> { TowerThreatRules098.Modifier098(11), TowerEnemyPartyRules137.Modifier137 };
            if (alteration == "unknown") tags[1] = "TOWER_PARTY137_V2_M06";
            if (alteration == "five") tags[1] = "TOWER_PARTY137_V1_M05";
            if (alteration == "two-markers") tags.Add("TOWER_PARTY137_V0_M06");
            if (alteration == "no-floor") tags.RemoveAt(0);
            if (alteration == "legacy-mix") tags.Add("TOWER_THREAT_TIER_10");
            if (alteration == "campaign-mix") tags.Add(EnemyForceProfile094.ForNewChapter135(25, 10).Tag094);
            var bad = Request137(raw, tags, alteration == "wrong-count" ? 5 : 6);
            Assert.Throws<InvalidOperationException>(() => TowerEnemyPartyRules137.ReadCommitted137(bad));
            Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(137, bad));
        }

        [TestCase("short")] [TestCase("duplicate")] [TestCase("suffix")] [TestCase("seed")] [TestCase("bosses")]
        public void TamperedResolvedPartyCannotEnterBattle137(string alteration)
        {
            var state = NewEleven137(Fixture137("R100_OldCompletedFloor010.json"));
            var request = state.Guild.GuildCity.PendingEncounter; var roster = _resolver.Resolve(state.CampaignSeed, request);
            Assert.That(AdmitCommitted137(state, request, roster).IsSuccess, Is.True,
                "Positive control: this exact committed admission must accept the valid roster.");
            var unions = roster.Unions.ToArray(); var selected = unions[1]; var members = selected.Members.ToArray();
            if (alteration == "short") members = members.Take(5).ToArray();
            if (alteration == "duplicate" || alteration == "suffix")
                members[0] = new EncounterEnemyMember070(alteration == "duplicate" ? unions[0].Members[0].MemberId : members[0].MemberId + "_TAMPERED",
                    members[0].SourceEnemyId, members[0].FamilyId, members[0].VisualVariantSeed, members[0].Definition);
            if (alteration == "bosses")
                for (var index = 0; index < 2; index++) members[index] = new EncounterEnemyMember070(members[index].MemberId,
                    members[index].SourceEnemyId, "ENEMY_FAMILY_GATEHEART_WARDEN", members[index].VisualVariantSeed, members[index].Definition);
            unions[1] = new EncounterEnemyUnion070(selected.UnionId, selected.SourceUnionId, selected.SourceDefinition, members, selected.FamilyIds);
            var bad = new EncounterRoster070(roster.RosterId, roster.CanonicalSeedIdentity + (alteration == "seed" ? "_TAMPERED" : ""), unions, roster.FamilyIds);
            Assert.Throws<InvalidOperationException>(() => TowerEnemyPartyRules137.ValidateRoster137(request, bad));
            var refused = AdmitCommitted137(state, request, bad);
            Assert.That(refused.IsSuccess, Is.False);
            Assert.That(string.Join("; ", refused.Errors), Does.Contain("TOWER137_"),
                "The committed admission must reject the actual composition defect, not missing request authority.");
        }

        [Test]
        public void ExistingLegacyRequestRosterBattleAndRewardHistoryRemainExact137()
        {
            var pending = Fixture137("R108_OldPendingFloor001.json");
            var before = CanonicalJson.Serialize(pending); var request = pending.Guild.GuildCity.PendingEncounter;
            Assert.That(TowerEnemyPartyRules137.ReadCommitted137(request), Is.False);
            Assert.That(CanonicalJson.Serialize(Require137(_tower.CommitAbyssBattleEncounter(Clone137(pending), _registry))), Is.EqualTo(before));
            AssertLegacyRoster137(pending.CampaignSeed, request);
            var completed = Fixture137("R100_OldCompletedFloor010.json"); var completedHash = CanonicalJson.Sha256Hex(completed);
            Assert.That(Require137(CampaignProgressionCommandService022.DescribeTowerFloors094(Clone137(completed), _registry)).HighestActualFloor, Is.EqualTo(10));
            Assert.That(CanonicalJson.Sha256Hex(completed), Is.EqualTo(completedHash));
            Assert.That(CanonicalJson.Serialize(Clone137(completed).Battle.Reward), Is.EqualTo(CanonicalJson.Serialize(completed.Battle.Reward)));
        }

        [Test]
        public void ExistingLegacyBattleFinishesThroughCommandsThenNewFloorClaimsOnlyOnce137()
        {
            var source = Fixture137("R108_OldPendingFloor001.json"); var sourceHash = CanonicalJson.Sha256Hex(source);
            var state = source;
            for (var round = 0; state.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                Assert.That(round, Is.LessThan(120), "Bounded real-command legacy fixture did not reach a terminal outcome.");
                foreach (var union in state.Battle.PlayerUnions.Where(u => !u.IsDefeated && !u.Retreated).ToArray())
                {
                    var forecast = state.Battle.CommittedForecasts.Where(f => f.UnionId == union.UnionId && f.SharedApCost <= union.CurrentAp)
                        .Where(f => f.CommandId == "CMD_ALL_OUT" || f.CommandId == "CMD_BALANCED")
                        .OrderBy(f => f.MemberActions.Sum(a => a.PredictedHpDelta)).ThenBy(f => f.ForecastId, StringComparer.Ordinal).FirstOrDefault();
                    Assert.That(forecast, Is.Not.Null); state = Require137(_battles.SelectForecast(state, union.UnionId, forecast.ForecastId));
                }
                state = Require137(_battles.ConfirmRound(state, _combat));
            }
            Assert.That(state.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory), "Never manufacture a victory to satisfy the transition fixture.");
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(state.Battle), Is.True);
            var rewardId = state.Battle.Reward.RewardId; var path = Path.Combine(_directory, "CommandEarnedLegacyVictory137.json");
            new AtomicSaveStore().Write(path, SaveEnvelopeV1.Create(state, DateTime.UtcNow));
            var owner = new M1RuntimeCoordinator(ContentRoot, path);
            Command137(owner.ClaimBattleRewards()); var claimed = CanonicalJson.Sha256Hex(State137(owner));
            Command137(owner.ClaimBattleRewards()); Assert.That(CanonicalJson.Sha256Hex(State137(owner)), Is.EqualTo(claimed));
            Command137(owner.BankTowerVictory110()); var banked = State137(owner); var bankedHash = CanonicalJson.Sha256Hex(banked);
            Assert.That(banked.Guild.Development.ClaimedBattleRewardIds.Count(id => id == rewardId), Is.EqualTo(1));
            Assert.That(source.Guild.Development.ClaimedBattleRewardIds.All(banked.Guild.Development.ClaimedBattleRewardIds.Contains), Is.True);
            Assert.That(source.Guild.Recruits.All(r => banked.Guild.Recruits.Any(a => a.RecruitId == r.RecruitId && a.Progression.TotalPersonalXp >= r.Progression.TotalPersonalXp)), Is.True);
            Assert.That(ItemIds137(source).All(ItemIds137(banked).Contains), Is.True);
            Assert.That(owner.BankTowerVictory110().Succeeded, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(State137(owner)), Is.EqualTo(bankedHash));
            owner = new M1RuntimeCoordinator(ContentRoot, path); Assert.That(CanonicalJson.Sha256Hex(State137(owner)), Is.EqualTo(bankedHash));
            Command137(owner.StartTowerBattle110()); var next = State137(owner); var request = next.Guild.GuildCity.PendingEncounter;
            Assert.That(TowerEnemyPartyRules137.ReadCommitted137(request), Is.True);
            Assert.That(TowerThreatRules098.ReadFloor098(request.RouteModifiers), Is.EqualTo(2));
            Assert.That(next.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(next.Battle.BattleId, Is.Not.EqualTo(state.Battle.BattleId));
            Assert.That(next.Battle.EnemyUnions.All(u => u.Members.Count == 6), Is.True);
            Assert.That(next.Guild.Development.ClaimedBattleRewardIds, Is.EqualTo(banked.Guild.Development.ClaimedBattleRewardIds));
            Assert.That(next.Guild.TreasuryXp, Is.EqualTo(banked.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(sourceHash));
            TestContext.Progress.WriteLine("AUTHORITY FIXTURE: preserved legacy floor1, real combat commands, one claim/bank, new137 floor2 started. Not a rendered soak.");
        }

        [Test]
        public void ExplicitEarnedV1CheckpointReconstructsWithoutRetaggingBattleOrHistory137()
        {
            var source = Environment.GetEnvironmentVariable("SD_TOWER137_EARNED_FIXTURE");
            if (string.IsNullOrWhiteSpace(source)) Assert.Ignore("Set SD_TOWER137_EARNED_FIXTURE and SD_TOWER137_EARNED_SHA256 for the protected earned evidence check.");
            var expectedHash = Environment.GetEnvironmentVariable("SD_TOWER137_EARNED_SHA256");
            var full = Path.GetFullPath(source);
            Assert.That(full.StartsWith(@"C:\SecondDimension\BuildEvidence\Reset110\", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(string.IsNullOrWhiteSpace(expectedHash), Is.False);
            Assert.That(Hash137(full), Is.EqualTo(expectedHash).IgnoreCase);
            var path = Path.Combine(_directory, "EarnedV1Copy137.json"); File.Copy(full, path, false);
            try
            {
                Assert.That(Hash137(path), Is.EqualTo(expectedHash).IgnoreCase);
                var saved = Require137(new AtomicSaveStore().ReadWithRecovery(path)); var old = saved.CampaignState;
                var request = old.Guild.GuildCity.PendingEncounter;
                Assert.That(request, Is.Not.Null); Assert.That(old.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
                Assert.That(TowerThreatRules098.ReadFloor098(request.RouteModifiers), Is.GreaterThan(0));
                Assert.That(TowerEnemyPartyRules137.ReadCommitted137(request), Is.False);
                Assert.That(CanonicalJson.Sha256Hex(Require137(_tower.CommitAbyssBattleEncounter(Clone137(old), _registry))), Is.EqualTo(saved.CanonicalStateHash));
                AssertLegacyRoster137(old.CampaignSeed, request);
                var owner = new M1RuntimeCoordinator(ContentRoot, path);
                Assert.That(CanonicalJson.Sha256Hex(State137(owner)), Is.EqualTo(saved.CanonicalStateHash));
                Assert.That(Hash137(path), Is.EqualTo(expectedHash).IgnoreCase, "Existing initialized battle and all history must remain byte-identical.");
                TestContext.Progress.WriteLine("IMMUTABLE EARNED V1 CHECKPOINT: " + expectedHash + "; exact request/battle/reward/history retained.");
            }
            finally { Assert.That(Hash137(full), Is.EqualTo(expectedHash).IgnoreCase, "Original evidence was modified."); }
        }

        CampaignState NewEleven137(CampaignState source)
        {
            var state = Require137(_tower.BeginTowerFloor094(Clone137(source), _registry));
            for (var i = 0; i < 24; i++)
            {
                var active = Progress137(state).ActiveAbyssOperation; var operation = _registry.AbyssOperations[active.OperationDefinitionId];
                if (operation.steps[active.CurrentStepIndex].requiresBattle) return Require137(_tower.CommitAbyssBattleEncounter(state, _registry));
                state = Require137(_tower.ApplyAbyssStep(Require137(_tower.CommitAbyssStep(state, _registry, "SUCCESS")), _registry));
            }
            Assert.Fail("No authored Tower battle step was reached."); return null;
        }
        void AssertFull137(EncounterLaunchRequest017D request, EncounterRoster070 roster)
        {
            TowerEnemyPartyRules137.ValidateRoster137(request, roster);
            Assert.That(roster.Unions.Count, Is.EqualTo(request.EnemyUnionCount));
            Assert.That(roster.Unions.All(u => u.Members.Count == 6), Is.True);
            var members = roster.Unions.SelectMany(u => u.Members).ToArray();
            Assert.That(members.Select(m => m.MemberId).Distinct().Count(), Is.EqualTo(6 * request.EnemyUnionCount));
            Assert.That(members.All(m => m.MemberId.EndsWith(TowerEnemyPartyRules137.SpawnSuffix137, StringComparison.Ordinal)), Is.True);
            Assert.That(members.Count(m => IsNamedBoss137(m.FamilyId)), Is.LessThanOrEqualTo(1));
        }
        void AssertLegacyRoster137(long seed, EncounterLaunchRequest017D request)
        {
            var frozen = _resolver.Resolve(seed, request.ContractId, request.BoardId, request.EncounterId, request.EnemyUnionCount, request.CanonicalSeedIdentity);
            Assert.That(CanonicalJson.Serialize(_resolver.Resolve(seed, request)), Is.EqualTo(CanonicalJson.Serialize(frozen)),
                "Marker-free V1/legacy request must use the original exact roster constructor.");
        }
        CampaignState Fixture137(string name)
        {
            var copy = Path.Combine(_directory, name);
            if (!File.Exists(copy)) File.Copy(Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", name), copy, false);
            return Require137(new AtomicSaveStore().ReadWithRecovery(copy)).CampaignState;
        }
        static EncounterLaunchRequest017D Request137(EncounterLaunchRequest017D r, IReadOnlyList<string> tags, int? unions = null) =>
            new EncounterLaunchRequest017D(r.RequestId, r.ContractId, r.ExpeditionId, r.BoardId, r.NodeId, r.EncounterId,
                r.BattleId, r.Objective, unions ?? r.EnemyUnionCount, r.CanonicalSeedIdentity, r.AlliedUnionIds,
                r.ReserveUnionIds, r.ObjectiveIds, tags, r.Supplies, r.Fatigue, r.Urgency, r.ReturnCheckpointId, r.PreBattleStateHash);
        static CampaignProgressionState022 Progress137(CampaignState s) => s.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static CampaignState State137(M1RuntimeCoordinator c) => (CampaignState)CampaignField.GetValue(c);
        static CampaignState Clone137(CampaignState s) => JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(s));
        static string[] ItemIds137(CampaignState s) => s.Guild.Inventory.Select(i => i.InstanceId).Concat(s.Guild.Recruits.SelectMany(r => r.Equipment.Assignments).Select(a => a.Item.InstanceId)).ToArray();
        Result<CampaignState> AdmitCommitted137(CampaignState state, EncounterLaunchRequest017D request, EncounterRoster070 roster)
        {
            Assert.That(CommittedEntry137, Is.Not.Null, "The exact shipping committed admission seam must exist.");
            return (Result<CampaignState>)CommittedEntry137.Invoke(_battles, new object[] { state, _combat, request, roster });
        }
        static bool IsNamedBoss137(string family) => family == "ENEMY_FAMILY_HINGE_EATER_COLOSSUS" ||
            family == "ENEMY_FAMILY_CAPTAIN_RAVEL" || family == "ENEMY_FAMILY_GATEHEART_WARDEN";
        static void Command137(M1CommandResult result) => Assert.That(result.Succeeded, Is.True, result.Message);
        static T Require137<T>(Result<T> result) { Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value; }
        static string Hash137(string path) { using (var file = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", ""); }
    }
}
