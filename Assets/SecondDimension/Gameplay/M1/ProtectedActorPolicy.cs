using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M1
{
    /// <summary>
    /// Central M1 guard for canon actors. ProtectedSpecial remains a state hook,
    /// but M1 normal applicant/roster/equipment/Union commands never authorize it.
    /// </summary>
    public static class ProtectedActorPolicy
    {
        private static readonly HashSet<string> KaelIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "KAEL",
            "CHAR_KAEL",
            "RECRUIT_KAEL",
            "SIGREC_KAEL"
        };

        private static readonly HashSet<string> FounderIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "KIRI",
            "LUNA",
            "GROM",
            "SERAPHINA",
            "AIRA",
            "LYSSARA",
            "RIKA",
            "KIRI_AETHERHEART",
            "LUNA_SILVERSTEP",
            "GROM_IRONBLOOD",
            "SERAPHINA_NIGHTVEIL",
            "AIRA_MOONFANG",
            "LYSSARA_DUSKWOOD",
            "RIKA_STORMPAW"
        };

        public static RecruitAuthorityKind ClassifyStableId(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return RecruitAuthorityKind.Normal;
            if (KaelIds.Contains(stableId)) return RecruitAuthorityKind.Kael;
            if (FounderIds.Contains(stableId)) return RecruitAuthorityKind.Founder;
            return RecruitAuthorityKind.Normal;
        }

        public static bool CanEnterNormalApplicantOrRoster(
            string recruitId,
            string signatureId,
            RecruitAuthorityKind declaredAuthorityKind)
        {
            return declaredAuthorityKind == RecruitAuthorityKind.Normal &&
                   ClassifyStableId(recruitId) == RecruitAuthorityKind.Normal &&
                   ClassifyStableId(signatureId) == RecruitAuthorityKind.Normal;
        }

        public static bool CanUseNormalEquipment(RecruitState recruit) =>
            recruit != null &&
            CanEnterNormalApplicantOrRoster(recruit.RecruitId, recruit.SignatureId, recruit.AuthorityKind);

        public static bool CanEnterNormalUnion(RecruitState recruit) => CanUseNormalEquipment(recruit);
    }
}
