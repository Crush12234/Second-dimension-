using System;
using System.IO;
using System.Linq;
using SecondDimension.Presentation.Release030;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    public static class TowerLoop097Batch
    {
        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var targetText = Value(args, "--sd-tower-target=");
                var secondsText = Value(args, "--sd-tower-seconds=");
                var target = string.IsNullOrEmpty(targetText) ? 12 : int.Parse(targetText);
                var seconds = string.IsNullOrEmpty(secondsText) ? 1800 : int.Parse(secondsText);
                var report = new TowerLoopVerification097().Run(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    Value(args, "--sd-tower-source="), Value(args, "--sd-tower-evidence="),
                    target, seconds, args.Contains("--sd-tower-defeat-probe"), Debug.Log);
                Debug.Log("TOWER097 " + report.status + " through=" + report.highestCleared +
                    " originalUnchanged=" + report.sourceUnchanged);
                EditorApplication.Exit(report.status.StartsWith("PASS_", StringComparison.Ordinal) ? 0 : 97);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(98); }
        }
        static string Value(string[] args, string prefix) => args.FirstOrDefault(value =>
            value.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length) ?? "";
    }
}
