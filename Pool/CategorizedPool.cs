using System.Collections.Generic;
using System;

namespace UnityCommonEx
{

    public class CategorizedPool<T1, T2> : Singleton<CategorizedPool<T1, T2>> where T1 : class, ICategorizedPoolable<T2>
    {

        readonly Dictionary<T2, Queue<T1>> pooled = new Dictionary<T2, Queue<T1>>();
        readonly Type[] typeArray = new Type[] { typeof(T2) };

        public T1 GetInstance(T2 key, Func<T2, T1> newFunc = null)
        {
            if (pooled.TryGetValue(key, out Queue<T1> pool))
            {
                while (pool.Count > 0)
                {
                    T1 item = pool.Dequeue();
                    if (item.IsPoolableReusable())
                    {
                        item.OnPoolableTaken();
                        return item;
                    }
                    else
                    {
                        item.OnPoolableDestroyed();
                    }
                }
            }
            if (newFunc != null)
            {
                T1 item = newFunc(key);
                item.SetPoolCategory(key);
                item.OnPoolableCreated();
                return item;
            }
            if (typeof(T1).GetConstructor(typeArray) != null)
            {
                T1 item = Activator.CreateInstance(typeof(T1), key) as T1;
                if (item != null)
                {
                    item.OnPoolableCreated();
                    return item;
                }
            }
            return null;
        }

        public void ReturnInstance(T1 item)
        {
            if (item.IsPoolableReusable())
            {
                T2 key = item.GetPoolCategory();
                Queue<T1> pool = null;
                if (pooled.TryGetValue(key, out pool))
                {
                    if (pool.Contains(item))
                    {
                        LogUtil.Error("return instance already in pool");
                        return;
                    }
                }
                else
                {
                    pool = new Queue<T1>();
                    pooled.Add(key, pool);
                }
                item.OnPoolableReturned();
                pool.Enqueue(item);
            }
            else
            {
                item.OnPoolableDestroyed();
            }
        }

        public void ClearInstances()
        {
            foreach (var kvp in pooled)
            {
                while (kvp.Value.Count > 0)
                {
                    kvp.Value.Dequeue().OnPoolableDestroyed();
                }
            }
        }

    }

}