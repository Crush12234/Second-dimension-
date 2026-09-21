using System;

namespace SecondDimension.Core
{
    [Serializable]
    public readonly struct FixedPoint : IEquatable<FixedPoint>, IComparable<FixedPoint>
    {
        public const long Scale = 10_000;

        public FixedPoint(long rawValue)
        {
            RawValue = rawValue;
        }

        public long RawValue { get; }

        public static FixedPoint FromBasisPoints(long basisPoints) => new FixedPoint(basisPoints);
        public static FixedPoint FromInteger(long value) => new FixedPoint(checked(value * Scale));

        public long MultiplyFloor(long value) => checked(value * RawValue) / Scale;
        public int CompareTo(FixedPoint other) => RawValue.CompareTo(other.RawValue);
        public bool Equals(FixedPoint other) => RawValue == other.RawValue;
        public override bool Equals(object obj) => obj is FixedPoint other && Equals(other);
        public override int GetHashCode() => RawValue.GetHashCode();
        public override string ToString() => $"{RawValue}/{Scale}";
    }
}

