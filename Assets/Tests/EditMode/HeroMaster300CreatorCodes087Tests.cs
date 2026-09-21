using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Creator028;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroMaster300CreatorCodes087Tests
    {
        private HeroMaster300CreatorRegistry087 _registry;
        private HeroMaster300Hero087 _vaelis;

        [SetUp]
        public void SetUp()
        {
            _registry = HeroMaster300CreatorRegistry087.Load();
            Assert.That(_registry.Codes.TryResolveInput(
                "SD-SS-02-VAELIS_NOCT", out _, out _vaelis), Is.True);
        }

        [Test]
        public void ResourceUsesStrictBoundaryAndAuthoredVeyraRaceCanResolve087()
        {
            Assert.That(
                _registry.Source.AcceptedHeroes.Count + _registry.Source.QuarantinedHeroes.Count,
                Is.EqualTo(300));
            Assert.That(_registry.Codes.CodeCount, Is.EqualTo(
                _registry.Source.AcceptedHeroes.Count(hero => hero.Rank == HeroMasterRank087.SS)));
            Assert.That(_registry.Codes.CodeCount, Is.GreaterThan(0).And.LessThan(30),
                "Malformed supplied SS records must reduce the redeemable count.");
            Assert.That(_registry.Codes.TryResolveInput(
                "SD-SS-01-VEYRA_ASHGLASS", out _, out var veyra), Is.True);
            Assert.That(veyra.StableId, Is.EqualTo("SIGREC_VEYRA_ASHGLASS"));
            Assert.That(veyra.Race, Is.EqualTo("DEMON_HERITAGE"));
        }

        [Test]
        public void OnlyDedicatedValidatedSsCodeProjectsIntoCreatorCatalog087()
        {
            Assert.That(_registry.Codes.TryResolveInput(
                "  sd ss 02 vaelis noct  ", out var code, out var hero), Is.True);
            Assert.That(hero, Is.SameAs(_vaelis));
            Assert.That(code.Category, Is.EqualTo("CHARACTER"));
            Assert.That(code.RewardId, Is.EqualTo(hero.StableId));
            Assert.That(code.ExactOncePerCampaign, Is.True);
            Assert.That(code.Active, Is.True);
            Assert.That(_registry.Codes.TryResolveInput(
                hero.GenerationCode, out _, out _), Is.False,
                "The package's all-rank generation_code is never a Creator SS claim code.");
            Assert.That(_registry.Source.NormalApplicantCandidates.All(candidate =>
                candidate.Rank != HeroMasterRank087.SS), Is.True);
        }

        [Test]
        public void SsProjectionIsNormalPlayableEmptyEquippedAndUsesExistingArtInitializer087()
        {
            var grant = HeroMaster300CreatorRecruitProjection087.FromHero(_vaelis);
            Assert.That(grant.InventoryItems, Is.Empty);
            Assert.That(grant.Recruit.AuthorityKind, Is.EqualTo(RecruitAuthorityKind.Normal));
            Assert.That(grant.Recruit.AuthoredStableRecruitId, Is.EqualTo(_vaelis.StableId));
            Assert.That(grant.Recruit.CurrentHp, Is.EqualTo(_vaelis.Hp));
            Assert.That(grant.Recruit.CurrentMp, Is.EqualTo(_vaelis.Ap));
            Assert.That(grant.Recruit.Equipment.Assignments, Is.Empty);

            var applicant = JObject.Parse(grant.Recruit.CanonicalApplicantJson);
            Assert.That(applicant["sourceType"]?.Value<string>(), Is.EqualTo("PROCEDURAL"));
            Assert.That(applicant["equipmentLoadout"]?["autoEquipAllowed"]?.Value<bool>(), Is.False);

            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var initialized = new RecruitAutoGenerationSigningService010(
                new M1CommandService(),
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)))
                .InitializeRecruit(grant.Recruit);
            Assert.That(initialized.Progression.LearnedArtIds, Is.Not.Empty,
                "SS heroes must initialize through the existing legal Art system.");
            Assert.That(initialized.Equipment.Assignments, Is.Empty);
        }

        [Test]
        public void EveryValidatedSsHeroCanInitializeThroughExistingRecruitAndArtSystems087()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var initializer = new RecruitAutoGenerationSigningService010(
                new M1CommandService(),
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)));
            var acceptedSs = _registry.Source.AcceptedHeroes
                .Where(hero => hero.Rank == HeroMasterRank087.SS)
                .ToArray();

            Assert.That(acceptedSs.Length, Is.EqualTo(25),
                "Five incomplete supplied SS records remain quarantined; Veyra is supported by the exact authored race records.");
            foreach (var hero in acceptedSs)
            {
                Assert.That(_registry.Codes.TryResolveInput(
                    hero.SsGenerationCode, out _, out var resolved), Is.True, hero.StableId);
                Assert.That(resolved, Is.SameAs(hero));
                var grant = HeroMaster300CreatorRecruitProjection087.FromHero(hero);
                var initialized = initializer.InitializeRecruit(grant.Recruit);
                Assert.That(initialized.Progression.LearnedArtIds, Is.Not.Empty, hero.StableId);
                Assert.That(initialized.Equipment.Assignments, Is.Empty, hero.StableId);
            }
        }

        [Test]
        public void ExistingCreatorServiceCommitsSsRecruitAndBothLedgersExactlyOnce087()
        {
            const string input = "SD-SS-02-VAELIS_NOCT";
            var campaign = CampaignFactory.CreateM0Proof(8702);
            var startingRecruitCount = campaign.Guild.Recruits.Count;
            var startingInventoryCount = campaign.Guild.Inventory.Count;
            var grant = HeroMaster300CreatorRecruitProjection087.FromHero(_vaelis);
            var service = new CreatorAccessCommandService028();

            var first = service.RedeemCode(campaign, _registry.Codes, input, grant);
            Assert.That(first.IsSuccess, Is.True, string.Join("\n", first.Errors));
            var access = first.Value.Guild.GuildCity.Strategic017H.Campaign019.Playable020.CreatorAccess028;
            Assert.That(first.Value.Guild.Recruits.Count, Is.EqualTo(startingRecruitCount + 1));
            Assert.That(first.Value.Guild.Inventory.Count, Is.EqualTo(startingInventoryCount));
            Assert.That(access.RedeemedCodeIds, Contains.Item(
                HeroMaster300CreatorCodeCatalog087.CodeIdFor(_vaelis)));
            Assert.That(access.ClaimedInvitationIds, Contains.Item(_vaelis.StableId));
            Assert.That(access.AppliedReceiptIds.Count, Is.EqualTo(1));
            Assert.That(first.Value.Guild.GuildCity.MemberAssignments.Any(assignment =>
                StringComparer.Ordinal.Equals(assignment.RecruitId, grant.Recruit.RecruitId) &&
                assignment.Kind == SecondDimension.Gameplay.GuildCity017D.GuildMemberAssignmentKind017D.Reserve), Is.True);

            var beforeReplay = CanonicalJson.Serialize(first.Value);
            var replay = service.RedeemCode(first.Value, _registry.Codes, input, grant);
            Assert.That(replay.IsSuccess, Is.True, string.Join("\n", replay.Errors));
            Assert.That(CanonicalJson.Serialize(replay.Value), Is.EqualTo(beforeReplay));
        }

        [Test]
        public void EveryValidatedSsCodeClaimsItsOwnHeroOnlyOnce087()
        {
            var service = new CreatorAccessCommandService028();
            var acceptedSs = _registry.Source.AcceptedHeroes
                .Where(hero => hero.Rank == HeroMasterRank087.SS)
                .OrderBy(hero => hero.RosterId)
                .ToArray();

            Assert.That(acceptedSs, Has.Length.EqualTo(25));
            foreach (var hero in acceptedSs)
            {
                var campaign = CampaignFactory.CreateM0Proof(88000 + hero.RosterId);
                var startingRecruitCount = campaign.Guild.Recruits.Count;
                var grant = HeroMaster300CreatorRecruitProjection087.FromHero(hero);

                var first = service.RedeemCode(
                    campaign, _registry.Codes, hero.SsGenerationCode, grant);
                Assert.That(first.IsSuccess, Is.True,
                    hero.StableId + "\n" + string.Join("\n", first.Errors));
                Assert.That(first.Value.Guild.Recruits.Count,
                    Is.EqualTo(startingRecruitCount + 1), hero.StableId);
                Assert.That(first.Value.Guild.Recruits.Count(recruit =>
                        StringComparer.Ordinal.Equals(
                            recruit.AuthoredStableRecruitId, hero.StableId)),
                    Is.EqualTo(1), hero.StableId);

                var access = first.Value.Guild.GuildCity.Strategic017H
                    .Campaign019.Playable020.CreatorAccess028;
                Assert.That(access.RedeemedCodeIds.Count(id =>
                        StringComparer.Ordinal.Equals(id,
                            HeroMaster300CreatorCodeCatalog087.CodeIdFor(hero))),
                    Is.EqualTo(1), hero.StableId);
                Assert.That(access.ClaimedInvitationIds.Count(id =>
                        StringComparer.Ordinal.Equals(id, hero.StableId)),
                    Is.EqualTo(1), hero.StableId);

                var beforeReplay = CanonicalJson.Serialize(first.Value);
                var replay = service.RedeemCode(
                    first.Value, _registry.Codes, hero.SsGenerationCode, grant);
                Assert.That(replay.IsSuccess, Is.True,
                    hero.StableId + "\n" + string.Join("\n", replay.Errors));
                Assert.That(CanonicalJson.Serialize(replay.Value),
                    Is.EqualTo(beforeReplay), hero.StableId);
            }
        }

        [Test]
        public void InvalidSsCodeFailsWithoutMutatingCampaign087()
        {
            var campaign = CampaignFactory.CreateM0Proof(8703);
            var before = CanonicalJson.Serialize(campaign);
            var service = new CreatorAccessCommandService028();

            var rejected = service.RedeemCode(
                campaign, _registry.Codes, "SD-SS-NOT-A-REAL-HERO-CODE");

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain("CREATOR028_CODE_INVALID"));
            Assert.That(CanonicalJson.Serialize(campaign), Is.EqualTo(before));
        }

        [Test]
        public void SsClaimAndExactOnceLedgersSurviveSaveReload087()
        {
            const string input = "SD-SS-02-VAELIS_NOCT";
            var service = new CreatorAccessCommandService028();
            var grant = HeroMaster300CreatorRecruitProjection087.FromHero(_vaelis);
            var first = service.RedeemCode(
                CampaignFactory.CreateM0Proof(8704), _registry.Codes, input, grant);
            Assert.That(first.IsSuccess, Is.True, string.Join("\n", first.Errors));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(first.Value));
            Assert.That(reloaded, Is.Not.Null);
            var access = reloaded.Guild.GuildCity.Strategic017H
                .Campaign019.Playable020.CreatorAccess028;
            Assert.That(access.RedeemedCodeIds, Contains.Item(
                HeroMaster300CreatorCodeCatalog087.CodeIdFor(_vaelis)));
            Assert.That(access.ClaimedInvitationIds,
                Contains.Item(_vaelis.StableId));
            Assert.That(access.AppliedReceiptIds, Has.Count.EqualTo(1));
            Assert.That(reloaded.Guild.Recruits.Count(recruit =>
                    StringComparer.Ordinal.Equals(
                        recruit.AuthoredStableRecruitId, _vaelis.StableId)),
                Is.EqualTo(1));
            Assert.That(reloaded.Guild.GuildCity.MemberAssignments.Count(assignment =>
                    StringComparer.Ordinal.Equals(
                        assignment.RecruitId, grant.Recruit.RecruitId) &&
                    assignment.Kind == SecondDimension.Gameplay.GuildCity017D
                        .GuildMemberAssignmentKind017D.Reserve),
                Is.EqualTo(1));

            var beforeReplay = CanonicalJson.Serialize(reloaded);
            var replay = service.RedeemCode(
                reloaded, _registry.Codes, input, grant);
            Assert.That(replay.IsSuccess, Is.True, string.Join("\n", replay.Errors));
            Assert.That(CanonicalJson.Serialize(replay.Value),
                Is.EqualTo(beforeReplay));
        }

        [Test]
        public void DuplicateAuthoredIdentityIsDetectedBeforeAnySecondGrant087()
        {
            var grant = HeroMaster300CreatorRecruitProjection087.FromHero(_vaelis);
            Assert.That(HeroMaster300CreatorRecruitProjection087.RosterContainsHero(
                new[] { grant.Recruit }, _vaelis), Is.True);
            Assert.That(HeroMaster300CreatorRecruitProjection087.RosterContainsHero(
                Array.Empty<RecruitState>(), _vaelis), Is.False);
        }
    }
}
