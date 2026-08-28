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

        protected override bool PlaySFXCore(string key, float pitch, float volume, Vector3? worldPosition)
        {
            string eventPath = GetSFXEventPath(key);
            if (string.IsNullOrEmpty(eventPath))
            {
                return false;
            }

#if FMOD_PRESENT
            try
            {
                var instance = RuntimeManager.CreateInstance(eventPath);
                ApplySFXPitch(instance, pitch);
                instance.setVolume(volume);
                if (worldPosition.HasValue)
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition.Value));
                instance.start();
                instance.release();
                return true;
            }
            catch (EventNotFoundException)
            {
                LogUtil.Warn("AudioManager_FMOD: invalid SFX event for {0}: {1}", key, eventPath);
                return false;
            }
#else
            LogUtil.Warn("AudioManager_FMOD: FMOD_PRESENT is not enabled, cannot play SFX {0}", key);
            return false;
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
            if (Mathf.Approximately(pitch, 1f))
            {
                return;
            }

            FMOD.RESULT result = instance.setParameterByName(PitchShiftParameterName, pitch, false);
            if (result != FMOD.RESULT.OK)
            {
                LogUtil.Error("Failed");
            }
        }
#endif
    }
}
