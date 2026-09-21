using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Pure adapter from one trusted, earned Hero Master lead into the existing
    /// immutable Applicant Board snapshot. Recruitment and signing remain owned by
    /// GuildCityRecruitmentService017D and Auto-Generation 010.
    /// </summary>
    public static class HeroMaster300ApplicantLead089
    {
        public static ApplicantSnapshotState ToApplicant(
            HeroMaster300Hero087 hero,
            int slot,
            string worldId)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            var record = HeroMaster300CreatorRecruitProjection087
                .BuildExpeditionApplicantRecord089(hero, worldId);
            var scouting = ScoutingReportFor(record, hero);
            var tactical = record.DisciplineAptitudes.TryGetValue(
                "TACTICAL", out var tacticalAptitude)
                ? tacticalAptitude
                : 0;

            return new ApplicantSnapshotState(
                slot,
                record.RecruitId,
                record.DisplayName,
                ApplicantKind.Procedural,
                record.GenerationSeed,
                string.Empty,
                record.RaceId,
                record.HomeCommunityId,
                record.StartingClassId,
                LeadershipBand(record.LeadershipScore),
                hero.Hp,
                hero.Hp,
                hero.Ap,
                hero.Ap,
                true,
                record.SigningCostXp,
                record.DevelopmentPotentialScore,
                CanonicalJson.Serialize(record),
                CanonicalJson.Serialize(scouting),
                Array.Empty<EquipmentItemState>(),
                string.Empty,
                hero.StableId,
                EquipmentLoadoutState.Empty(),
                record.LeadershipScore,
                tactical);
        }

        public static bool RosterContains(
            IEnumerable<RecruitState> roster,
            HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (roster == null) return false;
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return roster.Any(recruit => recruit != null &&
                (StringComparer.Ordinal.Equals(recruit.RecruitId, projectedId) ||
                 StringComparer.Ordinal.Equals(recruit.RecruitId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(recruit.RecruitId, hero.GameEntityId) ||
                 StringComparer.Ordinal.Equals(recruit.AuthoredStableRecruitId,
                     hero.StableId) ||
                 StringComparer.Ordinal.Equals(recruit.SignatureId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(recruit.SignatureId, hero.GameEntityId)));
        }

        public static bool BoardContains(
            ApplicantBoardState board,
            HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (board == null) return false;
            return ApplicantsContain(board.Applicants, hero);
        }

        public static bool ApplicantsContain(
            IEnumerable<ApplicantSnapshotState> applicants,
            HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (applicants == null) return false;
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return applicants.Any(applicant => applicant != null &&
                (StringComparer.Ordinal.Equals(applicant.RecruitId, projectedId) ||
                 StringComparer.Ordinal.Equals(applicant.RecruitId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(applicant.RecruitId, hero.GameEntityId) ||
                 StringComparer.Ordinal.Equals(applicant.AuthoredStableRecruitId,
                     hero.StableId) ||
                 StringComparer.Ordinal.Equals(applicant.SignatureId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(applicant.SignatureId,
                     hero.GameEntityId)));
        }

        private static ScoutingReport ScoutingReportFor(
            OpeningRecruitRecord record,
            HeroMaster300Hero087 hero)
        {
            var estimates = new Dictionary<string, AptitudeEstimate>(
                StringComparer.Ordinal);
            foreach (var pair in record.DisciplineAptitudes
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                estimates[pair.Key] = new AptitudeEstimate
                {
                    EstimatedRange = Array.AsReadOnly(new[]
                    {
                        Math.Max(0, pair.Value - 5),
                        Math.Min(100, pair.Value + 5)
                    }),
                    Label = pair.Value >= 90 ? "Exceptional" :
                        pair.Value >= 75 ? "Strong" : "Developing"
                };
            }

            return new ScoutingReport
            {
                RecruitId = record.RecruitId,
                DisplayName = record.DisplayName,
                RaceId = record.RaceId,
                HomeCommunityId = record.HomeCommunityId,
                StartingClassId = record.StartingClassId,
                BackgroundId = record.BackgroundId,
                VisibleTraitIds = record.VisibleTraitIds,
                LeadershipEstimate = new ValueRange
                {
                    Range = Array.AsReadOnly(new[]
                    {
                        Math.Max(0, record.LeadershipScore - 5),
                        Math.Min(100, record.LeadershipScore + 5)
                    }),
                    Confidence = 90
                },
                DisciplineEstimates = estimates,
                GrowthAssessment = "A named expedition contact with a proven field record.",
                QualityProfile = "HERO MASTER " + hero.Rank.ToString().ToUpperInvariant(),
                ExactPotentialDisplayed = false,
                HiddenTraitRevealed = false,
                HiddenDataWithheld = true,
                ScoutingAccuracy = 90,
                HiddenTraitId = string.Empty
            };
        }

        private static string LeadershipBand(int value) =>
            value >= 85 ? "EXCEPTIONAL" : value >= 70 ? "STRONG" :
            value >= 55 ? "STEADY" : "DEVELOPING";
    }
}
