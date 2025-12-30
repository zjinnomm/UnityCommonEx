using UnityEngine;

namespace UnityCommonEx
{
    public class ProgressRing : MonoBehaviour
    {

        public UIRing Background;
        public UIRing Progress;
        public float OuterRadius;
        public float InnerRadius;

        float progress = 0f;

        private void Start()
        {
            Background.OuterRadius = Progress.OuterRadius = OuterRadius;
            Background.InnerRadius = Progress.InnerRadius = InnerRadius;
            Background.Progress = 1f;
            Background.SetAllDirty();
        }

        public void SetProgress(float progress)
        {
            if (this.progress != progress)
            {
                this.progress = progress;
                Progress.Progress = progress;
                Progress.SetAllDirty();
            }
        }

    }
}