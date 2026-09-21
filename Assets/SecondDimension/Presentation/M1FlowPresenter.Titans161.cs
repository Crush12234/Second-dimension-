using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private int _titanSlot161=1,_titanTier161=1;
        private bool _titanDetail161;
        private int _titanPage164;
        private Text TitanLabel161(Transform parent,string name,string value,Rect bounds,int pixels,
            Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var label=TownLabel153(parent,name,value,bounds,pixels,color,bold,alignment);
            FitTitanText161(label,pixels);return label;
        }
        private Button TitanButton161(Transform parent,string name,string title,Rect bounds,Action action,
            bool enabled=true,bool selected=false,int pixels=15)
        {
            var button=TownButton153(parent,name,title,bounds,action,enabled,selected,pixels);
            FitTitanText161(button.GetComponentInChildren<Text>(),pixels);return button;
        }
        private void FitTitanText161(Text text,int pixels)
        {
            text.resizeTextForBestFit=true;text.resizeTextMaxSize=text.fontSize;
            text.resizeTextMinSize=TownFont153(Mathf.Min(pixels,9));
        }
        private void BuildTitans161()
        {
            var owner=_coordinator as M1RuntimeCoordinator;if(owner==null)return;
            var view=owner.ReadTitans161(_titanTier161);
            var compact=Screen.width<1000||Screen.height<570||_textScale>=1.25f;
            RuntimeUi.ClearChildren(_screenRoot);RuntimeUi.EnsureEventSystem();_activeContent=null;_activeScroll=null;
            var root=RuntimeUi.AddPanel(_screenRoot,"Titan trials161",TownInk153);Stretch(root.rectTransform);_activePage=root.rectTransform;
            root.raycastTarget=false;
            if(compact){BuildCompactTitans164(root.transform,owner,view);return;}
            TitanLabel161(root.transform,"Titan title161","TITAN TRIALS",new Rect(.035f,.900f,.58f,.085f),compact?22:29,TownGold153,true);
            TitanLabel161(root.transform,"Titan gate161","GUILD HALL  "+view.HallLevel+" / 10",new Rect(.63f,.927f,.335f,.045f),12,
                view.HallLevel>=10?RuntimeUi.Positive:RuntimeUi.MutedText,true,TextAnchor.MiddleRight);
            TitanLabel161(root.transform,"Titan subtitle161",view.Status,new Rect(.035f,.87f,.93f,.045f),compact?11:14,RuntimeUi.MutedText);
            if(!view.Available)
            {TownScrollText153(root.transform,"Titan unavailable161",view.Status,new Rect(.05f,.25f,.9f,.55f),17);}
            else
            {
                var selected=view.Trials.First(t=>t.Slot==_titanSlot161);
                if(!compact||!_titanDetail161)
                {
                    foreach(var row in view.Trials)
                    {
                        var c=compact?2:3;var width=compact?.45f:.185f;var h=compact?.111f:.16f;
                        var rect=new Rect(.035f+((row.Slot-1)%c)*(width+.018f),.722f-((row.Slot-1)/c)*(h+.01f),width,h);
                        var button=TitanButton161(root.transform,"Titan select "+row.Slot+"161","",rect,()=>
                            {_titanSlot161=row.Slot;_titanDetail161=true;_localStatus="";BuildCurrentScreen();},true,row.Slot==_titanSlot161);
                        TownImage153(button.transform,"Titan face "+row.Slot+"161",TitanArt161.BossPortrait(row.Id),new Rect(.02f,.08f,.29f,.84f));
                        TitanLabel161(button.transform,"Titan name "+row.Slot+"161",row.Slot.ToString("00")+"  "+row.Name,
                            new Rect(.34f,.43f,.63f,.50f),compact?11:12,RuntimeUi.Text,true);
                        TitanLabel161(button.transform,"Titan record "+row.Slot+"161",row.HighestTier>0?"CLEARED TIER "+row.HighestTier:"UNCLEARED",
                            new Rect(.34f,.08f,.63f,.30f),10,row.HighestTier>0?RuntimeUi.Positive:RuntimeUi.MutedText);
                    }
                }
                if(!compact||_titanDetail161)
                {
                    var panel=TownPanel153(root.transform,"Titan detail161",compact?new Rect(.035f,.165f,.93f,.68f):new Rect(.655f,.145f,.31f,.70f),new Color(.018f,.03f,.05f,.98f));
                    TownImage153(panel.transform,"Titan selected portrait161",TitanArt161.BossPortrait(selected.Id),new Rect(.04f,.73f,.26f,.23f));
                    TitanLabel161(panel.transform,"Titan selected name161",selected.Name,new Rect(.34f,.78f,.62f,.17f),compact?19:21,TownGold153,true);
                    TitanLabel161(panel.transform,"Titan tier161","TIER "+_titanTier161+" · "+selected.Wins+" WINS",new Rect(.34f,.72f,.62f,.06f),12,RuntimeUi.MutedText,true);
                    TitanButton161(panel.transform,"Titan previous tier161","−",new Rect(.04f,.63f,.12f,.075f),()=>{_titanTier161--;BuildCurrentScreen();},_titanTier161>1);
                    TitanLabel161(panel.transform,"Titan stats161",selected.Hp+" HP · "+selected.Attack+" ATK · "+selected.Magic+" MAG",new Rect(.18f,.63f,.64f,.075f),12,RuntimeUi.Text,false,TextAnchor.MiddleCenter);
                    TitanButton161(panel.transform,"Titan next tier161","+",new Rect(.84f,.63f,.12f,.075f),()=>{_titanTier161++;BuildCurrentScreen();},_titanTier161<int.MaxValue);
                    TownImage153(panel.transform,"Titan reward portrait161",TitanArt161.HeroPortrait(selected.HeroId),new Rect(.04f,.43f,.2f,.18f));
                    TownScrollText153(panel.transform,"Titan reward161",selected.Reward,new Rect(.28f,.43f,.68f,.18f),14);
                    TownScrollText153(panel.transform,"Titan pattern161",selected.Pattern,new Rect(.04f,.20f,.92f,.21f),13);
                    TitanLabel161(panel.transform,"Titan reason161",selected.Reason,new Rect(.04f,.125f,.92f,.07f),11,selected.CanStart?RuntimeUi.Positive:RuntimeUi.Warning);
                    TitanButton161(panel.transform,"Titan begin161",view.HasSavedBattle?"CONTINUE SAVED BATTLE":"REVIEW ENCOUNTER",new Rect(.04f,.025f,.92f,.095f),()=>
                        {if(view.HasSavedBattle)Navigate(M1Screen.Battle);else ReviewTitan161(owner,selected);},view.HasSavedBattle||selected.CanStart,true,15);
                }
            }
            TitanButton161(root.transform,"Titan back161",compact&&_titanDetail161?"←  ALL TITANS":"←  TOWN",new Rect(.035f,.027f,.3f,.073f),()=>
                {if(compact&&_titanDetail161){_titanDetail161=false;BuildCurrentScreen();}else OpenTownService153("TOWN");},true,false,14).Select();
            TitanLabel161(root.transform,"Titan status161",_localStatus??"",new Rect(.37f,.025f,.595f,.085f),11,
                _localStatusPositive?RuntimeUi.Positive:RuntimeUi.Warning,false,TextAnchor.MiddleRight);
        }
        private void ReviewTitan161(M1RuntimeCoordinator owner,TitanTrialView161 selected)
        {
            var quote=owner.QuoteTitan161(selected.Slot,selected.Tier);
            if(!quote.IsSuccess){_localStatus=string.Join("\n",quote.Errors);_localStatusPositive=false;BuildCurrentScreen();return;}
            ShowTownConfirmation154(selected.Name+" · TIER "+selected.Tier,selected.Reward,
                "FREE ATTEMPT\nYour current battle formation will enter. Campaign cards and Tower progress remain saved.","ENTER BATTLE",()=>
                {
                    if(!ReferenceEquals(owner,_coordinator))return;
                    var result=owner.BeginTitan161(quote.Value);_localStatus=result.Message;_localStatusPositive=result.Succeeded;
                    if(result.Succeeded)Navigate(M1Screen.Battle);else BuildCurrentScreen();
                });
        }
    }
}

