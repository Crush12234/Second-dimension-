using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        ForgeQuote154 _forgeQuote154;
        // Existing PROGRESSION / OPEN WORKBENCH route calls this; no new screen shell.
        void BuildForge154(Transform body)
        {
            var authority=_coordinator as M1RuntimeCoordinator;
            if(authority==null){AddMessagePanel(body,"FORGE","Return to your Guild and reopen the Forge.",RuntimeUi.Warning);return;}
            if(!string.IsNullOrWhiteSpace(_localStatus))AddStatus(body,_localStatus,_localStatusPositive);
            AddMessagePanel(body,"FORGE WORKBENCH","Upgrade a weapon with mastery and materials earned on adventures. The same weapon stays in its current slot or Inventory.",RuntimeUi.Accent);
            if(_forgeQuote154!=null)
            {
                var q=_forgeQuote154;
                var panel=AddMessagePanel(body,"REVIEW "+q.Name.ToUpperInvariant(),
                    "Quality: "+q.TargetTier+"\nWeapon physical attack: "+q.BeforePhysical+" → "+q.AfterPhysical+
                    "\nWeapon mystic attack: "+q.BeforeMystic+" → "+q.AfterMystic+"\nCost: "+q.CostText+
                    (q.PlayerLocked?"\nThis weapon is locked. Confirming upgrades this same locked weapon; its lock stays on.":""),RuntimeUi.Accent);
                RuntimeUi.AddButton(panel,"Confirm weapon upgrade 154","UPGRADE AND SAVE",()=>
                {
                    if(!ReferenceEquals(authority,_coordinator)||!ReferenceEquals(_forgeQuote154,q))return;
                    // Consume this review before dispatch. A failed or uncertain save requires a fresh quote.
                    _forgeQuote154=null;
                    ApplyGuildCity017D(authority.ConfirmForge154(q));
                },104f,RuntimeUi.Positive);
                RuntimeUi.AddButton(panel,"Cancel weapon upgrade 154","CANCEL",()=>
                {
                    if(!ReferenceEquals(authority,_coordinator)||!ReferenceEquals(_forgeQuote154,q))return;
                    _forgeQuote154=null;BuildCurrentScreen();
                },82f,RuntimeUi.ButtonNormal);
                UseContentDrivenBoardPanelHeight084(panel);
                return;
            }
            var offers=authority.ReadForge154();
            if(offers.Count==0)AddMessagePanel(body,"NO UPGRADE READY","Use your weapons in battles to earn mastery. Gear already above its recipe quality is kept as it is.",RuntimeUi.Accent);
            foreach(var offer in offers)
            {
                var current=offer;
                var text=current.Progress+"\nPhysical attack "+current.BeforePhysical+" → "+current.AfterPhysical+
                    " · Mystic attack "+current.BeforeMystic+" → "+current.AfterMystic+"\nCost: "+current.CostText+
                    (current.CanUpgrade?"":"\n"+current.Reason);
                var row=AddMessagePanel(body,current.Name,text,RuntimeUi.Accent);
                var sprite=ResolveTownArt153(current.ArtKey);
                if(sprite!=null)
                {
                    // Reserve the right column for this owned weapon's native art.
                    // The ignored image remains anchored when the row relayouts.
                    var vertical=row.GetComponent<VerticalLayoutGroup>();
                    if(vertical!=null)vertical.padding.right=206;
                    var art=TownImage153(row,"Forge weapon "+current.ItemId+" 154",sprite,new Rect(1f,1f,0f,0f));
                    var ignored=art.GetComponent<LayoutElement>()??art.gameObject.AddComponent<LayoutElement>();
                    ignored.ignoreLayout=true;
                    art.rectTransform.anchorMin=art.rectTransform.anchorMax=new Vector2(1f,1f);
                    art.rectTransform.pivot=new Vector2(1f,1f);
                    art.rectTransform.anchoredPosition=new Vector2(-34f,-24f);
                    art.rectTransform.sizeDelta=new Vector2(148f,148f);
                }
                if(current.CanUpgrade)RuntimeUi.AddButton(row,"Review forge "+current.ItemId+" 154","REVIEW UPGRADE",()=>
                {
                    if(!ReferenceEquals(authority,_coordinator))return;
                    var fresh=authority.QuoteForge154(current.ItemId,current.RecipeId);
                    if(fresh.IsSuccess){_forgeQuote154=fresh.Value;_localStatus=string.Empty;}
                    else{_forgeQuote154=null;_localStatus=string.Join("; ",fresh.Errors);_localStatusPositive=false;}
                    BuildCurrentScreen();
                },90f,RuntimeUi.Accent);
                UseContentDrivenBoardPanelHeight084(row);
            }
        }
    }
}
