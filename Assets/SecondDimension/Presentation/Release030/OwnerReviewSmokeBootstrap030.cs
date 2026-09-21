using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation.Release030
{
    public sealed class OwnerReviewSmokeBootstrap030 : MonoBehaviour
    {
        private static bool _created;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateWhenExplicitlyRequested()
        {
            if (_created) return;
            var arguments = Environment.GetCommandLineArgs();
            if (!arguments.Any(value =>
                    StringComparer.Ordinal.Equals(value, OwnerReviewSmokeOptions030.InitialFlag) ||
                    StringComparer.Ordinal.Equals(value, OwnerReviewSmokeOptions030.RelaunchFlag))) return;
            _created = true;
            var host = new GameObject("OwnerReviewSmokeBootstrap030");
            DontDestroyOnLoad(host);
            host.AddComponent<OwnerReviewSmokeBootstrap030>();
        }

        private IEnumerator Start()
        {
            yield return null;
            var arguments = Environment.GetCommandLineArgs();
            if (!OwnerReviewSmokeOptions030.TryParse(arguments, Application.persistentDataPath,
                    Application.dataPath, out var options, out var error))
            {
                var exit = WriteInvalidArgumentsReport(arguments, error);
                Debug.LogError("OWNER REVIEW 030 INVALID ARGUMENTS: " + error);
                Quit(exit);
                yield break;
            }

            Debug.Log("OWNER REVIEW 030 START • " + options.RunKind + " • " + options.ReportPath);
            var execution = new OwnerReviewSmokeWorkflow030(options).Execute();
            Debug.Log((execution.ExitCode == OwnerReviewSmokeWorkflow030.ExitPass
                ? "OWNER REVIEW 030 PASS • "
                : "OWNER REVIEW 030 FAIL • ") + options.ReportPath);
            Quit(execution.ExitCode);
        }

        private static int WriteInvalidArgumentsReport(string[] arguments, string error)
        {
            try
            {
                var relaunch = arguments.Any(value =>
                    StringComparer.Ordinal.Equals(value, OwnerReviewSmokeOptions030.RelaunchFlag));
                var evidenceDirectory = Path.Combine(Application.persistentDataPath, "OwnerReviewEvidence030");
                var reportPath = Path.Combine(evidenceDirectory,
                    relaunch ? "built_player_relaunch_030.json" : "built_player_smoke_030.json");
                var report = new OwnerReviewSmokeReport030
                {
                    runKind = relaunch ? "RELAUNCH" : "INITIAL",
                    runStatus = "FAIL",
                    processExitCode = OwnerReviewSmokeWorkflow030.ExitInvalidArguments,
                    currentStage = "Source",
                    startedUtc = DateTime.UtcNow.ToString("O"),
                    completedUtc = DateTime.UtcNow.ToString("O"),
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    executablePath = Application.dataPath,
                    reportPath = reportPath,
                    failure = error ?? "Invalid owner-review command-line arguments."
                };
                report.gates.Add(new OwnerReviewGateEvidence030
                {
                    id = "command_line",
                    stage = "Source",
                    status = "FAIL",
                    detail = report.failure
                });
                OwnerReviewSmokeEvidenceWriter030.Write(reportPath, report);
                return OwnerReviewSmokeWorkflow030.ExitInvalidArguments;
            }
            catch (Exception exception)
            {
                Debug.LogError("OWNER REVIEW 030 FAILURE REPORT WRITE FAILED: " + exception);
                return OwnerReviewSmokeWorkflow030.ExitEvidenceFailure;
            }
        }

        private void Quit(int exitCode)
        {
#if UNITY_EDITOR
            Destroy(gameObject);
#else
            Application.Quit(exitCode);
#endif
        }
    }
}
