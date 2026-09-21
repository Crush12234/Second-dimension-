using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Campaign023;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        // This panel contains the only full event copy for the committed room.
        // Retain a short settled beat after the saved outcome is revealed.
        // Even dice plus chest finish within three seconds; Skip keeps
        // this same receipt and omits only its presentation animation.
        const float WorldGateRoomResultHold084 = 0.35f;
        readonly HashSet<string> _scheduledWorldGateReceiptApplies084 =
            new HashSet<string>(StringComparer.Ordinal);
        string _worldGateReceiptRetryId084 = string.Empty;
        string _worldGateReceiptRetryMessage084 = string.Empty;
        bool _worldGateRoomCommandRunning084;
        bool _showMoreAdventureBoards084;
        bool _showAdventureWorldTravel084;

        void BuildGuildCityWorldGate023(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            if (state == null || !state.IsAvailable)
            {
                AddMessagePanel(body, "QUEST BOARD",
                    state?.Error ?? "The Guild's quest board could not be opened.",
                    RuntimeUi.Warning);
                return;
            }

            ConcealCommittedEffectCopy132(coordinator,state);

            // A completed deck may leave a committed authored story interruption.
            // Show that existing020 owner instead of the unrelated board shelf.
            if (string.IsNullOrWhiteSpace(state.ActiveOperationId) &&
                coordinator is Campaign020.ICampaignPlayablePresentationCoordinator020 playableOwner)
            {
                var story = playableOwner.CampaignPlayable020;
                if (story?.IsAvailable == true && !string.IsNullOrWhiteSpace(story.ActiveOperationId))
                {
                    BuildGuildCityCampaign020(body, playableOwner, story);
                    return;
                }
            }

            if (state.LegacyRecoveryRequired)
            {
                var recovery = AddMessagePanel(body,
                    "OLD UNFINISHED ADVENTURE FOUND",
                    "This route began before the new saved tile-proof rules. Recover old quest — " +
                    "no reward lost outside this unfinished run. Completed quests, world standing, " +
                    "guild progress, recruits, inventory, and previous rewards stay exactly as saved.",
                    RuntimeUi.Warning);
                RuntimeUi.AddButton(recovery, "Recover legacy World Gate quest 084",
                    "RECOVER OLD QUEST — NO REWARD FROM THIS RUN",
                    () => RunGuildCityCommand017D(
                        coordinator.RecoverLegacyWorldGateQuest023),
                    132f, RuntimeUi.Warning);
                UseContentDrivenBoardPanelHeight084(recovery);
                return;
            }

            if (!string.IsNullOrWhiteSpace(state.ActiveOperationId))
                BuildActiveAdventureBoard084(body, coordinator, state);
            else
            {
                BuildAdventureBoardShelf084(body, coordinator, state);
                AddMessagePanel(body, "HOW TO PLAY",
                    "Start a quest, choose one of three shuffled route cards, then watch the " +
                    "selected card flip, dice roll, and its event, chest, boon, " +
                    "danger, recruit lead, or Union battle resolve.",
                    RuntimeUi.Accent);
            }

            if (!string.IsNullOrWhiteSpace(state.PendingReceiptId)) return;
            BuildAdventureWorldTravel084(body, coordinator, state);
        }

        void BuildActiveAdventureBoard084(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            if (TryBuildActiveCampaignDeck131(body, coordinator, state)) return;
            var node = state.CurrentNode;
            var playableRoomCount084 = AdventurePlayableSpaceCount084(state);
            var playableRoom084 = AdventureCurrentPlayableSpace084(
                state, playableRoomCount084);
            if (_screenRoot != null && node != null && !string.IsNullOrWhiteSpace(state.PendingReceiptId))
            {
                BuildFullScreenWorldGateEvent110(coordinator, state);
                return;
            }

            // First-time play is one short tutorial card at a time. Do not build
            // the route row underneath it: off-screen cards could otherwise finish
            // their one-time reveals and become clickable before "show my routes".
            if (!state.ExpeditionDeckTutorialSeen)
            {
                BuildExpeditionDeckHelp089(body, coordinator, state);
                return;
            }

            var missionSummary084 = AddMessagePanel(body,
                (state.ActiveOperationKind == "CHAPTER" ? "STORY QUEST" :
                 state.ActiveOperationKind == "REPEATABLE" ? "GUILD CONTRACT" : "WORLD CRISIS") +
                "  •  " + (state.ActiveBoardTitle ?? "ACTIVE QUEST").ToUpperInvariant(),
                "GOAL  •  " + CompactWorldGateCopy084(state.BoardObjective,
                    "Reach the objective and return to the Guild.", 118) +
                "\nNOW  •  " + CompactWorldGateCopy084(
                    WorldGateCurrentTask084(state, node),
                    "Move forward and flip the next room.", 92) +
                "\nCARD ROUND  " + Math.Max(1, state.DeckChoiceRound) +
                " OF " + Math.Max(
                    ExpeditionDeckService089.MinimumQuestChoiceRounds089,
                    state.DeckMinimumChoiceRounds) + "+  •  ROOM  " +
                    playableRoom084 + " OF " +
                    playableRoomCount084 + "  •  SUPPLY  " + state.Supplies,
                RuntimeUi.Warning);
            var missionSummaryCopy084 = missionSummary084
                .GetComponentsInChildren<Text>(true)
                .FirstOrDefault(value => value != null &&
                    value.name.StartsWith("Message", StringComparison.Ordinal));
            if (missionSummaryCopy084 != null)
            {
                RuntimeUi.SetLayout(missionSummaryCopy084,
                    preferredHeight: 94f);
                ConfigureResponsiveText062(missionSummaryCopy084, 20, 24);
            }
            StyleNeutralBoardSurface091(missionSummary084.GetComponent<Image>(), 0.72f);
            UseContentDrivenBoardPanelHeight084(missionSummary084);
            if (node == null)
            {
                AddMessagePanel(body, "QUEST TILE UNAVAILABLE",
                    "The saved quest references a room that is not in the certified board. No move was made.",
                    RuntimeUi.Warning);
                return;
            }

            // Keep the one actionable expedition beat in the first viewport.
            // The objective and room recap provide context without a node map.
            // Context must never push FLIP / BATTLE / RETURN (or the
            // start of the physical three-card choice row) below the fold on
            // phone layouts.
            BuildWorldGatePrimaryAction084(body, coordinator, state, node);
            // Once learned, help is optional reference material. Keep it below
            // the live decision so it can never displace the
            // current action from the first viewport.
            BuildExpeditionDeckHelp089(body, coordinator, state);

            if (!string.IsNullOrWhiteSpace(state.PendingReceiptId))
                return;

            if (state.HasLastAppliedRoom)
            {
                var lastSetback = IsWorldGateSetback084(state.LastAppliedOutcomeTitle);
                var recap = AddMessagePanel(body,
                    "LAST ROOM  •  " +
                    (state.LastAppliedRoomTitle ?? "ROOM CLEARED").ToUpperInvariant(),
                    (state.LastAppliedOutcomeTitle ?? "RESOLVED") +
                    (string.IsNullOrWhiteSpace(state.LastAppliedReward)
                        ? string.Empty
                        : "  •  " + state.LastAppliedReward),
                    lastSetback ? RuntimeUi.Warning : RuntimeUi.Positive);
                UseContentDrivenBoardPanelHeight084(recap);
            }

            var activeLockedBattleDecision084 = node.RequiresBattle &&
                state.ActiveStatus == "Active";
            var revealCurrentRoom = !activeLockedBattleDecision084 &&
                                    (node.RequiresBattle ||
                                    state.ActiveStatus == "AwaitingBattle" ||
                                    state.ActiveStatus == "ReadyToFinalize");
            if (revealCurrentRoom)
            {
                var lockedEncounter = node.RequiresBattle
                    ? (state.RouteCards ?? Array.Empty<
                        Campaign023.ExpeditionRouteCardView089>())
                        .FirstOrDefault(value => value != null &&
                            value.LockedToBattle)
                    : null;
                var storyFirst = state.ActiveOperationKind == "CHAPTER" &&
                                 !string.IsNullOrWhiteSpace(node.StoryFlavor);
                var eventCopy = lockedEncounter?.Description;
                if (string.IsNullOrWhiteSpace(eventCopy))
                    eventCopy = storyFirst ? node.StoryFlavor : node.Description;
                var rewardCopy = lockedEncounter?.RewardPreview;
                if (string.IsNullOrWhiteSpace(rewardCopy))
                    rewardCopy = node.RewardPreview;
                var encounterKind = string.IsNullOrWhiteSpace(
                    lockedEncounter?.Category) ? "BATTLE" :
                    lockedEncounter.Category;
                var room = AddMessagePanel(body,
                    (node.RequiresBattle
                        ? encounterKind + " ENCOUNTER"
                        : "ROOM CLEARED") + "  •  " +
                    (node.Title ?? "STORY MOMENT").ToUpperInvariant(),
                    "EVENT  •  " + CompactWorldGateCopy084(
                        eventCopy, "The Guild reaches the next room.", 168) +
                    "\nREWARD  •  " + CompactWorldGateCopy084(
                        rewardCopy, "Story progress", 100),
                    node.RequiresBattle ? RuntimeUi.Warning : RuntimeUi.ButtonNormal);
                var roomCard = room as RectTransform ??
                               room.GetComponent<RectTransform>();
                DecorateBoardAdventureRevealedCard084(
                    roomCard,
                    lockedEncounter?.VisualCategoryKey ??
                    (string.IsNullOrWhiteSpace(node.RoomKind)
                        ? node.Kind : node.RoomKind),
                    rewardCopy);
                if (!string.IsNullOrWhiteSpace(
                        lockedEncounter?.VisualResourcePath) &&
                    Resources.Load<Sprite>(lockedEncounter.VisualResourcePath) != null)
                    GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                        room, "Locked Expedition Encounter Card Face 089",
                        lockedEncounter.VisualResourcePath, 220f);
                else if (!string.IsNullOrWhiteSpace(node.IconResource))
                    GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                        room, "Current Adventure Room Icon 084", node.IconResource,
                        156f);
                if (node.RequiresBattle)
                {
                    var lockCue = RuntimeUi.AddText(room,
                        "Locked Expedition Story Battle Cue 089",
                        "LOCKED STORY BATTLE  •  FULL UNION FORECAST COMBAT",
                        16, TextAnchor.MiddleCenter, RuntimeUi.Accent,
                        FontStyle.Bold);
                    RuntimeUi.SetLayout(lockCue, preferredHeight: 34f);
                    AnimateBoardAdventureCardFlip084(roomCard,
                        "WORLD_GATE_LOCKED_BATTLE_CARD_089|" +
                        state.ActiveOperationId + "|" + node.NodeId,
                        "STORY ENCOUNTER  •  FACE DOWN");
                }
                UseContentDrivenBoardPanelHeight084(room);
            }
        }

        void BuildWorldGatePrimaryAction084(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state,
            Campaign023.NodeView023 node)
        {
            if (TryBuildCommittedEffect132(body, coordinator, state)) return;
            if (!string.IsNullOrWhiteSpace(state.PendingReceiptId))
            {
                var outcomeCopy = state.PendingOutcomeTitle ?? string.Empty;
                var setback = IsWorldGateSetback084(outcomeCopy);
                var chestReveal092 = state.PendingChestHasReward092 && !setback &&
                    StringComparer.Ordinal.Equals(state.PendingCardCategory, "CHEST");
                var storyFirst = state.ActiveOperationKind == "CHAPTER" &&
                                 !string.IsNullOrWhiteSpace(node.StoryFlavor);
                var roomCopy = CompactWorldGateCopy084(
                    storyFirst ? node.StoryFlavor : node.Description,
                    "The Guild reaches the next room.", 150);
                var revealedCardTitle089 = string.IsNullOrWhiteSpace(
                    state.PendingCardTitle)
                    ? (node.Title ?? node.RoomTitle ?? "STORY MOMENT")
                    : state.PendingCardTitle;
                var result = AddMessagePanel(body,
                    (state.PendingIsEncounterRound
                        ? "ENCOUNTER CARD FLIPPED  •  "
                        : "ROOM FLIPPED  •  ") +
                    revealedCardTitle089.ToUpperInvariant(),
                    "EVENT  •  " + roomCopy + "\n" +
                    "RESULT  •  " + state.PendingOutcomeTitle + "\n" +
                    (state.PendingRequiresCertifiedBattle ? "AFTER VICTORY  •  " : "REWARD  •  ") +
                    (IsFullScreenWorldGateEventBody110(body)
                        ? state.PendingReward ?? "Story progress"
                        : CompactWorldGateCopy084(state.PendingReward, "Story progress", 96)),
                    setback || state.PendingRequiresCertifiedBattle ? RuntimeUi.Warning : RuntimeUi.Positive);
                var resultCard = result as RectTransform ??
                                 result.GetComponent<RectTransform>();
                var visualRoomKind = !string.IsNullOrWhiteSpace(
                        state.PendingCardCategory)
                    ? state.PendingCardCategory
                    : string.IsNullOrWhiteSpace(node.RoomKind)
                        ? node.Kind
                        : node.RoomKind;
                DecorateBoardAdventureRevealedCard084(
                    resultCard, visualRoomKind, state.PendingReward);
                if (chestReveal092)
                {
                    var chestArt092 = RuntimeUi.AddPanel(result, "Revealed Expedition Card Face 089", Color.white);
                    chestArt092.sprite = BoardChestReveal092.Frame092(0);
                    chestArt092.preserveAspect = true;
                    chestArt092.raycastTarget = false;
                    RuntimeUi.SetLayout(chestArt092, preferredHeight: 156f);
                }
                else if (!string.IsNullOrWhiteSpace(state.PendingCardVisualResourcePath) &&
                    Resources.Load<Sprite>(state.PendingCardVisualResourcePath) != null)
                    GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                        result, "Revealed Expedition Card Face 089",
                        state.PendingCardVisualResourcePath, 156f);
                else if (!string.IsNullOrWhiteSpace(node.IconResource))
                    GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                        result, "Revealed World Gate Room Icon 084",
                        node.IconResource, 132f);
                if (state.PendingHasCheck)
                {
                    var diceHeading = RuntimeUi.AddText(result,
                        "World Gate Saved Dice Heading 084",
                        "SAVED CHECK  •  TARGET " + state.PendingDifficulty,
                        20, TextAnchor.MiddleCenter,
                        setback ? RuntimeUi.Warning : RuntimeUi.Positive,
                        FontStyle.Bold);
                    RuntimeUi.SetLayout(diceHeading, preferredHeight: 32f);
                    BuildAuthoritativeDiceRoll084(
                        result,
                        state.PendingDieOne,
                        state.PendingDieTwo,
                        state.PendingModifier,
                        state.PendingTotal,
                        "WORLD_GATE_DICE_084|" + state.PendingReceiptId,
                        !setback);
                }
                if (chestReveal092)
                {
                    var chestReward092 = RuntimeUi.AddText(result, "World Gate Chest Exact Reward 092",
                        state.PendingChestRewardCopy092 ?? state.PendingReward,
                        22, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
                    RuntimeUi.SetLayout(chestReward092, preferredHeight: 94f);
                    ConfigureAuthoredCompactText076(chestReward092, 18, 24);
                }
                else if (state.PendingRequiresCertifiedBattle)
                {
                    var encounterReady110 = RuntimeUi.AddText(result,
                        "World Gate Encounter Ready 110",
                        "ENCOUNTER SAVED  •  BATTLE READY\nNO BATTLE REWARD CLAIMED",
                        22, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold);
                    RuntimeUi.SetLayout(encounterReady110, preferredHeight: 78f);
                }
                else
                    BuildBoardAdventureResolvedRewardStrip087(
                        result,
                        visualRoomKind,
                        state.PendingReward,
                        "WORLD_GATE_REWARD_087|" + state.PendingReceiptId,
                        !setback,
                        state.PendingHasCheck);
                var waitingForRetry = StringComparer.Ordinal.Equals(
                    _worldGateReceiptRetryId084, state.PendingReceiptId);
                var resolving = RuntimeUi.AddText(result,
                    "World Gate Automatic Result Apply 084",
                    waitingForRetry
                         ? "SAVED RESULT IS SAFE  •  READY TO RETRY"
                        : state.PendingRequiresCertifiedBattle
                            ? "SAVED  •  ENTER UNION COMBAT WHEN READY"
                        : state.PendingIsEncounterRound
                            ? "SAVED  •  SHUFFLING THE THREE ROUTE CARDS…"
                            : "SAVED  •  MOVING TO THE NEXT ROOM…",
                    18, TextAnchor.MiddleCenter,
                    waitingForRetry ? RuntimeUi.Warning : RuntimeUi.MutedText,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(resolving, preferredHeight: 30f);
                if (state.PendingRequiresCertifiedBattle)
                {
                    var enterBattle = RuntimeUi.AddButton(result,
                        "Enter optional Expedition card battle 089",
                        "FACE THE THREAT  •  ENTER / RETURN TO BATTLE",
                        () =>
                        {
                            var battle = (coordinator as Campaign023
                                .IExpeditionDeckPresentationCoordinator089)
                                ?.EnterExpeditionCardBattle089();
                            _localStatus = battle?.Message ??
                                "The optional battle could not begin.";
                            _localStatusPositive = battle?.Succeeded == true;
                            if (_localStatusPositive)
                                Navigate(M1Screen.Battle);
                            else BuildCurrentScreen();
                        }, 118f, RuntimeUi.Warning);
                    ConfigureResponsiveText062(
                        enterBattle.GetComponentInChildren<Text>(), 17, 22);
                }
                if (waitingForRetry)
                {
                    var retryCopy = RuntimeUi.AddText(result,
                        "World Gate Saved Result Retry Message 084",
                        string.IsNullOrWhiteSpace(_worldGateReceiptRetryMessage084)
                            ? "The result is already saved. Retry applying it to continue."
                            : _worldGateReceiptRetryMessage084,
                        17, TextAnchor.MiddleCenter, RuntimeUi.Text,
                        FontStyle.Normal);
                    RuntimeUi.SetLayout(retryCopy, preferredHeight: 54f);
                    var retryReceiptId = state.PendingReceiptId;
                    RuntimeUi.AddButton(result,
                        "Retry saved World Gate result 084",
                        "RETRY SAVED RESULT",
                        () => ApplyWorldGateReceiptNow084(
                            coordinator, retryReceiptId),
                        94f, RuntimeUi.Warning);
                }
                UseContentDrivenBoardPanelHeight084(result);
                ArrangeIllustratedBoardCard091(resultCard,
                    !chestReveal092 && string.IsNullOrWhiteSpace(state.PendingCardVisualResourcePath)
                        ? "Revealed World Gate Room Icon 084"
                        : "Revealed Expedition Card Face 089", false);
                if (chestReveal092)
                    AttachChestRewardReveal092(resultCard, "Revealed Expedition Card Face 089", "Title",
                        new[] { "Message", "World Gate Chest Exact Reward 092" },
                        "WORLD_GATE_CHEST_092|" + state.PendingReceiptId,
                        state.PendingChestItemVisualId092, "CHEST OPENED  •  READY TO COLLECT",
                        state.PendingHasCheck ? BoardAdventureCommittedCheckRewardDelay084 :
                            BoardAdventurePawnTravelDuration084 + BoardAdventureCardFlipDuration084);
                RuntimeUi.SetLayout(resultCard, preferredHeight: 790f);
                ExpandWorldGateEventCard110(resultCard, chestReveal092);
                AnimateBoardAdventureCardFlip084(
                    resultCard,
                    "WORLD_GATE_CARD_084|" + state.PendingReceiptId,
                        (state.PendingIsEncounterRound
                            ? "ENCOUNTER ROUND " +
                              Math.Max(1, state.DeckChoiceRound)
                            : "ROOM " + AdventureCurrentPlayableSpace084(
                                state, AdventurePlayableSpaceCount084(state))) +
                        "  •  FACE DOWN");
                if (!waitingForRetry &&
                    !state.PendingRequiresCertifiedBattle)
                    ScheduleWorldGateReceiptApply092(
                        coordinator,
                        state.PendingReceiptId,
                        state.PendingHasCheck,
                        chestReveal092, resultCard.GetComponentInChildren<CommittedQuestDice132>(true));
                return;
            }

            if (state.ActiveStatus == "ReadyToFinalize")
            {
                var activeBoard=(state.Boards??Array.Empty<Campaign023.BoardView023>())
                    .FirstOrDefault(value=>StringComparer.Ordinal.Equals(
                        value.DefinitionId,state.ActiveDefinitionId));
                if((activeBoard?.RecruitContactCount??0)>0)
                    AddMessagePanel(body,"RECRUIT NETWORK DISCOVERED",
                        activeBoard.RecruitContactCount+
                        " new world contacts will be checked against your standing when you return. " +
                        "Any earned contacts join the Recruitment Desk pool; they are not handed to the roster.",
                        RuntimeUi.Positive);
                RuntimeUi.AddButton(body, "Return from completed quest board 084",
                    "RETURN TO THE GUILD & SAVE THE QUEST",
                    () => RunGuildCityCommand017D(coordinator.FinalizeWorldGateOperation023),
                    132f, RuntimeUi.Positive);
                return;
            }

            if (node.RequiresBattle && state.ActiveStatus == "Active")
            {
                BuildLockedWorldGateBattleDecision089(
                    body, coordinator, state, node);
                return;
            }

            if (state.ActiveStatus == "AwaitingBattle")
            {
                RuntimeUi.AddButton(body, "Return to active quest battle 084",
                    "RETURN TO THE UNION BATTLE",
                    () => Navigate(M1Screen.Battle), 126f, RuntimeUi.Warning);
                return;
            }

            if (state.RouteCards != null && state.RouteCards.Count > 0 &&
                coordinator is Campaign023.IExpeditionDeckPresentationCoordinator089 deckCoordinator)
            {
                BuildExpeditionRouteRow089(body, deckCoordinator, state);
                return;
            }

            var action = AddMessagePanel(body, "NEXT ROOM  •  FACE DOWN",
                "Continue to reveal the next saved room and resolve its event or reward.",
                RuntimeUi.Accent);
            var move = RuntimeUi.AddButton(action,
                "Move forward World Gate room 084",
                "MOVE FORWARD  •  FLIP NEXT ROOM",
                () => CommitAndRevealWorldGateRoom084(coordinator),
                126f, RuntimeUi.Accent);
            // The GOAL / NOW header already supplies the decision context. Put
            // the only action first inside this physical card so it remains
            // immediately reachable; the short explanatory copy follows it.
            move.transform.SetAsFirstSibling();
            UseContentDrivenBoardPanelHeight084(action);
        }

        void BuildLockedWorldGateBattleDecision089(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state,
            Campaign023.NodeView023 node)
        {
            var lockedEncounter = (state.RouteCards ?? Array.Empty<
                    Campaign023.ExpeditionRouteCardView089>())
                .FirstOrDefault(value => value != null && value.LockedToBattle);
            var eventCopy = lockedEncounter?.Description;
            if (string.IsNullOrWhiteSpace(eventCopy))
                eventCopy = !string.IsNullOrWhiteSpace(node.StoryFlavor)
                    ? node.StoryFlavor : node.Description;
            var rewardCopy = string.IsNullOrWhiteSpace(
                    lockedEncounter?.RewardPreview)
                ? node.RewardPreview : lockedEncounter.RewardPreview;
            var encounterKind = string.IsNullOrWhiteSpace(
                    lockedEncounter?.Category)
                ? "BATTLE" : lockedEncounter.Category;

            var card = RuntimeUi.AddPanel(body,
                "Locked Expedition Encounter Decision Card 089",
                new Color(0.08f, 0.025f, 0.025f, 0.98f));
            RuntimeUi.SetLayout(card, preferredHeight: 270f);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.Warning);
            RuntimeUi.AddHorizontalLayout(card.transform,
                new RectOffset(16, 16, 14, 14), 14f, TextAnchor.MiddleCenter);

            var visual = RuntimeUi.AddPanel(card.transform,
                "Locked Expedition Encounter Visual 089",
                new Color(0.015f, 0.025f, 0.045f, 1f));
            RuntimeUi.SetLayout(visual, preferredWidth: 340f,
                preferredHeight: 238f, flexibleWidth: 0.88f);
            M1PremiumUi.StylePanel(visual, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(visual.transform,
                new RectOffset(6, 6, 6, 6), 0f, TextAnchor.MiddleCenter);
            var visualResource = !string.IsNullOrWhiteSpace(
                                     lockedEncounter?.VisualResourcePath) &&
                                 Resources.Load<Sprite>(
                                     lockedEncounter.VisualResourcePath) != null
                ? lockedEncounter.VisualResourcePath
                : node.IconResource;
            if (!string.IsNullOrWhiteSpace(visualResource))
                GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                    visual.transform,
                    "Locked Expedition Encounter Card Face 089",
                    visualResource, 226f);

            var decision = RuntimeUi.AddPanel(card.transform,
                "Locked Expedition Encounter Decision 089", Color.clear);
            RuntimeUi.SetLayout(decision, preferredHeight: 238f,
                flexibleWidth: 1.45f);
            RuntimeUi.AddVerticalLayout(decision.transform,
                new RectOffset(8, 8, 8, 8), 4f, TextAnchor.UpperCenter);
            var heading = RuntimeUi.AddText(decision.transform,
                "Locked Expedition Encounter Heading 089",
                encounterKind + " ENCOUNTER  •  " +
                (node.Title ?? "STORY BATTLE").ToUpperInvariant(),
                20, TextAnchor.MiddleCenter, RuntimeUi.Warning,
                FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 32f);
            ConfigureResponsiveText062(heading, 18, 20);
            var lockCue = RuntimeUi.AddText(decision.transform,
                "Locked Expedition Story Battle Cue 089",
                "LOCKED STORY BATTLE  •  FULL UNION FORECAST COMBAT",
                14, TextAnchor.MiddleCenter, RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(lockCue, preferredHeight: 26f);

            var battleButton = RuntimeUi.AddButton(decision.transform,
                "Flip monster battle tile 084",
                "FACE THE MONSTER  •  ENTER UNION BATTLE",
                () =>
                {
                    var result = coordinator.EnterWorldGateBattle023();
                    _localStatus = result?.Message ??
                        "The battle could not begin.";
                    _localStatusPositive = result?.Succeeded == true;
                    if (_localStatusPositive) Navigate(M1Screen.Battle);
                    else BuildCurrentScreen();
                }, 132f, RuntimeUi.Accent);
            ConfigureResponsiveText062(
                battleButton.GetComponentInChildren<Text>(), 18, 21);

            var detail = RuntimeUi.AddText(decision.transform,
                "Locked Expedition Encounter Preview 089",
                "ENEMY  •  " + CompactWorldGateCopy084(eventCopy,
                    "A hostile Union blocks the route.", 96) +
                "\nREWARD  •  " + CompactWorldGateCopy084(
                    rewardCopy, "Certified battle rewards", 72),
                15, TextAnchor.UpperCenter, RuntimeUi.Text,
                FontStyle.Normal);
            RuntimeUi.SetLayout(detail, preferredHeight: 70f);

            DecorateBoardAdventureRevealedCard084(card.rectTransform,
                lockedEncounter?.VisualCategoryKey ?? "BATTLE", rewardCopy);
            AnimateBoardAdventureCardFlip084(card.rectTransform,
                "WORLD_GATE_LOCKED_BATTLE_CARD_089|" +
                state.ActiveOperationId + "|" + node.NodeId,
                "STORY ENCOUNTER  •  FACE DOWN");
            // Decoration and the temporary flip stage add overlay siblings.
            // Keep the authored enemy illustration unambiguously first in the
            // encounter hierarchy; ignored-layout overlays do not affect the
            // side-by-side geometry.
            visual.transform.SetAsFirstSibling();
        }

        void CommitAndRevealWorldGateRoom084(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator)
        {
            RunWorldGateAnimatedCommand084(
                coordinator == null
                    ? null
                    : (Func<M1CommandResult>)coordinator.CommitAutomaticWorldGateRoom023,
                "The quest board is unavailable. Try opening Missions again.");
        }

        void RunWorldGateAnimatedCommand084(
            Func<M1CommandResult> command,
            string unavailableMessage084)
        {
            if (_worldGateRoomCommandRunning084) return;
            _worldGateRoomCommandRunning084 = true;
            M1CommandResult result;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                result = command?.Invoke() ?? M1CommandResult.Failure(
                    string.IsNullOrWhiteSpace(unavailableMessage084)
                        ? "The quest board is unavailable. Try opening Missions again."
                        : unavailableMessage084);
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _worldGateRoomCommandRunning084 = false;
            }
            _localStatus = result?.Succeeded == true
                ? string.Empty
                : result?.Message ?? unavailableMessage084;
            _localStatusPositive = result?.Succeeded == true;
            BuildCurrentScreen();
        }

        void ScheduleWorldGateReceiptApply084(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            string receiptId,
            bool hasCheck) => ScheduleWorldGateReceiptApply092(coordinator, receiptId, hasCheck, false);

        void ScheduleWorldGateReceiptApply092(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            string receiptId,
            bool hasCheck,
            bool chestReveal092, CommittedQuestDice132 dice132 = null)
        {
            if (!Application.isPlaying || coordinator == null ||
                string.IsNullOrWhiteSpace(receiptId) ||
                !_scheduledWorldGateReceiptApplies084.Add(receiptId)) return;
            var routine = StartCoroutine(ApplyWorldGateReceiptAfterReveal084(
                coordinator, receiptId, hasCheck, chestReveal092, dice132));
            BindWorldGateEventReceiptRoutine110(receiptId, routine);
        }

        IEnumerator ApplyWorldGateReceiptAfterReveal084(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            string expectedReceiptId,
            bool hasCheck,
            bool chestReveal092 = false, CommittedQuestDice132 dice132 = null)
        {
            try
            {
                var completeRevealDuration084 = _reducedMotion
                    ? 0f
                    : hasCheck
                        ? BoardAdventureCommittedCheckRewardDelay084 +
                          BoardAdventureRewardRevealDuration084
                        : BoardAdventurePawnTravelDuration084 +
                          BoardAdventureCardFlipDuration084 +
                          BoardAdventureRewardRevealDuration084;
                if (chestReveal092 && !_reducedMotion)
                    completeRevealDuration084 = (hasCheck ? BoardAdventureCommittedCheckRewardDelay084 :
                        BoardAdventurePawnTravelDuration084 + BoardAdventureCardFlipDuration084) +
                        BoardChestReveal092.OpeningDuration092;
                if (dice132 != null)
                {
                    // A committed result cannot advance while the player has not rolled.
                    // The view owns cosmetic time; the existing receipt owns the only command.
                    while (dice132 != null && !dice132.IsSettled132)
                    {
                        if (!dice132.isActiveAndEnabled) yield break;
                        yield return null;
                    }
                    if (dice132 == null || !dice132.isActiveAndEnabled) yield break;
                    var chest132 = chestReveal092 ? dice132.CopyScope132
                        .GetComponentInChildren<BoardChestReveal092>(true) : null;
                    while (chest132 != null && chest132.IsPlaying092)
                    {
                        if (dice132 == null || !dice132.isActiveAndEnabled) yield break;
                        yield return null;
                    }
                    completeRevealDuration084 = _reducedMotion || chestReveal092 ? 0f :
                        BoardAdventureRewardAfterDicePause084 + BoardAdventureRewardRevealDuration084;
                }
                yield return new WaitForSecondsRealtime(
                    completeRevealDuration084 + WorldGateRoomResultHold084);

                var latest = coordinator?.CampaignWorldGate023;
                if (latest == null || !StringComparer.Ordinal.Equals(
                        latest.PendingReceiptId, expectedReceiptId)) yield break;
                DetachWorldGateEventReceiptRoutine110(expectedReceiptId);
                ApplyWorldGateReceiptNow084(coordinator, expectedReceiptId);
            }
            finally
            {
                // Unity disposes an interrupted iterator when its host is stopped;
                // this keeps a saved pending receipt schedulable after re-entry.
                _scheduledWorldGateReceiptApplies084.Remove(expectedReceiptId);
            }
        }

        void ApplyWorldGateReceiptNow084(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            string expectedReceiptId)
        {
            var latest = coordinator?.CampaignWorldGate023;
            if (latest == null || !StringComparer.Ordinal.Equals(
                    latest.PendingReceiptId, expectedReceiptId))
            {
                ClearWorldGateReceiptRetry084(expectedReceiptId);
                BuildCurrentScreen();
                return;
            }

            M1CommandResult applied;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                applied = coordinator.ApplyWorldGateReceipt023();
            }
            catch (Exception exception)
            {
                applied = M1CommandResult.Failure(
                    "The saved result could not be applied: " + exception.Message);
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
            }

            if (applied?.Succeeded == true)
            {
                ClearWorldGateReceiptRetry084(expectedReceiptId);
            }
            else
            {
                _worldGateReceiptRetryId084 = expectedReceiptId ?? string.Empty;
                _worldGateReceiptRetryMessage084 = applied?.Message ??
                    "The result is already saved. Retry applying it to continue.";
            }
            _localStatus = applied?.Succeeded == true
                ? string.Empty
                : applied?.Message ?? "The saved room result could not be applied.";
            _localStatusPositive = applied?.Succeeded == true;
            BuildCurrentScreen();
        }

        void ClearWorldGateReceiptRetry084(string receiptId)
        {
            if (!string.IsNullOrWhiteSpace(receiptId) &&
                !StringComparer.Ordinal.Equals(
                    _worldGateReceiptRetryId084, receiptId)) return;
            _worldGateReceiptRetryId084 = string.Empty;
            _worldGateReceiptRetryMessage084 = string.Empty;
        }

        void ClearWorldGateReceiptApplyScheduling084()
        {
            _scheduledWorldGateReceiptApplies084.Clear();
        }

        static bool IsWorldGateSetback084(string outcome) =>
            !string.IsNullOrWhiteSpace(outcome) &&
            (outcome.IndexOf("SETBACK", StringComparison.OrdinalIgnoreCase) >= 0 ||
             outcome.IndexOf("DEFEAT", StringComparison.OrdinalIgnoreCase) >= 0 ||
             outcome.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0);

        static string WorldGateCurrentTask084(
            Campaign023.CampaignWorldGatePresentationState023 state,
            Campaign023.NodeView023 node)
        {
            var revealed = !string.IsNullOrWhiteSpace(state?.PendingReceiptId) ||
                           node?.RequiresBattle == true ||
                           state?.ActiveStatus == "AwaitingBattle" ||
                           state?.ActiveStatus == "ReadyToFinalize";
            return revealed && !string.IsNullOrWhiteSpace(node?.Objective)
                ? node.Objective
                : "Move forward and flip the next room.";
        }

        static string CompactWorldGateCopy084(
            string value,
            string fallback,
            int maximumLength) =>
            CompactBoardQuestCopy081(
                string.IsNullOrWhiteSpace(value) ? fallback : value,
                maximumLength);

        static int AdventurePlayableSpaceCount084(
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var highestSpace084 = (state?.AdventureTiles ??
                                   Array.Empty<Campaign023.AdventureTrackTileView023>())
                .Where(value => value != null)
                .Select(value => value.SpaceNumber)
                .DefaultIfEmpty(0)
                .Max();
            return Math.Max(1, highestSpace084 > 0
                ? highestSpace084
                : state?.TotalNodes ?? 1);
        }

        static int AdventureCurrentPlayableSpace084(
            Campaign023.CampaignWorldGatePresentationState023 state,
            int playableRoomCount084)
        {
            var current084 = (state?.AdventureTiles ??
                              Array.Empty<Campaign023.AdventureTrackTileView023>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.State,
                        Campaign023.AdventureBoardTrackProjection084.CurrentState));
            var inferred084 = current084?.SpaceNumber ??
                              Math.Max(1, (state?.CompletedNodes ?? 0) + 1);
            return Math.Min(Math.Max(1, playableRoomCount084),
                Math.Max(1, inferred084));
        }

        void BuildAdventureTrack084(
            Transform body,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var tiles = (state.AdventureTiles ??
                         Array.Empty<Campaign023.AdventureTrackTileView023>())
                .OrderBy(value => value.SpaceNumber)
                .ThenBy(value => value.AuthoredOrder)
                .ToArray();
            if (tiles.Length == 0)
            {
                var fallbackCount = Math.Max(1, state.TotalNodes);
                var current = Math.Max(0, Math.Min(fallbackCount - 1,
                    state.CompletedNodes));
                tiles = Enumerable.Range(0, fallbackCount)
                    .Select(index => new Campaign023.AdventureTrackTileView023
                    {
                        AuthoredOrder = index,
                        SpaceNumber = index + 1,
                        BranchCount = 1,
                        State = index < current
                            ? Campaign023.AdventureBoardTrackProjection084.ClearedState
                            : index == current
                                ? Campaign023.AdventureBoardTrackProjection084.CurrentState
                                : Campaign023.AdventureBoardTrackProjection084.FaceDownState,
                        RevealedLabel = index == current ? "CURRENT ROOM" : "ROOM CLEARED"
                    })
                    .ToArray();
            }

            var spaces = tiles
                .GroupBy(value => value.SpaceNumber)
                .OrderBy(value => value.Key)
                .ToArray();
            var currentIndex = Array.FindIndex(spaces, group => group.Any(value =>
                value.State == Campaign023.AdventureBoardTrackProjection084.CurrentState));
            if (currentIndex < 0)
                currentIndex = Math.Max(0, Math.Min(spaces.Length - 1,
                    state.CompletedNodes));
            const int visibleRooms = 5;
            var windowStart = Math.Max(0, Math.Min(currentIndex - 2,
                Math.Max(0, spaces.Length - visibleRooms)));
            var visibleSpaces = spaces.Skip(windowStart).Take(visibleRooms).ToArray();

            var track = RuntimeUi.AddPanel(body, "Adventure Compact Progress Strip 084",
                new Color(0.006f, 0.016f, 0.028f, 0.96f));
            RuntimeUi.SetLayout(track, preferredHeight: 1f);
            M1PremiumUi.StylePanel(track, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(track.transform,
                new RectOffset(12, 12, 10, 12), 8f, TextAnchor.UpperCenter);
            var maxSpace = spaces.Length == 0 ? 1 : spaces.Max(value => value.Key);
            var pawnSpace = currentIndex >= 0 && currentIndex < spaces.Length
                ? spaces[currentIndex].Key
                : Math.Min(maxSpace, Math.Max(1, state.CompletedNodes + 1));
            var heading = RuntimeUi.AddText(track.transform,
                "Adventure Compact Progress Heading 084",
                "QUEST MAP  •  BRANCHING PATH  •  ROOM " + pawnSpace +
                " OF " + maxSpace,
                20, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 38f);
            ConfigureResponsiveText062(heading, 14, 21);

            var row = AddRow(track.transform,
                "Adventure Compact Progress Row 084", 8f, 106f);
            foreach (var space in visibleSpaces)
            {
                var values = space.ToArray();
                var current = values.Any(value => value.State ==
                    Campaign023.AdventureBoardTrackProjection084.CurrentState);
                var complete = values.Any(value => value.State ==
                    Campaign023.AdventureBoardTrackProjection084.ClearedState);
                var currentRevealed = current &&
                    (!string.IsNullOrWhiteSpace(state.PendingReceiptId) ||
                     state.CurrentNode?.RequiresBattle == true ||
                     state.ActiveStatus == "AwaitingBattle" ||
                     state.ActiveStatus == "ReadyToFinalize");
                var revealed = values.FirstOrDefault(value =>
                    !string.IsNullOrWhiteSpace(value.RevealedLabel));
                var copyValue = current
                    ? "●  YOUR PAWN\nROOM " + space.Key + "\n" +
                      (currentRevealed
                          ? CompactWorldGateCopy084(
                              revealed?.RevealedLabel, "ROOM REVEALED", 30)
                          : "FACE DOWN")
                    : complete
                        ? "✓  ROOM " + space.Key + "\nCLEARED"
                        : "◇  ROOM " + space.Key + "\nFACE DOWN";
                var tile = RuntimeUi.AddPanel(row,
                    "Adventure Compact Room " + space.Key + " 084", Color.white);
                RuntimeUi.SetLayout(tile, preferredHeight: 96f, flexibleWidth: 1f);
                M1PremiumUi.StylePanel(tile,
                    current ? M1PremiumUi.Surface.Warning :
                    complete ? M1PremiumUi.Surface.Positive :
                    M1PremiumUi.Surface.WorldGlass);
                BuildAdventureBranchMarkers089(tile.transform, values);
                var copy = RuntimeUi.AddText(tile.transform,
                    "Adventure Compact Room Copy " + space.Key + " 084",
                    copyValue, 16, TextAnchor.MiddleCenter,
                    current ? RuntimeUi.Warning :
                    complete ? RuntimeUi.Positive : RuntimeUi.MutedText,
                    FontStyle.Bold);
                Stretch(copy.rectTransform);
                copy.rectTransform.offsetMin = new Vector2(
                    current ? 48f : 5f, 4f);
                copy.rectTransform.offsetMax = new Vector2(-5f, -24f);
                ConfigureAuthoredCompactText076(copy, 10, 17);
                if (current)
                    BuildBoardAdventurePawn084(
                        tile.transform,
                        "WORLD_GATE_PAWN_086|" +
                        (state.ActiveOperationId ?? string.Empty) + "|" +
                        space.Key,
                        true);
            }
            for (var fillerIndex = visibleSpaces.Length;
                 fillerIndex < visibleRooms; fillerIndex++)
            {
                var filler = RuntimeUi.AddPanel(row,
                    "Adventure Compact Empty Room " + fillerIndex + " 084",
                    Color.clear);
                RuntimeUi.SetLayout(filler, preferredHeight: 96f, flexibleWidth: 1f);
                filler.raycastTarget = false;
            }
            UseContentDrivenBoardPanelHeight084(track.transform);
        }

        void BuildAdventureBranchMarkers089(
            Transform room,
            IReadOnlyList<Campaign023.AdventureTrackTileView023> branches)
        {
            if (room == null || branches == null || branches.Count == 0) return;
            var lane = RuntimeUi.AddPanel(room,
                "Adventure Quest Map Branch Lane 089", Color.clear);
            var laneLayout = lane.gameObject.AddComponent<LayoutElement>();
            laneLayout.ignoreLayout = true;
            lane.rectTransform.anchorMin = new Vector2(0.12f, 0.77f);
            lane.rectTransform.anchorMax = new Vector2(0.88f, 0.98f);
            lane.rectTransform.offsetMin = Vector2.zero;
            lane.rectTransform.offsetMax = Vector2.zero;
            RuntimeUi.AddHorizontalLayout(lane.transform,
                new RectOffset(1, 1, 1, 1), 4f, TextAnchor.MiddleCenter);
            foreach (var branch in branches.OrderBy(value => value.BranchIndex))
            {
                var isCurrent = StringComparer.Ordinal.Equals(branch.State,
                    Campaign023.AdventureBoardTrackProjection084.CurrentState);
                var isCleared = StringComparer.Ordinal.Equals(branch.State,
                    Campaign023.AdventureBoardTrackProjection084.ClearedState);
                var isClosed = StringComparer.Ordinal.Equals(branch.State,
                    Campaign023.AdventureBoardTrackProjection084.ClosedState);
                var marker = RuntimeUi.AddPanel(lane.transform,
                    "Adventure Quest Map Node " + branch.SpaceNumber + " " +
                    branch.BranchIndex + " 089", Color.white);
                RuntimeUi.SetLayout(marker, preferredWidth: 26f,
                    preferredHeight: 22f, flexibleWidth: 0f);
                M1PremiumUi.StylePanel(marker,
                    isCurrent ? M1PremiumUi.Surface.Warning :
                    isCleared ? M1PremiumUi.Surface.Positive :
                    isClosed ? M1PremiumUi.Surface.Iron :
                    M1PremiumUi.Surface.WorldRibbon);
                var glyph = RuntimeUi.AddText(marker.transform,
                    "Adventure Quest Map Node Glyph 089",
                    isCurrent ? "●" : isCleared ? "✓" : isClosed ? "×" : "◇",
                    14, TextAnchor.MiddleCenter,
                    isCurrent ? RuntimeUi.Warning :
                    isCleared ? RuntimeUi.Positive : RuntimeUi.MutedText,
                    FontStyle.Bold);
                Stretch(glyph.rectTransform);
                glyph.raycastTarget = false;
            }
        }

        void BuildAdventureBoardShelf084(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var allBoards = state.Boards ?? Array.Empty<Campaign023.BoardView023>();
            var requiredStory = allBoards.FirstOrDefault(value =>
                value.IsCurrentStoryBoard &&
                !value.Available &&
                !StringComparer.Ordinal.Equals(value.WorldId, state.CurrentWorldId));
            if (requiredStory != null)
                BuildRequiredStoryTravel084(body, coordinator, state, requiredStory);

            var available = allBoards
                .Where(value => value.Available)
                .OrderBy(value => value.IsCurrentStoryBoard ? 0 : value.Kind == "REPEATABLE" ? 1 : 2)
                .ThenBy(value => value.Title)
                .ToArray();
            if (available.Length == 0 && requiredStory == null)
            {
                AddMessagePanel(body, "NO QUEST READY",
                    "Continue the current story in Campaign, or finish an active Guild operation. " +
                    "New contracts appear here when their chapter unlocks them.",
                    RuntimeUi.ButtonNormal);
                return;
            }

            var recommended = available.FirstOrDefault();
            if (recommended != null)
                BuildAdventureBoardOffer084(body, coordinator, recommended, true);

            if (available.Length <= 1) return;
            var toggle = RuntimeUi.AddButton(
                body,
                "Toggle additional adventure boards 084",
                _showMoreAdventureBoards084
                    ? "HIDE MORE QUESTS"
                    : "MORE QUESTS  •  " + (available.Length - 1),
                () =>
                {
                    _showMoreAdventureBoards084 = !_showMoreAdventureBoards084;
                    BuildCurrentScreen();
                },
                104f,
                RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(toggle.GetComponentInChildren<Text>(), 16, 25);
            if (!_showMoreAdventureBoards084) return;

            foreach (var board in available.Skip(1).Take(5))
                BuildAdventureBoardOffer084(body, coordinator, board, false);
            if (available.Length > 6)
                AddMessagePanel(body, "MORE QUESTS IN OTHER WORLDS",
                    (available.Length - 6) +
                    " additional contracts are available through Change World.",
                    RuntimeUi.ButtonNormal);
        }

        void BuildAdventureBoardOffer084(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.BoardView023 board,
            bool recommended)
        {
            if (board == null) return;
            var captured = board;
            var panel = AddMessagePanel(body,
                (recommended ? "PLAY NEXT  •  " : string.Empty) +
                board.Title.ToUpperInvariant(),
                CompactWorldGateCopy084(
                    board.PlayerKind + " in " + board.WorldName + ". " +
                    board.NodeCount + " cards. " + AdventureDeckSummary088(board) + ".",
                    "A Guild quest is ready.",
                    126) +
                "\nGOAL  •  " + CompactWorldGateCopy084(
                    board.Objective,
                    "Complete the quest and return to the Guild.", 132) +
                "\nSUCCESS REWARD  •  " + CompactWorldGateCopy084(
                    board.RewardPreview, "Story progress", 104),
                recommended ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            RuntimeUi.AddButton(panel,
                "Begin adventure board " + board.DefinitionId + " 084",
                recommended && board.IsCurrentStoryBoard
                    ? "START STORY QUEST"
                    : recommended
                        ? "START RECOMMENDED QUEST"
                        : "START QUEST",
                () => RunWorldGateAnimatedCommand084(
                    () => coordinator.BeginWorldGateOperation023(captured.DefinitionId),
                    "The quest could not begin. Try opening Missions again."),
                recommended ? 132f : 112f,
                recommended ? RuntimeUi.Accent : RuntimeUi.Positive);
            UseContentDrivenBoardPanelHeight084(panel);
        }

        static string AdventureDeckSummary088(Campaign023.BoardView023 board)
        {
            if(board==null)return "STORY • REWARDS • BATTLES";
            var parts=new List<string>();
            if(board.TreasureCardCount>0)
                parts.Add(board.TreasureCardCount+" CHEST"+
                    (board.TreasureCardCount==1?string.Empty:"S"));
            if(board.DiceCardCount>0)
                parts.Add(board.DiceCardCount+" DICE");
            if(board.StoryCardCount>0)
                parts.Add(board.StoryCardCount+" STORY");
            if(board.BattleCardCount>0)
                parts.Add(board.BattleCardCount+" BATTLE"+
                    (board.BattleCardCount==1?string.Empty:"S"));
            if(board.RecruitContactCount>0)
                parts.Add("UP TO "+board.RecruitContactCount+" RECRUIT CONTACTS");
            return parts.Count==0?"STORY PROGRESS":string.Join("  •  ",parts);
        }

        void BuildRequiredStoryTravel084(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state,
            Campaign023.BoardView023 story)
        {
            var destination = (state.Standings ?? Array.Empty<Campaign023.StandingView023>())
                .FirstOrDefault(value => value.WorldId == story.WorldId);
            var unlocked = destination?.Unlocked == true;
            var canTravel = unlocked && destination.CanAffordTravel;
            var panel = AddMessagePanel(body,
                "STORY QUEST ROAD  •  " + story.Title.ToUpperInvariant(),
                "Your story pawn belongs in " + story.WorldName + ". " +
                (canTravel
                    ? "Travel there now; the correct quest board will be waiting at the top."
                    : !unlocked
                        ? "This road has not unlocked yet. Return to Campaign and claim the preceding chapter result."
                        : "You need " + destination.TravelSupplyCost +
                          " travel supplies to reach this story quest."),
                canTravel ? RuntimeUi.Warning : RuntimeUi.ButtonNormal);
            if (canTravel)
                RuntimeUi.AddButton(panel,
                    "Travel to required story board 084",
                    "TRAVEL TO " + story.WorldName.ToUpperInvariant() +
                    "  •  OPEN STORY QUEST",
                    () => RunGuildCityCommand017D(
                        () => coordinator.TravelWorldGate023(story.WorldId)),
                    132f, RuntimeUi.Accent);
            UseContentDrivenBoardPanelHeight084(panel);
        }

        void BuildAdventureWorldTravel084(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            if (!string.IsNullOrWhiteSpace(state.ActiveOperationId)) return;
            var worlds = (state.Standings ?? Array.Empty<Campaign023.StandingView023>())
                .Where(value => value.Unlocked)
                .OrderBy(value => value.DisplayName)
                .ToArray();
            if (worlds.Length <= 1) return;

            var toggle = RuntimeUi.AddButton(
                body,
                "Toggle adventure world travel 084",
                _showAdventureWorldTravel084
                    ? "CLOSE CHANGE WORLD"
                    : "CHANGE WORLD  •  " + (state.CurrentWorldName ?? "SKYHOME"),
                () =>
                {
                    _showAdventureWorldTravel084 = !_showAdventureWorldTravel084;
                    BuildCurrentScreen();
                },
                104f,
                RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(toggle.GetComponentInChildren<Text>(), 16, 25);
            if (!_showAdventureWorldTravel084) return;

            AddMessagePanel(body, "CHANGE WORLD",
                "CURRENT  •  " + (state.CurrentWorldName ?? "SKYHOME") +
                "  •  TRAVEL SUPPLIES  " + state.TravelSupplies,
                RuntimeUi.ButtonNormal);
            foreach (var world in worlds
                .Where(value => value.WorldId != state.CurrentWorldId).Take(5))
            {
                var captured = world;
                var travelButton = RuntimeUi.AddButton(body,
                    "Travel adventure board " + world.WorldId + " 084",
                    "TRAVEL TO  " + world.DisplayName.ToUpperInvariant() +
                    "  •  COST " + world.TravelSupplyCost + " SUPPLIES" +
                    (world.CanAffordTravel ? string.Empty : "  •  NEED MORE"),
                    () => RunGuildCityCommand017D(
                        () => coordinator.TravelWorldGate023(captured.WorldId)),
                    112f, world.CanAffordTravel ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                travelButton.interactable = world.CanAffordTravel;
            }
            if (state.CurrentWorldId != "SKYHOME")
                RuntimeUi.AddButton(body, "Return Skyhome adventure board 084",
                    "RETURN TO SKYHOME",
                    () => RunGuildCityCommand017D(
                        () => coordinator.TravelWorldGate023("SKYHOME")),
                    112f, RuntimeUi.Positive);
        }
    }
}
