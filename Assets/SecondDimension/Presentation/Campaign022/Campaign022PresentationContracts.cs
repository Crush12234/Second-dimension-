using System;
using System.Collections.Generic;
using UnityEngine;
namespace SecondDimension.Presentation.Campaign022
{
 public interface ICampaignProgressionPresentationCoordinator022
 {
  CampaignProgressionPresentationState022 CampaignProgression022{get;}
  M1CommandResult RecordEquipmentUse022(string itemInstanceId,string trackId);
  M1CommandResult EvolveWeapon022(string itemInstanceId,string recipeId);
  M1CommandResult CertifyAdvancedClass022(string recruitId,string classId);
  M1CommandResult BeginAbyssOperation022(string operationId);
  M1CommandResult BeginTowerRun081();
  M1CommandResult AdvanceTowerRun081();
  M1CommandResult RetreatTowerRun081();
  M1CommandResult RecoverInactiveLegacyTower084();
  M1CommandResult EnterAbyssBattle022();
  M1CommandResult CommitAbyssStep022();
  M1CommandResult ApplyAbyssStep022();
  M1CommandResult CommitAbyssBattleResult022();
  M1CommandResult FinalizeAbyssOperation022();
  M1CommandResult FinalizeAbyssBattle022();
  M1CommandResult CraftInvocationArtifact022(string baseId);
  M1CommandResult CraftInvocationArtifact022(string baseId,IReadOnlyList<string> affixIds);
  M1CommandResult EvolveInvocationArtifact022(string itemInstanceId);
  M1CommandResult InvokeEligibleEchoForecast022();
  M1CommandResult InvokeAcceptedCovenantForecast022(string covenantId);
  M1CommandResult AdvanceCovenant022(string covenantId);
  M1CommandResult AcceptCovenant022(string covenantId);
 }
 public interface ITowerVictoryBankCoordinator110
 {
  M1CommandResult BankTowerVictory110();
 }
 public interface ITowerAutoTransitionCoordinator108
 {
  M1CommandResult AdvanceTowerAutoAfterVictory108();
 }
 public sealed class CampaignProgressionPresentationState022
 {
  public bool IsAvailable;
  public string Error;
  public int WeaponTracks;
  public int WeaponRecipes;
  public int ArmorRecipes;
  public int AdvancedClasses;
  public int CertificationPaths;
  public int AbyssFloors;
  public int AbyssOperations;
  public int ArtifactBases;
  public int Covenants;
  public int SummonResonance;
  public int EchoInvocationReceiptCount;
  public bool GreatCovenantGateEarned;
  public string ActiveAbyssOperationId;
  public string ActiveAbyssFloorId;
  public string ActiveAbyssStatus;
  public bool LegacyTowerRecoveryRequired;
  public bool LegacyTowerRecoveryRequiresSupport;
  public int ActiveStepIndex;
  public int ActiveStepCount;
  public int ActiveCompletedStepCount;
  public string ActiveOperationDisplayName;
  public string ActiveOperationKind;
  public string ActiveStepKind;
  public string ActiveStepTitle;
  public string ActiveStepDescription;
  public bool ActiveStepRequiresBattle;
  public bool HasPendingAbyssStepReceipt;
  public bool HasPendingAbyssBattleReceipt;
  public string PendingAbyssOutcome;
  public string PendingAbyssReward;
  public int TowerTrackPhase;
  public IReadOnlyList<string> TowerTrackLabels=Array.Empty<string>();
  public bool TowerBattleInProgress;
  public bool TowerBattleRewardAwaitingClaim;
  public bool TowerBattleResolved;
  public bool TowerBattleWon;
  public int HighestClearedTowerFloor;
  public int TotalTowerClears;
  public int TowerFloorNumber;
  // Player-facing actual floor is separate from the reusable authored template.
  public int TowerContentTemplateFloor094;
  public bool TowerUsesNewFloorPolicy094;
  public string TowerAuthorityError094;
  public int TowerLastHeroRewardFloor094;
  public bool TowerLastHeroRewardWon094;
  public string TowerLastHeroRewardSummary094;
  public int TowerRunNumber;
  public int TowerExpectedEnemyUnions;
  public int TowerGuildXpReward;
  public int TowerHallXpReward;
  public IReadOnlyList<string> TowerRewardMaterialIds108=Array.Empty<string>();
  public string TowerFloorId;
  public string TowerFloorDisplayName;
  public string TowerOperationId;
  public string TowerOperationDisplayName;
  public string TowerRewardSummary;
  public string TowerArtResourcePath;
  // A committed data-only seam for BOARD_TOWER_ENHANCEMENT_PACK_001. Only the
  // most recently applied room's narrative identity is surfaced; its unimplemented
  // effect/reward prose remains hidden and cannot create a parallel Tower resolver.
  public string TowerRoomModuleId001;
  public string TowerRoomModuleDisplayName001;
  public string TowerRoomModuleNarrative001;
  public int TowerRoomModuleRevealedStepNumber001;
  public IReadOnlyList<AbyssOperationView084> TowerOperationChoices=
   Array.Empty<AbyssOperationView084>();
  public IReadOnlyList<ProgressionItemView022> Equipment=Array.Empty<ProgressionItemView022>();
  public IReadOnlyList<ClassCertificationView022> ClassStates=Array.Empty<ClassCertificationView022>();
  public IReadOnlyList<AbyssFloorView022> FloorStates=Array.Empty<AbyssFloorView022>();
  public IReadOnlyList<InvocationArtifactBaseView022> InvocationArtifactBases=Array.Empty<InvocationArtifactBaseView022>();
  public IReadOnlyList<InvocationAffixView022> InvocationAffixes=Array.Empty<InvocationAffixView022>();
  public IReadOnlyList<ArtifactView022> Artifacts=Array.Empty<ArtifactView022>();
  public IReadOnlyList<CovenantView022> CovenantStates=Array.Empty<CovenantView022>();
 }
 public sealed class ProgressionItemView022 { public string ItemInstanceId; public string DisplayName; public string TrackId; public string TierId; public int MeaningfulUses; public int MasteryPoints; public bool IsEquipped; }
 public sealed class ClassCertificationView022 { public string RecruitId; public string ActiveClassId; public int CertifiedCount; }
 public sealed class AbyssFloorView022
 {
  public string FloorId;
  public string DisplayName;
  public int FloorNumber;
  public int ClearCount;
  public bool IsUnlocked;
  public bool IsCurrentGoal;
  public string Status;
  public string AboveGroundChangeId;
 }
 public sealed class AbyssOperationView084
 {
  public string OperationId;
  public string FloorId;
  public int FloorNumber;
  public string FloorDisplayName;
  public string DisplayName;
  public string Kind;
  public string PlayerKind;
  public int StepCount;
  public bool RequiresBattle;
  public bool FirstClearOnly;
  public bool Available;
  public string LockedReason;
  public string RewardSummary;
 }
 public sealed class InvocationArtifactBaseView022 { public string BaseId; public string DisplayName; public string WeaponFamilyId; public IReadOnlyList<string> MaterialCosts=Array.Empty<string>(); }
 public sealed class InvocationAffixView022 { public string AffixId; public string DisplayName; public string ForecastCategory; public int EffectPermille; }
 public sealed class ArtifactView022 { public string InstanceId; public string DisplayName; public int EvolutionStage; public int Resonance; public string EvolutionPathId; public IReadOnlyList<string> AffixIds=Array.Empty<string>(); }
 public sealed class CovenantView022 { public string CovenantId; public string DisplayName; public string Role; public string Status; public int TrialProgress; public int Trust; public int TrialReceiptCount; public bool CanAdvanceTrial; public bool CanAccept; public bool AcceptanceReceiptApplied; }

 public static class TowerRunRules081
 {
  public const int OpeningFloorCount=10;
  public const string FloorOneCampaign083BattleArtResourcePath=
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_01_MUD_TRENCHES_BATTLE_083";
  public const string FloorOneCampaign083BattleArtTextureName=
   "ABYSS_FLOOR_01_MUD_TRENCHES_BATTLE_083";
  public static readonly IReadOnlyList<string> ProductionBattleArtPaths084=new[]
  {
   FloorOneCampaign083BattleArtResourcePath,
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_02_ARROW_RAIN_FIELD_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_03_SILENT_CAMP_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_04_COLLAPSED_SIEGE_WALL_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_05_GRAVE_BANNER_HILL_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_06_IRON_MARCH_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_07_BLOODLESS_RIVER_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_08_WAR_BEAST_PENS_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_09_ASH_COMMAND_TENT_BATTLE_084",
   "SecondDimension/Art/Campaign083/ABYSS_FLOOR_10_AEGIS_GATE_BATTLE_084"
  };
  public static readonly IReadOnlyList<string> ProductionBattleArtSha256084=new[]
  {
   "0EEC14FC782F262B98578BEF3B41C17A3A585400DCBB20A6E374F1D28582E633",
   "BE86B18582B038152B4D33857AA781DEADC7A2BB58FC7557C247EBE68E30325A",
   "4D5D4F05F7A20D5774FB59672C073FEFF712466CA6576DA52614DFF33E021A44",
   "5D981B56D29362F8F5A3A4340F1F3E1AC833A8875B538A8E7A3C0237B865AB0B",
   "713F396CC1D27D6F8B4509FEF553DAC39F9B62A1849CA0A22CA5DBD4C79B0C09",
   "57E82E5A65413317B431C2F78B96E06E9E8E1A08EF74CE55BAE1C97411231243",
   "7A7806293B3774421DD77BF84CF77B8AD12D20D52AF704D7C25EBF69753D83AF",
   "19373D37019C2C27FC23537D796608CA67D9C7ED0F793FEFA33D6ADFE4E9E427",
   "1345BD9696073351D4A6118145D12E41F7A2D5C42A16E5CD757E46427D225F4B",
   "3ECDB0FB09B997D3E711BA936A4DE16AF5D59FF76204E36ADC09C3AAA6FC7B8B"
  };
  public const float MinimumReadableContrast083=4.5f;

  // Critical Tower text uses these exact foreground/background pairs. Keeping the
  // values beside the stable art identity lets EditMode, PlayMode, and the packaged
  // smoke prove readability instead of relying on a screenshot-only judgement.
  public static readonly Color FirstClimbHeadingColor083=
   new Color(0.945098f,0.772549f,0.431373f,1f);
  public static readonly Color TowerRibbonSurfaceColor083=
   new Color(0.020f,0.035f,0.045f,1f);
  public static readonly Color LightActionSurfaceColor083=
   new Color(0.819608f,0.682353f,0.4f,1f);
  public static readonly Color LightActionDisabledSurfaceColor083=
   new Color(0.72f,0.61f,0.38f,1f);
  public static readonly Color LightSurfaceInkColor083=
   new Color(0.035f,0.040f,0.045f,1f);
  public static readonly Color ForecastSurfaceColor083=
   new Color(0.075f,0.125f,0.19f,1f);
  public static readonly Color ForecastTextColor083=
   new Color(0.929412f,0.952941f,1f,1f);

  public static int NextFloorNumber(int highestClearedFloor)
   =>Math.Max(1,Math.Min(OpeningFloorCount,highestClearedFloor+1));

  public static int RunNumber(int highestClearedFloor,int floorTenClearCount)
   =>highestClearedFloor<OpeningFloorCount?1:Math.Max(2,floorTenClearCount+1);

  public static string OperationId(int floorNumber,bool firstClear)
   =>"ABYSS_OP022_"+Math.Max(1,Math.Min(OpeningFloorCount,floorNumber)).ToString("00")+(firstClear?"_GUARDIAN":"_TRIAL");

  public static string ArtResourcePath(int floorNumber)
  {
   var index=Math.Max(1,Math.Min(OpeningFloorCount,floorNumber))-1;
   return ProductionBattleArtPaths084[index];
  }

  public static string ArtSha256084(int floorNumber)
  {
   var index=Math.Max(1,Math.Min(OpeningFloorCount,floorNumber))-1;
   return ProductionBattleArtSha256084[index];
  }

  public static float ContrastRatio083(Color foreground,Color background)
  {
   var lighter=Mathf.Max(RelativeLuminance083(foreground),RelativeLuminance083(background));
   var darker=Mathf.Min(RelativeLuminance083(foreground),RelativeLuminance083(background));
   return (lighter+0.05f)/(darker+0.05f);
  }

  public static bool ContainsForbiddenPlaceholderCopy083(string value)
  {
   if(string.IsNullOrWhiteSpace(value))return false;
   var normalized=value.ToUpperInvariant();
   return normalized.Contains("SCHEMATIC MAP")||
          normalized.Contains("FINAL ILLUSTRATION MAY REPLACE")||
          normalized.Contains("PLACEHOLDER ART");
  }

  static float RelativeLuminance083(Color color)
   =>0.2126f*LinearChannel083(color.r)+
     0.7152f*LinearChannel083(color.g)+
     0.0722f*LinearChannel083(color.b);

  static float LinearChannel083(float channel)
  {
   var clamped=Mathf.Clamp01(channel);
   return clamped<=0.04045f
    ?clamped/12.92f
    :Mathf.Pow((clamped+0.055f)/1.055f,2.4f);
  }
 }

 public static class InvocationAffixSelectionRules022
 {
  public const int PageSize=8;
  public const int MaximumSelected=2;
  public static int PageCount(int itemCount)=>Math.Max(1,(Math.Max(0,itemCount)+PageSize-1)/PageSize);
  public static int ClampPage(int page,int itemCount)=>Math.Max(0,Math.Min(PageCount(itemCount)-1,page));
  public static IReadOnlyList<InvocationAffixView022> Page(IReadOnlyList<InvocationAffixView022> values,int page)
  {
   var source=values??Array.Empty<InvocationAffixView022>();var safe=ClampPage(page,source.Count);var result=new List<InvocationAffixView022>();for(var index=safe*PageSize;index<source.Count&&result.Count<PageSize;index++)if(source[index]!=null)result.Add(source[index]);return result.AsReadOnly();
  }
  public static IReadOnlyList<string> Toggle(IReadOnlyList<string> selected,string affixId)
  {
   var result=new List<string>();if(selected!=null)for(var index=0;index<selected.Count;index++)if(!string.IsNullOrWhiteSpace(selected[index])&&!result.Contains(selected[index]))result.Add(selected[index]);
   if(string.IsNullOrWhiteSpace(affixId)){result.Sort(StringComparer.Ordinal);return result.AsReadOnly();}
   if(result.Contains(affixId))result.Remove(affixId);else if(result.Count<MaximumSelected)result.Add(affixId);
   result.Sort(StringComparer.Ordinal);return result.AsReadOnly();
  }
 }
}
