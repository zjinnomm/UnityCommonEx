using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public class InteractionMask : IPoolable
    {

        public uint Id;
        public Rect MaskedRegion;
        public Dictionary<int, Rect> ExcludeRegions;

        public void OnPoolableReturned()
        {
            Id = 0;
            MaskedRegion = Rect.zero;
            if (ExcludeRegions != null)
            {
                ExcludeRegions.Clear();
            }
        }

        public bool Masked(Vector2 point)
        {
            if (!MaskedRegion.Contains(point))
            {
                return false;
            }
            if (ExcludeRegions != null)
            {
                foreach (var pair in ExcludeRegions)
                {
                    if (pair.Value.Contains(point))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void AddExcludeRegion(int key, Rect rect)
        {
            if (ExcludeRegions == null)
            {
                ExcludeRegions = new Dictionary<int, Rect>();
            }
            ExcludeRegions.TryAdd(key, rect);
        }

        public void RemoveExcludeRegion(int key)
        {
            if (ExcludeRegions != null)
            {
                ExcludeRegions.Remove(key);
            }
        }

    }

}