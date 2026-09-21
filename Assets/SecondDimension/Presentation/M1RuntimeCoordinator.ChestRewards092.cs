using System;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        private static string QuestCardItemVisualId092(GuildQuestCardOffer090 offer)
        {
            if (offer == null || offer.Category != "CHEST" && offer.Category != "MERCHANT")
                return string.Empty;
            var item = GuildCityExpeditionService017D.CreateQuestCardEquipment090(
                offer.CardId, GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + offer.CardId,
                offer.Category == "MERCHANT");
            return EquipmentVisualId090(item, item.ValidSlotIds.FirstOrDefault());
        }

        private static M1CommandResult WithChestReceipt092(M1CommandResult result,
            CampaignState before, CampaignState after, EquipmentItemState item)
        {
            if (result?.Succeeded != true || item == null || after?.Guild == null) return result;
            var owner = after.Guild.Recruits.FirstOrDefault(value => value.Equipment.Assignments
                .Any(slot => slot.Item.InstanceId == item.InstanceId));
            var inInventory = after.Guild.Inventory.Any(value => value.InstanceId == item.InstanceId);
            if (owner == null && !inInventory) return result;
            var slotId = owner?.Equipment.Assignments.First(value =>
                value.Item.InstanceId == item.InstanceId).SlotId ?? item.ValidSlotIds.FirstOrDefault();
            var oldItem = before?.Guild?.Recruits.FirstOrDefault(value =>
                value.RecruitId == owner?.RecruitId)?.Equipment.Find(slotId)?.Item;
            var currentPower = M2EquipmentPowerPolicy087.Resolve(item);
            var oldPower = M2EquipmentPowerPolicy087.Resolve(oldItem);
            var physical = owner == null ? 0 : currentPower.PhysicalAttack - oldPower.PhysicalAttack;
            var mystic = owner == null ? 0 : currentPower.MysticAttack - oldPower.MysticAttack;
            var summary = item.DisplayName + " • " + (owner == null
                ? "ADDED TO INVENTORY"
                : "AUTO-EQUIPPED TO " + owner.DisplayName + " • PWR " + Signed092(physical) +
                  " • MYS " + Signed092(mystic) +
                  (oldItem == null ? " • empty slot filled" : " • previous gear kept in Inventory"));
            var receipt = new LootRewardView092
            {
                ItemName = item.DisplayName,
                ItemVisualId = EquipmentVisualId090(item, slotId),
                RarityId = item.QualityId,
                Summary = summary,
                IsCommitted = true,
                WasAutoEquipped = owner != null,
                RecipientName = owner?.DisplayName ?? string.Empty,
                PhysicalGain = physical,
                MysticGain = mystic
            };
            var completed = M1CommandResult.Success(result.Message + " " + summary);
            completed.LootReward092 = receipt;
            return completed;
        }

        private static string Signed092(int value) => value >= 0 ? "+" + value : value.ToString();

    }
}
