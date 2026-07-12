using UnityEngine;

namespace UnityCommonEx
{
    public class ProgressBar : MonoBehaviour
    {

        public RectTransform Background;
        public RectTransform Meter;

        float progress = 0;
        public float Progress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = Mathf.Clamp01(value);
                ApplyProgress();
            }
        }

        void OnRectTransformDimensionsChange()
        {
            ApplyProgress();
        }

        void ApplyProgress()
        {
            if (Meter == null || Background == null)
                return;

            Meter.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Background.rect.width * progress);
        }

    }
}