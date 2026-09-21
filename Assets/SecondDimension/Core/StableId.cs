using System;

namespace SecondDimension.Core
{
    [Serializable]
    public readonly struct StableId : IEquatable<StableId>, IComparable<StableId>
    {
        public string Value { get; }

        public StableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable ID cannot be empty.", nameof(value));
            }

            Value = value.Trim();
        }

        public int CompareTo(StableId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public bool Equals(StableId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is StableId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value;

        public static bool operator ==(StableId left, StableId right) => left.Equals(right);
        public static bool operator !=(StableId left, StableId right) => !left.Equals(right);
    }
}

