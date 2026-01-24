using System;
using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// Select 类型设置项 UI 的抽象基类。
    /// 
    /// 只约束接口，不关心具体表现形式（下拉框 / Spinner / 单选按钮组等），
    /// 具体样式由各自子类实现。
    /// </summary>
    public abstract class SelectSettingItemUIController : NodeController
    {
        /// <summary>
        /// 当前可选项列表（由上层传入，用于回调时做边界检查）
        /// </summary>
        protected string[] _options = Array.Empty<string>();

        /// <summary>
        /// 当选中索引变化时触发（仅用户交互产生的变化应触发）
        /// 参数为新的选中索引
        /// </summary>
        public event Action<int> OnSelectedIndexChanged;

        /// <summary>
        /// 设置可选项列表
        /// </summary>
        public abstract void SetOptions(string[] options);

        /// <summary>
        /// 设置当前选中索引（不应触发 OnSelectedIndexChanged）
        /// </summary>
        public abstract void SetSelectedIndex(int index);

        /// <summary>
        /// 获取当前选中索引
        /// </summary>
        public abstract int GetSelectedIndex();

        /// <summary>
        /// 由子类在用户交互导致选中项变化时调用，用于广播事件
        /// </summary>
        /// <param name="index">新的选中索引</param>
        protected void RaiseSelectedIndexChanged(int index)
        {
            OnSelectedIndexChanged?.Invoke(index);
        }
    }
}

