using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// 使用 <see cref="CapsuleSwitchToggle"/> 实现的 Toggle 设置项。
    /// </summary>
    public class CapsuleSwitchToggleSettingItemUIController : ToggleSettingItemUIController
    {
        public CapsuleSwitchToggle Toggle;

        private ToggleGameSettingFieldConfig _config;

        protected override void OnInit()
        {
            base.OnInit();

            if (Toggle == null)
            {
                Toggle = GetComponent<CapsuleSwitchToggle>();
            }

            if (Toggle != null)
            {
                Toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        protected override void OnRelease()
        {
            if (Toggle != null)
            {
                Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            base.OnRelease();
        }

        public override void SetConfig(ToggleGameSettingFieldConfig config)
        {
            _config = config;
        }

        public override void SetValue(bool value)
        {
            if (Toggle != null)
            {
                Toggle.SetIsOnWithoutNotify(value);
            }
        }

        public override bool GetValue()
        {
            if (Toggle == null)
            {
                return _config != null && _config.Default;
            }

            return Toggle.isOn;
        }

        private void OnToggleValueChanged(bool value)
        {
            RaiseValueChanged(value);
        }
    }
}
