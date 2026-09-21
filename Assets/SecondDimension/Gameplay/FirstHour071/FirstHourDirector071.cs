using System;
using System.Collections.Generic;
using SecondDimension.Core;

namespace SecondDimension.Gameplay.FirstHour071
{
    /// <summary>
    /// Canonical Release 071 opening-hour authority. Presentation code reads this
    /// director; it must not keep a second objective, route, or story cursor.
    /// </summary>
    public sealed class FirstHourDirector071
    {
        public const string ReleaseId = "FIRST_HOUR_071";
        public const string ChapterOneTitle = "Chapter 1: The Bell Beneath Skyhome";
        public const string ChapterTwoTitle = "Chapter 2: The Door Inside";
        public const string InitialCheckpointId = "FH071_CP_00_NEW_GUILD";

        private static readonly FirstHourSegment071[] TimelineSource = CreateTimeline();
        private static readonly FirstHourRosterEntry071[] RosterSource = CreateRoster();
        private static readonly FirstHourProtectedActor071[] ProtectedActorSource = CreateProtectedActors();
        private static readonly IReadOnlyList<FirstHourSegment071> TimelineView =
            Array.AsReadOnly(TimelineSource);
        private static readonly IReadOnlyList<FirstHourRosterEntry071> RosterView =
            Array.AsReadOnly(RosterSource);
        private static readonly IReadOnlyList<FirstHourProtectedActor071> ProtectedActorView =
            Array.AsReadOnly(ProtectedActorSource);

        public IReadOnlyList<FirstHourSegment071> Timeline => TimelineView;
        public IReadOnlyList<FirstHourRosterEntry071> PlayableRoster => RosterView;
        public IReadOnlyList<FirstHourProtectedActor071> ProtectedActors => ProtectedActorView;

        public FirstHourState071 StartNewGuild()
        {
            return new FirstHourState071(0);
        }

        public FirstHourPhase071 PhaseOf(FirstHourState071 state)
        {
            RequireValidState(state);
            return state.CompletedSegmentCount == TimelineSource.Length
                ? FirstHourPhase071.Complete
                : TimelineSource[state.CompletedSegmentCount].Phase;
        }

        public bool IsComplete(FirstHourState071 state)
        {
            RequireValidState(state);
            return state.CompletedSegmentCount == TimelineSource.Length;
        }

        /// <summary>
        /// Returns the authored pacing marker for the current story segment.
        /// This is a content-planning value, not measured player elapsed time and
        /// must never be used as proof of a sixty-minute play session.
        /// </summary>
        public int PacingMarkerMinute(FirstHourState071 state)
        {
            RequireValidState(state);
            return IsComplete(state) ? 60 : TimelineSource[state.CompletedSegmentCount].StartMinute;
        }

        public string LastCheckpointId(FirstHourState071 state)
        {
            RequireValidState(state);
            return state.CompletedSegmentCount == 0
                ? InitialCheckpointId
                : TimelineSource[state.CompletedSegmentCount - 1].CompletionCheckpointId;
        }

        public Result<FirstHourSegment071> CurrentSegment(FirstHourState071 state)
        {
            if (!IsValidState(state))
                return Result<FirstHourSegment071>.Failure("FH071_VALID_STATE_REQUIRED");
            if (state.CompletedSegmentCount == TimelineSource.Length)
                return Result<FirstHourSegment071>.Failure("FH071_FIRST_HOUR_COMPLETE");
            return Result<FirstHourSegment071>.Success(TimelineSource[state.CompletedSegmentCount]);
        }

        public Result<FirstHourState071> Advance(FirstHourState071 state, FirstHourAction071 action)
        {
            if (!IsValidState(state))
                return Result<FirstHourState071>.Failure("FH071_VALID_STATE_REQUIRED");
            if (state.CompletedSegmentCount == TimelineSource.Length)
                return Result<FirstHourState071>.Failure("FH071_FIRST_HOUR_COMPLETE");

            var segment = TimelineSource[state.CompletedSegmentCount];
            if (segment.RequiredAction != action)
                return Result<FirstHourState071>.Failure(
                    "FH071_ACTION_REQUIRED_" + segment.RequiredAction.ToString().ToUpperInvariant());

            return Result<FirstHourState071>.Success(
                new FirstHourState071(state.CompletedSegmentCount + 1));
        }

        public IReadOnlyList<FirstHourRosterEntry071> AvailableRoster(FirstHourState071 state)
        {
            var minute = PacingMarkerMinute(state);
            var available = new List<FirstHourRosterEntry071>();
            for (var index = 0; index < RosterSource.Length; index++)
            {
                if (RosterSource[index].AvailableAtMinute <= minute)
                    available.Add(RosterSource[index]);
            }
            return available.AsReadOnly();
        }

        private static bool IsValidState(FirstHourState071 state)
        {
            return state != null &&
                   state.CompletedSegmentCount >= 0 &&
                   state.CompletedSegmentCount <= TimelineSource.Length;
        }

        private static void RequireValidState(FirstHourState071 state)
        {
            if (!IsValidState(state))
                throw new ArgumentException("A valid Release 071 first-hour state is required.", nameof(state));
        }

        private static FirstHourSegment071[] CreateTimeline()
        {
            return new[]
            {
                Segment(
                    "FH071_00_SKYHOME_ARRIVAL", FirstHourPhase071.SkyhomeArrival, 0, 2,
                    FirstHourSequenceKind071.Story,
                    "Walk with Maren to the Guild Hall.",
                    FirstHourAction071.ReachGuildHall, "Reach the Guild Hall doors.",
                    "DEST_SKYHOME_MARKET", "Skyhome Market",
                    "FH071_CP_01_GUILD_HALL_REACHED", string.Empty,
                    Beat("CANON_KIRI_AETHERHEART", "Kiri Aetherheart",
                        "Skyhome is where broken worlds agreed to build something together."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt",
                        "Then give us a charter, and we'll carry our share.")),

                Segment(
                    "FH071_01_EMERGENCY_CHARTER", FirstHourPhase071.EmergencyCharter, 2, 5,
                    FirstHourSequenceKind071.Story,
                    "Accept Kiri's emergency guild charter.",
                    FirstHourAction071.SignEmergencyCharter, "Speak with Kiri and sign the charter.",
                    "DEST_GUILD_CHARTER_DESK", "Guild Hall charter desk",
                    "FH071_CP_02_CHARTER_SIGNED", string.Empty,
                    Beat("CANON_KIRI_AETHERHEART", "Kiri Aetherheart",
                        "The Founders guard the gates. Your guild protects whoever falls between them."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt",
                        "Tell us who needs help first.")),

                Segment(
                    "FH071_02_BELL_WITHOUT_ROPE", FirstHourPhase071.BellWithoutRope, 5, 7,
                    FirstHourSequenceKind071.Story,
                    "Find what is ringing the bell below the hall.",
                    FirstHourAction071.FollowTheBell, "Follow Orren to the sealed stair.",
                    "DEST_GUILD_BELL_STAIR", "Bell stair",
                    "FH071_CP_03_BREACH_FOUND", string.Empty,
                    Beat("SIGREC_ORREN_CLAY", "Orren Clay", "That bell has no rope."),
                    Beat("CANON_KIRI_AETHERHEART", "Kiri Aetherheart",
                        "Then something below is pulling it.")),

                Segment(
                    "FH071_03_HALL_BREACH", FirstHourPhase071.HallBreachBattle, 7, 14,
                    FirstHourSequenceKind071.Battle,
                    "Protect the hall while Kael holds the breach.",
                    FirstHourAction071.WinHallBreach, "Win the Hall Breach battle.",
                    "DEST_GUILD_HALL_BREACH", "Guild Hall breach",
                    "FH071_CP_04_HALL_BREACH_WON", "ENCOUNTER071_HALL_BREACH",
                    Beat("CANON_KAEL", "Kael",
                        "The strike has been returned. The hand that sent it is still moving."),
                    Beat("PROC_36344E2400DC98B6", "Gara Redtail", "Guild, hold this hall!")),

                Segment(
                    "FH071_04_LANTERN_ROAD_ORDER", FirstHourPhase071.LanternRoadOrder, 14, 18,
                    FirstHourSequenceKind071.Story,
                    "Take Kiri's rescue order for Lantern Road.",
                    FirstHourAction071.AcceptLanternRoadOrder,
                    "Speak with Kiri and review the rescue map.",
                    "DEST_GUILD_STRATEGY_TABLE", "Guild Hall strategy table",
                    "FH071_CP_05_RESCUE_ORDER_ACCEPTED", string.Empty,
                    Beat("CANON_KIRI_AETHERHEART", "Kiri Aetherheart",
                        "Kael, hold the breach. Maren, take Lantern Road. Recover the Wayglass and bring our people home."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt", "We leave as soon as the guild is ready.")),

                Segment(
                    "FH071_05_READY_THE_GUILD", FirstHourPhase071.ReadyTheGuild, 18, 24,
                    FirstHourSequenceKind071.GuildManagement,
                    "Review the ten recruits and equip their starting weapons.",
                    FirstHourAction071.ReadyRosterAndEquipment,
                    "Confirm every recruit's weapon and role.",
                    "DEST_GUILD_ARMORY", "Guild Hall armory",
                    "FH071_CP_06_ROSTER_READY", string.Empty,
                    Beat("SIGREC_ODELIA_FEN", "Odelia Fen",
                        "Keep each recruit with one weapon discipline for now. We can learn by using it."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt", "Clear roles. No one gets lost in the noise.")),

                Segment(
                    "FH071_06_FORM_THE_UNIONS", FirstHourPhase071.FormTheUnions, 24, 28,
                    FirstHourSequenceKind071.GuildManagement,
                    "Place the ten recruits into three Unions.",
                    FirstHourAction071.ConfirmThreeUnions, "Confirm all three Unions.",
                    "DEST_GUILD_UNION_TABLE", "Guild Hall Union table",
                    "FH071_CP_07_UNIONS_READY", string.Empty,
                    Beat("SIGREC_TALA_STORMROAD", "Tala Stormroad",
                        "Maren holds the road, I watch the flank, and Vaelis guards our rear."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt", "Three Unions, one rescue.")),

                Segment(
                    "FH071_07_WALK_LANTERN_ROAD", FirstHourPhase071.WalkLanternRoad, 28, 34,
                    FirstHourSequenceKind071.Exploration,
                    "Follow Lantern Road to the missing patrol.",
                    FirstHourAction071.ReachBrokenWaymarker, "Reach the broken waymarker.",
                    "DEST_LANTERN_ROAD_WAYMARKER", "Lantern Road waymarker",
                    "FH071_CP_08_WAYMARKER_REACHED", string.Empty,
                    Beat("PROC_5B14E7816E55FFB5", "Tazren Warmask",
                        "The patrol left fresh marks. Something drove them off the road.")),

                Segment(
                    "FH071_08_ROADSIDE_AMBUSH", FirstHourPhase071.RoadsideAmbushBattle, 34, 40,
                    FirstHourSequenceKind071.Battle,
                    "Drive the raiders away from Lantern Road.",
                    FirstHourAction071.WinRoadsideAmbush, "Win the Lantern Road battle.",
                    "DEST_LANTERN_ROAD_CUT", "Lantern Road crossing",
                    "FH071_CP_09_ROAD_CLEARED", "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                    Beat("PROC_F85A4CAA747BC8C6", "Daeven Fellstar",
                        "Break their line and the road opens."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt", "Unions, move together!")),

                Segment(
                    "FH071_09_FIND_THE_PATROL", FirstHourPhase071.FindThePatrol, 40, 45,
                    FirstHourSequenceKind071.Exploration,
                    "Find the patrol beyond the broken waymarker.",
                    FirstHourAction071.FollowPatrolTrail, "Follow the tracks to the old gatehouse.",
                    "DEST_OLD_GATEHOUSE_TRAIL", "Old gatehouse trail",
                    "FH071_CP_10_PATROL_FOUND", string.Empty,
                    Beat("SIGREC_VAELIS_NOCT", "Vaelis Noct",
                        "Ten sets of tracks went in. None came back out."),
                    Beat("SIGREC_ZORIN_BRAMBLECROSS", "Zorin Bramblecross",
                        "Skyhome guild! We're inside, and the Wayglass is still with us!")),

                Segment(
                    "FH071_10_RECOVER_THE_WAYGLASS", FirstHourPhase071.RecoverTheWayglass, 45, 50,
                    FirstHourSequenceKind071.Exploration,
                    "Free the patrol and recover the Wayglass.",
                    FirstHourAction071.FreePatrolAndRecoverWayglass,
                    "Release the trapped patrol and take the Wayglass.",
                    "DEST_OLD_GATEHOUSE", "Old gatehouse",
                    "FH071_CP_11_WAYGLASS_RECOVERED", string.Empty,
                    Beat("SIGREC_ZORIN_BRAMBLECROSS", "Zorin Bramblecross",
                        "We can fight. Open the way and our ten join your guild."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt",
                        "Stay close. Everyone goes home together.")),

                Segment(
                    "FH071_11_GATE_EATER", FirstHourPhase071.GateEaterBattle, 50, 57,
                    FirstHourSequenceKind071.Battle,
                    "Defeat the Gate-Eater before it reaches Skyhome.",
                    FirstHourAction071.WinGateEaterBattle, "Win the Gate-Eater battle.",
                    "DEST_GATEHOUSE_BREACH", "Gatehouse breach",
                    "FH071_CP_12_GATE_EATER_DEFEATED", "ENCOUNTER071_GATE_EATER",
                    Beat("SIGREC_ZORIN_BRAMBLECROSS", "Zorin Bramblecross",
                        "The Wayglass brought it here. Let us help finish this."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt",
                        "Twenty guildmates. One line. Hold!")),

                Segment(
                    "FH071_12_RETURN_TO_SKYHOME", FirstHourPhase071.ReturnToSkyhome, 57, 59,
                    FirstHourSequenceKind071.Story,
                    "Carry the Wayglass and the rescued patrol home.",
                    FirstHourAction071.EnterGuildHall, "Enter the Guild Hall together.",
                    "DEST_GUILD_HALL_RETURN", "Guild Hall",
                    "FH071_CP_13_GUILD_RETURNED", string.Empty,
                    Beat("CANON_KIRI_AETHERHEART", "Kiri Aetherheart",
                        "Twenty returned where ten were expected. That is what this charter is for."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt", "The Wayglass has something to show us.")),

                Segment(
                    "FH071_13_CHAPTER_TWO_HOOK", FirstHourPhase071.ChapterTwoHook, 59, 60,
                    FirstHourSequenceKind071.ChapterHook,
                    "Report the rescue and open the Wayglass map.",
                    FirstHourAction071.OpenWayglassMap, "Inspect the new door on the Wayglass map.",
                    "DEST_GUILD_WAYGLASS_TABLE", "Guild Hall Wayglass table",
                    "FH071_CP_14_CHAPTER_TWO_READY", string.Empty,
                    Beat("SIGREC_BESSA_BRASSWHISTLE", "Bessa Brasswhistle",
                        "That route does not point beyond Skyhome. It points beneath us."),
                    Beat("CANON_KIRI_AETHERHEART", "Kiri Aetherheart", "A door inside Skyhome."),
                    Beat("SIGREC_MAREN_HOLT", "Maren Holt", "Then Chapter Two starts here."))
            };
        }

        private static FirstHourRosterEntry071[] CreateRoster()
        {
            const string charterReason = "Available after the emergency charter is signed.";
            const string patrolReason = "Rescued on Lantern Road and available for the Gate-Eater battle.";
            return new[]
            {
                Recruit("PROC_36344E2400DC98B6", "Gara Redtail", "CLASS_GUARDIAN", "Skyhome Gateward",
                    FirstHourRosterWave071.OpeningLead, 0, FirstHourPhase071.SkyhomeArrival,
                    "Opening field lead and first playable guild member."),
                Recruit("PROC_F85A4CAA747BC8C6", "Daeven Fellstar", "CLASS_WARRIOR", "Skyhome Freight Ward",
                    FirstHourRosterWave071.OpeningLead, 0, FirstHourPhase071.SkyhomeArrival,
                    "Founding companion on the emergency charter."),
                Recruit("PROC_5B14E7816E55FFB5", "Tazren Warmask", "CLASS_RANGER", "Lantern Road Camp",
                    FirstHourRosterWave071.OpeningLead, 0, FirstHourPhase071.SkyhomeArrival,
                    "Founding companion on the emergency charter."),
                Recruit("PROC_748DD03A23E1FEB0", "Jazzi Wirewick", "CLASS_PRIEST", "Brasswork Annex",
                    FirstHourRosterWave071.OpeningLead, 0, FirstHourPhase071.SkyhomeArrival,
                    "Founding companion on the emergency charter."),
                Recruit("SIGREC_MAREN_HOLT", "Maren Holt", "CLASS_GUARDIAN", "Gateward East Barracks",
                    FirstHourRosterWave071.OpeningLead, 0, FirstHourPhase071.SkyhomeArrival,
                    "Founding companion and clear-order Union leader."),
                Recruit("SIGREC_ODELIA_FEN", "Odelia Fen", "CLASS_PRIEST", "South Infirmary Annex",
                    FirstHourRosterWave071.OpeningLead, 0, FirstHourPhase071.SkyhomeArrival,
                    "Founding companion and restoration specialist."),
                Recruit("SIGREC_TALA_STORMROAD", "Tala Stormroad", "CLASS_RANGER", "Stormroad Route Council",
                    FirstHourRosterWave071.SkyhomeCharter, 5, FirstHourPhase071.BellWithoutRope, charterReason),
                Recruit("SIGREC_ORREN_CLAY", "Orren Clay", "CLASS_WARRIOR", "Gateward Freight Yard",
                    FirstHourRosterWave071.SkyhomeCharter, 5, FirstHourPhase071.BellWithoutRope, charterReason),
                Recruit("SIGREC_BESSA_BRASSWHISTLE", "Bessa Brasswhistle", "CLASS_MAGE", "Brasswork Signal Loft",
                    FirstHourRosterWave071.SkyhomeCharter, 5, FirstHourPhase071.BellWithoutRope, charterReason),
                Recruit("SIGREC_VAELIS_NOCT", "Vaelis Noct", "CLASS_ROGUE", "Dusk Archive Row",
                    FirstHourRosterWave071.SkyhomeCharter, 5, FirstHourPhase071.BellWithoutRope, charterReason),
                Recruit("SIGREC_ZORIN_BRAMBLECROSS", "Zorin Bramblecross", "CLASS_GUARDIAN", "Cinder Yard",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_UNA_QUEENSREST", "Una Queensrest", "CLASS_WARRIOR", "Chainmarket",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_JUNIA_SKYWARD", "Junia Skyward", "CLASS_PRIEST", "Furnace Nine",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_DAIN_DEEPWELL", "Dain Deepwell", "CLASS_MAGE", "Slag Gardens",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_WILLOW_LONGSTRIDE", "Willow Longstride", "CLASS_GUARDIAN", "Emberwall",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_QUIN_LOWEN", "Quin Lowen", "CLASS_RANGER", "Cinder Yard",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_ASTER_MARSHLIGHT", "Aster Marshlight", "CLASS_ROGUE", "Chainmarket",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_PETRA_RUNEBROOK", "Petra Runebrook", "CLASS_WARRIOR", "Furnace Nine",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_QUIN_CROWNHILL", "Quin Crownhill", "CLASS_PRIEST", "Slag Gardens",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason),
                Recruit("SIGREC_YVES_THORNFIELD", "Yves Thornfield", "CLASS_RANGER", "Emberwall",
                    FirstHourRosterWave071.LanternPatrol, 50, FirstHourPhase071.GateEaterBattle, patrolReason)
            };
        }

        private static FirstHourProtectedActor071[] CreateProtectedActors()
        {
            return new[]
            {
                new FirstHourProtectedActor071("CANON_KAEL", "Kael", "CORE_CANON_SPECIAL"),
                new FirstHourProtectedActor071("CANON_KIRI_AETHERHEART", "Kiri Aetherheart", "FOUNDER"),
                new FirstHourProtectedActor071("CANON_LUNA_SILVERSTEP", "Luna Silverstep", "FOUNDER"),
                new FirstHourProtectedActor071("CANON_GROM_IRONBLOOD", "Grom Ironblood", "FOUNDER"),
                new FirstHourProtectedActor071("CANON_SERAPHINA_NIGHTVEIL", "Seraphina Nightveil", "FOUNDER"),
                new FirstHourProtectedActor071("CANON_AIRA_MOONFANG", "Aira Moonfang", "FOUNDER"),
                new FirstHourProtectedActor071("CANON_LYSSARA_DUSKWOOD", "Lyssara Duskwood", "FOUNDER"),
                new FirstHourProtectedActor071("CANON_RIKA_STORMPAW", "Rika Stormpaw", "FOUNDER")
            };
        }

        private static FirstHourSegment071 Segment(
            string segmentId,
            FirstHourPhase071 phase,
            int startMinute,
            int endMinute,
            FirstHourSequenceKind071 kind,
            string objective,
            FirstHourAction071 requiredAction,
            string nextAction,
            string destinationId,
            string destinationName,
            string completionCheckpointId,
            string encounterId,
            params FirstHourDialogueBeat071[] dialogue)
        {
            return new FirstHourSegment071(
                segmentId, phase, startMinute, endMinute, kind, objective, requiredAction,
                nextAction, destinationId, destinationName, completionCheckpointId, encounterId,
                Array.AsReadOnly(dialogue ?? Array.Empty<FirstHourDialogueBeat071>()));
        }

        private static FirstHourDialogueBeat071 Beat(string speakerId, string speakerName, string line)
        {
            return new FirstHourDialogueBeat071(speakerId, speakerName, line);
        }

        private static FirstHourRosterEntry071 Recruit(
            string recruitId,
            string displayName,
            string classId,
            string homeName,
            FirstHourRosterWave071 wave,
            int availableAtMinute,
            FirstHourPhase071 availableAtPhase,
            string availabilityReason)
        {
            return new FirstHourRosterEntry071(
                recruitId, displayName, classId, homeName, wave, availableAtMinute,
                availableAtPhase, availabilityReason);
        }
    }
}
