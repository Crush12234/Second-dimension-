using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.BoardTower001;
using SecondDimension.Presentation.GuildCity017D;
using BoardRoomAuthority081 = SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Player-facing rules for the compact quest board. Save authority keeps its
    /// authored node IDs; this projection deliberately exposes only places,
    /// characters, dice, consequences, and rewards.
    /// </summary>
    public static class BoardQuestRules081
    {
        public const int SuccessTarget081 = 7;
        public const int BonusTarget081 = 10;

        private static readonly string[] SpaceLabels081 =
        {
            "LEAVE HOME", "FOLLOW THE TRAIL", "FACE THE DANGER",
            "SAVE THE PEOPLE", "BRING THEM HOME"
        };

        public static string SpaceLabel081(int index) =>
            SpaceLabels081[Mathf.Clamp(index, 0, SpaceLabels081.Length - 1)];

        public static int GuildXpForOutcome081(string outcome)
        {
            return SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                .BoardGameCheckGuildXp081(outcome);
        }

        public static string OutcomeHeading081(string outcome)
        {
            switch ((outcome ?? string.Empty).ToUpperInvariant())
            {
                case "EXCEPTIONAL": return "PERFECT ROLL";
                case "FULL_SUCCESS": return "CLEAN SUCCESS";
                case "SUCCESS_WITH_COST": return "SUCCESS — WITH A COST";
                case "SETBACK": return "SETBACK — KEEP MOVING";
                default: return "HARD SETBACK — THE STORY CONTINUES";
            }
        }

        public static bool IsPositive081(string outcome) =>
            StringComparer.Ordinal.Equals(outcome, "EXCEPTIONAL") ||
            StringComparer.Ordinal.Equals(outcome, "FULL_SUCCESS") ||
            StringComparer.Ordinal.Equals(outcome, "SUCCESS_WITH_COST");

        public static bool CanSurfaceEnhancementRoomIdentity001(string boardRoomKind)
        {
            switch ((boardRoomKind ?? string.Empty).ToUpperInvariant())
            {
                case "MONSTER":
                case "TREASURE":
                case "SKILL":
                case "BLESSING":
                case "CAMPFIRE":
                case "DISCOVERY":
                    return true;
                default:
                    // FATE and STORY retain the exact authored event/anchor copy.
                    return false;
            }
        }

        public static string RoomTitle081(string roomKind)
        {
            switch ((roomKind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return "GUILD HALL DOOR";
                case "MONSTER": return "MONSTER ROOM";
                case "FATE": return "FATE ROOM";
                case "TREASURE": return "SUPPLY CACHE";
                case "SKILL": return "SKILL SHRINE";
                case "BLESSING": return "GUILD BLESSING";
                case "CAMPFIRE": return "CAMPFIRE";
                case "STORY": return "STORY ROOM";
                case "RETURN": return "THE WAY HOME";
                default: return "DISCOVERY ROOM";
            }
        }

        public static string RoomReward081(string roomKind)
        {
            switch ((roomKind ?? string.Empty).ToUpperInvariant())
            {
                case "MONSTER": return "SURPRISE  •  WIN THE SAME FULL UNION BATTLE FOR EQUIPMENT + XP";
                case "FATE": return "FATE CHECK  •  YOUR BEST TEAM ROLLS 2D6  •  EARN GUILD XP";
                case "TREASURE": return "SUPPLY CACHE  •  +1 SUPPLY  •  SALVAGE FOUND";
                case "SKILL": return "NEW QUEST SKILL  •  TRAILCRAFT +1 TO FUTURE ROLLS  •  +4 GUILD XP";
                case "BLESSING": return "QUEST BUFF  •  WAYGLASS FAVOR +1 TO FUTURE ROLLS  •  +1 SUPPLY  •  -2 FATIGUE";
                case "CAMPFIRE": return "PARTY RESTED  •  -3 FATIGUE";
                case "STORY": return "STORY FOUND  •  COMPLETE THE CLEAR OBJECTIVE";
                case "RETURN": return "QUEST COMPLETE  •  BRING THE REWARDS HOME";
                case "START": return "PLACE THE GUILD PAWN, THEN FLIP THE FIRST ROOM";
                default: return "NEW CLUE FOUND  •  -1 PRESSURE  •  THE NEXT MOVE IS SAFER";
            }
        }

        public static string RoomKind081(
            GuildCityPresentationState017D state,
            ExpeditionBoardNodeView074 node)
        {
            var authoredKind = node?.Kind ?? "START";
            if (node?.IsCurrent == true &&
                !StringComparer.Ordinal.Equals(authoredKind, "CAMP") &&
                !string.IsNullOrWhiteSpace(state?.Expedition?.CurrentEventId))
                authoredKind = "EVENT";
            return BoardRoomAuthority081.BoardRoomKind081(
                state?.Expedition?.ExpeditionId ??
                state?.Expedition?.BoardId ??
                "NEW_QUEST_081",
                node?.NodeId ?? "START",
                authoredKind);
        }

        public static bool QuestFailed081(GuildCityPresentationState017D state) =>
            StringComparer.OrdinalIgnoreCase.Equals(
                state?.Expedition?.Status,
                "Failed");

        /// <summary>
        /// The phone-simple mission layer never asks the player to interpret a
        /// route graph. It prefers the story-rich/rest stop path over safe or
        /// optional detours, then chooses within the same priority using the
        /// existing stable shuffle key; save IDs and authored topology remain
        /// unchanged.
        /// </summary>
        public static ExpeditionBoardNodeView074 AutomaticDestination081(
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var expedition = state?.Expedition;
            if (expedition == null || view?.Nodes == null) return null;
            var linked = new HashSet<string>(
                expedition.LinkedNodeIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            return view.Nodes
                .Where(value => value != null && linked.Contains(value.NodeId))
                .OrderBy(AutomaticDestinationPriority081)
                .ThenBy(value => BoardRoomAuthority081.BoardRoomShuffleKey081(
                    expedition.ExpeditionId,
                    expedition.CurrentNodeId,
                    value.NodeId), StringComparer.Ordinal)
                .ThenBy(value => value.NodeId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static int AutomaticDestinationPriority081(
            ExpeditionBoardNodeView074 destination)
        {
            var kind = destination?.Kind ?? string.Empty;
            var kindTokens = kind
                .Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (kindTokens.Any(token =>
                    StringComparer.OrdinalIgnoreCase.Equals(token, "OPTIONAL") ||
                    StringComparer.OrdinalIgnoreCase.Equals(token, "ELITE")))
                return 40;

            if (StringComparer.OrdinalIgnoreCase.Equals(kind, "CAMP") ||
                StringComparer.OrdinalIgnoreCase.Equals(kind, "MAIN_OBJECTIVE") ||
                StringComparer.OrdinalIgnoreCase.Equals(kind, "EXIT"))
                return 0;
            if (StringComparer.OrdinalIgnoreCase.Equals(kind, "EVENT") ||
                StringComparer.OrdinalIgnoreCase.Equals(kind, "SECONDARY_OBJECTIVE"))
                return 10;
            if (StringComparer.OrdinalIgnoreCase.Equals(kind, "SAFE_ROUTE"))
                return 30;
            return 20;
        }

        /// <summary>
        /// Automatic field checks spend pressure only when the partner produces
        /// a strictly better visible modifier. Ties use the free solo roll.
        /// </summary>
        public static bool ShouldUseBestTeam081(
            bool canTeamUp,
            int teamVisibleModifier,
            int fastVisibleModifier) =>
            canTeamUp && teamVisibleModifier > fastVisibleModifier;
    }

    public sealed partial class M1FlowPresenter
    {
        readonly HashSet<string> _scheduledBoardQuestChecks081 =
            new HashSet<string>(StringComparer.Ordinal);
        bool _boardQuestCommandRunning081;
        float _boardQuestActionUnlockAt081;
        string _boardQuestActionUnlockNode081 = string.Empty;
        Coroutine _boardQuestActionUnlockRoutine081;

        private sealed class BoardQuestCrew081
        {
            public GuildCityAssignmentView017D Lead;
            public GuildCityAssignmentView017D Partner;
            public string LeadClass;
            public string PartnerClass;
            public int FastModifier;
            public int TeamModifier;
            public int FastVisibleModifier;
            public int TeamVisibleModifier;
            public int FieldSkillBonus;
            public int BlessingBonus;
            public bool CanTeamUp;
        }

        private void BuildBoardQuestExperience081(
            Transform body,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            RuntimeUi.ClearChildren(_screenRoot);
            var root = RuntimeUi.AddPanel(
                _screenRoot,
                "Board Quest 081",
                new Color(0.006f, 0.012f, 0.020f, 1f));
            NormalizeExpeditionViewport076(root.rectTransform);
            _activePage = root.rectTransform;
            _activeContent = null;
            _activeScroll = null;

            var view = ExpeditionBoardProjection074.Build(state);
            NormalizeExpeditionDestination074(state.Expedition);
            BuildStudioExpeditionBackdrop076(root.transform, view);
            if (ShouldShowLanternPatrolCeremony076(state))
            {
                BuildLanternPatrolCeremony076(root.transform, state);
                FinalizeExpeditionViewport076(root.rectTransform);
                ScheduleExpeditionViewportFinalize076(root.rectTransform);
                return;
            }

            var back = RuntimeUi.AddButton(
                root.transform,
                "Expedition Return To Hall 074",
                "←  GUILD HALL",
                () =>
                {
                    _guildCityTab017D = "HALL";
                    BuildCurrentScreen();
                },
                70f,
                RuntimeUi.ButtonNormal);
            SetAnchors074(back.GetComponent<RectTransform>(),
                new Vector2(0.025f, 0.895f), new Vector2(0.145f, 0.975f));
            var backLabel = back.GetComponentInChildren<Text>();
            ConfigureAuthoredCompactText076(backLabel, 24, 30);
            backLabel.transform.SetAsLastSibling();

            BuildBoardQuestHeader081(root.transform, state, view);
            RectTransform actions;
            var fate165 = (coordinator as IQuestFateCoordinator165)?.PendingQuestFate165;
            if (fate165 != null)
                actions = BuildQuestFate165(root.transform,
                    (IQuestFateCoordinator165)coordinator, fate165);
            else if (ShouldShowBoardQuestCardResolution090(state) ||
                (!ShouldHoldBoardQuestAction081(state) &&
                 ShouldBuildBoardQuestCardDraft090(coordinator, state, view)))
                actions = BuildBoardQuestCardDraft090(
                    root.transform, coordinator, state);
            else
            {
                BuildBoardQuestScene081(root.transform, state, view);
                actions = BuildBoardQuestActions081(
                    root.transform, coordinator, state, view);
            }

            var selected = actions == null
                ? null
                : actions.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(value => value != null && value.interactable);
            if (selected != null)
            {
                selected.Select();
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(selected.gameObject);
            }
            else back.Select();

            FinalizeExpeditionViewport076(root.rectTransform);
            ScheduleExpeditionViewportFinalize076(root.rectTransform);
        }

        private static void BuildBoardQuestHeader081(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var header = RuntimeUi.AddPanel(
                parent,
                "Board Quest Header 081",
                new Color(0.004f, 0.012f, 0.022f, 0.94f));
            SetAnchors074(header.rectTransform,
                new Vector2(0.155f, 0.885f), new Vector2(0.975f, 0.985f));
            StyleNeutralBoardSurface091(header, 0.76f);

            var readyContractTitle = state?.Expedition == null
                ? state?.Contracts?
                    .FirstOrDefault(value => value != null && value.IsActive &&
                        StringComparer.Ordinal.Equals(value.BoardId, view.BoardId))
                    ?.DisplayName
                : null;
            var missionTitle = !string.IsNullOrWhiteSpace(readyContractTitle)
                ? readyContractTitle
                : view.BoardTitle ?? "GUILD MISSION";
            var chapter = RuntimeUi.AddText(
                header.transform,
                "Board Quest Chapter 081",
                "MISSION  •  " + missionTitle.ToUpperInvariant(),
                30,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            SetAnchors074(chapter.rectTransform,
                new Vector2(0.025f, 0.53f), new Vector2(0.76f, 0.95f));
            ConfigureAuthoredCompactText076(chapter, 20, 34);
            var roomNumber = state?.Expedition == null
                ? 0
                : ExpeditionBoardProjection074.StepNumberForNode074(
                    state.Expedition.CurrentNodeId);
            var fieldResources = "XP TO SPEND  " + (state?.TreasuryXp ?? 0) +
                                 (state?.Expedition == null
                                     ? "\nQUEST READY"
                                     : "\nROOM  " + roomNumber + " / 15");
            var resources = RuntimeUi.AddText(
                header.transform,
                "Board Quest Spendable XP Resources 081",
                fieldResources,
                22,
                TextAnchor.MiddleRight,
                RuntimeUi.Warning,
                FontStyle.Bold);
            SetAnchors074(resources.rectTransform,
                new Vector2(0.815f, 0.10f), new Vector2(0.975f, 0.95f));
            ConfigureAuthoredCompactText076(resources, 15, 23);

            var objective = RuntimeUi.AddText(
                header.transform,
                "Board Quest Goal 081",
                BoardQuestRules081.QuestFailed081(state)
                    ? "NEXT  •  RETURN SAFELY, KEEP BATTLE GROWTH, THEN REGROUP"
                    : "OBJECTIVE  •  " +
                      (view.StoryObjective ?? "Finish the mission and return home."),
                23,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors074(objective.rectTransform,
                new Vector2(0.025f, 0.04f), new Vector2(0.80f, 0.53f));
            ConfigureAuthoredCompactText076(objective, 17, 25);
        }

        private void BuildBoardQuestPath081(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var track = RuntimeUi.AddPanel(
                parent,
                "Board Quest Face Down Rooms 081",
                Color.clear);
            SetAnchors074(track.rectTransform,
                new Vector2(0.10f, 0.805f), new Vector2(0.90f, 0.875f));
            var line = RuntimeUi.AddPanel(track.transform,
                "Board Quest Waypoint Thread 091", new Color(0.75f, 0.62f, 0.36f, 0.48f));
            var lineLayout = line.gameObject.AddComponent<LayoutElement>();
            lineLayout.ignoreLayout = true;
            SetAnchors074(line.rectTransform, new Vector2(0.045f, 0.47f),
                new Vector2(0.955f, 0.50f));
            RuntimeUi.AddHorizontalLayout(
                track.transform,
                new RectOffset(6, 6, 2, 2),
                12f,
                TextAnchor.MiddleCenter);

            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent) ?? view.Nodes[0];
            var currentPhase = state?.Expedition == null
                ? 0
                : ExpeditionBoardProjection074.PhaseIndexForNode074(
                    state.Expedition.CurrentNodeId);
            var currentRoom = state?.Expedition == null
                ? 0
                : ExpeditionBoardProjection074.StepNumberForNode074(
                    state.Expedition.CurrentNodeId);
            const int visiblePhaseCount = 5;
            for (var index = 0; index < visiblePhaseCount; index++)
            {
                var complete = index < currentPhase;
                var currentOrNext = index == currentPhase;
                var phaseLabel = BoardQuestRules081.SpaceLabel081(index);
                var tile = RuntimeUi.AddPanel(
                    track.transform,
                    "Board Quest Space " + index + " 081",
                    new Color(0.006f, 0.012f, 0.022f, 0.72f));
                RuntimeUi.SetLayout(tile, preferredHeight: 42f, flexibleWidth: 1f);
                var label = RuntimeUi.AddText(
                    tile.transform,
                    "Board Quest Space Label " + index + " 081",
                    complete
                        ? "✓  " + phaseLabel
                        : currentOrNext
                            ? state?.Expedition == null
                                ? "◆  " + phaseLabel
                                : "◆  " + phaseLabel
                            : "○  " + phaseLabel,
                    21,
                    TextAnchor.MiddleCenter,
                    currentOrNext ? RuntimeUi.Warning : complete ? RuntimeUi.Positive : RuntimeUi.MutedText,
                    FontStyle.Bold);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(
                    currentOrNext ? 38f : 8f, 4f);
                label.rectTransform.offsetMax = new Vector2(-8f, -5f);
                ConfigureAuthoredCompactText076(label, 17, 23);
                if (currentOrNext)
                    BuildBoardAdventurePawn084(
                        tile.transform,
                        state?.Expedition == null
                            ? string.Empty
                            : "BOARD_QUEST_PAWN_086|" +
                              state.Expedition.ExpeditionId + "|" +
                              state.Expedition.CurrentNodeId,
                        true);
            }
        }

        private void BuildBoardQuestScene081(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var scene = RuntimeUi.AddPanel(
                parent,
                "Board Quest Scene 081",
                new Color(0.006f, 0.014f, 0.024f, 0.90f));
            SetAnchors074(scene.rectTransform,
                new Vector2(0.025f, 0.035f), new Vector2(0.715f, 0.790f));
            StyleNeutralBoardSurface091(scene);
            RuntimeUi.AddVerticalLayout(
                scene.transform,
                new RectOffset(20, 20, 10, 10),
                4f,
                TextAnchor.UpperLeft);

            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent) ?? view.Nodes[0];
            var roomKind = BoardQuestRules081.RoomKind081(state, current);
            var roomModule001 = SafeCommittedRoomModule001(state, current, roomKind);
            DecorateBoardAdventureRevealedCard084(scene.rectTransform, roomKind,
                visualLabelOverride091: StringComparer.Ordinal.Equals(roomKind, "TREASURE")
                    ? "SUPPLIES  •  SUPPLY CACHE" : null);
            AddBoardQuestText081(
                scene.transform,
                "Board Quest Place 081",
                BoardQuestRules081.QuestFailed081(state)
                    ? "QUEST FAILED  •  THE PARTY WITHDRAWS"
                    : state.Expedition == null
                        ? "READY TO BEGIN"
                        : "CARD REVEALED",
                20,
                26f,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AddBoardQuestText081(
                scene.transform,
                "Board Quest Scene Title 081",
                (roomModule001 == null || StringComparer.Ordinal.Equals(roomKind, "TREASURE")
                    ? BoardQuestRules081.RoomTitle081(roomKind)
                    : roomModule001.displayName).ToUpperInvariant(),
                38,
                48f,
                RuntimeUi.Text,
                FontStyle.Bold);
            AddBoardQuestText081(
                scene.transform,
                "Expedition Current Position Summary 074",
                CompactBoardQuestCopy081(
                    BoardQuestCurrentMomentSummary081(state, current), 190),
                23,
                58f,
                RuntimeUi.Text,
                FontStyle.Normal);

            var revealedRewardCopy = BoardQuestRules081.QuestFailed081(state)
                ? "DEFEAT  •  NO QUEST COMPLETION REWARD  •  EARNED BATTLE GROWTH IS KEPT"
                : state.Expedition?.HasCommittedCheckAtCurrentNode == true
                    ? "FATE RESOLVED  •  REWARD SAVED  •  MOVE AGAIN"
                    : BoardQuestRules081.RoomReward081(roomKind);
            var reveal = RuntimeUi.AddPanel(
                scene.transform,
                "Board Quest Revealed Reward 081",
                new Color(0.040f, 0.080f, 0.105f, 0.96f));
            RuntimeUi.SetLayout(reveal, preferredHeight: 66f);
            var revealGroup = reveal.gameObject.AddComponent<CanvasGroup>();
            var rewardPositive = !BoardQuestRules081.QuestFailed081(state) &&
                                 (state.Expedition?.HasCommittedCheckAtCurrentNode == true
                                     ? BoardQuestRules081.IsPositive081(
                                         state.Expedition.LastCheckOutcome)
                                     : !StringComparer.Ordinal.Equals(
                                         roomKind, "MONSTER"));
            StyleNeutralBoardSurface091(reveal, 0.70f);
            BuildBoardAdventureRewardToken084(
                reveal.transform,
                roomKind,
                state?.Expedition == null
                    ? string.Empty
                    : "BOARD_QUEST_REWARD_086|" +
                      state.Expedition.ExpeditionId + "|" +
                      state.Expedition.CurrentNodeId + "|" +
                      (state.Expedition.HasCommittedCheckAtCurrentNode
                          ? state.Expedition.LastCheckOutcome
                          : "ROOM"),
                rewardPositive,
                state?.Expedition?.HasCommittedCheckAtCurrentNode == true,
                revealGroup);
            var revealText = RuntimeUi.AddText(
                reveal.transform,
                "Board Quest Revealed Reward Copy 081",
                revealedRewardCopy,
                21,
                TextAnchor.MiddleLeft,
                rewardPositive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            Stretch(revealText.rectTransform);
            revealText.rectTransform.offsetMin = new Vector2(92f, 5f);
            revealText.rectTransform.offsetMax = new Vector2(-14f, -5f);
            ConfigureAuthoredCompactText076(revealText, 15, 23);

            var companion = ExpeditionBoardProjection074.CompanionStoryBeat076(state, current);
            var quotePanel = RuntimeUi.AddPanel(
                scene.transform,
                "Expedition Companion Story Beat 076",
                new Color(0.025f, 0.055f, 0.080f, 0.94f));
            RuntimeUi.SetLayout(quotePanel, preferredHeight: 104f);
            M1PremiumUi.StylePanel(quotePanel, M1PremiumUi.Surface.WorldPaper);
            var quote = RuntimeUi.AddText(
                quotePanel.transform,
                "Expedition Companion Story Beat Text 076",
                "GUILDMATE  •  " + CompactBoardQuestCopy081(companion, 150),
                21,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(quote.rectTransform);
            quote.rectTransform.offsetMin = new Vector2(14f, 6f);
            quote.rectTransform.offsetMax = new Vector2(-14f, -6f);
            ConfigureAuthoredCompactText076(quote, 17, 25);
            if (ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                    state.Expedition?.BoardId) &&
                (ExpeditionBoardProjection074.RequiresChapterTwoStoryAnchor079(
                     state.Expedition?.BoardId,
                     current.NodeId) ||
                 !ExpeditionBoardProjection074.ChapterTwoUsesRosterSpeakerIdentity079(
                     companion)))
                AddChapterTwoStoryIdentity079(
                    quotePanel.transform,
                    state.Expedition?.BoardId,
                    current.NodeId,
                    quote);
            else
                AddExpeditionCompanionIdentityChip078(
                    quotePanel.transform,
                    companion,
                    quote);

            if (state.Expedition?.HasCommittedCheckAtCurrentNode == true)
                BuildBoardQuestRollResult081(scene.transform, state.Expedition);
            else
                AddBoardQuestText081(
                    scene.transform,
                    "Board Quest Immediate Promise 081",
                    BoardQuestPromise081(view.ActionKind, state?.Expedition?.Status),
                    20,
                    52f,
                    RuntimeUi.Warning,
                    FontStyle.Bold);

            GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                scene.transform, "Board Quest Room Illustration 091",
                BoardRoomIllustrationResource091(roomKind), 360f);
            ArrangeIllustratedBoardCard091(scene.rectTransform,
                "Board Quest Room Illustration 091", false);

            if (state?.Expedition != null &&
                !string.IsNullOrWhiteSpace(state.Expedition.CurrentNodeId))
            {
                var roomNumber = ExpeditionBoardProjection074.StepNumberForNode074(
                    state.Expedition.CurrentNodeId);
                AnimateBoardAdventureCardFlip084(
                    scene.rectTransform,
                    "BOARD_QUEST_081|" + state.Expedition.ExpeditionId + "|" +
                    state.Expedition.CurrentNodeId,
                    "ROOM " + roomNumber + "  •  FACE DOWN");
            }
        }

        private static string CompactBoardQuestCopy081(string copy, int maximumLength)
        {
            var value = (copy ?? string.Empty).Replace('\n', ' ').Trim();
            while (value.Contains("  ")) value = value.Replace("  ", " ");
            if (value.Length <= maximumLength) return value;
            var cut = value.LastIndexOf(' ', maximumLength);
            if (cut < maximumLength / 2) cut = maximumLength;
            return value.Substring(0, cut).TrimEnd(' ', '.', ',', ';', ':') + "…";
        }

        private static string BoardQuestCurrentMomentSummary081(
            GuildCityPresentationState017D state,
            ExpeditionBoardNodeView074 current)
        {
            var summary = ExpeditionBoardProjection074.CurrentMomentSummary076(state, current);
            var expedition = state?.Expedition;
            if (expedition?.HasCommittedCheckAtCurrentNode != true)
                return summary;

            var legacyHeading = ExpeditionBoardProjection074.CheckOutcomeLabel076(
                expedition.LastCheckOutcome);
            var boardHeading = BoardQuestRules081.OutcomeHeading081(
                expedition.LastCheckOutcome);
            return string.IsNullOrWhiteSpace(legacyHeading) ||
                   StringComparer.Ordinal.Equals(legacyHeading, boardHeading)
                ? summary
                : summary.Replace(legacyHeading, boardHeading);
        }

        private void BuildBoardQuestRollResult081(
            Transform parent,
            GuildCityExpeditionView017D expedition)
        {
            var positive = BoardQuestRules081.IsPositive081(expedition.LastCheckOutcome);
            var result = RuntimeUi.AddPanel(
                parent,
                "Board Quest Dice Result 081",
                Color.white);
            // Keep the physical dice tray at full height even when the reading
            // column is compressed at the smaller supported window size.
            RuntimeUi.SetLayout(result, preferredHeight: 360f).minHeight = 360f;
            StyleNeutralBoardSurface091(result, 0.74f);
            RuntimeUi.AddVerticalLayout(
                result.transform,
                new RectOffset(16, 16, 4, 4),
                1f,
                TextAnchor.MiddleLeft);
            AddBoardQuestText081(
                result.transform,
                "Board Quest Dice Numbers 081",
                "DICE SETTLED  •  SAVED RESULT",
                20,
                24f,
                positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            BuildAuthoritativeDiceRoll084(
                result.transform,
                expedition.LastCheckDieOne,
                expedition.LastCheckDieTwo,
                expedition.LastCheckModifier,
                expedition.LastCheckTotal,
                "BOARD_QUEST_DICE_081|" + expedition.ExpeditionId + "|" +
                expedition.CurrentNodeId + "|" + expedition.LastCheckDieOne + "|" +
                expedition.LastCheckDieTwo + "|" + expedition.LastCheckModifier,
                positive);
            AddBoardQuestText081(
                result.transform,
                "Board Quest Dice Outcome 081",
                BoardQuestRules081.OutcomeHeading081(expedition.LastCheckOutcome),
                20,
                26f,
                RuntimeUi.Text,
                FontStyle.Bold);
            AddBoardQuestText081(
                result.transform,
                "Board Quest Dice Reward 081",
                "+" + BoardQuestRules081.GuildXpForOutcome081(expedition.LastCheckOutcome) +
                " GUILD XP" +
                (string.IsNullOrWhiteSpace(expedition.LastCheckAssistantRecruitId)
                    ? string.Empty
                    : "   •   NEW BOND MEMORY"),
                20,
                26f,
                RuntimeUi.Accent,
                FontStyle.Bold);
        }

        private RectTransform BuildBoardQuestActions081(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var context = new GameObject(
                    "Expedition Context Action Area 074",
                    typeof(RectTransform))
                .GetComponent<RectTransform>();
            context.SetParent(parent, false);
            SetAnchors074(context,
                new Vector2(0.735f, 0.035f), new Vector2(0.975f, 0.790f));
            var panel = RuntimeUi.AddPanel(
                context,
                "Board Quest Action Panel 081",
                new Color(0.006f, 0.016f, 0.028f, 0.97f));
            Stretch(panel.rectTransform);
            StyleNeutralBoardSurface091(panel, 0.58f);
            RuntimeUi.AddVerticalLayout(
                panel.transform,
                new RectOffset(18, 18, 14, 14),
                8f,
                TextAnchor.UpperCenter);

            AddBoardQuestText081(
                panel.transform,
                "Expedition Field Command Heading 074",
                "YOUR MOVE",
                30,
                38f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            if (!string.IsNullOrWhiteSpace(_localStatus) && !_localStatusPositive)
            {
                var status = RuntimeUi.AddText(
                    panel.transform,
                    "Board Quest Saved Command Status 081",
                    "NEEDS ATTENTION  •  " + _localStatus,
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Error,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(status, preferredHeight: 44f);
                ConfigureAuthoredCompactText076(status, 14, 20);
            }

            if (ShouldHoldBoardQuestAction081(state))
            {
                AddBoardQuestPrompt081(
                    panel.transform,
                    state?.Expedition?.HasCommittedCheckAtCurrentNode == true
                        ? "The saved dice have settled. Your next move appears in a moment."
                        : "The room card is flipping. See what happens before moving again.");
                var waiting = AddBoardQuestButton081(
                    panel.transform,
                    state?.Expedition?.HasCommittedCheckAtCurrentNode == true
                        ? "DICE SETTLED\nRESULT SAVED"
                        : "FLIPPING ROOM…\nREVEAL IN PROGRESS",
                    null,
                    RuntimeUi.ButtonNormal,
                    116f);
                waiting.gameObject.name = "Board Quest Reveal In Progress 081";
                waiting.interactable = false;
                ScheduleBoardQuestActionUnlock081(state?.Expedition?.CurrentNodeId);
                return context;
            }

            switch (view.ActionKind)
            {
                case ExpeditionBoardActionKind074.ChooseContract:
                    AddBoardQuestPrompt081(panel.transform, "Choose the mission your guild will answer.");
                    AddBoardQuestButton081(panel.transform, "CHOOSE A MISSION", () =>
                    {
                        _guildCityTab017D = "CONTRACTS";
                        BuildCurrentScreen();
                    }, RuntimeUi.Accent);
                    break;
                case ExpeditionBoardActionKind074.BeginExpedition:
                    AddBoardQuestPrompt081(panel.transform,
                        "Begin the mission and reveal the first room from its saved deck.");
                    NameBoardQuestPrimary081(AddBoardQuestButton081(
                        panel.transform,
                        "START QUEST\nPLACE PAWN AT GUILD DOOR",
                        () => RunBoardQuestCommand081(
                            coordinator.StartGuildCityExpedition017D,
                            "The mission could not begin."),
                        RuntimeUi.Accent));
                    break;
                case ExpeditionBoardActionKind074.ResolveObjective:
                    BuildBoardQuestRescue081(panel.transform, coordinator);
                    break;
                case ExpeditionBoardActionKind074.ResolveCheck:
                    BuildBoardQuestDiceChoices081(panel.transform, coordinator, state);
                    break;
                case ExpeditionBoardActionKind074.CommitEncounter:
                    AddExpeditionContextNotice074(
                        panel.transform,
                        "ENEMY CONTACT",
                        "The enemy blocks this room. Commit the fight and return here after the Unions win.",
                        RuntimeUi.Warning);
                    NameBoardQuestPrimary081(AddBoardQuestButton081(
                        panel.transform,
                        "FIGHT NOW",
                        () => EnterBoardQuestBattle081(
                            coordinator,
                            state.Expedition?.CurrentEncounterId,
                            true),
                        RuntimeUi.Warning));
                    break;
                case ExpeditionBoardActionKind074.EnterBattle:
                    AddBoardQuestPrompt081(panel.transform,
                        "Your Unions are deployed. Enter the battlefield.");
                    NameBoardQuestPrimary081(AddBoardQuestButton081(
                        panel.transform,
                        "ENTER BATTLE",
                        () => EnterBoardQuestBattle081(
                            coordinator,
                            state.Expedition?.CurrentEncounterId,
                            false),
                        RuntimeUi.Warning));
                    break;
                case ExpeditionBoardActionKind074.AwaitBattleReturn:
                    AddBoardQuestPrompt081(panel.transform,
                        "Finish the active battle and claim its reward, then continue this mission.");
                    break;
                case ExpeditionBoardActionKind074.ChooseRoute:
                    BuildBoardQuestMoveChoices081(panel.transform, coordinator, state, view);
                    break;
                case ExpeditionBoardActionKind074.FinalizeOperation:
                    var questFailed = BoardQuestRules081.QuestFailed081(state);
                    var chapterTwoReturn =
                        ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                            state?.Expedition?.BoardId);
                    if (questFailed)
                        AddBoardQuestPrompt081(
                            panel.transform,
                            "The quest failed. Return safely with battle growth already earned; no quest completion reward is granted.");
                    else
                        AddExpeditionContextNotice074(
                            panel.transform,
                            ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                                state?.Expedition?.BoardId)
                                ? "THE SURVEYORS ARE COMING HOME"
                                : "EVERYONE IS COMING HOME",
                            ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                                state?.Expedition?.BoardId)
                                ? "Sella, Orra, and the survey crew are safe. Return their evidence to Skyhome's Wayglass table."
                                : "The patrol and Wayglass are secure. Return to the Hall for the chapter report.",
                            RuntimeUi.Positive);
                    NameBoardQuestPrimary081(AddBoardQuestButton081(
                        panel.transform,
                        questFailed
                            ? "RETURN TO GUILD\nREGROUP & RETRY"
                            : chapterTwoReturn
                                ? "RETURN TO SKYHOME"
                                : "RETURN HOME",
                        () => FinalizeStoryOperation065(coordinator),
                        questFailed ? RuntimeUi.Warning : RuntimeUi.Positive));
                    break;
                default:
                    AddBoardQuestPrompt081(panel.transform,
                        "Your party is regrouping. Complete the highlighted action to continue.");
                    break;
            }
            return context;
        }

        private void BuildBoardQuestDiceChoices081(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            var crew = BoardQuestCrewFor081(state);
            if (crew?.Lead == null)
            {
                AddBoardQuestPrompt081(parent,
                    "No field leader is assigned. Return to the Party screen and place one member in a Union.");
                return;
            }

            var useTeam = BoardQuestRules081.ShouldUseBestTeam081(
                crew.CanTeamUp,
                crew.TeamVisibleModifier,
                crew.FastVisibleModifier);
            var helperName = useTeam
                ? crew.Partner?.RecruitName ?? "PARTNER"
                : string.Empty;
            AddBoardQuestPrompt081(
                parent,
                "The game chose the best useful crew. The saved 2d6 result will " +
                "appear on the table—there is no approach menu to study.");
            AddBoardQuestText081(
                parent,
                "Expedition Check Lead Heading 074",
                "READY  •  " + (crew.Lead.RecruitName ?? "GUILDMATE").ToUpperInvariant() +
                (useTeam ? " + " + helperName.ToUpperInvariant() : " GOES FAST") +
                "  •  BONUS " + SignedBoardQuest081(
                    useTeam ? crew.TeamVisibleModifier : crew.FastVisibleModifier),
                20,
                48f,
                RuntimeUi.Text,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            var checkKey = (state?.Expedition?.ExpeditionId ?? "QUEST") + "|" +
                           (state?.Expedition?.CurrentNodeId ?? "ROOM");
            var rollingAutomatically = Application.isPlaying &&
                                       _scheduledBoardQuestChecks081.Add(checkKey);
            var roll = AddBoardQuestButton081(
                parent,
                rollingAutomatically
                    ? "ROLLING 2D6…\nWATCH THE DICE"
                    : "ROLL 2D6\nAUTO-RESOLVE",
                rollingAutomatically
                    ? null
                    : (Action)(() => ResolveBoardQuestRoll081(
                        coordinator, state, crew, useTeam)),
                RuntimeUi.Accent,
                116f);
            roll.interactable = !rollingAutomatically;
            NameBoardQuestPrimary081(roll);
            if (rollingAutomatically)
                StartCoroutine(AutoResolveBoardQuestCheck081(
                    coordinator,
                    state?.Expedition?.CurrentNodeId,
                    checkKey));
        }

        private IEnumerator AutoResolveBoardQuestCheck081(
            IGuildCityPresentationCoordinator017D coordinator,
            string expectedNodeId,
            string checkKey)
        {
            yield return new WaitForSecondsRealtime(
                BoardAdventureCardFlipDuration084 + 0.16f);
            var latest = coordinator?.GuildCity017D;
            if (latest?.Expedition == null ||
                !StringComparer.Ordinal.Equals(
                    latest.Expedition.CurrentNodeId, expectedNodeId) ||
                latest.Expedition.HasCommittedCheckAtCurrentNode)
                yield break;

            var crew = BoardQuestCrewFor081(latest);
            if (crew?.Lead == null)
            {
                _localStatus = "Assign at least one member to a Union before this roll.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                yield break;
            }
            var useTeam = BoardQuestRules081.ShouldUseBestTeam081(
                crew.CanTeamUp,
                crew.TeamVisibleModifier,
                crew.FastVisibleModifier);
            ResolveBoardQuestRoll081(coordinator, latest, crew, useTeam);
        }

        private void HoldBoardQuestAction081(
            GuildCityPresentationState017D state,
            float seconds)
        {
            if (_reducedMotion || !Application.isPlaying ||
                state?.Expedition == null) return;
            _boardQuestActionUnlockNode081 = state.Expedition.CurrentNodeId ?? string.Empty;
            _boardQuestActionUnlockAt081 = Time.unscaledTime + Mathf.Max(0f, seconds);
        }

        private bool ShouldHoldBoardQuestAction081(GuildCityPresentationState017D state)
        {
            if (!Application.isPlaying || _reducedMotion || state?.Expedition == null) return false;
            var dice132 = _screenRoot == null ? null : _screenRoot.GetComponentInChildren<CommittedQuestDice132>();
            if (dice132 != null && !dice132.IsSettled132) return true;
            return StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeId,
                _boardQuestActionUnlockNode081) && Time.unscaledTime < _boardQuestActionUnlockAt081;
        }

        private void ScheduleBoardQuestActionUnlock081(string expectedNodeId)
        {
            if (!Application.isPlaying || _boardQuestActionUnlockRoutine081 != null)
                return;
            _boardQuestActionUnlockRoutine081 = StartCoroutine(
                UnlockBoardQuestAction081(expectedNodeId));
        }

        private IEnumerator UnlockBoardQuestAction081(string expectedNodeId)
        {
            var remaining = Mathf.Max(0f,
                _boardQuestActionUnlockAt081 - Time.unscaledTime);
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);
            var dice132 = _screenRoot == null ? null :
                _screenRoot.GetComponentInChildren<CommittedQuestDice132>();
            while (dice132 != null && !dice132.IsSettled132)
            {
                if (!dice132.isActiveAndEnabled) { _boardQuestActionUnlockRoutine081 = null; yield break; }
                yield return null;
            }
            _boardQuestActionUnlockRoutine081 = null;
            // A quest-card result owns only this reveal hold. Clearing its
            // transient presentation token here prevents a later authored dice
            // hold from resurrecting the previous chest, recruit, or battle card.
            _lastBoardQuestCard090 = null;
            _boardQuestCardAwaitingAcknowledgement090 = false;
            var latest = (_coordinator as IGuildCityPresentationCoordinator017D)
                ?.GuildCity017D;
            if (StringComparer.Ordinal.Equals(
                    latest?.Expedition?.CurrentNodeId,
                    expectedNodeId) &&
                StringComparer.Ordinal.Equals(_guildCityTab017D, "EXPEDITION"))
                BuildCurrentScreen();
        }

        private BoardQuestCrew081 BoardQuestCrewFor081(GuildCityPresentationState017D state)
        {
            var activeRecruitIds = new HashSet<string>(
                (_coordinator?.State?.Unions ?? Array.Empty<M1UnionView>())
                    .Where(value => value != null)
                    .SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>()),
                StringComparer.Ordinal);
            var recruitViews = (_coordinator?.State?.Recruits ??
                                Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToDictionary(value => value.RecruitId, value => value, StringComparer.Ordinal);
            var eligibleSkills = state?.Expedition?.CurrentEventEligibleSkills ??
                                 Array.Empty<string>();
            var participants = (state?.Assignments ?? Array.Empty<GuildCityAssignmentView017D>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId) &&
                                (activeRecruitIds.Count == 0 || activeRecruitIds.Contains(value.RecruitId)))
                .OrderByDescending(value => recruitViews.TryGetValue(value.RecruitId, out var recruit)
                    ? ExpeditionLeadSuitabilityScore076(recruit.ObservedClass, eligibleSkills)
                    : 0)
                .ThenBy(value => value.RecruitName, StringComparer.Ordinal)
                .Take(3)
                .ToArray();
            if (participants.Length == 0) return null;

            var lead = participants[0];
            recruitViews.TryGetValue(lead.RecruitId, out var leadView);
            var partner = participants.Skip(1).FirstOrDefault();
            recruitViews.TryGetValue(partner?.RecruitId ?? string.Empty, out var partnerView);
            var leadCandidate = new ChapterTwoCrewCandidate079
            {
                RecruitId = lead.RecruitId,
                DisplayName = lead.RecruitName,
                ObservedClass = leadView?.ObservedClass ?? string.Empty
            };
            ChapterTwoCrewCandidate079 partnerCandidate = null;
            if (partner != null)
                partnerCandidate = new ChapterTwoCrewCandidate079
                {
                    RecruitId = partner.RecruitId,
                    DisplayName = partner.RecruitName,
                    ObservedClass = partnerView?.ObservedClass ?? string.Empty
                };
            var fast = ChapterTwoCrewMechanics079.BuildCheckPreview079(
                leadCandidate,
                null,
                eligibleSkills,
                1,
                state?.CampaignModeId,
                true);
            var team = ChapterTwoCrewMechanics079.BuildCheckPreview079(
                leadCandidate,
                partnerCandidate,
                eligibleSkills,
                0,
                state?.CampaignModeId,
                true);
            var fieldSkillBonus = BoardRoomAuthority081.BoardRoomSkillBonus081(
                state?.Expedition?.ObjectiveFlags);
            var blessingBonus = BoardRoomAuthority081.BoardRoomBlessingBonus081(
                state?.Expedition?.ObjectiveFlags);
            var questCardModifier = BoardRoomAuthority081
                .QuestCardRunCheckModifier090(
                    state?.Expedition?.ObjectiveFlags);
            // The command service owns this permanent actor bonus. Include it
            // in visible odds only; sending it in the command would add it twice.
            var permanentHeroBonus165 = BoardRoomAuthority081.UsesBoardQuestRewards081(state?.Expedition?.BoardId)
                ? (_coordinator as IQuestFateCoordinator165)?.OpeningHeroCheckBoon165(lead.RecruitId) ?? 0 : 0;
            return new BoardQuestCrew081
            {
                Lead = lead,
                Partner = partner,
                LeadClass = leadView?.ObservedClass ?? string.Empty,
                PartnerClass = partnerView?.ObservedClass ?? string.Empty,
                FastModifier = fast.CommandModifier + fieldSkillBonus + blessingBonus +
                               questCardModifier,
                TeamModifier = team.CommandModifier + fieldSkillBonus + blessingBonus +
                               questCardModifier,
                FastVisibleModifier = fast.EffectiveModifier + fieldSkillBonus +
                                      blessingBonus + questCardModifier + permanentHeroBonus165,
                TeamVisibleModifier = team.EffectiveModifier + fieldSkillBonus +
                                      blessingBonus + questCardModifier + permanentHeroBonus165,
                FieldSkillBonus = fieldSkillBonus,
                BlessingBonus = blessingBonus,
                CanTeamUp = partner != null &&
                            SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                                .CanUseCarefulApproach076(state?.Expedition?.Urgency ?? 0)
            };
        }

        private void ResolveBoardQuestRoll081(
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            BoardQuestCrew081 crew,
            bool teamUp)
        {
            if (_boardQuestCommandRunning081) return;
            _boardQuestCommandRunning081 = true;
            M1CommandResult result;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                result = coordinator?.ResolveGuildCityCheck017D(
                    string.IsNullOrWhiteSpace(state?.Expedition?.CurrentEventId)
                        ? "EVENT_COLLAPSED_HANDRAIL"
                        : state.Expedition.CurrentEventId,
                    crew.Lead.RecruitId,
                    teamUp ? crew.Partner?.RecruitId ?? string.Empty : string.Empty,
                    teamUp ? crew.TeamModifier : crew.FastModifier);
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _boardQuestCommandRunning081 = false;
            }
            _localStatus = result?.Message ?? "The dice roll could not be saved.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
                HoldBoardQuestAction081(
                    coordinator?.GuildCity017D,
                    BoardAdventureCommittedCheckRewardDelay084 +
                    BoardAdventureRewardRevealDuration084 + 0.12f);
            BuildCurrentScreen();
        }

        private void BuildBoardQuestMoveChoices081(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var destination = BoardQuestRules081.AutomaticDestination081(state, view);
            var offersElite = StringComparer.Ordinal.Equals(
                                  state.Expedition?.CurrentNodeKind,
                                  "OPTIONAL_ELITE") &&
                              state.Expedition.CanCommitEncounter;
            if (offersElite)
            {
                AddBoardQuestPrompt081(
                    parent,
                    "A monster was under this card. The mission pauses for the full Union battle.");
                NameBoardQuestPrimary081(AddBoardQuestButton081(
                    parent,
                    "FIGHT THE MONSTER\nFULL UNION BATTLE",
                    () => EnterBoardQuestBattle081(
                        coordinator,
                        state.Expedition.CurrentEncounterId,
                        true),
                    RuntimeUi.Warning,
                    118f));
                if (destination != null)
                {
                    var bypass = AddBoardQuestButton081(
                        parent,
                        "SKIP ELITE\nMOVE FORWARD",
                        () => FlipBoardQuestRoom081(coordinator, destination.NodeId),
                        RuntimeUi.ButtonNormal,
                        104f);
                    bypass.gameObject.name = "Board Quest Skip Optional Elite 081";
                }
                return;
            }

            if (destination == null)
            {
                AddBoardQuestPrompt081(
                    parent,
                    "The next room is not available yet. Finish the highlighted story or battle step.");
                return;
            }

            BuildBoardAdventureNextRoomPreview084(
                parent,
                "ROOM " + ExpeditionBoardProjection074.StepNumberForNode074(
                    destination.NodeId));
            AddBoardQuestPrompt081(
                parent,
                "Continue to reveal the next saved room. Its reward or dice result resolves once.");
            NameBoardQuestPrimary081(AddBoardQuestButton081(
                parent,
                "MOVE FORWARD\nFLIP NEXT ROOM",
                () => FlipBoardQuestRoom081(coordinator, destination.NodeId),
                RuntimeUi.Accent,
                124f));
        }

        private void FlipBoardQuestRoom081(
            IGuildCityPresentationCoordinator017D coordinator,
            string destinationNodeId)
        {
            if (_boardQuestCommandRunning081) return;
            _boardQuestCommandRunning081 = true;
            M1CommandResult result;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                result = coordinator?.MoveGuildCityExpedition017D(destinationNodeId);
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _boardQuestCommandRunning081 = false;
            }
            _localStatus = result?.Succeeded == true
                ? string.Empty
                : result?.Message ?? "That room could not be reached.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
                HoldBoardQuestAction081(
                    coordinator?.GuildCity017D,
                    BoardAdventurePawnTravelDuration084 +
                    BoardAdventureCardFlipDuration084 + 0.34f);
            if (_localStatusPositive) _selectedExpeditionDestination074 = null;
            BuildCurrentScreen();
        }

        private void BuildBoardQuestRescue081(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator)
        {
            AddExpeditionContextNotice074(
                parent,
                ExpeditionBoardProjection074.PatrolFoundDecisionTitle076,
                ExpeditionBoardProjection074.PatrolFoundDecisionCopy076,
                RuntimeUi.Warning);
            var rescue = coordinator as IFirstHourPatrolRescueCoordinator071;
            var button = AddBoardQuestButton081(
                parent,
                rescue == null ? "RESCUE UNAVAILABLE" : "RALLY THE LANTERN PATROL",
                rescue == null
                    ? null
                    : (Action)(() => RunBoardQuestCommand081(
                        rescue.RescueFirstHourLanternPatrol071,
                        "The patrol rescue could not be saved.")),
                rescue == null ? RuntimeUi.ButtonNormal : RuntimeUi.Positive,
                108f);
            button.interactable = rescue != null;
            NameBoardQuestPrimary081(button);
        }

        private void EnterBoardQuestBattle081(
            IGuildCityPresentationCoordinator017D coordinator,
            string encounterId,
            bool commitFirst)
        {
            if (coordinator == null)
            {
                _localStatus = "The battle coordinator is unavailable.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                return;
            }
            if (commitFirst)
            {
                var committed = coordinator.CommitGuildCityEncounter017D(encounterId);
                if (committed == null || !committed.Succeeded)
                {
                    _localStatus = committed?.Message ?? "The encounter could not be saved.";
                    _localStatusPositive = false;
                    BuildCurrentScreen();
                    return;
                }
            }
            var started = coordinator.StartCommittedGuildCityBattle017D();
            _localStatus = started?.Message ?? "The battle could not begin.";
            _localStatusPositive = started != null && started.Succeeded;
            if (_localStatusPositive) Navigate(M1Screen.Battle);
            else BuildCurrentScreen();
        }

        private void RunBoardQuestCommand081(Func<M1CommandResult> command, string fallback)
        {
            var result = command == null ? null : command();
            _localStatus = result?.Message ?? fallback;
            _localStatusPositive = result != null && result.Succeeded;
            BuildCurrentScreen();
        }

        private static string BoardQuestPromise081(
            ExpeditionBoardActionKind074 action,
            string expeditionStatus)
        {
            switch (action)
            {
                case ExpeditionBoardActionKind074.ResolveCheck:
                    return "NEXT  •  YOUR BEST CREW ROLLS AUTOMATICALLY. WATCH THE DICE.";
                case ExpeditionBoardActionKind074.CommitEncounter:
                case ExpeditionBoardActionKind074.EnterBattle:
                    return "NEXT  •  WIN THE UNION BATTLE TO OPEN THIS SPACE.";
                case ExpeditionBoardActionKind074.ChooseRoute:
                    return "NEXT  •  TAP MOVE FORWARD. THE NEXT ROOM FLIPS AND RESOLVES.";
                case ExpeditionBoardActionKind074.FinalizeOperation:
                    return StringComparer.OrdinalIgnoreCase.Equals(expeditionStatus, "Failed")
                        ? "NEXT  •  RETURN SAFELY. NO QUEST COMPLETION REWARD; BATTLE GROWTH IS KEPT."
                        : "NEXT  •  RETURN HOME AND COLLECT THE MISSION REWARD.";
                default:
                    return "NEXT  •  USE THE ONE LARGE ACTION ON THE RIGHT.";
            }
        }

        private static BoardTowerRoom001 SafeCommittedRoomModule001(
            GuildCityPresentationState017D state,
            ExpeditionBoardNodeView074 node,
            string boardRoomKind)
        {
            // The source modules' unresolved prose choices are deliberately not
            // shown. A committed compatible name adds room identity while the
            // existing authored action, exact cost, reward, and story stay true.
            if (!BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001(boardRoomKind))
                return null;
            try
            {
                return BoardTowerEnhancementCatalog001.LoadFromResources()
                    .CommittedBoardRoom001(
                        state?.Expedition?.ExpeditionId,
                        node?.NodeId,
                        boardRoomKind,
                        state?.Expedition?.ObjectiveFlags);
            }
            catch (InvalidOperationException)
            {
                // Release validation is fail-closed. At runtime, preserving the
                // certified board is safer than interrupting a player's save.
                return null;
            }
        }

        private static void AddBoardQuestPrompt081(Transform parent, string copy)
        {
            var prompt = RuntimeUi.AddPanel(
                parent,
                "Board Quest Plain Prompt 081",
                new Color(0.020f, 0.045f, 0.068f, 0.95f));
            RuntimeUi.SetLayout(prompt, preferredHeight: 108f);
            M1PremiumUi.StylePanel(prompt, M1PremiumUi.Surface.WorldPaper);
            var text = RuntimeUi.AddText(
                prompt.transform,
                "Board Quest Plain Prompt Copy 081",
                copy,
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(16f, 8f);
            text.rectTransform.offsetMax = new Vector2(-16f, -8f);
            ConfigureAuthoredCompactText076(text, 15, 21);
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static Button AddBoardQuestButton081(
            Transform parent,
            string label,
            Action action,
            Color color,
            float height = 96f)
        {
            var button = RuntimeUi.AddButton(
                parent,
                "Board Quest Action " + label.Replace('\n', ' ') + " 081",
                label,
                action,
                height,
                color);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.rectTransform.offsetMin = new Vector2(12f, 6f);
                text.rectTransform.offsetMax = new Vector2(-12f, -6f);
                ConfigureAuthoredCompactText076(text, 15, 24);
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
            return button;
        }

        private static Button NameBoardQuestPrimary081(Button button)
        {
            if (button != null)
                button.gameObject.name = "Expedition Primary Context Action 074";
            return button;
        }

        private static Text AddBoardQuestText081(
            Transform parent,
            string name,
            string copy,
            int fontSize,
            float height,
            Color color,
            FontStyle style,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = RuntimeUi.AddText(parent, name, copy, fontSize, alignment, color, style);
            RuntimeUi.SetLayout(text, preferredHeight: height);
            ConfigureAuthoredCompactText076(text, Math.Max(14, fontSize - 8), fontSize);
            return text;
        }

        private static string SignedBoardQuest081(int value) =>
            value >= 0 ? "+" + value : value.ToString();
    }
}
