using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public static class GuildMemberRetentionPolicy017D
    {
        public static bool CanAutomaticallyRemoveNormalRecruit(string reason) => false;
        public static bool RelationshipSceneConsumesOperation => false;
        public static bool EmptyWaitOperationAllowed => false;
        public static bool RealTimeConstructionTimerAllowed => false;
    }

    public sealed class GuildCityCommandService017D
    {
        public const int OpeningPlotUnlockProgress = 50;
        public Result<CampaignState> SetAssignment(CampaignState campaign, string recruitId,
            GuildMemberAssignmentKind017D kind, string facilityId = "")
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            if (string.IsNullOrWhiteSpace(recruitId)) return Result<CampaignState>.Failure("GC017D_RECRUIT_REQUIRED");
            if (!ContainsRecruit(campaign.Guild.Recruits, recruitId)) return Result<CampaignState>.Failure("GC017D_RECRUIT_NOT_OWNED");
            if (kind == GuildMemberAssignmentKind017D.Archived)
                return Result<CampaignState>.Failure("GC017D_ARCHIVE_REQUIRES_EXPLICIT_CONFIRMATION");
            if (kind == GuildMemberAssignmentKind017D.Staff && string.IsNullOrWhiteSpace(facilityId))
                return Result<CampaignState>.Failure("GC017D_STAFF_FACILITY_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var assignments = new List<GuildMemberAssignmentState017D>(state.MemberAssignments);
            var index = FindAssignment(assignments, recruitId);
            var targetFacility = kind == GuildMemberAssignmentKind017D.Staff ? facilityId : string.Empty;
            if (index >= 0 && assignments[index].Kind == kind &&
                StringComparer.Ordinal.Equals(assignments[index].FacilityId, targetFacility))
                return Result<CampaignState>.Success(campaign);
            var value = new GuildMemberAssignmentState017D(recruitId, kind,
                targetFacility, 0, 0, 0);
            if (index >= 0) assignments[index] = value; else assignments.Add(value);
            assignments.Sort((a,b)=>StringComparer.Ordinal.Compare(a.RecruitId,b.RecruitId));
            return Success(campaign, state.With(memberAssignments: assignments.AsReadOnly(), lastCheckpointId: "assignment_changed"));
        }

        public Result<CampaignState> ArchiveMemberExplicit(CampaignState campaign, string recruitId, bool confirmed)
        {
            if (!confirmed) return Result<CampaignState>.Failure("GC017D_ARCHIVE_CONFIRMATION_REQUIRED");
            if (campaign == null || !ContainsRecruit(campaign.Guild.Recruits, recruitId))
                return Result<CampaignState>.Failure("GC017D_RECRUIT_NOT_OWNED");
            var state = campaign.Guild.GuildCity;
            var assignments = new List<GuildMemberAssignmentState017D>(state.MemberAssignments);
            var index = FindAssignment(assignments, recruitId);
            var value = new GuildMemberAssignmentState017D(recruitId, GuildMemberAssignmentKind017D.Archived, string.Empty, 0, 0, 0);
            if (index >= 0) assignments[index] = value; else assignments.Add(value);
            return Success(campaign, state.With(memberAssignments: assignments.AsReadOnly(), lastCheckpointId: "member_archived_explicitly"));
        }

        public Result<CampaignState> AddRelationshipMemory(CampaignState campaign, string firstRecruitId,
            string secondRecruitId, string sourceId, string summary, int strength, string sceneId)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            if (!ContainsRecruit(campaign.Guild.Recruits, firstRecruitId) || !ContainsRecruit(campaign.Guild.Recruits, secondRecruitId))
                return Result<CampaignState>.Failure("GC017D_RELATIONSHIP_RECRUITS_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var hash = CanonicalJson.Sha256Hex(new { campaign.CampaignGuid, state.OperationOrdinal, firstRecruitId, secondRecruitId, sourceId });
            var memoryId = "REL_MEMORY_" + hash.Substring(0,24).ToUpperInvariant();
            for (var i=0;i<state.RelationshipMemories.Count;i++)
                if (StringComparer.Ordinal.Equals(state.RelationshipMemories[i].MemoryId,memoryId)) return Result<CampaignState>.Success(campaign);
            var memories = new List<RelationshipMemoryState017D>(state.RelationshipMemories)
            {
                new RelationshipMemoryState017D(memoryId, firstRecruitId, secondRecruitId, sourceId,
                    summary, state.OperationOrdinal, strength, sceneId, false)
            };
            memories.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MemoryId,b.MemoryId));
            return Success(campaign, state.With(relationshipMemories: memories.AsReadOnly(), lastCheckpointId: "relationship_memory_added"));
        }

        public Result<CampaignState> ViewRelationshipScene(CampaignState campaign, string sceneId)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            var state = campaign.Guild.GuildCity;
            if (GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign))
                return Result<CampaignState>.Failure(
                    "GC017D_FINISH_ACTIVE_OPERATION_FIRST");
            var memories = new List<RelationshipMemoryState017D>();
            var found = false;
            for (var i=0;i<state.RelationshipMemories.Count;i++)
            {
                var value = state.RelationshipMemories[i];
                if (StringComparer.Ordinal.Equals(value.SceneId,sceneId)) { value = value.WithViewed(true); found = true; }
                memories.Add(value);
            }
            if (!found) return Result<CampaignState>.Failure("GC017D_RELATIONSHIP_SCENE_NOT_FOUND");
            return Success(campaign, state.With(relationshipMemories: memories.AsReadOnly(), lastCheckpointId: "relationship_scene_viewed_free"));
        }

        public Result<CampaignState> PlaceBuilding(CampaignState campaign, GuildCityContent017D content,
            string plotId, string buildingId)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_CITY_INPUT_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var plots = new List<CityPlotState017D>(state.CityPlots);
            var plotIndex = FindPlot(plots, plotId);
            if (plotIndex < 0) return Result<CampaignState>.Failure("GC017D_PLOT_NOT_FOUND");
            var plot = plots[plotIndex];
            if (!plot.Unlocked) return Result<CampaignState>.Failure("GC017D_PLOT_LOCKED");
            if (!string.IsNullOrEmpty(plot.BuildingId)) return Result<CampaignState>.Failure("GC017D_PLOT_OCCUPIED");
            if (!content.Buildings.ContainsKey(buildingId)) return Result<CampaignState>.Failure("GC017D_BUILDING_NOT_FOUND");
            var definition = content.Building(buildingId);
            if (!StringComparer.Ordinal.Equals(plot.DistrictId,definition.DistrictId))
                return Result<CampaignState>.Failure("GC017D_BUILDING_DISTRICT_ILLEGAL");
            var cost = definition.CostForLevel(1);
            var credits = state.CharterBuildCredits;
            var guild = campaign.Guild;
            var materials = new List<GuildMaterialState017D>(state.Materials);
            // Global facility benefits may predate this plot or belong to another plot.
            // Adding construction never removes an already purchased level.
            int retainedLevel = Math.Max(1, new GuildCityEffectService017D().FacilityLevel(guild.Development, definition.FacilityId));
            GuildDevelopmentState development;
            if (credits > 0)
            {
                credits--;
                development = guild.Development.SetFacilityLevel(definition.FacilityId, retainedLevel, 0);
            }
            else
            {
                if (cost == null) return Result<CampaignState>.Failure("GC017D_BUILDING_COST_MISSING");
                if (guild.Development.HallEnhancementXp < cost.HallXp) return Result<CampaignState>.Failure("GC017D_HALL_XP_INSUFFICIENT");
                if (!CanAfford(materials,cost.Materials)) return Result<CampaignState>.Failure("GC017D_MATERIALS_INSUFFICIENT");
                materials = SpendMaterials(materials,cost.Materials);
                development = guild.Development.SpendHallEnhancementXp(cost.HallXp)
                    .SetFacilityLevel(definition.FacilityId, retainedLevel, cost.HallXp);
            }
            guild = guild.With(guild.TreasuryXp, guild.Recruits, guild.Unions, guild.Inventory, development);
            plots[plotIndex] = plot.With(buildingId:buildingId,buildingLevel:1,constructionProgress:100);
            var city = state.With(charterBuildCredits:credits,materials:materials.AsReadOnly(),cityPlots:plots.AsReadOnly(),lastCheckpointId:"building_placed");
            return Result<CampaignState>.Success(campaign.With(guild.WithGuildCity(city),campaign.OpeningFlow));
        }

        public Result<CampaignState> UpgradeBuilding(CampaignState campaign, GuildCityContent017D content, string plotId)
        {
            if (campaign == null || content == null) return Result<CampaignState>.Failure("GC017D_CITY_INPUT_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var plots = new List<CityPlotState017D>(state.CityPlots);
            var plotIndex = FindPlot(plots,plotId);
            if (plotIndex < 0 || string.IsNullOrEmpty(plots[plotIndex].BuildingId)) return Result<CampaignState>.Failure("GC017D_BUILDING_REQUIRED");
            var plot = plots[plotIndex];
            var definition = content.Building(plot.BuildingId);
            var next = plot.BuildingLevel + 1;
            if (next > definition.MaxLevel) return Result<CampaignState>.Failure("GC017D_BUILDING_MAX_LEVEL");
            var cost = definition.CostForLevel(next);
            if (campaign.Guild.Development.HallEnhancementXp < cost.HallXp) return Result<CampaignState>.Failure("GC017D_HALL_XP_INSUFFICIENT");
            var materials = new List<GuildMaterialState017D>(state.Materials);
            if (!CanAfford(materials,cost.Materials)) return Result<CampaignState>.Failure("GC017D_MATERIALS_INSUFFICIENT");
            materials = SpendMaterials(materials,cost.Materials);
            plots[plotIndex] = plot.With(buildingLevel:next,constructionProgress:100);
            var development = campaign.Guild.Development.SpendHallEnhancementXp(cost.HallXp).SetFacilityLevel(definition.FacilityId,Math.Max(next,new GuildCityEffectService017D().FacilityLevel(campaign.Guild.Development,definition.FacilityId)),cost.HallXp);
            var city = state.With(materials:materials.AsReadOnly(),cityPlots:plots.AsReadOnly(),lastCheckpointId:"building_upgraded");
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development).WithGuildCity(city);
            return Result<CampaignState>.Success(campaign.With(guild,campaign.OpeningFlow));
        }

        public Result<CampaignState> AssignStaff(CampaignState campaign, string plotId, string recruitId)
        {
            if (campaign == null || !ContainsRecruit(campaign.Guild.Recruits,recruitId))
                return Result<CampaignState>.Failure("GC017D_RECRUIT_NOT_OWNED");
            var state = campaign.Guild.GuildCity;
            var existingAssignment = FindAssignmentState(state.MemberAssignments, recruitId);
            if (existingAssignment != null && existingAssignment.Kind == GuildMemberAssignmentKind017D.Deployed)
                return Result<CampaignState>.Failure("GC017D_DEPLOYED_MEMBER_CANNOT_STAFF");
            var plots = new List<CityPlotState017D>();
            var targetIndex = -1;
            for (var plotIndex = 0; plotIndex < state.CityPlots.Count; plotIndex++)
            {
                var plot = state.CityPlots[plotIndex];
                var staff = new List<string>(plot.StaffRecruitIds);
                staff.Remove(recruitId);
                if (StringComparer.Ordinal.Equals(plot.PlotId, plotId)) targetIndex = plotIndex;
                plots.Add(plot.With(staffRecruitIds: staff.AsReadOnly()));
            }
            if (targetIndex < 0 || string.IsNullOrEmpty(plots[targetIndex].BuildingId))
                return Result<CampaignState>.Failure("GC017D_BUILDING_REQUIRED");
            var targetStaff = new List<string>(plots[targetIndex].StaffRecruitIds);
            if (!targetStaff.Contains(recruitId)) targetStaff.Add(recruitId);
            targetStaff.Sort(StringComparer.Ordinal);
            plots[targetIndex] = plots[targetIndex].With(staffRecruitIds: targetStaff.AsReadOnly());
            var assignmentResult = SetAssignment(campaign,recruitId,
                GuildMemberAssignmentKind017D.Staff,plots[targetIndex].BuildingId);
            if (!assignmentResult.IsSuccess) return assignmentResult;
            var after = assignmentResult.Value.Guild.GuildCity.With(
                cityPlots:plots.AsReadOnly(),lastCheckpointId:"staff_assigned");
            return Success(assignmentResult.Value,after);
        }

        public Result<CampaignState> DeployStaffMember(CampaignState campaign, string recruitId)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            var state = campaign.Guild.GuildCity;
            var plots = new List<CityPlotState017D>();
            for (var i=0;i<state.CityPlots.Count;i++)
            {
                var plot = state.CityPlots[i];
                var staff = new List<string>(plot.StaffRecruitIds);
                staff.Remove(recruitId);
                plots.Add(plot.With(staffRecruitIds:staff.AsReadOnly()));
            }
            var assigned = SetAssignment(campaign,recruitId,GuildMemberAssignmentKind017D.Active);
            if (!assigned.IsSuccess) return assigned;
            return Success(assigned.Value,assigned.Value.Guild.GuildCity.With(cityPlots:plots.AsReadOnly(),lastCheckpointId:"staff_recalled_for_deployment"));
        }

        public int AdjacencyBonusCount(GuildCityState017D state, GuildCityContent017D content)
        {
            if (state == null || content == null) return 0;
            var count = 0;
            var counted = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < state.CityPlots.Count; index++)
            {
                var plot = state.CityPlots[index];
                if (string.IsNullOrEmpty(plot.BuildingId)) continue;
                var plotDefinition = content.Plot(plot.PlotId);
                var buildingDefinition = content.Building(plot.BuildingId);
                for (var adjacentIndex = 0; adjacentIndex < (plotDefinition.AdjacentPlotIds?.Length ?? 0); adjacentIndex++)
                {
                    var adjacentPlot = FindPlotState(state.CityPlots, plotDefinition.AdjacentPlotIds[adjacentIndex]);
                    if (adjacentPlot == null || string.IsNullOrEmpty(adjacentPlot.BuildingId)) continue;
                    if (!IsAdjacencyPartner(buildingDefinition, adjacentPlot.BuildingId) &&
                        !IsAdjacencyPartner(content.Building(adjacentPlot.BuildingId), plot.BuildingId)) continue;
                    var pair = StringComparer.Ordinal.Compare(plot.PlotId, adjacentPlot.PlotId) < 0
                        ? plot.PlotId + "|" + adjacentPlot.PlotId
                        : adjacentPlot.PlotId + "|" + plot.PlotId;
                    if (counted.Add(pair)) count++;
                }
            }
            return count;
        }

        public Result<CampaignState> CompleteMeaningfulOperation(CampaignState campaign, GuildCityContent017D content = null)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            var state = campaign.Guild.GuildCity;
            if (GuildCityExpeditionService017D.HasUnresolvedOperation(state))
                return Result<CampaignState>.Failure(
                    "GC017D_FINISH_ACTIVE_OPERATION_FIRST");
            var effects = new GuildCityEffectService017D();
            var assignments = new List<GuildMemberAssignmentState017D>();
            for (var i=0;i<state.MemberAssignments.Count;i++)
            {
                var value = state.MemberAssignments[i];
                switch(value.Kind)
                {
                    case GuildMemberAssignmentKind017D.Recovering:
                        value = value.With(recoveryProgress:(int)Math.Min(int.MaxValue, checked((long)value.RecoveryProgress + effects.RecoveryProgressPerOperation(campaign.Guild.Development, state, content))));
                        break;
                    case GuildMemberAssignmentKind017D.Training:
                        value = value.With(trainingProgress:value.TrainingProgress + effects.TrainingProgressPerOperation(campaign.Guild.Development, state, content));
                        break;
                    case GuildMemberAssignmentKind017D.Staff:
                        value = value.With(dutyProgress:value.DutyProgress + effects.DutyProgressPerOperation(campaign.Guild.Development, state, content));
                        break;
                }
                assignments.Add(value);
            }
            var plots = AdvanceOpeningCityProject(state.CityPlots, 30);
            return Success(campaign,state.With(
                operationOrdinal:state.OperationOrdinal+1,
                memberAssignments:assignments.AsReadOnly(),
                cityPlots:plots,
                lastCheckpointId:"meaningful_operation_completed"));
        }

        public static IReadOnlyList<CityPlotState017D> AdvanceOpeningCityProject(
            IReadOnlyList<CityPlotState017D> source,
            int contribution)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (contribution < 0) throw new ArgumentOutOfRangeException(nameof(contribution));
            if (contribution == 0) return source;

            var result = new List<CityPlotState017D>(source);
            for (var index = 0; index < result.Count; index++)
            {
                var plot = result[index];
                if (plot.Unlocked) continue;
                var progress = checked(plot.ConstructionProgress + contribution);
                var unlocked = progress >= OpeningPlotUnlockProgress;
                result[index] = plot.With(
                    unlocked: unlocked,
                    constructionProgress: Math.Min(OpeningPlotUnlockProgress, progress));
                break;
            }
            return result.AsReadOnly();
        }

        private static CityPlotState017D FindPlotState(IReadOnlyList<CityPlotState017D> values, string id)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].PlotId, id)) return values[index];
            return null;
        }

        private static bool IsAdjacencyPartner(GuildCityBuildingDefinition017D definition, string buildingId)
        {
            for (var index = 0; index < (definition.AdjacencyBuildingIds?.Length ?? 0); index++)
                if (StringComparer.Ordinal.Equals(definition.AdjacencyBuildingIds[index], buildingId)) return true;
            return false;
        }

        private static Result<CampaignState> Success(CampaignState campaign, GuildCityState017D city) =>
            Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
        private static bool ContainsRecruit(IReadOnlyList<RecruitState> values,string id){for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i].RecruitId,id))return true;return false;}
        private static int FindAssignment(IReadOnlyList<GuildMemberAssignmentState017D> values,string id){for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i].RecruitId,id))return i;return -1;}
        private static GuildMemberAssignmentState017D FindAssignmentState(IReadOnlyList<GuildMemberAssignmentState017D> values,string id){var index=FindAssignment(values,id);return index<0?null:values[index];}
        private static int FindPlot(IReadOnlyList<CityPlotState017D> values,string id){for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i].PlotId,id))return i;return -1;}
        private static int MaterialAmount(IReadOnlyList<GuildMaterialState017D> values,string id){for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i].MaterialId,id))return values[i].Amount;return 0;}
        private static bool CanAfford(IReadOnlyList<GuildMaterialState017D> values,IReadOnlyList<GuildMaterialDefinition017D> costs){if(costs==null)return true;for(var i=0;i<costs.Count;i++)if(MaterialAmount(values,costs[i].MaterialId)<costs[i].Amount)return false;return true;}
        private static List<GuildMaterialState017D> SpendMaterials(IReadOnlyList<GuildMaterialState017D> source,IReadOnlyList<GuildMaterialDefinition017D> costs)
        {
            var result=new List<GuildMaterialState017D>(source);
            if(costs!=null)for(var i=0;i<costs.Count;i++)
            {
                var found=false;
                for(var j=0;j<result.Count;j++)if(StringComparer.Ordinal.Equals(result[j].MaterialId,costs[i].MaterialId)){result[j]=result[j].WithAmount(result[j].Amount-costs[i].Amount);found=true;break;}
                if(!found)throw new InvalidOperationException("Missing required material.");
            }
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId));return result;
        }
    }
}

