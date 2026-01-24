using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    /// <summary>
    /// 使用左右按钮切换选项的 Spinner 风格 Select 实现。
    /// 
    /// 结构约定：
    /// - 根节点：行容器（带 RectTransform），作为整个控件的根
    /// - LeftButton / RightButton：切换前后选项
    /// - OptionText：显示当前选中项文本
    /// </summary>
    public class SpinnerSelectSettingItemUIController : SelectSettingItemUIController
    {
        public Button LeftButton;
        public Button RightButton;
        public TMP_Text OptionText;

        private int _selectedIndex = 0;

        /// <summary>
        /// 初始化生命周期（NodeController 版本）
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            if (LeftButton != null)
            {
                LeftButton.onClick.AddListener(OnLeftClicked);
            }
            if (RightButton != null)
            {
                RightButton.onClick.AddListener(OnRightClicked);
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 释放生命周期（NodeController 版本）
        /// </summary>
        protected override void OnRelease()
        {
            if (LeftButton != null)
            {
                LeftButton.onClick.RemoveListener(OnLeftClicked);
            }
            if (RightButton != null)
            {
                RightButton.onClick.RemoveListener(OnRightClicked);
            }

            base.OnRelease();
        }

        public override void SetOptions(string[] options)
        {
            _options = options ?? Array.Empty<string>();
            // 重置索引到合法范围
            if (_options.Length == 0)
            {
                _selectedIndex = 0;
            }
            else if (_selectedIndex < 0 || _selectedIndex >= _options.Length)
            {
                _selectedIndex = 0;
            }
            UpdateDisplay();
        }

        public override void SetSelectedIndex(int index)
        {
            if (_options.Length == 0)
            {
                _selectedIndex = 0;
                UpdateDisplay();
                return;
            }

            _selectedIndex = Mathf.Clamp(index, 0, _options.Length - 1);
            UpdateDisplay();
        }

        public override int GetSelectedIndex()
        {
            return _selectedIndex;
        }

        private void OnLeftClicked()
        {
            if (_options.Length == 0)
            {
                return;
            }

            // 已经在最左侧，忽略点击
            if (_selectedIndex <= 0)
            {
                return;
            }

            int newIndex = _selectedIndex - 1;
            if (newIndex != _selectedIndex)
            {
                _selectedIndex = newIndex;
                UpdateDisplay();
                RaiseSelectedIndexChanged(_selectedIndex);
            }
        }

        private void OnRightClicked()
        {
            if (_options.Length == 0)
            {
                return;
            }

            // 已经在最右侧，忽略点击
            if (_selectedIndex >= _options.Length - 1)
            {
                return;
            }

            int newIndex = _selectedIndex + 1;
            if (newIndex != _selectedIndex)
            {
                _selectedIndex = newIndex;
                UpdateDisplay();
                RaiseSelectedIndexChanged(_selectedIndex);
            }
        }

        private void UpdateDisplay()
        {
            // 文本显示
            if (OptionText != null)
            {
                if (_options == null || _options.Length == 0)
                {
                    OptionText.text = string.Empty;
                }
                else
                {
                    int safeIndex = Mathf.Clamp(_selectedIndex, 0, _options.Length - 1);
                    OptionText.text = _options[safeIndex];
                }
            }

            // 左右按钮可用状态：最左禁用 Left，最右禁用 Right
            bool hasOptions = _options != null && _options.Length > 0;
            if (LeftButton != null)
            {
                LeftButton.interactable = hasOptions && _selectedIndex > 0;
            }
            if (RightButton != null)
            {
                RightButton.interactable = hasOptions && _selectedIndex < (_options.Length - 1);
            }
        }
    }
}

