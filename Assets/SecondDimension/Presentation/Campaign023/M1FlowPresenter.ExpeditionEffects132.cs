using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SecondDimension.Gameplay.Campaign023;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        static void ConcealCommittedEffectCopy132(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var source = coordinator as IExpeditionEffectCoordinator132;
            if (source != null && state?.RouteCards != null)
                foreach(var card in state.RouteCards.Where(value=>source.MysteryEffectCardIds132.Contains(value.CardId)))
                {
                    card.Title = card.Category == "CHANCE" ? "Fortune Wheel" : card.Category == "HAZARD" || card.PermanentHeroEffectKind == "SCAR" ? "Rift Hex" : "Wayglass Blessing";
                    card.Description = "A mystery awaits. Choose this card, then roll to discover its effect.";
                    card.RewardPreview = "MYSTERY • REVEALED AFTER THE ROLL";
                    card.OutcomePreview = "ROLL TO DISCOVER YOUR FATE";
                    card.Odds = ""; card.RiskLabel = "MYSTERY";
                    card.PermanentHeroName = ""; card.PermanentHeroEffectKind = ""; card.PermanentHeroCheckModifier = 0;
                }
            var effect = source?.PendingExpeditionEffect132;
            if (effect == null || state == null || effect.PresentationReceiptId132 != state.PendingReceiptId) return;
            // Header projections must not reveal the already saved result while
            // the physical die is still tumbling. The face reveals it on settle.
            state.PendingOutcomeTitle = effect.Kind == ExpeditionDeckService089.CurseD20132 ? "MYSTERY CURSE" : "MYSTERY FATE";
            state.PendingReward = "REVEALED AFTER THE ROLL";
            state.PendingDice = "";
            state.PendingHasCheck = false;
        }

        bool TryBuildCommittedEffect132(Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var authority = coordinator as IExpeditionEffectCoordinator132;
            var effect = authority?.PendingExpeditionEffect132;
            if (effect == null || effect.PresentationReceiptId132 != state?.PendingReceiptId) return false;
            var wheel = effect.Kind == ExpeditionDeckService089.Wheel132;
            var curse = effect.Kind == ExpeditionDeckService089.CurseD20132;
            var accent = curse ? new Color(.93f,.32f,.57f) : RuntimeUi.Accent;
            var panel = RuntimeUi.AddPanel(body,"Committed Fate Table 132",new Color(.018f,.025f,.058f,.98f));
            var face = panel.rectTransform;
            if (IsFullScreenWorldGateEventBody110(body))
                CommittedGlassShatter132.Fit132(face,new Vector2(.08f,.035f),new Vector2(.92f,.96f));
            else RuntimeUi.SetLayout(panel,preferredHeight:680f);
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = accent; border.effectDistance = new Vector2(2f,-2f);
            var heading = RuntimeUi.AddText(face,"Fate Event Heading 132",effect.Title.ToUpperInvariant(),42,
                TextAnchor.MiddleCenter,accent,FontStyle.Bold);
            CommittedGlassShatter132.Fit132(heading.rectTransform,new Vector2(.05f,.84f),new Vector2(.95f,.98f));
            var visual = RuntimeUi.AddStretchRect(face,"Fate Event Visual 132");
            CommittedGlassShatter132.Fit132(visual,new Vector2(.045f,.10f),new Vector2(.515f,.84f));
            var status = RuntimeUi.AddText(face,"Fate Event Result 132",effect.Rolled ? "FATE IN MOTION…" :
                wheel ? "A SEALED FORTUNE" : curse ? "A MYSTERIOUS CURSE" : "A MYSTERIOUS BLESSING",
                38,TextAnchor.MiddleCenter,RuntimeUi.Text,FontStyle.Bold);
            CommittedGlassShatter132.Fit132(status.rectTransform,new Vector2(.53f,.60f),new Vector2(.96f,.82f));
            status.resizeTextForBestFit = true; status.resizeTextMinSize = 26; status.resizeTextMaxSize = 38;
            var copy = RuntimeUi.AddText(face,"Fate Event Reward 132",effect.Rolled ? "The result will appear when the motion settles." :
                wheel ? "Spin to discover what the wheel holds." : "Roll the D20 to discover its effect.",30,
                TextAnchor.MiddleCenter,RuntimeUi.Text);
            CommittedGlassShatter132.Fit132(copy.rectTransform,new Vector2(.53f,.26f),new Vector2(.96f,.60f));
            copy.resizeTextForBestFit = true; copy.resizeTextMinSize = 23; copy.resizeTextMaxSize = 30;
            var receiptId = effect.PresentationReceiptId132;
            var rollReceiptId = effect.ReceiptId;
            var action = RuntimeUi.AddButton(face,"Committed Fate Action 132",effect.Rolled ? "REVEALING…" : wheel ? "SPIN THE WHEEL" : "ROLL D20",
                () =>
                {
                    if (!effect.Rolled)
                        RunWorldGateAnimatedCommand084(()=>authority.RollCommittedExpeditionEffect132(rollReceiptId),
                            "The roll could not save. Try again; your reward is still sealed.");
                    else ApplyWorldGateReceiptNow084(coordinator,receiptId);
                },110f,accent);
            CommittedGlassShatter132.Fit132(action.GetComponent<RectTransform>(),new Vector2(.57f,.065f),new Vector2(.92f,.23f));
            ConfigureAuthoredCompactText076(action.GetComponentInChildren<Text>(),26,34);
            if(wheel)
            {
                CommittedGlassShatter132.Fit132(visual,new Vector2(.025f,.065f),new Vector2(.64f,.86f));
                CommittedGlassShatter132.Fit132(status.rectTransform,new Vector2(.65f,.59f),new Vector2(.97f,.82f));
                CommittedGlassShatter132.Fit132(copy.rectTransform,new Vector2(.65f,.265f),new Vector2(.97f,.59f));
                CommittedGlassShatter132.Fit132(action.GetComponent<RectTransform>(),new Vector2(.65f,.065f),new Vector2(.97f,.23f));
            }
            action.interactable = !effect.Rolled;
            var skip = _worldGateEventRoot110?.Find("Committed Expedition Event Skip 110")?.GetComponent<Button>();
            if (skip != null)
            {
                skip.interactable = effect.Rolled && !_reducedMotion && _skippedWorldGateEventReceipt110 != receiptId;
                if (!effect.Rolled) skip.GetComponentInChildren<Text>().text = "ROLL TO REVEAL";
            }
            if (!effect.Rolled)
            {
                var silhouette = new GameObject("Sealed Fate Silhouette 132",typeof(RectTransform),typeof(FatePolyhedronGraphic132));
                var rect = silhouette.GetComponent<RectTransform>(); rect.SetParent(visual,false);
                CommittedGlassShatter132.Fit132(rect,Vector2.zero,Vector2.one);
                var graphic = silhouette.GetComponent<FatePolyhedronGraphic132>(); graphic.Wheel132=wheel;
                graphic.color=accent; graphic.raycastTarget=false;
                if(wheel)
                {
                    FateWheelLabels132.Attach132(graphic);
                    var pointer=RuntimeUi.AddText(visual,"Fortune Wheel Pointer 132","▼",62,TextAnchor.MiddleCenter,RuntimeUi.Accent,FontStyle.Bold);
                    CommittedGlassShatter132.Fit132(pointer.rectTransform,new Vector2(.42f,.82f),new Vector2(.58f,1f));
                    pointer.raycastTarget=false;
                }
                var mystery = RuntimeUi.AddText(visual,"Sealed Fate Mark 132","?",96,TextAnchor.MiddleCenter,RuntimeUi.Text,FontStyle.Bold);
                CommittedGlassShatter132.Fit132(mystery.rectTransform,Vector2.zero,Vector2.one);
            }
            else
            {
                var animation = CommittedFateEffect132.Play132(visual,wheel,wheel?effect.WheelSector:effect.D20,accent,
                    _reducedMotion || _skippedWorldGateEventReceipt110 == receiptId,()=>
                    {
                        if (face == null || !face.gameObject.activeInHierarchy) return;
                        status.text=effect.Result; copy.text=effect.Reward;
                        status.color=curse?RuntimeUi.Warning:RuntimeUi.Positive;
                        action.GetComponentInChildren<Text>().text="COLLECT & CONTINUE";
                        action.interactable=true;
                        if (skip != null) skip.interactable=false;
                    });
                if (_worldGateEventRoot110 != null)
                    _worldGateEventRoot110.GetComponent<WorldGateEventLifetime110>()?.BindCancellation110(animation.Cancel132);
            }
            if (!_localStatusPositive && !string.IsNullOrWhiteSpace(_localStatus))
            {
                var failure=RuntimeUi.AddText(face,"Fate Save Failure 132",_localStatus,24,TextAnchor.MiddleCenter,RuntimeUi.Error,FontStyle.Bold);
                CommittedGlassShatter132.Fit132(failure.rectTransform,new Vector2(.05f,.005f),new Vector2(.95f,.06f));
            }
            return true;
        }
    }
}
