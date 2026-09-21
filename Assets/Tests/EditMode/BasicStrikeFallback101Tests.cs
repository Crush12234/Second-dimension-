using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>Isolated authority fixtures, not earned native campaign/balance proof.</summary>
    public sealed class BasicStrikeFallback101Tests
    {
        M2CombatContent _content;
        readonly M2BattleCommandService _commands = new M2BattleCommandService();
        const string Assist = "ART_ASSIST_ALLY";
        const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        [OneTimeSetUp]
        public void Load101() => _content = HeroRosterAudit093.LoadCombatContent093();

        [Test]
        public void BasicStrikeIsNotAReplacementOrRelabelOfAuthoredSupport101()
        {
            var basic = _content.Art(BasicStrikeFallback101.ArtId);
            Assert.That(basic.Name, Is.EqualTo("Basic Strike"));
            Assert.That(basic.Family, Is.EqualTo("BASIC"));
            Assert.That(basic.Discipline, Is.EqualTo("Martial"));
            Assert.That(basic.SharedApCost, Is.EqualTo(1));
            Assert.That(basic.PersonalMpCost, Is.Zero);
            Assert.That(basic.RequiredEquipmentTags, Is.Empty);
            Assert.That(basic.PowerCoefficientPermille, Is.EqualTo(1000));
            Assert.That(basic.EffectTags, Does.Contain("HP_DAMAGE"));
            Assert.That(basic.PlayerDirectlySelectableInStandard, Is.False);
            Assert.That(M2BattleCommandService.IsRevivalArtForVerification080(basic), Is.False);
            var assist = _content.Art(Assist);
            Assert.That(assist.Name, Is.EqualTo("Assist Ally"));
            Assert.That(assist.Family, Is.EqualTo("SUPPORT"));
            Assert.That(assist.Discipline, Is.EqualTo("Support"));
            Assert.That(assist.IntentTags, Does.Contain("SUPPORT").And.Contain("RESCUE"));
            Assert.That(assist.EffectTags, Does.Contain("ASSIST").And.Not.Contain("HP_DAMAGE"));
            Assert.That(assist.SharedApCost, Is.EqualTo(basic.SharedApCost));
            Assert.That(assist.PersonalMpCost, Is.EqualTo(basic.PersonalMpCost));
            Assert.That(_content.Art("ART_SHIELD_BASH").SharedApCost, Is.EqualTo(4));
        }

        [TestCase("SHIELD")]
        [TestCase("GAUNTLETS")]
        [TestCase("TOOL")]
        [TestCase("UNSUPPORTED_TEST_ONLY")]
        [TestCase("")]
        public void UnsupportedEquipmentExecutesHonestBasicStrikeAndDoesNotTrainAssist101(string tag)
        {
            var campaign = Fixture101(tag, 1);
            var persistent = CanonicalJson.Serialize(campaign.Guild.Recruits);
            var started = Require101(_commands.StartEncounterBattle(campaign, _content,
                "BASIC_STRIKE101_" + tag, "Isolated real command regression", 1));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.Recruits), Is.EqualTo(persistent));
            var before = started.Battle.PlayerUnions.Single();
            var member = before.Members.Single();
            Assert.That(member.LearnedArtIds, Does.Contain(BasicStrikeFallback101.ArtId).And.Contain(Assist));
            Assert.That(member.LearnedArtIds, Does.Not.Contain("ART_SHIELD_BASH").And.Not.Contain("ART_SHIELD_RAM"));
            var forecast = started.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_BALANCED");
            var action = forecast.MemberActions.Single();
            AssertStrike101(started, action);
            var enemyHp = started.Battle.EnemyUnions.SelectMany(value => value.Members).Sum(value => value.CurrentHp);
            var assistProgressBefore = member.ArtProgress.Where(value => value.ArtId == Assist).ToArray();
            var selected = Require101(_commands.SelectForecast(started, forecast.UnionId, forecast.ForecastId));
            var resolved = Require101(_commands.ConfirmRound(selected, _content));
            var hit = resolved.Battle.RoundRecords.Last().Events.Single(value =>
                value.EventType == "MARTIAL_HIT" && value.ActorMemberId == member.MemberId);
            Assert.That(hit.ArtId, Is.EqualTo(BasicStrikeFallback101.ArtId));
            Assert.That(hit.Amount, Is.GreaterThan(0));
            Assert.That(resolved.Battle.EnemyUnions.SelectMany(value => value.Members).Sum(value => value.CurrentHp),
                Is.LessThan(enemyHp));
            var after = resolved.Battle.PlayerUnions.Single();
            Assert.That(after.CurrentAp, Is.EqualTo(Math.Min(before.MaximumAp, before.CurrentAp - 1 + 3)),
                "Pay one actual base AP, then only the existing round recovery.");
            Assert.That(after.Members.Single().CurrentMp, Is.EqualTo(member.CurrentMp));
            var growth = after.Members.Single().ArtProgress.Single(value => value.ArtId == BasicStrikeFallback101.ArtId);
            Assert.That(growth.MeaningfulUses, Is.EqualTo(1));
            Assert.That(growth.Discipline, Is.EqualTo("Martial"));
            Assert.That(CanonicalJson.Serialize(after.Members.Single().ArtProgress.Where(value => value.ArtId == Assist).ToArray()),
                Is.EqualTo(CanonicalJson.Serialize(assistProgressBefore)), "Damage never gives support mastery.");
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value =>
                value.ArtId == Assist && value.EventType.EndsWith("_HIT", StringComparison.Ordinal)), Is.False);
        }

        [TestCase(72)] [TestCase(84)] [TestCase(87)] [TestCase(99)] [TestCase(102)] [TestCase(108)]
        [TestCase(187)] [TestCase(234)] [TestCase(262)] [TestCase(266)] [TestCase(272)] [TestCase(279)]
        [TestCase(280)] [TestCase(286)] [TestCase(294)] [TestCase(295)] [TestCase(298)] [TestCase(299)]
        public void ExactEighteenNativeFindingsUseExistingIdentityGearAndRealStrike101(int rosterId)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == rosterId);
            var state = HeroRosterAudit093.SignOutfitAndPlace093(HeroRosterAudit093.CreateFixture093(hero),
                hero, new HeroRosterAuditRow093());
            var savedRecruitHash = CanonicalJson.Sha256Hex(state.Guild.Recruits);
            var started = HeroRosterAudit093.StartBattleAndVerifyForecast093(state, new HeroRosterAuditRow093());
            Assert.That(CanonicalJson.Sha256Hex(state.Guild.Recruits), Is.EqualTo(savedRecruitHash));
            Assert.That(started.Battle.PlayerUnions.Single().Members.Single().MemberId,
                Is.EqualTo(state.Guild.Recruits.Single().RecruitId));
            var forecast = started.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_BALANCED");
            AssertStrike101(started, forecast.MemberActions.Single());
            Assert.That(started.Battle.CommittedForecasts.SelectMany(value => value.MemberActions)
                .Any(value => value.ArtId == Assist && value.PredictedHpDelta < 0), Is.False);
            var selected = Require101(_commands.SelectForecast(started, forecast.UnionId, forecast.ForecastId));
            var resolved = Require101(_commands.ConfirmRound(selected, _content));
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value =>
                value.ArtId == BasicStrikeFallback101.ArtId && value.EventType == "MARTIAL_HIT" && value.Amount > 0), Is.True);
            Assert.That(resolved.Battle.PlayerUnions.SelectMany(value => value.Members).Single().ArtProgress
                .Single(value => value.ArtId == BasicStrikeFallback101.ArtId).MeaningfulUses, Is.EqualTo(1));
        }

        [Test]
        public void GenuineAssistAllyStillTargetsFriendlyUnionAndExecutesSupport101()
        {
            var started = Require101(_commands.StartEncounterBattle(Fixture101("SHIELD", 1, 1), _content,
                "ASSIST_SUPPORT101", "Preserve authored assistance", 1, new[] { "HIGH_FATIGUE" }));
            foreach (var union in started.Battle.PlayerUnions)
            {
                var support = started.Battle.CommittedForecasts.Single(value =>
                    value.UnionId == union.UnionId && value.CommandId == "CMD_SUPPORT");
                var action = support.MemberActions.Single();
                Assert.That(action.ArtId, Is.EqualTo(Assist));
                Assert.That(action.Kind, Is.EqualTo(BattleActionKind.Recovery));
                Assert.That(action.PredictedHpDelta, Is.Zero);
                Assert.That(started.Battle.PlayerUnions.Any(value => value.UnionId == action.TargetUnionId), Is.True);
                started = Require101(_commands.SelectForecast(started, union.UnionId, support.ForecastId));
            }
            var resolved = Require101(_commands.ConfirmRound(started, _content));
            var events = resolved.Battle.RoundRecords.Last().Events;
            Assert.That(events.Any(value => value.ArtId == Assist && value.EventType == "ALLY_SUPPORT" && value.Amount > 0), Is.True);
            Assert.That(events.Any(value => value.ArtId == Assist && value.EventType.EndsWith("_HIT", StringComparison.Ordinal)), Is.False);
            Assert.That(resolved.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Any(value => value.ArtProgress.Any(art => art.ArtId == Assist && art.Discipline == "Support" && art.MeaningfulUses > 0)), Is.True);
        }

        [TestCase("CMD_BALANCED")]
        [TestCase("CMD_FLANK")]
        public void ExhaustedApWithholdsStrikeWithoutFreeDamage101(string command)
        {
            var state = Require101(_commands.StartEncounterBattle(Fixture101("SHIELD", 1, 1), _content,
                "NO_AP_STRIKE101", "Actual zero-budget Forecast", 1));
            var battle = state.Battle.With(playerUnions: state.Battle.PlayerUnions
                .Select(value => value.With(currentAp: 0)).ToArray());
            var commit = typeof(M2BattleCommandService).GetMethod("CommitForecasts", StaticPrivate);
            Assert.That(commit, Is.Not.Null);
            state = state.WithBattle((BattleState)commit.Invoke(null, new object[] { state, battle, _content }));
            var basic = state.Battle.CommittedForecasts.First(value => value.CommandId == command);
            Assert.That(basic.MemberActions.Single().ArtId, Is.EqualTo("ART_RECOVER_BREATH"));
            Assert.That(basic.MemberActions.Single().PredictedHpDelta, Is.Zero);
            foreach (var union in state.Battle.PlayerUnions)
            {
                var choice = union.UnionId == basic.UnionId ? basic : state.Battle.CommittedForecasts.Single(value =>
                    value.UnionId == union.UnionId && value.CommandId == "CMD_BALANCED");
                state = Require101(_commands.SelectForecast(state, union.UnionId, choice.ForecastId));
            }
            var resolved = Require101(_commands.ConfirmRound(state, _content));
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value =>
                value.Side == BattleSide.Player && value.EventType.EndsWith("_HIT", StringComparison.Ordinal)), Is.False);
        }

        [TestCase(M2BattleCommandService.PreviousTutorialRulesVersion088, false)]
        [TestCase(M2BattleCommandService.PreviousTutorialRulesVersion101, true)]
        [TestCase(M2BattleCommandService.TutorialRulesVersion, true)]
        public void RecordedPolicyPendingSaveRoundAndReplayStayExact101(string policy, bool masteryScaling)
        {
            // Private replay seam creates a historical-policy unit fixture. It does
            // not edit a real save or fabricate an earned campaign completion.
            var source = Fixture101("SHIELD", 1);
            var method = typeof(M2BattleCommandService).GetMethod("StartEncounterBattleInternal",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var state = Require101((Result<CampaignState>)method.Invoke(_commands, new object[] {
                source, _content, M2BattleCommandService.TutorialBattleId,
                "Defeat the Gate Gnawer training projection — or withdraw safely.", 1,
                Array.Empty<string>(), null, null, false, null, policy }));
            Assert.That(state.Battle.ContentVersion, Is.EqualTo(_content.ContentVersion + "|" + policy));
            Assert.That(M2BattleCommandService.UsesCurrentTutorialRules(state.Battle, _content), Is.True);
            Assert.That(M2BattleCommandService.UsesArtMasteryLevelScaling088(state.Battle, _content), Is.EqualTo(masteryScaling));
            var expected = policy == M2BattleCommandService.TutorialRulesVersion ? BasicStrikeFallback101.ArtId : Assist;
            var forecast = state.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_BALANCED");
            Assert.That(forecast.MemberActions.Single().ArtId, Is.EqualTo(expected));
            var selected = Require101(_commands.SelectForecast(state, forecast.UnionId, forecast.ForecastId));
            var hash = CanonicalJson.Sha256Hex(selected);
            var loaded = RoundTrip101(selected);
            Assert.That(CanonicalJson.Sha256Hex(Require101(_commands.StartTutorialBattle(loaded, _content))), Is.EqualTo(hash),
                "Resuming a pending battle cannot rewrite recorded policy, learned Arts, selection or Forecast.");
            var first = Require101(_commands.ConfirmRound(selected, _content));
            var second = Require101(_commands.ConfirmRound(loaded, _content));
            Assert.That(CanonicalJson.Sha256Hex(second), Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            Assert.That(first.Battle.RoundRecords.Last().Events.Any(value => value.ArtId == expected &&
                value.EventType == "MARTIAL_HIT"), Is.True);
            if (first.Battle.Outcome == BattleOutcome.InProgress)
                Assert.That(first.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_BALANCED")
                    .MemberActions.Single().ArtId, Is.EqualTo(expected), "The rest of an old battle stays on its old policy.");
            var replay = Require101(_commands.ReplayTutorialBattle(RoundTrip101(first), _content));
            Assert.That(CanonicalJson.Sha256Hex(replay.Battle), Is.EqualTo(CanonicalJson.Sha256Hex(first.Battle)));
            Assert.That(CanonicalJson.Sha256Hex(selected), Is.EqualTo(hash));
        }

        [TestCase("SWORD", "ART_BASIC_SABER_CUT")]
        [TestCase("BOW", "ART_BASIC_SHOT")]
        [TestCase("STAFF", "ART_BASIC_STAFF_TAP")]
        [TestCase("HAMMER", "ART_BASIC_HEAVY_SWING")]
        [TestCase("DAGGER", "ART_BASIC_DAGGER_CUT")]
        [TestCase("SPEAR", "ART_BASIC_THRUST")]
        [TestCase("WAND", "ART_BASIC_RUNE_BOLT")]
        public void ExistingSupportedBasicArtAndAuthoredCostsRemainUnchanged101(string tag, string artId)
        {
            var state = Require101(_commands.StartEncounterBattle(Fixture101(tag, 1), _content,
                "EXISTING_BASIC101_" + tag, "Existing weapon authority", 1));
            var action = state.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_BALANCED").MemberActions.Single();
            Assert.That(action.ArtId, Is.EqualTo(artId));
            Assert.That(action.SharedApCost, Is.EqualTo(_content.Art(artId).SharedApCost));
            Assert.That(action.PersonalMpCost, Is.EqualTo(_content.Art(artId).PersonalMpCost));
            Assert.That(state.Battle.PlayerUnions.Single().Members.Single().LearnedArtIds,
                Does.Not.Contain(BasicStrikeFallback101.ArtId));
        }

        [Test]
        public void UntouchedR173PendingSaveRegeneratesItsActualHistoricalForecasts101()
        {
            const string fileSha = "7F7A9ED22F467580D46C74B63BA1FD41BEDFD31AA1536725C411024EAF4FEFB9";
            const string stateSha = "943dbcf8fc9807359de20697c916c93c5d382a8a12daed21ae8452bef656421f";
            var path = Path.Combine(Application.dataPath, "Tests", "EditMode", "Fixtures",
                "R173_Hero072_OldPending101.fixture.json");
            using (var sha = System.Security.Cryptography.SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""), Is.EqualTo(fileSha));
            var loaded = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            var state = loaded.Value.CampaignState;
            Assert.That(CanonicalJson.Sha256Hex(state), Is.EqualTo(stateSha));
            Assert.That(state.Battle.ContentVersion, Is.EqualTo(_content.ContentVersion + "|" + M2BattleCommandService.PreviousTutorialRulesVersion101));
            var committed = CanonicalJson.Serialize(state.Battle.CommittedForecasts);
            var rebuilt = (BattleState)typeof(M2BattleCommandService).GetMethod("CommitForecasts", StaticPrivate)
                .Invoke(null, new object[] { state, state.Battle, _content });
            Assert.That(CanonicalJson.Serialize(rebuilt.CommittedForecasts), Is.EqualTo(committed),
                "This golden pre-patch save independently fixes the old Forecast IDs, costs, targets, damage and RNG.");
            // This original synthetic roster fixture already contains a loaded
            // pending battle but not completed opening-flow flags. Continue its
            // committed selection/resolution authority directly; starting another
            // encounter would legitimately require a completed opening flow.
            Assert.That(state.Battle.Phase, Is.EqualTo(BattlePhase.ForecastSelection));
            Assert.That(state.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(M2BattleCommandService.UsesCurrentTutorialRules(state.Battle, _content), Is.True);
            var basic = state.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_BALANCED");
            Assert.That(basic.MemberActions.Single().ArtId, Is.EqualTo(Assist),
                "A committed old action is grandfathered, not silently relabelled.");
            var selected = Require101(_commands.SelectForecast(state, basic.UnionId, basic.ForecastId));
            var first = Require101(_commands.ConfirmRound(selected, _content));
            var again = Require101(_commands.ConfirmRound(RoundTrip101(selected), _content));
            Assert.That(CanonicalJson.Sha256Hex(first), Is.EqualTo(CanonicalJson.Sha256Hex(again)));
            Assert.That(first.Battle.RoundRecords.Last().Events.Any(value => value.ArtId == Assist && value.EventType == "MARTIAL_HIT"), Is.True);
            Assert.That(CanonicalJson.Sha256Hex(state), Is.EqualTo(stateSha));
            using (var sha = System.Security.Cryptography.SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""), Is.EqualTo(fileSha));
        }

        [Test]
        public void ActualVictoryClaimReloadAndNextBattleRetainBasicMasteryExactlyOnce101()
        {
            var source = Fixture101("SHIELD", 3, 3);
            var sourceHash = CanonicalJson.Sha256Hex(source);
            var state = Require101(_commands.StartEncounterBattle(source, _content,
                "BASIC_STRIKE_CLAIM101", "Isolated earned battle reward regression", 1));
            for (var round = 0; round < 20 && state.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                foreach (var union in state.Battle.PlayerUnions.Where(value => !value.IsDefeated && !value.Retreated))
                {
                    var choice = state.Battle.CommittedForecasts.Single(value =>
                        value.UnionId == union.UnionId && value.CommandId == "CMD_BALANCED");
                    state = Require101(_commands.SelectForecast(state, union.UnionId, choice.ForecastId));
                }
                state = Require101(_commands.ConfirmRound(state, _content));
            }
            Assert.That(state.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory), "No injected terminal flags or reward.");
            var member = state.Battle.PlayerUnions.SelectMany(value => value.Members).First();
            var earned = member.ArtProgress.Single(value => value.ArtId == BasicStrikeFallback101.ArtId);
            Assert.That(earned.MeaningfulUses, Is.GreaterThan(0));
            var claimed = Require101(_commands.ClaimBattleRewards(RoundTrip101(state)));
            Assert.That(claimed.Battle.Reward.Claimed, Is.True);
            var recruit = claimed.Guild.Recruits.Single(value => value.RecruitId == member.MemberId);
            Assert.That(recruit.Progression.LearnedArtIds, Does.Contain(Assist).And.Contain(BasicStrikeFallback101.ArtId));
            var persistent = recruit.Progression.ArtMastery.Single(value => value.ArtId == BasicStrikeFallback101.ArtId);
            Assert.That(persistent.MeaningfulUses, Is.EqualTo(earned.MeaningfulUses));
            Assert.That(persistent.MasteryPoints, Is.EqualTo(earned.MasteryPoints));
            Assert.That(persistent.Discipline, Is.EqualTo("Martial"));
            var claimedHash = CanonicalJson.Sha256Hex(claimed);
            var loaded = RoundTrip101(claimed);
            Assert.That(CanonicalJson.Sha256Hex(Require101(_commands.ClaimBattleRewards(loaded))), Is.EqualTo(claimedHash));
            var next = Require101(_commands.StartEncounterBattle(loaded, _content,
                "BASIC_STRIKE_NEXT101", "Next genuine battle projection", 1));
            var carried = next.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == member.MemberId).ArtProgress.Single(value => value.ArtId == BasicStrikeFallback101.ArtId);
            Assert.That(CanonicalJson.Serialize(carried), Is.EqualTo(CanonicalJson.Serialize(earned)));
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(sourceHash));
        }

        static CampaignState Fixture101(string tag, params int[] sizes)
        {
            // Reuse the existing isolated battle fixture, never a personal/earned save.
            var factory = typeof(M2BattleForecastTests).GetMethod("CreateCampaign", StaticPrivate);
            Assert.That(factory, Is.Not.Null);
            var state = (CampaignState)factory.Invoke(null, new object[] { sizes });
            var recruits = state.Guild.Recruits.Select(source => {
                var gear = new EquipmentItemState("BASIC101_" + source.RecruitId, "BASIC101_TEST",
                    "Isolated equipment", new[] { EquipmentSlotIds.MainHand },
                    string.IsNullOrEmpty(tag) ? Array.Empty<string>() : new[] { tag }, "QUALITY_STANDARD", 10000, false);
                var progression = source.Progression.WithArts(new[] { Assist }, source.Progression.ArtMastery);
                return new RecruitState(source.RecruitId, source.CurrentHp, source.MaximumHp, source.CurrentMp, source.MaximumMp,
                    source.DisplayName, source.OriginKind, source.SignatureId, source.RaceId, source.WorldId,
                    "CLASS_TEND_SUPPORT", source.LeadershipBand, source.PotentialBasisPoints, source.AuthorityKind,
                    source.CanonicalApplicantJson, source.CanonicalScoutingReportJson,
                    new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, gear) }),
                    source.VitalsInitialized, source.TutorialAliasId, source.AuthoredStableRecruitId,
                    source.LeadershipScore, source.TacticalAptitude, progression);
            }).ToArray();
            return state.With(state.Guild.With(state.Guild.TreasuryXp, recruits, state.Guild.Unions, state.Guild.Inventory), state.OpeningFlow);
        }

        static void AssertStrike101(CampaignState state, BattlePlannedActionState action)
        {
            Assert.That(action.ArtId, Is.EqualTo(BasicStrikeFallback101.ArtId));
            Assert.That(action.ArtName, Is.EqualTo("Basic Strike"));
            Assert.That(action.Discipline, Is.EqualTo("Martial"));
            Assert.That(action.Kind, Is.EqualTo(BattleActionKind.Martial));
            Assert.That(action.PredictedHpDelta, Is.LessThan(0));
            Assert.That(action.SharedApCost, Is.EqualTo(1));
            Assert.That(action.PersonalMpCost, Is.Zero);
            Assert.That(state.Battle.EnemyUnions.Any(value => value.UnionId == action.TargetUnionId &&
                value.Members.Any(member => member.MemberId == action.TargetMemberId && !member.Downed)), Is.True);
        }

        static CampaignState RoundTrip101(CampaignState state)
        {
            var directory = Path.Combine(Path.GetTempPath(), "BasicStrike101_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "battle.json");
                new AtomicSaveStore().Write(path, SaveEnvelopeV1.Create(state,
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState), Is.EqualTo(CanonicalJson.Sha256Hex(state)));
                return loaded.Value.CampaignState;
            }
            finally { Directory.Delete(directory, true); }
        }

        static CampaignState Require101(Result<CampaignState> result)
        { Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors)); return result.Value; }
    }
}
