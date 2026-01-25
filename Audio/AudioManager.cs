using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{
    /// <summary>
    /// 简单的音效管理器（将来可扩展管理 BGM）
    /// - 单例控制器，常驻场景
    /// - 通过 SFXConfigRow 配置音效 Key 与资源路径
    /// - 提供 PlaySFX(key, pitch) 播放音效
    /// - 内部使用 AudioSource 池，播放结束后回收到池中
    /// </summary>
    public class AudioManager : SingletonController<AudioManager>
    {
        /// <summary>
        /// 音效资源根路径（与配置中的 Path 拼接）
        /// </summary>
        public string SFXRootResPath;

        private DataTable<string, SFXConfigRow> sfxConfigTable;
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> sfxHandles =
            new Dictionary<string, AsyncOperationHandle<AudioClip>>();

        private readonly Queue<AudioSource> idleSources = new Queue<AudioSource>();

        private struct PlayingSFX
        {
            public AudioSource Source;
        }

        private readonly List<PlayingSFX> playingSources = new List<PlayingSFX>();

        protected override bool IsPersistent => true;

        public void Initialize()
        {
            DontDestroyOnLoad(gameObject);

            sfxConfigTable = DataTableManager.Get<string, SFXConfigRow>();
            if (sfxConfigTable == null)
            {
                LogUtil.Error("AudioManager: 缺少 SFXConfig 数据表");
            }
        }

        /// <summary>
        /// 播放指定 Key 的音效
        /// </summary>
        public void PlaySFX(string key, float pitch = 1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            AudioClip clip = GetOrLoadClip(key);
            if (clip == null)
            {
                return;
            }

            AudioSource source = GetAudioSource();
            if (source == null)
            {
                return;
            }

            source.clip = clip;
            source.pitch = pitch;
            source.gameObject.SetActive(true);
            source.Play();

            playingSources.Add(new PlayingSFX { Source = source });
        }

        private AudioClip GetOrLoadClip(string key)
        {
            if (sfxConfigTable == null)
            {
                return null;
            }

            SFXConfigRow row = sfxConfigTable.GetRow(key);
            if (row == null)
            {
                LogUtil.Warn("AudioManager: SFX {0} 未在配置表中找到", key);
                return null;
            }

            if (sfxHandles.TryGetValue(key, out var handle) && handle.IsValid())
            {
                return handle.Result;
            }

            string path = string.IsNullOrEmpty(SFXRootResPath)
                ? row.Path
                : $"{SFXRootResPath}/{row.Path}";

            var result = ResourceUtil.LoadSync<AudioClip>(path);
            if (!result.IsValid)
            {
                LogUtil.Warn("AudioManager: SFX {0} 的路径 {1} 无效", key, row.Path);
                return null;
            }

            sfxHandles[key] = result.Handle;
            return result.Asset;
        }

        private AudioSource GetAudioSource()
        {
            AudioSource source = null;

            while (idleSources.Count > 0 && source == null)
            {
                source = idleSources.Dequeue();
                if (source == null)
                {
                    // 已被销毁
                    source = null;
                }
            }

            if (source == null)
            {
                var go = new GameObject("SFX_AudioSource");
                go.transform.SetParent(transform, false);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }

            return source;
        }

        private void RecycleAudioSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            idleSources.Enqueue(source);
        }

        private void Update()
        {
            for (int i = playingSources.Count - 1; i >= 0; i--)
            {
                var entry = playingSources[i];
                if (entry.Source == null)
                {
                    playingSources.RemoveAt(i);
                    continue;
                }

                if (!entry.Source.isPlaying)
                {
                    RecycleAudioSource(entry.Source);
                    playingSources.RemoveAt(i);
                }
            }
        }

        protected override void OnRelease()
        {
            foreach (var handle in sfxHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            sfxHandles.Clear();

            while (idleSources.Count > 0)
            {
                var src = idleSources.Dequeue();
                if (src != null)
                {
                    Destroy(src.gameObject);
                }
            }

            for (int i = 0; i < playingSources.Count; i++)
            {
                var src = playingSources[i].Source;
                if (src != null)
                {
                    Destroy(src.gameObject);
                }
            }
            playingSources.Clear();

            base.OnRelease();
        }
    }
}

