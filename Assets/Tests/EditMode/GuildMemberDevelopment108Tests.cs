using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildMemberDevelopment108Tests
    {
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        GuildCityRecruitmentService017D _service;

        [OneTimeSetUp]
        public void LoadExistingAuthority()
        {
            _service = new GuildCityRecruitmentService017D(new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(ContentRoot)));
        }

        [Test]
        public void EverySssAuthorityEnvelopeProjectsItsOwnClassWeaponAndArtsWithoutRegeneration108()
        {
            foreach (var hero in SssTenV4Roster090.All)
            {
                var recruit = SssTenV4Roster090.MaterializeGrant(hero.HeroId).Recruit;
                var before = CanonicalJson.Serialize(recruit);
                Assert.That(JObject.Parse(recruit.CanonicalApplicantJson)["startingClassId"], Is.Null,
                    "This is the real SSS envelope that used to fail in the ordinary generator.");
                var path = _service.DescribeRecruit067(recruit);
                Assert.That(path.RecruitId, Is.EqualTo(recruit.RecruitId));
                Assert.That(path.StartingClassId, Is.EqualTo(hero.ClassId), hero.HeroId);
                Assert.That(path.FixedWeaponFamilyId, Is.EqualTo(hero.WeaponFamilyId), hero.HeroId);
                Assert.That(path.StartingStableNodeIds, Is.EqualTo(recruit.Progression.LearnedArtIds));
                Assert.That(path.StartingAdvantage, Does.Contain(hero.Role));
                Assert.That(new[] { path.WeaponTreeId, path.PrimaryRoleTreeId,
                    path.MysticTreeId, path.SecondaryRoleTreeId }, Is.All.Empty,
                    "SSS must not acquire unrelated procedural tree assignments from a display query.");
                Assert.That(CanonicalJson.Serialize(recruit), Is.EqualTo(before));
            }
        }

        [Test]
        public void UnknownIdentityCannotUseAnSssEnvelopeToBypassOrdinaryClassValidation108()
        {
            var row = JObject.FromObject(SssTenV4Roster090.MaterializeGrant(
                "SSS_ASTERION_SUNWARD").Recruit);
            row["RecruitId"] = "UNKNOWN_RECRUIT_108";
            row["AuthoredStableRecruitId"] = "UNKNOWN_AUTHORITY_108";
            row["SignatureId"] = "UNKNOWN_SIGNATURE_108";
            row["TutorialAliasId"] = string.Empty;
            Assert.Throws<KeyNotFoundException>(() => _service.DescribeRecruit067(row.ToObject<RecruitState>()));
        }

        [Test]
        public void AllOwnedAffectedProfileMembersReachProductionDevelopmentProjectionReadOnly108()
        {
            // The owner profile stays outside source/distribution. Root supplies
            // its preserved, isolated snapshot only for this integration gate.
            var source = Environment.GetEnvironmentVariable("SECOND_DIMENSION_RESET_PROFILE");
            if (string.IsNullOrWhiteSpace(source))
                Assert.Ignore("Set SECOND_DIMENSION_RESET_PROFILE to the preserved affected profile copy.");
            Assert.That(File.Exists(source), Is.True, source);
            var bytes = File.ReadAllBytes(source);
            var envelope = JsonConvert.DeserializeObject<SaveEnvelopeV1>(File.ReadAllText(source));
            var campaign = envelope.CampaignState;
            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(61),
                "This gate uses the preserved September 12 affected roster, not a smaller fixture.");
            var before = CanonicalJson.Sha256Hex(campaign);
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimension_Development108_" + Guid.NewGuid().ToString("N"));
            var emptySavePath = Path.Combine(directory, "unused.json");
            var coordinator = new M1RuntimeCoordinator(ContentRoot, emptySavePath);
            var field = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(coordinator, campaign);
            var views = coordinator.BuildGuildMemberDevelopment067();
            Assert.That(views.Count, Is.EqualTo(61));
            CollectionAssert.AreEquivalent(campaign.Guild.Recruits.Select(value => value.RecruitId),
                views.Select(value => value.RecruitId));
            foreach (var recruit in campaign.Guild.Recruits)
            {
                var view = views.Single(value => value.RecruitId == recruit.RecruitId);
                Assert.That(view.DisplayName, Is.EqualTo(recruit.DisplayName));
                Assert.That(view.StartingClassId, Is.Not.Null.And.Not.Empty, recruit.RecruitId);
                Assert.That(view.FixedWeaponFamilyId, Is.Not.Null.And.Not.Empty, recruit.RecruitId);
                Assert.That(view.Level, Is.EqualTo(recruit.Progression.Level), recruit.RecruitId);
                Assert.That(view.LearnedArtCount, Is.EqualTo(recruit.Progression.LearnedArtIds.Count), recruit.RecruitId);
            }
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(bytes));
            Assert.That(Directory.Exists(directory), Is.False, "Read-only projection must not write a save.");
        }
    }
}
