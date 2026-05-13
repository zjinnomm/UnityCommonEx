using System;
using TMPro;
using UnityEngine;

namespace UnityCommonEx
{
    public class GeneralOptionDialogButtonConfig
    {
        public string Title;
        public string Subtitle;
        public Sprite Icon;
        public Action Callback;
        public bool Interactable = true;
    }

    public struct GeneralOptionDialogConfig
    {
        public string Message;
        public GeneralOptionDialogButtonConfig Button1;
        public GeneralOptionDialogButtonConfig Button2;
        public GeneralOptionDialogButtonConfig Button3;
    }

    public class GeneralOptionDialogController : NodeController
    {

        public TMP_Text MessageText;
        public RichButtonController Button1;
        public RichButtonController Button2;
        public RichButtonController Button3;

        private const int ButtonSlotCount = 3;
        private readonly Action[] callbacks = new Action[ButtonSlotCount];
        private RichButtonController[] buttons;

        protected override void OnInit()
        {
            base.OnInit();
            buttons = new[] { Button1, Button2, Button3 };
            Clear();
        }

        public void Set(in GeneralOptionDialogConfig config)
        {
            Clear();
            MessageText.text = config.Message;
            ApplyButton(0, config.Button1);
            ApplyButton(1, config.Button2);
            ApplyButton(2, config.Button3);
        }

        void ApplyButton(int index, GeneralOptionDialogButtonConfig config)
        {
            if (buttons == null || index < 0 || index >= buttons.Length)
                return;

            RichButtonController button = buttons[index];
            if (button == null)
                return;

            bool visible = config != null && !string.IsNullOrEmpty(config.Title);
            button.gameObject.SetActive(visible);
            callbacks[index] = visible ? config.Callback : null;
            if (!visible)
                return;

            button.Set(config.Title, () => { OnButtonClick(index); }, config.Icon, config.Subtitle, config.Interactable);
        }

        void OnButtonClick(int index)
        {
            if (index >= 0 && index < callbacks.Length)
            {
                callbacks[index]?.Invoke();
            }
            Deactivate();
        }

        public void Clear()
        {
            MessageText.text = null;
            if (buttons == null)
                return;

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < callbacks.Length; i++)
                callbacks[i] = null;
        }

    }

}
