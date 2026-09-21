using System;
using System.Globalization;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    // Authored R50 wares MR002_OFFER_018/019 only. The existing merchant item,
    // quality, price, inventory and receipt authorities remain authoritative.
    public static class TownCommissions154
    {
        public const int RequiredMarketLevel = 3;
        public const string WeaponWareId = "MR002_OFFER_018";
        public const string ArmorWareId = "MR002_OFFER_019";

        public static string OfferId(CampaignState state, bool armor) =>
            "TOWN154_COMMISSION_" + state.Guild.GuildCity.OperationOrdinal + "_" +
            (armor ? ArmorWareId : WeaponWareId) + TownService153.StockSuffix159(state);

        public static bool IsCurrentOffer(CampaignState state, string offer) =>
            state?.Guild?.GuildCity != null &&
            (offer == OfferId(state, false) || offer == OfferId(state, true));

        public static bool IsArmor(CampaignState state, string offer) =>
            offer == OfferId(state, true);

        public static EquipmentItemState CreateItem(CampaignState state, string offer, string receipt)
        {
            if (!IsCurrentOffer(state, offer))
                throw new ArgumentException("This commission is no longer available.");
            var card = CanonicalJson.Sha256Hex(new
            {
                state.CampaignGuid, Offer = offer, Policy = "NATIVE_MERCHANT_089_COMMISSION_154"
            });
            // Native 089 reads type from the third hex nibble from the end and
            // quality from the final two. Constrain ONLY type to the authored
            // known weapon/armor output; keep the original native quality roll.
            // There is no search, reroll, counter, quality boost or new price.
            int kind = IsArmor(state, offer) ? 2 :
                (Convert.ToInt32(card.Substring(0, 1), 16) & 1);
            var typedCard = card.Substring(0, card.Length - 3) +
                kind.ToString(CultureInfo.InvariantCulture) + card.Substring(card.Length - 2);
            var item = ExpeditionDeckService089.MerchantEquipmentReward089(typedCard, receipt);
            var expectedSlot = IsArmor(state, offer) ? EquipmentSlotIds.BodyArmor : EquipmentSlotIds.MainHand;
            if (item.ValidSlotIds.Count != 1 || item.ValidSlotIds[0] != expectedSlot || item.InventoryOnly)
                throw new InvalidOperationException("Native commission output no longer matches its authored slot.");
            return item;
        }

        public static string CostText(GuildCityCostDefinition017D cost) =>
            cost.HallXp + " Hall XP" + string.Concat(
                (cost.Materials ?? Array.Empty<GuildMaterialDefinition017D>()).Select(material =>
                    " · " + material.Amount + " " + material.MaterialId.Replace("MAT_", "")
                        .Replace('_', ' ').ToLowerInvariant()));

        public static string CombinedCostText(GuildCityCostDefinition017D second,
            GuildCityCostDefinition017D third)
        {
            long hall = checked((long)second.HallXp + third.HallXp);
            var materials = (second.Materials ?? Array.Empty<GuildMaterialDefinition017D>())
                .Concat(third.Materials ?? Array.Empty<GuildMaterialDefinition017D>())
                .GroupBy(material => material.MaterialId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => " · " + group.Sum(material => (long)material.Amount) + " " +
                    group.Key.Replace("MAT_", "").Replace('_', ' ').ToLowerInvariant());
            return "Level 2: " + CostText(second) + "\nLevel 3: " + CostText(third) +
                "\nTOTAL: " + hall + " Hall XP" + string.Concat(materials);
        }
    }
}
