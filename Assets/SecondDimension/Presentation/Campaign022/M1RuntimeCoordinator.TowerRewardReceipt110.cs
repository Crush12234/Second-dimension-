using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation.Campaign022
{
    public interface ITowerSavedRewardReader110
    {
        TowerSavedReward110 ReadTowerSavedReward110(string completedBattleId, string nextBattleId);
    }

    // Presentation-only immutable snapshot. It is exposed only after a durable
    // transaction and never used for eligibility, reward application or saving.
    public sealed class TowerSavedReward110
    {
        public int Floor { get; }
        public int GuildXp { get; }
        public int HallXp { get; }
        public IReadOnlyList<string> MaterialIds { get; }
        public string HeroSummary { get; }
        internal TowerSavedReward110(int floor,int guildXp,int hallXp,
            IEnumerable<string> materials,string heroSummary)
        {
            Floor=floor;GuildXp=guildXp;HallXp=hallXp;
            MaterialIds=Array.AsReadOnly((materials??Array.Empty<string>()).ToArray());
            HeroSummary=heroSummary??string.Empty;
        }
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign022.ITowerSavedRewardReader110
    {
        CampaignState _towerRewardSavedCampaign110;
        string _towerRewardCompletedBattle110;
        string _towerRewardNextBattle110;
        Campaign022.TowerSavedReward110 _towerSavedReward110;

        public Campaign022.TowerSavedReward110 ReadTowerSavedReward110(
            string completedBattleId,string nextBattleId)
            =>ReferenceEquals(_campaign,_towerRewardSavedCampaign110)&&
                StringComparer.Ordinal.Equals(completedBattleId,_towerRewardCompletedBattle110)&&
                StringComparer.Ordinal.Equals(nextBattleId,_towerRewardNextBattle110)
                ?_towerSavedReward110:null;

        Campaign022.TowerSavedReward110 PrepareTowerRewardDisplay110(
            CampaignState before,CampaignState banked,ICampaignRegistry022 registry)
        {
            var active=ProgressionState081(before).ActiveAbyssOperation;
            if(active==null||!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var definition))
                return null;
            // The same floor authority used by the full projection still runs.
            // Unrelated covenant, next-floor and roster projections are omitted.
            var floors=CampaignProgressionCommandService022.DescribeTowerFloors094(banked,registry);
            if(!floors.IsSuccess)return null;
            var floor=floors.Value.LatestCompletedActualFloor130;
            var summary=string.Empty;
            if(TowerHeroRewardRules094.IsRewardFloor(floor)&&_guildCityRecruitment!=null)
            {
                var hero=_guildCityRecruitment.DescribeLatestTowerHeroReward094(banked,registry);
                if(hero.IsSuccess&&hero.Value?.ActualFloor==floor)
                    summary=TowerSavedHeroRewardCopy094(hero.Value);
            }
            return new Campaign022.TowerSavedReward110(floor,definition.guildXp,definition.hallXp,
                definition.rewardMaterialIds,summary);
        }

        void PublishTowerRewardDisplay110(string completedBattleId,Campaign022.TowerSavedReward110 reward)
        {
            _towerRewardSavedCampaign110=_campaign;
            _towerRewardCompletedBattle110=completedBattleId;
            _towerRewardNextBattle110=_campaign?.Battle?.BattleId;
            _towerSavedReward110=reward;
        }
    }
}
