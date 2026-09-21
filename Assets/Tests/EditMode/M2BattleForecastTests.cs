using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    public sealed class M2BattleForecastTests
    {
        private M2CombatContent _content;
        private M2BattleCommandService _commands;

        [SetUp]
        public void SetUp()
        {
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _commands = new M2BattleCommandService();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BattleTownRewardRatesAreFrozenAndReplayIgnoresLaterTownGrowth159(bool upgraded)
        {
            var source = CreateCampaign(3);
            if (upgraded) source = WithTownLevels159(source, 10, 4, 6);
            var started = Require(_commands.StartTutorialBattle(source, _content));
            var battle = started.Battle;
            Assert.That(battle.TownBonuses159?.TreasuryBasisPoints ?? 0, Is.EqualTo(upgraded ? 850 : 0));
            Assert.That(battle.TownBonuses159?.PersonalBasisPoints ?? 0, Is.EqualTo(upgraded ? 300 : 0));
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(started));
            Assert.That(CanonicalJson.Serialize(reloaded.Battle), Is.EqualTo(CanonicalJson.Serialize(battle)));
            var laterTown = WithTownLevels159(reloaded, 100, 100, 100);
            var replay = _commands.ReplayTutorialBattle(laterTown, _content);
            Assert.That(replay.IsSuccess, Is.True, string.Join(" ", replay.Errors));
            Assert.That(CanonicalJson.Serialize(replay.Value.Battle), Is.EqualTo(CanonicalJson.Serialize(battle)));
            var terminal = battle.With(phase: BattlePhase.Resolved, outcome: BattleOutcome.Victory);
            var reviewed = M2ProgressionRewards.CreatePending(started, terminal, _content);
            var reproduced = M2ProgressionRewards.CreatePending(laterTown, terminal, _content);
            Assert.That(CanonicalJson.Serialize(reproduced), Is.EqualTo(CanonicalJson.Serialize(reviewed)),
                "Later facilities must not change a saved battle's reward or receipt identity.");
            Assert.That(terminal.WithReward(reviewed).TownBonuses159, Is.SameAs(battle.TownBonuses159));
        }

        [Test]
        public void TownRewardSnapshotChangesIntegrityButCannotRerollBattleForecasts159()
        {
            var source = CreateCampaign(3);
            var baseline = Require(_commands.StartTutorialBattle(source, _content)).Battle;
            var invested = Require(_commands.StartTutorialBattle(WithTownLevels159(source, 10, 4, 6), _content)).Battle;
            Assert.That(CanonicalJson.Serialize(invested.CommittedForecasts),
                Is.EqualTo(CanonicalJson.Serialize(baseline.CommittedForecasts)));
            Assert.That(M2BattleCommandService.GameplayRngStateHash090(invested),
                Is.EqualTo(M2BattleCommandService.GameplayRngStateHash090(baseline)));
            Assert.That(M2BattleCommandService.AuthoritativeStateHash(invested),
                Is.Not.EqualTo(M2BattleCommandService.AuthoritativeStateHash(baseline)));
            Assert.That(CanonicalJson.Serialize(baseline), Does.Not.Contain("TownBonuses159"));
        }

        private static CampaignState WithTownLevels159(CampaignState source, int hall, int expedition, int academy)
        {
            var guild = source.Guild;
            var development = guild.Development.SetFacilityLevel("CITYTAB_GUILD_HALL", hall, 0)
                .SetFacilityLevel("CITYTAB_EXPEDITION", expedition, 0)
                .SetFacilityLevel("CITYTAB_ACADEMY", academy, 0);
            return source.With(guild.With(guild.TreasuryXp, guild.Recruits, guild.Unions, guild.Inventory,
                development), source.OpeningFlow);
        }

        [Test]
        public void RuntimeAuthorityProvidesCombatVarietyAndNoDirectlySelectableStandardArts()
        {
            Assert.That(_content.Commands.Keys, Does.Contain("CMD_BALANCED"));
            Assert.That(_content.Commands.Keys, Does.Contain("CMD_GUARD"));
            Assert.That(_content.Commands.Keys, Does.Contain("CMD_HEAL"));
            Assert.That(_content.Commands.Keys, Does.Contain("CMD_MYSTIC"));
            Assert.That(_content.Commands.Keys, Does.Contain("CMD_AP_RECOVERY"));
            Assert.That(_content.GuaranteedBreakthroughThreshold, Is.EqualTo(100));
            Assert.That(_content.Arts.Values.All(value => !value.PlayerDirectlySelectableInStandard), Is.True);

            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3), _content));
            Assert.That(M2BattleCommandService.UsesCurrentTutorialRules(campaign.Battle, _content), Is.True);
            Assert.That(campaign.Battle.ContentVersion, Does.EndWith(M2BattleCommandService.TutorialRulesVersion));
        }

        [Test]
        public void EnemyArt700IdentityIsCommittedByRealCampaignAndTowerUnionBattlesAndSurvivesRoundAndJson090()
        {
            var campaign = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_ENEMY_ART_CAMPAIGN_090",
                "Verify stable Campaign enemy presentation identities.",
                1));
            var committed090 = campaign.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .ToDictionary(
                    value => value.MemberId,
                    value => new[]
                    {
                        value.EnemyArtBaseId090,
                        value.EnemyArtVariantId090,
                        value.VisualVariantSeed090.ToString()
                    },
                    StringComparer.Ordinal);
            Assert.That(committed090.Count, Is.EqualTo(3));
            Assert.That(committed090.Values.All(value =>
                StringComparer.Ordinal.Equals(value[0], "ENEMY_REC_050")), Is.True);
            Assert.That(committed090.Values.Select(value => value[1]).Distinct().Count(),
                Is.EqualTo(3), "The three authored Gnawer ranks must keep distinct stable appearances.");

            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_GUARD");
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            foreach (var member090 in campaign.Battle.EnemyUnions.SelectMany(value => value.Members))
            {
                Assert.That(committed090.ContainsKey(member090.MemberId), Is.True);
                Assert.That(member090.EnemyArtBaseId090,
                    Is.EqualTo(committed090[member090.MemberId][0]));
                Assert.That(member090.EnemyArtVariantId090,
                    Is.EqualTo(committed090[member090.MemberId][1]));
                Assert.That(member090.VisualVariantSeed090.ToString(),
                    Is.EqualTo(committed090[member090.MemberId][2]));
            }

            var reopened090 = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));
            Assert.That(reopened090, Is.Not.Null);
            foreach (var member090 in reopened090.Battle.EnemyUnions.SelectMany(value => value.Members))
            {
                Assert.That(member090.EnemyArtBaseId090,
                    Is.EqualTo(committed090[member090.MemberId][0]));
                Assert.That(member090.EnemyArtVariantId090,
                    Is.EqualTo(committed090[member090.MemberId][1]));
            }

            var tower090 = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "ABYSS_BATTLE022_FLOOR_05_ENEMY_ART_090",
                "Verify stable Tower enemy presentation identities.",
                2));
            Assert.That(tower090.Battle.EnemyUnions.Count, Is.EqualTo(2));
            for (var unionIndex090 = 0;
                 unionIndex090 < tower090.Battle.EnemyUnions.Count;
                 unionIndex090++)
            {
                var union090 = tower090.Battle.EnemyUnions[unionIndex090];
                for (var memberIndex090 = 0;
                     memberIndex090 < union090.Members.Count;
                     memberIndex090++)
                {
                    var expectedBaseIndex090 = 29 + unionIndex090 * 3 + memberIndex090;
                    var member090 = union090.Members[memberIndex090];
                    Assert.That(member090.EnemyArtBaseId090,
                        Is.EqualTo(EnemyArtIdentity090.BaseId090(expectedBaseIndex090)));
                    Assert.That(member090.EnemyArtVariantId090,
                        Is.EqualTo(EnemyArtIdentity090.VariantId090(expectedBaseIndex090, 5)));
                    Assert.That(member090.VisualVariantSeed090, Is.EqualTo(5));
                }
            }
        }

        [Test]
        public void EnemyArtIdentityChangesCannotAlterForecastEnemyRngOrBattleOutcome090()
        {
            var baseline090 = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_ENEMY_ART_RNG_ISOLATION_090",
                "Prove presentation identity cannot influence combat.",
                1));
            var alteredBattle090 =
                WithDifferentEnemyPresentationIdentity090(baseline090.Battle);
            alteredBattle090 = alteredBattle090.With(
                initialIntegrityStateHash090:
                    InitialIntegrityStateHashFor090(alteredBattle090));
            var altered090 = baseline090.WithBattle(alteredBattle090);

            Assert.That(
                M2BattleCommandService.AuthoritativeStateHash(altered090.Battle),
                Is.Not.EqualTo(M2BattleCommandService.AuthoritativeStateHash(baseline090.Battle)),
                "The full integrity hash must continue to commit persisted presentation identity.");
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(altered090.Battle),
                Is.EqualTo(M2BattleCommandService.GameplayRngStateHash090(baseline090.Battle)),
                "Presentation-only fields must not enter any gameplay RNG seed.");
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(baseline090.Battle),
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(baseline090.Battle)),
                "An untampered battle must retain the certified pre-090 RNG basis byte-for-byte.");
            Assert.That(altered090.Battle.InitialBattleStateHash,
                Is.EqualTo(baseline090.Battle.InitialBattleStateHash));
            Assert.That(altered090.Battle.InitialIntegrityStateHash090,
                Is.Not.EqualTo(baseline090.Battle.InitialIntegrityStateHash090));
            Assert.That(
                CanonicalJson.Serialize(altered090.Battle.CommittedForecasts),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.CommittedForecasts)));

            baseline090 = SelectCommandForEveryActiveUnion(baseline090, "CMD_GUARD");
            altered090 = SelectCommandForEveryActiveUnion(altered090, "CMD_GUARD");
            baseline090 = Require(_commands.ConfirmRound(baseline090, _content));
            altered090 = Require(_commands.ConfirmRound(altered090, _content));

            Assert.That(altered090.Battle.Outcome, Is.EqualTo(baseline090.Battle.Outcome));
            Assert.That(CanonicalJson.Serialize(altered090.Battle.EventLog),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.EventLog)),
                "Enemy target selection and damage events must be identical.");
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(altered090.Battle),
                Is.EqualTo(M2BattleCommandService.GameplayRngStateHash090(baseline090.Battle)),
                "Art-derived integrity hashes in round records must not leak into the next RNG seed.");
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(baseline090.Battle),
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(baseline090.Battle)),
                "Canonical round records must preserve the established next-round RNG basis.");
            Assert.That(altered090.Battle.ForecastStateBasisHash,
                Is.EqualTo(baseline090.Battle.ForecastStateBasisHash));
            Assert.That(CanonicalJson.Serialize(altered090.Battle.CommittedForecasts),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.CommittedForecasts)),
                "The next round's generated Forecasts must remain byte-identical.");
            Assert.That(
                M2BattleCommandService.AuthoritativeStateHash(altered090.Battle),
                Is.Not.EqualTo(M2BattleCommandService.AuthoritativeStateHash(baseline090.Battle)),
                "Presentation identity must remain covered by integrity hashing after resolution.");

            for (var round090 = 0;
                 round090 < 20 &&
                 baseline090.Battle.Outcome == BattleOutcome.InProgress &&
                 altered090.Battle.Outcome == BattleOutcome.InProgress;
                 round090++)
            {
                baseline090 = SelectCommandForEveryActiveUnion(
                    baseline090, "CMD_ALL_OUT", "CMD_BALANCED");
                altered090 = SelectCommandForEveryActiveUnion(
                    altered090, "CMD_ALL_OUT", "CMD_BALANCED");
                baseline090 = Require(_commands.ConfirmRound(baseline090, _content));
                altered090 = Require(_commands.ConfirmRound(altered090, _content));
            }

            Assert.That(baseline090.Battle.Outcome,
                Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(altered090.Battle.Outcome,
                Is.EqualTo(baseline090.Battle.Outcome));
            Assert.That(CanonicalJson.Serialize(altered090.Battle.EventLog),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.EventLog)));
            Assert.That(CanonicalJson.Serialize(altered090.Battle.Reward),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.Reward)),
                "XP, reward, and equipment identifiers must be art-invariant.");
            Assert.That(altered090.Battle.FinalStateHash,
                Is.EqualTo(baseline090.Battle.FinalStateHash),
                "Every downstream receipt must receive the same gameplay result hash.");
            Assert.That(baseline090.Battle.FinalStateHash,
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(baseline090.Battle)),
                "The normal terminal battle must preserve its established authority/result hash.");
            Assert.That(altered090.Battle.FinalIntegrityStateHash090,
                Is.Not.EqualTo(baseline090.Battle.FinalIntegrityStateHash090),
                "The separate audit hash must still commit the different artwork.");
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(
                baseline090.Battle), Is.True);
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(
                altered090.Battle), Is.True);
            var visuallyTampered090 = WithDifferentEnemyPresentationIdentity090(
                baseline090.Battle);
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(
                visuallyTampered090), Is.False,
                "Changing persisted artwork without its audit hash must still fail integrity validation.");

            var lootResolver090 = LootResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var baselineLoot090 = lootResolver090.ResolveReceipt(
                baseline090,
                "CONTRACT_ENEMY_ART_RNG_090",
                "BOARD_ENEMY_ART_RNG_090",
                "ENCOUNTER_ENEMY_ART_RNG_090",
                baseline090.Battle.Reward.RewardId,
                baseline090.Battle.FinalStateHash,
                baseline090.Battle.Outcome,
                1);
            var alteredLoot090 = lootResolver090.ResolveReceipt(
                altered090,
                "CONTRACT_ENEMY_ART_RNG_090",
                "BOARD_ENEMY_ART_RNG_090",
                "ENCOUNTER_ENEMY_ART_RNG_090",
                altered090.Battle.Reward.RewardId,
                altered090.Battle.FinalStateHash,
                altered090.Battle.Outcome,
                1);
            Assert.That(CanonicalJson.Serialize(alteredLoot090),
                Is.EqualTo(CanonicalJson.Serialize(baselineLoot090)),
                "Loot selection, receipt ID, and item instance ID must be art-invariant.");
        }

        [Test]
        public void WhollyBlankLegacyEnemyArtKeepsHistoricalHashAndReviveContinuation090()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_3", null, null,
                "ART_STAND_AGAIN");
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_REVIVED_ORDER_CENSUS_086",
                "Restore the defeated allied Union.", 1));
            campaign = campaign.WithBattle(
                WithoutEnemyPresentationIdentity090(campaign.Battle));

            Assert.That(campaign.Battle.EnemyUnions
                .SelectMany(value => value.Members).All(value =>
                    string.IsNullOrWhiteSpace(value.EnemyArtBaseId090) &&
                    string.IsNullOrWhiteSpace(value.EnemyArtVariantId090) &&
                    value.VisualVariantSeed090 <= 0), Is.True);
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(campaign.Battle),
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(
                    campaign.Battle)),
                "A wholly no-art legacy battle must retain its historical full-state RNG recipe.");

            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var target = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId != source.UnionId);
            target = target.With(members: target.Members.Select(value =>
                value.With(currentHp: 0, stabilized: false)).ToArray());
            var actor = source.Members.Single(value =>
                value.MemberId == "RECRUIT_3");
            var art = _content.Art("ART_STAND_AGAIN");
            var action = new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, target.UnionId,
                target.Members[0].MemberId, art.Id, art.Name,
                BattleActionKind.Restoration, art.SharedApCost,
                art.PersonalMpCost, 30, 0, 0,
                "Revive the defeated allied Union.", true, false,
                art.AnimationTag, art.Discipline);
            var forged = new BattleForecastState(
                "FORECAST_LEGACY_REVIVED_ORDER_090", source.UnionId,
                "CMD_HEAL", "Save Them!", "Bring them back.", "Rescue",
                target.UnionId, target.DisplayName, new[] { action },
                art.SharedApCost, 0, art.PersonalMpCost,
                "Revive one ally.", "Committed", "Revival is meaningful.",
                "Fall back to the next legal ally.",
                "LEGACY_REVIVED_ORDER_090", "AUTHORITY_TEST_090");
            var forecasts = campaign.Battle.CommittedForecasts.ToList();
            forecasts.Add(forged);
            var players = new[] { source, target };
            campaign = campaign.WithBattle(campaign.Battle.With(
                playerUnions: players,
                committedForecasts: forecasts.AsReadOnly(),
                selections: Array.Empty<BattleForecastSelectionState>()));
            foreach (var union in players.Where(value =>
                         !value.Retreated && !value.IsDefeated))
            {
                var selected = StringComparer.Ordinal.Equals(
                    union.UnionId, forged.UnionId)
                    ? forged
                    : forecasts.First(value =>
                        value.UnionId == union.UnionId &&
                        value.CommandId == "CMD_GUARD");
                campaign = Require(_commands.SelectForecast(
                    campaign, union.UnionId, selected.ForecastId));
            }

            var historicalPreRoundHash090 =
                M2BattleCommandService.AuthoritativeStateHash(campaign.Battle);
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(campaign.Battle),
                Is.EqualTo(historicalPreRoundHash090));
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var record090 = campaign.Battle.RoundRecords.Last();
            Assert.That(record090.PreRoundStateHash,
                Is.EqualTo(historicalPreRoundHash090),
                "The resumed round must consume the exact historical no-art seed.");
            var revived090 = record090.Events.FirstOrDefault(value =>
                value.EventType == "REVIVED" &&
                value.TargetUnionId == target.UnionId);
            Assert.That(revived090, Is.Not.Null);
            var targetAfterRound090 = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == target.UnionId);
            Assert.That(targetAfterRound090.Retreated, Is.False);
            var targetIsActive090 = !targetAfterRound090.IsDefeated;
            var hasNextRoundForecast090 = campaign.Battle.CommittedForecasts.Any(value =>
                value.UnionId == target.UnionId);
            Assert.That(hasNextRoundForecast090, Is.EqualTo(targetIsActive090),
                "A revived Union receives a next-round Forecast iff it survives the deterministic enemy counterattack.");
            if (!targetIsActive090)
                Assert.That(record090.Events.Any(value =>
                    value.EventType == "DOWNED" &&
                    value.TargetUnionId == target.UnionId &&
                    value.Sequence > revived090.Sequence), Is.True,
                    "An inactive revived Union must have been legally re-downed after its REVIVED event; presentation identity must not fabricate a Forecast.");
            Assert.That(
                M2BattleCommandService.GameplayRngStateHash090(campaign.Battle),
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(
                    campaign.Battle)),
                "New round records from a wholly legacy battle must remain on the historical recipe.");
        }

        [Test]
        public void EncounterVisualVariantSeedCannotAlterEnemyStatsForecastsOrDamage090()
        {
            var resolver090 = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var roster090 = resolver090.Resolve(
                20260906L,
                "CONTRACT_ENEMY_ART_RNG_090",
                "BOARD_ENEMY_ART_RNG_090",
                "ENCOUNTER_ENEMY_ART_RNG_090",
                1,
                "ENEMY_ART_RNG_ISOLATION_090");
            var recoloredRoster090 = WithDifferentVisualSeeds090(roster090);
            var baseline090 = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_ROSTER_VISUAL_SEED_ISOLATION_090",
                "Prove roster color variants cannot grant combat power.",
                roster090));
            var recolored090 = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_ROSTER_VISUAL_SEED_ISOLATION_090",
                "Prove roster color variants cannot grant combat power.",
                recoloredRoster090));

            foreach (var sourceMember090 in roster090.Unions
                         .SelectMany(value => value.Members))
            {
                var battleMember090 = baseline090.Battle.EnemyUnions
                    .SelectMany(value => value.Members)
                    .Single(value => value.MemberId == sourceMember090.MemberId);
                Assert.That(battleMember090.Attack, Is.EqualTo(Math.Max(
                    14,
                    13 + sourceMember090.Definition.ApContribution +
                    Math.Abs(sourceMember090.VisualVariantSeed % 5))),
                    "Decoupling visuals must preserve the certified rank-based attack tuning.");
                Assert.That(battleMember090.MagicAttack, Is.EqualTo(Math.Max(
                    8,
                    8 + sourceMember090.Definition.MaximumMp / 24 +
                    Math.Abs(sourceMember090.VisualVariantSeed % 3))),
                    "Decoupling visuals must preserve the certified rank-based magic tuning.");
            }

            var baselineStats090 = baseline090.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .Select(value => new
                {
                    value.MemberId,
                    value.CurrentHp,
                    value.MaximumHp,
                    value.CurrentMp,
                    value.MaximumMp,
                    value.Attack,
                    value.MagicAttack
                }).ToArray();
            var recoloredStats090 = recolored090.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .Select(value => new
                {
                    value.MemberId,
                    value.CurrentHp,
                    value.MaximumHp,
                    value.CurrentMp,
                    value.MaximumMp,
                    value.Attack,
                    value.MagicAttack
                }).ToArray();
            Assert.That(CanonicalJson.Serialize(recoloredStats090),
                Is.EqualTo(CanonicalJson.Serialize(baselineStats090)));
            Assert.That(recolored090.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .Select(value => value.VisualVariantSeed090).ToArray(),
                Is.EqualTo(baseline090.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .Select(value => value.VisualVariantSeed090).ToArray()),
                "Mutable roster color seeds must normalize to stable authored enemy ranks.");
            Assert.That(CanonicalJson.Serialize(recolored090.Battle.CommittedForecasts),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.CommittedForecasts)));
            Assert.That(M2BattleCommandService.GameplayRngStateHash090(recolored090.Battle),
                Is.EqualTo(M2BattleCommandService.GameplayRngStateHash090(baseline090.Battle)));
            Assert.That(recolored090.Battle.InitialBattleStateHash,
                Is.EqualTo(baseline090.Battle.InitialBattleStateHash));
            Assert.That(recolored090.Battle.InitialIntegrityStateHash090,
                Is.EqualTo(baseline090.Battle.InitialIntegrityStateHash090));

            baseline090 = SelectCommandForEveryActiveUnion(baseline090, "CMD_GUARD");
            recolored090 = SelectCommandForEveryActiveUnion(recolored090, "CMD_GUARD");
            baseline090 = Require(_commands.ConfirmRound(baseline090, _content));
            recolored090 = Require(_commands.ConfirmRound(recolored090, _content));

            Assert.That(CanonicalJson.Serialize(recolored090.Battle.EventLog),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.EventLog)));
            Assert.That(recolored090.Battle.Outcome, Is.EqualTo(baseline090.Battle.Outcome));
            Assert.That(CanonicalJson.Serialize(recolored090.Battle.CommittedForecasts),
                Is.EqualTo(CanonicalJson.Serialize(baseline090.Battle.CommittedForecasts)));
        }

        [Test]
        public void ActiveUnionHeadingKeepsAuthoredVanguardName079()
        {
            Assert.That(
                M2BattleCommandHud072.ActiveUnionHeadingForVerification079(
                    "Zorin's Vanguard",
                    4,
                    4),
                Is.EqualTo("04/04 ZORIN'S VANGUARD"));
            Assert.That(
                M2BattleCommandHud072.ActiveUnionHeadingForVerification079(
                    "Opening Union 1",
                    1,
                    10),
                Is.EqualTo("01/10 OPENING"));
        }

        [Test]
        public void OpeningFormationAuthorityPreservesThreeThroughFiveAndAddsSixMembers()
        {
            Assert.That(_content.ContentVersion, Does.Contain("PASS02_FORMATIONS_0.7"),
                "This additive capacity change intentionally preserves active save version compatibility.");
            var openingFormationIds = OpeningUnionCatalog.Formations
                .Select(value => value.Id)
                .ToArray();
            Assert.That(openingFormationIds.Length, Is.EqualTo(8));

            foreach (var formationId in openingFormationIds)
            {
                var formation = _content.Formation(formationId);
                Assert.That(formation.Supports(2), Is.False, formationId);
                Assert.That(formation.Supports(3), Is.True, formationId);
                Assert.That(formation.Supports(4), Is.True, formationId);
                Assert.That(formation.Supports(5), Is.True, formationId);
                Assert.That(formation.Supports(6), Is.True, formationId);
            }

            var hollowSquare = _content.Formation("FORMATION_HOLLOW_SQUARE");
            Assert.That(hollowSquare.Supports(4), Is.True);
            Assert.That(hollowSquare.Supports(5), Is.True);
            Assert.That(hollowSquare.Supports(6), Is.False,
                "Campaign capacity must not silently broaden a non-opening formation's authority.");
            Assert.That(_content.TutorialEnemyUnion.MemberIds.Count, Is.EqualTo(3));
            Assert.That(_content.Formation(_content.TutorialEnemyUnion.FormationId).Supports(3), Is.True,
                "Existing three-member enemy formation behavior must remain legal and unchanged.");
        }

        [Test]
        public void TutorialDeploysOnlyFirstTwoNonemptyPlansAndPreservesFlexibleSavedPlans()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(1, 2, 2, 1), _content));

            Assert.That(campaign.Guild.Unions.Count, Is.EqualTo(4));
            Assert.That(campaign.Battle.PlayerUnions.Count, Is.EqualTo(2));
            Assert.That(campaign.Battle.PlayerUnions[0].Members.Count, Is.EqualTo(1));
            Assert.That(campaign.Battle.PlayerUnions[1].Members.Count, Is.EqualTo(2));
            Assert.That(campaign.Battle.PlayerUnions.All(value => !value.FormationBenefitActive), Is.True);
            Assert.That(campaign.Battle.PlayerUnions.All(value => value.FormationInactiveReason.Contains("at least 3")), Is.True);
            Assert.That(campaign.Guild.Unions.Select(value => value.FormationId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(campaign.Guild.Unions.All(value => value.LeaderRecruitId == value.MemberRecruitIds[0]), Is.True);
        }

        [Test]
        public void CompletedLegacyOpeningStageCanEnterBattleWhenOldCompletionFlagIsMissing()
        {
            var campaign = CreateCampaign(3, 3);
            var legacyFlow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, false, false, "autosave_unions");
            campaign = campaign.With(campaign.Guild, legacyFlow);

            var started = Require(_commands.StartTutorialBattle(campaign, _content));

            Assert.That(started.Battle, Is.Not.Null);
            Assert.That(started.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(started.Battle.PlayerUnions.Count, Is.EqualTo(2));
            Assert.That(started.Battle.PlayerUnions.SelectMany(value => value.Members)
                .All(value => value.ClassId.StartsWith("CLASS_", StringComparison.Ordinal) &&
                              !value.ClassId.StartsWith("CLASS_TEND_", StringComparison.Ordinal)), Is.True);
            var candidate = started.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == started.Battle.TutorialBreakthroughMemberId);
            var targetArt = _content.Art(started.Battle.TutorialBreakthroughArtId);
            Assert.That(targetArt.RequiredEquipmentTags.Count == 0 ||
                        targetArt.RequiredEquipmentTags.Any(required => candidate.EquipmentTags.Contains(required)), Is.True,
                "The guaranteed breakthrough must be legal for the recruit's real starter equipment.");
        }

        [Test]
        public void EveryActiveUnionReceivesThreeToSixCompleteReadableForecasts()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            foreach (var union in campaign.Battle.PlayerUnions)
            {
                var cards = campaign.Battle.CommittedForecasts.Where(value => value.UnionId == union.UnionId).ToArray();
                Assert.That(cards.Length, Is.InRange(3, 6), union.UnionId);
                foreach (var card in cards)
                {
                    Assert.That(card.CommandName, Is.Not.Empty);
                    Assert.That(card.TacticalIntent, Is.Not.Empty);
                    Assert.That(card.TargetName, Is.Not.Empty);
                    Assert.That(card.MemberActions.Count, Is.EqualTo(union.Members.Count));
                    Assert.That(card.ExpectedEffect, Is.Not.Empty);
                    Assert.That(card.Risk, Is.Not.Empty);
                    Assert.That(card.LearningOpportunity, Is.Not.Empty);
                    Assert.That(card.FallbackBehavior, Is.Not.Empty);
                    Assert.That(card.DeterministicDebugEvidence, Does.Contain("LegalActionPool"));
                    Assert.That(card.DeterministicDebugEvidence, Does.Contain("AdaptiveFallbacks"));
                }
            }
            Assert.That(campaign.Battle.CommittedForecasts.Any(value => value.CommandId == "CMD_GUARD"), Is.True);
            Assert.That(campaign.Battle.CommittedForecasts.Any(value => value.CommandId == "CMD_AP_RECOVERY"), Is.False,
                "AP recovery must not be offered while every Union is already at full AP.");
            Assert.That(campaign.Battle.CommittedForecasts.Any(value => value.CommandId == "CMD_HEAL"), Is.True);
            Assert.That(campaign.Battle.CommittedForecasts.Any(value => value.CommandId == "CMD_MYSTIC"), Is.True);
            var healing = campaign.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_HEAL");
            Assert.That(healing.TargetId, Is.Not.EqualTo(healing.UnionId),
                "The tutorial wound is intentionally placed in the other Union to prove cross-Union Restoration targeting.");
            Assert.That(healing.MemberActions.Any(value => value.Kind == BattleActionKind.Restoration &&
                value.TargetUnionId == healing.TargetId), Is.True);
        }

        [Test]
        public void FriendlyUnionTargetAuthorityDistinguishesHealingRevivalBuffsAndSelfRecovery080()
        {
            var remedy = _content.Art("ART_MINOR_REMEDY");
            var revival = _content.Art("ART_STAND_AGAIN");
            var rally = _content.Art("ART_BRASS_RALLY");
            var selfRecovery = _content.Art("ART_RECOVER_BREATH");
            var deepSupport = _content.Art("TREE_CA002_ROLE_SABOTEUR_N01");

            Assert.That(remedy.EffectTags, Does.Contain("HEAL"));
            Assert.That(revival.EffectTags, Does.Contain("REVIVE"));
            Assert.That(M2BattleCommandService.IsRevivalArtForVerification080(revival), Is.True);
            Assert.That(M2BattleCommandService.SupportsFriendlyUnionTargetForVerification080(remedy), Is.True);
            Assert.That(M2BattleCommandService.SupportsFriendlyUnionTargetForVerification080(revival), Is.True);
            Assert.That(M2BattleCommandService.SupportsFriendlyUnionTargetForVerification080(rally), Is.True);
            Assert.That(M2BattleCommandService.SupportsFriendlyUnionTargetForVerification080(selfRecovery), Is.False,
                "Recover Breath must remain an own-member resource fallback.");
            Assert.That(deepSupport.TargetRule, Is.EqualTo("SELF_OR_ALLY_UNION"));
            Assert.That(M2BattleCommandService.SupportsFriendlyUnionTargetForVerification080(deepSupport), Is.True);
        }

        [Test]
        public void StandAgainRevivesADownedMemberInAnotherFriendlyUnion080()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var players = campaign.Battle.PlayerUnions.ToList();
            var source = players[0];
            var target = players[1];
            var targetMembers = target.Members.ToList();
            var downed = targetMembers[targetMembers.Count - 1];
            targetMembers[targetMembers.Count - 1] = downed.With(currentHp: 0, stabilized: false);
            players[1] = target.With(members: targetMembers.AsReadOnly());

            var art = _content.Art("ART_STAND_AGAIN");
            var sourceMembers = source.Members.ToList();
            sourceMembers[0] = WithBattleArt086(
                sourceMembers[0], art.Id, "STAFF");
            source = source.With(members: sourceMembers.AsReadOnly());
            players[0] = source;
            var actor = source.Members[0];
            var action = new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, target.UnionId, downed.MemberId,
                art.Id, art.Name, BattleActionKind.Restoration,
                art.SharedApCost, art.PersonalMpCost, 28, 0, 0,
                "Revive a Downed ally.", true, false, art.AnimationTag, art.Discipline);
            var forged = new BattleForecastState(
                "FORECAST_CROSS_UNION_REVIVE_080", source.UnionId, "CMD_HEAL",
                "Stand Again!", "Bring them back into the fight.", "Rescue",
                target.UnionId, target.DisplayName, new[] { action },
                art.SharedApCost, 0, art.PersonalMpCost, "Revive one ally", "Committed",
                "Revival is meaningful.", "Fall back to the most wounded friendly Union.",
                "CROSS_UNION_REVIVE_080", "AUTHORITY_TEST_080");

            campaign = ResolveForgedForecast080(campaign, players.AsReadOnly(), forged);

            var revival = campaign.Battle.EventLog.Single(value =>
                value.EventType == "REVIVED" && value.ArtId == art.Id &&
                value.TargetMemberId == downed.MemberId);
            Assert.That(revival.ActorUnionId, Is.EqualTo(source.UnionId));
            Assert.That(revival.TargetUnionId, Is.EqualTo(target.UnionId));
            Assert.That(revival.Amount, Is.GreaterThan(0),
                "Resolution must derive revival HP from the legal Art and actor, not trust a forged preview amount.");
            Assert.That(campaign.Battle.PlayerUnions.Single(value => value.UnionId == target.UnionId)
                .Members.Single(value => value.MemberId == downed.MemberId).CurrentHp, Is.GreaterThan(0));
        }

        [Test]
        public void PlainRestorationRejectsUnvouchedInvocationLikeCostSurcharge086()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var players = campaign.Battle.PlayerUnions.ToList();
            var source = players[0];
            var target = players[1];
            var targetMembers = target.Members.ToList();
            var downed = targetMembers[targetMembers.Count - 1];
            targetMembers[targetMembers.Count - 1] = downed.With(
                currentHp: 0, stabilized: false);
            target = target.With(members: targetMembers.AsReadOnly());

            var art = _content.Art("ART_STAND_AGAIN");
            var sourceMembers = source.Members.ToList();
            sourceMembers[0] = WithBattleArt086(
                sourceMembers[0], art.Id, "STAFF");
            source = source.With(members: sourceMembers.AsReadOnly());
            players[0] = source;
            players[1] = target;
            var actor = source.Members[0];
            var apBefore = source.CurrentAp;
            var mpBefore = actor.CurrentMp;
            var action = new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, target.UnionId, downed.MemberId,
                art.Id, art.Name, BattleActionKind.Restoration,
                art.SharedApCost + 1, art.PersonalMpCost + 1, 28, 0, 0,
                "Unvouched surcharge must not pass Art legality.", true, false,
                art.AnimationTag, art.Discipline);
            var forged = new BattleForecastState(
                "FORECAST_UNVOUCHED_RESTORATION_COST_086", source.UnionId,
                "CMD_HEAL", "Forged Invocation Heal", "Reject the unvouched surcharge.",
                "Rescue", target.UnionId, target.DisplayName, new[] { action },
                action.SharedApCost, 0, action.PersonalMpCost, "Revive one ally",
                "Committed", "The target genuinely needs revival.",
                "Do not accept costs without Invocation authority.",
                "UNVOUCHED_RESTORATION_COST_086", "AUTHORITY_TEST_086");

            campaign = ResolveForgedForecast080(
                campaign, players.AsReadOnly(), forged);

            var roundEvents = campaign.Battle.RoundRecords.Last().Events;
            Assert.That(roundEvents.Any(value =>
                value.EventType == "REVIVED" &&
                value.ActorMemberId == actor.MemberId &&
                value.ArtId == art.Id), Is.False);
            Assert.That(roundEvents.Any(value =>
                value.EventType == "RESTORATION_WITHHELD" &&
                value.ActorMemberId == actor.MemberId &&
                value.ArtId == art.Id), Is.True);
            var resolvedSource = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == source.UnionId);
            Assert.That(resolvedSource.CurrentAp,
                Is.EqualTo(Math.Min(resolvedSource.MaximumAp, apBefore + 3)));
            Assert.That(resolvedSource.Members.Single(value =>
                    value.MemberId == actor.MemberId).CurrentMp,
                Is.EqualTo(mpBefore));
        }

        [Test]
        public void CrossUnionSupportRejectsInvalidTargetAndFallsBackToMostNeedyAlly080()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var players = campaign.Battle.PlayerUnions.ToList();
            var source = players[0].With(cohesion: 100, formationConditionBasisPoints: 10000);
            var target = players[1].With(cohesion: 42, formationConditionBasisPoints: 4200);
            var art = _content.Art("ART_BRASS_RALLY");
            var sourceMembers = source.Members.ToList();
            sourceMembers[0] = WithBattleArt086(
                sourceMembers[0], art.Id, "SIGNAL_HORN", "COMMAND");
            source = source.With(members: sourceMembers.AsReadOnly());
            players[0] = source;
            players[1] = target;
            var actor = source.Members[0];
            var action = new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, "ENEMY_FORGED_TARGET_080", string.Empty,
                art.Id, art.Name, BattleActionKind.Recovery,
                art.SharedApCost, art.PersonalMpCost, 0, 10, 1200,
                "Rally a friendly Union.", true, false, art.AnimationTag, art.Discipline);
            var forged = new BattleForecastState(
                "FORECAST_CROSS_UNION_SUPPORT_080", source.UnionId, "CMD_SUPPORT",
                "Form Up!", "Rally the Union that needs it most.", "Support",
                "ENEMY_FORGED_TARGET_080", "Forged enemy target", new[] { action },
                art.SharedApCost, 0, art.PersonalMpCost, "Cohesion +10, formation +12%", "Committed",
                "Support is meaningful.", "Fall back to the most needy active friendly Union.",
                "CROSS_UNION_SUPPORT_080", "AUTHORITY_TEST_080");

            campaign = ResolveForgedForecast080(campaign, players.AsReadOnly(), forged);

            var recovery = campaign.Battle.EventLog.Single(value =>
                value.EventType == "FORMATION_RECOVERY" && value.ArtId == "CMD_SUPPORT" &&
                value.ActorUnionId == source.UnionId);
            Assert.That(recovery.TargetUnionId, Is.EqualTo(target.UnionId));
            Assert.That(recovery.TargetUnionId, Is.Not.EqualTo("ENEMY_FORGED_TARGET_080"));
            Assert.That(recovery.Text, Does.Contain(source.DisplayName).And.Contain(target.DisplayName));
            Assert.That(campaign.Battle.PlayerUnions.Single(value => value.UnionId == target.UnionId)
                .FormationConditionBasisPoints, Is.GreaterThan(target.FormationConditionBasisPoints));
        }

        [Test]
        public void CriticalAlliedUnionReceivesReadableLegalRescueForecast086()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_0", 9, null);
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_CROSS_UNION_RESCUE_086",
                "Rescue the wounded allied Union.", 1));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var target = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_0"));
            var rescue = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId && value.CommandId == "CMD_HEAL");
            var action = rescue.MemberActions.Single(value =>
                value.Kind == BattleActionKind.Restoration);
            var actor = source.Members.Single(value =>
                value.MemberId == action.ActorMemberId);
            var art = _content.Art(action.ArtId);

            Assert.That(rescue.CommandName, Is.EqualTo("Save Them!"));
            Assert.That(rescue.TargetId, Is.EqualTo(target.UnionId));
            Assert.That(action.TargetUnionId, Is.EqualTo(target.UnionId));
            Assert.That(action.TargetMemberId, Is.EqualTo("RECRUIT_0"));
            Assert.That(action.Prediction,
                Does.Contain(target.DisplayName).And.Contain("heal Recruit 0"));
            Assert.That(actor.LearnedArtIds, Does.Contain(art.Id));
            Assert.That(art.RequiredEquipmentTags.Count == 0 ||
                        art.RequiredEquipmentTags.Any(actor.EquipmentTags.Contains), Is.True);
            Assert.That(action.SharedApCost, Is.LessThanOrEqualTo(source.CurrentAp));
            Assert.That(action.PersonalMpCost, Is.LessThanOrEqualTo(actor.CurrentMp));

            var beforeTargetHp = target.Members.Single(value =>
                value.MemberId == action.TargetMemberId).CurrentHp;
            var beforeActorMp = actor.CurrentMp;
            campaign = SelectForecasts086(campaign, source.UnionId, rescue.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var resolvedEvent = campaign.Battle.RoundRecords.Last().Events.Single(value =>
                value.EventType == "RESTORATION" &&
                value.ActorMemberId == action.ActorMemberId &&
                value.TargetMemberId == action.TargetMemberId);
            Assert.That(resolvedEvent.ActorUnionId, Is.EqualTo(source.UnionId));
            Assert.That(resolvedEvent.TargetUnionId, Is.EqualTo(target.UnionId));
            Assert.That(resolvedEvent.Text,
                Does.Contain("across the line").And.Contain(target.DisplayName));
            var afterTarget = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == target.UnionId).Members.Single(value =>
                value.MemberId == action.TargetMemberId);
            var afterActor = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == source.UnionId).Members.Single(value =>
                value.MemberId == action.ActorMemberId);
            Assert.That(afterTarget.CurrentHp, Is.GreaterThan(beforeTargetHp));
            Assert.That(afterActor.CurrentMp,
                Is.EqualTo(beforeActorMp - action.PersonalMpCost));
        }

        [Test]
        public void NearlyHealthyAllyUsesMinorRemedyInsteadOfMajorMassHeal086()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_0", 100, null);
            campaign = ConfigureRecruit086(
                campaign, "RECRUIT_3", null, null,
                "ART_DAWN_WITHOUT_LOSS");
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_RESTORATION_NO_WASTE_086",
                "Use only the support needed.", 1));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var rescue = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId && value.CommandId == "CMD_HEAL");
            var action = rescue.MemberActions.Single(value =>
                value.Kind == BattleActionKind.Restoration);

            Assert.That(action.ArtId, Is.EqualTo("ART_MINOR_REMEDY"));
            Assert.That(action.ArtId, Is.Not.EqualTo("ART_DAWN_WITHOUT_LOSS"));
            Assert.That(action.PredictedHpDelta, Is.EqualTo(5));
            Assert.That(rescue.CommandName, Is.EqualTo("Support the Other Union!"));
        }

        [Test]
        public void OnlyMajorMassHealIsOmittedForTrivialOneHpDamage086()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(1, 1, 1, 1, 1),
                "RECRUIT_0", 104, null);
            campaign = ConfigureRecruit086(
                campaign, "RECRUIT_4", null, null,
                "ART_DAWN_WITHOUT_LOSS");
            campaign = ConfigureUnionCohesion086(
                campaign, "UNION_OPENING_01", 10000);
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_MAJOR_HEAL_ANTI_WASTE_086",
                "Do not spend a major rescue on a scratch.", 1));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_4"));
            var actor = source.Members.Single(value =>
                value.MemberId == "RECRUIT_4");
            var restorationArts = actor.LearnedArtIds.Where(value =>
                _content.Arts.TryGetValue(value, out var art) &&
                art.Discipline == "Restoration").ToArray();

            Assert.That(restorationArts,
                Is.EquivalentTo(new[] { "ART_DAWN_WITHOUT_LOSS" }),
                "This proof must offer only the expensive mass-heal option.");
            Assert.That(campaign.Battle.CommittedForecasts.Any(value =>
                value.UnionId == source.UnionId &&
                value.CommandId == "CMD_HEAL"), Is.False,
                "A one-HP scratch must not advertise a 9 AP / 18 MP mass heal.");
        }

        [Test]
        public void StaleMajorHealRetargetsToTheMostNeedyLegalAlly086()
        {
            var campaign = StartOverlappingRestorationBattle086();
            var originalTarget = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_0"));
            var adaptiveTargetIndex = campaign.Battle.PlayerUnions.ToList()
                .FindIndex(value => value.Members.Any(member =>
                    member.MemberId == "RECRUIT_1"));
            var players = campaign.Battle.PlayerUnions.ToList();
            var adaptiveMembers = players[adaptiveTargetIndex].Members.ToList();
            adaptiveMembers[0] = adaptiveMembers[0].With(currentHp: 10);
            players[adaptiveTargetIndex] = players[adaptiveTargetIndex].With(
                members: adaptiveMembers.AsReadOnly());
            campaign = campaign.WithBattle(campaign.Battle.With(
                playerUnions: players.AsReadOnly()));

            var priestSource = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var majorSource = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_4"));
            var adaptiveTarget = campaign.Battle.PlayerUnions[adaptiveTargetIndex];
            var priestForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == priestSource.UnionId &&
                value.CommandId == "CMD_HEAL");
            var majorForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == majorSource.UnionId &&
                value.CommandId == "CMD_HEAL");
            var majorAction = majorForecast.MemberActions.Single(value =>
                value.ArtId == "ART_DAWN_WITHOUT_LOSS");
            Assert.That(priestForecast.TargetId, Is.EqualTo(originalTarget.UnionId));
            Assert.That(majorAction.TargetUnionId, Is.EqualTo(originalTarget.UnionId));
            var apBefore = majorSource.CurrentAp;
            var mpBefore = majorSource.Members.Single(value =>
                value.MemberId == "RECRUIT_4").CurrentMp;

            foreach (var union in campaign.Battle.PlayerUnions.Where(value =>
                         !value.Retreated && !value.IsDefeated))
            {
                var forecast = union.UnionId == priestSource.UnionId
                    ? priestForecast
                    : union.UnionId == majorSource.UnionId
                        ? majorForecast
                        : campaign.Battle.CommittedForecasts.Single(value =>
                            value.UnionId == union.UnionId &&
                            value.CommandId == "CMD_GUARD");
                campaign = Require(_commands.SelectForecast(
                    campaign, union.UnionId, forecast.ForecastId));
            }
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var events = campaign.Battle.RoundRecords.Last().Events;
            var adapted = events.Single(value =>
                value.EventType == "RESTORATION_RETARGETED" &&
                value.ActorMemberId == "RECRUIT_4" &&
                value.ArtId == "ART_DAWN_WITHOUT_LOSS");
            var restoration = events.Single(value =>
                value.EventType == "RESTORATION" &&
                value.ActorMemberId == "RECRUIT_4" &&
                value.ArtId == "ART_DAWN_WITHOUT_LOSS");
            Assert.That(adapted.TargetUnionId,
                Is.EqualTo(adaptiveTarget.UnionId));
            Assert.That(adapted.Text,
                Does.Contain(adaptiveTarget.DisplayName).And.Contain("adapts"));
            Assert.That(restoration.TargetUnionId,
                Is.EqualTo(adaptiveTarget.UnionId));
            Assert.That(events.Any(value =>
                value.EventType == "RESTORATION" &&
                value.ActorMemberId == "RECRUIT_4" &&
                value.TargetUnionId == originalTarget.UnionId), Is.False);
            var resolvedSource = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == majorSource.UnionId);
            Assert.That(resolvedSource.CurrentAp,
                Is.EqualTo(Math.Min(
                    resolvedSource.MaximumAp,
                    apBefore - majorAction.SharedApCost + 3)));
            Assert.That(resolvedSource.Members.Single(value =>
                    value.MemberId == "RECRUIT_4").CurrentMp,
                Is.EqualTo(mpBefore - majorAction.PersonalMpCost));
        }

        [Test]
        public void StaleMajorHealWithNoJustifiedTargetPreservesApAndMp086()
        {
            var campaign = StartOverlappingRestorationBattle086();
            var priestSource = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var majorSource = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_4"));
            var priestForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == priestSource.UnionId &&
                value.CommandId == "CMD_HEAL");
            var majorForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == majorSource.UnionId &&
                value.CommandId == "CMD_HEAL");
            var majorAction = majorForecast.MemberActions.Single(value =>
                value.ArtId == "ART_DAWN_WITHOUT_LOSS");
            var apBefore = majorSource.CurrentAp;
            var mpBefore = majorSource.Members.Single(value =>
                value.MemberId == "RECRUIT_4").CurrentMp;

            foreach (var union in campaign.Battle.PlayerUnions.Where(value =>
                         !value.Retreated && !value.IsDefeated))
            {
                var forecast = union.UnionId == priestSource.UnionId
                    ? priestForecast
                    : union.UnionId == majorSource.UnionId
                        ? majorForecast
                        : campaign.Battle.CommittedForecasts.Single(value =>
                            value.UnionId == union.UnionId &&
                            value.CommandId == "CMD_GUARD");
                campaign = Require(_commands.SelectForecast(
                    campaign, union.UnionId, forecast.ForecastId));
            }
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var events = campaign.Battle.RoundRecords.Last().Events;
            var withheld = events.Single(value =>
                value.EventType == "RESTORATION_WITHHELD" &&
                value.ActorMemberId == "RECRUIT_4" &&
                value.ArtId == "ART_DAWN_WITHOUT_LOSS");
            Assert.That(withheld.Amount, Is.EqualTo(majorAction.SharedApCost));
            Assert.That(withheld.Text,
                Does.Contain("AP preserved").And.Contain("no MP spent"));
            Assert.That(events.Any(value =>
                value.EventType == "RESTORATION" &&
                value.ActorMemberId == "RECRUIT_4" &&
                value.ArtId == "ART_DAWN_WITHOUT_LOSS"), Is.False);
            var resolvedSource = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId == majorSource.UnionId);
            Assert.That(resolvedSource.CurrentAp, Is.EqualTo(apBefore));
            Assert.That(resolvedSource.Members.Single(value =>
                    value.MemberId == "RECRUIT_4").CurrentMp,
                Is.EqualTo(mpBefore));
        }

        [Test]
        public void CriticalMemberOutweighsLargerButModerateUnionHpDeficit086()
        {
            var campaign = CreateCampaign(1, 2, 3);
            campaign = ConfigureRecruit086(campaign, "RECRUIT_0", 30, null);
            campaign = ConfigureRecruit086(campaign, "RECRUIT_1", 55, null);
            campaign = ConfigureRecruit086(campaign, "RECRUIT_2", 55, null);
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_CRITICAL_PRIORITY_086",
                "Save the ally closest to destruction.", 1));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var criticalTarget = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_0"));
            var moderateTarget = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_1"));
            Assert.That(moderateTarget.Members.Sum(value =>
                    value.MaximumHp - value.CurrentHp),
                Is.GreaterThan(criticalTarget.Members.Sum(value =>
                    value.MaximumHp - value.CurrentHp)));

            var rescue = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId && value.CommandId == "CMD_HEAL");
            Assert.That(rescue.TargetId, Is.EqualTo(criticalTarget.UnionId));
            Assert.That(rescue.CommandName, Is.EqualTo("Save Them!"));
        }

        [Test]
        public void MassHealingForecastPredictsAndRestoresMultipleAlliedMembers086()
        {
            var campaign = CreateCampaign(3, 3);
            campaign = ConfigureRecruit086(campaign, "RECRUIT_0", 10, null);
            campaign = ConfigureRecruit086(campaign, "RECRUIT_1", 12, null);
            campaign = ConfigureRecruit086(campaign, "RECRUIT_2", 14, null);
            campaign = ConfigureRecruit086(
                campaign, "RECRUIT_3", null, null,
                "ART_DAWN_WITHOUT_LOSS");
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_GROUP_RESCUE_086",
                "Restore the collapsing allied line.", 1));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var target = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_0"));
            var rescue = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId && value.CommandId == "CMD_HEAL");
            var action = rescue.MemberActions.Single(value =>
                value.Kind == BattleActionKind.Restoration);

            Assert.That(action.ArtId, Is.EqualTo("ART_DAWN_WITHOUT_LOSS"));
            Assert.That(action.Prediction,
                Does.Contain("Recruit 0").And.Contain("Recruit 1").And.Contain("Recruit 2"));
            campaign = SelectForecasts086(campaign, source.UnionId, rescue.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var healingEvents = campaign.Battle.RoundRecords.Last().Events.Where(value =>
                value.EventType == "RESTORATION" &&
                value.ArtId == "ART_DAWN_WITHOUT_LOSS" &&
                value.TargetUnionId == target.UnionId).ToArray();
            Assert.That(healingEvents.Length, Is.EqualTo(3));
            Assert.That(healingEvents.Select(value => value.TargetMemberId),
                Is.EquivalentTo(new[] { "RECRUIT_0", "RECRUIT_1", "RECRUIT_2" }));
        }

        [TestCase("ART_STAND_AGAIN")]
        [TestCase("TREE_CA002_MYS_RESTORATION_N08")]
        [TestCase("TREE_CA002_ROLE_FIELD_MEDIC_N08")]
        public void LearnedStandAgainNaturallyWinsForecastWhenAnotherUnionIsDowned086(string revivalArtId)
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_3", null, null,
                revivalArtId);
            if (_content.DeepProgression.TryNode(revivalArtId, out var revivalNode))
            {
                // A learned Mystic node is intentionally dormant while its tree
                // is locked. This positive rescue case represents the legitimately
                // unlocked progression path, not merely an injected learned ID.
                var recruits = campaign.Guild.Recruits.ToList();
                var healerIndex = recruits.FindIndex(value => value.RecruitId == "RECRUIT_3");
                var healer = recruits[healerIndex];
                var priorNodes = _content.DeepProgression.Tree(revivalNode.TreeId).NodeIds
                    .Where(value => _content.DeepProgression.Node(value).Index <= revivalNode.Index);
                var progression = healer.Progression.WithArts(
                    healer.Progression.LearnedArtIds.Concat(priorNodes)
                        .Distinct(StringComparer.Ordinal).ToArray(), healer.Progression.ArtMastery)
                    .WithUnlockedTrees(healer.Progression.UnlockedTreeIds
                        .Concat(new[] { revivalNode.TreeId }).Distinct(StringComparer.Ordinal).ToArray());
                recruits[healerIndex] = healer.WithProgression(progression);
                campaign = campaign.With(campaign.Guild.With(
                    campaign.Guild.TreasuryXp, recruits.AsReadOnly(), campaign.Guild.Unions,
                    campaign.Guild.Inventory), campaign.OpeningFlow);
            }
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_NATURAL_REVIVE_086",
                "Bring a fallen ally back into the battle.", 1));
            var players = campaign.Battle.PlayerUnions.ToList();
            var target = players.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_0"));
            var targetIndex = players.FindIndex(value => value.UnionId == target.UnionId);
            var targetMembers = target.Members.ToList();
            var downedIndex = targetMembers.FindIndex(value =>
                value.MemberId == "RECRUIT_0");
            targetMembers[downedIndex] = targetMembers[downedIndex].With(
                currentHp: 0, stabilized: false);
            players[targetIndex] = target.With(members: targetMembers.AsReadOnly());
            campaign = campaign.WithBattle(campaign.Battle.With(
                playerUnions: players.AsReadOnly()));
            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_GUARD");
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var actingHealer = source.Members.Single(value => value.MemberId == "RECRUIT_3");
            Assert.That(actingHealer.LearnedArtIds, Does.Contain(revivalArtId),
                "The positive rescue fixture must enter combat with its revival Art legally active.");
            Assert.That(actingHealer.CurrentMp, Is.GreaterThanOrEqualTo(_content.Art(revivalArtId).PersonalMpCost));
            Assert.That(source.CurrentAp, Is.GreaterThanOrEqualTo(_content.Art(revivalArtId).SharedApCost));
            var rescue = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId && value.CommandId == "CMD_HEAL");
            var action = rescue.MemberActions.Single(value =>
                value.Kind == BattleActionKind.Restoration);
            Assert.That(rescue.CommandName, Is.EqualTo("Save Them!"));
            Assert.That(action.ArtId, Is.EqualTo(revivalArtId));
            Assert.That(action.TargetMemberId, Is.EqualTo("RECRUIT_0"));
            Assert.That(action.Prediction, Does.Contain("revive Recruit 0"));

            campaign = SelectForecasts086(campaign, source.UnionId, rescue.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var revival = campaign.Battle.RoundRecords.Last().Events.Single(value =>
                value.EventType == "REVIVED" &&
                value.ArtId == revivalArtId &&
                value.TargetMemberId == "RECRUIT_0");
            Assert.That(revival.ActorUnionId, Is.EqualTo(source.UnionId));
            Assert.That(revival.TargetUnionId, Is.Not.EqualTo(source.UnionId));
            Assert.That(revival.Amount, Is.GreaterThan(0));
        }

        [Test]
        public void LearnedRevivalInLockedMysticTreeCannotEnterBattleForecast090()
        {
            const string artId = "TREE_CA002_MYS_RESTORATION_N08";
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_3", null, null, artId);
            var healer = campaign.Guild.Recruits.Single(value => value.RecruitId == "RECRUIT_3");
            Assert.That(healer.Progression.LearnedArtIds, Does.Contain(artId));
            Assert.That(healer.Progression.UnlockedTreeIds,
                Does.Not.Contain("TREE_CA002_MYS_RESTORATION"));
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(healer, _content), Does.Not.Contain(artId));
            campaign = Require(_commands.StartEncounterBattle(campaign, _content,
                "BATTLE_LOCKED_REVIVAL_090", "Preserve the existing Mystic-tree unlock authority.", 1));
            Assert.That(campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == healer.RecruitId).LearnedArtIds, Does.Not.Contain(artId));
            Assert.That(campaign.Battle.CommittedForecasts.SelectMany(value => value.MemberActions)
                .Any(value => value.ArtId == artId), Is.False);
        }

        [Test]
        public void RevivedUnionWaitsUntilNextRoundForItsOwnForecast086()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_3", null, null,
                "ART_STAND_AGAIN");
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_REVIVED_ORDER_CENSUS_086",
                "Restore the defeated allied Union.", 1));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var target = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId != source.UnionId);
            var downedMembers = target.Members.Select(value => value.With(
                currentHp: 0, stabilized: false)).ToArray();
            target = target.With(members: downedMembers);
            var actor = source.Members.Single(value =>
                value.MemberId == "RECRUIT_3");
            var art = _content.Art("ART_STAND_AGAIN");
            var action = new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, target.UnionId,
                target.Members[0].MemberId, art.Id, art.Name,
                BattleActionKind.Restoration, art.SharedApCost,
                art.PersonalMpCost, 30, 0, 0,
                "Revive the defeated allied Union.", true, false,
                art.AnimationTag, art.Discipline);
            var forged = new BattleForecastState(
                "FORECAST_REVIVED_ORDER_CENSUS_086", source.UnionId,
                "CMD_HEAL", "Save Them!", "Bring them back.", "Rescue",
                target.UnionId, target.DisplayName, new[] { action },
                art.SharedApCost, 0, art.PersonalMpCost,
                "Revive one ally.", "Committed", "Revival is meaningful.",
                "Fall back to the next legal ally.",
                "REVIVED_ORDER_CENSUS_086", "AUTHORITY_TEST_086");

            campaign = ResolveForgedForecast080(
                campaign, new[] { source, target }, forged);

            Assert.That(campaign.Battle.RoundRecords.Last().Events.Any(value =>
                value.EventType == "REVIVED" &&
                value.TargetUnionId == target.UnionId), Is.True);
            Assert.That(campaign.Battle.CommittedForecasts.Any(value =>
                value.UnionId == target.UnionId), Is.True,
                "The restored Union should receive commands only after the next round is committed.");
        }

        [Test]
        public void LegalCrossUnionSupportForecastRestoresTheOtherFormation086()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_3", null, null,
                "ART_BRASS_RALLY");
            campaign = ConfigureRecruitEquipment086(
                campaign, "RECRUIT_3",
                "STAFF", "HEALING", "SIGNAL_HORN", "COMMAND");
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_CROSS_UNION_SUPPORT_086",
                "Keep the allied line together.", 1,
                new[] { "HIGH_FATIGUE" }));
            var source = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var target = campaign.Battle.PlayerUnions.Single(value =>
                value.UnionId != source.UnionId);
            var support = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == source.UnionId && value.CommandId == "CMD_SUPPORT");
            var action = support.MemberActions.Single(value =>
                value.ArtId == "ART_BRASS_RALLY");

            Assert.That(support.CommandName, Is.EqualTo("Restore Their Formation!"));
            Assert.That(support.TargetId, Is.EqualTo(target.UnionId));
            Assert.That(action.TargetUnionId, Is.EqualTo(target.UnionId));
            Assert.That(action.Prediction,
                Does.Contain(target.DisplayName).And.Contain("Cohesion"));
            var cohesionBefore = target.Cohesion;
            campaign = SelectForecasts086(campaign, source.UnionId, support.ForecastId);
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            var supportEvent = campaign.Battle.RoundRecords.Last().Events.Single(value =>
                value.EventType == "ALLY_SUPPORT" &&
                value.ActorMemberId == "RECRUIT_3");
            Assert.That(supportEvent.ActorUnionId, Is.EqualTo(source.UnionId));
            Assert.That(supportEvent.TargetUnionId, Is.EqualTo(target.UnionId));
            Assert.That(supportEvent.Text, Does.Contain("across the line"));
            Assert.That(campaign.Battle.PlayerUnions.Single(value =>
                    value.UnionId == target.UnionId).Cohesion,
                Is.GreaterThan(cohesionBefore));
        }

        [Test]
        public void CleanseForecastClearsExistingBrokenPressureWithoutInventingHp086()
        {
            var campaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_3", null, null,
                "ART_CLEANSE");
            campaign = Require(_commands.StartEncounterBattle(
                campaign, _content, "BATTLE_CROSS_UNION_CLEANSE_086",
                "Clear the allied Union's broken pressure.", 1));
            var originalPlayers = campaign.Battle.PlayerUnions.ToList();
            var source = originalPlayers.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var target = originalPlayers.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_0"));
            var targetMembers = target.Members.ToList();
            targetMembers[0] = targetMembers[0].With(currentHp: 50);
            target = target.With(
                members: targetMembers.AsReadOnly(),
                cohesion: 20,
                formationConditionBasisPoints: 4000,
                engagement: EngagementState.Broken);
            // Put the support Union first so its legal cleanse resolves while Broken
            // pressure is still present; the target's later Guard order may then
            // transition its engagement normally.
            var players = new[] { source, target };
            var actor = source.Members.Single(value =>
                value.MemberId == "RECRUIT_3");
            var art = _content.Art("ART_CLEANSE");
            var action = new BattlePlannedActionState(
                actor.MemberId, actor.DisplayName, target.UnionId,
                target.LeaderMemberId, art.Id, art.Name,
                BattleActionKind.Restoration, art.SharedApCost,
                art.PersonalMpCost, 0, 10, 500,
                "Help " + target.DisplayName +
                ": Cohesion +10, formation +5%, clear Broken pressure.",
                true, false, art.AnimationTag, art.Discipline);
            var forged = new BattleForecastState(
                "FORECAST_CROSS_UNION_CLEANSE_086", source.UnionId,
                "CMD_HEAL", "Protect and Heal Them!",
                "Clear the allied line.", "Rescue", target.UnionId,
                target.DisplayName, new[] { action }, art.SharedApCost, 0,
                art.PersonalMpCost, "Clear Broken pressure.", "Committed",
                "Cleanse only while an existing status is present.",
                "Fallback to a legal heal or guard.",
                "CROSS_UNION_CLEANSE_086", "AUTHORITY_TEST_086");
            Assert.That(action.PredictedHpDelta, Is.Zero);
            Assert.That(action.Prediction, Does.Contain("clear Broken pressure"));
            campaign = ResolveForgedForecast080(
                campaign, players, forged);

            var roundEvents = campaign.Battle.RoundRecords.Last().Events;
            Assert.That(roundEvents.Any(value =>
                value.EventType == "CLEANSED" &&
                value.ArtId == "ART_CLEANSE" &&
                value.TargetUnionId != value.ActorUnionId), Is.True);
            Assert.That(roundEvents.Any(value =>
                value.EventType == "RESTORATION" &&
                value.ArtId == "ART_CLEANSE"), Is.False,
                "A cleanse may restore Cohesion/formation but must not fabricate HP healing.");
        }

        [TestCase("ART_FIELD_REMEDY", 1, "RESTORATION")]
        [TestCase("ART_STAND_AGAIN", 0, "REVIVED")]
        [TestCase("TREE_CA002_MYS_RESTORATION_N08", 0, "REVIVED")]
        [TestCase("TREE_CA002_ROLE_FIELD_MEDIC_N08", 0, "REVIVED")]
        public void EnemyMedicUsesSameLegalRestorationPathToRescueAlliedUnion086(
            string restorationArtId, int targetHp, string expectedEventType)
        {
            var medic = _content.Enemy("ENEMY_ASH_MEDIC_01");
            var wounded = _content.Enemy("ENEMY_GATE_GNAWER_01");
            var medicSource = new M2EnemyUnionDefinition(
                "SOURCE_MEDIC_UNION_086", "Ash Medic Wing", medic.Id,
                "FORMATION_SHIELD_WALL", 18, 80, new[] { medic.Id }, 1000);
            var woundedSource = new M2EnemyUnionDefinition(
                "SOURCE_WOUNDED_UNION_086", "Wounded Gnawer Wing", wounded.Id,
                "FORMATION_SHIELD_WALL", 18, 80, new[] { wounded.Id }, 1000);
            var medicUnion = new EncounterEnemyUnion070(
                "ENEMY_MEDIC_UNION_086", medicSource.Id, medicSource,
                new[]
                {
                    new EncounterEnemyMember070(
                        "ENEMY_MEDIC_MEMBER_086", medic.Id,
                        "ENEMY_FAMILY_ASH_MEDIC", 86, medic)
                },
                new[] { "ENEMY_FAMILY_ASH_MEDIC" });
            var woundedUnion = new EncounterEnemyUnion070(
                "ENEMY_WOUNDED_UNION_086", woundedSource.Id, woundedSource,
                new[]
                {
                    new EncounterEnemyMember070(
                        "ENEMY_WOUNDED_MEMBER_086", wounded.Id,
                        "ENEMY_FAMILY_GATE_GNAWER", 87, wounded)
                },
                new[] { "ENEMY_FAMILY_GATE_GNAWER" });
            var roster = new EncounterRoster070(
                "ROSTER_ENEMY_RESCUE_086", "SEED_ENEMY_RESCUE_086",
                new[] { medicUnion, woundedUnion },
                new[] { "ENEMY_FAMILY_ASH_MEDIC", "ENEMY_FAMILY_GATE_GNAWER" });
            var campaign = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3), _content,
                "BATTLE_ENEMY_RESCUE_086", "Break the enemy support line.", roster));
            var enemies = campaign.Battle.EnemyUnions.ToList();
            var medicIndex = enemies.FindIndex(value => value.UnionId == medicUnion.UnionId);
            var medicMembers = enemies[medicIndex].Members.ToList();
            medicMembers[0] = medicMembers[0].With(learnedArtIds: new[] { restorationArtId });
            enemies[medicIndex] = enemies[medicIndex].With(members: medicMembers.AsReadOnly());
            var targetIndex = enemies.FindIndex(value =>
                value.UnionId == woundedUnion.UnionId);
            var enemyMembers = enemies[targetIndex].Members.ToList();
            enemyMembers[0] = enemyMembers[0].With(currentHp: targetHp);
            enemies[targetIndex] = enemies[targetIndex].With(
                members: enemyMembers.AsReadOnly(),
                cohesion: 20,
                formationConditionBasisPoints: 3000);
            campaign = campaign.WithBattle(campaign.Battle.With(
                enemyUnions: enemies.AsReadOnly()));
            var committedMedic = enemies.Single(value =>
                value.UnionId == medicUnion.UnionId).Members[0];
            Assert.That(committedMedic.LearnedArtIds,
                Does.Contain(restorationArtId));
            Assert.That(committedMedic.EquipmentTags, Does.Contain("STAFF"));
            var medicMpBefore = committedMedic.CurrentMp;
            var medicApBefore = enemies[medicIndex].CurrentAp;

            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_GUARD");
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var events = campaign.Battle.RoundRecords.Last().Events;
            var forecastEvent = events.Single(value =>
                value.EventType == "ENEMY_SUPPORT_FORECAST" &&
                value.ActorUnionId == medicUnion.UnionId);
            var healEvent = events.Single(value =>
                value.EventType == expectedEventType &&
                value.Side == BattleSide.Enemy &&
                value.ActorUnionId == medicUnion.UnionId &&
                value.TargetUnionId == woundedUnion.UnionId);
            Assert.That(forecastEvent.TargetUnionId,
                Is.EqualTo(woundedUnion.UnionId));
            Assert.That(healEvent.ArtId, Is.EqualTo(restorationArtId));
            Assert.That(healEvent.Amount, Is.GreaterThan(0));
            var resolvedMedic = campaign.Battle.EnemyUnions.Single(value =>
                value.UnionId == medicUnion.UnionId).Members[0];
            Assert.That(resolvedMedic.CurrentMp,
                Is.EqualTo(medicMpBefore -
                    _content.Art(restorationArtId).PersonalMpCost));
            Assert.That(campaign.Battle.EnemyUnions.Single(value =>
                    value.UnionId == medicUnion.UnionId).CurrentAp,
                Is.EqualTo(Math.Min(enemies[medicIndex].MaximumAp,
                    medicApBefore - _content.Art(restorationArtId).SharedApCost + 3)),
                "Enemy restoration pays the same authored Art AP cost as player restoration.");
            Assert.That(campaign.Battle.EnemyUnions.Single(value =>
                    value.UnionId == woundedUnion.UnionId).Members[0].CurrentHp,
                Is.GreaterThan(targetHp));
        }

        [Test]
        public void DeepRevivalBindingsPreserveAuthoredAuthorityAndDoNotActivatePassives090()
        {
            foreach (var artId in new[]
                     { "TREE_CA002_MYS_RESTORATION_N08", "TREE_CA002_ROLE_FIELD_MEDIC_N08" })
            {
                var runtime = _content.DeepProgression.RuntimeArts.Single(value => value.NodeId == artId);
                var art = _content.Art(artId);
                Assert.That(runtime.LegacyArtIds, Does.Contain("ART_STAND_AGAIN"));
                Assert.That(M2BattleCommandService.IsRevivalArtForVerification080(art), Is.True);
                Assert.That(art.SharedApCost, Is.EqualTo(runtime.SharedApCost));
                Assert.That(art.PersonalMpCost, Is.EqualTo(runtime.PersonalMpCost));
                Assert.That(art.RequiredEquipmentTags, Is.EqualTo(runtime.RequiredEquipmentTagsAny));
                Assert.That(art.TargetRule, Is.EqualTo(runtime.TargetRule));
            }
            foreach (var id in new[]
                     { "TREE_CA002_MYS_RESTORATION_N01", "TREE_CA002_MYS_RESTORATION_N05",
                       "TREE_CA002_MYS_RESTORATION_N09", "TREE_CA002_ROLE_FIELD_MEDIC_N09" })
                Assert.That(M2BattleCommandService.IsRevivalArtForVerification080(_content.Art(id)),
                    Is.False, id + " is not an authorized learned revival action.");
        }

        [Test]
        public void EnemyPulseScribeUsesLegalWardSigilOnCriticalAlliedUnion086()
        {
            var scribe = _content.Enemy("ENEMY_PULSE_SCRIBE_01");
            var ally = _content.Enemy("ENEMY_GATE_GNAWER_01");
            var scribeSource = new M2EnemyUnionDefinition(
                "SOURCE_SCRIBE_UNION_086", "Pulse Scribe Wing", scribe.Id,
                "FORMATION_ARCANE_CIRCLE", 18, 100,
                new[] { scribe.Id }, 1000);
            var allySource = new M2EnemyUnionDefinition(
                "SOURCE_WARD_TARGET_086", "Gnawer Ward", ally.Id,
                "FORMATION_SHIELD_WALL", 18, 100,
                new[] { ally.Id }, 1000);
            var scribeUnion = new EncounterEnemyUnion070(
                "ENEMY_SCRIBE_UNION_086", scribeSource.Id, scribeSource,
                new[]
                {
                    new EncounterEnemyMember070(
                        "ENEMY_SCRIBE_MEMBER_086", scribe.Id,
                        "ENEMY_FAMILY_PULSE_SCRIBE", 88, scribe)
                },
                new[] { "ENEMY_FAMILY_PULSE_SCRIBE" });
            var allyUnion = new EncounterEnemyUnion070(
                "ENEMY_WARD_TARGET_086", allySource.Id, allySource,
                new[]
                {
                    new EncounterEnemyMember070(
                        "ENEMY_WARD_TARGET_MEMBER_086", ally.Id,
                        "ENEMY_FAMILY_GATE_GNAWER", 89, ally)
                },
                new[] { "ENEMY_FAMILY_GATE_GNAWER" });
            var roster = new EncounterRoster070(
                "ROSTER_ENEMY_WARD_086", "SEED_ENEMY_WARD_086",
                new[] { scribeUnion, allyUnion },
                new[]
                {
                    "ENEMY_FAMILY_PULSE_SCRIBE",
                    "ENEMY_FAMILY_GATE_GNAWER"
                });
            var campaign = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3), _content,
                "BATTLE_ENEMY_WARD_086", "Break the enemy ward line.", roster));
            var enemies = campaign.Battle.EnemyUnions.ToList();
            var targetIndex = enemies.FindIndex(value =>
                value.UnionId == allyUnion.UnionId);
            enemies[targetIndex] = enemies[targetIndex].With(
                cohesion: 20,
                formationConditionBasisPoints: 3000,
                engagement: EngagementState.Broken,
                guarding: false);
            campaign = campaign.WithBattle(campaign.Battle.With(
                enemyUnions: enemies.AsReadOnly()));
            var committedScribe = enemies.Single(value =>
                value.UnionId == scribeUnion.UnionId);
            var actor = committedScribe.Members[0];
            var art = _content.Art("ART_WARD_SIGIL");
            Assert.That(actor.LearnedArtIds, Does.Contain(art.Id));
            Assert.That(actor.EquipmentTags, Does.Contain("FOCUS_TOOL"));
            var apBefore = committedScribe.CurrentAp;
            var mpBefore = actor.CurrentMp;

            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_GUARD");
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var events = campaign.Battle.RoundRecords.Last().Events;
            var forecastEvent = events.Single(value =>
                value.EventType == "ENEMY_SUPPORT_FORECAST" &&
                value.ActorUnionId == scribeUnion.UnionId);
            var supportEvent = events.Single(value =>
                value.EventType == "ALLY_SUPPORT" &&
                value.Side == BattleSide.Enemy &&
                value.ActorUnionId == scribeUnion.UnionId &&
                value.TargetUnionId == allyUnion.UnionId);
            var protectedEvent = events.Single(value =>
                value.EventType == "ALLY_PROTECTED" &&
                value.Side == BattleSide.Enemy &&
                value.ActorUnionId == scribeUnion.UnionId &&
                value.TargetUnionId == allyUnion.UnionId);

            Assert.That(forecastEvent.ArtId, Is.EqualTo(art.Id));
            Assert.That(forecastEvent.TargetUnionId,
                Is.EqualTo(allyUnion.UnionId));
            Assert.That(supportEvent.ArtId, Is.EqualTo(art.Id));
            Assert.That(protectedEvent.ArtId, Is.EqualTo(art.Id));
            var resolvedScribe = campaign.Battle.EnemyUnions.Single(value =>
                value.UnionId == scribeUnion.UnionId);
            var resolvedTarget = campaign.Battle.EnemyUnions.Single(value =>
                value.UnionId == allyUnion.UnionId);
            Assert.That(resolvedScribe.CurrentAp,
                Is.EqualTo(Math.Min(
                    resolvedScribe.MaximumAp,
                    apBefore - art.SharedApCost + 3)));
            Assert.That(resolvedScribe.Members[0].CurrentMp,
                Is.EqualTo(mpBefore - art.PersonalMpCost));
            Assert.That(resolvedTarget.Cohesion, Is.GreaterThan(20));
            Assert.That(resolvedTarget.FormationConditionBasisPoints,
                Is.GreaterThan(3000));
            Assert.That(resolvedTarget.Guarding, Is.True);
        }

        [Test]
        public void FundamentalCombatArtAndMysticCommandsBuildDistinctLegalActionPools()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var front = campaign.Battle.PlayerUnions[0];
            var fundamental = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == front.UnionId && value.CommandId == "CMD_BALANCED");
            var combatArts = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == front.UnionId && value.CommandId == "CMD_ALL_OUT");

            Assert.That(fundamental.CommandName, Is.EqualTo("Attack!"));
            Assert.That(combatArts.CommandName, Is.EqualTo("Attack Using Combat Arts!"));
            Assert.That(fundamental.MemberActions.All(value => value.SharedApCost == 0), Is.True);
            Assert.That(combatArts.MemberActions.All(value =>
                value.Discipline == "Martial" || value.Discipline == "Tactical"), Is.True);
            Assert.That(combatArts.MemberActions.Select(value => value.ArtId).ToArray(),
                Is.Not.EqualTo(fundamental.MemberActions.Select(value => value.ArtId).ToArray()));
            Assert.That(combatArts.MemberActions.Any(value => value.SharedApCost > 0), Is.True);

            var casterUnion = campaign.Battle.PlayerUnions[1];
            var mage = casterUnion.Members.Single(value => value.ClassId == "CLASS_MAGE");
            var mystic = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == casterUnion.UnionId && value.CommandId == "CMD_MYSTIC");
            var mageAction = mystic.MemberActions.Single(value => value.ActorMemberId == mage.MemberId);
            Assert.That(mystic.CommandName, Is.EqualTo("Use Mystic Arts!"));
            Assert.That(mageAction.Discipline, Is.EqualTo("Mystic"));
            Assert.That(mage.LearnedArtIds, Does.Contain(mageAction.ArtId));
        }

        [Test]
        public void GateEaterClimaxHasRealBossPowerAndAnEnforcedSkyhomeDeadline()
        {
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var bossRoster = resolver.Resolve(
                117,
                "CONTRACT_GATE_EATER",
                "BOARD_FIRST_HOUR",
                "ENCOUNTER071_GATE_EATER",
                1,
                "FIRST_HOUR_GATE_EATER_076");
            var escortRoster = resolver.Resolve(
                117,
                "CONTRACT_RELIEF_ESCORT",
                "BOARD_FIRST_HOUR",
                "ENCOUNTER_GATEIRON_ESCORT",
                1,
                "FIRST_HOUR_GATEIRON_076");
            Assert.That(bossRoster.Unions.Single().SourceUnionId, Is.EqualTo("EU_HINGE_EATER"));
            Assert.That(escortRoster.Unions.Single().SourceUnionId, Is.EqualTo("EU_BRUTE_ESCORT"));

            var bossCampaign = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER_AUTHORITY_076",
                "Defeat the Gate-Eater before it reaches Skyhome.",
                bossRoster));
            var escortCampaign = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_GATEIRON_ESCORT_AUTHORITY_076",
                "Break the Gateiron Escort.",
                escortRoster));

            var bossUnion = bossCampaign.Battle.EnemyUnions.Single();
            var boss = bossUnion.Members.Single(value =>
                value.ClassId.IndexOf(
                    M2BattleCommandService.GateEaterBossClassToken076,
                    StringComparison.OrdinalIgnoreCase) >= 0);
            var bossHp = bossUnion.Members.Sum(value => value.MaximumHp);
            var escortHp = escortCampaign.Battle.EnemyUnions.Single().Members.Sum(value => value.MaximumHp);
            Assert.That(boss.MaximumHp,
                Is.GreaterThanOrEqualTo(M2BattleCommandService.GateEaterBossMinimumMaximumHp076));
            Assert.That(boss.Attack,
                Is.GreaterThanOrEqualTo(M2BattleCommandService.GateEaterBossMinimumAttack076));
            Assert.That(bossUnion.MaximumAp,
                Is.GreaterThanOrEqualTo(M2BattleCommandService.GateEaterBossMinimumUnionAp076));
            Assert.That(bossHp, Is.GreaterThan(escortHp),
                "The first-hour climax must be stronger in authority than its earlier Gateiron escort.");
            Assert.That(bossCampaign.Battle.EventLog.Any(value =>
                value.EventType == "BOSS_ADVANCE_CLOCK" &&
                value.Amount == M2BattleCommandService.GateEaterDeadlineRounds076), Is.True);
            var leadUnion = bossCampaign.Battle.PlayerUnions.First();
            var expectedPressure = leadUnion.Members.Sum(value => value.MaximumHp) *
                                   M2BattleCommandService.GateEaterThreatPercent076 / 100;
            Assert.That(bossCampaign.Battle.EventLog.Single(value =>
                    value.EventType == M2BattleCommandService.StoryThreatTelegraphEventType076).Amount,
                Is.EqualTo(expectedPressure));
            Assert.That(bossCampaign.Battle.CommittedForecasts.Single(value =>
                    value.UnionId == leadUnion.UnionId && value.CommandId == "CMD_GUARD").Risk,
                Does.Contain("cuts").And.Contain(expectedPressure + " HP pressure"));
            Assert.That(escortCampaign.Battle.EventLog.Any(value =>
                value.EventType == M2BattleCommandService.StoryThreatTelegraphEventType076), Is.False,
                "Generic Gateiron fights must retain the general combat pressure model.");

            for (var guard = 0;
                 guard < M2BattleCommandService.GateEaterDeadlineRounds076 &&
                 bossCampaign.Battle.Outcome == BattleOutcome.InProgress;
                 guard++)
            {
                bossCampaign = SelectCommandForEveryActiveUnion(bossCampaign, "CMD_GUARD");
                bossCampaign = Require(_commands.ConfirmRound(bossCampaign, _content));
            }

            Assert.That(bossCampaign.Battle.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(bossCampaign.Battle.EventLog.Any(value => value.EventType == "BOSS_BREACH"), Is.True,
                "The displayed boss clock must be a real terminal rule, not presentation-only urgency copy.");
            Assert.That(bossCampaign.Battle.EventLog.Any(value =>
                    value.EventType == "INTERCEPTION" &&
                    value.Text.IndexOf("telegraphed strike", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    value.Amount == Math.Max(1, expectedPressure / 2)), Is.True,
                "HOLD THE LINE must cut the advertised Gate-Eater pressure in half.");
        }

        [Test]
        public void GateEaterEscortsScreenTheSignatureThreatWithoutAddingBossHpOrRounds()
        {
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var roster = resolver.Resolve(
                117,
                "CONTRACT_GATE_EATER",
                "BOARD_FIRST_HOUR",
                "ENCOUNTER071_GATE_EATER",
                3,
                "FIRST_HOUR_GATE_EATER_THREE_UNIONS_076");
            Assert.That(roster.Unions.Any(value => value.SourceUnionId == "EU_HINGE_EATER"), Is.True);

            var campaign = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER_THREE_UNIONS_076",
                "Defeat the Gate-Eater before it reaches Skyhome.",
                roster));

            Assert.That(campaign.Battle.EnemyUnions.Last().Members.Any(value =>
                value.ClassId.IndexOf(
                    M2BattleCommandService.GateEaterBossClassToken076,
                    StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                "The Gateheart and Ravel escorts must act before the Hinge-Eater signature threat.");
            Assert.That(campaign.Battle.EnemyUnions.Take(
                    campaign.Battle.EnemyUnions.Count - 1).SelectMany(value => value.Members).Any(value =>
                    value.ClassId.IndexOf(
                        M2BattleCommandService.GateEaterBossClassToken076,
                        StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(campaign.Battle.EventLog.Count(value =>
                    value.EventType == M2BattleCommandService.StoryThreatTelegraphEventType076),
                Is.EqualTo(1));
            Assert.That(M2BattleCommandService.GateEaterDeadlineRounds076, Is.EqualTo(4));
        }

        [TestCase(
            M2BattleCommandService.HallBreachBattleToken076,
            M2BattleCommandService.HallBreachEnemyHpPercent078)]
        [TestCase(
            M2BattleCommandService.LanternRoadBattleToken076,
            M2BattleCommandService.LanternRoadEnemyHpPercent078)]
        [TestCase(
            M2BattleCommandService.FogStalkersBattleToken078,
            M2BattleCommandService.FogStalkersEnemyHpPercent078)]
        [TestCase(
            M2BattleCommandService.SurveyorRescueBattleToken079,
            M2BattleCommandService.SurveyorRescueEnemyHpPercent079)]
        public void MandatorySliceEncounterEndurancePreservesDamageTruthAndTakesTwoToFourRounds(
            string encounterToken,
            int expectedHpPercent)
        {
            var baseline = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_PACING_BASELINE_078",
                "Measure the unmodified encounter roster.",
                1));
            var paced = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_PACING_" + encounterToken + "_078",
                "Prove the mandatory encounter has a readable exchange.",
                1));

            var baselineMembers = baseline.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .ToDictionary(value => value.MemberId, StringComparer.Ordinal);
            foreach (var member in paced.Battle.EnemyUnions.SelectMany(value => value.Members))
            {
                var source = baselineMembers[member.MemberId];
                Assert.That(member.MaximumHp,
                    Is.EqualTo(source.MaximumHp * expectedHpPercent / 100));
                Assert.That(member.CurrentHp, Is.EqualTo(member.MaximumHp));
                Assert.That(member.Attack, Is.EqualTo(source.Attack),
                    "Encounter pacing must not increase enemy damage.");
                Assert.That(member.MagicAttack, Is.EqualTo(source.MagicAttack),
                    "Encounter pacing must not increase enemy magic damage.");
            }

            var initialEnemyHp = paced.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .Sum(value => value.CurrentHp);
            var openingAllOutBurst = paced.Battle.PlayerUnions
                .Where(value => !value.IsDefeated && !value.Retreated)
                .Sum(union => paced.Battle.CommittedForecasts.Single(value =>
                        value.UnionId == union.UnionId && value.CommandId == "CMD_ALL_OUT")
                    .MemberActions.Sum(value => Math.Max(0, -value.PredictedHpDelta)));
            Assert.That(initialEnemyHp, Is.GreaterThan(openingAllOutBurst),
                "The mandatory encounter must survive the strongest opening order shown to every Union.");

            paced = SelectCommandForEveryActiveUnion(paced, "CMD_ALL_OUT");
            paced = Require(_commands.ConfirmRound(paced, _content));
            Assert.That(paced.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(paced.Battle.RoundRecords.Count, Is.EqualTo(1));
            Assert.That(paced.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .Sum(value => value.CurrentHp),
                Is.InRange(1, initialEnemyHp - 1),
                "Round one must show real HP loss without erasing the encounter.");
            Assert.That(paced.Battle.PlayerUnions.Any(value => !value.IsDefeated && !value.Retreated),
                Is.True, "The longer exchange must preserve a playable founding force.");

            for (var guard = 1;
                 guard < 4 && paced.Battle.Outcome == BattleOutcome.InProgress;
                 guard++)
            {
                paced = SelectCommandForEveryActiveUnion(paced, "CMD_ALL_OUT", "CMD_BALANCED");
                paced = Require(_commands.ConfirmRound(paced, _content));
            }

            Assert.That(paced.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(paced.Battle.RoundRecords.Count, Is.InRange(2, 4));
            TestContext.WriteLine(
                encounterToken + ": " + paced.Battle.RoundRecords.Count +
                " rounds; opening burst " + openingAllOutBurst +
                " into " + initialEnemyHp + " enemy HP.");
        }

        [Test]
        public void GateEaterClimaxHasThreeRoundEnduranceWithoutIncreasingEnemyDamage()
        {
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var roster = resolver.Resolve(
                117,
                "CONTRACT_GATE_EATER",
                "BOARD_FIRST_HOUR",
                M2BattleCommandService.GateEaterBattleToken076,
                3,
                "FIRST_HOUR_GATE_EATER_PACING_078");
            var baseline = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_GATE_ROSTER_BASELINE_078",
                "Measure the unmodified Gate-Eater roster.",
                roster));
            var paced = Require(_commands.StartEncounterBattleWithRoster070(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_CONTRACT_" + M2BattleCommandService.GateEaterBattleToken076 + "_PACING_078",
                "Defeat the Gate-Eater before it reaches Skyhome.",
                roster));

            var baselineMembers = baseline.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .ToDictionary(value => value.MemberId, StringComparer.Ordinal);
            foreach (var member in paced.Battle.EnemyUnions.SelectMany(value => value.Members))
            {
                var source = baselineMembers[member.MemberId];
                var boss = member.ClassId.IndexOf(
                    M2BattleCommandService.GateEaterBossClassToken076,
                    StringComparison.OrdinalIgnoreCase) >= 0;
                var prePacingMaximumHp = boss
                    ? Math.Max(
                        M2BattleCommandService.GateEaterBossMinimumMaximumHp076,
                        source.MaximumHp * 2)
                    : source.MaximumHp;
                Assert.That(member.MaximumHp,
                    Is.EqualTo(prePacingMaximumHp *
                               M2BattleCommandService.GateEaterEnemyHpPercent078 / 100));
                Assert.That(member.Attack,
                    Is.EqualTo(boss
                        ? Math.Max(M2BattleCommandService.GateEaterBossMinimumAttack076, source.Attack)
                        : source.Attack),
                    "Gate-Eater endurance must not add damage beyond its existing boss authority.");
            }

            var initialEnemyHp = paced.Battle.EnemyUnions
                .SelectMany(value => value.Members)
                .Sum(value => value.CurrentHp);
            var openingAllOutBurst = paced.Battle.PlayerUnions
                .Where(value => !value.IsDefeated && !value.Retreated)
                .Sum(union => paced.Battle.CommittedForecasts.Single(value =>
                        value.UnionId == union.UnionId && value.CommandId == "CMD_ALL_OUT")
                    .MemberActions.Sum(value => Math.Max(0, -value.PredictedHpDelta)));
            Assert.That(initialEnemyHp, Is.GreaterThan(openingAllOutBurst * 2),
                "The climax needs more than two opening-burst equivalents before its deadline.");

            paced = SelectCommandForEveryActiveUnion(paced, "CMD_ALL_OUT");
            paced = Require(_commands.ConfirmRound(paced, _content));
            Assert.That(paced.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(paced.Battle.RoundRecords.Count, Is.EqualTo(1));
            Assert.That(paced.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .Sum(value => value.CurrentHp),
                Is.InRange(1, initialEnemyHp - 1));
            Assert.That(paced.Battle.PlayerUnions.Any(value => !value.IsDefeated && !value.Retreated),
                Is.True);
            TestContext.WriteLine(
                "Gate-Eater opening burst " + openingAllOutBurst +
                " into " + initialEnemyHp +
                " enemy HP; round one remained live for the three-round smoke target.");
        }

        [Test]
        public void SecondUnionReceivesDeterministicSideStrikeAndOpensBlindSidePressure()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var lead = campaign.Battle.PlayerUnions[0];
            var wing = campaign.Battle.PlayerUnions[1];
            Assert.That(campaign.Battle.CommittedForecasts.Any(value =>
                value.UnionId == lead.UnionId && value.CommandId == "CMD_FLANK"), Is.False,
                "The lead Union must establish the deadlock instead of receiving the same flank role.");

            var flank = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == wing.UnionId && value.CommandId == "CMD_FLANK");
            Assert.That(flank.CommandName, Is.EqualTo("Side Strike!"));
            Assert.That(flank.MemberActions.All(value =>
                value.Kind == BattleActionKind.Tactical || value.Kind == BattleActionKind.Martial), Is.True);
            Assert.That(flank.ExpectedEffect, Does.Contain("side strike"));
            Assert.That(flank.Risk, Does.Contain("blindside"));

            var leadAttack = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == lead.UnionId && value.CommandId == "CMD_BALANCED");
            campaign = Require(_commands.SelectForecast(campaign, lead.UnionId, leadAttack.ForecastId));
            campaign = Require(_commands.SelectForecast(campaign, wing.UnionId, flank.ForecastId));
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "POSITION_SHIFT" &&
                value.Text.Contains("SIDE STRIKE") && value.Text.Contains("BLIND SIDE")), Is.True);
            Assert.That(campaign.Battle.PlayerUnions.Single(value => value.UnionId == wing.UnionId).Engagement,
                Is.EqualTo(EngagementState.Flanking));
            Assert.That(campaign.Battle.EnemyUnions.Any(value =>
                value.Engagement == EngagementState.RearPressure || value.IsDefeated), Is.True);
        }

        [Test]
        public void ReopeningBattleReturnsSameCommittedForecastSetWithoutReroll()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var before = CanonicalJson.Sha256Hex(campaign.Battle.CommittedForecasts);
            var reopened = Require(_commands.StartTutorialBattle(campaign, _content));

            Assert.That(CanonicalJson.Sha256Hex(reopened.Battle.CommittedForecasts), Is.EqualTo(before));
            Assert.That(M2BattleCommandService.AuthoritativeStateHash(reopened.Battle),
                Is.EqualTo(M2BattleCommandService.AuthoritativeStateHash(campaign.Battle)));
        }

        [Test]
        public void ForecastsEnforceSharedApOnceAndEachMembersPersonalMp()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            foreach (var union in campaign.Battle.PlayerUnions)
            foreach (var forecast in campaign.Battle.CommittedForecasts.Where(value => value.UnionId == union.UnionId))
            {
                Assert.That(forecast.SharedApCost, Is.LessThanOrEqualTo(union.CurrentAp));
                Assert.That(forecast.SharedApCost, Is.EqualTo(forecast.MemberActions.Sum(value => value.SharedApCost)));
                foreach (var action in forecast.MemberActions)
                {
                    var actor = union.Members.Single(value => value.MemberId == action.ActorMemberId);
                    Assert.That(action.PersonalMpCost, Is.LessThanOrEqualTo(actor.CurrentMp), action.ActorMemberId);
                }
            }
        }

        [Test]
        public void StandardCommandSurfaceAcceptsCommittedWholeForecastIdentityOnly()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            var rejected = _commands.SelectForecast(campaign, campaign.Battle.PlayerUnions[0].UnionId, "ART_POWER_CUT");
            Assert.That(rejected.IsSuccess, Is.False);

            var publicMethods = typeof(M2BattleCommandService).GetMethods()
                .Where(value => value.DeclaringType == typeof(M2BattleCommandService))
                .Select(value => value.Name)
                .ToArray();
            Assert.That(publicMethods.Any(value => value.IndexOf("Individual", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(publicMethods.Any(value =>
                (value.StartsWith("Select", StringComparison.OrdinalIgnoreCase) ||
                 value.StartsWith("Choose", StringComparison.OrdinalIgnoreCase) ||
                 value.StartsWith("Set", StringComparison.OrdinalIgnoreCase)) &&
                value.IndexOf("Art", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void SameSeedSelectionsReplayIdenticalEventLogAndStateHash()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_BALANCED");
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var eventHash = CanonicalJson.Sha256Hex(campaign.Battle.EventLog);
            var stateHash = M2BattleCommandService.AuthoritativeStateHash(campaign.Battle);

            var replay = Require(_commands.ReplayTutorialBattle(campaign, _content));

            Assert.That(CanonicalJson.Sha256Hex(replay.Battle.EventLog), Is.EqualTo(eventHash));
            Assert.That(M2BattleCommandService.AuthoritativeStateHash(replay.Battle), Is.EqualTo(stateHash));
            Assert.That(replay.Battle.RoundRecords[0].DeterministicTraceHash,
                Is.EqualTo(campaign.Battle.RoundRecords[0].DeterministicTraceHash));
            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "ART_GROWTH"), Is.True);
            Assert.That(campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Any(value => value.ArtProgress.Count > 0), Is.True);
        }

        [Test]
        public void GuaranteedTutorialBreakthroughCannotFailAfterMeaningfulAssault()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3), _content));
            var candidateMember = campaign.Battle.TutorialBreakthroughMemberId;
            var candidateArt = campaign.Battle.TutorialBreakthroughArtId;
            var candidateUnion = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == candidateMember));
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == candidateUnion.UnionId && value.CommandId == "CMD_ALL_OUT");
            var sourceAction = forecast.MemberActions.Single(value => value.ActorMemberId == candidateMember);

            Assert.That(sourceAction.BreakthroughOpportunity, Is.True);
            Assert.That(sourceAction.ArtId, Is.Not.EqualTo(candidateArt), "An unlearned Art must not execute before breakthrough.");
            Assert.That(sourceAction.BreakthroughTargetArtId, Is.EqualTo(candidateArt));
            Assert.That(sourceAction.Discipline == "Martial" || sourceAction.Discipline == "Tactical", Is.True);
            Assert.That(forecast.LearningOpportunity, Does.Contain(sourceAction.ArtName));
            Assert.That(forecast.LearningOpportunity, Does.Contain(sourceAction.BreakthroughTargetArtName));

            campaign = Require(_commands.SelectForecast(campaign, candidateUnion.UnionId, forecast.ForecastId));
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            Assert.That(campaign.Battle.TutorialBreakthroughOccurred, Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "BREAKTHROUGH"), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "ART_GROWTH" && value.ArtId == sourceAction.ArtId), Is.True);
            var learned = campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == candidateMember);
            Assert.That(learned.DiscoveryProgress, Is.EqualTo(100));
            Assert.That(learned.LearnedArtIds, Does.Contain(candidateArt));
            var sourceProgress = learned.ArtProgress.Single(value => value.ArtId == sourceAction.ArtId);
            Assert.That(sourceProgress.MeaningfulUses, Is.EqualTo(1));
            Assert.That(sourceProgress.MasteryPoints, Is.GreaterThan(0));
            Assert.That(learned.ArtProgress.Single(value => value.ArtId == candidateArt).MeaningfulUses, Is.Zero);
            Assert.That(learned.ArtProgress.Select(value => value.ArtId).ToArray(),
                Is.EqualTo(learned.ArtProgress.Select(value => value.ArtId).OrderBy(value => value, StringComparer.Ordinal).ToArray()));
            Assert.That(campaign.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            var nextCombatForecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == candidateUnion.UnionId && value.CommandId == "CMD_ALL_OUT");
            Assert.That(nextCombatForecast.MemberActions.Single(value => value.ActorMemberId == candidateMember).ArtId,
                Is.EqualTo(candidateArt));
            Assert.That(campaign.Battle.CommittedForecasts.Any(value =>
                DebugEvidenceContainsCandidate(value.DeterministicDebugEvidence, candidateMember, candidateArt)), Is.True,
                "The newly learned Art must enter a later committed legal pool.");

            for (var recruitIndex = 0; recruitIndex < 6; recruitIndex++)
            {
                var singleMemberCampaign = Require(_commands.StartTutorialBattle(
                    CreateSingleRecruitCampaign(recruitIndex), _content));
                var singleMemberId = singleMemberCampaign.Battle.TutorialBreakthroughMemberId;
                var singleUnion = singleMemberCampaign.Battle.PlayerUnions.Single();
                var learningCommand = singleMemberCampaign.Battle.CommittedForecasts.Single(value =>
                    value.UnionId == singleUnion.UnionId && value.MemberActions.Any(action =>
                        action.ActorMemberId == singleMemberId && action.BreakthroughOpportunity));
                if (learningCommand.MemberActions.All(value => value.ArtId.StartsWith("ART_BASIC_", StringComparison.Ordinal)))
                    Assert.That(learningCommand.CommandName, Is.Not.EqualTo("Attack Using Combat Arts!"),
                        "A wholly fundamental plan must use an honest command label.");

                singleMemberCampaign = Require(_commands.SelectForecast(
                    singleMemberCampaign, singleUnion.UnionId, learningCommand.ForecastId));
                singleMemberCampaign = Require(_commands.ConfirmRound(singleMemberCampaign, _content));

                Assert.That(singleMemberCampaign.Battle.TutorialBreakthroughOccurred, Is.True,
                    "Every legal one-member class plan must retain its guaranteed meaningful-use breakthrough: recruit " + recruitIndex);
                Assert.That(singleMemberCampaign.Battle.PlayerUnions.Single().Members.Single().LearnedArtIds,
                    Does.Contain(singleMemberCampaign.Battle.TutorialBreakthroughArtId));
            }
        }

        [Test]
        public void MeaningfulUseProgressesMoreThanHarmlessSpam()
        {
            var meaningfulAttack = M2MeaningfulUse.PersonalProgressGain(BattleActionKind.Martial, true, -40);
            var harmlessAttack = M2MeaningfulUse.PersonalProgressGain(BattleActionKind.Martial, false, 0);
            var meaningfulGuard = M2MeaningfulUse.PersonalProgressGain(BattleActionKind.Guard, true, 0);
            var harmlessGuard = M2MeaningfulUse.PersonalProgressGain(BattleActionKind.Guard, false, 0);

            Assert.That(meaningfulAttack, Is.GreaterThan(harmlessAttack));
            Assert.That(meaningfulGuard, Is.GreaterThan(harmlessGuard));
            Assert.That(harmlessGuard, Is.Zero);
            Assert.That(M2MeaningfulUse.PersonalProgressGain(BattleActionKind.Recovery, false, 0), Is.Zero);
            Assert.That(M2MeaningfulUse.UnionProgressGain(true), Is.GreaterThan(M2MeaningfulUse.UnionProgressGain(false)));
        }

        [Test]
        public void RecoveryAppearsOnlyWhenUsefulAndDoesNotFakePersonalArtGrowth()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateRecoveryCampaign(), _content));
            Assert.That(campaign.Battle.CommittedForecasts.Any(value => value.CommandId == "CMD_AP_RECOVERY"), Is.False);
            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT");
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            Assert.That(campaign.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.PlayerUnions.Single().CurrentAp,
                Is.LessThan(campaign.Battle.PlayerUnions.Single().MaximumAp),
                "The fixture must spend more shared AP than passive round recovery restores.");
            Assert.That(campaign.Battle.CommittedForecasts.Any(value => value.CommandId == "CMD_AP_RECOVERY"), Is.True);
            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_AP_RECOVERY");
            campaign = Require(_commands.ConfirmRound(campaign, _content));

            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "AP_RECOVERY" && value.Amount > 0), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value =>
                value.EventType == "ART_GROWTH" && value.ArtId == "ART_RECOVER_BREATH"), Is.False);
            Assert.That(campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .SelectMany(value => value.ArtProgress)
                .Any(value => value.ArtId == "ART_RECOVER_BREATH"), Is.False);
        }

        [Test]
        public void Update010BattleEnvelopeVerifiesRawHashBeforeArtGrowthDefaults()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3), _content));
            var legacyStateJson = RemoveJsonProperties(
                CanonicalJson.Serialize(campaign),
                "ArtProgress",
                "Discipline",
                "PredictedGrowth",
                "BreakthroughTargetArtId",
                "BreakthroughTargetArtName",
                "ActorUnionId",
                "ActorMemberId",
                "TargetUnionId",
                "TargetMemberId");
            var envelopeJson = "{" +
                "\"SaveFormatVersion\":3," +
                "\"ContentAuthorityVersion\":" + CanonicalJson.Serialize(campaign.ContentAuthorityVersion) + "," +
                "\"CampaignGuid\":" + CanonicalJson.Serialize(campaign.CampaignGuid) + "," +
                "\"CampaignSeed\":" + CanonicalJson.Serialize(campaign.CampaignSeed) + "," +
                "\"DisplayTimestampUtc\":\"1970-01-01T00:00:00.0000000Z\"," +
                "\"CampaignState\":" + legacyStateJson + "," +
                "\"CanonicalStateHash\":" + CanonicalJson.Serialize(Sha256HexUtf8(legacyStateJson)) + "," +
                "\"MigrationHistory\":[\"save_v2_to_v3_m2_battle_defaults\"]" +
                "}";
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionM2LegacySaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "update010.json");
            try
            {
                File.WriteAllText(path, envelopeJson);
                var loadedEnvelope = new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loadedEnvelope.IsSuccess, Is.True, string.Join("\n", loadedEnvelope.Errors));
                Assert.That(loadedEnvelope.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
                Assert.That(loadedEnvelope.Value.MigrationHistory,
                    Contains.Item("save_v3_to_v4_m2_art_growth_defaults"));
                Assert.That(loadedEnvelope.Value.MigrationHistory,
                    Contains.Item("save_v4_to_v5_progression_foundation_defaults"));
                var loaded = loadedEnvelope.Value.CampaignState.Battle;
                Assert.That(loaded.PlayerUnions.SelectMany(value => value.Members).All(value => value.ArtProgress.Count == 0), Is.True);
                Assert.That(loaded.CommittedForecasts.SelectMany(value => value.MemberActions).All(value =>
                value.Discipline == string.Empty && value.PredictedGrowth == 0 &&
                value.BreakthroughTargetArtId == string.Empty && value.BreakthroughTargetArtName == string.Empty), Is.True);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void DownedIsARecoverableStateAndNeverADeathRecord()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            for (var round = 0; round < 8 && campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
                campaign = Require(_commands.ConfirmRound(campaign, _content));
            }

            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "DOWNED"), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value => value.Text.IndexOf("not dead", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Assert.That(typeof(BattleMemberState).GetProperty("Downed"), Is.Not.Null);
            Assert.That(typeof(BattleMemberState).GetProperty("Stabilized"), Is.Not.Null);
            Assert.That(typeof(BattleMemberState).GetProperty("Dead"), Is.Null);
        }

        [Test]
        public void ResolvedBattleResultIsPartOfTheFinalImmutablePresentationRound()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            for (var round = 0; round < 12 && campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
                campaign = Require(_commands.ConfirmRound(campaign, _content));
            }

            Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.RoundRecords, Is.Not.Empty);
            Assert.That(campaign.Battle.RoundRecords.Last().Events.Any(value => value.EventType == "BATTLE_RESULT"),
                Is.True, "The cinematic queue must receive victory, defeat, or retreat in the same immutable final round.");
        }

        [Test]
        public void Pass03RewardsUseExplicitPositiveTerminalMultipliers()
        {
            Assert.That(_content.TutorialEnemyUnion.RewardMultiplierPermille, Is.EqualTo(1000));
            Assert.That(_content.TutorialEnemyUnion.MemberIds.Sum(value => _content.Enemy(value).PersonalXpReward),
                Is.EqualTo(42));
            Assert.That(_content.TutorialEnemyUnion.MemberIds.Sum(value => _content.Enemy(value).GuildTreasuryXpReward),
                Is.EqualTo(24));
            Assert.That(M2ProgressionRewards.OutcomeMultiplierPermille(BattleOutcome.Victory), Is.EqualTo(1000));
            Assert.That(M2ProgressionRewards.OutcomeMultiplierPermille(BattleOutcome.Retreat), Is.EqualTo(350));
            Assert.That(M2ProgressionRewards.OutcomeMultiplierPermille(BattleOutcome.Defeat), Is.EqualTo(200));
            Assert.That(M2ProgressionRewards.ScalePositive(42, 1000, 1000, 100), Is.EqualTo(42));
            Assert.That(M2ProgressionRewards.ScalePositive(42, 1000, 350, 100), Is.EqualTo(14));
            Assert.That(M2ProgressionRewards.ScalePositive(42, 1000, 200, 100), Is.EqualTo(8));
            Assert.That(M2ProgressionRewards.ScalePositive(42, 1000, 200, 0), Is.EqualTo(1),
                "Even a zero-percent custom mode cannot turn a terminal training outcome into zero XP.");
        }

        [Test]
        public void RecruitAndGuildProgressionFoundationsAreDeterministic()
        {
            var progression = RecruitProgressionState.Default().GainPersonalXp(300, "CLASS_WARRIOR");

            Assert.That(progression.Level, Is.EqualTo(3));
            Assert.That(progression.TotalPersonalXp, Is.EqualTo(300));
            Assert.That(progression.MaximumHpBonus, Is.EqualTo(12));
            Assert.That(progression.MaximumMpBonus, Is.EqualTo(4));
            Assert.That(progression.StrengthBonus, Is.EqualTo(4));
            Assert.That(progression.DefenseBonus, Is.EqualTo(2));
            Assert.That(RecruitProgressionRules021.TotalXpRequiredForLevel(2), Is.EqualTo(100));
            Assert.That(RecruitProgressionRules021.TotalXpRequiredForLevel(3), Is.EqualTo(300));

            var development = GuildDevelopmentState.Default();
            Assert.That(development.HallStageIndex, Is.Zero);
            Assert.That(development.HallStageId, Is.EqualTo("HALL_STAGE_RUINED_ANNEX"));
            Assert.That(development.Facilities.Count, Is.EqualTo(31));
            Assert.That(development.Facilities.All(value => value.Level == 0 && value.TotalFacilityXp == 0), Is.True);
            development = development.RecordTreasuryReward("TEST_REWARD_GUILD_LEVEL", 200);
            Assert.That(development.LifetimeTreasuryXpEarned, Is.EqualTo(200));
            Assert.That(development.HallEnhancementXp, Is.EqualTo(200));
            Assert.That(development.GuildLevel, Is.EqualTo(2));
            Assert.That(GuildProgressionRules021.TotalXpRequiredForLevel(2), Is.EqualTo(200));
        }

        [Test]
        public void PendingBattleEquipmentIdentityIsDeterministicAndPartOfAuthorityHash()
        {
            var first = ResolveTutorialBattle(CreateCampaign(3, 3));
            var second = ResolveTutorialBattle(CreateCampaign(3, 3));
            var reward = first.Battle.Reward;
            var item = reward.EquipmentReward;

            Assert.That(item, Is.Not.Null);
            Assert.That(item.InstanceId, Does.StartWith("BATTLE_ITEM_"));
            Assert.That(item.DefinitionId, Is.EqualTo(M2ProgressionRewards.EquipmentRewardDefinitionId));
            Assert.That(item.DisplayName, Is.EqualTo(M2ProgressionRewards.EquipmentRewardDisplayName));
            Assert.That(item.ValidSlotIds, Is.EqualTo(new[] { EquipmentSlotIds.MainHand }));
            Assert.That(item.EquipmentTags, Is.EqualTo(new[] { "SWORD", "WEAPON" }));
            Assert.That(item.PlayerLocked, Is.False);
            Assert.That(second.Battle.Reward.EquipmentReward.InstanceId, Is.EqualTo(item.InstanceId));
            Assert.That(second.Battle.Reward.RewardId, Is.EqualTo(reward.RewardId));

            var alteredItem = new EquipmentItemState(
                item.InstanceId,
                item.DefinitionId,
                item.DisplayName + " Altered",
                item.ValidSlotIds,
                item.EquipmentTags,
                item.QualityId,
                item.ConditionBasisPoints,
                item.PlayerLocked);
            var alteredReward = new BattleRewardState(
                reward.RewardId,
                reward.RewardRulesVersion,
                reward.Outcome,
                reward.BasePersonalXpPerMember,
                reward.BaseGuildTreasuryXp,
                reward.EnemyUnionMultiplierPermille,
                reward.OutcomeMultiplierPermille,
                reward.PersonalXpModePercent,
                reward.TreasuryXpModePercent,
                reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward,
                reward.MemberRewards,
                reward.Claimed,
                alteredItem);
            Assert.That(
                M2BattleCommandService.AuthoritativeStateHash(first.Battle.WithReward(alteredReward)),
                Is.Not.EqualTo(M2BattleCommandService.AuthoritativeStateHash(first.Battle)));

            var legacyCompatibleReward = new BattleRewardState(
                reward.RewardId,
                "M2_REWARDS_021_V1",
                reward.Outcome,
                reward.BasePersonalXpPerMember,
                reward.BaseGuildTreasuryXp,
                reward.EnemyUnionMultiplierPermille,
                reward.OutcomeMultiplierPermille,
                reward.PersonalXpModePercent,
                reward.TreasuryXpModePercent,
                reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward,
                reward.MemberRewards,
                reward.Claimed);
            Assert.That(CanonicalJson.Serialize(legacyCompatibleReward),
                Does.Not.Contain("\"EquipmentReward\""),
                "A missing item must remain omitted so pre-repair save-v11 hashes stay readable.");
        }

        [Test]
        public void FirstHourBattlesGrantThreeRealEquipmentProfilesThatProjectAndClaimExactly()
        {
            var profiles = new[]
            {
                new
                {
                    BattleId = "BATTLE_FIRST_HOUR072_HALL_BREACH",
                    DefinitionId = M2ProgressionRewards.EquipmentRewardDefinitionId,
                    DisplayName = M2ProgressionRewards.EquipmentRewardDisplayName,
                    SlotId = EquipmentSlotIds.MainHand,
                    Tags = new[] { "SWORD", "WEAPON" }
                },
                new
                {
                    BattleId = "BATTLE_CONTRACT_ENCOUNTER071_LANTERN_ROAD_AMBUSH_TEST",
                    DefinitionId = M2ProgressionRewards.LanternRoadEquipmentRewardDefinitionId,
                    DisplayName = M2ProgressionRewards.LanternRoadEquipmentRewardDisplayName,
                    SlotId = EquipmentSlotIds.MainHand,
                    Tags = new[] { "SPEAR", "WEAPON" }
                },
                new
                {
                    BattleId = "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER_TEST",
                    DefinitionId = M2ProgressionRewards.GateEaterEquipmentRewardDefinitionId,
                    DisplayName = M2ProgressionRewards.GateEaterEquipmentRewardDisplayName,
                    SlotId = EquipmentSlotIds.OffHand,
                    Tags = new[] { "SHIELD", "ARMOR" }
                }
            };
            var rewardIds = new HashSet<string>(StringComparer.Ordinal);
            var definitionIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var profile in profiles)
            {
                var pending = ResolveEncounterBattle(CreateCampaign(3, 3), profile.BattleId);
                var reward = pending.Battle.Reward;
                var item = reward.EquipmentReward;
                Assert.That(reward.RewardRulesVersion, Is.EqualTo(M2ProgressionRewards.RewardRulesVersion));
                Assert.That(item.DefinitionId, Is.EqualTo(profile.DefinitionId));
                Assert.That(item.DisplayName, Is.EqualTo(profile.DisplayName));
                Assert.That(item.ValidSlotIds, Is.EqualTo(new[] { profile.SlotId }));
                Assert.That(item.EquipmentTags, Is.EqualTo(profile.Tags));
                Assert.That(item.QualityId, Is.EqualTo("QUALITY_STANDARD"));
                Assert.That(rewardIds.Add(reward.RewardId), Is.True);
                Assert.That(definitionIds.Add(item.DefinitionId), Is.True);

                var rebuilt = M2ProgressionRewards.CreatePending(pending, pending.Battle, _content);
                Assert.That(rebuilt.RewardId, Is.EqualTo(reward.RewardId),
                    "The selected equipment profile must participate deterministically in the reward hash.");
                Assert.That(rebuilt.EquipmentReward.InstanceId, Is.EqualTo(item.InstanceId));

                var savePath = Path.Combine(Path.GetTempPath(),
                    "sdg_first_hour_reward_profile_" + Guid.NewGuid().ToString("N") + ".json");
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                var campaignField = typeof(M1RuntimeCoordinator).GetField(
                    "_campaign",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(campaignField, Is.Not.Null);
                campaignField.SetValue(coordinator, pending);
                var projected = coordinator.State.Battle.Reward;
                Assert.That(projected.EquipmentRewardDefinitionId, Is.EqualTo(profile.DefinitionId));
                Assert.That(projected.EquipmentRewardDisplayName, Is.EqualTo(profile.DisplayName));
                Assert.That(projected.EquipmentRewardValidSlotIds, Is.EqualTo(new[] { profile.SlotId }));

                var claimed = Require(_commands.ClaimBattleRewards(pending));
                var claimedItem = claimed.Guild.Inventory.Single(value => value.InstanceId == item.InstanceId);
                Assert.That(claimedItem.DefinitionId, Is.EqualTo(profile.DefinitionId));
                Assert.That(claimedItem.DisplayName, Is.EqualTo(profile.DisplayName));
                Assert.That(claimedItem.ValidSlotIds, Is.EqualTo(new[] { profile.SlotId }));
                Assert.That(claimedItem.EquipmentTags, Is.EqualTo(profile.Tags));
                var repeated = Require(_commands.ClaimBattleRewards(claimed));
                Assert.That(repeated.Guild.Inventory.Count(value => value.InstanceId == item.InstanceId),
                    Is.EqualTo(1), "A projected first-hour equipment profile must still claim exactly once.");
            }

            Assert.That(definitionIds.Count, Is.EqualTo(3),
                "Hall Breach, Lantern Road, and Gate-Eater must not collapse to one placeholder item.");
        }

        [Test]
        public void BattleRewardReadModelExposesExactGenericEquipmentIdentity()
        {
            var pending = ResolveTutorialBattle(CreateCampaign(3, 3));
            var reward = pending.Battle.Reward;
            var authoritativeItem = new EquipmentItemState(
                "BATTLE_ITEM_FUTURE_TEST_099",
                "LOOT_FUTURE_TEST_099",
                "Future-Test Star Compass",
                new[] { EquipmentSlotIds.ToolRelic },
                new[] { "RELIC", "COMPASS" },
                "QUALITY_MASTERWORK",
                8765,
                false);
            var futureReward = new BattleRewardState(
                reward.RewardId,
                reward.RewardRulesVersion,
                reward.Outcome,
                reward.BasePersonalXpPerMember,
                reward.BaseGuildTreasuryXp,
                reward.EnemyUnionMultiplierPermille,
                reward.OutcomeMultiplierPermille,
                reward.PersonalXpModePercent,
                reward.TreasuryXpModePercent,
                reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward,
                reward.MemberRewards,
                reward.Claimed,
                authoritativeItem);
            pending = pending.WithBattle(pending.Battle.WithReward(futureReward));
            var savePath = Path.Combine(Path.GetTempPath(),
                "sdg_reward_projection_" + Guid.NewGuid().ToString("N") + ".json");
            var coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                savePath);
            var campaignField = typeof(M1RuntimeCoordinator).GetField(
                "_campaign",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(campaignField, Is.Not.Null);
            campaignField.SetValue(coordinator, pending);

            var view = coordinator.State.Battle.Reward;

            Assert.That(view.EquipmentRewardDisplayName, Is.EqualTo(authoritativeItem.DisplayName));
            Assert.That(view.EquipmentRewardInstanceId, Is.EqualTo(authoritativeItem.InstanceId));
            Assert.That(view.EquipmentRewardDefinitionId, Is.EqualTo(authoritativeItem.DefinitionId));
            Assert.That(view.EquipmentRewardQualityId, Is.EqualTo(authoritativeItem.QualityId));
            Assert.That(view.EquipmentRewardValidSlotIds, Is.EqualTo(authoritativeItem.ValidSlotIds));
        }

        [Test]
        public void EquipmentRewardClaimAddsOneInventoryInstanceExactlyOnce()
        {
            var pending = ResolveTutorialBattle(CreateCampaign(3, 3));
            var itemId = pending.Battle.Reward.EquipmentReward.InstanceId;

            var claimed = Require(_commands.ClaimBattleRewards(pending));
            Assert.That(claimed.Guild.Inventory.Count(value => value.InstanceId == itemId), Is.EqualTo(1));

            var repeated = Require(_commands.ClaimBattleRewards(claimed));
            Assert.That(repeated.Guild.Inventory.Count(value => value.InstanceId == itemId), Is.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(repeated), Is.EqualTo(CanonicalJson.Sha256Hex(claimed)));

            var staleReceipt = claimed.WithBattle(pending.Battle);
            var ledgerProtected = Require(_commands.ClaimBattleRewards(staleReceipt));
            Assert.That(ledgerProtected.Guild.Inventory.Count(value => value.InstanceId == itemId), Is.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(ledgerProtected), Is.EqualTo(CanonicalJson.Sha256Hex(claimed)));
        }

        [Test]
        public void EquipmentRewardClaimNeverAutoEquipsTheItem()
        {
            var pending = ResolveTutorialBattle(CreateCampaign(3, 3));
            var itemId = pending.Battle.Reward.EquipmentReward.InstanceId;

            var claimed = Require(_commands.ClaimBattleRewards(pending));

            Assert.That(claimed.Guild.Inventory.Any(value => value.InstanceId == itemId), Is.True);
            Assert.That(claimed.Guild.Recruits.SelectMany(value => value.Equipment.Assignments)
                .Any(value => value.Item.InstanceId == itemId), Is.False);
        }

        [Test]
        public void ClaimedEquipmentRewardCanBeManuallyEquippedToLegalNormalRecruit()
        {
            var pending = ResolveTutorialBattle(CreateCampaign(3, 3));
            var item = pending.Battle.Reward.EquipmentReward;
            var claimed = Require(_commands.ClaimBattleRewards(pending));
            var recruit = claimed.Guild.Recruits.First(value => value.AuthorityKind == RecruitAuthorityKind.Normal);
            var previousMainHand = recruit.Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId;

            var equipped = Require(new M1CommandService().EquipItem(
                claimed,
                recruit.RecruitId,
                EquipmentSlotIds.MainHand,
                item.InstanceId));

            Assert.That(equipped.Guild.Inventory.Any(value => value.InstanceId == item.InstanceId), Is.False);
            Assert.That(equipped.Guild.Inventory.Any(value => value.InstanceId == previousMainHand), Is.True);
            Assert.That(equipped.Guild.Recruits.Single(value => value.RecruitId == recruit.RecruitId)
                .Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId, Is.EqualTo(item.InstanceId));
            Assert.That(equipped.OpeningFlow.ManualEquipmentCommitObserved, Is.True);
        }

        [Test]
        public void TerminalRewardClaimPersistsXpArtsAndTreasuryExactlyOnce()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            for (var round = 0; round < 12 && campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
                campaign = Require(_commands.ConfirmRound(campaign, _content));
            }

            Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.Reward, Is.Not.Null);
            Assert.That(campaign.Battle.Reward.Claimed, Is.False);
            Assert.That(campaign.Battle.Reward.BasePersonalXpPerMember, Is.EqualTo(42));
            Assert.That(campaign.Battle.Reward.BaseGuildTreasuryXp, Is.EqualTo(24));
            Assert.That(campaign.Battle.Reward.MemberRewards.All(value => value.PersonalXp > 0), Is.True);
            Assert.That(campaign.Battle.Reward.GuildTreasuryXpAward, Is.GreaterThan(0));
            Assert.That(campaign.Battle.Reward.HallEnhancementXpAward, Is.GreaterThan(0));
            var authoritativeBeforeClaim = M2BattleCommandService.AuthoritativeStateHash(campaign.Battle);
            var canonicalBeforeClaim = CanonicalJson.Sha256Hex(campaign);
            var treasuryBefore = campaign.Guild.TreasuryXp;
            var rewardId = campaign.Battle.Reward.RewardId;
            var award = campaign.Battle.Reward.GuildTreasuryXpAward;
            var hallAward = campaign.Battle.Reward.HallEnhancementXpAward;

            var claimed = Require(_commands.ClaimBattleRewards(campaign));

            Assert.That(claimed.Battle.Reward.Claimed, Is.True);
            Assert.That(claimed.Guild.TreasuryXp, Is.EqualTo(treasuryBefore + award));
            Assert.That(claimed.Guild.Development.LifetimeTreasuryXpEarned, Is.EqualTo(award));
            Assert.That(claimed.Guild.Development.HallEnhancementXp, Is.EqualTo(hallAward));
            Assert.That(claimed.Guild.Development.GuildLevel, Is.GreaterThanOrEqualTo(1));
            Assert.That(claimed.Guild.Development.ClaimedBattleRewardIds, Does.Contain(rewardId));
            Assert.That(claimed.Guild.Recruits.All(value => value.Progression.TotalPersonalXp > 0), Is.True);
            Assert.That(claimed.Guild.Recruits.Any(value => value.Progression.ArtMastery.Count > 0), Is.True);
            Assert.That(claimed.Guild.Recruits.Any(value => value.Progression.LearnedArtIds.Count > 0), Is.True);
            Assert.That(M2BattleCommandService.AuthoritativeStateHash(claimed.Battle),
                Is.EqualTo(authoritativeBeforeClaim), "Claim metadata cannot mutate replay authority.");
            Assert.That(CanonicalJson.Sha256Hex(claimed), Is.Not.EqualTo(canonicalBeforeClaim));

            var claimedAgain = Require(_commands.ClaimBattleRewards(claimed));
            Assert.That(CanonicalJson.Sha256Hex(claimedAgain), Is.EqualTo(CanonicalJson.Sha256Hex(claimed)));
            var staleUnclaimedReceipt = claimed.WithBattle(campaign.Battle);
            var ledgerProtected = Require(_commands.ClaimBattleRewards(staleUnclaimedReceipt));
            Assert.That(CanonicalJson.Sha256Hex(ledgerProtected), Is.EqualTo(CanonicalJson.Sha256Hex(claimed)),
                "The persisted reward ledger must reject a stale duplicate receipt without double-paying it.");
            Assert.That(_commands.ReplayTutorialBattle(claimed, _content).IsSuccess, Is.False,
                "Replay after campaign mutation must fail instead of comparing unlike starting rosters.");

            var restarted = Require(_commands.RestartTutorialBattle(claimed, _content));
            var persistedArt = claimed.Guild.Recruits.SelectMany(value => value.Progression.ArtMastery)
                .OrderByDescending(value => value.MasteryPoints)
                .First();
            var restartedMember = restarted.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.ArtProgress.Any(art => art.ArtId == persistedArt.ArtId));
            Assert.That(restartedMember.ArtProgress.Single(value => value.ArtId == persistedArt.ArtId).MasteryPoints,
                Is.EqualTo(persistedArt.MasteryPoints));
        }

        [Test]
        public void StartingAnotherBattleRequiresManualClaimOfResolvedReward()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            for (var guard = 0; guard < 20 && campaign.Battle.Outcome == BattleOutcome.InProgress; guard++)
            {
                campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
                campaign = Require(_commands.ConfirmRound(campaign, _content));
            }

            Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.Reward, Is.Not.Null);
            Assert.That(campaign.Battle.Reward.Claimed, Is.False);

            var blocked = _commands.StartEncounterBattle(
                campaign,
                _content,
                "BATTLE_NEXT_CONTRACT_017D",
                "Do not silently claim the previous reward.",
                1);

            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors, Does.Contain("M2_UNCLAIMED_REWARD_REQUIRED"));
            Assert.That(campaign.Battle.Reward.Claimed, Is.False,
                "Starting another encounter must leave the committed reward under explicit player control.");

            var claimed = Require(_commands.ClaimBattleRewards(campaign));
            var next = Require(_commands.StartEncounterBattle(
                claimed.WithBattle(null),
                _content,
                "BATTLE_NEXT_CONTRACT_017D",
                "Begin only after the previous reward is claimed.",
                1));
            Assert.That(next.Battle.BattleId, Is.EqualTo("BATTLE_NEXT_CONTRACT_017D"));
        }

        [Test]
        public void ActiveBattleIsIdempotentOnlyForTheExactBattleId()
        {
            var active = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_EXACT_ID_A_069",
                "Protect the rescue corridor.",
                1));
            var before = CanonicalJson.Sha256Hex(active);

            var exactRetry = Require(_commands.StartEncounterBattle(
                active,
                _content,
                "BATTLE_EXACT_ID_A_069",
                "Protect the rescue corridor.",
                1));
            Assert.That(CanonicalJson.Sha256Hex(exactRetry), Is.EqualTo(before));

            var wrongBattle = _commands.StartEncounterBattle(
                active,
                _content,
                "BATTLE_EXACT_ID_B_069",
                "A different encounter must not replace the active one.",
                1);
            Assert.That(wrongBattle.IsSuccess, Is.False);
            Assert.That(wrongBattle.Errors, Does.Contain("M2_ACTIVE_BATTLE_MUST_FINISH"));
            Assert.That(active.Battle.BattleId, Is.EqualTo("BATTLE_EXACT_ID_A_069"));
        }

        [Test]
        public void ExpeditionEncounterSupportsMultipleEnemyUnionsAndRouteModifiers()
        {
            var campaign = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_CONTRACT_RESCUE_TEST",
                "Rescue the trapped civilians.",
                4,
                new[] { "SCOUTED_APPROACH", "HIGH_FATIGUE", "URGENT_OBJECTIVE" }));

            Assert.That(campaign.Battle.EnemyUnions.Count, Is.EqualTo(4));
            Assert.That(campaign.Battle.EnemyUnions.Select(value => value.UnionId).Distinct().Count(), Is.EqualTo(4));
            Assert.That(campaign.Battle.EnemyUnions.SelectMany(value => value.Members)
                .Select(value => value.MemberId).Distinct().Count(),
                Is.EqualTo(campaign.Battle.EnemyUnions.SelectMany(value => value.Members).Count()));
            Assert.That(campaign.Battle.PlayerUnions[0].Engagement, Is.EqualTo(EngagementState.Flanking));
            Assert.That(campaign.Battle.PlayerUnions.All(value => value.CurrentAp == value.MaximumAp - 3), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "ROUTE_ADVANTAGE"), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "ROUTE_COST"), Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value => value.EventType == "OBJECTIVE_URGENCY"), Is.True);
            Assert.That(campaign.Battle.TutorialBreakthroughMemberId, Is.Empty);
            Assert.That(campaign.Battle.TutorialBreakthroughArtId, Is.Empty);
        }

        [Test]
        public void ContractEncounterUsesExpeditionResultLanguageInsteadOfTutorialText()
        {
            var campaign = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_CONTRACT_RESULT_LANGUAGE_017D",
                "Clear the contracted encounter.",
                1));
            for (var guard = 0; guard < 20 && campaign.Battle.Outcome == BattleOutcome.InProgress; guard++)
            {
                campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
                campaign = Require(_commands.ConfirmRound(campaign, _content));
            }

            Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));
            var result = campaign.Battle.EventLog.Last(value => value.EventType == "BATTLE_RESULT");
            Assert.That(result.Text, Does.Not.Contain("Training projection"));
            Assert.That(result.Text, Does.Not.Contain("tutorial"));
            Assert.That(result.Text, Does.Contain("Expedition command"));
        }

        [Test]
        public void ExpeditionEncounterCanDeployTenSelectedAlliedUnionsAgainstTenEnemyUnions()
        {
            var source = CreateTenUnionCampaign();
            var alliedIds = source.Guild.Unions.Select(value => value.UnionId).ToArray();
            var campaign = Require(_commands.StartEncounterBattle(
                source,
                _content,
                "BATTLE_TWENTY_UNION_CAPACITY_017D",
                "Prove ten allied and ten enemy Union capacity.",
                10,
                new[] { "SCOUTED_APPROACH" },
                alliedIds));

            Assert.That(campaign.Battle.PlayerUnions.Count, Is.EqualTo(10));
            Assert.That(campaign.Battle.EnemyUnions.Count, Is.EqualTo(10));
            Assert.That(campaign.Battle.PlayerUnions.Select(value => value.UnionId), Is.EquivalentTo(alliedIds));
            Assert.That(campaign.Battle.PlayerUnions.All(value => value.Members.Count == 6), Is.True);
            Assert.That(campaign.Battle.PlayerUnions.All(value => value.FormationBenefitActive), Is.True,
                "Every six-member Shield Wall must receive the newly authoritative formation benefit.");
            Assert.That(campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Select(value => value.MemberId).Distinct().Count(), Is.EqualTo(60));
            Assert.That(campaign.Battle.EnemyUnions.Select(value => value.UnionId).Distinct().Count(), Is.EqualTo(10));
            Assert.That(campaign.Battle.CommittedForecasts.Select(value => value.UnionId).Distinct().Count(),
                Is.EqualTo(10));
            Assert.That(campaign.Battle.CommittedForecasts
                .Where(value => alliedIds.Contains(value.UnionId))
                .All(value => value.MemberActions.Count == 6), Is.True,
                "A complete Union order must account for all six deployed members.");

            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_BALANCED");
            Assert.That(campaign.Battle.Selections.Select(value => value.UnionId),
                Is.EquivalentTo(alliedIds));
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var resolvedRound = campaign.Battle.RoundRecords.Last();
            Assert.That(resolvedRound.Selections.Select(value => value.UnionId),
                Is.EquivalentTo(alliedIds),
                "All ten complete six-member forecasts must pass round validation and resolution.");
            Assert.That(campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Select(value => value.MemberId).Distinct().Count(), Is.EqualTo(60));
        }

        [Test]
        public void BattleStateRoundTripsThroughCurrentAtomicSave()
        {
            var campaign = Require(_commands.StartTutorialBattle(CreateCampaign(3, 3), _content));
            campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionM2SaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "battle.json");
            try
            {
                new AtomicSaveStore().Write(path, SaveEnvelopeV1.Create(
                    campaign, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
                Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState.Battle),
                    Is.EqualTo(CanonicalJson.Sha256Hex(campaign.Battle)));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LevelTenArtsStrengthenNewBattleDamageAndHealingWithoutOverheal088()
        {
            const string basicSwordArt = "ART_BASIC_SABER_CUT";
            var lowDamage = Require(_commands.StartEncounterBattle(
                CreateCampaign(3, 3),
                _content,
                "BATTLE_ART_LEVEL_DAMAGE_088",
                "Compare deterministic Art mastery damage.",
                1));
            var highDamageCampaign = ConfigureRecruitArtMastery088(
                CreateCampaign(3, 3),
                "RECRUIT_1",
                basicSwordArt,
                _content.Art(basicSwordArt).Discipline,
                4700);
            var highDamage = Require(_commands.StartEncounterBattle(
                highDamageCampaign,
                _content,
                "BATTLE_ART_LEVEL_DAMAGE_088",
                "Compare deterministic Art mastery damage.",
                1));
            var lowDamageAction = lowDamage.Battle.CommittedForecasts
                .Single(value => value.UnionId == "UNION_OPENING_01" &&
                                 value.CommandId == "CMD_BALANCED")
                .MemberActions.Single(value => value.ActorMemberId == "RECRUIT_1");
            var highDamageAction = highDamage.Battle.CommittedForecasts
                .Single(value => value.UnionId == "UNION_OPENING_01" &&
                                 value.CommandId == "CMD_BALANCED")
                .MemberActions.Single(value => value.ActorMemberId == "RECRUIT_1");
            var lowDamageActor = lowDamage.Battle.PlayerUnions
                .SelectMany(value => value.Members)
                .Single(value => value.MemberId == "RECRUIT_1");
            var highDamageActor = highDamage.Battle.PlayerUnions
                .SelectMany(value => value.Members)
                .Single(value => value.MemberId == "RECRUIT_1");

            Assert.That(lowDamageAction.ArtId, Is.EqualTo(basicSwordArt));
            Assert.That(highDamageAction.ArtId, Is.EqualTo(lowDamageAction.ArtId));
            Assert.That(
                M2BattleCommandService.EffectiveArtPowerPermille088(
                    lowDamage.Battle, _content, lowDamageActor, basicSwordArt),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.BasePowerPermille));
            Assert.That(
                M2BattleCommandService.EffectiveArtPowerPermille088(
                    highDamage.Battle, _content, highDamageActor, basicSwordArt),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.MaximumPowerPermille));
            Assert.That(-highDamageAction.PredictedHpDelta,
                Is.GreaterThan(-lowDamageAction.PredictedHpDelta));

            var lowHealingCampaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_0", 9, null);
            var lowHealing = Require(_commands.StartEncounterBattle(
                lowHealingCampaign,
                _content,
                "BATTLE_ART_LEVEL_HEALING_088",
                "Compare deterministic Art mastery restoration.",
                1));
            var lowHealingSource = lowHealing.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var lowHealingForecast = lowHealing.Battle.CommittedForecasts.Single(value =>
                value.UnionId == lowHealingSource.UnionId &&
                value.CommandId == "CMD_HEAL");
            var lowHealingAction = lowHealingForecast.MemberActions.Single(value =>
                value.ActorMemberId == "RECRUIT_3" &&
                value.Kind == BattleActionKind.Restoration);

            var highHealingCampaign = ConfigureRecruit086(
                CreateCampaign(3, 3), "RECRUIT_0", 9, null);
            highHealingCampaign = ConfigureRecruitArtMastery088(
                highHealingCampaign,
                "RECRUIT_3",
                lowHealingAction.ArtId,
                _content.Art(lowHealingAction.ArtId).Discipline,
                4700);
            var highHealing = Require(_commands.StartEncounterBattle(
                highHealingCampaign,
                _content,
                "BATTLE_ART_LEVEL_HEALING_088",
                "Compare deterministic Art mastery restoration.",
                1));
            var highHealingSource = highHealing.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == "RECRUIT_3"));
            var highHealingForecast = highHealing.Battle.CommittedForecasts.Single(value =>
                value.UnionId == highHealingSource.UnionId &&
                value.CommandId == "CMD_HEAL");
            var highHealingAction = highHealingForecast.MemberActions.Single(value =>
                value.ActorMemberId == "RECRUIT_3" &&
                value.Kind == BattleActionKind.Restoration);
            var highHealingTarget = highHealing.Battle.PlayerUnions.Single(value =>
                value.UnionId == highHealingForecast.TargetId);
            var missingHp = highHealingTarget.Members.Sum(value =>
                value.MaximumHp - value.CurrentHp);

            Assert.That(highHealingAction.ArtId, Is.EqualTo(lowHealingAction.ArtId));
            Assert.That(highHealingAction.PredictedHpDelta,
                Is.GreaterThan(lowHealingAction.PredictedHpDelta));
            Assert.That(highHealingAction.PredictedHpDelta,
                Is.LessThanOrEqualTo(missingHp));

            var resolvedHealing = SelectForecasts086(
                highHealing,
                highHealingSource.UnionId,
                highHealingForecast.ForecastId);
            resolvedHealing = Require(_commands.ConfirmRound(
                resolvedHealing,
                _content));
            var actualHealing = resolvedHealing.Battle.RoundRecords.Last().Events
                .Where(value => value.ActorMemberId == "RECRUIT_3" &&
                                value.ArtId == highHealingAction.ArtId &&
                                (value.EventType == "RESTORATION" ||
                                 value.EventType == "REVIVED"))
                .Sum(value => value.Amount);
            Assert.That(actualHealing, Is.EqualTo(highHealingAction.PredictedHpDelta),
                "The existing restoration resolver must reproduce the scaled committed Forecast exactly.");
        }

        [Test]
        public void PreviousBattleRulesResumeUnchangedAtLegacyArtPower088()
        {
            const string artId = "ART_BASIC_SABER_CUT";
            var campaign = ConfigureRecruitArtMastery088(
                CreateCampaign(3, 3),
                "RECRUIT_1",
                artId,
                _content.Art(artId).Discipline,
                4700);
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "BATTLE_ART_LEVEL_LEGACY_088",
                "Resume the prior battle rules without mutation.",
                1));
            var legacyVersion = _content.ContentVersion + "|" +
                                M2BattleCommandService.PreviousTutorialRulesVersion088;
            var legacyBattle = WithBattleContentVersion088(
                campaign.Battle,
                legacyVersion);
            campaign = campaign.WithBattle(legacyBattle);
            var actor = legacyBattle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == "RECRUIT_1");
            var beforeResume = CanonicalJson.Serialize(legacyBattle);

            Assert.That(
                M2BattleCommandService.UsesCurrentTutorialRules(
                    legacyBattle, _content),
                Is.True);
            Assert.That(
                M2BattleCommandService.UsesArtMasteryLevelScaling088(
                    legacyBattle, _content),
                Is.False);
            Assert.That(
                M2BattleCommandService.EffectiveArtPowerPermille088(
                    legacyBattle, _content, actor, artId),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.BasePowerPermille));

            var resumed = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                legacyBattle.BattleId,
                legacyBattle.Objective,
                1));
            Assert.That(CanonicalJson.Serialize(resumed.Battle),
                Is.EqualTo(beforeResume),
                "Resuming an immediately previous-version save must not rebuild or rescale its committed Forecasts.");
        }

        [Test]
        public void BreakthroughAndJsonReloadRetainEveryPreviouslyLearnedArt088()
        {
            var campaign = Require(_commands.StartTutorialBattle(
                CreateCampaign(3),
                _content));
            var memberId = campaign.Battle.TutorialBreakthroughMemberId;
            var targetArtId = campaign.Battle.TutorialBreakthroughArtId;
            var union = campaign.Battle.PlayerUnions.Single(value =>
                value.Members.Any(member => member.MemberId == memberId));
            var before = union.Members.Single(value => value.MemberId == memberId)
                .LearnedArtIds.ToArray();
            var forecast = campaign.Battle.CommittedForecasts.Single(value =>
                value.UnionId == union.UnionId &&
                value.CommandId == "CMD_ALL_OUT");

            campaign = Require(_commands.SelectForecast(
                campaign,
                union.UnionId,
                forecast.ForecastId));
            campaign = Require(_commands.ConfirmRound(campaign, _content));
            var learned = campaign.Battle.PlayerUnions.SelectMany(value => value.Members)
                .Single(value => value.MemberId == memberId);
            Assert.That(before.All(learned.LearnedArtIds.Contains), Is.True);
            Assert.That(learned.LearnedArtIds, Does.Contain(targetArtId));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));
            Assert.That(reloaded, Is.Not.Null);
            var reloadedMember = reloaded.Battle.PlayerUnions
                .SelectMany(value => value.Members)
                .Single(value => value.MemberId == memberId);
            Assert.That(before.All(reloadedMember.LearnedArtIds.Contains), Is.True);
            Assert.That(reloadedMember.LearnedArtIds, Does.Contain(targetArtId));
        }

        private CampaignState SelectCommandForEveryActiveUnion(
            CampaignState campaign,
            string firstChoice,
            string remainingChoice = null)
        {
            var active = campaign.Battle.PlayerUnions.Where(value => !value.Retreated && !value.IsDefeated).ToArray();
            for (var index = 0; index < active.Length; index++)
            {
                var desired = index == 0 ? firstChoice : remainingChoice ?? firstChoice;
                var forecast = campaign.Battle.CommittedForecasts.FirstOrDefault(value =>
                    value.UnionId == active[index].UnionId && value.CommandId == desired) ??
                    campaign.Battle.CommittedForecasts.First(value => value.UnionId == active[index].UnionId);
                campaign = Require(_commands.SelectForecast(campaign, active[index].UnionId, forecast.ForecastId));
            }
            return campaign;
        }

        private CampaignState ResolveTutorialBattle(CampaignState campaign)
        {
            campaign = Require(_commands.StartTutorialBattle(campaign, _content));
            return ResolveActiveBattle(campaign);
        }

        private CampaignState ResolveEncounterBattle(CampaignState campaign, string battleId)
        {
            campaign = Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                battleId,
                "Complete the first-hour battle reward profile test.",
                1));
            return ResolveActiveBattle(campaign);
        }

        private CampaignState ResolveActiveBattle(CampaignState campaign)
        {
            for (var round = 0; round < 20 && campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                campaign = SelectCommandForEveryActiveUnion(campaign, "CMD_ALL_OUT", "CMD_BALANCED");
                campaign = Require(_commands.ConfirmRound(campaign, _content));
            }
            Assert.That(campaign.Battle.Outcome, Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.Reward, Is.Not.Null);
            return campaign;
        }

        private static CampaignState CreateCampaign(params int[] planSizes)
        {
            var classes = new[]
            {
                "CLASS_TEND_GUARDIAN", "CLASS_TEND_WARRIOR", "CLASS_TEND_RANGER",
                "CLASS_TEND_PRIEST", "CLASS_TEND_MAGE", "CLASS_TEND_ROGUE"
            };
            var weaponTags = new[]
            {
                new[] { "SHIELD", "SWORD", "WEAPON" }, new[] { "SWORD", "WEAPON" },
                new[] { "BOW", "WEAPON" }, new[] { "STAFF", "HEALING" },
                new[] { "WAND", "FOCUS_TOOL" }, new[] { "DAGGER", "WEAPON" }
            };
            var recruits = new List<RecruitState>();
            for (var index = 0; index < 6; index++)
            {
                var main = new EquipmentItemState(
                    "ITEM_WEAPON_" + index, "EQ_WEAPON_" + index, "Starter Weapon " + index,
                    new[] { EquipmentSlotIds.MainHand }, weaponTags[index], "QUALITY_STANDARD", 10000, false);
                var body = new EquipmentItemState(
                    "ITEM_BODY_" + index, "EQ_BODY_" + index, "Starter Armor " + index,
                    new[] { EquipmentSlotIds.BodyArmor }, new[] { "ARMOR" }, "QUALITY_STANDARD", 10000, false);
                recruits.Add(new RecruitState(
                    "RECRUIT_" + index, 105 + index * 5, 105 + index * 5, 20 + index * 2, 20 + index * 2,
                    "Recruit " + index, RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01",
                    classes[index], "Observed", 5200 + index * 200, RecruitAuthorityKind.Normal,
                    string.Empty, string.Empty,
                    new EquipmentLoadoutState(new[]
                    {
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, main),
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.BodyArmor, body)
                    }), true, string.Empty, string.Empty, 48 + index, 45 + index));
            }
            var unions = new List<UnionState>();
            var cursor = 0;
            for (var plan = 0; plan < planSizes.Length; plan++)
            {
                var members = new List<string>();
                for (var member = 0; member < planSizes[plan] && cursor < recruits.Count; member++, cursor++)
                    members.Add(recruits[cursor].RecruitId);
                if (members.Count == 0) continue;
                unions.Add(new UnionState(
                    "UNION_OPENING_0" + (plan + 1), "Opening Union " + (plan + 1), UnionKind.Normal,
                    members[0], members.AsReadOnly(), "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED",
                    18, 8500));
            }
            var guild = new GuildState("GUILD_M2_TEST", 0, recruits.AsReadOnly(), unions.AsReadOnly());
            var profile = new NewGuildProfileState(
                "Test Guildmaster", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000210", 20260814L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }

        private static CampaignState CreateTenUnionCampaign()
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

        private static CampaignState CreateSingleRecruitCampaign(int recruitIndex)
        {
            var campaign = CreateCampaign(1);
            var recruit = campaign.Guild.Recruits[recruitIndex];
            var union = new UnionState(
                "UNION_OPENING_01", "Opening Union 1", UnionKind.Normal,
                recruit.RecruitId, new[] { recruit.RecruitId }, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED",
                18, 8500);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                new[] { union },
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState CreateRecoveryCampaign()
        {
            var campaign = CreateCampaign(3);
            var memberIds = new[] { "RECRUIT_1", "RECRUIT_2", "RECRUIT_5" };
            var union = new UnionState(
                "UNION_OPENING_01", "Opening Union 1", UnionKind.Normal,
                memberIds[0], memberIds, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED",
                18, 8500);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                new[] { union },
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState ConfigureUnionCohesion086(
            CampaignState campaign,
            string unionId,
            int cohesionBasisPoints)
        {
            var unions = campaign.Guild.Unions.ToList();
            var unionIndex = unions.FindIndex(value =>
                value.UnionId == unionId);
            Assert.That(unionIndex, Is.GreaterThanOrEqualTo(0));
            var source = unions[unionIndex];
            unions[unionIndex] = new UnionState(
                source.UnionId,
                source.DisplayName,
                source.Kind,
                source.LeaderRecruitId,
                source.MemberRecruitIds,
                source.FormationId,
                source.DoctrineId,
                source.SharedAp,
                cohesionBasisPoints,
                source.MemberPositions);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                unions.AsReadOnly(),
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private CampaignState StartOverlappingRestorationBattle086()
        {
            var campaign = CreateCampaign(1, 1, 1, 1, 1);
            campaign = ConfigureRecruit086(
                campaign, "RECRUIT_0", 75, null);
            campaign = ConfigureRecruit086(
                campaign, "RECRUIT_4", null, null,
                "ART_DAWN_WITHOUT_LOSS");
            for (var unionIndex = 1; unionIndex <= 5; unionIndex++)
                campaign = ConfigureUnionCohesion086(
                    campaign,
                    "UNION_OPENING_0" + unionIndex,
                    10000);
            return Require(_commands.StartEncounterBattle(
                campaign,
                _content,
                "BATTLE_STALE_RESTORATION_086",
                "Adapt overlapping allied restoration.",
                1));
        }

        private static CampaignState ConfigureRecruit086(
            CampaignState campaign,
            string recruitId,
            int? currentHp,
            int? currentMp,
            params string[] learnedArtIds)
        {
            var recruits = campaign.Guild.Recruits.ToList();
            var recruitIndex = recruits.FindIndex(value =>
                value.RecruitId == recruitId);
            Assert.That(recruitIndex, Is.GreaterThanOrEqualTo(0));
            var source = recruits[recruitIndex];
            var learned = source.Progression.LearnedArtIds.ToList();
            if (learnedArtIds != null)
                for (var index = 0; index < learnedArtIds.Length; index++)
                    if (!learned.Contains(learnedArtIds[index]))
                        learned.Add(learnedArtIds[index]);
            learned.Sort(StringComparer.Ordinal);
            var progression = source.Progression.WithArts(
                learned.AsReadOnly(), source.Progression.ArtMastery);
            recruits[recruitIndex] = new RecruitState(
                source.RecruitId,
                currentHp ?? source.CurrentHp,
                source.MaximumHp,
                currentMp ?? source.CurrentMp,
                source.MaximumMp,
                source.DisplayName,
                source.OriginKind,
                source.SignatureId,
                source.RaceId,
                source.WorldId,
                source.ClassTendencyId,
                source.LeadershipBand,
                source.PotentialBasisPoints,
                source.AuthorityKind,
                source.CanonicalApplicantJson,
                source.CanonicalScoutingReportJson,
                source.Equipment,
                source.VitalsInitialized,
                source.TutorialAliasId,
                source.AuthoredStableRecruitId,
                source.LeadershipScore,
                source.TacticalAptitude,
                progression);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                campaign.Guild.Unions,
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState ConfigureRecruitEquipment086(
            CampaignState campaign,
            string recruitId,
            params string[] equipmentTags)
        {
            var recruits = campaign.Guild.Recruits.ToList();
            var recruitIndex = recruits.FindIndex(value =>
                value.RecruitId == recruitId);
            Assert.That(recruitIndex, Is.GreaterThanOrEqualTo(0));
            var source = recruits[recruitIndex];
            var assignments = source.Equipment.Assignments.Where(value =>
                value.SlotId != EquipmentSlotIds.MainHand).ToList();
            var item = new EquipmentItemState(
                "ITEM_SUPPORT_STAFF_086_" + recruitId,
                "EQ_SUPPORT_STAFF_086",
                "Signal Staff",
                new[] { EquipmentSlotIds.MainHand },
                equipmentTags,
                "QUALITY_STANDARD",
                10000,
                false);
            assignments.Add(new EquipmentSlotAssignmentState(
                EquipmentSlotIds.MainHand, item));
            recruits[recruitIndex] = source.WithEquipment(
                new EquipmentLoadoutState(assignments.AsReadOnly()));
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                campaign.Guild.Unions,
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private CampaignState SelectForecasts086(
            CampaignState campaign,
            string rescueUnionId,
            string rescueForecastId)
        {
            foreach (var union in campaign.Battle.PlayerUnions.Where(value =>
                         !value.Retreated && !value.IsDefeated))
            {
                var forecast = union.UnionId == rescueUnionId
                    ? campaign.Battle.CommittedForecasts.Single(value =>
                        value.ForecastId == rescueForecastId)
                    : campaign.Battle.CommittedForecasts.Single(value =>
                        value.UnionId == union.UnionId &&
                        value.CommandId == "CMD_GUARD");
                campaign = Require(_commands.SelectForecast(
                    campaign, union.UnionId, forecast.ForecastId));
            }
            return campaign;
        }

        private CampaignState ResolveForgedForecast080(
            CampaignState campaign,
            IReadOnlyList<BattleUnionState> playerUnions,
            BattleForecastState forged)
        {
            var forecasts = campaign.Battle.CommittedForecasts.ToList();
            forecasts.Add(forged);
            campaign = campaign.WithBattle(campaign.Battle.With(
                playerUnions: playerUnions,
                committedForecasts: forecasts.AsReadOnly(),
                selections: Array.Empty<BattleForecastSelectionState>()));
            foreach (var union in playerUnions.Where(value => !value.Retreated && !value.IsDefeated))
            {
                var selected = StringComparer.Ordinal.Equals(union.UnionId, forged.UnionId)
                    ? forged
                    : forecasts.First(value => value.UnionId == union.UnionId &&
                        value.CommandId == "CMD_GUARD");
                campaign = Require(_commands.SelectForecast(
                    campaign, union.UnionId, selected.ForecastId));
            }
            return Require(_commands.ConfirmRound(campaign, _content));
        }

        private static BattleMemberState WithBattleArt086(
            BattleMemberState source,
            string artId,
            params string[] equipmentTags)
        {
            var learned = source.LearnedArtIds.ToList();
            if (!learned.Contains(artId)) learned.Add(artId);
            learned.Sort(StringComparer.Ordinal);
            var tags = source.EquipmentTags.ToList();
            if (equipmentTags != null)
                for (var index = 0; index < equipmentTags.Length; index++)
                    if (!tags.Contains(equipmentTags[index]))
                        tags.Add(equipmentTags[index]);
            tags.Sort(StringComparer.Ordinal);
            return new BattleMemberState(
                source.MemberId,
                source.DisplayName,
                source.ClassId,
                source.CurrentHp,
                source.MaximumHp,
                source.CurrentMp,
                source.MaximumMp,
                source.Attack,
                source.MagicAttack,
                tags.AsReadOnly(),
                source.Downed,
                source.Stabilized,
                source.Guarding,
                learned.AsReadOnly(),
                source.MeaningfulUsePoints,
                source.DiscoveryProgress,
                source.BreakthroughArtId,
                source.ArtProgress,
                source.EquippedMainHandInstanceId);
        }

        private static CampaignState ConfigureRecruitArtMastery088(
            CampaignState campaign,
            string recruitId,
            string artId,
            string discipline,
            int masteryPoints)
        {
            var recruits = campaign.Guild.Recruits.ToList();
            var recruitIndex = recruits.FindIndex(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            Assert.That(recruitIndex, Is.GreaterThanOrEqualTo(0));
            var source = recruits[recruitIndex];
            var learned = source.Progression.LearnedArtIds.ToList();
            if (!learned.Contains(artId)) learned.Add(artId);
            learned.Sort(StringComparer.Ordinal);
            var mastery = source.Progression.ArtMastery
                .Where(value => !StringComparer.Ordinal.Equals(value.ArtId, artId))
                .ToList();
            mastery.Add(new RecruitArtMasteryState(
                artId,
                discipline,
                masteryPoints > 0 ? 1 : 0,
                masteryPoints));
            mastery.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.ArtId, right.ArtId));
            recruits[recruitIndex] = source.WithProgression(
                source.Progression.WithArts(
                    learned.AsReadOnly(),
                    mastery.AsReadOnly()));
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                campaign.Guild.Unions,
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static BattleState WithBattleContentVersion088(
            BattleState source,
            string contentVersion) =>
            new BattleState(
                source.BattleId,
                contentVersion,
                source.Round,
                source.Phase,
                source.Outcome,
                source.Objective,
                source.PlayerUnions,
                source.EnemyUnions,
                source.CommittedForecasts,
                source.Selections,
                source.EventLog,
                source.RoundRecords,
                source.ForecastStateBasisHash,
                source.InitialBattleStateHash,
                source.FinalStateHash,
                source.TutorialBreakthroughMemberId,
                source.TutorialBreakthroughArtId,
                source.TutorialBreakthroughOccurred,
                source.Reward,
                source.InitialIntegrityStateHash090,
                source.FinalIntegrityStateHash090);

        private static BattleState WithDifferentEnemyPresentationIdentity090(
            BattleState source)
        {
            var enemyUnions090 = new List<BattleUnionState>(source.EnemyUnions.Count);
            for (var unionIndex090 = 0;
                 unionIndex090 < source.EnemyUnions.Count;
                 unionIndex090++)
            {
                var union090 = source.EnemyUnions[unionIndex090];
                var members090 = new List<BattleMemberState>(union090.Members.Count);
                for (var memberIndex090 = 0;
                     memberIndex090 < union090.Members.Count;
                     memberIndex090++)
                {
                    var variantIndex090 = 10 - memberIndex090;
                    members090.Add(union090.Members[memberIndex090].WithEnemyArt090(
                        EnemyArtIdentity090.BaseId090(70),
                        EnemyArtIdentity090.VariantId090(70, variantIndex090),
                        variantIndex090));
                }
                enemyUnions090.Add(union090.With(members: members090.AsReadOnly()));
            }
            return source.With(enemyUnions: enemyUnions090.AsReadOnly());
        }

        private static BattleState WithoutEnemyPresentationIdentity090(
            BattleState source)
        {
            var enemyUnions090 = source.EnemyUnions.Select(union090 =>
                union090.With(members: union090.Members.Select(member090 =>
                    member090.WithEnemyArt090(null, null, 0)).ToArray()))
                .ToArray();
            return source.With(enemyUnions: enemyUnions090);
        }

        private static string InitialIntegrityStateHashFor090(BattleState source) =>
            M2BattleCommandService.AuthoritativeStateHash(new BattleState(
                source.BattleId,
                source.ContentVersion,
                1,
                BattlePhase.ForecastSelection,
                BattleOutcome.InProgress,
                source.Objective,
                source.PlayerUnions,
                source.EnemyUnions,
                Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(),
                source.EventLog,
                Array.Empty<BattleRoundRecordState>(),
                string.Empty,
                string.Empty,
                string.Empty,
                source.TutorialBreakthroughMemberId,
                source.TutorialBreakthroughArtId,
                false));

        private static EncounterRoster070 WithDifferentVisualSeeds090(
            EncounterRoster070 source)
        {
            var unions090 = new List<EncounterEnemyUnion070>(source.Unions.Count);
            for (var unionIndex090 = 0;
                 unionIndex090 < source.Unions.Count;
                 unionIndex090++)
            {
                var union090 = source.Unions[unionIndex090];
                var members090 = new List<EncounterEnemyMember070>(union090.Members.Count);
                for (var memberIndex090 = 0;
                     memberIndex090 < union090.Members.Count;
                     memberIndex090++)
                {
                    var member090 = union090.Members[memberIndex090];
                    var normalizedSeed090 = 1 +
                        ((member090.VisualVariantSeed % 10) + 9) % 10;
                    var differentSeed090 = 1 + (normalizedSeed090 + 4) % 10;
                    members090.Add(new EncounterEnemyMember070(
                        member090.MemberId,
                        member090.SourceEnemyId,
                        member090.FamilyId,
                        differentSeed090,
                        member090.Definition));
                }
                unions090.Add(new EncounterEnemyUnion070(
                    union090.UnionId,
                    union090.SourceUnionId,
                    union090.SourceDefinition,
                    members090.AsReadOnly(),
                    union090.FamilyIds));
            }
            return new EncounterRoster070(
                source.RosterId,
                source.CanonicalSeedIdentity,
                unions090.AsReadOnly(),
                source.FamilyIds);
        }

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static bool DebugEvidenceContainsCandidate(string evidence, string memberId, string artId)
        {
            if (string.IsNullOrWhiteSpace(evidence)) return false;
            var memberToken = "\"MemberId\":" + CanonicalJson.Serialize(memberId);
            var artToken = "\"ArtId\":" + CanonicalJson.Serialize(artId);
            return evidence.IndexOf("\"LegalActionPool\":", StringComparison.Ordinal) >= 0 &&
                   evidence.IndexOf(memberToken, StringComparison.Ordinal) >= 0 &&
                   evidence.IndexOf(artToken, StringComparison.Ordinal) >= 0;
        }

        private static string RemoveJsonProperties(string json, params string[] propertyNames)
        {
            return new CanonicalJsonPropertyFilter(json, propertyNames).Filter();
        }

        private static string Sha256HexUtf8(string value)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private sealed class CanonicalJsonPropertyFilter
        {
            private readonly string _source;
            private readonly HashSet<string> _removed;
            private int _index;

            public CanonicalJsonPropertyFilter(string source, IEnumerable<string> removed)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
                _removed = new HashSet<string>(removed ?? Array.Empty<string>(), StringComparer.Ordinal);
            }

            public string Filter()
            {
                var output = new StringBuilder(_source.Length);
                CopyValue(output);
                SkipWhitespace();
                if (_index != _source.Length) throw new InvalidDataException("Legacy JSON fixture has trailing data.");
                return output.ToString();
            }

            private void CopyValue(StringBuilder output)
            {
                SkipWhitespace();
                if (_index >= _source.Length) throw new InvalidDataException("Legacy JSON fixture ended early.");
                switch (_source[_index])
                {
                    case '{': CopyObject(output); return;
                    case '[': CopyArray(output); return;
                    case '"': output.Append(ReadString()); return;
                    default: CopyLiteral(output); return;
                }
            }

            private void CopyObject(StringBuilder output)
            {
                Expect('{');
                output.Append('{');
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    output.Append('}');
                    return;
                }

                var wroteProperty = false;
                while (true)
                {
                    SkipWhitespace();
                    var rawName = ReadString();
                    var name = rawName.Substring(1, rawName.Length - 2);
                    SkipWhitespace();
                    Expect(':');
                    if (_removed.Contains(name))
                    {
                        CopyValue(new StringBuilder());
                    }
                    else
                    {
                        if (wroteProperty) output.Append(',');
                        output.Append(rawName).Append(':');
                        CopyValue(output);
                        wroteProperty = true;
                    }

                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        output.Append('}');
                        return;
                    }
                    Expect(',');
                }
            }

            private void CopyArray(StringBuilder output)
            {
                Expect('[');
                output.Append('[');
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    output.Append(']');
                    return;
                }

                var wroteItem = false;
                while (true)
                {
                    if (wroteItem) output.Append(',');
                    CopyValue(output);
                    wroteItem = true;
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        output.Append(']');
                        return;
                    }
                    Expect(',');
                }
            }

            private string ReadString()
            {
                SkipWhitespace();
                if (_index >= _source.Length || _source[_index] != '"')
                    throw new InvalidDataException("Legacy JSON fixture expected a string.");
                var start = _index++;
                var escaped = false;
                while (_index < _source.Length)
                {
                    var value = _source[_index++];
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (value == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (value == '"') return _source.Substring(start, _index - start);
                }
                throw new InvalidDataException("Legacy JSON fixture contains an unterminated string.");
            }

            private void CopyLiteral(StringBuilder output)
            {
                var start = _index;
                while (_index < _source.Length)
                {
                    var value = _source[_index];
                    if (value == ',' || value == ']' || value == '}' || char.IsWhiteSpace(value)) break;
                    _index++;
                }
                if (_index == start) throw new InvalidDataException("Legacy JSON fixture contains an invalid value.");
                output.Append(_source, start, _index - start);
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (_index >= _source.Length || _source[_index] != expected) return false;
                _index++;
                return true;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (_index >= _source.Length || _source[_index] != expected)
                    throw new InvalidDataException("Legacy JSON fixture expected '" + expected + "'.");
                _index++;
            }

            private void SkipWhitespace()
            {
                while (_index < _source.Length && char.IsWhiteSpace(_source[_index])) _index++;
            }
        }
    }
}
