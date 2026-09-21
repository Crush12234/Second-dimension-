using System;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.RecruitChronicles025
{
    public interface IRecruitChronicleBoardAuthority025
    {
        bool TryGetCanonicalBoard(string boardId, out PersonalQuestBoardDefinition025 board);
        bool RecruitOwnsBoard(string authoredRecruitId, string boardId);
    }

    public static class RecruitChronicleBoardAuthorityRules025
    {
        public static bool TryProjectCanonicalBoard(
            CampaignState campaign,
            IRecruitChronicleBoardAuthority025 authority,
            string boardId,
            string runtimeRecruitId,
            out PersonalQuestBoardDefinition025 projected)
        {
            projected = null;
            if (authority == null || string.IsNullOrWhiteSpace(boardId) ||
                string.IsNullOrWhiteSpace(runtimeRecruitId) ||
                !authority.TryGetCanonicalBoard(boardId, out var canonical) || canonical == null ||
                !authority.RecruitOwnsBoard(canonical.recruitId, canonical.boardId))
                return false;
            var runtimeRecruit = (campaign?.Guild?.Recruits ?? Array.Empty<RecruitState>()).FirstOrDefault(value =>
                value.AuthorityKind == RecruitAuthorityKind.Normal &&
                StringComparer.Ordinal.Equals(value.RecruitId, runtimeRecruitId));
            if (runtimeRecruit == null || !MatchesAuthoredIdentity(runtimeRecruit, canonical.recruitId))
                return false;
            projected = ProjectRecruitIdentity(canonical, runtimeRecruitId);
            return true;
        }

        public static bool TryValidate(
            CampaignState campaign,
            IRecruitChronicleBoardAuthority025 authority,
            PersonalQuestBoardDefinition025 supplied,
            out PersonalQuestBoardDefinition025 canonical,
            out string error)
        {
            canonical = null;
            error = string.Empty;
            if (authority == null)
            {
                error = "CHRONICLE025_BOARD_AUTHORITY_REQUIRED";
                return false;
            }
            if (supplied == null || string.IsNullOrWhiteSpace(supplied.boardId) ||
                !authority.TryGetCanonicalBoard(supplied.boardId, out canonical) || canonical == null)
            {
                error = "CHRONICLE025_UNKNOWN_BOARD";
                return false;
            }
            if (!authority.RecruitOwnsBoard(canonical.recruitId, canonical.boardId))
            {
                error = "CHRONICLE025_BOARD_PROFILE_MISMATCH";
                return false;
            }
            if (!TryProjectCanonicalBoard(campaign, authority, supplied.boardId, supplied.recruitId,
                    out var expected))
            {
                error = "CHRONICLE025_BOARD_RECRUIT_MISMATCH";
                return false;
            }
            if (!StringComparer.Ordinal.Equals(CanonicalJson.Sha256Hex(expected), CanonicalJson.Sha256Hex(supplied)))
            {
                error = "CHRONICLE025_BOARD_CONTENT_MISMATCH";
                return false;
            }
            return true;
        }

        static bool MatchesAuthoredIdentity(RecruitState runtime, string authoredRecruitId) =>
            StringComparer.Ordinal.Equals(runtime.RecruitId, authoredRecruitId) ||
            StringComparer.Ordinal.Equals(runtime.AuthoredStableRecruitId, authoredRecruitId) ||
            StringComparer.Ordinal.Equals(runtime.TutorialAliasId, authoredRecruitId) ||
            StringComparer.Ordinal.Equals(runtime.SignatureId, authoredRecruitId);

        static PersonalQuestBoardDefinition025 ProjectRecruitIdentity(
            PersonalQuestBoardDefinition025 canonical,
            string runtimeRecruitId) =>
            new PersonalQuestBoardDefinition025
            {
                boardId = canonical.boardId,
                recruitId = runtimeRecruitId,
                displayName = canonical.displayName,
                theme = canonical.theme,
                entryNodeId = canonical.entryNodeId,
                nodes = canonical.nodes,
                deterministic = canonical.deterministic,
                reloadCannotReroll = canonical.reloadCannotReroll,
                failureRecoverable = canonical.failureRecoverable,
                canCauseDeparture = canonical.canCauseDeparture
            };
    }
}
