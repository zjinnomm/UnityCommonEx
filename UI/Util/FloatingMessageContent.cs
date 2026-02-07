using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// 浮动消息内容
    /// </summary>
    public struct FloatingMessageContent
    {
        public Sprite Icon;
        public string MainText;
        public string SubText;
        public Color TextColor;
        /// <summary>主文本字号相对缩放，默认1，0.5表示原大小的一半</summary>
        public float MainTextFontSize;
        /// <summary>副文本字号相对缩放，默认1，0.5表示原大小的一半</summary>
        public float SubTextFontSize;
    }
}

