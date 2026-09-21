using System;
using System.Collections.Generic;

namespace SecondDimension.Determinism
{
    public sealed class Pcg32
    {
        public const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private readonly ulong _increment;

        public Pcg32(ulong seed, ulong stream)
        {
            _state = 0UL;
            _increment = unchecked((stream << 1) | 1UL);
            NextUInt32();
            _state = unchecked(_state + seed);
            NextUInt32();
        }

        public static Pcg32 FromParts(params object[] parts)
        {
            var pair = SemanticSeed.Derive(parts);
            return new Pcg32(pair.Seed, pair.Stream);
        }

        public uint NextUInt32()
        {
            var oldState = _state;
            _state = unchecked(oldState * Multiplier + _increment);
            var xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            var rotation = (int)(oldState >> 59);
            return RotateRight(xorshifted, rotation);
        }

        public uint NextBounded(uint bound)
        {
            if (bound == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "Bound must be in 1..2^32-1.");
            }

            var threshold = unchecked(0U - bound) % bound;
            while (true)
            {
                var value = NextUInt32();
                if (value >= threshold)
                {
                    return value % bound;
                }
            }
        }

        public ulong NextBounded(ulong bound)
        {
            if (bound == 0 || bound > (1UL << 32))
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "Bound must be in 1..2^32.");
            }

            return bound == (1UL << 32) ? NextUInt32() : NextBounded((uint)bound);
        }

        public int NextInclusive(int low, int high)
        {
            if (high < low)
            {
                throw new ArgumentOutOfRangeException(nameof(high));
            }

            var range = checked((uint)((long)high - low + 1L));
            return checked(low + (int)NextBounded(range));
        }

        public T Choose<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0)
            {
                throw new ArgumentException("Cannot choose from an empty collection.", nameof(values));
            }

            return values[(int)NextBounded((uint)values.Count)];
        }

        public T WeightedChoice<T>(IReadOnlyList<WeightedValue<T>> values)
        {
            if (values == null || values.Count == 0)
            {
                throw new ArgumentException("At least one weighted value is required.", nameof(values));
            }

            ulong total = 0;
            foreach (var value in values)
            {
                if (value.Weight == 0)
                {
                    throw new ArgumentException("Weights must be positive.", nameof(values));
                }

                total = checked(total + value.Weight);
            }

            if (total > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "M0 weighted tables must fit in uint32.");
            }

            var roll = NextBounded((uint)total);
            ulong cursor = 0;
            foreach (var value in values)
            {
                cursor += value.Weight;
                if (roll < cursor)
                {
                    return value.Value;
                }
            }

            throw new InvalidOperationException("Weighted selection exhausted unexpectedly.");
        }

        private static uint RotateRight(uint value, int rotation)
        {
            rotation &= 31;
            return (value >> rotation) | (value << ((-rotation) & 31));
        }
    }

    public readonly struct WeightedValue<T>
    {
        public WeightedValue(T value, uint weight)
        {
            Value = value;
            Weight = weight;
        }

        public T Value { get; }
        public uint Weight { get; }
    }
}
