using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    /// <summary>
    /// Converts a claimed, immutable M2 battle receipt into equipment-instance growth.
    /// The battle member's creation-time main-hand snapshot is the only item binding.
    /// </summary>
    public sealed class BattleEquipmentGrowthBridge022
    {
        private const string GrowthEventType = "ART_GROWTH";
        private const string ReceiptPrefix = "EQUSE022_";

        public Result<CampaignState> ApplyClaimedBattleGrowth(
            CampaignState campaign,
            ICampaignRegistry022 registry)
        {
            if (campaign?.Battle == null || campaign.Guild == null || registry?.WeaponTracks == null)
                return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_BATTLE_REQUIRED");

            var battle = campaign.Battle;
            if (battle.Phase != BattlePhase.Resolved || battle.Outcome == BattleOutcome.InProgress ||
                battle.Reward == null || battle.Reward.Outcome != battle.Outcome)
                return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_TERMINAL_REWARD_REQUIRED");
            if (!battle.Reward.Claimed)
                return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_REWARD_CLAIM_REQUIRED");
            if (!campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId))
                return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_REWARD_LEDGER_REQUIRED");
            if (!M2BattleCommandService.HasValidFinalStateHash090(battle))
                return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_BATTLE_AUTHORITY_INVALID");

            if (!TryContext(campaign, out var city, out var strategic, out var progress,
                    out var playable, out var progression, out var contextError))
                return Result<CampaignState>.Failure(contextError);

            try
            {
                var evidence = CollectCanonicalEvidence(battle, out var evidenceError);
                if (evidence == null) return Result<CampaignState>.Failure(evidenceError);
                if (evidence.Count == 0) return Result<CampaignState>.Success(campaign);

                var equipment = new List<EquipmentEvolutionState022>(progression.EquipmentEvolution);
                var appliedReceipts = new List<string>(progression.AppliedReceiptIds);
                var newlyBoundItems = new HashSet<string>(StringComparer.Ordinal);
                var changed = false;

                for (var evidenceIndex = 0; evidenceIndex < evidence.Count; evidenceIndex++)
                {
                    var candidate = evidence[evidenceIndex];
                    var receiptId = CreateReceiptId(campaign, battle, candidate);
                    if (Contains(appliedReceipts, receiptId)) continue;

                    if (!newlyBoundItems.Add(candidate.Member.EquippedMainHandInstanceId))
                        return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_ITEM_SNAPSHOT_DUPLICATE");
                    var item = FindUniqueOwnedItem(
                        campaign.Guild,
                        candidate.Member.EquippedMainHandInstanceId,
                        out var itemError);
                    if (item == null) return Result<CampaignState>.Failure(itemError);
                    if (!item.CanEquipIn(EquipmentSlotIds.MainHand))
                        return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_MAIN_HAND_ITEM_INVALID");
                    var track = ResolveWeaponTrack(item, registry);
                    if (track == null)
                        return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_WEAPON_TRACK_UNRESOLVED");

                    var equipmentIndex = FindEquipmentIndex(equipment, item.InstanceId);
                    var current = equipmentIndex >= 0
                        ? equipment[equipmentIndex]
                        : new EquipmentEvolutionState022(
                            item.InstanceId,
                            track.trackId,
                            "TRAINING",
                            0,
                            0,
                            Array.Empty<string>(),
                            string.Empty);
                    if (!StringComparer.Ordinal.Equals(current.TrackId, track.trackId))
                        return Result<CampaignState>.Failure("CAMPAIGN022_EQUIPMENT_ITEM_TRACK_IMMUTABLE");

                    current = current.With(
                        meaningfulUses: checked(current.MeaningfulUses + candidate.Events.Count),
                        masteryPoints: checked(current.MasteryPoints + candidate.MasteryPoints),
                        historyTag: receiptId);
                    if (equipmentIndex >= 0) equipment[equipmentIndex] = current;
                    else equipment.Add(current);
                    appliedReceipts.Add(receiptId);
                    changed = true;
                }

                if (!changed) return Result<CampaignState>.Success(campaign);
                var next = progression.With(
                    equipmentEvolution: equipment.AsReadOnly(),
                    appliedReceiptIds: appliedReceipts.AsReadOnly(),
                    lastCheckpointId: "campaign022_equipment_battle_claim:" + battle.Reward.RewardId);
                return Success(campaign, city, strategic, progress, playable, next);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "CAMPAIGN022_EQUIPMENT_GROWTH_REJECTED: " + exception.Message);
            }
        }

        private static List<MemberGrowthEvidence022> CollectCanonicalEvidence(
            BattleState battle,
            out string error)
        {
            error = string.Empty;
            var result = new List<MemberGrowthEvidence022>();
            var evidenceIds = new HashSet<string>(StringComparer.Ordinal);
            for (var eventIndex = 0; eventIndex < battle.EventLog.Count; eventIndex++)
            {
                var growth = battle.EventLog[eventIndex];
                if (!StringComparer.Ordinal.Equals(growth.EventType, GrowthEventType) || growth.Amount <= 0)
                    continue;
                if (growth.Side != BattleSide.Player || string.IsNullOrWhiteSpace(growth.ArtId) ||
                    !HasCanonicalEventHash(growth))
                {
                    error = "CAMPAIGN022_EQUIPMENT_ART_EVIDENCE_INVALID";
                    return null;
                }

                var member = FindUniqueBattleMember(
                    battle.PlayerUnions,
                    growth.MemberId,
                    out var union,
                    out var memberError);
                if (member == null)
                {
                    error = memberError;
                    return null;
                }

                // Legacy save-v11 battles did not bind an item instance and remain claimable,
                // but their unbound Art evidence cannot create equipment growth.
                if (string.IsNullOrWhiteSpace(member.EquippedMainHandInstanceId)) continue;
                if (!StringComparer.Ordinal.Equals(growth.UnionId, union.UnionId) ||
                    !StringComparer.Ordinal.Equals(growth.ActorUnionId, union.UnionId) ||
                    !StringComparer.Ordinal.Equals(growth.ActorMemberId, member.MemberId) ||
                    !StringComparer.Ordinal.Equals(growth.TargetUnionId, union.UnionId) ||
                    !StringComparer.Ordinal.Equals(growth.TargetMemberId, member.MemberId) ||
                    CountRoundEvidence(battle.RoundRecords, growth) != 1)
                {
                    error = "CAMPAIGN022_EQUIPMENT_ART_EVIDENCE_INVALID";
                    return null;
                }

                var evidenceId = growth.Round + ":" + growth.Sequence + ":" + growth.StateHash;
                if (!evidenceIds.Add(evidenceId))
                {
                    error = "CAMPAIGN022_EQUIPMENT_ART_EVIDENCE_DUPLICATE";
                    return null;
                }

                var candidate = FindEvidence(result, union.UnionId, member.MemberId);
                if (candidate == null)
                {
                    candidate = new MemberGrowthEvidence022(union, member);
                    result.Add(candidate);
                }
                candidate.Add(growth);
            }
            result.Sort(MemberGrowthEvidence022.Compare);
            return result;
        }

        private static bool HasCanonicalEventHash(BattleEventState value) =>
            StringComparer.Ordinal.Equals(
                value.StateHash,
                CanonicalJson.Sha256Hex(new
                {
                    sequence = value.Sequence,
                    round = value.Round,
                    type = value.EventType,
                    side = value.Side,
                    unionId = value.UnionId,
                    memberId = value.MemberId,
                    artId = value.ArtId,
                    text = value.Text,
                    amount = value.Amount,
                    actorUnionId = value.ActorUnionId,
                    actorMemberId = value.ActorMemberId,
                    targetUnionId = value.TargetUnionId,
                    targetMemberId = value.TargetMemberId
                }));

        private static int CountRoundEvidence(
            IReadOnlyList<BattleRoundRecordState> rounds,
            BattleEventState expected)
        {
            var count = 0;
            for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
                for (var eventIndex = 0; eventIndex < rounds[roundIndex].Events.Count; eventIndex++)
                {
                    var candidate = rounds[roundIndex].Events[eventIndex];
                    if (candidate.Round == expected.Round && candidate.Sequence == expected.Sequence &&
                        StringComparer.Ordinal.Equals(candidate.StateHash, expected.StateHash))
                        count++;
                }
            return count;
        }

        private static BattleMemberState FindUniqueBattleMember(
            IReadOnlyList<BattleUnionState> unions,
            string memberId,
            out BattleUnionState resolvedUnion,
            out string error)
        {
            resolvedUnion = null;
            error = "CAMPAIGN022_EQUIPMENT_ART_MEMBER_UNRESOLVED";
            BattleMemberState resolved = null;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var memberIndex = unions[unionIndex].FindMemberIndex(memberId);
                if (memberIndex < 0) continue;
                if (resolved != null)
                {
                    error = "CAMPAIGN022_EQUIPMENT_ART_MEMBER_AMBIGUOUS";
                    return null;
                }
                resolvedUnion = unions[unionIndex];
                resolved = unions[unionIndex].Members[memberIndex];
            }
            return resolved;
        }

        private static EquipmentItemState FindUniqueOwnedItem(
            GuildState guild,
            string instanceId,
            out string error)
        {
            error = "CAMPAIGN022_EQUIPMENT_SNAPSHOT_ITEM_NOT_OWNED";
            EquipmentItemState resolved = null;
            var matchCount = 0;
            for (var index = 0; index < guild.Inventory.Count; index++)
                if (StringComparer.Ordinal.Equals(guild.Inventory[index].InstanceId, instanceId))
                {
                    resolved = guild.Inventory[index];
                    matchCount++;
                }
            for (var recruitIndex = 0; recruitIndex < guild.Recruits.Count; recruitIndex++)
                for (var assignmentIndex = 0;
                     assignmentIndex < guild.Recruits[recruitIndex].Equipment.Assignments.Count;
                     assignmentIndex++)
                {
                    var item = guild.Recruits[recruitIndex].Equipment.Assignments[assignmentIndex].Item;
                    if (!StringComparer.Ordinal.Equals(item.InstanceId, instanceId)) continue;
                    resolved = item;
                    matchCount++;
                }
            if (matchCount == 1) return resolved;
            if (matchCount > 1) error = "CAMPAIGN022_EQUIPMENT_SNAPSHOT_ITEM_AMBIGUOUS";
            return null;
        }

        private static WeaponTrackDto022 ResolveWeaponTrack(
            EquipmentItemState item,
            ICampaignRegistry022 registry)
        {
            WeaponTrackDto022 resolved = null;
            var bestScore = 0;
            foreach (var pair in registry.WeaponTracks)
            {
                var candidate = pair.Value;
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.trackId) ||
                    string.IsNullOrWhiteSpace(candidate.weaponFamilyId))
                    continue;
                var score = WeaponFamilyScore(item.EquipmentTags, candidate.weaponFamilyId);
                if (score <= 0 || score < bestScore) continue;
                if (score == bestScore && resolved != null &&
                    StringComparer.Ordinal.Compare(candidate.trackId, resolved.trackId) >= 0)
                    continue;
                resolved = candidate;
                bestScore = score;
            }
            return resolved;
        }

        private static int WeaponFamilyScore(IReadOnlyList<string> tags, string familyId)
        {
            if (Contains(tags, familyId)) return 1000;
            // SSS signatures and Creator weapons carry the authored WFxx family
            // ID. Bind that exact family to its existing evolution track even
            // when the item does not also carry the older combat-tag aliases.
            // Unknown families still fail resolution; snapshot ownership, Art
            // evidence and immutable-track checks remain the claim authority.
            var packagedFamily107 = PackagedWeaponFamily107(familyId);
            if (packagedFamily107 != null && Contains(tags, packagedFamily107)) return 1000;
            switch (familyId)
            {
                case "WEAPON_FAMILY_SWORD": return Has(tags, "SWORD") ? 120 : 0;
                case "WEAPON_FAMILY_GREAT_WEAPON":
                    if (Has(tags, "HAMMER")) return 150;
                    if (Has(tags, "GREAT_AXE")) return 140;
                    return Has(tags, "POLEARM") ? 100 : 0;
                case "WEAPON_FAMILY_AXE":
                    if (Has(tags, "AXE")) return 150;
                    return Has(tags, "GREAT_AXE") ? 130 : 0;
                case "WEAPON_FAMILY_SPEAR_POLEARM":
                    if (Has(tags, "SPEAR") || Has(tags, "LANCE")) return 150;
                    return Has(tags, "POLEARM") ? 130 : 0;
                case "WEAPON_FAMILY_BOW":
                    return Has(tags, "BOW") || Has(tags, "SHORTBOW") || Has(tags, "LONGBOW") ? 150 : 0;
                case "WEAPON_FAMILY_DAGGER": return Has(tags, "DAGGER") ? 150 : 0;
                case "WEAPON_FAMILY_SHIELD":
                    return Has(tags, "SHIELD") || Has(tags, "WARD_BUCKLER") || Has(tags, "SHIELD_COMPATIBLE") ? 110 : 0;
                case "WEAPON_FAMILY_GAUNTLET":
                    return Has(tags, "UNARMED") || Has(tags, "GAUNTLET") ? 150 : 0;
                case "WEAPON_FAMILY_STAFF": return Has(tags, "STAFF") ? 150 : 0;
                case "WEAPON_FAMILY_FOCUS":
                    return Has(tags, "FOCUS_TOOL") || Has(tags, "WAND") || Has(tags, "FOCUS") ? 150 : 0;
                case "WEAPON_FAMILY_ENGINEERING_TOOL":
                    return Has(tags, "TOOL") || Has(tags, "ARCANE_CONDUCTOR") ||
                           Has(tags, "SIGNAL_HORN") || Has(tags, "SIGNAL_KIT") || Has(tags, "ALCHEMY") ? 150 : 0;
                case "WEAPON_FAMILY_HYBRID_RELIC_WEAPON":
                    if (Has(tags, "HYBRID_RELIC")) return 170;
                    return Has(tags, "SWORD") && Has(tags, "FOCUS_TOOL") ? 160 : 0;
                default: return 0;
            }
        }

        private static string PackagedWeaponFamily107(string familyId)
        {
            switch (familyId)
            {
                case "WEAPON_FAMILY_SWORD": return "WF01_SWORD";
                case "WEAPON_FAMILY_GREAT_WEAPON": return "WF02_GREAT_WEAPON";
                case "WEAPON_FAMILY_AXE": return "WF03_AXE";
                case "WEAPON_FAMILY_SPEAR_POLEARM": return "WF04_SPEAR_POLEARM";
                case "WEAPON_FAMILY_BOW": return "WF05_BOW";
                case "WEAPON_FAMILY_DAGGER": return "WF06_DAGGER";
                case "WEAPON_FAMILY_SHIELD": return "WF07_SHIELD";
                case "WEAPON_FAMILY_GAUNTLET": return "WF08_GAUNTLET";
                case "WEAPON_FAMILY_STAFF": return "WF09_STAFF";
                case "WEAPON_FAMILY_FOCUS": return "WF10_CATALYST_FOCUS";
                case "WEAPON_FAMILY_ENGINEERING_TOOL": return "WF11_ENGINEERING_TOOL";
                case "WEAPON_FAMILY_HYBRID_RELIC_WEAPON": return "WF12_HYBRID_RELIC";
                default: return null;
            }
        }

        private static string CreateReceiptId(
            CampaignState campaign,
            BattleState battle,
            MemberGrowthEvidence022 evidence)
        {
            var eventAuthority = new List<object>();
            for (var index = 0; index < evidence.Events.Count; index++)
            {
                var value = evidence.Events[index];
                eventAuthority.Add(new
                {
                    value.Round,
                    value.Sequence,
                    value.StateHash,
                    value.ArtId,
                    value.Amount
                });
            }
            var hash = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                battle.BattleId,
                battle.FinalStateHash,
                RewardId = battle.Reward.RewardId,
                evidence.Union.UnionId,
                evidence.Member.MemberId,
                evidence.Member.EquippedMainHandInstanceId,
                Events = eventAuthority,
                MeaningfulUses = evidence.Events.Count,
                evidence.MasteryPoints
            });
            return ReceiptPrefix + hash.Substring(0, 24).ToUpperInvariant();
        }

        private static MemberGrowthEvidence022 FindEvidence(
            IReadOnlyList<MemberGrowthEvidence022> values,
            string unionId,
            string memberId)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].Union.UnionId, unionId) &&
                    StringComparer.Ordinal.Equals(values[index].Member.MemberId, memberId))
                    return values[index];
            return null;
        }

        private static int FindEquipmentIndex(
            IReadOnlyList<EquipmentEvolutionState022> values,
            string itemInstanceId)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].ItemInstanceId, itemInstanceId)) return index;
            return -1;
        }

        private static bool Has(IReadOnlyList<string> values, string value) => Contains(values, value);

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (values != null)
                for (var index = 0; index < values.Count; index++)
                    if (StringComparer.Ordinal.Equals(values[index], value)) return true;
            return false;
        }

        private static bool TryContext(
            CampaignState campaign,
            out GuildCityState017D city,
            out GuildCityStrategicState017H strategic,
            out CampaignProgressState019 progress,
            out CampaignPlayableState020 playable,
            out CampaignProgressionState022 progression,
            out string error)
        {
            city = null;
            strategic = null;
            progress = null;
            playable = null;
            progression = null;
            error = "CAMPAIGN022_EQUIPMENT_PROGRESSION_CONTEXT_REQUIRED";
            if (campaign?.Guild?.GuildCity == null) return false;
            city = campaign.Guild.GuildCity;
            strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            playable = progress.Playable020 ?? CampaignPlayableState020.Default();
            progression = playable.Progression022 ?? CampaignProgressionState022.Default();
            return true;
        }

        private static Result<CampaignState> Success(
            CampaignState campaign,
            GuildCityState017D city,
            GuildCityStrategicState017H strategic,
            CampaignProgressState019 progress,
            CampaignPlayableState020 playable,
            CampaignProgressionState022 progression)
        {
            var nextPlayable = playable.With(
                progression022: progression,
                replaceProgression022: true,
                lastCheckpointId: progression.LastCheckpointId);
            var nextProgress = progress.With(
                playable020: nextPlayable,
                replacePlayable020: true,
                lastCheckpointId: progression.LastCheckpointId);
            var nextStrategic = strategic.With(
                campaign019: nextProgress,
                replaceCampaign019: true,
                lastCheckpointId: progression.LastCheckpointId);
            var nextCity = city.With(
                strategic017H: nextStrategic,
                replaceStrategic017H: true,
                lastCheckpointId: progression.LastCheckpointId);
            return Result<CampaignState>.Success(
                campaign.With(campaign.Guild.WithGuildCity(nextCity), campaign.OpeningFlow));
        }

        private sealed class MemberGrowthEvidence022
        {
            private readonly List<BattleEventState> _events = new List<BattleEventState>();

            public MemberGrowthEvidence022(BattleUnionState union, BattleMemberState member)
            {
                Union = union;
                Member = member;
            }

            public BattleUnionState Union { get; }
            public BattleMemberState Member { get; }
            public IReadOnlyList<BattleEventState> Events => _events.AsReadOnly();
            public int MasteryPoints { get; private set; }

            public void Add(BattleEventState value)
            {
                _events.Add(value);
                MasteryPoints = checked(MasteryPoints + value.Amount);
            }

            public static int Compare(MemberGrowthEvidence022 left, MemberGrowthEvidence022 right)
            {
                var byUnion = StringComparer.Ordinal.Compare(left.Union.UnionId, right.Union.UnionId);
                return byUnion != 0
                    ? byUnion
                    : StringComparer.Ordinal.Compare(left.Member.MemberId, right.Member.MemberId);
            }
        }
    }
}
