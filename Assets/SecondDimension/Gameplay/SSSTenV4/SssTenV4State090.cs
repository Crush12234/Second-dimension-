using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    [Serializable]
    public sealed class SssPreparedFamilyLoadout090
    {
        [JsonConstructor]
        public SssPreparedFamilyLoadout090(string heroId, IEnumerable<string> familyIds)
        {
            HeroId = SssHeroes.CanonicalId(heroId);
            if (!SssHeroes.IsSss(HeroId))
            {
                throw new ArgumentException("Prepared-family owner must be an SSS hero.", nameof(heroId));
            }

            FamilyIds = (familyIds ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        public string HeroId { get; }
        public string[] FamilyIds { get; }
    }

    /// <summary>
    /// Optional SSS Ten V4 state embedded in the existing CampaignState save authority.
    /// Recruit ascension rank intentionally remains authoritative in RecruitProgressionState.
    /// </summary>
    [Serializable]
    public sealed class SssTenV4State090
    {
        [JsonConstructor]
        public SssTenV4State090(
            SssSave progression = null,
            SssHuntSave weaponHunt = null,
            IEnumerable<SssPreparedFamilyLoadout090> preparedFamilies = null,
            IEnumerable<string> specialUseReceiptIds = null)
        {
            Progression = progression?.Copy() ?? new SssSave();
            WeaponHunt = weaponHunt?.Copy() ?? new SssHuntSave();
            PreparedFamilies = (preparedFamilies ?? Array.Empty<SssPreparedFamilyLoadout090>())
                .Where(x => x != null)
                .Select(x => new SssPreparedFamilyLoadout090(x.HeroId, x.FamilyIds))
                .OrderBy(x => x.HeroId, StringComparer.Ordinal)
                .ToArray();
            SpecialUseReceiptIds = (specialUseReceiptIds ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            SssProgression.Validate(Progression);
            SssWeaponHunt.Validate(WeaponHunt);
        }

        public SssSave Progression { get; }
        public SssHuntSave WeaponHunt { get; }
        public SssPreparedFamilyLoadout090[] PreparedFamilies { get; }
        public string[] SpecialUseReceiptIds { get; }

        public static SssTenV4State090 Default() => new SssTenV4State090();

        public SssTenV4State090 With(
            SssSave progression = null,
            SssHuntSave weaponHunt = null,
            IEnumerable<SssPreparedFamilyLoadout090> preparedFamilies = null,
            IEnumerable<string> specialUseReceiptIds = null) =>
            new SssTenV4State090(
                progression ?? Progression,
                weaponHunt ?? WeaponHunt,
                preparedFamilies ?? PreparedFamilies,
                specialUseReceiptIds ?? SpecialUseReceiptIds);
    }

    public static class SssTenV4CampaignAccessor090
    {
        public static SssTenV4State090 Read(CampaignState campaign)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            return campaign.SssV4090 ?? SssTenV4State090.Default();
        }

        public static CampaignState WithState(CampaignState campaign, SssTenV4State090 state)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            return campaign.WithSssV4090(state ?? throw new ArgumentNullException(nameof(state)));
        }
    }
}
