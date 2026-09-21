using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// One enemy spawned into one resolved encounter. SourceEnemyId remains the
    /// content-authority identity; MemberId is battle-run-local and therefore
    /// cannot collide when two authored enemy Unions share a support member.
    /// </summary>
    public sealed class EncounterEnemyMember070
    {
        public EncounterEnemyMember070(
            string memberId,
            string sourceEnemyId,
            string familyId,
            int visualVariantSeed,
            M2EnemyDefinition definition)
        {
            MemberId = Require(memberId, nameof(memberId));
            SourceEnemyId = Require(sourceEnemyId, nameof(sourceEnemyId));
            FamilyId = Require(familyId, nameof(familyId));
            VisualVariantSeed = visualVariantSeed;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public string MemberId { get; }
        public string SourceEnemyId { get; }
        public string FamilyId { get; }
        public int VisualVariantSeed { get; }
        public M2EnemyDefinition Definition { get; }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    /// <summary>A real Pass 03 enemy Union selected for a Version 70 encounter.</summary>
    public sealed class EncounterEnemyUnion070
    {
        public EncounterEnemyUnion070(
            string unionId,
            string sourceUnionId,
            M2EnemyUnionDefinition sourceDefinition,
            IReadOnlyList<EncounterEnemyMember070> members,
            IReadOnlyList<string> familyIds)
        {
            UnionId = Require(unionId, nameof(unionId));
            SourceUnionId = Require(sourceUnionId, nameof(sourceUnionId));
            SourceDefinition = sourceDefinition ?? throw new ArgumentNullException(nameof(sourceDefinition));
            Members = CopyMembers(members);
            FamilyIds = CopyUnique(familyIds);
            if (Members.Count == 0) throw new ArgumentException("Enemy Union requires members.", nameof(members));
            if (FamilyIds.Count == 0) throw new ArgumentException("Enemy Union requires a family.", nameof(familyIds));
            var leader = Members.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.SourceEnemyId, SourceDefinition.LeaderId));
            if (leader == null)
                throw new ArgumentException("Resolved enemy Union is missing its source leader.", nameof(members));
            Definition = new M2EnemyUnionDefinition(
                UnionId,
                SourceDefinition.Name,
                leader.MemberId,
                SourceDefinition.FormationId,
                SourceDefinition.SharedApBase,
                SourceDefinition.CohesionBase,
                Members.Select(value => value.MemberId).ToArray(),
                SourceDefinition.RewardMultiplierPermille);
        }

        public string UnionId { get; }
        public string SourceUnionId { get; }
        public M2EnemyUnionDefinition SourceDefinition { get; }
        public M2EnemyUnionDefinition Definition { get; }
        public string LeaderMemberId => Definition.LeaderId;
        public IReadOnlyList<EncounterEnemyMember070> Members { get; }
        public IReadOnlyList<string> FamilyIds { get; }

        private static IReadOnlyList<EncounterEnemyMember070> CopyMembers(
            IReadOnlyList<EncounterEnemyMember070> values)
        {
            var result = new List<EncounterEnemyMember070>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    var value = values[index] ?? throw new ArgumentException(
                        "Enemy member cannot be null.", nameof(values));
                    if (result.Any(existing => StringComparer.Ordinal.Equals(existing.MemberId, value.MemberId)))
                        throw new ArgumentException("Resolved enemy member IDs must be unique.", nameof(values));
                    result.Add(value);
                }
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    var value = Require(values[index], nameof(values));
                    if (!result.Contains(value)) result.Add(value);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    /// <summary>
    /// Immutable roster commitment. Reopening the same encounter recreates this
    /// exact identity and cannot reroll enemy families or visual variants.
    /// </summary>
    public sealed class EncounterRoster070
    {
        public EncounterRoster070(
            string rosterId,
            string canonicalSeedIdentity,
            IReadOnlyList<EncounterEnemyUnion070> unions,
            IReadOnlyList<string> familyIds)
        {
            RosterId = Require(rosterId, nameof(rosterId));
            CanonicalSeedIdentity = Require(canonicalSeedIdentity, nameof(canonicalSeedIdentity));
            Unions = CopyUnions(unions);
            FamilyIds = CopyUnique(familyIds);
            if (Unions.Count == 0) throw new ArgumentException("Encounter roster requires a Union.", nameof(unions));
        }

        public string RosterId { get; }
        public string CanonicalSeedIdentity { get; }
        public IReadOnlyList<EncounterEnemyUnion070> Unions { get; }
        public IReadOnlyList<string> FamilyIds { get; }

        private static IReadOnlyList<EncounterEnemyUnion070> CopyUnions(
            IReadOnlyList<EncounterEnemyUnion070> values)
        {
            var result = new List<EncounterEnemyUnion070>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    var value = values[index] ?? throw new ArgumentException(
                        "Encounter Union cannot be null.", nameof(values));
                    if (result.Any(existing => StringComparer.Ordinal.Equals(existing.UnionId, value.UnionId)))
                        throw new ArgumentException("Resolved enemy Union IDs must be unique.", nameof(values));
                    result.Add(value);
                }
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    var value = Require(values[index], nameof(values));
                    if (!result.Contains(value)) result.Add(value);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    /// <summary>
    /// Loads all real Pass 03 enemy definitions and Unions, then deterministically
    /// commits a diverse encounter from contract, board, encounter and campaign seed.
    /// This replaces the presentation-era behavior of cloning EU_GNAWER_PACK for
    /// every enemy slot without changing Pass 03 authority.
    /// </summary>
    public sealed class EncounterRosterResolver070
    {
        public const string RulesVersion = "ENCOUNTER_ROSTER_070_V1";

        private readonly Dictionary<string, EnemyAuthority070> _enemies;
        private readonly IReadOnlyList<EnemyUnionAuthority070> _unions;
        private readonly IReadOnlyList<string> _familyIds;

        private EncounterRosterResolver070(
            Dictionary<string, EnemyAuthority070> enemies,
            IReadOnlyList<EnemyUnionAuthority070> unions)
        {
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _unions = unions ?? throw new ArgumentNullException(nameof(unions));
            var families = new List<string>();
            foreach (var enemy in _enemies.Values)
                if (!families.Contains(enemy.FamilyId)) families.Add(enemy.FamilyId);
            families.Sort(StringComparer.Ordinal);
            _familyIds = families.AsReadOnly();
            ValidateAuthority();
        }

        public int EnemyDefinitionCount => _enemies.Count;
        public int EnemyUnionDefinitionCount => _unions.Count;
        public int EnemyFamilyCount => _familyIds.Count;
        public IReadOnlyList<string> EnemyFamilyIds => _familyIds;

        public static EncounterRosterResolver070 LoadFromContentRoot(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot))
                throw new ArgumentException("Content root is required.", nameof(contentRoot));
            return FromJson(
                File.ReadAllText(Path.Combine(contentRoot, "PASS_03", "ENEMY_DEFINITIONS.json")),
                File.ReadAllText(Path.Combine(contentRoot, "PASS_03", "ENEMY_UNIONS.json")));
        }

        public static EncounterRosterResolver070 FromJson(string enemiesJson, string unionsJson)
        {
            var enemyRoot = JObject.Parse(enemiesJson ?? throw new ArgumentNullException(nameof(enemiesJson)));
            var unionRoot = JObject.Parse(unionsJson ?? throw new ArgumentNullException(nameof(unionsJson)));
            var enemies = new Dictionary<string, EnemyAuthority070>(StringComparer.Ordinal);
            foreach (var token in RequiredArray(enemyRoot, "enemies"))
            {
                var item = token as JObject ?? throw new InvalidDataException("Enemy entry must be an object.");
                var id = Required(item, "id");
                var authority = new EnemyAuthority070(
                    id,
                    Required(item, "familyId"),
                    Required(item, "displayName"),
                    RequiredInt(item, "visualVariantSeed"),
                    new M2EnemyDefinition(
                        id,
                        Required(item, "displayName"),
                        RequiredInt(item, "maxHp"),
                        RequiredInt(item, "maxMp"),
                        RequiredInt(item, "unionApContribution"),
                        RequiredInt(item, "cohesionContribution"),
                        Strings(item["artIds"]),
                        RequiredInt(item, "personalXpReward"),
                        RequiredInt(item, "guildTreasuryXpReward")));
                if (enemies.ContainsKey(id)) throw new InvalidDataException("Duplicate enemy ID: " + id + ".");
                enemies.Add(id, authority);
            }

            var unions = new List<EnemyUnionAuthority070>();
            foreach (var token in RequiredArray(unionRoot, "enemyUnions"))
            {
                var item = token as JObject ?? throw new InvalidDataException("Enemy Union entry must be an object.");
                var id = Required(item, "id");
                if (unions.Any(value => StringComparer.Ordinal.Equals(value.Definition.Id, id)))
                    throw new InvalidDataException("Duplicate enemy Union ID: " + id + ".");
                var memberIds = Strings(item["memberIds"]);
                var definition = new M2EnemyUnionDefinition(
                    id,
                    Required(item, "name"),
                    Required(item, "leaderMemberId"),
                    Required(item, "formationId"),
                    RequiredInt(item, "sharedApBase"),
                    RequiredInt(item, "cohesionBase"),
                    memberIds,
                    RequiredInt(item, "rewardMultiplierPermille"));
                unions.Add(new EnemyUnionAuthority070(definition, memberIds));
            }
            unions.Sort((left, right) => StringComparer.Ordinal.Compare(left.Definition.Id, right.Definition.Id));
            return new EncounterRosterResolver070(enemies, unions.AsReadOnly());
        }

        public EncounterRoster070 Resolve(long campaignSeed, EncounterLaunchRequest017D request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return ResolveCore094(
                campaignSeed,
                request.ContractId,
                request.BoardId,
                request.EncounterId,
                request.EnemyUnionCount,
                request.CanonicalSeedIdentity,
                EnemyForceProfile094.ReadCommitted094(request),
                TowerEnemyPartyRules137.ReadCommitted137(request));
        }

        public EncounterRoster070 Resolve(
            long campaignSeed,
            string contractId,
            string boardId,
            string encounterId,
            int enemyUnionCount,
            string canonicalSeedIdentity = "UNSPECIFIED") =>
            ResolveCore094(campaignSeed, contractId, boardId, encounterId,
                enemyUnionCount, canonicalSeedIdentity, null);

        private EncounterRoster070 ResolveCore094(
            long campaignSeed, string contractId, string boardId,
            string encounterId, int enemyUnionCount, string canonicalSeedIdentity,
            EnemyForceProfile094 force094, bool fullTowerParties137 = false)
        {
            contractId = Require(contractId, nameof(contractId));
            boardId = Require(boardId, nameof(boardId));
            encounterId = Require(encounterId, nameof(encounterId));
            canonicalSeedIdentity = Require(canonicalSeedIdentity, nameof(canonicalSeedIdentity));
            enemyUnionCount = Math.Max(1, Math.Min(10, enemyUnionCount));
            if (enemyUnionCount > _unions.Count)
                throw new InvalidOperationException("Enemy Union authority cannot satisfy the requested roster size.");

            var context = (contractId + " " + boardId + " " + encounterId).ToUpperInvariant();
            var ranked = new List<RankedUnion070>();
            for (var index = 0; index < _unions.Count; index++)
            {
                var source = _unions[index];
                var rng = Pcg32.FromParts(
                    RulesVersion,
                    campaignSeed,
                    contractId,
                    boardId,
                    encounterId,
                    canonicalSeedIdentity,
                    source.Definition.Id);
                ranked.Add(new RankedUnion070(
                    source,
                    Affinity(context, source),
                    rng.NextUInt32()));
            }
            ranked.Sort(RankedUnion070.Compare);
            if (force094 != null || fullTowerParties137)
            {
                // Keep the primary authored story boss once, never populate an
                // expanded army with unrelated named bosses or clone its leader.
                var primary094 = ranked.Count > 0 &&
                    Families(ranked[0].Authority).Any(EnemyForceProfile094.IsNamedBossFamily094)
                    ? ranked[0].Authority : null;
                ranked.RemoveAll(value => value.Authority != primary094 &&
                    Families(value.Authority).Any(EnemyForceProfile094.IsNamedBossFamily094));
                if (ranked.Count < enemyUnionCount)
                    throw new InvalidOperationException("ENEMY_FORCE094_LEGAL_UNIONS_INSUFFICIENT");
            }

            var selected = new List<EnemyUnionAuthority070>();
            var selectedFamilies = new HashSet<string>(StringComparer.Ordinal);
            for (var pass = 0; pass < 2 && selected.Count < enemyUnionCount; pass++)
            {
                for (var index = 0; index < ranked.Count && selected.Count < enemyUnionCount; index++)
                {
                    var candidate = ranked[index].Authority;
                    if (selected.Contains(candidate)) continue;
                    var families = Families(candidate);
                    if (pass == 0 && families.All(selectedFamilies.Contains)) continue;
                    selected.Add(candidate);
                    for (var familyIndex = 0; familyIndex < families.Count; familyIndex++)
                        selectedFamilies.Add(families[familyIndex]);
                }
            }

            var selectedSourceIds = selected.Select(value => value.Definition.Id).ToArray();
            var rosterHash = CanonicalJson.Sha256Hex(new
            {
                RulesVersion,
                campaignSeed,
                contractId,
                boardId,
                encounterId,
                canonicalSeedIdentity,
                SelectedSourceUnionIds = selectedSourceIds
            });
            if (force094 != null)
                rosterHash = CanonicalJson.Sha256Hex(new
                {
                    RulesVersion, campaignSeed, contractId, boardId, encounterId,
                    canonicalSeedIdentity, SelectedSourceUnionIds = selectedSourceIds,
                    ForceProfile094 = force094.Tag094
                });
            if (fullTowerParties137)
                rosterHash = CanonicalJson.Sha256Hex(new
                {
                    RulesVersion, campaignSeed, contractId, boardId, encounterId,
                    canonicalSeedIdentity, SelectedSourceUnionIds = selectedSourceIds,
                    TowerPartyPolicy137 = TowerEnemyPartyRules137.Modifier137
                });
            var minimumMembers137 = fullTowerParties137 ? TowerEnemyPartyRules137.MemberCount137 : force094?.MemberCount094 ?? 0;
            var compositionTag137 = fullTowerParties137 ? TowerEnemyPartyRules137.Modifier137 : force094?.Tag094;
            var rosterId = "ENCOUNTER_ROSTER_070_" + rosterHash.Substring(0, 24).ToUpperInvariant();
            var resolved = new List<EncounterEnemyUnion070>();
            var allFamilies = new List<string>();
            for (var unionIndex = 0; unionIndex < selected.Count; unionIndex++)
            {
                var source = selected[unionIndex];
                var unionHash = CanonicalJson.Sha256Hex(new { rosterId, unionIndex, source.Definition.Id });
                var unionId = "ENEMY_UNION_070_" + unionHash.Substring(0, 20).ToUpperInvariant();
                var members = new List<EncounterEnemyMember070>();
                var memberIds094 = new List<string>(source.MemberIds);
                if (memberIds094.Count < minimumMembers137)
                {
                    var escorts094 = source.MemberIds.Where(id =>
                        !EnemyForceProfile094.IsNamedBossFamily094(_enemies[id].FamilyId)).ToArray();
                    if (escorts094.Length == 0)
                        throw new InvalidOperationException("ENEMY_FORCE094_LEGAL_ESCORTS_REQUIRED");
                    var rng094 = Pcg32.FromParts(RulesVersion, campaignSeed, contractId,
                        boardId, encounterId, canonicalSeedIdentity, source.Definition.Id,
                        compositionTag137, "ESCORTS094");
                    var cursor094 = (int)(rng094.NextUInt32() % (uint)escorts094.Length);
                    while (memberIds094.Count < minimumMembers137)
                    {
                        memberIds094.Add(escorts094[cursor094 % escorts094.Length]);
                        cursor094++;
                    }
                }
                for (var memberIndex = 0; memberIndex < memberIds094.Count; memberIndex++)
                {
                    var enemy = _enemies[memberIds094[memberIndex]];
                    var memberHash = CanonicalJson.Sha256Hex(new
                    {
                        rosterId,
                        unionIndex,
                        memberIndex,
                        SourceUnionId = source.Definition.Id,
                        EnemyDefinitionId = enemy.Definition.Id
                    });
                    members.Add(new EncounterEnemyMember070(
                        enemy.Definition.Id + "_SPAWN070_" +
                        memberHash.Substring(0, 12).ToUpperInvariant() +
                        (fullTowerParties137 ? TowerEnemyPartyRules137.SpawnSuffix137 : force094?.SpawnSuffix094 ?? string.Empty),
                        enemy.Definition.Id,
                        enemy.FamilyId,
                        enemy.VisualVariantSeed,
                        enemy.Definition));
                    if (!allFamilies.Contains(enemy.FamilyId)) allFamilies.Add(enemy.FamilyId);
                }
                var unionFamilies = members.Select(value => value.FamilyId).Distinct().ToList();
                unionFamilies.Sort(StringComparer.Ordinal);
                resolved.Add(new EncounterEnemyUnion070(
                    unionId,
                    source.Definition.Id,
                    source.Definition,
                    members.AsReadOnly(),
                    unionFamilies.AsReadOnly()));
            }
            allFamilies.Sort(StringComparer.Ordinal);
            return new EncounterRoster070(
                rosterId,
                canonicalSeedIdentity,
                resolved.AsReadOnly(),
                allFamilies.AsReadOnly());
        }

        private void ValidateAuthority()
        {
            if (_enemies.Count < 2 || _unions.Count < 2 || _familyIds.Count < 2)
                throw new InvalidDataException("Version 70 requires multiple real enemy families and Unions.");
            for (var unionIndex = 0; unionIndex < _unions.Count; unionIndex++)
            {
                var union = _unions[unionIndex];
                if (union.MemberIds.Count == 0)
                    throw new InvalidDataException("Enemy Union has no members: " + union.Definition.Id + ".");
                if (!union.MemberIds.Contains(union.Definition.LeaderId))
                    throw new InvalidDataException("Enemy Union leader is not a member: " + union.Definition.Id + ".");
                for (var memberIndex = 0; memberIndex < union.MemberIds.Count; memberIndex++)
                    if (!_enemies.ContainsKey(union.MemberIds[memberIndex]))
                        throw new InvalidDataException(
                            "Enemy Union references missing enemy " + union.MemberIds[memberIndex] + ".");
            }
        }

        private IReadOnlyList<string> Families(EnemyUnionAuthority070 authority)
        {
            var result = new List<string>();
            for (var index = 0; index < authority.MemberIds.Count; index++)
            {
                var family = _enemies[authority.MemberIds[index]].FamilyId;
                if (!result.Contains(family)) result.Add(family);
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private int Affinity(string context, EnemyUnionAuthority070 authority)
        {
            var id = authority.Definition.Id.ToUpperInvariant();
            var name = authority.Definition.Name.ToUpperInvariant();
            var families = Families(authority);
            var score = 0;
            var sourceTokens = (id + " " + name + " " + string.Join(" ", families)).Split(
                new[] { '_', ' ', '-', '\u2014' }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < sourceTokens.Length; index++)
                if (sourceTokens[index].Length >= 4 && context.Contains(sourceTokens[index])) score += 120;

            if (ContainsAny(context, "GNAWER"))
                score += ContainsAny(id, "GNAWER") ? 1000 : ContainsAny(string.Join(" ", families), "GNAWER") ? 260 : 0;
            if (ContainsAny(context, "RESCUE", "BELL", "CIVILIAN"))
                score += ContainsAny(id, "GNAWER", "RUSTBACK", "HOLLOW", "BRUTE") ? 320 : 0;
            if (ContainsAny(context, "INVESTIGATION", "SURVEY", "LINES_NOT", "MISSING"))
                score += ContainsAny(id, "ECHO", "SHARDWING", "RIFT", "RESONANCE") ? 420 : 0;
            if (ContainsAny(context, "RELIEF", "ROAD", "CONVOY", "PROTECTION", "ESCORT"))
                score += ContainsAny(id, "CUTTER", "RUSTBACK", "BRUTE", "AMBUSH") ? 420 : 0;
            if (ContainsAny(context, "ELITE"))
                score += ContainsAny(id, "PACKLORD", "RAVEL", "GATEHEART", "HINGE") ? 520 : 0;
            var bossContext = ContainsAny(context, "BOSS", "COLOSSUS", "HINGE", "GATE_EATER", "CAPTAIN_RAVEL");
            if (bossContext)
                score += ContainsAny(id, "HINGE", "RAVEL", "GATEHEART") ? 900 : 0;
            else if (ContainsAny(id, "HINGE", "RAVEL"))
                score -= 250;
            return score;
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            if (string.IsNullOrEmpty(source) || values == null) return false;
            for (var index = 0; index < values.Length; index++)
                if (source.IndexOf(values[index], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static JArray RequiredArray(JObject root, string name) =>
            root[name] as JArray ?? throw new InvalidDataException(name + " array is missing.");

        private static string Required(JObject item, string name)
        {
            var value = item[name]?.Value<string>();
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidDataException(name + " is missing.")
                : value;
        }

        private static int RequiredInt(JObject item, string name) =>
            item[name]?.Type == JTokenType.Integer
                ? item[name].Value<int>()
                : throw new InvalidDataException(name + " is missing.");

        private static IReadOnlyList<string> Strings(JToken token)
        {
            var result = new List<string>();
            if (token is JArray values)
            {
                foreach (var item in values)
                {
                    var value = item.Value<string>();
                    if (string.IsNullOrWhiteSpace(value))
                        throw new InvalidDataException("Stable ID array contains an empty value.");
                    result.Add(value);
                }
            }
            return result.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;

        private sealed class EnemyAuthority070
        {
            public EnemyAuthority070(
                string id,
                string familyId,
                string displayName,
                int visualVariantSeed,
                M2EnemyDefinition definition)
            {
                Id = id;
                FamilyId = familyId;
                DisplayName = displayName;
                VisualVariantSeed = visualVariantSeed;
                Definition = definition;
            }

            public string Id { get; }
            public string FamilyId { get; }
            public string DisplayName { get; }
            public int VisualVariantSeed { get; }
            public M2EnemyDefinition Definition { get; }
        }

        private sealed class EnemyUnionAuthority070
        {
            public EnemyUnionAuthority070(
                M2EnemyUnionDefinition definition,
                IReadOnlyList<string> memberIds)
            {
                Definition = definition;
                MemberIds = memberIds;
            }

            public M2EnemyUnionDefinition Definition { get; }
            public IReadOnlyList<string> MemberIds { get; }
        }

        private sealed class RankedUnion070
        {
            public RankedUnion070(EnemyUnionAuthority070 authority, int affinity, uint tieBreak)
            {
                Authority = authority;
                Affinity = affinity;
                TieBreak = tieBreak;
            }

            public EnemyUnionAuthority070 Authority { get; }
            public int Affinity { get; }
            public uint TieBreak { get; }

            public static int Compare(RankedUnion070 left, RankedUnion070 right)
            {
                var affinity = right.Affinity.CompareTo(left.Affinity);
                if (affinity != 0) return affinity;
                var tie = right.TieBreak.CompareTo(left.TieBreak);
                return tie != 0
                    ? tie
                    : StringComparer.Ordinal.Compare(left.Authority.Definition.Id, right.Authority.Definition.Id);
            }
        }
    }
}
