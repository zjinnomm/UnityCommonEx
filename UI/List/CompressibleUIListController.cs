using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    /// <summary>
    /// 可压缩列表控制器：当数量超过 Rows*Cols 时，自动压缩布局，将更多元素塞入每行。
    /// 适用于 Elim/Fall 等数量可能超限的列表，替代 GridLayoutGroup 的手动布局。
    /// </summary>
    public class CompressibleUIListController : UIListController
    {
        [Header("Compressible Layout")]
        public int Rows = 1;
        public int Cols = 1;
        public Vector2 ItemSize = Vector2.zero;
        public Vector2 ItemSpace = new Vector2(10f, 10f);

        private RectTransform _rectTransform;
        private LayoutGroup _layoutGroup;

        protected override void OnInit()
        {
            base.OnInit();
            _rectTransform = transform as RectTransform;
            _layoutGroup = GetComponent<LayoutGroup>();
        }

        public override void SetItems<T>(IList<T> items, int minSize = 0)
        {
            base.SetItems(items, minSize);
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;
            if (_rectTransform == null)
                return;

            int count = controllers.Count;
            if (count == 0)
                return;

            if (_layoutGroup != null && _layoutGroup.enabled)
                _layoutGroup.enabled = false;

            float itemW = ItemSize.x > 0 ? ItemSize.x : GetChildSize().x;
            float itemH = ItemSize.y > 0 ? ItemSize.y : GetChildSize().y;

            int maxSlots = Rows * Cols;
            float totalWidth = Cols * itemW + (Cols - 1) * ItemSpace.x;
            if (ItemSize.x <= 0 || totalWidth <= 0)
                totalWidth = _rectTransform.rect.width;

            if (count <= maxSlots)
            {
                for (int i = 0; i < count; i++)
                {
                    int row = i / Cols;
                    int col = i % Cols;
                    float x = col * (itemW + ItemSpace.x);
                    float y = -row * (itemH + ItemSpace.y);
                    SetChildLayout(controllers[i], x, y, itemW, itemH);
                }
            }
            else
            {
                int actualCols = (Rows > 1) ? Mathf.CeilToInt((float)count / Rows) : count;
                float stepX = actualCols > 1 ? (totalWidth - itemW) / (actualCols - 1) : 0f;

                for (int i = 0; i < count; i++)
                {
                    int row = i / actualCols;
                    int col = i % actualCols;
                    float x = actualCols > 1 ? col * stepX : 0f;
                    float y = -row * (itemH + ItemSpace.y);
                    SetChildLayout(controllers[i], x, y, itemW, itemH);
                }
            }
        }

        private Vector2 GetChildSize()
        {
            if (ChildPrefab != null)
            {
                var rt = ChildPrefab.GetComponent<RectTransform>();
                if (rt != null)
                    return rt.rect.size;
            }
            if (controllers.Count > 0)
            {
                var rt = controllers[0].transform as RectTransform;
                if (rt != null)
                    return rt.rect.size;
            }
            return new Vector2(48f, 48f);
        }

        private void SetChildLayout(UIListItemController controller, float x, float y, float width, float height)
        {
            var rt = controller.transform as RectTransform;
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
