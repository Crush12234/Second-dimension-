using System;
using System.Collections.Generic;
using System.Linq;
namespace SecondDimension.Gameplay.Campaign020
{
    public static class CampaignMissionMap132
    {
        public const int MissionCount = 81;
        public const string FinaleChapterId = "CH018_082";
        public const string FinaleOpeningChapterId = "CH018_081";
        public static int Number(string chapterId)
        {
            if (chapterId == null || chapterId.Length != 9 || !chapterId.StartsWith("CH018_", StringComparison.Ordinal) ||
                !int.TryParse(chapterId.Substring(6), out var index) || index < 1 || index > 82) return 0;
            return Math.Min(MissionCount, index);
        }
        public static string Label(string chapterId) => Number(chapterId) == 0 ? "STORY QUEST" :
            "MISSION " + Number(chapterId) + (chapterId == FinaleChapterId ? " • FINALE" : string.Empty);
        public static int CompletedCount(IEnumerable<string> ids)
        {
            var completed = new HashSet<string>(ids ?? Array.Empty<string>(), StringComparer.Ordinal);
            return Enumerable.Range(1,80).Count(index => completed.Contains("CH018_" + index.ToString("000"))) +
                (completed.Contains(FinaleOpeningChapterId) && completed.Contains(FinaleChapterId) ? 1 : 0);
        }
    }
}
