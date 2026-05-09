using System;

namespace UnityCommonEx
{
    /// <summary>
    /// Toggle 类型设置项 UI 的抽象基类。
    /// </summary>
    public abstract class ToggleSettingItemUIController : NodeController
    {
        /// <summary>
        /// 当开关状态变化时触发（仅用户交互产生的变化应触发）
        /// </summary>
        public event Action<bool> OnValueChanged;

        public abstract void SetConfig(ToggleGameSettingFieldConfig config);

        /// <summary>
        /// 设置当前值（不应触发 OnValueChanged）
        /// </summary>
        public abstract void SetValue(bool value);

        /// <summary>
        /// 获取当前值
        /// </summary>
        public abstract bool GetValue();

        protected void RaiseValueChanged(bool value)
        {
            OnValueChanged?.Invoke(value);
        }
    }
}
