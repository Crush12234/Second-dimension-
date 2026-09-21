using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.SSSTenV4
{
    // Settles earned/code ascension credits through the existing rank authority.
    // Run inside the caller's save transaction, after battle rewards are committed.
    public static class SssAutomaticRewards107
    {
        public static Result<CampaignState> ApplyReady(CampaignState campaign)
        {
            if (campaign?.Guild == null || campaign.Battle != null &&
                (campaign.Battle.Outcome == BattleOutcome.InProgress ||
                 campaign.Battle.Reward?.Claimed != true))
                return Result<CampaignState>.Success(campaign);

            var next = campaign;
            foreach (var hero in SssTenV4Roster090.All)
            {
                var recruit = SssTenV4Roster090.FindOwned(next.Guild.Recruits, hero.HeroId);
                if (recruit == null) continue;
                while (recruit.Progression.AscensionLevel < RecruitAscensionRules089.MaximumLevel)
                {
                    var credit = next.Guild.Inventory.FirstOrDefault(item =>
                        SssTenV4Inventory090.IsAscensionCreditForHero(item, hero));
                    if (credit == null) break;
                    var preview = SssTenV4HostRewards090.PreviewAscension(next, hero.HeroId);
                    var raised = SssTenV4HostRewards090.Ascend(next, hero.HeroId,
                        "AUTO_ASCEND107:" + credit.InstanceId, preview.CurrentRank,
                        preview.CreditCount, preview.StateGuard);
                    if (!raised.IsSuccess) return raised;
                    if (ReferenceEquals(next, raised.Value))
                        return Result<CampaignState>.Failure("SSS_AUTO_ASCENSION_RECEIPT_CONFLICT107");
                    next = raised.Value;
                    recruit = SssTenV4Roster090.FindOwned(next.Guild.Recruits, hero.HeroId);
                }
                // Existing heroes keep their exact loadouts. Signature rewards
                // remain in inventory until a player equipment command selects
                // them. Historical equipment receipts remain preserved in state.
            }
            return Result<CampaignState>.Success(next);
        }
    }
}
