using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only chapter marker for an already-authoritative battle event stream.
    /// It never reorders events and never decides who acts.
    /// </summary>
    public sealed class BattleUnionTurnPhase019
    {
        public BattleUnionTurnPhase019(
            int firstEventIndex,
            string unionId,
            string displayName,
            bool player,
            int playerOrdinal,
            int playerTurnCount,
            string commandName,
            string formation,
            string engagement)
        {
            FirstEventIndex = firstEventIndex;
            UnionId = unionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsPlayer = player;
            PlayerOrdinal = playerOrdinal;
            PlayerTurnCount = playerTurnCount;
            CommandName = commandName ?? string.Empty;
            Formation = formation ?? string.Empty;
            Engagement = engagement ?? string.Empty;
        }

        public int FirstEventIndex { get; }
        public string UnionId { get; }
        public string DisplayName { get; }
        public bool IsPlayer { get; }
        public int PlayerOrdinal { get; }
        public int PlayerTurnCount { get; }
        public string CommandName { get; }
        public string Formation { get; }
        public string Engagement { get; }

        public string TurnBadge => IsPlayer
            ? "UNION " + PlayerOrdinal + " OF " + PlayerTurnCount
            : "ENEMY RESPONSE";

        public string TurnTitle => IsPlayer
            ? DisplayName.ToUpperInvariant() + "'S TURN"
            : DisplayName.ToUpperInvariant() + " ATTACKS";

        public string OrderLine => IsPlayer
            ? "ORDER · " + (string.IsNullOrWhiteSpace(CommandName) ? "COMMITTED UNION COMMAND" : CommandName.ToUpperInvariant())
            : "ENEMY UNION IN MOTION";

        public string TacticalLine
        {
            get
            {
                var formation = string.IsNullOrWhiteSpace(Formation) ? "FORMATION READY" : Formation.ToUpperInvariant();
                var engagement = string.IsNullOrWhiteSpace(Engagement) ? "OPEN" : Engagement.ToUpperInvariant();
                return formation + "  ·  " + engagement;
            }
        }

        public string StableDescriptor => string.Join("|", new[]
        {
            FirstEventIndex.ToString(), UnionId, DisplayName, IsPlayer ? "PLAYER" : "ENEMY",
            PlayerOrdinal.ToString(), PlayerTurnCount.ToString(), CommandName, Formation, Engagement
        });
    }

    /// <summary>
    /// Finds visible Union chapters without changing the source event order. Player
    /// chapters may only begin at authoritative FORECAST_COMMITTED events. The enemy
    /// response begins at the first raw event whose authoritative actor is an enemy
    /// Union; caption text and visually-swapped interception beats are never consulted.
    /// </summary>
    public static class BattleUnionTurnPlanner019
    {
        public static IReadOnlyList<BattleUnionTurnPhase019> Plan(
            M2BattleView battle,
            IReadOnlyList<M2BattleEventView> events)
        {
            var result = new List<BattleUnionTurnPhase019>();
            if (battle == null || events == null) return result.AsReadOnly();

            var players = IndexUnions(battle.PlayerUnions);
            var enemies = IndexUnions(battle.EnemyUnions);
            var committedPlayerIds = CommittedPlayerUnionIds(events, players);
            var seenPlayers = new HashSet<string>(StringComparer.Ordinal);
            var seenEnemies = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < events.Count; index++)
            {
                var item = events[index];
                if (item == null) continue;
                var actorUnionId = string.IsNullOrWhiteSpace(item.ActorUnionId)
                    ? item.UnionId ?? string.Empty
                    : item.ActorUnionId;

                if (IsType(item, "FORECAST_COMMITTED") &&
                    players.TryGetValue(actorUnionId, out var playerUnion) &&
                    seenPlayers.Add(actorUnionId))
                {
                    var ordinal = committedPlayerIds.IndexOf(actorUnionId) + 1;
                    result.Add(new BattleUnionTurnPhase019(
                        index,
                        actorUnionId,
                        PlayerFacingName(playerUnion, ordinal, true),
                        true,
                        Math.Max(1, ordinal),
                        Math.Max(1, committedPlayerIds.Count),
                        SelectedCommandName(battle, playerUnion, item.ArtId),
                        playerUnion.Formation,
                        playerUnion.Engagement));
                    continue;
                }

                if (IsEnemyAction(item) &&
                    enemies.TryGetValue(actorUnionId, out var enemyUnion) &&
                    seenEnemies.Add(actorUnionId))
                {
                    result.Add(new BattleUnionTurnPhase019(
                        index,
                        actorUnionId,
                        PlayerFacingName(enemyUnion, 0, false),
                        false,
                        0,
                        committedPlayerIds.Count,
                        string.Empty,
                        enemyUnion.Formation,
                        enemyUnion.Engagement));
                }
            }

            return result.AsReadOnly();
        }

        private static Dictionary<string, M2BattleUnionView> IndexUnions(
            IReadOnlyList<M2BattleUnionView> unions)
        {
            var result = new Dictionary<string, M2BattleUnionView>(StringComparer.Ordinal);
            if (unions == null) return result;
            for (var index = 0; index < unions.Count; index++)
            {
                var union = unions[index];
                if (union == null || string.IsNullOrWhiteSpace(union.UnionId) || result.ContainsKey(union.UnionId))
                    continue;
                result.Add(union.UnionId, union);
            }
            return result;
        }

        private static List<string> CommittedPlayerUnionIds(
            IReadOnlyList<M2BattleEventView> events,
            IDictionary<string, M2BattleUnionView> players)
        {
            var result = new List<string>();
            for (var index = 0; index < events.Count; index++)
            {
                var item = events[index];
                if (!IsType(item, "FORECAST_COMMITTED")) continue;
                var unionId = string.IsNullOrWhiteSpace(item.ActorUnionId)
                    ? item.UnionId ?? string.Empty
                    : item.ActorUnionId;
                if (players.ContainsKey(unionId) && !result.Contains(unionId)) result.Add(unionId);
            }
            return result;
        }

        private static string SelectedCommandName(
            M2BattleView battle,
            M2BattleUnionView union,
            string authoritativeCommandId)
        {
            var forecasts = battle.Forecasts ?? Array.Empty<M2ForecastView>();
            for (var index = 0; index < forecasts.Count; index++)
            {
                var forecast = forecasts[index];
                if (forecast == null || !StringComparer.Ordinal.Equals(forecast.UnionId, union.UnionId)) continue;
                if ((!string.IsNullOrWhiteSpace(union.SelectedForecastId) &&
                     StringComparer.Ordinal.Equals(forecast.ForecastId, union.SelectedForecastId)) ||
                    forecast.IsSelected ||
                    (!string.IsNullOrWhiteSpace(authoritativeCommandId) &&
                     StringComparer.Ordinal.Equals(forecast.CommandId, authoritativeCommandId)))
                    return forecast.CommandName ?? string.Empty;
            }
            return string.Empty;
        }

        private static string PlayerFacingName(M2BattleUnionView union, int ordinal, bool player)
        {
            if (!string.IsNullOrWhiteSpace(union?.DisplayName)) return union.DisplayName.Trim();
            if (!player) return "Enemy Union";
            return ordinal == 2 ? "Second Union" : "First Union";
        }

        private static bool IsEnemyAction(M2BattleEventView item)
        {
            if (item == null) return false;
            switch (Normalize(item.EventType))
            {
                case "ENEMY_HIT":
                case "INTERCEPTION":
                case "DOWNED":
                case "TACTICAL_HIT":
                case "MARTIAL_HIT":
                case "MYSTIC_HIT":
                case "GUARD":
                case "RESTORATION":
                case "ENEMY_SUPPORT_FORECAST":
                case "RECOVERY":
                case "POSITION_SHIFT":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsType(M2BattleEventView item, string eventType) =>
            item != null && StringComparer.Ordinal.Equals(Normalize(item.EventType), eventType);

        private static string Normalize(string value) =>
            (value ?? string.Empty).Trim().Replace(' ', '_').ToUpperInvariant();
    }
}
