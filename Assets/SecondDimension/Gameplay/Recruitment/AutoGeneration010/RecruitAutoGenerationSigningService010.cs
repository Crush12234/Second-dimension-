using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment.AutoGeneration010
{
    /// <summary>
    /// Safe wrapper that signs through the existing M1 command and then initializes
    /// all starting Arts from the deterministic generated profile. It never changes
    /// equipment ownership and never enables auto-equip.
    /// </summary>
    public sealed class RecruitAutoGenerationSigningService010
    {
        private readonly M1CommandService _commands;
        private readonly RecruitAutoGenerator010 _generator;

        public RecruitAutoGenerationSigningService010(M1CommandService commands, RecruitAutoGenerator010 generator)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        public Result<CampaignState> SignApplicant(CampaignState state, string recruitId)
        {
            var signed = _commands.SignApplicant(state, recruitId);
            if (!signed.IsSuccess) return signed;
            var campaign = signed.Value;
            var recruits = new List<RecruitState>(campaign.Guild.Recruits);
            for (var index = 0; index < recruits.Count; index++)
            {
                if (!StringComparer.Ordinal.Equals(recruits[index].RecruitId, recruitId)) continue;
                recruits[index] = InitializeRecruit(recruits[index]);
                var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, recruits, campaign.Guild.Unions, campaign.Guild.Inventory);
                return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
            }
            return Result<CampaignState>.Failure("AUTOGEN010_SIGNED_RECRUIT_NOT_FOUND");
        }

        public RecruitState InitializeRecruit(RecruitState recruit)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            if (recruit.AuthorityKind != RecruitAuthorityKind.Normal)
                throw new InvalidOperationException("Protected actors cannot use normal recruit auto-generation.");
            if (recruit.Progression != null && recruit.Progression.LearnedArtIds.Count > 0) return recruit;
            var profile = _generator.Generate(recruit);
            var mastery = new List<RecruitArtMasteryState>();
            foreach (var artId in profile.StartingLearnedArtIds)
            {
                var discipline = profile.ArtDisciplineById.TryGetValue(artId, out var value) ? value : "UNKNOWN";
                mastery.Add(new RecruitArtMasteryState(artId, discipline, 0, 0));
            }
            var progression = new RecruitProgressionState(
                1, 0, 0, 0, 0, 0, 0, 0, 0,
                profile.StartingLearnedArtIds,
                mastery,
                StartingTreeIds(profile));
            return recruit.WithProgression(progression);
        }

        private static IReadOnlyList<string> StartingTreeIds(GeneratedRecruitProfile010 profile)
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(profile.WeaponTreeId))
                result.Add(profile.WeaponTreeId);
            if (!string.IsNullOrWhiteSpace(profile.PrimaryRoleTreeId) &&
                !result.Contains(profile.PrimaryRoleTreeId))
                result.Add(profile.PrimaryRoleTreeId);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }
    }
}
