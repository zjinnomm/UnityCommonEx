using System;
using System.Collections.Generic;

namespace UnityCommonEx
{
    public struct GameplayRng
    {
        private ulong _state;

        public ulong State
        {
            get => _state;
            set => _state = value == 0 ? 0x9E3779B97F4A7C15UL : value;
        }

        public void Init(int seed)
        {
            unchecked
            {
                ulong x = (uint)seed;
                x ^= x << 13;
                x ^= x >> 7;
                x ^= x << 17;
                _state = x == 0 ? 0x9E3779B97F4A7C15UL : x;
            }
        }

        public void Init(ulong persistedState) => State = persistedState;

        public ulong NextULong()
        {
            ulong z = (_state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            ulong span = (ulong)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextULong() % span);
        }

        public float Value01() => (NextULong() >> 11) * (1f / (1ul << 53));

        public float RangeInclusive(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return minInclusive + Value01() * (maxInclusive - minInclusive);
        }

        public float RangeFloatExclusive0(float totalExclusive)
        {
            if (totalExclusive <= 0f) return 0f;
            return Value01() * totalExclusive;
        }

        public int RandomIndex(IList<float> weights, bool normalized = true)
        {
            if (weights == null || weights.Count == 0) return -1;
            float sum;
            if (normalized)
            {
                sum = 0f;
                for (int i = 0; i < weights.Count; i++)
                    sum += weights[i];
            }
            else
                sum = 1f;

            float r = RangeFloatExclusive0(sum);
            for (int i = 0; i < weights.Count; i++)
            {
                r -= weights[i];
                if (r <= 0f)
                    return i;
            }
            return normalized ? weights.Count - 1 : -1;
        }
    }
}
