using System.Collections.Generic;

namespace UnityCommonEx
{
    public static class TickingManager
    {

        static readonly HashSet<ITickable> tickables = new();
        static readonly HashSet<ITickable> toAdd = new();
        static readonly HashSet<ITickable> toRemove = new();
        static readonly List<ITickable> scratch = new();

        /// <summary>
        /// Whether a Tick pass is currently in progress.
        /// </summary>
        static bool isTicking = false;

        /// <summary>
        /// Resets the ticking manager and force-stops all tracked tickables.
        /// </summary>
        public static void Init()
        {
            scratch.Clear();
            scratch.AddRange(tickables);
            foreach (var tickable in toAdd)
            {
                if (!scratch.Contains(tickable))
                {
                    scratch.Add(tickable);
                }
            }

            tickables.Clear();
            toAdd.Clear();
            toRemove.Clear();
            isTicking = false;

            foreach (var tickable in scratch)
            {
                tickable.OnTickStopped();
            }
            scratch.Clear();
        }

        public static void Tick(float gameDelta, float uiDelta)
        {
            isTicking = true;

            FlushRemovals();

            foreach (var tickable in toAdd)
            {
                tickables.Add(tickable);
            }
            toAdd.Clear();

            foreach (var tickable in tickables)
            {
                float delta = tickable.GetTickType() == TickType.UI ? uiDelta : gameDelta;
                if (delta > 0f)
                {
                    tickable.Tick(delta);
                }
                if (!tickable.IsTicking())
                {
                    toRemove.Add(tickable);
                }
            }

            FlushRemovals();

            isTicking = false;
        }

        public static void Register(ITickable tickable)
        {
            toRemove.Remove(tickable);
            if (isTicking)
            {
                toAdd.Add(tickable);
            }
            else
            {
                tickables.Add(tickable);
            }
        }

        public static void Unregister(ITickable tickable)
        {
            toAdd.Remove(tickable);
            toRemove.Add(tickable);
        }

        static void FlushRemovals()
        {
            if (toRemove.Count == 0)
            {
                return;
            }

            scratch.Clear();
            scratch.AddRange(toRemove);
            toRemove.Clear();

            foreach (var tickable in scratch)
            {
                if (tickables.Remove(tickable))
                {
                    tickable.OnTickStopped();
                }
            }

            scratch.Clear();
        }

    }
}
