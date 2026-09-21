#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildCityWorldGateMirrorProjection090Tests
    {
        private const string ChapterId090 = "CH018_001";
        private const string UnionId090 = "WORLD_GATE_MIRROR_UNION_090";
        private const string ActorId090 = "WORLD_GATE_MIRROR_ACTOR_090";
        private const string AssistantId090 = "WORLD_GATE_MIRROR_ASSISTANT_090";

        [Test]
        public void ReloadedWorldGateMirrorKeepsApplicantsAndWorldGateReadable090()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT");
            var recruitmentContent = RecruitmentContent.LoadFromDirectory(
                contentRoot);
            var cityContent = GuildCityContent017D.LoadFromDirectory(
                Path.Combine(contentRoot, "GUILD_CITY_017D"));
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var recruitment = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(
                        contentRoot)),
                heroes);
            var campaign020 = new CampaignPlayableCommandService020();
            var catalog020 = new Campaign020RuleCatalogAdapter(
                CampaignRegistry020.LoadFromResources());
            var catalog023 = new Campaign023RuleCatalogAdapter(
                CampaignRegistry023.LoadFromResources());
            var worldGate = new CampaignWorldGateCommandService023();

            var campaign = CreateWorldBoardReadyCampaign090(
                9002301L, recruitment, recruitmentContent, cityContent,
                campaign020, catalog020);
            campaign = Require090(worldGate.BeginOperation(
                campaign, catalog023, ChapterId090, new[] { UnionId090 },
                catalog020, heroes));
            var sourceOperation = WorldGate090(campaign).ActiveOperation;
            Assert.That(sourceOperation, Is.Not.Null);
            Assert.That(CampaignWorldGateCommandService023
                    .HasCanonicalLegacyHandoff084(
                        campaign.Guild.GuildCity, sourceOperation),
                Is.True, "The fixture must contain the exact live World Gate mirror.");

            var savePath = Path.Combine(Path.GetTempPath(),
                "sd_world_gate_mirror_090_" + Guid.NewGuid().ToString("N") +
                ".json");
            var invalidPath = Path.Combine(Path.GetTempPath(),
                "sd_invalid_legacy_board_090_" + Guid.NewGuid().ToString("N") +
                ".json");
            try
            {
                Write090(savePath, campaign);
                var savedBytes090 = File.ReadAllBytes(savePath);
                var coordinator = new M1RuntimeCoordinator(
                    contentRoot, savePath);

                M1PresentationState shellView090 = null;
                Assert.DoesNotThrow(() => shellView090 = coordinator.State,
                    "The standard shell must project loadouts when the optional opening tutorial flow is absent.");
                Assert.That(shellView090, Is.Not.Null);
                Assert.That(shellView090.Recruits.Count, Is.EqualTo(2));
                foreach (var recruitView090 in shellView090.Recruits)
                    Assert.That(recruitView090.HasManualEquipAction, Is.False,
                        recruitView090.RecruitId);
                CollectionAssert.AreEqual(savedBytes090, File.ReadAllBytes(savePath),
                    "Reading a valid post-opening save must not normalize or rewrite it.");

                global::SecondDimension.Presentation.GuildCity017D
                    .GuildCityPresentationState017D cityView = null;
                Assert.DoesNotThrow(() => cityView = coordinator.GuildCity017D,
                    "A canonical World Gate mirror must not be resolved through the legacy GuildCity board catalog.");
                Assert.That(cityView, Is.Not.Null);
                Assert.That(cityView.IsAvailable, Is.True, cityView.Error);
                Assert.That(cityView.Expedition, Is.Null,
                    "The shared battle-bridge mirror is not a second player-facing GuildCity expedition.");
                Assert.That(cityView.Applicants.Count, Is.GreaterThan(0),
                    "The saved Applicant Board must remain readable during an active World Gate run.");

                var worldView = coordinator.CampaignWorldGate023;
                Assert.That(worldView.IsAvailable, Is.True, worldView.Error);
                Assert.That(worldView.ActiveOperationId,
                    Is.EqualTo(sourceOperation.OperationId));
                Assert.That(worldView.ActiveDefinitionId,
                    Is.EqualTo(sourceOperation.DefinitionId));

                // A non-matching legacy expedition must remain on the strict
                // GuildCity lookup path; the World Gate exception applies only
                // to the exact live mirror created by Campaign 023.
                var invalid = WithoutActiveWorldGate090(campaign);
                Assert.That(CampaignWorldGateCommandService023
                        .HasCanonicalLegacyHandoff084(
                            invalid.Guild.GuildCity,
                            WorldGate090(invalid).ActiveOperation),
                    Is.False);
                Write090(invalidPath, invalid);
                var invalidCoordinator = new M1RuntimeCoordinator(
                    contentRoot, invalidPath);
                Assert.Throws<KeyNotFoundException>(() =>
                {
                    var unused = invalidCoordinator.GuildCity017D;
                }, "A genuinely invalid legacy board ID must not be silently suppressed.");
            }
            finally
            {
                DeleteSave090(savePath);
                DeleteSave090(invalidPath);
            }
        }

        private static CampaignState CreateWorldBoardReadyCampaign090(
            long seed,
            GuildCityRecruitmentService017D recruitment,
            RecruitmentContent recruitmentContent,
            GuildCityContent017D cityContent,
            CampaignPlayableCommandService020 campaign020,
            Campaign020RuleCatalogAdapter catalog020)
        {
            var actor = new RecruitState(ActorId090, 120, 120, 30, 30);
            var assistant = new RecruitState(
                AssistantId090, 110, 110, 30, 30);
            var union = new UnionState(
                UnionId090, "Mirror Union", UnionKind.Normal,
                actor.RecruitId, new[] { actor.RecruitId, assistant.RecruitId },
                "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(
                source.Guild.GuildId, 50000,
                new[] { actor, assistant }, new[] { union },
                source.Guild.Inventory, source.Guild.Development,
                guildCity: null);
            var campaign = source.With(guild, source.OpeningFlow);
            campaign = Require090(LegacyApplicantBoardFixture124.Commit(
                campaign, recruitmentContent, cityContent));

            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019.With(
                activeChapterId: ChapterId090,
                activeOperation: null,
                replaceActiveOperation: true,
                pendingReceipt: null,
                replacePendingReceipt: true,
                completedChapterIds: Array.Empty<string>(),
                unlockedWorldIds: new[] { "SKYHOME" },
                lastCheckpointId: "world_gate_mirror_090_ready",
                playable020: CampaignPlayableState020.Default(),
                replacePlayable020: true);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            city = city.With(
                activeContract: null,
                replaceActiveContract: true,
                expedition: null,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true,
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: progress.LastCheckpointId);
            campaign = new CampaignState(
                campaign.CampaignGuid, seed,
                campaign.ContentAuthorityVersion, campaign.Rules,
                campaign.Guild.WithGuildCity(city), campaign.Profile,
                campaign.OpeningFlow, battle: null);

            campaign = Require090(campaign020.BeginOperation(
                campaign, catalog020, ChapterId090));
            campaign = Require090(campaign020.CommitNonBattleStep(
                campaign, catalog020, "SUCCESS"));
            return Require090(campaign020.ApplyStepReceiptExactlyOnce(
                campaign, catalog020));
        }

        private static CampaignState WithoutActiveWorldGate090(
            CampaignState campaign)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var runtime = playable.WorldGate023.With(
                activeOperation: null,
                replaceActiveOperation: true,
                lastCheckpointId: "invalid_legacy_board_090");
            playable = playable.With(
                worldGate023: runtime,
                replaceWorldGate023: true,
                lastCheckpointId: runtime.LastCheckpointId);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: playable.LastCheckpointId);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: strategic.LastCheckpointId);
            return campaign.With(
                campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static WorldGateRuntimeState023 WorldGate090(
            CampaignState campaign) => campaign.Guild.GuildCity.Strategic017H
                .Campaign019.Playable020.WorldGate023;

        private static CampaignState Require090(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void Write090(string path, CampaignState campaign) =>
            new AtomicSaveStore().Write(path,
                SaveEnvelopeV1.Create(campaign,
                    new DateTime(1970, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc)));

        private static void DeleteSave090(string path)
        {
            foreach (var suffix in new[] { string.Empty, ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }
}
#endif
