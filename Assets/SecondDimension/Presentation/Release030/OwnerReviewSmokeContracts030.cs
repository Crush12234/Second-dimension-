using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Presentation.Release030
{
    public enum OwnerReviewSmokeRunKind030
    {
        None,
        Initial,
        Relaunch
    }

    public sealed class OwnerReviewSmokeOptions030
    {
        public const string InitialFlag = "--sd-owner-smoke-030";
        public const string RelaunchFlag = "--sd-owner-relaunch-030";
        public const string EvidenceDirectoryFlag = "--sd-owner-evidence-dir=";
        public const string SavePathFlag = "--sd-owner-save-path=";
        public const string CreatorCodeFlag = "--sd-owner-creator-code-030=";
        public const string CreatorCodeEnvironmentVariable = "SECOND_DIMENSION_OWNER_CREATOR_CODE_030";

        public OwnerReviewSmokeRunKind030 RunKind { get; private set; }
        public string EvidenceDirectory { get; private set; }
        public string SavePath { get; private set; }
        [JsonIgnore]
        public string CreatorCode { get; private set; }
        public string ReportPath => Path.Combine(EvidenceDirectory,
            RunKind == OwnerReviewSmokeRunKind030.Relaunch
                ? "built_player_relaunch_030.json"
                : "built_player_smoke_030.json");
        public string ProgressPath => Path.Combine(EvidenceDirectory,
            RunKind == OwnerReviewSmokeRunKind030.Relaunch
                ? "built_player_relaunch_030.progress.json"
                : "built_player_smoke_030.progress.json");
        public string InitialReportPath => Path.Combine(EvidenceDirectory, "built_player_smoke_030.json");

        public static bool TryParse(IReadOnlyList<string> arguments, string persistentDataPath,
            string applicationDataPath, out OwnerReviewSmokeOptions030 options, out string error)
        {
            options = null;
            error = string.Empty;
            arguments = arguments ?? Array.Empty<string>();
            var initial = arguments.Any(value => StringComparer.Ordinal.Equals(value, InitialFlag));
            var relaunch = arguments.Any(value => StringComparer.Ordinal.Equals(value, RelaunchFlag));
            if (!initial && !relaunch) return false;
            if (initial == relaunch)
            {
                error = "Specify exactly one Release 030 owner-review run flag.";
                return false;
            }

            try
            {
                var evidenceValue = LastValue(arguments, EvidenceDirectoryFlag);
                var saveValue = LastValue(arguments, SavePathFlag);
                var evidenceDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(evidenceValue)
                    ? Path.Combine(persistentDataPath, "OwnerReviewEvidence030")
                    : evidenceValue);
                var savePath = Path.GetFullPath(string.IsNullOrWhiteSpace(saveValue)
                    ? Path.Combine(persistentDataPath, "owner_review_smoke_030_v11.json")
                    : saveValue);
                var dataPath = Path.GetFullPath(applicationDataPath ?? string.Empty);
                if (string.IsNullOrWhiteSpace(evidenceDirectory) || IsSameOrChild(evidenceDirectory, dataPath))
                    throw new InvalidOperationException("Evidence must be written outside the player Data folder.");
                if (string.IsNullOrWhiteSpace(savePath) || IsSameOrChild(savePath, dataPath))
                    throw new InvalidOperationException("The owner-review save must be written outside the player Data folder.");
                if (string.IsNullOrWhiteSpace(Path.GetFileName(savePath)))
                    throw new InvalidOperationException("The owner-review save path must name a file.");

                var creatorCode = LastValue(arguments, CreatorCodeFlag);
                if (creatorCode == null && initial)
                    creatorCode = Environment.GetEnvironmentVariable(CreatorCodeEnvironmentVariable);

                options = new OwnerReviewSmokeOptions030
                {
                    RunKind = initial ? OwnerReviewSmokeRunKind030.Initial : OwnerReviewSmokeRunKind030.Relaunch,
                    EvidenceDirectory = evidenceDirectory,
                    SavePath = savePath,
                    CreatorCode = creatorCode ?? string.Empty
                };
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static string LastValue(IReadOnlyList<string> arguments, string prefix)
        {
            string result = null;
            for (var index = 0; index < arguments.Count; index++)
            {
                var value = arguments[index];
                if (value != null && value.StartsWith(prefix, StringComparison.Ordinal))
                    result = value.Substring(prefix.Length);
            }
            return result;
        }

        private static bool IsSameOrChild(string candidate, string parent)
        {
            if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(parent)) return false;
            var comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var normalizedParent = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedCandidate = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return normalizedCandidate.Equals(normalizedParent, comparison) ||
                   normalizedCandidate.StartsWith(normalizedParent + Path.DirectorySeparatorChar, comparison) ||
                   normalizedCandidate.StartsWith(normalizedParent + Path.AltDirectorySeparatorChar, comparison);
        }
    }

    [Serializable]
    public sealed class OwnerReviewGateEvidence030
    {
        public string id = string.Empty;
        public string stage = string.Empty;
        public string status = string.Empty;
        public string detail = string.Empty;
    }

    [Serializable]
    public sealed class OwnerReviewStateEvidence030
    {
        public string guildmasterName = string.Empty;
        public int signedRecruitCount;
        public int permanentRecruitCount;
        public int normalUnionCount;
        public int placedBuildingCount;
        public int staffedBuildingCount;
        public int relationshipCount;
        public int claimedBattleRewardCount;
        public int defensesResolved;
        public int completedPersonalQuests;
        public int completedWorldGateBoards;
        public int completedCampaignChapters;
        public int completedCreatorRooms;
        public int progressionItems;
        public int invocationArtifacts;
        public int summonResonance;
        public string lastCheckpointId = string.Empty;
    }

    [Serializable]
    public sealed class OwnerReviewSmokeReport030
    {
        public string schema = "SECOND_DIMENSION_OWNER_REVIEW_SMOKE_030_1";
        public string releaseId = "030";
        public string runKind = string.Empty;
        public string runStatus = "RUNNING";
        public bool checklistComplete;
        public bool automaticExit = true;
        public int processExitCode;
        public string currentStage = "Source";
        public string startedUtc = string.Empty;
        public string completedUtc = string.Empty;
        public string unityVersion = string.Empty;
        public string platform = string.Empty;
        public string executablePath = string.Empty;
        public string activePipeline = string.Empty;
        public int saveFormatVersion;
        public string canonicalStateHash = string.Empty;
        public string expectedCanonicalStateHash = string.Empty;
        public bool stateIdentityMatched;
        public string reportPath = string.Empty;
        public string savePath = string.Empty;
        public string failure = string.Empty;
        public int requiredGateCount;
        public int passedRequiredGateCount;
        public List<string> requiredGateIds = new List<string>();
        public List<string> missingRequiredGateIds = new List<string>();
        public List<string> duplicateRequiredGateIds = new List<string>();
        public List<string> nonPassingRequiredGateIds = new List<string>();
        public OwnerReviewStateEvidence030 state = new OwnerReviewStateEvidence030();
        public List<OwnerReviewGateEvidence030> gates = new List<OwnerReviewGateEvidence030>();
    }

    public static class OwnerReviewSmokeEvidenceWriter030
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        public static void Write(string path, OwnerReviewSmokeReport030 report)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Evidence path is required.", nameof(path));
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Evidence directory is required.");
            Directory.CreateDirectory(directory);
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(report, Settings));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporaryPath, path);
        }

        public static OwnerReviewSmokeReport030 Read(string path)
        {
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<OwnerReviewSmokeReport030>(File.ReadAllText(path), Settings);
        }
    }
}
