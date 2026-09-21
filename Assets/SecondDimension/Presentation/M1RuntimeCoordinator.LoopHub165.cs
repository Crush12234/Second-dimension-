using SecondDimension.Gameplay.Navigation164;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        private string UnionAddNotice165()
        {
            var city = _campaign?.Guild?.GuildCity;
            var parkedCampaign = _campaign?.Loops164?.Find(LoopCheckpoint164.Campaign);
            if (city?.Strategic017H?.Campaign019?.Playable020?.WorldGate023?.ActiveOperation != null ||
                parkedCampaign?.Campaign?.Playable020?.WorldGate023?.ActiveOperation != null)
                return "Union saved for your next adventure. Assign heroes to prepare it.";
            var expedition = city?.Expedition ?? parkedCampaign?.Expedition;
            if (expedition != null && expedition.Status == ExpeditionStatus017D.Active &&
                GuildCityExpeditionService017D.UsesBoardQuestRewards081(expedition.BoardId))
                return "Union saved for your next battle. Assign heroes to prepare it.";
            return UnionBattlePlanRules132.HasCommittedRoster(_campaign)
                ? "Union plan saved for future battles. Assign heroes to prepare it."
                : "Union created. Assign heroes to prepare it.";
        }

        public string LoopStatus165(string loop)
        {
            if (_campaign?.Guild == null) return "OPEN YOUR GUILD";
            if (loop == LoopCheckpoint164.Town)
                return "GUILD HALL  LV " + TownProgression159.Level(_campaign, 0);
            var current = CurrentLoop164 == loop;
            var saved = _campaign.Loops164?.Find(loop);
            var battle = current ? _campaign.Battle : saved?.Battle;
            if (battle != null && battle.Outcome == BattleOutcome.InProgress)
                return "BATTLE SAVED  ·  RESUME";
            if (battle?.Reward != null && !battle.Reward.Claimed)
                return "RESULT SAVED  ·  CLAIM";
            var expedition = current ? _campaign.Guild.GuildCity?.Expedition : saved?.Expedition;
            if (loop == LoopCheckpoint164.Campaign && expedition != null)
            {
                if (expedition.PendingQuestFate165 != null)
                    return expedition.PendingQuestFate165.IsRolled165 ? "FATE SAVED  ·  COLLECT" : "FATE SAVED  ·  REVEAL";
                if (expedition.Status == ExpeditionStatus017D.Active)
                    return "QUEST SAVED  ·  RESUME";
            }
            if (LoopCheckpoint164.HasParked(_campaign, loop)) return "PROGRESS SAVED  ·  RESUME";
            if (loop == LoopCheckpoint164.Titans && TownProgression159.Level(_campaign, 0) < 10)
                return "UNLOCK AT GUILD HALL 10";
            return current ? "YOU ARE HERE" : "READY TO EXPLORE";
        }
    }
}
