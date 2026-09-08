using System.Collections.Generic;
using UnityEngine;

#if FMOD_PRESENT
using FMOD.Studio;
using FMODUnity;
#endif

namespace UnityCommonEx
{
    /// <summary>
    /// FMOD audio backend.
    /// In this backend, config row Path values are treated as FMOD event paths.
    /// </summary>
    public class AudioManager_FMOD : AudioManager
    {
        private const string PitchShiftParameterName = "PitchShift";
        public string BGMBusPath = "bus:/";
        public string SFXBusPath = "bus:/";

        private DataTable<string, SFXConfigRow> sfxConfigTable;
        private DataTable<string, BGMConfigRow> bgmConfigTable;

#if FMOD_PRESENT
        private sealed class FMODSFXHandle : SFXHandle
        {
            private EventInstance instance;
            private readonly AudioManager_FMOD owner;
            private float volume;
            public FMODSFXHandle(AudioManager_FMOD owner, EventInstance instance, float volume)
            {
                this.owner = owner;
                this.instance = instance;
                this.volume = volume;
            }
            public override bool IsPlaying
            {
                get
                {
                    if (IsFinished || !instance.isValid()) return false;
                    return instance.getPlaybackState(out var state) == FMOD.RESULT.OK && state != PLAYBACK_STATE.STOPPED;
                }
            }
            public override void SetVolume(float value)
            {
                if (IsFinished) return;
                volume = Mathf.Clamp01(value);
                ApplyVolume();
            }
            public void ApplyVolume()
            {
                if (!IsFinished) instance.setVolume(volume * (owner.sfxBus.isValid() ? 1f : owner.SFXVolume));
            }
            public override void SetPitch(float pitch) { if (!IsFinished) ApplySFXPitch(instance, pitch); }
            public override void SetPosition(Vector3 position)
            {
                if (!IsFinished) instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            }
            public override bool SetParameter(string name, float value)
            {
                return !IsFinished && !string.IsNullOrEmpty(name) && instance.setParameterByName(name, value) == FMOD.RESULT.OK;
            }
            protected override void ReleasePlayback()
            {
                if (!instance.isValid()) return;
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
                instance.clearHandle();
            }
        }
        private readonly List<FMODSFXHandle> playingSFX = new List<FMODSFXHandle>();
        private EventInstance currentBGMInstance;
        private Bus bgmBus;
        private Bus sfxBus;
#endif

        protected override void OnInitializeBackend()
        {
            sfxConfigTable = DataTableManager.Get<string, SFXConfigRow>()
                ?? DataTableManager.Get<string, SFXConfigRow>("SFXConfig");
            if (sfxConfigTable == null)
            {
                LogUtil.Error("AudioManager_FMOD: missing SFXConfig table");
            }

            bgmConfigTable = DataTableManager.Get<string, BGMConfigRow>()
                ?? DataTableManager.Get<string, BGMConfigRow>("BGMConfig");

            if (bgmConfigTable == null)
            {
                LogUtil.Error("AudioManager_FMOD: missing BGMConfig table");
            }

#if FMOD_PRESENT
            bgmBus = TryGetBus(BGMBusPath);
            sfxBus = TryGetBus(SFXBusPath);
#endif
        }

        protected override SFXHandle PlaySFXCore(string key, float pitch, float volume, Vector3? worldPosition)
        {
            string eventPath = GetSFXEventPath(key);
            if (string.IsNullOrEmpty(eventPath))
            {
                return null;
            }

#if FMOD_PRESENT
            try
            {
                var instance = RuntimeManager.CreateInstance(eventPath);
                ApplySFXPitch(instance, pitch);
                instance.setVolume(volume * (sfxBus.isValid() ? 1f : SFXVolume));
                if (worldPosition.HasValue)
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition.Value));
                if (instance.start() != FMOD.RESULT.OK)
                {
                    instance.release();
                    return null;
                }
                var handle = new FMODSFXHandle(this, instance, volume);
                playingSFX.Add(handle);
                return handle;
            }
            catch (EventNotFoundException)
            {
                LogUtil.Warn("AudioManager_FMOD: invalid SFX event for {0}: {1}", key, eventPath);
                return null;
            }
#else
            LogUtil.Warn("AudioManager_FMOD: FMOD_PRESENT is not enabled, cannot play SFX {0}", key);
            return null;
#endif
        }

        protected override bool PlayBGMCore(string key, bool fadeIn)
        {
            string eventPath = GetBGMEventPath(key);
            if (string.IsNullOrEmpty(eventPath))
            {
                return false;
            }

#if FMOD_PRESENT
            StopBGMInstance();

            try
            {
                currentBGMInstance = RuntimeManager.CreateInstance(eventPath);
                currentBGMInstance.setVolume(1f);
                currentBGMInstance.start();
                return true;
            }
            catch (EventNotFoundException)
            {
                LogUtil.Warn("AudioManager_FMOD: invalid BGM event for {0}: {1}", key, eventPath);
                currentBGMInstance.clearHandle();
                return false;
            }
#else
            LogUtil.Warn("AudioManager_FMOD: FMOD_PRESENT is not enabled, cannot play BGM {0}", key);
            return false;
#endif
        }

        protected override void StopBGMCore(bool fadeOut)
        {
#if FMOD_PRESENT
            StopBGMInstance();
#endif
        }

        protected override void PauseBGMCore()
        {
#if FMOD_PRESENT
            if (currentBGMInstance.isValid())
            {
                currentBGMInstance.setPaused(true);
            }
#endif
        }

        protected override void ResumeBGMCore()
        {
#if FMOD_PRESENT
            if (currentBGMInstance.isValid())
            {
                currentBGMInstance.setPaused(false);
            }
#endif
        }

        protected override void ApplyBGMVolume(float volume)
        {
#if FMOD_PRESENT
            if (bgmBus.isValid())
            {
                bgmBus.setVolume(volume);
            }
            else if (currentBGMInstance.isValid())
            {
                currentBGMInstance.setVolume(volume);
            }
#endif
        }

        protected override void ApplySFXVolume(float volume)
        {
#if FMOD_PRESENT
            if (sfxBus.isValid())
            {
                sfxBus.setVolume(volume);
            }
            foreach (var handle in playingSFX) handle.ApplyVolume();
#endif
        }

        protected override bool IsBGMPlayingCore()
        {
#if FMOD_PRESENT
            if (!currentBGMInstance.isValid())
            {
                return false;
            }

            currentBGMInstance.getPlaybackState(out var state);
            return state != PLAYBACK_STATE.STOPPED;
#else
            return false;
#endif
        }

        protected override void ReleaseBackend()
        {
#if FMOD_PRESENT
            var active = playingSFX.ToArray();
            playingSFX.Clear();
            foreach (var handle in active) handle.Finish(SFXEndReason.BackendReleased);
            StopBGMInstance();
#endif
        }

        private string GetSFXEventPath(string key)
        {
            if (sfxConfigTable == null)
            {
                return null;
            }

            SFXConfigRow row = sfxConfigTable.GetRow(key);
            if (row == null)
            {
                LogUtil.Warn("AudioManager_FMOD: SFX {0} not found in config", key);
                return null;
            }

            return row.Path;
        }

        private string GetBGMEventPath(string key)
        {
            if (bgmConfigTable == null)
            {
                LogUtil.Warn("AudioManager_FMOD: BGMConfig table is not configured");
                return null;
            }

            BGMConfigRow row = bgmConfigTable.GetRow(key);
            if (row == null)
            {
                LogUtil.Warn("AudioManager_FMOD: BGM {0} not found in config", key);
                return null;
            }

            return row.Path;
        }

#if FMOD_PRESENT
        private void Update()
        {
            for (int i = playingSFX.Count - 1; i >= 0; i--)
            {
                var handle = playingSFX[i];
                if (handle.IsPlaying) continue;
                playingSFX.RemoveAt(i);
                handle.Finish(SFXEndReason.Completed);
            }
        }

        private static Bus TryGetBus(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return default;
            }

            try
            {
                return RuntimeManager.GetBus(path);
            }
            catch (BusNotFoundException)
            {
                LogUtil.Warn("AudioManager_FMOD: bus not found, falling back to instance volume: {0}", path);
                return default;
            }
        }

        private void StopBGMInstance()
        {
            if (!currentBGMInstance.isValid())
            {
                return;
            }

            currentBGMInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentBGMInstance.release();
            currentBGMInstance.clearHandle();
        }

        private static void ApplySFXPitch(EventInstance instance, float pitch)
        {
            FMOD.RESULT result = instance.setParameterByName(PitchShiftParameterName, pitch, false);
            if (result != FMOD.RESULT.OK)
            {
                instance.setPitch(pitch);
            }
        }
#endif
    }
}
