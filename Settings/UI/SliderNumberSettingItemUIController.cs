using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    /// <summary>
    /// Slider + 数字文本显示的 Number 设置项实现。
    ///
    /// 结构约定：
    /// - Slider：范围与交互来源
    /// - NumberText：显示当前数值
    /// </summary>
    public class SliderNumberSettingItemUIController : NumberSettingItemUIController
    {
        public Slider Slider;
        public TMP_Text NumberText;

        private NumberGameSettingFieldConfig _config;
        private bool _suppressCallback;
        private float _value;

        protected override void OnInit()
        {
            base.OnInit();
            if (Slider != null)
            {
                Slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            RefreshUI();
        }

        protected override void OnRelease()
        {
            if (Slider != null)
            {
                Slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            base.OnRelease();
        }

        public override void SetConfig(NumberGameSettingFieldConfig config)
        {
            _config = config;
            ApplySliderConfig();
            _value = Snap(Clamp(_value));
            RefreshUI();
        }

        public override void SetValue(float value)
        {
            _value = Snap(Clamp(value));
            RefreshUI();
        }

        public override float GetValue()
        {
            return _value;
        }

        private void ApplySliderConfig()
        {
            if (Slider == null || _config == null)
                return;

            float min = _config.Min;
            float max = _config.Max;
            if (max < min)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }

            Slider.minValue = min;
            Slider.maxValue = max;
        }

        private void OnSliderValueChanged(float raw)
        {
            if (_suppressCallback)
                return;

            float v = Snap(Clamp(raw));
            _value = v;
            RefreshUI(updateSlider: true);
            RaiseValueChanged(_value);
        }

        private void RefreshUI(bool updateSlider = false)
        {
            if (NumberText != null)
            {
                NumberText.text = FormatNumber(_value);
            }

            if (Slider != null)
            {
                if (updateSlider || !Mathf.Approximately(Slider.value, _value))
                {
                    _suppressCallback = true;
                    Slider.value = _value;
                    _suppressCallback = false;
                }
            }
        }

        private float Clamp(float v)
        {
            if (_config == null)
                return v;

            float min = _config.Min;
            float max = _config.Max;
            if (max < min)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }
            return Mathf.Clamp(v, min, max);
        }

        private float Snap(float v)
        {
            if (_config == null)
                return v;

            float step = _config.Step;
            if (step <= 0f)
                return v;

            float min = _config.Min;
            float t = (v - min) / step;
            float snapped = min + Mathf.Round(t) * step;
            return snapped;
        }

        private string FormatNumber(float v)
        {
            if (_config == null)
                return v.ToString("0.##");

            float step = _config.Step;
            if (step >= 1f && Mathf.Approximately(step, Mathf.Round(step)))
                return Mathf.RoundToInt(v).ToString();

            return v.ToString("0.##");
        }
    }
}

