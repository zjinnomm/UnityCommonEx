using UnityEngine;

namespace UnityCommonEx
{

    public readonly struct TutorialRectSource
    {

        public readonly int RectIndex;
        public readonly Rect NormalizedRect;

        public TutorialRectSource(int rectIndex, Rect normalizedRect)
        {
            RectIndex = rectIndex;
            NormalizedRect = normalizedRect;
        }

        public static TutorialRectSource FromNormalizedRect(int rectIndex, Rect normalizedRect)
        {
            return new TutorialRectSource(rectIndex, normalizedRect);
        }

        public static bool TryCreate(int rectIndex, RectTransform rectTransform, out TutorialRectSource source)
        {
            if (UIUtil.TryGetNormalizedScreenRect(rectTransform, out Rect normalizedRect))
            {
                source = new TutorialRectSource(rectIndex, normalizedRect);
                return true;
            }

            source = default;
            return false;
        }

        public static bool TryCreate(int rectIndex, Bounds worldBounds, Camera worldCamera, out TutorialRectSource source)
        {
            if (UIUtil.TryGetNormalizedScreenRect(worldBounds, worldCamera, out Rect normalizedRect))
            {
                source = new TutorialRectSource(rectIndex, normalizedRect);
                return true;
            }

            source = default;
            return false;
        }

    }

}
