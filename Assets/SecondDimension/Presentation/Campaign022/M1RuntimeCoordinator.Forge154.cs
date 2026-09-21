using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed class ForgeQuote154
    {
        public string ProfileId { get; } public string Revision { get; }
        public string ItemId { get; } public string RecipeId { get; }
        public string Name { get; } public string TargetTier { get; }
        public string CostText { get; } public bool PlayerLocked { get; }
        public int BeforePhysical { get; } public int AfterPhysical { get; }
        public int BeforeMystic { get; } public int AfterMystic { get; }
        internal string CandidateHash { get; }
        internal ForgeQuote154(string profile, string revision, string item, string recipe,
            string name, string tier, string costs, bool locked, int bp, int ap, int bm, int am, string candidate)
        { ProfileId=profile;Revision=revision;ItemId=item;RecipeId=recipe;Name=name;TargetTier=tier;
          CostText=costs;PlayerLocked=locked;BeforePhysical=bp;AfterPhysical=ap;BeforeMystic=bm;AfterMystic=am;CandidateHash=candidate; }
    }
    public sealed class ForgeOffer154
    {
        public string ItemId,RecipeId,Name,Progress,Reason,CostText,ArtKey;
        public bool CanUpgrade;
        public int BeforePhysical,AfterPhysical,BeforeMystic,AfterMystic;
    }

    public sealed partial class M1RuntimeCoordinator
    {
        public Result<ForgeQuote154> QuoteForge154(string itemId,string recipeId)
        {
            if(TowerWriteBusy116())return Result<ForgeQuote154>.Failure(TowerBusy116);
            // The preview runs exactly the immutable command that confirmation will run.
            // It never saves or replaces the coordinator's campaign.
            var result=_campaignCommands022.EvolveWeapon(_campaign,Registry022(),itemId,recipeId);
            if(!result.IsSuccess)return Result<ForgeQuote154>.Failure(result.Errors.ToArray());
            if(ReferenceEquals(result.Value,_campaign))return Result<ForgeQuote154>.Failure("This upgrade is already saved.");
            var item=WeaponEvolutionCombat154.FindUnique(_campaign,itemId);
            var upgraded=WeaponEvolutionCombat154.FindUnique(result.Value,itemId);
            var recipe=Registry022().WeaponRecipes[recipeId];
            var before=M2EquipmentPowerPolicy087.Resolve(item);var after=M2EquipmentPowerPolicy087.Resolve(upgraded);
            var costText=ForgeCosts154(recipe);
            return Result<ForgeQuote154>.Success(new ForgeQuote154(_campaign.CampaignGuid,CanonicalJson.Sha256Hex(_campaign),
                itemId,recipeId,ForgeOwnedName154(item),recipe.toTier,costText,item.PlayerLocked,
                before.PhysicalAttack,after.PhysicalAttack,before.MysticAttack,after.MysticAttack,CanonicalJson.Sha256Hex(result.Value)));
        }
        public M1CommandResult ConfirmForge154(ForgeQuote154 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            if(quote==null||_campaign==null||quote.ProfileId!=_campaign.CampaignGuid)return M1CommandResult.Failure("Review this upgrade again.");
            var history=_campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022.EquipmentEvolution
                .FirstOrDefault(x=>x.ItemInstanceId==quote.ItemId);
            if(history!=null&&history.AppliedRecipeIds.Contains(quote.RecipeId))return M1CommandResult.Success("This upgrade is already saved.");
            if(quote.Revision!=CanonicalJson.Sha256Hex(_campaign))return M1CommandResult.Failure("Your Guild changed. Review the upgrade again.");
            var result=_campaignCommands022.EvolveWeapon(_campaign,Registry022(),quote.ItemId,quote.RecipeId);
            if(!result.IsSuccess)return ApplyAndPersist(result,true,"Weapon upgraded.");
            if(CanonicalJson.Sha256Hex(result.Value)!=quote.CandidateHash)return M1CommandResult.Failure("The upgrade changed. Review it again.");
            return ApplyAndPersist(result,true,"Weapon upgraded and saved. Its new power applies when the next battle begins.");
        }
        public IReadOnlyList<ForgeOffer154> ReadForge154()
        {
            if(_campaign?.Guild==null)return Array.Empty<ForgeOffer154>();
            var registry=Registry022();var history=_campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022.EquipmentEvolution;
            var owned=_campaign.Guild.Inventory.Concat(_campaign.Guild.Recruits.SelectMany(r=>r.Equipment.Assignments.Select(a=>a.Item)))
                .GroupBy(i=>i.InstanceId).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.Single(),StringComparer.Ordinal);
            var rows=new List<ForgeOffer154>();
            foreach(var entry in history.OrderBy(x=>x.ItemInstanceId,StringComparer.Ordinal))
            {
                if(!owned.TryGetValue(entry.ItemInstanceId,out var item))continue;
                var effectiveTier=WeaponEvolutionCombat154.EffectiveRecipeTier154(entry,item);
                if(effectiveTier==null)continue;
                var recipe=registry.WeaponRecipes.Values.Where(r=>r.trackId==entry.TrackId&&r.fromTier==effectiveTier)
                    .OrderBy(r=>r.recipeId,StringComparer.Ordinal).FirstOrDefault();
                if(recipe==null)continue;
                // The service rechecks this same earned-quality baseline before
                // spending; skip any malformed non-improving native recipe.
                if(WeaponEvolutionCombat154.TierIndex(item.QualityId)>=WeaponEvolutionCombat154.TierIndex(recipe.toTier))continue;
                // A list read never hashes the campaign or builds a binding quote.
                // This pure native projection checks the real gates and exact spend.
                var candidate=_campaignCommands022.EvolveWeapon(_campaign,registry,item.InstanceId,recipe.recipeId);
                var before=M2EquipmentPowerPolicy087.Resolve(item);
                var projected=WeaponEvolutionCombat154.Project(item,recipe.toTier);
                var after=projected.IsSuccess?M2EquipmentPowerPolicy087.Resolve(projected.Value):before;
                rows.Add(new ForgeOffer154{ItemId=item.InstanceId,RecipeId=recipe.recipeId,Name=ForgeOwnedName154(item),
                    ArtKey="EQUIPMENT:"+EquipmentVisualId090(item,EquipmentSlotIds.MainHand),
                    Progress="Uses "+entry.MeaningfulUses+" / "+recipe.requiredMeaningfulUses+" · Mastery "+entry.MasteryPoints+" / "+recipe.requiredMasteryPoints+" · Forge level "+recipe.forgeLevelRequired,
                    CanUpgrade=candidate.IsSuccess&&!ReferenceEquals(candidate.Value,_campaign),CostText=ForgeCosts154(recipe),
                    BeforePhysical=before.PhysicalAttack,AfterPhysical=after.PhysicalAttack,BeforeMystic=before.MysticAttack,AfterMystic=after.MysticAttack,
                    Reason=candidate.IsSuccess?"":ForgeReason154(string.Join("; ",candidate.Errors))});
            }
            return rows;
        }
        string ForgeCosts154(WeaponRecipeDto022 recipe)
        {
            var text=string.Join(" · ",WeaponEvolutionCombat154.MaterialCosts159(_campaign,recipe.materialCosts).Where(c=>c.amount>0)
                .Select(c=>c.amount+" "+(Registry020().Materials.TryGetValue(c.materialId,out var material)?material.displayName:c.materialId)));
            return string.IsNullOrWhiteSpace(text)?"No material cost":text;
        }
        string ForgeOwnedName154(EquipmentItemState item)
        {
            var owner=_campaign.Guild.Recruits.FirstOrDefault(r=>r.Equipment.Assignments.Any(a=>a.Item.InstanceId==item.InstanceId));
            return ForgeName154(item.DisplayName)+" · "+(owner==null?"Inventory":owner.DisplayName);
        }
        static string ForgeName154(string name)
        {
            if(name==null)return string.Empty;
            if(name.StartsWith("EQ_PROC_",StringComparison.Ordinal))name=name.Substring(8);
            else if(name.StartsWith("CA002_WPN_",StringComparison.Ordinal))
            {
                name=name.Substring(10);
                if(name.EndsWith("_TRAINING",StringComparison.Ordinal))name="TRAINING_"+name.Substring(0,name.Length-9);
            }
            else return name;
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Replace('_',' ').ToLowerInvariant());
        }
        static string ForgeReason154(string error)
        {
            switch(error)
            {
                case "CAMPAIGN022_FORGE_LEVEL_REQUIRED":return "Construct or upgrade the Forge in Town first.";
                case "CAMPAIGN022_MEANINGFUL_HISTORY_INSUFFICIENT":return "Use this weapon in real battles to earn its required mastery.";
                case "CAMPAIGN022_MATERIALS_INSUFFICIENT":return "Earn the recipe's materials from adventures before upgrading.";
                case "CAMPAIGN022_GODLY_GATE_REQUIRED":return "Continue your story and Tower progression to unlock this recipe.";
                default:return error;
            }
        }
    }
}
