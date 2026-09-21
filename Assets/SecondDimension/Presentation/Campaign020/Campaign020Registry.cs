using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation.Campaign020
{
 public sealed class CampaignRegistry020
 {
   public Campaign019.CampaignRegistry019 Base019{get;private set;} public CampaignManifest020 Manifest{get;private set;}
   public IReadOnlyDictionary<string,ChapterOperationBlueprint020> Blueprints=>_blueprints; public IReadOnlyDictionary<string,WorldEnemyPack020> EnemyPacks=>_enemy; public IReadOnlyDictionary<string,WorldRecruitPack020> RecruitPacks=>_recruits; public IReadOnlyDictionary<string,WorldLootProfile020> LootProfiles=>_loot; public IReadOnlyDictionary<string,WorldMaterialDefinition020> Materials=>_materials; public IReadOnlyDictionary<string,WorldGateTravelDefinition020> Travel=>_travel; public IReadOnlyDictionary<string,RepeatableContract020> Repeatables=>_repeatables; public IReadOnlyDictionary<string,WorldCrisisOperation020> Crises=>_crises; public IReadOnlyDictionary<string,WorldSettlementOutcome020> Settlements=>_settlements;
   readonly Dictionary<string,ChapterOperationBlueprint020> _blueprints=new Dictionary<string,ChapterOperationBlueprint020>(StringComparer.Ordinal); readonly Dictionary<string,WorldEnemyPack020> _enemy=new Dictionary<string,WorldEnemyPack020>(StringComparer.Ordinal); readonly Dictionary<string,WorldRecruitPack020> _recruits=new Dictionary<string,WorldRecruitPack020>(StringComparer.Ordinal); readonly Dictionary<string,WorldLootProfile020> _loot=new Dictionary<string,WorldLootProfile020>(StringComparer.Ordinal); readonly Dictionary<string,WorldMaterialDefinition020> _materials=new Dictionary<string,WorldMaterialDefinition020>(StringComparer.Ordinal); readonly Dictionary<string,WorldGateTravelDefinition020> _travel=new Dictionary<string,WorldGateTravelDefinition020>(StringComparer.Ordinal); readonly Dictionary<string,RepeatableContract020> _repeatables=new Dictionary<string,RepeatableContract020>(StringComparer.Ordinal); readonly Dictionary<string,WorldCrisisOperation020> _crises=new Dictionary<string,WorldCrisisOperation020>(StringComparer.Ordinal); readonly Dictionary<string,WorldSettlementOutcome020> _settlements=new Dictionary<string,WorldSettlementOutcome020>(StringComparer.Ordinal);
   public static CampaignRegistry020 LoadFromResources(){var r=new CampaignRegistry020{Base019=Campaign019.CampaignRegistry019.LoadFromResources(),Manifest=Load<CampaignManifest020>("SecondDimension/Campaign020/Data/CampaignManifest020")};foreach(var x in Load<ChapterOperationBlueprintsFile020>("SecondDimension/Campaign020/Data/ChapterOperationBlueprints020").blueprints??Array.Empty<ChapterOperationBlueprint020>())r._blueprints.Add(x.chapterId,x);foreach(var x in Load<WorldEnemyPacksFile020>("SecondDimension/Campaign020/Data/WorldEnemyPacks020").packs??Array.Empty<WorldEnemyPack020>())r._enemy.Add(x.worldId,x);foreach(var x in Load<WorldRecruitPacksFile020>("SecondDimension/Campaign020/Data/WorldRecruitPacks020").packs??Array.Empty<WorldRecruitPack020>())r._recruits.Add(x.worldId,x);foreach(var x in Load<WorldLootProfilesFile020>("SecondDimension/Campaign020/Data/WorldLootProfiles020").profiles??Array.Empty<WorldLootProfile020>())r._loot.Add(x.worldId,x);foreach(var x in Load<WorldMaterialsFile020>("SecondDimension/Campaign020/Data/WorldMaterials020").materials??Array.Empty<WorldMaterialDefinition020>())r._materials.Add(x.materialId,x);foreach(var x in Load<WorldGateTravelFile020>("SecondDimension/Campaign020/Data/WorldGateTravel020").routes??Array.Empty<WorldGateTravelDefinition020>())r._travel.Add(x.worldId,x);foreach(var x in Load<RepeatableContractsFile020>("SecondDimension/Campaign020/Data/RepeatableContracts020").contracts??Array.Empty<RepeatableContract020>())r._repeatables.Add(x.contractId,x);foreach(var x in Load<WorldCrisisOperationsFile020>("SecondDimension/Campaign020/Data/WorldCrisisOperations020").operations??Array.Empty<WorldCrisisOperation020>())r._crises.Add(x.crisisId,x);foreach(var x in Load<WorldSettlementOutcomesFile020>("SecondDimension/Campaign020/Data/WorldSettlementOutcomes020").outcomes??Array.Empty<WorldSettlementOutcome020>())r._settlements.Add(x.worldId,x);r.ValidateOrThrow();r.ValidateBattleParity084();return r;}
   static T Load<T>(string path){var a=Resources.Load<TextAsset>(path);if(a==null)throw new InvalidOperationException("Missing Campaign 020 resource: "+path);var v=JsonUtility.FromJson<T>(a.text);if(v==null)throw new InvalidOperationException("Invalid Campaign 020 JSON: "+path);return v;}
   public void ValidateOrThrow(){if(Manifest==null||Manifest.saveFormatVersion<8||Manifest.saveFormatVersion>SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion)throw new InvalidOperationException("Campaign 020 manifest/save version invalid");if(_blueprints.Count!=82)throw new InvalidOperationException("Campaign 020 requires 82 chapter blueprints");if(_enemy.Count!=8||_recruits.Count!=7||_loot.Count!=8||_travel.Count!=8)throw new InvalidOperationException("Campaign 020 region pack counts invalid");if(_materials.Count!=48||_repeatables.Count!=32||_crises.Count!=16)throw new InvalidOperationException("Campaign 020 support content counts invalid");foreach(var b in _blueprints.Values){if(!b.exactOnce||!b.noConquestOnly||!b.existingEquipmentRewardRemainsAuthoritative||b.maximumAlliedUnions>10||b.maximumEnemyUnions>10||b.steps==null||b.steps.Length<4)throw new InvalidOperationException("Invalid chapter blueprint "+b.chapterId);if(!Base019.Chapters.ContainsKey(b.chapterId))throw new InvalidOperationException("Blueprint chapter missing from 019: "+b.chapterId);foreach(var s in b.steps)if(!s.committedBeforeReveal||!s.exactOnce)throw new InvalidOperationException("Uncommitted step "+s.stepId);}foreach(var e in _enemy.Values)if(e.archetypes==null||e.archetypes.Length!=8||e.archetypes.Any(x=>!x.noRacewideEvilFraming))throw new InvalidOperationException("Enemy pack law failed: "+e.worldId);foreach(var r in _recruits.Values)if(r.profiles==null||r.profiles.Length!=8||r.profiles.Any(x=>!x.permanentWhenSigned||x.runtimeGenerativeAi||!x.usesRecruitAutogen010))throw new InvalidOperationException("Recruit pack law failed: "+r.worldId);foreach(var l in _loot.Values)if(l.entries==null||l.entries.Length!=12||!l.exactItemCommittedBeforeResults||!l.noAutoEquip||l.entries.Any(x=>!x.manualEquipOnly||x.createsDuplicateItemCatalog))throw new InvalidOperationException("Loot profile law failed: "+l.worldId);}
   void ValidateBattleParity084()
   {
    foreach(var blueprint in _blueprints.Values)
    {
     if(!Base019.Chapters.TryGetValue(blueprint.chapterId,out var narrative))
      throw new InvalidOperationException(
          "Blueprint chapter missing from 019: "+blueprint.chapterId);
     var battleSteps=(blueprint.steps??Array.Empty<CampaignStepDefinition020>())
         .Count(step=>step.requiresCertifiedBattle);
     var expected=narrative.battleRequired?1:0;
     if(blueprint.requiresCertifiedBattle!=narrative.battleRequired||
        battleSteps!=expected||
        (blueprint.steps??Array.Empty<CampaignStepDefinition020>()).Any(step=>
            step.requiresCertifiedBattle!=StringComparer.Ordinal.Equals(
                step.kind,"CERTIFIED_BATTLE")))
      throw new InvalidOperationException(
          "Campaign 019/020 certified battle authority mismatch: "+
          blueprint.chapterId);
    }
   }
 }
}
