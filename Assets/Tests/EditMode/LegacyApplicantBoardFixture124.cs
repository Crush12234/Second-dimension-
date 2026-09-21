using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    // Historical-fixture construction only. This preserves the pre-124 board
    // payload used by signing/progression tests; production must never call it.
    // Eligibility still comes from the existing world/story projection. Nothing
    // here grants a campaign reward or changes the current recurring-board rules.
    internal static class LegacyApplicantBoardFixture124
    {
        internal static Result<CampaignState> Commit(CampaignState campaign,
            RecruitmentContent content, GuildCityContent017D cityContent)
        {
            if (campaign.Guild.GuildCity.RecruitmentBoard != null)
                return Result<CampaignState>.Success(campaign);
            var effects = new GuildCityEffectService017D();
            var city = campaign.Guild.GuildCity;
            var access = typeof(GuildCityRecruitmentService017D).GetMethod(
                "RecruitmentAccess017D", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { campaign, content });
            IReadOnlyList<string> Read(string property) =>
                (IReadOnlyList<string>)access.GetType().GetProperty(property).GetValue(access);
            var signatures = campaign.Guild.Recruits.Where(value =>
                value.OriginKind == RecruitOriginKind.Signature && !string.IsNullOrWhiteSpace(value.SignatureId))
                .Select(value => value.SignatureId).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var request = new ApplicantBoardRequest {
                CampaignSeed = campaign.CampaignSeed, GuildDay = city.OperationOrdinal + 1,
                RefreshIndex = city.RecruitmentRefreshOrdinal,
                OfficeTier = Math.Max(0, Math.Min(6, effects.FacilityLevel(campaign.Guild.Development, "FACILITY_RECRUITMENT_OFFICE"))),
                DryStreak = city.RecruitmentDryStreak, UnlockedWorlds = Read("WorldIds"),
                UnlockedRaces = Read("RaceIds"), EligibilityFlags = Read("EligibilityFlags"),
                GuildRank = campaign.Guild.Development.GuildLevel,
                RecruitedSignatureIds = signatures, SeenSignatureIds = signatures,
                DeclinedSignatureUntilDay = new Dictionary<string, int>(StringComparer.Ordinal),
                EventBonusBasisPoints = 0,
                ScoutSkill = 50 + effects.ScoutInformationBonus(campaign.Guild.Development, city, cityContent),
                TargetedSearch = null, ForcedApplicants = Array.Empty<ForcedApplicantSpec>(),
                BoardSizeOverride = Math.Max(6, Math.Min(10, 6 + effects.ApplicantBoardBonusSlots(campaign.Guild.Development, city, cityContent))) };
            var detailed = new DetailedApplicantBoardGenerator(content).Generate(request);
            var board = ApplicantBoardStateAdapter.ToM1State(detailed,
                "GC017D_RECRUITMENT_OP_" + city.OperationOrdinal.ToString("D4") +
                "_R_" + city.RecruitmentRefreshOrdinal.ToString("D4"));
            city = city.With(recruitmentBoard: board, replaceRecruitmentBoard: true,
                recruitmentDryStreak: detailed.DryStreakAfter, lastCheckpointId: "recruitment_board_committed");
            return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow));
        }
    }
}
