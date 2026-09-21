using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed partial class GuildCityRecruitmentService017D
    {
        const string EarnedDuplicatePrefix099 = "EARNED_RECRUIT099_DUPLICATE|";

        List<SelectedEarnedReward094> SelectEarnedRewards099(CampaignState campaign,
            IEnumerable<EarnedSource094> sources)
        {
            var result = new List<SelectedEarnedReward094>();
            var remaining = _heroMasterCatalog089.AcceptedHeroes.Where(hero =>
                hero.IsNormalApplicantEligible &&
                !HeroMaster300ApplicantLead089.RosterContains(campaign.Guild.Recruits, hero)).ToList();
            var selectedIdentities = new HashSet<string>(StringComparer.Ordinal);
            var newMembers = 0;
            foreach (var source in sources.OrderByDescending(value => value.IsCard))
            {
                HeroMaster300Hero087 hero;
                var duplicate = false;
                if (source.IsCard && !string.IsNullOrWhiteSpace(source.ExactHeroId))
                {
                    if (!_heroMasterCatalog089.TryGetAcceptedHero(source.ExactHeroId, out hero) ||
                        !hero.IsNormalApplicantEligible) continue;
                    var matches = MatchingEarnedCardOwners099(campaign, hero);
                    if (matches.Length > 1 || (matches.Length == 1 &&
                        matches[0].AuthorityKind != RecruitAuthorityKind.Normal)) continue;
                    duplicate = matches.Length == 1 || selectedIdentities.Contains(hero.StableId);
                }
                else
                {
                    // Preserve distinct new chapter recruits while any remain. Once
                    // the accepted pool is owned, each still-unclaimed chapter
                    // slot keeps its exact receipt and grants duplicate growth.
                    hero = remaining.OrderBy(value => CanonicalJson.Sha256Hex(new {
                        Rule = "EARNED_CAMPAIGN_RECRUIT_094", campaign.CampaignGuid,
                        campaign.CampaignSeed, source.SourceId, value.StableId }),
                        StringComparer.Ordinal).FirstOrDefault();
                    if (hero == null)
                    {
                        hero = _heroMasterCatalog089.AcceptedHeroes.Where(value =>
                            value.IsNormalApplicantEligible &&
                            MatchingEarnedCardOwners099(campaign, value).Length == 1 &&
                            MatchingEarnedCardOwners099(campaign, value)[0].AuthorityKind == RecruitAuthorityKind.Normal)
                            .OrderBy(value => CanonicalJson.Sha256Hex(new {
                                Rule = "EARNED_CAMPAIGN_RECRUIT_094", campaign.CampaignGuid,
                                campaign.CampaignSeed, source.SourceId, value.StableId }),
                                StringComparer.Ordinal).FirstOrDefault();
                        duplicate = hero != null;
                    }
                }
                if (hero == null) continue;
                if (!duplicate && newMembers + campaign.Guild.Recruits.Count >= NormalHeroAuthorityCapacity094)
                    continue; // Later exact duplicates need no new roster slot.
                result.Add(new SelectedEarnedReward094 {
                    Source = source, Hero = hero, IsDuplicate099 = duplicate });
                selectedIdentities.Add(hero.StableId);
                remaining.Remove(hero);
                if (!duplicate) newMembers++;
            }
            return result;
        }

        static RecruitState[] MatchingEarnedCardOwners099(CampaignState campaign, HeroMaster300Hero087 hero) =>
            campaign.Guild.Recruits.Where(recruit =>
                HeroMaster300ApplicantLead089.RosterContains(new[] { recruit }, hero)).Take(2).ToArray();

        string EarnedCardRewardSummary099(CampaignState campaign, SelectedEarnedReward094 selected)
        {
            if (!selected.IsDuplicate099) return "New recruit • no XP cost";
            var matches = MatchingEarnedCardOwners099(campaign, selected.Hero);
            if (matches.Length != 1) return "Duplicate hero • Ascension / Art growth";
            return BuildHeroMasterDuplicateForecast089(matches[0],
                campaign.Guild.Recruits.ToList().IndexOf(matches[0]), selected.Hero.StableId).Summary;
        }

        Result<CampaignState> ClaimEarnedCardDuplicate099(CampaignState campaign,
            EarnedSource094 source, HeroMaster300Hero087 hero)
        {
            // This private adapter is called only from verified earned sources
            // at a safe boundary: exact card wins or owed chapter slots after
            // the normal unique pool is exhausted. No public arbitrary IDs.
            if ((source.IsCard && source.ExactHeroId != hero.StableId) || !hero.IsNormalApplicantEligible)
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT099_EXACT_CARD_REQUIRED");
            var claim = ClaimAuthority094(source.SourceId);
            var development = campaign.Guild.Development;
            if (development.HasAdventureAuthority(claim)) return Result<CampaignState>.Success(campaign);
            var matches = MatchingEarnedCardOwners099(campaign, hero);
            if (matches.Length != 1 || matches[0].AuthorityKind != RecruitAuthorityKind.Normal)
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT099_EXACT_NORMAL_OWNER_REQUIRED");
            var owned = matches[0];
            var recruits = campaign.Guild.Recruits.ToList();
            var index = recruits.IndexOf(owned);
            HeroMasterDuplicateMergeForecast089 growth;
            try { growth = BuildHeroMasterDuplicateForecast089(owned, index, hero.StableId); }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT099_GROWTH_AUTHORITY_REQUIRED:" + exception.Message);
            }
            var receipt = EarnedDuplicatePrefix099 + claim + "|" + hero.StableId + "|" + growth.Kind + "|" +
                CanonicalJson.Sha256Hex(owned.Progression) + "|" + CanonicalJson.Sha256Hex(growth.ProjectedProgression);
            if (!development.CanRecordAdventureAuthority(claim))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_LEDGER_FULL");
            development = development.RecordAdventureAuthority(claim);
            if (!development.CanRecordAdventureAuthority(receipt))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_LEDGER_FULL");
            recruits[index] = owned.WithProgression(growth.ProjectedProgression);
            // No Creator new-body grant and no EARNED_RECRUIT094_HERO record:
            // duplicate growth must not mint inventory, XP, bodies or housing.
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, recruits.AsReadOnly(),
                campaign.Guild.Unions, campaign.Guild.Inventory, development.RecordAdventureAuthority(receipt));
            var merged = campaign.With(guild, campaign.OpeningFlow);
            return Result<CampaignState>.Success(source.IsCard
                ? ConsumeExpeditionRecruitLead089(merged, hero.StableId) : merged);
        }

        public static IReadOnlyDictionary<string, string> ClaimedCardDuplicateHeroIds099(GuildDevelopmentState development) =>
            (development?.AppliedAdventureAuthorityIds ?? Array.Empty<string>()).Where(value =>
                value != null && value.StartsWith(EarnedDuplicatePrefix099, StringComparison.Ordinal))
                .Select(value => value.Substring(EarnedDuplicatePrefix099.Length).Split('|'))
                .Where(parts => parts.Length == 5 && parts[0].StartsWith(EarnedClaimPrefix094, StringComparison.Ordinal) &&
                    development.HasAdventureAuthority(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]) &&
                    Enum.TryParse<HeroMasterDuplicateMergeKind089>(parts[2], out var kind) &&
                    kind != HeroMasterDuplicateMergeKind089.None && Enum.IsDefined(typeof(HeroMasterDuplicateMergeKind089), kind) &&
                    parts[3].Length == 64 && parts[3].All(Uri.IsHexDigit) &&
                    parts[4].Length == 64 && parts[4].All(Uri.IsHexDigit))
                .GroupBy(parts => parts[0], StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single()[1], StringComparer.Ordinal);

        public static int ClaimedCardDuplicateCount099(GuildDevelopmentState development) =>
            ClaimedCardDuplicateHeroIds099(development).Count;
    }
}
