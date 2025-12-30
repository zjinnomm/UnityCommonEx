using System;
using System.Collections.Generic;

namespace UnityCommonEx
{

    public struct ScopedPoolable<T> : IDisposable where T : class, IPoolable
    {

        T poolableObject;

        public T Get()
        {
            if (poolableObject == null)
            {
                poolableObject = InstancePool<T>.Instance.GetInstance();
            }
            return poolableObject;
        }

        public void Dispose()
        {
            if (poolableObject != null)
            {
                InstancePool<T>.Instance.ReturnInstance(poolableObject);
                poolableObject = null;
            }
        }

    }

}