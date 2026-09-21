using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Immutable presentation snapshot for one Union's tactical-overview position.
    /// Rows and columns are zero-based; SideOrdinal is one-based for player-facing UI.
    /// </summary>
    public sealed class BattleUnionDeploymentSlot020
    {
        internal BattleUnionDeploymentSlot020(
            string unionId,
            string displayName,
            bool enemy,
            int sideOrdinal,
            int row,
            int column,
            Vector3 anchor,
            float facingYaw,
            float overviewScale)
        {
            UnionId = unionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Enemy = enemy;
            SideOrdinal = sideOrdinal;
            Row = row;
            Column = column;
            Anchor = anchor;
            FacingYaw = facingYaw;
            OverviewScale = overviewScale;
        }

        public string UnionId { get; }
        public bool Enemy { get; }
        public int SideOrdinal { get; }
        public int Row { get; }
        public int Column { get; }
        public Vector3 Anchor { get; }
        public float FacingYaw { get; }
        public float OverviewScale { get; }
        public string DisplayName { get; }

        public string StableDescriptor => string.Join("|", new[]
        {
            UnionId,
            DisplayName,
            Enemy ? "ENEMY" : "PLAYER",
            SideOrdinal.ToString(CultureInfo.InvariantCulture),
            Row.ToString(CultureInfo.InvariantCulture),
            Column.ToString(CultureInfo.InvariantCulture),
            StableFloat(Anchor.x),
            StableFloat(Anchor.y),
            StableFloat(Anchor.z),
            StableFloat(FacingYaw),
            StableFloat(OverviewScale)
        });

        private static string StableFloat(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Immutable local contact positions derived from two deployment slots. These are
    /// presentation coordinates only and do not decide engagement, targeting, or results.
    /// </summary>
    public sealed class BattleUnionEngagementPocket020
    {
        internal BattleUnionEngagementPocket020(
            string playerUnionId,
            string enemyUnionId,
            Vector3 center,
            Vector3 playerContactAnchor,
            Vector3 enemyContactAnchor,
            float halfSeparation)
        {
            PlayerUnionId = playerUnionId ?? string.Empty;
            EnemyUnionId = enemyUnionId ?? string.Empty;
            Center = center;
            PlayerContactAnchor = playerContactAnchor;
            EnemyContactAnchor = enemyContactAnchor;
            HalfSeparation = halfSeparation;
        }

        public string PlayerUnionId { get; }
        public string EnemyUnionId { get; }
        public Vector3 Center { get; }
        public Vector3 PlayerContactAnchor { get; }
        public Vector3 EnemyContactAnchor { get; }
        public float HalfSeparation { get; }
        public float PlayerFacingYaw => BattleUnionDeploymentPlanning020.PlayerFacingYaw;
        public float EnemyFacingYaw => BattleUnionDeploymentPlanning020.EnemyFacingYaw;
    }

    /// <summary>
    /// Read-only result of mapping authoritative battle-view Unions into the twenty
    /// presentation slots. Capacity overflow remains explicit instead of disappearing.
    /// </summary>
    public sealed class BattleUnionDeploymentPlan020
    {
        private readonly Dictionary<string, BattleUnionDeploymentSlot020> _slotsByUnionId;

        internal BattleUnionDeploymentPlan020(
            IList<BattleUnionDeploymentSlot020> slots,
            int playerOverflowCount,
            int enemyOverflowCount)
        {
            var snapshot = slots == null
                ? new List<BattleUnionDeploymentSlot020>()
                : new List<BattleUnionDeploymentSlot020>(slots);

            Slots = snapshot.AsReadOnly();
            PlayerOverflowCount = Math.Max(0, playerOverflowCount);
            EnemyOverflowCount = Math.Max(0, enemyOverflowCount);
            _slotsByUnionId = new Dictionary<string, BattleUnionDeploymentSlot020>(StringComparer.Ordinal);

            for (var index = 0; index < snapshot.Count; index++)
            {
                var slot = snapshot[index];
                if (slot == null || string.IsNullOrWhiteSpace(slot.UnionId) ||
                    _slotsByUnionId.ContainsKey(slot.UnionId))
                    continue;
                _slotsByUnionId.Add(slot.UnionId, slot);
            }
        }

        public IReadOnlyList<BattleUnionDeploymentSlot020> Slots { get; }
        public int PlayerOverflowCount { get; }
        public int EnemyOverflowCount { get; }

        public bool TryGetSlot(string unionId, out BattleUnionDeploymentSlot020 slot)
        {
            if (string.IsNullOrWhiteSpace(unionId))
            {
                slot = null;
                return false;
            }
            return _slotsByUnionId.TryGetValue(unionId, out slot);
        }
    }

    /// <summary>
    /// Deterministic, presentation-only deployment policy for a maximum of ten Unions
    /// on each side. It reads M2BattleView but never changes battle or Union state.
    /// </summary>
    public static class BattleUnionDeploymentPlanning020
    {
        public const int ColumnCount = 2;
        public const int RowsPerColumn = 5;
        public const int MaxSlotsPerSide = ColumnCount * RowsPerColumn;
        public const int TotalSlotCapacity = MaxSlotsPerSide * 2;

        public const float FrontColumnDistance = 8f;
        public const float RearColumnDistance = 13f;
        public const float RowSpacing = 4.6f;
        public const float ArenaRadius = 18f;
        public const float TacticalOverviewScale = 0.72f;
        public const float PlayerFacingYaw = 90f;
        public const float EnemyFacingYaw = -90f;

        public const float DefaultEngagementHalfSeparation = 3.6f;
        public const float MinimumEngagementHalfSeparation = 2.4f;
        public const float MaximumEngagementHalfSeparation = 6.5f;

        public static BattleUnionDeploymentPlan020 Plan(M2BattleView battle)
        {
            if (battle == null)
                return new BattleUnionDeploymentPlan020(
                    new List<BattleUnionDeploymentSlot020>(), 0, 0);

            var slots = new List<BattleUnionDeploymentSlot020>(TotalSlotCapacity);
            AddSide(slots, battle.PlayerUnions, false);
            AddSide(slots, battle.EnemyUnions, true);

            return new BattleUnionDeploymentPlan020(
                slots,
                OverflowCount(battle.PlayerUnions),
                OverflowCount(battle.EnemyUnions));
        }

        /// <summary>
        /// Produces a neutral contact pocket midway between the source rows. The overload
        /// accepting halfSeparation lets the 3D world supply renderer-bounds clearance.
        /// </summary>
        public static bool TryCreateEngagementPocket(
            BattleUnionDeploymentSlot020 first,
            BattleUnionDeploymentSlot020 second,
            out BattleUnionEngagementPocket020 pocket)
        {
            return TryCreateEngagementPocket(
                first, second, DefaultEngagementHalfSeparation, out pocket);
        }

        public static bool TryCreateEngagementPocket(
            BattleUnionDeploymentSlot020 first,
            BattleUnionDeploymentSlot020 second,
            float halfSeparation,
            out BattleUnionEngagementPocket020 pocket)
        {
            pocket = null;
            if (first == null || second == null || first.Enemy == second.Enemy) return false;

            var player = first.Enemy ? second : first;
            var enemy = first.Enemy ? first : second;
            var separation = Mathf.Clamp(
                halfSeparation,
                MinimumEngagementHalfSeparation,
                MaximumEngagementHalfSeparation);
            var rowLimit = (RowsPerColumn - 1) * RowSpacing * 0.5f;
            var centerZ = Mathf.Clamp(
                (player.Anchor.z + enemy.Anchor.z) * 0.5f,
                -rowLimit,
                rowLimit);
            var center = new Vector3(0f, 0f, centerZ);

            pocket = new BattleUnionEngagementPocket020(
                player.UnionId,
                enemy.UnionId,
                center,
                center + Vector3.left * separation,
                center + Vector3.right * separation,
                separation);
            return true;
        }

        private static void AddSide(
            ICollection<BattleUnionDeploymentSlot020> destination,
            IReadOnlyList<M2BattleUnionView> unions,
            bool enemy)
        {
            if (unions == null) return;
            var count = Math.Min(MaxSlotsPerSide, unions.Count);
            for (var index = 0; index < count; index++)
            {
                var sideOrdinal = index + 1;
                var column = index / RowsPerColumn;
                var row = index % RowsPerColumn;
                var distance = column == 0 ? FrontColumnDistance : RearColumnDistance;
                var union = unions[index];
                var unionId = union?.UnionId ?? string.Empty;
                var displayName = string.IsNullOrWhiteSpace(union?.DisplayName)
                    ? (enemy ? "Enemy Union " : "Player Union ") + sideOrdinal
                    : union.DisplayName.Trim();

                destination.Add(new BattleUnionDeploymentSlot020(
                    unionId,
                    displayName,
                    enemy,
                    sideOrdinal,
                    row,
                    column,
                    new Vector3(
                        enemy ? distance : -distance,
                        0f,
                        (row - 2) * RowSpacing),
                    enemy ? EnemyFacingYaw : PlayerFacingYaw,
                    TacticalOverviewScale));
            }
        }

        private static int OverflowCount(IReadOnlyList<M2BattleUnionView> unions) =>
            unions == null ? 0 : Math.Max(0, unions.Count - MaxSlotsPerSide);
    }
}
