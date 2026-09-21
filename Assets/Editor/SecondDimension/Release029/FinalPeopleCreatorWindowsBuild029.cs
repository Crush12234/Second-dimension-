#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondDimension.Editor.Release029
{
    public static class FinalPeopleCreatorWindowsBuild029
    {
        const string BootScene="Assets/Scenes/Boot.unity";
        const string ExeName="SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        [MenuItem("Second Dimension/Final People Creator 029/Build Windows Owner Review")]
        public static void Build()
        {
            FinalPeopleCreatorValidation029.Validate();
            var project=Directory.GetParent(Application.dataPath).FullName;
            var output=Path.Combine(project,"Builds","Windows_Owner_Review_029");
            if(Directory.Exists(output))Directory.Delete(output,true);Directory.CreateDirectory(output);
            if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Install Windows Build Support for Unity 6000.3.22f1.");
            var started=DateTime.UtcNow;var exe=Path.Combine(output,ExeName);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{BootScene},locationPathName=exe,target=BuildTarget.StandaloneWindows64,targetGroup=BuildTargetGroup.Standalone,options=BuildOptions.None});
            var s=report.summary;File.WriteAllText(Path.Combine(output,"BUILD_SUMMARY_029.txt"),"Result: "+s.result+"\nWarnings: "+s.totalWarnings+"\nErrors: "+s.totalErrors+"\nTime: "+s.totalTime+"\nSize: "+s.totalSize+"\nStartedUtc: "+started.ToString("o")+"\nFinishedUtc: "+DateTime.UtcNow.ToString("o")+"\n");
            if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Windows build failed: "+s.result);
            var data=Path.Combine(output,"SECOND_DIMENSION_GUILD_OF_WORLDS_Data");var player=Path.Combine(output,"UnityPlayer.dll");
            if(!File.Exists(exe)||!Directory.Exists(data)||!File.Exists(player))throw new InvalidOperationException("Complete Unity build folder is required.");
            File.WriteAllText(Path.Combine(output,"PLAY_SECOND_DIMENSION.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \""+ExeName+"\"\r\n");
            File.WriteAllText(Path.Combine(output,"README_PLAY_BUILD_029.txt"),"Keep the entire build folder together. Complete the 029 Creator + People smoke path and save/relaunch proof before approval.\n");
            WriteHashes(output);Debug.Log("029 Windows build succeeded: "+exe);
        }
        public static void BuildFromCommandLine(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        static void WriteHashes(string root){var b=new StringBuilder();foreach(var path in Directory.GetFiles(root,"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)){if(path.EndsWith("BUILD_SHA256_029.txt",StringComparison.OrdinalIgnoreCase))continue;using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())b.Append(string.Concat(sha.ComputeHash(stream).Select(x=>x.ToString("x2")))).Append("  ").Append(path.Substring(root.Length+1).Replace('\\','/')).Append('\n');}File.WriteAllText(Path.Combine(root,"BUILD_SHA256_029.txt"),b.ToString());}
    }
}
#endif
