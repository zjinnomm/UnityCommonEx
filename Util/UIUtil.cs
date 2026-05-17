using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public static class UIUtil
    {

        static readonly Vector3[] getWorldRectTempVec = new Vector3[4];
        static readonly Vector3[] getBoundsScreenRectTempVec = new Vector3[8];

        public static Rect GetWorldRect(this RectTransform rectTransform)
        {
            rectTransform.GetWorldCorners(getWorldRectTempVec);
            var result = new Rect(
                          getWorldRectTempVec[0].x,
                          getWorldRectTempVec[0].y,
                          getWorldRectTempVec[2].x - getWorldRectTempVec[0].x,
                          getWorldRectTempVec[2].y - getWorldRectTempVec[0].y);
            return result;
        }

        public static Rect GetScreenRect(this RectTransform rectTransform, Camera camera = null)
        {
            rectTransform.GetWorldCorners(getWorldRectTempVec);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, getWorldRectTempVec[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, getWorldRectTempVec[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static Camera GetEventCamera(this Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                return canvas.worldCamera ?? Camera.main;
            }

            return null;
        }

        public static Camera GetEventCamera(this RectTransform rectTransform)
        {
            Canvas canvas = rectTransform != null ? rectTransform.GetComponentInParent<Canvas>() : null;
            return canvas.GetEventCamera();
        }

        public static Rect GetScreenRect(Bounds bounds, Camera camera)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            getBoundsScreenRectTempVec[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            getBoundsScreenRectTempVec[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
            getBoundsScreenRectTempVec[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
            getBoundsScreenRectTempVec[3] = center + new Vector3(-extents.x, extents.y, extents.z);
            getBoundsScreenRectTempVec[4] = center + new Vector3(extents.x, -extents.y, -extents.z);
            getBoundsScreenRectTempVec[5] = center + new Vector3(extents.x, -extents.y, extents.z);
            getBoundsScreenRectTempVec[6] = center + new Vector3(extents.x, extents.y, -extents.z);
            getBoundsScreenRectTempVec[7] = center + new Vector3(extents.x, extents.y, extents.z);

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < getBoundsScreenRectTempVec.Length; i++)
            {
                Vector3 screenPoint = camera.WorldToScreenPoint(getBoundsScreenRectTempVec[i]);
                minX = Mathf.Min(minX, screenPoint.x);
                minY = Mathf.Min(minY, screenPoint.y);
                maxX = Mathf.Max(maxX, screenPoint.x);
                maxY = Mathf.Max(maxY, screenPoint.y);
            }

            if (float.IsInfinity(minX) || float.IsInfinity(minY) ||
                float.IsInfinity(maxX) || float.IsInfinity(maxY))
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        public static Rect ScreenRectToNormalizedRect(Rect screenRect)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(
                screenRect.xMin / Screen.width,
                screenRect.yMin / Screen.height,
                screenRect.xMax / Screen.width,
                screenRect.yMax / Screen.height);
        }

        public static Rect NormalizedRectToScreenRect(Rect normalizedRect)
        {
            return Rect.MinMaxRect(
                normalizedRect.xMin * Screen.width,
                normalizedRect.yMin * Screen.height,
                normalizedRect.xMax * Screen.width,
                normalizedRect.yMax * Screen.height);
        }

        public static bool TryGetNormalizedScreenRect(RectTransform rectTransform, out Rect normalizedRect)
        {
            if (rectTransform == null)
            {
                normalizedRect = Rect.zero;
                return false;
            }

            normalizedRect = ScreenRectToNormalizedRect(rectTransform.GetScreenRect(rectTransform.GetEventCamera()));
            return normalizedRect.size.sqrMagnitude > 0f;
        }

        public static bool TryGetNormalizedScreenRect(Bounds bounds, Camera camera, out Rect normalizedRect)
        {
            Camera activeCamera = camera != null ? camera : Camera.main;
            if (activeCamera == null)
            {
                normalizedRect = Rect.zero;
                return false;
            }

            normalizedRect = ScreenRectToNormalizedRect(GetScreenRect(bounds, activeCamera));
            return normalizedRect.size.sqrMagnitude > 0f;
        }

        public static Vector2 GetScreenPixelSize(this RectTransform rectTransform, Camera camera = null)
        {
            rectTransform.GetWorldCorners(getWorldRectTempVec);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, getWorldRectTempVec[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, getWorldRectTempVec[2]);
            return new Vector2(max.x - min.x, max.y - min.y);
        }

        public static void ForceRebuildLayout(this RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            Canvas.ForceUpdateCanvases();
        }

    }
}
