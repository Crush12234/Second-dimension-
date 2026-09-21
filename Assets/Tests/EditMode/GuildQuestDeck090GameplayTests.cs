using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>
    /// Gameplay-authority coverage for the physical three-card quest deck. These
    /// tests intentionally start the real first-hour contract and use its authored
    /// N00 -> N01 handoff so the reward layer cannot replace story-battle authority.
    /// </summary>
    public sealed class GuildQuestDeck090GameplayTests
    {
        private static IEnumerable<EquipmentItemState> OwnedChestItems092(CampaignState campaign) =>
            campaign.Guild.Inventory.Concat(campaign.Guild.Recruits.SelectMany(recruit =>
                recruit.Equipment.Assignments.Select(slot => slot.Item)));

        private GuildCityContent017D _content;
        private GuildCityExpeditionService017D _expeditions;
        private RecruitmentContent _recruitmentContent;
        private GuildCityRecruitmentService017D _recruitment;
        private GuildCityBattleBridgeService017D _battleBridge;
        private M2BattleCommandService _battles;
        private M2CombatContent _combatContent;

        [SetUp]
        public void SetUp()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT");
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                contentRoot, "GUILD_CITY_017D"));
            _expeditions = new GuildCityExpeditionService017D();
            _recruitmentContent = RecruitmentContent.LoadFromDirectory(contentRoot);
            _recruitment = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)));
            _battleBridge = new GuildCityBattleBridgeService017D();
            _battles = new M2BattleCommandService();
            _combatContent = M2CombatContent.LoadFromDirectory(contentRoot);
        }

        [Test]
        public void TenRoundsExposeThirtyDeterministicChoicesBesideAuthoredBattles090()
        {
            var active = CreateActiveFirstHourCampaign090(withApplicantBoard: true);
            var allCardIds = new HashSet<string>(StringComparer.Ordinal);
            var categories = new HashSet<string>(StringComparer.Ordinal);

            for (var round = 0;
                 round < GuildCityExpeditionService017D.MinimumQuestCardRounds090;
                 round++)
            {
                var staged = WithQuestRound090(active, round);
                var first = _expeditions.BuildQuestCardRow090(
                    staged, _content, _recruitment);
                var replay = _expeditions.BuildQuestCardRow090(
                    staged, _content, _recruitment);

                Assert.That(first, Has.Count.EqualTo(3),
                    "Every quest turn must present exactly three physical choices.");
                Assert.That(CanonicalJson.Serialize(replay),
                    Is.EqualTo(CanonicalJson.Serialize(first)),
                    "Reopening a saved turn must never reshuffle its card row.");
                Assert.That(first.Select(value => value.CardId).Distinct().Count(),
                    Is.EqualTo(3));
                foreach (var card in first)
                {
                    Assert.That(allCardIds.Add(card.CardId), Is.True,
                        "Card authority IDs must remain unique across all ten rounds.");
                    categories.Add(card.Category);
                }
            }

            Assert.That(allCardIds.Count, Is.EqualTo(30));
            Assert.That(categories, Is.SupersetOf(new[]
            {
                "CHEST", "XP", "BOON", "MERCHANT", "RECRUIT", "BATTLE",
                "SCAR", "FATE"
            }));

            var board = _content.Board(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071);
            var authoredBattles = new[] { "N01", "N06", "N13" }
                .Select(board.Node)
                .ToArray();
            Assert.That(authoredBattles.Select(value => value.EncounterId),
                Is.EqualTo(new[]
                {
                    GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071,
                    "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                    GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071
                }));
            Assert.That(authoredBattles.All(value =>
                    !GuildCityExpeditionService017D.IsEncounterCleared(
                        active.Guild.GuildCity.Expedition, value)),
                Is.True,
                "The thirty-card reward schedule must coexist with all three locked story battles.");
        }

        [TestCase(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071, 12)]
        [TestCase(GuildCityExpeditionService017D.SecondStoryBoardId076, 10)]
        [TestCase(GuildCityExpeditionService017D.ReliefRoadBoardId081, 10)]
        public void EveryAdvertisedOpeningQuestHasTenActualCardMovesOnShortestRoute090(
            string boardId,
            int expectedMinimum)
        {
            var board = _content.Board(boardId);
            var minimum = GuildCityContent017D
                .MinimumQuestCardMovesToExit090(board);

            Assert.That(minimum, Is.EqualTo(expectedMinimum));
            Assert.That(minimum,
                Is.GreaterThanOrEqualTo(
                    GuildCityExpeditionService017D.MinimumQuestCardRounds090),
                boardId +
                " must provide ten real card choices before exit; locked story battles do not count as rounds.");
        }

        [Test]
        public void FreshProceduralApplicantsRemainRealRecruitChoices090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var applicants = active.Guild.GuildCity.RecruitmentBoard.Applicants;

            Assert.That(applicants, Is.Not.Empty);
            Assert.That(applicants.Any(value => string.IsNullOrWhiteSpace(
                    _recruitment.OwnedRecruitIdForApplicant089(active, value))),
                Is.True,
                "Blank stable-authority fields must not make every normal applicant look owned.");
            Assert.That(_expeditions.BuildQuestCardRow090(
                    active, _content, _recruitment)
                    .Count(value => StringComparer.Ordinal.Equals(
                        value.Category, "RECRUIT")),
                Is.EqualTo(1));
        }

        [Test]
        public void UnclearedOptionalEliteKeepsItsFightOrSkipChoiceInsteadOfDeck090()
        {
            var active = CreateActiveFirstHourCampaign090(
                withApplicantBoard: true);
            var staged = WithCurrentNode090(
                active,
                "N09",
                Array.Empty<string>());
            var elite = _content.Board(
                    GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071)
                .Node("N09");

            Assert.That(elite.Kind, Is.EqualTo("OPTIONAL_ELITE"));
            Assert.That(GuildCityExpeditionService017D.IsEncounterCleared(
                staged.Guild.GuildCity.Expedition, elite), Is.False);
            Assert.That(_expeditions.BuildQuestCardRow090(
                staged, _content, _recruitment), Is.Empty,
                "The existing visible FIGHT/SKIP decision must not be covered by a deck row.");
        }

        [Test]
        public void RecruitCardIdBindsTheCurrentApplicantAndRejectsAStaleBoard090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var original = RequireCard090(active, "RECRUIT");
            var board = active.Guild.GuildCity.RecruitmentBoard;
            var replacementBoard = new ApplicantBoardState(
                board.BoardId,
                board.GenerationKey,
                board.RefreshOrdinal,
                true,
                board.Applicants.Where(value => !StringComparer.Ordinal.Equals(
                        value.RecruitId, original.RecruitApplicantId))
                    .ToArray(),
                string.Empty);
            var changed = WithRecruitmentBoard090(active, replacementBoard);
            var replacement = RequireCard090(changed, "RECRUIT");

            Assert.That(replacement.RecruitApplicantId,
                Is.Not.EqualTo(original.RecruitApplicantId));
            Assert.That(replacement.CardId, Is.Not.EqualTo(original.CardId),
                "Saved card identity must include the applicant it will sign or ascend.");
            var stale = _expeditions.CommitQuestCard090(
                changed, _content, _recruitment, original.CardId);
            Assert.That(stale.IsSuccess, Is.False);
            Assert.That(stale.Errors,
                Does.Contain("QUEST_CARD090_NOT_IN_CURRENT_ROW"));
        }

        [Test]
        public void RecruitCardIdBindsCanonicalSnapshotAndEffectiveSigningCost090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var original = RequireCard090(active, "RECRUIT");
            var board = active.Guild.GuildCity.RecruitmentBoard;
            var applicant = board.FindApplicant(original.RecruitApplicantId);
            var changedApplicant = CopyApplicant090(
                applicant,
                applicant.SigningCostTreasuryXp + 7,
                applicant.CanonicalScoutingReportJson + "|UPDATED");
            var changedBoard = new ApplicantBoardState(
                board.BoardId,
                board.GenerationKey,
                board.RefreshOrdinal,
                true,
                board.Applicants.Select(value =>
                        StringComparer.Ordinal.Equals(
                            value.RecruitId, applicant.RecruitId)
                            ? changedApplicant
                            : value)
                    .ToArray(),
                string.Empty);
            var changed = WithRecruitmentBoard090(active, changedBoard);
            var replacement = RequireCard090(changed, "RECRUIT");

            Assert.That(replacement.RecruitApplicantId,
                Is.EqualTo(original.RecruitApplicantId));
            Assert.That(replacement.CardId, Is.Not.EqualTo(original.CardId),
                "Card authority must bind RecruitId, stable identity, effective " +
                "price, and the canonical applicant snapshot.");
            var stale = _expeditions.CommitQuestCard090(
                changed, _content, _recruitment, original.CardId);
            Assert.That(stale.IsSuccess, Is.False);
            Assert.That(stale.Errors,
                Does.Contain("QUEST_CARD090_NOT_IN_CURRENT_ROW"));
        }

        [Test]
        public void FullRosterRecruitCardUsesTheSigningForecastAndStaysLocked090()
        {
            var active = WithRosterCount090(
                WithQuestRound090(
                    CreateActiveFirstHourCampaign090(withApplicantBoard: true),
                    1),
                12);
            var card = RequireCard090(active, "RECRUIT");
            var applicant = active.Guild.GuildCity.RecruitmentBoard
                .FindApplicant(card.RecruitApplicantId);
            var forecast = _recruitment.DescribeApplicantSigning090(
                active, applicant);

            Assert.That(forecast.CanSign, Is.False);
            Assert.That(forecast.FailureCode,
                Is.EqualTo("GC017D_ROSTER_CAPACITY_REACHED"));
            Assert.That(card.CanChoose, Is.False);
            Assert.That(card.LockedReason, Is.EqualTo(forecast.LockedReason));
            Assert.That(card.LockedReason, Does.Contain("Roster full"));
            Assert.That(_recruitment.SignApplicant(
                    active, applicant.RecruitId).Errors,
                Does.Contain(forecast.FailureCode),
                "The card preview and commit service must share one legality forecast.");
            Assert.That(_expeditions.CommitQuestCard090(
                    active, _content, _recruitment, card.CardId).Errors,
                Does.Contain(forecast.LockedReason));
        }

        [Test]
        public void PermanentBoonCardIdBindsTheHeroItWillRaise090()
        {
            var source = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 0);
            CampaignState ready = null;
            GuildQuestCardOffer090 original = null;
            for (var ordinal = 0; ordinal < 4096 && original == null; ordinal++)
            {
                var candidate = WithExpeditionId090(source,
                    "EXPEDITION_QUEST_BOON_IDENTITY_" + ordinal.ToString("D4"));
                var offer = RequireCard090(candidate, "BOON");
                if (!offer.IsPermanentHeroBoon) continue;
                ready = candidate;
                original = offer;
            }

            Assert.That(original, Is.Not.Null);
            var replacementId = original.HeroRecruitId + "_ALT";
            var changed = ReplaceActiveRecruitIdentity090(
                ready, original.HeroRecruitId, replacementId);
            var replacement = RequireCard090(changed, "BOON");

            Assert.That(replacement.IsPermanentHeroBoon, Is.True);
            Assert.That(replacement.HeroRecruitId, Is.EqualTo(replacementId));
            Assert.That(replacement.CardId, Is.Not.EqualTo(original.CardId),
                "Saved card identity must include the permanent-growth recipient.");
            var stale = _expeditions.CommitQuestCard090(
                changed, _content, _recruitment, original.CardId);
            Assert.That(stale.IsSuccess, Is.False);
            Assert.That(stale.Errors,
                Does.Contain("QUEST_CARD090_NOT_IN_CURRENT_ROW"));
        }

        [Test]
        public void FateCardStaysNeutralUntilCommitAndUsesSavedRunModifier090()
        {
            var source = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 3);
            CampaignState exactSix = null;
            GuildQuestCardOffer090 unmodified = null;
            for (var ordinal = 0; ordinal < 4096 && unmodified == null; ordinal++)
            {
                var candidate = WithExpeditionId090(source,
                    "EXPEDITION_QUEST_FATE_MODIFIER_" + ordinal.ToString("D4"));
                var fate = RequireCard090(candidate, "FATE");
                if (fate.DieOne + fate.DieTwo != 6) continue;
                exactSix = candidate;
                unmodified = fate;
            }

            Assert.That(unmodified, Is.Not.Null);
            var blessedCampaign = WithObjectiveFlag090(
                exactSix,
                GuildCityExpeditionService017D.QuestCardRunBoonPrefix090 +
                "FATE_FIXTURE");
            var blessed = RequireCard090(blessedCampaign, "FATE");

            Assert.That(unmodified.Title, Is.EqualTo("Test Your Fate"));
            Assert.That(blessed.Title, Is.EqualTo("Test Your Fate"));
            Assert.That(blessed.RewardPreview, Does.Contain("Pass:"));
            Assert.That(blessed.RewardPreview, Does.Contain("Miss:"));
            Assert.That(blessed.CardId, Is.EqualTo(unmodified.CardId));
            Assert.That(blessed.DieOne, Is.EqualTo(unmodified.DieOne));
            Assert.That(blessed.DieTwo, Is.EqualTo(unmodified.DieTwo));
            Assert.That(unmodified.FateCheckModifier, Is.Zero);
            Assert.That(blessed.FateCheckModifier, Is.EqualTo(1));
            Assert.That(unmodified.TreasuryXpDelta, Is.EqualTo(3));
            Assert.That(blessed.TreasuryXpDelta, Is.EqualTo(20),
                "The saved quest boon must turn a six into a passing total of seven.");
        }

        [Test]
        public void ChestCommitsExactPowerReceiptAndSavePersistentEquipment090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 0);
            var card = RequireCard090(active, "CHEST");
            var receiptId = GuildCityExpeditionService017D.QuestCardReceiptPrefix090 +
                            card.CardId;
            var expectedItem = GuildCityExpeditionService017D
                .CreateQuestCardEquipment090(card.CardId, receiptId, false);
            var expectedPower = M2EquipmentPowerPolicy087.Resolve(expectedItem);
            var movedWithoutCard = Require(_expeditions.CommitMove(
                active, _content, card.DestinationNodeId));

            var committed = Require(_expeditions.CommitQuestCard090(
                active, _content, _recruitment, card.CardId));
            var item = OwnedChestItems092(committed).Single(value =>
                StringComparer.Ordinal.Equals(value.InstanceId,
                    expectedItem.InstanceId));

            Assert.That(CanonicalJson.Serialize(item),
                Is.EqualTo(CanonicalJson.Serialize(expectedItem)));
            Assert.That(card.PhysicalPower, Is.EqualTo(expectedPower.PhysicalAttack));
            Assert.That(card.MysticPower, Is.EqualTo(expectedPower.MysticAttack));
            Assert.That(committed.Guild.TreasuryXp,
                Is.EqualTo(movedWithoutCard.Guild.TreasuryXp +
                           card.TreasuryXpDelta));
            Assert.That(committed.Guild.Development.HasAdventureAuthority(receiptId),
                Is.True);
            Assert.That(committed.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Contain(receiptId));

            var savePath = Path.Combine(Path.GetTempPath(),
                "quest_deck_090_chest_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new AtomicSaveStore();
                store.Write(savePath, SaveEnvelopeV1.Create(
                    committed,
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(savePath);
                Assert.That(loaded.IsSuccess, Is.True,
                    string.Join("\n", loaded.Errors));
                Assert.That(OwnedChestItems092(loaded.Value.CampaignState).Count(value =>
                        StringComparer.Ordinal.Equals(value.InstanceId,
                            expectedItem.InstanceId)),
                    Is.EqualTo(1), "The exact reward survives either equipped or in Inventory, never both.");
                Assert.That(loaded.Value.CampaignState.Guild.Development
                    .HasAdventureAuthority(receiptId), Is.True);
            }
            finally
            {
                DeleteSaveFamily090(savePath);
            }
        }

        [Test]
        public void MerchantChargesItsDisplayedExactXpAndLocksWhenOneXpShort090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var card = RequireCard090(active, "MERCHANT");
            Assert.That(card.TreasuryXpCost, Is.GreaterThan(0));
            var movedWithoutCard = Require(_expeditions.CommitMove(
                active, _content, card.DestinationNodeId));

            var purchased = Require(_expeditions.CommitQuestCard090(
                active, _content, _recruitment, card.CardId));
            Assert.That(purchased.Guild.TreasuryXp,
                Is.EqualTo(movedWithoutCard.Guild.TreasuryXp -
                           card.TreasuryXpCost));
            Assert.That(purchased.Guild.Inventory.Count,
                Is.EqualTo(active.Guild.Inventory.Count + 1));
            var item = purchased.Guild.Inventory.Single(value =>
                StringComparer.Ordinal.Equals(value.DisplayName, card.ItemName));
            var power = M2EquipmentPowerPolicy087.Resolve(item);
            Assert.That(power.PhysicalAttack, Is.EqualTo(card.PhysicalPower));
            Assert.That(power.MysticAttack, Is.EqualTo(card.MysticPower));

            var underfunded = WithTreasuryXp090(active,
                Math.Max(0, card.TreasuryXpCost - 1));
            var locked = RequireCard090(underfunded, "MERCHANT");
            Assert.That(locked.CardId, Is.EqualTo(card.CardId));
            Assert.That(locked.CanChoose, Is.False);
            Assert.That(locked.LockedReason,
                Does.Contain(card.TreasuryXpCost.ToString()));
            var rejected = _expeditions.CommitQuestCard090(
                underfunded, _content, _recruitment, locked.CardId);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain(locked.LockedReason));
            Assert.That(underfunded.Guild.TreasuryXp,
                Is.EqualTo(card.TreasuryXpCost - 1));
        }

        [Test]
        public void BoonAndScarCommitOpposedRunModifiersAndExactConsequences090()
        {
            var boonReady = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 0);
            var boon = RequireCard090(boonReady, "BOON");
            var boonMove = Require(_expeditions.CommitMove(
                boonReady, _content, boon.DestinationNodeId));
            var blessed = Require(_expeditions.CommitQuestCard090(
                boonReady, _content, _recruitment, boon.CardId));

            Assert.That(GuildCityExpeditionService017D.QuestCardRunCheckModifier090(
                    blessed.Guild.GuildCity.Expedition.ObjectiveFlags),
                Is.EqualTo(1));
            Assert.That(blessed.Guild.GuildCity.Expedition.Supplies,
                Is.EqualTo(boonMove.Guild.GuildCity.Expedition.Supplies + 1));
            Assert.That(blessed.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Contain(GuildCityExpeditionService017D.QuestCardRunBoonPrefix090 +
                             boon.CardId));

            var scarReady = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 2);
            var scar = RequireCard090(scarReady, "SCAR");
            var scarMove = Require(_expeditions.CommitMove(
                scarReady, _content, scar.DestinationNodeId));
            var scarred = Require(_expeditions.CommitQuestCard090(
                scarReady, _content, _recruitment, scar.CardId));

            Assert.That(GuildCityExpeditionService017D.QuestCardRunCheckModifier090(
                    scarred.Guild.GuildCity.Expedition.ObjectiveFlags),
                Is.EqualTo(-1));
            Assert.That(scarred.Guild.TreasuryXp,
                Is.EqualTo(scarMove.Guild.TreasuryXp + 24));
            Assert.That(scarred.Guild.GuildCity.Expedition.Fatigue,
                Is.EqualTo(scarMove.Guild.GuildCity.Expedition.Fatigue + 2));
            Assert.That(scarred.Guild.GuildCity.Expedition.Threat,
                Is.EqualTo(scarMove.Guild.GuildCity.Expedition.Threat + 1));
            Assert.That(scarred.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Contain(GuildCityExpeditionService017D.QuestCardRunScarPrefix090 +
                             scar.CardId));
        }

        [Test]
        public void HeroicBreakthroughPermanentlyRaisesTheSelectedRealHero090()
        {
            var source = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 0);
            CampaignState ready = null;
            GuildQuestCardOffer090 boon = null;
            for (var ordinal = 0; ordinal < 4096 && boon == null; ordinal++)
            {
                var candidate = WithExpeditionId090(source,
                    "EXPEDITION_QUEST_BOON_TEST_" + ordinal.ToString("D4"));
                var offer = RequireCard090(candidate, "BOON");
                if (!offer.IsPermanentHeroBoon) continue;
                ready = candidate;
                boon = offer;
            }

            Assert.That(boon, Is.Not.Null,
                "A bounded deterministic search must expose the authored permanent-boon branch.");
            var before = ready.Guild.Recruits.Single(value =>
                StringComparer.Ordinal.Equals(value.RecruitId,
                    boon.HeroRecruitId)).Progression;
            var committed = Require(_expeditions.CommitQuestCard090(
                ready, _content, _recruitment, boon.CardId));
            var after = committed.Guild.Recruits.Single(value =>
                StringComparer.Ordinal.Equals(value.RecruitId,
                    boon.HeroRecruitId)).Progression;

            Assert.That(after.MaximumHpBonus, Is.EqualTo(before.MaximumHpBonus + 8));
            Assert.That(after.MaximumMpBonus, Is.EqualTo(before.MaximumMpBonus + 2));
            Assert.That(after.StrengthBonus, Is.EqualTo(before.StrengthBonus + 1));
            Assert.That(after.DefenseBonus, Is.EqualTo(before.DefenseBonus + 1));
            Assert.That(after.AgilityBonus, Is.EqualTo(before.AgilityBonus + 1));
            Assert.That(after.MagicBonus, Is.EqualTo(before.MagicBonus + 1));
            Assert.That(after.WillBonus, Is.EqualTo(before.WillBonus + 1));
            Assert.That(committed.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Contain("QUEST_CARD090_PERMANENT_BOON_" + boon.CardId + "_" +
                             boon.HeroRecruitId));
        }

        [Test]
        public void RecruitCardUsesTheExistingSigningAuthority090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var card = RequireCard090(active, "RECRUIT");
            var applicant = active.Guild.GuildCity.RecruitmentBoard
                .FindApplicant(card.RecruitApplicantId);
            Assert.That(applicant, Is.Not.Null);
            var signingCost = GuildCityRecruitmentService017D
                .EffectiveSigningCostTreasuryXp(active,
                    applicant.SigningCostTreasuryXp);
            var movedWithoutCard = Require(_expeditions.CommitMove(
                active, _content, card.DestinationNodeId));

            var recruited = Require(_expeditions.CommitQuestCard090(
                active, _content, _recruitment, card.CardId));
            var signed = recruited.Guild.Recruits.Single(value =>
                StringComparer.Ordinal.Equals(value.RecruitId,
                    applicant.RecruitId));

            Assert.That(recruited.Guild.Recruits.Count,
                Is.EqualTo(active.Guild.Recruits.Count + 1));
            Assert.That(recruited.Guild.TreasuryXp,
                Is.EqualTo(movedWithoutCard.Guild.TreasuryXp - signingCost));
            Assert.That(signed.DisplayName, Is.EqualTo(applicant.DisplayName));
            Assert.That(signed.SignatureId, Is.EqualTo(applicant.SignatureId));
            Assert.That(signed.AuthoredStableRecruitId,
                Is.EqualTo(applicant.AuthoredStableRecruitId));
            Assert.That(signed.Progression.LearnedArtIds, Is.Not.Empty,
                "Quest recruitment must pass through the normal auto-generation signing service.");
            Assert.That(signed.Equipment.Assignments, Is.Not.Empty,
                "The real signing authority must preserve its legal opening loadout.");
            Assert.That(_recruitment.OwnedRecruitIdForApplicant089(
                    recruited, applicant),
                Is.EqualTo(signed.RecruitId));
        }

        [Test]
        public void BattleCardCommitsBridgeAuthorityWithoutClearingLockedStoryBattle090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var card = RequireCard090(active, "BATTLE");
            var cardReceipt = GuildCityExpeditionService017D
                                  .QuestCardReceiptPrefix090 + card.CardId;
            var board = _content.Board(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071);
            var lockedStoryBattle = board.Node("N01");
            Assert.That(GuildCityExpeditionService017D.IsEncounterCleared(
                active.Guild.GuildCity.Expedition, lockedStoryBattle), Is.False);

            var committed = Require(_expeditions.CommitQuestCard090(
                active, _content, _recruitment, card.CardId));
            var city = committed.Guild.GuildCity;
            var pending = city.PendingEncounter;
            Assert.That(pending, Is.Not.Null);
            Assert.That(city.Expedition.Status,
                Is.EqualTo(ExpeditionStatus017D.AwaitingBattle));
            Assert.That(city.Expedition.CurrentNodeId, Is.EqualTo("N01"));
            Assert.That(pending.EncounterId, Is.EqualTo(card.EncounterId));
            Assert.That(pending.EnemyUnionCount, Is.EqualTo(card.EnemyUnionCount));
            Assert.That(pending.NodeId, Does.StartWith("QUEST_CARD_BATTLE_NODE090_"));
            Assert.That(pending.NodeId, Is.Not.EqualTo(lockedStoryBattle.Id),
                "The random card battle must not masquerade as the authored N01 encounter.");
            Assert.That(pending.RouteModifiers,
                Does.Contain("QUEST_CARD_RANDOM_BATTLE_090"));
            Assert.That(GuildCityExpeditionService017D.IsEncounterCleared(
                city.Expedition, lockedStoryBattle), Is.False,
                "Returning from the card battle must still leave the authored Hall Breach locked.");

            var bridgeAuthority = GuildCityBattleBridgeService017D
                .EncounterRequestAuthorityId084(pending);
            Assert.That(committed.Guild.Development.AppliedAdventureAuthorityIds
                    .Count(value => StringComparer.Ordinal.Equals(
                        value, bridgeAuthority)),
                Is.EqualTo(1));
            Assert.That(committed.Guild.Development.AppliedAdventureAuthorityIds
                    .Count(value => StringComparer.Ordinal.Equals(
                        value, cardReceipt)),
                Is.EqualTo(1));
            Assert.That(city.Expedition.ObjectiveFlags.Count(value =>
                    StringComparer.Ordinal.Equals(value, cardReceipt)),
                Is.EqualTo(1));

            var replay = _expeditions.CommitQuestCard090(
                committed, _content, _recruitment, card.CardId);
            Assert.That(replay.IsSuccess, Is.False);
            Assert.That(committed.Guild.Development.AppliedAdventureAuthorityIds
                    .Count(value => StringComparer.Ordinal.Equals(
                        value, bridgeAuthority)),
                Is.EqualTo(1));
            Assert.That(committed.Guild.Development.AppliedAdventureAuthorityIds
                    .Count(value => StringComparer.Ordinal.Equals(
                        value, cardReceipt)),
                Is.EqualTo(1));

            var receiptAlreadyApplied = WithAdventureAuthority090(
                active, cardReceipt);
            var duplicate = _expeditions.CommitQuestCard090(
                receiptAlreadyApplied, _content, _recruitment, card.CardId);
            Assert.That(duplicate.IsSuccess, Is.False);
            Assert.That(duplicate.Errors,
                Does.Contain("QUEST_CARD090_RECEIPT_ALREADY_APPLIED"));
            Assert.That(receiptAlreadyApplied.Guild.GuildCity.PendingEncounter,
                Is.Null);
            Assert.That(GuildCityExpeditionService017D.IsEncounterCleared(
                receiptAlreadyApplied.Guild.GuildCity.Expedition,
                lockedStoryBattle), Is.False);
        }

        [Test]
        public void OptionalBattleWinsClaimsReturnsAndReloadsExactlyOnce090()
        {
            var active = WithQuestRound090(
                CreateActiveFirstHourCampaign090(withApplicantBoard: true), 1);
            var card = RequireCard090(active, "BATTLE");
            Assert.That(card.Description, Does.Contain("Defeat ends"),
                "A card must not promise route continuation after a loss.");
            var committed = Require(_expeditions.CommitQuestCard090(
                active, _content, _recruitment, card.CardId));
            var request = committed.Guild.GuildCity.PendingEncounter;
            var storyNodeId = committed.Guild.GuildCity.Expedition.CurrentNodeId;

            var started = Require(_battleBridge.StartCertifiedEncounter(
                committed, _battles, _combatContent));
            Assert.That(started.Battle.BattleId, Is.EqualTo(request.BattleId));
            Assert.That(started.Battle.EnemyUnions,
                Has.Count.EqualTo(card.EnemyUnionCount));
            var won = ResolveBattleVictory090(started);
            var returnCommitted = Require(
                _battleBridge.CommitBattleReturn(won));
            var returnReceipt = returnCommitted.Guild.GuildCity
                .PendingBattleReturn;
            var claimed = Require(_battles.ClaimBattleRewards(returnCommitted));
            var returned = Require(
                _battleBridge.ApplyBattleReturnExactlyOnce(claimed));

            Assert.That(returned.Guild.GuildCity.PendingEncounter, Is.Null);
            Assert.That(returned.Guild.GuildCity.PendingBattleReturn, Is.Null);
            Assert.That(returned.Guild.GuildCity.Expedition.Status,
                Is.EqualTo(ExpeditionStatus017D.Active));
            Assert.That(returned.Guild.GuildCity.Expedition.CurrentNodeId,
                Is.EqualTo(storyNodeId));
            Assert.That(returned.Guild.GuildCity.AppliedBattleReturnIds.Count(
                    value => StringComparer.Ordinal.Equals(
                        value, returnReceipt.ReceiptId)),
                Is.EqualTo(1));
            Assert.That(returned.Guild.Development.ClaimedBattleRewardIds.Count(
                    value => StringComparer.Ordinal.Equals(
                        value, returned.Battle.Reward.RewardId)),
                Is.EqualTo(1));
            Assert.That(returned.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Contain(GuildCityExpeditionService017D
                    .EncounterClearedFlag(request.NodeId)));

            var savePath = Path.Combine(Path.GetTempPath(),
                "quest_deck_090_battle_" + Guid.NewGuid().ToString("N") +
                ".json");
            try
            {
                var store = new AtomicSaveStore();
                store.Write(savePath, SaveEnvelopeV1.Create(
                    returned,
                    new DateTime(1970, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(savePath);
                Assert.That(loaded.IsSuccess, Is.True,
                    string.Join("\n", loaded.Errors));
                var reloaded = loaded.Value.CampaignState;
                var beforeReplay = CanonicalJson.Sha256Hex(reloaded);
                var replay = Require(
                    _battleBridge.ApplyBattleReturnExactlyOnce(reloaded));
                Assert.That(CanonicalJson.Sha256Hex(replay),
                    Is.EqualTo(beforeReplay),
                    "A save reload must not apply card-battle rewards twice.");
            }
            finally
            {
                DeleteSaveFamily090(savePath);
            }
        }

        private GuildQuestCardOffer090 RequireCard090(
            CampaignState campaign,
            string category)
        {
            var row = _expeditions.BuildQuestCardRow090(
                campaign, _content, _recruitment);
            var card = row.SingleOrDefault(value =>
                StringComparer.Ordinal.Equals(value.Category, category));
            Assert.That(card, Is.Not.Null,
                "Expected the deterministic row to contain " + category + ".");
            return card;
        }

        private CampaignState CreateActiveFirstHourCampaign090(
            bool withApplicantBoard)
        {
            var campaign = CreateCampaign090();
            if (withApplicantBoard)
                campaign = Require(LegacyApplicantBoardFixture124.Commit(
                    campaign, _recruitmentContent, _content));
            campaign = Require(_expeditions.AcceptContract(
                campaign,
                _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            return Require(_expeditions.StartExpedition(campaign, _content));
        }

        private static CampaignState CreateCampaign090()
        {
            var recruits = Enumerable.Range(1, 10)
                .Select(index => new RecruitState(
                    "R" + index, 100, 100, 20, 20))
                .ToArray();
            var unions = new[]
            {
                new UnionState("U1", "First Union", UnionKind.Normal, "R1",
                    new[] { "R1", "R2", "R3" }, "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Second Union", UnionKind.Normal, "R4",
                    new[] { "R4", "R5", "R6" }, "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED", 30, 7000)
            };
            var guild = new GuildState(
                "GUILD_QUEST_DECK_090", 5000, recruits, unions);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                true,
                "complete");
            var profile = new NewGuildProfileState(
                "Quest Deck Tester",
                SecondDimension.Core.GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            return new CampaignState(
                "00000000-0000-0000-0000-000000090090",
                90090,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                flow);
        }

        private static CampaignState WithQuestRound090(
            CampaignState campaign,
            int round)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var flags = source.ObjectiveFlags
                .Where(value => !(value ?? string.Empty).StartsWith(
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090,
                    StringComparison.Ordinal))
                .Concat(Enumerable.Range(0, round).Select(index =>
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090 +
                    "HISTORY_" + index.ToString("D2")))
                .ToArray();
            var expedition = source.With(
                objectiveFlags: flags,
                lastCheckpointId: "quest_deck_090_round_" + round);
            return WithExpedition090(campaign, expedition);
        }

        private static CampaignState WithExpeditionId090(
            CampaignState campaign,
            string expeditionId)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var expedition = new ExpeditionState017D(
                expeditionId,
                source.ContractCommitId,
                source.BoardId,
                source.CurrentNodeId,
                source.Status,
                source.Supplies,
                source.Fatigue,
                source.Threat,
                source.Urgency,
                source.VisitedNodeIds,
                source.RevealedNodeIds,
                source.CommittedMoveIds,
                source.CommittedChecks,
                source.ObjectiveFlags,
                source.LastCheckpointId);
            return WithExpedition090(campaign, expedition);
        }

        private static CampaignState WithExpedition090(
            CampaignState campaign,
            ExpeditionState017D expedition)
        {
            var city = campaign.Guild.GuildCity.With(
                expedition: expedition,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true);
            return campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        private static CampaignState WithCurrentNode090(
            CampaignState campaign,
            string nodeId,
            IReadOnlyList<string> objectiveFlags)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var expedition = new ExpeditionState017D(
                source.ExpeditionId,
                source.ContractCommitId,
                source.BoardId,
                nodeId,
                ExpeditionStatus017D.Active,
                source.Supplies,
                source.Fatigue,
                source.Threat,
                source.Urgency,
                new[] { nodeId },
                new[] { nodeId, "N10" },
                source.CommittedMoveIds,
                source.CommittedChecks,
                objectiveFlags ?? Array.Empty<string>(),
                "quest_deck_090_node_fixture");
            return WithExpedition090(campaign, expedition);
        }

        private static CampaignState WithRecruitmentBoard090(
            CampaignState campaign,
            ApplicantBoardState board)
        {
            var city = campaign.Guild.GuildCity.With(
                recruitmentBoard: board,
                replaceRecruitmentBoard: true);
            return campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        private static ApplicantSnapshotState CopyApplicant090(
            ApplicantSnapshotState source,
            int signingCostTreasuryXp,
            string scoutingReport)
        {
            return new ApplicantSnapshotState(
                source.Slot,
                source.RecruitId,
                source.DisplayName,
                source.Kind,
                source.SourceSeed,
                source.SignatureId,
                source.RaceId,
                source.WorldId,
                source.ClassTendencyId,
                source.LeadershipBand,
                source.CurrentHp,
                source.MaximumHp,
                source.CurrentMp,
                source.MaximumMp,
                source.VitalsInitialized,
                signingCostTreasuryXp,
                source.PotentialBasisPoints,
                source.CanonicalApplicantJson,
                scoutingReport,
                source.OpeningEquipment,
                source.TutorialAliasId,
                source.AuthoredStableRecruitId,
                source.OpeningLoadout,
                source.LeadershipScore,
                source.TacticalAptitude);
        }

        private static CampaignState WithRosterCount090(
            CampaignState campaign,
            int targetCount)
        {
            var recruits = new List<RecruitState>(campaign.Guild.Recruits);
            for (var ordinal = 0; recruits.Count < targetCount; ordinal++)
            {
                var recruitId = "QUEST090_CAPACITY_MEMBER_" +
                                ordinal.ToString("D3");
                if (recruits.Any(value => StringComparer.Ordinal.Equals(
                        value.RecruitId, recruitId)))
                    continue;
                recruits.Add(new RecruitState(
                    recruitId, 100, 100, 20, 20));
            }
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                campaign.Guild.Development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private CampaignState ResolveBattleVictory090(CampaignState campaign)
        {
            for (var round = 0;
                 round < 100 &&
                 campaign.Battle.Outcome == BattleOutcome.InProgress;
                 round++)
            {
                var active = campaign.Battle.PlayerUnions
                    .Where(value => !value.Retreated && !value.IsDefeated)
                    .ToArray();
                for (var index = 0; index < active.Length; index++)
                {
                    var forecast = campaign.Battle.CommittedForecasts
                                       .FirstOrDefault(value =>
                                           StringComparer.Ordinal.Equals(
                                               value.UnionId,
                                               active[index].UnionId) &&
                                           StringComparer.Ordinal.Equals(
                                               value.CommandId,
                                               "CMD_ALL_OUT")) ??
                                   campaign.Battle.CommittedForecasts.First(
                                       value => StringComparer.Ordinal.Equals(
                                           value.UnionId,
                                           active[index].UnionId));
                    campaign = Require(_battles.SelectForecast(
                        campaign,
                        active[index].UnionId,
                        forecast.ForecastId));
                }
                campaign = Require(_battles.ConfirmRound(
                    campaign, _combatContent));
            }
            Assert.That(campaign.Battle.Outcome,
                Is.EqualTo(BattleOutcome.Victory));
            return campaign;
        }

        private static CampaignState WithObjectiveFlag090(
            CampaignState campaign,
            string flag)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var flags = source.ObjectiveFlags.Concat(new[] { flag }).ToArray();
            return WithExpedition090(campaign, source.With(
                objectiveFlags: flags,
                lastCheckpointId: "quest_deck_090_flag_fixture"));
        }

        private static CampaignState ReplaceActiveRecruitIdentity090(
            CampaignState campaign,
            string sourceRecruitId,
            string replacementRecruitId)
        {
            var recruits = campaign.Guild.Recruits
                .Select(value => StringComparer.Ordinal.Equals(
                        value.RecruitId, sourceRecruitId)
                    ? new RecruitState(replacementRecruitId, 100, 100, 20, 20)
                    : value)
                .ToArray();
            var unions = campaign.Guild.Unions.Select(value =>
                new UnionState(
                    value.UnionId,
                    value.DisplayName,
                    value.Kind,
                    StringComparer.Ordinal.Equals(
                        value.LeaderRecruitId, sourceRecruitId)
                        ? replacementRecruitId
                        : value.LeaderRecruitId,
                    value.MemberRecruitIds.Select(memberId =>
                            StringComparer.Ordinal.Equals(
                                memberId, sourceRecruitId)
                                ? replacementRecruitId
                                : memberId)
                        .ToArray(),
                    value.FormationId,
                    value.DoctrineId,
                    value.SharedAp,
                    value.CohesionBasisPoints))
                .ToArray();
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits,
                unions,
                campaign.Guild.Inventory,
                campaign.Guild.Development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState WithTreasuryXp090(
            CampaignState campaign,
            long treasuryXp)
        {
            var guild = campaign.Guild.With(
                treasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                campaign.Guild.Development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState WithAdventureAuthority090(
            CampaignState campaign,
            string receiptId)
        {
            var development = campaign.Guild.Development
                .RecordAdventureAuthority(receiptId);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState Require(
            SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void DeleteSaveFamily090(string path)
        {
            foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(candidate)) File.Delete(candidate);
        }
    }
}
