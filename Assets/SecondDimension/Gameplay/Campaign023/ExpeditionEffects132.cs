using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign023
{
    [Serializable]
    public sealed class ExpeditionEffectReceipt132
    {
        [JsonConstructor]
        public ExpeditionEffectReceipt132(string kind, int d20, int wheelSector,
            string targetRecruitId, IReadOnlyList<string> eligibleRecruitIds,
            string routeReceiptHash132 = null, string worldGateReceiptHash132 = null, string luckChargeId165 = null)
        {
            LuckChargeId165 = string.IsNullOrEmpty(luckChargeId165)?null:luckChargeId165;
            RouteReceiptHash132 = routeReceiptHash132;
            WorldGateReceiptHash132 = worldGateReceiptHash132;
            Kind = kind ?? string.Empty;
            D20 = d20;
            WheelSector = wheelSector;
            TargetRecruitId = targetRecruitId ?? string.Empty;
            EligibleRecruitIds = Array.AsReadOnly((eligibleRecruitIds ?? Array.Empty<string>()).ToArray());
        }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RouteReceiptHash132 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string WorldGateReceiptHash132 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LuckChargeId165 { get; }
        public string Kind { get; }
        public int D20 { get; }
        public int WheelSector { get; }
        public string TargetRecruitId { get; }
        public IReadOnlyList<string> EligibleRecruitIds { get; }
        [JsonIgnore]
        public bool IsRolled132 => Kind == ExpeditionDeckService089.Wheel132 ? WheelSector >= 0 : D20 > 0;
    }

    public sealed partial class ExpeditionDeckService089
    {
        public const string BoonD20132 = "BOON_D20_132";
        public const string CurseD20132 = "CURSE_D20_132";
        public const string Wheel132 = "FORTUNE_WHEEL_132";

        public static string EffectKind132(ExpeditionRouteCardState089 card)
        {
            if (card == null || card.AdvancesRoute) return string.Empty;
            foreach (var kind in new[] { BoonD20132, CurseD20132, Wheel132 })
                if (card.SourceTag.EndsWith("|" + kind, StringComparison.Ordinal)) return kind;
            return string.Empty;
        }

        // Classifies uncommitted choices for creation and read-only mystery copy.
        // Existing pending legacy receipts retain their original interpretation.
        public static bool CanOfferEffect132(ExpeditionRouteCardState089 card) =>
            CanOfferRouteFate132(card) || card != null && !card.AdvancesRoute && card.TreasuryXpCost == 0 &&
            (EffectKind132(card).Length > 0 || card.Category == "BUFF" || card.Category == "HAZARD" ||
                card.Category == "CHANCE" || card.Category == "PERMANENT");

        static ExpeditionRouteCardState089 DecorateEffectCard132(ExpeditionRouteCardState089 card, bool preserveIdentity = false)
        {
            var kind = card.AdvancesRoute ? string.Empty :
                card.Category == "BUFF" || card.Category == "STORY" ? BoonD20132 :
                card.Category == "HAZARD" ? CurseD20132 :
                card.Category == "CHANCE" ? Wheel132 :
                card.Category == "PERMANENT" ?
                    card.PermanentHeroEffectKind == "SCAR" ? CurseD20132 : BoonD20132 : string.Empty;
            if (kind.Length == 0 && card.Category != "PERMANENT") return card;
            // New permanent-fate route cards become ordinary temporary boons.
            // Natural20 on an encounter D20 is the sole new permanent grant.
            var category = kind == CurseD20132 ? "HAZARD" : kind == Wheel132 ? "CHANCE" : "BUFF";
            var xp = kind == Wheel132 ? card.GuildXp : CardXp(category);
            var title = kind == Wheel132 ? "Fortune Wheel" : kind == CurseD20132 ? "Rift Hex" : "Wayglass Blessing";
            var description = kind == Wheel132 ? "A sealed Fortune Wheel waits. Spin to discover your reward."
                : kind == CurseD20132 ? "A rift curse gathers. Roll the D20 to discover its temporary effect."
                : kind == BoonD20132 ? "A mysterious blessing awaits. Roll the D20 and discover your fate."
                : "A passing blessing strengthens this quest.";
            var reward = kind.Length == 0 ? "A temporary quest blessing" : "MYSTERY • REVEALED AFTER THE ROLL";
            var id = preserveIdentity ? card.CardId : "EXPCARD132_" + CanonicalJson.Sha256Hex(new { card.CardId, kind, category }).Substring(0,24).ToUpperInvariant();
            return new ExpeditionRouteCardState089(id, card.NodeId, card.ChoiceId,
                category, kind == CurseD20132 ? "HAZARD" : kind == Wheel132 ? "CHANCE" : "BUFF",
                title, description, kind == CurseD20132 ? "TEMPORARY HEX" : "SAVED FATE",
                description, reward, kind.Length == 0 ? card.ResolutionDifficulty : 0,
                card.ItemCheckModifier, xp, xp, kind == Wheel132 ? card.MaterialIds : Array.Empty<string>(),
                kind.Length == 0 ? 1 : 0, null, null, null,
                card.SourceTag + (kind.Length == 0 ? "" : "|" + kind),
                advancesRoute: card.AdvancesRoute);
        }

        static int ExistingHeroBoon132(RecruitState hero) =>
            (hero.Progression?.UnlockedTreeIds ?? Array.Empty<string>()).Count(id =>
                id.StartsWith(PermanentBoonPrefix089, StringComparison.Ordinal)) -
            (hero.Progression?.UnlockedTreeIds ?? Array.Empty<string>()).Count(id =>
                id.StartsWith(PermanentScarPrefix089, StringComparison.Ordinal));

        Result<ExpeditionDeckState089> CommitEffect132(ExpeditionDeckState089 deck,
            ExpeditionRouteCardState089 card, CampaignState campaign)
        {
            if (campaign?.Guild == null)
                return Result<ExpeditionDeckState089>.Failure("EXPEDITION132_OWNED_GUILD_REQUIRED");
            if (card.TreasuryXpCost != 0 || card.AdvancesRoute || IsOptionalBattleCard089(card))
                return Result<ExpeditionDeckState089>.Failure("EXPEDITION132_EFFECT_CARD_INVALID");
            // Selecting a card commits only the sealed encounter. No die,
            // recipient or wheel result exists until the explicit Roll command.
            var kind=EffectKind132(card);
            var charge=kind==BoonD20132?TownLuckConsumables165.ActiveChargeId(campaign):null;
            var effect = new ExpeditionEffectReceipt132(kind,0,-1,"",Array.Empty<string>(),luckChargeId165:charge);
            var receipt = EffectReceipt132(deck, card, effect);
            return Result<ExpeditionDeckState089>.Success(deck.With(pendingReceipt: receipt, replacePendingReceipt: true));
        }

        public Result<ExpeditionDeckState089> RollEffect132(ExpeditionDeckState089 deck,
            CampaignState campaign, string expectedReceiptId)
        {
            var pending = deck?.PendingReceipt;
            if (campaign?.Guild == null || pending?.Effect132 == null || pending.Effect132.IsRolled132 ||
                !StringComparer.Ordinal.Equals(expectedReceiptId,pending.ReceiptId) ||
                !ValidateCommittedReceipt(deck,pending) || deck.AppliedReceiptIds.Contains(pending.ReceiptId))
                return Result<ExpeditionDeckState089>.Failure("EXPEDITION132_ROLL_NOT_AVAILABLE");
            if(pending.Effect132.LuckChargeId165!=null&&!TownLuckConsumables165.WasConsumedFor(campaign,pending.Effect132.LuckChargeId165,pending.ReceiptId))
                return Result<ExpeditionDeckState089>.Failure("EXPEDITION165_LUCK_SEAL_RECEIPT_REQUIRED");
            var card = deck.CurrentRow.First(value=>value.CardId==pending.CardId);
            var eligible = pending.Effect132.Kind == BoonD20132
                ? campaign.Guild.Recruits.Where(hero=>hero!=null && ExistingHeroBoon132(hero)<2)
                    .Select(hero=>hero.RecruitId).Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            var effect = ResolveEffect132(deck,card,eligible,
                pending.Effect132.RouteReceiptHash132,pending.Effect132.WorldGateReceiptHash132,pending.Effect132.LuckChargeId165);
            return Result<ExpeditionDeckState089>.Success(deck.With(pendingReceipt:EffectReceipt132(deck,card,effect),replacePendingReceipt:true));
        }

        static ExpeditionEffectReceipt132 ResolveEffect132(ExpeditionDeckState089 deck,
            ExpeditionRouteCardState089 card, IReadOnlyList<string> eligible,
            string routeReceiptHash132 = null, string worldGateReceiptHash132 = null, string luckChargeId165 = null)
        {
            var kind = card.AdvancesRoute ? RouteFateKind132(card) : EffectKind132(card);
            var rng = Pcg32.FromParts("EXPEDITION_EFFECT_132", deck.ShuffleSeedIdentity, deck.OperationId, deck.DeckId, card.CardId);
            if (kind == Wheel132) return new ExpeditionEffectReceipt132(kind, 0, rng.NextInclusive(0,2), "", Array.Empty<string>());
            var die = rng.NextInclusive(1,20);
            if(kind==BoonD20132 && !string.IsNullOrEmpty(luckChargeId165))die=Math.Max(die,rng.NextInclusive(1,20));
            var target = kind == BoonD20132 && die == 20 && eligible.Count > 0
                ? eligible[rng.NextInclusive(0,eligible.Count-1)] : string.Empty;
            return new ExpeditionEffectReceipt132(kind, die, -1, target, eligible,routeReceiptHash132,worldGateReceiptHash132,luckChargeId165);
        }

        static ExpeditionCardReceipt089 EffectReceipt132(ExpeditionDeckState089 deck,
            ExpeditionRouteCardState089 card, ExpeditionEffectReceipt132 effect)
        {
            if (IsRouteFate132(effect))
                return WrapRouteFateReceipt132(BaseRouteReceipt132(deck.PendingReceipt),effect);
            var wheel = effect.Kind == Wheel132;
            if (!effect.IsRolled132)
            {
                var sealedReceipt = new ExpeditionCardReceipt089(ReceiptId(deck.OperationId,card.CardId),
                    deck.OperationId,card.CardId,card.NodeId,card.ChoiceId,"","",0,0,0,0,0,
                    "AWAITING_ROLL_132",0,0,Array.Empty<string>(),0,"","PENDING132",false,
                    ReceiptAuthoritySchemaVersion089,effect132:effect);
                return CopyEffectReceipt132(sealedReceipt,effect,ReceiptHash(sealedReceipt));
            }
            var momentum = effect.Kind == BoonD20132 ? effect.D20 == 20 ? effect.TargetRecruitId.Length > 0 ? 0 : 2 : effect.D20 >= 10 ? 2 : 1 :
                effect.Kind == CurseD20132 ? effect.D20 <= 9 ? -2 : -1 : 0;
            var xp = !wheel || effect.WheelSector == 0 ? card.GuildXp : 0;
            var hall = !wheel || effect.WheelSector == 0 ? card.HallXp : 0;
            var materials = wheel && effect.WheelSector == 1 ? card.MaterialIds : Array.Empty<string>();
            var receipt = new ExpeditionCardReceipt089(ReceiptId(deck.OperationId, card.CardId),
                deck.OperationId, card.CardId, card.NodeId, card.ChoiceId, "", "", 0,0,0,0,0,
                wheel ? "WHEEL_" + effect.WheelSector : effect.Kind == BoonD20132 && effect.D20 == 20 ? "NATURAL20_BOON" : "D20_RESOLVED",
                xp, hall, materials, momentum, "", "PENDING132", false,
                ReceiptAuthoritySchemaVersion089, effect132: effect);
            return CopyEffectReceipt132(receipt, effect, ReceiptHash(receipt));
        }

        static ExpeditionCardReceipt089 CopyEffectReceipt132(ExpeditionCardReceipt089 r,
            ExpeditionEffectReceipt132 effect, string hash) => new ExpeditionCardReceipt089(
                r.ReceiptId,r.OperationId,r.CardId,r.NodeId,r.ChoiceId,r.ActorRecruitId,r.AssistantRecruitId,
                r.DieOne,r.DieTwo,r.BaseCheckModifier,r.EffectiveModifier,r.Difficulty,r.Outcome,r.GuildXp,r.HallXp,
                r.MaterialIds,r.MomentumDelta,r.RecruitStableId,hash,r.DelegatedToWorldGateCheck,
                r.AuthoritySchemaVersion,r.TreasuryXpCost,r.RequiresCertifiedBattle,r.BattleRequestId,
                r.BattleId,r.BattlePreStateHash,r.BattleReturnCheckpointId,r.BattleReturnReceiptId,effect);

        static bool ValidateEffect132(ExpeditionDeckState089 deck, ExpeditionRouteCardState089 card,
            ExpeditionCardReceipt089 receipt)
        {
            if (card.AdvancesRoute) return ValidateRouteFateReceipt132(deck,card,receipt);
            var kind = EffectKind132(card);
            if (kind.Length == 0) return receipt.Effect132 == null;
            var effect = receipt.Effect132;
            if (effect == null || effect.Kind != kind || card.AdvancesRoute) return false;
            var eligible = effect.EligibleRecruitIds;
            if (eligible.Any(string.IsNullOrWhiteSpace) || !eligible.SequenceEqual(eligible.Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal))) return false;
            if (kind != BoonD20132 && (eligible.Count != 0 || effect.LuckChargeId165!=null)) return false;
            var expected = effect.IsRolled132 ? ResolveEffect132(deck, card, eligible,luckChargeId165:effect.LuckChargeId165)
                : new ExpeditionEffectReceipt132(kind,0,-1,"",Array.Empty<string>(),luckChargeId165:effect.LuckChargeId165);
            if (CanonicalJson.Serialize(expected) != CanonicalJson.Serialize(effect)) return false;
            return CanonicalJson.Serialize(EffectReceipt132(deck, card, expected)) == CanonicalJson.Serialize(receipt);
        }

        public static string PermanentEffectId132(ExpeditionCardReceipt089 receipt) =>
            PermanentBoonPrefix089 + "NATURAL20_132_" + receipt.ReceiptId;

        public static void ApplyEffect132(ExpeditionCardReceipt089 receipt,
            List<RecruitState> recruits, List<EquipmentItemState> inventory)
        {
            var effect = receipt?.Effect132;
            if (effect == null) return;
            if (effect.Kind == Wheel132 && effect.WheelSector == 2)
            {
                var item = ChestEquipmentReward089(receipt.CardId, receipt.ReceiptId);
                if (!inventory.Any(value => value.InstanceId == item.InstanceId)) inventory.Add(item);
            }
            if (effect.Kind != BoonD20132 || effect.D20 != 20 || effect.TargetRecruitId.Length == 0) return;
            var index = recruits.FindIndex(hero => hero.RecruitId == effect.TargetRecruitId);
            if (index < 0 || ExistingHeroBoon132(recruits[index]) >= 2)
                throw new InvalidOperationException("EXPEDITION132_PERMANENT_BOON_TARGET_NO_LONGER_ELIGIBLE");
            var hero = recruits[index];
            var progression = hero.Progression ?? RecruitProgressionState.Default();
            var traits = progression.UnlockedTreeIds.Concat(new[] { PermanentEffectId132(receipt) })
                .Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            recruits[index] = hero.WithProgression(progression.WithUnlockedTrees(traits));
        }
    }
}
