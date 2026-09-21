using System;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
namespace SecondDimension.Presentation
{
    public sealed class TownConsumable165 { public string InstanceId,Name,Description; public bool CanUse; }
    public sealed class TownConsumablesView165 { public string Revision,Status; public bool HasActiveLuck; public TownConsumable165[] Items; }
    public sealed partial class M1RuntimeCoordinator
    {
        public TownConsumablesView165 ReadTownConsumables165()
        {
            if(_campaign?.Guild==null)return new TownConsumablesView165{Revision="",Status="Open a Guild first.",Items=Array.Empty<TownConsumable165>()};
            var active=TownLuckConsumables165.ActiveChargeId(_campaign).Length>0;var safe=TownService153.Safety(_campaign);
            return new TownConsumablesView165{Revision=CanonicalJson.Sha256Hex(_campaign),HasActiveLuck=active,
                Status=active?"Luck ready: your next unsealed blessing rolls 2D20 and keeps the higher. One charge is consumed when you choose that blessing.":"Activate one tonic before choosing a blessing. Saved or already sealed results never change.",
                Items=_campaign.Guild.Inventory.Where(TownLuckConsumables165.IsLuckItem).Select(i=>new TownConsumable165{
                    InstanceId=i.InstanceId,Name=i.DisplayName,CanUse=!active&&safe==null,
                    Description="One use. Next blessing: roll two D20s, keep the higher. Natural20 chance increases from 5% to 9.75%."}).ToArray()};
        }
        public M1CommandResult UseTownConsumable165(string instanceId,string expectedRevision)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var result=TownLuckConsumables165.Activate(_campaign,instanceId,expectedRevision);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("This tonic was already activated.");
            return ApplyAndPersist(result,true,"Luck is ready for your next unsealed blessing. Existing saved rolls are unchanged.");
        }
    }
}
