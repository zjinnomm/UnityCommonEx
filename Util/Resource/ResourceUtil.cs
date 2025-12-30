using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{
    /// <summary>
    /// 资源加载结果，包含资源对象和 Handle
    /// </summary>
    public struct ResourceLoadResult<T> where T : UnityEngine.Object
    {
        public T Asset;
        public AsyncOperationHandle<T> Handle;
        
        public bool IsValid => Asset != null && Handle.IsValid();
    }

    /// <summary>
    /// 资源加载工具类
    /// </summary>
    public static class ResourceUtil
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <returns>加载的资源对象和 Handle，失败时 Asset 为 null，Handle 会被自动释放</returns>
        public static ResourceLoadResult<T> LoadSync<T>(string path) where T : UnityEngine.Object
        {
            ResourceLoadResult<T> result = default;
            
            if (string.IsNullOrEmpty(path))
            {
                LogUtil.Error("Resource path is empty");
                return result;
            }

            AsyncOperationHandle<T> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<T>(path);
                handle.WaitForCompletion();
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    result.Asset = handle.Result;
                    result.Handle = handle;
                    // 成功时不释放 Handle，由调用方负责释放
                    return result;
                }
                else
                {
                    LogUtil.Error("Failed to load resource from path: {0}", path);
                    // 失败时释放 Handle
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                    return result;
                }
            }
            catch
            {
                // 异常时释放 Handle
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }
    }
}

