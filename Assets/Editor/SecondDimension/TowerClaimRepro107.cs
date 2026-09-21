using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Release030;
using SecondDimension.Save;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    public static class TowerClaimRepro107
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var source = Value(args, "--sd-repro-source=");
            var output = Value(args, "--sd-repro-output=");
            string originalHash = null;
            try
            {
                var isolated = DeepCampaignVerification093.IsolatedSavePath093(source, output);
                if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                    throw new IOException("Fresh reproduction output required.");
                originalHash = Hash(source);
                Directory.CreateDirectory(output);
                File.Copy(source, isolated, false);
                var store = new AtomicSaveStore();
                var coordinator = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath,
                    "Authority", "CONTENT"), isolated);
                var initial = coordinator.CampaignProgression022;
                var claimed = coordinator.ClaimBattleRewards();
                if (!claimed.Succeeded) throw new InvalidOperationException("CLAIM: " + claimed.Message);
                var after = store.ReadWithRecovery(isolated);
                if (!after.IsSuccess) throw new InvalidOperationException(string.Join(" | ", after.Errors));
                var once = CanonicalJson.Sha256Hex(after.Value.CampaignState);
                var replay = coordinator.ClaimBattleRewards();
                var replayed = store.ReadWithRecovery(isolated);
                if (!replay.Succeeded || !replayed.IsSuccess ||
                    CanonicalJson.Sha256Hex(replayed.Value.CampaignState) != once)
                    throw new InvalidOperationException("Claim replay changed rewards: " + replay.Message);
                for (var step = 0; step < 32 && !string.IsNullOrEmpty(
                    coordinator.CampaignProgression022.ActiveAbyssOperationId); step++)
                {
                    var advanced = coordinator.AdvanceTowerRun081();
                    if (!advanced.Succeeded) throw new InvalidOperationException("BANK: " + advanced.Message);
                }
                var banked = coordinator.CampaignProgression022;
                if (!string.IsNullOrEmpty(banked.ActiveAbyssOperationId))
                    throw new InvalidOperationException("Floor did not finish in bounded transitions.");
                var begin = coordinator.BeginTowerRun081();
                if (!begin.Succeeded) throw new InvalidOperationException("NEXT FLOOR: " + begin.Message);
                for (var step = 0; step < 32; step++)
                {
                    var view = coordinator.CampaignProgression022;
                    if (view.ActiveAbyssStatus == "Active" && view.ActiveStepRequiresBattle) break;
                    var advanced = coordinator.AdvanceTowerRun081();
                    if (!advanced.Succeeded) throw new InvalidOperationException("PREPARE: " + advanced.Message);
                }
                var entered = coordinator.EnterAbyssBattle022();
                if (!entered.Succeeded) throw new InvalidOperationException("ENTER: " + entered.Message);
                var final = store.ReadWithRecovery(isolated).Value.CampaignState;
                if (Hash(source) != originalHash) throw new InvalidOperationException("Original source changed.");
                File.WriteAllText(Path.Combine(output, "REPORT107.json"), JsonConvert.SerializeObject(new {
                    status = "PASS_CLAIM_REPLAY_BANK_NEXT_BATTLE", source, originalHash, isolated,
                    initialFloor = initial.TowerFloorNumber, bankedFloor = banked.HighestClearedTowerFloor,
                    nextBattleId = final.Battle.BattleId,
                    heroes = final.Guild.Recruits.Where(r => r.RecruitId.StartsWith("SSS_")).Select(r => new {
                        r.DisplayName, r.Progression.AscensionLevel,
                        equipped = r.Equipment.Assignments.Select(s => s.Item.DefinitionId).ToArray() }),
                    sourceUnchanged = true
                }, Formatting.Indented));
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                if (Directory.Exists(output)) File.WriteAllText(Path.Combine(output, "FAILURE107.json"),
                    JsonConvert.SerializeObject(new { error = exception.ToString(), source, originalHash,
                        sourceUnchanged = File.Exists(source) && Hash(source) == originalHash }, Formatting.Indented));
                Debug.LogException(exception);
                EditorApplication.Exit(107);
            }
        }
        private static string Value(string[] args, string prefix) =>
            args.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length) ?? "";
        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
    }
}
