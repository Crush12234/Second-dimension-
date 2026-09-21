#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Creator028;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class ExpeditionDeck089Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void CampaignRecruitChance094ActualDiceWinEarnsInvitationLossDoesNot(bool win)
        {
            CampaignState committed = null;
            ExpeditionRouteCardState089 selected = null;
            ExpeditionCardReceipt089 receipt = null;
            for (var seed = 94100; seed < 94356 && committed == null; seed++)
            {
                var candidate = CreateAtWorldBoard("CH018_001", "SKYHOME", seed);
                candidate = Require(_worldGate.BeginOperation(candidate, _catalog023,
                    "CH018_001", new[] { "DECK_UNION_089" }, _catalog020,
                    HeroMaster300CreatorRegistry087.Load().Source));
                var encounter = WorldGate(candidate).ActiveOperation.ExpeditionDeck089.CurrentRow
                    .FirstOrDefault(value => !ExpeditionDeckService089.IsOptionalBattleCard089(value)
                        && value.TreasuryXpCost == 0);
                if (encounter == null) continue;
                candidate = Require(_commands.CommitRouteCard(candidate, _catalog023, encounter.CardId,
                    "DECK_RECRUIT_A_089", "DECK_RECRUIT_B_089"));
                var sealedEffect132 = WorldGate(candidate).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                if (sealedEffect132?.Effect132 != null && !sealedEffect132.Effect132.IsRolled132)
                    candidate = Require(_commands.RollCommittedEffect132(candidate, _catalog023,
                        sealedEffect132.ReceiptId));
                candidate = Require(_commands.ApplyWorldGateReceiptExactlyOnce(candidate, _catalog023));
                var card = WorldGate(candidate).ActiveOperation.ExpeditionDeck089.CurrentRow
                    .FirstOrDefault(GuildCityRecruitmentService017D.IsFreeRecruitChanceCard094);
                if (card == null) continue;
                candidate = Require(_commands.CommitRouteCard(candidate, _catalog023, card.CardId,
                    "DECK_RECRUIT_A_089", "DECK_RECRUIT_B_089"));
                var pending = WorldGate(candidate).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                if (!string.IsNullOrWhiteSpace(pending.RecruitStableId) != win) continue;
                committed = candidate; selected = card; receipt = pending;
            }
            Assert.That(committed, Is.Not.Null, "Bounded real dice sample must include " + (win ? "win" : "loss"));
            var oldCount = committed.Guild.Recruits.Count;
            var oldXp = committed.Guild.TreasuryXp;
            var prefix = "EARNED_RECRUIT094_CARD|" + receipt.ReceiptId + "|";
            Assert.That(committed.Guild.Development.AppliedAdventureAuthorityIds.Any(value => value.StartsWith(prefix)), Is.False);
            if (win)
                Assert.That(GuildCityRecruitmentService017D.RecordEarnedCardRecruit094(
                    committed, selected, receipt).IsSuccess, Is.False,
                    "Committed dice alone are not an applied card reward.");
            var earned = Require(_commands.ApplyWorldGateReceiptExactlyOnce(committed, _catalog023));
            Assert.That(earned.Guild.Development.AppliedAdventureAuthorityIds.Count(value => value.StartsWith(prefix)),
                Is.EqualTo(win ? 1 : 0));
            Assert.That(earned.Guild.Recruits.Count, Is.EqualTo(oldCount), "No roster mutation inside an active board.");
            Assert.That(earned.Guild.TreasuryXp, Is.GreaterThanOrEqualTo(oldXp), "A free invitation cannot debit XP.");
            var replay = Require(GuildCityRecruitmentService017D.RecordEarnedCardRecruit094(earned, selected, receipt));
            Assert.That(CanonicalJson.Sha256Hex(replay), Is.EqualTo(CanonicalJson.Sha256Hex(earned)));
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(earned));
            Assert.That(reloaded.Guild.Development.AppliedAdventureAuthorityIds.Count(value => value.StartsWith(prefix)),
                Is.EqualTo(win ? 1 : 0));
        }
    }
}
#endif
