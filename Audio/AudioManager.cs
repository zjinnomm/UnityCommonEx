using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// Abstract audio manager with a stable public API.
    /// Concrete playback is implemented by backend-specific subclasses.
    /// </summary>
    public abstract class AudioManager : SingletonController<AudioManager>
    {
        [Range(0f, 1f)]
        public float BGMVolume = 1f;

        [Range(0f, 1f)]
        public float SFXVolume = 1f;

        protected string currentBGMKey;
        private bool initialized = false;

        protected override bool IsPersistent => true;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            DontDestroyOnLoad(gameObject);
            OnInitializeBackend();
            ApplyBGMVolume(BGMVolume);
            ApplySFXVolume(SFXVolume);
        }

        public bool PlaySFX(string key, float pitch = 1f, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return PlaySFXCore(key, pitch, Mathf.Clamp01(volume), null);
        }

        /// <summary>
        /// Plays an SFX at a world-space position so backends can provide directional audio.
        /// </summary>
        public bool PlaySFXAtPosition(string key, Vector3 worldPosition, float pitch = 1f, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return PlaySFXCore(key, pitch, Mathf.Clamp01(volume), worldPosition);
        }

        public void PlayBGM(string key, bool fadeIn = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (currentBGMKey == key && IsBGMPlayingCore())
            {
                return;
            }

            if (PlayBGMCore(key, fadeIn))
            {
                currentBGMKey = key;
            }
        }

        public void StopBGM(bool fadeOut = false)
        {
            StopBGMCore(fadeOut);
            currentBGMKey = null;
        }

        public void PauseBGM()
        {
            PauseBGMCore();
        }

        public void ResumeBGM()
        {
            ResumeBGMCore();
        }

        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
            ApplyBGMVolume(BGMVolume);
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            ApplySFXVolume(SFXVolume);
        }

        public string GetCurrentBGMKey()
        {
            return currentBGMKey;
        }

        public bool IsBGMPlaying()
        {
            return IsBGMPlayingCore();
        }

        protected abstract void OnInitializeBackend();
        protected abstract bool PlaySFXCore(string key, float pitch, float volume, Vector3? worldPosition);
        protected abstract bool PlayBGMCore(string key, bool fadeIn);
        protected abstract void StopBGMCore(bool fadeOut);
        protected abstract void PauseBGMCore();
        protected abstract void ResumeBGMCore();
        protected abstract void ApplyBGMVolume(float volume);
        protected abstract void ApplySFXVolume(float volume);
        protected abstract bool IsBGMPlayingCore();
        protected abstract void ReleaseBackend();

        protected override void OnRelease()
        {
            ReleaseBackend();
            currentBGMKey = null;
            initialized = false;
            base.OnRelease();
        }
    }
}
