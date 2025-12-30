using System.Collections.Generic;

namespace UnityCommonEx
{
    public static class TickingManager
    {

        static readonly HashSet<ITickable> tickables = new();
        static readonly HashSet<ITickable> toRemove = new();

        public static void Tick(float delta)
        {
            foreach (var tickable in toRemove)
            {
                tickables.Remove(tickable);
            }
            toRemove.Clear();
            foreach (var tickable in tickables)
            {
                tickable.Tick(delta);
                if (!tickable.IsTicking())
                {
                    toRemove.Add(tickable);
                }
            }
            foreach (var tickable in toRemove)
            {
                tickables.Remove(tickable);
            }
            toRemove.Clear();
        }

        public static void Register(ITickable tickable)
        {
            tickables.Add(tickable);
        }

        public static void Unregister(ITickable tickable)
        {
            toRemove.Add(tickable);
        }

    }
}