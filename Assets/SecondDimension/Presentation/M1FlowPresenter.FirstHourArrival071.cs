using System;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private WalkableSkyhomeArrival071 _walkableSkyhomeArrival071;
        private bool _skyhomeArrivalComplete071;

        private void EnterDirectGuildCharter091()
        {
            // The charter remains the player's explicit campaign-creation action.
            // Arrival is now a direct screen transition: no movement or proximity
            // interaction is needed to reach the Guild's working menus.
            _skyhomeArrivalComplete071 = true;
            Navigate(M1Screen.FirstHourOpening);
        }

        private bool TryEnterWalkableSkyhomeArrival071()
        {
            if (_skyhomeArrivalComplete071 || !(_coordinator is M1RuntimeCoordinator)) return false;
            if (_walkableSkyhomeArrival071 != null && _walkableSkyhomeArrival071.IsActive071)
            {
                SuspendStudioAmbienceForWorld076();
                if (_canvas != null) _canvas.gameObject.SetActive(false);
                return true;
            }

            SuspendStudioAmbienceForWorld076();
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            try
            {
                _walkableSkyhomeArrival071 = gameObject.AddComponent<WalkableSkyhomeArrival071>();
                _walkableSkyhomeArrival071.Begin071(
                    CompleteWalkableSkyhomeArrival071,
                    () => Navigate(M1Screen.MainMenu));
                return true;
            }
            catch (Exception exception)
            {
                var failed071 = _walkableSkyhomeArrival071;
                _walkableSkyhomeArrival071 = null;
                if (failed071 != null)
                {
                    failed071.Shutdown071();
                    Destroy(failed071);
                }
                if (_canvas != null) _canvas.gameObject.SetActive(true);
                _localStatus =
                    "Skyhome Market could not open, so the emergency charter is available here.";
                _localStatusPositive = false;
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void CompleteWalkableSkyhomeArrival071()
        {
            _skyhomeArrivalComplete071 = true;
            CloseWalkableSkyhomeArrival071();
            Navigate(M1Screen.FirstHourOpening);
        }

        private void CloseWalkableSkyhomeArrival071()
        {
            var arrival071 = _walkableSkyhomeArrival071;
            _walkableSkyhomeArrival071 = null;
            if (arrival071 != null)
            {
                arrival071.Shutdown071();
                Destroy(arrival071);
            }
            if (_canvas != null) _canvas.gameObject.SetActive(true);
        }
    }
}
