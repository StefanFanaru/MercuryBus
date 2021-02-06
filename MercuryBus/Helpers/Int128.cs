using System;
using System.Globalization;

namespace MercuryBus.Helpers
{
    public sealed class Int128 : IComparable<Int128>, IComparable
    {
        public Int128(long hi, long lo)
        {
            Hi = hi;
            Lo = lo;
        }

        public long Hi { get; }

        public long Lo { get; }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return 0;
            }

            if (ReferenceEquals(null, obj))
            {
                return 1;
            }

            return obj is Int128 other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(Int128)}");
        }

        public int CompareTo(Int128 other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            int hiComparison = Hi.CompareTo(other.Hi);
            return hiComparison != 0 ? hiComparison : Lo.CompareTo(other.Lo);
        }

        public string AsString()
        {
            return $"{Hi:x16}-{Lo:x16}";
        }

        public override string ToString()
        {
            return "Int123{" + AsString() + '}';
        }

        private bool Equals(Int128 other)
        {
            return Hi == other.Hi && Lo == other.Lo;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Int128) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Hi.GetHashCode() * 397) ^ Lo.GetHashCode();
            }
        }

        public static Int128 FromString(string str)
        {
            string[] s = str.Split('-');
            if (s.Length != 2)
            {
                throw new ArgumentException($"Should have 2 parts: {str}");
            }

            return new Int128(long.Parse(s[0], NumberStyles.HexNumber), long.Parse(s[1], NumberStyles.HexNumber));
        }
    }
}
