using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        void StartCampaignQuest131(Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator, string chapterId)
        {
            RunWorldGateAnimatedCommand084(
                () => coordinator is Campaign020.ICampaignDeckPresentationCoordinator131 deck
                    ? deck.StartOrResumeCampaignDeck131(chapterId)
                    : coordinator.StartPlayableChapter020(chapterId),
                "This story quest could not continue. Your saved progress is safe.");
        }

        bool TryBuildCampaignDeckStep131(Transform fallback,
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            Campaign020.CampaignPlayablePresentationState020 state,
            Campaign020.CampaignStepView020 current, string chapterReceipt)
        {
            if (current?.IsWorldBoard != true || !string.IsNullOrWhiteSpace(state.PendingStepReceiptId) ||
                !string.IsNullOrWhiteSpace(chapterReceipt) ||
                !(coordinator is Campaign023.ICampaignWorldGatePresentationCoordinator023 world)) return false;
            var deck = world.CampaignWorldGate023;
            if (deck?.IsAvailable == true && deck.ActiveOperationKind == "CHAPTER" &&
                deck.ActiveDefinitionId == state.ActiveChapterId && !string.IsNullOrWhiteSpace(deck.ActiveOperationId))
            {
                BuildGuildCityWorldGate023(fallback, world, deck);
                return true;
            }
            var body = BuildCampaignQuestShell131(state.OperationTitle, current.Description,
                state.WorldName, "JOURNEY TO " + (state.WorldName ?? "THE NEXT WORLD"), ReturnToWalkableHall069);
            var scene = BuildCampaignQuestScene131(body, state.OperationTitle, current.Description,
                BoardRoomIllustrationResource091("STORY"));
            var reading = CampaignQuestActionsParent131(scene);
            if (deck?.IsAvailable != true || !string.IsNullOrWhiteSpace(deck.ActiveOperationId))
                AddMessagePanel(reading, "QUEST NEEDS ATTENTION", deck?.Error ??
                    "Continue the current adventure before opening another route.", RuntimeUi.Warning);
            else if (!StringComparer.Ordinal.Equals(deck.CurrentWorldId, state.WorldId))
            {
                var destination = deck.Standings?.FirstOrDefault(value => value.WorldId == state.WorldId);
                var copy = "TRAVEL TO " + (state.WorldName ?? state.WorldId).ToUpperInvariant() +
                    "  •  " + (destination?.TravelSupplyCost ?? 0) + " SUPPLIES";
                var travel = RuntimeUi.AddButton(reading, "Campaign journey travel 131", copy,
                    () => RunWorldGateAnimatedCommand084(() => world.TravelWorldGate023(state.WorldId),
                        "This journey could not begin. Your saved quest is safe."), 100f, RuntimeUi.Accent);
                travel.interactable = destination?.Unlocked == true && destination.CanAffordTravel;
                if (!travel.interactable)
                    AddMessagePanel(reading, "JOURNEY REQUIREMENT", destination?.Unlocked == true
                        ? "Travel supplies: " + deck.TravelSupplies + " / " + destination.TravelSupplyCost
                        : "This world's story gate is still locked.", RuntimeUi.Warning);
            }
            else
                RuntimeUi.AddButton(reading, "Resume saved campaign deck 131", "CONTINUE QUEST",
                    () => StartCampaignQuest131(coordinator, state.ActiveChapterId), 100f, RuntimeUi.Accent);
            FitCampaignQuestActions131(scene);
            return true;
        }

        void FitCampaignLockedEncounter131(RectTransform face)
        {
            StyleCampaignQuestExistingScene131(face, null);
            var layout = face.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var visual = face.Find("Locked Expedition Encounter Visual 089") as RectTransform;
            var reading = face.Find("Locked Expedition Encounter Decision 089") as RectTransform;
            if (visual != null)
            {
                var column = visual.GetComponent<VerticalLayoutGroup>();
                if (column != null) column.enabled = false;
                SetAnchors074(visual, new Vector2(.03f,.05f), new Vector2(.47f,.95f));
                var picture = visual.Find("Locked Expedition Encounter Card Face 089") as RectTransform;
                if (picture != null)
                {
                    var pictureSize = picture.GetComponent<ContentSizeFitter>();
                    if (pictureSize != null) pictureSize.enabled = false;
                    SetAnchors074(picture, new Vector2(.02f,.02f), new Vector2(.98f,.98f));
                }
            }
            if (reading != null)
            {
                SetAnchors074(reading, new Vector2(.50f,.06f), new Vector2(.97f,.95f));
                foreach (var copy in reading.GetComponentsInChildren<Text>(true))
                    ConfigureAuthoredCompactText076(copy, 20, 30);
                var heading = reading.Find("Locked Expedition Encounter Heading 089")?.GetComponent<Text>();
                if (heading != null) RuntimeUi.SetLayout(heading, preferredHeight: 88f);
                var details = reading.Find("Locked Expedition Encounter Preview 089")?.GetComponent<Text>();
                if (details != null) RuntimeUi.SetLayout(details, preferredHeight: 150f);
            }
            face.Find("Board Adventure Card Flip Stage 086")?.SetAsLastSibling();
        }

        bool TryBuildActiveCampaignDeck131(Transform fallback,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            if (_screenRoot == null || state.ActiveOperationKind != "CHAPTER" || state.CurrentNode == null) return false;
            if (!string.IsNullOrWhiteSpace(state.PendingReceiptId))
            {
                BuildFullScreenWorldGateEvent110(coordinator, state);
                return true;
            }
            var node = state.CurrentNode;
            var progress = "ROOM " + AdventureCurrentPlayableSpace084(state, AdventurePlayableSpaceCount084(state)) +
                " / " + AdventurePlayableSpaceCount084(state) + "  •  SUPPLIES " + state.Supplies;
            var body = BuildCampaignQuestShell131(state.ActiveBoardTitle, state.BoardObjective,
                state.CurrentWorldName, progress, ReturnToWalkableHall069);
            if (_showExpeditionDeckDetails089)
            {
                var helpContent164 = BuildCampaignHelpScroll164(body);
                BuildExpeditionDeckHelp089(helpContent164, coordinator, state);
                FinishCampaignHelpScroll164(body, helpContent164);
                return true;
            }
            var hasChoice = !node.RequiresBattle && state.ActiveStatus != "AwaitingBattle" &&
                state.ActiveStatus != "ReadyToFinalize" && state.RouteCards?.Count == 3 &&
                coordinator is Campaign023.IExpeditionDeckPresentationCoordinator089;
            if (hasChoice)
            {
                BuildExpeditionRouteRow089(body, (Campaign023.IExpeditionDeckPresentationCoordinator089)coordinator, state);
                var heading = body.Find("Expedition three card route heading 089") as RectTransform;
                var row = body.Find("Expedition route row 089") as RectTransform;
                if (heading != null)
                {
                    var layout = heading.GetComponent<VerticalLayoutGroup>();
                    if (layout != null) layout.enabled = false;
                    var fitter = heading.GetComponent<ContentSizeFitter>();
                    if (fitter != null) fitter.enabled = false;
                    SetAnchors074(heading, new Vector2(.07f, .83f), new Vector2(.93f, .985f));
                    var label = heading.Find("Expedition route row heading text 089")?.GetComponent<Text>();
                    var status = heading.Find("Expedition shuffle and deal status 089")?.GetComponent<Text>();
                    var moment = heading.Find("Expedition route row instruction 089")?.GetComponent<Text>();
                    if (label != null) { SetAnchors074(label.rectTransform, new Vector2(.025f,.65f), new Vector2(.62f,.98f)); ConfigureAuthoredCompactText076(label,19,29); }
                    if (status != null) { SetAnchors074(status.rectTransform, new Vector2(.62f,.65f), new Vector2(.975f,.98f)); ConfigureAuthoredCompactText076(status,15,21); }
                    if (moment != null)
                    {
                        // The next room is still concealed. Only already-known
                        // objective/progress belongs above the face-down cards.
                        moment.text = "NOW  •  " + WorldGateCurrentTask084(state, node) +
                            "\nChoose one card. The other two return unseen to the deck.";
                        SetAnchors074(moment.rectTransform, new Vector2(.025f,.08f), new Vector2(.975f,.62f));
                        ConfigureAuthoredCompactText076(moment,20,25);
                    }
                }
                if (row != null)
                {
                    SetAnchors074(row, new Vector2(.11f,.02f), new Vector2(.89f,.815f));
                    row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(6,6,5,5);
                    row.GetComponent<HorizontalLayoutGroup>().spacing = 20f;
                    foreach (var slot in row.Cast<Transform>())
                    {
                        var sizing = slot.GetComponent<LayoutElement>();
                        if (sizing != null) { sizing.minHeight = 0f; sizing.preferredHeight = -1f; sizing.flexibleHeight = 1f; }
                    }
                    if (Application.isPlaying && isActiveAndEnabled)
                        StartCoroutine(FitCampaignDraftPresentation131(row, _campaignQuestRoot131));
                }
            }
            else
            {
                BuildWorldGatePrimaryAction084(body, coordinator, state, node);
                // Preserve the actual locked encounter, including its authored
                // enemy art, preview, action and running reveal controller.
                var locked = body.Find("Locked Expedition Encounter Decision Card 089") as RectTransform;
                if (locked != null)
                {
                    FitCampaignLockedEncounter131(locked);
                    return true;
                }
                var actions = body.GetComponentsInChildren<Button>(true);
                var stale = body.Cast<Transform>().ToArray();
                var scene = BuildCampaignQuestScene131(body,
                    state.ActiveStatus == "ReadyToFinalize" ? "THE JOURNEY CONTINUES" : node.Title,
                    (!string.IsNullOrWhiteSpace(node.StoryFlavor) ? node.StoryFlavor : node.Description) +
                    (string.IsNullOrWhiteSpace(node.RewardPreview) ? "" : "\n\n" + node.RewardPreview),
                    !string.IsNullOrWhiteSpace(node.IconResource) ? node.IconResource : BoardRoomIllustrationResource091(node.RoomKind));
                var reading = CampaignQuestActionsParent131(scene);
                foreach (var action in actions)
                {
                    action.transform.SetParent(reading, false);
                    if (action.name == "Return from completed quest board 084") action.GetComponentInChildren<Text>().text = "CONTINUE THE STORY";
                }
                // A primary action may itself have been a direct child. The
                // exact moved control remains live inside the new scene.
                foreach (var old in stale)
                    if (old.parent == body) { old.gameObject.SetActive(false); Destroy(old.gameObject); }

                FitCampaignQuestActions131(scene);
            }
            var help = RuntimeUi.AddButton(_campaignQuestRoot131, "Campaign deck help 131", "HELP",
                () => { _showExpeditionDeckDetails089 = true; BuildCurrentScreen(); }, 65f, RuntimeUi.ButtonNormal);
            SetAnchors074(help.GetComponent<RectTransform>(), new Vector2(.017f,.80f), new Vector2(.097f,.86f));
            if (LoopStripVisible164 && (Screen.width < 1000 || Screen.height < 570))
                SetAnchors074(help.GetComponent<RectTransform>(), new Vector2(.025f,.84f), new Vector2(.145f,.985f));
            ConfigureAuthoredCompactText076(help.GetComponentInChildren<Text>(), 18, 24);
            // Opening081 finalizes only after all cards exist. Do the same here:
            // Motion089 captures its home in the first LateUpdate, before Unity's
            // deferred canvas layout would otherwise separate these three slots.
            FinalizeExpeditionViewport076(_campaignQuestRoot131);
            return true;
        }

        IEnumerator FitCampaignDraftPresentation131(RectTransform row, RectTransform owner)
        {
            var choice = row.GetComponent<Campaign023.ExpeditionCardChoice091>();
            var faces = row.Cast<Transform>().Select(slot => slot.Cast<Transform>()
                    .FirstOrDefault(child => child.name.StartsWith("Expedition route card surface ", StringComparison.Ordinal))
                    as RectTransform)
                .Where(face => face != null).ToArray();
            var measuredWidths = faces.Select(face => -1f).ToArray();
            var previousScale164 = -1f;
            bool? previousExpanded = null;
            while (isActiveAndEnabled && owner != null && owner.gameObject.activeInHierarchy &&
                   row != null && row.gameObject.activeInHierarchy && choice != null)
            {
                var phase = choice.Phase091;
                var expanded = phase == Campaign023.ExpeditionCardChoice091.ChoicePhase091.Revealing ||
                    phase == Campaign023.ExpeditionCardChoice091.ChoicePhase091.AwaitingAction ||
                    phase == Campaign023.ExpeditionCardChoice091.ChoicePhase091.Submitted;
                var changed = previousExpanded != expanded;
                var scale164 = Mathf.Max(.01f,_canvas.scaleFactor);
                var scaleChanged164 = !Mathf.Approximately(previousScale164,scale164);
                if (changed)
                {
                    // Change only the illustrated face. Existing091 still owns
                    // the physical flip, the centered slot, Seen and hidden cards.
                    // During the edge-on turn this is set before its face appears.
                    foreach (var face in faces)
                        if (face != null) SetAnchors074(face,
                            new Vector2(expanded ? -1f : -.30f, 0f),
                            new Vector2(expanded ? 2f : 1.30f, 1f));
                    previousExpanded = expanded;
                }
                for (var index = 0; index < faces.Length; index++)
                {
                    var face = faces[index];
                    if (face == null || !face.gameObject.activeInHierarchy) continue;
                    var content = face.Find("Board Card Reading Column 091/Expedition Route Card Copy Viewport 110/Expedition Route Card Copy 110") as RectTransform;
                    var width = content == null ? 0f : content.rect.width;
                    if (width <= 1f || (!changed && !scaleChanged164 && Mathf.Abs(measuredWidths[index] - width) < .5f)) continue;
                    FitCampaignDraftText131(face);
                    measuredWidths[index] = width;
                }
                previousScale164 = scale164;
                // Stable frames only inspect presentation dimensions/phase.
                // No state reads, commands, canvas rebuilds or save work occur.
                yield return null;
            }
        }

        static void FitCampaignDraftText131(RectTransform face)
        {
            var reading = face?.Find("Board Card Reading Column 091") as RectTransform;
            var content = reading?.Find("Expedition Route Card Copy Viewport 110/Expedition Route Card Copy 110") as RectTransform;
            if (content == null || !face.gameObject.activeInHierarchy) return;
            // The face/reading layouts are disabled after091 arranges them.
            // Rebuild the active content layout itself first so each Text has
            // its real column width before preferredHeight is measured.
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var phone164 = Screen.width < 1000 || Screen.height < 570;
            var scale164 = Mathf.Max(.01f,face.GetComponentInParent<Canvas>()?.scaleFactor??1f);
            foreach (var text in content.Cast<Transform>().Select(child => child.GetComponent<Text>())
                .Where(text => text != null && text.gameObject.activeSelf))
            {
                if(phone164)
                {
                    text.resizeTextForBestFit=false;
                    text.fontSize=Mathf.CeilToInt((text.name.StartsWith("Expedition card title",StringComparison.Ordinal)?14f:12f)/scale164);
                    text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
                }
                // Text.preferredHeight measures this exact font/copy at the
                // actual reading width. The existing scroll retains long copy;
                // text rows never overlap odds/rewards or squeeze the actions.
                var height = Mathf.Max(36f, Mathf.Ceil(text.preferredHeight) + 4f);
                RuntimeUi.SetLayout(text, preferredHeight: height).minHeight = height;
            }
            if(phone164)foreach(var button in reading.GetComponentsInChildren<Button>())
            {
                // Reflow the retained native actions at the expanded card width.
                // Their concealed-face layout otherwise leaves narrow labels.
                var size=button.GetComponent<LayoutElement>();
                if(size!=null){size.minWidth=reading.rect.width;size.preferredWidth=reading.rect.width;}
                var label=button.GetComponentInChildren<Text>();if(label==null)continue;
                label.resizeTextForBestFit=false;label.fontSize=Mathf.CeilToInt(12f/scale164);
                label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            }
            if(phone164&&reading.Find("Expedition Route Card Actions 110") is RectTransform actions164)
                LayoutRebuilder.ForceRebuildLayoutImmediate(actions164);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }
}
