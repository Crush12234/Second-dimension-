using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.SSS.V3;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleAutoForecastQuality097Tests
    {
        M1RuntimeCoordinator _coordinator;
        M2CombatContent _combat;
        string _directory;
        readonly List<string> _actualChoices = new List<string>();
        static readonly FieldInfo CampaignField = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.NonPublic | BindingFlags.Instance);

        [OneTimeSetUp]
        public void Setup097()
        {
            _directory = Path.Combine(Path.GetTempPath(), "sd_auto_quality097_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, Path.Combine(_directory, "isolated.json"));
            _combat = (M2CombatContent)typeof(M1RuntimeCoordinator).GetField("_combatContent", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_coordinator);
            Assert.That(_combat.HeroMasterGeneratedAuthority096, Is.Not.Null, "Use actual shipping coordinator content.");
        }

        [OneTimeTearDown]
        public void Cleanup097()
        {
            TestContext.WriteLine("AUTO097_ACTUAL_CHOICES=" + JsonConvert.SerializeObject(_actualChoices));
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        static IEnumerable<TestCaseData> Heroes097() => HeroRosterAudit093.Catalog093.AcceptedHeroes
            .OrderBy(hero => hero.RosterId).Select(hero => new TestCaseData(hero.RosterId)
                .SetName("ActualAutoLegalRound097_" + hero.StableId));
        static IEnumerable<TestCaseData> Sss097() => SssHeroes.All.Select(hero => new TestCaseData(hero)
            .SetName("ActualAutoLegalRound097_" + hero));

        [TestCaseSource(nameof(Heroes097))]
        public void All250AcceptedHeroesUseOnlyActualLegalCompleteForecasts097(int rosterId)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == rosterId);
            var signed = HeroRosterAudit093.SignOutfitAndPlace093(HeroRosterAudit093.CreateFixture093(hero),
                hero, new HeroRosterAuditRow093());
            var started = HeroRosterAudit093.Require093(new M2BattleCommandService().StartEncounterBattle(
                signed, _combat, "AUTO097_HM_" + rosterId, "Isolated Auto legality, not natural acquisition pacing", 1));
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(signed.Guild.Recruits.Single(), out var profile), Is.True);
            var root = _combat.DeepProgression.Tree(profile.WeaponTreeId).RootNodeId;
            Assert.That(started.Battle.PlayerUnions.Single().Members.Single().LearnedArtIds, Does.Contain(root));
            ExecuteActualAutoRound097(started, hero.StableId);
        }

        [TestCaseSource(nameof(Sss097))]
        public void AllTenSssRetainTheirExistingPaidLegalAutoOptions097(string heroId)
        {
            // Reuse the existing certified SSS fixture (including its explicitly
            // prepared family setup), not a replacement SSS grant/resolution path.
            var factory = typeof(SssTenV4BattleIntegration090Tests).GetMethod("Campaign090", BindingFlags.NonPublic | BindingFlags.Static);
            var needsWoundedOtherUnion = heroId == "SSS_NERIS_DAWNWELL";
            var heroes = needsWoundedOtherUnion ? new[] { heroId, "SSS_ASTERION_SUNWARD" } : new[] { heroId };
            var state = (CampaignState)factory.Invoke(null, new object[] { heroes, needsWoundedOtherUnion, true });
            var started = HeroRosterAudit093.Require093(new M2BattleCommandService().StartEncounterBattle(
                state, _combat, "AUTO097_" + heroId, "Isolated existing SSS Auto authority", 2));
            Assert.That(started.Battle.CommittedForecasts.Any(value => value.CommandId == "SSS_CMD_" + heroId.Substring(4)), Is.True);
            ExecuteActualAutoRound097(started, heroId);
        }

        [Test]
        public void KaraAutoActuallyUsesUsefulLearnedArtsAndReplaysExactCommandsAndState097()
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == 180);
            var signed = HeroRosterAudit093.SignOutfitAndPlace093(HeroRosterAudit093.CreateFixture093(hero), hero, new HeroRosterAuditRow093());
            var initial = HeroRosterAudit093.Require093(new M2BattleCommandService().StartEncounterBattle(
                signed, _combat, "AUTO097_KARA_THREE_REAL_ROUNDS", "Actual Auto chooses useful learned Arts through existing Forecasts", 1));
            var beforeHash = CanonicalJson.Sha256Hex(initial);
            var first = RunKara097(initial);
            var restored = JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(initial));
            Assert.That(CanonicalJson.Sha256Hex(restored), Is.EqualTo(beforeHash));
            var replay = RunKara097(restored);
            Assert.That(replay, Is.EqualTo(first), "Save/replay retains exact chosen commands and resolved canonical hashes.");
            Assert.That(CanonicalJson.Sha256Hex(initial), Is.EqualTo(beforeHash), "No forecast mutation or source save migration.");
        }

        string[] RunKara097(CampaignState initial)
        {
            CampaignField.SetValue(_coordinator, initial); // Isolated active-battle fixture; all actions below use shipping coordinator commands.
            var evidence = new List<string>();
            var learnedActionsUsed = 0;
            for (var round = 0; round < 3; round++)
            {
                var current = _coordinator.State.Battle;
                var choice = M2BattleAutoOrders091.Choose(current, current.PlayerUnions.Single().UnionId);
                Assert.That(choice, Is.Not.Null);
                var artId = choice.MemberActions.Single().ArtId;
                if (_combat.DeepProgression.TryNode(artId, out _)) learnedActionsUsed++;
                var planned = ((CampaignState)CampaignField.GetValue(_coordinator)).Battle.CommittedForecasts.Single(value => value.ForecastId == choice.ForecastId);
                Assert.That(choice.MemberActions.Single().PredictedHpDelta097, Is.EqualTo(planned.MemberActions.Single().PredictedHpDelta));
                Assert.That(M2BattleAutoOrders091.SelectCompletePlan(_coordinator, out var failure), Is.True, failure);
                var result = _coordinator.ConfirmBattleRound();
                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(_coordinator.State.Battle.LastResolvedRoundEvents.Any(item => item.ArtId == artId &&
                    (item.EventType == "MYSTIC_HIT" || item.EventType == "MARTIAL_HIT" || item.EventType == "TACTICAL_HIT") && item.Amount > 0), Is.True);
                Assert.That(_coordinator.State.Battle.LastResolvedRoundEvents.Any(item => item.ArtId == artId &&
                    item.EventType == "ART_GROWTH" && item.Amount > 0), Is.True);
                evidence.Add(choice.ForecastId + "|" + CanonicalJson.Sha256Hex((CampaignState)CampaignField.GetValue(_coordinator)));
            }
            Assert.That(learnedActionsUsed, Is.GreaterThan(0),
                "R95 Auto repeated only Basic. A real useful learned Formation Strike may fairly outrank weaker Bound Cut/Echo Cast; do not force those instead.");
            var retained = ((CampaignState)CampaignField.GetValue(_coordinator)).Battle.PlayerUnions.Single().Members.Single().LearnedArtIds;
            Assert.That(retained, Does.Contain("TREE_CA002_WPN_HYBRID_RELIC_N01").And.Contain("TREE_CA002_WPN_HYBRID_RELIC_N02"));
            return evidence.ToArray();
        }

        void ExecuteActualAutoRound097(CampaignState started, string identity)
        {
            var originalHash = CanonicalJson.Sha256Hex(started);
            CampaignField.SetValue(_coordinator, started);
            var view = _coordinator.State.Battle;
            var choices = view.PlayerUnions.Where(value => value.CanAct).Select(union =>
            {
                var choice = M2BattleAutoOrders091.Choose(view, union.UnionId);
                Assert.That(choice, Is.Not.Null, identity);
                var canonical = started.Battle.CommittedForecasts.Single(value => value.ForecastId == choice.ForecastId);
                Assert.That(choice.SharedApCost, Is.EqualTo(canonical.SharedApCost));
                Assert.That(choice.MemberActions.Select(value => value.ArtId), Is.EqualTo(canonical.MemberActions.Select(value => value.ArtId)));
                Assert.That(choice.MemberActions.Select(value => value.PredictedHpDelta097), Is.EqualTo(canonical.MemberActions.Select(value => value.PredictedHpDelta)));
                foreach (var action in choice.MemberActions)
                {
                    var source = canonical.MemberActions.Single(value => value.ActorMemberId == action.ActorMemberId);
                    Assert.That(action.DamageRecipients097.Sum(value => value.PredictedHpLoss),
                        Is.EqualTo(source.AreaActionPlan095?.PredictedHpLoss ?? 0));
                }
                return choice;
            }).ToArray();
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(_coordinator, out var failure), Is.True, identity + ": " + failure);
            var committed = _coordinator.ConfirmBattleRound();
            Assert.That(committed.Succeeded, Is.True, identity + ": " + committed.Message);
            Assert.That(((CampaignState)CampaignField.GetValue(_coordinator)).Battle.EventLog.Count, Is.GreaterThan(started.Battle.EventLog.Count));
            Assert.That(CanonicalJson.Sha256Hex(started), Is.EqualTo(originalHash));
            _actualChoices.Add(identity + ": " + string.Join("; ", choices.Select(choice =>
                choice.CommandId + " [" + string.Join(",", choice.MemberActions.Select(action => action.ArtId)) + "]")));
        }

        [Test]
        public void UsefulLearnedArtWinsButBasicFinisherConservesApAndMp097()
        {
            var view = Simple097(100);
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("ART"));
            view.EnemyUnions[0].Members[0].CurrentHp = 4;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("BASIC"), "Do not spend4AP to remove the same4HP.");
            view.EnemyUnions[0].Members[0].CurrentHp = 100;
            view.Forecasts[1].MemberActions[0].PersonalMpCost = 15;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("BASIC"), "A modest damage increase does not justify a large MP spend.");
            view.Forecasts[1].SharedApCost = 999;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("BASIC"));
        }

        [Test]
        public void AreaCountsExactCommittedRecipientsOnceAndDoesNotRecycleOverkill097()
        {
            var view = Simple097(1);
            var area = view.Forecasts[1].MemberActions[0];
            area.PredictedHpDelta097 = -1000;
            area.AreaTargetCount095 = 5;
            area.AreaPredictedHpLoss095 = 1000;
            area.DamageRecipients097 = new[] { new M2PredictedDamageRecipient097 { UnionId = "ENEMY", MemberId = "FOE", PredictedHpLoss = 40 },
                new M2PredictedDamageRecipient097 { UnionId = "ENEMY", MemberId = "DEAD", PredictedHpLoss = 40 } };
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("BASIC"),
                "No extra damage for count5, scalar1000, dead targets or overkill; there is exactly1HP to remove.");
            view.EnemyUnions[0].Members = new[] { view.EnemyUnions[0].Members[0], new M2BattleMemberView { MemberId = "DEAD", CurrentHp = 50, MaximumHp = 50 } };
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("ART"), "A genuinely living second recipient makes this committed area plan useful.");
        }

        [Test]
        public void PowerfulMageForecastDoesNotTieWithBasicAboveFiveHundredDamage107()
        {
            var view = Simple097(10000);
            view.Forecasts[0].MemberActions[0].PredictedHpDelta097 = -600;
            view.Forecasts[1].MemberActions[0].PredictedHpDelta097 = -2400;
            view.Forecasts[1].MemberActions[0].PersonalMpCost = 25;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[1]),
                "Both attacks exceeded the old 500-HP scoring ceiling; useful legal magic must still receive its damage value.");
            view.EnemyUnions[0].Members[0].CurrentHp = 500;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[0]),
                "The cheaper basic finisher still wins when both Forecasts remove the same remaining HP.");
            view.EnemyUnions[0].Members[0].CurrentHp = 10000;
            view.PlayerUnions[0].Members[0].CurrentMp = 24;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[0]),
                "Useful magic remains unavailable without its real personal MP.");
        }

        [Test]
        public void LargeAreaForecastValuesEveryExactLivingRecipientBeyondOldDamageCeiling107()
        {
            var view = Simple097(4000);
            view.Forecasts[0].MemberActions[0].PredictedHpDelta097 = -600;
            view.EnemyUnions[0].Members = new[] { view.EnemyUnions[0].Members[0],
                new M2BattleMemberView { MemberId = "FOE_2", CurrentHp = 4000, MaximumHp = 4000 } };
            var area = view.Forecasts[1].MemberActions[0];
            area.PersonalMpCost = 20;
            area.PredictedHpDelta097 = -1200;
            area.DamageRecipients097 = new[] {
                new M2PredictedDamageRecipient097 { UnionId = "ENEMY", MemberId = "FOE", PredictedHpLoss = 600 },
                new M2PredictedDamageRecipient097 { UnionId = "ENEMY", MemberId = "FOE_2", PredictedHpLoss = 600 } };
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[1]));
            view.EnemyUnions[0].Members[1].CurrentHp = 0;
            view.EnemyUnions[0].Members[1].Downed = true;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[0]),
                "A dead second recipient cannot justify extra area costs.");
        }

        [Test]
        public void CriticalAllyReceivesStrongerUsefulHealAndZeroHpSupportCannotMasqueradeAsHealing107()
        {
            var view = Simple097(1000);
            view.PlayerUnions = new[] { view.PlayerUnions[0], new M2BattleUnionView { UnionId = "PATIENTS",
                Members = new[] { new M2BattleMemberView { MemberId = "PATIENT", CurrentHp = 5, MaximumHp = 100 } } } };
            var small = new M2ForecastView { ForecastId = "SMALL", UnionId = "ALLY", SharedApCost = 1,
                MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "HERO", ArtId = "LEARNED_SMALL_HEAL",
                    ActionKind = "Restoration", TargetUnionId = "PATIENTS", TargetMemberId = "PATIENT",
                    PredictedHpDelta097 = 10, PersonalMpCost = 2 } } };
            var strong = new M2ForecastView { ForecastId = "STRONG", UnionId = "ALLY", SharedApCost = 6,
                MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "HERO", ArtId = "LEARNED_STRONG_HEAL",
                    ActionKind = "Restoration", TargetUnionId = "PATIENTS", TargetMemberId = "PATIENT",
                    PredictedHpDelta097 = 75, PersonalMpCost = 12 } } };
            view.Forecasts = new[] { view.Forecasts[1], small, strong };
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(strong),
                "Both help the same critical ally, so compare committed HP restored as well as real costs.");
            strong.MemberActions[0].PredictedHpDelta097 = 0;
            strong.MemberActions[0].Prediction = "Ultimate life-saving support";
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(small),
                "A support label does not confer the priority of real HP restoration.");
            small.MemberActions[0].PredictedHpDelta097 = 0;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[0]));
        }

        [Test]
        public void OtherWoundedMemberDoesNotMakeHealthyPrimaryTargetAnUrgentRescue107()
        {
            var view = Simple097(1000);
            view.PlayerUnions[0].Members = new[] { view.PlayerUnions[0].Members[0],
                new M2BattleMemberView { MemberId = "WOUNDED_OTHER", CurrentHp = 1, MaximumHp = 100 } };
            var support = new M2ForecastView { ForecastId = "SUPPORT", UnionId = "ALLY", SharedApCost = 6,
                MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "HERO", ActionKind = "Restoration",
                    TargetUnionId = "ALLY", TargetMemberId = "HERO", PredictedHpDelta097 = 0, PersonalMpCost = 12 } } };
            view.Forecasts = new[] { view.Forecasts[1], support };
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY"), Is.SameAs(view.Forecasts[0]));
        }

        [Test]
        public void DisplayTextCannotInfluenceAutoAndRevivalStillOutranksExtremeDamage097()
        {
            var view = Simple097(100000);
            view.Forecasts[0].MemberActions[0].Prediction = "Instant ultimate victory9999999 HP";
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("ART"));
            var fallen = new M2BattleUnionView { UnionId = "WOUNDED", Members = new[] { new M2BattleMemberView { MemberId = "PATIENT", CurrentHp = 0, MaximumHp = 100, Downed = true } } };
            view.PlayerUnions = new[] { view.PlayerUnions[0], fallen };
            var rescue = new M2ForecastView { ForecastId = "REVIVE", UnionId = "ALLY", CommandId = "CMD_HEAL", SharedApCost = 6,
                MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "HERO", ArtId = "ART_STAND_AGAIN", ActionKind = "Restoration",
                    TargetUnionId = "WOUNDED", TargetMemberId = "PATIENT", IsRevival091 = true,
                    PredictedHpDelta097 = 20, PersonalMpCost = 12 } } };
            view.Forecasts = new[] { view.Forecasts[0], view.Forecasts[1], rescue };
            view.Forecasts[1].MemberActions[0].PredictedHpDelta097 = -100000;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("REVIVE"));
            rescue.MemberActions[0].IsRevival091 = false;
            fallen.Members[0].Downed = false;
            fallen.Members[0].CurrentHp = 1;
            Assert.That(M2BattleAutoOrders091.Choose(view, "ALLY").ForecastId, Is.EqualTo("REVIVE"), "Critical lawful healing keeps priority even against extreme attack previews.");
        }

        static M2BattleView Simple097(int enemyHp) => new M2BattleView
        {
            BattleId = "AUTO_NUMERIC097", Round = 1,
            PlayerUnions = new[] { new M2BattleUnionView { UnionId = "ALLY", CanAct = true, CurrentAp = 30,
                Members = new[] { new M2BattleMemberView { MemberId = "HERO", CurrentHp = 100, MaximumHp = 100, CurrentMp = 100 } } } },
            EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY", CanAct = true,
                Members = new[] { new M2BattleMemberView { MemberId = "FOE", CurrentHp = enemyHp, MaximumHp = enemyHp } } } },
            Forecasts = new[] {
                new M2ForecastView { ForecastId = "BASIC", UnionId = "ALLY", CommandId = "CMD_BALANCED", SharedApCost = 0,
                    MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "HERO", ArtId = "ART_BASIC_SABER_CUT", ActionKind = "Martial",
                        TargetUnionId = "ENEMY", TargetMemberId = "FOE", PredictedHpDelta097 = -39, PredictedGrowth = 5 } } },
                new M2ForecastView { ForecastId = "ART", UnionId = "ALLY", CommandId = "CMD_MYSTIC", SharedApCost = 4,
                    MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "HERO", ArtId = "TREE_CA002_WPN_HYBRID_RELIC_N01", ActionKind = "Mystic",
                        TargetUnionId = "ENEMY", TargetMemberId = "FOE", PredictedHpDelta097 = -43, PredictedGrowth = 8 } } }
            }
        };
    }
}
