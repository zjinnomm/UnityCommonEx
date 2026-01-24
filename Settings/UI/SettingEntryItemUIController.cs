using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    /// <summary>
    /// 单条设置项的容器：负责承载 Label 和具体的 SettingItem UI（例如 Select）。
    /// 
    /// 结构约定：
    /// - 根节点：一行容器（带 RectTransform），控制整行的布局
    /// - Label：引用到用于显示字段名的文本组件（TMP_Text 或 Text）
    /// - SettingItemRoot：用于承载实际的 SettingItem 控件（如 SpinnerSelectSettingItemUIController）
    /// </summary>
    public class SettingEntryItemUIController : NodeController
    {
        [Header("UI 引用")]
        public TMP_Text LabelTMP;
        public Transform SettingItemRoot;

        /// <summary>
        /// 设置显示名称
        /// </summary>
        public void SetLabel(string displayName)
        {
            if (LabelTMP != null)
            {
                LabelTMP.text = displayName;
            }
        }

        /// <summary>
        /// 获取用于承载 SettingItem 的根 Transform
        /// </summary>
        public Transform GetSettingItemRoot()
        {
            return SettingItemRoot != null ? SettingItemRoot : transform;
        }
    }
}

