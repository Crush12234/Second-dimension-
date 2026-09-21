#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    public sealed class UnionBattlePlan132Tests
    {
        const string Baseline = @"C:\Users\simon\Documents\ChatGPT\second dimension\SaveBackups\20260913_075155_409_alpha132_baseline\Profile\second_dimension_first_hour_slice_071.json";
        const string BaselineSha = "AD42DB5EDAA35C3149BF33E9308EB3E7EF4FF1852CD1109D44EBCF56FBB142E5";
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        string _directory, _path;
        M1RuntimeCoordinator _owner;
        readonly M1CommandService _commands = new M1CommandService();

        [SetUp]
        public void Setup132()
        {
            _directory = Path.Combine(Path.GetTempPath(), "sd_union132_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "campaign.json");
        }

        [TearDown]
        public void Teardown132()
        {
            if (_directory == null || !Directory.Exists(_directory)) return;
            Assert.That(Path.GetFullPath(_directory).StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\') + "\\sd_union132_", StringComparison.OrdinalIgnoreCase), Is.True);
            Directory.Delete(_directory, true);
        }

        [Test]
        public void CurrentSeventyOwnedTower307CombatCannotChangePlansOrSave132()
        {
            var bytes = File.ReadAllBytes(Baseline);
            Assert.That(Hash(bytes), Is.EqualTo(BaselineSha));
            var copy = Path.Combine(_directory, "locked307.json");
            File.WriteAllBytes(copy, bytes);
            var loaded = new AtomicSaveStore().ReadWithRecovery(copy);
            Assert.That(loaded.IsSuccess, Is.True, string.Join(";", loaded.Errors));
            var source = loaded.Value.CampaignState;
            Assert.That(source.Guild.Recruits.Count, Is.EqualTo(70));
            Assert.That(source.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(source.Guild.GuildCity.PendingEncounter, Is.Not.Null);
            var assigned = UnionBattlePlanRules132.Read(source).SelectMany(value => value.MemberRecruitIds).ToArray();
            var reserve = source.Guild.Recruits.First(value => !assigned.Contains(value.RecruitId));
            var before = CanonicalJson.Serialize(source);
            Assert.That(_commands.AssignReserveRecruitToUnion109(source, reserve.RecruitId, 0, 0, assigned[0]).IsSuccess, Is.False);
            Assert.That(_commands.UnassignRecruitFromUnion(source, assigned[0]).IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(source), Is.EqualTo(before));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Baseline));
        }

        [Test]
        public void Banked309CanPlanInNewIdleTowerOwnerWithoutRewritingItsGrant132()
        {
            const string banked = @"C:\Users\simon\Documents\ChatGPT\second dimension\SaveBackups\20260913_080448_090_alpha132_baseline\Profile\second_dimension_first_hour_slice_071.json";
            var bytes = File.ReadAllBytes(banked);
            Assert.That(Hash(bytes), Is.EqualTo("93FE85F40089E1ECF5498B8B1413A2038712ED1A42123AFD66BE4A33772E4942"));
            var copy = Path.Combine(_directory, "banked309.json");
            File.WriteAllBytes(copy, bytes);
            var loaded = new AtomicSaveStore().ReadWithRecovery(copy);
            Assert.That(loaded.IsSuccess, Is.True, string.Join(";", loaded.Errors));
            var registry = SecondDimension.Presentation.Campaign022.CampaignRegistry022.LoadFromResources();
            var active = Require(new CampaignProgressionCommandService022().BeginTowerFloor094(loaded.Value.CampaignState, registry));
            Assert.That(active.Guild.GuildCity.PendingEncounter, Is.Null);
            var assigned = active.Guild.Unions.SelectMany(value => value.MemberRecruitIds).ToArray();
            var reserve = active.Guild.Recruits.First(value => !assigned.Contains(value.RecruitId));
            var snapshot = CanonicalJson.Serialize(active.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022);
            var planned = Require(_commands.AssignReserveRecruitToUnion109(active, reserve.RecruitId, 0, 0, active.Guild.Unions[0].MemberRecruitIds[0]));
            Assert.That(planned.NextBattleUnions132, Is.Not.Null);
            Assert.That(UnionBattlePlanRules132.Read(planned)[0].MemberRecruitIds[0], Is.EqualTo(reserve.RecruitId));
            Assert.That(CanonicalJson.Serialize(planned.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(active.Guild.Unions)));
            Assert.That(CanonicalJson.Serialize(planned.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022), Is.EqualTo(snapshot));
            var originalCovenant = active.Guild.Unions.Single(value => value.UnionId == "UNION_OPENING_02");
            var plannedCovenant = planned.NextBattleUnions132.Unions.Single(value => value.UnionId == originalCovenant.UnionId);
            Assert.That(originalCovenant.LeaderRecruitId, Is.EqualTo("SSS_ASTERION_SUNWARD"));
            Assert.That(originalCovenant.LeaderRecruitId, Is.Not.EqualTo(originalCovenant.MemberRecruitIds[0]), "The exact preserved save legitimately has a Covenant leader outside slot one.");
            Assert.That(CanonicalJson.Serialize(plannedCovenant), Is.EqualTo(CanonicalJson.Serialize(originalCovenant)), "Editing another Union must preserve this valid leader and formation order.");
            var invalid = planned.NextBattleUnions132.Unions.ToArray();
            var invalidIndex = Array.FindIndex(invalid, value => value.UnionId == originalCovenant.UnionId);
            invalid[invalidIndex] = new UnionState(plannedCovenant.UnionId, plannedCovenant.DisplayName,
                plannedCovenant.Kind, plannedCovenant.MemberRecruitIds[0], plannedCovenant.MemberRecruitIds,
                plannedCovenant.FormationId, plannedCovenant.DoctrineId, plannedCovenant.SharedAp,
                plannedCovenant.CohesionBasisPoints, plannedCovenant.MemberPositions);
            Assert.Throws<InvalidOperationException>(() => planned.WithNextBattleUnions132(
                new UnionBattlePlan132(planned.NextBattleUnions132.SourceRosterHash, invalid)), "A forged ordinary leader cannot displace the required Covenant leader.");
            new AtomicSaveStore().Write(copy, SaveEnvelopeV1.Create(planned, DateTime.UtcNow));
            var planReload = new AtomicSaveStore().ReadWithRecovery(copy);
            Assert.That(planReload.IsSuccess, Is.True, string.Join(";", planReload.Errors));
            Assert.That(CanonicalJson.Serialize(planReload.Value.CampaignState), Is.EqualTo(CanonicalJson.Serialize(planned)));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(banked));
        }

        [Test]
        public void ActiveAuthoredQuestPlacementSwapReloadAndCertifiedBattleUsePlan132()
        {
            BeginSyntheticQuest132();
            var source = State132();
            var originalRoster = CanonicalJson.Serialize(source.Guild.Unions);
            var originalOperation = CanonicalJson.Serialize(World132(source));
            var beforeWrite = File.ReadAllBytes(_path);
            var placed = _owner.AssignReserveRecruitToUnion109("PLAN_R4", 0, 2, null);
            Assert.That(placed.Succeeded, Is.True, placed.Message);
            CollectionAssert.AreEqual(beforeWrite, File.ReadAllBytes(_path + ".bak"));
            Assert.That(placed.Message, Does.Contain("next battle"));
            Assert.That(_owner.State.Unions[0].MemberRecruitIds, Is.EqualTo(new[] { "PLAN_R0", "PLAN_R1", "PLAN_R4" }));
            Assert.That(_owner.AssignReserveRecruitToUnion109("PLAN_R5", 0, 0, "PLAN_R0").Succeeded, Is.True);
            Assert.That(_owner.AssignReserveRecruitToUnion109("PLAN_R5", 1, 0, "PLAN_R2").Succeeded, Is.False, "A hero cannot occupy two Unions.");
            var saved = State132();
            Assert.That(CanonicalJson.Serialize(saved.Guild.Unions), Is.EqualTo(originalRoster));
            Assert.That(CanonicalJson.Serialize(World132(saved)), Is.EqualTo(originalOperation), "The expedition seed, committed roster, IDs and receipts remain unchanged.");
            Assert.That(CanonicalJson.Serialize(saved.Guild.Recruits), Is.EqualTo(CanonicalJson.Serialize(source.Guild.Recruits)));
            var reloaded = new AtomicSaveStore().ReadWithRecovery(_path);
            Assert.That(reloaded.IsSuccess, Is.True, string.Join(";", reloaded.Errors));
            var candidate = reloaded.Value.CampaignState;
            Assert.That(CanonicalJson.Serialize(candidate), Is.EqualTo(CanonicalJson.Serialize(saved)));
            Assert.That(UnionBattlePlanRules132.Read(candidate)[0].MemberRecruitIds, Is.EqualTo(new[] { "PLAN_R5", "PLAN_R1", "PLAN_R4" }));
            var catalog = new Campaign023RuleCatalogAdapter(CampaignRegistry023.LoadFromResources());
            var world = new CampaignWorldGateCommandService023();
            Assert.That(CampaignWorldGateCommandService023.ValidateActiveAuthority093(candidate, catalog, out var error), Is.True, error);

            // Synthetic initial roster, real shipping CH018_001 three-card deck.
            // Select a genuinely offered optional battle, never a forced node.
            var deckCommands = new ExpeditionDeckCommandService089();
            var reached = false;
            for (var count = 0; count < 32; count++)
            {
                var operation = World132(candidate);
                var row = operation.ExpeditionDeck089.CurrentRow;
                Assert.That(row, Has.Count.EqualTo(3));
                var battleCard = row.FirstOrDefault(ExpeditionDeckService089.IsOptionalBattleCard089);
                if (battleCard != null)
                {
                    candidate = Require(deckCommands.CommitRouteCard(candidate, catalog, battleCard.CardId, "PLAN_R0", "PLAN_R1"));
                    Assert.That(deckCommands.HasCanonicalOptionalBattleHandoff104(candidate), Is.True);
                    Assert.That(_commands.AssignReserveRecruitToUnion109(candidate, "PLAN_R0", 1, 2, null).IsSuccess, Is.False);
                    reached = true;
                    break;
                }
                Assert.That(operation.Status, Is.Not.EqualTo(WorldGateOperationStatus023.ReadyToFinalize), "The deterministic deck must offer an actual battle before its end.");
                var choices = row.Where(card => card.TreasuryXpCost == 0 && card.Category != "PERMANENT" && card.Category != "CHANCE" && card.Category != "HAZARD").ToArray();
                var committed = Result<CampaignState>.Failure("The real row must offer a safe free path.");
                foreach (var card in choices)
                {
                    committed = deckCommands.CommitRouteCard(candidate, catalog, card.CardId, "PLAN_R0", "PLAN_R1");
                    if (committed.IsSuccess) break;
                }
                candidate = Require(committed);
                Assert.That(_commands.AssignReserveRecruitToUnion109(candidate, "PLAN_R0", 1, 2, null).IsSuccess, Is.False, "A committed card locks planning.");
                candidate = Require(deckCommands.ApplyWorldGateReceiptExactlyOnce(candidate, catalog));
            }
            Assert.That(reached, Is.True, "No forced node or battle fixture is used.");
            var combat = (M2CombatContent)typeof(M1RuntimeCoordinator).GetField("_combatContent", Private).GetValue(_owner);
            var resolver = (EncounterRosterResolver070)typeof(M1RuntimeCoordinator).GetField("_encounterRosterResolver070", Private).GetValue(_owner);
            Assert.That(resolver, Is.Not.Null, "Use the production certified enemy roster resolver.");
            candidate = Require(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(candidate, new M2BattleCommandService(), combat, resolver));
            var members = candidate.Battle.PlayerUnions.SelectMany(value => value.Members).Select(value => value.MemberId).ToArray();
            Assert.That(members, Does.Contain("PLAN_R4").And.Contain("PLAN_R5"));
            Assert.That(members, Does.Not.Contain("PLAN_R0"));
            Assert.That(members.Distinct().Count(), Is.EqualTo(members.Length));
            Assert.That(_commands.AssignReserveRecruitToUnion109(candidate, "PLAN_R0", 1, 2, null).IsSuccess, Is.False);
            new AtomicSaveStore().Write(_path, SaveEnvelopeV1.Create(candidate, DateTime.UtcNow));
            var battleReload = new AtomicSaveStore().ReadWithRecovery(_path);
            Assert.That(battleReload.IsSuccess, Is.True, string.Join(";", battleReload.Errors));
            Assert.That(CanonicalJson.Serialize(battleReload.Value.CampaignState.Battle), Is.EqualTo(CanonicalJson.Serialize(candidate.Battle)));
        }

        [Test]
        public void ActiveQuestPlanWriteFailureRetainsOriginalAndRetrySavesOnce132()
        {
            BeginSyntheticQuest132();
            var source = State132();
            var disk = File.ReadAllBytes(_path);
            var changed = 0; _owner.Changed += () => changed++;
            Directory.CreateDirectory(_path + ".tmp");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"(?s)SAVE_WRITE_FAILED109\s+System\.(UnauthorizedAccessException|IO\.IOException).*"));
            Assert.That(_owner.AssignReserveRecruitToUnion109("PLAN_R4", 0, 2, null).Succeeded, Is.False);
            Assert.That(ReferenceEquals(State132(), source), Is.True);
            CollectionAssert.AreEqual(disk, File.ReadAllBytes(_path));
            Assert.That(changed, Is.Zero);
            Directory.Delete(_path + ".tmp");
            Assert.That(_owner.AssignReserveRecruitToUnion109("PLAN_R4", 0, 2, null).Succeeded, Is.True);
            Assert.That(changed, Is.EqualTo(1));
            CollectionAssert.AreEqual(disk, File.ReadAllBytes(_path + ".bak"));
        }

        [Test]
        public void AuthoredRecruitMaterializationKeepsPlanAndCommittedUnionCount132()
        {
            // A small synthetic plan exercises the real recruit materializer;
            // it is not evidence that an opening reward was earned here.
            var source = Fresh132();
            var assigned = Require(_commands.AssignReserveRecruitToUnion109(source, "PLAN_R4", 0, 2, null));
            var planned = source.WithNextBattleUnions132(new UnionBattlePlan132(
                CanonicalJson.Sha256Hex(source.Guild.Unions), assigned.Guild.Unions));
            var planJson = CanonicalJson.Serialize(planned.NextBattleUnions132);
            var materializer = SecondDimension.Gameplay.FirstHour071.FirstHourRosterService071.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var rewarded = Require(materializer.EnsureCharterRoster(planned));
            Assert.That(rewarded.Guild.Recruits.Count, Is.EqualTo(source.Guild.Recruits.Count + 4));
            Assert.That(rewarded.Guild.Unions.Count, Is.EqualTo(2), "New heroes stay in reserve; no automatic Union expansion changes a committed roster.");
            Assert.That(CanonicalJson.Serialize(rewarded.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(source.Guild.Unions)));
            Assert.That(CanonicalJson.Serialize(rewarded.NextBattleUnions132), Is.EqualTo(planJson));
            Assert.That(CanonicalJson.Serialize(Require(materializer.EnsureCharterRoster(rewarded))), Is.EqualTo(CanonicalJson.Serialize(rewarded)));
            Assert.That(CanonicalJson.Serialize(JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(rewarded))), Is.EqualTo(CanonicalJson.Serialize(rewarded)));
        }

        [Test]
        public void NullPlanKeepsLegacyCanonicalAndForgedOrDuplicatePlansFail132()
        {
            var state = Fresh132();
            var json = CanonicalJson.Serialize(state);
            Assert.That(json, Does.Not.Contain("NextBattleUnions132"));
            Assert.That(CanonicalJson.Serialize(JsonConvert.DeserializeObject<CampaignState>(json)), Is.EqualTo(json));
            Assert.Throws<InvalidOperationException>(() => state.WithNextBattleUnions132(new UnionBattlePlan132(new string('0', 64), state.Guild.Unions)));
            var duplicate = state.Guild.Unions.ToArray();
            duplicate[1] = new UnionState("PLAN_U1", "Second", UnionKind.Normal, "PLAN_R0", new[] { "PLAN_R0", "PLAN_R3" }, "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000);
            Assert.Throws<InvalidOperationException>(() => state.WithNextBattleUnions132(new UnionBattlePlan132(CanonicalJson.Sha256Hex(state.Guild.Unions), duplicate)));
            var changed = Require(_commands.AssignReserveRecruitToUnion109(state, "PLAN_R4", 0, 2, null));
            var planned = state.WithNextBattleUnions132(new UnionBattlePlan132(CanonicalJson.Sha256Hex(state.Guild.Unions), changed.Guild.Unions));
            var promoted = UnionBattlePlanRules132.PromoteWhenIdle(planned);
            Assert.That(promoted.NextBattleUnions132, Is.Null);
            Assert.That(CanonicalJson.Serialize(promoted.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(changed.Guild.Unions)));
        }

        void BeginSyntheticQuest132()
        {
            _owner = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _path);
            typeof(M1RuntimeCoordinator).GetField("_campaign", Private).SetValue(_owner, Fresh132());
            var started = _owner.StartOrResumeCampaignDeck131("CH018_001");
            Assert.That(started.Succeeded, Is.True, started.Message);
            Assert.That(World132(State132()), Is.Not.Null);
        }

        static CampaignState Fresh132()
        {
            // Explicit synthetic roster/profile only; all quest grants and receipts
            // below are produced by shipping authorities, not injected as earned.
            var recruits = Enumerable.Range(0, 6).Select(i => new RecruitState("PLAN_R" + i, 100, 100, 20, 20)).ToArray();
            var unions = Enumerable.Range(0, 2).Select(i => new UnionState("PLAN_U" + i, "Union " + i,
                UnionKind.Normal, "PLAN_R" + (i * 2), new[] { "PLAN_R" + (i * 2), "PLAN_R" + (i * 2 + 1) },
                "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000)).ToArray();
            return new CampaignState("00000000-0000-0000-0000-000000000132", 132, "1.0", ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD_UNION132", 1000, recruits, unions),
                new NewGuildProfileState("Synthetic Union132", GameMode.Standard, TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false, 439, 0, true, true, true, true, "synthetic_union132"));
        }

        CampaignState State132() => (CampaignState)typeof(M1RuntimeCoordinator).GetField("_campaign", Private).GetValue(_owner);
        static WorldGateOperationState023 World132(CampaignState state) => state.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation;
        static CampaignState Require(Result<CampaignState> result) { Assert.That(result.IsSuccess, Is.True, string.Join(";", result.Errors)); return result.Value; }
        static string Hash(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", ""); }
    }
}
#endif
