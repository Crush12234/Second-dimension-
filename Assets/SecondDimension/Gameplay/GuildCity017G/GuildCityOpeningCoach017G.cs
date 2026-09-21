
using System;
using System.Collections.Generic;

namespace SecondDimension.Gameplay.GuildCity017G
{
    public enum GuildCityGuideDestination017G
    {
        Hall,
        Applicants,
        Equipment,
        Unions,
        City,
        Contracts,
        Expedition,
        Relationships,
        Battle,
        Results,
        Complete
    }

    public sealed class GuildCityOpeningSnapshot017G
    {
        public int OperationOrdinal { get; set; }
        public bool HasRecruitmentBoard { get; set; }
        public int SignedApplicantCount { get; set; }
        public int TotalRecruitCount { get; set; }
        public int EquippedRecruitCount { get; set; }
        public int NormalUnionCount { get; set; }
        public int PlacedBuildingCount { get; set; }
        public int StaffedBuildingCount { get; set; }
        public int UpgradedBuildingCount { get; set; }
        public int AdjacencyBonusCount { get; set; }
        public bool HasActiveContract { get; set; }
        public bool HasExpedition { get; set; }
        public int ExpeditionVisitedNodeCount { get; set; }
        public int CommittedCheckCount { get; set; }
        public bool CanCommitEncounter { get; set; }
        public bool HasPendingEncounter { get; set; }
        public bool HasActiveCertifiedBattle { get; set; }
        public bool HasUnclaimedBattleReward { get; set; }
        public int ClaimedBattleRewardCount { get; set; }
        public bool CanFinalizeOperation { get; set; }
        public int RelationshipCount { get; set; }
        public int UnviewedRelationshipCount { get; set; }
    }

    public sealed class GuildCityGuideStep017G
    {
        public GuildCityGuideStep017G(string id, string title, string summary, string action,
            GuildCityGuideDestination017G destination, int priority, bool completed)
        {
            Id = Require(id, nameof(id));
            Title = Require(title, nameof(title));
            Summary = summary ?? string.Empty;
            Action = action ?? string.Empty;
            Destination = destination;
            Priority = priority;
            Completed = completed;
        }

        public string Id { get; }
        public string Title { get; }
        public string Summary { get; }
        public string Action { get; }
        public GuildCityGuideDestination017G Destination { get; }
        public int Priority { get; }
        public bool Completed { get; }

        private static string Require(string value, string name) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value;
    }

    public static class GuildCityOpeningCoach017G
    {
        public static IReadOnlyList<GuildCityGuideStep017G> Evaluate(GuildCityOpeningSnapshot017G snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var result = new List<GuildCityGuideStep017G>();
            Add(result, "GUIDE_OPEN_APPLICANTS", "Open the Applicant Board",
                "Commit the recurring board before reveal so applicants cannot reroll.",
                "Open Applicants", GuildCityGuideDestination017G.Applicants, 100, snapshot.HasRecruitmentBoard);
            Add(result, "GUIDE_SIGN_RECRUIT", "Sign a permanent recruit",
                "Choose one applicant. Signing creates their permanent identity, weapon family, Arts, and visual seed exactly once.",
                "Review and sign", GuildCityGuideDestination017G.Applicants, 95,
                snapshot.SignedApplicantCount > 0 || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_EQUIP_TEAM", "Equip the opening team",
                "Every normal recruit is manually equipable. Check main hand, armor, and legal Art changes.",
                "Open Equipment", GuildCityGuideDestination017G.Equipment, 90,
                snapshot.TotalRecruitCount > 0 && snapshot.EquippedRecruitCount >= Math.Min(3, snapshot.TotalRecruitCount));
            Add(result, "GUIDE_FORM_UNIONS", "Create two legal Unions",
                "Build two three-member Unions so command forecasts can demonstrate mixed weapon, Mystic, healing, and Guard roles.",
                "Open Union Builder", GuildCityGuideDestination017G.Unions, 85, snapshot.NormalUnionCount >= 2);
            Add(result, "GUIDE_FIRST_BUILDING", "Place the first facility",
                "Spend earned city resources on a visible building that immediately changes Guild operations.",
                "Open City", GuildCityGuideDestination017G.City, 80, snapshot.PlacedBuildingCount >= 1);
            Add(result, "GUIDE_STAFF_FACILITY", "Assign a facility staff member",
                "Staff remain permanent and deployable; staffing improves the facility without taking ownership away.",
                "Assign Staff", GuildCityGuideDestination017G.City, 75, snapshot.StaffedBuildingCount >= 1);
            Add(result, "GUIDE_ACCEPT_CONTRACT", "Accept a detailed contract",
                "Compare objective, hazards, route pressure, skills, rewards, and city consequences before committing.",
                "Open Contracts", GuildCityGuideDestination017G.Contracts, 70,
                snapshot.HasActiveContract || snapshot.HasExpedition || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_START_EXPEDITION", "Deploy to the expedition board",
                "Supplies, scouting, fatigue, urgency, and route choice shape the encounter without becoming combat movement.",
                "Begin Expedition", GuildCityGuideDestination017G.Expedition, 65,
                snapshot.HasExpedition || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_EXPLORE_BOARD", "Make meaningful route choices",
                "Reveal routes, resolve events, use camps, and decide whether optional danger is worth the reward.",
                "Resume Expedition", GuildCityGuideDestination017G.Expedition, 60,
                snapshot.ExpeditionVisitedNodeCount >= 3 || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_COMMIT_CHECK", "Resolve a committed 2d6 check",
                "Choose the acting recruit and assistant. The committed result cannot reroll on reload.",
                "Resolve Current Node", GuildCityGuideDestination017G.Expedition, 55,
                snapshot.CommittedCheckCount >= 1 || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_ENTER_BATTLE", "Enter the certified Union battle",
                "Commit the encounter, preserve the exact return checkpoint, and choose complete contextual Union Forecasts.",
                "Enter Battle", GuildCityGuideDestination017G.Battle, 50,
                snapshot.HasActiveCertifiedBattle || snapshot.HasUnclaimedBattleReward ||
                snapshot.ClaimedBattleRewardCount > 0 || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_CLAIM_REWARD", "Claim the deterministic equipment reward",
                "The exact reward was committed before presentation. Claim it once; the game will never auto-equip it.",
                "Complete Battle / Claim Reward", GuildCityGuideDestination017G.Results, 45,
                snapshot.ClaimedBattleRewardCount > 0 || snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_FINALIZE_OPERATION", "Finish the operation",
                "Return to the exact board state, complete or extract, then apply growth, materials, relationships, and city progress.",
                "Finalize Operation", GuildCityGuideDestination017G.Expedition, 40, snapshot.OperationOrdinal > 0);
            Add(result, "GUIDE_RELATIONSHIP_SCENE", "View a free relationship scene",
                "Relationship scenes come from shared play, cost no operation, never expire, and cannot make a recruit leave.",
                "Open Relationships", GuildCityGuideDestination017G.Relationships, 35,
                snapshot.RelationshipCount > 0 && snapshot.UnviewedRelationshipCount == 0);
            Add(result, "GUIDE_SECOND_CITY_GROWTH", "Create the first city synergy",
                "Place a second facility or upgrade one, then activate an adjacency or district bonus.",
                "Continue Building", GuildCityGuideDestination017G.City, 30,
                snapshot.PlacedBuildingCount >= 2 || snapshot.UpgradedBuildingCount > 0 || snapshot.AdjacencyBonusCount > 0);
            return result.AsReadOnly();
        }

        public static GuildCityGuideStep017G Current(GuildCityOpeningSnapshot017G snapshot)
        {
            var steps = Evaluate(snapshot);
            for (var index = 0; index < steps.Count; index++) if (!steps[index].Completed) return steps[index];
            return new GuildCityGuideStep017G("GUIDE_OPENING_COMPLETE", "Opening operation ready",
                "The complete Guild, city, expedition, battle, reward, relationship, and persistence loop is established.",
                "Continue the Guild", GuildCityGuideDestination017G.Complete, 0, true);
        }

        public static int ProgressPercent(GuildCityOpeningSnapshot017G snapshot)
        {
            var steps = Evaluate(snapshot);
            var complete = 0;
            for (var index = 0; index < steps.Count; index++) if (steps[index].Completed) complete++;
            return steps.Count == 0 ? 100 : (int)Math.Round(complete * 100.0 / steps.Count);
        }

        private static void Add(List<GuildCityGuideStep017G> target, string id, string title,
            string summary, string action, GuildCityGuideDestination017G destination,
            int priority, bool completed) =>
            target.Add(new GuildCityGuideStep017G(id, title, summary, action, destination, priority, completed));
    }
}
