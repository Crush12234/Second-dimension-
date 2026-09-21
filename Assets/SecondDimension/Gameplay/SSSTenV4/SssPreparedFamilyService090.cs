using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    public sealed class SssPreparedFamilyDefinition090
    {
        internal SssPreparedFamilyDefinition090(string familyId, string displayName)
        {
            FamilyId = familyId;
            DisplayName = displayName;
        }

        public string FamilyId { get; }
        public string DisplayName { get; }
    }

    public sealed class SssPreparedFamilyOption090
    {
        internal SssPreparedFamilyOption090(
            SssPreparedFamilyDefinition090 definition,
            MasteryView mastery)
        {
            FamilyId = definition.FamilyId;
            DisplayName = definition.DisplayName;
            TotalDefeats = mastery.totalDefeats;
            MasteryRank = mastery.rank;
            DefeatsRemaining = mastery.defeatsRemaining;
            Unlocked = mastery.unlocked;
        }

        public string FamilyId { get; }
        public string DisplayName { get; }
        public string TotalDefeats { get; }
        public string MasteryRank { get; }
        public string DefeatsRemaining { get; }
        public bool Unlocked { get; }
    }

    public sealed class SssPreparedFamilyLoadoutView090
    {
        internal SssPreparedFamilyLoadoutView090(
            string heroId,
            string commandName,
            int requiredSlotCount,
            IReadOnlyList<string> preparedFamilyIds,
            IReadOnlyList<SssPreparedFamilyOption090> families,
            bool legalPreparationBoundary,
            bool signatureWeaponOwned,
            bool signatureWeaponClaimable)
        {
            HeroId = heroId;
            CommandName = commandName;
            RequiredSlotCount = requiredSlotCount;
            PreparedFamilyIds = preparedFamilyIds ?? Array.Empty<string>();
            Families = families ?? Array.Empty<SssPreparedFamilyOption090>();
            LegalPreparationBoundary = legalPreparationBoundary;
            SignatureWeaponOwned = signatureWeaponOwned;
            SignatureWeaponClaimable = signatureWeaponClaimable;
        }

        public string HeroId { get; }
        public string CommandName { get; }
        public int RequiredSlotCount { get; }
        public IReadOnlyList<string> PreparedFamilyIds { get; }
        public IReadOnlyList<SssPreparedFamilyOption090> Families { get; }
        public bool LegalPreparationBoundary { get; }
        public bool SignatureWeaponOwned { get; }
        public bool SignatureWeaponClaimable { get; }
        public int UnlockedFamilyCount => Families.Count(value => value.Unlocked);
        public bool Complete => RequiredSlotCount > 0 &&
                                PreparedFamilyIds.Count == RequiredSlotCount;

        public SssPreparedFamilyOption090 Family(string familyId) =>
            Families.FirstOrDefault(value => StringComparer.Ordinal.Equals(
                value.FamilyId, familyId));
    }

    /// <summary>
    /// The production preparation boundary for the three family-based SSS Gold
    /// commands. It mutates only CampaignState.SssV4090 and deliberately relies on
    /// CovenantMastery's real unlock rule; duplicate Rylen slots remain distinct.
    /// </summary>
    public static class SssPreparedFamilyService090
    {
        private static readonly SssPreparedFamilyDefinition090[] Definitions =
        {
            new SssPreparedFamilyDefinition090("ENEMY_REC_001", "Gloomroot Wolf"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_002", "Thornbound Forest Assassin"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_003", "Elder Rootlord"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_004", "Corrupted Treant Spearman"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_005", "Fungal Shaman"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_006", "Treant Warrior"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_007", "Mossbound Golem"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_008", "Hexbloom Witch"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_009", "Fungal Archer"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_010", "Twin Treant Guardian"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_011", "Ironhorn Bulwark"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_012", "Gorechain Ravager"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_013", "Carrion-Beak Hexer"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_014", "Chainwraith Executioner"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_015", "Thornshade Witch"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_016", "Cindercoil Ascetic"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_017", "Miasma Apothecary"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_018", "Void-Lantern Hierophant"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_019", "Gravechain Marauder"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_020", "Emberfury Warlord"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_021", "Crystal Myrmidon"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_022", "Thunderbeak Harrier"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_023", "Frostmane Huntress"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_024", "Gloomspore Myconid"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_025", "Boneclad Warden"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_026", "Mirefang Stalker"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_027", "Orc Artillery Trooper"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_028", "Dreadcoil Seer"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_029", "Ashroot Treant"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_030", "Sunken Templar"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_031", "Gearbanner Castellan"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_032", "Gilded Maw Mimic"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_033", "Ossuary Wyvern"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_034", "Coralback Siege Crab"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_035", "Magmacoil Gastropod"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_036", "Astral Bell Medusa"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_037", "Dunejaw Burrower"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_038", "Cindermaw Cerberus"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_039", "Prismwatch Oculus"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_040", "Ironquill Pangolin"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_041", "Emeraldback Tortoise"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_042", "Tempest Roc"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_043", "Bogvenom Hydra"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_044", "Sunken Idol Sentinel"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_045", "Riftglide Manta"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_046", "Sporefrill Basilisk"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_047", "Ashhorn Ram"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_048", "Umbral Owlbear"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_049", "Lurejaw Anglerbeast"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_050", "Suncrest Scarab"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_051", "Glacier Sabercat"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_052", "Mirestone Troll"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_053", "Stormplate Ankylosaur"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_054", "Emberweb Arachnid"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_055", "Shattermirror Wraith"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_056", "Sunfire Phoenix"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_057", "Rotroot Warboar"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_058", "Nightglass Bat Lord"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_059", "Tidemaw Sharkfiend"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_060", "Frosttusk Mammoth"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_061", "Reefstorm Crustacean"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_062", "Spectral Thornbloom"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_063", "Sunstone Gryphon"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_064", "Mirevenom Naga"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_065", "Obsidian Hornbeetle"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_066", "Moonveil Stag Wraith"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_067", "Ironwood Rhinoceros"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_068", "Frostfeather Harpy"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_069", "Voidspore Slime Tyrant"),
            new SssPreparedFamilyDefinition090("ENEMY_REC_070", "Emberjaw Crocodrake")
        };

        public static IReadOnlyList<SssPreparedFamilyDefinition090> AllFamilies =>
            Array.AsReadOnly(Definitions);

        public static int RequiredSlotCount(string heroId)
        {
            if (!SssTenV4Roster090.TryGet(heroId, out var hero)) return 0;
            var index = Array.IndexOf(SssHeroes.All, hero.HeroId);
            return index == 0 ? 6 : index == 1 || index == 2 ? 1 : 0;
        }

        public static SssPreparedFamilyLoadoutView090 View(
            CampaignState campaign,
            string heroId)
        {
            if (campaign?.Guild == null)
                throw new ArgumentException("An active campaign and Guild are required.", nameof(campaign));
            var hero = SssTenV4Roster090.Get(heroId);
            var required = RequiredSlotCount(hero.HeroId);
            var state = SssTenV4CampaignAccessor090.Read(campaign);
            var prepared = state.PreparedFamilies.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.HeroId, hero.HeroId));
            var families = Definitions.Select(definition =>
            {
                var mastery = CovenantMastery.View(CovenantMastery.Count(
                    state.Progression.familyProgress,
                    hero.HeroId,
                    definition.FamilyId));
                return new SssPreparedFamilyOption090(definition, mastery);
            }).ToArray();
            var entitlement = SssSignatureWeapons.EntitlementReceipt(hero.HeroId);
            var weaponOwned = state.Progression.codeReceipts.Contains(
                                  entitlement, StringComparer.Ordinal) ||
                              campaign.Guild.Inventory.Any(item => item != null &&
                                  StringComparer.Ordinal.Equals(
                                      item.DefinitionId, hero.WeaponItemId));
            var boundary = IsLegalPreparationBoundary(campaign);
            return new SssPreparedFamilyLoadoutView090(
                hero.HeroId,
                CommandName(hero.HeroId),
                required,
                prepared?.FamilyIds ?? Array.Empty<string>(),
                Array.AsReadOnly(families),
                boundary,
                weaponOwned,
                boundary && !weaponOwned &&
                SssTenV4Roster090.RosterContains(
                    campaign.Guild.Recruits, hero.HeroId) &&
                SssWeaponHunt.Complete(state.WeaponHunt));
        }

        public static Result<CampaignState> Configure(
            CampaignState campaign,
            string heroId,
            IEnumerable<string> familyIds)
        {
            try
            {
                if (campaign?.Guild == null)
                    return Result<CampaignState>.Failure(
                        "SSS090_PREPARED_FAMILY_CAMPAIGN_REQUIRED");
                var hero = SssTenV4Roster090.Get(heroId);
                var required = RequiredSlotCount(hero.HeroId);
                if (required == 0)
                    return Result<CampaignState>.Failure(
                        "Only Rylen, Elysia, and Vaelis prepare monster families.");
                if (!SssTenV4Roster090.RosterContains(
                        campaign.Guild.Recruits, hero.HeroId))
                    return Result<CampaignState>.Failure(
                        "Recruit " + hero.DisplayName + " before preparing a Gold Art.");
                if (!IsLegalPreparationBoundary(campaign))
                    return Result<CampaignState>.Failure(
                        "Family preparation is locked after battle Forecasts are committed.");

                var requested = (familyIds ?? Array.Empty<string>())
                    .Select(value => value?.Trim().ToUpperInvariant())
                    .ToArray();
                if (requested.Length != required ||
                    requested.Any(string.IsNullOrWhiteSpace))
                    return Result<CampaignState>.Failure(required == 6
                        ? "Rylen must prepare exactly six family slots; repeated families are allowed."
                        : hero.DisplayName + " must prepare exactly one family.");

                var state = SssTenV4CampaignAccessor090.Read(campaign);
                for (var index = 0; index < requested.Length; index++)
                {
                    if (!SssTenV4FamilyResolver090.Instance.TryResolve(
                            requested[index], out var baseFamily) ||
                        !StringComparer.Ordinal.Equals(
                            baseFamily, requested[index]))
                        return Result<CampaignState>.Failure(
                            "Prepared slot " + (index + 1) +
                            " is not one of the frozen 70 base families.");
                    var mastery = CovenantMastery.View(CovenantMastery.Count(
                        state.Progression.familyProgress,
                        hero.HeroId,
                        requested[index]));
                    if (!mastery.unlocked)
                        return Result<CampaignState>.Failure(
                            FamilyName(requested[index]) +
                            " unlocks for this hero after 10 confirmed defeats.");
                }

                var loadouts = state.PreparedFamilies
                    .Where(value => !StringComparer.Ordinal.Equals(
                        value.HeroId, hero.HeroId))
                    .Concat(new[]
                    {
                        new SssPreparedFamilyLoadout090(hero.HeroId, requested)
                    })
                    .ToArray();
                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(
                        campaign,
                        state.With(preparedFamilies: loadouts)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_PREPARED_FAMILY_REJECTED:" + exception.Message);
            }
        }

        public static Result<CampaignState> CycleSlot(
            CampaignState campaign,
            string heroId,
            int slotIndex)
        {
            try
            {
                var view = View(campaign, heroId);
                if (slotIndex < 0 || slotIndex >= view.RequiredSlotCount)
                    return Result<CampaignState>.Failure(
                        "SSS090_PREPARED_FAMILY_SLOT_INVALID");
                var unlocked = view.Families.Where(value => value.Unlocked)
                    .ToArray();
                if (unlocked.Length == 0)
                    return Result<CampaignState>.Failure(
                        "Defeat 10 enemies from a family with this deployed hero to unlock preparation.");

                var configured = view.PreparedFamilyIds.Count ==
                                 view.RequiredSlotCount;
                var next = configured
                    ? view.PreparedFamilyIds.ToArray()
                    : Enumerable.Repeat(
                        unlocked[0].FamilyId,
                        view.RequiredSlotCount).ToArray();
                if (configured)
                {
                    var current = Array.FindIndex(unlocked, value =>
                        StringComparer.Ordinal.Equals(
                            value.FamilyId, next[slotIndex]));
                    next[slotIndex] = unlocked[(current + 1 + unlocked.Length) %
                                               unlocked.Length].FamilyId;
                }
                return Configure(campaign, heroId, next);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_PREPARED_FAMILY_CYCLE_REJECTED:" + exception.Message);
            }
        }

        public static bool IsLegalPreparationBoundary(CampaignState campaign) =>
            campaign != null &&
            (campaign.Battle == null ||
             campaign.Battle.Outcome != BattleOutcome.InProgress);

        public static string FamilyName(string familyId)
        {
            var definition = Definitions.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.FamilyId, familyId));
            return definition?.DisplayName ?? "Unknown family";
        }

        private static string CommandName(string heroId)
        {
            var index = Array.IndexOf(SssHeroes.All,
                SssHeroes.CanonicalId(heroId));
            switch (index)
            {
                case 0: return "Call the Tamed Union";
                case 1: return "Manifest the Pact";
                case 2: return "Union Metamorphosis";
                default: return string.Empty;
            }
        }
    }
}
