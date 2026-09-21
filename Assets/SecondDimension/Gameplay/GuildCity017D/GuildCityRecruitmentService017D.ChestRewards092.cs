using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed partial class GuildCityRecruitmentService017D
    {
        public const string ChestRecruitReceiptPrefix092 = "CHEST_RECRUIT092_";
        private const string ChestRecruitCreditPrefix092 = "CHEST_RECRUIT_CREDIT092_";
        private const string ChestRecruitCreditUsedPrefix092 = "CHEST_RECRUIT_USED092_";

        public HeroMaster300Hero087 PreviewChestRecruit092(CampaignState campaign, string cardId)
        {
            var runtime = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.WorldGate023;
            if (runtime == null || _heroMasterCatalog089 == null || string.IsNullOrWhiteSpace(cardId) ||
                campaign.Guild.Development.HasAdventureAuthority(ChestRecruitReceiptPrefix092 + cardId))
                return null;
            var hash = CanonicalJson.Sha256Hex(new
            {
                Rule = "CHEST_HIGH_RANK_RECRUIT_092", campaign.CampaignSeed, CardId = cardId
            });
            var roll = Convert.ToInt32(hash.Substring(0, 4), 16) % 1024;
            // A rare 26/1024 S-rank invitation in addition to equipment. SS/SSS
            // code/contract authorities stay exclusive; no quarantined heroes.
            if (roll >= 26) return null;
            var available = _heroMasterCatalog089.AcceptedHeroes
                .Where(hero => hero.Rank == HeroMasterRank087.S && hero.IsNormalApplicantEligible &&
                    !runtime.ExpeditionRecruitLeadIds089.Contains(hero.StableId))
                .OrderBy(hero => hero.StableId, StringComparer.Ordinal).ToArray();
            if (available.Length == 0) return null;
            var selected = available[Convert.ToInt32(hash.Substring(4, 4), 16) % available.Length];
            var development = campaign.Guild.Development;
            var neededReceipts = new[]
            {
                GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + cardId,
                ChestRecruitReceiptPrefix092 + cardId,
                ChestRecruitCreditPrefix092 + selected.StableId + "_" + cardId
            }.Count(id => !development.HasAdventureAuthority(id));
            // A preview must never promise a bonus that the saved receipt ledger
            // cannot hold after the base chest commits.
            return development.AppliedAdventureAuthorityIds.Count + neededReceipts <=
                GuildDevelopmentState.AdventureAuthorityEntryLimit ? selected : null;
        }

        public CampaignState ApplyChestRecruit092(CampaignState campaign, string cardId)
        {
            if (campaign?.Guild == null || string.IsNullOrWhiteSpace(cardId)) return campaign;
            var receipt = ChestRecruitReceiptPrefix092 + cardId;
            var development = campaign.Guild.Development;
            if (!development.HasAdventureAuthority(
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + cardId) ||
                development.HasAdventureAuthority(receipt) ||
                !development.CanRecordAdventureAuthority(receipt)) return campaign;
            var hero = PreviewChestRecruit092(campaign, cardId);
            if (hero == null) return campaign;
            var credit = ChestRecruitCreditPrefix092 + hero.StableId + "_" + cardId;
            var creditedDevelopment = development.RecordAdventureAuthority(receipt);
            if (!creditedDevelopment.CanRecordAdventureAuthority(credit)) return campaign;
            creditedDevelopment = creditedDevelopment.RecordAdventureAuthority(credit);
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var runtime = playable.WorldGate023;
            var leads = runtime.ExpeditionRecruitLeadIds089.Concat(new[] { hero.StableId })
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            runtime = runtime.With(expeditionRecruitLeadIds089: leads);
            playable = playable.With(worldGate023: runtime, replaceWorldGate023: true);
            progress = progress.With(playable020: playable, replacePlayable020: true);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true);
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory,
                creditedDevelopment).WithGuildCity(city);
            // Existing recruitment desk consumes this same earned lead, materializes
            // the exact hero, and owns duplicate/Ascension behavior.
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static string AvailableChestRecruitCredit092(CampaignState campaign, string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId) || !HasExpeditionRecruitLead089(campaign, heroId))
                return null;
            var prefix = ChestRecruitCreditPrefix092 + heroId + "_";
            return campaign.Guild.Development.AppliedAdventureAuthorityIds
                .Where(value => value.StartsWith(prefix, StringComparison.Ordinal) &&
                    !campaign.Guild.Development.HasAdventureAuthority(
                        ChestRecruitCreditUsedPrefix092 + value.Substring(ChestRecruitCreditPrefix092.Length)))
                .OrderBy(value => value, StringComparer.Ordinal).FirstOrDefault();
        }

        public Result<CampaignState> SignApplicant(CampaignState campaign, string recruitId)
        {
            var applicant = campaign?.Guild?.GuildCity?.RecruitmentBoard?.FindApplicant(recruitId);
            var credit = AvailableChestRecruitCredit092(campaign, applicant?.AuthoredStableRecruitId);
            var used = credit == null ? null : ChestRecruitCreditUsedPrefix092 +
                credit.Substring(ChestRecruitCreditPrefix092.Length);
            if (used != null && !campaign.Guild.Development.CanRecordAdventureAuthority(used))
                return Result<CampaignState>.Failure("CHEST_RECRUIT092_RECEIPT_CAPACITY_REQUIRED");
            var result = SignApplicantCore092(campaign, recruitId);
            if (!result.IsSuccess || used == null) return result;
            var state = result.Value;
            var guild = state.Guild.With(state.Guild.TreasuryXp, state.Guild.Recruits,
                state.Guild.Unions, state.Guild.Inventory,
                state.Guild.Development.RecordAdventureAuthority(used));
            return Result<CampaignState>.Success(state.With(guild, state.OpeningFlow));
        }
    }
}
