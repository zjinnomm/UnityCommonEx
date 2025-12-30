using System.Collections.Generic;
using System;

namespace UnityCommonEx
{

    public class InstancePool<T> : Singleton<InstancePool<T>> where T : class, IPoolable
    {

        readonly Queue<T> pooled = new Queue<T>();

        public T GetInstance(Func<T> newFunc = null)
        {
            while (pooled.Count > 0)
            {
                T item = pooled.Dequeue();
                if (item == null)
                {
                    continue;
                }
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
            if (newFunc != null)
            {
                T item = newFunc();
                item.OnPoolableCreated();
                return item;
            }
            if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
            {
                T item = Activator.CreateInstance<T>();
                item.OnPoolableCreated();
                return item;
            }
            return null;
        }

        public void ReturnInstance(T item)
        {
            if (item == null)
            {
                return;
            }
            if (pooled.Contains(item))
            {
                LogUtil.Error("return instance already in pool");
                return;
            }
            if (item.IsPoolableReusable())
            {
                item.OnPoolableReturned();
                pooled.Enqueue(item);
            }
            else
            {
                item.OnPoolableDestroyed();
            }
        }

        public void ClearInstances()
        {
            while (pooled.Count > 0)
            {
                pooled.Dequeue().OnPoolableDestroyed();
            }
        }

    }

}