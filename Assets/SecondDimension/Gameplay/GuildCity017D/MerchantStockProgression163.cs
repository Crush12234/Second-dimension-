using System;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    // The R-series market milestones remain 1/3/5/8/10. Existing equipment has
    // four merchant quality bands, so the fourth milestone expands choice and
    // the fifth guarantees the existing Godly band. Owned items never change.
    public static class MerchantStockProgression163
    {
        public static int StockTier(int level)
        {
            if(level<0)throw new ArgumentOutOfRangeException(nameof(level));
            return level>=10?5:level>=8?4:level>=5?3:level>=3?2:1;
        }
        public static string QualityFloor(int level)
        {
            if(level<0)throw new ArgumentOutOfRangeException(nameof(level));
            return level>=10?"QUALITY_GODLY":level>=5?"QUALITY_LEGENDARY":level>=3?"QUALITY_EPIC":"QUALITY_RARE";
        }
        public static int SlotsPerMerchant(CampaignState state)=>TownProgression159.Level(state,1)>=8?6:4;
        public static string Summary(CampaignState state)
        {
            int level=TownProgression159.Level(state,1);
            string quality=QualityFloor(level).Substring(8).ToLowerInvariant();
            return "Stock tier "+StockTier(level)+" · Equipment "+char.ToUpperInvariant(quality[0])+quality.Substring(1)+
                " or better · "+SlotsPerMerchant(state)+" offers per merchant";
        }
        public static EquipmentItemState Apply(CampaignState state,EquipmentItemState item)
        {
            if(state?.Guild==null||item==null)throw new ArgumentNullException();
            string floor=QualityFloor(TownProgression159.Level(state,1));
            if(WeaponEvolutionCombat154.TierIndex(item.QualityId)>=WeaponEvolutionCombat154.TierIndex(floor))return item;
            string quality=floor.Substring(8).ToLowerInvariant();quality=char.ToUpperInvariant(quality[0])+quality.Substring(1);
            int split=item.DisplayName.IndexOf(' ');
            string name=quality+(split<0?" "+item.DisplayName:item.DisplayName.Substring(split));
            return new EquipmentItemState(item.InstanceId,item.DefinitionId,name,item.ValidSlotIds,item.EquipmentTags,floor,
                item.ConditionBasisPoints,item.PlayerLocked,item.InventoryOnly);
        }
    }
}
