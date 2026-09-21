using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public enum FoundingPreparationStage078
    {
        ChooseFieldLead = 0,
        ChooseEquipmentPriority = 1,
        PlaceFieldLead = 2,
        ChooseFormation = 3,
        ConfirmRescueTeam = 4,
        Complete = 5
    }

    /// <summary>
    /// A short, person-first Guildmaster preparation loop for the first rescue.
    /// It intentionally asks for one decision at a time and then uses the existing
    /// saved M1 commands to fill the legal remainder.  No graph, stat spreadsheet,
    /// or presentation-only roster is introduced here.
    /// </summary>
    public sealed partial class M1FlowPresenter
    {
        public const string FoundingPreparationRootName078 =
            "Founding Rescue Preparation 078";
        public const string FoundingPreparationConfirmName078 =
            "Confirm Rescue Team 078";
        public const string FoundingPreparationBackName078 =
            "Founding Edit Previous 078";
        public const int FoundingPreparationDecisionCount078 = 5;

        private FoundingPreparationStage078 _foundingPreparationStage078 =
            FoundingPreparationStage078.ChooseFieldLead;
        private string _foundingLeadRecruitId078 = string.Empty;
        private string _foundingCommittedLeadRecruitId078 = string.Empty;
        private string _foundingEquipmentSlotId078 = string.Empty;
        private int _foundingPlacementChoiceUnionIndex078 = -1;
        private int _foundingLeadUnionIndex078 = -1;
        private string _foundingFormationId078 = string.Empty;
        private bool _foundingPreparationEditingPrevious078;
        private FoundingPreparationStage078 _foundingPreparationEditResumeStage078 =
            FoundingPreparationStage078.ChooseFieldLead;

        private void ResetFoundingPreparation078()
        {
            _foundingPreparationStage078 =
                FoundingPreparationStage078.ChooseFieldLead;
            _foundingLeadRecruitId078 = string.Empty;
            _foundingCommittedLeadRecruitId078 = string.Empty;
            _foundingEquipmentSlotId078 = string.Empty;
            _foundingPlacementChoiceUnionIndex078 = -1;
            _foundingLeadUnionIndex078 = -1;
            _foundingFormationId078 = string.Empty;
            _foundingPreparationEditingPrevious078 = false;
            _foundingPreparationEditResumeStage078 =
                FoundingPreparationStage078.ChooseFieldLead;
        }

        private bool ShouldBuildFoundingPreparation078()
        {
            if (!(_coordinator is M1RuntimeCoordinator) ||
                _coordinator?.State == null ||
                !_coordinator.State.HasCampaign)
                return false;

            if ((_coordinator.State.Recruits?.Count ?? 0) >= 10 &&
                _coordinator.State.OpeningUnionsLegal)
            {
                _foundingPreparationStage078 =
                    FoundingPreparationStage078.Complete;
                return false;
            }

            SynchronizeFoundingPreparationFromSavedState078();
            return _foundingPreparationStage078 !=
                   FoundingPreparationStage078.Complete;
        }

        private void SynchronizeFoundingPreparationFromSavedState078()
        {
            var recruits = (_coordinator?.State?.Recruits ??
                            Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            if (recruits.Length == 0)
            {
                if (_foundingPreparationStage078 !=
                    FoundingPreparationStage078.ChooseFieldLead)
                    ResetFoundingPreparation078();
                return;
            }

            // Back/Edit Previous and failed-command retry deliberately render an
            // explicit decision stage. Their commit methods advance the five
            // screens, so restore inference must not skip the player forward.
            if (_foundingPreparationEditingPrevious078)
            {
                _foundingPreparationStage078 =
                    ResolveFoundingStageForVerification078(
                        currentStage: _foundingPreparationStage078,
                        preserveExplicitStage: true,
                        recruitCount: recruits.Length,
                        hasLeadUnion: false);
                return;
            }

            var lockedLead = recruits.FirstOrDefault(value =>
                (value.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .Any(slot => slot != null && slot.IsLocked));
            if (string.IsNullOrWhiteSpace(_foundingLeadRecruitId078))
                _foundingLeadRecruitId078 =
                    lockedLead?.RecruitId ?? recruits[0].RecruitId;
            if (string.IsNullOrWhiteSpace(_foundingCommittedLeadRecruitId078))
                _foundingCommittedLeadRecruitId078 = _foundingLeadRecruitId078;

            if (recruits.Length < OpeningFlowState.RequiredOpeningRecruitCount)
            {
                _foundingPreparationStage078 =
                    ResolveFoundingStageForVerification078(
                        currentStage: _foundingPreparationStage078,
                        preserveExplicitStage: false,
                        recruitCount: recruits.Length,
                        hasLeadUnion: false);
                return;
            }

            var leadUnion = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .FirstOrDefault(value => value != null &&
                    (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(
                        _foundingLeadRecruitId078,
                        StringComparer.Ordinal));
            if (leadUnion == null)
            {
                _foundingPreparationStage078 =
                    ResolveFoundingStageForVerification078(
                        currentStage: _foundingPreparationStage078,
                        preserveExplicitStage: false,
                        recruitCount: recruits.Length,
                        hasLeadUnion: false);
                return;
            }

            _foundingLeadUnionIndex078 = leadUnion.Index;
            _foundingPreparationStage078 =
                ResolveFoundingStageForVerification078(
                    currentStage: _foundingPreparationStage078,
                    preserveExplicitStage: false,
                    recruitCount: recruits.Length,
                    hasLeadUnion: true);
        }

        private void BuildFoundingPreparation078(Transform parent)
        {
            var root = RuntimeUi.AddPanel(
                parent,
                FoundingPreparationRootName078,
                Color.clear);
            Stretch(root.rectTransform);
            root.raycastTarget = false;

            BuildFoundingPreparationProgress078(root.transform);
            switch (_foundingPreparationStage078)
            {
                case FoundingPreparationStage078.ChooseFieldLead:
                    BuildFoundingLeadChoice078(root.transform);
                    break;
                case FoundingPreparationStage078.ChooseEquipmentPriority:
                    BuildFoundingEquipmentChoice078(root.transform);
                    break;
                case FoundingPreparationStage078.PlaceFieldLead:
                    BuildFoundingPlacementChoice078(root.transform);
                    break;
                case FoundingPreparationStage078.ChooseFormation:
                    BuildFoundingFormationChoice078(root.transform);
                    break;
                default:
                    BuildFoundingConfirmation078(root.transform);
                    break;
            }
        }

        private void BuildFoundingPreparationProgress078(Transform parent)
        {
            var strip = RuntimeUi.AddPanel(
                parent,
                "Founding Preparation Progress 078",
                new Color(0.010f, 0.025f, 0.037f, 0.98f));
            AnchorStudioRect076(
                strip.rectTransform,
                new Vector2(0.018f, 0.855f),
                new Vector2(0.982f, 0.982f));
            M1PremiumUi.StylePanel(strip, M1PremiumUi.Surface.WorldRibbon);

            var title = RuntimeUi.AddText(
                strip.transform,
                "Founding Preparation Title 078",
                FoundingPreparationHeading078(_foundingPreparationStage078),
                30,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorStudioRect076(
                title.rectTransform,
                new Vector2(0.025f, 0.40f),
                new Vector2(0.66f, 0.92f));
            ConfigureResponsiveText062(title, 20, 31);

            var step = Mathf.Clamp((int)_foundingPreparationStage078 + 1,
                1, FoundingPreparationDecisionCount078);
            var progress = RuntimeUi.AddText(
                strip.transform,
                "Founding Preparation Step 078",
                "RESCUE PREPARATION  •  STEP " + step + " OF " +
                FoundingPreparationDecisionCount078,
                22,
                TextAnchor.MiddleRight,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorStudioRect076(
                progress.rectTransform,
                new Vector2(0.69f, 0.40f),
                new Vector2(0.975f, 0.92f));
            ConfigureResponsiveText062(progress, 16, 23);

            var promise = RuntimeUi.AddText(
                strip.transform,
                "Founding Preparation Promise 078",
                "ONE CLEAR DECISION AT A TIME  •  THE REST AUTO-FILLS SAFELY  •  EVERY CHOICE SAVES",
                17,
                TextAnchor.MiddleLeft,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorStudioRect076(
                promise.rectTransform,
                new Vector2(0.025f, 0.06f),
                new Vector2(0.975f, 0.36f));
            ConfigureResponsiveText062(promise, 13, 18);
        }

        private void BuildFoundingLeadChoice078(Transform parent)
        {
            var applicants = (_coordinator.State.Applicants ??
                              Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                .ToArray();

            var instruction = AddFoundingInstruction078(
                parent,
                "1  •  CHOOSE THE PERSON YOU TRUST FIRST",
                "All six join. Colored ribbons are rescue promises; trained classes stay unchanged. Your choice becomes field lead, receives the first equipment order, and anchors the Union placed next.");
            instruction.color = RuntimeUi.Warning;

            var cardRects = FoundingLeadCardRectsForVerification078(
                applicants.Length);
            Button firstCard = null;
            for (var index = 0; index < applicants.Length; index++)
            {
                var applicant = applicants[index];
                var captured = applicant;
                var selected = StringComparer.Ordinal.Equals(
                    captured.RecruitId,
                    _foundingLeadRecruitId078);
                var role = FoundingPromiseRoleForVerification079(
                    index,
                    captured.ObservedClass);
                var roleColor = UnionPlannerRoleColorForVerification074(
                    role);
                var card = RuntimeUi.AddButton(
                    parent,
                    "Founding Applicant " + captured.RecruitId + " 076",
                    (selected ? "◆  " : string.Empty) +
                    (captured.DisplayName ?? "Volunteer").ToUpperInvariant() +
                    "\n" + FoundingRolePromise078(role),
                    () =>
                    {
                        _foundingLeadRecruitId078 = captured.RecruitId;
                        _foundingEquipmentSlotId078 = string.Empty;
                        BuildCurrentScreen();
                    },
                    RuntimeUi.MinimumTouchPixels,
                    selected
                        ? FoundingGoldChoiceSurfaceForVerification078(true)
                        : new Color(0.018f, 0.040f, 0.060f, 0.98f));
                if (firstCard == null) firstCard = card;
                AnchorStudioRect076(
                    card.GetComponent<RectTransform>(),
                    cardRects[index].min,
                    cardRects[index].max);
                AddPortraitToButton(card, captured, large: false, cropToFill: true);
                var label = card.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.rectTransform.anchorMin = new Vector2(0.34f, 0.06f);
                    label.rectTransform.anchorMax = new Vector2(0.97f, 0.69f);
                    label.rectTransform.offsetMin = Vector2.zero;
                    label.rectTransform.offsetMax = Vector2.zero;
                    label.alignment = TextAnchor.MiddleLeft;
                    ConfigureResponsiveText062(label, 14, 20);
                }
                var badge = RuntimeUi.AddPanel(
                    card.transform,
                    "Founding Applicant Role Color " + captured.RecruitId + " 076",
                    roleColor);
                badge.raycastTarget = false;
                AnchorStudioRect076(
                    badge.rectTransform,
                    new Vector2(0.34f, 0.74f),
                    new Vector2(0.97f, 0.95f));
                var badgeText = RuntimeUi.AddText(
                    badge.transform,
                    "Founding Applicant Role " + captured.RecruitId + " 076",
                    role + " PROMISE",
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Background,
                    FontStyle.Bold);
                Stretch(badgeText.rectTransform);
                ConfigureResponsiveText062(badgeText, 13, 19);
                badgeText.raycastTarget = false;
                StyleFoundingGoldChoice078(card, selected);
            }

            var selectedApplicant = applicants.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    _foundingLeadRecruitId078));
            var confirm = RuntimeUi.AddButton(
                parent,
                "Founding Lead Confirm 078",
                selectedApplicant == null
                    ? "SELECT ONE PERSON"
                    : "TRUST " + selectedApplicant.DisplayName.ToUpperInvariant() +
                      " AS FIELD LEAD  →",
                selectedApplicant == null
                    ? (Action)null
                    : () => CommitFoundingLead078(selectedApplicant.RecruitId),
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                confirm.GetComponent<RectTransform>(),
                new Vector2(0.66f, 0.035f),
                new Vector2(0.982f, 0.235f));
            confirm.interactable = selectedApplicant != null;
            StyleFoundingCallToAction078(confirm);
            ConfigureResponsiveText062(confirm.GetComponentInChildren<Text>(), 18, 27);
            if (confirm.interactable) confirm.Select();
            else firstCard?.Select();

            AddFoundingStatus078(parent,
                "PERSON → EQUIPMENT → PLACE → FORMATION → CONFIRM");
        }

        private void BuildFoundingEquipmentChoice078(Transform parent)
        {
            var recruit = FoundingLeadRecruit078();
            AddFoundingInstruction078(
                parent,
                "2  •  SET THE LEAD'S EQUIPMENT PRIORITY",
                "Choose the piece the quartermaster must keep on this rescue loadout. It stays locked until you deliberately unlock it in the Armory.");
            if (recruit == null)
            {
                AddFoundingBlocking078(parent,
                    "The selected field lead could not be restored. Return to the title and continue the saved Guild.");
                AddFoundingBack078(parent, focus: true);
                return;
            }

            var portrait = RuntimeUi.AddPanel(
                parent,
                "Founding Equipment Lead Portrait 078",
                UnionPlannerRoleColorForVerification074(recruit.ObservedClass));
            AnchorStudioRect076(
                portrait.rectTransform,
                new Vector2(0.045f, 0.245f),
                new Vector2(0.30f, 0.705f));
            PopulatePortraitFrame(
                portrait,
                recruit.RecruitId,
                recruit.VisualSeed,
                recruit.RaceId,
                recruit.PortraitAuthorityId,
                recruit.DisplayName,
                recruit.ObservedClass,
                recruit.DisplayName.ToUpperInvariant() + "\n" +
                UnionPlannerRoleDesignationForVerification074(recruit.ObservedClass),
                roleIdentity: recruit.ObservedClass);

            var choices = (recruit.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.EquippedItemId) &&
                                (StringComparer.Ordinal.Equals(
                                     value.SlotId, EquipmentSlotIds.MainHand) ||
                                 StringComparer.Ordinal.Equals(
                                     value.SlotId, EquipmentSlotIds.BodyArmor)))
                .OrderBy(value => StringComparer.Ordinal.Equals(
                    value.SlotId, EquipmentSlotIds.MainHand) ? 0 : 1)
                .Take(2)
                .ToArray();
            Button firstChoice = null;
            for (var index = 0; index < choices.Length; index++)
            {
                var slot = choices[index];
                var captured = slot;
                var selected = StringComparer.Ordinal.Equals(
                    captured.SlotId,
                    _foundingEquipmentSlotId078);
                var card = RuntimeUi.AddButton(
                    parent,
                    "Founding Equipment Choice " + captured.SlotId + " 078",
                    (selected ? "◆  " : string.Empty) +
                    captured.DisplayName.ToUpperInvariant() + "\n" +
                    FoundingEquipmentDisplayName078(captured).ToUpperInvariant() +
                    "\n" + EquipmentPriorityPromise078(captured.SlotId),
                    () =>
                    {
                        _foundingEquipmentSlotId078 = captured.SlotId;
                        BuildCurrentScreen();
                    },
                    RuntimeUi.PrimaryTouchPixels,
                    FoundingGoldChoiceSurfaceForVerification078(selected));
                if (firstChoice == null) firstChoice = card;
                AnchorStudioRect076(
                    card.GetComponent<RectTransform>(),
                    new Vector2(0.35f, 0.49f - index * 0.255f),
                    new Vector2(0.955f, 0.70f - index * 0.255f));
                ConfigureResponsiveText062(card.GetComponentInChildren<Text>(), 15, 23);
                M1PremiumUi.AddEquipmentArtworkToButton(
                    card,
                    captured.EquippedVisualId,
                    captured.EquippedRarityTierId,
                    captured.EquippedRarityDisplayName,
                    captured.VisualGlyph,
                    equipped: false,
                    locked: false);
                var cardLabel = FoundingButtonLabel078(card);
                if (cardLabel != null)
                {
                    var labelRect = FoundingEquipmentLabelRectForVerification079(
                        selected);
                    AnchorStudioRect076(
                        cardLabel.rectTransform,
                        labelRect.min,
                        labelRect.max);
                    cardLabel.alignment = TextAnchor.MiddleLeft;
                }
                if (selected)
                {
                    var priorityBadge = RuntimeUi.AddPanel(
                        card.transform,
                        "Founding Equipment Priority Badge " + captured.SlotId + " 078",
                        RuntimeUi.Accent);
                    priorityBadge.raycastTarget = false;
                    AnchorStudioRect076(
                        priorityBadge.rectTransform,
                        FoundingEquipmentPriorityBadgeRectForVerification079.min,
                        FoundingEquipmentPriorityBadgeRectForVerification079.max);
                    var priorityText = RuntimeUi.AddText(
                        priorityBadge.transform,
                        "Founding Equipment Priority Text " + captured.SlotId + " 078",
                        "PRIORITY",
                        16,
                        TextAnchor.MiddleCenter,
                        RuntimeUi.Background,
                        FontStyle.Bold);
                    Stretch(priorityText.rectTransform);
                    ConfigureResponsiveText062(priorityText, 12, 17);
                    priorityText.raycastTarget = false;
                }
                StyleFoundingGoldChoice078(card, selected);
            }

            var selectedSlot = choices.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(
                    value.SlotId,
                    _foundingEquipmentSlotId078));
            var confirm = RuntimeUi.AddButton(
                parent,
                "Founding Equipment Confirm 078",
                selectedSlot == null
                    ? "CHOOSE WEAPON OR ARMOR PRIORITY"
                    : "LOCK " + FoundingEquipmentDisplayName078(selectedSlot).ToUpperInvariant() +
                      " FOR THE RESCUE  →",
                selectedSlot == null
                    ? (Action)null
                    : () => CommitFoundingEquipment078(selectedSlot.SlotId),
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                confirm.GetComponent<RectTransform>(),
                new Vector2(0.55f, 0.035f),
                new Vector2(0.955f, 0.205f));
            confirm.interactable = selectedSlot != null;
            StyleFoundingCallToAction078(confirm);
            ConfigureResponsiveText062(confirm.GetComponentInChildren<Text>(), 17, 25);
            if (confirm.interactable) confirm.Select();
            else firstChoice?.Select();
            AddFoundingStatus078(parent,
                "THIS IS A REAL SAVED LOADOUT ORDER  •  CHANGE IT LATER IN ARMORY");
            AddFoundingBack078(parent);
        }

        private void BuildFoundingPlacementChoice078(Transform parent)
        {
            var recruit = FoundingLeadRecruit078();
            AddFoundingInstruction078(
                parent,
                "3  •  PLACE THE FIELD LEAD",
                "Choose one clear team job. Your lead gets a compatible teammate, then every remaining founder is distributed safely across the other two teams. No one is discarded.");
            if (recruit == null)
            {
                AddFoundingBlocking078(parent,
                    "The field lead is unavailable. Continue the saved Guild to restore preparation.");
                AddFoundingBack078(parent, focus: true);
                return;
            }

            var unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            Button firstDestination = null;
            for (var index = 0; index < unions.Length; index++)
            {
                var union = unions[index];
                var captured = union;
                var selected = captured.Index ==
                               _foundingPlacementChoiceUnionIndex078;
                var color = FoundingDestinationColor078(index);
                var card = RuntimeUi.AddButton(
                    parent,
                    "Founding Union Destination " + captured.Index + " 078",
                    (selected ? "◆  " : string.Empty) +
                    M1UnionIdentity076.Resolve(
                        captured.UnionId,
                        captured.DisplayName,
                        captured.Index).ToUpperInvariant() +
                    "\n" + FoundingDestinationPromise078(index),
                    () =>
                    {
                        _foundingPlacementChoiceUnionIndex078 = captured.Index;
                        BuildCurrentScreen();
                    },
                    RuntimeUi.PrimaryTouchPixels,
                    selected
                        ? FoundingGoldChoiceSurfaceForVerification078(true)
                        : new Color(0.018f, 0.040f, 0.060f, 0.98f));
                if (firstDestination == null) firstDestination = card;
                AnchorStudioRect076(
                    card.GetComponent<RectTransform>(),
                    new Vector2(0.055f + index * 0.31f, 0.33f),
                    new Vector2(0.335f + index * 0.31f, 0.70f));
                var label = FoundingButtonLabel078(card);
                if (label != null)
                {
                    AnchorStudioRect076(
                        label.rectTransform,
                        new Vector2(0.06f, 0.08f),
                        new Vector2(0.94f, 0.74f));
                    label.alignment = TextAnchor.MiddleCenter;
                    ConfigureResponsiveText062(label, 15, 22);
                }

                var roleStrip = RuntimeUi.AddPanel(
                    card.transform,
                    "Founding Union Destination Role Color " + captured.Index + " 078",
                    color);
                roleStrip.raycastTarget = false;
                AnchorStudioRect076(
                    roleStrip.rectTransform,
                    new Vector2(0.04f, 0.78f),
                    new Vector2(0.96f, 0.94f));
                var roleStripText = RuntimeUi.AddText(
                    roleStrip.transform,
                    "Founding Union Destination Role " + captured.Index + " 078",
                    FoundingDestinationRibbonForVerification078(index),
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Background,
                    FontStyle.Bold);
                Stretch(roleStripText.rectTransform);
                ConfigureResponsiveText062(roleStripText, 14, 19);
                roleStripText.raycastTarget = false;
                StyleFoundingGoldChoice078(card, selected);
            }

            var chosen = unions.FirstOrDefault(value => value.Index ==
                _foundingPlacementChoiceUnionIndex078);
            var confirm = RuntimeUi.AddButton(
                parent,
                "Founding Placement Confirm 078",
                chosen == null
                    ? "CHOOSE A UNION JOB"
                    : "PLACE " + recruit.DisplayName.ToUpperInvariant() +
                      " IN " + M1UnionIdentity076.Resolve(
                          chosen.UnionId,
                          chosen.DisplayName,
                          chosen.Index).ToUpperInvariant() + "  →",
                chosen == null
                    ? (Action)null
                    : () => CommitFoundingPlacement078(chosen.Index),
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                confirm.GetComponent<RectTransform>(),
                new Vector2(0.55f, 0.055f),
                new Vector2(0.955f, 0.235f));
            confirm.interactable = chosen != null;
            StyleFoundingCallToAction078(confirm);
            ConfigureResponsiveText062(confirm.GetComponentInChildren<Text>(), 16, 24);
            if (confirm.interactable) confirm.Select();
            else firstDestination?.Select();
            var chosenPosition = Array.FindIndex(unions,
                value => value.Index == _foundingPlacementChoiceUnionIndex078);
            AddFoundingStatus078(parent,
                "TEAM JOB  •  " + (chosenPosition >= 0
                    ? FoundingDestinationRibbonForVerification078(chosenPosition)
                    : "CHOOSE HOLD, STRIKE, OR SCOUT") +
                "  •  LEADER CLASS  •  " +
                UnionPlannerRoleDesignationForVerification074(recruit.ObservedClass));
            AddFoundingBack078(parent);
        }

        private void BuildFoundingFormationChoice078(Transform parent)
        {
            var leadUnion = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .FirstOrDefault(value => value != null &&
                    value.Index == _foundingLeadUnionIndex078);
            var leadUnionName = M1UnionIdentity076.Resolve(
                leadUnion?.UnionId,
                leadUnion?.DisplayName,
                leadUnion?.Index ?? 0);
            AddFoundingInstruction078(
                parent,
                "4  •  CHOOSE HOW YOUR FIRST UNION STANDS TOGETHER",
                "Pick one starting position for the whole team. The six dots are member places; more formations unlock through play.");

            var formations = (_coordinator.State.Formations ??
                              Array.Empty<M1ChoiceView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            Button firstFormation = null;
            for (var index = 0; index < formations.Length; index++)
            {
                var formation = formations[index];
                var captured = formation;
                var selected = StringComparer.Ordinal.Equals(
                    captured.Id,
                    _foundingFormationId078);
                var card = RuntimeUi.AddButton(
                    parent,
                    "Founding Formation Choice " + captured.Id + " 078",
                    (selected ? "◆  " : string.Empty) +
                    captured.DisplayName.ToUpperInvariant(),
                    () =>
                    {
                        _foundingFormationId078 = captured.Id;
                        BuildCurrentScreen();
                    },
                    RuntimeUi.PrimaryTouchPixels,
                    FoundingGoldChoiceSurfaceForVerification078(selected));
                if (firstFormation == null) firstFormation = card;
                AnchorStudioRect076(
                    card.GetComponent<RectTransform>(),
                    new Vector2(0.055f + index * 0.31f, 0.31f),
                    new Vector2(0.335f + index * 0.31f, 0.70f));
                var label = FoundingButtonLabel078(card);
                if (label != null)
                {
                    AnchorStudioRect076(
                        label.rectTransform,
                        new Vector2(0.06f, 0.80f),
                        new Vector2(0.94f, 0.96f));
                    label.alignment = TextAnchor.MiddleCenter;
                    ConfigureResponsiveText062(label, 15, 22);
                }
                AddFoundingFormationDiagram078(card, captured, selected);
                StyleFoundingGoldChoice078(card, selected);
            }

            var chosen = formations.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.Id, _foundingFormationId078));
            var confirm = RuntimeUi.AddButton(
                parent,
                "Founding Formation Confirm 078",
                chosen == null
                    ? "CHOOSE ONE FORMATION"
                    : "USE " + chosen.DisplayName.ToUpperInvariant() +
                      " FOR " + leadUnionName.ToUpperInvariant() +
                      "  →",
                chosen == null
                    ? (Action)null
                    : () => CommitFoundingFormation078(chosen.Id),
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                confirm.GetComponent<RectTransform>(),
                new Vector2(0.55f, 0.055f),
                new Vector2(0.955f, 0.235f));
            confirm.interactable = chosen != null;
            StyleFoundingCallToAction078(confirm);
            ConfigureResponsiveText062(confirm.GetComponentInChildren<Text>(), 16, 24);
            if (confirm.interactable) confirm.Select();
            else firstFormation?.Select();
            AddFoundingStatus078(parent,
                "CHOOSE FOR THE TEAM  •  THREE STARTERS NOW  •  MORE FORMATIONS UNLOCK THROUGH PLAY");
            AddFoundingBack078(parent);
        }

        private void BuildFoundingConfirmation078(Transform parent)
        {
            var recruit = FoundingLeadRecruit078();
            var unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            var union = unions
                .FirstOrDefault(value => value != null &&
                    value.Index == _foundingLeadUnionIndex078);
            var formation = (_coordinator.State.Formations ??
                             Array.Empty<M1ChoiceView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(
                        value.Id,
                        _foundingFormationId078));
            var recruitSlots = recruit?.Slots ?? Array.Empty<M1EquipmentSlotView>();
            var locked = recruitSlots.FirstOrDefault(value => value != null &&
                             value.IsLocked &&
                             StringComparer.Ordinal.Equals(
                                 value.SlotId,
                                 _foundingEquipmentSlotId078)) ??
                         recruitSlots.FirstOrDefault(value =>
                             value != null && value.IsLocked);

            var founderIds = (_coordinator.State.Applicants ??
                              Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                .Select(value => value.RecruitId)
                .ToArray();
            var founders = (_coordinator.State.Recruits ??
                            Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null &&
                    founderIds.Contains(value.RecruitId, StringComparer.Ordinal))
                .ToArray();
            var selectedUnionPosition = Array.FindIndex(
                unions,
                value => value.Index == _foundingLeadUnionIndex078);
            if (selectedUnionPosition < 0) selectedUnionPosition = 0;
            var founderGroups = BuildFoundingRoleGroupsForVerification078(
                founders,
                _foundingLeadRecruitId078,
                selectedUnionPosition);
            var founderNames = founders
                .GroupBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToDictionary(
                    value => value.Key,
                    value => value.First().DisplayName ?? value.Key,
                    StringComparer.Ordinal);
            var charter = new FirstHourDirector071().PlayableRoster
                .Where(value => value != null &&
                    value.Wave == FirstHourRosterWave071.SkyhomeCharter)
                .Take(4)
                .ToArray();
            var reserve = charter.FirstOrDefault(value =>
                              (value.DisplayName ?? string.Empty).StartsWith(
                                  "Bessa ",
                                  StringComparison.OrdinalIgnoreCase)) ??
                          charter.Skip(2).FirstOrDefault();
            var fieldCharter = charter
                .Where(value => !ReferenceEquals(value, reserve))
                .Take(3)
                .ToArray();

            AddFoundingInstruction078(
                parent,
                "5  •  CONFIRM THE RESCUE TEAM",
                "Your six founders form the three pairs below. Tala, Orren, and Vaelis join for the rescue; Bessa stays ready at Hall.");

            var summary = RuntimeUi.AddPanel(
                parent,
                "Founding Rescue Team Summary 078",
                new Color(0.018f, 0.040f, 0.052f, 0.98f));
            AnchorStudioRect076(
                summary.rectTransform,
                new Vector2(0.055f, 0.24f),
                new Vector2(0.945f, 0.70f));
            M1PremiumUi.StylePanel(summary, M1PremiumUi.Surface.WorldPaper);

            var choices = RuntimeUi.AddText(
                summary.transform,
                "Founding Rescue Team Saved Choices 078",
                "YOUR SAVED CHOICES  •  LEAD " +
                (recruit?.DisplayName ?? "NOT SELECTED").ToUpperInvariant() +
                "  •  " + FoundingEquipmentDisplayName078(locked).ToUpperInvariant() +
                "  •  " +
                (formation?.DisplayName ?? union?.FormationId ?? "STARTER FORMATION")
                    .ToUpperInvariant(),
                18,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorStudioRect076(
                choices.rectTransform,
                new Vector2(0.025f, 0.84f),
                new Vector2(0.975f, 0.97f));
            ConfigureResponsiveText062(choices, 14, 19);

            for (var index = 0; index < unions.Length; index++)
            {
                var currentUnion = unions[index];
                var card = RuntimeUi.AddPanel(
                    summary.transform,
                    "Founding Confirmation Union " + currentUnion.Index + " 078",
                    new Color(0.012f, 0.030f, 0.043f, 0.98f));
                AnchorStudioRect076(
                    card.rectTransform,
                    new Vector2(0.025f + index * 0.325f, 0.235f),
                    new Vector2(0.325f + index * 0.325f, 0.80f));
                M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldRibbon);

                var ribbon = RuntimeUi.AddPanel(
                    card.transform,
                    "Founding Confirmation Union Role Color " + currentUnion.Index + " 078",
                    FoundingDestinationColor078(index));
                ribbon.raycastTarget = false;
                AnchorStudioRect076(
                    ribbon.rectTransform,
                    new Vector2(0.035f, 0.78f),
                    new Vector2(0.965f, 0.95f));
                var ribbonText = RuntimeUi.AddText(
                    ribbon.transform,
                    "Founding Confirmation Union Role " + currentUnion.Index + " 078",
                    FoundingDestinationRibbonForVerification078(index),
                    16,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Background,
                    FontStyle.Bold);
                Stretch(ribbonText.rectTransform);
                ConfigureResponsiveText062(ribbonText, 12, 17);
                ribbonText.raycastTarget = false;

                var group = index < founderGroups.Count
                    ? founderGroups[index]
                    : Array.Empty<string>();
                var groupNames = group
                    .Select(value => founderNames.TryGetValue(value, out var name)
                        ? name
                        : value)
                    .Take(2)
                    .ToArray();
                var charterName = index < fieldCharter.Length
                    ? fieldCharter[index].DisplayName
                    : "Charter ally";
                var unionCopy = RuntimeUi.AddText(
                    card.transform,
                    "Founding Confirmation Union Text " + currentUnion.Index + " 078",
                    M1UnionIdentity076.Resolve(
                        currentUnion.UnionId,
                        currentUnion.DisplayName,
                        currentUnion.Index).ToUpperInvariant() +
                    "\nFOUNDERS\n" +
                    string.Join("\n", groupNames.Select(value =>
                        "•  " + value.ToUpperInvariant())) +
                    "\nCHARTER ALLY  •  " + charterName.ToUpperInvariant(),
                    16,
                    TextAnchor.MiddleLeft,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                AnchorStudioRect076(
                    unionCopy.rectTransform,
                    new Vector2(0.06f, 0.05f),
                    new Vector2(0.94f, 0.74f));
                ConfigureResponsiveText062(unionCopy, 12, 17);
            }

            var reserveText = RuntimeUi.AddText(
                summary.transform,
                "Founding Confirmation Reserve Text 078",
                "READY AT HALL  •  " +
                (reserve?.DisplayName ?? "Bessa Brasswhistle").ToUpperInvariant() +
                "  •  ROTATE HER IN BETWEEN MISSIONS",
                17,
                TextAnchor.MiddleCenter,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorStudioRect076(
                reserveText.rectTransform,
                new Vector2(0.035f, 0.035f),
                new Vector2(0.965f, 0.19f));
            ConfigureResponsiveText062(reserveText, 13, 18);

            var confirm = RuntimeUi.AddButton(
                parent,
                FoundingPreparationConfirmName078,
                "CONFIRM FIRST RESCUE TEAM",
                ConfirmFoundingRescueTeam078,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                confirm.GetComponent<RectTransform>(),
                new Vector2(0.55f, 0.045f),
                new Vector2(0.925f, 0.215f));
            ConfigureResponsiveText062(confirm.GetComponentInChildren<Text>(), 19, 28);
            confirm.Select();
            AddFoundingStatus078(parent,
                "CONFIRM ONCE  •  THE THREE TEAMS FILL SAFELY  •  REBUILD THEM ANY TIME AT THE STRATEGY TABLE");
            AddFoundingBack078(parent);
        }

        private void CommitFoundingLead078(string recruitId)
        {
            var previousLeadRecruitId = _foundingCommittedLeadRecruitId078;
            var applicant = (_coordinator.State.Applicants ??
                             Array.Empty<M1ApplicantView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            if (applicant == null)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.ChooseFieldLead,
                    FoundingPreparationStage078.ChooseEquipmentPriority);
                SetFoundingFailure078("That volunteer is no longer on the founding desk.");
                return;
            }

            M1CommandResult result;
            var previousBuilding = _building;
            _building = true;
            try
            {
                result = applicant.IsSigned
                    ? M1CommandResult.Success("Field lead restored.")
                    : _coordinator.SignRecruit(recruitId);
                if (result != null && result.Succeeded &&
                    !string.IsNullOrWhiteSpace(previousLeadRecruitId) &&
                    !StringComparer.Ordinal.Equals(previousLeadRecruitId, recruitId))
                {
                    result = ClearPreviousFoundingLead078(previousLeadRecruitId);
                }
            }
            finally
            {
                _building = previousBuilding;
            }

            if (result == null || !result.Succeeded)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.ChooseFieldLead,
                    FoundingPreparationStage078.ChooseEquipmentPriority);
                SetFoundingFailure078(result?.Message ??
                                      "The field lead could not be signed.");
                return;
            }

            _foundingLeadRecruitId078 = recruitId;
            _foundingCommittedLeadRecruitId078 = recruitId;
            _foundingEquipmentSlotId078 = string.Empty;
            _foundingPreparationStage078 =
                FoundingPreparationStage078.ChooseEquipmentPriority;
            CompleteFoundingEditIfResumed078();
            _localStatus = applicant.DisplayName +
                           " answered first. Choose one rescue equipment priority.";
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private M1CommandResult ClearPreviousFoundingLead078(string recruitId)
        {
            var previous = (_coordinator.State.Recruits ??
                            Array.Empty<M1RecruitLoadoutView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            var previousLocks = (previous?.Slots ??
                                 Array.Empty<M1EquipmentSlotView>())
                .Where(value => value != null && value.IsLocked &&
                    (StringComparer.Ordinal.Equals(
                         value.SlotId, EquipmentSlotIds.MainHand) ||
                     StringComparer.Ordinal.Equals(
                         value.SlotId, EquipmentSlotIds.BodyArmor)))
                .ToArray();
            var previousPlacement = (_coordinator.State.Unions ??
                                     Array.Empty<M1UnionView>())
                .FirstOrDefault(value => value != null &&
                    (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(
                        recruitId,
                        StringComparer.Ordinal));
            var previousSlot = previousPlacement == null
                ? -1
                : (previousPlacement.MemberRecruitIds ?? Array.Empty<string>())
                    .ToList()
                    .FindIndex(value => StringComparer.Ordinal.Equals(
                        value,
                        recruitId));
            var previousWasLeader = previousPlacement != null &&
                                    StringComparer.Ordinal.Equals(
                                        previousPlacement.LeaderRecruitId,
                                        recruitId);

            M1CommandResult result = previousPlacement == null
                ? M1CommandResult.Success()
                : _coordinator.UnassignRecruitFromUnion(recruitId);
            var previousPlacementRemoved = previousPlacement != null &&
                                           result != null && result.Succeeded;
            foreach (var slot in previousLocks)
            {
                if (result == null || !result.Succeeded) break;
                result = _coordinator.SetEquipmentLock(
                    recruitId,
                    slot.SlotId,
                    false);
                if (result == null || !result.Succeeded) break;
            }

            if (result != null && result.Succeeded)
                return M1CommandResult.Success("Previous field-lead order cleared.");

            var originalFailure = result;
            var equipmentRollback = M1CommandResult.Success();
            foreach (var slot in previousLocks)
            {
                equipmentRollback = _coordinator.SetEquipmentLock(
                    recruitId,
                    slot.SlotId,
                    true);
                if (equipmentRollback == null || !equipmentRollback.Succeeded) break;
            }
            var placementRollback = previousPlacementRemoved
                ? RestoreFoundingPlacement078(
                    recruitId,
                    previousPlacement,
                    previousSlot,
                    previousWasLeader,
                    replacementWasAssigned: false)
                : M1CommandResult.Success();
            return equipmentRollback == null || !equipmentRollback.Succeeded ||
                   placementRollback == null || !placementRollback.Succeeded
                ? M1CommandResult.Failure(
                    (originalFailure?.Message ??
                     "The previous field-lead order could not be cleared.") +
                    " The saved lead could not be restored.")
                : originalFailure;
        }

        private void CommitFoundingEquipment078(string slotId)
        {
            var recruit = FoundingLeadRecruit078();
            var slot = (recruit?.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.SlotId, slotId) &&
                    !string.IsNullOrWhiteSpace(value.EquippedItemId));
            if (recruit == null || slot == null)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.ChooseEquipmentPriority,
                    FoundingPreparationStage078.PlaceFieldLead);
                SetFoundingFailure078(
                    "Choose one equipped weapon or armor card before continuing.");
                return;
            }

            var previousPriorities = (recruit.Slots ??
                                      Array.Empty<M1EquipmentSlotView>())
                .Where(value => value != null && value.IsLocked &&
                    !StringComparer.Ordinal.Equals(value.SlotId, slot.SlotId) &&
                    (StringComparer.Ordinal.Equals(
                         value.SlotId, EquipmentSlotIds.MainHand) ||
                     StringComparer.Ordinal.Equals(
                         value.SlotId, EquipmentSlotIds.BodyArmor)))
                .ToArray();
            var replacementLockAdded = false;
            M1CommandResult result = M1CommandResult.Success();
            var previousBuilding = _building;
            _building = true;
            try
            {
                // Establish the replacement before releasing the prior saved
                // priority. If a persistence write fails, the old choice remains.
                if (!slot.IsLocked)
                {
                    result = _coordinator.SetEquipmentLock(
                        recruit.RecruitId,
                        slot.SlotId,
                        true);
                    replacementLockAdded = result != null && result.Succeeded;
                }
                foreach (var previousPriority in previousPriorities)
                {
                    if (result == null || !result.Succeeded) break;
                    result = _coordinator.SetEquipmentLock(
                        recruit.RecruitId,
                        previousPriority.SlotId,
                        false);
                    if (result == null || !result.Succeeded) break;
                }
                if (result != null && result.Succeeded)
                    result = SignRemainingFounders078();
                if (result != null && result.Succeeded)
                    result = _coordinator.CompleteEquipmentReview();
                if (result != null && result.Succeeded)
                    result = PrepareFoundingUnionDrafts078(recruit.RecruitId);
                if ((result == null || !result.Succeeded) &&
                    (replacementLockAdded || previousPriorities.Length > 0))
                {
                    var originalFailure = result;
                    var rollback = M1CommandResult.Success();
                    foreach (var previousPriority in previousPriorities)
                    {
                        rollback = _coordinator.SetEquipmentLock(
                            recruit.RecruitId,
                            previousPriority.SlotId,
                            true);
                        if (rollback == null || !rollback.Succeeded) break;
                    }
                    if (rollback != null && rollback.Succeeded &&
                        replacementLockAdded)
                    {
                        rollback = _coordinator.SetEquipmentLock(
                            recruit.RecruitId,
                            slot.SlotId,
                            false);
                    }
                    result = rollback == null || !rollback.Succeeded
                        ? M1CommandResult.Failure(
                            (originalFailure?.Message ??
                             "The replacement equipment order failed.") +
                            " The previous priority could not be restored.")
                        : originalFailure;
                }
            }
            finally
            {
                _building = previousBuilding;
            }

            if (result == null || !result.Succeeded)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.ChooseEquipmentPriority,
                    FoundingPreparationStage078.PlaceFieldLead);
                SetFoundingFailure078(result?.Message ??
                                      "The rescue loadout could not be saved.");
                return;
            }

            _foundingEquipmentSlotId078 = slotId;
            _foundingPlacementChoiceUnionIndex078 = -1;
            _foundingPreparationStage078 =
                FoundingPreparationStage078.PlaceFieldLead;
            CompleteFoundingEditIfResumed078();
            _localStatus = FoundingEquipmentDisplayName078(slot) +
                           " is locked for the first rescue. Place the field lead.";
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private M1CommandResult SignRemainingFounders078()
        {
            var founders = (_coordinator.State.Applicants ??
                            Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                .ToArray();
            if (founders.Length != OpeningFlowState.RequiredOpeningRecruitCount)
                return M1CommandResult.Failure(
                    "The six founding volunteers are not all available.");

            foreach (var founder in founders)
            {
                var signed = (_coordinator.State.Recruits ??
                              Array.Empty<M1RecruitLoadoutView>())
                    .Any(value => value != null &&
                        StringComparer.Ordinal.Equals(
                            value.RecruitId,
                            founder.RecruitId));
                if (signed) continue;
                var result = _coordinator.SignRecruit(founder.RecruitId);
                if (result == null || !result.Succeeded) return result;
            }
            return M1CommandResult.Success("All six founders answered the charter.");
        }

        private M1CommandResult PrepareFoundingUnionDrafts078(string leadRecruitId)
        {
            var unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            if (unions.Count == 0)
            {
                var first = _coordinator.AddUnion();
                if (first == null || !first.Succeeded) return first;
                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            }
            while (unions.Count < 3)
            {
                var added = _coordinator.AddUnion();
                if (added == null || !added.Succeeded) return added;
                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            }
            if (unions.Count > NormalUnionPlanRules.MaximumPlanCount)
                return M1CommandResult.Failure(
                    "The campaign Union-plan capacity was exceeded.");

            var leadIsAssigned = unions.Any(value => value != null &&
                (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(
                    leadRecruitId,
                    StringComparer.Ordinal));
            if (leadIsAssigned)
            {
                var unassigned = _coordinator.UnassignRecruitFromUnion(leadRecruitId);
                if (unassigned == null || !unassigned.Succeeded) return unassigned;
            }
            return M1CommandResult.Success(
                "Three readable Union destinations are ready.");
        }

        private void CommitFoundingPlacement078(int unionIndex)
        {
            var recruit = FoundingLeadRecruit078();
            var union = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .FirstOrDefault(value => value != null && value.Index == unionIndex);
            if (recruit == null || union == null)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.PlaceFieldLead,
                    FoundingPreparationStage078.ChooseFormation);
                SetFoundingFailure078("Choose one of the three visible Union jobs.");
                return;
            }

            var existingPlacement = (_coordinator.State.Unions ??
                                     Array.Empty<M1UnionView>())
                .FirstOrDefault(value => value != null &&
                    (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(
                        recruit.RecruitId,
                        StringComparer.Ordinal));
            var previousSlot = existingPlacement == null
                ? -1
                : (existingPlacement.MemberRecruitIds ?? Array.Empty<string>())
                    .ToList()
                    .FindIndex(value => StringComparer.Ordinal.Equals(
                        value,
                        recruit.RecruitId));
            var previousWasLeader = existingPlacement != null &&
                                    StringComparer.Ordinal.Equals(
                                        existingPlacement.LeaderRecruitId,
                                        recruit.RecruitId);
            var targetCount = union.MemberRecruitIds?.Count ?? 0;
            var projectedTargetCount = targetCount -
                (existingPlacement != null && existingPlacement.Index == unionIndex
                    ? 1
                    : 0);
            if (projectedTargetCount >= NormalUnionPlanRules.MaximumMembersPerUnion)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.PlaceFieldLead,
                    FoundingPreparationStage078.ChooseFormation);
                SetFoundingFailure078("That Union is already full. Choose another color.");
                return;
            }

            var removedPrevious = false;
            var assignedReplacement = false;
            M1CommandResult result = M1CommandResult.Success();
            var previousBuilding = _building;
            _building = true;
            try
            {
                if (existingPlacement != null)
                {
                    result = _coordinator.UnassignRecruitFromUnion(recruit.RecruitId);
                    removedPrevious = result != null && result.Succeeded;
                }

                if (result != null && result.Succeeded)
                {
                    union = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                        .FirstOrDefault(value => value != null &&
                            value.Index == unionIndex);
                    var targetSlot = union?.MemberRecruitIds?.Count ?? 0;
                    result = union == null ||
                             targetSlot >= NormalUnionPlanRules.MaximumMembersPerUnion
                        ? M1CommandResult.Failure(
                            "That Union is already full. Choose another color.")
                        : _coordinator.AssignRecruitToUnion(
                            recruit.RecruitId,
                            union.Index,
                            targetSlot);
                    assignedReplacement = result != null && result.Succeeded;
                }
                if (result != null && result.Succeeded)
                {
                    result = _coordinator.SetUnionLeader(
                        union.Index,
                        recruit.RecruitId);
                }
                if ((result == null || !result.Succeeded) &&
                    (removedPrevious || assignedReplacement))
                {
                    var originalFailure = result;
                    var rollback = RestoreFoundingPlacement078(
                        recruit.RecruitId,
                        existingPlacement,
                        previousSlot,
                        previousWasLeader,
                        assignedReplacement);
                    result = rollback == null || !rollback.Succeeded
                        ? M1CommandResult.Failure(
                            (originalFailure?.Message ??
                             "The replacement Union order failed.") +
                            " The previous placement could not be restored.")
                        : originalFailure;
                }
            }
            finally
            {
                _building = previousBuilding;
            }
            if (result == null || !result.Succeeded)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.PlaceFieldLead,
                    FoundingPreparationStage078.ChooseFormation);
                SetFoundingFailure078(result?.Message ??
                                      "The field lead could not be placed.");
                return;
            }

            _foundingLeadUnionIndex078 = union.Index;
            _foundingFormationId078 = string.Empty;
            _foundingPreparationStage078 =
                FoundingPreparationStage078.ChooseFormation;
            CompleteFoundingEditIfResumed078();
            _localStatus = recruit.DisplayName + " now leads " +
                           M1UnionIdentity076.Resolve(
                               union.UnionId,
                               union.DisplayName,
                               union.Index) + ".";
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private M1CommandResult RestoreFoundingPlacement078(
            string recruitId,
            M1UnionView previousPlacement,
            int previousSlot,
            bool previousWasLeader,
            bool replacementWasAssigned)
        {
            M1CommandResult result = M1CommandResult.Success();
            if (replacementWasAssigned)
                result = _coordinator.UnassignRecruitFromUnion(recruitId);
            if (result == null || !result.Succeeded || previousPlacement == null)
                return result;

            result = _coordinator.AssignRecruitToUnion(
                recruitId,
                previousPlacement.Index,
                Mathf.Max(0, previousSlot));
            if (result != null && result.Succeeded && previousWasLeader)
                result = _coordinator.SetUnionLeader(
                    previousPlacement.Index,
                    recruitId);
            return result;
        }

        private void CommitFoundingFormation078(string formationId)
        {
            var formation = (_coordinator.State.Formations ??
                             Array.Empty<M1ChoiceView>())
                .Take(3)
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.Id, formationId));
            if (formation == null || _foundingLeadUnionIndex078 < 0)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.ChooseFormation,
                    FoundingPreparationStage078.ConfirmRescueTeam);
                SetFoundingFailure078("Choose one of the three starter formations.");
                return;
            }

            M1CommandResult result;
            var previousBuilding = _building;
            _building = true;
            try
            {
                result = _coordinator.SetFormation(
                    _foundingLeadUnionIndex078,
                    formation.Id);
            }
            finally
            {
                _building = previousBuilding;
            }
            if (result == null || !result.Succeeded)
            {
                HoldFoundingDecisionForRetry078(
                    FoundingPreparationStage078.ChooseFormation,
                    FoundingPreparationStage078.ConfirmRescueTeam);
                SetFoundingFailure078(result?.Message ??
                                      "The formation order could not be saved.");
                return;
            }

            _foundingFormationId078 = formation.Id;
            _foundingPreparationStage078 =
                FoundingPreparationStage078.ConfirmRescueTeam;
            CompleteFoundingEditIfResumed078();
            _localStatus = formation.DisplayName +
                           " is saved for the lead Union. Confirm the rescue team.";
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private void ConfirmFoundingRescueTeam078()
        {
            var result = M1CommandResult.Success();
            var previousBuilding = _building;
            _building = true;
            try
            {
                result = AutoFillFoundingRescueTeam078();
                if (result != null && result.Succeeded)
                    result = _coordinator.SaveAndReloadProof();
                var founderIds = (_coordinator.State.Applicants ??
                                  Array.Empty<M1ApplicantView>())
                    .Where(value => value != null)
                    .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                    .Select(value => value.RecruitId)
                    .ToArray();
                if (result != null && result.Succeeded)
                    result = FillFirstHourReadyUnions071(founderIds);
                if (result != null && result.Succeeded)
                    result = _coordinator.SaveAndReloadProof();
            }
            finally
            {
                _building = previousBuilding;
            }

            if (result == null || !result.Succeeded)
            {
                SetFoundingFailure078(result?.Message ??
                                      "The rescue team could not be confirmed.");
                return;
            }

            var activeUnions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                (value.MemberRecruitIds?.Count ?? 0) > 0)
                .ToArray();
            if ((_coordinator.State.Recruits?.Count ?? 0) != 10 ||
                !_coordinator.State.OpeningUnionsLegal ||
                activeUnions.Length != 3 ||
                activeUnions.Any(value =>
                    value.MemberRecruitIds.Count >
                    NormalUnionPlanRules.MaximumMembersPerUnion) ||
                activeUnions.Length > NormalUnionPlanRules.MaximumPlanCount)
            {
                SetFoundingFailure078(
                    "The saved rescue team did not pass its final legal-slot check.");
                return;
            }

            _foundingPreparationStage078 = FoundingPreparationStage078.Complete;
            _foundingPreparationEditingPrevious078 = false;
            _foundingPreparationEditResumeStage078 =
                FoundingPreparationStage078.ChooseFieldLead;
            _foundingBriefingPage076 = 1;
            _localStatus =
                "Rescue team confirmed: three field Unions and one ready reserve.";
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private M1CommandResult AutoFillFoundingRescueTeam078()
        {
            var founderIds = (_coordinator.State.Applicants ??
                              Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                .Select(value => value.RecruitId)
                .ToArray();
            var founders = (_coordinator.State.Recruits ??
                            Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null &&
                                founderIds.Contains(
                                    value.RecruitId,
                                    StringComparer.Ordinal))
                .ToArray();
            var unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            if (founders.Length != OpeningFlowState.RequiredOpeningRecruitCount ||
                unions.Length != 3 ||
                string.IsNullOrWhiteSpace(_foundingLeadRecruitId078))
                return M1CommandResult.Failure(
                    "Six founders and three Union destinations are required.");

            var selectedPosition = Array.FindIndex(
                unions,
                value => value.Index == _foundingLeadUnionIndex078);
            if (selectedPosition < 0) selectedPosition = 0;
            var groups = BuildFoundingRoleGroupsForVerification078(
                founders,
                _foundingLeadRecruitId078,
                selectedPosition);
            if (groups.Count != 3 || groups.Any(value => value.Count != 2))
                return M1CommandResult.Failure(
                    "The safe team fill did not produce three two-person founder groups.");

            var assignedFounderIds = new HashSet<string>(
                unions.SelectMany(value => value.MemberRecruitIds ??
                                           Array.Empty<string>())
                    .Where(value => founderIds.Contains(value, StringComparer.Ordinal)),
                StringComparer.Ordinal);
            foreach (var founderId in assignedFounderIds)
            {
                var removed = _coordinator.UnassignRecruitFromUnion(founderId);
                if (removed == null || !removed.Succeeded) return removed;
            }

            for (var unionPosition = 0; unionPosition < unions.Length; unionPosition++)
            {
                var members = groups[unionPosition];
                for (var slot = 0; slot < members.Count; slot++)
                {
                    var assigned = _coordinator.AssignRecruitToUnion(
                        members[slot],
                        unions[unionPosition].Index,
                        slot);
                    if (assigned == null || !assigned.Succeeded) return assigned;
                }
            }

            var formations = (_coordinator.State.Formations ??
                              Array.Empty<M1ChoiceView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            var remainingFormationIds = formations
                .Select(value => value.Id)
                .Where(value => !StringComparer.Ordinal.Equals(
                    value,
                    _foundingFormationId078))
                .ToList();
            for (var unionPosition = 0; unionPosition < unions.Length; unionPosition++)
            {
                var formationId = unionPosition == selectedPosition
                    ? _foundingFormationId078
                    : remainingFormationIds.Count > 0
                        ? PopFirst078(remainingFormationIds)
                        : formations.FirstOrDefault()?.Id;
                if (string.IsNullOrWhiteSpace(formationId)) continue;
                var formation = _coordinator.SetFormation(
                    unions[unionPosition].Index,
                    formationId);
                if (formation == null || !formation.Succeeded) return formation;
            }

            var active = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                (value.MemberRecruitIds?.Count ?? 0) > 0)
                .ToArray();
            return _coordinator.State.OpeningUnionsLegal &&
                   active.Length == 3 &&
                   active.All(value => value.MemberRecruitIds.Count == 2 &&
                       value.MemberRecruitIds.Count <=
                       NormalUnionPlanRules.MaximumMembersPerUnion)
                ? M1CommandResult.Success(
                    "The six founders were distributed safely into three compatible pairs.")
                : M1CommandResult.Failure(
                    "The safe team fill did not produce three legal founder Unions.");
        }

        public static IReadOnlyList<IReadOnlyList<string>>
            BuildFoundingRoleGroupsForVerification078(
                IReadOnlyList<M1RecruitLoadoutView> founders,
                string leadRecruitId,
                int leadGroupIndex)
        {
            var source = (founders ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.RecruitId))
                .GroupBy(value => value.RecruitId, StringComparer.Ordinal)
                .Select(value => value.First())
                .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                .ToArray();
            var groups = new[]
            {
                new List<string>(2),
                new List<string>(2),
                new List<string>(2)
            };
            if (source.Length == 0) return Array.AsReadOnly(
                groups.Select(value => (IReadOnlyList<string>)value.AsReadOnly())
                    .ToArray());

            var lead = source.FirstOrDefault(value => StringComparer.Ordinal.Equals(
                           value.RecruitId,
                           leadRecruitId)) ?? source[0];
            var leadGroup = Mathf.Clamp(leadGroupIndex, 0, groups.Length - 1);
            groups[leadGroup].Add(lead.RecruitId);
            var remaining = source.Where(value => !StringComparer.Ordinal.Equals(
                                      value.RecruitId,
                                      lead.RecruitId))
                .ToList();
            var leadBand = FoundingRoleBandForVerification078(lead.ObservedClass);
            var partner = remaining.FirstOrDefault(value => StringComparer.Ordinal.Equals(
                FoundingRoleBandForVerification078(value.ObservedClass),
                leadBand));
            if (partner != null)
            {
                groups[leadGroup].Add(partner.RecruitId);
                remaining.Remove(partner);
            }

            foreach (var bandGroup in remaining
                         .GroupBy(value => FoundingRoleBandForVerification078(
                             value.ObservedClass), StringComparer.Ordinal)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var bandMembers = bandGroup.ToArray();
                var memberIndex = 0;
                while (memberIndex < bandMembers.Length)
                {
                    var target = Enumerable.Range(0, groups.Length)
                        .Where(index => groups[index].Count < 2)
                        .OrderBy(index => groups[index].Count == 0 ? 0 : 1)
                        .ThenBy(index => index == leadGroup ? 1 : 0)
                        .ThenBy(index => index)
                        .FirstOrDefault();
                    while (memberIndex < bandMembers.Length &&
                           groups[target].Count < 2)
                    {
                        groups[target].Add(bandMembers[memberIndex].RecruitId);
                        memberIndex++;
                    }
                }
            }

            return Array.AsReadOnly(groups
                .Select(value => (IReadOnlyList<string>)value.AsReadOnly())
                .ToArray());
        }

        public static string FoundingRoleBandForVerification078(string observedClass)
        {
            var role = (observedClass ?? string.Empty).ToUpperInvariant();
            if (role.Contains("GUARD") || role.Contains("WARRIOR") ||
                role.Contains("FIGHTER")) return "FRONTLINE";
            if (role.Contains("PRIEST") || role.Contains("HEAL") ||
                role.Contains("MAGE") || role.Contains("MYSTIC")) return "SUPPORT";
            if (role.Contains("RANGER") || role.Contains("ROGUE") ||
                role.Contains("SCOUT")) return "MOBILE";
            return "FLEX";
        }

        public static IReadOnlyList<Rect> FoundingLeadCardRectsForVerification078(
            int visibleCount)
        {
            var count = Mathf.Clamp(visibleCount, 0,
                OpeningFlowState.RequiredOpeningRecruitCount);
            var result = new List<Rect>(count);
            const int columns = 3;
            const float gapX = 0.012f;
            const float gapY = 0.018f;
            var region = new Rect(0.018f, 0.275f, 0.964f, 0.430f);
            var width = (region.width - gapX * 2f) / columns;
            var height = (region.height - gapY) / 2f;
            for (var index = 0; index < count; index++)
            {
                var column = index % columns;
                var row = index / columns;
                result.Add(new Rect(
                    region.xMin + column * (width + gapX),
                    region.yMax - (row + 1) * height - row * gapY,
                    width,
                    height));
            }
            return result.AsReadOnly();
        }

        public static bool FoundingCanEditPreviousForVerification078(
            FoundingPreparationStage078 stage)
        {
            return stage >= FoundingPreparationStage078.ChooseEquipmentPriority &&
                   stage <= FoundingPreparationStage078.ConfirmRescueTeam;
        }

        public static FoundingPreparationStage078
            ResolveFoundingStageForVerification078(
                FoundingPreparationStage078 currentStage,
                bool preserveExplicitStage,
                int recruitCount,
                bool hasLeadUnion)
        {
            if (preserveExplicitStage) return currentStage;
            if (recruitCount <= 0)
                return FoundingPreparationStage078.ChooseFieldLead;
            if (recruitCount < OpeningFlowState.RequiredOpeningRecruitCount)
                return FoundingPreparationStage078.ChooseEquipmentPriority;
            if (!hasLeadUnion)
                return FoundingPreparationStage078.PlaceFieldLead;
            return currentStage < FoundingPreparationStage078.ChooseFormation
                ? FoundingPreparationStage078.ChooseFormation
                : currentStage;
        }

        public static FoundingPreparationStage078
            FoundingPreviousStageForVerification078(
                FoundingPreparationStage078 stage)
        {
            switch (stage)
            {
                case FoundingPreparationStage078.ChooseEquipmentPriority:
                    return FoundingPreparationStage078.ChooseFieldLead;
                case FoundingPreparationStage078.PlaceFieldLead:
                    return FoundingPreparationStage078.ChooseEquipmentPriority;
                case FoundingPreparationStage078.ChooseFormation:
                    return FoundingPreparationStage078.PlaceFieldLead;
                case FoundingPreparationStage078.ConfirmRescueTeam:
                case FoundingPreparationStage078.Complete:
                    return FoundingPreparationStage078.ChooseFormation;
                default:
                    return FoundingPreparationStage078.ChooseFieldLead;
            }
        }

        public static string FoundingEditPreviousLabelForVerification078(
            FoundingPreparationStage078 stage)
        {
            switch (stage)
            {
                case FoundingPreparationStage078.ChooseEquipmentPriority:
                    return "←  EDIT FIELD LEAD";
                case FoundingPreparationStage078.PlaceFieldLead:
                    return "←  EDIT EQUIPMENT";
                case FoundingPreparationStage078.ChooseFormation:
                    return "←  EDIT UNION";
                default:
                    return "←  EDIT FORMATION";
            }
        }

        public static string FoundingEquipmentDisplayNameForVerification078(
            string displayedName,
            string itemId,
            string slotId)
        {
            var display = (displayedName ?? string.Empty).Trim();
            if (!LooksLikeEquipmentAuthorityId078(display)) return display;

            // The runtime slot's item id is an opaque instance id. The displayed
            // value carries the definition token when legacy content has no
            // authored display name, so humanize it before ever consulting the id.
            var authorityId = !string.IsNullOrWhiteSpace(display)
                ? display
                : itemId?.Trim() ?? string.Empty;
            if (authorityId.StartsWith("SIGI_", StringComparison.OrdinalIgnoreCase) ||
                authorityId.StartsWith("PROC_", StringComparison.OrdinalIgnoreCase))
                return FoundingEquipmentFallback078(slotId);
            var tokens = authorityId.Replace('-', '_')
                .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var firstWord = 0;
            while (firstWord < tokens.Length &&
                   IsEquipmentAuthorityPrefix078(tokens[firstWord]))
                firstWord++;
            var friendly = string.Join(" ", tokens.Skip(firstWord)
                .Select(TitleEquipmentWord078));
            if (!string.IsNullOrWhiteSpace(friendly)) return friendly;

            return FoundingEquipmentFallback078(slotId);
        }

        private static string FoundingEquipmentFallback078(string slotId)
        {
            return StringComparer.Ordinal.Equals(slotId, EquipmentSlotIds.MainHand)
                ? "Starter Weapon"
                : StringComparer.Ordinal.Equals(slotId, EquipmentSlotIds.BodyArmor)
                    ? "Starter Armor"
                    : "Starter Equipment";
        }

        public static Color FoundingGoldChoiceSurfaceForVerification078(bool selected)
        {
            return selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal;
        }

        public static Color FoundingGoldChoiceLabelForVerification078(bool selected)
        {
            return selected
                ? new Color(0.07f, 0.08f, 0.09f, 1f)
                : RuntimeUi.Text;
        }

        public static Color FoundingGoldChoiceFocusedSurfaceForVerification078()
        {
            return RuntimeUi.Warning;
        }

        public static Color FoundingGoldChoicePressedSurfaceForVerification078()
        {
            return new Color(0.72f, 0.54f, 0.24f, 1f);
        }

        public static Color FoundingDisabledActionSurfaceForVerification078()
        {
            return new Color(0.09f, 0.13f, 0.18f, 1f);
        }

        public static Color FoundingDisabledActionLabelForVerification078()
        {
            return RuntimeUi.Text;
        }

        private void EditPreviousFoundingDecision078()
        {
            if (!FoundingCanEditPreviousForVerification078(
                    _foundingPreparationStage078)) return;
            if (!_foundingPreparationEditingPrevious078)
                _foundingPreparationEditResumeStage078 =
                    _foundingPreparationStage078;
            _foundingPreparationStage078 =
                FoundingPreviousStageForVerification078(
                    _foundingPreparationStage078);
            _foundingPreparationEditingPrevious078 = true;
            _localStatus = "Review the previous rescue-team decision, then continue.";
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private void CompleteFoundingEditIfResumed078()
        {
            if (!_foundingPreparationEditingPrevious078 ||
                _foundingPreparationStage078 <
                _foundingPreparationEditResumeStage078) return;
            _foundingPreparationEditingPrevious078 = false;
            _foundingPreparationEditResumeStage078 =
                FoundingPreparationStage078.ChooseFieldLead;
        }

        private void HoldFoundingDecisionForRetry078(
            FoundingPreparationStage078 failedStage,
            FoundingPreparationStage078 resumeStage)
        {
            if (!_foundingPreparationEditingPrevious078 ||
                resumeStage > _foundingPreparationEditResumeStage078)
            {
                _foundingPreparationEditResumeStage078 = resumeStage;
            }
            _foundingPreparationStage078 = failedStage;
            _foundingPreparationEditingPrevious078 = true;
        }

        private Button AddFoundingBack078(Transform parent, bool focus = false)
        {
            if (!FoundingCanEditPreviousForVerification078(
                    _foundingPreparationStage078)) return null;
            var back = RuntimeUi.AddButton(
                parent,
                FoundingPreparationBackName078,
                FoundingEditPreviousLabelForVerification078(
                    _foundingPreparationStage078),
                EditPreviousFoundingDecision078,
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);
            AnchorStudioRect076(
                back.GetComponent<RectTransform>(),
                new Vector2(0.025f, 0.055f),
                new Vector2(0.19f, 0.205f));
            ConfigureResponsiveText062(back.GetComponentInChildren<Text>(), 14, 20);
            if (focus) back.Select();
            return back;
        }

        private static void StyleFoundingGoldChoice078(Button button, bool selected)
        {
            if (button != null && selected)
            {
                var colors = button.colors;
                colors.normalColor =
                    FoundingGoldChoiceSurfaceForVerification078(true);
                colors.highlightedColor =
                    FoundingGoldChoiceFocusedSurfaceForVerification078();
                colors.selectedColor =
                    FoundingGoldChoiceFocusedSurfaceForVerification078();
                colors.pressedColor =
                    FoundingGoldChoicePressedSurfaceForVerification078();
                button.colors = colors;
            }
            var label = FoundingButtonLabel078(button);
            if (label != null)
                label.color = FoundingGoldChoiceLabelForVerification078(selected);
        }

        private static void StyleFoundingCallToAction078(Button button)
        {
            if (button == null || button.interactable) return;
            var colors = button.colors;
            colors.disabledColor = FoundingDisabledActionSurfaceForVerification078();
            button.colors = colors;
            var label = FoundingButtonLabel078(button);
            if (label != null)
                label.color = FoundingDisabledActionLabelForVerification078();
        }

        private static Text FoundingButtonLabel078(Button button)
        {
            if (button == null) return null;
            return button.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(value => value != null &&
                    value.gameObject.name.StartsWith("Label", StringComparison.Ordinal));
        }

        private static string FoundingEquipmentDisplayName078(
            M1EquipmentSlotView slot)
        {
            return FoundingEquipmentDisplayNameForVerification078(
                slot?.EquippedItemName,
                slot?.EquippedItemId,
                slot?.SlotId);
        }

        private static bool LooksLikeEquipmentAuthorityId078(string displayedName)
        {
            if (string.IsNullOrWhiteSpace(displayedName)) return true;
            return displayedName.StartsWith("EQ_", StringComparison.OrdinalIgnoreCase) ||
                   (displayedName.IndexOf('_') >= 0 &&
                    displayedName.IndexOf(' ') < 0);
        }

        private static bool IsEquipmentAuthorityPrefix078(string token)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(token, "EQ") ||
                   StringComparer.OrdinalIgnoreCase.Equals(token, "PROC") ||
                   StringComparer.OrdinalIgnoreCase.Equals(token, "ITEM") ||
                   StringComparer.OrdinalIgnoreCase.Equals(token, "EQUIPMENT");
        }

        private static string TitleEquipmentWord078(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            var lower = token.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        private M1RecruitLoadoutView FoundingLeadRecruit078()
        {
            var recruits = _coordinator?.State?.Recruits ??
                           Array.Empty<M1RecruitLoadoutView>();
            var selected = recruits.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    _foundingLeadRecruitId078));
            if (selected != null) return selected;
            selected = recruits.FirstOrDefault(value => value != null &&
                (value.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .Any(slot => slot != null && slot.IsLocked));
            if (selected != null)
                _foundingLeadRecruitId078 = selected.RecruitId;
            return selected;
        }

        private Text AddFoundingInstruction078(
            Transform parent,
            string heading,
            string copy)
        {
            var panel = RuntimeUi.AddPanel(
                parent,
                "Founding Preparation Instruction 078",
                new Color(0.020f, 0.040f, 0.050f, 0.97f));
            AnchorStudioRect076(
                panel.rectTransform,
                new Vector2(0.035f, 0.725f),
                new Vector2(0.965f, 0.835f));
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldPaper);
            var text = RuntimeUi.AddText(
                panel.transform,
                "Founding Preparation Instruction Copy 078",
                heading + "\n" + copy,
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorStudioRect076(
                text.rectTransform,
                new Vector2(0.025f, 0.08f),
                new Vector2(0.975f, 0.92f));
            ConfigureResponsiveText062(text, 15, 23);
            return text;
        }

        private void AddFoundingStatus078(Transform parent, string defaultText)
        {
            var failed = !_localStatusPositive &&
                         !string.IsNullOrWhiteSpace(_localStatus);
            var canEditPrevious = FoundingCanEditPreviousForVerification078(
                _foundingPreparationStage078);
            var isOpeningBreadcrumb = !failed &&
                                      _foundingPreparationStage078 ==
                                      FoundingPreparationStage078.ChooseFieldLead;
            var status = RuntimeUi.AddText(
                parent,
                "Founding Preparation Status 078",
                failed ? "ORDER NOT SAVED  •  " + _localStatus : defaultText,
                17,
                TextAnchor.MiddleLeft,
                failed ? RuntimeUi.Warning : RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorStudioRect076(
                status.rectTransform,
                canEditPrevious
                    ? new Vector2(0.205f, 0.035f)
                    : new Vector2(0.025f, 0.035f),
                isOpeningBreadcrumb
                    ? new Vector2(0.64f, 0.225f)
                    : canEditPrevious
                        ? new Vector2(0.535f, 0.225f)
                        : new Vector2(0.45f, 0.225f));
            if (isOpeningBreadcrumb)
            {
                ConfigureAuthoredCompactText076(status, 12, 16);
                status.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            else
            {
                ConfigureResponsiveText062(status, 13, 18);
            }
        }

        private void AddFoundingBlocking078(Transform parent, string copy)
        {
            var message = RuntimeUi.AddPanel(
                parent,
                "Founding Preparation Blocking 078",
                new Color(0.08f, 0.03f, 0.02f, 0.98f));
            AnchorStudioRect076(
                message.rectTransform,
                new Vector2(0.12f, 0.30f),
                new Vector2(0.88f, 0.65f));
            var text = RuntimeUi.AddText(
                message.transform,
                "Founding Preparation Blocking Copy 078",
                copy,
                24,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(24f, 18f);
            text.rectTransform.offsetMax = new Vector2(-24f, -18f);
            ConfigureResponsiveText062(text, 17, 25);
        }

        private void SetFoundingFailure078(string message)
        {
            _localStatus = string.IsNullOrWhiteSpace(message)
                ? "The Guild order could not be saved. Nothing was changed."
                : message.Trim();
            _localStatusPositive = false;
            BuildCurrentScreen();
        }

        private static string FoundingPreparationHeading078(
            FoundingPreparationStage078 stage)
        {
            switch (stage)
            {
                case FoundingPreparationStage078.ChooseFieldLead:
                    return "MEET THE SIX WHO ANSWERED";
                case FoundingPreparationStage078.ChooseEquipmentPriority:
                    return "READY ONE PERSON";
                case FoundingPreparationStage078.PlaceFieldLead:
                    return "CHOOSE THE FIRST UNION'S JOB";
                case FoundingPreparationStage078.ChooseFormation:
                    return "CHOOSE HOW THEY FIGHT";
                default:
                    return "YOUR FIRST RESCUE TEAM";
            }
        }

        public static string FoundingRolePromiseForVerification078(
            string observedClass)
        {
            var role = (observedClass ?? string.Empty).ToUpperInvariant();
            if (role.Contains("GUARD")) return "HOLDS THE LINE";
            if (role.Contains("WARRIOR") || role.Contains("FIGHTER"))
                return "BREAKS THE ENEMY LINE";
            if (role.Contains("PRIEST") || role.Contains("HEAL"))
                return "RESTORES ALLIES";
            if (role.Contains("MAGE") || role.Contains("MYSTIC"))
                return "CHANNELS BATTLE ARTS";
            if (role.Contains("RANGER") || role.Contains("SCOUT"))
                return "SCOUTS THE FIELD";
            if (role.Contains("ROGUE")) return "FLANKS OPEN THREATS";
            return "ADAPTS TO THE ORDER";
        }

        public static string FoundingPromiseRoleForVerification079(
            int founderIndex,
            string observedClass)
        {
            // Promise ribbons teach the role without contradicting the permanent
            // class shown everywhere else. Duplicate classes intentionally share
            // the same color and promise.
            _ = founderIndex;
            return UnionPlannerRoleDesignationForVerification074(observedClass);
        }

        public static Rect FoundingEquipmentArtworkRectForVerification079 =>
            new Rect(0.015f, 0.10f, 0.220f, 0.80f);

        public static Rect FoundingEquipmentLabelRectForVerification079(
            bool selected) => selected
            ? new Rect(0.255f, 0.06f, 0.475f, 0.88f)
            : new Rect(0.255f, 0.06f, 0.700f, 0.88f);

        public static Rect FoundingEquipmentPriorityBadgeRectForVerification079 =>
            new Rect(0.760f, 0.640f, 0.225f, 0.270f);

        public const float FoundingEquipmentCardWidthFraction079 = 0.605f;

        private static string FoundingRolePromise078(string observedClass)
        {
            return FoundingRolePromiseForVerification078(observedClass);
        }

        private static string EquipmentPriorityPromise078(string slotId)
        {
            return StringComparer.Ordinal.Equals(slotId, EquipmentSlotIds.MainHand)
                ? "KEEP THIS WEAPON DISCIPLINE READY"
                : "KEEP THIS PROTECTION READY";
        }

        private static Color FoundingDestinationColor078(int index)
        {
            switch (index)
            {
                case 0: return new Color(0.25f, 0.63f, 0.92f, 1f);
                case 1: return new Color(0.92f, 0.39f, 0.30f, 1f);
                default: return new Color(0.34f, 0.82f, 0.53f, 1f);
            }
        }

        public static Color FoundingDestinationColorForVerification078(int index)
        {
            return FoundingDestinationColor078(index);
        }

        public static string FoundingDestinationRibbonForVerification078(int index)
        {
            switch (index)
            {
                case 0: return "HOLD TEAM";
                case 1: return "STRIKE TEAM";
                default: return "SCOUT TEAM";
            }
        }

        private static string FoundingDestinationPromise078(int index)
        {
            switch (index)
            {
                case 0: return "HOLD  •  PROTECT ALLIES UNDER PRESSURE";
                case 1: return "BREAK  •  FOCUS FORCE ON ONE OPENING";
                default: return "MOVE  •  ANSWER CHANGING THREATS";
            }
        }

        private void AddFoundingFormationDiagram078(
            Button card,
            M1ChoiceView formation,
            bool selected)
        {
            if (card == null || formation == null) return;
            var diagram = RuntimeUi.AddPanel(
                card.transform,
                "Founding Formation Diagram " + formation.Id + " 078",
                new Color(0.010f, 0.025f, 0.037f, 0.98f));
            diagram.raycastTarget = false;
            AnchorStudioRect076(
                diagram.rectTransform,
                new Vector2(0.07f, 0.27f),
                new Vector2(0.93f, 0.78f));
            M1PremiumUi.StylePanel(diagram, M1PremiumUi.Surface.WorldRibbon);

            var frontCue = RuntimeUi.AddText(
                diagram.transform,
                "Founding Formation Front Cue " + formation.Id + " 078",
                "ENEMY SIDE  ↑",
                14,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorStudioRect076(
                frontCue.rectTransform,
                new Vector2(0.05f, 0.79f),
                new Vector2(0.95f, 0.98f));
            ConfigureResponsiveText062(frontCue, 11, 15);
            frontCue.raycastTarget = false;

            var positions = RuntimeUi.AddText(
                diagram.transform,
                "Founding Formation Positions " + formation.Id + " 078",
                UnionPlannerFormationDiagramForVerification078(formation.Id),
                28,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorStudioRect076(
                positions.rectTransform,
                new Vector2(0.05f, 0.17f),
                new Vector2(0.95f, 0.80f));
            ConfigureResponsiveText062(positions, 16, 30);
            positions.raycastTarget = false;

            var rearCue = RuntimeUi.AddText(
                diagram.transform,
                "Founding Formation Rear Cue " + formation.Id + " 078",
                "BACK LINE",
                13,
                TextAnchor.MiddleCenter,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorStudioRect076(
                rearCue.rectTransform,
                new Vector2(0.05f, 0.01f),
                new Vector2(0.95f, 0.18f));
            ConfigureResponsiveText062(rearCue, 10, 14);
            rearCue.raycastTarget = false;

            var promise = RuntimeUi.AddText(
                card.transform,
                "Founding Formation Promise " + formation.Id + " 078",
                FriendlyFormationPromise078(formation),
                16,
                TextAnchor.MiddleCenter,
                FoundingGoldChoiceLabelForVerification078(selected),
                FontStyle.Bold);
            AnchorStudioRect076(
                promise.rectTransform,
                new Vector2(0.06f, 0.035f),
                new Vector2(0.94f, 0.235f));
            ConfigureResponsiveText062(promise, 12, 17);
            promise.raycastTarget = false;
        }

        public static string FriendlyFormationPromiseForVerification078(
            M1ChoiceView formation)
        {
            return FriendlyFormationPromise078(formation);
        }

        private static string FriendlyFormationPromise078(M1ChoiceView formation)
        {
            var id = (formation?.Id ?? string.Empty).ToUpperInvariant();
            var name = (formation?.DisplayName ?? string.Empty).ToUpperInvariant();
            if (id.Contains("SHIELD") || name.Contains("SHIELD"))
                return "STAND CLOSE  •  PROTECT EACH OTHER";
            if (id.Contains("WEDGE") || name.Contains("WEDGE"))
                return "PRESS THE CENTER  •  BREAK THE OPENING";
            return "SPREAD OUT  •  RESPOND QUICKLY";
        }

        private static string PopFirst078(IList<string> values)
        {
            if (values == null || values.Count == 0) return string.Empty;
            var first = values[0];
            values.RemoveAt(0);
            return first;
        }
    }
}
