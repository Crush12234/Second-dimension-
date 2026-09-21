#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerLedgerIndex110Tests
    {
        [Test]
        public void ExactReceiptCountsOrdinalMembershipAndFreshInputArePreserved110()
        {
            var ids = new List<string> { "RECEIPT_A", "RECEIPT_A", "receipt_a", null, null, "", " " };
            var index = Index(ids);
            foreach (var key in new[] { "RECEIPT_A", "receipt_a", "Receipt_A", "missing", null, "", " " })
            {
                var expectedCount = ids.Count(id => StringComparer.Ordinal.Equals(id, key));
                Assert.That(Read<int>(index, "AppliedCount", key), Is.EqualTo(expectedCount));
                Assert.That(Read<int>(index, "BattleReturnCount", key), Is.EqualTo(expectedCount));
                var expectedMember = !string.IsNullOrWhiteSpace(key) && ids.Contains(key, StringComparer.Ordinal);
                Assert.That(Read<bool>(index, "HasClaimedReward", key), Is.EqualTo(expectedMember));
                Assert.That(Read<bool>(index, "HasAdventureAuthority", key), Is.EqualTo(expectedMember));
            }
            ids.RemoveAt(0);
            var next = Index(ids);
            Assert.That(Read<int>(index, "AppliedCount", "RECEIPT_A"), Is.EqualTo(2));
            Assert.That(Read<int>(next, "AppliedCount", "RECEIPT_A"), Is.EqualTo(1),
                "Every validation must index its current input; no prior index is retained.");
        }

        [Test]
        public void SuccessfulAuthorityReadDoesNotHideLaterReceiptOrProofChanges110()
        {
            // Existing command fixture uses a labelled synthetic victory return.
            // The real begin/step/return/completion authorities produce floor1.
            var fixture = new TowerHeroRewards094Tests();
            fixture.SetUp();
            var fresh = (CampaignState)typeof(TowerHeroRewards094Tests).GetMethod(
                "NewCampaign", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            var complete = (CampaignState)typeof(TowerHeroRewards094Tests).GetMethod(
                "CompleteNewFloor", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(fixture, new object[] { fresh, 1 });
            var registry = CampaignRegistry022.LoadFromResources();
            var snapshot = JObject.FromObject(complete, JsonSerializer.Create(CanonicalJson.DefaultSettings()));
            var before = CanonicalJson.Serialize(complete);
            Assert.That(Validate(complete, registry), Is.True);
            for (var variant = 0; variant < 5; variant++)
            {
                var token = (JObject)snapshot.DeepClone();
                var guild = token["Guild"];
                var state = guild["GuildCity"]["Strategic017H"]["Campaign019"]["Playable020"]["Progression022"];
                if (variant == 0) ((JArray)state["AppliedReceiptIds"]).Clear();
                if (variant == 1) ((JArray)guild["Development"]["ClaimedBattleRewardIds"]).Clear();
                if (variant == 2) ((JArray)guild["GuildCity"]["AppliedBattleReturnIds"]).Clear();
                if (variant == 3) ((JArray)guild["Development"]["AppliedAdventureAuthorityIds"]).Clear();
                if (variant == 4) state["AbyssAuthorityEntries"][0]["EntryHash"] = "invalid_hash_110";
                var changed = token.ToObject<CampaignState>();
                Assert.That(Validate(changed, registry), Is.False, "Tamper variant " + variant);
                Assert.That(Validate(complete, registry), Is.True, "The original valid snapshot remains valid.");
            }
            Assert.That(CanonicalJson.Serialize(complete), Is.EqualTo(before));
        }

        static object Index(IReadOnlyList<string> ids)
        {
            var type = typeof(CampaignProgressionCommandService022).GetNestedType(
                "AbyssValidationLedgerIndex110", BindingFlags.NonPublic);
            return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { ids, ids, ids, ids }, null);
        }
        static T Read<T>(object index, string method, string key) => (T)index.GetType()
            .GetMethod(method).Invoke(index, new object[] { key });
        static bool Validate(CampaignState campaign, CampaignRegistry022 registry) =>
            (bool)typeof(CampaignProgressionCommandService022).GetMethod("ValidateAbyssAuthority084",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] {
                    campaign, registry, campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022 });
    }
}
#endif
