using System;
using System.Diagnostics;
using System.Globalization;
using SecondDimension.Presentation.Boot;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        // Diagnostic only: the existing soak must have completed its isolated
        // save preparation, and this coordinator must own that exact copy.
        // Timing never reads a presentation DTO or serializes campaign state.
        TowerTiming110 BeginTowerTiming110(string scope)
        {
            if (!TowerTimingEnabled110()) return null;
            try { return new TowerTiming110(scope, _campaign?.Battle?.BattleId ?? "none"); }
            catch { return null; }
        }

        bool TowerTimingEnabled110()
        {
            if (UnityEngine.Application.isEditor) return false;
            var prepared = TowerAutoPlayerSoak110.Prepared110;
            return prepared != null && string.Equals(_savePath, prepared.SavePath,
                StringComparison.OrdinalIgnoreCase);
        }

        TowerTiming110 BeginTowerObserverTiming110(Action observer)
        {
            if (!TowerTimingEnabled110()) return null;
            return BeginTowerTiming110("observer:" + observer.Method.DeclaringType?.FullName +
                "." + observer.Method.Name);
        }

        sealed class TowerTiming110 : IDisposable
        {
            static int _sequence;
            readonly int _id;
            readonly string _scope, _battle;
            readonly long _start;
            long _last;

            internal TowerTiming110(string scope, string battle)
            {
                _id = ++_sequence;
                _scope = scope;
                _battle = battle;
                _start = _last = Stopwatch.GetTimestamp();
            }

            internal void Mark(string phase)
            {
                var now = Stopwatch.GetTimestamp();
                Write(phase, now, (now - _last) * 1000d / Stopwatch.Frequency);
                _last = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                var now = Stopwatch.GetTimestamp();
                Write("total", now, (now - _start) * 1000d / Stopwatch.Frequency);
            }

            void Write(string phase, long now, double milliseconds)
            {
                // Diagnostics cannot turn a valid authority/save action into a
                // failure, including when a third-party log observer throws.
                try
                {
                    UnityEngine.Debug.Log("TOWER_TRANSACTION_TIMING110|utc=" +
                        DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) +
                        "|id=" + _id + "|scope=" + _scope + "|phase=" + phase +
                        "|phase_ms=" + milliseconds.ToString("F3", CultureInfo.InvariantCulture) +
                        "|elapsed_ms=" + ((now - _start) * 1000d / Stopwatch.Frequency)
                            .ToString("F3", CultureInfo.InvariantCulture) + "|battle=" + _battle);
                }
                catch { }
            }
        }
    }
}