using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.Campaign023
{
    /// <summary>
    /// Deterministic card-deck projection over the authored Campaign023 graph.
    /// Cards decide a legal authored edge and add a small, exact-once expedition
    /// reward; battle cards remain locked to the certified Union battle engine.
    /// </summary>
    public sealed partial class ExpeditionDeckService089
    {
        public const int RouteRowSize = 3;
        public const int CardsPerNode = 6;
        public const int ChoiceRowsPerNonBattleNode089 = 2;
        public const int MinimumQuestChoiceRounds089 = 10;
        public const string NewRecruitOfferKind089 = "NEW_RECRUIT";
        public const string AscensionOfferKind089 = "ASCENSION";
        public const string PermanentBoonPrefix089 =
            "EXPEDITION_PERMANENT_BOON_089_";
        public const string PermanentScarPrefix089 =
            "EXPEDITION_PERMANENT_SCAR_089_";
        public const int ReceiptAuthoritySchemaVersion089 = 2;

        public static readonly IReadOnlyList<string> TutorialSteps =
            Array.AsReadOnly(new[]
            {
                "Each quest has a shuffled Expedition Deck with at least 10 real card-choice rounds. Locked story battles happen between those rounds and never replace them.",
                "Every safe room shuffles three face-down encounter cards, then three face-down route cards. Pick one each round; the other two return unseen to the deck.",
                "Only your chosen card flips. Discover its event, reward, and any dice check, then use its revealed action. Nothing behind the other cards is shown.",
                "Optional battle cards use your full Unions and return to the same quest room. Merchants show their exact XP cost and equipment power before purchase.",
                "Chests can grant Common through Godly gear. Roll D20 blessings and temporary curses, or spin the Fortune Wheel. A natural 20 blessing may grant a permanent owned-hero boon."
            });

        public ExpeditionDeckState089 Create(
            long campaignSeed,
            string operationId,
            WorldGateBoardRule023 board,
            IReadOnlyList<RecruitState> ownedRecruits,
            IReadOnlyList<EquipmentItemState> inventory,
            HeroMaster300Catalog087 heroCatalog,
            IReadOnlyList<string> previouslyEarnedLeadIds,
            int guildLevel = 1,
            SssSave sssProgression = null)
        {
            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentException("Operation ID is required.", nameof(operationId));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.Nodes == null || board.Nodes.Count == 0)
                throw new ArgumentException("A deck requires authored board nodes.", nameof(board));

            var seed = SemanticSeed.Derive(campaignSeed, "EXPEDITION_DECK_089",
                operationId, board.BoardId);
            var profile = ItemProfile089.From(inventory, ownedRecruits);
            var heroPool = EligibleHeroes(heroCatalog, ownedRecruits,
                previouslyEarnedLeadIds,
                new Pcg32(seed.Stream, seed.Seed)).ToList();
            var progressionTier = ProgressionTierForGuildLevel089(guildLevel);
            var cardsPerNode = CardsPerNodeForProgressionTier089(
                progressionTier);
            var unlockedCategories = UnlockedCategoriesForTier089(
                progressionTier);
            var heroCursor = 0;
            var cards = new List<ExpeditionRouteCardState089>();
            foreach (var node in board.Nodes)
            {
                if (node == null) continue;
                var choices = BoardAdventureRules084.OrderedChoices084(
                    operationId, node.NodeId,
                    node.ChoiceIds ?? Array.Empty<string>());
                if (choices.Count == 0) choices = new[] { "CONTINUE" };
                var nodeCardCount = node.RequiresCertifiedBattle
                    ? CardsPerNode : cardsPerNode;
                var phaseCardCount = node.RequiresCertifiedBattle
                    ? RouteRowSize : nodeCardCount / 2;
                for (var index = 0; index < nodeCardCount; index++)
                {
                    var advancesRoute = node.RequiresCertifiedBattle ||
                                        index >= phaseCardCount;
                    var phaseIndex = advancesRoute
                        ? index - phaseCardCount : index;
                    var category = CategoryFor089(operationId, node,
                        advancesRoute, phaseIndex, progressionTier);
                    if (profile.HasTreasureMap &&
                        !node.RequiresCertifiedBattle && !advancesRoute &&
                        phaseIndex == phaseCardCount - 1)
                        category = "CHEST";
                    HeroMaster300Hero087 hero = null;
                    RecruitState ownedHero = null;
                    RecruitState permanentHero = null;
                    if (StringComparer.Ordinal.Equals(category, "RECRUIT"))
                    {
                        if (heroCursor >= heroPool.Count) category = "STORY";
                        else
                        {
                            hero = heroPool[heroCursor++];
                            ownedHero = FindOwnedHero089(ownedRecruits, hero);
                        }
                    }
                    else if (StringComparer.Ordinal.Equals(category, "CHEST") &&
                             progressionTier >= 2 && heroCursor < heroPool.Count &&
                             ChestCarriesRecruitLead089(campaignSeed,
                                 operationId, node.NodeId, index))
                    {
                        // A rare chest contact uses the same authoritative lead
                        // pool as a Recruit card. Consuming the pool cursor keeps
                        // one hero from appearing in two simultaneous offers.
                        hero = heroPool[heroCursor++];
                        ownedHero = FindOwnedHero089(ownedRecruits, hero);
                    }
                    if (StringComparer.Ordinal.Equals(category, "PERMANENT"))
                    {
                        permanentHero = PermanentHero089(ownedRecruits,
                            operationId, node.NodeId, index);
                        if (permanentHero == null) category = "BUFF";
                    }
                    cards.Add(DecorateEffectCard132(BuildCard(operationId, board, node,
                        choices[index % choices.Count], category, index, profile,
                        hero, ownedHero, permanentHero, advancesRoute,
                        progressionTier)));
                }
            }
            var sssContract = SssTenV4AcquisitionService090
                .CreateCampaignContractCandidate090(campaignSeed, operationId,
                    board, ownedRecruits, sssProgression);
            if (sssContract != null) cards.Add(sssContract);
            Shuffle(cards, new Pcg32(seed.Seed, seed.Stream));
            var deckId = "EXPDECK089_" + CanonicalJson.Sha256Hex(new
            {
                operationId,
                board.BoardId,
                Seed = seed.ToString(),
                Profile = profile.Identity,
                ProgressionTier = progressionTier,
                CardsPerNode = cardsPerNode,
                UnlockedCategories = unlockedCategories,
                HeroPool = string.Join("|", heroPool.Select(hero =>
                {
                    var ownedHero = FindOwnedHero089(ownedRecruits, hero);
                    return hero.StableId + "@" +
                           (ownedHero == null
                               ? NewRecruitOfferKind089
                               : AscensionOfferKind089 +
                                 (ownedHero.Progression?.AscensionLevel ?? 0));
                })),
                SssContractCardId090 = sssContract?.CardId ?? string.Empty
            }).Substring(0, 24).ToUpperInvariant();
            var labels = new List<string>(profile.Labels)
            {
                "Guild deck tier " + progressionTier +
                " • " + cardsPerNode + " cards per safe room"
            };
            var draft = new ExpeditionDeckState089(
                ExpeditionDeckState089.Version, deckId, operationId,
                seed.ToString(), cards.AsReadOnly(),
                Array.Empty<ExpeditionRouteCardState089>(),
                Array.Empty<ExpeditionRouteCardState089>(),
                Array.Empty<ExpeditionRouteCardState089>(), null,
                Array.Empty<ExpeditionCardReceipt089>(), Array.Empty<string>(),
                0, labels.AsReadOnly(), Array.Empty<string>(), 0,
                progressionTier, unlockedCategories, cardsPerNode);
            return DrawRow(draft, board.StartNodeId);
        }

        public Result<ExpeditionDeckState089> CommitCard(
            long campaignSeed,
            WorldGateOperationState023 operation,
            WorldGateNodeRule023 node,
            string cardId,
            string actorRecruitId,
            string assistantRecruitId,
            CampaignState campaign = null)
        {
            var deck = operation?.ExpeditionDeck089;
            if (operation == null || node == null || deck == null)
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_ACTIVE_DECK_REQUIRED");
            if (deck.PendingReceipt != null || operation.PendingReceipt != null)
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_APPLY_PENDING_CARD_FIRST");
            var card = deck.CurrentRow.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.CardId, cardId));
            if (card == null || !StringComparer.Ordinal.Equals(card.NodeId, node.NodeId))
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_ROUTE_CARD_NOT_IN_CURRENT_ROW");
            var legalChoices = node.ChoiceIds ?? Array.Empty<string>();
            if ((legalChoices.Count > 0 && !legalChoices.Contains(card.ChoiceId)) ||
                (legalChoices.Count == 0 && !StringComparer.Ordinal.Equals(
                    card.ChoiceId, "CONTINUE")))
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_CARD_EDGE_ILLEGAL");
            if (node.RequiresCertifiedBattle)
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_CERTIFIED_BATTLE_CARD_LOCKED");

            if (!card.AdvancesRoute && EffectKind132(card).Length == 0 && CanOfferEffect132(card))
            {
                // Authenticate the existing current-row CardId and authored
                // edge above, then upgrade only this still-uncommitted choice
                // in the same save as its sealed receipt. Retain its identity.
                card = DecorateEffectCard132(card,preserveIdentity:true);
                deck = deck.With(currentRow:deck.CurrentRow.Select(value=>
                    value.CardId == cardId ? card : value).ToArray());
            }
            if (EffectKind132(card).Length > 0)
                return CommitEffect132(deck, card, campaign);
            var optionalBattle = IsOptionalBattleCard089(card);
            if (card.TreasuryXpCost > 0 &&
                (campaign?.Guild?.TreasuryXp ?? 0) < card.TreasuryXpCost)
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_TREASURY_XP_INSUFFICIENT");
            if (optionalBattle)
            {
                var city = campaign?.Guild?.GuildCity;
                if (city?.Expedition == null || city.ActiveContract == null)
                    return Result<ExpeditionDeckState089>.Failure(
                        "EXPEDITION089_BATTLE_EXPEDITION_REQUIRED");
                if (city.PendingEncounter != null ||
                    city.PendingBattleReturn != null ||
                    campaign.Battle != null &&
                    (campaign.Battle.Phase != BattlePhase.Resolved ||
                     campaign.Battle.Reward == null ||
                     !campaign.Battle.Reward.Claimed))
                    return Result<ExpeditionDeckState089>.Failure(
                        "EXPEDITION089_BATTLE_AUTHORITY_BUSY");
                if (operation.AlliedUnionIds == null ||
                    operation.AlliedUnionIds.Count == 0)
                    return Result<ExpeditionDeckState089>.Failure(
                        "EXPEDITION089_ALLIED_UNION_REQUIRED");
            }

            var difficulty = optionalBattle ? 0 : card.AdvancesRoute && node.CheckDifficulty > 0
                ? node.CheckDifficulty : card.ResolutionDifficulty;
            // Encounter rows resolve against the saved deck receipt only.  The
            // authored World Gate check remains authoritative when the second
            // three-card row actually chooses a route edge.
            var delegated = card.AdvancesRoute && node.CheckDifficulty > 0;
            var hasAssistant = !string.IsNullOrWhiteSpace(assistantRecruitId) &&
                               !StringComparer.Ordinal.Equals(actorRecruitId,
                                    assistantRecruitId);
            if (difficulty > 0 && string.IsNullOrWhiteSpace(actorRecruitId))
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_CHECK_ACTOR_REQUIRED");
            if (!operation.AlliedRecruitIds.Contains(actorRecruitId ?? string.Empty) &&
                difficulty > 0)
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_CHECK_ACTOR_NOT_COMMITTED");
            if (hasAssistant &&
                !operation.AlliedRecruitIds.Contains(assistantRecruitId))
                return Result<ExpeditionDeckState089>.Failure(
                    "EXPEDITION089_CHECK_ASSISTANT_NOT_COMMITTED");

            var modifierBreakdown = CheckModifierBreakdown089(
                campaign, operation, card, actorRecruitId, assistantRecruitId);
            var baseModifier = difficulty > 0
                ? modifierBreakdown.BaseModifier : 0;
            var dieOne = 0;
            var dieTwo = 0;
            var effective = difficulty > 0
                ? modifierBreakdown.EffectiveModifier : 0;
            var outcome = optionalBattle ? "BATTLE_PENDING" :
                delegated ? "WORLD_GATE_CHECK" : "SUCCESS";
            if (difficulty > 0 && !delegated)
            {
                var rollSeed = SemanticSeed.Derive(campaignSeed, operation.OperationId,
                    deck.DeckId, card.CardId, actorRecruitId ?? string.Empty,
                    assistantRecruitId ?? string.Empty);
                var rng = new Pcg32(rollSeed.Seed, rollSeed.Stream);
                dieOne = rng.NextInclusive(1, 6);
                dieTwo = rng.NextInclusive(1, 6);
                outcome = Outcome(dieOne + dieTwo + effective, difficulty);
            }
            if (difficulty == 0)
            {
                actorRecruitId = string.Empty;
                assistantRecruitId = string.Empty;
            }
            var battlePreStateHash = optionalBattle
                ? CanonicalJson.Sha256Hex(campaign) : string.Empty;
            var provisional = Receipt(operation.OperationId, card, actorRecruitId,
                assistantRecruitId, dieOne, dieTwo, baseModifier, effective,
                difficulty, outcome, delegated, null, battlePreStateHash);
            return Result<ExpeditionDeckState089>.Success(deck.With(
                pendingReceipt: provisional, replacePendingReceipt: true));
        }

        public ExpeditionCardReceipt089 FinalizeAgainstWorldGate(
            ExpeditionDeckState089 deck,
            WorldGateNodeReceipt023 worldGateReceipt)
        {
            var pending = deck?.PendingReceipt ?? throw new ArgumentNullException(
                nameof(deck));
            if (!pending.DelegatedToWorldGateCheck) return pending;
            if (worldGateReceipt == null ||
                !StringComparer.Ordinal.Equals(pending.OperationId,
                    worldGateReceipt.OperationId) ||
                !StringComparer.Ordinal.Equals(pending.NodeId,
                    worldGateReceipt.NodeId) ||
                !StringComparer.Ordinal.Equals(pending.ChoiceId,
                    worldGateReceipt.ChoiceId))
                throw new InvalidOperationException(
                    "World Gate receipt does not match the selected route card.");
            var card = FindCard(deck, pending.CardId) ?? throw new InvalidOperationException(
                "The selected route card is missing from its deck.");
            return Receipt(pending.OperationId, card,
                worldGateReceipt.ActorRecruitId, worldGateReceipt.AssistantRecruitId,
                worldGateReceipt.DieOne, worldGateReceipt.DieTwo,
                pending.BaseCheckModifier, worldGateReceipt.Modifier,
                worldGateReceipt.DieOne > 0 ? pending.Difficulty : 0,
                worldGateReceipt.Outcome, true, worldGateReceipt);
        }

        public ExpeditionDeckState089 FinalizeOptionalBattle089(
            ExpeditionDeckState089 deck,
            BattleState battle,
            BattleReturnReceipt017D battleReturn)
        {
            var pending = deck?.PendingReceipt ?? throw new ArgumentNullException(
                nameof(deck));
            var card = FindCard(deck, pending.CardId) ??
                       throw new InvalidOperationException(
                           "The selected battle card is missing from its deck.");
            if (!IsOptionalBattleCard089(card) ||
                !pending.RequiresCertifiedBattle ||
                string.IsNullOrWhiteSpace(pending.BattleRequestId) ||
                string.IsNullOrWhiteSpace(pending.BattleId))
                throw new InvalidOperationException(
                    "A saved optional battle card receipt is required.");
            if (battle == null || battleReturn == null ||
                battle.Phase != BattlePhase.Resolved || battle.Reward == null ||
                !battle.Reward.Claimed ||
                !StringComparer.Ordinal.Equals(battle.BattleId,
                    pending.BattleId) ||
                !StringComparer.Ordinal.Equals(battleReturn.LaunchRequestId,
                    pending.BattleRequestId) ||
                !StringComparer.Ordinal.Equals(battleReturn.BattleRunId,
                    pending.BattleId) ||
                !StringComparer.Ordinal.Equals(battleReturn.Outcome,
                    battle.Outcome.ToString()) ||
                !StringComparer.Ordinal.Equals(battleReturn.BattleResultHash,
                    battle.FinalStateHash) ||
                !StringComparer.Ordinal.Equals(
                    battleReturn.EquipmentRewardReceiptId,
                    battle.Reward.RewardId))
                throw new InvalidOperationException(
                    "The certified battle return does not match the saved card.");
            var finalized = Receipt(pending.OperationId, card,
                pending.ActorRecruitId, pending.AssistantRecruitId, 0, 0, 0, 0,
                0, battle.Outcome.ToString().ToUpperInvariant(), false, null,
                pending.BattlePreStateHash, battleReturn);
            if (!StringComparer.Ordinal.Equals(finalized.ReceiptId,
                    pending.ReceiptId) ||
                !StringComparer.Ordinal.Equals(finalized.BattleRequestId,
                    pending.BattleRequestId) ||
                !StringComparer.Ordinal.Equals(finalized.BattleId,
                    pending.BattleId))
                throw new InvalidOperationException(
                    "The optional battle linkage changed after combat.");
            return deck.With(pendingReceipt: finalized,
                replacePendingReceipt: true);
        }

        public static bool IsOptionalBattleCard089(
            ExpeditionRouteCardState089 card) => card != null &&
            !card.AdvancesRoute &&
            StringComparer.Ordinal.Equals(card.Category, "BATTLE") &&
            !string.IsNullOrWhiteSpace(card.EncounterId) &&
            card.EnemyUnionCount > 0;

        public bool ValidateCommittedReceipt(
            ExpeditionDeckState089 deck,
            ExpeditionCardReceipt089 receipt)
        {
            if (deck == null || receipt == null ||
                !StringComparer.Ordinal.Equals(deck.OperationId, receipt.OperationId) ||
                !StringComparer.Ordinal.Equals(receipt.ReceiptId,
                    ReceiptId(receipt.OperationId, receipt.CardId)) ||
                !StringComparer.Ordinal.Equals(receipt.AuthoritativeHash,
                    ReceiptHash(receipt))) return false;
            var card = FindCard(deck, receipt.CardId);
            if (card == null ||
                !StringComparer.Ordinal.Equals(card.NodeId, receipt.NodeId) ||
                !StringComparer.Ordinal.Equals(card.ChoiceId, receipt.ChoiceId))
                return false;
            if (!ValidateEffect132(deck, card, receipt)) return false;
            if (receipt.AuthoritySchemaVersion <
                ReceiptAuthoritySchemaVersion089) return true;
            var battleCard = IsOptionalBattleCard089(card);
            return receipt.TreasuryXpCost == card.TreasuryXpCost &&
                   receipt.RequiresCertifiedBattle == battleCard &&
                   (!battleCard ||
                    (!string.IsNullOrWhiteSpace(receipt.BattleRequestId) &&
                     !string.IsNullOrWhiteSpace(receipt.BattleId) &&
                     !string.IsNullOrWhiteSpace(receipt.BattlePreStateHash) &&
                     !string.IsNullOrWhiteSpace(
                         receipt.BattleReturnCheckpointId)));
        }

        public ExpeditionDeckState089 Advance(
            ExpeditionDeckState089 deck,
            string nextNodeId)
        {
            var receipt = deck?.PendingReceipt ?? throw new InvalidOperationException(
                "A committed card receipt is required before advancing the deck.");
            if (receipt.Effect132 != null && !receipt.Effect132.IsRolled132)
                throw new InvalidOperationException("EXPEDITION132_ROLL_BEFORE_ADVANCING");
            if (!ValidateCommittedReceipt(deck, receipt))
                throw new InvalidOperationException("The card receipt is invalid.");
            if (deck.AppliedReceiptIds.Contains(receipt.ReceiptId))
                throw new InvalidOperationException("The card receipt was already applied.");
            var selected = deck.CurrentRow.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.CardId, receipt.CardId)) ??
                throw new InvalidOperationException("The selected card left its route row.");
            var draw = new List<ExpeditionRouteCardState089>(deck.DrawPile);
            foreach (var unchosen in deck.CurrentRow)
                if (!StringComparer.Ordinal.Equals(unchosen.CardId, selected.CardId))
                    draw.Add(unchosen);
            var discard = new List<ExpeditionRouteCardState089>(deck.DiscardPile)
                { selected };
            var receipts = new List<ExpeditionCardReceipt089>(deck.AppliedReceipts)
                { receipt };
            var ids = new List<string>(deck.AppliedReceiptIds) { receipt.ReceiptId };
            ids.Sort(StringComparer.Ordinal);
            var leads = new List<string>(deck.EarnedRecruitLeadIds);
            if (!string.IsNullOrWhiteSpace(receipt.RecruitStableId) &&
                !leads.Contains(receipt.RecruitStableId)) leads.Add(receipt.RecruitStableId);
            leads.Sort(StringComparer.Ordinal);
            var advanced = deck.With(drawPile: draw.AsReadOnly(),
                currentRow: Array.Empty<ExpeditionRouteCardState089>(),
                discardPile: discard.AsReadOnly(), pendingReceipt: null,
                replacePendingReceipt: true, appliedReceipts: receipts.AsReadOnly(),
                appliedReceiptIds: ids.AsReadOnly(),
                momentum: Math.Max(-2, Math.Min(2,
                    deck.Momentum + AppliedMomentum132(receipt))),
                earnedRecruitLeadIds: leads.AsReadOnly());
            return string.IsNullOrWhiteSpace(nextNodeId)
                ? advanced : DrawRow(advanced, nextNodeId);
        }

        public ExpeditionDeckState089 AdvanceLockedBattle(
            ExpeditionDeckState089 deck,
            string completedNodeId,
            string nextNodeId)
        {
            if (deck == null || deck.PendingReceipt != null) return deck;
            if (deck.CurrentRow.Count == 0 || deck.CurrentRow.Any(value =>
                    !StringComparer.Ordinal.Equals(value.NodeId, completedNodeId)))
                return deck;
            var banished = new List<ExpeditionRouteCardState089>(deck.BanishedCards);
            banished.AddRange(deck.CurrentRow);
            var advanced = deck.With(
                currentRow: Array.Empty<ExpeditionRouteCardState089>(),
                banishedCards: banished.AsReadOnly());
            return string.IsNullOrWhiteSpace(nextNodeId)
                ? advanced : DrawRow(advanced, nextNodeId);
        }

        public static bool TryAuthorizedWorldGateModifier(
            WorldGateOperationState023 operation,
            string nodeId,
            string choiceId,
            string actorRecruitId,
            string assistantRecruitId,
            out int baseModifier)
        {
            baseModifier = 0;
            var deck = operation?.ExpeditionDeck089;
            if (deck == null) return true;
            var receipts = new List<ExpeditionCardReceipt089>(deck.AppliedReceipts);
            if (deck.PendingReceipt != null) receipts.Add(deck.PendingReceipt);
            return TryAuthorizedWorldGateModifier093(operation.OperationId, receipts,
                nodeId, choiceId, actorRecruitId, assistantRecruitId, out baseModifier);
        }

        public static bool TryAuthorizedWorldGateModifier093(
            string operationId, IEnumerable<ExpeditionCardReceipt089> receipts,
            string nodeId, string choiceId, string actorRecruitId,
            string assistantRecruitId, out int baseModifier)
        {
            baseModifier = 0;
            var receipt = receipts.LastOrDefault(value =>
                value.DelegatedToWorldGateCheck &&
                StringComparer.Ordinal.Equals(value.NodeId, nodeId) &&
                StringComparer.Ordinal.Equals(value.ChoiceId, choiceId));
            if (receipt == null) return true;
            if (!StringComparer.Ordinal.Equals(receipt.OperationId,
                    operationId) ||
                !StringComparer.Ordinal.Equals(receipt.ActorRecruitId,
                    actorRecruitId ?? string.Empty) ||
                !StringComparer.Ordinal.Equals(receipt.AssistantRecruitId,
                    assistantRecruitId ?? string.Empty) ||
                !StringComparer.Ordinal.Equals(receipt.ReceiptId,
                    ReceiptId(receipt.OperationId, receipt.CardId)) ||
                !StringComparer.Ordinal.Equals(receipt.AuthoritativeHash,
                    ReceiptHash(receipt))) return false;
            baseModifier = receipt.BaseCheckModifier;
            return true;
        }

        public static ExpeditionCheckModifierBreakdown089 CheckModifierBreakdown089(
            CampaignState campaign,
            WorldGateOperationState023 operation,
            ExpeditionRouteCardState089 card,
            string actorRecruitId,
            string assistantRecruitId)
        {
            if (operation == null || card == null || card.ResolutionDifficulty <= 0)
                return new ExpeditionCheckModifierBreakdown089(0, 0, 0, 0, 0, 0);
            var recruit = campaign?.Guild?.Recruits?.FirstOrDefault(value =>
                value != null && StringComparer.Ordinal.Equals(
                    value.RecruitId, actorRecruitId ?? string.Empty));
            var guildMember = 0;
            if (recruit != null)
            {
                var aptitude = Math.Max(recruit.TacticalAptitude,
                    recruit.LeadershipScore);
                if (aptitude >= 80) guildMember++;
                if ((recruit.Progression?.Level ?? 1) >= 8) guildMember++;
            }
            if ((campaign?.Guild?.Development?.GuildLevel ?? 1) >= 5)
                guildMember++;
            guildMember = Math.Min(2, guildMember);

            var chapter = 0;
            if (operation.Trust >= 50 || operation.CivilianSupport >= 50)
                chapter++;
            if (operation.Fatigue >= 4) chapter--;
            if (operation.Threat >= 5) chapter--;
            if (operation.Urgency >= 5) chapter--;
            chapter = Math.Max(-2, Math.Min(1, chapter));

            var condition = 0;
            if (recruit != null && recruit.VitalsInitialized &&
                recruit.MaximumHp > 0)
            {
                var hpBasisPoints = (long)recruit.CurrentHp * 10000L /
                                    recruit.MaximumHp;
                condition = hpBasisPoints <= 2500 ? -2 :
                    hpBasisPoints <= 5000 ? -1 : 0;
                if (recruit.MaximumMp > 0 && recruit.CurrentMp == 0)
                    condition = Math.Max(-2, condition - 1);
            }
            var team = !string.IsNullOrWhiteSpace(assistantRecruitId) &&
                       !StringComparer.Ordinal.Equals(actorRecruitId,
                            assistantRecruitId) ? 1 : 0;
            var permanentHero = 0;
            foreach (var traitId in recruit?.Progression?.UnlockedTreeIds ??
                     Array.Empty<string>())
            {
                if ((traitId ?? string.Empty).StartsWith(
                        PermanentBoonPrefix089, StringComparison.Ordinal))
                    permanentHero++;
                else if ((traitId ?? string.Empty).StartsWith(
                             PermanentScarPrefix089,
                             StringComparison.Ordinal))
                    permanentHero--;
            }
            permanentHero = Math.Max(-2, Math.Min(2, permanentHero));
            return new ExpeditionCheckModifierBreakdown089(
                card.ItemCheckModifier,
                operation.ExpeditionDeck089?.Momentum ?? 0,
                guildMember, chapter, condition, team, permanentHero);
        }

        public static int ChanceBasisPoints(int difficulty, int modifier)
        {
            if (difficulty <= 0) return 10000;
            var success = 0;
            for (var first = 1; first <= 6; first++)
                for (var second = 1; second <= 6; second++)
                    if (first + second + modifier >= difficulty) success++;
            return (success * 10000) / 36;
        }

        public static bool RequiresDeckRecovery089(
            WorldGateOperationState023 operation) =>
            operation != null && operation.ExpeditionDeck089 == null;

        static ExpeditionDeckState089 DrawRow(ExpeditionDeckState089 deck,
            string nodeId)
        {
            var nodeCards = deck.DrawPile.Where(value =>
                StringComparer.Ordinal.Equals(value.NodeId, nodeId)).ToList();
            var hasEncounterRow = nodeCards.Any(value => !value.AdvancesRoute);
            var encounterResolved = deck.AppliedReceipts.Any(receipt =>
                StringComparer.Ordinal.Equals(receipt.NodeId, nodeId) &&
                FindCard(deck, receipt.CardId)?.AdvancesRoute == false);
            var drawRoute = !hasEncounterRow || encounterResolved;
            var candidates = nodeCards.Where(value =>
                value.AdvancesRoute == drawRoute).ToList();
            var chosen = new List<ExpeditionRouteCardState089>();
            var routes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var card in candidates)
                if (chosen.Count < RouteRowSize && routes.Add(card.ChoiceId))
                    chosen.Add(card);
            foreach (var card in candidates)
                if (chosen.Count < RouteRowSize && !chosen.Any(value =>
                        StringComparer.Ordinal.Equals(value.CardId, card.CardId)))
                    chosen.Add(card);
            if (chosen.Count != RouteRowSize)
                throw new InvalidOperationException(
                    "Every authored quest node requires a three-card route row.");
            var chosenIds = new HashSet<string>(chosen.Select(value => value.CardId),
                StringComparer.Ordinal);
            var remaining = deck.DrawPile.Where(value =>
                !chosenIds.Contains(value.CardId)).ToArray();
            return deck.With(drawPile: remaining, currentRow: chosen.AsReadOnly(),
                drawCount: deck.DrawCount + RouteRowSize);
        }

        static ExpeditionRouteCardState089 BuildCard(
            string operationId,
            WorldGateBoardRule023 board,
            WorldGateNodeRule023 node,
            string choice,
            string category,
            int variant,
            ItemProfile089 profile,
            HeroMaster300Hero087 hero,
            RecruitState ownedHero,
            RecruitState permanentHero,
            bool advancesRoute,
            int progressionTier)
        {
            var recruitOfferKind = hero == null
                ? string.Empty
                : ownedHero == null
                    ? NewRecruitOfferKind089
                    : AscensionOfferKind089;
            var difficulty = advancesRoute && node.CheckDifficulty > 0
                ? node.CheckDifficulty :
                category == "HAZARD" ? 8 :
                category == "CHANCE" || category == "RECRUIT" ? 7 :
                category == "CHEST" && variant % 2 == 1 ? 6 : 0;
            if (category == "BATTLE" || category == "MERCHANT" ||
                category == "PERMANENT") difficulty = 0;
            var itemModifier = profile.ScoutModifier;
            if (category == "CHEST") itemModifier += profile.LockpickModifier;
            if (category == "HAZARD") itemModifier += profile.WardModifier;
            itemModifier = Math.Max(-2, Math.Min(2, itemModifier));
            var xp = CardXp(category);
            var materials = CardMaterials(category, board, node, variant);
            var momentum = category == "BUFF" || category == "CAMP" ? 1 : 0;
            var chance = ChanceBasisPoints(difficulty, itemModifier);
            var risk = difficulty <= 0 ? "SAFE" :
                chance >= 7500 ? "FAVORED" : chance >= 5000 ? "EVEN" : "RISKY";
            var outcome = difficulty <= 0
                ? "SUCCESS • full listed reward  |  NO FAILURE ROLL"
                : "SUCCESS • full listed reward  |  SETBACK • up to 2 XP, no item, material, or recruit" +
                  (category == "HAZARD" || category == "CHANCE" ||
                   category == "RECRUIT" ? ", −1 momentum" : string.Empty);
            var hash = CanonicalJson.Sha256Hex(new
            {
                operationId, board.BoardId, node.NodeId, choice, category, variant,
                advancesRoute,
                Profile = profile.Identity,
                Hero = hero?.StableId ?? string.Empty,
                PermanentHero = permanentHero?.RecruitId ?? string.Empty,
                ProgressionTier = progressionTier,
                RecruitOfferKind = recruitOfferKind,
                AscensionLevel = ownedHero?.Progression?.AscensionLevel ?? 0
            });
            var cardId = "EXPCARD089_" +
                         hash.Substring(0, 24).ToUpperInvariant();
            var permanentKind = string.Empty;
            var permanentEffectId = string.Empty;
            var permanentModifier = 0;
            if (StringComparer.Ordinal.Equals(category, "PERMANENT") &&
                permanentHero != null)
            {
                var boon = StableHexNibble089(hash, 0) % 2 == 0;
                permanentKind = boon ? "BOON" : "SCAR";
                permanentModifier = boon ? 1 : -1;
                permanentEffectId = (boon
                    ? PermanentBoonPrefix089 : PermanentScarPrefix089) +
                    hash.Substring(0, 20).ToUpperInvariant();
                if (!boon) xp = 32;
            }
            var title = StringComparer.Ordinal.Equals(category, "PERMANENT")
                ? permanentKind == "BOON"
                    ? "Hero-Bound Wayglass Blessing"
                    : "Hero-Bound Rift Scar"
                : CardTitle(category, node, hero, ownedHero, variant);
            var description = StringComparer.Ordinal.Equals(category, "PERMANENT")
                ? (permanentHero?.DisplayName ?? "A Guild hero") +
                  (permanentKind == "BOON"
                      ? " permanently gains +1 on Expedition checks whenever chosen as the lead hero."
                      : " permanently suffers −1 on Expedition checks whenever chosen as the lead hero; accepting the scar grants extra XP.")
                : CardDescription(category, node, hero, ownedHero, variant);
            if (StringComparer.Ordinal.Equals(category, "CHEST") && hero != null)
                description += ownedHero == null
                    ? " A sealed Guild contact inside reveals a recruit lead for " +
                      hero.Name + "."
                    : " A matching hero sigil inside reveals an Ascension copy for " +
                      hero.Name + ".";
            var chestEquipment = category == "CHEST"
                ? ChestEquipmentReward089(cardId, "PREVIEW_" + cardId)
                : null;
            var merchantEquipment = category == "MERCHANT"
                ? MerchantEquipmentReward089(cardId, "PREVIEW_" + cardId)
                : null;
            var treasuryXpCost = merchantEquipment == null ? 0 :
                MerchantCost089(merchantEquipment.QualityId);
            var enemyUnionCount = category == "BATTLE" && !advancesRoute
                ? Math.Max(1, Math.Min(4, progressionTier + variant % 2)) : 0;
            var encounterId = enemyUnionCount > 0
                ? "EXPEDITION_CARD089_WANDERING_THREAT_" +
                  hash.Substring(0, 12).ToUpperInvariant()
                : string.Empty;
            var reward = RewardCopy(category, xp, materials, hero, ownedHero,
                momentum, chestEquipment);
            if (merchantEquipment != null)
            {
                var power = M2EquipmentPowerPolicy087.Resolve(
                    merchantEquipment);
                description = "A Wayglass merchant offers one permanent item. " +
                              "Choose another card to leave without spending XP.";
                reward = "SPEND " + treasuryXpCost + " XP • " +
                         merchantEquipment.DisplayName + " • PWR +" +
                         power.PhysicalAttack + " / MYS +" +
                         power.MysticAttack + " • sent to Inventory";
            }
            else if (enemyUnionCount > 0)
            {
                description = "A wandering threat blocks this optional detour. " +
                              "Enter the existing Union Forecast battle, then return to this same quest room.";
                reward = enemyUnionCount + " enemy Union" +
                         (enemyUnionCount == 1 ? string.Empty : "s") +
                         " • battle loot + Art growth • route cards next";
            }
            else if (!string.IsNullOrWhiteSpace(permanentEffectId))
                reward = (permanentHero?.DisplayName ?? "Hero") +
                         (permanentKind == "BOON"
                             ? " • PERMANENT +1 EXPEDITION CHECKS"
                             : " • PERMANENT −1 EXPEDITION CHECKS • +32 Guild / Hall XP");
            var visualCategory = category == "AMBUSH" || category == "ELITE" ||
                                  category == "BOSS" ? "BATTLE" :
                                  category == "XP" ? "STORY" :
                                  category == "MERCHANT" ? "CHEST" :
                                  category == "PERMANENT"
                                      ? permanentKind == "SCAR" ? "HAZARD" : "BUFF"
                                      : category;
            return new ExpeditionRouteCardState089(
                cardId,
                node.NodeId, choice, category, visualCategory, title, description,
                risk, outcome, reward, difficulty, itemModifier, xp, xp,
                materials, momentum, hero?.StableId, hero?.Name,
                hero?.Rank.ToString(), "CAMPAIGN023|" + board.BoardId,
                recruitOfferKind, advancesRoute, treasuryXpCost, encounterId,
                enemyUnionCount, permanentHero?.RecruitId,
                permanentHero?.DisplayName, permanentKind,
                permanentEffectId, permanentModifier);
        }

        /// <summary>
        /// Returns the shortest playable card-choice route through a certified
        /// board.  Every non-battle node owns one encounter row and one route
        /// row; locked battles remain additional beats with no fake card choice.
        /// </summary>
        public static int MinimumChoiceRoundsForBoard089(
            WorldGateBoardRule023 board)
        {
            if (board?.Nodes == null || board.Nodes.Count == 0) return 0;
            var byId = board.Nodes.Where(value => value != null &&
                    !string.IsNullOrWhiteSpace(value.NodeId))
                .ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            if (byId.Count == 0 || string.IsNullOrWhiteSpace(board.StartNodeId) ||
                !byId.ContainsKey(board.StartNodeId)) return 0;
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var memo = new Dictionary<string, int>(StringComparer.Ordinal);
            return MinimumChoiceRoundsFrom089(
                board.StartNodeId, byId, visiting, memo);
        }

        static int MinimumChoiceRoundsFrom089(
            string nodeId,
            IReadOnlyDictionary<string, WorldGateNodeRule023> byId,
            ISet<string> visiting,
            IDictionary<string, int> memo)
        {
            if (memo.TryGetValue(nodeId, out var cached)) return cached;
            if (!byId.TryGetValue(nodeId, out var node) || node == null) return 0;
            // Authored packs are acyclic. Fail closed for a malformed cycle
            // instead of recursing forever or claiming extra playable rounds.
            if (!visiting.Add(nodeId)) return 0;
            var next = (node.NextNodeIds ?? Array.Empty<string>())
                .Where(byId.ContainsKey).Distinct(StringComparer.Ordinal)
                .ToArray();
            var after = next.Length == 0 ? 0 : next.Min(value =>
                MinimumChoiceRoundsFrom089(value, byId, visiting, memo));
            visiting.Remove(nodeId);
            var here = node.RequiresCertifiedBattle ? 0 :
                ChoiceRowsPerNonBattleNode089;
            var result = checked(here + after);
            memo[nodeId] = result;
            return result;
        }

        static ExpeditionCardReceipt089 Receipt(
            string operationId,
            ExpeditionRouteCardState089 card,
            string actor,
            string assistant,
            int dieOne,
            int dieTwo,
            int baseModifier,
            int effectiveModifier,
            int difficulty,
            string outcome,
            bool delegated,
            WorldGateNodeReceipt023 worldGateReceipt,
            string battlePreStateHash = null,
            BattleReturnReceipt017D battleReturnReceipt = null)
        {
            var success = !IsSetback(outcome);
            var xp = success ? card.GuildXp : Math.Min(2, card.GuildXp);
            var hall = success ? card.HallXp : Math.Min(2, card.HallXp);
            var materials = success ? card.MaterialIds : Array.Empty<string>();
            var momentum = success ? card.MomentumDelta :
                card.Category == "HAZARD" || card.Category == "CHANCE" ||
                card.Category == "RECRUIT" ? -1 : 0;
            var recruit = success ? card.RecruitStableId : string.Empty;
            if (delegated && worldGateReceipt == null)
            {
                xp = 0; hall = 0; materials = Array.Empty<string>();
                momentum = 0; recruit = string.Empty;
            }
            var optionalBattle = IsOptionalBattleCard089(card);
            var battleRequestId = string.Empty;
            var battleId = string.Empty;
            var battleReturnCheckpointId = string.Empty;
            if (optionalBattle)
            {
                var battleHash = CanonicalJson.Sha256Hex(new
                {
                    Rule = "EXPEDITION_OPTIONAL_BATTLE_089",
                    operationId,
                    card.CardId,
                    card.NodeId,
                    card.EncounterId,
                    card.EnemyUnionCount
                });
                battleRequestId = "EXPCARD_ENCOUNTER089_" +
                                  battleHash.Substring(0, 24).ToUpperInvariant();
                battleId = "EXPEDITION_CARD_BATTLE089_" +
                           battleHash.Substring(0, 20).ToUpperInvariant();
                battleReturnCheckpointId =
                    "expedition_089_optional_battle_return_" +
                    battleHash.Substring(0, 16).ToLowerInvariant();
            }
            var draft = new ExpeditionCardReceipt089(
                ReceiptId(operationId, card.CardId), operationId, card.CardId,
                card.NodeId, card.ChoiceId, actor ?? string.Empty,
                assistant ?? string.Empty, dieOne, dieTwo, baseModifier,
                effectiveModifier, difficulty, outcome, xp, hall, materials,
                momentum, recruit, "PENDING_EXPEDITION_CARD_HASH_089", delegated,
                ReceiptAuthoritySchemaVersion089, card.TreasuryXpCost,
                optionalBattle, battleRequestId, battleId,
                battlePreStateHash ?? string.Empty,
                battleReturnCheckpointId,
                battleReturnReceipt?.ReceiptId ?? string.Empty);
            return new ExpeditionCardReceipt089(
                draft.ReceiptId, draft.OperationId, draft.CardId, draft.NodeId,
                draft.ChoiceId, draft.ActorRecruitId, draft.AssistantRecruitId,
                draft.DieOne, draft.DieTwo, draft.BaseCheckModifier,
                draft.EffectiveModifier, draft.Difficulty, draft.Outcome,
                draft.GuildXp, draft.HallXp, draft.MaterialIds,
                draft.MomentumDelta, draft.RecruitStableId,
                ReceiptHash(draft), draft.DelegatedToWorldGateCheck,
                draft.AuthoritySchemaVersion, draft.TreasuryXpCost,
                draft.RequiresCertifiedBattle, draft.BattleRequestId,
                draft.BattleId, draft.BattlePreStateHash,
                draft.BattleReturnCheckpointId,
                draft.BattleReturnReceiptId);
        }

        static string ReceiptId(string operationId, string cardId)
        {
            var hash = CanonicalJson.Sha256Hex(new
                { operationId, cardId, Authority = ExpeditionDeckState089.Version });
            return "EXPREC089_" + hash.Substring(0, 24).ToUpperInvariant();
        }

        static string ReceiptHash(ExpeditionCardReceipt089 receipt)
        {
            if (receipt.Effect132 != null)
                return CanonicalJson.Sha256Hex(new
                {
                    BaseHash = ReceiptHash(CopyEffectReceipt132(receipt, null, receipt.AuthoritativeHash)),
                    receipt.Effect132
                });
            if (receipt.AuthoritySchemaVersion <
                ReceiptAuthoritySchemaVersion089)
                return CanonicalJson.Sha256Hex(new
                {
                    receipt.ReceiptId,
                    receipt.OperationId,
                    receipt.CardId,
                    receipt.NodeId,
                    receipt.ChoiceId,
                    receipt.ActorRecruitId,
                    receipt.AssistantRecruitId,
                    receipt.DieOne,
                    receipt.DieTwo,
                    receipt.BaseCheckModifier,
                    receipt.EffectiveModifier,
                    receipt.Difficulty,
                    receipt.Outcome,
                    receipt.GuildXp,
                    receipt.HallXp,
                    receipt.MaterialIds,
                    receipt.MomentumDelta,
                    receipt.RecruitStableId,
                    receipt.DelegatedToWorldGateCheck,
                    Authority = ExpeditionDeckState089.Version
                });
            return CanonicalJson.Sha256Hex(new
            {
                receipt.ReceiptId,
                receipt.OperationId,
                receipt.CardId,
                receipt.NodeId,
                receipt.ChoiceId,
                receipt.ActorRecruitId,
                receipt.AssistantRecruitId,
                receipt.DieOne,
                receipt.DieTwo,
                receipt.BaseCheckModifier,
                receipt.EffectiveModifier,
                receipt.Difficulty,
                receipt.Outcome,
                receipt.GuildXp,
                receipt.HallXp,
                receipt.MaterialIds,
                receipt.MomentumDelta,
                receipt.RecruitStableId,
                receipt.DelegatedToWorldGateCheck,
                receipt.AuthoritySchemaVersion,
                receipt.TreasuryXpCost,
                receipt.RequiresCertifiedBattle,
                receipt.BattleRequestId,
                receipt.BattleId,
                receipt.BattlePreStateHash,
                receipt.BattleReturnCheckpointId,
                receipt.BattleReturnReceiptId,
                Authority = ExpeditionDeckState089.Version
            });
        }

        static ExpeditionRouteCardState089 FindCard(ExpeditionDeckState089 deck,
            string cardId) => deck.CurrentRow.Concat(deck.DrawPile)
            .Concat(deck.DiscardPile).Concat(deck.BanishedCards)
            .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.CardId, cardId));

        static string Outcome(int total, int difficulty) =>
            total >= difficulty + 3 ? "TRIUMPH" :
            total >= difficulty ? "SUCCESS" : "SETBACK";

        static bool IsSetback(string outcome) =>
            !string.IsNullOrWhiteSpace(outcome) &&
            (outcome.IndexOf("SETBACK", StringComparison.OrdinalIgnoreCase) >= 0 ||
             outcome.IndexOf("DEFEAT", StringComparison.OrdinalIgnoreCase) >= 0 ||
             outcome.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0);

        public static int ProgressionTierForGuildLevel089(int guildLevel) =>
            guildLevel >= 10 ? 4 : guildLevel >= 6 ? 3 :
            guildLevel >= 3 ? 2 : 1;

        public static int CardsPerNodeForProgressionTier089(int tier) =>
            CardsPerNode + (Math.Max(1, Math.Min(4, tier)) - 1) * 2;

        public static IReadOnlyList<string> UnlockedCategoriesForTier089(
            int tier)
        {
            var result = new List<string>
            {
                "STORY", "CHEST", "XP", "BUFF", "HAZARD", "CHANCE",
                "RECRUIT", "MERCHANT", "OPTIONAL_BATTLE"
            };
            if (tier >= 2) result.Add("NATURAL20_PERMANENT_HERO_BOON");
            if (tier >= 3) result.Add("DEEP_MERCHANT_STOCK");
            if (tier >= 4) result.Add("MAXIMUM_DECK_VARIETY");
            return result.AsReadOnly();
        }

        static string CategoryFor089(string operationId,
            WorldGateNodeRule023 node,
            bool advancesRoute, int phaseIndex, int progressionTier)
        {
            if (node.RequiresCertifiedBattle || StringComparer.OrdinalIgnoreCase.Equals(
                    node.Kind, "BATTLE"))
            {
                var battleCategory = node.EnemyUnionCount >= 8 ? "BOSS" :
                    node.EnemyUnionCount >= 4 ? "ELITE" :
                    node.EnemyUnionCount == 3 ? "AMBUSH" : "BATTLE";
                return battleCategory;
            }
            string[] categories;
            switch ((node.Kind ?? string.Empty).ToUpperInvariant())
            {
                case "RESOURCE": categories = advancesRoute
                    ? new[] { "XP", "HAZARD", "RECRUIT" }
                    : new[] { "CHEST", "MERCHANT", "XP" }; break;
                case "CAMP": categories = advancesRoute
                    ? new[] { "CHEST", "CHANCE", "RECRUIT" }
                    : new[] { "CAMP", "MERCHANT", "XP" }; break;
                case "CHECK":
                case "SKILL_CHECK": categories = advancesRoute
                    ? new[] { "CHEST", "XP", "RECRUIT" }
                    : new[] { "CHANCE", "HAZARD", "BATTLE" }; break;
                case "OBJECTIVE": categories = advancesRoute
                    ? new[] { "CHEST", "XP", "HAZARD" }
                    : new[] { "OBJECTIVE", "RECRUIT", "BATTLE" }; break;
                case "EXIT": categories = advancesRoute
                    ? new[] { "STORY", "BUFF", "XP" }
                    : new[] { "CHEST", "XP", "RECRUIT" }; break;
                case "DEFENSE_PREP": categories = advancesRoute
                    ? new[] { "CHEST", "XP", "RECRUIT" }
                    : new[] { "BATTLE", "BUFF", "HAZARD" }; break;
                default: categories = advancesRoute
                    ? new[] { "HAZARD", "CHANCE", "RECRUIT" }
                    : new[] { "STORY", "CHEST", "BATTLE" }; break;
            }
            if (phaseIndex < categories.Length)
                return categories[phaseIndex];
            var growthSlot = phaseIndex - categories.Length;
            if (progressionTier <= 1 || growthSlot < 0)
                return categories[phaseIndex % categories.Length];
            if (growthSlot == 0)
            {
                // Permanent hero fate is an unlock, not a routine tax.  The
                // saved operation ID makes this a deterministic 12.5% deck
                // event while save/reload and receipt retries remain pure.
                var fateHash = CanonicalJson.Sha256Hex(new
                {
                    Rule = "EXPEDITION_PERMANENT_FATE_CHANCE_089",
                    operationId,
                    node.NodeId,
                    advancesRoute,
                    progressionTier
                });
                return StableHexNibble089(fateHash, 0) % 8 == 0
                    ? "PERMANENT"
                    : advancesRoute ? "BUFF" : "CHANCE";
            }
            if (growthSlot == 1)
                return advancesRoute ? "CHEST" : "MERCHANT";
            return advancesRoute ? "BUFF" : "BATTLE";
        }

        static int CardXp(string category)
        {
            switch (category)
            {
                case "CHEST": return 10;
                case "HAZARD": return 14;
                case "CHANCE": return 12;
                case "RECRUIT": return 6;
                case "OBJECTIVE": return 8;
                case "XP": return 20;
                case "PERMANENT": return 4;
                case "BUFF":
                case "CAMP": return 4;
                case "STORY": return 3;
                default: return 0;
            }
        }

        static IReadOnlyList<string> CardMaterials(string category,
            WorldGateBoardRule023 board, WorldGateNodeRule023 node, int variant)
        {
            if (category != "CHEST" && category != "CHANCE" &&
                category != "HAZARD") return Array.Empty<string>();
            if (node.MaterialIds != null && node.MaterialIds.Count > 0)
                return new[] { node.MaterialIds[variant % node.MaterialIds.Count] };
            var world = string.IsNullOrWhiteSpace(board.WorldId) ? "SKYHOME" : board.WorldId;
            return new[] { "MAT089_" + world.ToUpperInvariant() + "_FIELD_CACHE" };
        }

        static string CardTitle(string category, WorldGateNodeRule023 node,
            HeroMaster300Hero087 hero, RecruitState ownedHero, int variant)
        {
            switch (category)
            {
                case "CHEST":
                    return new[] { "Wayglass Cache", "Sealed Guild Lockbox",
                        "Forgotten Relic Coffer" }[variant % 3];
                case "BUFF":
                    return new[] { "Lantern-Bright Omen", "Rally at the Guild Banner",
                        "Second-Dimension Surge" }[variant % 3];
                case "HAZARD": return "Hazard: " + ShortNodeTitle(node);
                case "CHANCE": return "Fate at " + ShortNodeTitle(node);
                case "XP": return new[] { "Guild Chronicle Page",
                    "Mentor's Field Lesson", "Wayfarer's Insight" }[variant % 3];
                case "MERCHANT": return "Wayglass Merchant";
                case "RECRUIT": return hero == null ? "A Stranger's Trail" :
                    ownedHero == null
                        ? "Recruit Lead: " + hero.Name
                        : "Ascension Copy: " + hero.Name;
                case "CAMP": return "Camp at " + ShortNodeTitle(node);
                case "AMBUSH": return "Ambush: " + ShortNodeTitle(node);
                case "ELITE": return "Elite Hunt: " + ShortNodeTitle(node);
                case "BOSS": return "Boss: " + ShortNodeTitle(node);
                case "BATTLE": return "Battle: " + ShortNodeTitle(node);
                case "OBJECTIVE": return "Objective: " + ShortNodeTitle(node);
                default: return string.IsNullOrWhiteSpace(node.Title)
                    ? "Follow the Story" : node.Title;
            }
        }

        static string CardDescription(string category, WorldGateNodeRule023 node,
            HeroMaster300Hero087 hero, RecruitState ownedHero, int variant)
        {
            var story = string.IsNullOrWhiteSpace(node.Description)
                ? "Advance the Guild expedition." : node.Description;
            switch (category)
            {
                case "CHEST": return story + " Search the detour, open the chest, and roll Common through Godly equippable gear, XP, and salvage.";
                case "BUFF": return story + " Secure a boon that strengthens your next route check.";
                case "HAZARD": return story + " The dangerous approach offers better salvage if your 2D6 check succeeds.";
                case "CHANCE": return story + " Roll two physical dice; success earns XP and field salvage.";
                case "XP": return story + " Claim direct Guild and Hall XP with no inventory detour.";
                case "RECRUIT":
                    if (hero == null) return story;
                    if (ownedHero == null)
                        return hero.Name + " — " + hero.Rank + " " + hero.Role +
                               " wielding " + hero.Weapon +
                               ". Earn their introduction for the Recruitment Desk.";
                    var ascension = ownedHero.Progression?.AscensionLevel ?? 0;
                    return ascension < RecruitAscensionRules089.MaximumLevel
                        ? hero.Name + " is already in your Guild. This matching copy raises " +
                          "Ascension to " + (ascension + 1) + "/" +
                          RecruitAscensionRules089.MaximumLevel + ": +" +
                          RecruitAscensionRules089.MaximumHpBonusPerLevel +
                          " HP, +" + RecruitAscensionRules089.MaximumMpBonusPerLevel +
                          " MP, and +" + RecruitAscensionRules089.CoreStatBonusPerLevel +
                          " to every core stat."
                        : hero.Name + " is at Ascension " +
                          RecruitAscensionRules089.MaximumLevel +
                          ". This matching copy unlocks " +
                          "an assigned earnable skill tree when available, otherwise it " +
                          "raises one learned Art by a full level (maximum 10).";
                case "CAMP": return story + " Rest, prepare, and bank momentum before the next reveal.";
                case "AMBUSH":
                case "ELITE":
                case "BOSS":
                case "BATTLE": return story + " Enemy preview: " +
                    Math.Max(1, node.EnemyUnionCount) +
                    " hostile Union" + (node.EnemyUnionCount == 1 ? string.Empty : "s") +
                    ". Resolve in certified Union combat.";
                default: return story;
            }
        }

        static string ShortNodeTitle(WorldGateNodeRule023 node)
        {
            var title = string.IsNullOrWhiteSpace(node?.Title)
                ? "the Uncharted Route" : node.Title.Trim();
            return title.Length <= 44 ? title : title.Substring(0, 43).TrimEnd() + "…";
        }

        static string RewardCopy(string category, int xp,
            IReadOnlyList<string> materials, HeroMaster300Hero087 hero,
            RecruitState ownedHero,
            int momentum, EquipmentItemState chestEquipment)
        {
            var parts = new List<string>();
            if (chestEquipment != null)
            {
                var power = SecondDimension.Gameplay.M2
                    .M2EquipmentPowerPolicy087.Resolve(chestEquipment);
                parts.Add(chestEquipment.DisplayName + " • EQUIP PREVIEW +" +
                    power.PhysicalAttack + " PHYSICAL / +" +
                    power.MysticAttack + " MYSTIC");
            }
            if (xp > 0) parts.Add("+" + xp + " Guild / Hall XP");
            if (materials.Count > 0) parts.Add("1 material");
            if (momentum > 0) parts.Add("+" + momentum + " route momentum");
            if (hero != null)
            {
                var ascension = ownedHero?.Progression?.AscensionLevel ?? 0;
                parts.Add(ownedHero == null
                    ? "unlock " + hero.Name + " recruitment lead"
                    : ascension < RecruitAscensionRules089.MaximumLevel
                        ? "merge " + hero.Name + " • Ascension " +
                          (ascension + 1) + "/" + RecruitAscensionRules089.MaximumLevel
                        : "merge " + hero.Name + " • skill tree / Art upgrade");
            }
            if (category == "BATTLE" || category == "AMBUSH" ||
                category == "ELITE" || category == "BOSS")
                parts.Add("certified battle rewards");
            return parts.Count == 0 ? "Story progress" : string.Join(" • ", parts);
        }

        public static EquipmentItemState ChestEquipmentReward089(
            string cardId, string receiptId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                throw new ArgumentException("Card ID is required.", nameof(cardId));
            if (string.IsNullOrWhiteSpace(receiptId))
                throw new ArgumentException("Receipt ID is required.", nameof(receiptId));

            // Two deterministic hash nibbles provide enough resolution for a
            // genuinely rare top tier without any reroll/load exploit.
            var qualityRoll = StableHexNibble089(cardId, 1) * 16 +
                              StableHexNibble089(cardId, 0);
            var qualityId = qualityRoll < 128 ? "QUALITY_COMMON" :
                qualityRoll < 192 ? "QUALITY_UNCOMMON" :
                qualityRoll < 224 ? "QUALITY_RARE" :
                qualityRoll < 244 ? "QUALITY_EPIC" :
                qualityRoll < 253 ? "QUALITY_LEGENDARY" :
                "QUALITY_GODLY";
            var qualityName = qualityId.Substring("QUALITY_".Length);
            var kind = StableHexNibble089(cardId, 2) % 4;
            string definitionId;
            string baseName;
            string slot;
            string[] tags;
            switch (kind)
            {
                case 0:
                    definitionId = "EXPEDITION089_WAYGLASS_BLADE";
                    baseName = "Wayglass Blade";
                    slot = EquipmentSlotIds.MainHand;
                    tags = new[] { "EXPEDITION_CHEST_089", "WF01_SWORD", "SWORD" };
                    break;
                case 1:
                    definitionId = "EXPEDITION089_LANTERN_STAFF";
                    baseName = "Lantern Staff";
                    slot = EquipmentSlotIds.MainHand;
                    tags = new[] { "EXPEDITION_CHEST_089", "WF09_STAFF", "STAFF" };
                    break;
                case 2:
                    definitionId = "EXPEDITION089_RIFTWOVEN_COAT";
                    baseName = "Riftwoven Coat";
                    slot = EquipmentSlotIds.BodyArmor;
                    tags = new[] { "EXPEDITION_CHEST_089", "ARMOR" };
                    break;
                default:
                    definitionId = "EXPEDITION089_GUILDFINDER_CHARM";
                    baseName = "Guildfinder Charm";
                    slot = EquipmentSlotIds.AccessoryOne;
                    tags = new[] { "EXPEDITION_CHEST_089", "WF12_HYBRID_RELIC", "RELIC" };
                    break;
            }
            var identity = new string(receiptId.Where(char.IsLetterOrDigit)
                .ToArray()).ToUpperInvariant();
            if (identity.Length > 28)
                identity = identity.Substring(identity.Length - 28);
            return new EquipmentItemState(
                "LOOT_ITEM_070_EXP089_" + identity,
                definitionId + "_" + qualityName,
                qualityName.Substring(0, 1) +
                qualityName.Substring(1).ToLowerInvariant() + " " + baseName,
                new[] { slot }, tags, qualityId, 10000, false);
        }

        public static EquipmentItemState MerchantEquipmentReward089(
            string cardId, string receiptId)
        {
            var source = ChestEquipmentReward089(cardId, receiptId);
            var qualityId = source.QualityId == "QUALITY_COMMON" ||
                            source.QualityId == "QUALITY_UNCOMMON"
                ? "QUALITY_RARE" : source.QualityId;
            var quality = FriendlyQuality089(qualityId);
            var split = source.DisplayName.IndexOf(' ');
            var baseName = split >= 0
                ? source.DisplayName.Substring(split + 1) : source.DisplayName;
            var tags = source.EquipmentTags.Concat(new[]
                { "EXPEDITION_MERCHANT_089" })
                .Distinct(StringComparer.Ordinal).ToArray();
            var identity = new string(receiptId.Where(char.IsLetterOrDigit)
                .ToArray()).ToUpperInvariant();
            if (identity.Length > 26)
                identity = identity.Substring(identity.Length - 26);
            return new EquipmentItemState(
                "LOOT_ITEM_070_EXPMERCHANT089_" + identity,
                "EXPEDITION089_MERCHANT_" + source.DefinitionId,
                quality + " Merchant " + baseName,
                source.ValidSlotIds, tags, qualityId, 10000, false);
        }

        public static int MerchantCost089(string qualityId)
        {
            switch ((qualityId ?? string.Empty).ToUpperInvariant())
            {
                case "QUALITY_GODLY": return 140;
                case "QUALITY_LEGENDARY": return 90;
                case "QUALITY_EPIC": return 65;
                default: return 45;
            }
        }

        static string FriendlyQuality089(string qualityId)
        {
            var value = (qualityId ?? string.Empty).Replace(
                "QUALITY_", string.Empty);
            if (string.IsNullOrWhiteSpace(value)) return "Rare";
            return value.Substring(0, 1).ToUpperInvariant() +
                   value.Substring(1).ToLowerInvariant();
        }

        static int StableHexNibble089(string value, int fromEnd)
        {
            var found = 0;
            for (var index = value.Length - 1; index >= 0; index--)
            {
                var character = char.ToUpperInvariant(value[index]);
                var nibble = character >= '0' && character <= '9'
                    ? character - '0'
                    : character >= 'A' && character <= 'F'
                        ? character - 'A' + 10 : -1;
                if (nibble < 0) continue;
                if (found++ == fromEnd) return nibble;
            }
            return 0;
        }

        static bool ChestCarriesRecruitLead089(long campaignSeed,
            string operationId, string nodeId, int variant)
        {
            var hash = CanonicalJson.Sha256Hex(new
            {
                Rule = "EXPEDITION_CHEST_RECRUIT_CHANCE_089",
                campaignSeed,
                operationId,
                nodeId,
                variant
            });
            // One deterministic result in sixteen: visible before selection,
            // stable through save/reload, and rare enough to stay exciting.
            return StableHexNibble089(hash, 0) == 0;
        }

        static IEnumerable<HeroMaster300Hero087> EligibleHeroes(
            HeroMaster300Catalog087 catalog,
            IReadOnlyList<RecruitState> owned,
            IReadOnlyList<string> earned,
            Pcg32 rng)
        {
            if (catalog == null) return Array.Empty<HeroMaster300Hero087>();
            var blocked = new HashSet<string>(earned ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            foreach (var leadId in earned ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(leadId) &&
                    catalog.TryGetAcceptedHero(leadId, out var pendingHero) &&
                    pendingHero != null)
                {
                    blocked.Add(pendingHero.StableId);
                    blocked.Add(pendingHero.GameEntityId);
                }

            var candidates = catalog.NormalApplicantCandidates.Where(hero =>
                hero != null && hero.IsNormalApplicantEligible &&
                !blocked.Contains(hero.StableId) &&
                !blocked.Contains(hero.GameEntityId))
                .Distinct()
                .ToArray();
            var unowned = candidates.Where(hero =>
                    FindOwnedHero089(owned, hero) == null)
                .ToList();
            var ascension = candidates.Where(hero =>
                    FindOwnedHero089(owned, hero) != null)
                .ToList();
            Shuffle(unowned, rng);
            Shuffle(ascension, rng);

            // Three new-hero opportunities are dealt for every matching-copy
            // opportunity while both pools remain. Every hero appears at most once
            // in the generated deck, so one unresolved lead cannot be offered twice.
            var weighted = new List<HeroMaster300Hero087>(candidates.Length);
            var unownedIndex = 0;
            var ascensionIndex = 0;
            while (unownedIndex < unowned.Count || ascensionIndex < ascension.Count)
            {
                for (var weight = 0; weight < 3 && unownedIndex < unowned.Count; weight++)
                    weighted.Add(unowned[unownedIndex++]);
                if (ascensionIndex < ascension.Count)
                    weighted.Add(ascension[ascensionIndex++]);
                if (unownedIndex >= unowned.Count)
                    while (ascensionIndex < ascension.Count)
                        weighted.Add(ascension[ascensionIndex++]);
            }
            return weighted;
        }

        static RecruitState PermanentHero089(
            IReadOnlyList<RecruitState> owned,
            string operationId,
            string nodeId,
            int variant)
        {
            var heroes = (owned ?? Array.Empty<RecruitState>())
                .Where(value => value != null)
                .OrderBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToArray();
            if (heroes.Length == 0) return null;
            var hash = CanonicalJson.Sha256Hex(new
            {
                Rule = "EXPEDITION_PERMANENT_HERO_089",
                operationId,
                nodeId,
                variant,
                Heroes = heroes.Select(value => value.RecruitId).ToArray()
            });
            return heroes[Convert.ToInt32(hash.Substring(0, 4), 16) %
                          heroes.Length];
        }

        static RecruitState FindOwnedHero089(
            IReadOnlyList<RecruitState> owned,
            HeroMaster300Hero087 hero)
        {
            if (owned == null || hero == null) return null;
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return owned.FirstOrDefault(recruit => recruit != null &&
                (StringComparer.Ordinal.Equals(recruit.RecruitId, projectedId) ||
                 StringComparer.Ordinal.Equals(recruit.RecruitId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(recruit.RecruitId, hero.GameEntityId) ||
                 StringComparer.Ordinal.Equals(
                     recruit.AuthoredStableRecruitId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(recruit.SignatureId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(recruit.SignatureId,
                     hero.GameEntityId)));
        }

        static void Shuffle<T>(IList<T> values, Pcg32 rng)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var other = rng.NextInclusive(0, index);
                var temp = values[index];
                values[index] = values[other];
                values[other] = temp;
            }
        }

        sealed class ItemProfile089
        {
            public int ScoutModifier;
            public int LockpickModifier;
            public int WardModifier;
            public bool HasTreasureMap;
            public string Identity;
            public IReadOnlyList<string> Labels;

            public static ItemProfile089 From(
                IReadOnlyList<EquipmentItemState> inventory,
                IReadOnlyList<RecruitState> ownedRecruits)
            {
                var equippedAndCarried = new Dictionary<string, EquipmentItemState>(
                    StringComparer.Ordinal);
                void Add(EquipmentItemState item)
                {
                    if (item != null && !equippedAndCarried.ContainsKey(item.InstanceId))
                        equippedAndCarried.Add(item.InstanceId, item);
                }
                if (inventory != null)
                    foreach (var item in inventory) Add(item);
                if (ownedRecruits != null)
                    foreach (var recruit in ownedRecruits)
                        if (recruit?.Equipment?.Assignments != null)
                            foreach (var assignment in recruit.Equipment.Assignments)
                                Add(assignment?.Item);
                bool Has(string token) => equippedAndCarried.Values.Any(item =>
                    item != null && ((item.DefinitionId ?? string.Empty).IndexOf(token,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.EquipmentTags.Any(tag => (tag ?? string.Empty).IndexOf(token,
                            StringComparison.OrdinalIgnoreCase) >= 0)));
                var scout = Has("SCOUT") || Has("LENS");
                var lockpick = Has("LOCKPICK");
                var ward = Has("WARD");
                var map = Has("TREASURE_MAP") || Has("EXPEDITION_MAP");
                var labels = new List<string>();
                if (scout) labels.Add("Scout Lens • +1 route checks");
                if (lockpick) labels.Add("Lockpick Kit • +1 chest checks");
                if (ward) labels.Add("Ward Charm • +1 hazard checks");
                if (map) labels.Add("Treasure Map • adds chest cards");
                if (labels.Count == 0)
                    labels.Add("No deck-changing expedition items owned or equipped");
                return new ItemProfile089
                {
                    ScoutModifier = scout ? 1 : 0,
                    LockpickModifier = lockpick ? 1 : 0,
                    WardModifier = ward ? 1 : 0,
                    HasTreasureMap = map,
                    Identity = "SCOUT=" + scout + "|LOCKPICK=" + lockpick +
                               "|WARD=" + ward + "|MAP=" + map,
                    Labels = labels.AsReadOnly()
                };
            }
        }
    }

    public sealed class ExpeditionCheckModifierBreakdown089
    {
        public ExpeditionCheckModifierBreakdown089(
            int cardItemModifier,
            int routeMomentumModifier,
            int guildMemberModifier,
            int chapterModifier,
            int conditionModifier,
            int teamModifier,
            int permanentHeroModifier = 0)
        {
            CardItemModifier = cardItemModifier;
            RouteMomentumModifier = routeMomentumModifier;
            GuildMemberModifier = guildMemberModifier;
            ChapterModifier = chapterModifier;
            ConditionModifier = conditionModifier;
            TeamModifier = teamModifier;
            PermanentHeroModifier = Math.Max(-2, Math.Min(2,
                permanentHeroModifier));
            RawBaseModifier = checked(cardItemModifier + routeMomentumModifier +
                guildMemberModifier + chapterModifier + conditionModifier +
                PermanentHeroModifier);
            BaseModifier = Math.Max(-4, Math.Min(4, RawBaseModifier));
            EffectiveModifier = checked(BaseModifier + teamModifier);
        }

        public int CardItemModifier { get; }
        public int RouteMomentumModifier { get; }
        public int GuildMemberModifier { get; }
        public int ChapterModifier { get; }
        public int ConditionModifier { get; }
        public int TeamModifier { get; }
        public int PermanentHeroModifier { get; }
        public int RawBaseModifier { get; }
        public int BaseModifier { get; }
        public int EffectiveModifier { get; }
        public bool WasCapped => RawBaseModifier != BaseModifier;
    }
}
