using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M1ModeAndPolicyTests
    {
        [Test]
        public void ModeCatalogMatchesFrozenPass10Presets()
        {
            var relaxed = ModeCatalog.GetPreset(GameMode.Relaxed);
            var standard = ModeCatalog.GetPreset(GameMode.Standard);
            var iron = ModeCatalog.GetPreset(GameMode.Iron);
            var overpowered = ModeCatalog.GetPreset(GameMode.OverpoweredStart);

            Assert.That(relaxed.PersonalXpPct, Is.EqualTo(150));
            Assert.That(relaxed.EnemyHpPct, Is.EqualTo(85));
            Assert.That(relaxed.PermanentDeathEnabled, Is.False);
            Assert.That(standard.EnemyHpPct, Is.EqualTo(100));
            Assert.That(standard.RelationshipDecayPct, Is.EqualTo(0));
            Assert.That(iron.EnemyDamagePct, Is.EqualTo(115));
            Assert.That(iron.PermanentDeathEnabled, Is.False);
            Assert.That(iron.DepartureEnabled, Is.False);
            Assert.That(iron.RelationshipDecayPct, Is.Zero);
            Assert.That(iron.InjuryPct, Is.EqualTo(160));
            Assert.That(overpowered.PersonalXpPct, Is.EqualTo(500));
            Assert.That(overpowered.StartingTreasuryXp, Is.EqualTo(12000));
            Assert.That(overpowered.StartingEquipmentQuality, Is.EqualTo("reinforced"));
            Assert.That(overpowered.StoryAuthorityBypassEnabled, Is.False);
            Assert.That(overpowered.ConsentBypassEnabled, Is.False);
            Assert.That(overpowered.ProtectedActorBypassEnabled, Is.False);
        }

        [Test]
        public void IronRequiresVisibleHardshipAcknowledgementWithoutRosterLoss()
        {
            Assert.Throws<ArgumentException>(() => new NewGuildProfileState(
                "Tester",
                GameMode.Iron,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                ironConsequencesAcknowledged: false));

            Assert.DoesNotThrow(() => new NewGuildProfileState(
                "Tester",
                GameMode.Iron,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                ironConsequencesAcknowledged: true));
        }

        [Test]
        public void CustomModeCannotEnablePermanentDeathOrDepartureInOwnerBuild()
        {
            var profile = new NewGuildProfileState(
                "Tester", GameMode.Custom, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), ironConsequencesAcknowledged: true);
            var service = new M1CommandService();
            var command = new NewGuildCommand(
                "00000000-0000-0000-0000-000000000099", 99, "1.0", "GUILD_CUSTOM",
                profile, customRules: new ModeRuleSnapshot(GameMode.Custom, true, false));
            var result = service.CreateNewGuild(command);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item("M1_OWNER_BUILD_PERMANENT_RECRUITS_REQUIRED"));

            command = new NewGuildCommand(
                "00000000-0000-0000-0000-000000000100", 100, "1.0", "GUILD_CUSTOM",
                profile, customRules: new ModeRuleSnapshot(GameMode.Custom, false, true));
            result = service.CreateNewGuild(command);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item("M1_OWNER_BUILD_PERMANENT_RECRUITS_REQUIRED"));
        }

        [Test]
        public void AccessibilityDefaultsMatchFrozenPass08Authority()
        {
            var settings = AccessibilitySettingsState.Defaults();
            Assert.That(settings.TextScalePercent, Is.EqualTo(100));
            Assert.That(settings.ReducedMotion, Is.False);
            Assert.That(settings.ScreenShakePercent, Is.EqualTo(60));
            Assert.That(settings.FlashIntensityPercent, Is.EqualTo(70));
            Assert.That(settings.CombatSpeed, Is.EqualTo(1));
            Assert.That(settings.HighContrast, Is.False);
            Assert.That(settings.ForecastDetail, Is.EqualTo("full"));
        }

        [Test]
        public void ProtectedActorsCannotEnterNormalM1Systems()
        {
            Assert.That(
                ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                    "CHAR_KAEL", string.Empty, RecruitAuthorityKind.Normal),
                Is.False);

            foreach (var founder in new[]
                     {
                         "KIRI", "LUNA", "GROM", "SERAPHINA", "AIRA", "LYSSARA", "RIKA"
                     })
            {
                Assert.That(
                    ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                        founder, string.Empty, RecruitAuthorityKind.Normal),
                    Is.False,
                    founder);
            }
        }

        [Test]
        public void OpeningUnionCatalogContainsAllFrozenThreeMemberChoices()
        {
            Assert.That(OpeningUnionCatalog.Formations.Count, Is.EqualTo(8));
            Assert.That(OpeningUnionCatalog.Doctrines.Count, Is.EqualTo(9));
            Assert.That(OpeningUnionCatalog.IsFormation("FORMATION_HOLLOW_SQUARE"), Is.False);
            Assert.That(OpeningUnionCatalog.IsFormation("FORMATION_SHIELD_WALL"), Is.True);
            Assert.That(OpeningUnionCatalog.IsFormation("FORMATION_VEILED_ECHELON"), Is.True);
            Assert.That(
                OpeningUnionCatalog.Formations.Single(value => value.Id == "FORMATION_SHIELD_WALL").Summary,
                Does.Contain("UP TO +8% COHESION"));
            Assert.That(
                OpeningUnionCatalog.Formations.Single(value => value.Id == "FORMATION_WEDGE").Summary,
                Does.Contain("NO RAW STAT CHANGE"));
            Assert.That(
                OpeningUnionCatalog.Doctrines.Single(value => value.Id == "DOCTRINE_AGGRESSIVE").Summary,
                Does.Contain("FORECAST BIAS").And.Contain("NO STAT BONUS"));
        }
    }
}
