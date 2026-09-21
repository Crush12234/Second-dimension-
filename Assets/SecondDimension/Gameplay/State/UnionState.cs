using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.State
{
    public enum UnionKind
    {
        Normal,
        ProtectedSpecial
    }

    [Serializable]
    public sealed class UnionState
    {
        public UnionState(
            string unionId,
            string displayName,
            UnionKind kind,
            string leaderRecruitId,
            IReadOnlyList<string> memberRecruitIds,
            string formationId,
            string doctrineId,
            int sharedAp,
            int cohesionBasisPoints)
            : this(
                unionId,
                displayName,
                kind,
                leaderRecruitId,
                memberRecruitIds,
                formationId,
                doctrineId,
                sharedAp,
                cohesionBasisPoints,
                CreateDefaultPositions(memberRecruitIds))
        {
        }

        [JsonConstructor]
        public UnionState(
            string unionId,
            string displayName,
            UnionKind kind,
            string leaderRecruitId,
            IReadOnlyList<string> memberRecruitIds,
            string formationId,
            string doctrineId,
            int sharedAp,
            int cohesionBasisPoints,
            IReadOnlyList<UnionMemberPositionState> memberPositions)
        {
            UnionId = Require(unionId, nameof(unionId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? UnionId : displayName;
            Kind = kind;
            LeaderRecruitId = leaderRecruitId;
            MemberRecruitIds = Copy(memberRecruitIds);
            MemberPositions = CopyPositions(memberPositions, MemberRecruitIds);
            FormationId = formationId;
            DoctrineId = doctrineId;
            if (sharedAp < 0) throw new ArgumentOutOfRangeException(nameof(sharedAp));
            if (cohesionBasisPoints < 0 || cohesionBasisPoints > 10_000) throw new ArgumentOutOfRangeException(nameof(cohesionBasisPoints));
            SharedAp = sharedAp;
            CohesionBasisPoints = cohesionBasisPoints;
        }

        public string UnionId { get; }
        public string DisplayName { get; }
        public UnionKind Kind { get; }
        public string LeaderRecruitId { get; }
        public IReadOnlyList<string> MemberRecruitIds { get; }
        public IReadOnlyList<UnionMemberPositionState> MemberPositions { get; }
        public string FormationId { get; }
        public string DoctrineId { get; }
        public int SharedAp { get; }
        public int CohesionBasisPoints { get; }

        private static IReadOnlyList<string> Copy(IReadOnlyList<string> values)
        {
            var copy = new List<string>();
            if (values != null)
            {
                foreach (var value in values) copy.Add(Require(value, nameof(values)));
            }
            return copy.AsReadOnly();
        }

        private static IReadOnlyList<UnionMemberPositionState> CreateDefaultPositions(IReadOnlyList<string> members)
        {
            var positions = new List<UnionMemberPositionState>();
            if (members != null)
            {
                for (var index = 0; index < members.Count; index++)
                {
                    positions.Add(new UnionMemberPositionState(index, "POSITION_" + (index + 1), members[index]));
                }
            }
            return positions.AsReadOnly();
        }

        private static IReadOnlyList<UnionMemberPositionState> CopyPositions(
            IReadOnlyList<UnionMemberPositionState> positions,
            IReadOnlyList<string> members)
        {
            var source = positions ?? CreateDefaultPositions(members);
            if (source.Count != members.Count) throw new ArgumentException("Union positions must match members.", nameof(positions));
            var copy = new List<UnionMemberPositionState>();
            for (var index = 0; index < source.Count; index++)
            {
                var position = source[index] ?? throw new ArgumentException("Union position cannot be null.", nameof(positions));
                if (position.SlotIndex != index || !StringComparer.Ordinal.Equals(position.RecruitId, members[index]))
                {
                    throw new ArgumentException("Union position order must exactly match member order.", nameof(positions));
                }
                copy.Add(position);
            }
            return copy.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class UnionMemberPositionState
    {
        [JsonConstructor]
        public UnionMemberPositionState(int slotIndex, string positionId, string recruitId)
        {
            if (slotIndex < 0) throw new ArgumentOutOfRangeException(nameof(slotIndex));
            SlotIndex = slotIndex;
            PositionId = Require(positionId, nameof(positionId));
            RecruitId = Require(recruitId, nameof(recruitId));
        }

        public int SlotIndex { get; }
        public string PositionId { get; }
        public string RecruitId { get; }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }
}
