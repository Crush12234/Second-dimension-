using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    // Reuses the established authored-content battle fixture. Numerical and
    // command/save regression coverage; the earned finale run is separate QA.
    public sealed partial class EnemyForceProfile094Tests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void FrozenVersionsKeepRequestRosterAndActualBattleStats138(int version)
        {
            var raw = Request094(1);
            var profile = version == 0 ? null : version == 1 ? EnemyForceProfile094.ForChapter094(35, 1)
                : version == 2 ? EnemyForceProfile094.ForNewChapter134(35, 1)
                : EnemyForceProfile094.ForNewChapter135(35, 1);
            var request = profile == null ? raw : profile.Apply094(raw);
            var campaign = RecordRequest094(CreateArmyFixture094(), request);
            var before = CanonicalJson.Sha256Hex(campaign);
            var loaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            var restored = EnemyForceProfile094.ForRequest094(loaded, raw, 35, false);
            Assert.That(CanonicalJson.Serialize(restored), Is.EqualTo(CanonicalJson.Serialize(request)));
            var roster = _resolver.Resolve(campaign.CampaignSeed, restored);
            Assert.That(CanonicalJson.Serialize(roster), Is.EqualTo(CanonicalJson.Serialize(
                _resolver.Resolve(campaign.CampaignSeed, request))));
            var unscaled = Unscaled138(roster);
            var frozen = EnemyForceProfile094.ApplyProgression135(unscaled, request.RouteModifiers);
            Assert.That(CanonicalJson.Serialize(EnemyForceProfile094.ApplyProgression138(unscaled, request.RouteModifiers)),
                Is.EqualTo(CanonicalJson.Serialize(frozen)), "The V4 dispatcher must delegate old versions unchanged.");
            var actual = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                loaded, _commands, _content, _resolver));
            AssertStats138(actual.Battle.EnemyUnions, frozen);
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(before));
        }

        [TestCase(1)] [TestCase(10)] [TestCase(11)] [TestCase(15)] [TestCase(20)] [TestCase(25)]
        public void V4PreservesCurrentOpeningCompositionAndCoefficients138(int chapter)
        {
            var old = EnemyForceProfile094.ForNewChapter135(chapter, 1);
            var next = EnemyForceProfile094.ForNewChapter138(chapter, 1);
            if (old == null) { Assert.That(next, Is.Null); return; }
            Assert.That(next.UnionCount094, Is.EqualTo(old.UnionCount094));
            Assert.That(next.MemberCount094, Is.EqualTo(old.MemberCount094));
            Assert.That(next.HpPercent138, Is.EqualTo(old.HpPercent135));
            Assert.That(next.OffensePercent138, Is.EqualTo(old.OffensePercent135));
        }

        [TestCase(26,330,300)] [TestCase(44,690,660)]
        [TestCase(65,1110,1080)] [TestCase(82,1450,1420)]
        public void V4LateCurveChangesOnlyVitalityAndOffense138(int chapter, int hp, int offense)
        {
            var source = StartArmy094().Battle.EnemyUnions;
            var before = CanonicalJson.Sha256Hex(source);
            var profile = EnemyForceProfile094.ForNewChapter138(chapter, 1);
            Assert.That(profile.HpPercent138, Is.EqualTo(hp));
            Assert.That(profile.OffensePercent138, Is.EqualTo(offense));
            var parsed = EnemyForceProfile094.ReadCommitted094(profile.Apply094(Request094(1)));
            Assert.That(parsed.IsPressure138, Is.True);
            Assert.That(parsed.Tag094, Is.EqualTo(profile.Tag094));
            var scaled = EnemyForceProfile094.ApplyProgression138(source, new[] { profile.Tag094 });
            var priorMembers = source.SelectMany(u => u.Members).ToArray();
            var members = scaled.SelectMany(u => u.Members).ToArray();
            for (var i = 0; i < members.Length; i++)
            {
                Assert.That(members[i].MaximumHp, Is.EqualTo((priorMembers[i].MaximumHp * hp + 99) / 100));
                Assert.That(members[i].Attack, Is.EqualTo((priorMembers[i].Attack * offense + 99) / 100));
                Assert.That(members[i].MagicAttack, Is.EqualTo((priorMembers[i].MagicAttack * offense + 99) / 100));
            }
            for (var i = 0; i < source.Count; i++)
            {
                var prior = JObject.FromObject(source[i]); var after = JObject.FromObject(scaled[i]);
                foreach (var token in new[] { prior, after }) foreach (JObject member in token["Members"])
                    foreach (var field in new[] { "CurrentHp", "MaximumHp", "Attack", "MagicAttack" }) member.Remove(field);
                Assert.That(JToken.DeepEquals(prior, after), Is.True, "No AP/MP/Arts/identity/formation change.");
            }
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(before));
            var roster = _resolver.Resolve(91294, profile.Apply094(Request094(1)));
            Assert.That(roster.Unions.All(u => u.Members.Count == 6), Is.True);
            foreach (var member in roster.Unions.SelectMany(u => u.Members))
                Assert.That(EnemyForceProfile094.TryArtTierFromSpawn094(member.MemberId, out var tier) && tier == profile.ArtTier094, Is.True);
        }

        [Test]
        public void V4CurveIncreasesEveryChapterAfterTwentyFive138()
        {
            var prior = EnemyForceProfile094.ForNewChapter138(25, 1);
            for (var chapter = 26; chapter <= 82; chapter++)
            {
                var next = EnemyForceProfile094.ForNewChapter138(chapter, 1);
                Assert.That(next.HpPercent138, Is.GreaterThan(prior.HpPercent138));
                Assert.That(next.OffensePercent138, Is.GreaterThan(prior.OffensePercent138));
                Assert.That(next.UnionCount094 * next.MemberCount094, Is.EqualTo(60)); prior = next;
            }
        }

        [Test]
        public void V4RejectsMalformedConflictingAndTowerProfiles138()
        {
            var profile = EnemyForceProfile094.ForNewChapter138(40, 10);
            foreach (var invalid in new[] { profile.Tag094.Replace("_H6_O8", "_H6_O9"),
                profile.Tag094.Replace("_L25_", "_L24_"), profile.Tag094.Replace("_M06_", "_M05_"),
                profile.Tag094.Replace("_S040_", "_S000_"), profile.Tag094 + "_EXTRA" })
                Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(91294, Request094(10, route: new[] { invalid })));
            var source = StartArmy094().Battle.EnemyUnions;
            foreach (var tower in new[] { "TOWER_ACTUAL_FLOOR098_1", "TOWER_THREAT_TIER_1", "TOWER_PARTY137_V1_M06" })
            {
                Assert.Throws<InvalidOperationException>(() => profile.Apply094(Request094(10, route: new[] { tower })));
                Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ApplyProgression138(source, new[] { profile.Tag094, tower }));
                Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ReadCommitted094(Request094(10, route: new[] { profile.Tag094, tower })));
            }
            var v3 = EnemyForceProfile094.ForNewChapter135(40, 10).Tag094;
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ApplyProgression138(source, new[] { profile.Tag094, v3 }));
            var raw = Request094(1);
            var ambiguous = RecordRequest094(RecordRequest094(CreateArmyFixture094(),
                EnemyForceProfile094.ForNewChapter135(35, 1).Apply094(raw)), EnemyForceProfile094.ForNewChapter138(35, 1).Apply094(raw));
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ForRequest094(ambiguous, raw, 35, false));
        }

        [Test]
        public void ActualNewV4BattleAndCommitmentSurviveDurableReload138()
        {
            var rules = new ChapterFixture094(82); var service = new CampaignCommandService019();
            var campaign = CreateArmyFixture094();
            campaign = Require094(service.StartChapter(campaign, rules, rules.Chapter.ChapterId,
                campaign.Guild.Unions.Select(u => u.UnionId).ToArray(), true));
            campaign = Require094(service.CommitCertifiedBattle(campaign, rules));
            var request = campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(EnemyForceProfile094.ReadCommitted094(request).IsPressure138, Is.True);
            campaign = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(campaign, _commands, _content, _resolver));
            Assert.That(campaign.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SecondDimension_Campaign138"));
            var directory = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "battle.json"); var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));
                var loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(loaded.Value.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
                var restored = EnemyForceProfile094.ForRequest094(loaded.Value.CampaignState, RequestFromTagged135(request), 82, false);
                Assert.That(CanonicalJson.Serialize(restored), Is.EqualTo(CanonicalJson.Serialize(request)));
                var resumed = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                    loaded.Value.CampaignState, _commands, _content, _resolver));
                Assert.That(CanonicalJson.Sha256Hex(resumed), Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
            }
            finally
            {
                var resolved = Path.GetFullPath(directory);
                Assert.That(Path.GetDirectoryName(resolved), Is.EqualTo(root).IgnoreCase);
                Assert.That(Guid.TryParseExact(Path.GetFileName(resolved), "N", out _), Is.True);
                Assert.That(File.GetAttributes(root) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                Assert.That(File.GetAttributes(resolved) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                Directory.Delete(resolved, true);
            }
        }

        [Test]
        public void ActualV4FinaleStatFloorIsAppliedAfterResetChapterOneCurve138()
        {
            var fixture = CreateArmyFixture094(); var raw = Request094(1);
            var finaleRequest = EnemyForceProfile094.ForNewChapter138(82, 1).Apply094(raw);
            var finale = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                RecordRequest094(fixture, finaleRequest), _commands, _content, _resolver)).Battle;
            var enemies = finale.EnemyUnions.SelectMany(u => u.Members).ToArray();
            var hp = enemies.Sum(m => (long)m.MaximumHp); var attack = enemies.Sum(m => (long)m.Attack);
            var magic = enemies.Sum(m => (long)m.MagicAttack);
            // Numerical boundary fixture using actual V4 constructor output;
            // this does not fabricate an earned campaign victory or save.
            var floor = new CampaignFinaleProof132(finale.BattleId, "MODEL_FLOOR_REWARD138", new string('a', 64),
                (hp * 125 + 99) / 100, (attack * 125 + 99) / 100, (magic * 125 + 99) / 100, 10);
            var resetRaw = new EncounterLaunchRequest017D(raw.RequestId, "CH018_001", raw.ExpeditionId,
                raw.BoardId, raw.NodeId, raw.EncounterId, raw.BattleId, raw.Objective, 1,
                raw.CanonicalSeedIdentity, raw.AlliedUnionIds, raw.ReserveUnionIds, raw.ObjectiveIds,
                new[] { CampaignReplayThreat130.FormatFinaleTag132(2, 25, floor) },
                raw.Supplies, raw.Fatigue, raw.Urgency, raw.ReturnCheckpointId, raw.PreBattleStateHash);
            var resetRequest = EnemyForceProfile094.ForRequest094(fixture, resetRaw, 82, true);
            var parsed = EnemyForceProfile094.ReadCommitted094(resetRequest);
            Assert.That(parsed.IsPressure138, Is.True); Assert.That(parsed.StatChapter134, Is.EqualTo(1));
            Assert.That(parsed.HpPercent138, Is.EqualTo(100)); Assert.That(parsed.OffensePercent138, Is.EqualTo(100));
            var reset = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                RecordRequest094(fixture, resetRequest), _commands, _content, _resolver)).Battle;
            var next = reset.EnemyUnions.SelectMany(u => u.Members).ToArray();
            Assert.That(next.Sum(m => (long)m.MaximumHp), Is.GreaterThanOrEqualTo(floor.MinimumTotalHp));
            Assert.That(next.Sum(m => (long)m.Attack), Is.GreaterThanOrEqualTo(floor.MinimumTotalAttack));
            Assert.That(next.Sum(m => (long)m.MagicAttack), Is.GreaterThanOrEqualTo(floor.MinimumTotalMagic));
            Assert.That(next.Sum(m => (long)m.MaximumHp), Is.GreaterThan(hp));
            Assert.That(next.Sum(m => (long)m.Attack), Is.GreaterThan(attack));
            Assert.That(next.Sum(m => (long)m.MagicAttack), Is.GreaterThan(magic));
        }

        IReadOnlyList<BattleUnionState> Unscaled138(EncounterRoster070 roster) =>
            (IReadOnlyList<BattleUnionState>)typeof(M2BattleCommandService).GetMethod("CreateEnemyUnions070",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { _content, roster });

        static void AssertStats138(IReadOnlyList<BattleUnionState> actual, IReadOnlyList<BattleUnionState> expected)
        {
            var a = actual.SelectMany(u => u.Members).ToArray(); var e = expected.SelectMany(u => u.Members).ToArray();
            Assert.That(a.Select(m => m.MaximumHp), Is.EqualTo(e.Select(m => m.MaximumHp)));
            Assert.That(a.Select(m => m.Attack), Is.EqualTo(e.Select(m => m.Attack)));
            Assert.That(a.Select(m => m.MagicAttack), Is.EqualTo(e.Select(m => m.MagicAttack)));
            Assert.That(actual.Select(u => u.CurrentAp), Is.EqualTo(expected.Select(u => u.CurrentAp)));
        }
    }
}
