using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private static readonly Color BattleHp = new Color(0.35f, 0.88f, 0.51f, 1f);
        private static readonly Color BattleMp = new Color(0.36f, 0.66f, 1f, 1f);
        private static readonly Color BattleAp = new Color(1f, 0.76f, 0.28f, 1f);
        private static readonly Color BattleCohesion = new Color(0.35f, 0.90f, 0.94f, 1f);
        private static readonly Color BattleEnemy = new Color(0.94f, 0.31f, 0.38f, 1f);
        private string _expandedBattleCommandUnionId;

        private void BeginTutorialBattle()
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null) return;
            _expandedBattleCommandUnionId = null;
            _battleEntranceSeen = false;
            var result = m2.StartTutorialBattle();
            _localStatus = result.Message;
            _localStatusPositive = result.Succeeded;
            if (result.Succeeded) Navigate(M1Screen.Battle);
            else BuildCurrentScreen();
        }

        private void BuildBattle()
        {
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null)
            {
                Navigate(M1Screen.Complete);
                return;
            }
            if (UsesFirstHourBattleExperience072(battle))
            {
                EnterFirstHourBattleExperience072();
                return;
            }
            if (_battleResolving)
            {
                BuildBattleResolution(battle);
                return;
            }
            if (battle.IsResolved)
            {
                Navigate(M1Screen.BattleResults);
                return;
            }

            BuildCinematicBattleScreen(battle);
        }

        private void AddBattleCommandGuide(Transform parent, M2BattleView battle)
        {
            var active = battle.PlayerUnions.Where(value => value.CanAct).ToArray();
            var ready = active.Count(value => value.IsSelected);
            var focus = active.FirstOrDefault(value =>
                            StringComparer.Ordinal.Equals(value.UnionId, _expandedBattleCommandUnionId)) ??
                        active.FirstOrDefault(value => !value.IsSelected);
            var panel = RuntimeUi.AddPanel(parent, "Union Command Phase Guide", new Color(0.035f, 0.105f, 0.16f, 0.98f));
            RuntimeUi.SetLayout(panel, preferredHeight: 178f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(28, 28, 16, 16), 3f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(panel.transform, "Command Phase Progress",
                "COMMAND PHASE · " + ready + " OF " + active.Length + " UNIONS READY",
                38, TextAnchor.MiddleCenter, BattleAp, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Current Union Instruction",
                focus == null
                    ? "Review the selected commands, then confirm the round."
                    : "Choose a command for " + focus.DisplayName + ". Everyone listed beneath it will automatically use the predicted Arts.",
                33, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Battle Objective Reminder", "OBJECTIVE · " + battle.Objective,
                29, TextAnchor.MiddleCenter, RuntimeUi.MutedText);
        }

        private void BuildBattleResolution(M2BattleView battle)
        {
            BuildCinematicResolutionScreen(_battlePresentationBefore ?? battle);
        }

        private void AddBattlefieldCommandOverview(Transform parent, M2BattleView battle)
        {
            var row = AddRow(parent, "Battlefield Command Overview", 16f, 164f);
            var players = RuntimeUi.AddPanel(row, "Command Overview Player Side", new Color(0.035f, 0.12f, 0.19f, 0.98f));
            RuntimeUi.SetLayout(players, preferredHeight: 164f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(players, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(players.transform, new RectOffset(20, 20, 10, 10), 2f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(players.transform, "Command Overview Player Label", "YOUR UNIONS", 27,
                TextAnchor.MiddleCenter, BattleCohesion, FontStyle.Bold);
            foreach (var union in battle.PlayerUnions.Where(value => value.CanAct))
                RuntimeUi.AddText(players.transform, "Command Overview Player Union " + union.UnionId,
                    string.Join(" ", union.Members.Select(value => value.ClassSymbol)) + "  " + union.DisplayName +
                    "  ·  HP " + union.Members.Sum(value => value.CurrentHp) + "/" + union.Members.Sum(value => value.MaximumHp) +
                    "  ·  AP " + union.CurrentAp + "/" + union.MaximumAp,
                    24, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);

            var clash = RuntimeUi.AddPanel(row, "Command Overview Clash", new Color(0.14f, 0.075f, 0.035f, 0.98f));
            RuntimeUi.SetLayout(clash, preferredWidth: 210f, preferredHeight: 164f, flexibleWidth: 0f);
            RuntimeUi.AddVerticalLayout(clash.transform, new RectOffset(8, 8, 8, 8), 0f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(clash.transform, "Command Overview Clash Icon", "⚔", 74,
                TextAnchor.MiddleCenter, BattleAp, FontStyle.Bold);

            var enemies = RuntimeUi.AddPanel(row, "Command Overview Enemy Side", new Color(0.18f, 0.04f, 0.065f, 0.98f));
            RuntimeUi.SetLayout(enemies, preferredHeight: 164f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(enemies, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(enemies.transform, new RectOffset(20, 20, 10, 10), 2f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(enemies.transform, "Command Overview Enemy Label", "ENEMY", 27,
                TextAnchor.MiddleCenter, BattleEnemy, FontStyle.Bold);
            foreach (var union in battle.EnemyUnions.Where(value => value.CanAct))
                RuntimeUi.AddText(enemies.transform, "Command Overview Enemy Union " + union.UnionId,
                    string.Join(" ", union.Members.Select(value => value.ClassSymbol)) + "  " + union.DisplayName +
                    "  ·  HP " + union.Members.Sum(value => value.CurrentHp) + "/" + union.Members.Sum(value => value.MaximumHp),
                    24, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
        }

        private void AddBattleObjectiveStrip(Transform parent, M2BattleView battle)
        {
            var panel = RuntimeUi.AddPanel(parent, "Battle Objective Strip", RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel, preferredHeight: 150f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(28, 28, 18, 18), 4f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(panel.transform, "Objective Label", "OBJECTIVE · DEFEAT OR SAFELY WITHDRAW", RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Round State", "ROUND " + battle.Round + "  ·  " + battle.Outcome.ToUpperInvariant(),
                RuntimeUi.SmallBodyFontPixels, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
        }

        private void AddBattleArena(Transform parent, M2BattleView battle)
        {
            var arena = RuntimeUi.AddPanel(parent, "Cinematic Battle Arena", new Color(0.018f, 0.04f, 0.075f, 0.98f));
            RuntimeUi.SetLayout(arena, preferredHeight: 940f);
            M1PremiumUi.StylePanel(arena, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(arena.transform, new RectOffset(32, 32, 24, 24), 16f);
            M1PremiumUi.AddSectionDivider(arena.transform, "SEPARATE CINEMATIC ARENA · NO GRID", "◇");

            var stage = AddRow(arena.transform, "Non Grid Union Staging", 26f, 650f);
            var playerStage = RuntimeUi.AddPanel(stage, "Player Union Cinematic Stage", new Color(0.04f, 0.14f, 0.22f, 0.98f));
            RuntimeUi.SetLayout(playerStage, preferredHeight: 640f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(playerStage, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(playerStage.transform, new RectOffset(24, 24, 20, 20), 12f);
            RuntimeUi.AddText(playerStage.transform, "Player Force Label", "YOUR UNIONS", RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter, BattleCohesion, FontStyle.Bold);
            foreach (var union in battle.PlayerUnions) AddUnionRail(playerStage.transform, union, false);
            _battleStagePlayer = playerStage.rectTransform;

            var versus = RuntimeUi.AddPanel(stage, "Cinematic Clash Axis", new Color(0.13f, 0.08f, 0.06f, 0.96f));
            RuntimeUi.SetLayout(versus, preferredWidth: 210f, preferredHeight: 640f, flexibleWidth: 0f);
            RuntimeUi.AddVerticalLayout(versus.transform, new RectOffset(12, 12, 22, 22), 12f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(versus.transform, "Cinematic Axis Icon", "⚔", 106, TextAnchor.MiddleCenter, BattleAp, FontStyle.Bold);
            _battleEventBanner = RuntimeUi.AddText(versus.transform, "Cinematic Event Banner", "COMMANDS\nREADY", RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);

            var enemyStage = RuntimeUi.AddPanel(stage, "Enemy Union Cinematic Stage", new Color(0.19f, 0.045f, 0.065f, 0.98f));
            RuntimeUi.SetLayout(enemyStage, preferredHeight: 640f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(enemyStage, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(enemyStage.transform, new RectOffset(24, 24, 20, 20), 12f);
            RuntimeUi.AddText(enemyStage.transform, "Enemy Force Label", "ENEMY UNION", RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter, BattleEnemy, FontStyle.Bold);
            foreach (var union in battle.EnemyUnions) AddUnionRail(enemyStage.transform, union, true);
            _battleStageEnemy = enemyStage.rectTransform;
        }

        private static void AddUnionRail(Transform parent, M2BattleUnionView union, bool enemy)
        {
            var panel = RuntimeUi.AddPanel(parent, (enemy ? "Enemy" : "Player") + " Resource Rail " + union.UnionId, RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel, preferredHeight: 245f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(18, 18, 12, 12), 4f);
            RuntimeUi.AddText(panel.transform, "Union Name", union.DisplayName, RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter, enemy ? BattleEnemy : BattleCohesion, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Union Shared Resources",
                "AP " + union.CurrentAp + "/" + union.MaximumAp + "   ·   COH " + union.Cohesion +
                "   ·   FORM " + union.FormationConditionPercent + "%   ·   " + union.Engagement.ToUpperInvariant() +
                "   ·   UNION TRAINING " + union.UnionDisciplinePoints,
                34, TextAnchor.MiddleCenter, BattleAp, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Formation State", union.FormationStatus, 30, TextAnchor.MiddleCenter,
                union.FormationBenefitActive ? RuntimeUi.Positive : RuntimeUi.Warning, FontStyle.Bold);
            foreach (var member in union.Members)
            {
                RuntimeUi.AddText(panel.transform, "Member HP MP Readout " + member.MemberId,
                    member.ClassSymbol + "  " + member.DisplayName + "  ·  HP " + member.CurrentHp + "/" + member.MaximumHp +
                    "  ·  MP " + member.CurrentMp + "/" + member.MaximumMp +
                    (member.Downed ? member.Stabilized ? "  ·  DOWNED · STABLE" : "  ·  DOWNED" : string.Empty) +
                    (string.IsNullOrWhiteSpace(member.ArtGrowthSummary) ? string.Empty : "  ·  " + member.ArtGrowthSummary),
                    25, TextAnchor.MiddleCenter,
                    enemy ? RuntimeUi.Text : M1PremiumUi.ClassColor(member.ClassName), FontStyle.Bold);
            }
        }

        private void AddBattleSpeedControls(Transform parent)
        {
            var row = AddRow(parent, "Battle Resolution Speed", 14f, RuntimeUi.MinimumTouchPixels);
            RuntimeUi.AddButton(row, "Resolution Speed 1x", "1×", () => { _battleAnimationSpeed = 1f; BuildCurrentScreen(); },
                RuntimeUi.MinimumTouchPixels, Mathf.Approximately(_battleAnimationSpeed, 1f) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            RuntimeUi.AddButton(row, "Resolution Speed 2x", "2×", () => { _battleAnimationSpeed = 2f; BuildCurrentScreen(); },
                RuntimeUi.MinimumTouchPixels, Mathf.Approximately(_battleAnimationSpeed, 2f) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            RuntimeUi.AddButton(row, "Resolution Speed 4x", "4×", () => { _battleAnimationSpeed = 4f; BuildCurrentScreen(); },
                RuntimeUi.MinimumTouchPixels, Mathf.Approximately(_battleAnimationSpeed, 4f) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            var resolve = RuntimeUi.AddButton(row, "Resolve Already Seen Animation", "RESOLVE NOW", () => _skipBattleAnimation = true,
                RuntimeUi.MinimumTouchPixels, RuntimeUi.Warning);
            resolve.interactable = _battleAnimationSeen && _battleResolving;
            RuntimeUi.AddButton(row, "Reduced Motion Toggle", _reducedMotion ? "REDUCED MOTION: ON" : "REDUCED MOTION: OFF", () =>
            {
                _reducedMotion = !_reducedMotion;
                BuildCurrentScreen();
            }, RuntimeUi.MinimumTouchPixels, _reducedMotion ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
        }

        private void AddForecastGroup(Transform parent, M2BattleView battle, M2BattleUnionView union)
        {
            var forecasts = battle.Forecasts.Where(value => StringComparer.Ordinal.Equals(value.UnionId, union.UnionId)).ToArray();
            var selected = forecasts.FirstOrDefault(value => value.IsSelected);
            if (selected != null && !StringComparer.Ordinal.Equals(_expandedBattleCommandUnionId, union.UnionId))
            {
                AddSelectedUnionCommand(parent, union, selected);
                return;
            }

            M1PremiumUi.AddSectionDivider(parent,
                union.DisplayName + " · CHOOSE A COMMAND",
                "◆");
            RuntimeUi.AddText(parent, "Active Union Command Resources " + union.UnionId,
                "SHARED AP " + union.CurrentAp + "/" + union.MaximumAp + "  ·  COHESION " + union.Cohesion +
                "  ·  FORMATION " + union.FormationConditionPercent + "%  ·  " + union.Engagement.ToUpperInvariant(),
                32, TextAnchor.MiddleCenter, BattleAp, FontStyle.Bold);
            for (var index = 0; index < forecasts.Length; index += 3)
            {
                var row = AddRow(parent, "Forecast Card Row " + union.UnionId + " " + index, 18f, 700f);
                AddForecastCard(row, forecasts[index]);
                if (index + 1 < forecasts.Length) AddForecastCard(row, forecasts[index + 1]);
                else AddForecastCardSpacer(row);
                if (index + 2 < forecasts.Length) AddForecastCard(row, forecasts[index + 2]);
                else AddForecastCardSpacer(row);
            }
        }

        private void AddSelectedUnionCommand(Transform parent, M2BattleUnionView union, M2ForecastView forecast)
        {
            M1PremiumUi.AddSectionDivider(parent, union.DisplayName + " · COMMAND READY", "✓");
            var row = AddRow(parent, "Selected Union Command Summary " + union.UnionId, 18f, 190f);
            var panel = RuntimeUi.AddPanel(row, "Selected Complete Forecast " + forecast.ForecastId,
                new Color(0.13f, 0.12f, 0.055f, 0.98f));
            RuntimeUi.SetLayout(panel, preferredHeight: 190f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Warning);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(28, 28, 16, 16), 3f, TextAnchor.MiddleLeft);
            RuntimeUi.AddText(panel.transform, "Selected Command Label", PlayerCommandLabel(forecast),
                40, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Selected Command Plan",
                "AP " + forecast.SharedApCost + "  ·  PREDICTED MP " + forecast.CombinedMpCost +
                "  ·  TARGET " + forecast.TargetName + "\n" + forecast.ExpectedEffect,
                30, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            var change = RuntimeUi.AddButton(row, "Change Union Command " + union.UnionId, "CHANGE COMMAND", () =>
            {
                _expandedBattleCommandUnionId = union.UnionId;
                BuildCurrentScreen();
            }, RuntimeUi.MinimumTouchPixels, RuntimeUi.ButtonNormal);
            RuntimeUi.SetLayout(change, preferredWidth: 430f, preferredHeight: 190f, flexibleWidth: 0f);
        }

        private static void AddForecastCardSpacer(Transform parent)
        {
            var spacer = RuntimeUi.AddPanel(parent, "Forecast Card Spacer", Color.clear);
            RuntimeUi.SetLayout(spacer, preferredHeight: 680f, flexibleWidth: 1f);
        }

        private void AddForecastCard(Transform parent, M2ForecastView forecast)
        {
            var card = RuntimeUi.AddPanel(parent, "Complete Forecast Card " + forecast.ForecastId,
                forecast.IsSelected ? new Color(0.22f, 0.17f, 0.07f, 0.98f) : RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(card, preferredHeight: 680f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(card, forecast.IsSelected ? M1PremiumUi.Surface.Warning : M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(card.transform, new RectOffset(22, 22, 18, 18), 5f);
            var button = RuntimeUi.AddButton(card.transform, "Select Complete Forecast " + forecast.ForecastId,
                PlayerCommandLabel(forecast),
                () => SelectCompleteForecast(forecast.UnionId, forecast.ForecastId), RuntimeUi.MinimumTouchPixels,
                forecast.IsSelected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            button.interactable = !_battleResolving;
            RuntimeUi.AddText(card.transform, "Forecast Command Name",
                forecast.CommandName.ToUpperInvariant() + " · " + forecast.TacticalIntent.ToUpperInvariant(),
                30, TextAnchor.MiddleCenter, forecast.IsSelected ? RuntimeUi.Warning : RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.AddText(card.transform, "Forecast Phrase", "“" + forecast.Phrase + "”  ·  TARGET: " + forecast.TargetName,
                29, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Italic);
            RuntimeUi.AddText(card.transform, "Forecast Resource Cost",
                "SHARED AP " + forecast.SharedApCost + (forecast.ApRecovery > 0 ? "  ·  RECOVER +" + forecast.ApRecovery : string.Empty) +
                "  ·  PREDICTED MP TOTAL " + forecast.CombinedMpCost,
                32, TextAnchor.MiddleCenter, BattleAp, FontStyle.Bold);

            var actions = RuntimeUi.AddPanel(card.transform, "Predicted Member Actions Not Clickable", new Color(0.025f, 0.055f, 0.09f, 0.94f));
            RuntimeUi.SetLayout(actions, preferredHeight: 70f + Math.Max(1, forecast.MemberActions.Count) * 64f);
            RuntimeUi.AddVerticalLayout(actions.transform, new RectOffset(14, 14, 8, 8), 2f);
            RuntimeUi.AddText(actions.transform, "Prediction Label", "PREDICTED MEMBER ACTIONS · DESCRIPTIONS ONLY", 27,
                TextAnchor.MiddleLeft, BattleMp, FontStyle.Bold);
            foreach (var action in forecast.MemberActions)
            {
                RuntimeUi.AddText(actions.transform, "Predicted Action Description " + action.ActorName,
                    PredictedActionDescription(action),
                    25, TextAnchor.MiddleLeft, action.BreakthroughOpportunity ? RuntimeUi.Warning : RuntimeUi.Text,
                    action.BreakthroughOpportunity ? FontStyle.Bold : FontStyle.Normal);
            }

            RuntimeUi.AddText(card.transform, "Forecast Expected Effect", "EXPECTED · " + forecast.ExpectedEffect,
                27, TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold);
            RuntimeUi.AddText(card.transform, "Forecast Learning", "LEARNING · " + forecast.LearningOpportunity,
                26, TextAnchor.MiddleLeft, BattleCohesion, FontStyle.Bold);
            RuntimeUi.AddText(card.transform, "Forecast Risk And Fallback",
                "RISK · " + forecast.Risk + "\nIF CONDITIONS CHANGE · " + forecast.FallbackBehavior,
                23, TextAnchor.MiddleLeft, RuntimeUi.MutedText);
        }

        private static string PlayerCommandLabel(M2ForecastView forecast)
        {
            return string.IsNullOrWhiteSpace(forecast?.CommandName)
                ? "CHOOSE COMMAND"
                : forecast.CommandName.ToUpperInvariant();
        }

        private static string PredictedActionDescription(M2PredictedActionView action)
        {
            var discipline = string.IsNullOrWhiteSpace(action.Discipline) ? string.Empty : " · " + action.Discipline.ToUpperInvariant();
            var growth = action.PredictedGrowth > 0 ? " · LEARNING +" + action.PredictedGrowth : string.Empty;
            var breakthrough = action.BreakthroughOpportunity
                ? " · ✦ NEW ART: " + (string.IsNullOrWhiteSpace(action.BreakthroughTargetArtName)
                    ? action.ArtName
                    : action.BreakthroughTargetArtName)
                : string.Empty;
            return action.ActorName + " → " + action.ArtName + discipline + " · TARGET " + action.TargetName.ToUpperInvariant() +
                   " · MP " + action.PersonalMpCost +
                   growth + breakthrough + "\n" + action.Prediction;
        }

        private void SelectCompleteForecast(string unionId, string forecastId)
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null) return;
            if (_battleAudio != null) _battleAudio.PlayCue("SFX_COMMAND_SELECT");
            _expandedBattleCommandUnionId = null;
            var result = m2.SelectForecast(unionId, forecastId);
            _localStatus = result.Succeeded ? string.Empty : result.Message;
            _localStatusPositive = false;
            if (!result.Succeeded) BuildCurrentScreen();
        }

        private static void AddBattleLog(Transform parent, IReadOnlyList<M2BattleEventView> events)
        {
            M1PremiumUi.AddSectionDivider(parent, "BATTLE LOG", "◇");
            var panel = RuntimeUi.AddPanel(parent, "Readable Battle Event Log", RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel, preferredHeight: Math.Max(190f, 58f * (events?.Count ?? 0)));
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(24, 24, 16, 16), 4f);
            if (events == null || events.Count == 0)
            {
                RuntimeUi.AddText(panel.transform, "No Events", "No resolved events yet.", RuntimeUi.SmallBodyFontPixels,
                    TextAnchor.MiddleLeft, RuntimeUi.MutedText);
                return;
            }
            foreach (var item in events)
                RuntimeUi.AddText(panel.transform, "Battle Event " + item.Sequence,
                    item.Sequence.ToString("D2") + " · " + item.EventType.ToUpperInvariant() + " · " + item.Text,
                    31, TextAnchor.MiddleLeft, RuntimeUi.Text);
        }

        private void BeginResolveRound()
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null || _battleResolving) return;
            if (_battleAudio != null) _battleAudio.PlayCue("SFX_COMMAND_CONFIRM");
            _battleResolving = true;
            _battleAnimationPaused = false;
            _skipBattleAnimation = false;
            _skipCurrentBattleBeat = false;
            _battlePresentationBefore = SnapshotBattlePresentation(_coordinator.State.Battle);
            var result = m2.ConfirmBattleRound();
            _localStatus = result.Message;
            _localStatusPositive = result.Succeeded;
            if (!result.Succeeded)
            {
                _battleResolving = false;
                BuildCurrentScreen();
                return;
            }
            _battlePresentationAfter = SnapshotBattlePresentation(_coordinator.State.Battle);
            var newlyVisible = _battlePresentationAfter?.LastResolvedRoundEvents ?? Array.Empty<M2BattleEventView>();
            BuildCurrentScreen();
            StartCoroutine(PlayResolvedEventStaging(newlyVisible));
        }

        private static M2BattleView SnapshotBattlePresentation(M2BattleView source)
        {
            if (source == null) return null;
            return JsonConvert.DeserializeObject<M2BattleView>(JsonConvert.SerializeObject(source));
        }

        private IEnumerator PlayResolvedEventStaging(IReadOnlyList<M2BattleEventView> events)
        {
            yield return PlayCinematicResolvedRound(events);
        }

        private void BuildBattleResults()
        {
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null)
            {
                Navigate(M1Screen.Complete);
                return;
            }
            if (UsesFirstHourBattleExperience072(battle))
            {
                _screen = M1Screen.Battle;
                EnterFirstHourBattleExperience072();
                return;
            }
            BuildCinematicBattleResultScreen(battle);
        }

        private static bool UsesFirstHourBattleExperience072(M2BattleView battle)
        {
            // The 072 diorama and Union Forecast HUD are now the shipping battle
            // presentation, not a first-hour encounter special case. The former
            // allow-list silently sent every later story and Tower battle back to
            // the legacy screen. Keep only the explicit internal training fixture
            // on its legacy test presenter; every real battle takes the shipping path.
            return battle != null &&
                   !string.IsNullOrWhiteSpace(battle.BattleId) &&
                   !StringComparer.Ordinal.Equals(
                       battle.BattleId,
                       "BATTLE_TUTORIAL_UNION_FORECAST_001");
        }

        private void VerifyBattleReplay()
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null) return;
            var result = m2.ReplayTutorialBattle();
            _localStatus = result.Message;
            _localStatusPositive = result.Succeeded;
            BuildCurrentScreen();
        }

        private void RetryBattle()
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null) return;
            _expandedBattleCommandUnionId = null;
            _battleEntranceSeen = false;
            var result = m2.RetryTutorialBattle();
            _localStatus = result.Message;
            _localStatusPositive = result.Succeeded;
            if (result.Succeeded) Navigate(M1Screen.Battle);
            else BuildCurrentScreen();
        }

        private static string ShortHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return "not available";
            return hash.Length <= 24 ? hash : hash.Substring(0, 12) + "…" + hash.Substring(hash.Length - 12);
        }
    }
}
