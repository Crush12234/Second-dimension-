using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.Campaign023
{
    /// <summary>
    /// A player-facing route card. The card chooses an authored World Gate edge;
    /// it never creates a parallel mission graph or a parallel battle authority.
    /// </summary>
    [Serializable]
    public sealed class ExpeditionRouteCardState089
    {
        [JsonConstructor]
        public ExpeditionRouteCardState089(
            string cardId,
            string nodeId,
            string choiceId,
            string category,
            string visualCategoryKey,
            string title,
            string description,
            string riskLabel,
            string outcomePreview,
            string rewardPreview,
            int resolutionDifficulty,
            int itemCheckModifier,
            int guildXp,
            int hallXp,
            IReadOnlyList<string> materialIds,
            int momentumDelta,
            string recruitStableId,
            string recruitName,
            string recruitRank,
            string sourceTag,
            string recruitOfferKind = null,
            bool? advancesRoute = null,
            int? treasuryXpCost = null,
            string encounterId = null,
            int? enemyUnionCount = null,
            string permanentHeroRecruitId = null,
            string permanentHeroName = null,
            string permanentHeroEffectKind = null,
            string permanentHeroEffectId = null,
            int? permanentHeroCheckModifier = null)
        {
            CardId = Require(cardId, nameof(cardId));
            NodeId = Require(nodeId, nameof(nodeId));
            ChoiceId = Require(choiceId, nameof(choiceId));
            Category = Require(category, nameof(category));
            VisualCategoryKey = Require(visualCategoryKey, nameof(visualCategoryKey));
            Title = string.IsNullOrWhiteSpace(title) ? Category : title;
            Description = description ?? string.Empty;
            RiskLabel = riskLabel ?? string.Empty;
            OutcomePreview = outcomePreview ?? string.Empty;
            RewardPreview = rewardPreview ?? string.Empty;
            ResolutionDifficulty = Math.Max(0, resolutionDifficulty);
            ItemCheckModifier = Math.Max(-2, Math.Min(2, itemCheckModifier));
            if (guildXp < 0 || hallXp < 0)
                throw new ArgumentOutOfRangeException(nameof(guildXp));
            GuildXp = guildXp;
            HallXp = hallXp;
            MaterialIds = CopyIds(materialIds);
            MomentumDelta = Math.Max(-2, Math.Min(2, momentumDelta));
            RecruitStableId = recruitStableId ?? string.Empty;
            RecruitName = recruitName ?? string.Empty;
            RecruitRank = recruitRank ?? string.Empty;
            SourceTag = sourceTag ?? string.Empty;
            RecruitOfferKind = recruitOfferKind ?? string.Empty;
            // V1 saves predate encounter rows and every saved card in those
            // decks was a route card.  Nullable construction keeps those saves
            // moving forward while allowing new decks to persist an explicit
            // false value for a card-only encounter round.
            AdvancesRoute = advancesRoute ?? true;
            TreasuryXpCost = Math.Max(0, treasuryXpCost ?? 0);
            EncounterId = encounterId ?? string.Empty;
            EnemyUnionCount = Math.Max(0, Math.Min(10,
                enemyUnionCount ?? 0));
            PermanentHeroRecruitId = permanentHeroRecruitId ?? string.Empty;
            PermanentHeroName = permanentHeroName ?? string.Empty;
            PermanentHeroEffectKind = permanentHeroEffectKind ?? string.Empty;
            PermanentHeroEffectId = permanentHeroEffectId ?? string.Empty;
            PermanentHeroCheckModifier = Math.Max(-1, Math.Min(1,
                permanentHeroCheckModifier ?? 0));
        }

        public string CardId { get; }
        public string NodeId { get; }
        public string ChoiceId { get; }
        public string Category { get; }
        public string VisualCategoryKey { get; }
        public string Title { get; }
        public string Description { get; }
        public string RiskLabel { get; }
        public string OutcomePreview { get; }
        public string RewardPreview { get; }
        public int ResolutionDifficulty { get; }
        public int ItemCheckModifier { get; }
        public int GuildXp { get; }
        public int HallXp { get; }
        public IReadOnlyList<string> MaterialIds { get; }
        public int MomentumDelta { get; }
        public string RecruitStableId { get; }
        public string RecruitName { get; }
        public string RecruitRank { get; }
        public string SourceTag { get; }

        [System.ComponentModel.DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool AdvancesRoute { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RecruitOfferKind { get; }

        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int TreasuryXpCost { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EncounterId { get; }

        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int EnemyUnionCount { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PermanentHeroRecruitId { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PermanentHeroName { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PermanentHeroEffectKind { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PermanentHeroEffectId { get; }

        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int PermanentHeroCheckModifier { get; }

        static IReadOnlyList<string> CopyIds(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
                for (var index = 0; index < values.Count; index++)
                    if (!string.IsNullOrWhiteSpace(values[index]) &&
                        !result.Contains(values[index])) result.Add(values[index]);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    [Serializable]
    public sealed class ExpeditionCardReceipt089
    {
        [JsonConstructor]
        public ExpeditionCardReceipt089(
            string receiptId,
            string operationId,
            string cardId,
            string nodeId,
            string choiceId,
            string actorRecruitId,
            string assistantRecruitId,
            int dieOne,
            int dieTwo,
            int baseCheckModifier,
            int effectiveModifier,
            int difficulty,
            string outcome,
            int guildXp,
            int hallXp,
            IReadOnlyList<string> materialIds,
            int momentumDelta,
            string recruitStableId,
            string authoritativeHash,
            bool delegatedToWorldGateCheck,
            int? authoritySchemaVersion = null,
            int? treasuryXpCost = null,
            bool? requiresCertifiedBattle = null,
            string battleRequestId = null,
            string battleId = null,
            string battlePreStateHash = null,
            string battleReturnCheckpointId = null,
            string battleReturnReceiptId = null,
            ExpeditionEffectReceipt132 effect132 = null)
        {
            ReceiptId = Require(receiptId, nameof(receiptId));
            OperationId = Require(operationId, nameof(operationId));
            CardId = Require(cardId, nameof(cardId));
            NodeId = Require(nodeId, nameof(nodeId));
            ChoiceId = Require(choiceId, nameof(choiceId));
            ActorRecruitId = actorRecruitId ?? string.Empty;
            AssistantRecruitId = assistantRecruitId ?? string.Empty;
            if (dieOne < 0 || dieOne > 6 || dieTwo < 0 || dieTwo > 6)
                throw new ArgumentOutOfRangeException(nameof(dieOne));
            DieOne = dieOne;
            DieTwo = dieTwo;
            BaseCheckModifier = Math.Max(-4, Math.Min(4, baseCheckModifier));
            EffectiveModifier = Math.Max(-4, Math.Min(5, effectiveModifier));
            Difficulty = Math.Max(0, difficulty);
            Outcome = Require(outcome, nameof(outcome));
            if (guildXp < 0 || hallXp < 0)
                throw new ArgumentOutOfRangeException(nameof(guildXp));
            GuildXp = guildXp;
            HallXp = hallXp;
            MaterialIds = CopyIds(materialIds);
            MomentumDelta = Math.Max(-2, Math.Min(2, momentumDelta));
            RecruitStableId = recruitStableId ?? string.Empty;
            AuthoritativeHash = Require(authoritativeHash, nameof(authoritativeHash));
            DelegatedToWorldGateCheck = delegatedToWorldGateCheck;
            AuthoritySchemaVersion = Math.Max(0, authoritySchemaVersion ?? 0);
            TreasuryXpCost = Math.Max(0, treasuryXpCost ?? 0);
            RequiresCertifiedBattle = requiresCertifiedBattle ?? false;
            BattleRequestId = battleRequestId ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            BattlePreStateHash = battlePreStateHash ?? string.Empty;
            BattleReturnCheckpointId = battleReturnCheckpointId ?? string.Empty;
            BattleReturnReceiptId = battleReturnReceiptId ?? string.Empty;
            Effect132 = effect132;
        }

        public string ReceiptId { get; }
        public string OperationId { get; }
        public string CardId { get; }
        public string NodeId { get; }
        public string ChoiceId { get; }
        public string ActorRecruitId { get; }
        public string AssistantRecruitId { get; }
        public int DieOne { get; }
        public int DieTwo { get; }
        public int BaseCheckModifier { get; }
        public int EffectiveModifier { get; }
        public int Difficulty { get; }
        public string Outcome { get; }
        public int GuildXp { get; }
        public int HallXp { get; }
        public IReadOnlyList<string> MaterialIds { get; }
        public int MomentumDelta { get; }
        public string RecruitStableId { get; }
        public string AuthoritativeHash { get; }
        public bool DelegatedToWorldGateCheck { get; }

        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int AuthoritySchemaVersion { get; }

        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int TreasuryXpCost { get; }

        [System.ComponentModel.DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool RequiresCertifiedBattle { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BattleRequestId { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BattleId { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BattlePreStateHash { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BattleReturnCheckpointId { get; }

        [System.ComponentModel.DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BattleReturnReceiptId { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ExpeditionEffectReceipt132 Effect132 { get; }

        static IReadOnlyList<string> CopyIds(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
                for (var index = 0; index < values.Count; index++)
                    if (!string.IsNullOrWhiteSpace(values[index]) &&
                        !result.Contains(values[index])) result.Add(values[index]);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    [Serializable]
    public sealed class ExpeditionDeckState089
    {
        public const string Version = "EXPEDITION_DECK_089_V1";

        [JsonConstructor]
        public ExpeditionDeckState089(
            string contentVersion,
            string deckId,
            string operationId,
            string shuffleSeedIdentity,
            IReadOnlyList<ExpeditionRouteCardState089> drawPile,
            IReadOnlyList<ExpeditionRouteCardState089> currentRow,
            IReadOnlyList<ExpeditionRouteCardState089> discardPile,
            IReadOnlyList<ExpeditionRouteCardState089> banishedCards,
            ExpeditionCardReceipt089 pendingReceipt,
            IReadOnlyList<ExpeditionCardReceipt089> appliedReceipts,
            IReadOnlyList<string> appliedReceiptIds,
            int momentum,
            IReadOnlyList<string> modifierLabels,
            IReadOnlyList<string> earnedRecruitLeadIds,
            int drawCount,
            int? progressionTier = null,
            IReadOnlyList<string> unlockedCategoryIds = null,
            int? cardsPerNodeAtCreation = null)
        {
            ContentVersion = string.IsNullOrWhiteSpace(contentVersion)
                ? Version : contentVersion;
            DeckId = Require(deckId, nameof(deckId));
            OperationId = Require(operationId, nameof(operationId));
            ShuffleSeedIdentity = Require(shuffleSeedIdentity,
                nameof(shuffleSeedIdentity));
            DrawPile = CopyCards(drawPile);
            CurrentRow = CopyCards(currentRow);
            DiscardPile = CopyCards(discardPile);
            BanishedCards = CopyCards(banishedCards);
            PendingReceipt = pendingReceipt;
            AppliedReceipts = CopyReceipts(appliedReceipts);
            AppliedReceiptIds = CopyIds(appliedReceiptIds);
            Momentum = Math.Max(-2, Math.Min(2, momentum));
            ModifierLabels = CopyIds(modifierLabels);
            EarnedRecruitLeadIds = CopyIds(earnedRecruitLeadIds);
            DrawCount = Math.Max(0, drawCount);
            ProgressionTier = Math.Max(1, progressionTier ?? 1);
            UnlockedCategoryIds = CopyIds(unlockedCategoryIds);
            CardsPerNodeAtCreation = Math.Max(6,
                cardsPerNodeAtCreation ?? 6);
        }

        public string ContentVersion { get; }
        public string DeckId { get; }
        public string OperationId { get; }
        public string ShuffleSeedIdentity { get; }
        public IReadOnlyList<ExpeditionRouteCardState089> DrawPile { get; }
        public IReadOnlyList<ExpeditionRouteCardState089> CurrentRow { get; }
        public IReadOnlyList<ExpeditionRouteCardState089> DiscardPile { get; }
        public IReadOnlyList<ExpeditionRouteCardState089> BanishedCards { get; }
        public ExpeditionCardReceipt089 PendingReceipt { get; }
        public IReadOnlyList<ExpeditionCardReceipt089> AppliedReceipts { get; }
        public IReadOnlyList<string> AppliedReceiptIds { get; }
        public int Momentum { get; }
        public IReadOnlyList<string> ModifierLabels { get; }
        public IReadOnlyList<string> EarnedRecruitLeadIds { get; }
        public int DrawCount { get; }

        [System.ComponentModel.DefaultValue(1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ProgressionTier { get; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IReadOnlyList<string> UnlockedCategoryIds { get; }

        [System.ComponentModel.DefaultValue(6)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int CardsPerNodeAtCreation { get; }

        public ExpeditionDeckState089 With(
            IReadOnlyList<ExpeditionRouteCardState089> drawPile = null,
            IReadOnlyList<ExpeditionRouteCardState089> currentRow = null,
            IReadOnlyList<ExpeditionRouteCardState089> discardPile = null,
            IReadOnlyList<ExpeditionRouteCardState089> banishedCards = null,
            ExpeditionCardReceipt089 pendingReceipt = null,
            bool replacePendingReceipt = false,
            IReadOnlyList<ExpeditionCardReceipt089> appliedReceipts = null,
            IReadOnlyList<string> appliedReceiptIds = null,
            int? momentum = null,
            IReadOnlyList<string> modifierLabels = null,
            IReadOnlyList<string> earnedRecruitLeadIds = null,
            int? drawCount = null,
            int? progressionTier = null,
            IReadOnlyList<string> unlockedCategoryIds = null,
            int? cardsPerNodeAtCreation = null) =>
            new ExpeditionDeckState089(ContentVersion, DeckId, OperationId,
                ShuffleSeedIdentity, drawPile ?? DrawPile, currentRow ?? CurrentRow,
                discardPile ?? DiscardPile, banishedCards ?? BanishedCards,
                replacePendingReceipt ? pendingReceipt : PendingReceipt,
                appliedReceipts ?? AppliedReceipts,
                appliedReceiptIds ?? AppliedReceiptIds, momentum ?? Momentum,
                modifierLabels ?? ModifierLabels,
                earnedRecruitLeadIds ?? EarnedRecruitLeadIds,
                drawCount ?? DrawCount,
                progressionTier ?? ProgressionTier,
                unlockedCategoryIds ?? UnlockedCategoryIds,
                cardsPerNodeAtCreation ?? CardsPerNodeAtCreation);

        static IReadOnlyList<ExpeditionRouteCardState089> CopyCards(
            IReadOnlyList<ExpeditionRouteCardState089> values)
        {
            var result = new List<ExpeditionRouteCardState089>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (values != null)
                for (var index = 0; index < values.Count; index++)
                {
                    var value = values[index] ?? throw new ArgumentException(
                        "Deck card cannot be null.", nameof(values));
                    if (seen.Add(value.CardId)) result.Add(value);
                }
            return result.AsReadOnly();
        }

        static IReadOnlyList<ExpeditionCardReceipt089> CopyReceipts(
            IReadOnlyList<ExpeditionCardReceipt089> values)
        {
            var result = new List<ExpeditionCardReceipt089>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (values != null)
                for (var index = 0; index < values.Count; index++)
                {
                    var value = values[index] ?? throw new ArgumentException(
                        "Deck receipt cannot be null.", nameof(values));
                    if (seen.Add(value.ReceiptId)) result.Add(value);
                }
            return result.AsReadOnly();
        }

        static IReadOnlyList<string> CopyIds(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
                for (var index = 0; index < values.Count; index++)
                    if (!string.IsNullOrWhiteSpace(values[index]) &&
                        !result.Contains(values[index])) result.Add(values[index]);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }
}
