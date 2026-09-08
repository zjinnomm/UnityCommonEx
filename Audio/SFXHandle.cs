using System;
using UnityEngine;

namespace UnityCommonEx
{
    public enum SFXEndReason { Completed, Stopped, BackendReleased }

    /// <summary>One playback, owned by its audio backend. Use on the Unity main thread.</summary>
    public abstract class SFXHandle : IDisposable
    {
        public bool IsFinished { get; private set; }
        public SFXEndReason? EndReason { get; private set; }
        public event Action<SFXHandle, SFXEndReason> Finished;

        public abstract bool IsPlaying { get; }
        public abstract void SetVolume(float volume);
        public abstract void SetPitch(float pitch);
        public abstract void SetPosition(Vector3 position);
        /// <summary>Returns false if the parameter is unsupported or playback has ended.</summary>
        public abstract bool SetParameter(string name, float value);
        protected abstract void ReleasePlayback();

        public void Stop() => Finish(SFXEndReason.Stopped);
        public void Dispose() => Stop();

        internal void Finish(SFXEndReason reason)
        {
            if (IsFinished) return;
            IsFinished = true;
            EndReason = reason;
            ReleasePlayback();
            var callbacks = Finished;
            Finished = null;
            if (callbacks == null) return;
            foreach (Action<SFXHandle, SFXEndReason> callback in callbacks.GetInvocationList())
            {
                try { callback(this, reason); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
    }
}
