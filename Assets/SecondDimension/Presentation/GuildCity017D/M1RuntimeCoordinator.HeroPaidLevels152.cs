using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        public Result<HeroLevelQuote152> QuoteHeroLevels152(string id,int added)=>TowerWriteBusy116()?
            Result<HeroLevelQuote152>.Failure(TowerBusy116):HeroPaidLevels152.Quote(_campaign,id,added);
        public M1CommandResult ConfirmHeroLevels152(HeroLevelQuote152 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var result=HeroPaidLevels152.Confirm(_campaign,quote);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("This level improvement is already saved.");
            return ApplyAndPersist(result,true,"Hero levels and their permanent stat growth saved.");
        }
    }
}
