#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class ExpeditionDeck089Tests
    {
        private static IEnumerable<EquipmentItemState> OwnedChestItems092(CampaignState campaign) =>
            campaign.Guild.Inventory.Concat(campaign.Guild.Recruits.SelectMany(recruit =>
                recruit.Equipment.Assignments.Select(slot => slot.Item)));

        ExpeditionDeckService089 _deck;
        CampaignRegistry020 _registry020;
        Campaign020RuleCatalogAdapter _catalog020;
        CampaignRegistry023 _registry023;
        Campaign023RuleCatalogAdapter _catalog023;
        CampaignPlayableCommandService020 _campaign020;
        CampaignWorldGateCommandService023 _worldGate;
        ExpeditionDeckCommandService089 _commands;
        GuildCityBattleBridgeService017D _battleBridge;
        M2BattleCommandService _battles;
        M2CombatContent _combatContent;

        [SetUp]
        public void SetUp()
        {
            _deck = new ExpeditionDeckService089();
            _registry020 = CampaignRegistry020.LoadFromResources();
            _catalog020 = new Campaign020RuleCatalogAdapter(_registry020);
            _registry023 = CampaignRegistry023.LoadFromResources();
            _catalog023 = new Campaign023RuleCatalogAdapter(_registry023);
            _campaign020 = new CampaignPlayableCommandService020();
            _worldGate = new CampaignWorldGateCommandService023();
            _commands = new ExpeditionDeckCommandService089(_worldGate, _deck);
            _battleBridge = new GuildCityBattleBridgeService017D();
            _battles = new M2BattleCommandService();
            _combatContent = M2CombatContent.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT"));
        }

        [Test]
        public void SameSeedBuildsRealDeterministicThreeCardRouteRow()
        {
            var board = SimpleBoard();
            var first = _deck.Create(89001, "OP_089", board,
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>());
            var second = _deck.Create(89001, "OP_089", board,
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>());

            Assert.That(first.CurrentRow.Count,
                Is.EqualTo(ExpeditionDeckService089.RouteRowSize));
            Assert.That(first.CurrentRow.Select(value => value.CardId).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(first.CurrentRow.All(value => value.NodeId == "N0"), Is.True);
            Assert.That(first.CurrentRow.All(value => !value.AdvancesRoute), Is.True,
                "Every safe room must begin with a genuine three-card encounter row.");
            CollectionAssert.AreEqual(
                first.CurrentRow.Select(value => value.CardId),
                second.CurrentRow.Select(value => value.CardId));
            CollectionAssert.AreEqual(
                first.DrawPile.Select(value => value.CardId),
                second.DrawPile.Select(value => value.CardId));
        }

        [TestCase(2, "BATTLE", "Battle:")]
        [TestCase(3, "AMBUSH", "Ambush:")]
        [TestCase(4, "ELITE", "Elite Hunt:")]
        [TestCase(10, "BOSS", "Boss:")]
        public void CertifiedBattleCardsExposeReadableThreatTiers(
            int enemyUnionCount,
            string expectedCategory,
            string expectedTitlePrefix)
        {
            var deck = _deck.Create(89010 + enemyUnionCount,
                "OP_BATTLE_TIER_089_" + enemyUnionCount,
                BattleBoard(enemyUnionCount), Array.Empty<RecruitState>(),
                Array.Empty<EquipmentItemState>(), null, Array.Empty<string>());

            Assert.That(deck.CurrentRow, Has.Count.EqualTo(3));
            Assert.That(deck.CurrentRow.All(value =>
                value.Category == expectedCategory), Is.True);
            Assert.That(deck.CurrentRow.All(value =>
                value.VisualCategoryKey == "BATTLE"), Is.True);
            Assert.That(deck.CurrentRow.All(value =>
                value.Title.StartsWith(expectedTitlePrefix,
                    StringComparison.Ordinal)), Is.True);
            Assert.That(deck.CurrentRow.All(value =>
                value.Description.Contains(enemyUnionCount + " hostile Union")),
                Is.True);
        }

        [Test]
        public void RewardCardsRetainTheCurrentAuthoredStoryContext()
        {
            var deck = _deck.Create(89020, "OP_STORY_CONTEXT_089", SimpleBoard(),
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>());
            var chest = AllCards(deck).First(value => value.Category == "CHEST");
            var returnStory = AllCards(deck).First(value =>
                value.NodeId == "N1" && value.Category == "STORY");

            Assert.That(chest.Description,
                Does.Contain("Choose a road through the old gate district."));
            Assert.That(returnStory.Description,
                Does.Contain("Carry the expedition's discoveries home."));
            Assert.That(chest.Title, Is.Not.EqualTo("Sealed Expedition Chest"));
            Assert.That(chest.RewardPreview,
                Does.Match("(Common|Uncommon|Rare|Epic|Legendary|Godly) .+ • " +
                           "EQUIP PREVIEW \\+[0-9]+ PHYSICAL / \\+[0-9]+ MYSTIC" +
                           " • \\+10 Guild / Hall XP • 1 material"));
        }

        [Test]
        public void DeckItemsChangeCompositionAndPublishedOddsDeterministically()
        {
            var board = SimpleBoard();
            var plain = _deck.Create(89002, "OP_ITEM_089", board,
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>());
            var item = new EquipmentItemState("ITEM_INSTANCE_089",
                "ITEM_EXPEDITION_MAP_SCOUT_LENS", "Scout's Treasure Map",
                new[] { EquipmentSlotIds.ToolRelic },
                new[] { "EXPEDITION_MAP", "SCOUT_LENS" }, "QUALITY_A", 10000,
                false);
            var equipped = _deck.Create(89002, "OP_ITEM_089", board,
                Array.Empty<RecruitState>(), new[] { item }, null,
                Array.Empty<string>());
            var holder = new RecruitState("ITEM_HOLDER_089", 100, 100, 20, 20)
                .WithEquipment(new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(
                        EquipmentSlotIds.ToolRelic, item)
                }));
            var actuallyEquipped = _deck.Create(89002, "OP_ITEM_089", board,
                new[] { holder }, Array.Empty<EquipmentItemState>(), null,
                Array.Empty<string>());
            var deduplicated = _deck.Create(89002, "OP_ITEM_089", board,
                new[] { holder }, new[] { item }, null, Array.Empty<string>());

            var plainCards = AllCards(plain);
            var equippedCards = AllCards(equipped);
            Assert.That(equippedCards.Count(value => value.Category == "CHEST"),
                Is.GreaterThan(plainCards.Count(value => value.Category == "CHEST")));
            var plainCheck = plainCards.First(value => value.ResolutionDifficulty > 0);
            var matched = equippedCards.First(value => value.NodeId == plainCheck.NodeId &&
                value.Category == plainCheck.Category);
            Assert.That(matched.ItemCheckModifier, Is.GreaterThan(
                plainCheck.ItemCheckModifier));
            Assert.That(ExpeditionDeckService089.ChanceBasisPoints(
                    matched.ResolutionDifficulty, matched.ItemCheckModifier),
                Is.GreaterThan(ExpeditionDeckService089.ChanceBasisPoints(
                    plainCheck.ResolutionDifficulty, plainCheck.ItemCheckModifier)));
            CollectionAssert.Contains(equipped.ModifierLabels,
                "Treasure Map • adds chest cards");
            CollectionAssert.AreEqual(
                AllCards(equipped).Select(value => value.CardId),
                AllCards(actuallyEquipped).Select(value => value.CardId),
                "An item must keep its deck effect after it moves from Inventory into a recruit loadout.");
            CollectionAssert.AreEqual(
                AllCards(actuallyEquipped).Select(value => value.CardId),
                AllCards(deduplicated).Select(value => value.CardId),
                "The same authoritative item instance must never apply twice.");
        }


        [Test]
        public void CheckBreakdownUsesAndPublishesHeroChapterConditionAndTeamTerms()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89021);
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var operation = WorldGate(campaign).ActiveOperation;
            var card = AllCards(operation.ExpeditionDeck089).First(value =>
                value.ResolutionDifficulty > 0);
            var healthy = ExpeditionDeckService089.CheckModifierBreakdown089(
                campaign, operation, card, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089");

            var injuredRecruits = campaign.Guild.Recruits.Select(value =>
                value.RecruitId == "DECK_RECRUIT_A_089"
                    ? new RecruitState(value.RecruitId, 20, value.MaximumHp,
                        value.CurrentMp, value.MaximumMp)
                    : value).ToArray();
            var injuredGuild = campaign.Guild.With(
                campaign.Guild.TreasuryXp, injuredRecruits,
                campaign.Guild.Unions, campaign.Guild.Inventory,
                campaign.Guild.Development);
            var injuredCampaign = campaign.With(injuredGuild,
                campaign.OpeningFlow);
            var injured = ExpeditionDeckService089.CheckModifierBreakdown089(
                injuredCampaign, operation, card, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089");

            Assert.That(healthy.CardItemModifier, Is.EqualTo(card.ItemCheckModifier));
            Assert.That(healthy.RouteMomentumModifier,
                Is.EqualTo(operation.ExpeditionDeck089.Momentum));
            Assert.That(healthy.TeamModifier, Is.EqualTo(1));
            Assert.That(injured.ConditionModifier, Is.LessThan(
                healthy.ConditionModifier));
            Assert.That(injured.EffectiveModifier, Is.LessThan(
                healthy.EffectiveModifier));
            Assert.That(healthy.BaseModifier, Is.EqualTo(Math.Max(-4,
                Math.Min(4, healthy.CardItemModifier +
                    healthy.RouteMomentumModifier + healthy.GuildMemberModifier +
                    healthy.ChapterModifier + healthy.ConditionModifier))));
        }

        [Test]
        public void RecruitCardsWeightNewHeroesThreeToOneAndExposeOneOwnedAscensionCopy()
        {
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var blocked = heroes.NormalApplicantCandidates.First();
            var owned = new RecruitState(blocked.StableId, 100, 100, 20, 20);
            var deck = _deck.Create(89003, "OP_HERO_089", RecruitHeavyBoard(),
                new[] { owned }, Array.Empty<EquipmentItemState>(), heroes,
                Array.Empty<string>());
            var recruits = AllCards(deck).Where(value => value.Category == "RECRUIT")
                .ToArray();

            Assert.That(recruits.Length, Is.EqualTo(4));
            Assert.That(recruits.All(value => !string.IsNullOrWhiteSpace(
                value.RecruitStableId)), Is.True);
            Assert.That(recruits.All(value => value.RecruitRank != "SS"), Is.True);
            Assert.That(recruits.Select(value => value.RecruitStableId).Distinct().Count(),
                Is.EqualTo(recruits.Length));
            Assert.That(recruits.Count(value => value.RecruitOfferKind ==
                ExpeditionDeckService089.NewRecruitOfferKind089), Is.EqualTo(3));
            var ascension = recruits.Single(value => value.RecruitOfferKind ==
                ExpeditionDeckService089.AscensionOfferKind089);
            Assert.That(ascension.RecruitStableId, Is.EqualTo(blocked.StableId));
            Assert.That(ascension.Title, Does.StartWith("Ascension Copy:"));
            Assert.That(ascension.Description, Does.Contain(
                "Ascension to 1/" + RecruitAscensionRules089.MaximumLevel));
            Assert.That(ascension.RewardPreview, Does.Contain(
                "Ascension 1/" + RecruitAscensionRules089.MaximumLevel));

            var pendingBlocked = _deck.Create(89003, "OP_HERO_PENDING_089",
                RecruitHeavyBoard(), new[] { owned },
                Array.Empty<EquipmentItemState>(), heroes,
                new[] { blocked.StableId });
            Assert.That(AllCards(pendingBlocked).Any(value =>
                value.RecruitStableId == blocked.StableId), Is.False,
                "A pending exact lead cannot be offered simultaneously again.");

            var everyOtherHeroPending = heroes.NormalApplicantCandidates
                .Where(value => value.StableId != blocked.StableId)
                .Select(value => value.StableId)
                .ToArray();
            var onlyOwnedEligible = _deck.Create(
                89003, "OP_HERO_NO_DUPLICATE_089", RecruitHeavyBoard(),
                new[] { owned }, Array.Empty<EquipmentItemState>(), heroes,
                everyOtherHeroPending);
            Assert.That(AllCards(onlyOwnedEligible).Count(value =>
                value.RecruitStableId == blocked.StableId), Is.EqualTo(1),
                "One eligible owned hero can occupy only one simultaneous card offer.");
        }

        [Test]
        public void EncounterThenRouteSelectionsPersistAndKeepWorldGateEdgeSeparate()
        {
            var board = SimpleBoard();
            var deck = _deck.Create(89004, "OP_ADVANCE_089", board,
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>());
            var operation = Operation(deck, "N0");
            var node = board.Nodes.Single(value => value.NodeId == "N0");
            var chosen = deck.CurrentRow[0];
            var unchosen = deck.CurrentRow.Skip(1).Select(value => value.CardId).ToArray();
            var committed = _deck.CommitCard(89004, operation, node, chosen.CardId,
                "RECRUIT_A_089", "RECRUIT_B_089", CampaignFactory.CreateM0Proof(89004));
            Assert.That(committed.IsSuccess, Is.True,
                string.Join("\n", committed.Errors));
            var saved = JsonConvert.DeserializeObject<ExpeditionDeckState089>(
                JsonConvert.SerializeObject(committed.Value));
            Assert.That(_deck.ValidateCommittedReceipt(saved, saved.PendingReceipt),
                Is.True);
            if(saved.PendingReceipt.Effect132 != null)
            {
                var rolled=_deck.RollEffect132(saved,CampaignFactory.CreateM0Proof(89004),saved.PendingReceipt.ReceiptId);
                Assert.That(rolled.IsSuccess,Is.True,string.Join("\n",rolled.Errors));saved=rolled.Value;
            }
            var advanced = _deck.Advance(saved, "N0");

            Assert.That(advanced.PendingReceipt, Is.Null);
            Assert.That(advanced.DiscardPile.Any(value => value.CardId == chosen.CardId),
                Is.True);
            Assert.That(unchosen.All(id => advanced.DrawPile.Any(value =>
                value.CardId == id)), Is.True);
            Assert.That(advanced.CurrentRow.Count, Is.EqualTo(3));
            Assert.That(advanced.CurrentRow.All(value => value.NodeId == "N0" &&
                value.AdvancesRoute), Is.True,
                "Resolving the encounter must deal the route row without moving the graph.");
            Assert.That(advanced.AppliedReceiptIds.Count, Is.EqualTo(1));

            var routeChosen = advanced.CurrentRow[0];
            var routeCommitted = _deck.CommitCard(89004,
                Operation(advanced, "N0"), node, routeChosen.CardId,
                "RECRUIT_A_089", "RECRUIT_B_089");
            Assert.That(routeCommitted.IsSuccess, Is.True,
                string.Join("\n", routeCommitted.Errors));
            var moved = _deck.Advance(routeCommitted.Value, "N1");
            Assert.That(moved.CurrentRow.All(value => value.NodeId == "N1" &&
                !value.AdvancesRoute), Is.True,
                "The next room must begin with its own shuffled encounter row.");
            Assert.That(moved.AppliedReceiptIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void EveryAuthoredQuestPathHasAtLeastTenCardChoicesPlusLockedBattles()
        {
            foreach (var definitionId in _registry023.Boards.Keys
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                Assert.That(_catalog023.TryGetBoard(definitionId, out var board),
                    Is.True, definitionId);
                var minimum = ExpeditionDeckService089
                    .MinimumChoiceRoundsForBoard089(board);
                Assert.That(minimum,
                    Is.GreaterThanOrEqualTo(
                        ExpeditionDeckService089.MinimumQuestChoiceRounds089),
                    definitionId +
                    " must offer at least ten authoritative three-card choices; " +
                    "locked battles are additional and do not count toward that floor.");
            }
        }

        [Test]
        public void LiveEncounterReceiptAppliesExactlyOnceWithoutMovingWorldGate()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89022);
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var before = WorldGate(campaign).ActiveOperation;
            var encounter = before.ExpeditionDeck089.CurrentRow[0];
            Assert.That(encounter.AdvancesRoute, Is.False);

            var committed = Require(_commands.CommitRouteCard(campaign,
                _catalog023, encounter.CardId, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089"));
            if(WorldGate(committed).ActiveOperation.ExpeditionDeck089.PendingReceipt.Effect132 != null)
                committed=Require(_commands.RollCommittedEffect132(committed,_catalog023,
                    WorldGate(committed).ActiveOperation.ExpeditionDeck089.PendingReceipt.ReceiptId));
            var staged = WorldGate(committed).ActiveOperation;
            Assert.That(staged.PendingReceipt, Is.Null,
                "An encounter card must not manufacture a World Gate receipt.");
            Assert.That(staged.ExpeditionDeck089.PendingReceipt, Is.Not.Null);

            var applied = Require(_commands.ApplyWorldGateReceiptExactlyOnce(
                committed, _catalog023));
            var after = WorldGate(applied).ActiveOperation;
            Assert.That(after.CurrentNodeId, Is.EqualTo(before.CurrentNodeId));
            CollectionAssert.AreEqual(before.CompletedNodeIds,
                after.CompletedNodeIds);
            Assert.That(after.ExpeditionDeck089.CurrentRow,
                Has.Count.EqualTo(3));
            Assert.That(after.ExpeditionDeck089.CurrentRow.All(value =>
                value.AdvancesRoute), Is.True);
            Assert.That(applied.Guild.Development.HasAdventureAuthority(
                staged.ExpeditionDeck089.PendingReceipt.ReceiptId), Is.True);

            var replay = _commands.ApplyWorldGateReceiptExactlyOnce(applied,
                _catalog023);
            Assert.That(replay.IsSuccess, Is.False);
        }

        [Test]
        public void LiveCampaignCardRewardAppliesExactlyOnceAndSurvivesSaveReload()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89005);
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var active = WorldGate(campaign).ActiveOperation;
            Assert.That(active.ExpeditionDeck089.CurrentRow.Count, Is.EqualTo(3));
            var selected = active.ExpeditionDeck089.CurrentRow[0];
            campaign = Require(_commands.CommitRouteCard(campaign, _catalog023,
                selected.CardId, "DECK_RECRUIT_A_089", "DECK_RECRUIT_B_089"));
            campaign = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));
            if(WorldGate(campaign).ActiveOperation.ExpeditionDeck089.PendingReceipt.Effect132 != null)
                campaign=Require(_commands.RollCommittedEffect132(campaign,_catalog023,
                    WorldGate(campaign).ActiveOperation.ExpeditionDeck089.PendingReceipt.ReceiptId));
            var cardReceipt = WorldGate(campaign).ActiveOperation
                .ExpeditionDeck089.PendingReceipt;
            Assert.That(cardReceipt, Is.Not.Null);
            var before = campaign.Guild.TreasuryXp;
            campaign = Require(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023));

            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(before + cardReceipt.GuildXp));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                cardReceipt.ReceiptId), Is.True);
            Assert.That(campaign.Guild.Development.HasClaimedReward(
                cardReceipt.ReceiptId), Is.EqualTo(cardReceipt.GuildXp > 0));
            Assert.That(WorldGate(campaign).ActiveOperation.ExpeditionDeck089
                .AppliedReceiptIds, Does.Contain(cardReceipt.ReceiptId));
            var replay = _commands.ApplyWorldGateReceiptExactlyOnce(campaign,
                _catalog023);
            Assert.That(replay.IsSuccess, Is.False);
            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(before + cardReceipt.GuildXp));
        }

        [Test]
        public void SuccessfulLiveChestAddsOnePreviewedOwnedItemExactlyOnce()
        {
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            CampaignState committed = null;
            ExpeditionRouteCardState089 selected = null;
            ExpeditionCardReceipt089 receipt = null;
            for (var attempt = 0; attempt < 96 && committed == null; attempt++)
            {
                var candidate = CreateAtWorldBoard("CH018_001", "SKYHOME",
                    89100 + attempt);
                candidate = Require(_worldGate.BeginOperation(candidate,
                    _catalog023, "CH018_001", new[] { "DECK_UNION_089" },
                    _catalog020, heroes));
                var chest = WorldGate(candidate).ActiveOperation
                    .ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                        value.Category == "CHEST");
                if (chest == null) continue;
                var commit = _commands.CommitRouteCard(candidate, _catalog023,
                    chest.CardId, "DECK_RECRUIT_A_089",
                    "DECK_RECRUIT_B_089");
                if (!commit.IsSuccess) continue;
                var candidateReceipt = WorldGate(commit.Value).ActiveOperation
                    .ExpeditionDeck089.PendingReceipt;
                if (candidateReceipt == null ||
                    candidateReceipt.MaterialIds.Count == 0) continue;
                committed = commit.Value;
                selected = chest;
                receipt = candidateReceipt;
            }

            Assert.That(committed, Is.Not.Null,
                "A deterministic successful Chest route should be available in the bounded seed proof.");
            var beforeIds = new HashSet<string>(OwnedChestItems092(committed)
                .Select(value => value.InstanceId), StringComparer.Ordinal);
            var applied = Require(_commands.ApplyWorldGateReceiptExactlyOnce(
                committed, _catalog023));
            var added = OwnedChestItems092(applied).Where(value =>
                !beforeIds.Contains(value.InstanceId)).ToArray();
            Assert.That(added, Has.Length.EqualTo(1));
            Assert.That(added[0].InstanceId,
                Does.StartWith("LOOT_ITEM_070_EXP089_"));
            Assert.That(selected.RewardPreview,
                Does.Contain(added[0].DisplayName));
            Assert.That(added[0].PlayerLocked, Is.False,
                "Earned chest equipment stays manageable after a legal upgrade or inventory grant.");

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(applied));
            Assert.That(OwnedChestItems092(reloaded).Count(value =>
                    value.InstanceId == added[0].InstanceId),
                Is.EqualTo(1));
            var replay = _commands.ApplyWorldGateReceiptExactlyOnce(reloaded,
                _catalog023);
            Assert.That(replay.IsSuccess, Is.False);
            Assert.That(OwnedChestItems092(reloaded).Count(value =>
                    value.InstanceId == added[0].InstanceId),
                Is.EqualTo(1));
            Assert.That(receipt.ReceiptId, Is.Not.Empty);
        }

        [Test]
        public void TutorialAndRecruitLeadFieldsAreBackwardCompatibleSaveDefaults()
        {
            var legacyJson = JsonConvert.SerializeObject(WorldGateRuntimeState023.Default());
            var legacy = JsonConvert.DeserializeObject<WorldGateRuntimeState023>(legacyJson);
            Assert.That(legacy.ExpeditionDeckTutorialSeen089, Is.False);
            Assert.That(legacy.ExpeditionRecruitLeadIds089, Is.Empty);
            var updated = legacy.With(expeditionDeckTutorialSeen089: true,
                expeditionRecruitLeadIds089: new[] { "HERO_089_TEST" });
            var reloaded = JsonConvert.DeserializeObject<WorldGateRuntimeState023>(
                JsonConvert.SerializeObject(updated));
            Assert.That(reloaded.ExpeditionDeckTutorialSeen089, Is.True);
            CollectionAssert.AreEqual(new[] { "HERO_089_TEST" },
                reloaded.ExpeditionRecruitLeadIds089);
            Assert.That(ExpeditionDeckService089.TutorialSteps.Count, Is.EqualTo(5));
        }

        static WorldGateBoardRule023 SimpleBoard() => new WorldGateBoardRule023
        {
            BoardId = "BOARD_089_TEST",
            DefinitionId = "DEF_089_TEST",
            OperationKind = "CHAPTER",
            WorldId = "SKYHOME",
            StartNodeId = "N0",
            ExitNodeId = "N1",
            MaximumAlliedUnions = 1,
            Nodes = new[]
            {
                new WorldGateNodeRule023
                {
                    NodeId = "N0", Kind = "ROUTE", Title = "Crossroads",
                    Description = "Choose a road through the old gate district.",
                    NextNodeIds = new[] { "N1" },
                    ChoiceIds = new[] { "TAKE_HIGH_ROAD", "TAKE_LOW_ROAD" },
                    CheckDifficulty = 0
                },
                new WorldGateNodeRule023
                {
                    NodeId = "N1", Kind = "EXIT", Title = "Return",
                    Description = "Carry the expedition's discoveries home.",
                    NextNodeIds = Array.Empty<string>(),
                    ChoiceIds = new[] { "RETURN_HOME" }, CheckDifficulty = 0
                }
            }
        };

        static WorldGateBoardRule023 BattleBoard(int enemyUnionCount) =>
            new WorldGateBoardRule023
            {
                BoardId = "BOARD_BATTLE_TIER_089_" + enemyUnionCount,
                DefinitionId = "DEF_BATTLE_TIER_089_" + enemyUnionCount,
                OperationKind = "CHAPTER",
                WorldId = "SKYHOME",
                StartNodeId = "BATTLE_0",
                ExitNodeId = "BATTLE_0",
                MaximumAlliedUnions = 10,
                Nodes = new[]
                {
                    new WorldGateNodeRule023
                    {
                        NodeId = "BATTLE_0",
                        Kind = "BATTLE",
                        Title = "The Glassbound Vanguard",
                        Description = "The Guild reaches a named story confrontation.",
                        NextNodeIds = Array.Empty<string>(),
                        ChoiceIds = new[] { "ENGAGE" },
                        RequiresCertifiedBattle = true,
                        EnemyUnionCount = enemyUnionCount
                    }
                }
            };

        static WorldGateBoardRule023 RecruitHeavyBoard()
        {
            var nodes = Enumerable.Range(0, 4)
                .Select(index => new WorldGateNodeRule023
                {
                    NodeId = "RECRUIT_NODE_089_" + index,
                    Kind = "ROUTE",
                    Title = "Recruit Road " + index,
                    Description = "The Guild meets another traveler.",
                    NextNodeIds = index < 3
                        ? new[] { "RECRUIT_NODE_089_" + (index + 1) }
                        : Array.Empty<string>(),
                    ChoiceIds = new[] { "CONTINUE_" + index },
                    CheckDifficulty = 0
                })
                .ToArray();
            return new WorldGateBoardRule023
            {
                BoardId = "BOARD_RECRUIT_WEIGHT_089",
                DefinitionId = "DEF_RECRUIT_WEIGHT_089",
                OperationKind = "CHAPTER",
                WorldId = "SKYHOME",
                StartNodeId = nodes[0].NodeId,
                ExitNodeId = nodes[nodes.Length - 1].NodeId,
                MaximumAlliedUnions = 1,
                Nodes = nodes
            };
        }

        static WorldGateOperationState023 Operation(ExpeditionDeckState089 deck,
            string nodeId) => new WorldGateOperationState023(
            "OP_ADVANCE_089", "DEF_089_TEST", "BOARD_089_TEST", "CHAPTER",
            "SKYHOME", "SEED_089", nodeId, WorldGateOperationStatus023.Active,
            10, 0, 0, 0, 0, 0, 0, new[] { "UNION_089" }, Array.Empty<string>(),
            Array.Empty<string>(), null, string.Empty, "test",
            alliedRosterIdentity: "ROSTER_089",
            alliedRecruitIds: new[] { "RECRUIT_A_089", "RECRUIT_B_089" },
            expeditionDeck089: deck);

        [Test]
        public void ActivePreDeckSaveRequiresSafeRecoveryInsteadOfRetiredFallback()
        {
            Assert.That(ExpeditionDeckService089.RequiresDeckRecovery089(
                Operation(null, "N0")), Is.True);

            var deck = _deck.Create(74089, "OP_RECOVERY_089", SimpleBoard(),
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>());
            Assert.That(ExpeditionDeckService089.RequiresDeckRecovery089(
                Operation(deck, "N0")), Is.False);
        }

        [Test]
        public void ChestEquipmentSpansCommonThroughGodlyAndIsManualProgressionGear()
        {
            var qualities = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<string>(StringComparer.Ordinal);
            for (var quality = 0; quality < 256; quality++)
                for (var kind = 0; kind < 4; kind++)
                {
                    var cardId = "EXPCARD089_TEST_" + kind.ToString("X") +
                                 quality.ToString("X2");
                    var item = ExpeditionDeckService089.ChestEquipmentReward089(
                        cardId, "EXPREC089_TEST_" + kind + "_" + quality);
                    Assert.That(item.InstanceId,
                        Does.StartWith("LOOT_ITEM_070_EXP089_"));
                    Assert.That(item.PlayerLocked, Is.False);
                    Assert.That(item.ConditionBasisPoints, Is.EqualTo(10000));
                    Assert.That(item.ValidSlotIds, Has.Count.EqualTo(1));
                    Assert.That(item.EquipmentTags,
                        Does.Contain("EXPEDITION_CHEST_089"));
                    qualities.Add(item.QualityId);
                    slots.Add(item.ValidSlotIds[0]);
                }
            CollectionAssert.AreEquivalent(new[]
            {
                "QUALITY_COMMON", "QUALITY_UNCOMMON", "QUALITY_RARE",
                "QUALITY_EPIC", "QUALITY_LEGENDARY", "QUALITY_GODLY"
            }, qualities);
            CollectionAssert.AreEquivalent(new[]
            {
                EquipmentSlotIds.MainHand, EquipmentSlotIds.BodyArmor,
                EquipmentSlotIds.AccessoryOne
            }, slots);
        }

        [Test]
        public void ProgressionTierIsPureSavedAndExpandsTheAuthoredDeck()
        {
            var board = SimpleBoard();
            var early = _deck.Create(89300, "OP_GROWTH_089", board,
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>(), guildLevel: 1);
            var mature = _deck.Create(89300, "OP_GROWTH_089", board,
                Array.Empty<RecruitState>(), Array.Empty<EquipmentItemState>(),
                null, Array.Empty<string>(), guildLevel: 10);

            Assert.That(early.ProgressionTier, Is.EqualTo(1));
            Assert.That(early.CardsPerNodeAtCreation, Is.EqualTo(6));
            Assert.That(mature.ProgressionTier, Is.EqualTo(4));
            Assert.That(mature.CardsPerNodeAtCreation, Is.EqualTo(12));
            Assert.That(AllCards(mature).Count,
                Is.GreaterThan(AllCards(early).Count));
            Assert.That(mature.UnlockedCategoryIds,
                Contains.Item("NATURAL20_PERMANENT_HERO_BOON"));
            Assert.That(mature.UnlockedCategoryIds,
                Contains.Item("DEEP_MERCHANT_STOCK"));

            var reloaded = JsonConvert.DeserializeObject<ExpeditionDeckState089>(
                JsonConvert.SerializeObject(mature));
            Assert.That(reloaded.ProgressionTier,
                Is.EqualTo(mature.ProgressionTier));
            Assert.That(reloaded.CardsPerNodeAtCreation,
                Is.EqualTo(mature.CardsPerNodeAtCreation));
            CollectionAssert.AreEqual(mature.UnlockedCategoryIds,
                reloaded.UnlockedCategoryIds,
                "Deck unlock policy must be saved, never recomputed during a run.");
            CollectionAssert.AreEqual(
                mature.CurrentRow.Select(value => value.CardId),
                reloaded.CurrentRow.Select(value => value.CardId));
        }

        [Test]
        public void NewFateDecksAreDeterministicAndHideOutcomesUntilD20Roll132()
        {
            var hero=new RecruitState("PERMANENT_HERO_089",100,100,20,20);
            var kinds=new HashSet<string>(StringComparer.Ordinal);
            for(var attempt=0;attempt<24;attempt++)
            {
                var first=_deck.Create(89500+attempt,"OP_FATE132_"+attempt,SimpleBoard(),
                    new[]{hero},Array.Empty<EquipmentItemState>(),null,Array.Empty<string>(),guildLevel:3);
                var second=_deck.Create(89500+attempt,"OP_FATE132_"+attempt,SimpleBoard(),
                    new[]{hero},Array.Empty<EquipmentItemState>(),null,Array.Empty<string>(),guildLevel:3);
                Assert.That(CanonicalJson.Serialize(first),Is.EqualTo(CanonicalJson.Serialize(second)));
                Assert.That(AllCards(first).Any(card=>card.PermanentHeroEffectKind=="SCAR"),Is.False);
                foreach(var card in AllCards(first).Where(card=>ExpeditionDeckService089.EffectKind132(card).Length>0))
                {
                    kinds.Add(ExpeditionDeckService089.EffectKind132(card));
                    Assert.That(card.PermanentHeroRecruitId,Is.Empty);
                    Assert.That(card.PermanentHeroEffectId,Is.Empty);
                    Assert.That(card.RewardPreview,Is.EqualTo("MYSTERY • REVEALED AFTER THE ROLL"));
                    Assert.That(card.RewardPreview,Does.Not.Contain(hero.RecruitId));
                }
            }
            CollectionAssert.AreEquivalent(new[]{ExpeditionDeckService089.BoonD20132,
                ExpeditionDeckService089.CurseD20132,ExpeditionDeckService089.Wheel132},kinds);
        }

        [Test]
        public void MerchantSpendsExactTreasuryXpAndGrantsPreviewedItemOnce()
        {
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89301);
            campaign = WithTreasuryXp089(campaign, 1000);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var runtime = WorldGate(campaign);
            var operation = runtime.ActiveOperation;
            Assert.That(_catalog023.TryGetBoard(operation.DefinitionId,
                out var board), Is.True);
            var expanded = _deck.Create(campaign.CampaignSeed,
                operation.OperationId, board, campaign.Guild.Recruits,
                campaign.Guild.Inventory, heroes,
                runtime.ExpeditionRecruitLeadIds089, guildLevel: 6);
            var merchant = AllCards(expanded).First(value =>
                value.NodeId == operation.CurrentNodeId &&
                !value.AdvancesRoute && value.Category == "MERCHANT");
            expanded = ForceEncounterCardIntoRow089(expanded, merchant);
            operation = operation.With(expeditionDeck089: expanded,
                replaceExpeditionDeck089: true);
            campaign = WithWorldGate089(campaign, runtime.With(
                activeOperation: operation, replaceActiveOperation: true));

            var expectedPreview = ExpeditionDeckService089
                .MerchantEquipmentReward089(merchant.CardId,
                    "PREVIEW_" + merchant.CardId);
            var expectedPower = M2EquipmentPowerPolicy087.Resolve(
                expectedPreview);
            Assert.That(merchant.TreasuryXpCost, Is.GreaterThan(0));
            Assert.That(merchant.RewardPreview,
                Does.Contain("SPEND " + merchant.TreasuryXpCost + " XP"));
            Assert.That(merchant.RewardPreview,
                Does.Contain("PWR +" + expectedPower.PhysicalAttack));
            Assert.That(merchant.RewardPreview,
                Does.Contain("MYS +" + expectedPower.MysticAttack));

            var beforeXp = campaign.Guild.TreasuryXp;
            var beforeItems = campaign.Guild.Inventory.Count;
            campaign = Require(_commands.CommitRouteCard(campaign,
                _catalog023, merchant.CardId, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089"));
            var receipt = WorldGate(campaign).ActiveOperation
                .ExpeditionDeck089.PendingReceipt;
            Assert.That(receipt.TreasuryXpCost,
                Is.EqualTo(merchant.TreasuryXpCost));
            campaign = Require(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023));

            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(beforeXp - merchant.TreasuryXpCost));
            Assert.That(campaign.Guild.Inventory, Has.Count.EqualTo(beforeItems + 1));
            var purchased = campaign.Guild.Inventory.Last();
            Assert.That(purchased.DisplayName,
                Is.EqualTo(expectedPreview.DisplayName));
            CollectionAssert.AreEqual(expectedPreview.ValidSlotIds,
                purchased.ValidSlotIds);
            Assert.That(M2EquipmentPowerPolicy087.Resolve(purchased)
                .PhysicalAttack, Is.EqualTo(expectedPower.PhysicalAttack));
            Assert.That(M2EquipmentPowerPolicy087.Resolve(purchased)
                .MysticAttack, Is.EqualTo(expectedPower.MysticAttack));
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023).IsSuccess, Is.False);
            Assert.That(campaign.Guild.Inventory.Count(value =>
                    value.InstanceId == purchased.InstanceId), Is.EqualTo(1));
        }

        [Test]
        public void PermanentHeroBoonUsesRecruitProgressionAndAppliesOnce()
        {
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89302);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var runtime = WorldGate(campaign);
            var operation = runtime.ActiveOperation;
            var original = operation.ExpeditionDeck089.CurrentRow;
            var effectId = ExpeditionDeckService089.PermanentBoonPrefix089 +
                           "TEST_HERO_89302";
            var fate = new ExpeditionRouteCardState089(
                "EXPCARD089_PERMANENT_TEST_89302",
                operation.CurrentNodeId, original[0].ChoiceId,
                "PERMANENT", "BUFF", "Hero-Bound Wayglass Blessing",
                "Deck Recruit A permanently gains +1 on Expedition checks.",
                "PERMANENT", "SUCCESS • PERMANENT BOON",
                "Deck Recruit A • PERMANENT +1 EXPEDITION CHECKS",
                0, 0, 4, 4, Array.Empty<string>(), 0,
                string.Empty, string.Empty, string.Empty,
                "CAMPAIGN023|PERMANENT_TEST_089", string.Empty, false,
                0, string.Empty, 0, "DECK_RECRUIT_A_089",
                "Deck Recruit A", "BOON", effectId, 1);
            var current = new[] { fate, original[0], original[1] };
            var checkCard = AllCards(operation.ExpeditionDeck089).First(value =>
                value.NodeId == operation.CurrentNodeId && value.AdvancesRoute &&
                value.ResolutionDifficulty > 0);
            var stagedDeck = operation.ExpeditionDeck089.With(
                currentRow: current);
            operation = operation.With(expeditionDeck089: stagedDeck,
                replaceExpeditionDeck089: true);
            campaign = WithWorldGate089(campaign, runtime.With(
                activeOperation: operation, replaceActiveOperation: true));

            var before = ExpeditionDeckService089.CheckModifierBreakdown089(
                campaign, operation, checkCard, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089");
            // Compatibility fixture: the unchanged legacy receipt encoder
            // supplies an ALREADY committed pre132 boon. New uncommitted fate
            // cards intentionally use D20 instead.
            var legacyReceipt=(ExpeditionCardReceipt089)typeof(ExpeditionDeckService089)
                .GetMethod("Receipt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
                .Invoke(null,new object[]{operation.OperationId,fate,"DECK_RECRUIT_A_089","DECK_RECRUIT_B_089",
                    0,0,0,0,0,"SUCCESS",false,null,null,null});
            var legacyJson=CanonicalJson.Serialize(legacyReceipt);
            Assert.That(legacyJson,Does.Not.Contain("Effect132"));
            Assert.That(CanonicalJson.Serialize(JsonConvert.DeserializeObject<ExpeditionCardReceipt089>(legacyJson)),Is.EqualTo(legacyJson));
            stagedDeck=stagedDeck.With(pendingReceipt:legacyReceipt,replacePendingReceipt:true);
            campaign=WithWorldGate089(campaign,runtime.With(activeOperation:operation.With(
                expeditionDeck089:stagedDeck,replaceExpeditionDeck089:true),replaceActiveOperation:true));
            campaign = Require(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023));
            var recruit = campaign.Guild.Recruits.Single(value =>
                value.RecruitId == "DECK_RECRUIT_A_089");
            Assert.That(recruit.Progression.UnlockedTreeIds,
                Contains.Item(effectId));
            var afterOperation = WorldGate(campaign).ActiveOperation;
            var after = ExpeditionDeckService089.CheckModifierBreakdown089(
                campaign, afterOperation, checkCard, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089");
            Assert.That(after.PermanentHeroModifier,
                Is.EqualTo(before.PermanentHeroModifier + 1));
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023).IsSuccess, Is.False);
            Assert.That(recruit.Progression.UnlockedTreeIds.Count(value =>
                value == effectId), Is.EqualTo(1));
        }

        [Test]
        public void RareChestRecruitLeadIsPreviewedAndPersistedExactlyOnce()
        {
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89303);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var runtime = WorldGate(campaign);
            var operation = runtime.ActiveOperation;
            Assert.That(_catalog023.TryGetBoard(operation.DefinitionId,
                out var board), Is.True);
            ExpeditionDeckState089 foundDeck = null;
            ExpeditionRouteCardState089 chest = null;
            for (var attempt = 0; attempt < 160 && chest == null; attempt++)
            {
                var candidate = _deck.Create(89400 + attempt,
                    operation.OperationId, board, campaign.Guild.Recruits,
                    campaign.Guild.Inventory, heroes,
                    runtime.ExpeditionRecruitLeadIds089, guildLevel: 3);
                chest = AllCards(candidate).FirstOrDefault(value =>
                    value.NodeId == operation.CurrentNodeId &&
                    !value.AdvancesRoute && value.Category == "CHEST" &&
                    !string.IsNullOrWhiteSpace(value.RecruitStableId));
                if (chest != null) foundDeck = candidate;
            }
            Assert.That(chest, Is.Not.Null,
                "The bounded deterministic proof must include the rare 1-in-16 chest contact.");
            Assert.That(chest.Description,
                Does.Contain(chest.RecruitName));
            Assert.That(chest.RewardPreview,
                Does.Contain(chest.RecruitName));

            foundDeck = ForceEncounterCardIntoRow089(foundDeck, chest);
            operation = operation.With(expeditionDeck089: foundDeck,
                replaceExpeditionDeck089: true);
            campaign = WithWorldGate089(campaign, runtime.With(
                activeOperation: operation, replaceActiveOperation: true));
            campaign = Require(_commands.CommitRouteCard(campaign,
                _catalog023, chest.CardId, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089"));
            var receiptId = WorldGate(campaign).ActiveOperation
                .ExpeditionDeck089.PendingReceipt.ReceiptId;
            campaign = Require(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023));
            CollectionAssert.Contains(WorldGate(campaign)
                .ExpeditionRecruitLeadIds089, chest.RecruitStableId);
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                receiptId), Is.True);
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(
                campaign, _catalog023).IsSuccess, Is.False);
            Assert.That(WorldGate(campaign).ExpeditionRecruitLeadIds089.Count(
                value => value == chest.RecruitStableId), Is.EqualTo(1));
        }

        [Test]
        public void OptionalBattleUsesCertifiedBridgeAndReturnsToSameCardRoomOnce()
        {
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89304);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020, heroes));
            var before = WorldGate(campaign).ActiveOperation;
            var battleCard = before.ExpeditionDeck089.CurrentRow.Single(value =>
                ExpeditionDeckService089.IsOptionalBattleCard089(value));

            campaign = Require(_commands.CommitRouteCard(campaign,
                _catalog023, battleCard.CardId, "DECK_RECRUIT_A_089",
                "DECK_RECRUIT_B_089"));
            var staged = WorldGate(campaign).ActiveOperation;
            var cardReceipt = staged.ExpeditionDeck089.PendingReceipt;
            Assert.That(staged.CurrentNodeId, Is.EqualTo(before.CurrentNodeId));
            Assert.That(staged.PendingReceipt, Is.Null);
            Assert.That(cardReceipt.RequiresCertifiedBattle, Is.True);
            Assert.That(campaign.Guild.GuildCity.PendingEncounter.RequestId,
                Is.EqualTo(cardReceipt.BattleRequestId));
            Assert.That(_commands.HasPendingOptionalBattle089(campaign), Is.True);

            campaign = Require(_battleBridge.StartCertifiedEncounter(campaign,
                _battles, _combatContent));
            Assert.That(campaign.Battle.BattleId,
                Is.EqualTo(cardReceipt.BattleId));
            Assert.That(campaign.Battle.EnemyUnions,
                Has.Count.EqualTo(battleCard.EnemyUnionCount));
            campaign = ResolveBattle089(campaign);
            campaign = Require(_battles.ClaimBattleRewards(campaign));
            campaign = Require(_battleBridge.CommitBattleReturn(campaign));
            campaign = Require(_battleBridge.ApplyBattleReturnExactlyOnce(campaign));
            campaign = Require(_commands.SynchronizeOptionalBattleAfterClaim089(
                campaign, _catalog023));

            var returned = WorldGate(campaign).ActiveOperation;
            Assert.That(returned.CurrentNodeId, Is.EqualTo(before.CurrentNodeId),
                "An optional encounter returns to its exact quest room.");
            Assert.That(returned.ExpeditionDeck089.PendingReceipt, Is.Null);
            Assert.That(returned.ExpeditionDeck089.CurrentRow,
                Has.Count.EqualTo(3));
            Assert.That(returned.ExpeditionDeck089.CurrentRow.All(value =>
                value.AdvancesRoute), Is.True);
            Assert.That(returned.ExpeditionDeck089.AppliedReceiptIds.Count(value =>
                value == cardReceipt.ReceiptId), Is.EqualTo(1));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                cardReceipt.ReceiptId), Is.True);
            Assert.That(_commands.SynchronizeOptionalBattleAfterClaim089(
                campaign, _catalog023).IsSuccess, Is.False);
        }

        CampaignState ResolveBattle089(CampaignState campaign)
        {
            for (var round = 0; round < 100 &&
                 campaign.Battle.Outcome == BattleOutcome.InProgress; round++)
            {
                var active = campaign.Battle.PlayerUnions.Where(value =>
                    !value.Retreated && !value.IsDefeated).ToArray();
                foreach (var union in active)
                {
                    var forecast = campaign.Battle.CommittedForecasts
                        .First(value => StringComparer.Ordinal.Equals(
                            value.UnionId, union.UnionId));
                    campaign = Require(_battles.SelectForecast(campaign,
                        union.UnionId, forecast.ForecastId));
                }
                campaign = Require(_battles.ConfirmRound(campaign,
                    _combatContent));
            }
            Assert.That(campaign.Battle.Outcome,
                Is.Not.EqualTo(BattleOutcome.InProgress));
            Assert.That(campaign.Battle.Phase, Is.EqualTo(BattlePhase.Resolved));
            return campaign;
        }

        static ExpeditionDeckState089 ForceEncounterCardIntoRow089(
            ExpeditionDeckState089 deck,
            ExpeditionRouteCardState089 selected)
        {
            var all = AllCards(deck);
            var row = new List<ExpeditionRouteCardState089> { selected };
            row.AddRange(all.Where(value => value.CardId != selected.CardId &&
                    value.NodeId == selected.NodeId && !value.AdvancesRoute)
                .Take(2));
            Assert.That(row, Has.Count.EqualTo(3));
            var rowIds = new HashSet<string>(row.Select(value => value.CardId),
                StringComparer.Ordinal);
            return deck.With(
                currentRow: row.AsReadOnly(),
                drawPile: all.Where(value => !rowIds.Contains(value.CardId))
                    .ToArray(),
                discardPile: Array.Empty<ExpeditionRouteCardState089>(),
                banishedCards: Array.Empty<ExpeditionRouteCardState089>());
        }

        static CampaignState WithTreasuryXp089(CampaignState campaign,
            long treasuryXp)
        {
            var guild = campaign.Guild.With(treasuryXp,
                campaign.Guild.Recruits, campaign.Guild.Unions,
                campaign.Guild.Inventory, campaign.Guild.Development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        static CampaignState WithWorldGate089(CampaignState campaign,
            WorldGateRuntimeState023 runtime)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                worldGate023: runtime, replaceWorldGate023: true,
                lastCheckpointId: runtime.LastCheckpointId);
            progress = progress.With(playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: runtime.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: runtime.LastCheckpointId);
            city = city.With(strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: runtime.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        CampaignState CreateAtWorldBoard(string chapterId, string worldId, long seed)
        {
            var recruitA = new RecruitState("DECK_RECRUIT_A_089", 100, 100, 20, 20);
            var recruitB = new RecruitState("DECK_RECRUIT_B_089", 100, 100, 20, 20);
            var union = new UnionState("DECK_UNION_089", "Expedition Union",
                UnionKind.Normal, recruitA.RecruitId,
                new[] { recruitA.RecruitId, recruitB.RecruitId },
                "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(source.Guild.GuildId,
                source.Guild.TreasuryXp, new[] { recruitA, recruitB },
                new[] { union }, source.Guild.Inventory, source.Guild.Development,
                guildCity: null);
            var completedOpening = new OpeningFlowState(
                OpeningStage.Complete,
                civicCharterAccepted: true,
                applicantBoard: source.OpeningFlow?.ApplicantBoard,
                equipmentReviewCompleted: true,
                lastCheckpointId: "expedition_089_post_opening_fixture");
            var profile = new NewGuildProfileState(
                "Expedition Deck Tester",
                GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            var campaign = new CampaignState(
                source.CampaignGuid,
                source.CampaignSeed,
                source.ContentAuthorityVersion,
                source.Rules,
                guild,
                profile,
                completedOpening,
                source.Battle);
            var city = campaign.Guild.GuildCity;
            var progress = city.Strategic017H.Campaign019.With(
                activeChapterId: chapterId,
                unlockedWorldIds: new[] { "SKYHOME", worldId }
                    .Distinct(StringComparer.Ordinal).ToArray(),
                lastCheckpointId: "chapter_active_089");
            var strategic = city.Strategic017H.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: progress.LastCheckpointId);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city.With(
                strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: progress.LastCheckpointId)), campaign.OpeningFlow);
            campaign = Require(_campaign020.BeginOperation(campaign, _catalog020,
                chapterId));
            campaign = Require(_campaign020.CommitNonBattleStep(campaign,
                _catalog020, "SUCCESS"));
            return Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                _catalog020));
        }

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023;

        static IReadOnlyList<ExpeditionRouteCardState089> AllCards(
            ExpeditionDeckState089 deck) => deck.CurrentRow.Concat(deck.DrawPile)
                .Concat(deck.DiscardPile).Concat(deck.BanishedCards).ToArray();

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
