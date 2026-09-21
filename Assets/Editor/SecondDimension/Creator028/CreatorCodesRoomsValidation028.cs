#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SecondDimension.Presentation.Creator028;
namespace SecondDimension.Editor.Creator028
{
 public static class CreatorCodesRoomsValidation028
 {
  [MenuItem("Second Dimension/Creator 028/Validate Codes and Rooms")]
  public static void Validate(){SecondDimension.Editor.Release026.FinalExecutionValidation026.Validate();var r=CreatorRegistry028.Load();if(r.CodeCount!=300||r.RoomCount!=133)throw new InvalidOperationException("Creator 028 count gate failed.");Debug.Log("CREATOR CODES & ROOMS 028 VALIDATION: PASS — 300 codes, 133 rooms.");}
  public static void ValidateFromCommandLine(){try{Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
 public static class CreatorCodesRoomsWindowsBuild028
 {
  const string BootScene="Assets/Scenes/Boot.unity";const string ExeName="SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
  [MenuItem("Second Dimension/Creator 028/Build Windows Owner Review")]
  public static void Build(){CreatorCodesRoomsValidation028.Validate();var project=Directory.GetParent(Application.dataPath).FullName;var output=Path.Combine(project,"Builds","Windows_Owner_Review_028");if(Directory.Exists(output))Directory.Delete(output,true);Directory.CreateDirectory(output);if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.22f1.");var started=DateTime.UtcNow;var exe=Path.Combine(output,ExeName);var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{BootScene},locationPathName=exe,target=BuildTarget.StandaloneWindows64,targetGroup=BuildTargetGroup.Standalone,options=BuildOptions.None});var summary=report.summary;File.WriteAllText(Path.Combine(output,"BUILD_SUMMARY_028.txt"),"Result: "+summary.result+"\nWarnings: "+summary.totalWarnings+"\nErrors: "+summary.totalErrors+"\nTime: "+summary.totalTime+"\nSize: "+summary.totalSize+"\nStartedUtc: "+started.ToString("o")+"\nFinishedUtc: "+DateTime.UtcNow.ToString("o")+"\n");if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Windows build failed: "+summary.result);var data=Path.Combine(output,"SECOND_DIMENSION_GUILD_OF_WORLDS_Data");var player=Path.Combine(output,"UnityPlayer.dll");if(!File.Exists(exe)||!Directory.Exists(data)||!File.Exists(player))throw new InvalidOperationException("Complete Unity build folder is required.");File.WriteAllText(Path.Combine(output,"PLAY_SECOND_DIMENSION.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \""+ExeName+"\"\r\n");WriteHashes(output);}
  public static void BuildFromCommandLine(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  static void WriteHashes(string root){var output=new StringBuilder();foreach(var path in Directory.GetFiles(root,"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)){if(path.EndsWith("BUILD_SHA256_028.txt",StringComparison.OrdinalIgnoreCase))continue;using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())output.Append(string.Concat(sha.ComputeHash(stream).Select(x=>x.ToString("x2")))).Append("  ").Append(path.Substring(root.Length+1).Replace('\\','/')).Append('\n');}File.WriteAllText(Path.Combine(root,"BUILD_SHA256_028.txt"),output.ToString());}
 }
}
#endif
