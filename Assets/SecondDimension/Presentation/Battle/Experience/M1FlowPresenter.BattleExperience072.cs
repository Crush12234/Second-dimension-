using System;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private M2BattleExperienceController072 _battleExperience072;
        private RectTransform _battleExperienceHost072;

        /// <summary>
        /// Fired only after the staged terminal result is shown, the player chooses
        /// Continue, and the deterministic reward claim succeeds.
        /// </summary>
        public event Action FirstHourBattleExperienceCompleted072;

        public bool IsFirstHourBattleExperienceActive072 =>
            _battleExperience072 != null && _battleExperience072.IsActive;

        /// <summary>
        /// Public bridge for the authored FirstHourExperienceRoot. The battle must
        /// already have been started by the coordinator; this method only presents it.
        /// </summary>
        public void EnterFirstHourBattleExperience072()
        {
            EnsureCanvas();
            if (!(_coordinator is IM2PresentationCoordinator m2))
                throw new InvalidOperationException(
                    "The first-hour battle experience requires an M2 presentation coordinator.");
            if (M2BattleViewAccess098.Read(_coordinator) == null)
                throw new InvalidOperationException(
                    "Start the authored first-hour encounter before entering its battle experience.");

            _screen = M1Screen.Battle;
            if (_battleExperienceHost072 == null)
            {
                RuntimeUi.ClearChildren(_screenRoot);
                _activePage = null;
                _activeContent = null;
                _activeScroll = null;
                _battleExperienceHost072 = RuntimeUi.AddStretchRect(
                    _screenRoot,
                    "First Hour Battle Experience Host 072");
            }
            if (_battleExperience072 == null)
            {
                _battleExperience072 = gameObject.AddComponent<M2BattleExperienceController072>();
                _battleExperience072.BattleCompleted += HandleBattleExperienceCompleted072;
            }
            _battleExperience072.Initialize(m2, _battleExperienceHost072);
            _battleExperience072.ContinueTowerAuto107 = HasActiveTowerRun081() &&
                !((_coordinator as M1RuntimeCoordinator)?.CurrentBattleIsTitan161 ?? false)
                ? (Func<M1CommandResult>)ContinueTowerAuto107 : null;

            _battleExperience072.ReducedMotion = _reducedMotion;
            _battleExperience072.PlaybackSpeedChanged091 = speed => _battleAnimationSpeed = speed;
            _battleExperience072.AnimationSpeed = Mathf.Clamp(
                _battleAnimationSpeed, 0.25f, M2BattleExperienceController072.MaximumPlaybackSpeed108);
            SetFirstHourBattleExperienceVisible072(true);
            // The replacement is the sole visual authority. Never layer the primitive
            // legacy perspective world behind the authored diorama.
            SetCinematic3DWorldVisible(false);
            if (!_battleExperience072.EnterCurrentBattle())
                throw new InvalidOperationException("The current coordinator battle could not be presented.");
            RefreshLoopNavigation164();
        }

        private bool RefreshFirstHourBattleExperience072()
        {
            if (!IsFirstHourBattleExperienceActive072) return false;
            // The initialized live controller already receives the same Changed
            // event. Let that one owner project and refresh this surface once.
            if (_battleExperience072.OwnsCoordinatorNotifications098(_coordinator)) return true;
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle != null) _battleExperience072.Refresh(battle);
            return true;
        }

        private void SetFirstHourBattleExperienceVisible072(bool visible)
        {
            if (_battleExperience072 != null) _battleExperience072.SetVisible(visible);
            if (!visible && _battleExperienceHost072 != null)
                _battleExperienceHost072.gameObject.SetActive(false);
            else if (visible && _battleExperienceHost072 != null)
                _battleExperienceHost072.gameObject.SetActive(true);
        }

        private void HandleBattleExperienceCompleted072()
        {
            var external = FirstHourBattleExperienceCompleted072;
            if (external != null)
            {
                external.Invoke();
                return;
            }

            // Compatibility fallback for the existing route. The new FirstHour root
            // subscribes to the public event and owns its next story beat instead.
            ReturnFromClaimedBattleExperience072();
        }

        private void ReturnFromClaimedBattleExperience072()
        {
            if ((_coordinator as M1RuntimeCoordinator)?.CurrentBattleIsTitan161 == true)
            {
                _guildCityTab017D = "TITANS";
                _guildCityMoreOpen060 = false;
                Navigate(M1Screen.GuildOperations);
                return;
            }
            if (HasActiveTowerRun081())
            {
                _guildCityTab017D = "ABYSS";
                _guildCityMoreOpen060 = false;
                Navigate(M1Screen.GuildOperations);
                return;
            }

            if (_coordinator is IGuildCityPresentationCoordinator017D guildCity &&
                guildCity.GuildCity017D?.Expedition != null)
            {
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "EXPEDITION";
                _guildCityMoreOpen060 = false;
                EnterExpeditionBoard074(guildCity);
                return;
            }

            if (_coordinator is IGuildCityPresentationCoordinator017D)
            {
                _guildCityTab017D = "HALL";
                Navigate(M1Screen.GuildOperations);
                return;
            }

            Navigate(M1Screen.Complete);
        }

        private M1CommandResult ContinueTowerAuto107()
        {
            var tower = _coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
            if (tower == null) return M1CommandResult.Failure("The Tower is unavailable.");
            var previousSuppression = _suppressBoardAdventureCoordinatorRefresh084;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                return PrepareNextTowerAutoBattle107(tower);
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = previousSuppression;
            }
        }

        private static M1CommandResult PrepareNextTowerAutoBattle107(
            Campaign022.ICampaignProgressionPresentationCoordinator022 tower)
        {
            // ClaimBattleRewards has already persisted the battle reward and its
            // return. Bank the remaining floor receipts before beginning a new one.
            var banked = AdvanceTowerBattleOnlyTransitions088(tower);
            if (banked == null || !banked.Succeeded)
                return banked ?? M1CommandResult.Failure("The Tower victory could not be saved.");
            if (!string.IsNullOrWhiteSpace(tower.CampaignProgression022?.ActiveAbyssOperationId))
                return M1CommandResult.Failure("The current Tower floor still needs attention. Auto is paused.");
            var begun = tower.BeginTowerRun081();
            if (begun == null || !begun.Succeeded)
                return begun ?? M1CommandResult.Failure("The next Tower floor could not be saved.");
            var prepared = AdvanceTowerBattleOnlyTransitions088(tower);
            if (prepared == null || !prepared.Succeeded)
                return prepared ?? M1CommandResult.Failure("The next Tower battle could not be prepared.");
            var state = tower.CampaignProgression022;
            if (state == null || !state.IsAvailable || !state.ActiveStepRequiresBattle ||
                !StringComparer.Ordinal.Equals(state.ActiveAbyssStatus, "Active"))
                return M1CommandResult.Failure("The next Tower floor is not ready for battle. Auto is paused.");
            return tower.EnterAbyssBattle022() ?? M1CommandResult.Failure("The next Tower battle could not begin.");
        }
    }
}
