using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class EnemyForceProfile094Tests
    {
        EncounterRosterResolver070 _resolver;
        M2CombatContent _content;
        M2BattleCommandService _commands;

        [SetUp] public void SetUp094()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _resolver = EncounterRosterResolver070.LoadFromContentRoot(root);
            _content = M2CombatContent.LoadFromDirectory(root);
            _commands = new M2BattleCommandService();
        }

        [Test]
        public void CandidateV3KeepsEveryFrozenRequestAndRosterByExactAuthority135()
        {
            var raw = Request094(1);
            var v1 = EnemyForceProfile094.ForChapter094(35, 1).Apply094(raw);
            var v2 = EnemyForceProfile094.ForNewChapter134(35, 1).Apply094(raw);
            var v3 = EnemyForceProfile094.ForNewChapter135(35, 1).Apply094(raw);
            Assert.That(v3.RouteModifiers.Last(), Is.EqualTo("ENEMY_FORCE094_V3_CH035_U10_M06_S035_H14_O12"));
            foreach (var request in new[] { raw, v1, v2, v3 })
            {
                var campaign = RecordRequest094(CreateArmyFixture094(), request);
                var hash = CanonicalJson.Sha256Hex(campaign);
                var loaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
                var restored = EnemyForceProfile094.ForRequest094(loaded, raw, 35, false);
                Assert.That(CanonicalJson.Serialize(restored), Is.EqualTo(CanonicalJson.Serialize(request)));
                Assert.That(CanonicalJson.Serialize(_resolver.Resolve(91294, restored)),
                    Is.EqualTo(CanonicalJson.Serialize(_resolver.Resolve(91294, request))));
                Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(hash));
            }
            var ambiguous = RecordRequest094(RecordRequest094(CreateArmyFixture094(), v2), v3);
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ForRequest094(ambiguous, raw, 35, false));
        }

        [TestCase(25,310,280)] [TestCase(40,520,460)] [TestCase(81,1094,952)]
        public void CandidateV3FrozenCurveKeepsSixtyAndOldStatResults135(int chapter, int hpPercent, int offensePercent)
        {
            var source = StartArmy094().Battle.EnemyUnions;
            var before = CanonicalJson.Sha256Hex(source);
            var profile = EnemyForceProfile094.ForNewChapter135(chapter, 1);
            Assert.That(profile.UnionCount094 * profile.MemberCount094, Is.EqualTo(60));
            var request = profile.Apply094(Request094(1));
            var roster = _resolver.Resolve(91294, request);
            Assert.That(roster.Unions.SelectMany(u => u.Members).Count(), Is.EqualTo(60));
            foreach (var member in roster.Unions.SelectMany(u => u.Members))
                Assert.That(EnemyForceProfile094.TryArtTierFromSpawn094(member.MemberId, out var tier) && tier == profile.ArtTier094, Is.True);
            var parsed = EnemyForceProfile094.ReadCommitted094(request);
            Assert.That(parsed.HpPercent135, Is.EqualTo(hpPercent));
            Assert.That(parsed.OffensePercent135, Is.EqualTo(offensePercent));
            var result = EnemyForceProfile094.ApplyProgression135(source, new[] { profile.Tag094 });
            var prior = source.SelectMany(u => u.Members).ToArray();
            var after = result.SelectMany(u => u.Members).ToArray();
            for (var i = 0; i < prior.Length; i++)
            {
                Assert.That(after[i].MaximumHp, Is.EqualTo((prior[i].MaximumHp * hpPercent + 99) / 100));
                Assert.That(after[i].Attack, Is.EqualTo((prior[i].Attack * offensePercent + 99) / 100));
                Assert.That(after[i].MagicAttack, Is.EqualTo((prior[i].MagicAttack * offensePercent + 99) / 100));
                Assert.That(after[i].LearnedArtIds, Is.EqualTo(prior[i].LearnedArtIds));
                Assert.That(after[i].CurrentMp, Is.EqualTo(prior[i].CurrentMp));
            }
            Assert.That(result.Select(u => u.CurrentAp), Is.EqualTo(source.Select(u => u.CurrentAp)));
            foreach (var tag in new[] { EnemyForceProfile094.ForChapter094(chapter,10).Tag094,
                EnemyForceProfile094.ForNewChapter134(chapter,10).Tag094 })
                Assert.That(CanonicalJson.Serialize(EnemyForceProfile094.ApplyProgression135(source,new[] { tag })),
                    Is.EqualTo(CanonicalJson.Serialize(EnemyForceProfile094.ApplyProgression134(source,new[] { tag }))));
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(before));
        }

        [Test]
        public void ShippingV4StartUsesFrozenStatsAndReloadKeepsBattle138()
        {
            var rules = new ChapterFixture094(40);
            var service = new CampaignCommandService019();
            var campaign = CreateArmyFixture094();
            campaign = Require094(service.StartChapter(campaign,rules,rules.Chapter.ChapterId,
                campaign.Guild.Unions.Select(u => u.UnionId).ToArray(),true));
            campaign = Require094(service.CommitCertifiedBattle(campaign,rules));
            var request = campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(EnemyForceProfile094.ReadCommitted094(request).IsPressure138, Is.True);
            var roster = _resolver.Resolve(campaign.CampaignSeed, request);
            var constructor = typeof(M2BattleCommandService).GetMethod("CreateEnemyUnions070",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var unscaled = (IReadOnlyList<BattleUnionState>)constructor.Invoke(null,new object[] { _content, roster });
            var expected = EnemyForceProfile094.ApplyProgression138(unscaled,request.RouteModifiers).SelectMany(u => u.Members).ToArray();
            campaign = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(campaign,_commands,_content,_resolver));
            var actual = campaign.Battle.EnemyUnions.SelectMany(u => u.Members).ToArray();
            Assert.That(actual.Select(m => m.MaximumHp), Is.EqualTo(expected.Select(m => m.MaximumHp)));
            Assert.That(actual.Select(m => m.Attack), Is.EqualTo(expected.Select(m => m.Attack)));
            Assert.That(actual.Select(m => m.MagicAttack), Is.EqualTo(expected.Select(m => m.MagicAttack)));
            var loaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            Assert.That(CanonicalJson.Sha256Hex(loaded), Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
            Assert.That(EnemyForceProfile094.ForRequest094(loaded, RequestFromTagged135(request),40,false).RouteModifiers,
                Is.EqualTo(request.RouteModifiers));
        }

        [Test]
        public void CandidateV3RejectsTowerAndRetainsActualReplayChapterAndFinaleFloor135()
        {
            var profile = EnemyForceProfile094.ForNewChapter135(40, 10);
            Assert.Throws<InvalidOperationException>(() => profile.Apply094(Request094(10,route:new[] { "TOWER_ACTUAL_FLOOR098_1" })));
            var source = StartArmy094().Battle.EnemyUnions;
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ApplyProgression135(source,
                new[] { profile.Tag094, "TOWER_THREAT_TIER_1" }));
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ApplyProgression135(source,
                new[] { profile.Tag094, EnemyForceProfile094.ForNewChapter134(40,10).Tag094 }));
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ReadCommitted094(Request094(10,
                route:new[] { profile.Tag094.Replace("H14_O12","H14_O13") })));
            var raw = Request094(10);
            var replay = new EncounterLaunchRequest017D(raw.RequestId,"CH018_001",raw.ExpeditionId,
                raw.BoardId,raw.NodeId,raw.EncounterId,raw.BattleId,raw.Objective,raw.EnemyUnionCount,
                raw.CanonicalSeedIdentity,raw.AlliedUnionIds,raw.ReserveUnionIds,raw.ObjectiveIds,raw.RouteModifiers,
                raw.Supplies,raw.Fatigue,raw.Urgency,raw.ReturnCheckpointId,raw.PreBattleStateHash);
            var committed = EnemyForceProfile094.ForNewChapter135(82,10,1).Apply094(replay);
            var parsed = EnemyForceProfile094.ReadCommitted094(committed);
            Assert.That(parsed.IsPressure135, Is.True);
            Assert.That(parsed.StatChapter134, Is.EqualTo(1));
            Assert.That(parsed.HpPercent135, Is.EqualTo(100));
            var finale = new CampaignFinaleProof132("BOSS","REWARD",new string('A',64),50000,5000,3000,10);
            var routes = new[] { parsed.Tag094, CampaignReplayThreat130.FormatFinaleTag132(2,25,finale) };
            var method = typeof(M2BattleCommandService).GetMethod("ApplyRouteModifiersToEnemies",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var result = (IReadOnlyList<BattleUnionState>)method.Invoke(null,new object[] { source,routes });
            Assert.That(result.Sum(u => u.Members.Sum(m => (long)m.MaximumHp)), Is.GreaterThanOrEqualTo(50000));
            Assert.That(result.Sum(u => u.Members.Sum(m => (long)m.Attack)), Is.GreaterThanOrEqualTo(5000));
        }

        static EncounterLaunchRequest017D RequestFromTagged135(EncounterLaunchRequest017D request) =>
            new EncounterLaunchRequest017D(request.RequestId,request.ContractId,request.ExpeditionId,
                request.BoardId,request.NodeId,request.EncounterId,request.BattleId,request.Objective,1,
                request.CanonicalSeedIdentity,request.AlliedUnionIds,request.ReserveUnionIds,request.ObjectiveIds,
                request.RouteModifiers.Where(t => !t.StartsWith("ENEMY_FORCE094_",StringComparison.Ordinal)).ToArray(),
                request.Supplies,request.Fatigue,request.Urgency,request.ReturnCheckpointId,request.PreBattleStateHash);

        [TestCase(11,3,3)] [TestCase(15,5,3)] [TestCase(20,7,4)]
        [TestCase(24,9,5)] [TestCase(25,10,6)] [TestCase(60,10,6)]
        public void CandidateV2ReachesSixtyAndKeepsExactRosterAfterJson134(int chapter, int unions, int members)
        {
            var profile = EnemyForceProfile094.ForNewChapter134(chapter, 1);
            var request = profile.Apply094(Request094(1));
            Assert.That(request.EnemyUnionCount, Is.EqualTo(unions));
            Assert.That(EnemyForceProfile094.ReadCommitted094(request).MemberCount094, Is.EqualTo(members));
            var first = _resolver.Resolve(91294, request);
            var reloaded = JsonConvert.DeserializeObject<EncounterLaunchRequest017D>(JsonConvert.SerializeObject(request));
            Assert.That(CanonicalJson.Serialize(_resolver.Resolve(91294, reloaded)), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(first.Unions.All(union => union.Members.Count >= members && union.Members.Count <= 6), Is.True);
            Assert.That(first.Unions.Count, Is.EqualTo(unions));
            foreach (var member in first.Unions.SelectMany(union => union.Members))
                Assert.That(EnemyForceProfile094.TryArtTierFromSpawn094(member.MemberId, out _), Is.True);
        }

        [Test]
        public void CandidateV2ReconstructsV1V2AndUntaggedOnlyByExactCommitment134()
        {
            var raw = Request094(1);
            var v1 = EnemyForceProfile094.ForChapter094(35, 1).Apply094(raw);
            var v2 = EnemyForceProfile094.ForNewChapter134(35, 1).Apply094(raw);
            Assert.That(v2.RouteModifiers.Last(), Does.StartWith(EnemyForceProfile094.Prefix134));
            foreach (var request in new[] { raw, v1, v2 })
            {
                var campaign = RecordRequest094(CreateArmyFixture094(), request);
                campaign = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
                var restored = EnemyForceProfile094.ForRequest094(campaign, raw, 35, false);
                Assert.That(CanonicalJson.Serialize(restored), Is.EqualTo(CanonicalJson.Serialize(request)));
                Assert.That(CanonicalJson.Serialize(_resolver.Resolve(91294, restored)),
                    Is.EqualTo(CanonicalJson.Serialize(_resolver.Resolve(91294, request))));
            }
            var ambiguous = RecordRequest094(RecordRequest094(CreateArmyFixture094(), v1), v2);
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ForRequest094(ambiguous, raw, 35, false));
        }

        [Test]
        public void CandidateV2ContinuesStatGrowthAfterSixtyWithoutChangingLegacyOrSource134()
        {
            var old = StartArmy094();
            var source = old.Battle.EnemyUnions;
            var hash = CanonicalJson.Sha256Hex(old);
            var v1 = EnemyForceProfile094.ForChapter094(60, 10).Tag094;
            Assert.That(EnemyForceProfile094.ApplyProgression134(source, new[] { v1 }), Is.SameAs(source));
            var previousHp = 0L; var previousAttack = 0L; var previousMagic = 0L;
            foreach (var chapter in new[] { 25, 40, 60, 81 })
            {
                var profile = EnemyForceProfile094.ForNewChapter134(chapter, 10);
                var result = EnemyForceProfile094.ApplyProgression134(source, new[] { profile.Tag094 });
                var hp = result.Sum(u => u.Members.Sum(m => (long)m.MaximumHp));
                var attack = result.Sum(u => u.Members.Sum(m => (long)m.Attack));
                var magic = result.Sum(u => u.Members.Sum(m => (long)m.MagicAttack));
                Assert.That(hp, Is.GreaterThan(previousHp)); Assert.That(attack, Is.GreaterThan(previousAttack));
                Assert.That(magic, Is.GreaterThan(previousMagic));
                previousHp = hp; previousAttack = attack; previousMagic = magic;
                Assert.That(result.Select(u => u.CurrentAp), Is.EqualTo(source.Select(u => u.CurrentAp)));
                Assert.That(result.SelectMany(u => u.Members).Select(m => m.LearnedArtIds),
                    Is.EqualTo(source.SelectMany(u => u.Members).Select(m => m.LearnedArtIds)));
                if (chapter == 60)
                {
                    Assert.That(hp, Is.EqualTo(4L * source.Sum(u => u.Members.Sum(m => (long)m.MaximumHp))));
                    Assert.That(attack, Is.EqualTo(3L * source.Sum(u => u.Members.Sum(m => (long)m.Attack))));
                }
            }
            Assert.That(CanonicalJson.Sha256Hex(old), Is.EqualTo(hash));
        }

        [Test]
        public void CandidateV2RejectsTowerAndKeepsActualReplayChapterSeparateFromForceFloor134()
        {
            var profile = EnemyForceProfile094.ForNewChapter134(60, 10);
            Assert.Throws<InvalidOperationException>(() => profile.Apply094(Request094(10,
                route: new[] { "TOWER_ACTUAL_FLOOR098_1" })));
            var historicalRaw = Request094(10, route: new[] { "TOWER_THREAT_TIER_1" });
            var historicalV1 = EnemyForceProfile094.ForChapter094(35,10).Apply094(historicalRaw);
            var historical = RecordRequest094(CreateArmyFixture094(),historicalV1);
            Assert.That(CanonicalJson.Serialize(EnemyForceProfile094.ForRequest094(historical,historicalRaw,35,false)),
                Is.EqualTo(CanonicalJson.Serialize(historicalV1)), "Existing V1 construction remains unchanged.");
            var source = StartArmy094().Battle.EnemyUnions;
            Assert.Throws<InvalidOperationException>(() => EnemyForceProfile094.ApplyProgression134(source,
                new[] { profile.Tag094, "TOWER_THREAT_TIER_1" }));
            var old = Request094(10);
            var replay = new EncounterLaunchRequest017D(old.RequestId,"CH018_001",old.ExpeditionId,
                old.BoardId,old.NodeId,old.EncounterId,old.BattleId,old.Objective,old.EnemyUnionCount,
                old.CanonicalSeedIdentity,old.AlliedUnionIds,old.ReserveUnionIds,old.ObjectiveIds,old.RouteModifiers,
                old.Supplies,old.Fatigue,old.Urgency,old.ReturnCheckpointId,old.PreBattleStateHash);
            var committed = EnemyForceProfile094.ForNewChapter134(82,10,1).Apply094(replay);
            var parsed = EnemyForceProfile094.ReadCommitted094(committed);
            Assert.That(parsed.Chapter094, Is.EqualTo(82));
            Assert.That(parsed.StatChapter134, Is.EqualTo(1));
            Assert.That(parsed.HpPercent134, Is.EqualTo(100));
            var finale = new CampaignFinaleProof132("BOSS", "REWARD", new string('A',64),50000,5000,3000,10);
            var routes = new[] { committed.RouteModifiers.Last(), CampaignReplayThreat130.FormatFinaleTag132(2,25,finale) };
            var method = typeof(M2BattleCommandService).GetMethod("ApplyRouteModifiersToEnemies",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var scaled = (IReadOnlyList<BattleUnionState>)method.Invoke(null, new object[] { source,routes });
            Assert.That(scaled.Sum(u => u.Members.Sum(m => (long)m.MaximumHp)), Is.GreaterThanOrEqualTo(50000));
            Assert.That(scaled.Sum(u => u.Members.Sum(m => (long)m.Attack)), Is.GreaterThanOrEqualTo(5000));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(5)] [TestCase(10)]
        public void EarlyRequestsAndRosterAreUnchanged094(int chapter)
        {
            var old = Request094(1);
            Assert.That(EnemyForceProfile094.ForChapter094(chapter, 1), Is.Null);
            var same = EnemyForceProfile094.ForRequest094(CreateArmyFixture094(), old, chapter, true);
            Assert.That(same, Is.SameAs(old));
            var direct = _resolver.Resolve(91294, old.ContractId, old.BoardId,
                old.EncounterId, old.EnemyUnionCount, old.CanonicalSeedIdentity);
            Assert.That(CanonicalJson.Serialize(_resolver.Resolve(91294, same)),
                Is.EqualTo(CanonicalJson.Serialize(direct)));
        }

        [TestCase(11,3,3)] [TestCase(15,3,3)]
        [TestCase(16,4,4)] [TestCase(20,4,4)]
        [TestCase(21,6,4)] [TestCase(25,6,4)]
        [TestCase(26,8,5)] [TestCase(30,8,5)]
        [TestCase(31,9,6)] [TestCase(34,9,6)]
        [TestCase(35,10,6)] [TestCase(40,10,6)] [TestCase(50,10,6)]
        public void RampBoundariesResolveLegalDeterministicForces094(int chapter, int unions, int members)
        {
            var profile = EnemyForceProfile094.ForChapter094(chapter, 1);
            var request = profile.Apply094(Request094(1));
            Assert.That(request.EnemyUnionCount, Is.EqualTo(unions));
            Assert.That(EnemyForceProfile094.ReadCommitted094(request).MemberCount094, Is.EqualTo(members));
            var first = _resolver.Resolve(91294, request);
            var restoredRequest = JsonConvert.DeserializeObject<EncounterLaunchRequest017D>(
                JsonConvert.SerializeObject(request));
            var restored = _resolver.Resolve(91294, restoredRequest);
            Assert.That(CanonicalJson.Serialize(restored), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(first.Unions, Has.Count.EqualTo(unions));
            Assert.That(first.Unions.All(value => value.Members.Count >= members &&
                value.Members.Count <= 6), Is.True);
            var all = first.Unions.SelectMany(value => value.Members).ToArray();
            Assert.That(all.Select(value => value.MemberId).Distinct().Count(), Is.EqualTo(all.Length));
            foreach (var member in all)
            {
                Assert.That(member.Definition.Id, Is.EqualTo(member.SourceEnemyId));
                Assert.That(_content.Enemy(member.SourceEnemyId).ArtIds, Is.EquivalentTo(member.Definition.ArtIds));
                Assert.That(member.Definition.ArtIds.All(_content.Arts.ContainsKey), Is.True,
                    member.SourceEnemyId + " must use real learned Arts, not art-pack metadata.");
                Assert.That(EnemyArtIdentity090.TryCampaignBaseIndex090(member.FamilyId, out _), Is.True);
                Assert.That(EnemyForceProfile094.TryArtTierFromSpawn094(member.MemberId, out var tier), Is.True);
                Assert.That(tier, Is.EqualTo(profile.ArtTier094));
            }
            foreach (var union in first.Unions)
                Assert.That(_content.Formation(union.Definition.FormationId).Supports(union.Members.Count),
                    Is.True, union.SourceUnionId);
        }

        [TestCase(11)] [TestCase(16)] [TestCase(26)]
        public void StrongerAuthoredUnionCountRemainsTen094(int chapter)
        {
            var request = EnemyForceProfile094.ForChapter094(chapter, 10).Apply094(Request094(10));
            Assert.That(request.EnemyUnionCount, Is.EqualTo(10));
            Assert.That(_resolver.Resolve(91294, request).Unions, Has.Count.EqualTo(10));
        }

        [Test]
        public void NamedStoryBossOccursOnceAndExtraSlotsAreItsExistingEscorts094()
        {
            var old = Request094(1, "ENCOUNTER_HINGE_EATER_COLOSSUS_BOSS");
            var original = _resolver.Resolve(91294, old).Unions[0];
            Assert.That(original.SourceUnionId, Is.EqualTo("EU_HINGE_EATER"));
            var expanded = _resolver.Resolve(91294,
                EnemyForceProfile094.ForChapter094(35, 1).Apply094(old));
            var bossUnion = expanded.Unions.Single(value => value.SourceUnionId == original.SourceUnionId);
            Assert.That(bossUnion.Members.Count, Is.EqualTo(6));
            var bossMembers = expanded.Unions.SelectMany(value => value.Members)
                .Where(value => value.FamilyId == "ENEMY_FAMILY_HINGE_EATER_COLOSSUS").ToArray();
            Assert.That(bossMembers, Has.Length.EqualTo(1));
            Assert.That(bossMembers[0].SourceEnemyId, Is.EqualTo("ENEMY_HINGE_EATER_COLOSSUS_01"));
            Assert.That(bossMembers[0].Definition, Is.SameAs(original.Members[0].Definition));
            Assert.That(bossUnion.Members.All(value =>
                original.Members.Any(before => before.SourceEnemyId == value.SourceEnemyId)), Is.True);
            Assert.That(expanded.Unions.SelectMany(value => value.Members).Any(value =>
                value.FamilyId == "ENEMY_FAMILY_CAPTAIN_RAVEL" ||
                value.FamilyId == "ENEMY_FAMILY_GATEHEART_WARDEN"), Is.False,
                "A larger army must not introduce unrelated named story bosses.");
        }

        [TestCase("ENEMY_FORCE094_V1_CH035_U09_M06")]
        [TestCase("ENEMY_FORCE094_V1_CH035_U10_M05")]
        [TestCase("ENEMY_FORCE094_V2_CH035_U10_M06")]
        [TestCase("ENEMY_FORCE094_V1_CH010_U10_M06")]
        public void MalformedOrContradictoryProfilesFailClosed094(string tag)
        {
            var request = Request094(10, route: new[] { tag });
            Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(91294, request));
        }

        [Test]
        public void UntaggedSavedRequestCannotBeUpgradedMerelyByCurrentChapter094()
        {
            var old = Request094(1);
            var campaign = RecordRequest094(CreateArmyFixture094(), old);
            var hash = CanonicalJson.Sha256Hex(campaign);
            var verified = EnemyForceProfile094.ForRequest094(campaign, old, 35, false);
            Assert.That(CanonicalJson.Serialize(verified), Is.EqualTo(CanonicalJson.Serialize(old)));
            Assert.That(EnemyForceProfile094.ReadCommitted094(verified), Is.Null);
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(hash));
            var next = EnemyForceProfile094.ForRequest094(campaign, old, 35, true);
            Assert.That(next.EnemyUnionCount, Is.EqualTo(10));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(next)), Is.False,
                "Constructing a candidate cannot itself authorize an encounter.");
        }

        [Test]
        public void NewCommitmentIsRecoveredByItsExactRecordedAuthorityAfterJson094()
        {
            var old = Request094(1);
            var expanded = EnemyForceProfile094.ForChapter094(35,1).Apply094(old);
            var campaign = RecordRequest094(CreateArmyFixture094(), expanded);
            campaign = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            Assert.That(CanonicalJson.Serialize(EnemyForceProfile094.ForRequest094(campaign, old,35,false)),
                Is.EqualTo(CanonicalJson.Serialize(expanded)));
            var forged = EnemyForceProfile094.ForChapter094(40,1).Apply094(old);
            var city = campaign.Guild.GuildCity.With(pendingEncounter:forged,replacePendingEncounter:true);
            var altered = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
            Assert.That(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                altered,_commands,_content,_resolver).IsSuccess, Is.False,
                "A well-formed force tag still needs the normal exact request authority.");
        }

        [Test]
        public void ShippingChapterCommitBindsTheRampAndReopensWithoutReroll094()
        {
            var rules = new ChapterFixture094(35);
            var service = new CampaignCommandService019();
            var campaign = CreateArmyFixture094();
            campaign = Require094(service.StartChapter(campaign,rules,rules.Chapter.ChapterId,
                campaign.Guild.Unions.Select(value=>value.UnionId).ToArray(),true));
            campaign = Require094(service.CommitCertifiedBattle(campaign,rules));
            var request = campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(request.EnemyUnionCount, Is.EqualTo(10));
            Assert.That(EnemyForceProfile094.ReadCommitted094(request).MemberCount094, Is.EqualTo(6));
            var hash = CanonicalJson.Sha256Hex(campaign);
            campaign = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            campaign = Require094(service.CommitCertifiedBattle(campaign,rules));
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(hash));
            campaign = Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                campaign,_commands,_content,_resolver));
            AssertArmy094(campaign);
        }

        [Test]
        public void CopiedR65Chapter35RequestRetainsActualPersistedTwelveMemberIdentities094()
        {
            var document = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
                "Tests","Fixtures","EnemyForce094","LegacyRequestFromR65_CH035_094.json")));
            var old = document["Request"].ToObject<EncounterLaunchRequest017D>();
            Assert.That(GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(old),
                Is.EqualTo(document.Value<string>("RecordedRequestAuthority")));
            var campaign = RecordRequest094(CreateArmyFixture094(), old);
            var verify = EnemyForceProfile094.ForRequest094(campaign,old,35,false);
            var roster = _resolver.Resolve(document.Value<long>("CampaignSeed"),verify);
            var persisted = document["PersistedFinalEnemyUnions"].ToArray();
            Assert.That(roster.Unions.Select(value=>value.UnionId),
                Is.EquivalentTo(persisted.Select(value=>value.Value<string>("UnionId"))));
            Assert.That(roster.Unions.SelectMany(value=>value.Members).Select(value=>value.MemberId),
                Is.EquivalentTo(persisted.SelectMany(value=>value["Members"])
                    .Select(value=>value.Value<string>("MemberId"))));
            Assert.That(roster.Unions.SelectMany(value=>value.Members).Count(),Is.EqualTo(12));
            Assert.That(verify.RouteModifiers.Any(value=>value.StartsWith("ENEMY_FORCE094_")),Is.False);
        }

        [Test]
        public void SixtyVersusSixtyUsesRealForecastRoundReplayAndTerminates094()
        {
            var campaign = StartArmy094();
            AssertArmy094(campaign);
            var before = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            var first = ConfirmEveryUnion094(campaign,"CMD_GUARD");
            var replay = ConfirmEveryUnion094(before,"CMD_GUARD");
            Assert.That(CanonicalJson.Sha256Hex(first),Is.EqualTo(CanonicalJson.Sha256Hex(replay)));
            Assert.That(first.Battle.RoundRecords.Last().Selections,Has.Count.EqualTo(10));
            Assert.That(first.Battle.RoundRecords.Last().Events.Any(value=>
                value.EventType=="ENEMY_HIT" || value.EventType=="INTERCEPTION"),Is.True);
            for(var round=0;round<30 && first.Battle.Outcome==BattleOutcome.InProgress;round++)
                first=ConfirmEveryUnion094(first,"CMD_BALANCED");
            Assert.That(first.Battle.Outcome,Is.Not.EqualTo(BattleOutcome.InProgress),
                "The actual engine must end this fixture; a sixty-member count alone is not proof.");
            Assert.That(first.Battle.Phase,Is.EqualTo(BattlePhase.Resolved));
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(first.Battle),Is.True);
            var endedHash=CanonicalJson.Sha256Hex(first);
            var forbidden=_commands.ConfirmRound(first,_content);
            Assert.That(forbidden.IsSuccess,Is.False);
            Assert.That(CanonicalJson.Sha256Hex(first),Is.EqualTo(endedHash));
            Assert.That(first.Battle.PlayerUnions.Concat(first.Battle.EnemyUnions)
                .SelectMany(value=>value.Members).All(value=>value.CurrentHp>=0 &&
                    value.CurrentHp<=value.MaximumHp && value.CurrentMp>=0),Is.True);
        }

        [Test]
        public void NaturallyAuthoredEnemyMedicsCanRescueAcrossTheSixtyMemberArmy094()
        {
            var campaign=StartArmy094();
            var enemies=campaign.Battle.EnemyUnions.ToList();
            var medics=enemies.SelectMany(union=>union.Members.Select(member=>new {union,member}))
                .Where(value=>value.member.ClassId=="ENEMY_ASH_MEDIC_01").ToArray();
            Assert.That(medics.Length,Is.GreaterThan(0));
            Assert.That(medics.All(value=>value.member.LearnedArtIds.Contains("ART_FIELD_REMEDY")),Is.True);
            var target=enemies.First(union=>union.Members.All(member=>member.ClassId!="ENEMY_ASH_MEDIC_01"));
            var targetIndex=enemies.FindIndex(value=>value.UnionId==target.UnionId);
            enemies[targetIndex]=target.With(members:target.Members.Select(member=>
                member.With(currentHp:Math.Max(1,member.MaximumHp/20))).ToArray());
            campaign=campaign.WithBattle(campaign.Battle.With(enemyUnions:enemies.AsReadOnly()));
            var before=enemies.SelectMany(value=>value.Members).ToDictionary(value=>value.MemberId);
            campaign=ConfirmEveryUnion094(campaign,"CMD_GUARD");
            var events=campaign.Battle.RoundRecords.Last().Events;
            var heals=events.Where(value=>value.Side==BattleSide.Enemy &&
                value.EventType=="RESTORATION" && value.TargetUnionId==target.UnionId &&
                value.ActorUnionId!=target.UnionId).ToArray();
            Assert.That(heals.Length,Is.GreaterThan(0),
                "No injected learned Arts: existing Ash Medics must find the critical friendly Union.");
            foreach(var heal in heals)
            {
                Assert.That(heal.Amount,Is.GreaterThan(0));
                Assert.That(before[heal.ActorMemberId].LearnedArtIds,Does.Contain(heal.ArtId));
                Assert.That(events.Any(value=>value.EventType=="ENEMY_SUPPORT_FORECAST" &&
                    value.ActorMemberId==heal.ActorMemberId && value.TargetUnionId==target.UnionId),Is.True);
                var after=campaign.Battle.EnemyUnions.SelectMany(value=>value.Members)
                    .Single(value=>value.MemberId==heal.ActorMemberId);
                Assert.That(after.CurrentMp,Is.LessThan(before[heal.ActorMemberId].CurrentMp));
            }
        }

        [TestCase(5,4)] [TestCase(35,10)]
        public void RevealedUncommittedCardShowsTheNewForceWithoutMutatingIt094(int chapter,int expected)
        {
            var campaign=CreateArmyFixture094();
            var before=CanonicalJson.Sha256Hex(campaign);
            Assert.That(EnemyForceProfile094.PreviewUnionCount094(campaign,
                "CH018_"+chapter.ToString("000"),4,"CARD_NEXT",null,null),Is.EqualTo(expected));
            Assert.That(CanonicalJson.Sha256Hex(campaign),Is.EqualTo(before));
        }

        [Test]
        public void RevealedOldPendingCardKeepsFourEvenAtChapterThirtyFive094()
        {
            var request=Request094(4);
            var campaign=RecordRequest094(CreateArmyFixture094(),request);
            Assert.That(EnemyForceProfile094.PreviewUnionCount094(campaign,
                "CH018_035",4,"CARD_SAVED","CARD_SAVED",request.BattleId),Is.EqualTo(4));
            Assert.That(EnemyForceProfile094.PreviewUnionCount094(campaign,
                "CH018_035",4,"CARD_OTHER","CARD_SAVED",request.BattleId),Is.EqualTo(10));
        }

        [Test]
        public void ReturnedLegacyBattleCountOutranksTheNewRampInReceiptPresentation094()
        {
            var request=Request094(4);
            var roster=_resolver.Resolve(91294,request);
            var campaign=Require094(_commands.StartEncounterBattleWithRoster070(
                CreateArmyFixture094(),_content,request.BattleId,request.Objective,roster));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter,Is.Null);
            Assert.That(EnemyForceProfile094.PreviewUnionCount094(campaign,
                "CH018_035",4,"CARD_SAVED","CARD_SAVED",request.BattleId),Is.EqualTo(4));
            Assert.That(EnemyForceProfile094.PreviewUnionCount094(campaign,
                "CH018_035",4,"CARD_SAVED","CARD_SAVED","MISSING_BATTLE"),Is.EqualTo(4),
                "Missing receipt linkage must not pretend the old request was upgraded.");
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingResolverOnlyRejectsNewTaggedCommitments094(bool tagged)
        {
            var request = Request094(1);
            if (tagged) request = EnemyForceProfile094.ForChapter094(35, 1).Apply094(request);
            var campaign = RecordRequest094(CreateArmyFixture094(), request);
            var before = CanonicalJson.Sha256Hex(campaign);
            var result = new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                campaign, _commands, _content);
            Assert.That(result.IsSuccess, Is.EqualTo(!tagged), string.Join("\n", result.Errors));
            if (tagged)
                Assert.That(string.Join("\n", result.Errors),
                    Does.Contain("ENEMY_FORCE094_COMMITTED_ROSTER_REQUIRED"));
            else
                Assert.That(result.Value.Battle.EnemyUnions, Has.Count.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(before));
        }

        [TestCase(false)] [TestCase(true)]
        public void DirectCommittedEngineCannotBypassMissingOrUnderfilledRosterGuard094(bool underfilled)
        {
            var old = Request094(10);
            var request = EnemyForceProfile094.ForChapter094(35, 10).Apply094(old);
            var campaign = RecordRequest094(CreateArmyFixture094(), request);
            var roster = underfilled ? _resolver.Resolve(91294, old) : null;
            if (underfilled)
            {
                Assert.That(roster.Unions, Has.Count.EqualTo(10));
                Assert.That(roster.Unions.Any(value => value.Members.Count < 6), Is.True);
            }
            // The shipping constructor is internal by design. Exercise its real
            // body without changing production visibility just for this fixture.
            var method = typeof(M2BattleCommandService).GetMethod(
                "StartCommittedEncounterBattle017D",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var before = CanonicalJson.Sha256Hex(campaign);
            var result = (Result<CampaignState>)method.Invoke(_commands,
                new object[] { campaign, _content, request, roster });
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(string.Join("\n", result.Errors), Does.Contain(underfilled ?
                "ENEMY_FORCE094_ROSTER_MEMBER_COUNT_INVALID" :
                "ENEMY_FORCE094_COMMITTED_ROSTER_REQUIRED"));
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(before));
            Assert.That(campaign.Battle, Is.Null);
        }

        [TestCase("seed")] [TestCase("suffix")] [TestCase("duplicate")]
        public void TaggedRosterRejectsWrongSeedStaleMemberMarkerAndCrossUnionDuplicate094(string alteration)
        {
            var request = EnemyForceProfile094.ForChapter094(35, 1).Apply094(Request094(1));
            var roster = _resolver.Resolve(91294, request);
            Assert.DoesNotThrow(() => EnemyForceProfile094.ValidateRoster094(request, roster));
            var unions = roster.Unions.ToArray();
            if (alteration != "seed")
            {
                var selected = unions[1];
                var members = selected.Members.ToArray();
                var first = members[0];
                members[0] = new EncounterEnemyMember070(
                    alteration == "duplicate" ? unions[0].Members[0].MemberId :
                        first.MemberId.Replace("_FORCE094_V1_CH035", "_FORCE094_V1_CH040"),
                    first.SourceEnemyId, first.FamilyId, first.VisualVariantSeed, first.Definition);
                unions[1] = new EncounterEnemyUnion070(selected.UnionId, selected.SourceUnionId,
                    selected.SourceDefinition, members, selected.FamilyIds);
            }
            var bad = new EncounterRoster070(roster.RosterId,
                alteration == "seed" ? roster.CanonicalSeedIdentity + "_DIFFERENT" :
                    roster.CanonicalSeedIdentity, unions, roster.FamilyIds);
            var failure = Assert.Throws<InvalidOperationException>(() =>
                EnemyForceProfile094.ValidateRoster094(request, bad));
            Assert.That(failure.Message, Does.Contain(alteration == "seed" ?
                "ROSTER_IDENTITY_MISMATCH" : "SPAWN_IDENTITY_MISMATCH"));
        }

        CampaignState StartArmy094()
        {
            var campaign=CreateArmyFixture094();
            var request=EnemyForceProfile094.ForChapter094(35,1).Apply094(Request094(1,
                allied:campaign.Guild.Unions.Select(value=>value.UnionId).ToArray()));
            campaign=RecordRequest094(campaign,request);
            return Require094(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                campaign,_commands,_content,_resolver));
        }

        void AssertArmy094(CampaignState campaign)
        {
            Assert.That(campaign.Battle.PlayerUnions,Has.Count.EqualTo(10));
            Assert.That(campaign.Battle.EnemyUnions,Has.Count.EqualTo(10));
            Assert.That(campaign.Battle.PlayerUnions.All(value=>value.Members.Count==6),Is.True);
            Assert.That(campaign.Battle.EnemyUnions.All(value=>value.Members.Count==6 &&
                value.FormationMemberCountEligible),Is.True);
            var enemies=campaign.Battle.EnemyUnions.SelectMany(value=>value.Members).ToArray();
            Assert.That(enemies.Select(value=>value.MemberId).Distinct().Count(),Is.EqualTo(60));
            foreach(var member in enemies)
            {
                Assert.That(member.EnemyArtVariantId090,Does.EndWith("_VAR_07"));
                var family=member.EquipmentTags.First(value=>value.StartsWith("ENEMY_FAMILY_"));
                Assert.That(EnemyArtIdentity090.TryCampaignBaseIndex090(family,out var baseIndex),Is.True);
                Assert.That(member.EnemyArtBaseId090,Is.EqualTo(EnemyArtIdentity090.BaseId090(baseIndex)));
                Assert.That(EnemyArt700Runtime090.TryResolveVariant090(
                    member.EnemyArtBaseId090,member.EnemyArtVariantId090,out _,out _),Is.True);
            }
            Assert.That(campaign.Battle.CommittedForecasts.Select(value=>value.UnionId).Distinct().Count(),
                Is.EqualTo(10));
            Assert.That(campaign.Battle.CommittedForecasts.All(value=>value.MemberActions.Count==6),Is.True);
        }

        CampaignState ConfirmEveryUnion094(CampaignState campaign,string preferred)
        {
            foreach(var union in campaign.Battle.PlayerUnions.Where(value=>!value.Retreated && !value.IsDefeated))
            {
                var options=campaign.Battle.CommittedForecasts.Where(value=>value.UnionId==union.UnionId).ToArray();
                var selected=options.FirstOrDefault(value=>value.CommandId==preferred)??options.First();
                campaign=Require094(_commands.SelectForecast(campaign,union.UnionId,selected.ForecastId));
            }
            return Require094(_commands.ConfirmRound(campaign,_content));
        }

        static CampaignState RecordRequest094(CampaignState campaign,EncounterLaunchRequest017D request)
        {
            var guild=campaign.Guild;
            var development=guild.Development.RecordAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request));
            var city=guild.GuildCity.With(pendingEncounter:request,replacePendingEncounter:true);
            guild=guild.With(guild.TreasuryXp,guild.Recruits,guild.Unions,guild.Inventory,development)
                .WithGuildCity(city);
            return campaign.With(guild,campaign.OpeningFlow);
        }

        static EncounterLaunchRequest017D Request094(int count,
            string encounter="ENCOUNTER_RELIEF_ROAD", IReadOnlyList<string> route=null,
            IReadOnlyList<string> allied=null) => new EncounterLaunchRequest017D(
                "REQUEST_FORCE094","CH018_035","EXPEDITION_FORCE094","BOARD_RELIEF_ROAD",
                "NODE_FORCE094",encounter,"BATTLE_FORCE094","Defend the relief road.",count,
                "FORCE094_TEST_COMMITMENT",allied??new[]{"CAPACITY_UNION_01"},Array.Empty<string>(),
                new[]{"OBJECTIVE_FORCE094"},route??Array.Empty<string>(),10,0,0,
                "RETURN_FORCE094","PREBATTLE_FORCE094");

        sealed class ChapterFixture094 : ICampaignRuleCatalog019
        {
            public readonly CampaignChapterRule019 Chapter;
            readonly CampaignArcRule019 _arc;
            public ChapterFixture094(int chapter)
            {
                var id="CH018_"+chapter.ToString("000");
                Chapter=new CampaignChapterRule019{ChapterId=id,ArcId="ARC_FORCE094",
                    WorldId="WORLD_GATE_01",MapIds=new[]{"BOARD_RELIEF_ROAD"},SiegeId=string.Empty,
                    PrimaryObjective="Defend the relief road.",BattleRequired=true,EnemyUnionCount=1,
                    BattleId="CAMPAIGN019_BATTLE_"+id,StoryGates=Array.Empty<string>()};
                _arc=new CampaignArcRule019{ArcId=Chapter.ArcId,WorldId=Chapter.WorldId,
                    ChapterIds=new[]{id},UnlockGates=Array.Empty<string>(),RequiresOwnerApproval=false};
            }
            public bool TryGetArc(string id,out CampaignArcRule019 arc){arc=_arc;return id==_arc.ArcId;}
            public bool TryGetChapter(string id,out CampaignChapterRule019 chapter){chapter=Chapter;return id==Chapter.ChapterId;}
            public string LastChapterId(string id)=>id==_arc.ArcId?Chapter.ChapterId:string.Empty;
        }

        static CampaignState Require094(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));
            return result.Value;
        }

        private static CampaignState CreateArmyFixture094()
        {
            var recruits = new List<RecruitState>();
            var unions = new List<UnionState>();
            for (var unionIndex = 0; unionIndex < 10; unionIndex++)
            {
                var memberIds = new List<string>();
                for (var memberIndex = 0; memberIndex < 6; memberIndex++)
                {
                    var ordinal = unionIndex * 6 + memberIndex + 1;
                    var recruitId = "CAPACITY_RECRUIT_" + ordinal.ToString("00");
                    var item = new EquipmentItemState(
                        "CAPACITY_ITEM_" + ordinal.ToString("00"),
                        "EQ_CAPACITY_SWORD",
                        "Capacity Sword",
                        new[] { EquipmentSlotIds.MainHand },
                        new[] { "SWORD", "WEAPON" },
                        "QUALITY_STANDARD", 10000, false);
                    recruits.Add(new RecruitState(
                        recruitId, 150, 150, 30, 30, "Capacity Recruit " + ordinal,
                        RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01",
                        "CLASS_TEND_WARRIOR", "Observed", 7500, RecruitAuthorityKind.Normal,
                        string.Empty, string.Empty,
                        new EquipmentLoadoutState(new[]
                        {
                            new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, item)
                        }), true, string.Empty, string.Empty, 60 + ordinal, 60 + ordinal));
                    memberIds.Add(recruitId);
                }

                unions.Add(new UnionState(
                    "CAPACITY_UNION_" + (unionIndex + 1).ToString("00"),
                    "Capacity Union " + (unionIndex + 1), UnionKind.Normal, memberIds[0],
                    memberIds.AsReadOnly(), "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 18, 8500));
            }
            var guild = new GuildState("GUILD_M2_CAPACITY", 0, recruits.AsReadOnly(), unions.AsReadOnly());
            var profile = new NewGuildProfileState(
                "Capacity Guildmaster", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000217", 20260817L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }

    }
}
