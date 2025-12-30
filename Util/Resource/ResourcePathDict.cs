using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{

    public class ResourcePathDict<KeyType, ObjType> : Dictionary<KeyType, string>, IDisposable where ObjType : UnityEngine.Object
    {

        [JsonIgnore]
        readonly Dictionary<KeyType, ObjType> cachedObj = new Dictionary<KeyType, ObjType>();
        [JsonIgnore]
        readonly Dictionary<KeyType, AsyncOperationHandle<ObjType>> handles = new Dictionary<KeyType, AsyncOperationHandle<ObjType>>();

        public ObjType Get(KeyType key)
        {
            if (!cachedObj.TryGetValue(key, out ObjType obj))
            {
                if (!TryGetValue(key, out string path))
                {
                    LogUtil.Warn("resource of {0} not exist", key);
                    return null;
                }
                var result = ResourceUtil.LoadSync<ObjType>(path);
                if (result.IsValid)
                {
                    obj = result.Asset;
                    cachedObj[key] = obj;
                    handles[key] = result.Handle;
                }
            }
            return obj;
        }

        public void Dispose()
        {
            foreach (var handle in handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            handles.Clear();
            cachedObj.Clear();
        }

    }

}