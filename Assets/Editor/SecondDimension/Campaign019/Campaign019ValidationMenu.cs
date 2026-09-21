#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Campaign019;
namespace SecondDimension.Editor.Campaign019
{
 public static class Campaign019ValidationMenu
 {
  [MenuItem("Second Dimension/Campaign 019/Validate Runtime Consolidation")]
  public static void ValidateMenu(){ValidateOrThrow();Debug.Log("Campaign Runtime Consolidation 019 validation PASS");}
  public static void ValidateFromCommandLine(){try{ValidateOrThrow();Debug.Log("Campaign Runtime Consolidation 019 validation PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  static void ValidateOrThrow(){var r=CampaignRegistry019.LoadFromResources();r.ValidateOrThrow();if(r.Manifest.saveFormatVersion!=SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion)throw new InvalidOperationException("Save format mismatch");}
 }
}
#endif
