using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M1
{
    // A manual batch over the existing item/EquipItem authority. It does not
    // create items, teach Arts, change character identity, or run from rewards.
    public sealed class AutoEquipmentService112
    {
        readonly M2CombatContent _combat;
        readonly RecruitStarterEquipment094 _authority;
        readonly HashSet<string> _artEquipmentTags;
        const long Forbidden = 1000000000000L;

        public AutoEquipmentService112(M2CombatContent combat, RecruitStarterEquipment094 authority)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _authority = authority ?? throw new ArgumentNullException(nameof(authority));
            _artEquipmentTags = new HashSet<string>(combat.Arts.Values.SelectMany(value => value.RequiredEquipmentTags),
                StringComparer.Ordinal);
        }

        public Result<CampaignState> EquipHero(CampaignState state, string recruitId)
        {
            if (string.IsNullOrWhiteSpace(recruitId)) return Failure("Select an owned hero first.");
            return Apply(state, new HashSet<string>(new[] { recruitId }, StringComparer.Ordinal));
        }

        public Result<CampaignState> EquipAllUnions(CampaignState state)
        {
            if (state?.Guild == null) return Failure("Load a Guild before changing equipment.");
            var active = new HashSet<string>(UnionBattlePlanRules132.Read(state).Where(value => value.Kind == UnionKind.Normal)
                .SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
            return Apply(state, active);
        }

        Result<CampaignState> Apply(CampaignState state, HashSet<string> selected)
        {
            var reason = MutationBlockedReason(state);
            if (!string.IsNullOrEmpty(reason)) return Failure(reason);
            if (selected.Count == 0) return Failure("Assign heroes to an active Union first.");
            if (selected.Any(id => !state.Guild.Recruits.Any(value => value.RecruitId == id)))
                return Failure("The selected roster changed. Select the heroes again.");
            if (!UniqueOwnership(state)) return Failure("Equipment ownership needs resolution before Auto Equip.");

            var rows = new List<Slot112>();
            foreach (var recruit in state.Guild.Recruits.Where(value => selected.Contains(value.RecruitId))
                         .OrderBy(value => value.RecruitId, StringComparer.Ordinal))
            {
                if (!ProtectedActorPolicy.CanUseNormalEquipment(recruit)) continue;
                JObject classAuthority;
                JObject familyAuthority;
                try
                {
                    classAuthority = _authority.EquipmentClassAuthority112(recruit);
                    familyAuthority = _authority.EquipmentFamilyAuthority112(recruit);
                }
                catch (KeyNotFoundException)
                {
                    return Failure("The approved equipment profile is unresolved for " + recruit.DisplayName + ". Equipment kept.");
                }
                var profile = Profile(recruit, state, classAuthority, familyAuthority);
                foreach (var slot in EquipmentSlotIds.All)
                {
                    var current = recruit.Equipment.Find(slot)?.Item;
                    // Existing locked, signature, or legacy-incompatible gear
                    // stays on its owner. No migration strips it into the pool.
                    if (current != null && (current.PlayerLocked || current.EquipmentTags.Contains("SSS_SIGNATURE") ||
                        !SssTenV4Inventory090.CanEquip(recruit, current, slot, out _))) continue;
                    rows.Add(new Slot112
                    {
                        Recruit = recruit, Slot = slot, Current = current, Profile = profile,
                        CriticalTags = (current?.EquipmentTags ?? Array.Empty<string>())
                            .Where(_artEquipmentTags.Contains).ToArray()
                    });
                }
            }
            if (rows.Count == 0) return Result<CampaignState>.Success(state);
            // Only the chosen heroes contribute equipped items. Reserves and
            // other heroes' gear cannot be stripped by a one-hero operation.
            var pool = state.Guild.Inventory.Where(value => !value.PlayerLocked && !value.InventoryOnly)
                .Concat(rows.Where(value => value.Current != null).Select(value => value.Current))
                .OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray();
            var emptyRows = rows.Select((row, index) => new { row, index })
                .Where(value => value.row.Current == null).Select(value => value.index).ToArray();
            var costs = new long[rows.Count, pool.Length + emptyRows.Length];
            for (var row = 0; row < rows.Count; row++)
            {
                for (var column = 0; column < pool.Length; column++)
                    costs[row, column] = Cost(rows[row], pool[column]);
                for (var empty = 0; empty < emptyRows.Length; empty++)
                    costs[row, pool.Length + empty] = row == emptyRows[empty] ? 0 : Forbidden;
            }
            var allocation = MinimumAssignment(costs);
            var changes = new List<EquipmentChange112>();
            for (var row = 0; row < rows.Count; row++)
            {
                var next = allocation[row] < pool.Length ? pool[allocation[row]] : null;
                if (costs[row, allocation[row]] >= Forbidden)
                    return Failure("No complete legal equipment allocation was found. Equipment kept.");
                if ((rows[row].Current?.InstanceId ?? string.Empty) == (next?.InstanceId ?? string.Empty)) continue;
                changes.Add(new EquipmentChange112(rows[row].Recruit.RecruitId, rows[row].Slot,
                    rows[row].Current?.InstanceId, next?.InstanceId));
            }
            if (changes.Count == 0) return Result<CampaignState>.Success(state);
            var applied = ApplyChanges(state, changes, undo: false);
            if (!applied.IsSuccess) return applied;
            var candidate = applied.Value;
            foreach (var recruit in state.Guild.Recruits.Where(value => selected.Contains(value.RecruitId)))
            {
                var after = candidate.Guild.Recruits.Single(value => value.RecruitId == recruit.RecruitId);
                if (!PreservesLearnedArts(recruit, after))
                    return Failure("The proposed equipment would disable a learned Art. Equipment kept.");
            }
            if (!SameItems(state, candidate)) return Failure("Equipment ownership validation failed. Equipment kept.");
            var summary = Summary(state, candidate, changes);
            return Result<CampaignState>.Success(candidate.WithEquipmentUndo112(
                new EquipmentUndoState112(Guard(candidate), changes, summary)));
        }

        long Cost(Slot112 slot, EquipmentItemState item)
        {
            if (slot.Current?.InstanceId == item.InstanceId) return 0;
            if (item.PlayerLocked || !SssTenV4Inventory090.CanEquip(slot.Recruit, item, slot.Slot, out _)) return Forbidden;
            if (slot.CriticalTags.Any(tag => !item.EquipmentTags.Contains(tag))) return Forbidden;
            if (slot.Current != null && item.ConditionBasisPoints < slot.Current.ConditionBasisPoints) return Forbidden;
            if (slot.Slot == EquipmentSlotIds.MainHand && slot.Current == null &&
                !slot.Profile.FamilyTags.Any(item.EquipmentTags.Contains)) return Forbidden;
            if (slot.Slot == EquipmentSlotIds.OffHand && slot.Current == null &&
                !slot.Profile.FamilyTags.Any(item.EquipmentTags.Contains) &&
                !slot.Profile.ClassWeaponTags.Any(item.EquipmentTags.Contains)) return Forbidden;
            if (slot.Slot == EquipmentSlotIds.BodyArmor)
            {
                // Class authority names the actual weight tokens. Untyped legacy
                // armor remains legal through the existing slot authority.
                var weight = item.EquipmentTags.FirstOrDefault(value => value == "LIGHT" || value == "MEDIUM" || value == "HEAVY");
                if (weight != null && !slot.Profile.ArmorWeights.Contains(weight)) return Forbidden;
            }
            var oldPower = M2EquipmentPowerPolicy087.Resolve(slot.Current);
            var newPower = M2EquipmentPowerPolicy087.Resolve(item);
            var physical = newPower.PhysicalAttack - oldPower.PhysicalAttack;
            var mystic = newPower.MysticAttack - oldPower.MysticAttack;
            // Ambiguous tradeoffs stay available to manual inventory selection.
            if (physical < 0 || mystic < 0) return Forbidden;
            var benefit = checked((long)physical * slot.Profile.PhysicalWeight + (long)mystic * slot.Profile.MysticWeight +
                (slot.Current == null ? 1 : 0));
            // A global maximum across every selected slot, with a small penalty
            // for needless equal-stat shuffling. Stable IDs break remaining ties.
            return -benefit * 1000 + 1;
        }

        Profile112 Profile(RecruitState recruit, CampaignState state, JObject cls, JObject family)
        {
            var affinities = cls["disciplineAffinities"];
            var physical = 1 + (affinities?["Martial"]?.Value<int>() ?? 0) / 20;
            var mystic = 1 + Math.Max(affinities?["Mystic"]?.Value<int>() ?? 0,
                affinities?["Restoration"]?.Value<int>() ?? 0) / 20;
            if (SssTenV4Roster090.TryGetRecruit(recruit, out var sss))
            {
                physical += sss.AttackWeight / 5;
                mystic += sss.MagicWeight / 5;
            }
            var maxMp = recruit.MaximumMp + recruit.Progression.MaximumMpBonus;
            var union = UnionBattlePlanRules132.Read(state).FirstOrDefault(value => value.MemberRecruitIds.Contains(recruit.RecruitId));
            var maxAp = Math.Max(14, union?.SharedAp ?? 14);
            var learned = M2DeepArtRuntime070.BattleLearnedArts(recruit, _combat)
                .Where(_combat.Arts.ContainsKey).Select(id => _combat.Arts[id])
                .Where(art => art.IsForecastAction && art.PersonalMpCost <= maxMp && art.SharedApCost <= maxAp).ToArray();
            physical += Math.Min(4, learned.Count(art => art.Discipline == "Martial"));
            mystic += Math.Min(4, learned.Count(art => art.Discipline == "Mystic" || art.Discipline == "Restoration"));
            return new Profile112
            {
                PhysicalWeight = physical, MysticWeight = mystic,
                FamilyTags = ((JArray)family["equipmentTagsGranted"]).Values<string>().ToArray(),
                ClassWeaponTags = ((JArray)cls["validWeaponFamilies"]).Values<string>().ToArray(),
                ArmorWeights = ((JArray)cls["armorWeights"]).Values<string>().ToArray()
            };
        }

        bool PreservesLearnedArts(RecruitState before, RecruitState after)
        {
            var oldTags = before.Equipment.Assignments.SelectMany(value => value.Item.EquipmentTags).ToArray();
            var nextTags = after.Equipment.Assignments.SelectMany(value => value.Item.EquipmentTags).ToArray();
            foreach (var id in M2DeepArtRuntime070.BattleLearnedArts(before, _combat))
            {
                if (!_combat.Arts.TryGetValue(id, out var art) || art.RequiredEquipmentTags.Count == 0) continue;
                if (art.RequiredEquipmentTags.Any(oldTags.Contains) && !art.RequiredEquipmentTags.Any(nextTags.Contains)) return false;
            }
            return true;
        }

        static Result<CampaignState> ApplyChanges(CampaignState state, IReadOnlyList<EquipmentChange112> changes, bool undo)
        {
            var commands = new M1CommandService();
            var candidate = state;
            // Every command runs against an immutable candidate; callers save
            // only the completed batch, never the intermediate unequipped state.
            foreach (var change in changes)
            {
                var removed = commands.UnequipItem(candidate, change.RecruitId, change.SlotId);
                if (!removed.IsSuccess) return removed;
                candidate = removed.Value;
            }
            foreach (var change in changes)
            {
                var itemId = undo ? change.BeforeItemId : change.AfterItemId;
                if (string.IsNullOrWhiteSpace(itemId)) continue;
                var equipped = commands.EquipItem(candidate, change.RecruitId, change.SlotId, itemId);
                if (!equipped.IsSuccess) return equipped;
                candidate = equipped.Value;
            }
            return Result<CampaignState>.Success(candidate);
        }

        public static string UndoBlockedReason(CampaignState state)
        {
            var blocked = MutationBlockedReason(state);
            if (!string.IsNullOrEmpty(blocked)) return blocked;
            if (state.EquipmentUndo112 == null || state.EquipmentUndo112.Changes.Count == 0)
                return "No Auto Equip transaction to undo.";
            if (!StringComparer.Ordinal.Equals(Guard(state), state.EquipmentUndo112.AfterGuard))
                return "Equipment or roster changed since Auto Equip. That Undo is no longer valid.";
            return string.Empty;
        }

        public static Result<CampaignState> Undo(CampaignState state)
        {
            var reason = UndoBlockedReason(state);
            if (!string.IsNullOrEmpty(reason)) return Failure(reason);
            var changes = state.EquipmentUndo112.Changes;
            if (!UniqueOwnership(state) || changes.Any(value => value == null) ||
                changes.Select(value => value.RecruitId + "\n" + value.SlotId).Distinct().Count() != changes.Count)
                return Failure("The Undo ownership record is invalid. Equipment kept.");
            foreach (var change in changes)
            {
                var recruit = state.Guild.Recruits.FirstOrDefault(value => value.RecruitId == change.RecruitId);
                if (recruit == null || !EquipmentSlotIds.IsOpeningSlot(change.SlotId) ||
                    (recruit.Equipment.Find(change.SlotId)?.Item.InstanceId ?? string.Empty) != change.AfterItemId)
                    return Failure("The Undo target changed. Equipment kept.");
            }
            var undone = ApplyChanges(state, changes, undo: true);
            if (!undone.IsSuccess) return undone;
            if (!SameItems(state, undone.Value)) return Failure("Undo item ownership validation failed. Equipment kept.");
            return Result<CampaignState>.Success(undone.Value.WithEquipmentUndo112(null));
        }

        public static string MutationBlockedReason(CampaignState state)
        {
            if (state?.Guild == null || state.Profile == null || state.OpeningFlow == null)
                return "Load a Guild before changing equipment.";
            if (state.Battle?.Outcome == BattleOutcome.InProgress ||
                GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(state, allowPausedCampaign150:true))
                return "Finish the active encounter and return before changing equipment.";
            return string.Empty;
        }

        static string Guard(CampaignState state) => CanonicalJson.Sha256Hex(new
        {
            state.CampaignGuid, state.Guild.Recruits, state.Guild.Unions, state.Guild.Inventory
        });
        static IEnumerable<EquipmentItemState> Items(CampaignState state) => state.Guild.Inventory.Concat(
            state.Guild.Recruits.SelectMany(value => value.Equipment.Assignments.Select(assignment => assignment.Item)));
        static bool UniqueOwnership(CampaignState state)
        {
            var items = Items(state).ToArray();
            return items.Select(value => value.InstanceId).Distinct(StringComparer.Ordinal).Count() == items.Length;
        }
        static bool SameItems(CampaignState left, CampaignState right) => UniqueOwnership(right) &&
            CanonicalJson.Serialize(Items(left).OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray()) ==
            CanonicalJson.Serialize(Items(right).OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray());
        static Result<CampaignState> Failure(string message) => Result<CampaignState>.Failure(message);

        static string Summary(CampaignState before, CampaignState after, IReadOnlyList<EquipmentChange112> changes)
        {
            var ids = changes.Select(value => value.RecruitId).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var lines = new List<string> { changes.Count + (changes.Count == 1 ? " item change across " : " item changes across ") +
                ids.Length + (ids.Length == 1 ? " hero." : " heroes.") };
            foreach (var id in ids)
            {
                var old = before.Guild.Recruits.Single(value => value.RecruitId == id);
                var next = after.Guild.Recruits.Single(value => value.RecruitId == id);
                var oldPower = M2EquipmentPowerPolicy087.Resolve(old.Equipment);
                var nextPower = M2EquipmentPowerPolicy087.Resolve(next.Equipment);
                lines.Add(next.DisplayName + ": physical +" + (nextPower.PhysicalAttack - oldPower.PhysicalAttack) +
                    ", mystic +" + (nextPower.MysticAttack - oldPower.MysticAttack) + ".");
            }
            return string.Join("\n", lines);
        }

        // Rectangular Hungarian assignment: each physical item is one column,
        // each movable slot one row. This maximizes the combined supported gains
        // instead of letting the first processed hero claim every shared upgrade.
        static int[] MinimumAssignment(long[,] costs)
        {
            var n = costs.GetLength(0);
            var m = costs.GetLength(1);
            var u = new long[n + 1];
            var v = new long[m + 1];
            var p = new int[m + 1];
            var way = new int[m + 1];
            for (var i = 1; i <= n; i++)
            {
                p[0] = i;
                var j0 = 0;
                var minimum = Enumerable.Repeat(Forbidden * 4, m + 1).ToArray();
                var used = new bool[m + 1];
                do
                {
                    used[j0] = true;
                    var i0 = p[j0];
                    var delta = Forbidden * 4;
                    var j1 = 0;
                    for (var j = 1; j <= m; j++)
                    {
                        if (used[j]) continue;
                        var current = costs[i0 - 1, j - 1] - u[i0] - v[j];
                        if (current < minimum[j]) { minimum[j] = current; way[j] = j0; }
                        if (minimum[j] < delta) { delta = minimum[j]; j1 = j; }
                    }
                    for (var j = 0; j <= m; j++)
                    {
                        if (used[j]) { u[p[j]] += delta; v[j] -= delta; }
                        else minimum[j] -= delta;
                    }
                    j0 = j1;
                } while (p[j0] != 0);
                do
                {
                    var j1 = way[j0];
                    p[j0] = p[j1];
                    j0 = j1;
                } while (j0 != 0);
            }
            var result = new int[n];
            for (var j = 1; j <= m; j++) if (p[j] != 0) result[p[j] - 1] = j - 1;
            return result;
        }

        sealed class Profile112
        {
            public int PhysicalWeight, MysticWeight;
            public string[] FamilyTags, ClassWeaponTags, ArmorWeights;
        }
        sealed class Slot112
        {
            public RecruitState Recruit;
            public string Slot;
            public EquipmentItemState Current;
            public Profile112 Profile;
            public string[] CriticalTags;
        }
    }
}
