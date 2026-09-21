using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed partial class GuildCityRecruitmentService017D
    {
        // Pure preparation inside the existing atomic save transaction. A board
        // contact alone is not an acquisition. Never buy an offer here.
        public Result<CampaignState> ApplyAutomaticEarnedDuplicates136(CampaignState campaign,
            ICampaignRuleCatalog019 chapters, IWorldGateOperationsCatalog023 gates)
        {
            if (campaign?.Guild == null || _heroMasterCatalog089 == null ||
                chapters == null || gates == null ||
                GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign))
                return Result<CampaignState>.Success(campaign);
            var candidate = campaign;
            var sources = EarnedSources094(candidate, chapters, gates, out _);
            foreach (var selected in SelectEarnedRewards094(candidate, sources))
            {
                // New heroes keep the existing reward claim presentation. Only
                // an actual, uniquely owned matching hero can auto-ascend.
                var owners = MatchingEarnedCardOwners099(candidate, selected.Hero);
                if (owners.Length != 1 || owners[0].AuthorityKind != RecruitAuthorityKind.Normal)
                    continue;
                var growth = ClaimEarnedCardDuplicate099(candidate, selected.Source, selected.Hero);
                if (!growth.IsSuccess) return growth;
                candidate = growth.Value;
            }
            var chestCredits = candidate.Guild.Development.AppliedAdventureAuthorityIds
                .Where(value => value.StartsWith(ChestRecruitCreditPrefix092, StringComparison.Ordinal) &&
                    !candidate.Guild.Development.HasAdventureAuthority(ChestRecruitCreditUsedPrefix092 +
                        value.Substring(ChestRecruitCreditPrefix092.Length))).ToArray();
            if (chestCredits.Length == 0) return Result<CampaignState>.Success(candidate);
            foreach (var hero in _heroMasterCatalog089.AcceptedHeroes
                .Where(value => value.IsNormalApplicantEligible && chestCredits.Any(credit =>
                    credit.StartsWith(ChestRecruitCreditPrefix092 + value.StableId + "_", StringComparison.Ordinal)))
                .OrderBy(value => value.StableId, StringComparer.Ordinal))
            {
                var prefix = ChestRecruitCreditPrefix092 + hero.StableId + "_";
                foreach (var credit in chestCredits
                    .Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray())
                {
                    var cardId = credit.Substring(prefix.Length);
                    var used = ChestRecruitCreditUsedPrefix092 + credit.Substring(ChestRecruitCreditPrefix092.Length);
                    var development = candidate.Guild.Development;
                    if (string.IsNullOrWhiteSpace(cardId) || development.HasAdventureAuthority(used) ||
                        !development.HasAdventureAuthority(ChestRecruitReceiptPrefix092 + cardId) ||
                        !development.HasAdventureAuthority(GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + cardId))
                        continue;
                    var owners = MatchingEarnedCardOwners099(candidate, hero);
                    if (owners.Length != 1 || owners[0].AuthorityKind != RecruitAuthorityKind.Normal) continue;
                    var recruits = candidate.Guild.Recruits.ToList();
                    var owned = owners[0];
                    var index = recruits.IndexOf(owned);
                    HeroMasterDuplicateMergeForecast089 growth;
                    try { growth = BuildHeroMasterDuplicateForecast089(owned, index, hero.StableId); }
                    catch (Exception e) { return Result<CampaignState>.Failure("RECRUIT_AUTO136_GROWTH_REQUIRED:" + e.Message); }
                    var receipt = "EARNED_RECRUIT136_CHEST_DUPLICATE|" + credit + "|" + growth.Kind + "|" +
                        CanonicalJson.Sha256Hex(owned.Progression) + "|" + CanonicalJson.Sha256Hex(growth.ProjectedProgression);
                    if (!development.CanRecordAdventureAuthority(used))
                        return Result<CampaignState>.Failure("RECRUIT_AUTO136_LEDGER_FULL");
                    development = development.RecordAdventureAuthority(used);
                    if (!development.CanRecordAdventureAuthority(receipt))
                        return Result<CampaignState>.Failure("RECRUIT_AUTO136_LEDGER_FULL");
                    recruits[index] = owned.WithProgression(growth.ProjectedProgression);
                    var guild = candidate.Guild.With(candidate.Guild.TreasuryXp, recruits.AsReadOnly(),
                        candidate.Guild.Unions, candidate.Guild.Inventory, development.RecordAdventureAuthority(receipt));
                    candidate = ConsumeExpeditionRecruitLead089(candidate.With(guild, candidate.OpeningFlow), hero.StableId);
                }
            }
            return Result<CampaignState>.Success(candidate);
        }
    }
}
