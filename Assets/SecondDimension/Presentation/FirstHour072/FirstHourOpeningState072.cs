using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SecondDimension.Presentation.FirstHour072
{
    public enum FirstHourOpeningPhase072
    {
        Title = 0,
        MarketArrival = 1,
        CharterDialogue = 2,
        FounderIntroductions = 3,
        LoadoutReview = 4,
        UnionSetup = 5,
        HallBreachReady = 6,
        HallBreachInProgress = 7,
        HallBreachRewarded = 8,
        LanternRoadHook = 9
    }

    [Serializable]
    public sealed class FirstHourUnionChoice072
    {
        [JsonConstructor]
        public FirstHourUnionChoice072(
            int unionIndex,
            string leaderRecruitId,
            string formationId,
            bool confirmed)
        {
            if (unionIndex < 0 || unionIndex > 1)
                throw new InvalidDataException("First-hour Union index must be zero or one.");
            UnionIndex = unionIndex;
            LeaderRecruitId = leaderRecruitId ?? string.Empty;
            FormationId = formationId ?? string.Empty;
            Confirmed = confirmed;
            if (Confirmed && !IsReady)
                throw new InvalidDataException("A confirmed first-hour Union requires a leader and formation.");
        }

        public int UnionIndex { get; }
        public string LeaderRecruitId { get; }
        public string FormationId { get; }
        public bool Confirmed { get; }
        public bool IsReady =>
            !string.IsNullOrWhiteSpace(LeaderRecruitId) &&
            !string.IsNullOrWhiteSpace(FormationId);

        public FirstHourUnionChoice072 WithLeader(string recruitId) =>
            new FirstHourUnionChoice072(
                UnionIndex,
                RequireText(recruitId, nameof(recruitId)),
                FormationId,
                confirmed: false);

        public FirstHourUnionChoice072 WithFormation(string formationId) =>
            new FirstHourUnionChoice072(
                UnionIndex,
                LeaderRecruitId,
                RequireText(formationId, nameof(formationId)),
                confirmed: false);

        public FirstHourUnionChoice072 Confirm()
        {
            if (!IsReady)
                throw new InvalidOperationException("Choose a leader and formation before confirming the Union.");
            return new FirstHourUnionChoice072(UnionIndex, LeaderRecruitId, FormationId, confirmed: true);
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty value is required.", parameterName);
            return value.Trim();
        }
    }

    /// <summary>
    /// Small, player-visible route record kept beside the deterministic campaign save.
    /// It owns presentation checkpoints only; roster, equipment, Unions, battle and
    /// rewards remain authoritative in the existing campaign save.
    /// </summary>
    [Serializable]
    public sealed class FirstHourOpeningState072
    {
        public const int CurrentSaveVersion = 1;
        public const string BuildId = "FIRST-HOUR-REBUILD-072";
        public const string HallBreachBattleId = "BATTLE_FIRST_HOUR072_HALL_BREACH";
        public const int RequiredFounderCount = 6;
        public const int CharterDialogueBeatCount = 3;

        [JsonConstructor]
        public FirstHourOpeningState072(
            int saveVersion,
            string buildId,
            FirstHourOpeningPhase072 phase,
            string lastCheckpointId,
            string campaignGuid,
            string guildmasterName,
            int charterDialogueBeatIndex,
            IReadOnlyList<string> acceptedFounderIds,
            bool loadoutsConfirmed,
            bool unionDraftCreated,
            IReadOnlyList<FirstHourUnionChoice072> unionChoices)
        {
            if (saveVersion != CurrentSaveVersion)
                throw new InvalidDataException("Unsupported first-hour route save version " + saveVersion + ".");
            if (!StringComparer.Ordinal.Equals(buildId, BuildId))
                throw new InvalidDataException("First-hour route build ID mismatch.");
            if (!Enum.IsDefined(typeof(FirstHourOpeningPhase072), phase))
                throw new InvalidDataException("Unknown first-hour route phase.");
            if (string.IsNullOrWhiteSpace(lastCheckpointId))
                throw new InvalidDataException("A first-hour checkpoint ID is required.");
            if (charterDialogueBeatIndex < 0 ||
                charterDialogueBeatIndex > CharterDialogueBeatCount)
                throw new InvalidDataException("Charter dialogue index is outside the authored range.");

            SaveVersion = saveVersion;
            BuildIdentity = buildId;
            Phase = phase;
            LastCheckpointId = lastCheckpointId.Trim();
            CampaignGuid = campaignGuid ?? string.Empty;
            GuildmasterName = guildmasterName ?? string.Empty;
            CharterDialogueBeatIndex = charterDialogueBeatIndex;
            AcceptedFounderIds = NormalizeFounderIds(acceptedFounderIds);
            LoadoutsConfirmed = loadoutsConfirmed;
            UnionDraftCreated = unionDraftCreated;
            UnionChoices = NormalizeUnionChoices(unionChoices);
            ValidateProgressionConsistency();
        }

        public int SaveVersion { get; }
        [JsonProperty("BuildId")]
        public string BuildIdentity { get; }
        public FirstHourOpeningPhase072 Phase { get; }
        public string LastCheckpointId { get; }
        public string CampaignGuid { get; }
        public string GuildmasterName { get; }
        public int CharterDialogueBeatIndex { get; }
        public IReadOnlyList<string> AcceptedFounderIds { get; }
        public bool LoadoutsConfirmed { get; }
        public bool UnionDraftCreated { get; }
        public IReadOnlyList<FirstHourUnionChoice072> UnionChoices { get; }
        public bool AllFoundersAccepted => AcceptedFounderIds.Count == RequiredFounderCount;
        public bool AllUnionsConfirmed => UnionChoices.Count == 2 && UnionChoices.All(value => value.Confirmed);

        public static FirstHourOpeningState072 New() =>
            new FirstHourOpeningState072(
                CurrentSaveVersion,
                BuildId,
                FirstHourOpeningPhase072.Title,
                "FH072_CP_00_TITLE",
                string.Empty,
                string.Empty,
                0,
                Array.Empty<string>(),
                loadoutsConfirmed: false,
                unionDraftCreated: false,
                DefaultUnionChoices());

        public FirstHourOpeningState072 AdvanceTo(
            FirstHourOpeningPhase072 phase,
            string checkpointId)
        {
            if (phase < Phase)
                throw new InvalidOperationException("First-hour route checkpoints cannot move backwards.");
            if ((int)phase > (int)Phase + 1)
                throw new InvalidOperationException(
                    "First-hour route checkpoints cannot skip a player-visible phase.");
            return Copy(phase: phase, checkpointId: checkpointId);
        }

        public FirstHourOpeningState072 WithCampaign(string campaignGuid, string guildmasterName)
        {
            RequirePhase(FirstHourOpeningPhase072.CharterDialogue, "sign the charter");
            return Copy(
                campaignGuid: RequireText(campaignGuid, nameof(campaignGuid)),
                guildmasterName: RequireText(guildmasterName, nameof(guildmasterName)));
        }

        public FirstHourOpeningState072 WithDialogueBeat(int beatIndex)
        {
            if (beatIndex < CharterDialogueBeatIndex || beatIndex > CharterDialogueBeatCount)
                throw new ArgumentOutOfRangeException(nameof(beatIndex));
            return Copy(dialogueBeatIndex: beatIndex);
        }

        public FirstHourOpeningState072 WithAcceptedFounder(string recruitId)
        {
            RequirePhase(FirstHourOpeningPhase072.FounderIntroductions, "accept a founder");
            var normalized = RequireText(recruitId, nameof(recruitId));
            if (AcceptedFounderIds.Contains(normalized, StringComparer.Ordinal)) return this;
            if (AcceptedFounderIds.Count >= RequiredFounderCount)
                throw new InvalidOperationException("The six-founder opening roster is already complete.");
            var accepted = AcceptedFounderIds.Concat(new[] { normalized }).ToArray();
            return Copy(acceptedFounderIds: accepted);
        }

        public FirstHourOpeningState072 ConfirmLoadouts()
        {
            RequirePhase(FirstHourOpeningPhase072.LoadoutReview, "confirm loadouts");
            return Copy(loadoutsConfirmed: true);
        }

        public FirstHourOpeningState072 MarkUnionDraftCreated()
        {
            RequirePhase(FirstHourOpeningPhase072.UnionSetup, "form founding Unions");
            return Copy(unionDraftCreated: true);
        }

        public FirstHourOpeningState072 WithUnionLeader(int unionIndex, string recruitId)
        {
            RequireUnionDraft("choose a Union leader");
            return WithUnionChoice(unionIndex, Choice(unionIndex).WithLeader(recruitId));
        }

        public FirstHourOpeningState072 WithUnionFormation(int unionIndex, string formationId)
        {
            RequireUnionDraft("choose a Union formation");
            return WithUnionChoice(unionIndex, Choice(unionIndex).WithFormation(formationId));
        }

        public FirstHourOpeningState072 ConfirmUnion(int unionIndex)
        {
            RequireUnionDraft("lock a founding Union");
            return WithUnionChoice(unionIndex, Choice(unionIndex).Confirm());
        }

        public FirstHourUnionChoice072 Choice(int unionIndex)
        {
            var choice = UnionChoices.FirstOrDefault(value => value.UnionIndex == unionIndex);
            return choice ?? throw new ArgumentOutOfRangeException(nameof(unionIndex));
        }

        public void ValidateForPersistence()
        {
            if (SaveVersion != CurrentSaveVersion ||
                !StringComparer.Ordinal.Equals(BuildIdentity, BuildId) ||
                string.IsNullOrWhiteSpace(LastCheckpointId) ||
                AcceptedFounderIds.Count > RequiredFounderCount ||
                UnionChoices.Count != 2)
                throw new InvalidDataException("First-hour route state failed validation.");
            ValidateProgressionConsistency();
        }

        private void ValidateProgressionConsistency()
        {
            if (Phase >= FirstHourOpeningPhase072.FounderIntroductions &&
                (string.IsNullOrWhiteSpace(CampaignGuid) ||
                 string.IsNullOrWhiteSpace(GuildmasterName) ||
                 CharterDialogueBeatIndex != CharterDialogueBeatCount))
                throw new InvalidDataException(
                    "Founder introductions require the visible charter dialogue and signature.");
            if (Phase >= FirstHourOpeningPhase072.LoadoutReview && !AllFoundersAccepted)
                throw new InvalidDataException(
                    "Loadout review requires six explicitly accepted founders.");
            if (Phase >= FirstHourOpeningPhase072.UnionSetup && !LoadoutsConfirmed)
                throw new InvalidDataException(
                    "Union setup requires the visible starter-loadout confirmation.");
            if (UnionChoices.Any(value => value.Confirmed) && !UnionDraftCreated)
                throw new InvalidDataException(
                    "A founding Union cannot be locked before its draft exists.");
            if (Phase >= FirstHourOpeningPhase072.HallBreachReady &&
                (!UnionDraftCreated || !AllUnionsConfirmed))
                throw new InvalidDataException(
                    "The Hall Breach requires two explicitly confirmed founding Unions.");
        }

        private void RequirePhase(FirstHourOpeningPhase072 required, string action)
        {
            if (Phase != required)
                throw new InvalidOperationException(
                    "The route cannot " + action + " during " + Phase + ".");
        }

        private void RequireUnionDraft(string action)
        {
            RequirePhase(FirstHourOpeningPhase072.UnionSetup, action);
            if (!UnionDraftCreated)
                throw new InvalidOperationException(
                    "Form the two visible founding Union drafts before you " + action + ".");
        }

        private FirstHourOpeningState072 WithUnionChoice(
            int unionIndex,
            FirstHourUnionChoice072 replacement)
        {
            if (unionIndex < 0 || unionIndex > 1)
                throw new ArgumentOutOfRangeException(nameof(unionIndex));
            var choices = UnionChoices
                .Select(value => value.UnionIndex == unionIndex ? replacement : value)
                .OrderBy(value => value.UnionIndex)
                .ToArray();
            return Copy(unionChoices: choices);
        }

        private FirstHourOpeningState072 Copy(
            FirstHourOpeningPhase072? phase = null,
            string checkpointId = null,
            string campaignGuid = null,
            string guildmasterName = null,
            int? dialogueBeatIndex = null,
            IReadOnlyList<string> acceptedFounderIds = null,
            bool? loadoutsConfirmed = null,
            bool? unionDraftCreated = null,
            IReadOnlyList<FirstHourUnionChoice072> unionChoices = null) =>
            new FirstHourOpeningState072(
                SaveVersion,
                BuildIdentity,
                phase ?? Phase,
                checkpointId ?? LastCheckpointId,
                campaignGuid ?? CampaignGuid,
                guildmasterName ?? GuildmasterName,
                dialogueBeatIndex ?? CharterDialogueBeatIndex,
                acceptedFounderIds ?? AcceptedFounderIds,
                loadoutsConfirmed ?? LoadoutsConfirmed,
                unionDraftCreated ?? UnionDraftCreated,
                unionChoices ?? UnionChoices);

        private static IReadOnlyList<string> NormalizeFounderIds(IReadOnlyList<string> source)
        {
            var result = new List<string>();
            foreach (var value in source ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException("Accepted founder IDs cannot be empty.");
                var normalized = value.Trim();
                if (result.Contains(normalized, StringComparer.Ordinal))
                    throw new InvalidDataException("Accepted founder IDs must be unique.");
                result.Add(normalized);
            }
            if (result.Count > RequiredFounderCount)
                throw new InvalidDataException("The first-hour route supports exactly six founders.");
            return result.AsReadOnly();
        }

        private static IReadOnlyList<FirstHourUnionChoice072> NormalizeUnionChoices(
            IReadOnlyList<FirstHourUnionChoice072> source)
        {
            var choices = (source ?? DefaultUnionChoices())
                .Where(value => value != null)
                .OrderBy(value => value.UnionIndex)
                .ToArray();
            if (choices.Length != 2 || choices[0].UnionIndex != 0 || choices[1].UnionIndex != 1)
                throw new InvalidDataException("First-hour route state requires Union choices zero and one.");
            return Array.AsReadOnly(choices);
        }

        private static IReadOnlyList<FirstHourUnionChoice072> DefaultUnionChoices() =>
            Array.AsReadOnly(new[]
            {
                new FirstHourUnionChoice072(0, string.Empty, string.Empty, confirmed: false),
                new FirstHourUnionChoice072(1, string.Empty, string.Empty, confirmed: false)
            });

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty value is required.", parameterName);
            return value.Trim();
        }
    }

    public sealed class FirstHourOpeningStateLoad072
    {
        public FirstHourOpeningStateLoad072(
            FirstHourOpeningState072 state,
            bool recoveredFromBackup,
            string diagnostic)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            RecoveredFromBackup = recoveredFromBackup;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public FirstHourOpeningState072 State { get; }
        public bool RecoveredFromBackup { get; }
        public string Diagnostic { get; }
    }

    /// <summary>Atomic, versioned persistence for visible First Hour 072 checkpoints.</summary>
    public sealed class FirstHourOpeningStateStore072
    {
        private readonly string _primaryPath;

        public FirstHourOpeningStateStore072(string primaryPath)
        {
            if (string.IsNullOrWhiteSpace(primaryPath))
                throw new ArgumentException("First-hour route save path is required.", nameof(primaryPath));
            _primaryPath = Path.GetFullPath(primaryPath);
        }

        public string PrimaryPath => _primaryPath;

        public FirstHourOpeningStateLoad072 Load()
        {
            if (!File.Exists(_primaryPath) && !File.Exists(_primaryPath + ".bak"))
                return new FirstHourOpeningStateLoad072(FirstHourOpeningState072.New(), false, string.Empty);

            try
            {
                return new FirstHourOpeningStateLoad072(ReadFile(_primaryPath), false, string.Empty);
            }
            catch (Exception) { }

            try
            {
                return new FirstHourOpeningStateLoad072(
                    ReadFile(_primaryPath + ".bak"),
                    true,
                    "Your Guild journey recovered safely from its backup record.");
            }
            catch (Exception)
            {
                return new FirstHourOpeningStateLoad072(
                    FirstHourOpeningState072.New(),
                    false,
                    "The previous journey record could not be recovered. Begin again at Skyhome Market.");
            }
        }

        public void Save(FirstHourOpeningState072 state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.ValidateForPersistence();
            var directory = Path.GetDirectoryName(_primaryPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidDataException("First-hour route save directory is unavailable.");
            Directory.CreateDirectory(directory);

            var temporaryPath = _primaryPath + ".tmp";
            var backupPath = _primaryPath + ".bak";
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            using (var stream = new FileStream(
                       temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            ReadFile(temporaryPath).ValidateForPersistence();
            if (!File.Exists(_primaryPath))
            {
                File.Move(temporaryPath, _primaryPath);
                return;
            }

            try
            {
                File.Replace(temporaryPath, _primaryPath, backupPath, ignoreMetadataErrors: true);
            }
            catch (PlatformNotSupportedException)
            {
                PortableReplace(temporaryPath, backupPath);
            }
            catch (IOException)
            {
                PortableReplace(temporaryPath, backupPath);
            }
        }

        private static FirstHourOpeningState072 ReadFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Route checkpoint not found.", path);
            var state = JsonConvert.DeserializeObject<FirstHourOpeningState072>(
                File.ReadAllText(path, Encoding.UTF8));
            if (state == null) throw new InvalidDataException("First-hour route checkpoint is empty.");
            state.ValidateForPersistence();
            return state;
        }

        private void PortableReplace(string temporaryPath, string backupPath)
        {
            File.Copy(_primaryPath, backupPath, overwrite: true);
            File.Delete(_primaryPath);
            File.Move(temporaryPath, _primaryPath);
        }
    }
}
