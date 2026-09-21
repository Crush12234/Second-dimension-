using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Boot;
using SecondDimension.Save;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    /// <summary>Explicit isolated QA only. No gameplay policy or source save mutation.</summary>
    public sealed class EarnedDeploymentSixtyVerification097
    {
        public static int DeployableAssigned097(CampaignState campaign) =>
            campaign.Guild.Unions.SelectMany(union => union.MemberRecruitIds).Distinct(StringComparer.Ordinal)
                .Count(id => GuildMemberDeploymentPolicy017D.IsRecruitDeployable(campaign.Guild.GuildCity, id));

        public static void ReturnFullHealthRecovery097(M1RuntimeCoordinator coordinator,
            Func<CampaignState> read, Action<string> log)
        {
            var before = read();
            var assigned = before.Guild.Unions.SelectMany(union => union.MemberRecruitIds)
                .Distinct(StringComparer.Ordinal).ToArray();
            foreach (var id in assigned)
            {
                var current = read();
                if (GuildMemberDeploymentPolicy017D.IsRecruitDeployable(current.Guild.GuildCity, id)) continue;
                var actor = current.Guild.Recruits.Single(value => value.RecruitId == id);
                var duty = current.Guild.GuildCity.MemberAssignments.Single(value => value.RecruitId == id);
                Require(duty.Kind == GuildMemberAssignmentKind017D.Recovering &&
                    duty.RecoveryProgress > 0 && actor.CurrentHp >= actor.MaximumHp && actor.CurrentHp > 0,
                    "Assigned member is not ready for this full-health recovery recall: " + id + " / " + duty.Kind);
                var label = "SetGuildCityAssignment017D(" + id + ", Reserve): " + actor.DisplayName +
                    "; HP " + actor.CurrentHp + "/" + actor.MaximumHp + "; recovery " + duty.RecoveryProgress;
                var command = coordinator.SetGuildCityAssignment017D(id, "Reserve");
                Require(command.Succeeded, label + ": " + command.Message);
                log(label);
                Require(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(read().Guild.GuildCity, id),
                    "The normal Reserve command did not restore deployment eligibility.");
            }
            var after = read();
            Require(CanonicalJson.Serialize(before.Guild.Recruits) == CanonicalJson.Serialize(after.Guild.Recruits),
                "Returning duty changed an owned identity, vitals, progression, or equipment.");
            Require(CanonicalJson.Serialize(before.Guild.Unions) == CanonicalJson.Serialize(after.Guild.Unions),
                "Returning duty changed assigned Union members or their positions.");
            Require(before.Guild.TreasuryXp == after.Guild.TreasuryXp, "Returning duty changed XP.");
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string Value(string prefix) => args.FirstOrDefault(value => value.StartsWith(prefix,
                StringComparison.Ordinal))?.Substring(prefix.Length) ?? "";
            var success = false;
            try
            {
                success = Assess097(Value("--sd-deployment-source="), Value("--sd-deployment-evidence="));
            }
            catch (Exception exception) { Debug.LogException(exception); }
            EditorApplication.Exit(success ? 0 : 97);
        }

        public static bool Assess097(string source, string evidence)
        {
            var report = new DeploymentReport097 { source = Path.GetFullPath(source),
                sourceSha256 = Hash097(source), startedUtc = DateTime.UtcNow.ToString("O") };
            // Reuse the copy-only launcher guard: explicit source, fresh external
            // directory, no personal/build/source overlap, exact bytes + save hash.
            var save = ManualEarnedSaveReview097.Prepare097(new[]
            {
                ManualEarnedSaveReview097.SourceFlag097 + source,
                ManualEarnedSaveReview097.OutputFlag097 + evidence
            }, Application.persistentDataPath, Application.dataPath);
            report.isolatedSave = save;
            var directory = Path.GetDirectoryName(save);
            var store = new AtomicSaveStore();
            CampaignState Read()
            {
                var loaded = store.ReadWithRecovery(save);
                Require(loaded.IsSuccess, string.Join("; ", loaded.Errors));
                Require(CanonicalJson.Sha256Hex(loaded.Value.CampaignState) == loaded.Value.CanonicalStateHash,
                    "Copied save canonical hash is invalid.");
                return loaded.Value.CampaignState;
            }
            M1RuntimeCoordinator coordinator = null;
            void Reload()
            {
                var hash = CanonicalJson.Sha256Hex(Read());
                coordinator = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath,
                    "Authority", "CONTENT"), save);
                Require(coordinator.State.CanonicalStateHash == hash && CanonicalJson.Sha256Hex(Read()) == hash,
                    "Loading the copied save changed canonical state.");
                report.reloadChecks++;
            }
            void Command(string name, Func<M1CommandResult> execute)
            {
                var result = execute();
                Require(result.Succeeded, name + ": " + result.Message);
                report.commands.Add(name);
            }
            try
            {
                Reload();
                var initial = Read();
                report.initialCanonicalHash = CanonicalJson.Sha256Hex(initial);
                report.initialXp = initial.Guild.TreasuryXp;
                var initialIds = initial.Guild.Unions.SelectMany(union => union.MemberRecruitIds)
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray();
                report.assignedBefore = initialIds.Length;
                report.deployableBefore = DeployableAssigned097(initial);
                report.unavailableBefore = initialIds.Where(id => !GuildMemberDeploymentPolicy017D
                    .IsRecruitDeployable(initial.Guild.GuildCity, id)).ToArray();
                Require(initialIds.Length == 60 && initialIds.Distinct().Count() == 60,
                    "Use the genuinely owned, already assigned ten-by-six checkpoint.");
                Require(initial.Battle == null || initial.Battle.Outcome != BattleOutcome.InProgress,
                    "Finish the existing battle before changing any member's duty.");
                Require(string.IsNullOrEmpty(coordinator.CampaignProgression022.ActiveAbyssOperationId),
                    "Finish the existing Tower operation before this bounded entry check.");
                ReturnFullHealthRecovery097(coordinator, Read, report.commands.Add);
                Reload();
                var ready = Read();
                report.deployableAfter = DeployableAssigned097(ready);
                report.identitiesVitalsGearProgressionUnchanged =
                    CanonicalJson.Serialize(initial.Guild.Recruits) == CanonicalJson.Serialize(ready.Guild.Recruits);
                report.assignmentsUnchanged = CanonicalJson.Serialize(initial.Guild.Unions) == CanonicalJson.Serialize(ready.Guild.Unions);
                Require(report.deployableAfter == 60, "Sixty assigned slots are not sixty genuinely deployable members.");
                report.readyManualSave = Path.Combine(directory, "CampaignReady60_097.json");
                File.Copy(save, report.readyManualSave, false);
                Command("BeginTowerRun081 (real next floor)", coordinator.BeginTowerRun081);
                var prepared = false;
                for (var step = 0; step < 32; step++)
                {
                    if (coordinator.CampaignProgression022.ActiveStepRequiresBattle)
                    { prepared = true; break; }
                    Command("AdvanceTowerRun081 (normal battle preparation)", coordinator.AdvanceTowerRun081);
                }
                Require(prepared, "Tower did not reach its legal battle entry within32 transitions.");
                Command("EnterAbyssBattle022", coordinator.EnterAbyssBattle022);
                var actual = Read();
                Require(actual.Battle != null && actual.Battle.Outcome == BattleOutcome.InProgress,
                    "The real coordinator did not enter a live battle.");
                var actualIds = actual.Battle.PlayerUnions.SelectMany(union => union.Members)
                    .Select(member => member.MemberId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
                report.actualBattleMembers = actualIds.Length;
                report.actualPlayerUnions = actual.Battle.PlayerUnions.Count;
                report.actualBattleId = actual.Battle.BattleId;
                report.exactSameSixtyInBattle = initialIds.SequenceEqual(actualIds);
                Require(actualIds.Length == 60 && report.exactSameSixtyInBattle,
                    "Actual battle materialization omitted, duplicated, or substituted an assigned hero.");
                Require(actual.Battle.PlayerUnions.All(union => union.Members.Count == 6),
                    "Actual player Unions are not ten by six.");
                Reload();
                report.finalXp = Read().Guild.TreasuryXp;
                Require(report.finalXp == report.initialXp, "The review entry awarded or spent XP.");
                report.status = "PASS_BATTLE_ENTRY_ONLY";
            }
            catch (Exception exception) { report.status = "BLOCKED"; report.failure = exception.ToString(); }
            finally
            {
                report.completedUtc = DateTime.UtcNow.ToString("O");
                report.sourceUnchanged = Hash097(source) == report.sourceSha256;
                if (!report.sourceUnchanged) { report.status = "BLOCKED"; report.failure += " Original source SHA-256 changed."; }
                File.WriteAllText(Path.Combine(directory, "deployment_ready097_report.json"),
                    JsonConvert.SerializeObject(report, Formatting.Indented));
                Debug.Log("DEPLOYMENT097 " + report.status + " assigned=" + report.assignedBefore +
                    " eligible=" + report.deployableBefore + "→" + report.deployableAfter +
                    " battle=" + report.actualBattleMembers + "; " + report.failure);
            }
            return report.status == "PASS_BATTLE_ENTRY_ONLY";
        }

        static string Hash097(string path)
        { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        static void Require(bool valid, string message)
        { if (!valid) throw new InvalidOperationException(message); }
    }

    [Serializable]
    public sealed class DeploymentReport097
    {
        public string status = "RUNNING", failure, source, sourceSha256, isolatedSave, readyManualSave;
        public string startedUtc, completedUtc, initialCanonicalHash, actualBattleId;
        public string scope = "Copied earned save; explicit normal Reserve duty command for full-health recovering assigned heroes; real Tower battle entry. No HP, XP, identity, gear, progression, or Union substitution; no rounds resolved, victories or rewards claimed, or balance/visual certification.";
        public long initialXp, finalXp;
        public int assignedBefore, deployableBefore, deployableAfter, actualBattleMembers, actualPlayerUnions, reloadChecks;
        public bool sourceUnchanged, identitiesVitalsGearProgressionUnchanged, assignmentsUnchanged, exactSameSixtyInBattle;
        public string[] unavailableBefore = Array.Empty<string>();
        public List<string> commands = new List<string>();
    }
}
