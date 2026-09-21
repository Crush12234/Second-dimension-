#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Campaign023;
namespace SecondDimension.Editor.Campaign023
{
 public static class Campaign023ValidationMenu
 {
  [MenuItem("Second Dimension/Campaign 023/Validate World Gate Operations")]
  public static void Validate(){var r=CampaignRegistry023.LoadFromResources();Debug.Log("Campaign 023 PASS — boards "+r.Boards.Count+", travel "+r.Travel.Count+", standing "+r.Standing.Count+".");}
  public static void ValidateFromCommandLine(){try{Validate();EditorApplication.Exit(0);}catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
#endif
