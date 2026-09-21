using System;
using System.Collections.Generic;
using System.Globalization;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Pure identity routing for the recovered Enemy Art 700 presentation pack.
    /// It never changes enemy rules, statistics, Arts, rewards, or AI. Explicit
    /// IDs are committed into BattleMemberState so a save/load cannot reroll art.
    /// </summary>
    public static class EnemyArtIdentity090
    {
        public const int BaseFamilyCount = 70;
        public const int VariantsPerBase = 10;
        public const string TowerBattleMarker = "ABYSS_BATTLE022_FLOOR_";

        // The live Pass 03 registry remains authoritative for gameplay. These are
        // deliberately presentation-only silhouette matches into the asset pack.
        private static readonly IReadOnlyDictionary<string, int> CampaignFamilyToBase090 =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "ENEMY_FAMILY_GATE_GNAWER", 50 },          // Suncrest Scarab
                { "ENEMY_FAMILY_RUSTBACK_HOUND", 1 },        // Gloomroot Wolf
                { "ENEMY_FAMILY_HOLLOW_SALVAGER", 19 },      // Gravechain Marauder
                { "ENEMY_FAMILY_SHARDWING_SWARM", 58 },      // Nightglass Bat Lord
                { "ENEMY_FAMILY_TOLLROAD_CUTTER", 2 },       // Thornbound Forest Assassin
                { "ENEMY_FAMILY_RIFT_MOLD_CREEPER", 24 },    // Gloomspore Myconid
                { "ENEMY_FAMILY_GATEIRON_BRUTE", 11 },       // Ironhorn Bulwark
                { "ENEMY_FAMILY_ECHO_STALKER", 26 },         // Mirefang Stalker
                { "ENEMY_FAMILY_BRASSJAW_PACKLORD", 38 },    // Cindermaw Cerberus
                { "ENEMY_FAMILY_PULSE_SCRIBE", 28 },         // Dreadcoil Seer
                { "ENEMY_FAMILY_CHAINCALLER", 14 },          // Chainwraith Executioner
                { "ENEMY_FAMILY_ASH_MEDIC", 17 },            // Miasma Apothecary
                { "ENEMY_FAMILY_HINGE_EATER_COLOSSUS", 53 }, // Stormplate Ankylosaur
                { "ENEMY_FAMILY_CAPTAIN_RAVEL", 31 },        // Gearbanner Castellan
                { "ENEMY_FAMILY_GATEHEART_WARDEN", 44 }      // Sunken Idol Sentinel
            };

        public static IReadOnlyList<BattleUnionState> CommitIdentities090(
            IReadOnlyList<BattleUnionState> source,
            string battleId)
        {
            var result = new List<BattleUnionState>();
            if (source == null) return result.AsReadOnly();
            for (var unionIndex = 0; unionIndex < source.Count; unionIndex++)
            {
                var union = source[unionIndex];
                if (union == null)
                {
                    result.Add(null);
                    continue;
                }

                var members = new List<BattleMemberState>(union.Members.Count);
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    Resolve090(battleId, unionIndex, memberIndex, member,
                        out var baseId, out var variantId, out var seed);
                    members.Add(member.WithEnemyArt090(baseId, variantId, seed));
                }
                result.Add(union.With(members: members.AsReadOnly()));
            }
            return result.AsReadOnly();
        }

        public static void Resolve090(
            string battleId,
            int unionIndex,
            int memberIndex,
            BattleMemberState member,
            out string baseEnemyId,
            out string variantId,
            out int stableVisualSeed)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));

            // Only the new begin-bound Tower098 request format may rotate the
            // color on a repeat visit. Malformed reserved IDs cannot silently
            // become a legacy template request. Existing untagged IDs are exact.
            var usesTowerCycle098 = TowerEnemyArtCycle098.HasMarker098(battleId);
            var rotatedVariant098 = 0;
            if (usesTowerCycle098 && !TowerEnemyArtCycle098.TryResolve098(
                    battleId, out _, out _, out rotatedVariant098))
                throw new ArgumentException("TOWER098_ENEMY_ART_IDENTITY_INVALID", nameof(battleId));

            if (TryTowerFloor090(battleId, out var towerFloor))
            {
                // Seven visual families belong to each of the ten authored Tower
                // floors. Repeat clears increase Union count, naturally exposing
                // every family without changing the Tower's combat templates.
                var slot = Math.Max(0, unionIndex) * 3 + Math.Max(0, memberIndex);
                var baseIndex = (towerFloor - 1) * 7 + slot % 7 + 1;
                stableVisualSeed = usesTowerCycle098 ? rotatedVariant098 : towerFloor;
                baseEnemyId = BaseId090(baseIndex);
                variantId = VariantId090(baseIndex, stableVisualSeed);
                return;
            }

            var familyId = LiveFamilyId090(member);
            if (!CampaignFamilyToBase090.TryGetValue(familyId, out var mappedBase))
            {
                mappedBase = 1 + (int)(StableHash090(
                    (battleId ?? string.Empty) + "|" + familyId + "|" + member.ClassId) %
                    BaseFamilyCount);
            }

            // VisualVariantSeed090 is persisted presentation state, not an input
            // to its own authority. Re-derive the canonical rank from immutable
            // live enemy identity so changing only saved artwork cannot change a
            // Forecast or combat RNG seed.
            stableVisualSeed = ParsedLiveRank090(member.ClassId, member.MemberId);
            var variantIndex = stableVisualSeed > 0
                ? (stableVisualSeed - 1) % VariantsPerBase + 1
                : 1 + (int)(StableHash090(member.MemberId + "|" + member.ClassId) % VariantsPerBase);
            // New force commitments retain an immutable chapter marker on the
            // spawned identity. It selects presentation tier only; source combat
            // rank, definitions, boss identity and old saved poses stay unchanged.
            if (EnemyForceProfile094.TryArtTierFromSpawn094(member.MemberId, out var forceTier094))
            {
                stableVisualSeed = forceTier094;
                variantIndex = forceTier094;
            }
            baseEnemyId = BaseId090(mappedBase);
            variantId = VariantId090(mappedBase, variantIndex);
        }

        public static string BaseId090(int baseIndex)
        {
            if (baseIndex < 1 || baseIndex > BaseFamilyCount)
                throw new ArgumentOutOfRangeException(nameof(baseIndex));
            return "ENEMY_REC_" + baseIndex.ToString("000", CultureInfo.InvariantCulture);
        }

        public static string VariantId090(int baseIndex, int variantIndex)
        {
            if (variantIndex < 1 || variantIndex > VariantsPerBase)
                throw new ArgumentOutOfRangeException(nameof(variantIndex));
            return BaseId090(baseIndex) + "_VAR_" +
                   variantIndex.ToString("00", CultureInfo.InvariantCulture);
        }

        public static bool TryTowerFloor090(string battleId, out int floor)
        {
            floor = 0;
            if (string.IsNullOrWhiteSpace(battleId)) return false;
            var marker = battleId.IndexOf(TowerBattleMarker, StringComparison.Ordinal);
            if (marker < 0) return false;
            var start = marker + TowerBattleMarker.Length;
            var end = battleId.IndexOf('_', start);
            var token = end > start
                ? battleId.Substring(start, end - start)
                : battleId.Substring(start);
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out floor))
            {
                floor = 0;
                return false;
            }
            floor = Math.Max(1, Math.Min(10, floor));
            return true;
        }

        public static bool TryCampaignBaseIndex090(string familyId, out int baseIndex) =>
            CampaignFamilyToBase090.TryGetValue(familyId ?? string.Empty, out baseIndex);

        private static string LiveFamilyId090(BattleMemberState member)
        {
            if (member.EquipmentTags != null)
                for (var index = 0; index < member.EquipmentTags.Count; index++)
                    if (!string.IsNullOrWhiteSpace(member.EquipmentTags[index]) &&
                        member.EquipmentTags[index].StartsWith("ENEMY_FAMILY_", StringComparison.Ordinal))
                        return member.EquipmentTags[index];

            var identity = NormalizeSpawnIdentity090(member.ClassId);
            if (StringComparer.Ordinal.Equals(identity, "ENEMY_FORMATION_NUISANCE"))
                identity = NormalizeSpawnIdentity090(member.MemberId);
            foreach (var pair in CampaignFamilyToBase090)
            {
                var familyToken = pair.Key.Substring("ENEMY_FAMILY_".Length);
                if (identity.IndexOf(familyToken, StringComparison.Ordinal) >= 0)
                    return pair.Key;
            }
            return identity;
        }

        private static int ParsedLiveRank090(string classId, string memberId)
        {
            var identity = NormalizeSpawnIdentity090(classId);
            if (StringComparer.Ordinal.Equals(identity, "ENEMY_FORMATION_NUISANCE") ||
                string.IsNullOrWhiteSpace(identity))
                identity = NormalizeSpawnIdentity090(memberId);
            var lastUnderscore = identity.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore + 1 < identity.Length &&
                int.TryParse(identity.Substring(lastUnderscore + 1), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var rank))
                return Math.Max(1, rank);
            return 0;
        }

        private static string NormalizeSpawnIdentity090(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity)) return string.Empty;
            var normalized = identity.Trim().ToUpperInvariant();
            var spawn = normalized.IndexOf("_SPAWN070_", StringComparison.Ordinal);
            if (spawn >= 0) normalized = normalized.Substring(0, spawn);
            var pack = normalized.IndexOf("_PACK_", StringComparison.Ordinal);
            if (pack >= 0) normalized = normalized.Substring(0, pack);
            return normalized;
        }

        private static uint StableHash090(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                var text = value ?? string.Empty;
                for (var index = 0; index < text.Length; index++)
                {
                    hash ^= text[index];
                    hash *= 16777619u;
                }
                return hash;
            }
        }
    }
}
