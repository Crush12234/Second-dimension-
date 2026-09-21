using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    /// <summary>
    /// Converts visible city construction and currently-active staff into bounded,
    /// explainable operational effects. These adapters never resolve combat, grant
    /// mastered Arts, or alter committed outcomes after the fact.
    /// </summary>
    public sealed class GuildCityEffectService017D
    {
        public const int MaximumAuthoredGuildHousingLevel = 4;

        // PASS_05 FACILITY_DEFINITIONS memberCapacity values, bounded to the
        // four authored Guild Housing upgrades exposed by Guild City 017D.
        private static readonly int[] DormitoryRosterCapacities =
        {
            12,
            24,
            40,
            75,
            125
        };

        public int FacilityLevel(GuildDevelopmentState development, string facilityId)
        {
            if (development == null || string.IsNullOrWhiteSpace(facilityId)) return 0;
            for (var index = 0; index < development.Facilities.Count; index++)
            {
                var value = development.Facilities[index];
                if (StringComparer.Ordinal.Equals(value.FacilityId, facilityId)) return value.Level;
            }
            return 0;
        }

        public int BuildingLevel(GuildCityState017D state, string buildingId)
        {
            if (state == null || string.IsNullOrWhiteSpace(buildingId)) return 0;
            var result = 0;
            for (var index = 0; index < state.CityPlots.Count; index++)
            {
                var plot = state.CityPlots[index];
                if (StringComparer.Ordinal.Equals(plot.BuildingId, buildingId))
                    result = Math.Max(result, plot.BuildingLevel);
            }
            return result;
        }

        public int ActiveStaffCount(GuildCityState017D state, string buildingId)
        {
            if (state == null || string.IsNullOrWhiteSpace(buildingId)) return 0;
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (var plotIndex = 0; plotIndex < state.CityPlots.Count; plotIndex++)
            {
                var plot = state.CityPlots[plotIndex];
                if (!StringComparer.Ordinal.Equals(plot.BuildingId, buildingId)) continue;
                for (var staffIndex = 0; staffIndex < plot.StaffRecruitIds.Count; staffIndex++)
                {
                    var recruitId = plot.StaffRecruitIds[staffIndex];
                    var assignment = FindAssignment(state.MemberAssignments, recruitId);
                    if (assignment != null && assignment.Kind == GuildMemberAssignmentKind017D.Staff &&
                        StringComparer.Ordinal.Equals(assignment.FacilityId, buildingId))
                        unique.Add(recruitId);
                }
            }
            return unique.Count;
        }

        public int RecoveryProgressPerOperation(GuildDevelopmentState development) =>
            RecoveryProgressPerOperation(development, null, null);

        public int RecoveryProgressPerOperation(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            long progress = 25L + Math.Max(FacilityLevel(development, "FACILITY_INFIRMARY"), FacilityLevel(development, "CITYTAB_INFIRMARY")) * 5L;
            progress += BuildingLevel(state, "GC017D_BUILD_HERBAL_GARDEN") * 2;
            progress += ActiveStaffCount(state, "GC017D_BUILD_INFIRMARY") * 3;
            progress += ActiveStaffCount(state, "GC017D_BUILD_HERBAL_GARDEN");
            if (HasAdjacency(state, content, "GC017D_BUILD_INFIRMARY", "GC017D_BUILD_HERBAL_GARDEN"))
                progress += 5;
            return (int)Math.Min(int.MaxValue, progress);
        }

        public int TrainingProgressPerOperation(GuildDevelopmentState development) =>
            TrainingProgressPerOperation(development, null, null);

        public int TrainingProgressPerOperation(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            var progress = 10 + FacilityLevel(development, "FACILITY_TRAINING_HALL") * 3;
            progress += FacilityLevel(development, "FACILITY_UNION_COMMAND_TABLE") * 2;
            progress += ActiveStaffCount(state, "GC017D_BUILD_TRAINING_GROUNDS") * 2;
            progress += ActiveStaffCount(state, "GC017D_BUILD_UNION_COMMAND");
            if (HasAdjacency(state, content, "GC017D_BUILD_TRAINING_GROUNDS", "GC017D_BUILD_UNION_COMMAND"))
                progress += 4;
            return progress;
        }

        public int DutyProgressPerOperation(GuildDevelopmentState development) =>
            DutyProgressPerOperation(development, null, null);

        public int DutyProgressPerOperation(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            var progress = 10 + FacilityLevel(development, "FACILITY_QUARTERMASTER") * 2;
            progress += TotalActiveStaff(state);
            if (HasAdjacency(state, content, "GC017D_BUILD_FORGE", "GC017D_BUILD_WAREHOUSE"))
                progress += 3;
            return Math.Min(40, progress);
        }

        public int ApplicantBoardBonusSlots(GuildDevelopmentState development) =>
            ApplicantBoardBonusSlots(development, null, null);

        public int ApplicantBoardBonusSlots(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            var result = Math.Min(4, FacilityLevel(development, "FACILITY_RECRUITMENT_OFFICE"));
            if (ActiveStaffCount(state, "GC017D_BUILD_RECRUITMENT_OFFICE") > 0) result++;
            if (HasAdjacency(state, content,
                    "GC017D_BUILD_RECRUITMENT_OFFICE", "GC017D_BUILD_TAVERN_COMMONS"))
                result++;
            return Math.Min(4, result);
        }

        public int ScoutInformationBonus(GuildDevelopmentState development) =>
            ScoutInformationBonus(development, null, null);

        public int ScoutInformationBonus(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            var result = FacilityLevel(development, "FACILITY_GATE_OPERATIONS") * 10;
            result += BuildingLevel(state, "GC017D_BUILD_WATCHTOWER") * 5;
            result += ActiveStaffCount(state, "GC017D_BUILD_SCOUT_LODGE") * 5;
            result += ActiveStaffCount(state, "GC017D_BUILD_WATCHTOWER") * 5;
            if (HasAdjacency(state, content,
                    "GC017D_BUILD_SCOUT_LODGE", "GC017D_BUILD_WATCHTOWER"))
                result += 10;
            return result;
        }

        public int StartingSupplyBonus(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            var result = FacilityLevel(development, "FACILITY_QUARTERMASTER") * 2;
            result += BuildingLevel(state, "GC017D_BUILD_SUPPLY_DEPOT") * 2;
            result += ActiveStaffCount(state, "GC017D_BUILD_WAREHOUSE");
            result += ActiveStaffCount(state, "GC017D_BUILD_SUPPLY_DEPOT");
            if (HasAdjacency(state, content,
                    "GC017D_BUILD_FORGE", "GC017D_BUILD_WAREHOUSE"))
                result += 1;
            return Math.Min(12, result);
        }

        public int InitialRevealNodeCount(GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content) =>
            Math.Min(6, ScoutInformationBonus(development, state, content) / 10);

        public int RosterCapacity(GuildDevelopmentState development) =>
            Math.Min(300, RosterCapacityAtDormitoryLevel(
                FacilityLevel(development, "FACILITY_DORMITORIES")) +
                GuildCityRecruitmentService017D.EarnedRecruitCapacity094(development));

        public int RosterCapacityAtDormitoryLevel(int dormitoryLevel)
        {
            var boundedLevel = Math.Max(
                0,
                Math.Min(MaximumAuthoredGuildHousingLevel, dormitoryLevel));
            return DormitoryRosterCapacities[boundedLevel];
        }

        public int UnionCapacityBonus(GuildDevelopmentState development) =>
            Math.Min(4, FacilityLevel(development, "FACILITY_UNION_COMMAND_TABLE"));

        public IReadOnlyList<string> ActiveEffectSummaries(
            GuildDevelopmentState development,
            GuildCityState017D state,
            GuildCityContent017D content)
        {
            var values = new List<string>
            {
                "Recovery +" + RecoveryProgressPerOperation(development, state, content) + " progress/operation",
                "Training +" + TrainingProgressPerOperation(development, state, content) + " progress/operation",
                "Starting supplies +" + StartingSupplyBonus(development, state, content),
                "Scout intelligence +" + ScoutInformationBonus(development, state, content),
                "Applicant slots +" + ApplicantBoardBonusSlots(development, state, content),
                "Roster capacity " + RosterCapacity(development),
                "Union capacity +" + UnionCapacityBonus(development),
                "Active city staff " + TotalActiveStaff(state)
            };
            return values.AsReadOnly();
        }

        public bool HasAdjacency(GuildCityState017D state,
            GuildCityContent017D content,
            string firstBuildingId,
            string secondBuildingId)
        {
            if (state == null || content == null || string.IsNullOrWhiteSpace(firstBuildingId) ||
                string.IsNullOrWhiteSpace(secondBuildingId)) return false;
            for (var index = 0; index < state.CityPlots.Count; index++)
            {
                var first = state.CityPlots[index];
                if (!StringComparer.Ordinal.Equals(first.BuildingId, firstBuildingId)) continue;
                var definition = content.Plot(first.PlotId);
                for (var adjacentIndex = 0;
                     adjacentIndex < (definition.AdjacentPlotIds?.Length ?? 0);
                     adjacentIndex++)
                {
                    var adjacent = FindPlot(state.CityPlots, definition.AdjacentPlotIds[adjacentIndex]);
                    if (adjacent != null &&
                        StringComparer.Ordinal.Equals(adjacent.BuildingId, secondBuildingId))
                        return true;
                }
            }
            return false;
        }

        private static int TotalActiveStaff(GuildCityState017D state)
        {
            if (state == null) return 0;
            var result = 0;
            for (var index = 0; index < state.MemberAssignments.Count; index++)
                if (state.MemberAssignments[index].Kind == GuildMemberAssignmentKind017D.Staff) result++;
            return result;
        }

        private static GuildMemberAssignmentState017D FindAssignment(
            IReadOnlyList<GuildMemberAssignmentState017D> values, string recruitId)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].RecruitId, recruitId)) return values[index];
            return null;
        }

        private static CityPlotState017D FindPlot(IReadOnlyList<CityPlotState017D> values, string id)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].PlotId, id)) return values[index];
            return null;
        }
    }
}
