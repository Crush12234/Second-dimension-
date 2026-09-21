using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
namespace SecondDimension.Presentation
{
    public sealed class CatchUpHeroView153
    {public string Id,Name,CourseId;public int Level,Target,Credits;public bool Eligible,Finished;}
    public sealed class CatchUpView153
    {public string Status;public bool Initialized,Available;public int Target,Slots;public CatchUpHeroView153[] Heroes;}
    public sealed partial class M1RuntimeCoordinator
    {
        public Result<HeroLevelQuote152> QuoteHeroCatchUp153(string hero)=>TowerWriteBusy116()?Result<HeroLevelQuote152>.Failure(TowerBusy116):HeroPaidLevels152.QuoteCatchUp153(_campaign,hero);
        public CatchUpView153 ReadCatchUp153()
        {
            var training=_campaign?.CatchUp153;
            if(_campaign?.Guild==null)return new CatchUpView153{Status="Open your Guild first.",Heroes=Array.Empty<CatchUpHeroView153>()};
            var benchmark=CatchUpTraining153.Benchmark(_campaign);
            var safety=TownService153.Safety(_campaign);bool grounded=_campaign.Guild.Development.Facilities.Any(f=>f.FacilityId=="FACILITY_TRAINING_HALL"&&f.Level>=1);
            var heroes=_campaign.Guild.Recruits.OrderBy(r=>r.Progression.Level).ThenBy(r=>r.DisplayName,StringComparer.Ordinal).Select(r=>
            {
                var course=training?.Courses.FirstOrDefault(c=>c.HeroId==r.RecruitId);
                return new CatchUpHeroView153{Id=r.RecruitId,Name=r.DisplayName,Level=r.Progression.Level,CourseId=course?.CourseId,
                    Target=course?.TargetLevel??(benchmark.IsSuccess?benchmark.Value:0),Credits=course?.Credits??0,
                    Finished=course!=null&&r.Progression.TotalPersonalXp>=course.TargetXp,
                    Eligible=course==null&&benchmark.IsSuccess&&grounded&&safety==null&&r.Progression.Level<benchmark.Value&&ProtectedActorPolicy.CanUseNormalEquipment(r)};
            }).ToArray();
            return new CatchUpView153{Initialized=training!=null,Available=benchmark.IsSuccess&&safety==null&&grounded,
                Status=safety??(!grounded?"Build Training Grounds in Town to open free Catch Up.":training==null?"Open free training to reconcile already-earned hero growth and prepare your Guild's training records.":benchmark.IsSuccess?"Training is free. Heroes keep their Union positions and remain available for battle.":string.Join(" ",benchmark.Errors)),
                Target=benchmark.IsSuccess?benchmark.Value:0,Slots=training?.Courses.Count??0,Heroes=heroes};
        }
        public M1CommandResult InitializeCatchUp153()
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            if(!_campaign.Guild.Development.Facilities.Any(f=>f.FacilityId=="FACILITY_TRAINING_HALL"&&f.Level>=1))return M1CommandResult.Failure("Build Training Grounds first.");
            // This release is the first native Catch Up service. Its absent optional
            // record is the explicit legacy migration boundary, never a reset of known history.
            return ApplyAndPersist(CatchUpTraining153.InitializeKnownPreService(_campaign),true,"Free training opened; earned hero development retained.");
        }
        public Result<CatchUpQuote153> QuoteCatchUp153(string[] ids,bool retiring)=>TowerWriteBusy116()?Result<CatchUpQuote153>.Failure(TowerBusy116):
            retiring?CatchUpTraining153.QuoteRetirement(_campaign,ids):CatchUpTraining153.QuoteEnrollment(_campaign,ids);
        public M1CommandResult ConfirmCatchUp153(CatchUpQuote153 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var result=CatchUpTraining153.Confirm(_campaign,quote);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("This training change is already saved.");
            return ApplyAndPersist(result,true,quote.Retiring?"Training slot released. All earned XP retained.":"Free training saved. Win battles to develop the enrolled heroes.");
        }
    }
}
