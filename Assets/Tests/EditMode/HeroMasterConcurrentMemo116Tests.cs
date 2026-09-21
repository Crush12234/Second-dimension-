using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroMasterConcurrentMemo116Tests
    {
        M2CombatContent _combat;
        RecruitAutoGenerationCatalog010 _generated;
        RecruitState[] _recruits;

        [OneTimeSetUp]
        public void Load116()
        {
            _combat = HeroRosterAudit093.LoadCombatContent093();
            _generated = RecruitAutoGenerationCatalog010.LoadFromContentRoot(HeroRosterAudit093.ContentRoot093);
            _recruits = new[] { 46, 179, 180, 181 }.Select(id => {
                var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == id);
                return HeroRosterAudit093.SignOutfitAndPlace093(
                    HeroRosterAudit093.CreateFixture093(hero), hero, new HeroRosterAuditRow093()).Guild.Recruits.Single();
            }).ToArray();
        }

        [Test, Timeout(60000)]
        public void ConcurrentColdAndWarmProfilesRetainExactlyTheSequentialAuthority116()
        {
            var authority = NewAuthority();
            var expected = NewAuthority();
            var hashes = _recruits.Select(recruit => {
                Assert.That(expected.TryDescribe(recruit, out var profile), Is.True);
                return CanonicalJson.Sha256Hex(profile);
            }).ToArray();
            var before = CanonicalJson.Sha256Hex(_recruits);
            var profiles = new GeneratedRecruitProfile010[8];
            ParallelCold116(index => {
                // Every worker races the same four initially cold memo keys.
                for (var pass = 0; pass < 3; pass++)
                    for (var recruitIndex = 0; recruitIndex < _recruits.Length; recruitIndex++)
                    {
                        if (!authority.TryDescribe(_recruits[recruitIndex], out var profile))
                            throw new InvalidOperationException("Valid authority rejected a concurrent cold/warm record.");
                        if (CanonicalJson.Sha256Hex(profile) != hashes[recruitIndex])
                            throw new InvalidOperationException("Concurrent profile differs from sequential authority.");
                        if (recruitIndex == 0) profiles[index] = profile;
                    }
            });
            Assert.That(profiles.All(profile => ReferenceEquals(profile, profiles[0])), Is.True,
                "Concurrent misses publish one existing memo value, preserving warm reference semantics.");
            Assert.That(CanonicalJson.Sha256Hex(_recruits), Is.EqualTo(before));
        }

        [Test, Timeout(60000)]
        public void ConcurrentColdInvalidAndWarmOuterIdentityChecksCannotBorrowAcceptedMemo116()
        {
            var authority = NewAuthority();
            var original = _recruits[0];
            Assert.That(authority.TryDescribe(original, out var accepted), Is.True);
            var profileHash = CanonicalJson.Sha256Hex(accepted);
            var invalid = new List<RecruitState>();
            foreach (var kind in new[] { "canonical", "race", "class", "signature" })
            {
                var token = JObject.FromObject(original);
                if (kind == "canonical")
                {
                    var record = JObject.Parse(original.CanonicalApplicantJson);
                    record["displayName"] = "Not the accepted hero";
                    token["CanonicalApplicantJson"] = record.ToString(Formatting.None);
                }
                else if (kind == "race") token["RaceId"] = "INVALID_RACE116";
                else if (kind == "class") token["ClassTendencyId"] = "INVALID_CLASS116";
                else token["SignatureId"] = "UNTRUSTED_SIGNATURE116";
                invalid.Add(token.ToObject<RecruitState>());
            }
            ParallelCold116(index => {
                for (var pass = 0; pass < 10; pass++)
                {
                    if (!authority.TryDescribe(original, out var actual) || !ReferenceEquals(actual, accepted))
                        throw new InvalidOperationException("Valid warm authority changed during negative reads.");
                    foreach (var forged in invalid)
                        if (authority.TryDescribe(forged, out _))
                            throw new InvalidOperationException("Forged authority borrowed a cached accepted profile.");
                }
            });
            Assert.That(CanonicalJson.Sha256Hex(accepted), Is.EqualTo(profileHash), "Published profiles are only read by these authority consumers.");
        }

        HeroMasterGeneratedBattleAuthority096 NewAuthority() => new HeroMasterGeneratedBattleAuthority096(
            HeroRosterAudit093.Catalog093, _generated, _combat.DeepProgression);

        static void ParallelCold116(Action<int> action)
        {
            using (var ready = new CountdownEvent(8))
            using (var start = new ManualResetEventSlim(false))
            {
                var tasks = Enumerable.Range(0, 8).Select(index => Task.Factory.StartNew(() => {
                    ready.Signal();
                    if (!start.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Concurrent start did not release.");
                    action(index);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
                Assert.That(ready.Wait(TimeSpan.FromSeconds(10)), Is.True);
                start.Set();
                Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(40)), Is.True);
            }
        }
    }
}
