using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnityCommonEx
{
    public class FloatingMessageController : BasePoolableController
    {
        public Image Icon;
        public TMP_Text MainText;
        public TMP_Text SubText;
        
        /// <summary>
        /// UITween 配置
        /// </summary>
        public UITweenConfig TweenConfig;
        
        /// <summary>
        /// UITween 操作的目标 RectTransform
        /// </summary>
        public RectTransform TweenRoot;

        /// <summary>
        /// 消息持续时间（秒），超过此时间后自动回收
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// 当前运行的 Tween ID
        /// </summary>
        private uint currentTweenId;

        private float defaultMainFontSize;
        private float defaultSubFontSize;

        protected override void OnInit()
        {
            base.OnInit();
            if (MainText != null)
                defaultMainFontSize = MainText.fontSize;
            if (SubText != null)
                defaultSubFontSize = SubText.fontSize;
        }

        public void SetContent(FloatingMessageContent content)
        {
            // 设置图标
            if (Icon != null)
            {
                Icon.sprite = content.Icon;
                Icon.gameObject.SetActive(content.Icon != null);
            }

            float mainScale = content.MainTextFontSize <= 0f ? 1f : content.MainTextFontSize;
            float subScale = content.SubTextFontSize <= 0f ? 1f : content.SubTextFontSize;

            // 设置主文本
            if (MainText != null)
            {
                MainText.text = content.MainText;
                MainText.color = content.TextColor;
                if (defaultMainFontSize > 0f)
                    MainText.fontSize = defaultMainFontSize * mainScale;
            }

            // 设置副文本
            if (SubText != null)
            {
                SubText.text = content.SubText;
                SubText.color = content.TextColor;
                SubText.gameObject.SetActive(!string.IsNullOrEmpty(content.SubText));
                if (defaultSubFontSize > 0f)
                    SubText.fontSize = defaultSubFontSize * subScale;
            }
        }

        public override void OnPoolableTaken()
        {
            base.OnPoolableTaken();
        }
        
        /// <summary>
        /// 启动 UITween 动画
        /// </summary>
        public void StartTween()
        {
            // 停止之前的 Tween（如果存在）
            if (currentTweenId != 0)
            {
                UITweenManager.Instance.StopTween(currentTweenId, false);
                currentTweenId = 0;
            }
            
            // 启动 UITween
            if (TweenRoot != null)
            {
                UITweenObject tweenObj = new UITweenObject
                {
                    TargetTransform = TweenRoot,
                    GetTargetAlphaFunc = GetAlpha,
                    SetTargetAlphaFunc = SetAlpha,
                    OnTweenFinished = null
                };
                
                currentTweenId = UITweenManager.Instance.StartTween(tweenObj, TweenConfig);
            }
        }

        public override void OnPoolableReturned()
        {
            base.OnPoolableReturned();
            
            // 停止 UITween
            if (currentTweenId != 0)
            {
                UITweenManager.Instance.StopTween(currentTweenId, false);
                currentTweenId = 0;
            }
        }
        
        private float GetAlpha()
        {
            if (MainText != null)
            {
                return MainText.color.a;
            }
            return 1f;
        }
        
        private void SetAlpha(float alpha)
        {
            Color color;
            if (Icon != null)
            {
                color = Icon.color;
                color.a = alpha;
                Icon.color = color;
            }
            if (MainText != null)
            {
                color = MainText.color;
                color.a = alpha;
                MainText.color = color;
            }
            if (SubText != null)
            {
                color = SubText.color;
                color.a = alpha;
                SubText.color = color;
            }
        }
    }
}