using System;
using System.Globalization;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed class TowerHeroRewardResult094
    {
        public int ActualFloor { get; internal set; }
        public string Tier { get; internal set; }
        public bool Guaranteed { get; internal set; }
        public string SelectedHeroId { get; internal set; }
        public string HeroDisplayName { get; internal set; }
        public string EventId { get; internal set; }
        public bool WinsHero => !string.IsNullOrWhiteSpace(SelectedHeroId);
    }

    public sealed partial class GuildCityRecruitmentService017D
    {
        public const string TowerHeroEventPrefix094 = "TOWER_HERO094_EVENT_";
        public const string TowerHeroResultPrefix094 = "TOWER_HERO094_RESULT|";

        internal Result<CampaignState> ApplyTowerFloorReward094(CampaignState campaign,
            ICampaignRegistry022 registry, string completionReceipt, int actualFloor)
        {
            if (!TowerHeroRewardRules094.IsRewardFloor(actualFloor) ||
                !CampaignProgressionCommandService022.HasVerifiedTowerFloorCompletion094(
                    campaign, registry, completionReceipt, actualFloor))
                return Result<CampaignState>.Failure("TOWER094_CERTIFIED_FLOOR_COMPLETION_REQUIRED");
            var eventId = TowerHeroEventId094(campaign, completionReceipt, actualFloor);
            var resultPrefix = TowerHeroResultPrefix094 + eventId + "|";
            var development = campaign.Guild.Development;
            var priorResults = development.AppliedAdventureAuthorityIds.Where(value =>
                value.StartsWith(resultPrefix, StringComparison.Ordinal)).Take(2).ToArray();
            if (development.HasAdventureAuthority(eventId))
                return priorResults.Length == 1 && ValidSavedTowerResult094(
                    campaign, priorResults[0], eventId, completionReceipt, actualFloor)
                    ? Result<CampaignState>.Success(campaign)
                    : Result<CampaignState>.Failure("TOWER094_SAVED_REWARD_INCONSISTENT");
            if (priorResults.Length != 0)
                return Result<CampaignState>.Failure("TOWER094_SAVED_REWARD_INCONSISTENT");
            TowerHeroRewardPlan094 plan;
            try
            {
                plan = TowerHeroRewardRules094.BuildPlan(campaign, completionReceipt,
                    actualFloor, _heroMasterCatalog089);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("TOWER094_REWARD_AUTHORITY_UNAVAILABLE:" + exception.Message);
            }
            var payload = eventId + "|" + actualFloor.ToString(CultureInfo.InvariantCulture) + "|" +
                plan.Tier + "|" + (plan.Guaranteed ? "GUARANTEED" : "CHANCE") + "|" +
                plan.ChanceBasisPoints.ToString(CultureInfo.InvariantCulture) + "|" +
                plan.SavedRoll0To9999.ToString(CultureInfo.InvariantCulture) + "|" +
                plan.SelectionIndex.ToString(CultureInfo.InvariantCulture) + "|" +
                plan.CandidatePoolHash + "|" + (plan.WinsHero ? plan.SelectedHeroId : "NO_HERO") + "|" + plan.PlanId +
                "|" + string.Join(",", plan.CandidateHeroIds);
            var result = TowerHeroResultPrefix094 + payload + "|" + CanonicalJson.Sha256Hex(payload);
            if (!development.CanRecordAdventureAuthority(eventId) ||
                !development.RecordAdventureAuthority(eventId).CanRecordAdventureAuthority(result))
                return Result<CampaignState>.Failure("TOWER094_REWARD_LEDGER_FULL");
            var candidate = campaign;
            if (plan.WinsHero)
            {
                Result<CampaignState> grant;
                try { grant = ApplyTowerHero094(candidate, plan, eventId); }
                catch (Exception exception)
                {
                    return Result<CampaignState>.Failure("TOWER094_HERO_GROWTH_AUTHORITY_REQUIRED:" + exception.Message);
                }
                if (!grant.IsSuccess) return grant;
                candidate = grant.Value;
            }
            // A miss is a saved outcome too. Normal XP/loot and the completed
            // battle are untouched, and the same event is never rolled again.
            var guild = candidate.Guild.With(candidate.Guild.TreasuryXp, candidate.Guild.Recruits,
                candidate.Guild.Unions, candidate.Guild.Inventory,
                candidate.Guild.Development.RecordAdventureAuthority(eventId).RecordAdventureAuthority(result));
            return Result<CampaignState>.Success(candidate.With(guild, candidate.OpeningFlow));
        }

        static string TowerHeroEventId094(CampaignState campaign, string completionReceipt, int actualFloor) =>
            TowerHeroEventPrefix094 + CanonicalJson.Sha256Hex(new {
                Policy = TowerHeroRewardRules094.PolicyVersion, campaign.CampaignGuid,
                CompletionReceiptId = completionReceipt, ActualFloor = actualFloor }).ToUpperInvariant();

        // Read-only UI authority: only the latest cleared floor can expose a
        // reward recap. It reads the saved exact pool/result, never BuildPlan.
        public Result<TowerHeroRewardResult094> DescribeLatestTowerHeroReward094(
            CampaignState campaign, ICampaignRegistry022 registry)
        {
            var floors = CampaignProgressionCommandService022.DescribeTowerFloors094(campaign, registry);
            if (!floors.IsSuccess) return Result<TowerHeroRewardResult094>.Failure(floors.Errors.ToArray());
            var actualFloor = floors.Value.LatestCompletedActualFloor130;
            if (!TowerHeroRewardRules094.IsRewardFloor(actualFloor))
                return Result<TowerHeroRewardResult094>.Success(null);
            var state = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
            var latest = state.AbyssAuthorityEntries.LastOrDefault(entry => !entry.Aborted);
            if (latest == null || !CampaignProgressionCommandService022.IsEndlessTowerOperation094(latest.OperationInstanceId))
                return Result<TowerHeroRewardResult094>.Success(null); // historical policy, not a new free hero
            var receipt = latest.CompletionProof?.CompletionReceipt?.ReceiptId;
            if (!CampaignProgressionCommandService022.HasVerifiedTowerFloorCompletion094(campaign, registry, receipt, actualFloor))
                return Result<TowerHeroRewardResult094>.Failure("TOWER094_CERTIFIED_FLOOR_COMPLETION_REQUIRED");
            var eventId = TowerHeroEventId094(campaign, receipt, actualFloor);
            var records = campaign.Guild.Development.AppliedAdventureAuthorityIds.Where(value =>
                value.StartsWith(TowerHeroResultPrefix094 + eventId + "|", StringComparison.Ordinal)).Take(2).ToArray();
            if (!campaign.Guild.Development.HasAdventureAuthority(eventId) || records.Length != 1 ||
                !ValidSavedTowerResult094(campaign, records[0], eventId, receipt, actualFloor))
                return Result<TowerHeroRewardResult094>.Failure("TOWER094_SAVED_REWARD_OR_TRUSTED_HERO_IDENTITY_INVALID");
            var fields = records[0].Substring(TowerHeroResultPrefix094.Length).Split('|');
            var heroId = fields[8] == "NO_HERO" ? string.Empty : fields[8];
            var name = string.Empty;
            if (!string.IsNullOrEmpty(heroId))
            {
                if (fields[2] == "SSS") name = SssTenV4Roster090.Get(heroId).DisplayName;
                else if (_heroMasterCatalog089.TryGetAcceptedHero(heroId, out var hero)) name = hero.Name;
            }
            return Result<TowerHeroRewardResult094>.Success(new TowerHeroRewardResult094 {
                ActualFloor = actualFloor, Tier = fields[2], Guaranteed = fields[3] == "GUARANTEED",
                SelectedHeroId = heroId, HeroDisplayName = name, EventId = eventId });
        }

        bool ValidSavedTowerResult094(CampaignState campaign, string record,
            string eventId, string completionReceipt, int actualFloor)
        {
            var parts = record.Substring(TowerHeroResultPrefix094.Length).Split('|');
            if (parts.Length != 12 || parts[0] != eventId ||
                parts[1] != actualFloor.ToString(CultureInfo.InvariantCulture) ||
                parts[2] != TowerHeroRewardRules094.TierForFloor(actualFloor) ||
                parts[3] != (TowerHeroRewardRules094.IsGuaranteedFloor(actualFloor) ? "GUARANTEED" : "CHANCE") ||
                !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var chance) ||
                !int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out var roll) ||
                !int.TryParse(parts[6], NumberStyles.None, CultureInfo.InvariantCulture, out var selection) ||
                chance != (TowerHeroRewardRules094.IsGuaranteedFloor(actualFloor) ? 10000 : TowerHeroRewardRules094.BonusChanceBasisPoints) ||
                roll < 0 || roll >= 10000 || selection < 0 || !Sha256Text094(parts[7]) ||
                string.IsNullOrWhiteSpace(parts[8]) || !parts[9].StartsWith("TOWER_HERO094_", StringComparison.Ordinal) ||
                !Sha256Text094(parts[9].Substring("TOWER_HERO094_".Length)) ||
                !Sha256Text094(parts[11]) || ((roll < chance) != (parts[8] != "NO_HERO"))) return false;
            var pool = parts[10].Split(',');
            if (pool.Length == 0 || selection >= pool.Length || pool.Any(string.IsNullOrWhiteSpace) ||
                !pool.SequenceEqual(pool.OrderBy(id => id, StringComparer.Ordinal)) ||
                pool.Distinct(StringComparer.Ordinal).Count() != pool.Length) return false;
            // Trust the recorded pool, not the current pool order/membership. Its
            // identities must still be legal in the loaded trusted authority.
            if (parts[2] == "SSS")
            {
                if (pool.Length != 10 || !pool.SequenceEqual(SssTenV4Roster090.All
                        .Select(hero => hero.HeroId).OrderBy(id => id, StringComparer.Ordinal))) return false;
            }
            else if (_heroMasterCatalog089 == null || pool.Any(id =>
                !_heroMasterCatalog089.TryGetAcceptedHero(id, out var hero) || hero.Rank != HeroMasterRank087.SS)) return false;
            // Verify the saved roll using its saved pool size and stable floor
            // salt. This never selects from a newer catalog pool or grants again.
            SssTenV4AcquisitionService090.TowerOfferRoll090(campaign.CampaignSeed, campaign.CampaignGuid,
                TowerHeroRewardRules094.RollIdentity094(campaign, actualFloor), actualFloor,
                pool.Length, out var expectedRoll, out var expectedSelection);
            if (roll != expectedRoll || selection != expectedSelection) return false;
            var selected = roll < chance ? pool[selection] : string.Empty;
            if (parts[8] != (string.IsNullOrEmpty(selected) ? "NO_HERO" : selected) ||
                parts[7] != CanonicalJson.Sha256Hex(new { Tier = parts[2], Heroes = pool }) ||
                parts[9] != TowerHeroRewardRules094.PlanId094(campaign, completionReceipt, actualFloor,
                    parts[2], parts[3] == "GUARANTEED", chance, roll, selection, parts[7], selected)) return false;
            var payload = string.Join("|", parts.Take(11));
            return parts[11] == CanonicalJson.Sha256Hex(payload);
        }

        static bool Sha256Text094(string value) => value != null && value.Length == 64 &&
            value.All(character => (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F'));

        Result<CampaignState> ApplyTowerHero094(CampaignState campaign, TowerHeroRewardPlan094 plan, string eventId)
        {
            RecruitState owned;
            if (plan.Tier == "SSS")
            {
                if (!SssTenV4Roster090.TryGet(plan.SelectedHeroId, out var sss) || sss.HeroId != plan.SelectedHeroId)
                    return Result<CampaignState>.Failure("TOWER094_EXACT_SSS_IDENTITY_REQUIRED");
                owned = SssTenV4Roster090.FindOwned(campaign.Guild.Recruits, sss.HeroId);
                if (owned == null)
                    return SssTenV4HostRewards090.GrantRecruit(campaign, sss.HeroId, eventId);
            }
            else
            {
                if (_heroMasterCatalog089 == null ||
                    !_heroMasterCatalog089.TryGetAcceptedHero(plan.SelectedHeroId, out var hero) ||
                    hero.Rank != HeroMasterRank087.SS)
                    return Result<CampaignState>.Failure("TOWER094_EXACT_ACCEPTED_SS_REQUIRED");
                owned = campaign.Guild.Recruits.FirstOrDefault(recruit =>
                    HeroMaster300CreatorRecruitProjection087.RosterContainsHero(new[] { recruit }, hero) ||
                    HeroMaster300ApplicantLead089.RosterContains(new[] { recruit }, hero));
                if (owned == null)
                {
                    var projected = HeroMaster300CreatorRecruitProjection087.FromHero(hero);
                    var newSsGrant = new CreatorRecruitGrant028(_autoGeneration.InitializeRecruit(projected.Recruit),
                        projected.InventoryItems);
                    return new CreatorAccessCommandService028().GrantCharacterReward(campaign,
                        eventId, hero.StableId, newSsGrant);
                }
            }
            if (owned.AuthorityKind != RecruitAuthorityKind.Normal)
                return Result<CampaignState>.Failure("TOWER094_NORMAL_RECRUIT_REQUIRED");
            var recruits = campaign.Guild.Recruits.ToList();
            var index = recruits.IndexOf(owned);
            // Reuse the existing duplicate authority. SSS learned Arts are legal,
            // but a SSS identity is not a HeroMaster generated-tree profile.
            var duplicate = BuildHeroMasterDuplicateForecast089(owned, index, plan.SelectedHeroId,
                allowGeneratedTreeUnlocks094: plan.Tier != "SSS");
            recruits[index] = owned.WithProgression(duplicate.ProjectedProgression);
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, recruits.AsReadOnly(),
                campaign.Guild.Unions, campaign.Guild.Inventory, campaign.Guild.Development);
            return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
        }
    }
}


