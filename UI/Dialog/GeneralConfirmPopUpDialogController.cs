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
                CancelButton.onClick.AddListener(OnCancelClicked);
            }
            if (BackgroundButton != null)
            {
                BackgroundButton.onClick.AddListener(OnCancelClicked);
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
                CancelButton.onClick.RemoveListener(OnCancelClicked);
            }
            if (BackgroundButton != null)
            {
                BackgroundButton.onClick.RemoveListener(OnCancelClicked);
            }

            base.OnRelease();
        }


        /// <summary>
        /// 显示对话框面板
        /// </summary>
        /// <param name="hint">提示文本</param>
        /// <param name="confirmCallback">确认回调</param>
        /// <param name="rect">目标Rect（屏幕坐标）</param>
        public void ShowDialogPanel(string hint, Action confirmCallback, Rect rect)
        {
            if (DialogPanel == null) return;

            currentConfirmCallback = confirmCallback;

            // 更新提示文本
            if (HintText != null)
            {
                HintText.text = hint ?? string.Empty;
            }

            // 计算并设置位置
            SetPanelPosition(rect);

            // 显示对话框
            DialogPanel.gameObject.SetActive(true);
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
            currentConfirmCallback = null;
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
        private DisplaySide DetermineDisplaySide(Rect rect)
        {
            // 获取 Panel 的尺寸
            Vector2 panelSize = GetPanelSize();

            // 如果 rect 的 x1 > w' + padding，说明可以显示在左边
            if (rect.x > panelSize.x + Padding)
            {
                return DisplaySide.Left;
            }
            else
            {
                return DisplaySide.Right;
            }
        }

        /// <summary>
        /// 确定对齐方式（上边缘或下边缘）
        /// </summary>
        private DisplayAlign DetermineDisplayAlign(Rect rect)
        {
            // 获取 Panel 的尺寸
            Vector2 panelSize = GetPanelSize();

            // rect.y2 = rect.y + rect.height（屏幕坐标系，y 从下往上）
            // 如果 rect.y2 > h' + padding，说明可以上边缘对齐
            float rectTop = rect.y + rect.height;
            if (rectTop > panelSize.y + Padding)
            {
                return DisplayAlign.Top;
            }
            else
            {
                return DisplayAlign.Bottom;
            }
        }

        /// <summary>
        /// 获取 Panel 的尺寸
        /// </summary>
        private Vector2 GetPanelSize()
        {
            if (DialogPanel == null)
            {
                return Vector2.zero;
            }

            Vector2 panelSize = DialogPanel.sizeDelta;
            if (panelSize.x == 0 || panelSize.y == 0)
            {
                // 如果尺寸为0，尝试获取布局后的尺寸
                Canvas.ForceUpdateCanvases();
                panelSize = DialogPanel.rect.size;
            }

            return panelSize;
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

            // 确定显示位置（左边或右边，上边缘或下边缘对齐）
            DisplaySide side = DetermineDisplaySide(rect);
            DisplayAlign align = DetermineDisplayAlign(rect);

            // 获取 Panel 的尺寸
            Vector2 panelSize = GetPanelSize();

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

            // 将屏幕坐标转换为 Canvas 本地坐标
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreenPos, canvas.worldCamera, out localPos);
            DialogPanel.anchoredPosition = localPos;
        }

        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        private void OnConfirmClicked()
        {
            // 执行确认回调
            if (currentConfirmCallback != null)
            {
                currentConfirmCallback.Invoke();
            }

            // 隐藏对话框并关闭
            HideDialog();
            Deactivate();
        }

        /// <summary>
        /// 取消按钮点击事件（也用于背景按钮）
        /// </summary>
        private void OnCancelClicked()
        {
            // 隐藏对话框并关闭（不执行确认回调）
            HideDialog();
            Deactivate();
        }
    }
}
