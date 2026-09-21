using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SssTenV4BattleIntegration090Tests
    {
        private M2CombatContent _content;
        private M2BattleCommandService _commands;

        [SetUp]
        public void SetUp()
        {
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath,
                    "Authority", "CONTENT"));
            _commands = new M2BattleCommandService();
        }

        [Test]
        public void AllTenGoldForecastsAreExactPaidLegalAndAdditive090()
        {
            var campaign = Campaign090(SssHeroes.All, woundSecondUnion: true);
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "SSS_ALL_TEN_BATTLE_090",
                "Verify every SSS gold Forecast through M2.",
                2));

            var gold = campaign.Battle.CommittedForecasts
                .Where(SssBattleIntegration090.IsGoldForecast090)
                .ToArray();
            Assert.That(gold.Length, Is.EqualTo(10));
            Assert.That(campaign.Battle.CommittedForecasts.Any(value =>
                value.CommandId == "CMD_BALANCED"), Is.True,
                "Gold choices must not replace ordinary Forecasts or Auto behavior.");

            foreach (var heroId in SssHeroes.All)
            {
                var commandId = "SSS_CMD_" + heroId.Substring(4);
                var forecast = gold.Single(value => value.CommandId == commandId);
                Assert.That(forecast.MemberActions[0].ArtId,
                    Is.EqualTo(heroId + "_ART_04"));
                Assert.That(forecast.SharedApCost,
                    Is.EqualTo(forecast.MemberActions.Sum(value =>
                        value.SharedApCost)));
                Assert.That(forecast.CombinedMpCost,
                    Is.EqualTo(forecast.MemberActions.Sum(value =>
                        value.PersonalMpCost)));
                if (StringComparer.Ordinal.Equals(
                        heroId, "SSS_VAELIS_MANYFORM"))
                {
                    Assert.That(forecast.MemberActions.Count, Is.EqualTo(1),
                        "Vaelis replaces the complete source Union for this round.");
                    Assert.That(forecast.SharedApCost,
                        Is.EqualTo(SssBattleIntegration090.GoldSharedApCost090));
                    Assert.That(forecast.CombinedMpCost,
                        Is.EqualTo(SssBattleIntegration090.GoldPersonalMpCost090));
                }
                else
                {
                    var source = campaign.Battle.PlayerUnions.Single(value =>
                        value.UnionId == forecast.UnionId);
                    var balanced = campaign.Battle.CommittedForecasts.Single(value =>
                        value.UnionId == forecast.UnionId &&
                        value.CommandId == "CMD_BALANCED");
                    var expectedCompanions = balanced.MemberActions
                        .Where(value => !StringComparer.Ordinal.Equals(
                            value.ActorMemberId, heroId))
                        .ToArray();
                    Assert.That(forecast.MemberActions.Skip(1)
                        .Select(value => value.ActorMemberId),
                        Is.EqualTo(expectedCompanions.Select(value =>
                            value.ActorMemberId)),
                        "Every living non-caster keeps the certified balanced action order.");
                    Assert.That(forecast.MemberActions.Skip(1)
                        .Select(value => value.ArtId),
                        Is.EqualTo(expectedCompanions.Select(value =>
                            value.ArtId)),
                        "Gold must compose with the existing M2 Forecast, not invent a second executor.");
                    Assert.That(forecast.MemberActions.Count,
                        Is.EqualTo(source.Members.Count(value => !value.Downed)),
                        "A non-Vaelis signature remains a complete Union command.");
                }
                Assert.That(forecast.DeterministicDebugEvidence,
                    Does.Contain("UsesCertifiedM2Commit"));
            }

            var neris = gold.Single(value =>
                value.CommandId == "SSS_CMD_NERIS_DAWNWELL");
            Assert.That(neris.TargetId, Is.Not.EqualTo(neris.UnionId),
                "Neris must preview a different allied Union.");
            var sixHeroUnion = campaign.Battle.PlayerUnions[0];
            Assert.That(gold.Count(value => value.UnionId == sixHeroUnion.UnionId),
                Is.EqualTo(6),
                "Every SSS member in one Union keeps an independent gold choice.");
            Assert.That(SssBattleIntegration090.BuildGoldForecasts090(
                    campaign,
                    campaign.Battle,
                    campaign.Battle.EnemyUnions[0],
                    0,
                    campaign.Battle.ForecastStateBasisHash),
                Is.Empty,
                "Enemy-side identity injection cannot expose player-only gold commands.");
        }

        [Test]
        public void NerisPaysOnceHealsOtherUnionAndNeverDuplicatesAction090()
        {
            var campaign = Campaign090(new[]
            {
                "SSS_NERIS_DAWNWELL",
                "SSS_ASTERION_SUNWARD"
            }, woundSecondUnion: true, oneHeroPerUnion: true);
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "SSS_NERIS_RESCUE_090",
                "Verify paid cross-Union rescue.",
                2));
            var nerisUnion = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member =>
                    member.MemberId == "SSS_NERIS_DAWNWELL"));
            var targetUnion = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId != nerisUnion.UnionId);
            var nerisBefore = nerisUnion.Members.Single(value =>
                value.MemberId == "SSS_NERIS_DAWNWELL");
            var apBefore = nerisUnion.CurrentAp;
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.CommandId == "SSS_CMD_NERIS_DAWNWELL");
            campaign = Select090(campaign, nerisUnion.UnionId,
                forecast.ForecastId);
            campaign = SelectCommand090(campaign, targetUnion.UnionId,
                "CMD_GUARD");

            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var nerisAfterUnion = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == nerisUnion.UnionId);
            var nerisAfter = nerisAfterUnion.Members.Single(value =>
                value.MemberId == nerisBefore.MemberId);
            Assert.That(nerisAfter.CurrentMp,
                Is.EqualTo(nerisBefore.CurrentMp -
                           SssBattleIntegration090.GoldPersonalMpCost090));
            Assert.That(nerisAfterUnion.CurrentAp,
                Is.EqualTo(apBefore -
                           SssBattleIntegration090.GoldSharedApCost090 + 3),
                "The normal end-of-round AP recovery follows the one paid gold cost.");
            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "RESTORATION" &&
                value.ActorMemberId == nerisBefore.MemberId &&
                value.TargetUnionId == targetUnion.UnionId &&
                value.Amount > 0), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.ActorMemberId == nerisBefore.MemberId &&
                (value.EventType == "MARTIAL_HIT" ||
                 value.EventType == "MYSTIC_HIT" ||
                 value.EventType == "TACTICAL_HIT")), Is.False,
                "The gold Forecast must not fall through to a duplicate normal action.");
            var receipt = SssBattleIntegration090.SpecialUseReceiptId090(
                campaign,
                campaign.Battle,
                "SSS_NERIS_DAWNWELL");
            Assert.That(campaign.SssV4090.SpecialUseReceiptIds,
                Does.Contain(receipt));
            Assert.That(campaign.Battle.CommittedForecasts.Any(value =>
                value.CommandId == "SSS_CMD_NERIS_DAWNWELL"), Is.False,
                "The persisted once-per-battle receipt closes the cooldown after reload/round advance.");
        }

        [Test]
        public void RylenGuestIsSixMembersActsNextRoundAndCannotEarnRewards090()
        {
            var campaign = Campaign090(new[] { "SSS_RYLEN_STONEBOND" });
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "SSS_RYLEN_GUEST_090",
                "Verify the complete guest Union lifecycle.",
                2));
            var source = campaign.Battle.PlayerUnions[0];
            var gold = campaign.Battle.CommittedForecasts.Single(value =>
                value.CommandId == "SSS_CMD_RYLEN_STONEBOND");
            campaign = Select090(campaign, source.UnionId, gold.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var guestRecord = campaign.Battle.SssBattleRuntime090.Guests
                .Single(value => value.Active);
            var guest = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == guestRecord.GuestUnionId);
            Assert.That(guest.Members.Count, Is.EqualTo(6));
            Assert.That(guest.Members.All(value =>
                SssBattleIntegration090.IsSyntheticMember090(value.MemberId)),
                Is.True);
            var guestForecasts = campaign.Battle.CommittedForecasts.Where(value =>
                value.UnionId == guest.UnionId).ToArray();
            Assert.That(guestForecasts.Length, Is.EqualTo(1));
            Assert.That(guestForecasts[0].CommandId,
                Is.EqualTo(SssBattleIntegration090.GuestCommandId090));

            var players = new List<BattleUnionState>(
                campaign.Battle.PlayerUnions);
            var events = new List<BattleEventState>();
            var runtime = campaign.Battle.SssBattleRuntime090;
            SssBattleIntegration090.RestoreTerminalActors090(
                campaign.Battle.Round,
                players,
                events,
                ref runtime);
            Assert.That(players.Any(value =>
                value.UnionId == guest.UnionId), Is.False);
            Assert.That(runtime.Guests.Single().Active, Is.False);
            var terminal = campaign.Battle.With(
                phase: BattlePhase.Resolved,
                outcome: BattleOutcome.Victory,
                playerUnions: players.AsReadOnly(),
                sssBattleRuntime090: runtime);
            var reward = M2ProgressionRewards.CreatePending(
                campaign,
                terminal,
                _content);
            Assert.That(reward.MemberRewards.Count,
                Is.EqualTo(source.Members.Count));
            Assert.That(reward.MemberRewards.Any(value =>
                SssBattleIntegration090.IsSyntheticMember090(value.MemberId)),
                Is.False,
                "Guest actors depart before the normal participant reward ledger.");
        }

        [Test]
        public void ElysiaPactConvergenceRequiresAndTargetsOnlyHerLivingGuest090()
        {
            const string heroId = "SSS_ELYSIA_NIGHTCALL";
            const string artId = heroId + "_ART_03";
            var campaign = Campaign090(new[] { heroId }, oneHeroPerUnion: true);
            campaign = ReplaceRecruit090(campaign, WithOnlyArt090(
                SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, heroId),
                artId, "Support"));
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "SSS_ELYSIA_OWNED_GUEST_SUPPORT_090",
                "Verify Pact Convergence cannot leave Elysia's owned guest Union.",
                2));

            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == heroId));
            var actor = source.Members.Single(value => value.MemberId == heroId);
            Assert.That(
                M2BattleCommandService.CandidateArtsForVerification090(
                    actor,
                    "CMD_SUPPORT",
                    _content,
                    source.CurrentAp,
                    campaign.Battle,
                    source.UnionId),
                Does.Not.Contain(artId),
                "Pact Convergence is not legal before Elysia owns a living guest Union.");
            Assert.That(campaign.Battle.CommittedForecasts.SelectMany(value =>
                    value.MemberActions).Any(value => value.ArtId == artId),
                Is.False);

            var gold = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId &&
                value.CommandId == "SSS_CMD_ELYSIA_NIGHTCALL");
            campaign = Select090(campaign, source.UnionId, gold.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var guestRecord = campaign.Battle.SssBattleRuntime090.Guests
                .Single(value => value.Active && value.OwnerHeroId == heroId);
            Assert.That(guestRecord.SourceUnionId, Is.EqualTo(source.UnionId));
            var guest = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == guestRecord.GuestUnionId);
            source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == heroId));
            actor = source.Members.Single(value => value.MemberId == heroId);
            Assert.That(
                M2BattleCommandService.CandidateArtsForVerification090(
                    actor,
                    "CMD_SUPPORT",
                    _content,
                    source.CurrentAp,
                    campaign.Battle,
                    source.UnionId),
                Does.Contain(artId));

            var support = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId &&
                value.MemberActions.Any(action => action.ArtId == artId));
            var pactAction = support.MemberActions.Single(value =>
                value.ArtId == artId);
            Assert.That(support.TargetId, Is.EqualTo(guest.UnionId));
            Assert.That(pactAction.TargetUnionId, Is.EqualTo(guest.UnionId));
            Assert.That(pactAction.Prediction,
                Does.Contain("Owned Pact guest only"));
            var actorBefore = actor;
            campaign = Select090(campaign, source.UnionId, support.ForecastId);
            var guestForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == guest.UnionId);
            campaign = Select090(
                campaign, guest.UnionId, guestForecast.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var supportEvents = campaign.Battle.EventLog.Where(value =>
                value.EventType == "ALLY_SUPPORT" &&
                value.ActorMemberId == heroId && value.ArtId == artId).ToArray();
            Assert.That(supportEvents.Length, Is.EqualTo(1));
            Assert.That(supportEvents[0].TargetUnionId,
                Is.EqualTo(guest.UnionId));
            Assert.That(supportEvents.Any(value =>
                value.TargetUnionId == source.UnionId), Is.False,
                "The guest-bound Art must never fall back to Elysia's source Union.");
            var actorAfter = campaign.Battle.PlayerUnions.SelectMany(value =>
                    value.Members)
                .Single(value => value.MemberId == heroId);
            Assert.That(actorAfter.CurrentMp,
                Is.EqualTo(Math.Min(
                    actorBefore.MaximumMp,
                    actorBefore.CurrentMp -
                    _content.Art(artId).PersonalMpCost + 2)));
        }

        [Test]
        public void VaelisTransformSerializesSuppressesOriginalsAndRestoresReducerExactly090()
        {
            var campaign = Campaign090(new[]
            {
                "SSS_VAELIS_MANYFORM",
                "SSS_ORINTH_WORLDSONG"
            });
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "SSS_VAELIS_FORM_090",
                "Verify frozen aggregate and exact terminal restoration.",
                2));
            var original = campaign.Battle.PlayerUnions[0];
            var gold = campaign.Battle.CommittedForecasts.Single(value =>
                value.CommandId == "SSS_CMD_VAELIS_MANYFORM");
            campaign = Select090(campaign, original.UnionId, gold.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var transformed = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == original.UnionId);
            Assert.That(transformed.Members.Count, Is.EqualTo(1));
            Assert.That(SssBattleIntegration090.IsSyntheticMember090(
                transformed.Members[0].MemberId), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value =>
                original.Members.Any(member =>
                    member.MemberId != "SSS_VAELIS_MANYFORM" &&
                    member.MemberId == value.ActorMemberId)), Is.False,
                "Original member actions are suppressed in the transformation round.");

            var json = JsonConvert.SerializeObject(campaign.Battle);
            var reopened = JsonConvert.DeserializeObject<BattleState>(json);
            Assert.That(reopened.SssBattleRuntime090, Is.Not.Null);
            var formUnion = reopened.PlayerUnions.Single(value =>
                value.UnionId == original.UnionId);
            var form = formUnion.Members[0];
            var nextHp = Math.Max(1, form.MaximumHp / 2);
            var nextMp = form.MaximumMp / 2;
            var changedForm = form.With(currentHp: nextHp, currentMp: nextMp);
            var changedUnion = formUnion.With(members: new[] { changedForm });
            var players = reopened.PlayerUnions.Select(value =>
                    value.UnionId == changedUnion.UnionId ? changedUnion : value)
                .ToList();
            var runtime = reopened.SssBattleRuntime090;
            var beforeRestore = runtime.Transformations.Single();
            var expectedSnapshot = CovenantTransformation.WithResources(
                beforeRestore.Transformation,
                nextHp.ToString(),
                nextMp.ToString());
            var expected = CovenantTransformation.EndBattle(expectedSnapshot);
            var events = new List<BattleEventState>();
            SssBattleIntegration090.RestoreTerminalActors090(
                reopened.Round,
                players,
                events,
                ref runtime);

            var restored = players.Single(value =>
                value.UnionId == original.UnionId);
            Assert.That(restored.Members.Select(value => value.MemberId),
                Is.EquivalentTo(original.Members.Select(value => value.MemberId)));
            foreach (var member in restored.Members)
            {
                var expectedId = member.MemberId ==
                                 beforeRestore.OriginalCasterMemberId
                    ? beforeRestore.Transformation.heroId
                    : member.MemberId;
                var expectedMember = expected.restoredMembers.Single(value =>
                    value.memberId == expectedId);
                Assert.That(member.CurrentHp,
                    Is.EqualTo(int.Parse(expectedMember.currentHp)));
                Assert.That(member.CurrentMp,
                    Is.EqualTo(int.Parse(expectedMember.currentMp)));
            }
            Assert.That(runtime.Transformations.Single().Transformation.restored,
                Is.True);
            Assert.That(events.Any(value =>
                value.EventType == "SSS_TRANSFORMATION_RESTORED"), Is.True);
        }

        [TestCase("SSS_RYLEN_STONEBOND")]
        [TestCase("SSS_ELYSIA_NIGHTCALL")]
        [TestCase("SSS_VAELIS_MANYFORM")]
        [TestCase("SSS_NERIS_DAWNWELL")]
        [TestCase("SSS_MYRIEN_STARFALL")]
        [TestCase("SSS_ASTERION_SUNWARD")]
        [TestCase("SSS_SOLENNE_AEGIS")]
        [TestCase("SSS_CAEDRAN_TEMPEST")]
        [TestCase("SSS_ISOLDE_ECLIPSERIFT")]
        [TestCase("SSS_ORINTH_WORLDSONG")]
        public void EverySignatureEffectRequiresItsExactEquippedWeapon090(
            string heroId)
        {
            var without = ResolveSignatureWeapon090(
                heroId, false, out var withoutForecast);
            var with = ResolveSignatureWeapon090(
                heroId, true, out var withForecast);
            var hero = SssTenV4Roster090.Get(heroId);

            Assert.That(
                SssBattleIntegration090.HasSignatureWeaponEffectHandler090(heroId),
                Is.True,
                heroId);
            Assert.That(
                SssBattleIntegration090.HasSignatureWeaponEffectHandler090(
                    "SSS_NOT_A_REAL_HERO"),
                Is.False);
            Assert.That(withoutForecast.ExpectedEffect,
                Does.Not.Contain("EQUIPPED SIGNATURE:"), heroId);
            Assert.That(withForecast.ExpectedEffect,
                Does.Contain("EQUIPPED SIGNATURE:"), heroId);
            Assert.That(withForecast.ExpectedEffect,
                Does.Contain(hero.SignatureEffectName), heroId);
            Assert.That(withForecast.DeterministicDebugEvidence,
                Does.Contain("\"SignatureWeaponEquipped\":true"), heroId);
            Assert.That(without.Battle.EventLog.Count(value =>
                    value.EventType == "SSS_SIGNATURE_WEAPON_EFFECT" &&
                    value.ActorMemberId == heroId),
                Is.Zero,
                "An owned or merely tagged inventory item must not activate the effect.");
            var applied = with.Battle.EventLog.Where(value =>
                    value.EventType == "SSS_SIGNATURE_WEAPON_EFFECT" &&
                    value.ActorMemberId == heroId)
                .ToArray();
            Assert.That(applied.Length, Is.EqualTo(1),
                "The equipped effect is budgeted once inside its paid gold command.");
            Assert.That(applied[0].ArtId, Is.EqualTo(heroId + "_ART_04"));
            Assert.That(applied[0].Amount,
                Is.InRange(1,
                    SssBattleIntegration090.SignatureEffectBudgetPoints090));
            Assert.That(applied[0].Text,
                Does.Contain(hero.WeaponName).And.Contain(hero.SignatureEffectName));
            Assert.That(with.Battle.EventLog.Count(value =>
                    value.EventType == "SSS_GOLD_COMMAND" &&
                    value.ActorMemberId == heroId),
                Is.EqualTo(1),
                "The weapon cannot grant a second action or receipt.");
        }

        [Test]
        public void NormalAreaDamageArtHitsEveryLivingEnemyUnionAndGrowsOnce090()
        {
            const string heroId = "SSS_MYRIEN_STARFALL";
            const string artId = heroId + "_ART_02";
            var campaign = Campaign090(new[] { heroId });
            campaign = ReplaceRecruit090(campaign, WithOnlyArt090(
                SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, heroId),
                artId, "Mystic"));
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "SSS_NORMAL_AREA_DAMAGE_090",
                "Verify one paid normal Art reaches every living enemy Union.", 3));
            var source = campaign.Battle.PlayerUnions.Single();
            var actorBefore = source.Members.Single(value => value.MemberId == heroId);
            var enemyIds = campaign.Battle.EnemyUnions
                .Where(value => !value.IsDefeated && !value.Retreated)
                .Select(value => value.UnionId).ToArray();
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId &&
                value.MemberActions.Any(action => action.ArtId == artId));
            Assert.That(forecast.MemberActions.Single().Prediction,
                Does.Contain("every living enemy Union"));
            campaign = Select090(campaign, source.UnionId, forecast.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var hits = campaign.Battle.EventLog.Where(value =>
                    value.ActorMemberId == heroId && value.ArtId == artId &&
                    value.EventType == "MYSTIC_HIT").ToArray();
            CollectionAssert.AreEquivalent(enemyIds,
                hits.Select(value => value.TargetUnionId).Distinct().ToArray());
            Assert.That(hits.Length, Is.EqualTo(enemyIds.Length));
            var actorAfter = campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == heroId);
            Assert.That(actorAfter.CurrentMp,
                Is.EqualTo(actorBefore.CurrentMp - _content.Art(artId).PersonalMpCost));
            Assert.That(campaign.Battle.EventLog.Count(value =>
                    value.EventType == "ART_GROWTH" && value.ActorMemberId == heroId &&
                    value.ArtId == artId), Is.EqualTo(1));
        }

        [Test]
        public void NormalAlliedHealArtRestoresEveryWoundedLivingUnionAndGrowsOnce090()
        {
            const string heroId = "SSS_NERIS_DAWNWELL";
            const string artId = heroId + "_ART_03";
            var woundedIds = new[] { "SSS_ASTERION_SUNWARD", "SSS_SOLENNE_AEGIS" };
            var campaign = Campaign090(new[] { heroId, woundedIds[0], woundedIds[1] },
                oneHeroPerUnion: true);
            campaign = ReplaceRecruit090(campaign, WithOnlyArt090(
                SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, heroId),
                artId, "Restoration"));
            foreach (var woundedId in woundedIds)
            {
                var recruit = SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, woundedId);
                campaign = ReplaceRecruit090(campaign, WithCurrentHp090(
                    recruit, Math.Max(1, recruit.MaximumHp / 4)));
            }
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "SSS_NORMAL_ALLIED_HEAL_090",
                "Verify one paid normal Art reaches every wounded allied Union.", 2));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == heroId));
            var actorBefore = source.Members.Single(value => value.MemberId == heroId);
            var expectedTargets = campaign.Battle.PlayerUnions.Where(value =>
                    value.Members.Any(member => woundedIds.Contains(member.MemberId)))
                .Select(value => value.UnionId).ToArray();
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId &&
                value.MemberActions.Any(action => action.ArtId == artId));
            Assert.That(forecast.MemberActions.Single().Prediction,
                Does.Contain("every living allied Union"));
            campaign = Select090(campaign, source.UnionId, forecast.ForecastId);
            foreach (var ally in campaign.Battle.PlayerUnions.Where(value =>
                         value.UnionId != source.UnionId))
                campaign = SelectCommand090(campaign, ally.UnionId, "CMD_GUARD");
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var restoration = campaign.Battle.EventLog.Where(value =>
                    value.EventType == "RESTORATION" && value.ActorMemberId == heroId &&
                    value.ArtId == artId).ToArray();
            CollectionAssert.AreEquivalent(expectedTargets,
                restoration.Select(value => value.TargetUnionId).Distinct().ToArray());
            Assert.That(restoration.All(value => value.Amount > 0), Is.True);
            var actorAfter = campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == heroId);
            Assert.That(actorAfter.CurrentMp,
                Is.EqualTo(actorBefore.CurrentMp - _content.Art(artId).PersonalMpCost));
            Assert.That(campaign.Battle.EventLog.Count(value =>
                    value.EventType == "ART_GROWTH" && value.ActorMemberId == heroId &&
                    value.ArtId == artId), Is.EqualTo(1));
        }

        [Test]
        public void NormalAlliedBuffArtSupportsEveryLivingUnionWithOneCost090()
        {
            const string heroId = "SSS_ASTERION_SUNWARD";
            const string artId = heroId + "_ART_03";
            var campaign = Campaign090(new[]
            {
                heroId, "SSS_NERIS_DAWNWELL", "SSS_SOLENNE_AEGIS"
            }, oneHeroPerUnion: true);
            campaign = ReplaceRecruit090(campaign, WithOnlyArt090(
                SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, heroId),
                artId, "Support"));
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "SSS_NORMAL_ALLIED_BUFF_090",
                "Verify one paid normal Art supports every living allied Union.", 2));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == heroId));
            var actorBefore = source.Members.Single(value => value.MemberId == heroId);
            var expectedTargets = campaign.Battle.PlayerUnions
                .Where(value => !value.IsDefeated && !value.Retreated)
                .Select(value => value.UnionId).ToArray();
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId &&
                value.MemberActions.Any(action => action.ArtId == artId));
            campaign = Select090(campaign, source.UnionId, forecast.ForecastId);
            foreach (var ally in campaign.Battle.PlayerUnions.Where(value =>
                         value.UnionId != source.UnionId))
                campaign = SelectCommand090(campaign, ally.UnionId, "CMD_GUARD");
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var support = campaign.Battle.EventLog.Where(value =>
                    value.EventType == "ALLY_SUPPORT" && value.ActorMemberId == heroId &&
                    value.ArtId == artId).ToArray();
            CollectionAssert.AreEquivalent(expectedTargets,
                support.Select(value => value.TargetUnionId).Distinct().ToArray());
            Assert.That(support.Length, Is.EqualTo(expectedTargets.Length));
            var actorAfter = campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == heroId);
            Assert.That(actorAfter.CurrentMp,
                Is.EqualTo(Math.Min(
                    actorBefore.MaximumMp,
                    actorBefore.CurrentMp -
                    _content.Art(artId).PersonalMpCost + 2)),
                "Support pays its Art MP once, then preserves the existing 2 MP personal recovery.");
            Assert.That(campaign.Battle.EventLog.Count(value =>
                    value.EventType == "ART_GROWTH" && value.ActorMemberId == heroId &&
                    value.ArtId == artId), Is.EqualTo(1));
        }

        private CampaignState Select090(
            CampaignState campaign,
            string unionId,
            string forecastId) =>
            Require(_commands.SelectForecast(campaign, unionId, forecastId));

        private CampaignState SelectCommand090(
            CampaignState campaign,
            string unionId,
            string commandId)
        {
            var forecast = campaign.Battle.CommittedForecasts.First(value =>
                value.UnionId == unionId && value.CommandId == commandId);
            return Select090(campaign, unionId, forecast.ForecastId);
        }

        private CampaignState ResolveSignatureWeapon090(
            string heroId,
            bool equipped,
            out BattleForecastState selectedForecast)
        {
            var companion = StringComparer.Ordinal.Equals(
                heroId, "SSS_NERIS_DAWNWELL")
                ? "SSS_ASTERION_SUNWARD"
                : "SSS_NERIS_DAWNWELL";
            var campaign = Campaign090(
                new[] { heroId, companion },
                oneHeroPerUnion: true);
            if (StringComparer.Ordinal.Equals(
                    heroId, "SSS_NERIS_DAWNWELL"))
            {
                var companionRecruit = SssTenV4Roster090.FindOwned(
                    campaign.Guild.Recruits, companion);
                campaign = ReplaceRecruit090(
                    campaign,
                    WithCurrentHp090(
                        companionRecruit,
                        companionRecruit.MaximumHp - 1));
            }
            if (equipped) campaign = EquipSignatureWeapon090(campaign, heroId);

            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "SSS_SIGNATURE_EFFECT_" + heroId,
                "Verify exact equipped signature effect binding.",
                2));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == heroId));
            selectedForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId &&
                value.CommandId == "SSS_CMD_" + heroId.Substring(4));
            campaign = Select090(
                campaign, source.UnionId, selectedForecast.ForecastId);
            foreach (var union in campaign.Battle.PlayerUnions.Where(value =>
                         value.UnionId != source.UnionId && !value.IsDefeated &&
                         !value.Retreated))
                campaign = SelectCommand090(
                    campaign, union.UnionId, "CMD_GUARD");
            return Require(_commands.ConfirmRound(campaign, _content));
        }

        private static CampaignState EquipSignatureWeapon090(
            CampaignState campaign,
            string heroId)
        {
            var hero = SssTenV4Roster090.Get(heroId);
            var item = SssTenV4Inventory090.CreateSignatureWeapon(
                campaign, hero.HeroId);
            var recruit = SssTenV4Roster090.FindOwned(
                campaign.Guild.Recruits, hero.HeroId);
            var equipped = recruit.WithEquipment(new EquipmentLoadoutState(
                new[]
                {
                    new EquipmentSlotAssignmentState(
                        item.ValidSlotIds[0], item)
                }));
            return ReplaceRecruit090(campaign, equipped);
        }

        private static CampaignState ReplaceRecruit090(
            CampaignState campaign,
            RecruitState replacement)
        {
            var recruits = campaign.Guild.Recruits.Select(value =>
                    StringComparer.Ordinal.Equals(
                        value.RecruitId, replacement.RecruitId)
                        ? replacement
                        : value)
                .ToArray();
            return campaign.With(
                campaign.Guild.With(
                    campaign.Guild.TreasuryXp,
                    recruits,
                    campaign.Guild.Unions,
                    campaign.Guild.Inventory,
                    campaign.Guild.Development),
                campaign.OpeningFlow);
        }

        private static CampaignState Campaign090(
            IReadOnlyList<string> heroIds,
            bool woundSecondUnion = false,
            bool oneHeroPerUnion = false)
        {
            var recruits = heroIds
                .Select(value => SssTenV4Roster090.MaterializeGrant(value).Recruit)
                .ToList();
            var unionSize = oneHeroPerUnion ? 1 : 6;
            if (woundSecondUnion && recruits.Count > unionSize)
                recruits[unionSize] = WithCurrentHp090(
                    recruits[unionSize],
                    Math.Max(1, recruits[unionSize].MaximumHp / 4));
            var unions = new List<UnionState>();
            for (var offset = 0; offset < recruits.Count; offset += unionSize)
            {
                var ids = recruits.Skip(offset).Take(unionSize)
                    .Select(value => value.RecruitId).ToArray();
                unions.Add(new UnionState(
                    "SSS_TEST_UNION_" + (unions.Count + 1).ToString("D2"),
                    "SSS Test Union " + (unions.Count + 1),
                    UnionKind.Normal,
                    ids[0],
                    ids,
                    "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED",
                    18,
                    8500));
            }
            var guild = new GuildState(
                "GUILD_SSS_BATTLE_090",
                0,
                recruits.AsReadOnly(),
                unions.AsReadOnly());
            var profile = new NewGuildProfileState(
                "SSS Battle Tester",
                GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                false,
                "sss_battle_ready");
            var progression = new SssSave();
            for (var index = 0; index < 3; index++)
                progression.familyProgress.counters.Add(new CounterRow
                {
                    heroId = SssHeroes.All[index],
                    baseFamilyId = "ENEMY_REC_001",
                    totalDefeats = "10"
                });
            var prepared = new[]
            {
                new SssPreparedFamilyLoadout090(
                    SssHeroes.All[0],
                    Enumerable.Repeat("ENEMY_REC_001", 6)),
                new SssPreparedFamilyLoadout090(
                    SssHeroes.All[1],
                    new[] { "ENEMY_REC_001" }),
                new SssPreparedFamilyLoadout090(
                    SssHeroes.All[2],
                    new[] { "ENEMY_REC_001" })
            };
            return new CampaignState(
                    "00000000-0000-0000-0000-000000090090",
                    90090090L,
                    "SSS_BATTLE_TEST_090",
                    ModeRuleSnapshot.StandardDefaults(),
                    guild,
                    profile,
                    flow)
                .WithSssV4090(new SssTenV4State090(
                    progression,
                    preparedFamilies: prepared));
        }

        private static RecruitState WithCurrentHp090(
            RecruitState recruit,
            int currentHp) =>
            new RecruitState(
                recruit.RecruitId,
                currentHp,
                recruit.MaximumHp,
                recruit.CurrentMp,
                recruit.MaximumMp,
                recruit.DisplayName,
                recruit.OriginKind,
                recruit.SignatureId,
                recruit.RaceId,
                recruit.WorldId,
                recruit.ClassTendencyId,
                recruit.LeadershipBand,
                recruit.PotentialBasisPoints,
                recruit.AuthorityKind,
                recruit.CanonicalApplicantJson,
                recruit.CanonicalScoutingReportJson,
                recruit.Equipment,
                recruit.VitalsInitialized,
                recruit.TutorialAliasId,
                recruit.AuthoredStableRecruitId,
                recruit.LeadershipScore,
                recruit.TacticalAptitude,
                recruit.Progression);

        private static RecruitState WithOnlyArt090(
            RecruitState recruit,
            string artId,
            string discipline) =>
            new RecruitState(
                recruit.RecruitId,
                recruit.CurrentHp,
                recruit.MaximumHp,
                recruit.CurrentMp,
                recruit.MaximumMp,
                recruit.DisplayName,
                recruit.OriginKind,
                recruit.SignatureId,
                recruit.RaceId,
                recruit.WorldId,
                recruit.ClassTendencyId,
                recruit.LeadershipBand,
                recruit.PotentialBasisPoints,
                recruit.AuthorityKind,
                recruit.CanonicalApplicantJson,
                recruit.CanonicalScoutingReportJson,
                recruit.Equipment,
                recruit.VitalsInitialized,
                recruit.TutorialAliasId,
                recruit.AuthoredStableRecruitId,
                recruit.LeadershipScore,
                recruit.TacticalAptitude,
                recruit.Progression.WithArts(
                    new[] { artId },
                    new[]
                    {
                        new RecruitArtMasteryState(
                            artId, discipline, 0, 0)
                    }));

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                result.IsSuccess ? string.Empty : string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
