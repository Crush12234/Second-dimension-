using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.M1
{
    [Serializable]
    public sealed class EquipmentChange112
    {
        [JsonConstructor]
        public EquipmentChange112(string recruitId, string slotId, string beforeItemId, string afterItemId)
        {
            RecruitId = recruitId;
            SlotId = slotId;
            BeforeItemId = beforeItemId ?? string.Empty;
            AfterItemId = afterItemId ?? string.Empty;
        }
        public string RecruitId { get; }
        public string SlotId { get; }
        public string BeforeItemId { get; }
        public string AfterItemId { get; }
    }

    // Only ownership references are retained; Undo never restores a previous
    // CampaignState, recruit progression, currency, or item payload.
    [Serializable]
    public sealed class EquipmentUndoState112
    {
        [JsonConstructor]
        public EquipmentUndoState112(string afterGuard, IEnumerable<EquipmentChange112> changes, string summary)
        {
            AfterGuard = afterGuard ?? string.Empty;
            Changes = Array.AsReadOnly((changes ?? Array.Empty<EquipmentChange112>()).ToArray());
            Summary = summary ?? string.Empty;
        }
        public string AfterGuard { get; }
        public IReadOnlyList<EquipmentChange112> Changes { get; }
        public string Summary { get; }
    }
}
