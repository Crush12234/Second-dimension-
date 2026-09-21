using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M1
{
    public sealed class ChestUpgradeDecision092
    {
        public string RecruitId { get; internal set; }
        public string RecruitName { get; internal set; }
        public string SlotId { get; internal set; }
        public int PhysicalGain { get; internal set; }
        public int MysticGain { get; internal set; }
        internal int Score { get; set; }
        internal bool Active { get; set; }
    }

    /// <summary>
    /// Chooses only a conservative upgrade to a newly earned chest item. The
    /// ordinary equipment command remains the final legality/inventory authority.
    /// Never steals another member's gear or changes a member's weapon discipline.
    /// </summary>
    public static class ChestLootAutoEquip092
    {
        private static readonly HashSet<string> ArtEquipmentTags = new HashSet<string>(
            new[] { "SWORD", "AXE", "GREAT_AXE", "GREAT_WEAPON", "SPEAR", "POLEARM",
                "BOW", "DAGGER", "SHIELD", "GAUNTLET", "STAFF", "WAND", "FOCUS",
                "FOCUS_TOOL", "CATALYST", "HEALING", "HORN", "ENGINEERING_TOOL" },
            StringComparer.Ordinal);

        public static ChestUpgradeDecision092 Preview(CampaignState campaign, EquipmentItemState item)
        {
            if (campaign?.Guild == null || item == null || item.PlayerLocked ||
                campaign.OpeningFlow == null || campaign.Profile == null ||
                campaign.Battle?.Outcome == BattleOutcome.InProgress)
                return null;
            var candidates = new List<ChestUpgradeDecision092>();
            var active = new HashSet<string>(campaign.Guild.Unions
                .Where(value => value.Kind == UnionKind.Normal)
                .SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
            var nextPower = M2EquipmentPowerPolicy087.Resolve(item);
            foreach (var recruit in campaign.Guild.Recruits)
            foreach (var slot in item.ValidSlotIds.Distinct(StringComparer.Ordinal))
            {
                if (!ProtectedActorPolicy.CanUseNormalEquipment(recruit) ||
                    !EquipmentSlotIds.IsOpeningSlot(slot) ||
                    !SssTenV4Inventory090.CanEquip(recruit, item, slot, out _)) continue;
                var current = recruit.Equipment.Find(slot)?.Item;
                if (current?.PlayerLocked == true ||
                    current?.EquipmentTags.Contains("SSS_SIGNATURE") == true) continue;
                if (!PreservesEquipmentDiscipline092(recruit, slot, current, item)) continue;
                var oldPower = M2EquipmentPowerPolicy087.Resolve(current);
                var physical = nextPower.PhysicalAttack - oldPower.PhysicalAttack;
                var mystic = nextPower.MysticAttack - oldPower.MysticAttack;
                // Ambiguous sidegrades remain in Inventory for an explicit choice.
                if (physical < 0 || mystic < 0 || physical + mystic <= 0) continue;
                var caster = IsCaster092(recruit);
                candidates.Add(new ChestUpgradeDecision092
                {
                    RecruitId = recruit.RecruitId,
                    RecruitName = recruit.DisplayName,
                    SlotId = slot,
                    PhysicalGain = physical,
                    MysticGain = mystic,
                    Score = checked(physical * (caster ? 1 : 3) + mystic * (caster ? 3 : 1)),
                    Active = active.Contains(recruit.RecruitId)
                });
            }
            return candidates.OrderByDescending(value => value.Score)
                .ThenByDescending(value => value.Active)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .ThenBy(value => value.SlotId, StringComparer.Ordinal).FirstOrDefault();
        }

        public static CampaignState Apply(CampaignState campaign, string earnedItemInstanceId)
        {
            var item = campaign?.Guild?.Inventory.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.InstanceId, earnedItemInstanceId));
            var decision = Preview(campaign, item);
            if (decision == null) return campaign;
            var equipped = new M1CommandService().EquipItem(campaign, decision.RecruitId,
                decision.SlotId, item.InstanceId);
            // An automatic reward cannot claim the player manually reviewed gear.
            return equipped.IsSuccess
                ? equipped.Value.With(equipped.Value.Guild, campaign.OpeningFlow)
                : campaign;
        }

        private static bool PreservesEquipmentDiscipline092(RecruitState recruit,
            string slot, EquipmentItemState current, EquipmentItemState next)
        {
            if (slot != EquipmentSlotIds.MainHand && slot != EquipmentSlotIds.OffHand) return true;
            var required = (current?.EquipmentTags ?? Array.Empty<string>())
                .Where(ArtEquipmentTags.Contains).ToArray();
            if (required.Length > 0)
                return required.All(value => next.EquipmentTags.Contains(value));
            // With an empty hand, do not give a caster a sword or a warrior a staff.
            var magic = next.EquipmentTags.Any(value => value == "STAFF" || value == "WAND" ||
                value == "FOCUS" || value == "FOCUS_TOOL" || value == "CATALYST" || value == "HEALING");
            return IsCaster092(recruit) == magic;
        }

        private static bool IsCaster092(RecruitState recruit)
        {
            var role = (recruit.ClassTendencyId ?? string.Empty).ToUpperInvariant();
            return role.Contains("MAGE") || role.Contains("PRIEST") || role.Contains("HEALER") ||
                role.Contains("MEDIC") || role.Contains("MYSTIC") || role.Contains("SUMMON");
        }
    }
}
