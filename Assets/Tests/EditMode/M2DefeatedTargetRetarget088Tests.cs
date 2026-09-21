using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2DefeatedTargetRetarget088Tests
    {
        private const string GenericArtId = "ART_SPARK_RUNE";
        private const string SpecificArtId = "ART_MARKED_TARGET";

        private M2CombatContent _content;
        private M2BattleCommandService _commands;

        [SetUp]
        public void SetUp()
        {
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _commands = new M2BattleCommandService();
        }

        [Test]
        public void SameUnionDeadMemberRetargetsLowestStableLivingMemberAndReloadIsDeterministic088()
        {
            var campaign = StartBattle(1, "BATTLE_SAME_UNION_RETARGET_088");
            var battle = campaign.Battle;
            var targetUnion = WithLivingMembers(battle.EnemyUnions[0], 3, 1, EngagementState.Engaged);
            var originalTarget = targetUnion.Members[0].MemberId;
            var replacementTarget = targetUnion.Members
                .Where(value => !value.Downed && value.MemberId != originalTarget)
                .OrderBy(value => value.MemberId, StringComparer.Ordinal)
                .First().MemberId;
            campaign = Commit(campaign, new[] { targetUnion }, "CMD_BALANCED",
                Attack(battle.PlayerUnions[0].Members[0], targetUnion, originalTarget, GenericArtId, -999),
                Attack(battle.PlayerUnions[0].Members[1], targetUnion, originalTarget, GenericArtId, -5));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));
            var resolved = Require(_commands.ConfirmRound(campaign, _content));
            var resolvedAfterReload = Require(_commands.ConfirmRound(reloaded, _content));
            var events = LastRoundEvents(resolved);

            Assert.That(M2BattleCommandService.AuthoritativeStateHash(resolved.Battle),
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(resolvedAfterReload.Battle)),
                "Save/reload must not change the stable replacement target or result.");
            Assert.That(events.Count(IsHit), Is.EqualTo(2));
            Assert.That(events.Where(IsHit).Select(value => value.TargetMemberId),
                Is.EqualTo(new[] { originalTarget, replacementTarget }));
            Assert.That(events.Single(value => value.EventType == "TARGET_MEMBER_RETARGETED").TargetMemberId,
                Is.EqualTo(replacementTarget));
            AssertResources(resolved, campaign.Battle.PlayerUnions[0], 6, 0, 1);
        }

        [Test]
        public void DefeatedUnionRetargetsEngagedNextUnionAndRecalculatesRearPressure088()
        {
            var campaign = StartBattle(3, "BATTLE_NEXT_UNION_RETARGET_088");
            var battle = campaign.Battle;
            var enemyA = WithLivingMembers(battle.EnemyUnions[0], 1, 1, EngagementState.Engaged);
            var enemyB = WithLivingMembers(battle.EnemyUnions[1], 3, null, EngagementState.Engaged);
            var enemyC = WithLivingMembers(battle.EnemyUnions[2], 3, null, EngagementState.Open);
            var targetA = enemyA.Members[0].MemberId;
            campaign = Commit(campaign, new[] { enemyA, enemyB, enemyC }, "CMD_FLANK",
                Attack(battle.PlayerUnions[0].Members[0], enemyA, targetA, GenericArtId, -999,
                    BattleActionKind.Tactical),
                Attack(battle.PlayerUnions[0].Members[1], enemyA, targetA, GenericArtId, -5,
                    BattleActionKind.Tactical));

            var resolved = Require(_commands.ConfirmRound(campaign, _content));
            var events = LastRoundEvents(resolved);
            var defeat = events.Single(value => value.EventType == "ENEMY_UNION_DEFEATED");
            var retarget = events.Single(value => value.EventType == "TARGET_UNION_RETARGETED");
            var hitsAfterDefeat = events.Where(value => IsHit(value) && value.Sequence > defeat.Sequence).ToArray();

            Assert.That(resolved.Battle.EnemyUnions[0].IsDefeated, Is.True);
            Assert.That(resolved.Battle.EnemyUnions[0].Engagement, Is.EqualTo(EngagementState.Broken));
            Assert.That(retarget.TargetUnionId, Is.EqualTo(enemyB.UnionId),
                "Engaged adjacency must outrank the open third Union.");
            Assert.That(hitsAfterDefeat, Has.Length.EqualTo(1));
            Assert.That(hitsAfterDefeat[0].TargetUnionId, Is.EqualTo(enemyB.UnionId));
            Assert.That(events.Any(value => value.EventType == "POSITION_SHIFT" &&
                                            value.TargetUnionId == enemyB.UnionId), Is.True);
            Assert.That(resolved.Battle.EnemyUnions[1].Engagement,
                Is.EqualTo(EngagementState.RearPressure),
                "The transferred flank must recalculate the new target's engagement result.");
            Assert.That(events.Any(value => IsHit(value) && value.Sequence > defeat.Sequence &&
                                            value.TargetUnionId == enemyA.UnionId), Is.False);
            AssertResources(resolved, campaign.Battle.PlayerUnions[0], 6, 0, 1);
        }

        [Test]
        public void TargetSpecificArtCancelsBeforeMpEffectMasteryOrPhantomHit088()
        {
            var campaign = StartBattle(2, "BATTLE_SPECIFIC_CANCEL_088");
            var battle = campaign.Battle;
            var enemyA = WithLivingMembers(battle.EnemyUnions[0], 1, 1, EngagementState.Engaged);
            var enemyB = WithLivingMembers(battle.EnemyUnions[1], 3, null, EngagementState.Engaged);
            var targetA = enemyA.Members[0].MemberId;
            var source = battle.PlayerUnions[0];
            var specificActorBefore = source.Members[2];
            campaign = Commit(campaign, new[] { enemyA, enemyB }, "CMD_BALANCED",
                Attack(source.Members[0], enemyA, targetA, GenericArtId, -999),
                Guard(source.Members[1]),
                Attack(source.Members[2], enemyA, targetA, SpecificArtId, -20,
                    BattleActionKind.Tactical));

            var resolved = Require(_commands.ConfirmRound(campaign, _content));
            var events = LastRoundEvents(resolved);
            var specificActorAfter = FindMember(resolved.Battle.PlayerUnions[0], specificActorBefore.MemberId);

            Assert.That(events.Count(value => value.EventType == "ACTION_CANCELED_DEAD_TARGET" &&
                                               value.ArtId == SpecificArtId), Is.EqualTo(1));
            Assert.That(events.Any(value => IsHit(value) && value.ArtId == SpecificArtId), Is.False);
            Assert.That(events.Any(value => value.EventType == "ART_GROWTH" &&
                                            value.ArtId == SpecificArtId), Is.False);
            Assert.That(specificActorAfter.CurrentMp, Is.EqualTo(specificActorBefore.CurrentMp),
                "Cancellation occurs before personal MP is charged.");
            Assert.That(FindMember(resolved.Battle.PlayerUnions[0], source.Members[0].MemberId).CurrentMp,
                Is.EqualTo(source.Members[0].CurrentMp - _content.Art(GenericArtId).PersonalMpCost));
            Assert.That(resolved.Battle.PlayerUnions[0].CurrentAp,
                Is.EqualTo(source.CurrentAp - _content.Art(GenericArtId).SharedApCost + 3),
                "The executed attack stays paid; only the canceled target-locked Art's AP is returned.");
            Assert.That(resolved.Battle.EnemyUnions[1].Members.Select(value => value.CurrentHp),
                Is.EqualTo(enemyB.Members.Select(value => value.CurrentHp)),
                "A target-specific cancellation cannot leak damage into another Union.");
        }

        [Test]
        public void FinalEnemyDefeatStopsAllQueuedActionsAndResolvesVictoryImmediately088()
        {
            var campaign = StartBattle(1, "BATTLE_IMMEDIATE_VICTORY_088");
            var battle = campaign.Battle;
            var enemy = WithLivingMembers(battle.EnemyUnions[0], 1, 1, EngagementState.Engaged);
            var target = enemy.Members[0].MemberId;
            var source = battle.PlayerUnions[0];
            campaign = Commit(campaign, new[] { enemy }, "CMD_BALANCED",
                Attack(source.Members[0], enemy, target, GenericArtId, -999),
                Attack(source.Members[1], enemy, target, GenericArtId, -5),
                Attack(source.Members[2], enemy, target, SpecificArtId, -5,
                    BattleActionKind.Tactical));

            var resolved = Require(_commands.ConfirmRound(campaign, _content));
            var events = LastRoundEvents(resolved);

            Assert.That(resolved.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(resolved.Battle.Phase, Is.EqualTo(BattlePhase.Resolved));
            Assert.That(events.Count(IsHit), Is.EqualTo(1));
            Assert.That(events.Count(value => value.EventType == "VICTORY_SEQUENCE_STOP"), Is.EqualTo(1));
            Assert.That(events.Count(value => value.EventType == "BATTLE_RESULT"), Is.EqualTo(1));
            Assert.That(events.Any(value => value.EventType == "ENEMY_HIT"), Is.False);
            Assert.That(events.Any(value => value.EventType == "TARGET_MEMBER_RETARGETED" ||
                                            value.EventType == "TARGET_UNION_RETARGETED" ||
                                            value.EventType == "ACTION_CANCELED_DEAD_TARGET"), Is.False);
            Assert.That(FindMember(resolved.Battle.PlayerUnions[0], source.Members[1].MemberId).CurrentMp,
                Is.EqualTo(source.Members[1].CurrentMp));
            Assert.That(FindMember(resolved.Battle.PlayerUnions[0], source.Members[2].MemberId).CurrentMp,
                Is.EqualTo(source.Members[2].CurrentMp));
            Assert.That(resolved.Battle.PlayerUnions[0].CurrentAp,
                Is.EqualTo(source.CurrentAp - _content.Art(GenericArtId).SharedApCost + 3),
                "Only the killing Art executes; queued unused base-Art AP and MP stay available.");
        }

        [Test]
        public void LivingButUnavailableOpponentDoesNotProduceFalseVictory090()
        {
            var campaign = StartBattle(1, "BATTLE_NO_LEGAL_TARGET_090");
            var source = campaign.Battle.PlayerUnions[0];
            var enemy = WithLivingMembers(campaign.Battle.EnemyUnions[0], 1, null,
                EngagementState.Open).With(retreated: true);
            campaign = Commit(campaign, new[] { enemy }, "CMD_BALANCED",
                Attack(source.Members[0], enemy, enemy.Members[0].MemberId, GenericArtId, -999));
            var resolved = Require(_commands.ConfirmRound(campaign, _content));
            var events = LastRoundEvents(resolved);
            Assert.That(resolved.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(events.Any(IsHit), Is.False);
            Assert.That(events.Any(value => value.EventType == "VICTORY_SEQUENCE_STOP"), Is.False);
            Assert.That(events.Any(value => value.EventType == "BATTLE_RESULT"), Is.False);
            Assert.That(resolved.Battle.PlayerUnions[0].CurrentAp,
                Is.EqualTo(Math.Min(source.MaximumAp, source.CurrentAp + 3)));
            Assert.That(resolved.Battle.PlayerUnions[0].Members[0].CurrentMp,
                Is.EqualTo(source.Members[0].CurrentMp));
        }

        [Test]
        public void FinalPlayerDefeatStopsPendingEnemyAttacksAndRescueWithoutRefundingKillingAttack090()
        {
            var campaign = StartBattle(3, "BATTLE_ENEMY_TERMINAL_STOP_090");
            var source = WithLivingMembers(campaign.Battle.PlayerUnions[0], 1, 1,
                EngagementState.Engaged);
            campaign = campaign.WithBattle(campaign.Battle.With(playerUnions: new[] { source }));
            var enemies = campaign.Battle.EnemyUnions.ToArray();
            enemies[0] = WithLivingMembers(enemies[0], 3, null, EngagementState.Engaged)
                .With(currentAp: 10);
            enemies[1] = WithLivingMembers(enemies[1], 1, null, EngagementState.Engaged)
                .With(currentAp: 10);
            var medicBefore = enemies[1].Members[0];
            var medic = new BattleMemberState(
                medicBefore.MemberId, medicBefore.DisplayName, "CLASS_TEND_HEALER",
                medicBefore.MaximumHp, medicBefore.MaximumHp, 30, 30,
                medicBefore.Attack, medicBefore.MagicAttack, new[] { "STAFF" },
                false, false, false, new[] { "ART_STAND_AGAIN" }, 0, 0, string.Empty);
            var medicMembers = enemies[1].Members.ToArray();
            medicMembers[0] = medic;
            enemies[1] = enemies[1].With(members: medicMembers);
            enemies[2] = WithLivingMembers(enemies[2], 0, null, EngagementState.Broken);
            campaign = Commit(campaign, enemies, "CMD_GUARD", Guard(source.Members[0]));

            var resolved = Require(_commands.ConfirmRound(campaign, _content));
            var events = LastRoundEvents(resolved);
            var finalDown = events.Single(value => value.EventType == "DOWNED" &&
                value.Side == BattleSide.Player);
            Assert.That(resolved.Battle.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(events.Count(value => value.EventType == "ENEMY_HIT" ||
                value.EventType == "INTERCEPTION"), Is.EqualTo(1));
            Assert.That(events.Any(value => value.Sequence > finalDown.Sequence &&
                (value.EventType == "ENEMY_HIT" || value.EventType == "INTERCEPTION" ||
                 value.EventType == "ENEMY_SUPPORT_FORECAST" || value.EventType == "REVIVED")), Is.False);
            Assert.That(events.Count(value => value.EventType == "BATTLE_RESULT"), Is.EqualTo(1));
            Assert.That(resolved.Battle.EnemyUnions[0].CurrentAp,
                Is.EqualTo(enemies[0].CurrentAp),
                "The lethal enemy attack pays its existing 3 AP before 3 AP round recovery.");
            Assert.That(resolved.Battle.EnemyUnions[1].CurrentAp,
                Is.EqualTo(Math.Min(enemies[1].MaximumAp, enemies[1].CurrentAp + 3)));
            Assert.That(resolved.Battle.EnemyUnions[1].Members[0].CurrentMp,
                Is.EqualTo(medic.CurrentMp), "The queued medic must not pay MP after battle ends.");
            Assert.That(resolved.Battle.EnemyUnions[2].IsDefeated, Is.True,
                "The queued enemy revival must not resolve after the last opponent falls.");
        }

        private CampaignState StartBattle(int enemyUnionCount, string battleId)
        {
            var campaign = Require(_commands.StartEncounterBattle(
                CreateCampaign(), _content, battleId, "Defeated-target authority fixture.", enemyUnionCount));
            var source = campaign.Battle.PlayerUnions[0];
            var members = source.Members.ToList();
            members[0] = Learn(members[0], GenericArtId);
            members[1] = Learn(members[1], GenericArtId);
            members[2] = Learn(members[2], SpecificArtId);
            source = source.With(members: members.AsReadOnly(), currentAp: 10);
            return campaign.WithBattle(campaign.Battle.With(playerUnions: new[] { source }));
        }

        private CampaignState Commit(CampaignState campaign,
            IReadOnlyList<BattleUnionState> enemies, string commandId,
            params BattlePlannedActionState[] actions)
        {
            var source = campaign.Battle.PlayerUnions[0];
            var forecast = new BattleForecastState(
                "FORECAST_DEAD_TARGET_088", source.UnionId,
                commandId, "Fixture Forecast", "Resolve the committed sequence.",
                "OFFENSE", enemies[0].UnionId, enemies[0].DisplayName,
                actions, actions.Sum(value => value.SharedApCost), 0,
                actions.Sum(value => value.PersonalMpCost),
                "Authoritative defeated-target fixture.", "None.", "None.",
                "Revalidate each pending member action.", "FIXTURE_088", "{}");
            var battle = campaign.Battle.With(
                enemyUnions: enemies,
                committedForecasts: new[] { forecast },
                selections: new[]
                {
                    new BattleForecastSelectionState(source.UnionId, forecast.ForecastId)
                });
            return campaign.WithBattle(battle);
        }

        private BattlePlannedActionState Attack(BattleMemberState actor,
            BattleUnionState targetUnion, string targetMemberId, string artId,
            int predictedHpDelta, BattleActionKind? kind = null)
        {
            var art = _content.Art(artId);
            return new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, targetUnion.UnionId, targetMemberId,
                art.Id, art.Name, kind ?? (art.Discipline == "Mystic"
                    ? BattleActionKind.Mystic : BattleActionKind.Martial),
                art.SharedApCost, art.PersonalMpCost,
                predictedHpDelta, -2, -300,
                "Fixture attack.", true, false, art.AnimationTag, art.Discipline, 1);
        }

        private BattlePlannedActionState Guard(BattleMemberState actor)
        {
            var art = _content.Art("ART_GUARD");
            return new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, string.Empty, string.Empty,
                art.Id, art.Name, BattleActionKind.Guard,
                art.SharedApCost, art.PersonalMpCost, 0, 2, 0,
                "Fixture guard.", true, false, art.AnimationTag, art.Discipline, 1);
        }

        private static BattleUnionState WithLivingMembers(BattleUnionState source,
            int livingCount, int? firstHp, EngagementState engagement)
        {
            var members = source.Members.Select((value, index) => value.With(
                currentHp: index < livingCount
                    ? index == 0 && firstHp.HasValue ? firstHp.Value : value.MaximumHp
                    : 0,
                guarding: false)).ToList();
            return source.With(members: members.AsReadOnly(), engagement: engagement, guarding: false);
        }

        private static BattleMemberState Learn(BattleMemberState source, string artId)
        {
            var learned = source.LearnedArtIds.Concat(new[] { artId })
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return source.With(learnedArtIds: learned);
        }

        private static IReadOnlyList<BattleEventState> LastRoundEvents(CampaignState campaign) =>
            campaign.Battle.RoundRecords[campaign.Battle.RoundRecords.Count - 1].Events;

        private static bool IsHit(BattleEventState value) =>
            value.EventType == "MARTIAL_HIT" || value.EventType == "MYSTIC_HIT" ||
            value.EventType == "TACTICAL_HIT";

        private static BattleMemberState FindMember(BattleUnionState union, string memberId) =>
            union.Members[union.FindMemberIndex(memberId)];

        private void AssertResources(CampaignState resolved, BattleUnionState before,
            int forecastApCost, params int[] executedMemberIndices)
        {
            Assert.That(resolved.Battle.PlayerUnions[0].CurrentAp,
                Is.EqualTo(before.CurrentAp - forecastApCost + 3));
            for (var index = 0; index < before.Members.Count; index++)
            {
                var after = FindMember(resolved.Battle.PlayerUnions[0], before.Members[index].MemberId);
                var expected = before.Members[index].CurrentMp;
                if (executedMemberIndices.Contains(index))
                    expected -= _content.Art(GenericArtId).PersonalMpCost;
                Assert.That(after.CurrentMp, Is.EqualTo(expected),
                    "Member " + index + " must pay personal MP exactly once iff its action executes.");
            }
        }

        private static CampaignState CreateCampaign()
        {
            var recruits = new List<RecruitState>();
            for (var index = 0; index < 3; index++)
            {
                var ranger = index == 2;
                var main = new EquipmentItemState(
                    "ITEM_DEAD_TARGET_WEAPON_" + index,
                    "EQ_DEAD_TARGET_WEAPON_" + index,
                    "Authority Fixture Weapon " + index,
                    new[] { EquipmentSlotIds.MainHand },
                    ranger ? new[] { "BOW", "WEAPON" } : new[] { "WAND", "FOCUS_TOOL" },
                    "QUALITY_STANDARD", 10000, false);
                var body = new EquipmentItemState(
                    "ITEM_DEAD_TARGET_BODY_" + index,
                    "EQ_DEAD_TARGET_BODY_" + index,
                    "Authority Fixture Armor " + index,
                    new[] { EquipmentSlotIds.BodyArmor }, new[] { "ARMOR" },
                    "QUALITY_STANDARD", 10000, false);
                recruits.Add(new RecruitState(
                    "RETARGET_RECRUIT_" + index, 180, 180, 30, 30,
                    "Retarget Recruit " + index, RecruitOriginKind.Procedural,
                    string.Empty, "HUMAN", "WORLD_GATE_01",
                    ranger ? "CLASS_TEND_RANGER" : "CLASS_TEND_MAGE",
                    "Observed", 7000, RecruitAuthorityKind.Normal,
                    string.Empty, string.Empty,
                    new EquipmentLoadoutState(new[]
                    {
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, main),
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.BodyArmor, body)
                    }), true, string.Empty, string.Empty, 70, 70));
            }
            var ids = recruits.Select(value => value.RecruitId).ToArray();
            var union = new UnionState(
                "UNION_RETARGET_088", "Retarget Authority Union", UnionKind.Normal,
                ids[0], ids, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 18, 8500);
            var guild = new GuildState("GUILD_RETARGET_088", 0, recruits, new[] { union });
            var profile = new NewGuildProfileState(
                "Retarget Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000288", 20260906L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
