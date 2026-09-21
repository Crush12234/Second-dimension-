using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        // One immutable campaign snapshot only. This cache is never consulted by
        // commands, reward application, save validation, or reload validation.
        TowerAuthorityProjection110 _towerAuthorityProjection110;

        sealed class TowerAuthorityProjection110
        {
            public readonly CampaignState Campaign;
            public readonly CampaignRegistry022 Registry;
            public readonly GuildCityRecruitmentService017D Recruitment;
            public readonly bool GreatCovenantGateEarned;
            public readonly bool BattleMatchesTower;
            public readonly Result<TowerFloorProgress094> Floors;
            public readonly Result<TowerHeroRewardResult094> LastHeroReward;

            public TowerAuthorityProjection110(CampaignState campaign,
                CampaignRegistry022 registry,GuildCityRecruitmentService017D recruitment,
                bool greatCovenantGateEarned,bool battleMatchesTower,
                Result<TowerFloorProgress094> floors,
                Result<TowerHeroRewardResult094> lastHeroReward)
            {
                Campaign=campaign;Registry=registry;Recruitment=recruitment;
                GreatCovenantGateEarned=greatCovenantGateEarned;
                BattleMatchesTower=battleMatchesTower;Floors=floors;
                LastHeroReward=lastHeroReward;
            }
        }

        TowerAuthorityProjection110 ReadTowerAuthorityProjection110(
            CampaignRegistry022 registry,bool legacyRecoveryRequired)
        {
            var cached=_towerAuthorityProjection110;
            if(cached!=null&&ReferenceEquals(cached.Campaign,_campaign)&&
               ReferenceEquals(cached.Registry,registry)&&
               ReferenceEquals(cached.Recruitment,_guildCityRecruitment))return cached;

            var campaign=_campaign;
            var gate=_campaignCommands022.HasEarnedGreatCovenantGate(campaign,registry);
            var floors=CampaignProgressionCommandService022.DescribeTowerFloors094(
                campaign,registry);
            var battleMatches=_campaignCommands022.HasMatchingActiveAbyssBattle(
                campaign,registry);
            var hero=Result<TowerHeroRewardResult094>.Success(null);
            if(!legacyRecoveryRequired&&floors.IsSuccess&&_guildCityRecruitment!=null)
                hero=_guildCityRecruitment.DescribeLatestTowerHeroReward094(
                    campaign,registry);

            // Publish only after every read finishes. Failed authority results
            // remain failed; public presentation objects never expose these DTOs.
            return _towerAuthorityProjection110=new TowerAuthorityProjection110(
                campaign,registry,_guildCityRecruitment,gate,battleMatches,floors,hero);
        }
    }
}
