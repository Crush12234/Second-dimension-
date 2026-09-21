using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.SpecialRelic001
{
    /// <summary>
    /// Local P0 mechanical authority for an Ultimate Art Seal.  The Library pack
    /// supplies names and design intent only; every executable gate below is authored
    /// here and is deliberately expressed in existing M2 state.
    /// </summary>
    public sealed class SpecialRelicUltimateArtRule001
    {
        internal SpecialRelicUltimateArtRule001(
            string relicId,
            string displayName,
            string ultimateArtName,
            string underlyingArtId,
            string equipmentTrackId,
            string preferredCommandId,
            string effectRole,
            int effectPotency,
            int requiredActionSharedApCost,
            int requiredActionPersonalMpCost,
            int minimumFormationBasisPoints,
            int minimumCohesion,
            int minimumMeaningfulUses,
            int minimumUnionMeaningfulUsePoints,
            int minimumRound,
            string targetGate,
            string battleStateGate)
        {
            RelicId = relicId;
            DisplayName = displayName;
            UltimateArtName = ultimateArtName;
            UnderlyingArtId = underlyingArtId;
            EquipmentTrackId = equipmentTrackId;
            PreferredCommandId = preferredCommandId;
            EffectRole = effectRole;
            EffectPotency = effectPotency;
            RequiredActionSharedApCost = requiredActionSharedApCost;
            RequiredActionPersonalMpCost = requiredActionPersonalMpCost;
            MinimumFormationBasisPoints = minimumFormationBasisPoints;
            MinimumCohesion = minimumCohesion;
            MinimumMeaningfulUses = minimumMeaningfulUses;
            MinimumUnionMeaningfulUsePoints = minimumUnionMeaningfulUsePoints;
            MinimumRound = minimumRound;
            TargetGate = targetGate;
            BattleStateGate = battleStateGate;
        }

        public string RelicId { get; }
        public string DisplayName { get; }
        public string UltimateArtName { get; }
        public string UnderlyingArtId { get; }
        public string EquipmentTrackId { get; }
        public string PreferredCommandId { get; }
        public string EffectRole { get; }
        public int EffectPotency { get; }
        public int RequiredActionSharedApCost { get; }
        public int RequiredActionPersonalMpCost { get; }
        public int MinimumFormationBasisPoints { get; }
        public int MinimumCohesion { get; }
        public int MinimumMeaningfulUses { get; }
        public int MinimumUnionMeaningfulUsePoints { get; }
        public int MinimumRound { get; }
        public string TargetGate { get; }
        public string BattleStateGate { get; }
        public string ActiveDisplayName => DisplayName + " • ACTIVE P0";
    }

    /// <summary>
    /// Deterministic proof used to decorate one already-complete Union Forecast.
    /// It contains no mutable state and adds no AP/MP surcharge.
    /// </summary>
    public sealed class SpecialRelicUltimateArtForecastAugmentation001
    {
        internal SpecialRelicUltimateArtForecastAugmentation001(
            SpecialRelicUltimateArtRule001 rule,
            int forecastSlot,
            string sealItemInstanceId,
            string invokerMemberId,
            string baseGenerationIdentity,
            string receiptId,
            string augmentedGenerationIdentity)
        {
            Rule = rule;
            ForecastSlot = forecastSlot;
            SealItemInstanceId = sealItemInstanceId;
            InvokerMemberId = invokerMemberId;
            BaseGenerationIdentity = baseGenerationIdentity;
            ReceiptId = receiptId;
            AugmentedGenerationIdentity = augmentedGenerationIdentity;
        }

        public SpecialRelicUltimateArtRule001 Rule { get; }
        public int ForecastSlot { get; }
        public string SealItemInstanceId { get; }
        public string InvokerMemberId { get; }
        public string BaseGenerationIdentity { get; }
        public string ReceiptId { get; }
        public string AugmentedGenerationIdentity { get; }
        public string ForecastId => SpecialRelicUltimateArtBattleHook001.ForecastId(AugmentedGenerationIdentity);
    }

    /// <summary>
    /// Immutable bridge proof emitted only after selected-forecast receipt
    /// authorization.  M2 can collect these during resolution and commit all growth
    /// atomically after the round succeeds.
    /// </summary>
    public sealed class SpecialRelicUltimateArtApplicationProof001
    {
        internal SpecialRelicUltimateArtApplicationProof001(
            string relicId,
            string underlyingArtId,
            string equipmentTrackId,
            string sealItemInstanceId,
            string receiptId,
            string battleId,
            int round,
            string unionId,
            string invokerMemberId,
            string forecastId,
            string eventStateHash)
        {
            RelicId = relicId;
            UnderlyingArtId = underlyingArtId;
            EquipmentTrackId = equipmentTrackId;
            SealItemInstanceId = sealItemInstanceId;
            ReceiptId = receiptId;
            BattleId = battleId;
            Round = round;
            UnionId = unionId;
            InvokerMemberId = invokerMemberId;
            ForecastId = forecastId;
            EventStateHash = eventStateHash;
        }

        public string RelicId { get; }
        public string UnderlyingArtId { get; }
        public string EquipmentTrackId { get; }
        public string SealItemInstanceId { get; }
        public string ReceiptId { get; }
        public string BattleId { get; }
        public int Round { get; }
        public string UnionId { get; }
        public string InvokerMemberId { get; }
        public string ForecastId { get; }
        public string EventStateHash { get; }
    }

    /// <summary>
    /// One globally unique, structurally valid P0 Seal that a member of the exact
    /// source Union manually equipped in the Tool/Relic slot.
    /// </summary>
    public sealed class SpecialRelicUltimateArtEquippedSeal001
    {
        internal SpecialRelicUltimateArtEquippedSeal001(
            SpecialRelicUltimateArtRule001 rule,
            EquipmentItemState item,
            string recruitId)
        {
            Rule = rule;
            Item = item;
            RecruitId = recruitId ?? string.Empty;
        }

        public SpecialRelicUltimateArtRule001 Rule { get; }
        public EquipmentItemState Item { get; }
        public string RecruitId { get; }
    }

    /// <summary>
    /// Additive bridge between P0 Ultimate Art Seals and certified M2 Union Forecasts.
    /// It never creates an individual command, replaces a member Art ID, rerolls a
    /// committed forecast, or adds a save-model field.  Its exact-once receipt is
    /// persisted through the existing BattleState event log.
    /// </summary>
    public static class SpecialRelicUltimateArtBattleHook001
    {
        public const string AuthorityVersion = "SPECIAL_RELIC_ULTIMATE_ART_BATTLE_001";
        public const string EventType = "ULTIMATE_ART_TRIGGERED";
        public const string ArtMarker = "ULTIMATE_ART_ID=";
        public const string ReceiptMarker = "ULTIMATE_RECEIPT=";
        public const string BaseIdentityMarker = "ULTIMATE_BASE_IDENTITY=";
        public const string SlotMarker = "ULTIMATE_FORECAST_SLOT=";
        public const string SealInstanceMarker = "ULTIMATE_SEAL_INSTANCE=";
        public const string CompleteForecastControl = "COMPLETE_UNION_FORECAST_ONLY";
        public const string SealEquipmentTag = "SPECIAL_RELIC_ULTIMATE_ART_SEAL";
        public const string P0ActiveEquipmentTag = "SPECIAL_RELIC_P0_ACTIVE";
        public const string QualityId = "EPIC";
        public const string EchoPendingCheckpointPrefix = "campaign022_echo_pending:";
        public const string CovenantBattlePendingCheckpointPrefix =
            "campaign022_covenant_battle_pending:";
        public const int GrantConditionBasisPoints = 10000;
        public const int MeaningfulUseMasteryGain = 5;

        private const string ReceiptHistorySeparator = " | ";
        private const string TargetActiveEnemy = "ACTIVE_ENEMY_UNION";
        private const string TargetSelf = "SOURCE_UNION";
        private const string TargetWoundedAlly = "WOUNDED_ALLY_UNION";

        private const string BattleEngagedEnemy = "ENGAGED_ENEMY";
        private const string BattleSourceUnderPressure = "SOURCE_UNDER_PRESSURE";
        private const string BattleBarrageWindow = "BARRAGE_WINDOW";
        private const string BattleExposedEnemy = "EXPOSED_ENEMY";
        private const string BattleCataclysmWindow = "CATACLYSM_WINDOW";
        private const string BattleRestorationEmergency = "RESTORATION_EMERGENCY";

        private static readonly SpecialRelicUltimateArtRule001[] RuleArray =
        {
            new SpecialRelicUltimateArtRule001(
                "RELIC001_ART_001", "Seal of Worldsplitter", "Worldsplitter",
                "TREE_CA002_WPN_SWORD_N11", "WTRACK022_01", "CMD_ALL_OUT", "COMBAT", 4,
                14, 0, 7500, 70, 5, 20, 2,
                TargetActiveEnemy, BattleEngagedEnemy),
            new SpecialRelicUltimateArtRule001(
                "RELIC001_ART_002", "Seal of Aegis Absolute", "Aegis Absolute",
                "TREE_CA002_WPN_SHIELD_N12", "WTRACK022_07", "CMD_GUARD", "GUARD", 4,
                14, 0, 6000, 60, 5, 16, 2,
                TargetSelf, BattleSourceUnderPressure),
            new SpecialRelicUltimateArtRule001(
                "RELIC001_ART_003", "Seal of Heavenfall Barrage", "Heavenfall Barrage",
                "TREE_CA002_WPN_BOW_N12", "WTRACK022_05", "CMD_ALL_OUT", "COMBAT", 4,
                14, 0, 7000, 65, 5, 20, 2,
                TargetActiveEnemy, BattleBarrageWindow),
            new SpecialRelicUltimateArtRule001(
                "RELIC001_ART_004", "Seal of Thousand Fang Tempest", "Thousand Fang Tempest",
                "TREE_CA002_WPN_DAGGER_N12", "WTRACK022_06", "CMD_ALL_OUT", "TACTICAL", 4,
                14, 0, 6500, 60, 5, 20, 2,
                TargetActiveEnemy, BattleExposedEnemy),
            new SpecialRelicUltimateArtRule001(
                "RELIC001_ART_005", "Seal of Starfire Cataclysm", "Starfire Cataclysm",
                "TREE_CA002_MYS_FLAME_N12", "WTRACK022_09", "CMD_MYSTIC", "MYSTIC", 5,
                15, 20, 8000, 75, 5, 24, 3,
                TargetActiveEnemy, BattleCataclysmWindow),
            new SpecialRelicUltimateArtRule001(
                "RELIC001_ART_006", "Seal of Grand Restoration", "Grand Restoration",
                "TREE_CA002_MYS_RESTORATION_N12", "WTRACK022_10", "CMD_HEAL", "RESTORATION", 5,
                15, 20, 5000, 50, 5, 16, 2,
                TargetWoundedAlly, BattleRestorationEmergency)
        };

        private static readonly IReadOnlyList<SpecialRelicUltimateArtRule001> ReadOnlyRules =
            Array.AsReadOnly(RuleArray);

        private static readonly Dictionary<string, SpecialRelicUltimateArtRule001> RulesByRelicId =
            RuleArray.ToDictionary(value => value.RelicId, value => value, StringComparer.Ordinal);

        public static IReadOnlyList<SpecialRelicUltimateArtRule001> All => ReadOnlyRules;

        public static bool TryGetRule(string relicId, out SpecialRelicUltimateArtRule001 rule) =>
            RulesByRelicId.TryGetValue(relicId ?? string.Empty, out rule);

        /// <summary>
        /// Creates the exact inventory shape used by natural and code grants.  The
        /// item is intentionally unequipped; normal M1 equipment commands own the
        /// manual Tool/Relic-slot decision.
        /// </summary>
        public static bool TryCreateP0SealGrantItem(
            string relicId,
            string instanceId,
            out EquipmentItemState item,
            out string error)
        {
            item = null;
            error = string.Empty;
            if (!TryGetRule(relicId, out var rule))
            {
                error = "SPECIAL_RELIC001_P0_ULTIMATE_REQUIRED";
                return false;
            }
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                error = "SPECIAL_RELIC001_SEAL_INSTANCE_REQUIRED";
                return false;
            }
            item = new EquipmentItemState(
                instanceId,
                rule.RelicId,
                rule.ActiveDisplayName,
                new[] { EquipmentSlotIds.ToolRelic },
                new[] { SealEquipmentTag, P0ActiveEquipmentTag },
                QualityId,
                GrantConditionBasisPoints,
                false);
            return true;
        }

        public static bool TryValidateP0SealItem(
            EquipmentItemState item,
            out SpecialRelicUltimateArtRule001 rule,
            out string error)
        {
            rule = null;
            error = string.Empty;
            if (item == null || !TryGetRule(item.DefinitionId, out rule))
            {
                error = "SPECIAL_RELIC001_P0_ULTIMATE_ITEM_REQUIRED";
                return false;
            }
            if (!StringComparer.Ordinal.Equals(item.DisplayName, rule.ActiveDisplayName) ||
                item.ValidSlotIds.Count != 1 ||
                !StringComparer.Ordinal.Equals(item.ValidSlotIds[0], EquipmentSlotIds.ToolRelic) ||
                item.EquipmentTags.Count != 2 ||
                !ContainsOrdinal(item.EquipmentTags, SealEquipmentTag) ||
                !ContainsOrdinal(item.EquipmentTags, P0ActiveEquipmentTag) ||
                !StringComparer.Ordinal.Equals(item.QualityId, QualityId) ||
                item.ConditionBasisPoints <= 0)
            {
                rule = null;
                error = "SPECIAL_RELIC001_P0_ULTIMATE_ITEM_INVALID";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Fails closed on unknown future ART definitions, copied instances, duplicate
        /// P0 definitions, or malformed local tags anywhere in Guild ownership.
        /// </summary>
        public static bool TryValidateGuildP0SealOwnership(GuildState guild, out string error) =>
            TryCollectOwnedP0Seals(guild, out _, out error);

        /// <summary>
        /// Resolves every distinct valid, manually equipped P0 Seal from members of
        /// this exact Union. Inventory-only and other-Union Seals grant no authority.
        /// </summary>
        public static bool TryResolveEquippedP0Seals(
            CampaignState campaign,
            BattleUnionState union,
            out IReadOnlyList<SpecialRelicUltimateArtEquippedSeal001> equippedSeals,
            out string error)
        {
            equippedSeals = Array.Empty<SpecialRelicUltimateArtEquippedSeal001>();
            error = string.Empty;
            if (campaign?.Guild == null || union == null)
            {
                error = "SPECIAL_RELIC001_SEAL_CONTEXT_REQUIRED";
                return false;
            }
            if (!TryCollectOwnedP0Seals(campaign.Guild, out var owned, out error))
                return false;

            var memberIds = new HashSet<string>(
                union.Members.Select(value => value.MemberId),
                StringComparer.Ordinal);
            var result = new List<SpecialRelicUltimateArtEquippedSeal001>();
            for (var index = 0; index < owned.Count; index++)
            {
                var location = owned[index];
                if (string.IsNullOrWhiteSpace(location.EquippedRecruitId) ||
                    !memberIds.Contains(location.EquippedRecruitId))
                    continue;
                if (!TryValidateP0SealItem(location.Item, out var rule, out error)) return false;
                result.Add(new SpecialRelicUltimateArtEquippedSeal001(
                    rule, location.Item, location.EquippedRecruitId));
            }
            if (result.Count == 0)
            {
                error = "SPECIAL_RELIC001_EQUIPPED_SEAL_REQUIRED";
                return false;
            }
            result.Sort((left, right) =>
            {
                var byRelic = StringComparer.Ordinal.Compare(
                    left.Rule.RelicId, right.Rule.RelicId);
                return byRelic != 0
                    ? byRelic
                    : StringComparer.Ordinal.Compare(left.Item.InstanceId, right.Item.InstanceId);
            });
            equippedSeals = result.AsReadOnly();
            return true;
        }

        /// <summary>
        /// Compatibility view for callers that need one stable equipped Seal outside
        /// forecast selection. Multiple distinct Seals coexist; forecast authority
        /// evaluates all of them through TrySelectForecastAugmentation.
        /// </summary>
        public static bool TryResolveEquippedP0Seal(
            CampaignState campaign,
            BattleUnionState union,
            out SpecialRelicUltimateArtRule001 rule,
            out EquipmentItemState sealItem,
            out string error)
        {
            rule = null;
            sealItem = null;
            if (!TryResolveEquippedP0Seals(campaign, union, out var equipped, out error))
                return false;
            rule = equipped[0].Rule;
            sealItem = equipped[0].Item;
            return true;
        }

        /// <summary>
        /// Strict startup seam for the prose catalog.  Executable balance remains in
        /// this class, but the six compiled bindings must retain the exact P0 design
        /// IDs, names and Ultimate Art aliases supplied by the safe Library payload.
        /// </summary>
        public static bool TryValidateP0CatalogIdentity(
            ISpecialRelicCatalog001 catalog,
            out string error)
        {
            error = string.Empty;
            if (catalog == null)
            {
                error = "SPECIAL_RELIC001_CATALOG_REQUIRED";
                return false;
            }
            if (catalog.P0Count != 12 || catalog.P0Relics == null || catalog.P0Relics.Count != 12)
            {
                error = "SPECIAL_RELIC001_P0_COUNT_INVALID";
                return false;
            }

            var ultimateCount = 0;
            for (var p0Index = 0; p0Index < catalog.P0Relics.Count; p0Index++)
            {
                var value = catalog.P0Relics[p0Index];
                if (value != null && StringComparer.Ordinal.Equals(value.Kind, "ULTIMATE_ART"))
                    ultimateCount++;
            }
            if (ultimateCount != RuleArray.Length)
            {
                error = "SPECIAL_RELIC001_P0_ULTIMATE_COUNT_INVALID";
                return false;
            }

            for (var ruleIndex = 0; ruleIndex < RuleArray.Length; ruleIndex++)
            {
                var rule = RuleArray[ruleIndex];
                if (!catalog.IsP0(rule.RelicId) ||
                    !catalog.TryGetRelic(rule.RelicId, out var design) ||
                    design == null ||
                    !StringComparer.Ordinal.Equals(design.RelicId, rule.RelicId) ||
                    !StringComparer.Ordinal.Equals(design.Name, rule.DisplayName) ||
                    !StringComparer.Ordinal.Equals(design.Kind, "ULTIMATE_ART") ||
                    !StringComparer.OrdinalIgnoreCase.Equals(design.Rarity, "Epic") ||
                    !StringComparer.Ordinal.Equals(design.UltimateArt, rule.UltimateArtName) ||
                    !string.IsNullOrWhiteSpace(design.Summon) ||
                    !design.IsPublicCodeEligible)
                {
                    error = "SPECIAL_RELIC001_P0_ULTIMATE_IDENTITY_INVALID:" + rule.RelicId;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Selects at most one owned, state-eligible Seal for this exact complete
        /// Forecast card. A different card may preview a different contextual Seal;
        /// execution still authorizes only one Ultimate for the Union/round. Ties use
        /// ordinal receipt hashes over stable IDs, never mutable RNG.
        /// </summary>
        public static bool TrySelectForecastAugmentation(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState union,
            int forecastSlot,
            BattleForecastState baseForecast,
            out SpecialRelicUltimateArtForecastAugmentation001 augmentation)
        {
            augmentation = null;
            return TryChooseEligibleAugmentation(
                campaign,
                battle,
                union,
                forecastSlot,
                baseForecast,
                baseForecast?.GenerationIdentity,
                false,
                out augmentation);
        }

        public static BattleForecastState ApplyForecastAugmentation(
            BattleForecastState baseForecast,
            SpecialRelicUltimateArtForecastAugmentation001 augmentation)
        {
            if (baseForecast == null) throw new ArgumentNullException(nameof(baseForecast));
            if (augmentation?.Rule == null) throw new ArgumentNullException(nameof(augmentation));
            if (!StringComparer.Ordinal.Equals(
                    baseForecast.GenerationIdentity, augmentation.BaseGenerationIdentity))
                throw new InvalidOperationException("Ultimate Art augmentation base identity changed.");

            var rule = augmentation.Rule;
            var marker = MarkerText(augmentation);
            var actionIds = baseForecast.MemberActions
                .Select(value => value.ArtId)
                .ToArray();
            var debug = CanonicalJson.Serialize(new
            {
                AuthorityVersion,
                BaseForecastId = baseForecast.ForecastId,
                BaseGenerationIdentity = augmentation.BaseGenerationIdentity,
                BaseDeterministicDebugEvidence = baseForecast.DeterministicDebugEvidence,
                UltimateRelicId = rule.RelicId,
                rule.UltimateArtName,
                rule.UnderlyingArtId,
                augmentation.InvokerMemberId,
                augmentation.SealItemInstanceId,
                augmentation.ForecastSlot,
                augmentation.ReceiptId,
                UnderlyingMemberArtIds = actionIds,
                UnderlyingMemberArtIdsPreserved = true,
                HiddenSharedApSurcharge = 0,
                HiddenPersonalMpSurcharge = 0,
                Control = CompleteForecastControl
            });

            return new BattleForecastState(
                augmentation.ForecastId,
                baseForecast.UnionId,
                baseForecast.CommandId,
                "ULTIMATE ART — " + rule.UltimateArtName + " • " + baseForecast.CommandName,
                "Unseal “" + rule.UltimateArtName + "” through this complete Union Forecast. " +
                baseForecast.Phrase,
                baseForecast.TacticalIntent + " · ULTIMATE ART",
                baseForecast.TargetId,
                baseForecast.TargetName,
                baseForecast.MemberActions,
                baseForecast.SharedApCost,
                baseForecast.ApRecovery,
                baseForecast.CombinedMpCost,
                baseForecast.ExpectedEffect +
                " · Ultimate seal resonance is authorized; underlying member Art resolution remains authoritative.",
                baseForecast.Risk,
                AppendMarker(baseForecast.LearningOpportunity, marker),
                baseForecast.FallbackBehavior,
                augmentation.AugmentedGenerationIdentity,
                debug);
        }

        public static bool TryAugmentForecast(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState union,
            int forecastSlot,
            BattleForecastState baseForecast,
            out BattleForecastState augmentedForecast)
        {
            augmentedForecast = baseForecast;
            if (!TrySelectForecastAugmentation(
                    campaign, battle, union, forecastSlot, baseForecast, out var augmentation))
                return false;
            augmentedForecast = ApplyForecastAugmentation(baseForecast, augmentation);
            return true;
        }

        /// <summary>
        /// Validates an augmented selected forecast and emits one receipt-bearing
        /// event.  M2 may opt into the existing InvocationBattleEffects022 adapter;
        /// false leaves all mechanical resolution to the underlying member Art.
        /// </summary>
        public static bool TryApplySelectedForecast(
            CampaignState campaign,
            BattleState battle,
            BattleForecastState forecast,
            int unionIndex,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events,
            bool applyInvocationBattleEffect = false) =>
            TryApplySelectedForecast(
                campaign,
                battle,
                forecast,
                unionIndex,
                players,
                enemies,
                events,
                out _,
                applyInvocationBattleEffect);

        public static bool TryApplySelectedForecast(
            CampaignState campaign,
            BattleState battle,
            BattleForecastState forecast,
            int unionIndex,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events,
            out SpecialRelicUltimateArtApplicationProof001 applicationProof,
            bool applyInvocationBattleEffect = false)
        {
            applicationProof = null;
            if (campaign == null || battle == null || forecast == null ||
                players == null || enemies == null || events == null ||
                unionIndex < 0 || unionIndex >= players.Count)
                return false;
            if (!TryReadMarker(
                    forecast.LearningOpportunity,
                    out var relicId,
                    out var receipt,
                    out var baseIdentity,
                    out var forecastSlot,
                    out var sealItemInstanceId))
                return false;
            if (!TryGetRule(relicId, out var rule)) return false;

            var sourceUnion = FindUnion(battle.PlayerUnions, forecast.UnionId);
            if (sourceUnion == null ||
                !StringComparer.Ordinal.Equals(players[unionIndex].UnionId, sourceUnion.UnionId))
                return false;
            if (!TryChooseEligibleAugmentation(
                    campaign,
                    battle,
                    sourceUnion,
                    forecastSlot,
                    forecast,
                    baseIdentity,
                    true,
                    out var selected) ||
                !StringComparer.Ordinal.Equals(selected.Rule.RelicId, rule.RelicId) ||
                !StringComparer.Ordinal.Equals(selected.SealItemInstanceId, sealItemInstanceId))
                return false;
            var invokerMemberId = selected.InvokerMemberId;

            var expectedBaseIdentity = ExpectedBaseIdentity(campaign, battle, sourceUnion, forecastSlot);
            if (!StringComparer.Ordinal.Equals(baseIdentity, expectedBaseIdentity)) return false;
            var expectedReceipt = BuildReceipt(
                campaign, battle, sourceUnion, forecastSlot, baseIdentity,
                forecast.TargetId, rule, sealItemInstanceId, invokerMemberId);
            if (!StringComparer.Ordinal.Equals(receipt, expectedReceipt)) return false;
            var expectedAugmentedIdentity = BuildAugmentedIdentity(baseIdentity, relicId, receipt);
            if (!StringComparer.Ordinal.Equals(forecast.GenerationIdentity, expectedAugmentedIdentity) ||
                !StringComparer.Ordinal.Equals(forecast.ForecastId, ForecastId(expectedAugmentedIdentity)))
                return false;
            if (HasAppliedReceipt(battle.EventLog, receipt) || HasAppliedReceipt(events, receipt) ||
                HasAppliedUnionRound(battle.EventLog, battle.Round, sourceUnion.UnionId) ||
                HasAppliedUnionRound(events, battle.Round, sourceUnion.UnionId))
                return false;

            InvocationBattleEffect022 effect = null;
            // The shared Restoration adapter intentionally prioritizes the source
            // Union.  Suppress it for a cross-Union Grand Restoration so the exact
            // mapped action target remains authoritative.
            var preserveCrossUnionRestorationTarget =
                StringComparer.Ordinal.Equals(rule.EffectRole, "RESTORATION") &&
                !StringComparer.Ordinal.Equals(forecast.TargetId, sourceUnion.UnionId);
            if (applyInvocationBattleEffect && !preserveCrossUnionRestorationTarget)
            {
                effect = InvocationBattleEffects022.Apply(
                    rule.EffectRole,
                    rule.EffectPotency,
                    sourceUnion.UnionId,
                    invokerMemberId,
                    forecast.TargetId,
                    players,
                    enemies);
            }

            var amount = effect?.Amount ?? 0;
            var targetUnionId = effect?.TargetUnionId ?? forecast.TargetId;
            var targetMemberId = effect?.TargetMemberId ?? string.Empty;
            var summary = effect == null
                ? preserveCrossUnionRestorationTarget
                    ? "The cross-Union Restoration target remains owned by the mapped member Art; no source-prioritized adapter effect is added."
                    : "Underlying member Arts resolve without an added adapter effect."
                : effect.Summary + ".";
            var text = sourceUnion.DisplayName + " unseals “" + rule.UltimateArtName +
                       "” through a complete Union Forecast. " + summary + " " +
                       ReceiptMarker + receipt;
            var stateHash = CanonicalJson.Sha256Hex(new
            {
                AuthorityVersion,
                battle.BattleId,
                battle.Round,
                sourceUnion.UnionId,
                InvokerMemberId = invokerMemberId,
                rule.RelicId,
                rule.UnderlyingArtId,
                SealItemInstanceId = sealItemInstanceId,
                ReceiptId = receipt,
                forecast.ForecastId,
                forecast.GenerationIdentity,
                Amount = amount,
                TargetUnionId = targetUnionId,
                TargetMemberId = targetMemberId,
                Control = CompleteForecastControl
            });
            var appliedEvent = new BattleEventState(
                events.Count,
                battle.Round,
                EventType,
                BattleSide.Player,
                sourceUnion.UnionId,
                invokerMemberId,
                rule.UnderlyingArtId,
                text,
                amount,
                stateHash,
                sourceUnion.UnionId,
                invokerMemberId,
                targetUnionId,
                targetMemberId);
            events.Add(appliedEvent);
            applicationProof = new SpecialRelicUltimateArtApplicationProof001(
                rule.RelicId,
                rule.UnderlyingArtId,
                rule.EquipmentTrackId,
                sealItemInstanceId,
                receipt,
                battle.BattleId,
                battle.Round,
                sourceUnion.UnionId,
                invokerMemberId,
                forecast.ForecastId,
                appliedEvent.StateHash);
            return true;
        }

        /// <summary>
        /// Atomically commits every authorized Ultimate application into the existing
        /// Campaign022 progression state.  AppliedReceiptIds is the exact-once law;
        /// no Seal-specific save model is introduced.
        /// </summary>
        public static Result<CampaignState> ApplyProgression(
            CampaignState campaign,
            IReadOnlyList<SpecialRelicUltimateArtApplicationProof001> applications)
        {
            if (campaign?.Guild?.GuildCity == null)
                return Result<CampaignState>.Failure("SPECIAL_RELIC001_PROGRESSION_CONTEXT_REQUIRED");
            if (applications == null || applications.Count == 0)
                return Result<CampaignState>.Success(campaign);

            var receiptInput = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < applications.Count; index++)
            {
                var proof = applications[index];
                if (proof == null || string.IsNullOrWhiteSpace(proof.ReceiptId))
                    return Result<CampaignState>.Failure("SPECIAL_RELIC001_APPLICATION_PROOF_REQUIRED");
                if (!receiptInput.Add(proof.ReceiptId))
                    return Result<CampaignState>.Failure("SPECIAL_RELIC001_APPLICATION_PROOF_DUPLICATE");
            }

            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var playable = progress.Playable020 ?? CampaignPlayableState020.Default();
            var state = playable.Progression022 ?? CampaignProgressionState022.Default();
            var equipmentEvolution = new List<EquipmentEvolutionState022>(state.EquipmentEvolution);
            var receipts = new List<string>(state.AppliedReceiptIds);
            var changed = false;
            var lastAppliedReceipt = string.Empty;

            var ordered = applications.OrderBy(
                value => value.ReceiptId,
                StringComparer.Ordinal).ToArray();
            for (var applicationIndex = 0; applicationIndex < ordered.Length; applicationIndex++)
            {
                var proof = ordered[applicationIndex];
                if (!TryValidateApplicationProof(campaign, proof, out var rule, out var error))
                    return Result<CampaignState>.Failure(error);

                if (ContainsOrdinal(receipts, proof.ReceiptId))
                {
                    var replayMatches = equipmentEvolution.Where(value =>
                        StringComparer.Ordinal.Equals(
                            value.ItemInstanceId, proof.SealItemInstanceId)).ToArray();
                    if (replayMatches.Length != 1 ||
                        !StringComparer.Ordinal.Equals(
                            replayMatches[0].TrackId, rule.EquipmentTrackId) ||
                        !HasExactReceiptHistory(replayMatches[0].HistoryTag, proof.ReceiptId))
                        return Result<CampaignState>.Failure(
                            "SPECIAL_RELIC001_APPLIED_RECEIPT_EVOLUTION_EVIDENCE_REQUIRED:" +
                            proof.ReceiptId);
                    if (!TryValidateUltimateReceiptHistory(
                            replayMatches[0].HistoryTag,
                            receipts,
                            out var replayHistoryError))
                        return Result<CampaignState>.Failure(replayHistoryError);
                    var receiptHistoryCount = CountUltimateReceiptHistory(
                        replayMatches[0].HistoryTag);
                    if (receiptHistoryCount < 1 ||
                        replayMatches[0].MeaningfulUses < receiptHistoryCount ||
                        replayMatches[0].MasteryPoints <
                            checked(receiptHistoryCount * MeaningfulUseMasteryGain))
                        return Result<CampaignState>.Failure(
                            "SPECIAL_RELIC001_APPLIED_RECEIPT_MASTERY_EVIDENCE_REQUIRED:" +
                            proof.ReceiptId);
                    continue;
                }

                var matches = equipmentEvolution.Count(value =>
                    StringComparer.Ordinal.Equals(value.ItemInstanceId, proof.SealItemInstanceId));
                if (matches > 1)
                    return Result<CampaignState>.Failure(
                        "SPECIAL_RELIC001_SEAL_EVOLUTION_AMBIGUOUS:" + proof.SealItemInstanceId);
                var evolutionIndex = equipmentEvolution.FindIndex(value =>
                    StringComparer.Ordinal.Equals(value.ItemInstanceId, proof.SealItemInstanceId));
                var evolution = evolutionIndex >= 0
                    ? equipmentEvolution[evolutionIndex]
                    : new EquipmentEvolutionState022(
                        proof.SealItemInstanceId,
                        rule.EquipmentTrackId,
                        "TRAINING",
                        0,
                        0,
                        Array.Empty<string>(),
                        string.Empty);
                if (!StringComparer.Ordinal.Equals(evolution.TrackId, rule.EquipmentTrackId))
                    return Result<CampaignState>.Failure(
                        "SPECIAL_RELIC001_SEAL_TRACK_IMMUTABLE:" + proof.SealItemInstanceId);
                if (HasExactReceiptHistory(evolution.HistoryTag, proof.ReceiptId))
                    return Result<CampaignState>.Failure(
                        "SPECIAL_RELIC001_INCOMING_RECEIPT_HISTORY_ORPHANED:" +
                        proof.ReceiptId);
                if (!TryValidateUltimateReceiptHistory(
                        evolution.HistoryTag, receipts, out var historyError))
                    return Result<CampaignState>.Failure(historyError);
                evolution = evolution.With(
                    meaningfulUses: checked(evolution.MeaningfulUses + 1),
                    masteryPoints: checked(evolution.MasteryPoints + MeaningfulUseMasteryGain),
                    historyTag: AppendReceiptHistory(evolution.HistoryTag, proof.ReceiptId));
                if (evolutionIndex >= 0) equipmentEvolution[evolutionIndex] = evolution;
                else equipmentEvolution.Add(evolution);
                receipts.Add(proof.ReceiptId);
                changed = true;
                lastAppliedReceipt = proof.ReceiptId;
            }
            if (!changed) return Result<CampaignState>.Success(campaign);

            receipts.Sort(StringComparer.Ordinal);
            if (!TryResolveProgressionCheckpoint(
                    "special_relic001_ultimate_applied:" + lastAppliedReceipt,
                    out var checkpoint,
                    out var checkpointError,
                    state.LastCheckpointId,
                    playable.LastCheckpointId,
                    progress.LastCheckpointId,
                    strategic.LastCheckpointId,
                    city.LastCheckpointId))
                return Result<CampaignState>.Failure(checkpointError);
            var next = state.With(
                equipmentEvolution: equipmentEvolution.AsReadOnly(),
                appliedReceiptIds: receipts.AsReadOnly(),
                lastCheckpointId: checkpoint);
            playable = playable.With(
                progression022: next,
                replaceProgression022: true,
                lastCheckpointId: checkpoint);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: checkpoint);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: checkpoint);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: checkpoint);
            return Result<CampaignState>.Success(
                campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow));
        }

        public static string MarkerText(
            SpecialRelicUltimateArtForecastAugmentation001 augmentation)
        {
            if (augmentation?.Rule == null) return string.Empty;
            return ArtMarker + augmentation.Rule.RelicId + " · " +
                   ReceiptMarker + augmentation.ReceiptId + " · " +
                   BaseIdentityMarker + augmentation.BaseGenerationIdentity + " · " +
                   SlotMarker + augmentation.ForecastSlot + " · " +
                   SealInstanceMarker + augmentation.SealItemInstanceId + " · " +
                   "Complete Forecast only · underlying member Art IDs preserved · " +
                   "one executed Ultimate Art per Union/round.";
        }

        internal static string ForecastId(string augmentedIdentity) =>
            string.IsNullOrWhiteSpace(augmentedIdentity)
                ? string.Empty
                : "FORECAST_" + augmentedIdentity.Substring(0, 16).ToUpperInvariant();

        private static bool TryChooseEligibleAugmentation(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState union,
            int forecastSlot,
            BattleForecastState forecast,
            string baseGenerationIdentity,
            bool allowUltimateMarker,
            out SpecialRelicUltimateArtForecastAugmentation001 augmentation)
        {
            augmentation = null;
            if (campaign == null || battle == null || union == null || forecast == null ||
                forecastSlot < 0 || string.IsNullOrWhiteSpace(baseGenerationIdentity) ||
                battle.Outcome != BattleOutcome.InProgress ||
                battle.Phase != BattlePhase.ForecastSelection ||
                union.Side != BattleSide.Player || union.Retreated || union.IsDefeated ||
                HasAppliedUnionRound(battle.EventLog, battle.Round, union.UnionId) ||
                !StringComparer.Ordinal.Equals(
                    baseGenerationIdentity,
                    ExpectedBaseIdentity(campaign, battle, union, forecastSlot)))
                return false;
            if (!TryResolveEquippedP0Seals(
                    campaign, union, out var equippedSeals, out _))
                return false;

            var eligible = new List<SpecialRelicUltimateArtForecastAugmentation001>();
            for (var index = 0; index < equippedSeals.Count; index++)
            {
                var equipped = equippedSeals[index];
                var rule = equipped.Rule;
                if (!PassesUnionStateGates(battle, union, rule) ||
                    !HasMasteredAffordableActor(union, rule) ||
                    !TryValidateForecastForRule(
                        battle,
                        union,
                        forecastSlot,
                        forecast,
                        rule,
                        allowUltimateMarker,
                        out var invokerMemberId))
                    continue;
                var receipt = BuildReceipt(
                    campaign,
                    battle,
                    union,
                    forecastSlot,
                    baseGenerationIdentity,
                    forecast.TargetId,
                    rule,
                    equipped.Item.InstanceId,
                    invokerMemberId);
                eligible.Add(new SpecialRelicUltimateArtForecastAugmentation001(
                    rule,
                    forecastSlot,
                    equipped.Item.InstanceId,
                    invokerMemberId,
                    baseGenerationIdentity,
                    receipt,
                    BuildAugmentedIdentity(baseGenerationIdentity, rule.RelicId, receipt)));
            }
            if (eligible.Count == 0) return false;

            // A complete card previews at most one contextual Seal. Receipt hashes
            // make ties deterministic without RNG; execution separately enforces one
            // Ultimate event/receipt for this Union and round across every card.
            eligible.Sort((left, right) =>
            {
                var byReceipt = StringComparer.Ordinal.Compare(left.ReceiptId, right.ReceiptId);
                if (byReceipt != 0) return byReceipt;
                var byRelic = StringComparer.Ordinal.Compare(
                    left.Rule.RelicId, right.Rule.RelicId);
                return byRelic != 0
                    ? byRelic
                    : StringComparer.Ordinal.Compare(
                        left.SealItemInstanceId, right.SealItemInstanceId);
            });
            augmentation = eligible[0];
            return true;
        }

        private static bool TryValidateForecastForRule(
            BattleState battle,
            BattleUnionState union,
            int forecastSlot,
            BattleForecastState forecast,
            SpecialRelicUltimateArtRule001 rule,
            bool allowUltimateMarker,
            out string invokerMemberId)
        {
            invokerMemberId = string.Empty;
            if (battle == null || union == null || forecast == null || rule == null ||
                forecastSlot < 0 ||
                !StringComparer.Ordinal.Equals(forecast.UnionId, union.UnionId) ||
                !StringComparer.Ordinal.Equals(forecast.CommandId, rule.PreferredCommandId) ||
                forecast.MemberActions.Count != union.Members.Count ||
                forecast.SharedApCost > union.CurrentAp ||
                SumSharedAp(forecast.MemberActions) != forecast.SharedApCost ||
                SumPersonalMp(forecast.MemberActions) != forecast.CombinedMpCost ||
                !IndividualMpAffordable(union, forecast.MemberActions) ||
                ContainsMarker(forecast.LearningOpportunity, "LINK_ART_ID=") ||
                !allowUltimateMarker && ContainsMarker(forecast.LearningOpportunity, ArtMarker))
                return false;

            BattlePlannedActionState mappedAction = null;
            BattleMemberState mappedMember = null;
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                if (member.Downed || !HasRequiredMastery(member, rule)) continue;
                for (var actionIndex = 0; actionIndex < forecast.MemberActions.Count; actionIndex++)
                {
                    var action = forecast.MemberActions[actionIndex];
                    if (!StringComparer.Ordinal.Equals(action.ActorMemberId, member.MemberId) ||
                        !StringComparer.Ordinal.Equals(action.ArtId, rule.UnderlyingArtId) ||
                        action.SharedApCost != rule.RequiredActionSharedApCost ||
                        action.PersonalMpCost != rule.RequiredActionPersonalMpCost)
                        continue;
                    if (mappedMember == null ||
                        StringComparer.Ordinal.Compare(member.MemberId, mappedMember.MemberId) < 0)
                    {
                        mappedMember = member;
                        mappedAction = action;
                    }
                }
            }
            if (mappedAction == null || mappedMember == null) return false;
            if (!PassesExactTargetGate(battle, union, forecast, mappedAction, rule)) return false;
            if (!PassesBattleStateGate(battle, union, FindForecastTarget(battle, union, forecast, rule), rule))
                return false;
            invokerMemberId = mappedMember.MemberId;
            return true;
        }

        private static bool PassesUnionStateGates(
            BattleState battle,
            BattleUnionState union,
            SpecialRelicUltimateArtRule001 rule)
        {
            if (battle.Round < rule.MinimumRound ||
                !union.FormationBenefitActive ||
                union.FormationConditionBasisPoints < rule.MinimumFormationBasisPoints ||
                union.Cohesion < rule.MinimumCohesion ||
                union.UnionMeaningfulUsePoints < rule.MinimumUnionMeaningfulUsePoints ||
                union.CurrentAp < rule.RequiredActionSharedApCost)
                return false;
            var target = DefaultTarget(battle, union, rule);
            return target != null && PassesBattleStateGate(battle, union, target, rule);
        }

        private static bool HasMasteredAffordableActor(
            BattleUnionState union,
            SpecialRelicUltimateArtRule001 rule)
        {
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                if (!member.Downed &&
                    member.CurrentMp >= rule.RequiredActionPersonalMpCost &&
                    HasRequiredMastery(member, rule))
                    return true;
            }
            return false;
        }

        private static bool HasRequiredMastery(
            BattleMemberState member,
            SpecialRelicUltimateArtRule001 rule)
        {
            if (!ContainsOrdinal(member.LearnedArtIds, rule.UnderlyingArtId)) return false;
            for (var index = 0; index < member.ArtProgress.Count; index++)
            {
                var progress = member.ArtProgress[index];
                if (StringComparer.Ordinal.Equals(progress.ArtId, rule.UnderlyingArtId) &&
                    progress.MeaningfulUses >= rule.MinimumMeaningfulUses &&
                    progress.MasteryPoints > 0)
                    return true;
            }
            return false;
        }

        private static bool PassesExactTargetGate(
            BattleState battle,
            BattleUnionState source,
            BattleForecastState forecast,
            BattlePlannedActionState mappedAction,
            SpecialRelicUltimateArtRule001 rule)
        {
            var target = FindForecastTarget(battle, source, forecast, rule);
            if (target == null) return false;
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetActiveEnemy))
                return target.Side == BattleSide.Enemy && IsActive(target) &&
                       StringComparer.Ordinal.Equals(mappedAction.TargetUnionId, target.UnionId);
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetSelf))
                return StringComparer.Ordinal.Equals(target.UnionId, source.UnionId) &&
                       StringComparer.Ordinal.Equals(mappedAction.TargetUnionId, source.UnionId);
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetWoundedAlly))
                return target.Side == BattleSide.Player && !target.Retreated && HasEmergencyWound(target) &&
                       StringComparer.Ordinal.Equals(mappedAction.TargetUnionId, target.UnionId);
            return false;
        }

        private static bool PassesBattleStateGate(
            BattleState battle,
            BattleUnionState source,
            BattleUnionState target,
            SpecialRelicUltimateArtRule001 rule)
        {
            if (StringComparer.Ordinal.Equals(rule.BattleStateGate, BattleEngagedEnemy))
                return target.Side == BattleSide.Enemy &&
                       target.Engagement != EngagementState.Open &&
                       target.Engagement != EngagementState.Disengaging;
            if (StringComparer.Ordinal.Equals(rule.BattleStateGate, BattleSourceUnderPressure))
                return AverageHpPercent(source) <= 75 || source.Cohesion <= 70 ||
                       source.FormationConditionBasisPoints <= 7500;
            if (StringComparer.Ordinal.Equals(rule.BattleStateGate, BattleBarrageWindow))
                return CountActive(battle.EnemyUnions) >= 2 || AverageHpPercent(target) <= 65;
            if (StringComparer.Ordinal.Equals(rule.BattleStateGate, BattleExposedEnemy))
                return target.FormationConditionBasisPoints <= 6000 || target.Cohesion <= 60 ||
                       target.Engagement == EngagementState.Broken ||
                       target.Engagement == EngagementState.Flanking ||
                       target.Engagement == EngagementState.RearPressure;
            if (StringComparer.Ordinal.Equals(rule.BattleStateGate, BattleCataclysmWindow))
                return CountActive(battle.EnemyUnions) >= 2 || AverageHpPercent(target) <= 50 ||
                       target.Cohesion <= 50;
            if (StringComparer.Ordinal.Equals(rule.BattleStateGate, BattleRestorationEmergency))
                return HasEmergencyWound(target);
            return false;
        }

        private static BattleUnionState DefaultTarget(
            BattleState battle,
            BattleUnionState source,
            SpecialRelicUltimateArtRule001 rule)
        {
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetSelf)) return source;
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetWoundedAlly))
                return MostWoundedUnion(battle.PlayerUnions);
            return FirstActiveUnion(battle.EnemyUnions);
        }

        private static BattleUnionState FindForecastTarget(
            BattleState battle,
            BattleUnionState source,
            BattleForecastState forecast,
            SpecialRelicUltimateArtRule001 rule)
        {
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetSelf))
                return StringComparer.Ordinal.Equals(forecast.TargetId, source.UnionId) ? source : null;
            if (StringComparer.Ordinal.Equals(rule.TargetGate, TargetWoundedAlly))
                return FindUnion(battle.PlayerUnions, forecast.TargetId);
            return FindUnion(battle.EnemyUnions, forecast.TargetId);
        }

        private static BattleUnionState MostWoundedUnion(IReadOnlyList<BattleUnionState> unions)
        {
            BattleUnionState best = null;
            var bestNeed = 0;
            for (var unionIndex = 0; unionIndex < (unions?.Count ?? 0); unionIndex++)
            {
                var union = unions[unionIndex];
                if (union == null || union.Retreated) continue;
                var need = 0;
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    if (member.Downed) need += 100000;
                    need += Math.Max(0, member.MaximumHp - member.CurrentHp);
                }
                if (need > bestNeed || need == bestNeed && need > 0 && best != null &&
                    StringComparer.Ordinal.Compare(union.UnionId, best.UnionId) < 0)
                {
                    best = union;
                    bestNeed = need;
                }
            }
            return bestNeed > 0 ? best : null;
        }

        private static BattleUnionState FirstActiveUnion(IReadOnlyList<BattleUnionState> unions)
        {
            for (var index = 0; index < (unions?.Count ?? 0); index++)
                if (IsActive(unions[index])) return unions[index];
            return null;
        }

        private static BattleUnionState FindUnion(
            IReadOnlyList<BattleUnionState> unions,
            string unionId)
        {
            for (var index = 0; index < (unions?.Count ?? 0); index++)
                if (StringComparer.Ordinal.Equals(unions[index].UnionId, unionId)) return unions[index];
            return null;
        }

        private static bool HasEmergencyWound(BattleUnionState union)
        {
            if (union == null || union.Retreated) return false;
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                if (member.Downed || member.CurrentHp * 2 <= member.MaximumHp) return true;
            }
            return false;
        }

        private static int AverageHpPercent(BattleUnionState union)
        {
            if (union == null) return 0;
            var current = 0;
            var maximum = 0;
            for (var index = 0; index < union.Members.Count; index++)
            {
                current += union.Members[index].CurrentHp;
                maximum += union.Members[index].MaximumHp;
            }
            return maximum <= 0 ? 0 : current * 100 / maximum;
        }

        private static int CountActive(IReadOnlyList<BattleUnionState> unions)
        {
            var count = 0;
            for (var index = 0; index < (unions?.Count ?? 0); index++)
                if (IsActive(unions[index])) count++;
            return count;
        }

        private static bool IsActive(BattleUnionState union) =>
            union != null && !union.Retreated && !union.IsDefeated;

        private static bool IndividualMpAffordable(
            BattleUnionState union,
            IReadOnlyList<BattlePlannedActionState> actions)
        {
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                var memberIndex = union.FindMemberIndex(action.ActorMemberId);
                if (memberIndex < 0 || action.PersonalMpCost > union.Members[memberIndex].CurrentMp)
                    return false;
            }
            return true;
        }

        private static int SumSharedAp(IReadOnlyList<BattlePlannedActionState> actions)
        {
            var sum = 0;
            for (var index = 0; index < actions.Count; index++)
                sum = checked(sum + actions[index].SharedApCost);
            return sum;
        }

        private static int SumPersonalMp(IReadOnlyList<BattlePlannedActionState> actions)
        {
            var sum = 0;
            for (var index = 0; index < actions.Count; index++)
                sum = checked(sum + actions[index].PersonalMpCost);
            return sum;
        }

        private static string BuildReceipt(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState union,
            int forecastSlot,
            string baseGenerationIdentity,
            string targetUnionId,
            SpecialRelicUltimateArtRule001 rule,
            string sealItemInstanceId,
            string invokerMemberId) =>
            "ULTREC001_" + CanonicalJson.Sha256Hex(new
            {
                AuthorityVersion,
                campaign.CampaignGuid,
                battle.BattleId,
                battle.Round,
                union.UnionId,
                ForecastSlot = forecastSlot,
                BaseGenerationIdentity = baseGenerationIdentity,
                TargetUnionId = targetUnionId,
                rule.RelicId,
                rule.UnderlyingArtId,
                SealItemInstanceId = sealItemInstanceId,
                InvokerMemberId = invokerMemberId,
                Control = CompleteForecastControl
            }).Substring(0, 24).ToUpperInvariant();

        private static string BuildAugmentedIdentity(
            string baseGenerationIdentity,
            string relicId,
            string receiptId) =>
            CanonicalJson.Sha256Hex(new
            {
                AuthorityVersion,
                BaseGenerationIdentity = baseGenerationIdentity,
                RelicId = relicId,
                ReceiptId = receiptId,
                Control = CompleteForecastControl
            });

        private static string ExpectedBaseIdentity(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState union,
            int forecastSlot) =>
            CanonicalJson.Sha256Hex(new
            {
                CampaignSeed = campaign.CampaignSeed,
                battle.BattleId,
                battle.Round,
                union.UnionId,
                ForecastSlot = forecastSlot,
                CurrentAuthoritativeStateHash = battle.ForecastStateBasisHash,
                battle.ContentVersion
            });

        private static string AppendMarker(string source, string marker) =>
            string.IsNullOrWhiteSpace(source) ? marker : source + " · " + marker;

        private static bool TryReadMarker(
            string text,
            out string relicId,
            out string receipt,
            out string baseIdentity,
            out int forecastSlot,
            out string sealItemInstanceId)
        {
            var artValid = TryReadUniqueMarker(text, ArtMarker, out relicId);
            var receiptValid = TryReadUniqueMarker(text, ReceiptMarker, out receipt);
            var baseValid = TryReadUniqueMarker(text, BaseIdentityMarker, out baseIdentity);
            var slotValid = TryReadUniqueMarker(text, SlotMarker, out var slot);
            var sealValid = TryReadUniqueMarker(text, SealInstanceMarker, out sealItemInstanceId);
            forecastSlot = -1;
            var parsedSlot = int.TryParse(slot, out var value);
            if (parsedSlot) forecastSlot = value;
            return artValid && receiptValid && baseValid && slotValid && sealValid &&
                   !string.IsNullOrWhiteSpace(relicId) &&
                   !string.IsNullOrWhiteSpace(receipt) &&
                   !string.IsNullOrWhiteSpace(baseIdentity) &&
                   !string.IsNullOrWhiteSpace(sealItemInstanceId) &&
                   parsedSlot && forecastSlot >= 0;
        }

        private static bool TryReadUniqueMarker(string text, string marker, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(marker))
                return false;
            var start = text.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0 ||
                text.IndexOf(marker, start + marker.Length, StringComparison.Ordinal) >= 0)
                return false;
            start += marker.Length;
            var end = text.IndexOfAny(new[] { ' ', '·', '|', ';' }, start);
            value = (end < 0 ? text.Substring(start) : text.Substring(start, end - start)).Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool ContainsMarker(string text, string marker) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.IndexOf(marker, StringComparison.Ordinal) >= 0;

        private static bool HasAppliedReceipt(
            IReadOnlyList<BattleEventState> events,
            string receipt)
        {
            for (var index = 0; index < (events?.Count ?? 0); index++)
            {
                var battleEvent = events[index];
                if (StringComparer.Ordinal.Equals(battleEvent.EventType, EventType) &&
                    ContainsMarker(battleEvent.Text, ReceiptMarker + receipt))
                    return true;
            }
            return false;
        }

        private static bool HasAppliedUnionRound(
            IReadOnlyList<BattleEventState> events,
            int round,
            string unionId)
        {
            for (var index = 0; index < (events?.Count ?? 0); index++)
            {
                var battleEvent = events[index];
                if (battleEvent.Round == round &&
                    StringComparer.Ordinal.Equals(battleEvent.EventType, EventType) &&
                    StringComparer.Ordinal.Equals(battleEvent.UnionId, unionId))
                    return true;
            }
            return false;
        }

        private static bool TryValidateApplicationProof(
            CampaignState campaign,
            SpecialRelicUltimateArtApplicationProof001 proof,
            out SpecialRelicUltimateArtRule001 rule,
            out string error)
        {
            rule = null;
            error = string.Empty;
            if (campaign?.Battle == null || proof == null ||
                !TryGetRule(proof.RelicId, out rule) ||
                !StringComparer.Ordinal.Equals(proof.UnderlyingArtId, rule.UnderlyingArtId) ||
                !StringComparer.Ordinal.Equals(proof.EquipmentTrackId, rule.EquipmentTrackId) ||
                string.IsNullOrWhiteSpace(proof.SealItemInstanceId) ||
                string.IsNullOrWhiteSpace(proof.EventStateHash) ||
                proof.Round < 1 ||
                !proof.ReceiptId.StartsWith("ULTREC001_", StringComparison.Ordinal) ||
                proof.ReceiptId.Length != "ULTREC001_".Length + 24 ||
                !proof.ForecastId.StartsWith("FORECAST_", StringComparison.Ordinal) ||
                !StringComparer.Ordinal.Equals(campaign.Battle.BattleId, proof.BattleId))
            {
                error = "SPECIAL_RELIC001_APPLICATION_PROOF_INVALID";
                return false;
            }

            var union = FindUnion(campaign.Battle.PlayerUnions, proof.UnionId);
            if (union == null || !TryResolveEquippedP0Seals(
                    campaign, union, out var equippedSeals, out error))
            {
                if (string.IsNullOrWhiteSpace(error))
                    error = "SPECIAL_RELIC001_APPLICATION_SEAL_AUTHORITY_CHANGED";
                return false;
            }
            var exactEquippedSeal = 0;
            for (var sealIndex = 0; sealIndex < equippedSeals.Count; sealIndex++)
                if (StringComparer.Ordinal.Equals(
                        equippedSeals[sealIndex].Rule.RelicId, rule.RelicId) &&
                    StringComparer.Ordinal.Equals(
                        equippedSeals[sealIndex].Item.InstanceId, proof.SealItemInstanceId))
                    exactEquippedSeal++;
            if (exactEquippedSeal != 1)
            {
                error = "SPECIAL_RELIC001_APPLICATION_SEAL_AUTHORITY_CHANGED";
                return false;
            }

            var evidenceCount = 0;
            for (var eventIndex = 0; eventIndex < campaign.Battle.EventLog.Count; eventIndex++)
            {
                var value = campaign.Battle.EventLog[eventIndex];
                if (!StringComparer.Ordinal.Equals(value.EventType, EventType) ||
                    value.Round != proof.Round ||
                    !StringComparer.Ordinal.Equals(value.UnionId, proof.UnionId) ||
                    !StringComparer.Ordinal.Equals(value.MemberId, proof.InvokerMemberId) ||
                    !StringComparer.Ordinal.Equals(value.ArtId, rule.UnderlyingArtId) ||
                    !StringComparer.Ordinal.Equals(value.StateHash, proof.EventStateHash) ||
                    !TryReadUniqueMarker(value.Text, ReceiptMarker, out var eventReceipt) ||
                    !StringComparer.Ordinal.Equals(eventReceipt, proof.ReceiptId))
                    continue;
                evidenceCount++;
            }
            if (evidenceCount != 1)
            {
                error = "SPECIAL_RELIC001_APPLICATION_EVENT_EVIDENCE_REQUIRED";
                return false;
            }
            return true;
        }

        private static bool TryCollectOwnedP0Seals(
            GuildState guild,
            out IReadOnlyList<SealOwnershipLocation001> seals,
            out string error)
        {
            var all = new List<SealOwnershipLocation001>();
            var validSeals = new List<SealOwnershipLocation001>();
            seals = Array.Empty<SealOwnershipLocation001>();
            error = string.Empty;
            if (guild == null)
            {
                error = "SPECIAL_RELIC001_GUILD_REQUIRED";
                return false;
            }

            for (var inventoryIndex = 0; inventoryIndex < guild.Inventory.Count; inventoryIndex++)
            {
                var item = guild.Inventory[inventoryIndex];
                if (item != null)
                    all.Add(new SealOwnershipLocation001(item, string.Empty, string.Empty));
            }
            for (var recruitIndex = 0; recruitIndex < guild.Recruits.Count; recruitIndex++)
            {
                var recruit = guild.Recruits[recruitIndex];
                var assignments = recruit?.Equipment?.Assignments;
                if (assignments == null) continue;
                for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
                {
                    var assignment = assignments[assignmentIndex];
                    if (assignment?.Item != null)
                        all.Add(new SealOwnershipLocation001(
                            assignment.Item, recruit.RecruitId, assignment.SlotId));
                }
            }

            var instanceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var definitionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < all.Count; index++)
            {
                var location = all[index];
                var item = location.Item;
                instanceCounts[item.InstanceId] = instanceCounts.TryGetValue(
                    item.InstanceId, out var instanceCount) ? instanceCount + 1 : 1;
                if (!LooksLikeP0Seal(item)) continue;
                if (!TryValidateP0SealItem(item, out _, out error))
                {
                    if (item.DefinitionId.StartsWith("RELIC001_ART_", StringComparison.Ordinal) &&
                        !RulesByRelicId.ContainsKey(item.DefinitionId))
                        error = "SPECIAL_RELIC001_NON_P0_ULTIMATE_SEALED:" + item.DefinitionId;
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(location.EquippedRecruitId) &&
                    !StringComparer.Ordinal.Equals(location.SlotId, EquipmentSlotIds.ToolRelic))
                {
                    error = "SPECIAL_RELIC001_SEAL_WRONG_SLOT";
                    return false;
                }
                definitionCounts[item.DefinitionId] = definitionCounts.TryGetValue(
                    item.DefinitionId, out var definitionCount) ? definitionCount + 1 : 1;
                validSeals.Add(location);
            }

            for (var index = 0; index < validSeals.Count; index++)
            {
                var item = validSeals[index].Item;
                if (!instanceCounts.TryGetValue(item.InstanceId, out var instanceCount) ||
                    instanceCount != 1)
                {
                    error = "SPECIAL_RELIC001_SEAL_INSTANCE_AMBIGUOUS:" + item.InstanceId;
                    return false;
                }
                if (!definitionCounts.TryGetValue(item.DefinitionId, out var definitionCount) ||
                    definitionCount != 1)
                {
                    error = "SPECIAL_RELIC001_SEAL_DEFINITION_AMBIGUOUS:" + item.DefinitionId;
                    return false;
                }
            }
            validSeals.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.Item.InstanceId, right.Item.InstanceId));
            seals = validSeals.AsReadOnly();
            return true;
        }

        private static bool LooksLikeP0Seal(EquipmentItemState item) =>
            item != null &&
            (RulesByRelicId.ContainsKey(item.DefinitionId) ||
             ContainsOrdinal(item.EquipmentTags, SealEquipmentTag) ||
             ContainsOrdinal(item.EquipmentTags, P0ActiveEquipmentTag));

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            for (var index = 0; index < (values?.Count ?? 0); index++)
                if (StringComparer.Ordinal.Equals(values[index], expected)) return true;
            return false;
        }

        private static string AppendReceiptHistory(string history, string receipt)
        {
            if (HasExactReceiptHistory(history, receipt)) return history;
            return string.IsNullOrWhiteSpace(history)
                ? receipt
                : history + ReceiptHistorySeparator + receipt;
        }

        private static bool HasExactReceiptHistory(string history, string receipt)
        {
            if (string.IsNullOrWhiteSpace(history) || string.IsNullOrWhiteSpace(receipt))
                return false;
            var values = history.Split(
                new[] { ReceiptHistorySeparator },
                StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < values.Length; index++)
                if (StringComparer.Ordinal.Equals(values[index].Trim(), receipt)) return true;
            return false;
        }

        private static int CountUltimateReceiptHistory(string history)
        {
            if (string.IsNullOrWhiteSpace(history)) return 0;
            var count = 0;
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var values = history.Split(
                new[] { ReceiptHistorySeparator },
                StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index].Trim();
                if (IsCanonicalUltimateReceipt(value) && unique.Add(value))
                    count++;
            }
            return count;
        }

        private static bool TryValidateUltimateReceiptHistory(
            string history,
            IReadOnlyList<string> appliedReceiptIds,
            out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(history)) return true;
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var values = history.Split(
                new[] { ReceiptHistorySeparator },
                StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index].Trim();
                if (!IsCanonicalUltimateReceipt(value)) continue;
                if (!unique.Add(value) ||
                    (appliedReceiptIds?.Count(candidate =>
                        StringComparer.Ordinal.Equals(candidate, value)) ?? 0) != 1)
                {
                    error = "SPECIAL_RELIC001_ORPHANED_EVOLUTION_RECEIPT:" + value;
                    return false;
                }
            }
            return true;
        }

        private static bool IsCanonicalUltimateReceipt(string value)
        {
            const string prefix = "ULTREC001_";
            if (string.IsNullOrWhiteSpace(value) ||
                !value.StartsWith(prefix, StringComparison.Ordinal) ||
                value.Length != prefix.Length + 24)
                return false;
            for (var index = prefix.Length; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'A' && character <= 'F')))
                    return false;
            }
            return true;
        }

        private static bool TryResolveProgressionCheckpoint(
            string ultimateCheckpoint,
            out string checkpoint,
            out string error,
            params string[] checkpoints)
        {
            checkpoint = string.Empty;
            error = string.Empty;
            var pending = string.Empty;
            for (var index = 0; index < (checkpoints?.Length ?? 0); index++)
            {
                var value = checkpoints[index] ?? string.Empty;
                if (value.StartsWith(EchoPendingCheckpointPrefix, StringComparison.Ordinal) ||
                    value.StartsWith(
                        CovenantBattlePendingCheckpointPrefix,
                        StringComparison.Ordinal))
                {
                    if (!IsCanonicalInvocationPendingCheckpoint(value))
                    {
                        error = "SPECIAL_RELIC001_INVOCATION_PENDING_CHECKPOINT_INVALID";
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(pending) &&
                        !StringComparer.Ordinal.Equals(pending, value))
                    {
                        error = "SPECIAL_RELIC001_INVOCATION_PENDING_CHECKPOINT_CONFLICT";
                        return false;
                    }
                    pending = value;
                }
                else if (value.IndexOf("pending:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    error = "SPECIAL_RELIC001_UNKNOWN_PENDING_CHECKPOINT";
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(pending))
            {
                // The Campaign022 progression checkpoint is transactional authority.
                // Other nested systems may advance their ordinary (non-pending)
                // checkpoint labels during the same resolved round. Preserve the one
                // canonical pending receipt while still rejecting malformed, unknown,
                // or distinct pending receipts above.
                checkpoint = pending;
                return true;
            }
            checkpoint = ultimateCheckpoint;
            return true;
        }

        private static bool IsCanonicalInvocationPendingCheckpoint(string checkpoint)
        {
            if (string.IsNullOrWhiteSpace(checkpoint)) return false;
            string receiptPrefix;
            int receiptStart;
            if (checkpoint.StartsWith(EchoPendingCheckpointPrefix, StringComparison.Ordinal))
            {
                receiptPrefix = "ECHOREC022_";
                receiptStart = EchoPendingCheckpointPrefix.Length;
            }
            else if (checkpoint.StartsWith(
                         CovenantBattlePendingCheckpointPrefix,
                         StringComparison.Ordinal))
            {
                receiptPrefix = "COVBATTLE022_";
                receiptStart = CovenantBattlePendingCheckpointPrefix.Length;
            }
            else return false;
            if (checkpoint.Length != receiptStart + receiptPrefix.Length + 24 ||
                !StringComparer.Ordinal.Equals(
                    checkpoint.Substring(receiptStart, receiptPrefix.Length),
                    receiptPrefix))
                return false;
            for (var index = receiptStart + receiptPrefix.Length;
                 index < checkpoint.Length;
                 index++)
            {
                var value = checkpoint[index];
                if (!((value >= '0' && value <= '9') || (value >= 'A' && value <= 'F')))
                    return false;
            }
            return true;
        }

        private sealed class SealOwnershipLocation001
        {
            public SealOwnershipLocation001(
                EquipmentItemState item,
                string equippedRecruitId,
                string slotId)
            {
                Item = item;
                EquippedRecruitId = equippedRecruitId ?? string.Empty;
                SlotId = slotId ?? string.Empty;
            }

            public EquipmentItemState Item { get; }
            public string EquippedRecruitId { get; }
            public string SlotId { get; }
        }
    }
}
