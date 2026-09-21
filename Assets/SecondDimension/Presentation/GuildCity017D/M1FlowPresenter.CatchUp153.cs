using System;
using System.Linq;
using UnityEngine;
namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        int _catchUpPage153;
        void DrawPaidCatchUp153(Transform body,M1RuntimeCoordinator authority,string hero)
        {
            RuntimeUi.AddButton(body,"Paid Catch Up153","CATCH UP NOW — REVIEW COST",()=>
            {
                var result=authority.QuoteHeroCatchUp153(hero);
                if(!result.IsSuccess){AddMessagePanel(body,"CATCH UP",string.Join(" ",result.Errors),RuntimeUi.Warning);return;}
                var q=result.Value;
                var text="Level "+q.Before.Level+" → "+q.After.Level+"\nPersonal XP added: "+q.PersonalXpAdded+
                    "\nCost: "+q.Cost+" Treasury XP · Available: "+q.Wallet+"\n"+LevelGrowthText152(q.Before,q.After)+
                    "\nThis uses the same target as new free training. It grants no training victories and keeps existing courses unchanged.";
                if(!q.Affordable){AddMessagePanel(body,"CATCH UP",text+"\nNeed "+(q.Cost-q.Wallet)+" more Treasury XP.",RuntimeUi.Warning);return;}
                ShowConfirmation("CATCH UP "+q.Name.ToUpperInvariant(),text,"CONFIRM — "+q.Cost+" TREASURY XP",()=>ApplyGuildCity017D(authority.ConfirmHeroLevels152(q)),confirmColor:RuntimeUi.Positive);
            },132f,RuntimeUi.ButtonNormal);
        }
        void BuildCatchUp153()
        {
            var authority=_coordinator as M1RuntimeCoordinator;
            var body=CreatePage("TRAINING GROUNDS","Help your heroes catch up — free",()=>{_guildCityTab017D="TOWN";BuildCurrentScreen();});
            if(authority==null)return;
            var view=authority.ReadCatchUp153();
            if(!string.IsNullOrWhiteSpace(_localStatus))AddStatus(body,_localStatus,_localStatusPositive);
            AddMessagePanel(body,"FREE CATCH UP",view.Status,view.Available?RuntimeUi.Positive:RuntimeUi.Warning);
            if(!view.Initialized)
            {
                RuntimeUi.AddButton(body,"Initialize free Catch Up 153","OPEN FREE TRAINING",()=>ShowConfirmation("OPEN FREE TRAINING",
                    "This applies any levels your heroes have already earned and prepares training records. No Treasury XP is spent and no free XP is granted.","OPEN TRAINING",
                    ()=>ApplyGuildCity017D(authority.InitializeCatchUp153())),90f,RuntimeUi.Positive);
                return;
            }
            AddMessagePanel(body,view.Target>0?"GUILD TRAINING TARGET · LEVEL "+view.Target:"TRAINING HISTORY",
                "Six slots · five earned victories · zero cost\nNew targets use the rounded-down average of your ten strongest eligible heroes' levels, excluding XP awarded by Catch Up. Each course keeps the target shown when you enroll.",RuntimeUi.Accent);
            foreach(var hero in view.Heroes.Where(h=>h.CourseId!=null))
            {
                var item=hero;var panel=AddMessagePanel(body,item.Name.ToUpperInvariant(),"Level "+item.Level+" · Target "+item.Target+" · "+item.Credits+" / 5 victories"+(item.Finished?" · COMPLETE":""),item.Finished?RuntimeUi.Positive:RuntimeUi.Text);
                RuntimeUi.AddButton(panel,"Retire course "+item.CourseId,item.Finished?"RELEASE SLOT":"STOP THIS COURSE",()=>ReviewCatchUp153(authority,new[]{item.CourseId},true,item.Name),80f,RuntimeUi.ButtonNormal);
                RuntimeUi.SetLayout(panel,preferredHeight:panel.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight+RuntimeUi.MinimumTouchPixels+4f);
            }
            var candidates=view.Heroes.Where(h=>h.CourseId==null).ToArray();
            _catchUpPage153=Math.Max(0,Math.Min(_catchUpPage153,Math.Max(0,(candidates.Length-1)/8)));
            var paging=AddRow(body,"Training pages153",12f,RuntimeUi.MinimumTouchPixels);
            RuntimeUi.AddButton(paging,"Training previous153","PREVIOUS",()=>{_catchUpPage153=Math.Max(0,_catchUpPage153-1);BuildCurrentScreen();},84f);
            RuntimeUi.AddButton(paging,"Training paid153","PAID LEVELS +1 / +5",()=>{_guildCityTab017D="DEVELOPMENT";BuildCurrentScreen();},84f);
            RuntimeUi.AddButton(paging,"Training next153","NEXT",()=>{_catchUpPage153++;BuildCurrentScreen();},84f);
            AddResponsiveText062(body,"Training availability153",view.Slots+" / 6 SLOTS IN USE · HEROES "+(_catchUpPage153*8+1)+"–"+Math.Min(candidates.Length,(_catchUpPage153+1)*8),20,28,50f,RuntimeUi.Accent);
            foreach(var hero in candidates.Skip(_catchUpPage153*8).Take(8))
            {
                var item=hero;var row=AddRow(body,"Training hero "+hero.Id,12f,RuntimeUi.MinimumTouchPixels);
                RuntimeUi.AddText(row,"Training name "+hero.Id,hero.Name+"\nLevel "+hero.Level,26,TextAnchor.MiddleLeft,RuntimeUi.Text);
                var button=RuntimeUi.AddButton(row,"Enroll "+hero.Id,view.Slots>=6?"ALL SLOTS FULL":hero.Eligible?"ENROLL FREE":"NOT ELIGIBLE",()=>ReviewCatchUp153(authority,new[]{item.Id},false,item.Name),85f,RuntimeUi.ButtonNormal);
                button.interactable=hero.Eligible&&view.Slots<6;
            }
        }
        void ReviewCatchUp153(M1RuntimeCoordinator authority,string[] ids,bool retiring,string name)
        {
            var read=authority.QuoteCatchUp153(ids,retiring);
            if(!read.IsSuccess){_localStatus=string.Join(" ",read.Errors);_localStatusPositive=false;BuildCurrentScreen();return;}
            var q=read.Value;
            ShowConfirmation(retiring?"RELEASE TRAINING SLOT":"ENROLL "+name.ToUpperInvariant(),retiring?
                "Stop this course and release its slot. All earned levels, XP and Union assignments remain.":
                "Frozen target: Level "+q.TargetLevel+"\nFive future battle victories complete the recorded XP gap. Ordinary and bought XP count toward the target. Your hero stays in their Union.\nCost: FREE",retiring?"RELEASE SLOT":"START FREE TRAINING",
                ()=>ApplyGuildCity017D(authority.ConfirmCatchUp153(q)),confirmColor:RuntimeUi.Positive);
        }
    }
}
