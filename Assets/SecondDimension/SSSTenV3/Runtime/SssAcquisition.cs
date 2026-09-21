using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.SSS.V3
{
    [Serializable] public sealed class SssRewardOffer
    {
        public string key,source,heroId,eligibilitySnapshot;
        public bool eligible,success,claimed;
        public int chanceBasisPoints,chanceRoll,selectionIndex;
        public SssRewardOffer Copy() { return (SssRewardOffer)MemberwiseClone(); }
    }
    public sealed class SssOfferReduction
    {
        public SssSave next;
        public SssRewardOffer offer;
        public bool duplicate;
    }
    public sealed class SssGrantPlan
    {
        public SssSave next;
        public string heroId,receipt,reason;
        public bool grant;
    }
    public static class SssAcquisition
    {
        public const int DraftTowerChanceBasisPoints=100; // 1% initial tuning, not owner-specified.
        public static bool CampaignUnlocked(string creditedCampaignWins)
        { return ExactNumbers.Read(creditedCampaignWins)>=100; }
        public static bool TowerUnlocked(string clearedFloor)
        { return ExactNumbers.Read(clearedFloor)>=500; }
        public static string[] AvailablePool(SssSave save,IEnumerable<string> ownedHeroIds)
        {
            SssProgression.Validate(save);if(ownedHeroIds==null)throw new ArgumentNullException("ownedHeroIds");
            var excluded=new HashSet<string>(ownedHeroIds.Select(SssHeroes.CanonicalId),StringComparer.Ordinal);
            foreach(var x in save.rewardOffers.Where(x=>x.success && !x.claimed))excluded.Add(x.heroId);
            return SssHeroes.All.Where(x=>!excluded.Contains(x)).ToArray();
        }
        static SssOfferReduction Record(SssSave current,string key,string source,string snapshot,bool eligible,bool passed,
            int chance,int roll,int selectionIndex,IEnumerable<string> owned)
        {
            SssProgression.Validate(current);ExactNumbers.RequireId(key);
            var prior=current.rewardOffers.SingleOrDefault(x=>x.key==key);
            if(prior!=null)return new SssOfferReduction {next=current.Copy(),offer=prior.Copy(),duplicate=true};
            var pool=AvailablePool(current,owned);
            bool success=eligible && passed && pool.Length>0;
            if(success && (selectionIndex<0 || selectionIndex>=pool.Length))throw new ArgumentOutOfRangeException("selectionIndex","Use the saved host RNG to select an index from AvailablePool().");
            var offer=new SssRewardOffer {key=key,source=source,eligibilitySnapshot=snapshot,eligible=eligible,success=success,
                heroId=success?pool[selectionIndex]:null,chanceBasisPoints=chance,chanceRoll=roll,selectionIndex=selectionIndex};
            var next=current.Copy();next.rewardOffers.Add(offer.Copy());next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            return new SssOfferReduction {next=next,offer=offer};
        }
        // Call from the SAME live Tower victory/reward transaction. RNG rolls and these
        // receipts must persist atomically with normal floor rewards and reward claims.
        public static SssOfferReduction TowerReward(SssSave s,string rewardReceipt,string actualClearedFloor,int savedRoll0To9999,
            int savedSelectionIndex,IEnumerable<string> owned,int chance=DraftTowerChanceBasisPoints)
        {
            if(chance<0 || chance>10000 || savedRoll0To9999<0 || savedRoll0To9999>=10000)throw new ArgumentOutOfRangeException("chance/roll");
            ExactNumbers.RequireId(rewardReceipt);
            bool eligible=TowerUnlocked(actualClearedFloor);
            return Record(s,ExactNumbers.Key("TowerSss",rewardReceipt),"Tower",actualClearedFloor,eligible,
                savedRoll0To9999<chance,chance,savedRoll0To9999,savedSelectionIndex,owned);
        }
        // Chance belongs to the EXISTING shuffled Expedition Deck, not an extra hidden
        // die after a successful SSS Contract draw. Do NOT call for the two unchosen cards.
        public static SssOfferReduction SelectedCampaignContract(SssSave s,string cardInstanceId,string winsAtDeckDraw,
            bool selectedResolvedSssCard,int savedSelectionIndex,IEnumerable<string> owned)
        {
            ExactNumbers.RequireId(cardInstanceId);
            if(!selectedResolvedSssCard)throw new InvalidOperationException("Only the selected, resolved SSS Contract grants an offer.");
            bool eligible=CampaignUnlocked(winsAtDeckDraw);
            return Record(s,ExactNumbers.Key("CampaignSss",cardInstanceId),"CampaignCard",winsAtDeckDraw,eligible,true,
                10000,0,savedSelectionIndex,owned);
        }
        public static SssGrantPlan ClaimOffer(SssSave current,string offerKey,IEnumerable<string> owned)
        {
            SssProgression.Validate(current);if(owned==null)throw new ArgumentNullException("owned");
            var next=current.Copy();var offer=next.rewardOffers.SingleOrDefault(x=>x.key==offerKey);
            if(offer==null || !offer.success)return new SssGrantPlan {next=next,reason="No successful offer."};
            if(offer.claimed)return new SssGrantPlan {next=next,reason="Already claimed."};
            bool has=owned.Select(SssHeroes.CanonicalId).Contains(offer.heroId,StringComparer.Ordinal);
            offer.claimed=true;next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            return new SssGrantPlan {next=next,heroId=offer.heroId,receipt=offerKey,grant=!has,
                reason=has?"Already owned: no duplicate, no replacement roll, no extra reward.":"Grant through current recruit authority."};
        }
        public static SssGrantPlan RedeemCode(SssSave current,string rawCode,IEnumerable<string> owned)
        {
            SssProgression.Validate(current);if(owned==null)throw new ArgumentNullException("owned");
            string hero=SssHeroes.RedemptionHero(rawCode);var next=current.Copy();
            if(hero==null)return new SssGrantPlan {next=next,reason="Unknown SSS code; delegate non-SSS codes to the existing registry."};
            string key=ExactNumbers.Key("SssCode",rawCode.Trim().ToUpperInvariant());
            if(current.codeReceipts.Contains(key))return new SssGrantPlan {next=next,heroId=hero,reason="Already redeemed in this save."};
            bool has=owned.Select(SssHeroes.CanonicalId).Contains(hero,StringComparer.Ordinal);
            next.codeReceipts.Add(key);next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            return new SssGrantPlan {next=next,heroId=hero,receipt=key,grant=!has,
                reason=has?"Already owned: code acknowledged without duplicate or extra reward.":"Grant this SSS hero only. No mastery, wins or floors added."};
        }
    }
}
