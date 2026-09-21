using System;
using System.Collections.Generic;
namespace SecondDimension.Gameplay.Campaign023
{
 public sealed class WorldGateNodeRule023
 {
  public string NodeId; public string Kind; public string Title; public string Description; public IReadOnlyList<string> NextNodeIds=Array.Empty<string>(); public IReadOnlyList<string> ChoiceIds=Array.Empty<string>(); public int CheckDifficulty; public int SupplyDelta; public int FatigueDelta; public int UrgencyDelta; public int ThreatDelta; public int TrustDelta; public int TensionDelta; public int CivilianSupportDelta; public int GuildXp; public int HallXp; public IReadOnlyList<string> MaterialIds=Array.Empty<string>(); public bool RequiresCertifiedBattle; public int EnemyUnionCount; public string Objective; public bool Optional; public string SourceId;
 }
 public sealed class WorldGateBoardRule023
 {
  public string BoardId; public string DefinitionId; public string OperationKind; public string WorldId; public string Title; public string StartNodeId; public string ExitNodeId; public string EnemyPackId; public string LootProfileId; public bool UsesStrategicDefense017H; public bool UsesCertifiedBattle; public int MaximumAlliedUnions; public int MaximumEnemyUnions; public IReadOnlyList<WorldGateNodeRule023> Nodes=Array.Empty<WorldGateNodeRule023>();
 }
 public sealed class WorldTravelRule023 { public string TravelId; public string WorldId; public string DisplayName; public string GateOrHub; public IReadOnlyList<string> UnlockGates=Array.Empty<string>(); public int SupplyCost; public IReadOnlyList<string> MaterialIds=Array.Empty<string>(); public bool ReturnToSkyhomeAlwaysAvailableAfterCommit; }
 public sealed class WorldStandingRule023 { public string WorldId; public string DisplayName; public IReadOnlyList<string> TierIds=Array.Empty<string>(); }
 public sealed class RecruitUnlockRule023 { public string WorldId; public string DisplayName; public IReadOnlyList<string> OriginProfileIds=Array.Empty<string>(); public string MinimumStandingTier; public bool NativeVisualReady; }
 public sealed class WorldGateRewardPolicy023 { public IReadOnlyList<int> RepeatableConsecutiveRewardBasisPoints=Array.Empty<int>(); public bool ResetOnDifferentCompletedOperation; public int TravelSupplyReserveDefault; public int TravelSupplyRewardPerCompletedOperation; }
 public interface IWorldGateOperationsCatalog023
 {
  bool TryGetBoard(string definitionId,out WorldGateBoardRule023 board); bool TryGetTravel(string worldId,out WorldTravelRule023 travel); bool TryGetStanding(string worldId,out WorldStandingRule023 standing); bool TryGetRecruitUnlock(string worldId,out RecruitUnlockRule023 unlock); WorldGateRewardPolicy023 RewardPolicy{get;} IReadOnlyList<WorldGateBoardRule023> AllBoards{get;}
 }
}
