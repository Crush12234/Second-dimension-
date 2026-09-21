using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.M1
{
    public sealed class NewGuildCommand
    {
        public NewGuildCommand(
            string campaignGuid,
            long campaignSeed,
            string contentAuthorityVersion,
            string guildId,
            NewGuildProfileState profile,
            long? initialTreasuryXp = null,
            ModeRuleSnapshot customRules = null)
        {
            CampaignGuid = campaignGuid;
            CampaignSeed = campaignSeed;
            ContentAuthorityVersion = contentAuthorityVersion;
            GuildId = guildId;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            InitialTreasuryXp = initialTreasuryXp;
            CustomRules = customRules;
        }

        public string CampaignGuid { get; }
        public long CampaignSeed { get; }
        public string ContentAuthorityVersion { get; }
        public string GuildId { get; }
        public NewGuildProfileState Profile { get; }
        public long? InitialTreasuryXp { get; }
        public ModeRuleSnapshot CustomRules { get; }
    }

    /// <summary>
    /// Deterministic opening-only preview of the exact AP and Cohesion values
    /// that CompleteOpening will persist. It exposes no hidden recruit data and
    /// does not apply the full M2 formation combat model.
    /// </summary>
    public sealed class OpeningUnionResourcePreview
    {
        public OpeningUnionResourcePreview(
            int sharedAp,
            int baseCohesionBasisPoints,
            int formationCohesionRuleBasisPoints,
            int cohesionBasisPoints)
        {
            SharedAp = sharedAp;
            BaseCohesionBasisPoints = baseCohesionBasisPoints;
            FormationCohesionRuleBasisPoints = formationCohesionRuleBasisPoints;
            CohesionBasisPoints = cohesionBasisPoints;
        }

        public int SharedAp { get; }
        public int BaseCohesionBasisPoints { get; }
        public int FormationCohesionRuleBasisPoints { get; }
        public int AppliedFormationCohesionBasisPoints => CohesionBasisPoints - BaseCohesionBasisPoints;
        public int CohesionBasisPoints { get; }
    }

    /// <summary>
    /// Pure copy-on-write commands for the M1 opening. This service owns no clock,
    /// file system, Unity object, or random generator. Generated boards must be
    /// committed as ApplicantBoardState before they can affect the campaign.
    /// </summary>
    public sealed class M1CommandService
    {
        public static OpeningUnionResourcePreview ProjectOpeningUnionResources(
            GuildState guild,
            UnionState union)
        {
            if (guild == null || union == null || union.MemberRecruitIds.Count == 0)
            {
                return new OpeningUnionResourcePreview(0, 0, 0, 0);
            }

            var leaderIndex = FindRecruitIndex(guild.Recruits, union.LeaderRecruitId);
            var leadership = leaderIndex < 0 ? 0 : guild.Recruits[leaderIndex].LeadershipScore;
            var tacticalTotal = 0;
            for (var memberIndex = 0; memberIndex < union.MemberRecruitIds.Count; memberIndex++)
            {
                var recruitIndex = FindRecruitIndex(guild.Recruits, union.MemberRecruitIds[memberIndex]);
                if (recruitIndex >= 0) tacticalTotal += guild.Recruits[recruitIndex].TacticalAptitude;
            }

            var averageTactical = tacticalTotal / union.MemberRecruitIds.Count;
            var sharedAp = Clamp(10 + leadership / 10 + averageTactical / 18, 14, 30);
            var baseCohesionPercent = Clamp(65 + leadership / 3, 60, 100);
            var formationRulePercent = FormationCohesionBonus(union.FormationId);
            var cohesionPercent = Clamp(baseCohesionPercent + formationRulePercent, 60, 100);
            return new OpeningUnionResourcePreview(
                sharedAp,
                baseCohesionPercent * 100,
                formationRulePercent * 100,
                cohesionPercent * 100);
        }

        public Result<CampaignState> CreateNewGuild(NewGuildCommand command)
        {
            if (command == null) return Result<CampaignState>.Failure("M1_NEW_GUILD_COMMAND_REQUIRED");
            try
            {
                ModeRuleSnapshot rules;
                if (command.Profile.Mode == GameMode.Custom)
                {
                    rules = command.CustomRules;
                    if (rules == null || rules.Mode != GameMode.Custom)
                    {
                        return Result<CampaignState>.Failure("M1_CUSTOM_RULES_REQUIRED");
                    }
                    if (rules.PermanentDeathEnabled || rules.DepartureEnabled)
                    {
                        return Result<CampaignState>.Failure("M1_OWNER_BUILD_PERMANENT_RECRUITS_REQUIRED");
                    }
                }
                else
                {
                    if (command.CustomRules != null)
                    {
                        return Result<CampaignState>.Failure("M1_PRESET_MODE_REJECTS_CUSTOM_RULES");
                    }
                    rules = ModeCatalog.GetPreset(command.Profile.Mode);
                }

                if (!rules.StartingTreasuryXp.HasValue &&
                    command.InitialTreasuryXp.HasValue &&
                    command.InitialTreasuryXp.Value != 0)
                {
                    return Result<CampaignState>.Failure("M1_UNFROZEN_STARTING_TREASURY_REJECTED");
                }
                var treasury = rules.StartingTreasuryXp ?? command.InitialTreasuryXp ?? 0;
                if (treasury < 0) return Result<CampaignState>.Failure("M1_NEGATIVE_STARTING_TREASURY");
                if (rules.StartingTreasuryXp.HasValue && treasury != rules.StartingTreasuryXp.Value)
                {
                    return Result<CampaignState>.Failure("M1_MODE_STARTING_TREASURY_MISMATCH");
                }

                var guild = new GuildState(
                    command.GuildId,
                    treasury,
                    Array.Empty<RecruitState>(),
                    Array.Empty<UnionState>(),
                    Array.Empty<EquipmentItemState>());
                return Result<CampaignState>.Success(new CampaignState(
                    command.CampaignGuid,
                    command.CampaignSeed,
                    command.ContentAuthorityVersion,
                    rules,
                    guild,
                    command.Profile,
                    OpeningFlowState.NewProfileCommitted()));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("M1_NEW_GUILD_INVALID: " + exception.Message);
            }
        }

        public Result<CampaignState> AcceptCivicCharter(CampaignState state)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var flow = state.OpeningFlow;
            if (flow.CivicCharterAccepted) return Result<CampaignState>.Success(state);
            return Result<CampaignState>.Success(state.With(
                state.Guild,
                NewFlow(
                    flow,
                    OpeningStage.ApplicantBoard,
                    true,
                    flow.ApplicantBoard,
                    flow.ApplicantBoardCommitted,
                    flow.SigningCreditTotal,
                    flow.SigningCreditRemaining,
                    flow.RecruitmentCompleted,
                    flow.EquipmentReviewCompleted,
                    flow.UnionBuilderCompleted,
                    "autosave_charter")));
        }

        public Result<CampaignState> CommitApplicantBoard(CampaignState state, ApplicantBoardState board)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            if (board == null) return Result<CampaignState>.Failure("M1_BOARD_REQUIRED");
            var flow = state.OpeningFlow;
            if (!flow.CivicCharterAccepted) return Result<CampaignState>.Failure("M1_CHARTER_NOT_ACCEPTED");
            if (flow.ApplicantBoard != null)
            {
                return StringComparer.Ordinal.Equals(
                        flow.ApplicantBoard.CommittedApplicantsHash,
                        board.CommittedApplicantsHash)
                    ? Result<CampaignState>.Success(state)
                    : Result<CampaignState>.Failure("M1_COMMITTED_BOARD_CANNOT_REROLL");
            }

            var tutorialValidation = ValidateFrozenTutorialBoard(board);
            if (!tutorialValidation.IsSuccess)
            {
                return Result<CampaignState>.Failure(CopyErrors(tutorialValidation.Errors));
            }

            try
            {
                var credit = 0;
                checked
                {
                    for (var index = 0; index < board.Applicants.Count; index++)
                    {
                        credit += board.Applicants[index].SigningCostTreasuryXp;
                    }
                }
                return Result<CampaignState>.Success(state.With(
                    state.Guild,
                    NewFlow(
                        flow,
                        OpeningStage.Recruitment,
                        true,
                        board,
                        true,
                        credit,
                        credit,
                        false,
                        false,
                        false,
                        flow.LastCheckpointId)));
            }
            catch (OverflowException)
            {
                return Result<CampaignState>.Failure("M1_SIGNING_CREDIT_OVERFLOW");
            }
        }

        public Result<CampaignState> SignApplicant(CampaignState state, string recruitId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var flow = state.OpeningFlow;
            if (flow.ApplicantBoard == null) return Result<CampaignState>.Failure("M1_BOARD_NOT_COMMITTED");
            var applicant = flow.ApplicantBoard.FindApplicant(recruitId);
            if (applicant == null) return Result<CampaignState>.Failure("M1_APPLICANT_NOT_FOUND");
            if (FindRecruitIndex(state.Guild.Recruits, recruitId) >= 0)
            {
                return Result<CampaignState>.Success(state);
            }
            if (state.Guild.Recruits.Count >= OpeningFlowState.RequiredOpeningRecruitCount)
            {
                return Result<CampaignState>.Failure("M1_OPENING_ROSTER_FULL");
            }
            if (!ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                    applicant.RecruitId,
                    applicant.SignatureId,
                    RecruitAuthorityKind.Normal))
            {
                return Result<CampaignState>.Failure("M1_PROTECTED_ACTOR_CANNOT_BE_SIGNED");
            }
            if (flow.SigningCreditRemaining < applicant.SigningCostTreasuryXp)
            {
                return Result<CampaignState>.Failure("M1_INSUFFICIENT_CHARTER_SIGNING_CREDIT");
            }

            var recruits = Copy(state.Guild.Recruits);
            recruits.Add(new RecruitState(
                applicant.RecruitId,
                applicant.CurrentHp,
                applicant.MaximumHp,
                applicant.CurrentMp,
                applicant.MaximumMp,
                applicant.DisplayName,
                applicant.Kind == ApplicantKind.Signature ? RecruitOriginKind.Signature : RecruitOriginKind.Procedural,
                applicant.SignatureId,
                applicant.RaceId,
                applicant.WorldId,
                applicant.ClassTendencyId,
                applicant.LeadershipBand,
                applicant.PotentialBasisPoints,
                RecruitAuthorityKind.Normal,
                applicant.CanonicalApplicantJson,
                applicant.CanonicalScoutingReportJson,
                applicant.OpeningLoadout,
                applicant.VitalsInitialized,
                applicant.TutorialAliasId,
                applicant.AuthoredStableRecruitId,
                applicant.LeadershipScore,
                applicant.TacticalAptitude));

            var inventory = Copy(state.Guild.Inventory);
            for (var itemIndex = 0; itemIndex < applicant.OpeningEquipment.Count; itemIndex++)
            {
                var item = applicant.OpeningEquipment[itemIndex];
                if (FindInventoryIndex(inventory, item.InstanceId) >= 0 ||
                    IsItemEquipped(state.Guild.Recruits, item.InstanceId))
                {
                    return Result<CampaignState>.Failure("M1_DUPLICATE_EQUIPMENT_INSTANCE");
                }
                if (!LoadoutContains(applicant.OpeningLoadout, item.InstanceId)) inventory.Add(item);
            }

            var completed = recruits.Count == OpeningFlowState.RequiredOpeningRecruitCount;
            var guild = state.Guild.With(state.Guild.TreasuryXp, recruits, state.Guild.Unions, inventory);
            return Result<CampaignState>.Success(state.With(
                guild,
                NewFlow(
                    flow,
                    completed ? OpeningStage.Equipment : OpeningStage.Recruitment,
                    flow.CivicCharterAccepted,
                    flow.ApplicantBoard,
                    flow.ApplicantBoardCommitted,
                    flow.SigningCreditTotal,
                    flow.SigningCreditRemaining - applicant.SigningCostTreasuryXp,
                    completed,
                    false,
                    false,
                    completed ? "autosave_recruits" : flow.LastCheckpointId)));
        }

        public Result<CampaignState> EquipItem(
            CampaignState state,
            string recruitId,
            string slotId,
            string itemInstanceId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            if (!EquipmentSlotIds.IsOpeningSlot(slotId))
            {
                return Result<CampaignState>.Failure("M1_EQUIPMENT_SLOT_INVALID");
            }
            var recruitIndex = FindRecruitIndex(state.Guild.Recruits, recruitId);
            if (recruitIndex < 0) return Result<CampaignState>.Failure("M1_RECRUIT_NOT_FOUND");
            var recruit = state.Guild.Recruits[recruitIndex];
            if (!ProtectedActorPolicy.CanUseNormalEquipment(recruit))
            {
                return Result<CampaignState>.Failure("M1_PROTECTED_ACTOR_CANNOT_EQUIP");
            }
            var inventory = Copy(state.Guild.Inventory);
            var itemIndex = FindInventoryIndex(inventory, itemInstanceId);
            if (itemIndex < 0) return Result<CampaignState>.Failure("M1_INVENTORY_ITEM_NOT_FOUND");
            var item = inventory[itemIndex];
            if (item.PlayerLocked) return Result<CampaignState>.Failure("M1_LOCKED_EQUIPMENT_REQUIRES_UNLOCK");
            if (!SssTenV4Inventory090.CanEquip(recruit, item, slotId, out var equipReason))
                return Result<CampaignState>.Failure(
                    string.IsNullOrWhiteSpace(equipReason)
                        ? "M1_ITEM_SLOT_INCOMPATIBLE"
                        : equipReason);

            var assignments = Copy(recruit.Equipment.Assignments);
            var current = recruit.Equipment.Find(slotId);
            if (current != null && current.Item.PlayerLocked)
            {
                return Result<CampaignState>.Failure("M1_LOCKED_EQUIPMENT_REQUIRES_UNLOCK");
            }
            RemoveAssignment(assignments, slotId);
            inventory.RemoveAt(itemIndex);
            if (current != null) inventory.Add(current.Item);
            assignments.Add(new EquipmentSlotAssignmentState(slotId, item));

            var recruits = Copy(state.Guild.Recruits);
            recruits[recruitIndex] = recruit.WithEquipment(new EquipmentLoadoutState(assignments));
            var guild = state.Guild.With(state.Guild.TreasuryXp, recruits, state.Guild.Unions, inventory);
            return Result<CampaignState>.Success(state.With(
                guild,
                EquipmentChangedFlow(state.OpeningFlow)));
        }

        public Result<CampaignState> UnequipItem(CampaignState state, string recruitId, string slotId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var recruitIndex = FindRecruitIndex(state.Guild.Recruits, recruitId);
            if (recruitIndex < 0) return Result<CampaignState>.Failure("M1_RECRUIT_NOT_FOUND");
            var recruit = state.Guild.Recruits[recruitIndex];
            if (!ProtectedActorPolicy.CanUseNormalEquipment(recruit))
            {
                return Result<CampaignState>.Failure("M1_PROTECTED_ACTOR_CANNOT_EQUIP");
            }
            var current = recruit.Equipment.Find(slotId);
            if (current == null) return Result<CampaignState>.Success(state);
            if (current.Item.PlayerLocked)
            {
                return Result<CampaignState>.Failure("M1_LOCKED_EQUIPMENT_REQUIRES_UNLOCK");
            }
            var assignments = Copy(recruit.Equipment.Assignments);
            RemoveAssignment(assignments, slotId);
            var recruits = Copy(state.Guild.Recruits);
            recruits[recruitIndex] = recruit.WithEquipment(new EquipmentLoadoutState(assignments));
            var inventory = Copy(state.Guild.Inventory);
            inventory.Add(current.Item);
            return Result<CampaignState>.Success(state.With(
                state.Guild.With(state.Guild.TreasuryXp, recruits, state.Guild.Unions, inventory),
                EquipmentChangedFlow(state.OpeningFlow)));
        }

        public Result<CampaignState> SetEquipmentLock(
            CampaignState state,
            string recruitId,
            string slotId,
            bool playerLocked)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var recruitIndex = FindRecruitIndex(state.Guild.Recruits, recruitId);
            if (recruitIndex < 0) return Result<CampaignState>.Failure("M1_RECRUIT_NOT_FOUND");
            var recruit = state.Guild.Recruits[recruitIndex];
            if (!ProtectedActorPolicy.CanUseNormalEquipment(recruit))
            {
                return Result<CampaignState>.Failure("M1_PROTECTED_ACTOR_CANNOT_EQUIP");
            }
            var current = recruit.Equipment.Find(slotId);
            if (current == null) return Result<CampaignState>.Failure("M1_CANNOT_LOCK_EMPTY_SLOT");
            if (current.Item.PlayerLocked == playerLocked) return Result<CampaignState>.Success(state);
            var assignments = Copy(recruit.Equipment.Assignments);
            RemoveAssignment(assignments, slotId);
            assignments.Add(new EquipmentSlotAssignmentState(slotId, current.Item.WithPlayerLock(playerLocked)));
            var recruits = Copy(state.Guild.Recruits);
            recruits[recruitIndex] = recruit.WithEquipment(new EquipmentLoadoutState(assignments));
            return Result<CampaignState>.Success(state.With(
                state.Guild.With(state.Guild.TreasuryXp, recruits, state.Guild.Unions, state.Guild.Inventory),
                EquipmentChangedFlow(state.OpeningFlow)));
        }

        public Result<CampaignState> CompleteEquipmentReview(CampaignState state)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var flow = state.OpeningFlow;
            if (!flow.RecruitmentCompleted) return Result<CampaignState>.Failure("M1_OPENING_ROSTER_INCOMPLETE");
            for (var recruitIndex = 0; recruitIndex < state.Guild.Recruits.Count; recruitIndex++)
            {
                if (state.Guild.Recruits[recruitIndex].Equipment.Find(EquipmentSlotIds.BodyArmor) == null)
                {
                    return Result<CampaignState>.Failure("M1_BODY_ARMOR_REQUIRED_FOR_ALL_OPENING_RECRUITS");
                }
            }
            return Result<CampaignState>.Success(state.With(
                state.Guild,
                NewFlow(
                    flow,
                    flow.Stage == OpeningStage.Complete
                        ? OpeningStage.Complete
                        : OpeningStage.UnionBuilder,
                    flow.CivicCharterAccepted,
                    flow.ApplicantBoard,
                    flow.ApplicantBoardCommitted,
                    flow.SigningCreditTotal,
                    flow.SigningCreditRemaining,
                    flow.RecruitmentCompleted,
                    true,
                    flow.UnionBuilderCompleted,
                    flow.LastCheckpointId)));
        }

        public Result<CampaignState> AssignRecruitToUnion(
            CampaignState state,
            string recruitId,
            int unionIndex,
            int slotIndex) =>
            AssignRecruitToUnionCore109(state, recruitId, unionIndex, slotIndex, false, null);

        // Click placement names the exact empty slot or explicitly confirmed
        // occupant. It must not silently append elsewhere or move a hero that
        // ceased to be a reserve while the player was choosing a destination.
        public Result<CampaignState> AssignReserveRecruitToUnion109(
            CampaignState state,
            string recruitId,
            int unionIndex,
            int slotIndex,
            string expectedOccupantId) =>
            AssignRecruitToUnionCore109(state, recruitId, unionIndex, slotIndex, true, expectedOccupantId);

        private Result<CampaignState> AssignRecruitToUnionCore109(
            CampaignState state,
            string recruitId,
            int unionIndex,
            int slotIndex,
            bool exactReservePlacement,
            string expectedOccupantId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (unionIndex < 0 || unionIndex >= unions.Count ||
                slotIndex < 0 || slotIndex >= NormalUnionPlanRules.MaximumMembersPerUnion)
            {
                return Result<CampaignState>.Failure("M1_UNION_SLOT_INVALID");
            }
            var recruitIndex = FindRecruitIndex(state.Guild.Recruits, recruitId);
            if (recruitIndex < 0) return Result<CampaignState>.Failure("M1_RECRUIT_NOT_FOUND");
            if (GuildMemberDeploymentPolicy017D.IsTraining(state.Guild.GuildCity, recruitId))
            {
                return Result<CampaignState>.Failure(
                    GuildMemberDeploymentPolicy017D.TrainingUnavailableError);
            }
            if (!ProtectedActorPolicy.CanEnterNormalUnion(state.Guild.Recruits[recruitIndex]))
            {
                return Result<CampaignState>.Failure("M1_PROTECTED_ACTOR_CANNOT_ENTER_NORMAL_UNION");
            }

            var sourceUnionIndex = -1;
            var sourceSlotIndex = -1;
            for (var index = 0; index < unions.Count; index++)
            {
                for (var memberIndex = 0; memberIndex < unions[index].MemberRecruitIds.Count; memberIndex++)
                {
                    if (!StringComparer.Ordinal.Equals(unions[index].MemberRecruitIds[memberIndex], recruitId)) continue;
                    sourceUnionIndex = index;
                    sourceSlotIndex = memberIndex;
                    break;
                }
                if (sourceUnionIndex >= 0) break;
            }

            var target = unions[unionIndex];
            var targetMembers = target.MemberRecruitIds;
            if (exactReservePlacement)
            {
                if (sourceUnionIndex >= 0)
                    return Result<CampaignState>.Failure("M1_SELECTED_HERO_NO_LONGER_IN_RESERVE");
                if (slotIndex > targetMembers.Count)
                    return Result<CampaignState>.Failure("M1_FILL_UNION_SLOTS_IN_ORDER");
                var currentOccupant = slotIndex < targetMembers.Count ? targetMembers[slotIndex] : null;
                if (!StringComparer.Ordinal.Equals(currentOccupant ?? string.Empty, expectedOccupantId ?? string.Empty))
                    return Result<CampaignState>.Failure("M1_RESERVE_DESTINATION_CHANGED");
            }
            if (slotIndex > targetMembers.Count)
            {
                if (sourceUnionIndex >= 0 || targetMembers.Count >= NormalUnionPlanRules.MaximumMembersPerUnion)
                {
                    return Result<CampaignState>.Failure("M1_FILL_UNION_SLOTS_IN_ORDER");
                }
                slotIndex = targetMembers.Count;
            }
            if (sourceUnionIndex == unionIndex && sourceSlotIndex == slotIndex)
            {
                return Result<CampaignState>.Success(state);
            }

            var targetOccupant = slotIndex < targetMembers.Count
                ? targetMembers[slotIndex]
                : null;
            if (sourceUnionIndex < 0 && !string.IsNullOrWhiteSpace(targetOccupant) && !exactReservePlacement)
            {
                return Result<CampaignState>.Failure("M1_PLACE_UNASSIGNED_RECRUIT_IN_EMPTY_SLOT");
            }

            if (sourceUnionIndex == unionIndex)
            {
                var reordered = Copy(target.MemberRecruitIds);
                if (slotIndex < reordered.Count)
                {
                    var displaced = reordered[slotIndex];
                    reordered[slotIndex] = recruitId;
                    reordered[sourceSlotIndex] = displaced;
                }
                else
                {
                    reordered.RemoveAt(sourceSlotIndex);
                    reordered.Add(recruitId);
                }
                unions[unionIndex] = CopyUnion(
                    target,
                    FirstMember(reordered),
                    reordered,
                    DefaultFormation(target.FormationId),
                    DefaultDoctrine(target.DoctrineId));
                return ApplyUnionChange(state, unions);
            }

            if (sourceUnionIndex >= 0)
            {
                unions[sourceUnionIndex] = WithoutMember(unions[sourceUnionIndex], recruitId);
            }

            target = unions[unionIndex];
            var mutableTargetMembers = Copy(targetMembers);
            if (slotIndex == mutableTargetMembers.Count) mutableTargetMembers.Add(recruitId);
            else mutableTargetMembers[slotIndex] = recruitId;
            unions[unionIndex] = CopyUnion(
                target,
                FirstMember(mutableTargetMembers),
                mutableTargetMembers,
                DefaultFormation(target.FormationId),
                DefaultDoctrine(target.DoctrineId));

            if (!string.IsNullOrWhiteSpace(targetOccupant) && sourceUnionIndex >= 0)
            {
                var source = unions[sourceUnionIndex];
                var sourceMembers = Copy(source.MemberRecruitIds);
                sourceMembers.Insert(Math.Min(sourceSlotIndex, sourceMembers.Count), targetOccupant);
                unions[sourceUnionIndex] = CopyUnion(
                    source,
                    FirstMember(sourceMembers),
                    sourceMembers,
                    DefaultFormation(source.FormationId),
                    DefaultDoctrine(source.DoctrineId));
            }
            else if (sourceUnionIndex >= 0)
            {
                var source = unions[sourceUnionIndex];
                unions[sourceUnionIndex] = CopyUnion(
                    source,
                    FirstMember(source.MemberRecruitIds),
                    source.MemberRecruitIds,
                    source.FormationId,
                    source.DoctrineId);
            }
            return ApplyUnionChange(state, unions);
        }

        /// <summary>
        /// Materializes the ten named Lantern Patrol members into legal Union plans
        /// as part of the already-committed first-hour rescue transaction. This is
        /// intentionally distinct from player-facing Union editing: it can run only
        /// at the exact rescued Gate-Eater objective, never moves an existing member,
        /// and never relaxes the active-adventure roster lock.
        /// </summary>
        public Result<CampaignState> MaterializeFirstHourLanternPatrolUnions071(
            CampaignState state)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;

            var city = state.Guild?.GuildCity;
            var contract = city?.ActiveContract;
            var expedition = city?.Expedition;
            if (expedition != null && Contains(
                    expedition.ObjectiveFlags,
                    GuildCityExpeditionService017D.EncounterClearedFlag("N13")))
            {
                return Result<CampaignState>.Failure(
                    "GC017D_FIRST_HOUR_PATROL_RESCUE_TOO_LATE");
            }
            if (contract == null || contract.Completed || contract.Failed ||
                !StringComparer.Ordinal.Equals(
                    contract.ContractId,
                    GuildCityExpeditionService017D.FirstStoryContractId066) ||
                expedition == null || expedition.Status != ExpeditionStatus017D.Active ||
                !StringComparer.Ordinal.Equals(contract.CommitId, expedition.ContractCommitId) ||
                !StringComparer.Ordinal.Equals(
                    contract.BoardId,
                    GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071) ||
                !StringComparer.Ordinal.Equals(
                    expedition.BoardId,
                    GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071) ||
                !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13") ||
                city.PendingEncounter != null || city.PendingBattleReturn != null ||
                !GuildCityExpeditionService017D.HasFirstHourLanternPatrolRescued071(expedition))
            {
                return Result<CampaignState>.Failure(
                    "M1_FIRST_HOUR_PATROL_RESCUE_AUTHORITY_REQUIRED");
            }

            // Supported older saves can still carry the retired FORMATION_LINE ID.
            // Normalize through the same deterministic boundary as ordinary Union
            // editing before validating; every other unknown formation remains
            // unchanged and therefore continues to fail closed below.
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            var normalizedGuild = state.Guild.With(
                state.Guild.TreasuryXp,
                state.Guild.Recruits,
                unions,
                state.Guild.Inventory);
            var currentValidation = ValidateGuildUnionPlans(normalizedGuild);
            if (!currentValidation.IsSuccess)
                return Result<CampaignState>.Failure(CopyErrors(currentValidation.Errors));

            var assignedRecruitIds = new HashSet<string>(
                unions.Where(value => value != null)
                    .SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>()),
                StringComparer.Ordinal);

            for (var patrolIndex = 0;
                 patrolIndex < FirstHourRosterService071.PatrolStableRecruitIds.Count;
                 patrolIndex++)
            {
                var stableRecruitId =
                    FirstHourRosterService071.PatrolStableRecruitIds[patrolIndex];
                var recruit = state.Guild.Recruits.FirstOrDefault(value =>
                    value != null &&
                    (StringComparer.Ordinal.Equals(
                         value.AuthoredStableRecruitId, stableRecruitId) ||
                     StringComparer.Ordinal.Equals(value.RecruitId, stableRecruitId)));
                if (recruit == null)
                    return Result<CampaignState>.Failure(
                        "FH071_REINFORCEMENT_RECRUIT_MISSING:" + stableRecruitId);
                if (assignedRecruitIds.Contains(recruit.RecruitId)) continue;
                if (GuildMemberDeploymentPolicy017D.IsTraining(city, recruit.RecruitId))
                    return Result<CampaignState>.Failure(
                        GuildMemberDeploymentPolicy017D.TrainingUnavailableError);
                if (!ProtectedActorPolicy.CanEnterNormalUnion(recruit))
                    return Result<CampaignState>.Failure(
                        "M1_PROTECTED_ACTOR_CANNOT_ENTER_NORMAL_UNION");

                var unionIndex = FirstAvailableNormalUnion(unions);
                if (unionIndex < 0 && unions.Count < NormalUnionPlanRules.MaximumPlanCount)
                {
                    unions.Add(NewOpeningUnionDraft(NextOpeningUnionOrdinal(unions)));
                    unionIndex = FirstAvailableNormalUnion(unions);
                }

                // A fully occupied ten-Union roster keeps any remaining rescued
                // members as permanent reserves instead of displacing player plans.
                if (unionIndex < 0) break;

                var target = unions[unionIndex];
                var members = Copy(target.MemberRecruitIds);
                members.Add(recruit.RecruitId);
                unions[unionIndex] = CopyUnion(
                    target,
                    FirstMember(members),
                    members,
                    DefaultFormation(target.FormationId, unionIndex + 1),
                    DefaultDoctrine(target.DoctrineId));
                assignedRecruitIds.Add(recruit.RecruitId);
            }

            var candidate = ApplyUnionChange(state, unions);
            if (!candidate.IsSuccess) return candidate;
            var finalValidation = ValidateGuildUnionPlans(candidate.Value.Guild);
            return finalValidation.IsSuccess
                ? candidate
                : Result<CampaignState>.Failure(CopyErrors(finalValidation.Errors));
        }

        private static int FirstAvailableNormalUnion(IReadOnlyList<UnionState> unions)
        {
            if (unions == null) return -1;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                if (union != null && union.Kind == UnionKind.Normal &&
                    union.MemberRecruitIds.Count < NormalUnionPlanRules.MaximumMembersPerUnion)
                    return unionIndex;
            }
            return -1;
        }

        public Result<CampaignState> UnassignRecruitFromUnion(CampaignState state, string recruitId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                if (!Contains(unions[unionIndex].MemberRecruitIds, recruitId)) continue;
                unions[unionIndex] = WithoutMember(unions[unionIndex], recruitId);
                return ApplyUnionChange(state, unions);
            }
            return Result<CampaignState>.Failure("M1_RECRUIT_NOT_ASSIGNED_TO_UNION");
        }

        /// <summary>
        /// Builds an optional, deterministic role-first Union plan in one immutable
        /// transaction. It uses the same roster lock, member cap, formation defaults,
        /// and final legality validation as manual editing. Protected or training
        /// members stay in reserve, and no more than the normal ten-by-six field cap
        /// can be selected.
        /// </summary>
        public Result<CampaignState> ApplySuggestedRoleUnions087(CampaignState state)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;

            var candidates = state.Guild.Recruits
                .Where(value => value != null &&
                                ProtectedActorPolicy.CanEnterNormalUnion(value) &&
                                !GuildMemberDeploymentPolicy017D.IsTraining(
                                    state.Guild.GuildCity, value.RecruitId))
                .Select((value, index) => new
                {
                    Recruit = value,
                    RosterIndex = index,
                    Role = SuggestedUnionRole087(value.ClassTendencyId)
                })
                .ToArray();
            if (candidates.Length < 2)
                return Result<CampaignState>.Failure(
                    "M1_SUGGESTED_UNIONS_REQUIRE_TWO_AVAILABLE_RECRUITS");

            var roleOrder = new[]
            {
                "HEALER", "TANK", "MAGE", "RANGER", "ROGUE", "WARRIOR", "FLEX"
            };
            var groups = new List<List<string>>();
            for (var roleIndex = 0;
                 roleIndex < roleOrder.Length &&
                 groups.Count < NormalUnionPlanRules.MaximumPlanCount;
                 roleIndex++)
            {
                var role = roleOrder[roleIndex];
                var members = candidates
                    .Where(value => StringComparer.Ordinal.Equals(value.Role, role))
                    .OrderByDescending(value => value.Recruit.LeadershipScore)
                    .ThenByDescending(value => value.Recruit.TacticalAptitude)
                    .ThenBy(value => value.RosterIndex)
                    .Select(value => value.Recruit.RecruitId)
                    .ToArray();
                for (var start = 0;
                     start < members.Length &&
                     groups.Count < NormalUnionPlanRules.MaximumPlanCount;
                     start += NormalUnionPlanRules.MaximumMembersPerUnion)
                    groups.Add(members
                        .Skip(start)
                        .Take(NormalUnionPlanRules.MaximumMembersPerUnion)
                        .ToList());
            }

            // A legal deployment always has at least two active Unions. When a
            // small roster is entirely one role, split that role into two teams
            // without mixing its training focus.
            if (groups.Count == 1 && groups[0].Count >= 2)
            {
                var split = Math.Max(1, groups[0].Count / 2);
                var second = groups[0].Skip(split).ToList();
                groups[0] = groups[0].Take(split).ToList();
                groups.Add(second);
            }
            if (groups.Count < NormalUnionPlanRules.MinimumUsedUnionCount)
                return Result<CampaignState>.Failure(
                    "M1_SUGGESTED_UNIONS_REQUIRE_TWO_ROLE_TEAMS");

            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (unions.Any(value => value == null || value.Kind != UnionKind.Normal) ||
                unions.Count > NormalUnionPlanRules.MaximumPlanCount)
                return Result<CampaignState>.Failure(
                    "M1_SUGGESTED_UNIONS_REQUIRE_NORMAL_PLANS");
            while (unions.Count < groups.Count)
                unions.Add(NewOpeningUnionDraft(NextOpeningUnionOrdinal(unions)));

            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var source = unions[unionIndex];
                var members = unionIndex < groups.Count
                    ? (IReadOnlyList<string>)groups[unionIndex]
                    : Array.Empty<string>();
                unions[unionIndex] = CopyUnion(
                    source,
                    FirstMember(members),
                    members,
                    DefaultFormation(source.FormationId, unionIndex + 1),
                    DefaultDoctrine(source.DoctrineId));
            }

            var candidate = ApplyUnionChange(state, unions);
            if (!candidate.IsSuccess) return candidate;
            var validation = state.OpeningFlow.Stage == OpeningStage.Complete
                ? ValidateGuildUnionPlans(candidate.Value.Guild)
                : ValidateOpeningUnions(candidate.Value.Guild);
            return validation.IsSuccess
                ? candidate
                : Result<CampaignState>.Failure(CopyErrors(validation.Errors));
        }

        public static string SuggestedUnionRole087(string classIdentity)
        {
            var value = (classIdentity ?? string.Empty).ToUpperInvariant();
            if (value.Contains("PRIEST") || value.Contains("HEAL") ||
                value.Contains("MEDIC") || value.Contains("CLERIC") ||
                value.Contains("RESTOR") || value.Contains("SUPPORT"))
                return "HEALER";
            if (value.Contains("GUARD") || value.Contains("TANK") ||
                value.Contains("SHIELD") || value.Contains("WARDEN"))
                return "TANK";
            if (value.Contains("MAGE") || value.Contains("MYST") ||
                value.Contains("RUNE") || value.Contains("SPELL") ||
                value.Contains("HEX") || value.Contains("AETHER"))
                return "MAGE";
            if (value.Contains("RANGER") || value.Contains("ARCHER") ||
                value.Contains("BOW") || value.Contains("GUNNER") ||
                value.Contains("MARKSMAN"))
                return "RANGER";
            if (value.Contains("ROGUE") || value.Contains("SCOUT") ||
                value.Contains("DUSK") || value.Contains("ASSASSIN"))
                return "ROGUE";
            if (value.Contains("WARRIOR") || value.Contains("FIGHT") ||
                value.Contains("VANGUARD") || value.Contains("GREATSWORD") ||
                value.Contains("GLAIVE") || value.Contains("LANCER"))
                return "WARRIOR";
            return "FLEX";
        }

        public Result<CampaignState> AddOpeningUnion(CampaignState state)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;

            // The first click in a fresh founding campaign is a quick-start action,
            // not a request for a third empty plan. Build the recommended two legal
            // three-member founder Unions in one deterministic transaction. Campaign
            // capacity is six members per Union, so quick-start indexing must use the
            // separate founding recommendation rather than the system maximum.
            if (state.Guild.Unions.Count == 0)
            {
                if (state.Guild.Recruits.Count < OpeningFlowState.RequiredOpeningRecruitCount)
                {
                    return Result<CampaignState>.Failure("M1_OPENING_ROSTER_INCOMPLETE");
                }

                var foundingUnions = new List<UnionState>(OpeningFlowState.RecommendedOpeningUnionCount);
                for (var unionIndex = 0; unionIndex < OpeningFlowState.RecommendedOpeningUnionCount; unionIndex++)
                {
                    var members = new List<string>(OpeningFlowState.RecommendedOpeningUnionMemberCount);
                    for (var memberIndex = 0;
                         memberIndex < OpeningFlowState.RecommendedOpeningUnionMemberCount;
                         memberIndex++)
                    {
                        var recruit = state.Guild.Recruits[
                            unionIndex * OpeningFlowState.RecommendedOpeningUnionMemberCount + memberIndex];
                        if (!ProtectedActorPolicy.CanEnterNormalUnion(recruit))
                        {
                            return Result<CampaignState>.Failure(
                                "M1_PROTECTED_ACTOR_CANNOT_ENTER_NORMAL_UNION");
                        }
                        members.Add(recruit.RecruitId);
                    }

                    foundingUnions.Add(NewOpeningFoundingUnion(unionIndex + 1, members));
                }
                return ApplyUnionChange(state, foundingUnions);
            }

            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (unions.Count >= NormalUnionPlanRules.MaximumPlanCount)
            {
                return Result<CampaignState>.Failure("M1_MAXIMUM_NORMAL_UNION_PLANS_REACHED");
            }

            var ordinal = NextOpeningUnionOrdinal(unions);
            unions.Add(NewOpeningUnionDraft(ordinal));
            return ApplyUnionChange(state, unions);
        }

        public Result<CampaignState> RemoveOpeningUnion(CampaignState state, int unionIndex)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (unions.Count <= OpeningFlowState.MinimumOpeningUnionCount)
            {
                return Result<CampaignState>.Failure("M1_MINIMUM_TWO_OPENING_UNION_PLANS");
            }
            if (!IsOpeningUnionIndex(unions, unionIndex))
            {
                return Result<CampaignState>.Failure("M1_UNION_INDEX_INVALID");
            }
            if (unions[unionIndex].MemberRecruitIds.Count > 0)
            {
                return Result<CampaignState>.Failure("M1_ONLY_EMPTY_UNION_PLAN_CAN_BE_REMOVED");
            }

            unions.RemoveAt(unionIndex);
            return ApplyUnionChange(state, unions);
        }

        public Result<CampaignState> SetUnionLeader(CampaignState state, int unionIndex, string recruitId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (!IsOpeningUnionIndex(unions, unionIndex)) return Result<CampaignState>.Failure("M1_UNION_INDEX_INVALID");
            if (!Contains(unions[unionIndex].MemberRecruitIds, recruitId))
            {
                return Result<CampaignState>.Failure("M1_UNION_LEADER_MUST_BE_MEMBER");
            }
            var current = unions[unionIndex];
            var reordered = Copy(current.MemberRecruitIds);
            for (var index = reordered.Count - 1; index >= 0; index--)
            {
                if (StringComparer.Ordinal.Equals(reordered[index], recruitId)) reordered.RemoveAt(index);
            }
            reordered.Insert(0, recruitId);
            unions[unionIndex] = CopyUnion(
                current,
                FirstMember(reordered),
                reordered,
                current.FormationId,
                current.DoctrineId);
            return ApplyUnionChange(
                state,
                unions,
                explicitlySelectedUnionId: current.UnionId,
                explicitlySelectedLeaderId: recruitId);
        }

        private static Result<CampaignState> RequireUnionRosterMutable084(
            CampaignState state)
        {
            if (!UnionBattlePlanRules132.CanEdit(state, out var reason132))
                return Result<CampaignState>.Failure(reason132);
            return Result<CampaignState>.Success(state);
        }

        public Result<CampaignState> SetFormation(CampaignState state, int unionIndex, string formationId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;
            if (!OpeningUnionCatalog.IsFormation(formationId))
            {
                return Result<CampaignState>.Failure("M1_FORMATION_NOT_LEGAL_FOR_OPENING_UNION");
            }
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (!IsOpeningUnionIndex(unions, unionIndex)) return Result<CampaignState>.Failure("M1_UNION_INDEX_INVALID");
            var current = unions[unionIndex];
            unions[unionIndex] = CopyUnion(
                current,
                current.LeaderRecruitId,
                current.MemberRecruitIds,
                formationId,
                current.DoctrineId);
            return ApplyUnionChange(state, unions);
        }

        public Result<CampaignState> SetDoctrine(CampaignState state, int unionIndex, string doctrineId)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var rosterGate = RequireUnionRosterMutable084(state);
            if (!rosterGate.IsSuccess) return rosterGate;
            if (!OpeningUnionCatalog.IsDoctrine(doctrineId))
            {
                return Result<CampaignState>.Failure("M1_DOCTRINE_NOT_LEGAL_FOR_OPENING_UNION");
            }
            var unions = EnsureUnionDrafts(UnionBattlePlanRules132.Read(state));
            if (!IsOpeningUnionIndex(unions, unionIndex)) return Result<CampaignState>.Failure("M1_UNION_INDEX_INVALID");
            var current = unions[unionIndex];
            unions[unionIndex] = CopyUnion(
                current,
                current.LeaderRecruitId,
                current.MemberRecruitIds,
                current.FormationId,
                doctrineId);
            return ApplyUnionChange(state, unions);
        }

        public Result<CampaignState> CompleteOpening(CampaignState state)
        {
            var gate = RequireM1(state);
            if (!gate.IsSuccess) return gate;
            var flow = state.OpeningFlow;
            if (!flow.RecruitmentCompleted) return Result<CampaignState>.Failure("M1_OPENING_ROSTER_INCOMPLETE");
            if (!flow.EquipmentReviewCompleted) return Result<CampaignState>.Failure("M1_EQUIPMENT_REVIEW_INCOMPLETE");
            if (flow.Stage == OpeningStage.Complete && UnionBattlePlanRules132.HasCommittedRoster(state))
            {
                var ready132 = ValidateGuildUnionPlans(UnionBattlePlanRules132.ProjectGuild(state));
                return ready132.IsSuccess ? Result<CampaignState>.Success(state)
                    : Result<CampaignState>.Failure(CopyErrors(ready132.Errors));
            }
            state = UnionBattlePlanRules132.PromoteWhenIdle(state);
            var finalizedGuild = FinalizeOpeningUnionResources(
                NormalizeOpeningUnionLeaders(TrimUnusedUnionDrafts(state.Guild)));
            var unionValidation = flow.Stage == OpeningStage.Complete
                ? ValidateGuildUnionPlans(finalizedGuild)
                : ValidateOpeningUnions(finalizedGuild);
            if (!unionValidation.IsSuccess)
            {
                return Result<CampaignState>.Failure(CopyErrors(unionValidation.Errors));
            }
            return Result<CampaignState>.Success(state.With(
                finalizedGuild,
                NewFlow(
                    flow,
                    OpeningStage.Complete,
                    flow.CivicCharterAccepted,
                    flow.ApplicantBoard,
                    flow.ApplicantBoardCommitted,
                    flow.SigningCreditTotal,
                    flow.SigningCreditRemaining,
                    true,
                    true,
                    true,
                    "autosave_unions")));
        }

        public Result<bool> ValidateOpeningUnions(GuildState guild) =>
            ValidateUnionPlans(guild, requireExactlySixAssigned: true);

        public Result<bool> ValidateGuildUnionPlans(GuildState guild) =>
            ValidateUnionPlans(guild, requireExactlySixAssigned: false);

        private static Result<bool> ValidateUnionPlans(
            GuildState guild,
            bool requireExactlySixAssigned)
        {
            if (guild == null) return Result<bool>.Failure("M1_GUILD_REQUIRED");
            if (guild.Unions.Count < NormalUnionPlanRules.MinimumUsedUnionCount ||
                guild.Unions.Count > NormalUnionPlanRules.MaximumPlanCount)
            {
                return Result<bool>.Failure("M1_NORMAL_UNION_PLAN_COUNT_OUT_OF_RANGE");
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var usedUnionCount = 0;
            for (var unionIndex = 0; unionIndex < guild.Unions.Count; unionIndex++)
            {
                var union = guild.Unions[unionIndex];
                if (union.Kind != UnionKind.Normal) return Result<bool>.Failure("M1_OPENING_UNION_MUST_BE_NORMAL");
                if (union.MemberRecruitIds.Count == 0) continue;
                usedUnionCount++;
                if (union.MemberRecruitIds.Count > NormalUnionPlanRules.MaximumMembersPerUnion)
                {
                    return Result<bool>.Failure("M1_NORMAL_UNION_SUPPORTS_ONE_TO_SIX_MEMBERS");
                }
                if (!Contains(union.MemberRecruitIds, union.LeaderRecruitId))
                {
                    return Result<bool>.Failure("M1_UNION_LEADER_MUST_BE_MEMBER");
                }
                if (!OpeningUnionCatalog.IsFormation(union.FormationId))
                {
                    return Result<bool>.Failure("M1_FORMATION_NOT_LEGAL_FOR_OPENING_UNION");
                }
                if (!OpeningUnionCatalog.IsDoctrine(union.DoctrineId))
                {
                    return Result<bool>.Failure("M1_DOCTRINE_NOT_LEGAL_FOR_OPENING_UNION");
                }
                for (var memberIndex = 0; memberIndex < union.MemberRecruitIds.Count; memberIndex++)
                {
                    var memberId = union.MemberRecruitIds[memberIndex];
                    if (!seen.Add(memberId)) return Result<bool>.Failure("M1_RECRUIT_DOUBLE_BOOKED_IN_UNIONS");
                    var recruitIndex = FindRecruitIndex(guild.Recruits, memberId);
                    if (recruitIndex < 0) return Result<bool>.Failure("M1_UNION_MEMBER_NOT_IN_OWNED_ROSTER");
                    if (!ProtectedActorPolicy.CanEnterNormalUnion(guild.Recruits[recruitIndex]))
                    {
                        return Result<bool>.Failure("M1_PROTECTED_ACTOR_CANNOT_ENTER_NORMAL_UNION");
                    }
                    if (GuildMemberDeploymentPolicy017D.IsTraining(guild.GuildCity, memberId))
                    {
                        return Result<bool>.Failure(
                            GuildMemberDeploymentPolicy017D.TrainingUnavailableError);
                    }
                }
            }
            if (usedUnionCount < NormalUnionPlanRules.MinimumUsedUnionCount)
            {
                return Result<bool>.Failure("M1_REQUIRES_AT_LEAST_TWO_USED_NORMAL_UNIONS");
            }
            if (requireExactlySixAssigned &&
                seen.Count != OpeningFlowState.RequiredOpeningRecruitCount)
            {
                return Result<bool>.Failure("M1_ALL_SIX_RECRUITS_MUST_BE_ASSIGNED");
            }
            return Result<bool>.Success(true);
        }

        private static Result<bool> ValidateFrozenTutorialBoard(ApplicantBoardState board)
        {
            if (!StringComparer.Ordinal.Equals(board.GenerationKey, ApplicantBoardState.FrozenTutorialSeedId))
            {
                return Result<bool>.Failure("M1_TUTORIAL_SEED_MISMATCH");
            }
            if (!StringComparer.Ordinal.Equals(board.BoardId, ApplicantBoardState.FrozenTutorialBoardId) ||
                board.RefreshOrdinal != ApplicantBoardState.FrozenTutorialRefreshOrdinal)
            {
                return Result<bool>.Failure("M1_TUTORIAL_BOARD_IDENTITY_MISMATCH");
            }
            if (board.Applicants.Count != OpeningFlowState.RequiredOpeningRecruitCount)
            {
                return Result<bool>.Failure("M1_TUTORIAL_BOARD_REQUIRES_SIX_APPLICANTS");
            }
            if (!StringComparer.Ordinal.Equals(
                    board.CommittedApplicantsHash,
                    ApplicantBoardState.FrozenTutorialApplicantsHash))
            {
                return Result<bool>.Failure("M1_TUTORIAL_APPLICANT_PAYLOAD_MISMATCH");
            }
            var proceduralSeeds = new[]
            {
                "TUT_APPLICANT_001",
                "TUT_APPLICANT_002",
                "TUT_APPLICANT_003",
                "TUT_APPLICANT_004"
            };
            for (var index = 0; index < 4; index++)
            {
                var applicant = board.Applicants[index];
                if (applicant.Slot != index + 1 ||
                    applicant.Kind != ApplicantKind.Procedural ||
                    !StringComparer.Ordinal.Equals(applicant.SourceSeed, proceduralSeeds[index]) ||
                    !string.IsNullOrEmpty(applicant.SignatureId))
                {
                    return Result<bool>.Failure("M1_TUTORIAL_PROCEDURAL_SLOT_MISMATCH");
                }
                if (!ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                        applicant.RecruitId,
                        applicant.SignatureId,
                        RecruitAuthorityKind.Normal))
                {
                    return Result<bool>.Failure("M1_PROTECTED_ACTOR_ON_APPLICANT_BOARD");
                }
            }
            if (!MatchesSignature(
                    board.Applicants[4],
                    5,
                    "SIG_MAREN_HOLT",
                    "SIG_W01_01",
                    "SIGI_4559425B6CF6CBE6",
                    "SIGREC_MAREN_HOLT") ||
                !MatchesSignature(
                    board.Applicants[5],
                    6,
                    "SIG_ODELIA_FEN",
                    "SIG_W01_03",
                    "SIGI_CE2768FD5A985F8B",
                    "SIGREC_ODELIA_FEN"))
            {
                return Result<bool>.Failure("M1_TUTORIAL_SIGNATURE_SLOT_MISMATCH");
            }
            return Result<bool>.Success(true);
        }

        private static bool MatchesSignature(
            ApplicantSnapshotState applicant,
            int slot,
            string tutorialAliasId,
            string signatureId,
            string recruitId,
            string authoredStableRecruitId)
        {
            return applicant.Slot == slot &&
                   applicant.Kind == ApplicantKind.Signature &&
                   StringComparer.Ordinal.Equals(applicant.SignatureId, signatureId) &&
                   StringComparer.Ordinal.Equals(applicant.TutorialAliasId, tutorialAliasId) &&
                   StringComparer.Ordinal.Equals(applicant.RecruitId, recruitId) &&
                   StringComparer.Ordinal.Equals(applicant.AuthoredStableRecruitId, authoredStableRecruitId) &&
                   applicant.CanonicalApplicantJson.IndexOf(
                       "\"signatureId\":\"" + signatureId + "\"",
                       StringComparison.Ordinal) >= 0 &&
                   ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                       applicant.RecruitId,
                       applicant.SignatureId,
                       RecruitAuthorityKind.Normal);
        }

        private Result<CampaignState> ApplyUnionChange(
            CampaignState state,
            List<UnionState> unions,
            string explicitlySelectedUnionId = null,
            string explicitlySelectedLeaderId = null)
        {
            var normalizedUnions = NormalizeCovenantUnionLeaders090(
                UnionBattlePlanRules132.Read(state),
                unions,
                explicitlySelectedUnionId,
                explicitlySelectedLeaderId);
            var guild = state.Guild.With(
                state.Guild.TreasuryXp,
                state.Guild.Recruits,
                normalizedUnions,
                state.Guild.Inventory);
            if (UnionBattlePlanRules132.HasCommittedRoster(state))
            {
                try
                {
                    var plan132 = new UnionBattlePlan132(
                        SecondDimension.Determinism.CanonicalJson.Sha256Hex(state.Guild.Unions), normalizedUnions);
                    plan132.Validate(state.Guild);
                    return Result<CampaignState>.Success(state.WithNextBattleUnions132(plan132));
                }
                catch (Exception exception)
                {
                    return Result<CampaignState>.Failure("Next-battle plan was not saved: " + exception.Message);
                }
            }
            state = state.WithNextBattleUnions132(null);
            var flow = state.OpeningFlow;
            var postOpening = flow.Stage == OpeningStage.Complete;
            var complete = (postOpening
                ? ValidateGuildUnionPlans(guild)
                : ValidateOpeningUnions(guild)).IsSuccess;
            return Result<CampaignState>.Success(state.With(
                guild,
                NewFlow(
                    flow,
                    postOpening ? OpeningStage.Complete : OpeningStage.UnionBuilder,
                    flow.CivicCharterAccepted,
                    flow.ApplicantBoard,
                    flow.ApplicantBoardCommitted,
                    flow.SigningCreditTotal,
                    flow.SigningCreditRemaining,
                    flow.RecruitmentCompleted,
                    flow.EquipmentReviewCompleted,
                    complete,
                    complete ? "autosave_unions" : flow.LastCheckpointId)));
        }

        private static Result<CampaignState> RequireM1(CampaignState state)
        {
            return state == null || state.Profile == null || state.OpeningFlow == null
                ? Result<CampaignState>.Failure("M1_CAMPAIGN_STATE_REQUIRED")
                : Result<CampaignState>.Success(state);
        }

        private static OpeningFlowState EquipmentChangedFlow(OpeningFlowState flow) =>
            NewFlow(
                flow,
                flow.Stage == OpeningStage.Complete ? OpeningStage.Complete : OpeningStage.Equipment,
                flow.CivicCharterAccepted,
                flow.ApplicantBoard,
                flow.ApplicantBoardCommitted,
                flow.SigningCreditTotal,
                flow.SigningCreditRemaining,
                flow.RecruitmentCompleted,
                flow.Stage == OpeningStage.Complete && flow.EquipmentReviewCompleted,
                flow.UnionBuilderCompleted,
                flow.LastCheckpointId,
                manualEquipmentCommitObserved: true);

        private static OpeningFlowState NewFlow(
            OpeningFlowState source,
            OpeningStage stage,
            bool civicCharterAccepted,
            ApplicantBoardState applicantBoard,
            bool applicantBoardCommitted,
            int signingCreditTotal,
            int signingCreditRemaining,
            bool recruitmentCompleted,
            bool equipmentReviewCompleted,
            bool unionBuilderCompleted,
            string lastCheckpointId,
            bool? manualEquipmentCommitObserved = null) =>
            new OpeningFlowState(
                stage,
                source.TutorialSeedId,
                civicCharterAccepted,
                applicantBoard,
                applicantBoardCommitted,
                signingCreditTotal,
                signingCreditRemaining,
                recruitmentCompleted,
                equipmentReviewCompleted,
                unionBuilderCompleted,
                manualEquipmentCommitObserved ?? source.ManualEquipmentCommitObserved,
                lastCheckpointId);

        private static List<UnionState> EnsureUnionDrafts(IReadOnlyList<UnionState> source)
        {
            var copy = Copy(source);
            for (var index = 0; index < copy.Count; index++)
            {
                var union = copy[index];
                copy[index] = CopyUnion(
                    union,
                    FirstMember(union.MemberRecruitIds),
                    union.MemberRecruitIds,
                    DefaultFormation(union.FormationId, index + 1),
                    DefaultDoctrine(union.DoctrineId));
            }
            while (copy.Count < OpeningFlowState.MinimumOpeningUnionCount)
            {
                copy.Add(NewOpeningUnionDraft(NextOpeningUnionOrdinal(copy)));
            }
            return copy;
        }

        private static UnionState NewOpeningUnionDraft(int ordinal) =>
            new UnionState(
                OpeningUnionId(ordinal),
                "Opening Union " + ordinal,
                UnionKind.Normal,
                leaderRecruitId: null,
                memberRecruitIds: Array.Empty<string>(),
                formationId: DefaultFormation(null, ordinal),
                doctrineId: DefaultDoctrine(null),
                sharedAp: 0,
                cohesionBasisPoints: 10_000);

        private static UnionState NewOpeningFoundingUnion(
            int ordinal,
            IReadOnlyList<string> memberRecruitIds) =>
            new UnionState(
                OpeningUnionId(ordinal),
                "Opening Union " + ordinal,
                UnionKind.Normal,
                FirstMember(memberRecruitIds),
                memberRecruitIds,
                DefaultFormation(null, ordinal),
                DefaultDoctrine(null),
                sharedAp: 0,
                cohesionBasisPoints: 10_000);

        private static int NextOpeningUnionOrdinal(IReadOnlyList<UnionState> unions)
        {
            for (var ordinal = 1; ordinal <= NormalUnionPlanRules.MaximumPlanCount; ordinal++)
            {
                var candidate = OpeningUnionId(ordinal);
                var exists = false;
                for (var index = 0; index < unions.Count; index++)
                {
                    if (!StringComparer.Ordinal.Equals(unions[index].UnionId, candidate)) continue;
                    exists = true;
                    break;
                }
                if (!exists) return ordinal;
            }
            return unions.Count + 1;
        }

        private static string OpeningUnionId(int ordinal) =>
            "UNION_OPENING_" + (ordinal < 10 ? "0" : string.Empty) + ordinal;

        private static GuildState TrimUnusedUnionDrafts(GuildState guild)
        {
            var used = new List<UnionState>();
            for (var index = 0; index < guild.Unions.Count; index++)
            {
                if (guild.Unions[index].MemberRecruitIds.Count > 0) used.Add(guild.Unions[index]);
            }
            return guild.With(guild.TreasuryXp, guild.Recruits, used, guild.Inventory);
        }

        private static GuildState NormalizeOpeningUnionLeaders(GuildState guild)
        {
            var unions = Copy(guild.Unions);
            for (var index = 0; index < unions.Count; index++)
            {
                var union = unions[index];
                unions[index] = CopyUnion(
                    union,
                    ResolveCovenantLeader090(
                        union.MemberRecruitIds,
                        union.LeaderRecruitId),
                    union.MemberRecruitIds,
                    DefaultFormation(union.FormationId, index + 1),
                    DefaultDoctrine(union.DoctrineId));
            }
            return guild.With(guild.TreasuryXp, guild.Recruits, unions, guild.Inventory);
        }

        private static IReadOnlyList<UnionState> NormalizeCovenantUnionLeaders090(
            IReadOnlyList<UnionState> previous,
            IReadOnlyList<UnionState> candidates,
            string explicitlySelectedUnionId,
            string explicitlySelectedLeaderId)
        {
            var result = new List<UnionState>();
            if (candidates == null) return result.AsReadOnly();
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate == null)
                    throw new ArgumentException("Union plan cannot be null.", nameof(candidates));
                var existingLeader = candidate.LeaderRecruitId;
                if (StringComparer.Ordinal.Equals(
                        candidate.UnionId, explicitlySelectedUnionId))
                    existingLeader = explicitlySelectedLeaderId;
                else if (previous != null)
                {
                    var prior = previous.FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(
                            value.UnionId, candidate.UnionId));
                    if (prior != null) existingLeader = prior.LeaderRecruitId;
                }
                result.Add(CopyUnion(
                    candidate,
                    ResolveCovenantLeader090(
                        candidate.MemberRecruitIds, existingLeader),
                    candidate.MemberRecruitIds,
                    candidate.FormationId,
                    candidate.DoctrineId));
            }
            return result.AsReadOnly();
        }

        private static string ResolveCovenantLeader090(
            IReadOnlyList<string> formationOrder,
            string existingLeader)
        {
            if (formationOrder == null || formationOrder.Count == 0) return null;
            return CovenantLeadership.Resolve(
                new List<string>(formationOrder),
                existingLeader);
        }

        private static UnionState WithoutMember(UnionState union, string recruitId)
        {
            if (!Contains(union.MemberRecruitIds, recruitId)) return union;
            var members = Copy(union.MemberRecruitIds);
            for (var index = members.Count - 1; index >= 0; index--)
            {
                if (StringComparer.Ordinal.Equals(members[index], recruitId)) members.RemoveAt(index);
            }
            return CopyUnion(union, FirstMember(members), members, union.FormationId, union.DoctrineId);
        }

        private static string FirstMember(IReadOnlyList<string> members) =>
            members == null || members.Count == 0 ? null : members[0];

        private static string DefaultFormation(string formationId, int ordinal = 1)
        {
            // FORMATION_LINE was used by older Release 030 fixtures and saves before
            // the eight-choice opening catalog was frozen. Normalize it at the Union
            // builder boundary instead of making the retired ID selectable or legal.
            if (StringComparer.Ordinal.Equals(formationId, "FORMATION_LINE"))
                return "FORMATION_SKIRMISH_LINE";
            if (!string.IsNullOrWhiteSpace(formationId)) return formationId;
            switch (ordinal)
            {
                case 2: return "FORMATION_WEDGE";
                case 3: return "FORMATION_SKIRMISH_LINE";
                default: return "FORMATION_SHIELD_WALL";
            }
        }

        private static string DefaultDoctrine(string doctrineId) =>
            string.IsNullOrWhiteSpace(doctrineId) ? "DOCTRINE_BALANCED" : doctrineId;

        private static UnionState CopyUnion(
            UnionState source,
            string leaderRecruitId,
            IReadOnlyList<string> members,
            string formationId,
            string doctrineId) =>
            new UnionState(
                source.UnionId,
                source.DisplayName,
                source.Kind,
                leaderRecruitId,
                members,
                formationId,
                doctrineId,
                source.SharedAp,
                source.CohesionBasisPoints);

        private static GuildState FinalizeOpeningUnionResources(GuildState guild)
        {
            var unions = Copy(guild.Unions);
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                var preview = ProjectOpeningUnionResources(guild, union);
                unions[unionIndex] = new UnionState(
                    union.UnionId,
                    union.DisplayName,
                    union.Kind,
                    union.LeaderRecruitId,
                    union.MemberRecruitIds,
                    union.FormationId,
                    union.DoctrineId,
                    preview.SharedAp,
                    preview.CohesionBasisPoints,
                    union.MemberPositions);
            }
            return guild.With(guild.TreasuryXp, guild.Recruits, unions, guild.Inventory);
        }

        private static int FormationCohesionBonus(string formationId)
        {
            if (StringComparer.Ordinal.Equals(formationId, "FORMATION_SHIELD_WALL")) return 8;
            if (StringComparer.Ordinal.Equals(formationId, "FORMATION_VEILED_ECHELON")) return 2;
            return 0;
        }

        private static int Clamp(int value, int minimum, int maximum) =>
            value < minimum ? minimum : value > maximum ? maximum : value;

        private static bool LoadoutContains(EquipmentLoadoutState loadout, string instanceId)
        {
            for (var index = 0; index < loadout.Assignments.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(loadout.Assignments[index].Item.InstanceId, instanceId)) return true;
            }
            return false;
        }

        private static void RemoveAssignment(List<EquipmentSlotAssignmentState> assignments, string slotId)
        {
            for (var index = assignments.Count - 1; index >= 0; index--)
            {
                if (StringComparer.Ordinal.Equals(assignments[index].SlotId, slotId)) assignments.RemoveAt(index);
            }
        }

        private static int FindRecruitIndex(IReadOnlyList<RecruitState> recruits, string recruitId)
        {
            for (var index = 0; index < recruits.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(recruits[index].RecruitId, recruitId)) return index;
            }
            return -1;
        }

        private static int FindInventoryIndex(IReadOnlyList<EquipmentItemState> items, string instanceId)
        {
            for (var index = 0; index < items.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(items[index].InstanceId, instanceId)) return index;
            }
            return -1;
        }

        private static bool IsItemEquipped(IReadOnlyList<RecruitState> recruits, string instanceId)
        {
            for (var recruitIndex = 0; recruitIndex < recruits.Count; recruitIndex++)
            {
                var assignments = recruits[recruitIndex].Equipment.Assignments;
                for (var itemIndex = 0; itemIndex < assignments.Count; itemIndex++)
                {
                    if (StringComparer.Ordinal.Equals(assignments[itemIndex].Item.InstanceId, instanceId)) return true;
                }
            }
            return false;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(values[index], value)) return true;
            }
            return false;
        }

        private static bool IsOpeningUnionIndex(IReadOnlyList<UnionState> unions, int index) =>
            unions != null && index >= 0 && index < unions.Count &&
            index < NormalUnionPlanRules.MaximumPlanCount;

        private static List<T> Copy<T>(IReadOnlyList<T> values)
        {
            var copy = new List<T>();
            if (values != null)
            {
                for (var index = 0; index < values.Count; index++) copy.Add(values[index]);
            }
            return copy;
        }

        private static string[] CopyErrors(IReadOnlyList<string> errors)
        {
            var copy = new string[errors.Count];
            for (var index = 0; index < errors.Count; index++) copy[index] = errors[index];
            return copy;
        }
    }
}
