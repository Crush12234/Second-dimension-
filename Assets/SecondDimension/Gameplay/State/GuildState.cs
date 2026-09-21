using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.GuildCity017D;

namespace SecondDimension.Gameplay.State
{
    [Serializable]
    public sealed class GuildState
    {
        public GuildState(
            string guildId,
            long treasuryXp,
            IReadOnlyList<RecruitState> recruits,
            IReadOnlyList<UnionState> unions)
            : this(
                guildId,
                treasuryXp,
                recruits,
                unions,
                Array.Empty<EquipmentItemState>(),
                GuildDevelopmentState.Default(),
                guildCity: null)
        {
        }

        [JsonConstructor]
        public GuildState(
            string guildId,
            long treasuryXp,
            IReadOnlyList<RecruitState> recruits,
            IReadOnlyList<UnionState> unions,
            IReadOnlyList<EquipmentItemState> inventory,
            GuildDevelopmentState development = null,
            GuildCityState017D guildCity = null)
        {
            GuildId = string.IsNullOrWhiteSpace(guildId)
                ? throw new ArgumentException("Guild ID is required.", nameof(guildId))
                : guildId;
            if (treasuryXp < 0) throw new ArgumentOutOfRangeException(nameof(treasuryXp));
            TreasuryXp = treasuryXp;
            Recruits = Copy(recruits);
            Unions = Copy(unions);
            Inventory = CopyUniqueInventory(inventory);
            Development = development ?? GuildDevelopmentState.Default();
            GuildCity = guildCity ?? GuildCityState017D.Default(Recruits, Unions);
        }

        public string GuildId { get; }
        public long TreasuryXp { get; }
        public IReadOnlyList<RecruitState> Recruits { get; }
        public IReadOnlyList<UnionState> Unions { get; }
        public IReadOnlyList<EquipmentItemState> Inventory { get; }
        public GuildDevelopmentState Development { get; }
        public GuildCityState017D GuildCity { get; }

        public GuildState With(
            long treasuryXp,
            IReadOnlyList<RecruitState> recruits,
            IReadOnlyList<UnionState> unions,
            IReadOnlyList<EquipmentItemState> inventory) =>
            new GuildState(GuildId, treasuryXp, recruits, unions, inventory, Development, GuildCity);

        public GuildState With(
            long treasuryXp,
            IReadOnlyList<RecruitState> recruits,
            IReadOnlyList<UnionState> unions,
            IReadOnlyList<EquipmentItemState> inventory,
            GuildDevelopmentState development) =>
            new GuildState(GuildId, treasuryXp, recruits, unions, inventory, development, GuildCity);

        public GuildState WithGuildCity(GuildCityState017D guildCity) =>
            new GuildState(GuildId, TreasuryXp, Recruits, Unions, Inventory, Development,
                guildCity ?? throw new ArgumentNullException(nameof(guildCity)));

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        {
            var copy = new List<T>();
            if (values != null) for (var i = 0; i < values.Count; i++) copy.Add(values[i]);
            return copy.AsReadOnly();
        }

        private static IReadOnlyList<EquipmentItemState> CopyUniqueInventory(IReadOnlyList<EquipmentItemState> values)
        {
            var copy = new List<EquipmentItemState>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    var item = values[index] ?? throw new ArgumentException("Inventory item cannot be null.", nameof(values));
                    for (var existing = 0; existing < copy.Count; existing++)
                    {
                        if (StringComparer.Ordinal.Equals(copy[existing].InstanceId, item.InstanceId))
                        {
                            throw new ArgumentException("Inventory instance IDs must be unique.", nameof(values));
                        }
                    }
                    copy.Add(item);
                }
            }
            copy.Sort((left, right) => StringComparer.Ordinal.Compare(left.InstanceId, right.InstanceId));
            return copy.AsReadOnly();
        }
    }
}
