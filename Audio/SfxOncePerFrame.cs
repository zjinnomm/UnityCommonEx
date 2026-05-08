using UnityEngine;

namespace UnityCommonEx
{
    public struct SfxOncePerFrame
    {
        private int lastFrame;

        public bool TryPlay(string key, float pitch = 1f, float volume = 1f)
        {
            if (lastFrame == Time.frameCount || string.IsNullOrEmpty(key) || AudioManager.Instance == null)
                return false;

            lastFrame = Time.frameCount;
            return AudioManager.Instance.PlaySFX(key, pitch, volume);
        }
    }
}
