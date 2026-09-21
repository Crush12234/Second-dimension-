#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace SecondDimension.Editor.Campaign023
{
 public static class Campaign023WindowsBuild
 {
  public static void BuildFromCommandLine(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  [MenuItem("Second Dimension/Campaign 023/Build Windows Owner Review")]
  public static void Build(){var root=Directory.GetParent(Application.dataPath).FullName;var dir=Path.Combine(root,"Builds","Windows_Owner_Review_023");Directory.CreateDirectory(dir);var scene="Assets/Scenes/Boot.unity";if(!File.Exists(Path.Combine(root,scene)))throw new FileNotFoundException(scene);if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Install Windows Build Support.");var exe=Path.Combine(dir,"SECOND_DIMENSION_GUILD_OF_WORLDS.exe");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=exe,target=BuildTarget.StandaloneWindows64,targetGroup=BuildTargetGroup.Standalone,options=BuildOptions.None});File.WriteAllText(Path.Combine(dir,"BUILD_SUMMARY_023.txt"),"Result: "+report.summary.result+"\nOutput: "+report.summary.outputPath+"\nWarnings: "+report.summary.totalWarnings+"\nErrors: "+report.summary.totalErrors+"\n");File.WriteAllText(Path.Combine(dir,"PLAY_SECOND_DIMENSION.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"SECOND_DIMENSION_GUILD_OF_WORLDS.exe\"\r\n");if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+report.summary.result);if(!Directory.Exists(Path.Combine(dir,"SECOND_DIMENSION_GUILD_OF_WORLDS_Data"))||!File.Exists(Path.Combine(dir,"UnityPlayer.dll")))throw new InvalidOperationException("Incomplete Unity build folder.");}
 }
}
#endif
