using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed class EarnedCampaignRecruitPreview094
    {
        public string StableId { get; internal set; }
        public string Name { get; internal set; }
        public string Rarity { get; internal set; }
        public string Race { get; internal set; }
        public string Role { get; internal set; }
        public string SourceLabel { get; internal set; }
        public bool IsDuplicate099 { get; internal set; }
        public string RewardSummary099 { get; internal set; }
    }

    public sealed class EarnedCampaignRecruitsView094
    {
        public int PendingChapterRecruits { get; internal set; }
        public int PendingOpeningStoryRecruits { get; internal set; }
        public int PendingCardRecruits { get; internal set; }
        public int BlockedChapterRecruits { get; internal set; }
        public int AvailableUniqueHeroes { get; internal set; }
        public int ClaimableCount { get; internal set; }
        public int ClaimableDuplicateCards099 { get; internal set; }
        public bool CanClaim { get; internal set; }
        public string Summary { get; internal set; } = string.Empty;
        public int PendingCount => PendingChapterRecruits + PendingOpeningStoryRecruits + PendingCardRecruits;
        public IReadOnlyList<EarnedCampaignRecruitPreview094> Preview { get; internal set; } =
            Array.Empty<EarnedCampaignRecruitPreview094>();
    }

    public sealed partial class GuildCityRecruitmentService017D
    {
        const string EarnedCardPrefix094 = "EARNED_RECRUIT094_CARD|";
        const string EarnedClaimPrefix094 = "EARNED_RECRUIT094_CLAIM_";
        const string EarnedIdentityPrefix094 = "EARNED_RECRUIT094_HERO|";
        const int NormalHeroAuthorityCapacity094 = 300;

        sealed class EarnedSource094
        {
            public string SourceId;
            public string WorldId;
            public string ExactHeroId;
            public bool IsCard;
            public bool IsOpening;
        }

        sealed class SelectedEarnedReward094
        {
            public EarnedSource094 Source;
            public HeroMaster300Hero087 Hero;
            public bool IsDuplicate099;
        }

        List<SelectedEarnedReward094> SelectEarnedRewards094(CampaignState campaign,
            IEnumerable<EarnedSource094> sources)
        {
            return SelectEarnedRewards099(campaign, sources);
        }

        /// <summary>
        /// The old save keeps the complete WorldGate chapter proof and chain, but
        /// not the chapter association of each historical Campaign019 receipt.
        /// Require those real proofs plus completed019 and receipt-ledger
        /// consistency; never reconstruct an unavailable receipt or trust flags.
        /// Insufficient legacy proof remains a visible pending/blocked reward.
        /// </summary>
        List<EarnedSource094> EarnedSources094(CampaignState campaign,
            ICampaignRuleCatalog019 chapters, IWorldGateOperationsCatalog023 gates,
            out int blocked)
        {
            blocked = 0;
            var result = new List<EarnedSource094>();
            var progress = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            if (campaign?.Guild == null || _heroMasterCatalog089 == null) return result;
            var runtime = progress?.Playable020?.WorldGate023;
            var development = campaign.Guild.Development;
            var completed = (progress?.CompletedChapterIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var validReceipts = progress != null && progress.AppliedReceiptIds.Count >= completed.Length &&
                progress.AppliedReceiptIds.All(development.HasClaimedReward);
            var validProofs = runtime != null && gates != null &&
                CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                    campaign, gates, runtime);
            foreach (var contract in OpeningStoryContracts094)
            {
                if (!HasVerifiedOpeningStoryReward094(campaign, contract)) continue;
                for (var slot = 0; slot < 3; slot++)
                {
                    var sourceId = "OPENING:" + contract + ":" + slot;
                    if (!development.HasAdventureAuthority(ClaimAuthority094(sourceId)))
                        result.Add(new EarnedSource094 { SourceId = sourceId,
                            WorldId = "SKYHOME", IsOpening = true });
                }
            }
            foreach (var chapterId in completed)
            {
                var proof = runtime?.CompletionProofs.FirstOrDefault(value =>
                    value != null && value.DefinitionId == chapterId);
                var valid = validReceipts && validProofs && proof != null &&
                    runtime.CompletedDefinitionIds.Contains(chapterId) &&
                    chapters != null && chapters.TryGetChapter(chapterId, out _);
                for (var slot = 0; slot < 3; slot++)
                {
                    var sourceId = "CHAPTER:" + chapterId + ":" + slot;
                    if (development.HasAdventureAuthority(ClaimAuthority094(sourceId)))
                        continue;
                    if (!valid) { blocked++; continue; }
                    result.Add(new EarnedSource094 { SourceId = sourceId,
                        WorldId = proof.WorldId });
                }
            }
            foreach (var entry in development.AppliedAdventureAuthorityIds)
            {
                if (!entry.StartsWith(EarnedCardPrefix094, StringComparison.Ordinal))
                    continue;
                var parts = entry.Substring(EarnedCardPrefix094.Length).Split('|');
                if (parts.Length != 3 || !development.HasAdventureAuthority(parts[0]) ||
                    !_heroMasterCatalog089.TryGetAcceptedHero(parts[1], out var hero) ||
                    !hero.IsNormalApplicantEligible) continue;
                var source = "CARD:" + parts[0];
                if (development.HasAdventureAuthority(ClaimAuthority094(source))) continue;
                result.Add(new EarnedSource094 { SourceId = source,
                    WorldId = parts[2], ExactHeroId = hero.StableId, IsCard = true });
            }
            return result.GroupBy(value => value.SourceId, StringComparer.Ordinal)
                .Select(value => value.First()).ToList();
        }

        public EarnedCampaignRecruitsView094 DescribeEarnedCampaignRecruits094(
            CampaignState campaign, ICampaignRuleCatalog019 chapters,
            IWorldGateOperationsCatalog023 gates)
        {
            var view = new EarnedCampaignRecruitsView094();
            if (_heroMasterCatalog089 == null || campaign?.Guild == null) return view;
            var sources = EarnedSources094(campaign, chapters, gates, out var blocked);
            view.PendingChapterRecruits = sources.Count(value => !value.IsCard && !value.IsOpening);
            view.PendingOpeningStoryRecruits = sources.Count(value => value.IsOpening);
            view.PendingCardRecruits = sources.Count(value => value.IsCard);
            view.BlockedChapterRecruits = blocked;
            view.AvailableUniqueHeroes = _heroMasterCatalog089.AcceptedHeroes.Count(hero =>
                hero.IsNormalApplicantEligible &&
                !HeroMaster300ApplicantLead089.RosterContains(campaign.Guild.Recruits, hero));
            var safe = !GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign);
            var selected = SelectEarnedRewards094(campaign, sources);
            view.ClaimableCount = selected.Count;
            view.ClaimableDuplicateCards099 = selected.Count(value => value.IsDuplicate099);
            view.Preview = selected.Take(3).Select(value => new EarnedCampaignRecruitPreview094 {
                StableId = value.Hero.StableId, Name = value.Hero.Name,
                Rarity = value.Hero.Rank.ToString(), Race = value.Hero.Race,
                IsDuplicate099 = value.IsDuplicate099,
                RewardSummary099 = EarnedCardRewardSummary099(campaign, value),
                Role = value.Hero.Role, SourceLabel = value.Source.IsCard
                    ? "Quest-card reward" : value.Source.IsOpening
                        ? "Opening story reward" : "Chapter " + value.Source.SourceId.Split(':')[1]
            }).ToArray();
            view.CanClaim = safe && selected.Count > 0;
            view.Summary = view.PendingCount > 0
                ? view.PendingCount + " earned recruits • no XP cost" +
                  (!safe ? " • claim after this adventure" :
                   view.AvailableUniqueHeroes == 0 && selected.Count == 0 ? " • all eligible heroes owned; rewards kept" :
                   campaign.Guild.Recruits.Count >= NormalHeroAuthorityCapacity094 && selected.Count == 0
                       ? " • roster full; rewards kept" : string.Empty)
                : "Three new recruits earned per completed chapter";
            if (blocked > 0) view.Summary += " • " + blocked +
                " older rewards need their verified chapter record; rewards kept";
            return view;
        }

        public Result<CampaignState> ClaimEarnedCampaignRecruits094(
            CampaignState campaign, ICampaignRuleCatalog019 chapters,
            IWorldGateOperationsCatalog023 gates)
        {
            if (campaign?.Guild == null || _heroMasterCatalog089 == null ||
                chapters == null || gates == null)
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_CONTEXT_REQUIRED");
            if (GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_FINISH_ADVENTURE_FIRST");
            var sources = EarnedSources094(campaign, chapters, gates, out var blocked);
            if (sources.Count == 0)
                return blocked > 0
                    ? Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_VERIFIED_CHAPTER_PROOF_REQUIRED")
                    : Result<CampaignState>.Success(campaign);
            var candidate = campaign;
            // Exact card identities get first claim so random chapter rewards do
            // not consume a promised named hero. All claims remain atomic.
            foreach (var selected in SelectEarnedRewards094(campaign, sources))
            {
                var source = selected.Source;
                var hero = selected.Hero;
                if (HeroMaster300ApplicantLead089.RosterContains(candidate.Guild.Recruits, hero))
                {
                    var merged = ClaimEarnedCardDuplicate099(candidate, source, hero);
                    if (!merged.IsSuccess) return merged;
                    candidate = merged.Value;
                    continue;
                }
                var claim = ClaimAuthority094(source.SourceId);
                var identity = EarnedIdentityPrefix094 + claim + "|" + hero.StableId;
                var development = candidate.Guild.Development;
                if (!development.CanRecordAdventureAuthority(claim))
                    return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_LEDGER_FULL");
                development = development.RecordAdventureAuthority(claim);
                if (!development.CanRecordAdventureAuthority(identity))
                    return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_LEDGER_FULL");
                var applicant = HeroMaster300ApplicantLead089.ToApplicant(hero, 1, source.WorldId);
                var recruit = MaterializeApplicant094(applicant);
                var grant = new CreatorAccessCommandService028().GrantCharacterReward(
                    candidate, claim, hero.StableId,
                    new CreatorRecruitGrant028(recruit, ApplicantInventory094(applicant)));
                if (!grant.IsSuccess) return grant;
                if (grant.Value.Guild.Recruits.Count != candidate.Guild.Recruits.Count + 1 ||
                    !HeroMaster300ApplicantLead089.RosterContains(grant.Value.Guild.Recruits, hero))
                    return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_NEW_IDENTITY_REQUIRED");
                candidate = grant.Value;
                development = candidate.Guild.Development
                    .RecordAdventureAuthority(claim).RecordAdventureAuthority(identity);
                candidate = WithRecruitRewardDevelopment094(candidate, development);
                if (source.IsCard) candidate = ConsumeExpeditionRecruitLead089(candidate, hero.StableId);
            }
            return Result<CampaignState>.Success(candidate);
        }

        RecruitState MaterializeApplicant094(ApplicantSnapshotState applicant)
        {
            var recruit = new RecruitState(applicant.RecruitId, applicant.CurrentHp,
                applicant.MaximumHp, applicant.CurrentMp, applicant.MaximumMp,
                applicant.DisplayName, applicant.Kind == ApplicantKind.Signature
                    ? RecruitOriginKind.Signature : RecruitOriginKind.Procedural,
                applicant.SignatureId, applicant.RaceId, applicant.WorldId,
                applicant.ClassTendencyId, applicant.LeadershipBand,
                applicant.PotentialBasisPoints, RecruitAuthorityKind.Normal,
                applicant.CanonicalApplicantJson, applicant.CanonicalScoutingReportJson,
                applicant.OpeningLoadout, applicant.VitalsInitialized,
                applicant.TutorialAliasId, applicant.AuthoredStableRecruitId,
                applicant.LeadershipScore, applicant.TacticalAptitude);
            return _autoGeneration.InitializeRecruit(recruit);
        }

        static IReadOnlyList<EquipmentItemState> ApplicantInventory094(ApplicantSnapshotState applicant) =>
            applicant.OpeningEquipment.Where(item =>
                !LoadoutContains(applicant.OpeningLoadout, item.InstanceId)).ToArray();

        public static bool IsFreeRecruitChanceCard094(ExpeditionRouteCardState089 card) =>
            card != null && card.Category == "RECRUIT" &&
            !SecondDimension.Gameplay.SSSTenV4.SssTenV4AcquisitionService090.IsCampaignContract090(card) &&
            card.RecruitOfferKind == ExpeditionDeckService089.NewRecruitOfferKind089 &&
            !string.IsNullOrWhiteSpace(card.RecruitStableId);

        public HeroMaster300Hero087 PreviewFreeQuestRecruit094(
            CampaignState campaign, string cardId)
        {
            if (campaign?.Guild == null || _heroMasterCatalog089 == null ||
                string.IsNullOrWhiteSpace(cardId)) return null;
            var promised = new HashSet<string>(campaign.Guild.Development
                .AppliedAdventureAuthorityIds.Where(value =>
                    value.StartsWith(EarnedCardPrefix094, StringComparison.Ordinal))
                .Select(value => value.Substring(EarnedCardPrefix094.Length).Split('|'))
                .Where(parts => parts.Length == 3).Select(parts => parts[1]),
                StringComparer.Ordinal);
            return _heroMasterCatalog089.AcceptedHeroes.Where(hero =>
                hero.IsNormalApplicantEligible && !promised.Contains(hero.StableId) &&
                !HeroMaster300ApplicantLead089.RosterContains(campaign.Guild.Recruits, hero))
                .OrderBy(hero => CanonicalJson.Sha256Hex(new {
                    Rule = "FREE_QUEST_RECRUIT_094", campaign.CampaignGuid,
                    campaign.CampaignSeed, cardId, hero.StableId }), StringComparer.Ordinal)
                .FirstOrDefault();
        }

        internal Result<CampaignState> RecordEarnedQuestCardRecruit094(
            CampaignState committedCampaign, CampaignState beforeChoice,
            GuildCityContent017D content, string cardId)
        {
            if (committedCampaign?.Guild == null || beforeChoice?.Guild == null ||
                committedCampaign.CampaignGuid != beforeChoice.CampaignGuid ||
                content == null || _heroMasterCatalog089 == null)
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_CONTEXT_REQUIRED");
            var card = new GuildCityExpeditionService017D().BuildQuestCardRow090(
                beforeChoice, content, this).FirstOrDefault(value => value.CardId == cardId);
            if (card == null || card.Category != "FREE_RECRUIT")
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_FREE_CARD_REQUIRED");
            var receipt = GuildCityExpeditionService017D.QuestCardReceiptPrefix090 + cardId;
            if (!committedCampaign.Guild.Development.HasAdventureAuthority(receipt) ||
                beforeChoice.Guild.Development.HasAdventureAuthority(receipt))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_COMMITTED_CARD_REQUIRED");
            if (card.DieOne + card.DieTwo + card.FateCheckModifier < card.Target)
                return Result<CampaignState>.Success(committedCampaign);
            if (!_heroMasterCatalog089.TryGetAcceptedHero(card.HeroRecruitId, out var hero) ||
                !hero.IsNormalApplicantEligible ||
                HeroMaster300ApplicantLead089.RosterContains(beforeChoice.Guild.Recruits, hero))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_NEW_IDENTITY_REQUIRED");
            return RecordCardInvitation094(committedCampaign, receipt, hero.StableId,
                beforeChoice.Guild.GuildCity.Strategic017H?.Campaign019?
                    .Playable020?.WorldGate023?.CurrentWorldId ?? "SKYHOME");
        }

        // Called only after the unchanged existing card authority has committed
        // its dice outcome and global receipt. No card preview can earn a grant.
        public static Result<CampaignState> RecordEarnedCardRecruit094(
            CampaignState campaign, ExpeditionRouteCardState089 card,
            ExpeditionCardReceipt089 receipt)
        {
            // An ASCENSION card is a free earned duplicate, not a second
            // paid applicant invitation. The exact successful committed receipt
            // below is required for both new and owned recruit card outcomes.
            if (!(IsFreeRecruitChanceCard094(card) || (card != null && card.Category == "RECRUIT" &&
                    card.RecruitOfferKind == ExpeditionDeckService089.AscensionOfferKind089 &&
                    !SecondDimension.Gameplay.SSSTenV4.SssTenV4AcquisitionService090.IsCampaignContract090(card))) || receipt == null ||
                string.IsNullOrWhiteSpace(receipt.RecruitStableId))
                return Result<CampaignState>.Success(campaign);
            var deck = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
                .Playable020?.WorldGate023?.ActiveOperation?.ExpeditionDeck089;
            if (deck == null || receipt.CardId != card.CardId ||
                receipt.RecruitStableId != card.RecruitStableId ||
                !campaign.Guild.Development.HasAdventureAuthority(receipt.ReceiptId) ||
                !deck.AppliedReceipts.Any(value =>
                    value.ReceiptId == receipt.ReceiptId &&
                    CanonicalJson.Sha256Hex(value) == CanonicalJson.Sha256Hex(receipt)))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_COMMITTED_CARD_REQUIRED");
            var world = campaign.Guild.GuildCity.Strategic017H.Campaign019
                .Playable020.WorldGate023.ActiveOperation.WorldId;
            return RecordCardInvitation094(campaign, receipt.ReceiptId,
                receipt.RecruitStableId, world);
        }

        static Result<CampaignState> RecordCardInvitation094(CampaignState campaign,
            string receiptId, string heroId, string world)
        {
            var entry = EarnedCardPrefix094 + receiptId + "|" + heroId + "|" + world;
            var development = campaign.Guild.Development;
            if (development.HasAdventureAuthority(entry))
                return Result<CampaignState>.Success(campaign);
            if (!development.CanRecordAdventureAuthority(entry))
                return Result<CampaignState>.Failure("CAMPAIGN_RECRUIT094_LEDGER_FULL");
            return Result<CampaignState>.Success(WithRecruitRewardDevelopment094(
                campaign, development.RecordAdventureAuthority(entry)));
        }

        static string ClaimAuthority094(string source) => EarnedClaimPrefix094 +
            CanonicalJson.Sha256Hex(new { Rule = "EARNED_RECRUIT_SOURCE_094", source });

        static readonly string[] OpeningStoryContracts094 = {
            GuildCityExpeditionService017D.FirstStoryContractId066,
            GuildCityExpeditionService017D.SecondStoryContractId076,
            "CONTRACT_RELIEF_ROAD"
        };

        public static bool HasVerifiedOpeningStoryReward094(CampaignState campaign,
            string contractId)
        {
            var city = campaign?.Guild?.GuildCity;
            if (city == null || !OpeningStoryContracts094.Contains(contractId)) return false;
            var boards = contractId == GuildCityExpeditionService017D.FirstStoryContractId066
                ? new[] { GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
                    GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069,
                    GuildCityExpeditionService017D.LegacyFirstRescueBoardId069 }
                : new[] { contractId == GuildCityExpeditionService017D.SecondStoryContractId076
                    ? GuildCityExpeditionService017D.SecondStoryBoardId076
                    : GuildCityExpeditionService017D.ReliefRoadBoardId081 };
            var claimed = new HashSet<string>(campaign.Guild.Development.ClaimedBattleRewardIds
                .Where(value => value.StartsWith("CONTRACT_REWARD_CONTRACT_COMMIT_",
                    StringComparison.Ordinal)), StringComparer.Ordinal);
            if (claimed.Count == 0) return false;
            // Exact historical AcceptContract identity, not an operation-count
            // approximation. No reward is synthesized when no receipt matches.
            for (var ordinal = 0; ordinal < city.OperationOrdinal; ordinal++)
                foreach (var boardId in boards)
                {
                    var seed = SemanticSeed.Derive(campaign.CampaignGuid,
                        campaign.CampaignSeed, ordinal, contractId, boardId).ToString();
                    var hash = CanonicalJson.Sha256Hex(new { campaign.CampaignGuid,
                        OperationOrdinal = ordinal, contractId,
                        committedBoardId = boardId, seed });
                    if (claimed.Contains("CONTRACT_REWARD_CONTRACT_COMMIT_" +
                        hash.Substring(0, 24).ToUpperInvariant())) return true;
                }
            return false;
        }

        public static int EarnedRecruitCapacity094(GuildDevelopmentState development) =>
            development?.AppliedAdventureAuthorityIds.Where(value =>
                value != null && value.StartsWith(EarnedIdentityPrefix094,
                    StringComparison.Ordinal)).Select(value =>
                        value.Substring(EarnedIdentityPrefix094.Length).Split('|'))
                .Where(parts => parts.Length == 2 &&
                    parts[0].StartsWith(EarnedClaimPrefix094, StringComparison.Ordinal) &&
                    development.HasAdventureAuthority(parts[0]))
                .Select(parts => parts[0]).Distinct(StringComparer.Ordinal).Count() ?? 0;

        static CampaignState WithRecruitRewardDevelopment094(CampaignState campaign,
            GuildDevelopmentState development) => campaign.With(campaign.Guild.With(
                campaign.Guild.TreasuryXp, campaign.Guild.Recruits, campaign.Guild.Unions,
                campaign.Guild.Inventory, development), campaign.OpeningFlow);
    }
}
