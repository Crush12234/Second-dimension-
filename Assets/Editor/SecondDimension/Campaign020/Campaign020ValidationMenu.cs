#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Campaign020;

namespace SecondDimension.Editor.Campaign020
{
 public static class Campaign020ValidationMenu
 {
  [MenuItem("Second Dimension/Campaign 020/Validate Playable Operations")]
  public static void Validate(){var r=CampaignRegistry020.LoadFromResources();if(r.Blueprints.Count!=82||r.EnemyPacks.Count!=8||r.RecruitPacks.Count!=7||r.LootProfiles.Count!=8||r.Materials.Count!=48||r.Repeatables.Count!=32||r.Crises.Count!=16)throw new InvalidOperationException("Campaign 020 content counts invalid");Debug.Log("Campaign Playable Operations 020 validation PASS");}
 }
}
#endif
