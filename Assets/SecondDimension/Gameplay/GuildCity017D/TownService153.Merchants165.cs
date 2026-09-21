using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.GuildCity017D
{
    public static partial class TownService153
    {
        public static string OfferId165(CampaignState s,int merchant,int slot)=>
            "TOWN165_"+s.Guild.GuildCity.OperationOrdinal+"_"+merchant+"_"+slot+StockSuffix159(s);
        static bool Coordinates165(CampaignState s,string offer,out int merchant,out int slot)
        {
            merchant=-1;slot=-1;if(s?.Guild?.GuildCity==null)return false;
            for(int m=0;m<5;m++)for(int n=0;n<MerchantStockProgression163.SlotsPerMerchant(s);n++)
                if(offer==OfferId165(s,m,n)){merchant=m;slot=n;return true;}
            return false;
        }
        public static bool IsOffer165(CampaignState s,string offer)=>Coordinates165(s,offer,out _,out _);
        static EquipmentItemState StockEquipment165(CampaignState state,string offer,string request,int merchant,int slot)
        {
            var hash=CanonicalJson.Sha256Hex(new{state.CampaignGuid,Offer=offer,Policy="MERCHANT_SPECIALTIES165"});
            var source=MerchantStockProgression163.Apply(state,ExpeditionDeckService089.MerchantEquipmentReward089(hash,request));
            string name,tag,equipmentSlot;int i=slot%6;
            if(merchant==0)
            {
                name=new[]{"Hearthguard Coat","Trailkeeper Coat","Packguard Buckler","Hearthguard Vest","Rainward Coat","Caravan Shield"}[i];
                bool shield=i==2||i==5;tag=shield?"SHIELD":"ARMOR";equipmentSlot=shield?EquipmentSlotIds.OffHand:EquipmentSlotIds.BodyArmor;
            }
            else if(merchant==1)
            {
                name=new[]{"Brasshand Sword","Brasshand Greataxe","Brasshand Pike","Brasshand Dagger","Brasshand Bow","Brasshand Shield"}[i];
                tag=new[]{"SWORD","GREAT_AXE","SPEAR","DAGGER","BOW","SHIELD"}[i];equipmentSlot=i==5?EquipmentSlotIds.OffHand:EquipmentSlotIds.MainHand;
            }
            else if(merchant==2)
            {
                name=new[]{"Wayfinder Amulet","Trailstar Pendant","Farpath Ring","Scout's Brooch","Windglass Charm","Horizon Seal"}[i];
                tag="RELIC";equipmentSlot=i%2==0?EquipmentSlotIds.AccessoryOne:EquipmentSlotIds.AccessoryTwo;
            }
            else
            {
                name=new[]{"Glass Lantern Staff","Astral Focus","Dreamglass Amulet","Archive Wand","Dawnlight Catalyst","Echo Charm"}[i];
                tag=i==2||i==5?"RELIC":i==1||i==4?"FOCUS":"STAFF";
                equipmentSlot=tag=="RELIC"?EquipmentSlotIds.ToolRelic:EquipmentSlotIds.MainHand;
            }
            var quality=source.QualityId.Substring("QUALITY_".Length).ToLowerInvariant();quality=char.ToUpperInvariant(quality[0])+quality.Substring(1);
            return new EquipmentItemState(source.InstanceId,"TOWN165_WARE_"+merchant+"_"+slot+"_"+source.QualityId,
                quality+" "+name,merchant==2?new[]{EquipmentSlotIds.AccessoryOne,EquipmentSlotIds.AccessoryTwo}:new[]{equipmentSlot},
                new[]{"EXPEDITION_MERCHANT_089","MERCHANT_SPECIALTY165",tag,tag=="RELIC"?"AMULET":"MERCHANT_GEAR165"},source.QualityId,10000,false);
        }
        static TownPurchaseQuote153 ReadOffer165(CampaignState state,string offer,string revision,GuildCityRecruitmentService017D recruitment)
        {
            if(!Coordinates165(state,offer,out var merchant,out var slot))throw new ArgumentException("Expired merchant offer.");
            var request=Receipt(state,offer);var rev=revision??CanonicalJson.Sha256Hex(state);
            if(merchant==3)
            {
                if(recruitment==null)throw new InvalidOperationException("Native recruitment authority is required for exchange offers.");
                var hero=recruitment.DescribeMerchantHero165(state,offer,slot);
                if(string.IsNullOrEmpty(hero.StableId))throw new InvalidOperationException(hero.Failure??"Hero offer unavailable.");
                return new TownPurchaseQuote153(offer,rev,request,null,TownProgression159.Discount(state,1,hero.Cost),state.Guild.TreasuryXp,
                    "HERO",hero.Name,hero.Failure??(hero.Duplicate?"Purchases one duplicate and applies the shown native Ascension or Art growth. No extra body is added.":"Purchases this exact authored hero and places them in Reserve. Unions remain your choice."),
                    hero.Rank+" / "+hero.Summary,"HERO:"+hero.StableId,hero.StableId,hero.Failure);
            }
            if(merchant==0&&slot==0)
            {
                var item=new EquipmentItemState("TOWN165_SUPPLY_"+CanonicalJson.Sha256Hex(request).Substring(0,24).ToUpperInvariant(),
                    TownLuckConsumables165.DefinitionId,"Wayglass Luck Tonic",Array.Empty<string>(),new[]{"CONSUMABLE165","LUCK_TONIC165"},"QUALITY_RARE",10000,false,true);
                return new TownPurchaseQuote153(offer,rev,request,item,TownProgression159.Discount(state,1,60),state.Guild.TreasuryXp,
                    "CONSUMABLE",item.DisplayName,"Activate from your Travel Pouch. Your next unsealed blessing rolls two D20s and keeps the higher. Consumed when you choose that blessing; existing sealed rolls never change.",
                    "One luck charge • natural20 chance 5% → 9.75%","CONSUMABLE:LUCK165");
            }
            var equipment=StockEquipment165(state,offer,request,merchant,slot);var power=M2EquipmentPowerPolicy087.Resolve(equipment);
            return new TownPurchaseQuote153(offer,rev,request,equipment,TownProgression159.Discount(state,1,ExpeditionDeckService089.MerchantCost089(equipment.QualityId)),state.Guild.TreasuryXp,
                "EQUIPMENT",equipment.DisplayName,"Adds this exact item to Inventory. Equip it deliberately through Heroes & Gear.",
                "Physical attack +"+power.PhysicalAttack+" · Mystic attack +"+power.MysticAttack,null);
        }
        static Result<CampaignState> ConfirmOffer165(CampaignState state,TownPurchaseQuote153 quoted,TownPurchaseQuote153 actual,GuildCityRecruitmentService017D recruitment)
        {
            if(actual.Cost!=quoted.Cost||actual.Kind165!=quoted.Kind165||actual.HeroStableId165!=quoted.HeroStableId165||
                CanonicalJson.Sha256Hex(new{actual.Item})!=CanonicalJson.Sha256Hex(new{quoted.Item}))return Result<CampaignState>.Failure("The offer changed. Review it again.");
            if(!actual.Affordable)return Result<CampaignState>.Failure("Not enough Treasury XP.");
            var development=state.Guild.Development;
            if(!development.CanRecordAdventureAuthority(actual.RequestId))return Result<CampaignState>.Failure("Purchase receipt history is full.");
            CampaignState candidate=state;
            if(actual.Kind165=="HERO")
            {
                if(recruitment==null||!Coordinates165(state,actual.OfferId,out _,out var slot))return Result<CampaignState>.Failure("Native recruitment authority is unavailable.");
                var granted=recruitment.GrantMerchantHero165(state,actual.OfferId,slot,actual.HeroStableId165,actual.RequestId);
                if(!granted.IsSuccess)return granted;candidate=granted.Value;
            }
            else
            {
                if(actual.Item==null||state.Guild.Inventory.Any(i=>i.InstanceId==actual.Item.InstanceId)||
                    state.Guild.Recruits.Any(r=>r.Equipment.Assignments.Any(a=>a.Item.InstanceId==actual.Item.InstanceId)))
                    return Result<CampaignState>.Failure("This item's ownership needs reconciliation.");
                var old=state.Guild;candidate=state.With(old.With(old.TreasuryXp,old.Recruits,old.Unions,
                    old.Inventory.Concat(new[]{actual.Item}).ToArray(),old.Development),state.OpeningFlow);
            }
            var g=candidate.Guild;
            return Result<CampaignState>.Success(candidate.With(g.With(checked(g.TreasuryXp-actual.Cost),g.Recruits,g.Unions,g.Inventory,
                g.Development.RecordAdventureAuthority(actual.RequestId)),candidate.OpeningFlow));
        }
    }
}
