using System.Collections.Generic;

namespace UnityCommonEx
{

    public class PoolableList<T> : List<T>, IPoolable
    {

        public void OnPoolableReturned()
        {
            Clear();
        }

    }

    public class PoolableMap<T1, T2> : Dictionary<T1, T2>, IPoolable
    {

        public void OnPoolableReturned()
        {
            Clear();
        }

    }


}