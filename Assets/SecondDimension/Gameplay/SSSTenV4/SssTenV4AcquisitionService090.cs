using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    /// <summary>
    /// Natural SSS acquisition adapter. Offers live inside the existing campaign
    /// save and are committed with the existing Tower/card reward transaction.
    /// </summary>
    public static class SssTenV4AcquisitionService090
    {
        public const string CampaignContractCategory090 = "SSS_CONTRACT";
        public const string CampaignContractOfferKind090 = "SSS_CONTRACT_090";
        public const string CampaignContractSourcePrefix090 =
            "SSS_CONTRACT_090|";

        /// <summary>
        /// Initial candidate weight is exactly one card in the existing shuffled
        /// deck. Returning null keeps pre-100-win and exhausted pools unchanged.
        /// </summary>
        public static ExpeditionRouteCardState089
            CreateCampaignContractCandidate090(
                long campaignSeed,
                string operationId,
                WorldGateBoardRule023 board,
                IReadOnlyList<RecruitState> ownedRecruits,
                SssSave progression)
        {
            if (progression == null || board?.Nodes == null ||
                string.IsNullOrWhiteSpace(operationId))
                return null;
            SssProgression.Validate(progression);
            if (!SssAcquisition.CampaignUnlocked(
                    progression.world.campaignBattleWins))
                return null;

            var owned = OwnedHeroIds090(ownedRecruits);
            var pool = SssAcquisition.AvailablePool(progression, owned);
            if (pool.Length == 0) return null;
            var node = board.Nodes.FirstOrDefault(value => value != null &&
                !value.RequiresCertifiedBattle);
            if (node == null) return null;
            var choices = BoardAdventureRules084.OrderedChoices084(operationId,
                node.NodeId, node.ChoiceIds ?? Array.Empty<string>());
            var choice = choices.Count > 0 ? choices[0] : "CONTINUE";

            var seed = SemanticSeed.Derive(campaignSeed,
                "SSS_CAMPAIGN_CONTRACT_090", operationId, board.BoardId,
                progression.world.campaignBattleWins);
            var rng = new Pcg32(seed.Seed, seed.Stream);
            var selectionIndex = rng.NextInclusive(0, pool.Length - 1);
            var heroId = pool[selectionIndex];
            var hash = CanonicalJson.Sha256Hex(new
            {
                operationId,
                board.BoardId,
                node.NodeId,
                Choice = choice,
                CampaignWins = progression.world.campaignBattleWins,
                HeroId = heroId,
                SelectionIndex = selectionIndex,
                CandidateWeight = 1,
                Seed = seed.ToString()
            });
            var cardId = "SSSCARD090_" +
                         hash.Substring(0, 24).ToUpperInvariant();
            return new ExpeditionRouteCardState089(
                cardId,
                node.NodeId,
                choice,
                CampaignContractCategory090,
                "RECRUIT",
                "SSS Covenant Contract",
                "A gold covenant answers your Guild. Choose this card to secure " +
                "one saved SSS recruit offer from the unowned roster.",
                "RARE • GUARANTEED IF CHOSEN",
                "SUCCESS • the shuffled card is the chance; there is no second roll",
                PrettyHeroName090(heroId) + " • persistent recruit offer",
                0,
                0,
                0,
                0,
                Array.Empty<string>(),
                1,
                heroId,
                PrettyHeroName090(heroId),
                "SSS",
                CampaignContractSourcePrefix090 +
                progression.world.campaignBattleWins + "|" +
                selectionIndex.ToString(CultureInfo.InvariantCulture) + "|" +
                heroId,
                CampaignContractOfferKind090,
                false);
        }

        public static bool IsCampaignContract090(
            ExpeditionRouteCardState089 card) =>
            card != null && StringComparer.Ordinal.Equals(card.Category,
                CampaignContractCategory090) &&
            StringComparer.Ordinal.Equals(card.RecruitOfferKind,
                CampaignContractOfferKind090);

        /// <summary>
        /// Called from the same exact-once deck receipt application. The selected
        /// contract always succeeds; the shuffled deck already supplied the chance.
        /// </summary>
        public static Result<CampaignState> ApplySelectedCampaignContract090(
            CampaignState campaign,
            ExpeditionRouteCardState089 card,
            ExpeditionCardReceipt089 receipt)
        {
            try
            {
                if (campaign == null || !IsCampaignContract090(card) ||
                    receipt == null ||
                    !StringComparer.Ordinal.Equals(receipt.CardId, card.CardId) ||
                    !StringComparer.Ordinal.Equals(receipt.RecruitStableId,
                        card.RecruitStableId) ||
                    !StringComparer.Ordinal.Equals(receipt.Outcome, "SUCCESS"))
                    return Result<CampaignState>.Failure(
                        "SSS090_SELECTED_CONTRACT_RECEIPT_INVALID");

                if (!TryReadCampaignContractSource090(card.SourceTag,
                        out var winsAtDeckDraw, out var savedSelectionIndex,
                        out var savedHeroId) ||
                    !StringComparer.Ordinal.Equals(savedHeroId,
                        card.RecruitStableId))
                    return Result<CampaignState>.Failure(
                        "SSS090_SELECTED_CONTRACT_SNAPSHOT_INVALID");

                var state = SssTenV4CampaignAccessor090.Read(campaign);
                var offerKey = ExactNumbers.Key("CampaignSss", card.CardId);
                var prior = state.Progression.rewardOffers.FirstOrDefault(value =>
                    value != null && StringComparer.Ordinal.Equals(value.key,
                        offerKey));
                if (prior != null)
                    return prior.success && StringComparer.Ordinal.Equals(
                               prior.heroId, savedHeroId)
                        ? Result<CampaignState>.Success(campaign)
                        : Result<CampaignState>.Failure(
                            "SSS090_SELECTED_CONTRACT_PRIOR_OFFER_INVALID");
                var owned = OwnedHeroIds090(campaign.Guild.Recruits);
                var pool = SssAcquisition.AvailablePool(state.Progression, owned);
                if (savedSelectionIndex < 0 || savedSelectionIndex >= pool.Length ||
                    !StringComparer.Ordinal.Equals(pool[savedSelectionIndex],
                        savedHeroId))
                    return Result<CampaignState>.Failure(
                        "SSS090_SELECTED_CONTRACT_POOL_CHANGED");

                var reduction = SssAcquisition.SelectedCampaignContract(
                    state.Progression, card.CardId, winsAtDeckDraw, true,
                    savedSelectionIndex, owned);
                if (reduction.offer == null || !reduction.offer.success ||
                    !StringComparer.Ordinal.Equals(reduction.offer.heroId,
                        savedHeroId))
                    return Result<CampaignState>.Failure(
                        "SSS090_SELECTED_CONTRACT_OFFER_INVALID");

                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(campaign,
                        state.With(progression: reduction.next)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_SELECTED_CONTRACT_REJECTED:" + exception.Message);
            }
        }

        /// <summary>
        /// Commits highest cleared floor and the persisted one-percent opportunity
        /// together with the existing Tower completion receipt. Tower remains a
        /// direct battle flow; no deck/card state is created here.
        /// </summary>
        public static Result<CampaignState> ApplyTowerCompletion090(
            CampaignState campaign,
            string completionReceiptId,
            int actualClearedFloor,
            bool awardLegacyNaturalOffers090 = true)
        {
            try
            {
                if (campaign?.Guild == null ||
                    string.IsNullOrWhiteSpace(completionReceiptId) ||
                    actualClearedFloor < 1)
                    return Result<CampaignState>.Failure(
                        "SSS090_TOWER_COMPLETION_REQUIRED");
                ExactNumbers.RequireId(completionReceiptId);

                var state = SssTenV4CampaignAccessor090.Read(campaign);
                var victory = SssProgression.Victory(state.Progression,
                    new VictoryFact
                    {
                        encounterInstanceId = completionReceiptId,
                        mode = "Tower",
                        towerFloor = actualClearedFloor.ToString(
                            CultureInfo.InvariantCulture),
                        won = true,
                        rewardEligible = true,
                        completedCampaignStage = false
                    });
                var progression = victory.next;

                if (awardLegacyNaturalOffers090 && actualClearedFloor >= 500)
                {
                    var owned = OwnedHeroIds090(campaign.Guild.Recruits);
                    var pool = SssAcquisition.AvailablePool(progression, owned);
                    TowerOfferRoll090(campaign.CampaignSeed,
                        campaign.CampaignGuid, completionReceiptId,
                        actualClearedFloor, pool.Length, out var roll,
                        out var selectionIndex);
                    progression = SssAcquisition.TowerReward(progression,
                        completionReceiptId,
                        actualClearedFloor.ToString(CultureInfo.InvariantCulture),
                        roll, selectionIndex, owned).next;
                }

                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(campaign,
                        state.With(progression: progression)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_TOWER_COMPLETION_REJECTED:" + exception.Message);
            }
        }

        /// <summary>
        /// Claims a persisted natural offer through the same live recruit grant
        /// authority used by SSS creator codes. There is no duplicate fallback roll.
        /// </summary>
        public static Result<CampaignState> ClaimOffer090(
            CampaignState campaign,
            string offerKey)
        {
            try
            {
                if (campaign?.Guild == null || string.IsNullOrWhiteSpace(offerKey))
                    return Result<CampaignState>.Failure(
                        "SSS090_RECRUIT_OFFER_REQUIRED");
                var state = SssTenV4CampaignAccessor090.Read(campaign);
                var stored = state.Progression.rewardOffers.FirstOrDefault(value =>
                    value != null && StringComparer.Ordinal.Equals(value.key,
                        offerKey));
                if (stored?.claimed == true)
                    return Result<CampaignState>.Success(campaign);
                var plan = SssAcquisition.ClaimOffer(state.Progression, offerKey,
                    OwnedHeroIds090(campaign.Guild.Recruits));
                if (string.IsNullOrWhiteSpace(plan.heroId))
                    return Result<CampaignState>.Failure(
                        "SSS090_RECRUIT_OFFER_NOT_CLAIMABLE:" + plan.reason);

                var host = plan.grant
                    ? SssTenV4HostRewards090.GrantRecruit(campaign, plan.heroId,
                        plan.receipt)
                    : Result<CampaignState>.Success(campaign);
                if (!host.IsSuccess) return host;
                var hostState = SssTenV4CampaignAccessor090.Read(host.Value);
                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(host.Value,
                        hostState.With(progression: plan.next)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_RECRUIT_OFFER_REJECTED:" + exception.Message);
            }
        }

        /// <summary>
        /// Shared natural reward entry point for authored Ascension Trials. The
        /// caller supplies its already-validated exact-once trial/reward receipt.
        /// </summary>
        public static Result<CampaignState> GrantAscensionTrialCredit090(
            CampaignState campaign,
            string heroId,
            string trialRewardReceiptId) =>
            SssTenV4HostRewards090.GrantAscensionCredit(campaign, heroId,
                trialRewardReceiptId);

        /// <summary>
        /// Binds the existing certified Campaign022 TRIAL completion to one
        /// natural bound credit. Stable committed formation order chooses the
        /// first deployed, owned SSS hero that can still ascend; bench heroes and
        /// A10 heroes are skipped. A Trial without an eligible SSS hero keeps its
        /// normal rewards and grants no hidden replacement.
        /// </summary>
        public static Result<CampaignState> ApplyCompletedAscensionTrial090(
            CampaignState campaign,
            string trialCompletionReceiptId,
            IReadOnlyList<string> committedAlliedRecruitIds)
        {
            try
            {
                if (campaign?.Guild == null ||
                    string.IsNullOrWhiteSpace(trialCompletionReceiptId) ||
                    committedAlliedRecruitIds == null)
                    return Result<CampaignState>.Failure(
                        "SSS090_ASCENSION_TRIAL_COMPLETION_REQUIRED");
                ExactNumbers.RequireId(trialCompletionReceiptId);

                var considered = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < committedAlliedRecruitIds.Count;
                     index++)
                {
                    var heroId = SssHeroes.CanonicalId(
                        committedAlliedRecruitIds[index]);
                    if (!SssHeroes.IsSss(heroId) || !considered.Add(heroId))
                        continue;
                    var recruit = SssTenV4Roster090.FindOwned(
                        campaign.Guild.Recruits, heroId);
                    if (recruit == null ||
                        recruit.Progression.AscensionLevel >=
                        RecruitAscensionRules089.MaximumLevel)
                        continue;
                    return GrantAscensionTrialCredit090(campaign, heroId,
                        ExactNumbers.Key("SssNaturalTrial090",
                            trialCompletionReceiptId));
                }

                return Result<CampaignState>.Success(campaign);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_ASCENSION_TRIAL_REJECTED:" + exception.Message);
            }
        }

        /// <summary>
        /// Claims one hero's completed 70-family hunt reward through the shared
        /// signature-weapon entitlement. The live Omega budget gate remains explicit.
        /// </summary>
        public static Result<CampaignState> ClaimSignatureWeapon090(
            CampaignState campaign,
            string heroId,
            bool liveOmegaDefinitionResolved)
        {
            try
            {
                if (campaign?.Guild == null)
                    return Result<CampaignState>.Failure(
                        "SSS090_SIGNATURE_WEAPON_CAMPAIGN_REQUIRED");
                var state = SssTenV4CampaignAccessor090.Read(campaign);
                var plan = SssSignatureWeapons.PlanHuntClaim(
                    state.Progression,
                    state.WeaponHunt,
                    heroId,
                    OwnedHeroIds090(campaign.Guild.Recruits),
                    campaign.Guild.Inventory.Where(item => item != null)
                        .Select(item => item.DefinitionId),
                    liveOmegaDefinitionResolved);
                var wasEntitled = state.Progression.codeReceipts.Contains(
                    plan.receipt);
                var becomesEntitled = plan.next.codeReceipts.Contains(plan.receipt);
                if (!becomesEntitled)
                    return Result<CampaignState>.Failure(
                        "SSS090_SIGNATURE_WEAPON_NOT_CLAIMABLE:" + plan.reason);
                if (wasEntitled)
                    return Result<CampaignState>.Success(campaign);

                // Host adapter owns the shared inventory/creator entitlement. Merge
                // the package plan only after that host transaction succeeds.
                var host = SssTenV4HostRewards090.GrantSignatureWeapon(
                    campaign, plan.heroId, plan.receipt);
                if (!host.IsSuccess) return host;
                var hostState = SssTenV4CampaignAccessor090.Read(host.Value);
                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(host.Value,
                        hostState.With(progression: plan.next)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_SIGNATURE_WEAPON_REJECTED:" + exception.Message);
            }
        }

        public static void TowerOfferRoll090(
            long campaignSeed,
            string campaignGuid,
            string completionReceiptId,
            int actualClearedFloor,
            int availableHeroCount,
            out int roll0To9999,
            out int selectionIndex)
        {
            if (string.IsNullOrWhiteSpace(campaignGuid) ||
                string.IsNullOrWhiteSpace(completionReceiptId) ||
                actualClearedFloor < 1 || availableHeroCount < 0)
                throw new ArgumentException("Invalid Tower offer identity.");
            var seed = SemanticSeed.Derive(campaignSeed, "SSS_TOWER_OFFER_090",
                campaignGuid, completionReceiptId,
                actualClearedFloor.ToString(CultureInfo.InvariantCulture));
            var rng = new Pcg32(seed.Seed, seed.Stream);
            roll0To9999 = rng.NextInclusive(0, 9999);
            selectionIndex = availableHeroCount == 0
                ? 0
                : rng.NextInclusive(0, availableHeroCount - 1);
        }

        public static bool TryReadCampaignContractSource090(
            string sourceTag,
            out string winsAtDeckDraw,
            out int selectionIndex,
            out string heroId)
        {
            winsAtDeckDraw = null;
            selectionIndex = -1;
            heroId = null;
            if (string.IsNullOrWhiteSpace(sourceTag) ||
                !sourceTag.StartsWith(CampaignContractSourcePrefix090,
                    StringComparison.Ordinal))
                return false;
            var parts = sourceTag.Substring(
                    CampaignContractSourcePrefix090.Length)
                .Split('|');
            if (parts.Length != 3 ||
                !int.TryParse(parts[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out selectionIndex) ||
                selectionIndex < 0 || !SssHeroes.IsSss(parts[2]))
                return false;
            try
            {
                ExactNumbers.Read(parts[0]);
            }
            catch (ArgumentException)
            {
                return false;
            }
            winsAtDeckDraw = parts[0];
            heroId = SssHeroes.CanonicalId(parts[2]);
            return true;
        }

        private static string[] OwnedHeroIds090(
            IReadOnlyList<RecruitState> recruits)
        {
            var identities = new List<string>();
            foreach (var recruit in recruits ?? Array.Empty<RecruitState>())
            {
                if (recruit == null) continue;
                identities.Add(recruit.RecruitId);
                if (!string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId))
                    identities.Add(recruit.AuthoredStableRecruitId);
                if (!string.IsNullOrWhiteSpace(recruit.SignatureId))
                    identities.Add(recruit.SignatureId);
                if (!string.IsNullOrWhiteSpace(recruit.TutorialAliasId))
                    identities.Add(recruit.TutorialAliasId);
            }
            return identities.Select(SssHeroes.CanonicalId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        }

        private static string PrettyHeroName090(string heroId)
        {
            var value = SssHeroes.CanonicalId(heroId);
            if (!SssHeroes.IsSss(value)) return value ?? "SSS Hero";
            var words = value.Substring(4).Split('_');
            return string.Join(" ", words.Select(word =>
                word.Length == 0 ? word :
                char.ToUpperInvariant(word[0]) +
                word.Substring(1).ToLowerInvariant()));
        }
    }
}
