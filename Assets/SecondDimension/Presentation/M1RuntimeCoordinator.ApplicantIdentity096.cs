using System;
using System.Linq;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        HeroMaster300Hero087 AuthoredApplicantHero096(ApplicantSnapshotState applicant)
        {
            if (applicant == null || !TryHeroMaster300CreatorRegistry087(out var registry))
                return null;
            return registry.Source.TryGetAcceptedHero(applicant.AuthoredStableRecruitId, out var hero)
                ? hero : null;
        }

        string ApplicantGearSummary096(ApplicantSnapshotState applicant, string ownedRecruitId,
            bool duplicate)
        {
            // A duplicate copy merges progression; it does not mint another kit.
            if (duplicate) return "Existing gear kept; no extra kit";
            var owned = _campaign.Guild.Recruits.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, ownedRecruitId));
            if (owned != null)
                return owned.Equipment.Assignments.Count == 0 ? "No gear equipped" :
                    EquipmentSummary(owned.Equipment.Assignments.Select(value => value.Item).ToArray());
            if (applicant.OpeningEquipment.Count > 0)
                return EquipmentSummary(applicant.OpeningEquipment);
            if (_starterEquipment094 != null && applicant.OpeningLoadout.Assignments.Count == 0)
                return "Inventory upgrades or basic gear + armor";
            return "No gear included";
        }
    }
}
