using System;

namespace UnityCommonEx
{
    public struct StatPointKey : IEquatable<StatPointKey>
    {
        public static readonly StatPointKey Empty = new StatPointKey();

        public byte DimensionCount;
        public StatDimensionValue Dim0;
        public StatDimensionValue Dim1;

        public StatPointKey(in StatDimensionValue dim0)
        {
            DimensionCount = 1;
            Dim0 = dim0;
            Dim1 = default;
        }

        public StatPointKey(in StatDimensionValue dim0, in StatDimensionValue dim1)
        {
            DimensionCount = 2;
            Dim0 = dim0;
            Dim1 = dim1;
        }

        public bool Equals(StatPointKey other)
        {
            if (DimensionCount != other.DimensionCount)
                return false;
            if (DimensionCount >= 1 && !Dim0.Equals(other.Dim0))
                return false;
            if (DimensionCount >= 2 && !Dim1.Equals(other.Dim1))
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is StatPointKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = DimensionCount;
                if (DimensionCount >= 1)
                    hash = (hash * 31) + Dim0.GetHashCode();
                if (DimensionCount >= 2)
                    hash = (hash * 31) + Dim1.GetHashCode();
                return hash;
            }
        }
    }
}
