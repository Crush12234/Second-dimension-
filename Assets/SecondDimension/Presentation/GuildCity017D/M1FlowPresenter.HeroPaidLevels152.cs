using UnityEngine;
namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        void DrawPaidHeroLevels152(Transform body,M1RuntimeCoordinator authority,string hero)
        {
            if(authority==null)return;
            DrawPaidCatchUp153(body,authority,hero);
            var row=AddRow(body,"Hero level choices 152",14f,132f);
            foreach(var value in new[]{1,5,0})
            {
                var added=value;
                RuntimeUi.AddButton(row,"Review hero levels "+added+" 152",added==0?"APPLY BANKED LEVELS":"LEVEL UP +"+added,
                    ()=>{
                        if(row==null||!row.gameObject.activeInHierarchy||!ReferenceEquals(authority,_coordinator))return;
                        var result=authority.QuoteHeroLevels152(hero,added);
                        if(!result.IsSuccess){AddMessagePanel(body,"LEVEL TRAINING",string.Join("\n",result.Errors),RuntimeUi.Warning);return;}
                        var q=result.Value;var before=q.Before;var after=q.After;
                        var summary=
                            "Level "+before.Level+" → "+after.Level+"\nPersonal XP added: "+q.PersonalXpAdded+
                            "\nCost: "+q.Cost+" Treasury XP · Available: "+q.Wallet+
                            "\nPermanent growth: "+LevelGrowthText152(before,after)+
                            (q.Affordable?"\nTreasury after: "+(q.Wallet-q.Cost):"\nNeed "+(q.Cost-q.Wallet)+" more Treasury XP");
                        if(!q.Affordable){AddMessagePanel(body,"LEVEL TRAINING",summary,RuntimeUi.Warning);return;}
                        ShowConfirmation("IMPROVE "+q.Name.ToUpperInvariant(),summary,added==0?"APPLY EARNED LEVELS":"CONFIRM — "+q.Cost+" TREASURY XP",
                            ()=>{
                                if(!ReferenceEquals(authority,_coordinator))return;
                                ApplyGuildCity017D(authority.ConfirmHeroLevels152(q));
                            },confirmColor:RuntimeUi.Positive);
                    },85f,RuntimeUi.ButtonNormal);
            }
        }
        static string LevelGrowthText152(Gameplay.State.RecruitProgressionState before,Gameplay.State.RecruitProgressionState after)
        {
            var values=new System.Collections.Generic.List<string>();
            System.Action<string,int> add=(name,value)=>{if(value!=0)values.Add(name+" +"+value);};
            add("HP",after.MaximumHpBonus-before.MaximumHpBonus);add("MP",after.MaximumMpBonus-before.MaximumMpBonus);
            add("STR",after.StrengthBonus-before.StrengthBonus);add("DEF",after.DefenseBonus-before.DefenseBonus);
            add("AGI",after.AgilityBonus-before.AgilityBonus);add("MAG",after.MagicBonus-before.MagicBonus);add("WILL",after.WillBonus-before.WillBonus);
            return string.Join(", ",values);
        }
    }
}
