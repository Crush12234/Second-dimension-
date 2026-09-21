using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class OrdinaryAreaForecast095Tests
    {
        const string Arc = "TREE_CA002_WPN_BOW_N03";
        const string Storm = "TREE_CA002_MYS_STORM_N10";
        M2CombatContent _content;
        M2BattleCommandService _commands;

        [OneTimeSetUp]
        public void Load095()
        {
            _content = M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _commands = new M2BattleCommandService();
        }

        [TestCase("TREE_CA002_WPN_STAFF_N02", 5, false)]
        [TestCase("TREE_CA002_WPN_GREAT_WEAPON_N03", 5, false)]
        [TestCase(Arc, 5, false)]
        [TestCase("TREE_CA002_WPN_AXE_N06", 5, false)]
        [TestCase("TREE_CA002_WPN_BOW_N08", 5, false)]
        [TestCase("ART_SPLIT_VOLLEY", 3, false)]
        [TestCase("ART_CONVERGENCE", 5, false)]
        [TestCase("TREE_CA002_MYS_FLAME_N10", 5, true)]
        [TestCase(Storm, 5, true)]
        public void ReviewedArtKeepsActualLearnedForecastAndExplicitBoundedScope(string artId, int cap, bool cross)
        {
            var state = Start(artId, 2, 6);
            var action = AreaAction(state, artId);
            Assert.That(action.AreaActionPlan095.MaximumTargets, Is.EqualTo(cap));
            Assert.That(action.AreaActionPlan095.Recipients.Count, Is.EqualTo(cap));
            Assert.That(action.AreaActionPlan095.Recipients.Select(value => value.UnionId).Distinct().Count(),
                Is.EqualTo(cross ? 2 : 1));
            Assert.That(action.Prediction, Does.Contain("targets"));
            Assert.That(action.Prediction, Does.Contain("one Art cost"));
            Assert.That(_content.Art(artId).PlayerDirectlySelectableInStandard, Is.False);
            var resolved = ResolveArea(state, artId);
            Assert.That(Hits(resolved, artId, BattleSide.Player).Length, Is.EqualTo(cap));
        }

        [TestCase(Arc, 1)] [TestCase(Arc, 2)] [TestCase(Arc, 5)]
        [TestCase(Storm, 1)] [TestCase(Storm, 2)] [TestCase(Storm, 5)]
        public void RealForecastDamageIsOneAuthoredPoolAtOneTwoOrFiveRecipients(string artId, int count)
        {
            var state = Start(artId, 1, count);
            var forecast = AreaForecast(state, artId);
            var action = forecast.MemberActions.Single();
            var actor = state.Battle.PlayerUnions.Single().Members.Single();
            var art = _content.Art(artId);
            var multiplier = forecast.CommandId == "CMD_ALL_OUT" ? 130 : forecast.CommandId == "CMD_FLANK" ? 115 : 100;
            if (state.Battle.PlayerUnions.Single().FormationBenefitActive) multiplier += 5;
            var power = action.Kind == BattleActionKind.Mystic ? actor.MagicAttack : actor.Attack;
            var expected = (10 + power * multiplier / 100) * art.AreaProfile095.DamageCoefficientPermille / 1000;
            Assert.That(action.AreaActionPlan095.TotalDamageBudget, Is.EqualTo(expected),
                "Authored coefficient replaces generic node power, and Art1 scales only once.");
            Assert.That(action.AreaActionPlan095.Recipients.Sum(value => value.DamageBudget), Is.EqualTo(expected));
            var resolved = ResolveArea(state, artId);
            var hits = Hits(resolved, artId, BattleSide.Player);
            Assert.That(hits.Length, Is.EqualTo(count));
            Assert.That(hits.Sum(value => value.Amount), Is.EqualTo(expected));
            Assert.That(resolved.Battle.PlayerUnions.Single().Members.Single().CurrentMp,
                Is.EqualTo(actor.CurrentMp - art.PersonalMpCost));
            Assert.That(resolved.Battle.PlayerUnions.Single().CurrentAp,
                Is.EqualTo(Math.Min(state.Battle.PlayerUnions.Single().MaximumAp,
                    state.Battle.PlayerUnions.Single().CurrentAp - forecast.SharedApCost + forecast.ApRecovery + 3)),
                "Pay the complete Forecast once, then retain the existing +3 AP round recovery.");
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Count(value =>
                value.EventType == "ART_GROWTH" && value.Side == BattleSide.Player && value.ArtId == artId), Is.EqualTo(1));
            var targetBefore = state.Battle.EnemyUnions.Single();
            var targetAfter = resolved.Battle.EnemyUnions.Single();
            Assert.That(targetBefore.Cohesion - targetAfter.Cohesion, Is.EqualTo(action.AreaActionPlan095.TotalCohesionBudget));
            Assert.That(targetBefore.FormationConditionBasisPoints - targetAfter.FormationConditionBasisPoints,
                Is.EqualTo(action.AreaActionPlan095.TotalFormationBudget));
        }

        [Test]
        public void WholeArtGuardSnapshotHalvesEveryRecipientWithoutMultiplyingSecondaryPulse()
        {
            var state = Start(Arc, 1, 5);
            state = Reforecast(state.WithBattle(state.Battle.With(enemyUnions:
                state.Battle.EnemyUnions.Select(value => value.With(guarding: true)).ToArray())));
            var plan = AreaAction(state, Arc).AreaActionPlan095;
            Assert.That(plan.Recipients.All(value => value.Guarded), Is.True);
            var resolved = ResolveArea(state, Arc);
            var hits = Hits(resolved, Arc, BattleSide.Player);
            Assert.That(hits.Select(value => value.Amount), Is.EqualTo(plan.Recipients.Select(value => value.PredictedHpLoss)));
            Assert.That(hits.Sum(value => value.Amount), Is.LessThanOrEqualTo(plan.TotalDamageBudget));
            Assert.That(state.Battle.EnemyUnions[0].Cohesion - resolved.Battle.EnemyUnions[0].Cohesion,
                Is.EqualTo(plan.TotalCohesionBudget));
        }

        [TestCase(1)] [TestCase(5)] [TestCase(10)]
        public void IndependentArtMasteryScalesTheWholeAreaPoolExactlyOnce(int level)
        {
            var points = M2ArtMasteryLevelPolicy088.ThresholdForLevel(level);
            var state = Start(Storm, 1, 5, masteryPoints: points);
            var forecast = AreaForecast(state, Storm);
            var action = AreaAction(state, Storm);
            var actor = state.Battle.PlayerUnions.Single().Members.Single();
            var multiplier = state.Battle.PlayerUnions.Single().FormationBenefitActive ? 105 : 100;
            var basePool = (10 + actor.MagicAttack * multiplier / 100) * 1960 / 1000;
            var expected = M2ArtMasteryLevelPolicy088.ScaleMagnitudeByPowerPermille(basePool,
                M2ArtMasteryLevelPolicy088.PowerPermilleForMasteryPoints(points));
            Assert.That(action.AreaActionPlan095.TotalDamageBudget, Is.EqualTo(expected));
            Assert.That(Hits(ResolveArea(state, Storm), Storm, BattleSide.Player).Sum(value => value.Amount), Is.EqualTo(expected));
        }

        [Test]
        public void TwoPointPoolAcrossFiveGuardedTargetsNeverCreatesFiveMinimumHits()
        {
            var state = Start(Arc, 1, 5);
            var targets = state.Battle.EnemyUnions.Select(value => value.With(guarding: true)).ToArray();
            var method = typeof(M2BattleCommandService).GetMethod("BuildAreaRecipients095", BindingFlags.NonPublic | BindingFlags.Static);
            var plan = (BattleAreaActionPlan095)method.Invoke(null, new object[] { Arc,
                M2AreaArtProfile095.SelectedUnion, 5, 2, 0, 0, targets[0].UnionId,
                targets[0].Members[0].MemberId, targets, "CMD_ALL_OUT" });
            Assert.That(plan.Recipients.Select(value => value.DamageBudget), Is.EqualTo(new[] { 1, 1, 0, 0, 0 }));
            Assert.That(plan.PredictedHpLoss, Is.EqualTo(2));
            var action = AreaAction(state, Arc);
            var projected = (BattlePlannedActionState)typeof(M2BattleCommandService)
                .GetMethod("WithAreaPlan095", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { action, plan });
            var events = new List<BattleEventState>();
            var actualTargets = targets.ToList();
            var useful = (int)typeof(M2BattleCommandService)
                .GetMethod("ResolveAreaAttack095", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { 1, projected, state.Battle.PlayerUnions.ToList(), actualTargets, events, BattleSide.Player });
            Assert.That(useful, Is.EqualTo(2));
            Assert.That(events.Count(value => value.EventType == "MARTIAL_HIT" || value.EventType == "TACTICAL_HIT"), Is.EqualTo(2));
            Assert.That(actualTargets[0].Cohesion, Is.EqualTo(targets[0].Cohesion));
            Assert.That(actualTargets[0].FormationConditionBasisPoints, Is.EqualTo(targets[0].FormationConditionBasisPoints));
        }

        [Test]
        public void CrossUnionPlanSkipsRetreatedAndDownedTargetsAndRetargetsEquivalentLivingMembers()
        {
            var state = Start(Storm, 4, 2);
            var oldPlan = AreaAction(state, Storm).AreaActionPlan095;
            var primary = oldPlan.Recipients[0];
            var enemies = state.Battle.EnemyUnions.Select(union => union.With(members:
                union.Members.Select(member => member.MemberId == primary.MemberId ? member.With(currentHp: 0) : member).ToArray())).ToArray();
            var retreatedId = enemies.Last().UnionId;
            enemies[enemies.Length - 1] = enemies.Last().With(retreated: true);
            state = state.WithBattle(state.Battle.With(enemyUnions: enemies));
            var resolved = ResolveArea(state, Storm);
            var hits = Hits(resolved, Storm, BattleSide.Player);
            Assert.That(hits.Length, Is.EqualTo(5));
            Assert.That(hits.Any(value => value.TargetMemberId == primary.MemberId || value.TargetUnionId == retreatedId), Is.False);
            Assert.That(hits.Sum(value => value.Amount), Is.EqualTo(oldPlan.TotalDamageBudget));
            Assert.That(hits.Select(value => value.TargetUnionId).Distinct().Count(), Is.EqualTo(3));
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value => value.EventType == "AREA_RETARGETED"), Is.True);
        }

        [TestCase("GEAR")] [TestCase("LEARNED")] [TestCase("MP")] [TestCase("AP")]
        public void IllegalOrdinaryAreaArtNeverEntersCurrentForecastPool(string missing)
        {
            // Worldstorm explicitly has no weapon-family requirement. Test gear
            // illegality on an actual weapon Art instead of inventing that gate.
            var artId = missing == "GEAR" ? Arc : Storm;
            if (missing == "GEAR") Assert.That(_content.Art(artId).RequiredEquipmentTags.Count, Is.GreaterThan(0));
            var state = Start(artId, 1, 5, correctGear: missing != "GEAR", learned: missing != "LEARNED", mp: missing == "MP" ? 0 : 100);
            if (missing == "AP")
                state = Reforecast(state.WithBattle(state.Battle.With(playerUnions:
                    state.Battle.PlayerUnions.Select(value => value.With(currentAp: 0)).ToArray())));
            Assert.That(state.Battle.CommittedForecasts.SelectMany(value => value.MemberActions)
                .Any(value => value.ArtId == artId), Is.False);
        }

        [Test]
        public void EnemyAreaUsesSameBoundedRecipientsPaysActualCostsAndGrowsOnce()
        {
            var state = Start(Arc, 1, 1, playerCount: 5, enemyArt: Storm);
            var enemy = state.Battle.EnemyUnions.Single();
            Assert.That(enemy.Members.Single().LearnedArtIds, Does.Contain(Storm));
            var resolved = ResolveCommand(state, "CMD_GUARD");
            var hits = Hits(resolved, Storm, BattleSide.Enemy);
            Assert.That(hits.Length, Is.EqualTo(5));
            var after = resolved.Battle.EnemyUnions.Single();
            Assert.That(after.CurrentAp, Is.EqualTo(Math.Min(enemy.MaximumAp,
                enemy.CurrentAp - _content.Art(Storm).SharedApCost + 3)),
                "Enemy area pays its real Art AP once, then receives normal +3 round recovery.");
            Assert.That(after.Members.Single().CurrentMp, Is.EqualTo(enemy.Members.Single().CurrentMp - _content.Art(Storm).PersonalMpCost));
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Count(value =>
                value.EventType == "ART_GROWTH" && value.Side == BattleSide.Enemy && value.ArtId == Storm), Is.EqualTo(1));
        }

        [TestCase("GEAR")] [TestCase("LEARNED")] [TestCase("MP")] [TestCase("AP")]
        public void EnemyCannotUseAreaWhenItsOwnRequiredAuthorityIsMissing(string missing)
        {
            var artId = missing == "GEAR" ? Arc : Storm;
            if (missing == "GEAR") Assert.That(_content.Art(artId).RequiredEquipmentTags.Count, Is.GreaterThan(0));
            var state = Start(Arc, 1, 1, playerCount: 5, enemyArt: artId);
            var enemy = state.Battle.EnemyUnions.Single();
            var member = enemy.Members.Single();
            if (missing == "GEAR")
            {
                var json = JObject.FromObject(member);
                json["EquipmentTags"] = new JArray("UNRELATED_TEST_TOOL");
                member = json.ToObject<BattleMemberState>();
            }
            if (missing == "LEARNED") member = member.With(learnedArtIds: new[] { "ART_QUICK_CUT" });
            if (missing == "MP") member = member.With(currentMp: 0);
            enemy = enemy.With(members: new[] { member }, currentAp: missing == "AP" ? 0 : enemy.CurrentAp);
            state = state.WithBattle(state.Battle.With(enemyUnions: new[] { enemy }));
            var resolved = ResolveCommand(state, "CMD_GUARD");
            Assert.That(Hits(resolved, artId, BattleSide.Enemy), Is.Empty);
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value => value.EventType == "ENEMY_AREA_FORECAST"), Is.False);
        }

        [Test]
        public void ExactSavedCommittedPlanReloadResolvesDeterministicallyAndNullLegacyActionsDoNotUpgrade()
        {
            var state = Start(Arc, 1, 5);
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(state));
            Assert.That(CanonicalJson.Sha256Hex(reloaded), Is.EqualTo(CanonicalJson.Sha256Hex(state)));
            var first = ResolveArea(state, Arc);
            var second = ResolveArea(reloaded, Arc);
            Assert.That(CanonicalJson.Sha256Hex(second), Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            var json = JObject.Parse(CanonicalJson.Serialize(state));
            foreach (var action in json.SelectTokens("Battle.CommittedForecasts[*].MemberActions[*]").OfType<JObject>())
                action.Remove("AreaActionPlan095");
            var legacy = json.ToObject<CampaignState>();
            var legacyActions = legacy.Battle.CommittedForecasts.SelectMany(value => value.MemberActions).ToArray();
            Assert.That(legacyActions.All(value => value.AreaActionPlan095 == null), Is.True);
            var serializedActions = JToken.Parse(CanonicalJson.Serialize(legacyActions));
            Assert.That(serializedActions.SelectTokens("$..AreaActionPlan095").Any(), Is.False,
                "Null optional plans must be absent from the actual serialized action schema.");
            Assert.That(CanonicalJson.Sha256Hex(legacyActions), Is.EqualTo(CanonicalJson.Sha256Hex(
                json.SelectTokens("Battle.CommittedForecasts[*].MemberActions[*]").ToArray())),
                "Deserializing old action objects must preserve their exact canonical action hash.");
            var selected = legacy.Battle.CommittedForecasts.First(value => value.MemberActions.Any(action => action.ArtId == Arc));
            legacy = Require(_commands.SelectForecast(legacy, selected.UnionId, selected.ForecastId));
            var oldResolved = Require(_commands.ConfirmRound(legacy, _content));
            Assert.That(Hits(oldResolved, Arc, BattleSide.Player).Length, Is.EqualTo(1),
                "An old committed action without area metadata is not rewritten on load.");
        }

        [Test]
        public void UnrelatedSingleTargetAndSssScopeAreNotPromotedByGenericUnionTags()
        {
            Assert.That(_content.Arts.Values.Where(value => value.AreaProfile095 != null).Count(), Is.EqualTo(9));
            Assert.That(_content.Art("TREE_CA002_WPN_SWORD_N01").AreaProfile095, Is.Null);
            Assert.That(_content.Art("TREE_CA002_MYS_FROST_N12").AreaProfile095, Is.Null);
            Assert.That(_content.Arts.Values.Where(value => value.Id.StartsWith("SSS_", StringComparison.Ordinal))
                .All(value => value.AreaProfile095 == null), Is.True);
        }

        [Test]
        public void TerminalTargetsCancelWithoutResourceSpendOrPostVictoryAttack()
        {
            var state = Start(Storm, 1, 2);
            var initial = state.Battle.PlayerUnions.Single();
            state = state.WithBattle(state.Battle.With(enemyUnions: state.Battle.EnemyUnions
                .Select(union => union.With(members: union.Members.Select(member => member.With(currentHp: 0)).ToArray())).ToArray()));
            var resolved = ResolveArea(state, Storm);
            Assert.That(Hits(resolved, Storm, BattleSide.Player), Is.Empty);
            Assert.That(resolved.Battle.PlayerUnions.Single().CurrentAp, Is.EqualTo(initial.CurrentAp));
            Assert.That(resolved.Battle.PlayerUnions.Single().Members.Single().CurrentMp, Is.EqualTo(initial.Members.Single().CurrentMp));
            Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value => value.EventType == "ENEMY_HIT"), Is.False);
        }

        BattlePlannedActionState AreaAction(CampaignState state, string art) =>
            AreaForecast(state, art).MemberActions.First(value => value.ArtId == art);
        BattleForecastState AreaForecast(CampaignState state, string art) =>
            state.Battle.CommittedForecasts.First(value => value.MemberActions.Any(action => action.ArtId == art));
        CampaignState ResolveArea(CampaignState state, string art)
        {
            var forecast = AreaForecast(state, art);
            return Require(_commands.ConfirmRound(Require(_commands.SelectForecast(state, forecast.UnionId, forecast.ForecastId)), _content));
        }
        CampaignState ResolveCommand(CampaignState state, string command)
        {
            foreach (var union in state.Battle.PlayerUnions)
            {
                var forecast = state.Battle.CommittedForecasts.First(value => value.UnionId == union.UnionId && value.CommandId == command);
                state = Require(_commands.SelectForecast(state, union.UnionId, forecast.ForecastId));
            }
            return Require(_commands.ConfirmRound(state, _content));
        }
        static BattleEventState[] Hits(CampaignState state, string art, BattleSide side) =>
            state.Battle.RoundRecords.Last().Events.Where(value => value.ArtId == art && value.Side == side &&
                (value.EventType == "MYSTIC_HIT" || value.EventType == "MARTIAL_HIT" || value.EventType == "TACTICAL_HIT" || value.EventType == "ENEMY_HIT")).ToArray();
        CampaignState Reforecast(CampaignState state) => state.WithBattle((BattleState)typeof(M2BattleCommandService)
            .GetMethod("CommitForecasts", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { state, state.Battle, _content }));

        CampaignState Start(string artId, int unionCount, int membersPerUnion, bool correctGear = true,
            bool learned = true, int mp = 100, int playerCount = 1, string enemyArt = "ART_QUICK_CUT", int masteryPoints = 0)
        {
            var campaign = Campaign(artId, correctGear, learned, mp, playerCount, masteryPoints);
            var enemies = new List<EncounterEnemyUnion070>();
            for (var unionIndex = 0; unionIndex < unionCount; unionIndex++)
            {
                var members = Enumerable.Range(0, membersPerUnion).Select(index =>
                {
                    var id = "AREA095_ENEMY_" + unionIndex + "_" + index;
                    var definition = new M2EnemyDefinition(id, "Area test enemy " + index, 5000, 100, 2, 60,
                        new[] { enemyArt }, 1, 1);
                    return new EncounterEnemyMember070(id, id, "AREA095_FAMILY", index, definition);
                }).ToArray();
                var source = new M2EnemyUnionDefinition("AREA095_UNION_" + unionIndex, "Enemy wing " + unionIndex,
                    members[0].SourceEnemyId, "FORMATION_SKIRMISH_LINE", 99, 100,
                    members.Select(value => value.SourceEnemyId).ToArray(), 1000);
                enemies.Add(new EncounterEnemyUnion070(source.Id, source.Id, source, members, new[] { "AREA095_FAMILY" }));
            }
            var roster = new EncounterRoster070("AREA095_ROSTER", "AREA095_DETERMINISTIC_FIXTURE", enemies, new[] { "AREA095_FAMILY" });
            return Require(_commands.StartEncounterBattleWithRoster070(campaign, _content, "AREA095_BATTLE",
                "Verify bounded ordinary Union Arts.", roster));
        }

        CampaignState Campaign(string artId, bool correctGear, bool learned, int mp, int playerCount, int masteryPoints)
        {
            var art = _content.Art(artId);
            var recruits = new List<RecruitState>();
            for (var index = 0; index < playerCount; index++)
            {
                var id = "AREA095_HERO_" + index;
                var tags = correctGear ? art.RequiredEquipmentTags.Concat(new[] { "WEAPON" }).Distinct().ToArray() : new[] { "UNRELATED_TEST_TOOL" };
                var item = new EquipmentItemState("AREA095_WEAPON_" + index, "AREA095_TEST_GEAR", "Area fixture weapon",
                    new[] { EquipmentSlotIds.MainHand }, tags, "QUALITY_STANDARD", 10000, false);
                var ids = learned ? new[] { artId } : Array.Empty<string>();
                var trees = string.IsNullOrEmpty(art.TreeId) ? Array.Empty<string>() : new[] { art.TreeId };
                var mastery = ids.Select(value => new RecruitArtMasteryState(value, art.Discipline, 0, masteryPoints)).ToArray();
                var progress = new RecruitProgressionState(40, RecruitProgressionRules021.TotalXpRequiredForLevel(40),
                    0, 0, 0, 0, 0, 0, 0, ids, mastery, trees);
                recruits.Add(new RecruitState(id, 4000, 4000, mp, 100, "Area hero " + index,
                    RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01", "CLASS_TEND_TEST",
                    "Observed", 5000, RecruitAuthorityKind.Normal, "{}", string.Empty,
                    new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, item) }),
                    true, string.Empty, string.Empty, 60, 60, progress));
            }
            var union = new UnionState("AREA095_PLAYER_UNION", "Area hero Union", UnionKind.Normal,
                recruits[0].RecruitId, recruits.Select(value => value.RecruitId).ToArray(), "FORMATION_SKIRMISH_LINE",
                "DOCTRINE_BALANCED", 99, 10000);
            var guild = new GuildState("AREA095_GUILD", 0, recruits, new[] { union });
            var profile = new NewGuildProfileState("Area tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var opening = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true,
                null, false, 439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState("00000000-0000-0000-0000-000000000395", 95001, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, opening);
        }

        static CampaignState Require(Result<CampaignState> value)
        {
            Assert.That(value.IsSuccess, Is.True, string.Join("\n", value.Errors));
            return value.Value;
        }
    }
}

