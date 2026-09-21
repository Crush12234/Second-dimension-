using System;
using System.Linq;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void AddInvocationForecastControls022(Transform parent,M2BattleView battle,Vector2 anchorMin,Vector2 anchorMax,int fontSize)
        {
            if(parent==null||battle==null)return;
            var coordinator=_coordinator as ICampaignProgressionPresentationCoordinator022;
            var panel=AddAnchoredPanel(parent,"Invocation Forecast Controls 022",new Color(0.055f,0.025f,0.085f,0.97f),anchorMin,anchorMax,Vector2.zero,Vector2.zero);
            M1PremiumUi.StylePanel(panel,M1PremiumUi.Surface.Warning);
            if(coordinator==null)
            {
                AddAnchoredText(panel.transform,"Invocation Adapter Unavailable","INVOCATION ADAPTER UNAVAILABLE",Math.Max(14,fontSize-4),UnityEngine.TextAnchor.MiddleCenter,RuntimeUi.MutedText,UnityEngine.FontStyle.Bold,new Vector2(0.02f,0.05f),new Vector2(0.98f,0.95f));
                return;
            }
            var selected=battle.Forecasts.Where(x=>x.IsSelected).ToArray();
            var progression=coordinator.CampaignProgression022;
            var covenants=(progression?.CovenantStates??Array.Empty<CovenantView022>()).Where(x=>StringComparer.Ordinal.Equals(x.Status,"Accepted")&&x.AcceptanceReceiptApplied&&selected.Any(f=>CovenantRoleMatches022(x.Role,f))).OrderBy(x=>x.DisplayName,StringComparer.Ordinal).ToArray();
            var controlCount=1+Math.Max(1,covenants.Length);
            var echo=AddAnchoredButton(panel.transform,"Invoke Echo Through Complete Forecast","ECHO · SELECTED FORECAST",()=>BeginInvocationForecast022(coordinator.InvokeEligibleEchoForecast022),TowerRunRules081.LightActionSurfaceColor083,new Vector2(0.025f,1f-1f/controlCount+0.025f),new Vector2(0.975f,0.975f));
            SetButtonFont(echo,fontSize);ApplyLightSurfaceForecastContrast083(echo);echo.interactable=battle.CanConfirmRound&&!_battleResolving;
            if(covenants.Length==0)
            {
                var locked=AddAnchoredButton(panel.transform,"Invoke Great Covenant Through Complete Forecast","GREAT COVENANT · MATCH FORECAST",()=>{},RuntimeUi.ButtonNormal,new Vector2(0.025f,0.025f),new Vector2(0.975f,1f-1f/controlCount-0.025f));
                SetButtonFont(locked,Math.Max(13,fontSize-3));locked.interactable=false;return;
            }
            for(var i=0;i<covenants.Length;i++)
            {
                var covenant=covenants[i];var top=1f-(float)(i+1)/controlCount;var bottom=1f-(float)(i+2)/controlCount;
                var button=AddAnchoredButton(panel.transform,"Invoke Great Covenant "+covenant.CovenantId,"GREAT COVENANT · "+covenant.DisplayName.ToUpperInvariant(),()=>BeginInvocationForecast022(()=>coordinator.InvokeAcceptedCovenantForecast022(covenant.CovenantId)),RuntimeUi.Warning,new Vector2(0.025f,bottom+0.025f),new Vector2(0.975f,top-0.025f));
                SetButtonFont(button,Math.Max(13,fontSize-3));ApplyLightSurfaceForecastContrast083(button);button.interactable=battle.CanConfirmRound&&!_battleResolving;
            }
        }

        private static void ApplyLightSurfaceForecastContrast083(Button button)
        {
            var label=button==null?null:button.transform.Find("Label")?.GetComponent<Text>();
            if(label==null)return;
            var colors=button.colors;
            colors.disabledColor=TowerRunRules081.LightActionDisabledSurfaceColor083;
            button.colors=colors;
            label.color=TowerRunRules081.LightSurfaceInkColor083;
            var shadow=label.GetComponent<Shadow>();
            if(shadow!=null)shadow.enabled=false;
        }

        private void BeginInvocationForecast022(Func<M1CommandResult> invoke)
        {
            if(invoke==null||_battleResolving)return;
            if(_battleAudio!=null)_battleAudio.PlayCue("SFX_COMMAND_CONFIRM");
            _battleResolving=true;_battleAnimationPaused=false;_skipBattleAnimation=false;_skipCurrentBattleBeat=false;_battlePresentationBefore=SnapshotBattlePresentation(_coordinator.State.Battle);
            var result=invoke();_localStatus=result.Message;_localStatusPositive=result.Succeeded;
            if(!result.Succeeded){_battleResolving=false;BuildCurrentScreen();return;}
            _battlePresentationAfter=SnapshotBattlePresentation(_coordinator.State.Battle);var newlyVisible=_battlePresentationAfter?.LastResolvedRoundEvents??Array.Empty<M2BattleEventView>();BuildCurrentScreen();StartCoroutine(PlayResolvedEventStaging(newlyVisible));
        }

        private static bool CovenantRoleMatches022(string role,M2ForecastView forecast)
        {
            if(forecast==null)return false;var actions=forecast.MemberActions??Array.Empty<M2PredictedActionView>();
            switch((role??string.Empty).ToUpperInvariant())
            {
                case "COMBAT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_ALL_OUT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_BALANCED");
                case "MYSTIC":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_MYSTIC")||actions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.ActionKind,"Mystic")||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Mystic"));
                case "RESTORATION":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_HEAL")||actions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.ActionKind,"Restoration")||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Restoration"));
                case "WARDING":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||actions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Warding"));
                case "GUARD":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||actions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.ActionKind,"Guard")||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Guard"));
                case "SUPPORT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_SUPPORT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_AP_RECOVERY")||actions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.ActionKind,"Recovery")||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Support"));
                case "TACTICAL":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_FLANK")||actions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.ActionKind,"Tactical")||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Tactical"));
                default:return false;
            }
        }
    }
}
