using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed class TownPurchaseQuote153
    {
        public string OfferId {get;} public string Revision {get;} public string RequestId {get;}
        public string Name=>Name165??Item?.DisplayName??string.Empty; public EquipmentItemState Item {get;}
        public string Kind165 {get;} public string Description165 {get;} public string Stats165 {get;}
        public string ArtKey165 {get;} public string HeroStableId165 {get;} public string Name165 {get;} public string UnavailableReason165 {get;}
        public long Cost {get;} public long Wallet {get;} public bool Affordable=>Cost<=Wallet;
        internal TownPurchaseQuote153(string offer,string revision,string request,EquipmentItemState item,long cost,long wallet)
        {OfferId=offer;Revision=revision;RequestId=request;Item=item;Cost=cost;Wallet=wallet;Kind165="EQUIPMENT";}
        internal TownPurchaseQuote153(string offer,string revision,string request,EquipmentItemState item,long cost,long wallet,
            string kind,string name,string description,string stats,string artKey,string heroId=null,string unavailableReason=null)
        {OfferId=offer;Revision=revision;RequestId=request;Item=item;Cost=cost;Wallet=wallet;Kind165=kind;Name165=name;
         Description165=description;Stats165=stats;ArtKey165=artKey;HeroStableId165=heroId;UnavailableReason165=unavailableReason;}
    }
    public sealed class TownBuildingQuote153
    {
        public string FacilityId {get;} public string PlotId {get;} public string BuildingId {get;}
        public string Revision {get;} public string RequestId {get;} public string Title {get;}
        public string Summary {get;} public string CostText {get;} public bool Affordable {get;}
        public bool IsNew {get;} internal string CandidateHash {get;}
        internal TownBuildingQuote153(string facility,string plot,string building,string revision,string request,
            string title,string summary,string cost,bool affordable,bool isNew,string candidate)
        {FacilityId=facility;PlotId=plot;BuildingId=building;Revision=revision;RequestId=request;Title=title;
         Summary=summary;CostText=cost;Affordable=affordable;IsNew=isNew;CandidateHash=candidate;}
    }
    public sealed class TownRestockQuote159
    {
        public string Revision {get;} public string RequestId {get;} public int Generation {get;}
        public long Cost {get;} public long Wallet {get;} public bool Affordable=>Cost<=Wallet;
        internal TownRestockQuote159(string revision,string request,int generation,long cost,long wallet)
        {Revision=revision;RequestId=request;Generation=generation;Cost=cost;Wallet=wallet;}
    }
    public static partial class TownService153
    {
        public static readonly string[] FacilityIds={"GUILD_HALL","MERCHANT_ROW","FORGE","TRAINING_GROUNDS","RECRUITMENT_HALL","EXPEDITION_GUILD","INFIRMARY","ACADEMY_ARCHIVE"};
        public static readonly string[] BuildingIds={"","GC017D_BUILD_MARKET","GC017D_BUILD_FORGE","GC017D_BUILD_TRAINING_GROUNDS","GC017D_BUILD_RECRUITMENT_OFFICE","GC017D_BUILD_CONTRACT_HOUSE","GC017D_BUILD_INFIRMARY","GC017D_BUILD_MYSTIC_ACADEMY"};
        public static string Safety(CampaignState s)=>IndependentProgression159.BlockReason(s);
        public static int BuildingLevel(CampaignState s,string id)=>s.Guild.GuildCity.CityPlots.Where(p=>p.BuildingId==id).Select(p=>p.BuildingLevel).DefaultIfEmpty(0).Max();
        static Result<CampaignState> Build(CampaignState s,GuildCityContent017D content,int index,string plot)
        {
            int before=TownProgression159.Level(s,index);
            if(before==int.MaxValue)return Result<CampaignState>.Failure("This facility needs a wider level record before the next upgrade.");
            int next=before+1;
            bool credit=index>0&&before==0&&s.Guild.GuildCity.CharterBuildCredits>0;
            long price=credit?0:TownProgression159.Cost(index,next);
            if(price>s.Guild.TreasuryXp)return Result<CampaignState>.Failure("Not enough Treasury XP.");
            var development=s.Guild.Development.SetFacilityLevel(TownProgression159.StateIds[index],next,price);
            var city=s.Guild.GuildCity;
            if(index>0)
            {
                var definition=content.Building(BuildingIds[index]);
                int prior=development.Facilities.Where(f=>f.FacilityId==definition.FacilityId).Select(f=>f.Level).DefaultIfEmpty(0).Max();
                int nativeLevel=Math.Max(prior,Math.Min(next,definition.MaxLevel));
                development=development.SetFacilityLevel(definition.FacilityId,nativeLevel,0);
                if(!string.IsNullOrEmpty(plot))
                {
                    var current=city.CityPlots.FirstOrDefault(p=>p.PlotId==plot);
                    if(current==null||current.DistrictId!=definition.DistrictId||
                        (!string.IsNullOrEmpty(current.BuildingId)&&current.BuildingId!=definition.Id))
                        return Result<CampaignState>.Failure("Review this building's plot again.");
                    // Existing native effects stay at their authored limits. The
                    // independent paid track carries its own one documented benefit.
                    city=city.With(cityPlots:city.CityPlots.Select(p=>p.PlotId==plot?
                        p.With(unlocked:true,buildingId:definition.Id,buildingLevel:Math.Max(p.BuildingLevel,Math.Min(next,definition.MaxLevel)),constructionProgress:100):p).ToArray());
                }
            }
            city=city.With(charterBuildCredits:city.CharterBuildCredits-(credit?1:0),lastCheckpointId:"town_progression159");
            var guild=s.Guild.With(checked(s.Guild.TreasuryXp-price),s.Guild.Recruits,s.Guild.Unions,s.Guild.Inventory,development).WithGuildCity(city);
            return Result<CampaignState>.Success(s.With(guild,s.OpeningFlow));
        }
        static string RestockPrefix159(CampaignState s)=>"TOWN_RESTOCK159_"+CanonicalJson.Sha256Hex(s.CampaignGuid).Substring(0,12).ToUpperInvariant()+"_";
        sealed class ShelfGeneration159 { public readonly int Value;public ShelfGeneration159(int value){Value=value;} }
        static readonly ConditionalWeakTable<CampaignState,ShelfGeneration159> ShelfGenerations159=new ConditionalWeakTable<CampaignState,ShelfGeneration159>();
        public static int RestockGeneration159(CampaignState s)=>ShelfGenerations159.GetValue(s,ReadRestockGeneration159).Value;
        static ShelfGeneration159 ReadRestockGeneration159(CampaignState s)
        {
            var prefix=RestockPrefix159(s);int generation=0;
            foreach(var receipt in s.Guild.Development.AppliedAdventureAuthorityIds)
                if(receipt.StartsWith(prefix,StringComparison.Ordinal)&&
                    int.TryParse(receipt.Substring(prefix.Length),NumberStyles.None,CultureInfo.InvariantCulture,out var value)&&
                    receipt.Substring(prefix.Length)==value.ToString(CultureInfo.InvariantCulture)&&value>generation)
                    generation=value;
            return new ShelfGeneration159(generation);
        }
        public static string StockSuffix159(CampaignState s)
        {var generation=RestockGeneration159(s);return generation==0?string.Empty:"_R"+generation;}
        public static long RestockCost159(CampaignState s)=>TownProgression159.Discount(s,1,25);
        public static Result<TownRestockQuote159> QuoteRestock159(CampaignState s)
        {
            var error=Safety(s);if(error!=null)return Result<TownRestockQuote159>.Failure(error);
            if(s.Guild.Development.AppliedAdventureAuthorityIds.Count>=GuildDevelopmentState.AdventureAuthorityEntryLimit)
                return Result<TownRestockQuote159>.Failure("The saved receipt history needs a capacity update.");
            var current=RestockGeneration159(s);
            if(current==int.MaxValue)return Result<TownRestockQuote159>.Failure("The market restock history is full.");
            var next=current+1;
            return Result<TownRestockQuote159>.Success(new TownRestockQuote159(CanonicalJson.Sha256Hex(s),
                RestockPrefix159(s)+next,next,RestockCost159(s),s.Guild.TreasuryXp));
        }
        public static Result<CampaignState> ConfirmRestock159(CampaignState s,TownRestockQuote159 q)
        {
            if(s?.Guild==null||q==null||q.Generation<=0||q.RequestId!=RestockPrefix159(s)+q.Generation)
                return Result<CampaignState>.Failure("Review this restock again.");
            if(s.Guild.Development.HasAdventureAuthority(q.RequestId))return Result<CampaignState>.Success(s);
            if(CanonicalJson.Sha256Hex(s)!=q.Revision)return Result<CampaignState>.Failure("Your Guild changed. Review the restock again.");
            var read=QuoteRestock159(s);if(!read.IsSuccess)return Result<CampaignState>.Failure(read.Errors.ToArray());
            if(read.Value.RequestId!=q.RequestId||read.Value.Cost!=q.Cost||!read.Value.Affordable)
                return Result<CampaignState>.Failure("Not enough Treasury XP, or the restock changed.");
            var g=s.Guild;return Result<CampaignState>.Success(s.With(g.With(checked(g.TreasuryXp-q.Cost),g.Recruits,g.Unions,
                g.Inventory,g.Development.RecordAdventureAuthority(q.RequestId)),s.OpeningFlow));
        }
        public static string OfferId(CampaignState s,int merchant,int slot)=>"TOWN153_"+s.Guild.GuildCity.OperationOrdinal+"_"+merchant+"_"+slot+StockSuffix159(s);
        static string Receipt(CampaignState s,string offer)=>"TOWN_PURCHASE153_"+CanonicalJson.Sha256Hex(new{s.CampaignGuid,Offer=offer}).Substring(0,26).ToUpperInvariant();
        static bool ValidOffer(CampaignState s,string offer)
        {if(IsOffer165(s,offer))return true;if(TownCommissions154.IsCurrentOffer(s,offer))return true;for(int m=0;m<5;m++)for(int i=0;i<MerchantStockProgression163.SlotsPerMerchant(s);i++)if(offer==OfferId(s,m,i))return true;return false;}
        public static TownPurchaseQuote153 ReadOffer(CampaignState s,string offer,string revision=null,GuildCityRecruitmentService017D recruitment=null)
        {
            if(s==null||!ValidOffer(s,offer))throw new ArgumentException("That offer is no longer on the shelves.");
            if(IsOffer165(s,offer))return ReadOffer165(s,offer,revision,recruitment);
            var request=Receipt(s,offer);
            // Both the preview and actual grant use this exact native card/receipt identity.
            var card=CanonicalJson.Sha256Hex(new{s.CampaignGuid,Offer=offer,Policy="NATIVE_MERCHANT_089"});
            var item=TownCommissions154.IsCurrentOffer(s,offer)?TownCommissions154.CreateItem(s,offer,request):
                ExpeditionDeckService089.MerchantEquipmentReward089(card,request);
            item=MerchantStockProgression163.Apply(s,item);
            return new TownPurchaseQuote153(offer,revision??CanonicalJson.Sha256Hex(s),request,item,
                TownProgression159.Discount(s,1,ExpeditionDeckService089.MerchantCost089(item.QualityId)),s.Guild.TreasuryXp);
        }
        public static bool Sold(CampaignState s,string offer)=>s.Guild.Development.HasAdventureAuthority(Receipt(s,offer));
        public static Result<TownPurchaseQuote153> QuotePurchase(CampaignState s,string offer,GuildCityRecruitmentService017D recruitment=null)
        {
            var error=Safety(s);if(error!=null)return Result<TownPurchaseQuote153>.Failure(error);
            if(!ValidOffer(s,offer))return Result<TownPurchaseQuote153>.Failure("The market stock changed. Review the shelves again.");
            if(TownCommissions154.IsCurrentOffer(s,offer)&&TownProgression159.Level(s,1)<TownCommissions154.RequiredMarketLevel)
                return Result<TownPurchaseQuote153>.Failure("Expand Merchant Row to level 3 to unlock weapon and armor commissions.");
            if(Sold(s,offer))return Result<TownPurchaseQuote153>.Failure("This item has already been purchased.");
            var quote=ReadOffer(s,offer,null,recruitment);
            return string.IsNullOrEmpty(quote.UnavailableReason165)?Result<TownPurchaseQuote153>.Success(quote):Result<TownPurchaseQuote153>.Failure(quote.UnavailableReason165);
        }
        public static Result<CampaignState> ConfirmPurchase(CampaignState s,TownPurchaseQuote153 q,GuildCityRecruitmentService017D recruitment=null)
        {
            if(s==null||q==null||q.RequestId!=Receipt(s,q.OfferId))return Result<CampaignState>.Failure("Review this purchase again.");
            if(s.Guild.Development.HasAdventureAuthority(q.RequestId))return Result<CampaignState>.Success(s);
            if(CanonicalJson.Sha256Hex(s)!=q.Revision)return Result<CampaignState>.Failure("Your Guild changed. Review the purchase again.");
            var current=QuotePurchase(s,q.OfferId,recruitment);if(!current.IsSuccess)return Result<CampaignState>.Failure(current.Errors.ToArray());
            var actual=current.Value;
            if(IsOffer165(s,q.OfferId))return ConfirmOffer165(s,q,actual,recruitment);
            if(actual.Cost!=q.Cost||CanonicalJson.Sha256Hex(actual.Item)!=CanonicalJson.Sha256Hex(q.Item))return Result<CampaignState>.Failure("The offer changed. Review it again.");
            if(!actual.Affordable)return Result<CampaignState>.Failure("Not enough Treasury XP.");
            if(s.Guild.Development.AppliedAdventureAuthorityIds.Count>=GuildDevelopmentState.AdventureAuthorityEntryLimit)return Result<CampaignState>.Failure("The saved receipt history needs a capacity update.");
            if(s.Guild.Inventory.Any(i=>i.InstanceId==actual.Item.InstanceId)||s.Guild.Recruits.Any(r=>r.Equipment.Assignments.Any(a=>a.Item.InstanceId==actual.Item.InstanceId)))return Result<CampaignState>.Failure("This item's ownership needs reconciliation.");
            var g=s.Guild;return Result<CampaignState>.Success(s.With(g.With(checked(g.TreasuryXp-actual.Cost),g.Recruits,g.Unions,
                g.Inventory.Concat(new[]{actual.Item}).ToArray(),g.Development.RecordAdventureAuthority(actual.RequestId)),s.OpeningFlow));
        }
        public static bool CanReviewBuilding154(CampaignState s,GuildCityContent017D content,string facility,SecondDimension.Gameplay.Campaign022.ICampaignRegistry022 forgeRecipes = null)=>QuoteBuildingCore154(s,content,facility,true,forgeRecipes).IsSuccess;
        public static Result<TownBuildingQuote153> QuoteBuilding(CampaignState s,GuildCityContent017D content,string facility,SecondDimension.Gameplay.Campaign022.ICampaignRegistry022 forgeRecipes = null)=>QuoteBuildingCore154(s,content,facility,false,forgeRecipes);
        static Result<TownBuildingQuote153> QuoteBuildingCore154(CampaignState s,GuildCityContent017D content,string facility,bool displayOnly,SecondDimension.Gameplay.Campaign022.ICampaignRegistry022 forgeRecipes = null)
        {
            var error=Safety(s);if(error!=null)return Result<TownBuildingQuote153>.Failure(error);
            int index=Array.IndexOf(FacilityIds,facility);
            if(index<0||content==null)return Result<TownBuildingQuote153>.Failure("This town facility is unavailable.");
            int before=TownProgression159.Level(s,index);
            if(before==int.MaxValue)return Result<TownBuildingQuote153>.Failure("This facility needs a wider level record before the next upgrade.");
            int next=before+1;
            var definition=index==0?null:content.Building(BuildingIds[index]);
            var plot=definition==null?null:s.Guild.GuildCity.CityPlots.FirstOrDefault(p=>p.BuildingId==definition.Id);
            if(plot==null&&definition!=null)plot=s.Guild.GuildCity.CityPlots.Where(p=>string.IsNullOrEmpty(p.BuildingId)&&p.DistrictId==definition.DistrictId).OrderByDescending(p=>p.Unlocked).FirstOrDefault();
            var plotId=plot?.PlotId??string.Empty;
            bool credit=index>0&&before==0&&s.Guild.GuildCity.CharterBuildCredits>0;
            long price=credit?0:TownProgression159.Cost(index,next);
            var costText=credit?"1 charter construction credit":price+" Treasury XP\nTreasury after upgrade: "+(price<=s.Guild.TreasuryXp?(s.Guild.TreasuryXp-price)+" XP":"not enough XP");
            string benefit=TownProgression159.Benefit(index,before,next);
            if(index==1&&before<3&&next>=3)benefit+="\nUnlocks Dorrik's weapon and armor commissions.";
            if(index==1)
            {
                if(MerchantStockProgression163.QualityFloor(before)!=MerchantStockProgression163.QualityFloor(next))
                    benefit+="\nMerchant equipment is now "+MerchantStockProgression163.QualityFloor(next).Substring(8).ToLowerInvariant()+" or better.";
                if(before<8&&next>=8)benefit+="\nSix offers per merchant, up from four.";
            }
            if(index==3&&before==0)benefit+="\nOpens six free Catch Up slots.";
            if(plot!=null&&!plot.Unlocked)benefit+="\nConstruction opens this town plot immediately.";
            if(index==2&&forgeRecipes!=null)
            {
                int currentNative=before;
                int oldCount=SecondDimension.Gameplay.Campaign022.ForgeNativeUnlock154.Count(forgeRecipes,currentNative);
                int newCount=SecondDimension.Gameplay.Campaign022.ForgeNativeUnlock154.Count(forgeRecipes,next);
                if(newCount>oldCount)benefit+="\nWeapon upgrade recipes: "+oldCount+" → "+newCount+".";
            }
            var result=Build(s,content,index,plotId);
            var revision=displayOnly?string.Empty:CanonicalJson.Sha256Hex(s);
            var request="TOWN_BUILD159_"+CanonicalJson.Sha256Hex(new{s.CampaignGuid,Revision=revision,Facility=facility,Plot=plotId,Level=next}).Substring(0,26).ToUpperInvariant();
            return Result<TownBuildingQuote153>.Success(new TownBuildingQuote153(facility,plotId,BuildingIds[index],revision,request,
                (before==0?"BUILD ":"UPGRADE ")+(definition?.DisplayName??"Guild Hall"),"Level "+before+" → "+next+"\n"+benefit,
                costText,result.IsSuccess,before==0,!displayOnly&&result.IsSuccess?CanonicalJson.Sha256Hex(result.Value):null));
        }
        public static Result<CampaignState> ConfirmBuilding(CampaignState s,GuildCityContent017D content,TownBuildingQuote153 q,SecondDimension.Gameplay.Campaign022.ICampaignRegistry022 forgeRecipes = null)
        {
            if(s==null||q==null)return Result<CampaignState>.Failure("Review this construction again.");
            // Quotes have no public constructor; recheck against current authority before any debit.
            if(s.Guild.Development.HasAdventureAuthority(q.RequestId))return Result<CampaignState>.Success(s);
            if(CanonicalJson.Sha256Hex(s)!=q.Revision)return Result<CampaignState>.Failure("Your town changed. Review construction again.");
            var read=QuoteBuilding(s,content,q.FacilityId,forgeRecipes);if(!read.IsSuccess)return Result<CampaignState>.Failure(read.Errors.ToArray());
            if(read.Value.RequestId!=q.RequestId||!read.Value.Affordable||read.Value.CandidateHash!=q.CandidateHash)return Result<CampaignState>.Failure("Construction is no longer available at that cost.");
            if(s.Guild.Development.AppliedAdventureAuthorityIds.Count>=GuildDevelopmentState.AdventureAuthorityEntryLimit)return Result<CampaignState>.Failure("The saved receipt history needs a capacity update.");
            var result=Build(s,content,Array.IndexOf(FacilityIds,q.FacilityId),q.PlotId);
            if(!result.IsSuccess)return result;var g=result.Value.Guild;
            return Result<CampaignState>.Success(result.Value.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,g.Development.RecordAdventureAuthority(q.RequestId)),s.OpeningFlow));
        }
    }
}
