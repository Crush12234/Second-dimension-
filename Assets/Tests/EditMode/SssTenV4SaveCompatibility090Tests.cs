using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SssTenV4SaveCompatibility090Tests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CurrentV11AcceptsNewLedgersAbsentOrExplicitlyEmpty090(
            bool explicitEmptyLedgers)
        {
            var campaign = CampaignFactory.CreateM0Proof(900911L);
            var serializer = JsonSerializer.Create(CanonicalJson.DefaultSettings());
            var root = JObject.FromObject(
                SaveEnvelopeV1.Create(campaign, UnixEpoch()), serializer);
            var rawState = (JObject)root["CampaignState"];
            rawState.Remove("SssV4090");
            var creator = (JObject)rawState["Guild"]?["GuildCity"]?["Strategic017H"]
                ?["Campaign019"]?["Playable020"]?["CreatorAccess028"];
            var development = (JObject)rawState["Guild"]?["Development"];
            Assert.That(creator, Is.Not.Null);
            Assert.That(development, Is.Not.Null);
            creator.Remove("GrowthAllocations10000");
            creator.Remove("RelicReceipts1000");
            development.Remove("AppliedAdventureAuthorityIds");
            if (explicitEmptyLedgers)
            {
                creator["GrowthAllocations10000"] = new JArray();
                creator["RelicReceipts1000"] = new JArray();
                development["AppliedAdventureAuthorityIds"] = new JArray();
            }

            var rawHash = CanonicalJson.Sha256Hex(rawState);
            root["CanonicalStateHash"] = rawHash;
            var path = Path.Combine(Path.GetTempPath(),
                "sss_v4_v11_hash_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, CanonicalJson.Serialize(root));
                var loaded = new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True,
                    string.Join(" | ", loaded.Errors));
                Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(11));
                Assert.That(CanonicalJson.Sha256Hex(
                    loaded.Value.CampaignState), Is.EqualTo(rawHash));
                Assert.That(loaded.Value.CampaignState.SssV4090, Is.Null,
                    "V4 state stays optional until the first real V4 transaction.");
                var restored = loaded.Value.CampaignState.Guild.GuildCity
                    .Strategic017H.Campaign019.Playable020.CreatorAccess028;
                Assert.That(restored.GrowthAllocations10000, Is.Empty);
                Assert.That(restored.RelicReceipts1000, Is.Empty);
                Assert.That(loaded.Value.CampaignState.Guild.Development
                    .AppliedAdventureAuthorityIds, Is.Empty);
                Assert.That(SssTenV4CampaignAccessor090.Read(
                    loaded.Value.CampaignState).Progression.world
                    .campaignBattleWins, Is.EqualTo("0"));
            }
            finally
            {
                DeleteIfPresent(path);
                DeleteIfPresent(path + ".bak");
                DeleteIfPresent(path + ".tmp");
            }
        }

        private static DateTime UnixEpoch() =>
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
