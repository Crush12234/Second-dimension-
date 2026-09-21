using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
namespace SecondDimension.Presentation
{
    public sealed class TownView153
    {public long Treasury,RestockCost;public bool CanRestock;public string Status,StockSummary;public TownFacility153[] Facilities;public TownMerchant153[] Merchants;}
    public sealed class TownFacility153
    {public string Id,Name,ArtKey,Description,ServiceLabel,ServiceTab,UpgradeLabel;public int Level;public bool CanUse,CanUpgrade;}
    public sealed class TownMerchant153
    {public string Id,Name,Shop,Specialty165;public TownOffer153[] Offers;}
    public sealed class TownOffer153
    {public string Id,Name,Quality,Stats,Description,ArtKey,UnavailableReason,Kind165;public long Cost;public bool CanBuy;}
    public sealed partial class M1RuntimeCoordinator
    {
        public TownView153 ReadTown153()
        {
            if(_campaign?.Guild==null)return new TownView153{Status="Open your Guild first.",Facilities=Array.Empty<TownFacility153>(),Merchants=Array.Empty<TownMerchant153>()};
            var names=new[]{"Guild Hall","Merchant Row","Forge","Training Grounds","Recruitment Hall","Expedition Guild","Infirmary","Academy & Archive"};
            var descriptions=new[]{"Your Guild, its people and your next story quest. Town levels use shared Treasury XP; earned Guild XP remains separate.","Five specialist merchants offer equipment, heroes and luck supplies for Treasury XP. Restock whenever you choose, or receive fresh stock after completed operations.","Inspect your equipment and the existing mastery workbench.","Six free Catch Up slots. Five earned victories develop reserve heroes without removing them from your Unions.","Meet applicants and collect your earned hero invitations.","Continue your story or choose an existing Guild contract.","Assign recovering members and inspect your Guild's recovery facilities.","Develop hero classes and levels, then revisit your Chronicle. Hero development is always available."};
            var tabs=new[]{"HALL","MERCHANT153","PROGRESSION","CATCHUP153","APPLICANTS","CAMPAIGN","DUTIES","DEVELOPMENT"};
            var labels=new[]{"RETURN TO GUILD","VISIT MERCHANTS","OPEN WORKBENCH","FREE CATCH UP","MEET RECRUITS","STORY QUESTS","MEMBER RECOVERY","HERO DEVELOPMENT"};
            var facilities=new TownFacility153[8];
            for(int i=0;i<8;i++)
            {
                var level=TownProgression159.Level(_campaign,i);
                if(i==2)
                {
                    var nativeForge=_campaign.Guild.Development.Facilities.Where(f=>f.FacilityId=="FACILITY_GUILD_FORGE").Select(f=>f.Level).DefaultIfEmpty(0).Max();
                    labels[i]=nativeForge==0?"INSPECT EQUIPMENT":"OPEN WORKBENCH";
                    descriptions[i]=nativeForge==0?"Build the Forge to purchase weapon upgrades. Inspect equipment and earned mastery here.":"Use earned mastery and materials to improve a weapon's battle power.";
                }
                // Core loops are independent; free Catch Up is an optional facility benefit.
                var canUse=i!=3||level>0;
                if(!canUse)
                {
                    labels[i]="BUILD FIRST";
                    descriptions[i]=(i==1?"Build Merchant Row to open its shops. ":
                        "Build Training Grounds to open Catch Up courses. ")+descriptions[i];
                }
                descriptions[i]+="\n\n"+TownProgression159.Benefit(i,level,level);
                if(i==0)descriptions[i]+=level>=10?"\nTitan Trials are unlocked. Challenge twelve Titans and earn their matched heroes and personal Gold Arts.":"\nReach Guild Hall level 10 to unlock Titan Trials and their matched reward heroes.";
                var canUpgrade=TownService153.CanReviewBuilding154(_campaign,_guildCityContent,TownService153.FacilityIds[i],i==2?Registry022():null);
                facilities[i]=new TownFacility153{Id=TownService153.FacilityIds[i],Name=names[i],Level=level,
                    ArtKey="CITY_"+TownService153.FacilityIds[i]+"_LV"+Math.Min(10,Math.Max(0,level)).ToString("00"),Description=descriptions[i],
                    ServiceTab=tabs[i],ServiceLabel=labels[i],CanUse=canUse,UpgradeLabel=level==0?"REVIEW CONSTRUCTION":"REVIEW UPGRADE",CanUpgrade=canUpgrade};
            }
            if(facilities[1].Level<3)facilities[1].Description+=" Expand to level 3 to unlock Dorrik's weapon and armor commissions.";
            else facilities[1].Description+=" Dorrik's weapon and armor commissions are available.";
            facilities[1].Description+="\n\n"+MerchantStockProgression163.Summary(_campaign);
            var merchantNames=new[]{"Mera Brindle","Dorrik Brasshand","Tavi Quillstep","Sella Vey","Orven Glass"};
            var shops=new[]{"Hearthpack Provisions","Brasshand Outfitters","Wayfinder's Nook","Silver Oath Exchange","Lantern & Relic"};
            var merchants=new TownMerchant153[5];var revision=string.Empty;var safety=TownService153.Safety(_campaign);
            for(int m=0;m<5;m++)
            {
                var offers=new TownOffer153[MerchantStockProgression163.SlotsPerMerchant(_campaign)];for(int n=0;n<offers.Length;n++)
                {
                    // Preserve all existing 153 offer/receipt identities. At
                    // Market 3 Dorrik presents two commissioned outputs in slots 3/4;
                    // his previous quoted 153 offers still resolve through authority.
                    var commissioned=m==1&&(n==2||n==3)&&facilities[1].Level>=TownCommissions154.RequiredMarketLevel;
                    var id=commissioned?TownCommissions154.OfferId(_campaign,n==3):TownService153.OfferId165(_campaign,m,n);
                    var q=TownService153.ReadOffer(_campaign,id,revision,_guildCityRecruitment);var power=M2EquipmentPowerPolicy087.Resolve(q.Item);
                    var reason=TownService153.Sold(_campaign,id)?"SOLD":safety??q.UnavailableReason165??(q.Affordable?"":"Not enough Treasury XP.");
                    offers[n]=new TownOffer153{Id=id,Name=q.Name,Kind165=q.Kind165,Quality=q.Item?.QualityId.Replace("QUALITY_","")??"HERO",Cost=q.Cost,
                        Stats=q.Stats165??("Physical attack +"+power.PhysicalAttack+" · Mystic attack +"+power.MysticAttack),
                        Description=q.Description165??((commissioned?(n==3?"Armor Commission: ":"Weapon Commission: "):"")+"Adds this exact item to Inventory. Equip it deliberately through Equipment."),ArtKey=q.ArtKey165??("EQUIPMENT:"+EquipmentVisualId090(q.Item,q.Item.ValidSlotIds.FirstOrDefault())),
                        CanBuy=string.IsNullOrEmpty(reason),UnavailableReason=reason};
                }
                merchants[m]=new TownMerchant153{Id=(m+1).ToString("00"),Name=merchantNames[m],Shop=shops[m],Specialty165=new[]{"Provisions & luck","Weapons & armor","Accessories","Heroes & ascension","Magic weapons & relics"}[m],Offers=offers};
            }
            var restockCost=TownService153.RestockCost159(_campaign);
            return new TownView153{StockSummary=MerchantStockProgression163.Summary(_campaign),Treasury=_campaign.Guild.TreasuryXp,RestockCost=restockCost,CanRestock=safety==null&&restockCost<=_campaign.Guild.TreasuryXp,
                Status=safety??"Spend shared XP on town, merchants or heroes. Each grows independently.",Facilities=facilities,Merchants=merchants};
        }
        public Result<TownPurchaseQuote153> QuoteTownPurchase153(string offer)=>TowerWriteBusy116()?Result<TownPurchaseQuote153>.Failure(TowerBusy116):TownService153.QuotePurchase(_campaign,offer,_guildCityRecruitment);
        public Result<TownRestockQuote159> QuoteTownRestock159()=>TowerWriteBusy116()?Result<TownRestockQuote159>.Failure(TowerBusy116):TownService153.QuoteRestock159(_campaign);
        public M1CommandResult ConfirmTownRestock159(TownRestockQuote159 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var result=TownService153.ConfirmRestock159(_campaign,quote);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("This restock is already saved.");
            return ApplyAndPersist(result,true,"All five merchants restocked. Your adventure progress is retained.");
        }
        public M1CommandResult ConfirmTownPurchase153(TownPurchaseQuote153 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var result=TownService153.ConfirmPurchase(_campaign,quote,_guildCityRecruitment);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("This purchase is already saved.");
            return ApplyAndPersist(result,true,quote?.Kind165=="HERO"?"Hero purchase and native growth saved.":quote?.Kind165=="CONSUMABLE"?"Luck tonic saved in your Travel Pouch. Activate it when ready.":"Equipment purchased and saved in Inventory.");
        }
        public Result<TownBuildingQuote153> QuoteTownBuilding153(string id)=>TowerWriteBusy116()?Result<TownBuildingQuote153>.Failure(TowerBusy116):TownService153.QuoteBuilding(_campaign,_guildCityContent,id,id=="FORGE"?Registry022():null);
        public M1CommandResult ConfirmTownBuilding153(TownBuildingQuote153 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var result=TownService153.ConfirmBuilding(_campaign,_guildCityContent,quote,quote?.FacilityId=="FORGE"?Registry022():null);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("This construction is already saved.");
            return ApplyAndPersist(result,true,"Town construction and its native benefit saved.");
        }
    }
}
