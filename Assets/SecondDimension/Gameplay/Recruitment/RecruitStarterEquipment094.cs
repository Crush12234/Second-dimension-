using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// A new-recruit grant policy, not a load-time migration or a second equipment
    /// engine. Blank loadouts receive basic family gear; compatible owned upgrades
    /// take priority. The existing EquipItem command owns every assignment.
    /// </summary>
    public sealed class RecruitStarterEquipment094
    {
        readonly RecruitAutoGenerationCatalog010 _catalog;
        readonly RecruitAutoGenerator010 _generator;
        readonly M2CombatContent _combat;

        public RecruitStarterEquipment094(RecruitAutoGenerationCatalog010 catalog,
            M2CombatContent combat)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _generator = new RecruitAutoGenerator010(catalog);
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        public CampaignState ApplyToNewRecruits(CampaignState before, CampaignState candidate)
        {
            if (before?.Guild == null || candidate?.Guild == null ||
                before.CampaignGuid != candidate.CampaignGuid) return candidate;
            var oldIds = new HashSet<string>(before.Guild.Recruits.Select(value => value.RecruitId),
                StringComparer.Ordinal);
            var arrivals = candidate.Guild.Recruits.Where(value => !oldIds.Contains(value.RecruitId) &&
                    ProtectedActorPolicy.CanUseNormalEquipment(value) &&
                    value.Equipment.Assignments.Count == 0)
                .OrderBy(value => value.RecruitId, StringComparer.Ordinal).ToArray();
            if (arrivals.Length == 0) return candidate;
            if (candidate.Battle?.Outcome == BattleOutcome.InProgress)
                throw new InvalidOperationException("Finish the battle before outfitting new recruits.");
            // Existing proof-only callers and legacy empty campaigns have no M1
            // equipment context. The live coordinator always supplies both.
            if (candidate.Profile == null || candidate.OpeningFlow == null)
                throw new InvalidOperationException("New-recruit equipment requires a live campaign context.");

            foreach (var arrival in arrivals)
            {
                var familyId = ResolveFamily(arrival);
                var family = _catalog.Family(familyId);
                var tags = ((JArray)family["equipmentTagsGranted"]).Values<string>().ToArray();
                var slots = ((JArray)family["validSlots"]).Values<string>().ToArray();
                var weaponSlot = familyId == "WEAPON_FAMILY_SHIELD" && slots.Contains(EquipmentSlotIds.OffHand)
                    ? EquipmentSlotIds.OffHand : slots.Contains(EquipmentSlotIds.MainHand)
                        ? EquipmentSlotIds.MainHand : slots[0];
                var weapon = Basic(candidate, arrival, weaponSlot, familyId,
                    "Basic " + FamilyName(familyId), tags.Concat(new[] { "WEAPON", familyId }).ToArray());
                candidate = EquipBest(candidate, arrival.RecruitId, weaponSlot, weapon, tags);
                var armor = Basic(candidate, arrival, EquipmentSlotIds.BodyArmor,
                    "RECRUIT_TRAVEL_ARMOR_094", "Basic Travel Armor", new[] { "ARMOR", "BODY_ARMOR" });
                candidate = EquipBest(candidate, arrival.RecruitId, EquipmentSlotIds.BodyArmor, armor,
                    Array.Empty<string>());
            }
            return candidate;
        }

        internal JObject EquipmentClassAuthority112(RecruitState recruit) =>
            _catalog.Class(SssTenV4Roster090.TryGetRecruit(recruit, out var hero)
                ? hero.ClassId : _generator.Generate(recruit).StartingClassId);
        internal JObject EquipmentFamilyAuthority112(RecruitState recruit) => _catalog.Family(ResolveFamily(recruit));

        string ResolveFamily(RecruitState recruit)
        {
            if (SssTenV4Roster090.TryGet(recruit.AuthoredStableRecruitId, out var sss) ||
                SssTenV4Roster090.TryGet(recruit.RecruitId, out sss) ||
                SssTenV4Roster090.TryGet(recruit.SignatureId, out sss))
            {
                switch (sss.WeaponFamilyId)
                {
                    case "WF01_SWORD": return "WEAPON_FAMILY_SWORD";
                    case "WF02_GREAT_WEAPON": return "WEAPON_FAMILY_GREAT_WEAPON";
                    case "WF03_AXE": return "WEAPON_FAMILY_AXE";
                    case "WF04_SPEAR_POLEARM": return "WEAPON_FAMILY_SPEAR_POLEARM";
                    case "WF05_BOW": return "WEAPON_FAMILY_BOW";
                    case "WF06_DAGGER": return "WEAPON_FAMILY_DAGGER";
                    case "WF07_SHIELD": return "WEAPON_FAMILY_SHIELD";
                    case "WF08_GAUNTLET": return "WEAPON_FAMILY_GAUNTLET";
                    case "WF09_STAFF": return "WEAPON_FAMILY_STAFF";
                    case "WF10_CATALYST_FOCUS": return "WEAPON_FAMILY_FOCUS";
                    case "WF11_ENGINEERING_TOOL": return "WEAPON_FAMILY_ENGINEERING_TOOL";
                    case "WF12_HYBRID_RELIC": return "WEAPON_FAMILY_HYBRID_RELIC_WEAPON";
                    default: throw new InvalidOperationException("Unresolved SSS starter family: " + sss.WeaponFamilyId);
                }
            }
            return _generator.Generate(recruit).FixedWeaponFamilyId;
        }

        CampaignState EquipBest(CampaignState campaign, string recruitId, string slot,
            EquipmentItemState basic, string[] familyTags)
        {
            var recruit = campaign.Guild.Recruits.Single(value => value.RecruitId == recruitId);
            var caster = IsCaster(recruit);
            var equippedIds = new HashSet<string>(campaign.Guild.Recruits.SelectMany(value =>
                value.Equipment.Assignments.Select(assignment => assignment.Item.InstanceId)), StringComparer.Ordinal);
            var choice = campaign.Guild.Inventory.Where(item => !item.PlayerLocked &&
                    !equippedIds.Contains(item.InstanceId) &&
                    (!item.EquipmentTags.Contains("SSS_SIGNATURE") || IsOwnSignature121(recruit, item)) &&
                    SssTenV4Inventory090.CanEquip(recruit, item, slot, out _) &&
                    (familyTags.Length == 0 || IsOwnSignature121(recruit, item) || familyTags.Any(item.EquipmentTags.Contains)) &&
                    PreservesLearnedArts(recruit, basic, item) &&
                    Score(item, caster) > Score(basic, caster))
                .OrderByDescending(item => IsOwnSignature121(recruit, item))
                .ThenByDescending(item => Score(item, caster))
                .ThenBy(item => item.InstanceId, StringComparer.Ordinal).FirstOrDefault();
            if (choice == null)
            {
                if (equippedIds.Contains(basic.InstanceId))
                    throw new InvalidOperationException("The recruit's basic item is already assigned elsewhere.");
                choice = campaign.Guild.Inventory.FirstOrDefault(item => item.InstanceId == basic.InstanceId);
                if (choice != null && CanonicalJson.Serialize(choice) != CanonicalJson.Serialize(basic))
                    throw new InvalidOperationException("Basic item identity collision.");
                if (choice == null)
                {
                    choice = basic;
                    var inventory = campaign.Guild.Inventory.Concat(new[] { basic })
                        .OrderBy(item => item.InstanceId, StringComparer.Ordinal).ToArray();
                    campaign = campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
                        campaign.Guild.Recruits, campaign.Guild.Unions, inventory), campaign.OpeningFlow);
                }
            }
            var flow = campaign.OpeningFlow;
            var equipped = new M1CommandService().EquipItem(campaign, recruitId, slot, choice.InstanceId);
            if (!equipped.IsSuccess)
                throw new InvalidOperationException(string.Join("; ", equipped.Errors));
            // Automatic outfitting is not a manual equipment/tutorial review.
            return equipped.Value.With(equipped.Value.Guild, flow);
        }

        bool PreservesLearnedArts(RecruitState recruit, EquipmentItemState basic, EquipmentItemState next)
        {
            var other = recruit.Equipment.Assignments.SelectMany(value => value.Item.EquipmentTags).ToArray();
            foreach (var id in recruit.Progression.LearnedArtIds)
            {
                if (!_combat.Arts.TryGetValue(id, out var art) || art.RequiredEquipmentTags.Count == 0) continue;
                if (art.RequiredEquipmentTags.Any(tag => basic.EquipmentTags.Contains(tag) || other.Contains(tag)) &&
                    !art.RequiredEquipmentTags.Any(tag => next.EquipmentTags.Contains(tag) || other.Contains(tag)))
                    return false;
            }
            return true;
        }

        // Only ApplyToNewRecruits reaches this selection. This consumes an
        // already-owned exact signature; it never creates its entitlement and
        // cannot re-outfit a hero who existed before the current grant.
        static bool IsOwnSignature121(RecruitState recruit, EquipmentItemState item) =>
            SssTenV4Roster090.TryGetRecruit(recruit, out var hero) &&
            StringComparer.Ordinal.Equals(item.DefinitionId, hero.WeaponItemId) &&
            item.EquipmentTags.Contains("SSS_SIGNATURE");

        static EquipmentItemState Basic(CampaignState campaign, RecruitState recruit, string slot,
            string definition, string name, string[] tags) => new EquipmentItemState(
                "RECRUIT_BASIC_094_" + CanonicalJson.Sha256Hex(new { campaign.CampaignGuid,
                    recruit.RecruitId, slot }).Substring(0, 24).ToUpperInvariant(),
                "BASIC_094_" + definition, name, new[] { slot },
                tags.Concat(new[] { "RECRUIT_STARTER_094" }).Distinct(StringComparer.Ordinal).ToArray(),
                "QUALITY_STARTER", 10000, false);

        static int Score(EquipmentItemState item, bool caster)
        {
            var power = M2EquipmentPowerPolicy087.Resolve(item);
            return checked(power.PhysicalAttack * (caster ? 1 : 3) + power.MysticAttack * (caster ? 3 : 1));
        }
        static bool IsCaster(RecruitState recruit)
        {
            var role = (recruit.ClassTendencyId ?? "").ToUpperInvariant();
            return new[] { "MAGE", "PRIEST", "HEAL", "MEDIC", "MYSTIC", "SUMMON" }.Any(role.Contains);
        }
        static string FamilyName(string family) => family.Replace("WEAPON_FAMILY_", "")
            .Replace("_", " ").ToLowerInvariant();
    }
}


