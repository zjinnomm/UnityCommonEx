using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public class InputBindingSettingItemUIController : NodeController
    {
        public Button[] BindingButtons;
        public Image[] BindingImages;
        public InputBindingIconResourceConfig IconResourceConfig;

        private InputBindingGameSettingFieldConfig _config;
        private IInputBindingSettingsManager _manager;
        private int _lastDisplayRevision = -1;

        private void Update()
        {
            if (_manager == null || _config == null)
                return;

            bool handledInput = _manager.TryFinishActiveRebindThisFrame(out _);
            if (handledInput || _lastDisplayRevision != _manager.DisplayRevision)
            {
                RefreshBindings();
                _lastDisplayRevision = _manager.DisplayRevision;
            }
        }

        private void OnDisable()
        {
            ClearButtonListeners();
        }

        public void SetConfig(InputBindingGameSettingFieldConfig config, IInputBindingSettingsManager manager)
        {
            _config = config;
            _manager = manager;
            _lastDisplayRevision = -1;

            RebindButtons();
            RefreshBindings();
            if (_manager != null)
                _lastDisplayRevision = _manager.DisplayRevision;
        }

        private void RebindButtons()
        {
            ClearButtonListeners();

            int maxSlots = GetSlotCapacity();
            for (int i = 0; i < maxSlots; i++)
            {
                Button button = GetButton(i);
                if (button == null)
                    continue;

                int slotIndex = i;
                button.onClick.AddListener(() => OnSlotClicked(slotIndex));
            }
        }

        private void RefreshBindings()
        {
            if (_config == null || _manager == null)
                return;

            int maxSlots = GetSlotCapacity();
            IReadOnlyList<InputBinding> bindings = _manager.GetPendingBindings(_config.Action);
            int visibleSlotCount = bindings != null ? bindings.Count : 0;
            for (int i = 0; i < maxSlots; i++)
            {
                Button button = GetButton(i);
                Image image = GetImage(i);
                bool visible = i < visibleSlotCount;

                if (button != null)
                    button.gameObject.SetActive(visible);
                if (image != null)
                    image.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                InputBinding binding = i < bindings.Count ? bindings[i] : null;
                ApplyBindingVisual(button, image, binding, _manager.IsRebindingTarget(_config.Action, i));
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (_config == null || _manager == null)
                return;

            IReadOnlyList<InputBinding> bindings = _manager.GetPendingBindings(_config.Action);
            InputBinding binding = slotIndex < bindings.Count ? bindings[slotIndex] : null;
            if (binding != null && binding.DeviceType != InputBindingDeviceType.KeyboardKey)
                return;

            _manager.BeginRebind(_config.Action, slotIndex);
            RefreshBindings();
        }

        private void ApplyBindingVisual(Button button, Image image, InputBinding binding, bool isListening)
        {
            if (button != null)
                button.interactable = binding == null || binding.DeviceType == InputBindingDeviceType.KeyboardKey;

            if (image == null)
                return;

            if (binding == null)
            {
                image.sprite = null;
                image.enabled = false;
                return;
            }

            InputBindingIconResourceConfig.Entry entry =
                IconResourceConfig != null ? IconResourceConfig.GetEntry(binding.GetBindingKey()) : null;

            image.sprite = entry != null ? entry.Sprite : null;
            image.enabled = image.sprite != null;

            RectTransform rect = image.rectTransform;
            Vector2 size = rect.sizeDelta;
            if (image.sprite != null)
            {
                Rect spriteRect = image.sprite.rect;
                float aspect = spriteRect.height > 0f ? spriteRect.width / spriteRect.height : 1f;
                size.x = size.y * aspect;
            }
            rect.sizeDelta = size;

            Color color = image.color;
            color.a = isListening ? 0.5f : 1f;
            image.color = color;
        }

        private int GetSlotCapacity()
        {
            return Mathf.Max(BindingButtons != null ? BindingButtons.Length : 0, BindingImages != null ? BindingImages.Length : 0);
        }

        private Button GetButton(int index)
        {
            return BindingButtons != null && index >= 0 && index < BindingButtons.Length ? BindingButtons[index] : null;
        }

        private Image GetImage(int index)
        {
            return BindingImages != null && index >= 0 && index < BindingImages.Length ? BindingImages[index] : null;
        }

        private void ClearButtonListeners()
        {
            if (BindingButtons == null)
                return;

            for (int i = 0; i < BindingButtons.Length; i++)
            {
                if (BindingButtons[i] != null)
                    BindingButtons[i].onClick.RemoveAllListeners();
            }
        }
    }
}
