using System;
using System.Collections.Generic;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// One-way commit adapter from deterministic recruitment payloads into M1 campaign snapshots.
    /// Canonical recruit/scouting JSON is retained so no authored or hidden recruitment data is lost.
    /// </summary>
    public static class ApplicantBoardStateAdapter
    {
        public static ApplicantBoardState ToFrozenTutorialState(DetailedApplicantBoard board)
        {
            return ToM1State(board, TutorialApplicantFactory.TutorialRootSeed, TutorialApplicantFactory.AuthoritySlots);
        }

        public static ApplicantBoardState ToM1State(
            DetailedApplicantBoard board,
            string generationKey,
            IReadOnlyList<TutorialApplicantAuthoritySlot> tutorialAuthority = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!board.Committed) throw new ArgumentException("Only a committed recruitment board can enter M1 state.", nameof(board));
            if (string.IsNullOrWhiteSpace(generationKey)) throw new ArgumentException("Generation key is required.", nameof(generationKey));

            var snapshots = new List<ApplicantSnapshotState>();
            for (var index = 0; index < board.Applicants.Count; index++)
            {
                var slot = board.Applicants[index];
                if (slot == null || slot.ApplicantRecord == null || slot.ScoutingReport == null)
                    throw new ArgumentException("Committed recruitment slots require full immutable payloads.", nameof(board));
                var authority = FindAuthority(tutorialAuthority, slot.SlotIndex);
                snapshots.Add(ToSnapshot(board, slot, authority));
            }

            return new ApplicantBoardState(
                board.BoardId,
                generationKey,
                board.RefreshIndex,
                true,
                snapshots,
                string.Empty);
        }

        private static ApplicantSnapshotState ToSnapshot(
            DetailedApplicantBoard board,
            ApplicantSlotDetailed slot,
            TutorialApplicantAuthoritySlot authority)
        {
            var recruit = slot.ApplicantRecord;
            var isSignature = StringComparer.Ordinal.Equals(recruit.SourceType, "SIGNATURE");
            var maximumHp = 55 + recruit.StatTendencies["HP"].BaseIndex * 2;
            var maximumMp = Math.Max(0, 4 + (recruit.StatTendencies["MAGIC"].BaseIndex - 40) / 3);
            var sourceSeed = authority == null
                ? recruit.GenerationSeed
                : isSignature ? authority.StableRecruitId : authority.SourceSeed;
            var signatureId = isSignature
                ? recruit.SignatureId
                : string.Empty;
            var stateSlot = authority?.AuthoringSlot ?? slot.SlotIndex + 1;
            var worldId = board.WorldIds != null && board.WorldIds.Count > 0 ? board.WorldIds[0] : string.Empty;
            var equipment = OpeningEquipment(recruit.EquipmentLoadout);

            return new ApplicantSnapshotState(
                stateSlot,
                recruit.RecruitId,
                recruit.DisplayName,
                isSignature ? ApplicantKind.Signature : ApplicantKind.Procedural,
                sourceSeed,
                signatureId,
                recruit.RaceId,
                worldId,
                recruit.StartingClassId,
                string.Empty,
                maximumHp,
                maximumHp,
                maximumMp,
                maximumMp,
                true,
                recruit.SigningCostXp,
                recruit.DevelopmentPotentialScore,
                CanonicalJson.Serialize(recruit),
                CanonicalJson.Serialize(slot.ScoutingReport),
                equipment.Items,
                authority?.TutorialStableId ?? string.Empty,
                authority?.StableRecruitId ?? string.Empty,
                equipment.Loadout,
                recruit.LeadershipScore,
                recruit.DisciplineAptitudes["TACTICAL"]);
        }

        private static OpeningEquipmentProjection OpeningEquipment(EquipmentLoadout loadout)
        {
            var items = new List<EquipmentItemState>();
            var assignments = new List<EquipmentSlotAssignmentState>();
            if (loadout?.Slots == null)
                return new OpeningEquipmentProjection(items.AsReadOnly(), EquipmentLoadoutState.Empty());
            foreach (var pair in loadout.Slots)
            {
                var item = pair.Value;
                if (item == null) continue;
                var slotId = StateSlotId(pair.Key);
                var stateItem = new EquipmentItemState(
                    item.InstanceId,
                    item.ItemDefinitionId,
                    item.ItemDefinitionId,
                    new[] { slotId },
                    item.Tags,
                    string.Empty,
                    10000,
                    item.Locked);
                items.Add(stateItem);
                assignments.Add(new EquipmentSlotAssignmentState(slotId, stateItem));
            }
            return new OpeningEquipmentProjection(
                items.AsReadOnly(),
                new EquipmentLoadoutState(assignments));
        }

        private static string StateSlotId(string recruitmentSlotId)
        {
            switch (recruitmentSlotId)
            {
                case "MAIN_HAND": return EquipmentSlotIds.MainHand;
                case "OFF_HAND": return EquipmentSlotIds.OffHand;
                case "BODY": return EquipmentSlotIds.BodyArmor;
                case "ACCESSORY_1": return EquipmentSlotIds.AccessoryOne;
                case "ACCESSORY_2": return EquipmentSlotIds.AccessoryTwo;
                case "TOOL_RELIC": return EquipmentSlotIds.ToolRelic;
                default: throw new InvalidOperationException("Unsupported opening equipment slot " + recruitmentSlotId + ".");
            }
        }

        private static TutorialApplicantAuthoritySlot FindAuthority(
            IReadOnlyList<TutorialApplicantAuthoritySlot> values,
            int boardSlotIndex)
        {
            if (values == null) return null;
            for (var index = 0; index < values.Count; index++)
                if (values[index].BoardSlotIndex == boardSlotIndex) return values[index];
            throw new InvalidOperationException("Tutorial authority is missing board slot " + boardSlotIndex + ".");
        }

        private sealed class OpeningEquipmentProjection
        {
            public OpeningEquipmentProjection(
                IReadOnlyList<EquipmentItemState> items,
                EquipmentLoadoutState loadout)
            {
                Items = items;
                Loadout = loadout;
            }

            public IReadOnlyList<EquipmentItemState> Items { get; }
            public EquipmentLoadoutState Loadout { get; }
        }
    }
}
