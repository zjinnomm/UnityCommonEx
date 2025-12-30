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
                if (Meter != null && Background != null)
                {
                    Meter.sizeDelta = new Vector2(Background.sizeDelta.x * progress, Meter.sizeDelta.y);
                }
            }
        }

    }
}