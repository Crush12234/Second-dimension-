using System;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.State
{
    public enum RecruitOriginKind
    {
        Legacy,
        Procedural,
        Signature
    }

    public enum RecruitAuthorityKind
    {
        Normal,
        Kael,
        Founder
    }

    [Serializable]
    public sealed class RecruitState
    {
        public RecruitState(string recruitId, int currentHp, int maximumHp, int currentMp, int maximumMp)
            : this(
                recruitId,
                currentHp,
                maximumHp,
                currentMp,
                maximumMp,
                recruitId,
                RecruitOriginKind.Legacy,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                RecruitAuthorityKind.Normal,
                string.Empty,
                string.Empty,
                EquipmentLoadoutState.Empty(),
                true,
                string.Empty,
                string.Empty,
                0,
                0)
        {
        }

        public RecruitState(
            string recruitId,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            string displayName,
            RecruitOriginKind originKind,
            string signatureId,
            string raceId,
            string worldId,
            string classTendencyId,
            string leadershipBand,
            int potentialBasisPoints,
            RecruitAuthorityKind authorityKind,
            string canonicalApplicantJson,
            string canonicalScoutingReportJson,
            EquipmentLoadoutState equipment,
            bool vitalsInitialized)
            : this(
                recruitId,
                currentHp,
                maximumHp,
                currentMp,
                maximumMp,
                displayName,
                originKind,
                signatureId,
                raceId,
                worldId,
                classTendencyId,
                leadershipBand,
                potentialBasisPoints,
                authorityKind,
                canonicalApplicantJson,
                canonicalScoutingReportJson,
                equipment,
                vitalsInitialized,
                string.Empty,
                string.Empty,
                0,
                0)
        {
        }

        [JsonConstructor]
        public RecruitState(
            string recruitId,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            string displayName,
            RecruitOriginKind originKind,
            string signatureId,
            string raceId,
            string worldId,
            string classTendencyId,
            string leadershipBand,
            int potentialBasisPoints,
            RecruitAuthorityKind authorityKind,
            string canonicalApplicantJson,
            string canonicalScoutingReportJson,
            EquipmentLoadoutState equipment,
            bool vitalsInitialized,
            string tutorialAliasId,
            string authoredStableRecruitId,
            int leadershipScore,
            int tacticalAptitude,
            RecruitProgressionState progression = null)
        {
            RecruitId = Require(recruitId, nameof(recruitId));
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
            CurrentHp = currentHp;
            MaximumHp = maximumHp;
            CurrentMp = currentMp;
            MaximumMp = maximumMp;
            VitalsInitialized = vitalsInitialized;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? RecruitId : displayName;
            OriginKind = originKind;
            SignatureId = signatureId ?? string.Empty;
            TutorialAliasId = tutorialAliasId ?? string.Empty;
            AuthoredStableRecruitId = authoredStableRecruitId ?? string.Empty;
            RaceId = raceId ?? string.Empty;
            WorldId = worldId ?? string.Empty;
            ClassTendencyId = classTendencyId ?? string.Empty;
            LeadershipBand = leadershipBand ?? string.Empty;
            if (potentialBasisPoints < 0) throw new ArgumentOutOfRangeException(nameof(potentialBasisPoints));
            if (leadershipScore < 0) throw new ArgumentOutOfRangeException(nameof(leadershipScore));
            if (tacticalAptitude < 0) throw new ArgumentOutOfRangeException(nameof(tacticalAptitude));
            PotentialBasisPoints = potentialBasisPoints;
            LeadershipScore = leadershipScore;
            TacticalAptitude = tacticalAptitude;
            AuthorityKind = authorityKind;
            CanonicalApplicantJson = canonicalApplicantJson ?? string.Empty;
            CanonicalScoutingReportJson = canonicalScoutingReportJson ?? string.Empty;
            Equipment = equipment ?? EquipmentLoadoutState.Empty();
            Progression = progression ?? RecruitProgressionState.Default();
        }

        public string RecruitId { get; }
        public int CurrentHp { get; }
        public int MaximumHp { get; }
        public int CurrentMp { get; }
        public int MaximumMp { get; }
        public bool VitalsInitialized { get; }
        public string DisplayName { get; }
        public RecruitOriginKind OriginKind { get; }
        public string SignatureId { get; }
        public string TutorialAliasId { get; }
        public string AuthoredStableRecruitId { get; }
        public string RaceId { get; }
        public string WorldId { get; }
        public string ClassTendencyId { get; }
        public string LeadershipBand { get; }
        public int PotentialBasisPoints { get; }
        public int LeadershipScore { get; }
        public int TacticalAptitude { get; }
        public RecruitAuthorityKind AuthorityKind { get; }
        public string CanonicalApplicantJson { get; }
        public string CanonicalScoutingReportJson { get; }
        public EquipmentLoadoutState Equipment { get; }
        public RecruitProgressionState Progression { get; }

        public RecruitState WithEquipment(EquipmentLoadoutState equipment) =>
            new RecruitState(
                RecruitId,
                CurrentHp,
                MaximumHp,
                CurrentMp,
                MaximumMp,
                DisplayName,
                OriginKind,
                SignatureId,
                RaceId,
                WorldId,
                ClassTendencyId,
                LeadershipBand,
                PotentialBasisPoints,
                AuthorityKind,
                CanonicalApplicantJson,
                CanonicalScoutingReportJson,
                equipment,
                VitalsInitialized,
                TutorialAliasId,
                AuthoredStableRecruitId,
                LeadershipScore,
                TacticalAptitude,
                Progression);

        public RecruitState WithProgression(RecruitProgressionState progression) =>
            new RecruitState(
                RecruitId,
                CurrentHp,
                MaximumHp,
                CurrentMp,
                MaximumMp,
                DisplayName,
                OriginKind,
                SignatureId,
                RaceId,
                WorldId,
                ClassTendencyId,
                LeadershipBand,
                PotentialBasisPoints,
                AuthorityKind,
                CanonicalApplicantJson,
                CanonicalScoutingReportJson,
                Equipment,
                VitalsInitialized,
                TutorialAliasId,
                AuthoredStableRecruitId,
                LeadershipScore,
                TacticalAptitude,
                progression ?? throw new ArgumentNullException(nameof(progression)));

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }
}
