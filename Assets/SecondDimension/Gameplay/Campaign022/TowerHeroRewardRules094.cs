using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    /// <summary>
    /// Pure reward selection only. This does not prove a clear or grant anything.
    /// The caller must supply the actual begin-bound Tower floor, never a sum of
    /// repeated authored-floor clears, and commit this plan with its valid proof.
    /// </summary>
    public sealed class TowerHeroRewardPlan094
    {
        internal TowerHeroRewardPlan094(int floor, string completionReceipt,
            string tier, bool guaranteed, int chance, int roll, int selection,
            string[] candidateHeroIds, string poolHash, string selectedHero, string planId)
        {
            ActualClearedFloor = floor;
            CompletionReceiptId = completionReceipt;
            Tier = tier;
            Guaranteed = guaranteed;
            ChanceBasisPoints = chance;
            SavedRoll0To9999 = roll;
            SelectionIndex = selection;
            CandidateHeroIds = Array.AsReadOnly(candidateHeroIds.ToArray());
            CandidatePoolHash = poolHash;
            SelectedHeroId = selectedHero;
            PlanId = planId;
        }

        public int ActualClearedFloor { get; }
        public string CompletionReceiptId { get; }
        public string Tier { get; }
        public bool Guaranteed { get; }
        public int ChanceBasisPoints { get; }
        public int SavedRoll0To9999 { get; }
        public int SelectionIndex { get; }
        public IReadOnlyList<string> CandidateHeroIds { get; }
        public string CandidatePoolHash { get; }
        public string SelectedHeroId { get; }
        public string PlanId { get; }
        public bool WinsHero => !string.IsNullOrWhiteSpace(SelectedHeroId);
    }

    public static class TowerHeroRewardRules094
    {
        public const string PolicyVersion = "TOWER_HERO_REWARDS_094_V1";
        public const int ChanceInterval = 10;
        public const int GuaranteedInterval = 50;
        public const int FirstSssFloor = 500;
        // Provisional balance value carried forward from the existing Tower offer.
        public const int BonusChanceBasisPoints = 100;

        public static bool IsRewardFloor(int actualFloor) =>
            actualFloor > 0 && actualFloor % ChanceInterval == 0;

        public static bool IsGuaranteedFloor(int actualFloor) =>
            actualFloor > 0 && actualFloor % GuaranteedInterval == 0;

        public static string TierForFloor(int actualFloor) => !IsRewardFloor(actualFloor)
            ? string.Empty : actualFloor >= FirstSssFloor ? "SSS" : "SS";

        // A lost/aborted attempt has a new certified completion receipt when it
        // is retried, but not a new floor prize roll. Never use that attempt's
        // terminal receipt as the random salt for an actual-floor milestone.
        public static string RollIdentity094(CampaignState campaign, int actualFloor)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (actualFloor < 1) throw new ArgumentOutOfRangeException(nameof(actualFloor));
            return "TOWER_HERO094_FLOOR_" + CanonicalJson.Sha256Hex(new {
                PolicyVersion, campaign.CampaignGuid, ActualFloor = actualFloor }).ToUpperInvariant();
        }

        /// <summary>
        /// Uses the existing semantic-seed/PCG Tower roll. Ownership, pending
        /// offers, Auto and animation speed are deliberately not selection inputs.
        /// Existing saved outcome plans must be read, not rebuilt under new data.
        /// </summary>
        public static TowerHeroRewardPlan094 BuildPlan(CampaignState campaign,
            string completionReceiptId, int actualClearedFloor,
            HeroMaster300Catalog087 heroCatalog)
        {
            if (campaign?.Guild == null) throw new ArgumentNullException(nameof(campaign));
            if (actualClearedFloor < 1) throw new ArgumentOutOfRangeException(nameof(actualClearedFloor));
            if (string.IsNullOrWhiteSpace(completionReceiptId))
                throw new ArgumentException("The exact completed Tower receipt is required.", nameof(completionReceiptId));
            if (!IsRewardFloor(actualClearedFloor)) return null;
            var tier = TierForFloor(actualClearedFloor);
            if (tier == "SS" && heroCatalog == null)
                throw new InvalidOperationException("Accepted SS HeroMaster authority is unavailable.");
            var pool = tier == "SSS"
                ? SssTenV4Roster090.All.Select(hero => hero.HeroId)
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray()
                : heroCatalog.AcceptedHeroes.Where(hero => hero.Rank == HeroMasterRank087.SS)
                    .Select(hero => hero.StableId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (pool.Length == 0 || pool.Distinct(StringComparer.Ordinal).Count() != pool.Length ||
                (tier == "SSS" && pool.Length != 10))
                throw new InvalidOperationException("The exact Tower hero pool is unavailable or inconsistent.");
            var poolHash = CanonicalJson.Sha256Hex(new { Tier = tier, Heroes = pool });
            var rollIdentity = RollIdentity094(campaign, actualClearedFloor);
            SssTenV4AcquisitionService090.TowerOfferRoll090(campaign.CampaignSeed,
                campaign.CampaignGuid, rollIdentity, actualClearedFloor,
                pool.Length, out var roll, out var selection);
            var guaranteed = IsGuaranteedFloor(actualClearedFloor);
            var chance = guaranteed ? 10000 : BonusChanceBasisPoints;
            var heroId = roll < chance ? pool[selection] : string.Empty;
            return new TowerHeroRewardPlan094(actualClearedFloor, completionReceiptId,
                tier, guaranteed, chance, roll, selection, pool, poolHash, heroId,
                PlanId094(campaign, completionReceiptId, actualClearedFloor, tier,
                    guaranteed, chance, roll, selection, poolHash, heroId));
        }

        internal static string PlanId094(CampaignState campaign, string completionReceiptId,
            int actualClearedFloor, string tier, bool guaranteed, int chance,
            int roll, int selection, string poolHash, string heroId)
        {
            var hash = CanonicalJson.Sha256Hex(new {
                PolicyVersion, campaign.CampaignGuid, ActualFloor = actualClearedFloor,
                CompletionReceiptId = completionReceiptId, RollIdentity = RollIdentity094(campaign, actualClearedFloor),
                Tier = tier, Guaranteed = guaranteed,
                ChanceBasisPoints = chance, SavedRoll = roll, SelectionIndex = selection,
                CandidatePoolHash = poolHash, HeroId = heroId });
            return "TOWER_HERO094_" + hash.ToUpperInvariant();
        }
    }
}


