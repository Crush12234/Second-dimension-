using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;

namespace SecondDimension.Save
{
    public sealed class AtomicSaveStore
    {
        public const int CurrentSaveFormatVersion = SaveEnvelopeV1.CurrentFormatVersion;
        public const int MinimumReadableSaveFormatVersion = 1;
        private const string V1ToV2MigrationId = "save_v1_to_v2_m1_state_defaults";
        private const string V2ToV3MigrationId = "save_v2_to_v3_m2_battle_defaults";
        private const string V3ToV4MigrationId = "save_v3_to_v4_m2_art_growth_defaults";
        private const string V4ToV5MigrationId = "save_v4_to_v5_progression_foundation_defaults";
        private const string V5ToV6MigrationId = "save_v5_to_v6_guild_city_foundation_defaults";
        private const string V6ToV7MigrationId = "save_v6_to_v7_campaign_runtime_019_defaults";
        private const string V7ToV8MigrationId = "save_v7_to_v8_campaign_playable_020_defaults";
        private const string V8ToV9MigrationId = "save_v8_to_v9_progression_abyss_covenant_022_defaults";
        private const string V9ToV10MigrationId = "save_v9_to_v10_world_gate_expedition_runtime_023_defaults";
        private const string V10ToV11MigrationId = "save_v10_to_v11_creator_codes_rooms_028_defaults";

        public void Write(string primaryPath, SaveEnvelopeV1 envelope)
        {
            if (string.IsNullOrWhiteSpace(primaryPath)) throw new ArgumentException("Save path is required.", nameof(primaryPath));
            if (envelope == null) throw new InvalidDataException("Save envelope is null.");
            if (envelope.SaveFormatVersion != CurrentSaveFormatVersion)
            {
                throw new InvalidDataException(
                    $"New writes require save format version {CurrentSaveFormatVersion}; read a legacy save before writing it.");
            }
            Validate(envelope);

            var directory = Path.GetDirectoryName(Path.GetFullPath(primaryPath));
            Directory.CreateDirectory(directory);
            var temporaryPath = primaryPath + ".tmp";
            var backupPath = primaryPath + ".bak";
            var json = JsonConvert.SerializeObject(envelope, CanonicalJson.DefaultSettings());

            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            var verification = ReadFile(temporaryPath);
            Validate(verification);

            if (File.Exists(primaryPath))
            {
                ReplaceWithBackup(temporaryPath, primaryPath, backupPath);
            }
            else
            {
                File.Move(temporaryPath, primaryPath);
            }
        }

        public Result<SaveEnvelopeV1> ReadWithRecovery(string primaryPath)
        {
            var primary = TryRead(primaryPath);
            if (primary.IsSuccess) return primary;

            var backup = TryRead(primaryPath + ".bak");
            return backup.IsSuccess
                ? backup
                : Result<SaveEnvelopeV1>.Failure(
                    $"Primary save failed: {string.Join("; ", primary.Errors)}",
                    $"Backup save failed: {string.Join("; ", backup.Errors)}");
        }

        private static Result<SaveEnvelopeV1> TryRead(string path)
        {
            if (!File.Exists(path)) return Result<SaveEnvelopeV1>.Failure($"Save file not found: {path}");
            try
            {
                var envelope = ReadFile(path);
                Validate(envelope);
                return Result<SaveEnvelopeV1>.Success(envelope);
            }
            catch (Exception exception)
            {
                return Result<SaveEnvelopeV1>.Failure(exception.Message);
            }
        }

        private static SaveEnvelopeV1 ReadFile(string path)
        {
            JObject root;
            using (var textReader = new StringReader(File.ReadAllText(path, Encoding.UTF8)))
            using (var jsonReader = new JsonTextReader(textReader) { DateParseHandling = DateParseHandling.None })
            {
                root = JObject.Load(jsonReader);
            }
            var version = RequiredInteger(root, "SaveFormatVersion");
            if (version < MinimumReadableSaveFormatVersion || version > CurrentSaveFormatVersion)
            {
                throw new InvalidDataException($"Unsupported save format version {version}.");
            }
            if (version == 1 || version == 2 || version == 3 || version == 4 || version == 5 || version == 6 || version == 7 || version == 8 || version == 9 || version == 10)
                return UpgradeLegacy(root, version);
            return root.ToObject<SaveEnvelopeV1>(JsonSerializer.Create(CanonicalJson.DefaultSettings()));
        }

        private static SaveEnvelopeV1 UpgradeLegacy(JObject root, int sourceVersion)
        {
            var rawState = root["CampaignState"];
            if (rawState == null || rawState.Type != JTokenType.Object)
                throw new InvalidDataException("Campaign state is missing.");

            var storedHash = RequiredString(root, "CanonicalStateHash");
            var legacyHash = CanonicalJson.Sha256Hex(rawState);
            if (!StringComparer.Ordinal.Equals(legacyHash, storedHash))
                throw new InvalidDataException("Canonical state hash mismatch.");

            var state = rawState.ToObject<SecondDimension.Gameplay.State.CampaignState>(
                JsonSerializer.Create(CanonicalJson.DefaultSettings()));
            if (state == null) throw new InvalidDataException("Campaign state is missing.");
            var contentAuthorityVersion = RequiredString(root, "ContentAuthorityVersion");
            var campaignGuid = RequiredString(root, "CampaignGuid");
            var campaignSeed = RequiredLong(root, "CampaignSeed");
            if (!StringComparer.Ordinal.Equals(campaignGuid, state.CampaignGuid) || campaignSeed != state.CampaignSeed)
                throw new InvalidDataException("Envelope identity differs from campaign state.");
            if (!StringComparer.Ordinal.Equals(contentAuthorityVersion, state.ContentAuthorityVersion))
                throw new InvalidDataException("Envelope content authority differs from campaign state.");

            return NewCurrentEnvelope(
                contentAuthorityVersion,
                campaignGuid,
                campaignSeed,
                RequiredString(root, "DisplayTimestampUtc"),
                state,
                AppendMigrations(ReadMigrationHistory(root["MigrationHistory"]), sourceVersion));
        }

        private static SaveEnvelopeV1 NewCurrentEnvelope(
            string contentAuthorityVersion,
            string campaignGuid,
            long campaignSeed,
            string displayTimestampUtc,
            SecondDimension.Gameplay.State.CampaignState state,
            IReadOnlyList<string> migrationHistory) =>
            new SaveEnvelopeV1(
                CurrentSaveFormatVersion,
                contentAuthorityVersion,
                campaignGuid,
                campaignSeed,
                displayTimestampUtc,
                state,
                CanonicalJson.Sha256Hex(state),
                migrationHistory);

        private static IReadOnlyList<string> ReadMigrationHistory(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return Array.Empty<string>();
            if (token.Type != JTokenType.Array) throw new InvalidDataException("Migration history must be an array.");
            var result = new System.Collections.Generic.List<string>();
            foreach (var item in (JArray)token)
            {
                if (item.Type != JTokenType.String || string.IsNullOrWhiteSpace(item.Value<string>()))
                    throw new InvalidDataException("Migration history contains an invalid entry.");
                result.Add(item.Value<string>());
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> AppendMigrations(IReadOnlyList<string> source, int sourceVersion)
        {
            var result = new System.Collections.Generic.List<string>();
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    if (string.IsNullOrWhiteSpace(source[index]))
                        throw new InvalidDataException("Migration history contains an invalid entry.");
                    result.Add(source[index]);
                }
            }
            if (sourceVersion <= 1 && !result.Contains(V1ToV2MigrationId)) result.Add(V1ToV2MigrationId);
            if (sourceVersion <= 2 && !result.Contains(V2ToV3MigrationId)) result.Add(V2ToV3MigrationId);
            if (sourceVersion <= 3 && !result.Contains(V3ToV4MigrationId)) result.Add(V3ToV4MigrationId);
            if (sourceVersion <= 4 && !result.Contains(V4ToV5MigrationId)) result.Add(V4ToV5MigrationId);
            if (sourceVersion <= 5 && !result.Contains(V5ToV6MigrationId)) result.Add(V5ToV6MigrationId);
            if (sourceVersion <= 6 && !result.Contains(V6ToV7MigrationId)) result.Add(V6ToV7MigrationId);
            if (sourceVersion <= 7 && !result.Contains(V7ToV8MigrationId)) result.Add(V7ToV8MigrationId);
            if (sourceVersion <= 8 && !result.Contains(V8ToV9MigrationId)) result.Add(V8ToV9MigrationId);
            if (sourceVersion <= 9 && !result.Contains(V9ToV10MigrationId)) result.Add(V9ToV10MigrationId);
            if (sourceVersion <= 10 && !result.Contains(V10ToV11MigrationId)) result.Add(V10ToV11MigrationId);
            return result.AsReadOnly();
        }

        private static int RequiredInteger(JObject root, string name)
        {
            var token = root[name];
            if (token == null || token.Type != JTokenType.Integer)
                throw new InvalidDataException(name + " is missing or invalid.");
            return token.Value<int>();
        }

        private static long RequiredLong(JObject root, string name)
        {
            var token = root[name];
            if (token == null || token.Type != JTokenType.Integer)
                throw new InvalidDataException(name + " is missing or invalid.");
            return token.Value<long>();
        }

        private static string RequiredString(JObject root, string name)
        {
            var token = root[name];
            if (token == null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
                throw new InvalidDataException(name + " is missing or invalid.");
            return token.Value<string>();
        }

        private static void Validate(SaveEnvelopeV1 envelope)
        {
            if (envelope == null) throw new InvalidDataException("Save envelope is null.");
            if (envelope.SaveFormatVersion < MinimumReadableSaveFormatVersion ||
                envelope.SaveFormatVersion > CurrentSaveFormatVersion)
            {
                throw new InvalidDataException($"Unsupported save format version {envelope.SaveFormatVersion}.");
            }
            if (envelope.CampaignState == null) throw new InvalidDataException("Campaign state is missing.");

            var expected = CanonicalJson.Sha256Hex(envelope.CampaignState);
            if (!StringComparer.Ordinal.Equals(expected, envelope.CanonicalStateHash))
            {
                throw new InvalidDataException("Canonical state hash mismatch.");
            }
            if (!StringComparer.Ordinal.Equals(envelope.CampaignGuid, envelope.CampaignState.CampaignGuid) ||
                envelope.CampaignSeed != envelope.CampaignState.CampaignSeed)
            {
                throw new InvalidDataException("Envelope identity differs from campaign state.");
            }
            if (!StringComparer.Ordinal.Equals(
                    envelope.ContentAuthorityVersion,
                    envelope.CampaignState.ContentAuthorityVersion))
            {
                throw new InvalidDataException("Envelope content authority differs from campaign state.");
            }
        }

        private static void ReplaceWithBackup(string temporaryPath, string primaryPath, string backupPath)
        {
            try
            {
                File.Replace(temporaryPath, primaryPath, backupPath, ignoreMetadataErrors: true);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceWithPortableFallback(temporaryPath, primaryPath, backupPath);
            }
            catch (IOException)
            {
                ReplaceWithPortableFallback(temporaryPath, primaryPath, backupPath);
            }
        }

        private static void ReplaceWithPortableFallback(string temporaryPath, string primaryPath, string backupPath)
        {
            File.Copy(primaryPath, backupPath, overwrite: true);
            File.Delete(primaryPath);
            File.Move(temporaryPath, primaryPath);
        }
    }
}
