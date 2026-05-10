using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{
    /// <summary>
    /// Unity built-in audio backend.
    /// </summary>
    public class AudioManager_BuiltIn : AudioManager
    {
        public string SFXRootResPath;
        public string BGMRootResPath;

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
        private AudioSource bgmSource;

        protected override void OnInitializeBackend()
        {
            sfxConfigTable = DataTableManager.Get<string, SFXConfigRow>()
                ?? DataTableManager.Get<string, SFXConfigRow>("SFXConfig");
            if (sfxConfigTable == null)
            {
                LogUtil.Error("AudioManager_BuiltIn: missing SFXConfig table");
            }

            bgmConfigTable = DataTableManager.Get<string, BGMConfigRow>()
                ?? DataTableManager.Get<string, BGMConfigRow>("BGMConfig");

            var bgmGo = new GameObject("BGM_AudioSource");
            bgmGo.transform.SetParent(transform, false);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = BGMVolume;
        }

        protected override bool PlaySFXCore(string key, float pitch, float volume)
        {
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
            source.volume = volume * SFXVolume;
            source.gameObject.SetActive(true);
            source.Play();

            playingSources.Add(new PlayingSFX { Source = source, BaseVolume = volume });
            return true;
        }

        protected override bool PlayBGMCore(string key, bool fadeIn)
        {
            AudioClip clip = GetOrLoadBGMClip(key);
            if (clip == null || bgmSource == null)
            {
                return false;
            }

            bgmSource.clip = clip;
            bgmSource.volume = BGMVolume;
            bgmSource.Play();
            return true;
        }

        protected override void StopBGMCore(bool fadeOut)
        {
            if (bgmSource == null)
            {
                return;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
        }

        protected override void PauseBGMCore()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        protected override void ResumeBGMCore()
        {
            if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
            {
                bgmSource.UnPause();
            }
        }

        protected override void ApplyBGMVolume(float volume)
        {
            if (bgmSource != null)
            {
                bgmSource.volume = volume;
            }
        }

        protected override void ApplySFXVolume(float volume)
        {
            for (int i = 0; i < playingSources.Count; i++)
            {
                var src = playingSources[i].Source;
                if (src != null && src.isPlaying)
                {
                    src.volume = playingSources[i].BaseVolume * volume;
                }
            }
        }

        protected override bool IsBGMPlayingCore()
        {
            return bgmSource != null && bgmSource.isPlaying;
        }

        private AudioClip GetOrLoadBGMClip(string key)
        {
            if (bgmConfigTable == null)
            {
                LogUtil.Warn("AudioManager_BuiltIn: BGMConfig table is not configured");
                return null;
            }

            BGMConfigRow row = bgmConfigTable.GetRow(key);
            if (row == null)
            {
                LogUtil.Warn("AudioManager_BuiltIn: BGM {0} not found in config", key);
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
                LogUtil.Warn("AudioManager_BuiltIn: invalid BGM path for {0}: {1}", key, row.Path);
                return null;
            }

            bgmHandles[key] = result.Handle;
            return result.Asset;
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
                LogUtil.Warn("AudioManager_BuiltIn: SFX {0} not found in config", key);
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
                LogUtil.Warn("AudioManager_BuiltIn: invalid SFX path for {0}: {1}", key, row.Path);
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

        protected override void ReleaseBackend()
        {
            foreach (var handle in sfxHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            sfxHandles.Clear();

            foreach (var handle in bgmHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            bgmHandles.Clear();

            if (bgmSource != null)
            {
                Destroy(bgmSource.gameObject);
                bgmSource = null;
            }

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
        }
    }
}
