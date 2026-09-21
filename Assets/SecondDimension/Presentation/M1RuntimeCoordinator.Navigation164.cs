using System.Linq;
using SecondDimension.Gameplay.Navigation164;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.M1;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        private static bool HasCanonicalTitanWorldGateMirror164(CampaignState campaign,
            SecondDimension.Gameplay.TitanTrials160.TitanTrialCatalog160 catalog)
        {
            if (campaign?.Guild?.GuildCity == null) return false;
            // Older Titan starts retained the waiting Campaign expedition while
            // replacing only its encounter. Recognize both exact owners for the
            // read model; an arbitrary BOARD023 ID must still fail strict lookup.
            var city = SecondDimension.Gameplay.Campaign020.CampaignRecoveryCommands150.WorldIdentityCity(campaign);
            var operation = city?.Strategic017H?.Campaign019?.Playable020?.WorldGate023?.ActiveOperation;
            var state = campaign?.TitanTrials160;
            var active = state?.Active;
            var battle = campaign?.Battle;
            var request = city?.PendingEncounter;
            if (catalog == null || active == null || state.ProfileId != campaign.CampaignGuid ||
                active.ProfileId != campaign.CampaignGuid || active.CatalogIdentity != catalog.Identity ||
                battle?.BattleId != active.BattleId || battle.TitanRuntime161?.Attempt.AttemptId != active.AttemptId ||
                battle.TitanRuntime161.Attempt.RecipeIdentity != active.RecipeIdentity || request == null ||
                city.PendingBattleReturn != null ||
                !SecondDimension.Gameplay.Campaign023.CampaignWorldGateCommandService023
                    .HasCanonicalLegacyExpeditionMirror104(city, operation)) return false;
            var trial = catalog.Trial(active.Slot);
            return active.RecipeIdentity == trial.RecipeIdentity && request.RequestId == active.AttemptId &&
                request.BattleId == active.BattleId && request.ContractId == trial.Id &&
                request.ExpeditionId == active.AttemptId && request.BoardId == "TITAN_BOARD161" &&
                request.NodeId == trial.Id && request.EncounterId == trial.Id &&
                request.CanonicalSeedIdentity == active.RecipeIdentity && request.ReturnCheckpointId == "TITAN_RETURN161" &&
                request.AlliedUnionIds.SequenceEqual(active.AlliedUnionIds.OrderBy(x => x, System.StringComparer.Ordinal)) &&
                campaign.Guild.Development.HasAdventureAuthority(
                    SecondDimension.Gameplay.GuildCity017D.GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request));
        }

        private M2BattleMemberRewardView BuildMemberRewardView164(BattleRewardState reward, BattleMemberRewardState value)
        {
            var view = new M2BattleMemberRewardView
            {
                MemberId = value.MemberId, DisplayName = value.DisplayName, PersonalXp = value.PersonalXp,
                PreviousLevel = value.PreviousLevel, ProjectedLevel = value.ProjectedLevel,
                LevelsGained = value.LevelsGained, MaximumHpGain = value.MaximumHpGain,
                MaximumMpGain = value.MaximumMpGain, StrengthGain = value.StrengthGain,
                DefenseGain = value.DefenseGain, AgilityGain = value.AgilityGain,
                MagicGain = value.MagicGain, WillGain = value.WillGain
            };
            var battle = _campaign?.Battle;
            var loops = _campaign?.Loops164;
            var saved = loops?.Find(loops.ActiveLoop);
            // Presentation only: a frozen battle must not advertise a stale hero
            // level after shared progression advanced in another activity.
            if (battle == null || loops == null || loops.ProfileId != _campaign.CampaignGuid || saved?.Members == null ||
                saved.Battle?.BattleId != battle.BattleId || saved.LoopId != LoopCheckpoint164.BattleOwner(_campaign) ||
                saved.Battle.InitialBattleStateHash != battle.InitialBattleStateHash ||
                saved.Battle.InitialIntegrityStateHash090 != battle.InitialIntegrityStateHash090 ||
                saved.Battle.Reward != null && saved.Battle.Reward.RewardId != reward.RewardId)
                return view;
            var current = FindRecruit(value.MemberId)?.Progression;
            var member = battle.PlayerUnions.SelectMany(x => x.Members).FirstOrDefault(x => x.MemberId == value.MemberId);
            if (current == null || member == null || !saved.Members.Any(x => x.MemberId == value.MemberId)) return view;
            // Once claimed, the old projection cannot prove which later levels
            // came from this result. Show current level and the exact awarded XP.
            var projected = reward.Claimed || value.PersonalXp <= 0 ? current : current.GainPersonalXp(value.PersonalXp, member.ClassId);
            view.PreviousLevel = current.Level;
            view.ProjectedLevel = projected.Level;
            view.LevelsGained = projected.Level - current.Level;
            view.MaximumHpGain = projected.MaximumHpBonus - current.MaximumHpBonus;
            view.MaximumMpGain = projected.MaximumMpBonus - current.MaximumMpBonus;
            view.StrengthGain = projected.StrengthBonus - current.StrengthBonus;
            view.DefenseGain = projected.DefenseBonus - current.DefenseBonus;
            view.AgilityGain = projected.AgilityBonus - current.AgilityBonus;
            view.MagicGain = projected.MagicBonus - current.MagicBonus;
            view.WillGain = projected.WillBonus - current.WillBonus;
            return view;
        }

        public bool CanNavigateLoops164 => _campaign?.Guild!=null&&_campaign.OpeningFlow!=null&&
            (_campaign.OpeningFlow.UnionBuilderCompleted||_campaign.OpeningFlow.Stage==OpeningStage.Complete);
        public string CurrentLoop164 => LoopCheckpoint164.Current(_campaign);
        public M1CommandResult SwitchLoop164(string destination)
        {
            // The UI stops automatic orders before calling. A worker already in
            // its atomic transaction keeps ownership until its save completes.
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            var switched=LoopCheckpoint164.Switch(_campaign,destination);
            if(switched.IsSuccess&&ReferenceEquals(switched.Value,_campaign))return M1CommandResult.Success("Activity checkpoint retained.");
            return ApplyAndPersist(switched,true,"Activity checkpoint saved. Continue where you left off.");
        }
        public string LoopResumeTab164(string destination) => destination==LoopCheckpoint164.Campaign&&
            _campaign?.Guild?.GuildCity?.Strategic017H?.ActiveDefense!=null?"DEFENSE":null;
        public M1Screen LoopResumeScreen164(string destination)
        {
            if(destination!=CurrentLoop164||destination==LoopCheckpoint164.Town)return M1Screen.GuildOperations;
            var screen=ResumeScreen(_campaign);
            return screen==M1Screen.Battle||screen==M1Screen.BattleResults?screen:M1Screen.GuildOperations;
        }
    }
}
