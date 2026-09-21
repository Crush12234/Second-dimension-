using System;
using System.Collections.Generic;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.Creator028
{
 public static class CreatorRecruitProjection028
 {
  public static CreatorRecruitGrant028 FromRecord(OpeningRecruitRecord record,string worldId)
  {
   if(record==null)throw new ArgumentNullException(nameof(record));var equipment=new List<EquipmentItemState>();var assignments=new List<EquipmentSlotAssignmentState>();
   if(record.EquipmentLoadout?.Slots!=null)foreach(var pair in record.EquipmentLoadout.Slots){var item=pair.Value;if(item==null)continue;var slot=Slot(pair.Key);var stateItem=new EquipmentItemState(item.InstanceId,item.ItemDefinitionId,item.ItemDefinitionId,new[]{slot},item.Tags,string.Empty,10000,item.Locked);equipment.Add(stateItem);assignments.Add(new EquipmentSlotAssignmentState(slot,stateItem));}
   var hp=55+record.StatTendencies["HP"].BaseIndex*2;var mp=Math.Max(0,4+(record.StatTendencies["MAGIC"].BaseIndex-40)/3);var recruit=new RecruitState(record.RecruitId,hp,hp,mp,mp,record.DisplayName,StringComparer.Ordinal.Equals(record.SourceType,"SIGNATURE")?RecruitOriginKind.Signature:RecruitOriginKind.Procedural,record.SignatureId,record.RaceId,worldId??record.HomeCommunityId,record.StartingClassId,string.Empty,record.DevelopmentPotentialScore,RecruitAuthorityKind.Normal,CanonicalJson.Serialize(record),string.Empty,new EquipmentLoadoutState(assignments),true,string.Empty,string.Empty,record.LeadershipScore,record.DisciplineAptitudes!=null&&record.DisciplineAptitudes.ContainsKey("TACTICAL")?record.DisciplineAptitudes["TACTICAL"]:0);
   var inventory=new List<EquipmentItemState>();foreach(var item in equipment){var equipped=false;foreach(var a in assignments)if(StringComparer.Ordinal.Equals(a.Item.InstanceId,item.InstanceId)){equipped=true;break;}if(!equipped)inventory.Add(item);}return new CreatorRecruitGrant028(recruit,inventory.AsReadOnly());
  }
  static string Slot(string id){switch(id){case "MAIN_HAND":return EquipmentSlotIds.MainHand;case "OFF_HAND":return EquipmentSlotIds.OffHand;case "BODY":return EquipmentSlotIds.BodyArmor;case "ACCESSORY_1":return EquipmentSlotIds.AccessoryOne;case "ACCESSORY_2":return EquipmentSlotIds.AccessoryTwo;case "TOOL_RELIC":return EquipmentSlotIds.ToolRelic;default:throw new InvalidOperationException("Unsupported creator recruit slot "+id);}}
 }
}
