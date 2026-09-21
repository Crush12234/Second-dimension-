using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SaveRoundTripTests
    {
        private string _directory;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SecondDimensionM0Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _savePath = Path.Combine(_directory, "campaign.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void AtomicSaveRoundTripPreservesCanonicalStateHash()
        {
            var campaign = CampaignFactory.CreateM0Proof(20260811);
            var beforeHash = CanonicalJson.Sha256Hex(campaign);
            var store = new AtomicSaveStore();

            store.Write(_savePath, SaveEnvelopeV1.Create(campaign, UnixEpoch()));
            var loaded = store.ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
            Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState), Is.EqualTo(beforeHash));
            Assert.That(loaded.Value.CanonicalStateHash, Is.EqualTo(beforeHash));
        }

        [Test]
        public void LegacyV1HashIsValidatedBeforeExpandedStateDefaultsAreAdded()
        {
            var legacyState = LegacyV1StateToken(404);
            var legacyHash = CanonicalJson.Sha256Hex(legacyState);
            File.WriteAllText(_savePath, CanonicalJson.Serialize(LegacyV1Envelope(legacyState, legacyHash)));

            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
            Assert.That(loaded.Value.CampaignSeed, Is.EqualTo(404));
            Assert.That(loaded.Value.CampaignState.Profile, Is.Null);
            Assert.That(loaded.Value.CampaignState.OpeningFlow, Is.Null);
            Assert.That(loaded.Value.CampaignState.Guild.Inventory, Is.Empty);
            Assert.That(loaded.Value.CampaignState.Rules.PersonalXpPct, Is.EqualTo(100));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v1_to_v2_m1_state_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v2_to_v3_m2_battle_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v3_to_v4_m2_art_growth_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v4_to_v5_progression_foundation_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v5_to_v6_guild_city_foundation_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v6_to_v7_campaign_runtime_019_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v7_to_v8_campaign_playable_020_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v8_to_v9_progression_abyss_covenant_022_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v9_to_v10_world_gate_expedition_runtime_023_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v10_to_v11_creator_codes_rooms_028_defaults"));
            Assert.That(loaded.Value.CampaignState.Guild.Development.HallStageId,
                Is.EqualTo(GuildDevelopmentState.InitialHallStageId));
            Assert.That(loaded.Value.CampaignState.Guild.Development.Facilities.Count, Is.EqualTo(31));
            Assert.That(loaded.Value.CampaignState.Guild.GuildCity.CityPlots.Count, Is.EqualTo(12));
            Assert.That(loaded.Value.CanonicalStateHash,
                Is.EqualTo(CanonicalJson.Sha256Hex(loaded.Value.CampaignState)));
            Assert.That(loaded.Value.CanonicalStateHash, Is.Not.EqualTo(legacyHash));
        }

        [Test]
        public void CorruptLegacyV1StateStillFailsHashValidation()
        {
            var legacyState = LegacyV1StateToken(505);
            var legacyHash = CanonicalJson.Sha256Hex(legacyState);
            ((Dictionary<string, object>)legacyState["Guild"])["TreasuryXp"] = 1;
            File.WriteAllText(_savePath, CanonicalJson.Serialize(LegacyV1Envelope(legacyState, legacyHash)));

            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.False);
            Assert.That(string.Join("\n", loaded.Errors), Does.Contain("Canonical state hash mismatch"));
        }

        [Test]
        public void Update009V2SaveMigratesToV3BeforeBattleStateIsAdded()
        {
            var campaign = CampaignFactory.CreateM0Proof(515);
            var legacy = new SaveEnvelopeV1(
                2,
                campaign.ContentAuthorityVersion,
                campaign.CampaignGuid,
                campaign.CampaignSeed,
                UnixEpoch().ToString("O"),
                campaign,
                CanonicalJson.Sha256Hex(campaign),
                new[] { "save_v1_to_v2_m1_state_defaults" });
            File.WriteAllText(_savePath, CanonicalJson.Serialize(legacy));

            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
            Assert.That(loaded.Value.CampaignState.Battle, Is.Null);
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v2_to_v3_m2_battle_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v3_to_v4_m2_art_growth_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v4_to_v5_progression_foundation_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v5_to_v6_guild_city_foundation_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v6_to_v7_campaign_runtime_019_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v7_to_v8_campaign_playable_020_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v8_to_v9_progression_abyss_covenant_022_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v9_to_v10_world_gate_expedition_runtime_023_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v10_to_v11_creator_codes_rooms_028_defaults"));
            Assert.That(loaded.Value.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(loaded.Value.CampaignState)));
        }

        [Test]
        public void V4HashIsValidatedBeforeProgressionFoundationDefaultsAreAdded()
        {
            var rawState = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["CampaignGuid"] = "00000000-0000-0000-0000-000000000004",
                ["CampaignSeed"] = 804,
                ["ContentAuthorityVersion"] = "1.0",
                ["Rules"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["Mode"] = 1,
                    ["PermanentDeathEnabled"] = false,
                    ["DepartureEnabled"] = false
                },
                ["Guild"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["GuildId"] = "GUILD_V4",
                    ["TreasuryXp"] = 0,
                    ["Recruits"] = new[]
                    {
                        new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            ["RecruitId"] = "RECRUIT_V4",
                            ["CurrentHp"] = 80,
                            ["MaximumHp"] = 80,
                            ["CurrentMp"] = 12,
                            ["MaximumMp"] = 12,
                            ["VitalsInitialized"] = true
                        }
                    },
                    ["Unions"] = Array.Empty<object>(),
                    ["Inventory"] = Array.Empty<object>()
                }
            };
            var rawHash = CanonicalJson.Sha256Hex(rawState);
            var envelope = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["SaveFormatVersion"] = 4,
                ["ContentAuthorityVersion"] = "1.0",
                ["CampaignGuid"] = "00000000-0000-0000-0000-000000000004",
                ["CampaignSeed"] = 804,
                ["DisplayTimestampUtc"] = "1970-01-01T00:00:00.0000000Z",
                ["CampaignState"] = rawState,
                ["CanonicalStateHash"] = rawHash,
                ["MigrationHistory"] = Array.Empty<object>()
            };
            File.WriteAllText(_savePath, CanonicalJson.Serialize(envelope));

            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
            Assert.That(loaded.Value.MigrationHistory,
                Is.EqualTo(new[]
                {
                    "save_v4_to_v5_progression_foundation_defaults",
                    "save_v5_to_v6_guild_city_foundation_defaults",
                    "save_v6_to_v7_campaign_runtime_019_defaults",
                    "save_v7_to_v8_campaign_playable_020_defaults",
                    "save_v8_to_v9_progression_abyss_covenant_022_defaults",
                    "save_v9_to_v10_world_gate_expedition_runtime_023_defaults",
                    "save_v10_to_v11_creator_codes_rooms_028_defaults"
                }));
            Assert.That(loaded.Value.CampaignState.Guild.Development.Facilities.Count, Is.EqualTo(31));
            Assert.That(loaded.Value.CampaignState.Guild.GuildCity.CityPlots.Count, Is.EqualTo(12));
            Assert.That(loaded.Value.CampaignState.Guild.Recruits[0].Progression.Level, Is.EqualTo(1));
            Assert.That(loaded.Value.CampaignState.Guild.Recruits[0].Progression.TotalPersonalXp, Is.Zero);
            Assert.That(loaded.Value.CanonicalStateHash, Is.Not.EqualTo(rawHash));
            Assert.That(loaded.Value.CanonicalStateHash,
                Is.EqualTo(CanonicalJson.Sha256Hex(loaded.Value.CampaignState)));
        }

        [Test]
        public void V5SaveWithoutGuildCityMigratesToV6WithPermanentRosterFoundation()
        {
            var campaign = CampaignFactory.CreateM0Proof(505);
            var rawState = JObject.Parse(CanonicalJson.Serialize(campaign));
            var guild = (JObject)rawState["Guild"];
            guild.Remove("GuildCity");
            var rawHash = CanonicalJson.Sha256Hex(rawState);
            var envelope = new JObject
            {
                ["SaveFormatVersion"] = 5,
                ["ContentAuthorityVersion"] = campaign.ContentAuthorityVersion,
                ["CampaignGuid"] = campaign.CampaignGuid,
                ["CampaignSeed"] = campaign.CampaignSeed,
                ["DisplayTimestampUtc"] = UnixEpoch().ToString("O"),
                ["CampaignState"] = rawState,
                ["CanonicalStateHash"] = rawHash,
                ["MigrationHistory"] = new JArray(
                    "save_v1_to_v2_m1_state_defaults",
                    "save_v2_to_v3_m2_battle_defaults",
                    "save_v3_to_v4_m2_art_growth_defaults",
                    "save_v4_to_v5_progression_foundation_defaults")
            };
            File.WriteAllText(_savePath, CanonicalJson.Serialize(envelope));

            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
            Assert.That(loaded.Value.MigrationHistory,
                Contains.Item("save_v5_to_v6_guild_city_foundation_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v6_to_v7_campaign_runtime_019_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v7_to_v8_campaign_playable_020_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v8_to_v9_progression_abyss_covenant_022_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v9_to_v10_world_gate_expedition_runtime_023_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v10_to_v11_creator_codes_rooms_028_defaults"));
            Assert.That(loaded.Value.CampaignState.Guild.GuildCity.CityPlots.Count, Is.EqualTo(12));
            Assert.That(loaded.Value.CampaignState.Guild.GuildCity.CharterBuildCredits, Is.EqualTo(1));
        }

        [Test]
        public void V9SaveMigratesThroughV11WithWorldGateAndCreatorDefaults()
        {
            var campaign = CampaignFactory.CreateM0Proof(909);
            var rawState = JObject.Parse(CanonicalJson.Serialize(campaign));
            var playable = rawState["Guild"]?["GuildCity"]?["Strategic017H"]?["Campaign019"]?["Playable020"] as JObject;
            Assert.That(playable, Is.Not.Null);
            playable.Remove("WorldGate023");

            var rawHash = CanonicalJson.Sha256Hex(rawState);
            var envelope = new JObject
            {
                ["SaveFormatVersion"] = 9,
                ["ContentAuthorityVersion"] = campaign.ContentAuthorityVersion,
                ["CampaignGuid"] = campaign.CampaignGuid,
                ["CampaignSeed"] = campaign.CampaignSeed,
                ["DisplayTimestampUtc"] = UnixEpoch().ToString("O"),
                ["CampaignState"] = rawState,
                ["CanonicalStateHash"] = rawHash,
                ["MigrationHistory"] = new JArray(
                    "save_v1_to_v2_m1_state_defaults",
                    "save_v2_to_v3_m2_battle_defaults",
                    "save_v3_to_v4_m2_art_growth_defaults",
                    "save_v4_to_v5_progression_foundation_defaults",
                    "save_v5_to_v6_guild_city_foundation_defaults",
                    "save_v6_to_v7_campaign_runtime_019_defaults",
                    "save_v7_to_v8_campaign_playable_020_defaults",
                    "save_v8_to_v9_progression_abyss_covenant_022_defaults")
            };
            File.WriteAllText(_savePath, CanonicalJson.Serialize(envelope));

            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(11));
            Assert.That(loaded.Value.MigrationHistory,
                Contains.Item("save_v9_to_v10_world_gate_expedition_runtime_023_defaults"));
            Assert.That(loaded.Value.MigrationHistory, Contains.Item("save_v10_to_v11_creator_codes_rooms_028_defaults"));
            var worldGate = loaded.Value.CampaignState.Guild.GuildCity.Strategic017H
                .Campaign019.Playable020.WorldGate023;
            Assert.That(worldGate, Is.Not.Null);
            Assert.That(worldGate.CurrentWorldId, Is.EqualTo("SKYHOME"));
            Assert.That(worldGate.TravelSupplies, Is.EqualTo(30));
            Assert.That(worldGate.ActiveOperation, Is.Null);
            Assert.That(loaded.Value.CanonicalStateHash,
                Is.EqualTo(CanonicalJson.Sha256Hex(loaded.Value.CampaignState)));
        }

        [Test]
        public void LegacyPrimaryHashFailureStillRecoversFromV2Backup()
        {
            var store = new AtomicSaveStore();
            store.Write(_savePath + ".bak", SaveEnvelopeV1.Create(CampaignFactory.CreateM0Proof(606), UnixEpoch()));
            var legacyState = LegacyV1StateToken(707);
            File.WriteAllText(_savePath, CanonicalJson.Serialize(
                LegacyV1Envelope(legacyState, new string('0', 64))));

            var loaded = store.ReadWithRecovery(_savePath);

            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.CampaignSeed, Is.EqualTo(606));
        }

        [Test]
        public void CorruptPrimaryFallsBackToKnownGoodBackup()
        {
            var store = new AtomicSaveStore();
            var first = CampaignFactory.CreateM0Proof(101);
            var second = CampaignFactory.CreateM0Proof(202);

            store.Write(_savePath, SaveEnvelopeV1.Create(first, UnixEpoch()));
            store.Write(_savePath, SaveEnvelopeV1.Create(second, UnixEpoch()));
            File.WriteAllText(_savePath, "{ corrupt");

            var recovered = store.ReadWithRecovery(_savePath);
            Assert.That(recovered.IsSuccess, Is.True, string.Join("\n", recovered.Errors));
            Assert.That(recovered.Value.CampaignSeed, Is.EqualTo(101));
        }

        [Test]
        public void UnknownFutureSaveVersionFailsSafely()
        {
            var campaign = CampaignFactory.CreateM0Proof(303);
            var valid = SaveEnvelopeV1.Create(campaign, UnixEpoch());
            var unsupported = new SaveEnvelopeV1(
                99,
                valid.ContentAuthorityVersion,
                valid.CampaignGuid,
                valid.CampaignSeed,
                valid.DisplayTimestampUtc,
                valid.CampaignState,
                valid.CanonicalStateHash,
                valid.MigrationHistory);
            File.WriteAllText(_savePath, CanonicalJson.Serialize(unsupported));

            var result = new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(result.IsSuccess, Is.False);
        }

        private static DateTime UnixEpoch() =>
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static Dictionary<string, object> LegacyV1StateToken(long seed) =>
            new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["CampaignGuid"] = "00000000-0000-0000-0000-000000000001",
            ["CampaignSeed"] = seed,
            ["ContentAuthorityVersion"] = "1.0",
            ["Rules"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Mode"] = 1,
                ["PermanentDeathEnabled"] = false,
                ["DepartureEnabled"] = false
            },
            ["Guild"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["GuildId"] = "GUILD_M0_PROOF",
                ["TreasuryXp"] = 0,
                ["Recruits"] = Array.Empty<object>(),
                ["Unions"] = Array.Empty<object>()
            }
        };

        private static Dictionary<string, object> LegacyV1Envelope(
            Dictionary<string, object> state,
            string stateHash) => new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["SaveFormatVersion"] = 1,
            ["ContentAuthorityVersion"] = "1.0",
            ["CampaignGuid"] = "00000000-0000-0000-0000-000000000001",
            ["CampaignSeed"] = state["CampaignSeed"],
            ["DisplayTimestampUtc"] = "1970-01-01T00:00:00.0000000Z",
            ["CampaignState"] = state,
            ["CanonicalStateHash"] = stateHash,
            ["MigrationHistory"] = Array.Empty<object>()
        };
    }
}
