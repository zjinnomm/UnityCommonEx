using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public abstract class ButtonInputActionWrapper<TAction> : NodeController, IInputActionWrapper<TAction> where TAction : struct, Enum
    {
        public TAction Action;
        public int Priority;
        public Button Button;
        public ButtonSFXPlayer SFXPlayer;
        public Image BindingImage;
        public InputBindingIconResourceConfig IconResourceConfig;

        TAction IInputActionWrapper<TAction>.Action => Action;
        int IInputActionWrapper<TAction>.Priority => Priority;

        protected abstract InputManager<TAction> GetInputManager();

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
            if (BindingImage == null)
                return;

            InputBinding binding = inputManager?.GetPendingBinding(Action, 0);
            if (binding == null || IconResourceConfig == null)
            {
                BindingImage.sprite = null;
                BindingImage.enabled = false;
                return;
            }

            InputBindingIconResourceConfig.Entry entry = IconResourceConfig.GetEntry(binding.GetBindingKey());
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
