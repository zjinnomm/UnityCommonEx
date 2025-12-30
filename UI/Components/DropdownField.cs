using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public class DropdownField : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_InputField inputField;
        public TMP_Dropdown dropdown;

        private void Awake()
        {
            // 确保组件引用存在
            if (inputField == null)
            {
                inputField = GetComponentInChildren<TMP_InputField>();
            }
            if (dropdown == null)
            {
                dropdown = GetComponentInChildren<TMP_Dropdown>();
            }

            // 绑定下拉框选择改变事件
            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }

        private void OnDestroy()
        {
            // 清理事件绑定
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }

        private void OnDropdownValueChanged(int index)
        {
            if (inputField != null && dropdown != null && index >= 0 && index < dropdown.options.Count)
            {
                inputField.text = dropdown.options[index].text;
            }
        }

        #region Public Interface - Dropdown Delegation

        /// <summary>
        /// 清除下拉框选项
        /// </summary>
        public void ClearOptions()
        {
            if (dropdown != null)
            {
                dropdown.ClearOptions();
            }
        }

        /// <summary>
        /// 添加选项到下拉框
        /// </summary>
        /// <param name="options">选项列表</param>
        public void AddOptions(List<string> options)
        {
            if (dropdown != null)
            {
                dropdown.AddOptions(options);
            }
        }

        /// <summary>
        /// 添加选项到下拉框
        /// </summary>
        /// <param name="options">选项列表</param>
        public void AddOptions(List<TMP_Dropdown.OptionData> options)
        {
            if (dropdown != null)
            {
                dropdown.AddOptions(options);
            }
        }

        /// <summary>
        /// 获取或设置下拉框当前选中的索引
        /// </summary>
        public int value
        {
            get => dropdown != null ? dropdown.value : -1;
            set
            {
                if (dropdown != null)
                {
                    dropdown.value = value;
                }
            }
        }

        #endregion

        #region Public Interface - InputField Delegation

        /// <summary>
        /// 获取或设置输入框的文本内容
        /// </summary>
        public string text
        {
            get => inputField != null ? inputField.text : string.Empty;
            set
            {
                if (inputField != null)
                {
                    inputField.text = value;
                }
            }
        }

        #endregion
    }
} 