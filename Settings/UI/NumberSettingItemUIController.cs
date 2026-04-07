using System;

namespace UnityCommonEx
{
    /// <summary>
    /// Number 类型设置项 UI 的抽象基类。
    /// 约束最小接口：接收配置、设置当前值、在用户交互产生变化时抛事件。
    /// </summary>
    public abstract class NumberSettingItemUIController : NodeController
    {
        /// <summary>
        /// 当数值变化时触发（仅用户交互产生的变化应触发）
        /// 参数为新的数值（float）
        /// </summary>
        public event Action<float> OnValueChanged;

        public abstract void SetConfig(NumberGameSettingFieldConfig config);

        /// <summary>
        /// 设置当前值（不应触发 OnValueChanged）
        /// </summary>
        public abstract void SetValue(float value);

        /// <summary>
        /// 获取当前值
        /// </summary>
        public abstract float GetValue();

        protected void RaiseValueChanged(float value)
        {
            OnValueChanged?.Invoke(value);
        }
    }
}

