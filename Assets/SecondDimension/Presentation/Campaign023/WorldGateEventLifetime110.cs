using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    // Owns only the pending presentation timer for this visible event instance.
    public sealed class WorldGateEventLifetime110 : MonoBehaviour
    {
        private Action _cancel;
        public void BindCancellation110(Action cancel)
        {
            Cancel110();
            _cancel = cancel;
        }
        public void Detach110() => _cancel = null;
        public void Cancel110()
        {
            var cancel = _cancel;
            _cancel = null;
            cancel?.Invoke();
        }
        private void OnDisable() => Cancel110();
        private void OnDestroy() => Cancel110();
    }
}
