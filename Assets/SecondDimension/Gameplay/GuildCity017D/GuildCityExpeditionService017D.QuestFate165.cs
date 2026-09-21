using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    [Serializable]
    public sealed class GuildQuestFateReceipt165
    {
        [JsonConstructor]
        public GuildQuestFateReceipt165(string receiptId, string campaignGuid, string expeditionId,
            string nodeId, string routeHash, string cardId, string kind, int d20, int wheelSector,
            string targetRecruitId, IReadOnlyList<string> eligibleRecruitIds, string authorityHash,
            string luckChargeId165=null)
        {
            ReceiptId=receiptId; CampaignGuid=campaignGuid; ExpeditionId=expeditionId;
            NodeId=nodeId; RouteHash=routeHash; CardId=cardId; Kind=kind;
            D20=d20; WheelSector=wheelSector; TargetRecruitId=targetRecruitId??string.Empty;
            EligibleRecruitIds=Array.AsReadOnly((eligibleRecruitIds??Array.Empty<string>()).ToArray());
            AuthorityHash=authorityHash;
            LuckChargeId165=luckChargeId165;
        }
        public string ReceiptId {get;}
        public string CampaignGuid {get;}
        public string ExpeditionId {get;}
        public string NodeId {get;}
        public string RouteHash {get;}
        public string CardId {get;}
        public string Kind {get;}
        public int D20 {get;}
        public int WheelSector {get;}
        public string TargetRecruitId {get;}
        public IReadOnlyList<string> EligibleRecruitIds {get;}
        public string AuthorityHash {get;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string LuckChargeId165 {get;}
        [JsonIgnore] public bool IsRolled165 => Kind==ExpeditionDeckService089.Wheel132 ? WheelSector>=0 : D20>0;
    }

    public sealed partial class GuildCityExpeditionService017D
    {
        static GuildQuestCardOffer090 DecorateQuestFate165(CampaignState campaign,
            ExpeditionState017D expedition, int round, int index, GuildQuestCardOffer090 card)
        {
            var kind=card.Category=="BOON"?ExpeditionDeckService089.BoonD20132:
                card.Category=="SCAR"?ExpeditionDeckService089.CurseD20132:
                card.Category=="FATE"?ExpeditionDeckService089.Wheel132:string.Empty;
            if(kind.Length==0)return card;
            // New uncommitted fate identities do not depend on a mutable roster,
            // training, or treasury. Old applied card IDs are never rewritten.
            card.CardId="QUESTCARD165_"+CanonicalJson.Sha256Hex(new {Rule="OPENING_FATE165",
                campaign.CampaignGuid,campaign.CampaignSeed,expedition.ExpeditionId,
                expedition.BoardId,expedition.CurrentNodeId,Round=round,Slot=index,
                card.Category,card.DestinationNodeId}).Substring(0,24).ToUpperInvariant();
            card.ReceiptId165=QuestCardReceiptPrefix090+card.CardId;
            card.EffectKind165=kind;
            card.TreasuryXpDelta=0;card.TreasuryXpCost=0;card.SupplyDelta=0;
            card.FatigueDelta=0;card.ThreatDelta=0;card.CheckModifierDelta=0;
            card.DieOne=0;card.DieTwo=0;card.IsPermanentHeroBoon=false;
            card.HeroRecruitId=string.Empty;card.HeroName=string.Empty;
            card.Title=kind==ExpeditionDeckService089.Wheel132?"Fortune Wheel":
                kind==ExpeditionDeckService089.CurseD20132?"Rift Hex":"Wayglass Blessing";
            card.Description=kind==ExpeditionDeckService089.Wheel132?
                "Choose this card, then spin for Guild XP, travel supplies, or permanent equipment.":
                kind==ExpeditionDeckService089.CurseD20132?
                "Choose this card, then roll a D20 to reveal a temporary quest hex.":
                "Choose this card, then roll a D20. A natural 20 can bless a hero permanently.";
            card.RiskLabel=kind==ExpeditionDeckService089.Wheel132?"SPIN THE WHEEL":"ROLL A D20";
            card.RewardPreview="MYSTERY • REVEALED AFTER THE "+
                (kind==ExpeditionDeckService089.Wheel132?"SPIN":"ROLL");
            return card;
        }

        static CampaignState WithQuestFate165(CampaignState campaign, GuildQuestFateReceipt165 pending)
        {
            var city=campaign.Guild.GuildCity;
            return campaign.With(campaign.Guild.WithGuildCity(city.With(
                expedition:city.Expedition.With(pendingQuestFate165:pending,replacePendingQuestFate165:true),
                replaceExpedition:true)),campaign.OpeningFlow);
        }

        static string FateRouteHash165(ExpeditionState017D expedition) =>
            CanonicalJson.Sha256Hex(expedition.With(pendingQuestFate165:null,replacePendingQuestFate165:true));

        static string FateHash165(GuildQuestFateReceipt165 receipt)
        {
            var payload=JObject.FromObject(new {Rule="OPENING_QUEST_FATE165",receipt.ReceiptId,
                receipt.CampaignGuid,receipt.ExpeditionId,receipt.NodeId,receipt.RouteHash,
                receipt.CardId,receipt.Kind,receipt.D20,receipt.WheelSector,
                receipt.TargetRecruitId,receipt.EligibleRecruitIds});
            // Preserve already sealed pre-luck receipt bytes and hashes.
            if(receipt.LuckChargeId165!=null)payload["LuckChargeId165"]=receipt.LuckChargeId165;
            return CanonicalJson.Sha256Hex(payload);
        }

        static GuildQuestFateReceipt165 FateReceipt165(CampaignState campaign, GuildQuestCardOffer090 card,
            int die=0,int sector=-1,string target="",IReadOnlyList<string> eligible=null,string luckChargeId=null)
        {
            var e=campaign.Guild.GuildCity.Expedition;
            var r=new GuildQuestFateReceipt165(card.ReceiptId165,campaign.CampaignGuid,e.ExpeditionId,
                e.CurrentNodeId,FateRouteHash165(e),card.CardId,card.EffectKind165,die,sector,target,eligible,"",luckChargeId);
            return new GuildQuestFateReceipt165(r.ReceiptId,r.CampaignGuid,r.ExpeditionId,r.NodeId,r.RouteHash,
                r.CardId,r.Kind,r.D20,r.WheelSector,r.TargetRecruitId,r.EligibleRecruitIds,FateHash165(r),luckChargeId);
        }

        Result<CampaignState> SealQuestFate165(CampaignState campaign,GuildQuestCardOffer090 card)
        {
            if(campaign.Guild.GuildCity.Expedition.PendingQuestFate165!=null ||
                campaign.Guild.Development.HasAdventureAuthority(card.ReceiptId165))
                return Result<CampaignState>.Failure("QUEST_FATE165_ALREADY_COMMITTED");
            if(!campaign.Guild.Development.CanRecordAdventureAuthority(card.ReceiptId165))
                return Result<CampaignState>.Failure("QUEST_CARD090_ADVENTURE_AUTHORITY_LEDGER_FULL");
            var charge=card.EffectKind165==ExpeditionDeckService089.BoonD20132?
                TownLuckConsumables165.ActiveChargeId(campaign):string.Empty;
            if(!string.IsNullOrEmpty(charge)&&campaign.Guild.Development.AppliedAdventureAuthorityIds.Count>
                GuildDevelopmentState.AdventureAuthorityEntryLimit-2)
                return Result<CampaignState>.Failure("QUEST_FATE165_LUCK_AND_REWARD_HISTORY_CAPACITY_REQUIRED");
            var receipt=FateReceipt165(campaign,card,luckChargeId:string.IsNullOrEmpty(charge)?null:charge);
            try
            {
                if(!string.IsNullOrEmpty(charge))campaign=TownLuckConsumables165.ConsumeForSeal(campaign,charge,receipt.ReceiptId);
                return Result<CampaignState>.Success(WithQuestFate165(campaign,receipt));
            }
            catch(InvalidOperationException exception)
            {return Result<CampaignState>.Failure(exception.Message);}
        }

        static int HeroBoonCount165(RecruitState hero) =>
            (hero.Progression?.UnlockedTreeIds??Array.Empty<string>()).Count(id=>
                id.StartsWith(ExpeditionDeckService089.PermanentBoonPrefix089,StringComparison.Ordinal))-
            (hero.Progression?.UnlockedTreeIds??Array.Empty<string>()).Count(id=>
                id.StartsWith(ExpeditionDeckService089.PermanentScarPrefix089,StringComparison.Ordinal));

        public static int HeroCheckBoon165(CampaignState campaign,string recruitId)
        {
            var hero=campaign?.Guild?.Recruits.FirstOrDefault(value=>value.RecruitId==recruitId);
            return hero==null?0:Math.Max(-2,Math.Min(2,HeroBoonCount165(hero)));
        }

        static GuildQuestFateReceipt165 ResolveQuestFate165(CampaignState campaign,
            GuildQuestCardOffer090 card,IReadOnlyList<string> eligible,string luckChargeId=null)
        {
            var e=campaign.Guild.GuildCity.Expedition;
            var rng=Pcg32.FromParts("OPENING_QUEST_FATE165",campaign.CampaignSeed.ToString(),
                campaign.CampaignGuid,e.ExpeditionId,card.CardId);
            if(card.EffectKind165==ExpeditionDeckService089.Wheel132)
                return FateReceipt165(campaign,card,sector:rng.NextInclusive(0,2));
            var die=rng.NextInclusive(1,20);
            if(!string.IsNullOrEmpty(luckChargeId))die=Math.Max(die,rng.NextInclusive(1,20));
            var target=card.EffectKind165==ExpeditionDeckService089.BoonD20132&&die==20&&eligible.Count>0?
                eligible[rng.NextInclusive(0,eligible.Count-1)]:string.Empty;
            return FateReceipt165(campaign,card,die:die,target:target,eligible:eligible,luckChargeId:luckChargeId);
        }

        bool TryQuestFate165(CampaignState campaign,GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment,string expectedReceiptId,
            out GuildQuestFateReceipt165 receipt,out GuildQuestCardOffer090 card)
        {
            receipt=campaign?.Guild?.GuildCity?.Expedition?.PendingQuestFate165;card=null;
            if(receipt==null||content==null||receipt.ReceiptId!=expectedReceiptId||
                campaign.Guild.Development.HasAdventureAuthority(receipt.ReceiptId)||
                receipt.AuthorityHash!=FateHash165(receipt))return false;
            var e=campaign.Guild.GuildCity.Expedition;
            if(receipt.CampaignGuid!=campaign.CampaignGuid||receipt.ExpeditionId!=e.ExpeditionId||
                receipt.NodeId!=e.CurrentNodeId||receipt.RouteHash!=FateRouteHash165(e))return false;
            if(receipt.LuckChargeId165!=null&&(receipt.Kind!=ExpeditionDeckService089.BoonD20132||
                string.IsNullOrEmpty(receipt.LuckChargeId165)||
                !TownLuckConsumables165.WasConsumedFor(campaign,receipt.LuckChargeId165,receipt.ReceiptId)))return false;
            var eligible=receipt.EligibleRecruitIds;
            if(eligible.Any(string.IsNullOrWhiteSpace)||
                !eligible.SequenceEqual(eligible.Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal))||
                receipt.Kind!=ExpeditionDeckService089.BoonD20132&&eligible.Count!=0)return false;
            var selectedCardId=receipt.CardId;
            card=BuildQuestCardRow090(WithQuestFate165(campaign,null),content,recruitment)
                .FirstOrDefault(value=>value.CardId==selectedCardId);
            if(card==null||card.EffectKind165!=receipt.Kind||card.ReceiptId165!=receipt.ReceiptId)return false;
            var expected=receipt.IsRolled165?ResolveQuestFate165(campaign,card,eligible,receipt.LuckChargeId165):
                FateReceipt165(campaign,card,luckChargeId:receipt.LuckChargeId165);
            return CanonicalJson.Serialize(expected)==CanonicalJson.Serialize(receipt);
        }

        public Result<CampaignState> RollQuestFate165(CampaignState campaign,GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment,string expectedReceiptId)
        {
            if(!TryQuestFate165(campaign,content,recruitment,expectedReceiptId,out var receipt,out var card)||receipt.IsRolled165)
                return Result<CampaignState>.Failure("QUEST_FATE165_ROLL_NOT_AVAILABLE");
            var eligible=receipt.Kind==ExpeditionDeckService089.BoonD20132?
                campaign.Guild.Recruits.Where(hero=>hero!=null&&HeroBoonCount165(hero)<2)
                    .Select(hero=>hero.RecruitId).Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal).ToArray():
                Array.Empty<string>();
            return Result<CampaignState>.Success(WithQuestFate165(campaign,ResolveQuestFate165(campaign,card,eligible,receipt.LuckChargeId165)));
        }

        public GuildQuestCardOffer090 DescribeQuestFate165(CampaignState campaign,GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment=null)
        {
            var id=campaign?.Guild?.GuildCity?.Expedition?.PendingQuestFate165?.ReceiptId;
            if(!TryQuestFate165(campaign,content,recruitment,id,out var receipt,out var card))return null;
            card.D20165=receipt.D20;card.WheelSector165=receipt.WheelSector;card.EffectRolled165=receipt.IsRolled165;
            if(receipt.LuckChargeId165!=null)
            {
                card.Lucky165=true;
                card.Description="Lucky blessing • roll two D20s and keep the higher. Your tonic is already committed to this card.";
                card.RewardPreview="LUCKY BLESSING • TWO D20s, KEEP THE HIGHER";
            }
            if(!receipt.IsRolled165)return card;
            if(receipt.Kind==ExpeditionDeckService089.Wheel132)
            {
                if(receipt.WheelSector==0){card.TreasuryXpDelta=20;card.RewardPreview="+20 spendable Guild XP";}
                else if(receipt.WheelSector==1){card.SupplyDelta=3;card.FatigueDelta=-2;card.RewardPreview="+3 travel supplies • −2 fatigue";}
                else
                {
                    var item=CreateQuestCardEquipment090(card.CardId,receipt.ReceiptId,false);
                    card.ItemName=item.DisplayName;card.RarityId=item.QualityId;
                    card.RewardPreview=item.DisplayName+" • permanent equipment";
                }
            }
            else if(receipt.Kind==ExpeditionDeckService089.CurseD20132)
            {
                card.CheckModifierDelta=receipt.D20<10?-2:-1;
                card.TreasuryXpDelta=24;card.FatigueDelta=2;card.ThreatDelta=1;
                card.RewardPreview="+24 Guild XP • Quest hex: "+card.CheckModifierDelta+
                    " to checks • +2 fatigue • +1 threat";
            }
            else if(receipt.D20==20&&!string.IsNullOrWhiteSpace(receipt.TargetRecruitId)&&
                campaign.Guild.Recruits.Any(hero=>hero.RecruitId==receipt.TargetRecruitId&&HeroBoonCount165(hero)<2))
            {
                card.HeroRecruitId=receipt.TargetRecruitId;
                card.HeroName=campaign.Guild.Recruits.FirstOrDefault(hero=>hero.RecruitId==receipt.TargetRecruitId)?.DisplayName??receipt.TargetRecruitId;
                card.RewardPreview="NATURAL 20 • "+card.HeroName+" gains a permanent hero blessing";
            }
            else
            {card.CheckModifierDelta=receipt.D20<10?1:2;card.RewardPreview="Quest blessing • +"+card.CheckModifierDelta+" to checks for this quest";}
            if(receipt.Kind==ExpeditionDeckService089.BoonD20132)
            {
                card.SupplyDelta=1;
                card.RewardPreview+=" • +1 supply";
            }
            if(card.Lucky165)card.RewardPreview+=" • higher of two D20 rolls";
            return card;
        }

        public Result<CampaignState> CollectQuestFate165(CampaignState campaign,GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment,string expectedReceiptId)
        {
            if(!TryQuestFate165(campaign,content,recruitment,expectedReceiptId,out var receipt,out var ignored)||!receipt.IsRolled165)
                return Result<CampaignState>.Failure("QUEST_FATE165_COLLECT_NOT_AVAILABLE");
            var card=DescribeQuestFate165(campaign,content,recruitment);
            // Apply to today's shared Guild state, never to a parked treasury or
            // roster snapshot. The pending encounter only owns its route/reward.
            var applied=ApplyQuestCard165(WithQuestFate165(campaign,null),content,recruitment,card);
            if(!applied.IsSuccess)return applied;
            var candidate=applied.Value;var guild=candidate.Guild;
            var recruits=new List<RecruitState>(guild.Recruits);var inventory=new List<EquipmentItemState>(guild.Inventory);
            if(receipt.Kind==ExpeditionDeckService089.Wheel132&&receipt.WheelSector==2)
            {
                var item=CreateQuestCardEquipment090(card.CardId,receipt.ReceiptId,false);
                if(!inventory.Any(value=>value.InstanceId==item.InstanceId))inventory.Add(item);
            }
            if(card.HeroRecruitId.Length>0)
            {
                var index=recruits.FindIndex(hero=>hero.RecruitId==receipt.TargetRecruitId);
                var hero=recruits[index];var progression=hero.Progression??RecruitProgressionState.Default();
                var trait=ExpeditionDeckService089.PermanentBoonPrefix089+"NATURAL20_165_"+receipt.ReceiptId;
                recruits[index]=hero.WithProgression(progression.WithUnlockedTrees(progression.UnlockedTreeIds.Concat(new[]{trait})
                    .Distinct(StringComparer.Ordinal).OrderBy(id=>id,StringComparer.Ordinal).ToArray()));
            }
            return Result<CampaignState>.Success(candidate.With(guild.With(guild.TreasuryXp,recruits.AsReadOnly(),guild.Unions,
                inventory.AsReadOnly(),guild.Development),candidate.OpeningFlow));
        }
    }
}
