using System;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.GuildCity017D;

namespace SecondDimension.Gameplay.Campaign023
{
    public sealed partial class ExpeditionDeckService089
    {
        // CAMP remains the existing rest/preparation card. Its incidental
        // momentum is not relabelled as a boon or changed into a new event.
        public static bool CanOfferRouteFate132(ExpeditionRouteCardState089 card) =>
            card != null && card.AdvancesRoute &&
            (card.Category == "BUFF" || card.Category == "PERMANENT" || card.Category == "HAZARD");

        static string RouteFateKind132(ExpeditionRouteCardState089 card) =>
            !CanOfferRouteFate132(card) ? string.Empty :
            card.Category == "HAZARD" || card.PermanentHeroEffectKind == "SCAR" ? CurseD20132 : BoonD20132;

        public static bool IsRouteFate132(ExpeditionEffectReceipt132 effect) =>
            !string.IsNullOrWhiteSpace(effect?.RouteReceiptHash132);

        // Retain the original flat card receipt in full. Completion proofs and
        // TryAuthorizedWorldGateModifier093 still see the exact authored 2D6,
        // actor, modifier, outcome and rewards they were originally given.
        public static ExpeditionCardReceipt089 BaseRouteReceipt132(ExpeditionCardReceipt089 receipt)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            return IsRouteFate132(receipt.Effect132)
                ? CopyEffectReceipt132(receipt,null,receipt.Effect132.RouteReceiptHash132) : receipt;
        }

        static ExpeditionCardReceipt089 WrapRouteFateReceipt132(ExpeditionCardReceipt089 basis,
            ExpeditionEffectReceipt132 effect)
        {
            var draft=CopyEffectReceipt132(basis,effect,basis.AuthoritativeHash);
            return CopyEffectReceipt132(draft,effect,ReceiptHash(draft));
        }

        public ExpeditionDeckState089 SealRouteFate132(ExpeditionDeckState089 deck,
            WorldGateNodeReceipt023 worldGateReceipt,CampaignState campaign165=null)
        {
            var receipt=deck?.PendingReceipt;
            if (receipt == null || receipt.Effect132 != null) return deck;
            var card=deck.CurrentRow.FirstOrDefault(value=>value.CardId==receipt.CardId);
            if (!CanOfferRouteFate132(card)) return deck;
            if (!ValidateCommittedReceipt(deck,receipt) || worldGateReceipt == null ||
                receipt.OperationId != worldGateReceipt.OperationId || receipt.NodeId != worldGateReceipt.NodeId ||
                receipt.ChoiceId != worldGateReceipt.ChoiceId)
                throw new InvalidOperationException("EXPEDITION132_ROUTE_FATE_PAIR_REQUIRED");
            var effect=new ExpeditionEffectReceipt132(RouteFateKind132(card),0,-1,"",Array.Empty<string>(),
                receipt.AuthoritativeHash,CanonicalJson.Sha256Hex(worldGateReceipt),
                RouteFateKind132(card)==BoonD20132?TownLuckConsumables165.ActiveChargeId(campaign165):null);
            return deck.With(pendingReceipt:WrapRouteFateReceipt132(receipt,effect),replacePendingReceipt:true);
        }

        static bool ValidateRouteFateReceipt132(ExpeditionDeckState089 deck,
            ExpeditionRouteCardState089 card,ExpeditionCardReceipt089 receipt)
        {
            var effect=receipt.Effect132;
            if(effect==null)return true; // Already committed pre-fate route.
            if(!CanOfferRouteFate132(card) || !IsRouteFate132(effect) ||
                string.IsNullOrWhiteSpace(effect.WorldGateReceiptHash132) || effect.Kind!=RouteFateKind132(card))return false;
            var basis=BaseRouteReceipt132(receipt);
            if(basis.AuthoritativeHash!=ReceiptHash(basis))return false;
            var eligible=effect.EligibleRecruitIds;
            if(eligible.Any(string.IsNullOrWhiteSpace) ||
                !eligible.SequenceEqual(eligible.Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal)) ||
                effect.Kind!=BoonD20132 && (eligible.Count!=0||effect.LuckChargeId165!=null))return false;
            var expected=effect.IsRolled132
                ?ResolveEffect132(deck,card,eligible,effect.RouteReceiptHash132,effect.WorldGateReceiptHash132,effect.LuckChargeId165)
                :new ExpeditionEffectReceipt132(effect.Kind,0,-1,"",Array.Empty<string>(),
                    effect.RouteReceiptHash132,effect.WorldGateReceiptHash132,effect.LuckChargeId165);
            return CanonicalJson.Serialize(expected)==CanonicalJson.Serialize(effect) &&
                CanonicalJson.Serialize(WrapRouteFateReceipt132(basis,expected))==CanonicalJson.Serialize(receipt);
        }

        public bool ValidateRouteFatePair132(WorldGateOperationState023 operation)
        {
            var deck=operation?.ExpeditionDeck089;var receipt=deck?.PendingReceipt;
            var effect=receipt?.Effect132;var route=operation?.PendingReceipt;
            if(!IsRouteFate132(effect) || route==null || !ValidateCommittedReceipt(deck,receipt) ||
                effect.WorldGateReceiptHash132!=CanonicalJson.Sha256Hex(route) ||
                receipt.OperationId!=route.OperationId || receipt.NodeId!=route.NodeId || receipt.ChoiceId!=route.ChoiceId ||
                receipt.NodeId!=operation.CurrentNodeId)return false;
            if(!receipt.DelegatedToWorldGateCheck)return true;
            var original=BaseRouteReceipt132(receipt);
            var finalized=FinalizeAgainstWorldGate(deck.With(pendingReceipt:original,replacePendingReceipt:true),route);
            return CanonicalJson.Serialize(original)==CanonicalJson.Serialize(finalized);
        }

        public static bool ValidateAppliedRouteFatePair132(ExpeditionCardReceipt089 receipt,
            WorldGateNodeReceipt023 route)
        {
            var effect=receipt?.Effect132;
            if(!IsRouteFate132(effect))return true;
            return effect.IsRolled132 && route!=null &&
                effect.WorldGateReceiptHash132==CanonicalJson.Sha256Hex(route) &&
                effect.RouteReceiptHash132==ReceiptHash(BaseRouteReceipt132(receipt));
        }

        public static int AppliedMomentum132(ExpeditionCardReceipt089 receipt)
        {
            var effect=receipt?.Effect132;
            if(!IsRouteFate132(effect))return receipt?.MomentumDelta??0;
            if(!effect.IsRolled132)return 0;
            // Replaces the former automatic +1; never stacks on top of it.
            return effect.Kind==CurseD20132 ? effect.D20<10?-2:-1 :
                effect.D20==20 && !string.IsNullOrWhiteSpace(effect.TargetRecruitId)?0:effect.D20<10?1:2;
        }
    }
}
