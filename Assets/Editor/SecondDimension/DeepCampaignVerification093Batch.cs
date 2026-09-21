using System;
using System.IO;
using System.Linq;
using SecondDimension.Presentation.Release030;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    public static class DeepCampaignVerification093Batch
    {
        // Explicit command-line QA only; not an editor startup hook or player save migration.
        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var source = Value(args, "--sd-deep-source=");
                var evidence = Value(args, "--sd-deep-evidence=");
                var targetText = Value(args, "--sd-deep-target=");
                var target = string.IsNullOrEmpty(targetText) ? 50 : int.Parse(targetText);
                var report = new DeepCampaignVerification093().Run(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    source, evidence, target, message => Debug.Log(message));
                Debug.Log("DEEP CAMPAIGN 093 " + report.status + " through=" + report.completedThroughChapter +
                    " evidence=" + report.evidenceDirectory);
                EditorApplication.Exit(report.status == "PASS" ? 0 : 93);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(94);
            }
        }

        private static string Value(string[] arguments, string prefix) =>
            arguments.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length) ?? "";
    }
}
