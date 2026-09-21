#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Campaign022;
namespace SecondDimension.Editor.Campaign022
{
 public static class Campaign022ValidationMenu
 {
  [MenuItem("Second Dimension/Campaign 022/Validate Progression Abyss Covenants")]
  public static void Validate(){var r=CampaignRegistry022.LoadFromResources();r.ValidateOrThrow();Debug.Log("Campaign Progression/Abyss/Covenant 022 validation PASS");}
  public static void ValidateFromCommandLine(){try{Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
#endif
