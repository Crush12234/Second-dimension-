using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.PeopleBonds026;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Pure, copy-on-write M2 battle authority. Standard presentation can select
    /// complete Forecast IDs only; it has no member-Art command surface.
    /// </summary>
    public sealed partial class M2BattleCommandService
    {
        public const string TutorialBattleId = "BATTLE_TUTORIAL_UNION_FORECAST_001";
        public const string PreviousTutorialRulesVersion088 =
            "M2_CINEMATIC_UNION_COMBAT_021_PROGRESSION";
        public const string PreviousTutorialRulesVersion101 =
            "M2_CINEMATIC_UNION_COMBAT_021_PROGRESSION_ART_LEVELS_088";
        public const string TutorialRulesVersion =
            PreviousTutorialRulesVersion101 + "_BASIC_STRIKE_101";
        public const string HallBreachBattleToken076 = "ENCOUNTER071_HALL_BREACH";
        public const string LanternRoadBattleToken076 = "ENCOUNTER071_LANTERN_ROAD_AMBUSH";
        public const string GateEaterBattleToken076 = "ENCOUNTER071_GATE_EATER";
        public const string FogStalkersBattleToken078 = "ENCOUNTER_FOG_STALKERS_STANDARD";
        public const string SurveyorRescueBattleToken079 = "ENCOUNTER_SURVEYOR_RESCUE";
        public const string GateEaterBossClassToken076 = "HINGE_EATER_COLOSSUS";
        public const string GateironBruteUnionToken076 = "BRUTE_ESCORT";
        public const string GateironBruteClassToken076 = "GATEIRON_BRUTE";
        public const string StoryThreatTelegraphEventType076 = "STORY_THREAT_TELEGRAPH";
        public const int HallBreachThreatPercent076 = 15;
        public const int LanternRoadThreatPercent076 = 20;
        public const int GateEaterThreatPercent076 = 25;
        public const int GateEaterDeadlineRounds076 = 4;
        public const int GateEaterBossMinimumMaximumHp076 = 720;
        public const int GateEaterBossMinimumAttack076 = 36;
        public const int GateEaterBossMinimumUnionAp076 = 24;
        public const int HallBreachEnemyHpPercent078 = 275;
        public const int LanternRoadEnemyHpPercent078 = 275;
        public const int GateEaterEnemyHpPercent078 = 110;
        public const int FogStalkersEnemyHpPercent078 = 325;
        public const int SurveyorRescueEnemyHpPercent079 = 350;
        public const int CrossUnionCriticalHpPercent086 = 35;
        public const int CrossUnionLowHpPercent086 = 70;
        public const int CrossUnionCriticalCohesion086 = 35;
        public const int CrossUnionCriticalFormationBasisPoints086 = 4000;

        public Result<CampaignState> StartTutorialBattle(CampaignState campaign, M2CombatContent content) =>
            StartEncounterBattle(
                campaign,
                content,
                TutorialBattleId,
                "Defeat the Gate Gnawer training projection — or withdraw safely.",
                1,
                Array.Empty<string>(),
                null);

        public Result<CampaignState> StartEncounterBattle(
            CampaignState campaign,
            M2CombatContent content,
            string battleId,
            string objective,
            int enemyUnionCount = 1,
            IReadOnlyList<string> routeModifiers = null,
            IReadOnlyList<string> alliedUnionIds = null,
            bool guaranteeOpeningBreakthrough = false)
        {
            return StartEncounterBattleInternal(
                campaign,
                content,
                battleId,
                objective,
                enemyUnionCount,
                routeModifiers,
                alliedUnionIds,
                null,
                guaranteeOpeningBreakthrough,
                null);
        }

        /// <summary>
        /// Starts a committed expedition battle with the deterministic Pass 03
        /// roster selected for that exact encounter. The legacy entry point above
        /// remains unchanged for tutorial and compatibility callers.
        /// </summary>
        public Result<CampaignState> StartEncounterBattleWithRoster070(
            CampaignState campaign,
            M2CombatContent content,
            string battleId,
            string objective,
            EncounterRoster070 encounterRoster,
            IReadOnlyList<string> routeModifiers = null,
            IReadOnlyList<string> alliedUnionIds = null,
            bool guaranteeOpeningBreakthrough = false)
        {
            if (encounterRoster == null)
                return Result<CampaignState>.Failure("M2_ENCOUNTER_ROSTER_070_REQUIRED");
            if (StringComparer.Ordinal.Equals(battleId, TutorialBattleId))
                return Result<CampaignState>.Failure("M2_TUTORIAL_ROSTER_070_FORBIDDEN");
            return StartEncounterBattleInternal(
                campaign,
                content,
                battleId,
                objective,
                encounterRoster.Unions.Count,
                routeModifiers,
                alliedUnionIds,
                encounterRoster,
                guaranteeOpeningBreakthrough,
                null);
        }

        /// <summary>
        /// The only gameplay entry point allowed to start a battle while a Guild City
        /// encounter request is pending. The request must be the exact committed value;
        /// free-form battle APIs cannot reuse only its BattleId with weaker parameters.
        /// </summary>
        internal Result<CampaignState> StartCommittedEncounterBattle017D(
            CampaignState campaign,
            M2CombatContent content,
            EncounterLaunchRequest017D committedRequest,
            EncounterRoster070 encounterRoster = null)
        {
            if (campaign?.Guild?.GuildCity?.PendingEncounter == null ||
                committedRequest == null ||
                !StringComparer.Ordinal.Equals(
                    CanonicalJson.Serialize(campaign.Guild.GuildCity.PendingEncounter),
                    CanonicalJson.Serialize(committedRequest)))
                return Result<CampaignState>.Failure(
                    "M2_COMMITTED_ENCOUNTER_REQUEST_MISMATCH");
            if (encounterRoster != null &&
                encounterRoster.Unions.Count != committedRequest.EnemyUnionCount)
                return Result<CampaignState>.Failure(
                    "M2_COMMITTED_ENCOUNTER_ROSTER_COUNT_MISMATCH");
            if (GuildCityBattleBridgeService017D
                    .RequiresEncounterRequestAuthority084(committedRequest) &&
                !campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityBattleBridgeService017D
                        .EncounterRequestAuthorityId084(committedRequest)))
                return Result<CampaignState>.Failure(
                    "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED");

            return StartEncounterBattleInternal(
                campaign,
                content,
                committedRequest.BattleId,
                committedRequest.Objective,
                committedRequest.EnemyUnionCount,
                committedRequest.RouteModifiers,
                committedRequest.AlliedUnionIds,
                encounterRoster,
                StringComparer.Ordinal.Equals(
                    committedRequest.EncounterId,
                    GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071),
                committedRequest);
        }

        private Result<CampaignState> StartEncounterBattleInternal(
            CampaignState campaign,
            M2CombatContent content,
            string battleId,
            string objective,
            int enemyUnionCount,
            IReadOnlyList<string> routeModifiers,
            IReadOnlyList<string> alliedUnionIds,
            EncounterRoster070 encounterRoster,
            bool guaranteeOpeningBreakthrough,
            EncounterLaunchRequest017D committedRequest,
            string replayRulesVersion101 = null,
            TownBattleBonuses159 replayTownBonuses159 = null,
            M2BattlePolicy163 replayProgression163 = null)
        {
            if (campaign == null) return Result<CampaignState>.Failure("M2_CAMPAIGN_REQUIRED");
            if (content == null) return Result<CampaignState>.Failure("M2_COMBAT_CONTENT_REQUIRED");
            var rulesVersion101 = replayRulesVersion101 ?? TutorialRulesVersion;
            if (rulesVersion101 != TutorialRulesVersion &&
                rulesVersion101 != PreviousTutorialRulesVersion101 &&
                rulesVersion101 != PreviousTutorialRulesVersion088)
                return Result<CampaignState>.Failure("M2_REPLAY_RULES_UNSUPPORTED_101");
            if (string.IsNullOrWhiteSpace(battleId)) return Result<CampaignState>.Failure("M2_BATTLE_ID_REQUIRED");
            if (string.IsNullOrWhiteSpace(objective)) return Result<CampaignState>.Failure("M2_OBJECTIVE_REQUIRED");
            var guildCity=campaign.Guild?.GuildCity;
            var pendingEncounter=guildCity?.PendingEncounter;
            var linkedPendingEncounter=pendingEncounter!=null&&
                committedRequest!=null&&
                StringComparer.Ordinal.Equals(
                    CanonicalJson.Serialize(pendingEncounter),
                    CanonicalJson.Serialize(committedRequest));
            if (pendingEncounter != null && !linkedPendingEncounter)
                return Result<CampaignState>.Failure(
                    "M2_PENDING_ENCOUNTER_REQUIRES_COMMITTED_BRIDGE");
            var resumingSameBattle=campaign.Battle!=null&&
                campaign.Battle.Outcome==BattleOutcome.InProgress&&
                StringComparer.Ordinal.Equals(campaign.Battle.BattleId,battleId);
            if (campaign.Battle != null &&
                campaign.Battle.Outcome == BattleOutcome.InProgress &&
                !resumingSameBattle)
                return Result<CampaignState>.Failure("M2_ACTIVE_BATTLE_MUST_FINISH");
            if (campaign.Battle != null && campaign.Battle.Outcome != BattleOutcome.InProgress &&
                campaign.Battle.Reward != null && !campaign.Battle.Reward.Claimed)
                return Result<CampaignState>.Failure("M2_UNCLAIMED_REWARD_REQUIRED");
            if (GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign) &&
                !linkedPendingEncounter&&!resumingSameBattle)
                return Result<CampaignState>.Failure(
                    "M2_FINISH_ACTIVE_ADVENTURE_FIRST");
            if (campaign.OpeningFlow == null ||
                (!campaign.OpeningFlow.UnionBuilderCompleted &&
                 campaign.OpeningFlow.Stage != OpeningStage.Complete))
                return Result<CampaignState>.Failure("M2_COMPLETED_OPENING_REQUIRED");
            if (campaign.Battle != null && campaign.Battle.Outcome == BattleOutcome.InProgress)
            {
                if (!StringComparer.Ordinal.Equals(campaign.Battle.BattleId, battleId))
                    return Result<CampaignState>.Failure("M2_ACTIVE_BATTLE_MUST_FINISH");
                if (!UsesCurrentTutorialRules(campaign.Battle, content))
                    return Result<CampaignState>.Failure("M2_ACTIVE_BATTLE_RULES_MISMATCH");
                return Result<CampaignState>.Success(campaign);
            }

            try
            {
                var isTutorialBattle = StringComparer.Ordinal.Equals(battleId, TutorialBattleId);
                CampaignProgressionCommandService022.ValidateCommittedTowerThreat098(
                    campaign, committedRequest, battleId, routeModifiers);
                var players = CreatePlayerUnionsForRules101(
                    campaign, content, alliedUnionIds, isTutorialBattle ? 2 : 10,
                    rulesVersion101 == TutorialRulesVersion);
                if (players.Count == 0) return Result<CampaignState>.Failure("M2_ACTIVE_UNION_REQUIRED");
                var titanAttempt161=ValidateTitanStart161(campaign,content,committedRequest,battleId,encounterRoster,players);
                enemyUnionCount = Math.Max(1, Math.Min(10, enemyUnionCount));
                EnemyForceProfile094.ValidateRoster094(committedRequest, encounterRoster);
                TowerEnemyPartyRules137.ValidateRoster137(committedRequest, encounterRoster);
                var enemies = encounterRoster == null
                    ? CreateEnemyUnions(content, enemyUnionCount,
                        StringComparer.Ordinal.Equals(battleId, TutorialBattleId))
                    : CreateEnemyUnions070(content, encounterRoster);
                var authoredEnemies163 = enemies;
                players = ApplyRouteModifiersToPlayers(players, routeModifiers);
                enemies = ApplyRouteModifiersToEnemies(enemies, routeModifiers);
                enemies = ApplyGateEaterClimaxAuthority076(enemies, battleId);
                enemies = ApplyFirstSliceEncounterPacing078(enemies, battleId);
                enemies = OrderFirstHourSignatureThreatLast076(enemies, battleId);
                // Presentation-only IDs are committed after every combat scaling
                // transform so no gameplay rule or damage calculation can depend
                // on the Enemy Art 700 selection.
                enemies = EnemyArtIdentity090.CommitIdentities090(enemies, battleId);
                // Absolute Titan profiles bypass ordinary enemy variation and
                // pacing transforms. Player town/route benefits still apply.
                if(titanAttempt161!=null)enemies=CreateFixedTitanEnemies161(content,titanAttempt161);
                var gateEaterClimax = IsGateEaterEncounter076(battleId) &&
                                     ContainsGateEaterBoss076(enemies);
                BattleMemberState breakthroughMember = null;
                var breakthroughArt = string.Empty;
                var hasBreakthrough = (isTutorialBattle || guaranteeOpeningBreakthrough) &&
                    TrySelectTutorialBreakthrough(
                    players, content, out breakthroughMember, out breakthroughArt);
                if (hasBreakthrough)
                    players = PrepareTutorialBreakthrough(
                        players,
                        breakthroughMember.MemberId,
                        breakthroughArt,
                        content.Art(breakthroughArt).Discipline);
                if (isTutorialBattle) players = PrepareRestorationDemonstration(players, content);
                var openingEvents = new List<BattleEventState>(
                    BuildOpeningEvents(routeModifiers, enemyUnionCount));
                var storyThreatTelegraph = BuildStoryThreatTelegraph076(
                    battleId, players, enemies, openingEvents.Count);
                if (storyThreatTelegraph != null)
                    openingEvents.Add(storyThreatTelegraph);
                if (gateEaterClimax)
                    openingEvents.Add(Event(
                        openingEvents.Count,
                        1,
                        "BOSS_ADVANCE_CLOCK",
                        BattleSide.Enemy,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        "The Gate-Eater will breach the Skyhome approach after " +
                        GateEaterDeadlineRounds076 + " unresolved rounds.",
                        GateEaterDeadlineRounds076));
                // Replay carries the original optional snapshot, including legacy
                // null. A newly started fight captures town investment once.
                var treasuryTown159 = TownProgression159.TreasuryBonusBasisPoints(campaign);
                var personalTown159 = TownProgression159.PersonalBonusBasisPoints(campaign);
                var townBonuses159 = replayRulesVersion101 != null ? replayTownBonuses159 :
                    treasuryTown159 == 0 && personalTown159 == 0 ? null :
                    new TownBattleBonuses159(treasuryTown159, personalTown159);
                var initial = new BattleState(
                    battleId, EffectiveContentVersion(content, rulesVersion101), 1, BattlePhase.ForecastSelection, BattleOutcome.InProgress,
                    objective,
                    players, enemies, Array.Empty<BattleForecastState>(), Array.Empty<BattleForecastSelectionState>(),
                    openingEvents.AsReadOnly(),
                    Array.Empty<BattleRoundRecordState>(), string.Empty, string.Empty, string.Empty,
                    hasBreakthrough ? breakthroughMember.MemberId : string.Empty,
                    hasBreakthrough ? breakthroughArt : string.Empty,
                    false, townBonuses159: townBonuses159,
                    titanRuntime161:titanAttempt161==null?null:new SecondDimension.Gameplay.TitanTrials160.TitanBossRuntime161(titanAttempt161),
                    titanHeroes161:SecondDimension.Gameplay.TitanHeroes161.TitanHeroBattleRuntime161.Capture(campaign),
                    progression163:replayRulesVersion101 != null ? replayProgression163 :
                        M2BattlePolicy163.Capture(campaign, players, authoredEnemies163, routeModifiers));
                var initialIntegrityHash090 = AuthoritativeStateHash(initial);
                var initialGameplayHash090 = GameplayRngStateHash090(initial);
                initial = initial.With(
                    initialBattleStateHash: initialGameplayHash090,
                    initialIntegrityStateHash090: initialIntegrityHash090);
                initial = CommitForecasts(campaign, initial, content);
                return Result<CampaignState>.Success(campaign.WithBattle(initial));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("M2_START_FAILED: " + exception.Message);
            }
        }

        public Result<CampaignState> SelectForecast(CampaignState campaign, string unionId, string forecastId)
        {
            var validation = ValidateSelecting(campaign);
            if (validation != null) return Result<CampaignState>.Failure(validation);
            var battle = campaign.Battle;
            var forecast = FindForecast(battle.CommittedForecasts, unionId, forecastId);
            if (forecast == null) return Result<CampaignState>.Failure("M2_COMMITTED_FORECAST_REQUIRED");
            var selections = new List<BattleForecastSelectionState>();
            var replaced = false;
            for (var i = 0; i < battle.Selections.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(battle.Selections[i].UnionId, unionId))
                {
                    selections.Add(new BattleForecastSelectionState(unionId, forecastId));
                    replaced = true;
                }
                else selections.Add(battle.Selections[i]);
            }
            if (!replaced) selections.Add(new BattleForecastSelectionState(unionId, forecastId));
            selections.Sort((left, right) => StringComparer.Ordinal.Compare(left.UnionId, right.UnionId));
            return Result<CampaignState>.Success(campaign.WithBattle(battle.With(selections: selections.AsReadOnly())));
        }

        public Result<CampaignState> ConfirmRound(CampaignState campaign, M2CombatContent content)
        {
            var validation = ValidateSelecting(campaign);
            if (validation != null) return Result<CampaignState>.Failure(validation);
            if (content == null) return Result<CampaignState>.Failure("M2_COMBAT_CONTENT_REQUIRED");
            var battle = campaign.Battle;
            var activeCount = CountActive(battle.PlayerUnions);
            if (battle.Selections.Count != activeCount) return Result<CampaignState>.Failure("M2_ONE_FORECAST_PER_ACTIVE_UNION_REQUIRED");

            try
            {
                var players = new List<BattleUnionState>(battle.PlayerUnions);
                var enemies = new List<BattleUnionState>(battle.EnemyUnions);
                var events = new List<BattleEventState>();
                if(battle.TitanRuntime161!=null&&!HasValidTitanOwner161(campaign,content))
                    throw new InvalidOperationException("TITAN161_NATIVE_OWNER_REQUIRED");
                ValidateTitanAction161(battle,content);
                BeginTitanRound161(battle,events);
                BeginProgressionRound163(battle,events);
                var selectedForecasts = new List<BattleForecastState>();
                var ultimateArtApplications001 = new List<SpecialRelicUltimateArtApplicationProof001>();
                var peopleBondState = PeopleBondBattleHook026.GetState(campaign);
                var sssBattleRuntime090 = battle.SssBattleRuntime090;
                var sssCampaignState090 = SssTenV4CampaignAccessor090.Read(campaign);
                // Guests enter after the round-start command census and receive
                // their first complete Forecast on the following round.
                var roundOrder161=battle.TitanRuntime161?.Action?.PlayerActionOrder??battle.PlayerUnions.Select(u=>u.UnionId).ToArray();
                if(roundOrder161.Count!=battle.PlayerUnions.Count||!roundOrder161.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(battle.PlayerUnions.Select(u=>u.UnionId).OrderBy(x=>x,StringComparer.Ordinal)))
                    throw new InvalidOperationException("TITAN161_COMMITTED_ARMY_ORDER_REQUIRED");
                for (var orderIndex161 = 0; orderIndex161 < roundOrder161.Count; orderIndex161++)
                {
                    var unionIndex=FindUnionIndex(battle.PlayerUnions,roundOrder161[orderIndex161]);
                    if (AllDefeated(enemies)) break;
                    // A Union revived by an earlier allied order rejoins the field
                    // immediately, but it did not exist in the round-start command
                    // census and therefore cannot be required to provide a Forecast
                    // until the next round.
                    if (!IsActive(battle.PlayerUnions[unionIndex])) continue;
                    var union = players[unionIndex];
                    var selection = FindSelection(battle.Selections, union.UnionId);
                    if (selection == null) return Result<CampaignState>.Failure("M2_ONE_FORECAST_PER_ACTIVE_UNION_REQUIRED");
                    var forecast = FindForecast(battle.CommittedForecasts, union.UnionId, selection.ForecastId);
                    if (forecast == null) return Result<CampaignState>.Failure("M2_SELECTED_FORECAST_NOT_COMMITTED");
                    if (forecast.SharedApCost > union.CurrentAp) return Result<CampaignState>.Failure("M2_SHARED_AP_BUDGET_EXCEEDED");
                    ValidateIndividualMp(union, forecast);
                    selectedForecasts.Add(forecast);
                    var sssGoldForecast090 =
                        SssBattleIntegration090.IsGoldForecast090(forecast);
                    var sssSyntheticForecast090 =
                        SssBattleIntegration090.IsSyntheticForecast090(forecast);
                    var titanHeroForecast161=IsTitanHeroForecast161(forecast);
                    if(titanHeroForecast161)
                    {
                        var heroRuntime161=GetTitanHeroRuntime161(events);
                        if(!TryResolveTitanHeroForecast161(campaign,battle,unionIndex,forecast,players,enemies,events,
                            ref heroRuntime161,out var titanFailure161))throw new InvalidOperationException(titanFailure161);
                        SetTitanHeroRuntime161(events,heroRuntime161);
                        if(TryBuildTitanHeroCompanion161(forecast,out var titanCompanion161))
                            ResolvePlayerForecast(campaign,battle,content,campaign.Rules,unionIndex,titanCompanion161,players,enemies,events);
                    }
                    else if (sssGoldForecast090)
                    {
                        if (!SssBattleIntegration090.TryResolveGoldForecast090(
                                campaign, battle, unionIndex, forecast,
                                players, enemies, events,
                                ref sssBattleRuntime090,
                                ref sssCampaignState090,
                                out var sssFailure090))
                            throw new InvalidOperationException(sssFailure090);
                        if (SssBattleIntegration090.TryBuildCompanionForecast090(
                                forecast,
                                out var companionForecast090))
                            ResolvePlayerForecast(
                                campaign,
                                battle,
                                content,
                                campaign.Rules,
                                unionIndex,
                                companionForecast090,
                                players,
                                enemies,
                                events);
                    }
                    else
                        ResolvePlayerForecast(
                            campaign, battle, content, campaign.Rules,
                            unionIndex, forecast, players, enemies, events);
                    var victoryReached088 = AllDefeated(enemies);
                    var hasUltimateArt001 = !string.IsNullOrWhiteSpace(forecast.LearningOpportunity) &&
                        forecast.LearningOpportunity.IndexOf(
                            SpecialRelicUltimateArtBattleHook001.ArtMarker,
                            StringComparison.Ordinal) >= 0;
                    SpecialRelicUltimateArtApplicationProof001 ultimateArtApplication001 = null;
                    if (!sssGoldForecast090 && !sssSyntheticForecast090 && !titanHeroForecast161 &&
                        !victoryReached088 && hasUltimateArt001 && !SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                            campaign, battle, forecast, unionIndex, players, enemies, events,
                            out ultimateArtApplication001,
                            applyInvocationBattleEffect: true))
                        throw new InvalidOperationException("SPECIAL_RELIC001_ULTIMATE_FORECAST_AUTHORITY_INVALID");
                    if (ultimateArtApplication001 != null)
                        ultimateArtApplications001.Add(ultimateArtApplication001);
                    if (victoryReached088) break;
                    if (!sssGoldForecast090 && !sssSyntheticForecast090 && !titanHeroForecast161)
                        peopleBondState = PeopleBondBattleHook026.ApplySelectedForecast(
                            campaign, battle, forecast, unionIndex, players, enemies, events, peopleBondState);
                }

                var retreated = CountActive(players) == 0 && !AllDefeated(players);
                var outcome = enemies.Count == 0 || AllDefeated(enemies)
                    ? BattleOutcome.Victory
                    : AllDefeated(players)
                        ? BattleOutcome.Defeat
                        : retreated
                            ? BattleOutcome.Retreat
                            : BattleOutcome.InProgress;
                if (outcome == BattleOutcome.InProgress && !ResolveTitanEnemyTurn161(battle,content,players,enemies,events))
                    ResolveEnemyTurn(
                        campaign.CampaignSeed, battle, content, players, enemies, events);
                SssBattleIntegration090.SynchronizeTransformations090(
                    players,
                    ref sssBattleRuntime090);
                ApplyDeferredGuardProgress(campaign, battle, content, campaign.Rules, selectedForecasts, players, events);
                outcome = AllDefeated(enemies)
                    ? BattleOutcome.Victory
                    : AllDefeated(players)
                        ? BattleOutcome.Defeat
                        : CountActive(players) == 0
                            ? BattleOutcome.Retreat
                            : BattleOutcome.InProgress;

                if (outcome == BattleOutcome.InProgress && IsGateEaterBattle076(battle))
                {
                    var remaining = GateEaterDeadlineRounds076 - battle.Round;
                    if (remaining <= 0)
                    {
                        outcome = BattleOutcome.Defeat;
                        events.Add(Event(
                            events.Count,
                            battle.Round,
                            "BOSS_BREACH",
                            BattleSide.Enemy,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            "The Gate-Eater breaches the Skyhome approach before the Guild can bring it down.",
                            0));
                    }
                    else
                    {
                        events.Add(Event(
                            events.Count,
                            battle.Round,
                            "BOSS_ADVANCE_CLOCK",
                            BattleSide.Enemy,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            "The Gate-Eater advances. " + remaining +
                            (remaining == 1 ? " round remains" : " rounds remain") +
                            " before the Skyhome breach.",
                            remaining));
                    }
                }

                EndTitanRound161(battle,players,enemies,events);
                // Timed native effects may be terminal; evaluate after their
                // real HP/retreat changes before constructing a reward.
                if(outcome==BattleOutcome.InProgress)
                    outcome=AllDefeated(enemies)?BattleOutcome.Victory:AllDefeated(players)?BattleOutcome.Defeat:
                        CountActive(players)==0?BattleOutcome.Retreat:BattleOutcome.InProgress;
                RecoverRoundResources(players);
                RecoverRoundResources(enemies);
                if (outcome != BattleOutcome.InProgress)
                {
                    SssBattleIntegration090.RestoreTerminalActors090(
                        battle.Round,
                        players,
                        events,
                        ref sssBattleRuntime090);
                    var resultText = BattleResultSummary(battle, outcome);
                    events.Add(Event(events.Count, battle.Round, "BATTLE_RESULT", BattleSide.Player,
                        string.Empty, string.Empty, string.Empty, resultText, 0));
                }
                var accumulatedEvents = new List<BattleEventState>(battle.EventLog);
                accumulatedEvents.AddRange(events);
                var occurred = battle.TutorialBreakthroughOccurred || ContainsTutorialBreakthrough070(battle, events);
                var resolved = battle.With(
                    phase: outcome == BattleOutcome.InProgress ? BattlePhase.ForecastSelection : BattlePhase.Resolved,
                    outcome: outcome,
                    playerUnions: players.AsReadOnly(), enemyUnions: enemies.AsReadOnly(),
                    committedForecasts: Array.Empty<BattleForecastState>(), selections: Array.Empty<BattleForecastSelectionState>(),
                    eventLog: accumulatedEvents.AsReadOnly(), tutorialBreakthroughOccurred: occurred,
                    sssBattleRuntime090: sssBattleRuntime090,
                    titanRuntime161:TitanRuntimeAfterRound161(events),titanHeroes161:GetTitanHeroRuntime161(events));
                if (outcome != BattleOutcome.InProgress)
                    resolved = resolved.WithReward(M2ProgressionRewards.CreatePending(campaign, resolved, content));
                var preRoundGameplayHash090 = GameplayRngStateHash090(battle);
                var traceHash = CanonicalJson.Sha256Hex(new
                {
                    battle.BattleId,
                    battle.Round,
                    PreRound = preRoundGameplayHash090,
                    LegalPools = selectedForecasts.ConvertAll(item => item.DeterministicDebugEvidence),
                    Selections = battle.Selections,
                    Events = events
                });
                var postRoundHash = GameplayRngStateHash090(resolved);
                var roundRecord = new BattleRoundRecordState(
                    battle.Round, preRoundGameplayHash090, battle.Selections,
                    events.AsReadOnly(), traceHash, postRoundHash);
                var records = new List<BattleRoundRecordState>(battle.RoundRecords) { roundRecord };
                resolved = resolved.With(roundRecords: records.AsReadOnly());

                CampaignState updatedCampaign;
                if (outcome == BattleOutcome.InProgress)
                {
                    resolved = resolved.With(round: battle.Round + 1);
                    updatedCampaign = PeopleBondBattleHook026.WithState(campaign.WithBattle(resolved), peopleBondState);
                }
                else
                {
                    resolved = resolved.With(
                        finalStateHash: GameplayRngStateHash090(resolved),
                        finalIntegrityStateHash090: AuthoritativeStateHash(resolved));
                    updatedCampaign = PeopleBondBattleHook026.WithState(campaign.WithBattle(resolved), peopleBondState);
                }
                updatedCampaign = SssTenV4CampaignAccessor090.WithState(
                    updatedCampaign,
                    sssCampaignState090);
                var ultimateArtProgression001 = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                    updatedCampaign,
                    ultimateArtApplications001.AsReadOnly());
                if (!ultimateArtProgression001.IsSuccess)
                    return Result<CampaignState>.Failure(ultimateArtProgression001.Errors.ToArray());
                updatedCampaign = ultimateArtProgression001.Value;
                if (outcome == BattleOutcome.InProgress)
                {
                    resolved = CommitForecasts(updatedCampaign, updatedCampaign.Battle, content);
                    updatedCampaign = updatedCampaign.WithBattle(resolved);
                }
                return Result<CampaignState>.Success(updatedCampaign);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("M2_ROUND_REJECTED: " + exception.Message);
            }
        }

        public Result<CampaignState> RestartTutorialBattle(CampaignState campaign, M2CombatContent content)
        {
            if (campaign == null) return Result<CampaignState>.Failure("M2_CAMPAIGN_REQUIRED");
            if (campaign.Battle != null && campaign.Battle.Outcome != BattleOutcome.InProgress &&
                campaign.Battle.Reward != null && !campaign.Battle.Reward.Claimed)
            {
                var claim = ClaimBattleRewards(campaign);
                if (!claim.IsSuccess) return claim;
                campaign = claim.Value;
            }
            return StartTutorialBattle(campaign.WithBattle(null), content);
        }

        public Result<CampaignState> ClaimBattleRewards(CampaignState campaign)
        {
            if (campaign?.Battle == null) return Result<CampaignState>.Failure("M2_REWARD_BATTLE_REQUIRED");
            var battle = campaign.Battle;
            if (battle.Outcome == BattleOutcome.InProgress || battle.Phase != BattlePhase.Resolved)
                return Result<CampaignState>.Failure("M2_TERMINAL_REWARD_REQUIRED");
            if (battle.Reward == null) return Result<CampaignState>.Failure("M2_PENDING_REWARD_REQUIRED");
            if (battle.Reward.Claimed) return Result<CampaignState>.Success(campaign);

            try
            {
                var reward = battle.Reward;
                if (reward.Outcome != battle.Outcome)
                    throw new InvalidOperationException("Pending reward outcome differs from the terminal battle.");
                var development = campaign.Guild.Development;
                if (development.HasClaimedReward(reward.RewardId))
                {
                    var alreadyRecorded090 = campaign.WithBattle(
                        battle.WithReward(reward.WithClaimed(true)));
                    var sssAlreadyRecorded090 =
                        SssTenV4ProgressionService090.ApplyClaimedBattleReward090(
                            alreadyRecorded090);
                    return sssAlreadyRecorded090.IsSuccess
                        ? Result<CampaignState>.Success(sssAlreadyRecorded090.Value)
                        : Result<CampaignState>.Failure(
                            sssAlreadyRecorded090.Errors.ToArray());
                }

                var recruits = new List<RecruitState>(campaign.Guild.Recruits);
                var inventory = new List<EquipmentItemState>(campaign.Guild.Inventory);
                if (reward.EquipmentReward != null)
                {
                    if (ContainsInventoryItem(inventory, reward.EquipmentReward.InstanceId) ||
                        IsEquipmentItemAssigned(recruits, reward.EquipmentReward.InstanceId))
                        throw new InvalidOperationException(
                            "Pending equipment reward instance already exists: " +
                            reward.EquipmentReward.InstanceId + ".");
                    inventory.Add(reward.EquipmentReward);
                }
                var parkedReward164 = ResolveParkedRewardContext164(campaign, battle);
                var routeModifiers017H = campaign.Guild.GuildCity?.PendingEncounter?.RouteModifiers;
                for (var rewardIndex = 0; rewardIndex < reward.MemberRewards.Count; rewardIndex++)
                {
                    var memberReward = reward.MemberRewards[rewardIndex];
                    var recruitIndex = FindRecruitIndex(recruits, memberReward.MemberId);
                    if (recruitIndex < 0)
                        throw new InvalidOperationException("Reward recruit is missing: " + memberReward.MemberId + ".");
                    var battleMember = FindBattleMember(battle.PlayerUnions, memberReward.MemberId);
                    var recruit = recruits[recruitIndex];
                    if (parkedReward164 != null)
                    {
                        recruits[recruitIndex] = recruit.WithProgression(MergeParkedMemberReward164(
                            campaign, parkedReward164, memberReward, battleMember, recruit.Progression));
                        continue;
                    }
                    var progression = recruit.Progression.GainPersonalXp(
                        memberReward.PersonalXp,
                        battleMember.ClassId);
                    ValidateMemberRewardProjection(memberReward, recruit.Progression, progression);
                    var mastery = new List<RecruitArtMasteryState>();
                    for (var artIndex = 0; artIndex < battleMember.ArtProgress.Count; artIndex++)
                    {
                        var art = battleMember.ArtProgress[artIndex];
                        var previousMastery017H = FindPersistentMastery017H(recruit.Progression.ArtMastery, art.ArtId);
                        var previousPoints017H = previousMastery017H?.MasteryPoints ?? 0;
                        var earnedPoints017H = Math.Max(0, art.MasteryPoints - previousPoints017H);
                        var masteryBonus017H = GuildCityBattleModifierRules017H.MasteryBasisPoints(routeModifiers017H, art.Discipline);
                        var finalPoints017H = checked(previousPoints017H + GuildCityBattleModifierRules017H.ApplyBasisPoints(earnedPoints017H, masteryBonus017H));
                        mastery.Add(new RecruitArtMasteryState(
                            art.ArtId,
                            art.Discipline,
                            art.MeaningfulUses,
                            finalPoints017H));
                    }
                    progression = progression.WithArts(battleMember.LearnedArtIds, mastery.AsReadOnly());
                    recruits[recruitIndex] = recruit.WithProgression(progression);
                }

                development = development.RecordBattleReward(
                    reward.RewardId,
                    reward.GuildTreasuryXpAward,
                    reward.HallEnhancementXpAward);
                var guild = campaign.Guild.With(
                    checked(campaign.Guild.TreasuryXp + reward.GuildTreasuryXpAward),
                    recruits.AsReadOnly(),
                    campaign.Guild.Unions,
                    inventory.AsReadOnly(),
                    development);
                var claimedBattle = battle.WithReward(reward.WithClaimed(true));
                var claimedCampaign090 = campaign
                    .With(guild, campaign.OpeningFlow)
                    .WithBattle(claimedBattle);
                var sssClaim090 =
                    SssTenV4ProgressionService090.ApplyClaimedBattleReward090(
                        claimedCampaign090);
                return sssClaim090.IsSuccess
                    ? SecondDimension.Gameplay.GuildCity017D.CatchUpTraining153.ApplyClaimedVictory(campaign,sssClaim090.Value)
                    : Result<CampaignState>.Failure(sssClaim090.Errors.ToArray());
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("M2_REWARD_CLAIM_REJECTED: " + exception.Message);
            }
        }

        private static RecruitArtMasteryState FindPersistentMastery017H(IReadOnlyList<RecruitArtMasteryState> values, string artId)
        {
            if (values != null)
                for (var i = 0; i < values.Count; i++)
                    if (StringComparer.Ordinal.Equals(values[i].ArtId, artId)) return values[i];
            return null;
        }

        public Result<CampaignState> ReplayTutorialBattle(CampaignState campaign, M2CombatContent content)
        {
            if (campaign?.Battle == null) return Result<CampaignState>.Failure("M2_REPLAY_SOURCE_REQUIRED");
            if (campaign.Battle.Reward != null && campaign.Battle.Reward.Claimed)
                return Result<CampaignState>.Failure("M2_REPLAY_REQUIRES_UNCLAIMED_SOURCE");
            if (!UsesCurrentTutorialRules(campaign.Battle, content))
                return Result<CampaignState>.Failure("M2_REPLAY_RULES_UNSUPPORTED_101");
            var sourceRecords = campaign.Battle.RoundRecords;
            // Rebuild with the recorded policy, never rewrite legacy Forecast IDs,
            // damage/mastery or RNG inputs merely because a newer executable is loaded.
            var recordedRules101 = campaign.Battle.ContentVersion.Substring(content.ContentVersion.Length + 1);
            var replay = StartEncounterBattleInternal(
                campaign.WithBattle(null), content, TutorialBattleId,
                "Defeat the Gate Gnawer training projection — or withdraw safely.",
                1, Array.Empty<string>(), null, null, false, null, recordedRules101,
                campaign.Battle.TownBonuses159, campaign.Battle.Progression163);
            if (!replay.IsSuccess) return replay;
            var state = replay.Value;
            for (var roundIndex = 0; roundIndex < sourceRecords.Count; roundIndex++)
            {
                var source = sourceRecords[roundIndex];
                for (var selectionIndex = 0; selectionIndex < source.Selections.Count; selectionIndex++)
                {
                    var selected = SelectForecast(state, source.Selections[selectionIndex].UnionId, source.Selections[selectionIndex].ForecastId);
                    if (!selected.IsSuccess) return Result<CampaignState>.Failure("M2_REPLAY_SELECTION_DIVERGED");
                    state = selected.Value;
                }
                var confirmed = ConfirmRound(state, content);
                if (!confirmed.IsSuccess) return Result<CampaignState>.Failure("M2_REPLAY_RESOLUTION_DIVERGED");
                state = confirmed.Value;
            }
            if (!StringComparer.Ordinal.Equals(
                    CanonicalJson.Sha256Hex(state.Battle.EventLog),
                    CanonicalJson.Sha256Hex(campaign.Battle.EventLog)) ||
                !StringComparer.Ordinal.Equals(AuthoritativeStateHash(state.Battle), AuthoritativeStateHash(campaign.Battle)))
                return Result<CampaignState>.Failure("M2_REPLAY_HASH_DIVERGED");
            return Result<CampaignState>.Success(state);
        }

        public static bool UsesCurrentTutorialRules(BattleState battle, M2CombatContent content)
        {
            if (battle == null || content == null) return false;
            return StringComparer.Ordinal.Equals(
                       battle.ContentVersion,
                       EffectiveContentVersion(content)) ||
                   StringComparer.Ordinal.Equals(
                       battle.ContentVersion,
                       EffectiveContentVersion(content, PreviousTutorialRulesVersion101)) ||
                   StringComparer.Ordinal.Equals(
                       battle.ContentVersion,
                       EffectiveContentVersion(
                           content,
                           PreviousTutorialRulesVersion088));
        }

        /// <summary>
        /// Only battles created under Version 88 receive mastery power scaling.
        /// The immediately previous rules version remains resumable at its original
        /// 1.0x behavior so loading a save cannot rewrite committed Forecasts.
        /// </summary>
        public static bool UsesArtMasteryLevelScaling088(
            BattleState battle,
            M2CombatContent content) =>
            battle != null && content != null &&
            (StringComparer.Ordinal.Equals(
                 battle.ContentVersion, EffectiveContentVersion(content)) ||
             StringComparer.Ordinal.Equals(
                 battle.ContentVersion, EffectiveContentVersion(content, PreviousTutorialRulesVersion101)));

        public static int EffectiveArtPowerPermille088(
            BattleState battle,
            M2CombatContent content,
            BattleMemberState member,
            string artId)
        {
            if (!UsesArtMasteryLevelScaling088(battle, content) ||
                member == null || string.IsNullOrWhiteSpace(artId))
                return M2ArtMasteryLevelPolicy088.BasePowerPermille;
            for (var index = 0; index < member.ArtProgress.Count; index++)
                if (StringComparer.Ordinal.Equals(
                        member.ArtProgress[index].ArtId,
                        artId))
                    return M2ArtMasteryLevelPolicy088
                        .PowerPermilleForMasteryPoints(
                            member.ArtProgress[index].MasteryPoints);
            return M2ArtMasteryLevelPolicy088.BasePowerPermille;
        }

        public static string AuthoritativeStateHash(BattleState battle)
        {
            if (battle == null) return string.Empty;
            var originalHash159 = AuthoritativeBattleHashBeforeTown159(battle);
            var townHash159=battle.TownBonuses159 == null ? originalHash159 : CanonicalJson.Sha256Hex(new
            {
                OriginalBattleHash = originalHash159,
                battle.TownBonuses159
            });
            return ExtendProgressionHash163(battle,ExtendTitanHash161(battle,townHash159));
        }

        private static string AuthoritativeBattleHashBeforeTown159(BattleState battle)
        {
            // Preserve the exact certified pre-V4 recipe for every battle that has
            // never created a battle-local SSS actor. Only the additive runtime
            // snapshot extends authority when it actually exists.
            if (battle.SssBattleRuntime090 != null)
                return CanonicalJson.Sha256Hex(new
                {
                    battle.BattleId, battle.ContentVersion, battle.Round, battle.Phase, battle.Outcome, battle.Objective,
                    battle.PlayerUnions, battle.EnemyUnions, battle.CommittedForecasts, battle.Selections,
                    battle.EventLog, battle.RoundRecords, battle.ForecastStateBasisHash,
                    battle.TutorialBreakthroughMemberId, battle.TutorialBreakthroughArtId,
                    battle.TutorialBreakthroughOccurred,
                    battle.SssBattleRuntime090,
                    Reward = AuthoritativeRewardHashState(battle.Reward)
                });
            return CanonicalJson.Sha256Hex(new
            {
                battle.BattleId, battle.ContentVersion, battle.Round, battle.Phase, battle.Outcome, battle.Objective,
                battle.PlayerUnions, battle.EnemyUnions, battle.CommittedForecasts, battle.Selections,
                battle.EventLog, battle.RoundRecords, battle.ForecastStateBasisHash,
                battle.TutorialBreakthroughMemberId, battle.TutorialBreakthroughArtId,
                battle.TutorialBreakthroughOccurred,
                Reward = AuthoritativeRewardHashState(battle.Reward)
            });
        }

        /// <summary>
        /// Deterministic battle decisions must be invariant under presentation-only
        /// Enemy Art 700 identity changes.  The full authority hash intentionally
        /// continues to commit those persisted fields for tamper evidence, while
        /// this projection restores them to the deterministic identity committed
        /// for the enemy before it is used as an RNG seed.  An untampered battle
        /// therefore retains its established authority hash byte-for-byte.
        /// </summary>
        public static string GameplayRngStateHash090(BattleState battle)
        {
            if (battle == null) return string.Empty;
            // Town bonuses affect rewards only. Deliberately omit that snapshot
            // here so upgrading facilities cannot reroll combat forecasts or hits.
            return AuthoritativeStateHash(new BattleState(
                battle.BattleId,
                battle.ContentVersion,
                battle.Round,
                battle.Phase,
                battle.Outcome,
                battle.Objective,
                GameplayRngUnions090(battle.PlayerUnions, battle.BattleId),
                GameplayRngUnions090(battle.EnemyUnions, battle.BattleId),
                battle.CommittedForecasts,
                battle.Selections,
                battle.EventLog,
                battle.RoundRecords,
                battle.ForecastStateBasisHash,
                string.Empty,
                string.Empty,
                battle.TutorialBreakthroughMemberId,
                battle.TutorialBreakthroughArtId,
                battle.TutorialBreakthroughOccurred,
                battle.Reward,
                sssBattleRuntime090: battle.SssBattleRuntime090,
                titanRuntime161:battle.TitanRuntime161,titanHeroes161:battle.TitanHeroes161,
                progression163:battle.Progression163?.GameplaySnapshot()));
        }

        /// <summary>
        /// Accepts the legacy terminal shape (FinalStateHash is the full authority
        /// hash) and the Version 90 shape (FinalStateHash is gameplay-stable while
        /// FinalIntegrityStateHash090 commits the complete persisted battle art).
        /// </summary>
        public static bool HasValidFinalStateHash090(BattleState battle)
        {
            if (battle == null || string.IsNullOrWhiteSpace(battle.FinalStateHash))
                return false;
            var integrityHash090 = AuthoritativeStateHash(battle);
            if (StringComparer.Ordinal.Equals(
                    battle.FinalStateHash,
                    integrityHash090))
                return string.IsNullOrWhiteSpace(battle.FinalIntegrityStateHash090) ||
                       StringComparer.Ordinal.Equals(
                           battle.FinalIntegrityStateHash090,
                           integrityHash090);
            if (!StringComparer.Ordinal.Equals(
                    battle.FinalStateHash,
                    GameplayRngStateHash090(battle)))
                return false;
            return !string.IsNullOrWhiteSpace(battle.FinalIntegrityStateHash090) &&
                   StringComparer.Ordinal.Equals(
                       battle.FinalIntegrityStateHash090,
                       integrityHash090);
        }

        private static object AuthoritativeRewardHashState(BattleRewardState reward)
        {
            if (reward == null) return null;
            if (reward.EquipmentReward == null)
                return new
                {
                    reward.RewardId,
                    reward.RewardRulesVersion,
                    reward.Outcome,
                    reward.BasePersonalXpPerMember,
                    reward.BaseGuildTreasuryXp,
                    reward.EnemyUnionMultiplierPermille,
                    reward.OutcomeMultiplierPermille,
                    reward.PersonalXpModePercent,
                    reward.TreasuryXpModePercent,
                    reward.GuildTreasuryXpAward,
                    reward.HallEnhancementXpAward,
                    reward.MemberRewards
                };
            return new
            {
                reward.RewardId,
                reward.RewardRulesVersion,
                reward.Outcome,
                reward.BasePersonalXpPerMember,
                reward.BaseGuildTreasuryXp,
                reward.EnemyUnionMultiplierPermille,
                reward.OutcomeMultiplierPermille,
                reward.PersonalXpModePercent,
                reward.TreasuryXpModePercent,
                reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward,
                reward.MemberRewards,
                reward.EquipmentReward
            };
        }

        private static IReadOnlyList<BattleUnionState> CreatePlayerUnions(
            CampaignState campaign,
            M2CombatContent content,
            IReadOnlyList<string> alliedUnionIds,
            int maximumUnionCount)
            => CreatePlayerUnionsForRules101(campaign, content, alliedUnionIds, maximumUnionCount, true);

        private static IReadOnlyList<BattleUnionState> CreatePlayerUnionsForRules101(
            CampaignState campaign,
            M2CombatContent content,
            IReadOnlyList<string> alliedUnionIds,
            int maximumUnionCount,
            bool basicStrike101)
        {
            var result = new List<BattleUnionState>();
            var plannedUnions132 = SecondDimension.Gameplay.M1.UnionBattlePlanRules132.Read(campaign);
            var allowed = alliedUnionIds != null && alliedUnionIds.Count > 0
                ? new HashSet<string>(alliedUnionIds, StringComparer.Ordinal)
                : null;
            maximumUnionCount = Math.Max(1, Math.Min(10, maximumUnionCount));
            for (var unionIndex = 0;
                 unionIndex < plannedUnions132.Count && result.Count < maximumUnionCount;
                 unionIndex++)
            {
                var source = plannedUnions132[unionIndex];
                if (source.Kind != UnionKind.Normal || source.MemberRecruitIds.Count == 0) continue;
                if (allowed != null && !allowed.Contains(source.UnionId)) continue;
                var formation = content.Formation(source.FormationId);
                var members = new List<BattleMemberState>();
                for (var memberIndex = 0; memberIndex < source.MemberRecruitIds.Count; memberIndex++)
                {
                    if (!IsGuildCityMemberDeployable(campaign.Guild.GuildCity,
                            source.MemberRecruitIds[memberIndex])) continue;
                    var recruit = FindRecruit(campaign.Guild.Recruits, source.MemberRecruitIds[memberIndex]);
                    var classId = StartingClassId(recruit.ClassTendencyId);
                    var tags = EquipmentTags(recruit);
                    var progression = recruit.Progression;
                    var learned = new List<string>(
                        M2DeepArtRuntime070.BattleLearnedArts(recruit, content));
                    AddUnique(learned, "ART_GUARD");
                    AddUnique(learned, "ART_RECOVER_BREATH");
                    var basic = BasicArtId(tags, basicStrike101);
                    AddUnique(learned, basic);
                    var classArt = ClassArtId(classId);
                    if (content.Arts.ContainsKey(classArt) && IsEquipmentLegal(content.Art(classArt), tags))
                        AddUnique(learned, classArt);
                    learned.Sort(StringComparer.Ordinal);
                    var artProgress = new List<BattleArtProgressState>();
                    var meaningfulUsePoints = 0;
                    for (var progressIndex = 0; progressIndex < progression.ArtMastery.Count; progressIndex++)
                    {
                        var persistent = progression.ArtMastery[progressIndex];
                        artProgress.Add(new BattleArtProgressState(
                            persistent.ArtId,
                            persistent.Discipline,
                            persistent.MeaningfulUses,
                            persistent.MasteryPoints));
                        meaningfulUsePoints = checked(meaningfulUsePoints + persistent.MasteryPoints);
                    }
                    M2DeepArtRuntime070.MergeActiveLearnedArts(
                        recruit,
                        content,
                        learned,
                        artProgress);
                    learned.Sort(StringComparer.Ordinal);
                    artProgress.Sort((left, right) => StringComparer.Ordinal.Compare(left.ArtId, right.ArtId));
                    meaningfulUsePoints = artProgress.Sum(value => value.MasteryPoints);
                    var maximumHp = checked(recruit.MaximumHp + progression.MaximumHpBonus);
                    var maximumMp = checked(recruit.MaximumMp + progression.MaximumMpBonus);
                    var currentHp = Math.Min(maximumHp, checked(recruit.CurrentHp + progression.MaximumHpBonus));
                    var currentMp = Math.Min(maximumMp, checked(recruit.CurrentMp + progression.MaximumMpBonus));
                    var mainHand = recruit.Equipment.Find(EquipmentSlotIds.MainHand);
                    var equipmentPower087 = M2EquipmentPowerPolicy087.Resolve(recruit.Equipment);
                    members.Add(new BattleMemberState(
                        recruit.RecruitId, recruit.DisplayName, classId,
                        currentHp, maximumHp, currentMp, maximumMp,
                        checked(18 + recruit.PotentialBasisPoints / 500 + recruit.TacticalAptitude / 8 +
                            progression.StrengthBonus + progression.AgilityBonus / 2 +
                            equipmentPower087.PhysicalAttack),
                        checked(12 + recruit.MaximumMp / 4 + recruit.PotentialBasisPoints / 750 +
                            progression.MagicBonus + progression.WillBonus / 2 +
                            equipmentPower087.MysticAttack),
                        tags, false, false, false, learned.AsReadOnly(), meaningfulUsePoints, 0, string.Empty,
                        artProgress.AsReadOnly(), mainHand?.Item.InstanceId));
                }
                if (members.Count == 0) continue;
                var maximumAp = Math.Max(14, source.SharedAp);
                var resolvedLeaderId = members.Exists(value => StringComparer.Ordinal.Equals(value.MemberId, source.LeaderRecruitId))
                    ? source.LeaderRecruitId
                    : members[0].MemberId;
                result.Add(new BattleUnionState(
                    source.UnionId, source.DisplayName, BattleSide.Player, resolvedLeaderId, members.AsReadOnly(),
                    source.FormationId, formation.Name, formation.Supports(members.Count),
                    formation.Supports(members.Count) ? string.Empty :
                        formation.Name + " remains selected; its combat benefit needs at least 3 members.",
                    maximumAp, maximumAp, Math.Max(1, source.CohesionBasisPoints / 100), 10000,
                    EngagementState.Open, false, false, 0));
            }
            return result.AsReadOnly();
        }

        private static bool IsGuildCityMemberDeployable(GuildCityState017D city, string recruitId)
            => GuildMemberDeploymentPolicy017D.IsRecruitDeployable(city, recruitId);

        private static IReadOnlyList<BattleUnionState> CreateEnemyUnions(
            M2CombatContent content,
            int count,
            bool trainingProjection)
        {
            var result = new List<BattleUnionState>();
            for (var index = 0; index < count; index++)
                result.Add(CreateEnemyUnion(content, index, count, trainingProjection));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<BattleUnionState> CreateEnemyUnions070(
            M2CombatContent content,
            EncounterRoster070 roster)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (roster.Unions.Count < 1 || roster.Unions.Count > 10)
                throw new InvalidOperationException("Version 70 encounter roster must contain one to ten Unions.");

            var result = new List<BattleUnionState>();
            var hpScale = Math.Max(360, 620 - (roster.Unions.Count - 1) * 70);
            for (var unionIndex = 0; unionIndex < roster.Unions.Count; unionIndex++)
            {
                var resolved = roster.Unions[unionIndex];
                var source = resolved.Definition;
                var members = new List<BattleMemberState>();
                for (var memberIndex = 0; memberIndex < resolved.Members.Count; memberIndex++)
                {
                    var enemy = resolved.Members[memberIndex];
                    var definition = enemy.Definition;
                    var hp = Math.Max(55, definition.MaximumHp * hpScale / 1000);
                    // Preserve the established rank-based combat tuning without
                    // reading the presentation-only VisualVariantSeed.  Current
                    // authored enemies encode that combat rank in their stable ID.
                    var combatRankAttack090 = EnemyCombatRankVariation090(
                        enemy.SourceEnemyId, 5);
                    var combatRankMagic090 = EnemyCombatRankVariation090(
                        enemy.SourceEnemyId, 3);
                    var attack = Math.Max(14,
                        13 + definition.ApContribution + combatRankAttack090);
                    var magicAttack = Math.Max(8,
                        8 + definition.MaximumMp / 24 + combatRankMagic090);
                    members.Add(new BattleMemberState(
                        enemy.MemberId,
                        definition.Name,
                        enemy.SourceEnemyId,
                        hp,
                        hp,
                        definition.MaximumMp,
                        definition.MaximumMp,
                        attack,
                        magicAttack,
                        EnemyEquipmentTags086(
                            definition, enemy.FamilyId, content),
                        false,
                        false,
                        false,
                        definition.ArtIds,
                        0,
                        0,
                        string.Empty,
                        artProgress: null,
                        equippedMainHandInstanceId: null,
                        enemyArtBaseId090: null,
                        enemyArtVariantId090: null,
                        visualVariantSeed090: enemy.VisualVariantSeed));
                }

                var formation = content.Formation(source.FormationId);
                var eligible = formation.Supports(members.Count);
                result.Add(new BattleUnionState(
                    resolved.UnionId,
                    source.Name,
                    BattleSide.Enemy,
                    resolved.LeaderMemberId,
                    members.AsReadOnly(),
                    source.FormationId,
                    formation.Name,
                    eligible,
                    eligible ? string.Empty : "Enemy formation member count is not eligible.",
                    Math.Max(1, source.SharedApBase),
                    Math.Max(1, source.SharedApBase),
                    Math.Max(0, Math.Min(100, source.CohesionBase)),
                    10000,
                    EngagementState.Open,
                    false,
                    false,
                    0));
            }
            return result.AsReadOnly();
        }

        private static int EnemyCombatRankVariation090(
            string sourceEnemyId,
            int modulus)
        {
            if (string.IsNullOrWhiteSpace(sourceEnemyId) || modulus < 1)
                return 0;
            var separator = sourceEnemyId.LastIndexOf('_');
            if (separator < 0 || separator == sourceEnemyId.Length - 1)
                return 0;
            int ordinal;
            return int.TryParse(sourceEnemyId.Substring(separator + 1), out ordinal) &&
                   ordinal >= 0
                ? ordinal % modulus
                : 0;
        }

        /// <summary>
        /// The first-hour climax is identified by committed encounter identity and a
        /// real Hinge-Eater roster member. Its pressure profile is battle authority:
        /// presentation reads these values but cannot manufacture boss power.
        /// </summary>
        private static IReadOnlyList<BattleUnionState> ApplyGateEaterClimaxAuthority076(
            IReadOnlyList<BattleUnionState> source,
            string battleId)
        {
            if (!IsGateEaterEncounter076(battleId) || !ContainsGateEaterBoss076(source))
                return source;

            var result = new List<BattleUnionState>(source.Count);
            for (var unionIndex = 0; unionIndex < source.Count; unionIndex++)
            {
                var union = source[unionIndex];
                var members = new List<BattleMemberState>(union.Members.Count);
                var bossUnion = false;
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    if (!IsGateEaterBoss076(member))
                    {
                        members.Add(member);
                        continue;
                    }

                    bossUnion = true;
                    var maximumHp = Math.Max(
                        GateEaterBossMinimumMaximumHp076,
                        checked(member.MaximumHp * 2));
                    members.Add(new BattleMemberState(
                        member.MemberId,
                        member.DisplayName,
                        member.ClassId,
                        maximumHp,
                        maximumHp,
                        member.CurrentMp,
                        member.MaximumMp,
                        Math.Max(GateEaterBossMinimumAttack076, member.Attack),
                        member.MagicAttack,
                        member.EquipmentTags,
                        false,
                        member.Stabilized,
                        member.Guarding,
                        member.LearnedArtIds,
                        member.MeaningfulUsePoints,
                        member.DiscoveryProgress,
                        member.BreakthroughArtId,
                        member.ArtProgress,
                        member.EquippedMainHandInstanceId,
                        member.EnemyArtBaseId090,
                        member.EnemyArtVariantId090,
                        member.VisualVariantSeed090));
                }

                if (!bossUnion)
                {
                    result.Add(union);
                    continue;
                }

                var maximumAp = Math.Max(GateEaterBossMinimumUnionAp076, union.MaximumAp);
                result.Add(new BattleUnionState(
                    union.UnionId,
                    union.DisplayName,
                    union.Side,
                    union.LeaderMemberId,
                    members.AsReadOnly(),
                    union.FormationId,
                    union.FormationName,
                    union.FormationMemberCountEligible,
                    union.FormationInactiveReason,
                    maximumAp,
                    maximumAp,
                    Math.Max(90, union.Cohesion),
                    union.FormationConditionBasisPoints,
                    union.Engagement,
                    union.Guarding,
                    union.Retreated,
                    union.UnionMeaningfulUsePoints));
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// The vertical slice's mandatory encounters use authored endurance instead
        /// of slowing every battle or hiding damage behind invulnerability. Scaling
        /// the committed enemy HP keeps every displayed impact truthful while giving
        /// the player time to read retaliation, AP pressure, and a follow-up order.
        /// Enemy attack is intentionally unchanged so the longer fights remain safe
        /// for the founding roster.
        /// </summary>
        private static IReadOnlyList<BattleUnionState> ApplyFirstSliceEncounterPacing078(
            IReadOnlyList<BattleUnionState> source,
            string battleId)
        {
            var hpPercent = FirstSliceEnemyHpPercent078(battleId);
            if (source == null || hpPercent == 100) return source;

            var result = new List<BattleUnionState>(source.Count);
            for (var unionIndex = 0; unionIndex < source.Count; unionIndex++)
            {
                var union = source[unionIndex];
                var members = new List<BattleMemberState>(union.Members.Count);
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    var maximumHp = checked((int)Math.Max(
                        1L,
                        (long)member.MaximumHp * hpPercent / 100L));
                    var currentHp = member.CurrentHp <= 0
                        ? 0
                        : checked((int)Math.Max(
                            1L,
                            (long)member.CurrentHp * hpPercent / 100L));
                    members.Add(new BattleMemberState(
                        member.MemberId,
                        member.DisplayName,
                        member.ClassId,
                        currentHp,
                        maximumHp,
                        member.CurrentMp,
                        member.MaximumMp,
                        member.Attack,
                        member.MagicAttack,
                        member.EquipmentTags,
                        member.Downed,
                        member.Stabilized,
                        member.Guarding,
                        member.LearnedArtIds,
                        member.MeaningfulUsePoints,
                        member.DiscoveryProgress,
                        member.BreakthroughArtId,
                        member.ArtProgress,
                        member.EquippedMainHandInstanceId,
                        member.EnemyArtBaseId090,
                        member.EnemyArtVariantId090,
                        member.VisualVariantSeed090));
                }
                result.Add(union.With(members: members.AsReadOnly()));
            }
            return result.AsReadOnly();
        }

        private static int FirstSliceEnemyHpPercent078(string battleId)
        {
            if (IsBattleToken076(battleId, HallBreachBattleToken076))
                return HallBreachEnemyHpPercent078;
            if (IsBattleToken076(battleId, LanternRoadBattleToken076))
                return LanternRoadEnemyHpPercent078;
            if (IsGateEaterEncounter076(battleId))
                return GateEaterEnemyHpPercent078;
            if (IsBattleToken076(battleId, FogStalkersBattleToken078))
                return FogStalkersEnemyHpPercent078;
            if (IsBattleToken076(battleId, SurveyorRescueBattleToken079))
                return SurveyorRescueEnemyHpPercent079;
            return 100;
        }

        public static bool IsGateEaterBattle076(BattleState battle) =>
            battle != null &&
            IsGateEaterEncounter076(battle.BattleId) &&
            ContainsGateEaterBoss076(battle.EnemyUnions);

        private static bool IsGateEaterEncounter076(string battleId) =>
            !string.IsNullOrWhiteSpace(battleId) &&
            battleId.IndexOf(GateEaterBattleToken076, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool ContainsGateEaterBoss076(IReadOnlyList<BattleUnionState> unions)
        {
            if (unions == null) return false;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var members = unions[unionIndex]?.Members;
                if (members == null) continue;
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                    if (IsGateEaterBoss076(members[memberIndex])) return true;
            }
            return false;
        }

        private static bool IsGateEaterBoss076(BattleMemberState member) =>
            member != null &&
            ((!string.IsNullOrWhiteSpace(member.ClassId) &&
              member.ClassId.IndexOf(GateEaterBossClassToken076, StringComparison.OrdinalIgnoreCase) >= 0) ||
             (!string.IsNullOrWhiteSpace(member.MemberId) &&
              member.MemberId.IndexOf(GateEaterBossClassToken076, StringComparison.OrdinalIgnoreCase) >= 0));

        /// <summary>
        /// The first-hour escorts screen their encounter's signature threat. This is
        /// a stable partition over the committed roster, so the roster itself and all
        /// deterministic member identities remain unchanged.
        /// </summary>
        private static IReadOnlyList<BattleUnionState> OrderFirstHourSignatureThreatLast076(
            IReadOnlyList<BattleUnionState> source,
            string battleId)
        {
            var road = IsBattleToken076(battleId, LanternRoadBattleToken076);
            var gate = IsGateEaterEncounter076(battleId);
            if ((!road && !gate) || source == null || source.Count < 2) return source;

            var escorts = new List<BattleUnionState>(source.Count);
            var signatures = new List<BattleUnionState>(source.Count);
            for (var unionIndex = 0; unionIndex < source.Count; unionIndex++)
            {
                var union = source[unionIndex];
                var signature = gate
                    ? UnionContainsGateEaterBoss076(union)
                    : IsGateironBruteUnion076(union);
                (signature ? signatures : escorts).Add(union);
            }
            if (signatures.Count == 0 || escorts.Count == 0) return source;
            escorts.AddRange(signatures);
            return escorts.AsReadOnly();
        }

        private static bool IsBattleToken076(string battleId, string token) =>
            !string.IsNullOrWhiteSpace(battleId) &&
            battleId.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool UnionContainsGateEaterBoss076(BattleUnionState union)
        {
            if (union?.Members == null) return false;
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                if (IsGateEaterBoss076(union.Members[memberIndex])) return true;
            return false;
        }

        private static bool IsGateironBruteUnion076(BattleUnionState union)
        {
            if (union == null) return false;
            if ((!string.IsNullOrWhiteSpace(union.UnionId) &&
                 union.UnionId.IndexOf(GateironBruteUnionToken076, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(union.DisplayName) &&
                 union.DisplayName.IndexOf("GATEIRON BRUTE", StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                if ((!string.IsNullOrWhiteSpace(member.ClassId) &&
                     member.ClassId.IndexOf(GateironBruteClassToken076, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(member.MemberId) &&
                     member.MemberId.IndexOf(GateironBruteClassToken076, StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            }
            return false;
        }

        private static BattleEventState BuildStoryThreatTelegraph076(
            string battleId,
            IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies,
            int sequence)
        {
            BattleUnionState threatUnion;
            var threat = FindStoryThreatAttacker076(
                battleId, enemies, out threatUnion);
            var target = FirstActiveUnion(players);
            var damage = StoryThreatDamage076(battleId, target);
            if (threat == null || threatUnion == null || target == null || damage <= 0) return null;
            var targetMemberIndex = FirstActiveMemberIndex(target);
            var targetMemberId = targetMemberIndex >= 0
                ? target.Members[targetMemberIndex].MemberId
                : target.LeaderMemberId;
            return Event(
                sequence,
                1,
                StoryThreatTelegraphEventType076,
                BattleSide.Enemy,
                target.UnionId,
                targetMemberId,
                string.Empty,
                threat.DisplayName + " marks " + target.DisplayName + " for about " + damage +
                " HP pressure. HOLD THE LINE cuts that hit in half.",
                damage,
                threatUnion.UnionId,
                threat.MemberId,
                target.UnionId,
                targetMemberId);
        }

        private static BattleMemberState FindStoryThreatAttacker076(
            string battleId,
            IReadOnlyList<BattleUnionState> enemies,
            out BattleUnionState threatUnion)
        {
            threatUnion = null;
            if (enemies == null) return null;

            if (IsGateEaterEncounter076(battleId))
            {
                for (var unionIndex = enemies.Count - 1; unionIndex >= 0; unionIndex--)
                {
                    var union = enemies[unionIndex];
                    if (!IsActive(union)) continue;
                    for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                    {
                        var member = union.Members[memberIndex];
                        if (member.Downed || !IsGateEaterBoss076(member)) continue;
                        threatUnion = union;
                        return member;
                    }
                }
                return null;
            }

            if (IsBattleToken076(battleId, LanternRoadBattleToken076))
            {
                for (var unionIndex = enemies.Count - 1; unionIndex >= 0; unionIndex--)
                {
                    var union = enemies[unionIndex];
                    if (!IsActive(union) || !IsGateironBruteUnion076(union)) continue;
                    threatUnion = union;
                    return FirstActiveThreatMember076(union);
                }
                return null;
            }

            if (!IsBattleToken076(battleId, HallBreachBattleToken076)) return null;
            for (var unionIndex = enemies.Count - 1; unionIndex >= 0; unionIndex--)
            {
                if (!IsActive(enemies[unionIndex])) continue;
                threatUnion = enemies[unionIndex];
                return FirstActiveThreatMember076(threatUnion);
            }
            return null;
        }

        private static BattleMemberState FirstActiveThreatMember076(BattleUnionState union)
        {
            if (union == null) return null;
            var leaderIndex = union.FindMemberIndex(union.LeaderMemberId);
            if (leaderIndex >= 0 && !union.Members[leaderIndex].Downed)
                return union.Members[leaderIndex];
            var activeIndex = FirstActiveMemberIndex(union);
            return activeIndex >= 0 ? union.Members[activeIndex] : null;
        }

        private static int StoryThreatPercent076(string battleId)
        {
            if (IsBattleToken076(battleId, HallBreachBattleToken076))
                return HallBreachThreatPercent076;
            if (IsBattleToken076(battleId, LanternRoadBattleToken076))
                return LanternRoadThreatPercent076;
            if (IsGateEaterEncounter076(battleId))
                return GateEaterThreatPercent076;
            return 0;
        }

        private static int StoryThreatDamage076(string battleId, BattleUnionState target)
        {
            var percent = StoryThreatPercent076(battleId);
            if (percent <= 0 || target == null || !IsActive(target)) return 0;
            var maximumHp = 0;
            for (var memberIndex = 0; memberIndex < target.Members.Count; memberIndex++)
                maximumHp = checked(maximumHp + target.Members[memberIndex].MaximumHp);
            return Math.Max(1, checked(maximumHp * percent) / 100);
        }

        private static BattleUnionState CreateEnemyUnion(
            M2CombatContent content,
            int packIndex,
            int packCount,
            bool trainingProjection)
        {
            var source = content.TutorialEnemyUnion;
            var members = new List<BattleMemberState>();
            var suffix = packIndex == 0 ? string.Empty : "_PACK_" + (packIndex + 1);
            var hpScale = trainingProjection ? 650 : Math.Max(360, 620 - (packCount - 1) * 70);
            var leaderId = string.Empty;
            for (var i = 0; i < source.MemberIds.Count; i++)
            {
                var enemy = content.Enemy(source.MemberIds[i]);
                var memberId = enemy.Id + suffix;
                if (StringComparer.Ordinal.Equals(source.LeaderId, enemy.Id)) leaderId = memberId;
                var hp = Math.Max(55, enemy.MaximumHp * hpScale / 1000);
                members.Add(new BattleMemberState(
                    memberId,
                    enemy.Name + (packCount > 1 ? " · Pack " + (packIndex + 1) : string.Empty),
                    "ENEMY_FORMATION_NUISANCE", hp, hp, enemy.MaximumMp, enemy.MaximumMp,
                    18 + i * 2, 8,
                    EnemyEquipmentTags086(enemy, string.Empty, content),
                    false, false, false,
                    enemy.ArtIds, 0, 0, string.Empty));
            }
            if (string.IsNullOrWhiteSpace(leaderId) && members.Count > 0) leaderId = members[0].MemberId;
            var formation = content.Formation(source.FormationId);
            var displayName = source.Name + (trainingProjection
                ? " · Training Projection"
                : packCount > 1 ? " · Pack " + (packIndex + 1) : string.Empty);
            return new BattleUnionState(
                source.Id + suffix, displayName, BattleSide.Enemy, leaderId,
                members.AsReadOnly(), source.FormationId, formation.Name, true, string.Empty,
                source.SharedApBase, source.SharedApBase, Math.Min(100, source.CohesionBase), 10000,
                EngagementState.Open, false, false, 0);
        }

        private static IReadOnlyList<string> EnemyEquipmentTags086(
            M2EnemyDefinition enemy,
            string familyId,
            M2CombatContent content)
        {
            var tags = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(familyId)) tags.Add(familyId);
            if (enemy?.ArtIds != null && content != null)
            {
                for (var artIndex = 0; artIndex < enemy.ArtIds.Count; artIndex++)
                {
                    if (!content.Arts.TryGetValue(
                            enemy.ArtIds[artIndex], out var art))
                        continue;
                    for (var tagIndex = 0;
                         tagIndex < art.RequiredEquipmentTags.Count;
                         tagIndex++)
                        tags.Add(art.RequiredEquipmentTags[tagIndex]);
                }
            }
            var result = tags.ToList();
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<BattleUnionState> ApplyRouteModifiersToPlayers(
            IReadOnlyList<BattleUnionState> source,
            IReadOnlyList<string> routeModifiers)
        {
            var result = new List<BattleUnionState>(source);
            if (ContainsRouteModifier(routeModifiers, "HIGH_FATIGUE"))
            {
                for (var index = 0; index < result.Count; index++)
                    result[index] = result[index].With(
                        currentAp: Math.Max(1, result[index].CurrentAp - 3),
                        cohesion: Math.Max(1, result[index].Cohesion - 12));
            }
            var apBonus017H = GuildCityBattleModifierRules017H.Amount(routeModifiers, "CITY_AP_BONUS_");
            var cohesionBonus017H = GuildCityBattleModifierRules017H.Amount(routeModifiers, "CITY_COHESION_BONUS_");
            var mpBonus017H = GuildCityBattleModifierRules017H.Amount(routeModifiers, "CITY_MP_BONUS_");
            var guardPrepared017H = ContainsRouteModifier(routeModifiers, "CITY_GUARD_PREPARED");
            if (apBonus017H > 0 || cohesionBonus017H > 0 || mpBonus017H > 0 || guardPrepared017H)
                for (var index = 0; index < result.Count; index++)
                    result[index] = ApplyCityPlayerBonuses017H(result[index], apBonus017H, cohesionBonus017H, mpBonus017H, guardPrepared017H);
            if (result.Count > 0 && (ContainsRouteModifier(routeModifiers, "SCOUTED_APPROACH") || ContainsRouteModifier(routeModifiers, "CITY_WAVE_SCOUTED")))
                result[0] = result[0].With(engagement: EngagementState.Flanking);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<BattleUnionState> ApplyRouteModifiersToEnemies(
            IReadOnlyList<BattleUnionState> source,
            IReadOnlyList<string> routeModifiers)
        {
            var result = new List<BattleUnionState>(EnemyForceProfile094.ApplyProgression138(source, routeModifiers));
            var replayThreat130 = CampaignReplayThreat130.ParseCommittedRoutes(routeModifiers);
            if (replayThreat130 != null)
                result = new List<BattleUnionState>(CampaignReplayThreat130.ApplyToEnemyUnions132(result, replayThreat130));
            var actualTowerFloor098 = TowerThreatRules098.ReadFloor098(routeModifiers);
            var scaledTowerFloor138 = TowerScalingRules138.ReadFloor138(routeModifiers);
            var towerThreatTier089 = GuildCityBattleModifierRules017H.Amount(
                routeModifiers, "TOWER_THREAT_TIER_");
            if (actualTowerFloor098 > 0)
                for (var index = 0; index < result.Count; index++)
                    result[index] = scaledTowerFloor138 > 0
                        ? TowerScalingRules138.Apply138(result[index], scaledTowerFloor138)
                        : TowerThreatRules098.Apply098(result[index], actualTowerFloor098);
            else if (towerThreatTier089 > 0)
                for (var index = 0; index < result.Count; index++)
                    result[index] = ApplyTowerThreatPower089(
                        result[index], towerThreatTier089);
            if (result.Count > 0 && (ContainsRouteModifier(routeModifiers, "SCOUTED_APPROACH") || ContainsRouteModifier(routeModifiers, "CITY_WAVE_SCOUTED")))
                result[0] = result[0].With(cohesion: Math.Max(1, result[0].Cohesion - 12));
            var cityCohesionDamage017H = GuildCityBattleModifierRules017H.Amount(routeModifiers, "CITY_ENEMY_COHESION_DAMAGE_");
            if (cityCohesionDamage017H > 0)
                for (var index = 0; index < result.Count; index++)
                    result[index] = result[index].With(cohesion: Math.Max(1, result[index].Cohesion - cityCohesionDamage017H));
            return result.AsReadOnly();
        }

        private static BattleUnionState ApplyTowerThreatPower089(
            BattleUnionState union,
            int requestedTier)
        {
            var tier = CampaignProgressionCommandService022
                .TowerThreatTier089(requestedTier);
            var hpBasisPoints = CampaignProgressionCommandService022
                .TowerThreatHpBasisPoints089(tier);
            var offenseBasisPoints = CampaignProgressionCommandService022
                .TowerThreatOffenseBasisPoints089(tier);
            var members = new List<BattleMemberState>(union.Members.Count);
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                var maximumHp = ScaleTowerStat089(member.MaximumHp, hpBasisPoints);
                var currentHp = member.CurrentHp <= 0
                    ? 0
                    : Math.Max(1, checked((int)Math.Min(maximumHp,
                        (long)member.CurrentHp * maximumHp /
                        member.MaximumHp)));
                members.Add(new BattleMemberState(
                    member.MemberId, member.DisplayName, member.ClassId,
                    currentHp, maximumHp, member.CurrentMp, member.MaximumMp,
                    ScaleTowerStat089(member.Attack, offenseBasisPoints),
                    ScaleTowerStat089(member.MagicAttack, offenseBasisPoints),
                    member.EquipmentTags, member.Downed, member.Stabilized,
                    member.Guarding, member.LearnedArtIds,
                    member.MeaningfulUsePoints, member.DiscoveryProgress,
                    member.BreakthroughArtId, member.ArtProgress,
                    member.EquippedMainHandInstanceId,
                    member.EnemyArtBaseId090,
                    member.EnemyArtVariantId090,
                    member.VisualVariantSeed090));
            }
            var apBonus = (tier - 1) / 3;
            var maximumAp = Math.Min(999, union.MaximumAp + apBonus);
            var currentAp = Math.Min(maximumAp, union.CurrentAp + apBonus);
            return new BattleUnionState(
                union.UnionId, union.DisplayName, union.Side,
                union.LeaderMemberId, members.AsReadOnly(), union.FormationId,
                union.FormationName, union.FormationMemberCountEligible,
                union.FormationInactiveReason, currentAp, maximumAp,
                Math.Min(100, union.Cohesion + tier - 1),
                union.FormationConditionBasisPoints, union.Engagement,
                union.Guarding, union.Retreated,
                union.UnionMeaningfulUsePoints);
        }

        private static int ScaleTowerStat089(int value, int basisPoints)
        {
            var scaled = checked((long)value * (10000L + basisPoints));
            return checked((int)Math.Min(int.MaxValue,
                Math.Max(1L, (scaled + 9999L) / 10000L)));
        }

        private static BattleUnionState ApplyCityPlayerBonuses017H(BattleUnionState union,int apBonus,int cohesionBonus,int mpBonus,bool guardPrepared)
        {
            var members=new List<BattleMemberState>();
            for(var i=0;i<union.Members.Count;i++)
            {
                var m=union.Members[i];var maxMp=Math.Min(9999,m.MaximumMp+mpBonus);var currentMp=Math.Min(maxMp,m.CurrentMp+mpBonus);
                members.Add(new BattleMemberState(m.MemberId,m.DisplayName,m.ClassId,m.CurrentHp,m.MaximumHp,currentMp,maxMp,m.Attack,m.MagicAttack,m.EquipmentTags,m.Downed,m.Stabilized,guardPrepared||m.Guarding,m.LearnedArtIds,m.MeaningfulUsePoints,m.DiscoveryProgress,m.BreakthroughArtId,m.ArtProgress,m.EquippedMainHandInstanceId));
            }
            var maximumAp=Math.Min(999,union.MaximumAp+apBonus);var currentAp=Math.Min(maximumAp,union.CurrentAp+apBonus);
            return new BattleUnionState(union.UnionId,union.DisplayName,union.Side,union.LeaderMemberId,members.AsReadOnly(),union.FormationId,union.FormationName,union.FormationMemberCountEligible,union.FormationInactiveReason,currentAp,maximumAp,Math.Min(100,union.Cohesion+cohesionBonus),union.FormationConditionBasisPoints,guardPrepared?EngagementState.Guarded:union.Engagement,guardPrepared||union.Guarding,union.Retreated,union.UnionMeaningfulUsePoints);
        }

        private static string BattleResultSummary(BattleState battle, BattleOutcome outcome)
        {
            var tutorial = StringComparer.Ordinal.Equals(battle.BattleId, TutorialBattleId);
            if (tutorial)
            {
                if (outcome == BattleOutcome.Victory)
                    return "Training projection defeated. Forecast proof complete.";
                if (outcome == BattleOutcome.Retreat)
                    return "Your Unions withdrew intact. Forecast proof complete.";
                return "Your Unions were Downed. The tutorial remains safely replayable.";
            }

            if (outcome == BattleOutcome.Victory)
                return "The encounter is cleared. Expedition command can return to the committed board checkpoint.";
            if (outcome == BattleOutcome.Retreat)
                return "Your Unions disengaged. Expedition command will apply the authorized retreat consequences.";
            return "Your deployed Unions were Downed. Expedition command will apply injury and recovery consequences.";
        }

        private static IReadOnlyList<BattleEventState> BuildOpeningEvents(
            IReadOnlyList<string> routeModifiers,
            int enemyUnionCount)
        {
            var events = new List<BattleEventState>
            {
                Event(0, 1, "BATTLE_START", BattleSide.Player, string.Empty, string.Empty, string.Empty,
                    "Command phase engaged. Choose one complete command for each active Union. " +
                    enemyUnionCount + " enemy Union" + (enemyUnionCount == 1 ? string.Empty : "s") + " detected.", 0)
            };
            if (ContainsRouteModifier(routeModifiers, "SCOUTED_APPROACH"))
                events.Add(Event(events.Count, 1, "ROUTE_ADVANTAGE", BattleSide.Player,
                    string.Empty, string.Empty, string.Empty,
                    "Scouting revealed an approach route: the lead Union begins with flank pressure.", 0));
            if (ContainsRouteModifier(routeModifiers, "HIGH_FATIGUE"))
                events.Add(Event(events.Count, 1, "ROUTE_COST", BattleSide.Player,
                    string.Empty, string.Empty, string.Empty,
                    "The expedition arrives fatigued: starting AP and Cohesion are reduced.", 0));
            if (ContainsRouteModifier(routeModifiers, "URGENT_OBJECTIVE"))
                events.Add(Event(events.Count, 1, "OBJECTIVE_URGENCY", BattleSide.Player,
                    string.Empty, string.Empty, string.Empty,
                    "The objective is urgent. Prolonged combat may worsen the expedition outcome.", 0));
            var actualTowerFloor098 = TowerThreatRules098.ReadFloor098(routeModifiers);
            if (actualTowerFloor098 > 0)
                events.Add(Event(events.Count, 1, "TOWER_ACTUAL_FLOOR_098", BattleSide.Enemy,
                    string.Empty, string.Empty, string.Empty,
                    "Tower floor " + actualTowerFloor098 +
                    " strength is committed: vitality and offense scale with the actual climb, " +
                    "independently of the repeating room and enemy artwork. Numerical safety ceilings apply.",
                    actualTowerFloor098));
            var towerThreatTier089 = GuildCityBattleModifierRules017H.Amount(
                routeModifiers, "TOWER_THREAT_TIER_");
            if (towerThreatTier089 > 0)
            {
                var tier = CampaignProgressionCommandService022
                    .TowerThreatTier089(towerThreatTier089);
                events.Add(Event(events.Count, 1, "TOWER_THREAT_TIER_089",
                    BattleSide.Enemy, string.Empty, string.Empty, string.Empty,
                    "Tower Threat " + tier + "/" +
                    CampaignProgressionCommandService022.MaximumTowerThreatTier089 +
                    " is active: enemy vitality, offense, AP, and Cohesion use the " +
                    "same certified Union combat authority.", tier));
            }
            var defensePower017H = GuildCityBattleModifierRules017H.Amount(routeModifiers, "CITY_DEFENSE_POWER_");
            if (defensePower017H > 0)
                events.Add(Event(events.Count, 1, "CITY_DEFENSE_SUPPORT", BattleSide.Player, string.Empty, string.Empty, string.Empty,
                    "Skyhome's facilities contribute " + defensePower017H + " defense power without selecting individual Arts.", defensePower017H));
            if (ContainsRouteModifier(routeModifiers, "CITY_REINFORCEMENT_READY"))
                events.Add(Event(events.Count, 1, "CITY_REINFORCEMENT_READY", BattleSide.Player, string.Empty, string.Empty, string.Empty,
                    "Signal and reserve facilities keep reinforcement routes ready.", 0));
            return events.AsReadOnly();
        }

        private static bool ContainsRouteModifier(IReadOnlyList<string> values, string value)
        {
            if (values == null) return false;
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index], value)) return true;
            return false;
        }

        private static IReadOnlyList<BattleUnionState> PrepareTutorialBreakthrough(
            IReadOnlyList<BattleUnionState> unions, string memberId, string artId, string targetDiscipline)
        {
            var result = new List<BattleUnionState>(unions);
            for (var unionIndex = 0; unionIndex < result.Count; unionIndex++)
            {
                var members = new List<BattleMemberState>(result[unionIndex].Members);
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                {
                    if (!StringComparer.Ordinal.Equals(members[memberIndex].MemberId, memberId)) continue;
                    // The selector normally supplies an unlearned target. Keep the
                    // complete learned set intact even if malformed legacy content
                    // supplies an already learned target; learning is additive only.
                    var learned = new List<string>(members[memberIndex].LearnedArtIds);
                    var currentHp = members[memberIndex].CurrentHp;
                    if (StringComparer.Ordinal.Equals(targetDiscipline, "Restoration"))
                        currentHp = Math.Max(1, currentHp - Math.Min(24, Math.Max(1, members[memberIndex].MaximumHp / 5)));
                    members[memberIndex] = members[memberIndex].With(
                        currentHp: currentHp,
                        learnedArtIds: learned.AsReadOnly(),
                        discoveryProgress: 95,
                        breakthroughArtId: artId);
                }
                result[unionIndex] = result[unionIndex].With(members: members.AsReadOnly());
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<BattleUnionState> PrepareRestorationDemonstration(
            IReadOnlyList<BattleUnionState> unions,
            M2CombatContent content)
        {
            var hasRestoration = false;
            for (var unionIndex = 0; unionIndex < unions.Count && !hasRestoration; unionIndex++)
                hasRestoration = HasAffordableArtInDisciplines(
                    unions[unionIndex], content, unions[unionIndex].CurrentAp, "Restoration");
            if (!hasRestoration || MostWoundedUnion(unions) != null) return unions;

            var result = new List<BattleUnionState>(unions);
            for (var unionIndex = 0; unionIndex < result.Count; unionIndex++)
            {
                var members = new List<BattleMemberState>(result[unionIndex].Members);
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                {
                    if (members[memberIndex].Downed) continue;
                    var bruise = Math.Max(8, members[memberIndex].MaximumHp / 8);
                    members[memberIndex] = members[memberIndex].With(
                        currentHp: Math.Max(1, members[memberIndex].CurrentHp - bruise));
                    result[unionIndex] = result[unionIndex].With(members: members.AsReadOnly());
                    return result.AsReadOnly();
                }
            }
            return result.AsReadOnly();
        }

        private static BattleState CommitForecasts(
            CampaignState campaign,
            BattleState battle,
            M2CombatContent content)
        {
            battle=CommitTitanForecast161(campaign,battle,content);
            var campaignSeed = campaign.CampaignSeed;
            var rules = campaign.Rules;
            var basisHash = ForecastBasisHash(battle);
            var forecasts = new List<BattleForecastState>();
            for (var unionIndex = 0; unionIndex < battle.PlayerUnions.Count; unionIndex++)
            {
                var union = battle.PlayerUnions[unionIndex];
                if (!IsActive(union)) continue;
                if (SssBattleIntegration090.TryBuildSyntheticForecast090(
                        campaign, battle, union, content, 0, basisHash,
                        out var syntheticForecast090))
                {
                    forecasts.Add(syntheticForecast090);
                    continue;
                }
                var commandIds = ForecastCommandIds(union, battle, content);
                BattleForecastState companionForecast090 = null;
                for (var slot = 0; slot < commandIds.Count; slot++)
                {
                    var ordinary = BuildForecast(
                        campaign, campaignSeed, battle, union, slot,
                        commandIds[slot], basisHash, content, rules);
                    forecasts.Add(ordinary);
                    if (StringComparer.Ordinal.Equals(
                            commandIds[slot], "CMD_BALANCED"))
                        companionForecast090 = ordinary;
                }
                forecasts.AddRange(SssBattleIntegration090.BuildGoldForecasts090(
                    campaign,
                    battle,
                    union,
                    commandIds.Count,
                    basisHash,
                    companionForecast090,
                    casterMemberId090 => BuildGoldCompanionForecast090(
                        campaign,
                        campaignSeed,
                        battle,
                        union,
                        casterMemberId090,
                        commandIds.Count + 100,
                        basisHash,
                        content,
                        rules)));
                forecasts.AddRange(BuildTitanHeroForecasts161(campaign,battle,content,union));
            }
            return battle.With(
                committedForecasts: forecasts.AsReadOnly(), selections: Array.Empty<BattleForecastSelectionState>(),
                forecastStateBasisHash: basisHash);
        }

        private static BattleForecastState BuildGoldCompanionForecast090(
            CampaignState campaign,
            long campaignSeed,
            BattleState battle,
            BattleUnionState source,
            string casterMemberId,
            int slot,
            string basisHash,
            M2CombatContent content,
            ModeRuleSnapshot rules)
        {
            var members = source.Members.Select(member =>
                    StringComparer.Ordinal.Equals(
                        member.MemberId, casterMemberId)
                        ? member.With(currentHp: 0)
                        : member)
                .ToArray();
            var casterIndex = source.FindMemberIndex(casterMemberId);
            var planningUnion = source.With(
                members: members,
                currentAp: Math.Max(
                    0,
                    source.CurrentAp -
                    SssBattleIntegration090.GoldSharedApCost090));
            return BuildForecast(
                campaign,
                campaignSeed,
                battle,
                planningUnion,
                slot + Math.Max(0, casterIndex),
                "CMD_BALANCED",
                basisHash,
                content,
                rules);
        }

        private static IReadOnlyList<string> ForecastCommandIds(BattleUnionState union, BattleState battle, M2CombatContent content)
        {
            var result = new List<string> { "CMD_BALANCED", "CMD_ALL_OUT", "CMD_GUARD" };
            var restorationTarget086 = MostWoundedUnion(battle.PlayerUnions);
            var hasRestoration = restorationTarget086 != null &&
                HasContextualRestorationArt086(
                    union, restorationTarget086, content, union.CurrentAp);
            var hasMystic = HasAffordableArtInDisciplines(union, content, union.CurrentAp, "Mystic");
            var supportTarget = MostSupportNeedyUnion(battle.PlayerUnions);
            var canSupportTarget = SupportCommandEligible080(supportTarget) &&
                (StringComparer.Ordinal.Equals(supportTarget.UnionId, union.UnionId) ||
                 HasAffordableFriendlyUnionArt(
                     union, content, union.CurrentAp, battle));

            // The three core choices never disappear.  Rescue and support then receive
            // first access to the remaining slots only when the battlefield actually
            // makes them urgent; flank, Mystic and recovery still compete normally.
            if (hasRestoration && IsCriticalRestorationTarget086(restorationTarget086))
                AddForecastCommand086(result, "CMD_HEAL");
            if (canSupportTarget && IsCriticalSupportTarget086(supportTarget))
                AddForecastCommand086(result, "CMD_SUPPORT");
            if (CanOpenSideStrike(union, battle)) AddForecastCommand086(result, "CMD_FLANK");
            if (hasRestoration) AddForecastCommand086(result, "CMD_HEAL");
            if (hasMystic) AddForecastCommand086(result, "CMD_MYSTIC");
            if (canSupportTarget) AddForecastCommand086(result, "CMD_SUPPORT");
            if (union.CurrentAp < union.MaximumAp) AddForecastCommand086(result, "CMD_AP_RECOVERY");
            if (battle.Round >= 2 && AverageHpPercent(union) <= 55)
                AddForecastCommand086(result, "CMD_RETREAT");
            if (EnemyGuarding(battle.EnemyUnions)) AddForecastCommand086(result, "CMD_FLANK");
            return result.AsReadOnly();
        }

        private static void AddForecastCommand086(List<string> result, string commandId)
        {
            if (result == null || result.Count >= 6 || Contains(result, commandId)) return;
            result.Add(commandId);
        }

        private static bool CanOpenSideStrike(BattleUnionState union, BattleState battle)
        {
            if (union == null || battle == null || CountActive(battle.PlayerUnions) < 2 ||
                FirstActiveUnion(battle.EnemyUnions) == null)
                return false;
            for (var index = 0; index < battle.PlayerUnions.Count; index++)
            {
                var ally = battle.PlayerUnions[index];
                if (!IsActive(ally)) continue;
                return !StringComparer.Ordinal.Equals(ally.UnionId, union.UnionId) ||
                       EnemyGuarding(battle.EnemyUnions);
            }
            return false;
        }

        private static BattleForecastState BuildForecast(
            CampaignState campaign, long campaignSeed, BattleState battle, BattleUnionState union, int slot,
            string commandId, string basisHash, M2CombatContent content, ModeRuleSnapshot rules)
        {
            var command = content.Command(commandId);
            var rng = Pcg32.FromParts(campaignSeed, battle.BattleId, battle.Round, union.UnionId, slot, basisHash, battle.ContentVersion);
            var phrase = command.Phrases.Count == 0 ? command.Name : command.Phrases[(int)rng.NextBounded((uint)command.Phrases.Count)];
            var targetUnion = TargetUnionForCommand(commandId, union, battle, content);
            var targetName = targetUnion?.DisplayName ?? "Battle objective";
            var actions = new List<BattlePlannedActionState>();
            var apRemaining = union.CurrentAp;
            var fallbacks = new List<string>();
            var deepLearningBars070 = new List<string>();
            var hasFriendlyUnionSupportAction080 = false;
            BattleUnionState ownedGuestForecastTarget090 = null;
            var projectedTargetHp086 = CreateProjectedHp086(targetUnion);
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                if (member.Downed)
                {
                    actions.Add(new BattlePlannedActionState(
                        member.MemberId, member.DisplayName, union.UnionId, member.MemberId,
                        "ART_DOWNED_RECOVERY_HOOK", "Await recovery", BattleActionKind.Recovery, 0, 0,
                        0, 0, 0, "Downed · may be stabilized by Restoration.", false, false, "DOWNED_IDLE",
                        "Recovery", 0, string.Empty, string.Empty));
                    continue;
                }
                var selectedArtId = commandId == "CMD_HEAL"
                    ? SelectRestorationArtForTarget086(
                          member, union, targetUnion, content, apRemaining,
                          projectedTargetHp086, battle) ?? "ART_GUARD"
                    : SelectArtForIntent(
                        member, union, commandId, content, rng, apRemaining,
                        battle);
                var art = content.Art(selectedArtId);
                if (commandId == "CMD_SUPPORT" && targetUnion != null &&
                    !StringComparer.Ordinal.Equals(targetUnion.UnionId, union.UnionId))
                {
                    var friendlyArtId080 = SelectFriendlyUnionSupportArt080(
                        member, targetUnion, content, apRemaining,
                        battle, union.UnionId);
                    if (!string.IsNullOrWhiteSpace(friendlyArtId080))
                    {
                        selectedArtId = friendlyArtId080;
                        art = content.Art(selectedArtId);
                    }
                }
                var ownedGuestTarget090 = IsSssOwnedGuestUnionScope090(art)
                    ? OwnedLivingGuestUnion090(
                        battle, battle.PlayerUnions, member.MemberId,
                        union.UnionId)
                    : null;
                if (IsSssOwnedGuestUnionScope090(art) &&
                    ownedGuestTarget090 == null)
                {
                    var prior = selectedArtId;
                    selectedArtId = "ART_RECOVER_BREATH";
                    art = content.Art(selectedArtId);
                    fallbacks.Add(member.MemberId + ":" + prior +
                                  "->" + selectedArtId +
                                  " (owned Pact guest unavailable)");
                }
                if (!IsArtCompatibleWithCommand(commandId, art))
                    fallbacks.Add(member.MemberId + ":no compatible " + PlayerFacingFamilyName(commandId) + " -> " + selectedArtId);
                var breakthroughTargetArtId = member.BreakthroughArtId;
                var breakthroughTarget = !string.IsNullOrWhiteSpace(breakthroughTargetArtId) &&
                    content.Arts.TryGetValue(breakthroughTargetArtId, out var targetArt)
                    ? targetArt
                    : null;
                var breakthrough =
                    StringComparer.Ordinal.Equals(member.MemberId, battle.TutorialBreakthroughMemberId) &&
                    !battle.TutorialBreakthroughOccurred &&
                    breakthroughTarget != null &&
                    CommandSupportsBreakthrough(commandId, breakthroughTarget.Discipline) &&
                    DisciplinesAreCompatibleForDiscovery(art.Discipline, breakthroughTarget.Discipline);
                if (art.PersonalMpCost > member.CurrentMp || art.SharedApCost > apRemaining || !IsEquipmentLegal(art, member.EquipmentTags))
                {
                    var prior = selectedArtId;
                    selectedArtId = commandId == "CMD_GUARD" ? "ART_GUARD" :
                        commandId == "CMD_AP_RECOVERY" || commandId == "CMD_SUPPORT"
                            ? "ART_RECOVER_BREATH"
                            : BasicArtId(member.EquipmentTags, battle);
                    art = content.Art(selectedArtId);
                    if (art.SharedApCost > apRemaining || art.PersonalMpCost > member.CurrentMp ||
                        !IsEquipmentLegal(art, member.EquipmentTags))
                    {
                        selectedArtId = "ART_RECOVER_BREATH";
                        art = content.Art(selectedArtId);
                    }
                    fallbacks.Add(member.MemberId + ":" + prior + "->" + selectedArtId);
                    breakthrough = breakthrough &&
                        DisciplinesAreCompatibleForDiscovery(art.Discipline, breakthroughTarget?.Discipline);
                }
                apRemaining -= art.SharedApCost;
                var kind = ActionKind(commandId, art, battle);
                BattleUnionState actionTargetUnion;
                if (ownedGuestTarget090 != null)
                    actionTargetUnion = ownedGuestTarget090;
                else if (kind == BattleActionKind.Restoration)
                    actionTargetUnion = SupportsFriendlyUnionTarget080(art) ? targetUnion : union;
                else if (commandId == "CMD_SUPPORT" && kind == BattleActionKind.Recovery &&
                         SupportsFriendlyUnionTarget080(art))
                    actionTargetUnion = targetUnion;
                else if (kind == BattleActionKind.Guard || kind == BattleActionKind.Recovery)
                    actionTargetUnion = union;
                else
                    actionTargetUnion = targetUnion;
                if (commandId == "CMD_SUPPORT" && actionTargetUnion != null &&
                    (IsSssAllAlliedUnionScope090(art) ||
                     !StringComparer.Ordinal.Equals(actionTargetUnion.UnionId, union.UnionId)))
                    hasFriendlyUnionSupportAction080 = true;
                if (ownedGuestTarget090 != null)
                    ownedGuestForecastTarget090 = ownedGuestTarget090;
                RestorationProjection086 restoration086 = null;
                BattleMemberState targetMember;
                if (kind == BattleActionKind.Restoration)
                {
                    restoration086 = BuildRestorationProjection086(
                        member,
                        actionTargetUnion,
                        art,
                        projectedTargetHp086,
                        EffectiveArtPowerPermille088(
                            battle, content, member, art.Id));
                    targetMember = FindMember086(
                        actionTargetUnion, restoration086.PrimaryTargetMemberId);
                    ApplyProjectedRestoration086(projectedTargetHp086, restoration086);
                }
                else
                    targetMember = TargetMemberFor(
                        commandId, kind, union, actionTargetUnion, memberIndex);

                var hpDelta = restoration086 != null
                    ? restoration086.TotalHp
                    : PredictedHpDelta(
                        member, targetMember, kind, commandId,
                        union.FormationBenefitActive, art,
                        EffectiveArtPowerPermille088(
                            battle, content, member, art.Id));
                var friendlySupport086 = commandId == "CMD_SUPPORT" &&
                    kind == BattleActionKind.Recovery && actionTargetUnion != null &&
                    SupportsFriendlyUnionTarget080(art) &&
                    !StringComparer.Ordinal.Equals(art.Id, "ART_RECOVER_BREATH");
                var cohesionDelta = restoration086 != null
                    ? restoration086.CohesionGain
                    : friendlySupport086
                        ? PredictedSupportCohesion086(actionTargetUnion, art)
                        : kind == BattleActionKind.Tactical ? -8 :
                          kind == BattleActionKind.Guard ? 3 : hpDelta < 0 ? -3 : 0;
                var formationDelta = restoration086 != null
                    ? restoration086.FormationGain
                    : friendlySupport086
                        ? PredictedSupportFormation086(actionTargetUnion, art)
                        : kind == BattleActionKind.Tactical ? -700 : 0;
                var areaPlan095 = BuildNewAreaPlan095(battle, content, member, union,
                    art, kind, commandId, actionTargetUnion, targetMember, battle.EnemyUnions);
                if (areaPlan095 != null)
                {
                    hpDelta = -areaPlan095.PredictedHpLoss;
                    cohesionDelta = -areaPlan095.TotalCohesionBudget;
                    formationDelta = -areaPlan095.TotalFormationBudget;
                }
                var meaningful = restoration086 != null
                    ? restoration086.Meaningful
                    : friendlySupport086
                        ? IsMeaningfulFriendlySupport086(
                            actionTargetUnion, art, cohesionDelta,
                            formationDelta)
                        : IsPredictedMeaningful(
                            kind, hpDelta, targetMember, actionTargetUnion,
                            member, union, commandId);
                var predictedGrowth = ScaleGrowth(
                    M2MeaningfulUse.PersonalProgressGain(kind, meaningful, hpDelta),
                    rules.ArtGrowthPct);
                var recruit070 = FindRecruit(campaign.Guild.Recruits, member.MemberId);
                if (!breakthrough && M2DeepArtRuntime070.TryGetNextLearning(
                        recruit070,
                        member,
                        art.Id,
                        meaningful ? predictedGrowth : 0,
                        content,
                        out var deepLearning070))
                {
                    deepLearningBars070.Add(member.DisplayName + " → " + deepLearning070.PlayerFacingProgress);
                    if (deepLearning070.CanLearnNow &&
                        content.Arts.TryGetValue(deepLearning070.TargetArtId, out var deepTarget070))
                    {
                        breakthrough = true;
                        breakthroughTargetArtId = deepLearning070.TargetArtId;
                        breakthroughTarget = deepTarget070;
                    }
                }
                var actionPrediction090 = restoration086 != null
                    ? restoration086.Prediction
                    : friendlySupport086
                        ? SupportPrediction086(
                            actionTargetUnion, art, cohesionDelta, formationDelta)
                        : Prediction(kind, hpDelta, cohesionDelta, formationDelta);
                if (areaPlan095 != null)
                    actionPrediction090 = AreaPrediction095(areaPlan095);
                else if (IsSssAllEnemyUnionScope090(art))
                    actionPrediction090 =
                        "Hits every living enemy Union. Per-Union preview: " +
                        actionPrediction090;
                else if (IsSssAllAlliedUnionScope090(art))
                    actionPrediction090 =
                        "Reaches every living allied Union that can benefit. " +
                        "Per-Union preview: " + actionPrediction090;
                else if (IsSssOwnedGuestUnionScope090(art))
                    actionPrediction090 =
                        "Owned Pact guest only — " + actionPrediction090;
                actions.Add(new BattlePlannedActionState(
                    member.MemberId, member.DisplayName, actionTargetUnion?.UnionId ?? string.Empty,
                    targetMember?.MemberId ?? string.Empty, art.Id, art.Name, kind,
                    art.SharedApCost, art.PersonalMpCost, hpDelta, cohesionDelta, formationDelta,
                    actionPrediction090,
                    meaningful,
                    breakthrough, art.AnimationTag, art.Discipline, predictedGrowth,
                    breakthrough ? breakthroughTarget.Id : string.Empty,
                    breakthrough ? breakthroughTarget.Name : string.Empty, areaPlan095));
            }

            // A Support order may contain self-recovery fallbacks.  Never advertise another
            // Union as the target unless at least one chosen Art can actually reach it.
            if (ownedGuestForecastTarget090 != null)
            {
                targetUnion = ownedGuestForecastTarget090;
                targetName = ownedGuestForecastTarget090.DisplayName;
            }
            else if (commandId == "CMD_SUPPORT" &&
                     !hasFriendlyUnionSupportAction080)
            {
                targetUnion = union;
                targetName = union.DisplayName;
            }

            var apCost = SumAp(actions);
            var mpCost = SumMp(actions);
            var desiredApRecovery = commandId == "CMD_AP_RECOVERY" ? 6 : commandId == "CMD_SUPPORT" ? 2 : 0;
            var apRecovery = Math.Min(desiredApRecovery, Math.Max(0, union.MaximumAp - (union.CurrentAp - apCost)));
            var expected = ExpectedEffect(actions, apRecovery, commandId);
            var risk = Risk(commandId, union, battle);
            var learning = commandId == "CMD_SUPPORT"
                ? "Meaningful formation recovery advances the Union discipline once; personal Arts grow only when they restore a missing resource."
                : commandId == "CMD_AP_RECOVERY"
                    ? "Meaningful shared AP recovery advances the Union discipline once; harmless full-resource recovery grants no growth."
                    : Learning(actions, content);
            if (deepLearningBars070.Count > 0)
                learning += " · NEXT ARTS: " + string.Join("; ", deepLearningBars070);
            LinkArtRuntimeRule026 linkArtRule;
            var hasLinkArt = PeopleBondBattleHook026.TrySelectForecastRule(
                campaign, union, battle.Round, slot, actions, apCost, out linkArtRule);
            var commandName = PlayerFacingCommandName(
                command.Id, command.Name, actions, union, targetUnion, battle);
            if (hasLinkArt)
            {
                apCost += linkArtRule.SharedApSurcharge;
                commandName = "LINK ART — " + linkArtRule.DisplayName + " • " + commandName;
                phrase = "Coordinate “" + linkArtRule.DisplayName + "” through this complete Union Forecast. " + phrase;
                expected += " · Link synergy: +" + linkArtRule.CohesionBonus + " Cohesion and " + linkArtRule.EnemyCohesionPressure + " enemy Cohesion pressure.";
                learning += " · " + PeopleBondBattleHook026.MarkerText(linkArtRule);
            }
            var identity = CanonicalJson.Sha256Hex(new
            {
                CampaignSeed = campaignSeed, battle.BattleId, battle.Round, union.UnionId,
                ForecastSlot = slot, CurrentAuthoritativeStateHash = basisHash, battle.ContentVersion
            });
            var forecastId = "FORECAST_" + identity.Substring(0, 16).ToUpperInvariant();
            var legalPool = ContextualLegalPool(
                union, commandId, content, actions, battle);
            var debug = CanonicalJson.Serialize(new
            {
                ForecastIdentity = identity, command.Id, command.Intent, LegalActionPool = legalPool,
                Weights = legalPool, ChosenMemberActions = actions,
                SharedApValidatedOnce = apCost, IndividualMpCosts = mpCost,
                AdaptiveFallbacks = fallbacks, LinkArtId = hasLinkArt ? linkArtRule.LinkArtId : string.Empty,
                LinkArtPreservesUnderlyingMemberArtIds = hasLinkArt,
                PredictedDamageStatusProgressionBreakthrough = expected + " | " + learning
            });
            var baseForecast = new BattleForecastState(
                forecastId, union.UnionId, command.Id,
                commandName, phrase, hasLinkArt ? linkArtRule.ForecastIntent : command.Intent,
                targetUnion?.UnionId ?? "OBJECTIVE_TUTORIAL", targetName, actions.AsReadOnly(),
                apCost, apRecovery, mpCost, expected, risk, learning,
                command.Fallback + (fallbacks.Count == 0 ? string.Empty : " · Applied: " + string.Join(", ", fallbacks)),
                identity, debug);
            return SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                    campaign, battle, union, slot, baseForecast, out var ultimateForecast)
                ? ultimateForecast
                : baseForecast;
        }

        private static void ResolvePlayerForecast(
            CampaignState campaign, BattleState battle, M2CombatContent content, ModeRuleSnapshot rules, int unionIndex,
            BattleForecastState forecast, List<BattleUnionState> players, List<BattleUnionState> enemies,
            List<BattleEventState> events)
        {
            var union = players[unionIndex];
            var opensSideStrike = StringComparer.Ordinal.Equals(forecast.CommandId, "CMD_FLANK");
            union = union.With(currentAp: union.CurrentAp - forecast.SharedApCost, guarding: false,
                engagement: forecast.CommandId == "CMD_RETREAT"
                    ? EngagementState.Disengaging
                    : opensSideStrike ? EngagementState.Flanking : EngagementState.Advancing);
            players[unionIndex] = union;
            events.Add(Event(events.Count, battle.Round, "FORECAST_COMMITTED", BattleSide.Player,
                union.UnionId, string.Empty, forecast.CommandId,
                union.DisplayName + " commits “" + forecast.CommandName + "” — " + forecast.Phrase,
                forecast.SharedApCost, union.UnionId, string.Empty, forecast.TargetId, string.Empty));
            if(EchoInvocationForecastAuthority022.TryAuthorize(campaign,battle,forecast,out var echoInvocation))
            {
                if (TryPrepayInvocationPersonalMp088(
                        players, unionIndex, echoInvocation.InvokerMemberId,
                        echoInvocation.PersonalMpCost))
                {
                    var effect=InvocationBattleEffects022.Apply(echoInvocation.Role,echoInvocation.SharedApCost,
                        union.UnionId,echoInvocation.InvokerMemberId,forecast.TargetId,players,enemies);
                    if(effect.Amount>0)
                        events.Add(Event(events.Count,battle.Round,EchoInvocationForecastAuthority022.EventType,BattleSide.Player,
                            union.UnionId,echoInvocation.InvokerMemberId,echoInvocation.EchoId,
                            EchoInvocationForecastAuthority022.EventText(union.DisplayName,echoInvocation),effect.Amount,
                            union.UnionId,echoInvocation.InvokerMemberId,effect.TargetUnionId,effect.TargetMemberId));
                }
                else echoInvocation = null;
            }
            if (StopPlayerForecastIfVictory088(
                    battle.Round, unionIndex, forecast, players, enemies, events, content))
                return;
            if(CovenantInvocationForecastAuthority022.TryAuthorize(campaign,battle,forecast,out var covenantInvocation))
            {
                if (TryPrepayInvocationPersonalMp088(
                        players, unionIndex, covenantInvocation.InvokerMemberId,
                        covenantInvocation.PersonalMpCost))
                {
                    var effect=InvocationBattleEffects022.Apply(covenantInvocation.Role,covenantInvocation.SharedApCost,
                        union.UnionId,covenantInvocation.InvokerMemberId,forecast.TargetId,players,enemies);
                    if(effect.Amount>0)
                        events.Add(Event(events.Count,battle.Round,CovenantInvocationForecastAuthority022.EventType,BattleSide.Player,
                            union.UnionId,covenantInvocation.InvokerMemberId,covenantInvocation.CovenantId,
                            CovenantInvocationForecastAuthority022.EventText(union.DisplayName,covenantInvocation),effect.Amount,
                            union.UnionId,covenantInvocation.InvokerMemberId,effect.TargetUnionId,effect.TargetMemberId));
                }
                else covenantInvocation = null;
            }
            if (StopPlayerForecastIfVictory088(
                    battle.Round, unionIndex, forecast, players, enemies, events, content))
                return;

            if (opensSideStrike)
            {
                var targetUnionIndex = FindUnionIndex(enemies, forecast.TargetId);
                if (targetUnionIndex < 0) targetUnionIndex = FirstActiveIndex(enemies);
                var targetUnion = targetUnionIndex < 0 ? null : enemies[targetUnionIndex];
                var targetMemberIndex = targetUnion == null ? -1 : FirstActiveMemberIndex(targetUnion);
                var targetMemberId = targetMemberIndex < 0 ? string.Empty : targetUnion.Members[targetMemberIndex].MemberId;
                events.Add(Event(events.Count, battle.Round, "POSITION_SHIFT", BattleSide.Player,
                    union.UnionId, union.LeaderMemberId, forecast.CommandId,
                    union.DisplayName + " breaks off the main deadlock and opens a SIDE STRIKE on " +
                    (targetUnion?.DisplayName ?? forecast.TargetName) + "'s BLIND SIDE.", 0,
                    union.UnionId, union.LeaderMemberId,
                    targetUnion?.UnionId ?? forecast.TargetId, targetMemberId));
            }

            for (var actionIndex = 0; actionIndex < forecast.MemberActions.Count; actionIndex++)
            {
                var action = forecast.MemberActions[actionIndex];
                union = players[unionIndex];
                var actorIndex = union.FindMemberIndex(action.ActorMemberId);
                if (actorIndex < 0 || union.Members[actorIndex].Downed) continue;
                if (!TryPreparePendingMemberAction088(
                        battle, content, forecast, unionIndex,
                        action, players, enemies, events,
                        out var preparedAction088,
                        out var victoryStop088))
                {
                    RefundUnexecutedBaseArtAp090(
                        players, unionIndex, forecast, content, actionIndex,
                        victoryStop088 ? forecast.MemberActions.Count : actionIndex + 1);
                    if (victoryStop088) break;
                    continue;
                }
                action = preparedAction088;
                var authorizedInvocationAp086 = 0;
                var authorizedInvocationMp086 = 0;
                if (echoInvocation != null && StringComparer.Ordinal.Equals(
                        echoInvocation.InvokerMemberId, action.ActorMemberId))
                {
                    authorizedInvocationAp086 = checked(
                        authorizedInvocationAp086 + echoInvocation.SharedApCost);
                    authorizedInvocationMp086 = checked(
                        authorizedInvocationMp086 + echoInvocation.PersonalMpCost);
                }
                if (covenantInvocation != null && StringComparer.Ordinal.Equals(
                        covenantInvocation.InvokerMemberId, action.ActorMemberId))
                {
                    authorizedInvocationAp086 = checked(
                        authorizedInvocationAp086 + covenantInvocation.SharedApCost);
                    authorizedInvocationMp086 = checked(
                        authorizedInvocationMp086 + covenantInvocation.PersonalMpCost);
                }
                if (action.Kind == BattleActionKind.Recovery &&
                    content.Arts.TryGetValue(
                        action.ArtId, out var ownedGuestSupportArt090) &&
                    IsSssOwnedGuestUnionScope090(ownedGuestSupportArt090))
                {
                    var ownedGuest090 = OwnedLivingGuestUnion090(
                        battle, players, action.ActorMemberId, union.UnionId);
                    if (ownedGuest090 == null)
                    {
                        var refundableBaseArtAp090 = Math.Max(
                            0,
                            action.SharedApCost - authorizedInvocationAp086);
                        var refundedAp090 = Math.Min(
                            refundableBaseArtAp090,
                            Math.Max(0, union.MaximumAp - union.CurrentAp));
                        if (refundedAp090 > 0)
                        {
                            union = union.With(
                                currentAp: union.CurrentAp + refundedAp090);
                            players[unionIndex] = union;
                        }
                        events.Add(Event(
                            events.Count,
                            battle.Round,
                            "SSS_OWNED_GUEST_SUPPORT_WITHHELD",
                            BattleSide.Player,
                            union.UnionId,
                            action.ActorMemberId,
                            action.ArtId,
                            action.ActorName + " withholds " +
                            action.ArtName +
                            "; their owned Pact guest Union is no longer active. " +
                            refundedAp090 +
                            " base-Art AP is preserved and no base-Art MP is spent.",
                            refundedAp090,
                            union.UnionId,
                            action.ActorMemberId,
                            action.TargetUnionId,
                            action.TargetMemberId));
                        continue;
                    }

                    var cohesion090 = PredictedSupportCohesion086(
                        ownedGuest090, ownedGuestSupportArt090);
                    var formation090 = PredictedSupportFormation086(
                        ownedGuest090, ownedGuestSupportArt090);
                    if (!StringComparer.Ordinal.Equals(
                            action.TargetUnionId, ownedGuest090.UnionId))
                    {
                        events.Add(Event(
                            events.Count,
                            battle.Round,
                            "SSS_OWNED_GUEST_SUPPORT_RETARGETED",
                            BattleSide.Player,
                            ownedGuest090.UnionId,
                            ownedGuest090.LeaderMemberId,
                            action.ArtId,
                            action.ActorName + " binds " + action.ArtName +
                            " to their owned Pact guest " +
                            ownedGuest090.DisplayName + ".",
                            0,
                            union.UnionId,
                            action.ActorMemberId,
                            ownedGuest090.UnionId,
                            ownedGuest090.LeaderMemberId));
                    }
                    action = RetargetSssAreaAction090(
                        action,
                        ownedGuest090.UnionId,
                        ownedGuest090.LeaderMemberId,
                        0,
                        cohesion090,
                        formation090,
                        "Owned Pact guest only — " + SupportPrediction086(
                            ownedGuest090,
                            ownedGuestSupportArt090,
                            cohesion090,
                            formation090));
                }
                if (action.Kind == BattleActionKind.Restoration)
                {
                    if (!TryPrepareRestorationAction086(
                            action, players, unionIndex, content, battle,
                            authorizedInvocationAp086,
                            authorizedInvocationMp086,
                            out var preparedRestoration086,
                            out var restorationTarget086,
                            out var retargetedRestoration086))
                    {
                        var refundableBaseArtAp086 = Math.Max(
                            0, action.SharedApCost - authorizedInvocationAp086);
                        var refundedAp086 = Math.Min(
                            refundableBaseArtAp086,
                            Math.Max(0, union.MaximumAp - union.CurrentAp));
                        if (refundedAp086 > 0)
                        {
                            union = union.With(
                                currentAp: union.CurrentAp + refundedAp086);
                            players[unionIndex] = union;
                        }
                        events.Add(Event(
                            events.Count,
                            battle.Round,
                            "RESTORATION_WITHHELD",
                            BattleSide.Player,
                            union.UnionId,
                            action.ActorMemberId,
                            action.ArtId,
                            action.ActorName + " withholds " + action.ArtName +
                            "; no allied Union now needs that restoration. " +
                            (authorizedInvocationAp086 > 0 ||
                             authorizedInvocationMp086 > 0
                                ? refundedAp086 +
                                  " base-Art AP and its base-Art MP are preserved; " +
                                  "the resolved Invocation cost remains paid."
                                : refundedAp086 +
                                  " AP preserved and no MP spent."),
                            refundedAp086,
                            union.UnionId,
                            action.ActorMemberId,
                            action.TargetUnionId,
                            action.TargetMemberId));
                        continue;
                    }
                    if (retargetedRestoration086)
                    {
                        events.Add(Event(
                            events.Count,
                            battle.Round,
                            "RESTORATION_RETARGETED",
                            BattleSide.Player,
                            restorationTarget086.UnionId,
                            preparedRestoration086.TargetMemberId,
                            action.ArtId,
                            action.ActorName + " adapts " + action.ArtName +
                            " to " + restorationTarget086.DisplayName +
                            ", the allied Union that now needs it most.",
                            0,
                            union.UnionId,
                            action.ActorMemberId,
                            restorationTarget086.UnionId,
                            preparedRestoration086.TargetMemberId));
                    }
                    action = preparedRestoration086;
                }
                var members = new List<BattleMemberState>(union.Members);
                var actor = members[actorIndex];
                // Invocation MP is paid at the moment its effect resolves so a
                // terminal summon cannot defeat the final enemy for zero MP.
                // The forecast action still carries the combined cost for
                // legality/preview, therefore only its remaining Art cost is
                // charged here.
                var remainingActionMpCost088 = Math.Max(
                    0, action.PersonalMpCost - authorizedInvocationMp086);
                actor = actor.With(
                    currentMp: actor.CurrentMp - remainingActionMpCost088,
                    guarding: false);
                members[actorIndex] = actor;
                union = union.With(members: members.AsReadOnly());
                players[unionIndex] = union;

                var meaningful = false;
                var usefulMagnitude = 0;
                switch (action.Kind)
                {
                    case BattleActionKind.Guard:
                        SetMemberGuarding(players, unionIndex, actorIndex, true);
                        players[unionIndex] = players[unionIndex].With(
                            guarding: true, cohesion: Math.Min(100, players[unionIndex].Cohesion + Math.Max(2, action.PredictedCohesionDelta)),
                            engagement: EngagementState.Guarded);
                        events.Add(Event(events.Count, battle.Round, "GUARD", BattleSide.Player,
                            union.UnionId, actor.MemberId, action.ArtId,
                            actor.DisplayName + " raises an interception guard.", 0,
                            union.UnionId, actor.MemberId, union.UnionId, actor.MemberId));
                        break;
                    case BattleActionKind.Restoration:
                        usefulMagnitude = content.Arts.TryGetValue(
                                action.ArtId, out var restorationArt090) &&
                            IsSssAllAlliedUnionScope090(restorationArt090)
                                ? ResolveSssAlliedRestoration090(
                                    battle.Round, action, players, unionIndex,
                                    content, events, BattleSide.Player,
                                    authorizedInvocationAp086,
                                    authorizedInvocationMp086, battle)
                                : ResolveRestoration(
                                    battle.Round, action, players, unionIndex,
                                    content, events, BattleSide.Player,
                                    authorizedInvocationAp086,
                                    authorizedInvocationMp086, battle);
                        meaningful = usefulMagnitude > 0;
                        break;
                    case BattleActionKind.Recovery:
                        if (forecast.CommandId == "CMD_SUPPORT" &&
                            content.Arts.TryGetValue(action.ArtId, out var supportArt086) &&
                            SupportsFriendlyUnionTarget080(supportArt086) &&
                            !StringComparer.Ordinal.Equals(
                                supportArt086.Id, "ART_RECOVER_BREATH"))
                        {
                            usefulMagnitude = IsSssAllAlliedUnionScope090(
                                    supportArt086)
                                ? ResolveSssAlliedSupport090(
                                    battle.Round, action, players, unionIndex,
                                    supportArt086, events, BattleSide.Player,
                                    authorizedInvocationAp086,
                                    authorizedInvocationMp086)
                                : ResolveFriendlySupportArt086(
                                    battle.Round, action, players, unionIndex,
                                    supportArt086, events, BattleSide.Player,
                                    authorizedInvocationAp086,
                                    authorizedInvocationMp086);
                            // Support Arts historically kept the actor's small personal
                            // recovery while restoring the Union. Cross-Union targeting
                            // adds the allied effect; it must not silently remove that
                            // established MP recovery or its visible Art-identity event.
                            var supportActor086 = players[unionIndex].Members[actorIndex];
                            var supportMpBefore086 = supportActor086.CurrentMp;
                            var supportMpAfter086 = Math.Min(
                                supportActor086.MaximumMp,
                                supportMpBefore086 + 2);
                            SetMemberMp(players, unionIndex, actorIndex, supportMpAfter086);
                            var supportMpRecovered086 = Math.Max(
                                0, supportMpAfter086 - supportMpBefore086);
                            events.Add(Event(
                                events.Count,
                                battle.Round,
                                "RECOVERY",
                                BattleSide.Player,
                                union.UnionId,
                                actor.MemberId,
                                action.ArtId,
                                actor.DisplayName +
                                (supportMpRecovered086 > 0
                                    ? " supports the line and recovers " +
                                      supportMpRecovered086 + " MP."
                                    : " supports the line; no MP was missing."),
                                supportMpRecovered086,
                                union.UnionId,
                                actor.MemberId,
                                union.UnionId,
                                actor.MemberId));
                            usefulMagnitude += supportMpRecovered086;
                            meaningful = usefulMagnitude > 0;
                            break;
                        }
                        var beforeRecovery = players[unionIndex].Members[actorIndex].CurrentMp;
                        var recovered = Math.Min(actor.MaximumMp, actor.CurrentMp + 2);
                        SetMemberMp(players, unionIndex, actorIndex, recovered);
                        usefulMagnitude = Math.Max(0, recovered - beforeRecovery);
                        meaningful = usefulMagnitude > 0;
                        events.Add(Event(events.Count, battle.Round, "RECOVERY", BattleSide.Player,
                            union.UnionId, actor.MemberId, action.ArtId,
                            actor.DisplayName + (usefulMagnitude > 0
                                ? " conserves strength and recovers " + usefulMagnitude + " MP."
                                : " conserves strength; no MP was missing."), usefulMagnitude,
                            union.UnionId, actor.MemberId, union.UnionId, actor.MemberId));
                        break;
                    default:
                        usefulMagnitude = action.AreaActionPlan095 != null
                            ? ResolveAreaAttack095(battle.Round, action, players, enemies, events, BattleSide.Player)
                            : content.Arts.TryGetValue(
                                action.ArtId, out var attackArt090) &&
                            IsSssAllEnemyUnionScope090(attackArt090)
                                ? ResolveSssAllEnemyUnions090(
                                    battle.Round, action, players, enemies,
                                    events)
                                : ResolveAttack(
                                    battle.Round, action, players, enemies,
                                    events);
                        meaningful = usefulMagnitude > 0;
                        break;
                }
                if (action.Kind != BattleActionKind.Guard &&
                    !SssBattleIntegration090.IsSyntheticMember090(
                        action.ActorMemberId))
                    ApplyActionProgress(
                        campaign, battle, content, rules, unionIndex, action, meaningful, usefulMagnitude,
                        players, events);
                if (AllDefeated(enemies))
                {
                    RefundUnexecutedBaseArtAp090(
                        players, unionIndex, forecast, content,
                        actionIndex + 1, forecast.MemberActions.Count);
                    AddVictorySequenceStop088(
                        battle.Round, union, action, events);
                    break;
                }
            }

            // Terminal combat cannot fall through into command-level recovery,
            // support, retreat, or movement after the final living enemy falls.
            if (AllDefeated(enemies))
            {
                players[unionIndex] = players[unionIndex].With(
                    engagement: EngagementState.Engaged);
                return;
            }

            union = players[unionIndex];
            if (forecast.CommandId == "CMD_SUPPORT")
            {
                var supportUnionIndex080 = ResolveSupportTargetIndex080(
                    forecast, players, unionIndex, content);
                var supportUnion080 = players[supportUnionIndex080];
                var cohesionGain = Math.Min(10, Math.Max(0, 100 - supportUnion080.Cohesion));
                var formationGain = Math.Min(
                    1200, Math.Max(0, 10000 - supportUnion080.FormationConditionBasisPoints));
                supportUnion080 = supportUnion080.With(
                    cohesion: supportUnion080.Cohesion + cohesionGain,
                    formationConditionBasisPoints:
                        supportUnion080.FormationConditionBasisPoints + formationGain,
                    unionMeaningfulUsePoints: supportUnionIndex080 == unionIndex
                        ? supportUnion080.UnionMeaningfulUsePoints +
                          (cohesionGain > 0 || formationGain > 0
                              ? ScaleGrowth(M2MeaningfulUse.UnionProgressGain(true), rules.UnionGrowthPct)
                              : 0)
                        : supportUnion080.UnionMeaningfulUsePoints);
                players[supportUnionIndex080] = supportUnion080;
                if (supportUnionIndex080 != unionIndex && (cohesionGain > 0 || formationGain > 0))
                    players[unionIndex] = players[unionIndex].With(
                        unionMeaningfulUsePoints: players[unionIndex].UnionMeaningfulUsePoints +
                            ScaleGrowth(M2MeaningfulUse.UnionProgressGain(true), rules.UnionGrowthPct));
                events.Add(Event(events.Count, battle.Round, "FORMATION_RECOVERY", BattleSide.Player,
                    supportUnion080.UnionId, string.Empty, forecast.CommandId,
                    union.DisplayName + (supportUnionIndex080 == unionIndex
                        ? " restores its own formation: "
                        : " supports " + supportUnion080.DisplayName + ": ") +
                    cohesionGain + " Cohesion and " + formationGain / 100 +
                    "% formation condition restored.", formationGain / 100,
                    union.UnionId, string.Empty, supportUnion080.UnionId, string.Empty));
                union = players[unionIndex];
            }
            if (forecast.ApRecovery > 0)
            {
                var actualAp = Math.Min(forecast.ApRecovery, Math.Max(0, union.MaximumAp - union.CurrentAp));
                union = union.With(
                    currentAp: union.CurrentAp + actualAp,
                    unionMeaningfulUsePoints: union.UnionMeaningfulUsePoints +
                        (actualAp > 0 && forecast.CommandId == "CMD_AP_RECOVERY"
                            ? ScaleGrowth(M2MeaningfulUse.UnionProgressGain(true), rules.UnionGrowthPct)
                            : 0));
                events.Add(Event(events.Count, battle.Round, "AP_RECOVERY", BattleSide.Player,
                    union.UnionId, string.Empty, forecast.CommandId,
                    union.DisplayName + " recovers " + actualAp + " shared AP.", actualAp,
                    union.UnionId, string.Empty, union.UnionId, string.Empty));
            }
            if (forecast.CommandId == "CMD_RETREAT")
            {
                union = union.With(retreated: true, engagement: EngagementState.Disengaging);
                events.Add(Event(events.Count, battle.Round, "RETREAT", BattleSide.Player,
                    union.UnionId, string.Empty, forecast.CommandId,
                    union.DisplayName + " withdraws safely from the training arena.", 0,
                    union.UnionId, string.Empty, union.UnionId, string.Empty));
            }
            else if (!union.IsDefeated)
                union = union.With(engagement: opensSideStrike ? EngagementState.Flanking : EngagementState.Engaged);
            players[unionIndex] = union;
        }

        private static int ResolveAttack(int round, BattlePlannedActionState action,
            List<BattleUnionState> players, List<BattleUnionState> enemies, List<BattleEventState> events,
            BattleSide actingSide = BattleSide.Player, int? exactDamage095 = null,
            int? exactCohesion095 = null, int? exactFormation095 = null)
        {
            var targetUnionIndex = FindUnionIndex(enemies, action.TargetUnionId);
            if (targetUnionIndex < 0 || !IsActive(enemies[targetUnionIndex])) return 0;
            var targetUnion = enemies[targetUnionIndex];
            var targetIndex = targetUnion.FindMemberIndex(action.TargetMemberId);
            if (targetIndex < 0 || targetUnion.Members[targetIndex].Downed) return 0;
            var members = new List<BattleMemberState>(targetUnion.Members);
            var target = members[targetIndex];
            var damage = exactDamage095 ?? Math.Max(1, -action.PredictedHpDelta);
            if (damage <= 0) return 0;
            if (!exactDamage095.HasValue && (targetUnion.Guarding || target.Guarding))
                damage = Math.Max(1, damage / 2);
            damage=AdjustNativeDamage161(round,actingSide,FindMemberUnionId(players,action.ActorMemberId),
                targetUnion.UnionId,target.MemberId,action.Kind,damage,enemies,events,action.ArtId);
            if(damage<=0)return 0;
            var hp = Math.Max(0, target.CurrentHp - damage);
            members[targetIndex] = target.With(currentHp: hp, stabilized: false, guarding: false);
            var cohesionDamage = exactCohesion095 ?? Math.Max(2, -action.PredictedCohesionDelta);
            var formationDamage = exactFormation095 ?? Math.Max(300, -action.PredictedFormationDelta);
            targetUnion = targetUnion.With(
                members: members.AsReadOnly(), cohesion: Math.Max(0, targetUnion.Cohesion - cohesionDamage),
                formationConditionBasisPoints: Math.Max(0, targetUnion.FormationConditionBasisPoints - formationDamage),
                engagement: members.All(value => value.Downed) ||
                            targetUnion.Cohesion - cohesionDamage <= 0
                    ? EngagementState.Broken
                    : action.Kind == BattleActionKind.Tactical
                        ? EngagementState.RearPressure
                        : EngagementState.Engaged,
                guarding: false);
            enemies[targetUnionIndex] = targetUnion;
            var hitType = actingSide == BattleSide.Enemy ? "ENEMY_HIT" : action.Kind == BattleActionKind.Mystic
                ? "MYSTIC_HIT"
                : action.Kind == BattleActionKind.Tactical ? "TACTICAL_HIT" : "MARTIAL_HIT";
            events.Add(Event(events.Count, round, hitType,
                actingSide, targetUnion.UnionId, target.MemberId, action.ArtId,
                action.ActorName + " uses " + action.ArtName + " on " + target.DisplayName + " for " + damage + " HP.", damage,
                FindMemberUnionId(players, action.ActorMemberId), action.ActorMemberId,
                targetUnion.UnionId, target.MemberId));
            if (hp == 0) events.Add(Event(events.Count, round, "DOWNED", targetUnion.Side,
                targetUnion.UnionId, target.MemberId, action.ArtId,
                target.DisplayName + " is Downed — not dead.", 0,
                FindMemberUnionId(players, action.ActorMemberId), action.ActorMemberId,
                targetUnion.UnionId, target.MemberId));
            if (targetUnion.IsDefeated)
                events.Add(Event(events.Count, round,
                    targetUnion.Side == BattleSide.Enemy ? "ENEMY_UNION_DEFEATED" : "PLAYER_UNION_DEFEATED", targetUnion.Side,
                    targetUnion.UnionId, target.MemberId, action.ArtId,
                    targetUnion.DisplayName +
                    " is defeated; its deadlock and target authority end immediately.", 0,
                    FindMemberUnionId(players, action.ActorMemberId), action.ActorMemberId,
                    targetUnion.UnionId, target.MemberId));
            return Math.Min(damage, target.CurrentHp);
        }

        private static int ResolveSssAllEnemyUnions090(
            int round,
            BattlePlannedActionState action,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events)
        {
            var useful = 0;
            for (var unionIndex = 0; unionIndex < enemies.Count; unionIndex++)
            {
                var targetUnion = enemies[unionIndex];
                if (!IsActive(targetUnion)) continue;
                var targetMemberIndex = FirstActiveMemberIndex(targetUnion);
                if (targetMemberIndex < 0) continue;
                var targetMember = targetUnion.Members[targetMemberIndex];
                useful = checked(useful + ResolveAttack(
                    round,
                    RetargetSssAreaAction090(
                        action,
                        targetUnion.UnionId,
                        targetMember.MemberId,
                        action.PredictedHpDelta,
                        action.PredictedCohesionDelta,
                        action.PredictedFormationDelta,
                        "Resolve against every living enemy Union."),
                    players,
                    enemies,
                    events));
            }
            return useful;
        }

        private static BattlePlannedActionState RetargetSssAreaAction090(
            BattlePlannedActionState action,
            string targetUnionId,
            string targetMemberId,
            int predictedHpDelta,
            int predictedCohesionDelta,
            int predictedFormationDelta,
            string prediction) =>
            new BattlePlannedActionState(
                action.ActorMemberId,
                action.ActorName,
                targetUnionId,
                targetMemberId,
                action.ArtId,
                action.ArtName,
                action.Kind,
                action.SharedApCost,
                action.PersonalMpCost,
                predictedHpDelta,
                predictedCohesionDelta,
                predictedFormationDelta,
                prediction,
                action.MeaningfulUse,
                action.BreakthroughOpportunity,
                action.AnimationTag,
                action.Discipline,
                action.PredictedGrowth,
                action.BreakthroughTargetArtId,
                action.BreakthroughTargetArtName);

        private static bool TryPreparePendingMemberAction088(
            BattleState battle,
            M2CombatContent content,
            BattleForecastState forecast,
            int sourceUnionIndex,
            BattlePlannedActionState action,
            List<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies,
            List<BattleEventState> events,
            out BattlePlannedActionState prepared,
            out bool victoryStop)
        {
            prepared = action;
            victoryStop = false;
            if (!IsOffensiveAction088(action.Kind)) return true;

            var source = players[sourceUnionIndex];
            var actorIndex = source.FindMemberIndex(action.ActorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed) return false;
            var actor = source.Members[actorIndex];
            if (action.AreaActionPlan095 != null)
                return TryPrepareAreaAction095(battle, content, forecast, source, actor,
                    action, enemies, events, out prepared, out victoryStop);
            var predictedUnionIndex = FindUnionIndex(enemies, action.TargetUnionId);
            var predictedUnion = predictedUnionIndex < 0
                ? null
                : enemies[predictedUnionIndex];
            var predictedMemberIndex = predictedUnion == null
                ? -1
                : predictedUnion.FindMemberIndex(action.TargetMemberId);
            if (predictedUnion != null && IsActive(predictedUnion) &&
                predictedMemberIndex >= 0 &&
                !predictedUnion.Members[predictedMemberIndex].Downed)
                return true;

            content.Arts.TryGetValue(action.ArtId, out var art);
            if (predictedUnion != null && IsActive(predictedUnion))
            {
                if (!AllowsEquivalentOffensiveRetarget088(art))
                {
                    AddDeadTargetCancellation088(
                        battle.Round, source, action, events);
                    return false;
                }
                var replacementMemberIndex = DeterministicLivingMemberIndex088(predictedUnion);
                if (replacementMemberIndex < 0)
                {
                    AddDeadTargetCancellation088(
                        battle.Round, source, action, events);
                    return false;
                }
                var replacementMember = predictedUnion.Members[replacementMemberIndex];
                prepared = RetargetOffensiveAction088(
                    battle, content, forecast, source, actor, action,
                    predictedUnion, replacementMember);
                events.Add(Event(events.Count, battle.Round,
                    "TARGET_MEMBER_RETARGETED", BattleSide.Player,
                    source.UnionId, actor.MemberId, action.ArtId,
                    actor.DisplayName + " redirects " + action.ArtName + " from the Downed target to " +
                    replacementMember.DisplayName + " in the same enemy Union.", 0,
                    source.UnionId, actor.MemberId,
                    predictedUnion.UnionId, replacementMember.MemberId));
                return true;
            }

            var replacementUnionIndex = NextLivingEnemyUnionIndex088(
                enemies, predictedUnionIndex, forecast.CommandId);
            if (replacementUnionIndex < 0)
            {
                if (AllDefeated(enemies))
                {
                    AddVictorySequenceStop088(battle.Round, source, action, events);
                    victoryStop = true;
                }
                else AddDeadTargetCancellation088(battle.Round, source, action, events);
                return false;
            }
            if (!AllowsEquivalentOffensiveRetarget088(art))
            {
                AddDeadTargetCancellation088(
                    battle.Round, source, action, events);
                return false;
            }

            var replacementUnion = enemies[replacementUnionIndex];
            var replacementIndex = DeterministicLivingMemberIndex088(replacementUnion);
            if (replacementIndex < 0)
            {
                AddDeadTargetCancellation088(
                    battle.Round, source, action, events);
                return false;
            }
            var replacement = replacementUnion.Members[replacementIndex];
            prepared = RetargetOffensiveAction088(
                battle, content, forecast, source, actor, action,
                replacementUnion, replacement);
            events.Add(Event(events.Count, battle.Round,
                "TARGET_UNION_RETARGETED", BattleSide.Player,
                source.UnionId, actor.MemberId, action.ArtId,
                actor.DisplayName + " transfers " + action.ArtName + " to " +
                replacementUnion.DisplayName + "; engagement legality is recalculated.", 0,
                source.UnionId, actor.MemberId,
                replacementUnion.UnionId, replacement.MemberId));
            events.Add(Event(events.Count, battle.Round,
                "POSITION_SHIFT", BattleSide.Player,
                source.UnionId, actor.MemberId, action.ArtId,
                source.DisplayName + " breaks the ended deadlock, reorients, and closes on " +
                replacementUnion.DisplayName + ".", 0,
                source.UnionId, actor.MemberId,
                replacementUnion.UnionId, replacement.MemberId));
            return true;
        }

        private static BattlePlannedActionState RetargetOffensiveAction088(
            BattleState battle,
            M2CombatContent content,
            BattleForecastState forecast,
            BattleUnionState source,
            BattleMemberState actor,
            BattlePlannedActionState action,
            BattleUnionState targetUnion,
            BattleMemberState target)
        {
            content.Arts.TryGetValue(action.ArtId, out var art);
            var hpDelta = PredictedHpDelta(
                actor, target, action.Kind, forecast.CommandId,
                source.FormationBenefitActive, art,
                EffectiveArtPowerPermille088(
                    battle, content, actor, action.ArtId));
            return new BattlePlannedActionState(
                action.ActorMemberId, action.ActorName,
                targetUnion.UnionId, target.MemberId,
                action.ArtId, action.ArtName, action.Kind,
                action.SharedApCost, action.PersonalMpCost,
                hpDelta, action.PredictedCohesionDelta,
                action.PredictedFormationDelta,
                action.Prediction, action.MeaningfulUse,
                action.BreakthroughOpportunity, action.AnimationTag,
                action.Discipline, action.PredictedGrowth,
                action.BreakthroughTargetArtId,
                action.BreakthroughTargetArtName);
        }

        private static bool AllowsEquivalentOffensiveRetarget088(M2ArtDefinition art)
        {
            if (art == null) return false;
            var targetRule = (art.TargetRule ?? string.Empty).Trim().ToUpperInvariant();
            if (targetRule.Contains("SPECIFIC") || targetRule.Contains("EXACT") ||
                targetRule.Contains("LOCKED") || targetRule.Contains("MARKED") ||
                targetRule.Contains("OBJECTIVE"))
                return false;
            if (HasAnyTag080(art,
                    "MARK", "TARGET_LOCK", "TARGET_LOCKED", "TARGET_SPECIFIC"))
                return false;
            return string.IsNullOrWhiteSpace(targetRule) ||
                   targetRule.Contains("CONTEXTUAL") ||
                   targetRule.Contains("ENEMY") ||
                   targetRule.Contains("FOE") ||
                   targetRule.Contains("HOSTILE");
        }

        private static int NextLivingEnemyUnionIndex088(
            IReadOnlyList<BattleUnionState> enemies,
            int endedUnionIndex,
            string commandId)
        {
            var best = -1;
            for (var index = 0; index < enemies.Count; index++)
            {
                if (!IsActive(enemies[index])) continue;
                if (best < 0 || CompareRetargetCandidates088(
                        enemies[index], index, enemies[best], best,
                        endedUnionIndex, commandId) < 0)
                    best = index;
            }
            return best;
        }

        private static int CompareRetargetCandidates088(
            BattleUnionState left,
            int leftIndex,
            BattleUnionState right,
            int rightIndex,
            int endedUnionIndex,
            string commandId)
        {
            var compare = EngagementAdjacencyPriority088(left.Engagement)
                .CompareTo(EngagementAdjacencyPriority088(right.Engagement));
            if (compare != 0) return compare;
            compare = RetargetOpportunityPriority088(left, commandId)
                .CompareTo(RetargetOpportunityPriority088(right, commandId));
            if (compare != 0) return compare;
            var leftDistance = endedUnionIndex < 0
                ? leftIndex
                : Math.Abs(leftIndex - endedUnionIndex);
            var rightDistance = endedUnionIndex < 0
                ? rightIndex
                : Math.Abs(rightIndex - endedUnionIndex);
            compare = leftDistance.CompareTo(rightDistance);
            if (compare != 0) return compare;
            compare = RetargetThreat088(right).CompareTo(RetargetThreat088(left));
            return compare != 0
                ? compare
                : StringComparer.Ordinal.Compare(left.UnionId, right.UnionId);
        }

        private static int EngagementAdjacencyPriority088(EngagementState engagement)
        {
            switch (engagement)
            {
                case EngagementState.Engaged:
                case EngagementState.Guarded:
                case EngagementState.Intercepting:
                    return 0;
                case EngagementState.Flanking:
                case EngagementState.RearPressure:
                case EngagementState.Broken:
                    return 1;
                case EngagementState.Advancing:
                    return 2;
                default:
                    return 3;
            }
        }

        private static int RetargetOpportunityPriority088(
            BattleUnionState union,
            string commandId)
        {
            if (StringComparer.Ordinal.Equals(commandId, "CMD_FLANK") &&
                (union.Engagement == EngagementState.Engaged || union.Guarding))
                return 0;
            return union.Engagement == EngagementState.Broken ||
                   union.Engagement == EngagementState.RearPressure ||
                   union.Engagement == EngagementState.Flanking
                ? 0
                : 1;
        }

        private static int RetargetThreat088(BattleUnionState union)
        {
            var threat = union.CurrentAp * 10 + union.Cohesion;
            for (var index = 0; index < union.Members.Count; index++)
                if (!union.Members[index].Downed)
                    threat = checked(threat + union.Members[index].Attack +
                                     union.Members[index].MagicAttack);
            return threat;
        }

        private static int DeterministicLivingMemberIndex088(BattleUnionState union)
        {
            var best = -1;
            for (var index = 0; index < union.Members.Count; index++)
            {
                if (union.Members[index].Downed) continue;
                if (best < 0 || StringComparer.Ordinal.Compare(
                        union.Members[index].MemberId,
                        union.Members[best].MemberId) < 0)
                    best = index;
            }
            return best;
        }

        private static bool IsOffensiveAction088(BattleActionKind kind) =>
            kind == BattleActionKind.Martial ||
            kind == BattleActionKind.Mystic ||
            kind == BattleActionKind.Tactical;

        private static void AddDeadTargetCancellation088(
            int round,
            BattleUnionState source,
            BattlePlannedActionState action,
            List<BattleEventState> events)
        {
            events.Add(Event(events.Count, round,
                "ACTION_CANCELED_DEAD_TARGET", BattleSide.Player,
                source.UnionId, action.ActorMemberId, action.ArtId,
                action.ActorName + " cancels " + action.ArtName +
                "; no legal target remains for this action. Base-Art AP is preserved; no MP, hit, effect, or mastery is applied.",
                0, source.UnionId, action.ActorMemberId,
                string.Empty, string.Empty));
        }

        private static void AddVictorySequenceStop088(
            int round,
            BattleUnionState source,
            BattlePlannedActionState action,
            List<BattleEventState> events)
        {
            if (ContainsEvent(events, "VICTORY_SEQUENCE_STOP")) return;
            events.Add(Event(events.Count, round,
                "VICTORY_SEQUENCE_STOP", BattleSide.Player,
                source.UnionId, action.ActorMemberId, action.ArtId,
                "No living enemy Union remains. The pending offensive sequence stops immediately.",
                0, source.UnionId, action.ActorMemberId,
                string.Empty, string.Empty));
        }

        private static bool StopPlayerForecastIfVictory088(
            int round,
            int unionIndex,
            BattleForecastState forecast,
            List<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies,
            List<BattleEventState> events,
            M2CombatContent content)
        {
            if (!AllDefeated(enemies)) return false;
            RefundUnexecutedBaseArtAp090(
                players, unionIndex, forecast, content, 0,
                forecast.MemberActions.Count);
            var source = players[unionIndex];
            var pending = forecast.MemberActions.FirstOrDefault();
            if (pending != null)
                AddVictorySequenceStop088(round, source, pending, events);
            else if (!ContainsEvent(events, "VICTORY_SEQUENCE_STOP"))
                events.Add(Event(events.Count, round,
                    "VICTORY_SEQUENCE_STOP", BattleSide.Player,
                    source.UnionId, source.LeaderMemberId, forecast.CommandId,
                    "No living enemy Union remains. The pending Union sequence stops immediately.",
                    0, source.UnionId, source.LeaderMemberId,
                    string.Empty, string.Empty));
            players[unionIndex] = source.With(
                engagement: EngagementState.Engaged);
            return true;
        }

        private static void RefundUnexecutedBaseArtAp090(
            List<BattleUnionState> players,
            int sourceUnionIndex,
            BattleForecastState forecast,
            M2CombatContent content,
            int firstActionIndex,
            int endActionIndex)
        {
            var refund = 0;
            for (var index = firstActionIndex; index < endActionIndex; index++)
            {
                var action = forecast.MemberActions[index];
                if (!content.Arts.TryGetValue(action.ArtId, out var art)) continue;
                // Combined forecast actions may include an already-resolved Echo
                // or Covenant. Only this unexecuted base Art's canonical AP is
                // returned; invocation/command costs are never refundable here.
                refund = checked(refund + Math.Max(0,
                    Math.Min(action.SharedApCost, art.SharedApCost)));
            }
            if (refund <= 0) return;
            var source = players[sourceUnionIndex];
            players[sourceUnionIndex] = source.With(currentAp:
                Math.Min(source.MaximumAp, checked(source.CurrentAp + refund)));
        }

        private static bool TryPrepayInvocationPersonalMp088(
            List<BattleUnionState> players,
            int unionIndex,
            string invokerMemberId,
            int personalMpCost)
        {
            if (players == null || unionIndex < 0 || unionIndex >= players.Count ||
                string.IsNullOrWhiteSpace(invokerMemberId) || personalMpCost < 0)
                return false;
            var union = players[unionIndex];
            var memberIndex = union.FindMemberIndex(invokerMemberId);
            if (memberIndex < 0) return false;
            var member = union.Members[memberIndex];
            if (member.Downed || member.CurrentMp < personalMpCost) return false;
            SetMemberMp(players, unionIndex, memberIndex,
                member.CurrentMp - personalMpCost);
            return true;
        }

        private static int ResolveRestoration(int round, BattlePlannedActionState action,
            List<BattleUnionState> players, int sourceUnionIndex, M2CombatContent content,
            List<BattleEventState> events, BattleSide side = BattleSide.Player,
            int authorizedInvocationAp086 = 0,
            int authorizedInvocationMp086 = 0,
            BattleState battle = null)
        {
            content.Arts.TryGetValue(action.ArtId, out var art);
            var canCrossUnion = SupportsFriendlyUnionTarget080(art);
            var targetUnionIndex = FindUnionIndex(players, action.TargetUnionId);
            if (targetUnionIndex < 0 || players[targetUnionIndex].Retreated ||
                targetUnionIndex != sourceUnionIndex && !canCrossUnion)
                targetUnionIndex = sourceUnionIndex;
            var union = players[targetUnionIndex];
            if (!HasRestorationNeed086(union) && canCrossUnion)
            {
                var fallbackUnion = MostWoundedUnion(players);
                if (fallbackUnion != null)
                {
                    targetUnionIndex = FindUnionIndex(players, fallbackUnion.UnionId);
                    union = players[targetUnionIndex];
                }
            }
            var sourceUnion = players[sourceUnionIndex];
            var sourceMemberIndex = sourceUnion.FindMemberIndex(action.ActorMemberId);
            if (sourceMemberIndex < 0 || sourceUnion.Members[sourceMemberIndex].Downed) return 0;
            var sourceMember086 = sourceUnion.Members[sourceMemberIndex];
            if (!Contains(sourceMember086.LearnedArtIds, action.ArtId) ||
                !IsEquipmentLegal(art, sourceMember086.EquipmentTags) ||
                !HasExactRestorationCosts086(
                    action, art, authorizedInvocationAp086,
                    authorizedInvocationMp086))
                return 0;
            var projection086 = BuildRestorationProjection086(
                sourceMember086,
                union,
                art,
                null,
                EffectiveArtPowerPermille088(
                    battle, content, sourceMember086, art.Id));
            if (!projection086.Meaningful) return 0;

            var members = new List<BattleMemberState>(union.Members);
            var useful = 0;
            for (var healIndex086 = 0;
                 healIndex086 < projection086.MemberHealing.Count;
                 healIndex086++)
            {
                var projectedHeal086 = projection086.MemberHealing[healIndex086];
                var memberIndex086 = union.FindMemberIndex(projectedHeal086.MemberId);
                if (memberIndex086 < 0) continue;
                var target086 = members[memberIndex086];
                var actual086 = Math.Min(
                    projectedHeal086.Amount,
                    target086.Downed && projectedHeal086.Revives
                        ? target086.MaximumHp
                        : Math.Max(0, target086.MaximumHp - target086.CurrentHp));
                if (actual086 <= 0) continue;
                members[memberIndex086] = target086.With(
                    currentHp: target086.Downed && projectedHeal086.Revives
                        ? actual086
                        : target086.CurrentHp + actual086,
                    stabilized: false,
                    guarding: false);
                useful += actual086;
                events.Add(Event(events.Count, round,
                    projectedHeal086.Revives ? "REVIVED" : "RESTORATION", side,
                    union.UnionId, target086.MemberId, action.ArtId,
                    projectedHeal086.Revives
                        ? action.ActorName + " sends " + action.ArtName + " across the line and revives " +
                          target086.DisplayName + " in " + union.DisplayName + " with " + actual086 + " HP."
                        : action.ActorName + " sends " + action.ArtName +
                          " across the line to " + union.DisplayName +
                          ", restoring " + actual086 + " HP to " + target086.DisplayName + ".",
                    actual086, sourceUnion.UnionId, action.ActorMemberId,
                    union.UnionId, target086.MemberId));
            }

            if (!string.IsNullOrWhiteSpace(projection086.StabilizeMemberId))
            {
                var stabilizeIndex086 = union.FindMemberIndex(
                    projection086.StabilizeMemberId);
                if (stabilizeIndex086 >= 0 && members[stabilizeIndex086].Downed &&
                    !members[stabilizeIndex086].Stabilized)
                {
                    var target086 = members[stabilizeIndex086];
                    members[stabilizeIndex086] = target086.With(stabilized: true);
                    useful++;
                    events.Add(Event(events.Count, round, "STABILIZED", side,
                        union.UnionId, target086.MemberId, action.ArtId,
                        action.ActorName + " sends " + action.ArtName + " to " + union.DisplayName +
                        " and stabilizes Downed " + target086.DisplayName + ".",
                        0, sourceUnion.UnionId, action.ActorMemberId,
                        union.UnionId, target086.MemberId));
                }
            }

            var cohesionGain086 = Math.Min(
                projection086.CohesionGain, Math.Max(0, 100 - union.Cohesion));
            var formationGain086 = Math.Min(
                projection086.FormationGain,
                Math.Max(0, 10000 - union.FormationConditionBasisPoints));
            var nextEngagement086 = projection086.CleansesBrokenPressure ||
                                    projection086.ProtectsUnion
                ? EngagementState.Reinforcing
                : union.Engagement;
            var nextGuarding086 = projection086.ProtectsUnion || union.Guarding;
            union = union.With(
                members: members.AsReadOnly(),
                cohesion: union.Cohesion + cohesionGain086,
                formationConditionBasisPoints:
                    union.FormationConditionBasisPoints + formationGain086,
                engagement: nextEngagement086,
                guarding: nextGuarding086);
            players[targetUnionIndex] = union;
            useful += cohesionGain086 + formationGain086 / 100 +
                      (projection086.CleansesBrokenPressure ? 1 : 0) +
                      (projection086.ProtectsUnion ? 1 : 0);

            if (projection086.CleansesBrokenPressure)
                events.Add(Event(events.Count, round, "CLEANSED", side,
                    union.UnionId, union.LeaderMemberId, action.ArtId,
                    action.ActorName + " carries " + action.ArtName + " to " +
                    union.DisplayName + ", clearing its broken pressure and restoring " +
                    cohesionGain086 + " Cohesion.", cohesionGain086,
                    sourceUnion.UnionId, action.ActorMemberId,
                    union.UnionId, union.LeaderMemberId));
            if (projection086.ProtectsUnion)
                events.Add(Event(events.Count, round, "ALLY_PROTECTED", side,
                    union.UnionId, union.LeaderMemberId, action.ArtId,
                    action.ActorName + " projects " + action.ArtName + " over " +
                    union.DisplayName + "; the allied formation is protected.",
                    Math.Max(1, formationGain086 / 100),
                    sourceUnion.UnionId, action.ActorMemberId,
                    union.UnionId, union.LeaderMemberId));
            return useful;
        }

        private static int ResolveSssAlliedRestoration090(
            int round,
            BattlePlannedActionState action,
            List<BattleUnionState> players,
            int sourceUnionIndex,
            M2CombatContent content,
            List<BattleEventState> events,
            BattleSide side,
            int authorizedInvocationAp086,
            int authorizedInvocationMp086,
            BattleState battle)
        {
            if (action == null || content == null ||
                !content.Arts.TryGetValue(action.ArtId, out var art) ||
                !IsSssAllAlliedUnionScope090(art) ||
                sourceUnionIndex < 0 || sourceUnionIndex >= players.Count)
                return 0;

            var useful = 0;
            for (var targetIndex = 0; targetIndex < players.Count; targetIndex++)
            {
                var target = players[targetIndex];
                if (!IsActive(target)) continue;
                var source = players[sourceUnionIndex];
                var actorIndex = source.FindMemberIndex(action.ActorMemberId);
                if (actorIndex < 0 || source.Members[actorIndex].Downed)
                    break;
                var actor = source.Members[actorIndex];
                var projection = BuildRestorationProjection086(
                    actor,
                    target,
                    art,
                    null,
                    EffectiveArtPowerPermille088(
                        battle, content, actor, art.Id));
                if (!projection.Meaningful ||
                    IsWastefulMajorRestoration086(
                        target, art, projection, null))
                    continue;
                useful = checked(useful + ResolveRestoration(
                    round,
                    RetargetSssAreaAction090(
                        action,
                        target.UnionId,
                        projection.PrimaryTargetMemberId,
                        projection.TotalHp,
                        projection.CohesionGain,
                        projection.FormationGain,
                        projection.Prediction),
                    players,
                    sourceUnionIndex,
                    content,
                    events,
                    side,
                    authorizedInvocationAp086,
                    authorizedInvocationMp086,
                    battle));
            }
            return useful;
        }

        private static bool NeedsRestoration080(BattleMemberState member) =>
            member != null && (member.Downed || member.CurrentHp < member.MaximumHp);

        private sealed class RestorationMemberProjection086
        {
            public RestorationMemberProjection086(
                string memberId, int amount, bool revives)
            {
                MemberId = memberId ?? string.Empty;
                Amount = Math.Max(0, amount);
                Revives = revives;
            }

            public string MemberId { get; }
            public int Amount { get; }
            public bool Revives { get; }
        }

        private sealed class RestorationProjection086
        {
            public RestorationProjection086(
                string primaryTargetMemberId,
                IReadOnlyList<RestorationMemberProjection086> memberHealing,
                string stabilizeMemberId,
                int cohesionGain,
                int formationGain,
                bool cleansesBrokenPressure,
                bool protectsUnion,
                string prediction)
            {
                PrimaryTargetMemberId = primaryTargetMemberId ?? string.Empty;
                MemberHealing = memberHealing ??
                    Array.Empty<RestorationMemberProjection086>();
                StabilizeMemberId = stabilizeMemberId ?? string.Empty;
                CohesionGain = Math.Max(0, cohesionGain);
                FormationGain = Math.Max(0, formationGain);
                CleansesBrokenPressure = cleansesBrokenPressure;
                ProtectsUnion = protectsUnion;
                Prediction = prediction ?? string.Empty;
            }

            public string PrimaryTargetMemberId { get; }
            public IReadOnlyList<RestorationMemberProjection086> MemberHealing { get; }
            public string StabilizeMemberId { get; }
            public int CohesionGain { get; }
            public int FormationGain { get; }
            public bool CleansesBrokenPressure { get; }
            public bool ProtectsUnion { get; }
            public int TotalHp => MemberHealing.Sum(value => value.Amount);
            public bool Meaningful => TotalHp > 0 ||
                                      !string.IsNullOrWhiteSpace(StabilizeMemberId) ||
                                      CohesionGain > 0 || FormationGain > 0 ||
                                      CleansesBrokenPressure || ProtectsUnion;
            public string Prediction { get; }
        }

        private static Dictionary<string, int> CreateProjectedHp086(
            BattleUnionState union)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (union == null) return result;
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                result[union.Members[memberIndex].MemberId] =
                    union.Members[memberIndex].CurrentHp;
            return result;
        }

        private static BattleMemberState FindMember086(
            BattleUnionState union,
            string memberId)
        {
            if (union == null || string.IsNullOrWhiteSpace(memberId)) return null;
            var memberIndex = union.FindMemberIndex(memberId);
            return memberIndex < 0 ? null : union.Members[memberIndex];
        }

        private static void ApplyProjectedRestoration086(
            IDictionary<string, int> projectedHp,
            RestorationProjection086 projection)
        {
            if (projectedHp == null || projection == null) return;
            for (var index = 0; index < projection.MemberHealing.Count; index++)
            {
                var healing = projection.MemberHealing[index];
                projectedHp.TryGetValue(healing.MemberId, out var currentHp);
                projectedHp[healing.MemberId] = checked(currentHp + healing.Amount);
            }
        }

        private static RestorationProjection086 BuildRestorationProjection086(
            BattleMemberState actor,
            BattleUnionState targetUnion,
            M2ArtDefinition art,
            IReadOnlyDictionary<string, int> projectedHp,
            int artPowerPermille088 = M2ArtMasteryLevelPolicy088.BasePowerPermille)
        {
            if (actor == null || targetUnion == null || art == null ||
                !StringComparer.Ordinal.Equals(art.Discipline, "Restoration"))
                return EmptyRestorationProjection086();

            var healing = new List<RestorationMemberProjection086>();
            var downed = new List<BattleMemberState>();
            var wounded = new List<BattleMemberState>();
            for (var memberIndex = 0; memberIndex < targetUnion.Members.Count; memberIndex++)
            {
                var member = targetUnion.Members[memberIndex];
                var currentHp = ProjectedHp086(member, projectedHp);
                if (currentHp <= 0) downed.Add(member);
                else if (currentHp < member.MaximumHp) wounded.Add(member);
            }
            downed.Sort(CompareRestorationTarget086);
            wounded.Sort((left, right) =>
            {
                var leftHp = ProjectedHp086(left, projectedHp);
                var rightHp = ProjectedHp086(right, projectedHp);
                var percentage = ((long)leftHp * right.MaximumHp).CompareTo(
                    (long)rightHp * left.MaximumHp);
                return percentage != 0
                    ? percentage
                    : StringComparer.Ordinal.Compare(left.MemberId, right.MemberId);
            });

            var baseHealing = Math.Max(
                1,
                checked(18 + actor.MagicAttack / 2) *
                Math.Max(1, art.PowerCoefficientPermille) / 1000);
            baseHealing = M2ArtMasteryLevelPolicy088.ScaleMagnitudeByPowerPermille(
                baseHealing,
                artPowerPermille088);
            var revival = IsRevivalArt080(art);
            var restoresHp = IsHealingArt086(art);
            var groupHealing = IsGroupRestorationArt086(art);
            var primaryTargetId = string.Empty;
            var stabilizeMemberId = string.Empty;
            if (revival && downed.Count > 0)
            {
                var target = downed[0];
                var amount = Math.Min(target.MaximumHp, baseHealing);
                healing.Add(new RestorationMemberProjection086(
                    target.MemberId, amount, true));
                primaryTargetId = target.MemberId;
            }
            else if (!revival && restoresHp && wounded.Count > 0)
            {
                var targetCount = groupHealing ? wounded.Count : 1;
                var perTargetHealing = groupHealing
                    ? Math.Max(1, baseHealing * 3 / 4)
                    : baseHealing;
                for (var index = 0; index < targetCount; index++)
                {
                    var target = wounded[index];
                    var currentHp = ProjectedHp086(target, projectedHp);
                    var amount = Math.Min(
                        Math.Max(0, target.MaximumHp - currentHp),
                        perTargetHealing);
                    if (amount <= 0) continue;
                    healing.Add(new RestorationMemberProjection086(
                        target.MemberId, amount, false));
                    if (string.IsNullOrWhiteSpace(primaryTargetId))
                        primaryTargetId = target.MemberId;
                }
            }
            else if (!revival && restoresHp && downed.Count > 0 &&
                     !IsCleanseArt086(art) && !IsProtectionArt086(art))
            {
                stabilizeMemberId = downed[0].MemberId;
                primaryTargetId = stabilizeMemberId;
            }

            var cleansesBroken = IsCleanseArt086(art) &&
                                  targetUnion.Engagement == EngagementState.Broken;
            var protectsUnion = IsProtectionArt086(art) &&
                                (!targetUnion.Guarding ||
                                 targetUnion.Engagement == EngagementState.Broken ||
                                 targetUnion.FormationConditionBasisPoints < 10000);
            var rawCohesion = HasAnyTag080(
                    art, "COHESION_RESTORE", "RESTORE_COHESION")
                ? 18
                : HasAnyTag080(art, "MORALE_UP", "MINOR_MORALE", "CLEAR_FEAR")
                    ? 10
                    : healing.Count > 0 || cleansesBroken ? 3 : 0;
            var rawFormation = protectsUnion
                ? HasAnyTag080(art, "AREA_PROTECTION") ? 1400 : 900
                : cleansesBroken ? 500 : 0;
            var cohesionGain = Math.Min(
                rawCohesion, Math.Max(0, 100 - targetUnion.Cohesion));
            var formationGain = Math.Min(
                rawFormation,
                Math.Max(0, 10000 - targetUnion.FormationConditionBasisPoints));

            var prediction = RestorationPrediction086(
                targetUnion,
                healing,
                stabilizeMemberId,
                cohesionGain,
                formationGain,
                cleansesBroken,
                protectsUnion);
            return new RestorationProjection086(
                primaryTargetId,
                healing.AsReadOnly(),
                stabilizeMemberId,
                cohesionGain,
                formationGain,
                cleansesBroken,
                protectsUnion,
                prediction);
        }

        private static RestorationProjection086 EmptyRestorationProjection086() =>
            new RestorationProjection086(
                string.Empty,
                Array.Empty<RestorationMemberProjection086>(),
                string.Empty,
                0,
                0,
                false,
                false,
                "No legal wounded target.");

        private static int ProjectedHp086(
            BattleMemberState member,
            IReadOnlyDictionary<string, int> projectedHp)
        {
            if (member == null) return 0;
            return projectedHp != null &&
                   projectedHp.TryGetValue(member.MemberId, out var value)
                ? Math.Max(0, Math.Min(member.MaximumHp, value))
                : member.CurrentHp;
        }

        private static int CompareRestorationTarget086(
            BattleMemberState left,
            BattleMemberState right)
        {
            var maximumHp = right.MaximumHp.CompareTo(left.MaximumHp);
            return maximumHp != 0
                ? maximumHp
                : StringComparer.Ordinal.Compare(left.MemberId, right.MemberId);
        }

        private static string RestorationPrediction086(
            BattleUnionState targetUnion,
            IReadOnlyList<RestorationMemberProjection086> healing,
            string stabilizeMemberId,
            int cohesionGain,
            int formationGain,
            bool cleansesBroken,
            bool protectsUnion)
        {
            var effects = new List<string>();
            for (var index = 0; index < healing.Count; index++)
            {
                var target = FindMember086(targetUnion, healing[index].MemberId);
                effects.Add(
                    (healing[index].Revives ? "revive " : "heal ") +
                    (target?.DisplayName ?? healing[index].MemberId) +
                    " " + healing[index].Amount + " HP");
            }
            if (!string.IsNullOrWhiteSpace(stabilizeMemberId))
            {
                var target = FindMember086(targetUnion, stabilizeMemberId);
                effects.Add("stabilize " +
                            (target?.DisplayName ?? stabilizeMemberId));
            }
            if (cohesionGain > 0) effects.Add("Cohesion +" + cohesionGain);
            if (formationGain > 0)
                effects.Add("formation +" + formationGain / 100 + "%");
            if (cleansesBroken) effects.Add("clear Broken pressure");
            if (protectsUnion) effects.Add("protect the Union");
            return effects.Count == 0
                ? "No legal wounded target."
                : "Help " + targetUnion.DisplayName + ": " +
                  string.Join(", ", effects) + ".";
        }

        private static bool IsGroupRestorationArt086(M2ArtDefinition art) =>
            art != null &&
            (HasAnyTag080(art, "MASS_HEAL", "AREA_HEAL") ||
             art.Name.IndexOf("group", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("everyone", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("all home", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("dawn without loss", StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool IsHealingArt086(M2ArtDefinition art) =>
            art != null &&
            (HasAnyTag080(
                 art, "HEAL", "MASS_HEAL", "AREA_HEAL", "EMERGENCY_HEAL",
                 "HEAL_OR_CLEANSE") ||
             art.Name.IndexOf("remedy", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("mend", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("mercy", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("dawn without loss", StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool IsCleanseArt086(M2ArtDefinition art) =>
            art != null &&
            (HasAnyTag080(art, "CLEANSE", "AREA_CLEANSE", "CLEAR_FEAR") ||
             art.Name.IndexOf("cleanse", StringComparison.OrdinalIgnoreCase) >= 0 ||
             art.Name.IndexOf("purifying", StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool IsProtectionArt086(M2ArtDefinition art) =>
            art != null && HasAnyTag080(
                art, "BARRIER", "MAGIC_BARRIER", "AREA_PROTECTION",
                "INTERCEPT_SUPPORT", "PROTECT", "BUFF_ALLY");

        private static bool IsWastefulMajorRestoration086(
            BattleUnionState targetUnion,
            M2ArtDefinition art,
            RestorationProjection086 projection,
            IReadOnlyDictionary<string, int> projectedHp)
        {
            if (targetUnion == null || art == null || projection == null)
                return false;
            var major = IsGroupRestorationArt086(art) ||
                        art.SharedApCost >= 6 || art.PersonalMpCost >= 12;
            if (!major || projection.TotalHp <= 0 ||
                IsCriticalRestorationTarget086(targetUnion, projectedHp) ||
                HasDownedMember086(targetUnion, projectedHp) ||
                !string.IsNullOrWhiteSpace(projection.StabilizeMemberId) ||
                projection.CleansesBrokenPressure || projection.ProtectsUnion ||
                projection.CohesionGain >= 10 || projection.FormationGain >= 700)
                return false;

            var maximumHp = 0;
            for (var memberIndex = 0;
                 memberIndex < targetUnion.Members.Count;
                 memberIndex++)
                maximumHp = checked(
                    maximumHp + targetUnion.Members[memberIndex].MaximumHp);
            var trivialHealingThreshold = Math.Max(1, maximumHp / 20);
            return projection.TotalHp <= trivialHealingThreshold;
        }

        private static bool TryPrepareRestorationAction086(
            BattlePlannedActionState action,
            IReadOnlyList<BattleUnionState> friendlyUnions,
            int sourceUnionIndex,
            M2CombatContent content,
            BattleState battle,
            int authorizedInvocationAp086,
            int authorizedInvocationMp086,
            out BattlePlannedActionState preparedAction,
            out BattleUnionState preparedTarget,
            out bool retargeted)
        {
            preparedAction = action;
            preparedTarget = null;
            retargeted = false;
            if (action == null || friendlyUnions == null || content == null ||
                sourceUnionIndex < 0 || sourceUnionIndex >= friendlyUnions.Count ||
                action.Kind != BattleActionKind.Restoration ||
                !content.Arts.TryGetValue(action.ArtId, out var art))
                return false;

            var source = friendlyUnions[sourceUnionIndex];
            var actorIndex = source.FindMemberIndex(action.ActorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed)
                return false;
            var actor = source.Members[actorIndex];
            if (!Contains(actor.LearnedArtIds, art.Id) ||
                !IsEquipmentLegal(art, actor.EquipmentTags) ||
                !HasExactRestorationCosts086(
                    action, art, authorizedInvocationAp086,
                    authorizedInvocationMp086))
                return false;

            var canCrossUnion = SupportsFriendlyUnionTarget080(art);
            var originalTargetIndex = FindUnionIndex(
                friendlyUnions, action.TargetUnionId);
            if (originalTargetIndex >= 0)
            {
                var originalTarget = friendlyUnions[originalTargetIndex];
                if (!originalTarget.Retreated &&
                    (originalTargetIndex == sourceUnionIndex || canCrossUnion))
                {
                    var originalProjection = BuildRestorationProjection086(
                        actor,
                        originalTarget,
                        art,
                        null,
                        EffectiveArtPowerPermille088(
                            battle, content, actor, art.Id));
                    if (originalProjection.Meaningful &&
                        !IsWastefulMajorRestoration086(
                            originalTarget, art, originalProjection, null))
                    {
                        preparedTarget = originalTarget;
                        preparedAction = PreparedRestorationAction086(
                            action, originalTarget, originalProjection);
                        retargeted = !StringComparer.Ordinal.Equals(
                            action.TargetMemberId,
                            preparedAction.TargetMemberId);
                        return true;
                    }
                }
            }

            RestorationProjection086 bestProjection = null;
            BattleUnionState bestTarget = null;
            long bestScore = long.MinValue;
            for (var targetIndex = 0;
                 targetIndex < friendlyUnions.Count;
                 targetIndex++)
            {
                var candidate = friendlyUnions[targetIndex];
                if (candidate.Retreated ||
                    targetIndex != sourceUnionIndex && !canCrossUnion)
                    continue;
                var projection = BuildRestorationProjection086(
                    actor,
                    candidate,
                    art,
                    null,
                    EffectiveArtPowerPermille088(
                        battle, content, actor, art.Id));
                if (!projection.Meaningful ||
                    IsWastefulMajorRestoration086(
                        candidate, art, projection, null))
                    continue;
                var score = RestorationRetargetScore086(
                    candidate, projection);
                if (score > bestScore || score == bestScore &&
                    bestTarget != null && StringComparer.Ordinal.Compare(
                        candidate.UnionId, bestTarget.UnionId) < 0)
                {
                    bestTarget = candidate;
                    bestProjection = projection;
                    bestScore = score;
                }
            }
            if (bestTarget == null) return false;

            preparedTarget = bestTarget;
            preparedAction = PreparedRestorationAction086(
                action, bestTarget, bestProjection);
            retargeted = !StringComparer.Ordinal.Equals(
                             action.TargetUnionId,
                             preparedAction.TargetUnionId) ||
                         !StringComparer.Ordinal.Equals(
                             action.TargetMemberId,
                             preparedAction.TargetMemberId);
            return true;
        }

        private static bool HasExactRestorationCosts086(
            BattlePlannedActionState action,
            M2ArtDefinition art,
            int authorizedInvocationAp086,
            int authorizedInvocationMp086)
        {
            if (action == null || art == null ||
                authorizedInvocationAp086 < 0 || authorizedInvocationMp086 < 0)
                return false;
            return (long)action.SharedApCost ==
                       (long)art.SharedApCost + authorizedInvocationAp086 &&
                   (long)action.PersonalMpCost ==
                       (long)art.PersonalMpCost + authorizedInvocationMp086;
        }

        private static long RestorationRetargetScore086(
            BattleUnionState target,
            RestorationProjection086 projection)
        {
            var score = (long)projection.TotalHp * 1000L +
                        (long)projection.CohesionGain * 100L +
                        projection.FormationGain;
            if (HasDownedMember086(target)) score += 1000000000L;
            if (IsCriticalRestorationTarget086(target)) score += 100000000L;
            if (projection.CleansesBrokenPressure) score += 50000000L;
            if (projection.ProtectsUnion) score += 10000000L;
            if (!string.IsNullOrWhiteSpace(projection.StabilizeMemberId))
                score += 250000000L;
            return score;
        }

        private static BattlePlannedActionState PreparedRestorationAction086(
            BattlePlannedActionState source,
            BattleUnionState target,
            RestorationProjection086 projection)
        {
            var targetMember = FindMember086(
                target, projection.PrimaryTargetMemberId);
            return new BattlePlannedActionState(
                source.ActorMemberId,
                source.ActorName,
                target.UnionId,
                targetMember?.MemberId ?? string.Empty,
                source.ArtId,
                source.ArtName,
                source.Kind,
                source.SharedApCost,
                source.PersonalMpCost,
                projection.TotalHp,
                projection.CohesionGain,
                projection.FormationGain,
                projection.Prediction,
                projection.Meaningful,
                source.BreakthroughOpportunity,
                source.AnimationTag,
                source.Discipline,
                source.PredictedGrowth,
                source.BreakthroughTargetArtId,
                source.BreakthroughTargetArtName);
        }

        private static string SelectRestorationArtForTarget086(
            BattleMemberState member,
            BattleUnionState sourceUnion,
            BattleUnionState targetUnion,
            M2CombatContent content,
            int apRemaining,
            IReadOnlyDictionary<string, int> projectedHp,
            BattleState battle = null)
        {
            if (member == null || member.Downed || sourceUnion == null ||
                targetUnion == null || content == null)
                return null;
            var crossUnion = !StringComparer.Ordinal.Equals(
                sourceUnion.UnionId, targetUnion.UnionId);
            M2ArtDefinition best = null;
            var bestScore = int.MinValue;
            for (var artIndex = 0; artIndex < member.LearnedArtIds.Count; artIndex++)
            {
                if (!content.Arts.TryGetValue(
                        member.LearnedArtIds[artIndex], out var art) ||
                    !art.IsForecastAction ||
                    !StringComparer.Ordinal.Equals(art.Discipline, "Restoration") ||
                    art.SharedApCost > apRemaining ||
                    art.PersonalMpCost > member.CurrentMp ||
                    !IsEquipmentLegal(art, member.EquipmentTags) ||
                    crossUnion && !SupportsFriendlyUnionTarget080(art))
                    continue;

                var projection = BuildRestorationProjection086(
                    member,
                    targetUnion,
                    art,
                    projectedHp,
                    EffectiveArtPowerPermille088(
                        battle, content, member, art.Id));
                if (!projection.Meaningful) continue;
                if (IsWastefulMajorRestoration086(
                        targetUnion, art, projection, projectedHp))
                    continue;
                var hasDowned = HasDownedMember086(targetUnion, projectedHp);
                var criticalTarget = IsCriticalRestorationTarget086(
                    targetUnion, projectedHp);
                if (IsRevivalArt080(art) && !hasDowned) continue;
                var score = checked(projection.TotalHp * 100 +
                    (criticalTarget && projection.TotalHp > 0 ? 50000 : 0) +
                    projection.CohesionGain * 40 +
                    projection.FormationGain / 2 +
                    (projection.CleansesBrokenPressure ? 30000 : 0) +
                    (projection.ProtectsUnion ? 5000 : 0) +
                    (IsRevivalArt080(art) && hasDowned ? 1000000 : 0) +
                    (!string.IsNullOrWhiteSpace(projection.StabilizeMemberId)
                        ? 100000
                        : 0) -
                    art.SharedApCost * 120 -
                    art.PersonalMpCost * 45);
                if (score > bestScore || score == bestScore && best != null &&
                    StringComparer.Ordinal.Compare(art.Id, best.Id) < 0)
                {
                    best = art;
                    bestScore = score;
                }
            }
            return best?.Id;
        }

        private static bool HasContextualRestorationArt086(
            BattleUnionState sourceUnion,
            BattleUnionState targetUnion,
            M2CombatContent content,
            int sharedAp)
        {
            if (sourceUnion == null || targetUnion == null) return false;
            var projectedHp = CreateProjectedHp086(targetUnion);
            for (var memberIndex = 0;
                 memberIndex < sourceUnion.Members.Count;
                 memberIndex++)
            {
                var member = sourceUnion.Members[memberIndex];
                var artId = SelectRestorationArtForTarget086(
                    member, sourceUnion, targetUnion, content, sharedAp,
                    projectedHp);
                if (!string.IsNullOrWhiteSpace(artId)) return true;
            }
            return false;
        }

        private static bool HasRestorationNeed086(BattleUnionState union)
        {
            if (union == null || union.Retreated) return false;
            if (union.Engagement == EngagementState.Broken ||
                union.Cohesion < 100 ||
                union.FormationConditionBasisPoints < 10000)
                return true;
            for (var index = 0; index < union.Members.Count; index++)
                if (NeedsRestoration080(union.Members[index])) return true;
            return false;
        }

        private static bool HasDownedMember086(
            BattleUnionState union,
            IReadOnlyDictionary<string, int> projectedHp = null)
        {
            if (union == null) return false;
            for (var index = 0; index < union.Members.Count; index++)
                if (ProjectedHp086(union.Members[index], projectedHp) <= 0)
                    return true;
            return false;
        }

        private static bool IsCriticalRestorationTarget086(
            BattleUnionState union,
            IReadOnlyDictionary<string, int> projectedHp = null)
        {
            if (union == null || union.Retreated) return false;
            if (HasDownedMember086(union, projectedHp)) return true;
            var currentHp = 0;
            var maximumHp = 0;
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                var memberHp = ProjectedHp086(member, projectedHp);
                currentHp = checked(currentHp + memberHp);
                maximumHp = checked(maximumHp + member.MaximumHp);
                if (member.MaximumHp > 0 &&
                    memberHp * 100 / member.MaximumHp <=
                    CrossUnionCriticalHpPercent086)
                    return true;
            }
            return maximumHp > 0 &&
                   currentHp * 100 / maximumHp <=
                   CrossUnionCriticalHpPercent086;
        }

        private static bool IsLowRestorationTarget086(BattleUnionState union)
        {
            if (union == null || union.Retreated) return false;
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                if (member.Downed || member.MaximumHp > 0 &&
                    member.CurrentHp * 100 / member.MaximumHp <=
                    CrossUnionLowHpPercent086)
                    return true;
            }
            return AverageHpPercent(union) <= CrossUnionLowHpPercent086;
        }

        private static bool IsCriticalSupportTarget086(BattleUnionState union) =>
            union != null && !union.Retreated &&
            (union.Cohesion <= CrossUnionCriticalCohesion086 ||
             union.FormationConditionBasisPoints <=
             CrossUnionCriticalFormationBasisPoints086 ||
             union.Engagement == EngagementState.Broken);

        private static int PredictedSupportCohesion086(
            BattleUnionState targetUnion,
            M2ArtDefinition art)
        {
            if (targetUnion == null || art == null) return 0;
            var raw = HasAnyTag080(
                    art, "COHESION_RESTORE", "RESTORE_COHESION")
                ? 18
                : HasAnyTag080(art, "MORALE_UP", "MINOR_MORALE", "CLEAR_FEAR")
                    ? 12
                    : 8;
            return Math.Min(raw, Math.Max(0, 100 - targetUnion.Cohesion));
        }

        private static int PredictedSupportFormation086(
            BattleUnionState targetUnion,
            M2ArtDefinition art)
        {
            if (targetUnion == null || art == null) return 0;
            var raw = HasAnyTag080(art, "AREA_PROTECTION")
                ? 1400
                : HasAnyTag080(art, "REPOSITION")
                    ? 1200
                    : IsProtectionArt086(art) ? 900 : 700;
            return Math.Min(
                raw,
                Math.Max(0, 10000 -
                            targetUnion.FormationConditionBasisPoints));
        }

        private static string SupportPrediction086(
            BattleUnionState targetUnion,
            M2ArtDefinition art,
            int cohesionGain,
            int formationGain)
        {
            var effects = new List<string>();
            if (cohesionGain > 0) effects.Add("Cohesion +" + cohesionGain);
            if (formationGain > 0)
                effects.Add("formation +" + formationGain / 100 + "%");
            if (targetUnion != null &&
                targetUnion.Engagement == EngagementState.Broken &&
                IsCleanseArt086(art))
                effects.Add("clear Broken pressure");
            if (IsProtectionArt086(art)) effects.Add("protect the Union");
            return "Support " + (targetUnion?.DisplayName ?? "allied Union") +
                   ": " + (effects.Count == 0
                       ? "no missing support resource"
                       : string.Join(", ", effects)) + ".";
        }

        private static bool IsMeaningfulFriendlySupport086(
            BattleUnionState targetUnion,
            M2ArtDefinition art,
            int cohesionGain,
            int formationGain) =>
            targetUnion != null && art != null &&
            (cohesionGain > 0 || formationGain > 0 ||
             targetUnion.Engagement == EngagementState.Broken &&
             IsCleanseArt086(art) ||
             !targetUnion.Guarding && IsProtectionArt086(art));

        private static int ResolveFriendlySupportArt086(
            int round,
            BattlePlannedActionState action,
            List<BattleUnionState> friendlyUnions,
            int sourceUnionIndex,
            M2ArtDefinition art,
            List<BattleEventState> events,
            BattleSide side,
            int authorizedInvocationAp086 = 0,
            int authorizedInvocationMp086 = 0)
        {
            if (action == null || art == null || sourceUnionIndex < 0 ||
                sourceUnionIndex >= friendlyUnions.Count ||
                !SupportsFriendlyUnionTarget080(art))
                return 0;
            var source = friendlyUnions[sourceUnionIndex];
            var actorIndex = source.FindMemberIndex(action.ActorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed ||
                !Contains(source.Members[actorIndex].LearnedArtIds, art.Id) ||
                !IsEquipmentLegal(art, source.Members[actorIndex].EquipmentTags) ||
                (long)action.SharedApCost !=
                    (long)art.SharedApCost + authorizedInvocationAp086 ||
                (long)action.PersonalMpCost !=
                    (long)art.PersonalMpCost + authorizedInvocationMp086)
                return 0;
            var targetIndex = FindUnionIndex(
                friendlyUnions, action.TargetUnionId);
            if (targetIndex < 0 || !IsActive(friendlyUnions[targetIndex]))
            {
                var fallback = MostSupportNeedyUnion(friendlyUnions);
                targetIndex = fallback == null
                    ? sourceUnionIndex
                    : FindUnionIndex(friendlyUnions, fallback.UnionId);
            }
            var target = friendlyUnions[targetIndex];
            var cohesionGain = PredictedSupportCohesion086(target, art);
            var formationGain = PredictedSupportFormation086(target, art);
            var cleansesBroken = target.Engagement == EngagementState.Broken &&
                                  IsCleanseArt086(art);
            var protects = IsProtectionArt086(art) && !target.Guarding;
            if (cohesionGain <= 0 && formationGain <= 0 &&
                !cleansesBroken && !protects)
                return 0;

            target = target.With(
                cohesion: target.Cohesion + cohesionGain,
                formationConditionBasisPoints:
                    target.FormationConditionBasisPoints + formationGain,
                engagement: cleansesBroken || protects
                    ? EngagementState.Reinforcing
                    : target.Engagement,
                guarding: target.Guarding || protects);
            friendlyUnions[targetIndex] = target;
            var useful = cohesionGain + formationGain / 100 +
                         (cleansesBroken ? 1 : 0) + (protects ? 1 : 0);
            events.Add(Event(events.Count, round, "ALLY_SUPPORT", side,
                target.UnionId, target.LeaderMemberId, art.Id,
                action.ActorName + " sends " + art.Name + " across the line to " +
                target.DisplayName + ": Cohesion +" + cohesionGain +
                ", formation +" + formationGain / 100 + "%.", useful,
                source.UnionId, action.ActorMemberId,
                target.UnionId, target.LeaderMemberId));
            if (cleansesBroken)
                events.Add(Event(events.Count, round, "CLEANSED", side,
                    target.UnionId, target.LeaderMemberId, art.Id,
                    action.ActorName + " clears Broken pressure from " +
                    target.DisplayName + ".", cohesionGain,
                    source.UnionId, action.ActorMemberId,
                    target.UnionId, target.LeaderMemberId));
            if (protects)
                events.Add(Event(events.Count, round, "ALLY_PROTECTED", side,
                    target.UnionId, target.LeaderMemberId, art.Id,
                    action.ActorName + " protects " + target.DisplayName +
                    " with " + art.Name + ".", Math.Max(1, formationGain / 100),
                    source.UnionId, action.ActorMemberId,
                    target.UnionId, target.LeaderMemberId));
            return useful;
        }

        private static int ResolveSssAlliedSupport090(
            int round,
            BattlePlannedActionState action,
            List<BattleUnionState> friendlyUnions,
            int sourceUnionIndex,
            M2ArtDefinition art,
            List<BattleEventState> events,
            BattleSide side,
            int authorizedInvocationAp086,
            int authorizedInvocationMp086)
        {
            if (action == null || art == null ||
                !IsSssAllAlliedUnionScope090(art) ||
                sourceUnionIndex < 0 ||
                sourceUnionIndex >= friendlyUnions.Count)
                return 0;

            var useful = 0;
            for (var targetIndex = 0;
                 targetIndex < friendlyUnions.Count;
                 targetIndex++)
            {
                var target = friendlyUnions[targetIndex];
                if (!IsActive(target)) continue;
                var cohesion = PredictedSupportCohesion086(target, art);
                var formation = PredictedSupportFormation086(target, art);
                if (!IsMeaningfulFriendlySupport086(
                        target, art, cohesion, formation))
                    continue;
                useful = checked(useful + ResolveFriendlySupportArt086(
                    round,
                    RetargetSssAreaAction090(
                        action,
                        target.UnionId,
                        target.LeaderMemberId,
                        0,
                        cohesion,
                        formation,
                        SupportPrediction086(
                            target, art, cohesion, formation)),
                    friendlyUnions,
                    sourceUnionIndex,
                    art,
                    events,
                    side,
                    authorizedInvocationAp086,
                    authorizedInvocationMp086));
            }
            return useful;
        }

        private static int ResolveSupportTargetIndex080(
            BattleForecastState forecast,
            List<BattleUnionState> players,
            int sourceUnionIndex,
            M2CombatContent content)
        {
            var canCrossUnion = false;
            for (var actionIndex = 0; actionIndex < forecast.MemberActions.Count; actionIndex++)
            {
                if (content.Arts.TryGetValue(forecast.MemberActions[actionIndex].ArtId, out var art) &&
                    (StringComparer.Ordinal.Equals(art.Discipline, "Support") ||
                     StringComparer.Ordinal.Equals(art.Discipline, "Recovery")) &&
                    SupportsFriendlyUnionTarget080(art))
                {
                    canCrossUnion = true;
                    break;
                }
            }
            if (!canCrossUnion) return sourceUnionIndex;

            var targetIndex = FindUnionIndex(players, forecast.TargetId);
            if (targetIndex >= 0 && IsActive(players[targetIndex]) && SupportNeed080(players[targetIndex]) > 0)
                return targetIndex;
            var fallback = MostSupportNeedyUnion(players);
            return fallback == null ? sourceUnionIndex : FindUnionIndex(players, fallback.UnionId);
        }

        private static int SupportNeed080(BattleUnionState union) =>
            union == null ? 0 :
                (union.Engagement == EngagementState.Broken ? 2000000 : 0) +
                Math.Max(0, 100 - union.Cohesion) * 10000 +
                Math.Max(0, 10000 - union.FormationConditionBasisPoints);

        private static bool SupportCommandEligible080(BattleUnionState union) =>
            union != null &&
            (union.Engagement == EngagementState.Broken ||
             union.Cohesion < 90 ||
             union.FormationConditionBasisPoints < 9000);

        private static void ApplyActionProgress(
            CampaignState campaign,
            BattleState battle,
            M2CombatContent content,
            ModeRuleSnapshot rules,
            int unionIndex,
            BattlePlannedActionState action,
            bool meaningful,
            int usefulMagnitude,
            List<BattleUnionState> players,
            List<BattleEventState> events)
        {
            if (!meaningful || unionIndex < 0 || unionIndex >= players.Count) return;
            var union = players[unionIndex];
            var memberIndex = union.FindMemberIndex(action.ActorMemberId);
            if (memberIndex < 0) return;
            var members = new List<BattleMemberState>(union.Members);
            var actor = members[memberIndex];
            var hpDelta = action.Kind == BattleActionKind.Restoration
                ? Math.Max(1, usefulMagnitude)
                : action.Kind == BattleActionKind.Martial || action.Kind == BattleActionKind.Mystic || action.Kind == BattleActionKind.Tactical
                    ? -Math.Max(1, usefulMagnitude)
                    : 0;
            var gain = ScaleGrowth(
                M2MeaningfulUse.PersonalProgressGain(action.Kind, true, hpDelta),
                rules.ArtGrowthPct);
            var actionDiscipline = !string.IsNullOrWhiteSpace(action.Discipline)
                ? action.Discipline
                : content.Arts.TryGetValue(action.ArtId, out var resolvedArt) ? resolvedArt.Discipline : string.Empty;
            var progress = AddMeaningfulArtUse(actor.ArtProgress, action.ArtId, actionDiscipline, gain);
            var artProgress = FindArtProgress(progress, action.ArtId);
            var learned = new List<string>(actor.LearnedArtIds);
            var discovery = actor.DiscoveryProgress;

            events.Add(Event(events.Count, battle.Round, "ART_GROWTH", BattleSide.Player,
                union.UnionId, actor.MemberId, action.ArtId,
                actor.DisplayName + " grows " + action.ArtName + " through meaningful use: +" + gain +
                " mastery (" + artProgress.MeaningfulUses + " meaningful use" +
                (artProgress.MeaningfulUses == 1 ? string.Empty : "s") + ", " + artProgress.MasteryPoints + " mastery).",
                gain, union.UnionId, actor.MemberId, union.UnionId, actor.MemberId));

            M2ArtDefinition targetArt = null;
            var tutorialBreakthrough070 = action.BreakthroughOpportunity &&
                !string.IsNullOrWhiteSpace(action.BreakthroughTargetArtId) &&
                StringComparer.Ordinal.Equals(action.BreakthroughTargetArtId, battle.TutorialBreakthroughArtId) &&
                StringComparer.Ordinal.Equals(action.BreakthroughTargetArtId, actor.BreakthroughArtId) &&
                !StringComparer.Ordinal.Equals(action.ArtId, action.BreakthroughTargetArtId) &&
                content.Arts.TryGetValue(action.BreakthroughTargetArtId, out targetArt) &&
                DisciplinesAreCompatibleForDiscovery(actionDiscipline, targetArt.Discipline);
            if (tutorialBreakthrough070)
            {
                discovery = Math.Min(100, discovery + 10);
                if (discovery >= content.GuaranteedBreakthroughThreshold && !learned.Contains(targetArt.Id))
                {
                    learned.Add(targetArt.Id);
                    progress = EnsureArtProgress(progress, targetArt.Id, targetArt.Discipline);
                    events.Add(Event(events.Count, battle.Round, "BREAKTHROUGH", BattleSide.Player,
                        union.UnionId, actor.MemberId, targetArt.Id,
                        actor.DisplayName + " learned " + targetArt.Name + " through " +
                        action.ArtName + ". Ready for future battles.", discovery,
                        union.UnionId, actor.MemberId, union.UnionId, actor.MemberId));
                }
            }
            else if (action.BreakthroughOpportunity &&
                     !string.IsNullOrWhiteSpace(action.BreakthroughTargetArtId) &&
                     !learned.Contains(action.BreakthroughTargetArtId))
            {
                var recruit070 = FindRecruit(campaign.Guild.Recruits, actor.MemberId);
                var progressedActor070 = actor.With(
                    learnedArtIds: learned.AsReadOnly(),
                    artProgress: progress);
                if (M2DeepArtRuntime070.TryGetNextLearning(
                        recruit070,
                        progressedActor070,
                        action.ArtId,
                        0,
                        content,
                        out var opportunity070) &&
                    opportunity070.CanLearnNow &&
                    StringComparer.Ordinal.Equals(
                        opportunity070.TargetArtId,
                        action.BreakthroughTargetArtId) &&
                    content.Arts.TryGetValue(opportunity070.TargetArtId, out var learnedArt070))
                {
                    learned.Add(learnedArt070.Id);
                    progress = EnsureArtProgress(progress, learnedArt070.Id, learnedArt070.Discipline);
                    events.Add(Event(events.Count, battle.Round, "BREAKTHROUGH", BattleSide.Player,
                        union.UnionId, actor.MemberId, learnedArt070.Id,
                        actor.DisplayName + " learned " + learnedArt070.Name + " through " +
                        action.ArtName + ". Ready for future battles.",
                        opportunity070.CurrentPoints,
                        union.UnionId, actor.MemberId, union.UnionId, actor.MemberId));
                }
            }

            var earlyMedicActor091 = actor.With(learnedArtIds: learned.AsReadOnly(), artProgress: progress);
            if (action.Kind == BattleActionKind.Restoration &&
                M2DeepArtRuntime070.CanEarnFieldMedicRevival091(
                    FindRecruit(campaign.Guild.Recruits, actor.MemberId), earlyMedicActor091, action.ArtId, content))
            {
                var revival091 = content.Art("ART_STAND_AGAIN");
                learned.Add(revival091.Id);
                progress = EnsureArtProgress(progress, revival091.Id, revival091.Discipline);
                events.Add(Event(events.Count, battle.Round, "BREAKTHROUGH", BattleSide.Player,
                    union.UnionId, actor.MemberId, revival091.Id,
                    actor.DisplayName + " learned Stand Again through Field Medic training. " +
                    "Revives a fallen ally through a Union rescue order (6 AP / 12 MP).",
                    gain, union.UnionId, actor.MemberId, union.UnionId, actor.MemberId));
            }

            learned.Sort(StringComparer.Ordinal);
            actor = actor.With(
                learnedArtIds: learned.AsReadOnly(),
                meaningfulUsePoints: actor.MeaningfulUsePoints + gain,
                discoveryProgress: discovery,
                artProgress: progress);
            members[memberIndex] = actor;
            players[unionIndex] = union.With(
                members: members.AsReadOnly(),
                unionMeaningfulUsePoints: union.UnionMeaningfulUsePoints +
                    ScaleGrowth(M2MeaningfulUse.UnionProgressGain(true), rules.UnionGrowthPct));
        }

        private static void ApplyDeferredGuardProgress(
            CampaignState campaign,
            BattleState battle,
            M2CombatContent content,
            ModeRuleSnapshot rules,
            IReadOnlyList<BattleForecastState> forecasts,
            List<BattleUnionState> players,
            List<BattleEventState> events)
        {
            for (var forecastIndex = 0; forecastIndex < forecasts.Count; forecastIndex++)
            {
                var unionIndex = FindUnionIndex(players, forecasts[forecastIndex].UnionId);
                if (unionIndex < 0) continue;
                for (var actionIndex = 0; actionIndex < forecasts[forecastIndex].MemberActions.Count; actionIndex++)
                {
                    var action = forecasts[forecastIndex].MemberActions[actionIndex];
                    if (action.Kind != BattleActionKind.Guard) continue;
                    var meaningful = ContainsInterceptionFor(events, action.ActorMemberId);
                    ApplyActionProgress(
                        campaign, battle, content, rules, unionIndex, action, meaningful, meaningful ? 1 : 0, players, events);
                }
            }
        }

        private static IReadOnlyList<BattleArtProgressState> AddMeaningfulArtUse(
            IReadOnlyList<BattleArtProgressState> source, string artId, string discipline, int gain)
        {
            var result = new List<BattleArtProgressState>(source ?? Array.Empty<BattleArtProgressState>());
            var found = false;
            for (var i = 0; i < result.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(result[i].ArtId, artId)) continue;
                result[i] = result[i].AddMeaningfulUse(gain);
                found = true;
                break;
            }
            if (!found) result.Add(new BattleArtProgressState(artId, discipline, 1, gain));
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.ArtId, right.ArtId));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<BattleArtProgressState> EnsureArtProgress(
            IReadOnlyList<BattleArtProgressState> source, string artId, string discipline)
        {
            var result = new List<BattleArtProgressState>(source ?? Array.Empty<BattleArtProgressState>());
            for (var i = 0; i < result.Count; i++)
                if (StringComparer.Ordinal.Equals(result[i].ArtId, artId)) return result.AsReadOnly();
            result.Add(new BattleArtProgressState(artId, discipline, 0, 0));
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.ArtId, right.ArtId));
            return result.AsReadOnly();
        }

        private static BattleArtProgressState FindArtProgress(IReadOnlyList<BattleArtProgressState> values, string artId)
        {
            for (var i = 0; i < values.Count; i++)
                if (StringComparer.Ordinal.Equals(values[i].ArtId, artId)) return values[i];
            throw new InvalidOperationException("Meaningful Art progress was not recorded for " + artId + ".");
        }

        private static bool ContainsInterceptionFor(IReadOnlyList<BattleEventState> events, string memberId)
        {
            for (var i = 0; i < events.Count; i++)
                if (StringComparer.Ordinal.Equals(events[i].EventType, "INTERCEPTION") &&
                    StringComparer.Ordinal.Equals(events[i].MemberId, memberId)) return true;
            return false;
        }

        private static void ResolveEnemyTurn(long campaignSeed, BattleState battle,
            M2CombatContent content, List<BattleUnionState> players,
            List<BattleUnionState> enemies, List<BattleEventState> events)
        {
            var gameplayRngStateHash090 = GameplayRngStateHash090(battle);
            // Read the round-start snapshot, never statuses added while this
            // round is executing. A slow changes only the next native queue.
            var enemyOrder161=Enumerable.Range(0,enemies.Count).OrderByDescending(index=>
                TitanHeroSpeedBasisPoints161(battle.TitanHeroes161,battle.Round,enemies[index].UnionId)).ThenBy(index=>index).ToArray();
            for (var enemyOrderIndex161 = 0; enemyOrderIndex161 < enemyOrder161.Length; enemyOrderIndex161++)
            {
                var enemyUnionIndex=enemyOrder161[enemyOrderIndex161];
                if (AllDefeated(players)) return;
                var enemyUnion = enemies[enemyUnionIndex];
                if (!IsActive(enemyUnion)) continue;
                var madeAttack086 = false;
                var rng = Pcg32.FromParts(campaignSeed, battle.BattleId, battle.Round, enemyUnion.UnionId,
                    "DETERMINISTIC_ENEMY_BEHAVIOR", gameplayRngStateHash090);
                for (var enemyIndex = 0; enemyIndex < enemyUnion.Members.Count; enemyIndex++)
                {
                    // A lethal strike ends this sequence before another member
                    // spends a heal/revival or attacks an empty battlefield. Break
                    // rather than return so the actual attack still pays its AP.
                    if (AllDefeated(players)) break;
                    enemyUnion = enemies[enemyUnionIndex];
                    var attacker = enemyUnion.Members[enemyIndex];
                    if (attacker.Downed) continue;
                    BattleUnionState storyThreatUnion;
                    var storyThreat = FindStoryThreatAttacker076(
                        battle.BattleId, enemies, out storyThreatUnion);
                    var isStoryThreatAttack = storyThreat != null && storyThreatUnion != null &&
                        StringComparer.Ordinal.Equals(storyThreatUnion.UnionId, enemyUnion.UnionId) &&
                        StringComparer.Ordinal.Equals(storyThreat.MemberId, attacker.MemberId);
                    if (!isStoryThreatAttack && TryResolveEnemyRescue086(
                            battle.Round, content, enemies, enemyUnionIndex,
                            attacker.MemberId, events))
                    {
                        enemyUnion = enemies[enemyUnionIndex];
                        continue;
                    }
                    var activePlayerIndices = ActiveIndices(players);
                    if (activePlayerIndices.Count == 0) return;
                    var tutorialGuardUnionIndex = FindGuardedTutorialCandidateUnionIndex(players, battle);
                    var targetUnionIndex = isStoryThreatAttack
                        ? FirstActiveIndex(players)
                        : tutorialGuardUnionIndex >= 0
                        ? tutorialGuardUnionIndex
                        : activePlayerIndices[(int)rng.NextBounded((uint)activePlayerIndices.Count)];
                    var threatTarget161=TitanHeroForcedTarget161(battle.TitanHeroes161,battle.Round,enemyUnion.UnionId);
                    var threatIndex161=string.IsNullOrEmpty(threatTarget161)?-1:FindUnionIndex(players,threatTarget161);
                    if(!isStoryThreatAttack&&threatIndex161>=0&&IsActive(players[threatIndex161]))targetUnionIndex=threatIndex161;
                    var targetUnion = players[targetUnionIndex];
                    var targetIndex = !isStoryThreatAttack && tutorialGuardUnionIndex >= 0
                        ? targetUnion.FindMemberIndex(battle.TutorialBreakthroughMemberId)
                        : FirstActiveMemberIndex(targetUnion);
                    if (targetIndex < 0) continue;
                    var interceptIndex = targetIndex >= 0 && targetUnion.Members[targetIndex].Guarding
                        ? targetIndex
                        : FirstGuardingMemberIndex(targetUnion);
                    var intercepted = interceptIndex >= 0;
                    if (intercepted) targetIndex = interceptIndex;
                    var target = targetUnion.Members[targetIndex];
                    var version70Enemy = !StringComparer.Ordinal.Equals(
                        attacker.ClassId, "ENEMY_FORMATION_NUISANCE");
                    var attackArt = version70Enemy
                        ? SelectEnemyArt070(
                            attacker, content, enemyUnion.CurrentAp,
                            battle.Round, enemyIndex)
                        : "ART_QUICK_CUT";
                    if (!isStoryThreatAttack && content.Arts.TryGetValue(attackArt, out var areaArt095) &&
                        areaArt095.AreaProfile095 != null)
                    {
                        // Reviewed area Arts cannot fall into the legacy flat-AP/
                        // free-MP hit path when their own cost or target is illegal.
                        TryResolveEnemyArea095(battle, content, enemies, enemyUnionIndex,
                            enemyIndex, players, targetUnion, target, areaArt095, events);
                        continue;
                    }
                    var baseDamage = isStoryThreatAttack
                        ? StoryThreatDamage076(battle.BattleId, targetUnion)
                        : version70Enemy
                        ? Math.Max(7, attacker.Attack / 2) + rng.NextInclusive(0, 5)
                        : 9 + rng.NextInclusive(0, 6) + enemyIndex;
                    var damage = intercepted || targetUnion.Guarding ? Math.Max(1, baseDamage / 2) : baseDamage;
                    damage=AdjustNativeDamage161(battle.Round,BattleSide.Enemy,enemyUnion.UnionId,
                        targetUnion.UnionId,target.MemberId,BattleActionKind.Martial,damage,players,events);
                    var members = new List<BattleMemberState>(targetUnion.Members);
                    var hp = Math.Max(0, target.CurrentHp - damage);
                    members[targetIndex] = target.With(currentHp: hp, stabilized: false, guarding: false);
                    targetUnion = targetUnion.With(
                        members: members.AsReadOnly(), cohesion: Math.Max(0, targetUnion.Cohesion - (intercepted ? 2 : 5)),
                        formationConditionBasisPoints: Math.Max(0, targetUnion.FormationConditionBasisPoints - (intercepted ? 200 : 500)),
                        guarding: intercepted ? HasOtherGuarding(members, targetIndex) : targetUnion.Guarding,
                        engagement: hp == 0 && targetUnion.IsDefeated
                            ? EngagementState.Broken
                            : targetUnion.Engagement == EngagementState.Flanking
                                ? EngagementState.Flanking
                                : EngagementState.Engaged);
                    players[targetUnionIndex] = targetUnion;
                    madeAttack086 = true;
                    if (intercepted)
                        events.Add(Event(events.Count, battle.Round, "INTERCEPTION", BattleSide.Player,
                            targetUnion.UnionId, target.MemberId, "ART_GUARD",
                            isStoryThreatAttack
                                ? target.DisplayName + " intercepts " + attacker.DisplayName +
                                  "'s telegraphed strike and cuts damage to " + damage + "."
                                : version70Enemy
                                ? target.DisplayName + " intercepts " + attacker.DisplayName + "'s " +
                                  HumanizeStableId070(attackArt) + " and cuts damage to " + damage + "."
                                : target.DisplayName + " intercepts the Gate Gnawer attack and cuts damage to " + damage + ".",
                            damage,
                            enemyUnion.UnionId, attacker.MemberId,
                            targetUnion.UnionId, target.MemberId));
                    else
                        events.Add(Event(events.Count, battle.Round, "ENEMY_HIT", BattleSide.Enemy,
                            targetUnion.UnionId, target.MemberId, attackArt,
                            isStoryThreatAttack
                                ? attacker.DisplayName + " lands the telegraphed strike on " +
                                  target.DisplayName + " for " + damage + " HP. HOLD THE LINE would halve it."
                                : version70Enemy
                                ? attacker.DisplayName + " uses " + HumanizeStableId070(attackArt) +
                                  " on " + target.DisplayName + " for " + damage + " HP."
                                : attacker.DisplayName + " hits " + target.DisplayName + " for " + damage + " HP.",
                            damage,
                            enemyUnion.UnionId, attacker.MemberId,
                            targetUnion.UnionId, target.MemberId));
                    if (hp == 0)
                        events.Add(Event(events.Count, battle.Round, "DOWNED", BattleSide.Player,
                            targetUnion.UnionId, target.MemberId, attackArt, target.DisplayName + " is Downed — recovery remains possible.", 0,
                            enemyUnion.UnionId, attacker.MemberId,
                            targetUnion.UnionId, target.MemberId));
                }
                enemyUnion = enemies[enemyUnionIndex];
                enemies[enemyUnionIndex] = enemyUnion.With(
                    currentAp: madeAttack086
                        ? Math.Max(0, enemyUnion.CurrentAp - 3)
                        : enemyUnion.CurrentAp,
                    engagement: enemyUnion.Engagement == EngagementState.RearPressure ||
                                enemyUnion.Engagement == EngagementState.Broken
                        ? enemyUnion.Engagement
                        : EngagementState.Engaged);
            }
        }

        private static bool TryResolveEnemyRescue086(
            int round,
            M2CombatContent content,
            List<BattleUnionState> enemyUnions,
            int sourceUnionIndex,
            string actorMemberId,
            List<BattleEventState> events)
        {
            if (content == null || enemyUnions == null || sourceUnionIndex < 0 ||
                sourceUnionIndex >= enemyUnions.Count)
                return false;
            var source = enemyUnions[sourceUnionIndex];
            var actorIndex = source.FindMemberIndex(actorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed) return false;

            var woundedTarget = MostWoundedUnion(enemyUnions);
            if (woundedTarget != null &&
                IsCriticalRestorationTarget086(woundedTarget) &&
                TryResolveEnemyRestoration086(
                    round, content, enemyUnions, sourceUnionIndex,
                    actorMemberId, woundedTarget, events))
                return true;

            var supportTarget = MostSupportNeedyUnion(enemyUnions);
            if (supportTarget == null ||
                !IsCriticalSupportTarget086(supportTarget))
                return false;
            if (TryResolveEnemyRestoration086(
                    round, content, enemyUnions, sourceUnionIndex,
                    actorMemberId, supportTarget, events))
                return true;
            return TryResolveEnemySupportArt086(
                round, content, enemyUnions, sourceUnionIndex,
                actorMemberId, supportTarget, events);
        }

        private static bool TryResolveEnemyRestoration086(
            int round,
            M2CombatContent content,
            List<BattleUnionState> enemyUnions,
            int sourceUnionIndex,
            string actorMemberId,
            BattleUnionState target,
            List<BattleEventState> events)
        {
            var source = enemyUnions[sourceUnionIndex];
            var actorIndex = source.FindMemberIndex(actorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed ||
                target == null)
                return false;
            var actor = source.Members[actorIndex];
            var artId = SelectRestorationArtForTarget086(
                actor, source, target, content, source.CurrentAp, null);
            if (string.IsNullOrWhiteSpace(artId) ||
                !content.Arts.TryGetValue(artId, out var art))
                return false;
            var projection = BuildRestorationProjection086(
                actor, target, art, null);
            if (!projection.Meaningful ||
                IsWastefulMajorRestoration086(
                    target, art, projection, null))
                return false;

            var members = new List<BattleMemberState>(source.Members);
            members[actorIndex] = actor.With(
                currentMp: actor.CurrentMp - art.PersonalMpCost,
                guarding: false);
            source = source.With(
                members: members.AsReadOnly(),
                currentAp: source.CurrentAp - art.SharedApCost,
                guarding: false,
                engagement: EngagementState.Reinforcing);
            enemyUnions[sourceUnionIndex] = source;
            var targetMember = FindMember086(
                target, projection.PrimaryTargetMemberId);
            var commandName = HasDownedMember086(target)
                ? "Save Them!"
                : "Heal the Wounded Union!";
            events.Add(Event(events.Count, round,
                "ENEMY_SUPPORT_FORECAST", BattleSide.Enemy,
                source.UnionId, actor.MemberId, art.Id,
                source.DisplayName + " commits “" + commandName + "” — " +
                actor.DisplayName + " will use " + art.Name + " on " +
                target.DisplayName + ".", art.SharedApCost,
                source.UnionId, actor.MemberId,
                target.UnionId, targetMember?.MemberId ?? string.Empty));
            var action = new BattlePlannedActionState(
                actor.MemberId,
                actor.DisplayName,
                target.UnionId,
                targetMember?.MemberId ?? string.Empty,
                art.Id,
                art.Name,
                BattleActionKind.Restoration,
                art.SharedApCost,
                art.PersonalMpCost,
                projection.TotalHp,
                projection.CohesionGain,
                projection.FormationGain,
                projection.Prediction,
                true,
                false,
                art.AnimationTag,
                art.Discipline);
            return ResolveRestoration(
                       round, action, enemyUnions, sourceUnionIndex,
                       content, events, BattleSide.Enemy) > 0;
        }

        private static bool TryResolveEnemySupportArt086(
            int round,
            M2CombatContent content,
            List<BattleUnionState> enemyUnions,
            int sourceUnionIndex,
            string actorMemberId,
            BattleUnionState target,
            List<BattleEventState> events)
        {
            var source = enemyUnions[sourceUnionIndex];
            var actorIndex = source.FindMemberIndex(actorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed ||
                target == null)
                return false;
            var actor = source.Members[actorIndex];
            var artId = SelectFriendlyUnionSupportArt080(
                actor, target, content, source.CurrentAp);
            if (string.IsNullOrWhiteSpace(artId) ||
                !content.Arts.TryGetValue(artId, out var art))
                return false;
            var cohesionGain = PredictedSupportCohesion086(target, art);
            var formationGain = PredictedSupportFormation086(target, art);
            if (!IsMeaningfulFriendlySupport086(
                    target, art, cohesionGain, formationGain))
                return false;

            var members = new List<BattleMemberState>(source.Members);
            members[actorIndex] = actor.With(
                currentMp: actor.CurrentMp - art.PersonalMpCost,
                guarding: false);
            source = source.With(
                members: members.AsReadOnly(),
                currentAp: source.CurrentAp - art.SharedApCost,
                guarding: false,
                engagement: EngagementState.Reinforcing);
            enemyUnions[sourceUnionIndex] = source;
            events.Add(Event(events.Count, round,
                "ENEMY_SUPPORT_FORECAST", BattleSide.Enemy,
                source.UnionId, actor.MemberId, art.Id,
                source.DisplayName + " commits “Support the Other Union!” — " +
                actor.DisplayName + " will use " + art.Name + " on " +
                target.DisplayName + ".", art.SharedApCost,
                source.UnionId, actor.MemberId,
                target.UnionId, target.LeaderMemberId));
            var action = new BattlePlannedActionState(
                actor.MemberId,
                actor.DisplayName,
                target.UnionId,
                target.LeaderMemberId,
                art.Id,
                art.Name,
                BattleActionKind.Recovery,
                art.SharedApCost,
                art.PersonalMpCost,
                0,
                cohesionGain,
                formationGain,
                SupportPrediction086(
                    target, art, cohesionGain, formationGain),
                true,
                false,
                art.AnimationTag,
                art.Discipline);
            return ResolveFriendlySupportArt086(
                       round, action, enemyUnions, sourceUnionIndex,
                       art, events, BattleSide.Enemy) > 0;
        }

        private static string SelectEnemyArt070(
            BattleMemberState attacker,
            M2CombatContent content,
            int sharedAp,
            int round,
            int memberIndex)
        {
            if (attacker?.LearnedArtIds == null || attacker.LearnedArtIds.Count == 0)
                return "ART_QUICK_CUT";
            var legal = new List<string>();
            for (var index = 0; index < attacker.LearnedArtIds.Count; index++)
            {
                if (content == null || !content.Arts.TryGetValue(
                        attacker.LearnedArtIds[index], out var art) ||
                    !art.IsForecastAction ||
                    art.SharedApCost > sharedAp ||
                    art.PersonalMpCost > attacker.CurrentMp ||
                    !IsEquipmentLegal(art, attacker.EquipmentTags) ||
                    StringComparer.Ordinal.Equals(art.Discipline, "Restoration") ||
                    StringComparer.Ordinal.Equals(art.Discipline, "Support") ||
                    StringComparer.Ordinal.Equals(art.Discipline, "Recovery"))
                    continue;
                legal.Add(art.Id);
            }
            if (legal.Count == 0) return "ART_QUICK_CUT";
            legal.Sort(StringComparer.Ordinal);
            var selectedIndex = Math.Abs(
                (round - 1 + memberIndex) % legal.Count);
            return legal[selectedIndex];
        }

        private static string HumanizeStableId070(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "attack";
            var readable = value.StartsWith("ART_", StringComparison.Ordinal)
                ? value.Substring("ART_".Length)
                : value;
            return readable.Replace('_', ' ').ToLowerInvariant();
        }

        private static string SelectArtForIntent(BattleMemberState member, BattleUnionState union,
            string commandId, M2CombatContent content, Pcg32 rng, int apRemaining,
            BattleState battle)
        {
            if (commandId == "CMD_AP_RECOVERY" || commandId == "CMD_RETREAT") return "ART_RECOVER_BREATH";
            if (commandId == "CMD_BALANCED") return BasicArtId(member.EquipmentTags, battle);
            if (commandId == "CMD_GUARD")
            {
                return SelectLearnedArt(
                    member, content, rng, apRemaining, battle, union.UnionId,
                    "Guard") ?? "ART_GUARD";
            }
            if (commandId == "CMD_HEAL")
                return SelectLearnedArt(
                    member, content, rng, apRemaining, battle, union.UnionId,
                    "Restoration") ?? "ART_GUARD";
            if (commandId == "CMD_MYSTIC")
            {
                return SelectLearnedArt(
                    member, content, rng, apRemaining, battle, union.UnionId,
                    "Mystic") ?? BasicArtId(member.EquipmentTags, battle);
            }
            if (commandId == "CMD_SUPPORT")
                return SelectLearnedArt(
                           member, content, rng, apRemaining, battle,
                           union.UnionId, "Support") ??
                       "ART_RECOVER_BREATH";
            if (commandId == "CMD_ALL_OUT" || commandId == "CMD_FLANK" || commandId == "CMD_BREAK" || commandId == "CMD_RANGED")
                return SelectLearnedArt(
                    member, content, rng, apRemaining, battle, union.UnionId,
                    "Martial", "Tactical") ?? BasicArtId(member.EquipmentTags, battle);
            return BasicArtId(member.EquipmentTags, battle);
        }

        private static string SelectLearnedArt(
            BattleMemberState member,
            M2CombatContent content,
            Pcg32 rng,
            int apRemaining,
            BattleState battle,
            string sourceUnionId,
            params string[] disciplines)
        {
            var candidates = new List<WeightedValue<string>>();
            var ordered = new List<M2ArtDefinition>();
            for (var i = 0; i < member.LearnedArtIds.Count; i++)
            {
                if (!content.Arts.TryGetValue(member.LearnedArtIds[i], out var art)) continue;
                if (!art.IsForecastAction || !Contains(disciplines, art.Discipline) || !IsEquipmentLegal(art, member.EquipmentTags) ||
                    art.PersonalMpCost > member.CurrentMp || art.SharedApCost > apRemaining) continue;
                if (IsSssOwnedGuestUnionScope090(art) &&
                    OwnedLivingGuestUnion090(
                        battle, battle?.PlayerUnions, member.MemberId,
                        sourceUnionId) == null)
                    continue;
                ordered.Add(art);
            }
            ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            for (var i = 0; i < ordered.Count; i++)
            {
                if (!M2DeepArtRuntime070.IsDeepArt(content, ordered[i].Id)) continue;
                for (var progressIndex = 0; progressIndex < member.ArtProgress.Count; progressIndex++)
                    if (StringComparer.Ordinal.Equals(member.ArtProgress[progressIndex].ArtId, ordered[i].Id) &&
                        member.ArtProgress[progressIndex].MeaningfulUses == 0)
                        return ordered[i].Id;
            }
            for (var i = 0; i < ordered.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(ordered[i].Id, member.BreakthroughArtId) ||
                    !Contains(member.LearnedArtIds, ordered[i].Id)) continue;
                for (var progressIndex = 0; progressIndex < member.ArtProgress.Count; progressIndex++)
                    if (StringComparer.Ordinal.Equals(member.ArtProgress[progressIndex].ArtId, ordered[i].Id) &&
                        member.ArtProgress[progressIndex].MeaningfulUses == 0)
                        return ordered[i].Id;
            }
            var hasCommittedArt = false;
            for (var i = 0; i < ordered.Count; i++)
                if (ordered[i].SharedApCost > 0) { hasCommittedArt = true; break; }
            for (var i = 0; i < ordered.Count; i++)
            {
                if (hasCommittedArt && ordered[i].SharedApCost == 0) continue;
                var weight = (uint)Math.Max(1, 1 + ordered[i].SharedApCost * 4 + ordered[i].PersonalMpCost);
                candidates.Add(new WeightedValue<string>(ordered[i].Id, weight));
            }
            return candidates.Count == 0 ? null : rng.WeightedChoice(candidates);
        }

        private static string SelectFriendlyUnionSupportArt080(
            BattleMemberState member,
            BattleUnionState targetUnion,
            M2CombatContent content,
            int apRemaining,
            BattleState battle = null,
            string sourceUnionId = null)
        {
            M2ArtDefinition best = null;
            var bestScore = int.MinValue;
            for (var index = 0; index < member.LearnedArtIds.Count; index++)
            {
                if (!content.Arts.TryGetValue(member.LearnedArtIds[index], out var art) ||
                    !art.IsForecastAction ||
                    (!StringComparer.Ordinal.Equals(art.Discipline, "Support") &&
                     !StringComparer.Ordinal.Equals(art.Discipline, "Recovery")) ||
                     !SupportsFriendlyUnionTarget080(art) ||
                     art.PersonalMpCost > member.CurrentMp || art.SharedApCost > apRemaining ||
                     !IsEquipmentLegal(art, member.EquipmentTags)) continue;
                if (IsSssOwnedGuestUnionScope090(art))
                {
                    var ownedGuest090 = OwnedLivingGuestUnion090(
                        battle, battle?.PlayerUnions, member.MemberId,
                        sourceUnionId);
                    if (ownedGuest090 == null || targetUnion == null ||
                        !StringComparer.Ordinal.Equals(
                            ownedGuest090.UnionId, targetUnion.UnionId))
                        continue;
                }
                var cohesion = PredictedSupportCohesion086(targetUnion, art);
                var formation = PredictedSupportFormation086(targetUnion, art);
                if (!IsMeaningfulFriendlySupport086(
                        targetUnion, art, cohesion, formation))
                    continue;
                var score = checked(cohesion * 100 + formation / 10 +
                    (targetUnion != null &&
                     targetUnion.Engagement == EngagementState.Broken &&
                     IsCleanseArt086(art) ? 30000 : 0) +
                    (targetUnion != null && !targetUnion.Guarding &&
                     IsProtectionArt086(art) ? 5000 : 0) -
                    art.SharedApCost * 80 - art.PersonalMpCost * 30);
                if (score > bestScore || score == bestScore && best != null &&
                    StringComparer.Ordinal.Compare(art.Id, best.Id) < 0)
                {
                    best = art;
                    bestScore = score;
                }
            }
            return best?.Id;
        }

        private static BattleActionKind ActionKind(string commandId, M2ArtDefinition art, BattleState battle)
        {
            // Exhausted budgets may substitute Recover Breath under an offensive
            // command. It cannot become a free hit or train Support by doing damage.
            // Old committed/replayed battles retain their recorded classification.
            if (UsesBasicStrikeRules101(battle) &&
                (StringComparer.Ordinal.Equals(art.Discipline, "Recovery") ||
                 StringComparer.Ordinal.Equals(art.Discipline, "Support")))
                return BattleActionKind.Recovery;
            if (commandId == "CMD_FLANK") return BattleActionKind.Tactical;
            if (StringComparer.Ordinal.Equals(art.Discipline, "Restoration")) return BattleActionKind.Restoration;
            if (commandId == "CMD_GUARD" || StringComparer.Ordinal.Equals(art.Discipline, "Guard")) return BattleActionKind.Guard;
            if (commandId == "CMD_AP_RECOVERY" || commandId == "CMD_RETREAT" || commandId == "CMD_SUPPORT") return BattleActionKind.Recovery;
            if (StringComparer.Ordinal.Equals(art.Discipline, "Mystic")) return BattleActionKind.Mystic;
            if (StringComparer.Ordinal.Equals(art.Discipline, "Tactical")) return BattleActionKind.Tactical;
            return BattleActionKind.Martial;
        }

        private static int PredictedHpDelta(BattleMemberState actor, BattleMemberState target,
            BattleActionKind kind, string commandId, bool formationActive, M2ArtDefinition art,
            int artPowerPermille088, int? actionCoefficient095 = null)
        {
            if (kind == BattleActionKind.Restoration)
            {
                if (target == null) return 0;
                var baseHealing = 18 + actor.MagicAttack / 2;
                var scaledHealing = Math.Max(1, baseHealing * (art?.PowerCoefficientPermille ?? 1000) / 1000);
                scaledHealing = M2ArtMasteryLevelPolicy088.ScaleMagnitudeByPowerPermille(
                    scaledHealing,
                    artPowerPermille088);
                return Math.Min(target.MaximumHp - target.CurrentHp, scaledHealing);
            }
            if (kind == BattleActionKind.Guard || kind == BattleActionKind.Recovery) return 0;
            var power = kind == BattleActionKind.Mystic ? actor.MagicAttack : actor.Attack;
            var multiplier = commandId == "CMD_ALL_OUT" ? 130 : commandId == "CMD_FLANK" ? 115 : 100;
            if (formationActive) multiplier += 5;
            var baseDamage = 10 + power * multiplier / 100;
            var scaledDamage = Math.Max(
                1,
                baseDamage * (actionCoefficient095 ?? art?.PowerCoefficientPermille ?? 1000) / 1000);
            return -Math.Max(
                1,
                M2ArtMasteryLevelPolicy088.ScaleMagnitudeByPowerPermille(
                    scaledDamage,
                    artPowerPermille088));
        }

        private static string ExpectedEffect(IReadOnlyList<BattlePlannedActionState> actions, int apRecovery, string commandId)
        {
            var damage = 0; var healing = 0; var cohesion = 0; var formation = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].PredictedHpDelta < 0) damage -= actions[i].PredictedHpDelta;
                else healing += actions[i].PredictedHpDelta;
                cohesion += actions[i].PredictedCohesionDelta;
                formation += actions[i].PredictedFormationDelta;
            }
            var result = damage > 0 ? "about " + damage + " HP damage" : healing > 0 ? "up to " + healing + " HP restored" : "no direct HP change";
            if (cohesion != 0) result += ", Cohesion " + Signed(cohesion);
            if (formation != 0) result += ", formation " + Signed(formation / 100) + "%";
            if (commandId == "CMD_SUPPORT") result += ", Cohesion +10, formation +12%";
            if (apRecovery > 0) result += ", shared AP +" + apRecovery;
            if (commandId == "CMD_GUARD") result += ", interception readied";
            if (commandId == "CMD_FLANK") result += ", side strike opens rear pressure";
            return result + ".";
        }

        private static string Risk(string commandId, BattleUnionState union, BattleState battle)
        {
            BattleUnionState threatUnion;
            var storyThreat = FindStoryThreatAttacker076(
                battle.BattleId, battle.EnemyUnions, out threatUnion);
            var leadUnion = FirstActiveUnion(battle.PlayerUnions);
            var storyThreatDamage = StoryThreatDamage076(battle.BattleId, union);
            if (storyThreat != null && leadUnion != null &&
                StringComparer.Ordinal.Equals(leadUnion.UnionId, union.UnionId) &&
                storyThreatDamage > 0)
            {
                if (commandId == "CMD_GUARD")
                    return "COUNTER: HOLD THE LINE cuts " + storyThreat.DisplayName +
                           "'s telegraphed " + storyThreatDamage + " HP pressure to about " +
                           Math.Max(1, storyThreatDamage / 2) + " HP.";
                return "DANGER: " + storyThreat.DisplayName + " has marked this Union for about " +
                       storyThreatDamage + " HP. HOLD THE LINE cuts that pressure in half.";
            }
            if (commandId == "CMD_ALL_OUT") return "High commitment: enemy retaliation may hit an exposed Union.";
            if (commandId == "CMD_GUARD") return "Low damage this round if the enemy does not pressure this Union.";
            if (commandId == "CMD_HEAL") return "Restoration is wasted on members who no longer have missing HP; fallback guards instead.";
            if (commandId == "CMD_AP_RECOVERY") return "Concedes tempo while rebuilding the shared AP pool.";
            if (commandId == "CMD_RETREAT") return "This Union leaves the proof; all Unions withdrawn ends in Retreat.";
            if (commandId == "CMD_FLANK")
                return "The flanking Union gains a blindside route while the lead Union holds the deadlock; if that line breaks, the attack falls back to frontal pressure.";
            if (!union.FormationBenefitActive) return union.FormationInactiveReason;
            return "Forecasted targets can become Downed first; deterministic fallback retargets the next legal member.";
        }

        private static string PlayerFacingCommandName(
            string commandId,
            string authorityName,
            IReadOnlyList<BattlePlannedActionState> actions,
            BattleUnionState union,
            BattleUnionState targetUnion,
            BattleState battle)
        {
            switch (commandId)
            {
                case "CMD_BALANCED": return "Attack!";
                case "CMD_ALL_OUT": return ContainsCompatibleFamilyAction(commandId, actions)
                    ? "Attack Using Combat Arts!"
                    : "Press the Attack!";
                case "CMD_MYSTIC": return "Use Mystic Arts!";
                case "CMD_GUARD": return "Hold the Line!";
                case "CMD_HEAL":
                    if (targetUnion != null &&
                        !StringComparer.Ordinal.Equals(
                            targetUnion.UnionId, union.UnionId))
                    {
                        if (HasDownedMember086(targetUnion) ||
                            IsCriticalRestorationTarget086(targetUnion))
                            return "Save Them!";
                        return IsLowRestorationTarget086(targetUnion)
                            ? "Heal the Wounded Union!"
                            : "Support the Other Union!";
                    }
                    return "Heal the Wounded!";
                case "CMD_AP_RECOVERY": return "Recover AP!";
                case "CMD_SUPPORT":
                    if (targetUnion != null &&
                        !StringComparer.Ordinal.Equals(
                            targetUnion.UnionId, union.UnionId))
                        return IsCriticalSupportTarget086(targetUnion)
                            ? "Protect and Heal Them!"
                            : "Restore Their Formation!";
                    return "Restore Formation!";
                case "CMD_FLANK": return EnemyGuarding(battle.EnemyUnions) ||
                    union.Engagement == EngagementState.Flanking
                        ? "Hit Their Blind Side!"
                        : "Side Strike!";
                case "CMD_RETREAT": return "Get Out of Here!";
                default: return authorityName;
            }
        }

        private static bool CommandSupportsBreakthrough(string commandId, string targetDiscipline)
        {
            if (StringComparer.Ordinal.Equals(targetDiscipline, "Guard"))
                return StringComparer.Ordinal.Equals(commandId, "CMD_GUARD");
            if (StringComparer.Ordinal.Equals(targetDiscipline, "Mystic"))
                return StringComparer.Ordinal.Equals(commandId, "CMD_MYSTIC");
            if (StringComparer.Ordinal.Equals(targetDiscipline, "Restoration"))
                return StringComparer.Ordinal.Equals(commandId, "CMD_HEAL");
            if (StringComparer.Ordinal.Equals(targetDiscipline, "Martial") ||
                StringComparer.Ordinal.Equals(targetDiscipline, "Tactical"))
                return StringComparer.Ordinal.Equals(commandId, "CMD_ALL_OUT");
            return false;
        }

        private static bool DisciplinesAreCompatibleForDiscovery(string usedDiscipline, string targetDiscipline)
        {
            if (string.IsNullOrWhiteSpace(usedDiscipline) || string.IsNullOrWhiteSpace(targetDiscipline)) return false;
            if (StringComparer.Ordinal.Equals(usedDiscipline, targetDiscipline)) return true;
            return (StringComparer.Ordinal.Equals(usedDiscipline, "Martial") || StringComparer.Ordinal.Equals(usedDiscipline, "Tactical")) &&
                   (StringComparer.Ordinal.Equals(targetDiscipline, "Martial") || StringComparer.Ordinal.Equals(targetDiscipline, "Tactical"));
        }

        private static string Learning(IReadOnlyList<BattlePlannedActionState> actions, M2CombatContent content)
        {
            for (var i = 0; i < actions.Count; i++)
                if (actions[i].BreakthroughOpportunity)
                {
                    if (M2DeepArtRuntime070.IsDeepArt(content, actions[i].BreakthroughTargetArtId))
                        return actions[i].ActorName + " can learn " +
                            actions[i].BreakthroughTargetArtName + " by using " +
                            actions[i].ArtName + " meaningfully. Its learning bar is ready; " +
                            "success makes the Art part of future battles.";
                    return actions[i].ActorName + " can learn " +
                        actions[i].BreakthroughTargetArtName + " by using " +
                        actions[i].ArtName + " (Discovery 95→100). " +
                        "Success makes the Art part of future battles.";
                }
            for (var i = 0; i < actions.Count; i++)
                if (actions[i].PredictedGrowth > 0)
                    return actions[i].ActorName + " can gain +" + actions[i].PredictedGrowth + " " +
                        actions[i].ArtName + " mastery from meaningful use.";
            return "No learning: harmless or already-satisfied actions grant no growth.";
        }

        private static string Prediction(BattleActionKind kind, int hp, int cohesion, int formation)
        {
            if (kind == BattleActionKind.Guard) return "Guard and intercept likely incoming harm.";
            if (kind == BattleActionKind.Recovery) return formation > 0 ? "Recover formation and Cohesion." : "Recover shared AP / conserve MP.";
            if (kind == BattleActionKind.Restoration) return hp > 0 ? "Restore up to " + hp + " HP." : "Stabilize a Downed ally if needed.";
            return "Deal about " + Math.Max(0, -hp) + " HP; Cohesion " + Signed(cohesion) + ".";
        }

        private static BattleMemberState TargetMemberFor(
            string commandId,
            BattleActionKind kind,
            BattleUnionState source,
            BattleUnionState targetUnion,
            int ordinal)
        {
            if (targetUnion == null) return null;
            if (kind == BattleActionKind.Restoration)
            {
                var injured = MostInjuredIndex(targetUnion, includeDowned: true);
                return injured >= 0 ? targetUnion.Members[injured] : source.Members[Math.Min(ordinal, source.Members.Count - 1)];
            }
            if (commandId == "CMD_SUPPORT" && kind == BattleActionKind.Recovery &&
                !StringComparer.Ordinal.Equals(source.UnionId, targetUnion.UnionId))
            {
                var leader = targetUnion.FindMemberIndex(targetUnion.LeaderMemberId);
                if (leader < 0 || targetUnion.Members[leader].Downed)
                    leader = FirstActiveMemberIndex(targetUnion);
                return leader >= 0 ? targetUnion.Members[leader] : null;
            }
            if (kind == BattleActionKind.Guard || kind == BattleActionKind.Recovery ||
                commandId == "CMD_SUPPORT" || commandId == "CMD_AP_RECOVERY" || commandId == "CMD_RETREAT")
                return source.Members[Math.Min(ordinal, source.Members.Count - 1)];
            var active = new List<BattleMemberState>();
            for (var i = 0; i < targetUnion.Members.Count; i++) if (!targetUnion.Members[i].Downed) active.Add(targetUnion.Members[i]);
            return active.Count == 0 ? null : active[ordinal % active.Count];
        }

        private static IReadOnlyList<object> ContextualLegalPool(
            BattleUnionState union,
            string commandId,
            M2CombatContent content,
            IReadOnlyList<BattlePlannedActionState> chosenActions,
            BattleState battle)
        {
            var result = new List<object>();
            var apRemaining = union.CurrentAp;
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                var candidates = CandidateArtsForCommand(
                    member, commandId, content, apRemaining,
                    battle, union.UnionId);
                var weighted = new List<object>();
                for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    var art = content.Art(candidates[candidateIndex]);
                    weighted.Add(new
                    {
                        ArtId = art.Id,
                        art.Family,
                        art.Discipline,
                        art.SharedApCost,
                        art.PersonalMpCost,
                        Weight = 1 + art.SharedApCost * 4 + art.PersonalMpCost,
                        CompatibleWithVisibleFamily = IsArtCompatibleWithCommand(commandId, art)
                    });
                }
                result.Add(new
                {
                    member.MemberId,
                    ApRemainingBeforeMember = apRemaining,
                    CandidateArts = weighted,
                    EligibleBreakthrough = member.BreakthroughArtId ?? string.Empty
                });

                var chosen = chosenActions.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.ActorMemberId, member.MemberId));
                if (chosen != null) apRemaining = Math.Max(0, apRemaining - chosen.SharedApCost);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> CandidateArtsForCommand(
            BattleMemberState member,
            string commandId,
            M2CombatContent content,
            int apRemaining,
            BattleState battle = null,
            string sourceUnionId = null)
        {
            if (commandId == "CMD_BALANCED")
            {
                var basicId = BasicArtId(member.EquipmentTags, battle);
                if (!content.Arts.TryGetValue(basicId, out var basic) ||
                    basic.PersonalMpCost > member.CurrentMp || basic.SharedApCost > apRemaining ||
                    !IsEquipmentLegal(basic, member.EquipmentTags)) return Array.Empty<string>();
                return new[] { basicId };
            }

            var disciplines = CommandDisciplines(commandId);
            var result = new List<string>();
            for (var i = 0; i < member.LearnedArtIds.Count; i++)
            {
                if (!content.Arts.TryGetValue(member.LearnedArtIds[i], out var art)) continue;
                if (!art.IsForecastAction) continue;
                if (disciplines.Count > 0 && !Contains(disciplines, art.Discipline)) continue;
                if (art.PersonalMpCost > member.CurrentMp || art.SharedApCost > apRemaining ||
                    !IsEquipmentLegal(art, member.EquipmentTags)) continue;
                if (IsSssOwnedGuestUnionScope090(art) &&
                    OwnedLivingGuestUnion090(
                        battle, battle?.PlayerUnions, member.MemberId,
                        sourceUnionId) == null)
                    continue;
                result.Add(art.Id);
            }
            result.Sort(StringComparer.Ordinal);
            var hasCommitted = false;
            for (var i = 0; i < result.Count; i++)
                if (content.Art(result[i]).SharedApCost > 0) { hasCommitted = true; break; }
            if (hasCommitted && (commandId == "CMD_ALL_OUT" || commandId == "CMD_FLANK"))
                result.RemoveAll(value => content.Art(value).SharedApCost == 0);

            if (result.Count == 0)
            {
                var fallback = commandId == "CMD_GUARD" || commandId == "CMD_HEAL"
                    ? "ART_GUARD"
                    : commandId == "CMD_AP_RECOVERY" || commandId == "CMD_SUPPORT" || commandId == "CMD_RETREAT"
                        ? "ART_RECOVER_BREATH"
                        : BasicArtId(member.EquipmentTags, battle);
                if (content.Arts.TryGetValue(fallback, out var fallbackArt) &&
                    fallbackArt.SharedApCost <= apRemaining && fallbackArt.PersonalMpCost <= member.CurrentMp &&
                    IsEquipmentLegal(fallbackArt, member.EquipmentTags)) result.Add(fallback);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> CommandDisciplines(string commandId)
        {
            if (commandId == "CMD_GUARD") return new[] { "Guard" };
            if (commandId == "CMD_HEAL") return new[] { "Restoration" };
            if (commandId == "CMD_MYSTIC") return new[] { "Mystic" };
            if (commandId == "CMD_ALL_OUT" || commandId == "CMD_FLANK" || commandId == "CMD_BREAK" || commandId == "CMD_RANGED")
                return new[] { "Martial", "Tactical" };
            if (commandId == "CMD_SUPPORT")
                return new[] { "Support", "Recovery" };
            if (commandId == "CMD_AP_RECOVERY" || commandId == "CMD_RETREAT")
                return new[] { "Recovery" };
            return Array.Empty<string>();
        }

        private static bool IsArtCompatibleWithCommand(string commandId, M2ArtDefinition art)
        {
            if (art == null) return false;
            if (commandId == "CMD_BALANCED") return StringComparer.Ordinal.Equals(art.Family, "BASIC");
            if (commandId == "CMD_ALL_OUT" || commandId == "CMD_FLANK")
                return !StringComparer.Ordinal.Equals(art.Family, "BASIC") &&
                       (StringComparer.Ordinal.Equals(art.Discipline, "Martial") ||
                        StringComparer.Ordinal.Equals(art.Discipline, "Tactical"));
            if (commandId == "CMD_MYSTIC") return StringComparer.Ordinal.Equals(art.Discipline, "Mystic");
            if (commandId == "CMD_HEAL") return StringComparer.Ordinal.Equals(art.Discipline, "Restoration");
            if (commandId == "CMD_GUARD") return StringComparer.Ordinal.Equals(art.Discipline, "Guard");
            if (commandId == "CMD_SUPPORT")
                return StringComparer.Ordinal.Equals(art.Discipline, "Support") ||
                       StringComparer.Ordinal.Equals(art.Discipline, "Recovery");
            if (commandId == "CMD_AP_RECOVERY" || commandId == "CMD_RETREAT")
                return StringComparer.Ordinal.Equals(art.Discipline, "Recovery");
            return true;
        }

        private static bool ContainsCompatibleFamilyAction(
            string commandId,
            IReadOnlyList<BattlePlannedActionState> actions)
        {
            for (var i = 0; i < actions.Count; i++)
                if (!string.IsNullOrWhiteSpace(actions[i].ArtId) &&
                    commandId == "CMD_ALL_OUT" &&
                    !actions[i].ArtId.StartsWith("ART_BASIC_", StringComparison.Ordinal) &&
                    (StringComparer.Ordinal.Equals(actions[i].Discipline, "Martial") ||
                     StringComparer.Ordinal.Equals(actions[i].Discipline, "Tactical"))) return true;
            return false;
        }

        private static string PlayerFacingFamilyName(string commandId)
        {
            if (commandId == "CMD_ALL_OUT") return "Combat Art";
            if (commandId == "CMD_MYSTIC") return "Mystic Art";
            if (commandId == "CMD_HEAL") return "Restoration Art";
            if (commandId == "CMD_GUARD") return "Guard Art";
            if (commandId == "CMD_SUPPORT") return "Support Art";
            if (commandId == "CMD_FLANK") return "side-strike Art";
            return "command-family action";
        }

        private static bool IsEquipmentLegal(M2ArtDefinition art, IReadOnlyList<string> equipmentTags)
        {
            if (art.RequiredEquipmentTags.Count == 0) return true;
            for (var required = 0; required < art.RequiredEquipmentTags.Count; required++)
                if (Contains(equipmentTags, art.RequiredEquipmentTags[required])) return true;
            return false;
        }

        private static bool UsesBasicStrikeRules101(BattleState battle) =>
            battle == null || battle.ContentVersion.EndsWith(
                "|" + TutorialRulesVersion, StringComparison.Ordinal);

        private static string BasicArtId(IReadOnlyList<string> tags, BattleState battle) =>
            BasicArtId(tags, UsesBasicStrikeRules101(battle));

        private static string BasicArtId(IReadOnlyList<string> tags, bool basicStrike101)
        {
            if (Contains(tags, "SPEAR")) return "ART_BASIC_THRUST";
            if (Contains(tags, "GREAT_AXE") || Contains(tags, "HAMMER") || Contains(tags, "POLEARM")) return "ART_BASIC_HEAVY_SWING";
            if (Contains(tags, "BOW") || Contains(tags, "SHORTBOW")) return "ART_BASIC_SHOT";
            if (Contains(tags, "DAGGER")) return "ART_BASIC_DAGGER_CUT";
            if (Contains(tags, "SWORD")) return "ART_BASIC_SABER_CUT";
            if (Contains(tags, "FOCUS_TOOL") || Contains(tags, "WAND")) return "ART_BASIC_RUNE_BOLT";
            if (Contains(tags, "STAFF")) return "ART_BASIC_STAFF_TAP";
            return basicStrike101 ? BasicStrikeFallback101.ArtId : "ART_ASSIST_ALLY";
        }

        private static string ClassArtId(string classId)
        {
            switch (classId)
            {
                case "CLASS_GUARDIAN": return "ART_SHIELD_BRACE";
                case "CLASS_WARRIOR": return "ART_POWER_CUT";
                case "CLASS_RANGER": return "ART_PIERCING_SHOT";
                case "CLASS_ROGUE": return "ART_QUICK_CUT";
                case "CLASS_MAGE": return "ART_EMBER_BOLT";
                case "CLASS_PRIEST": return "ART_MINOR_REMEDY";
                default: return "ART_ASSIST_ALLY";
            }
        }

        private static IReadOnlyList<string> EquipmentTags(RecruitState recruit)
        {
            var result = new List<string>();
            for (var assignment = 0; assignment < recruit.Equipment.Assignments.Count; assignment++)
                for (var tag = 0; tag < recruit.Equipment.Assignments[assignment].Item.EquipmentTags.Count; tag++)
                    if (!result.Contains(recruit.Equipment.Assignments[assignment].Item.EquipmentTags[tag]))
                        result.Add(recruit.Equipment.Assignments[assignment].Item.EquipmentTags[tag]);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static void ValidateIndividualMp(BattleUnionState union, BattleForecastState forecast)
        {
            for (var i = 0; i < forecast.MemberActions.Count; i++)
            {
                var action = forecast.MemberActions[i];
                var memberIndex = union.FindMemberIndex(action.ActorMemberId);
                if (memberIndex >= 0 && action.PersonalMpCost > union.Members[memberIndex].CurrentMp)
                    throw new InvalidOperationException("Individual MP budget exceeded for " + action.ActorMemberId + ".");
            }
        }

        private static void RecoverRoundResources(List<BattleUnionState> unions)
        {
            for (var i = 0; i < unions.Count; i++)
            {
                var union = unions[i];
                if (!IsActive(union)) continue;
                unions[i] = union.With(currentAp: Math.Min(union.MaximumAp, union.CurrentAp + 3));
            }
        }

        private static string ForecastBasisHash(BattleState battle)
            =>ExtendTitanHash161(battle,ForecastBasisHashBeforeTitan161(battle));

        private static string ForecastBasisHashBeforeTitan161(BattleState battle)
        {
            if (battle.SssBattleRuntime090 != null)
                return CanonicalJson.Sha256Hex(new
                {
                    battle.BattleId, battle.ContentVersion, battle.Round, battle.Outcome,
                    PlayerUnions = GameplayRngUnions090(battle.PlayerUnions, battle.BattleId),
                    EnemyUnions = GameplayRngUnions090(battle.EnemyUnions, battle.BattleId),
                    battle.EventLog,
                    battle.RoundRecords,
                    battle.TutorialBreakthroughMemberId, battle.TutorialBreakthroughArtId,
                    battle.TutorialBreakthroughOccurred,
                    battle.SssBattleRuntime090
                });
            return CanonicalJson.Sha256Hex(new
            {
                battle.BattleId, battle.ContentVersion, battle.Round, battle.Outcome,
                PlayerUnions = GameplayRngUnions090(battle.PlayerUnions, battle.BattleId),
                EnemyUnions = GameplayRngUnions090(battle.EnemyUnions, battle.BattleId),
                battle.EventLog,
                battle.RoundRecords,
                battle.TutorialBreakthroughMemberId, battle.TutorialBreakthroughArtId, battle.TutorialBreakthroughOccurred
            });
        }

        private static IReadOnlyList<BattleUnionState> GameplayRngUnions090(
            IReadOnlyList<BattleUnionState> source,
            string battleId)
        {
            var unions = new List<BattleUnionState>();
            if (source == null) return unions.AsReadOnly();
            // Saves created before Enemy Art 700 have no presentation identity on
            // any enemy member.  Their historical gameplay seed was the full
            // authority hash of that blank-art state, so preserve that state
            // exactly.  Once even one enemy carries an art identity this is a
            // current roster and every enemy is canonicalized together; a caller
            // cannot opt individual members out of presentation isolation by
            // blanking only part of a roster.
            var preserveWhollyLegacyEnemyRoster090 =
                IsWhollyLegacyEnemyRoster090(source);
            for (var unionIndex = 0; unionIndex < source.Count; unionIndex++)
            {
                var union = source[unionIndex];
                var members = new List<BattleMemberState>(union.Members.Count);
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    if (union.Side == BattleSide.Enemy &&
                        preserveWhollyLegacyEnemyRoster090)
                    {
                        members.Add(member);
                        continue;
                    }
                    string canonicalBaseId090 = null;
                    string canonicalVariantId090 = null;
                    var canonicalVisualSeed090 = 0;
                    if (union.Side == BattleSide.Enemy)
                        EnemyArtIdentity090.Resolve090(
                            battleId,
                            unionIndex,
                            memberIndex,
                            member,
                            out canonicalBaseId090,
                            out canonicalVariantId090,
                            out canonicalVisualSeed090);
                    members.Add(member.WithEnemyArt090(
                        canonicalBaseId090,
                        canonicalVariantId090,
                        canonicalVisualSeed090));
                }
                unions.Add(union.With(members: members.AsReadOnly()));
            }
            return unions.AsReadOnly();
        }

        private static bool IsWhollyLegacyEnemyRoster090(
            IReadOnlyList<BattleUnionState> source)
        {
            var foundEnemyMember090 = false;
            for (var unionIndex = 0; unionIndex < source.Count; unionIndex++)
            {
                var union = source[unionIndex];
                if (union.Side != BattleSide.Enemy) continue;
                for (var memberIndex = 0;
                     memberIndex < union.Members.Count;
                     memberIndex++)
                {
                    foundEnemyMember090 = true;
                    var member = union.Members[memberIndex];
                    if (!string.IsNullOrWhiteSpace(member.EnemyArtBaseId090) ||
                        !string.IsNullOrWhiteSpace(member.EnemyArtVariantId090) ||
                        member.VisualVariantSeed090 > 0)
                        return false;
                }
            }
            return foundEnemyMember090;
        }

        private static BattleEventState Event(int sequence, int round, string type, BattleSide side,
            string unionId, string memberId, string artId, string text, int amount,
            string actorUnionId = "", string actorMemberId = "",
            string targetUnionId = "", string targetMemberId = "") =>
            new BattleEventState(sequence, round, type, side, unionId, memberId, artId, text, amount,
                CanonicalJson.Sha256Hex(new
                {
                    sequence, round, type, side, unionId, memberId, artId, text, amount,
                    actorUnionId, actorMemberId, targetUnionId, targetMemberId
                }),
                actorUnionId, actorMemberId, targetUnionId, targetMemberId);

        internal static BattleEventState CreateSssEvent090(
            int sequence, int round, string type, BattleSide side,
            string unionId, string memberId, string artId, string text, int amount,
            string actorUnionId = "", string actorMemberId = "",
            string targetUnionId = "", string targetMemberId = "") =>
            Event(sequence, round, type, side, unionId, memberId, artId, text, amount,
                actorUnionId, actorMemberId, targetUnionId, targetMemberId);

        internal static int ResolveSssAttack090(
            int round,
            BattlePlannedActionState action,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events) =>
            ResolveAttack(round, action, players, enemies, events);

        private static string ValidateSelecting(CampaignState campaign)
        {
            if (campaign?.Battle == null) return "M2_ACTIVE_BATTLE_REQUIRED";
            if (campaign.Battle.Outcome != BattleOutcome.InProgress || campaign.Battle.Phase != BattlePhase.ForecastSelection)
                return "M2_BATTLE_NOT_ACCEPTING_FORECASTS";
            return null;
        }

        private static RecruitState FindRecruit(IReadOnlyList<RecruitState> recruits, string id)
        {
            for (var i = 0; i < recruits.Count; i++) if (StringComparer.Ordinal.Equals(recruits[i].RecruitId, id)) return recruits[i];
            throw new InvalidOperationException("Union recruit is missing: " + id + ".");
        }
        private static bool TrySelectTutorialBreakthrough(
            IReadOnlyList<BattleUnionState> unions,
            M2CombatContent content,
            out BattleMemberState selectedMember,
            out string selectedArtId)
        {
            selectedMember = null;
            selectedArtId = string.Empty;
            var classPreference = new[]
            {
                "CLASS_WARRIOR", "CLASS_RANGER", "CLASS_ROGUE", "CLASS_MAGE", "CLASS_GUARDIAN", "CLASS_PRIEST"
            };
            for (var classIndex = 0; classIndex < classPreference.Length; classIndex++)
                for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
                    for (var memberIndex = 0; memberIndex < unions[unionIndex].Members.Count; memberIndex++)
                    {
                        var member = unions[unionIndex].Members[memberIndex];
                        if (member.Downed || !StringComparer.Ordinal.Equals(member.ClassId, classPreference[classIndex])) continue;
                        var artId = TutorialBreakthroughArtId(member, content);
                        if (string.IsNullOrWhiteSpace(artId)) continue;
                        selectedMember = member;
                        selectedArtId = artId;
                        return true;
                    }
            for (var i = 0; i < unions.Count; i++)
                for (var j = 0; j < unions[i].Members.Count; j++)
                {
                    var member = unions[i].Members[j];
                    if (member.Downed) continue;
                    var artId = TutorialBreakthroughArtId(member, content);
                    if (string.IsNullOrWhiteSpace(artId)) continue;
                    selectedMember = member;
                    selectedArtId = artId;
                    return true;
                }
            return false;
        }

        private static string TutorialBreakthroughArtId(BattleMemberState member, M2CombatContent content)
        {
            var desiredDiscipline = TutorialDiscipline(member.ClassId);
            var desiredFamilies = TutorialFamilies(member.ClassId);
            var candidates = new List<M2ArtDefinition>();
            foreach (var value in content.Arts.Values)
            {
                if (!value.IsForecastAction ||
                    value.Id.StartsWith("ART_BASIC_", StringComparison.Ordinal) ||
                    StringComparer.Ordinal.Equals(value.Id, "ART_GUARD") ||
                    StringComparer.Ordinal.Equals(value.Id, "ART_RECOVER_BREATH") ||
                    StringComparer.Ordinal.Equals(value.Id, "ART_ASSIST_ALLY") ||
                    Contains(member.LearnedArtIds, value.Id) ||
                    !IsBreakthroughDiscipline(value.Discipline) ||
                    !IsEquipmentLegal(value, member.EquipmentTags) ||
                    value.PersonalMpCost > member.CurrentMp)
                    continue;
                candidates.Add(value);
            }
            candidates.Sort((left, right) =>
            {
                var comparison = (!StringComparer.Ordinal.Equals(left.Discipline, desiredDiscipline))
                    .CompareTo(!StringComparer.Ordinal.Equals(right.Discipline, desiredDiscipline));
                if (comparison != 0) return comparison;
                comparison = (!Contains(desiredFamilies, left.Family)).CompareTo(!Contains(desiredFamilies, right.Family));
                return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.Id, right.Id);
            });
            return candidates.Count == 0 ? string.Empty : candidates[0].Id;
        }

        private static string StartingClassId(string classOrTendencyId)
        {
            const string tendencyPrefix = "CLASS_TEND_";
            if (!string.IsNullOrWhiteSpace(classOrTendencyId) &&
                classOrTendencyId.StartsWith(tendencyPrefix, StringComparison.Ordinal))
                return "CLASS_" + classOrTendencyId.Substring(tendencyPrefix.Length);
            return classOrTendencyId ?? string.Empty;
        }

        private static string TutorialDiscipline(string classId)
        {
            switch (classId)
            {
                case "CLASS_GUARDIAN": return "Guard";
                case "CLASS_WARRIOR": return "Martial";
                case "CLASS_RANGER":
                case "CLASS_ROGUE": return "Tactical";
                case "CLASS_MAGE": return "Mystic";
                case "CLASS_PRIEST": return "Restoration";
                default: return "Martial";
            }
        }

        private static IReadOnlyList<string> TutorialFamilies(string classId)
        {
            switch (classId)
            {
                case "CLASS_GUARDIAN": return new[] { "GUARDIAN", "GUARD" };
                case "CLASS_WARRIOR": return new[] { "WARRIOR", "MARTIAL" };
                case "CLASS_RANGER": return new[] { "RANGER", "TACTICAL" };
                case "CLASS_ROGUE": return new[] { "ROGUE", "TACTICAL" };
                case "CLASS_MAGE": return new[] { "MAGE", "MYSTIC" };
                case "CLASS_PRIEST": return new[] { "PRIEST", "RESTORATION" };
                default: return Array.Empty<string>();
            }
        }

        private static bool IsBreakthroughDiscipline(string discipline) =>
            StringComparer.Ordinal.Equals(discipline, "Guard") ||
            StringComparer.Ordinal.Equals(discipline, "Martial") ||
            StringComparer.Ordinal.Equals(discipline, "Tactical") ||
            StringComparer.Ordinal.Equals(discipline, "Mystic") ||
            StringComparer.Ordinal.Equals(discipline, "Restoration");

        private static int FindGuardedTutorialCandidateUnionIndex(
            IReadOnlyList<BattleUnionState> players, BattleState battle)
        {
            if (battle.TutorialBreakthroughOccurred) return -1;
            for (var unionIndex = 0; unionIndex < players.Count; unionIndex++)
            {
                var memberIndex = players[unionIndex].FindMemberIndex(battle.TutorialBreakthroughMemberId);
                if (memberIndex >= 0 && players[unionIndex].Members[memberIndex].Guarding &&
                    !players[unionIndex].Members[memberIndex].Downed)
                    return unionIndex;
            }
            return -1;
        }
        private static BattleUnionState FirstActiveUnion(IReadOnlyList<BattleUnionState> unions)
        {
            for (var i = 0; i < unions.Count; i++) if (IsActive(unions[i])) return unions[i];
            return null;
        }
        private static BattleForecastState FindForecast(IReadOnlyList<BattleForecastState> values, string unionId, string forecastId)
        {
            for (var i = 0; i < values.Count; i++)
                if (StringComparer.Ordinal.Equals(values[i].UnionId, unionId) && StringComparer.Ordinal.Equals(values[i].ForecastId, forecastId)) return values[i];
            return null;
        }
        private static BattleForecastSelectionState FindSelection(IReadOnlyList<BattleForecastSelectionState> values, string unionId)
        {
            for (var i = 0; i < values.Count; i++) if (StringComparer.Ordinal.Equals(values[i].UnionId, unionId)) return values[i];
            return null;
        }
        private static int FindUnionIndex(IReadOnlyList<BattleUnionState> values, string unionId)
        {
            for (var i = 0; i < values.Count; i++) if (StringComparer.Ordinal.Equals(values[i].UnionId, unionId)) return i;
            return -1;
        }
        private static string FindMemberUnionId(IReadOnlyList<BattleUnionState> values, string memberId)
        {
            for (var unionIndex = 0; unionIndex < values.Count; unionIndex++)
                if (values[unionIndex].FindMemberIndex(memberId) >= 0) return values[unionIndex].UnionId;
            return string.Empty;
        }
        private static int FirstActiveIndex(IReadOnlyList<BattleUnionState> unions)
        {
            for (var i = 0; i < unions.Count; i++) if (IsActive(unions[i])) return i;
            return -1;
        }
        private static int FirstActiveMemberIndex(BattleUnionState union)
        {
            for (var i = 0; i < union.Members.Count; i++) if (!union.Members[i].Downed) return i;
            return -1;
        }
        private static int FirstGuardingMemberIndex(BattleUnionState union)
        {
            for (var i = 0; i < union.Members.Count; i++) if (!union.Members[i].Downed && union.Members[i].Guarding) return i;
            return -1;
        }
        private static int MostInjuredIndex(BattleUnionState union, bool includeDowned)
        {
            var best = -1; var missing = 0;
            for (var i = 0; i < union.Members.Count; i++)
            {
                var member = union.Members[i];
                if (member.Downed && !includeDowned) continue;
                var value = member.MaximumHp - member.CurrentHp;
                if (member.Downed) value += member.MaximumHp;
                if (value > missing) { missing = value; best = i; }
            }
            return best;
        }
        private static IReadOnlyList<int> ActiveIndices(IReadOnlyList<BattleUnionState> unions)
        {
            var result = new List<int>();
            for (var i = 0; i < unions.Count; i++) if (IsActive(unions[i])) result.Add(i);
            return result.AsReadOnly();
        }
        private static bool IsActive(BattleUnionState union) => !union.Retreated && !union.IsDefeated;
        private static int CountActive(IReadOnlyList<BattleUnionState> unions)
        {
            var result = 0; for (var i = 0; i < unions.Count; i++) if (IsActive(unions[i])) result++; return result;
        }
        private static bool AllDefeated(IReadOnlyList<BattleUnionState> unions)
        {
            if (unions.Count == 0) return true;
            for (var i = 0; i < unions.Count; i++) if (!unions[i].IsDefeated) return false;
            return true;
        }
        private static bool EnemyGuarding(IReadOnlyList<BattleUnionState> unions)
        {
            for (var i = 0; i < unions.Count; i++) if (unions[i].Guarding) return true; return false;
        }
        private static bool HasLegalClassArt(BattleUnionState union, string classId, string artId, M2CombatContent content)
        {
            for (var i = 0; i < union.Members.Count; i++)
                if (StringComparer.Ordinal.Equals(union.Members[i].ClassId, classId) && Contains(union.Members[i].LearnedArtIds, artId) &&
                    IsEquipmentLegal(content.Art(artId), union.Members[i].EquipmentTags)) return true;
            return false;
        }
        private static bool HasArtFamily(BattleUnionState union, M2CombatContent content, string discipline)
        {
            for (var i = 0; i < union.Members.Count; i++) for (var j = 0; j < union.Members[i].LearnedArtIds.Count; j++)
                if (content.Arts.TryGetValue(union.Members[i].LearnedArtIds[j], out var art) && art.IsForecastAction && StringComparer.Ordinal.Equals(art.Discipline, discipline)) return true;
            return false;
        }
        private static bool HasAffordableArtInDisciplines(
            BattleUnionState union,
            M2CombatContent content,
            int sharedAp,
            params string[] disciplines)
        {
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                if (member.Downed) continue;
                for (var artIndex = 0; artIndex < member.LearnedArtIds.Count; artIndex++)
                {
                    if (!content.Arts.TryGetValue(member.LearnedArtIds[artIndex], out var art)) continue;
                    if (!art.IsForecastAction || !Contains(disciplines, art.Discipline) || art.SharedApCost > sharedAp ||
                        art.PersonalMpCost > member.CurrentMp || !IsEquipmentLegal(art, member.EquipmentTags)) continue;
                    return true;
                }
            }
            return false;
        }

        private static bool HasAffordableFriendlyUnionArt(
            BattleUnionState union,
            M2CombatContent content,
            int sharedAp,
            BattleState battle = null)
        {
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var member = union.Members[memberIndex];
                if (member.Downed) continue;
                for (var artIndex = 0; artIndex < member.LearnedArtIds.Count; artIndex++)
                {
                    if (!content.Arts.TryGetValue(member.LearnedArtIds[artIndex], out var art) ||
                        !art.IsForecastAction || art.SharedApCost > sharedAp ||
                        art.PersonalMpCost > member.CurrentMp ||
                        (!StringComparer.Ordinal.Equals(art.Discipline, "Support") &&
                         !StringComparer.Ordinal.Equals(art.Discipline, "Recovery")) ||
                         !IsEquipmentLegal(art, member.EquipmentTags) ||
                         !SupportsFriendlyUnionTarget080(art)) continue;
                    if (IsSssOwnedGuestUnionScope090(art) &&
                        OwnedLivingGuestUnion090(
                            battle, battle?.PlayerUnions, member.MemberId,
                            union.UnionId) == null)
                        continue;
                    return true;
                }
            }
            return false;
        }

        public static bool SupportsFriendlyUnionTargetForVerification080(M2ArtDefinition art) =>
            SupportsFriendlyUnionTarget080(art);

        /// <summary>
        /// Read-only verification seam over the real Forecast candidate authority.
        /// This intentionally delegates to CandidateArtsForCommand so package and
        /// regression tests cannot certify a parallel approximation of legality.
        /// </summary>
        public static IReadOnlyList<string> CandidateArtsForVerification090(
            BattleMemberState member,
            string commandId,
            M2CombatContent content,
            int apRemaining,
            BattleState battle = null,
            string sourceUnionId = null) =>
            CandidateArtsForCommand(
                member, commandId, content, apRemaining,
                battle, sourceUnionId);

        public static bool IsRevivalArtForVerification080(M2ArtDefinition art) => IsRevivalArt080(art);

        private static bool SupportsFriendlyUnionTarget080(M2ArtDefinition art)
        {
            if (art == null) return false;
            if (StringComparer.Ordinal.Equals(art.TargetRule, "SELF_OR_ALLY_UNION") ||
                StringComparer.Ordinal.Equals(art.TargetRule, "ALLY_UNION") ||
                StringComparer.Ordinal.Equals(art.TargetRule, "ALLY_MEMBER")) return true;
            if (StringComparer.Ordinal.Equals(art.TargetRule, "SELF")) return false;

            // Restoration has always been allowed to leave the acting Union.  Legacy
            // authority uses the broad CONTEXTUAL rule, so its authored effects decide
            // whether Support Arts may do the same.
            if (StringComparer.Ordinal.Equals(art.Discipline, "Restoration")) return true;
            if (!StringComparer.Ordinal.Equals(art.Discipline, "Support") &&
                !StringComparer.Ordinal.Equals(art.Discipline, "Recovery")) return false;

            var selfOnly = HasAnyTag080(art, "AP_CONSERVE", "MP_CONSERVE") &&
                           !HasAnyTag080(art,
                               "ASSIST", "MINOR_MORALE", "MORALE_UP", "RESTORE_COHESION",
                               "CLEAR_FEAR", "MAGIC_BARRIER", "BARRIER", "BUFF_ALLY",
                               "CONCEAL", "REPOSITION", "AREA_PROTECTION", "COHESION_RESTORE");
            if (selfOnly) return false;
            if (HasAnyTag080(art,
                    "ASSIST", "MINOR_MORALE", "MORALE_UP", "RESTORE_COHESION",
                    "CLEAR_FEAR", "MAGIC_BARRIER", "BARRIER", "BUFF_ALLY",
                    "CONCEAL", "REPOSITION", "AREA_PROTECTION", "COHESION_RESTORE")) return true;
            return Contains(art.IntentTags, "SUPPORT") || Contains(art.IntentTags, "RESCUE") ||
                   Contains(art.IntentTags, "HEAL");
        }

        private static bool IsRevivalArt080(M2ArtDefinition art) =>
            art != null && (StringComparer.Ordinal.Equals(art.Id, "ART_STAND_AGAIN") ||
                            HasAnyTag080(art, "REVIVE"));

        private static bool IsSssAllEnemyUnionScope090(
            M2ArtDefinition art) =>
            art != null &&
            art.Id.StartsWith("SSS_", StringComparison.Ordinal) &&
            HasAnyTag080(art, "SSS_V4_SCOPE_ALL_ENEMY_UNIONS");

        private static bool IsSssAllAlliedUnionScope090(
            M2ArtDefinition art) =>
            art != null &&
            art.Id.StartsWith("SSS_", StringComparison.Ordinal) &&
            HasAnyTag080(art, "SSS_V4_SCOPE_ALL_ALLIED_UNIONS");

        private static bool IsSssOwnedGuestUnionScope090(
            M2ArtDefinition art) =>
            art != null &&
            StringComparer.Ordinal.Equals(
                art.Id, "SSS_ELYSIA_NIGHTCALL_ART_03") &&
            HasAnyTag080(art, "SSS_V4_SCOPE_OWNED_GUEST_UNION");

        private static BattleUnionState OwnedLivingGuestUnion090(
            BattleState battle,
            IReadOnlyList<BattleUnionState> friendlyUnions,
            string ownerHeroId,
            string sourceUnionId = null)
        {
            if (battle?.SssBattleRuntime090 == null ||
                friendlyUnions == null ||
                !StringComparer.Ordinal.Equals(
                    ownerHeroId, "SSS_ELYSIA_NIGHTCALL"))
                return null;

            if (string.IsNullOrWhiteSpace(sourceUnionId))
            {
                for (var unionIndex = 0;
                     unionIndex < friendlyUnions.Count;
                     unionIndex++)
                {
                    var candidate = friendlyUnions[unionIndex];
                    if (candidate.FindMemberIndex(ownerHeroId) < 0) continue;
                    sourceUnionId = candidate.UnionId;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(sourceUnionId)) return null;

            SssBattleGuestState090 owned = null;
            for (var guestIndex = 0;
                 guestIndex < battle.SssBattleRuntime090.Guests.Length;
                 guestIndex++)
            {
                var candidate = battle.SssBattleRuntime090.Guests[guestIndex];
                if (!candidate.Active ||
                    !StringComparer.Ordinal.Equals(
                        candidate.OwnerHeroId, ownerHeroId) ||
                    !StringComparer.Ordinal.Equals(
                        candidate.SourceUnionId, sourceUnionId))
                    continue;
                if (owned != null)
                    return null;
                owned = candidate;
            }
            if (owned == null ||
                StringComparer.Ordinal.Equals(
                    owned.GuestUnionId, sourceUnionId))
                return null;

            for (var unionIndex = 0;
                 unionIndex < friendlyUnions.Count;
                 unionIndex++)
            {
                var union = friendlyUnions[unionIndex];
                if (StringComparer.Ordinal.Equals(
                        union.UnionId, owned.GuestUnionId) &&
                    IsActive(union))
                    return union;
            }
            return null;
        }

        private static bool HasAnyTag080(M2ArtDefinition art, params string[] tags)
        {
            for (var index = 0; index < tags.Length; index++)
                if (Contains(art.EffectTags, tags[index]) || Contains(art.StatusTags, tags[index]))
                    return true;
            return false;
        }

        private static BattleUnionState TargetUnionForCommand(
            string commandId,
            BattleUnionState source,
            BattleState battle,
            M2CombatContent content)
        {
            if (commandId == "CMD_HEAL") return MostWoundedUnion(battle.PlayerUnions) ?? source;
            if (commandId == "CMD_SUPPORT")
            {
                var supportTarget = MostSupportNeedyUnion(battle.PlayerUnions);
                if (supportTarget == null) return source;
                if (StringComparer.Ordinal.Equals(supportTarget.UnionId, source.UnionId) ||
                    HasAffordableFriendlyUnionArt(
                        source, content, source.CurrentAp, battle))
                    return supportTarget;
                return source;
            }
            if (commandId == "CMD_AP_RECOVERY" ||
                commandId == "CMD_GUARD" || commandId == "CMD_RETREAT") return source;
            return FirstActiveUnion(battle.EnemyUnions);
        }

        private static BattleUnionState MostSupportNeedyUnion(IReadOnlyList<BattleUnionState> unions)
        {
            BattleUnionState best = null;
            var bestNeed = 0;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                if (!IsActive(union)) continue;
                var cohesionNeed = Math.Max(0, 100 - union.Cohesion);
                var formationNeed = Math.Max(0, 10000 - union.FormationConditionBasisPoints);
                var need = (union.Engagement == EngagementState.Broken
                               ? 2000000
                               : 0) +
                           cohesionNeed * 10000 + formationNeed;
                if (need > bestNeed || need == bestNeed && need > 0 && best != null &&
                    StringComparer.Ordinal.Compare(union.UnionId, best.UnionId) < 0)
                {
                    best = union;
                    bestNeed = need;
                }
            }
            return bestNeed > 0 ? best : null;
        }
        private static BattleUnionState MostWoundedUnion(IReadOnlyList<BattleUnionState> unions)
        {
            BattleUnionState best = null;
            long bestNeed = 0;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                // A fully Downed Union is defeated for turn order, but remains a valid
                // friendly Restoration/Revival target until it retreats.
                if (union.Retreated) continue;
                long need = 0;
                var currentHp = 0;
                var maximumHp = 0;
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    currentHp = checked(currentHp + member.CurrentHp);
                    maximumHp = checked(maximumHp + member.MaximumHp);
                    if (member.Downed) need += 10000000L;
                    else if (member.CurrentHp * 100 / member.MaximumHp <=
                             CrossUnionCriticalHpPercent086)
                        need += 1000000L;
                    else if (member.CurrentHp * 100 / member.MaximumHp <=
                             CrossUnionLowHpPercent086)
                        need += 10000L;
                    need += Math.Max(0, member.MaximumHp - member.CurrentHp);
                }
                if (maximumHp > 0 && currentHp * 100 / maximumHp <=
                    CrossUnionCriticalHpPercent086)
                    need += 500000L;
                if (need > bestNeed || need == bestNeed && need > 0 && best != null &&
                    StringComparer.Ordinal.Compare(union.UnionId, best.UnionId) < 0)
                {
                    best = union;
                    bestNeed = need;
                }
            }
            return bestNeed > 0 ? best : null;
        }
        private static int AverageHpPercent(BattleUnionState union)
        {
            var current = 0; var maximum = 0;
            for (var i = 0; i < union.Members.Count; i++) { current += union.Members[i].CurrentHp; maximum += union.Members[i].MaximumHp; }
            return maximum == 0 ? 0 : current * 100 / maximum;
        }
        private static int SumAp(IReadOnlyList<BattlePlannedActionState> values)
        { var sum = 0; for (var i = 0; i < values.Count; i++) sum += values[i].SharedApCost; return sum; }
        private static int SumMp(IReadOnlyList<BattlePlannedActionState> values)
        { var sum = 0; for (var i = 0; i < values.Count; i++) sum += values[i].PersonalMpCost; return sum; }
        private static bool Contains(IReadOnlyList<string> values, string value)
        { for (var i = 0; i < values.Count; i++) if (StringComparer.Ordinal.Equals(values[i], value)) return true; return false; }
        private static bool ContainsEvent(IReadOnlyList<BattleEventState> values, string type)
        { for (var i = 0; i < values.Count; i++) if (StringComparer.Ordinal.Equals(values[i].EventType, type)) return true; return false; }
        private static bool ContainsTutorialBreakthrough070(BattleState battle, IReadOnlyList<BattleEventState> values)
        {
            if (battle == null || string.IsNullOrWhiteSpace(battle.TutorialBreakthroughArtId)) return false;
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].EventType, "BREAKTHROUGH") &&
                    StringComparer.Ordinal.Equals(values[index].ArtId, battle.TutorialBreakthroughArtId) &&
                    StringComparer.Ordinal.Equals(values[index].ActorMemberId, battle.TutorialBreakthroughMemberId))
                    return true;
            return false;
        }
        private static bool IsPredictedMeaningful(
            BattleActionKind kind,
            int hp,
            BattleMemberState target,
            BattleUnionState targetUnion,
            BattleMemberState actor,
            BattleUnionState sourceUnion,
            string commandId)
        {
            if (kind == BattleActionKind.Guard) return targetUnion != null && !targetUnion.IsDefeated;
            if (kind == BattleActionKind.Restoration) return target != null && (target.Downed || target.CurrentHp < target.MaximumHp);
            if (kind == BattleActionKind.Recovery) return actor.CurrentMp < actor.MaximumMp;
            return hp < 0 && target != null && !target.Downed;
        }
        private static bool HasOtherGuarding(IReadOnlyList<BattleMemberState> members, int excluded)
        { for (var i = 0; i < members.Count; i++) if (i != excluded && members[i].Guarding && !members[i].Downed) return true; return false; }
        private static void SetMemberGuarding(List<BattleUnionState> unions, int unionIndex, int memberIndex, bool value)
        {
            var union = unions[unionIndex]; var members = new List<BattleMemberState>(union.Members);
            members[memberIndex] = members[memberIndex].With(guarding: value); unions[unionIndex] = union.With(members: members.AsReadOnly());
        }
        private static void SetMemberMp(List<BattleUnionState> unions, int unionIndex, int memberIndex, int value)
        {
            var union = unions[unionIndex]; var members = new List<BattleMemberState>(union.Members);
            members[memberIndex] = members[memberIndex].With(currentMp: value); unions[unionIndex] = union.With(members: members.AsReadOnly());
        }
        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();

        private static int ScaleGrowth(int baseGrowth, int modePercent)
        {
            if (baseGrowth <= 0 || modePercent <= 0) return 0;
            var value = checked((long)baseGrowth * modePercent / 100L);
            return checked((int)Math.Max(1L, Math.Min(int.MaxValue, value)));
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value)) values.Add(value);
        }

        private static int FindRecruitIndex(IReadOnlyList<RecruitState> recruits, string id)
        {
            for (var index = 0; index < recruits.Count; index++)
                if (StringComparer.Ordinal.Equals(recruits[index].RecruitId, id)) return index;
            return -1;
        }

        private static bool ContainsInventoryItem(
            IReadOnlyList<EquipmentItemState> inventory,
            string itemInstanceId)
        {
            for (var index = 0; index < inventory.Count; index++)
                if (StringComparer.Ordinal.Equals(inventory[index].InstanceId, itemInstanceId)) return true;
            return false;
        }

        private static bool IsEquipmentItemAssigned(
            IReadOnlyList<RecruitState> recruits,
            string itemInstanceId)
        {
            for (var recruitIndex = 0; recruitIndex < recruits.Count; recruitIndex++)
                for (var assignmentIndex = 0;
                     assignmentIndex < recruits[recruitIndex].Equipment.Assignments.Count;
                     assignmentIndex++)
                    if (StringComparer.Ordinal.Equals(
                            recruits[recruitIndex].Equipment.Assignments[assignmentIndex].Item.InstanceId,
                            itemInstanceId))
                        return true;
            return false;
        }

        private static BattleMemberState FindBattleMember(
            IReadOnlyList<BattleUnionState> unions,
            string memberId)
        {
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var memberIndex = unions[unionIndex].FindMemberIndex(memberId);
                if (memberIndex >= 0) return unions[unionIndex].Members[memberIndex];
            }
            throw new InvalidOperationException("Battle reward member is missing: " + memberId + ".");
        }

        private static void ValidateMemberRewardProjection(
            BattleMemberRewardState reward,
            RecruitProgressionState previous,
            RecruitProgressionState projected)
        {
            if (reward.PreviousLevel != previous.Level || reward.ProjectedLevel != projected.Level ||
                reward.MaximumHpGain != projected.MaximumHpBonus - previous.MaximumHpBonus ||
                reward.MaximumMpGain != projected.MaximumMpBonus - previous.MaximumMpBonus ||
                reward.StrengthGain != projected.StrengthBonus - previous.StrengthBonus ||
                reward.DefenseGain != projected.DefenseBonus - previous.DefenseBonus ||
                reward.AgilityGain != projected.AgilityBonus - previous.AgilityBonus ||
                reward.MagicGain != projected.MagicBonus - previous.MagicBonus ||
                reward.WillGain != projected.WillBonus - previous.WillBonus)
            {
                throw new InvalidOperationException(
                    "Pending reward no longer matches recruit progression for " + reward.MemberId + ".");
            }
        }

        private static string EffectiveContentVersion(M2CombatContent content) =>
            EffectiveContentVersion(content, TutorialRulesVersion);

        private static string EffectiveContentVersion(
            M2CombatContent content,
            string rulesVersion) =>
            content.ContentVersion + "|" + rulesVersion;
    }

    public static class M2MeaningfulUse
    {
        public static int PersonalProgressGain(BattleActionKind kind, bool meaningful, int hpDelta)
        {
            if (!meaningful) return 0;
            var baseProgress = kind == BattleActionKind.Restoration ? Math.Max(4, hpDelta / 5) :
                kind == BattleActionKind.Guard ? 7 : kind == BattleActionKind.Recovery ? 3 : Math.Max(5, -hpDelta / 5);
            return baseProgress;
        }

        public static int UnionProgressGain(bool meaningful) => meaningful ? 4 : 0;
    }
}
