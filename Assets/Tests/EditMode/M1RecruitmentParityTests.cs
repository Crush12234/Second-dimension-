using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M1RecruitmentParityTests
    {
        private static readonly string[] DefaultRaces =
        {
            "HUMAN", "ORC", "GOBLIN", "DOG_TRIBE", "DARK_ELF", "DEMON_HERITAGE"
        };

        private static readonly string[] DefaultEligibilityFlags =
        {
            "GUILD_CHARTER_SIGNED", "FREIGHT_YARD_OPEN", "INFIRMARY_CONTACT",
            "BRASSWORK_TRUST_1", "IRON_WALL_TRUST_1", "STORMROAD_ACCESS",
            "STORMROAD_TRUST_2", "ASHGLASS_CHARTER_RECOGNIZED",
            "DUSK_ARCHIVE_TRUST_2", "FOUND_FALSE_OBJECTIVE"
        };

        private RecruitmentContent _content;

        [SetUp]
        public void SetUp()
        {
            _content = RecruitmentContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
        }

        [Test]
        public void ProceduralRecruitMatchesFrozenGoldenVectorExactly()
        {
            var recruit = GoldenRecruit();

            Assert.That(recruit.RecruitId, Is.EqualTo("PROC_EF80D0CC2FBF14E6"));
            Assert.That(recruit.DisplayName, Is.EqualTo("Morga Wolfscar"));
            Assert.That(recruit.StartingClassId, Is.EqualTo("CLASS_MAGE"));
            Assert.That(recruit.GenerationSeed, Is.EqualTo("70943B3B85283DD6:67BD750FBD631149"));
            Assert.That(recruit.VisualSeed, Is.EqualTo("E1D6F4B16ACE116E19D6A488"));
            Assert.That(recruit.SigningCostXp, Is.EqualTo(72));
            Assert.That(recruit.DevelopmentPotentialScore, Is.EqualTo(624));
            Assert.That(
                CanonicalJson.Sha256Hex(recruit),
                Is.EqualTo("a5ec2ff88286a371e3d9113001edbfdc6dc44b86532f8c0070dc219485ee1561"));
        }

        [Test]
        public void ScoutingReportMatchesFrozenGoldenAndWithholdsPrivateFields()
        {
            var report = new OpeningRecruitGenerator(_content).CreateScoutingReport(
                GoldenRecruit(), officeTier: 2, scoutingAccuracyBase: 48, scoutSkill: 45);
            var json = CanonicalJson.Serialize(report);

            Assert.That(
                CanonicalJson.Sha256Hex(report),
                Is.EqualTo("2e107bcad3d08fabde6aab4d800182174e964f1f47558d474be2895863b2eeac"));
            Assert.That(report.ExactPotentialDisplayed, Is.False);
            Assert.That(report.HiddenTraitRevealed, Is.False);
            Assert.That(report.HiddenDataWithheld, Is.True);
            Assert.That(json, Does.Not.Contain("hiddenTraitId"));
            Assert.That(json, Does.Not.Contain("developmentPotentialScore"));
        }

        [Test]
        public void ApplicantBoardMatchesFrozenGoldenVectorExactly()
        {
            var board = new DetailedApplicantBoardGenerator(_content).Generate(new ApplicantBoardRequest
            {
                CampaignSeed = 20260811L,
                GuildDay = 2,
                RefreshIndex = 0,
                OfficeTier = 2,
                DryStreak = 4,
                UnlockedWorlds = new[] { "WORLD_GATE_01" },
                UnlockedRaces = DefaultRaces,
                EligibilityFlags = DefaultEligibilityFlags,
                GuildRank = 2,
                RecruitedSignatureIds = Array.Empty<string>(),
                SeenSignatureIds = Array.Empty<string>(),
                DeclinedSignatureUntilDay = new Dictionary<string, int>(StringComparer.Ordinal),
                EventBonusBasisPoints = 0,
                ScoutSkill = 45,
                ForcedApplicants = new[]
                {
                    new ForcedApplicantSpec
                    {
                        SlotIndex = 1,
                        SourceType = "SIGNATURE",
                        SignatureId = "SIG_W01_01",
                        SourceChannel = "REFERRAL"
                    }
                }
            });

            Assert.That(board.BoardId, Is.EqualTo("BOARD_3D298B124B5C6607_D0002_R00"));
            Assert.That(board.StateHash, Is.EqualTo("3D298B124B5C6607740B"));
            Assert.That(board.Committed, Is.True);
            Assert.That(
                CanonicalJson.Sha256Hex(board),
                Is.EqualTo("9c3b5ba68f0b1de4d5609907da18c4421e257abcd2e68fb4621c1c1a3693ed07"));
        }

        [Test]
        public void TutorialAuthorityMaterializesCommittedSixSlotBoardExactly()
        {
            var first = new TutorialApplicantFactory(_content).CreateFrozenBoard();
            var second = new TutorialApplicantFactory(_content).CreateFrozenBoard();

            Assert.That(first.BoardId, Is.EqualTo("BOARD_TUTORIAL_V1_001"));
            Assert.That(first.Applicants.Count, Is.EqualTo(6));
            Assert.That(first.Committed, Is.True);
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));

            var expectedIds = new[]
            {
                "PROC_36344E2400DC98B6", "PROC_F85A4CAA747BC8C6",
                "PROC_5B14E7816E55FFB5", "PROC_748DD03A23E1FEB0",
                "SIGI_4559425B6CF6CBE6", "SIGI_CE2768FD5A985F8B"
            };
            var expectedCosts = new[] { 73, 49, 69, 58, 101, 89 };
            var proceduralCount = 0;
            var totalCost = 0;
            for (var index = 0; index < first.Applicants.Count; index++)
            {
                var applicant = first.Applicants[index];
                Assert.That(applicant.SlotIndex, Is.EqualTo(index));
                Assert.That(applicant.RecruitId, Is.EqualTo(expectedIds[index]));
                Assert.That(applicant.SigningCostXp, Is.EqualTo(expectedCosts[index]));
                totalCost += applicant.SigningCostXp;
                if (applicant.SourceType == "PROCEDURAL") proceduralCount++;
            }
            Assert.That(proceduralCount, Is.EqualTo(4));
            Assert.That(totalCost, Is.EqualTo(439));
        }

        [Test]
        public void TutorialAliasesPreserveLegacyCanonicalAndStableIds()
        {
            var maren = TutorialApplicantFactory.ResolveSignatureAlias("SIG_MAREN_HOLT");
            var odelia = TutorialApplicantFactory.ResolveSignatureAlias("SIG_ODELIA_FEN");

            Assert.That(maren.SignatureId, Is.EqualTo("SIG_W01_01"));
            Assert.That(maren.StableRecruitId, Is.EqualTo("SIGREC_MAREN_HOLT"));
            Assert.That(odelia.SignatureId, Is.EqualTo("SIG_W01_03"));
            Assert.That(odelia.StableRecruitId, Is.EqualTo("SIGREC_ODELIA_FEN"));
        }

        [Test]
        public void KnownProceduralCanExceedEveryOpeningSignatureCeiling()
        {
            var recruit = new OpeningRecruitGenerator(_content).Generate(new ProceduralRecruitRequest
            {
                CampaignSeed = 20260811L,
                GuildDay = 1,
                RefreshIndex = 0,
                SlotIndex = 1,
                SourceChannel = "GUILD_BOARD",
                UnlockedRaces = DefaultRaces,
                ExtraSalt = "EXAMPLE_01"
            });

            Assert.That(recruit.DisplayName, Is.EqualTo("Holl Brighttrail"));
            Assert.That(recruit.DevelopmentPotentialScore, Is.EqualTo(795));
            Assert.That(recruit.DevelopmentPotentialScore, Is.GreaterThan(768));
        }

        [Test]
        public void SignatureMaterializationIsDeterministicAndUsesOnlyFrozenBoundedVariance()
        {
            var materializer = new SignatureRecruitMaterializer(_content);
            var first = materializer.Materialize(20260811L, "SIG_W01_01", "GUILD_BOARD");
            var second = materializer.Materialize(20260811L, "SIG_W01_01", "GUILD_BOARD");

            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(first.RecruitId, Is.EqualTo("SIGI_6EDD1E84682CF7A9"));
            Assert.That(first.SourceType, Is.EqualTo("SIGNATURE"));
            Assert.That(first.SignatureId, Is.EqualTo("SIG_W01_01"));
            Assert.That(first.DevelopmentPotentialScore, Is.EqualTo(638));
            Assert.That(first.DevelopmentPotentialScore, Is.InRange(632, 648));
            Assert.That(first.VariantFlags, Does.Contain("GROWTH_VARIANCE_-2"));
        }

        private OpeningRecruitRecord GoldenRecruit()
        {
            return new OpeningRecruitGenerator(_content).Generate(new ProceduralRecruitRequest
            {
                CampaignSeed = 20260811L,
                GuildDay = 1,
                RefreshIndex = 0,
                SlotIndex = 0,
                SourceChannel = "GUILD_BOARD",
                UnlockedRaces = DefaultRaces,
                ExtraSalt = string.Empty
            });
        }
    }
}
