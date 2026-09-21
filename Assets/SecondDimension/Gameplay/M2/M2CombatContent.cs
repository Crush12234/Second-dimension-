using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;

namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2CombatContent
    {
        public const string TutorialEnemyUnionId = "EU_GNAWER_PACK";

        private readonly Dictionary<string, M2CommandDefinition> _commands;
        private readonly Dictionary<string, M2ArtDefinition> _arts;
        private readonly Dictionary<string, M2FormationDefinition> _formations;
        private readonly Dictionary<string, M2EnemyDefinition> _enemies;

        private M2CombatContent(
            string contentVersion,
            Dictionary<string, M2CommandDefinition> commands,
            Dictionary<string, M2ArtDefinition> arts,
            Dictionary<string, M2FormationDefinition> formations,
            Dictionary<string, M2EnemyDefinition> enemies,
            M2EnemyUnionDefinition enemyUnion,
            int earlyEligible,
            int guaranteedThreshold,
            DeepProgressionCatalog070 deepProgression = null,
            HeroMasterGeneratedBattleAuthority096 heroMasterGeneratedAuthority096 = null)
        {
            ContentVersion = contentVersion;
            _commands = commands;
            _arts = arts;
            _formations = formations;
            _enemies = enemies;
            TutorialEnemyUnion = enemyUnion;
            EarlyBreakthroughEligible = earlyEligible;
            GuaranteedBreakthroughThreshold = guaranteedThreshold;
            DeepProgression = deepProgression;
            HeroMasterGeneratedAuthority096 = heroMasterGeneratedAuthority096;
            DeepTreeProgression = deepProgression == null
                ? null
                : new RecruitTreeProgressionService070(deepProgression);
            ValidateRequiredAuthority();
        }

        public string ContentVersion { get; }
        public M2EnemyUnionDefinition TutorialEnemyUnion { get; }
        public int EarlyBreakthroughEligible { get; }
        public int GuaranteedBreakthroughThreshold { get; }
        public DeepProgressionCatalog070 DeepProgression { get; }
        public RecruitTreeProgressionService070 DeepTreeProgression { get; }
        public HeroMasterGeneratedBattleAuthority096 HeroMasterGeneratedAuthority096 { get; }

        public M2CombatContent WithHeroMasterGeneratedAuthority096(
            HeroMaster300Catalog087 heroes, RecruitAutoGenerationCatalog010 generated)
        {
            return new M2CombatContent(ContentVersion,
                new Dictionary<string, M2CommandDefinition>(_commands, StringComparer.Ordinal),
                new Dictionary<string, M2ArtDefinition>(_arts, StringComparer.Ordinal),
                new Dictionary<string, M2FormationDefinition>(_formations, StringComparer.Ordinal),
                new Dictionary<string, M2EnemyDefinition>(_enemies, StringComparer.Ordinal),
                TutorialEnemyUnion, EarlyBreakthroughEligible, GuaranteedBreakthroughThreshold,
                DeepProgression, new HeroMasterGeneratedBattleAuthority096(heroes, generated, DeepProgression));
        }
        public IReadOnlyDictionary<string, M2CommandDefinition> Commands => _commands;
        public IReadOnlyDictionary<string, M2ArtDefinition> Arts => _arts;

        public M2CommandDefinition Command(string id) =>
            _commands.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException("Unknown combat command " + id + ".");
        public M2ArtDefinition Art(string id) =>
            _arts.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException("Unknown combat Art " + id + ".");
        public M2FormationDefinition Formation(string id) =>
            _formations.TryGetValue(id ?? string.Empty, out var value)
                ? value
                : new M2FormationDefinition(id ?? string.Empty, Humanize(id), new[] { 3, 4, 5 }, 0, Array.Empty<string>());
        public M2EnemyDefinition Enemy(string id) =>
            TryEnemy(id, out var value) ? value : throw new KeyNotFoundException("Unknown enemy " + id + ".");
        public bool TryEnemy(string id, out M2EnemyDefinition value) =>
            _enemies.TryGetValue(id ?? string.Empty, out value) || TryTitanEnemy161(id, out value);

        public static M2CombatContent LoadFromDirectory(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot)) throw new ArgumentException("Content root is required.", nameof(contentRoot));
            var legacy = FromJson(
                File.ReadAllText(Resolve(contentRoot, "PASS_02", "UNION_COMMANDS.json")),
                File.ReadAllText(Resolve(contentRoot, "PASS_02", "ART_DEFINITIONS.json")),
                File.ReadAllText(Resolve(contentRoot, "PASS_02", "FORMATIONS.json")),
                File.ReadAllText(Resolve(contentRoot, "PASS_03", "ENEMY_DEFINITIONS.json")),
                File.ReadAllText(Resolve(contentRoot, "PASS_03", "ENEMY_UNIONS.json")),
                File.ReadAllText(Resolve(contentRoot, "PASS_02", "LEARN_BY_USE_SPEC.json")));
            return legacy
                .WithDeepProgression(
                    DeepProgressionCatalog070.LoadFromContentRoot(contentRoot))
                .WithSssTenV4Arts090().WithTitanHeroes161();
        }

        private M2CombatContent WithSssTenV4Arts090()
        {
            var arts = new Dictionary<string, M2ArtDefinition>(
                _arts, StringComparer.Ordinal);
            var authored = SssTenV4ArtRegistry090.Build();
            for (var index = 0; index < authored.Count; index++)
            {
                var art = authored[index];
                if (arts.ContainsKey(art.Id))
                    throw new InvalidDataException(
                        "SSS Ten V4 Art ID collides with live M2 content: " +
                        art.Id);
                arts.Add(art.Id, art);
            }
            return new M2CombatContent(
                ContentVersion + "|" +
                SssTenV4ArtRegistry090.ContentVersion090,
                new Dictionary<string, M2CommandDefinition>(
                    _commands, StringComparer.Ordinal),
                arts,
                new Dictionary<string, M2FormationDefinition>(
                    _formations, StringComparer.Ordinal),
                new Dictionary<string, M2EnemyDefinition>(
                    _enemies, StringComparer.Ordinal),
                TutorialEnemyUnion,
                EarlyBreakthroughEligible,
                GuaranteedBreakthroughThreshold,
                DeepProgression, HeroMasterGeneratedAuthority096);
        }

        private M2CombatContent WithDeepProgression(DeepProgressionCatalog070 catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var arts = new Dictionary<string, M2ArtDefinition>(_arts, StringComparer.Ordinal);
            foreach (var runtime in catalog.RuntimeArts)
            {
                if (arts.ContainsKey(runtime.NodeId))
                    throw new InvalidDataException("Deep Art ID collides with legacy battle authority: " + runtime.NodeId);
                var animationTag = runtime.AnimationTags.Count > 0
                    ? runtime.AnimationTags[0]
                    : "DEEP_ART";
                var forecastAction = StringComparer.Ordinal.Equals(runtime.NodeType, "ACTION") ||
                                     StringComparer.Ordinal.Equals(runtime.NodeType, "STANCE") ||
                                     StringComparer.Ordinal.Equals(runtime.NodeType, "SIGNATURE");
                var effects = new List<string>(runtime.EffectTypes);
                // The skill authority explicitly maps these learned nodes to Stand
                // Again. Preserve that capability through the runtime adapter; the
                // node keeps its own authored costs, equipment and target rule.
                if (forecastAction && runtime.Discipline == "Restoration" &&
                    runtime.LegacyArtIds.Contains("ART_STAND_AGAIN") &&
                    _arts.TryGetValue("ART_STAND_AGAIN", out var revivalAuthority) &&
                    (revivalAuthority.EffectTags.Contains("REVIVE") ||
                     revivalAuthority.StatusTags.Contains("REVIVE")) &&
                    !effects.Contains("REVIVE"))
                    effects.Add("REVIVE");
                arts.Add(runtime.NodeId, new M2ArtDefinition(
                    runtime.NodeId,
                    runtime.DisplayName,
                    runtime.ArtClass,
                    runtime.Discipline,
                    runtime.RequiredEquipmentTagsAny,
                    runtime.SharedApCost,
                    runtime.PersonalMpCost,
                    runtime.ForecastIntentTags,
                    runtime.MeaningfulUseDefinition,
                    animationTag,
                    false,
                    runtime.TreeId,
                    runtime.NodeType,
                    runtime.PowerCoefficientPermille,
                    forecastAction,
                    effectTags: effects.AsReadOnly(),
                    targetRule: runtime.TargetRule,
                    areaProfile095: M2AreaArtProfile095.FromAuthored(runtime.NodeId,
                        runtime.MaximumTargets095, runtime.DamageBudgetCoefficient095,
                        runtime.CohesionBudgetCoefficient095, runtime.FormationBudgetCoefficient095)));
            }
            return new M2CombatContent(
                ContentVersion + "|" + DeepProgressionCatalog070.ContentVersion,
                new Dictionary<string, M2CommandDefinition>(_commands, StringComparer.Ordinal),
                arts,
                new Dictionary<string, M2FormationDefinition>(_formations, StringComparer.Ordinal),
                new Dictionary<string, M2EnemyDefinition>(_enemies, StringComparer.Ordinal),
                TutorialEnemyUnion,
                EarlyBreakthroughEligible,
                GuaranteedBreakthroughThreshold,
                catalog, HeroMasterGeneratedAuthority096);
        }

        public static M2CombatContent FromJson(
            string commandsJson, string artsJson, string formationsJson,
            string enemiesJson, string enemyUnionsJson, string learningJson)
        {
            var commandRoot = JObject.Parse(commandsJson);
            var artRoot = JObject.Parse(artsJson);
            var formationRoot = JObject.Parse(formationsJson);
            var enemyRoot = JObject.Parse(enemiesJson);
            var unionRoot = JObject.Parse(enemyUnionsJson);
            var learningRoot = JObject.Parse(learningJson);

            var commands = new Dictionary<string, M2CommandDefinition>(StringComparer.Ordinal);
            foreach (var token in RequiredArray(commandRoot, "commands"))
            {
                var item = (JObject)token;
                var phrases = Strings(item["phraseBank"]);
                commands.Add(Required(item, "id"), new M2CommandDefinition(
                    Required(item, "id"), Required(item, "displayName"), Required(item, "tacticalIntent"),
                    phrases, Required(item, "fallbackBehavior")));
            }

            var arts = new Dictionary<string, M2ArtDefinition>(StringComparer.Ordinal);
            foreach (var token in RequiredArray(artRoot, "arts"))
            {
                var item = (JObject)token;
                var id = Required(item, "id");
                arts.Add(id, new M2ArtDefinition(
                    id, Required(item, "displayName"), Required(item, "family"), Required(item, "discipline"),
                    Strings(item["requiredEquipmentTags"]), RequiredInt(item, "sharedApCost"),
                    RequiredInt(item, "personalMpCost"), Strings(item["forecastIntentTags"]),
                    Required(item, "meaningfulUseDefinition"), Required(item, "animationTag"),
                    item["playerDirectlySelectableInStandard"]?.Value<bool>() ?? true,
                    effectTags: Strings(item["effectFormula"]),
                    statusTags: Strings(item["statusTags"]),
                    targetRule: Required(item, "targeting")));
            }

            EnemyReferencedSupplementalArts094.AddToRuntime094(arts, artRoot, enemyRoot);
            BasicStrikeFallback101.AddToRuntime(arts);

            var formations = new Dictionary<string, M2FormationDefinition>(StringComparer.Ordinal);
            foreach (var token in RequiredArray(formationRoot, "formations"))
            {
                var item = (JObject)token;
                var id = Required(item, "id");
                formations.Add(id, new M2FormationDefinition(
                    id, Required(item, "displayName"), Integers(item["validMemberCount"]),
                    item["cohesionBonus"]?.Value<int>() ?? 0, Strings(item["strengths"])));
            }

            var enemies = new Dictionary<string, M2EnemyDefinition>(StringComparer.Ordinal);
            foreach (var token in RequiredArray(enemyRoot, "enemies"))
            {
                var item = (JObject)token;
                var id = Required(item, "id");
                enemies.Add(id, new M2EnemyDefinition(
                    id, Required(item, "displayName"), RequiredInt(item, "maxHp"), RequiredInt(item, "maxMp"),
                    RequiredInt(item, "unionApContribution"), RequiredInt(item, "cohesionContribution"),
                    Strings(item["artIds"]), RequiredInt(item, "personalXpReward"),
                    RequiredInt(item, "guildTreasuryXpReward")));
            }

            M2EnemyUnionDefinition tutorialUnion = null;
            foreach (var token in RequiredArray(unionRoot, "enemyUnions"))
            {
                var item = (JObject)token;
                if (!StringComparer.Ordinal.Equals(Required(item, "id"), TutorialEnemyUnionId)) continue;
                tutorialUnion = new M2EnemyUnionDefinition(
                    TutorialEnemyUnionId, Required(item, "name"), Required(item, "leaderMemberId"),
                    Required(item, "formationId"), RequiredInt(item, "sharedApBase"),
                    RequiredInt(item, "cohesionBase"), Strings(item["memberIds"]),
                    RequiredInt(item, "rewardMultiplierPermille"));
                break;
            }
            if (tutorialUnion == null) throw new InvalidDataException("Tutorial enemy Union authority is missing.");

            var early = learningRoot["earlyBreakthrough"]?["eligibleDiscovery"]?.Value<int>() ?? -1;
            var threshold = learningRoot["guaranteedLearning"]?["threshold"]?.Value<int>() ?? -1;
            if (learningRoot["guaranteedLearning"]?["cannotFail"]?.Value<bool>() != true)
                throw new InvalidDataException("Guaranteed learning authority must be cannot-fail.");
            var version = string.Join("|", new[]
            {
                Required(commandRoot, "contentVersion"), Required(artRoot, "contentVersion"),
                Required(formationRoot, "contentVersion"), Required(enemyRoot, "contentVersion"),
                Required(unionRoot, "contentVersion"), Required(learningRoot, "contentVersion")
            });
            return new M2CombatContent(version, commands, arts, formations, enemies, tutorialUnion, early, threshold);
        }

        private void ValidateRequiredAuthority()
        {
            foreach (var id in new[]
            {
                "CMD_BALANCED", "CMD_ALL_OUT", "CMD_GUARD", "CMD_HEAL", "CMD_MYSTIC",
                "CMD_AP_RECOVERY", "CMD_SUPPORT", "CMD_FLANK", "CMD_RETREAT"
            }) Command(id);
            foreach (var id in new[]
            {
                "ART_GUARD", "ART_RECOVER_BREATH", "ART_BASIC_SABER_CUT", "ART_BASIC_STAFF_TAP",
                "ART_BASIC_RUNE_BOLT", "ART_POWER_CUT", "ART_SHIELD_BRACE", "ART_PIERCING_SHOT",
                "ART_QUICK_CUT", "ART_EMBER_BOLT", "ART_MINOR_REMEDY"
            })
            {
                var art = Art(id);
                if (art.PlayerDirectlySelectableInStandard)
                    throw new InvalidDataException(id + " violates complete-Forecast Standard combat authority.");
            }
            for (var i = 0; i < TutorialEnemyUnion.MemberIds.Count; i++) Enemy(TutorialEnemyUnion.MemberIds[i]);
            if (TutorialEnemyUnion.RewardMultiplierPermille <= 0)
                throw new InvalidDataException("Tutorial enemy Union reward multiplier must be positive.");
            for (var i = 0; i < TutorialEnemyUnion.MemberIds.Count; i++)
            {
                var enemy = Enemy(TutorialEnemyUnion.MemberIds[i]);
                if (enemy.PersonalXpReward <= 0 || enemy.GuildTreasuryXpReward <= 0)
                    throw new InvalidDataException("Tutorial enemy rewards must be positive for " + enemy.Id + ".");
            }
            if (EarlyBreakthroughEligible != 60 || GuaranteedBreakthroughThreshold != 100)
                throw new InvalidDataException("Tutorial breakthrough thresholds drifted from Pass 02 authority.");
        }

        private static string Resolve(string root, string pass, string file)
        {
            var nested = Path.Combine(root, pass, file);
            if (File.Exists(nested)) return nested;
            var flat = Path.Combine(root, file);
            if (File.Exists(flat)) return flat;
            throw new FileNotFoundException("Required M2 authority file is missing.", nested);
        }
        private static JArray RequiredArray(JObject root, string name) =>
            root[name] as JArray ?? throw new InvalidDataException(name + " array is missing.");
        private static string Required(JObject item, string name)
        {
            var value = item[name]?.Value<string>();
            return string.IsNullOrWhiteSpace(value) ? throw new InvalidDataException(name + " is missing.") : value;
        }
        private static int RequiredInt(JObject item, string name) =>
            item[name]?.Type == JTokenType.Integer ? item[name].Value<int>() : throw new InvalidDataException(name + " is missing.");
        private static IReadOnlyList<string> Strings(JToken token)
        {
            var result = new List<string>();
            if (token is JArray values) foreach (var item in values) result.Add(item.Value<string>() ?? string.Empty);
            return result.AsReadOnly();
        }
        private static IReadOnlyList<int> Integers(JToken token)
        {
            var result = new List<int>();
            if (token is JArray values) foreach (var item in values) result.Add(item.Value<int>());
            return result.AsReadOnly();
        }
        private static string Humanize(string id) => string.IsNullOrWhiteSpace(id) ? "Open Formation" : id.Replace("FORMATION_", string.Empty).Replace('_', ' ');
    }

    public sealed class M2CommandDefinition
    {
        public M2CommandDefinition(string id, string name, string intent, IReadOnlyList<string> phrases, string fallback)
        { Id = id; Name = name; Intent = intent; Phrases = phrases; Fallback = fallback; }
        public string Id { get; }
        public string Name { get; }
        public string Intent { get; }
        public IReadOnlyList<string> Phrases { get; }
        public string Fallback { get; }
    }

    public sealed class M2ArtDefinition
    {
        public M2ArtDefinition(string id, string name, string family, string discipline,
            IReadOnlyList<string> requiredEquipmentTags, int ap, int mp, IReadOnlyList<string> intentTags,
            string meaningfulUse, string animationTag, bool directlySelectable,
            string treeId = null, string nodeType = null, int powerCoefficientPermille = 1000,
            bool forecastAction = true, IReadOnlyList<string> effectTags = null,
            IReadOnlyList<string> statusTags = null, string targetRule = null,
            M2AreaArtProfile095 areaProfile095 = null)
        {
            Id = id; Name = name; Family = family; Discipline = discipline;
            RequiredEquipmentTags = requiredEquipmentTags; SharedApCost = ap; PersonalMpCost = mp;
            IntentTags = intentTags; MeaningfulUse = meaningfulUse; AnimationTag = animationTag;
            PlayerDirectlySelectableInStandard = directlySelectable;
            TreeId = treeId ?? string.Empty;
            NodeType = nodeType ?? "LEGACY_ACTION";
            PowerCoefficientPermille = Math.Max(1, powerCoefficientPermille);
            IsForecastAction = forecastAction;
            EffectTags = effectTags ?? Array.Empty<string>();
            StatusTags = statusTags ?? Array.Empty<string>();
            TargetRule = targetRule ?? string.Empty;
            AreaProfile095 = areaProfile095 ?? M2AreaArtProfile095.Legacy(id);
        }
        public string Id { get; }
        public string Name { get; }
        public string Family { get; }
        public string Discipline { get; }
        public IReadOnlyList<string> RequiredEquipmentTags { get; }
        public int SharedApCost { get; }
        public int PersonalMpCost { get; }
        public IReadOnlyList<string> IntentTags { get; }
        public string MeaningfulUse { get; }
        public string AnimationTag { get; }
        public bool PlayerDirectlySelectableInStandard { get; }
        public string TreeId { get; }
        public string NodeType { get; }
        public int PowerCoefficientPermille { get; }
        public bool IsForecastAction { get; }
        public IReadOnlyList<string> EffectTags { get; }
        public IReadOnlyList<string> StatusTags { get; }
        public string TargetRule { get; }
        public M2AreaArtProfile095 AreaProfile095 { get; }
    }

    public sealed class M2FormationDefinition
    {
        public M2FormationDefinition(string id, string name, IReadOnlyList<int> validMemberCounts, int cohesionBonus, IReadOnlyList<string> strengths)
        { Id = id; Name = name; ValidMemberCounts = validMemberCounts; CohesionBonus = cohesionBonus; Strengths = strengths; }
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<int> ValidMemberCounts { get; }
        public int CohesionBonus { get; }
        public IReadOnlyList<string> Strengths { get; }
        public bool Supports(int memberCount)
        {
            for (var i = 0; i < ValidMemberCounts.Count; i++) if (ValidMemberCounts[i] == memberCount) return true;
            return false;
        }
    }

    public sealed class M2EnemyDefinition
    {
        public M2EnemyDefinition(
            string id,
            string name,
            int maxHp,
            int maxMp,
            int ap,
            int cohesion,
            IReadOnlyList<string> artIds,
            int personalXpReward,
            int guildTreasuryXpReward)
        {
            Id = id;
            Name = name;
            MaximumHp = maxHp;
            MaximumMp = maxMp;
            ApContribution = ap;
            CohesionContribution = cohesion;
            ArtIds = artIds;
            PersonalXpReward = personalXpReward;
            GuildTreasuryXpReward = guildTreasuryXpReward;
        }
        public string Id { get; }
        public string Name { get; }
        public int MaximumHp { get; }
        public int MaximumMp { get; }
        public int ApContribution { get; }
        public int CohesionContribution { get; }
        public IReadOnlyList<string> ArtIds { get; }
        public int PersonalXpReward { get; }
        public int GuildTreasuryXpReward { get; }
    }

    public sealed class M2EnemyUnionDefinition
    {
        public M2EnemyUnionDefinition(
            string id,
            string name,
            string leaderId,
            string formationId,
            int ap,
            int cohesion,
            IReadOnlyList<string> members,
            int rewardMultiplierPermille)
        {
            Id = id;
            Name = name;
            LeaderId = leaderId;
            FormationId = formationId;
            SharedApBase = ap;
            CohesionBase = cohesion;
            MemberIds = members;
            RewardMultiplierPermille = rewardMultiplierPermille;
        }
        public string Id { get; }
        public string Name { get; }
        public string LeaderId { get; }
        public string FormationId { get; }
        public int SharedApBase { get; }
        public int CohesionBase { get; }
        public IReadOnlyList<string> MemberIds { get; }
        public int RewardMultiplierPermille { get; }
    }
}
