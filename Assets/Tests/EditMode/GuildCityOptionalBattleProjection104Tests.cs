#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class ExpeditionDeck089Tests
    {
        [Test]
        public void OptionalCardCommitKeepsGuildProjectionReadableBeforeAndAfterReload104()
        {
            var path = OptionalProjectionSavePath104();
            try
            {
                var initial = OptionalProjectionReadyCampaign104();
                var operation = WorldGate(initial).ActiveOperation;
                var card = operation.ExpeditionDeck089.CurrentRow.Single(
                    ExpeditionDeckService089.IsOptionalBattleCard089);
                WriteOptionalProjection104(path, initial);
                var coordinator = OptionalProjectionCoordinator104(path);

                // Exercise the same public commit used by the revealed card.
                // It persists a real pending request before rebuilding the UI.
                var committed = coordinator.CommitExpeditionRouteCard089(card.CardId);
                Assert.That(committed.Succeeded, Is.True, committed.Message);
                var loaded = new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True,
                    string.Join("\n", loaded.Errors));
                var campaign = loaded.Value.CampaignState;
                var request = campaign.Guild.GuildCity.PendingEncounter;
                Assert.That(request, Is.Not.Null);
                Assert.That(request.NodeId, Is.Not.EqualTo(operation.CurrentNodeId),
                    "An optional card owns a synthetic encounter node, not the route node.");
                Assert.That(_commands.HasPendingOptionalBattle089(campaign), Is.True);

                var savedBytes = File.ReadAllBytes(path);
                AssertOptionalProjectionReadable104(coordinator, operation.OperationId);
                AssertOptionalProjectionReadable104(
                    OptionalProjectionCoordinator104(path), operation.OperationId);
                CollectionAssert.AreEqual(savedBytes, File.ReadAllBytes(path),
                    "Projection and reload must not rewrite the committed save.");
            }
            finally { DeleteOptionalProjectionSave104(path); }
        }

        [TestCase("request_node")]
        [TestCase("expedition_node")]
        public void OptionalCardProjectionRejectsMismatchedNode104(string mismatch)
        {
            var path = OptionalProjectionSavePath104();
            try
            {
                var initial = OptionalProjectionReadyCampaign104();
                var card = WorldGate(initial).ActiveOperation.ExpeditionDeck089
                    .CurrentRow.Single(ExpeditionDeckService089.IsOptionalBattleCard089);
                // Normalize the valid fixture through the same coordinator and
                // public commit as native play before introducing one mismatch.
                // Otherwise bootstrap initialization, unrelated to projection,
                // would rewrite the intentionally minimal test campaign.
                WriteOptionalProjection104(path, initial);
                var commitCoordinator = OptionalProjectionCoordinator104(path);
                var committed = commitCoordinator.CommitExpeditionRouteCard089(card.CardId);
                Assert.That(committed.Succeeded, Is.True, committed.Message);
                var loaded = new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True,
                    string.Join("\n", loaded.Errors));
                var campaign = loaded.Value.CampaignState;
                var document = JObject.FromObject(campaign);
                var city = (JObject)document["Guild"]["GuildCity"];
                if (mismatch == "request_node")
                    city["PendingEncounter"]["NodeId"] = "NOT_THE_COMMITTED_CARD_NODE_104";
                else
                    city["Expedition"]["CurrentNodeId"] = "NOT_THE_COMMITTED_ROUTE_NODE_104";
                var invalid = document.ToObject<CampaignState>();
                WriteOptionalProjection104(path, invalid);
                var savedBytes = File.ReadAllBytes(path);
                var coordinator = OptionalProjectionCoordinator104(path);
                Assert.Throws<KeyNotFoundException>(() =>
                {
                    var unused = coordinator.GuildCity017D;
                }, "A mismatched BOARD023 mirror must remain on the strict legacy diagnostic path.");
                CollectionAssert.AreEqual(savedBytes, File.ReadAllBytes(path));
            }
            finally { DeleteOptionalProjectionSave104(path); }
        }

        CampaignState OptionalProjectionReadyCampaign104()
        {
            // Existing deterministic fixture also used by the real optional
            // battle return/cost tests. No saved player progress is modified.
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89304);
            return Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020,
                HeroMaster300CreatorRegistry087.Load().Source));
        }

        static void AssertOptionalProjectionReadable104(
            M1RuntimeCoordinator coordinator, string operationId)
        {
            global::SecondDimension.Presentation.GuildCity017D
                .GuildCityPresentationState017D cityView = null;
            Assert.DoesNotThrow(() => cityView = coordinator.GuildCity017D,
                "The real optional encounter mirror belongs to World Gate, not GuildCityContent.");
            Assert.That(cityView.IsAvailable, Is.True, cityView.Error);
            Assert.That(cityView.Expedition, Is.Null);
            Assert.That(cityView.HasPendingEncounter, Is.True);
            var worldView = coordinator.CampaignWorldGate023;
            Assert.That(worldView.IsAvailable, Is.True, worldView.Error);
            Assert.That(worldView.ActiveOperationId, Is.EqualTo(operationId));
            Assert.That(worldView.PendingRequiresCertifiedBattle, Is.True);
        }

        static M1RuntimeCoordinator OptionalProjectionCoordinator104(string path) =>
            new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath,
                "Authority", "CONTENT"), path);

        static string OptionalProjectionSavePath104() => Path.Combine(
            Path.GetTempPath(), "sd_optional_projection_104_" +
                Guid.NewGuid().ToString("N") + ".json");

        static void WriteOptionalProjection104(string path, CampaignState campaign) =>
            new AtomicSaveStore().Write(path, SaveEnvelopeV1.Create(campaign,
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        static void DeleteOptionalProjectionSave104(string path)
        {
            foreach (var suffix in new[] { string.Empty, ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }
}
#endif
