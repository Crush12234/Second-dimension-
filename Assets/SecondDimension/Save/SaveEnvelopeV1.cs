using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Save
{
    [Serializable]
    public sealed class SaveEnvelopeV1
    {
        public const int CurrentFormatVersion = 11;

        [JsonConstructor]
        public SaveEnvelopeV1(
            int saveFormatVersion,
            string contentAuthorityVersion,
            string campaignGuid,
            long campaignSeed,
            string displayTimestampUtc,
            CampaignState campaignState,
            string canonicalStateHash,
            IReadOnlyList<string> migrationHistory)
        {
            SaveFormatVersion = saveFormatVersion;
            ContentAuthorityVersion = contentAuthorityVersion;
            CampaignGuid = campaignGuid;
            CampaignSeed = campaignSeed;
            DisplayTimestampUtc = displayTimestampUtc;
            CampaignState = campaignState;
            CanonicalStateHash = canonicalStateHash;
            MigrationHistory = migrationHistory ?? Array.Empty<string>();
        }

        public int SaveFormatVersion { get; }
        public string ContentAuthorityVersion { get; }
        public string CampaignGuid { get; }
        public long CampaignSeed { get; }
        public string DisplayTimestampUtc { get; }
        public CampaignState CampaignState { get; }
        public string CanonicalStateHash { get; }
        public IReadOnlyList<string> MigrationHistory { get; }

        public static SaveEnvelopeV1 Create(CampaignState state, DateTime displayTimestampUtc)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new SaveEnvelopeV1(
                CurrentFormatVersion,
                state.ContentAuthorityVersion,
                state.CampaignGuid,
                state.CampaignSeed,
                displayTimestampUtc.ToUniversalTime().ToString("O"),
                state,
                SecondDimension.Determinism.CanonicalJson.Sha256Hex(state),
                Array.Empty<string>());
        }
    }
}
