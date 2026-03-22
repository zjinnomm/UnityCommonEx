using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityCommonEx;

namespace UnityCommonEx
{
    /// <summary>
    /// 通用确认弹出对话框控制器
    /// </summary>
    public class GeneralConfirmPopUpDialogController : NodeController
    {
        /// <summary>
        /// 对话框面板
        /// </summary>
        public RectTransform DialogPanel;

        /// <summary>
        /// 提示文本
        /// </summary>
        public TMP_Text HintText;

        /// <summary>
        /// 确认按钮
        /// </summary>
        public Button ConfirmButton;

        /// <summary>
        /// 取消按钮
        /// </summary>
        public Button CancelButton;

        /// <summary>
        /// 背景按钮（点击背景关闭对话框）
        /// </summary>
        public Button BackgroundButton;

        /// <summary>
        /// 与目标Rect之间的间距
        /// </summary>
        public float Padding = 10f;

        /// <summary>
        /// 显示动画配置
        /// </summary>
        public UITweenConfig ShowTween;

        /// <summary>
        /// Tween 操作对象
        /// </summary>
        public RectTransform TweenRoot;

        /// <summary>
        /// 当前确认回调
        /// </summary>
        private Action currentConfirmCallback;

        /// <summary>
        /// 当前取消按钮回调（点击取消按钮时调用）
        /// </summary>
        private Action currentCancelCallback;

        /// <summary>
        /// 当前点击背景回调（点击背景关闭时调用，若为 null 则与取消按钮行为一致）
        /// </summary>
        private Action currentBackgroundCallback;

        protected override void OnInit()
        {
            base.OnInit();

            // 绑定按钮事件
            if (ConfirmButton != null)
            {
                ConfirmButton.onClick.AddListener(OnConfirmClicked);
            }
            if (CancelButton != null)
            {
                CancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            if (BackgroundButton != null)
            {
                BackgroundButton.onClick.AddListener(OnBackgroundClicked);
            }

            // 初始状态隐藏对话框
            HideDialog();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            // 激活时播放显示动画
            PlayShowTween();
        }

        protected override void OnRelease()
        {
            // 取消绑定按钮事件
            if (ConfirmButton != null)
            {
                ConfirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
            if (CancelButton != null)
            {
                CancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
            if (BackgroundButton != null)
            {
                BackgroundButton.onClick.RemoveListener(OnBackgroundClicked);
            }

            base.OnRelease();
        }


        /// <summary>
        /// 显示对话框面板
        /// </summary>
        /// <param name="hint">提示文本</param>
        /// <param name="confirmCallback">确认回调</param>
        /// <param name="rect">目标Rect（屏幕坐标）</param>
        /// <param name="cancelCallback">取消回调（点击取消按钮时调用，可选）</param>
        /// <param name="backgroundCallback">点击背景时的回调（可选；为 null 时与取消按钮同用 cancelCallback）</param>
        public void ShowDialogPanel(string hint, Action confirmCallback, Rect rect, Action cancelCallback = null, Action backgroundCallback = null)
        {
            if (DialogPanel == null) return;

            currentConfirmCallback = confirmCallback;
            currentCancelCallback = cancelCallback;
            currentBackgroundCallback = backgroundCallback;

            // 更新提示文本
            if (HintText != null)
            {
                HintText.text = hint ?? string.Empty;
            }

            // 显示后再算布局与位置（与 DetailPanel 一致：屏幕像素 rect 与面板尺寸同坐标系）
            DialogPanel.gameObject.SetActive(true);
            SetPanelPosition(rect);
        }

        /// <summary>
        /// 播放显示动画
        /// </summary>
        private void PlayShowTween()
        {
            // 检查是否配置了动画
            if (ShowTween.PropType == 0)
            {
                return; // 没有配置动画
            }

            if (TweenRoot == null)
            {
                return;
            }

            // 创建 Tween 对象
            UITweenObject tweenObj = new UITweenObject
            {
                TargetTransform = TweenRoot,
                GetTargetAlphaFunc = null,
                SetTargetAlphaFunc = null,
                OnTweenFinished = null
            };

            // 启动动画
            UITweenManager.Instance.StartTween(tweenObj, ShowTween);
        }

        /// <summary>
        /// 隐藏对话框
        /// </summary>
        public void HideDialog()
        {
            if (DialogPanel != null)
            {
                DialogPanel.gameObject.SetActive(false);
            }
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            // 确保面板被隐藏
            HideDialog();
        }

        /// <summary>
        /// 显示位置枚举
        /// </summary>
        private enum DisplaySide
        {
            Left,   // 左边
            Right   // 右边
        }

        /// <summary>
        /// 对齐方式枚举
        /// </summary>
        private enum DisplayAlign
        {
            Top,    // 上边缘对齐
            Bottom  // 下边缘对齐
        }

        /// <summary>
        /// 确定显示侧边（左边或右边）
        /// </summary>
        private DisplaySide DetermineDisplaySide(Rect rect, Vector2 panelScreenPixelSize)
        {
            if (rect.x > panelScreenPixelSize.x + Padding)
                return DisplaySide.Left;
            return DisplaySide.Right;
        }

        /// <summary>
        /// 确定对齐方式（上边缘或下边缘）
        /// </summary>
        private DisplayAlign DetermineDisplayAlign(Rect rect, Vector2 panelScreenPixelSize)
        {
            float rectTop = rect.y + rect.height;
            if (rectTop > panelScreenPixelSize.y + Padding)
                return DisplayAlign.Top;
            return DisplayAlign.Bottom;
        }

        private void RefreshDialogPanelLayout()
        {
            if (DialogPanel == null)
                return;

            Canvas.ForceUpdateCanvases();
            var rects = DialogPanel.GetComponentsInChildren<RectTransform>(true);
            for (int i = rects.Length - 1; i >= 0; i--)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rects[i]);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// DialogPanel 在当前画布下的屏幕像素尺寸
        /// </summary>
        private Vector2 GetDialogPanelScreenPixelSize(Canvas canvas)
        {
            if (DialogPanel == null)
                return Vector2.zero;

            Vector3[] corners = new Vector3[4];
            DialogPanel.GetWorldCorners(corners);
            Camera cam = null;
            if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace))
                cam = canvas.worldCamera ?? Camera.main;

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return new Vector2(max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// 设置Panel位置
        /// </summary>
        private void SetPanelPosition(Rect rect)
        {
            Canvas canvas = DialogPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                return;
            }

            RefreshDialogPanelLayout();

            Vector2 panelScreenSize = GetDialogPanelScreenPixelSize(canvas);
            DisplaySide side = DetermineDisplaySide(rect, panelScreenSize);
            DisplayAlign align = DetermineDisplayAlign(rect, panelScreenSize);

            Camera eventCam = null;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                eventCam = canvas.worldCamera ?? Camera.main;

            // 计算目标位置（屏幕坐标）
            Vector2 targetScreenPos = Vector2.zero;

            // 根据侧边决定 X 坐标
            if (side == DisplaySide.Left)
            {
                // 显示在左边：rect.x1 - w' - padding
                targetScreenPos.x = rect.x - Padding;
                // 设置 Pivot 为右中（右边对齐 rect）
                DialogPanel.pivot = new Vector2(1f, 0.5f);
            }
            else
            {
                // 显示在右边：rect.x2 + padding
                targetScreenPos.x = rect.x + rect.width + Padding;
                // 设置 Pivot 为左中（左边对齐 rect）
                DialogPanel.pivot = new Vector2(0f, 0.5f);
            }

            // 根据对齐方式决定 Y 坐标
            if (align == DisplayAlign.Top)
            {
                // 上边缘对齐：rect.y2（rect.y + rect.height）
                targetScreenPos.y = rect.y + rect.height;
                // 调整 Pivot 的 Y 为 1（上边缘对齐，Panel 的上边缘对齐 rect 的上边缘）
                DialogPanel.pivot = new Vector2(DialogPanel.pivot.x, 1f);
            }
            else
            {
                // 下边缘对齐：rect.y1（rect.y）
                targetScreenPos.y = rect.y;
                // 调整 Pivot 的 Y 为 0（下边缘对齐，Panel 的下边缘对齐 rect 的下边缘）
                DialogPanel.pivot = new Vector2(DialogPanel.pivot.x, 0f);
            }

            // 将屏幕坐标转换为 Canvas 根节点本地坐标
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreenPos, eventCam, out localPos);
            DialogPanel.anchoredPosition = localPos;

            ClampDialogPanelToScreenEdges(eventCam);
        }

        /// <summary>
        /// 将已定位的 Panel 整体平移，使屏幕像素包围盒落在安全区内。
        /// </summary>
        private void ClampDialogPanelToScreenEdges(Camera eventCam)
        {
            if (DialogPanel == null)
                return;

            RectTransform parentRt = DialogPanel.parent as RectTransform;
            if (parentRt == null)
                return;

            const float edgePad = 4f;
            float minX = edgePad;
            float maxX = Screen.width - edgePad;
            float minY = edgePad;
            float maxY = Screen.height - edgePad;

            Vector3[] corners = new Vector3[4];
            DialogPanel.GetWorldCorners(corners);
            float sMinX = float.PositiveInfinity, sMaxX = float.NegativeInfinity;
            float sMinY = float.PositiveInfinity, sMaxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(eventCam, corners[i]);
                if (sp.x < sMinX) sMinX = sp.x;
                if (sp.x > sMaxX) sMaxX = sp.x;
                if (sp.y < sMinY) sMinY = sp.y;
                if (sp.y > sMaxY) sMaxY = sp.y;
            }

            float dx = 0f, dy = 0f;
            if (sMinX < minX) dx = minX - sMinX;
            else if (sMaxX > maxX) dx = maxX - sMaxX;
            if (sMinY < minY) dy = minY - sMinY;
            else if (sMaxY > maxY) dy = maxY - sMaxY;

            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
                return;

            Vector2 screenRef = RectTransformUtility.WorldToScreenPoint(eventCam, corners[0]);
            Vector2 screenRef2 = screenRef + new Vector2(dx, dy);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screenRef, eventCam, out Vector2 local0))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screenRef2, eventCam, out Vector2 local1))
                return;

            DialogPanel.anchoredPosition += local1 - local0;
        }

        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        private void OnConfirmClicked()
        {
            // 隐藏对话框并关闭
            HideDialog();
            Deactivate();
            // 执行确认回调
            if (currentConfirmCallback != null)
            {
                currentConfirmCallback.Invoke();
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void OnCancelButtonClicked()
        {
            HideDialog();
            Deactivate();
            if (currentCancelCallback != null)
            {
                currentCancelCallback.Invoke();
            }
        }

        /// <summary>
        /// 背景点击事件（点击别处关闭）
        /// </summary>
        private void OnBackgroundClicked()
        {
            HideDialog();
            Deactivate();
            if (currentBackgroundCallback != null)
            {
                currentBackgroundCallback.Invoke();
            }
        }
    }
}
