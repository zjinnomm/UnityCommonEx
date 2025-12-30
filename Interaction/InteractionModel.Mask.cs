using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public static partial class InteractionModel
    {

        static uint NextMaskId = 1;
        static List<InteractionMask> Masks = new List<InteractionMask>();

        static bool IsPointMasked(Vector2 point)
        {
            foreach (var mask in Masks)
            {
                if (mask.Masked(point))
                {
                    return true;
                }
            }
            return false;
        }

        public static uint MaskRegion(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return 0;
            }
            Rect rect = rectTransform.GetWorldRect();
            rect.x /= Screen.width;
            rect.width /= Screen.width;
            rect.y /= Screen.height;
            rect.height /= Screen.height;
            return MaskRegion(rect);
        }

        public static uint MaskRegion(Rect rect)
        {
            InteractionMask mask = InstancePool<InteractionMask>.Instance.GetInstance();
            mask.Id = NextMaskId;
            NextMaskId ++;
            mask.MaskedRegion = rect;
            Masks.Add(mask);
            return mask.Id;
        }

        public static void RemoveMask(uint id)
        {
            for (int i = 0; i < Masks.Count; i++)
            {
                InteractionMask mask = Masks[i];
                if (mask.Id == id)
                {
                    InstancePool<InteractionMask>.Instance.ReturnInstance(mask);
                    Masks.RemoveAt(i);
                    return;
                }
            }
        }

        public static void ExcludeRegion(uint id, int key, Rect rect)
        {
            for (int i = 0; i < Masks.Count; i++)
            {
                InteractionMask mask = Masks[i];
                if (mask.Id == id)
                {
                    mask.AddExcludeRegion(key, rect);
                    return;
                }
            }
        }

        public static void RemoveExcludeRegion(uint id, int key)
        {
            for (int i = 0; i < Masks.Count; i++)
            {
                InteractionMask mask = Masks[i];
                if (mask.Id == id)
                {
                    mask.RemoveExcludeRegion(key);
                    return;
                }
            }
        }

    }

}