using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Presentation
{
    // Display-only comparison of fresh roster projections after the existing claim
    // authority succeeds. Never computes or grants a progression outcome itself.
    public static class EarnedRecruitFeedback099
    {
        public static string ClaimSummary099(int joined, int duplicates)
        {
            var parts = new List<string>();
            if (joined > 0) parts.Add(joined + (joined == 1 ? " new recruit joined your reserves" : " new recruits joined your reserves"));
            if (duplicates > 0) parts.Add(duplicates + (duplicates == 1 ? " duplicate invitation applied" : " duplicate invitations applied") +
                " • Ascension / skill growth");
            return parts.Count == 0 ? "No new earned rewards to claim." : string.Join(" • ", parts) + " • No XP spent.";
        }

        public static string PreviewSummary099(EarnedCampaignRecruitPreview094 preview) =>
            preview?.IsDuplicate099 == true
                ? (preview.RewardSummary099 ?? "Duplicate hero • Ascension / skill growth")
                : string.Empty;

        public static string CommittedGrowth099(M1RecruitLoadoutView before, M1RecruitLoadoutView after)
        {
            if (before == null || after == null || before.RecruitId != after.RecruitId) return string.Empty;
            if (after.AscensionLevel > before.AscensionLevel)
                return "ASCENDED " + before.AscensionLevel + " → " + after.AscensionLevel + "/10\n" +
                    "+" + Math.Max(0, after.MaximumHp - before.MaximumHp) + " HP  •  +" +
                    Math.Max(0, after.MaximumMp - before.MaximumMp) + " MP";
            var oldIds = new HashSet<string>(before.LearnedArtIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var learned = (after.LearnedArtIds ?? Array.Empty<string>()).Where(id => !oldIds.Contains(id)).ToArray();
            if (learned.Length > 0)
            {
                var names = learned.Select(id => (after.ArtMastery ?? Array.Empty<M1ArtMasteryView>())
                    .FirstOrDefault(art => art != null && art.ArtId == id)?.DisplayName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)).Take(2).ToArray();
                return "NEW SKILL TREE / ART UNLOCKED" + (names.Length == 0 ? string.Empty : "\n" + string.Join(" • ", names));
            }
            foreach (var art in after.ArtMastery ?? Array.Empty<M1ArtMasteryView>())
            {
                if (art == null) continue;
                var old = (before.ArtMastery ?? Array.Empty<M1ArtMasteryView>()).FirstOrDefault(value => value != null && value.ArtId == art.ArtId);
                var previous = old?.MasteryPoints ?? 0;
                if (art.MasteryPoints <= previous) continue;
                var oldLevel = M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(previous);
                var newLevel = M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(art.MasteryPoints);
                var name = string.IsNullOrWhiteSpace(art.DisplayName) ? "Learned Art" : art.DisplayName;
                return name + (newLevel > oldLevel ? "\nART LEVEL " + oldLevel + " → " + newLevel
                    : "\nMASTERY +" + (art.MasteryPoints - previous));
            }
            if (after.TotalPersonalXp > before.TotalPersonalXp)
                return "VETERAN TRAINING\n+" + (after.TotalPersonalXp - before.TotalPersonalXp) + " PERSONAL XP";
            return string.Empty; // Caller may show a verified consumed invitation at the existing level cap.
        }

        public static string AppliedWithoutDelta099 => "INVITATION APPLIED\nExisting progression caps retained";

        public static string CommittedInvitationSummary099(M1RecruitLoadoutView before, M1RecruitLoadoutView after)
        {
            if (before == null || after == null || string.IsNullOrEmpty(after.RecruitId) || before.RecruitId != after.RecruitId)
                return string.Empty;
            var growth = CommittedGrowth099(before, after);
            return string.IsNullOrEmpty(growth) ? AppliedWithoutDelta099 : growth;
        }
    }
}
