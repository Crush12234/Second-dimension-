using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    public sealed class SssTenV4HeroDefinition090
    {
        internal SssTenV4HeroDefinition090(
            string heroId,
            string displayName,
            string role,
            string classId,
            string primaryDiscipline,
            string secondaryDiscipline,
            int hpWeight,
            int mpWeight,
            int attackWeight,
            int magicWeight,
            int defenseWeight,
            int resistanceWeight,
            int agilityWeight,
            string recruitCode,
            string weaponName,
            string weaponFamilyId,
            string signatureEffectName,
            params string[] aliases)
        {
            HeroId = heroId;
            DisplayName = displayName;
            Role = role;
            ClassId = classId;
            PrimaryDiscipline = primaryDiscipline;
            SecondaryDiscipline = secondaryDiscipline;
            HpWeight = hpWeight;
            MpWeight = mpWeight;
            AttackWeight = attackWeight;
            MagicWeight = magicWeight;
            DefenseWeight = defenseWeight;
            ResistanceWeight = resistanceWeight;
            AgilityWeight = agilityWeight;
            RecruitCode = recruitCode;
            WeaponName = weaponName;
            WeaponFamilyId = weaponFamilyId;
            SignatureEffectName = signatureEffectName;
            Aliases = Array.AsReadOnly(aliases ?? Array.Empty<string>());
        }

        public string HeroId { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public string ClassId { get; }
        public string PrimaryDiscipline { get; }
        public string SecondaryDiscipline { get; }
        public int HpWeight { get; }
        public int MpWeight { get; }
        public int AttackWeight { get; }
        public int MagicWeight { get; }
        public int DefenseWeight { get; }
        public int ResistanceWeight { get; }
        public int AgilityWeight { get; }
        public string RecruitCode { get; }
        public string WeaponName { get; }
        public string WeaponFamilyId { get; }
        public string SignatureEffectName { get; }
        public IReadOnlyList<string> Aliases { get; }
        public string CreditItemId => "SSS_ASCENSION_CREDIT_" + HeroId.Substring(4);
        public string WeaponItemId => SecondDimension.Gameplay.TitanHeroes161.TitanHeroCatalog161.Slot(HeroId) > 0
            ? "TITAN_SIGNATURE_" + HeroId
            : SssSignatureWeapons.ItemId(HeroId);
        public string IdleArtResourcePath => ArtResourcePath("IDLE");
        public string AttackArtResourcePath => ArtResourcePath("ATTACK");
        public string PortraitArtResourcePath => ArtResourcePath("PORTRAIT");
        public string WeaponArtResourcePath =>
            "SecondDimension/SSSTenV4/Weapons/" + WeaponItemId;

        private string ArtResourcePath(string pose) =>
            "SecondDimension/SSSTenV4/Heroes/" + HeroId + "/" + HeroId + "_" + pose;
    }

    /// <summary>
    /// Live bridge for the ten package-authored identities. IDs, codes, roles,
    /// weights, weapon names and effect names mirror the V4 package data.
    /// Numeric live stats are a deterministic host projection of those weights.
    /// </summary>
    public static class SssTenV4Roster090
    {
        private static readonly SssTenV4HeroDefinition090[] Heroes =
        {
            Hero("SSS_RYLEN_STONEBOND", "Rylen Stonebond", "Monster Tamer", "CLASS_RANGER", "Beastcraft", "Survival", 19,10,19,9,16,13,14, "SDGOW-SSS-BOND", "Wild Covenant", "WF09_STAFF", "Covenant Rally", "SS_RYLEN_STONEBOND", "HERO_SS_RYLEN_STONEBOND"),
            Hero("SSS_ELYSIA_NIGHTCALL", "Elysia Nightcall", "Pact Summoner", "CLASS_MAGE", "Conjuration", "Warding", 14,22,7,25,8,16,8, "SDGOW-SSS-PACT", "Astral Pact", "WF09_STAFF", "Pact Resonance", "SS_ELYSIA_NIGHTCALL", "HERO_SS_ELYSIA_NIGHTCALL"),
            Hero("SSS_VAELIS_MANYFORM", "Vaelis Manyform", "Union Shapeshifter", "CLASS_WARRIOR", "Metamorphosis", "Instinct", 21,10,21,15,15,10,8, "SDGOW-SSS-FORM", "Manyform Talons", "WF08_GAUNTLET", "Perfect Confluence", "SS_VAELIS_MANYFORM", "HERO_SS_VAELIS_MANYFORM"),
            Hero("SSS_NERIS_DAWNWELL", "Neris Dawnwell", "Lifeweaver", "CLASS_PRIEST", "Restoration", "Warding", 16,23,5,23,9,17,7, "SDGOW-SSS-MERCY", "Dawnwell Mercy", "WF09_STAFF", "Mercy Overflow"),
            Hero("SSS_MYRIEN_STARFALL", "Myrien Starfall", "Astral Archmage", "CLASS_MAGE", "Astral", "Elemental", 12,24,5,30,7,14,8, "SDGOW-SSS-NOVA", "Falling Heaven", "WF10_CATALYST_FOCUS", "Falling Constellation"),
            Hero("SSS_ASTERION_SUNWARD", "Asterion Sunward", "Dawn Marshal", "CLASS_WARRIOR", "Leadership", "Warding", 18,13,17,16,16,12,8, "SDGOW-SSS-DAWN", "First Dawn Standard", "WF04_SPEAR_POLEARM", "Dawn Standard"),
            Hero("SSS_SOLENNE_AEGIS", "Solenne Aegis", "Bastion Oracle", "CLASS_GUARDIAN", "Warding", "Restoration", 23,15,9,14,21,13,5, "SDGOW-SSS-AEGIS", "Unbroken Horizon", "WF07_SHIELD", "Horizon Ward"),
            Hero("SSS_CAEDRAN_TEMPEST", "Caedran Tempest", "Tempest Sovereign", "CLASS_MAGE", "Storm", "Arcane", 14,19,7,25,10,12,13, "SDGOW-SSS-STORM", "Skybreaker Conductor", "WF09_STAFF", "Tempest Conductor"),
            Hero("SSS_ISOLDE_ECLIPSERIFT", "Isolde Eclipserift", "Eclipse Arcanist", "CLASS_MAGE", "Void", "Hexcraft", 13,20,7,27,9,17,7, "SDGOW-SSS-ECLIPSE", "Eclipse Meridian", "WF10_CATALYST_FOCUS", "Eclipse Meridian"),
            Hero("SSS_ORINTH_WORLDSONG", "Orinth Worldsong", "Worldsinger", "CLASS_PRIEST", "Harmonics", "Leadership", 16,19,8,19,12,16,10, "SDGOW-SSS-SONG", "Worldsong Accord", "WF10_CATALYST_FOCUS", "Worldsong Harmony")
        };

        public static IReadOnlyList<SssTenV4HeroDefinition090> All =>
            Array.AsReadOnly(Heroes);

        public static bool TryGet(string identity, out SssTenV4HeroDefinition090 hero)
        {
            var canonical = SssHeroes.CanonicalId(identity);
            hero = Heroes.FirstOrDefault(x =>
                StringComparer.Ordinal.Equals(x.HeroId, canonical));
            return hero != null || SecondDimension.Gameplay.TitanHeroes161.TitanHeroCatalog161.TryGet(identity, out hero);
        }

        public static SssTenV4HeroDefinition090 Get(string identity)
        {
            if (!TryGet(identity, out var hero))
                throw new ArgumentException("Unknown SSS Ten hero.", nameof(identity));
            return hero;
        }

        public static bool TryGetByWeaponItemId(
            string weaponItemId,
            out SssTenV4HeroDefinition090 hero)
        {
            hero = Heroes.FirstOrDefault(x => StringComparer.Ordinal.Equals(
                x.WeaponItemId, weaponItemId));
            return hero != null;
        }

        public static bool TryGetRecruit(
            RecruitState recruit,
            out SssTenV4HeroDefinition090 hero)
        {
            hero = null;
            return recruit != null &&
                   (TryGet(recruit.RecruitId, out hero) ||
                    TryGet(recruit.AuthoredStableRecruitId, out hero) ||
                    TryGet(recruit.SignatureId, out hero) ||
                    TryGet(recruit.TutorialAliasId, out hero));
        }

        public static bool RosterContains(
            IReadOnlyList<RecruitState> recruits,
            string identity)
        {
            if (!TryGet(identity, out var hero) || recruits == null) return false;
            for (var index = 0; index < recruits.Count; index++)
            {
                var recruit = recruits[index];
                if (recruit == null) continue;
                if (Matches(recruit.RecruitId, hero.HeroId) ||
                    Matches(recruit.AuthoredStableRecruitId, hero.HeroId) ||
                    Matches(recruit.SignatureId, hero.HeroId) ||
                    Matches(recruit.TutorialAliasId, hero.HeroId)) return true;
            }
            return false;
        }

        public static RecruitState FindOwned(
            IReadOnlyList<RecruitState> recruits,
            string identity)
        {
            if (!TryGet(identity, out var hero) || recruits == null) return null;
            return recruits.FirstOrDefault(x => x != null &&
                (Matches(x.RecruitId, hero.HeroId) ||
                 Matches(x.AuthoredStableRecruitId, hero.HeroId) ||
                 Matches(x.SignatureId, hero.HeroId) ||
                 Matches(x.TutorialAliasId, hero.HeroId)));
        }

        public static CreatorRecruitGrant028 MaterializeGrant(string identity)
        {
            var hero = Get(identity);
            var arts = Enumerable.Range(1, SecondDimension.Gameplay.TitanHeroes161.TitanHeroCatalog161.Slot(hero.HeroId) > 0 ? 3 : 4)
                .Select(index => hero.HeroId + "_ART_" + index.ToString("00"))
                .ToArray();
            var mastery = arts.Select((art, index) => new RecruitArtMasteryState(
                art,
                index < 2 ? hero.PrimaryDiscipline : hero.SecondaryDiscipline,
                0,
                0)).ToArray();
            var progression = new RecruitProgressionState(
                1, 0, 0, 0, 0, 0, 0, 0, 0,
                arts,
                mastery,
                Array.Empty<string>(),
                RecruitAscensionRules089.MinimumLevel);
            var maximumHp = 120 + hero.HpWeight * 6;
            var maximumMp = 24 + hero.MpWeight * 3;
            var applicantJson = CanonicalJson.Serialize(new
            {
                authority = "SSS_TEN_V4",
                heroId = hero.HeroId,
                role = hero.Role,
                rarity = hero.HeroId == "HERO_TITAN_013" ? "SSSS" : SssHeroes.Rarity,
                statWeights = new
                {
                    hp = hero.HpWeight,
                    mp = hero.MpWeight,
                    attack = hero.AttackWeight,
                    magic = hero.MagicWeight,
                    defense = hero.DefenseWeight,
                    resistance = hero.ResistanceWeight,
                    agility = hero.AgilityWeight
                }
            });
            var recruit = new RecruitState(
                hero.HeroId,
                maximumHp,
                maximumHp,
                maximumMp,
                maximumMp,
                hero.DisplayName,
                RecruitOriginKind.Signature,
                hero.HeroId,
                "SSS",
                "WORLD_COVENANT",
                hero.ClassId,
                "SSS",
                10000,
                RecruitAuthorityKind.Normal,
                applicantJson,
                CanonicalJson.Serialize(new
                {
                    hero.Role,
                    hero.PrimaryDiscipline,
                    hero.SecondaryDiscipline,
                    signature = SecondDimension.Gameplay.TitanHeroes161.TitanHeroCatalog161.Slot(hero.HeroId) > 0 ? hero.SignatureEffectName : SssHeroes.Signature(hero.HeroId)
                }),
                EquipmentLoadoutState.Empty(),
                true,
                hero.Aliases.FirstOrDefault() ?? string.Empty,
                hero.HeroId,
                90 + hero.DefenseWeight,
                90 + hero.AgilityWeight,
                progression);
            return new CreatorRecruitGrant028(recruit, Array.Empty<EquipmentItemState>());
        }

        private static bool Matches(string candidate, string canonical) =>
            !string.IsNullOrWhiteSpace(candidate) &&
            StringComparer.Ordinal.Equals(SssHeroes.CanonicalId(candidate), canonical);

        private static SssTenV4HeroDefinition090 Hero(
            string id, string name, string role, string classId,
            string primary, string secondary,
            int hp, int mp, int attack, int magic, int defense, int resistance,
            int agility, string code, string weapon, string family,
            string effect, params string[] aliases) =>
            new SssTenV4HeroDefinition090(
                id, name, role, classId, primary, secondary,
                hp, mp, attack, magic, defense, resistance, agility,
                code, weapon, family, effect, aliases);
    }

    public sealed class SssTenV4CreatorCatalog090 : ICreatorContentCatalog028
    {
        private readonly IReadOnlyList<CreatorCodeRule028> _codes;

        public SssTenV4CreatorCatalog090()
        {
            _codes = SssTenV4Roster090.All.Select(hero => new CreatorCodeRule028
            {
                CodeId = "CODE_SSS_V4_" + hero.HeroId.Substring(4),
                CodeHash = CreatorAccessCommandService028.HashNormalizedCode(hero.RecruitCode),
                Category = "CHARACTER",
                RewardId = hero.HeroId,
                DisplayLabel = hero.DisplayName + " â€¢ SSS Recruit",
                ExactOncePerCampaign = true,
                Active = true
            }).ToArray();
        }

        public int CodeCount => _codes.Count;
        public int RoomCount => 0;
        public IReadOnlyList<CreatorRoomRule028> AllRooms => Array.Empty<CreatorRoomRule028>();

        public bool TryResolveInput(
            string input,
            out CreatorCodeRule028 code,
            out SssTenV4HeroDefinition090 hero)
        {
            var hash = CreatorAccessCommandService028.HashNormalizedCode(input);
            if (TryGetCodeByHash(hash, out code))
            {
                hero = SssTenV4Roster090.Get(code.RewardId);
                return true;
            }
            hero = null;
            return false;
        }

        public bool TryGetCodeByHash(string hash, out CreatorCodeRule028 rule)
        {
            rule = _codes.FirstOrDefault(x =>
                StringComparer.Ordinal.Equals(x.CodeHash, hash));
            return rule != null;
        }

        public bool TryGetResource(string id, out CreatorResourceRule028 rule)
        { rule = null; return false; }
        public bool TryGetInvitation(string id, out CreatorInvitationRule028 rule)
        { rule = null; return false; }
        public bool TryGetWeapon(string id, out CreatorWeaponRule028 rule)
        { rule = null; return false; }
        public bool TryGetContent(string id, out CreatorContentRule028 rule)
        { rule = null; return false; }
        public bool TryGetRoomForBoard(string boardDefinitionId, string boardId, out CreatorRoomRule028 rule)
        { rule = null; return false; }
    }
}
