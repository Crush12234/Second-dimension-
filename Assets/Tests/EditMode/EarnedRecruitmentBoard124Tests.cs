using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EarnedRecruitmentBoard124Tests
    {
        RecruitmentContent _content;
        GuildCityContent017D _cityContent;
        GuildCityRecruitmentService017D _service;
        HeroMaster300Catalog087 _heroes;

        [OneTimeSetUp]
        public void LoadExistingAuthorities124()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _content = RecruitmentContent.LoadFromDirectory(root);
            _cityContent = GuildCityContent017D.LoadFromDirectory(Path.Combine(root, "GUILD_CITY_017D"));
            _heroes = HeroMaster300CreatorRegistry087.Load().Source;
            _service = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(RecruitAutoGenerationCatalog010.LoadFromContentRoot(root)),
                _heroes, new RecruitTreeProgressionService070(DeepProgressionCatalog070.LoadFromContentRoot(root)));
        }

        [Test]
        public void EmptyEarnedQueueNeverMintsBoardChargesXpOrAdvancesCounters124()
        {
            var campaign = Create124(0);
            var canonical = CanonicalJson.Serialize(campaign);
            Assert.That(Require124(_service.CommitBoard(campaign, _content, _cityContent)), Is.SameAs(campaign));
            Assert.That(Require124(_service.RefreshBoard(campaign, _content, _cityContent)), Is.SameAs(campaign));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard, Is.Null);
            Assert.That(CanonicalJson.Serialize(campaign), Is.EqualTo(canonical));
        }

        [Test]
        public void ExistingFixedCharterCompanionsAllowFirstStoryWithoutRandomRecurringRecruit124()
        {
            var campaign = Create124(recruitCount: 6);
            var expeditions = new GuildCityExpeditionService017D();
            var blocked = expeditions.AcceptContract(campaign, _cityContent, GuildCityExpeditionService017D.FirstStoryContractId066);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors, Does.Contain("GC017D_FIRST_RECURRING_RECRUIT_REQUIRED"));
            var roster = FirstHourRosterService071.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            campaign = Require124(roster.EnsureCharterRoster(campaign));
            Assert.That(campaign.Guild.Recruits, Has.Count.EqualTo(10));
            foreach (var id in FirstHourRosterService071.CharterStableRecruitIds)
                Assert.That(campaign.Guild.Recruits.Count(value => value.AuthoredStableRecruitId == id), Is.EqualTo(1));
            var unchanged = Require124(_service.CommitBoard(campaign, _content, _cityContent));
            Assert.That(unchanged, Is.SameAs(campaign));
            Assert.That(unchanged.Guild.GuildCity.RecruitmentBoard, Is.Null);
            var accepted = Require124(expeditions.AcceptContract(unchanged, _cityContent,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            Assert.That(accepted.Guild.GuildCity.ActiveContract.ContractId, Is.EqualTo(GuildCityExpeditionService017D.FirstStoryContractId066));
        }

        [Test]
        public void OnlySavedEligibleExactCatalogLeadsCreateBoardAndSigningConsumesOnce124()
        {
            var hero = Normal124().First();
            var excluded = _heroes.AcceptedHeroes.First(value => !value.IsNormalApplicantEligible);
            var campaign = WithLeads124(Create124(), hero.StableId, excluded.StableId, "UNKNOWN_UNAUTHORED_124");
            var beforeXp = campaign.Guild.TreasuryXp;
            campaign = Require124(_service.CommitBoard(campaign, _content, _cityContent));
            var board = campaign.Guild.GuildCity.RecruitmentBoard;
            Assert.That(board.Applicants, Has.Count.EqualTo(1));
            var offer = board.Applicants.Single();
            Assert.That(offer.AuthoredStableRecruitId, Is.EqualTo(hero.StableId));
            Assert.That(offer.DisplayName, Is.EqualTo(hero.Name));
            Assert.That(offer.RecruitId, Is.EqualTo(HeroMaster300CreatorRecruitProjection087.ExpeditionApplicantRecruitIdFor089(hero)));
            Assert.That(offer.SigningCostTreasuryXp, Is.EqualTo(hero.RecruitCostXp));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(beforeXp));
            Assert.That(Require124(_service.CommitBoard(campaign, _content, _cityContent)), Is.SameAs(campaign));
            var cost = _service.DescribeApplicantSigning090(campaign, offer).EffectiveCostTreasuryXp;
            campaign = Require124(_service.SignApplicant(campaign, offer.RecruitId));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(beforeXp - cost));
            Assert.That(World124(campaign).ExpeditionRecruitLeadIds089, Does.Not.Contain(hero.StableId));
            Assert.That(World124(campaign).ExpeditionRecruitLeadIds089, Does.Contain(excluded.StableId));
            Assert.That(World124(campaign).ExpeditionRecruitLeadIds089, Does.Contain("UNKNOWN_UNAUTHORED_124"));
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(campaign));
            var before = CanonicalJson.Serialize(reloaded);
            Assert.That(Require124(_service.RefreshBoard(reloaded, _content, _cityContent)), Is.SameAs(reloaded));
            Assert.That(CanonicalJson.Serialize(Require124(_service.SignApplicant(reloaded, offer.RecruitId))), Is.EqualTo(before));
            Assert.That(_service.DescribeHeroMasterDuplicate089(reloaded, offer).IsDuplicateOffer, Is.False);
        }

        [Test]
        public void FullUnconsumedBoardPreservesLegacyOffersAndQueuesExcessUntilSigning124()
        {
            var campaign = WithBoard124(Create124(), FrozenLegacyBoard124());
            var originals = campaign.Guild.GuildCity.RecruitmentBoard.Applicants.ToArray();
            var contacts = Normal124().Take(5).ToArray();
            campaign = WithLeads124(campaign, contacts.Select(value => value.StableId).ToArray());
            campaign = Require124(_service.CommitBoard(campaign, _content, _cityContent));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.Applicants, Has.Count.EqualTo(10));
            Assert.That(_service.CanInviteEarnedContacts124(campaign), Is.False);
            foreach (var offer in originals)
                Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.RecruitmentBoard.FindApplicant(offer.RecruitId)),
                    Is.EqualTo(CanonicalJson.Serialize(offer)), "Every unconsumed legacy offer retains its exact identity, slot and loadout.");
            var queued = _service.PendingExpeditionRecruitLeads089(campaign).Single();
            Assert.That(Require124(_service.RefreshBoard(campaign, _content, _cityContent)), Is.SameAs(campaign));
            var consumed = originals.First(value => value.Kind == ApplicantKind.Procedural);
            campaign = Require124(_service.SignApplicant(campaign, consumed.RecruitId));
            Assert.That(_service.CanInviteEarnedContacts124(campaign), Is.True);
            campaign = Require124(_service.CommitBoard(campaign, _content, _cityContent));
            var board = campaign.Guild.GuildCity.RecruitmentBoard;
            Assert.That(board.Applicants, Has.Count.EqualTo(10));
            Assert.That(board.FindApplicant(consumed.RecruitId), Is.Null);
            Assert.That(board.Applicants.Single(value => value.AuthoredStableRecruitId == queued.StableId).Slot,
                Is.EqualTo(consumed.Slot));
            foreach (var offer in originals.Where(value => value.RecruitId != consumed.RecruitId))
                Assert.That(CanonicalJson.Serialize(board.FindApplicant(offer.RecruitId)), Is.EqualTo(CanonicalJson.Serialize(offer)));
            Assert.That(_service.PendingExpeditionRecruitLeads089(campaign), Is.Empty);
        }

        [Test]
        public void OwnedDuplicateOfferIsUnconsumedUntilItsExistingSigningAuthorityRuns124()
        {
            var heroes = Normal124().Take(11).ToArray();
            var hero = heroes[0];
            var campaign = Require124(_service.CommitBoard(WithLeads124(Create124(), hero.StableId), _content, _cityContent));
            var offer = campaign.Guild.GuildCity.RecruitmentBoard.Applicants.Single();
            campaign = Require124(_service.SignApplicant(campaign, offer.RecruitId));
            var count = campaign.Guild.Recruits.Count;
            campaign = WithLeads124(campaign, heroes.Select(value => value.StableId).ToArray());
            campaign = Require124(_service.CommitBoard(campaign, _content, _cityContent));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.Applicants, Has.Count.EqualTo(10));
            Assert.That(_service.DescribeHeroMasterDuplicate089(campaign, offer).IsDuplicateOffer, Is.True);
            Assert.That(_service.PendingExpeditionRecruitLeads089(campaign), Has.Count.EqualTo(1));
            Assert.That(_service.CanInviteEarnedContacts124(campaign), Is.False);
            Assert.That(Require124(_service.RefreshBoard(campaign, _content, _cityContent)), Is.SameAs(campaign),
                "An owned hero's earned duplicate offer must not be evicted to make room.");
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.RecruitmentBoard.FindApplicant(offer.RecruitId)),
                Is.EqualTo(CanonicalJson.Serialize(offer)));
            var beforeXp = campaign.Guild.TreasuryXp;
            var cost = _service.DescribeApplicantSigning090(campaign, offer).EffectiveCostTreasuryXp;
            campaign = Require124(_service.SignApplicant(campaign, offer.RecruitId));
            Assert.That(campaign.Guild.Recruits, Has.Count.EqualTo(count));
            Assert.That(campaign.Guild.Recruits.Single(value => value.RecruitId == offer.RecruitId).Progression.AscensionLevel, Is.EqualTo(1));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(beforeXp - cost));
            Assert.That(World124(campaign).ExpeditionRecruitLeadIds089, Does.Not.Contain(hero.StableId));
            campaign = Require124(_service.CommitBoard(campaign, _content, _cityContent));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.FindApplicant(offer.RecruitId), Is.Null);
            Assert.That(_service.PendingExpeditionRecruitLeads089(campaign), Is.Empty);
        }

        [Test]
        public void ExplicitRefreshChargesExistingFeeOnlyWhenAnExactLeadActuallyFits124()
        {
            var campaign = WithBoard124(Create124(), FrozenLegacyBoard124());
            var hero = Normal124().First();
            campaign = WithLeads124(campaign, hero.StableId);
            var original = campaign;
            var expectedCost = GuildCityRecruitmentService017D.NextBoardRefreshCostTreasuryXp067(campaign.Guild.GuildCity.RecruitmentRefreshOrdinal);
            campaign = Require124(_service.RefreshBoard(campaign, _content, _cityContent));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(original.Guild.TreasuryXp - expectedCost));
            Assert.That(campaign.Guild.GuildCity.RecruitmentRefreshOrdinal, Is.EqualTo(original.Guild.GuildCity.RecruitmentRefreshOrdinal + 1));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.RefreshOrdinal, Is.EqualTo(campaign.Guild.GuildCity.RecruitmentRefreshOrdinal));
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal, Is.EqualTo(original.Guild.GuildCity.OperationOrdinal));
            Assert.That(campaign.Guild.GuildCity.RecruitmentDryStreak, Is.EqualTo(original.Guild.GuildCity.RecruitmentDryStreak));
            foreach (var offer in original.Guild.GuildCity.RecruitmentBoard.Applicants)
                Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.RecruitmentBoard.FindApplicant(offer.RecruitId)), Is.EqualTo(CanonicalJson.Serialize(offer)));
            Assert.That(Require124(_service.RefreshBoard(campaign, _content, _cityContent)), Is.SameAs(campaign));
            var poor = WithLeads124(Create124(0), hero.StableId);
            var poorJson = CanonicalJson.Serialize(poor);
            Assert.That(_service.RefreshBoard(poor, _content, _cityContent).IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(poor), Is.EqualTo(poorJson));
            Assert.That(Require124(_service.CommitBoard(poor, _content, _cityContent)).Guild.TreasuryXp, Is.Zero,
                "The existing free Add Earned Contact action remains available.");
        }

        [Test]
        public void OriginalSixtyOneSaveRetainsAllPendingOffersAndNoOpCommandsPreserveFiles124()
        {
            var source = Environment.GetEnvironmentVariable("SECOND_DIMENSION_RESET_PROFILE");
            if (string.IsNullOrWhiteSpace(source)) Assert.Ignore("Set SECOND_DIMENSION_RESET_PROFILE to the preserved original 61-owned baseline.");
            var originalBytes = File.ReadAllBytes(source);
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(originalBytes)).Replace("-", ""),
                    Is.EqualTo("1668BC89991E6A41D2BCEFD66084753715E7C5311B22A4BA534E680AF86F3A75"));
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionEarnedBoard124_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "affected.json");
            File.Copy(source, path);
            try
            {
                var coordinator = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), path);
                var state = (CampaignState)typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coordinator);
                Assert.That(state.Guild.Recruits, Has.Count.EqualTo(61));
                var board = state.Guild.GuildCity.RecruitmentBoard;
                Assert.That(board.Applicants, Has.Count.EqualTo(6));
                Assert.That(board.Applicants.All(value => value.RecruitId.StartsWith("PROC_", StringComparison.Ordinal)), Is.True);
                Assert.That(board.Applicants.All(value => !state.Guild.Recruits.Any(owned => owned.RecruitId == value.RecruitId)), Is.True);
                Assert.That(_service.PendingExpeditionRecruitLeads089(state), Is.Empty);
                var primary = File.ReadAllBytes(path);
                var backup = File.Exists(path + ".bak") ? File.ReadAllBytes(path + ".bak") : null;
                var canonical = CanonicalJson.Sha256Hex(state);
                var notices = 0;
                coordinator.Changed += () => notices++;
                for (var repeat = 0; repeat < 3; repeat++)
                {
                    var commit = coordinator.CommitGuildCityApplicantBoard017D();
                    Assert.That(commit.Succeeded, Is.True, commit.Message);
                    var refresh = coordinator.RefreshGuildCityApplicantBoard017D();
                    Assert.That(refresh.Succeeded, Is.True, refresh.Message);
                }
                Assert.That(notices, Is.Zero);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(primary));
                Assert.That(File.Exists(path + ".bak"), Is.EqualTo(backup != null));
                if (backup != null) Assert.That(File.ReadAllBytes(path + ".bak"), Is.EqualTo(backup));
                Assert.That(File.Exists(path + ".tmp"), Is.False);
                var reloaded = Require124(new AtomicSaveStore().ReadWithRecovery(path)).CampaignState;
                Assert.That(CanonicalJson.Sha256Hex(reloaded), Is.EqualTo(canonical));
                Assert.That(CanonicalJson.Serialize(reloaded.Guild.GuildCity.RecruitmentBoard), Is.EqualTo(CanonicalJson.Serialize(board)));
            }
            finally
            {
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(originalBytes));
                Directory.Delete(directory, true);
            }
        }

        HeroMaster300Hero087[] Normal124() => _heroes.AcceptedHeroes.Where(value => value.IsNormalApplicantEligible)
            .OrderBy(value => value.StableId, StringComparer.Ordinal).ToArray();

        ApplicantBoardState FrozenLegacyBoard124() => ApplicantBoardStateAdapter.ToM1State(
            new TutorialApplicantFactory(_content).CreateFrozenBoard(), "LEGACY_COMMITTED_OFFERS_124");

        static CampaignState Create124(long treasury = 500000, int recruitCount = 7)
        {
            var recruits = Enumerable.Range(1, recruitCount).Select(value => new RecruitState("LEGACY_TEST_124_" + value, 100, 100, 20, 20)).ToArray();
            var union = new UnionState("U124", "Existing Union", UnionKind.Normal, recruits[0].RecruitId,
                recruits.Take(3).Select(value => value.RecruitId).ToArray(), "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000);
            return new CampaignState("00000000-0000-0000-0000-000000000124", 124L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), new GuildState("GUILD_124", treasury, recruits, new[] { union }),
                new NewGuildProfileState("Earned board fixture", GameMode.Standard, TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false, 439, 0, true, true, true, true, "complete"));
        }

        static CampaignState WithBoard124(CampaignState campaign, ApplicantBoardState board) => campaign.With(
            campaign.Guild.WithGuildCity(campaign.Guild.GuildCity.With(recruitmentBoard: board, replaceRecruitmentBoard: true)), campaign.OpeningFlow);

        static CampaignState WithLeads124(CampaignState campaign, params string[] ids)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var runtime = playable.WorldGate023.With(expeditionRecruitLeadIds089:
                playable.WorldGate023.ExpeditionRecruitLeadIds089.Concat(ids).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            return campaign.With(campaign.Guild.WithGuildCity(city.With(strategic017H: strategic.With(
                campaign019: progress.With(playable020: playable.With(worldGate023: runtime, replaceWorldGate023: true), replacePlayable020: true),
                replaceCampaign019: true), replaceStrategic017H: true)), campaign.OpeningFlow);
        }

        static WorldGateRuntimeState023 World124(CampaignState campaign) => campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023;
        static T Require124<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
