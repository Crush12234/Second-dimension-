using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017E;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017D
{
    public enum ExpeditionBoardActionKind074
    {
        ChooseContract,
        BeginExpedition,
        ResolveObjective,
        ResolveCheck,
        CommitEncounter,
        EnterBattle,
        AwaitBattleReturn,
        ChooseRoute,
        FinalizeOperation,
        Regroup
    }

    public sealed class ExpeditionBoardNodeView074
    {
        public string NodeId { get; set; }
        public string DisplayName { get; set; }
        public string Kind { get; set; }
        public string Summary { get; set; }
        public string Risk { get; set; }
        public string RouteCostSummary { get; set; }
        public Vector2 NormalizedPosition { get; set; }
        public Vector2 LabelOffset { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsVisited { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsLocked { get; set; }
        public bool IsObjective { get; set; }
        public bool IsCamp { get; set; }
        public bool IsThreat { get; set; }
        public string StateLabel { get; set; }
    }

    public sealed class ExpeditionBoardView074
    {
        public string BoardId { get; set; }
        public string BoardTitle { get; set; }
        public string OperationTitle { get; set; }
        public string RouteTitle { get; set; }
        public string MapResourcePath { get; set; }
        public string StoryObjective { get; set; }
        public string CurrentLocationName { get; set; }
        public string PositionStatus { get; set; }
        public ExpeditionBoardActionKind074 ActionKind { get; set; }
        public IReadOnlyList<ExpeditionBoardNodeView074> Nodes { get; set; } =
            Array.Empty<ExpeditionBoardNodeView074>();
    }

    /// <summary>
    /// Pure projection used by the fixed-screen route board and its tests. Node
    /// IDs remain internal save keys; every player-facing label is authored copy.
    /// </summary>
    public static class ExpeditionBoardProjection074
    {
        public const string FirstBoardId074 = "BOARD_BELL_BENEATH_GATE_071";
        public const string ChapterTwoBoardId074 = "BOARD_LINES_NOT_RETURNED";
        public const string ChapterThreeBoardId165 = "BOARD_RELIEF_ROAD";
        public const int NodeCount074 = 15;
        public const int ChapterPhaseCount074 = 5;
        public const float MinimumSupportedAspect074 = 1.6f;
        public const string ExpeditionRouteMapResource076 =
            "SecondDimension/Art/Guided063/EXPEDITION_ROUTE_MAP_V63";
        public const string ExpeditionHallBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071";
        public const string ExpeditionLanternRoadBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/LANTERN_ROAD_GAMEPLAY_PLATE_071";
        public const string ExpeditionGatehouseBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/GATEHOUSE_BOSS_ARENA_071";
        public const string GuildUndercroftLedgerBackdropResource086 =
            "SecondDimension/Art/Board086/SKYHOME_GUILD_UNDERCROFT_LEDGER_086";
        public const string LanternRoadCacheBackdropResource086 =
            "SecondDimension/Art/Board086/LANTERN_ROAD_QUARTERMASTER_CACHE_086";
        public const string WayglassThresholdBackdropResource086 =
            "SecondDimension/Art/Board086/WAYGLASS_THRESHOLD_086";
        public const string BrokenSurveyBridgeBackdropResource086 =
            "SecondDimension/Art/Board086/WAYGLASS_BROKEN_SURVEY_BRIDGE_086";
        public const string LostSurveyCampBackdropResource086 =
            "SecondDimension/Art/Board086/WAYGLASS_LOST_SURVEY_CAMP_086";
        public const string UnrecordedDoorBackdropResource086 =
            "SecondDimension/Art/Board086/WAYGLASS_UNRECORDED_DOOR_086";
        public const string ChapterTwoSellaPortraitResource079 =
            "SecondDimension/Art/Portraits/ChapterTwo079/SELLA_VEY_PORTRAIT_079";
        public const string ChapterTwoOrraPortraitResource079 =
            "SecondDimension/Art/Portraits/ChapterTwo079/ORRA_VALE_PORTRAIT_079";
        public const string ChapterTwoStorySceneHeading079 = "STORY SCENE";
        public const string ChapterTwoFieldOrderHeading079 = "FIELD ORDER";
        public const string PatrolFoundDecisionTitle076 = "ZORIN'S PATROL FOUND";
        public const string PatrolFoundDecisionCopy076 =
            "Zorin's patrol is pinned behind the Gatehouse supports. " +
            "Rally the survivors first and recover the Wayglass. Then confront the Gate-Eater.";
        public const string RescueCeremonyRosterTruth076 =
            "MAREN  •  “Nineteen stand at this gate. Eighteen are assigned to this fight; Bessa holds the Hall.\nAll twenty belong to the Guild—and this charter can one day field sixty.”";
        public const string CarefulApproachLabel076 =
            "CAREFUL\nPARTNER +2\nCOST 1 PRESSURE";
        public const string SwiftApproachLabel076 =
            "SWIFT\nLEAD +0\nCOST 0 PRESSURE";

        private static readonly string[] PhaseTitles074 =
        {
            "DEPART", "TRACK", "CONFRONT", "SECURE", "RETURN"
        };

        private static readonly string[] PhaseSubtitles074 =
        {
            "Leave the Guild", "Follow the trail", "Break the threat",
            "Complete objective", "Report home"
        };

        private static readonly string[] FallbackNames074 =
        {
            "Skyhome Gate", "Hall Breach", "Patrol Runner", "Waymarker Overlook",
            "Broken Waymarker", "Patrol Cache", "Lantern Ambush", "Watch Camp",
            "Wayglass Echo", "Gatehouse Stalker", "Trapped Patrol", "Route Ledger",
            "Service Passage", "Old Gatehouse", "Road Home"
        };

        private static readonly string[] FallbackKinds074 =
        {
            "START", "ENCOUNTER", "EVENT", "SCOUTING", "SKILL_CHECK", "RESOURCE",
            "ENCOUNTER", "CAMP", "EVENT", "OPTIONAL_ELITE", "SKILL_CHECK",
            "SECONDARY_OBJECTIVE", "SAFE_ROUTE", "MAIN_OBJECTIVE", "EXIT"
        };

        public static ExpeditionBoardView074 Build(GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            var activeContract = (state?.Contracts ?? Array.Empty<GuildCityContractView017D>())
                .FirstOrDefault(value => value != null && value.IsActive &&
                                         !value.IsCompleted && !value.IsFailed);
            var boardId = string.IsNullOrWhiteSpace(expedition?.BoardId)
                ? string.IsNullOrWhiteSpace(activeContract?.BoardId)
                    ? FirstBoardId074
                    : activeContract.BoardId
                : expedition.BoardId;
            var currentNodeId = string.IsNullOrWhiteSpace(expedition?.CurrentNodeId)
                ? "N00"
                : expedition.CurrentNodeId;
            var visited = new HashSet<string>(
                expedition?.VisitedNodeIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var revealed = new HashSet<string>(
                expedition?.RevealedNodeIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var available = new HashSet<string>(
                expedition != null && expedition.CanMove
                    ? expedition.LinkedNodeIds ?? Array.Empty<string>()
                    : Array.Empty<string>(),
                StringComparer.Ordinal);

            if (expedition == null && state?.HasActiveContract == true)
                available.Add("N00");

            var nodes = new List<ExpeditionBoardNodeView074>(NodeCount074);
            for (var index = 0; index < NodeCount074; index++)
            {
                var nodeId = "N" + index.ToString("00");
                var presentation = SafeNode074(boardId, nodeId);
                var kind = string.IsNullOrWhiteSpace(presentation?.kind)
                    ? FallbackKinds074[index]
                    : presentation.kind;
                var isCurrent = expedition != null &&
                                StringComparer.Ordinal.Equals(currentNodeId, nodeId);
                var isAvailable = available.Contains(nodeId);
                var isVisited = visited.Contains(nodeId);
                var isRevealed = revealed.Contains(nodeId) || isAvailable || isVisited || isCurrent;
                var isObjective = StringComparer.Ordinal.Equals(kind, "MAIN_OBJECTIVE") ||
                                  StringComparer.Ordinal.Equals(kind, "SECONDARY_OBJECTIVE") ||
                                  StringComparer.Ordinal.Equals(kind, "EXIT");
                var isCamp = StringComparer.Ordinal.Equals(kind, "CAMP");
                var isThreat = StringComparer.Ordinal.Equals(kind, "ENCOUNTER") ||
                               StringComparer.Ordinal.Equals(kind, "OPTIONAL_ELITE") ||
                               StringComparer.OrdinalIgnoreCase.Equals(presentation?.risk, "High") ||
                               StringComparer.OrdinalIgnoreCase.Equals(presentation?.risk, "Very High");
                nodes.Add(new ExpeditionBoardNodeView074
                {
                    NodeId = nodeId,
                    DisplayName = ShortName074(index, presentation?.displayName),
                    Kind = kind,
                    Summary = PlayerFacingRouteSummary079(
                        presentation?.summary ?? FallbackSummary074(kind)),
                    Risk = presentation?.risk ?? (isThreat ? "High" : "Unknown"),
                    RouteCostSummary = presentation?.routeCostSummary ?? "Route cost shown on arrival",
                    NormalizedPosition = Position074(index),
                    LabelOffset = LabelOffset074(index),
                    IsCurrent = isCurrent,
                    IsVisited = isVisited,
                    IsAvailable = isAvailable,
                    IsRevealed = isRevealed,
                    IsLocked = !isCurrent && !isVisited && !isAvailable,
                    IsObjective = isObjective,
                    IsCamp = isCamp,
                    IsThreat = isThreat,
                    StateLabel = StateLabel074(isCurrent, isAvailable, isVisited, isObjective, isCamp, isThreat)
                });
            }

            var current = nodes.FirstOrDefault(value => value.IsCurrent) ?? nodes[0];
            return new ExpeditionBoardView074
            {
                BoardId = boardId,
                BoardTitle = BoardTitle074(boardId),
                OperationTitle = OperationTitle074(boardId),
                RouteTitle = RouteTitle074(boardId, expedition),
                MapResourcePath = SafeMapPath074(boardId),
                StoryObjective = StoryObjective074(state, current.DisplayName),
                CurrentLocationName = current.DisplayName,
                PositionStatus = PositionStatus074(state, current.DisplayName),
                ActionKind = ActionKind074(state),
                Nodes = nodes.AsReadOnly()
            };
        }

        /// <summary>
        /// The save keeps fifteen authored route nodes, but the player-facing board
        /// groups them into five readable operation phases. This avoids presenting
        /// a database graph while retaining the exact authoritative route state.
        /// </summary>
        public static int PhaseIndexForNode074(string nodeId)
        {
            var step = StepNumberForNode074(nodeId);
            if (step <= 2) return 0;
            if (step <= 6) return 1;
            if (step <= 10) return 2;
            if (step <= 14) return 3;
            return 4;
        }

        public static int StepNumberForNode074(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodeId.Length != 3 ||
                nodeId[0] != 'N' || !int.TryParse(nodeId.Substring(1), out var index))
                return 1;
            return Mathf.Clamp(index + 1, 1, NodeCount074);
        }

        public static string PhaseTitle074(int phaseIndex)
        {
            return PhaseTitles074[Mathf.Clamp(phaseIndex, 0, ChapterPhaseCount074 - 1)];
        }

        public static string PhaseSubtitle074(int phaseIndex)
        {
            return PhaseSubtitles074[Mathf.Clamp(phaseIndex, 0, ChapterPhaseCount074 - 1)];
        }

        public static string ChapterLabel074(string boardId)
        {
            if (IsFirstStoryBoard074(boardId)) return "CHAPTER 1";
            if (StringComparer.Ordinal.Equals(boardId, ChapterTwoBoardId074))
                return "CHAPTER 2";
            return "GUILD OPERATION";
        }

        public static bool NeedsFirstHourPatrolRescue074(GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            if (expedition == null ||
                !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13") ||
                !IsFirstStoryBoard074(expedition.BoardId)) return false;
            return !HasFirstHourPatrolRescueProof074(expedition.ObjectiveFlags);
        }

        public static bool HasFirstHourPatrolRescueProof074(
            IReadOnlyList<string> objectiveFlags)
        {
            if (SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                .HasFirstHourLanternPatrolRescued071(objectiveFlags)) return true;
            return (objectiveFlags ?? Array.Empty<string>()).Any(value =>
                StringComparer.Ordinal.Equals(value, "PRIMARY_OBJECTIVE_RESCUE_COMPLETE"));
        }

        public static bool HasReturnedToCurrentNode074(GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            if (expedition == null) return false;
            var clearedFlag = "ENCOUNTER_CLEARED_" + (expedition.CurrentNodeId ?? string.Empty);
            return (expedition.ObjectiveFlags ?? Array.Empty<string>()).Any(value =>
                       StringComparer.Ordinal.Equals(value, clearedFlag)) ||
                   state.HasPendingBattleReturn;
        }

        public static bool IsFirstStoryBoard074(string boardId)
        {
            return StringComparer.Ordinal.Equals(boardId, "BOARD_BELL_BENEATH_GATE") ||
                   StringComparer.Ordinal.Equals(boardId, "BOARD_BELL_BENEATH_GATE_069") ||
                   StringComparer.Ordinal.Equals(boardId, FirstBoardId074);
        }

        public static bool IsChapterTwoStoryBoard074(string boardId)
        {
            return StringComparer.Ordinal.Equals(boardId, ChapterTwoBoardId074);
        }

        public static bool IsReliefRoadBoard081(string boardId)
        {
            return StringComparer.Ordinal.Equals(
                boardId,
                SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                    .ReliefRoadBoardId081);
        }

        public static string ChapterTwoStoryIdentityName079(
            string boardId,
            string nodeId)
        {
            if (!IsChapterTwoStoryBoard074(boardId)) return string.Empty;
            return StringComparer.Ordinal.Equals(nodeId, "N13") ||
                   StringComparer.Ordinal.Equals(nodeId, "N14")
                ? "ORRA VALE"
                : "SELLA VEY";
        }

        public static string ChapterTwoStoryIdentityRole079(
            string boardId,
            string nodeId)
        {
            var identity = ChapterTwoStoryIdentityName079(boardId, nodeId);
            if (StringComparer.Ordinal.Equals(identity, "ORRA VALE"))
                return StringComparer.Ordinal.Equals(nodeId, "N14")
                    ? "SURVEY LEAD  •  RESCUED"
                    : "SURVEY LEAD  •  RESCUE OBJECTIVE";
            return string.IsNullOrWhiteSpace(identity)
                ? string.Empty
                : "WAYGLASS WITNESS  •  SURVEY APPRENTICE";
        }

        public static string ChapterTwoStoryIdentityDisplayText079(
            string boardId,
            string nodeId)
        {
            var identity = ChapterTwoStoryIdentityName079(boardId, nodeId);
            if (StringComparer.Ordinal.Equals(identity, "SELLA VEY"))
                return "SELLA VEY\nWAYGLASS\nWITNESS\nSURVEY\nAPPRENTICE";
            if (!StringComparer.Ordinal.Equals(identity, "ORRA VALE"))
                return string.Empty;
            return StringComparer.Ordinal.Equals(nodeId, "N14")
                ? "ORRA VALE\nSURVEY\nLEAD\nRESCUED"
                : "ORRA VALE\nSURVEY\nLEAD\nRESCUE\nOBJECTIVE";
        }

        public static string ChapterTwoStoryIdentityPortraitResource079(
            string boardId,
            string nodeId)
        {
            var identity = ChapterTwoStoryIdentityName079(boardId, nodeId);
            if (StringComparer.Ordinal.Equals(identity, "ORRA VALE"))
                return ChapterTwoOrraPortraitResource079;
            return string.IsNullOrWhiteSpace(identity)
                ? string.Empty
                : ChapterTwoSellaPortraitResource079;
        }

        public static string ChapterTwoCompanionSpeaker079(string companionBeat)
        {
            if (string.IsNullOrWhiteSpace(companionBeat)) return string.Empty;
            var separator = companionBeat.IndexOf('•');
            return (separator > 0 ? companionBeat.Substring(0, separator) : companionBeat)
                .Trim()
                .ToUpperInvariant();
        }

        public static bool ChapterTwoUsesRosterSpeakerIdentity079(string companionBeat)
        {
            var speaker = ChapterTwoCompanionSpeaker079(companionBeat);
            // The quote owns the identity card. Sella and Orra have dedicated
            // authored story portraits; every other named voice is resolved
            // through the live roster. This prevents a node's witness/objective
            // portrait from being presented as the person who is speaking.
            return !string.IsNullOrWhiteSpace(speaker) &&
                   !StringComparer.Ordinal.Equals(speaker, "SELLA") &&
                   !StringComparer.Ordinal.Equals(speaker, "ORRA");
        }

        public static bool RequiresChapterTwoStoryAnchor079(
            string boardId,
            string nodeId)
        {
            if (!IsChapterTwoStoryBoard074(boardId)) return false;
            return StringComparer.Ordinal.Equals(nodeId, "N00") ||
                   StringComparer.Ordinal.Equals(nodeId, "N13") ||
                   StringComparer.Ordinal.Equals(nodeId, "N14");
        }

        public static string ChapterTwoApproachDisplayLabel079(
            string authoredLabel,
            int approachIndex,
            string eventId = null)
        {
            if (StringComparer.Ordinal.Equals(eventId, "EVENT_ABANDONED_SURVEY_PACK") ||
                StringComparer.Ordinal.Equals(
                    eventId,
                    SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                        .SecondStorySafeDescentEventId080))
                return authoredLabel ?? string.Empty;
            var framing = approachIndex == 0 ? "PROTECT" : "PRESS";
            return framing + "  •  " + (authoredLabel ?? string.Empty);
        }

        public static string CarefulApproachAvailabilityLabel076(
            string authoredLabel,
            int pressure,
            bool hasFieldPartner)
        {
            if (hasFieldPartner &&
                SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                    .CanUseCarefulApproach076(pressure))
                return authoredLabel ?? string.Empty;
            var reason = hasFieldPartner
                ? "LOCKED  •  NEED 1 PRESSURE"
                : "LOCKED  •  NEED A FIELD PARTNER";
            var label = authoredLabel ?? string.Empty;
            // Once unavailable, the bonus/cost ledger is not actionable. Keep the
            // authored order line and replace the entire consequence line with one
            // explicit lock reason, preserving the 28 px floor at 1280x800.
            var firstBreak = label.IndexOf('\n');
            var order = (firstBreak >= 0 ? label.Substring(0, firstBreak) : label).Trim();
            return string.IsNullOrWhiteSpace(order)
                ? reason
                : order + "\n" + reason;
        }

        public static string RescueCeremonyHeading076(int pageIndex)
        {
            var page = Mathf.Clamp(pageIndex, 0, 1) + 1;
            return "THE LANTERN PATROL LIVES  •  PAGE " + page + "/2\n" +
                   "10 RESCUED  •  WAYGLASS RECOVERED  •  PERMANENT ROSTER 20";
        }

        public static string CompanionStoryBeat076(
            GuildCityPresentationState017D state,
            ExpeditionBoardNodeView074 current)
        {
            if (state?.Expedition == null)
            {
                var preparedBoardId = (state?.Contracts ??
                                       Array.Empty<GuildCityContractView017D>())
                    .FirstOrDefault(value => value != null && value.IsActive &&
                                             !value.IsCompleted && !value.IsFailed)?.BoardId;
                if (IsChapterTwoStoryBoard074(preparedBoardId))
                    return "SELLA  •  “The false marks begin beneath our own Hall. Follow the brass line and bring Orra's survey crew home.”";
                if (IsReliefRoadBoard081(preparedBoardId))
                    return "KIRI  •  “Medicine for the outer wards is on those wagons. Keep one road open and bring the convoy through.”";
                return "KIRI  •  “The bell is below us. Find Zorin, recover the Wayglass, and bring every Lantern home.”";
            }
            if (current == null)
                return "MAREN  •  “One clear order at a time. We keep moving together.”";
            if (IsChapterTwoStoryBoard074(state.Expedition.BoardId))
            {
                switch (current.NodeId)
                {
                    case "N02":
                        return "TALA  •  “These fresh Wayglass marks were painted after the survey crew passed. Someone wants us off their brass line.”";
                    case "N03":
                        return "ZORIN  •  “Sella's brass tag matches: the crew left three tacks in the mortar. That is a warning to people who know their route.”";
                    case "N04":
                        return "ORREN  •  “The bridge was measured twice, but Sella's brass line continues below it—toward the unrecorded door.”";
                    case "N05":
                        return "BESSA  •  “Survey twine, dry oil, half a ration. They made camp here, then left in a hurry.”";
                    case "N06":
                        return "MAREN  •  “Fog Stalkers between us and the crew. Hold the brass line and give them nowhere to hide.”";
                    case "N07":
                        return "JAZZI  •  “Bind the wounds now. Sella and Orra's testimony keeps the brass line true for the next room.”";
                    case "N08":
                        return "ODELIA  •  “That voice knows our names, but it does not know the order we met. It is copying memory, not living it.”";
                    case "N09":
                        return "DAEVEN  •  “The thing changing the marks is close. Hunt it now and keep the missing crew's brass line clear.”";
                    case "N10":
                        return "ORREN  •  “Sella's seventh notch agrees: the astrolabe points down because the road itself is moving. I can prove where.”";
                    case "N11":
                        return "TAZREN  •  “Their instruments are behind that false wall. Recover them and the crew keeps its names, its work, and its proof.”";
                    case "N12":
                        return "GARA  •  “The safer line costs us the instruments, not the people. I will hold the retreat open.”";
                    case "N13":
                        return "KIRI  •  “I hear the survey bell. Bring them out first. Whatever waits at the unrecorded door can face the Guild together.”";
                    case "N14":
                        return "MAREN  •  “We found the door and brought the crew home. Now Skyhome must decide what kind of Guild stands before it.”";
                    default:
                        return "MAREN  •  “Follow the survey crew's brass line toward the unrecorded door. Ignore every fresh mark that turns us aside.”";
                }
            }
            if (IsReliefRoadBoard081(state.Expedition.BoardId))
            {
                switch (current.NodeId)
                {
                    case "N00": return "KIRI  •  “The outer wards need this medicine before nightfall. Put the Guild pawn on the road.”";
                    case "N01": return "MAREN  •  “Broken roadworks split ahead. The Guild pawn follows the clearest road and keeps the wagons together.”";
                    case "N02": return "BESSA  •  “That axle is failing. One steady hand saves the medicine wagon here.”";
                    case "N03": return "TAZREN  •  “Fresh tracks leave the road and return behind us. The next ambush is already moving.”";
                    case "N04": return "ORREN  •  “The culvert is blocked, not collapsed. We can clear it before the convoy loses the light.”";
                    case "N05": return "BESSA  •  “Dry timber and gate iron. Take what repairs the wagons; the rest goes home to Skyhome.”";
                    case "N06": return "KIRI  •  “Gate Gnawers on both flanks. Hold the wagons inside our Union line and break the ambush.”";
                    case "N07": return "JAZZI  •  “Drivers, beasts, and Unions all need one quiet breath. We rest now so the medicine reaches people alive.”";
                    case "N08": return "TALA  •  “The stolen crate still carries the ward seal. Recover it before the raiders disappear into the works.”";
                    case "N09": return "DAEVEN  •  “Their nest is off the safe road. Destroy it for good, then bring the convoy through.”";
                    case "N10": return "MAREN  •  “The draft beasts are panicking. One clear voice keeps every wagon from turning into wreckage.”";
                    case "N11": return "TAZREN  •  “Their route marks expose the next strike. Take the intelligence and Skyhome can protect tomorrow's convoy.”";
                    case "N12": return "GARA  •  “This road is slower, but every wheel stays under us. I can hold the rear until the wagons clear.”";
                    case "N13": return "KIRI  •  “The outer wards are beyond that barricade. Form around the convoy and win the road.”";
                    case "N14": return "MAREN  •  “The medicine arrived and the road still belongs to Skyhome. Bring the Guild pawn home.”";
                    default: return "MAREN  •  “Keep the wagons together. The relief road only matters if the people at its end receive what they need.”";
                }
            }
            switch (current.NodeId)
            {
                case "N00":
                    return "KAEL  •  “I have the stair. Take your Unions below and cut the hand from the bell.”";
                case "N01":
                    return "KAEL  •  “The breach is inside our Hall. Break the first wave here, then follow the Lantern marks.”";
                case "N02":
                    return "JAZZI  •  “The runner can still speak, but only if we stop treating the ledger like it matters more than the person holding it.”";
                case "N03":
                    return "TAZREN  •  “Three lantern scuffs, one dragged heel, no blood. The patrol left us a trail on purpose.”";
                case "N04":
                    return "ORREN  •  “The rail is gone. We cross together before the old stone settles.”";
                case "N05":
                    return "BESSA  •  “Patrol cache is intact. Take what keeps people alive; leave enough that the road still serves Skyhome.”";
                case "N06":
                    return "TALA  •  “Lanterns ahead—but those shadows are moving against the wind.”";
                case "N07":
                    return "MAREN  •  “Camp is not waiting. It is where we mend the Union before the next order asks what we are made of.”";
                case "N08":
                    return "ODELIA  •  “The Wayglass pulse is bending the old chain. Ground it now before that hazard follows us into battle.”";
                case "N09":
                    return "DAEVEN  •  “The elite has the patrol's route ledger. High risk, real reward—break it and recover the proof.”";
                case "N10":
                    return "JAZZI  •  “Zorin will not leave while anyone is pinned. Stabilize the foreman and the whole patrol moves together.”";
                case "N11":
                    return "ORREN  •  “The hidden service line reaches the Gatehouse flank. Repair it and our Unions enter on better ground.”";
                case "N12":
                    return "GARA  •  “The safe road gives up the ledger, not the rescue. I can keep everyone together from here.”";
                case "N13":
                    return "MAREN  •  “Zorin is alive. Rally the patrol and lift the Wayglass clear; then our Unions face the Gate-Eater.”";
                case "N14":
                    return "KIRI  •  “Bring them home. Skyhome will hear their names before it hears our victory.”";
                default:
                    return "MAREN  •  “The patrol's trail is still warm. Keep the Unions together and follow the lantern marks.”";
            }
        }

        public static string StoryBattleName074(GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            if (expedition == null) return "UNION BATTLE";
            var encounterId = expedition.CurrentEncounterId ?? string.Empty;
            if (encounterId.IndexOf("HALL_BREACH", StringComparison.OrdinalIgnoreCase) >= 0)
                return "HALL BREACH";
            if (encounterId.IndexOf("LANTERN_ROAD_AMBUSH", StringComparison.OrdinalIgnoreCase) >= 0)
                return "LANTERN ROAD AMBUSH";
            if (encounterId.IndexOf("GATE_EATER", StringComparison.OrdinalIgnoreCase) >= 0)
                return "GATE-EATER";
            if (IsFirstStoryBoard074(expedition.BoardId))
            {
                switch (expedition.CurrentNodeId)
                {
                    case "N01": return "HALL BREACH";
                    case "N06": return "LANTERN ROAD AMBUSH";
                    case "N13": return "GATE-EATER";
                }
            }
            var presentation = SafeNode074(expedition.BoardId, expedition.CurrentNodeId);
            return string.IsNullOrWhiteSpace(presentation?.displayName)
                ? "UNION BATTLE"
                : presentation.displayName.ToUpperInvariant();
        }

        public static string BattleActionLabel074(
            GuildCityPresentationState017D state,
            bool enterBattle)
        {
            return (enterBattle ? "ENTER BATTLE  •  " : "COMMIT  •  ") + StoryBattleName074(state);
        }

        public static string StoryBackdropResource076(ExpeditionBoardView074 view)
        {
            var boardFallback = string.IsNullOrWhiteSpace(view?.MapResourcePath)
                ? ExpeditionRouteMapResource076
                : view.MapResourcePath;
            if (view == null) return boardFallback;
            var current = (view.Nodes ?? Array.Empty<ExpeditionBoardNodeView074>())
                .FirstOrDefault(value => value != null && value.IsCurrent);
            var nodeId = current?.NodeId ?? string.Empty;

            // The Relief Road's archived map is a node graph baked into a PNG.
            // Keep those authoritative node ids in the save; show the road,
            // supplies and camp behind the card adventure instead of the graph.
            if (StringComparer.Ordinal.Equals(view.BoardId, ChapterThreeBoardId165))
            {
                switch (nodeId)
                {
                    case "N00":
                    case "N14": return ExpeditionHallBackdropResource076;
                    case "N02":
                    case "N05": return LanternRoadCacheBackdropResource086;
                    case "N04": return BrokenSurveyBridgeBackdropResource086;
                    case "N07":
                    case "N10":
                    case "N11": return LostSurveyCampBackdropResource086;
                    case "N13": return ExpeditionGatehouseBackdropResource076;
                    default: return ExpeditionLanternRoadBackdropResource076;
                }
            }

            // Chapter 2's archival survey image has a retired N00-N14 graph baked
            // into its pixels. The authored save keys remain intact while the
            // player sees the place represented by each room instead of the ledger.
            if (IsChapterTwoStoryBoard074(view.BoardId))
            {
                switch (nodeId)
                {
                    case "N00":
                        return WayglassThresholdBackdropResource086;
                    case "N01":
                        return BrokenSurveyBridgeBackdropResource086;
                    case "N02":
                        return WayglassThresholdBackdropResource086;
                    case "N03":
                        return LostSurveyCampBackdropResource086;
                    case "N04":
                        return BrokenSurveyBridgeBackdropResource086;
                    case "N05":
                        return LostSurveyCampBackdropResource086;
                    case "N06":
                        return WayglassThresholdBackdropResource086;
                    case "N07":
                        return LostSurveyCampBackdropResource086;
                    case "N08":
                        return WayglassThresholdBackdropResource086;
                    case "N09":
                        return BrokenSurveyBridgeBackdropResource086;
                    case "N10":
                        return UnrecordedDoorBackdropResource086;
                    case "N11":
                        return LostSurveyCampBackdropResource086;
                    case "N12":
                        return BrokenSurveyBridgeBackdropResource086;
                    case "N13":
                        return UnrecordedDoorBackdropResource086;
                    case "N14":
                        return ExpeditionHallBackdropResource076;
                    default:
                        return WayglassThresholdBackdropResource086;
                }
            }

            if (!IsFirstStoryBoard074(view.BoardId)) return boardFallback;
            switch (nodeId)
            {
                case "N00":
                case "N01":
                    return ExpeditionHallBackdropResource076;
                case "N02":
                    return GuildUndercroftLedgerBackdropResource086;
                case "N03":
                case "N04":
                    return ExpeditionLanternRoadBackdropResource076;
                case "N05":
                    return LanternRoadCacheBackdropResource086;
                case "N06":
                    return ExpeditionLanternRoadBackdropResource076;
                case "N07":
                    return LostSurveyCampBackdropResource086;
                case "N08":
                    return WayglassThresholdBackdropResource086;
                case "N09":
                    return LostSurveyCampBackdropResource086;
                case "N10":
                    return WayglassThresholdBackdropResource086;
                case "N11":
                    return BrokenSurveyBridgeBackdropResource086;
                case "N12":
                    return UnrecordedDoorBackdropResource086;
                case "N13":
                    return ExpeditionGatehouseBackdropResource076;
                case "N14":
                    return ExpeditionHallBackdropResource076;
                default:
                    return boardFallback;
            }
        }

        public static string CurrentMomentSummary076(
            GuildCityPresentationState017D state,
            ExpeditionBoardNodeView074 current)
        {
            var expedition = state?.Expedition;
            var isLegacyFirstHourCamp = expedition != null && current != null &&
                                        IsFirstStoryBoard074(expedition.BoardId) &&
                                        StringComparer.Ordinal.Equals(current.NodeId, "N07") &&
                                        StringComparer.Ordinal.Equals(
                                            expedition.CurrentEventId,
                                            "EVENT_CAMP_ARGUMENT");
            if (isLegacyFirstHourCamp && !expedition.ResolutionComplete)
                return current.Summary;
            if (expedition != null && !expedition.ResolutionComplete &&
                !string.IsNullOrWhiteSpace(expedition.CurrentEventProblem))
                return expedition.CurrentEventProblem;
            if (expedition?.HasCommittedCheckAtCurrentNode == true)
            {
                var lead = RecruitName076(state, expedition.LastCheckActorRecruitId);
                var partner = RecruitName076(state, expedition.LastCheckAssistantRecruitId);
                var team = string.IsNullOrWhiteSpace(partner)
                    ? lead
                    : lead + " and " + partner;
                var aftermath079 = ChapterTwoCrewMechanics079.OutcomeAftermath079(expedition);
                if (!string.IsNullOrWhiteSpace(aftermath079))
                    return aftermath079 + "\n" + CheckOutcomeLabel076(expedition.LastCheckOutcome) +
                           "  •  " + team +
                           (expedition.CurrentEventUsesCommitted2d6
                                ? "  •  TOTAL " + expedition.LastCheckTotal + "."
                                : "  •  THE GUILD'S PEOPLE, EVIDENCE, AND TRUST SHAPED THIS RESULT.");
                var eventName = isLegacyFirstHourCamp ||
                                string.IsNullOrWhiteSpace(expedition.CurrentEventTitle)
                    ? current?.DisplayName ?? "the field decision"
                    : expedition.CurrentEventTitle;
                return CheckOutcomeLabel076(expedition.LastCheckOutcome) + "  •  " + team +
                       " handled “" + eventName + "” with " + expedition.LastCheckTotal +
                       " (" + expedition.LastCheckDieOne + " + " + expedition.LastCheckDieTwo +
                       (expedition.LastCheckModifier == 0
                           ? string.Empty
                           : expedition.LastCheckModifier > 0
                               ? " + " + expedition.LastCheckModifier
                               : " − " + Math.Abs(expedition.LastCheckModifier)) + ").";
            }
            if (expedition != null && current != null &&
                IsFirstStoryBoard074(expedition.BoardId) &&
                StringComparer.Ordinal.Equals(current.NodeId, "N13"))
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Failed"))
                    return "The Gate-Eater broke the line. Bring every surviving Union home; all battle growth is kept.";
                if (HasObjectiveFlag076(expedition.ObjectiveFlags, "ENCOUNTER_CLEARED_N13"))
                    return "The Gate-Eater is defeated. Zorin's patrol and the Wayglass are secure—follow the marked road home.";
                return NeedsFirstHourPatrolRescue074(state)
                    ? "Rally Zorin's trapped patrol and recover the Wayglass. Then confront the Gate-Eater."
                    : "The patrol and Wayglass are secure. Defeat the Gate-Eater in Battle 3 of 3 before it reaches Skyhome.";
            }
            return current?.Summary ?? "Hold the current route and await the Guildmaster's next order.";
        }

        public static string ProgressConsequence076(
            GuildCityPresentationState017D state,
            ExpeditionBoardNodeView074 current)
        {
            var expedition = state?.Expedition;
            if (expedition == null)
                return "MISSION STAKES  •  FIND ZORIN  •  RECOVER THE WAYGLASS  •  BRING EVERYONE HOME";
            if (expedition.HasCommittedCheckAtCurrentNode)
            {
                if (current != null && IsFirstStoryBoard074(expedition.BoardId) &&
                    StringComparer.Ordinal.Equals(current.NodeId, "N07"))
                    return "LAST RESULT  ✓  " + CheckOutcomeLabel076(expedition.LastCheckOutcome) +
                           "  •  UNIONS RESTED  •  ZORIN RESCUE ORDER AGREED";
                var consequence = string.IsNullOrWhiteSpace(expedition.CurrentEventConsequence)
                    ? "THE ROAD AHEAD CHANGED"
                    : PlayerFacingStoryConsequence079(
                        expedition.CurrentEventConsequence).ToUpperInvariant();
                return "LAST RESULT  ✓  " + CheckOutcomeLabel076(expedition.LastCheckOutcome) +
                       "  •  " + consequence;
            }
            if (IsFirstStoryBoard074(expedition.BoardId))
            {
                if (HasFirstHourPatrolRescueProof074(expedition.ObjectiveFlags))
                    return "LAST RESULT  ✓  LANTERN PATROL RALLIED  •  WAYGLASS RECOVERED";
                if (HasObjectiveFlag076(expedition.ObjectiveFlags, "ENCOUNTER_CLEARED_N06"))
                    return "LAST RESULT  ✓  LANTERN ROAD AMBUSH WON  •  ROAD TO ZORIN OPEN";
                if (HasObjectiveFlag076(expedition.ObjectiveFlags, "ENCOUNTER_CLEARED_N01"))
                    return "LAST RESULT  ✓  HALL BREACH WON  •  LANTERN ROAD OPEN";
                if (current != null && StringComparer.Ordinal.Equals(current.NodeId, "N00"))
                    return "MISSION STAKES  •  FIND ZORIN  •  RECOVER THE WAYGLASS  •  BRING EVERYONE HOME";
            }
            if (!expedition.ResolutionComplete &&
                !string.IsNullOrWhiteSpace(expedition.CurrentEventConsequence))
                return "WHAT THIS CHANGES  •  " +
                       PlayerFacingStoryConsequence079(
                           expedition.CurrentEventConsequence).ToUpperInvariant();
            return "CURRENT BEAT  •  " + PhaseTitle074(PhaseIndexForNode074(current?.NodeId)) +
                   "  •  COMPLETE THIS ORDER TO OPEN THE NEXT ROAD";
        }

        private static string RecruitName076(
            GuildCityPresentationState017D state,
            string recruitId)
        {
            if (string.IsNullOrWhiteSpace(recruitId)) return string.Empty;
            var assignment = (state?.Assignments ?? Array.Empty<GuildCityAssignmentView017D>())
                .FirstOrDefault(value => value != null &&
                                         StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            return string.IsNullOrWhiteSpace(assignment?.RecruitName)
                ? "A Guildmate"
                : assignment.RecruitName;
        }

        public static string CheckOutcomeLabel076(string outcome)
        {
            switch (outcome)
            {
                case "EXCEPTIONAL": return "EXCEPTIONAL SUCCESS";
                case "FULL_SUCCESS": return "FULL SUCCESS";
                case "SUCCESS_WITH_COST": return "SUCCESS WITH A COST";
                case "SETBACK": return "SETBACK — RECOVERABLE";
                case "SEVERE_SETBACK": return "SEVERE SETBACK — EVERYONE SURVIVES";
                default: return "RESULT COMMITTED";
            }
        }

        public static IReadOnlyList<string> EventApproachLabels076(string eventId)
        {
            switch (eventId)
            {
                case "EVENT_FOUND_APPRENTICE":
                    return Array.AsReadOnly(new[]
                    {
                        "LET THEM BREATHE, THEN LISTEN\nPARTNER +2  •  COST 1 PRESSURE",
                        "ASK FOR THE MISSING CREW NOW\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_INJURED_COURIER":
                    return Array.AsReadOnly(new[]
                    {
                        "STABILIZE UNA, THEN READ HER LEDGER\nPARTNER +2  •  COST 1 PRESSURE",
                        "TAKE UNA'S BEARING AND MOVE\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_COLLAPSED_HANDRAIL":
                    return Array.AsReadOnly(new[]
                    {
                        "ANCHOR QUIN'S RESCUE LINE\nPARTNER +2  •  COST 1 PRESSURE",
                        "LIFT QUIN BEFORE THE SPAN SETTLES\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_UNSTABLE_BELL_CHAIN":
                    return Array.AsReadOnly(new[]
                    {
                        "ODELIA GROUNDS THE WAYGLASS PULSE\nPARTNER +2  •  COST 1 PRESSURE",
                        "BREAK THE CHAIN AND CLEAR THE ROAD\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_TRAPPED_FOREMAN":
                    return Array.AsReadOnly(new[]
                    {
                        "STABILIZE PETRA, THEN LIFT\nPARTNER +2  •  COST 1 PRESSURE",
                        "RALLY ZORIN'S PATROL TO PULL PETRA CLEAR\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_GATEGLASS_PULSE":
                    return Array.AsReadOnly(new[]
                    {
                        "ORREN TRACES THE BLUE SERVICE LINE\nPARTNER +2  •  COST 1 PRESSURE",
                        "MARK GARA'S SAFE PASSAGE\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_GARA_WOUNDED_LANTERN_PASSAGE":
                    return Array.AsReadOnly(new[]
                    {
                        "PAIR CARRIERS WITH GARA\nEVERY WOUNDED LANTERN CROSSES  •  LEDGER WAITS",
                        "LET GARA HOLD THE WHEEL\nUNION CARRIES THE WOUNDED  •  LEDGER WAITS"
                    });
                case "EVENT_LANTERN_WATCH_CAMP":
                    return Array.AsReadOnly(new[]
                    {
                        "HEAR JAZZI AND BOTH UNIONS OUT\nPARTNER +2  •  COST 1 PRESSURE",
                        "ISSUE ZORIN'S RESCUE ORDER\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_CAMP_ARGUMENT":
                    return Array.AsReadOnly(new[]
                    {
                        "HEAR BOTH UNIONS OUT\nPARTNER +2  •  COST 1 PRESSURE",
                        "ISSUE ONE CLEAR ORDER\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_WRONG_ROUTE_MARKS":
                    return Array.AsReadOnly(new[]
                    {
                        "COMPARE BOTH MARK SYSTEMS\nPARTNER +2  •  COST 1 PRESSURE",
                        "FOLLOW THE ORIGINAL BRASS LINE\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_BROKEN_SURVEY_BRIDGE":
                    return Array.AsReadOnly(new[]
                    {
                        "RIG A SAFETY LINE\nPARTNER +2  •  COST 1 PRESSURE",
                        "CROSS BEFORE IT FAILS\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_FOG_ECHO":
                    return Array.AsReadOnly(new[]
                    {
                        "TEST THE VOICE WITH A MEMORY\nPARTNER +2  •  COST 1 PRESSURE",
                        "IGNORE IT AND PRESS ON\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_BROKEN_ASTROLABE":
                    return Array.AsReadOnly(new[]
                    {
                        "REBUILD THE NEEDLE\nPARTNER +2  •  COST 1 PRESSURE",
                        "TRACK WHAT MOVES BELOW\nLEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_ABANDONED_SURVEY_PACK":
                    return Array.AsReadOnly(new[]
                    {
                        "RECOVER ORRA'S FIELD BOOK SAFELY\nSELLA HOLDS THE LINE  •  PARTNER +2  •  COST 1 PRESSURE",
                        "TAKE ORRA'S FIELD BOOK NOW\nBEAT THE WATCHERS  •  LEAD +0  •  COST 0 PRESSURE"
                    });
                case "EVENT_GARA_SURVEY_CREW_DESCENT":
                    return Array.AsReadOnly(new[]
                    {
                        "LOWER SELLA WITH THE WOUNDED\nGARA HOLDS THE CRADLE  •  FIELD BOOK WAITS",
                        "GARA ANCHORS; UNION ESCORTS\nPEOPLE FIRST  •  FIELD BOOK WAITS"
                    });
                default:
                    return Array.AsReadOnly(new[]
                    {
                        CarefulApproachLabel076,
                        SwiftApproachLabel076
                    });
            }
        }

        public static string EventCommitActionLabel076(string eventId, int approachIndex)
        {
            if (StringComparer.Ordinal.Equals(eventId, "EVENT_FOUND_APPRENTICE"))
                return "COMMIT  •  HEAR SELLA";
            if (StringComparer.Ordinal.Equals(eventId, "EVENT_ABANDONED_SURVEY_PACK"))
                return "COMMIT  •  SECURE FIELD BOOK";
            var approaches = EventApproachLabels076(eventId);
            if (approaches.Count == 0) return "COMMIT THE ORDER";
            var selected = approaches[Mathf.Clamp(approachIndex, 0, approaches.Count - 1)] ??
                           string.Empty;
            var lineBreak = selected.IndexOf('\n');
            var order = (lineBreak >= 0 ? selected.Substring(0, lineBreak) : selected).Trim();
            return string.IsNullOrWhiteSpace(order)
                ? "COMMIT THE ORDER"
                : "COMMIT  •  " + order;
        }

        public static string FieldConditionCopy076(
            GuildCityExpeditionView017D expedition,
            string currentLocation)
        {
            if (expedition == null)
                return "UNIONS READY TO DEPLOY\nSAVE  ✓  AUTOSAVE BEGINS ON DEPARTURE";
            var location = string.IsNullOrWhiteSpace(currentLocation)
                ? "CURRENT POSITION"
                : currentLocation.Trim().ToUpperInvariant();
            return "SUPPLIES " + expedition.Supplies +
                   "  •  FATIGUE " + expedition.Fatigue +
                   "  •  THREAT " + expedition.Threat +
                   "  •  PRESSURE " + expedition.Urgency +
                   "\nSAVE  ✓  " + location + " AUTOSAVED";
        }

        public static string RouteDecisionSummary076(ExpeditionBoardNodeView074 destination)
        {
            if (destination == null) return "SELECT A DESTINATION TO PREVIEW WHAT HAPPENS NEXT.";
            return "ARRIVAL FORECAST  •  " + RouteCostForecast076(destination.RouteCostSummary) +
                   "\nNEXT  •  " + PlayerFacingRouteSummary079(destination.Summary) +
                   "\nSAVE  ✓  AUTOSAVE ON ARRIVAL";
        }

        public static string RouteDecisionSummary079(
            string boardId,
            ExpeditionBoardNodeView074 destination)
        {
            if (!IsChapterTwoStoryBoard074(boardId))
                return RouteDecisionSummary076(destination);
            if (destination == null)
                return "SELECT AN ORDER TO SEE WHO IT HELPS AND WHAT EVIDENCE IT PRESERVES.";

            return "ORDER CONSEQUENCE  •  " + RouteCostForecast076(destination.RouteCostSummary) +
                   "\n" + ChapterTwoConsequenceSubject079(destination.NodeId) + "  •  " +
                   PlayerFacingRouteSummary079(destination.Summary) +
                   "\nSAVE  ✓  AUTOSAVE ON ARRIVAL";
        }

        public static string ChapterTwoConsequenceSubject079(string destinationNodeId)
        {
            switch (destinationNodeId ?? string.Empty)
            {
                case "N02": return "SELLA'S WARNING";
                case "N04": return "ORRA'S BRASS LINE";
                case "N07": return "SELLA'S TESTIMONY";
                case "N08": return "ORRA'S VOICE";
                case "N11": return "ORRA'S FIELD BOOK";
                case "N12": return "SELLA AND THE SURVEY CREW";
                default: return "SELLA / ORRA EVIDENCE";
            }
        }

        public static string PlayerFacingRouteSummary079(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return string.Empty;
            const string implementationPhrase = "certified Union battle";
            var index = summary.IndexOf(
                implementationPhrase,
                StringComparison.OrdinalIgnoreCase);
            if (index < 0) return summary.Trim();
            return (summary.Substring(0, index) + "Union battle" +
                    summary.Substring(index + implementationPhrase.Length)).Trim();
        }

        public static string PlayerFacingStoryConsequence079(string consequence)
        {
            if (string.IsNullOrWhiteSpace(consequence)) return string.Empty;
            const string systemPhrase = "recoverable relationship memory";
            var index = consequence.IndexOf(systemPhrase, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return consequence.Trim();
            return (consequence.Substring(0, index) +
                    "the bond formed while hearing her warning" +
                    consequence.Substring(index + systemPhrase.Length)).Trim();
        }

        public static string RouteChoiceTitle079(
            string boardId,
            ExpeditionBoardNodeView074 node)
        {
            if (node == null) return "CHOOSE A ROUTE";
            if (!IsChapterTwoStoryBoard074(boardId))
                return (node.DisplayName ?? "CHOOSE A ROUTE").ToUpperInvariant();
            if (StringComparer.Ordinal.Equals(node.NodeId, "N02"))
                return "TEST SELLA'S WARNING";
            if (StringComparer.Ordinal.Equals(node.NodeId, "N04"))
                return "PROTECT ORRA'S BRASS LINE";
            return (node.DisplayName ?? "CHOOSE A ROUTE").ToUpperInvariant();
        }

        public static string RouteDecisionHeading079(
            string boardId,
            string currentNodeId,
            bool offersEliteChoice,
            int linkedRouteCount)
        {
            if (offersEliteChoice) return "FIGHT OR TAKE THE BYPASS";
            if (linkedRouteCount <= 1) return "STORY ROUTE  •  REQUIRED";
            if (!IsChapterTwoStoryBoard074(boardId)) return "CHOOSE A ROUTE";

            return StringComparer.Ordinal.Equals(currentNodeId, "N01")
                ? "CHOOSE WHO YOU BACK"
                : "CHOOSE THE GUILD'S PRIORITY";
        }

        public static string RouteChoiceSubtitle079(
            string boardId,
            ExpeditionBoardNodeView074 node)
        {
            if (node == null) return "ROUTE PREVIEW";
            if (IsChapterTwoStoryBoard074(boardId) &&
                StringComparer.Ordinal.Equals(node.NodeId, "N02"))
                return "VERIFY FALSE MARKS  •  RECOVER THE BRASS LINE";
            switch ((node.Kind ?? string.Empty).ToUpperInvariant())
            {
                case "OPTIONAL_ELITE": return "OPTIONAL ELITE  •  HIGH RISK";
                case "SKILL_CHECK": return "RESCUE ROUTE  •  FIELD DECISION";
                case "SECONDARY_OBJECTIVE": return "SECONDARY OBJECTIVE";
                case "SAFE_ROUTE": return "SAFER ROUTE";
                case "ENCOUNTER": return "UNION BATTLE";
                case "CAMP": return "CAMP  •  RECOVERY";
                case "RESOURCE": return "SUPPLY STOP";
                case "MAIN_OBJECTIVE": return "STORY OBJECTIVE";
                case "EXIT": return "ROAD HOME";
                default: return node.IsThreat ? "THREAT ROUTE" : "STORY ROUTE";
            }
        }

        public static string RouteCostForecast076(string routeCostSummary)
        {
            if (string.IsNullOrWhiteSpace(routeCostSummary))
                return "ROUTE EFFECT SHOWN ON ARRIVAL";
            var parts = routeCostSummary.Split(new[] { '•' }, StringSplitOptions.RemoveEmptyEntries);
            var forecast = new List<string>(3);
            foreach (var rawPart in parts)
            {
                var part = (rawPart ?? string.Empty).Trim();
                var separator = part.LastIndexOf(' ');
                if (separator <= 0 || separator >= part.Length - 1 ||
                    !int.TryParse(part.Substring(separator + 1), out var amount))
                    continue;
                var key = part.Substring(0, separator).Trim().ToUpperInvariant();
                switch (key)
                {
                    case "SUPPLIES":
                        forecast.Add("SUPPLIES " + SignedRouteCost076(amount, decreases: true));
                        break;
                    case "FATIGUE":
                        forecast.Add("FATIGUE " + SignedRouteCost076(amount, decreases: false));
                        break;
                    case "URGENCY":
                    case "PRESSURE":
                        forecast.Add("PRESSURE " + SignedRouteCost076(amount, decreases: true));
                        break;
                }
            }
            return forecast.Count == 0
                ? routeCostSummary.Trim().ToUpperInvariant()
                : string.Join("  •  ", forecast);
        }

        private static string SignedRouteCost076(int amount, bool decreases)
        {
            if (amount <= 0) return "±0";
            return (decreases ? "−" : "+") + amount;
        }

        private static bool HasObjectiveFlag076(
            IReadOnlyList<string> objectiveFlags,
            string expected)
        {
            return (objectiveFlags ?? Array.Empty<string>()).Any(value =>
                StringComparer.Ordinal.Equals(value, expected));
        }

        private static ExpeditionBoardActionKind074 ActionKind074(
            GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            if (expedition == null)
                return state?.HasActiveContract == true
                    ? ExpeditionBoardActionKind074.BeginExpedition
                    : ExpeditionBoardActionKind074.ChooseContract;
            if (state.HasPendingBattleReturn)
                return ExpeditionBoardActionKind074.AwaitBattleReturn;
            if (state.HasPendingEncounter)
                return ExpeditionBoardActionKind074.EnterBattle;
            if (expedition.CanFinalizeOperation)
                return ExpeditionBoardActionKind074.FinalizeOperation;
            if (NeedsFirstHourPatrolRescue074(state))
                return ExpeditionBoardActionKind074.ResolveObjective;
            if (!expedition.ResolutionComplete &&
                (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "CAMP") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SECONDARY_OBJECTIVE") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SAFE_ROUTE")))
                return ExpeditionBoardActionKind074.ResolveCheck;
            // Optional elites are a real player decision: challenge the threat for
            // its reward or take the authored bypass. Preserve both choices on the
            // field-command panel instead of letting CommitEncounter hide the road.
            if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                expedition.CanCommitEncounter &&
                expedition.CanMove &&
                (expedition.LinkedNodeIds?.Count ?? 0) > 0)
                return ExpeditionBoardActionKind074.ChooseRoute;
            if (expedition.CanCommitEncounter)
                return ExpeditionBoardActionKind074.CommitEncounter;
            if (expedition.CanMove &&
                (expedition.LinkedNodeIds?.Count ?? 0) > 0)
                return ExpeditionBoardActionKind074.ChooseRoute;
            return ExpeditionBoardActionKind074.Regroup;
        }

        private static string StoryObjective074(
            GuildCityPresentationState017D state,
            string currentLocation)
        {
            if (state?.Expedition == null)
            {
                var activeContract = (state?.Contracts ?? Array.Empty<GuildCityContractView017D>())
                    .FirstOrDefault(value => value != null && value.IsActive &&
                                             !value.IsCompleted && !value.IsFailed);
                if (activeContract != null)
                    return string.IsNullOrWhiteSpace(activeContract.PrimaryObjective)
                        ? "Place the Guild pawn and begin " +
                          (activeContract.DisplayName ?? "the prepared mission") + "."
                        : activeContract.PrimaryObjective;
                return "Choose a Guild contract, prepare the Unions, then return to this quest board.";
            }
            if (NeedsFirstHourPatrolRescue074(state))
                return "The Lantern Patrol is alive at the Old Gatehouse. Rally Zorin's survivors, recover the Wayglass, then stop the Gate-Eater.";
            if (IsChapterTwoStoryBoard074(state.Expedition.BoardId))
                return ChapterTwoStoryObjective074(state, currentLocation);
            if (IsReliefRoadBoard081(state.Expedition.BoardId))
                return ReliefRoadStoryObjective081(state, currentLocation);
            if (state.HasPendingEncounter)
                return "An enemy Union blocks " + currentLocation + ". Commit your Union orders and win the field.";
            if (state.Expedition.CanFinalizeOperation)
                return "The objective is secure. Bring every surviving Union home and commit the operation report.";
            if (StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                state.Expedition.CanCommitEncounter && state.Expedition.CanMove)
                return "An elite ambush blocks " + currentLocation +
                       ". Win the Union battle to claim its reward and reopen the road.";
            if (state.Expedition.CanCommitEncounter)
                return "Enemy contact confirmed at " + currentLocation + ". Review the risk, then commit the encounter.";
            if (!state.Expedition.ResolutionComplete &&
                StringComparer.Ordinal.Equals(
                    state.Expedition.CurrentNodeKind,
                    "SECONDARY_OBJECTIVE"))
                return "OPTIONAL  •  Recover the " + currentLocation +
                       " for extra evidence and rewards; the main rescue does not depend on it.";
            if (!state.Expedition.ResolutionComplete &&
                IsFirstStoryBoard074(state.Expedition.BoardId) &&
                StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeId, "N12"))
                return "Help Gara bring every wounded Lantern through the sealing passage, then carry her choice into Zorin's rescue.";
            if (!state.Expedition.ResolutionComplete)
                return "Finish the event at " + currentLocation +
                       ". Your best crew resolves it, then Move Forward unlocks.";
            return "Move forward one room and flip the next card toward the missing patrol.";
        }

        private static string ReliefRoadStoryObjective081(
            GuildCityPresentationState017D state,
            string currentLocation)
        {
            var expedition = state.Expedition;
            if (state.HasPendingEncounter)
                return "An enemy Union blocks " + currentLocation +
                       ". Protect the medicine wagons and win the road.";
            if (expedition.CanFinalizeOperation)
                return "The medicine convoy reached the outer wards. Bring the Guild pawn home and bank the operation reward.";
            if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                expedition.CanCommitEncounter && expedition.CanMove)
                return "A raider elite blocks the convoy. Win the Union battle to claim its reward and reopen the relief road.";
            if (expedition.CanCommitEncounter)
                return "Enemy contact confirmed at " + currentLocation +
                       ". Start the Union battle and win before a medicine wagon is lost.";
            if (!expedition.ResolutionComplete)
                return "Resolve the problem at " + currentLocation +
                       " so the medicine wagons can reach the outer wards.";
            return "Move the Guild pawn into the next face-down room. Keep the relief convoy together until the medicine reaches the outer wards.";
        }

        private static string ChapterTwoStoryObjective074(
            GuildCityPresentationState017D state,
            string currentLocation)
        {
            var expedition = state.Expedition;
            if (state.HasPendingEncounter)
                return "An enemy Union blocks " + currentLocation +
                       ". Commit your Union orders and keep the survey crew's route open.";
            if (expedition.CanFinalizeOperation)
                return "The survey line is secure. Extract the crew and carry their record of the unrecorded door home.";
            switch (expedition.CurrentNodeId)
            {
                case "N02":
                    return expedition.ResolutionComplete
                        ? "The marks are false. Follow the survey crew's original brass line toward the unrecorded door."
                        : "Inspect the fresh false Wayglass marks and recover the survey crew's original brass line.";
                case "N04":
                    return expedition.ResolutionComplete
                        ? "The bridge holds. Follow the survey crew's brass line toward the unrecorded door."
                        : "Test the twice-measured bridge, keep the survey crew's brass line in sight, and continue toward the unrecorded door.";
                case "N12":
                    return expedition.ResolutionComplete
                        ? "Gara's descent is secure. Follow Sella and the wounded survey hands toward Orra's bell."
                        : "Help Gara lower Sella and the wounded survey hands toward Orra's bell before the fog closes the descent.";
            }
            if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                expedition.CanCommitEncounter && expedition.CanMove)
                return "An elite ambush blocks " + currentLocation +
                       ". Win the Union battle to protect the survey crew's road.";
            if (expedition.CanCommitEncounter)
                return "Enemy contact confirmed at " + currentLocation +
                       ". Review the risk, then clear the survey crew's route.";
            if (!expedition.ResolutionComplete)
                return "Resolve the evidence at " + currentLocation +
                       " and recover the survey crew's route to the unrecorded door.";
            return "Move forward one room and flip the next card along the survey crew's brass line toward the unrecorded door.";
        }

        private static string PositionStatus074(
            GuildCityPresentationState017D state,
            string currentLocation)
        {
            if (state?.Expedition == null) return "ROUTE NOT YET COMMITTED";
            if (HasReturnedToCurrentNode074(state))
                return "BATTLE RETURN • POSITION HELD AT " + currentLocation.ToUpperInvariant();
            return "PARTY POSITION • " + currentLocation.ToUpperInvariant();
        }

        private static string BoardTitle074(string boardId)
        {
            if (IsFirstStoryBoard074(boardId)) return "THE BELL BENEATH SKYHOME";
            if (StringComparer.Ordinal.Equals(boardId, ChapterTwoBoardId074))
                return SecondDimension.Presentation.M1FlowPresenter.ChapterTwoArcTitle076;
            if (StringComparer.Ordinal.Equals(boardId, "BOARD_RELIEF_ROAD"))
                return "KEEP THE RELIEF ROAD OPEN";
            return "GUILD EXPEDITION";
        }

        private static string OperationTitle074(string boardId)
        {
            return StringComparer.Ordinal.Equals(boardId, ChapterTwoBoardId074)
                ? "OPERATION 2  •  " +
                  SecondDimension.Presentation.M1FlowPresenter.ChapterTwoOperationTitle076
                : string.Empty;
        }

        private static string RouteTitle074(
            string boardId,
            GuildCityExpeditionView017D expedition)
        {
            if (!StringComparer.Ordinal.Equals(boardId, ChapterTwoBoardId074))
                return string.Empty;
            var visited = expedition?.VisitedNodeIds ?? Array.Empty<string>();
            if (StringComparer.Ordinal.Equals(expedition?.CurrentNodeId, "N02"))
                return "ROUTE A  •  " +
                       SecondDimension.Presentation.M1FlowPresenter
                           .ChapterTwoFreshMarksRouteTitle076;
            if (StringComparer.Ordinal.Equals(expedition?.CurrentNodeId, "N04"))
                return "ROUTE B  •  " +
                       SecondDimension.Presentation.M1FlowPresenter
                           .ChapterTwoBrokenBridgeRouteTitle076;
            if (visited.Contains("N02", StringComparer.Ordinal))
                return "ROUTE A  •  " +
                       SecondDimension.Presentation.M1FlowPresenter
                           .ChapterTwoFreshMarksRouteTitle076;
            if (visited.Contains("N04", StringComparer.Ordinal))
                return "ROUTE B  •  " +
                       SecondDimension.Presentation.M1FlowPresenter
                           .ChapterTwoBrokenBridgeRouteTitle076;
            return "ROUTE  •  NOT YET CHOSEN";
        }

        private static GuildCityNodePresentation017E SafeNode074(string boardId, string nodeId)
        {
            try { return GuildCityOpeningExperienceRegistry017E.Node(boardId, nodeId); }
            catch (InvalidOperationException) { return null; }
        }

        private static string SafeMapPath074(string boardId)
        {
            try
            {
                var path = GuildCityOpeningExperienceRegistry017E.BoardMapPath(boardId);
                return string.IsNullOrWhiteSpace(path)
                    ? "SecondDimension/Art/Guided063/EXPEDITION_ROUTE_MAP_V63"
                    : path;
            }
            catch (InvalidOperationException)
            {
                return "SecondDimension/Art/Guided063/EXPEDITION_ROUTE_MAP_V63";
            }
        }

        private static string ShortName074(int index, string authoredName)
        {
            if (index < 0 || index >= FallbackNames074.Length) return "Unknown Route";
            var value = string.IsNullOrWhiteSpace(authoredName)
                ? FallbackNames074[index]
                : authoredName.Trim();
            if (value.Length <= 23) return value;
            return FallbackNames074[index];
        }

        private static string FallbackSummary074(string kind)
        {
            switch (kind)
            {
                case "CAMP": return "A safe place to recover and speak with the party.";
                case "ENCOUNTER": return "An enemy Union controls this part of the road.";
                case "OPTIONAL_ELITE": return "A dangerous enemy guards an optional reward.";
                case "MAIN_OBJECTIVE": return "The contract's central crisis waits here.";
                case "SECONDARY_OBJECTIVE": return "Optional civic work can improve the Guild's result.";
                case "EXIT": return "The secured road returns to Skyhome.";
                default: return "A known stop on the Guild's committed route.";
            }
        }

        private static string StateLabel074(
            bool current,
            bool available,
            bool visited,
            bool objective,
            bool camp,
            bool threat)
        {
            var progress = current ? "CURRENT" : available ? "AVAILABLE" : visited ? "VISITED" : "LOCKED";
            var identities = new List<string>();
            if (objective) identities.Add("OBJECTIVE");
            if (camp) identities.Add("CAMP");
            if (threat) identities.Add("THREAT");
            return identities.Count == 0
                ? progress
                : progress + " • " + string.Join(" • ", identities);
        }

        private static Vector2 Position074(int index)
        {
            switch (index)
            {
                case 0: return new Vector2(0.07f, 0.49f);
                case 1: return new Vector2(0.17f, 0.49f);
                case 2: return new Vector2(0.27f, 0.70f);
                case 3: return new Vector2(0.38f, 0.78f);
                case 4: return new Vector2(0.27f, 0.27f);
                case 5: return new Vector2(0.39f, 0.17f);
                case 6: return new Vector2(0.49f, 0.49f);
                case 7: return new Vector2(0.59f, 0.70f);
                case 8: return new Vector2(0.59f, 0.27f);
                case 9: return new Vector2(0.69f, 0.17f);
                case 10: return new Vector2(0.70f, 0.49f);
                case 11: return new Vector2(0.80f, 0.70f);
                case 12: return new Vector2(0.80f, 0.27f);
                case 13: return new Vector2(0.89f, 0.49f);
                case 14: return new Vector2(0.965f, 0.49f);
                default: return new Vector2(0.07f, 0.49f);
            }
        }

        private static Vector2 LabelOffset074(int index)
        {
            switch (index)
            {
                case 0: return new Vector2(6f, -62f);
                case 1: return new Vector2(0f, 61f);
                case 2: return new Vector2(-10f, -62f);
                case 3: return new Vector2(0f, -62f);
                case 4: return new Vector2(-8f, 61f);
                case 5: return new Vector2(0f, 61f);
                case 6: return new Vector2(0f, -62f);
                case 7: return new Vector2(0f, -62f);
                case 8: return new Vector2(-12f, 61f);
                case 9: return new Vector2(0f, 61f);
                case 10: return new Vector2(0f, -62f);
                case 11: return new Vector2(0f, -62f);
                case 12: return new Vector2(0f, 61f);
                case 13: return new Vector2(-8f, -62f);
                case 14: return new Vector2(-58f, 61f);
                default: return Vector2.zero;
            }
        }
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private const string ExpeditionIllustratedMapResource074 =
            ExpeditionBoardProjection074.ExpeditionRouteMapResource076;

        public const float ExpeditionContextAnchorMinY074 = 0.025f;
        public const float ExpeditionContextAnchorMaxY074 = 0.730f;
        public const float ExpeditionContextAnchorMinX074 = 0.670f;
        public const float ExpeditionContextAnchorMaxX074 = 0.988f;
        public const float ExpeditionContextMinimumScreenHeight074 = 800f;
        public const int ExpeditionMaximumDecisionCards074 = 3;
        public const string ExpeditionFieldLeadLabel078 = "FIELD LEAD";
        public const string ExpeditionFieldPartnerLabel078 = "FIELD PARTNER";
        public const string ExpeditionTopFitLabel078 = "TOP FIT";
        public const string ExpeditionCompanionVoiceLabel078 = "COMPANION VOICE";

        private static readonly Rect LanternPatrolHeroRect076 =
            new Rect(0.018f, 0.245f, 0.327f, 0.575f);
        private static readonly Rect LanternPatrolStoryRect076 =
            new Rect(0.365f, 0.610f, 0.617f, 0.210f);
        private static readonly Rect LanternPatrolCompanionRegion076 =
            new Rect(0.365f, 0.245f, 0.617f, 0.345f);
        private const float LanternPatrolCompanionGap076 = 0.012f;

        private const float ExpeditionContextSpacing074 = 6f;
        private const float ExpeditionContextVerticalPadding074 = 32f;
        private const float ExpeditionContextDecisionHeadingHeight078 = 44f;
        private const float ExpeditionContextHeadingHeight074 = 40f;
        private const float ExpeditionContextTypeHeight074 = 30f;
        private const float ExpeditionContextLocationHeight074 = 48f;
        private const float ExpeditionContextSummaryHeight074 = 68f;
        private const float ExpeditionContextRiskHeight074 = 82f;
        private const float ExpeditionContextNoticeHeight074 = 266f;
        private const float ExpeditionContextNoticeTitleHeight074 = 30f;
        private const float ExpeditionContextNoticeCopyHeight074 = 196f;
        // Two-line story promises need room at the 145% accessibility setting.
        private const float ExpeditionContextPrimaryHeight074 = RuntimeUi.PrimaryTouchPixels + 24f;
        private const float ExpeditionContextRouteHeadingHeight074 = 38f;
        private const float ExpeditionContextRouteRowHeight074 = 188f;
        private const float ExpeditionContextRouteCardHeight074 = 180f;
        // Route consequences deliberately keep the authored 28 px floor. The longest
        // story forecast wraps to eight lines in the narrow certification rail, so it
        // needs its own vertical room instead of relying on truncation or tiny type.
        private const float ExpeditionContextRouteSummaryHeight074 = 276f;
        private const float ExpeditionContextCheckHeadingHeight074 = 40f;
        private const float ExpeditionContextCheckLeadRowHeight074 = 188f;
        private const float ExpeditionContextCheckLeadCardHeight078 = 180f;
        private const float ExpeditionContextCheckApproachCardHeight078 = 176f;
        private const float ExpeditionContextCheckApproachStackHeight078 =
            ExpeditionContextCheckApproachCardHeight078 * 2f + ExpeditionContextSpacing074;
        private const float ExpeditionDecisionCardSpacing074 = 8f;
        private int _lanternPatrolCeremonyPage076;
        private bool _lanternPatrolCeremonyDismissed076;
        private bool _returnToExpeditionAfterUnionReview076;

        public static Rect LanternPatrolHeroRectForVerification076 =>
            LanternPatrolHeroRect076;

        public static Rect LanternPatrolStoryRectForVerification076 =>
            LanternPatrolStoryRect076;

        public static IReadOnlyList<Rect> LanternPatrolCompanionRectsForVerification076()
        {
            const int count = 4;
            var width = (LanternPatrolCompanionRegion076.width -
                         LanternPatrolCompanionGap076 * (count - 1)) / count;
            var rects = new Rect[count];
            for (var index = 0; index < count; index++)
                rects[index] = new Rect(
                    LanternPatrolCompanionRegion076.xMin +
                    index * (width + LanternPatrolCompanionGap076),
                    LanternPatrolCompanionRegion076.yMin,
                    width,
                    LanternPatrolCompanionRegion076.height);
            return Array.AsReadOnly(rects);
        }

        // Legacy helpers remain private and unreachable while the fixed operation
        // plan replaces the old graph. Keeping their types compiling lets the
        // presentation be removed independently from save projection compatibility.
        private static readonly string[][] ExpeditionRouteEdges074 =
        {
            new[] { "N00", "N01" },
            new[] { "N01", "N02" },
            new[] { "N02", "N03" }, new[] { "N03", "N04" },
            new[] { "N04", "N05" }, new[] { "N05", "N06" },
            new[] { "N06", "N07" }, new[] { "N07", "N08" },
            new[] { "N08", "N09" }, new[] { "N08", "N10" },
            new[] { "N09", "N10" },
            new[] { "N10", "N11" }, new[] { "N10", "N12" },
            new[] { "N11", "N13" }, new[] { "N12", "N13" },
            new[] { "N13", "N14" }
        };

        private string _selectedExpeditionDestination074;
        private const string ExpeditionEliteEncounterSelection074 = "__ELITE_ENCOUNTER__";

        private sealed class ExpeditionBoardLine074
        {
            public RectTransform Transform;
            public Vector2 From;
            public Vector2 To;
        }

        /// <summary>
        /// Worst-case preferred height for the fixed field-command panel. This is a
        /// presentation contract, not a second layout implementation: every value is
        /// the same constant used by the controls below. It keeps the densest check
        /// state inside the supported 1280x800 viewport.
        /// </summary>
        public static float ExpeditionContextRequiredHeightForVerification074(
            ExpeditionBoardActionKind074 actionKind)
        {
            // BuildStudioExpeditionAction076 owns one decision heading followed by
            // only the controls for the active action. Keep this verification budget
            // aligned with that live hierarchy rather than the retired ledger panel.
            var childCount = 1;
            var height = ExpeditionContextDecisionHeadingHeight078;
            switch (actionKind)
            {
                case ExpeditionBoardActionKind074.ResolveCheck:
                    childCount += 5;
                    height += ExpeditionContextCheckHeadingHeight074 * 2f +
                              ExpeditionContextCheckLeadRowHeight074 +
                              ExpeditionContextCheckApproachStackHeight078 +
                              ExpeditionContextPrimaryHeight074;
                    break;
                case ExpeditionBoardActionKind074.ChooseRoute:
                    childCount += 4;
                    height += ExpeditionContextRouteHeadingHeight074 +
                              ExpeditionContextRouteRowHeight074 +
                              ExpeditionContextRouteSummaryHeight074 +
                              ExpeditionContextPrimaryHeight074;
                    break;
                case ExpeditionBoardActionKind074.AwaitBattleReturn:
                case ExpeditionBoardActionKind074.Regroup:
                    childCount += 1;
                    height += ExpeditionContextNoticeHeight074;
                    break;
                default:
                    childCount += 2;
                    height += ExpeditionContextNoticeHeight074 +
                              ExpeditionContextPrimaryHeight074;
                    break;
            }

            return ExpeditionContextVerticalPadding074 + height +
                   Mathf.Max(0, childCount - 1) * ExpeditionContextSpacing074;
        }

        public static float ExpeditionContextAvailableHeightForVerification074(
            float screenWidth,
            float screenHeight)
        {
            return ExpeditionCanvasSizeForVerification074(screenWidth, screenHeight).y *
                   (ExpeditionContextAnchorMaxY074 - ExpeditionContextAnchorMinY074);
        }

        public static Vector2 ExpeditionCanvasSizeForVerification074(
            float screenWidth,
            float screenHeight)
        {
            var safeWidth = Mathf.Max(1f, screenWidth);
            var safeHeight = Mathf.Max(1f, screenHeight);
            var reference = RuntimeUi.ReferenceResolution;
            // Match the production CanvasScaler (ScaleWithScreenSize, Match=0.5).
            var logWidth = Mathf.Log(safeWidth / reference.x, 2f);
            var logHeight = Mathf.Log(safeHeight / reference.y, 2f);
            var scale = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, 0.5f));
            return new Vector2(safeWidth, safeHeight) / Mathf.Max(0.0001f, scale);
        }

        public static float ExpeditionDecisionAvailableWidthForVerification074(
            float screenWidth,
            float screenHeight)
        {
            return ExpeditionCanvasSizeForVerification074(screenWidth, screenHeight).x *
                   (ExpeditionContextAnchorMaxX074 - ExpeditionContextAnchorMinX074) - 32f;
        }

        public static float ExpeditionDecisionRequiredWidthForVerification074()
        {
            return ExpeditionMaximumDecisionCards074 * RuntimeUi.MinimumTouchPixels +
                   (ExpeditionMaximumDecisionCards074 - 1) * ExpeditionDecisionCardSpacing074;
        }

        public static float ExpeditionRouteSummaryHeightForVerification074()
        {
            return ExpeditionContextRouteSummaryHeight074;
        }

        /// <summary>
        /// Complete fixed-screen expedition presentation. GuildCityFlowPresenter
        /// can call this directly for the EXPEDITION tab.
        /// </summary>
        private void BuildExpeditionBoardExperience074(
            Transform body,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            if (coordinator == null || state == null || !state.IsAvailable)
            {
                if (body == null) return;
                AddMessagePanel(
                    body,
                    "EXPEDITION BOARD UNAVAILABLE",
                    state?.Error ?? "The Guild's expedition records could not be opened.",
                    RuntimeUi.Warning);
                return;
            }

            // Release 081 presents the authoritative mission as a compact board
            // game: one pawn, one illustrated moment, one understandable action,
            // visible dice, and an immediate reward. The retired ledger/graph
            // renderer remains below for save compatibility and diagnostic tests.
            var boardQuestBoardId081 = state.Expedition?.BoardId ??
                (state.Contracts ?? Array.Empty<GuildCityContractView017D>())
                .FirstOrDefault(value => value != null && value.IsActive &&
                                         !value.IsCompleted && !value.IsFailed)?.BoardId;
            if (SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                    .UsesBoardQuestRewards081(boardQuestBoardId081) ||
                (state.Expedition == null && !state.HasActiveContract))
            {
                BuildBoardQuestExperience081(body, coordinator, state);
                return;
            }

            // The authoritative save still owns every route node. The studio-facing
            // presentation turns those records into five story beats, one current
            // moment, and one immediate decision instead of exposing a route ledger.
            RuntimeUi.ClearChildren(_screenRoot);
            var root = RuntimeUi.AddPanel(
                _screenRoot,
                "Full Screen Expedition Board 074",
                new Color(0.008f, 0.014f, 0.022f, 1f));
            NormalizeExpeditionViewport076(root.rectTransform);
            _activePage = root.rectTransform;
            _activeContent = null;
            _activeScroll = null;

            var view = ExpeditionBoardProjection074.Build(state);
            NormalizeExpeditionDestination074(state.Expedition);
            var guidedFieldBrief076 =
                ShouldEnterGuidedFirstHourField076(coordinator);

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
                guidedFieldBrief076
                    ? "Expedition Return To Field 076"
                    : "Expedition Return To Hall 074",
                guidedFieldBrief076
                    ? "←\nRETURN TO FIELD"
                    : "←\nGUILD HALL",
                guidedFieldBrief076
                    ? (Action)(() => EnterOuterGateworks066(coordinator))
                    : () =>
                    {
                        _guildCityTab017D = "HALL";
                        BuildCurrentScreen();
                    },
                72f,
                RuntimeUi.ButtonNormal);
            SetAnchors074(
                back.GetComponent<RectTransform>(),
                new Vector2(0.012f, 0.865f),
                new Vector2(0.105f, 0.985f));
            var backLabel076 = back.GetComponentInChildren<Text>();
            ConfigureResponsiveText062(backLabel076, 24, 34);
            backLabel076.color = RuntimeUi.Text;
            backLabel076.horizontalOverflow = HorizontalWrapMode.Wrap;
            backLabel076.verticalOverflow = VerticalWrapMode.Truncate;
            backLabel076.rectTransform.offsetMin = new Vector2(10f, 8f);
            backLabel076.rectTransform.offsetMax = new Vector2(-10f, -8f);
            // Premium rails are appended after RuntimeUi creates the label. Keep the
            // navigation words above those decorative siblings at every resolution.
            backLabel076.transform.SetAsLastSibling();

            var story = AddExpeditionStoryRibbon074(root.transform, state, view);
            SetAnchors074(
                story.rectTransform,
                new Vector2(0.120f, 0.865f),
                new Vector2(0.988f, 0.985f));

            var phases = BuildStudioExpeditionPhaseTrack076(root.transform, state, view);
            SetAnchors074(
                phases,
                new Vector2(0.012f, 0.748f),
                new Vector2(0.988f, 0.850f));

            var moment = BuildStudioExpeditionMoment076(root.transform, state, view);
            SetAnchors074(
                moment,
                new Vector2(0.012f, 0.025f),
                new Vector2(0.655f, 0.730f));
            var context = BuildStudioExpeditionAction076(
                root.transform,
                coordinator,
                state,
                view);
            SetAnchors074(
                context,
                new Vector2(ExpeditionContextAnchorMinX074, ExpeditionContextAnchorMinY074),
                new Vector2(ExpeditionContextAnchorMaxX074, ExpeditionContextAnchorMaxY074));
            if (!SelectExpeditionDefaultAction076(context)) back.Select();
            FinalizeExpeditionViewport076(root.rectTransform);
            ScheduleExpeditionViewportFinalize076(root.rectTransform);
        }

        private static bool SelectExpeditionDefaultAction076(RectTransform context)
        {
            if (context == null) return false;
            var buttons = context.GetComponentsInChildren<Button>(true)
                .Where(value => value != null &&
                                value.gameObject.activeInHierarchy &&
                                value.interactable)
                .ToArray();
            var primary = buttons.FirstOrDefault(value =>
                value.gameObject.name.StartsWith(
                    "Expedition Primary Context Action 074",
                    StringComparison.Ordinal));
            var selected = primary ?? buttons.FirstOrDefault();
            if (selected == null) return false;
            selected.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selected.gameObject);
            return true;
        }

        private static void BuildStudioExpeditionBackdrop076(
            Transform parent,
            ExpeditionBoardView074 view)
        {
            var art = RuntimeUi.AddPanel(
                parent,
                "Expedition Illustrated Story Backdrop 076",
                new Color(0.035f, 0.050f, 0.060f, 1f));
            Stretch(art.rectTransform);
            art.raycastTarget = false;
            var primaryPath = ExpeditionBoardProjection074.StoryBackdropResource076(view);
            var reliefRoad165 = StringComparer.Ordinal.Equals(view?.BoardId,
                ExpeditionBoardProjection074.ChapterThreeBoardId165);
            var fixedStoryBoard076 = view != null &&
                                     (ExpeditionBoardProjection074.IsFirstStoryBoard074(view.BoardId) ||
                                      ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(view.BoardId));
            var fallbackPath = reliefRoad165
                ? ExpeditionBoardProjection074.ExpeditionLanternRoadBackdropResource076
                : fixedStoryBoard076
                ? ExpeditionBoardProjection074.ExpeditionRouteMapResource076
                : string.IsNullOrWhiteSpace(view?.MapResourcePath)
                    ? ExpeditionIllustratedMapResource074
                    : view.MapResourcePath;
            art.sprite = GuildCityOpeningExperienceRegistry017E.Sprite(primaryPath);
            if (art.sprite == null && !StringComparer.Ordinal.Equals(primaryPath, fallbackPath))
                art.sprite = GuildCityOpeningExperienceRegistry017E.Sprite(fallbackPath);
            if (art.sprite == null && !reliefRoad165 &&
                !StringComparer.Ordinal.Equals(fallbackPath, ExpeditionIllustratedMapResource074))
                art.sprite = GuildCityOpeningExperienceRegistry017E.Sprite(
                    ExpeditionIllustratedMapResource074);
            art.preserveAspect = false;
            if (art.sprite != null)
            {
                art.color = Color.white;
                var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = art.sprite.rect.width /
                                     Mathf.Max(1f, art.sprite.rect.height);
            }

            var shade = RuntimeUi.AddPanel(
                art.transform,
                "Expedition Illustrated Story Shade 076",
                new Color(0.004f, 0.010f, 0.018f, 0.54f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;
        }

        private static RectTransform BuildStudioExpeditionPhaseTrack076(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var track = RuntimeUi.AddPanel(
                parent,
                "Expedition Five Phase Plan 074",
                new Color(0.006f, 0.016f, 0.028f, 0.94f));
            M1PremiumUi.StylePanel(track, M1PremiumUi.Surface.WorldRibbon);
            var row = AddRow(track.transform, "Expedition Phase Cards 074", 7f, 72f);
            Stretch(row);
            row.offsetMin = new Vector2(12f, 8f);
            row.offsetMax = new Vector2(-12f, -8f);

            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent) ?? view.Nodes[0];
            var currentPhase = ExpeditionBoardProjection074.PhaseIndexForNode074(current.NodeId);
            for (var phaseIndex = 0;
                 phaseIndex < ExpeditionBoardProjection074.ChapterPhaseCount074;
                 phaseIndex++)
            {
                var isCurrent = state.Expedition != null && phaseIndex == currentPhase;
                var isNext = state.Expedition == null && phaseIndex == 0;
                var isComplete = state.Expedition != null && phaseIndex < currentPhase;
                var phase = RuntimeUi.AddPanel(
                    row,
                    "Expedition Route Phase Card " + phaseIndex + " 074",
                    Color.white);
                RuntimeUi.SetLayout(phase, preferredHeight: 66f, flexibleWidth: 1f);
                M1PremiumUi.StylePanel(
                    phase,
                    isCurrent || isNext
                        ? M1PremiumUi.Surface.Warning
                        : isComplete
                            ? M1PremiumUi.Surface.Positive
                            : M1PremiumUi.Surface.WorldGlass);
                var text = RuntimeUi.AddText(
                    phase.transform,
                    "Expedition Route Phase Title " + phaseIndex + " 074",
                    (isComplete ? "✓  " : isCurrent || isNext ? "◆  " : string.Empty) +
                    (phaseIndex + 1) + "  " +
                    ExpeditionBoardProjection074.PhaseTitle074(phaseIndex) + "\n" +
                    ExpeditionBoardProjection074.PhaseSubtitle074(phaseIndex),
                    18,
                    TextAnchor.MiddleCenter,
                    isCurrent || isNext
                        ? RuntimeUi.Warning
                        : isComplete
                            ? RuntimeUi.Positive
                            : RuntimeUi.MutedText,
                    FontStyle.Bold);
                Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(6f, 4f);
                text.rectTransform.offsetMax = new Vector2(-6f, -4f);
                ConfigureResponsiveText062(text, 12, 19);
                text.raycastTarget = false;
            }
            return track.rectTransform;
        }

        private RectTransform BuildStudioExpeditionMoment076(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var card = RuntimeUi.AddPanel(
                parent,
                "Expedition Journey Overview 074",
                new Color(0.006f, 0.016f, 0.028f, 0.91f));
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(
                card.transform,
                new RectOffset(28, 28, 22, 22),
                7f,
                TextAnchor.UpperLeft);

            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent) ?? view.Nodes[0];
            var phase = ExpeditionBoardProjection074.PhaseIndexForNode074(current.NodeId);
            var returned = ExpeditionBoardProjection074.HasReturnedToCurrentNode074(state);
            var eyebrow = AddResponsiveText062(
                card.transform,
                "Expedition Current Position Eyebrow 074",
                "CURRENT BEAT  •  " + ExpeditionBoardProjection074.PhaseTitle074(phase) +
                (returned ? "  •  GROUND HELD" : string.Empty),
                17,
                25,
                38f,
                returned ? RuntimeUi.Positive : RuntimeUi.Accent,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(eyebrow, 28, 32);
            var name = AddResponsiveText062(
                card.transform,
                "Expedition Current Position Name 074",
                current.DisplayName.ToUpperInvariant(),
                32,
                48,
                68f,
                RuntimeUi.Text,
                FontStyle.Bold);
            M1PremiumUi.ConfigureDisplayText(name);
            var summary = AddResponsiveText062(
                card.transform,
                "Expedition Current Position Summary 074",
                ExpeditionBoardProjection074.CurrentMomentSummary076(state, current),
                28,
                34,
                132f,
                RuntimeUi.Text,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(summary, 28, 34);
            summary.verticalOverflow = VerticalWrapMode.Truncate;

            var consequenceCopy = ExpeditionBoardProjection074.ProgressConsequence076(state, current);
            var consequence = AddResponsiveText062(
                card.transform,
                "Expedition Progress Consequence 076",
                consequenceCopy,
                28,
                32,
                112f,
                consequenceCopy.StartsWith("LAST RESULT", StringComparison.Ordinal)
                    ? RuntimeUi.Positive
                    : RuntimeUi.Warning,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(consequence, 28, 32);
            consequence.verticalOverflow = VerticalWrapMode.Truncate;

            var companion = RuntimeUi.AddPanel(
                card.transform,
                "Expedition Companion Story Beat 076",
                Color.white);
            RuntimeUi.SetLayout(companion, preferredHeight: 152f);
            M1PremiumUi.StylePanel(companion, M1PremiumUi.Surface.WorldRibbon);
            var companionBeat078 = ExpeditionBoardProjection074.CompanionStoryBeat076(state, current);
            var companionText = RuntimeUi.AddText(
                companion.transform,
                "Expedition Companion Story Beat Text 076",
                ExpeditionCompanionVoiceLabel078 + "  •  " +
                companionBeat078,
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Warning,
                FontStyle.Italic);
            Stretch(companionText.rectTransform);
            companionText.rectTransform.offsetMin = new Vector2(18f, 10f);
            companionText.rectTransform.offsetMax = new Vector2(-18f, -10f);
            ConfigureAuthoredCompactText076(companionText, 28, 30);
            companionText.verticalOverflow = VerticalWrapMode.Truncate;
            companionText.raycastTarget = false;
            if (ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                    state.Expedition?.BoardId) &&
                (ExpeditionBoardProjection074.RequiresChapterTwoStoryAnchor079(
                     state.Expedition?.BoardId,
                     current.NodeId) ||
                 !ExpeditionBoardProjection074.ChapterTwoUsesRosterSpeakerIdentity079(
                     companionBeat078)))
                AddChapterTwoStoryIdentity079(
                    companion.transform,
                    state.Expedition?.BoardId,
                    current.NodeId,
                    companionText);
            else
                AddExpeditionCompanionIdentityChip078(
                    companion.transform,
                    companionBeat078,
                    companionText);

            var expedition = state.Expedition;
            var chapterTwoEvidence079 = ChapterTwoCrewMechanics079.BuildEvidenceStatus079(state);
            var fieldConditionCopy079 = chapterTwoEvidence079.IsVisible
                ? chapterTwoEvidence079.CompactReadout
                : ExpeditionBoardProjection074.FieldConditionCopy076(
                    expedition,
                    current.DisplayName);
            var fieldCondition = AddResponsiveText062(
                card.transform,
                "Expedition Field Condition 076",
                fieldConditionCopy079,
                28,
                30,
                112f,
                chapterTwoEvidence079.IsVisible
                    ? RuntimeUi.Accent
                    : expedition != null && expedition.Threat > 4
                        ? RuntimeUi.Warning
                        : RuntimeUi.Positive,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(fieldCondition, 28, 30);
            fieldCondition.verticalOverflow = VerticalWrapMode.Truncate;
            return card.rectTransform;
        }

        private static void AddChapterTwoStoryIdentity079(
            Transform parent,
            string boardId,
            string nodeId,
            Text companionText)
        {
            if (parent == null || companionText == null) return;
            var identityName = ExpeditionBoardProjection074
                .ChapterTwoStoryIdentityName079(boardId, nodeId);
            var identityRole = ExpeditionBoardProjection074
                .ChapterTwoStoryIdentityRole079(boardId, nodeId);
            var identityDisplayText = ExpeditionBoardProjection074
                .ChapterTwoStoryIdentityDisplayText079(boardId, nodeId);
            var portraitResource = ExpeditionBoardProjection074
                .ChapterTwoStoryIdentityPortraitResource079(boardId, nodeId);
            if (string.IsNullOrWhiteSpace(identityName) ||
                string.IsNullOrWhiteSpace(identityRole) ||
                string.IsNullOrWhiteSpace(portraitResource))
                return;

            var identity = RuntimeUi.AddPanel(
                parent,
                "Chapter Two Story Identity " + identityName + " 079",
                new Color(0.010f, 0.024f, 0.040f, 0.98f));
            SetAnchors074(
                identity.rectTransform,
                new Vector2(0.012f, 0.015f),
                new Vector2(0.300f, 0.985f));
            M1PremiumUi.StylePanel(identity, M1PremiumUi.Surface.WorldGlass);
            identity.raycastTarget = false;

            var portraitObject = new GameObject(
                "Chapter Two Story Portrait " + identityName + " 079",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            portraitObject.transform.SetParent(identity.transform, false);
            var portrait = portraitObject.GetComponent<Image>();
            portrait.sprite = GuildCityOpeningExperienceRegistry017E.Sprite(portraitResource);
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetAnchors074(
                portrait.rectTransform,
                new Vector2(0.025f, 0.05f),
                new Vector2(0.350f, 0.95f));

            var identityText = RuntimeUi.AddText(
                identity.transform,
                "Chapter Two Story Identity Text " + identityName + " 079",
                identityDisplayText,
                20,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors074(
                identityText.rectTransform,
                new Vector2(0.380f, 0.02f),
                new Vector2(0.990f, 0.98f));
            ConfigureAuthoredCompactText076(identityText, 18, 20);
            identityText.horizontalOverflow = HorizontalWrapMode.Wrap;
            identityText.verticalOverflow = VerticalWrapMode.Truncate;
            identityText.lineSpacing = 0.78f;
            identityText.raycastTarget = false;

            companionText.rectTransform.anchorMin = new Vector2(0.315f, 0f);
            companionText.rectTransform.offsetMin = new Vector2(14f, 10f);
        }

        private void AddExpeditionCompanionIdentityChip078(
            Transform parent,
            string companionBeat,
            Text companionText)
        {
            if (parent == null || companionText == null || string.IsNullOrWhiteSpace(companionBeat))
                return;
            var separator = companionBeat.IndexOf('•');
            if (separator <= 0) return;
            var speaker = companionBeat.Substring(0, separator).Trim();
            if (string.IsNullOrWhiteSpace(speaker)) return;
            var recruit = (_coordinator?.State?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .FirstOrDefault(value => value != null &&
                    ((!string.IsNullOrWhiteSpace(value.DisplayName) &&
                      value.DisplayName.StartsWith(speaker, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(value.PortraitAuthorityId) &&
                      value.PortraitAuthorityId.IndexOf(
                          speaker,
                          StringComparison.OrdinalIgnoreCase) >= 0)));
            if (recruit == null) return;

            var chip = RuntimeUi.AddButton(
                parent,
                "Expedition Companion Identity Chip 078",
                speaker.ToUpperInvariant(),
                null,
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);
            chip.interactable = false;
            var chipRect = chip.GetComponent<RectTransform>();
            SetAnchors074(chipRect, new Vector2(0.012f, 0.10f), new Vector2(0.205f, 0.90f));
            AddPortraitToButton(chip, recruit, large: false, cropToFill: true);
            var chipLabel = chip.GetComponentInChildren<Text>();
            if (chipLabel != null)
            {
                ConfigureAuthoredCompactText076(chipLabel, 24, 28);
                chipLabel.verticalOverflow = VerticalWrapMode.Truncate;
            }
            companionText.rectTransform.offsetMin = new Vector2(340f, 10f);
        }

        private RectTransform BuildStudioExpeditionAction076(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var panelImage = RuntimeUi.AddPanel(
                parent,
                "Expedition Context Action Area 074",
                new Color(0.006f, 0.016f, 0.028f, 0.97f));
            M1PremiumUi.StylePanel(panelImage, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(
                panelImage.transform,
                new RectOffset(18, 18, 16, 16),
                ExpeditionContextSpacing074,
                TextAnchor.UpperLeft);
            var panel = panelImage.rectTransform;
            var linkedDestinations078 = state.Expedition?.LinkedNodeIds ?? Array.Empty<string>();
            var offersEliteChoice078 =
                StringComparer.Ordinal.Equals(
                    state.Expedition?.CurrentNodeKind,
                    "OPTIONAL_ELITE") &&
                state.Expedition.CanCommitEncounter;
            var singleRouteOrder078 = view.ActionKind == ExpeditionBoardActionKind074.ChooseRoute &&
                                      linkedDestinations078.Count == 1 &&
                                      !offersEliteChoice078;
            var decisionHeading078 = AddResponsiveText062(
                panel,
                "Expedition Field Command Heading 074",
                ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(view.BoardId)
                    ? ExpeditionBoardProjection074.ChapterTwoStorySceneHeading079
                    : ShouldEnterGuidedFirstHourField076(coordinator)
                    ? "MISSION BRIEF"
                    : singleRouteOrder078
                        ? "YOUR NEXT ORDER"
                        : "YOUR NEXT DECISION",
                19,
                28,
                ExpeditionContextDecisionHeadingHeight078,
                RuntimeUi.Accent,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(decisionHeading078, 28, 32);

            if (ShouldEnterGuidedFirstHourField076(coordinator))
            {
                AddExpeditionContextNotice074(
                    panel,
                    "FIELD POSITION SAVED",
                    "Your deployed companions are waiting at the single gold marker shown in this brief.",
                    RuntimeUi.Positive);
                AddExpeditionPrimaryAction074(
                    panel,
                    "RETURN TO FIELD",
                    () => EnterOuterGateworks066(coordinator),
                    RuntimeUi.Accent);
                return panel;
            }

            switch (view.ActionKind)
            {
                case ExpeditionBoardActionKind074.ChooseContract:
                    AddExpeditionContextNotice074(panel, "NO ACTIVE ORDER", "Choose the Guild's current story contract before deploying.", RuntimeUi.Warning);
                    AddExpeditionPrimaryAction074(panel, "OPEN CONTRACT BOARD", () =>
                    {
                        _guildCityTab017D = "CONTRACTS";
                        BuildCurrentScreen();
                    }, RuntimeUi.Accent);
                    break;
                case ExpeditionBoardActionKind074.BeginExpedition:
                    AddExpeditionContextNotice074(panel, "DESCEND BELOW SKYHOME", "The Hall stair is open. Your three prepared Unions deploy together.", RuntimeUi.Accent);
                    AddExpeditionPrimaryAction074(
                        panel,
                        "BEGIN THE RESCUE",
                        () => BeginFirstContractFromParty063(coordinator),
                        RuntimeUi.Accent);
                    break;
                case ExpeditionBoardActionKind074.ResolveObjective:
                    AddFirstHourRescueAction074(panel, coordinator);
                    break;
                case ExpeditionBoardActionKind074.ResolveCheck:
                    AddExpeditionCheckAction074(panel, coordinator, state);
                    break;
                case ExpeditionBoardActionKind074.CommitEncounter:
                    AddExpeditionContextNotice074(panel, "ENEMY CONTACT", "The enemy blocks the marked road. Commit the contact to open the Union battle.", RuntimeUi.Error);
                    AddExpeditionPrimaryAction074(
                        panel,
                        ExpeditionBoardProjection074.BattleActionLabel074(state, enterBattle: false),
                        () => RunExpeditionBoardCommand074(
                            () => coordinator.CommitGuildCityEncounter017D(state.Expedition.CurrentEncounterId),
                            "The encounter could not be committed."),
                        RuntimeUi.Warning);
                    break;
                case ExpeditionBoardActionKind074.EnterBattle:
                    AddExpeditionContextNotice074(panel, "UNIONS IN POSITION", "The party's location is saved. Win the battle to reopen this road.", RuntimeUi.Error);
                    AddExpeditionPrimaryAction074(
                        panel,
                        ExpeditionBoardProjection074.BattleActionLabel074(state, enterBattle: true),
                        () => EnterCommittedGuildCityBattle017D(coordinator),
                        RuntimeUi.Warning);
                    break;
                case ExpeditionBoardActionKind074.AwaitBattleReturn:
                    AddExpeditionContextNotice074(panel, "BATTLE STILL ACTIVE", "Finish the current battle and claim its result. The Guild returns here automatically.", RuntimeUi.Positive);
                    break;
                case ExpeditionBoardActionKind074.ChooseRoute:
                    AddExpeditionRouteChoiceAction074(panel, coordinator, state, view);
                    break;
                case ExpeditionBoardActionKind074.FinalizeOperation:
                    AddExpeditionContextNotice074(
                        panel,
                        ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                            state?.Expedition?.BoardId)
                            ? "THE SURVEYORS ARE COMING HOME"
                            : "EVERYONE IS COMING HOME",
                        ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(
                            state?.Expedition?.BoardId)
                            ? "Sella, Orra, and the survey crew are safe. Return their evidence to Skyhome's Wayglass table."
                            : "The patrol and Wayglass are secure. Return to the Hall for the chapter report.",
                        RuntimeUi.Positive);
                    AddExpeditionPrimaryAction074(
                        panel,
                        "RETURN TO SKYHOME",
                        () => FinalizeStoryOperation065(coordinator),
                        RuntimeUi.Positive);
                    break;
                default:
                    AddExpeditionContextNotice074(panel, "HOLD POSITION", "Complete the current story beat to open the next road.", RuntimeUi.MutedText);
                    break;
            }
            return panel;
        }

        private bool ShouldShowLanternPatrolCeremony076(
            GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            if (_lanternPatrolCeremonyDismissed076 || expedition == null ||
                !ExpeditionBoardProjection074.IsFirstStoryBoard074(expedition.BoardId) ||
                !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13")) return false;
            var rescued = ExpeditionBoardProjection074
                .HasFirstHourPatrolRescueProof074(expedition.ObjectiveFlags);
            return rescued && (_coordinator.State.Recruits?.Count ?? 0) >= 20;
        }

        private void BuildLanternPatrolCeremony076(
            Transform parent,
            GuildCityPresentationState017D state)
        {
            var veil = RuntimeUi.AddPanel(
                parent,
                "Lantern Patrol Rescue Ceremony 076",
                new Color(0.004f, 0.010f, 0.018f, 0.94f));
            Stretch(veil.rectTransform);
            M1PremiumUi.StylePage(veil, _highContrast);

            var recruits = _coordinator.State.Recruits ??
                           Array.Empty<M1RecruitLoadoutView>();
            // Signature materialization gives each campaign member a generated
            // RecruitId. PortraitAuthorityId carries the authored stable identity
            // used by roster art and ceremony order, so resolve through that
            // authority instead of assuming the generated save ID is SIGREC_*.
            var patrol = FirstHourRosterService071.PatrolStableRecruitIds
                .Select(stableRecruitId => new
                {
                    StableRecruitId = stableRecruitId,
                    Recruit = recruits.FirstOrDefault(value => value != null &&
                        (StringComparer.Ordinal.Equals(
                             value.PortraitAuthorityId,
                             stableRecruitId) ||
                         StringComparer.Ordinal.Equals(
                             value.RecruitId,
                             stableRecruitId)))
                })
                .Where(value => value.Recruit != null)
                .ToArray();
            var page = Mathf.Clamp(_lanternPatrolCeremonyPage076, 0, 1);
            var visible = patrol.Skip(page * 5).Take(5).ToArray();

            var heading = RuntimeUi.AddPanel(
                veil.transform,
                "Lantern Patrol Rescue Heading 076",
                new Color(0.006f, 0.016f, 0.030f, 0.98f));
            SetAnchors074(
                heading.rectTransform,
                new Vector2(0.018f, 0.84f),
                new Vector2(0.982f, 0.982f));
            M1PremiumUi.StylePanel(heading, M1PremiumUi.Surface.WorldRibbon);
            var headingText = RuntimeUi.AddText(
                heading.transform,
                "Lantern Patrol Rescue Heading Text 076",
                ExpeditionBoardProjection074.RescueCeremonyHeading076(page),
                32,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors074(
                headingText.rectTransform,
                new Vector2(0.025f, 0.08f),
                new Vector2(0.975f, 0.92f));
            ConfigureResponsiveText062(headingText, 19, 34);

            var story = RuntimeUi.AddPanel(
                veil.transform,
                "Lantern Patrol Rescue Story 076",
                new Color(0.040f, 0.032f, 0.012f, 0.98f));
            SetAnchors074(
                story.rectTransform,
                LanternPatrolStoryRect076.min,
                LanternPatrolStoryRect076.max);
            M1PremiumUi.StylePanel(story, M1PremiumUi.Surface.Warning);
            var storyText = RuntimeUi.AddText(
                story.transform,
                "Lantern Patrol Rescue Story Text 076",
                page == 0
                    ? "ZORIN  •  “We kept the Wayglass lit for Skyhome. If your charter still has room, the Lantern Patrol answers it together.”"
                    : ExpeditionBoardProjection074.RescueCeremonyRosterTruth076,
                24,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Italic);
            SetAnchors074(
                storyText.rectTransform,
                new Vector2(0.035f, 0.10f),
                new Vector2(0.965f, 0.90f));
            ConfigureResponsiveText062(storyText, 17, 26);

            var companionRects = LanternPatrolCompanionRectsForVerification076();
            for (var index = 0; index < visible.Length; index++)
            {
                var recruit = visible[index].Recruit;
                var card = RuntimeUi.AddButton(
                    veil.transform,
                    "Rescued Lantern Patrol Portrait " +
                    visible[index].StableRecruitId + " 076",
                    recruit.DisplayName + "\n" + recruit.ObservedClass,
                    null,
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.018f, 0.040f, 0.060f, 0.98f));
                card.interactable = false;
                var rect = index == 0
                    ? LanternPatrolHeroRect076
                    : companionRects[index - 1];
                SetAnchors074(
                    card.GetComponent<RectTransform>(),
                    rect.min,
                    rect.max);
                AddPortraitToButton(card, recruit, large: true, cropToFill: true);
                M1PremiumUi.AddClassCrest(card, recruit.ClassSymbol, recruit.ObservedClass);
                ConfigureResponsiveText062(
                    card.GetComponentInChildren<Text>(),
                    index == 0 ? 16 : 13,
                    index == 0 ? 25 : 21);
            }

            var next = RuntimeUi.AddPanel(
                veil.transform,
                "Lantern Patrol Rescue Next Objective 076",
                new Color(0.012f, 0.040f, 0.038f, 0.98f));
            SetAnchors074(
                next.rectTransform,
                new Vector2(0.018f, 0.035f),
                new Vector2(0.690f, 0.215f));
            M1PremiumUi.StylePanel(next, M1PremiumUi.Surface.Positive);
            var nextText = RuntimeUi.AddText(
                next.transform,
                "Lantern Patrol Rescue Next Objective Text 076",
                page == 0
                    ? "FIVE OF TEN  •  Named people, permanent roles, gear, growth paths, and a place in the Guild."
                    : "NEXT OBJECTIVE  •  Review the twenty-member Union plan. Then confront the Gate-Eater before it reaches Skyhome.",
                21,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors074(
                nextText.rectTransform,
                new Vector2(0.035f, 0.10f),
                new Vector2(0.965f, 0.90f));
            ConfigureResponsiveText062(nextText, 15, 23);

            var action = RuntimeUi.AddButton(
                veil.transform,
                "Lantern Patrol Rescue Continue 076",
                page == 0
                    ? "MEET THE REST OF THE PATROL"
                    : "REVIEW 20-MEMBER UNION PLAN",
                page == 0
                    ? (Action)(() =>
                    {
                        _lanternPatrolCeremonyPage076 = 1;
                        BuildCurrentScreen();
                    })
                    : OpenPostRescueUnionReview076,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            SetAnchors074(
                action.GetComponent<RectTransform>(),
                new Vector2(0.715f, 0.055f),
                new Vector2(0.982f, 0.195f));
            ConfigureResponsiveText062(action.GetComponentInChildren<Text>(), 17, 27);
            action.Select();
        }

        private void OpenPostRescueUnionReview076()
        {
            _lanternPatrolCeremonyDismissed076 = true;
            _returnToExpeditionAfterUnionReview076 = true;
            _guildCityTab017D = "EXPEDITION";
            _screen = M1Screen.UnionBuilder;
            BuildCurrentScreen();
        }

        private static Image AddExpeditionStoryRibbon074(
            Transform body,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var hasOperationHierarchy =
                !string.IsNullOrWhiteSpace(view.OperationTitle) ||
                !string.IsNullOrWhiteSpace(view.RouteTitle);
            var ribbon = RuntimeUi.AddPanel(body, "Persistent Expedition Story Card 074", Color.white);
            RuntimeUi.SetLayout(ribbon, preferredHeight: hasOperationHierarchy ? 166f : 132f);
            M1PremiumUi.StylePanel(ribbon, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(
                ribbon.transform,
                new RectOffset(24, 24, 10, 10),
                2f,
                TextAnchor.MiddleLeft);
            AddResponsiveText062(
                ribbon.transform,
                "Expedition Chapter Title 074",
                ExpeditionBoardProjection074.ChapterLabel074(view.BoardId) + "  •  " + view.BoardTitle,
                18,
                28,
                36f,
                RuntimeUi.Accent,
                FontStyle.Bold);
            if (hasOperationHierarchy)
            {
                AddResponsiveText062(
                    ribbon.transform,
                    "Expedition Operation Hierarchy 076",
                    view.OperationTitle + "  •  " + view.RouteTitle,
                    16,
                    23,
                    38f,
                    RuntimeUi.Positive,
                    FontStyle.Bold);
            }
            AddResponsiveText062(
                ribbon.transform,
                "Expedition Persistent Objective 074",
                "OBJECTIVE  •  " + view.StoryObjective,
                17,
                26,
                60f,
                RuntimeUi.Text,
                FontStyle.Bold);
            return ribbon;
        }

        private static RectTransform AddExpeditionResourceRibbon074(
            Transform body,
            GuildCityExpeditionView017D expedition)
        {
            var row = AddRow(body, "Expedition Resources And Pressure 074", 8f, 82f);
            AddExpeditionMetric074(row, "SUPPLIES", expedition == null ? "—" : expedition.Supplies.ToString(), RuntimeUi.Positive);
            AddExpeditionMetric074(row, "FATIGUE", expedition == null ? "—" : expedition.Fatigue.ToString(), RuntimeUi.Warning);
            AddExpeditionMetric074(row, "THREAT", expedition == null ? "—" : expedition.Threat.ToString(), RuntimeUi.Error);
            AddExpeditionMetric074(row, "URGENCY", expedition == null ? "—" : expedition.Urgency.ToString(), RuntimeUi.Accent);
            AddExpeditionMetric074(
                row,
                "ROUTE",
                expedition == null
                    ? "0 / 15"
                    : (expedition.VisitedNodeIds?.Count ?? 0) + " / 15",
                RuntimeUi.Text);
            return row;
        }

        private static void AddExpeditionMetric074(
            Transform parent,
            string label,
            string value,
            Color color)
        {
            var metric = RuntimeUi.AddPanel(parent, "Expedition Metric " + label + " 074", Color.white);
            RuntimeUi.SetLayout(metric, preferredHeight: 78f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(metric, M1PremiumUi.Surface.WorldGlass);
            var text = RuntimeUi.AddText(
                metric.transform,
                "Expedition Metric Copy " + label + " 074",
                label + "  " + value,
                25,
                TextAnchor.MiddleCenter,
                color,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8f, 4f);
            text.rectTransform.offsetMax = new Vector2(-8f, -4f);
            ConfigureResponsiveText062(text, 18, 25);
        }

        private static RectTransform BuildExpeditionJourneyOverview074(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var frame = RuntimeUi.AddPanel(parent, "Expedition Journey Overview 074", Color.white);
            var mapPath = ExpeditionBoardProjection074.IsFirstStoryBoard074(view.BoardId)
                ? ExpeditionIllustratedMapResource074
                : view.MapResourcePath;
            frame.sprite = GuildCityOpeningExperienceRegistry017E.Sprite(mapPath);
            frame.preserveAspect = false;
            frame.color = Color.white;

            var shade = RuntimeUi.AddPanel(
                frame.transform,
                "Expedition Journey Readability Shade 074",
                new Color(0.008f, 0.015f, 0.026f, 0.82f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent) ?? view.Nodes[0];
            var currentStep = ExpeditionBoardProjection074.StepNumberForNode074(current.NodeId);
            var currentPhase = ExpeditionBoardProjection074.PhaseIndexForNode074(current.NodeId);
            var returned = ExpeditionBoardProjection074.HasReturnedToCurrentNode074(state);

            var position = RuntimeUi.AddPanel(
                frame.transform,
                "Expedition Current Position Card 074",
                Color.white);
            SetAnchors074(position.rectTransform, new Vector2(0.025f, 0.455f), new Vector2(0.975f, 0.965f));
            M1PremiumUi.StylePanel(
                position,
                returned ? M1PremiumUi.Surface.Positive : M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(
                position.transform,
                new RectOffset(24, 24, 14, 14),
                4f,
                TextAnchor.UpperLeft);
            AddResponsiveText062(
                position.transform,
                "Expedition Current Position Eyebrow 074",
                "CURRENT POSITION  •  STEP " + currentStep + " OF " +
                ExpeditionBoardProjection074.NodeCount074,
                16,
                22,
                30f,
                returned ? RuntimeUi.Positive : RuntimeUi.Accent,
                FontStyle.Bold);
            AddResponsiveText062(
                position.transform,
                "Expedition Current Position Name 074",
                current.DisplayName.ToUpperInvariant(),
                27,
                42,
                54f,
                RuntimeUi.Text,
                FontStyle.Bold);
            AddResponsiveText062(
                position.transform,
                "Expedition Current Position Kind 074",
                PlayerFacingNodeKind074(current.Kind) + "  •  " + current.StateLabel,
                15,
                22,
                28f,
                ExpeditionNodeRingColor074(current),
                FontStyle.Bold);
            AddResponsiveText062(
                position.transform,
                "Expedition Current Position Summary 074",
                current.Summary,
                18,
                27,
                72f,
                RuntimeUi.Text);
            AddResponsiveText062(
                position.transform,
                "Expedition Current Position Risk 074",
                "RISK  " + current.Risk.ToUpperInvariant() + "  •  " +
                current.RouteCostSummary.ToUpperInvariant(),
                14,
                20,
                28f,
                current.IsThreat ? RuntimeUi.Error : RuntimeUi.MutedText,
                FontStyle.Bold);
            AddResponsiveText062(
                position.transform,
                "Expedition Position Status 074",
                view.PositionStatus,
                15,
                22,
                34f,
                returned ? RuntimeUi.Positive : RuntimeUi.Accent,
                FontStyle.Bold);
            AddResponsiveText062(
                position.transform,
                "Expedition Save Status 074",
                "AUTOSAVE READY  •  POSITION & OBJECTIVE SECURED",
                13,
                18,
                25f,
                RuntimeUi.MutedText,
                FontStyle.Bold);

            var plan = RuntimeUi.AddPanel(
                frame.transform,
                "Expedition Five Phase Plan 074",
                Color.white);
            SetAnchors074(plan.rectTransform, new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.425f));
            M1PremiumUi.StylePanel(plan, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(
                plan.transform,
                new RectOffset(18, 18, 12, 12),
                6f,
                TextAnchor.UpperLeft);
            AddResponsiveText062(
                plan.transform,
                "Expedition Operation Plan Heading 074",
                "OPERATION PLAN  •  FIVE CLEAR PHASES",
                17,
                24,
                32f,
                RuntimeUi.Accent,
                FontStyle.Bold);

            var phaseRow = AddRow(plan.transform, "Expedition Phase Cards 074", 7f, 150f);
            for (var phaseIndex = 0;
                 phaseIndex < ExpeditionBoardProjection074.ChapterPhaseCount074;
                 phaseIndex++)
            {
                var isCurrent = state.Expedition != null && phaseIndex == currentPhase;
                var isNext = state.Expedition == null && phaseIndex == 0;
                var isComplete = state.Expedition != null && phaseIndex < currentPhase;
                var phase = RuntimeUi.AddPanel(
                    phaseRow,
                    "Expedition Route Phase Card " + phaseIndex + " 074",
                    Color.white);
                RuntimeUi.SetLayout(phase, preferredHeight: 146f, flexibleWidth: 1f);
                M1PremiumUi.StylePanel(
                    phase,
                    isCurrent || isNext
                        ? M1PremiumUi.Surface.Warning
                        : isComplete
                            ? M1PremiumUi.Surface.Positive
                            : M1PremiumUi.Surface.WorldGlass);
                RuntimeUi.AddVerticalLayout(
                    phase.transform,
                    new RectOffset(8, 8, 7, 7),
                    2f,
                    TextAnchor.MiddleCenter);
                AddResponsiveText062(
                    phase.transform,
                    "Expedition Route Phase State " + phaseIndex + " 074",
                    isCurrent ? "CURRENT" : isNext ? "NEXT" : isComplete ? "COMPLETE" : "UPCOMING",
                    12,
                    17,
                    22f,
                    isCurrent || isNext ? RuntimeUi.Warning : isComplete ? RuntimeUi.Positive : RuntimeUi.MutedText,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                AddResponsiveText062(
                    phase.transform,
                    "Expedition Route Phase Title " + phaseIndex + " 074",
                    (phaseIndex + 1) + "  " + ExpeditionBoardProjection074.PhaseTitle074(phaseIndex),
                    20,
                    28,
                    48f,
                    RuntimeUi.Text,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                AddResponsiveText062(
                    phase.transform,
                    "Expedition Route Phase Subtitle " + phaseIndex + " 074",
                    ExpeditionBoardProjection074.PhaseSubtitle074(phaseIndex),
                    16,
                    21,
                    34f,
                    RuntimeUi.MutedText,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);
            }

            var visitedNames = view.Nodes
                .Where(value => value.IsVisited || value.IsCurrent)
                .OrderBy(value => ExpeditionBoardProjection074.StepNumberForNode074(value.NodeId))
                .Select(value => value.DisplayName.ToUpperInvariant())
                .ToArray();
            var recentNames = visitedNames
                .Skip(Mathf.Max(0, visitedNames.Length - 3))
                .ToArray();
            AddResponsiveText062(
                plan.transform,
                "Expedition Recent Route Breadcrumb 074",
                recentNames.Length == 0
                    ? "ROUTE LOG  •  DEPLOYMENT NOT YET COMMITTED"
                    : "RECENT ROUTE  •  " + string.Join("  ›  ", recentNames),
                14,
                20,
                42f,
                RuntimeUi.MutedText,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            return frame.rectTransform;
        }

        private RectTransform BuildExpeditionMap074(
            Transform parent,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var frame = RuntimeUi.AddPanel(parent, "Illustrated Expedition Route Board 074", Color.white);
            var mapPath = ExpeditionBoardProjection074.IsFirstStoryBoard074(view.BoardId)
                ? ExpeditionIllustratedMapResource074
                : view.MapResourcePath;
            frame.sprite = GuildCityOpeningExperienceRegistry017E.Sprite(mapPath);
            frame.preserveAspect = false;
            frame.color = Color.white;

            var shade = RuntimeUi.AddPanel(
                frame.transform,
                "Expedition Map Readability Shade 074",
                new Color(0.015f, 0.025f, 0.04f, 0.22f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            var overlay = new GameObject(
                "Expedition Board Overlay 074",
                typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.SetParent(frame.transform, false);
            Stretch(overlay);

            AddAnchoredMapHeading074(overlay, view);
            var byId = view.Nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var lines = new List<ExpeditionBoardLine074>();
            for (var index = 0; index < ExpeditionRouteEdges074.Length; index++)
            {
                var edge = ExpeditionRouteEdges074[index];
                if (!byId.TryGetValue(edge[0], out var from) ||
                    !byId.TryGetValue(edge[1], out var to)) continue;
                var lineColor = ExpeditionLineColor074(from, to);
                var line = RuntimeUi.AddPanel(
                    overlay,
                    "Route Connection " + edge[0] + " " + edge[1] + " 074",
                    lineColor);
                line.raycastTarget = false;
                line.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                line.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                lines.Add(new ExpeditionBoardLine074
                {
                    Transform = line.rectTransform,
                    From = from.NormalizedPosition,
                    To = to.NormalizedPosition
                });
            }

            foreach (var node in view.Nodes) AddExpeditionMapNode074(overlay, node);
            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent);
            RectTransform token = null;
            if (current != null) token = AddExpeditionPartyToken074(overlay, current);
            AddExpeditionLegend074(overlay);
            StartCoroutine(LayoutExpeditionBoardMap074(overlay, lines, token, current?.NormalizedPosition));
            return frame.rectTransform;
        }

        private static void AddAnchoredMapHeading074(
            RectTransform overlay,
            ExpeditionBoardView074 view)
        {
            var heading = RuntimeUi.AddPanel(
                overlay,
                "Expedition Map Heading 074",
                new Color(0.025f, 0.04f, 0.065f, 0.91f));
            SetAnchors074(heading.rectTransform, new Vector2(0.015f, 0.885f), new Vector2(0.985f, 0.985f));
            var text = RuntimeUi.AddText(
                heading.transform,
                "Expedition Map Heading Copy 074",
                view.BoardTitle + "  •  CHOOSE ROUTES, RESOLVE EVENTS, RETURN TO THE SAME POSITION",
                26,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(16f, 4f);
            text.rectTransform.offsetMax = new Vector2(-16f, -4f);
            ConfigureResponsiveText062(text, 18, 26);
        }

        private static void AddExpeditionMapNode074(
            RectTransform overlay,
            ExpeditionBoardNodeView074 node)
        {
            var ring = RuntimeUi.AddPanel(
                overlay,
                "Expedition Location " + node.NodeId + " 074",
                ExpeditionNodeRingColor074(node));
            ring.raycastTarget = false;
            ring.rectTransform.anchorMin = node.NormalizedPosition;
            ring.rectTransform.anchorMax = node.NormalizedPosition;
            ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ring.rectTransform.anchoredPosition = Vector2.zero;
            var size = node.IsCurrent ? 76f : node.IsAvailable ? 70f : 62f;
            ring.rectTransform.sizeDelta = new Vector2(size, size);

            var fill = RuntimeUi.AddPanel(
                ring.transform,
                "Expedition Location Fill " + node.NodeId + " 074",
                ExpeditionNodeFillColor074(node));
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(6f, 6f);
            fill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            fill.raycastTarget = false;

            var glyph = RuntimeUi.AddText(
                fill.transform,
                "Expedition Location Glyph " + node.NodeId + " 074",
                ExpeditionNodeGlyph074(node),
                node.IsThreat ? 32 : 27,
                TextAnchor.MiddleCenter,
                node.IsLocked ? RuntimeUi.MutedText : RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(glyph.rectTransform);
            glyph.raycastTarget = false;

            var tag = RuntimeUi.AddPanel(
                overlay,
                "Expedition Location Name " + node.NodeId + " 074",
                new Color(0.018f, 0.03f, 0.05f, node.IsLocked ? 0.72f : 0.94f));
            tag.raycastTarget = false;
            tag.rectTransform.anchorMin = node.NormalizedPosition;
            tag.rectTransform.anchorMax = node.NormalizedPosition;
            tag.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            tag.rectTransform.anchoredPosition = node.LabelOffset;
            tag.rectTransform.sizeDelta = new Vector2(202f, 56f);
            var label = RuntimeUi.AddText(
                tag.transform,
                "Expedition Human Location Label " + node.NodeId + " 074",
                node.DisplayName.ToUpperInvariant() + "\n" + node.StateLabel,
                22,
                TextAnchor.MiddleCenter,
                node.IsLocked ? new Color(0.69f, 0.73f, 0.79f, 1f) : RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(5f, 2f);
            label.rectTransform.offsetMax = new Vector2(-5f, -2f);
            ConfigureResponsiveText062(label, 14, 22);
            label.raycastTarget = false;
        }

        private static RectTransform AddExpeditionPartyToken074(
            RectTransform overlay,
            ExpeditionBoardNodeView074 current)
        {
            var token = RuntimeUi.AddPanel(
                overlay,
                "Visible Guild Party Token 074",
                new Color(0.05f, 0.78f, 0.96f, 1f));
            token.raycastTarget = false;
            token.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            token.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            token.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            token.rectTransform.sizeDelta = new Vector2(150f, 38f);
            var text = RuntimeUi.AddText(
                token.transform,
                "Guild Party Token Label 074",
                "GUILD PARTY",
                20,
                TextAnchor.MiddleCenter,
                Color.black,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            token.transform.SetAsLastSibling();
            return token.rectTransform;
        }

        private static void AddExpeditionLegend074(RectTransform overlay)
        {
            var legend = RuntimeUi.AddPanel(
                overlay,
                "Expedition Map Legend 074",
                new Color(0.02f, 0.035f, 0.055f, 0.92f));
            SetAnchors074(legend.rectTransform, new Vector2(0.015f, 0.015f), new Vector2(0.985f, 0.09f));
            var text = RuntimeUi.AddText(
                legend.transform,
                "Expedition Map Legend Copy 074",
                "CYAN  CURRENT   •   GOLD  AVAILABLE / OBJECTIVE   •   GREEN  VISITED / CAMP   •   RED  THREAT   •   DIM  LOCKED",
                20,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12f, 3f);
            text.rectTransform.offsetMax = new Vector2(-12f, -3f);
            ConfigureResponsiveText062(text, 15, 20);
        }

        private RectTransform BuildExpeditionContextActionArea074(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var panelImage = RuntimeUi.AddPanel(
                parent,
                "Expedition Context Action Area 074",
                new Color(0.012f, 0.024f, 0.040f, 0.97f));
            M1PremiumUi.StylePanel(panelImage, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(
                panelImage.transform,
                new RectOffset(16, 16, 12, 12),
                ExpeditionContextSpacing074,
                TextAnchor.UpperLeft);
            var panel = panelImage.rectTransform;
            AddResponsiveText062(
                panel,
                "Expedition Field Command Heading 074",
                "NEXT ORDER",
                18,
                27,
                ExpeditionContextHeadingHeight074,
                RuntimeUi.Accent,
                FontStyle.Bold);
            var current = view.Nodes.FirstOrDefault(value => value.IsCurrent) ?? view.Nodes[0];
            AddResponsiveText062(
                panel,
                "Expedition Current Location Type 074",
                PlayerFacingNodeKind074(current.Kind) + "  •  " +
                (current.IsCurrent ? "CURRENT" : current.IsAvailable ? "AVAILABLE" : "ROUTE"),
                17,
                23,
                ExpeditionContextTypeHeight074,
                ExpeditionNodeRingColor074(current),
                FontStyle.Bold);
            AddResponsiveText062(
                panel,
                "Expedition Current Location 074",
                current.DisplayName.ToUpperInvariant(),
                25,
                38,
                ExpeditionContextLocationHeight074,
                RuntimeUi.Text,
                FontStyle.Bold);
            AddResponsiveText062(
                panel,
                "Expedition Current Location Summary 074",
                current.Summary,
                18,
                27,
                ExpeditionContextSummaryHeight074,
                RuntimeUi.Text);
            AddResponsiveText062(
                panel,
                "Expedition Current Risk 074",
                "RISK  " + current.Risk.ToUpperInvariant() + "\n" +
                current.RouteCostSummary.ToUpperInvariant(),
                15,
                21,
                ExpeditionContextRiskHeight074,
                current.IsThreat ? RuntimeUi.Error : RuntimeUi.MutedText,
                FontStyle.Bold);

            switch (view.ActionKind)
            {
                case ExpeditionBoardActionKind074.ChooseContract:
                    AddExpeditionContextNotice074(panel, "NO ACTIVE ORDER", "Choose one story contract before deploying a Guild party.", RuntimeUi.Warning);
                    AddExpeditionPrimaryAction074(panel, "OPEN CONTRACT BOARD", () =>
                    {
                        _guildCityTab017D = "CONTRACTS";
                        BuildCurrentScreen();
                    }, RuntimeUi.Accent);
                    break;
                case ExpeditionBoardActionKind074.BeginExpedition:
                    AddExpeditionContextNotice074(panel, "UNIONS READY", "The route begins at Skyhome Gate. Supplies and party state commit when you deploy.", RuntimeUi.Accent);
                    AddExpeditionPrimaryAction074(
                        panel,
                        "BEGIN EXPEDITION",
                        () => BeginFirstContractFromParty063(coordinator),
                        RuntimeUi.Accent);
                    break;
                case ExpeditionBoardActionKind074.ResolveObjective:
                    AddFirstHourRescueAction074(panel, coordinator);
                    break;
                case ExpeditionBoardActionKind074.ResolveCheck:
                    AddExpeditionCheckAction074(panel, coordinator, state);
                    break;
                case ExpeditionBoardActionKind074.CommitEncounter:
                    AddExpeditionContextNotice074(panel, "ENEMY CONTACT", "Committing locks this contact and opens the separate Union battle.", RuntimeUi.Error);
                    AddExpeditionPrimaryAction074(
                        panel,
                        ExpeditionBoardProjection074.BattleActionLabel074(state, enterBattle: false),
                        () => RunExpeditionBoardCommand074(
                            () => coordinator.CommitGuildCityEncounter017D(state.Expedition.CurrentEncounterId),
                            "The encounter could not be committed."),
                        RuntimeUi.Warning);
                    break;
                case ExpeditionBoardActionKind074.EnterBattle:
                    AddExpeditionContextNotice074(panel, "UNION BATTLE READY", "Your map position is saved. The party returns to this exact location after battle rewards.", RuntimeUi.Error);
                    AddExpeditionPrimaryAction074(
                        panel,
                        ExpeditionBoardProjection074.BattleActionLabel074(state, enterBattle: true),
                        () => EnterCommittedGuildCityBattle017D(coordinator),
                        RuntimeUi.Warning);
                    break;
                case ExpeditionBoardActionKind074.AwaitBattleReturn:
                    AddExpeditionContextNotice074(panel, "POSITION HELD", "Finish the battle result, then the route board restores this exact location and its open roads.", RuntimeUi.Positive);
                    break;
                case ExpeditionBoardActionKind074.ChooseRoute:
                    AddExpeditionRouteChoiceAction074(panel, coordinator, state, view);
                    break;
                case ExpeditionBoardActionKind074.FinalizeOperation:
                    AddExpeditionContextNotice074(panel, "ROAD HOME OPEN", "The route outcome is complete. Return to the Guild Hall to commit rewards, recovery, and story progress.", RuntimeUi.Positive);
                    AddExpeditionPrimaryAction074(
                        panel,
                        "RETURN TO GUILD HALL",
                        () => FinalizeStoryOperation065(coordinator),
                        RuntimeUi.Positive);
                    break;
                default:
                    AddExpeditionContextNotice074(panel, "PARTY REGROUPING", "Complete the current story beat; the next route will unlock here.", RuntimeUi.MutedText);
                    break;
            }
            return panel;
        }

        private static void AddExpeditionContextNotice074(
            Transform parent,
            string title,
            string copy,
            Color color)
        {
            var notice = RuntimeUi.AddPanel(parent, "Expedition Context Notice " + title + " 074", Color.white);
            RuntimeUi.SetLayout(notice, preferredHeight: ExpeditionContextNoticeHeight074);
            M1PremiumUi.StylePanel(notice, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(notice.transform, new RectOffset(16, 16, 7, 7), 2f, TextAnchor.MiddleLeft);
            AddResponsiveText062(
                notice.transform,
                "Expedition Context Notice Title 074",
                title,
                18,
                24,
                ExpeditionContextNoticeTitleHeight074,
                color,
                FontStyle.Bold);
            AddResponsiveText062(
                notice.transform,
                "Expedition Context Notice Copy 074",
                copy,
                18,
                22,
                ExpeditionContextNoticeCopyHeight074,
                RuntimeUi.Text);
        }

        private static Button AddExpeditionPrimaryAction074(
            Transform parent,
            string label,
            Action action,
            Color color)
        {
            var button = RuntimeUi.AddButton(
                parent,
                "Expedition Primary Context Action 074",
                label,
                action,
                ExpeditionContextPrimaryHeight074,
                color);
            var buttonLabel = button.GetComponentInChildren<Text>();
            // Field commands can carry one authored consequence line. Preserve the
            // 20 px floor so 145% accessibility text may best-fit without clipping.
            ConfigureAuthoredCompactText076(buttonLabel, 20, 30);
            if (buttonLabel != null)
            {
                // Selected premium buttons normally assume a parchment fill, while
                // controller focus tints their target graphic blue. Keep this field
                // command CTA white-on-navy in every state so focus never makes it
                // look disabled.
                // Preserve a visibly muted pending command without dropping below AA
                // against the controller-focus blue (plain MutedText is only 4.19:1).
                buttonLabel.color = action == null
                    ? Color.Lerp(RuntimeUi.MutedText, RuntimeUi.Text, 0.20f)
                    : RuntimeUi.Text;
                buttonLabel.verticalOverflow = VerticalWrapMode.Truncate;
            }
            var colors = button.colors;
            colors.normalColor = RuntimeUi.ButtonNormal;
            colors.highlightedColor = RuntimeUi.ButtonHighlighted;
            colors.selectedColor = RuntimeUi.ButtonHighlighted;
            colors.pressedColor = RuntimeUi.ButtonPressed;
            button.colors = colors;
            return button;
        }

        private void AddExpeditionRouteChoiceAction074(
            Transform panel,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view)
        {
            var linked = state.Expedition.LinkedNodeIds ?? Array.Empty<string>();
            var offersEliteChoice =
                StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                state.Expedition.CanCommitEncounter;
            var selectedRouteStillAvailable078 = linked.Any(nodeId =>
                StringComparer.Ordinal.Equals(nodeId, _selectedExpeditionDestination074));
            if (!offersEliteChoice && !selectedRouteStillAvailable078 && linked.Count > 0 &&
                ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(view.BoardId))
                _selectedExpeditionDestination074 = linked[0];
            var isSingleRequiredRoute078 = linked.Count == 1 && !offersEliteChoice;
            var routeHeading078 = AddResponsiveText062(
                panel,
                "Expedition Route Choice Heading 074",
                ExpeditionBoardProjection074.RouteDecisionHeading079(
                    view.BoardId,
                    state.Expedition.CurrentNodeId,
                    offersEliteChoice,
                    linked.Count),
                28,
                32,
                ExpeditionContextRouteHeadingHeight074,
                RuntimeUi.Accent,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(routeHeading078, 28, 32);
            routeHeading078.verticalOverflow = VerticalWrapMode.Truncate;
            var row = AddRow(
                panel,
                "Expedition Route Choice Buttons 074",
                ExpeditionDecisionCardSpacing074,
                ExpeditionContextRouteRowHeight074);
            if (offersEliteChoice)
            {
                var selectedElite = StringComparer.Ordinal.Equals(
                    _selectedExpeditionDestination074,
                    ExpeditionEliteEncounterSelection074);
                var elite = RuntimeUi.AddButton(
                    row,
                    "Select Expedition Elite Encounter 074",
                    (selectedElite ? "◆  " : string.Empty) + "CHALLENGE THE ELITE\nOPTIONAL REWARD • HIGH RISK",
                    () =>
                    {
                        _selectedExpeditionDestination074 = ExpeditionEliteEncounterSelection074;
                        BuildCurrentScreen();
                    },
                    ExpeditionContextRouteCardHeight074,
                    selectedElite ? RuntimeUi.Warning : RuntimeUi.ButtonNormal);
                var eliteLabel078 = elite.GetComponentInChildren<Text>();
                if (eliteLabel078 != null)
                {
                    eliteLabel078.rectTransform.offsetMin = new Vector2(12f, 10f);
                    eliteLabel078.rectTransform.offsetMax = new Vector2(-12f, -10f);
                    ConfigureAuthoredCompactText076(eliteLabel078, 28, 32);
                    eliteLabel078.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }
            foreach (var nodeId in linked.Take(
                         offersEliteChoice
                             ? ExpeditionMaximumDecisionCards074 - 1
                             : ExpeditionMaximumDecisionCards074))
            {
                var captured = nodeId;
                var node = view.Nodes.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.NodeId, captured));
                if (node == null) continue;
                var selected = StringComparer.Ordinal.Equals(_selectedExpeditionDestination074, captured);
                var button = RuntimeUi.AddButton(
                    row,
                    "Select Expedition Destination " + captured + " 074",
                    (selected ? "◆  " : string.Empty) +
                    ExpeditionBoardProjection074.RouteChoiceTitle079(view.BoardId, node) +
                    "\n" + (isSingleRequiredRoute078
                        ? "STORY ROUTE  •  REQUIRED"
                        : ExpeditionBoardProjection074.RouteChoiceSubtitle079(
                            view.BoardId,
                            node)),
                    () =>
                    {
                        _selectedExpeditionDestination074 = captured;
                        BuildCurrentScreen();
                    },
                    ExpeditionContextRouteCardHeight074,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                var destinationLabel078 = button.GetComponentInChildren<Text>();
                if (destinationLabel078 != null)
                {
                    destinationLabel078.rectTransform.offsetMin = new Vector2(12f, 10f);
                    destinationLabel078.rectTransform.offsetMax = new Vector2(-12f, -10f);
                    ConfigureAuthoredCompactText076(destinationLabel078, 28, 32);
                    destinationLabel078.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }

            var selectedNode = view.Nodes.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.NodeId, _selectedExpeditionDestination074));
            var eliteSelected = offersEliteChoice && StringComparer.Ordinal.Equals(
                _selectedExpeditionDestination074,
                ExpeditionEliteEncounterSelection074);
            if (eliteSelected)
            {
                var eliteSummary = AddResponsiveText062(
                    panel,
                    "Selected Expedition Destination Summary 074",
                    "ELITE CHALLENGE  •  Commit this optional battle for its reward, or select the safe route instead.",
                    16,
                    23,
                    ExpeditionContextRouteSummaryHeight074,
                    RuntimeUi.Warning,
                    FontStyle.Bold);
                ConfigureAuthoredCompactText076(eliteSummary, 28, 32);
                eliteSummary.verticalOverflow = VerticalWrapMode.Truncate;
            }
            else if (selectedNode != null)
            {
                var routeSummary = AddResponsiveText062(
                    panel,
                    "Selected Expedition Destination Summary 074",
                    ExpeditionBoardProjection074.RouteDecisionSummary079(view.BoardId, selectedNode),
                    28,
                    32,
                    ExpeditionContextRouteSummaryHeight074,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                ConfigureAuthoredCompactText076(routeSummary, 28, 32);
                routeSummary.verticalOverflow = VerticalWrapMode.Truncate;
            }
            var travel = AddExpeditionPrimaryAction074(
                panel,
                eliteSelected ? "COMMIT TO ELITE ENCOUNTER" :
                selectedNode == null
                    ? offersEliteChoice ? "SELECT FIGHT OR BYPASS" : "SELECT A ROUTE"
                    : isSingleRequiredRoute078
                        ? "DEPART  •  " + selectedNode.DisplayName.ToUpperInvariant()
                        : ExpeditionBoardProjection074.IsChapterTwoStoryBoard074(view.BoardId)
                            ? "COMMIT ORDER  •  " +
                              ExpeditionBoardProjection074.RouteChoiceTitle079(view.BoardId, selectedNode)
                            : "TRAVEL TO " + ExpeditionBoardProjection074.RouteChoiceTitle079(
                                view.BoardId,
                                selectedNode),
                eliteSelected
                    ? () => RunExpeditionBoardCommand074(
                        () => coordinator.CommitGuildCityEncounter017D(state.Expedition.CurrentEncounterId),
                        "The elite encounter could not be committed.")
                    : selectedNode == null
                        ? (Action)null
                        : () => MoveExpeditionFromBoard074(coordinator, selectedNode.NodeId),
                eliteSelected ? RuntimeUi.Warning :
                selectedNode == null ? RuntimeUi.ButtonNormal : RuntimeUi.Accent);
            travel.interactable = eliteSelected || selectedNode != null;
        }

        private void AddExpeditionCheckAction074(
            Transform panel,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
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
            var eligibleSkills = state.Expedition?.CurrentEventEligibleSkills ??
                                 Array.Empty<string>();
            var participants = (state.Assignments ?? Array.Empty<GuildCityAssignmentView017D>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId) &&
                                (activeRecruitIds.Count == 0 || activeRecruitIds.Contains(value.RecruitId)))
                .OrderByDescending(value => recruitViews.TryGetValue(value.RecruitId, out var recruit)
                    ? ExpeditionLeadSuitabilityScore076(recruit.ObservedClass, eligibleSkills)
                    : 0)
                .ThenBy(value => value.RecruitName, StringComparer.Ordinal)
                .Take(3)
                .ToArray();
            if (participants.Length == 0)
            {
                AddExpeditionContextNotice074(panel, "NO FIELD LEAD", "Assign at least one adventurer to the deployment before resolving this event.", RuntimeUi.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_expeditionLeadRecruitId065) ||
                participants.All(value => !StringComparer.Ordinal.Equals(value.RecruitId, _expeditionLeadRecruitId065)))
                _expeditionLeadRecruitId065 = participants[0].RecruitId;

            var isChapterTwo079 = ChapterTwoCrewMechanics079.IsChapterTwoBoard079(
                state.Expedition?.BoardId);
            var crewCandidates079 = participants.Select(value =>
            {
                recruitViews.TryGetValue(value.RecruitId, out var recruit);
                return new ChapterTwoCrewCandidate079
                {
                    RecruitId = value.RecruitId,
                    DisplayName = value.RecruitName,
                    ObservedClass = recruit?.ObservedClass ?? string.Empty
                };
            }).ToArray();
            var leadCandidate079 = crewCandidates079.First(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, _expeditionLeadRecruitId065));
            var suggestedAssistantCandidate079 = isChapterTwo079
                ? ChapterTwoCrewMechanics079.BestAssistant079(
                    leadCandidate079,
                    crewCandidates079,
                    eligibleSkills)
                : null;
            var lead = participants.First(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, _expeditionLeadRecruitId065));
            var suggestedPartner079 = isChapterTwo079
                ? participants.FirstOrDefault(value =>
                    suggestedAssistantCandidate079 != null &&
                    StringComparer.Ordinal.Equals(
                        value.RecruitId,
                        suggestedAssistantCandidate079.RecruitId))
                : participants.FirstOrDefault(value =>
                    !StringComparer.Ordinal.Equals(value.RecruitId, lead.RecruitId));
            var carefulAvailable079 = suggestedPartner079 != null &&
                                      SecondDimension.Gameplay.GuildCity017D
                                          .GuildCityExpeditionService017D
                                          .CanUseCarefulApproach076(
                                              state.Expedition?.Urgency ?? 0);
            if (!carefulAvailable079 && _expeditionApproachIndex065 == 0)
                _expeditionApproachIndex065 = 1;

            var skillCopy = eligibleSkills.Count == 0
                ? string.Empty
                : "  •  FIT: " + string.Join(" / ", eligibleSkills).ToUpperInvariant();
            var leadHeading079 = AddResponsiveText062(
                panel,
                "Expedition Check Lead Heading 074",
                ExpeditionFieldLeadLabel078 + skillCopy,
                16,
                22,
                ExpeditionContextCheckHeadingHeight074,
                RuntimeUi.Accent,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(leadHeading079, 28, 32);
            leadHeading079.verticalOverflow = VerticalWrapMode.Truncate;
            var leads = AddRow(panel, "Expedition Check Lead Choices 074", 6f, ExpeditionContextCheckLeadRowHeight074);
            for (var participantIndex = 0; participantIndex < participants.Length; participantIndex++)
            {
                var participant = participants[participantIndex];
                var captured = participant;
                var selected = StringComparer.Ordinal.Equals(captured.RecruitId, _expeditionLeadRecruitId065);
                recruitViews.TryGetValue(captured.RecruitId, out var recruit);
                var tags079 = new List<string>();
                if (isChapterTwo079)
                    tags079.Add(ChapterTwoCrewMechanics079.RoleFitBonus079(
                        recruit?.ObservedClass,
                        eligibleSkills) > 0
                        ? "FIT +1"
                        : "FIT +0");
                if (participantIndex == 0) tags079.Add(ExpeditionTopFitLabel078);
                var tagLine079 = tags079.Count == 0
                    ? string.Empty
                    : "\n" + string.Join("  •  ", tags079);
                var button = RuntimeUi.AddButton(
                    leads,
                    "Expedition Check Lead " + captured.RecruitId + " 074",
                    (selected ? "◆ " : string.Empty) +
                    (captured.RecruitName ?? "ADVENTURER").ToUpperInvariant() +
                    "\n" + (recruit?.ObservedClass ?? "GUILDMATE").ToUpperInvariant() +
                    tagLine079,
                    () =>
                    {
                        _expeditionLeadRecruitId065 = captured.RecruitId;
                        BuildCurrentScreen();
                    },
                    ExpeditionContextCheckLeadCardHeight078,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                var leadLabel079 = button.GetComponentInChildren<Text>();
                if (leadLabel079 != null)
                {
                    leadLabel079.rectTransform.offsetMin = new Vector2(12f, 8f);
                    leadLabel079.rectTransform.offsetMax = new Vector2(-12f, -8f);
                    ConfigureAuthoredCompactText076(leadLabel079, 28, 32);
                    leadLabel079.verticalOverflow = VerticalWrapMode.Truncate;
                }
                if (selected) ConfigureExpeditionGoldButton076(button);
            }

            var approachHeading079 = AddResponsiveText062(
                panel,
                "Expedition Check Approach Heading 074",
                isChapterTwo079
                    ? ExpeditionBoardProjection074.ChapterTwoFieldOrderHeading079
                    : "CHOOSE THE ORDER",
                28,
                32,
                ExpeditionContextCheckHeadingHeight074,
                RuntimeUi.Accent,
                FontStyle.Bold);
            ConfigureAuthoredCompactText076(approachHeading079, 28, 32);
            approachHeading079.verticalOverflow = VerticalWrapMode.Truncate;
            // Authored approach names and consequences are decision-critical. Stack
            // the two cards at the 1280 floor so neither is forced into five narrow
            // lines; this keeps the full story consequence at the authored 28 px floor.
            var approaches = new GameObject(
                    "Expedition Check Approach Choices 074",
                    typeof(RectTransform),
                    typeof(LayoutElement))
                .GetComponent<RectTransform>();
            approaches.SetParent(panel, false);
            RuntimeUi.AddVerticalLayout(
                approaches,
                new RectOffset(0, 0, 0, 0),
                6f,
                TextAnchor.UpperCenter);
            RuntimeUi.SetLayout(
                approaches,
                preferredHeight: ExpeditionContextCheckApproachStackHeight078);
            var approachLabels = ExpeditionBoardProjection074.EventApproachLabels076(
                state.Expedition?.CurrentEventId);
            var carefulOrder079 = ExpeditionBoardProjection074
                .CarefulApproachAvailabilityLabel076(
                    approachLabels[0],
                    state.Expedition?.Urgency ?? 0,
                    suggestedPartner079 != null);
            var swiftOrder079 = approachLabels[1];
            if (isChapterTwo079)
            {
                carefulOrder079 = ExpeditionBoardProjection074
                    .ChapterTwoApproachDisplayLabel079(
                        carefulOrder079,
                        0,
                        state.Expedition?.CurrentEventId);
                swiftOrder079 = ExpeditionBoardProjection074
                    .ChapterTwoApproachDisplayLabel079(
                        swiftOrder079,
                        1,
                        state.Expedition?.CurrentEventId);
            }
            var carefulApproachLabel079 = suggestedPartner079 != null
                ? carefulOrder079 + "\n" + ExpeditionFieldPartnerLabel078 + "  •  " +
                  (suggestedPartner079.RecruitName ?? "GUILDMATE").ToUpperInvariant()
                : carefulOrder079;
            AddExpeditionApproachButton074(
                approaches,
                0,
                carefulApproachLabel079,
                carefulAvailable079);
            AddExpeditionApproachButton074(
                approaches,
                1,
                swiftOrder079,
                true);

            var assistant = _expeditionApproachIndex065 == 0
                ? suggestedPartner079
                : null;
            var selectedAssistantCandidate079 = _expeditionApproachIndex065 == 0
                ? suggestedAssistantCandidate079
                : null;
            var preview079 = isChapterTwo079
                ? ChapterTwoCrewMechanics079.BuildCheckPreview079(
                    leadCandidate079,
                    selectedAssistantCandidate079,
                    eligibleSkills,
                    _expeditionApproachIndex065,
                    state.CampaignModeId,
                    state.Expedition.CurrentEventUsesCommitted2d6,
                    ChapterTwoCrewMechanics079.BuildEvidenceStatus079(state))
                : null;
            var modifier = preview079 != null
                ? preview079.CommandModifier
                : _expeditionApproachIndex065 == 0 && assistant != null
                    ? SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                        .CarefulApproachModifier076
                    : SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                        .SwiftApproachModifier076;
            var commitLabel079 = ExpeditionBoardProjection074.EventCommitActionLabel076(
                state.Expedition?.CurrentEventId,
                _expeditionApproachIndex065) +
                (preview079 == null
                    ? string.Empty
                    : preview079.UsesCommitted2d6
                        ? "\nSUCCESS " + preview079.SuccessChancePercent + "%  •  FULL SUCCESS " +
                          preview079.FullSuccessChancePercent + "%"
                        : "\nPROMISED CONSEQUENCE  •  " +
                          preview079.GuaranteedOutcome.Replace('_', ' '));
            AddExpeditionPrimaryAction074(
                panel,
                commitLabel079,
                () => RunExpeditionBoardCommand074(
                    () => coordinator.ResolveGuildCityCheck017D(
                        string.IsNullOrWhiteSpace(state.Expedition.CurrentEventId)
                            ? "EVENT_COLLAPSED_HANDRAIL"
                            : state.Expedition.CurrentEventId,
                        lead.RecruitId,
                        assistant?.RecruitId ?? string.Empty,
                        modifier),
                    "The route check could not be resolved."),
                RuntimeUi.Warning);
        }

        public static int ExpeditionLeadSuitabilityScore076(
            string observedClass,
            IReadOnlyList<string> eligibleSkills)
        {
            var role = (observedClass ?? string.Empty).ToUpperInvariant();
            var score = 0;
            foreach (var skillValue in eligibleSkills ?? Array.Empty<string>())
            {
                var skill = (skillValue ?? string.Empty).ToUpperInvariant();
                if ((skill == "MEDICINE" || skill == "DIPLOMACY") &&
                    (role.Contains("PRIEST") || role.Contains("HEAL") || role.Contains("MAGE")))
                    score += 4;
                if ((skill == "PERCEPTION" || skill == "SURVIVAL" || skill == "STEALTH") &&
                    (role.Contains("RANGER") || role.Contains("ROGUE") || role.Contains("SCOUT")))
                    score += 4;
                if ((skill == "LORE" || skill == "ENGINEERING") &&
                    (role.Contains("MAGE") || role.Contains("GUARDIAN") || role.Contains("WARRIOR")))
                    score += 3;
                if ((skill == "COMMAND" || skill == "RESOLVE") &&
                    (role.Contains("GUARDIAN") || role.Contains("WARRIOR") || role.Contains("PRIEST")))
                    score += 3;
                if (skill == "ATHLETICS" &&
                    (role.Contains("WARRIOR") || role.Contains("GUARDIAN")))
                    score += 4;
            }
            return score;
        }

        private void AddExpeditionApproachButton074(
            Transform parent,
            int index,
            string label,
            bool interactable)
        {
            var selected = _expeditionApproachIndex065 == index;
            var button = RuntimeUi.AddButton(
                parent,
                "Expedition Check Approach " + index + " 074",
                (selected ? "◆ " : string.Empty) + label,
                () =>
                {
                    _expeditionApproachIndex065 = index;
                    BuildCurrentScreen();
                },
                ExpeditionContextCheckApproachCardHeight078,
                selected ? RuntimeUi.Warning : RuntimeUi.ButtonNormal);
            button.interactable = interactable;
            var buttonLabel076 = button.GetComponentInChildren<Text>();
            // These two cards carry the authored order, its pressure cost, and (for
            // the careful option) the actual field partner. Keep that bounded copy
            // inside the touch target without applying compact type to the page.
            if (buttonLabel076 != null)
            {
                buttonLabel076.rectTransform.offsetMin = new Vector2(8f, 8f);
                buttonLabel076.rectTransform.offsetMax = new Vector2(-8f, -8f);
                ConfigureAuthoredCompactText076(buttonLabel076, 28, 32);
                buttonLabel076.verticalOverflow = VerticalWrapMode.Truncate;
                if (!interactable) buttonLabel076.color = RuntimeUi.MutedText;
            }
            if (selected && interactable) ConfigureExpeditionGoldButton076(button);
        }

        private static void ConfigureExpeditionGoldButton076(Button button)
        {
            if (button == null) return;
            // Warning-role buttons normally use pale text and a blue controller
            // focus. Authored gold selections bind every focused state to a warm
            // surface and use high-contrast ink instead.
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.color = new Color(0.07f, 0.08f, 0.09f, 1f);
            var colors076 = button.colors;
            colors076.normalColor = RuntimeUi.Warning;
            colors076.highlightedColor = RuntimeUi.Accent;
            colors076.selectedColor = RuntimeUi.Warning;
            colors076.pressedColor = RuntimeUi.Accent;
            button.colors = colors076;
        }

        private void AddFirstHourRescueAction074(
            Transform panel,
            IGuildCityPresentationCoordinator017D coordinator)
        {
            AddExpeditionContextNotice074(
                panel,
                ExpeditionBoardProjection074.PatrolFoundDecisionTitle076,
                ExpeditionBoardProjection074.PatrolFoundDecisionCopy076,
                RuntimeUi.Warning);
            var rescue = coordinator as IFirstHourPatrolRescueCoordinator071;
            var action = AddExpeditionPrimaryAction074(
                panel,
                rescue == null ? "RESCUE AUTHORITY UNAVAILABLE" : "RALLY THE LANTERN PATROL",
                rescue == null
                    ? (Action)null
                    : () => RunExpeditionBoardCommand074(
                        rescue.RescueFirstHourLanternPatrol071,
                        "The patrol rescue could not be committed."),
                rescue == null ? RuntimeUi.ButtonNormal : RuntimeUi.Warning);
            action.interactable = rescue != null;
        }

        private void MoveExpeditionFromBoard074(
            IGuildCityPresentationCoordinator017D coordinator,
            string destinationNodeId)
        {
            var result = coordinator.MoveGuildCityExpedition017D(destinationNodeId);
            _localStatus = result?.Message ?? "The party could not reach that destination.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive) _selectedExpeditionDestination074 = null;
            BuildCurrentScreen();
        }

        private void RunExpeditionBoardCommand074(
            Func<M1CommandResult> command,
            string fallback)
        {
            var result = command == null ? null : command();
            _localStatus = result?.Message ?? fallback;
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive) _selectedExpeditionDestination074 = null;
            BuildCurrentScreen();
        }

        private void NormalizeExpeditionDestination074(GuildCityExpeditionView017D expedition)
        {
            var linked = expedition?.LinkedNodeIds ?? Array.Empty<string>();
            if (expedition != null &&
                StringComparer.Ordinal.Equals(
                    _selectedExpeditionDestination074,
                    ExpeditionEliteEncounterSelection074) &&
                StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                expedition.CanCommitEncounter)
                return;
            if (linked.Count == 1)
                _selectedExpeditionDestination074 = linked[0];
            else if (string.IsNullOrWhiteSpace(_selectedExpeditionDestination074) ||
                     !linked.Contains(_selectedExpeditionDestination074))
                _selectedExpeditionDestination074 = null;
        }

        private static IEnumerator LayoutExpeditionBoardMap074(
            RectTransform overlay,
            IReadOnlyList<ExpeditionBoardLine074> lines,
            RectTransform token,
            Vector2? currentPosition)
        {
            yield return null;
            if (overlay == null) yield break;
            Canvas.ForceUpdateCanvases();
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (line?.Transform == null) continue;
                var start = MapPixelPosition074(overlay, line.From);
                var end = MapPixelPosition074(overlay, line.To);
                var delta = end - start;
                line.Transform.anchoredPosition = (start + end) * 0.5f;
                line.Transform.sizeDelta = new Vector2(delta.magnitude, 7f);
                line.Transform.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
            if (token != null && currentPosition.HasValue)
                token.anchoredPosition = MapPixelPosition074(overlay, currentPosition.Value) + new Vector2(0f, 52f);
        }

        private static Vector2 MapPixelPosition074(RectTransform overlay, Vector2 normalized)
        {
            return new Vector2(
                (normalized.x - 0.5f) * overlay.rect.width,
                (normalized.y - 0.5f) * overlay.rect.height);
        }

        private static void NormalizeExpeditionViewport076(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void FinalizeExpeditionViewport076(RectTransform rect)
        {
            if (rect == null) return;
            // Battle return, window-size restoration, and SafeAreaFitter can all
            // settle in the same frame. Reassert a zero-offset root and resolve the
            // complete fixed layout before input or evidence capture sees it.
            NormalizeExpeditionViewport076(rect);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
        }

        private void ScheduleExpeditionViewportFinalize076(RectTransform rect)
        {
            if (!isActiveAndEnabled || rect == null) return;
            StartCoroutine(FinalizeExpeditionViewportNextFrame076(rect));
        }

        private static IEnumerator FinalizeExpeditionViewportNextFrame076(RectTransform rect)
        {
            yield return null;
            if (rect == null) yield break;
            FinalizeExpeditionViewport076(rect);
        }

        private static void SetAnchors074(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color ExpeditionLineColor074(
            ExpeditionBoardNodeView074 from,
            ExpeditionBoardNodeView074 to)
        {
            if ((from.IsCurrent && to.IsAvailable) || (to.IsCurrent && from.IsAvailable))
                return new Color(0.95f, 0.71f, 0.20f, 0.94f);
            if ((from.IsVisited || from.IsCurrent) && (to.IsVisited || to.IsCurrent))
                return new Color(0.31f, 0.78f, 0.49f, 0.78f);
            return new Color(0.54f, 0.60f, 0.68f, 0.42f);
        }

        private static Color ExpeditionNodeRingColor074(ExpeditionBoardNodeView074 node)
        {
            if (node.IsCurrent) return new Color(0.13f, 0.83f, 0.96f, 1f);
            if (node.IsThreat) return new Color(0.95f, 0.34f, 0.30f, 1f);
            if (node.IsObjective || node.IsAvailable) return RuntimeUi.Accent;
            if (node.IsCamp || node.IsVisited) return RuntimeUi.Positive;
            return new Color(0.34f, 0.39f, 0.47f, node.IsRevealed ? 0.92f : 0.64f);
        }

        private static Color ExpeditionNodeFillColor074(ExpeditionBoardNodeView074 node)
        {
            if (node.IsCurrent) return new Color(0.04f, 0.24f, 0.31f, 1f);
            if (node.IsAvailable) return new Color(0.30f, 0.20f, 0.055f, 1f);
            if (node.IsVisited) return new Color(0.055f, 0.24f, 0.15f, 1f);
            return new Color(0.035f, 0.055f, 0.085f, node.IsRevealed ? 0.98f : 0.82f);
        }

        private static string ExpeditionNodeGlyph074(ExpeditionBoardNodeView074 node)
        {
            if (node.IsCurrent) return "◆";
            if (node.IsThreat) return "!";
            if (node.IsObjective) return "★";
            if (node.IsCamp) return "C";
            if (node.IsVisited) return "✓";
            if (node.IsAvailable) return "+";
            return "•";
        }

        private static string PlayerFacingNodeKind074(string kind)
        {
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return "DEPLOYMENT";
                case "FORK": return "ROUTE DECISION";
                case "SKILL_CHECK": return "FIELD CHECK";
                case "SCOUTING": return "SCOUTING";
                case "RESOURCE": return "SUPPLY OPPORTUNITY";
                case "ENCOUNTER": return "ENEMY UNION";
                case "OPTIONAL_ELITE": return "ELITE THREAT";
                case "CAMP": return "FIELD CAMP";
                case "EVENT": return "STORY EVENT";
                case "SECONDARY_OBJECTIVE": return "SIDE OBJECTIVE";
                case "MAIN_OBJECTIVE": return "MAIN OBJECTIVE";
                case "SAFE_ROUTE": return "SAFE ROUTE";
                case "EXIT": return "RETURN ROUTE";
                default: return "ROUTE LOCATION";
            }
        }
    }
}
