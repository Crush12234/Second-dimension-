using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    public sealed class IndependentHeroProgression159Tests
    {
        [TestCase(0, 50, 551, 11)]
        [TestCase(10, 46, 501, 10)]
        [TestCase(100, 25, 276, 6)]
        [TestCase(int.MaxValue, 1, 1, 1)]
        public void TownSavingsReachTrainingHiringAndForgeWithoutChangingPaidLevelRate159(
            int level, int sessionCost, int hireCost, int materialCost)
        {
            var hero = new RecruitState("R1", 100, 100, 20, 20);
            var state = new CampaignState("TOWN_SAVINGS159", 159, "1.0", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD159", 5000, new[] { hero }, Array.Empty<UnionState>()));
            var baseLevel = HeroPaidLevels152.Quote(state, "R1", 1);
            Assert.That(baseLevel.IsSuccess, Is.True, string.Join(" ", baseLevel.Errors));
            var development = state.Guild.Development;
            foreach (var index in new[] { 2, 3, 4 })
                development = development.SetFacilityLevel(TownProgression159.StateIds[index], level, 0);
            var guild = state.Guild;
            state = state.With(guild.With(guild.TreasuryXp, guild.Recruits, guild.Unions, guild.Inventory,
                development), state.OpeningFlow);
            Assert.That(GuildCityRecruitmentService017D.MemberTrainingCost159(state), Is.EqualTo(sessionCost));
            Assert.That(GuildCityRecruitmentService017D.EffectiveSigningCostTreasuryXp(state, 551), Is.EqualTo(hireCost));
            Assert.That(GuildCityRecruitmentService017D.EffectiveSigningCostTreasuryXp(state, 0), Is.Zero);
            var authored = new[] { new MaterialCostDto022 { materialId = "ORE", amount = 11 },
                new MaterialCostDto022 { materialId = "FREE", amount = 0 },
                new MaterialCostDto022 { materialId = "RARE", amount = 1 } };
            var actual = WeaponEvolutionCombat154.MaterialCosts159(state, authored);
            Assert.That(actual.Select(c => c.amount), Is.EqualTo(new[] { materialCost, 0, 1 }));
            Assert.That(actual.Select(c => c.materialId), Is.EqualTo(authored.Select(c => c.materialId)));
            Assert.That(authored[0].amount, Is.EqualTo(11), "A quote must not mutate the authored recipe.");
            var upgradedLevel = HeroPaidLevels152.Quote(state, "R1", 1);
            Assert.That(upgradedLevel.IsSuccess, Is.True, string.Join(" ", upgradedLevel.Errors));
            Assert.That(upgradedLevel.Value.Cost, Is.EqualTo(baseLevel.Value.Cost), "Paid level conversion remains unchanged.");
        }

        [TestCase("GUARDIAN", 0, 2, 0, 0, 1)]
        [TestCase("WARRIOR", 2, 1, 0, 0, 0)]
        [TestCase("RANGER", 1, 0, 2, 0, 0)]
        [TestCase("ROGUE", 1, 0, 2, 0, 0)]
        [TestCase("MAGE", 0, 0, 0, 2, 1)]
        [TestCase("PRIEST", 0, 0, 0, 1, 2)]
        public void OwnedClassIdentityGrantsTheSameGrowthAsBattleClass159(
            string role, int strength, int defense, int agility, int magic, int will)
        {
            foreach (var migrated in new[] { false, true })
            {
                var before = RecruitProgressionState.Default();
                if (migrated) before = before.MigrateGrowth152("CLASS_" + role);
                var owned = before.GainPersonalXp(100, "CLASS_TEND_" + role);
                var battle = before.GainPersonalXp(100, "CLASS_" + role);
                Assert.That(CanonicalJson.Serialize(owned), Is.EqualTo(CanonicalJson.Serialize(battle)));
                Assert.That(new[] { owned.StrengthBonus, owned.DefenseBonus, owned.AgilityBonus,
                    owned.MagicBonus, owned.WillBonus }, Is.EqualTo(new[] { strength, defense, agility, magic, will }));
                Assert.That(owned.Level, Is.EqualTo(2));
                Assert.That(owned.MaximumHpBonus, Is.EqualTo(6));
                Assert.That(owned.MaximumMpBonus, Is.EqualTo(2));
            }
        }

        [TestCase(1, 30L, 22L)]
        [TestCase(5, 1430L, 1022L)]
        public void TreasuryLevelsNeedNoFacilityAndOnlyChargeMissingXp159(int levels, long gap, long cost)
        {
            var hero = new RecruitState("R1", 100, 100, 20, 20)
                .WithProgression(RecruitProgressionState.Default().GainPersonalXp(70, "CLASS_WARRIOR"));
            var state = new CampaignState("HERO_PROGRESS159", 159, "1.0", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD159", 5000, new[] { hero }, Array.Empty<UnionState>()));
            Assert.That(state.Guild.Development.Facilities.All(f => f.Level == 0), Is.True);
            Assert.That(state.CatchUp153, Is.Null);
            var beforeHash = CanonicalJson.Sha256Hex(state);
            var quote = HeroPaidLevels152.Quote(state, hero.RecruitId, levels);
            Assert.That(quote.IsSuccess, Is.True, string.Join(" ", quote.Errors));
            Assert.That(quote.Value.PersonalXpAdded, Is.EqualTo(gap));
            Assert.That(quote.Value.Cost, Is.EqualTo(cost));
            Assert.That(CanonicalJson.Sha256Hex(state), Is.EqualTo(beforeHash));
            var after = HeroPaidLevels152.Confirm(state, quote.Value);
            Assert.That(after.IsSuccess, Is.True, string.Join(" ", after.Errors));
            Assert.That(after.Value.Guild.TreasuryXp, Is.EqualTo(5000 - cost));
            Assert.That(after.Value.Guild.Recruits.Single().Progression.Level, Is.EqualTo(1 + levels));
            Assert.That(CanonicalJson.Serialize(after.Value.Guild.GuildCity.MemberAssignments),
                Is.EqualTo(CanonicalJson.Serialize(state.Guild.GuildCity.MemberAssignments)));
            Assert.That(HeroPaidLevels152.Confirm(after.Value, quote.Value).Value, Is.SameAs(after.Value));
        }
    }
}
