using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace UnityCommonEx
{

    public static class RandomUtil
    {
        
        public static int RandomIndex(IList<float> weights, bool normalized = true)
        {
            float sum;
            if (normalized)
            {
                sum = 0;
                foreach (var i in weights)
                {
                    sum += i;
                }
            }
            else
            {
                sum = 1;
            }
            sum = Random.Range(0, sum);
            for (int i = 0; i < weights.Count; i++)
            {
                sum -= weights[i];
                if (sum <= 0)
                {
                    return i;
                }
            }
            return normalized ? weights.Count - 1 : -1;
        }

        public static T GetRandomElementNullable<T>(IDictionary<T, float> map) where T : class
        {
            using (ScopedPoolable<PoolableList<T>> Keys = new ScopedPoolable<PoolableList<T>>())
            using (ScopedPoolable<PoolableList<float>> Weights = new ScopedPoolable<PoolableList<float>>())
            {
                var keys = Keys.Get();
                var weights = Weights.Get();
                foreach (var kvp in map)
                {
                    keys.Add(kvp.Key);
                    weights.Add(kvp.Value);
                }
                int index = RandomIndex(weights);
                return index >= 0 ? keys[index] : null;
            }
        }

        public static T GetRandomElement<T>(IDictionary<T, float> map)
        {
            using (ScopedPoolable<PoolableList<T>> Keys = new ScopedPoolable<PoolableList<T>>())
            using (ScopedPoolable<PoolableList<float>> Weights = new ScopedPoolable<PoolableList<float>>())
            {
                var keys = Keys.Get();
                var weights = Weights.Get();
                foreach (var kvp in map)
                {
                    keys.Add(kvp.Key);
                    weights.Add(kvp.Value);
                }
                return keys[RandomIndex(weights)];
            }
        }

        public static T GetRandomElement<T>(IList<T> list, IList<float> weights = null)
        {
            int index = weights == null ? Random.Range(0, list.Count) : RandomIndex(weights);
            return list[index];
        }

        public static T PopRandomElement<T>(IList<T> list, IList<float> weights = null)
        {
            int index = weights == null ? Random.Range(0, list.Count) : RandomIndex(weights);
            T t = list[index];
            list.RemoveAt(index);
            return t;
        }

        public static T GetRandomElement<T>(IList<T> list, System.Func<T, float> weightSelector)
        {
            if (list == null || list.Count == 0)
            {
                return default(T);
            }

            using (ScopedPoolable<PoolableList<float>> weights = new ScopedPoolable<PoolableList<float>>())
            {
                var weightList = weights.Get();
                foreach (var item in list)
                {
                    weightList.Add(weightSelector(item));
                }
                return GetRandomElement(list, weightList);
            }
        }

    }

}