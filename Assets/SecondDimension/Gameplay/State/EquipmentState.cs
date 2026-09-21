using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.State
{
    public static class EquipmentSlotIds
    {
        public const string MainHand = "SLOT_MAIN_HAND";
        public const string OffHand = "SLOT_OFF_HAND";
        public const string BodyArmor = "SLOT_BODY_ARMOR";
        public const string AccessoryOne = "SLOT_ACCESSORY_1";
        public const string AccessoryTwo = "SLOT_ACCESSORY_2";
        public const string ToolRelic = "SLOT_TOOL_RELIC";

        private static readonly string[] AllValues =
        {
            MainHand,
            OffHand,
            BodyArmor,
            AccessoryOne,
            AccessoryTwo,
            ToolRelic
        };

        public static IReadOnlyList<string> All => Array.AsReadOnly(AllValues);

        public static bool IsOpeningSlot(string slotId)
        {
            for (var index = 0; index < AllValues.Length; index++)
            {
                if (StringComparer.Ordinal.Equals(AllValues[index], slotId)) return true;
            }
            return false;
        }
    }

    [Serializable]
    public sealed class EquipmentItemState
    {
        [JsonConstructor]
        public EquipmentItemState(
            string instanceId,
            string definitionId,
            string displayName,
            IReadOnlyList<string> validSlotIds,
            IReadOnlyList<string> equipmentTags,
            string qualityId,
            int conditionBasisPoints,
            bool playerLocked,
            bool inventoryOnly = false)
        {
            InstanceId = Require(instanceId, nameof(instanceId));
            DefinitionId = Require(definitionId, nameof(definitionId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? DefinitionId : displayName;
            ValidSlotIds = CopyStableIds(validSlotIds, nameof(validSlotIds));
            if (ValidSlotIds.Count == 0 && !inventoryOnly)
                throw new ArgumentException("Equipment requires a legal slot.", nameof(validSlotIds));
            EquipmentTags = CopyStableIds(equipmentTags, nameof(equipmentTags));
            QualityId = qualityId ?? string.Empty;
            if (conditionBasisPoints < 0 || conditionBasisPoints > 10_000)
            {
                throw new ArgumentOutOfRangeException(nameof(conditionBasisPoints));
            }
            ConditionBasisPoints = conditionBasisPoints;
            PlayerLocked = playerLocked;
            InventoryOnly = inventoryOnly;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> ValidSlotIds { get; }
        public IReadOnlyList<string> EquipmentTags { get; }
        public string QualityId { get; }
        public int ConditionBasisPoints { get; }
        public bool PlayerLocked { get; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool InventoryOnly { get; }

        public bool CanEquipIn(string slotId)
        {
            if (InventoryOnly) return false;
            for (var index = 0; index < ValidSlotIds.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(ValidSlotIds[index], slotId)) return true;
            }
            return false;
        }

        public EquipmentItemState WithPlayerLock(bool playerLocked) =>
            new EquipmentItemState(
                InstanceId,
                DefinitionId,
                DisplayName,
                ValidSlotIds,
                EquipmentTags,
                QualityId,
                ConditionBasisPoints,
                playerLocked,
                InventoryOnly);

        private static IReadOnlyList<string> CopyStableIds(IReadOnlyList<string> values, string parameter)
        {
            var copy = new List<string>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    copy.Add(Require(values[index], parameter));
                }
            }
            return copy.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    [Serializable]
    public sealed class EquipmentSlotAssignmentState
    {
        [JsonConstructor]
        public EquipmentSlotAssignmentState(string slotId, EquipmentItemState item)
        {
            if (!EquipmentSlotIds.IsOpeningSlot(slotId))
            {
                throw new ArgumentException("Unknown opening equipment slot.", nameof(slotId));
            }
            SlotId = slotId;
            Item = item ?? throw new ArgumentNullException(nameof(item));
            if (!item.CanEquipIn(slotId))
            {
                throw new ArgumentException("Item is not legal in the selected slot.", nameof(item));
            }
        }

        public string SlotId { get; }
        public EquipmentItemState Item { get; }
    }

    [Serializable]
    public sealed class EquipmentSlotSnapshotState
    {
        [JsonConstructor]
        public EquipmentSlotSnapshotState(string slotId, EquipmentItemState item, string lockedInstanceId)
        {
            if (!EquipmentSlotIds.IsOpeningSlot(slotId))
            {
                throw new ArgumentException("Unknown opening equipment slot.", nameof(slotId));
            }
            if (item != null && !item.CanEquipIn(slotId))
            {
                throw new ArgumentException("Item is not legal in the selected slot.", nameof(item));
            }
            var expectedLockId = item != null && item.PlayerLocked ? item.InstanceId : string.Empty;
            if (!string.IsNullOrEmpty(lockedInstanceId) &&
                !StringComparer.Ordinal.Equals(expectedLockId, lockedInstanceId))
            {
                throw new ArgumentException("Slot lock must identify its exact equipped item instance.", nameof(lockedInstanceId));
            }
            SlotId = slotId;
            Item = item;
            LockedInstanceId = expectedLockId;
        }

        public string SlotId { get; }
        public EquipmentItemState Item { get; }
        public string LockedInstanceId { get; }
    }

    [Serializable]
    public sealed class EquipmentLoadoutState
    {
        public EquipmentLoadoutState(IReadOnlyList<EquipmentSlotAssignmentState> assignments)
            : this(assignments, slots: null)
        {
        }

        [JsonConstructor]
        public EquipmentLoadoutState(
            IReadOnlyList<EquipmentSlotAssignmentState> assignments,
            IReadOnlyList<EquipmentSlotSnapshotState> slots)
        {
            var copy = new List<EquipmentSlotAssignmentState>();
            if (assignments != null)
            {
                for (var index = 0; index < assignments.Count; index++)
                {
                    var assignment = assignments[index] ?? throw new ArgumentException(
                        "Equipment assignment cannot be null.",
                        nameof(assignments));
                    for (var existingIndex = 0; existingIndex < copy.Count; existingIndex++)
                    {
                        if (StringComparer.Ordinal.Equals(copy[existingIndex].SlotId, assignment.SlotId))
                        {
                            throw new ArgumentException("An equipment slot may be assigned only once.", nameof(assignments));
                        }
                        if (StringComparer.Ordinal.Equals(copy[existingIndex].Item.InstanceId, assignment.Item.InstanceId))
                        {
                            throw new ArgumentException("An equipment instance may be assigned only once.", nameof(assignments));
                        }
                    }
                    copy.Add(assignment);
                }
            }
            copy.Sort((left, right) => StringComparer.Ordinal.Compare(left.SlotId, right.SlotId));
            Assignments = copy.AsReadOnly();
            Slots = CreateSlots(copy, slots);
        }

        public IReadOnlyList<EquipmentSlotAssignmentState> Assignments { get; }
        public IReadOnlyList<EquipmentSlotSnapshotState> Slots { get; }

        public EquipmentSlotAssignmentState Find(string slotId)
        {
            for (var index = 0; index < Assignments.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(Assignments[index].SlotId, slotId)) return Assignments[index];
            }
            return null;
        }

        public static EquipmentLoadoutState Empty() =>
            new EquipmentLoadoutState(Array.Empty<EquipmentSlotAssignmentState>());

        private static IReadOnlyList<EquipmentSlotSnapshotState> CreateSlots(
            IReadOnlyList<EquipmentSlotAssignmentState> assignments,
            IReadOnlyList<EquipmentSlotSnapshotState> serializedSlots)
        {
            var slots = new List<EquipmentSlotSnapshotState>();
            for (var slotIndex = 0; slotIndex < EquipmentSlotIds.All.Count; slotIndex++)
            {
                var slotId = EquipmentSlotIds.All[slotIndex];
                EquipmentItemState item = null;
                for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
                {
                    if (StringComparer.Ordinal.Equals(assignments[assignmentIndex].SlotId, slotId))
                    {
                        item = assignments[assignmentIndex].Item;
                        break;
                    }
                }

                if (serializedSlots != null)
                {
                    for (var serializedIndex = 0; serializedIndex < serializedSlots.Count; serializedIndex++)
                    {
                        var serialized = serializedSlots[serializedIndex];
                        if (serialized != null && StringComparer.Ordinal.Equals(serialized.SlotId, slotId))
                        {
                            var serializedId = serialized.Item == null ? string.Empty : serialized.Item.InstanceId;
                            var currentId = item == null ? string.Empty : item.InstanceId;
                            if (!StringComparer.Ordinal.Equals(serializedId, currentId))
                            {
                                throw new ArgumentException("Serialized slot snapshot differs from assignments.", nameof(serializedSlots));
                            }
                        }
                    }
                }
                slots.Add(new EquipmentSlotSnapshotState(
                    slotId,
                    item,
                    item != null && item.PlayerLocked ? item.InstanceId : string.Empty));
            }
            return slots.AsReadOnly();
        }
    }
}
