using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{
    /// <summary>
    /// 音频管理器
    /// - 单例控制器，常驻场景
    /// - 通过 SFXConfigRow 配置音效 Key 与资源路径
    /// - 通过 BGMConfigRow 配置背景音乐 Key 与资源路径
    /// - 提供 PlaySFX(key, pitch, volume) 播放音效
    /// - 提供 PlayBGM(key) / StopBGM() 管理背景音乐
    /// - 内部使用 AudioSource 池，播放结束后回收到池中
    /// </summary>
    public class AudioManager : SingletonController<AudioManager>
    {
        /// <summary>
        /// 音效资源根路径（与配置中的 Path 拼接）
        /// </summary>
        public string SFXRootResPath;

        /// <summary>
        /// BGM 资源根路径（与配置中的 Path 拼接）
        /// </summary>
        public string BGMRootResPath;

        /// <summary>
        /// BGM 音量（0-1）
        /// </summary>
        [Range(0f, 1f)]
        public float BGMVolume = 1f;

        /// <summary>
        /// SFX 音量（0-1）
        /// </summary>
        [Range(0f, 1f)]
        public float SFXVolume = 1f;

        private DataTable<string, SFXConfigRow> sfxConfigTable;
        private DataTable<string, BGMConfigRow> bgmConfigTable;

        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> sfxHandles =
            new Dictionary<string, AsyncOperationHandle<AudioClip>>();
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> bgmHandles =
            new Dictionary<string, AsyncOperationHandle<AudioClip>>();

        private readonly Queue<AudioSource> idleSources = new Queue<AudioSource>();

        private struct PlayingSFX
        {
            public AudioSource Source;
            public float BaseVolume;
        }

        private readonly List<PlayingSFX> playingSources = new List<PlayingSFX>();

        /// <summary>
        /// 同名音效压制：上一帧播放过的 key（SuppressDuration == 0 时用）
        /// </summary>
        /// <summary>
        /// 同名音效压制：该 key 在此时间前不再播放（SuppressDuration > 0 时用）
        /// </summary>

        // BGM 相关
        private AudioSource bgmSource;
        private string currentBGMKey;

        protected override bool IsPersistent => true;

        public void Initialize()
        {
            DontDestroyOnLoad(gameObject);

            sfxConfigTable = DataTableManager.Get<string, SFXConfigRow>();
            if (sfxConfigTable == null)
            {
                LogUtil.Error("AudioManager: 缺少 SFXConfig 数据表");
            }

            bgmConfigTable = DataTableManager.Get<string, BGMConfigRow>();
            // BGM 配置表可选，不报错

            // 创建 BGM AudioSource
            var bgmGo = new GameObject("BGM_AudioSource");
            bgmGo.transform.SetParent(transform, false);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = BGMVolume;
        }

        /// <summary>
        /// 播放指定 Key 的音效
        /// </summary>
        /// <param name="key">音效 Key</param>
        /// <param name="pitch">音高</param>
        /// <param name="suppressDuration">同名音效压制：&lt;0 不压制，=0 本帧压制，&gt;0 该时长（秒）内压制</param>
        /// <returns>是否成功播放</returns>
        public bool PlaySFX(string key, float pitch = 1f, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            AudioClip clip = GetOrLoadClip(key);
            if (clip == null)
            {
                return false;
            }

            AudioSource source = GetAudioSource();
            if (source == null)
            {
                return false;
            }

            source.clip = clip;
            source.pitch = pitch;
            float baseVolume = Mathf.Clamp01(volume);
            source.volume = baseVolume * SFXVolume;
            source.gameObject.SetActive(true);
            source.Play();

            playingSources.Add(new PlayingSFX { Source = source, BaseVolume = baseVolume });

            return true;
        }

        #region BGM

        /// <summary>
        /// 播放指定 Key 的背景音乐
        /// </summary>
        /// <param name="key">BGM Key</param>
        /// <param name="fadeIn">是否淡入（暂未实现）</param>
        public void PlayBGM(string key, bool fadeIn = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            // 如果已经在播放相同的 BGM，不做处理
            if (currentBGMKey == key && bgmSource != null && bgmSource.isPlaying)
            {
                return;
            }

            AudioClip clip = GetOrLoadBGMClip(key);
            if (clip == null)
            {
                return;
            }

            if (bgmSource == null)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.volume = BGMVolume;
            bgmSource.Play();
            currentBGMKey = key;
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        /// <param name="fadeOut">是否淡出（暂未实现）</param>
        public void StopBGM(bool fadeOut = false)
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
            currentBGMKey = null;
        }

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        public void PauseBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public void ResumeBGM()
        {
            if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
            {
                bgmSource.UnPause();
            }
        }

        /// <summary>
        /// 设置 BGM 音量
        /// </summary>
        /// <param name="volume">音量（0-1）</param>
        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
            if (bgmSource != null)
            {
                bgmSource.volume = BGMVolume;
            }
        }

        /// <summary>
        /// 设置 SFX 音量
        /// </summary>
        /// <param name="volume">音量（0-1）</param>
        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            for (int i = 0; i < playingSources.Count; i++)
            {
                var src = playingSources[i].Source;
                if (src != null && src.isPlaying)
                    src.volume = playingSources[i].BaseVolume * SFXVolume;
            }
        }

        /// <summary>
        /// 获取当前 BGM Key
        /// </summary>
        public string GetCurrentBGMKey()
        {
            return currentBGMKey;
        }

        /// <summary>
        /// 当前是否正在播放 BGM
        /// </summary>
        public bool IsBGMPlaying()
        {
            return bgmSource != null && bgmSource.isPlaying;
        }

        private AudioClip GetOrLoadBGMClip(string key)
        {
            if (bgmConfigTable == null)
            {
                LogUtil.Warn("AudioManager: BGMConfig 数据表未配置");
                return null;
            }

            BGMConfigRow row = bgmConfigTable.GetRow(key);
            if (row == null)
            {
                LogUtil.Warn("AudioManager: BGM {0} 未在配置表中找到", key);
                return null;
            }

            if (bgmHandles.TryGetValue(key, out var handle) && handle.IsValid())
            {
                return handle.Result;
            }

            string path = string.IsNullOrEmpty(BGMRootResPath)
                ? row.Path
                : $"{BGMRootResPath}/{row.Path}";

            var result = ResourceUtil.LoadSync<AudioClip>(path);
            if (!result.IsValid)
            {
                LogUtil.Warn("AudioManager: BGM {0} 的路径 {1} 无效", key, row.Path);
                return null;
            }

            bgmHandles[key] = result.Handle;
            return result.Asset;
        }

        #endregion

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
            // 释放 SFX 资源
            foreach (var handle in sfxHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            sfxHandles.Clear();

            // 释放 BGM 资源
            foreach (var handle in bgmHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            bgmHandles.Clear();

            // 销毁 BGM AudioSource
            if (bgmSource != null)
            {
                Destroy(bgmSource.gameObject);
                bgmSource = null;
            }
            currentBGMKey = null;

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

