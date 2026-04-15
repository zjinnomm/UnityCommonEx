using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnityCommonEx
{
    /// <summary>
    /// 通用弹出对话框显示参数（最多三枚操作键 + 背景回调）
    /// </summary>
    public struct GeneralPopUpDialogConfig
    {
        public string HintText;
        public Rect TargetRect;
        public string Button1Text;
        public string Button2Text;
        public string Button3Text;
        public Action Button1Callback;
        public Action Button2Callback;
        public Action Button3Callback;
        public Action BackgroundCallback;
    }

    /// <summary>
    /// 通用弹出对话框控制器（最多三枚按钮）
    /// </summary>
    public class GeneralPopUpDialogController : NodeController
    {
        public RectTransform DialogPanel;
        public TMP_Text HintText;
        /// <summary>左 / 中 / 右 与 Button1..3 对应</summary>
        public Button[] Buttons;
        /// <summary>与 Buttons 一一对应；可为空或未赋值，则从按钮子物体解析 TMP_Text</summary>
        public TMP_Text[] ButtonTexts;
        public Button BackgroundButton;
        public float Padding = 10f;
        public UITweenConfig ShowTween;
        public RectTransform TweenRoot;

        private const int ButtonSlotCount = 3;
        private readonly Action[] currentButtonCallbacks = new Action[ButtonSlotCount];
        private Action currentBackgroundCallback;
        private readonly UnityEngine.Events.UnityAction[] buttonClickActions = new UnityEngine.Events.UnityAction[ButtonSlotCount];

        protected override void OnInit()
        {
            base.OnInit();

            if (Buttons != null)
            {
                for (int i = 0; i < Buttons.Length && i < ButtonSlotCount; i++)
                {
                    int index = i;
                    if (Buttons[i] == null)
                        continue;
                    if (buttonClickActions[index] == null)
                        buttonClickActions[index] = () => OnButtonClicked(index);
                    Buttons[i].onClick.AddListener(buttonClickActions[index]);
                }
            }

            if (BackgroundButton != null)
                BackgroundButton.onClick.AddListener(OnBackgroundClicked);

            HideDialog();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            PlayShowTween();
        }

        protected override void OnRelease()
        {
            if (Buttons != null)
            {
                for (int i = 0; i < Buttons.Length && i < ButtonSlotCount; i++)
                {
                    if (Buttons[i] != null && buttonClickActions[i] != null)
                        Buttons[i].onClick.RemoveListener(buttonClickActions[i]);
                }
            }

            if (BackgroundButton != null)
                BackgroundButton.onClick.RemoveListener(OnBackgroundClicked);

            base.OnRelease();
        }

        /// <summary>
        /// 显示对话框：无文案的按钮槽隐藏；回调可省略。
        /// </summary>
        public void ShowDialogPanel(in GeneralPopUpDialogConfig config)
        {
            if (DialogPanel == null)
                return;

            currentButtonCallbacks[0] = config.Button1Callback;
            currentButtonCallbacks[1] = config.Button2Callback;
            currentButtonCallbacks[2] = config.Button3Callback;
            currentBackgroundCallback = config.BackgroundCallback;

            if (HintText != null)
                HintText.text = config.HintText ?? string.Empty;

            ApplyButtonSlot(0, config.Button1Text);
            ApplyButtonSlot(1, config.Button2Text);
            ApplyButtonSlot(2, config.Button3Text);

            DialogPanel.gameObject.SetActive(true);
            SetPanelPosition(config.TargetRect);
        }

        private void ApplyButtonSlot(int index, string labelText)
        {
            if (Buttons == null || index < 0 || index >= Buttons.Length)
                return;
            Button b = Buttons[index];
            if (b == null)
                return;

            bool visible = !string.IsNullOrEmpty(labelText);
            b.gameObject.SetActive(visible);
            if (!visible)
                return;

            TMP_Text tmp = ResolveButtonLabel(index);
            if (tmp != null)
                tmp.text = labelText;
        }

        private TMP_Text ResolveButtonLabel(int index)
        {
            if (ButtonTexts != null && index < ButtonTexts.Length && ButtonTexts[index] != null)
                return ButtonTexts[index];
            if (Buttons != null && index < Buttons.Length && Buttons[index] != null)
                return Buttons[index].GetComponentInChildren<TMP_Text>(true);
            return null;
        }

        private void OnButtonClicked(int index)
        {
            HideDialog();
            Deactivate();
            if (index >= 0 && index < ButtonSlotCount && currentButtonCallbacks[index] != null)
                currentButtonCallbacks[index].Invoke();
        }

        private void OnBackgroundClicked()
        {
            HideDialog();
            Deactivate();
            if (currentBackgroundCallback != null)
                currentBackgroundCallback.Invoke();
        }

        private void PlayShowTween()
        {
            if (ShowTween.PropType == 0 || TweenRoot == null)
                return;

            UITweenObject tweenObj = new UITweenObject
            {
                TargetTransform = TweenRoot,
                GetTargetAlphaFunc = null,
                SetTargetAlphaFunc = null,
                OnTweenFinished = null
            };
            UITweenManager.Instance.StartTween(tweenObj, ShowTween);
        }

        public void HideDialog()
        {
            if (DialogPanel != null)
                DialogPanel.gameObject.SetActive(false);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            HideDialog();
        }

        private enum DisplaySide
        {
            Left,
            Right
        }

        private enum DisplayAlign
        {
            Top,
            Bottom
        }

        private DisplaySide DetermineDisplaySide(Rect rect, Vector2 panelScreenPixelSize)
        {
            if (rect.x > panelScreenPixelSize.x + Padding)
                return DisplaySide.Left;
            return DisplaySide.Right;
        }

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

        private void SetPanelPosition(Rect rect)
        {
            Canvas canvas = DialogPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
                return;

            RefreshDialogPanelLayout();

            Vector2 panelScreenSize = GetDialogPanelScreenPixelSize(canvas);
            DisplaySide side = DetermineDisplaySide(rect, panelScreenSize);
            DisplayAlign align = DetermineDisplayAlign(rect, panelScreenSize);

            Camera eventCam = null;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                eventCam = canvas.worldCamera ?? Camera.main;

            Vector2 targetScreenPos = Vector2.zero;

            if (side == DisplaySide.Left)
            {
                targetScreenPos.x = rect.x - Padding;
                DialogPanel.pivot = new Vector2(1f, 0.5f);
            }
            else
            {
                targetScreenPos.x = rect.x + rect.width + Padding;
                DialogPanel.pivot = new Vector2(0f, 0.5f);
            }

            if (align == DisplayAlign.Top)
            {
                targetScreenPos.y = rect.y + rect.height;
                DialogPanel.pivot = new Vector2(DialogPanel.pivot.x, 1f);
            }
            else
            {
                targetScreenPos.y = rect.y;
                DialogPanel.pivot = new Vector2(DialogPanel.pivot.x, 0f);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreenPos, eventCam, out Vector2 localPos);
            DialogPanel.anchoredPosition = localPos;

            ClampDialogPanelToScreenEdges(eventCam);
        }

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
    }
}
