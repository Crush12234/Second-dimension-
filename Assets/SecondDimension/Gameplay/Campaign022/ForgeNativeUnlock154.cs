using System.Linq;

namespace SecondDimension.Gameplay.Campaign022
{
    public static class ForgeNativeUnlock154
    {
        public static int Count(ICampaignRegistry022 registry,int level)
        {
            if(registry==null||level<1)return 0;
            return registry.WeaponRecipes.Values.Count(r=>r.forgeLevelRequired>0&&r.forgeLevelRequired<=level&&
                registry.WeaponTracks.TryGetValue(r.trackId,out var track)&&track.weaponFamilyId==r.weaponFamilyId&&
                WeaponEvolutionCombat154.TierIndex(r.fromTier)>=0&&
                WeaponEvolutionCombat154.TierIndex(r.toTier)>WeaponEvolutionCombat154.TierIndex(r.fromTier)&&
                WeaponEvolutionCombat154.TierIndex(r.toTier)<=6&&
                r.preserveInstanceId&&r.noAutoEquip&&r.exactOnce);
        }
    }
}
