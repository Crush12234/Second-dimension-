using System;
using NUnit.Framework;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation.GuildCity017D;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ChapterTwoCrewMechanics079Tests
    {
        [Test]
        public void DeterministicPreviewNamesGuaranteedOutcomeWithoutRollPercentages079()
        {
            var preview = ChapterTwoCrewMechanics079.BuildCheckPreview079(
                Candidate079("RANGER", "Ranger", "CLASS_RANGER"),
                Candidate079("GUARDIAN", "Guardian", "CLASS_GUARDIAN"),
                new[] { "Survival", "Engineering" },
                0,
                "STANDARD",
                false,
                new ChapterTwoEvidenceStatus079
                {
                    ReliableEvidenceCount = 2,
                    RelationshipStrength = 3,
                    TrustScore = 5
                });

            Assert.That(preview.UsesCommitted2d6, Is.False);
            Assert.That(preview.GuaranteedOutcome, Is.EqualTo("EXCEPTIONAL"));
            Assert.That(preview.CompactReadout, Does.Contain("PROMISED CONSEQUENCE"));
            Assert.That(preview.CompactReadout, Does.Not.Contain("AUTHORED").IgnoreCase);
            Assert.That(preview.CompactReadout, Does.Not.Contain("%"));
            Assert.That(preview.EvidenceContribution, Is.EqualTo(2));
            Assert.That(preview.TrustContribution, Is.EqualTo(1));
        }

        [Test]
        public void MatchingLeadAndAssistantRaiseTruthfulCommittedOdds079()
        {
            var skills = new[] { "Survival", "Engineering" };
            var ranger = Candidate079("RANGER", "Ranger", "Ranger");
            var guardian = Candidate079("GUARDIAN", "Guardian", "Guardian");
            var matched = ChapterTwoCrewMechanics079.BuildCheckPreview079(
                ranger,
                guardian,
                skills,
                0,
                "Standard");
            var unmatched = ChapterTwoCrewMechanics079.BuildCheckPreview079(
                Candidate079("PRIEST_1", "Priest One", "Priest"),
                Candidate079("PRIEST_2", "Priest Two", "Priest"),
                skills,
                0,
                "Standard");

            Assert.That(matched.ApproachModifier, Is.EqualTo(2));
            Assert.That(matched.LeadRoleBonus, Is.EqualTo(1));
            Assert.That(matched.AssistantSynergyBonus, Is.EqualTo(1));
            Assert.That(matched.CommandModifier, Is.EqualTo(4),
                "Only this command modifier is submitted; mode authority is applied downstream.");
            Assert.That(matched.EffectiveModifier, Is.EqualTo(4));
            Assert.That(matched.SuccessChancePercent, Is.EqualTo(97));
            Assert.That(matched.FullSuccessChancePercent, Is.EqualTo(72));
            Assert.That(matched.SuccessChancePercent, Is.GreaterThan(unmatched.SuccessChancePercent));
            Assert.That(matched.CompactReadout, Does.Contain("LEAD FIT +1"));
            Assert.That(matched.CompactReadout, Does.Contain("PARTNER +1"));
            Assert.That(matched.CompactReadout, Does.Contain("SUCCESS 97%"));
        }

        [Test]
        public void ModeModifierChangesPreviewButNotSubmittedCrewCommand079()
        {
            var preview = ChapterTwoCrewMechanics079.BuildCheckPreview079(
                Candidate079("RANGER", "Ranger", "Ranger"),
                Candidate079("GUARDIAN", "Guardian", "Guardian"),
                new[] { "Survival", "Engineering" },
                0,
                "OverpoweredStart");

            Assert.That(preview.CommandModifier, Is.EqualTo(4));
            Assert.That(preview.ModeModifier, Is.EqualTo(2));
            Assert.That(preview.EffectiveModifier, Is.EqualTo(6));
            Assert.That(preview.CompactReadout, Does.Contain("MODE +2"));
        }

        [Test]
        public void BestAssistantCoversTheSelectedLeadsMissingSkill079()
        {
            var lead = Candidate079("LEAD", "Iria", "Ranger");
            var assistant = ChapterTwoCrewMechanics079.BestAssistant079(
                lead,
                new[]
                {
                    lead,
                    Candidate079("PRIEST", "Sella", "Priest"),
                    Candidate079("GUARDIAN", "Orren", "Guardian")
                },
                new[] { "Survival", "Engineering" });

            Assert.That(assistant, Is.Not.Null);
            Assert.That(assistant.RecruitId, Is.EqualTo("GUARDIAN"));
            Assert.That(ChapterTwoCrewMechanics079.RoleFitScore079(
                assistant.ObservedClass,
                new[] { "Engineering" }), Is.GreaterThan(0));
        }

        [Test]
        public void EvidenceReadoutSeparatesReliablePartialAndRelationshipTrust079()
        {
            var state = new GuildCityPresentationState017D
            {
                CivicTrust = 2,
                Relationships = new[]
                {
                    new GuildCityRelationshipView017D { Strength = 2 },
                    new GuildCityRelationshipView017D { Strength = 1 }
                },
                Expedition = new GuildCityExpeditionView017D
                {
                    BoardId = ChapterTwoCrewMechanics079.ChapterTwoBoardId079,
                    ObjectiveFlags = new[]
                    {
                        "EVENT_RESOLVED_EVENT_FOUND_APPRENTICE",
                        GuildCityExpeditionService017D.EventSuccessFlag076(
                            "EVENT_FOUND_APPRENTICE"),
                        "EVENT_RESOLVED_EVENT_WRONG_ROUTE_MARKS",
                        "EVENT_RESOLVED_EVENT_BROKEN_SURVEY_BRIDGE",
                        GuildCityExpeditionService017D.EventSuccessFlag076(
                            "EVENT_BROKEN_SURVEY_BRIDGE")
                    }
                }
            };

            var status = ChapterTwoCrewMechanics079.BuildEvidenceStatus079(state);

            Assert.That(status.IsVisible, Is.True);
            Assert.That(status.ReliableEvidenceCount, Is.EqualTo(2));
            Assert.That(status.PartialEvidenceCount, Is.EqualTo(1));
            Assert.That(status.OrraTrailCount, Is.EqualTo(1));
            Assert.That(status.OrraTrailTotal, Is.EqualTo(6));
            Assert.That(status.OrraRescued, Is.False);
            Assert.That(status.RelationshipStrength, Is.EqualTo(3));
            Assert.That(status.SellaStatus, Is.EqualTo("TRUSTING"));
            Assert.That(status.CrewTrustBand, Is.EqualTo("GROWING"));
            Assert.That(status.CompactReadout, Does.Contain("2 RELIABLE"));
            Assert.That(status.CompactReadout, Does.Contain("1 PARTIAL"));
            Assert.That(status.CompactReadout, Does.Contain("ORRA TRAIL 1/6"));
        }

        [TestCase("PRIMARY_OBJECTIVE_RESCUE_COMPLETE")]
        [TestCase("ENCOUNTER_CLEARED_N13")]
        public void AuthoritativeRescueFlagsReplaceImpossibleBranchFraction080(string rescueFlag)
        {
            var status = ChapterTwoCrewMechanics079.BuildEvidenceStatus079(
                new GuildCityPresentationState017D
                {
                    Expedition = new GuildCityExpeditionView017D
                    {
                        BoardId = ChapterTwoCrewMechanics079.ChapterTwoBoardId079,
                        CurrentNodeId = "N14",
                        ObjectiveFlags = new[] { rescueFlag }
                    }
                });

            Assert.That(status.OrraRescued, Is.True);
            Assert.That(status.CompactReadout, Does.Contain("ORRA TRAIL SECURED"));
            Assert.That(status.CompactReadout, Does.Not.Contain("/6"));
        }

        [Test]
        public void ReturnNodeAloneCannotClaimOrraWasRescued080()
        {
            var status = ChapterTwoCrewMechanics079.BuildEvidenceStatus079(
                new GuildCityPresentationState017D
                {
                    Expedition = new GuildCityExpeditionView017D
                    {
                        BoardId = ChapterTwoCrewMechanics079.ChapterTwoBoardId079,
                        CurrentNodeId = "N14",
                        ObjectiveFlags = Array.Empty<string>()
                    }
                });

            Assert.That(status.OrraRescued, Is.False);
            Assert.That(status.CompactReadout, Does.Contain("ORRA TRAIL 0/6"));
            Assert.That(status.CompactReadout, Does.Not.Contain("SECURED"));
        }

        [Test]
        public void OutcomeCopyPrefersAuthorityAndFallbackKeepsNamedCrewAftermath079()
        {
            Assert.That(
                ChapterTwoCrewMechanics079.SelectOutcomeCopy079(
                    "FULL_SUCCESS",
                    "exceptional authority",
                    "full authority",
                    "cost authority",
                    "setback authority",
                    "severe authority"),
                Is.EqualTo("full authority"));

            var authoritative = new GuildCityExpeditionView017D
            {
                HasCommittedCheckAtCurrentNode = true,
                CurrentEventId = "EVENT_BROKEN_SURVEY_BRIDGE",
                LastCheckOutcome = "FULL_SUCCESS",
                CurrentEventOutcomeText = "Orra's authored fifth-pin consequence."
            };
            Assert.That(
                ChapterTwoCrewMechanics079.OutcomeAftermath079(authoritative),
                Is.EqualTo("Orra's authored fifth-pin consequence."));

            var unaFallback = new GuildCityExpeditionView017D
            {
                HasCommittedCheckAtCurrentNode = true,
                CurrentEventId = "EVENT_INJURED_COURIER",
                LastCheckOutcome = "SETBACK"
            };
            Assert.That(
                ChapterTwoCrewMechanics079.OutcomeAftermath079(unaFallback),
                Does.Contain("Una"));
        }

        private static ChapterTwoCrewCandidate079 Candidate079(
            string recruitId,
            string displayName,
            string observedClass) =>
            new ChapterTwoCrewCandidate079
            {
                RecruitId = recruitId,
                DisplayName = displayName,
                ObservedClass = observedClass
            };
    }
}
