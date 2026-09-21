using System;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        // Keep every phone action comfortably tappable in physical screen space.
        private void BuildCompactTitans164(Transform root,M1RuntimeCoordinator owner,TitanBoardView161 view)
        {
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)root;
            var height=Mathf.Max(220f,rect.rect.height*(_canvas==null?1f:_canvas.scaleFactor));
            var width=Mathf.Max(400f,rect.rect.width*(_canvas==null?1f:_canvas.scaleFactor));
            const float touch=48f,gap=6f;
            Func<float,float,float,float,Rect> box=(x,y,w,h)=>new Rect(x/width,y/height,w/width,h/height);
            float margin=width*.035f,inner=width-margin*2f;
            TitanLabel161(root,"Titan title161","TITAN TRIALS",box(margin,height-33f,inner*.60f,29f),22,TownGold153,true);
            TitanLabel161(root,"Titan gate161","GUILD HALL  "+view.HallLevel+" / 10",
                box(width-margin-inner*.4f,height-31f,inner*.4f,25f),12,
                view.HallLevel>=10?RuntimeUi.Positive:RuntimeUi.MutedText,true,TextAnchor.MiddleRight);
            TitanLabel161(root,"Titan subtitle161",view.Status,box(margin,height-58f,inner,24f),11,RuntimeUi.MutedText);
            if(!view.Available)
            {
                TownScrollText153(root,"Titan unavailable161",view.Status,box(margin,touch+gap,inner,height-touch-120f),16);
                TitanButton161(root,"Titan back161","←  TOWN",box(margin,4f,inner*.32f,touch),()=>OpenTownService153("TOWN"));
                return;
            }
            if(!_titanDetail161)
            {
                const int perPage=4;
                var pages=Math.Max(1,(view.Trials.Length+perPage-1)/perPage);
                _titanPage164=Mathf.Clamp(_titanPage164,0,pages-1);
                var rows=view.Trials.Skip(_titanPage164*perPage).Take(perPage).ToArray();
                float tileWidth=(inner-gap)/2f,tileHeight=(height-64f-touch-gap*2f)/2f;
                for(int i=0;i<rows.Length;i++)
                {
                    var row=rows[i];var y=height-64f-(i/2+1)*tileHeight-(i/2)*gap;
                    var button=TitanButton161(root,"Titan select "+row.Slot+"161","",
                        box(margin+(i%2)*(tileWidth+gap),y,tileWidth,tileHeight),()=>
                        {_titanSlot161=row.Slot;_titanDetail161=true;_localStatus="";BuildCurrentScreen();},true,row.Slot==_titanSlot161);
                    TownImage153(button.transform,"Titan face "+row.Slot+"161",TitanArt161.BossPortrait(row.Id),new Rect(.025f,.05f,.30f,.90f));
                    TitanLabel161(button.transform,"Titan name "+row.Slot+"161",row.Slot.ToString("00")+"  "+row.Name,
                        new Rect(.35f,.42f,.62f,.53f),14,RuntimeUi.Text,true);
                    TitanLabel161(button.transform,"Titan record "+row.Slot+"161",row.HighestTier>0?"CLEARED TIER "+row.HighestTier:"UNCLEARED",
                        new Rect(.35f,.06f,.62f,.31f),11,row.HighestTier>0?RuntimeUi.Positive:RuntimeUi.MutedText);
                }
                var footerWidth=(inner-gap*3f)/4f;
                TitanButton161(root,"Titan back161","←  TOWN",box(margin,4f,footerWidth,touch),()=>OpenTownService153("TOWN"));
                TitanButton161(root,"Titan previous page164","←  PREVIOUS",box(margin+footerWidth+gap,4f,footerWidth,touch),()=>
                    {_titanPage164--;BuildCurrentScreen();},_titanPage164>0,false,13);
                TitanLabel161(root,"Titan page164",(_titanPage164+1)+" / "+pages,
                    box(margin+(footerWidth+gap)*2f,4f,footerWidth,touch),14,RuntimeUi.MutedText,true,TextAnchor.MiddleCenter);
                TitanButton161(root,"Titan next page164","NEXT  →",box(margin+(footerWidth+gap)*3f,4f,footerWidth,touch),()=>
                    {_titanPage164++;BuildCurrentScreen();},_titanPage164+1<pages,false,13);
                return;
            }
            var selected=view.Trials.First(t=>t.Slot==_titanSlot161);
            float leftWidth=inner*.29f,rightX=margin+leftWidth+12f,rightWidth=inner-leftWidth-12f;
            TownPanel153(root,"Titan detail164",box(margin,58f,inner,height-122f),new Color(.018f,.03f,.05f,.98f));
            TownImage153(root,"Titan selected portrait161",TitanArt161.BossPortrait(selected.Id),
                box(margin+4f,114f,leftWidth-8f,Mathf.Max(32f,height-180f)));
            TitanButton161(root,"Titan previous tier161","−",box(margin,60f,touch,touch),()=>
                {_titanTier161--;BuildCurrentScreen();},_titanTier161>1);
            TitanLabel161(root,"Titan tier161","TIER "+_titanTier161,
                box(margin+touch+3f,60f,Mathf.Max(24f,leftWidth-touch*2f-6f),touch),12,TownGold153,true,TextAnchor.MiddleCenter);
            TitanButton161(root,"Titan next tier161","+",box(margin+leftWidth-touch,60f,touch,touch),()=>
                {_titanTier161++;BuildCurrentScreen();},_titanTier161<int.MaxValue);
            TownImage153(root,"Titan reward portrait161",TitanArt161.HeroPortrait(selected.HeroId),
                box(rightX,height-119f,48f,53f));
            TitanLabel161(root,"Titan selected name161",selected.Name,
                box(rightX+56f,height-96f,rightWidth-60f,31f),18,TownGold153,true);
            TitanLabel161(root,"Titan stats161",selected.Hp+" HP · "+selected.Attack+" ATK · "+selected.Magic+" MAG",
                box(rightX+56f,height-119f,rightWidth-60f,23f),11,RuntimeUi.MutedText);
            TownScrollText153(root,"Titan reward and pattern164",selected.Reward+"\n\nBATTLE PATTERN\n"+selected.Pattern,
                box(rightX,84f,rightWidth,Mathf.Max(24f,height-207f)),13);
            TitanLabel161(root,"Titan reason161",selected.Reason,box(rightX,59f,rightWidth,23f),11,
                selected.CanStart?RuntimeUi.Positive:RuntimeUi.Warning);
            TitanButton161(root,"Titan back161","←  ALL TITANS",box(margin,4f,leftWidth,touch),()=>
                {_titanDetail161=false;BuildCurrentScreen();},true,false,13);
            TitanButton161(root,"Titan begin161",view.HasSavedBattle?"CONTINUE SAVED BATTLE":"REVIEW ENCOUNTER",
                box(rightX,4f,rightWidth,touch),()=>
                {if(view.HasSavedBattle)Navigate(M1Screen.Battle);else ReviewTitan161(owner,selected);},
                view.HasSavedBattle||selected.CanStart,true,15);
        }
    }
}
