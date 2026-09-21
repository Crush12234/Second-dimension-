using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.GuildCity017D
{
    public static class TownLuckConsumables165
    {
        public const string DefinitionId="TOWN165_WAYGLASS_LUCK_TONIC";
        const string ActivePrefix="TOWN_LUCK165_ACTIVE_";
        const string UsedPrefix="TOWN_LUCK165_USED_";
        public static bool IsLuckItem(EquipmentItemState item)=>item!=null&&item.InventoryOnly&&item.DefinitionId==DefinitionId&&
            item.InstanceId.StartsWith("TOWN165_SUPPLY_",StringComparison.Ordinal)&&item.EquipmentTags.Contains("LUCK_TONIC165");
        public static string ActiveChargeId(CampaignState state)
        {
            var rows=state?.Guild?.Development?.AppliedAdventureAuthorityIds??Array.Empty<string>();
            var active=rows.Where(x=>x.StartsWith(ActivePrefix,StringComparison.Ordinal)).Select(x=>x.Substring(ActivePrefix.Length))
                .Where(id=>!rows.Any(r=>r.StartsWith(UsedPrefix+id+"|",StringComparison.Ordinal))).ToArray();
            if(active.Length>1)throw new InvalidOperationException("Only one active luck charge is permitted.");
            return active.Length==0?string.Empty:active[0];
        }
        public static bool WasConsumedFor(CampaignState state,string chargeId,string fateReceiptId)=>
            !string.IsNullOrEmpty(chargeId)&&!string.IsNullOrEmpty(fateReceiptId)&&
            state.Guild.Development.HasAdventureAuthority(ActivePrefix+chargeId)&&
            state.Guild.Development.HasAdventureAuthority(UsedPrefix+chargeId+"|"+fateReceiptId);
        public static CampaignState ConsumeForSeal(CampaignState state,string chargeId,string fateReceiptId)
        {
            if(WasConsumedFor(state,chargeId,fateReceiptId))return state;
            if(string.IsNullOrWhiteSpace(fateReceiptId)||string.IsNullOrWhiteSpace(chargeId)||ActiveChargeId(state)!=chargeId)
                throw new InvalidOperationException("A matching active luck charge is required before sealing.");
            var id=UsedPrefix+chargeId+"|"+fateReceiptId;var g=state.Guild;
            if(!g.Development.CanRecordAdventureAuthority(id))throw new InvalidOperationException("Luck receipt history is full.");
            return state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,g.Development.RecordAdventureAuthority(id)),state.OpeningFlow);
        }
        public static Result<CampaignState> Activate(CampaignState state,string instanceId,string expectedRevision)
        {
            var error=TownService153.Safety(state);if(error!=null)return Result<CampaignState>.Failure(error);
            var activeId=CanonicalJson.Sha256Hex(new{state.CampaignGuid,Item=instanceId});
            if(state.Guild.Development.HasAdventureAuthority(ActivePrefix+activeId))return Result<CampaignState>.Success(state);
            if(expectedRevision!=CanonicalJson.Sha256Hex(state))return Result<CampaignState>.Failure("Your Guild changed. Review this tonic again.");
            if(ActiveChargeId(state).Length>0)return Result<CampaignState>.Failure("A luck tonic is already active for your next blessing.");
            var item=state.Guild.Inventory.SingleOrDefault(x=>x.InstanceId==instanceId);
            if(!IsLuckItem(item))return Result<CampaignState>.Failure("This luck tonic is no longer in your inventory.");
            var g=state.Guild;var receipt=ActivePrefix+activeId;
            if(!g.Development.CanRecordAdventureAuthority(receipt))return Result<CampaignState>.Failure("Luck receipt history is full.");
            return Result<CampaignState>.Success(state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,
                g.Inventory.Where(x=>x.InstanceId!=instanceId).ToArray(),g.Development.RecordAdventureAuthority(receipt)),state.OpeningFlow));
        }
    }
}
