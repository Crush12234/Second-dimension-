using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M1
{
    public enum ApplicantKind
    {
        Procedural,
        Signature
    }

    [Serializable]
    public sealed class ApplicantSnapshotState
    {
        public ApplicantSnapshotState(
            int slot,
            string recruitId,
            string displayName,
            ApplicantKind kind,
            string sourceSeed,
            string signatureId,
            string raceId,
            string worldId,
            string classTendencyId,
            string leadershipBand,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            int signingCostTreasuryXp,
            int potentialBasisPoints,
            string canonicalApplicantJson,
            string canonicalScoutingReportJson,
            IReadOnlyList<EquipmentItemState> openingEquipment)
            : this(
                slot,
                recruitId,
                displayName,
                kind,
                sourceSeed,
                signatureId,
                raceId,
                worldId,
                classTendencyId,
                leadershipBand,
                currentHp,
                maximumHp,
                currentMp,
                maximumMp,
                maximumHp > 0,
                signingCostTreasuryXp,
                potentialBasisPoints,
                canonicalApplicantJson,
                canonicalScoutingReportJson,
                openingEquipment,
                tutorialAliasId: string.Empty,
                authoredStableRecruitId: string.Empty,
                openingLoadout: EquipmentLoadoutState.Empty(),
                leadershipScore: 0,
                tacticalAptitude: 0)
        {
        }

        [JsonConstructor]
        public ApplicantSnapshotState(
            int slot,
            string recruitId,
            string displayName,
            ApplicantKind kind,
            string sourceSeed,
            string signatureId,
            string raceId,
            string worldId,
            string classTendencyId,
            string leadershipBand,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            bool vitalsInitialized,
            int signingCostTreasuryXp,
            int potentialBasisPoints,
            string canonicalApplicantJson,
            string canonicalScoutingReportJson,
            IReadOnlyList<EquipmentItemState> openingEquipment,
            string tutorialAliasId,
            string authoredStableRecruitId,
            EquipmentLoadoutState openingLoadout,
            int leadershipScore,
            int tacticalAptitude)
        {
            if (slot <= 0) throw new ArgumentOutOfRangeException(nameof(slot));
            Slot = slot;
            RecruitId = Require(recruitId, nameof(recruitId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? RecruitId : displayName;
            Kind = kind;
            SourceSeed = sourceSeed ?? string.Empty;
            var normalizedSignatureId = signatureId ?? string.Empty;
            var normalizedAliasId = tutorialAliasId ?? string.Empty;
            var normalizedAuthoredStableId = authoredStableRecruitId ?? string.Empty;
            NormalizeTutorialSignatureIdentity(
                kind,
                SourceSeed,
                ref normalizedSignatureId,
                ref normalizedAliasId,
                ref normalizedAuthoredStableId);
            SignatureId = normalizedSignatureId;
            TutorialAliasId = normalizedAliasId;
            AuthoredStableRecruitId = normalizedAuthoredStableId;
            RaceId = raceId ?? string.Empty;
            WorldId = worldId ?? string.Empty;
            ClassTendencyId = classTendencyId ?? string.Empty;
            LeadershipBand = leadershipBand ?? string.Empty;
            if (vitalsInitialized && (maximumHp <= 0 || currentHp < 0 || currentHp > maximumHp))
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }
            if (vitalsInitialized && (maximumMp < 0 || currentMp < 0 || currentMp > maximumMp))
            {
                throw new ArgumentOutOfRangeException(nameof(currentMp));
            }
            if (!vitalsInitialized && (currentHp != 0 || maximumHp != 0 || currentMp != 0 || maximumMp != 0))
            {
                throw new ArgumentException("Deferred M1 vitals must use exact zero placeholders.");
            }
            if (signingCostTreasuryXp < 0) throw new ArgumentOutOfRangeException(nameof(signingCostTreasuryXp));
            if (potentialBasisPoints < 0) throw new ArgumentOutOfRangeException(nameof(potentialBasisPoints));
            if (leadershipScore < 0) throw new ArgumentOutOfRangeException(nameof(leadershipScore));
            if (tacticalAptitude < 0) throw new ArgumentOutOfRangeException(nameof(tacticalAptitude));
            CurrentHp = currentHp;
            MaximumHp = maximumHp;
            CurrentMp = currentMp;
            MaximumMp = maximumMp;
            VitalsInitialized = vitalsInitialized;
            SigningCostTreasuryXp = signingCostTreasuryXp;
            PotentialBasisPoints = potentialBasisPoints;
            LeadershipScore = leadershipScore;
            TacticalAptitude = tacticalAptitude;
            CanonicalApplicantJson = canonicalApplicantJson ?? string.Empty;
            CanonicalScoutingReportJson = canonicalScoutingReportJson ?? string.Empty;
            OpeningEquipment = CopyEquipment(openingEquipment);
            OpeningLoadout = openingLoadout ?? EquipmentLoadoutState.Empty();
            ValidateLoadoutItems(OpeningEquipment, OpeningLoadout);
        }

        public int Slot { get; }
        public string RecruitId { get; }
        public string DisplayName { get; }
        public ApplicantKind Kind { get; }
        public string SourceSeed { get; }
        public string SignatureId { get; }
        public string TutorialAliasId { get; }
        public string AuthoredStableRecruitId { get; }
        public string RaceId { get; }
        public string WorldId { get; }
        public string ClassTendencyId { get; }
        public string LeadershipBand { get; }
        public int CurrentHp { get; }
        public int MaximumHp { get; }
        public int CurrentMp { get; }
        public int MaximumMp { get; }
        public bool VitalsInitialized { get; }
        public int SigningCostTreasuryXp { get; }
        public int PotentialBasisPoints { get; }
        public int LeadershipScore { get; }
        public int TacticalAptitude { get; }
        public string CanonicalApplicantJson { get; }
        public string CanonicalScoutingReportJson { get; }
        public IReadOnlyList<EquipmentItemState> OpeningEquipment { get; }
        public EquipmentLoadoutState OpeningLoadout { get; }

        private static void ValidateLoadoutItems(
            IReadOnlyList<EquipmentItemState> equipment,
            EquipmentLoadoutState loadout)
        {
            for (var assignmentIndex = 0; assignmentIndex < loadout.Assignments.Count; assignmentIndex++)
            {
                var instanceId = loadout.Assignments[assignmentIndex].Item.InstanceId;
                var found = false;
                for (var itemIndex = 0; itemIndex < equipment.Count; itemIndex++)
                {
                    if (StringComparer.Ordinal.Equals(equipment[itemIndex].InstanceId, instanceId))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) throw new ArgumentException("Opening loadout item is missing from opening equipment.");
            }
        }

        private static void NormalizeTutorialSignatureIdentity(
            ApplicantKind kind,
            string sourceSeed,
            ref string signatureId,
            ref string tutorialAliasId,
            ref string authoredStableRecruitId)
        {
            if (kind != ApplicantKind.Signature) return;
            if (StringComparer.Ordinal.Equals(signatureId, "SIG_MAREN_HOLT"))
            {
                if (string.IsNullOrEmpty(tutorialAliasId)) tutorialAliasId = signatureId;
                if (string.IsNullOrEmpty(authoredStableRecruitId)) authoredStableRecruitId = sourceSeed;
                signatureId = "SIG_W01_01";
            }
            else if (StringComparer.Ordinal.Equals(signatureId, "SIG_ODELIA_FEN"))
            {
                if (string.IsNullOrEmpty(tutorialAliasId)) tutorialAliasId = signatureId;
                if (string.IsNullOrEmpty(authoredStableRecruitId)) authoredStableRecruitId = sourceSeed;
                signatureId = "SIG_W01_03";
            }
        }

        private static IReadOnlyList<EquipmentItemState> CopyEquipment(IReadOnlyList<EquipmentItemState> items)
        {
            var copy = new List<EquipmentItemState>();
            if (items != null)
            {
                for (var index = 0; index < items.Count; index++)
                {
                    var item = items[index] ?? throw new ArgumentException("Opening equipment cannot contain null.");
                    for (var existing = 0; existing < copy.Count; existing++)
                    {
                        if (StringComparer.Ordinal.Equals(copy[existing].InstanceId, item.InstanceId))
                        {
                            throw new ArgumentException("Opening equipment instance IDs must be unique.");
                        }
                    }
                    copy.Add(item);
                }
            }
            copy.Sort((left, right) => StringComparer.Ordinal.Compare(left.InstanceId, right.InstanceId));
            return copy.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    [Serializable]
    public sealed class ApplicantBoardState
    {
        public const string FrozenTutorialSeedId = "SDGOW_TUTORIAL_V1_001";
        public const string FrozenTutorialBoardId = "BOARD_TUTORIAL_V1_001";
        public const int FrozenTutorialRefreshOrdinal = 0;

        // CanonicalJson.Sha256Hex(Applicants) for the exact authority-derived board
        // produced by TutorialApplicantFactory -> ApplicantBoardStateAdapter. This is
        // deliberately a literal trust anchor: a self-consistent hash of a modified
        // board is not sufficient to claim the frozen tutorial identity.
        public const string FrozenTutorialApplicantsHash =
            "f257b85fc6fff62d390bcaa1e5012273e0d100b9857f010d04514d741ab56dd2";

        [JsonConstructor]
        public ApplicantBoardState(
            string boardId,
            string generationKey,
            int refreshOrdinal,
            bool committed,
            IReadOnlyList<ApplicantSnapshotState> applicants,
            string committedApplicantsHash)
        {
            BoardId = Require(boardId, nameof(boardId));
            GenerationKey = Require(generationKey, nameof(generationKey));
            if (refreshOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(refreshOrdinal));
            RefreshOrdinal = refreshOrdinal;
            if (!committed) throw new ArgumentException("Only committed boards may enter campaign state.", nameof(committed));
            Committed = true;
            Applicants = CopyApplicants(applicants);
            if (Applicants.Count == 0) throw new ArgumentException("A committed board cannot be empty.", nameof(applicants));
            var calculatedHash = CanonicalJson.Sha256Hex(Applicants);
            if (!string.IsNullOrEmpty(committedApplicantsHash) &&
                !StringComparer.Ordinal.Equals(calculatedHash, committedApplicantsHash))
            {
                throw new ArgumentException("Committed applicant hash differs from its applicant payload.", nameof(committedApplicantsHash));
            }
            CommittedApplicantsHash = calculatedHash;
        }

        public string BoardId { get; }
        public string GenerationKey { get; }
        public int RefreshOrdinal { get; }
        public bool Committed { get; }
        public IReadOnlyList<ApplicantSnapshotState> Applicants { get; }
        public string CommittedApplicantsHash { get; }

        public ApplicantSnapshotState FindApplicant(string recruitId)
        {
            for (var index = 0; index < Applicants.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(Applicants[index].RecruitId, recruitId)) return Applicants[index];
            }
            return null;
        }

        private static IReadOnlyList<ApplicantSnapshotState> CopyApplicants(IReadOnlyList<ApplicantSnapshotState> applicants)
        {
            var copy = new List<ApplicantSnapshotState>();
            if (applicants != null)
            {
                for (var index = 0; index < applicants.Count; index++)
                {
                    var applicant = applicants[index] ?? throw new ArgumentException("Applicant cannot be null.", nameof(applicants));
                    for (var existing = 0; existing < copy.Count; existing++)
                    {
                        if (copy[existing].Slot == applicant.Slot ||
                            StringComparer.Ordinal.Equals(copy[existing].RecruitId, applicant.RecruitId))
                        {
                            throw new ArgumentException("Applicant slots and recruit IDs must be unique.", nameof(applicants));
                        }
                    }
                    copy.Add(applicant);
                }
            }
            copy.Sort((left, right) => left.Slot.CompareTo(right.Slot));
            return copy.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }
}
