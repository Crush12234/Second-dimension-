using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRosterOutfittedFixture095Tests
    {
        [TestCase(180)]
        [TestCase(12)]
        public void BuiltPlayerFixtureUsesProductionNewArrivalEquipmentWithoutChangingAcquisition095(int rosterId)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == rosterId);
            var before = HeroRosterAudit093.CreateFixture093(hero);
            var beforeHash = CanonicalJson.Sha256Hex(before);
            var raw = HeroRosterAudit093.SignAndPlace093(before, hero, new HeroRosterAuditRow093());
            Assert.That(raw.Guild.Recruits.Single().Equipment.Assignments, Is.Empty,
                "Keep the raw helper intact for existing starter-equipment preference tests.");
            var row = new HeroRosterAuditRow093 { rosterId = rosterId, stableId = hero.StableId };
            var outfitted = HeroRosterAudit093.SignOutfitAndPlace093(before, hero, row);
            var recruit = outfitted.Guild.Recruits.Single();
            Assert.That(CanonicalJson.Sha256Hex(before), Is.EqualTo(beforeHash));
            Assert.That(recruit.AuthoredStableRecruitId, Is.EqualTo(hero.StableId));
            Assert.That(recruit.DisplayName, Is.EqualTo(hero.Name));
            Assert.That(recruit.Equipment.Find(EquipmentSlotIds.MainHand), Is.Not.Null);
            Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
            Assert.That(recruit.Equipment.Assignments.Count, Is.EqualTo(2));
            Assert.That(CanonicalJson.Serialize(recruit.Progression),
                Is.EqualTo(CanonicalJson.Serialize(raw.Guild.Recruits.Single().Progression)));
            Assert.That(recruit.CurrentHp, Is.EqualTo(raw.Guild.Recruits.Single().CurrentHp));
            Assert.That(recruit.CurrentMp, Is.EqualTo(raw.Guild.Recruits.Single().CurrentMp));
            Assert.That(outfitted.Guild.TreasuryXp, Is.EqualTo(raw.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(outfitted.OpeningFlow), Is.EqualTo(CanonicalJson.Serialize(raw.OpeningFlow)));
            Assert.That(row.exactOnce && row.xpChargedCorrectly && row.placedInUnion, Is.True);
            Assert.That(outfitted.Guild.Inventory, Is.Empty, "Starter gear is equipped, not duplicated into inventory.");

            var combat = HeroRosterAudit093.LoadCombatContent093();
            var policy = new RecruitStarterEquipment094(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(HeroRosterAudit093.ContentRoot093), combat);
            Assert.That(CanonicalJson.Sha256Hex(policy.ApplyToNewRecruits(before, outfitted)),
                Is.EqualTo(CanonicalJson.Sha256Hex(outfitted)), "The same arrival cannot issue equipment twice.");
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(outfitted));
            Assert.That(CanonicalJson.Sha256Hex(reloaded), Is.EqualTo(CanonicalJson.Sha256Hex(outfitted)));
            var projected = HeroRosterAudit093.StartBattleAndVerifyForecast093(reloaded, row);
            var battle = projected.Battle;
            var member = battle.PlayerUnions.SelectMany(value => value.Members).Single();
            Assert.That(member.EquipmentTags, Is.Not.Empty);
            Assert.That(battle.CommittedForecasts.SelectMany(value => value.MemberActions)
                .Any(action => action.ActorMemberId == recruit.RecruitId &&
                    action.PredictedHpDelta < 0 && action.ArtId != "ART_ASSIST_ALLY"), Is.True);
            if (rosterId == 180)
            {
                Assert.That(member.EquipmentTags, Does.Contain("SWORD"));
                Assert.That(battle.CommittedForecasts.Where(value => value.CommandId == "CMD_BALANCED")
                    .SelectMany(value => value.MemberActions).Select(value => value.ArtId),
                    Does.Not.Contain("ART_ASSIST_ALLY"));
                const string boundCut = "TREE_CA002_WPN_HYBRID_RELIC_N01";
                const string echoCast = "TREE_CA002_WPN_HYBRID_RELIC_N02";
                Assert.That(member.LearnedArtIds, Does.Contain(boundCut).And.Contain(echoCast));
                var echoDefinition = combat.Art(echoCast);
                Assert.That(echoDefinition.IsForecastAction, Is.True);
                Assert.That(echoDefinition.RequiredEquipmentTags.Any(member.EquipmentTags.Contains), Is.True);
                Assert.That(member.CurrentMp, Is.GreaterThanOrEqualTo(echoDefinition.PersonalMpCost));
                Assert.That(battle.PlayerUnions.Single().CurrentAp, Is.GreaterThanOrEqualTo(echoDefinition.SharedApCost));

                // The production selector deliberately tries unused learned deep
                // Arts in stable-ID order. Bound Cut N01 precedes Echo Cast N02;
                // prove real use advances the choice instead of requiring N02 to
                // displace an older, still-unused Art in the very first Forecast.
                var firstMystic = battle.CommittedForecasts.Single(value => value.CommandId == "CMD_MYSTIC");
                Assert.That(firstMystic.MemberActions.Any(action => action.ArtId == boundCut &&
                    action.PredictedHpDelta < 0), Is.True);
                var commands = new M2BattleCommandService();
                var selected = HeroRosterAudit093.Require093(commands.SelectForecast(
                    projected, firstMystic.UnionId, firstMystic.ForecastId));
                var usedBoundCut = HeroRosterAudit093.Require093(commands.ConfirmRound(selected, combat));
                Assert.That(usedBoundCut.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
                var nextMember = usedBoundCut.Battle.PlayerUnions.SelectMany(value => value.Members).Single();
                Assert.That(nextMember.LearnedArtIds, Does.Contain(boundCut).And.Contain(echoCast));
                Assert.That(nextMember.ArtProgress.Single(value => value.ArtId == boundCut).MeaningfulUses,
                    Is.GreaterThan(0));
                var nextMystic = usedBoundCut.Battle.CommittedForecasts.Single(value => value.CommandId == "CMD_MYSTIC");
                Assert.That(nextMystic.MemberActions.Any(action => action.ArtId == echoCast &&
                    action.PredictedHpDelta < 0), Is.True,
                    "After Bound Cut's actual use, the next legal Mystic Forecast must offer Kara's retained Echo Cast.");
                selected = HeroRosterAudit093.Require093(commands.SelectForecast(
                    usedBoundCut, nextMystic.UnionId, nextMystic.ForecastId));
                var usedEchoCast = HeroRosterAudit093.Require093(commands.ConfirmRound(selected, combat));
                Assert.That(usedEchoCast.Battle.PlayerUnions.SelectMany(value => value.Members).Single()
                    .ArtProgress.Single(value => value.ArtId == echoCast).MeaningfulUses, Is.GreaterThan(0));
            }
        }
    }
}
