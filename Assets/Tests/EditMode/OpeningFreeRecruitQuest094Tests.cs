using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>
    /// Uses the real opening contract and move/card command. Round-only fixtures
    /// select the added encounter without claiming a full campaign playthrough.
    /// Claims and later Campaign023 rewards are covered by the shared 094 tests.
    /// </summary>
    public sealed class OpeningFreeRecruitQuest094Tests
    {
        const string InvitationPrefix = "EARNED_RECRUIT094_CARD|";
        GuildCityContent017D _content;
        RecruitmentContent _recruitmentContent;
        GuildCityExpeditionService017D _expeditions;
        GuildCityRecruitmentService017D _recruitment;
        GuildCityRecruitmentService017D _legacyRecruitment;
        HeroMaster300Catalog087 _catalog;

        [SetUp]
        public void SetUp()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(root, "GUILD_CITY_017D"));
            _recruitmentContent = RecruitmentContent.LoadFromDirectory(root);
            var resource = Resources.Load<TextAsset>(
                "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300");
            Assert.That(resource, Is.Not.Null, "The actual accepted HeroMaster catalog is required.");
            _catalog = HeroMaster300Catalog087.FromJson(resource.text);
            var generator = new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(root));
            _recruitment = new GuildCityRecruitmentService017D(generator, _catalog);
            _legacyRecruitment = new GuildCityRecruitmentService017D(generator);
            _expeditions = new GuildCityExpeditionService017D();
        }

        [Test]
        public void OnlyOneOfThirtySlotsChangesAndUnrelatedCardIdentitiesStayStable094()
        {
            var active = CreateActive();
            var freeCount = 0;
            for (var round = 0; round < 10; round++)
            {
                var campaign = WithRound(active, round);
                var row = _expeditions.BuildQuestCardRow090(campaign, _content, _recruitment);
                var legacy = _expeditions.BuildQuestCardRow090(campaign, _content, _legacyRecruitment);
                Assert.That(row.Count, Is.EqualTo(3), "Blind three-card rounds must remain intact.");
                Assert.That(legacy.Count, Is.EqualTo(3));
                for (var slot = 0; slot < row.Count; slot++)
                {
                    if (row[slot].Category == "FREE_RECRUIT")
                    {
                        freeCount++;
                        Assert.That(round, Is.EqualTo(5));
                        Assert.That(slot, Is.EqualTo(1));
                        Assert.That(legacy[slot].Category, Is.EqualTo("XP"),
                            "Missing catalog must safely retain the former XP slot.");
                        Assert.That(row[slot].DestinationNodeId, Is.EqualTo(legacy[slot].DestinationNodeId));
                    }
                    else
                        Assert.That(CanonicalJson.Serialize(row[slot]),
                            Is.EqualTo(CanonicalJson.Serialize(legacy[slot])),
                            "Unrelated cards, IDs and authored destinations must not migrate.");
                }
            }
            Assert.That(freeCount, Is.EqualTo(1));
            Assert.That(GuildCityExpeditionService017D.MinimumQuestCardRounds090, Is.EqualTo(10));
        }

        [TestCase(2, 1, 1)]
        [TestCase(0, 3, -3)]
        [TestCase(5, 0, 3)]
        public void ExactTwoDiceAndSavedModifierSurviveReloadWithoutReroll094(
            int boons, int scars, int expectedModifier)
        {
            var active = WithRound(CreateActive(), 5);
            var expedition = active.Guild.GuildCity.Expedition;
            var flags = expedition.ObjectiveFlags
                .Concat(Enumerable.Range(0, boons).Select(index =>
                    GuildCityExpeditionService017D.QuestCardRunBoonPrefix090 + "FIXTURE_" + index))
                .Concat(Enumerable.Range(0, scars).Select(index =>
                    GuildCityExpeditionService017D.QuestCardRunScarPrefix090 + "FIXTURE_" + index))
                .ToArray();
            active = WithExpedition(active, expedition.With(objectiveFlags: flags));
            var before = CanonicalJson.Serialize(active);
            var card = FreeCard(active);
            var hash = card.CardId.Substring("QUESTCARD090_".Length);
            Assert.That(card.DieOne, Is.EqualTo(1 + Convert.ToInt32(hash.Substring(10, 2), 16) % 6));
            Assert.That(card.DieTwo, Is.EqualTo(1 + Convert.ToInt32(hash.Substring(12, 2), 16) % 6));
            Assert.That(card.FateCheckModifier, Is.EqualTo(expectedModifier));
            Assert.That(card.Target, Is.EqualTo(7));
            Assert.That(card.DieOne, Is.InRange(1, 6));
            Assert.That(card.DieTwo, Is.InRange(1, 6));
            Assert.That(CanonicalJson.Serialize(active), Is.EqualTo(before), "Preview may not mutate RNG/save state.");
            var loaded = Reload(active);
            Assert.That(CanonicalJson.Serialize(FreeCard(loaded)), Is.EqualTo(CanonicalJson.Serialize(card)));
            Assert.That(_catalog.TryGetAcceptedHero(card.HeroRecruitId, out var hero), Is.True);
            Assert.That(hero.IsNormalApplicantEligible, Is.True, "No quarantined or SS automatic grants.");
            Assert.That(HeroMaster300ApplicantLead089.RosterContains(active.Guild.Recruits, hero), Is.False);
        }

        [Test]
        public void SuccessfulChanceMovesAndRecordsExactInvitationWithoutSpendingXpOrHiring094()
        {
            var active = FindOutcome(true);
            var card = FreeCard(active);
            var baseline = Require(_expeditions.CommitMove(active, _content, card.DestinationNodeId));
            var committed = Require(_expeditions.CommitQuestCard090(active, _content, _recruitment, card.CardId));
            var receipt = GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + card.CardId;
            Assert.That(card.TreasuryXpCost, Is.Zero);
            Assert.That(card.TreasuryXpDelta, Is.Zero);
            Assert.That(committed.Guild.TreasuryXp, Is.EqualTo(baseline.Guild.TreasuryXp));
            Assert.That(committed.Guild.Recruits.Count, Is.EqualTo(active.Guild.Recruits.Count),
                "A winning encounter promises a hero; it does not bypass safe-boundary recruitment.");
            Assert.That(committed.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo(card.DestinationNodeId));
            Assert.That(committed.Guild.Development.HasAdventureAuthority(receipt), Is.True);
            Assert.That(committed.Guild.GuildCity.Expedition.ObjectiveFlags, Does.Contain(receipt));
            Assert.That(Invitations(committed), Is.EqualTo(new[] {
                InvitationPrefix + receipt + "|" + card.HeroRecruitId + "|SKYHOME" }));
            Assert.That(committed.Guild.GuildCity.Expedition.Fatigue,
                Is.EqualTo(baseline.Guild.GuildCity.Expedition.Fatigue));
            Assert.That(committed.Guild.GuildCity.Expedition.Threat,
                Is.EqualTo(baseline.Guild.GuildCity.Expedition.Threat));
            var preview = _recruitment.DescribeEarnedCampaignRecruits094(committed, null, null);
            Assert.That(preview.PendingCardRecruits, Is.EqualTo(1));
            Assert.That(preview.CanClaim, Is.False);
            Assert.That(preview.Preview.Single().StableId, Is.EqualTo(card.HeroRecruitId));
            Assert.That(preview.Summary, Does.Contain("claim after this adventure"));
        }

        [Test]
        public void SuccessfulSavedCardCannotAwardASecondInvitationOnReplay094()
        {
            var active = FindOutcome(true);
            var card = FreeCard(active);
            var committed = Require(_expeditions.CommitQuestCard090(active, _content, _recruitment, card.CardId));
            var loaded = Reload(committed);
            Assert.That(Invitations(loaded), Is.EqualTo(Invitations(committed)));
            var beforeReplay = CanonicalJson.Serialize(loaded);
            var replay = _expeditions.CommitQuestCard090(loaded, _content, _recruitment, card.CardId);
            Assert.That(replay.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(loaded), Is.EqualTo(beforeReplay));
            Assert.That(Invitations(loaded).Length, Is.EqualTo(1));
            Assert.That(_recruitment.DescribeEarnedCampaignRecruits094(loaded, null, null)
                .PendingCardRecruits, Is.EqualTo(1));
        }

        [Test]
        public void FailedChanceAdvancesNormallyWithoutInvitationOrXpPenalty094()
        {
            var active = FindOutcome(false);
            var card = FreeCard(active);
            var baseline = Require(_expeditions.CommitMove(active, _content, card.DestinationNodeId));
            var committed = Require(_expeditions.CommitQuestCard090(active, _content, _recruitment, card.CardId));
            Assert.That(committed.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo(card.DestinationNodeId));
            Assert.That(committed.Guild.TreasuryXp, Is.EqualTo(baseline.Guild.TreasuryXp));
            Assert.That(committed.Guild.Recruits.Count, Is.EqualTo(active.Guild.Recruits.Count));
            Assert.That(Invitations(committed), Is.Empty);
            Assert.That(committed.Guild.Development.HasAdventureAuthority(
                GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + card.CardId), Is.True);
            Assert.That(GuildCityExpeditionService017D.QuestCardRoundCount090(
                committed.Guild.GuildCity.Expedition.ObjectiveFlags), Is.EqualTo(6));
            Assert.That(Invitations(Reload(committed)), Is.Empty);
        }

        [Test]
        public void NewlyOwnedCandidateChangesIdentityAndRejectsStaleFreeCard094()
        {
            var active = WithRound(CreateActive(), 5);
            var original = FreeCard(active);
            var guild = active.Guild.With(active.Guild.TreasuryXp,
                active.Guild.Recruits.Concat(new[] {
                    new RecruitState(original.HeroRecruitId, 100, 100, 20, 20) }).ToArray(),
                active.Guild.Unions, active.Guild.Inventory, active.Guild.Development);
            var changed = active.With(guild, active.OpeningFlow);
            var replacement = FreeCard(changed);
            Assert.That(replacement.HeroRecruitId, Is.Not.EqualTo(original.HeroRecruitId));
            Assert.That(replacement.CardId, Is.Not.EqualTo(original.CardId));
            Assert.That(_expeditions.CommitQuestCard090(changed, _content, _recruitment, original.CardId)
                .Errors, Does.Contain("QUEST_CARD090_NOT_IN_CURRENT_ROW"));
            Assert.That(Invitations(changed), Is.Empty);
        }

        [Test]
        public void NormalPaidRecruitCardKeepsExistingSigningPriceAndAuthority094()
        {
            var active = WithRound(CreateActive(true), 1);
            var card = _expeditions.BuildQuestCardRow090(active, _content, _legacyRecruitment)
                .Single(value => value.Category == "RECRUIT");
            var applicant = active.Guild.GuildCity.RecruitmentBoard.FindApplicant(card.RecruitApplicantId);
            Assert.That(applicant, Is.Not.Null);
            var price = GuildCityRecruitmentService017D.EffectiveSigningCostTreasuryXp(
                active, applicant.SigningCostTreasuryXp);
            Assert.That(price, Is.GreaterThan(0));
            var baseline = Require(_expeditions.CommitMove(active, _content, card.DestinationNodeId));
            var committed = Require(_expeditions.CommitQuestCard090(active, _content, _legacyRecruitment, card.CardId));
            Assert.That(committed.Guild.TreasuryXp, Is.EqualTo(baseline.Guild.TreasuryXp - price));
            var signed = committed.Guild.Recruits.Single(value => value.RecruitId == applicant.RecruitId);
            Assert.That(signed.Progression.LearnedArtIds, Is.Not.Empty);
            Assert.That(signed.Equipment.Assignments, Is.Not.Empty);
            Assert.That(Invitations(committed), Is.Empty);
        }

        CampaignState CreateActive(bool withPaidBoard = false)
        {
            var recruits = Enumerable.Range(1, 10).Select(index =>
                new RecruitState("R" + index, 100, 100, 20, 20)).ToArray();
            var unions = new[] {
                new UnionState("U1", "First Union", UnionKind.Normal, "R1",
                    new[] { "R1", "R2", "R3" }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Second Union", UnionKind.Normal, "R4",
                    new[] { "R4", "R5", "R6" }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000) };
            var guild = new GuildState("GUILD_OPENING_FREE_RECRUIT_094", 5000, recruits, unions);
            var flow = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001",
                true, null, false, 439, 0, true, true, true, true, "complete");
            var profile = new NewGuildProfileState("Opening recruitment fixture",
                SecondDimension.Core.GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var campaign = new CampaignState("00000000-0000-0000-0000-000000094090", 94090,
                "1.0", ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
            if (withPaidBoard)
                campaign = Require(LegacyApplicantBoardFixture124.Commit(campaign, _recruitmentContent, _content));
            campaign = Require(_expeditions.AcceptContract(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            return Require(_expeditions.StartExpedition(campaign, _content));
        }

        CampaignState FindOutcome(bool win)
        {
            var source = WithRound(CreateActive(), 5);
            for (var index = 0; index < 128; index++)
            {
                var current = source.Guild.GuildCity.Expedition;
                var expedition = new ExpeditionState017D("OPENING_RECRUIT_094_OUTCOME_" + index,
                    current.ContractCommitId, current.BoardId, current.CurrentNodeId, current.Status,
                    current.Supplies, current.Fatigue, current.Threat, current.Urgency,
                    current.VisitedNodeIds, current.RevealedNodeIds, current.CommittedMoveIds,
                    current.CommittedChecks, current.ObjectiveFlags, current.LastCheckpointId);
                var candidate = WithExpedition(source, expedition);
                var card = FreeCard(candidate);
                if ((card.DieOne + card.DieTwo + card.FateCheckModifier >= card.Target) == win)
                    return candidate;
            }
            Assert.Fail("No deterministic " + (win ? "success" : "miss") + " fixture found in bounded search.");
            return null;
        }

        GuildQuestCardOffer090 FreeCard(CampaignState campaign) =>
            _expeditions.BuildQuestCardRow090(campaign, _content, _recruitment)
                .Single(value => value.Category == "FREE_RECRUIT");

        static string[] Invitations(CampaignState campaign) => campaign.Guild.Development
            .AppliedAdventureAuthorityIds.Where(value => value.StartsWith(InvitationPrefix,
                StringComparison.Ordinal)).ToArray();

        static CampaignState WithRound(CampaignState campaign, int round)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var flags = source.ObjectiveFlags.Where(value => !(value ?? string.Empty).StartsWith(
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090, StringComparison.Ordinal))
                .Concat(Enumerable.Range(0, round).Select(index =>
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + "FIXTURE_HISTORY_" + index))
                .ToArray();
            return WithExpedition(campaign, source.With(objectiveFlags: flags,
                lastCheckpointId: "opening_094_round_fixture_" + round));
        }

        static CampaignState WithExpedition(CampaignState campaign, ExpeditionState017D expedition)
        {
            var city = campaign.Guild.GuildCity.With(expedition: expedition, replaceExpedition: true,
                pendingEncounter: null, replacePendingEncounter: true,
                pendingBattleReturn: null, replacePendingBattleReturn: true);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState Reload(CampaignState campaign)
        {
            var path = Path.Combine(Path.GetTempPath(), "opening_recruit_094_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(campaign,
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                return loaded.Value.CampaignState;
            }
            finally
            {
                foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
                    if (File.Exists(candidate)) File.Delete(candidate);
            }
        }

        static CampaignState Require(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
