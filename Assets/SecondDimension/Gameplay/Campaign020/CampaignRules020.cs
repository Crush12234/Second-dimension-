using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.Campaign023;

namespace SecondDimension.Gameplay.Campaign020
{
    public sealed class CampaignStepRule020
    {
        public string StepId; public string Kind; public string Title; public string SourceId; public bool RequiresCertifiedBattle; public bool ConsumesOperation;
    }
    public sealed class CampaignBlueprintRule020
    {
        public string BlueprintId; public string ChapterId; public string ArcId; public string WorldId; public string MapId; public string SiegeId; public string EnemyPackId; public string LootProfileId;
        public IReadOnlyList<string> MaterialIds=Array.Empty<string>(); public IReadOnlyList<string> RepeatableUnlockIds=Array.Empty<string>(); public IReadOnlyList<CampaignStepRule020> Steps=Array.Empty<CampaignStepRule020>();
        public bool RequiresCertifiedBattle; public int MaximumAlliedUnions; public int MaximumEnemyUnions;
    }
    public sealed class CampaignRepeatableUnlockRule020
    {
        public CampaignRepeatableUnlockRule020(string contractId,string worldId,
            int unlockedByChapterCount)
        {
            ContractId=contractId;WorldId=worldId;
            UnlockedByChapterCount=unlockedByChapterCount;
        }
        public string ContractId { get; }
        public string WorldId { get; }
        public int UnlockedByChapterCount { get; }
    }
    // Optional so existing gameplay catalog adapters retain their authored grants.
    public interface ICampaignRepeatableUnlockCatalog020
    {
        IReadOnlyList<CampaignRepeatableUnlockRule020> RepeatableUnlockRules { get; }
    }
    public interface ICampaignPlayableCatalog020
    {
        bool TryGetBlueprint(string chapterId,out CampaignBlueprintRule020 blueprint);
        IReadOnlyList<string> RepeatableContractsForWorld(string worldId);
        bool IsKnownMaterial(string materialId);
        IWorldGateOperationsCatalog023 WorldGateProofCatalog023 { get; }
    }
}
