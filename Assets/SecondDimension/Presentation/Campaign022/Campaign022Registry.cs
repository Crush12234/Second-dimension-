using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Presentation.Campaign022
{
    public sealed class CampaignRegistry022 : ICampaignRegistry022
    {
        public CampaignManifestDto022 Manifest{get;private set;} public IReadOnlyDictionary<string,WeaponTrackDto022> WeaponTracks{get;private set;} public IReadOnlyDictionary<string,WeaponRecipeDto022> WeaponRecipes{get;private set;} public IReadOnlyDictionary<string,ArmorRecipeDto022> ArmorRecipes{get;private set;} public IReadOnlyDictionary<string,ClassProfileDto022> Classes{get;private set;} public IReadOnlyDictionary<string,CertificationPathDto022> CertificationPaths{get;private set;} public IReadOnlyDictionary<string,AbyssFloorDto022> Floors{get;private set;} public IReadOnlyDictionary<string,AbyssOperationDto022> AbyssOperations{get;private set;} public IReadOnlyDictionary<string,ArtifactBaseDto022> ArtifactBases{get;private set;} public IReadOnlyDictionary<string,AffixDto022> Affixes{get;private set;} public IReadOnlyDictionary<string,ArtifactPathDto022> ArtifactPaths{get;private set;} public IReadOnlyDictionary<string,EchoDto022> Echoes{get;private set;} public IReadOnlyDictionary<string,CovenantDto022> Covenants{get;private set;} public IReadOnlyDictionary<string,UnlockDto022> Unlocks{get;private set;}
        public static CampaignRegistry022 LoadFromResources(){var r=new CampaignRegistry022();r.Manifest=Load<CampaignManifestDto022>("SecondDimension/Campaign022/Data/CampaignManifest022");var w=Load<WeaponTrackRoot022>("SecondDimension/Campaign022/Data/WeaponEvolutionTracks022");r.WeaponTracks=(w.tracks??Array.Empty<WeaponTrackDto022>()).ToDictionary(x=>x.trackId,StringComparer.Ordinal);r.WeaponRecipes=(w.recipes??Array.Empty<WeaponRecipeDto022>()).ToDictionary(x=>x.recipeId,StringComparer.Ordinal);r.ArmorRecipes=(Load<ArmorRoot022>("SecondDimension/Campaign022/Data/ArmorEvolutionRecipes022").recipes??Array.Empty<ArmorRecipeDto022>()).ToDictionary(x=>x.recipeId,StringComparer.Ordinal);var classRoot=Load<ClassRoot022>("SecondDimension/Campaign022/Data/AdvancedClassCertifications022");r.Classes=(classRoot.classes??Array.Empty<ClassProfileDto022>()).ToDictionary(x=>x.classId,StringComparer.Ordinal);r.CertificationPaths=(classRoot.certificationPaths??Array.Empty<CertificationPathDto022>()).ToDictionary(x=>x.certificationId,StringComparer.Ordinal);r.Floors=(Load<FloorRoot022>("SecondDimension/Campaign022/Data/AbyssFloors022").floors??Array.Empty<AbyssFloorDto022>()).ToDictionary(x=>x.floorId,StringComparer.Ordinal);r.AbyssOperations=(Load<AbyssOperationRoot022>("SecondDimension/Campaign022/Data/AbyssOperations022").operations??Array.Empty<AbyssOperationDto022>()).Concat(Load<AbyssOperationRoot022>("SecondDimension/Campaign022/Data/EndlessBattleOperations094").operations??Array.Empty<AbyssOperationDto022>()).ToDictionary(x=>x.operationId,StringComparer.Ordinal);var a=Load<ArtifactRoot022>("SecondDimension/Campaign022/Data/InvocationArtifacts022");r.ArtifactBases=(a.bases??Array.Empty<ArtifactBaseDto022>()).ToDictionary(x=>x.baseId,StringComparer.Ordinal);r.Affixes=(Load<AffixRoot022>("SecondDimension/Campaign022/Data/InvocationAffixes022").affixes??Array.Empty<AffixDto022>()).ToDictionary(x=>x.affixId,StringComparer.Ordinal);r.ArtifactPaths=(Load<ArtifactPathRoot022>("SecondDimension/Campaign022/Data/ArtifactEvolutionPaths022").paths??Array.Empty<ArtifactPathDto022>()).ToDictionary(x=>x.pathId,StringComparer.Ordinal);r.Echoes=(Load<EchoRoot022>("SecondDimension/Campaign022/Data/SummonEchoes022").echoes??Array.Empty<EchoDto022>()).ToDictionary(x=>x.echoId,StringComparer.Ordinal);r.Covenants=(Load<CovenantRoot022>("SecondDimension/Campaign022/Data/GreatCovenants022").covenants??Array.Empty<CovenantDto022>()).ToDictionary(x=>x.covenantId,StringComparer.Ordinal);r.Unlocks=(Load<UnlockRoot022>("SecondDimension/Campaign022/Data/EndgameUnlocks022").unlocks??Array.Empty<UnlockDto022>()).ToDictionary(x=>x.unlockId,StringComparer.Ordinal);r.ValidateOrThrow();return r;}
        public void ValidateOrThrow()
        {
            if(Manifest==null||Manifest.saveFormatVersion!=SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion)throw new InvalidOperationException("Campaign 022 save format mismatch.");
            if(WeaponTracks.Count!=12||WeaponRecipes.Count!=72||ArmorRecipes.Count!=144||Classes.Count!=18||CertificationPaths.Count!=30||Floors.Count!=10||AbyssOperations.Count!=40||ArtifactBases.Count!=24||Affixes.Count!=30||ArtifactPaths.Count!=12||Echoes.Count!=10||Covenants.Count!=8||Unlocks.Count!=10)throw new InvalidOperationException("Campaign 022 content counts invalid.");
            if(AbyssOperations.Values.Count(operation=>operation.kind==CampaignProgressionCommandService022.EndlessBattleKind094)!=10||
               AbyssOperations.Values.Count(operation=>operation.kind!=CampaignProgressionCommandService022.EndlessBattleKind094)!=30)
                throw new InvalidOperationException("Tower094 repeat definitions must extend, not replace, the thirty historical operations.");
            if(CertificationPaths.Values.Any(x=>!Classes.ContainsKey(x.advancedClassId)))throw new InvalidOperationException("Campaign 022 class-route reference invalid.");
            if(Covenants.Values.Any(x=>x.sentientOwnershipAllowed||!x.voluntaryAcceptanceRequired))throw new InvalidOperationException("Covenant ownership law violated.");
            if(Echoes.Values.Any(x=>x.directIndividualSelection))throw new InvalidOperationException("Individual summon selection is forbidden.");
            if(Floors.Values.Select(x=>x.floor).Distinct().Count()!=Floors.Count||Floors.Values.Any(x=>x.floor<1||x.floor>10||string.IsNullOrWhiteSpace(x.firstClearAboveGroundChangeId)))throw new InvalidOperationException("Abyss floor authority law violated.");
            foreach(var operation in AbyssOperations.Values)
            {
                if(operation==null||string.IsNullOrWhiteSpace(operation.operationId)||!Floors.TryGetValue(operation.floorId,out var floor))throw new InvalidOperationException("Abyss operation floor authority law violated.");
                if(operation.requiresPreviousFloorClear!=(floor.floor>1))throw new InvalidOperationException("Abyss previous-floor law violated.");
                if(operation.guildXp<=0||operation.hallXp<=0||operation.summonResonance<0||operation.rewardMaterialIds==null||operation.rewardMaterialIds.Length==0||operation.rewardMaterialIds.Any(string.IsNullOrWhiteSpace))throw new InvalidOperationException("Abyss positive reward law violated.");
                if(operation.rewardMaterialIds.Distinct(StringComparer.Ordinal).Count()!=operation.rewardMaterialIds.Length)throw new InvalidOperationException("Abyss material IDs must be unique.");
                if(!operation.exactOnceReceipts||operation.steps==null||operation.steps.Length==0||operation.steps.Any(x=>x==null||!x.exactOnce||string.IsNullOrWhiteSpace(x.stepId))||operation.steps.Select(x=>x.stepId).Distinct(StringComparer.Ordinal).Count()!=operation.steps.Length)throw new InvalidOperationException("Abyss exact-once receipt law violated.");
                if(!operation.existingEquipmentRewardRemainsAuthoritative||operation.steps.Count(x=>x.requiresBattle)>1)throw new InvalidOperationException("Abyss existing-equipment reward authority law violated.");
                if(operation.kind==CampaignProgressionCommandService022.EndlessBattleKind094)
                {
                    var expected=CampaignProgressionCommandService022.TowerOperationDefinitionId094(floor.floor,false);
                    var guardian=AbyssOperations[CampaignProgressionCommandService022.TowerOperationDefinitionId094(floor.floor,true)];
                    var trial=AbyssOperations["ABYSS_OP022_"+floor.floor.ToString("00")+"_TRIAL"];
                    var guardianBattle=guardian.steps.Single(step=>step.requiresBattle);
                    var repeatedBattle=operation.steps.SingleOrDefault(step=>step.requiresBattle);
                    if(operation.operationId!=expected||operation.firstClearOnly||
                       !TowerAdventureRules084.IsCompatible084(operation)||repeatedBattle==null||
                       repeatedBattle.bossId!=guardianBattle.bossId||
                       operation.guildXp!=trial.guildXp||operation.hallXp!=trial.hallXp||
                       operation.summonResonance!=trial.summonResonance||
                       !operation.rewardMaterialIds.SequenceEqual(trial.rewardMaterialIds))
                        throw new InvalidOperationException("Tower094 repeat must retain its guardian encounter and existing trial reward budget.");
                }
            }
            if(Affixes.Values.Any(x=>x==null||!InvocationArtifactAuthority022.IsCanonicalAffixCategory(x.forecastCategory)||x.effectPermille<=0||!x.meaningfulUseRequired||!x.zeroEffectSpamForbidden||x.maximumCopiesPerArtifact!=1))throw new InvalidOperationException("Invocation affix law invalid.");
            if(ArtifactPaths.Values.Any(x=>!InvocationArtifactAuthority022.IsCanonicalPath(x)))throw new InvalidOperationException("Invocation path law invalid.");
            foreach(var artifactBase in ArtifactBases.Values)
            {
                if(!InvocationArtifactAuthority022.TryResolveBaseAndPath(this,artifactBase?.baseId,out _,out _,out var error))throw new InvalidOperationException("Invocation base/path law invalid: "+error);
            }
        }
        static T Load<T>(string path){var asset=Resources.Load<TextAsset>(path);if(asset==null)throw new InvalidOperationException("Missing resource: "+path);var value=JsonConvert.DeserializeObject<T>(asset.text);if(value==null)throw new InvalidOperationException("Invalid resource: "+path);return value;}
    }
}
