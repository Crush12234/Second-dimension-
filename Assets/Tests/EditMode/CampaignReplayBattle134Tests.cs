using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign020;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignReplayBattle134Tests
    {
        [Test]
        public void ExistingUnmarkedFirstPassAndReplayBlueprintsRemainExact134()
        {
            var catalog = new Campaign020RuleCatalogAdapter(CampaignRegistry020.LoadFromResources());
            Assert.That(catalog.TryGetBlueprint(CampaignReplayBattle134.ChapterId, out var original), Is.True);
            var before = CanonicalJson.Serialize(original);
            Assert.That(original.RequiresCertifiedBattle, Is.False);
            foreach (var replay in new[] { false, true })
            {
                var state = PolicyOnlyFixture134(false, replay);
                Assert.That(CampaignReplayBattle134.Effective(state, original), Is.SameAs(original));
                Assert.That(CampaignReplayBattle134.Required(state, original.ChapterId), Is.False);
                Assert.That(CanonicalJson.Serialize(original), Is.EqualTo(before));
            }
        }

        [Test]
        public void MarkedResetBlueprintAddsOneNormalBattleAndKeepsPolicyThroughReloadAndClose134()
        {
            var catalog = new Campaign020RuleCatalogAdapter(CampaignRegistry020.LoadFromResources());
            Assert.That(catalog.TryGetBlueprint(CampaignReplayBattle134.ChapterId, out var original), Is.True);
            var state = PolicyOnlyFixture134(true, true);
            var before = CanonicalJson.Serialize(state);
            var effective = CampaignReplayBattle134.Effective(state, original);
            Assert.That(effective.RequiresCertifiedBattle, Is.True);
            Assert.That(effective.BlueprintId, Is.EqualTo(original.BlueprintId + CampaignReplayBattle134.BlueprintSuffix));
            Assert.That(effective.Steps.Count, Is.EqualTo(original.Steps.Count + 1));
            Assert.That(effective.Steps.Count(s => s.RequiresCertifiedBattle), Is.EqualTo(1));
            Assert.That(effective.Steps[effective.Steps.Count - 2].StepId, Is.EqualTo(CampaignReplayBattle134.BattleStepId));
            CollectionAssert.AreEqual(original.Steps.Select(s => s.StepId),
                effective.Steps.Where(s => s.StepId != CampaignReplayBattle134.BattleStepId).Select(s => s.StepId));
            Assert.That(CampaignAdventureRules084.IsCompatible084(effective, out var error), Is.True, error);
            var loaded = JsonConvert.DeserializeObject<CampaignState>(before);
            Assert.That(CanonicalJson.Serialize(CampaignReplayBattle134.Effective(loaded, original)), Is.EqualTo(CanonicalJson.Serialize(effective)));
            var p = Progress134(loaded);
            var op = new CampaignPlayableOperationState020("POLICY_ONLY_OP134", effective.BlueprintId, effective.ChapterId,
                effective.WorldId, "POLICY_ONLY_SEED134", 0, CampaignPlayableOperationStatus020.Active,
                Array.Empty<string>(), Array.Empty<string>(), null, "", "POLICY_ONLY134");
            loaded = WithProgress134(loaded, p.With(activeOperation: null, replaceActiveOperation: true,
                playable020: p.Playable020.With(activeOperation: op, replaceActiveOperation: true), replacePlayable020: true));
            Assert.That(CampaignReplayBattle134.Required(loaded, effective.ChapterId), Is.True,
                "020 must validate the same version after019 closes its active operation.");
            Assert.That(CampaignReplayBattle134.Required(loaded, "CH018_002"), Is.False);
            Assert.That(CanonicalJson.Serialize(state), Is.EqualTo(before));
        }

        [Test, Timeout(240000)]
        public void ActualEarnedResetStoryEncounterUsesFinaleFloorClaimsAndReloads134()
        {
            var source = Environment.GetEnvironmentVariable("SD_REPLAY_BATTLE134_SOURCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
                Assert.Ignore("Set SD_REPLAY_BATTLE134_SOURCE to an actual earned resetQuest1 saved BOSS interruption.");
            var original = File.ReadAllBytes(source);
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SecondDimensionReplayBattle134"));
            var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "earned-copy.json"); File.WriteAllBytes(path, original);
                var owner = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), path);
                var p = Progress134(State134(owner));
                Assert.That(CampaignReplayBattle134.Required(State134(owner), CampaignReplayBattle134.ChapterId), Is.True);
                Assert.That(p.Replay130.CurrentCycle, Is.EqualTo(2));
                var floor = p.Replay130.CycleStarts.Last().Finale132;
                Assert.That(owner.CampaignInterruption132.Kind, Is.EqualTo("BOSS"));
                Command134(owner.CompleteCampaignInterruption132(owner.CampaignInterruption132.Identity));
                var battle = State134(owner).Battle;
                Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
                Assert.That(battle.BattleId, Is.Not.EqualTo(floor.BattleId));
                var enemies = battle.EnemyUnions.SelectMany(u => u.Members).ToArray();
                Assert.That(enemies.Sum(m => (long)m.MaximumHp), Is.GreaterThanOrEqualTo(floor.MinimumTotalHp));
                Assert.That(enemies.Sum(m => (long)m.Attack), Is.GreaterThanOrEqualTo(floor.MinimumTotalAttack));
                Assert.That(enemies.Sum(m => (long)m.MagicAttack), Is.GreaterThanOrEqualTo(floor.MinimumTotalMagic));
                for (var round = 0; State134(owner).Battle.Outcome == BattleOutcome.InProgress && round < 200; round++)
                {
                    Assert.That(M2BattleAutoOrders091.SelectCompletePlan(owner, out var failure), Is.True, failure);
                    Command134(owner.ConfirmBattleRound());
                }
                Assert.That(State134(owner).Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
                Command134(owner.ClaimBattleRewards());
                for (var guard = 0; Progress134(State134(owner)).Playable020.ActiveOperation != null && guard < 12; guard++)
                {
                    var view = owner.CampaignInterruption132; Assert.That(view, Is.Not.Null);
                    if (view.Kind == "RESUME") Command134(owner.ResumeCampaignInterruption132(view.OperationId, view.StepIndex));
                    else Command134(owner.CompleteCampaignInterruption132(view.Identity));
                }
                Assert.That(Progress134(State134(owner)).Playable020.ActiveOperation, Is.Null);
                Assert.That(CampaignReplayRules130.CurrentCompleted(Progress134(State134(owner))), Does.Contain(CampaignReplayBattle134.ChapterId));
                var final = CanonicalJson.Sha256Hex(State134(owner));
                Command134(owner.ClaimBattleRewards());
                Assert.That(CanonicalJson.Sha256Hex(State134(owner)), Is.EqualTo(final));
                var reloaded = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), path);
                Assert.That(CanonicalJson.Sha256Hex(State134(reloaded)), Is.EqualTo(final));
                CollectionAssert.AreEqual(original, File.ReadAllBytes(source));
            }
            finally
            {
                var resolved = Path.GetFullPath(directory);
                Assert.That(Path.GetDirectoryName(resolved), Is.EqualTo(root).IgnoreCase);
                Assert.That(Guid.TryParseExact(Path.GetFileName(resolved), "N", out _), Is.True);
                if (Directory.Exists(resolved))
                {
                    Assert.That(File.GetAttributes(root) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Assert.That(File.GetAttributes(resolved) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Directory.Delete(resolved, true);
                }
            }
        }

        // Model-only version-selection fixture. This does not claim to earn a
        // cycle, spawn battle actors, assign enemy stats, or test a player run.
        static CampaignState PolicyOnlyFixture134(bool marked, bool replay)
        {
            var source = CampaignFactory.CreateM0Proof(134);
            var p = CampaignProgressState019.Default();
            var floor = new CampaignFinaleProof132("MODEL_ONLY_FINALE", "MODEL_ONLY_REWARD", new string('a', 64), 100, 100, 100, 1);
            if (replay) p = p.With(replay130: new CampaignReplayState130(2, 25, Array.Empty<string>(), new[] {
                new CampaignCycleBoundary130(2, 82, 82, new string('b', 64), "MODEL_ONLY_BOUNDARY", floor) }), replaceReplay130: true);
            var objectives = marked ? new[] { CampaignReplayBattle134.ObjectivePolicy, CampaignReplayThreat130.FormatFinaleTag132(2, 25, floor) } : Array.Empty<string>();
            var commit = new CampaignOperationCommit019("MODEL_ONLY_REQUEST", CampaignReplayBattle134.ChapterId,
                "ARC018_FIRST_GATE_ECHOES", "SKYHOME", "MAP017H_SKYHOME_CITY", "", "MODEL_ONLY_SEED", Array.Empty<string>(),
                objectives, "MODEL_ONLY_RETURN", new string('c', 64), false);
            p = p.With(activeOperation: commit, replaceActiveOperation: true, activeChapterId: commit.ChapterId,
                playable020: CampaignPlayableState020.Default(), replacePlayable020: true);
            return WithProgress134(source, p);
        }
        static CampaignState WithProgress134(CampaignState source, CampaignProgressState019 p)
        {
            var city = source.Guild.GuildCity ?? GuildCityState017D.Default(source.Guild.Recruits, source.Guild.Unions);
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            return source.With(source.Guild.WithGuildCity(city.With(strategic017H: strategic.With(campaign019: p,
                replaceCampaign019: true), replaceStrategic017H: true)), source.OpeningFlow);
        }
        static CampaignProgressState019 Progress134(CampaignState state) => state.Guild.GuildCity.Strategic017H.Campaign019;
        static CampaignState State134(M1RuntimeCoordinator owner) => (CampaignState)typeof(M1RuntimeCoordinator)
            .GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        static void Command134(M1CommandResult command) => Assert.That(command.Succeeded, Is.True, command.Message);
    }
}
