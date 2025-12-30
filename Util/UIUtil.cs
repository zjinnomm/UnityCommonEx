using UnityEngine;

namespace UnityCommonEx
{
    public static class UIUtil
    {

        static readonly Vector3[] getWorldRectTempVec = new Vector3[4];

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

    }
}