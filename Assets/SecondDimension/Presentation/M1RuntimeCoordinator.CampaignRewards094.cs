using System.Collections.Generic;
using SecondDimension.Gameplay.GuildCity017D;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        public IReadOnlyDictionary<string, string> ClaimedEarnedCardDuplicates099 =>
            GuildCityRecruitmentService017D.ClaimedCardDuplicateHeroIds099(_campaign?.Guild?.Development);

        public EarnedCampaignRecruitsView094 EarnedCampaignRecruits094
        {
            get
            {
                Registry019(); Registry023();
                return _guildCityRecruitment?.DescribeEarnedCampaignRecruits094(
                    _campaign, _campaignRules019, _campaignRules023)
                    ?? new EarnedCampaignRecruitsView094();
            }
        }

        public M1CommandResult ClaimEarnedCampaignRecruits094()
        {
            Registry019(); Registry023();
            if (_guildCityRecruitment == null)
                return M1CommandResult.Failure("Recruitment is not ready.");
            var boundary = ReleaseIdleTowerForGuildProgression107(_campaign);
            if (!boundary.IsSuccess)
                return M1CommandResult.Failure(FriendlyErrors(boundary.Errors));
            var candidate = boundary.Value;
            var before = candidate.Guild.Recruits.Count;
            var duplicateClaimsBefore = GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(candidate.Guild.Development);
            var claimed = _guildCityRecruitment.ClaimEarnedCampaignRecruits094(
                candidate, _campaignRules019, _campaignRules023);
            var joined = claimed.IsSuccess ? claimed.Value.Guild.Recruits.Count - before : 0;
            var duplicates = claimed.IsSuccess
                ? GuildCityRecruitmentService017D.ClaimedCardDuplicateCount099(claimed.Value.Guild.Development) - duplicateClaimsBefore : 0;
            return ApplyAndPersist(claimed, true, EarnedRecruitFeedback099.ClaimSummary099(joined, duplicates));
        }
    }
}
