using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SssTenV4ProgressionAcquisition090Tests
    {
        [Test]
        public void FrozenResolverMapsAllSeventyFamiliesAndTenVariantsExactly090()
        {
            var resolver = SssTenV4FamilyResolver090.Instance;
            for (var family = 1; family <= 70; family++)
            {
                var baseId = "ENEMY_REC_" + family.ToString("000");
                Assert.That(resolver.TryResolve(baseId, out var resolvedBase),
                    Is.True);
                Assert.That(resolvedBase, Is.EqualTo(baseId));
                for (var variant = 1; variant <= 10; variant++)
                {
                    var variantId = baseId + "_VAR_" + variant.ToString("00");
                    Assert.That(resolver.TryResolve(variantId,
                        out var resolvedVariant), Is.True);
                    Assert.That(resolvedVariant, Is.EqualTo(baseId));
                }
            }

            Assert.That(resolver.TryResolve("ENEMY_REC_071", out _), Is.False);
            Assert.That(resolver.TryResolve("ENEMY_REC_001_VAR_11", out _),
                Is.False);
            Assert.That(resolver.TryResolve("Gloomroot Wolf", out _), Is.False);
        }

        [Test]
        public void EligibleSpawnCreditsEachDeployedSpecialistAndHuntOnce090()
        {
            var state = SssTenV4State090.Default();
            state = SssTenV4ProgressionService090.ApplyConfirmedDefeatForTests090(
                state, "ENCOUNTER_A", "SPAWN_A", "ENEMY_REC_007_VAR_02",
                new[]
                {
                    CovenantHeroes.Tamer,
                    CovenantHeroes.Summoner,
                    CovenantHeroes.Shapeshifter,
                    "SSS_NERIS_DAWNWELL"
                });

            Assert.That(CovenantMastery.Count(state.Progression.familyProgress,
                CovenantHeroes.Tamer, "ENEMY_REC_007"), Is.EqualTo("1"));
            Assert.That(CovenantMastery.Count(state.Progression.familyProgress,
                CovenantHeroes.Summoner, "ENEMY_REC_007"), Is.EqualTo("1"));
            Assert.That(CovenantMastery.Count(state.Progression.familyProgress,
                CovenantHeroes.Shapeshifter, "ENEMY_REC_007"), Is.EqualTo("1"));
            Assert.That(SssGrowth.TotalCount(state.Progression,
                "SSS_NERIS_DAWNWELL"), Is.EqualTo("1"));
            Assert.That(SssGrowth.TotalCount(state.Progression,
                "SSS_MYRIEN_STARFALL"), Is.EqualTo("0"),
                "A bench specialist must not receive kill credit.");
            Assert.That(HuntCount090(state, "ENEMY_REC_007"), Is.EqualTo("1"),
                "The guild-wide hunt counts one spawn, not one per specialist.");

            state = SssTenV4ProgressionService090.ApplyConfirmedDefeatForTests090(
                state, "ENCOUNTER_A", "SPAWN_B", "ENEMY_REC_007_VAR_10",
                new[] { CovenantHeroes.Tamer, "SSS_MYRIEN_STARFALL" });
            Assert.That(CovenantMastery.Count(state.Progression.familyProgress,
                CovenantHeroes.Tamer, "ENEMY_REC_007"), Is.EqualTo("2"));
            Assert.That(CovenantMastery.Count(state.Progression.familyProgress,
                CovenantHeroes.Summoner, "ENEMY_REC_007"), Is.EqualTo("1"));
            Assert.That(SssGrowth.TotalCount(state.Progression,
                "SSS_MYRIEN_STARFALL"), Is.EqualTo("1"));
            Assert.That(HuntCount090(state, "ENEMY_REC_007"), Is.EqualTo("2"),
                "Cosmetic variants must aggregate into their base family.");

            var duplicate =
                SssTenV4ProgressionService090.ApplyConfirmedDefeatForTests090(
                    state, "ENCOUNTER_A", "SPAWN_B",
                    "ENEMY_REC_007_VAR_10",
                    new[] { CovenantHeroes.Tamer, "SSS_MYRIEN_STARFALL" });
            Assert.That(CovenantMastery.Count(
                duplicate.Progression.familyProgress, CovenantHeroes.Tamer,
                "ENEMY_REC_007"), Is.EqualTo("2"));
            Assert.That(HuntCount090(duplicate, "ENEMY_REC_007"),
                Is.EqualTo("2"));
        }

        [Test]
        public void FamilyProgressRemainsExactBeyondInt64AndReload090()
        {
            var huge = BigInteger.Parse("999999999999999999999999999999999999");
            var progression = new SssSave();
            progression.familyProgress.counters.Add(new CounterRow
            {
                heroId = CovenantHeroes.Tamer,
                baseFamilyId = "ENEMY_REC_070",
                totalDefeats = huge.ToString()
            });
            var state = new SssTenV4State090(progression);
            state = SssTenV4ProgressionService090.ApplyConfirmedDefeatForTests090(
                state, "HUGE_ENCOUNTER", "HUGE_SPAWN",
                "ENEMY_REC_070_VAR_09", new[] { CovenantHeroes.Tamer });
            Assert.That(CovenantMastery.Count(state.Progression.familyProgress,
                CovenantHeroes.Tamer, "ENEMY_REC_070"),
                Is.EqualTo((huge + 1).ToString()));

            var json = JsonConvert.SerializeObject(state);
            var reloaded = JsonConvert.DeserializeObject<SssTenV4State090>(json);
            Assert.That(CovenantMastery.Count(
                reloaded.Progression.familyProgress, CovenantHeroes.Tamer,
                "ENEMY_REC_070"), Is.EqualTo((huge + 1).ToString()));
            Assert.That(HuntCount090(reloaded, "ENEMY_REC_070"),
                Is.EqualTo("1"));
        }

        [Test]
        public void CampaignCompletionCountsDistinctOperationsWithoutFakeWins090()
        {
            var campaign = WithCompletedDefinitions090(Campaign090(),
                "CHAPTER_01", "CHAPTER_02");
            var historical = SssTenV4ProgressionService090
                .CampaignCompletionKey090("CHAPTER_01");
            var first = SssTenV4ProgressionService090
                .CampaignCompletionKey090("CHAPTER_02");
            var sameDefinitionAnotherRun = SssTenV4ProgressionService090
                .CampaignCompletionKey090("CHAPTER_02");
            var second = SssTenV4ProgressionService090
                .CampaignCompletionKey090("ECHO_01");
            Assert.That(sameDefinitionAnotherRun, Is.EqualTo(first));

            campaign = Require(SssTenV4ProgressionService090
                .RecordCampaignCompletion090(campaign, first));
            campaign = Require(SssTenV4ProgressionService090
                .RecordCampaignCompletion090(campaign,
                    sameDefinitionAnotherRun));
            campaign = Require(SssTenV4ProgressionService090
                .RecordCampaignCompletion090(campaign, second));
            var progression = SssTenV4CampaignAccessor090.Read(campaign)
                .Progression;
            Assert.That(progression.world.uniqueCampaignStageCompletions,
                Is.EqualTo("3"));
            Assert.That(progression.world.campaignBattleWins, Is.EqualTo("0"));
            CollectionAssert.AreEquivalent(new[] { historical, first, second },
                progression.world.completedStageKeys);
        }

        [Test]
        public void ContractIsOneGuaranteedShuffledCandidateAtWinOneHundred090()
        {
            var board = Board090();
            var before = new SssSave();
            before.world.campaignBattleWins = "99";
            Assert.That(SssTenV4AcquisitionService090
                .CreateCampaignContractCandidate090(90090L, "OP_090", board,
                    Array.Empty<RecruitState>(), before), Is.Null);

            var unlocked = before.Copy();
            unlocked.world.campaignBattleWins = "100";
            var card = SssTenV4AcquisitionService090
                .CreateCampaignContractCandidate090(90090L, "OP_090", board,
                    Array.Empty<RecruitState>(), unlocked);
            Assert.That(card, Is.Not.Null);
            Assert.That(card.Category,
                Is.EqualTo(SssTenV4AcquisitionService090
                    .CampaignContractCategory090));
            Assert.That(card.AdvancesRoute, Is.False);
            Assert.That(card.ResolutionDifficulty, Is.Zero,
                "A selected shuffled contract must not roll a second failure check.");
            Assert.That(SssTenV4AcquisitionService090
                .TryReadCampaignContractSource090(card.SourceTag,
                    out var wins, out var selection, out var heroId), Is.True);
            Assert.That(wins, Is.EqualTo("100"));
            Assert.That(selection, Is.GreaterThanOrEqualTo(0));
            Assert.That(heroId, Is.EqualTo(card.RecruitStableId));

            var firstDeck = new ExpeditionDeckService089().Create(
                90090L, "OP_090", board, Array.Empty<RecruitState>(),
                Array.Empty<EquipmentItemState>(), null, Array.Empty<string>(),
                1, unlocked);
            var secondDeck = new ExpeditionDeckService089().Create(
                90090L, "OP_090", board, Array.Empty<RecruitState>(),
                Array.Empty<EquipmentItemState>(), null, Array.Empty<string>(),
                1, unlocked);
            Assert.That(AllCards090(firstDeck).Count(
                    SssTenV4AcquisitionService090.IsCampaignContract090),
                Is.EqualTo(1),
                "The live shuffled Expedition Deck gets one candidate, not a second deck or hidden roll.");
            Assert.That(JsonConvert.SerializeObject(firstDeck),
                Is.EqualTo(JsonConvert.SerializeObject(secondDeck)),
                "Deck generation must not depend on frame timing or execution speed.");

            var campaign = Campaign090().WithSssV4090(
                new SssTenV4State090(unlocked));
            var receipt = ContractReceipt090(card);
            campaign = Require(SssTenV4AcquisitionService090
                .ApplySelectedCampaignContract090(campaign, card, receipt));
            var offerKey = ExactNumbers.Key("CampaignSss", card.CardId);
            var offer = SssTenV4CampaignAccessor090.Read(campaign).Progression
                .rewardOffers.Single(value => value.key == offerKey);
            Assert.That(offer.success, Is.True);
            Assert.That(offer.chanceBasisPoints, Is.EqualTo(10000));
            Assert.That(offer.heroId, Is.EqualTo(card.RecruitStableId));

            var replay = Require(SssTenV4AcquisitionService090
                .ApplySelectedCampaignContract090(campaign, card, receipt));
            Assert.That(SssTenV4CampaignAccessor090.Read(replay).Progression
                .rewardOffers.Count, Is.EqualTo(1));
            campaign = Require(SssTenV4AcquisitionService090.ClaimOffer090(
                campaign, offerKey));
            Assert.That(SssTenV4Roster090.RosterContains(
                campaign.Guild.Recruits, card.RecruitStableId), Is.True);
            var rosterCount = campaign.Guild.Recruits.Count;
            campaign = Require(SssTenV4AcquisitionService090.ClaimOffer090(
                campaign, offerKey));
            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(rosterCount));
        }

        [Test]
        public void TowerFloorFiveHundredPersistsDeterministicOnePercentOffer090()
        {
            var owned = SssTenV4Roster090.MaterializeGrant(
                SssHeroes.All[0]).Recruit;
            var campaign = Campaign090(new[] { owned });
            campaign = Require(SssTenV4AcquisitionService090
                .ApplyTowerCompletion090(campaign, "TOWER_CLEAR_499", 499));
            var state = SssTenV4CampaignAccessor090.Read(campaign);
            Assert.That(state.Progression.world.highestClearedTowerFloor,
                Is.EqualTo("499"));
            Assert.That(state.Progression.rewardOffers, Is.Empty);

            var winningReceipt = FindPassingTowerReceipt090(campaign, 500,
                SssHeroes.All.Length - 1);
            campaign = Require(SssTenV4AcquisitionService090
                .ApplyTowerCompletion090(campaign, winningReceipt, 500));
            state = SssTenV4CampaignAccessor090.Read(campaign);
            Assert.That(state.Progression.world.highestClearedTowerFloor,
                Is.EqualTo("500"));
            Assert.That(state.Progression.rewardOffers.Count, Is.EqualTo(1));
            var offer = state.Progression.rewardOffers[0];
            Assert.That(offer.eligible, Is.True);
            Assert.That(offer.success, Is.True);
            Assert.That(offer.chanceBasisPoints, Is.EqualTo(100));
            Assert.That(offer.chanceRoll, Is.LessThan(100));
            Assert.That(offer.heroId, Is.Not.EqualTo(SssHeroes.All[0]),
                "Owned and successful pending heroes must be excluded.");

            var reload = JsonConvert.DeserializeObject<SssTenV4State090>(
                JsonConvert.SerializeObject(state));
            var reloadedCampaign = campaign.WithSssV4090(reload);
            reloadedCampaign = Require(SssTenV4AcquisitionService090
                .ApplyTowerCompletion090(reloadedCampaign, winningReceipt, 500));
            var replay = SssTenV4CampaignAccessor090.Read(reloadedCampaign);
            Assert.That(replay.Progression.rewardOffers.Count, Is.EqualTo(1));
            Assert.That(replay.Progression.rewardOffers[0].chanceRoll,
                Is.EqualTo(offer.chanceRoll));
            Assert.That(replay.Progression.rewardOffers[0].heroId,
                Is.EqualTo(offer.heroId));
        }

        [Test]
        public void OldSaveDefaultsHonestlyToZeroAndNaturalCreditsAreExactOnce090()
        {
            var campaign = Campaign090();
            var oldJson = JObject.FromObject(campaign);
            oldJson.Remove("SssV4090");
            var migrated = oldJson.ToObject<CampaignState>();
            var state = SssTenV4CampaignAccessor090.Read(migrated);
            Assert.That(state.Progression.world.campaignBattleWins,
                Is.EqualTo("0"));
            Assert.That(state.WeaponHunt.families, Is.Empty,
                "Unverifiable historical kills must not be invented.");

            var hero = SssTenV4Roster090.MaterializeGrant(
                SssHeroes.All[0]).Recruit;
            campaign = Campaign090(new[] { hero });
            campaign = Require(SssTenV4AcquisitionService090
                .GrantAscensionTrialCredit090(campaign, hero.RecruitId,
                    "ASCENSION_TRIAL_REWARD_001"));
            campaign = Require(SssTenV4AcquisitionService090
                .GrantAscensionTrialCredit090(campaign, hero.RecruitId,
                    "ASCENSION_TRIAL_REWARD_001"));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, hero.RecruitId), Is.EqualTo(1));
            Assert.That(campaign.Guild.Inventory.Single().InventoryOnly, Is.True);
            Assert.That(hero.Equipment.Assignments, Is.Empty,
                "Natural grants enter inventory and never auto-equip.");
        }

        [Test]
        public void CompletedNaturalHuntUsesSharedUniqueWeaponReceiptExactlyOnce090()
        {
            var hero = SssTenV4Roster090.MaterializeGrant(
                SssHeroes.All[0]).Recruit;
            var hunt = new SssHuntSave();
            hunt.families.AddRange(SssWeaponHunt.RequiredFamilies().Select(
                familyId => new SssHuntRow
                {
                    baseFamilyId = familyId,
                    totalDefeats = SssWeaponHunt.DefeatsRequiredPerFamily
                        .ToString()
                }));
            var campaign = Campaign090(new[] { hero }).WithSssV4090(
                new SssTenV4State090(weaponHunt: hunt));

            campaign = Require(SssTenV4AcquisitionService090
                .ClaimSignatureWeapon090(campaign, hero.RecruitId, true));
            var itemId = SssSignatureWeapons.ItemId(hero.RecruitId);
            Assert.That(campaign.Guild.Inventory.Count(item =>
                item.DefinitionId == itemId), Is.EqualTo(1));
            Assert.That(SssTenV4CampaignAccessor090.Read(campaign).Progression
                    .codeReceipts,
                Does.Contain(SssSignatureWeapons.EntitlementReceipt(
                    hero.RecruitId)));
            Assert.That(campaign.Guild.Recruits.Single().Equipment.Assignments,
                Is.Empty, "Natural signature weapons must never auto-equip.");

            campaign = Require(SssTenV4AcquisitionService090
                .ClaimSignatureWeapon090(campaign, hero.RecruitId, true));
            Assert.That(campaign.Guild.Inventory.Count(item =>
                item.DefinitionId == itemId), Is.EqualTo(1));
        }

        [Test]
        public void CompletedTrialCreditsFirstEligibleDeployedSssExactlyOnce090()
        {
            var bench = SssTenV4Roster090.MaterializeGrant(
                SssHeroes.All[0]).Recruit;
            var deployed = SssTenV4Roster090.MaterializeGrant(
                SssHeroes.All[1]).Recruit;
            var campaign = Campaign090(new[] { bench, deployed });

            campaign = Require(SssTenV4AcquisitionService090
                .ApplyCompletedAscensionTrial090(campaign,
                    "ABYSS_TRIAL_COMPLETION_090",
                    new[] { deployed.RecruitId }));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, bench.RecruitId), Is.Zero,
                "A rostered bench SSS hero must not take the deployed hero's Trial credit.");
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, deployed.RecruitId), Is.EqualTo(1));

            campaign = Require(SssTenV4AcquisitionService090
                .ApplyCompletedAscensionTrial090(campaign,
                    "ABYSS_TRIAL_COMPLETION_090",
                    new[] { deployed.RecruitId }));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(
                campaign, deployed.RecruitId), Is.EqualTo(1));
        }

        private static string HuntCount090(SssTenV4State090 state,
            string familyId)
        {
            return state.WeaponHunt.families.FirstOrDefault(value =>
                       value.baseFamilyId == familyId)?.totalDefeats ?? "0";
        }

        private static IEnumerable<ExpeditionRouteCardState089> AllCards090(
            ExpeditionDeckState089 deck) =>
            deck.CurrentRow.Concat(deck.DrawPile).Concat(deck.DiscardPile)
                .Concat(deck.BanishedCards);

        private static CampaignState Campaign090(
            IReadOnlyList<RecruitState> recruits = null)
        {
            return new CampaignState(
                "00000000-0000-0000-0000-000000000090",
                90090L,
                "SSS_TEST_090",
                ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD_SSS_090", 0,
                    recruits ?? Array.Empty<RecruitState>(),
                    Array.Empty<UnionState>()));
        }

        private static CampaignState WithCompletedDefinitions090(
            CampaignState campaign,
            params string[] definitionIds)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var worldGate = playable.WorldGate023.With(
                completedDefinitionIds: definitionIds);
            playable = playable.With(worldGate023: worldGate,
                replaceWorldGate023: true);
            progress = progress.With(playable020: playable,
                replacePlayable020: true);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true);
            city = city.With(strategic017H: strategic,
                replaceStrategic017H: true);
            return campaign.With(campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        private static WorldGateBoardRule023 Board090()
        {
            return new WorldGateBoardRule023
            {
                BoardId = "BOARD_SSS_090",
                DefinitionId = "DEFINITION_SSS_090",
                OperationKind = "REPEATABLE",
                WorldId = "SKYHOME",
                Title = "SSS Test Board",
                StartNodeId = "NODE_SSS_090",
                ExitNodeId = "NODE_SSS_090",
                Nodes = new[]
                {
                    new WorldGateNodeRule023
                    {
                        NodeId = "NODE_SSS_090",
                        Kind = "START",
                        Title = "Start",
                        ChoiceIds = new[] { "CONTINUE" },
                        NextNodeIds = Array.Empty<string>(),
                        RequiresCertifiedBattle = false
                    }
                }
            };
        }

        private static ExpeditionCardReceipt089 ContractReceipt090(
            ExpeditionRouteCardState089 card)
        {
            return new ExpeditionCardReceipt089(
                "SSS_CONTRACT_RECEIPT_090",
                "OP_090",
                card.CardId,
                card.NodeId,
                card.ChoiceId,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                "SUCCESS",
                0,
                0,
                Array.Empty<string>(),
                card.MomentumDelta,
                card.RecruitStableId,
                "SSS_CONTRACT_AUTHORITY_090",
                false);
        }

        private static string FindPassingTowerReceipt090(
            CampaignState campaign,
            int floor,
            int availableHeroCount)
        {
            for (var index = 0; index < 10000; index++)
            {
                var receipt = "TOWER_CLEAR_500_" + index.ToString("00000");
                SssTenV4AcquisitionService090.TowerOfferRoll090(
                    campaign.CampaignSeed, campaign.CampaignGuid, receipt, floor,
                    availableHeroCount, out var roll, out _);
                if (roll < SssAcquisition.DraftTowerChanceBasisPoints)
                    return receipt;
            }
            Assert.Fail("Deterministic search did not find a passing 1% fixture.");
            return null;
        }

        private static CampaignState Require(
            Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join(" | ", result.Errors));
            return result.Value;
        }
    }
}
