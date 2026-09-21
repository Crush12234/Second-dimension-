using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017H
{
    public static class GuildCityStoryGateService017H
    {
        public const string OpeningGuild = "STORY_GATE_OPENING_GUILD";
        public const string CityStageTwo = "STORY_GATE_CITY_STAGE_2";
        public const string ChronicleUnlocked = "STORY_GATE_CHRONICLE_UNLOCKED";
        public const string FirstCityDefense = "STORY_GATE_FIRST_CITY_DEFENSE";

        public static Result<CampaignState> SynchronizeDerivedGates(CampaignState campaign)
        {
            if (campaign?.Guild?.GuildCity == null)
                return Result<CampaignState>.Failure("GC017H_GUILD_CITY_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var gates = new List<string>(strategic.StoryGates);
            Add(gates, OpeningGuild);

            var builtCount = 0;
            var chronicleBuilt = false;
            for (var index = 0; index < city.CityPlots.Count; index++)
            {
                var plot = city.CityPlots[index];
                if (string.IsNullOrWhiteSpace(plot.BuildingId) || plot.BuildingLevel <= 0) continue;
                builtCount++;
                if (StringComparer.Ordinal.Equals(plot.BuildingId, "GC017D_BUILD_LIBRARY_ARCHIVE") ||
                    StringComparer.Ordinal.Equals(plot.BuildingId, "GC017H_BUILD_CHRONICLE_PLAZA"))
                    chronicleBuilt = true;
            }

            var hallStage = campaign.Guild.Development?.HallStageIndex ?? 0;
            if (hallStage >= 1 || builtCount >= 4 || city.OperationOrdinal >= 3) Add(gates, CityStageTwo);
            if (chronicleBuilt) Add(gates, ChronicleUnlocked);
            if (strategic.TotalDefensesWon > 0) Add(gates, FirstCityDefense);

            gates.Sort(StringComparer.Ordinal);
            if (Same(strategic.StoryGates, gates)) return Result<CampaignState>.Success(campaign);
            var next = strategic.With(storyGates: gates.AsReadOnly(), lastCheckpointId: "derived_story_gates_synchronized");
            city = city.With(strategic017H: next, replaceStrategic017H: true,
                lastCheckpointId: "derived_story_gates_synchronized");
            return Result<CampaignState>.Success(
                campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow));
        }

        private static void Add(List<string> values, string value)
        { if (!values.Contains(value)) values.Add(value); }

        private static bool Same(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Count != b.Count) return false;
            for (var index = 0; index < a.Count; index++)
                if (!StringComparer.Ordinal.Equals(a[index], b[index])) return false;
            return true;
        }
    }
}
