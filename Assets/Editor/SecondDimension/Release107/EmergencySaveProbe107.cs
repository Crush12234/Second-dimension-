using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Release030;
using SecondDimension.Save;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor.Release107
{
    [Serializable]
    public sealed class EmergencySaveProbeReport107
    {
        public string Schema = "SECOND_DIMENSION_EMERGENCY_SAVE_PROBE_107_1";
        public string Status = "RUNNING";
        public string StartedUtc;
        public string CompletedUtc;
        public string SourceSave;
        public string SourceSha256;
        public string SourceSha256After;
        public string TowerSave;
        public int TowerFloor;
        public int TowerPreparationTransitions;
        public bool TowerBattleStarted;
        public bool TowerReloadPassed;
        public string CampaignSave;
        public string CampaignVerificationStatus;
        public string CompletedChapterId;
        public int RecruitCountBeforeClaim;
        public int PendingRecruitRewardsBeforeClaim;
        public int RecruitCountAfterClaim;
        public List<string> NewlyOwnedRecruitIds = new List<string>();
        public string AssignedRecruitId;
        public int AssignedUnionIndex = -1;
        public bool UnionAssignmentReloadPassed;
        public string NextChapterId;
        public bool NextCampaignActionStarted;
        public string FinalCanonicalHash;
        public string Failure;
    }

    /// <summary>
    /// Runs the shipping coordinator against disposable copies of an earned save.
    /// The source file is hash-checked before and after the probe and is never
    /// passed to a coordinator, so verification cannot mutate the player's save.
    /// </summary>
    public static class EmergencySaveProbe107
    {
        const string SourceArgument = "--sd-emergency-source=";
        const string OutputArgument = "--sd-emergency-output=";
        const string RecruitArgument = "--sd-emergency-recruit=";
        const string ExpectedHashArgument = "--sd-emergency-expected-hash=";

        public static void RunLiveRepair()
        {
            var liveSave = Path.GetFullPath(RequiredArgument107(SourceArgument));
            var output = Path.GetFullPath(RequiredArgument107(OutputArgument));
            var expectedHash = RequiredArgument107(ExpectedHashArgument).ToUpperInvariant();
            var result = new Dictionary<string, object>();
            var exitCode = 1;
            try
            {
                Require107(File.Exists(liveSave), "The live save does not exist.");
                Require107(StringComparer.Ordinal.Equals(FileSha256107(liveSave), expectedHash),
                    "The live save changed after backup; refusing to repair a different checkpoint.");
                Require107(!Directory.Exists(output), "Use a new live-repair evidence directory.");
                Directory.CreateDirectory(output);

                var coordinator = new M1RuntimeCoordinator(null, liveSave);
                var beforeRewards = coordinator.EarnedCampaignRecruits094;
                var beforeProgression = coordinator.CampaignProgression022;
                var beforeRoster = coordinator.State.Recruits.Count;
                Require107(beforeRewards.PendingCount > 0,
                    "The backed-up live save has no pending recruit rewards to repair.");
                Require107(!string.IsNullOrWhiteSpace(beforeProgression.ActiveAbyssOperationId),
                    "The backed-up live save no longer has the Tower boundary reproduced by the probe.");

                RequireCommand107(coordinator.ClaimEarnedCampaignRecruits094(),
                    "repair the live Tower boundary and claim earned recruits");
                var afterRewards = coordinator.EarnedCampaignRecruits094;
                var afterProgression = coordinator.CampaignProgression022;
                var afterRoster = coordinator.State.Recruits.Count;
                Require107(afterRewards.PendingCount == 0,
                    "The live reward migration left pending recruits.");
                Require107(string.IsNullOrWhiteSpace(afterProgression.ActiveAbyssOperationId),
                    "The live reward migration did not close the idle Tower boundary.");
                Require107(afterRoster >= beforeRoster,
                    "The live roster count decreased during repair.");

                var reloaded = new M1RuntimeCoordinator(null, liveSave);
                var loaded = new AtomicSaveStore().ReadWithRecovery(liveSave);
                Require107(loaded.IsSuccess, "The repaired live save failed atomic reload: " +
                    string.Join("; ", loaded.Errors));
                Require107(reloaded.EarnedCampaignRecruits094.PendingCount == 0 &&
                    string.IsNullOrWhiteSpace(reloaded.CampaignProgression022.ActiveAbyssOperationId) &&
                    reloaded.State.Recruits.Count == afterRoster,
                    "The repaired live state did not survive coordinator reload.");

                result["Status"] = "PASS";
                result["LiveSave"] = liveSave;
                result["BeforeSha256"] = expectedHash;
                result["AfterSha256"] = FileSha256107(liveSave);
                result["TowerFloorPreservedAsNextFloor"] = beforeProgression.TowerFloorNumber;
                result["HighestClearedTowerFloor"] = reloaded.CampaignProgression022.HighestClearedTowerFloor;
                result["PendingRewardsBefore"] = beforeRewards.PendingCount;
                result["PendingRewardsAfter"] = afterRewards.PendingCount;
                result["RosterBefore"] = beforeRoster;
                result["RosterAfter"] = afterRoster;
                result["ActiveTowerAfter"] = reloaded.CampaignProgression022.ActiveAbyssOperationId ?? string.Empty;
                result["CanonicalStateHash"] = loaded.Value.CanonicalStateHash;
                exitCode = 0;
            }
            catch (Exception exception)
            {
                result["Status"] = "FAIL";
                result["Failure"] = exception.ToString();
                Debug.LogError("LIVE SAVE REPAIR 107 FAILED: " + exception);
            }
            finally
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "live_save_repair107_report.json"),
                    JsonConvert.SerializeObject(result, Formatting.Indented));
                EditorApplication.Exit(exitCode);
            }
        }

        public static void RunRewardedHeroBattle()
        {
            var source = Path.GetFullPath(RequiredArgument107(SourceArgument));
            var output = Path.GetFullPath(RequiredArgument107(OutputArgument));
            var recruitId = RequiredArgument107(RecruitArgument);
            var sourceHash = FileSha256107(source);
            var result = new Dictionary<string, object>();
            var exitCode = 1;
            try
            {
                Require107(File.Exists(source), "The assigned-roster source save does not exist.");
                Require107(!Directory.Exists(output), "Use a new evidence directory.");
                Directory.CreateDirectory(output);
                var save = Path.Combine(output, "RewardedHeroBattle107.json");
                File.Copy(source, save, false);
                var coordinator = new M1RuntimeCoordinator(null, save);
                RequireCommand107(coordinator.BeginTowerRun081(), "begin rewarded-hero battle probe");
                var transitions = 0;
                while (!coordinator.CampaignProgression022.ActiveStepRequiresBattle && transitions < 32)
                {
                    RequireCommand107(coordinator.AdvanceTowerRun081(),
                        "prepare rewarded-hero battle transition " + (transitions + 1));
                    transitions++;
                }
                Require107(coordinator.CampaignProgression022.ActiveStepRequiresBattle,
                    "The rewarded-hero probe never reached battle.");
                RequireCommand107(coordinator.EnterAbyssBattle022(), "enter rewarded-hero battle");
                var battle = coordinator.State.Battle;
                var present = battle != null && battle.PlayerUnions.SelectMany(value => value.Members)
                    .Any(value => StringComparer.Ordinal.Equals(value.MemberId, recruitId));
                Require107(present, "The newly rewarded Union member was missing from the battle roster.");
                var reloaded = new M1RuntimeCoordinator(null, save);
                var presentAfterReload = reloaded.State.Battle != null &&
                    reloaded.State.Battle.PlayerUnions.SelectMany(value => value.Members)
                        .Any(value => StringComparer.Ordinal.Equals(value.MemberId, recruitId));
                Require107(presentAfterReload,
                    "The newly rewarded Union member disappeared from battle after reload.");
                result["Status"] = "PASS";
                result["RecruitId"] = recruitId;
                result["TowerFloor"] = coordinator.CampaignProgression022.TowerFloorNumber;
                result["PreparationTransitions"] = transitions;
                result["AppearedInBattle"] = true;
                result["AppearedAfterReload"] = true;
                result["SourceSha256"] = sourceHash;
                result["SourceSha256After"] = FileSha256107(source);
                Require107(StringComparer.Ordinal.Equals(sourceHash, (string)result["SourceSha256After"]),
                    "The assigned-roster source changed during its isolated battle probe.");
                exitCode = 0;
            }
            catch (Exception exception)
            {
                result["Status"] = "FAIL";
                result["Failure"] = exception.ToString();
                Debug.LogError("REWARDED HERO BATTLE PROBE 107 FAILED: " + exception);
            }
            finally
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "rewarded_hero_battle107_report.json"),
                    JsonConvert.SerializeObject(result, Formatting.Indented));
                EditorApplication.Exit(exitCode);
            }
        }

        public static void Run()
        {
            var report = new EmergencySaveProbeReport107 { StartedUtc = DateTime.UtcNow.ToString("O") };
            var exitCode = 1;
            string output = null;
            try
            {
                var source = Path.GetFullPath(RequiredArgument107(SourceArgument));
                output = Path.GetFullPath(RequiredArgument107(OutputArgument));
                Require107(File.Exists(source), "The earned source save does not exist: " + source);
                Require107(!StringComparer.OrdinalIgnoreCase.Equals(
                    Path.GetDirectoryName(source)?.TrimEnd(Path.DirectorySeparatorChar),
                    output.TrimEnd(Path.DirectorySeparatorChar)),
                    "The evidence directory must be separate from the source save directory.");
                Require107(!Directory.Exists(output),
                    "The evidence directory already exists; use a new path so no prior result can be overwritten.");
                Directory.CreateDirectory(output);
                report.SourceSave = source;
                report.SourceSha256 = FileSha256107(source);

                RunTowerProbe107(source, output, report);
                RunCampaignRecruitUnionProbe107(source, output, report);

                report.SourceSha256After = FileSha256107(source);
                Require107(StringComparer.Ordinal.Equals(report.SourceSha256, report.SourceSha256After),
                    "The source save changed during the isolated probe.");
                report.Status = "PASS";
                exitCode = 0;
            }
            catch (Exception exception)
            {
                report.Status = "FAIL";
                report.Failure = exception.ToString();
                Debug.LogError("EMERGENCY SAVE PROBE 107 FAILED: " + exception);
            }
            finally
            {
                report.CompletedUtc = DateTime.UtcNow.ToString("O");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    try
                    {
                        Directory.CreateDirectory(output);
                        File.WriteAllText(Path.Combine(output, "emergency_save_probe107_report.json"),
                            JsonConvert.SerializeObject(report, Formatting.Indented));
                    }
                    catch (Exception writeException)
                    {
                        Debug.LogError("Could not write emergency probe report: " + writeException);
                        exitCode = 1;
                    }
                }
                Debug.Log("EMERGENCY SAVE PROBE 107 " + report.Status);
                EditorApplication.Exit(exitCode);
            }
        }

        static void RunTowerProbe107(string source, string output, EmergencySaveProbeReport107 report)
        {
            var towerDirectory = Path.Combine(output, "tower");
            Directory.CreateDirectory(towerDirectory);
            var towerSave = Path.Combine(towerDirectory, "TowerSave107.json");
            File.Copy(source, towerSave, false);
            report.TowerSave = towerSave;

            var coordinator = new M1RuntimeCoordinator(null, towerSave);
            var before = coordinator.CampaignProgression022;
            Require107(before.IsAvailable, "Tower projection unavailable: " + before.Error);
            Require107(!string.IsNullOrWhiteSpace(before.ActiveAbyssOperationId),
                "The earned source save has no active Tower operation to test.");
            Require107(before.TowerFloorNumber > 0, "The active Tower floor number is invalid.");
            report.TowerFloor = before.TowerFloorNumber;

            // The player-facing Tower-only action commits and applies every
            // remaining authored preparation room before it enters combat.
            // Resume the earned mid-floor checkpoint through that same public
            // command sequence instead of assuming the stored step is combat.
            for (var transition = 0;
                 !coordinator.CampaignProgression022.ActiveStepRequiresBattle && transition < 32;
                 transition++)
            {
                RequireCommand107(coordinator.AdvanceTowerRun081(),
                    "advance earned Tower preparation transition " + (transition + 1));
                report.TowerPreparationTransitions++;
            }
            Require107(coordinator.CampaignProgression022.ActiveStepRequiresBattle,
                "The earned Tower floor did not reach its certified battle step.");

            RequireCommand107(coordinator.EnterAbyssBattle022(),
                "start earned Tower floor " + before.TowerFloorNumber);
            var started = coordinator.CampaignProgression022;
            report.TowerBattleStarted = started.TowerBattleInProgress &&
                started.TowerFloorNumber == before.TowerFloorNumber;
            Require107(report.TowerBattleStarted,
                "The Tower encounter command succeeded but did not expose an in-progress battle on the same floor.");

            var reloaded = new M1RuntimeCoordinator(null, towerSave);
            var afterReload = reloaded.CampaignProgression022;
            report.TowerReloadPassed = afterReload.IsAvailable &&
                afterReload.TowerBattleInProgress &&
                afterReload.TowerFloorNumber == before.TowerFloorNumber &&
                reloaded.State.Battle != null;
            Require107(report.TowerReloadPassed,
                "The started Tower encounter did not survive save and reload.");
        }

        static void RunCampaignRecruitUnionProbe107(string source, string output,
            EmergencySaveProbeReport107 report)
        {
            var campaignDirectory = Path.Combine(output, "campaign");
            var deep = new DeepCampaignVerification093().Run(
                null, source, campaignDirectory, 1,
                message => Debug.Log("EMERGENCY CAMPAIGN 107: " + message));
            report.CampaignVerificationStatus = deep.status;
            report.CampaignSave = deep.isolatedSave;
            Require107(StringComparer.Ordinal.Equals(deep.status, "PASS"),
                "The real Chapter 1 loop did not complete: " + deep.failure);
            report.CompletedChapterId = "CH018_001";

            var coordinator = new M1RuntimeCoordinator(null, deep.isolatedSave);
            var beforeIds = new HashSet<string>(
                coordinator.State.Recruits.Select(value => value.RecruitId), StringComparer.Ordinal);
            report.RecruitCountBeforeClaim = beforeIds.Count;
            var rewards = coordinator.EarnedCampaignRecruits094;
            report.PendingRecruitRewardsBeforeClaim = rewards.PendingCount;
            Require107(rewards.PendingCount > 0, "Chapter completion exposed no earned recruit rewards.");
            Require107(rewards.CanClaim,
                "Chapter recruit rewards remained blocked after the Tower boundary was closed: " + rewards.Summary);

            RequireCommand107(coordinator.ClaimEarnedCampaignRecruits094(),
                "claim earned Campaign recruit rewards");
            var afterClaim = coordinator.State;
            report.RecruitCountAfterClaim = afterClaim.Recruits.Count;
            report.NewlyOwnedRecruitIds = afterClaim.Recruits.Select(value => value.RecruitId)
                .Where(value => !beforeIds.Contains(value))
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
            Require107(report.NewlyOwnedRecruitIds.Count > 0,
                "The reward claim succeeded but no newly owned roster hero was created.");
            Require107(coordinator.EarnedCampaignRecruits094.PendingCount == 0,
                "The recruit reward claim left pending rewards behind.");

            var union = afterClaim.Unions.FirstOrDefault(value => value.MemberRecruitIds.Count < 6);
            Require107(union != null, "No normal Union has an open slot for the earned hero.");
            var recruitId = report.NewlyOwnedRecruitIds[0];
            RequireCommand107(coordinator.AssignRecruitToUnion(
                recruitId, union.Index, union.MemberRecruitIds.Count),
                "assign newly earned hero to an open Union slot");
            report.AssignedRecruitId = recruitId;
            report.AssignedUnionIndex = union.Index;

            var reloaded = new M1RuntimeCoordinator(null, deep.isolatedSave);
            report.UnionAssignmentReloadPassed = reloaded.State.Unions.Any(value =>
                value.Index == union.Index && value.MemberRecruitIds.Contains(recruitId));
            Require107(report.UnionAssignmentReloadPassed,
                "The earned hero's Union assignment did not survive save and reload.");

            var next = reloaded.Campaign019.Chapters.FirstOrDefault(value =>
                StringComparer.OrdinalIgnoreCase.Equals(value.Status, "AVAILABLE"));
            Require107(next != null, "No next Campaign chapter is available after Chapter 1 completion.");
            report.NextChapterId = next.ChapterId;
            RequireCommand107(reloaded.StartPlayableChapter020(next.ChapterId),
                "start next playable Campaign chapter");
            var finalReload = new M1RuntimeCoordinator(null, deep.isolatedSave);
            report.NextCampaignActionStarted =
                StringComparer.Ordinal.Equals(finalReload.Campaign019.ActiveChapterId, next.ChapterId) &&
                StringComparer.Ordinal.Equals(finalReload.CampaignPlayable020.ActiveChapterId, next.ChapterId) &&
                !string.IsNullOrWhiteSpace(finalReload.CampaignPlayable020.ActiveOperationId);
            Require107(report.NextCampaignActionStarted,
                "The next Campaign action did not persist as a playable operation.");

            var saved = new AtomicSaveStore().ReadWithRecovery(deep.isolatedSave);
            Require107(saved.IsSuccess,
                "The final isolated save could not be read: " + string.Join("; ", saved.Errors));
            report.FinalCanonicalHash = saved.Value.CanonicalStateHash;
        }

        static string RequiredArgument107(string prefix)
        {
            var value = Environment.GetCommandLineArgs().FirstOrDefault(argument =>
                argument.StartsWith(prefix, StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(value) || value.Length <= prefix.Length)
                throw new ArgumentException("Missing required command-line argument " + prefix + "<path>");
            return value.Substring(prefix.Length).Trim('"');
        }

        static void RequireCommand107(M1CommandResult result, string action)
        {
            Require107(result != null && result.Succeeded,
                action + " failed: " + (result?.Message ?? "no command result"));
        }

        static void Require107(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static string FileSha256107(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
    }
}
