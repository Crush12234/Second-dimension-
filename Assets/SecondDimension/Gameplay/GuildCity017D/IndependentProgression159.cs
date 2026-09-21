using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    // Management does not occupy an adventure slot. It can change the live Guild
    // between decisions without abandoning the current Campaign/Tower checkpoint.
    // Starting another adventure continues to use HasAnyUnresolvedAdventure084.
    public static class IndependentProgression159
    {
        public static string BlockReason(CampaignState state)
        {
            var city = state?.Guild?.GuildCity;
            if (city == null) return "Open your Guild first.";
            if (state.TitanTrials160?.Active != null)
                return "Finish the saved Titan battle and claim its result before changing development.";
            var battle = state.Battle;
            if (battle != null && battle.Outcome == BattleOutcome.InProgress)
                return "Finish the current battle before spending XP or changing development.";
            if (battle != null && (battle.Reward == null || !battle.Reward.Claimed))
                return "Claim the saved battle result before changing development.";
            if (city.PendingEncounter != null || city.PendingBattleReturn != null ||
                city.Expedition?.Status == ExpeditionStatus017D.AwaitingBattle)
                return "Resolve the committed encounter and its return before changing development.";
            var strategic = city.Strategic017H;
            if (strategic?.ActiveDefense != null)
                return "Resolve the committed city defense before changing development.";
            var campaign = strategic?.Campaign019;
            var playable = campaign?.Playable020;
            var chapter = playable?.ActiveOperation;
            var world = playable?.WorldGate023?.ActiveOperation;
            var tower = playable?.Progression022?.ActiveAbyssOperation;
            if (campaign?.PendingReceipt != null || chapter?.PendingReceipt != null ||
                world?.PendingReceipt != null || world?.ExpeditionDeck089?.PendingReceipt != null ||
                tower?.PendingReceipt != null)
                return "Apply the current card or activity result before changing development.";
            if ((chapter != null && chapter.Status != CampaignPlayableOperationStatus020.Active) ||
                (world != null && world.Status != WorldGateOperationStatus023.Active) ||
                (tower != null && tower.Status != AbyssOperationStatus022.Active))
                return "Finish the current activity's saved result before changing development.";
            return null;
        }
    }
}
