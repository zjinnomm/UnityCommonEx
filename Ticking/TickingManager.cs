using System.Collections.Generic;

namespace UnityCommonEx
{
    public static class TickingManager
    {

        static readonly HashSet<ITickable> tickables = new();
        static readonly HashSet<ITickable> toAdd = new();
        static readonly HashSet<ITickable> toRemove = new();
        
        /// <summary>
        /// 是否正在 Tick 的标志位
        /// </summary>
        static bool isTicking = false;

        public static void Tick(float delta)
        {
            // 设置标志位：开始 Tick
            isTicking = true;
            
            // 处理待移除的项
            foreach (var tickable in toRemove)
            {
                tickables.Remove(tickable);
            }
            toRemove.Clear();
            
            // 处理待添加的项
            foreach (var tickable in toAdd)
            {
                tickables.Add(tickable);
            }
            toAdd.Clear();
            
            // 遍历并 Tick
            foreach (var tickable in tickables)
            {
                tickable.Tick(delta);
                if (!tickable.IsTicking())
                {
                    toRemove.Add(tickable);
                }
            }
            
            // 处理 Tick 过程中标记为移除的项
            foreach (var tickable in toRemove)
            {
                tickables.Remove(tickable);
            }
            toRemove.Clear();
            
            // 清除标志位：结束 Tick
            isTicking = false;
        }

        public static void Register(ITickable tickable)
        {
            if (isTicking)
            {
                // 如果正在 Tick，加入待添加列表
                toAdd.Add(tickable);
            }
            else
            {
                // 如果不在 Tick，直接添加
                tickables.Add(tickable);
            }
        }

        public static void Unregister(ITickable tickable)
        {
            toRemove.Add(tickable);
        }

    }
}