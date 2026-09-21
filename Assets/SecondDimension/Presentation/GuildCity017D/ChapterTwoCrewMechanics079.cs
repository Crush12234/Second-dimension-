using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017G;

namespace SecondDimension.Presentation.GuildCity017D
{
    public sealed class ChapterTwoCrewCandidate079
    {
        public string RecruitId { get; set; }
        public string DisplayName { get; set; }
        public string ObservedClass { get; set; }
    }

    public sealed class ChapterTwoCheckPreview079
    {
        public string LeadRecruitId { get; set; }
        public string AssistantRecruitId { get; set; }
        public int ApproachModifier { get; set; }
        public int LeadRoleBonus { get; set; }
        public int AssistantSynergyBonus { get; set; }
        public int CommandModifier { get; set; }
        public int ModeModifier { get; set; }
        public int EffectiveModifier { get; set; }
        public int SuccessChancePercent { get; set; }
        public int FullSuccessChancePercent { get; set; }
        public bool UsesCommitted2d6 { get; set; }
        public string GuaranteedOutcome { get; set; }
        public int EvidenceContribution { get; set; }
        public int TrustContribution { get; set; }
        public string CompactReadout { get; set; }
    }

    public sealed class ChapterTwoEvidenceStatus079
    {
        public bool IsVisible { get; set; }
        public int ReliableEvidenceCount { get; set; }
        public int PartialEvidenceCount { get; set; }
        public int OrraTrailCount { get; set; }
        public int OrraTrailTotal { get; set; }
        public bool OrraRescued { get; set; }
        public int RelationshipStrength { get; set; }
        public int TrustScore { get; set; }
        public string SellaStatus { get; set; }
        public string CrewTrustBand { get; set; }
        public string CompactReadout { get; set; }
    }

    /// <summary>
    /// Pure Chapter 2 crew projection. The command modifier is submitted through
    /// Guild City authority; the effective modifier adds the already-authoritative
    /// mode rule only for a truthful odds preview. Evidence and trust are read from
    /// committed objective flags and relationship memories, never local UI state.
    /// </summary>
    public static class ChapterTwoCrewMechanics079
    {
        public const string ChapterTwoBoardId079 = "BOARD_LINES_NOT_RETURNED";

        private static readonly string[] EvidenceEventIds079 =
        {
            "EVENT_FOUND_APPRENTICE",
            "EVENT_WRONG_ROUTE_MARKS",
            "EVENT_BROKEN_SURVEY_BRIDGE",
            "EVENT_CAMP_ARGUMENT",
            "EVENT_FOG_ECHO",
            "EVENT_BROKEN_ASTROLABE",
            "EVENT_ABANDONED_SURVEY_PACK"
        };

        public static bool IsChapterTwoBoard079(string boardId) =>
            StringComparer.Ordinal.Equals(boardId, ChapterTwoBoardId079);

        public static int RoleFitScore079(
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

        public static int RoleFitBonus079(
            string observedClass,
            IReadOnlyList<string> eligibleSkills) =>
            RoleFitScore079(observedClass, eligibleSkills) > 0 ? 1 : 0;

        public static ChapterTwoCrewCandidate079 BestAssistant079(
            ChapterTwoCrewCandidate079 lead,
            IReadOnlyList<ChapterTwoCrewCandidate079> candidates,
            IReadOnlyList<string> eligibleSkills)
        {
            if (lead == null) return null;
            return (candidates ?? Array.Empty<ChapterTwoCrewCandidate079>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.RecruitId) &&
                                !StringComparer.Ordinal.Equals(value.RecruitId, lead.RecruitId))
                .OrderByDescending(value => RoleFitScore079(value.ObservedClass, eligibleSkills))
                .ThenByDescending(value => !StringComparer.OrdinalIgnoreCase.Equals(
                    value.ObservedClass ?? string.Empty,
                    lead.ObservedClass ?? string.Empty))
                .ThenBy(value => value.DisplayName ?? value.RecruitId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static ChapterTwoCheckPreview079 BuildCheckPreview079(
            ChapterTwoCrewCandidate079 lead,
            ChapterTwoCrewCandidate079 assistant,
            IReadOnlyList<string> eligibleSkills,
            int approachIndex,
            string campaignModeId,
            bool usesCommitted2d6 = true,
            ChapterTwoEvidenceStatus079 evidenceStatus = null)
        {
            var partnered = approachIndex == 0 && assistant != null && lead != null &&
                             !StringComparer.Ordinal.Equals(assistant.RecruitId, lead.RecruitId);
            var approachModifier = partnered
                ? GuildCityExpeditionService017D.CarefulApproachModifier076
                : GuildCityExpeditionService017D.SwiftApproachModifier076;
            var leadBonus = RoleFitBonus079(lead?.ObservedClass, eligibleSkills);
            var assistantBonus = partnered
                ? RoleFitBonus079(assistant.ObservedClass, eligibleSkills)
                : 0;
            var commandModifier = checked(approachModifier + leadBonus + assistantBonus);
            var modeModifier = usesCommitted2d6 ? ModeCheckModifier079(campaignModeId) : 0;
            var effectiveModifier = checked(commandModifier + modeModifier);
            var successChance = ChanceAtOrAbove079(7, effectiveModifier);
            var fullSuccessChance = ChanceAtOrAbove079(10, effectiveModifier);
            var reliableEvidence = evidenceStatus?.ReliableEvidenceCount ?? 0;
            var partialEvidence = evidenceStatus?.PartialEvidenceCount ?? 0;
            var relationshipStrength = evidenceStatus?.RelationshipStrength ?? 0;
            var trustScore = evidenceStatus?.TrustScore ?? 0;
            var civicTrust = Math.Max(0, trustScore - relationshipStrength);
            var evidenceContribution = GuildCityExpeditionService017D
                .ChapterTwoEvidenceContribution079(reliableEvidence, partialEvidence);
            var trustContribution = GuildCityExpeditionService017D
                .ChapterTwoTrustContribution079(civicTrust, relationshipStrength);
            var guaranteedOutcome = usesCommitted2d6
                ? string.Empty
                : GuildCityExpeditionService017D.ChapterTwoAuthoredOutcome079(
                    leadBonus > 0,
                    assistantBonus > 0,
                    reliableEvidence,
                    partialEvidence,
                    civicTrust,
                    relationshipStrength);
            return new ChapterTwoCheckPreview079
            {
                LeadRecruitId = lead?.RecruitId ?? string.Empty,
                AssistantRecruitId = partnered ? assistant.RecruitId : string.Empty,
                ApproachModifier = approachModifier,
                LeadRoleBonus = leadBonus,
                AssistantSynergyBonus = assistantBonus,
                CommandModifier = commandModifier,
                ModeModifier = modeModifier,
                EffectiveModifier = effectiveModifier,
                SuccessChancePercent = successChance,
                FullSuccessChancePercent = fullSuccessChance,
                UsesCommitted2d6 = usesCommitted2d6,
                GuaranteedOutcome = guaranteedOutcome,
                EvidenceContribution = evidenceContribution,
                TrustContribution = trustContribution,
                CompactReadout = usesCommitted2d6
                    ? "LEAD FIT " + Signed079(leadBonus) +
                      "  •  PARTNER " + Signed079(assistantBonus) +
                      "  •  ORDER " + Signed079(commandModifier) +
                      (modeModifier == 0 ? string.Empty : "  •  MODE " + Signed079(modeModifier)) +
                      "\nSUCCESS " + successChance + "%  •  FULL SUCCESS " + fullSuccessChance + "%"
                    : "LEAD FIT " + Signed079(leadBonus > 0 ? 2 : 0) +
                      "  •  PARTNER " + Signed079(assistantBonus) +
                      "  •  EVIDENCE " + Signed079(evidenceContribution) +
                      "  •  TRUST " + Signed079(trustContribution) +
                      "\nPROMISED CONSEQUENCE  •  " +
                      HumanizeOutcome079(guaranteedOutcome)
            };
        }

        public static ChapterTwoEvidenceStatus079 BuildEvidenceStatus079(
            GuildCityPresentationState017D state)
        {
            var expedition = state?.Expedition;
            if (expedition == null || !IsChapterTwoBoard079(expedition.BoardId))
                return new ChapterTwoEvidenceStatus079
                {
                    IsVisible = false,
                    OrraTrailTotal = EvidenceEventIds079.Length - 1,
                    SellaStatus = "UNMET",
                    CrewTrustBand = "UNPROVEN",
                    CompactReadout = string.Empty
                };

            var flags = new HashSet<string>(
                expedition.ObjectiveFlags ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var reliable = 0;
            var partial = 0;
            var trail = 0;
            for (var index = 0; index < EvidenceEventIds079.Length; index++)
            {
                var eventId = EvidenceEventIds079[index];
                var resolved = flags.Contains("EVENT_RESOLVED_" + eventId);
                var successful = flags.Contains(GuildCityExpeditionService017D.EventSuccessFlag076(eventId));
                if (successful)
                {
                    reliable++;
                    if (index > 0) trail++;
                }
                else if (resolved)
                {
                    partial++;
                }
            }

            var relationshipStrength = (state.Relationships ??
                                        Array.Empty<GuildCityRelationshipView017D>())
                .Where(value => value != null)
                .Sum(value => Math.Max(0, value.Strength));
            var trustScore = Math.Max(0, state.CivicTrust) + relationshipStrength;
            var trustBand = trustScore >= 7
                ? "STRONG"
                : trustScore >= 3
                    ? "GROWING"
                    : trustScore > 0
                        ? "TESTED"
                        : "UNPROVEN";
            var sellaResolved = flags.Contains("EVENT_RESOLVED_EVENT_FOUND_APPRENTICE");
            var sellaTrusted = flags.Contains(
                GuildCityExpeditionService017D.EventSuccessFlag076("EVENT_FOUND_APPRENTICE"));
            var sella = sellaTrusted ? "TRUSTING" : sellaResolved ? "SAFE" : "WAITING";
            var orraRescued = flags.Contains("PRIMARY_OBJECTIVE_RESCUE_COMPLETE") ||
                              flags.Contains(
                                  GuildCityExpeditionService017D.EncounterClearedFlag("N13"));
            return new ChapterTwoEvidenceStatus079
            {
                IsVisible = true,
                ReliableEvidenceCount = reliable,
                PartialEvidenceCount = partial,
                OrraTrailCount = trail,
                OrraTrailTotal = EvidenceEventIds079.Length - 1,
                OrraRescued = orraRescued,
                RelationshipStrength = relationshipStrength,
                TrustScore = trustScore,
                SellaStatus = sella,
                CrewTrustBand = trustBand,
                CompactReadout =
                    "EVIDENCE  " + reliable + " RELIABLE  •  " + partial + " PARTIAL" +
                    "\nCREW TRUST  " + trustBand + "  •  SELLA " + sella +
                    (orraRescued
                        ? "  •  ORRA TRAIL SECURED"
                        : "  •  ORRA TRAIL " + trail + "/" +
                          (EvidenceEventIds079.Length - 1))
            };
        }

        public static string OutcomeAftermath079(GuildCityExpeditionView017D expedition)
        {
            if (expedition == null || !expedition.HasCommittedCheckAtCurrentNode)
                return string.Empty;
            var held = OutcomeHeld079(expedition.LastCheckOutcome);
            // Saves committed before the dedicated Chapter 1 camp event existed
            // still carry EVENT_CAMP_ARGUMENT on the first-story board. Keep their
            // visible aftermath on the Lantern Watch story during migration.
            if (StringComparer.Ordinal.Equals(
                    expedition.CurrentEventId,
                    "EVENT_CAMP_ARGUMENT") &&
                !IsChapterTwoBoard079(expedition.BoardId))
                return held
                    ? "Maren's Lantern Watch holds: Jazzi tends the wounded while the Unions agree how they will bring Zorin home."
                    : "Maren keeps the Lantern Watch together while Jazzi tends the strain; Zorin's rescue remains the shared order.";
            if (!string.IsNullOrWhiteSpace(expedition.CurrentEventOutcomeText))
                return expedition.CurrentEventOutcomeText.Trim();

            switch (expedition.CurrentEventId)
            {
                case "EVENT_FOUND_APPRENTICE":
                    return held
                        ? "Sella puts Orra's brass line-tag in the Guild's hands. Her seventh-return testimony makes the fresh marks evidence, not rumor."
                        : "Sella remains guarded, but the Guild gets her to safety and records the brass tag as partial evidence.";
                case "EVENT_WRONG_ROUTE_MARKS":
                    return held
                        ? "The crew proves the fresh paint is a forgery and carries Orra's original brass line forward."
                        : "The reversed stroke remains uncertain; both routes stay open while the crew marks the evidence incomplete.";
                case "EVENT_BROKEN_SURVEY_BRIDGE":
                    return held
                        ? "Orra's missing fifth pin proves the bridge was altered. The crew crosses with sabotage evidence intact."
                        : "The bridge costs time and gear, but the empty fifth socket survives as partial sabotage evidence.";
                case "EVENT_CAMP_ARGUMENT":
                    return held
                        ? "Sella names Orra and the erased seventh-return mark. The Unions leave camp trusting the same warning."
                        : "The crew preserves the camp, but Sella's warning remains incomplete and trust must be rebuilt on the road.";
                case "EVENT_FOG_ECHO":
                    return held
                        ? "The seventh-return phrase breaks the false voice and exposes the Route Mutilator's hidden line."
                        : "The imitation keeps part of its secret; the crew falls back to Sella's last true mark without losing anyone.";
                case "EVENT_BROKEN_ASTROLABE":
                    return held
                        ? "The repaired needle points beneath Skyhome and gives Orra's field-book route a trustworthy bearing."
                        : "The lens proves only that the road continues below. The safe descent and field-book route both remain legal.";
                case "EVENT_ABANDONED_SURVEY_PACK":
                    return held
                        ? "Orra's field book adds the missing headcount, door sketch, and rescue line to the Guild record."
                        : "The pack stays exposed, but its location and the surveyors' rescue route remain recorded for a later recovery.";
                case "EVENT_INJURED_COURIER":
                    return held
                        ? "Una steadies enough to match her ledger to the Lantern trail; the crew now knows who passed this way."
                        : "Una is safe, but her broken account leaves the next trail uncertain and the crew carrying extra strain.";
                case "EVENT_COLLAPSED_HANDRAIL":
                    return held
                        ? "Quin's rescue line holds. The crossing becomes a shared proof that this Union protects people before speed."
                        : "Quin clears the span, but the failed first brace costs the crew strength before the next road.";
                case "EVENT_TRAPPED_FOREMAN":
                    return held
                        ? "Petra is free, and Zorin's patrol turns the rescue line into better ground for the coming fight."
                        : "Petra survives the failed lift; the patrol regroups without the position advantage it hoped to win.";
                case "EVENT_GATEGLASS_PULSE":
                    return held
                        ? "The Wayglass pulse settles into a readable service line, giving the crew a safer flank."
                        : "The Wayglass remains unstable, but its dangerous cadence is recorded before the crew advances.";
                case "EVENT_GARA_WOUNDED_LANTERN_PASSAGE":
                    return held
                        ? "Gara brings every wounded Lantern through and seals the exposed road behind them."
                        : "Gara gets every wounded Lantern through, but the broken passage leaves no safe retreat.";
                case "EVENT_GARA_SURVEY_CREW_DESCENT":
                    return held
                        ? "Gara lowers Sella and the wounded survey hands toward Orra's bell before the fog closes."
                        : "Gara saves every survey hand, but the failed cradle leaves the rescue party below without an easy return.";
                default:
                    return held
                        ? "The crew's order holds. Its evidence and relationship memory are committed to the route."
                        : "The order bends without breaking; the crew keeps a legal route and records what remains unresolved.";
            }
        }

        public static string SelectOutcomeCopy079(
            string outcome,
            string exceptional,
            string fullSuccess,
            string successWithCost,
            string setback,
            string severeSetback)
        {
            switch (outcome)
            {
                case "EXCEPTIONAL": return exceptional ?? string.Empty;
                case "FULL_SUCCESS": return fullSuccess ?? string.Empty;
                case "SUCCESS_WITH_COST": return successWithCost ?? string.Empty;
                case "SETBACK": return setback ?? string.Empty;
                case "SEVERE_SETBACK": return severeSetback ?? string.Empty;
                default: return string.Empty;
            }
        }

        private static bool OutcomeHeld079(string outcome) =>
            StringComparer.Ordinal.Equals(outcome, "EXCEPTIONAL") ||
            StringComparer.Ordinal.Equals(outcome, "FULL_SUCCESS") ||
            StringComparer.Ordinal.Equals(outcome, "SUCCESS_WITH_COST");

        private static int ModeCheckModifier079(string campaignModeId)
        {
            if (!Enum.TryParse(campaignModeId ?? string.Empty, true, out GameMode mode))
                mode = GameMode.Standard;
            return GuildCityOpeningBalance017G.For(mode).CheckModifier;
        }

        private static int ChanceAtOrAbove079(int targetTotal, int modifier)
        {
            var successful = 0;
            for (var one = 1; one <= 6; one++)
            for (var two = 1; two <= 6; two++)
                if (one + two + modifier >= targetTotal) successful++;
            return (int)Math.Round(successful * 100d / 36d, MidpointRounding.AwayFromZero);
        }

        private static string HumanizeOutcome079(string outcome) =>
            (outcome ?? string.Empty).Replace('_', ' ');

        private static string Signed079(int value) =>
            value >= 0 ? "+" + value : value.ToString();
    }
}
