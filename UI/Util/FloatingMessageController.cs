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
        public Animator Animator;

        /// <summary>
        /// 消息持续时间（秒），超过此时间后自动回收
        /// </summary>
        public float Duration;

        public void SetContent(FloatingMessageContent content)
        {
            // 设置图标
            if (Icon != null)
            {
                Icon.sprite = content.Icon;
                Icon.gameObject.SetActive(content.Icon != null);
            }

            // 设置主文本
            if (MainText != null)
            {
                MainText.text = content.MainText;
                MainText.color = content.TextColor;
            }

            // 设置副文本
            if (SubText != null)
            {
                SubText.text = content.SubText;
                SubText.color = content.TextColor;
                SubText.gameObject.SetActive(!string.IsNullOrEmpty(content.SubText));
            }
        }

        public override void OnPoolableTaken()
        {
            base.OnPoolableTaken();
            
            // 每次启动时重启动画
            if (Animator != null)
            {
                Animator.Rebind();
                Animator.Update(0f);
            }
        }

        public override void OnPoolableReturned()
        {
            base.OnPoolableReturned();
        }
    }
}