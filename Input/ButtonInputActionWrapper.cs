using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnityCommonEx
{
    public abstract class ButtonInputActionWrapper<TAction> : NodeController, IInputActionWrapper<TAction> where TAction : struct, Enum
    {
        public TAction Action;
        public int Priority;
        public bool AllowWhenGamepadSuppressed;
        public Button Button;
        public ButtonSFXPlayer SFXPlayer;
        public Image BindingImage;
        public TMP_Text BindingText;
        public InputBindingIconResourceConfig IconResourceConfig;

        TAction IInputActionWrapper<TAction>.Action => Action;
        int IInputActionWrapper<TAction>.Priority => Priority;

        protected abstract InputManager<TAction> GetInputManager();
        private int lastDisplayRevision = -1;

        private void Update()
        {
            var manager = GetInputManager();
            if (manager == null || lastDisplayRevision == manager.DisplayRevision) return;
            lastDisplayRevision = manager.DisplayRevision;
            RefreshBindingImage(manager);
        }

        private void OnEnable()
        {
            InputManager<TAction> inputManager = GetInputManager();
            inputManager?.RegisterWrapper(this);
            RefreshBindingImage(inputManager);
        }

        private void OnDisable()
        {
            GetInputManager()?.UnregisterWrapper(this);
        }

        public virtual bool CanTrigger()
        {
            return isActiveAndEnabled &&
                gameObject.activeInHierarchy &&
                Button != null &&
                Button.gameObject.activeInHierarchy &&
                Button.interactable;
        }

        public virtual void Trigger()
        {
            if (!CanTrigger())
                return;
            SFXPlayer?.Play();
            Button.onClick.Invoke();
        }

        protected virtual void RefreshBindingImage(InputManager<TAction> inputManager)
        {
            InputBinding binding = inputManager?.GetDisplayBinding(Action);
            InputBindingIconResourceConfig.Entry entry = binding != null && IconResourceConfig != null
                ? IconResourceConfig.GetEntry(binding.GetBindingKey()) : null;
            if (BindingText != null)
            {
                BindingText.gameObject.SetActive(binding != null && (BindingImage == null || entry?.Sprite == null));
                BindingText.text = binding?.GetDisplayText() ?? string.Empty;
            }
            if (BindingImage == null)
                return;
            if (binding == null || IconResourceConfig == null)
            {
                BindingImage.sprite = null;
                BindingImage.enabled = false;
                return;
            }

            BindingImage.sprite = entry != null ? entry.Sprite : null;
            BindingImage.enabled = BindingImage.sprite != null;

            if (BindingImage.sprite == null)
                return;

            RectTransform rect = BindingImage.rectTransform;
            Vector2 size = rect.sizeDelta;
            Rect spriteRect = BindingImage.sprite.rect;
            float aspect = spriteRect.height > 0f ? spriteRect.width / spriteRect.height : 1f;
            size.x = size.y * aspect;
            rect.sizeDelta = size;
        }
    }
}
